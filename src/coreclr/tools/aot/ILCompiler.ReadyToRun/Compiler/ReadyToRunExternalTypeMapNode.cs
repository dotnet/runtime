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
    internal class ReadyToRunExternalTypeMapNode(ModuleDesc triggeringModule, TypeDesc group, TypeMapMetadata.IExternalTypeMap map, ImportReferenceProvider importProvider) : SortableDependencyNode, IExternalTypeMapNode
    {
        public TypeDesc TypeMapGroup => group;

        public override int ClassCode => 565222977;

        public override bool InterestingForDynamicDependencyAnalysis => false;

        public override bool HasDynamicDependencies => false;

        public override bool HasConditionalStaticDependencies => false;

        public override bool StaticDependenciesAreComputed => true;

        private ModuleDesc TriggeringModule { get; } = triggeringModule;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            ReadyToRunExternalTypeMapNode otherNode = (ReadyToRunExternalTypeMapNode)other;
            int result = comparer.Compare(TypeMapGroup, otherNode.TypeMapGroup);
            if (result != 0)
                return result;

            return comparer.Compare(TriggeringModule, otherNode.TriggeringModule);
        }

        public Vertex CreateTypeMap(NodeFactory factory, NativeWriter writer, Section section, INativeFormatTypeReferenceProvider externalReferences)
        {
            if (map.ThrowingMethodStub is not null)
            {
                // We don't write out the throwing method stub for R2R
                // as emitting loose methods is not supported/very expensive.
                // Instead, we defer to the runtime to generate the type map
                // and throw on error cases.
                return section.Place(writer.GetUnsignedConstant(0)); // Invalid type map state
            }

            VertexHashtable typeMapHashTable = new();

            Section typeMapEntriesSection = writer.NewSection();

            foreach ((string key, (TypeDesc type, _)) in map.TypeMap)
            {
                Vertex keyVertex = writer.GetStringConstant(key);
                Vertex valueVertex = externalReferences.EncodeReferenceToType(writer, type);
                Vertex entry = writer.GetTuple(keyVertex, valueVertex);
                typeMapHashTable.Append((uint)TypeHashingAlgorithms.ComputeNameHashCode(key), typeMapEntriesSection.Place(entry));
            }

            Vertex typeMapStateVertex = writer.GetUnsignedConstant(1); // Valid type map state
            Vertex typeMapGroupVertex = externalReferences.EncodeReferenceToType(writer, TypeMapGroup);
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
                yield return new DependencyListEntry(importProvider.GetImportToType(entry.Value.type), $"External type map entry target for key '{entry.Key}'");
            }
        }
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => [];
        protected override string GetName(NodeFactory context) => $"ExternalTypeMap {TypeMapGroup} entries in assembly {TriggeringModule.GetDisplayName()}";
    }
}
