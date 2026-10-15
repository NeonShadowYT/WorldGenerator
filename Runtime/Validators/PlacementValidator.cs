using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.AI;

#if UNITY_MATHEMATICS
using Unity.Mathematics;
#endif

namespace NeonImperium.WorldGenerations
{
    public sealed class PlacementValidator : IPlacementValidator
    {
        // ──────────────────────── Константы (H4) ────────────────────────

        /// <summary>Множитель дистанции при наклонном луче (случайный maxRayAngle != 0).</summary>
        private const float RayAngleDistanceMultiplier = 1.5f;

        /// <summary>Допуск «точка уже на NavMesh» в метрах.</summary>
        private const float NavMeshPositionTolerance = 0.01f;

        /// <summary>Офсет старта луча над поверхностью (floor / ceiling checks).</summary>
        private const float SurfaceRayStartOffset = 0.1f;

        /// <summary>Шаг квантования масштаба (для детерминизма между сессиями).</summary>
        private const float ScaleQuantizationStep = 0.1f;

        /// <summary>Офсет старта лучей проверки стабильности над центром.</summary>
        private const float StabilityRayStartOffset = 2f;

        /// <summary>Максимальная дистанция лучей проверки стабильности.</summary>
        private const float StabilityRayDistance = 4f;

        /// <summary>Порог вырожденности tangent-вектора.</summary>
        private const float TangentEpsilonSq = 1e-6f;

        // ──────────────────────── Поля ────────────────────────

        private readonly RaycastHit[] _singleHit = new RaycastHit[1];
        private readonly FastRandom _rng;
        private readonly List<ISpawnZone> _zones;
        private readonly SpawnSettings _settings;
        private readonly PlacementJobs _jobs;

        // Кэш горячих параметров.
        private readonly GameObject[] _prefabs;
        private readonly int _prefabCount;
        private readonly float _vOffMin, _vOffRange;
        private readonly float _rotMin, _rotRange;
        private readonly float _scaleMin, _scaleRange;
        private readonly float _rayAngMin, _rayAngRange;
        private readonly bool _hasRayAngle;
        private readonly float _minDistSq;
        private readonly bool _useMinDist;

        // Surface angle range (min/max).
        private readonly float _minSurfaceAngleDeg;
        private readonly float _maxSurfaceAngleDeg;
        private readonly bool _checkSurfaceAngle;

        private Vector3[] _batchRayStarts = new Vector3[64];
        private Vector3[] _batchRayDirs = new Vector3[64];
        private float[] _batchRayDists = new float[64];
        private int[] _batchZoneIndices = new int[64];

        private bool _disposed;

        /// <summary>true, если job-путь технически доступен (RaycastCommand).</summary>
        public bool JobsAvailable => !_disposed && _jobs != null && _jobs.IsAvailable;

        public PlacementValidator(SpawnSettings settings, int seed, List<ISpawnZone> zones)
        {
            _settings = settings;
            _rng = new FastRandom(seed);
            _zones = zones;
            _jobs = new PlacementJobs();

            _prefabs = settings.prefabs ?? Array.Empty<GameObject>();
            _prefabCount = _prefabs.Length;

            _vOffMin = Mathf.Min(settings.verticalOffset.x, settings.verticalOffset.y);
            _vOffRange = Mathf.Abs(settings.verticalOffset.y - settings.verticalOffset.x);

            _rotMin = Mathf.Min(settings.rotationRange.x, settings.rotationRange.y);
            _rotRange = Mathf.Abs(settings.rotationRange.y - settings.rotationRange.x);

            _scaleMin = Mathf.Min(settings.scaleRange.x, settings.scaleRange.y);
            _scaleRange = Mathf.Abs(settings.scaleRange.y - settings.scaleRange.x);

            float rayA = settings.maxRayAngle.x;
            float rayB = settings.maxRayAngle.y;
            _rayAngMin = Mathf.Min(rayA, rayB);
            _rayAngRange = Mathf.Abs(rayB - rayA);
            _hasRayAngle = _rayAngRange > 0f || _rayAngMin > 0f;

            float surfA = settings.surfaceAngleRange.x;
            float surfB = settings.surfaceAngleRange.y;
            _minSurfaceAngleDeg = Mathf.Min(surfA, surfB);
            _maxSurfaceAngleDeg = Mathf.Max(surfA, surfB);
            _checkSurfaceAngle = _maxSurfaceAngleDeg > 0f;

            _useMinDist = settings.minDistanceBetweenObjects > 0f;
            _minDistSq = settings.minDistanceBetweenObjects * settings.minDistanceBetweenObjects;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _jobs?.Dispose();
        }

