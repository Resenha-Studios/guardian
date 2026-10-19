using System;
using System.Collections.Generic;
using Resenha.Guardian.Editor.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Resenha.Guardian.Editor.Scanners
{
    public class MissingReferencesScanner : IGuardianScanner
    {
        public string Name => "Missing References";
        public string Description => "Detecta campos serializados que apontam para assets deletados ou referências nulas corrompidas.";
        public IssueCategory Category => IssueCategory.Integrity;
        public bool IsEnabled { get; set; } = true;

        public IEnumerable<GuardianIssue> Scan(Action<float, string> onProgress)
        {
            var issues = new List<GuardianIssue>();

            // 1. Prefabs
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            int totalPrefabs = prefabGuids.Length;

            for (int i = 0; i < totalPrefabs; i++)
            {
                if (i % 15 == 0)
                {
                    onProgress?.Invoke((float)i / Math.Max(1, totalPrefabs), $"Auditing Serialized Fields ({i}/{totalPrefabs})...");
                }

                string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                if (string.IsNullOrEmpty(path)) continue;
                // Auditar exclusivamente prefabs na pasta Assets/ (ignora Packages/)
                if (!path.StartsWith("Assets/")) continue;

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                ScanSerializedProperties(prefab, path, issues);
            }

            // 2. Cena Ativa
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.isLoaded)
            {
                GameObject[] rootObjects = activeScene.GetRootGameObjects();
                foreach (var root in rootObjects)
                {
                    ScanSerializedProperties(root, activeScene.path, issues);
                }
            }

            return issues;
        }

        private void ScanSerializedProperties(GameObject go, string assetPath, List<GuardianIssue> issues)
        {
            Component[] components = go.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp == null) continue;

                var so = new SerializedObject(comp);
                var prop = so.GetIterator();

                while (prop.NextVisible(true))
                {
                    if (prop.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        if (prop.objectReferenceValue == null && prop.objectReferenceInstanceIDValue != 0)
                        {
                            issues.Add(new GuardianIssue
                            {
                                Title = $"Missing Reference: {prop.displayName}",
                                Description = $"Field '{prop.propertyPath}' on component '{comp.GetType().Name}' in '{go.name}' has a missing reference target.",
                                Severity = IssueSeverity.Warning,
                                Category = IssueCategory.Integrity,
                                AssetPath = assetPath,
                                TargetObject = go
                            });
                        }
                    }
                }
            }

            for (int i = 0; i < go.transform.childCount; i++)
            {
                ScanSerializedProperties(go.transform.GetChild(i).gameObject, assetPath, issues);
            }
        }
    }
}
