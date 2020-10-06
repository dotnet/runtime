// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
//
//
// Specialized producer/consumer queues.
//
//
// ************<IMPORTANT NOTE>*************
//
//  src\ndp\clr\src\bcl\system\threading\tasks\producerConsumerQueue.cs
//  src\ndp\fx\src\dataflow\system\threading\tasks\dataflow\internal\producerConsumerQueue.cs
// Keep both of them consistent by changing the other file when you change this one, also avoid:
//  1- To reference interneal types in mscorlib
//  2- To reference any dataflow specific types
// This should be fixed post Dev11 when this class becomes public.
//
// ************</IMPORTANT NOTE>*************
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

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
        bool IProducerConsumerQueue<T>.IsEmpty => base.IsEmpty;

        /// <summary>Gets the number of items in the collection.</summary>
        int IProducerConsumerQueue<T>.Count => base.Count;
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
        // as an array with two indexes: m_first and m_last. m_first is the index of the array slot for the consumer
        // to read next, and m_last is the slot for the producer to write next. The circular buffer is empty when
        // (m_first == m_last), and full when ((m_last+1) % m_array.Length == m_first).
        //
        // Since m_first is only ever modified by the consumer thread and m_last by the producer, the two indices can
        // be updated without interlocked operations. As long as the queue size fits inside a single circular buffer,
        // enqueues and dequeues simply advance the corresponding indices around the circular buffer. If an enqueue finds
        // that there is no room in the existing buffer, however, a new circular buffer is allocated that is twice as big
        // as the old buffer. From then on, the producer will insert values into the new buffer. The consumer will first
        // empty out the old buffer and only then follow the producer into the new (larger) buffer.
        //
        // As described above, the enqueue operation on the fast path only modifies the m_first field of the current segment.
        // However, it also needs to read m_last in order to verify that there is room in the current segment. Similarly, the
        // dequeue operation on the fast path only needs to modify m_last, but also needs to read m_first to verify that the
        // queue is non-empty. This results in true cache line sharing between the producer and the consumer.
        //
        // The cache line sharing issue can be mitigating by having a possibly stale copy of m_first that is owned by the producer,
        // and a possibly stale copy of m_last that is owned by the consumer. So, the consumer state is described using
        // (m_first, m_lastCopy) and the producer state using (m_firstCopy, m_last). The consumer state is separated from
        // the producer state by padding, which allows fast-path enqueues and dequeues from hitting shared cache lines.
        // m_lastCopy is the consumer's copy of m_last. Whenever the consumer can tell that there is room in the buffer
        // simply by observing m_lastCopy, the consumer thread does not need to read m_last and thus encounter a cache miss. Only
        // when the buffer appears to be empty will the consumer refresh m_lastCopy from m_last. m_firstCopy is used by the producer
        // in the same way to avoid reading m_first on the hot path.

        /// <summary>The initial size to use for segments (in number of elements).</summary>
        private const int INIT_SEGMENT_SIZE = 32; // must be a power of 2
        /// <summary>The maximum size to use for segments (in number of elements).</summary>
        private const int MAX_SEGMENT_SIZE = 0x1000000; // this could be made as large as int.MaxValue / 2

        /// <summary>The head of the linked list of segments.</summary>
        private volatile Segment m_head;
        /// <summary>The tail of the linked list of segments.</summary>
        private volatile Segment m_tail;

        /// <summary>Initializes the queue.</summary>
        internal SingleProducerSingleConsumerQueue()
        {
            // Validate constants in ctor rather than in an explicit cctor that would cause perf degradation
            Debug.Assert(INIT_SEGMENT_SIZE > 0, "Initial segment size must be > 0.");
            Debug.Assert((INIT_SEGMENT_SIZE & (INIT_SEGMENT_SIZE - 1)) == 0, "Initial segment size must be a power of 2");
            Debug.Assert(INIT_SEGMENT_SIZE <= MAX_SEGMENT_SIZE, "Initial segment size should be <= maximum.");
            Debug.Assert(MAX_SEGMENT_SIZE < int.MaxValue / 2, "Max segment size * 2 must be < int.MaxValue, or else overflow could occur.");

            // Initialize the queue
            m_head = m_tail = new Segment(INIT_SEGMENT_SIZE);
        }

        /// <summary>Enqueues an item into the queue.</summary>
        /// <param name="item">The item to enqueue.</param>
        public void Enqueue(T item)
        {
            Segment segment = m_tail;
            T[] array = segment.m_array;
            int last = segment.m_state.m_last; // local copy to avoid multiple volatile reads

            // Fast path: there's obviously room in the current segment
            int tail2 = (last + 1) & (array.Length - 1);
            if (tail2 != segment.m_state.m_firstCopy)
            {
                array[last] = item;
                segment.m_state.m_last = tail2;
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

            if (segment.m_state.m_firstCopy != segment.m_state.m_first)
            {
                segment.m_state.m_firstCopy = segment.m_state.m_first;
                Enqueue(item); // will only recur once for this enqueue operation
                return;
            }

            int newSegmentSize = m_tail.m_array.Length << 1; // double size
            Debug.Assert(newSegmentSize > 0, "The max size should always be small enough that we don't overflow.");
            if (newSegmentSize > MAX_SEGMENT_SIZE) newSegmentSize = MAX_SEGMENT_SIZE;

            var newSegment = new Segment(newSegmentSize);
            newSegment.m_array[0] = item;
            newSegment.m_state.m_last = 1;
            newSegment.m_state.m_lastCopy = 1;

            try { }
            finally
            {
                // Finally block to protect against corruption due to a thread abort
                // between setting m_next and setting m_tail.
                Volatile.Write(ref m_tail.m_next, newSegment); // ensure segment not published until item is fully stored
                m_tail = newSegment;
            }
        }

        /// <summary>Attempts to dequeue an item from the queue.</summary>
        /// <param name="result">The dequeued item.</param>
        /// <returns>true if an item could be dequeued; otherwise, false.</returns>
        public bool TryDequeue([MaybeNullWhen(false)] out T result)
        {
            Segment segment = m_head;
            T[] array = segment.m_array;
            int first = segment.m_state.m_first; // local copy to avoid multiple volatile reads

            // Fast path: there's obviously data available in the current segment
            if (first != segment.m_state.m_lastCopy)
            {
                result = array[first];
                array[first] = default!; // Clear the slot to release the element
                segment.m_state.m_first = (first + 1) & (array.Length - 1);
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

            if (segment.m_state.m_last != segment.m_state.m_lastCopy)
            {
                segment.m_state.m_lastCopy = segment.m_state.m_last;
                return TryDequeue(out result); // will only recur once for this dequeue operation
            }

            if (segment.m_next != null && segment.m_state.m_first == segment.m_state.m_last)
            {
                segment = segment.m_next;
                array = segment.m_array;
                m_head = segment;
            }

            int first = segment.m_state.m_first; // local copy to avoid extraneous volatile reads

            if (first == segment.m_state.m_last)
            {
                result = default;
                return false;
            }

            result = array[first];
            array[first] = default!; // Clear the slot to release the element
            segment.m_state.m_first = (first + 1) & (segment.m_array.Length - 1);
            segment.m_state.m_lastCopy = segment.m_state.m_last; // Refresh m_lastCopy to ensure that m_first has not passed m_lastCopy

            return true;
        }

        /// <summary>Gets whether the collection is currently empty.</summary>
        /// <remarks>WARNING: This should not be used concurrently without further vetting.</remarks>
        public bool IsEmpty
        {
            // This implementation is optimized for calls from the consumer.
            get
            {
                Segment head = m_head;
                if (head.m_state.m_first != head.m_state.m_lastCopy) return false; // m_first is volatile, so the read of m_lastCopy cannot get reordered
                if (head.m_state.m_first != head.m_state.m_last) return false;
                return head.m_next == null;
            }
        }

        /// <summary>Gets an enumerable for the collection.</summary>
        /// <remarks>WARNING: This should only be used for debugging purposes.  It is not safe to be used concurrently.</remarks>
        public IEnumerator<T> GetEnumerator()
        {
            for (Segment? segment = m_head; segment != null; segment = segment.m_next)
            {
                for (int pt = segment.m_state.m_first;
                    pt != segment.m_state.m_last;
                    pt = (pt + 1) & (segment.m_array.Length - 1))
                {
                    yield return segment.m_array[pt];
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
                for (Segment? segment = m_head; segment != null; segment = segment.m_next)
                {
                    int arraySize = segment.m_array.Length;
                    int first, last;
                    while (true) // Count is not meant to be used concurrently, but this helps to avoid issues if it is
                    {
                        first = segment.m_state.m_first;
                        last = segment.m_state.m_last;
                        if (first == segment.m_state.m_first) break;
                    }
                    count += (last - first) & (arraySize - 1);
                }
                return count;
            }
        }

        /// <summary>A segment in the queue containing one or more items.</summary>
        [StructLayout(LayoutKind.Sequential)]
        private sealed class Segment
        {
            /// <summary>The next segment in the linked list of segments.</summary>
            internal Segment? m_next;
            /// <summary>The data stored in this segment.</summary>
            internal readonly T[] m_array;
            /// <summary>Details about the segment.</summary>
            internal SegmentState m_state; // separated out to enable StructLayout attribute to take effect

            /// <summary>Initializes the segment.</summary>
            /// <param name="size">The size to use for this segment.</param>
            internal Segment(int size)
            {
                Debug.Assert((size & (size - 1)) == 0, "Size must be a power of 2");
                m_array = new T[size];
            }
        }

        /// <summary>Stores information about a segment.</summary>
        [StructLayout(LayoutKind.Sequential)] // enforce layout so that padding reduces false sharing
        private struct SegmentState
        {
            /// <summary>Padding to reduce false sharing between the segment's array and m_first.</summary>
            internal Internal.PaddingFor32 m_pad0;

            /// <summary>The index of the current head in the segment.</summary>
            internal volatile int m_first;
            /// <summary>A copy of the current tail index.</summary>
            internal int m_lastCopy; // not volatile as read and written by the producer, except for IsEmpty, and there m_lastCopy is only read after reading the volatile m_first

            /// <summary>Padding to reduce false sharing between the first and last.</summary>
            internal Internal.PaddingFor32 m_pad1;

            /// <summary>A copy of the current head index.</summary>
            internal int m_firstCopy; // not voliatle as only read and written by the consumer thread
            /// <summary>The index of the current tail in the segment.</summary>
            internal volatile int m_last;

            /// <summary>Padding to reduce false sharing with the last and what's after the segment.</summary>
            internal Internal.PaddingFor32 m_pad2;
        }

        /// <summary>Debugger type proxy for a SingleProducerSingleConsumerQueue of T.</summary>
        private sealed class SingleProducerSingleConsumerQueue_DebugView
        {
            /// <summary>The queue being visualized.</summary>
            private readonly SingleProducerSingleConsumerQueue<T> m_queue;

            /// <summary>Initializes the debug view.</summary>
            /// <param name="queue">The queue being debugged.</param>
            public SingleProducerSingleConsumerQueue_DebugView(SingleProducerSingleConsumerQueue<T> queue)
            {
                Debug.Assert(queue != null, "Expected a non-null queue.");
                m_queue = queue;
            }
        }
    }
}
