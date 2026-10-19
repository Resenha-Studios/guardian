using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Resenha.Guardian.Editor.Core;
using UnityEditor;

namespace Resenha.Guardian.Editor.Scanners
{
    public class LocalizationComplianceScanner : IGuardianScanner
    {
        public string Name => "Localization Compliance";
        public string Description => "Verifica se elementos visuais e interfaces de runtime estão utilizando LocalizedString em vez de textos hardcoded.";
        public IssueCategory Category => IssueCategory.Localization;
        public bool IsEnabled { get; set; } = true;

        private static readonly HashSet<string> IgnoredTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Teclas e Atalhos
            "ESC", "SHIFT", "CTRL", "ALT", "TAB", "SPACE", "ENTER", "DEL", "CMD",
            // Códigos de Idioma
            "EN", "PT", "ES", "JA", "ZH", "DE", "FR", "IT",
            // Badges e Notas
            "A+", "A", "B", "C", "D", "F", "OK", "WIP",
            // Símbolos
            "📋", "+", "-", "--", "---", "•", "...", "---"
        };

        public IEnumerable<GuardianIssue> Scan(Action<float, string> onProgress)
        {
            var issues = new List<GuardianIssue>();

            string[] uxmlGuids = AssetDatabase.FindAssets("t:VisualTreeAsset");
            int totalUxml = uxmlGuids.Length;

            for (int i = 0; i < totalUxml; i++)
            {
                if (i % 5 == 0)
                {
                    onProgress?.Invoke((float)i / Math.Max(1, totalUxml), $"Auditing UI Toolkit UXML ({i}/{totalUxml})...");
                }

                string path = AssetDatabase.GUIDToAssetPath(uxmlGuids[i]);
                if (string.IsNullOrEmpty(path)) continue;
                // Auditar exclusivamente a pasta Assets/ do projeto (ignora todos os Packages/)
                if (!path.StartsWith("Assets/")) continue;
                if (path.Contains("/Editor/")) continue;

                AnalyzeUxmlFile(path, issues);
            }

            return issues;
        }

        private void AnalyzeUxmlFile(string path, List<GuardianIssue> issues)
        {
            if (!File.Exists(path)) return;

            string[] lines = File.ReadAllLines(path);
            var hardcodedTextRegex = new Regex(@"\btext=""([^""{][^""]*)""");

            for (int lineIdx = 0; lineIdx < lines.Length; lineIdx++)
            {
                string line = lines[lineIdx];
                var match = hardcodedTextRegex.Match(line);

                if (match.Success)
                {
                    string textVal = match.Groups[1].Value.Trim();
                    if (IsHardcodedEligible(textVal))
                    {
                        issues.Add(new GuardianIssue
                        {
                            Title = $"Hardcoded UI Text: \"{textVal}\"",
                            Description = $"Visual element in UXML defines literal text \"{textVal}\". Use LocalizedString to support all 5 project languages (EN, PT, ES, JA, ZH).",
                            Severity = IssueSeverity.Suggestion,
                            Category = IssueCategory.Localization,
                            AssetPath = path,
                            LineNumber = lineIdx + 1
                        });
                    }
                }
            }
        }

        private bool IsHardcodedEligible(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            if (text.Length <= 1) return false;

            // Tokens estáticos ignorados (teclas, códigos de língua, etc.)
            if (IgnoredTokens.Contains(text)) return false;

            // Se for número puro, fração ou porcentagem (ex: "100%", "100 / 100", "0/0", "1.0.0", "v1.0.0")
            string cleaned = text.Replace("%", "").Replace("/", "").Replace(".", "").Replace("v", "").Trim();
            if (double.TryParse(cleaned, out _)) return false;

            // Placeholders com contadores (ex: "All (0)", "Critical (0)", "0 issues detected", "24h 18m", "LVL 42", "FILE 1")
            if (Regex.IsMatch(text, @"^.*\(\d+\)$")) return false;
            if (Regex.IsMatch(text, @"^\d+h\s*\d+m$")) return false;
            if (Regex.IsMatch(text, @"^(LVL|LEVEL|FILE|SLOT)\s*\d+$", RegexOptions.IgnoreCase)) return false;
            if (Regex.IsMatch(text, @"^\d+\s*issues\s*detected$", RegexOptions.IgnoreCase)) return false;

            // Placeholders de pontuação
            if (text.StartsWith("...") || text.StartsWith("---") || text.StartsWith("///")) return false;

            return true;
        }
    }
}
