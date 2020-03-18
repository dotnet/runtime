// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections.Immutable;
using System.Diagnostics;
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

        public DebugDirectoryNode(EcmaModule sourceModule)
        {
            _module = sourceModule;
        }

        public override ObjectNodeSection Section => ObjectNodeSection.TextSection;

        public override bool IsShareable => false;

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;

        public override int ClassCode => 315358387;

        public override bool StaticDependenciesAreComputed => true;

        public int Offset => 0;

        public int Size => (GetNumDebugDirectoryEntriesInModule() + 1) * ImageDebugDirectorySize;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append($"__DebugDirectory_{_module.Assembly.GetName().Name}");
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        int GetNumDebugDirectoryEntriesInModule()
        {
            ImmutableArray<DebugDirectoryEntry> entries = _module.PEReader.ReadDebugDirectory();
            return entries == null ? 0 : entries.Length;
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.RequireInitialPointerAlignment();
            builder.AddSymbol(this);

            ImmutableArray<DebugDirectoryEntry> entries = _module.PEReader.ReadDebugDirectory();
            int numEntries = GetNumDebugDirectoryEntriesInModule();

            // First, write the native debug directory entry
            {
                var entry = (NativeDebugDirectoryEntryNode)factory.DebugDirectoryEntry(_module, -1);

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

            // Second, copy existing entries from input module
            for(int i = 0; i < numEntries; i++)
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
            return _module.CompareTo(((DebugDirectoryNode)other)._module);
        }
    }
}
