// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    internal sealed class AssociatedTypeMapEntryNode(TypeDesc typeMapGroup, TypeDesc key, TypeDesc targetType) : DependencyNodeCore<NodeFactory>, ISortableNode
    {
        public override bool InterestingForDynamicDependencyAnalysis => true;

        public override bool HasDynamicDependencies => false;

        public override bool HasConditionalStaticDependencies => false;

        public override bool StaticDependenciesAreComputed => true;

        public TypeDesc TypeMapGroup { get; } = typeMapGroup;

        public TypeDesc Key { get; } = key;

        public TypeDesc TargetType { get; } = targetType;

        public int ClassCode => 779513676;

        public int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            AssociatedTypeMapEntryNode otherEntry = (AssociatedTypeMapEntryNode)other;
            if (TypeMapGroup != otherEntry.TypeMapGroup)
            {
                return comparer.Compare(TypeMapGroup, otherEntry.TypeMapGroup);
            }
            else if (Key != otherEntry.Key)
            {
                return comparer.Compare(Key, otherEntry.Key);
            }
            else if (TargetType != otherEntry.TargetType)
            {
                return comparer.Compare(TargetType, otherEntry.TargetType);
            }

            return 0;
        }

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => Array.Empty<CombinedDependencyListEntry>();

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context)
        {
            return [
                new DependencyListEntry(context.MaximallyConstructableType(TargetType), "Target type in associated type map")
            ];
        }

        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => Array.Empty<CombinedDependencyListEntry>();
        protected override string GetName(NodeFactory context) => $"AssociatedTypeMapEntryNode({Key}, {TargetType}) in group {TypeMapGroup}";
    }
}
