// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a node containing information necessary at runtime to locate type's thread static base.
    /// </summary>
    public class TypeThreadStaticIndexNode : DehydratableObjectNode, ISymbolDefinitionNode, ISortableSymbolNode
    {
        private MetadataType _type;
        private ThreadStaticsNode _inlinedThreadStatics;

        public TypeThreadStaticIndexNode(MetadataType type, ThreadStaticsNode inlinedThreadStatics)
        {
            _type = type;
            _inlinedThreadStatics = inlinedThreadStatics;
        }

        public bool IsInlined => _inlinedThreadStatics != null;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.NodeMangler.ThreadStaticsIndex(_type));
        }

        public int Offset => 0;
        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        protected override ObjectNodeSection GetDehydratedSection(NodeFactory factory)
        {
            if (factory.Target.IsWindows)
                return ObjectNodeSection.ReadOnlyDataSection;
            else
                return ObjectNodeSection.DataSection;
        }

        public override bool IsShareable => true;
        public override bool StaticDependenciesAreComputed => true;

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            ISymbolDefinitionNode node = _inlinedThreadStatics ?? factory.TypeThreadStaticsSymbol(_type);

            return new DependencyList
            {
                new DependencyListEntry(node, "Thread static storage")
            };
        }

        protected override ObjectData GetDehydratableData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder objData = new ObjectDataBuilder(factory, relocsOnly);

            objData.RequireInitialPointerAlignment();
            objData.AddSymbol(this);

            int typeTlsIndex = 0;
            if (!relocsOnly)
            {
                if (IsInlined)
                {
                    // Inlined threadstatics are stored as a single data block and thus do not need
                    // an index in the containing storage.
                    // We use a negative index to indicate that. Any negative value would work.
                    // For the purpose of natvis we will encode the offset of the type storage within the block.
                    typeTlsIndex = - (_inlinedThreadStatics.GetTypeStorageOffset(_type) + factory.Target.PointerSize);

                    // the type of the storage block for inlined threadstatics, if present,
                    // is serialized as the item #0 among other storage block types.
                    Debug.Assert(_inlinedThreadStatics.IndexFromBeginningOfArray == 0);
                }
                else
                {
                    ISymbolDefinitionNode node = factory.TypeThreadStaticsSymbol(_type);
                    typeTlsIndex = ((ThreadStaticsNode)node).IndexFromBeginningOfArray;
                }
            }

            // needed to construct storage
            objData.EmitPointerReloc(factory.TypeManagerIndirection);

            // tls storage ID for uninlined types. used to:
            // - get the type from the type manager
            // - get the slot from the per-type storage array
            objData.EmitNaturalInt(typeTlsIndex);

            return objData.ToObjectData();
        }

        public MetadataType Type => _type;

        public override int ClassCode => -149601250;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return comparer.Compare(_type, ((TypeThreadStaticIndexNode)other)._type);
        }
    }
}
