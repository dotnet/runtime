// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILCompiler.DependencyAnalysis.Wasm;
using Internal.ReadyToRunConstants;
using Internal.Text;
using Internal.JitInterface;
using Internal.TypeSystem;
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

        private static readonly WasmSignature _genericLookupSignature32Bit = new WasmSignature(
            new WasmFuncType(
                new WasmResultType(new[] { WasmValueType.I32, WasmValueType.I32 }),
                new WasmResultType(new[] { WasmValueType.I32 })),
            "iii");
        private static readonly WasmSignature _genericLookupSignature64Bit = new WasmSignature(
            new WasmFuncType(
                new WasmResultType(new[] { WasmValueType.I64, WasmValueType.I64 }),
                new WasmResultType(new[] { WasmValueType.I64 })),
            "lll");

        public override ObjectData GetData(NodeFactory factory, System.Boolean relocsOnly = false)
        {
            // The layout of this matches READYTORUN_IMPORT_THUNK_PORTABLE_ENTRYPOINT

            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.AddSymbol(this);
            WasmSignature wasmSignature;

            RelocType tableIndexPointerRelocType = factory.Target.PointerSize == 4 ? RelocType.WASM_TABLE_INDEX_I32 : RelocType.WASM_TABLE_INDEX_I64;

            if (_import.Signature is GenericLookupSignature)
            {
                wasmSignature = factory.Target.PointerSize == 4 ? _genericLookupSignature32Bit : _genericLookupSignature64Bit;
            }
            else
            {
                MethodDesc method = ((MethodFixupSignature)(_import.Signature)).Method;
                // The import thunk always uses managed calling convention ($sp + PE entrypoint)
                // even if the underlying method is UnmanagedCallersOnly, because the thunk is
                // called from R2R-generated managed code.
                WasmLowering.LoweringFlags flags = WasmLowering.GetLoweringFlags(method) & ~WasmLowering.LoweringFlags.IsUnmanagedCallersOnly;
                wasmSignature = WasmLowering.GetSignature(method.Signature, flags);
            }
            builder.EmitReloc(factory.WasmImportThunk(wasmSignature, HelperId, _import.Table, UseVirtualCall, UseJumpableStub), tableIndexPointerRelocType);
            builder.EmitReloc(_import, RelocType.IMAGE_REL_BASED_ADDR32NB);
            if (factory.Target.PointerSize == 8)
            {
                builder.EmitUInt(0); // Padding to make the structure the same size on 32 and 64 bit
            }

            return builder.ToObjectData();
        }
    }
}
