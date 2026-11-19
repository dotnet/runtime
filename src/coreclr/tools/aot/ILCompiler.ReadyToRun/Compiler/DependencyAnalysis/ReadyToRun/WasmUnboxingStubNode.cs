// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using ILCompiler.DependencyAnalysis.Wasm;
using ILCompiler.DependencyAnalysisFramework;
using ILCompiler.ObjectWriter;
using ILCompiler.ObjectWriter.WasmInstructions;
using Internal.JitInterface;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public sealed class WasmUnboxingStubNode : StringDiscoverableAssemblyStubNode, INodeWithTypeSignature, ISortableSymbolNode
    {
        private readonly TypeSystemContext _context;
        private readonly WasmSignature _signature;
        private readonly WasmTypeNode _targetType;
        private readonly UnboxingStubKind _kind;
        private readonly bool _hasReturnBuffer;
        private readonly string _lookupString;

        public WasmUnboxingStubNode(
            NodeFactory factory,
            WasmSignature signature,
            WasmTypeNode targetType,
            UnboxingStubKind kind,
            bool hasReturnBuffer)
        {
            _context = factory.TypeSystemContext;
            _signature = signature;
            _targetType = targetType;
            _kind = kind;
            _hasReturnBuffer = hasReturnBuffer;
            string prefix = kind switch
            {
                UnboxingStubKind.Normal => "U",
                UnboxingStubKind.MethodTable => "UG",
                UnboxingStubKind.MethodDesc => "UM",
                _ => throw new UnreachableException(),
            };
            _lookupString = GetLookupString(prefix, signature.FuncType, hasReturnBuffer);
        }

        MethodSignature INodeWithTypeSignature.Signature => WasmLowering.RaiseSignature(_signature, _context);
        bool INodeWithTypeSignature.IsUnmanagedCallersOnly => false;
        bool INodeWithTypeSignature.IsAsyncCall => false;
        bool INodeWithTypeSignature.HasGenericContextArg => false;

        public override string LookupString => _lookupString;

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("WasmUnboxingStub("u8);
            sb.Append(LookupString);
            sb.Append(")"u8);
        }

        protected override string GetName(NodeFactory factory)
        {
            Utf8StringBuilder sb = new Utf8StringBuilder();
            AppendMangledName(factory.NameMangler, sb);
            return sb.ToString();
        }

        public override int ClassCode => 1931567394;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            WasmUnboxingStubNode otherNode = (WasmUnboxingStubNode)other;
            int result = _kind.CompareTo(otherNode._kind);
            if (result != 0)
            {
                return result;
            }

            result = _signature.FuncType.CompareTo(otherNode._signature.FuncType);
            if (result != 0)
            {
                return result;
            }

            result = comparer.Compare(_targetType, otherNode._targetType);
            return result != 0 ? result : _hasReturnBuffer.CompareTo(otherNode._hasReturnBuffer);
        }

        protected override void ComputeNonRelocationBasedDependencies(DependencySink sink, NodeFactory factory)
        {
            base.ComputeNonRelocationBasedDependencies(sink, factory);
            sink.Add(_targetType, "Wasm unboxing stub requires target type node");
            sink.Add(factory.WasmTypeNode(_signature), "Wasm unboxing stub requires entry type node");
        }

        protected override void EmitCode(NodeFactory factory, ref WasmEmitter instructionEncoder, bool relocsOnly)
        {
            bool is64Bit = instructionEncoder.Is64Bit;
            WasmValueType pointerType = is64Bit ? WasmValueType.I64 : WasmValueType.I32;

            ReadOnlySpan<WasmValueType> parameters = _signature.FuncType.Params.Types;
            int portableEntryPointIndex = parameters.Length - 1;
            int targetEntryPointLocalIndex = parameters.Length;

            List<WasmExpr> expressions = new List<WasmExpr>(parameters.Length + 16);

            // The target PEP is stored immediately before this stub's PEP.
            expressions.Add(Local.Get(portableEntryPointIndex));
            expressions.Add(PointerConst(is64Bit, factory.Target.PointerSize));
            expressions.Add(is64Bit ? I64.Sub : I32.Sub);
            expressions.Add(LoadPointer(is64Bit));
            expressions.Add(Local.Set(targetEntryPointLocalIndex));

            AppendTargetCall(
                expressions,
                portableEntryPointIndex,
                targetEntryPointLocalIndex,
                genericContext: _kind != UnboxingStubKind.Normal,
                hasReturnBuffer: _hasReturnBuffer,
                pointerSize: factory.Target.PointerSize,
                is64Bit: is64Bit);

            instructionEncoder.FunctionBody = new WasmFunctionBody(
                _signature.FuncType,
                new[] { pointerType },
                expressions.ToArray());
        }

        private void AppendTargetCall(
            List<WasmExpr> expressions,
            int portableEntryPointIndex,
            int targetEntryPointLocalIndex,
            bool genericContext,
            bool hasReturnBuffer,
            int pointerSize,
            bool is64Bit)
        {
            expressions.Add(Local.Get(0));

            // Skip the MethodTable pointer at the start of the boxed object.
            expressions.Add(Local.Get(1));
            expressions.Add(PointerConst(is64Bit, pointerSize));
            expressions.Add(is64Bit ? I64.Add : I32.Add);

            int firstExplicitArgumentIndex = 2;
            if (hasReturnBuffer)
            {
                expressions.Add(Local.Get(firstExplicitArgumentIndex));
                firstExplicitArgumentIndex++;
            }

            if (genericContext && _kind == UnboxingStubKind.MethodTable)
            {
                expressions.Add(Local.Get(1));
                expressions.Add(LoadPointer(is64Bit));
            }
            else if (genericContext && _kind == UnboxingStubKind.MethodDesc)
            {
                // The target MethodDesc is stored two pointer-sized fields before this stub's PEP.
                expressions.Add(Local.Get(portableEntryPointIndex));
                expressions.Add(PointerConst(is64Bit, 2 * pointerSize));
                expressions.Add(is64Bit ? I64.Sub : I32.Sub);
                expressions.Add(LoadPointer(is64Bit));
            }

            for (int argumentIndex = firstExplicitArgumentIndex; argumentIndex < portableEntryPointIndex; argumentIndex++)
            {
                expressions.Add(Local.Get(argumentIndex));
            }

            expressions.Add(Local.Get(targetEntryPointLocalIndex));
            expressions.Add(Local.Get(targetEntryPointLocalIndex));
            expressions.Add(LoadPointer(is64Bit));
            expressions.Add(ControlFlow.CallIndirect(_targetType, 0));
        }

        private static WasmExpr PointerConst(bool is64Bit, int value) => is64Bit ? I64.Const(value) : I32.Const(value);

        private static WasmExpr LoadPointer(bool is64Bit) => is64Bit ? I64.Load(0) : I32.Load(0);

        protected override void EmitCode(NodeFactory factory, ref X64.X64Emitter instructionEncoder, bool relocsOnly) => throw new NotSupportedException();
        protected override void EmitCode(NodeFactory factory, ref X86.X86Emitter instructionEncoder, bool relocsOnly) => throw new NotSupportedException();
        protected override void EmitCode(NodeFactory factory, ref ARM.ARMEmitter instructionEncoder, bool relocsOnly) => throw new NotSupportedException();
        protected override void EmitCode(NodeFactory factory, ref ARM64.ARM64Emitter instructionEncoder, bool relocsOnly) => throw new NotSupportedException();
        protected override void EmitCode(NodeFactory factory, ref LoongArch64.LoongArch64Emitter instructionEncoder, bool relocsOnly) => throw new NotSupportedException();
        protected override void EmitCode(NodeFactory factory, ref RiscV64.RiscV64Emitter instructionEncoder, bool relocsOnly) => throw new NotSupportedException();
    }

    public sealed class WasmUnboxingStubTargetNode : DependencyNodeCore<NodeFactory>
    {
        private readonly MethodDesc _targetMethod;
        private readonly WasmSignature _signature;
        private readonly WasmUnboxingStubNode _stub;

        public WasmUnboxingStubTargetNode(MethodDesc targetMethod, WasmSignature signature, WasmUnboxingStubNode stub)
        {
            _targetMethod = targetMethod;
            _signature = signature;
            _stub = stub;
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;

        public override bool HasDynamicDependencies => false;

        public override bool HasConditionalStaticDependencies => false;

        public override bool StaticDependenciesAreComputed => true;

        public override void AddConditionalDependencies(DependencySink sink, NodeFactory context) { }

        public override void AddStaticDependencies(DependencySink sink, NodeFactory context)
        {
            sink.Add(_stub, "Wasm unboxing stub for target method");
            sink.Add(context.CompiledMethodNode(_targetMethod), "Target method for Wasm unboxing stub");
            sink.Add(context.WasmR2RToInterpreterThunk(WasmLowering.GetSignature(_targetMethod)), "Interpreter fallback for Wasm unboxing target");
            sink.Add(context.WasmInterpreterToR2RThunk(_signature), "Interpreter-to-R2R thunk for Wasm unboxing stub target");
        }

        public override void SearchDynamicDependencies(
            List<DependencyNodeCore<NodeFactory>> markedNodes,
            int firstNode,
            DependencySink sink,
            NodeFactory context) { }

        protected override string GetName(NodeFactory factory) => $"Wasm unboxing stub target for {_targetMethod}";
    }

    public enum UnboxingStubKind
    {
        Normal,
        MethodTable,
        MethodDesc,
    }
}
