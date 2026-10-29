using System;
using System.Collections.Generic;

namespace Resenha.Guardian.Editor.Architecture
{
    /// <summary>
    /// Represents a code entity (class or struct) within the architectural dependency graph.
    /// </summary>
    [Serializable]
    public class DependencyNode
    {
        public string Name { get; set; }
        public string NamespaceName { get; set; }
        public string FilePath { get; set; }
        public string BaseTypeName { get; set; }
        public HashSet<string> ImplementedInterfaces { get; } = new HashSet<string>();
        public HashSet<string> Dependencies { get; } = new HashSet<string>();
        public HashSet<string> Dependents { get; } = new HashSet<string>();

        public int OutgoingDependenciesCount => Dependencies.Count;
        public int IncomingDependentsCount => Dependents.Count;

        public bool IsHighCoupling => OutgoingDependenciesCount >= 8;
        public bool IsGodObjectCandidate => IncomingDependentsCount >= 10 && OutgoingDependenciesCount >= 6;
    }

    /// <summary>
    /// Complete dependency graph mapping nodes and directional relationships.
    /// </summary>
    [Serializable]
    public class DependencyGraph
    {
        public Dictionary<string, DependencyNode> Nodes { get; } = new Dictionary<string, DependencyNode>();

        public void AddNode(DependencyNode node)
        {
            if (node != null && !Nodes.ContainsKey(node.Name))
            {
                Nodes[node.Name] = node;
            }
        }

        public void AddEdge(string sourceName, string targetName)
        {
            if (string.IsNullOrEmpty(sourceName) || string.IsNullOrEmpty(targetName) || sourceName == targetName)
            {
                return;
            }

            if (Nodes.TryGetValue(sourceName, out var sourceNode) && Nodes.TryGetValue(targetName, out var targetNode))
            {
                sourceNode.Dependencies.Add(targetName);
                targetNode.Dependents.Add(sourceName);
            }
        }
    }
}
