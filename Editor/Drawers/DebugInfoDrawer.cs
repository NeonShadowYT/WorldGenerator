#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.SceneManagement;

namespace NeonImperium.WorldGeneration
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

                    if (spawner.FailureStatistics != null && spawner.FailureStatistics.Count > 0)
                    {
                        EditorGUILayout.Space(3f);
                        EditorGUILayout.LabelField("❌ Причины ошибок:", EditorStyles.boldLabel);
                        
                        List<KeyValuePair<FailureReasonType, int>> sortedReasons = new List<KeyValuePair<FailureReasonType, int>>();
                        foreach (KeyValuePair<FailureReasonType, int> kvp in spawner.FailureStatistics)
                        {
                            sortedReasons.Add(kvp);
                        }
                        
                        for (int i = 0; i < sortedReasons.Count - 1; i++)
                        {
                            for (int j = 0; j < sortedReasons.Count - i - 1; j++)
                            {
                                if (sortedReasons[j].Value < sortedReasons[j + 1].Value)
                                {
                                    KeyValuePair<FailureReasonType, int> temp = sortedReasons[j];
                                    sortedReasons[j] = sortedReasons[j + 1];
                                    sortedReasons[j + 1] = temp;
                                }
                            }
                        }

                        for (int i = 0; i < sortedReasons.Count; i++)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField($"{GetFailureReasonName(sortedReasons[i].Key)}:");
                            EditorGUILayout.LabelField($"{sortedReasons[i].Value}", GUILayout.Width(50));
                            EditorGUILayout.EndHorizontal();
                        }

                        if (sortedReasons.Count > 0)
                        {
                            KeyValuePair<FailureReasonType, int> topReason = sortedReasons[0];
                            if (topReason.Value > 0)
                            {
                                EditorGUILayout.Space(3f);
                                string advice = GetAdviceForReason(topReason.Key);
                                
                                if (topReason.Key == FailureReasonType.EdgeCheck)
                                {
                                    EditorGUILayout.HelpBox(
                                        $"⚠️ <b>ОСНОВНАЯ ПРОБЛЕМА: СТРОГАЯ ПРОВЕРКА СТАБИЛЬНОСТИ</b>\n" +
                                        $"Обнаружено {topReason.Value} отказов из-за неровной поверхности.\n\n" +
                                        $"💡 <b>Рекомендации:</b>\n" +
                                        $"• Уменьшите edgeCheckRadius до 0.5-1 метра\n" +
                                        $"• Увеличьте maxHeightDifference до 0.3-0.5 метра\n" +
                                        $"• Уменьшите stabilityCheckRays до 4 для скорости\n" +
                                        $"• Или отключите проверку стабильности (edgeCheckRadius = 0)",
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
                    
                    Dictionary<DebugRayType, int> rayStats = new Dictionary<DebugRayType, int>();
                    for (int i = 0; i < spawner.debugRays.Count; i++)
                    {
                        DebugRayType rayType = spawner.debugRays[i].rayType;
                        if (rayStats.ContainsKey(rayType))
                        {
                            rayStats[rayType]++;
                        }
                        else
                        {
                            rayStats[rayType] = 1;
                        }
                    }
                    
                    EditorGUILayout.LabelField("📊 Статистика лучей:", EditorStyles.boldLabel);
                    foreach (KeyValuePair<DebugRayType, int> stat in rayStats)
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
                DebugRaySettings debugSettings = spawner.settings.debugRaySettings;
                
                EditorGUI.BeginChangeCheck();
                
                debugSettings.enabled = EditorGUILayout.Toggle("enabled", debugSettings.enabled);
                
                if (debugSettings.enabled)
                {
                    debugSettings.showMainRays = EditorGUILayout.Toggle("showMainRays", debugSettings.showMainRays);
                    debugSettings.showStabilityRays = EditorGUILayout.Toggle("showStabilityRays", debugSettings.showStabilityRays);
                    debugSettings.showFloorRays = EditorGUILayout.Toggle("showFloorRays", debugSettings.showFloorRays);
                    debugSettings.showAvoidanceRays = EditorGUILayout.Toggle("showAvoidanceRays", debugSettings.showAvoidanceRays);
                    debugSettings.showCeilingRays = EditorGUILayout.Toggle("showCeilingRays", debugSettings.showCeilingRays);
                    
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
                }
                
                if (EditorGUI.EndChangeCheck())
                {
                    if (!Application.isPlaying)
                    {
                        EditorUtility.SetDirty(spawner);
                        EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
                    }
                }
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
                    return "Уменьшите edgeCheckRadius, увеличьте maxHeightDifference или отключите проверку";
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