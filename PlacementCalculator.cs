using UnityEngine;
using System.Collections.Generic;

namespace NeonImperium
{
    public static class PlacementCalculator
    {
        public static PlacementData CalculatePoint(PlacementPoint point, SpawnSettings settings, 
            Transform spawner, List<PlacementData> existingPoints, WorldGeneration debugSource)
        {
            Vector3 worldPos = spawner.TransformPoint(point.localPosition);
            float halfX = settings.dimensions.x * 0.5f;
            float halfY = settings.dimensions.y * 0.5f;
            float halfZ = settings.dimensions.z * 0.5f;
            
            Vector3 rayStart = GetRayStart(point, settings, spawner, halfX, halfY, halfZ);
            Vector3 rayDirection = Vector3.down;
            float rayDistance = settings.dimensions.y;

            SetRayDirection(settings, ref rayDirection, ref rayDistance);

            bool hasHit = false;
            RaycastHit hit = new();
            Color rayColor = Color.red;

            switch (settings.rayCastType)
            {
                case RayCastType.Sphere:
                    hasHit = Physics.SphereCast(
                        rayStart, settings.avoidanceRadius, rayDirection,
                        out hit, rayDistance, settings.collisionMask
                    );
                    rayColor = hasHit ? Color.green : Color.red;
                    break;

                case RayCastType.Ray:
                default:
                    hasHit = Physics.Raycast(
                        rayStart, rayDirection,
                        out hit, rayDistance, settings.collisionMask
                    );
                    rayColor = hasHit ? Color.blue : Color.red;

                    if (hasHit && settings.avoidanceRadius > 0)
                    {
                        bool obstacleNear = Physics.CheckSphere(
                            hit.point,
                            settings.avoidanceRadius,
                            settings.avoidMask
                        );

                        if (obstacleNear)
                        {
                            debugSource?.AddDebugRay(
                                hit.point,
                                Vector3.up,
                                1f,
                                Color.yellow,
                                DebugRayType.Avoidance
                            );
                            return InvalidResult(worldPos, FailureReasonType.NearObstacle);
                        }
                    }
                    break;
            }
            
            debugSource?.AddDebugRay(
                rayStart, 
                rayDirection, 
                hasHit ? hit.distance : rayDistance,
                rayColor,
                DebugRayType.Main
            );

            if (!hasHit) return InvalidResult(worldPos, FailureReasonType.NoHit);

            // ПРОВЕРКА СТАБИЛЬНОСТИ ПО ВЫСОТЕ ОТНОСИТЕЛЬНО НОРМАЛИ ПОВЕРХНОСТИ
            if (settings.edgeCheckRadius > 0 && !IsSurfaceStableByHeight(hit.point, hit.normal, settings, debugSource))
                return InvalidResult(worldPos, FailureReasonType.EdgeCheck);

            if (settings.checkCeiling && settings.avoidMask != 0)
            {
                float topHeight = spawner.position.y + halfY;
                float distanceUp = topHeight - hit.point.y;
                if (distanceUp > 0 && Physics.Raycast(hit.point, Vector3.up, distanceUp, settings.avoidMask))
                {
                    debugSource?.AddDebugRay(
                        hit.point,
                        Vector3.up,
                        distanceUp,
                        Color.magenta,
                        DebugRayType.Ceiling
                    );
                    return InvalidResult(worldPos, FailureReasonType.CeilingCheck);
                }
            }

            if (settings.floorCheckDistance > 0 && settings.avoidMask != 0)
            {
                if (HasFloorHole(hit.point, settings, debugSource))
                    return InvalidResult(worldPos, FailureReasonType.FloorCheck);
            }

            bool nearObstacle = settings.avoidanceRadius > 0 && settings.avoidMask != 0 && 
                HasNearbyObstacles(hit.point, settings);
            bool validPosition = (settings.avoidMask.value & (1 << hit.collider.gameObject.layer)) == 0;
            
            if (nearObstacle) 
                return InvalidResult(worldPos, FailureReasonType.NearObstacle);
                
            if (!validPosition) 
                return InvalidResult(worldPos, FailureReasonType.InvalidLayer);
                
            if (!IsPointInSpawnArea(hit.point, settings, spawner)) 
                return InvalidResult(worldPos, FailureReasonType.OutOfBounds);
            
            return CreateValidPoint(settings, hit, spawner, existingPoints, worldPos);
        }

        private static Vector3 GetRayStart(PlacementPoint point, SpawnSettings settings, 
                                         Transform spawner, float halfX, float halfY, float halfZ)
        {
            Vector3 localStart = Vector3.zero;
            
            switch (settings.rayOriginType)
            {
                case RayOriginType.SideFaces:
                    int side = Random.Range(0, 4);
                    float offsetY = Random.Range(-halfY, halfY);
                    
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
                        Random.Range(-halfX, halfX),
                        Random.Range(-halfY, halfY),
                        Random.Range(-halfZ, halfZ)
                    );
                    break;
                
                default:
                    localStart = new Vector3(point.localPosition.x, halfY, point.localPosition.z);
                    break;
            }
            
