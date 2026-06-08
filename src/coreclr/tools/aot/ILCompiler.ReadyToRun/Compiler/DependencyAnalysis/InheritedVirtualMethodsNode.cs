// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// Type discovery marker node for virtual dispatch dependency analysis.
    ///
    /// Marked InterestingForDynamicDependencyAnalysis so that GVMDependenciesNode
    /// can iterate on these type nodes to discover new virtual method targets to include in compilation.
    /// </summary>
    public class InheritedVirtualMethodsNode : DependencyNodeCore<NodeFactory>
    {
        private readonly TypeDesc _type;

        public InheritedVirtualMethodsNode(TypeDesc type)
        {
            Debug.Assert(type.IsDefType && !type.IsInterface);
            _type = type;
        }

        public TypeDesc Type => _type;

        public override bool InterestingForDynamicDependencyAnalysis => true;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => null;
        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context) => null;

        protected override string GetName(NodeFactory factory) => $"Inherited virtual methods on {_type}";
    }
}
