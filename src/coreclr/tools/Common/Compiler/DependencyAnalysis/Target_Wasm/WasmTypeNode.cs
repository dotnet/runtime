// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using ILCompiler.DependencyAnalysis;
using ILCompiler.ObjectWriter;

namespace ILCompiler.DependencyAnalysis.Wasm
{
    //
    // Represents a WASM type signature, e.g. "(i32, i32) -> (i64)". Used as a relocation target for things like 'call_indirect'.
    // Does not currently support multiple return values; the return type is always the first type in the array and may be Void.
    //
    public class WasmTypeNode : ObjectNode, ISymbolNode, ISymbolDefinitionNode
    {
        private readonly WasmFuncType _type;
        public WasmFuncType Type => _type;

        public WasmTypeNode(WasmFuncType type)
        {
            _type = type;
            _data = new byte[_type.EncodeSize()];
            _type.Encode(_data);
        }

        public override bool IsShareable => true;

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;

        public override int ClassCode => -856789310;

        public override bool StaticDependenciesAreComputed => true;

        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.WasmTypeSection;

        protected override string GetName(NodeFactory factory)
            => $"Wasm Type Signature: {Type.ToString()}";

        private byte[] _data;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.AddSymbol(this);
            builder.EmitBytes(_data);

            return builder.ToObjectData();
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            var wtn = (WasmTypeNode)other;
            // Put shorter signatures earlier in the sort order, on the assumption that they are more likely to be used.
            int result = _type.SignatureLength.CompareTo(wtn.Type.SignatureLength);
            if (result == 0)
                return _type.CompareTo(wtn.Type);
            return result;
        }

        public void AppendMangledName(NameMangler nameMangler, Internal.Text.Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            _type.AppendMangledName(sb);
        }

        public int Offset => 0;
    }
}
