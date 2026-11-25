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
    internal sealed class AnalyzedExternalTypeMapNode(TypeDesc typeMapGroup, TypeMapFlavors flavors, IReadOnlyDictionary<string, TypeDesc> entries, IReadOnlyList<TypeMapValueFlavor> valueFlavors) : SortableDependencyNode, IExternalTypeMapNode
    {
        public TypeDesc TypeMapGroup => typeMapGroup;

        public Vertex CreateTypeMap(NodeFactory factory, NativeWriter writer, Section section, INativeFormatTypeReferenceProvider externalReferences)
        {
            VertexHashtable typeMapHashTable = new();
            var ranks = TypeMapValueSupport.GetRanks(factory, TypeMapGroup, isProxy: false, valueFlavors);

            foreach ((string key, TypeDesc type) in entries)
            {
                Vertex entry = TypeMapValueSupport.CreateExternalEntry(writer, externalReferences, flavors, key, type);
                entry = TypeMapValueSupport.AppendEntryData(factory, writer, entry, valueFlavors, ranks, type);
                typeMapHashTable.Append((uint)TypeHashingAlgorithms.ComputeNameHashCode(key), section.Place(entry));
            }

            return section.Place(TypeMapValueSupport.CreateGroupHeader(factory, writer, externalReferences, TypeMapGroup, isProxy: false, flavors, valueFlavors, typeMapHashTable));
        }

        public override void AddConditionalDependencies(DependencySink sink, NodeFactory context) { }
        public override void AddStaticDependencies(DependencySink sink, NodeFactory context)
        {
            sink.Add(context.NecessaryTypeSymbol(TypeMapGroup), "Type map group");

            foreach (TypeMapValueFlavor valueFlavor in valueFlavors)
                sink.Add(context.NecessaryTypeSymbol(valueFlavor.ValueType), "Analyzed external type map value type");

            foreach (TypeDesc targetType in entries.Values)
            {
                if ((flavors & TypeMapFlavors.Types) != 0)
                    sink.Add(context.MetadataTypeSymbol(targetType), "Analyzed external type map entry target type");
                foreach (TypeMapValueFlavor valueFlavor in valueFlavors)
                {
                    object dependency = valueFlavor.Evaluate(targetType).GetDependencyNode(context, TypeMapGroup, isProxy: false);
                    if (dependency is not null)
                        sink.Add(dependency, "Analyzed type map value");
                }
            }
        }
        public override void SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, DependencySink sink, NodeFactory context) { }
        protected override string GetName(NodeFactory context) => $"Analyzed External Type Map: {TypeMapGroup}";
        public IExternalTypeMapNode ToAnalysisBasedNode(NodeFactory factory) => this;

        public override bool InterestingForDynamicDependencyAnalysis => false;

        public override bool HasDynamicDependencies => false;

        public override bool HasConditionalStaticDependencies => false;

        public override bool StaticDependenciesAreComputed => true;
        public override int ClassCode => -874354558;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            AnalyzedExternalTypeMapNode otherEntry = (AnalyzedExternalTypeMapNode)other;
            return comparer.Compare(TypeMapGroup, otherEntry.TypeMapGroup);
        }
    }
}
