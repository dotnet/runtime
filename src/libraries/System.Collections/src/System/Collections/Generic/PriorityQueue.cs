// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Collections.Generic
{
    /// <summary>
    /// Represents a data structure in which each element has an associated priority
    /// that determines the order in which the pair is dequeued.
    /// </summary>
    /// <typeparam name="TElement">The type of the element.</typeparam>
    /// <typeparam name="TPriority">The type of the priority.</typeparam>
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(PriorityQueueDebugView<,>))]
    public class PriorityQueue<TElement, TPriority>
    {
        /// <summary>
        /// Represents an implicit heap-ordered complete d-ary tree, stored as an array.
        /// </summary>
        private (TElement Element, TPriority Priority)[] _nodes;

        /// <summary>
        /// Custom comparer used to order the heap.
        /// </summary>
        private readonly IComparer<TPriority>? _comparer;

        /// <summary>
        /// Lazily-initialized collection used to expose the contents of the queue.
        /// </summary>
        private UnorderedItemsCollection? _unorderedItems;

        /// <summary>
        /// The number of nodes in the heap.
        /// </summary>
        private int _size;

        /// <summary>
        /// Version updated on mutation to help validate enumerators operate on a consistent state.
        /// </summary>
        private int _version;

        /// <summary>
        /// Specifies the arity of the d-ary heap, which here is quaternary.
        /// It is assumed that this value is a power of 2.
        /// </summary>
        private const int Arity = 4;

        /// <summary>
        /// The binary logarithm of <see cref="Arity" />.
        /// </summary>
        private const int Log2Arity = 2;

#if DEBUG
        static PriorityQueue()
        {
            Debug.Assert(Log2Arity > 0 && Math.Pow(2, Log2Arity) == Arity);
        }
#endif

        /// <summary>
        /// Creates an empty priority queue.
        /// </summary>
        public PriorityQueue()
        {
            _nodes = Array.Empty<(TElement, TPriority)>();
            _comparer = InitializeComparer(null);
        }

        /// <summary>
        /// Creates an empty priority queue with the specified initial capacity for its underlying array.
        /// </summary>
        public PriorityQueue(int initialCapacity)
            : this(initialCapacity, comparer: null)
        {
        }

        /// <summary>
        /// Creates an empty priority queue with the specified priority comparer.
        /// </summary>
        public PriorityQueue(IComparer<TPriority>? comparer)
        {
            _nodes = Array.Empty<(TElement, TPriority)>();
            _comparer = InitializeComparer(comparer);
        }

        /// <summary>
        /// Creates an empty priority queue with the specified priority comparer and
        /// the specified initial capacity for its underlying array.
        /// </summary>
        public PriorityQueue(int initialCapacity, IComparer<TPriority>? comparer)
        {
            if (initialCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(initialCapacity), initialCapacity, SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            _nodes = new (TElement, TPriority)[initialCapacity];
            _comparer = InitializeComparer(comparer);
        }

        /// <summary>
        /// Creates a priority queue populated with the specified elements and priorities.
        /// </summary>
        public PriorityQueue(IEnumerable<(TElement Element, TPriority Priority)> items)
            : this(items, comparer: null)
        {
        }

        /// <summary>
        /// Creates a priority queue populated with the specified elements and priorities,
        /// and with the specified priority comparer.
        /// </summary>
        public PriorityQueue(IEnumerable<(TElement Element, TPriority Priority)> items, IComparer<TPriority>? comparer)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            _nodes = EnumerableHelpers.ToArray(items, out _size);
            _comparer = InitializeComparer(comparer);

            if (_size > 1)
            {
                Heapify();
            }
        }

        /// <summary>
        /// Gets the current amount of items in the priority queue.
        /// </summary>
        public int Count => _size;

        /// <summary>
        /// Gets the priority comparer of the priority queue.
        /// </summary>
        public IComparer<TPriority> Comparer => _comparer ?? Comparer<TPriority>.Default;

        /// <summary>
        /// Gets a collection that enumerates the elements of the queue.
        /// </summary>
        public UnorderedItemsCollection UnorderedItems => _unorderedItems ??= new UnorderedItemsCollection(this);

        /// <summary>
        /// Enqueues the specified element and associates it with the specified priority.
        /// </summary>
        public void Enqueue(TElement element, TPriority priority)
        {
            // Virtually add the node at the end of the underlying array.
            // Note that the node being enqueued does not need to be physically placed
            // there at this point, as such an assignment would be redundant.

            int currentSize = _size++;
            _version++;

            if (_nodes.Length == currentSize)
            {
                Grow(currentSize + 1);
            }

            if (_comparer == null)
            {
                MoveUpDefaultComparer((element, priority), currentSize);
            }
            else
            {
                MoveUpCustomComparer((element, priority), currentSize);
            }
        }

        /// <summary>
        /// Gets the element associated with the minimal priority.
        /// </summary>
        /// <exception cref="InvalidOperationException">The queue is empty.</exception>
        public TElement Peek()
        {
            if (_size == 0)
            {
                throw new InvalidOperationException(SR.InvalidOperation_EmptyQueue);
            }

            return _nodes[0].Element;
        }

        /// <summary>
        /// Dequeues the element associated with the minimal priority.
        /// </summary>
        /// <exception cref="InvalidOperationException">The queue is empty.</exception>
        public TElement Dequeue()
        {
            if (_size == 0)
            {
                throw new InvalidOperationException(SR.InvalidOperation_EmptyQueue);
            }

            TElement element = _nodes[0].Element;
            RemoveRootNode();
            return element;
        }

        /// <summary>
        /// Dequeues the element associated with the minimal priority
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the priority queue is non-empty; <see langword="false"/> otherwise.
        /// </returns>
        public bool TryDequeue([MaybeNullWhen(false)] out TElement element, [MaybeNullWhen(false)] out TPriority priority)
        {
            if (_size != 0)
            {
                (element, priority) = _nodes[0];
                RemoveRootNode();
                return true;
            }

            element = default;
            priority = default;
            return false;
        }

        /// <summary>
        /// Gets the element associated with the minimal priority.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the priority queue is non-empty; <see langword="false"/> otherwise.
        /// </returns>
        public bool TryPeek([MaybeNullWhen(false)] out TElement element, [MaybeNullWhen(false)] out TPriority priority)
        {
            if (_size != 0)
            {
                (element, priority) = _nodes[0];
                return true;
            }

            element = default;
            priority = default;
            return false;
        }

        /// <summary>
        /// Combined enqueue/dequeue operation, generally more efficient than sequential Enqueue/Dequeue calls.
        /// </summary>
        public TElement EnqueueDequeue(TElement element, TPriority priority)
        {
            if (_size != 0)
            {
                (TElement Element, TPriority Priority) root = _nodes[0];

                if (_comparer == null)
                {
                    if (Comparer<TPriority>.Default.Compare(priority, root.Priority) > 0)
                    {
                        MoveDownDefaultComparer((element, priority), 0);
                        _version++;
                        return root.Element;
                    }
                }
                else
                {
                    if (_comparer.Compare(priority, root.Priority) > 0)
                    {
                        MoveDownCustomComparer((element, priority), 0);
                        _version++;
                        return root.Element;
                    }
                }
            }

            return element;
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

            if (_size == 0)
            {
                _nodes = EnumerableHelpers.ToArray(items, out _size);
                _version++;

                if (_size > 1)
                {
                    Heapify();
                }
            }
            else
            {
                if (EnumerableHelpers.TryGetCount(items, out int count) && _nodes.Length - _size < count)
                {
                    Grow(_size + count);
                }

                foreach ((TElement element, TPriority priority) in items)
                {
                    Enqueue(element, priority);
                }
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

            if (EnumerableHelpers.TryGetCount(elements, out int count) && _nodes.Length - _size < count)
            {
                Grow(_size + count);
            }

            if (_size == 0)
            {
                int i = 0;
                foreach (TElement element in elements)
                {
                    if (_nodes.Length == i)
                    {
                        Grow(i + 1);
                    }

                    _nodes[i++] = (element, priority);
                }

                _size = i;
                _version++;

                if (i > 1)
                {
                    Heapify();
                }
            }
            else
            {
                foreach (TElement element in elements)
                {
                    Enqueue(element, priority);
                }
            }
        }

        /// <summary>
        /// Removes all items from the priority queue.
        /// </summary>
        public void Clear()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<(TElement, TPriority)>())
            {
                // Clear the elements so that the gc can reclaim the references
                Array.Clear(_nodes, 0, _size);
            }
            _size = 0;
            _version++;
        }

        /// <summary>
        /// Ensures that the priority queue has the specified capacity
        /// and resizes its underlying array if necessary.
        /// </summary>
        public int EnsureCapacity(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (_nodes.Length < capacity)
            {
                Grow(capacity);
                _version++;
            }

            return _nodes.Length;
        }

        /// <summary>
        /// Sets the capacity to the actual number of items in the priority queue,
        /// if that is less than 90 percent of current capacity.
        /// </summary>
        public void TrimExcess()
        {
            int threshold = (int)(_nodes.Length * 0.9);
            if (_size < threshold)
            {
                Array.Resize(ref _nodes, _size);
                _version++;
            }
        }

        /// <summary>
        /// Grows the priority queue to match the specified min capacity.
        /// </summary>
        private void Grow(int minCapacity)
        {
            Debug.Assert(_nodes.Length < minCapacity);

            // Array.MaxArrayLength is internal to S.P.CoreLib, replicate here.
            const int MaxArrayLength = 0X7FEFFFFF;
            const int GrowFactor = 2;
            const int MinimumGrow = 4;

            int newcapacity = GrowFactor * _nodes.Length;

            // Allow the queue to grow to maximum possible capacity (~2G elements) before encountering overflow.
            // Note that this check works even when _nodes.Length overflowed thanks to the (uint) cast
            if ((uint)newcapacity > MaxArrayLength) newcapacity = MaxArrayLength;

            // Ensure minimum growth is respected.
            newcapacity = Math.Max(newcapacity, _nodes.Length + MinimumGrow);

            // If the computed capacity is still less than specified, set to the original argument.
            // Capacities exceeding MaxArrayLength will be surfaced as OutOfMemoryException by Array.Resize.
            if (newcapacity < minCapacity) newcapacity = minCapacity;

            Array.Resize(ref _nodes, newcapacity);
        }

        /// <summary>
        /// Removes the node from the root of the heap
        /// </summary>
        private void RemoveRootNode()
        {
            // The idea is to replace the root node by the very last
            // node and shorten the array by one.
            int lastNodeIndex = _size - 1;
            (TElement Element, TPriority Priority) lastNode = _nodes[lastNodeIndex];
            if (RuntimeHelpers.IsReferenceOrContainsReferences<(TElement, TPriority)>())
            {
                _nodes[lastNodeIndex] = default;
            }

            _size--;
            _version++;

            if (_comparer == null)
            {
                MoveDownDefaultComparer(lastNode, 0);
            }
            else
            {
                MoveDownCustomComparer(lastNode, 0);
            }
        }

        /// <summary>
        /// Gets the index of an element's parent.
        /// </summary>
        private int GetParentIndex(int index) => (index - 1) >> Log2Arity;

        /// <summary>
        /// Gets the index of the first child of an element.
        /// </summary>
        private int GetFirstChildIndex(int index) => (index << Log2Arity) + 1;

        /// <summary>
        /// Converts an unordered list into a heap.
        /// </summary>
        private void Heapify()
        {
            // Leaves of the tree are in fact 1-element heaps, for which there
            // is no need to correct them. The heap property needs to be restored
            // only for higher nodes, starting from the first node that has children.
            // It is the parent of the very last element in the array.

            (TElement Element, TPriority Priority)[] nodes = _nodes;
            int lastParentWithChildren = GetParentIndex(_size - 1);

            if (_comparer == null)
            {
                for (int index = lastParentWithChildren; index >= 0; --index)
                {
                    MoveDownDefaultComparer(nodes[index], index);
                }
            }
            else
            {
                for (int index = lastParentWithChildren; index >= 0; --index)
                {
                    MoveDownCustomComparer(nodes[index], index);
                }
            }
        }

        /// <summary>
        /// Moves a node up in the tree to restore heap order.
        /// </summary>
        private void MoveUpDefaultComparer((TElement Element, TPriority Priority) node, int nodeIndex)
        {
            // Instead of swapping items all the way to the root, we will perform
            // a similar optimization as in the insertion sort.

            Debug.Assert(_comparer is null);

            (TElement Element, TPriority Priority)[] nodes = _nodes;

            while (nodeIndex > 0)
            {
                int parentIndex = GetParentIndex(nodeIndex);
                (TElement Element, TPriority Priority) parent = nodes[parentIndex];

                if (Comparer<TPriority>.Default.Compare(node.Priority, parent.Priority) < 0)
                {
                    nodes[nodeIndex] = parent;
                    nodeIndex = parentIndex;
                }
                else
                {
                    break;
                }
            }

            nodes[nodeIndex] = node;
        }

        /// <summary>
        /// Moves a node up in the tree to restore heap order.
        /// </summary>
        private void MoveUpCustomComparer((TElement Element, TPriority Priority) node, int nodeIndex)
        {
            // Instead of swapping items all the way to the root, we will perform
            // a similar optimization as in the insertion sort.

            Debug.Assert(_comparer is not null);

            IComparer<TPriority> comparer = _comparer;
            (TElement Element, TPriority Priority)[] nodes = _nodes;

            while (nodeIndex > 0)
            {
                int parentIndex = GetParentIndex(nodeIndex);
                (TElement Element, TPriority Priority) parent = nodes[parentIndex];

                if (comparer.Compare(node.Priority, parent.Priority) < 0)
                {
                    nodes[nodeIndex] = parent;
                    nodeIndex = parentIndex;
                }
                else
                {
                    break;
                }
            }

            nodes[nodeIndex] = node;
        }

        /// <summary>
        /// Moves a node down in the tree to restore heap order.
        /// </summary>
        private void MoveDownDefaultComparer((TElement Element, TPriority Priority) node, int nodeIndex)
        {
            // The node to move down will not actually be swapped every time.
            // Rather, values on the affected path will be moved up, thus leaving a free spot
            // for this value to drop in. Similar optimization as in the insertion sort.

            Debug.Assert(_comparer is null);

            (TElement Element, TPriority Priority)[] nodes = _nodes;
            int size = _size;

            int i;
            while ((i = GetFirstChildIndex(nodeIndex)) < size)
            {
                // Find the child node with the minimal priority
                (TElement Element, TPriority Priority) minChild = nodes[i];
                int minChildIndex = i;

                int childIndexUpperBound = Math.Min(i + Arity, size);
                while (++i < childIndexUpperBound)
                {
                    (TElement Element, TPriority Priority) nextChild = nodes[i];
                    if (Comparer<TPriority>.Default.Compare(nextChild.Priority, minChild.Priority) < 0)
                    {
                        minChild = nextChild;
                        minChildIndex = i;
                    }
                }

                // Heap property is satisfied; insert node in this location.
                if (Comparer<TPriority>.Default.Compare(node.Priority, minChild.Priority) <= 0)
                {
                    break;
                }

                // Move the minimal child up by one node and
                // continue recursively from its location.
                nodes[nodeIndex] = minChild;
                nodeIndex = minChildIndex;
            }

            nodes[nodeIndex] = node;
        }

        /// <summary>
        /// Moves a node down in the tree to restore heap order.
        /// </summary>
        private void MoveDownCustomComparer((TElement Element, TPriority Priority) node, int nodeIndex)
        {
            // The node to move down will not actually be swapped every time.
            // Rather, values on the affected path will be moved up, thus leaving a free spot
            // for this value to drop in. Similar optimization as in the insertion sort.

            Debug.Assert(_comparer is not null);

            IComparer<TPriority> comparer = _comparer;
            (TElement Element, TPriority Priority)[] nodes = _nodes;
            int size = _size;

            int i;
            while ((i = GetFirstChildIndex(nodeIndex)) < size)
            {
                // Find the child node with the minimal priority
                (TElement Element, TPriority Priority) minChild = nodes[i];
                int minChildIndex = i;

                int childIndexUpperBound = Math.Min(i + Arity, size);
                while (++i < childIndexUpperBound)
                {
                    (TElement Element, TPriority Priority) nextChild = nodes[i];
                    if (comparer.Compare(nextChild.Priority, minChild.Priority) < 0)
                    {
                        minChild = nextChild;
                        minChildIndex = i;
                    }
                }

                // Heap property is satisfied; insert node in this location.
                if (comparer.Compare(node.Priority, minChild.Priority) <= 0)
                {
                    break;
                }

                // Move the minimal child up by one node and continue recursively from its location.
                nodes[nodeIndex] = minChild;
                nodeIndex = minChildIndex;
            }

            nodes[nodeIndex] = node;
        }

        /// <summary>
        /// Initializes the custom comparer to be used internally by the heap.
        /// </summary>
        private static IComparer<TPriority>? InitializeComparer(IComparer<TPriority>? comparer)
        {
            if (typeof(TPriority).IsValueType)
            {
                if (comparer == Comparer<TPriority>.Default)
                {
                    // if the user manually specifies the default comparer,
                    // revert to using the optimized path.
                    return null;
                }

                return comparer;
            }
            else
            {
                // Currently the JIT doesn't optimize direct Comparer<T>.Default.Compare
                // calls for reference types, so we want to cache the comparer instance instead.
                // TODO https://github.com/dotnet/runtime/issues/10050: Update if this changes in the future.
                return comparer ?? Comparer<TPriority>.Default;
            }
        }

        /// <summary>
        /// Represents the contents of a <see cref="PriorityQueue{TElement, TPriority}"/> without ordering.
        /// </summary>
        [DebuggerDisplay("Count = {Count}")]
        [DebuggerTypeProxy(typeof(PriorityQueueDebugView<,>))]
        public sealed class UnorderedItemsCollection : IReadOnlyCollection<(TElement Element, TPriority Priority)>, ICollection
        {
            internal readonly PriorityQueue<TElement, TPriority> _queue;

            internal UnorderedItemsCollection(PriorityQueue<TElement, TPriority> queue) => _queue = queue;

            public int Count => _queue._size;
            object ICollection.SyncRoot => this;
            bool ICollection.IsSynchronized => false;

            void ICollection.CopyTo(Array array, int index)
            {
                if (array is null)
                {
                    throw new ArgumentNullException(nameof(array));
                }

                if (array.Rank != 1)
                {
                    throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, nameof(array));
                }

                if (array.GetLowerBound(0) != 0)
                {
                    throw new ArgumentException(SR.Arg_NonZeroLowerBound, nameof(array));
                }

                if (index < 0 || index > array.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_Index);
                }

                if (array.Length - index < _queue._size)
                {
                    throw new ArgumentException(SR.Argument_InvalidOffLen);
                }

                try
                {
                    Array.Copy(_queue._nodes, 0, array, index, _queue._size);
                }
                catch (ArrayTypeMismatchException)
                {
                    throw new ArgumentException(SR.Argument_InvalidArrayType, nameof(array));
                }
            }

            /// <summary>
            /// Enumerates the element and priority pairs of a <see cref="PriorityQueue{TElement, TPriority}"/>.
            /// </summary>
            public struct Enumerator : IEnumerator<(TElement Element, TPriority Priority)>
            {
                private readonly PriorityQueue<TElement, TPriority> _queue;
                private readonly int _version;
                private int _index;
                private (TElement, TPriority) _current;

                internal Enumerator(PriorityQueue<TElement, TPriority> queue)
                {
                    _queue = queue;
                    _index = 0;
                    _version = queue._version;
                    _current = default;
                }

                /// <summary>
                /// Releases all resources used by the <see cref="Enumerator"/>.
                /// </summary>
                public void Dispose() { }

                /// <summary>
                /// Advances the enumerator to the next element of the <see cref="UnorderedItems"/>.
                /// </summary>
                /// <returns><see langword="true"/> if the enumerator was successfully advanced to the next element; <see langword="false"/> if the enumerator has passed the end of the collection.</returns>
                public bool MoveNext()
                {
                    PriorityQueue<TElement, TPriority> localQueue = _queue;

                    if (_version == localQueue._version && ((uint)_index < (uint)localQueue._size))
                    {
                        _current = localQueue._nodes[_index];
                        _index++;
                        return true;
                    }

                    return MoveNextRare();
                }

                private bool MoveNextRare()
                {
                    if (_version != _queue._version)
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                    }

                    _index = _queue._size + 1;
                    _current = default;
                    return false;
                }

                /// <summary>
                /// Gets the element at the current position of the enumerator.
                /// </summary>
                public (TElement Element, TPriority Priority) Current => _current;

                object IEnumerator.Current =>
                    _index == 0 || _index == _queue._size + 1 ? throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen) :
                    Current;

                void IEnumerator.Reset()
                {
                    if (_version != _queue._version)
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                    }

                    _index = 0;
                    _current = default;
                }
            }

            /// <summary>
            /// Returns an enumerator that iterates through the <see cref="UnorderedItems"/>.
            /// </summary>
            /// <returns>An <see cref="Enumerator"/> for the <see cref="UnorderedItems"/>.</returns>
            public Enumerator GetEnumerator() => new Enumerator(_queue);

            IEnumerator<(TElement Element, TPriority Priority)> IEnumerable<(TElement Element, TPriority Priority)>.GetEnumerator() => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
