#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace NeonImperium
{
    public class SectionDrawer
    {
        private readonly EditorStyleManager _styleManager;

        public SectionDrawer(EditorStyleManager styleManager)
        {
            _styleManager = styleManager;
        }

        public void DrawSpawnSettings(SerializedProperty settings, ref bool showState, bool showHelpBoxes)
        {
            DrawSection("⚙️ Основные настройки", ref showState, () => {
                EditorGUILayout.BeginVertical("box");
                
                DrawPropertyWithHelp(settings, "gizmoColor", "Цвет зоны спавна в редакторе. Не влияет на игровой процесс.");
                EditorGUILayout.Space(3f);
                
                DrawPropertyWithHelp(settings, "spawnType", "Тип объектов для спавна:\n• Object - обычные объекты\n• Enemy - враги (специальная логика)");
                EditorGUILayout.Space(3f);

                DrawPropertyWithHelp(settings, "raceType", "Тип расы объекта, фракция которой он принадлежит");
                EditorGUILayout.Space(3f);
                
                DrawPropertyWithHelp(settings, "prefabs", "Список префабов для случайного выбора. Если пустой - генерация не работает.");
                EditorGUILayout.Space(3f);
                
                DrawPropertyWithHelp(settings, "population", "Целевое количество объектов. Реальное количество может быть меньше из-за ограничений размещения.");
                EditorGUILayout.Space(3f);
                
                DrawPropertyWithHelp(settings, "dimensions", "Размеры зоны спавна:\n• X - ширина\n• Z - глубина\n• Y - высота проверки поверхности");
                
                if (showHelpBoxes)
                {
                    EditorGUILayout.Space(3f);
                    EditorGUILayout.BeginVertical(_styleManager.HelpBoxStyle);
                    EditorGUILayout.LabelField("<b>📏 Размеры зоны спавна</b>", _styleManager.MiniLabelStyle);
                    EditorGUILayout.LabelField("• X,Z определяют площадь размещения\n• Y влияет на высоту проверки поверхности", _styleManager.MiniLabelStyle);
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("🎨 Вариации внешнего вида", EditorStyles.boldLabel);
                EditorGUILayout.Space(3f);
                
                DrawPropertyWithHelp(settings, "scaleRange", "Диапазон случайного масштаба:\n• Min - минимальный масштаб\n• Max - максимальный масштаб");
                EditorGUILayout.Space(3f);
                
                DrawPropertyWithHelp(settings, "rotationRange", "Диапазон случайного вращения вокруг оси Y:\n• (0, 360) - полный случайный поворот\n• (0, 0) - без вращения");
                EditorGUILayout.Space(3f);
                
                DrawPropertyWithHelp(settings, "verticalOffset", "Случайное смещение по вертикали от точки попадания:\n• X - минимальное смещение\n• Y - максимальное смещение");

                if (showHelpBoxes)
                {
                    EditorGUILayout.Space(3f);
                    EditorGUILayout.BeginVertical(_styleManager.HelpBoxStyle);
                    EditorGUILayout.LabelField("<b>💡 Советы по вариациям</b>", _styleManager.MiniLabelStyle);
                    EditorGUILayout.LabelField("• <b>Scale Range:</b> Используйте (0.8, 1.2) для естественного вида\n• <b>Rotation Range:</b> (0, 360) для случайной ориентации\n• <b>Vertical Offset:</b> Корректирует высоту посадки объектов", _styleManager.MiniLabelStyle);
                    EditorGUILayout.EndVertical();
                }
                
                EditorGUILayout.EndVertical();
            });
        }

        public void DrawClusteringSettings(SerializedProperty settings, ref bool showState, bool showHelpBoxes)
        {
            DrawSection("🌳 Кластеризация", ref showState, () =>
            {
                EditorGUILayout.BeginVertical("box");
                var useClustering = settings.FindPropertyRelative("useClustering");
                EditorGUILayout.PropertyField(useClustering, new GUIContent("Использовать кластеризацию", "Группировать объекты в скопления вместо равномерного распределения"));
                
                if (useClustering.boolValue)
                {
                    EditorGUI.indentLevel++;
                    
                    EditorGUILayout.Space(3f);
                    DrawPropertyWithHelp(settings, "clusterCount", "Количество групп объектов:\n• Меньше = более разреженное размещение\n• Больше = более плотное заполнение");
                    EditorGUILayout.Space(3f);
                    
                    DrawPropertyWithHelp(settings, "clusterRadiusRange", "Размеры групп:\n• X - минимальный радиус кластера\n• Y - максимальный радиус кластера");
                    EditorGUILayout.Space(3f);
                    
                    DrawPropertyWithHelp(settings, "objectsPerClusterRange", "Количество объектов в группе:\n• X - минимальное количество\n• Y - максимальное количество");
                    EditorGUILayout.Space(3f);
                    
                    DrawPropertyWithHelp(settings, "minDistanceBetweenClusters", "Минимальное расстояние между центрами кластеров:\n• Предотвращает пересечение групп\n• Больше значения = более равномерное распределение");

                    if (showHelpBoxes)
                    {
                        EditorGUILayout.Space(3f);
                        EditorGUILayout.BeginVertical(_styleManager.HelpBoxStyle);
                        EditorGUILayout.LabelField("<b>🌳 Примеры использования кластеризации</b>", _styleManager.MiniLabelStyle);
                        EditorGUILayout.LabelField("• <b>Деревья:</b> 10-20 кластеров, 3-8 объектов в каждом\n• <b>Камни:</b> 15-30 кластеров, 2-5 объектов в каждом\n• <b>Здания:</b> 5-15 кластеров, 4-10 объектов в каждом", _styleManager.MiniLabelStyle);
                        EditorGUILayout.EndVertical();
                    }
                    
                    EditorGUI.indentLevel--;
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

        public void DrawPlacementSettings(SerializedProperty settings, ref bool showState, bool showHelpBoxes, UnityEngine.Object[] targets)
        {
            DrawSection("📐 Правила размещения", ref showState, () => {
                EditorGUILayout.BeginVertical("box");
                DrawPropertyWithHelp(settings, "collisionMask", "Слои для поиска поверхности. Отметьте ТОЛЬКО те слои, которые должны считаться валидной поверхностью.");
                
                if (showHelpBoxes)
                {
                    EditorGUILayout.Space(3f);
                    EditorGUILayout.BeginVertical(_styleManager.HelpBoxStyle);
                    EditorGUILayout.LabelField("<b>✅ СЛОИ ДЛЯ РАЗМЕЩЕНИЯ</b>", _styleManager.MiniLabelStyle);
                    EditorGUILayout.LabelField("Отметьте <b>ТОЛЬКО</b> те слои, которые должны считаться валидной поверхностью для размещения объектов.\n<b>Пример:</b> Ground, Terrain, Floor, Platform", _styleManager.MiniLabelStyle);
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(3f);
                
                EditorGUILayout.BeginVertical("box");
                DrawPropertyWithHelp(settings, "avoidMask", "Слои-препятствия. Отметьте слои, которые должны блокировать размещение объектов.");
                
                if (showHelpBoxes)
                {
                    EditorGUILayout.Space(3f);
                    EditorGUILayout.BeginVertical(_styleManager.HelpBoxStyle);
                    EditorGUILayout.LabelField("<b>❌ СЛОИ-ПРЕПЯТСТВИЯ</b>", _styleManager.MiniLabelStyle);
                    EditorGUILayout.LabelField("Отметьте слои, которые должны блокировать размещение объектов.\n<b>Пример:</b> Building, Obstacle, Water, Player\n\n<b>💡 Важно:</b> AvoidMask должен быть <b>ПОДМНОЖЕСТВОМ</b> CollisionMask!", _styleManager.MiniLabelStyle);
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(3f);
                
                EditorGUILayout.BeginVertical("box");
                DrawPropertyWithHelp(settings, "alignToSurface", "Автоматически выравнивать объекты по нормали поверхности.");
                EditorGUILayout.Space(3f);
                DrawPropertyWithHelp(settings, "maxPlacementAttempts", "Максимальное количество попыток размещения одного объекта перед отказом.");

                if (showHelpBoxes)
                {
                    EditorGUILayout.Space(3f);
                    EditorGUILayout.BeginVertical(_styleManager.HelpBoxStyle);
                    EditorGUILayout.LabelField("<b>⚙️ Рекомендации</b>", _styleManager.MiniLabelStyle);
                    EditorGUILayout.LabelField("• <b>Align To Surface:</b> ВКЛ для деревьев, камней, построек\n• <b>Align To Surface:</b> ВЫКЛ для вертикальных объектов, столбов\n• <b>Max Placement Attempts:</b> 10-30 для баланса скорости/качества", _styleManager.MiniLabelStyle);
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();
            });

            // Проверка конфликта масок
            LayerMask collision = settings.FindPropertyRelative("collisionMask").intValue;
            LayerMask avoid = settings.FindPropertyRelative("avoidMask").intValue;
            int conflictLayers = avoid.value & ~collision.value;

            if (conflictLayers != 0)
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.HelpBox(
                    "⚠️ <b>ВНИМАНИЕ:</b> Avoid Mask содержит слои, не включенные в Collision Mask!\n" +
                    "Это может привести к непредсказуемому поведению.",
                    MessageType.Warning
                );
                
                if (GUILayout.Button("🔧 Автоисправление: Добавить слои Avoid в Collision"))
                {
                    foreach (var tgt in targets.Cast<WorldGeneration>())
                    {
                        Undo.RecordObject(tgt, "Исправить Collision Mask");
                        tgt.settings.collisionMask.value |= conflictLayers;
                        EditorUtility.SetDirty(tgt);
                    }
                }
                EditorGUILayout.EndVertical();
            }
        }

        public void DrawRaySettings(SerializedProperty settings, ref bool showState, bool showHelpBoxes)
        {
            DrawSection("🔦 Настройки луча", ref showState, () =>
            {
                EditorGUILayout.BeginVertical("box");
                
                DrawPropertyWithHelp(settings, "rayCastType", "Тип луча для обнаружения поверхности:\n• Ray - обычный луч (точечный)\n• Sphere - сферический луч (объемный)");
                EditorGUILayout.Space(3f);
                
                DrawPropertyWithHelp(settings, "rayOriginType", "Точка испускания лучей:\n• TopFace - с верхней грани (стандартный)\n• SideFaces - со случайной боковой грани\n• InsideVolume - из случайной точки внутри объема");
                EditorGUILayout.Space(3f);
                
                DrawPropertyWithHelp(settings, "maxRayAngle", "Угол отклонения луча от вертикали:\n• (0,0) - строго вертикально вниз\n• (5,15) - небольшой случайный разброс\n• (30,60) - для сложных поверхностей");

                if (showHelpBoxes)
                {
                    EditorGUILayout.Space(3f);
                    EditorGUILayout.BeginVertical(_styleManager.HelpBoxStyle);
                    EditorGUILayout.LabelField("<b>🎯 Советы по настройке лучей</b>", _styleManager.MiniLabelStyle);
                    EditorGUILayout.LabelField("<b>Ray Cast Type:</b>\n• <b>Ray</b> - для точного позиционирования\n• <b>Sphere</b> - для сложных поверхностей\n\n<b>Ray Origin Type:</b>\n• <b>TopFace</b> - стандартный вариант\n• <b>SideFaces</b> - для стен и вертикальных поверхностей\n• <b>InsideVolume</b> - для заполнения объемов\n\n<b>Max Ray Angle:</b>\n• <b>(0,0)</b> - строго вертикально\n• <b>(5,15)</b> - небольшой разброс\n• <b>(30,60)</b> - для наклонных поверхностей", _styleManager.MiniLabelStyle);
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();
            });
        }

        public void DrawStabilitySettings(SerializedProperty settings, ref bool showState, bool showHelpBoxes)
        {
            DrawSection("📊 Проверка ровности поверхности", ref showState, () =>
            {
                EditorGUILayout.BeginVertical("box");
                var edgeCheckRadius = settings.FindPropertyRelative("edgeCheckRadius");
                EditorGUILayout.PropertyField(edgeCheckRadius, new GUIContent("Радиус проверки ровности", "Радиус вокруг точки, в котором проверяется ровность поверхности. 0 = отключить проверку"));
                
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
                    
                    EditorGUI.indentLevel++;
                    EditorGUILayout.Space(3f);
                    
                    DrawPropertyWithHelp(settings, "maxHeightDifference", "Максимальная разница высот (в метрах):\n• 0.1 = 10 см - очень строгая проверка\n• 0.3 = 30 см - стандартная проверка\n• 0.5 = 50 см - мягкая проверка\n• 1.0 = 1 метр - очень мягкая проверка\n• 0.0 = любая разница = отказ");
                    EditorGUILayout.Space(3f);
                    
                    DrawPropertyWithHelp(settings, "stabilityCheckRays", "Количество проверочных лучей:\n• 4-6 - быстрая проверка по основным направлениям\n• 8-12 - точная проверка\n• 16 - максимальная точность (медленно)");
                    EditorGUILayout.Space(3f);
                    
                    DrawPropertyWithHelp(settings, "minSuccessPercentage", "Минимальный процент успешных проверок:\n• 100% - ВСЕ лучи должны быть успешны\n• 75% - 3/4 лучей должны быть успешны\n• 50% - половина лучей должна быть успешна\n• 25% - 1/4 лучей должна быть успешна");

                    var maxHeightDiff = settings.FindPropertyRelative("maxHeightDifference");
                    if (maxHeightDiff.floatValue == 0 && showHelpBoxes)
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
                        EditorGUILayout.LabelField($"<b>📏 Допустимая разница высот: {maxHeightDiff.floatValue:F1} метра</b>", _styleManager.MiniLabelStyle);
                        EditorGUILayout.LabelField("<b>Примеры использования:</b>\n• <b>0.1</b> - для точного размещения на ровных поверхностях\n• <b>0.3</b> - для большинства случаев (рекомендуется)\n• <b>0.5</b> - для холмистой местности\n• <b>1.0</b> - для горной местности", _styleManager.MiniLabelStyle);
                        EditorGUILayout.EndVertical();
                    }

                    // Подсказка о работе проверки
                    if (showHelpBoxes)
                    {
                        var rays = settings.FindPropertyRelative("stabilityCheckRays");
                        var percent = settings.FindPropertyRelative("minSuccessPercentage");
                        
                        EditorGUILayout.Space(3f);
                        EditorGUILayout.BeginVertical(_styleManager.HelpBoxStyle);
                        EditorGUILayout.LabelField("<b>🎯 КАК РАБОТАЕТ ПРОВЕРКА</b>", _styleManager.MiniLabelStyle);
                        EditorGUILayout.LabelField($"Система запускает <b>{rays.intValue} лучей</b> на расстоянии {edgeCheckRadius.floatValue} метров:\n• Сравнивает высоты точек с центральной высотой\n• Высота может отличаться на <b>{maxHeightDiff.floatValue:F1} метра</b>\n• Требуется <b>{percent.floatValue}%</b> успешных проверок\n• <b>✅ Зеленые лучи</b> - успешная проверка\n• <b>🔴 Красные лучи</b> - слишком большая разница высот\n• <b>🟡 Желтые лучи</b> - луч не попал (обрыв)", _styleManager.MiniLabelStyle);
                        EditorGUILayout.EndVertical();
                    }
                    
                    EditorGUI.indentLevel--;
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

        public void DrawAvoidanceSettings(SerializedProperty settings, ref bool showState, bool showHelpBoxes)
        {
            DrawSection("🚫 Избегание препятствий", ref showState, () =>
            {
                EditorGUILayout.BeginVertical("box");
                DrawPropertyWithHelp(settings, "checkCeiling", "Проверять наличие препятствий над точкой размещения.");
                
                if (showHelpBoxes)
                {
                    EditorGUILayout.Space(3f);
                    EditorGUILayout.BeginVertical(_styleManager.HelpBoxStyle);
                    EditorGUILayout.LabelField("<b>🏠 Проверка потолка</b>", _styleManager.MiniLabelStyle);
                    EditorGUILayout.LabelField("<b>Полезна для:</b>\n• Предотвращения спавна внутри зданий\n• Избегания размещения под мостами\n• Создания открытых пространств", _styleManager.MiniLabelStyle);
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(3f);
                
                EditorGUILayout.BeginVertical("box");
                DrawPropertyWithHelp(settings, "floorCheckDistance", "Дистанция проверки пола под точкой размещения.");
                EditorGUILayout.Space(3f);
                
                DrawPropertyWithHelp(settings, "avoidanceRadius", "Радиус проверки препятствий вокруг точки размещения.");
                EditorGUILayout.Space(3f);
                
                DrawPropertyWithHelp(settings, "minDistanceBetweenObjects", "Минимальное расстояние между объектами.");
                
                if (showHelpBoxes)
                {
                    EditorGUILayout.Space(3f);
                    EditorGUILayout.BeginVertical(_styleManager.HelpBoxStyle);
                    EditorGUILayout.LabelField("<b>🚫 Настройки избегания препятствий</b>", _styleManager.MiniLabelStyle);
                    EditorGUILayout.LabelField("• <b>Floor Check Distance:</b> Обнаруживает ямы и провалы (0 = отключить)\n• <b>Avoidance Radius:</b> Защита от близких препятствий\n• <b>Min Distance:</b> Предотвращает наслоение объектов", _styleManager.MiniLabelStyle);
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();
            });
        }

        private void DrawSection(string title, ref bool showState, System.Action content)
        {
            EditorGUILayout.BeginVertical("box");
            
            showState = EditorGUILayout.Foldout(showState, title, _styleManager.FoldoutStyle);
            
            if (showState)
            {
                EditorGUI.indentLevel++;
                content();
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(4f);
        }

        private void DrawPropertyWithHelp(SerializedProperty parent, string propertyName, string tooltip)
        {
            var property = parent.FindPropertyRelative(propertyName);
            if (property != null)
            {
                EditorGUILayout.PropertyField(property, new GUIContent(property.displayName, tooltip));
            }
        }
    }
}
#endif