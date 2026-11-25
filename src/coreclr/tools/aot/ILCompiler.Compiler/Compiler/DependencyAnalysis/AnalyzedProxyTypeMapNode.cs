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
    internal sealed class AnalyzedProxyTypeMapNode(TypeDesc typeMapGroup, TypeMapFlavors flavors, IReadOnlyDictionary<TypeDesc, TypeDesc> entries, IReadOnlyList<TypeMapValueFlavor> valueFlavors) : SortableDependencyNode, IProxyTypeMapNode
    {
        public TypeDesc TypeMapGroup => typeMapGroup;

        public Vertex CreateTypeMap(NodeFactory factory, NativeWriter writer, Section section, INativeFormatTypeReferenceProvider externalReferences)
        {
            VertexHashtable typeMapHashTable = new();
            var ranks = TypeMapValueSupport.GetRanks(factory, TypeMapGroup, isProxy: true, valueFlavors);

            foreach ((TypeDesc key, TypeDesc type) in entries)
            {
                Vertex entry = TypeMapValueSupport.CreateProxyEntry(writer, externalReferences, flavors, key, type);
                entry = TypeMapValueSupport.AppendEntryData(factory, writer, entry, valueFlavors, ranks, type);
                typeMapHashTable.Append((uint)key.GetHashCode(), section.Place(entry));
            }

            return section.Place(TypeMapValueSupport.CreateGroupHeader(factory, writer, externalReferences, TypeMapGroup, isProxy: true, flavors, valueFlavors, typeMapHashTable));
        }

        public override void AddConditionalDependencies(DependencySink sink, NodeFactory context) { }
        public override void AddStaticDependencies(DependencySink sink, NodeFactory context)
        {
            sink.Add(context.NecessaryTypeSymbol(TypeMapGroup), "Type map group");

            foreach (TypeMapValueFlavor valueFlavor in valueFlavors)
                sink.Add(context.NecessaryTypeSymbol(valueFlavor.ValueType), "Analyzed proxy type map value type");

            foreach (var (sourceType, proxyType) in entries)
            {
                sink.Add(context.MaximallyConstructableType(sourceType), "Analyzed proxy type map entry source type");
                if ((flavors & TypeMapFlavors.Types) != 0)
                    sink.Add(context.MetadataTypeSymbol(proxyType), "Analyzed proxy type map entry proxy type");
                foreach (TypeMapValueFlavor valueFlavor in valueFlavors)
                {
                    object dependency = valueFlavor.Evaluate(proxyType).GetDependencyNode(context, TypeMapGroup, isProxy: true);
                    if (dependency is not null)
                        sink.Add(dependency, "Analyzed type map value");
                }
            }
        }
        public override void SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, DependencySink sink, NodeFactory context) { }
        protected override string GetName(NodeFactory context) => $"Analyzed Proxy Type Map: {typeMapGroup}";
        public IProxyTypeMapNode ToAnalysisBasedNode(NodeFactory factory) => this;
        public override bool InterestingForDynamicDependencyAnalysis => false;

        public override bool HasDynamicDependencies => false;

        public override bool HasConditionalStaticDependencies => false;

        public override bool StaticDependenciesAreComputed => true;

        public override int ClassCode => 171742984;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            AnalyzedProxyTypeMapNode otherEntry = (AnalyzedProxyTypeMapNode)other;
            return comparer.Compare(TypeMapGroup, otherEntry.TypeMapGroup);
        }
    }
}
