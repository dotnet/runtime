// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Describes types of arguments of generic type instances.
    /// </summary>
    public class GenericCompositionNode : ObjectNode, ISymbolDefinitionNode
    {
        private readonly Instantiation _details;
        private readonly bool _constructed;

        internal GenericCompositionNode(Instantiation details, bool constructed)
        {
            _details = details;
            _constructed = constructed;
        }

        public override bool ShouldSkipEmittingObjectNode(NodeFactory factory) => !_constructed && factory.ConstructedGenericComposition(_details).Marked;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("__GenericInstance"u8);

            foreach (TypeDesc instArg in _details)
            {
                sb.Append('_');
                sb.Append(nameMangler.GetMangledTypeName(instArg));
            }
        }

        public int Offset
        {
            get
            {
                return 0;
            }
        }

        public override ObjectNodeSection GetSection(NodeFactory factory)
        {
            if (factory.Target.IsWindows)
                return ObjectNodeSection.FoldableReadOnlyDataSection;
            else
                return ObjectNodeSection.DataSection;
        }

        public override bool IsShareable => true;

        public override bool StaticDependenciesAreComputed => true;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            var builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.AddSymbol(this);

            bool useRelativePointers = factory.Target.SupportsRelativePointers;
            if (useRelativePointers)
                builder.RequireInitialAlignment(4);
            else
                builder.RequireInitialPointerAlignment();

            foreach (var typeArg in _details)
            {
                IEETypeNode node = _constructed ? factory.MaximallyConstructableType(typeArg) : factory.NecessaryTypeSymbol(typeArg);

                if (useRelativePointers)
                    builder.EmitReloc(node, RelocType.IMAGE_REL_BASED_RELPTR32);
                else
                    builder.EmitPointerReloc(node);
            }

            return builder.ToObjectData();
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override int ClassCode => -762680703;
        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            var otherComposition = (GenericCompositionNode)other;
            var compare = _details.Length.CompareTo(otherComposition._details.Length);
            if (compare != 0)
                return compare;

            compare = _constructed.CompareTo(otherComposition._constructed);
            if (compare != 0)
                return compare;

            for (int i = 0; i < _details.Length; i++)
            {
                compare = comparer.Compare(_details[i], otherComposition._details[i]);
                if (compare != 0)
                    return compare;
            }

            return 0;
        }
    }
}
