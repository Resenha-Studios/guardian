using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Resenha.Guardian.Editor.Core
{
    [Serializable]
    public class GuardianScanResult
    {
        public List<GuardianIssue> Issues { get; } = new List<GuardianIssue>();
        public double ScanDurationSeconds { get; set; }
        public int TotalFilesScanned { get; set; }
        public DateTime ScanTimestamp { get; set; } = DateTime.Now;

        public int CriticalCount => Issues.Count(i => i.Severity == IssueSeverity.Critical);
        public int WarningCount => Issues.Count(i => i.Severity == IssueSeverity.Warning);
        public int SuggestionCount => Issues.Count(i => i.Severity == IssueSeverity.Suggestion);

        public float HealthScore
        {
            get
            {
                if (Issues.Count == 0) return 100f;
                // Penalidade ponderada com tetos calibrados
                float critPenalty = CriticalCount * 20f;
                float warnPenalty = Math.Min(30f, WarningCount * 5f);
                float suggPenalty = Math.Min(15f, SuggestionCount * 0.2f);

                float totalPenalty = critPenalty + warnPenalty + suggPenalty;
                return Math.Max(0f, Math.Min(100f, 100f - totalPenalty));
            }
        }

        public string Grade
        {
            get
            {
                float score = HealthScore;
                if (score >= 95f) return "A+";
                if (score >= 85f) return "A";
                if (score >= 70f) return "B";
                if (score >= 50f) return "C";
                return "F";
            }
        }

        public string ToMarkdownReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# 🛡️ Guardian Audit Report");
            sb.AppendLine($"- **Timestamp**: {ScanTimestamp:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- **Health Score**: {HealthScore:F1}% ({Grade})");
            sb.AppendLine($"- **Scan Duration**: {ScanDurationSeconds:F2}s");
            sb.AppendLine($"- **Total Issues**: {Issues.Count} (Critical: {CriticalCount}, Warnings: {WarningCount}, Suggestions: {SuggestionCount})");
            sb.AppendLine();

            if (Issues.Count == 0)
            {
                sb.AppendLine("✨ *No issues found! Project is clean, robust, and production-ready.*");
                return sb.ToString();
            }

            void AppendGroup(string groupTitle, IEnumerable<GuardianIssue> groupIssues)
            {
                var list = groupIssues.ToList();
                sb.AppendLine($"## {groupTitle} ({list.Count})");
                if (list.Count == 0)
                {
                    sb.AppendLine("- None");
                    sb.AppendLine();
                    return;
                }

                foreach (var issue in list)
                {
                    string loc = !string.IsNullOrEmpty(issue.AssetPath) 
                        ? (issue.LineNumber > 0 ? $"`{issue.AssetPath}:{issue.LineNumber}`" : $"`{issue.AssetPath}`") 
                        : "Project Wide";
                    sb.AppendLine($"- **[{issue.Category}] {issue.Title}**");
                    sb.AppendLine($"  - **Location**: {loc}");
                    if (!string.IsNullOrEmpty(issue.Description))
                    {
                        sb.AppendLine($"  - **Details**: {issue.Description.Replace("\n", " ")}");
                    }
                }
                sb.AppendLine();
            }

            AppendGroup("🚨 Critical Issues", Issues.Where(i => i.Severity == IssueSeverity.Critical));
            AppendGroup("⚠️ Warnings", Issues.Where(i => i.Severity == IssueSeverity.Warning));
            AppendGroup("💡 Suggestions", Issues.Where(i => i.Severity == IssueSeverity.Suggestion));

            return sb.ToString();
        }
    }
}
