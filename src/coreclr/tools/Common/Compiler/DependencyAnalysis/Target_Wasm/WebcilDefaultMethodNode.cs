// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using ILCompiler.DependencyAnalysis.ARM;
using ILCompiler.DependencyAnalysis.ARM64;
using ILCompiler.DependencyAnalysis.LoongArch64;
using ILCompiler.DependencyAnalysis.RiscV64;
using ILCompiler.DependencyAnalysis.Wasm;
using ILCompiler.DependencyAnalysis.X64;
using ILCompiler.DependencyAnalysis.X86;
using ILCompiler.ObjectWriter;
using ILCompiler.ObjectWriter.WasmInstructions;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    public enum WebcilDefaultMethodKind
    {
        GetWebcilSize,
        FillWebcilTable,
        GetWebcilPayload
    }

    /// <summary>
    /// Represents a default method that is emitted into the Webcil code section. These methods are part of the webcil spec.
    /// </summary>
    internal sealed class WebcilDefaultMethodNode : AssemblyStubNode, INodeWithTypeSignature
    {
        private readonly WebcilDefaultMethodKind _kind;
        private readonly MethodSignature _signature;

        public override ObjectNodeSection GetSection(NodeFactory factory)
        {
            return ObjectNodeSection.WasmCodeSection;
        }

        public override bool IsShareable => true;

        public WebcilDefaultMethodNode(WebcilDefaultMethodKind kind, NodeFactory factory)
        {
            _kind = kind;
            TypeDesc @void = factory.TypeSystemContext.GetWellKnownType(Internal.TypeSystem.WellKnownType.Void);
            TypeDesc @int = factory.TypeSystemContext.GetWellKnownType(Internal.TypeSystem.WellKnownType.Int32);
            _signature = _kind switch
            {
                WebcilDefaultMethodKind.GetWebcilSize=> new MethodSignature(MethodSignatureFlags.Static, 0, @void, new TypeDesc[] { @int }),
                WebcilDefaultMethodKind.FillWebcilTable => new MethodSignature(MethodSignatureFlags.Static, 0, @void, Array.Empty<TypeDesc>()),
                WebcilDefaultMethodKind.GetWebcilPayload => new MethodSignature(MethodSignatureFlags.Static, 0, @void, new TypeDesc[] { @int, @int }),
                _ => throw new NotImplementedException(),
            };
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(_kind switch
            {
                WebcilDefaultMethodKind.GetWebcilSize => "getWebcilSize",
                WebcilDefaultMethodKind.FillWebcilTable => "fillWebcilTable",
                WebcilDefaultMethodKind.GetWebcilPayload => "getWebcilPayload",
                _ => throw new NotImplementedException(),
            });
        }

        public override int ClassCode => (int)ObjectNodeOrder.WebcilDefaultMethodNode;

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            var otherNode = (WebcilDefaultMethodNode)other;
            return _kind.CompareTo(otherNode._kind);
        }

        protected override void EmitCode(NodeFactory factory, ref WasmEmitter instructionEncoder, bool relocsOnly)
        {
            var body = _kind switch
            {
                WebcilDefaultMethodKind.GetWebcilSize => GetWebcilSize,
                WebcilDefaultMethodKind.FillWebcilTable => FillWebcilTable(factory), // TODO: Pass in table size
                WebcilDefaultMethodKind.GetWebcilPayload => GetWebcilPayload,
                _ => throw new NotImplementedException(),
            };
            instructionEncoder.FunctionBody = body;
        }

        static WasmFunctionBody GetWebcilSize = new WasmFunctionBody(
            new WasmFuncType(new([WasmValueType.I32]), new([])), // (func (destPtr i32) (result))
                [
                    Local.Get(0), // (local.get $destPtr)
                    I32.Const(0),
                    I32.Const(4),
                    Memory.Init(0)
                ]
        );

        WasmFunctionBody FillWebcilTable(NodeFactory factory) => new WasmFunctionBody(
            new WasmFuncType(new([]), new([])), // (func)
            GetFillWebcilTableInstructions(I32.ConstFunctionCount(factory.WasmFunctionCount))
        );

        internal static WasmExpr[] GetFillWebcilTableInstructions(WasmExpr functionCount) =>
        [
            Global.Get(WebCilObjectWriter.TableBaseGlobalIndex),
            I32.Const(0),
            functionCount,
            Table.Init(0, 0)
        ];

        private static WasmFunctionBody GetWebcilPayload => new WasmFunctionBody(
            new WasmFuncType(new([WasmValueType.I32, WasmValueType.I32]), new([])), // (func ($d i32) ($n i32))
                [
                    Local.Get(0), // (local.get $d)
                    I32.Const(0),
                    Local.Get(1), // (local.get $n)
                    Memory.Init(1),
                    Local.Get(1),
                    I32.Const(32),
                    I32.Ge_s,
                    Block.If(WasmBlockType.Empty),
                    Local.Get(0), // (local.get $d)
                    Global.Get(WebCilObjectWriter.TableBaseGlobalIndex), // (global.get $tableBase)
                    I32.Store((ulong)WebcilEncoder.TableBaseOffset), // i32.store offset=TableBaseOffset
                    Block.End
                ]
        );

        public MethodSignature Signature => _signature;

        // This is called from wasm at load time, not from managed code, and should not have their signatures modified
        // in lowering to wasm, so mark with UnmanagedCallersOnly
        public bool IsUnmanagedCallersOnly => true;

        public bool IsAsyncCall => false;

        public bool HasGenericContextArg => false;

        protected override string GetName(NodeFactory context) => $"WebcilDefaultMethod: {_kind}";

        // This node only exists on WASM.
        protected override void EmitCode(NodeFactory factory, ref X64Emitter instructionEncoder, bool relocsOnly) => throw new PlatformNotSupportedException();
        protected override void EmitCode(NodeFactory factory, ref X86Emitter instructionEncoder, bool relocsOnly) => throw new PlatformNotSupportedException();
        protected override void EmitCode(NodeFactory factory, ref ARMEmitter instructionEncoder, bool relocsOnly) => throw new PlatformNotSupportedException();
        protected override void EmitCode(NodeFactory factory, ref ARM64Emitter instructionEncoder, bool relocsOnly) => throw new PlatformNotSupportedException();
        protected override void EmitCode(NodeFactory factory, ref LoongArch64Emitter instructionEncoder, bool relocsOnly) => throw new PlatformNotSupportedException();
        protected override void EmitCode(NodeFactory factory, ref RiscV64Emitter instructionEncoder, bool relocsOnly) => throw new PlatformNotSupportedException();
    }

    internal class WasmFunctionCountNode : ObjectNode, ISymbolDefinitionNode
    {
        public override bool IsShareable => true;

        public override int ClassCode => unchecked((int)0x92b11dff);

        public int Offset => 0;

        public override bool StaticDependenciesAreComputed => true;

        public void AppendMangledName(ILCompiler.NameMangler nameMangler, Utf8StringBuilder sb) => sb.Append("WasmFunctionCountNode");

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            return new ObjectData(
                data: [],
                relocs: [],
                alignment: 1,
                definedSymbols: [this]);
        }

        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.WasmCodeSection;
        protected override string GetName(NodeFactory context) => $"WasmFunctionCountNode";
    }
}
