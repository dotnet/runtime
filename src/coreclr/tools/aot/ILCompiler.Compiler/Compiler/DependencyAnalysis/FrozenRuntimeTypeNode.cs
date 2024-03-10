// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.IL;
using Internal.Text;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    public sealed class FrozenRuntimeTypeNode : FrozenObjectNode
    {
        private readonly TypeDesc _type;
        private readonly bool _constructed;

        public FrozenRuntimeTypeNode(TypeDesc type, bool constructed)
        {
            _type = type;
            _constructed = constructed;
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__RuntimeType_").Append(nameMangler.GetMangledTypeName(_type));
        }

        protected override int ContentSize => ObjectType.InstanceByteCount.AsInt;

        public override void EncodeContents(ref ObjectDataBuilder dataBuilder, NodeFactory factory, bool relocsOnly)
        {
            IEETypeNode typeSymbol = _constructed
                ? factory.ConstructedTypeSymbol(_type)
                : factory.NecessaryTypeSymbol(_type);

            dataBuilder.EmitPointerReloc(factory.ConstructedTypeSymbol(ObjectType));
            dataBuilder.EmitPointerReloc(typeSymbol); // RuntimeType::_pUnderlyingEEType
            dataBuilder.EmitZeroPointer(); // RuntimeType::_runtimeTypeInfoHandle
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override int ClassCode => 726422757;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return comparer.Compare(_type, ((FrozenRuntimeTypeNode)other)._type);
        }

        public override int? ArrayLength => null;

        public override bool IsKnownImmutable => false;

        public override DefType ObjectType => _type.Context.SystemModule.GetKnownType("System", "RuntimeType");
    }
}
