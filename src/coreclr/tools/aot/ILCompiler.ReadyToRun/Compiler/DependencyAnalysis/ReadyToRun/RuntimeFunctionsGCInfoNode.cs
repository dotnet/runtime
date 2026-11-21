// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class RuntimeFunctionsGCInfoNode : ArrayOfEmbeddedDataNode<MethodGCInfoNode>
    {
        public RuntimeFunctionsGCInfoNode()
            : base("RuntimeFunctionsGCInfo", new EmbeddedObjectNodeComparer(CompilerComparer.Instance))
        {
        }

        public HashSet<MethodGCInfoNode> Deduplicator;
        private ObjectData _wasmData;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly)
        {
            if (!factory.Target.IsWasm || relocsOnly)
            {
                return base.GetData(factory, relocsOnly);
            }

            return _wasmData ??= base.GetData(factory, relocsOnly);
        }

        protected override void SortNodes(List<MethodGCInfoNode> nodes, NodeFactory factory)
        {
            if (!factory.Target.IsWasm || nodes.Count < 2)
            {
                base.SortNodes(nodes, factory);
                return;
            }

            Dictionary<MethodGCInfoNode, int> duplicationCounts = new Dictionary<MethodGCInfoNode, int>(nodes.Count, Deduplicator.Comparer);
            foreach (MethodGCInfoNode node in nodes)
            {
                duplicationCounts.TryGetValue(node, out int count);
                duplicationCounts[node] = count + 1;
            }

            Dictionary<MethodGCInfoNode, int> frequencies = new Dictionary<MethodGCInfoNode, int>(nodes.Count);
            Dictionary<MethodGCInfoNode, int> sizes = new Dictionary<MethodGCInfoNode, int>(nodes.Count);
            foreach (MethodGCInfoNode node in nodes)
            {
                frequencies.Add(node, duplicationCounts[node]);
                sizes.Add(node, node.GetWasmDataSize());
            }

            nodes.MergeSort((x, y) =>
            {
                int result = frequencies[y].CompareTo(frequencies[x]);
                if (result == 0)
                {
                    result = sizes[x].CompareTo(sizes[y]);
                }
                return result != 0 ? result : CompilerComparer.Instance.Compare(x, y);
            });
        }

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;

        public override int ClassCode => (int)ObjectNodeOrder.RuntimeFunctionsGCInfoNode;

        public override ObjectNodeSection GetSection(NodeFactory factory)
        {
            return ObjectNodeSection.ReadOnlyDataSection;
        }

        public override bool StaticDependenciesAreComputed => true;

        public override bool IsShareable => false;
    }
}
