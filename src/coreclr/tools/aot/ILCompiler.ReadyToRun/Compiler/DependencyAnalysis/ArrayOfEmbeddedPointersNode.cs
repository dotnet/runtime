// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.Text;

namespace ILCompiler.DependencyAnalysis
{
    interface ISimpleEmbeddedPointerIndirectionNode<out TTarget>
        where TTarget : ISortableSymbolNode
    {
        TTarget Target { get; }
    }

    /// <summary>
    /// Represents an array of pointers to symbols. <typeparamref name="TTarget"/> is the type
    /// of node each pointer within the vector points to.
    /// </summary>
    public sealed class ArrayOfEmbeddedPointersNode<TTarget> : ArrayOfEmbeddedDataNode<EmbeddedPointerIndirectionNode<TTarget>>
        where TTarget : ISortableSymbolNode
    {
        private int _nextId;
        private string _startSymbolMangledName;

        /// <summary>
        /// Provides a callback mechanism for notification when an EmbeddedPointerIndirectionNode is marked and added to the
        /// parent ArrayOfEmbeddedPointersNode's internal list
        /// </summary>
        public delegate void OnMarkedDelegate(EmbeddedPointerIndirectionNode<TTarget> embeddedObject);

        public ArrayOfEmbeddedPointersNode(string startSymbolMangledName, string endSymbolMangledName, IComparer<EmbeddedPointerIndirectionNode<TTarget>> nodeSorter)
            : base(
                  startSymbolMangledName,
                  endSymbolMangledName,
                  nodeSorter)
        {
            _startSymbolMangledName = startSymbolMangledName;
        }

        public EmbeddedObjectNode NewNode(TTarget target)
        {
            return new SimpleEmbeddedPointerIndirectionNode(this, target);
        }

        public EmbeddedObjectNode NewNodeWithSymbol(TTarget target)
        {
            return new EmbeddedPointerIndirectionWithSymbolNode(this, target, GetNextId());
        }

        int GetNextId()
        {
            return System.Threading.Interlocked.Increment(ref _nextId);
        }

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;

        public override int ClassCode => (int)ObjectNodeOrder.ArrayOfEmbeddedPointersNode;

        public class PointerIndirectionNodeComparer : IComparer<EmbeddedPointerIndirectionNode<TTarget>>
        {
            private IComparer<TTarget> _innerComparer;

            public PointerIndirectionNodeComparer(IComparer<TTarget> innerComparer)
            {
                _innerComparer = innerComparer;
            }

            public int Compare(EmbeddedPointerIndirectionNode<TTarget> x, EmbeddedPointerIndirectionNode<TTarget> y)
            {
                return _innerComparer.Compare(x.Target, y.Target);
            }
        }

        private class SimpleEmbeddedPointerIndirectionNode : EmbeddedPointerIndirectionNode<TTarget>, ISimpleEmbeddedPointerIndirectionNode<TTarget>
        {
            protected ArrayOfEmbeddedPointersNode<TTarget> _parentNode;

            public SimpleEmbeddedPointerIndirectionNode(ArrayOfEmbeddedPointersNode<TTarget> futureParent, TTarget target)
                : base(target)
            {
                _parentNode = futureParent;
            }

            protected override string GetName(NodeFactory factory) => $"Embedded pointer to {Target.GetMangledName(factory.NameMangler)}";

            protected override void OnMarked(NodeFactory factory)
            {
                // We don't want the child in the parent collection unless it's necessary.
                // Only when this node gets marked, the parent node becomes the actual parent.
                _parentNode.AddEmbeddedObject(this);
            }

            public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
            {
                return new[]
                {
                    new DependencyListEntry(Target, "reloc"),
                    new DependencyListEntry(_parentNode, "Pointer region")
                };
            }

            public override int ClassCode => -66002498;

            public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
            {
                var otherNode = (ISimpleEmbeddedPointerIndirectionNode<ISortableSymbolNode>)other;
                return comparer.Compare(Target, otherNode.Target);
            }
        }

        private class EmbeddedPointerIndirectionWithSymbolNode : SimpleEmbeddedPointerIndirectionNode, ISymbolDefinitionNode
        {
            private int _id;

            public EmbeddedPointerIndirectionWithSymbolNode(ArrayOfEmbeddedPointersNode<TTarget> futureParent, TTarget target, int id)
                : base(futureParent, target)
            {
                _id = id;
            }


            int ISymbolNode.Offset => 0;

            int ISymbolDefinitionNode.Offset => OffsetFromBeginningOfArray;

            public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
            {
                sb.Append(nameMangler.CompilationUnitPrefix).Append(_parentNode._startSymbolMangledName).Append("_").Append(_id.ToStringInvariant());
            }
        }
    }
}
