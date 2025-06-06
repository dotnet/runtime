// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using Internal.NativeFormat;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    internal sealed class ExternalTypeMapObjectNode(ExternalReferencesTableNode externalReferences) : ObjectNode, ISymbolDefinitionNode, INodeWithSize
    {
        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__external_type_map__"u8);
        }

        public int Size { get; private set; }
        public int Offset => 0;
        public override bool IsShareable => false;
        public override ObjectNodeSection GetSection(NodeFactory factory) => externalReferences.GetSection(factory);
        protected internal override int Phase => (int)ObjectNodePhase.Ordered;

        public override int ClassCode => (int)ObjectNodeOrder.ExternalTypeMapObjectNode;

        public override bool StaticDependenciesAreComputed => true;

        protected override string GetName(NodeFactory context) => "External Type Map Hash Table";
        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            // This node does not trigger generation of other nodes.
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, [this]);

            var writer = new NativeWriter();
            var typeMapGroupHashTable = new VertexHashtable();

            Dictionary<TypeDesc, VertexHashtable> typeMapHashTables = new();

            Section hashTableSection = writer.NewSection();
            hashTableSection.Place(typeMapGroupHashTable);

            foreach (ExternalTypeMapNode externalTypeMap in factory.TypeMapManager.GetExternalTypeMaps())
            {
                if (!typeMapHashTables.TryGetValue(externalTypeMap.TypeMapGroup, out VertexHashtable typeMapHashTable))
                {
                    TypeDesc typeMapGroup = externalTypeMap.TypeMapGroup;
                    typeMapHashTable = typeMapHashTables[typeMapGroup] = new VertexHashtable();
                }

                foreach ((string key, IEETypeNode valueNode) in externalTypeMap.GetMarkedEntries(factory))
                {
                    Vertex keyVertex = writer.GetStringConstant(key);
                    Vertex valueVertex = writer.GetUnsignedConstant(externalReferences.GetIndex(valueNode));
                    Vertex entry = writer.GetTuple(keyVertex, valueVertex);
                    typeMapHashTable.Append((uint)TypeHashingAlgorithms.ComputeNameHashCode(key), hashTableSection.Place(entry));
                }
            }

            foreach ((TypeDesc typeMapGroup, VertexHashtable typeMapHashTable) in typeMapHashTables)
            {
                Vertex typeMapStateVertex = writer.GetUnsignedConstant(1); // Valid type map state
                Vertex typeMapGroupVertex = writer.GetUnsignedConstant(externalReferences.GetIndex(factory.NecessaryTypeSymbol(typeMapGroup)));
                Vertex tuple = writer.GetTuple(typeMapGroupVertex, typeMapStateVertex, typeMapHashTable);
                typeMapGroupHashTable.Append((uint)typeMapGroup.GetHashCode(), hashTableSection.Place(tuple));
            }

            foreach (InvalidExternalTypeMapNode invalidNode in factory.TypeMapManager.GetInvalidExternalTypeMaps())
            {
                TypeDesc typeMapGroup = invalidNode.TypeMapGroup;
                Vertex typeMapStateVertex = writer.GetUnsignedConstant(0); // Invalid type map state
                Vertex typeMapGroupVertex = writer.GetUnsignedConstant(externalReferences.GetIndex(factory.NecessaryTypeSymbol(typeMapGroup)));
                Vertex throwingMethodStubVertex = writer.GetUnsignedConstant(externalReferences.GetIndex(factory.MethodEntrypoint(invalidNode.ThrowingMethodStub)));
                Vertex tuple = writer.GetTuple(typeMapGroupVertex, typeMapStateVertex, throwingMethodStubVertex);
                typeMapGroupHashTable.Append((uint)typeMapGroup.GetHashCode(), hashTableSection.Place(tuple));
            }

            byte[] hashTableBytes = writer.Save();

            Size = hashTableBytes.Length;

            return new ObjectData(hashTableBytes, Array.Empty<Relocation>(), 1, [this]);
        }
    }
}
