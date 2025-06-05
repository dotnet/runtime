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
    internal sealed class AssociatedTypeMapNode(TypeDesc typeMapGroup, IEnumerable<KeyValuePair<TypeDesc, TypeDesc>> mapEntries) : DependencyNodeCore<NodeFactory>
    {
        public override bool InterestingForDynamicDependencyAnalysis => false;

        public override bool HasDynamicDependencies => false;

        public override bool HasConditionalStaticDependencies => true;

        public override bool StaticDependenciesAreComputed => true;

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context)
        {
            List<CombinedDependencyListEntry> entries = [];
            foreach (var (key, value) in mapEntries)
            {
                entries.Add(new CombinedDependencyListEntry(
                    new AssociatedTypeMapEntryNode(typeMapGroup, key, value),
                    context.MaximallyConstructableType(key),
                    "Type in associated map may be constructed"));
            }

            return entries;
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context) => Array.Empty<DependencyListEntry>();
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => Array.Empty<CombinedDependencyListEntry>();
        protected override string GetName(NodeFactory context) => "Associated type map";
    }
}
