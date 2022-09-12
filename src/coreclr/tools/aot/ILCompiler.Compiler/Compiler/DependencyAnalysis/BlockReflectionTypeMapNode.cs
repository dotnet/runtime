// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.TypeSystem;
using Internal.NativeFormat;
using Internal.Text;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a hashtable of all type blocked from reflection.
    /// </summary>
    public sealed class BlockReflectionTypeMapNode : ObjectNode, ISymbolDefinitionNode
    {
        private ObjectAndOffsetSymbolNode _endSymbol;
        private ExternalReferencesTableNode _externalReferences;

        public BlockReflectionTypeMapNode(ExternalReferencesTableNode externalReferences)
        {
            _endSymbol = new ObjectAndOffsetSymbolNode(this, 0, "__block_reflection_type_map_End", true);
            _externalReferences = externalReferences;
        }

        public ISymbolDefinitionNode EndSymbol => _endSymbol;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__block_reflection_type_map");
        }
        public int Offset => 0;
        public override bool IsShareable => false;

        public override ObjectNodeSection Section => _externalReferences.Section;

        public override bool StaticDependenciesAreComputed => true;

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            // This node does not trigger generation of other nodes.
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });

            var writer = new NativeWriter();
            var reflectionBlockTypeMapHashTable = new VertexHashtable();

            Section hashTableSection = writer.NewSection();
            hashTableSection.Place(reflectionBlockTypeMapHashTable);

            foreach (var type in factory.MetadataManager.GetTypesWithEETypes())
            {
                if (!type.IsTypeDefinition)
                    continue;

                var mdType = type as MetadataType;
                if (mdType == null)
                    continue;

                if (!factory.MetadataManager.IsReflectionBlocked(mdType))
                    continue;

                if (!factory.CompilationModuleGroup.ContainsType(mdType))
                    continue;

                // Go with a necessary type symbol. It will be upgraded to a constructed one if a constructed was emitted.
                IEETypeNode typeSymbol = factory.NecessaryTypeSymbol(type);

                Vertex vertex = writer.GetUnsignedConstant(_externalReferences.GetIndex(typeSymbol));

                int hashCode = typeSymbol.Type.GetHashCode();
                reflectionBlockTypeMapHashTable.Append((uint)hashCode, hashTableSection.Place(vertex));
            }

            byte[] hashTableBytes = writer.Save();

            _endSymbol.SetSymbolOffset(hashTableBytes.Length);

            return new ObjectData(hashTableBytes, Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this, _endSymbol });
        }

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;
        public override int ClassCode => (int)ObjectNodeOrder.BlockReflectionTypeMapNode;
    }
}
