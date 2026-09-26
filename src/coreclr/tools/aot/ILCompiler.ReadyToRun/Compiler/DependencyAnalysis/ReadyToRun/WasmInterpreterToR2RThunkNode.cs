// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILCompiler.DependencyAnalysis.Wasm;
using ILCompiler.ObjectWriter;
using ILCompiler.ObjectWriter.WasmInstructions;
using Internal.JitInterface;
using Internal.Text;
using Internal.TypeSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;

using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// A thunk that takes arguments in the interpreter calling convention
    /// (pcode, pArgs, pRet, pPortableEntryPointContext) and calls a function
    /// compiled via R2R with the appropriate wasm-level calling convention.
    /// </summary>
    public class WasmInterpreterToR2RThunkNode : StringDiscoverableAssemblyStubNode, INodeWithTypeSignature, ISymbolDefinitionNode, ISortableSymbolNode
    {
        private readonly TypeSystemContext _context;
        private readonly WasmSignature _wasmSignature;
        private readonly WasmTypeNode _targetTypeNode;

        private const int TerminateR2RStackWalk = 1;

        public override bool StaticDependenciesAreComputed => true;
        public override bool IsShareable => false;
        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.TextSection;

        public override string LookupString => "M" + _wasmSignature.SignatureString;

        private static WasmSignature sigForInterpToR2RThunks = new WasmSignature(new WasmFuncType(new WasmResultType(new WasmValueType[]{WasmValueType.I32, WasmValueType.I32, WasmValueType.I32}), new WasmResultType(Array.Empty<WasmValueType>())), "viii");
        MethodSignature INodeWithTypeSignature.Signature => WasmLowering.RaiseSignature(sigForInterpToR2RThunks, _context);
        bool INodeWithTypeSignature.IsUnmanagedCallersOnly => false;
        bool INodeWithTypeSignature.IsAsyncCall => false;
        bool INodeWithTypeSignature.HasGenericContextArg => false;

        public WasmInterpreterToR2RThunkNode(NodeFactory factory, WasmSignature wasmSignature)
        {
            _context = factory.TypeSystemContext;
            _wasmSignature = wasmSignature;
            _targetTypeNode = factory.WasmTypeNode(wasmSignature);
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("WasmInterpreterToR2RThunk("u8);
            sb.Append(_wasmSignature.SignatureString);
            sb.Append(")"u8);
        }

        protected override string GetName(NodeFactory factory)
        {
            Utf8StringBuilder sb = new Utf8StringBuilder();
            AppendMangledName(factory.NameMangler, sb);
            return sb.ToString();
        }

        public override int ClassCode => 948271450;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            WasmInterpreterToR2RThunkNode otherNode = (WasmInterpreterToR2RThunkNode)other;
            return _wasmSignature.CompareTo(otherNode._wasmSignature);
        }

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            DependencyList dependencies = base.ComputeNonRelocationBasedDependencies(factory);
            dependencies.Add(_targetTypeNode, "Wasm interpreter-to-R2R thunk requires target type node");
            dependencies.Add(factory.WasmTypeNode(sigForInterpToR2RThunks), "Wasm interpreter-to-R2R thunk requires type for the function entry point");
            return dependencies;
        }

        protected override void EmitCode(NodeFactory factory, ref Wasm.WasmEmitter instructionEncoder, bool relocsOnly)
        {
            Debug.Assert(!instructionEncoder.Is64Bit);

            ISymbolNode targetTypeIndex = _targetTypeNode;

            WasmThunkArgLayout layout = new WasmThunkArgLayout(_wasmSignature, _context);

            // The interpreter buffer has no TransitionBlock, so pArgs offsets are ArgIterator offsets
            // less SizeOfTransitionBlock.
            int sizeOfTransitionBlock = layout.TransitionBlock.SizeOfTransitionBlock;

            WasmFuncType targetFuncType = _targetTypeNode.Type;
            bool hasWasmReturn = targetFuncType.Returns.Types.Length > 0;

            // Wasm locals for this thunk:
            //   local 0: portableEntryPoint (I32)
            //   local 1: pArgs (I32)
            //   local 2: pRet (I32)
            //   local 3: savedSp (I32) - save/restore SP global
            const int LocalPortableEntrypoint = 0;
            const int LocalPArgs = 1;
            const int LocalPRet = 2;
            int localSavedSp = 3;

            const int FrameSize = 16; // 16-byte aligned allocation for framePointer

            List<WasmExpr> expressions = new List<WasmExpr>();

            // Save the current stack pointer global
            expressions.Add(Global.Get(WebCilObjectWriter.StackPointerGlobalIndex));
            expressions.Add(Local.Set(localSavedSp));

            // Allocate frame space: sp -= FrameSize
            expressions.Add(Local.Get(localSavedSp));
            expressions.Add(I32.Const(FrameSize));
            expressions.Add(I32.Sub);
            expressions.Add(Global.Set(WebCilObjectWriter.StackPointerGlobalIndex));

            // Write TERMINATE_R2R_STACK_WALK (1) into the framePointer at new SP
            expressions.Add(Global.Get(WebCilObjectWriter.StackPointerGlobalIndex));
            expressions.Add(I32.Const(TerminateR2RStackWalk));
            expressions.Add(I32.Store(0));

            // If there is a wasm return value, push pRet underneath all the call args
            // so that after call_indirect the stack is [pRet, return_value] for the store.
            if (hasWasmReturn)
            {
                expressions.Add(Local.Get(LocalPRet));
            }

            // Param 0: $sp — pointer to the framePointer on the shadow stack
            expressions.Add(Global.Get(WebCilObjectWriter.StackPointerGlobalIndex));

            foreach (WasmThunkArg arg in layout.Args)
            {
                int interpOffset = arg.Offset - sizeOfTransitionBlock;

                if (arg.Kind == WasmThunkArgKind.RetBuf)
                {
                    // Pass pRet directly as the retbuf arg
                    expressions.Add(Local.Get(LocalPRet));
                }
                else if (arg.Kind == WasmThunkArgKind.AsyncContinuation)
                {
                    expressions.Add(I32.Const(0));
                }
                else if (arg.IsIndirectStruct)
                {
                    // Byreference struct — pass a pointer into the incoming pArgs buffer
                    expressions.Add(Local.Get(LocalPArgs));
                    expressions.Add(I32.Const(interpOffset));
                    expressions.Add(I32.Add);
                }
                else
                {
                    int slotSize = arg.IsMultiSlot ? WasmLowering.GetMultiSegmentSlotSize(arg.WasmType) : 0;
                    for (int slot = 0; slot < arg.WasmParamCount; slot++)
                    {
                        expressions.Add(Local.Get(LocalPArgs));
                        expressions.Add(Memory.Load(arg.WasmType, (ulong)(interpOffset + (slot * slotSize))));
                    }
                }
            }

            // Last R2R arg: portable entrypoint context
            expressions.Add(Local.Get(LocalPortableEntrypoint));

            // call_indirect with the target R2R function's type signature
            expressions.Add(Local.Get(LocalPortableEntrypoint));
            expressions.Add(I32.Load(0)); // load the actual function index from the portable entrypoint
            expressions.Add(ControlFlow.CallIndirect(targetTypeIndex, 0));

            // Handle wasm return value — pRet is already on the stack under the return value
            if (hasWasmReturn)
            {
                Debug.Assert(targetFuncType.Returns.Types.Length == 1, "Expected exactly one wasm return type");
                WasmValueType returnWasmType = targetFuncType.Returns.Types[0];

                // Stack is [pRet, return_value]. Store consumes [addr, value].
                expressions.Add(Memory.Store(returnWasmType, 0));
            }

            // For struct returns via retbuf the R2R function has already written the struct into
            // pRet, and there is nothing more to do.

            // Restore the stack pointer global
            expressions.Add(Local.Get(localSavedSp));
            expressions.Add(Global.Set(WebCilObjectWriter.StackPointerGlobalIndex));

            instructionEncoder.FunctionBody = new WasmFunctionBody(
                sigForInterpToR2RThunks.FuncType,
                new[] { WasmValueType.I32 },
                expressions.ToArray());
        }

        protected override void EmitCode(NodeFactory factory, ref X64.X64Emitter instructionEncoder, bool relocsOnly) { throw new NotSupportedException(); }
        protected override void EmitCode(NodeFactory factory, ref X86.X86Emitter instructionEncoder, bool relocsOnly) { throw new NotSupportedException(); }
        protected override void EmitCode(NodeFactory factory, ref ARM.ARMEmitter instructionEncoder, bool relocsOnly) { throw new NotSupportedException(); }
        protected override void EmitCode(NodeFactory factory, ref ARM64.ARM64Emitter instructionEncoder, bool relocsOnly) { throw new NotSupportedException(); }
        protected override void EmitCode(NodeFactory factory, ref LoongArch64.LoongArch64Emitter instructionEncoder, bool relocsOnly) { throw new NotSupportedException(); }
        protected override void EmitCode(NodeFactory factory, ref RiscV64.RiscV64Emitter instructionEncoder, bool relocsOnly) { throw new NotSupportedException(); }
    }
}
