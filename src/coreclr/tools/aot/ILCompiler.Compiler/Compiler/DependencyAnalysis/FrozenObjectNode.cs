// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a frozen object that is statically preallocated within the data section
    /// of the executable instead of on the GC heap.
    /// </summary>
    public class FrozenObjectNode : EmbeddedObjectNode, ISymbolDefinitionNode
    {
        private readonly FieldDesc _field;
        private readonly TypePreinit.ISerializableReference _data;
        
        public FrozenObjectNode(FieldDesc field, TypePreinit.ISerializableReference data)
        {
            _field = field;
            _data = data;
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__FrozenObj_")
                .Append(nameMangler.GetMangledFieldName(_field));
        }

        public override bool StaticDependenciesAreComputed => true;

        int ISymbolNode.Offset => 0;

        int ISymbolDefinitionNode.Offset
        {
            get
            {
                // The frozen object symbol points at the MethodTable portion of the object, skipping over the sync block
                return OffsetFromBeginningOfArray + _field.Context.Target.PointerSize;
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

            return dependencies;
        }

        protected override void OnMarked(NodeFactory factory)
        {
            factory.FrozenSegmentRegion.AddEmbeddedObject(this);
        }

        public override int ClassCode => 1789429316;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return comparer.Compare(((FrozenObjectNode)other)._field, _field);
        }
    }
}
