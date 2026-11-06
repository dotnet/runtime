// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using ILCompiler.DependencyAnalysisFramework;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun;

internal sealed class DeferredWasmCallNode(DelayLoadMethodImport fallback, WasmTypeNode signature = null) : SortableDependencyNode, IMethodCodeNodeWithTypeSignature
{
    internal DelayLoadMethodImport Fallback => fallback;
    internal WasmTypeNode Signature => signature;
    internal MethodWithGCInfo Target => fallback.MethodCodeNode;
    public MethodDesc Method => Target.Method;
    public int Offset => 0;
    public bool RepresentsIndirectionCell => false;
    public override int ClassCode => 1932831984;
    public override bool InterestingForDynamicDependencyAnalysis => false;
    public override bool HasDynamicDependencies => false;
    public override bool HasConditionalStaticDependencies => false;
    public override bool StaticDependenciesAreComputed => true;

    public override void AddStaticDependencies(DependencySink sink, NodeFactory factory)
    {
        sink.Add(Target, "Deferred Wasm call discovery");
        fallback.Signature.AddStaticDependencies(sink, factory);
    }

    protected override string GetName(NodeFactory factory) => $"Deferred Wasm call: {Method}";
    public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder builder) => fallback.AppendMangledName(nameMangler, builder);
    public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
    {
        DeferredWasmCallNode otherCall = (DeferredWasmCallNode)other;
        int result = comparer.Compare(fallback, otherCall.Fallback);
        return result != 0 ? result : comparer.Compare(signature, otherCall.Signature);
    }
    public override void AddConditionalDependencies(DependencySink sink, NodeFactory factory) { }
    public override void SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> nodes, int firstNode, DependencySink sink, NodeFactory factory) { }
}
