using UnityEngine;
using System.Collections.Generic;

namespace NeonImperium.WorldGeneration
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

            bool hasHit;
            RaycastHit hit;
            Color rayColor;

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
            
            PlacementData result = new()
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
                foreach (PlacementData existing in existingPoints)
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
                localPoint.y >= -halfDims.y &&
                localPoint.y <= halfDims.y;
        }

        private static bool IsSurfaceStableByHeight(Vector3 centerPosition, Vector3 surfaceNormal, SpawnSettings settings, WorldGeneration debugSource)
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
                
                if (Physics.Raycast(rayStart, rayDirection, out RaycastHit hit, 4f, settings.collisionMask))
                {
                    float heightDifference = CalculateHeightDifferenceRelativeToSurface(centerPosition, hit.point, surfaceNormal);
                    
                    if (Mathf.Abs(heightDifference) <= adaptiveHeightDifference)
                    {
                        successCount++;
                        debugSource?.AddDebugRay(rayStart, rayDirection, hit.distance, Color.green, DebugRayType.Stability);
                    }
                    else
                    {
                        debugSource?.AddDebugRay(rayStart, rayDirection, hit.distance, Color.red, DebugRayType.Stability);
                    }
                }
                else
                {
                    debugSource?.AddDebugRay(rayStart, rayDirection, 4f, Color.magenta, DebugRayType.Stability);
                }
            }

            float successPercentage = (float)successCount / totalPoints * 100f;
            bool isStable = successPercentage >= settings.minSuccessPercentage;

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

        private static float CalculateAdaptiveHeightDifference(float baseHeightDifference, float surfaceAngle)
        {
            if (surfaceAngle <= 15f)
                return baseHeightDifference;
            
            if (surfaceAngle <= 45f)
                return baseHeightDifference * (1f + (surfaceAngle - 15f) / 60f);
            
            return baseHeightDifference * 2f;
        }

        private static float CalculateHeightDifferenceRelativeToSurface(Vector3 centerPoint, Vector3 checkPoint, Vector3 surfaceNormal)
        {
            Vector3 direction = checkPoint - centerPoint;
            float projection = Vector3.Dot(direction, surfaceNormal);
            return projection;
        }

        private static bool HasFloorHole(Vector3 position, SpawnSettings settings, WorldGeneration debugSource)
        {
            Vector3 rayStart = position + Vector3.up * 0.1f;
            float checkDistance = settings.floorCheckDistance + 0.1f;
            
            if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, checkDistance, settings.collisionMask))
            {
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
            Vector3 rayStart = position + Vector3.up * settings.dimensions.y;
            if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, settings.dimensions.y * 2, settings.collisionMask))
                return false;
            
            Vector3 surfacePosition = hit.point;
            
            if (!IsPointInSpawnArea(surfacePosition, settings, spawner)) 
                return false;

            return (settings.avoidMask.value & (1 << hit.collider.gameObject.layer)) == 0;
        }
    }
}