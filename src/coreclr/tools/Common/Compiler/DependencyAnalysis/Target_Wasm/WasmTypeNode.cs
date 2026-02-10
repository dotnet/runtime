// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using ILCompiler.DependencyAnalysis;
using Internal.JitInterface;

namespace ILCompiler.DependencyAnalysis.Wasm
{
    //
    // Represents a WASM type signature, e.g. "(i32, i32) -> (i64)". Used as a relocation target for things like 'call_indirect'.
    // Does not currently support multiple return values; the return type is always the first type in the array and may be Void.
    //
    public class WasmTypeNode : ObjectNode, ISymbolNode
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

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
            => new ObjectData(
                Array.Empty<byte>(),
                Array.Empty<Relocation>(),
                1,
                Array.Empty<ISymbolDefinitionNode>());

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            var wtm = (WasmTypeNode)other;
            ReadOnlySpan<CorInfoWasmType> lhs = _types, rhs = wtm._types;
            // Put shorter signatures earlier in the sort order, on the assumption that they are more likely to be used.
            int result = lhs.Length.CompareTo(rhs.Length);
            if (result == 0)
                result = MemoryExtensions.SequenceCompareTo(lhs, rhs);
            return result;
        }

        public void AppendMangledName(NameMangler nameMangler, Internal.Text.Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append("__wasmtype_"u8);

            foreach (var type in _types)
                sb.Append(type switch {
                    CorInfoWasmType.CORINFO_WASM_TYPE_VOID => 'v',
                    CorInfoWasmType.CORINFO_WASM_TYPE_V128 => 'V',
                    CorInfoWasmType.CORINFO_WASM_TYPE_F64  => 'd',
                    CorInfoWasmType.CORINFO_WASM_TYPE_F32  => 'f',
                    CorInfoWasmType.CORINFO_WASM_TYPE_I64  => 'j',
                    CorInfoWasmType.CORINFO_WASM_TYPE_I32  => 'i',
                    _ => throw new NotImplementedException($"Unknown CorInfoWasmType: {type}"),
                });
        }

        public int Offset => 0;
    }
}
