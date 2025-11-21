#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

namespace NeonImperium
{
    public class ActionButtonsDrawer
    {
        public void DrawActionButtons(WorldGenerationView view, UnityEngine.Object[] targets, 
            ref bool isRegeneratingAll, ref int currentSpawnerIndex, ref int totalSpawners, ref List<WorldGeneration> allSpawners)
        {
            bool anyGenerating = targets.Cast<WorldGeneration>().Any(s => s.IsGenerating) || isRegeneratingAll;
            bool anyHaveObjects = targets.Cast<WorldGeneration>().Any(s => s.transform.childCount > 0);

            bool masksConflict = targets.Cast<WorldGeneration>()
                .Any(s => s.settings.avoidMask.value == s.settings.collisionMask.value && 
                         s.settings.avoidMask.value != 0);
            
            if (masksConflict)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.HelpBox(
                    "🚫 <b>Невозможно сгенерировать: полный конфликт масок!</b>\n" +
                    "Collision Mask и Avoid Mask содержат одинаковые слои.",
                    MessageType.Error
                );
                EditorGUILayout.EndVertical();
                GUI.enabled = false;
            }
            
            EditorGUILayout.BeginHorizontal();
            {
                if (anyGenerating)
                {
                    if (DrawButton(" ⏹️ Остановить", "d_PreMatQuad", "Прервать текущую генерацию")) 
                        view.CancelAllGenerations();
                }
                else
                {
                    if (DrawButton(" 🎲 Сгенерировать", "d_Toolbar Plus", "Начать создание объектов")) 
                        view.GenerateSelected();
                    if (anyHaveObjects && DrawButton(" 🗑️ Очистить", "d_TreeEditor.Trash", "Удалить все созданные объекты")) 
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
                if (DrawButton(" 🔄 Полная перегенерация", "d_PreMatCube", "Перегенерировать все спавнеры на сцене"))
                {
                    view.RegenerateAllSpawners();
                }
            }
            
            EditorGUILayout.Space(4f);
        }
        
        private bool DrawButton(string text, string iconName, string tooltip)
        {
            var style = new GUIStyle(EditorStyles.miniButton)
            {
                padding = new RectOffset(10, 10, 5, 5),
                fixedHeight = 30,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };
            
            return GUILayout.Button(
                new GUIContent(text, EditorGUIUtility.IconContent(iconName).image, tooltip), 
                style
            );
        }
    }
}
#endif