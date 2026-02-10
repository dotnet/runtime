// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using ILCompiler.DependencyAnalysis;
using ILCompiler.ObjectWriter;
using Internal.JitInterface;
using Internal.Text;

namespace ILCompiler.DependencyAnalysis.Wasm
{
    //
    // Represents a WASM type signature, e.g. "(i32, i32) -> (i64)". Used as a relocation target for things like 'call_indirect'.
    // Does not currently support multiple return values; the return type is always the first type in the array and may be Void.
    //
    public class WasmTypeNode : ObjectNode, ISymbolNode
    {
        readonly WasmFuncType _type;
        public WasmFuncType Type => _type;

        public WasmTypeNode(WasmFuncType type)
        {
            _type = type;
        }

        public override bool IsShareable => true;

        public override int ClassCode => -45678931;

        public override bool StaticDependenciesAreComputed => true;

        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.WasmTypeSection;

        protected override string GetName(NodeFactory factory)
            => $"Wasm Type Signature: {Type.ToString()}";

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
            => new ObjectData(
                Array.Empty<byte>(),
                Array.Empty<Relocation>(),
                1,
                Array.Empty<ISymbolDefinitionNode>());

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            var wtn = (WasmTypeNode)other;
            // Put shorter signatures earlier in the sort order, on the assumption that they are more likely to be used.
            int result = _type.SignatureLength.CompareTo(wtn.Type.SignatureLength);
            if (result == 0)
                return wtn.Type.CompareTo(_type);
            return result;
        }

        public void AppendMangledName(NameMangler nameMangler, Internal.Text.Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append("__wasmtype_"u8);

            _type.AppendMangledName(sb);
        }

        public int Offset => 0;
    }
}
