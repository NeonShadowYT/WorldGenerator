#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace NeonImperium.WorldGenerations
{
    [CustomEditor(typeof(WorldGeneration))]
    [CanEditMultipleObjects]
    public class WorldGenerationView : Editor
    {
        private const int MaxPreviewDraw = 500;

        private EditorStyleManager _styleManager;
        private SectionDrawer _sectionDrawer;
        private ActionButtonsDrawer _actionDrawer;
        private DebugInfoDrawer _debugDrawer;

        private bool _showDebugInfo, _showHelpBoxes;

        private bool _isRegeneratingAll;
        private int _currentSpawnerIndex;
        private int _totalSpawners;
        private List<WorldGeneration> _allSpawners = new();

        private static readonly List<WorldGeneration> _pendingNavMeshSpawners = new();
        private static bool _navMeshUpdateSubscribed;

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

        /// <summary>Интерактивное редактирование зон + отрисовка preview для выделенных спавнеров.</summary>
        private void OnSceneGUI()
        {
            if (Application.isPlaying) return;

            GameObject[] selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0) return;

            for (int i = 0; i < selected.Length; i++)
            {
                GameObject go = selected[i];
                if (go == null) continue;

                WorldGeneration spawner = go.GetComponent<WorldGeneration>();
                if (spawner == null) continue;

                SpawnZoneSceneEditor.DrawForSpawner(spawner);
                DrawPreview(spawner);
            }
        }

        /// <summary>Рисует точки PreviewCache. Валидные — зелёные, невалидные — красные (если включено).</summary>
        private static void DrawPreview(WorldGeneration spawner)
        {
            IReadOnlyList<PlacementData> cache = spawner.PreviewCache;
            if (cache == null || cache.Count == 0) return;

            SpawnSettings s = spawner.settings;
            int limit = cache.Count < MaxPreviewDraw ? cache.Count : MaxPreviewDraw;

            for (int i = 0; i < limit; i++)
            {
                PlacementData p = cache[i];
                if (!p.isValid && !s.previewShowInvalid) continue;

                Handles.color = p.isValid ? s.previewValidColor : s.previewInvalidColor;
                float radius = HandleUtility.GetHandleSize(p.position) * 0.3f;
                Handles.SphereHandleCap(0, p.position, Quaternion.identity, radius, EventType.Repaint);
            }
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
            _actionDrawer.DrawActionButtons(this, targets, ref _isRegeneratingAll,
                ref _currentSpawnerIndex, ref _totalSpawners, ref _allSpawners, _styleManager);
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

            if (newHelpState != _showHelpBoxes) _showHelpBoxes = newHelpState;

            if (_showHelpBoxes)
            {
                EditorGUILayout.Space(3f);
                EditorGUILayout.BeginVertical(_styleManager.HelpBoxStyle);
                EditorGUILayout.LabelField("💡 <b>Режим подсказок активен</b>. Наводите курсор на названия настроек для получения информации.",
                    _styleManager.MiniLabelStyle);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        private void DrawSettings()
        {
            SerializedProperty settings = serializedObject.FindProperty("settings");
            SerializedProperty seedProp = serializedObject.FindProperty("_seed");
            WorldGeneration spawner = target as WorldGeneration;

            _sectionDrawer.DrawSpawnSettings(settings, seedProp, _showHelpBoxes, targets, spawner);
            _sectionDrawer.DrawSpawnZoneSettings(settings, _showHelpBoxes, spawner);
            _sectionDrawer.DrawClusteringSettings(settings, _showHelpBoxes, spawner);
            _sectionDrawer.DrawRaySettings(settings, _showHelpBoxes, spawner);
            _sectionDrawer.DrawStabilitySettings(settings, _showHelpBoxes, spawner);
            _sectionDrawer.DrawAvoidanceSettings(settings, _showHelpBoxes, spawner);
            _sectionDrawer.DrawRuntimeSettings(settings, _showHelpBoxes, spawner);
            _sectionDrawer.DrawNavMeshSettings(settings, _showHelpBoxes, spawner);
        }

        public void GenerateSelected()
        {
            bool anyRequestNavMesh = false;
            Object[] targetArray = targets;

            for (int i = 0; i < targetArray.Length; i++)
            {
                WorldGeneration spawner = targetArray[i] as WorldGeneration;
                if (spawner.settings.requestNavMeshRegeneration) anyRequestNavMesh = true;
                spawner.Generate();
                MarkSceneDirty(spawner.gameObject);
            }

            if (anyRequestNavMesh)
            {
                WorldGeneration first = target as WorldGeneration;
                NavMeshRegenerationMode mode = first != null
                    ? first.settings.navMeshRegenerationMode
                    : NavMeshRegenerationMode.Never;

                if (mode == NavMeshRegenerationMode.OnSingleGeneration ||
                    mode == NavMeshRegenerationMode.Always)
                {
                    ScheduleNavMeshRegenAfter(targetArray);
                }
            }
        }

        public void ClearSelected()
        {
            Object[] targetArray = targets;
            for (int i = 0; i < targetArray.Length; i++)
            {
                WorldGeneration spawner = targetArray[i] as WorldGeneration;
                spawner.ClearAll();
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
            _allSpawners = new List<WorldGeneration>(FindObjectsByType<WorldGeneration>(FindObjectsInactive.Include));

            for (int i = 0; i < _allSpawners.Count - 1; i++)
            {
                for (int j = 0; j < _allSpawners.Count - i - 1; j++)
                {
                    if (_allSpawners[j].settings.priority < _allSpawners[j + 1].settings.priority)
                        (_allSpawners[j + 1], _allSpawners[j]) = (_allSpawners[j], _allSpawners[j + 1]);
                }
            }

            _totalSpawners = _allSpawners.Count;
            _currentSpawnerIndex = 0;
            _isRegeneratingAll = true;

            for (int i = 0; i < _allSpawners.Count; i++)
                _allSpawners[i].ClearAll();

            EditorApplication.update += RegenerateAllUpdate;
        }

        private void RegenerateAllUpdate()
        {
            if (_currentSpawnerIndex >= _totalSpawners)
            {
                EditorApplication.update -= RegenerateAllUpdate;
                _isRegeneratingAll = false;

                TryRegenerateNavMeshOnFullComplete();

                Debug.Log($"✅ Перегенерация всех спавнеров завершена: {_totalSpawners} объектов");
                return;
            }

            WorldGeneration current = _allSpawners[_currentSpawnerIndex];

            if (!current.IsGenerating && current.SpawnedCount == 0)
                current.Generate();
            else if (!current.IsGenerating)
                _currentSpawnerIndex++;
        }

        private void TryRegenerateNavMeshOnFullComplete()
        {
            NavMeshRegenerationMode mode = NavMeshRegenerationMode.Never;

            for (int i = 0; i < _allSpawners.Count; i++)
            {
                if (_allSpawners[i] == null) continue;
                mode = _allSpawners[i].settings.navMeshRegenerationMode;
                break;
            }

            if (mode != NavMeshRegenerationMode.OnFullRegeneration &&
                mode != NavMeshRegenerationMode.Always)
                return;

            for (int i = 0; i < _allSpawners.Count; i++)
            {
                if (_allSpawners[i] != null && _allSpawners[i].settings.requestNavMeshRegeneration)
                {
                    NavMeshRegenerationService.RegenerateAll();
                    return;
                }
            }
        }

        private void ScheduleNavMeshRegenAfter(Object[] spawners)
        {
            _pendingNavMeshSpawners.Clear();
            for (int i = 0; i < spawners.Length; i++)
            {
                WorldGeneration s = spawners[i] as WorldGeneration;
                if (s != null && s.settings.requestNavMeshRegeneration)
                    _pendingNavMeshSpawners.Add(s);
            }

            if (_pendingNavMeshSpawners.Count == 0) return;

            if (!_navMeshUpdateSubscribed)
            {
                EditorApplication.update += PendingNavMeshUpdate;
                _navMeshUpdateSubscribed = true;
            }
        }

        private static void PendingNavMeshUpdate()
        {
            for (int i = _pendingNavMeshSpawners.Count - 1; i >= 0; i--)
            {
                WorldGeneration s = _pendingNavMeshSpawners[i];
                if (s == null || !s.IsGenerating)
                    _pendingNavMeshSpawners.RemoveAt(i);
            }

            if (_pendingNavMeshSpawners.Count > 0) return;

            NavMeshRegenerationService.RegenerateAll();
            EditorApplication.update -= PendingNavMeshUpdate;
            _navMeshUpdateSubscribed = false;
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