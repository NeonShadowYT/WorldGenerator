#if UNITY_EDITOR
using System;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NeonImperium.WorldGenerations
{
    public class SectionDrawer
    {
        private readonly EditorStyleManager _styleManager;

        private static readonly Vector3[] _convexityBuffer = new Vector3[64];
        private static readonly StringBuilder _concaveSb = new(160);
        private static string _concaveCachedText;
        private static int _concaveCachedCount = -1;

        private static readonly float[] SnapSteps = { 0.1f, 0.5f, 1f, 2f, 5f, 0f };
        private static readonly string[] SnapLabels = { "0.1", "0.5", "1", "2", "5", "Off" };
        private static readonly GUIContent SnapLabelContent = new("Snap (Ctrl):",
            "Шаг снапа при зажатом Ctrl во время drag в Scene View.");
        private static readonly GUIContent ZoneModeContent = new("Форма зон",
            "Box — основная зона + дополнительные боксы.\nPolygon — произвольные объёмные фигуры (3D).");
        private static readonly GUIContent PolygonsContent = new("Полигональные зоны",
            "Вершины в локальных координатах. position — центр ребра, verticalSize — размер по вертикали.");

        public SectionDrawer(EditorStyleManager styleManager) { _styleManager = styleManager; }

        // ───────────────────── Основные настройки ─────────────────────

        public void DrawSpawnSettings(SerializedProperty settings, SerializedProperty seedProperty,
            bool showHelpBoxes, UnityEngine.Object[] targets, WorldGeneration spawner)
        {
            Color textColor = settings.FindPropertyRelative("gizmoColor").colorValue;
            EditorFoldoutState.SpawnSettings = DrawSection("⚙️", "Основные настройки",
                EditorFoldoutState.SpawnSettings, textColor, spawner, () =>
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("🎨 Визуальные настройки", EditorStyles.boldLabel);
                DrawRelative(settings, "gizmoColor",
                    "Цвет отображения зоны спавна в редакторе. Не влияет на игровой процесс.");
                DrawRelative(settings, "generationAlgorithm",
                    "Тип алгоритма генерации объектов.");
                DrawRelative(settings, "priority",
                    "Приоритет генерации. Спавнеры с более высоким приоритетом генерируются первыми при полной перегенерации.");
                DrawRelative(settings, "prefabs",
                    "Список префабов для случайного выбора при генерации. Если пустой — генерация не работает.");
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(3f);

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("📏 Количество", EditorStyles.boldLabel);
                DrawRelative(settings, "population",
                    "Целевое количество объектов для создания. Реальное количество может быть меньше из-за ограничений размещения.");

                SerializedProperty detProp = settings.FindPropertyRelative("useDeterministicSeed");
                EditorGUILayout.PropertyField(detProp, new GUIContent(detProp.displayName,
                    "Если включено — seed фиксирован, генерация воспроизводима между запусками.\n" +
                    "Нужно для сетевой синхронизации, сохранений, тестов."));

                if (detProp.boolValue && seedProperty != null)
                {
                    EditorGUILayout.PropertyField(seedProperty, new GUIContent("Seed",
                        $"Пользовательский seed.\n" +
                        $"• 0 = использовать детерминированный ({SpawnSettings.DeterministicSeed}).\n" +
                        $"• ≠ 0 = использовать введённое значение."));
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(3f);

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("👁️ Предпросмотр", EditorStyles.boldLabel);
                DrawRelative(settings, "previewAttempts",
                    "Количество попыток валидации в preview-режиме (без создания объектов).");
                DrawRelative(settings, "previewValidColor",
                    "Цвет валидной точки предпросмотра.");
                DrawRelative(settings, "previewInvalidColor",
                    "Цвет невалидной точки предпросмотра.");
                DrawRelative(settings, "previewShowInvalid",
                    "Показывать также невалидные точки в preview-режиме.");
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(3f);

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("🎲 Вариации объектов", EditorStyles.boldLabel);
                DrawRelative(settings, "scaleRange",
                    "Диапазон случайного масштаба объектов. (0.8, 1.2) = от 80% до 120% от оригинального размера.");
                DrawRelative(settings, "rotationRange",
                    "Диапазон случайного вращения вокруг оси Y в градусах. (0, 360) = полный случайный поворот.");
                DrawRelative(settings, "verticalOffset",
                    "Случайное смещение по вертикали от точки попадания. Полезно для корректировки высоты посадки.");
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(3f);

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("🎯 Правила размещения", EditorStyles.boldLabel);
                DrawRelative(settings, "collisionMask",
                    "Слои, по которым осуществляется поиск поверхности для размещения.\nТолько эти слои будут считаться валидной поверхностью.");
                DrawRelative(settings, "avoidMask",
                    "Слои, которых нужно избегать при размещении.");
                DrawRelative(settings, "alignToSurface",
                    "Автоматически выравнивать объекты по нормали поверхности.");
                DrawRelative(settings, "surfaceAngleRange",
                    "Допустимый диапазон углов наклона поверхности (градусы).\n" +
                    "X — минимум (0 = допустима любая поверхность, включая горизонталь).\n" +
                    "Y — максимум (0 = проверка угла отключена).\n\n" +
                    "Примеры:\n" +
                    "• (0, 45)    — только пологие поверхности (как раньше).\n" +
                    "• (60, 90)   — только стены и сильные уклоны.\n" +
                    "• (150, 180) — только потолки.\n" +
                    "• (0, 180)   — любая поверхность.");
                DrawRelative(settings, "maxPlacementAttempts",
                    "Максимальное количество попыток размещения одного объекта перед отказом.");
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(3f);

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("🔌 Расширения", EditorStyles.boldLabel);
                DrawRelative(settings, "onSpawnExtensions",
                    "Расширения, вызываемые при создании каждого объекта.");
                DrawRelative(settings, "onGenerationCompleteExtensions",
                    "Расширения, вызываемые после завершения генерации всех объектов.");
                EditorGUILayout.EndVertical();

                DrawMaskConflictWarning(settings, targets);
            });
        }

        private void DrawMaskConflictWarning(SerializedProperty settings, UnityEngine.Object[] targets)
        {
            int collision = settings.FindPropertyRelative("collisionMask").intValue;
            int avoid = settings.FindPropertyRelative("avoidMask").intValue;
            int conflictLayers = avoid & ~collision;
            if (conflictLayers == 0) return;

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(
                "⚠️ <b>ВНИМАНИЕ:</b> Avoid Mask содержит слои, не включённые в Collision Mask!",
                _styleManager.MiniLabelStyle);

            if (GUILayout.Button("🔧 Автоисправление: Добавить слои Avoid в Collision"))
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    WorldGeneration tgt = targets[i] as WorldGeneration;
                    if (tgt == null) continue;
                    Undo.RecordObject(tgt, "Исправить Collision Mask");
                    LayerMask mask = tgt.settings.collisionMask;
                    mask.value |= conflictLayers;
                    tgt.settings.collisionMask = mask;
                    EditorUtility.SetDirty(tgt);
                }
            }
            EditorGUILayout.EndVertical();
        }

        // ───────────────────── Зоны спавна ─────────────────────

        public void DrawSpawnZoneSettings(SerializedProperty settings, bool showHelpBoxes, WorldGeneration spawner)
        {
            Color textColor = settings.FindPropertyRelative("gizmoColor").colorValue;
            EditorFoldoutState.SpawnZoneSettings = DrawSection("🗺️", "Зоны спавна",
                EditorFoldoutState.SpawnZoneSettings, textColor, spawner, () =>
            {
                SerializedProperty modeProp = settings.FindPropertyRelative("zoneMode");
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(modeProp, ZoneModeContent);
                bool modeChanged = EditorGUI.EndChangeCheck();

                SpawnZoneMode mode = (SpawnZoneMode)modeProp.enumValueIndex;

                if (modeChanged && mode == SpawnZoneMode.Polygon)
                {
                    SerializedProperty polys = settings.FindPropertyRelative("polygons");
                    if (polys != null && polys.arraySize == 0)
                    {
                        if (spawner != null) Undo.RecordObject(spawner, "Add Default Polygon Zones");
                        SpawnZoneDefaults.FillPolygonDefaults(polys);
                    }
                }

                EditorGUILayout.Space(4f);
                DrawSnapToolbar();
                EditorGUILayout.Space(4f);

                if (mode == SpawnZoneMode.Box) DrawBoxZones(settings);
                else DrawPolygonZones(settings);
            });
        }

        private static void DrawSnapToolbar()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(SnapLabelContent, GUILayout.Width(70f));

            int current = FindSnapIndex(SpawnZoneSceneEditor.SnapStep);
            int next = GUILayout.Toolbar(current, SnapLabels, GUILayout.Height(18));

            if (next != current) SpawnZoneSceneEditor.SnapStep = SnapSteps[next];
            EditorGUILayout.EndHorizontal();
        }

        private static int FindSnapIndex(float step)
        {
            for (int i = 0; i < SnapSteps.Length; i++)
                if (Mathf.Approximately(SnapSteps[i], step)) return i;
            return 2;
        }

        private void DrawBoxZones(SerializedProperty settings)
        {
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("dimensions"),
                new GUIContent("Основная зона",
                    "Размер основной зоны. Центр всегда в локальном (0, 0, 0).\nX, Z — площадь; Y — вертикальный диапазон проверки."),
                true);

            EditorGUILayout.Space(4f);

            EditorGUILayout.PropertyField(settings.FindPropertyRelative("extraBoxes"),
                new GUIContent("Дополнительные зоны",
                    "Дополнительные box-зоны с собственным центром и размером.\nПеретаскивайте центр в Scene View."),
                true);
        }

        private void DrawPolygonZones(SerializedProperty settings)
        {
            EditorGUILayout.BeginVertical(_styleManager.HelpBoxStyle);
            EditorGUILayout.LabelField("<b>✏️ Редактирование в Scene View</b>", _styleManager.MiniLabelStyle);
            EditorGUILayout.LabelField(
                "• <b>Жёлтый шар</b> — центр ребра (position, 3D)\n" +
                "• <b>Голубой шар</b> над вершиной — вертикальный размер (Y)\n" +
                "• <b>Розовый шар</b> — первая вершина (начало контура)\n" +
                "• <b>Оранжевый шар</b> — центр всей фигуры\n" +
                "• <b>Зелёная точка</b> на ребре — вставить вершину\n" +
                "• <b>Shift+клик</b> по вершине — удалить (мин. 3)\n" +
                "• <b>Ctrl</b> при drag — снап к сетке (см. выше)\n\n" +
                "Каждая вершина — центр вертикальной грани. Верхний и нижний контуры образуют 3D-объём.",
                _styleManager.MiniLabelStyle);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4f);

            SerializedProperty polys = settings.FindPropertyRelative("polygons");
            if (polys != null) DrawConcavityWarningIfNeeded(polys);
            EditorGUILayout.PropertyField(polys, PolygonsContent, true);
        }

        private void DrawConcavityWarningIfNeeded(SerializedProperty polys)
        {
            int concaveCount = 0;

            for (int i = 0; i < polys.arraySize; i++)
            {
                SerializedProperty verts = polys.GetArrayElementAtIndex(i).FindPropertyRelative("vertices");
                if (verts == null) continue;

                int n = verts.arraySize;
                if (n < 3 || n > _convexityBuffer.Length) continue;

                for (int j = 0; j < n; j++)
                {
                    SerializedProperty v = verts.GetArrayElementAtIndex(j);
                    _convexityBuffer[j] = v.FindPropertyRelative("position").vector3Value;
                }

                if (!PolygonTriangulator.IsConvex(new ReadOnlySpan<Vector3>(_convexityBuffer, 0, n)))
                    concaveCount++;
            }

            if (concaveCount == 0) return;

            if (concaveCount != _concaveCachedCount)
            {
                _concaveSb.Clear();
                _concaveSb.Append("⚠️ Невыпуклая форма (").Append(concaveCount)
                    .Append("). Используется ear-clipping триангуляция. Убедитесь, что форма не самопересекается.");
                _concaveCachedText = _concaveSb.ToString();
                _concaveCachedCount = concaveCount;
            }

            EditorGUILayout.BeginVertical(_styleManager.HelpBoxStyle);
            EditorGUILayout.LabelField(_concaveCachedText, _styleManager.MiniLabelStyle);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        // ───────────────────── Кластеризация ─────────────────────

        public void DrawClusteringSettings(SerializedProperty settings, bool showHelpBoxes, WorldGeneration spawner)
        {
            Color textColor = settings.FindPropertyRelative("gizmoColor").colorValue;
            EditorFoldoutState.ClusteringSettings = DrawSection("🌳", "Кластеризация",
                EditorFoldoutState.ClusteringSettings, textColor, spawner, () =>
            {
                EditorGUILayout.BeginVertical("box");
                SerializedProperty useClustering = settings.FindPropertyRelative("useClustering");
                EditorGUILayout.PropertyField(useClustering, new GUIContent("useClustering",
                    "Группировать объекты в кластеры."));

                if (useClustering.boolValue)
                {
                    DrawRelative(settings, "clusterCount",
                        "Количество кластеров.");
                    DrawRelative(settings, "clusterRadiusRange",
                        "Диапазон радиусов кластеров.");
                    DrawRelative(settings, "objectsPerClusterRange",
                        "Диапазон количества объектов в кластере.");
                    DrawRelative(settings, "minDistanceBetweenClusters",
                        "Минимальное расстояние между центрами кластеров.");
                }
                EditorGUILayout.EndVertical();
            });
        }

        // ───────────────────── Лучи ─────────────────────

        public void DrawRaySettings(SerializedProperty settings, bool showHelpBoxes, WorldGeneration spawner)
        {
            Color textColor = settings.FindPropertyRelative("gizmoColor").colorValue;
            EditorFoldoutState.RaySettings = DrawSection("🔦", "Настройки луча",
                EditorFoldoutState.RaySettings, textColor, spawner, () =>
            {
                EditorGUILayout.BeginVertical("box");
                DrawRelative(settings, "rayCastType",
                    "Тип луча для обнаружения поверхности:\n• Ray — точечный\n• Sphere — сферический");

                SerializedProperty rayCastTypeProp = settings.FindPropertyRelative("rayCastType");
                if (rayCastTypeProp != null && (RayCastType)rayCastTypeProp.enumValueIndex == RayCastType.Sphere)
                    DrawRelative(settings, "raySphereRadius",
                        "Радиус сферы при rayCastType = Sphere.");

                DrawRelative(settings, "rayOriginType",
                    "Точка испускания лучей:\n• TopFace — верхняя грань\n• SideFaces — боковые грани\n• InsideVolume — изнутри объёма");
                DrawRelative(settings, "maxRayAngle",
                    "Максимальный случайный угол отклонения луча от вертикали. (0,0) = строго вниз.");
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(5f);

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("🚀 Job-валидация (>2000 объектов)", EditorStyles.boldLabel);

                SerializedProperty useJobs = settings.FindPropertyRelative("useJobValidation");
                EditorGUILayout.PropertyField(useJobs, new GUIContent(useJobs.displayName,
                    "Батч-рейкасты через RaycastCommand.ScheduleBatch.\n" +
                    "Сильно ускоряет валидацию на больших сценах (>2000 кандидатов в кадре).\n" +
                    "Требует rayCastType = Ray.\n" +
                    "При недоступности Unity.Collections или Sphere-кастов — авто-fallback на CPU."));

                if (useJobs.boolValue)
                {
                    DrawRelative(settings, "jobBatchSize",
                        "Количество команд на один worker-поток (minCommandsPerJob).\n" +
                        "64 — безопасный дефолт, для тяжёлых сцен можно 128-256.");

                    SerializedProperty rct = settings.FindPropertyRelative("rayCastType");
                    if (rct != null && (RayCastType)rct.enumValueIndex == RayCastType.Sphere)
                    {
                        EditorGUILayout.Space(3f);
                        EditorGUILayout.BeginVertical(_styleManager.HelpBoxStyle);
                        EditorGUILayout.LabelField(
                            "ℹ️ <b>rayCastType = Sphere</b> — job-путь не поддерживает сферические касты и использует CPU.\n" +
                            "Для активации job-валидации переключите rayCastType на Ray.",
                            _styleManager.MiniLabelStyle);
                        EditorGUILayout.EndVertical();
                    }
                }
                EditorGUILayout.EndVertical();
            });
        }

        // ───────────────────── Стабильность ─────────────────────

        public void DrawStabilitySettings(SerializedProperty settings, bool showHelpBoxes, WorldGeneration spawner)
        {
            Color textColor = settings.FindPropertyRelative("gizmoColor").colorValue;
            EditorFoldoutState.StabilitySettings = DrawSection("📊", "Проверка ровности поверхности",
                EditorFoldoutState.StabilitySettings, textColor, spawner, () =>
            {
                EditorGUILayout.BeginVertical("box");
                SerializedProperty edgeCheckRadius = settings.FindPropertyRelative("edgeCheckRadius");
                EditorGUILayout.PropertyField(edgeCheckRadius, new GUIContent("edgeCheckRadius",
                    "Радиус проверки стабильности поверхности. 0 = отключить."));

                if (edgeCheckRadius.floatValue > 0)
                {
                    DrawRelative(settings, "maxHeightDifference",
                        "Максимальная допустимая разница высот (м). 0 = любая разница = отказ.");
                    DrawRelative(settings, "stabilityCheckRays",
                        "Количество лучей проверки стабильности. 4-8 быстро, 12-16 точно.");
                    DrawRelative(settings, "minSuccessPercentage",
                        "Минимальный процент успешных проверок для валидности точки.");
                }
                EditorGUILayout.EndVertical();
            });
        }

        // ───────────────────── Избегание / полы / потолки ─────────────────────

        public void DrawAvoidanceSettings(SerializedProperty settings, bool showHelpBoxes, WorldGeneration spawner)
        {
            Color textColor = settings.FindPropertyRelative("gizmoColor").colorValue;
            EditorFoldoutState.AvoidanceSettings = DrawSection("🚫", "Избегание препятствий и потолки",
                EditorFoldoutState.AvoidanceSettings, textColor, spawner, () =>
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("🏠 Проверка потолка", EditorStyles.boldLabel);
                SerializedProperty ceilingMode = settings.FindPropertyRelative("ceilingCheckMode");
                EditorGUILayout.PropertyField(ceilingMode, new GUIContent("ceilingCheckMode",
                    "Режим проверки потолка:\n" +
                    "• None — не проверять\n" +
                    "• RequireOpenSky — требовать открытое небо\n" +
                    "• RequireRoof — требовать крышу (пещеры, дома)"));

                if ((CeilingCheckMode)ceilingMode.enumValueIndex != CeilingCheckMode.None)
                {
                    DrawRelative(settings, "ceilingMask",
                        "Слои, считающиеся потолком. Если пусто — используется avoidMask.");
                    DrawRelative(settings, "ceilingCheckDistance",
                        "Длина луча проверки потолка в метрах.");
                    DrawRelative(settings, "ceilingCheckRadius",
                        "Радиус сферической проверки потолка. 0 = точечный луч.");
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(3f);

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("🕳️ Пол и препятствия", EditorStyles.boldLabel);
                DrawRelative(settings, "floorCheckDistance",
                    "Дистанция проверки пола под точкой размещения. 0 = отключить.");
                DrawRelative(settings, "avoidanceRadius",
                    "Радиус проверки препятствий вокруг точки размещения. 0 = отключить.");
                DrawRelative(settings, "minDistanceBetweenObjects",
                    "Минимальное расстояние между объектами.");
                EditorGUILayout.EndVertical();
            });
        }

        // ───────────────────── Runtime ─────────────────────

        public void DrawRuntimeSettings(SerializedProperty settings, bool showHelpBoxes, WorldGeneration spawner)
        {
            Color textColor = settings.FindPropertyRelative("gizmoColor").colorValue;
            EditorFoldoutState.RuntimeSettings = DrawSection("⚡", "Настройки Runtime",
                EditorFoldoutState.RuntimeSettings, textColor, spawner, () =>
            {
                EditorGUILayout.BeginVertical("box");
                SerializedProperty enableProp = settings.FindPropertyRelative("enableRuntimeGeneration");
                EditorGUILayout.PropertyField(enableProp, new GUIContent(enableProp.displayName,
                    "Разрешить работу генератора в собранной игре."));

                if (enableProp.boolValue)
                {
                    DrawRelative(settings, "autoGenerateOnStart",
                        "Автоматически запускать генерацию при старте (только если enableRuntimeGeneration).");
                    DrawRelative(settings, "destroyAfterGeneration",
                        "Удалить компонент после завершения генерации.");

                    EditorGUILayout.Space(5f);
                    EditorGUILayout.LabelField("🛡️ Защита от бесконечных циклов", EditorStyles.boldLabel);
                    DrawRelative(settings, "runtimeMaxAttempts",
                        "Максимальное общее количество попыток размещения в Runtime.");
                    DrawRelative(settings, "runtimeRetryCount",
                        "Количество перезапусков при недостижении цели (только Runtime).");
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(3f);

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("♻️ Переиспользование объектов", EditorStyles.boldLabel);
                DrawRelative(settings, "reuseExistingObjects",
                    "Переиспользовать существующие дочерние объекты при повторной генерации:\n" +
                    "• Меньше объектов → лишние деактивируются (SetActive false), не удаляются.\n" +
                    "• Больше объектов → недостающие инстанцируются.\n" +
                    "Работает и в редакторе, и в Runtime.");
                EditorGUILayout.EndVertical();
            });
        }

        // ───────────────────── NavMesh ─────────────────────

        public void DrawNavMeshSettings(SerializedProperty settings, bool showHelpBoxes, WorldGeneration spawner)
        {
            Color textColor = settings.FindPropertyRelative("gizmoColor").colorValue;
            EditorFoldoutState.NavMeshSettings = DrawSection("🧭", "NavMesh",
                EditorFoldoutState.NavMeshSettings, textColor, spawner, () =>
            {
                EditorGUILayout.BeginVertical("box");
                SerializedProperty useProp = settings.FindPropertyRelative("useNavMeshCheck");
                EditorGUILayout.PropertyField(useProp, new GUIContent(useProp.displayName,
                    "Проверять, что точка находится на NavMesh."));

                if (useProp.boolValue)
                {
                    DrawRelative(settings, "navMeshSampleRadius",
                        "Радиус поиска ближайшей точки на NavMesh. 0 = 1.");
                    DrawRelative(settings, "snapToNavMesh",
                        "Корректировать позицию на ближайшую точку NavMesh.");
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(3f);

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("🔄 Авто-перегенерация NavMesh Surface", EditorStyles.boldLabel);

                SerializedProperty modeProp = settings.FindPropertyRelative("navMeshRegenerationMode");
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(modeProp, new GUIContent("Режим",
                    "Когда вызывать NavMeshSurface.BuildNavMesh() (только редактор):\n" +
                    "• Never — никогда\n" +
                    "• OnSingleGeneration — только при одиночной генерации\n" +
                    "• OnFullRegeneration — только при полной перегенерации\n" +
                    "• Always — всегда\n\n" +
                    "Настройка автоматически синхронизируется со всеми спавнерами сцены."));
                bool modeChanged = EditorGUI.EndChangeCheck();

                DrawRelative(settings, "requestNavMeshRegeneration",
                    "Этот спавнер запрашивает перегенерацию NavMesh после своей генерации.\n" +
                    "Отключите для мобов и декора, не меняющих проходимость.");

                if (modeChanged)
                {
                    int newIdx = modeProp.enumValueIndex;
                    WorldGeneration[] all = UnityEngine.Object.FindObjectsByType<WorldGeneration>(FindObjectsInactive.Include);
                    for (int i = 0; i < all.Length; i++)
                    {
                        if (all[i] == spawner) continue;
                        all[i].settings.navMeshRegenerationMode = (NavMeshRegenerationMode)newIdx;
                        EditorUtility.SetDirty(all[i]);
                    }
                }

                if ((NavMeshRegenerationMode)modeProp.enumValueIndex != NavMeshRegenerationMode.Never &&
                    !NavMeshRegenerationService.HasNavMeshSurface())
                {
                    EditorGUILayout.Space(3f);
                    EditorGUILayout.BeginVertical(_styleManager.HelpBoxStyle);
                    EditorGUILayout.LabelField(
                        "ℹ️ <b>Пакет AI Navigation не обнаружен.</b>\nУстановите его через Package Manager.",
                        _styleManager.MiniLabelStyle);
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();
            });
        }

        // ───────────────────── Хелперы ─────────────────────

        private bool DrawSection(string emoji, string title, bool showState, Color textColor,
            WorldGeneration spawner, System.Action content)
        {
            EditorGUILayout.BeginVertical("box");
            string colorHex = ColorUtility.ToHtmlStringRGB(textColor);
            string displayName = $"{emoji} <color=#{colorHex}>{title}</color>";
            GUIContent contentLabel = new(displayName);

            bool newState = EditorGUILayout.Foldout(showState, contentLabel, _styleManager.FoldoutStyle);
            if (newState != showState) showState = newState;

            if (showState) { EditorGUI.indentLevel++; content(); EditorGUI.indentLevel--; }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
            return showState;
        }

        /// <summary>Рисует поле с тултипом. Display name берётся из атрибута или camelCase.</summary>
        private void DrawRelative(SerializedProperty parent, string propertyName, string tooltip)
        {
            SerializedProperty property = parent.FindPropertyRelative(propertyName);
            if (property == null) return;
            EditorGUILayout.PropertyField(property, new GUIContent(property.displayName, tooltip));
        }
    }
}
#endif