        // ──────────────────────── Single ────────────────────────

        public PlacementData Validate(PlacementPoint point, Transform spawner,
            List<PlacementData> existingPoints, IDebugLogger debugLogger, IPhysicsService physicsService,
            SpatialHash placementHash)
        {
            SpawnSettings settings = _settings;
            Vector3 worldPos = spawner.TransformPoint(point.localPosition);
            ISpawnZone zone = GetZone(point.zoneIndex);

            float rayStartY = zone != null ? zone.RayStartY : settings.dimensions.y * 0.5f;
            float rayDistance = zone != null ? zone.RayDistance : settings.dimensions.y;

            Vector3 rayStart = GetRayStart(point, zone, settings, spawner, rayStartY);
            Vector3 rayDirection = Vector3.down;
            SetRayDirection(ref rayDirection, ref rayDistance);

            bool hasHit;
            RaycastHit hit;

            if (settings.rayCastType == RayCastType.Sphere)
                hasHit = SphereCastNonAlloc(physicsService, rayStart, settings.raySphereRadius,
                    rayDirection, out hit, rayDistance, settings.collisionMask);
            else
                hasHit = RaycastNonAlloc(physicsService, rayStart, rayDirection,
                    out hit, rayDistance, settings.collisionMask);

            return PostProcessHit(point, zone, worldPos, rayStart, rayDirection, rayDistance,
                hasHit, hit, spawner, debugLogger, physicsService, placementHash, existingPoints);
        }

        // ──────────────────────── Batch ────────────────────────

        public void ValidateBatch(List<PlacementPoint> points, int startIndex, int count,
            Transform spawner, IDebugLogger debugLogger, IPhysicsService physicsService,
            SpatialHash placementHash, PlacementData[] output)
        {
            if (count <= 0) return;
            if (points == null) throw new ArgumentNullException(nameof(points));
            if (output == null || output.Length < count)
                throw new ArgumentException("Output array too small.", nameof(output));

            if (TryValidateBatchJob(points, startIndex, count, spawner,
                debugLogger, physicsService, placementHash, output))
                return;

            for (int i = 0; i < count; i++)
                output[i] = Validate(points[startIndex + i], spawner, null,
                    debugLogger, physicsService, placementHash);
        }

        /// <summary>Job-путь. Возвращает false, если недоступен (Sphere-cast / отключено / disposed).</summary>
        private bool TryValidateBatchJob(List<PlacementPoint> points, int startIndex, int count,
            Transform spawner, IDebugLogger debugLogger, IPhysicsService physicsService,
            SpatialHash placementHash, PlacementData[] output)
        {
#if UNITY_2022_2_OR_NEWER
            if (_disposed) return false;
            if (!_settings.useJobValidation) return false;
            if (_settings.rayCastType != RayCastType.Ray) return false;
            if (_jobs == null || !_jobs.IsAvailable) return false;

            EnsureBatchCapacity(count);
            _jobs.EnsureCapacity(count);

            SpawnSettings s = _settings;
            QueryParameters queryParams = new QueryParameters { layerMask = s.collisionMask };

            for (int i = 0; i < count; i++)
            {
                PlacementPoint point = points[startIndex + i];
                ISpawnZone zone = GetZone(point.zoneIndex);

                float rayStartY = zone != null ? zone.RayStartY : s.dimensions.y * 0.5f;
                float rayDistance = zone != null ? zone.RayDistance : s.dimensions.y;

                Vector3 rayStart = GetRayStart(point, zone, s, spawner, rayStartY);
                Vector3 rayDirection = Vector3.down;
                SetRayDirection(ref rayDirection, ref rayDistance);

                _batchRayStarts[i] = rayStart;
                _batchRayDirs[i] = rayDirection;
                _batchRayDists[i] = rayDistance;
                _batchZoneIndices[i] = point.zoneIndex;

                _jobs.SetCommand(i, new RaycastCommand(rayStart, rayDirection, queryParams, rayDistance));
            }

            _jobs.Execute(count, s.jobBatchSize);

            for (int i = 0; i < count; i++)
            {
                PlacementPoint point = points[startIndex + i];
                ISpawnZone zone = GetZone(_batchZoneIndices[i]);
                Vector3 worldPos = spawner.TransformPoint(point.localPosition);

                RaycastHit hit = _jobs.GetResult(i);
                bool hasHit = hit.collider != null;

                output[i] = PostProcessHit(point, zone, worldPos,
                    _batchRayStarts[i], _batchRayDirs[i], _batchRayDists[i],
                    hasHit, hit, spawner, debugLogger, physicsService, placementHash, null);
            }
            return true;
#else
            return false;
#endif
        }

