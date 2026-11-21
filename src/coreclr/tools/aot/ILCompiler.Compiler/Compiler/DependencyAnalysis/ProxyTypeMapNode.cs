// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using Internal.NativeFormat;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    internal sealed class ProxyTypeMapNode : SortableDependencyNode, IProxyTypeMapNode
    {
        private readonly IEnumerable<KeyValuePair<TypeDesc, TypeMapMetadata.ProxyTypeMapEntry>> _mapEntries;

        /// <param name="flavors">
        /// The values the entries of the map carry. A node in the dependency graph has a single flavor. A node
        /// with more flavors only exists at emission time and describes all flavors requested for a group.
        /// </param>
        /// <param name="valueFlavors">The typed value flavors (if <paramref name="flavors"/> has <see cref="TypeMapFlavors.Values"/>).</param>
        public ProxyTypeMapNode(TypeDesc typeMapGroup, IEnumerable<KeyValuePair<TypeDesc, TypeMapMetadata.ProxyTypeMapEntry>> mapEntries, TypeMapFlavors flavors = TypeMapFlavors.Types, IReadOnlyList<TypeMapValueFlavor> valueFlavors = null)
        {
            _mapEntries = mapEntries;
            TypeMapGroup = typeMapGroup;
            Flavors = flavors;
            ValueFlavors = valueFlavors ?? [];
            Debug.Assert(((flavors & TypeMapFlavors.Values) != 0) == (ValueFlavors.Count != 0));
        }

        public IReadOnlyList<TypeMapValueFlavor> ValueFlavors { get; }

        public TypeDesc TypeMapGroup { get; }

        public TypeMapFlavors Flavors { get; }

        public IEnumerable<KeyValuePair<TypeDesc, TypeMapMetadata.ProxyTypeMapEntry>> MapEntries => _mapEntries;
        public override bool InterestingForDynamicDependencyAnalysis => false;

        public override bool HasDynamicDependencies => false;

        public override bool HasConditionalStaticDependencies => true;

        public override bool StaticDependenciesAreComputed => true;

        public override int ClassCode => 779513676;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            ProxyTypeMapNode otherNode = (ProxyTypeMapNode)other;
            int result = comparer.Compare(TypeMapGroup, otherNode.TypeMapGroup);
            if (result != 0)
                return result;

            result = Flavors.CompareTo(otherNode.Flavors);
            if (result != 0)
                return result;

            // Nodes for different typed value flavors of a group are different nodes.
            for (int i = 0; i < ValueFlavors.Count && i < otherNode.ValueFlavors.Count; i++)
            {
                result = comparer.Compare(ValueFlavors[i].ValueType, otherNode.ValueFlavors[i].ValueType);
                if (result != 0)
                    return result;
            }

            return ValueFlavors.Count.CompareTo(otherNode.ValueFlavors.Count);
        }

        public override void AddConditionalDependencies(DependencySink sink, NodeFactory context)
        {
            foreach (var (key, entry) in _mapEntries)
            {
                if ((Flavors & TypeMapFlavors.Types) != 0)
                {
                    sink.Add(new CombinedDependencyListEntry(
                        context.MetadataTypeSymbol(entry.Type),
                        context.MaximallyConstructableType(key),
                        "Proxy type map entry"));
                }

                foreach (TypeMapValueFlavor valueFlavor in ValueFlavors)
                {
                    object dependency = valueFlavor.Evaluate(entry.Type).GetDependencyNode(context, TypeMapGroup, isProxy: true);
                    if (dependency is not null)
                    {
                        sink.Add(new CombinedDependencyListEntry(
                            dependency,
                            context.MaximallyConstructableType(key),
                            "Proxy type map entry typed value"));
                    }
                }

                // If the key type has a canonical form, it could be created at runtime by the type loader.
                // If there is a type loader template for it, create the generic type instantiation eagerly.
                RuntimeConstructableTypeDependencies.AddTypeLoaderDependencies(
                    sink,
                    context,
                    context.MaximallyConstructableType(key),
                    "Proxy map entry that could be loaded at runtime");
            }
        }

        public override void AddStaticDependencies(DependencySink sink, NodeFactory context)
        {
            sink.Add(context.NecessaryTypeSymbol(TypeMapGroup), "Type map group");
        }
        public override void SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, DependencySink sink, NodeFactory context) { }
        protected override string GetName(NodeFactory context) => Flavors == TypeMapFlavors.Types
            ? $"Proxy type map: {TypeMapGroup}"
            : $"Proxy type map ({Flavors}{string.Concat(ValueFlavors.Select(f => ", " + f.ValueType))}): {TypeMapGroup}";

        // The entries of all flavors are present under the same conditions, so one set of entries serves all of them.
        private IEnumerable<(IEETypeNode key, TypeDesc value)> GetMarkedEntries(NodeFactory factory)
        {
            foreach (var (key, entry) in MapEntries)
            {
                IEETypeNode keyNode = factory.MaximallyConstructableType(key);
                if (keyNode.Marked)
                {
                    if ((Flavors & TypeMapFlavors.Types) != 0)
                        Debug.Assert(factory.MetadataTypeSymbol(entry.Type).Marked);
                    foreach (TypeMapValueFlavor valueFlavor in ValueFlavors)
                    {
                        object dependency = valueFlavor.Evaluate(entry.Type).GetDependencyNode(factory, TypeMapGroup, isProxy: true);
                        Debug.Assert(dependency is null || ((DependencyNodeCore<NodeFactory>)dependency).Marked);
                    }
                    yield return (keyNode, entry.Type);
                }
            }
        }

        public Vertex CreateTypeMap(NodeFactory factory, NativeWriter writer, Section section, INativeFormatTypeReferenceProvider externalReferences)
        {
            VertexHashtable typeMapHashTable = new VertexHashtable();
            var ranks = TypeMapValueSupport.GetRanks(factory, TypeMapGroup, isProxy: true, ValueFlavors);

            foreach ((IEETypeNode keyNode, TypeDesc value) in GetMarkedEntries(factory))
            {
                Vertex entry = TypeMapValueSupport.CreateProxyEntry(writer, externalReferences, Flavors, keyNode.Type, value);
                entry = TypeMapValueSupport.AppendEntryData(factory, writer, entry, ValueFlavors, ranks, value);
                typeMapHashTable.Append((uint)keyNode.Type.GetHashCode(), section.Place(entry));
            }

            return section.Place(TypeMapValueSupport.CreateGroupHeader(factory, writer, externalReferences, TypeMapGroup, isProxy: true, Flavors, ValueFlavors, typeMapHashTable));
        }

        public IProxyTypeMapNode ToAnalysisBasedNode(NodeFactory factory)
        {
            ImmutableDictionary<TypeDesc, TypeDesc> entries = GetMarkedEntries(factory).ToImmutableDictionary(p => p.key.Type, p => p.value);

            // The values are evaluated once, in the phase that analyzes the program. The result is what ends up in the image.
            List<TypeMapValueFlavor> valueFlavors = [];
            foreach (TypeMapValueFlavor flavor in ValueFlavors)
            {
                Dictionary<TypeDesc, TypeMapValueEvaluation> evaluations = [];
                foreach (TypeDesc value in entries.Values)
                    evaluations[value] = flavor.Evaluate(value);
                valueFlavors.Add(new TypeMapValueFlavor(flavor.ValueType, value => evaluations[value]));
            }

            return new AnalyzedProxyTypeMapNode(TypeMapGroup, Flavors, entries, valueFlavors);
        }
    }
}
