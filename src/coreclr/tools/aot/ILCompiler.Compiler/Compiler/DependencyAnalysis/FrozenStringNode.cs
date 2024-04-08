// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    public sealed class FrozenStringNode : FrozenObjectNode
    {
        private string _data;
        private readonly DefType _stringType;

        public FrozenStringNode(string data, CompilerTypeSystemContext context)
        {
            _data = data;
            _stringType = context.GetWellKnownType(WellKnownType.String);
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__Str_").Append(nameMangler.GetMangledStringName(_data));
        }

        protected override int ContentSize => _stringType.Context.Target.PointerSize + sizeof(int) + (_data.Length + 1) * sizeof(char);

        public override void EncodeContents(ref ObjectDataBuilder dataBuilder, NodeFactory factory, bool relocsOnly)
        {
            dataBuilder.EmitPointerReloc(factory.ConstructedTypeSymbol(ObjectType));

            dataBuilder.EmitInt(_data.Length);

            foreach (char c in _data)
            {
                dataBuilder.EmitShort((short)c);
            }

            // Null-terminate for friendliness with interop
            dataBuilder.EmitShort(0);
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override int ClassCode => -1733946122;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return string.CompareOrdinal(_data, ((FrozenStringNode)other)._data);
        }

        public string Data => _data;

        public override int? ArrayLength => _data.Length;

        public override bool IsKnownImmutable => true;

        public override TypeDesc ObjectType => _stringType;

        public override string ToString() => $"\"{_data}\"";
    }
}
