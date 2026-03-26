// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.ReadyToRunConstants;
using Internal.Text;
using Internal.JitInterface;
using System.Diagnostics;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class WasmImportThunkPortableEntrypoint : ObjectNode, ISymbolDefinitionNode, ISortableSymbolNode
    {
        private readonly DelayLoadHelperImport _import;

        public bool UseVirtualCall => _import.UseVirtualCall;
        public bool UseJumpableStub => _import.UseJumpableStub;
        public ReadyToRunHelper HelperId => _import.HelperId;

        public WasmImportThunkPortableEntrypoint(NodeFactory factory, DelayLoadHelperImport import)
        {
            Debug.Assert(!import.UseVirtualCall); // We don't currently support these for WASM, so detect them early so we can diagnose issues faster.
            Debug.Assert(import.HelperId != ReadyToRunHelper.GetString);
            _import = import;
            Debug.Assert(import.Signature is MethodFixupSignature || import.Signature is GenericLookupSignature);
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("WasmImportThunkPortableEntrypoint->"u8);
            _import.AppendMangledName(nameMangler, sb);
        }

        protected override string GetName(NodeFactory factory)
        {
            Utf8StringBuilder sb = new Utf8StringBuilder();
            AppendMangledName(factory.NameMangler, sb);
            return sb.ToString();
        }

        public int Offset => 0;

        public override bool StaticDependenciesAreComputed => true;

        public override bool IsShareable => false;

        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.ReadOnlyDataSection;

        public override int ClassCode => 1738294057;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            WasmImportThunkPortableEntrypoint otherNode = (WasmImportThunkPortableEntrypoint)other;
            return comparer.Compare(_import, otherNode._import);
        }

        private static readonly CorInfoWasmType[] _genericLookupTypes32Bit = new CorInfoWasmType[] { CorInfoWasmType.CORINFO_WASM_TYPE_I32, CorInfoWasmType.CORINFO_WASM_TYPE_I32, CorInfoWasmType.CORINFO_WASM_TYPE_I32 };
        private static readonly CorInfoWasmType[] _genericLookupTypes64Bit = new CorInfoWasmType[] { CorInfoWasmType.CORINFO_WASM_TYPE_I64, CorInfoWasmType.CORINFO_WASM_TYPE_I64, CorInfoWasmType.CORINFO_WASM_TYPE_I64 };

        public override ObjectData GetData(NodeFactory factory, System.Boolean relocsOnly = false)
        {
            // The layout of this matches READYTORUN_IMPORT_THUNK_PORTABLE_ENTRYPOINT

            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.AddSymbol(this);
            WasmTypeNode typeNode;

            RelocType tableIndexPointerRelocType = factory.Target.PointerSize == 4 ? RelocType.WASM_TABLE_INDEX_U32 : RelocType.WASM_TABLE_INDEX_U64;

            if (_import.Signature is GenericLookupSignature)
            {
                typeNode = factory.WasmTypeNode(factory.Target.PointerSize == 4 ? _genericLookupTypes32Bit : _genericLookupTypes64Bit);
            }
            else
            {
                typeNode = factory.WasmTypeNode(((MethodFixupSignature)(_import.Signature)).Method);
            }
            builder.EmitReloc(factory.WasmImportThunk(typeNode, HelperId, _import.Table, UseVirtualCall, UseJumpableStub), tableIndexPointerRelocType);
            builder.EmitReloc(_import, RelocType.IMAGE_REL_BASED_ADDR32NB);
            if (factory.Target.PointerSize == 8)
            {
                builder.EmitUInt(0); // Padding to make the structure the same size on 32 and 64 bit
            }

            return builder.ToObjectData();
        }
    }
}
