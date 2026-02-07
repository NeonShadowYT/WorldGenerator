#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace NeonImperium.WorldGeneration
{
    [CustomEditor(typeof(WorldGeneration))]
    [CanEditMultipleObjects]
    public partial class WorldGenerationView : Editor
    {
        private EditorStyleManager _styleManager;
        private SectionDrawer _sectionDrawer;
        private ActionButtonsDrawer _actionDrawer;
        private DebugInfoDrawer _debugDrawer;
        
        private bool _showDebugInfo, _showHelpBoxes;

        private bool _isRegeneratingAll;
        private int _currentSpawnerIndex;
        private int _totalSpawners;
        private List<WorldGeneration> _allSpawners = new();

        private void OnEnable()
        {
            _styleManager = new EditorStyleManager();
            _sectionDrawer = new SectionDrawer(_styleManager);
            _actionDrawer = new ActionButtonsDrawer();
            _debugDrawer = new DebugInfoDrawer();
        }

        private void OnDestroy()
        {
            EditorApplication.update -= RegenerateAllUpdate;
        }

        public override void OnInspectorGUI()
        {
            _styleManager.InitializeStyles();
            serializedObject.Update();

            SerializedProperty settingsProp = serializedObject.FindProperty("settings");
            Color headerColor = settingsProp.FindPropertyRelative("gizmoColor").colorValue;
            _styleManager.UpdateStyles(headerColor);

            DrawHeaders();
            DrawDisplaySettings();
            DrawSettings();
            _actionDrawer.DrawActionButtons(this, targets, ref _isRegeneratingAll, ref _currentSpawnerIndex, ref _totalSpawners, ref _allSpawners);
            _debugDrawer.DrawDebugInfo(target as WorldGeneration, ref _showDebugInfo, _styleManager);

            serializedObject.ApplyModifiedProperties();
            if (_isRegeneratingAll) Repaint();
        }

        private void DrawHeaders()
        {
            Rect rect = EditorGUILayout.BeginVertical();
            GUI.DrawTexture(rect, _styleManager.SolidColorTexture, ScaleMode.StretchToFill);
            EditorGUILayout.LabelField("🔄 Продвинутый генератор", _styleManager.HeaderStyle);
            EditorGUILayout.LabelField("Neon Shadow && DeepSeek", _styleManager.CenteredLabelStyle);
            EditorGUILayout.EndVertical();
        }

        private void DrawDisplaySettings()
        {
            EditorGUILayout.BeginVertical("box");
            
            bool newHelpState = GUILayout.Toggle(_showHelpBoxes, 
                new GUIContent(" 📚 Показать подсказки", "Включает/выключает подробные подсказки и советы"), 
                EditorStyles.miniButton, GUILayout.Height(22));
            
            if (newHelpState != _showHelpBoxes)
            {
                _showHelpBoxes = newHelpState;
            }

            if (_showHelpBoxes)
            {
                EditorGUILayout.Space(3f);
                EditorGUILayout.BeginVertical(_styleManager.HelpBoxStyle);
                EditorGUILayout.LabelField("💡 <b>Режим подсказок активен</b>. Наводите курсор на названия настроек для получения информации.", _styleManager.MiniLabelStyle);
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(4f);
        }

        private void DrawSettings()
        {
            SerializedProperty settings = serializedObject.FindProperty("settings");
            WorldGeneration spawner = target as WorldGeneration;
            
            _sectionDrawer.DrawSpawnSettings(settings, _showHelpBoxes, targets, spawner);
            _sectionDrawer.DrawClusteringSettings(settings, _showHelpBoxes, spawner);
            _sectionDrawer.DrawRaySettings(settings, _showHelpBoxes, spawner);
            _sectionDrawer.DrawStabilitySettings(settings, _showHelpBoxes, spawner);
            _sectionDrawer.DrawAvoidanceSettings(settings, _showHelpBoxes, spawner);
        }

        public void GenerateSelected() => ProcessSpawners(s => s.GenerateObjects());
        public void ClearSelected() => ProcessSpawners(s => s.ClearAll());
        
        private void ProcessSpawners(System.Action<WorldGeneration> action)
        {
            UnityEngine.Object[] targetArray = targets;
            for (int i = 0; i < targetArray.Length; i++)
            {
                WorldGeneration spawner = targetArray[i] as WorldGeneration;
                action(spawner);
                MarkSceneDirty(spawner.gameObject);
            }
        }

        public void CancelAllGenerations()
        {
            if (_isRegeneratingAll)
            {
                _isRegeneratingAll = false;
                EditorApplication.update -= RegenerateAllUpdate;
            }
            ClearSelected();
        }

        public void RegenerateAllSpawners()
        {
            _allSpawners = new List<WorldGeneration>(FindObjectsByType<WorldGeneration>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            
            for (int i = 0; i < _allSpawners.Count - 1; i++)
            {
                for (int j = 0; j < _allSpawners.Count - i - 1; j++)
                {
                    if (_allSpawners[j].settings.priority < _allSpawners[j + 1].settings.priority)
                    {
                        (_allSpawners[j + 1], _allSpawners[j]) = (_allSpawners[j], _allSpawners[j + 1]);
                    }
                }
            }

            _totalSpawners = _allSpawners.Count;
            _currentSpawnerIndex = 0;
            _isRegeneratingAll = true;

            for (int i = 0; i < _allSpawners.Count; i++)
            {
                _allSpawners[i].ClearAll();
            }

            EditorApplication.update += RegenerateAllUpdate;
        }

        private void RegenerateAllUpdate()
        {
            if (_currentSpawnerIndex >= _totalSpawners)
            {
                EditorApplication.update -= RegenerateAllUpdate;
                _isRegeneratingAll = false;
                Debug.Log($"✅ Перегенерация всех спавнеров завершена: {_totalSpawners} объектов");
                return;
            }

            WorldGeneration currentSpawner = _allSpawners[_currentSpawnerIndex];
            
            if (!currentSpawner.IsGenerating && currentSpawner.SpawnedCount == 0)
            {
                currentSpawner.GenerateObjects();
            }
            else if (!currentSpawner.IsGenerating)
            {
                _currentSpawnerIndex++;
            }
        }

        private void MarkSceneDirty(GameObject obj)
        {
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(obj);
                EditorSceneManager.MarkSceneDirty(obj.scene);
            }
        }
    }
}
#endif