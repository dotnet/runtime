// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

using ILCompiler.ObjectWriter;

namespace ILCompiler.DependencyAnalysis
{
    //
    // Represents an entry in the Function section, which correlates a Code section entry to its type signature, e.g. "(i32, i32) -> (i64)".
    //
    public class WasmFunctionEntryNode : ObjectNode
    {
        private readonly ObjectNode _methodCodeNode;
        private readonly WasmTypeNode _wasmTypeNode;
        private readonly int? _funcletIndex;

        public WasmFunctionEntryNode(ObjectNode methodCodeNode, WasmTypeNode wasmTypeNode, int? funcletIndex = null)
        {
            Debug.Assert(funcletIndex is null || methodCodeNode is INodeWithFunclets);
            Debug.Assert(methodCodeNode is INodeWithTypeSignature);
            _methodCodeNode = methodCodeNode;
            _wasmTypeNode = wasmTypeNode;
            _funcletIndex = funcletIndex;
        }

        public override bool IsShareable => false;

        public override int ClassCode => unchecked((int)0xbd3183bc);

        public override bool StaticDependenciesAreComputed => true;

        public override ObjectNodeSection GetSection(NodeFactory factory) => WasmObjectNodeSection.FunctionSection;

        public override bool ShouldSkipEmittingObjectNode(NodeFactory factory)
        {
            if (_methodCodeNode.ShouldSkipEmittingObjectNode(factory))
                return true;

            return _methodCodeNode is ISymbolNode symbolNode &&
                factory.ObjectInterner.GetDeduplicatedSymbol(factory, symbolNode) != symbolNode;
        }

        protected override string GetName(NodeFactory factory)
            => $"Wasm Function Entry: {_methodCodeNode} -> {_wasmTypeNode}";

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            Relocation[] relocs = [new Relocation(RelocType.WASM_TYPE_INDEX_LEB, 0, _wasmTypeNode)];
            byte[] data = new byte[Relocation.GetSize(RelocType.WASM_TYPE_INDEX_LEB)];

            return new ObjectData(
                   data: data,
                   relocs: relocs,
                   alignment: 1,
                   definedSymbols: []);
        }

        // We need to have the exact same order as the code emitted for these functions
        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            WasmFunctionEntryNode o = (WasmFunctionEntryNode)other;
            int result = SortableDependencyNode.CompareImpl(
                _methodCodeNode,
                o._methodCodeNode,
                comparer);
            if (result != 0)
                return result;

            return Nullable.Compare(_funcletIndex, o._funcletIndex);
        }
    }
}
