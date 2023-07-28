// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a frozen object that is statically preallocated within the data section
    /// of the executable instead of on the GC heap.
    /// </summary>
    public sealed class FrozenObjectNode : EmbeddedObjectNode, ISymbolDefinitionNode
    {
        private readonly MetadataType _owningType;
        private readonly TypePreinit.ISerializableReference _data;
        private readonly int _allocationSiteId;

        public FrozenObjectNode(MetadataType owningType, int allocationSiteId, TypePreinit.ISerializableReference data)
        {
            _owningType = owningType;
            _allocationSiteId = allocationSiteId;
            _data = data;
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__FrozenObj_")
                .Append(nameMangler.GetMangledTypeName(_owningType))
                .Append(_allocationSiteId.ToStringInvariant());
        }

        public override bool StaticDependenciesAreComputed => true;

        public TypeDesc ObjectType => _data.Type;

        public bool IsKnownImmutable => _data.IsKnownImmutable;

        public int GetArrayLength()
        {
            Debug.Assert(ObjectType.IsArray);
            return _data.ArrayLength;
        }

        int ISymbolNode.Offset => 0;

        int ISymbolDefinitionNode.Offset
        {
            get
            {
                // The frozen object symbol points at the MethodTable portion of the object, skipping over the sync block
                return OffsetFromBeginningOfArray + _owningType.Context.Target.PointerSize;
            }
        }

        public override void EncodeData(ref ObjectDataBuilder dataBuilder, NodeFactory factory, bool relocsOnly)
        {
            // Sync Block
            dataBuilder.EmitZeroPointer();

            // byte contents
            _data.WriteContent(ref dataBuilder, this, factory);
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            ObjectDataBuilder builder = new ObjectDataBuilder(factory, true);
            EncodeData(ref builder, factory, true);
            Relocation[] relocs = builder.ToObjectData().Relocs;
            DependencyList dependencies = null;

            if (relocs != null)
            {
                dependencies = new DependencyList();
                foreach (Relocation reloc in relocs)
                {
                    dependencies.Add(reloc.Target, "reloc");
                }
            }

            _data.GetNonRelocationDependencies(ref dependencies, factory);

            return dependencies;
        }

        public override int ClassCode => 1789429316;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            var otherFrozenObjectNode = (FrozenObjectNode)other;
            int result = comparer.Compare(otherFrozenObjectNode._owningType, _owningType);
            if (result != 0)
                return result;

            return _allocationSiteId.CompareTo(otherFrozenObjectNode._allocationSiteId);
        }

        public override string ToString() => $"Frozen {_data.Type.GetDisplayNameWithoutNamespace()} object";
    }
}
