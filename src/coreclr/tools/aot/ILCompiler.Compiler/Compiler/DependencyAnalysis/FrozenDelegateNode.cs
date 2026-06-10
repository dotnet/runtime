// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using Internal.Text;
using Internal.TypeSystem;

using CombinedDependencyList = System.Collections.Generic.List<ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.CombinedDependencyListEntry>;

namespace ILCompiler.DependencyAnalysis
{
    public sealed class FrozenDelegateNode : FrozenObjectNode
    {
        private readonly MethodDesc _method;
        private readonly TypePreinit.ISerializableReference _serializableObject;

        public override int ClassCode => 378301474;

        public override DefType ObjectType => (DefType)_serializableObject.Type;

        protected override int ContentSize => ObjectType.InstanceByteCount.AsInt;

        public override bool HasConditionalStaticDependencies => _serializableObject.HasConditionalDependencies;

        public override bool IsKnownImmutable => _serializableObject.IsKnownImmutable;

        public override int? ArrayLength => null;

        public FrozenDelegateNode(MethodDesc method, TypePreinit.ISerializableReference serializableObject)
        {
            Debug.Assert(!serializableObject.Type.IsCanonicalSubtype(CanonicalFormKind.Any));
            Debug.Assert(serializableObject.Type is DefType);
            Debug.Assert(!method.IsCanonicalMethod(CanonicalFormKind.Any));

            _method = method;
            _serializableObject = serializableObject;
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__Delegate_"u8)
                .Append(nameMangler.GetMangledTypeName(_serializableObject.Type)).Append('_')
                .Append(nameMangler.GetMangledMethodName(_method));
        }

        public override void EncodeContents(ref ObjectDataBuilder dataBuilder, NodeFactory factory, bool relocsOnly)
        {
            // byte contents
            _serializableObject.WriteContent(ref dataBuilder, this, factory);
        }

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory)
        {
            CombinedDependencyList result = null;
            _serializableObject.GetConditionalDependencies(ref result, factory);
            return result;
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            var otherFrozenDelegateNode = (FrozenDelegateNode)other;
            int result = comparer.Compare(otherFrozenDelegateNode.ObjectType, ObjectType);
            if (result != 0)
                return result;

            return comparer.Compare(otherFrozenDelegateNode._method, _method);
        }

        public override string ToString() => $"Frozen Delegate {_serializableObject.Type.GetDisplayNameWithoutNamespace()} for {_method.GetDisplayName()}";
    }
}
