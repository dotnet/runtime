// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using ILCompiler.DependencyAnalysis;
using Internal.JitInterface;

namespace ILCompiler.DependencyAnalysis.Wasm
{
    public class WasmTypeNode : ObjectNode
    {
        private readonly CorInfoWasmType[] _types;

        public WasmTypeNode(CorInfoWasmType[] types)
        {
            _types = types;
        }

        public override bool IsShareable => true;

        public override int ClassCode => -45678931;

        public override bool StaticDependenciesAreComputed => true;

        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.WasmTypeSection;

        protected override string GetName(NodeFactory factory)
            => $"Wasm Type ({string.Join(",", _types.Skip(1))}) -> {_types.FirstOrDefault()}";

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false) => null;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
            => 0 /* FIXME-WASM */;
    }
}
