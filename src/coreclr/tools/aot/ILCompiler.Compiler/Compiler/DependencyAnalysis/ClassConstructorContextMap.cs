// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.NativeFormat;
using Internal.Text;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    class ClassConstructorContextMap : ObjectNode, ISymbolDefinitionNode
    {
        private ObjectAndOffsetSymbolNode _endSymbol;
        private ExternalReferencesTableNode _externalReferences;

        public ClassConstructorContextMap(ExternalReferencesTableNode externalReferences)
        {
            _endSymbol = new ObjectAndOffsetSymbolNode(this, 0, "__type_to_cctorContext_map_End", true);
            _externalReferences = externalReferences;
        }

        public ISymbolDefinitionNode EndSymbol => _endSymbol;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__type_to_cctorContext_map");
        }
        public int Offset => 0;

        public override ObjectNodeSection Section => _externalReferences.Section;
        public override bool IsShareable => false;

        public override bool StaticDependenciesAreComputed => true;

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            // This node does not trigger generation of other nodes.
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });

            var writer = new NativeWriter();
            var typeMapHashTable = new VertexHashtable();

            Section hashTableSection = writer.NewSection();
            hashTableSection.Place(typeMapHashTable);

            foreach (var node in factory.MetadataManager.GetCctorContextMapping())
            {
                MetadataType type = node.Type;

                Debug.Assert(factory.PreinitializationManager.HasLazyStaticConstructor(type));

                // If this type doesn't generate an MethodTable in the current compilation, don't report it in the table.
                // If nobody can get to the MethodTable, they can't ask to run the cctor. We don't need to force generate it.
                if (!factory.MetadataManager.TypeGeneratesEEType(type))
                    continue;

                // Hash table is hashed by the hashcode of the owning type.
                // Each entry has: the MethodTable of the type, followed by the non-GC static base.
                // The non-GC static base is prefixed by the class constructor context.
                Vertex vertex = writer.GetTuple(
                    writer.GetUnsignedConstant(_externalReferences.GetIndex(factory.NecessaryTypeSymbol(type))),
                    writer.GetUnsignedConstant(_externalReferences.GetIndex(node))
                    );

                int hashCode = type.GetHashCode();
                typeMapHashTable.Append((uint)hashCode, hashTableSection.Place(vertex));
            }

            byte[] hashTableBytes = writer.Save();

            _endSymbol.SetSymbolOffset(hashTableBytes.Length);

            return new ObjectData(hashTableBytes, Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this, _endSymbol });
        }

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;
        public override int ClassCode => (int)ObjectNodeOrder.ClassConstructorContextMap;
    }
}
