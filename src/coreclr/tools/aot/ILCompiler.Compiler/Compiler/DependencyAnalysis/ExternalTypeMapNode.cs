// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using ILCompiler.DependencyAnalysisFramework;
using Internal.NativeFormat;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    internal sealed class ExternalTypeMapNode : SortableDependencyNode, IExternalTypeMapNode
    {
        private readonly IEnumerable<KeyValuePair<string, TypeMapMetadata.ExternalTypeMapEntry>> _mapEntries;

        /// <param name="flavors">
        /// The values the entries of the map carry. A node in the dependency graph has a single flavor. A node
        /// with more flavors only exists at emission time and describes all flavors requested for a group.
        /// </param>
        /// <param name="valueFlavors">The typed value flavors (if <paramref name="flavors"/> has <see cref="TypeMapFlavors.Values"/>).</param>
        public ExternalTypeMapNode(TypeDesc typeMapGroup, IEnumerable<KeyValuePair<string, TypeMapMetadata.ExternalTypeMapEntry>> mapEntries, TypeMapFlavors flavors = TypeMapFlavors.Types, IReadOnlyList<TypeMapValueFlavor> valueFlavors = null)
        {
            _mapEntries = mapEntries;
            TypeMapGroup = typeMapGroup;
            Flavors = flavors;
            ValueFlavors = valueFlavors ?? [];
            Debug.Assert(((flavors & TypeMapFlavors.Values) != 0) == (ValueFlavors.Count != 0));
        }

        public IReadOnlyList<TypeMapValueFlavor> ValueFlavors { get; }

        public override bool InterestingForDynamicDependencyAnalysis => false;

        public override bool HasDynamicDependencies => false;

        public override bool HasConditionalStaticDependencies => true;

        public override bool StaticDependenciesAreComputed => true;

        public TypeDesc TypeMapGroup { get; }

        public TypeMapFlavors Flavors { get; }

        public IEnumerable<KeyValuePair<string, TypeMapMetadata.ExternalTypeMapEntry>> MapEntries => _mapEntries;

        public override void AddConditionalDependencies(DependencySink sink, NodeFactory context)
        {
            foreach (var entry in _mapEntries)
            {
                TypeDesc targetType = entry.Value.Type;
                TypeDesc trimmingTargetType = entry.Value.TrimmingType;
                if (trimmingTargetType is not null)
                {
                    IEETypeNode effectiveTrimTargetType = GetEffectiveTrimTargetType(context, trimmingTargetType);

                    if ((Flavors & TypeMapFlavors.Types) != 0)
                    {
                        sink.Add(new CombinedDependencyListEntry(
                            context.MetadataTypeSymbol(targetType),
                            effectiveTrimTargetType,
                            "Type in external type map is cast target"));
                    }

                    foreach (TypeMapValueFlavor valueFlavor in ValueFlavors)
                    {
                        object dependency = valueFlavor.Evaluate(targetType).GetDependencyNode(context, TypeMapGroup, isProxy: false);
                        if (dependency is not null)
                            sink.Add(new CombinedDependencyListEntry(dependency, effectiveTrimTargetType, "Typed value of type in external type map is cast target"));
                    }

                    RuntimeConstructableTypeDependencies.AddTypeLoaderDependencies(sink, context, effectiveTrimTargetType, "External type map trim target that could be loaded at runtime");
                }
            }

        }

        public override void AddStaticDependencies(DependencySink sink, NodeFactory context)
        {
            sink.Add(context.NecessaryTypeSymbol(TypeMapGroup), "Type map group");

            foreach (var entry in _mapEntries)
            {
                TypeDesc targetType = entry.Value.Type;
                TypeDesc trimmingTargetType = entry.Value.TrimmingType;
                if (trimmingTargetType is null)
                {
                    if ((Flavors & TypeMapFlavors.Types) != 0)
                    {
                        sink.Add(
                            context.MetadataTypeSymbol(targetType),
                            "External type map entry target type");
                    }

                    foreach (TypeMapValueFlavor valueFlavor in ValueFlavors)
                    {
                        object dependency = valueFlavor.Evaluate(targetType).GetDependencyNode(context, TypeMapGroup, isProxy: false);
                        if (dependency is not null)
                            sink.Add(dependency, "Type map value");
                    }
                }
            }
        }

        public override void SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, DependencySink sink, NodeFactory context) { }
        protected override string GetName(NodeFactory context) => Flavors == TypeMapFlavors.Types
            ? $"External type map: {TypeMapGroup}"
            : $"External type map ({Flavors}{string.Concat(ValueFlavors.Select(f => ", " + f.ValueType))}): {TypeMapGroup}";

        public override int ClassCode => -785190502;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            ExternalTypeMapNode otherEntry = (ExternalTypeMapNode)other;
            int result = comparer.Compare(TypeMapGroup, otherEntry.TypeMapGroup);
            if (result != 0)
                return result;

            result = Flavors.CompareTo(otherEntry.Flavors);
            if (result != 0)
                return result;

            // Nodes for different typed value flavors of a group are different nodes.
            for (int i = 0; i < ValueFlavors.Count && i < otherEntry.ValueFlavors.Count; i++)
            {
                result = comparer.Compare(ValueFlavors[i].ValueType, otherEntry.ValueFlavors[i].ValueType);
                if (result != 0)
                    return result;
            }

            return ValueFlavors.Count.CompareTo(otherEntry.ValueFlavors.Count);
        }

        // The entries of all flavors are present under the same conditions, so one set of entries serves all of them.
        private IEnumerable<(string Name, TypeDesc target)> GetMarkedEntries(NodeFactory factory)
        {
            foreach (var entry in _mapEntries)
            {
                TypeDesc targetType = entry.Value.Type;
                TypeDesc trimmingTargetType = entry.Value.TrimmingType;

                if (trimmingTargetType is null
                    || GetEffectiveTrimTargetType(factory, trimmingTargetType).Marked)
                {
                    if ((Flavors & TypeMapFlavors.Types) != 0)
                        Debug.Assert(factory.MetadataTypeSymbol(targetType).Marked);
                    foreach (TypeMapValueFlavor valueFlavor in ValueFlavors)
                    {
                        object dependency = valueFlavor.Evaluate(targetType).GetDependencyNode(factory, TypeMapGroup, isProxy: false);
                        Debug.Assert(dependency is null || ((DependencyNodeCore<NodeFactory>)dependency).Marked);
                    }
                    yield return (entry.Key, targetType);
                }
            }
        }

        private static IEETypeNode GetEffectiveTrimTargetType(NodeFactory factory, TypeDesc trimmingTargetType)
            => RuntimeConstructableTypeDependencies.GetEffectiveTrimTargetType(factory, trimmingTargetType, conditionConstructed: false);

        public Vertex CreateTypeMap(NodeFactory factory, NativeWriter writer, Section section, INativeFormatTypeReferenceProvider externalReferences)
        {
            VertexHashtable typeMapHashTable = new();
            var ranks = TypeMapValueSupport.GetRanks(factory, TypeMapGroup, isProxy: false, ValueFlavors);

            foreach ((string key, TypeDesc target) in GetMarkedEntries(factory))
            {
                Vertex entry = TypeMapValueSupport.CreateExternalEntry(factory, writer, externalReferences, Flavors, key, target);
                entry = TypeMapValueSupport.AppendEntryData(factory, writer, entry, ValueFlavors, ranks, target);
                typeMapHashTable.Append((uint)TypeHashingAlgorithms.ComputeNameHashCode(key), section.Place(entry));
            }

            return section.Place(TypeMapValueSupport.CreateGroupHeader(factory, writer, externalReferences, TypeMapGroup, isProxy: false, Flavors, ValueFlavors, typeMapHashTable));
        }

        public IExternalTypeMapNode ToAnalysisBasedNode(NodeFactory factory)
        {
            // Sorted so that the order of the entries in the image does not depend on the hash codes of the strings (these are randomized).
            ImmutableSortedDictionary<string, TypeDesc> entries = GetMarkedEntries(factory).ToImmutableSortedDictionary(p => p.Name, p => p.target, StringComparer.Ordinal);

            // The values are evaluated once, in the phase that analyzes the program. The result is what ends up in the image.
            List<TypeMapValueFlavor> valueFlavors = [];
            foreach (TypeMapValueFlavor flavor in ValueFlavors)
            {
                Dictionary<TypeDesc, TypeMapValueEvaluation> evaluations = [];
                foreach (TypeDesc target in entries.Values)
                    evaluations[target] = flavor.Evaluate(target);
                valueFlavors.Add(new TypeMapValueFlavor(flavor.ValueType, target => evaluations[target]));
            }

            return new AnalyzedExternalTypeMapNode(TypeMapGroup, Flavors, entries, valueFlavors);
        }
    }
}
