// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class CopiedFieldRvaNode : ObjectNode, ISymbolDefinitionNode, IObjectNodeWithAlignment
    {
        private int _rva;
        private EcmaModule _module;

        public CopiedFieldRvaNode(EcmaModule module, int rva)
        {
            _rva = rva;
            _module = module;
        }

        public override ObjectNodeSection GetSection(NodeFactory factory)
        {
            return ObjectNodeSection.ReadOnlyDataSection;
        }

        public override bool IsShareable => false;

        public override int ClassCode => 223495;

        public override bool StaticDependenciesAreComputed => true;

        public int Offset => 0;

        // Keep this in sync with the alignment applied in GetData: the RVA blob is at least
        // pointer-aligned, raised to the largest packing size among the fields that share this RVA.
        public int GetAlignment(NodeFactory factory)
        {
            int requiredAlignment = factory.Target.PointerSize;
            foreach (EcmaField field in GetFieldsAtRva())
            {
                requiredAlignment = Math.Max(requiredAlignment, (field.FieldType as MetadataType)?.GetClassLayout().PackingSize ?? 1);
            }

            return requiredAlignment;
        }

        // Enumerates the fields whose FieldRVA entry points at this node's RVA. The ECMA-335
        // FieldRVA table (II.22.18) has an RVA column (a fixed 4-byte constant) followed by a
        // Field column that indexes the Field table; that index is 2 bytes when the Field table
        // is "small" (< 2^16 rows) and 4 bytes otherwise. So the row size tells us the index width.
        private unsafe List<EcmaField> GetFieldsAtRva()
        {
            MetadataReader metadataReader = _module.MetadataReader;
            int rowCount = metadataReader.GetTableRowCount(TableIndex.FieldRva);
            bool smallFieldIndex = (metadataReader.GetTableRowSize(TableIndex.FieldRva) - sizeof(int)) == sizeof(ushort);

            BlobReader metadataBlob = new BlobReader(_module.PEReader.GetMetadata().Pointer, _module.PEReader.GetMetadata().Length);
            metadataBlob.Offset = metadataReader.GetTableMetadataOffset(TableIndex.FieldRva);

            List<EcmaField> fields = new List<EcmaField>();
            for (int i = 1; i <= rowCount; i++)
            {
                int currentFieldRva = metadataBlob.ReadInt32();
                int currentFieldRid = smallFieldIndex ? metadataBlob.ReadUInt16() : metadataBlob.ReadInt32();
                if (currentFieldRva == _rva)
                {
                    fields.Add(_module.GetField(MetadataTokens.FieldDefinitionHandle(currentFieldRid)));
                }
            }

            return fields;
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            if (relocsOnly)
            {
                return new ObjectData(
                    data: Array.Empty<byte>(),
                    relocs: Array.Empty<Relocation>(),
                    alignment: 1,
                    definedSymbols: new ISymbolDefinitionNode[] { this });
            }

            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            byte[] rvaData = GetRvaData(factory.Target.PointerSize);
            builder.RequireInitialAlignment(GetAlignment(factory));
            builder.AddSymbol(this);
            builder.EmitBytes(rvaData);
            return builder.ToObjectData();
        }

        private byte[] GetRvaData(int targetPointerSize)
        {
            int size = 0;
            foreach (EcmaField field in GetFieldsAtRva())
            {
                Debug.Assert(field.HasRva);

                // Handle overlapping fields sharing the RVA by reusing the blob and keeping the largest size.
                size = Math.Max(size, field.FieldType.GetElementSize().AsInt);
            }

            Debug.Assert(size > 0);

            PEMemoryBlock block = _module.PEReader.GetSectionData(_rva);
            if (block.Length < size)
                throw new BadImageFormatException();

            byte[] result = new byte[AlignmentHelper.AlignUp(size, targetPointerSize)];
            block.GetContent(0, size).CopyTo(result);
            return result;
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append($"_FieldRvaData_{_module.Assembly.GetName().Name}_{_rva}");
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            int result = _module.CompareTo(((CopiedFieldRvaNode)other)._module);
            if (result != 0)
                return result;

            return _rva - ((CopiedFieldRvaNode)other)._rva;
        }
    }
}
