using System.Collections.Generic;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Resenha.Guardian.Editor.Localization
{
    public enum GuardianLanguage
    {
        English = 0,
        Portuguese = 1,
        Spanish = 2,
        Japanese = 3,
        Chinese = 4
    }

    public static class GuardianStrings
    {
        private static GuardianLanguage _currentLanguage = GuardianLanguage.English;

        public static GuardianLanguage CurrentLanguage
        {
            get => _currentLanguage;
            set => _currentLanguage = value;
        }

        private static readonly Dictionary<string, string[]> Translations = new Dictionary<string, string[]>
        {
            // Window Header & Core
            { "window_title", new[] { "Guardian", "Guardian", "Guardian", "Guardian", "Guardian" } },
            { "window_subtitle", new[] { "Project Integrity & Quality Auditor", "Auditor de Integridade e Qualidade do Projeto", "Auditor de Integridad y Calidad del Projeto", "プロジェクト完全性・品質監査", "项目完整性与质量审计" } },
            { "scan_button", new[] { "Scan", "Auditar", "Auditar", "監査", "审计" } },
            { "scanning", new[] { "Scanning...", "Auditando...", "Auditando...", "監査中...", "正在审计..." } },
            { "score_label", new[] { "Health Score", "Pontuação de Saúde", "Puntaje de Salud", "健全性スコア", "健康评分" } },
            
            // Filter Tabs
            { "filter_all", new[] { "All", "Todos", "Todos", "すべて", "全部" } },
            { "filter_critical", new[] { "Critical", "Críticos", "Críticos", "クリティカル", "严重" } },
            { "filter_warnings", new[] { "Warnings", "Avisos", "Advertencias", "警告", "警告" } },
            { "filter_suggestions", new[] { "Suggestions", "Sugestões", "Sugerencias", "提案", "建议" } },
            
            // Stats & Info
            { "issues_found", new[] { "{0} issues detected", "{0} problemas detectados", "{0} problemas detectados", "{0} 件の問題を検出", "检测到 {0} 个问题" } },
            { "scan_time", new[] { "Completed in {0:F2}s", "Concluído em {0:F2}s", "Completado en {0:F2}s", "{0:F2}秒で完了", "耗时 {0:F2}秒" } },
            { "clean_state", new[] { "No issues found! Project is clean and robust.", "Nenhum problema encontrado! O projeto está saudável e robusto.", "¡No se encontraron problemas! El proyecto está limpio y robusto.", "問題は見つかりませんでした！プロジェクトは健全です。", "未发现问题！项目健康且稳健。" } },
            
            // Actions
            { "ping_asset", new[] { "Select Asset", "Selecionar Asset", "Seleccionar Asset", "アセットを選択", "选中资源" } },
            { "quick_fix", new[] { "Quick Fix", "Correção Rápida", "Corrección Rápida", "クイック修正", "快速修复" } },
            { "open_code", new[] { "Open Line", "Abrir Linha", "Abrir Línea", "行を開く", "打开代码行" } },
            { "copy_report", new[] { "Report", "Relatório", "Informe", "レポート", "报告" } },
            { "copied", new[] { "Copied!", "Copiado!", "¡Copiado!", "コピーしました！", "已复制！" } },
            { "copy_tooltip", new[] { "Copy complete Markdown report to clipboard (ideal for AI or team review)", "Copiar relatório completo em Markdown para a área de transferência (ideal para IA ou revisão de time)", "Copiar informe completo en Markdown al portapapeles (ideal para IA o revisión de equipo)", "Markdown形式の完全なレポートをクリップボードにコピー（AIまたはチームレビューに最適）", "将完整的Markdown报告复制到剪贴板（适合AI分析或团队审查）" } },
            { "copy_mermaid", new[] { "Mermaid", "Mermaid", "Mermaid", "Mermaid", "Mermaid" } },
            { "mermaid_copied", new[] { "Copied!", "Copiado!", "¡Copiado!", "コピー済!", "已复制!" } },
            { "view_interactive", new[] { "Diagram", "Diagrama", "Diagrama", "図", "图表" } },
            { "diagram_tooltip", new[] { "Open interactive Pan & Zoom dependency diagram in browser", "Abrir diagrama de dependências interativo com Pan e Zoom no navegador", "Abrir diagrama de dependencias interactivo con Pan y Zoom en el navegador", "ブラウザでインタラクティブな依存関係図を開く", "在浏览器中打开交互式依赖关系图" } },
            { "language_selector", new[] { "Language", "Idioma", "Idioma", "言語", "语言" } }
        };

        public static string Get(string key, params object[] args)
        {
            if (Translations.TryGetValue(key, out var list))
            {
                int langIndex = (int)_currentLanguage;
                if (langIndex < 0 || langIndex >= list.Length) langIndex = 0;
                string format = list[langIndex];
                return args != null && args.Length > 0 ? string.Format(format, args) : format;
            }
            return key;
        }

        public static LocalizedString CreateLocalizedString(string table, string entry)
        {
            return new LocalizedString(table, entry);
        }
    }
}
