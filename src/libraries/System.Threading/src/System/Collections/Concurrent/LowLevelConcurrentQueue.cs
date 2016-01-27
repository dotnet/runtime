// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#pragma warning disable 0420

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// A lock-free, concurrent queue primitive, and its associated debugger view type.
//
// This is a stripped-down version of ConcurrentQueue, for use from within the System.Threading
// surface to eliminate a dependency on System.Collections.Concurrent.
// Please try to keep this in sync with the public ConcurrentQueue implementation.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;

namespace System.Collections.Concurrent
{
    /// <summary>
    /// Represents a thread-safe first-in, first-out collection of objects.
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements in the queue.</typeparam>
    /// <remarks>
    /// All public  and protected members of <see cref="ConcurrentQueue{T}"/> are thread-safe and may be used
    /// concurrently from multiple threads.
    /// </remarks>
    internal class LowLevelConcurrentQueue<T> /*: IProducerConsumerCollection<T>*/ : IEnumerable<T>
    {
        //fields of ConcurrentQueue
        private volatile Segment _head;

        private volatile Segment _tail;

        private const int SEGMENT_SIZE = 32;

        //number of snapshot takers, GetEnumerator(), ToList() and ToArray() operations take snapshot.
        internal volatile int m_numSnapshotTakers = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentQueue{T}"/> class.
        /// </summary>
        public LowLevelConcurrentQueue()
        {
            _head = _tail = new Segment(0, this);
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator"/> that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }

        /// <summary>
        /// Gets a value that indicates whether the <see cref="ConcurrentQueue{T}"/> is empty.
        /// </summary>
        /// <value>true if the <see cref="ConcurrentQueue{T}"/> is empty; otherwise, false.</value>
        /// <remarks>
        /// For determining whether the collection contains any items, use of this property is recommended
        /// rather than retrieving the number of items from the <see cref="Count"/> property and comparing it
        /// to 0.  However, as this collection is intended to be accessed concurrently, it may be the case
        /// that another thread will modify the collection after <see cref="IsEmpty"/> returns, thus invalidating
        /// the result.
        /// </remarks>
        public bool IsEmpty
        {
            get
            {
                Segment head = _head;
                if (!head.IsEmpty)
                    //fast route 1:
                    //if current head is not empty, then queue is not empty
                    return false;
                else if (head.Next == null)
                    //fast route 2:
                    //if current head is empty and it's the last segment
                    //then queue is empty
                    return true;
                else
                //slow route:
                //current head is empty and it is NOT the last segment,
                //it means another thread is growing new segment 
                {
                    SpinWait spin = new SpinWait();
                    while (head.IsEmpty)
                    {
                        if (head.Next == null)
                            return true;

                        spin.SpinOnce();
                        head = _head;
                    }
                    return false;
                }
            }
        }

        /// <summary>
        /// Store the position of the current head and tail positions.
        /// </summary>
        /// <param name="head">return the head segment</param>
        /// <param name="tail">return the tail segment</param>
        /// <param name="headLow">return the head offset, value range [0, SEGMENT_SIZE]</param>
        /// <param name="tailHigh">return the tail offset, value range [-1, SEGMENT_SIZE-1]</param>
        private void GetHeadTailPositions(out Segment head, out Segment tail,
            out int headLow, out int tailHigh)
        {
            head = _head;
            tail = _tail;
            headLow = head.Low;
            tailHigh = tail.High;
            SpinWait spin = new SpinWait();

            //we loop until the observed values are stable and sensible.  
            //This ensures that any update order by other methods can be tolerated.
            while (
                //if head and tail changed, retry
                head != _head || tail != _tail
                //if low and high pointers, retry
                || headLow != head.Low || tailHigh != tail.High
                //if head jumps ahead of tail because of concurrent grow and dequeue, retry
                || head.m_index > tail.m_index)
            {
                spin.SpinOnce();
                head = _head;
                tail = _tail;
                headLow = head.Low;
                tailHigh = tail.High;
            }
        }


        /// <summary>
        /// Gets the number of elements contained in the <see cref="ConcurrentQueue{T}"/>.
        /// </summary>
        /// <value>The number of elements contained in the <see cref="ConcurrentQueue{T}"/>.</value>
        /// <remarks>
        /// For determining whether the collection contains any items, use of the <see cref="IsEmpty"/>
        /// property is recommended rather than retrieving the number of items from the <see cref="Count"/>
        /// property and comparing it to 0.
        /// </remarks>
        public int Count
        {
            get
            {
                //store head and tail positions in buffer, 
                Segment head, tail;
                int headLow, tailHigh;
                GetHeadTailPositions(out head, out tail, out headLow, out tailHigh);

                if (head == tail)
                {
                    return tailHigh - headLow + 1;
                }

                //head segment
                int count = SEGMENT_SIZE - headLow;

                //middle segment(s), if any, are full.
                //We don't deal with overflow to be consistent with the behavior of generic types in CLR.
                count += SEGMENT_SIZE * ((int)(tail.m_index - head.m_index - 1));

                //tail segment
                count += tailHigh + 1;

                return count;
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see
        /// cref="ConcurrentQueue{T}"/>.
        /// </summary>
        /// <returns>An enumerator for the contents of the <see
        /// cref="ConcurrentQueue{T}"/>.</returns>
        /// <remarks>
        /// The enumeration represents a moment-in-time snapshot of the contents
        /// of the queue.  It does not reflect any updates to the collection after 
        /// <see cref="GetEnumerator"/> was called.  The enumerator is safe to use
        /// concurrently with reads from and writes to the queue.
        /// </remarks>
        public IEnumerator<T> GetEnumerator()
        {
            // Increments the number of active snapshot takers. This increment must happen before the snapshot is 
            // taken. At the same time, Decrement must happen after the enumeration is over. Only in this way, can it
            // eliminate race condition when Segment.TryRemove() checks whether m_numSnapshotTakers == 0. 
            Interlocked.Increment(ref m_numSnapshotTakers);

            // Takes a snapshot of the queue. 
            // A design flaw here: if a Thread.Abort() happens, we cannot decrement m_numSnapshotTakers. But we cannot 
            // wrap the following with a try/finally block, otherwise the decrement will happen before the yield return 
            // statements in the GetEnumerator (head, tail, headLow, tailHigh) method.           
            Segment head, tail;
            int headLow, tailHigh;
            GetHeadTailPositions(out head, out tail, out headLow, out tailHigh);

            //If we put yield-return here, the iterator will be lazily evaluated. As a result a snapshot of
            // the queue is not taken when GetEnumerator is initialized but when MoveNext() is first called.
            // This is inconsistent with existing generic collections. In order to prevent it, we capture the 
            // value of m_head in a buffer and call out to a helper method.
            //The old way of doing this was to return the ToList().GetEnumerator(), but ToList() was an 
            // unnecessary perfomance hit.
            return GetEnumerator(head, tail, headLow, tailHigh);
        }

        /// <summary>
        /// Helper method of GetEnumerator to seperate out yield return statement, and prevent lazy evaluation. 
        /// </summary>
        private IEnumerator<T> GetEnumerator(Segment head, Segment tail, int headLow, int tailHigh)
        {
            try
            {
                SpinWait spin = new SpinWait();

                if (head == tail)
                {
                    for (int i = headLow; i <= tailHigh; i++)
                    {
                        // If the position is reserved by an Enqueue operation, but the value is not written into,
                        // spin until the value is available.
                        spin.Reset();
                        while (!head.m_state[i].m_value)
                        {
                            spin.SpinOnce();
                        }
                        yield return head.m_array[i];
                    }
                }
                else
                {
                    //iterate on head segment
                    for (int i = headLow; i < SEGMENT_SIZE; i++)
                    {
                        // If the position is reserved by an Enqueue operation, but the value is not written into,
                        // spin until the value is available.
                        spin.Reset();
                        while (!head.m_state[i].m_value)
                        {
                            spin.SpinOnce();
                        }
                        yield return head.m_array[i];
                    }
                    //iterate on middle segments
                    Segment curr = head.Next;
                    while (curr != tail)
                    {
                        for (int i = 0; i < SEGMENT_SIZE; i++)
                        {
                            // If the position is reserved by an Enqueue operation, but the value is not written into,
                            // spin until the value is available.
                            spin.Reset();
                            while (!curr.m_state[i].m_value)
                            {
                                spin.SpinOnce();
                            }
                            yield return curr.m_array[i];
                        }
                        curr = curr.Next;
                    }

                    //iterate on tail segment
                    for (int i = 0; i <= tailHigh; i++)
                    {
                        // If the position is reserved by an Enqueue operation, but the value is not written into,
                        // spin until the value is available.
                        spin.Reset();
                        while (!tail.m_state[i].m_value)
                        {
                            spin.SpinOnce();
                        }
                        yield return tail.m_array[i];
                    }
                }
            }
            finally
            {
                // This Decrement must happen after the enumeration is over. 
                Interlocked.Decrement(ref m_numSnapshotTakers);
            }
        }

        /// <summary>
        /// Adds an object to the end of the <see cref="ConcurrentQueue{T}"/>.
        /// </summary>
        /// <param name="item">The object to add to the end of the <see
        /// cref="ConcurrentQueue{T}"/>. The value can be a null reference
        /// (Nothing in Visual Basic) for reference types.
        /// </param>
        public void Enqueue(T item)
        {
            SpinWait spin = new SpinWait();
            while (true)
            {
                Segment tail = _tail;
                if (tail.TryAppend(item))
                    return;
                spin.SpinOnce();
            }
        }


        /// <summary>
        /// Attempts to remove and return the object at the beginning of the <see
        /// cref="ConcurrentQueue{T}"/>.
        /// </summary>
        /// <param name="result">
        /// When this method returns, if the operation was successful, <paramref name="result"/> contains the
        /// object removed. If no object was available to be removed, the value is unspecified.
        /// </param>
        /// <returns>true if an element was removed and returned from the beggining of the <see
        /// cref="ConcurrentQueue{T}"/>
        /// succesfully; otherwise, false.</returns>
        public bool TryDequeue(out T result)
        {
            while (!IsEmpty)
            {
                Segment head = _head;
                if (head.TryRemove(out result))
                    return true;
                //since method IsEmpty spins, we don't need to spin in the while loop
            }
            result = default(T);
            return false;
        }

        /// <summary>
        /// private class for ConcurrentQueue. 
        /// a queue is a linked list of small arrays, each node is called a segment.
        /// A segment contains an array, a pointer to the next segment, and m_low, m_high indices recording
        /// the first and last valid elements of the array.
        /// </summary>
        private class Segment
        {
            //we define two volatile arrays: m_array and m_state. Note that the accesses to the array items 
            //do not get volatile treatment. But we don't need to worry about loading adjacent elements or 
            //store/load on adjacent elements would suffer reordering. 
            // - Two stores:  these are at risk, but CLRv2 memory model guarantees store-release hence we are safe.
            // - Two loads: because one item from two volatile arrays are accessed, the loads of the array references
            //          are sufficient to prevent reordering of the loads of the elements.
            internal volatile T[] m_array;

            // For each entry in m_array, the corresponding entry in m_state indicates whether this position contains 
            // a valid value. m_state is initially all false. 
            internal volatile VolatileBool[] m_state;

            //pointer to the next segment. null if the current segment is the last segment
            private volatile Segment _next;

            //We use this zero based index to track how many segments have been created for the queue, and
            //to compute how many active segments are there currently. 
            // * The number of currently active segments is : m_tail.m_index - m_head.m_index + 1;
            // * m_index is incremented with every Segment.Grow operation. We use Int64 type, and we can safely 
            //   assume that it never overflows. To overflow, we need to do 2^63 increments, even at a rate of 4 
            //   billion (2^32) increments per second, it takes 2^31 seconds, which is about 64 years.
            internal readonly long m_index;

            //indices of where the first and last valid values
            // - m_low points to the position of the next element to pop from this segment, range [0, infinity)
            //      m_low >= SEGMENT_SIZE implies the segment is disposable
            // - m_high points to the position of the latest pushed element, range [-1, infinity)
            //      m_high == -1 implies the segment is new and empty
            //      m_high >= SEGMENT_SIZE-1 means this segment is ready to grow. 
            //        and the thread who sets m_high to SEGMENT_SIZE-1 is responsible to grow the segment
            // - Math.Min(m_low, SEGMENT_SIZE) > Math.Min(m_high, SEGMENT_SIZE-1) implies segment is empty
            // - initially m_low =0 and m_high=-1;
            private volatile int _low;
            private volatile int _high;

            private volatile LowLevelConcurrentQueue<T> _source;

            /// <summary>
            /// Create and initialize a segment with the specified index.
            /// </summary>
            internal Segment(long index, LowLevelConcurrentQueue<T> source)
            {
                m_array = new T[SEGMENT_SIZE];
                m_state = new VolatileBool[SEGMENT_SIZE]; //all initialized to false
                _high = -1;
                Debug.Assert(index >= 0);
                m_index = index;
                _source = source;
            }

            /// <summary>
            /// return the next segment
            /// </summary>
            internal Segment Next
            {
                get { return _next; }
            }


            /// <summary>
            /// return true if the current segment is empty (doesn't have any element available to dequeue, 
            /// false otherwise
            /// </summary>
            internal bool IsEmpty
            {
                get { return (Low > High); }
            }

            /// <summary>
            /// Add an element to the tail of the current segment
            /// exclusively called by ConcurrentQueue.InitializedFromCollection
            /// InitializeFromCollection is responsible to guaratee that there is no index overflow,
            /// and there is no contention
            /// </summary>
            /// <param name="value"></param>
            internal void UnsafeAdd(T value)
            {
                Debug.Assert(_high < SEGMENT_SIZE - 1);
                _high++;
                m_array[_high] = value;
                m_state[_high].m_value = true;
            }

            /// <summary>
            /// Create a new segment and append to the current one
            /// Does not update the m_tail pointer
            /// exclusively called by ConcurrentQueue.InitializedFromCollection
            /// InitializeFromCollection is responsible to guaratee that there is no index overflow,
            /// and there is no contention
            /// </summary>
            /// <returns>the reference to the new Segment</returns>
            internal Segment UnsafeGrow()
            {
                Debug.Assert(_high >= SEGMENT_SIZE - 1);
                Segment newSegment = new Segment(m_index + 1, _source); //m_index is Int64, we don't need to worry about overflow
                _next = newSegment;
                return newSegment;
            }

            /// <summary>
            /// Create a new segment and append to the current one
            /// Update the m_tail pointer
            /// This method is called when there is no contention
            /// </summary>
            internal void Grow()
            {
                //no CAS is needed, since there is no contention (other threads are blocked, busy waiting)
                Segment newSegment = new Segment(m_index + 1, _source);  //m_index is Int64, we don't need to worry about overflow
                _next = newSegment;
                Debug.Assert(_source._tail == this);
                _source._tail = _next;
            }


            /// <summary>
            /// Try to append an element at the end of this segment.
            /// </summary>
            /// <param name="value">the element to append</param>
            /// <param name="tail">The tail.</param>
            /// <returns>true if the element is appended, false if the current segment is full</returns>
            /// <remarks>if appending the specified element succeeds, and after which the segment is full, 
            /// then grow the segment</remarks>
            internal bool TryAppend(T value)
            {
                //quickly check if m_high is already over the boundary, if so, bail out
                if (_high >= SEGMENT_SIZE - 1)
                {
                    return false;
                }

                //Now we will use a CAS to increment m_high, and store the result in newhigh.
                //Depending on how many free spots left in this segment and how many threads are doing this Increment
                //at this time, the returning "newhigh" can be 
                // 1) < SEGMENT_SIZE - 1 : we took a spot in this segment, and not the last one, just insert the value
                // 2) == SEGMENT_SIZE - 1 : we took the last spot, insert the value AND grow the segment
                // 3) > SEGMENT_SIZE - 1 : we failed to reserve a spot in this segment, we return false to 
                //    Queue.Enqueue method, telling it to try again in the next segment.

                int newhigh = SEGMENT_SIZE; //initial value set to be over the boundary

                //We need do Interlocked.Increment and value/state update in a finally block to ensure that they run
                //without interuption. This is to prevent anything from happening between them, and another dequeue
                //thread maybe spinning forever to wait for m_state[] to be true;
                try
                { }
                finally
                {
                    newhigh = Interlocked.Increment(ref _high);
                    if (newhigh <= SEGMENT_SIZE - 1)
                    {
                        m_array[newhigh] = value;
                        m_state[newhigh].m_value = true;
                    }

                    //if this thread takes up the last slot in the segment, then this thread is responsible
                    //to grow a new segment. Calling Grow must be in the finally block too for reliability reason:
                    //if thread abort during Grow, other threads will be left busy spinning forever.
                    if (newhigh == SEGMENT_SIZE - 1)
                    {
                        Grow();
                    }
                }

                //if newhigh <= SEGMENT_SIZE-1, it means the current thread successfully takes up a spot
                return newhigh <= SEGMENT_SIZE - 1;
            }


            /// <summary>
            /// try to remove an element from the head of current segment
            /// </summary>
            /// <param name="result">The result.</param>
            /// <param name="head">The head.</param>
            /// <returns>return false only if the current segment is empty</returns>
            internal bool TryRemove(out T result)
            {
                SpinWait spin = new SpinWait();
                int lowLocal = Low, highLocal = High;
                while (lowLocal <= highLocal)
                {
                    //try to update m_low
                    if (Interlocked.CompareExchange(ref _low, lowLocal + 1, lowLocal) == lowLocal)
                    {
                        //if the specified value is not available (this spot is taken by a push operation,
                        // but the value is not written into yet), then spin
                        SpinWait spinLocal = new SpinWait();
                        while (!m_state[lowLocal].m_value)
                        {
                            spinLocal.SpinOnce();
                        }
                        result = m_array[lowLocal];

                        // If there is no other thread taking snapshot (GetEnumerator(), ToList(), etc), reset the deleted entry to null.
                        // It is ok if after this conditional check m_numSnapshotTakers becomes > 0, because new snapshots won't include 
                        // the deleted entry at m_array[lowLocal]. 
                        if (_source.m_numSnapshotTakers <= 0)
                        {
                            m_array[lowLocal] = default(T); //release the reference to the object. 
                        }

                        //if the current thread sets m_low to SEGMENT_SIZE, which means the current segment becomes
                        //disposable, then this thread is responsible to dispose this segment, and reset m_head 
                        if (lowLocal + 1 >= SEGMENT_SIZE)
                        {
                            //  Invariant: we only dispose the current m_head, not any other segment
                            //  In usual situation, disposing a segment is simply seting m_head to m_head.m_next
                            //  But there is one special case, where m_head and m_tail points to the same and ONLY
                            //segment of the queue: Another thread A is doing Enqueue and finds that it needs to grow,
                            //while the *current* thread is doing *this* Dequeue operation, and finds that it needs to 
                            //dispose the current (and ONLY) segment. Then we need to wait till thread A finishes its 
                            //Grow operation, this is the reason of having the following while loop
                            spinLocal = new SpinWait();
                            while (_next == null)
                            {
                                spinLocal.SpinOnce();
                            }
                            Debug.Assert(_source._head == this);
                            _source._head = _next;
                        }
                        return true;
                    }
                    else
                    {
                        //CAS failed due to contention: spin briefly and retry
                        spin.SpinOnce();
                        lowLocal = Low; highLocal = High;
                    }
                }//end of while
                result = default(T);
                return false;
            }

            /// <summary>
            /// return the position of the head of the current segment
            /// Value range [0, SEGMENT_SIZE], if it's SEGMENT_SIZE, it means this segment is exhausted and thus empty
            /// </summary>
            internal int Low
            {
                get
                {
                    return Math.Min(_low, SEGMENT_SIZE);
                }
            }

            /// <summary>
            /// return the logical position of the tail of the current segment      
            /// Value range [-1, SEGMENT_SIZE-1]. When it's -1, it means this is a new segment and has no elemnet yet
            /// </summary>
            internal int High
            {
                get
                {
                    //if m_high > SEGMENT_SIZE, it means it's out of range, we should return
                    //SEGMENT_SIZE-1 as the logical position
                    return Math.Min(_high, SEGMENT_SIZE - 1);
                }
            }
        }
    }//end of class Segment

    /// <summary>
    /// A wrapper struct for volatile bool, please note the copy of the struct it self will not be volatile
    /// for example this statement will not include in volatilness operation volatileBool1 = volatileBool2 the jit will copy the struct and will ignore the volatile
    /// </summary>
    internal struct VolatileBool
    {
        public VolatileBool(bool value)
        {
            m_value = value;
        }
        public volatile bool m_value;
    }
}
