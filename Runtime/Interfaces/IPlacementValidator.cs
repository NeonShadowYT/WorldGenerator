using UnityEngine;
using System;
using System.Collections.Generic;

namespace NeonImperium.WorldGenerations
{
    /// <summary>
    /// Валидатор точек размещения. Single и batch режимы.
    /// Владеет внутренними persistent-буферами (NativeArray в job-режиме), поэтому IDisposable.
    /// </summary>
    public interface IPlacementValidator : IDisposable
    {
        /// <summary>Валидация одной точки. placementHash может быть null (fallback на existingPoints).</summary>
        PlacementData Validate(PlacementPoint point, Transform spawner,
            List<PlacementData> existingPoints, IDebugLogger debugLogger, IPhysicsService physicsService,
            SpatialHash placementHash);

        /// <summary>
        /// Батч-валидация. output.Length ≥ count.
        /// Внутренний выбор: job-путь (RaycastCommand) или CPU-fallback.
        /// </summary>
        void ValidateBatch(List<PlacementPoint> points, int startIndex, int count,
            Transform spawner, IDebugLogger debugLogger, IPhysicsService physicsService,
            SpatialHash placementHash, PlacementData[] output);
    }
}