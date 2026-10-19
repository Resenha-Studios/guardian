using System;
using System.Collections.Generic;
using Resenha.Guardian.Editor.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Resenha.Guardian.Editor.Scanners
{
    public class MissingScriptsScanner : IGuardianScanner
    {
        public string Name => "Missing Scripts";
        public string Description => "Detecta GameObjects com componentes MonoBehaviour corrompidos ou excluídos.";
        public IssueCategory Category => IssueCategory.Integrity;
        public bool IsEnabled { get; set; } = true;

        public IEnumerable<GuardianIssue> Scan(Action<float, string> onProgress)
        {
            var issues = new List<GuardianIssue>();

            // 1. Escanear Prefabs no projeto
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            int totalPrefabs = prefabGuids.Length;

            for (int i = 0; i < totalPrefabs; i++)
            {
                if (i % 10 == 0)
                {
                    onProgress?.Invoke((float)i / Math.Max(1, totalPrefabs), $"Auditing Prefabs ({i}/{totalPrefabs})...");
                }

                string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                if (string.IsNullOrEmpty(path)) continue;

                // Auditar exclusivamente prefabs na pasta Assets/ (ignora Packages/)
                if (!path.StartsWith("Assets/")) continue;

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                ScanGameObjectHierarchy(prefab, path, issues, isPrefab: true);
            }

            // 2. Escanear Cena Ativa
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.isLoaded)
            {
                onProgress?.Invoke(0.9f, $"Auditing Active Scene: {activeScene.name}...");
                GameObject[] rootObjects = activeScene.GetRootGameObjects();
                foreach (var root in rootObjects)
                {
                    ScanGameObjectHierarchy(root, activeScene.path, issues, isPrefab: false);
                }
            }

            return issues;
        }

        private void ScanGameObjectHierarchy(GameObject go, string assetPath, List<GuardianIssue> issues, bool isPrefab)
        {
            Component[] components = go.GetComponents<Component>();
            for (int c = 0; c < components.Length; c++)
            {
                if (components[c] == null)
                {
                    var targetObj = go;
                    var issue = new GuardianIssue
                    {
                        Title = $"Missing Script on '{go.name}'",
                        Description = $"GameObject '{go.name}' contains a missing or corrupted MonoBehaviour reference.",
                        Severity = IssueSeverity.Critical,
                        Category = IssueCategory.Integrity,
                        AssetPath = assetPath,
                        TargetObject = targetObj,
                        CanQuickFix = true,
                        QuickFixLabel = "Remove Missing Component",
                        QuickFixAction = () =>
                        {
                            if (targetObj != null)
                            {
                                Undo.RegisterCompleteObjectUndo(targetObj, "Remove Missing Scripts");
                                int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(targetObj);
                                if (isPrefab && !string.IsNullOrEmpty(assetPath))
                                {
                                    PrefabUtility.SavePrefabAsset(targetObj);
                                }
                                Debug.Log($"[Guardian] Removed {count} missing script(s) from '{targetObj.name}'.");
                            }
                        }
                    };
                    issues.Add(issue);
                }
            }

            for (int i = 0; i < go.transform.childCount; i++)
            {
                ScanGameObjectHierarchy(go.transform.GetChild(i).gameObject, assetPath, issues, isPrefab);
            }
        }
    }
}
