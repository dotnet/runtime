// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        public TypeThreadStaticIndexNode(MetadataType type)
        {
            _type = type;
        }

        public TypeThreadStaticIndexNode(ThreadStaticsNode inlinedThreadStatics)
        {
            _inlinedThreadStatics = inlinedThreadStatics;
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(_type != null ? nameMangler.NodeMangler.ThreadStaticsIndex(_type) : "_inlinedThreadStaticsIndex");
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
            ISymbolDefinitionNode node = _type != null ?
                        factory.TypeThreadStaticsSymbol(_type) :
                        _inlinedThreadStatics;

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
                ISymbolDefinitionNode node = _type != null ?
                    factory.TypeThreadStaticsSymbol(_type) :
                    _inlinedThreadStatics;

                typeTlsIndex = ((ThreadStaticsNode)node).IndexFromBeginningOfArray;

                if (_type == null)
                {
                    typeTlsIndex = -1;
                }
            }

            objData.EmitPointerReloc(factory.TypeManagerIndirection);
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
