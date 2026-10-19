using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Resenha.Guardian.Editor.Core;
using UnityEditor;

namespace Resenha.Guardian.Editor.Scanners
{
    public class CodeAntiPatternsScanner : IGuardianScanner
    {
        public string Name => "Code Anti-Patterns";
        public string Description => "Analisa scripts C# em busca de chamadas caras no Update (Find, GetComponent, Camera.main) e alocações excessivas de GC.";
        public IssueCategory Category => IssueCategory.CodeQuality;
        public bool IsEnabled { get; set; } = true;

        private static readonly HashSet<string> FrameMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Update", "FixedUpdate", "LateUpdate", "OnGUI"
        };

        private static readonly (string Pattern, string Title, string Tip, IssueSeverity Severity)[] Rules = new[]
        {
            (
                @"FindObject(s)?OfType",
                "FindObjectOfType in Frame Loop",
                "FindObjectOfType searches the entire scene graph. Cache the reference in Awake/Start or inject via dependency.",
                IssueSeverity.Critical
            ),
            (
                @"GameObject\.Find(WithTag)?\(",
                "GameObject.Find in Frame Loop",
                "GameObject.Find is expensive in frame methods. Cache the reference or use direct serialized fields.",
                IssueSeverity.Critical
            ),
            (
                @"Camera\.main",
                "Camera.main in Frame Loop",
                "Camera.main internally calls FindGameObjectWithTag('MainCamera') in older Unity versions or has lookup overhead. Cache Camera reference in Awake.",
                IssueSeverity.Warning
            ),
            (
                @"GetComponent(InChildren|InParent)?\s*(<|\()",
                "GetComponent in Frame Loop",
                "GetComponent queries GameObject components every frame. Cache in Awake/Start to avoid per-frame native boundary overhead.",
                IssueSeverity.Warning
            ),
            (
                @"new\s+(List|Dictionary|HashSet)\s*<",
                "Collection Allocation in Frame Loop",
                "Instantiating collections inside frame loops triggers Garbage Collection spikes. Reuse persistent collection buffers.",
                IssueSeverity.Warning
            )
        };

        public IEnumerable<GuardianIssue> Scan(Action<float, string> onProgress)
        {
            var issues = new List<GuardianIssue>();

            string[] scriptGuids = AssetDatabase.FindAssets("t:MonoScript");
            int totalScripts = scriptGuids.Length;

            for (int i = 0; i < totalScripts; i++)
            {
                if (i % 25 == 0)
                {
                    onProgress?.Invoke((float)i / Math.Max(1, totalScripts), $"Auditing C# Scripts ({i}/{totalScripts})...");
                }

                string path = AssetDatabase.GUIDToAssetPath(scriptGuids[i]);
                if (string.IsNullOrEmpty(path) || !path.EndsWith(".cs")) continue;

                // Auditar exclusivamente scripts na pasta Assets/ do projeto (ignora Packages/)
                if (!path.StartsWith("Assets/")) continue;
                if (path.Contains("/ThirdParty/") || path.Contains("/Plugins/")) continue;

                AnalyzeScriptFile(path, issues);
            }

            return issues;
        }

        private void AnalyzeScriptFile(string path, List<GuardianIssue> issues)
        {
            if (!File.Exists(path)) return;

            string[] lines = File.ReadAllLines(path);
            string currentMethod = null;
            int methodBraceDepth = 0;

            var methodRegex = new Regex(@"(?:void|IEnumerator|async\s+Task)\s+([A-Za-z0-9_]+)\s*\(");

            for (int lineIdx = 0; lineIdx < lines.Length; lineIdx++)
            {
                string line = lines[lineIdx].Trim();

                // Ignorar comentários
                if (line.StartsWith("//") || line.StartsWith("/*") || line.StartsWith("*"))
                {
                    continue;
                }

                // Detectar declaração de método
                var match = methodRegex.Match(line);
                if (match.Success)
                {
                    string methodName = match.Groups[1].Value;
                    if (FrameMethods.Contains(methodName))
                    {
                        currentMethod = methodName;
                        methodBraceDepth = 0;
                    }
                }

                int openBraces = CountOccurrences(line, '{');
                int closeBraces = CountOccurrences(line, '}');

                if (currentMethod != null)
                {
                    methodBraceDepth += openBraces - closeBraces;

                    // Se estamos dentro do corpo do método
                    if (methodBraceDepth > 0)
                    {
                        foreach (var (pattern, title, tip, severity) in Rules)
                        {
                            if (Regex.IsMatch(line, pattern))
                            {
                                int lineNumber = lineIdx + 1;
                                issues.Add(new GuardianIssue
                                {
                                    Title = $"{title} [{currentMethod}]",
                                    Description = $"{tip}\nLine {lineNumber}: {line}",
                                    Severity = severity,
                                    Category = IssueCategory.CodeQuality,
                                    AssetPath = path,
                                    LineNumber = lineNumber
                                });
                            }
                        }
                    }
                    else if (methodBraceDepth <= 0 && closeBraces > 0)
                    {
                        // Saiu do método
                        currentMethod = null;
                        methodBraceDepth = 0;
                    }
                }
            }
        }

        private int CountOccurrences(string text, char target)
        {
            int count = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == target) count++;
            }
            return count;
        }
    }
}
