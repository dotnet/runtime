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
        private readonly WasmSignature _wasmSignature;

        private readonly ImportThunkKind _thunkKind;

        private readonly ImportSectionNode _containingImportSection;

        public override bool StaticDependenciesAreComputed => true;

        public override bool IsShareable => false;

        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.TextSection;
        /// <summary>
        /// Import thunks are used to call a runtime-provided helper which fixes up an indirection cell in a particular
        /// import section. Optionally they may also contain a relocation for a specific indirection cell to fix up.
        /// </summary>
        public WasmImportThunk(NodeFactory factory, WasmSignature wasmSignature, ReadyToRunHelper helperId, ImportSectionNode containingImportSection, bool useVirtualCall, bool useJumpableStub)
        {
            _context = factory.TypeSystemContext;
            _wasmSignature = wasmSignature;
            _typeNode = factory.WasmTypeNode(wasmSignature);
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
            sb.Append($"(ImportSection:{_containingImportSection.Name},Kind:{_thunkKind},Sig:{_wasmSignature.SignatureString})");
        }

        protected override string GetName(NodeFactory factory)
        {
            Utf8StringBuilder sb = new Utf8StringBuilder();
            AppendMangledName(factory.NameMangler, sb);
            return sb.ToString();
        }

        public override int ClassCode => 948271336;

        MethodSignature INodeWithTypeSignature.Signature => WasmLowering.RaiseSignature(_wasmSignature, _context);

        bool INodeWithTypeSignature.IsUnmanagedCallersOnly => false;
        bool INodeWithTypeSignature.IsAsyncCall => false;
        bool INodeWithTypeSignature.HasGenericContextArg => false;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            WasmImportThunk otherNode = (WasmImportThunk)other;
            int result = ((int)_thunkKind).CompareTo((int)otherNode._thunkKind);
            if (result != 0)
                return result;

            result = _wasmSignature.CompareTo(otherNode._wasmSignature);
            if (result != 0)
                return result;

            result = ((ImportSectionNode)_containingImportSection).CompareToImpl((ImportSectionNode)otherNode._containingImportSection, comparer);
            if (result != 0)
                return result;

            return comparer.Compare(_helperCell, otherNode._helperCell);
        }

        static CorInfoWasmType[] _helperTypeParams = new CorInfoWasmType[] { CorInfoWasmType.CORINFO_WASM_TYPE_I32, CorInfoWasmType.CORINFO_WASM_TYPE_I32, CorInfoWasmType.CORINFO_WASM_TYPE_I32, CorInfoWasmType.CORINFO_WASM_TYPE_I32, CorInfoWasmType.CORINFO_WASM_TYPE_I32 };

        protected override void EmitCode(NodeFactory factory, ref Wasm.WasmEmitter instructionEncoder, bool relocsOnly)
        {
            Debug.Assert(_thunkKind == ImportThunkKind.DelayLoadHelper);
            Debug.Assert(!instructionEncoder.Is64Bit); // We currently only support 32-bit, and the thunk logic is currently tied to that assumption

            // WASM-TODO! This is NOT an efficient way to implement this thunk. Currently it writes all the arguments to the stack, not just the ones which need to be saved for GC purposes.
            // At some point we'll want to only write the arguments which need GC tracking, and skip the save/restore for other arguments. This might require changes to code
            // which is currently architecture neutral on the VM side, so we should wait to do this until we have a better picture of how the VM and compiler sides will interact for this thunk.

            ISymbolNode helperTypeIndex = factory.WasmTypeNode(_helperTypeParams);

            MethodSignature methodSignature = WasmLowering.RaiseSignature(_wasmSignature, _context);
            (ArgIterator argit, TransitionBlock transitionBlock) = GCRefMapBuilder.BuildArgIterator(methodSignature, _context);

            int[] offsets = new int[methodSignature.Length];
            bool[] isIndirectStructArg = new bool[methodSignature.Length];

            int argIndex = 0;
            int argOffset;
            while ((argOffset = argit.GetNextOffset()) != TransitionBlock.InvalidOffset)
            {
                offsets[argIndex] = argOffset;
                isIndirectStructArg[argIndex] = argit.IsArgPassedByRef() && argit.IsValueType();
                argIndex++;
            }

            argit.Reset();

            // The arguments are $sp, ARG0-ARGN, PortableEntrypointThunk.
            // Compute stack offset needed.

            // Align total allocation (args + transition block) to 16 byte boundaries
            int sizeOfStoredLocals = AlignmentHelper.AlignUp(argit.SizeOfFrameArgumentArray() + transitionBlock.SizeOfTransitionBlock, 16);

            List<WasmExpr> expressions = new List<WasmExpr>();
            // local.get 0
            expressions.Add(Local.Get(0));
            // i32.const {sizeOfStoredLocals}
            expressions.Add(I32.Const(sizeOfStoredLocals));
            // i32.sub
            expressions.Add(I32.Sub);
            // local.set 0
            expressions.Add(Local.Set(0));

            // Initialize m_ReturnAddress to 0 at offset 0 of the transition block
            // The 0 is a marker that the actual return address is to be computed from the m_StackPointer at offset 4.
            expressions.Add(Local.Get(0));
            expressions.Add(I32.Const(0));
            expressions.Add(I32.Store(0));

            // Store the original caller's frame pointer (SP before allocation) at offset 4
            expressions.Add(Local.Get(0));
            expressions.Add(Local.Get(0));
            expressions.Add(I32.Const(sizeOfStoredLocals));
            expressions.Add(I32.Add);
            expressions.Add(I32.Store(4));

            //
            // ; Stash all the locals away
            // for (int i = 0; i < N; i++)
            // {
            //   local.get 0
            //   local.get (i+1)
            //   i32(or i64/fp32/fp64).store (offset from base)
            // }

            // In the calling convention, the first arg is the sp arg, and the last is the portable entrypoint arg. Each of those are treated specially
            // Iterate over the raised MethodSignature params rather than wasm-level types.
            // This allows us to:
            //   - Skip empty struct params (no wasm local exists)
            //   - Zero-fill indirect struct params instead of copying the byref pointer
            bool hasThis = !methodSignature.IsStatic;
            int wasmLocalIndex = 1; // local 0 is $sp

            // Store 'this' pointer if present — it occupies a wasm local but is not in the raised MethodSignature params
            if (hasThis)
            {
                expressions.Add(Local.Get(0));
                expressions.Add(Local.Get(wasmLocalIndex));
                expressions.Add(I32.Store((ulong)transitionBlock.ThisOffset));
                wasmLocalIndex++;
            }

            for (int i = 0; i < methodSignature.Length; i++)
            {
                TypeDesc paramType = methodSignature[i];

                if (WasmLowering.IsEmptyStruct(paramType))
                {
                    // Empty struct — no wasm local, nothing to store
                    continue;
                }

                int currentOffset = offsets[i];

                if (isIndirectStructArg[i])
                {
                    // Indirect struct — zero-fill the transition block slot instead of copying the byref pointer.
                    int structSize = paramType.GetElementSize().AsInt;
                    int fillSize = AlignmentHelper.AlignUp(structSize, 8);

                    // memory.fill: (dst, val, len) -> ()
                    expressions.Add(Local.Get(0));
                    expressions.Add(I32.Const(currentOffset));
                    expressions.Add(I32.Add);
                    expressions.Add(I32.Const(0));
                    expressions.Add(I32.Const(fillSize));
                    expressions.Add(Memory.Fill());
                    wasmLocalIndex++;
                }
                else
                {
                    expressions.Add(Local.Get(0));
                    expressions.Add(Local.Get(wasmLocalIndex));
                    WasmValueType type = _typeNode.Type.Params.Types[wasmLocalIndex];
                    switch (type)
                    {
                        case WasmValueType.I32:
                            expressions.Add(I32.Store((ulong)currentOffset));
                            break;
                        case WasmValueType.F32:
                            expressions.Add(F32.Store((ulong)currentOffset));
                            break;
                        case WasmValueType.I64:
                            expressions.Add(I64.Store((ulong)currentOffset));
                            break;
                        case WasmValueType.F64:
                            expressions.Add(F64.Store((ulong)currentOffset));
                            break;
                        case WasmValueType.V128:
                            expressions.Add(V128.Store((ulong)currentOffset));
                            break;

                        default:
                            throw new System.Exception("Unexpected wasm type arg");
                    }
                    wasmLocalIndex++;
                }
            }
            //
            // ; Call the right helper to fill in the table
            // local.get (PortableEntrypointThunk)
            int portableEntrypointLocalIndex = _typeNode.Type.Params.Types.Length - 1;

            expressions.Add(Local.Get(0)); // The address of the args is passed as the first argument
            expressions.Add(Local.Get(portableEntrypointLocalIndex)); // The address of the portable entrypoint is passed as the second
            expressions.Add(Global.Get(WasmObjectWriter.ImageBaseGlobalIndex)); // The module base address is passed as the third argument

            // Pass the RVA of the Module fixup as the fourth argument
            // i32.const (RVA of Module fixup)
            expressions.Add(I32.ConstRVA(factory.ModuleImport));

            // Load the helper function address and dispatch
            // global.get {module base}
            expressions.Add(Global.Get(WasmObjectWriter.ImageBaseGlobalIndex)); // Module base used to load the helper function address
            expressions.Add(I32.LoadWithRVAOffset(_helperCell)); // Load the helper call function pointer from the helper cell, using a load with an RVA offset so that the helper cell can be left as a zero in the R2R image and fixed up at runtime. This avoids the need to emit a runtime relocation for the helper cell.
            // call_indirect (i32, i32, i32, i32) -> (i32)
            expressions.Add(ControlFlow.CallIndirect(helperTypeIndex, 0));

            // local.set (PortableEntrypointThunk)  / At this point we can overwrite with the incoming portable entrypoint local, since the old value will no longer be used
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
            // Iterate over the raised MethodSignature params to handle indirect/empty structs correctly
            wasmLocalIndex = 1;

            // Restore 'this' pointer if present
            if (hasThis)
            {
                expressions.Add(Local.Get(0));
                expressions.Add(I32.Load((ulong)transitionBlock.ThisOffset));
                wasmLocalIndex++;
            }

            for (int i = 0; i < methodSignature.Length; i++)
            {
                TypeDesc paramType = methodSignature[i];

                if (WasmLowering.IsEmptyStruct(paramType))
                {
                    // Empty struct — no wasm local, nothing to restore
                    continue;
                }

                if (isIndirectStructArg[i])
                {
                    // Indirect struct — pass the original byref pointer from the caller
                    expressions.Add(Local.Get(wasmLocalIndex));
                    wasmLocalIndex++;
                }
                else
                {
                    expressions.Add(Local.Get(0));
                    WasmValueType type = _typeNode.Type.Params.Types[wasmLocalIndex];
                    int currentOffset = offsets[i];
                    switch (type)
                    {
                        case WasmValueType.I32:
                            expressions.Add(I32.Load((ulong)currentOffset));
                            break;
                        case WasmValueType.F32:
                            expressions.Add(F32.Load((ulong)currentOffset));
                            break;
                        case WasmValueType.I64:
                            expressions.Add(I64.Load((ulong)currentOffset));
                            break;
                        case WasmValueType.F64:
                            expressions.Add(F64.Load((ulong)currentOffset));
                            break;
                        case WasmValueType.V128:
                            expressions.Add(V128.Load((ulong)currentOffset));
                            break;

                        default:
                            throw new System.Exception("Unexpected wasm type arg");
                    }
                    wasmLocalIndex++;
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
