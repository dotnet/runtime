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

using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// A thunk that captures all arguments and dispatches to the interpreter via
    /// READYTORUN_HELPER_R2RToInterpreter. This node is string-discoverable so the
    /// runtime can find it by WasmSignature string at execution time.
    /// </summary>
    public class WasmR2RToInterpreterThunkNode : StringDiscoverableAssemblyStubNode, INodeWithTypeSignature, ISymbolDefinitionNode, ISortableSymbolNode
    {
        private readonly TypeSystemContext _context;
        private readonly WasmSignature _wasmSignature;
        private readonly WasmTypeNode _typeNode;
        private readonly Import _helperCell;

        // Helper signature: (I32, I32, I32, I32) -> ()
        private static readonly CorInfoWasmType[] s_helperTypeParams = new CorInfoWasmType[]
        {
            CorInfoWasmType.CORINFO_WASM_TYPE_VOID,
            CorInfoWasmType.CORINFO_WASM_TYPE_I32,
            CorInfoWasmType.CORINFO_WASM_TYPE_I32,
            CorInfoWasmType.CORINFO_WASM_TYPE_I32,
            CorInfoWasmType.CORINFO_WASM_TYPE_I32,
        };

        public override bool StaticDependenciesAreComputed => true;
        public override bool IsShareable => false;
        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.TextSection;

        public override string LookupString => "I" + _wasmSignature.SignatureString;

        MethodSignature INodeWithTypeSignature.Signature => WasmLowering.RaiseSignature(_wasmSignature, _context);
        bool INodeWithTypeSignature.IsUnmanagedCallersOnly => false;
        bool INodeWithTypeSignature.IsAsyncCall => false;
        bool INodeWithTypeSignature.HasGenericContextArg => false;

        public WasmR2RToInterpreterThunkNode(NodeFactory factory, WasmSignature wasmSignature)
        {
            _context = factory.TypeSystemContext;
            _wasmSignature = wasmSignature;
            _typeNode = factory.WasmTypeNode(wasmSignature);
            _helperCell = factory.GetReadyToRunHelperCell(ReadyToRunHelper.R2RToInterpreter);
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("WasmR2RToInterpreterThunk("u8);
            sb.Append(_wasmSignature.SignatureString);
            sb.Append(")"u8);
        }

        protected override string GetName(NodeFactory factory)
        {
            Utf8StringBuilder sb = new Utf8StringBuilder();
            AppendMangledName(factory.NameMangler, sb);
            return sb.ToString();
        }

        public override int ClassCode => 948271449;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            WasmR2RToInterpreterThunkNode otherNode = (WasmR2RToInterpreterThunkNode)other;
            return _wasmSignature.CompareTo(otherNode._wasmSignature);
        }

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            DependencyList dependencies = base.ComputeNonRelocationBasedDependencies(factory);
            dependencies.Add(_typeNode, "Wasm R2R to interpreter thunk requires type node");
            return dependencies;
        }

        protected override void EmitCode(NodeFactory factory, ref Wasm.WasmEmitter instructionEncoder, bool relocsOnly)
        {
            Debug.Assert(!instructionEncoder.Is64Bit);

            ISymbolNode helperTypeIndex = factory.WasmTypeNode(s_helperTypeParams);

            MethodSignature methodSignature = WasmLowering.RaiseSignature(_wasmSignature, _context);
            (ArgIterator argit, TransitionBlock transitionBlock) = GCRefMapBuilder.BuildArgIterator(methodSignature, _context);

            bool hasRetBuffArg = _wasmSignature.SignatureString[0] == 'S';
            bool hasThis = !methodSignature.IsStatic;

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

            int sizeOfArgumentArray = argit.SizeOfFrameArgumentArray();
            int sizeOfTransitionBlock = transitionBlock.SizeOfTransitionBlock;

            // The arguments area must be 16-byte aligned. The TransitionBlock (8 bytes on Wasm32)
            // sits before the arguments, so it is 8-byte aligned but not 16-byte aligned.
            // Layout from base: [TransitionBlock (8)] [args...]
            int argumentsOffset = AlignmentHelper.AlignUp(sizeOfTransitionBlock, 16);
            int transitionBlockOffset = argumentsOffset - sizeOfTransitionBlock;
            for (int i = 0; i < offsets.Length; i++)
            {
                offsets[i] += transitionBlockOffset;
            }
            int sizeOfStoredLocals = argumentsOffset + AlignmentHelper.AlignUp(sizeOfArgumentArray, 16);

            bool hasWasmReturn = _typeNode.Type.Returns.Types.Length > 0;
            WasmValueType returnWasmType = hasWasmReturn ? _typeNode.Type.Returns.Types[0] : default;

            // Allocate a local return buffer. V128 needs 16 bytes and 16-byte alignment;
            // other types need at most 8 bytes. localRetBufOffset is already 16-byte aligned
            // (argumentsOffset is 16-aligned, and args size is rounded up to 16).
            int retBufSize = (hasWasmReturn && returnWasmType == WasmValueType.V128) ? 16 : 8;
            int localRetBufOffset = sizeOfStoredLocals;
            int totalAlloc = AlignmentHelper.AlignUp(sizeOfStoredLocals + retBufSize, 16);

            List<WasmExpr> expressions = new List<WasmExpr>();

            // Allocate stack space: local.get 0; i32.const totalAlloc; i32.sub; local.set 0
            expressions.Add(Local.Get(0));
            expressions.Add(I32.Const(totalAlloc));
            expressions.Add(I32.Sub);
            expressions.Add(Local.Set(0));

            // Initialize TransitionBlock:
            //   First 4 bytes (m_ReturnAddress) = 0
            expressions.Add(Local.Get(0));
            expressions.Add(I32.Const(0));
            expressions.Add(I32.Store((ulong)transitionBlockOffset));

            //   Second 4 bytes (m_StackPointer) = original SP (local 0 + totalAlloc)
            expressions.Add(Local.Get(0));
            expressions.Add(Local.Get(0));
            expressions.Add(I32.Const(totalAlloc));
            expressions.Add(I32.Add);
            expressions.Add(I32.Store((ulong)(transitionBlockOffset + 4)));

            // Store all arguments into the transition block area
            int wasmLocalIndex = 1; // local 0 is $sp

            // Handle 'this' pointer — it occupies a wasm local but is not in methodSignature.Length
            if (hasThis)
            {
                int thisOffset = transitionBlock.ThisOffset + transitionBlockOffset;
                expressions.Add(Local.Get(0));
                expressions.Add(Local.Get(wasmLocalIndex));
                expressions.Add(I32.Store((ulong)thisOffset));
                wasmLocalIndex++;
            }

            // Hidden retbuf pointer occupies a wasm local but is not in methodSignature params
            if (hasRetBuffArg)
            {
                wasmLocalIndex++;
            }

            for (int i = 0; i < methodSignature.Length; i++)
            {
                TypeDesc paramType = methodSignature[i];

                int currentOffset = offsets[i];

                if (WasmLowering.IsEmptyStruct(paramType))
                {
                    expressions.Add(Local.Get(0));
                    expressions.Add(I32.Const(0));
                    expressions.Add(I32.Store((ulong)currentOffset));
                }
                else if (isIndirectStructArg[i])
                {
                    // Indirect struct — copy the exact contents from the incoming pointer
                    int structSize = paramType.GetElementSize().AsInt;

                    // memory.copy: (dst, src, len) -> ()
                    // dst: base + currentOffset
                    expressions.Add(Local.Get(0));
                    expressions.Add(I32.Const(currentOffset));
                    expressions.Add(I32.Add);
                    // src: the byref pointer passed as the wasm local
                    expressions.Add(Local.Get(wasmLocalIndex));
                    // len: struct size
                    expressions.Add(I32.Const(structSize));
                    expressions.Add(Memory.Copy());

                    // Pad remaining bytes to alignment boundary with zeros
                    int alignment = structSize <= 4 ? 4 : 8;
                    int padding = AlignmentHelper.AlignUp(structSize, alignment) - structSize;
                    if (padding > 0)
                    {
                        // memory.fill: (dst, val, len) -> ()
                        expressions.Add(Local.Get(0));
                        expressions.Add(I32.Const(currentOffset + structSize));
                        expressions.Add(I32.Add);
                        expressions.Add(I32.Const(0));
                        expressions.Add(I32.Const(padding));
                        expressions.Add(Memory.Fill());
                    }

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
                            throw new Exception("Unexpected wasm type arg");
                    }
                    wasmLocalIndex++;
                }
            }

            // Zero the local return buffer
            if (retBufSize <= 8)
            {
                expressions.Add(Local.Get(0));
                expressions.Add(I64.Const(0));
                expressions.Add(I64.Store((ulong)localRetBufOffset));
            }
            else
            {
                expressions.Add(Local.Get(0));
                expressions.Add(I32.Const(localRetBufOffset));
                expressions.Add(I32.Add);
                expressions.Add(I32.Const(0));
                expressions.Add(I32.Const(retBufSize));
                expressions.Add(Memory.Fill());
            }

            // Prepare helper call arguments:
            //   arg1: portable entrypoint (last wasm local)
            int portableEntrypointLocalIndex = _typeNode.Type.Params.Types.Length - 1;
            expressions.Add(Local.Get(portableEntrypointLocalIndex));

            //   arg2: pointer to the collected arguments and transition block (base + transitionBlockOffset)
            expressions.Add(Local.Get(0));
            expressions.Add(I32.Const(transitionBlockOffset));
            expressions.Add(I32.Add);

            //   arg3: size of arguments (excluding transition block)
            expressions.Add(I32.Const(sizeOfArgumentArray));

            //   arg4: return buffer pointer
            if (hasRetBuffArg)
            {
                // The retbuf is a wasm parameter — pass it through directly.
                // For managed calls: local 0 = $sp, local 1 = this (if present), then retbuf.
                int retBufLocalIndex = 1 + (hasThis ? 1 : 0);
                expressions.Add(Local.Get(retBufLocalIndex));
            }
            else
            {
                // Pass pointer to local 8-byte return buffer
                expressions.Add(Local.Get(0));
                expressions.Add(I32.Const(localRetBufOffset));
                expressions.Add(I32.Add);
            }

            // Extra local to save/restore the stack pointer global across the helper call
            int savedSpLocalIndex = _typeNode.Type.Params.Types.Length;

            // Save the current stack pointer global into a local, then set it to local 0
            // (16-byte aligned, <= all buffers allocated in this thunk).
            expressions.Add(Global.Get(WasmObjectWriter.StackPointerGlobalIndex));
            expressions.Add(Local.Set(savedSpLocalIndex));
            expressions.Add(Local.Get(0));
            expressions.Add(Global.Set(WasmObjectWriter.StackPointerGlobalIndex));

            // Load the helper function address and call
            expressions.Add(Global.Get(WasmObjectWriter.ImageBaseGlobalIndex));
            expressions.Add(I32.LoadWithRVAOffset(_helperCell));
            expressions.Add(ControlFlow.CallIndirect(helperTypeIndex, 0));

            // Restore the old stack pointer global
            expressions.Add(Local.Get(savedSpLocalIndex));
            expressions.Add(Global.Set(WasmObjectWriter.StackPointerGlobalIndex));

            // If the function has a wasm return value, load it from the local return buffer
            if (hasWasmReturn)
            {
                Debug.Assert(_typeNode.Type.Returns.Types.Length == 1, "Expected exactly one wasm return type");
                expressions.Add(Local.Get(0));
                switch (returnWasmType)
                {
                    case WasmValueType.I32:
                        expressions.Add(I32.Load((ulong)localRetBufOffset));
                        break;
                    case WasmValueType.F32:
                        expressions.Add(F32.Load((ulong)localRetBufOffset));
                        break;
                    case WasmValueType.I64:
                        expressions.Add(I64.Load((ulong)localRetBufOffset));
                        break;
                    case WasmValueType.F64:
                        expressions.Add(F64.Load((ulong)localRetBufOffset));
                        break;
                    case WasmValueType.V128:
                        expressions.Add(V128.Load((ulong)localRetBufOffset));
                        break;
                    default:
                        throw new Exception("Unexpected wasm return type");
                }
            }

            instructionEncoder.FunctionBody = new WasmFunctionBody(_typeNode.Type, new[] { WasmValueType.I32 }, expressions.ToArray());
        }

        protected override void EmitCode(NodeFactory factory, ref X64.X64Emitter instructionEncoder, bool relocsOnly) { throw new NotSupportedException(); }
        protected override void EmitCode(NodeFactory factory, ref X86.X86Emitter instructionEncoder, bool relocsOnly) { throw new NotSupportedException(); }
        protected override void EmitCode(NodeFactory factory, ref ARM.ARMEmitter instructionEncoder, bool relocsOnly) { throw new NotSupportedException(); }
        protected override void EmitCode(NodeFactory factory, ref ARM64.ARM64Emitter instructionEncoder, bool relocsOnly) { throw new NotSupportedException(); }
        protected override void EmitCode(NodeFactory factory, ref LoongArch64.LoongArch64Emitter instructionEncoder, bool relocsOnly) { throw new NotSupportedException(); }
        protected override void EmitCode(NodeFactory factory, ref RiscV64.RiscV64Emitter instructionEncoder, bool relocsOnly) { throw new NotSupportedException(); }
    }
}
