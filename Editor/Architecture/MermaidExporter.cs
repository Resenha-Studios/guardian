using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Resenha.Guardian.Editor.Architecture
{
    /// <summary>
    /// Exports a <see cref="DependencyGraph"/> as Mermaid flowchart code and as a
    /// self contained HTML viewer with search, focus and pan/zoom.
    /// </summary>
    public static class MermaidExporter
    {
        private const string HtmlTemplate = @"<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""UTF-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<title>Guardian Architecture</title>
<script src=""https://cdn.jsdelivr.net/npm/svg-pan-zoom@3.6.1/dist/svg-pan-zoom.min.js""></script>
<style>
  * { box-sizing: border-box; }
  html, body { margin: 0; height: 100%; font-family: Inter, -apple-system, 'Segoe UI', Roboto, sans-serif; color: #0f172a; background: #f8fafc; }
  body { display: flex; }
  aside { width: 280px; flex-shrink: 0; background: #ffffff; border-right: 1px solid #e2e8f0; display: flex; flex-direction: column; }
  aside header { padding: 18px 18px 12px; border-bottom: 1px solid #f1f5f9; }
  aside h1 { margin: 0; font-size: 15px; font-weight: 600; }
  aside .meta { margin-top: 4px; font-size: 12px; color: #64748b; }
  #search { margin: 12px 18px; padding: 8px 10px; border: 1px solid #e2e8f0; border-radius: 6px; font-size: 13px; outline: none; }
  #search:focus { border-color: #6366f1; }
  #list { flex: 1; overflow-y: auto; padding: 0 8px 12px; }
  .item { display: flex; justify-content: space-between; align-items: center; padding: 7px 10px; border-radius: 6px; font-size: 13px; cursor: pointer; }
  .item:hover { background: #f1f5f9; }
  .item.active { background: #eef2ff; color: #4338ca; }
  .item .dot { width: 8px; height: 8px; border-radius: 50%; margin-right: 8px; flex-shrink: 0; background: #cbd5e1; }
  .item.high .dot { background: #f59e0b; }
  .item.god .dot { background: #ef4444; }
  .item .name { flex: 1; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
  .item .num { font-size: 11px; color: #94a3b8; font-variant-numeric: tabular-nums; }
  .legend { padding: 12px 18px; border-top: 1px solid #f1f5f9; font-size: 12px; color: #475569; }
  .legend div { display: flex; align-items: center; margin: 4px 0; }
  .legend span { width: 12px; height: 12px; border-radius: 3px; margin-right: 8px; border: 1.5px solid; }
  main { flex: 1; position: relative; overflow: hidden; background-image: radial-gradient(#e2e8f0 1px, transparent 1px); background-size: 20px 20px; }
  #diagram { width: 100%; height: 100%; }
  #diagram svg { width: 100%; height: 100%; max-width: none !important; }
  #hint { position: absolute; bottom: 14px; left: 50%; transform: translateX(-50%); background: #ffffffee; border: 1px solid #e2e8f0; border-radius: 999px; padding: 6px 14px; font-size: 12px; color: #64748b; }
  #status { position: absolute; inset: 0; display: flex; align-items: center; justify-content: center; color: #94a3b8; font-size: 14px; }
  svg.focusing .node, svg.focusing .edgePath, svg.focusing .flowchart-link, svg.focusing .edgeLabel { opacity: 0.12; transition: opacity .15s; }
  svg.focusing .node.lit, svg.focusing .flowchart-link.lit, svg.focusing .edgePath.lit { opacity: 1; }
  svg .flowchart-link.lit { stroke: #6366f1 !important; stroke-width: 2px !important; }
  svg .node { cursor: pointer; }
</style>
</head>
<body>
<aside>
  <header>
    <h1>Guardian Architecture</h1>
    <div class=""meta"">__NODE_COUNT__ types &middot; __EDGE_COUNT__ dependencies</div>
  </header>
  <input id=""search"" placeholder=""Search types"" autocomplete=""off"">
  <div id=""list""></div>
  <div class=""legend"">
    <div><span style=""background:#ffffff;border-color:#94a3b8""></span>Healthy</div>
    <div title=""8 or more outgoing dependencies""><span style=""background:#fff7ed;border-color:#f59e0b""></span>High coupling</div>
    <div title=""10 or more dependents and 6 or more dependencies""><span style=""background:#fef2f2;border-color:#ef4444""></span>God object candidate</div>
  </div>
</aside>
<main>
  <div id=""status"">Rendering</div>
  <div id=""diagram""></div>
  <div id=""hint"">Click a type to isolate its connections &middot; Esc to reset</div>
</main>

<script type=""text/plain"" id=""mermaid-src"">
__MERMAID__
</script>
<script id=""graph-data"" type=""application/json"">__GRAPH_JSON__</script>

<script type=""module"">
import mermaid from 'https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.esm.min.mjs';

const source = document.getElementById('mermaid-src').textContent;
const graph = JSON.parse(document.getElementById('graph-data').textContent);
const host = document.getElementById('diagram');
const status = document.getElementById('status');

const baseConfig = {
  startOnLoad: false,
  securityLevel: 'loose',
  theme: 'base',
  themeVariables: {
    fontFamily: 'Inter, -apple-system, Segoe UI, Roboto, sans-serif',
    fontSize: '14px',
    primaryColor: '#ffffff',
    primaryBorderColor: '#94a3b8',
    primaryTextColor: '#0f172a',
    lineColor: '#94a3b8',
    clusterBkg: '#ffffffcc',
    clusterBorder: '#cbd5e1',
    titleColor: '#475569'
  },
  flowchart: { curve: 'basis', nodeSpacing: 40, rankSpacing: 70, padding: 12, htmlLabels: true }
};

async function render() {
  // ELK gives orthogonal, far less tangled edges. Fall back to dagre if it cannot load.
  try {
    const elk = (await import('https://cdn.jsdelivr.net/npm/@mermaid-js/layout-elk@0/dist/mermaid-layout-elk.esm.min.mjs')).default;
    mermaid.registerLayoutLoaders(elk);
    mermaid.initialize({ ...baseConfig, layout: 'elk', elk: { mergeEdges: true, nodePlacementStrategy: 'BRANDES_KOEPF' } });
    return (await mermaid.render('guardian-graph', source)).svg;
  } catch (e) {
    console.warn('ELK layout unavailable, using dagre.', e);
    mermaid.initialize(baseConfig);
    return (await mermaid.render('guardian-graph-dagre', source)).svg;
  }
}

const svgMarkup = await render();
host.innerHTML = svgMarkup;
status.remove();

const svg = host.querySelector('svg');
const panZoom = svgPanZoom(svg, { zoomEnabled: true, controlIconsEnabled: false, fit: true, center: true, minZoom: 0.05, maxZoom: 20, dblClickZoomEnabled: false });

// Map rendered elements back to graph ids.
const nodeEls = {};
svg.querySelectorAll('g.node').forEach(el => {
  const m = el.id.match(/^(?:flowchart-)?(n[A-Za-z0-9]+)(?:-\d+)?$/);
  if (m) nodeEls[m[1]] = el;
});
const edgeEls = [];
svg.querySelectorAll('path.flowchart-link, .edgePaths path').forEach(el => {
  let from = null, to = null;
  el.classList.forEach(c => { if (c.startsWith('LS-')) from = c.slice(3); if (c.startsWith('LE-')) to = c.slice(3); });
  if (!from || !to) { const m = el.id.match(/^L_(n[A-Za-z0-9]+)_(n[A-Za-z0-9]+)_\d+$/); if (m) { from = m[1]; to = m[2]; } }
  if (from && to) edgeEls.push({ el, from, to });
});

let focused = null;
function focus(id) {
  focused = id;
  svg.classList.toggle('focusing', !!id);
  Object.values(nodeEls).forEach(el => el.classList.remove('lit'));
  edgeEls.forEach(e => e.el.classList.remove('lit'));
  document.querySelectorAll('.item').forEach(i => i.classList.toggle('active', i.dataset.id === id));
  if (!id) return;
  const lit = new Set([id]);
  edgeEls.forEach(e => {
    if (e.from === id || e.to === id) { e.el.classList.add('lit'); lit.add(e.from); lit.add(e.to); }
  });
  lit.forEach(n => nodeEls[n] && nodeEls[n].classList.add('lit'));
}

Object.entries(nodeEls).forEach(([id, el]) => el.addEventListener('click', ev => { ev.stopPropagation(); focus(focused === id ? null : id); }));
svg.addEventListener('click', () => focus(null));
document.addEventListener('keydown', e => { if (e.key === 'Escape') focus(null); });
window.addEventListener('resize', () => { panZoom.resize(); panZoom.fit(); panZoom.center(); });

// Sidebar list, most coupled first.
const list = document.getElementById('list');
const sorted = [...graph].sort((a, b) => (b.out + b.in) - (a.out + a.in) || a.name.localeCompare(b.name));
for (const n of sorted) {
  const row = document.createElement('div');
  row.className = 'item ' + n.level;
  row.dataset.id = n.id;
  row.dataset.name = n.name.toLowerCase();
  row.title = n.group;
  row.innerHTML = `<span class=""dot""></span><span class=""name"">${n.name}</span><span class=""num"">${n.out} out &middot; ${n.in} in</span>`;
  row.addEventListener('click', () => focus(focused === n.id ? null : n.id));
  list.appendChild(row);
}
document.getElementById('search').addEventListener('input', e => {
  const q = e.target.value.trim().toLowerCase();
  list.querySelectorAll('.item').forEach(r => r.style.display = r.dataset.name.includes(q) ? '' : 'none');
});
</script>
</body>
</html>";

        /// <summary>
        /// Builds Mermaid flowchart code. Types are grouped by namespace, or by
        /// folder when a type lives in the global namespace.
        /// </summary>
        public static string ExportToMermaid(DependencyGraph graph)
        {
            if (graph == null || graph.Nodes.Count == 0)
            {
                return "flowchart LR\n    empty[\"No scripts found in Assets\"]";
            }

            var sb = new StringBuilder();
            sb.AppendLine("flowchart LR");

            foreach (var group in graph.Nodes.Values.GroupBy(GetGroupName).OrderBy(g => g.Key))
            {
                sb.AppendLine($"    subgraph {GroupId(group.Key)}[\"{Escape(group.Key)}\"]");
                sb.AppendLine("        direction TB");
                foreach (var node in group.OrderBy(n => n.Name))
                {
                    sb.AppendLine($"        {NodeId(node.Name)}[\"<b>{Escape(node.Name)}</b><br/><small>{node.OutgoingDependenciesCount} out · {node.IncomingDependentsCount} in</small>\"]");
                }
                sb.AppendLine("    end");
            }

            sb.AppendLine();
            foreach (var node in graph.Nodes.Values.OrderBy(n => n.Name))
            {
                foreach (var dep in node.Dependencies.OrderBy(d => d))
                {
                    if (graph.Nodes.ContainsKey(dep))
                    {
                        sb.AppendLine($"    {NodeId(node.Name)} --> {NodeId(dep)}");
                    }
                }
            }

            sb.AppendLine();
            sb.AppendLine("    classDef normal fill:#ffffff,stroke:#94a3b8,stroke-width:1px,color:#0f172a;");
            sb.AppendLine("    classDef high fill:#fff7ed,stroke:#f59e0b,stroke-width:2px,color:#7c2d12;");
            sb.AppendLine("    classDef god fill:#fef2f2,stroke:#ef4444,stroke-width:2.5px,color:#7f1d1d;");

            foreach (var node in graph.Nodes.Values)
            {
                sb.AppendLine($"    class {NodeId(node.Name)} {GetLevel(node)};");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Writes the interactive HTML viewer to <paramref name="destinationPath"/>.
        /// </summary>
        public static string ExportToHtmlViewer(DependencyGraph graph, string destinationPath)
        {
            int edgeCount = graph?.Nodes.Values.Sum(n => n.Dependencies.Count) ?? 0;

            string html = HtmlTemplate
                .Replace("__MERMAID__", ExportToMermaid(graph))
                .Replace("__GRAPH_JSON__", BuildJson(graph))
                .Replace("__NODE_COUNT__", (graph?.Nodes.Count ?? 0).ToString())
                .Replace("__EDGE_COUNT__", edgeCount.ToString());

            File.WriteAllText(destinationPath, html, Encoding.UTF8);
            return destinationPath;
        }

        private static string BuildJson(DependencyGraph graph)
        {
            if (graph == null) return "[]";

            var items = new List<string>();
            foreach (var n in graph.Nodes.Values)
            {
                items.Add(
                    $"{{\"id\":\"{NodeId(n.Name)}\",\"name\":\"{JsonEscape(n.Name)}\",\"group\":\"{JsonEscape(GetGroupName(n))}\"," +
                    $"\"out\":{n.OutgoingDependenciesCount},\"in\":{n.IncomingDependentsCount},\"level\":\"{GetLevel(n)}\"}}");
            }
            return "[" + string.Join(",", items) + "]";
        }

        private static string GetLevel(DependencyNode node)
        {
            if (node.IsGodObjectCandidate) return "god";
            if (node.IsHighCoupling) return "high";
            return "normal";
        }

        private static string GetGroupName(DependencyNode node)
        {
            if (!string.IsNullOrEmpty(node.NamespaceName) && node.NamespaceName != "Global")
            {
                return node.NamespaceName;
            }

            // No namespace: use the containing folder relative to Assets.
            string dir = Path.GetDirectoryName(node.FilePath ?? string.Empty)?.Replace('\\', '/') ?? string.Empty;
            if (dir.StartsWith("Assets/")) dir = dir.Substring("Assets/".Length);
            else if (dir == "Assets") dir = string.Empty;
            return string.IsNullOrEmpty(dir) ? "Assets" : dir;
        }

        // Ids carry a prefix so names like "end" or "graph" never collide with Mermaid keywords,
        // and contain no underscores so the viewer can parse edge ids unambiguously.
        private static string NodeId(string raw) => "n" + Sanitize(raw);
        private static string GroupId(string raw) => "g" + Sanitize(raw);

        private static string Sanitize(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "X";
            var sb = new StringBuilder(raw.Length);
            foreach (char c in raw)
            {
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')) sb.Append(c);
            }
            return sb.Length == 0 ? "X" : sb.ToString();
        }

        private static string Escape(string text) => (text ?? string.Empty).Replace("\"", "#quot;");

        private static string JsonEscape(string text) =>
            (text ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("<", "\\u003c");
    }
}