        private void EnsureBatchCapacity(int n)
        {
            if (_batchRayStarts.Length >= n) return;
            int cap = Mathf.NextPowerOfTwo(n);
            _batchRayStarts = new Vector3[cap];
            _batchRayDirs = new Vector3[cap];
            _batchRayDists = new float[cap];
            _batchZoneIndices = new int[cap];
        }

        // ──────────────────────── Пост-обработка ────────────────────────

        private PlacementData PostProcessHit(PlacementPoint point, ISpawnZone zone, Vector3 worldPos,
            Vector3 rayStart, Vector3 rayDirection, float rayDistance, bool hasHit, RaycastHit hit,
            Transform spawner, IDebugLogger debugLogger, IPhysicsService physicsService,
            SpatialHash placementHash, List<PlacementData> existingPoints)
        {
            SpawnSettings settings = _settings;

            if (!hasHit)
            {
                debugLogger.TryAddRay(rayStart, rayDirection, rayDistance, Color.red, DebugRayType.Main);
                return InvalidResult(worldPos, FailureReasonType.NoHit);
            }

            Color mainColor = settings.rayCastType == RayCastType.Sphere ? Color.green : Color.blue;
            debugLogger.TryAddRay(rayStart, rayDirection, hit.distance, mainColor, DebugRayType.Main);

            if (_checkSurfaceAngle && !IsSurfaceAngleValid(hit.normal))
                return InvalidResult(worldPos, FailureReasonType.InvalidSurfaceAngle);

            if (settings.avoidanceRadius > 0 &&
                physicsService.CheckSphere(hit.point, settings.avoidanceRadius, settings.avoidMask))
            {
                debugLogger.TryAddRay(hit.point, Vector3.up, 1f, Color.yellow, DebugRayType.Avoidance);
                return InvalidResult(worldPos, FailureReasonType.NearObstacle);
            }

            if (settings.useNavMeshCheck && !ValidateNavMesh(ref hit, settings, debugLogger))
                return InvalidResult(worldPos, FailureReasonType.NoNavMesh);

            if (settings.edgeCheckRadius > 0 &&
                !IsSurfaceStableByHeight(hit.point, hit.normal, settings, debugLogger, physicsService))
                return InvalidResult(worldPos, FailureReasonType.EdgeCheck);

            if (settings.ceilingCheckMode != CeilingCheckMode.None &&
                !CheckCeiling(hit.point, settings, debugLogger, physicsService))
                return InvalidResult(worldPos, FailureReasonType.CeilingCheck);

            if (settings.floorCheckDistance > 0 && settings.avoidMask != 0 &&
                HasFloorHole(hit.point, settings, debugLogger, physicsService))
                return InvalidResult(worldPos, FailureReasonType.FloorCheck);

            if ((settings.avoidMask.value & (1 << hit.collider.gameObject.layer)) != 0)
                return InvalidResult(worldPos, FailureReasonType.InvalidLayer);

            if (!IsPointInSpawnArea(hit.point, settings, spawner, point.zoneIndex))
                return InvalidResult(worldPos, FailureReasonType.OutOfBounds);

            return CreateValidPoint(settings, hit, existingPoints, placementHash, worldPos);
        }

