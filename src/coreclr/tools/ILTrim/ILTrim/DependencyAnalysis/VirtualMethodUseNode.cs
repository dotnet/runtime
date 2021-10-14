// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILTrim.DependencyAnalysis
{
    /// <summary>
    /// Represents a virtual method slot that is considered used.
    /// </summary>
    public class VirtualMethodUseNode : DependencyNodeCore<NodeFactory>
    {
        private readonly EcmaMethod _decl;

        public VirtualMethodUseNode(EcmaMethod decl)
        {
            Debug.Assert(decl.IsVirtual);

            // Virtual method use always represents the slot defining method of the virtual.
            // Places that might see virtual methods being used through an override need to normalize
            // to the slot defining method.
            Debug.Assert(MetadataVirtualMethodAlgorithm.FindSlotDefiningMethodForVirtualMethod(decl) == decl);

            _decl = decl;
        }

        protected override string GetName(NodeFactory factory)
        {
            return $"VirtualMethodUse: {_decl}";
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context) => null;
        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;        
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;
    }
}
