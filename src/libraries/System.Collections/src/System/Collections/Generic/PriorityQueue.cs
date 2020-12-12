// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Generic
{
    [System.Runtime.CompilerServices.TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class PriorityQueue<TElement, TPriority>
    {
        /// <summary>
        /// Creates an empty PriorityQueue instance.
        /// </summary>
        public PriorityQueue()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///   Creates a PriorityQueue instance with specified initial capacity in its backing array.
        /// </summary>
        public PriorityQueue(int initialCapacity)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///   Creates a PriorityQueue instance with specified priority comparer.
        /// </summary>
        public PriorityQueue(IComparer<TPriority>? comparer)
        {
            throw new NotImplementedException();
        }

        public PriorityQueue(int initialCapacity, IComparer<TPriority>? comparer)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///   Creates a PriorityQueue populated with the specified values and priorities.
        /// </summary>
        public PriorityQueue(IEnumerable<(TElement Element, TPriority Priority)> values)
        {
            throw new NotImplementedException();
        }

        public PriorityQueue(IEnumerable<(TElement Element, TPriority Priority)> values, IComparer<TPriority>? comparer)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///   Gets the current element count in the queue.
        /// </summary>
        public int Count => throw new NotImplementedException();

        /// <summary>
        ///   Gets the priority comparer of the queue.
        /// </summary>
        public IComparer<TPriority> Comparer => throw new NotImplementedException();

        /// <summary>
        ///   Enqueues the element with specified priority.
        /// </summary>
        public void Enqueue(TElement element, TPriority priority)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///   Gets the element with minimal priority, if it exists.
        /// </summary>
        /// <exception cref="InvalidOperationException">The queue is empty.</exception>
        public TElement Peek()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///    Dequeues the element with minimal priority, if it exists.
        /// </summary>
        /// <exception cref="InvalidOperationException">The queue is empty.</exception>
        public TElement Dequeue()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///    Try-variants of Dequeue and Peek methods.
        /// </summary>
        public bool TryDequeue([MaybeNullWhen(false)] out TElement element, [MaybeNullWhen(false)] out TPriority priority)
        {
            throw new NotImplementedException();
        }
        public bool TryPeek([MaybeNullWhen(false)] out TElement element, [MaybeNullWhen(false)] out TPriority priority)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///   Combined enqueue/dequeue operation, generally more efficient than sequential Enqueue/Dequeue calls.
        /// </summary>
        public TElement EnqueueDequeue(TElement element, TPriority priority)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///   Enqueues a sequence of element/priority pairs to the queue.
        /// </summary>
        public void EnqueueRange(IEnumerable<(TElement Element, TPriority Priority)> values)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///   Enqueues a sequence of elements with provided priority.
        /// </summary>
        public void EnqueueRange(IEnumerable<TElement> values, TPriority priority)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///   Removes all objects from the PriorityQueue.
        /// </summary>
        public void Clear()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///   Ensures that the PriorityQueue can hold the specified capacity and resizes its underlying buffer if necessary.
        /// </summary>
        public void EnsureCapacity(int capacity)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///   Sets capacity to the actual number of elements in the queue, if that is less than 90 percent of current capacity.
        /// </summary>
        public void TrimExcess()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///   Gets a collection that enumerates the elements of the queue.
        /// </summary>
        public UnorderedItemsCollection UnorderedItems { get; }

        public class UnorderedItemsCollection : IReadOnlyCollection<(TElement Element, TPriority Priority)>, ICollection
        {
            public int Count => throw new NotImplementedException();
            object ICollection.SyncRoot => throw new NotImplementedException();
            bool ICollection.IsSynchronized => throw new NotImplementedException();

            public void CopyTo(Array array, int index) => throw new NotImplementedException();

            public struct Enumerator : IEnumerator<(TElement TElement, TPriority Priority)>, IEnumerator
            {
                (TElement TElement, TPriority Priority) IEnumerator<(TElement TElement, TPriority Priority)>.Current => throw new NotImplementedException();
                object IEnumerator.Current => throw new NotImplementedException();

                void IDisposable.Dispose() => throw new NotImplementedException();
                bool IEnumerator.MoveNext() => throw new NotImplementedException();
                void IEnumerator.Reset() => throw new NotImplementedException();
            }

            public Enumerator GetEnumerator() => throw new NotImplementedException();
            IEnumerator<(TElement Element, TPriority Priority)> IEnumerable<(TElement Element, TPriority Priority)>.GetEnumerator() => throw new NotImplementedException();
            IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
        }
    }
}
