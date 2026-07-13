using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

namespace NeonImperium.WorldGenerations
{
    public class PlacementValidator : IPlacementValidator
    {
        private readonly RaycastHit[] _singleHit = new RaycastHit[1];
        private readonly System.Random _random;

        public PlacementValidator(int seed)
        {
            _random = new System.Random(seed);
        }

        public PlacementData Validate(PlacementPoint point, SpawnSettings settings, Transform spawner, List<PlacementData> existingPoints, IDebugLogger debugLogger, IPhysicsService physicsService)
        {
            Vector3 worldPos = spawner.TransformPoint(point.localPosition);
            float halfX = settings.dimensions.x * 0.5f;
            float halfY = settings.dimensions.y * 0.5f;
            float halfZ = settings.dimensions.z * 0.5f;

            Vector3 rayStart = GetRayStart(point, settings, spawner, halfX, halfY, halfZ);
            Vector3 rayDirection = Vector3.down;
            float rayDistance = settings.dimensions.y;

            SetRayDirection(settings, ref rayDirection, ref rayDistance);

            bool hasHit;
            RaycastHit hit;
            Color rayColor;

            switch (settings.rayCastType)
            {
                case RayCastType.Sphere:
                    hasHit = SphereCastNonAlloc(physicsService, rayStart, settings.raySphereRadius, rayDirection, out hit, rayDistance, settings.collisionMask);
                    rayColor = hasHit ? Color.green : Color.red;
                    break;
                case RayCastType.Ray:
                default:
                    hasHit = RaycastNonAlloc(physicsService, rayStart, rayDirection, out hit, rayDistance, settings.collisionMask);
                    rayColor = hasHit ? Color.blue : Color.red;
                    break;
            }

            debugLogger?.AddDebugRay(rayStart, rayDirection, hasHit ? hit.distance : rayDistance, rayColor, DebugRayType.Main);

            if (!hasHit)
                return InvalidResult(worldPos, FailureReasonType.NoHit);

            if (settings.maxSurfaceAngle > 0f)
            {
                float angle = Vector3.Angle(hit.normal, Vector3.up);
                if (angle > settings.maxSurfaceAngle)
                    return InvalidResult(worldPos, FailureReasonType.InvalidLayer);
            }

            if (settings.avoidanceRadius > 0)
            {
                bool obstacleNear = physicsService.CheckSphere(hit.point, settings.avoidanceRadius, settings.avoidMask);
                if (obstacleNear)
                {
                    debugLogger?.AddDebugRay(hit.point, Vector3.up, 1f, Color.yellow, DebugRayType.Avoidance);
                    return InvalidResult(worldPos, FailureReasonType.NearObstacle);
                }
            }

            if (settings.useNavMeshCheck)
            {
                Vector3 hitPoint = hit.point;
                float sampleRadius = settings.navMeshSampleRadius > 0 ? settings.navMeshSampleRadius : 1f;
                if (NavMesh.SamplePosition(hitPoint, out NavMeshHit navHit, sampleRadius, NavMesh.AllAreas))
                {
                    if (settings.snapToNavMesh)
                    {
                        hit.point = navHit.position;
                    }
                    else
                    {
                        float distanceToNavMesh = Vector3.Distance(hitPoint, navHit.position);
                        if (distanceToNavMesh > 0.01f)
                        {
                            debugLogger?.AddDebugRay(hit.point, Vector3.up, 0.5f, Color.red, DebugRayType.Main);
                            return InvalidResult(worldPos, FailureReasonType.NoNavMesh);
                        }
                    }
                }
                else
                {
                    debugLogger?.AddDebugRay(hit.point, Vector3.up, 0.5f, Color.red, DebugRayType.Main);
                    return InvalidResult(worldPos, FailureReasonType.NoNavMesh);
                }
            }

            if (settings.edgeCheckRadius > 0 && !IsSurfaceStableByHeight(hit.point, hit.normal, settings, debugLogger, physicsService))
                return InvalidResult(worldPos, FailureReasonType.EdgeCheck);

            if (settings.checkCeiling && settings.avoidMask != 0)
            {
                float topHeight = spawner.position.y + halfY;
                float distanceUp = topHeight - hit.point.y;
                if (distanceUp > 0 && RaycastNonAlloc(physicsService, hit.point, Vector3.up, out RaycastHit ceilingHit, distanceUp, settings.avoidMask))
                {
                    debugLogger?.AddDebugRay(hit.point, Vector3.up, distanceUp, Color.magenta, DebugRayType.Ceiling);
                    return InvalidResult(worldPos, FailureReasonType.CeilingCheck);
                }
            }

            if (settings.floorCheckDistance > 0 && settings.avoidMask != 0)
            {
                if (HasFloorHole(hit.point, settings, debugLogger, physicsService))
                    return InvalidResult(worldPos, FailureReasonType.FloorCheck);
            }

            bool validPosition = (settings.avoidMask.value & (1 << hit.collider.gameObject.layer)) == 0;
            if (!validPosition)
                return InvalidResult(worldPos, FailureReasonType.InvalidLayer);

            if (!IsPointInSpawnArea(hit.point, settings, spawner))
                return InvalidResult(worldPos, FailureReasonType.OutOfBounds);

            return CreateValidPoint(settings, hit, spawner, existingPoints, worldPos);
        }

