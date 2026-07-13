using UnityEngine;
using System.Collections.Generic;

namespace NeonImperium.WorldGenerations
{
    [System.Serializable]
    public class SpawnSettings
    {
        [Tooltip("Цвет отображения зоны спавна в редакторе. Не влияет на игровой процесс.")]
        [ColorUsage(false)] public Color gizmoColor = new(0.65f, 0f, 1f);
        
        [Tooltip("Тип алгоритма генерации объектов.")]
        public GenerationAlgorithmType generationAlgorithm = GenerationAlgorithmType.Standard;
        
        [Tooltip("Приоритет генерации. Спавнеры с более высоким приоритетом генерируются первыми при полной перегенерации.")]
        [Range(0, 100)] public int priority = 50;
        
        [Tooltip("Список префабов для случайного выбора при генерации. Если пустой - генерация не работает.")]
        public GameObject[] prefabs;
        
        [Tooltip("Целевое количество объектов для создания. Реальное количество может быть меньше из-за ограничений размещения.")]
        [Range(1, 1000)] public int population = 100;
        
        [Tooltip("Размеры зоны спавна в локальных координатах спавнера. X,Z - горизонтальная площадь, Y - высота проверки.")]
        public Vector3 dimensions = new(100, 10, 100);
        
        [Tooltip("Диапазон случайного масштаба объектов. (0.8, 1.2) = от 80% до 120% от оригинального размера.")]
        [MinMaxRange(0.1f, 10f, "F1")] public Vector2 scaleRange = new(0.8f, 1.2f);
        
        [Tooltip("Диапазон случайного вращения вокруг оси Y в градусах. (0, 360) = полный случайный поворот.")]
        [MinMaxRange(0f, 360f, "F0")] public Vector2 rotationRange = new(0f, 360f);
        
        [Tooltip("Случайное смещение по вертикали от точки попадания. Полезно для корректировки высоты посадки.")]
        public Vector2 verticalOffset = Vector2.zero;
        
        [Tooltip("Слои, по которым осуществляется поиск поверхности для размещения. Только эти слои будут считаться валидной поверхностью.")]
        public LayerMask collisionMask = -1;
        
        [Tooltip("Слои, которых нужно избегать при размещении. Объекты не будут спавниться на этих слоях или рядом с ними.")]
        public LayerMask avoidMask;
        
        [Tooltip("Автоматически выравнивать объекты по нормали поверхности. Отключите для вертикальных объектов.")]
        public bool alignToSurface = true;
        
        [Tooltip("Радиус проверки препятствий вокруг точки размещения. 0 = отключить проверку.")]
        [Min(0f)] public float avoidanceRadius = 3.5f;
        
        [Tooltip("Максимальное количество попыток размещения одного объекта перед отказом. Больше = лучшее заполнение, но медленнее.")]
        [Range(1, 100)] public int maxPlacementAttempts = 10;

        [Tooltip("Расширения, вызываемые при создании каждого объекта")]
        public List<WorldGenerationExtension> onSpawnExtensions = new();
        
        [Tooltip("Расширения, вызываемые после завершения генерации всех объектов")]
        public List<WorldGenerationExtension> onGenerationCompleteExtensions = new();

        [Tooltip("Тип луча для обнаружения поверхности:\n- Ray: обычный луч\n- Sphere: сферический луч (толще)")]
        public RayCastType rayCastType = RayCastType.Ray;
        
        [Tooltip("Радиус сферы при использовании SphereCast (только если rayCastType = Sphere).")]
        [Min(0f)] public float raySphereRadius = 1f;
        
        [Tooltip("Максимальный случайный угол отклонения луча от вертикали в градусах. (0,0) = строго вертикально вниз.")]
        [MinMaxRange(0f, 90f, "F0")] public Vector2 maxRayAngle = Vector2.zero;
        
        [Tooltip("Точка испускания лучей:\n- TopFace: с верхней грани\n- SideFaces: со случайной боковой грани\n- InsideVolume: из случайной точки внутри объема")]
        public RayOriginType rayOriginType = RayOriginType.TopFace;

        [Header("Полы")]
        [Tooltip("Дистанция проверки пола под точкой размещения. Обнаруживает ямы и провалы. 0 = отключить проверку.")]
        [Min(0f)] public float floorCheckDistance = 0f;

