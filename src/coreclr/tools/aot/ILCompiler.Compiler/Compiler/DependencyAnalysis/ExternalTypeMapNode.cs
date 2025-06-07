// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    internal sealed class ExternalTypeMapNode : DependencyNodeCore<NodeFactory>, ISortableNode, IExternalTypeMapNode
    {
        private readonly IEnumerable<KeyValuePair<string, (TypeDesc targetType, TypeDesc trimmingTargetType)>> _mapEntries;

        public ExternalTypeMapNode(TypeDesc typeMapGroup, IEnumerable<KeyValuePair<string, (TypeDesc targetType, TypeDesc trimmingTargetType)>> mapEntries)
        {
            _mapEntries = mapEntries;
            TypeMapGroup = typeMapGroup;
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;

        public override bool HasDynamicDependencies => false;

        public override bool HasConditionalStaticDependencies => true;

        public override bool StaticDependenciesAreComputed => true;

        public TypeDesc TypeMapGroup { get; }

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context)
        {
            List<CombinedDependencyListEntry> entries = [];
            foreach (var entry in _mapEntries)
            {
                var (targetType, trimmingTargetType) = entry.Value;
                entries.Add(new CombinedDependencyListEntry(
                    context.MaximallyConstructableType(targetType),
                    context.NecessaryTypeSymbol(trimmingTargetType),
                    "Type in external type map is cast target"));
                entries.Add(new CombinedDependencyListEntry(
                    context.MaximallyConstructableType(targetType),
                    context.ScannedCastTarget(trimmingTargetType),
                    "Type in external type map is cast target for cast that may have been optimized away"));
            }

            return entries;
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context) => [];

        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => Array.Empty<CombinedDependencyListEntry>();
        protected override string GetName(NodeFactory context) => $"External type map: {TypeMapGroup}";

        public int ClassCode => -785190502;

        public int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            ExternalTypeMapNode otherEntry = (ExternalTypeMapNode)other;
            return comparer.Compare(TypeMapGroup, otherEntry.TypeMapGroup);
        }

        public IEnumerable<(string Name, IEETypeNode target)> GetMarkedEntries(NodeFactory factory)
        {
            List<(string Name, IEETypeNode target)> markedEntries = [];
            foreach (var entry in _mapEntries)
            {
                var (targetType, trimmingTargetType) = entry.Value;
                IEETypeNode trimmingTarget = factory.NecessaryTypeSymbol(trimmingTargetType);
                ScannedCastTargetNode scannedCastTarget = factory.ScannedCastTarget(trimmingTargetType);

                if (trimmingTarget.Marked || scannedCastTarget.Marked)
                {
                    IEETypeNode targetNode = factory.MaximallyConstructableType(targetType);
                    Debug.Assert(targetNode.Marked);
                    markedEntries.Add((entry.Key, targetNode));
                }
            }
            return markedEntries;
        }
    }
}
