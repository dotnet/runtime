// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Tracks uses of interface in IL sense: at the level of type definitions.
    /// Trim warning suppressions within the framework prevent us from optimizing
    /// at a smaller granularity (e.g. canonical forms or concrete instantiations).
    /// </summary>
    internal sealed class InterfaceUseNode : DependencyNodeCore<NodeFactory>
    {
        public TypeDesc Type { get; }

        public InterfaceUseNode(TypeDesc type)
        {
            Debug.Assert(type.IsTypeDefinition);
            Debug.Assert(type.IsInterface);
            Type = type;
        }

        protected override string GetName(NodeFactory factory) => $"Interface use: {Type}";

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory) => null;
        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => null;
    }
}
