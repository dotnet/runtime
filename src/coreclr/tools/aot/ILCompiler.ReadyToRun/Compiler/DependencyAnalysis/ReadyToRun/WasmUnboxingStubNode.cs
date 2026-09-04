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
            _lookupString = GetLookupString(kind, signature.FuncType, hasReturnBuffer);
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
            return result != 0 ? result : _hasReturnBuffer.CompareTo(otherNode._hasReturnBuffer);
        }

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            DependencyList dependencies = base.ComputeNonRelocationBasedDependencies(factory);
            dependencies.Add(_targetType, "Wasm unboxing stub requires target type node");
            dependencies.Add(factory.WasmTypeNode(_signature), "Wasm unboxing stub requires entry type node");
            return dependencies;
        }

        protected override void EmitCode(NodeFactory factory, ref WasmEmitter instructionEncoder, bool relocsOnly)
        {
            Debug.Assert(!instructionEncoder.Is64Bit);

            ReadOnlySpan<WasmValueType> parameters = _signature.FuncType.Params.Types;
            int portableEntryPointIndex = parameters.Length - 1;
            int targetEntryPointLocalIndex = parameters.Length;

            List<WasmExpr> expressions = new List<WasmExpr>(parameters.Length + 16);

            // The target PEP is stored immediately before this stub's PEP.
            expressions.Add(Local.Get(portableEntryPointIndex));
            expressions.Add(I32.Const(factory.Target.PointerSize));
            expressions.Add(I32.Sub);
            expressions.Add(I32.Load(0));
            expressions.Add(Local.Set(targetEntryPointLocalIndex));

            AppendTargetCall(
                expressions,
                portableEntryPointIndex,
                targetEntryPointLocalIndex,
                genericContext: _kind != UnboxingStubKind.Normal,
                hasReturnBuffer: _hasReturnBuffer,
                pointerSize: factory.Target.PointerSize);

            instructionEncoder.FunctionBody = new WasmFunctionBody(
                _signature.FuncType,
                new[] { WasmValueType.I32 },
                expressions.ToArray());
        }

        private void AppendTargetCall(
            List<WasmExpr> expressions,
            int portableEntryPointIndex,
            int targetEntryPointLocalIndex,
            bool genericContext,
            bool hasReturnBuffer,
            int pointerSize)
        {
            expressions.Add(Local.Get(0));

            // Skip the MethodTable pointer at the start of the boxed object.
            expressions.Add(Local.Get(1));
            expressions.Add(I32.Const(pointerSize));
            expressions.Add(I32.Add);

            int firstExplicitArgumentIndex = 2;
            if (hasReturnBuffer)
            {
                expressions.Add(Local.Get(firstExplicitArgumentIndex));
                firstExplicitArgumentIndex++;
            }

            if (genericContext && _kind == UnboxingStubKind.MethodTable)
            {
                expressions.Add(Local.Get(1));
                expressions.Add(I32.Load(0));
            }
            else if (genericContext && _kind == UnboxingStubKind.MethodDesc)
            {
                // The target MethodDesc is stored two pointer-sized fields before this stub's PEP.
                expressions.Add(Local.Get(portableEntryPointIndex));
                expressions.Add(I32.Const(2 * pointerSize));
                expressions.Add(I32.Sub);
                expressions.Add(I32.Load(0));
            }

            for (int argumentIndex = firstExplicitArgumentIndex; argumentIndex < portableEntryPointIndex; argumentIndex++)
            {
                expressions.Add(Local.Get(argumentIndex));
            }

            expressions.Add(Local.Get(targetEntryPointLocalIndex));
            expressions.Add(Local.Get(targetEntryPointLocalIndex));
            expressions.Add(I32.Load(0));
            expressions.Add(ControlFlow.CallIndirect(_targetType, 0));
        }

        private static string GetLookupString(UnboxingStubKind kind, WasmFuncType funcType, bool hasReturnBuffer)
        {
            Utf8StringBuilder sb = new Utf8StringBuilder();
            sb.Append(kind switch
            {
                UnboxingStubKind.Normal => "U",
                UnboxingStubKind.MethodTable => "UG",
                UnboxingStubKind.MethodDesc => "UM",
                _ => throw new UnreachableException(),
            });

            if (funcType.Returns.Types.Length == 0)
            {
                sb.Append('v');
            }
            else
            {
                foreach (WasmValueType type in funcType.Returns.Types)
                {
                    AppendTypeCode(sb, type);
                }
            }

            if (hasReturnBuffer)
            {
                sb.Append('r');
            }

            foreach (WasmValueType type in funcType.Params.Types)
            {
                AppendTypeCode(sb, type);
            }

            return sb.ToString();
        }

        private static void AppendTypeCode(Utf8StringBuilder sb, WasmValueType type)
        {
            sb.Append(type switch
            {
                WasmValueType.I32 => 'i',
                WasmValueType.I64 => 'l',
                WasmValueType.F32 => 'f',
                WasmValueType.F64 => 'd',
                WasmValueType.V128 => 'V',
                _ => throw new UnreachableException(),
            });
        }

        protected override void EmitCode(NodeFactory factory, ref X64.X64Emitter instructionEncoder, bool relocsOnly) => throw new NotSupportedException();
        protected override void EmitCode(NodeFactory factory, ref X86.X86Emitter instructionEncoder, bool relocsOnly) => throw new NotSupportedException();
        protected override void EmitCode(NodeFactory factory, ref ARM.ARMEmitter instructionEncoder, bool relocsOnly) => throw new NotSupportedException();
        protected override void EmitCode(NodeFactory factory, ref ARM64.ARM64Emitter instructionEncoder, bool relocsOnly) => throw new NotSupportedException();
        protected override void EmitCode(NodeFactory factory, ref LoongArch64.LoongArch64Emitter instructionEncoder, bool relocsOnly) => throw new NotSupportedException();
        protected override void EmitCode(NodeFactory factory, ref RiscV64.RiscV64Emitter instructionEncoder, bool relocsOnly) => throw new NotSupportedException();
    }

    public enum UnboxingStubKind
    {
        Normal,
        MethodTable,
        MethodDesc,
    }
}
