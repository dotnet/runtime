// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.NativeFormat;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a hash table of array types generated into the image.
    /// </summary>
    internal sealed class ArrayMapNode : ObjectNode, ISymbolDefinitionNode
    {
        private ObjectAndOffsetSymbolNode _endSymbol;
        private ExternalReferencesTableNode _externalReferences;

        public ArrayMapNode(ExternalReferencesTableNode externalReferences)
        {
            _endSymbol = new ObjectAndOffsetSymbolNode(this, 0, "__array_type_map_End", true);
            _externalReferences = externalReferences;
        }

        public ISymbolDefinitionNode EndSymbol => _endSymbol;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__array_type_map");
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

            foreach (var type in factory.MetadataManager.GetTypesWithConstructedEETypes())
            {
                if (!type.IsSzArray)
                    continue;

                var arrayType = (ArrayType)type;

                // This optimization is not compatible with canInlineTypeCheck on JIT/EE interface returning
                // CORINFO_INLINE_TYPECHECK_PASS unconditionally.
                //
                // If we're generating a template for this type, we can skip generating the hashtable entry
                // since the type loader can just create this type at runtime if something needs it. It's
                // okay to have multiple EETypes for the same array type.
                // var canonArrayType = arrayType.ConvertToCanonForm(CanonicalFormKind.Specific);
                // if (arrayType != canonArrayType && factory.NativeLayout.TemplateTypeLayout(canonArrayType).Marked)
                //     continue;

                // Look at the constructed type symbol. If a constructed type wasn't emitted, then the array map entry isn't valid for use
                IEETypeNode arrayTypeSymbol = factory.ConstructedTypeSymbol(arrayType);

                Vertex vertex = writer.GetUnsignedConstant(_externalReferences.GetIndex(arrayTypeSymbol));

                int hashCode = arrayType.GetHashCode();
                typeMapHashTable.Append((uint)hashCode, hashTableSection.Place(vertex));
            }

            byte[] hashTableBytes = writer.Save();

            _endSymbol.SetSymbolOffset(hashTableBytes.Length);

            return new ObjectData(hashTableBytes, Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this, _endSymbol });
        }

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;
        public override int ClassCode => (int)ObjectNodeOrder.ArrayMapNode;
    }
}
