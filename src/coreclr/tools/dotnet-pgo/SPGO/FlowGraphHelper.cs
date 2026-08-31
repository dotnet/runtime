// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Internal.IL;

namespace Microsoft.Diagnostics.Tools.Pgo
{
    internal static class FlowGraphHelper
    {
        public static string DumpGraph<T>(
            List<T> nodes,
            T startNode,
            Func<T, HashSet<T>> getSuccessors,
            string title,
            Func<T, string> getNodeLabel,
            Func<(T, T), string> getEdgeLabel)
        {
            var sb = new StringBuilder();
            sb.AppendLine("digraph G {");
            sb.AppendLine("  labelloc=\"t\";");
            sb.AppendLine($"  label=\"{title}\";");
            sb.AppendLine("  forcelabels=true;");
            sb.AppendLine();
            Dictionary<T, int> bbToIndex = new Dictionary<T, int>();
            for (int i = 0; i < nodes.Count; i++)
                bbToIndex.Add(nodes[i], i);

            foreach (T bb in nodes)
            {
                string label = $"{getNodeLabel(bb)}";
                sb.AppendLine($"  N{bbToIndex[bb]} [label=\"{label}\"];");
            }

            sb.AppendLine();

            foreach (T bb in nodes)
            {
                foreach (T tar in getSuccessors(bb))
                {
                    string label = getEdgeLabel((bb, tar));
                    string postfix = string.IsNullOrEmpty(label) ? "" : $" [label=\"{label}\"]";
                    sb.AppendLine($"  N{bbToIndex[bb]} -> N{bbToIndex[tar]}{postfix};");
                }
            }

            // Write ranks with BFS.
            List<T> curRank = new List<T> { startNode };
            HashSet<T> seen = new HashSet<T>(curRank);
            while (curRank.Count > 0)
            {
                sb.AppendLine($"  {{rank = same; {string.Concat(curRank.Select(bb => $"N{bbToIndex[bb]}; "))}}}");
                curRank = curRank.SelectMany(getSuccessors).Where(seen.Add).ToList();
            }

            sb.AppendLine("}");
            return sb.ToString();
        }

        public static string Dump(
            this FlowGraph fg,
            Func<BasicBlock, string> getNodeAnnot,
            Func<(BasicBlock, BasicBlock), string> getEdgeAnnot)
        {
            string getBasicBlockLabel(BasicBlock bb)
            {
                string label = $"[{bb.Start:x}..{bb.Start + bb.Size:x})\\n{getNodeAnnot(bb)}";
                return label;
            }

            return DumpGraph(
                fg.BasicBlocks,
                fg.BasicBlocks.Single(bb => bb.Start == 0),
                bb => bb.Targets,
                "",
                getBasicBlockLabel,
                getEdgeAnnot);

        }
    }
}
