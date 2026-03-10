// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Linq
{
    /// <summary>
    /// An immutable node in a singly-linked list of items.
    /// </summary>
    /// <typeparam name="TSource">The type of the node's item.</typeparam>
    internal sealed class SingleLinkedNode<TSource>
    {
        /// <summary>
        /// Constructs a tail node.
        /// </summary>
        /// <param name="item">The item to place in the tail node.</param>
        public SingleLinkedNode(TSource item)
        {
            Item = item;
        }

        /// <summary>
        /// Constructs a node linked to the specified node.
        /// </summary>
        /// <param name="linked">The linked node.</param>
        /// <param name="item">The item to place in this node.</param>
        private SingleLinkedNode(SingleLinkedNode<TSource> linked, TSource item)
        {
            Debug.Assert(linked is not null);
            Linked = linked;
            Item = item;
        }

        /// <summary>
        /// The item held by this node.
        /// </summary>
        public TSource Item { get; }

        /// <summary>
        /// The next node in the singly-linked list.
        /// </summary>
        public SingleLinkedNode<TSource>? Linked { get; }

        /// <summary>
        /// Creates a new node that holds the specified item and is linked to this node.
        /// </summary>
        /// <param name="item">The item to place in the new node.</param>
        public SingleLinkedNode<TSource> Add(TSource item) => new SingleLinkedNode<TSource>(this, item);

        /// <summary>
        /// Gets the number of items in this and subsequent nodes by walking the linked list.
        /// </summary>
        public int GetCount()
        {
            int count = 0;
            for (SingleLinkedNode<TSource>? node = this; node is not null; node = node.Linked)
            {
                count++;
            }

            return count;
        }

        /// <summary>
        /// Gets the node at a logical index by walking the linked list.
        /// </summary>
        /// <param name="index">The logical index.</param>
        /// <remarks>
        /// The caller should make sure <paramref name="index"/> is less than this node's count.
        /// </remarks>
        public SingleLinkedNode<TSource> GetNode(int index)
        {
            Debug.Assert(index >= 0 && index < GetCount());

            SingleLinkedNode<TSource> node = this;
            for (; index > 0; index--)
            {
                node = node.Linked!;
                Debug.Assert(node is not null);
            }

            return node;
        }

        /// <summary>
        /// Returns an array that contains the items of this node's singly-linked list in reverse.
        /// </summary>
        /// <param name="count">The number of items in this node.</param>
        public TSource[] ToArray(int count)
        {
            Debug.Assert(count == GetCount());

            TSource[] array = new TSource[count];
            FillReversed(array);
            return array;
        }

        /// <summary>
        /// Fills a start of a span with the items of this node's singly-linked list.
        /// </summary>
        /// <param name="span">The span to fill. Must be at least the size required.</param>
        public void Fill(Span<TSource> span)
        {
            int index = 0;
            for (SingleLinkedNode<TSource>? node = this; node is not null; node = node.Linked)
            {
                span[index] = node.Item;
                index++;
            }
        }

        /// <summary>
        /// Fills the end of a span with the items of this node's singly-linked list in reverse.
        /// </summary>
        /// <param name="span">The span to fill.</param>
        public void FillReversed(Span<TSource> span)
        {
            int index = span.Length;
            for (SingleLinkedNode<TSource>? node = this; node is not null; node = node.Linked)
            {
                --index;
                span[index] = node.Item;
            }
        }
    }
}
