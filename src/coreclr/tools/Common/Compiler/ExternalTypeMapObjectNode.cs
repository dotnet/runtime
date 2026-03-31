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
    public sealed class ExternalTypeMapObjectNode(TypeMapManager manager, INativeFormatTypeReferenceProvider externalReferences) : ObjectNode, ISymbolDefinitionNode
    {
        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            // This node does not trigger generation of other nodes.
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, [this]);

            var writer = new NativeWriter();
            var typeMapGroupHashTable = new VertexHashtable();

            Section hashTableSection = writer.NewSection();
            hashTableSection.Place(typeMapGroupHashTable);

            foreach (IExternalTypeMapNode externalTypeMap in manager.GetExternalTypeMaps())
            {
                typeMapGroupHashTable.Append((uint)externalTypeMap.TypeMapGroup.GetHashCode(), externalTypeMap.CreateTypeMap(factory, writer, hashTableSection, externalReferences));
            }

            byte[] hashTableBytes = writer.Save();

            return new ObjectData(hashTableBytes, Array.Empty<Relocation>(), 1, [this]);
        }
        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__external_type_map__"u8);
        }

        public int Offset => 0;
        public override bool IsShareable => false;
        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.ReadOnlyDataSection;
        protected internal override int Phase => (int)ObjectNodePhase.Ordered;

        public override int ClassCode => (int)ObjectNodeOrder.ExternalTypeMapObjectNode;

        public override bool StaticDependenciesAreComputed => true;

        protected override string GetName(NodeFactory context) => "External Type Map Hash Table";
    }
}
