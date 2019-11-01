// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        public override int ClassCode => 82124527;

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
            sb.Append($"__CorHeader_{_module.Assembly.GetName().Name}");
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.RequireInitialPointerAlignment();
            builder.AddSymbol(this);
            ReadyToRunCodegenNodeFactory r2rFactory = ((ReadyToRunCodegenNodeFactory)factory);

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
            var metadataBlob = r2rFactory.CopiedMetadataBlob(_module);
            builder.EmitReloc(metadataBlob, RelocType.IMAGE_REL_BASED_ADDR32NB);
            builder.EmitInt(metadataBlob.Size);

            // Flags
            builder.EmitUInt((uint)(((CorFlags)reader.ReadUInt32() & ~CorFlags.ILOnly) | CorFlags.ILLibrary));

            // Entrypoint
            builder.EmitInt(reader.ReadInt32());

            // Resources Directory
            if (ReadDirectoryEntry(ref reader).Size > 0)
            {
                var managedResources = r2rFactory.CopiedManagedResources(_module);
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
                var strongNameSignature = r2rFactory.CopiedStrongNameSignature(_module);
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
            builder.EmitReloc(r2rFactory.Header, RelocType.IMAGE_REL_BASED_ADDR32NB);
            builder.EmitInt(r2rFactory.Header.GetData(factory, relocsOnly).Data.Length);

            // Did we fully read the header?
            Debug.Assert(reader.Offset - headerSize == _module.PEReader.PEHeaders.CorHeaderStartOffset);
            Debug.Assert(builder.CountBytes == headerSize);
            Debug.Assert(headerSize == Size);

            return builder.ToObjectData();
        }

        private void WriteEmptyDirectoryEntry(ref ObjectDataBuilder builder)
        {
            builder.EmitInt(0);
            builder.EmitInt(0);
        }
    }
}
