using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;

namespace Resenha.Guardian.Editor.Architecture
{
    /// <summary>
    /// Analyzes project scripts and maps type dependencies and coupling metrics.
    /// </summary>
    public class ArchitectureAnalyzer
    {
        private static readonly Regex ClassDeclRegex = new Regex(
            @"\b(?:public|internal|private)?\s*(?:abstract|sealed|static|partial)?\s*(?:class|struct|interface)\s+([A-Za-z0-9_]+)(?:\s*:\s*([^{]+))?",
            RegexOptions.Compiled
        );

        private static readonly Regex NamespaceRegex = new Regex(
            @"\bnamespace\s+([A-Za-z0-9_.]+)",
            RegexOptions.Compiled
        );

        private static readonly Regex GetComponentRegex = new Regex(
            @"GetComponent(?:InChildren|InParent)?\s*<\s*([A-Za-z0-9_]+)\s*>",
            RegexOptions.Compiled
        );

        public DependencyGraph BuildGraph(string rootFolder = "Assets", Action<float, string> onProgress = null)
        {
            var graph = new DependencyGraph();
            string[] scriptGuids = AssetDatabase.FindAssets("t:MonoScript");
            var validScripts = new List<(string Path, string Content)>();

            // 1. Coleta scripts válidos da pasta alvo
            for (int i = 0; i < scriptGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(scriptGuids[i]);
                if (string.IsNullOrEmpty(path) || !path.EndsWith(".cs")) continue;
                if (!path.StartsWith(rootFolder)) continue;
                if (path.Contains("/ThirdParty/") || path.Contains("/Plugins/")) continue;

                if (File.Exists(path))
                {
                    validScripts.Add((path, File.ReadAllText(path)));
                }
            }

            // 2. Primeiro passo: registrar todos os nós (classes/interfaces do projeto)
            var knownTypes = new Dictionary<string, DependencyNode>();

            for (int i = 0; i < validScripts.Count; i++)
            {
                var (path, content) = validScripts[i];
                string ns = "Global";
                var nsMatch = NamespaceRegex.Match(content);
                if (nsMatch.Success)
                {
                    ns = nsMatch.Groups[1].Value.Trim();
                }

                var classMatches = ClassDeclRegex.Matches(content);
                foreach (Match match in classMatches)
                {
                    string className = match.Groups[1].Value.Trim();
                    var node = new DependencyNode
                    {
                        Name = className,
                        NamespaceName = ns,
                        FilePath = path
                    };

                    if (match.Groups[2].Success)
                    {
                        string inheritanceList = match.Groups[2].Value.Trim();
                        string[] parts = inheritanceList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int p = 0; p < parts.Length; p++)
                        {
                            string item = parts[p].Trim();
                            int genericIdx = item.IndexOf('<');
                            if (genericIdx > 0) item = item.Substring(0, genericIdx).Trim();

                            if (p == 0 && !item.StartsWith("I"))
                            {
                                node.BaseTypeName = item;
                            }
                            else
                            {
                                node.ImplementedInterfaces.Add(item);
                            }
                        }
                    }

                    // Desconsiderar ScriptableObjects da arquitetura para focar em lógica de runtime/sistemas
                    if (node.BaseTypeName == "ScriptableObject")
                    {
                        continue;
                    }

                    knownTypes[className] = node;
                    graph.AddNode(node);
                }
            }

            // 3. Segundo passo: mapear dependências e conexões entre nós conhecidos
            int total = validScripts.Count;
            for (int i = 0; i < total; i++)
            {
                onProgress?.Invoke((float)i / Math.Max(1, total), $"Mapping relations ({i + 1}/{total})...");
                var (path, content) = validScripts[i];

                foreach (var sourceNode in knownTypes.Values)
                {
                    if (sourceNode.FilePath != path) continue;

                    // Herança e interfaces
                    if (!string.IsNullOrEmpty(sourceNode.BaseTypeName) && knownTypes.ContainsKey(sourceNode.BaseTypeName))
                    {
                        graph.AddEdge(sourceNode.Name, sourceNode.BaseTypeName);
                    }

                    foreach (var iface in sourceNode.ImplementedInterfaces)
                    {
                        if (knownTypes.ContainsKey(iface))
                        {
                            graph.AddEdge(sourceNode.Name, iface);
                        }
                    }

                    // GetComponent<T>
                    var getCompMatches = GetComponentRegex.Matches(content);
                    foreach (Match m in getCompMatches)
                    {
                        string targetType = m.Groups[1].Value.Trim();
                        if (knownTypes.ContainsKey(targetType))
                        {
                            graph.AddEdge(sourceNode.Name, targetType);
                        }
                    }

                    // Referências de campo, instanciações e chamadas estáticas
                    foreach (var targetTypeName in knownTypes.Keys)
                    {
                        if (targetTypeName == sourceNode.Name) continue;

                        // Padrão que busca menção ao tipo como identificador (campo, tipo de retorno, new, etc.)
                        string pattern = $@"\b{Regex.Escape(targetTypeName)}\b";
                        if (Regex.IsMatch(content, pattern))
                        {
                            graph.AddEdge(sourceNode.Name, targetTypeName);
                        }
                    }
                }
            }

            onProgress?.Invoke(1f, "Architecture graph completed.");
            return graph;
        }
    }
}
