using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

namespace NeonImperium.WorldGenerations
{
    [System.Serializable]
    public class SpawnSettings
    {
        /// <summary>Фиксированный seed для детерминированной генерации.</summary>
        public const int DeterministicSeed = 12345;

        // ──────────── Верхний уровень ────────────

        [Header("Визуал / Алгоритм")]
        [Tooltip("Цвет отображения зоны спавна в редакторе. Не влияет на игровой процесс.")]
        [ColorUsage(false)] public Color gizmoColor = new(0.65f, 0f, 1f);

        [Tooltip("Тип алгоритма генерации объектов.")]
        public GenerationAlgorithmType generationAlgorithm = GenerationAlgorithmType.Standard;

        [Tooltip("Приоритет генерации. Спавнеры с более высоким приоритетом генерируются первыми при полной перегенерации.")]
        [Range(0, 100)] public int priority = 50;

        [Tooltip("Список префабов для случайного выбора при генерации. Если пустой - генерация не работает.")]
        public GameObject[] prefabs;

        [Tooltip("Целевое количество объектов для создания. Реальное количество может быть меньше из-за ограничений размещения.")]
        [Range(1, 2048)] public int population = 100;

        [Tooltip("Если включено — seed фиксирован, генерация воспроизводима между запусками. Нужно для сетевой синхронизации, сохранений, тестов.")]
        public bool useDeterministicSeed = false;

        [Header("Предпросмотр")]
        [Tooltip("Количество попыток валидации в preview-режиме (без создания объектов).")]
        [Range(10, 1000)] public int previewAttempts = 200;

        [Tooltip("Цвет валидной точки предпросмотра.")]
        public Color previewValidColor = new(0.2f, 1f, 0.3f, 0.6f);

        [Tooltip("Цвет невалидной точки предпросмотра.")]
        public Color previewInvalidColor = new(1f, 0.25f, 0.25f, 0.35f);

        [Tooltip("Показывать также невалидные точки в preview-режиме.")]
        public bool previewShowInvalid = false;

        [Header("Зоны спавна")]
        [Tooltip("Форма зон размещения:\n• Box — основная зона + N дополнительных\n• Polygon — произвольные фигуры")]
        public SpawnZoneMode zoneMode = SpawnZoneMode.Box;

        [Tooltip("Размеры основной (legacy) зоны. Центр всегда в локальном (0,0,0).\nX,Z — площадь, Y — вертикальный диапазон проверки.")]
        public Vector3 dimensions = new(100, 10, 100);

        [Tooltip("Дополнительные box-зоны с собственным центром и размером. Спавн идёт во всех включённых.")]
        public List<BoxSpawnZoneSettings> extraBoxes = new();

        [Tooltip("Произвольные полигональные зоны.")]
        public List<PolygonSpawnZoneSettings> polygons = new();

        [Header("Вариации объектов")]
        [Tooltip("Диапазон случайного масштаба объектов. (0.8, 1.2) = от 80% до 120% от оригинального размера.")]
        [MinMaxRange(0.1f, 10f, "F1")] public Vector2 scaleRange = new(0.8f, 1.2f);

        [Tooltip("Диапазон случайного вращения вокруг оси Y в градусах. (0, 360) = полный случайный поворот.")]
        [MinMaxRange(0f, 360f, "F0")] public Vector2 rotationRange = new(0f, 360f);

        [Tooltip("Случайное смещение по вертикали от точки попадания. Полезно для корректировки высоты посадки.")]
        public Vector2 verticalOffset = Vector2.zero;

        // ──────────── Правила размещения ────────────

        [Header("Правила размещения")]
        [Tooltip("Слои, по которым осуществляется поиск поверхности для размещения. Только эти слои будут считаться валидной поверхностью.")]
        public LayerMask collisionMask = -1;

        [Tooltip("Слои, которых нужно избегать при размещении.")]
        public LayerMask avoidMask;

        [Tooltip("Автоматически выравнивать объекты по нормали поверхности.")]
        public bool alignToSurface = true;

        [Tooltip("Радиус проверки препятствий вокруг точки размещения. 0 = отключить.")]
        [Min(0f)] public float avoidanceRadius = 3.5f;

        [Tooltip("Максимальное количество попыток размещения одного объекта перед отказом.")]
        [Range(1, 100)] public int maxPlacementAttempts = 10;

