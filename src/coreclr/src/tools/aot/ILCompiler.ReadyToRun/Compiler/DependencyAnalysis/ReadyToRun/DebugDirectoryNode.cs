// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection.PortableExecutable;
using Internal.Text;
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
        private bool _insertDeterministicEntry;

        public DebugDirectoryNode(EcmaModule sourceModule, string outputFileName)
        {
            _module = sourceModule;
            _insertDeterministicEntry = sourceModule == null; // Mark module as deterministic if generating composite image
            string pdbNameRoot = Path.GetFileNameWithoutExtension(outputFileName);
            if (sourceModule != null)
            {
                pdbNameRoot = sourceModule.Assembly.GetName().Name;
            }
            _nativeEntry = new NativeDebugDirectoryEntryNode(pdbNameRoot + ".ni.pdb");
        }

        public override ObjectNodeSection Section => ObjectNodeSection.TextSection;

        public override bool IsShareable => false;

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;

        public override int ClassCode => 315358387;

        public override bool StaticDependenciesAreComputed => true;

        public int Offset => 0;

        public int Size => (GetNumDebugDirectoryEntriesInModule() + 1 + (_insertDeterministicEntry ? 1 : 0)) * ImageDebugDirectorySize;

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

            ImmutableArray<DebugDirectoryEntry> entries = _module.PEReader.ReadDebugDirectory();
            return entries == null ? 0 : entries.Length;
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.RequireInitialPointerAlignment();
            builder.AddSymbol(this);

            ImmutableArray<DebugDirectoryEntry> entries = default(ImmutableArray<DebugDirectoryEntry>);
            if (_module != null)
                entries = _module.PEReader.ReadDebugDirectory();

            int numEntries = GetNumDebugDirectoryEntriesInModule();

            // First, write the native debug directory entry
            {
                var entry = _nativeEntry;

                builder.EmitUInt(0 /* Characteristics */);
                if (numEntries > 0)
                {
                    builder.EmitUInt(entries[0].Stamp);
                    builder.EmitUShort(entries[0].MajorVersion);
                }
                else
                {
                    builder.EmitUInt(0);
                    builder.EmitUShort(0);
                }
                // Make sure the "is portable pdb" indicator (MinorVersion == 0x504d) is clear
                // for the NGen debug directory entry since this debug directory can be copied
                // from an existing entry which could be a portable pdb.
                builder.EmitUShort(0 /* MinorVersion */);
                builder.EmitInt((int)DebugDirectoryEntryType.CodeView);
                builder.EmitInt(entry.Size);
                builder.EmitReloc(entry, RelocType.IMAGE_REL_BASED_ADDR32NB);
                builder.EmitReloc(entry, RelocType.IMAGE_REL_FILE_ABSOLUTE);
            }

            // If generating a composite image, emit the deterministic marker
            if (_insertDeterministicEntry)
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
