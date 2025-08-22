// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using Internal.NativeFormat;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    internal sealed class AnalyzedProxyTypeMapNode(TypeDesc typeMapGroup, IReadOnlyDictionary<TypeDesc, TypeDesc> entries) : DependencyNodeCore<NodeFactory>, IProxyTypeMapNode
    {
        public TypeDesc TypeMapGroup => typeMapGroup;
        public Vertex CreateTypeMap(NodeFactory factory, NativeWriter writer, Section section, ExternalReferencesTableNode externalReferences)
        {
            VertexHashtable typeMapHashTable = new();

            foreach ((TypeDesc key, TypeDesc type) in entries)
            {
                Vertex keyVertex = writer.GetUnsignedConstant(externalReferences.GetIndex(factory.MaximallyConstructableType(key)));
                Vertex valueVertex = writer.GetUnsignedConstant(externalReferences.GetIndex(factory.MaximallyConstructableType(type)));
                Vertex entry = writer.GetTuple(keyVertex, valueVertex);
                typeMapHashTable.Append((uint)key.GetHashCode(), section.Place(entry));
            }

            Vertex typeMapStateVertex = writer.GetUnsignedConstant(1); // Valid type map state
            Vertex typeMapGroupVertex = writer.GetUnsignedConstant(externalReferences.GetIndex(factory.NecessaryTypeSymbol(TypeMapGroup)));
            Vertex tuple = writer.GetTuple(typeMapGroupVertex, typeMapStateVertex, typeMapHashTable);
            return section.Place(tuple);
        }

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => [];
        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context)
        {
            foreach (var (sourceType, proxyType) in entries)
            {
                yield return new DependencyListEntry(context.MaximallyConstructableType(sourceType), "Analyzed proxy type map entry source type");
                yield return new DependencyListEntry(context.MaximallyConstructableType(proxyType), "Analyzed proxy type map entry proxy type");
            }
        }
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => [];
        protected override string GetName(NodeFactory context) => $"Analyzed Proxy Type Map: {typeMapGroup}";
        public IProxyTypeMapNode ToAnalysisBasedNode(NodeFactory factory) => this;
        public override bool InterestingForDynamicDependencyAnalysis => false;

        public override bool HasDynamicDependencies => false;

        public override bool HasConditionalStaticDependencies => false;

        public override bool StaticDependenciesAreComputed => true;

        public int ClassCode => 171742984;

        public int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            AnalyzedProxyTypeMapNode otherEntry = (AnalyzedProxyTypeMapNode)other;
            return comparer.Compare(TypeMapGroup, otherEntry.TypeMapGroup);
        }
    }
}