        // ──────────────────────── Проверки ────────────────────────

        /// <summary>Угол наклона поверхности от вертикали: 0° — пол, 90° — стена, 180° — потолок.</summary>
        private static float SurfaceAngleDegrees(Vector3 normal)
        {
#if UNITY_MATHEMATICS
            float cos = math.clamp(normal.y, -1f, 1f);
            return math.degrees(math.acos(cos));
#else
            return Vector3.Angle(normal, Vector3.up);
#endif
        }

        /// <summary>true, если угол попадает в [_minSurfaceAngleDeg, _maxSurfaceAngleDeg].</summary>
        private bool IsSurfaceAngleValid(Vector3 normal)
        {
            float angle = SurfaceAngleDegrees(normal);
            return angle >= _minSurfaceAngleDeg && angle <= _maxSurfaceAngleDeg;
        }

        private ISpawnZone GetZone(int index) =>
            (_zones != null && index >= 0 && index < _zones.Count) ? _zones[index] : null;

        private bool CheckCeiling(Vector3 point, SpawnSettings settings,
            IDebugLogger debugLogger, IPhysicsService physicsService)
        {
            int mask = settings.ceilingMask.value != 0 ? settings.ceilingMask.value : settings.avoidMask.value;
            if (mask == 0) return true;

            Vector3 rayStart = point + Vector3.up * SurfaceRayStartOffset;
            float distance = settings.ceilingCheckDistance;

            bool hasHit;
            RaycastHit hit;
            if (settings.ceilingCheckRadius > 0f)
                hasHit = SphereCastNonAlloc(physicsService, rayStart, settings.ceilingCheckRadius,
                    Vector3.up, out hit, distance, mask);
            else
                hasHit = RaycastNonAlloc(physicsService, rayStart, Vector3.up, out hit, distance, mask);

            switch (settings.ceilingCheckMode)
            {
                case CeilingCheckMode.RequireOpenSky:
                    if (hasHit)
                    {
                        debugLogger.TryAddRay(rayStart, Vector3.up, hit.distance, Color.red, DebugRayType.Ceiling);
                        return false;
                    }
                    debugLogger.TryAddRay(rayStart, Vector3.up, distance, Color.green, DebugRayType.Ceiling);
                    return true;

                case CeilingCheckMode.RequireRoof:
                    if (!hasHit)
                    {
                        debugLogger.TryAddRay(rayStart, Vector3.up, distance, Color.red, DebugRayType.Ceiling);
                        return false;
                    }
                    debugLogger.TryAddRay(rayStart, Vector3.up, hit.distance, Color.green, DebugRayType.Ceiling);
                    return true;
            }
            return true;
        }

        private Vector3 GetRayStart(PlacementPoint point, ISpawnZone zone, SpawnSettings settings,
            Transform spawner, float rayStartY)
        {
            Vector3 localStart;

            switch (settings.rayOriginType)
            {
                case RayOriginType.SideFaces:
                    if (zone != null)
                    {
                        Bounds b = zone.LocalBounds;
                        int side = _rng.Next(4);
                        float offsetY = _rng.Range(b.min.y, b.max.y);
                        localStart = side switch
                        {
                            0 => new Vector3(b.min.x, offsetY, b.center.z),
                            1 => new Vector3(b.max.x, offsetY, b.center.z),
                            2 => new Vector3(b.center.x, offsetY, b.min.z),
                            _ => new Vector3(b.center.x, offsetY, b.max.z)
                        };
                    }
                    else
                    {
                        float hx = settings.dimensions.x * 0.5f;
                        float hy = settings.dimensions.y * 0.5f;
                        float hz = settings.dimensions.z * 0.5f;
                        int side = _rng.Next(4);
                        float oy = _rng.Range(-hy, hy);
                        localStart = side switch
                        {
                            0 => new Vector3(-hx, oy, 0),
                            1 => new Vector3(hx, oy, 0),
                            2 => new Vector3(0, oy, -hz),
                            _ => new Vector3(0, oy, hz)
                        };
                    }
                    break;

                case RayOriginType.InsideVolume:
                    if (zone != null)
                    {
                        Bounds b = zone.LocalBounds;
                        localStart = new Vector3(
                            _rng.Range(b.min.x, b.max.x),
                            _rng.Range(b.min.y, b.max.y),
                            _rng.Range(b.min.z, b.max.z));
                    }
                    else
                    {
                        float hx = settings.dimensions.x * 0.5f;
                        float hy = settings.dimensions.y * 0.5f;
                        float hz = settings.dimensions.z * 0.5f;
                        localStart = new Vector3(
                            _rng.Range(-hx, hx),
                            _rng.Range(-hy, hy),
                            _rng.Range(-hz, hz));
                    }
                    break;

                default:
                    localStart = new Vector3(point.localPosition.x, rayStartY, point.localPosition.z);
                    break;
            }

            return spawner.TransformPoint(localStart);
        }

