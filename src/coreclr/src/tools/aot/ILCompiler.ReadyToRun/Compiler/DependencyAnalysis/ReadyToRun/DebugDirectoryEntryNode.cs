// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Text;
using System.Diagnostics;
using System.Reflection.PortableExecutable;

using Internal.Text;
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

    public class NativeDebugDirectoryEntryNode : DebugDirectoryEntryNode
    {
        const int RSDSSize =
            sizeof(int) +   // Magic
            16 +            // Signature (guid)
            sizeof(int) +   // Age
            260;            // FileName

        public override int ClassCode => 119958401;

        public unsafe int Size => RSDSSize;

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

        public byte[] GenerateRSDSEntryData(byte[] md5Hash)
        {
            MemoryStream rsdsEntry = new MemoryStream(RSDSSize);

            using (BinaryWriter writer = new BinaryWriter(rsdsEntry))
            {
                // Magic "RSDS"
                writer.Write((uint)0x53445352);

                // The PDB signature will be the same as our NGEN signature.
                // However we want the printed version of the GUID to be the same as the
                // byte dump of the signature so we swap bytes to make this work.
                Debug.Assert(md5Hash.Length == 16);
                writer.Write((uint)((md5Hash[0] * 256 + md5Hash[1]) * 256 + md5Hash[2]) * 256 + md5Hash[3]);
                writer.Write((ushort)(md5Hash[4] * 256 + md5Hash[5]));
                writer.Write((ushort)(md5Hash[6] * 256 + md5Hash[7]));
                writer.Write(md5Hash, 8, 8);

                // Age
                writer.Write(1);

                string pdbFileName = _pdbName;
                byte[] pdbFileNameBytes = Encoding.UTF8.GetBytes(pdbFileName);
                writer.Write(pdbFileNameBytes);

                Debug.Assert(rsdsEntry.Length <= RSDSSize);
                return rsdsEntry.ToArray();
            }
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return _pdbName.CompareTo(((NativeDebugDirectoryEntryNode)other)._pdbName);
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

            ImmutableArray<DebugDirectoryEntry> entries = _module.PEReader.ReadDebugDirectory();
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
