// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
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
        private (TElement Element, TPriority Priority)[] _nodes;

        private UnorderedItemsCollection? _unorderedItems;

        /// <summary>
        /// The number of nodes in the heap.
        /// </summary>
        private int _size;

        /// <summary>
        /// Used to keep enumerator in sync with the collection.
        /// </summary>
        private int _version;

        private const int MinimumElementsToGrowBy = 4;

        private const int RootIndex = 0;

        /// <summary>
        /// Specifies the arity of the d-ary heap, which here is quaternary.
        /// </summary>
        private const int Arity = 4;

        /// <summary>
        /// The binary logarithm of <see cref="Arity" />.
        /// </summary>
        private const int ArityLog2 = 2;

        /// <summary>
        /// Creates an empty priority queue.
        /// </summary>
        public PriorityQueue()
            : this(initialCapacity: 0, comparer: null)
        {
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
            : this(initialCapacity: 0, comparer)
        {
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

            _nodes = (initialCapacity == 0)
                ? Array.Empty<(TElement, TPriority)>()
                : new (TElement, TPriority)[initialCapacity];

            Comparer = comparer ?? Comparer<TPriority>.Default;
        }

        /// <summary>
        /// Creates a priority queue populated with the specified elements and priorities.
        /// </summary>
        public PriorityQueue(IEnumerable<(TElement element, TPriority priority)> items)
            : this(items, comparer: null)
        {
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

            _nodes = EnumerableHelpers.ToArray(items, out _size);
            Comparer = comparer ?? Comparer<TPriority>.Default;

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
        public IComparer<TPriority> Comparer { get; }

        /// <summary>
        /// Gets a collection that enumerates the elements of the queue.
        /// </summary>
        public UnorderedItemsCollection UnorderedItems => _unorderedItems ??= new UnorderedItemsCollection(this);

        /// <summary>
        /// Enqueues the specified element and associates it with the specified priority.
        /// </summary>
        public void Enqueue(TElement element, TPriority priority)
        {
            EnsureEnoughCapacityBeforeAddingNode();

            // Virtually add the node at the end of the underlying array.
            // Note that the node being enqueued does not need to be physically placed
            // there at this point, as such an assignment would be redundant.
            var node = (element, priority);
            _size++;
            _version++;

            // Restore the heap order
            int lastNodeIndex = GetLastNodeIndex();
            MoveUp(node, lastNodeIndex);
        }

        /// <summary>
        /// Gets the element associated with the minimal priority.
        /// </summary>
        /// <exception cref="InvalidOperationException">The queue is empty.</exception>
        public TElement Peek()
        {
            if (TryPeek(out TElement? element, out _))
            {
                return element;
            }
            else
            {
                throw new InvalidOperationException(SR.InvalidOperation_EmptyQueue);
            }
        }

        /// <summary>
        /// Dequeues the element associated with the minimal priority.
        /// </summary>
        /// <exception cref="InvalidOperationException">The queue is empty.</exception>
        public TElement Dequeue()
        {
            if (TryDequeue(out TElement? element, out _))
            {
                return element;
            }
            else
            {
                throw new InvalidOperationException(SR.InvalidOperation_EmptyQueue);
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
            if (_size != 0)
            {
                (element, priority) = _nodes[RootIndex];
                Remove(RootIndex);
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
                (element, priority) = _nodes[RootIndex];
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
            var root = _nodes[RootIndex];

            if (Comparer.Compare(priority, root.Priority) <= 0)
            {
                return element;
            }
            else
            {
                var newRoot = (element, priority);
                _nodes[RootIndex] = newRoot;

                MoveDown(newRoot, RootIndex);
                _version++;

                return root.Element;
            }
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

                if (_size > 1)
                {
                    Heapify();
                }
            }
            else
            {
                foreach (var (element, priority) in items)
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

            if (_size == 0)
            {
                using (var eumerator = elements.GetEnumerator())
                {
                    if (eumerator.MoveNext())
                    {
                        _nodes = new (TElement, TPriority)[MinimumElementsToGrowBy];
                        _nodes[0] = (eumerator.Current, priority);
                        _size = 1;

                        while (eumerator.MoveNext())
                        {
                            EnsureEnoughCapacityBeforeAddingNode();
                            _nodes[_size++] = (eumerator.Current, priority);
                        }
                    }
                }

                if (_size > 1)
                {
                    Heapify();
                }
            }
            else
            {
                foreach (var element in elements)
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
                throw new ArgumentOutOfRangeException(
                    nameof(capacity), capacity, SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            int currentCapacity = _nodes.Length;
            if (currentCapacity >= capacity)
            {
                return currentCapacity;
            }

            int capacityForNextGrowth = ComputeCapacityForNextGrowth();
            if (capacityForNextGrowth > capacity)
            {
                capacity = capacityForNextGrowth;
            }

            SetCapacity(capacity);
            return capacity;
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
                SetCapacity(_size);
            }
        }

        private void EnsureEnoughCapacityBeforeAddingNode()
        {
            if (_size < _nodes.Length)
            {
                return;
            }

            int newCapacity = ComputeCapacityForNextGrowth();
            SetCapacity(newCapacity);
        }

        private int ComputeCapacityForNextGrowth()
        {
            const int GrowthFactor = 2;
            const int MaxArrayLength = 0X7FEFFFFF;

            int newCapacity = _nodes.Length * GrowthFactor;
            if (newCapacity < _nodes.Length + MinimumElementsToGrowBy)
            {
                newCapacity = _nodes.Length + MinimumElementsToGrowBy;
            }

            // Allow the structure to grow to maximum possible capacity (~2G elements) before encountering overflow.
            // Note that this check works even when _nodes.Length overflowed thanks to the (uint) cast.

            if ((uint)newCapacity > MaxArrayLength)
            {
                newCapacity = MaxArrayLength;
            }

            return newCapacity;
        }

        /// <summary>
        /// Grows or shrinks the array holding nodes. Capacity must be >= _size.
        /// </summary>
        private void SetCapacity(int capacity)
        {
            Array.Resize(ref _nodes, capacity);
            _version++;
        }

        /// <summary>
        /// Removes the node at the specified index.
        /// </summary>
        private void Remove(int indexOfNodeToRemove)
        {
            // The idea is to replace the specified node by the very last
            // node and shorten the array by one.

            int lastNodeIndex = GetLastNodeIndex();
            var lastNode = _nodes[lastNodeIndex];
            _size--;
            _version++;
            _nodes[lastNodeIndex] = default;

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

            var nodeToRemove = _nodes[indexOfNodeToRemove];

            int relation = Comparer.Compare(lastNode.Priority, nodeToRemove.Priority);
            _nodes[indexOfNodeToRemove] = lastNode;

            if (relation < 0)
            {
                MoveUp(lastNode, indexOfNodeToRemove);
            }
            else
            {
                MoveDown(lastNode, indexOfNodeToRemove);
            }
        }

        /// <summary>
        /// Gets the index of the last node in the heap.
        /// </summary>
        private int GetLastNodeIndex() => _size - 1;

        /// <summary>
        /// Gets the index of an element's parent.
        /// </summary>
        private int GetParentIndex(int index) => (index - 1) >> ArityLog2;

        /// <summary>
        /// Gets the index of the first child of an element.
        /// </summary>
        private int GetFirstChildIndex(int index) => (int)(Arity * index + 1);

        /// <summary>
        /// Converts an unordered list into a heap.
        /// </summary>
        private void Heapify()
        {
            // Leaves of the tree are in fact 1-element heaps, for which there
            // is no need to correct them. The heap property needs to be restored
            // only for higher nodes, starting from the first node that has children.
            // It is the parent of the very last element in the array.

            int lastNodeIndex = GetLastNodeIndex();
            int lastParentWithChildren = GetParentIndex(lastNodeIndex);

            for (int index = lastParentWithChildren; index >= 0; --index)
            {
                MoveDown(_nodes[index], index);
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
                int parentIndex = GetParentIndex(nodeIndex);
                var parent = _nodes[parentIndex];

                if (Comparer.Compare(node.priority, parent.Priority) < 0)
                {
                    _nodes[nodeIndex] = parent;
                    nodeIndex = parentIndex;
                }
                else
                {
                    break;
                }
            }

            _nodes[nodeIndex] = node;
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
            while ((i = GetFirstChildIndex(nodeIndex)) < _size)
            {
                // Check if the current node (pointed by 'nodeIndex') should really be extracted
                // first, or maybe one of its children should be extracted earlier.
                var topChild = _nodes[i];
                int childrenIndexesLimit = Math.Min(i + Arity, _size);
                int topChildIndex = i;

                while (++i < childrenIndexesLimit)
                {
                    var child = _nodes[i];
                    if (Comparer.Compare(child.Priority, topChild.Priority) < 0)
                    {
                        topChild = child;
                        topChildIndex = i;
                    }
                }

                // In case no child needs to be extracted earlier than the current node,
                // there is nothing more to do - the right spot was found.
                if (Comparer.Compare(node.priority, topChild.Priority) <= 0)
                {
                    break;
                }

                // Move the top child up by one node and now investigate the
                // node that was considered to be the top child (recursive).
                _nodes[nodeIndex] = topChild;
                nodeIndex = topChildIndex;
            }

            _nodes[nodeIndex] = node;
        }

        public partial class UnorderedItemsCollection : IReadOnlyCollection<(TElement element, TPriority priority)>, ICollection
        {
            private readonly PriorityQueue<TElement, TPriority> _queue;

            internal UnorderedItemsCollection(PriorityQueue<TElement, TPriority> queue)
            {
                _queue = queue;
            }

            public int Count => _queue._size;
            object ICollection.SyncRoot => this;
            bool ICollection.IsSynchronized => false;

            public void CopyTo(Array array, int index)
            {
                if (array == null)
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

            public struct Enumerator : IEnumerator<(TElement element, TPriority priority)>
            {
                private readonly PriorityQueue<TElement, TPriority> _queue;
                private readonly int _version;

                private int _index;
                private (TElement element, TPriority priority)? _currentElement;

                private const int FirstCallToEnumerator = -2;
                private const int EndOfEnumeration = -1;

                internal Enumerator(PriorityQueue<TElement, TPriority> queue)
                {
                    _queue = queue;
                    _version = queue._version;
                    _index = FirstCallToEnumerator;
                    _currentElement = default;
                }

                public void Dispose()
                {
                    _index = EndOfEnumeration;
                }

                public bool MoveNext()
                {
                    bool advancedEnumerator;

                    if (_version != _queue._version)
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                    }

                    if (_index == FirstCallToEnumerator)
                    {
                        _index = 0;
                        advancedEnumerator = (_queue._size > 0);

                        if (advancedEnumerator)
                        {
                            _currentElement = _queue._nodes[_index];
                        }
                        else
                        {
                            _index = EndOfEnumeration;
                        }

                        return advancedEnumerator;
                    }

                    if (_index == EndOfEnumeration)
                    {
                        return false;
                    }

                    _index++;
                    advancedEnumerator = (_index < _queue._size);

                    if (advancedEnumerator)
                    {
                        _currentElement = _queue._nodes[_index];
                    }
                    else
                    {
                        _currentElement = default;
                    }

                    return advancedEnumerator;
                }

                public (TElement element, TPriority priority) Current
                {
                    get
                    {
                        if (_index < 0)
                        {
                            ThrowEnumerationNotStartedOrEnded();
                        }
                        return _currentElement!.Value;
                    }
                }

                private void ThrowEnumerationNotStartedOrEnded()
                {
                    Debug.Assert(_index == FirstCallToEnumerator || _index == EndOfEnumeration);

                    string message = _index == FirstCallToEnumerator
                        ? SR.InvalidOperation_EnumNotStarted
                        : SR.InvalidOperation_EnumEnded;

                    throw new InvalidOperationException(message);
                }

                object IEnumerator.Current => Current;

                void IEnumerator.Reset()
                {
                    if (_version != _queue._version)
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                    }

                    _index = FirstCallToEnumerator;
                    _currentElement = default;
                }
            }

            public Enumerator GetEnumerator()
                => new Enumerator(_queue);

            IEnumerator<(TElement element, TPriority priority)> IEnumerable<(TElement element, TPriority priority)>.GetEnumerator()
                => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();
        }
    }
}