        private void SetRayDirection(ref Vector3 rayDirection, ref float rayDistance)
        {
            if (!_hasRayAngle) return;
            float angle = _rng.Range(_rayAngMin, _rayAngMin + _rayAngRange);
            float ax = _rng.Range(-angle, angle);
            float az = _rng.Range(-angle, angle);
            rayDirection = Quaternion.Euler(ax, 0f, az) * Vector3.down;
            rayDistance *= RayAngleDistanceMultiplier;
        }

        private bool ValidateNavMesh(ref RaycastHit hit, SpawnSettings settings, IDebugLogger debugLogger)
        {
            Vector3 hp = hit.point;
            float sampleRadius = settings.navMeshSampleRadius > 0 ? settings.navMeshSampleRadius : 1f;

            if (!NavMesh.SamplePosition(hp, out NavMeshHit navHit, sampleRadius, NavMesh.AllAreas))
            {
                debugLogger.TryAddRay(hp, Vector3.up, 0.5f, Color.red, DebugRayType.Main);
                return false;
            }

            if (settings.snapToNavMesh)
            {
                hit.point = navHit.position;
                return true;
            }

            if (Vector3.Distance(hp, navHit.position) > NavMeshPositionTolerance)
            {
                debugLogger.TryAddRay(hp, Vector3.up, 0.5f, Color.red, DebugRayType.Main);
                return false;
            }
            return true;
        }

        private static PlacementData InvalidResult(Vector3 worldPos, FailureReasonType reason) => new()
        {
            isValid = false,
            position = worldPos,
            failureReason = reason
        };

        private PlacementData CreateValidPoint(SpawnSettings settings, RaycastHit hit,
            List<PlacementData> existingPoints, SpatialHash placementHash, Vector3 worldPos)
        {
            float vOffset = _rng.Range(_vOffMin, _vOffMin + _vOffRange);
            float rotY = _rng.Range(_rotMin, _rotMin + _rotRange);
            float scaleVal = _rng.Range(_scaleMin, _scaleMin + _scaleRange);
            scaleVal = ScaleQuantizationStep * Mathf.Round(scaleVal / ScaleQuantizationStep);
            GameObject selectedPrefab = _prefabCount > 0 ? _prefabs[_rng.Next(_prefabCount)] : null;

            PlacementData result = new()
            {
                isValid = true,
                position = hit.point + Vector3.up * vOffset,
                rotation = settings.alignToSurface
                    ? Quaternion.FromToRotation(Vector3.up, hit.normal) * Quaternion.Euler(0, rotY, 0)
                    : Quaternion.Euler(0, rotY, 0),
                scale = scaleVal * Vector3.one,
                prefab = selectedPrefab
            };

            if (!_useMinDist) return result;

            if (placementHash != null)
            {
                if (placementHash.HasWithin(result.position, settings.minDistanceBetweenObjects))
                {
                    result.isValid = false;
                    result.failureReason = FailureReasonType.TooCloseToOther;
                    result.position = worldPos;
                }
                return result;
            }

            if (existingPoints == null) return result;

            for (int i = 0; i < existingPoints.Count; i++)
            {
                PlacementData e = existingPoints[i];
                if (!e.isValid) continue;
                if ((result.position - e.position).sqrMagnitude < _minDistSq)
                {
                    result.isValid = false;
                    result.failureReason = FailureReasonType.TooCloseToOther;
                    result.position = worldPos;
                    break;
                }
            }
            return result;
        }

