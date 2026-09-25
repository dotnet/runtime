// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using ILCompiler.DependencyAnalysis.Wasm;
using ILCompiler.ObjectWriter;

using Internal.Text;

namespace ILCompiler.DependencyAnalysis
{
    //
    // Represents an entry in the Wasm import section that imports an extern function. The node defines the
    // extern function's symbol name, so relocations against the extern function resolve to this import.
    //
    public class WasmFunctionImportNode : ObjectNode, ISymbolDefinitionNode
    {
        private static readonly Utf8String s_moduleName = new Utf8String("env"u8);

        private readonly Utf8String _name;
        private readonly INodeWithTypeSignature _signatureSource;

        /// <param name="name">The symbol name of the imported function.</param>
        /// <param name="signatureSource">The node that provides the signature of the imported function.</param>
        public WasmFunctionImportNode(Utf8String name, INodeWithTypeSignature signatureSource)
        {
            _name = name;
            _signatureSource = signatureSource;
        }

        public override bool IsShareable => true;

        public override int ClassCode => -683661982;

        public override bool StaticDependenciesAreComputed => true;

        public override ObjectNodeSection GetSection(NodeFactory factory) => WasmObjectNodeSection.ImportSection;

        protected override string GetName(NodeFactory factory) => $"Wasm Function Import: {_name}";

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.RequireInitialAlignment(1);
            builder.AddSymbol(this);

            // import ::= module:name name:name desc:importdesc
            // where importdesc for a function is 0x00 typeidx
            // and name is a length-prefixed UTF-8 string
            EmitName(ref builder, s_moduleName);
            EmitName(ref builder, _name);
            builder.EmitByte((byte)WasmExternalKind.Function);
            builder.EmitReloc(factory.WasmTypeNode(_signatureSource), RelocType.WASM_TYPE_INDEX_LEB);

            return builder.ToObjectData();
        }

        private static void EmitName(ref ObjectDataBuilder builder, Utf8String name)
        {
            // name is a length-prefixed UTF-8 string
            byte[] encodedName = new byte[DwarfHelper.SizeOfULEB128((ulong)name.Length) + name.Length];
            int lengthSize = DwarfHelper.WriteULEB128(encodedName, (ulong)name.Length);
            name.AsSpan().CopyTo(encodedName.AsSpan(lengthSize));
            builder.EmitBytes(encodedName);
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return _name.CompareTo(((WasmFunctionImportNode)other)._name);
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(_name);
        }

        public int Offset => 0;
    }
}
