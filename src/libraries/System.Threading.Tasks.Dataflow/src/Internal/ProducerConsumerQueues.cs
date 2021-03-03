// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// ProducerConsumerQueues.cs
//
//
// Specialized producer/consumer queues.
//
//
// ************<IMPORTANT NOTE>*************
//
// There are two exact copies of this file:
//  src\ndp\clr\src\bcl\system\threading\tasks\producerConsumerQueue.cs
//  src\ndp\fx\src\dataflow\system\threading\tasks\dataflow\internal\producerConsumerQueue.cs
// Keep both of them consistent by changing the other file when you change this one, also avoid:
//  1- To reference internal types in mscorlib
//  2- To reference any dataflow specific types
// This should be fixed post Dev11 when this class becomes public.
//
// ************</IMPORTANT NOTE>*************
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections;
#if USE_INTERNAL_CONCURRENT_COLLECTIONS
using System.Threading.Tasks.Dataflow.Internal.Collections;
#else
using System.Collections.Concurrent;
#endif
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Internal;

namespace System.Threading.Tasks
{
    /// <summary>Represents a producer/consumer queue used internally by dataflow blocks.</summary>
    /// <typeparam name="T">Specifies the type of data contained in the queue.</typeparam>
    internal interface IProducerConsumerQueue<T> : IEnumerable<T>
    {
        /// <summary>Enqueues an item into the queue.</summary>
        /// <param name="item">The item to enqueue.</param>
        /// <remarks>This method is meant to be thread-safe subject to the particular nature of the implementation.</remarks>
        void Enqueue(T item);

        /// <summary>Attempts to dequeue an item from the queue.</summary>
        /// <param name="result">The dequeued item.</param>
        /// <returns>true if an item could be dequeued; otherwise, false.</returns>
        /// <remarks>This method is meant to be thread-safe subject to the particular nature of the implementation.</remarks>
        bool TryDequeue([MaybeNullWhen(false)] out T result);

        /// <summary>Gets whether the collection is currently empty.</summary>
        /// <remarks>This method may or may not be thread-safe.</remarks>
        bool IsEmpty { get; }

        /// <summary>Gets the number of items in the collection.</summary>
        /// <remarks>In many implementations, this method will not be thread-safe.</remarks>
        int Count { get; }

        /// <summary>A thread-safe way to get the number of items in the collection. May synchronize access by locking the provided synchronization object.</summary>
        /// <param name="syncObj">The sync object used to lock</param>
        /// <returns>The collection count</returns>
        int GetCountSafe(object syncObj);
    }

    /// <summary>
    /// Provides a producer/consumer queue safe to be used by any number of producers and consumers concurrently.
    /// </summary>
    /// <typeparam name="T">Specifies the type of data contained in the queue.</typeparam>
    [DebuggerDisplay("Count = {Count}")]
    internal sealed class MultiProducerMultiConsumerQueue<T> : ConcurrentQueue<T>, IProducerConsumerQueue<T>
    {
        /// <summary>Enqueues an item into the queue.</summary>
        /// <param name="item">The item to enqueue.</param>
        void IProducerConsumerQueue<T>.Enqueue(T item) { base.Enqueue(item); }

        /// <summary>Attempts to dequeue an item from the queue.</summary>
        /// <param name="result">The dequeued item.</param>
        /// <returns>true if an item could be dequeued; otherwise, false.</returns>
        bool IProducerConsumerQueue<T>.TryDequeue([MaybeNullWhen(false)] out T result) { return base.TryDequeue(out result); }

        /// <summary>Gets whether the collection is currently empty.</summary>
        bool IProducerConsumerQueue<T>.IsEmpty { get { return base.IsEmpty; } }

        /// <summary>Gets the number of items in the collection.</summary>
        int IProducerConsumerQueue<T>.Count { get { return base.Count; } }

        /// <summary>A thread-safe way to get the number of items in the collection. May synchronize access by locking the provided synchronization object.</summary>
        /// <remarks>ConcurrentQueue.Count is thread safe, no need to acquire the lock.</remarks>
        int IProducerConsumerQueue<T>.GetCountSafe(object syncObj) { return base.Count; }
    }

