// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILCompiler.DependencyAnalysis.Wasm;
using ILCompiler.DependencyAnalysisFramework;
using ILCompiler.ObjectWriter;
using ILCompiler.ObjectWriter.WasmInstructions;
using Internal.JitInterface;
using Internal.Text;
using Internal.TypeSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// Dispatches a virtual call through the vtable offsets stored in its portable entrypoint.
    /// </summary>
    public sealed class WasmVirtualDispatchThunkNode : StringDiscoverableAssemblyStubNode, INodeWithTypeSignature, ISymbolDefinitionNode, ISortableSymbolNode
    {
        private readonly TypeSystemContext _context;
        private readonly WasmSignature _wasmSignature;
        private readonly WasmTypeNode _typeNode;
        private readonly string _lookupString;

        public WasmVirtualDispatchThunkNode(NodeFactory factory, WasmSignature wasmSignature)
        {
            _context = factory.TypeSystemContext;
            _wasmSignature = wasmSignature;
            _typeNode = factory.WasmTypeNode(wasmSignature);
            _lookupString = GetLookupString(wasmSignature.FuncType);
        }

        public override bool StaticDependenciesAreComputed => true;
        public override bool IsShareable => false;
        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.TextSection;
        public override string LookupString => _lookupString;

        MethodSignature INodeWithTypeSignature.Signature => WasmLowering.RaiseSignature(_wasmSignature, _context);
        bool INodeWithTypeSignature.IsUnmanagedCallersOnly => false;
        bool INodeWithTypeSignature.IsAsyncCall => _wasmSignature.SignatureString.Contains('a');
        bool INodeWithTypeSignature.HasGenericContextArg => false;

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("WasmVirtualDispatchThunk("u8);
            sb.Append(_lookupString);
            sb.Append(")"u8);
        }

        protected override string GetName(NodeFactory factory)
        {
            Utf8StringBuilder sb = new Utf8StringBuilder();
            AppendMangledName(factory.NameMangler, sb);
            return sb.ToString();
        }

        public override int ClassCode => 115732791;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            WasmVirtualDispatchThunkNode otherNode = (WasmVirtualDispatchThunkNode)other;
            return _wasmSignature.FuncType.CompareTo(otherNode._wasmSignature.FuncType);
        }

        private static string GetLookupString(WasmFuncType funcType)
        {
            Utf8StringBuilder sb = new Utf8StringBuilder();
            sb.Append('V');

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
                _ => throw new UnreachableException()
            });
        }

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            DependencyList dependencies = base.ComputeNonRelocationBasedDependencies(factory);
            dependencies.Add(_typeNode, "Wasm virtual dispatch thunk requires type node");
            return dependencies;
        }

        protected override void EmitCode(NodeFactory factory, ref Wasm.WasmEmitter instructionEncoder, bool relocsOnly)
        {
            Debug.Assert(!instructionEncoder.Is64Bit);

            MethodSignature methodSignature = WasmLowering.RaiseSignature(_wasmSignature, _context);
            Debug.Assert(!methodSignature.IsStatic);

            int portableEntrypointLocalIndex = _typeNode.Type.Params.Types.Length - 1;
            int targetPortableEntrypointLocalIndex = _typeNode.Type.Params.Types.Length;
            int targetCodeLocalIndex = targetPortableEntrypointLocalIndex + 1;
            const int ThisLocalIndex = 1;
            const ulong PackedDispatchOffsetsField = 4;
            const ulong InitialEntryField = 8;

            List<WasmExpr> expressions = new List<WasmExpr>();

            // targetPEP = *(*(*(this) + pep->offsetOfIndirection) + pep->offsetAfterIndirection)
            expressions.Add(Local.Get(ThisLocalIndex));
            expressions.Add(I32.Load(0));
            expressions.Add(Local.Get(portableEntrypointLocalIndex));
            expressions.Add(I32.Load16_u(PackedDispatchOffsetsField));
            expressions.Add(I32.Add);
            expressions.Add(I32.Load(0));
            expressions.Add(Local.Get(portableEntrypointLocalIndex));
            expressions.Add(I32.Load16_u(PackedDispatchOffsetsField + sizeof(ushort)));
            expressions.Add(I32.Add);
            expressions.Add(I32.Load(0));
            expressions.Add(Local.Set(targetPortableEntrypointLocalIndex));

            expressions.Add(Local.Get(targetPortableEntrypointLocalIndex));
            expressions.Add(I32.Load(0));
            expressions.Add(Local.Set(targetCodeLocalIndex));

            // Redispatch through the original entrypoint if this receiver's target is not yet callable from R2R.
            expressions.Add(Local.Get(targetCodeLocalIndex));
            expressions.Add(I32.Eqz);
            expressions.Add(Block.If(WasmBlockType.Empty));
            expressions.Add(Local.Get(portableEntrypointLocalIndex));
            expressions.Add(I32.Load(InitialEntryField));
            expressions.Add(Local.Set(targetPortableEntrypointLocalIndex));
            expressions.Add(Local.Get(targetPortableEntrypointLocalIndex));
            expressions.Add(I32.Load(0));
            expressions.Add(Local.Set(targetCodeLocalIndex));
            expressions.Add(Block.End);

            for (int i = 0; i < portableEntrypointLocalIndex; i++)
            {
                expressions.Add(Local.Get(i));
            }
            expressions.Add(Local.Get(targetPortableEntrypointLocalIndex));
            expressions.Add(Local.Get(targetCodeLocalIndex));
            expressions.Add(ControlFlow.CallIndirect(_typeNode, 0));

            instructionEncoder.FunctionBody = new WasmFunctionBody(
                _typeNode.Type,
                new[] { WasmValueType.I32, WasmValueType.I32 },
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
