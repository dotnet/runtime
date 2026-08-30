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
    internal sealed class ExternalTypeMapNode : SortableDependencyNode, IExternalTypeMapNode
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

        public override void AddConditionalDependencies(DependencySink<NodeFactory> sink, NodeFactory context)
        {
            DependencySink<NodeFactory> dependencies = sink;

            foreach (var entry in _mapEntries)
            {
                var (targetType, trimmingTargetType) = entry.Value;
                if (trimmingTargetType is not null)
                {
                    IEETypeNode effectiveTrimTargetType = GetEffectiveTrimTargetType(context, trimmingTargetType);

                    dependencies.Add(new CombinedDependencyListEntry(
                        context.MetadataTypeSymbol(targetType),
                        effectiveTrimTargetType,
                        "Type in external type map is cast target"));

                    RuntimeConstructableTypeDependencies.AddTypeLoaderDependencies(dependencies, context, effectiveTrimTargetType, "External type map trim target that could be loaded at runtime");
                }
            }

        }

        public override void AddStaticDependencies(DependencySink<NodeFactory> sink, NodeFactory context)
        {
            foreach (var entry in _mapEntries)
            {
                var (targetType, trimmingTargetType) = entry.Value;
                if (trimmingTargetType is null)
                {
                    sink.Add(
                        context.MetadataTypeSymbol(targetType),
                        "External type map entry target type");
                }
            }
        }

        public override void SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, DependencySink<NodeFactory> sink, NodeFactory context) { }
        protected override string GetName(NodeFactory context) => $"External type map: {TypeMapGroup}";

        public override int ClassCode => -785190502;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            ExternalTypeMapNode otherEntry = (ExternalTypeMapNode)other;
            return comparer.Compare(TypeMapGroup, otherEntry.TypeMapGroup);
        }

        private IEnumerable<(string Name, IEETypeNode target)> GetMarkedEntries(NodeFactory factory)
        {
            foreach (var entry in _mapEntries)
            {
                var (targetType, trimmingTargetType) = entry.Value;

                if (trimmingTargetType is null
                    || GetEffectiveTrimTargetType(factory, trimmingTargetType).Marked)
                {
                    IEETypeNode targetNode = factory.MetadataTypeSymbol(targetType);
                    Debug.Assert(targetNode.Marked);
                    yield return (entry.Key, targetNode);
                }
            }
        }

        private static IEETypeNode GetEffectiveTrimTargetType(NodeFactory factory, TypeDesc trimmingTargetType)
            => RuntimeConstructableTypeDependencies.GetEffectiveTrimTargetType(factory, trimmingTargetType, conditionConstructed: false);

        public Vertex CreateTypeMap(NodeFactory factory, NativeWriter writer, Section section, INativeFormatTypeReferenceProvider externalReferences)
        {
            VertexHashtable typeMapHashTable = new();

            foreach ((string key, IEETypeNode valueNode) in GetMarkedEntries(factory))
            {
                Vertex keyVertex = writer.GetStringConstant(key);
                Vertex valueVertex = externalReferences.EncodeReferenceToType(writer, valueNode.Type, null);
                Vertex entry = writer.GetTuple(keyVertex, valueVertex);
                typeMapHashTable.Append((uint)TypeHashingAlgorithms.ComputeNameHashCode(key), section.Place(entry));
            }

            Vertex typeMapStateVertex = writer.GetUnsignedConstant(1); // Valid type map state
            Vertex typeMapGroupVertex = externalReferences.EncodeReferenceToType(writer, TypeMapGroup, null);
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
