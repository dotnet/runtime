// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.Text;
using Internal.TypeSystem;

using CombinedDependencyList = System.Collections.Generic.List<ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.CombinedDependencyListEntry>;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a frozen object that is statically preallocated within the data section
    /// of the executable instead of on the GC heap.
    /// </summary>
    public sealed class SerializedFrozenObjectNode : FrozenObjectNode
    {
        private readonly MetadataType _owningType;
        private readonly TypePreinit.ISerializableReference _data;
        private readonly int _allocationSiteId;

        public MetadataType OwningType => _owningType;

        public SerializedFrozenObjectNode(MetadataType owningType, int allocationSiteId, TypePreinit.ISerializableReference data)
        {
            _owningType = owningType;
            _allocationSiteId = allocationSiteId;
            _data = data;
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__FrozenObj_")
                .Append(nameMangler.GetMangledTypeName(_owningType))
                .Append(_allocationSiteId.ToStringInvariant());
        }

        public override TypeDesc ObjectType => _data.Type;

        public override bool IsKnownImmutable => _data.IsKnownImmutable;

        protected override int ContentSize
            => _data.Type.IsArray
            ? _data.Type.Context.Target.PointerSize * 2 + ((ArrayType)_data.Type).ElementType.GetElementSize().AsInt * _data.ArrayLength
            : ((DefType)_data.Type).InstanceByteCount.AsInt + (_data.Type.IsValueType ? _data.Type.Context.Target.PointerSize : 0);

        public override int? ArrayLength => _data.Type.IsArray ? _data.ArrayLength : null;

        public override void EncodeContents(ref ObjectDataBuilder dataBuilder, NodeFactory factory, bool relocsOnly)
        {
            // byte contents
            _data.WriteContent(ref dataBuilder, this, factory);
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override bool HasConditionalStaticDependencies => _data.HasConditionalDependencies;

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory)
        {
            CombinedDependencyList result = null;
            _data.GetConditionalDependencies(ref result, factory);
            return result;
        }

        public override int ClassCode => 1789429316;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            var otherFrozenObjectNode = (SerializedFrozenObjectNode)other;
            int result = comparer.Compare(otherFrozenObjectNode._owningType, _owningType);
            if (result != 0)
                return result;

            return _allocationSiteId.CompareTo(otherFrozenObjectNode._allocationSiteId);
        }

        public override string ToString() => $"Frozen {_data.Type.GetDisplayNameWithoutNamespace()} object";
    }
}