        [Tooltip("Допустимый диапазон углов наклона поверхности (градусы).\n" +
                 "X — минимум (0 = допустима любая поверхность, включая горизонталь).\n" +
                 "Y — максимум (0 = проверка угла отключена).\n\n" +
                 "Примеры:\n" +
                 "• (0, 45)   — только пологие поверхности (как раньше).\n" +
                 "• (60, 90)  — только стены и сильные уклоны.\n" +
                 "• (150, 180) — только потолки.\n" +
                 "• (0, 180)  — любая поверхность.")]
        [MinMaxRange(0f, 180f, "F0")]
        public Vector2 surfaceAngleRange = new(0f, 45f);

        [Tooltip("Радиус проверки стабильности поверхности. 0 = отключить.")]
        [Min(0f)] public float edgeCheckRadius = 0f;

        [Tooltip("Максимальная допустимая разница высот (м). 0 = любая разница = отказ.")]
        [Min(0f)] public float maxHeightDifference = 0.1f;

        [Tooltip("Количество лучей проверки стабильности. 4-8 быстро, 12-16 точно.")]
        [Range(1, 16)] public int stabilityCheckRays = 8;

        [Tooltip("Минимальный процент успешных проверок для валидности точки.")]
        [Range(0f, 100f)] public float minSuccessPercentage = 100f;

        [Tooltip("Дистанция проверки пола под точкой размещения. 0 = отключить.")]
        [Min(0f)] public float floorCheckDistance = 0f;

        [Tooltip("Режим проверки потолка:\n• None — не проверять\n• RequireOpenSky — требовать открытое небо\n• RequireRoof — требовать крышу (пещеры, дома)")]
        public CeilingCheckMode ceilingCheckMode = CeilingCheckMode.None;

        [Tooltip("Слои, считающиеся потолком. Если пусто — используется avoidMask.")]
        public LayerMask ceilingMask;

        [Tooltip("Длина луча проверки потолка в метрах.")]
        [Min(0.1f)] public float ceilingCheckDistance = 50f;

        [Tooltip("Радиус сферической проверки потолка. 0 = точечный луч.")]
        [Min(0f)] public float ceilingCheckRadius = 0f;

        [Tooltip("Минимальное расстояние между объектами.")]
        [Min(0f)] public float minDistanceBetweenObjects = 5f;

        // ──────────── Луч ────────────

        [Header("Настройки луча")]
        [Tooltip("Тип луча для обнаружения поверхности:\n• Ray — точечный\n• Sphere — сферический")]
        public RayCastType rayCastType = RayCastType.Ray;

        [Tooltip("Радиус сферы при rayCastType = Sphere.")]
        [Min(0f)] public float raySphereRadius = 1f;

        [Tooltip("Максимальный случайный угол отклонения луча от вертикали. (0,0) = строго вниз.")]
        [MinMaxRange(0f, 90f, "F0")] public Vector2 maxRayAngle = Vector2.zero;

        [Tooltip("Точка испускания лучей:\n• TopFace\n• SideFaces\n• InsideVolume")]
        public RayOriginType rayOriginType = RayOriginType.TopFace;

        // ──────────── Job-валидация ────────────

        [Header("Job-валидация (>2000 объектов)")]
        [Tooltip("Батч-рейкасты через RaycastCommand.ScheduleBatch для сцен с большим количеством объектов.\n" +
                 "Требует rayCastType = Ray. При недоступности Unity.Collections или Sphere-кастов — авто-fallback на CPU.")]
        public bool useJobValidation = false;

        [Tooltip("Количество команд на один worker-поток (minCommandsPerJob для ScheduleBatch).\n" +
                 "64 — безопасный дефолт, для тяжёлых сцен можно 128-256.")]
        [Min(1)] public int jobBatchSize = 64;

        // ──────────── Кластеризация ────────────

        [Header("Кластеризация")]
        [Tooltip("Группировать объекты в кластеры.")]
        public bool useClustering = false;

        [Tooltip("Количество кластеров.")]
        [Range(1, 100)] public int clusterCount = 10;

        [Tooltip("Диапазон радиусов кластеров.")]
        [MinMaxRange(1f, 50f, "F1")] public Vector2 clusterRadiusRange = new(5f, 15f);

        [Tooltip("Диапазон количества объектов в кластере.")]
        [MinMaxRangeInt(1, 20)] public Vector2Int objectsPerClusterRange = new(3, 8);

