#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NeonImperium.WorldGenerations
{
    /// <summary>Драйвер редакторной генерации. Хранит WeakReference и тикает активные спавнеры.</summary>
    public static class EditorGenerationDriver
    {
        private static readonly List<WeakReference<WorldGeneration>> _active = new();
        private static bool _subscribed;

        public static void Start(WorldGeneration spawner)
        {
            if (spawner == null) return;

            for (int i = 0; i < _active.Count; i++)
                if (_active[i].TryGetTarget(out WorldGeneration existing) && existing == spawner) return;

            _active.Add(new WeakReference<WorldGeneration>(spawner));
            EnsureSubscribed();
        }

        public static void Stop(WorldGeneration spawner)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (!_active[i].TryGetTarget(out WorldGeneration t) || t == null || t == spawner)
                    _active.RemoveAt(i);
            }
            if (_active.Count == 0) Unsubscribe();
        }

        private static void EnsureSubscribed()
        {
            if (_subscribed) return;
            EditorApplication.update += Tick;
            SceneView.duringSceneGui += OnSceneGUI;
            _subscribed = true;
        }

        private static void Unsubscribe()
        {
            if (!_subscribed) return;
            EditorApplication.update -= Tick;
            SceneView.duringSceneGui -= OnSceneGUI;
            _subscribed = false;
        }

        private static void Tick()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (!_active[i].TryGetTarget(out WorldGeneration spawner) || spawner == null)
                {
                    _active.RemoveAt(i);
                    continue;
                }

                if (!spawner.IsGenerating) continue;

                // Изолируем исключения: сбой одного спавнера не блокирует тики остальных.
                try
                {
                    spawner.EditorTick();
                }
                catch (Exception e)
                {
                    Debug.LogException(e, spawner);
                    _active.RemoveAt(i);
                }
            }

            if (_active.Count == 0) Unsubscribe();
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i].TryGetTarget(out WorldGeneration spawner) && spawner != null && spawner.IsGenerating)
                {
                    sceneView.Repaint();
                    return;
                }
            }
        }
    }
}
#endif