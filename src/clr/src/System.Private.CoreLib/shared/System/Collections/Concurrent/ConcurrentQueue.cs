// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Collections.Concurrent
{
    /// <summary>
    /// Represents a thread-safe first-in, first-out collection of objects.
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements in the queue.</typeparam>
    /// <remarks>
    /// All public and protected members of <see cref="ConcurrentQueue{T}"/> are thread-safe and may be used
    /// concurrently from multiple threads.
    /// </remarks>
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(IProducerConsumerCollectionDebugView<>))]
    public class ConcurrentQueue<T> : IProducerConsumerCollection<T>, IReadOnlyCollection<T>
    {
        // This implementation provides an unbounded, multi-producer multi-consumer queue
        // that supports the standard Enqueue/TryDequeue operations, as well as support for
        // snapshot enumeration (GetEnumerator, ToArray, CopyTo), peeking, and Count/IsEmpty.
        // It is composed of a linked list of bounded ring buffers, each of which has a head
        // and a tail index, isolated from each other to minimize false sharing.  As long as
        // the number of elements in the queue remains less than the size of the current
        // buffer (Segment), no additional allocations are required for enqueued items.  When
        // the number of items exceeds the size of the current segment, the current segment is
        // "frozen" to prevent further enqueues, and a new segment is linked from it and set
        // as the new tail segment for subsequent enqueues.  As old segments are consumed by
        // dequeues, the head reference is updated to point to the segment that dequeuers should
        // try next.  To support snapshot enumeration, segments also support the notion of
        // preserving for observation, whereby they avoid overwriting state as part of dequeues.
        // Any operation that requires a snapshot results in all current segments being
        // both frozen for enqueues and preserved for observation: any new enqueues will go
        // to new segments, and dequeuers will consume from the existing segments but without
        // overwriting the existing data.

        /// <summary>Initial length of the segments used in the queue.</summary>
        private const int InitialSegmentLength = 32;
        /// <summary>
        /// Maximum length of the segments used in the queue.  This is a somewhat arbitrary limit:
        /// larger means that as long as we don't exceed the size, we avoid allocating more segments,
        /// but if we do exceed it, then the segment becomes garbage.
        /// </summary>
        private const int MaxSegmentLength = 1024 * 1024;

        /// <summary>
        /// Lock used to protect cross-segment operations, including any updates to <see cref="_tail"/> or <see cref="_head"/>
        /// and any operations that need to get a consistent view of them.
        /// </summary>
        private readonly object _crossSegmentLock;
        /// <summary>The current tail segment.</summary>
        private volatile ConcurrentQueueSegment<T> _tail;
        /// <summary>The current head segment.</summary>
        private volatile ConcurrentQueueSegment<T> _head; // SOS's ThreadPool command depends on this name

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentQueue{T}"/> class.
        /// </summary>
        public ConcurrentQueue()
        {
            _crossSegmentLock = new object();
            _tail = _head = new ConcurrentQueueSegment<T>(InitialSegmentLength);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentQueue{T}"/> class that contains elements copied
        /// from the specified collection.
        /// </summary>
        /// <param name="collection">
        /// The collection whose elements are copied to the new <see cref="ConcurrentQueue{T}"/>.
        /// </param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="collection"/> argument is null.</exception>
        public ConcurrentQueue(IEnumerable<T> collection)
        {
            if (collection == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.collection);
            }

            _crossSegmentLock = new object();

            // Determine the initial segment size.  We'll use the default,
            // unless the collection is known to be larger than that, in which
            // case we round its length up to a power of 2, as all segments must
            // be a power of 2 in length.
            int length = InitialSegmentLength;
            if (collection is ICollection<T> c)
            {
                int count = c.Count;
                if (count > length)
                {
                    length = Math.Min(ConcurrentQueueSegment<T>.RoundUpToPowerOf2(count), MaxSegmentLength);
                }
            }

            // Initialize the segment and add all of the data to it.
            _tail = _head = new ConcurrentQueueSegment<T>(length);
            foreach (T item in collection!) // TODO-NULLABLE: Remove ! when [DoesNotReturn] respected
            {
                Enqueue(item);
            }
        }

        /// <summary>
        /// Copies the elements of the <see cref="ICollection"/> to an <see
        /// cref="Array"/>, starting at a particular <see cref="Array"/> index.
        /// </summary>
        /// <param name="array">
        /// The one-dimensional <see cref="Array">Array</see> that is the destination of the
        /// elements copied from the <see cref="ConcurrentQueue{T}"/>. <paramref name="array"/> must have
        /// zero-based indexing.
        /// </param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is a null reference (Nothing in
        /// Visual Basic).</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than
        /// zero.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="array"/> is multidimensional. -or-
        /// <paramref name="array"/> does not have zero-based indexing. -or-
        /// <paramref name="index"/> is equal to or greater than the length of the <paramref name="array"/>
        /// -or- The number of elements in the source <see cref="ICollection"/> is
        /// greater than the available space from <paramref name="index"/> to the end of the destination
        /// <paramref name="array"/>. -or- The type of the source <see
        /// cref="ICollection"/> cannot be cast automatically to the type of the
        /// destination <paramref name="array"/>.
        /// </exception>
        void ICollection.CopyTo(Array array, int index)
        {
            // Special-case when the Array is actually a T[], taking a faster path
            if (array is T[] szArray)
            {
                CopyTo(szArray, index);
                return;
            }

            // Validate arguments.
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            // Otherwise, fall back to the slower path that first copies the contents
            // to an array, and then uses that array's non-generic CopyTo to do the copy.
            ToArray().CopyTo(array!, index); // TODO-NULLABLE: Remove ! when [DoesNotReturn] respected
        }

        /// <summary>
        /// Gets a value indicating whether access to the <see cref="ICollection"/> is
        /// synchronized with the SyncRoot.
        /// </summary>
        /// <value>true if access to the <see cref="ICollection"/> is synchronized
        /// with the SyncRoot; otherwise, false. For <see cref="ConcurrentQueue{T}"/>, this property always
        /// returns false.</value>
        bool ICollection.IsSynchronized => false; // always false, as true implies synchronization via SyncRoot

        /// <summary>
        /// Gets an object that can be used to synchronize access to the <see
        /// cref="ICollection"/>. This property is not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">The SyncRoot property is not supported.</exception>
        object ICollection.SyncRoot { get { ThrowHelper.ThrowNotSupportedException(ExceptionResource.ConcurrentCollection_SyncRoot_NotSupported); return default!; } } // TODO-NULLABLE: Remove ! when [DoesNotReturn] respected

        /// <summary>Returns an enumerator that iterates through a collection.</summary>
        /// <returns>An <see cref="IEnumerator"/> that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)this).GetEnumerator();

        /// <summary>
        /// Attempts to add an object to the <see cref="Concurrent.IProducerConsumerCollection{T}"/>.
        /// </summary>
        /// <param name="item">The object to add to the <see
        /// cref="Concurrent.IProducerConsumerCollection{T}"/>. The value can be a null
        /// reference (Nothing in Visual Basic) for reference types.
        /// </param>
        /// <returns>true if the object was added successfully; otherwise, false.</returns>
        /// <remarks>For <see cref="ConcurrentQueue{T}"/>, this operation will always add the object to the
        /// end of the <see cref="ConcurrentQueue{T}"/>
        /// and return true.</remarks>
        bool IProducerConsumerCollection<T>.TryAdd(T item)
        {
            Enqueue(item);
            return true;
        }

        /// <summary>
        /// Attempts to remove and return an object from the <see cref="Concurrent.IProducerConsumerCollection{T}"/>.
        /// </summary>
        /// <param name="item">
        /// When this method returns, if the operation was successful, <paramref name="item"/> contains the
        /// object removed. If no object was available to be removed, the value is unspecified.
        /// </param>
        /// <returns>true if an element was removed and returned successfully; otherwise, false.</returns>
        /// <remarks>For <see cref="ConcurrentQueue{T}"/>, this operation will attempt to remove the object
        /// from the beginning of the <see cref="ConcurrentQueue{T}"/>.
        /// </remarks>
        bool IProducerConsumerCollection<T>.TryTake(out T item) => TryDequeue(out item);

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
                // IsEmpty == !TryPeek. We use a "resultUsed:false" peek in order to avoid marking
                // segments as preserved for observation, making IsEmpty a cheaper way than either
                // TryPeek(out T) or Count == 0 to check whether any elements are in the queue.
                T ignoredResult;
                return !TryPeek(out ignoredResult, resultUsed: false);
            }
        }

        /// <summary>Copies the elements stored in the <see cref="ConcurrentQueue{T}"/> to a new array.</summary>
        /// <returns>A new array containing a snapshot of elements copied from the <see cref="ConcurrentQueue{T}"/>.</returns>
        public T[] ToArray()
        {
            // Snap the current contents for enumeration.
            ConcurrentQueueSegment<T> head, tail;
            int headHead, tailTail;
            SnapForObservation(out head, out headHead, out tail, out tailTail);

            // Count the number of items in that snapped set, and use it to allocate an
            // array of the right size.
            long count = GetCount(head, headHead, tail, tailTail);
            T[] arr = new T[count];

            // Now enumerate the contents, copying each element into the array.
            using (IEnumerator<T> e = Enumerate(head, headHead, tail, tailTail))
            {
                int i = 0;
                while (e.MoveNext())
                {
                    arr[i++] = e.Current;
                }
                Debug.Assert(count == i);
            }

            // And return it.
            return arr;
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
                var spinner = new SpinWait();
                while (true)
                {
                    // Capture the head and tail, as well as the head's head and tail.
                    ConcurrentQueueSegment<T> head = _head;
                    ConcurrentQueueSegment<T> tail = _tail;
                    int headHead = Volatile.Read(ref head._headAndTail.Head);
                    int headTail = Volatile.Read(ref head._headAndTail.Tail);

                    if (head == tail)
                    {
                        // There was a single segment in the queue.  If the captured segments still
                        // match, then we can trust the values to compute the segment's count. (It's
                        // theoretically possible the values could have looped around and still exactly match,
                        // but that would required at least ~4 billion elements to have been enqueued and
                        // dequeued between the reads.)
                        if (head == _head &&
                            tail == _tail &&
                            headHead == Volatile.Read(ref head._headAndTail.Head) &&
                            headTail == Volatile.Read(ref head._headAndTail.Tail))
                        {
                            return GetCount(head, headHead, headTail);
                        }
                    }
                    else if (head._nextSegment == tail)
                    {
                        // There were two segments in the queue.  Get the positions from the tail, and as above,
                        // if the captured values match the previous reads, return the sum of the counts from both segments.
                        int tailHead = Volatile.Read(ref tail._headAndTail.Head);
                        int tailTail = Volatile.Read(ref tail._headAndTail.Tail);
                        if (head == _head &&
                            tail == _tail &&
                            headHead == Volatile.Read(ref head._headAndTail.Head) &&
                            headTail == Volatile.Read(ref head._headAndTail.Tail) &&
                            tailHead == Volatile.Read(ref tail._headAndTail.Head) &&
                            tailTail == Volatile.Read(ref tail._headAndTail.Tail))
                        {
                            return GetCount(head, headHead, headTail) + GetCount(tail, tailHead, tailTail);
                        }
                    }
                    else
                    {
                        // There were more than two segments in the queue.  Fall back to taking the cross-segment lock,
                        // which will ensure that the head and tail segments we read are stable (since the lock is needed to change them);
                        // for the two-segment case above, we can simply rely on subsequent comparisons, but for the two+ case, we need
                        // to be able to trust the internal segments between the head and tail.
                        lock (_crossSegmentLock)
                        {
                            // Now that we hold the lock, re-read the previously captured head and tail segments and head positions.
                            // If either has changed, start over.
                            if (head == _head && tail == _tail)
                            {
                                // Get the positions from the tail, and as above, if the captured values match the previous reads,
                                // we can use the values to compute the count of the head and tail segments.
                                int tailHead = Volatile.Read(ref tail._headAndTail.Head);
                                int tailTail = Volatile.Read(ref tail._headAndTail.Tail);
                                if (headHead == Volatile.Read(ref head._headAndTail.Head) &&
                                    headTail == Volatile.Read(ref head._headAndTail.Tail) &&
                                    tailHead == Volatile.Read(ref tail._headAndTail.Head) &&
                                    tailTail == Volatile.Read(ref tail._headAndTail.Tail))
                                {
                                    // We got stable values for the head and tail segments, so we can just compute the sizes
                                    // based on those and add them. Note that this and the below additions to count may overflow: previous
                                    // implementations allowed that, so we don't check, either, and it is theoretically possible for the
                                    // queue to store more than int.MaxValue items.
                                    int count = GetCount(head, headHead, headTail) + GetCount(tail, tailHead, tailTail);

                                    // Now add the counts for each internal segment. Since there were segments before these,
                                    // for counting purposes we consider them to start at the 0th element, and since there is at
                                    // least one segment after each, each was frozen, so we can count until each's frozen tail.
                                    // With the cross-segment lock held, we're guaranteed that all of these internal segments are
                                    // consistent, as the head and tail segment can't be changed while we're holding the lock, and
                                    // dequeueing and enqueueing can only be done from the head and tail segments, which these aren't.
                                    for (ConcurrentQueueSegment<T> s = head._nextSegment!; s != tail; s = s._nextSegment!)
                                    {
                                        Debug.Assert(s._frozenForEnqueues, "Internal segment must be frozen as there's a following segment.");
                                        count += s._headAndTail.Tail - s.FreezeOffset;
                                    }

                                    return count;
                                }
                            }
                        }
                    }

                    // We raced with enqueues/dequeues and captured an inconsistent picture of the queue.
                    // Spin and try again.
                    spinner.SpinOnce();
                }
            }
        }

        /// <summary>Computes the number of items in a segment based on a fixed head and tail in that segment.</summary>
        private static int GetCount(ConcurrentQueueSegment<T> s, int head, int tail)
        {
            if (head != tail && head != tail - s.FreezeOffset)
            {
                head &= s._slotsMask;
                tail &= s._slotsMask;
                return head < tail ? tail - head : s._slots.Length - head + tail;
            }
            return 0;
        }

        /// <summary>Gets the number of items in snapped region.</summary>
        private static long GetCount(ConcurrentQueueSegment<T> head, int headHead, ConcurrentQueueSegment<T> tail, int tailTail)
        {
            // All of the segments should have been both frozen for enqueues and preserved for observation.
            // Validate that here for head and tail; we'll validate it for intermediate segments later.
            Debug.Assert(head._preservedForObservation);
            Debug.Assert(head._frozenForEnqueues);
            Debug.Assert(tail._preservedForObservation);
            Debug.Assert(tail._frozenForEnqueues);

            long count = 0;

            // Head segment.  We've already marked it as frozen for enqueues, so its tail position is fixed,
            // and we've already marked it as preserved for observation (before we grabbed the head), so we
            // can safely enumerate from its head to its tail and access its elements.
            int headTail = (head == tail ? tailTail : Volatile.Read(ref head._headAndTail.Tail)) - head.FreezeOffset;
            if (headHead < headTail)
            {
                // Mask the head and tail for the head segment
                headHead &= head._slotsMask;
                headTail &= head._slotsMask;

                // Increase the count by either the one or two regions, based on whether tail
                // has wrapped to be less than head.
                count += headHead < headTail ?
                    headTail - headHead :
                    head._slots.Length - headHead + headTail;
            }

            // We've enumerated the head.  If the tail is different from the head, we need to
            // enumerate the remaining segments.
            if (head != tail)
            {
                // Count the contents of each segment between head and tail, not including head and tail.
                // Since there were segments before these, for our purposes we consider them to start at
                // the 0th element, and since there is at least one segment after each, each was frozen
                // by the time we snapped it, so we can iterate until each's frozen tail.
                for (ConcurrentQueueSegment<T> s = head._nextSegment!; s != tail; s = s._nextSegment!)
                {
                    Debug.Assert(s._preservedForObservation);
                    Debug.Assert(s._frozenForEnqueues);
                    count += s._headAndTail.Tail - s.FreezeOffset;
                }

                // Finally, enumerate the tail.  As with the intermediate segments, there were segments
                // before this in the snapped region, so we can start counting from the beginning. Unlike
                // the intermediate segments, we can't just go until the Tail, as that could still be changing;
                // instead we need to go until the tail we snapped for observation.
                count += tailTail - tail.FreezeOffset;
            }

            // Return the computed count.
            return count;
        }

        /// <summary>
        /// Copies the <see cref="ConcurrentQueue{T}"/> elements to an existing one-dimensional <see
        /// cref="Array">Array</see>, starting at the specified array index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="Array">Array</see> that is the
        /// destination of the elements copied from the
        /// <see cref="ConcurrentQueue{T}"/>. The <see cref="Array">Array</see> must have zero-based
        /// indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying
        /// begins.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is a null reference (Nothing in
        /// Visual Basic).</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than
        /// zero.</exception>
        /// <exception cref="ArgumentException"><paramref name="index"/> is equal to or greater than the
        /// length of the <paramref name="array"/>
        /// -or- The number of elements in the source <see cref="ConcurrentQueue{T}"/> is greater than the
        /// available space from <paramref name="index"/> to the end of the destination <paramref
        /// name="array"/>.
        /// </exception>
        public void CopyTo(T[] array, int index)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }
            if (index < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            // Snap for enumeration
            ConcurrentQueueSegment<T> head, tail;
            int headHead, tailTail;
            SnapForObservation(out head, out headHead, out tail, out tailTail);

            // Get the number of items to be enumerated
            long count = GetCount(head, headHead, tail, tailTail);
            if (index > array!.Length - count) // TODO-NULLABLE: Remove ! when [DoesNotReturn] respected
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
            }

            // Copy the items to the target array
            int i = index;
            using (IEnumerator<T> e = Enumerate(head, headHead, tail, tailTail))
            {
                while (e.MoveNext())
                {
                    array[i++] = e.Current;
                }
            }
            Debug.Assert(count == i - index);
        }

        /// <summary>Returns an enumerator that iterates through the <see cref="ConcurrentQueue{T}"/>.</summary>
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
            ConcurrentQueueSegment<T> head, tail;
            int headHead, tailTail;
            SnapForObservation(out head, out headHead, out tail, out tailTail);
            return Enumerate(head, headHead, tail, tailTail);
        }

        /// <summary>
        /// Gets the head and tail information of the current contents of the queue.
        /// After this call returns, the specified region can be enumerated any number
        /// of times and will not change.
        /// </summary>
        private void SnapForObservation(out ConcurrentQueueSegment<T> head, out int headHead, out ConcurrentQueueSegment<T> tail, out int tailTail)
        {
            lock (_crossSegmentLock) // _head and _tail may only change while the lock is held.
            {
                // Snap the head and tail
                head = _head;
                tail = _tail;
                Debug.Assert(head != null);
                Debug.Assert(tail != null);
                Debug.Assert(tail._nextSegment == null);

                // Mark them and all segments in between as preserving, and ensure no additional items
                // can be added to the tail.
                for (ConcurrentQueueSegment<T> s = head; ; s = s._nextSegment!)
                {
                    s._preservedForObservation = true;
                    if (s == tail) break;
                    Debug.Assert(s._frozenForEnqueues); // any non-tail should already be marked
                }
                tail.EnsureFrozenForEnqueues(); // we want to prevent the tailTail from moving

                // At this point, any dequeues from any segment won't overwrite the value, and
                // none of the existing segments can have new items enqueued.

                headHead = Volatile.Read(ref head._headAndTail.Head);
                tailTail = Volatile.Read(ref tail._headAndTail.Tail);
            }
        }

        /// <summary>Gets the item stored in the <paramref name="i"/>th entry in <paramref name="segment"/>.</summary>
        private T GetItemWhenAvailable(ConcurrentQueueSegment<T> segment, int i)
        {
            Debug.Assert(segment._preservedForObservation);

            // Get the expected value for the sequence number
            int expectedSequenceNumberAndMask = (i + 1) & segment._slotsMask;

            // If the expected sequence number is not yet written, we're still waiting for
            // an enqueuer to finish storing it.  Spin until it's there.
            if ((segment._slots[i].SequenceNumber & segment._slotsMask) != expectedSequenceNumberAndMask)
            {
                var spinner = new SpinWait();
                while ((Volatile.Read(ref segment._slots[i].SequenceNumber) & segment._slotsMask) != expectedSequenceNumberAndMask)
                {
                    spinner.SpinOnce();
                }
            }

            // Return the value from the slot.
            return segment._slots[i].Item;
        }

        private IEnumerator<T> Enumerate(ConcurrentQueueSegment<T> head, int headHead, ConcurrentQueueSegment<T> tail, int tailTail)
        {
            Debug.Assert(head._preservedForObservation);
            Debug.Assert(head._frozenForEnqueues);
            Debug.Assert(tail._preservedForObservation);
            Debug.Assert(tail._frozenForEnqueues);

            // Head segment.  We've already marked it as not accepting any more enqueues,
            // so its tail position is fixed, and we've already marked it as preserved for
            // enumeration (before we grabbed its head), so we can safely enumerate from
            // its head to its tail.
            int headTail = (head == tail ? tailTail : Volatile.Read(ref head._headAndTail.Tail)) - head.FreezeOffset;
            if (headHead < headTail)
            {
                headHead &= head._slotsMask;
                headTail &= head._slotsMask;

                if (headHead < headTail)
                {
                    for (int i = headHead; i < headTail; i++) yield return GetItemWhenAvailable(head, i);
                }
                else
                {
                    for (int i = headHead; i < head._slots.Length; i++) yield return GetItemWhenAvailable(head, i);
                    for (int i = 0; i < headTail; i++) yield return GetItemWhenAvailable(head, i);
                }
            }

            // We've enumerated the head.  If the tail is the same, we're done.
            if (head != tail)
            {
                // Each segment between head and tail, not including head and tail.  Since there were
                // segments before these, for our purposes we consider it to start at the 0th element.
                for (ConcurrentQueueSegment<T> s = head._nextSegment!; s != tail; s = s._nextSegment!)
                {
                    Debug.Assert(s._preservedForObservation, "Would have had to been preserved as a segment part of enumeration");
                    Debug.Assert(s._frozenForEnqueues, "Would have had to be frozen for enqueues as it's intermediate");

                    int sTail = s._headAndTail.Tail - s.FreezeOffset;
                    for (int i = 0; i < sTail; i++)
                    {
                        yield return GetItemWhenAvailable(s, i);
                    }
                }

                // Enumerate the tail.  Since there were segments before this, we can just start at
                // its beginning, and iterate until the tail we already grabbed.
                tailTail -= tail.FreezeOffset;
                for (int i = 0; i < tailTail; i++)
                {
                    yield return GetItemWhenAvailable(tail, i);
                }
            }
        }

        /// <summary>Adds an object to the end of the <see cref="ConcurrentQueue{T}"/>.</summary>
        /// <param name="item">
        /// The object to add to the end of the <see cref="ConcurrentQueue{T}"/>.
        /// The value can be a null reference (Nothing in Visual Basic) for reference types.
        /// </param>
        public void Enqueue(T item)
        {
            // Try to enqueue to the current tail.
            if (!_tail.TryEnqueue(item))
            {
                // If we're unable to, we need to take a slow path that will
                // try to add a new tail segment.
                EnqueueSlow(item);
            }
        }

        /// <summary>Adds to the end of the queue, adding a new segment if necessary.</summary>
        private void EnqueueSlow(T item)
        {
            while (true)
            {
                ConcurrentQueueSegment<T> tail = _tail;

                // Try to append to the existing tail.
                if (tail.TryEnqueue(item))
                {
                    return;
                }

                // If we were unsuccessful, take the lock so that we can compare and manipulate
                // the tail.  Assuming another enqueuer hasn't already added a new segment,
                // do so, then loop around to try enqueueing again.
                lock (_crossSegmentLock)
                {
                    if (tail == _tail)
                    {
                        // Make sure no one else can enqueue to this segment.
                        tail.EnsureFrozenForEnqueues();

                        // We determine the new segment's length based on the old length.
                        // In general, we double the size of the segment, to make it less likely
                        // that we'll need to grow again.  However, if the tail segment is marked
                        // as preserved for observation, something caused us to avoid reusing this
                        // segment, and if that happens a lot and we grow, we'll end up allocating
                        // lots of wasted space.  As such, in such situations we reset back to the
                        // initial segment length; if these observations are happening frequently,
                        // this will help to avoid wasted memory, and if they're not, we'll
                        // relatively quickly grow again to a larger size.
                        int nextSize = tail._preservedForObservation ? InitialSegmentLength : Math.Min(tail.Capacity * 2, MaxSegmentLength);
                        var newTail = new ConcurrentQueueSegment<T>(nextSize);

                        // Hook up the new tail.
                        tail._nextSegment = newTail;
                        _tail = newTail;
                    }
                }
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
        /// <returns>
        /// true if an element was removed and returned from the beginning of the
        /// <see cref="ConcurrentQueue{T}"/> successfully; otherwise, false.
        /// </returns>
        public bool TryDequeue([MaybeNullWhen(false)] out T result) =>
            _head.TryDequeue(out result) || // fast-path that operates just on the head segment
            TryDequeueSlow(out result); // slow path that needs to fix up segments

        /// <summary>Tries to dequeue an item, removing empty segments as needed.</summary>
        private bool TryDequeueSlow([MaybeNullWhen(false)] out T item)
        {
            while (true)
            {
                // Get the current head
                ConcurrentQueueSegment<T> head = _head;

                // Try to take.  If we're successful, we're done.
                if (head.TryDequeue(out item))
                {
                    return true;
                }

                // Check to see whether this segment is the last. If it is, we can consider
                // this to be a moment-in-time empty condition (even though between the TryDequeue
                // check and this check, another item could have arrived).
                if (head._nextSegment == null)
                {
                    item = default!;
                    return false;
                }

                // At this point we know that head.Next != null, which means
                // this segment has been frozen for additional enqueues. But between
                // the time that we ran TryDequeue and checked for a next segment,
                // another item could have been added.  Try to dequeue one more time
                // to confirm that the segment is indeed empty.
                Debug.Assert(head._frozenForEnqueues);
                if (head.TryDequeue(out item))
                {
                    return true;
                }

                // This segment is frozen (nothing more can be added) and empty (nothing is in it).
                // Update head to point to the next segment in the list, assuming no one's beat us to it.
                lock (_crossSegmentLock)
                {
                    if (head == _head)
                    {
                        _head = head._nextSegment;
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to return an object from the beginning of the <see cref="ConcurrentQueue{T}"/>
        /// without removing it.
        /// </summary>
        /// <param name="result">
        /// When this method returns, <paramref name="result"/> contains an object from
        /// the beginning of the <see cref="Concurrent.ConcurrentQueue{T}"/> or default(T)
        /// if the operation failed.
        /// </param>
        /// <returns>true if and object was returned successfully; otherwise, false.</returns>
        /// <remarks>
        /// For determining whether the collection contains any items, use of the <see cref="IsEmpty"/>
        /// property is recommended rather than peeking.
        /// </remarks>
        public bool TryPeek([MaybeNullWhen(false)] out T result) => TryPeek(out result, resultUsed: true);

        /// <summary>Attempts to retrieve the value for the first element in the queue.</summary>
        /// <param name="result">The value of the first element, if found.</param>
        /// <param name="resultUsed">true if the result is needed; otherwise false if only the true/false outcome is needed.</param>
        /// <returns>true if an element was found; otherwise, false.</returns>
        private bool TryPeek([MaybeNullWhen(false)] out T result, bool resultUsed)
        {
            // Starting with the head segment, look through all of the segments
            // for the first one we can find that's not empty.
            ConcurrentQueueSegment<T> s = _head;
            while (true)
            {
                // Grab the next segment from this one, before we peek.
                // This is to be able to see whether the value has changed
                // during the peek operation.
                ConcurrentQueueSegment<T>? next = Volatile.Read(ref s._nextSegment);

                // Peek at the segment.  If we find an element, we're done.
                if (s.TryPeek(out result, resultUsed))
                {
                    return true;
                }

                // The current segment was empty at the moment we checked.

                if (next != null)
                {
                    // If prior to the peek there was already a next segment, then
                    // during the peek no additional items could have been enqueued
                    // to it and we can just move on to check the next segment.
                    Debug.Assert(next == s._nextSegment);
                    s = next;
                }
                else if (Volatile.Read(ref s._nextSegment) == null)
                {
                    // The next segment is null.  Nothing more to peek at.
                    break;
                }

                // The next segment was null before we peeked but non-null after.
                // That means either when we peeked the first segment had
                // already been frozen but the new segment not yet added,
                // or that the first segment was empty and between the time
                // that we peeked and then checked _nextSegment, so many items
                // were enqueued that we filled the first segment and went
                // into the next.  Since we need to peek in order, we simply
                // loop around again to peek on the same segment.  The next
                // time around on this segment we'll then either successfully
                // peek or we'll find that next was non-null before peeking,
                // and we'll traverse to that segment.
            }

            result = default!;
            return false;
        }

        /// <summary>
        /// Removes all objects from the <see cref="ConcurrentQueue{T}"/>.
        /// </summary>
        public void Clear()
        {
            lock (_crossSegmentLock)
            {
                // Simply substitute a new segment for the existing head/tail,
                // as is done in the constructor.  Operations currently in flight
                // may still read from or write to an existing segment that's
                // getting dropped, meaning that in flight operations may not be
                // linear with regards to this clear operation.  To help mitigate
                // in-flight operations enqueuing onto the tail that's about to
                // be dropped, we first freeze it; that'll force enqueuers to take
                // this lock to synchronize and see the new tail.
                _tail.EnsureFrozenForEnqueues();
                _tail = _head = new ConcurrentQueueSegment<T>(InitialSegmentLength);
            }
        }
    }
}
