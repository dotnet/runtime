// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Linq;

namespace System.Collections.Concurrent
{
    /// <summary>
    /// Represents a thread-safe priority queue that supports multiple dequeue strategies for concurrent scenarios.
    /// This implementation uses separate concurrent queues for each priority level to maximize throughput.
    /// </summary>
    /// <typeparam name="TElement">The type of elements stored in the queue.</typeparam>
    /// <typeparam name="TPriority">The type used to define priorities. Must implement IComparable{TPriority}.</typeparam>
    /// <remarks>
    /// <para>
    /// This queue provides two dequeue strategies:
    /// 1. Strict Priority (TryDequeueStrict): Guarantees higher priority items are processed first
    /// 2. Fast Priority (TryDequeueFast): Maximizes throughput at the cost of strict ordering
    /// </para>
    /// <para>
    /// The implementation uses a combination of SortedDictionary and ConcurrentQueue to provide
    /// thread-safe operations with minimal locking. Each priority level maintains its own
    /// ConcurrentQueue, allowing for maximum parallelism during enqueue operations.
    /// </para>
    /// <para>
    /// Guidelines for choosing dequeue strategy:
    /// - Use TryDequeueStrict when strict priority ordering is required
    /// - Use TryDequeueFast when maximum throughput is needed
    /// - TryDequeueStrict has higher contention due to locking
    /// - TryDequeueFast may process lower priority items before higher ones
    /// </para>
    /// <remarks>
    /// Thread-safety guarantees:
    /// - Multiple threads can safely enqueue concurrently
    /// - TryDequeueStrict provides strict ordering but may block other dequeue operations
    /// - TryDequeueFast provides maximum concurrency but may not maintain strict priority order
    /// - Enumeration provides a snapshot of the queue at a point in time
    /// </remarks>
    /// </remarks>
    /// <example>
    /// <code>
    /// var queue = new ConcurrentPriorityQueue<string, int>(new[] { 1, 2, 3 });
    /// 
    /// // Producer
    /// queue.TryAdd("High Priority", 1);
    /// queue.TryAdd("Low Priority", 3);
    /// 
    /// // Consumer (Strict Priority)
    /// if (queue.TryDequeueStrict(out var item))
    ///     Console.WriteLine(item); // Prints "High Priority"
    /// 
    /// // Consumer (Fast Priority)
    /// if (queue.TryDequeueFast(out var item))
    ///     Console.WriteLine(item); // May print either item
    /// </code>
    /// </example>
    /// <summary>
    /// Performance Characteristics:
    /// - Enqueue: O(1)
    /// - TryDequeueStrict: O(p) where p is number of priority levels
    /// - TryDequeueFast: O(p) best case, where p is number of priority levels
    /// - Count: O(p) where p is number of priority levels
    /// - IsEmpty: O(p) where p is number of priority levels
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
    public class ConcurrentPriorityQueue<TElement, TPriority> : IProducerConsumerCollection<TElement>, IEnumerable<TElement>
        where TPriority : IComparable<TPriority>
    {
        private readonly SortedDictionary<TPriority, ConcurrentQueue<TElement>> _queues;
        private readonly object _syncLock;
        private readonly ConcurrentQueue<TElement>[] _queueArray;

        /// <summary>
        /// Initializes a new instance of the ConcurrentPriorityQueue class with specified priority levels.
        /// </summary>
        /// <param name="priorities">Collection of priority levels to initialize the queue with.</param>
        /// <param name="comparer">Optional custom comparer for priority ordering.</param>
        /// <exception cref="ArgumentNullException">Thrown when priorities is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when priorities is empty or contains duplicates.</exception>
        public ConcurrentPriorityQueue(IEnumerable<TPriority> priorities, IComparer<TPriority> comparer = null)
        {
            if (priorities == null) throw new ArgumentNullException(nameof(priorities));
            
            var priorityList = priorities.ToList();
            if (priorityList.Count == 0)
                throw new ArgumentException("At least one priority level must be specified", nameof(priorities));
            
            if (priorityList.Count != priorityList.Distinct().Count())
                throw new ArgumentException("Duplicate priorities are not allowed", nameof(priorities));

            _syncLock = new object();
            _queues = new SortedDictionary<TPriority, ConcurrentQueue<TElement>>(comparer);
            
            foreach (var priority in priorityList)
            {
                _queues[priority] = new ConcurrentQueue<TElement>();
            }
            _queueArray = _queues.Values.ToArray();
        }

        /// <summary>
        /// Gets the total number of elements contained in the queue across all priority levels.
        /// </summary>
        /// <remarks>
        /// The count is calculated by summing the counts of all internal queues.
        /// This operation is not atomic and the value may change as items are added or removed.
        /// </remarks>
        public int Count
        {
            get
            {
                var count = 0;
                var queues = _queueArray; // Get local copy to prevent torn reads
                for (int i = 0; i < queues.Length; i++)
                {
                    count += queues[i].Count;
                }
                return count;
            }
        }

        /// <summary>
        /// Gets a value that indicates whether the queue is empty.
        /// </summary>
        /// <remarks>
        /// This operation is not atomic and the value may change as items are added or removed.
        /// </remarks>
        public bool IsEmpty
        {
            get
            {
                var queues = _queueArray; // Get local copy to prevent torn reads
                for (int i = 0; i < queues.Length; i++)
                {
                    if (!queues[i].IsEmpty)
                        return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Adds an element to the queue with the specified priority.
        /// </summary>
        /// <param name="item">The element to add to the queue.</param>
        /// <param name="priority">The priority level for the element.</param>
        /// <exception cref="InvalidOperationException">Thrown when the specified priority level has not been initialized.</exception>
        public void Enqueue(TElement item, TPriority priority)
        {
            if (!_queues.TryGetValue(priority, out var queue))
            {
                throw new InvalidOperationException($"Priority {priority} not initialized.");
            }
            queue.Enqueue(item);
        }

        /// <summary>
        /// Attempts to add an element to the queue with the specified priority.
        /// </summary>
        /// <param name="item">The element to add to the queue.</param>
        /// <param name="priority">The priority level for the element.</param>
        /// <returns>true if the element was added successfully; otherwise, false.</returns>
        public bool TryAdd(TElement item, TPriority priority)
        {
            if (!_queues.TryGetValue(priority, out var queue))
            {
                return false;
            }
            queue.Enqueue(item);
            return true;
        }

        /// <summary>
        /// Attempts to remove and return an element from the queue, ensuring strict priority ordering.
        /// </summary>
        /// <param name="result">
        /// When this method returns, contains the element removed from the queue, if the operation was successful;
        /// otherwise, the default value for the type TElement.
        /// </param>
        /// <returns>true if an element was removed successfully; otherwise, false.</returns>
        public bool TryDequeueStrict(out TElement result)
        {
            lock (_syncLock)
            {
                foreach (var queue in _queues.Values)
                {
                    if (queue.TryDequeue(out result))
                    {
                        return true;
                    }
                }
                result = default;
                return false;
            }
        }

        /// <summary>
        /// Attempts to remove and return an element from the queue with maximum throughput.
        /// </summary>
        /// <param name="result">
        /// When this method returns, contains the element removed from the queue, if the operation was successful;
        /// otherwise, the default value for the type TElement.
        /// </param>
        /// <returns>true if an element was removed successfully; otherwise, false.</returns>
        public bool TryDequeueFast(out TElement result)
        {
            var queues = _queueArray; // Get local copy to prevent torn reads
            for (int i = 0; i < queues.Length; i++)
            {
                if (queues[i].TryDequeue(out result))
                {
                    return true;
                }
            }
            result = default;
            return false;
        }

        /// <summary>
        /// Attempts to return an element from the queue without removing it.
        /// </summary>
        /// <param name="result">
        /// When this method returns, contains the element at the head of the queue, if the operation was successful;
        /// otherwise, the default value for the type TElement.
        /// </param>
        /// <returns>true if an element was found; otherwise, false.</returns>
        public bool TryPeek(out TElement result)
        {
            foreach (var queue in _queues.Values)
            {
                if (queue.TryPeek(out result))
                {
                    return true;
                }
            }
            result = default;
            return false;
        }

        /// <summary>
        /// Attempts to return an element from a specific priority level without removing it.
        /// </summary>
        /// <param name="priority">The priority level to peek from.</param>
        /// <param name="result">
        /// When this method returns, contains the element at the head of the specified priority queue,
        /// if the operation was successful; otherwise, the default value for the type TElement.
        /// </param>
        /// <returns>true if an element was found at the specified priority level; otherwise, false.</returns>
        public bool TryPeek(TPriority priority, out TElement result)
        {
            if (_queues.TryGetValue(priority, out var queue))
            {
                return queue.TryPeek(out result);
            }
            result = default;
            return false;
        }

        /// <summary>
        /// Gets the number of elements at a specific priority level.
        /// </summary>
        /// <param name="priority">The priority level to check.</param>
        /// <returns>The number of elements at the specified priority level, or 0 if the priority doesn't exist.</returns>
        /// <remarks>
        /// This operation is O(1) as it directly accesses the specific priority queue.
        /// The count may change immediately after the method returns due to concurrent operations.
        /// </remarks>
        public int GetCount(TPriority priority)
        {
            return _queues.TryGetValue(priority, out var queue) ? queue.Count : 0;
        }

        /// <summary>
        /// Removes all elements from all priority queues in a thread-safe manner.
        /// </summary>
        /// <remarks>
        /// This operation acquires a lock to ensure thread safety while clearing all queues.
        /// Other threads attempting to access the queue during the clear operation will block
        /// until it completes.
        /// </remarks>
        public void Clear()
        {
            lock (_syncLock)
            {
                var queues = _queueArray; // Use cached array for better performance
                foreach (var queue in _queueArray)
                {
                    queue.Clear(); // Assuming ConcurrentQueue.Clear exists
                }
            }
        }

        /// <summary>
        /// Provides a debug view of the queue's current state.
        /// </summary>
        private sealed class DebugView
        {
            private readonly ConcurrentPriorityQueue<TElement, TPriority> _queue;

            public DebugView(ConcurrentPriorityQueue<TElement, TPriority> queue)
            {
                _queue = queue;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public KeyValuePair<TPriority, int>[] Items => _queue._queues
                .Select(kvp => new KeyValuePair<TPriority, int>(kvp.Key, kvp.Value.Count))
                .ToArray();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the queue.
        /// </summary>
        /// <returns>An enumerator for the queue.</returns>
        /// <remarks>
        /// The enumeration represents a moment-in-time snapshot of the contents of the queue.
        /// It does not reflect any updates to the collection after GetEnumerator was called.
        /// The enumerator is safe to use concurrently with reads from and writes to the queue.
        /// Elements are enumerated in priority order, from highest to lowest priority.
        /// </remarks>
        public IEnumerator<TElement> GetEnumerator()
        {
            // Take a complete snapshot before starting enumeration
            TElement[] snapshot;
            lock (_syncLock)
            {
                // Take snapshot of all items
                var items = new List<TElement>();
                foreach (var queue in _queues.Values)
                {
                    items.AddRange(queue.ToArray());
                }
                snapshot = items.ToArray();
            }

            // Return items from the snapshot
            for (int i = 0; i < snapshot.Length; i++)
            {
                yield return snapshot[i];
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the queue.
        /// </summary>
        /// <returns>An enumerator for the queue.</returns>
        /// <remarks>
        /// The enumeration represents a moment-in-time snapshot of the contents of the queue.
        /// See <see cref="GetEnumerator"/> for more details.
        /// </remarks>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Converts the queue to an array.
        /// </summary>
        /// <returns>An array containing all elements in the queue, ordered by priority from highest to lowest.</returns>
        /// <remarks>
        /// The array represents a moment-in-time snapshot of the contents of the queue.
        /// It does not reflect any updates to the collection after ToArray was called.
        /// </remarks>
        public TElement[] ToArray()
        {
            lock (_syncLock) // worth the consistency
            {
                var count = Count;
                if (count == 0) return Array.Empty<TElement>();
                
                var result = new List<TElement>(count);
                foreach (var queue in _queues.Values)
                {
                    result.AddRange(queue.ToArray());
                }
                return result.ToArray();
            }
        }

        /// <summary>
        /// Copies the elements of the queue to an array, starting at a particular array index.
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from the queue.</param>
        /// <param name="index">The zero-based index in array at which copying begins.</param>
        /// <remarks>
        /// Elements are copied in priority order, from highest to lowest priority.
        /// The operation represents a moment-in-time snapshot of the contents of the queue.
        /// This operation acquires a lock to ensure consistency of the snapshot.
        /// </remarks>
        /// <exception cref="ArgumentNullException">array is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">index is less than zero.</exception>
        /// <exception cref="ArgumentException">
        /// The number of elements in the source queue is greater than the available space from index 
        /// to the end of the destination array.
        /// </exception>
        public void CopyTo(TElement[] array, int index)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));

            lock (_syncLock)
            {
                var totalCount = Count;
                // Check array size before attempting to copy
                if (array.Length - index < totalCount)
                    throw new ArgumentException("The number of elements in the source is greater than the available space from index to the end of the destination array.", nameof(array));

                foreach (var queue in _queues.Values)
                {
                    foreach (var item in queue)
                    {
                        array[index++] = item;
                    }
                }
            }
        }

        // Add these methods to implement IProducerConsumerCollection<TElement>
        bool IProducerConsumerCollection<TElement>.TryAdd(TElement item)
        {
            // Use the lowest priority by default when adding through the interface
            var lowestPriority = _queues.Keys.Max();
            return TryAdd(item, lowestPriority);
        }

        bool IProducerConsumerCollection<TElement>.TryTake(out TElement item)
        {
            // Use strict dequeue for the interface implementation
            return TryDequeueStrict(out item);
        }

        // Add these properties and methods to implement ICollection
        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => _syncLock;

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));

            lock (_syncLock)
            {
                var totalCount = Count;
                // Check array size before attempting to copy
                if (array.Length - index < totalCount)
                    throw new ArgumentException("The number of elements in the source is greater than the available space from index to the end of the destination array.", nameof(array));

                if (array is TElement[] typedArray)
                {
                    CopyTo(typedArray, index);
                    return;
                }

                // Handle non-TElement[] arrays
                foreach (var queue in _queues.Values)
                {
                    foreach (var item in queue)
                    {
                        array.SetValue(item, index++);
                    }
                }
            }
        }
    }
}
