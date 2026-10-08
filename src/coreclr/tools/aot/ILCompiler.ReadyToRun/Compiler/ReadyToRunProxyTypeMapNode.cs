// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using Internal.NativeFormat;
using Internal.TypeSystem;

namespace ILCompiler.ReadyToRun
{
    internal class ReadyToRunProxyTypeMapNode(ModuleDesc triggeringModule, TypeDesc group, TypeMapMetadata.IProxyTypeMap map, ImportReferenceProvider importProvider) : SortableDependencyNode, IProxyTypeMapNode
    {
        public TypeDesc TypeMapGroup => group;

        public override int ClassCode => 210131165;

        public override bool InterestingForDynamicDependencyAnalysis => false;

        public override bool HasDynamicDependencies => false;

        public override bool HasConditionalStaticDependencies => false;

        public override bool StaticDependenciesAreComputed => true;

        private ModuleDesc TriggeringModule { get; } = triggeringModule;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            ReadyToRunProxyTypeMapNode otherNode = (ReadyToRunProxyTypeMapNode)other;
            int result = comparer.Compare(TypeMapGroup, otherNode.TypeMapGroup);
            if (result != 0)
                return result;

            return comparer.Compare(TriggeringModule, otherNode.TriggeringModule);
        }

        public Vertex CreateTypeMap(NodeFactory factory, NativeWriter writer, Section section, INativeFormatTypeReferenceProvider ProxyReferences)
        {
            Vertex typeMapGroupVertex = ProxyReferences.EncodeReferenceToType(writer, TypeMapGroup, TriggeringModule);
            if (map.ThrowingMethodStub is not null)
            {
                // We don't write out the throwing method stub for R2R
                // as emitting loose methods is not supported/very expensive.
                // Also, matching CoreCLR's exact set of exceptions is difficult
                // in the managed type system.
                // Instead, we defer to the runtime to generate the type map
                // and throw on error cases.
                return section.Place(writer.GetTuple(
                    typeMapGroupVertex,
                    writer.GetUnsignedConstant(ReadyToRunTypeMapEncoding.RuntimeAttributeFallback)));
            }

            VertexHashtable typeMapHashTable = new();
            VertexSequence namedEntries = new();
            bool hasNamedEntries = false;

            Section typeMapEntriesSection = writer.NewSection();

            foreach ((TypeDesc type, TypeMapMetadata.ProxyTypeMapEntry mapEntry) in map.TypeMap)
            {
                if (ReadyToRunTypeMapEncoding.IsTypeDescEncodable(factory, TriggeringModule, type) &&
                    ReadyToRunTypeMapEncoding.IsTypeDescEncodable(factory, TriggeringModule, mapEntry.Type))
                {
                    Vertex entry = writer.GetTuple(
                        ProxyReferences.EncodeReferenceToType(writer, type, TriggeringModule),
                        ProxyReferences.EncodeReferenceToType(writer, mapEntry.Type, TriggeringModule));
                    typeMapHashTable.Append((uint)type.GetHashCode(), typeMapEntriesSection.Place(entry));
                }
                else
                {
                    Debug.Assert(TriggeringModule.Assembly == mapEntry.DeclaringModule.Assembly);
                    namedEntries.Append(writer.GetTuple(
                        writer.GetStringConstant(mapEntry.SerializedSourceTypeName),
                        writer.GetStringConstant(mapEntry.SerializedTypeName)));
                    hasNamedEntries = true;
                }
            }

            uint typeMapState = hasNamedEntries
                ? ReadyToRunTypeMapEncoding.PrecomputedFixupsAndTypeNames
                : ReadyToRunTypeMapEncoding.PrecomputedFixups;
            Vertex typeMapStateVertex = writer.GetUnsignedConstant(typeMapState);
            Vertex typeMapData = hasNamedEntries
                ? writer.GetTuple(typeMapHashTable, namedEntries)
                : typeMapHashTable;
            Vertex tuple = writer.GetTuple(typeMapGroupVertex, typeMapStateVertex, typeMapData);
            return section.Place(tuple);
        }

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => [];
        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context)
        {
            yield return new DependencyListEntry(importProvider.GetImportToType(TypeMapGroup, TriggeringModule), $"Type map '{TypeMapGroup}' key type");

            if (map.ThrowingMethodStub is not null)
            {
                yield break;
            }

            foreach (var entry in map.TypeMap)
            {
                if (ReadyToRunTypeMapEncoding.IsTypeDescEncodable(context, TriggeringModule, entry.Key) &&
                    ReadyToRunTypeMapEncoding.IsTypeDescEncodable(context, TriggeringModule, entry.Value.Type))
                {
                    yield return new DependencyListEntry(importProvider.GetImportToType(entry.Key, TriggeringModule), $"Key type of Proxy type map entry");
                    yield return new DependencyListEntry(importProvider.GetImportToType(entry.Value.Type, TriggeringModule), $"Proxy type map entry target for key '{entry.Key}'");
                }
            }
        }
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => [];
        protected override string GetName(NodeFactory context) => $"ProxyTypeMap {TypeMapGroup} entries in assembly {TriggeringModule.GetDisplayName()}";
    }
}
