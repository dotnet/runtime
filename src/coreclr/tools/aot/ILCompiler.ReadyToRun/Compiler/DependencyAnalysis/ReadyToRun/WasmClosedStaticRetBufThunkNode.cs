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
    public sealed class WasmClosedStaticRetBufThunkNode : StringDiscoverableAssemblyStubNode, INodeWithTypeSignature, ISortableSymbolNode
    {
        private readonly TypeSystemContext _context;
        private readonly WasmSignature _signature;
        private readonly WasmTypeNode _typeNode;
        private readonly string _lookupString;

        public WasmClosedStaticRetBufThunkNode(NodeFactory factory, WasmSignature signature)
        {
            _context = factory.TypeSystemContext;
            _signature = signature;
            _typeNode = factory.WasmTypeNode(signature);
            _lookupString = GetLookupString("D", signature.FuncType);
        }

        MethodSignature INodeWithTypeSignature.Signature => WasmLowering.RaiseSignature(_signature, _context);
        bool INodeWithTypeSignature.IsUnmanagedCallersOnly => false;
        bool INodeWithTypeSignature.IsAsyncCall => false;
        bool INodeWithTypeSignature.HasGenericContextArg => false;

        public override string LookupString => _lookupString;

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("WasmClosedStaticRetBufThunk("u8);
            sb.Append(_lookupString);
            sb.Append(")"u8);
        }

        protected override string GetName(NodeFactory factory)
        {
            Utf8StringBuilder sb = new Utf8StringBuilder();
            AppendMangledName(factory.NameMangler, sb);
            return sb.ToString();
        }

        // Keep closed-static return-buffer thunks after the existing Wasm transition and virtual thunks.
        public override int ClassCode => 948271452;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer) =>
            _signature.FuncType.CompareTo(((WasmClosedStaticRetBufThunkNode)other)._signature.FuncType);

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            DependencyList dependencies = base.ComputeNonRelocationBasedDependencies(factory);
            dependencies.Add(_typeNode, "Wasm closed static return-buffer thunk requires type node");
            return dependencies;
        }

        protected override void EmitCode(NodeFactory factory, ref WasmEmitter instructionEncoder, bool relocsOnly)
        {
            Debug.Assert(!instructionEncoder.Is64Bit);
            Debug.Assert(_signature.SignatureString[0] == 'S');
            Debug.Assert(!_signature.SignatureString.Contains('a'));

            ReadOnlySpan<WasmValueType> parameters = _signature.FuncType.Params.Types;
            Debug.Assert(parameters.Length >= 4);

            int portableEntryPointIndex = parameters.Length - 1;
            int targetEntryPointLocalIndex = parameters.Length;
            List<WasmExpr> expressions = new List<WasmExpr>(parameters.Length + 8);

            // The target PEP is stored immediately before this stub's PEP.
            expressions.Add(Local.Get(portableEntryPointIndex));
            expressions.Add(I32.Const(factory.Target.PointerSize));
            expressions.Add(I32.Sub);
            expressions.Add(I32.Load(0));
            expressions.Add(Local.Set(targetEntryPointLocalIndex));

            expressions.Add(Local.Get(0)); // shadow stack pointer
            expressions.Add(Local.Get(2)); // return buffer
            expressions.Add(Local.Get(1)); // captured first argument

            for (int argumentIndex = 3; argumentIndex < portableEntryPointIndex; argumentIndex++)
            {
                expressions.Add(Local.Get(argumentIndex));
            }

            expressions.Add(Local.Get(targetEntryPointLocalIndex));
            expressions.Add(Local.Get(targetEntryPointLocalIndex));
            expressions.Add(I32.Load(0));
            expressions.Add(ControlFlow.CallIndirect(_typeNode, 0));

            instructionEncoder.FunctionBody = new WasmFunctionBody(
                _signature.FuncType,
                new[] { WasmValueType.I32 },
                expressions.ToArray());
        }

        protected override void EmitCode(NodeFactory factory, ref X64.X64Emitter instructionEncoder, bool relocsOnly) => throw new NotSupportedException();
        protected override void EmitCode(NodeFactory factory, ref X86.X86Emitter instructionEncoder, bool relocsOnly) => throw new NotSupportedException();
        protected override void EmitCode(NodeFactory factory, ref ARM.ARMEmitter instructionEncoder, bool relocsOnly) => throw new NotSupportedException();
        protected override void EmitCode(NodeFactory factory, ref ARM64.ARM64Emitter instructionEncoder, bool relocsOnly) => throw new NotSupportedException();
        protected override void EmitCode(NodeFactory factory, ref LoongArch64.LoongArch64Emitter instructionEncoder, bool relocsOnly) => throw new NotSupportedException();
        protected override void EmitCode(NodeFactory factory, ref RiscV64.RiscV64Emitter instructionEncoder, bool relocsOnly) => throw new NotSupportedException();
    }
}
