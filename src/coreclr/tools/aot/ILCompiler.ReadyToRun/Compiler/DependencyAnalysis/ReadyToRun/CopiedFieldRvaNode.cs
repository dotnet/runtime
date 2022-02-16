// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
    public class CopiedFieldRvaNode : ObjectNode, ISymbolDefinitionNode
    {
        private int _rva;
        private EcmaModule _module;

        public CopiedFieldRvaNode(EcmaModule module, int rva)
        {
            _rva = rva;
            _module = module;
        }

        public override ObjectNodeSection Section => ObjectNodeSection.TextSection;

        public override bool IsShareable => false;

        public override int ClassCode => 223495;

        public override bool StaticDependenciesAreComputed => true;

        public int Offset => 0;

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
            byte[] rvaData = GetRvaData(factory.Target.PointerSize, out int requiredAlignment);
            builder.RequireInitialAlignment(requiredAlignment);
            builder.AddSymbol(this);
            builder.EmitBytes(rvaData);
            return builder.ToObjectData();
        }

        private unsafe byte[] GetRvaData(int targetPointerSize, out int requiredAlignment)
        {
            int size = 0;
            requiredAlignment = targetPointerSize;

            MetadataReader metadataReader = _module.MetadataReader;
            BlobReader metadataBlob = new BlobReader(_module.PEReader.GetMetadata().Pointer, _module.PEReader.GetMetadata().Length);
            metadataBlob.Offset = metadataReader.GetTableMetadataOffset(TableIndex.FieldRva);
            bool compressedFieldRef = 6 == metadataReader.GetTableRowSize(TableIndex.FieldRva);
            
            for (int i = 1; i <= metadataReader.GetTableRowCount(TableIndex.FieldRva); i++)
            {
                int currentFieldRva = metadataBlob.ReadInt32();
                int currentFieldRid;
                if (compressedFieldRef)
                {
                    currentFieldRid = metadataBlob.ReadUInt16();
                }
                else
                {
                    currentFieldRid = metadataBlob.ReadInt32();
                }
                if (currentFieldRva != _rva)
                    continue;

                EcmaField field = (EcmaField)_module.GetField(MetadataTokens.FieldDefinitionHandle(currentFieldRid));
                Debug.Assert(field.HasRva);

                int currentSize = field.FieldType.GetElementSize().AsInt;
                requiredAlignment = Math.Max(requiredAlignment, (field.FieldType as MetadataType)?.GetClassLayout().PackingSize ?? 1);
                if (currentSize > size)
                {
                    // We need to handle overlapping fields by reusing blobs based on the rva, and just update
                    // the size and contents
                    size = currentSize;
                }
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
