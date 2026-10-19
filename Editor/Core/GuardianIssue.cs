using System;
using UnityEngine;

namespace Resenha.Guardian.Editor.Core
{
    /// <summary>
    /// Represents an actionable issue or recommendation identified during a project audit.
    /// </summary>
    [Serializable]
    public class GuardianIssue
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public IssueSeverity Severity { get; set; }
        public IssueCategory Category { get; set; }
        public string AssetPath { get; set; }
        public int LineNumber { get; set; } = -1;
        public UnityEngine.Object TargetObject { get; set; }
        public bool CanQuickFix { get; set; }
        public Action QuickFixAction { get; set; }
        public string QuickFixLabel { get; set; }

        public GuardianIssue()
        {
            Id = Guid.NewGuid().ToString();
        }
    }
}