    /// <summary>
    /// Provides a producer/consumer queue safe to be used by only one producer and one consumer concurrently.
    /// </summary>
    /// <typeparam name="T">Specifies the type of data contained in the queue.</typeparam>
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(SingleProducerSingleConsumerQueue<>.SingleProducerSingleConsumerQueue_DebugView))]
    internal sealed class SingleProducerSingleConsumerQueue<T> : IProducerConsumerQueue<T>
    {
        // Design:
        //
        // SingleProducerSingleConsumerQueue (SPSCQueue) is a concurrent queue designed to be used
        // by one producer thread and one consumer thread. SPSCQueue does not work correctly when used by
        // multiple producer threads concurrently or multiple consumer threads concurrently.
        //
        // SPSCQueue is based on segments that behave like circular buffers. Each circular buffer is represented
        // as an array with two indexes: _first and _last. _first is the index of the array slot for the consumer
        // to read next, and _last is the slot for the producer to write next. The circular buffer is empty when
        // (_first == _last), and full when ((_last+1) % _array.Length == _first).
        //
        // Since _first is only ever modified by the consumer thread and _last by the producer, the two indices can
        // be updated without interlocked operations. As long as the queue size fits inside a single circular buffer,
        // enqueues and dequeues simply advance the corresponding indices around the circular buffer. If an enqueue finds
        // that there is no room in the existing buffer, however, a new circular buffer is allocated that is twice as big
        // as the old buffer. From then on, the producer will insert values into the new buffer. The consumer will first
        // empty out the old buffer and only then follow the producer into the new (larger) buffer.
        //
        // As described above, the enqueue operation on the fast path only modifies the _first field of the current segment.
        // However, it also needs to read _last in order to verify that there is room in the current segment. Similarly, the
        // dequeue operation on the fast path only needs to modify _last, but also needs to read _first to verify that the
        // queue is non-empty. This results in true cache line sharing between the producer and the consumer.
        //
        // The cache line sharing issue can be mitigating by having a possibly stale copy of _first that is owned by the producer,
        // and a possibly stale copy of _last that is owned by the consumer. So, the consumer state is described using
        // (_first, _lastCopy) and the producer state using (_firstCopy, _last). The consumer state is separated from
        // the producer state by padding, which allows fast-path enqueues and dequeues from hitting shared cache lines.
        // _lastCopy is the consumer's copy of _last. Whenever the consumer can tell that there is room in the buffer
        // simply by observing _lastCopy, the consumer thread does not need to read _last and thus encounter a cache miss. Only
        // when the buffer appears to be empty will the consumer refresh _lastCopy from _last. _firstCopy is used by the producer
        // in the same way to avoid reading _first on the hot path.

        /// <summary>The initial size to use for segments (in number of elements).</summary>
        private const int INIT_SEGMENT_SIZE = 32; // must be a power of 2
        /// <summary>The maximum size to use for segments (in number of elements).</summary>
        private const int MAX_SEGMENT_SIZE = 0x1000000; // this could be made as large as Int32.MaxValue / 2

        /// <summary>The head of the linked list of segments.</summary>
        private volatile Segment _head;
        /// <summary>The tail of the linked list of segments.</summary>
        private volatile Segment _tail;

        /// <summary>Initializes the queue.</summary>
        internal SingleProducerSingleConsumerQueue()
        {
            // Validate constants in ctor rather than in an explicit cctor that would cause perf degradation
            Debug.Assert(INIT_SEGMENT_SIZE > 0, "Initial segment size must be > 0.");
            Debug.Assert((INIT_SEGMENT_SIZE & (INIT_SEGMENT_SIZE - 1)) == 0, "Initial segment size must be a power of 2");
            Debug.Assert(INIT_SEGMENT_SIZE <= MAX_SEGMENT_SIZE, "Initial segment size should be <= maximum.");
            Debug.Assert(MAX_SEGMENT_SIZE < int.MaxValue / 2, "Max segment size * 2 must be < Int32.MaxValue, or else overflow could occur.");

            // Initialize the queue
            _head = _tail = new Segment(INIT_SEGMENT_SIZE);
        }

        /// <summary>Enqueues an item into the queue.</summary>
        /// <param name="item">The item to enqueue.</param>
        public void Enqueue(T item)
        {
            Segment segment = _tail;
            T[] array = segment._array;
            int last = segment._state._last; // local copy to avoid multiple volatile reads

            // Fast path: there's obviously room in the current segment
            int tail2 = (last + 1) & (array.Length - 1);
            if (tail2 != segment._state._firstCopy)
            {
                array[last] = item;
                segment._state._last = tail2;
            }
            // Slow path: there may not be room in the current segment.
            else EnqueueSlow(item, ref segment);
        }

        /// <summary>Enqueues an item into the queue.</summary>
        /// <param name="item">The item to enqueue.</param>
        /// <param name="segment">The segment in which to first attempt to store the item.</param>
        private void EnqueueSlow(T item, ref Segment segment)
        {
            Debug.Assert(segment != null, "Expected a non-null segment.");

            if (segment._state._firstCopy != segment._state._first)
            {
                segment._state._firstCopy = segment._state._first;
                Enqueue(item); // will only recur once for this enqueue operation
                return;
            }

            int newSegmentSize = _tail._array.Length << 1; // double size
            Debug.Assert(newSegmentSize > 0, "The max size should always be small enough that we don't overflow.");
            if (newSegmentSize > MAX_SEGMENT_SIZE) newSegmentSize = MAX_SEGMENT_SIZE;

            var newSegment = new Segment(newSegmentSize);
            newSegment._array[0] = item;
            newSegment._state._last = 1;
            newSegment._state._lastCopy = 1;

            try { }
            finally
            {
                // Finally block to protect against corruption due to a thread abort
                // between setting _next and setting _tail.
                Volatile.Write(ref _tail._next, newSegment); // ensure segment not published until item is fully stored
                _tail = newSegment;
            }
        }

        /// <summary>Attempts to dequeue an item from the queue.</summary>
        /// <param name="result">The dequeued item.</param>
        /// <returns>true if an item could be dequeued; otherwise, false.</returns>
        public bool TryDequeue([MaybeNullWhen(false)] out T result)
        {
            Segment segment = _head;
            T[] array = segment._array;
            int first = segment._state._first; // local copy to avoid multiple volatile reads

            // Fast path: there's obviously data available in the current segment
            if (first != segment._state._lastCopy)
            {
                result = array[first];
                array[first] = default(T)!; // Clear the slot to release the element
                segment._state._first = (first + 1) & (array.Length - 1);
                return true;
            }
            // Slow path: there may not be data available in the current segment
            else return TryDequeueSlow(ref segment, ref array, out result);
        }

        /// <summary>Attempts to dequeue an item from the queue.</summary>
        /// <param name="array">The array from which the item was dequeued.</param>
        /// <param name="segment">The segment from which the item was dequeued.</param>
        /// <param name="result">The dequeued item.</param>
        /// <returns>true if an item could be dequeued; otherwise, false.</returns>
        private bool TryDequeueSlow(ref Segment segment, ref T[] array, [MaybeNullWhen(false)] out T result)
        {
            Debug.Assert(segment != null, "Expected a non-null segment.");
            Debug.Assert(array != null, "Expected a non-null item array.");

            if (segment._state._last != segment._state._lastCopy)
            {
                segment._state._lastCopy = segment._state._last;
                return TryDequeue(out result); // will only recur once for this dequeue operation
            }

            if (segment._next != null && segment._state._first == segment._state._last)
            {
                segment = segment._next;
                array = segment._array;
                _head = segment;
            }

            int first = segment._state._first; // local copy to avoid extraneous volatile reads

            if (first == segment._state._last)
            {
                result = default(T);
                return false;
            }

            result = array[first];
            array[first] = default(T)!; // Clear the slot to release the element
            segment._state._first = (first + 1) & (segment._array.Length - 1);
            segment._state._lastCopy = segment._state._last; // Refresh _lastCopy to ensure that _first has not passed _lastCopy

            return true;
        }

        /// <summary>Attempts to peek at an item in the queue.</summary>
        /// <param name="result">The peeked item.</param>
        /// <returns>true if an item could be peeked; otherwise, false.</returns>
        public bool TryPeek([MaybeNullWhen(false)] out T result)
        {
            Segment segment = _head;
            T[] array = segment._array;
            int first = segment._state._first; // local copy to avoid multiple volatile reads

            // Fast path: there's obviously data available in the current segment
            if (first != segment._state._lastCopy)
            {
                result = array[first];
                return true;
            }
            // Slow path: there may not be data available in the current segment
            else return TryPeekSlow(ref segment, ref array, out result);
        }

        /// <summary>Attempts to peek at an item in the queue.</summary>
        /// <param name="array">The array from which the item is peeked.</param>
        /// <param name="segment">The segment from which the item is peeked.</param>
        /// <param name="result">The peeked item.</param>
        /// <returns>true if an item could be peeked; otherwise, false.</returns>
        private bool TryPeekSlow(ref Segment segment, ref T[] array, [MaybeNullWhen(false)] out T result)
        {
            Debug.Assert(segment != null, "Expected a non-null segment.");
            Debug.Assert(array != null, "Expected a non-null item array.");

            if (segment._state._last != segment._state._lastCopy)
            {
                segment._state._lastCopy = segment._state._last;
                return TryPeek(out result); // will only recur once for this peek operation
            }

            if (segment._next != null && segment._state._first == segment._state._last)
            {
                segment = segment._next;
                array = segment._array;
                _head = segment;
            }

            int first = segment._state._first; // local copy to avoid extraneous volatile reads

            if (first == segment._state._last)
            {
                result = default(T);
                return false;
            }

            result = array[first];
            return true;
        }

        /// <summary>Attempts to dequeue an item from the queue.</summary>
        /// <param name="predicate">The predicate that must return true for the item to be dequeued.  If null, all items implicitly return true.</param>
        /// <param name="result">The dequeued item.</param>
        /// <returns>true if an item could be dequeued; otherwise, false.</returns>
        public bool TryDequeueIf(Predicate<T>? predicate, [MaybeNullWhen(false)] out T result)
        {
            Segment segment = _head;
            T[] array = segment._array;
            int first = segment._state._first; // local copy to avoid multiple volatile reads

            // Fast path: there's obviously data available in the current segment
            if (first != segment._state._lastCopy)
            {
                result = array[first];
                if (predicate == null || predicate(result))
                {
                    array[first] = default(T)!; // Clear the slot to release the element
                    segment._state._first = (first + 1) & (array.Length - 1);
                    return true;
                }
                else
                {
                    result = default(T);
                    return false;
                }
            }
            // Slow path: there may not be data available in the current segment
            else return TryDequeueIfSlow(predicate, ref segment, ref array, out result);
        }

        /// <summary>Attempts to dequeue an item from the queue.</summary>
        /// <param name="predicate">The predicate that must return true for the item to be dequeued.  If null, all items implicitly return true.</param>
        /// <param name="array">The array from which the item was dequeued.</param>
        /// <param name="segment">The segment from which the item was dequeued.</param>
        /// <param name="result">The dequeued item.</param>
        /// <returns>true if an item could be dequeued; otherwise, false.</returns>
        private bool TryDequeueIfSlow(Predicate<T>? predicate, ref Segment segment, ref T[] array, [MaybeNullWhen(false)] out T result)
        {
            Debug.Assert(segment != null, "Expected a non-null segment.");
            Debug.Assert(array != null, "Expected a non-null item array.");

            if (segment._state._last != segment._state._lastCopy)
            {
                segment._state._lastCopy = segment._state._last;
                return TryDequeueIf(predicate, out result); // will only recur once for this dequeue operation
            }

            if (segment._next != null && segment._state._first == segment._state._last)
            {
                segment = segment._next;
                array = segment._array;
                _head = segment;
            }

            int first = segment._state._first; // local copy to avoid extraneous volatile reads

            if (first == segment._state._last)
            {
                result = default(T);
                return false;
            }

            result = array[first];
            if (predicate == null || predicate(result))
            {
                array[first] = default(T)!; // Clear the slot to release the element
                segment._state._first = (first + 1) & (segment._array.Length - 1);
                segment._state._lastCopy = segment._state._last; // Refresh _lastCopy to ensure that _first has not passed _lastCopy
                return true;
            }
            else
            {
                result = default(T);
                return false;
            }
        }

        public void Clear()
        {
            while (TryDequeue(out _)) ;
        }

        /// <summary>Gets whether the collection is currently empty.</summary>
        /// <remarks>WARNING: This should not be used concurrently without further vetting.</remarks>
        public bool IsEmpty
        {
            // This implementation is optimized for calls from the consumer.
            get
            {
                Segment head = _head;
                if (head._state._first != head._state._lastCopy) return false; // _first is volatile, so the read of _lastCopy cannot get reordered
                if (head._state._first != head._state._last) return false;
                return head._next == null;
            }
        }

        /// <summary>Gets an enumerable for the collection.</summary>
        /// <remarks>WARNING: This should only be used for debugging purposes.  It is not safe to be used concurrently.</remarks>
        public IEnumerator<T> GetEnumerator()
        {
            for (Segment? segment = _head; segment != null; segment = segment._next)
            {
                for (int pt = segment._state._first;
                    pt != segment._state._last;
                    pt = (pt + 1) & (segment._array.Length - 1))
                {
                    yield return segment._array[pt];
                }
            }
        }
        /// <summary>Gets an enumerable for the collection.</summary>
        /// <remarks>WARNING: This should only be used for debugging purposes.  It is not safe to be used concurrently.</remarks>
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        /// <summary>Gets the number of items in the collection.</summary>
        /// <remarks>WARNING: This should only be used for debugging purposes.  It is not meant to be used concurrently.</remarks>
        public int Count
        {
            get
            {
                int count = 0;
                for (Segment? segment = _head; segment != null; segment = segment._next)
                {
                    int arraySize = segment._array.Length;
                    int first, last;
                    while (true) // Count is not meant to be used concurrently, but this helps to avoid issues if it is
                    {
                        first = segment._state._first;
                        last = segment._state._last;
                        if (first == segment._state._first) break;
                    }
                    count += (last - first) & (arraySize - 1);
                }
                return count;
            }
        }

        /// <summary>A thread-safe way to get the number of items in the collection. May synchronize access by locking the provided synchronization object.</summary>
        /// <remarks>The Count is not thread safe, so we need to acquire the lock.</remarks>
        int IProducerConsumerQueue<T>.GetCountSafe(object syncObj)
        {
            Debug.Assert(syncObj != null, "The syncObj parameter is null.");
            lock (syncObj)
            {
                return Count;
            }
        }

        /// <summary>A segment in the queue containing one or more items.</summary>
        [StructLayout(LayoutKind.Sequential)]
        private sealed class Segment
        {
            /// <summary>The next segment in the linked list of segments.</summary>
            internal Segment? _next;
            /// <summary>The data stored in this segment.</summary>
            internal readonly T[] _array;
            /// <summary>Details about the segment.</summary>
            internal SegmentState _state; // separated out to enable StructLayout attribute to take effect

            /// <summary>Initializes the segment.</summary>
            /// <param name="size">The size to use for this segment.</param>
            internal Segment(int size)
            {
                Debug.Assert((size & (size - 1)) == 0, "Size must be a power of 2");
                _array = new T[size];
            }
        }

        /// <summary>Stores information about a segment.</summary>
        [StructLayout(LayoutKind.Sequential)] // enforce layout so that padding reduces false sharing
        private struct SegmentState
        {
            /// <summary>Padding to reduce false sharing between the segment's array and _first.</summary>
            internal PaddingFor32 _pad0;

            /// <summary>The index of the current head in the segment.</summary>
            internal volatile int _first;
            /// <summary>A copy of the current tail index.</summary>
            internal int _lastCopy; // not volatile as read and written by the producer, except for IsEmpty, and there _lastCopy is only read after reading the volatile _first

            /// <summary>Padding to reduce false sharing between the first and last.</summary>
            internal PaddingFor32 _pad1;

            /// <summary>A copy of the current head index.</summary>
            internal int _firstCopy; // not volatile as only read and written by the consumer thread
            /// <summary>The index of the current tail in the segment.</summary>
            internal volatile int _last;

            /// <summary>Padding to reduce false sharing with the last and what's after the segment.</summary>
            internal PaddingFor32 _pad2;
        }

        /// <summary>Debugger type proxy for a SingleProducerSingleConsumerQueue of T.</summary>
        private sealed class SingleProducerSingleConsumerQueue_DebugView
        {
            /// <summary>The queue being visualized.</summary>
            private readonly SingleProducerSingleConsumerQueue<T> _queue;

            /// <summary>Initializes the debug view.</summary>
            /// <param name="queue">The queue being debugged.</param>
            public SingleProducerSingleConsumerQueue_DebugView(SingleProducerSingleConsumerQueue<T> queue)
            {
                Debug.Assert(queue != null, "Expected a non-null queue.");
                _queue = queue;
            }

            /// <summary>Gets the contents of the list.</summary>
            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public T[] Items
            {
                get
                {
                    List<T> list = new List<T>();
                    foreach (T item in _queue)
                        list.Add(item);
                    return list.ToArray();
                }
            }
        }
    }
}