        private Vector3 GetRayStart(PlacementPoint point, SpawnSettings settings, Transform spawner, float halfX, float halfY, float halfZ)
        {
            Vector3 localStart = Vector3.zero;
            switch (settings.rayOriginType)
            {
                case RayOriginType.SideFaces:
                    int side = _random.Next(0, 4);
                    float offsetY = (float)(_random.NextDouble() * 2.0 - 1.0) * halfY;
                    localStart = side switch
                    {
                        0 => new Vector3(-halfX, offsetY, 0),
                        1 => new Vector3(halfX, offsetY, 0),
                        2 => new Vector3(0, offsetY, -halfZ),
                        _ => new Vector3(0, offsetY, halfZ)
                    };
                    break;
                case RayOriginType.InsideVolume:
                    localStart = new Vector3(
                        (float)(_random.NextDouble() * 2.0 - 1.0) * halfX,
                        (float)(_random.NextDouble() * 2.0 - 1.0) * halfY,
                        (float)(_random.NextDouble() * 2.0 - 1.0) * halfZ
                    );
                    break;
                default:
                    localStart = new Vector3(point.localPosition.x, halfY, point.localPosition.z);
                    break;
            }
            return spawner.TransformPoint(localStart);
        }

        private void SetRayDirection(SpawnSettings settings, ref Vector3 rayDirection, ref float rayDistance)
        {
            if (settings.maxRayAngle == Vector2.zero) return;
            float minAngle = Mathf.Min(settings.maxRayAngle.x, settings.maxRayAngle.y);
            float maxAngle = Mathf.Max(settings.maxRayAngle.x, settings.maxRayAngle.y);
            float angle = (float)(_random.NextDouble() * (maxAngle - minAngle) + minAngle);
            float angleX = (float)(_random.NextDouble() * 2.0 - 1.0) * angle;
            float angleZ = (float)(_random.NextDouble() * 2.0 - 1.0) * angle;
            rayDirection = Quaternion.Euler(angleX, 0, angleZ) * Vector3.down;
            rayDistance *= 1.5f;
        }

        private PlacementData InvalidResult(Vector3 worldPos, FailureReasonType reason) => new PlacementData
        {
            isValid = false,
            position = worldPos,
            failureReason = reason
        };

        private PlacementData CreateValidPoint(SpawnSettings settings, RaycastHit hit, Transform spawner, List<PlacementData> existingPoints, Vector3 worldPos)
        {
            float verticalOffset = (float)(_random.NextDouble() * (settings.verticalOffset.y - settings.verticalOffset.x) + settings.verticalOffset.x);
            float rotY = (float)(_random.NextDouble() * (settings.rotationRange.y - settings.rotationRange.x) + settings.rotationRange.x);
            float scaleVal = (float)(_random.NextDouble() * (settings.scaleRange.y - settings.scaleRange.x) + settings.scaleRange.x);
            scaleVal = 0.1f * Mathf.Round(scaleVal / 0.1f);
            GameObject selectedPrefab = settings.prefabs.Length > 0 ? settings.prefabs[_random.Next(0, settings.prefabs.Length)] : null;

            PlacementData result = new PlacementData
            {
                isValid = true,
                position = hit.point + Vector3.up * verticalOffset,
                rotation = settings.alignToSurface ?
                    Quaternion.FromToRotation(Vector3.up, hit.normal) * Quaternion.Euler(0, rotY, 0) :
                    Quaternion.Euler(0, rotY, 0),
                scale = scaleVal * Vector3.one,
                prefab = selectedPrefab
            };

            if (existingPoints != null && settings.minDistanceBetweenObjects > 0)
            {
                foreach (PlacementData existing in existingPoints)
                {
                    if (existing.isValid && Vector3.Distance(result.position, existing.position) < settings.minDistanceBetweenObjects)
                    {
                        result.isValid = false;
                        result.failureReason = FailureReasonType.TooCloseToOther;
                        result.position = worldPos;
                        return result;
                    }
                }
            }
            return result;
        }

        private bool IsPointInSpawnArea(Vector3 worldPoint, SpawnSettings settings, Transform spawner)
        {
            Vector3 localPoint = spawner.InverseTransformPoint(worldPoint);
            Vector3 halfDims = settings.dimensions * 0.5f;
            return Mathf.Abs(localPoint.x) <= halfDims.x && Mathf.Abs(localPoint.z) <= halfDims.z &&
                   localPoint.y >= -halfDims.y && localPoint.y <= halfDims.y;
        }

