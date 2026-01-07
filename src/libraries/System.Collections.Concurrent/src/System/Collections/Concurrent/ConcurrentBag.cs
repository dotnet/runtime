// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Collections.Concurrent
{
    /// <summary>
    /// Represents a thread-safe, unordered collection of objects.
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements in the bag.</typeparam>
    /// <remarks>
    /// <para>
    /// Bags are useful for storing objects when ordering doesn't matter, and unlike sets, bags support
    /// duplicates. <see cref="ConcurrentBag{T}"/> is a thread-safe bag implementation, optimized for
    /// scenarios where the same thread will be both producing and consuming data stored in the bag.
    /// </para>
    /// <para>
    /// <see cref="ConcurrentBag{T}"/> accepts null reference (Nothing in Visual Basic) as a valid
    /// value for reference types.
    /// </para>
    /// <para>
    /// All public and protected members of <see cref="ConcurrentBag{T}"/> are thread-safe and may be used
    /// concurrently from multiple threads.
    /// </para>
    /// </remarks>
    [DebuggerTypeProxy(typeof(IProducerConsumerCollectionDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    public class ConcurrentBag<T> : IProducerConsumerCollection<T>, IReadOnlyCollection<T>
    {
        /// <summary>The per-bag, per-thread work-stealing queues.</summary>
        private readonly ThreadLocal<WorkStealingQueue> _locals;
        /// <summary>The head work stealing queue in a linked list of queues.</summary>
        private volatile WorkStealingQueue? _workStealingQueues;
        /// <summary>Number of times any list transitions from empty to non-empty.</summary>
        private long _emptyToNonEmptyListTransitionCount;

        /// <summary>Initializes a new instance of the <see cref="ConcurrentBag{T}"/> class.</summary>
        public ConcurrentBag()
        {
            _locals = new ThreadLocal<WorkStealingQueue>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentBag{T}"/>
        /// class that contains elements copied from the specified collection.
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new <see
        /// cref="ConcurrentBag{T}"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        public ConcurrentBag(IEnumerable<T> collection)
        {
            ArgumentNullException.ThrowIfNull(collection);

            _locals = new ThreadLocal<WorkStealingQueue>();

            WorkStealingQueue queue = GetCurrentThreadWorkStealingQueue(forceCreate: true)!;
            foreach (T item in collection)
            {
                queue.LocalPush(item, ref _emptyToNonEmptyListTransitionCount);
            }
        }

        /// <summary>
        /// Adds an object to the <see cref="ConcurrentBag{T}"/>.
        /// </summary>
        /// <param name="item">The object to be added to the
        /// <see cref="ConcurrentBag{T}"/>. The value can be a null reference
        /// (Nothing in Visual Basic) for reference types.</param>
        public void Add(T item) =>
            GetCurrentThreadWorkStealingQueue(forceCreate: true)!
            .LocalPush(item, ref _emptyToNonEmptyListTransitionCount);

        /// <summary>
        /// Attempts to add an object to the <see cref="ConcurrentBag{T}"/>.
        /// </summary>
        /// <param name="item">The object to be added to the
        /// <see cref="ConcurrentBag{T}"/>. The value can be a null reference
        /// (Nothing in Visual Basic) for reference types.</param>
        /// <returns>Always returns true</returns>
        bool IProducerConsumerCollection<T>.TryAdd(T item)
        {
            Add(item);
            return true;
        }

        /// <summary>
        /// Attempts to remove and return an object from the <see cref="ConcurrentBag{T}"/>.
        /// </summary>
        /// <param name="result">When this method returns, <paramref name="result"/> contains the object
        /// removed from the <see cref="ConcurrentBag{T}"/> or the default value
        /// of <typeparamref name="T"/> if the operation failed.</param>
        /// <returns>true if an object was removed successfully; otherwise, false.</returns>
        public bool TryTake([MaybeNullWhen(false)] out T result)
        {
            WorkStealingQueue? queue = GetCurrentThreadWorkStealingQueue(forceCreate: false);
            return (queue != null && queue.TryLocalPop(out result)) || TrySteal(out result, take: true);
        }

        /// <summary>
        /// Attempts to return an object from the <see cref="ConcurrentBag{T}"/> without removing it.
        /// </summary>
        /// <param name="result">When this method returns, <paramref name="result"/> contains an object from
        /// the <see cref="ConcurrentBag{T}"/> or the default value of
        /// <typeparamref name="T"/> if the operation failed.</param>
        /// <returns>true if and object was returned successfully; otherwise, false.</returns>
        public bool TryPeek([MaybeNullWhen(false)] out T result)
        {
            WorkStealingQueue? queue = GetCurrentThreadWorkStealingQueue(forceCreate: false);
            return (queue != null && queue.TryLocalPeek(out result)) || TrySteal(out result, take: false);
        }

        /// <summary>Gets the work-stealing queue data structure for the current thread.</summary>
        /// <param name="forceCreate">Whether to create a new queue if this thread doesn't have one.</param>
        /// <returns>The local queue object, or null if the thread doesn't have one.</returns>
        private WorkStealingQueue? GetCurrentThreadWorkStealingQueue(bool forceCreate) =>
            _locals.Value ??
            (forceCreate ? CreateWorkStealingQueueForCurrentThread() : null);

        private WorkStealingQueue CreateWorkStealingQueueForCurrentThread()
        {
            lock (GlobalQueuesLock) // necessary to update _workStealingQueues, so as to synchronize with freezing operations
            {
                WorkStealingQueue? head = _workStealingQueues;

                WorkStealingQueue? queue = head != null ? GetUnownedWorkStealingQueue() : null;
                if (queue == null)
                {
                    _workStealingQueues = queue = new WorkStealingQueue(head);
                }
                _locals.Value = queue;

                return queue;
            }
        }

        /// <summary>
        /// Try to reuse an unowned queue.  If a thread interacts with the bag and then exits,
        /// the bag purposefully retains its queue, as it contains data associated with the bag.
        /// </summary>
        /// <returns>The queue object, or null if no unowned queue could be gathered.</returns>
        private WorkStealingQueue? GetUnownedWorkStealingQueue()
        {
            Debug.Assert(Monitor.IsEntered(GlobalQueuesLock));

            // Look for a thread that has the same ID as this one.  It won't have come from the same thread,
            // but if our thread ID is reused, we know that no other thread can have the same ID and thus
            // no other thread can be using this queue.
            int currentThreadId = Environment.CurrentManagedThreadId;
            for (WorkStealingQueue? queue = _workStealingQueues; queue != null; queue = queue._nextQueue)
            {
                if (queue._ownerThreadId == currentThreadId)
                {
                    return queue;
                }
            }

            return null;
        }

        /// <summary>Local helper method to steal an item from any other non empty thread.</summary>
        /// <param name="result">To receive the item retrieved from the bag</param>
        /// <param name="take">Whether to remove or peek.</param>
        /// <returns>True if succeeded, false otherwise.</returns>
        private bool TrySteal([MaybeNullWhen(false)] out T result, bool take)
        {
            if (CDSCollectionETWBCLProvider.Log.IsEnabled())
            {
                if (take)
                {
                    CDSCollectionETWBCLProvider.Log.ConcurrentBag_TryTakeSteals();
                }
                else
                {
                    CDSCollectionETWBCLProvider.Log.ConcurrentBag_TryPeekSteals();
                }
            }

            while (true)
            {
                // We need to track whether any lists transition from empty to non-empty both before
                // and after we attempt the steal in case we don't get an item:
                //
                // If we don't get an item, we need to handle the possibility of a race condition that led to
                // an item being added to a list after we already looked at it in a way that breaks
                // linearizability.  For example, say there are three threads 0, 1, and 2, each with their own
                // list that's currently empty.  We could then have the following series of operations:
                // - Thread 2 adds an item, such that there's now 1 item in the bag.
                // - Thread 1 sees that the count is 1 and does a Take. Its local list is empty, so it tries to
                //   steal from list 0, but it's empty.  Before it can steal from Thread 2, it's pre-empted.
                // - Thread 0 adds an item.  The count is now 2.
                // - Thread 2 takes an item, which comes from its local queue.  The count is now 1.
                // - Thread 1 continues to try to steal from 2, finds it's empty, and fails its take, even though
                //   at any given time during its take the count was >= 1.  Oops.
                // This is particularly problematic for wrapper types that track count using their own synchronization,
                // e.g. BlockingCollection, and thus expect that a take will always be successful if the number of items
                // is known to be > 0.
                //
                // We work around this by looking at the number of times any list transitions from == 0 to > 0,
                // checking that before and after the steal attempts.  We don't care about > 0 to > 0 transitions,
                // because a steal from a list with > 0 elements would have been successful.
                long initialEmptyToNonEmptyCounts = Interlocked.Read(ref _emptyToNonEmptyListTransitionCount);

                // If there's no local queue for this thread, just start from the head queue
                // and try to steal from each queue until we get a result. If there is a local queue from this thread,
                // then start from the next queue after it, and then iterate around back from the head to this queue,
                // not including it.
                WorkStealingQueue? localQueue = GetCurrentThreadWorkStealingQueue(forceCreate: false);
                if (localQueue is null ?
                    TryStealFromTo(_workStealingQueues, null, out result, take) :
                    (TryStealFromTo(localQueue._nextQueue, null, out result, take) || TryStealFromTo(_workStealingQueues, localQueue, out result, take)))
                {
                    return true;
                }

                if (Interlocked.Read(ref _emptyToNonEmptyListTransitionCount) == initialEmptyToNonEmptyCounts)
                {
                    // The version number matched, so we didn't get an item and we're confident enough
                    // in our steal attempt to say so.
                    return false;
                }

                // Some list transitioned from empty to non-empty between just before the steal and now.
                // Since we don't know if it caused a race condition like the above description, we
                // have little choice but to try to steal again.
            }
        }

        /// <summary>
        /// Attempts to steal from each queue starting from <paramref name="startInclusive"/> to <paramref name="endExclusive"/>.
        /// </summary>
        private static bool TryStealFromTo(WorkStealingQueue? startInclusive, WorkStealingQueue? endExclusive, [MaybeNullWhen(false)] out T result, bool take)
        {
            for (WorkStealingQueue? queue = startInclusive; queue != endExclusive; queue = queue._nextQueue)
            {
                if (queue!.TrySteal(out result, take))
                {
                    return true;
                }
            }

            result = default(T);
            return false;
        }

        /// <summary>
        /// Copies the <see cref="ConcurrentBag{T}"/> elements to an existing
        /// one-dimensional <see cref="System.Array">Array</see>, starting at the specified array
        /// index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="System.Array">Array</see> that is the
        /// destination of the elements copied from the
        /// <see cref="ConcurrentBag{T}"/>. The <see
        /// cref="System.Array">Array</see> must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying
        /// begins.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is a null reference (Nothing in
        /// Visual Basic).</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than
        /// zero.</exception>
        /// <exception cref="ArgumentException"><paramref name="index"/> is equal to or greater than the
        /// length of the <paramref name="array"/>
        /// -or- the number of elements in the source <see
        /// cref="ConcurrentBag{T}"/> is greater than the available space from
        /// <paramref name="index"/> to the end of the destination <paramref name="array"/>.</exception>
        public void CopyTo(T[] array, int index)
        {
            ArgumentNullException.ThrowIfNull(array);
            ArgumentOutOfRangeException.ThrowIfNegative(index);

            // Short path if the bag is empty
            if (_workStealingQueues == null)
            {
                return;
            }

            bool lockTaken = false;
            try
            {
                FreezeBag(ref lockTaken);

                // Make sure we won't go out of bounds on the array
                int count = DangerousCount;
                if (index > array.Length - count)
                {
                    throw new ArgumentException(SR.Collection_CopyTo_TooManyElems, nameof(index));
                }

                // Do the copy
                try
                {
                    int copied = CopyFromEachQueueToArray(array, index);
                    Debug.Assert(copied == count);
                }
                catch (ArrayTypeMismatchException e)
                {
                    // Propagate same exception as in desktop
                    throw new InvalidCastException(e.Message, e);
                }
            }
            finally
            {
                UnfreezeBag(lockTaken);
            }
        }

        /// <summary>Copies from each queue to the target array, starting at the specified index.</summary>
        private int CopyFromEachQueueToArray(T[] array, int index)
        {
            Debug.Assert(Monitor.IsEntered(GlobalQueuesLock));

            int i = index;
            for (WorkStealingQueue? queue = _workStealingQueues; queue != null; queue = queue._nextQueue)
            {
                i += queue.DangerousCopyTo(array, i);
            }
            return i - index;
        }

        /// <summary>
        /// Copies the elements of the <see cref="System.Collections.ICollection"/> to an <see
        /// cref="System.Array"/>, starting at a particular
        /// <see cref="System.Array"/> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="System.Array">Array</see> that is the
        /// destination of the elements copied from the
        /// <see cref="ConcurrentBag{T}"/>. The <see
        /// cref="System.Array">Array</see> must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying
        /// begins.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is a null reference (Nothing in
        /// Visual Basic).</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than
        /// zero.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="array"/> is multidimensional. -or-
        /// <paramref name="array"/> does not have zero-based indexing. -or-
        /// <paramref name="index"/> is equal to or greater than the length of the <paramref name="array"/>
        /// -or- The number of elements in the source <see cref="System.Collections.ICollection"/> is
        /// greater than the available space from <paramref name="index"/> to the end of the destination
        /// <paramref name="array"/>. -or- The type of the source <see
        /// cref="System.Collections.ICollection"/> cannot be cast automatically to the type of the
        /// destination <paramref name="array"/>.
        /// </exception>
        void ICollection.CopyTo(Array array, int index)
        {
            // If the destination is actually a T[], use the strongly-typed
            // overload that doesn't allocate/copy an extra array.
            T[]? szArray = array as T[];
            if (szArray != null)
            {
                CopyTo(szArray, index);
                return;
            }

            // Otherwise, fall back to first storing the contents to an array,
            // and then relying on its CopyTo to copy to the target Array.
            ArgumentNullException.ThrowIfNull(array);
            ToArray().CopyTo(array, index);
        }

        /// <summary>
        /// Copies the <see cref="ConcurrentBag{T}"/> elements to a new array.
        /// </summary>
        /// <returns>A new array containing a snapshot of elements copied from the <see
        /// cref="ConcurrentBag{T}"/>.</returns>
        public T[] ToArray()
        {
            if (_workStealingQueues != null)
            {
                bool lockTaken = false;
                try
                {
                    FreezeBag(ref lockTaken);

                    int count = DangerousCount;
                    if (count > 0)
                    {
                        var arr = new T[count];
                        int copied = CopyFromEachQueueToArray(arr, 0);
                        Debug.Assert(copied == count);
                        return arr;
                    }
                }
                finally
                {
                    UnfreezeBag(lockTaken);
                }
            }

            // Bag was empty
            return Array.Empty<T>();
        }

        /// <summary>
        /// Removes all values from the <see cref="ConcurrentBag{T}"/>.
        /// </summary>
        public void Clear()
        {
            // If there are no queues in the bag, there's nothing to clear.
            if (_workStealingQueues == null)
            {
                return;
            }

            // Clear the local queue.
            WorkStealingQueue? local = GetCurrentThreadWorkStealingQueue(forceCreate: false);
            if (local != null)
            {
                local.LocalClear();
                if (local._nextQueue == null && local == _workStealingQueues)
                {
                    // If it's the only queue, nothing more to do.
                    return;
                }
            }

            // Clear the other queues by stealing all remaining items. We freeze the bag to
            // avoid having to contend with too many new items being added while we're trying
            // to drain the bag. But we can't just freeze the bag and attempt to remove all
            // items from every other queue, as even with freezing the bag it's dangerous to
            // manipulate other queues' tail pointers and add/take counts.
            bool lockTaken = false;
            try
            {
                FreezeBag(ref lockTaken);
                for (WorkStealingQueue? queue = _workStealingQueues; queue != null; queue = queue._nextQueue)
                {
                    T? ignored;
                    while (queue.TrySteal(out ignored, take: true)) ;
                }
            }
            finally
            {
                UnfreezeBag(lockTaken);
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see
        /// cref="ConcurrentBag{T}"/>.
        /// </summary>
        /// <returns>An enumerator for the contents of the <see
        /// cref="ConcurrentBag{T}"/>.</returns>
        /// <remarks>
        /// The enumeration represents a moment-in-time snapshot of the contents
        /// of the bag.  It does not reflect any updates to the collection after
        /// <see cref="GetEnumerator"/> was called.  The enumerator is safe to use
        /// concurrently with reads from and writes to the bag.
        /// </remarks>
        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)ToArray()).GetEnumerator();

        /// <summary>
        /// Returns an enumerator that iterates through the <see
        /// cref="ConcurrentBag{T}"/>.
        /// </summary>
        /// <returns>An enumerator for the contents of the <see
        /// cref="ConcurrentBag{T}"/>.</returns>
        /// <remarks>
        /// The items enumerated represent a moment-in-time snapshot of the contents
        /// of the bag.  It does not reflect any update to the collection after
        /// <see cref="GetEnumerator"/> was called.
        /// </remarks>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Gets the number of elements contained in the <see cref="ConcurrentBag{T}"/>.
        /// </summary>
        /// <value>The number of elements contained in the <see cref="ConcurrentBag{T}"/>.</value>
        /// <remarks>
        /// The count returned represents a moment-in-time snapshot of the contents
        /// of the bag.  It does not reflect any updates to the collection after
        /// <see cref="GetEnumerator"/> was called.
        /// </remarks>
        public int Count
        {
            get
            {
                // Short path if the bag is empty
                if (_workStealingQueues == null)
                {
                    return 0;
                }

                bool lockTaken = false;
                try
                {
                    FreezeBag(ref lockTaken);
                    return DangerousCount;
                }
                finally
                {
                    UnfreezeBag(lockTaken);
                }
            }
        }

        /// <summary>Gets the number of items stored in the bag.</summary>
        /// <remarks>Only provides a stable result when the bag is frozen.</remarks>
        private int DangerousCount
        {
            get
            {
                int count = 0;
                for (WorkStealingQueue? queue = _workStealingQueues; queue != null; queue = queue._nextQueue)
                {
                    checked { count += queue.DangerousCount; }
                }

                Debug.Assert(count >= 0);
                return count;
            }
        }

        /// <summary>
        /// Gets a value that indicates whether the <see cref="ConcurrentBag{T}"/> is empty.
        /// </summary>
        /// <value>true if the <see cref="ConcurrentBag{T}"/> is empty; otherwise, false.</value>
        public bool IsEmpty
        {
            get
            {
                // Fast-path based on the current thread's local queue.
                WorkStealingQueue? local = GetCurrentThreadWorkStealingQueue(forceCreate: false);
                if (local != null)
                {
                    // We don't need the lock to check the local queue, as no other thread
                    // could be adding to it, and a concurrent steal that transitions from
                    // non-empty to empty doesn't matter because if we see this as non-empty,
                    // then that's a valid moment-in-time answer, and if we see this as empty,
                    // we check other things.
                    if (!local.IsEmpty)
                    {
                        return false;
                    }

                    // We know the local queue is empty (no one besides this thread could have
                    // added to it since we checked).  If the local queue is the only one
                    // in the bag, then the bag is empty, too.
                    if (local._nextQueue == null && local == _workStealingQueues)
                    {
                        return true;
                    }
                }

                // Couldn't take a fast path. Freeze the bag, and enumerate the queues to see if
                // any is non-empty.
                bool lockTaken = false;
                try
                {
                    FreezeBag(ref lockTaken);
                    for (WorkStealingQueue? queue = _workStealingQueues; queue != null; queue = queue._nextQueue)
                    {
                        if (!queue.IsEmpty)
                        {
                            return false;
                        }
                    }
                }
                finally
                {
                    UnfreezeBag(lockTaken);
                }

                // All queues were empty, so the bag was empty.
                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether access to the <see cref="System.Collections.ICollection"/> is
        /// synchronized with the SyncRoot.
        /// </summary>
        /// <value>true if access to the <see cref="System.Collections.ICollection"/> is synchronized
        /// with the SyncRoot; otherwise, false. For <see cref="ConcurrentBag{T}"/>, this property always
        /// returns false.</value>
        bool ICollection.IsSynchronized => false;

        /// <summary>
        /// Gets an object that can be used to synchronize access to the <see
        /// cref="System.Collections.ICollection"/>. This property is not supported.
        /// </summary>
        /// <exception cref="System.NotSupportedException">The SyncRoot property is not supported.</exception>
        object ICollection.SyncRoot
        {
            get { throw new NotSupportedException(SR.ConcurrentCollection_SyncRoot_NotSupported); }
        }

        /// <summary>Global lock used to synchronize the queues pointer and all bag-wide operations (e.g. ToArray, Count, etc.).</summary>
        private object GlobalQueuesLock
        {
            get
            {
                Debug.Assert(_locals != null);
                return _locals;
            }
        }

        /// <summary>"Freezes" the bag, such that no concurrent operations will be mutating the bag when it returns.</summary>
        /// <param name="lockTaken">true if the global lock was taken; otherwise, false.</param>
        private void FreezeBag(ref bool lockTaken)
        {
            // Take the global lock to start freezing the bag.  This helps, for example,
            // to prevent other threads from joining the bag (adding their local queues)
            // while a global operation is in progress.
            Debug.Assert(!Monitor.IsEntered(GlobalQueuesLock));
            Monitor.Enter(GlobalQueuesLock, ref lockTaken);
            WorkStealingQueue? head = _workStealingQueues; // stable at least until GlobalQueuesLock is released in UnfreezeBag

            // Then acquire all local queue locks, noting on each that it's been taken.
            for (WorkStealingQueue? queue = head; queue != null; queue = queue._nextQueue)
            {
                Monitor.Enter(queue, ref queue._frozen);
            }
        }

        /// <summary>"Unfreezes" a bag frozen with <see cref="FreezeBag(ref bool)"/>.</summary>
        /// <param name="lockTaken">The result of the <see cref="FreezeBag(ref bool)"/> method.</param>
        private void UnfreezeBag(bool lockTaken)
        {
            Debug.Assert(Monitor.IsEntered(GlobalQueuesLock) == lockTaken);
            if (lockTaken)
            {
                // Release all of the individual queue locks.
                for (WorkStealingQueue? queue = _workStealingQueues; queue != null; queue = queue._nextQueue)
                {
                    if (queue._frozen)
                    {
                        queue._frozen = false;
                        Monitor.Exit(queue);
                    }
                }

                // Then release the global lock.
                Monitor.Exit(GlobalQueuesLock);
            }
        }

        /// <summary>Provides a work-stealing queue data structure stored per thread.</summary>
        private sealed class WorkStealingQueue
        {
            /// <summary>Initial size of the queue's array.</summary>
            private const int InitialSize = 32;
            /// <summary>Top index (steal end). Modified by stealers via CAS.</summary>
            private long _top;
            /// <summary>Bottom index (push/pop end). Modified only by owner.</summary>
            private long _bottom;
            /// <summary>The array storing the queue's data.</summary>
            private volatile T[] _array = new T[InitialSize];
            /// <summary>Mask and'd with indices to get an index into <see cref="_array"/>.</summary>
            private volatile int _mask = InitialSize - 1;
            /// <summary>true if this queue's lock is held as part of a global freeze.</summary>
            internal bool _frozen;
            /// <summary>Next queue in the <see cref="ConcurrentBag{T}"/>'s set of thread-local queues.</summary>
            internal readonly WorkStealingQueue? _nextQueue;
            /// <summary>Thread ID that owns this queue.</summary>
            internal readonly int _ownerThreadId;

            /// <summary>Initialize the WorkStealingQueue.</summary>
            /// <param name="nextQueue">The next queue in the linked list of work-stealing queues.</param>
            internal WorkStealingQueue(WorkStealingQueue? nextQueue)
            {
                _ownerThreadId = Environment.CurrentManagedThreadId;
                _nextQueue = nextQueue;
            }

            /// <summary>Gets whether the queue is empty.</summary>
            internal bool IsEmpty
            {
                get
                {
                    long b = _bottom;
                    long t = Volatile.Read(ref _top);
                    return t >= b;
                }
            }

            /// <summary>
            /// Add new item to the tail of the queue.
            /// </summary>
            /// <param name="item">The item to add.</param>
            /// <param name="emptyToNonEmptyListTransitionCount"></param>
            internal void LocalPush(T item, ref long emptyToNonEmptyListTransitionCount)
            {
                Debug.Assert(Environment.CurrentManagedThreadId == _ownerThreadId);

                long b = _bottom;
                long t = Volatile.Read(ref _top);
                T[] arr = _array;
                int mask = _mask;

                long size = b - t;

                // Check if we need to grow the array or handle small queue case
                if (size >= mask || size <= 0)
                {
                    lock (this)
                    {
                        // Re-read after lock
                        t = Volatile.Read(ref _top);
                        b = _bottom;
                        arr = _array;
                        mask = _mask;
                        size = b - t;

                        // If we're full, expand the array
                        if (size >= mask)
                        {
                            // Grow the array by doubling its size
                            var newArray = new T[arr.Length << 1];
                            for (long i = t; i < b; i++)
                            {
                                newArray[i & (newArray.Length - 1)] = arr[i & mask];
                            }

                            // Clear old array
                            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                            {
                                Array.Clear(arr);
                            }

                            _array = arr = newArray;
                            _mask = mask = newArray.Length - 1;
                        }

                        // Store the item
                        arr[b & mask] = item;

                        // Ensure the item is visible before bottom is updated
                        Thread.MemoryBarrier();
                        _bottom = b + 1;

                        // Track empty-to-non-empty transition
                        if (size == 0)
                        {
                            Interlocked.Increment(ref emptyToNonEmptyListTransitionCount);
                        }
                        return;
                    }
                }

                // Fast path: multiple items already present, no lock needed
                arr[b & mask] = item;
                Thread.MemoryBarrier();
                _bottom = b + 1;
            }

            /// <summary>Clears the contents of the local queue.</summary>
            internal void LocalClear()
            {
                Debug.Assert(Environment.CurrentManagedThreadId == _ownerThreadId);
                lock (this) // synchronize with steals
                {
                    long t = Volatile.Read(ref _top);
                    long b = _bottom;

                    // If the queue isn't empty, reset the state to clear out all items.
                    if (t < b)
                    {
                        // Clear the references
                        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                        {
                            T[] arr = _array;
                            int mask = _mask;
                            for (long i = t; i < b; i++)
                            {
                                arr[i & mask] = default!;
                            }
                        }

                        _bottom = t;
                    }
                }
            }

            /// <summary>Remove an item from the tail of the queue.</summary>
            /// <param name="result">The removed item</param>
            internal bool TryLocalPop([MaybeNullWhen(false)] out T result)
            {
                Debug.Assert(Environment.CurrentManagedThreadId == _ownerThreadId);

                long b = _bottom - 1;
                T[] arr = _array;
                _bottom = b;
                Thread.MemoryBarrier();
                long t = Volatile.Read(ref _top);

                if (t <= b)
                {
                    // Non-empty queue
                    result = arr[b & _mask];
                    if (t == b)
                    {
                        // Last element - need to compete with stealers using lock
                        lock (this)
                        {
                            t = Volatile.Read(ref _top);
                            if (t <= b)
                            {
                                // Successfully got the element, increment top
                                _top = t + 1;
                                _bottom = t + 1;
                                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                                {
                                    arr[b & _mask] = default!;
                                }
                                return true;
                            }
                            // Lost race to stealer
                            _bottom = t;
                            result = default!;
                            return false;
                        }
                    }

                    // Clear the reference
                    if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                    {
                        arr[b & _mask] = default!;
                    }
                    return true;
                }

                // Empty queue
                _bottom = t;
                result = default!;
                return false;
            }

            /// <summary>Peek an item from the tail of the queue.</summary>
            /// <param name="result">the peeked item</param>
            /// <returns>True if succeeded, false otherwise</returns>
            internal bool TryLocalPeek([MaybeNullWhen(false)] out T result)
            {
                Debug.Assert(Environment.CurrentManagedThreadId == _ownerThreadId);

                long b = _bottom;
                long t = Volatile.Read(ref _top);
                if (t < b)
                {
                    // Use lock to safely peek at the element
                    lock (this)
                    {
                        b = _bottom;
                        t = Volatile.Read(ref _top);
                        if (t < b)
                        {
                            result = _array[(b - 1) & _mask];
                            return true;
                        }
                    }
                }

                result = default!;
                return false;
            }

            /// <summary>Steal an item from the head of the queue.</summary>
            /// <param name="result">the removed item</param>
            /// <param name="take">true to take the item; false to simply peek at it</param>
            internal bool TrySteal([MaybeNullWhen(false)] out T result, bool take)
            {
                // Use lock to synchronize with LocalPush when queue might be empty/small
                // This is necessary for correct empty-to-non-empty transition tracking
                lock (this)
                {
                    long t = Volatile.Read(ref _top);
                    long b = _bottom;

                    if (t < b)
                    {
                        // Non-empty queue
                        T[] arr = _array;
                        result = arr[t & _mask];

                        if (take)
                        {
                            // Increment top to steal the element
                            _top = t + 1;

                            // Clear the reference
                            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                            {
                                arr[t & _mask] = default!;
                            }
                        }
                        return true;
                    }

                    // Empty queue
                    result = default!;
                    return false;
                }
            }

            /// <summary>Copies the contents of this queue to the target array starting at the specified index.</summary>
            internal int DangerousCopyTo(T[] array, int arrayIndex)
            {
                Debug.Assert(Monitor.IsEntered(this));
                Debug.Assert(_frozen);
                Debug.Assert(array != null);
                Debug.Assert(arrayIndex >= 0 && arrayIndex <= array.Length);

                long t = Volatile.Read(ref _top);
                long b = _bottom;
                long size = b - t;

                // Handle case where bottom was temporarily decremented by TryLocalPop before it noticed we're frozen
                if (size <= 0)
                {
                    return 0;
                }

                int count = (int)size;
                int available = array.Length - arrayIndex;
                if (count > available)
                {
                    // Cap count to available space - this can happen due to races with TryLocalPop
                    count = available;
                }

                T[] arr = _array;
                int mask = _mask;

                // Copy from this queue's array to the destination array, but in reverse
                // order to match the ordering of desktop.
                for (int i = arrayIndex + count - 1; i >= arrayIndex; i--)
                {
                    array[i] = arr[t++ & mask];
                }

                return count;
            }

            /// <summary>Gets the total number of items in the queue.</summary>
            /// <remarks>
            /// This is not thread safe, only providing an accurate result either from the owning
            /// thread while its lock is held or from any thread while the bag is frozen.
            /// </remarks>
            internal int DangerousCount
            {
                get
                {
                    Debug.Assert(Monitor.IsEntered(this));
                    long t = Volatile.Read(ref _top);
                    long b = _bottom;
                    long size = b - t;
                    // Handle case where bottom was temporarily decremented by TryLocalPop before it noticed we're frozen
                    int count = size >= 0 ? (int)size : 0;
                    return count;
                }
            }
        }
    }
}
