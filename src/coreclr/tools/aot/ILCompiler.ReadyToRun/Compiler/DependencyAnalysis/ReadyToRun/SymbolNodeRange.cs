// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.Text;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// Provides a machanism to represent a contiguous range of object nodes as a single node where the <see cref="RelocType.IMAGE_REL_SYMBOL_SIZE" /> reloc can refer to the range between two nodes.
    /// </summary>
    public sealed class SymbolNodeRange(string name) : DependencyNodeCore<NodeFactory>, ISymbolRangeNode
    {
        private ISortableSymbolNode _startNode;
        private ISortableSymbolNode _endNode;

        protected override string GetName(NodeFactory context) => name;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append($"__{name}");
        }

        public void OnNodeInRangeMarked(ISortableSymbolNode node)
        {
            if (_startNode is null
                || CompilerComparer.Instance.Compare(node, _startNode) <= 0)
            {
                _startNode = node;
            }

            if (_endNode is null
                || CompilerComparer.Instance.Compare(node, _endNode) > 0)
            {
                _endNode = node;
            }
        }

        public ISymbolNode StartNode(NodeFactory factory) => _startNode;
        public ISymbolNode EndNode(NodeFactory factory) => _endNode;

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public int Offset => 0;
        public bool RepresentsIndirectionCell => false;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => [];
        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context) => [];
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => [];

    }
}