            return spawner.TransformPoint(localStart);
        }

        private static void SetRayDirection(SpawnSettings settings, ref Vector3 rayDirection, ref float rayDistance)
        {
            if (settings.maxRayAngle == Vector2.zero) return;
            
            float minAngle = Mathf.Min(settings.maxRayAngle.x, settings.maxRayAngle.y);
            float maxAngle = Mathf.Max(settings.maxRayAngle.x, settings.maxRayAngle.y);
            float angle = Random.Range(minAngle, maxAngle);
            
            float angleX = Random.Range(-angle, angle);
            float angleZ = Random.Range(-angle, angle);
            
            rayDirection = Quaternion.Euler(angleX, 0, angleZ) * Vector3.down;
            rayDistance *= 1.5f;
        }

        private static PlacementData InvalidResult(Vector3 worldPos, FailureReasonType reason) => new()
        {
            isValid = false,
            position = worldPos,
            failureReason = reason
        };

        private static PlacementData CreateValidPoint(SpawnSettings settings, RaycastHit hit, Transform spawner, 
            List<PlacementData> existingPoints, Vector3 worldPos)
        {
            float verticalOffset = Random.Range(settings.verticalOffset.x, settings.verticalOffset.y);
            
            var result = new PlacementData
            {
                isValid = true,
                position = hit.point + Vector3.up * verticalOffset,
                rotation = settings.alignToSurface ? 
                    Quaternion.FromToRotation(Vector3.up, hit.normal) * 
                    Quaternion.Euler(0, Random.Range(settings.rotationRange.x, settings.rotationRange.y), 0) :
                    Quaternion.Euler(0, Random.Range(settings.rotationRange.x, settings.rotationRange.y), 0),
                scale = 0.1f * Mathf.Round(Random.Range(settings.scaleRange.x, settings.scaleRange.y) / 0.1f) * Vector3.one,
                prefab = settings.prefabs.Length > 0 ? 
                    settings.prefabs[Random.Range(0, settings.prefabs.Length)] : null
            };

            if (settings.minDistanceBetweenObjects > 0 && existingPoints != null)
            {
                foreach (var existing in existingPoints)
                {
                    if (existing.isValid && 
                        Vector3.Distance(result.position, existing.position) < settings.minDistanceBetweenObjects)
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
        
        public static bool IsPointInSpawnArea(Vector3 worldPoint, SpawnSettings settings, Transform spawner)
        {
            Vector3 localPoint = spawner.InverseTransformPoint(worldPoint);
            Vector3 halfDims = settings.dimensions * 0.5f;
            
            return Mathf.Abs(localPoint.x) <= halfDims.x && 
                Mathf.Abs(localPoint.z) <= halfDims.z &&
                localPoint.y >= -halfDims.y * 2 &&
                localPoint.y <= halfDims.y * 2;
        }

        /// <summary>
        /// ПРОВЕРКА СТАБИЛЬНОСТИ С УЧЕТОМ УГЛА ПОВЕРХНОСТИ
        /// Сравнивает высоту относительно нормали поверхности, а не абсолютную высоту
        /// </summary>
        private static bool IsSurfaceStableByHeight(Vector3 centerPosition, Vector3 surfaceNormal, SpawnSettings settings, WorldGeneration debugSource)
        {
            if (settings.edgeCheckRadius <= 0) return true;

            int successCount = 0;
            int totalPoints = settings.stabilityCheckRays;
            
            // Получаем высоту центральной точки относительно мировых координат
            float centerHeight = centerPosition.y;
            
            // Вычисляем угол наклона поверхности
            float surfaceAngle = Vector3.Angle(surfaceNormal, Vector3.up);
            
            // Адаптируем максимальную разницу высот в зависимости от угла поверхности
            float adaptiveHeightDifference = CalculateAdaptiveHeightDifference(settings.maxHeightDifference, surfaceAngle);

            // Создаем базовые направления относительно нормали поверхности
            Vector3 tangent = Vector3.Cross(surfaceNormal, Vector3.up).normalized;
            if (tangent.magnitude < 0.1f) // Если нормаль почти вертикальна, используем стандартные направления
                tangent = Vector3.Cross(surfaceNormal, Vector3.forward).normalized;
            
            Vector3 bitangent = Vector3.Cross(surfaceNormal, tangent).normalized;

            // Проверяем точки по кругу вокруг центральной позиции относительно нормали поверхности
            for (int i = 0; i < totalPoints; i++)
            {
                float angle = i * (360f / totalPoints);
                float radians = angle * Mathf.Deg2Rad;
                
                // Вычисляем направление в касательной плоскости к нормали
                Vector3 direction = (tangent * Mathf.Cos(radians) + bitangent * Mathf.Sin(radians)).normalized;
                Vector3 checkPoint = centerPosition + direction * settings.edgeCheckRadius;
                
                // Вычисляем направление луча относительно нормали поверхности
                Vector3 rayDirection = -surfaceNormal;
                
                // Поднимаем точку проверки в направлении, противоположном нормали
                Vector3 rayStart = checkPoint + surfaceNormal * 2f;
                
                // Пускаем луч в направлении нормали поверхности (вниз по склону)
                if (Physics.Raycast(rayStart, rayDirection, out RaycastHit hit, 4f, settings.collisionMask))
                {
                    // Вместо абсолютной разницы высот, вычисляем разницу относительно нормали поверхности
                    float heightDifference = CalculateHeightDifferenceRelativeToSurface(centerPosition, hit.point, surfaceNormal);
                    
                    // Проверяем адаптивную разницу высот
                    if (Mathf.Abs(heightDifference) <= adaptiveHeightDifference)
                    {
                        successCount++;
                        debugSource?.AddDebugRay(rayStart, rayDirection, hit.distance, Color.green, DebugRayType.Stability);
                    }
                    else
                    {
                        // Слишком большая разница высот относительно поверхности
                        debugSource?.AddDebugRay(rayStart, rayDirection, hit.distance, Color.red, DebugRayType.Stability);
                    }
                }
                else
                {
                    // Луч не попал - вероятно обрыв
                    debugSource?.AddDebugRay(rayStart, rayDirection, 4f, Color.magenta, DebugRayType.Stability);
                }
            }

            // Вычисляем процент успешных проверок
            float successPercentage = (float)successCount / totalPoints * 100f;
            bool isStable = successPercentage >= settings.minSuccessPercentage;

            // Добавляем отладочную информацию о проверке стабильности
            if (debugSource != null)
            {
                debugSource.AddDebugRay(
                    centerPosition, 
                    surfaceNormal, 
                    1f, 
                    isStable ? Color.cyan : Color.yellow, 
                    DebugRayType.Stability
                );
            }

            return isStable;
        }

        /// <summary>
        /// Вычисляет адаптивную максимальную разницу высот в зависимости от угла поверхности
        /// </summary>
        private static float CalculateAdaptiveHeightDifference(float baseHeightDifference, float surfaceAngle)
        {
            // Для пологих поверхностей (0-15 градусов) используем базовую разницу
            if (surfaceAngle <= 15f)
                return baseHeightDifference;
            
            // Для средних уклонов (15-45 градусов) увеличиваем допустимую разницу
            if (surfaceAngle <= 45f)
                return baseHeightDifference * (1f + (surfaceAngle - 15f) / 60f); // Увеличиваем до 1.5x
            
            // Для крутых склонов (45+ градусов) значительно увеличиваем допустимую разницу
            return baseHeightDifference * 2f;
        }

        /// <summary>
        /// Вычисляет разницу высот относительно нормали поверхности
        /// </summary>
        private static float CalculateHeightDifferenceRelativeToSurface(Vector3 centerPoint, Vector3 checkPoint, Vector3 surfaceNormal)
        {
            // Вектор от центральной точки к проверочной
            Vector3 direction = checkPoint - centerPoint;
            
            // Проецируем этот вектор на нормаль поверхности
            float projection = Vector3.Dot(direction, surfaceNormal);
            
            // Возвращаем длину проекции (разница высот относительно поверхности)
            return projection;
        }

        /// <summary>
        /// Проверяет наличие дыр/провалов в полу под точкой размещения
        /// </summary>
        private static bool HasFloorHole(Vector3 position, SpawnSettings settings, WorldGeneration debugSource)
        {
            Vector3 rayStart = position + Vector3.up * 0.1f;
            float checkDistance = settings.floorCheckDistance + 0.1f;
            
            // Пускаем луч вниз для проверки пола
            if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, checkDistance, settings.collisionMask))
            {
                // Нет пола - дыра
                debugSource?.AddDebugRay(rayStart, Vector3.down, checkDistance, Color.red, DebugRayType.Floor);
                return true;
            }

            debugSource?.AddDebugRay(rayStart, Vector3.down, hit.distance, Color.green, DebugRayType.Floor);
            return false;
        }

        private static bool HasNearbyObstacles(Vector3 position, SpawnSettings settings)
        {
            if (settings.avoidanceRadius <= 0) return false;
            return Physics.CheckSphere(position, settings.avoidanceRadius, settings.avoidMask);
        }

        public static bool IsValidClusterCenter(Vector3 position, SpawnSettings settings, Transform spawner)
        {
            // Пытаемся найти поверхность
            Vector3 rayStart = position + Vector3.up * settings.dimensions.y;
            if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, settings.dimensions.y * 2, settings.collisionMask))
                return false;
            
            // Используем фактическую позицию на поверхности
            Vector3 surfacePosition = hit.point;
            
            // Проверяем, что точка на поверхности в зоне спавна
            if (!IsPointInSpawnArea(surfacePosition, settings, spawner)) 
                return false;

            // Проверяем слой поверхности
            return (settings.avoidMask.value & (1 << hit.collider.gameObject.layer)) == 0;
        }
    }
}