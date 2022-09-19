// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class DebugDirectoryNode : ObjectNode, ISymbolDefinitionNode
    {
        const int ImageDebugDirectorySize =
            sizeof(int) +   // Characteristics
            sizeof(int) +   // TimeDateStamp
            sizeof(short) + // MajorVersion:
            sizeof(short) + // MinorVersion
            sizeof(int) +   // Type
            sizeof(int) +   // SizeOfData:
            sizeof(int) +   // AddressOfRawData:
            sizeof(int);    // PointerToRawData

        private EcmaModule _module;
        private NativeDebugDirectoryEntryNode _nativeEntry;
        private PerfMapDebugDirectoryEntryNode _perfMapEntry;

        private bool _insertDeterministicEntry;

        public DebugDirectoryNode(EcmaModule sourceModule, string outputFileName, bool shouldAddNiPdb, bool shouldGeneratePerfmap)
        {
            _module = sourceModule;
            _insertDeterministicEntry = sourceModule == null; // Mark module as deterministic if generating composite image
            string pdbNameRoot = Path.GetFileNameWithoutExtension(outputFileName);
            if (sourceModule != null)
            {
                pdbNameRoot = sourceModule.Assembly.GetName().Name;
            }

            if (shouldAddNiPdb)
            {
                _nativeEntry = new NativeDebugDirectoryEntryNode(pdbNameRoot + ".ni.pdb");
            }

            if (shouldGeneratePerfmap)
            {
                _perfMapEntry = new PerfMapDebugDirectoryEntryNode(pdbNameRoot + ".ni.r2rmap");
            }
        }

        public override ObjectNodeSection Section => ObjectNodeSection.TextSection;

        public override bool IsShareable => false;

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;

        public override int ClassCode => 315358387;

        public override bool StaticDependenciesAreComputed => true;

        public int Offset => 0;

        public int Size => (GetNumDebugDirectoryEntriesInModule()
            + (_nativeEntry is not null ? 1 : 0)
            + (_perfMapEntry is not null ? 1 : 0)
            + (_insertDeterministicEntry ? 1 : 0)) * ImageDebugDirectorySize;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            string directoryName;
            if (_module != null)
                directoryName = _module.Assembly.GetName().Name;
            else
                directoryName = "Composite";

            sb.Append($"__DebugDirectory_{directoryName}");
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        int GetNumDebugDirectoryEntriesInModule()
        {
            if (_module == null)
                return 0;

            ImmutableArray<DebugDirectoryEntry> entries = _module.PEReader.SafeReadDebugDirectory();
            return entries == null ? 0 : entries.Length;
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.RequireInitialPointerAlignment();
            builder.AddSymbol(this);

            ImmutableArray<DebugDirectoryEntry> entries = ImmutableArray<DebugDirectoryEntry>.Empty;

            if (_module != null)
                entries = _module.PEReader.SafeReadDebugDirectory();

            int numEntries = GetNumDebugDirectoryEntriesInModule();

            // Reuse the module's PDB entry
            DebugDirectoryEntry pdbEntry = entries.Where(s => s.Type == DebugDirectoryEntryType.CodeView).FirstOrDefault();

            // NI PDB entry
            _nativeEntry?.EmitHeader(ref builder, pdbEntry.Stamp, pdbEntry.MajorVersion);

            _perfMapEntry?.EmitHeader(ref builder);

            // If generating a composite image, emit the deterministic marker
            if (_insertDeterministicEntry)
            {
                DeterministicDebugDirectoryEntry.EmitHeader(ref builder);
            }

            // Second, copy existing entries from input module
            for (int i = 0; i < numEntries; i++)
            {
                builder.EmitUInt(0 /* Characteristics */);
                builder.EmitUInt(entries[i].Stamp);
                builder.EmitUShort(entries[i].MajorVersion);
                builder.EmitUShort(entries[i].MinorVersion);
                builder.EmitInt((int)entries[i].Type);
                builder.EmitInt(entries[i].DataSize);
                if (entries[i].DataSize == 0)
                {
                    builder.EmitUInt(0);
                    builder.EmitUInt(0);
                }
                else
                {
                    builder.EmitReloc(factory.DebugDirectoryEntry(_module, i), RelocType.IMAGE_REL_BASED_ADDR32NB);
                    builder.EmitReloc(factory.DebugDirectoryEntry(_module, i), RelocType.IMAGE_REL_FILE_ABSOLUTE);
                }
            }

            Debug.Assert(builder.CountBytes == Size);

            return builder.ToObjectData();
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            if (_module == null)
            {
                if (((DebugDirectoryNode)other)._module == null)
                    return 0;
                return -1;
            }
            else if (((DebugDirectoryNode)other)._module == null)
            {
                return 1;
            }

            return _module.CompareTo(((DebugDirectoryNode)other)._module);
        }
    }
}
