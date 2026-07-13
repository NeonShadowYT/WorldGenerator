#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NeonImperium.WorldGenerations
{
    public class ActionButtonsDrawer
    {
        public void DrawActionButtons(WorldGenerationView view, Object[] targets, 
            ref bool isRegeneratingAll, ref int currentSpawnerIndex, ref int totalSpawners, ref List<WorldGeneration> allSpawners,
            EditorStyleManager styleManager)
        {
            bool anyGenerating = false;
            bool anyHaveObjects = false;
            bool masksConflict = false;

            for (int i = 0; i < targets.Length; i++)
            {
                WorldGeneration spawner = targets[i] as WorldGeneration;
                if (spawner.IsGenerating) anyGenerating = true;
                if (spawner.transform.childCount > 0) anyHaveObjects = true;
                if (spawner.settings.avoidMask.value == spawner.settings.collisionMask.value && 
                    spawner.settings.avoidMask.value != 0)
                {
                    masksConflict = true;
                }
            }
            
            if (masksConflict)
            {
                EditorGUILayout.BeginVertical("box");
                // Кастомный блок с rich-текстом вместо HelpBox
                GUIStyle labelStyle = new(EditorStyles.label)
                {
                    wordWrap = true,
                    richText = true,
                    fontSize = 11
                };
                EditorGUILayout.LabelField(
                    "🚫 <b>Невозможно сгенерировать: полный конфликт масок!</b>\n" +
                    "Collision Mask и Avoid Mask содержат одинаковые слои.",
                    labelStyle
                );
                EditorGUILayout.EndVertical();
                GUI.enabled = false;
            }
            
            EditorGUILayout.BeginHorizontal();
            {
                if (anyGenerating)
                {
                    if (DrawButton(" ⏹️ Остановить", "Прервать текущую генерацию")) 
                        view.CancelAllGenerations();
                }
                else
                {
                    if (DrawButton(" 🎲 Сгенерировать", "Начать создание объектов")) 
                        view.GenerateSelected();
                    if (anyHaveObjects && DrawButton(" 🗑️ Очистить", "Удалить все созданные объекты")) 
                        view.ClearSelected();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (masksConflict) GUI.enabled = true;
            
            if (isRegeneratingAll)
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.BeginVertical("box");
                float progress = (float)currentSpawnerIndex / totalSpawners;
                Rect rect = EditorGUILayout.GetControlRect(false, 20);
                
                string spawnerName = currentSpawnerIndex < totalSpawners 
                    ? allSpawners[currentSpawnerIndex].name 
                    : "Завершение...";
                
                EditorGUI.ProgressBar(rect, progress, 
                    $"🔄 Перегенерация: {currentSpawnerIndex}/{totalSpawners} ({progress:P0}) - {spawnerName}");
                EditorGUILayout.EndVertical();
            }
            else if (!anyGenerating)
            {
                EditorGUILayout.Space(4f);
                if (DrawButton(" 🔄 Полная перегенерация", "Перегенерировать все спавнеры на сцене"))
                {
                    view.RegenerateAllSpawners();
                }
            }
            
            EditorGUILayout.Space(4f);
        }
        
        private bool DrawButton(string text, string tooltip)
        {
            GUIStyle style = new(EditorStyles.miniButton)
            {
                padding = new RectOffset(10, 10, 5, 5),
                fixedHeight = 30,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };
            
            return GUILayout.Button(
                new GUIContent(text, tooltip), 
                style
            );
        }
    }
}
#endif