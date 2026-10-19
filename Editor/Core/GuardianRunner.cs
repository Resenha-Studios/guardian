using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;

namespace Resenha.Guardian.Editor.Core
{
    /// <summary>
    /// Coordinates scanner execution and aggregates findings into a unified audit result.
    /// </summary>
    public class GuardianRunner
    {
        private readonly List<IGuardianScanner> _scanners = new List<IGuardianScanner>();

        /// <summary>
        /// Currently registered audit scanners.
        /// </summary>
        public IReadOnlyList<IGuardianScanner> Scanners => _scanners;

        /// <summary>
        /// Registers a scanner instance into the audit pipeline.
        /// </summary>
        public void RegisterScanner(IGuardianScanner scanner)
        {
            if (scanner != null && !_scanners.Contains(scanner))
            {
                _scanners.Add(scanner);
            }
        }

        /// <summary>
        /// Removes all registered scanners.
        /// </summary>
        public void ClearScanners()
        {
            _scanners.Clear();
        }

        /// <summary>
        /// Runs all enabled scanners synchronously in memory and returns the compiled report.
        /// </summary>
        public GuardianScanResult RunScan(Action<float, string> onProgress = null)
        {
            var result = new GuardianScanResult();
            var stopwatch = Stopwatch.StartNew();

            int totalScanners = _scanners.Count;
            if (totalScanners == 0)
            {
                stopwatch.Stop();
                result.ScanDurationSeconds = 0;
                return result;
            }

            for (int i = 0; i < totalScanners; i++)
            {
                var scanner = _scanners[i];
                if (!scanner.IsEnabled) continue;

                float baseProgress = (float)i / totalScanners;
                float scannerSlice = 1f / totalScanners;

                Action<float, string> subProgress = (p, text) =>
                {
                    float currentP = baseProgress + (p * scannerSlice);
                    onProgress?.Invoke(currentP, text);
                };

                try
                {
                    var issues = scanner.Scan(subProgress);
                    if (issues != null)
                    {
                        foreach (var issue in issues)
                        {
                            result.Issues.Add(issue);
                        }
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[Guardian] Scanner '{scanner.Name}' failed: {ex.Message}\n{ex.StackTrace}");
                    result.Issues.Add(new GuardianIssue
                    {
                        Title = $"Scanner Error: {scanner.Name}",
                        Description = ex.Message,
                        Severity = IssueSeverity.Warning,
                        Category = IssueCategory.Project
                    });
                }
            }

            stopwatch.Stop();
            result.ScanDurationSeconds = stopwatch.Elapsed.TotalSeconds;
            onProgress?.Invoke(1f, "Audit complete.");
            return result;
        }
    }
}
