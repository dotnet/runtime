// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;

using Internal.ReadyToRunConstants;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// Signature node for the READYTORUN_FIXUP_InjectStringThunks fixup.
    /// Encodes a series of (null-terminated UTF8 string, 4-byte RVA/table index) pairs,
    /// terminated by an empty string (single 0x00 byte with no trailing RVA).
    /// </summary>
    internal class InjectStringThunksSignature : Signature
    {
        public InjectStringThunksSignature()
        {
        }

        public override int ClassCode => 1493287651;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataSignatureBuilder builder = new ObjectDataSignatureBuilder(factory, relocsOnly);
            builder.AddSymbol(this);
            builder.EmitByte((byte)ReadyToRunFixupKind.InjectStringThunks);

            if (!relocsOnly)
            {
                List<StringDiscoverableAssemblyStubNode> stubs = factory.GetStringDiscoverableStubs();
                stubs.Sort((a, b) => string.CompareOrdinal(a.LookupString, b.LookupString));

                foreach (StringDiscoverableAssemblyStubNode stub in stubs)
                {
                    // Emit the null-terminated UTF8 string
                    byte[] stringBytes = Encoding.UTF8.GetBytes(stub.LookupString);
                    builder.EmitBytes(stringBytes);
                    builder.EmitByte(0); // null terminator

                    // Emit a 4-byte relocation to the stub code.
                    // On WASM, this is a table index; on other platforms, an RVA.
                    RelocType relocType = factory.Target.Architecture == TargetArchitecture.Wasm32
                        ? RelocType.WASM_TABLE_INDEX_REL_I32
                        : RelocType.IMAGE_REL_BASED_ADDR32NB;
                    builder.EmitReloc(stub, relocType, delta: factory.Target.CodeDelta);
                }
            }

            // Terminal empty string (no trailing RVA)
            builder.EmitByte(0);

            return builder.ToObjectData();
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append("InjectStringThunks"u8);
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            // There should only be one instance of this signature per compilation
            return 0;
        }
    }
}
