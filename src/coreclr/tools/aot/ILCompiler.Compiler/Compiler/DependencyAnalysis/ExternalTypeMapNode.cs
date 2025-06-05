// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    internal sealed class ExternalTypeMapNode(TypeDesc typeMapGroup, IEnumerable<KeyValuePair<string, (TypeDesc targetType, TypeDesc trimmingTargetType)>> mapEntries) : DependencyNodeCore<NodeFactory>
    {
        public override bool InterestingForDynamicDependencyAnalysis => false;

        public override bool HasDynamicDependencies => false;

        public override bool HasConditionalStaticDependencies => true;

        public override bool StaticDependenciesAreComputed => true;

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context)
        {
            List<CombinedDependencyListEntry> entries = [];
            foreach (var entry in mapEntries)
            {
                var targetType = entry.Value.targetType;
                var trimmingTargetType = entry.Value.trimmingTargetType;
                ExternalTypeMapEntryNode node = new(typeMapGroup, entry.Key, targetType);
                entries.Add(new CombinedDependencyListEntry(
                    node,
                    context.NecessaryTypeSymbol(trimmingTargetType),
                    "Type in external type map is cast target"));
                entries.Add(new CombinedDependencyListEntry(
                    node,
                    context.ScannedCastTarget(trimmingTargetType),
                    "Type in external type map is cast target for cast that may have been optimized away"));
            }

            return entries;
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context) => [];

        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => Array.Empty<CombinedDependencyListEntry>();
        protected override string GetName(NodeFactory context) => "External type map";
    }
}
