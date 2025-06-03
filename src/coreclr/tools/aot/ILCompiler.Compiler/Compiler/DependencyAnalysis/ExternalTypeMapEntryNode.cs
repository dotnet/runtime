// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    internal sealed class ExternalTypeMapEntryNode(TypeDesc typeMapGroup, string key, TypeDesc targetType) : DependencyNodeCore<NodeFactory>, ISortableNode
    {
        public override bool InterestingForDynamicDependencyAnalysis => true;

        public override bool HasDynamicDependencies => false;

        public override bool HasConditionalStaticDependencies => false;

        public override bool StaticDependenciesAreComputed => true;

        public TypeDesc TypeMapGroup { get; } = typeMapGroup;

        public string Key { get; } = key;

        public TypeDesc TargetType { get; } = targetType;

        public int ClassCode => -785190502;

        public int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            ExternalTypeMapEntryNode otherEntry = (ExternalTypeMapEntryNode)other;
            if (TypeMapGroup != otherEntry.TypeMapGroup)
            {
                return comparer.Compare(TypeMapGroup, otherEntry.TypeMapGroup);
            }
            else if (Key != otherEntry.Key)
            {
                return string.Compare(Key, otherEntry.Key, StringComparison.Ordinal);
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
                new DependencyListEntry(context.MaximallyConstructableType(TargetType), "Target type in external type map")
            ];
        }

        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => Array.Empty<CombinedDependencyListEntry>();
        protected override string GetName(NodeFactory context) => $"ExternalTypeMapEntryNode({Key}, {TargetType}) in group {TypeMapGroup}";
    }
}
