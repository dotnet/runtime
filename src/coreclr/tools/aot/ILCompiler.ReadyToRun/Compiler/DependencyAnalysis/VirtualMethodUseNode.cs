// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// Marker node representing that a non-GVM virtual method slot is used in the program.
    /// It has no dependencies of its own, but is referenced as a condition in
    /// conditional static dependencies from InheritedVirtualMethodsNode to trigger
    /// compilation of specific virtual method implementations.
    /// </summary>
    public class VirtualMethodUseNode : DependencyNodeCore<NodeFactory>
    {
        private readonly MethodDesc _decl;

        public MethodDesc Method => _decl;

        public VirtualMethodUseNode(MethodDesc decl)
        {
            Debug.Assert(!decl.IsRuntimeDeterminedExactMethod);
            Debug.Assert(decl.IsVirtual);
            Debug.Assert(!decl.HasInstantiation);
            Debug.Assert(MetadataVirtualMethodAlgorithm.FindSlotDefiningMethodForVirtualMethod(decl) == decl);

            _decl = decl;
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context) => null;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => null;

        protected override string GetName(NodeFactory factory) => $"VirtualMethodUse {_decl}";
    }
}