        private bool IsPointInSpawnArea(Vector3 worldPoint, SpawnSettings settings, Transform spawner, int zoneIndex)
        {
            Vector3 localPoint = spawner.InverseTransformPoint(worldPoint);
            ISpawnZone zone = GetZone(zoneIndex);

            if (zone == null)
            {
                Vector3 half = settings.dimensions * 0.5f;
                return Mathf.Abs(localPoint.x) <= half.x
                    && Mathf.Abs(localPoint.z) <= half.z
                    && localPoint.y >= -half.y && localPoint.y <= half.y;
            }

            if (zone is BoxSpawnZone)
            {
                if (!zone.ContainsXZ(localPoint.x, localPoint.z)) return false;
                Bounds b = zone.LocalBounds;
                return localPoint.y >= b.min.y && localPoint.y <= b.max.y;
            }

            if (!zone.ContainsXZ(localPoint.x, localPoint.z)) return false;

            float surfaceY = zone.GetSurfaceY(localPoint.x, localPoint.z, float.NaN);
            if (float.IsNaN(surfaceY)) return false;

            float halfSize = zone.LocalBounds.size.y * 0.5f;
            return localPoint.y >= surfaceY - halfSize && localPoint.y <= surfaceY + halfSize;
        }

        /// <summary>Проверка стабильности поверхности: tangent/bitangent через math при UNITY_MATHEMATICS.</summary>
        private bool IsSurfaceStableByHeight(Vector3 centerPosition, Vector3 surfaceNormal, SpawnSettings settings,
            IDebugLogger debugLogger, IPhysicsService physicsService)
        {
            if (settings.edgeCheckRadius <= 0) return true;

            int successCount = 0;
            int totalPoints = settings.stabilityCheckRays;

#if UNITY_MATHEMATICS
            float3 n = new float3(surfaceNormal.x, surfaceNormal.y, surfaceNormal.z);
            float cosAng = math.clamp(n.y, -1f, 1f);
            float surfaceAngle = math.degrees(math.acos(cosAng));
            float adaptive = CalculateAdaptiveHeightDifference(settings.maxHeightDifference, surfaceAngle);

            float3 up = new float3(0f, 1f, 0f);
            float3 t0 = math.cross(n, up);
            if (math.lengthsq(t0) < TangentEpsilonSq)
                t0 = math.cross(n, new float3(0f, 0f, 1f));
            float3 tangent = math.normalize(t0);
            float3 bitangent = math.normalize(math.cross(n, tangent));

            float3 cp = new float3(centerPosition.x, centerPosition.y, centerPosition.z);
            float radius = settings.edgeCheckRadius;
            int collisionMask = settings.collisionMask;

            for (int i = 0; i < totalPoints; i++)
            {
                float rad = i * (360f / totalPoints) * Mathf.Deg2Rad;
                float3 dir = math.normalize(tangent * math.cos(rad) + bitangent * math.sin(rad));
                float3 checkPoint = cp + dir * radius;
                float3 rayDir = -n;
                float3 rayStart = checkPoint + n * StabilityRayStartOffset;

                if (RaycastNonAlloc(physicsService, rayStart, rayDir, out RaycastHit hit, StabilityRayDistance, collisionMask))
                {
                    float3 hp = new float3(hit.point.x, hit.point.y, hit.point.z);
                    float diff = math.dot(hp - cp, n);
                    bool ok = math.abs(diff) <= adaptive;
                    debugLogger.TryAddRay(rayStart, rayDir, hit.distance, ok ? Color.green : Color.red, DebugRayType.Stability);
                    if (ok) successCount++;
                }
                else
                {
                    debugLogger.TryAddRay(rayStart, rayDir, StabilityRayDistance, Color.magenta, DebugRayType.Stability);
                }
            }
#else
            float surfaceAngle = Vector3.Angle(surfaceNormal, Vector3.up);
            float adaptive = CalculateAdaptiveHeightDifference(settings.maxHeightDifference, surfaceAngle);

            Vector3 tangent = Vector3.Cross(surfaceNormal, Vector3.up).normalized;
            if (tangent.sqrMagnitude < TangentEpsilonSq)
                tangent = Vector3.Cross(surfaceNormal, Vector3.forward).normalized;
            Vector3 bitangent = Vector3.Cross(surfaceNormal, tangent).normalized;

            float radius = settings.edgeCheckRadius;
            int collisionMask = settings.collisionMask;

            for (int i = 0; i < totalPoints; i++)
            {
                float rad = i * (360f / totalPoints) * Mathf.Deg2Rad;
                Vector3 dir = (tangent * Mathf.Cos(rad) + bitangent * Mathf.Sin(rad)).normalized;
                Vector3 checkPoint = centerPosition + dir * radius;
                Vector3 rayDir = -surfaceNormal;
                Vector3 rayStart = checkPoint + surfaceNormal * StabilityRayStartOffset;

                if (RaycastNonAlloc(physicsService, rayStart, rayDir, out RaycastHit hit, StabilityRayDistance, collisionMask))
                {
                    float diff = Vector3.Dot(hit.point - centerPosition, surfaceNormal);
                    bool ok = Mathf.Abs(diff) <= adaptive;
                    debugLogger.TryAddRay(rayStart, rayDir, hit.distance, ok ? Color.green : Color.red, DebugRayType.Stability);
                    if (ok) successCount++;
                }
                else
                {
                    debugLogger.TryAddRay(rayStart, rayDir, StabilityRayDistance, Color.magenta, DebugRayType.Stability);
                }
            }
#endif

            float percent = (float)successCount / totalPoints * 100f;
            bool stable = percent >= settings.minSuccessPercentage;
            debugLogger.TryAddRay(centerPosition, surfaceNormal, 1f,
                stable ? Color.cyan : Color.yellow, DebugRayType.Stability);
            return stable;
        }

