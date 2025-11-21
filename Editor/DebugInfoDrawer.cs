#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace NeonImperium
{
    public class DebugInfoDrawer
    {
        private bool showDebugRaySettings = false;

        public void DrawDebugInfo(WorldGeneration spawner, ref bool showDebugInfo, EditorStyleManager styleManager)
        {
            if (spawner == null) return;

            EditorGUILayout.BeginVertical("box");
                
            showDebugInfo = EditorGUILayout.Foldout(showDebugInfo, "📊 Дебаг информация", styleManager.FoldoutStyle);
            
            if (showDebugInfo)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.LabelField("📈 Статус:", spawner.IsGenerating ? "🔄 Генерация..." : "✅ Готов");
                EditorGUILayout.Space(3f);
                
                EditorGUILayout.LabelField("🎯 Объектов создано:", $"{spawner.SpawnedCount} / {spawner.settings.population}");
                EditorGUILayout.Space(3f);
                
                EditorGUILayout.LabelField("🎲 Успешные попытки:", $"{spawner.ValidPlacementCount} / {spawner.TotalPlacementAttempts}");
                EditorGUILayout.Space(3f);

                if (spawner.ValidPlacementCount > 0)
                {
                    float progress = spawner.ValidPlacementCount / (float)spawner.settings.population;
                    Rect rect = EditorGUILayout.GetControlRect(false, 20);
                    EditorGUI.ProgressBar(rect, progress, $"📊 Прогресс: {progress:P0}");
                    EditorGUILayout.Space(3f);
                }

                if (spawner.TotalPlacementAttempts > 0)
                {
                    float efficiency = (float)spawner.ValidPlacementCount / spawner.TotalPlacementAttempts;
                    Rect rectEff = EditorGUILayout.GetControlRect(false, 20);
                    EditorGUI.ProgressBar(rectEff, efficiency, $"⚡ Эффективность: {efficiency:P0}");
                    EditorGUILayout.Space(3f);

                    // Статистика ошибок
                    if (spawner.FailureStatistics != null && spawner.FailureStatistics.Count > 0)
                    {
                        EditorGUILayout.Space(3f);
                        EditorGUILayout.LabelField("❌ Причины ошибок:", EditorStyles.boldLabel);
                        
                        var sortedReasons = spawner.FailureStatistics
                            .OrderByDescending(kvp => kvp.Value)
                            .ToList();

                        foreach (var kvp in sortedReasons)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField($"{GetFailureReasonName(kvp.Key)}:");
                            EditorGUILayout.LabelField($"{kvp.Value}", GUILayout.Width(50));
                            EditorGUILayout.EndHorizontal();
                        }

                        var topReason = sortedReasons.FirstOrDefault();
                        if (topReason.Value > 0)
                        {
                            EditorGUILayout.Space(3f);
                            string advice = GetAdviceForReason(topReason.Key);
                            
                            // Особые советы для проблем со стабильностью
                            if (topReason.Key == FailureReasonType.EdgeCheck)
                            {
                                EditorGUILayout.HelpBox(
                                    $"⚠️ <b>ОСНОВНАЯ ПРОБЛЕМА: СТРОГАЯ ПРОВЕРКА СТАБИЛЬНОСТИ</b>\n" +
                                    $"Обнаружено {topReason.Value} отказов из-за неровной поверхности.\n\n" +
                                    $"💡 <b>Рекомендации:</b>\n" +
                                    $"• Уменьшите Edge Check Radius до 0.5-1 метра\n" +
                                    $"• Увеличьте Allowed Slope Angles до (0,45)\n" +
                                    $"• Уменьшите Slope Check Rays до 4 для скорости\n" +
                                    $"• Или отключите проверку стабильности (Edge Check Radius = 0)",
                                    MessageType.Warning);
                            }
                            else if (efficiency <= 0.025f)
                            {
                                EditorGUILayout.HelpBox(
                                    $"⚠️ <b>Спавнер не эффективен. Основная причина:</b>\n" +
                                    $"{GetFailureReasonName(topReason.Key)}\n" +
                                    $"💡 <b>Рекомендация:</b> {advice}",
                                    MessageType.Warning);
                            }
                        }
                    }
                    else if (efficiency <= 0.025f)
                    {
                        EditorGUILayout.Space(3f);
                        EditorGUILayout.HelpBox(
                            "⚠️ <b>Спавнер не эффективен. Рекомендации:</b>\n" +
                            "• Уменьшите avoidanceRadius и minDistanceBetweenObjects\n" +
                            "• Увеличить зону спавна\n" +
                            "• Проверить настройки collisionMask и avoidMask\n" +
                            "• Ослабить проверки стабильности поверхности",
                            MessageType.Warning);
                    }

                    // Советы по эффективности
                    if (efficiency > 0.7f)
                    {
                        EditorGUILayout.Space(3f);
                        EditorGUILayout.HelpBox("✅ <b>Отличная эффективность!</b> Настройки оптимальны.", MessageType.Info);
                    }
                    else if (efficiency > 0.3f)
                    {
                        EditorGUILayout.Space(3f);
                        EditorGUILayout.HelpBox("⚠️ <b>Средняя эффективность.</b> Можно улучшить настройки.", MessageType.Info);
                    }
                }

                if (spawner.debugRays != null && spawner.debugRays.Count > 0)
                {
                    EditorGUILayout.Space(3f);
                    EditorGUILayout.LabelField("🔦 Лучей отладки:", $"{spawner.debugRays.Count}");
                    
                    // Показываем статистику по типам лучей
                    var rayStats = spawner.debugRays.GroupBy(r => r.rayType)
                        .ToDictionary(g => g.Key, g => g.Count());
                    
                    EditorGUILayout.LabelField("📊 Статистика лучей:", EditorStyles.boldLabel);
                    foreach (var stat in rayStats)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"{GetRayTypeName(stat.Key)}:");
                        EditorGUILayout.LabelField($"{stat.Value}", GUILayout.Width(50));
                        EditorGUILayout.EndHorizontal();
                    }
                }
                
                if (spawner.settings.useClustering)
                {
                    EditorGUILayout.Space(3f);
                    int clusterCount = spawner.GetClusterCentersCount();
                    EditorGUILayout.LabelField("🎯 Кластеров создано:", $"{clusterCount}/{spawner.settings.clusterCount}");
                    
                    if (clusterCount < spawner.settings.clusterCount)
                    {
                        EditorGUILayout.Space(3f);
                        EditorGUILayout.HelpBox(
                            "⚠️ <b>Не удалось создать все кластеры. Решения:</b>\n" +
                            "• Увеличить размер зоны спавна\n" +
                            "• Уменьшить minDistanceBetweenClusters\n" +
                            "• Уменьшить clusterCount",
                            MessageType.Warning);
                    }
                }

                // Кнопка настроек отображения лучей
                EditorGUILayout.Space(3f);
                DrawDebugRaySettings(spawner, styleManager);

                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawDebugRaySettings(WorldGeneration spawner, EditorStyleManager styleManager)
        {
            EditorGUILayout.BeginVertical("box");
            
            showDebugRaySettings = EditorGUILayout.Foldout(showDebugRaySettings, "🔦 Настройки отображения лучей", styleManager.FoldoutStyle);
            
            if (showDebugRaySettings)
            {
                EditorGUI.indentLevel++;
                
                var debugSettings = spawner.settings.debugRaySettings;
                
                EditorGUI.BeginChangeCheck();
                
                debugSettings.enabled = EditorGUILayout.Toggle("Включить отображение лучей", debugSettings.enabled);
                
                if (debugSettings.enabled)
                {
                    EditorGUI.indentLevel++;
                    
                    debugSettings.showMainRays = EditorGUILayout.Toggle("Основные лучи", debugSettings.showMainRays);
                    debugSettings.showStabilityRays = EditorGUILayout.Toggle("Лучи стабильности", debugSettings.showStabilityRays);
                    debugSettings.showFloorRays = EditorGUILayout.Toggle("Лучи проверки пола", debugSettings.showFloorRays);
                    debugSettings.showAvoidanceRays = EditorGUILayout.Toggle("Лучи препятствий", debugSettings.showAvoidanceRays);
                    debugSettings.showCeilingRays = EditorGUILayout.Toggle("Лучи проверки потолка", debugSettings.showCeilingRays);
                    
                    EditorGUILayout.Space(3f);
                    
                    if (GUILayout.Button("Выбрать все", EditorStyles.miniButton))
                    {
                        debugSettings.showMainRays = true;
                        debugSettings.showStabilityRays = true;
                        debugSettings.showFloorRays = true;
                        debugSettings.showAvoidanceRays = true;
                        debugSettings.showCeilingRays = true;
                    }
                    
                    if (GUILayout.Button("Очистить все", EditorStyles.miniButton))
                    {
                        debugSettings.showMainRays = false;
                        debugSettings.showStabilityRays = false;
                        debugSettings.showFloorRays = false;
                        debugSettings.showAvoidanceRays = false;
                        debugSettings.showCeilingRays = false;
                    }
                    
                    EditorGUI.indentLevel--;
                }
                
                if (EditorGUI.EndChangeCheck())
                {
                    // Помечаем сцену как измененную для сохранения настроек
                    if (!Application.isPlaying)
                    {
                        UnityEditor.EditorUtility.SetDirty(spawner);
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
                    }
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }

        private string GetRayTypeName(DebugRayType rayType)
        {
            return rayType switch
            {
                DebugRayType.Main => "🎯 Основные лучи",
                DebugRayType.Stability => "🏔️ Лучи стабильности",
                DebugRayType.Floor => "🕳️ Лучи проверки пола",
                DebugRayType.Avoidance => "🚫 Лучи препятствий",
                DebugRayType.Ceiling => "🏠 Лучи проверки потолка",
                _ => "❓ Неизвестные лучи"
            };
        }

        private string GetFailureReasonName(FailureReasonType reason)
        {
            return reason switch
            {
                FailureReasonType.NoHit => "🎯 Рейкаст не попал",
                FailureReasonType.CeilingCheck => "🏠 Проверка потолка",
                FailureReasonType.EdgeCheck => "📐 Проверка стабильности",
                FailureReasonType.FloorCheck => "🕳️ Проверка пола",
                FailureReasonType.NearObstacle => "🚫 Рядом препятствие",
                FailureReasonType.InvalidLayer => "🏷️ Невалидный слой",
                FailureReasonType.OutOfBounds => "📏 Вне зоны спавна",
                FailureReasonType.TooCloseToOther => "🔗 Близко к другим объектам",
                FailureReasonType.ClusterFailed => "🎯 Ошибка кластеризации",
                _ => "❓ Неизвестная ошибка",
            };
        }

        private string GetAdviceForReason(FailureReasonType reason)
        {
            switch (reason)
            {
                case FailureReasonType.NoHit: 
                    return "Увеличьте высоту луча, проверьте Collision Mask";
                case FailureReasonType.CeilingCheck: 
                    return "Отключите checkCeiling или уменьшите высоту";
                case FailureReasonType.EdgeCheck: 
                    return "Уменьшите edgeCheckRadius, увеличьте allowedSlopeAngles или отключите проверку";
                case FailureReasonType.FloorCheck: 
                    return "Уменьшите floorCheckDistance или настройте avoidMask";
                case FailureReasonType.NearObstacle: 
                    return "Уменьшите avoidanceRadius или настройте avoidMask";
                case FailureReasonType.InvalidLayer: 
                    return "Проверьте collisionMask и avoidMask";
                case FailureReasonType.OutOfBounds: 
                    return "Увеличьте dimensions спавнера";
                case FailureReasonType.TooCloseToOther: 
                    return "Уменьшите minDistanceBetweenObjects";
                case FailureReasonType.ClusterFailed: 
                    return "Уменьшите clusterCount или minDistanceBetweenClusters";
                default: 
                    return "Проверьте настройки генерации";
            }
        }
    }
}
#endif