        private bool IsSurfaceStableByHeight(Vector3 centerPosition, Vector3 surfaceNormal, SpawnSettings settings, IDebugLogger debugLogger, IPhysicsService physicsService)
        {
            if (settings.edgeCheckRadius <= 0) return true;
            int successCount = 0;
            int totalPoints = settings.stabilityCheckRays;
            float surfaceAngle = Vector3.Angle(surfaceNormal, Vector3.up);
            float adaptiveHeightDifference = CalculateAdaptiveHeightDifference(settings.maxHeightDifference, surfaceAngle);
            Vector3 tangent = Vector3.Cross(surfaceNormal, Vector3.up).normalized;
            if (tangent.magnitude < 0.1f)
                tangent = Vector3.Cross(surfaceNormal, Vector3.forward).normalized;
            Vector3 bitangent = Vector3.Cross(surfaceNormal, tangent).normalized;

            for (int i = 0; i < totalPoints; i++)
            {
                float angle = i * (360f / totalPoints);
                float radians = angle * Mathf.Deg2Rad;
                Vector3 direction = (tangent * Mathf.Cos(radians) + bitangent * Mathf.Sin(radians)).normalized;
                Vector3 checkPoint = centerPosition + direction * settings.edgeCheckRadius;
                Vector3 rayDirection = -surfaceNormal;
                Vector3 rayStart = checkPoint + surfaceNormal * 2f;
                if (RaycastNonAlloc(physicsService, rayStart, rayDirection, out RaycastHit hit, 4f, settings.collisionMask))
                {
                    float heightDifference = CalculateHeightDifferenceRelativeToSurface(centerPosition, hit.point, surfaceNormal);
                    if (Mathf.Abs(heightDifference) <= adaptiveHeightDifference)
                    {
                        successCount++;
                        debugLogger?.AddDebugRay(rayStart, rayDirection, hit.distance, Color.green, DebugRayType.Stability);
                    }
                    else
                    {
                        debugLogger?.AddDebugRay(rayStart, rayDirection, hit.distance, Color.red, DebugRayType.Stability);
                    }
                }
                else
                {
                    debugLogger?.AddDebugRay(rayStart, rayDirection, 4f, Color.magenta, DebugRayType.Stability);
                }
            }

            float successPercentage = (float)successCount / totalPoints * 100f;
            bool isStable = successPercentage >= settings.minSuccessPercentage;
            if (debugLogger != null)
            {
                debugLogger.AddDebugRay(centerPosition, surfaceNormal, 1f, isStable ? Color.cyan : Color.yellow, DebugRayType.Stability);
            }
            return isStable;
        }

        private float CalculateAdaptiveHeightDifference(float baseHeightDifference, float surfaceAngle)
        {
            if (surfaceAngle <= 15f)
                return baseHeightDifference;
            if (surfaceAngle <= 45f)
                return baseHeightDifference * (1f + (surfaceAngle - 15f) / 60f);
            return baseHeightDifference * 2f;
        }

        private float CalculateHeightDifferenceRelativeToSurface(Vector3 centerPoint, Vector3 checkPoint, Vector3 surfaceNormal)
        {
            Vector3 direction = checkPoint - centerPoint;
            float projection = Vector3.Dot(direction, surfaceNormal);
            return projection;
        }

        private bool HasFloorHole(Vector3 position, SpawnSettings settings, IDebugLogger debugLogger, IPhysicsService physicsService)
        {
            Vector3 rayStart = position + Vector3.up * 0.1f;
            float checkDistance = settings.floorCheckDistance + 0.1f;
            if (!RaycastNonAlloc(physicsService, rayStart, Vector3.down, out RaycastHit hit, checkDistance, settings.collisionMask))
            {
                debugLogger?.AddDebugRay(rayStart, Vector3.down, checkDistance, Color.red, DebugRayType.Floor);
                return true;
            }
            debugLogger?.AddDebugRay(rayStart, Vector3.down, hit.distance, Color.green, DebugRayType.Floor);
            return false;
        }

        private bool RaycastNonAlloc(IPhysicsService physicsService, Vector3 origin, Vector3 direction, out RaycastHit hit, float maxDistance, int layerMask)
        {
            int count = physicsService.RaycastNonAlloc(origin, direction, _singleHit, maxDistance, layerMask);
            if (count > 0)
            {
                hit = _singleHit[0];
                return true;
            }
            hit = default;
            return false;
        }

        private bool SphereCastNonAlloc(IPhysicsService physicsService, Vector3 origin, float radius, Vector3 direction, out RaycastHit hit, float maxDistance, int layerMask)
        {
            int count = physicsService.SphereCastNonAlloc(origin, radius, direction, _singleHit, maxDistance, layerMask);
            if (count > 0)
            {
                hit = _singleHit[0];
                return true;
            }
            hit = default;
            return false;
        }
    }
}