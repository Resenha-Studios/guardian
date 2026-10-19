using System;
using System.Collections.Generic;

namespace Resenha.Guardian.Editor.Core
{
    /// <summary>
    /// Contract for modular audit scanners in Guardian.
    /// Implement this interface to add custom integrity, architecture, or style checks.
    /// </summary>
    public interface IGuardianScanner
    {
        /// <summary>
        /// Display name of the scanner.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Brief explanation of the scanner's purpose.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Domain category of issues produced by this scanner.
        /// </summary>
        IssueCategory Category { get; }

        /// <summary>
        /// Whether this scanner is currently active during audit runs.
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Executes the audit pass and yields identified issues.
        /// </summary>
        /// <param name="onProgress">Optional callback reporting 0-1 progress and status text.</param>
        IEnumerable<GuardianIssue> Scan(Action<float, string> onProgress);
    }
}
