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

        public NativeDebugDirectoryEntryNode(EcmaModule sourceModule)
            : base(sourceModule)
        { }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append($"__NativeRvaBlob_{_module.Assembly.GetName().Name}");
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

        public unsafe byte[] GenerateRSDSEntryData(byte[] md5Hash)
        {
            byte[] rsdsEntry = new byte[RSDSSize];

            fixed (byte* pData = rsdsEntry)
            {
                byte* pSignature = pData + 4;
                byte* pAge = pSignature + 16;
                byte* pFileName = pAge + 4;

                *(uint*)pData = 0x53445352;     // Magic "RSDS"
                *(uint*)pAge = 1;

                // our PDB signature will be the same as our NGEN signature.
                // However we want the printed version of the GUID to be the same as the
                // byte dump of the signature so we swap bytes to make this work.
                Debug.Assert(md5Hash.Length == 16);
                *(uint*)pSignature = (uint)((md5Hash[0] * 256 + md5Hash[1]) * 256 + md5Hash[2]) * 256 + md5Hash[3];
                pSignature += 4;
                *(ushort*)pSignature = (ushort)(md5Hash[4] * 256 + md5Hash[5]);
                pSignature += 2;
                *(ushort*)pSignature = (ushort)(md5Hash[6] * 256 + md5Hash[7]);
                Array.Copy(md5Hash, 8, rsdsEntry, 12, 8);

                string moduleFilePath = ((CompilerTypeSystemContext)_module.Context).GetFilePathForLoadedModule(_module);
                Debug.Assert(!String.IsNullOrEmpty(moduleFilePath));
                string pdbFileName = Path.GetFileNameWithoutExtension(moduleFilePath) + ".ni.pdb";
                Debug.Assert(pdbFileName.Length < 260);

                byte[] pdbFileNameBytes = Encoding.ASCII.GetBytes(pdbFileName);
                Array.Copy(Encoding.ASCII.GetBytes(pdbFileName), 0, rsdsEntry, (pFileName - pData), pdbFileNameBytes.Length);
            }

            return rsdsEntry;
        }
    }

    public class CopiedDebugDirectoryEntryNode : DebugDirectoryEntryNode
    {
        private readonly DebugDirectoryEntry _sourceDebugEntry;

        public override int ClassCode => 1558397;

        public CopiedDebugDirectoryEntryNode(EcmaModule sourceModule, DebugDirectoryEntry sourceEntry)
            : base(sourceModule)
        {
            Debug.Assert(sourceEntry.DataRelativeVirtualAddress > 0 && sourceEntry.DataSize > 0);
            _sourceDebugEntry = sourceEntry;
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append($"__CopiedDebugEntryNode_{_sourceDebugEntry.DataRelativeVirtualAddress}_{_module.Assembly.GetName().Name}");
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            if (relocsOnly)
            {
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });
            }

            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.RequireInitialPointerAlignment();
            builder.AddSymbol(this);

            PEMemoryBlock block = _module.PEReader.GetSectionData(_sourceDebugEntry.DataRelativeVirtualAddress);
            byte[] result = new byte[_sourceDebugEntry.DataSize];
            block.GetContent(0, _sourceDebugEntry.DataSize).CopyTo(result);
            builder.EmitBytes(result);

            return builder.ToObjectData();
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            int moduleComp = base.CompareToImpl(other, comparer);
            if (moduleComp != 0)
                return moduleComp;

            return _sourceDebugEntry.DataRelativeVirtualAddress - ((CopiedDebugDirectoryEntryNode)other)._sourceDebugEntry.DataRelativeVirtualAddress;
        }
    }
}