        private static float CalculateAdaptiveHeightDifference(float baseDiff, float surfaceAngle)
        {
            if (surfaceAngle <= 15f) return baseDiff;
            if (surfaceAngle <= 45f) return baseDiff * (1f + (surfaceAngle - 15f) / 60f);
            return baseDiff * 2f;
        }

        private bool HasFloorHole(Vector3 position, SpawnSettings settings,
            IDebugLogger debugLogger, IPhysicsService physicsService)
        {
            Vector3 rayStart = position + Vector3.up * SurfaceRayStartOffset;
            float checkDistance = settings.floorCheckDistance + SurfaceRayStartOffset;

            if (!RaycastNonAlloc(physicsService, rayStart, Vector3.down, out RaycastHit hit, checkDistance, settings.collisionMask))
            {
                debugLogger.TryAddRay(rayStart, Vector3.down, checkDistance, Color.red, DebugRayType.Floor);
                return true;
            }
            debugLogger.TryAddRay(rayStart, Vector3.down, hit.distance, Color.green, DebugRayType.Floor);
            return false;
        }

        private bool RaycastNonAlloc(IPhysicsService physicsService, Vector3 origin, Vector3 direction,
            out RaycastHit hit, float maxDistance, int layerMask)
        {
            int count = physicsService.RaycastNonAlloc(origin, direction, _singleHit, maxDistance, layerMask);
            if (count > 0) { hit = _singleHit[0]; return true; }
            hit = default;
            return false;
        }

        private bool SphereCastNonAlloc(IPhysicsService physicsService, Vector3 origin, float radius,
            Vector3 direction, out RaycastHit hit, float maxDistance, int layerMask)
        {
            int count = physicsService.SphereCastNonAlloc(origin, radius, direction, _singleHit, maxDistance, layerMask);
            if (count > 0) { hit = _singleHit[0]; return true; }
            hit = default;
            return false;
        }
    }
}