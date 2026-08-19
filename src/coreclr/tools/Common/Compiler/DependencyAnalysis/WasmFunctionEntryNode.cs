// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILCompiler.ObjectWriter;
using System.Diagnostics;

namespace ILCompiler.DependencyAnalysis
{
    //
    // Represents an entry in the Function section, which correlates a Code section entry to its type signature, e.g. "(i32, i32) -> (i64)".
    //
    public class WasmFunctionEntryNode : ObjectNode
    {
        private readonly INodeWithTypeSignature _methodCodeNode;
        private readonly WasmTypeNode _wasmTypeNode;
        private readonly int? _funcletIndex;

        public WasmFunctionEntryNode(INodeWithTypeSignature methodCodeNode, WasmTypeNode wasmTypeNode, int? funcletIndex = null)
        {
            Debug.Assert(funcletIndex is null || methodCodeNode is INodeWithFunclets);
            _methodCodeNode = methodCodeNode;
            _wasmTypeNode = wasmTypeNode;
            _funcletIndex = funcletIndex;
        }

        public override bool IsShareable => false;

        public override int ClassCode => unchecked((int)0xbd3183bc86);

        public override bool StaticDependenciesAreComputed => true;

        public override ObjectNodeSection GetSection(NodeFactory factory) => WasmObjectNodeSection.FunctionSection;

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
            int result = comparer.Compare((ISortableNode)_methodCodeNode, (ISortableNode)o._methodCodeNode);
            if (result != 0)
                return result;

            if (_funcletIndex.HasValue && !o._funcletIndex.HasValue)
                return 1;
            else if (!_funcletIndex.HasValue && o._funcletIndex.HasValue)
                return -1;
            else
                return _funcletIndex.Value.CompareTo(o._funcletIndex.Value);
        }
    }
}
