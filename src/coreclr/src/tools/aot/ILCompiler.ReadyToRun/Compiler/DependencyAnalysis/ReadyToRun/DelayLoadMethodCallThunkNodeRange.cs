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
    public class DelayLoadMethodCallThunkNodeRange : DependencyNodeCore<NodeFactory>, ISymbolDefinitionNode
    {
        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public int Offset => 0;
        public bool RepresentsIndirectionCell => false;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null;
        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => null;
        protected override string GetName(NodeFactory context) => "DelayLoadMethodCallThunkNodeRange";

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(GetName(null));
        }
    }
}
