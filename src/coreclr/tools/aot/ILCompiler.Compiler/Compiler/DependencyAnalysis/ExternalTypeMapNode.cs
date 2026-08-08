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
        private readonly IEnumerable<KeyValuePair<string, (TypeDesc targetType, List<TypeDesc> trimmingTargetTypes)>> _mapEntries;

        public ExternalTypeMapNode(TypeDesc typeMapGroup, IEnumerable<KeyValuePair<string, (TypeDesc targetType, List<TypeDesc> trimmingTargetTypes)>> mapEntries)
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
            List<CombinedDependencyListEntry> dependencies = [];

            foreach (var entry in _mapEntries)
            {
                var (targetType, trimmingTargetTypes) = entry.Value;
                bool unconditional = false;
                foreach (var trimTarget in trimmingTargetTypes)
                {
                    if (trimTarget is null)
                    {
                        unconditional = true;
                        break;
                    }
                }
                if (unconditional)
                    continue;
                foreach (var trimmingTargetType in trimmingTargetTypes)
                {
                    IEETypeNode effectiveTrimTargetType = GetEffectiveTrimTargetType(context, trimmingTargetType);

                    dependencies.Add(new CombinedDependencyListEntry(
                        context.MetadataTypeSymbol(targetType),
                        effectiveTrimTargetType,
                        "Type in external type map is cast target"));

                    RuntimeConstructableTypeDependencies.AddTypeLoaderDependencies(dependencies, context, effectiveTrimTargetType, "External type map trim target that could be loaded at runtime");
                }
            }

            return dependencies;
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context)
        {
            foreach (var entry in _mapEntries)
            {
                var (targetType, trimmingTargetTypes) = entry.Value;
                foreach (var trimTarget in trimmingTargetTypes)
                {
                    if (trimTarget is null)
                    {
                        yield return new DependencyListEntry(
                            context.MetadataTypeSymbol(targetType),
                            "External type map entry target type");
                        break;
                    }
                }
            }
        }

        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => Array.Empty<CombinedDependencyListEntry>();
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
                var (targetType, trimmingTargetTypes) = entry.Value;

                foreach (var trimmingTargetType in trimmingTargetTypes)
                {
                    if (trimmingTargetType is null
                        || GetEffectiveTrimTargetType(factory, trimmingTargetType).Marked)
                    {
                        IEETypeNode targetNode = factory.MetadataTypeSymbol(targetType);
                        Debug.Assert(targetNode.Marked);
                        yield return (entry.Key, targetNode);
                        break;
                    }
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
                Vertex valueVertex = externalReferences.EncodeReferenceToType(writer, valueNode.Type);
                Vertex entry = writer.GetTuple(keyVertex, valueVertex);
                typeMapHashTable.Append((uint)TypeHashingAlgorithms.ComputeNameHashCode(key), section.Place(entry));
            }

            Vertex typeMapStateVertex = writer.GetUnsignedConstant(1); // Valid type map state
            Vertex typeMapGroupVertex = externalReferences.EncodeReferenceToType(writer, TypeMapGroup);
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
