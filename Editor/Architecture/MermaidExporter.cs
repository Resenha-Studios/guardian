using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Resenha.Guardian.Editor.Architecture
{
    /// <summary>
    /// Exports DependencyGraph data into Mermaid.js notation and standalone interactive HTML viewers.
    /// </summary>
    public static class MermaidExporter
    {
        public static string ExportToMermaid(DependencyGraph graph)
        {
            if (graph == null || graph.Nodes.Count == 0)
            {
                return "graph TD\n    Empty[No project scripts found in Assets/]";
            }

            var sb = new StringBuilder();
            sb.AppendLine("graph TD");
            sb.AppendLine("    %% Resenha Guardian Architecture Diagram");
            sb.AppendLine();

            // 1. Agrupamento por Namespace / Módulo
            var groupedByNamespace = graph.Nodes.Values
                .GroupBy(n => string.IsNullOrEmpty(n.NamespaceName) ? "Global" : n.NamespaceName)
                .OrderBy(g => g.Key);

            foreach (var group in groupedByNamespace)
            {
                string safeNsId = SanitizeId(group.Key);
                sb.AppendLine($"    subgraph {safeNsId}[\"{group.Key}\"]");
                foreach (var node in group.OrderBy(n => n.Name))
                {
                    string safeNodeId = SanitizeId(node.Name);
                    string label = node.Name;
                    if (node.OutgoingDependenciesCount > 0 || node.IncomingDependentsCount > 0)
                    {
                        label += $" [{node.OutgoingDependenciesCount} out / {node.IncomingDependentsCount} in]";
                    }
                    sb.AppendLine($"        {safeNodeId}[\"{label}\"]");
                }
                sb.AppendLine("    end");
                sb.AppendLine();
            }

            // 2. Conexões direcionadas
            var connections = new HashSet<string>();
            foreach (var node in graph.Nodes.Values)
            {
                string sourceId = SanitizeId(node.Name);
                foreach (var dep in node.Dependencies)
                {
                    string targetId = SanitizeId(dep);
                    string key = $"{sourceId}-->{targetId}";
                    if (!connections.Contains(key))
                    {
                        connections.Add(key);
                        sb.AppendLine($"    {sourceId} --> {targetId}");
                    }
                }
            }

            // 3. Estilos de Destaque por Calor / Acoplamento
            sb.AppendLine();
            sb.AppendLine("    %% Styling Rules");
            sb.AppendLine("    classDef normal fill:#1e293b,stroke:#38bdf8,stroke-width:1px,color:#f8fafc;");
            sb.AppendLine("    classDef highCoupling fill:#78350f,stroke:#f59e0b,stroke-width:2px,color:#fef3c7;");
            sb.AppendLine("    classDef godObject fill:#7f1d1d,stroke:#ef4444,stroke-width:3px,color:#fee2e2;");

            foreach (var node in graph.Nodes.Values)
            {
                string safeId = SanitizeId(node.Name);
                if (node.IsGodObjectCandidate)
                {
                    sb.AppendLine($"    class {safeId} godObject;");
                }
                else if (node.IsHighCoupling)
                {
                    sb.AppendLine($"    class {safeId} highCoupling;");
                }
                else
                {
                    sb.AppendLine($"    class {safeId} normal;");
                }
            }

            return sb.ToString();
        }

        public static string ExportToHtmlViewer(DependencyGraph graph, string destinationPath)
        {
            string mermaidCode = ExportToMermaid(graph);

            string html = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Guardian Architecture Diagram</title>
    <script src=""https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.min.js""></script>
    <script src=""https://cdn.jsdelivr.net/npm/svg-pan-zoom@3.6.1/dist/svg-pan-zoom.min.js""></script>
    <style>
        body, html {{
            margin: 0;
            padding: 0;
            width: 100%;
            height: 100%;
            background-color: #0b0f14;
            color: #e2e8f0;
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
            overflow: hidden;
        }}
        #header {{
            position: absolute;
            top: 16px;
            left: 16px;
            z-index: 10;
            background: rgba(20, 26, 34, 0.9);
            border: 1px solid #242f3d;
            border-radius: 8px;
            padding: 12px 18px;
            backdrop-filter: blur(8px);
            box-shadow: 0 4px 16px rgba(0, 0, 0, 0.4);
        }}
        h1 {{
            margin: 0;
            font-size: 16px;
            font-weight: 600;
            color: #f8fafc;
        }}
        p {{
            margin: 4px 0 0 0;
            font-size: 11px;
            color: #94a3b8;
        }}
        #container {{
            width: 100%;
            height: 100%;
            display: flex;
            align-items: center;
            justify-content: center;
        }}
        .mermaid {{
            width: 100%;
            height: 100%;
        }}
    </style>
</head>
<body>
    <div id=""header"">
        <h1>🛡️ Guardian &bull; Architecture Dependency Graph</h1>
        <p>Interactive Pan &amp; Zoom &bull; Drag to move, scroll to zoom &bull; Total Nodes: {graph.Nodes.Count}</p>
    </div>
    <div id=""container"">
        <pre class=""mermaid"">
{mermaidCode}
        </pre>
    </div>
    <script>
        mermaid.initialize({{
            startOnLoad: true,
            theme: 'dark',
            securityLevel: 'loose',
            flowchart: {{ useMaxWidth: false, htmlLabels: true }}
        }});

        window.addEventListener('load', () => {{
            setTimeout(() => {{
                const svg = document.querySelector('.mermaid svg');
                if (svg) {{
                    svg.style.width = '100%';
                    svg.style.height = '100%';
                    svgPanZoom(svg, {{
                        zoomEnabled: true,
                        controlIconsEnabled: true,
                        fit: true,
                        center: true,
                        minZoom: 0.1,
                        maxZoom: 10
                    }});
                }}
            }}, 500);
        }});
    </script>
</body>
</html>";

            File.WriteAllText(destinationPath, html, Encoding.UTF8);
            return destinationPath;
        }

        private static string SanitizeId(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "Node";
            var sb = new StringBuilder();
            for (int i = 0; i < raw.Length; i++)
            {
                char c = raw[i];
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append('_');
                }
            }
            return sb.ToString();
        }
    }
}
