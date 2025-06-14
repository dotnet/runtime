// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using ILCompiler.DependencyAnalysisFramework;
using Internal.NativeFormat;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    internal sealed class ExternalTypeMapNode : DependencyNodeCore<NodeFactory>, IExternalTypeMapNode
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
            foreach (var entry in _mapEntries)
            {
                var (targetType, trimmingTargetType) = entry.Value;
                yield return new CombinedDependencyListEntry(
                    context.MaximallyConstructableType(targetType),
                    context.NecessaryTypeSymbol(trimmingTargetType),
                    "Type in external type map is cast target");
            }
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

        private IEnumerable<(string Name, IEETypeNode target)> GetMarkedEntries(NodeFactory factory)
        {
            foreach (var entry in _mapEntries)
            {
                var (targetType, trimmingTargetType) = entry.Value;

                if (factory.NecessaryTypeSymbol(trimmingTargetType).Marked)
                {
                    IEETypeNode targetNode = factory.MaximallyConstructableType(targetType);
                    Debug.Assert(targetNode.Marked);
                    yield return (entry.Key, targetNode);
                }
            }
        }

        public Vertex CreateTypeMap(NodeFactory factory, NativeWriter writer, Section section, ExternalReferencesTableNode externalReferences)
        {
            VertexHashtable typeMapHashTable = new();

            foreach ((string key, IEETypeNode valueNode) in GetMarkedEntries(factory))
            {
                Vertex keyVertex = writer.GetStringConstant(key);
                Vertex valueVertex = writer.GetUnsignedConstant(externalReferences.GetIndex(valueNode));
                Vertex entry = writer.GetTuple(keyVertex, valueVertex);
                typeMapHashTable.Append((uint)TypeHashingAlgorithms.ComputeNameHashCode(key), section.Place(entry));
            }

            Vertex typeMapStateVertex = writer.GetUnsignedConstant(1); // Valid type map state
            Vertex typeMapGroupVertex = writer.GetUnsignedConstant(externalReferences.GetIndex(factory.NecessaryTypeSymbol(TypeMapGroup)));
            Vertex tuple = writer.GetTuple(typeMapGroupVertex, typeMapStateVertex, typeMapHashTable);
            return section.Place(tuple);
        }

        public IExternalTypeMapNode ToAnalysisBasedNode(NodeFactory factory)
            => new AnalyzedExternalTypeMapNode(
                    TypeMapGroup,
                    GetMarkedEntries(factory)
                    .ToImmutableDictionary(p => p.Name, p => p.target.Type));
    }
}
