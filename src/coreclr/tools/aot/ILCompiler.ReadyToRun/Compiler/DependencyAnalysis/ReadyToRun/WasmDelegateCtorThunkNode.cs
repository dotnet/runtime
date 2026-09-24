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
    public sealed class WasmDelegateCtorThunkNode : StringDiscoverableAssemblyStubNode, INodeWithTypeSignature, ISortableSymbolNode
    {
        private const int TargetMethodOffset = 4;
        private const int ShuffleThunkOffset = 8;
        private const int ConstructorOffset = 12;

        private readonly TypeSystemContext _context;
        private readonly bool _hasShuffleThunk;
        private readonly WasmTypeNode _constructorType;

        public static WasmSignature HelperSignature { get; } = CreateSignature(explicitArgumentCount: 1);

        public WasmDelegateCtorThunkNode(NodeFactory factory, bool hasShuffleThunk)
        {
            _context = factory.TypeSystemContext;
            _hasShuffleThunk = hasShuffleThunk;
            _constructorType = factory.WasmTypeNode(CreateSignature(explicitArgumentCount: hasShuffleThunk ? 3 : 2));
        }

        public override string LookupString => _hasShuffleThunk ? "DC1" : "DC0";

        MethodSignature INodeWithTypeSignature.Signature => WasmLowering.RaiseSignature(HelperSignature, _context);
        bool INodeWithTypeSignature.IsUnmanagedCallersOnly => false;
        bool INodeWithTypeSignature.IsAsyncCall => false;
        bool INodeWithTypeSignature.HasGenericContextArg => false;

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(_hasShuffleThunk ? "WasmDelegateCtorThunk(DC1)"u8 : "WasmDelegateCtorThunk(DC0)"u8);
        }

        protected override string GetName(NodeFactory factory)
        {
            Utf8StringBuilder sb = new Utf8StringBuilder();
            AppendMangledName(factory.NameMangler, sb);
            return sb.ToString();
        }

        public override int ClassCode => 948271453;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer) =>
            _hasShuffleThunk.CompareTo(((WasmDelegateCtorThunkNode)other)._hasShuffleThunk);

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            DependencyList dependencies = base.ComputeNonRelocationBasedDependencies(factory);
            dependencies.Add(_constructorType, "Wasm delegate constructor thunk requires constructor type node");
            return dependencies;
        }

        protected override void EmitCode(NodeFactory factory, ref WasmEmitter instructionEncoder, bool relocsOnly)
        {
            Debug.Assert(!instructionEncoder.Is64Bit);

            const int ShadowStackPointerIndex = 0;
            const int ThisIndex = 1;
            const int TargetObjectIndex = 2;
            const int PortableEntryPointIndex = 3;

            List<WasmExpr> expressions = new List<WasmExpr>(16)
            {
                Local.Get(ShadowStackPointerIndex),
                Local.Get(ThisIndex),
                Local.Get(TargetObjectIndex),
                Local.Get(PortableEntryPointIndex),
                I32.Load(TargetMethodOffset),
            };

            if (_hasShuffleThunk)
            {
                expressions.Add(Local.Get(PortableEntryPointIndex));
                expressions.Add(I32.Load(ShuffleThunkOffset));
            }

            expressions.Add(Local.Get(PortableEntryPointIndex));
            expressions.Add(I32.Load(ConstructorOffset));
            expressions.Add(Local.Get(PortableEntryPointIndex));
            expressions.Add(I32.Load(ConstructorOffset));
            expressions.Add(I32.Load(0));
            expressions.Add(ControlFlow.CallIndirect(_constructorType, 0));

            instructionEncoder.FunctionBody = new WasmFunctionBody(
                HelperSignature.FuncType,
                expressions.ToArray());
        }

        private static WasmSignature CreateSignature(int explicitArgumentCount)
        {
            WasmValueType[] parameters = new WasmValueType[explicitArgumentCount + 3];
            Array.Fill(parameters, WasmValueType.I32);

            string signatureString = explicitArgumentCount switch
            {
                1 => "vTip",
                2 => "vTiip",
                3 => "vTiiip",
                _ => throw new UnreachableException(),
            };

            return new WasmSignature(
                new WasmFuncType(
                    new WasmResultType(parameters),
                    new WasmResultType(Array.Empty<WasmValueType>())),
                signatureString);
        }

        protected override void EmitCode(NodeFactory factory, ref X64.X64Emitter instructionEncoder, bool relocsOnly) => throw new NotSupportedException();
        protected override void EmitCode(NodeFactory factory, ref X86.X86Emitter instructionEncoder, bool relocsOnly) => throw new NotSupportedException();
        protected override void EmitCode(NodeFactory factory, ref ARM.ARMEmitter instructionEncoder, bool relocsOnly) => throw new NotSupportedException();
        protected override void EmitCode(NodeFactory factory, ref ARM64.ARM64Emitter instructionEncoder, bool relocsOnly) => throw new NotSupportedException();
        protected override void EmitCode(NodeFactory factory, ref LoongArch64.LoongArch64Emitter instructionEncoder, bool relocsOnly) => throw new NotSupportedException();
        protected override void EmitCode(NodeFactory factory, ref RiscV64.RiscV64Emitter instructionEncoder, bool relocsOnly) => throw new NotSupportedException();
    }
}
