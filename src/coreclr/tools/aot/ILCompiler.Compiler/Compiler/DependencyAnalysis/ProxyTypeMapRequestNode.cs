// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;
using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    public sealed class ProxyTypeMapRequestNode(TypeDesc typeMapGroup) : DependencyNodeCore<NodeFactory>
    {
        public TypeDesc TypeMapGroup { get; } = typeMapGroup;
        public override bool InterestingForDynamicDependencyAnalysis => false;

        public override bool HasDynamicDependencies => false;

        public override bool HasConditionalStaticDependencies => false;

        public override bool StaticDependenciesAreComputed => true;

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => [];
        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context) => [];
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => [];
        protected override string GetName(NodeFactory context) => $"Proxy type map request: {TypeMapGroup}";
    }
}
