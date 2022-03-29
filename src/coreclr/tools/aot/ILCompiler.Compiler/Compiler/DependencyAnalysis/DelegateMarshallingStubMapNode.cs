// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.NativeFormat;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a hash table of delegate marshalling stub types generated into the image.
    /// </summary>
    internal sealed class DelegateMarshallingStubMapNode : ObjectNode, ISymbolDefinitionNode
    {
        private readonly ObjectAndOffsetSymbolNode _endSymbol;
        private readonly ExternalReferencesTableNode _externalReferences;
        private readonly InteropStateManager _interopStateManager;

        public DelegateMarshallingStubMapNode(ExternalReferencesTableNode externalReferences, InteropStateManager interopStateManager)
        {
            _endSymbol = new ObjectAndOffsetSymbolNode(this, 0, "__delegate_marshalling_stub_map_End", true);
            _externalReferences = externalReferences;
            _interopStateManager = interopStateManager;
        }

        public ISymbolDefinitionNode EndSymbol => _endSymbol;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__delegate_marshalling_stub_map");
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
            var typeMapHashTable = new VertexHashtable();

            Section hashTableSection = writer.NewSection();
            hashTableSection.Place(typeMapHashTable);

            foreach (var delegateType in factory.MetadataManager.GetTypesWithDelegateMarshalling())
            {
                Vertex thunks= writer.GetTuple(
                    writer.GetUnsignedConstant(_externalReferences.GetIndex(factory.MethodEntrypoint(_interopStateManager.GetOpenStaticDelegateMarshallingThunk(delegateType)))),
                    writer.GetUnsignedConstant(_externalReferences.GetIndex(factory.MethodEntrypoint(_interopStateManager.GetClosedDelegateMarshallingThunk(delegateType)))),
                    writer.GetUnsignedConstant(_externalReferences.GetIndex(factory.MethodEntrypoint(_interopStateManager.GetForwardDelegateCreationThunk(delegateType))))
                    );

                Vertex vertex = writer.GetTuple(
                    writer.GetUnsignedConstant(_externalReferences.GetIndex(factory.NecessaryTypeSymbol(delegateType))),
                    thunks
                );

                int hashCode = delegateType.GetHashCode();
                typeMapHashTable.Append((uint)hashCode, hashTableSection.Place(vertex));
            }

            byte[] hashTableBytes = writer.Save();

            _endSymbol.SetSymbolOffset(hashTableBytes.Length);

            return new ObjectData(hashTableBytes, Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this, _endSymbol });
        }

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;
        public override int ClassCode => (int)ObjectNodeOrder.DelegateMarshallingStubMapNode;
    }
}
