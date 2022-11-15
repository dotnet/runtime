// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;

using Internal.Text;

namespace ILCompiler.DependencyAnalysis
{
    public sealed class LazyStringNode : ObjectNode, ISymbolDefinitionNode
    {
        private readonly string _data;

        public LazyStringNode(string data)
        {
            _data = data;
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__LazyStr_").Append(nameMangler.GetMangledStringName(_data));
        }

        public override bool StaticDependenciesAreComputed => true;

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override int ClassCode => -45676679;

        public override bool IsShareable => true;

        public int Offset => 0;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
            => string.CompareOrdinal(_data, ((LazyStringNode)other)._data);
        public override string ToString() => $"\"{_data}\"";
        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.FoldableReadOnlyDataSection;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            if (relocsOnly)
                new ObjectData(null, Array.Empty<Relocation>(), 0, null);

            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.AddSymbol(this);

            // TODO: we actually want WTF-8, not UTF-8
            byte[] bytes = Encoding.UTF8.GetBytes(_data);
            builder.EmitCompressedUInt((uint)bytes.Length);
            builder.EmitBytes(bytes);
            return builder.ToObjectData();
        }
    }
}
