// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    internal sealed class InvalidExternalTypeMapNode(TypeDesc typeMapGroup, MethodDesc throwingMethodStub) : DependencyNodeCore<NodeFactory>, ISortableNode, IExternalTypeMapNode
    {
        public override bool InterestingForDynamicDependencyAnalysis => false;

        public override bool HasDynamicDependencies => false;

        public override bool HasConditionalStaticDependencies => false;

        public override bool StaticDependenciesAreComputed => true;

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => Array.Empty<CombinedDependencyListEntry>();
        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context)
        {
            return [
                new DependencyListEntry(context.MethodEntrypoint(ThrowingMethodStub), "Throwing method stub for invalid type map"),
                ];
        }

        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => Array.Empty<CombinedDependencyListEntry>();
        protected override string GetName(NodeFactory context) => "InvalidExternalTypeMapNode";

        public TypeDesc TypeMapGroup { get; } = typeMapGroup;
        public MethodDesc ThrowingMethodStub { get; } = throwingMethodStub;

        public int ClassCode => 36910224;

        public int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            var otherNode = (InvalidExternalTypeMapNode)other;

            int result = comparer.Compare(TypeMapGroup, otherNode.TypeMapGroup);
            if (result != 0)
                return result;
            return comparer.Compare(ThrowingMethodStub, otherNode.ThrowingMethodStub);
        }
    }
}
