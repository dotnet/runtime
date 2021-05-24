// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

using Internal.Text;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class CopiedCorHeaderNode : ObjectNode, ISymbolDefinitionNode
    {
        private static readonly int s_corHeaderSize = 0x48;

        private EcmaModule _module;
        
        public CopiedCorHeaderNode(EcmaModule sourceModule)
        {
            _module = sourceModule;
        }

        public override ObjectNodeSection Section => ObjectNodeSection.TextSection;

        public override bool IsShareable => false;

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;

        public override int ClassCode => (int)ObjectNodeOrder.CorHeaderNode;

        public override bool StaticDependenciesAreComputed => true;

        public int Offset => 0;

        public int Size => s_corHeaderSize;

        /// <summary>
        /// Deserialize a directory entry from a blob reader.
        /// </summary>
        /// <param name="reader">Reader to deserialize directory entry from</param>
        private static DirectoryEntry ReadDirectoryEntry(ref BlobReader reader)
        {
            int rva = reader.ReadInt32();
            int size = reader.ReadInt32();
            return new DirectoryEntry(rva, size);
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            if (_module != null)
                sb.Append($"__CorHeader_{_module.Assembly.GetName().Name}");
            else
                sb.Append("__CompositeCorHeader_");
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.RequireInitialPointerAlignment();
            builder.AddSymbol(this);

            if (_module != null)
            {
                BlobReader reader = _module.PEReader.GetEntireImage().GetReader();
                reader.Offset = _module.PEReader.PEHeaders.CorHeaderStartOffset;

                // Header Size
                int headerSize = reader.ReadInt32();
                builder.EmitInt(headerSize);

                // Runtime major, minor version
                builder.EmitUShort(reader.ReadUInt16());
                builder.EmitUShort(reader.ReadUInt16());

                // Metadata Directory
                ReadDirectoryEntry(ref reader);
                var metadataBlob = factory.CopiedMetadataBlob(_module);
                builder.EmitReloc(metadataBlob, RelocType.IMAGE_REL_BASED_ADDR32NB);
                builder.EmitInt(metadataBlob.Size);

                // Flags
                builder.EmitUInt((uint)(((CorFlags)reader.ReadUInt32() & ~CorFlags.ILOnly) | CorFlags.ILLibrary));

                // Entrypoint
                builder.EmitInt(reader.ReadInt32());

                // Resources Directory
                if (ReadDirectoryEntry(ref reader).Size > 0)
                {
                    var managedResources = factory.CopiedManagedResources(_module);
                    builder.EmitReloc(managedResources, RelocType.IMAGE_REL_BASED_ADDR32NB);
                    builder.EmitInt(managedResources.Size);
                }
                else
                {
                    WriteEmptyDirectoryEntry(ref builder);
                }

                // Strong Name Signature Directory
                if (ReadDirectoryEntry(ref reader).Size > 0)
                {
                    var strongNameSignature = factory.CopiedStrongNameSignature(_module);
                    builder.EmitReloc(strongNameSignature, RelocType.IMAGE_REL_BASED_ADDR32NB);
                    builder.EmitInt(strongNameSignature.Size);
                }
                else
                {
                    WriteEmptyDirectoryEntry(ref builder);
                }

                // Code Manager Table Directory
                ReadDirectoryEntry(ref reader);
                WriteEmptyDirectoryEntry(ref builder);

                // VTable Fixups Directory
                ReadDirectoryEntry(ref reader);
                WriteEmptyDirectoryEntry(ref builder);

                // Export Address Table Jumps Directory
                ReadDirectoryEntry(ref reader);
                WriteEmptyDirectoryEntry(ref builder);

                // Managed Native (ReadyToRun) Header Directory
                ReadDirectoryEntry(ref reader);
                builder.EmitReloc(factory.Header, RelocType.IMAGE_REL_BASED_ADDR32NB);
                builder.EmitReloc(factory.Header, RelocType.IMAGE_REL_SYMBOL_SIZE);

                // Did we fully read the header?
                Debug.Assert(reader.Offset - headerSize == _module.PEReader.PEHeaders.CorHeaderStartOffset);
                Debug.Assert(builder.CountBytes == headerSize);
                Debug.Assert(headerSize == Size);
            }
            else
            {
                // Generating CORHeader for composite image
                // Header Size
                builder.EmitInt(Size);

                // Runtime major, minor version
                builder.EmitUShort(0);
                builder.EmitUShort(0);

                // Metadata Directory
                builder.EmitReloc(factory.ManifestMetadataTable, RelocType.IMAGE_REL_BASED_ADDR32NB);
                builder.EmitReloc(factory.ManifestMetadataTable, RelocType.IMAGE_REL_SYMBOL_SIZE);

                // Flags
                if (factory.CompositeImageSettings.PublicKey != null)
                {
                    const uint COMIMAGE_FLAGS_STRONGNAMESIGNED = 8;
                    builder.EmitUInt(COMIMAGE_FLAGS_STRONGNAMESIGNED);
                }
                else
                {
                    builder.EmitUInt(0);
                }

                // Entrypoint
                builder.EmitInt(0);

                // Resources Directory
                WriteEmptyDirectoryEntry(ref builder);

                // Strong Name Signature Directory
                WriteEmptyDirectoryEntry(ref builder);

                // Code Manager Table Directory
                WriteEmptyDirectoryEntry(ref builder);

                // VTable Fixups Directory
                WriteEmptyDirectoryEntry(ref builder);

                // Export Address Table Jumps Directory
                WriteEmptyDirectoryEntry(ref builder);

                // Managed Native (ReadyToRun) Header Directory
                builder.EmitReloc(factory.Header, RelocType.IMAGE_REL_BASED_ADDR32NB);
                builder.EmitReloc(factory.Header, RelocType.IMAGE_REL_SYMBOL_SIZE);
            }

            return builder.ToObjectData();
        }

        private void WriteEmptyDirectoryEntry(ref ObjectDataBuilder builder)
        {
            builder.EmitInt(0);
            builder.EmitInt(0);
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            if (_module == null)
            {
                if (((CopiedCorHeaderNode)other)._module == null)
                    return 0;
                return -1;
            }
            else if (((CopiedCorHeaderNode)other)._module == null)
            {
                return 1;
            }

            return _module.CompareTo(((CopiedCorHeaderNode)other)._module);
        }
    }
}
