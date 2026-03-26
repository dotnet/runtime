// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILCompiler.DependencyAnalysis.Wasm;
using ILCompiler.ObjectWriter;
using ILCompiler.ObjectWriter.WasmInstructions;
using Internal.JitInterface;
using Internal.Text;
using Internal.TypeSystem;
using Internal.ReadyToRunConstants;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class WasmImportThunk : AssemblyStubNode, INodeWithTypeSignature, ISymbolDefinitionNode, ISortableSymbolNode
    {
        private readonly TypeSystemContext _context;
        private readonly Import _helperCell;
        private readonly WasmTypeNode _typeNode;

        private readonly ImportThunkKind _thunkKind;

        private readonly ImportSectionNode _containingImportSection;

        public override bool StaticDependenciesAreComputed => true;

        public override bool IsShareable => false;

        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.TextSection;
        /// <summary>
        /// Import thunks are used to call a runtime-provided helper which fixes up an indirection cell in a particular
        /// import section. Optionally they may also contain a relocation for a specific indirection cell to fix up.
        /// </summary>
        public WasmImportThunk(NodeFactory factory, WasmTypeNode typeNode, ReadyToRunHelper helperId, ImportSectionNode containingImportSection, bool useVirtualCall, bool useJumpableStub)
        {
            _context = factory.TypeSystemContext;
            _typeNode = typeNode;
            _helperCell = factory.GetReadyToRunHelperCell(helperId);
            _containingImportSection = containingImportSection;

            if (useVirtualCall)
            {
                // In wasm we should always be using a helper to get the function pointer target, and then dispatching on that instead of using a thunk
                throw new System.NotSupportedException(nameof(useVirtualCall));
            }
            else if (useJumpableStub)
            {
                _thunkKind = ImportThunkKind.DelayLoadHelperWithExistingIndirectionCell;
            }
            else if (helperId == ReadyToRunHelper.GetString)
            {
                // This helper is only used for a size optimization, which will not be relevant in the WASM case, so we should fix any logic in the compiler that tries to use this sort of helper
                throw new System.NotSupportedException(nameof(helperId));
            }
            else if (helperId == ReadyToRunHelper.DelayLoad_MethodCall ||
                helperId == ReadyToRunHelper.DelayLoad_Helper ||
                helperId == ReadyToRunHelper.DelayLoad_Helper_Obj ||
                helperId == ReadyToRunHelper.DelayLoad_Helper_ObjObj)
            {
                _thunkKind = ImportThunkKind.DelayLoadHelper;
            }
            else
            {
                // Unknown helper kind, we should not be trying to produce a thunk for it
                throw new System.ArgumentException(nameof(helperId));
            }
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("WasmDelayLoadHelper->"u8);
            _helperCell.AppendMangledName(nameMangler, sb);
            sb.Append($"(ImportSection:{_containingImportSection.Name},Kind:{_thunkKind})");
            _typeNode.AppendMangledName(nameMangler, sb);
        }

        protected override string GetName(NodeFactory factory)
        {
            Utf8StringBuilder sb = new Utf8StringBuilder();
            AppendMangledName(factory.NameMangler, sb);
            return sb.ToString();
        }

        public override int ClassCode => 948271336;

        MethodSignature INodeWithTypeSignature.Signature => WasmLowering.RaiseSignature(_typeNode.Type, _context);

        bool INodeWithTypeSignature.IsUnmanagedCallersOnly => false;
        bool INodeWithTypeSignature.IsAsyncCall => false;
        bool INodeWithTypeSignature.HasGenericContextArg => false;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            WasmImportThunk otherNode = (WasmImportThunk)other;
            int result = ((int)_thunkKind).CompareTo((int)otherNode._thunkKind);
            if (result != 0)
                return result;

            result = _typeNode.CompareToImpl(otherNode._typeNode, comparer);
            if (result != 0)
                return result;

            result = ((ImportSectionNode)_containingImportSection).CompareToImpl((ImportSectionNode)otherNode._containingImportSection, comparer);
            if (result != 0)
                return result;

            return comparer.Compare(_helperCell, otherNode._helperCell);
        }

        static CorInfoWasmType[] _helperTypeParams = new CorInfoWasmType[] { CorInfoWasmType.CORINFO_WASM_TYPE_I32, CorInfoWasmType.CORINFO_WASM_TYPE_I32, CorInfoWasmType.CORINFO_WASM_TYPE_I32, CorInfoWasmType.CORINFO_WASM_TYPE_I32 };

        protected override void EmitCode(NodeFactory factory, ref Wasm.WasmEmitter instructionEncoder, bool relocsOnly)
        {
            Debug.Assert(_thunkKind == ImportThunkKind.DelayLoadHelper);
            Debug.Assert(!instructionEncoder.Is64Bit); // We currently only support 32-bit, and the thunk logic is currently tied to that assumption

            // WASM-TODO! This is NOT an efficient way to implement this thunk. Currently it writes all the arguments to the stack, not just the ones which need to be saved for GC purposes.
            // At some point we'll want to only write the arguments which need GC tracking, and skip the save/restore for other arguments. This will require changes to code
            // which is currently architecture neutral on the VM side, so we should wait to do this until we have a better picture of how the VM and compiler sides will interact for this thunk.

            ISymbolNode helperTypeIndex = factory.WasmTypeNode(_helperTypeParams);

            // The arguments are $sp, ARG0-ARGN, PortableEntrypointThunk.
            // The general logic is...
            // Compute stack offset needed.
            int currentOffset = 0;

            for (int i = 1; i < _typeNode.Type.Params.Types.Length - 1; i++)
            {
                WasmValueType type = _typeNode.Type.Params.Types[i];
                switch (type)
                {
                    case WasmValueType.I32:
                    case WasmValueType.F32:
                        currentOffset = AlignmentHelper.AlignUp(currentOffset, 4);
                        currentOffset += 4;
                        break;
                    case WasmValueType.I64:
                    case WasmValueType.F64:
                        currentOffset = AlignmentHelper.AlignUp(currentOffset, 8);
                        currentOffset += 8;
                        break;
                    case WasmValueType.V128:
                        currentOffset = AlignmentHelper.AlignUp(currentOffset, 16);
                        currentOffset += 16;
                        break;

                    default:
                        throw new System.Exception("Unexpected wasm type arg");
                }
            }

            // Align stack to 16 byte boundaries
            int sizeOfStoredLocals = AlignmentHelper.AlignUp(currentOffset, 16);

            List<WasmExpr> expressions = new List<WasmExpr>();
            // local.get 0
            expressions.Add(Local.Get(0));
            // i32.const {sizeOfStoredLocals}
            expressions.Add(I32.Const(sizeOfStoredLocals));
            // i32.sub
            expressions.Add(I32.Sub);
            // local.tee 0
            expressions.Add(Local.Tee(0));

            // global.set {stack pointer global}  // This is a callout from managed to native, we need to set the global stack pointer so that C++ code will work
            expressions.Add(Global.Set(0));

            //
            // ; Stash all the locals away
            // for (int i = 0; i < N; i++)
            // {
            //   local.get 0
            //   local.get (i+1)
            //   i32(or i64/fp32/fp64).store (offset from base)
            // }

            // In the calling convention, the first arg is the sp arg, and the last is the portable entrypoint arg. Each of those are treated specially
            currentOffset = 0;
            for (int i = 1; i < _typeNode.Type.Params.Types.Length - 1; i++)
            {
                expressions.Add(Local.Get(0));
                expressions.Add(Local.Get(i));
                WasmValueType type = _typeNode.Type.Params.Types[i];
                switch (type)
                {
                    case WasmValueType.I32:
                    case WasmValueType.F32:
                        currentOffset = AlignmentHelper.AlignUp(currentOffset, 4);
                        if (type == WasmValueType.I32)
                        {
                            expressions.Add(I32.Store((ulong)currentOffset));
                        }
                        else
                        {
                            expressions.Add(F32.Store((ulong)currentOffset));
                        }
                        currentOffset += 4;
                        break;
                    case WasmValueType.I64:
                    case WasmValueType.F64:
                        currentOffset = AlignmentHelper.AlignUp(currentOffset, 8);
                        if (type == WasmValueType.I64)
                        {
                            expressions.Add(I64.Store((ulong)currentOffset));
                        }
                        else
                        {
                            expressions.Add(F64.Store((ulong)currentOffset));
                        }
                        currentOffset += 8;
                        break;
                    case WasmValueType.V128:
                        currentOffset = AlignmentHelper.AlignUp(currentOffset, 16);
                        expressions.Add(V128.Store((ulong)currentOffset));
                        currentOffset += 16;
                        break;

                    default:
                        throw new System.Exception("Unexpected wasm type arg");
                }
            }
            //
            // ; Call the right helper to fill in the table
            // local.get (PortableEntrypointThunk)
            int portableEntrypointLocalIndex = _typeNode.Type.Params.Types.Length - 1;

            expressions.Add(Local.Get(0)); // The address of the args is passed as the first argument
            expressions.Add(Local.Get(portableEntrypointLocalIndex)); // The address of the portable entrypoint is passed as the second
            expressions.Add(Global.Get(1)); // The module base address is passed as the third argument

            // Load the helper function address and dispatch
            // global.get {module base}
            expressions.Add(Global.Get(1)); // Module base?
            // i32.const (RVA of R2RHelperID)
            expressions.Add(I32.ConstRVA(_helperCell));
            // i32.add
            expressions.Add(I32.Add);
            // i32.load 0
            expressions.Add(I32.Load(0));
            // call_indirect (i32, i32, i32, i32) (returns i32)
            expressions.Add(ControlFlow.CallIndirect(helperTypeIndex, 0));

            // local.set (PortableEntrypointThunk)  / At this point we can overwrite with the incoming portable entrypoint local, since the old value it will no longer be used
            expressions.Add(Local.Set(portableEntrypointLocalIndex));
            //
             // ;Setup sp arg for the final call, with the call address now coming from the portable entrypoint
            // local.get 0
            expressions.Add(Local.Get(0));
            // i32.const {sizeofstoredlocals}
            expressions.Add(I32.Const(sizeOfStoredLocals));
            // i32.add
            expressions.Add(I32.Add);
            //
            // ; Setup normal args
            // for (int i = 0; i < N; i++)
            // {
            //   local.get 0
            //   i32(or i64/fp32/fp64).load (offset from base)
            //   local.set (i+1)
            // }
            // In the calling convention, the first arg is the sp arg, and the last is the portable entrypoint arg. Each of those are treated specially
            currentOffset = 0;
            for (int i = 1; i < _typeNode.Type.Params.Types.Length - 1; i++)
            {
                expressions.Add(Local.Get(0));
                WasmValueType type = _typeNode.Type.Params.Types[i];
                switch (type)
                {
                    case WasmValueType.I32:
                    case WasmValueType.F32:
                        currentOffset = AlignmentHelper.AlignUp(currentOffset, 4);
                        if (type == WasmValueType.I32)
                        {
                            expressions.Add(I32.Load((ulong)currentOffset));
                        }
                        else
                        {
                            expressions.Add(F32.Load((ulong)currentOffset));
                        }
                        currentOffset += 4;
                        break;
                    case WasmValueType.I64:
                    case WasmValueType.F64:
                        currentOffset = AlignmentHelper.AlignUp(currentOffset, 8);
                        if (type == WasmValueType.I64)
                        {
                            expressions.Add(I64.Load((ulong)currentOffset));
                        }
                        else
                        {
                            expressions.Add(F64.Load((ulong)currentOffset));
                        }
                        currentOffset += 8;
                        break;
                    case WasmValueType.V128:
                        currentOffset = AlignmentHelper.AlignUp(currentOffset, 16);
                        expressions.Add(V128.Load((ulong)currentOffset));
                        currentOffset += 16;
                        break;

                    default:
                        throw new System.Exception("Unexpected wasm type arg");
                }
            }
            // ; Add the portable entrypoint arg
            // local.get (PortableEntrypointThunk)
            expressions.Add(Local.Get(portableEntrypointLocalIndex));
            //
            // ; Load the actual target to jump to
            // local.get (PortableEntrypointThunk)
            expressions.Add(Local.Get(portableEntrypointLocalIndex));
            // i32.load 0
            expressions.Add(I32.Load(0));
            // return_call_indirect (actual type index)  ; We can use return_call_index here, or call_index. Semantically they are identical
            expressions.Add(ControlFlow.CallIndirect(_typeNode, 0));

            // Encode as a complete function body
            instructionEncoder.FunctionBody = new WasmFunctionBody(_typeNode.Type, expressions.ToArray());
        }

        protected override void EmitCode(NodeFactory factory, ref X64.X64Emitter instructionEncoder, bool relocsOnly) { throw new NotSupportedException(); }
        protected override void EmitCode(NodeFactory factory, ref X86.X86Emitter instructionEncoder, bool relocsOnly) { throw new NotSupportedException(); }
        protected override void EmitCode(NodeFactory factory, ref ARM.ARMEmitter instructionEncoder, bool relocsOnly) { throw new NotSupportedException(); }
        protected override void EmitCode(NodeFactory factory, ref ARM64.ARM64Emitter instructionEncoder, bool relocsOnly) { throw new NotSupportedException(); }
        protected override void EmitCode(NodeFactory factory, ref LoongArch64.LoongArch64Emitter instructionEncoder, bool relocsOnly) { throw new NotSupportedException(); }
        protected override void EmitCode(NodeFactory factory, ref RiscV64.RiscV64Emitter instructionEncoder, bool relocsOnly) { throw new NotSupportedException(); }

    }
}
