// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using Internal.NativeFormat;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

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
            if (map.ThrowingMethodStub is not null)
            {
                // We don't write out the throwing method stub for R2R
                // as emitting loose methods is not supported/very expensive.
                // Also, matching CoreCLR's exact set of exceptions is difficult
                // in the managed type system.
                // Instead, we defer to the runtime to generate the type map
                // and throw on error cases.
                return section.Place(writer.GetUnsignedConstant(0)); // Invalid type map state
            }

            VertexHashtable typeMapHashTable = new();

            Section typeMapEntriesSection = writer.NewSection();

            foreach ((TypeDesc type, TypeDesc targetType) in map.TypeMap)
            {
                Vertex keyVertex = ProxyReferences.EncodeReferenceToType(writer, type);
                Vertex valueVertex = ProxyReferences.EncodeReferenceToType(writer, targetType);
                Vertex entry = writer.GetTuple(keyVertex, valueVertex);
                typeMapHashTable.Append((uint)type.GetHashCode(), typeMapEntriesSection.Place(entry));
            }

            Vertex typeMapStateVertex = writer.GetUnsignedConstant(1); // Valid type map state
            Vertex typeMapGroupVertex = ProxyReferences.EncodeReferenceToType(writer, TypeMapGroup);
            Vertex tuple = writer.GetTuple(typeMapGroupVertex, typeMapStateVertex, typeMapHashTable);
            return section.Place(tuple);
        }

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => [];
        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context)
        {
            if (map.ThrowingMethodStub is not null)
            {
                yield break;
            }

            foreach (var entry in map.TypeMap)
            {
                yield return new DependencyListEntry(importProvider.GetImportToType(entry.Key), $"Key type of Proxy type map entry");
                yield return new DependencyListEntry(importProvider.GetImportToType(entry.Value), $"Proxy type map entry target for key '{entry.Key}'");
            }
        }
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => [];
        protected override string GetName(NodeFactory context) => $"ProxyTypeMap {TypeMapGroup} entries in assembly {TriggeringModule.GetDisplayName()}";
    }
}
