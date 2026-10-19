namespace Resenha.Guardian.Editor.Core
{
    /// <summary>
    /// Classification of issue severity for reporting and health score weighting.
    /// </summary>
    public enum IssueSeverity
    {
        /// <summary>
        /// Critical failures that break builds, scene execution, or corrupt data.
        /// </summary>
        Critical,

        /// <summary>
        /// Anti-patterns or performance bottlenecks that should be addressed.
        /// </summary>
        Warning,

        /// <summary>
        /// Best practices, code quality guidelines, and localization suggestions.
        /// </summary>
        Suggestion
    }

    /// <summary>
    /// Functional domain of an audited item.
    /// </summary>
    public enum IssueCategory
    {
        Integrity,
        CodeQuality,
        Localization,
        Project
    }
}
