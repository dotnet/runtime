// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// Copies the metadata blob from input MSIL assembly to output ready-to-run image, fixing up Rvas to 
    /// method IL bodies and FieldRvas.
    /// </summary>
    public class CopiedMetadataBlobNode : ObjectNode, ISymbolDefinitionNode
    {
        EcmaModule _sourceModule;
        
        public CopiedMetadataBlobNode(EcmaModule sourceModule)
        {
            _sourceModule = sourceModule;
        }

        public override ObjectNodeSection Section => ObjectNodeSection.TextSection;

        public override bool IsShareable => false;

        public override int ClassCode => 635464644;

        public override bool StaticDependenciesAreComputed => true;
        public int Offset => 0;

        public int Size => _sourceModule.PEReader.GetMetadata().Length;

        private void WriteMethodTableRvas(NodeFactory factory, ref ObjectDataBuilder builder, ref BlobReader reader)
        {
            MetadataReader metadataReader = _sourceModule.MetadataReader;
            var tableIndex = TableIndex.MethodDef;
            int rowCount = metadataReader.GetTableRowCount(tableIndex);
            int rowSize = metadataReader.GetTableRowSize(tableIndex);

            for (int i = 1; i <= rowCount; i++)
            {
                Debug.Assert(builder.CountBytes == reader.Offset);

                int inputRva = reader.ReadInt32();

                if (inputRva == 0)
                {
                    // Don't fix up 0 Rvas (abstract methods in the methodDef table)
                    builder.EmitInt(0);
                }
                else
                {
                    var methodDefHandle = MetadataTokens.EntityHandle(TableIndex.MethodDef, i);
                    EcmaMethod method = _sourceModule.GetMethod(methodDefHandle) as EcmaMethod;
                    builder.EmitReloc(factory.CopiedMethodIL(method), RelocType.IMAGE_REL_BASED_ADDR32NB);
                }

                // Skip the rest of the row
                int remainingBytes = rowSize - sizeof(int);
                builder.EmitBytes(reader.ReadBytes(remainingBytes));
            }
        }

        private void WriteFieldRvas(NodeFactory factory, ref ObjectDataBuilder builder, ref BlobReader reader)
        {
            MetadataReader metadataReader = _sourceModule.MetadataReader;
            var tableIndex = TableIndex.FieldRva;
            int rowCount = metadataReader.GetTableRowCount(tableIndex);
            bool compressedFieldRef = 6 == metadataReader.GetTableRowSize(TableIndex.FieldRva);
            
            for (int i = 1; i <= rowCount; i++)
            {
                Debug.Assert(builder.CountBytes == reader.Offset);

                // Rva
                reader.ReadInt32();

                int fieldToken;
                if (compressedFieldRef)
                {
                    fieldToken = reader.ReadUInt16();
                }
                else
                {
                    fieldToken = reader.ReadInt32();
                }
                EntityHandle fieldHandle = MetadataTokens.EntityHandle(TableIndex.Field, fieldToken);
                EcmaField fieldDesc = (EcmaField)_sourceModule.GetField(fieldHandle);
                Debug.Assert(fieldDesc.HasRva);

                builder.EmitReloc(factory.CopiedFieldRva(fieldDesc), RelocType.IMAGE_REL_BASED_ADDR32NB);
                if (compressedFieldRef)
                {
                    builder.EmitUShort((ushort)fieldToken);
                }
                else
                {
                    builder.EmitUInt((uint)fieldToken);
                }
            }
        }

        public unsafe override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.RequireInitialPointerAlignment();
            builder.AddSymbol(this);

            BlobReader metadataBlob = new BlobReader(_sourceModule.PEReader.GetMetadata().Pointer, _sourceModule.PEReader.GetMetadata().Length);
            var metadataReader = _sourceModule.MetadataReader;

            //
            // methodDef table
            //

            int methodDefTableOffset = metadataReader.GetTableMetadataOffset(TableIndex.MethodDef);
            builder.EmitBytes(metadataBlob.ReadBytes(methodDefTableOffset));
            
            WriteMethodTableRvas(factory, ref builder, ref metadataBlob);

            //
            // fieldRva table
            //

            int fieldRvaTableOffset = metadataReader.GetTableMetadataOffset(TableIndex.FieldRva);
            builder.EmitBytes(metadataBlob.ReadBytes(fieldRvaTableOffset - metadataBlob.Offset));

            WriteFieldRvas(factory, ref builder, ref metadataBlob);

            // Copy the rest of the metadata blob
            builder.EmitBytes(metadataBlob.ReadBytes(metadataReader.MetadataLength - metadataBlob.Offset));

            Debug.Assert(builder.CountBytes == metadataBlob.Length);
            Debug.Assert(builder.CountBytes == metadataBlob.Offset);
            Debug.Assert(builder.CountBytes == Size);

            return builder.ToObjectData();
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append("__MetadataBlob");
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return _sourceModule.CompareTo(((CopiedMetadataBlobNode)other)._sourceModule);
        }
    }
}
