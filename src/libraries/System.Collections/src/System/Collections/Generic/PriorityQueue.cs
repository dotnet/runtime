// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Collections.Generic
{
    /// <summary>
    /// Represents a data structure in which each element additionally has a priority
    /// associated with it.
    /// </summary>
    /// <typeparam name="TElement">The type of the element.</typeparam>
    /// <typeparam name="TPriority">The type of the priority.</typeparam>
    public class PriorityQueue<TElement, TPriority>
    {
        /// <summary>
        /// Represents an implicit heap-ordered complete d-ary tree, stored as an array.
        /// </summary>
        private readonly List<(TElement element, TPriority priority)> nodes;

        private const int RootIndex = 0;

        /// <summary>
        /// Specifies the arity of the d-ary heap, which here is quaternary.
        /// </summary>
        private const int Arity = 4;

        /// <summary>
        /// Creates an empty priority queue.
        /// </summary>
        public PriorityQueue()
        {
            this.nodes = new List<(TElement, TPriority)>();
            this.UnorderedItems = new UnorderedItemsCollection(this.nodes);
            this.Comparer = Comparer<TPriority>.Default;
        }

        /// <summary>
        /// Creates an empty priority queue with the specified initial capacity for its underlying array.
        /// </summary>
        public PriorityQueue(int initialCapacity)
        {
            if (initialCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));
            }

            this.nodes = new List<(TElement, TPriority)>(initialCapacity);
            this.UnorderedItems = new UnorderedItemsCollection(this.nodes);
            this.Comparer = Comparer<TPriority>.Default;
        }

        /// <summary>
        /// Creates an empty priority queue with the specified priority comparer.
        /// </summary>
        public PriorityQueue(IComparer<TPriority>? comparer)
        {
            this.nodes = new List<(TElement, TPriority)>();
            this.UnorderedItems = new UnorderedItemsCollection(this.nodes);
            this.Comparer = comparer ?? Comparer<TPriority>.Default;
        }

        /// <summary>
        /// Creates an empty priority queue with the specified priority comparer and
        /// the specified initial capacity for its underlying array.
        /// </summary>
        public PriorityQueue(int initialCapacity, IComparer<TPriority>? comparer)
        {
            if (initialCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));
            }

            this.nodes = new List<(TElement, TPriority)>(initialCapacity);
            this.UnorderedItems = new UnorderedItemsCollection(this.nodes);
            this.Comparer = comparer ?? Comparer<TPriority>.Default;
        }

        /// <summary>
        /// Creates a priority queue populated with the specified elements and priorities.
        /// </summary>
        public PriorityQueue(IEnumerable<(TElement element, TPriority priority)> items)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            this.nodes = new List<(TElement, TPriority)>(items);
            this.UnorderedItems = new UnorderedItemsCollection(this.nodes);
            this.Comparer = Comparer<TPriority>.Default;

            if (this.nodes.Count > 1)
            {
                this.Heapify();
            }
        }

        /// <summary>
        /// Creates a priority queue populated with the specified elements and priorities,
        /// and with the specified priority comparer.
        /// </summary>
        public PriorityQueue(IEnumerable<(TElement element, TPriority priority)> items, IComparer<TPriority>? comparer)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            this.nodes = new List<(TElement, TPriority)>(items);
            this.UnorderedItems = new UnorderedItemsCollection(this.nodes);
            this.Comparer = comparer ?? Comparer<TPriority>.Default;

            if (this.nodes.Count > 1)
            {
                this.Heapify();
            }
        }

        /// <summary>
        /// Gets the current amount of items in the priority queue.
        /// </summary>
        public int Count => this.nodes.Count;

        /// <summary>
        /// Gets the priority comparer of the priority queue.
        /// </summary>
        public IComparer<TPriority> Comparer { get; }

        /// <summary>
        /// Enqueues the specified element and associates it with the specified priority.
        /// </summary>
        public void Enqueue(TElement element, TPriority priority)
        {
            if (element is null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            if (priority is null)
            {
                throw new ArgumentNullException(nameof(priority));
            }

            // Add the node at the end
            var node = (element, priority);
            this.nodes.Add(node);

            // Restore the heap order
            var lastNodeIndex = this.GetLastNodeIndex();
            this.MoveUp(node, lastNodeIndex);
        }

        /// <summary>
        /// Gets the element associated with the minimal priority.
        /// </summary>
        /// <exception cref="InvalidOperationException">The queue is empty.</exception>
        public TElement Peek()
        {
            if (this.TryPeek(out var element, out var priority))
            {
                return element;
            }
            else
            {
                throw new InvalidOperationException(
                    "The priority queue is empty, cannot get the element with minimal priority.");
            }
        }

        /// <summary>
        /// Dequeues the element associated with the minimal priority.
        /// </summary>
        /// <exception cref="InvalidOperationException">The queue is empty.</exception>
        public TElement Dequeue()
        {
            if (this.TryDequeue(out var element, out var priority))
            {
                return element;
            }
            else
            {
                throw new InvalidOperationException(
                    "The priority queue is empty, cannot dequeue an element.");
            }
        }

        /// <summary>
        /// Dequeues the element associated with the minimal priority
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the priority queue is non-empty; <see langword="false"/> otherwise.
        /// </returns>
        public bool TryDequeue([MaybeNullWhen(false)] out TElement element, [MaybeNullWhen(false)] out TPriority priority)
        {
            if (this.nodes.Count == 0)
            {
                element = default;
                priority = default;
                return false;
            }
            else
            {
                (element, priority) = this.nodes[RootIndex];
                this.Remove(RootIndex);
                return true;
            }
        }

        /// <summary>
        /// Gets the element associated with the minimal priority.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the priority queue is non-empty; <see langword="false"/> otherwise.
        /// </returns>
        public bool TryPeek([MaybeNullWhen(false)] out TElement element, [MaybeNullWhen(false)] out TPriority priority)
        {
            if (this.nodes.Count == 0)
            {
                element = default;
                priority = default;
                return false;
            }
            else
            {
                (element, priority) = this.nodes[RootIndex];
                return true;
            }
        }

        /// <summary>
        /// Combined enqueue/dequeue operation, generally more efficient than sequential Enqueue/Dequeue calls.
        /// </summary>
        public TElement EnqueueDequeue(TElement element, TPriority priority)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Enqueues a collection of element/priority pairs.
        /// </summary>
        public void EnqueueRange(IEnumerable<(TElement Element, TPriority Priority)> items)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            foreach (var (element, priority) in items)
            {
                this.Enqueue(element, priority);
            }
        }

        /// <summary>
        /// Enqueues a collection of elements, each associated with the specified priority.
        /// </summary>
        public void EnqueueRange(IEnumerable<TElement> elements, TPriority priority)
        {
            if (elements is null)
            {
                throw new ArgumentNullException(nameof(elements));
            }

            foreach (var element in elements)
            {
                this.Enqueue(element, priority);
            }
        }

        /// <summary>
        /// Removes all items from the priority queue.
        /// </summary>
        public void Clear()
        {
            this.nodes.Clear();
        }

        /// <summary>
        /// Ensures that the priority queue has the specified capacity
        /// and resizes its underlying array if necessary.
        /// </summary>
        public void EnsureCapacity(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            if (capacity <= this.nodes.Count)
            {
                return;
            }

            this.nodes.Capacity = capacity;
        }

        /// <summary>
        /// Sets the capacity to the actual number of items in the priority queue,
        /// if that is less than 90 percent of current capacity.
        /// </summary>
        public void TrimExcess()
        {
            this.nodes.TrimExcess();
        }

        /// <summary>
        /// Removes the node at the specified index.
        /// </summary>
        private void Remove(int indexOfNodeToRemove)
        {
            // The idea is to replace the specified node by the very last
            // node and shorten the array by one.

            var lastNodeIndex = this.GetLastNodeIndex();
            var lastNode = this.nodes[lastNodeIndex];
            this.nodes.RemoveAt(lastNodeIndex);

            // In case we wanted to remove the node that was the last one,
            // we are done.

            if (indexOfNodeToRemove == lastNodeIndex)
            {
                return;
            }

            // Our last node was erased from the array and needs to be
            // inserted again. Of course, we will overwrite the node we
            // wanted to remove. After that operation, we will need
            // to restore the heap property (in general).

            var nodeToRemove = this.nodes[indexOfNodeToRemove];

            var relation = this.Comparer.Compare(lastNode.priority, nodeToRemove.priority);
            this.PutAt(lastNode, indexOfNodeToRemove);

            if (relation < 0)
            {
                this.MoveUp(lastNode, indexOfNodeToRemove);
            }
            else
            {
                this.MoveDown(lastNode, indexOfNodeToRemove);
            }
        }

        /// <summary>
        /// Puts a node at the specified index.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PutAt((TElement element, TPriority priority) node, int index)
        {
            this.nodes[index] = node;
        }

        /// <summary>
        /// Gets the index of the last node in the heap.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetLastNodeIndex() => this.nodes.Count - 1;

        /// <summary>
        /// Gets the index of an element's parent.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetParentIndex(int index) => (index - 1) / Arity;

        /// <summary>
        /// Gets the index of the first child of an element.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetFirstChildIndex(int index) => Arity * index + 1;

        /// <summary>
        /// Converts an unordered list into a heap.
        /// </summary>
        private void Heapify()
        {
            // Leaves of the tree are in fact 1-element heaps, for which there
            // is no need to correct them. The heap property needs to be restored
            // only for higher nodes, starting from the first node that has children.
            // It is the parent of the very last element in the array.

            var lastNodeIndex = this.GetLastNodeIndex();
            var lastParentWithChildren = this.GetParentIndex(lastNodeIndex);

            for (var index = lastParentWithChildren; index >= 0; --index)
            {
                this.MoveDown(this.nodes[index], index);
            }
        }

        /// <summary>
        /// Moves a node up in the tree to restore heap order.
        /// </summary>
        private void MoveUp((TElement element, TPriority priority) node, int nodeIndex)
        {
            // Instead of swapping items all the way to the root, we will perform
            // a similar optimization as in the insertion sort.

            while (nodeIndex > 0)
            {
                var parentIndex = this.GetParentIndex(nodeIndex);
                var parent = this.nodes[parentIndex];

                if (this.Comparer.Compare(node.priority, parent.priority) < 0)
                {
                    this.PutAt(parent, nodeIndex);
                    nodeIndex = parentIndex;
                }
                else
                {
                    break;
                }
            }

            this.PutAt(node, nodeIndex);
        }

        /// <summary>
        /// Moves a node down in the tree to restore heap order.
        /// </summary>
        private void MoveDown((TElement element, TPriority priority) node, int nodeIndex)
        {
            // The node to move down will not actually be swapped every time.
            // Rather, values on the affected path will be moved up, thus leaving a free spot
            // for this value to drop in. Similar optimization as in the insertion sort.

            int i;
            while ((i = this.GetFirstChildIndex(nodeIndex)) < this.nodes.Count)
            {
                // Check if the current node (pointed by 'nodeIndex') should really be extracted
                // first, or maybe one of its children should be extracted earlier.
                var topChild = this.nodes[i];
                var childrenIndexesLimit = Math.Min(i + Arity, this.nodes.Count);
                int topChildIndex = i;

                while (++i < childrenIndexesLimit)
                {
                    var child = this.nodes[i];
                    if (this.Comparer.Compare(child.priority, topChild.priority) < 0)
                    {
                        topChild = child;
                        topChildIndex = i;
                    }
                }

                // In case no child needs to be extracted earlier than the current node,
                // there is nothing more to do - the right spot was found.
                if (this.Comparer.Compare(node.priority, topChild.priority) <= 0)
                {
                    break;
                }

                // Move the top child up by one node and now investigate the
                // node that was considered to be the top child (recursive).
                this.PutAt(topChild, nodeIndex);
                nodeIndex = topChildIndex;
            }

            this.PutAt(node, nodeIndex);
        }

        /// <summary>
        /// Gets a collection that enumerates the elements of the queue.
        /// </summary>
        public UnorderedItemsCollection UnorderedItems { get; }

        public partial class UnorderedItemsCollection : IReadOnlyCollection<(TElement element, TPriority priority)>, ICollection
        {
            private readonly IReadOnlyCollection<(TElement element, TPriority priority)> items;

            public UnorderedItemsCollection(IReadOnlyCollection<(TElement element, TPriority priority)> items)
            {
                this.items = items;
            }

            public int Count => this.items.Count;
            object ICollection.SyncRoot => this;
            bool ICollection.IsSynchronized => false;

            public void CopyTo(Array array, int index) => throw new NotImplementedException();

            public struct Enumerator : IEnumerator<(TElement element, TPriority priority)>, IEnumerator
            {
                private readonly IEnumerator<(TElement element, TPriority priority)> enumerator;

                internal Enumerator(System.Collections.Generic.IEnumerator<(TElement element, TPriority priority)> enumerator)
                {
                    this.enumerator = enumerator;
                }

                (TElement element, TPriority priority) IEnumerator<(TElement element, TPriority priority)>.Current
                    => this.enumerator.Current;

                object IEnumerator.Current => this.enumerator.Current;

                void IDisposable.Dispose() => this.enumerator.Dispose();
                bool IEnumerator.MoveNext() => this.enumerator.MoveNext();
                void IEnumerator.Reset() => this.enumerator.Reset();
            }

            public Enumerator GetEnumerator()
                => new Enumerator(this.items.GetEnumerator());

            IEnumerator<(TElement element, TPriority priority)> IEnumerable<(TElement element, TPriority priority)>.GetEnumerator()
                => new Enumerator(this.items.GetEnumerator());

            IEnumerator IEnumerable.GetEnumerator()
                => new Enumerator(this.items.GetEnumerator());
        }
    }
}
