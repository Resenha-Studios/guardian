using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Resenha.Guardian.Editor.Architecture
{
    /// <summary>
    /// Exports a <see cref="DependencyGraph"/> as Mermaid flowchart code and as a
    /// self contained HTML viewer with search, focus mode, pan/zoom and export.
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
  aside { width: 280px; flex-shrink: 0; background: #fff; border-right: 1px solid #e2e8f0; display: flex; flex-direction: column; }
  aside header { padding: 18px 18px 12px; border-bottom: 1px solid #f1f5f9; }
  aside h1 { margin: 0; font-size: 15px; font-weight: 600; }
  .meta { margin-top: 4px; font-size: 12px; color: #64748b; }
  #search { margin: 12px 18px; padding: 8px 10px; border: 1px solid #e2e8f0; border-radius: 6px; font-size: 13px; outline: none; }
  #search:focus { border-color: #6366f1; }
  #list { flex: 1; overflow-y: auto; padding: 0 8px 12px; }
  .item { display: flex; align-items: center; padding: 7px 10px; border-radius: 6px; font-size: 13px; cursor: pointer; }
  .item:hover { background: #f1f5f9; }
  .item.active { background: #eef2ff; color: #4338ca; }
  .dot { width: 8px; height: 8px; border-radius: 50%; margin-right: 8px; flex-shrink: 0; background: #cbd5e1; }
  .item.high .dot { background: #f59e0b; }
  .item.god .dot { background: #ef4444; }
  .name { flex: 1; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
  .num { font-size: 11px; color: #94a3b8; font-variant-numeric: tabular-nums; }
  .legend { padding: 12px 18px; border-top: 1px solid #f1f5f9; font-size: 12px; color: #475569; }
  .legend div { display: flex; align-items: center; margin: 4px 0; }
  .legend span { width: 12px; height: 12px; border-radius: 3px; margin-right: 8px; border: 1.5px solid; }
  main { flex: 1; display: flex; flex-direction: column; min-width: 0; }
  .toolbar { display: flex; align-items: center; gap: 8px; padding: 10px 16px; background: #fff; border-bottom: 1px solid #e2e8f0; font-size: 13px; }
  .crumb { flex: 1; color: #64748b; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
  .crumb b { color: #0f172a; }
  button { font: inherit; font-size: 12px; padding: 6px 12px; border-radius: 6px; border: 1px solid #e2e8f0; background: #fff; color: #334155; cursor: pointer; }
  button:hover { border-color: #6366f1; color: #4338ca; }
  #back[hidden] { display: none; }
  #stage { flex: 1; position: relative; overflow: hidden; background-image: radial-gradient(#e2e8f0 1px, transparent 1px); background-size: 20px 20px; }
  #diagram, #diagram svg { width: 100%; height: 100%; max-width: none !important; }
  #diagram .node { cursor: pointer; }
  #status { position: absolute; inset: 0; display: flex; align-items: center; justify-content: center; color: #94a3b8; font-size: 14px; pointer-events: none; }
  #hint { position: absolute; bottom: 14px; left: 50%; transform: translateX(-50%); background: #ffffffee; border: 1px solid #e2e8f0; border-radius: 999px; padding: 6px 14px; font-size: 12px; color: #64748b; pointer-events: none; }
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
    <div><span style=""background:#fff;border-color:#94a3b8""></span>Healthy</div>
    <div title=""8 or more outgoing dependencies""><span style=""background:#fff7ed;border-color:#f59e0b""></span>High coupling</div>
    <div title=""10 or more dependents and 6 or more dependencies""><span style=""background:#fef2f2;border-color:#ef4444""></span>God object candidate</div>
    <div title=""Type currently isolated""><span style=""background:#eef2ff;border-color:#6366f1""></span>Selected</div>
  </div>
</aside>
<main>
  <div class=""toolbar"">
    <button id=""back"" hidden title=""Back to the full diagram (Esc)"">All types</button>
    <div class=""crumb"" id=""crumb""></div>
    <button id=""export-svg"" title=""Download the current view as SVG"">SVG</button>
    <button id=""export-png"" title=""Download the current view as PNG"">PNG</button>
    <button id=""export-mmd"" title=""Download the current view as Mermaid source"">Mermaid</button>
  </div>
  <div id=""stage"">
    <div id=""status"">Rendering</div>
    <div id=""diagram""></div>
    <div id=""hint"">Click a type to isolate it with its dependencies and dependents</div>
  </div>
</main>

<script id=""graph-data"" type=""application/json"">__GRAPH_JSON__</script>

<script type=""module"">
import mermaid from 'https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.esm.min.mjs';

mermaid.initialize({
  startOnLoad: false,
  securityLevel: 'loose',
  theme: 'base',
  themeVariables: {
    fontFamily: 'Inter, -apple-system, Segoe UI, Roboto, sans-serif',
    fontSize: '14px',
    primaryColor: '#ffffff', primaryBorderColor: '#94a3b8', primaryTextColor: '#0f172a',
    lineColor: '#94a3b8', clusterBkg: '#ffffff', clusterBorder: '#cbd5e1', titleColor: '#475569'
  },
  // SVG labels (no foreignObject) keep PNG export working.
  flowchart: { htmlLabels: false, curve: 'step', nodeSpacing: 36, rankSpacing: 80, padding: 14 }
});

const nodes = JSON.parse(document.getElementById('graph-data').textContent);
const byId = Object.fromEntries(nodes.map(n => [n.id, n]));
nodes.forEach(n => { n.deps = n.deps.filter(d => byId[d]); n.users = []; });
nodes.forEach(n => n.deps.forEach(d => byId[d].users.push(n.id)));

const host = document.getElementById('diagram');
const status = document.getElementById('status');
const crumb = document.getElementById('crumb');
const back = document.getElementById('back');

const esc = s => s.replace(/""/g, '#quot;');
const nodeLine = n => `    ${n.id}[""${esc(n.name)}<br/>${n.deps.length} out · ${n.users.length} in""]`;
const styles = [
  '    classDef normal fill:#ffffff,stroke:#94a3b8,stroke-width:1px,color:#0f172a;',
  '    classDef high fill:#fff7ed,stroke:#f59e0b,stroke-width:2px,color:#7c2d12;',
  '    classDef god fill:#fef2f2,stroke:#ef4444,stroke-width:2.5px,color:#7f1d1d;',
  '    classDef focus fill:#eef2ff,stroke:#6366f1,stroke-width:3px,color:#312e81;'
];

function fullSource() {
  const out = ['flowchart LR'];
  const groups = {};
  nodes.forEach(n => (groups[n.group] ||= []).push(n));
  Object.keys(groups).sort().forEach((g, i) => {
    out.push(`  subgraph grp${i}[""${esc(g)}""]`, '    direction TB');
    groups[g].sort((a, b) => a.name.localeCompare(b.name)).forEach(n => out.push(nodeLine(n)));
    out.push('  end');
  });
  nodes.forEach(n => n.deps.forEach(d => out.push(`  ${n.id} --> ${d}`)));
  out.push(...styles);
  nodes.forEach(n => out.push(`  class ${n.id} ${n.level};`));
  return out.join('\n');
}

// Dependents on the left, selected type in the middle, dependencies on the right.
function focusSource(id) {
  const c = byId[id];
  const outs = [...new Set(c.deps)];
  const ins = [...new Set(c.users)].filter(u => !outs.includes(u));
  const out = ['flowchart LR'];
  if (ins.length) {
    out.push(`  subgraph usedBy[""Used by (${c.users.length})""]`, '    direction TB');
    ins.forEach(u => out.push(nodeLine(byId[u])));
    out.push('  end');
  }
  out.push(nodeLine(c));
  if (outs.length) {
    out.push(`  subgraph dependsOn[""Depends on (${outs.length})""]`, '    direction TB');
    outs.forEach(d => out.push(nodeLine(byId[d])));
    out.push('  end');
  }
  c.users.forEach(u => out.push(`  ${u} --> ${id}`));
  outs.forEach(d => out.push(`  ${id} --> ${d}`));
  out.push(...styles);
  [...ins, ...outs].forEach(n => out.push(`  class ${n} ${byId[n].level};`));
  out.push(`  class ${id} focus;`);
  return out.join('\n');
}

let renderCount = 0, panZoom = null, currentSvg = '', currentSource = '', currentName = 'architecture', focused = null;

async function draw(source) {
  status.textContent = 'Rendering';
  status.style.display = '';
  const { svg } = await mermaid.render('guardian' + (++renderCount), source);
  currentSvg = svg;
  currentSource = source;
  if (panZoom) { panZoom.destroy(); panZoom = null; }
  host.innerHTML = svg;
  status.style.display = 'none';

  const el = host.querySelector('svg');
  el.removeAttribute('style');
  panZoom = svgPanZoom(el, { zoomEnabled: true, fit: true, center: true, minZoom: 0.05, maxZoom: 20, dblClickZoomEnabled: false });

  el.querySelectorAll('g.node').forEach(g => {
    const raw = g.dataset.id || g.id;
    const m = raw.match(/(?:^|-)(n[A-Za-z0-9]+)(?:-\d+)?$/);
    const id = m ? m[1] : raw;
    if (!byId[id]) return;
    g.addEventListener('click', e => { e.stopPropagation(); show(id === focused ? null : id); });
  });
}

function show(id) {
  focused = id;
  back.hidden = !id;
  document.querySelectorAll('.item').forEach(i => i.classList.toggle('active', i.dataset.id === id));
  if (id) {
    const n = byId[id];
    crumb.innerHTML = `<b>${n.name}</b> &middot; ${n.deps.length} out &middot; ${n.users.length} in &middot; ${n.group}`;
    currentName = n.name;
    draw(focusSource(id));
  } else {
    crumb.textContent = 'All types';
    currentName = 'architecture';
    draw(fullSource());
  }
}

back.onclick = () => show(null);
document.addEventListener('keydown', e => { if (e.key === 'Escape' && focused) show(null); });
window.addEventListener('resize', () => { if (panZoom) { panZoom.resize(); panZoom.fit(); panZoom.center(); } });

// Sidebar
const list = document.getElementById('list');
[...nodes].sort((a, b) => (b.deps.length + b.users.length) - (a.deps.length + a.users.length) || a.name.localeCompare(b.name))
  .forEach(n => {
    const row = document.createElement('div');
    row.className = 'item ' + n.level;
    row.dataset.id = n.id;
    row.dataset.name = n.name.toLowerCase();
    row.title = n.group;
    row.innerHTML = `<span class=""dot""></span><span class=""name"">${n.name}</span><span class=""num"">${n.deps.length} out · ${n.users.length} in</span>`;
    row.onclick = () => show(focused === n.id ? null : n.id);
    list.appendChild(row);
  });
document.getElementById('search').addEventListener('input', e => {
  const q = e.target.value.trim().toLowerCase();
  list.querySelectorAll('.item').forEach(r => r.style.display = r.dataset.name.includes(q) ? '' : 'none');
});

// Export
function download(blob, name) {
  const a = document.createElement('a');
  a.href = URL.createObjectURL(blob);
  a.download = name;
  a.click();
  setTimeout(() => URL.revokeObjectURL(a.href), 1000);
}
document.getElementById('export-svg').onclick = () => download(new Blob([currentSvg], { type: 'image/svg+xml' }), currentName + '.svg');
document.getElementById('export-mmd').onclick = () => download(new Blob([currentSource], { type: 'text/plain' }), currentName + '.mmd');
document.getElementById('export-png').onclick = () => {
  const doc = new DOMParser().parseFromString(currentSvg, 'image/svg+xml').documentElement;
  const vb = (doc.getAttribute('viewBox') || '0 0 1200 800').split(/\s+/).map(Number);
  const scale = 2, w = Math.ceil(vb[2]), h = Math.ceil(vb[3]);
  doc.setAttribute('width', w); doc.setAttribute('height', h);
  const img = new Image();
  img.onload = () => {
    const canvas = document.createElement('canvas');
    canvas.width = w * scale; canvas.height = h * scale;
    const ctx = canvas.getContext('2d');
    ctx.fillStyle = '#ffffff';
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
    canvas.toBlob(b => download(b, currentName + '.png'), 'image/png');
  };
  img.src = 'data:image/svg+xml;charset=utf-8,' + encodeURIComponent(new XMLSerializer().serializeToString(doc));
};

show(null);
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

            int groupIndex = 0;
            foreach (var group in graph.Nodes.Values.GroupBy(GetGroupName).OrderBy(g => g.Key))
            {
                sb.AppendLine($"    subgraph grp{groupIndex++}[\"{Escape(group.Key)}\"]");
                sb.AppendLine("        direction TB");
                foreach (var node in group.OrderBy(n => n.Name))
                {
                    sb.AppendLine($"        {NodeId(node.Name)}[\"{Escape(node.Name)}<br/>{node.OutgoingDependenciesCount} out · {node.IncomingDependentsCount} in\"]");
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
        /// The diagram itself is generated in the browser from the embedded graph data,
        /// which is what allows the viewer to isolate a single type on demand.
        /// </summary>
        public static string ExportToHtmlViewer(DependencyGraph graph, string destinationPath)
        {
            int edgeCount = graph?.Nodes.Values.Sum(n => n.Dependencies.Count(d => graph.Nodes.ContainsKey(d))) ?? 0;

            string html = HtmlTemplate
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
                string deps = string.Join(",", n.Dependencies
                    .Where(d => graph.Nodes.ContainsKey(d))
                    .Select(d => $"\"{NodeId(d)}\""));

                items.Add(
                    $"{{\"id\":\"{NodeId(n.Name)}\",\"name\":\"{JsonEscape(n.Name)}\",\"group\":\"{JsonEscape(GetGroupName(n))}\"," +
                    $"\"level\":\"{GetLevel(n)}\",\"deps\":[{deps}]}}");
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

        // The "n" prefix keeps names like "end" or "graph" from colliding with Mermaid keywords.
        private static string NodeId(string raw) => "n" + Sanitize(raw);

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
