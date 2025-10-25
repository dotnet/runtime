// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.Text;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// Provides an ISymbolNode for the R2R header table to relocate against when looking up the delay load method call thunks.
    /// They are emitted in a contiguous run of object nodes. This symbol is used in the object writer to represent the range
    /// of bytes containing all the thunks. 
    /// </summary>
    public class DelayLoadMethodCallThunkNodeRange : DependencyNodeCore<NodeFactory>, ISymbolRangeNode
    {
        private const string NodeName = "DelayLoadMethodCallThunkNodeRange";
        private ImportThunk _startNode;
        private ImportThunk _endNode;

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public int Offset => 0;
        public bool RepresentsIndirectionCell => false;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => [];
        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context) => [];
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => [];
        protected override string GetName(NodeFactory context) => NodeName;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append($"__{NodeName}");
        }

        public void OnNodeMarked(DependencyNodeCore<NodeFactory> node)
        {
            if (node is ImportThunk thunk)
            {
                if (_startNode is null
                    || CompilerComparer.Instance.Compare(thunk, _startNode) <= 0)
                {
                    _startNode = thunk;
                }

                if (_endNode is null
                    || CompilerComparer.Instance.Compare(thunk, _endNode) > 0)
                {
                    _endNode = thunk;
                }
            }
        }

        public ISymbolNode StartNode(NodeFactory factory) => _startNode;
        public ISymbolNode EndNode(NodeFactory factory) => _endNode;
    }
}
