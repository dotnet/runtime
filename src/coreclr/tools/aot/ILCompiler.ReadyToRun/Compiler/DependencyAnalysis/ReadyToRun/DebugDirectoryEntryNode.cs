// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Text;
using System.Diagnostics;
using System.Reflection.PortableExecutable;

using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using System.IO;
using System.Collections.Immutable;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public abstract class DebugDirectoryEntryNode : ObjectNode, ISymbolDefinitionNode
    {
        protected readonly EcmaModule _module;

        public DebugDirectoryEntryNode(EcmaModule module)
        {
            _module = module;
        }

        public override ObjectNodeSection Section => ObjectNodeSection.TextSection;

        public override bool IsShareable => false;

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;

        public override bool StaticDependenciesAreComputed => true;

        public int Offset => 0;

        public abstract void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb);

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return _module.CompareTo(((DebugDirectoryEntryNode)other)._module);
        }
    }

    public static class DeterministicDebugDirectoryEntry
    {
        internal static void EmitHeader(ref ObjectDataBuilder builder)
        {
            builder.EmitUInt(0 /* Characteristics */);
            builder.EmitUInt(0);
            builder.EmitUShort(0);
            builder.EmitUShort(0);
            builder.EmitInt((int)DebugDirectoryEntryType.Reproducible);
            builder.EmitInt(0);
            builder.EmitUInt(0);
            builder.EmitUInt(0);
        }
    }

    public class PerfMapDebugDirectoryEntryNode : DebugDirectoryEntryNode
    {
        const int PerfMapEntrySize =
            sizeof(uint) +   // Magic
            SignatureSize + // Signature
            sizeof(uint) +   // Age
            260;            // FileName

        public const uint PerfMapMagic = 0x4D523252;// R2RM

        public const int PerfMapEntryType = 21; // DebugDirectoryEntryType for this entry.

        private const int SignatureSize = 16;

        public override int ClassCode => 813123850;

        public unsafe int Size => PerfMapEntrySize;

        public PerfMapDebugDirectoryEntryNode(string entryName)
            : base(null)
        {
            _entryName = entryName;
        }

        private string _entryName;

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append($"__PerfMapDebugDirectoryEntryNode_{_entryName.Replace('.','_')}");
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.RequireInitialPointerAlignment();
            builder.AddSymbol(this);

            // Emit empty entry. This will be filled with data after the output image is emitted
            builder.EmitZeros(PerfMapEntrySize);

            return builder.ToObjectData();
        }

        public byte[] GeneratePerfMapEntryData(byte[] signature, int version)
        {
            Debug.Assert(SignatureSize == signature.Length);
            MemoryStream perfmapEntry = new MemoryStream(PerfMapEntrySize);

            using (BinaryWriter writer = new BinaryWriter(perfmapEntry))
            {
                writer.Write(PerfMapMagic);
                writer.Write(signature);
                writer.Write(version);

                byte[] perfmapNameBytes = Encoding.UTF8.GetBytes(_entryName);
                writer.Write(perfmapNameBytes);
                writer.Write(0); // Null terminator

                Debug.Assert(perfmapEntry.Length <= PerfMapEntrySize);
                return perfmapEntry.ToArray();
            }
        }

        internal void EmitHeader(ref ObjectDataBuilder builder)
        {
            builder.EmitUInt(0);        /* Characteristics */
            builder.EmitUInt(0);        /* Stamp */
            builder.EmitUShort(1);      /* Major */
            builder.EmitUShort(0);      /* Minor */
            builder.EmitInt((int)PerfMapEntryType);
            builder.EmitInt(Size);
            builder.EmitReloc(this, RelocType.IMAGE_REL_BASED_ADDR32NB);
            builder.EmitReloc(this, RelocType.IMAGE_REL_FILE_ABSOLUTE);
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return _entryName.CompareTo(((PerfMapDebugDirectoryEntryNode)other)._entryName);
        }
    }

    public class NativeDebugDirectoryEntryNode : DebugDirectoryEntryNode
    {
        const int RSDSSize =
            sizeof(int) +   // Magic
            16 +            // Signature (guid)
            sizeof(int) +   // Age
            260;            // FileName

        public override int ClassCode => 119958401;

        public unsafe int Size => RSDSSize;

        public const uint RsdsMagic = 0x53445352;// R2RM

        public NativeDebugDirectoryEntryNode(string pdbName)
            : base(null)
        {
            _pdbName = pdbName;
        }

        private string _pdbName;

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append($"__NativeDebugDirectory_{_pdbName.Replace('.','_')}");
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.RequireInitialPointerAlignment();
            builder.AddSymbol(this);

            // Emit empty entry. This will be filled with data after the output image is emitted
            builder.EmitZeros(RSDSSize);

            return builder.ToObjectData();
        }

        public byte[] GenerateRSDSEntryData(byte[] hash)
        {
            MemoryStream rsdsEntry = new MemoryStream(RSDSSize);

            using (BinaryWriter writer = new BinaryWriter(rsdsEntry))
            {
                writer.Write(RsdsMagic);

                Debug.Assert(hash.Length >= 16);
                writer.Write(hash, 0, 16);

                // Age
                writer.Write(1);

                string pdbFileName = _pdbName;
                byte[] pdbFileNameBytes = Encoding.UTF8.GetBytes(pdbFileName);
                writer.Write(pdbFileNameBytes);
                writer.Write(0); // Null terminator

                Debug.Assert(rsdsEntry.Length <= RSDSSize);
                return rsdsEntry.ToArray();
            }
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return _pdbName.CompareTo(((NativeDebugDirectoryEntryNode)other)._pdbName);
        }

        internal void EmitHeader(ref ObjectDataBuilder builder, uint stamp, ushort majorVersion)
        {
            builder.EmitUInt(0);        /* Characteristics */
            builder.EmitUInt(stamp);
            builder.EmitUShort(majorVersion);
            // Make sure the "is portable pdb" indicator (MinorVersion == 0x504d) is clear.
            // The NI PDB generated currently is a full PDB.
            builder.EmitUShort(0 /* MinorVersion */);
            builder.EmitInt((int)DebugDirectoryEntryType.CodeView);
            builder.EmitInt(Size);
            builder.EmitReloc(this, RelocType.IMAGE_REL_BASED_ADDR32NB);
            builder.EmitReloc(this, RelocType.IMAGE_REL_FILE_ABSOLUTE);
        }
    }

    public class CopiedDebugDirectoryEntryNode : DebugDirectoryEntryNode
    {
        private readonly int _debugEntryIndex;

        public override int ClassCode => 1558397;

        public CopiedDebugDirectoryEntryNode(EcmaModule sourceModule, int debugEntryIndex)
            : base(sourceModule)
        {
            Debug.Assert(debugEntryIndex >= 0);
            _debugEntryIndex = debugEntryIndex;
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append($"__CopiedDebugEntryNode_{_debugEntryIndex}_{_module.Assembly.GetName().Name}");
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            if (relocsOnly)
            {
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });
            }

            ImmutableArray<DebugDirectoryEntry> entries = _module.PEReader.SafeReadDebugDirectory();
            Debug.Assert(entries != null && _debugEntryIndex < entries.Length);

            DebugDirectoryEntry sourceDebugEntry = entries[_debugEntryIndex];

            PEMemoryBlock block = _module.PEReader.GetSectionData(sourceDebugEntry.DataRelativeVirtualAddress);
            byte[] result = new byte[sourceDebugEntry.DataSize];
            block.GetContent(0, sourceDebugEntry.DataSize).CopyTo(result);

            return new ObjectData(result, Array.Empty<Relocation>(), _module.Context.Target.PointerSize, new ISymbolDefinitionNode[] { this });
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            int moduleComp = base.CompareToImpl(other, comparer);
            if (moduleComp != 0)
                return moduleComp;

            return _debugEntryIndex - ((CopiedDebugDirectoryEntryNode)other)._debugEntryIndex;
        }
    }
}
