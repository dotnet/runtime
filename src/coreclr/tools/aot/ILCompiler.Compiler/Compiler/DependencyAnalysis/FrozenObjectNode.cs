// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.Text;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    public abstract class FrozenObjectNode : EmbeddedObjectNode, ISymbolDefinitionNode
    {
        int ISymbolNode.Offset => 0;

        int ISymbolDefinitionNode.Offset
        {
            get
            {
                // The frozen symbol points at the MethodTable portion of the object, skipping over the sync block
                return OffsetFromBeginningOfArray + ObjectType.Context.Target.PointerSize;
            }
        }

        public abstract TypeDesc ObjectType { get; }

        public abstract int? ArrayLength { get; }
        public abstract bool IsKnownImmutable { get; }
        public int Size => ObjectType.Context.Target.PointerSize + ContentSize; // SyncBlock + size of contents
        protected abstract int ContentSize { get; }

        public abstract void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb);

        public sealed override bool StaticDependenciesAreComputed => true;

        public sealed override void EncodeData(ref ObjectDataBuilder dataBuilder, NodeFactory factory, bool relocsOnly)
        {
            dataBuilder.EmitZeroPointer(); // Sync block

            int sizeBefore = dataBuilder.CountBytes;
            EncodeContents(ref dataBuilder, factory, relocsOnly);
            Debug.Assert(dataBuilder.CountBytes == sizeBefore + ContentSize);
        }

        public sealed override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            var builder = new ObjectDataBuilder(factory, relocsOnly: true);
            EncodeData(ref builder, factory, relocsOnly: true);
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

        public abstract void EncodeContents(ref ObjectDataBuilder dataBuilder, NodeFactory factory, bool relocsOnly);
    }
}