        [Tooltip("Минимальное расстояние между центрами кластеров.")]
        [Min(0f)] public float minDistanceBetweenClusters = 20f;

        // ──────────── Runtime ────────────

        [Header("Runtime")]
        [Tooltip("Разрешить работу генератора в собранной игре.")]
        public bool enableRuntimeGeneration = false;

        [Tooltip("Автоматически запускать генерацию при старте (только если enableRuntimeGeneration).")]
        public bool autoGenerateOnStart = false;

        [Tooltip("Удалить компонент после завершения генерации.")]
        public bool destroyAfterGeneration = false;

        [Tooltip("Максимальное общее количество попыток размещения в Runtime.")]
        [Min(1)] public int runtimeMaxAttempts = 10000;

        [Tooltip("Количество перезапусков при недостижении цели (только Runtime).")]
        [Range(0, 10)] public int runtimeRetryCount = 3;

        [Tooltip("Переиспользовать существующие дочерние объекты при повторной генерации.\n" +
                 "• Меньше объектов → лишние деактивируются (SetActive false), не удаляются.\n" +
                 "• Больше объектов → недостающие инстанцируются.\n" +
                 "Работает и в редакторе, и в Runtime.")]
        public bool reuseExistingObjects = false;

        // ──────────── NavMesh ────────────

        [Header("NavMesh")]
        [Tooltip("Проверять, что точка находится на NavMesh.")]
        public bool useNavMeshCheck = false;

        [Tooltip("Радиус поиска ближайшей точки на NavMesh. 0 = 1.")]
        public float navMeshSampleRadius = 1f;

        [Tooltip("Корректировать позицию на ближайшую точку NavMesh.")]
        public bool snapToNavMesh = false;

        [Tooltip("Режим авто-перегенерации NavMesh Surface (только редактор).\nНастройка синхронизируется со всеми спавнерами сцены.")]
        public NavMeshRegenerationMode navMeshRegenerationMode = NavMeshRegenerationMode.OnFullRegeneration;

        [Tooltip("Этот спавнер запрашивает перегенерацию NavMesh после своей генерации.\nОтключите для мобов и декора, не меняющих проходимость.")]
        public bool requestNavMeshRegeneration = false;

        [Tooltip("NavMeshData asset для runtime-перегенерации.\n" +
             "Создаётся через AI Navigation → NavMeshSurface → Bake, сохраняется как .asset.\n" +
             "Обязателен, если NavMeshSurface отсутствует на сцене.")]
        public NavMeshData runtimeNavMeshData;

        [Tooltip("Agent Type ID для runtime bake.\n0 = Humanoid (по умолчанию).\n" +
                 "Список ID: Window → AI → Navigation → Agents.")]
        [Min(0)] public int runtimeAgentTypeID = 0;

        [Tooltip("Границы сбора геометрии для runtime bake (world-space).\n" +
                 "Если size == 0 — автоматически берётся dimensions * 2 относительно спавнера.")]
        public Bounds runtimeBounds = new Bounds(Vector3.zero, new Vector3(200f, 20f, 200f));

        // ──────────── Расширения / Отладка ────────────

        [Header("Расширения")]
        [Tooltip("Расширения, вызываемые при создании каждого объекта.")]
        public List<WorldGenerationExtension> onSpawnExtensions = new();

        [Tooltip("Расширения, вызываемые после завершения генерации всех объектов.")]
        public List<WorldGenerationExtension> onGenerationCompleteExtensions = new();

        [Header("Отладка лучей")]
        [Tooltip("Настройки отображения лучей отладки.")]
        public DebugRaySettings debugRaySettings = new();

        // ──────────── Методы ────────────

        /// <summary>Возвращает 0 для уникального seed, DeterministicSeed при детерминированном режиме, иначе rawSeed.</summary>
        public static int ResolveSeed(int rawSeed, bool deterministic)
        {
            if (rawSeed != 0) return rawSeed;
            return deterministic ? DeterministicSeed : 0;
        }

