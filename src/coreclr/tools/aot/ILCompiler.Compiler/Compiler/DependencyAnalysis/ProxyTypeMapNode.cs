// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using Internal.NativeFormat;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    internal sealed class ProxyTypeMapNode : DependencyNodeCore<NodeFactory>, IProxyTypeMapNode
    {
        private readonly IEnumerable<KeyValuePair<TypeDesc, TypeDesc>> _mapEntries;

        public ProxyTypeMapNode(TypeDesc typeMapGroup, IEnumerable<KeyValuePair<TypeDesc, TypeDesc>> mapEntries)
        {
            _mapEntries = mapEntries;
            TypeMapGroup = typeMapGroup;
        }

        public TypeDesc TypeMapGroup { get; }

        public IEnumerable<KeyValuePair<TypeDesc, TypeDesc>> MapEntries => _mapEntries;
        public override bool InterestingForDynamicDependencyAnalysis => false;

        public override bool HasDynamicDependencies => false;

        public override bool HasConditionalStaticDependencies => true;

        public override bool StaticDependenciesAreComputed => true;

        public int ClassCode => 779513676;

        public int CompareToImpl(ISortableNode other, CompilerComparer comparer) => comparer.Compare(TypeMapGroup, ((ProxyTypeMapNode)other).TypeMapGroup);

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context)
        {
            foreach (var (key, value) in _mapEntries)
            {
                yield return new CombinedDependencyListEntry(
                    context.MaximallyConstructableType(value),
                    context.MaximallyConstructableType(key),
                    "Proxy type map entry");
            }
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context) => Array.Empty<DependencyListEntry>();
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => Array.Empty<CombinedDependencyListEntry>();
        protected override string GetName(NodeFactory context) => $"Proxy type map: {TypeMapGroup}";

        private IEnumerable<(IEETypeNode key, IEETypeNode value)> GetMarkedEntries(NodeFactory factory)
        {
            foreach (var (key, value) in MapEntries)
            {
                IEETypeNode keyNode = factory.MaximallyConstructableType(key);
                if (keyNode.Marked)
                {
                    IEETypeNode valueNode = factory.MaximallyConstructableType(value);
                    Debug.Assert(valueNode.Marked);
                    yield return (keyNode, valueNode);
                }
            }
        }

        public Vertex CreateTypeMap(NodeFactory factory, NativeWriter writer, Section section, ExternalReferencesTableNode externalReferences)
        {
            VertexHashtable typeMapHashTable = new VertexHashtable();

            foreach ((IEETypeNode keyNode, IEETypeNode valueNode) in GetMarkedEntries(factory))
            {
                Vertex keyVertex = writer.GetUnsignedConstant(externalReferences.GetIndex(keyNode));
                Vertex valueVertex = writer.GetUnsignedConstant(externalReferences.GetIndex(valueNode));
                Vertex entry = writer.GetTuple(keyVertex, valueVertex);
                typeMapHashTable.Append((uint)keyNode.Type.GetHashCode(), section.Place(entry));
            }

            Vertex typeMapStateVertex = writer.GetUnsignedConstant(1); // Valid type map state
            Vertex typeMapGroupVertex = writer.GetUnsignedConstant(externalReferences.GetIndex(factory.NecessaryTypeSymbol(TypeMapGroup)));
            Vertex tuple = writer.GetTuple(typeMapGroupVertex, typeMapStateVertex, typeMapHashTable);
            return section.Place(tuple);
        }

        public IProxyTypeMapNode ToAnalysisBasedNode(NodeFactory factory)
            => new AnalyzedProxyTypeMapNode(
                TypeMapGroup,
                GetMarkedEntries(factory)
                .ToImmutableDictionary(p => p.key.Type, p => p.value.Type));
    }
}
