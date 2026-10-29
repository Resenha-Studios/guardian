using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Resenha.Guardian.Editor.Architecture;
using Resenha.Guardian.Editor.Core;
using Resenha.Guardian.Editor.Localization;
using Resenha.Guardian.Editor.Scanners;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Resenha.Guardian.Editor.UI
{
    public class GuardianWindow : EditorWindow
    {
        private VisualElement _root;
        private GuardianRunner _runner;
        private GuardianScanResult _lastResult;
        private IssueSeverity? _selectedSeverityFilter = null; // null = All

        // Visual Elements
        private Label _headerTitle;
        private Label _headerSubtitle;
        private DropdownField _langDropdown;
        private Button _btnViewDiagram;
        private Button _btnCopyMermaid;
        private Button _btnCopyReport;
        private Button _btnScan;
        private Label _gradeBadge;
        private Label _scoreTitle;
        private Label _scoreSummary;
        private Label _statDuration;
        private Button _filterAll;
        private Button _filterCritical;
        private Button _filterWarnings;
        private Button _filterSuggestions;
        private ScrollView _issuesScrollView;
        private VisualElement _emptyState;
        private Label _emptyStateText;

        [MenuItem("Tools/Resenha/Guardian %g", priority = 100)]
        public static void Open()
        {
            var window = GetWindow<GuardianWindow>();
            window.titleContent = new GUIContent("Guardian", EditorGUIUtility.IconContent("d_UnityEditor.ConsoleWindow").image);
            window.minSize = new Vector2(440, 360);
            window.Show();
        }

        private void OnEnable()
        {
            InitializeRunner();
        }

        private void CreateGUI()
        {
            _root = rootVisualElement;

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.resenha.guardian/Editor/UI/GuardianWindow.uxml");
            if (visualTree != null)
            {
                visualTree.CloneTree(_root);
            }
            else
            {
                var label = new Label("Could not load GuardianWindow.uxml from Packages/com.resenha.guardian/Editor/UI/");
                _root.Add(label);
                return;
            }

            QueryVisualElements();
            SetupLanguageSelector();
            SetupEvents();
            ApplyLocalization();

            if (_lastResult != null)
            {
                RenderResults();
            }
            else
            {
                UpdateScoreHeader(100f, "A+", 0, 0, 0, 0);
            }
        }

        private void InitializeRunner()
        {
            _runner = new GuardianRunner();
            _runner.RegisterScanner(new MissingScriptsScanner());
            _runner.RegisterScanner(new MissingReferencesScanner());
            _runner.RegisterScanner(new CodeAntiPatternsScanner());
            _runner.RegisterScanner(new LocalizationComplianceScanner());
        }

        private void QueryVisualElements()
        {
            _headerTitle = _root.Q<Label>("header-title");
            _headerSubtitle = _root.Q<Label>("header-subtitle");
            _langDropdown = _root.Q<DropdownField>("lang-dropdown");
            _btnViewDiagram = _root.Q<Button>("btn-view-diagram");
            _btnCopyMermaid = _root.Q<Button>("btn-copy-mermaid");
            _btnCopyReport = _root.Q<Button>("btn-copy-report");
            _btnScan = _root.Q<Button>("btn-scan");
            _gradeBadge = _root.Q<Label>("grade-badge");
            _scoreTitle = _root.Q<Label>("score-title");
            _scoreSummary = _root.Q<Label>("score-summary");
            _statDuration = _root.Q<Label>("stat-duration");

            _filterAll = _root.Q<Button>("filter-all");
            _filterCritical = _root.Q<Button>("filter-critical");
            _filterWarnings = _root.Q<Button>("filter-warnings");
            _filterSuggestions = _root.Q<Button>("filter-suggestions");

            _issuesScrollView = _root.Q<ScrollView>("issues-scrollview");
            _emptyState = _root.Q<VisualElement>("empty-state");
            _emptyStateText = _root.Q<Label>("empty-state-text");
        }

        private void SetupLanguageSelector()
        {
            if (_langDropdown == null) return;

            var languages = new List<string> { "English", "Português", "Español", "日本語", "简体中文" };
            _langDropdown.choices = languages;
            _langDropdown.index = (int)GuardianStrings.CurrentLanguage;

            _langDropdown.RegisterValueChangedCallback(evt =>
            {
                int idx = languages.IndexOf(evt.newValue);
                if (idx >= 0)
                {
                    GuardianStrings.CurrentLanguage = (GuardianLanguage)idx;
                    ApplyLocalization();
                    if (_lastResult != null)
                    {
                        RenderResults();
                    }
                }
            });
        }

        private void SetupEvents()
        {
            if (_btnScan != null)
            {
                _btnScan.clicked += PerformScan;
            }

            if (_btnCopyReport != null)
            {
                _btnCopyReport.clicked += CopyReportToClipboard;
            }

            if (_btnCopyMermaid != null)
            {
                _btnCopyMermaid.clicked += CopyMermaidToClipboard;
            }

            if (_btnViewDiagram != null)
            {
                _btnViewDiagram.clicked += ViewArchitectureDiagram;
            }

            if (_filterAll != null)
            {
                _filterAll.clicked += () => SetFilter(null);
            }

            if (_filterCritical != null)
            {
                _filterCritical.clicked += () => SetFilter(IssueSeverity.Critical);
            }

            if (_filterWarnings != null)
            {
                _filterWarnings.clicked += () => SetFilter(IssueSeverity.Warning);
            }

            if (_filterSuggestions != null)
            {
                _filterSuggestions.clicked += () => SetFilter(IssueSeverity.Suggestion);
            }
        }

        private void CopyReportToClipboard()
        {
            if (_lastResult == null)
            {
                _lastResult = _runner.RunScan();
                RenderResults();
            }

            string report = _lastResult.ToMarkdownReport();
            EditorGUIUtility.systemCopyBuffer = report;

            if (_btnCopyReport != null)
            {
                _btnCopyReport.text = GuardianStrings.Get("copied");
                _btnCopyReport.AddToClassList("copied");

                double restoreTime = EditorApplication.timeSinceStartup + 2.0;
                void ResetButton()
                {
                    if (EditorApplication.timeSinceStartup >= restoreTime)
                    {
                        EditorApplication.update -= ResetButton;
                        if (_btnCopyReport != null)
                        {
                            _btnCopyReport.text = GuardianStrings.Get("copy_report");
                            _btnCopyReport.RemoveFromClassList("copied");
                        }
                    }
                }
                EditorApplication.update += ResetButton;
            }

            Debug.Log("[Guardian] Audit report copied to clipboard!");
        }

        private void CopyMermaidToClipboard()
        {
            var analyzer = new ArchitectureAnalyzer();
            var graph = analyzer.BuildGraph("Assets");
            string mermaid = MermaidExporter.ExportToMermaid(graph);
            EditorGUIUtility.systemCopyBuffer = mermaid;

            if (_btnCopyMermaid != null)
            {
                _btnCopyMermaid.text = GuardianStrings.Get("mermaid_copied");
                _btnCopyMermaid.AddToClassList("copied");

                double restoreTime = EditorApplication.timeSinceStartup + 2.0;
                void ResetButton()
                {
                    if (EditorApplication.timeSinceStartup >= restoreTime)
                    {
                        EditorApplication.update -= ResetButton;
                        if (_btnCopyMermaid != null)
                        {
                            _btnCopyMermaid.text = GuardianStrings.Get("copy_mermaid");
                            _btnCopyMermaid.RemoveFromClassList("copied");
                        }
                    }
                }
                EditorApplication.update += ResetButton;
            }

            Debug.Log("[Guardian] Mermaid architecture diagram copied to clipboard!");
        }

        private void ViewArchitectureDiagram()
        {
            var analyzer = new ArchitectureAnalyzer();
            var graph = analyzer.BuildGraph("Assets");
            string tempDir = Path.Combine(Directory.GetCurrentDirectory(), "Temp");
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }
            string htmlPath = Path.Combine(tempDir, "Guardian_Architecture.html");
            MermaidExporter.ExportToHtmlViewer(graph, htmlPath);

            Application.OpenURL("file://" + htmlPath);
            Debug.Log($"[Guardian] Opened architecture diagram in browser: {htmlPath}");
        }

        private void ApplyLocalization()
        {
            if (_headerTitle != null) _headerTitle.text = GuardianStrings.Get("window_title");
            if (_headerSubtitle != null) _headerSubtitle.text = GuardianStrings.Get("window_subtitle");
            if (_btnScan != null) _btnScan.text = GuardianStrings.Get("scan_button");
            if (_btnCopyReport != null)
            {
                _btnCopyReport.text = GuardianStrings.Get("copy_report");
                _btnCopyReport.tooltip = GuardianStrings.Get("copy_tooltip");
            }
            if (_btnCopyMermaid != null)
            {
                _btnCopyMermaid.text = GuardianStrings.Get("copy_mermaid");
                _btnCopyMermaid.tooltip = "Copy Mermaid.js diagram code to clipboard";
            }
            if (_btnViewDiagram != null)
            {
                _btnViewDiagram.text = GuardianStrings.Get("view_interactive");
                _btnViewDiagram.tooltip = GuardianStrings.Get("diagram_tooltip");
            }
            if (_emptyStateText != null) _emptyStateText.text = GuardianStrings.Get("clean_state");
        }

        private void SetFilter(IssueSeverity? severity)
        {
            _selectedSeverityFilter = severity;

            _filterAll?.EnableInClassList("active", severity == null);
            _filterCritical?.EnableInClassList("active", severity == IssueSeverity.Critical);
            _filterWarnings?.EnableInClassList("active", severity == IssueSeverity.Warning);
            _filterSuggestions?.EnableInClassList("active", severity == IssueSeverity.Suggestion);

            RenderIssueList();
        }

        private void PerformScan()
        {
            if (_btnScan != null)
            {
                _btnScan.SetEnabled(false);
                _btnScan.text = GuardianStrings.Get("scanning");
            }

            EditorApplication.delayCall += () =>
            {
                try
                {
                    _lastResult = _runner.RunScan();
                    RenderResults();
                }
                finally
                {
                    if (_btnScan != null)
                    {
                        _btnScan.SetEnabled(true);
                        _btnScan.text = GuardianStrings.Get("scan_button");
                    }
                }
            };
        }

        private void RenderResults()
        {
            if (_lastResult == null) return;

            UpdateScoreHeader(
                _lastResult.HealthScore,
                _lastResult.Grade,
                _lastResult.Issues.Count,
                _lastResult.CriticalCount,
                _lastResult.WarningCount,
                _lastResult.SuggestionCount
            );

            if (_statDuration != null)
            {
                _statDuration.text = GuardianStrings.Get("scan_time", _lastResult.ScanDurationSeconds);
            }

            RenderIssueList();
        }

        private void UpdateScoreHeader(float score, string grade, int total, int critical, int warnings, int suggestions)
        {
            if (_gradeBadge != null)
            {
                _gradeBadge.text = grade;
                _gradeBadge.RemoveFromClassList("grade-f");
                _gradeBadge.RemoveFromClassList("grade-c");
                _gradeBadge.RemoveFromClassList("grade-b");
                _gradeBadge.RemoveFromClassList("grade-a");

                if (grade == "F") _gradeBadge.AddToClassList("grade-f");
                else if (grade == "C") _gradeBadge.AddToClassList("grade-c");
                else if (grade == "B") _gradeBadge.AddToClassList("grade-b");
                else _gradeBadge.AddToClassList("grade-a");
            }

            if (_scoreTitle != null)
            {
                _scoreTitle.text = $"{Mathf.RoundToInt(score)}% {GuardianStrings.Get("score_label")}";
            }

            if (_scoreSummary != null)
            {
                _scoreSummary.text = GuardianStrings.Get("issues_found", total);
            }

            if (_filterAll != null) _filterAll.text = $"{GuardianStrings.Get("filter_all")} ({total})";
            if (_filterCritical != null) _filterCritical.text = $"{GuardianStrings.Get("filter_critical")} ({critical})";
            if (_filterWarnings != null) _filterWarnings.text = $"{GuardianStrings.Get("filter_warnings")} ({warnings})";
            if (_filterSuggestions != null) _filterSuggestions.text = $"{GuardianStrings.Get("filter_suggestions")} ({suggestions})";
        }

        private void RenderIssueList()
        {
            if (_issuesScrollView == null) return;

            _issuesScrollView.Clear();

            if (_lastResult == null || _lastResult.Issues.Count == 0)
            {
                if (_emptyState != null)
                {
                    _emptyState.style.display = DisplayStyle.Flex;
                    _issuesScrollView.Add(_emptyState);
                }
                return;
            }

            var filtered = _lastResult.Issues
                .Where(i => _selectedSeverityFilter == null || i.Severity == _selectedSeverityFilter)
                .ToList();

            if (filtered.Count == 0)
            {
                if (_emptyState != null)
                {
                    _emptyState.style.display = DisplayStyle.Flex;
                    _issuesScrollView.Add(_emptyState);
                }
                return;
            }

            if (_emptyState != null)
            {
                _emptyState.style.display = DisplayStyle.None;
            }

            foreach (var issue in filtered)
            {
                var card = CreateIssueCard(issue);
                _issuesScrollView.Add(card);
            }
        }

        private VisualElement CreateIssueCard(GuardianIssue issue)
        {
            var card = new VisualElement();
            card.AddToClassList("guardian-issue-card");
            card.tooltip = issue.Description;

            // Info Section (Dot + Texts)
            var infoBox = new VisualElement();
            infoBox.AddToClassList("guardian-card-info");

            var dot = new VisualElement();
            dot.AddToClassList("guardian-severity-dot");
            switch (issue.Severity)
            {
                case IssueSeverity.Critical:
                    dot.AddToClassList("sev-critical");
                    break;
                case IssueSeverity.Warning:
                    dot.AddToClassList("sev-warning");
                    break;
                case IssueSeverity.Suggestion:
                    dot.AddToClassList("sev-suggestion");
                    break;
            }
            infoBox.Add(dot);

            var textBox = new VisualElement();
            textBox.AddToClassList("guardian-card-text");

            var titleLabel = new Label(issue.Title);
            titleLabel.AddToClassList("guardian-card-title");
            textBox.Add(titleLabel);

            if (!string.IsNullOrEmpty(issue.AssetPath))
            {
                string lineInfo = issue.LineNumber > 0 ? $":{issue.LineNumber}" : "";
                var pathLabel = new Label($"{issue.AssetPath}{lineInfo}");
                pathLabel.AddToClassList("guardian-card-path");
                textBox.Add(pathLabel);
            }

            infoBox.Add(textBox);
            card.Add(infoBox);

            // Actions Section
            var actionsBox = new VisualElement();
            actionsBox.AddToClassList("guardian-card-actions");

            // Ping/Select Object Action
            if (issue.TargetObject != null || !string.IsNullOrEmpty(issue.AssetPath))
            {
                var btnSelect = new Button(() =>
                {
                    if (issue.TargetObject != null)
                    {
                        EditorGUIUtility.PingObject(issue.TargetObject);
                        Selection.activeObject = issue.TargetObject;
                    }
                    else if (!string.IsNullOrEmpty(issue.AssetPath))
                    {
                        var asset = AssetDatabase.LoadMainAssetAtPath(issue.AssetPath);
                        if (asset != null)
                        {
                            EditorGUIUtility.PingObject(asset);
                            Selection.activeObject = asset;
                        }
                    }
                });
                btnSelect.text = GuardianStrings.Get("ping_asset");
                btnSelect.AddToClassList("guardian-btn-action");
                btnSelect.tooltip = "Locate and highlight this asset in Project view";
                actionsBox.Add(btnSelect);
            }

            // Open Line in IDE Action
            if (issue.LineNumber > 0 && !string.IsNullOrEmpty(issue.AssetPath))
            {
                var btnCode = new Button(() =>
                {
                    var scriptAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(issue.AssetPath);
                    if (scriptAsset != null)
                    {
                        AssetDatabase.OpenAsset(scriptAsset, issue.LineNumber);
                    }
                });
                btnCode.text = GuardianStrings.Get("open_code");
                btnCode.AddToClassList("guardian-btn-action");
                btnCode.tooltip = $"Open script directly at line {issue.LineNumber}";
                actionsBox.Add(btnCode);
            }

            // Quick Fix Action
            if (issue.CanQuickFix && issue.QuickFixAction != null)
            {
                var btnFix = new Button(() =>
                {
                    issue.QuickFixAction.Invoke();
                    _lastResult.Issues.Remove(issue);
                    RenderResults();
                });
                btnFix.text = !string.IsNullOrEmpty(issue.QuickFixLabel) ? issue.QuickFixLabel : GuardianStrings.Get("quick_fix");
                btnFix.AddToClassList("guardian-btn-action");
                btnFix.AddToClassList("guardian-btn-quickfix");
                btnFix.tooltip = "Apply automatic fix for this issue";
                actionsBox.Add(btnFix);
            }

            card.Add(actionsBox);
            return card;
        }
    }
}
