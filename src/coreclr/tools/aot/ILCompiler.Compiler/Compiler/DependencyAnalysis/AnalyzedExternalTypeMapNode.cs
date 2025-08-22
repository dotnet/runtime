// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using ILCompiler.DependencyAnalysisFramework;
using Internal.NativeFormat;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    internal sealed class AnalyzedExternalTypeMapNode(TypeDesc typeMapGroup, IReadOnlyDictionary<string, TypeDesc> entries) : DependencyNodeCore<NodeFactory>, IExternalTypeMapNode
    {
        public TypeDesc TypeMapGroup => typeMapGroup;

        public Vertex CreateTypeMap(NodeFactory factory, NativeWriter writer, Section section, ExternalReferencesTableNode externalReferences)
        {
            VertexHashtable typeMapHashTable = new();

            foreach ((string key, TypeDesc type) in entries)
            {
                Vertex keyVertex = writer.GetStringConstant(key);
                Vertex valueVertex = writer.GetUnsignedConstant(externalReferences.GetIndex(factory.MaximallyConstructableType(type)));
                Vertex entry = writer.GetTuple(keyVertex, valueVertex);
                typeMapHashTable.Append((uint)TypeHashingAlgorithms.ComputeNameHashCode(key), section.Place(entry));
            }

            Vertex typeMapStateVertex = writer.GetUnsignedConstant(1); // Valid type map state
            Vertex typeMapGroupVertex = writer.GetUnsignedConstant(externalReferences.GetIndex(factory.NecessaryTypeSymbol(TypeMapGroup)));
            Vertex tuple = writer.GetTuple(typeMapGroupVertex, typeMapStateVertex, typeMapHashTable);
            return section.Place(tuple);
        }

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => [];
        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context)
        {
            foreach (TypeDesc targetType in entries.Values)
            {
                yield return new DependencyListEntry(context.MaximallyConstructableType(targetType), "Analyzed external type map entry target type");
            }
        }
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => [];
        protected override string GetName(NodeFactory context) => $"Analyzed External Type Map: {TypeMapGroup}";
        public IExternalTypeMapNode ToAnalysisBasedNode(NodeFactory factory) => this;

        public override bool InterestingForDynamicDependencyAnalysis => false;

        public override bool HasDynamicDependencies => false;

        public override bool HasConditionalStaticDependencies => false;

        public override bool StaticDependenciesAreComputed => true;
        public int ClassCode => -874354558;

        public int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            AnalyzedExternalTypeMapNode otherEntry = (AnalyzedExternalTypeMapNode)other;
            return comparer.Compare(TypeMapGroup, otherEntry.TypeMapGroup);
        }
    }
}
