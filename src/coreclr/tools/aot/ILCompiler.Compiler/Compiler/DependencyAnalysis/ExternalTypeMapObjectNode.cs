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

        public override bool HasDynamicDependencies => true;

        public override int ClassCode => 2090746844;

        public override bool StaticDependenciesAreComputed => true;

        protected override string GetName(NodeFactory context) => $"External Type Map";
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

            foreach (ExternalTypeMapEntryNode entryNode in factory.MetadataManager.GetExternalTypeMapEntries())
            {
                if (!typeMapHashTables.TryGetValue(entryNode.TypeMapGroup, out VertexHashtable typeMapHashTable))
                {
                    TypeDesc typeMapGroup = entryNode.TypeMapGroup;
                    typeMapHashTable = typeMapHashTables[typeMapGroup] = new VertexHashtable();
                    Vertex typeMapStateVertex = writer.GetUnsignedConstant(1); // Valid type map state
                    Vertex typeMapGroupVertex = writer.GetUnsignedConstant(externalReferences.GetIndex(factory.NecessaryTypeSymbol(typeMapGroup)));
                    typeMapGroupHashTable.Append((uint)typeMapGroup.GetHashCode(), writer.GetTuple(typeMapGroupVertex, typeMapStateVertex, hashTableSection.Place(typeMapHashTable)));
                }

                Vertex nameVertex = writer.GetStringConstant(entryNode.Key);
                Vertex targetTypeVertex = writer.GetUnsignedConstant(externalReferences.GetIndex(factory.MaximallyConstructableType(entryNode.TargetType)));
                Vertex entry = writer.GetTuple(nameVertex, targetTypeVertex);

                typeMapHashTable.Append((uint)TypeHashingAlgorithms.ComputeNameHashCode(entryNode.Key), hashTableSection.Place(entry));
            }

            foreach (InvalidExternalTypeMapNode invalidNode in factory.MetadataManager.GetInvalidExternalTypeMaps())
            {
                if (!typeMapHashTables.TryGetValue(invalidNode.TypeMapGroup, out _))
                {
                    TypeDesc typeMapGroup = invalidNode.TypeMapGroup;
                    typeMapHashTables[typeMapGroup] = new VertexHashtable();
                    Vertex typeMapStateVertex = writer.GetUnsignedConstant(0); // Invalid type map state
                    Vertex typeMapGroupVertex = writer.GetUnsignedConstant(externalReferences.GetIndex(factory.NecessaryTypeSymbol(typeMapGroup)));
                    Vertex throwingMethodStubVertex = writer.GetUnsignedConstant(externalReferences.GetIndex(factory.MethodEntrypoint(invalidNode.ThrowingMethodStub)));
                    typeMapGroupHashTable.Append((uint)typeMapGroup.GetHashCode(), writer.GetTuple(typeMapGroupVertex, typeMapStateVertex, throwingMethodStubVertex));
                }
            }

            byte[] hashTableBytes = writer.Save();

            Size = hashTableBytes.Length;

            return new ObjectData(hashTableBytes, Array.Empty<Relocation>(), 1, [this]);
        }
    }
}