        /// <summary>Кламп всех полей. Вызывается из WorldGeneration.OnValidate.</summary>
        public void Clamp()
        {
            int maxPop = useJobValidation ? 2048 : 1024;
            population = Mathf.Clamp(population, 1, maxPop);
            priority = Mathf.Clamp(priority, 0, 100);
            previewAttempts = Mathf.Clamp(previewAttempts, 10, 1000);

            rotationRange.x = Mathf.Clamp(rotationRange.x, 0f, 360f);
            rotationRange.y = Mathf.Clamp(rotationRange.y, 0f, 360f);
            if (rotationRange.x > rotationRange.y) rotationRange.x = rotationRange.y;

            scaleRange.x = Mathf.Max(0.1f, scaleRange.x);
            scaleRange.y = Mathf.Max(0.1f, scaleRange.y);
            if (scaleRange.x > scaleRange.y) scaleRange.x = scaleRange.y;

            avoidanceRadius = Mathf.Max(0f, avoidanceRadius);
            maxPlacementAttempts = Mathf.Max(1, maxPlacementAttempts);

            surfaceAngleRange.x = Mathf.Clamp(surfaceAngleRange.x, 0f, 180f);
            surfaceAngleRange.y = Mathf.Clamp(surfaceAngleRange.y, 0f, 180f);
            if (surfaceAngleRange.x > surfaceAngleRange.y)
                surfaceAngleRange.x = surfaceAngleRange.y;

            edgeCheckRadius = Mathf.Max(0f, edgeCheckRadius);
            maxHeightDifference = Mathf.Max(0f, maxHeightDifference);
            stabilityCheckRays = Mathf.Clamp(stabilityCheckRays, 1, 16);
            minSuccessPercentage = Mathf.Clamp(minSuccessPercentage, 0f, 100f);
            floorCheckDistance = Mathf.Max(0f, floorCheckDistance);
            ceilingCheckDistance = Mathf.Max(0.1f, ceilingCheckDistance);
            ceilingCheckRadius = Mathf.Max(0f, ceilingCheckRadius);
            minDistanceBetweenObjects = Mathf.Max(0f, minDistanceBetweenObjects);

            raySphereRadius = Mathf.Max(0f, raySphereRadius);
            maxRayAngle.x = Mathf.Clamp(maxRayAngle.x, 0f, 90f);
            maxRayAngle.y = Mathf.Clamp(maxRayAngle.y, 0f, 90f);
            if (maxRayAngle.x > maxRayAngle.y) maxRayAngle.x = maxRayAngle.y;

            jobBatchSize = Mathf.Max(1, jobBatchSize);

            clusterCount = Mathf.Clamp(clusterCount, 1, 100);
            clusterRadiusRange.x = Mathf.Max(1f, clusterRadiusRange.x);
            clusterRadiusRange.y = Mathf.Max(1f, clusterRadiusRange.y);
            if (clusterRadiusRange.x > clusterRadiusRange.y) clusterRadiusRange.x = clusterRadiusRange.y;

            objectsPerClusterRange.x = Mathf.Clamp(objectsPerClusterRange.x, 1, 20);
            objectsPerClusterRange.y = Mathf.Clamp(objectsPerClusterRange.y, 1, 20);
            if (objectsPerClusterRange.x > objectsPerClusterRange.y) objectsPerClusterRange.x = objectsPerClusterRange.y;

            runtimeMaxAttempts = Mathf.Max(1, runtimeMaxAttempts);
            runtimeRetryCount = Mathf.Clamp(runtimeRetryCount, 0, 10);

            navMeshSampleRadius = Mathf.Max(0f, navMeshSampleRadius);
            if (runtimeAgentTypeID < 0) runtimeAgentTypeID = 0;

            if (zoneMode == SpawnZoneMode.Box)
            {
                dimensions.x = Mathf.Max(0.1f, dimensions.x);
                dimensions.y = Mathf.Max(0.1f, dimensions.y);
                dimensions.z = Mathf.Max(0.1f, dimensions.z);
            }
        }
    }

    [System.Serializable]
    public class DebugRaySettings
    {
        [Tooltip("Включить отображение лучей отладки на сцене.")]
        public bool enabled = false;

        [Tooltip("Показывать основные лучи проверки поверхности.")]
        public bool showMainRays = true;

        [Tooltip("Показывать лучи проверки стабильности поверхности (edge check).")]
        public bool showStabilityRays = true;

        [Tooltip("Показывать лучи проверки пола (floor check).")]
        public bool showFloorRays = true;

        [Tooltip("Показывать лучи проверки препятствий (avoidance).")]
        public bool showAvoidanceRays = true;

        [Tooltip("Показывать лучи проверки потолка (ceiling check).")]
        public bool showCeilingRays = true;
    }
}