        [Header("Потолки")]
        [Tooltip("Проверять наличие препятствий над точкой размещения. Полезно для предотвращения спавна под мостами или крышами.")]
        public bool checkCeiling = false;

        [Tooltip("Радиус проверки стабильности поверхности вокруг точки. 0 = отключить проверку.\nПроверяет, что объект не окажется на краю обрыва или неровной поверхности.")]
        [Min(0f)] public float edgeCheckRadius = 0f;
        
        [Tooltip("Максимальная допустимая разница высот между центральной точкой и точками проверки.\n• 0.1 = 10 см\n• 0.5 = 50 см\n• 1.0 = 1 метр\n• 0.0 = любая разница = отказ")]
        [Min(0f)] public float maxHeightDifference = 0.1f;
        
        [Tooltip("Количество лучей для проверки стабильности поверхности. Больше = точнее, но медленнее.\n4-8 для быстрой проверки, 12-16 для точной.")]
        [Range(1, 16)] public int stabilityCheckRays = 8;
        
        [Tooltip("Минимальный процент успешных проверок для валидности точки.\n• 100% = все лучи должны быть успешны\n• 75% = 3/4 лучей должны быть успешны\n• 50% = половина лучей должна быть успешна")]
        [Range(0f, 100f)] public float minSuccessPercentage = 100f;

        [Header("Расстояние")]
        [Tooltip("Минимальное расстояние между объектами. Предотвращает наслоение объектов.")]
        [Min(0f)] public float minDistanceBetweenObjects = 5f;
        
        [Tooltip("Включить группировку объектов в кластеры. Полезно для создания природных скоплений.")]
        public bool useClustering = false;
        
        [Tooltip("Количество кластеров для создания. Меньше = более разреженное размещение.")]
        [Range(1, 100)] public int clusterCount = 10;
        
        [Tooltip("Диапазон радиусов кластеров. Объекты внутри кластера будут в пределах этого расстояния от центра.")]
        [MinMaxRange(1f, 50f, "F1")] public Vector2 clusterRadiusRange = new(5f, 15f);
        
        [Tooltip("Диапазон количества объектов в каждом кластере. Случайное значение для каждого кластера.")]
        [MinMaxRangeInt(1, 20)] public Vector2Int objectsPerClusterRange = new(3, 8);
        
        [Tooltip("Минимальное расстояние между центрами кластеров. Предотвращает пересечение групп.")]
        [Min(0f)] public float minDistanceBetweenClusters = 20f;

        [Header("Отладка лучей")]
        [Tooltip("Настройки отображения лучей отладки для визуализации процесса генерации.")]
        public DebugRaySettings debugRaySettings = new();

        [Header("Runtime")]
        [Tooltip("Разрешить работу генератора в собранной игре (Runtime). Если отключено, компонент будет уничтожен при старте.")]
        public bool enableRuntimeGeneration = false;

        [Tooltip("Автоматически запускать генерацию при старте (только если enableRuntimeGeneration = true).")]
        public bool autoGenerateOnStart = false;

        [Tooltip("Удалить компонент после завершения генерации (экономит память).")]
        public bool destroyAfterGeneration = false;

        [Tooltip("Максимальное общее количество попыток размещения в Runtime (предотвращает бесконечные циклы).")]
        [Min(1)] public int runtimeMaxAttempts = 10000;

        [Tooltip("Количество перезапусков генерации при недостижении целевого количества объектов (только Runtime).")]
        [Range(0, 10)] public int runtimeRetryCount = 3;

        [Header("NavMesh")]
        [Tooltip("Проверять, что точка размещения находится на NavMesh (требуется система NavMesh).")]
        public bool useNavMeshCheck = false;

        [Tooltip("Радиус поиска ближайшей точки на NavMesh. Если 0, используется стандартное значение.")]
        public float navMeshSampleRadius = 1f;

        [Tooltip("Корректировать позицию на ближайшую точку NavMesh (если точка не на NavMesh, но рядом).")]
        public bool snapToNavMesh = false;

        [Tooltip("Максимально допустимый угол наклона поверхности (в градусах) для размещения. 0 = отключить проверку.")]
        [Range(0f, 90f)] public float maxSurfaceAngle = 45f;
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