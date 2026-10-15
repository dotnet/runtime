// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using ILCompiler.DependencyAnalysis.Wasm;
using ILCompiler.ObjectWriter;
using ILCompiler.ObjectWriter.WasmInstructions;
using Internal.JitInterface;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun;

// Module-local leaf assembly, with an explicit shadow-stack argument and no managed frame.
public sealed class WasmPrologueHelperNode : ObjectNode, ISymbolDefinitionNode, IPCodeSymbolNode, INodeWithTypeSignature
{
    private readonly CorInfoHelpFunc _helper;
    private readonly WasmTypeNode _typeNode;
    public MethodSignature Signature { get; }
    public bool IsUnmanagedCallersOnly => true;
    public bool IsAsyncCall => false;
    public bool HasGenericContextArg => false;
    public int Offset => 0;
    public override int ClassCode => 948271500;
    public override bool IsShareable => false;
    public override bool StaticDependenciesAreComputed => true;

    private bool HomesParameter => _helper == CorInfoHelpFunc.CORINFO_HELP_WASM_PROLOGUE_HOME12;
    private bool ZeroesFrame => _helper is CorInfoHelpFunc.CORINFO_HELP_WASM_PROLOGUE_ZERO or CorInfoHelpFunc.CORINFO_HELP_WASM_PROLOGUE_RESUME_ZERO;
    private bool InitializesResume => _helper is CorInfoHelpFunc.CORINFO_HELP_WASM_PROLOGUE_RESUME or CorInfoHelpFunc.CORINFO_HELP_WASM_PROLOGUE_RESUME_ZERO;

    public WasmPrologueHelperNode(NodeFactory factory, CorInfoHelpFunc helper)
    {
        _helper = helper;
        TypeDesc intType = factory.TypeSystemContext.GetWellKnownType(WellKnownType.Int32);
        TypeDesc[] parameters = new TypeDesc[ZeroesFrame ? 5 : HomesParameter ? 4 : 3];
        System.Array.Fill(parameters, intType);
        Signature = new MethodSignature(MethodSignatureFlags.Static, 0, intType, parameters);
        _typeNode = factory.WasmTypeNode(this);
    }

    public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.TextSection;
    public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder builder) => builder.Append(_helper.ToString());
    protected override string GetName(NodeFactory factory) => _helper.ToString();
    public override int CompareToImpl(ISortableNode other, CompilerComparer comparer) => _helper.CompareTo(((WasmPrologueHelperNode)other)._helper);

    protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory) =>
        new DependencyList { new DependencyListEntry(_typeNode, "Prologue stub signature") };

    public override ObjectData GetData(NodeFactory factory, bool relocsOnly)
    {
        List<WasmExpr> instructions =
        [
            Local.Get(0), Local.Get(1), I32.Sub, Local.Set(0),
            Local.Get(0), Global.Get(WebCilObjectWriter.TableBaseGlobalIndex), Local.Get(2), I32.Add, I32.Store(0),
        ];
        if (InitializesResume)
        {
            instructions.AddRange([Local.Get(0), I32.Const(0), I32.Store(8)]);
        }
        if (ZeroesFrame)
        {
            instructions.AddRange([Local.Get(0), Local.Get(3), I32.Add, I32.Const(0), Local.Get(4), Memory.Fill()]);
        }
        if (HomesParameter)
        {
            instructions.AddRange([Local.Get(0), Local.Get(3), I32.Store(12)]);
        }
        instructions.Add(Local.Get(0));
        WasmEmitter emitter = new WasmEmitter(factory, relocsOnly)
        {
            FunctionBody = new WasmFunctionBody(_typeNode.Type, [], instructions.ToArray())
        };
        return emitter.Encode(this);
    }
}
