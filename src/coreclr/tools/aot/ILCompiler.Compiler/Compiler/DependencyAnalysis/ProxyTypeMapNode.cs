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
    internal sealed class ProxyTypeMapNode(TypeDesc typeMapGroup, IEnumerable<KeyValuePair<TypeDesc, TypeDesc>> mapEntries) : DependencyNodeCore<NodeFactory>, IProxyTypeMapNode
    {
        public TypeDesc TypeMapGroup { get; } = typeMapGroup;

        public IEnumerable<KeyValuePair<TypeDesc, TypeDesc>> MapEntries => mapEntries;
        public override bool InterestingForDynamicDependencyAnalysis => false;

        public override bool HasDynamicDependencies => false;

        public override bool HasConditionalStaticDependencies => true;

        public override bool StaticDependenciesAreComputed => true;

        public int ClassCode => 779513676;

        public int CompareToImpl(ISortableNode other, CompilerComparer comparer) => comparer.Compare(TypeMapGroup, ((ProxyTypeMapNode)other).TypeMapGroup);

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context)
        {
            List<CombinedDependencyListEntry> entries = [];
            foreach (var (key, value) in mapEntries)
            {
                entries.Add(new CombinedDependencyListEntry(
                    context.MaximallyConstructableType(value),
                    context.MaximallyConstructableType(key),
                    "Proxy type map entry"));
            }

            return entries;
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context) => Array.Empty<DependencyListEntry>();
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => Array.Empty<CombinedDependencyListEntry>();
        protected override string GetName(NodeFactory context) => $"Proxy type map: {TypeMapGroup}";

        public IEnumerable<(IEETypeNode key, IEETypeNode value)> GetMarkedEntries(NodeFactory factory)
        {
            List<(IEETypeNode key, IEETypeNode value)> markedEntries = [];
            foreach (var (key, value) in MapEntries)
            {
                IEETypeNode keyNode = factory.MaximallyConstructableType(key);
                if (keyNode.Marked)
                {
                    IEETypeNode valueNode = factory.MaximallyConstructableType(value);
                    Debug.Assert(valueNode.Marked);
                    markedEntries.Add((keyNode, valueNode));
                }
            }

            return markedEntries;
        }
    }
}
