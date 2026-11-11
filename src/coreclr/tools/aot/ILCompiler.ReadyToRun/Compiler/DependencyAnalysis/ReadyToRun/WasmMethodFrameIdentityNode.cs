// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;
using Internal.Text;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    internal sealed class WasmMethodFrameIdentityNode : SortableDependencyNode, ISortableSymbolNode
    {
        private readonly NodeFactory _factory;
        private readonly MethodWithGCInfo _method;

        public WasmMethodFrameIdentityNode(NodeFactory factory, MethodWithGCInfo method)
        {
            _factory = factory;
            _method = method;
        }

        public int Offset => checked(-(_factory.RuntimeFunctionsTable.GetIndex(_method) * 2 + 1));

        public bool RepresentsIndirectionCell => false;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            _method.AppendMangledName(nameMangler, sb);
        }

        protected override string GetName(NodeFactory factory) => $"Wasm frame identity: {_method.Method}";

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;

        public override void AddStaticDependencies(DependencySink sink, NodeFactory factory)
        {
            sink.Add(_method, "Method for frame identity");
        }

        public override void AddConditionalDependencies(DependencySink sink, NodeFactory factory) { }

        public override void SearchDynamicDependencies(
            List<DependencyNodeCore<NodeFactory>> markedNodes,
            int firstNode,
            DependencySink sink,
            NodeFactory factory) { }

        public override int ClassCode => 1987324652;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return comparer.Compare(_method.Method, ((WasmMethodFrameIdentityNode)other)._method.Method);
        }
    }
}
