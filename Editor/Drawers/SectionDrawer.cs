#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace NeonImperium.WorldGenerations
{
    public class SectionDrawer
    {
        private readonly EditorStyleManager _styleManager;

        public SectionDrawer(EditorStyleManager styleManager)
        {
            _styleManager = styleManager;
        }

        public void DrawSpawnSettings(SerializedProperty settings, bool showHelpBoxes, UnityEngine.Object[] targets, WorldGeneration spawner)
        {
            Color textColor = settings.FindPropertyRelative("gizmoColor").colorValue;
            EditorFoldoutState.SpawnSettings = DrawSection("⚙️", "Основные настройки", EditorFoldoutState.SpawnSettings, textColor, spawner, () =>
            {
                EditorGUILayout.BeginVertical("box");
                {
                    EditorGUILayout.LabelField("🎨 Визуальные настройки", EditorStyles.boldLabel);
                    DrawPropertyWithHelp(settings, "gizmoColor", "Цвет зоны спавна в редакторе. Не влияет на игровой процесс.");
                    DrawPropertyWithHelp(settings, "generationAlgorithm", "Тип алгоритма генерации объектов. Влияет на логику обработки после создания.");
                    DrawPropertyWithHelp(settings, "priority", "Приоритет генерации. Спавнеры с более высоким приоритетом генерируются первыми при полной перегенерации.");
                    DrawPropertyWithHelp(settings, "prefabs", "Список префабов для случайного выбора. Если пустой - генерация не работает.");
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(3f);
                
                EditorGUILayout.BeginVertical("box");
                {
                    EditorGUILayout.LabelField("📏 Размеры и количество", EditorStyles.boldLabel);
                    DrawPropertyWithHelp(settings, "dimensions", "Размеры зоны спавна:\n• X - ширина\n• Z - глубина\n• Y - высота проверки поверхности");
                    DrawPropertyWithHelp(settings, "population", "Целевое количество объектов. Реальное количество может быть меньше из-за ограничений размещения.");
                }
                EditorGUILayout.EndVertical();

                if (showHelpBoxes)
                {
                    EditorGUILayout.Space(3f);
                    EditorGUILayout.BeginVertical(_styleManager.HelpBoxStyle);
                    EditorGUILayout.LabelField("<b>📏 Размеры зоны спавна</b>", _styleManager.MiniLabelStyle);
                    EditorGUILayout.LabelField("• X,Z определяют площадь размещения\n• Y влияет на высоту проверки поверхности", _styleManager.MiniLabelStyle);
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.Space(3f);
                
                EditorGUILayout.BeginVertical("box");
                {
                    EditorGUILayout.LabelField("🎲 Вариации объектов", EditorStyles.boldLabel);
                    DrawPropertyWithHelp(settings, "scaleRange", "Диапазон случайного масштаба:\n• Min - минимальный масштаб\n• Max - максимальный масштаб");
                    DrawPropertyWithHelp(settings, "rotationRange", "Диапазон случайного вращения вокруг оси Y:\n• (0, 360) - полный случайный поворот\n• (0, 0) - без вращения");
                    DrawPropertyWithHelp(settings, "verticalOffset", "Случайное смещение по вертикали от точки попадания:\n• X - минимальное смещение\n• Y - максимальное смещение");
                }
                EditorGUILayout.EndVertical();

                if (showHelpBoxes)
                {
                    EditorGUILayout.Space(3f);
                    EditorGUILayout.BeginVertical(_styleManager.HelpBoxStyle);
                    EditorGUILayout.LabelField("<b>💡 Советы по вариациям</b>", _styleManager.MiniLabelStyle);
                    EditorGUILayout.LabelField("• <b>Scale Range:</b> Используйте (0.8, 1.2) для естественного вида\n• <b>Rotation Range:</b> (0, 360) для случайной ориентации\n• <b>Vertical Offset:</b> Корректирует высоту посадки объектов", _styleManager.MiniLabelStyle);
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.Space(3f);
                
                EditorGUILayout.BeginVertical("box");
                {
                    EditorGUILayout.LabelField("🎯 Правила размещения", EditorStyles.boldLabel);
                    DrawPropertyWithHelp(settings, "collisionMask", "Слои для поиска поверхности. Отметьте ТОЛЬКО те слои, которые должны считаться валидной поверхностью.");
                    DrawPropertyWithHelp(settings, "avoidMask", "Слои-препятствия. Отметьте слои, которые должны блокировать размещение объектов.");
                    DrawPropertyWithHelp(settings, "alignToSurface", "Автоматически выравнивать объекты по нормали поверхности.");
                    DrawPropertyWithHelp(settings, "maxSurfaceAngle", "Максимальный допустимый угол наклона поверхности (в градусах). 0 = отключить проверку.");
                    DrawPropertyWithHelp(settings, "maxPlacementAttempts", "Максимальное количество попыток размещения одного объекта перед отказом.");
                }
                EditorGUILayout.EndVertical();

                if (showHelpBoxes)
                {
                    EditorGUILayout.Space(3f);
                    EditorGUILayout.BeginVertical(_styleManager.HelpBoxStyle);
                    EditorGUILayout.LabelField("<b>✅ СЛОИ ДЛЯ РАЗМЕЩЕНИЯ</b>", _styleManager.MiniLabelStyle);
                    EditorGUILayout.LabelField("Отметьте <b>ТОЛЬКО</b> те слои, которые должны считаться валидной поверхностью для размещения объектов.\n<b>Пример:</b> Ground, Terrain, Floor, Platform", _styleManager.MiniLabelStyle);
                    EditorGUILayout.LabelField("<b>❌ СЛОИ-ПРЕПЯТСТВИЯ</b>", _styleManager.MiniLabelStyle);
                    EditorGUILayout.LabelField("Отметьте слои, которые должны блоклировать размещение объектов.\n<b>Пример:</b> Building, Obstacle, Water, Player", _styleManager.MiniLabelStyle);
                    EditorGUILayout.LabelField("<b>⚙️ Рекомендации</b>", _styleManager.MiniLabelStyle);
                    EditorGUILayout.LabelField("• <b>Align To Surface:</b> ВКЛ для деревьев, камней, построек\n• <b>Align To Surface:</b> ВЫКЛ для вертикальных объектов, столбов\n• <b>Max Placement Attempts:</b> 10-30 для баланса скорости/качества", _styleManager.MiniLabelStyle);
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.Space(3f);
                
                EditorGUILayout.BeginVertical("box");
                {
                    EditorGUILayout.LabelField("🔌 Расширения", EditorStyles.boldLabel);
                    DrawPropertyWithHelp(settings, "onSpawnExtensions", "Расширения, вызываемые при создании каждого объекта.");
                    DrawPropertyWithHelp(settings, "onGenerationCompleteExtensions", "Расширения, вызываемые после завершения генерации всех объектов.");
                }
                EditorGUILayout.EndVertical();

                if (showHelpBoxes)
                {
                    EditorGUILayout.Space(3f);
                    EditorGUILayout.BeginVertical(_styleManager.HelpBoxStyle);
                    EditorGUILayout.LabelField("<b>🔌 Как использовать расширения</b>", _styleManager.MiniLabelStyle);
                    EditorGUILayout.LabelField("<b>1.</b> Создайте новый скрипт, наследующий от <b>WorldGenerationExtension</b>\n<b>2.</b> Реализуйте методы:\n   • <b>OnGameObjectSpawned</b> - вызывается для каждого созданного объекта\n   • <b>OnGenerationComplete</b> - вызывается один раз после всей генерации\n<b>3.</b> Добавьте компонент с этим скриптом на любой GameObject в сцене\n<b>4.</b> Перетащите его в соответствующий список расширений выше", _styleManager.MiniLabelStyle);
                    EditorGUILayout.EndVertical();
                }

                int collision = settings.FindPropertyRelative("collisionMask").intValue;
                int avoid = settings.FindPropertyRelative("avoidMask").intValue;
                int conflictLayers = avoid & ~collision;

                if (conflictLayers != 0)
                {
                    EditorGUILayout.Space(4f);
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField(
                        "⚠️ <b>ВНИМАНИЕ:</b> Avoid Mask содержит слои, не включенные в Collision Mask!\n" +
                        "Это может привести к непредсказуемому поведению.",
                        _styleManager.MiniLabelStyle
                    );
                    
                    if (GUILayout.Button("🔧 Автоисправление: Добавить слои Avoid в Collision"))
                    {
                        for (int i = 0; i < targets.Length; i++)
                        {
                            WorldGeneration tgt = targets[i] as WorldGeneration;
                            Undo.RecordObject(tgt, "Исправить Collision Mask");
                            tgt.settings.collisionMask.value |= conflictLayers;
                            EditorUtility.SetDirty(tgt);
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
            });
        }

        public void DrawClusteringSettings(SerializedProperty settings, bool showHelpBoxes, WorldGeneration spawner)
        {
            Color textColor = settings.FindPropertyRelative("gizmoColor").colorValue;
            EditorFoldoutState.ClusteringSettings = DrawSection("🌳", "Кластеризация", EditorFoldoutState.ClusteringSettings, textColor, spawner, () =>
            {
                EditorGUILayout.BeginVertical("box");
                SerializedProperty useClustering = settings.FindPropertyRelative("useClustering");
                EditorGUILayout.PropertyField(useClustering, new GUIContent("useClustering", "Группировать объекты в скопления вместо равномерного распределения"));
                
                if (useClustering.boolValue)
                {
                    DrawPropertyWithHelp(settings, "clusterCount", "Количество групп объектов:\n• Меньше = более разреженное размещение\n• Больше = более плотное заполнение");
                    DrawPropertyWithHelp(settings, "clusterRadiusRange", "Размеры групп:\n• X - минимальный радиус кластера\n• Y - максимальный радиус кластера");
                    DrawPropertyWithHelp(settings, "objectsPerClusterRange", "Количество объектов в группе:\n• X - минимальное количество\n• Y - максимальное количество");
                    DrawPropertyWithHelp(settings, "minDistanceBetweenClusters", "Минимальное расстояние между центрами кластеров:\n• Предотвращает пересечение групп\n• Больше значения = более равномерное распределение");

                    if (showHelpBoxes)
                    {
                        EditorGUILayout.Space(3f);
                        EditorGUILayout.BeginVertical(_styleManager.HelpBoxStyle);
                        EditorGUILayout.LabelField("<b>🌳 Примеры использования кластеризации</b>", _styleManager.MiniLabelStyle);
                        EditorGUILayout.LabelField("• <b>Деревья:</b> 10-20 кластеров, 3-8 объектов в каждом\n• <b>Камни:</b> 15-30 кластеров, 2-5 объектов в каждом\n• <b>Здания:</b> 5-15 кластеров, 4-10 объектов в каждом", _styleManager.MiniLabelStyle);
                        EditorGUILayout.EndVertical();
                    }
                }
                else if (showHelpBoxes)
                {
                    EditorGUILayout.Space(3f);
                    EditorGUILayout.BeginVertical(_styleManager.HelpBoxStyle);
                    EditorGUILayout.LabelField("<b>💡 Кластеризация отключена</b>", _styleManager.MiniLabelStyle);
                    EditorGUILayout.LabelField("Объекты будут распределены равномерно по всей зоне.", _styleManager.MiniLabelStyle);
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();
            });
        }

        public void DrawRaySettings(SerializedProperty settings, bool showHelpBoxes, WorldGeneration spawner)
        {
            Color textColor = settings.FindPropertyRelative("gizmoColor").colorValue;
            EditorFoldoutState.RaySettings = DrawSection("🔦", "Настройки луча", EditorFoldoutState.RaySettings, textColor, spawner, () =>
            {
                EditorGUILayout.BeginVertical("box");
                DrawPropertyWithHelp(settings, "rayCastType", "Тип луча для обнаружения поверхности:\n• Ray - обычный луч (точечный)\n• Sphere - сферический луч (объемный)");
                
                SerializedProperty rayCastTypeProp = settings.FindPropertyRelative("rayCastType");
                if (rayCastTypeProp != null && (RayCastType)rayCastTypeProp.enumValueIndex == RayCastType.Sphere)
                {
                    DrawPropertyWithHelp(settings, "raySphereRadius", "Радиус сферы при использовании SphereCast.");
                }
                
                DrawPropertyWithHelp(settings, "rayOriginType", "Точка испускания лучей:\n• TopFace - с верхней грани (стандартный)\n• SideFaces - со случайной боковой грани\n• InsideVolume - из случайной точки внутри объема");
                DrawPropertyWithHelp(settings, "maxRayAngle", "Угол отклонения луча от вертикали:\n• (0,0) - строго вертикально вниз\n• (5,15) - небольшой случайный разброс\n• (30,60) - для сложных поверхностей");
                EditorGUILayout.EndVertical();

                if (showHelpBoxes)
                {
                    EditorGUILayout.Space(3f);
                    EditorGUILayout.BeginVertical(_styleManager.HelpBoxStyle);
                    EditorGUILayout.LabelField("<b>🎯 Советы по настройке лучей</b>", _styleManager.MiniLabelStyle);
                    EditorGUILayout.LabelField("<b>Ray Cast Type:</b>\n• <b>Ray</b> - для точного позиционирования\n• <b>Sphere</b> - для сложных поверхностей\n\n<b>Ray Origin Type:</b>\n• <b>TopFace</b> - стандартный вариант\n• <b>SideFaces</b> - для стен и вертикальных поверхностей\n• <b>InsideVolume</b> - для заполнения объемов\n\n<b>Max Ray Angle:</b>\n• <b>(0,0)</b> - строго вертикально\n• <b>(5,15)</b> - небольшой разброс\n• <b>(30,60)</b> - для наклонных поверхностей", _styleManager.MiniLabelStyle);
                    EditorGUILayout.EndVertical();
                }
            });
        }

        public void DrawStabilitySettings(SerializedProperty settings, bool showHelpBoxes, WorldGeneration spawner)
        {
            Color textColor = settings.FindPropertyRelative("gizmoColor").colorValue;
            EditorFoldoutState.StabilitySettings = DrawSection("📊", "Проверка ровности поверхности", EditorFoldoutState.StabilitySettings, textColor, spawner, () =>
            {
                EditorGUILayout.BeginVertical("box");
                SerializedProperty edgeCheckRadius = settings.FindPropertyRelative("edgeCheckRadius");
                EditorGUILayout.PropertyField(edgeCheckRadius, new GUIContent("edgeCheckRadius", "Радиус проверки ровности поверхности. 0 = отключить проверку"));
                
                if (edgeCheckRadius.floatValue > 0)
                {
                    if (showHelpBoxes)
                    {
                        EditorGUILayout.Space(3f);
                        EditorGUILayout.BeginVertical(_styleManager.HelpBoxStyle);
                        EditorGUILayout.LabelField("<b>📐 ПРОСТАЯ ПРОВЕРКА ПО ВЫСОТЕ</b>", _styleManager.MiniLabelStyle);
                        EditorGUILayout.LabelField("Система проверяет высоту поверхности вокруг точки и сравнивает с центральной высотой.\n• Простая и надежная логика\n• Не зависит от углов наклона\n• Обнаруживает обрывы и резкие перепады", _styleManager.MiniLabelStyle);
                        EditorGUILayout.EndVertical();
                    }
                    
                    DrawPropertyWithHelp(settings, "maxHeightDifference", "Максимальная разница высот (в метрах):\n• 0.1 = 10 см - очень строгая проверка\n• 0.3 = 30 см - стандартная проверка\n• 0.5 = 50 см - мягкая проверка\n• 1.0 = 1 метр - очень мягкая проверка\n• 0.0 = любая разница = отказ");
                    DrawPropertyWithHelp(settings, "stabilityCheckRays", "Количество проверочных лучей:\n• 4-6 - быстрая проверка по основным направлениям\n• 8-12 - точная проверка\n• 16 - максимальная точность (медленно)");
                    DrawPropertyWithHelp(settings, "minSuccessPercentage", "Минимальный процент успешных проверок:\n• 100% - ВСЕ лучи должны быть успешны\n• 75% - 3/4 лучей должны быть успешны\n• 50% - половина лучей должна быть успешна\n• 25% - 1/4 лучей должна быть успешна");

                    float maxHeightDiff = settings.FindPropertyRelative("maxHeightDifference").floatValue;
                    if (maxHeightDiff == 0 && showHelpBoxes)
                    {
                        EditorGUILayout.Space(3f);
                        EditorGUILayout.BeginVertical(_styleManager.HelpBoxStyle);
                        EditorGUILayout.LabelField("<b>⚠️ РЕЖИМ АБСОЛЮТНОЙ СТРОГОСТИ</b>", _styleManager.MiniLabelStyle);
                        EditorGUILayout.LabelField("<b>ЛЮБАЯ разница высот = НЕМЕДЛЕННЫЙ ОТКАЗ!</b>\n• Объекты будут размещаться только на идеально ровных поверхностях\n• Любое отклонение высоты приведет к отказу", _styleManager.MiniLabelStyle);
                        EditorGUILayout.EndVertical();
                    }
                    else if (showHelpBoxes)
                    {
                        EditorGUILayout.Space(3f);
                        EditorGUILayout.BeginVertical(_styleManager.HelpBoxStyle);
                        EditorGUILayout.LabelField($"<b>📏 Допустимая разница высот: {maxHeightDiff:F1} метра</b>", _styleManager.MiniLabelStyle);
                        EditorGUILayout.LabelField("<b>Примеры использования:</b>\n• <b>0.1</b> - для точного размещения на ровных поверхностях\n• <b>0.3</b> - для большинства случаев (рекомендуется)\n• <b>0.5</b> - для холмистой местности\n• <b>1.0</b> - для горной местности", _styleManager.MiniLabelStyle);
                        EditorGUILayout.EndVertical();
                    }

                    if (showHelpBoxes)
                    {
                        int rays = settings.FindPropertyRelative("stabilityCheckRays").intValue;
                        float percent = settings.FindPropertyRelative("minSuccessPercentage").floatValue;
                        
                        EditorGUILayout.Space(3f);
                        EditorGUILayout.BeginVertical(_styleManager.HelpBoxStyle);
                        EditorGUILayout.LabelField("<b>🎯 КАК РАБОТАЕТ ПРОВЕРКА</b>", _styleManager.MiniLabelStyle);
                        EditorGUILayout.LabelField($"Система запускает <b>{rays} лучей</b> на расстоянии {edgeCheckRadius.floatValue} метров:\n• Сравнивает высоты точек с центральной высотой\n• Высота может отличаться на <b>{maxHeightDiff:F1} метра</b>\n• Требуется <b>{percent}%</b> успешных проверок\n• <b>✅ Зеленые лучи</b> - успешная проверка\n• <b>🔴 Красные лучи</b> - слишком большая разница высот\n• <b>🟡 Желтые лучи</b> - луч не попал (обрыв)", _styleManager.MiniLabelStyle);
                        EditorGUILayout.EndVertical();
                    }
                }
                else if (showHelpBoxes)
                {
                    EditorGUILayout.Space(3f);
                    EditorGUILayout.BeginVertical(_styleManager.HelpBoxStyle);
                    EditorGUILayout.LabelField("<b>❓ Проверка ровности отключена</b>", _styleManager.MiniLabelStyle);
                    EditorGUILayout.LabelField("Объекты могут размещаться на краях, обрывах и любых неровностях.", _styleManager.MiniLabelStyle);
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();
            });
        }

        public void DrawAvoidanceSettings(SerializedProperty settings, bool showHelpBoxes, WorldGeneration spawner)
        {
            Color textColor = settings.FindPropertyRelative("gizmoColor").colorValue;
            EditorFoldoutState.AvoidanceSettings = DrawSection("🚫", "Избегание препятствий", EditorFoldoutState.AvoidanceSettings, textColor, spawner, () =>
            {
                EditorGUILayout.BeginVertical("box");
                DrawPropertyWithHelp(settings, "checkCeiling", "Проверять наличие препятствий над точкой размещения.");
                EditorGUILayout.EndVertical();

                if (showHelpBoxes)
                {
                    EditorGUILayout.Space(3f);
                    EditorGUILayout.BeginVertical(_styleManager.HelpBoxStyle);
                    EditorGUILayout.LabelField("<b>🏠 Проверка потолка</b>", _styleManager.MiniLabelStyle);
                    EditorGUILayout.LabelField("<b>Полезно для:</b>\n• Предотвращения спавна внутри зданий\n• Избегания размещения под мостами\n• Создания открытых пространств", _styleManager.MiniLabelStyle);
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.Space(3f);
                
                EditorGUILayout.BeginVertical("box");
                DrawPropertyWithHelp(settings, "floorCheckDistance", "Дистанция проверки пола под точкой размещения.");
                DrawPropertyWithHelp(settings, "avoidanceRadius", "Радиус проверки препятствий вокруг точки размещения.");
                DrawPropertyWithHelp(settings, "minDistanceBetweenObjects", "Минимальное расстояние между объектами.");
                EditorGUILayout.EndVertical();
                
                if (showHelpBoxes)
                {
                    EditorGUILayout.Space(3f);
                    EditorGUILayout.BeginVertical(_styleManager.HelpBoxStyle);
                    EditorGUILayout.LabelField("<b>🚫 Настройки избегания препятствий</b>", _styleManager.MiniLabelStyle);
                    EditorGUILayout.LabelField("• <b>Floor Check Distance:</b> Обнаруживает ямы и провалы (0 = отключить)\n• <b>Avoidance Radius:</b> Защита от близких препятствий\n• <b>Min Distance:</b> Предотвращает наслоение объектов", _styleManager.MiniLabelStyle);
                    EditorGUILayout.EndVertical();
                }
            });
        }

        public void DrawRuntimeSettings(SerializedProperty settings, bool showHelpBoxes, WorldGeneration spawner)
        {
            Color textColor = settings.FindPropertyRelative("gizmoColor").colorValue;
            EditorFoldoutState.RuntimeSettings = DrawSection("⚡", "Настройки Runtime", EditorFoldoutState.RuntimeSettings, textColor, spawner, () =>
            {
                EditorGUILayout.BeginVertical("box");
                DrawPropertyWithHelp(settings, "enableRuntimeGeneration", "Разрешить работу генератора в собранной игре (Runtime). Если отключено, компонент будет уничтожен при старте.");
                if (settings.FindPropertyRelative("enableRuntimeGeneration").boolValue)
                {
                    DrawPropertyWithHelp(settings, "autoGenerateOnStart", "Автоматически запускать генерацию при старте (только если enableRuntimeGeneration = true).");
                    DrawPropertyWithHelp(settings, "destroyAfterGeneration", "Удалить компонент после завершения генерации (экономит память).");
                    EditorGUILayout.Space(5f);
                    EditorGUILayout.LabelField("🛡️ Защита от бесконечных циклов", EditorStyles.boldLabel);
                    DrawPropertyWithHelp(settings, "runtimeMaxAttempts", "Максимальное общее количество попыток размещения в Runtime (предотвращает зависания).");
                    DrawPropertyWithHelp(settings, "runtimeRetryCount", "Количество перезапусков генерации при недостижении цели (0 = не перезапускать).");
                }
                EditorGUILayout.EndVertical();

                if (showHelpBoxes)
                {
                    EditorGUILayout.Space(3f);
                    EditorGUILayout.BeginVertical(_styleManager.HelpBoxStyle);
                    EditorGUILayout.LabelField("<b>⚡ Особенности работы в Runtime</b>", _styleManager.MiniLabelStyle);
                    EditorGUILayout.LabelField(
                        "• <b>enableRuntimeGeneration</b> – если выключено, компонент удаляется при старте игры.\n" +
                        "• <b>autoGenerateOnStart</b> – автоматический запуск генерации при старте.\n" +
                        "• <b>destroyAfterGeneration</b> – удаляет компонент после генерации, освобождая память.\n" +
                        "• <b>runtimeMaxAttempts</b> – лимит попыток, после которого попытка прерывается.\n" +
                        "• <b>runtimeRetryCount</b> – количество дополнительных попыток, если цель не достигнута.\n" +
                        "• <b>Вручную</b> можно запустить генерацию через <b>spawner.Generate()</b> или <b>spawner.GenerateAsync()</b> (Awaitable).",
                        _styleManager.MiniLabelStyle);
                    EditorGUILayout.EndVertical();
                }
            });
        }

        public void DrawNavMeshSettings(SerializedProperty settings, bool showHelpBoxes, WorldGeneration spawner)
        {
            Color textColor = settings.FindPropertyRelative("gizmoColor").colorValue;
            EditorFoldoutState.NavMeshSettings = DrawSection("🧭", "NavMesh", EditorFoldoutState.NavMeshSettings, textColor, spawner, () =>
            {
                EditorGUILayout.BeginVertical("box");
                DrawPropertyWithHelp(settings, "useNavMeshCheck", "Проверять, что точка размещения находится на NavMesh (требуется система NavMesh).");
                if (settings.FindPropertyRelative("useNavMeshCheck").boolValue)
                {
                    DrawPropertyWithHelp(settings, "navMeshSampleRadius", "Радиус поиска ближайшей точки на NavMesh. 0 = стандартное значение (1).");
                    DrawPropertyWithHelp(settings, "snapToNavMesh", "Корректировать позицию на ближайшую точку NavMesh (если точка не на NavMesh, но рядом).");
                }
                EditorGUILayout.EndVertical();

                if (showHelpBoxes)
                {
                    EditorGUILayout.Space(3f);
                    EditorGUILayout.BeginVertical(_styleManager.HelpBoxStyle);
                    EditorGUILayout.LabelField("<b>🧭 Использование NavMesh</b>", _styleManager.MiniLabelStyle);
                    EditorGUILayout.LabelField(
                        "• <b>useNavMeshCheck</b> – включает проверку принадлежности точки NavMesh.\n" +
                        "• <b>snapToNavMesh</b> – автоматически корректирует позицию, если точка рядом с NavMesh.\n" +
                        "• <b>navMeshSampleRadius</b> – зона поиска для SamplePosition.\n" +
                        "Полезно для размещения объектов, с которыми взаимодействуют NPC (добыча, ресурсы).",
                        _styleManager.MiniLabelStyle);
                    EditorGUILayout.EndVertical();
                }
            });
        }

        private bool DrawSection(string emoji, string title, bool showState, Color textColor, WorldGeneration spawner, System.Action content)
        {
            EditorGUILayout.BeginVertical("box");
            
            // Формируем строку с Rich Text: эмодзи без тега (будут белыми), текст окрашен
            string colorHex = ColorUtility.ToHtmlStringRGB(textColor);
            string displayName = $"{emoji} <color=#{colorHex}>{title}</color>";
            GUIContent contentLabel = new GUIContent(displayName);
            
            bool newState = EditorGUILayout.Foldout(showState, contentLabel, _styleManager.FoldoutStyle);
            
            if (newState != showState)
            {
                showState = newState;
            }
            
            if (showState)
            {
                EditorGUI.indentLevel++;
                content();
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(4f);
            
            return showState;
        }

        private void DrawPropertyWithHelp(SerializedProperty parent, string propertyName, string tooltip)
        {
            SerializedProperty property = parent.FindPropertyRelative(propertyName);
            if (property != null)
            {
                EditorGUILayout.PropertyField(property, new GUIContent(property.displayName, tooltip));
            }
        }
    }
}
#endif