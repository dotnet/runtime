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
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection), SR.ConcurrentBag_Ctor_ArgumentNullException);
            }

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
                bool gotItem = localQueue == null ?
                    TryStealFromTo(_workStealingQueues, null, out result, take) :
                    (TryStealFromTo(localQueue._nextQueue, null, out result, take) || TryStealFromTo(_workStealingQueues, localQueue, out result, take));
                if (gotItem)
                {
#pragma warning disable CS8762
                    // https://github.com/dotnet/runtime/issues/36132
                    // Compiler can't automatically deduce that nullability constraints
                    // for 'result' are satisfied at this exit point.
                    return true;
#pragma warning restore CS8762
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
        private bool TryStealFromTo(WorkStealingQueue? startInclusive, WorkStealingQueue? endExclusive, [MaybeNullWhen(false)] out T result, bool take)
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
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array), SR.ConcurrentBag_CopyTo_ArgumentNullException);
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), SR.Collection_CopyTo_ArgumentOutOfRangeException);
            }

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
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array), SR.ConcurrentBag_CopyTo_ArgumentNullException);
            }
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
                    while (queue.TrySteal(out ignored, take: true));
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
        public IEnumerator<T> GetEnumerator() => new Enumerator(ToArray());

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
            Interlocked.MemoryBarrier(); // prevent reads of _currentOp from moving before writes to _frozen

            // Finally, wait for all unsynchronized operations on each queue to be done.
            for (WorkStealingQueue? queue = head; queue != null; queue = queue._nextQueue)
            {
                if (queue._currentOp != (int)Operation.None)
                {
                    SpinWait spinner = default;
                    do { spinner.SpinOnce(); }
                    while (queue._currentOp != (int)Operation.None);
                }
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
            /// <summary>Starting index for the head and tail indices.</summary>
            private const int StartIndex =
#if DEBUG
                int.MaxValue; // in debug builds, start at the end so we exercise the index reset logic
#else
                0;
#endif
            /// <summary>Head index from which to steal.  This and'd with the <see cref="_mask"/> is the index into <see cref="_array"/>.</summary>
            private volatile int _headIndex = StartIndex;
            /// <summary>Tail index at which local pushes/pops happen. This and'd with the <see cref="_mask"/> is the index into <see cref="_array"/>.</summary>
            private volatile int _tailIndex = StartIndex;
            /// <summary>The array storing the queue's data.</summary>
            private volatile T[] _array = new T[InitialSize];
            /// <summary>Mask and'd with <see cref="_headIndex"/> and <see cref="_tailIndex"/> to get an index into <see cref="_array"/>.</summary>
            private volatile int _mask = InitialSize - 1;
            /// <summary>Numbers of elements in the queue from the local perspective; needs to be combined with <see cref="_stealCount"/> to get an actual Count.</summary>
            private int _addTakeCount;
            /// <summary>Number of steals; needs to be combined with <see cref="_addTakeCount"/> to get an actual Count.</summary>
            private int _stealCount;
            /// <summary>The current queue operation. Used to quiesce before performing operations from one thread onto another.</summary>
            internal volatile int _currentOp;
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
                    // _tailIndex can be decremented even while the bag is frozen, as the decrement in TryLocalPop happens prior
                    // to the check for _frozen.  But that's ok, as if _tailIndex is being decremented such that _headIndex becomes
                    // >= _tailIndex, then the queue is about to be empty.  This does mean, though, that while holding the lock,
                    // it is possible to observe Count == 1 but IsEmpty == true.  As such, we simply need to avoid doing any operation
                    // while the bag is frozen that requires those values to be consistent.
                    return _headIndex - _tailIndex >= 0;
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
                bool lockTaken = false;
                try
                {
                    // Full fence to ensure subsequent reads don't get reordered before this
                    Interlocked.Exchange(ref _currentOp, (int)Operation.Add);
                    int tail = _tailIndex;

                    // Rare corner case (at most once every 2 billion pushes on this thread):
                    // We're going to increment the tail; if we'll overflow, then we need to reset our counts
                    if (tail == int.MaxValue)
                    {
                        _currentOp = (int)Operation.None; // set back to None temporarily to avoid a deadlock
                        lock (this)
                        {
                            Debug.Assert(_tailIndex == tail, "No other thread should be changing _tailIndex");

                            // Rather than resetting to zero, we'll just mask off the bits we don't care about.
                            // This way we don't need to rearrange the items already in the queue; they'll be found
                            // correctly exactly where they are.  One subtlety here is that we need to make sure that
                            // if head is currently < tail, it remains that way.  This happens to just fall out from
                            // the bit-masking, because we only do this if tail == int.MaxValue, meaning that all
                            // bits are set, so all of the bits we're keeping will also be set.  Thus it's impossible
                            // for the head to end up > than the tail, since you can't set any more bits than all of them.
                            _headIndex = _headIndex & _mask;
                            _tailIndex = tail = tail & _mask;
                            Debug.Assert(_headIndex - _tailIndex <= 0);

                            Interlocked.Exchange(ref _currentOp, (int)Operation.Add); // ensure subsequent reads aren't reordered before this
                        }
                    }

                    // We'd like to take the fast path that doesn't require locking, if possible. It's not possible if:
                    // - another thread is currently requesting that the whole bag synchronize, e.g. a ToArray operation
                    // - if there are fewer than two spaces available.  One space is necessary for obvious reasons:
                    //   to store the element we're trying to push.  The other is necessary due to synchronization with steals.
                    //   A stealing thread first increments _headIndex to reserve the slot at its old value, and then tries to
                    //   read from that slot.  We could potentially have a race condition whereby _headIndex is incremented just
                    //   before this check, in which case we could overwrite the element being stolen as that slot would appear
                    //   to be empty.  Thus, we only allow the fast path if there are two empty slots.
                    // - if there <= 1 elements in the list.  We need to be able to successfully track transitions from
                    //   empty to non-empty in a way that other threads can check, and we can only do that tracking
                    //   correctly if we synchronize with steals when it's possible a steal could take the last item
                    //   in the list just as we're adding.  It's possible at this point that there's currently an active steal
                    //   operation happening but that it hasn't yet incremented the head index, such that we could read a smaller
                    //   than accurate by 1 value for the head.  However, only one steal could possibly be doing so, as steals
                    //   take the lock, and another steal couldn't then increment the header further because it'll see that
                    //   there's currently an add operation in progress and wait until the add completes.
                    int head = _headIndex; // read after _currentOp set to Add
                    if (!_frozen && (head - (tail - 1) < 0) && (tail - (head + _mask) < 0))
                    {
                        _array[tail & _mask] = item;
                        _tailIndex = tail + 1;
                    }
                    else
                    {
                        // We need to contend with foreign operations (e.g. steals, enumeration, etc.), so we lock.
                        _currentOp = (int)Operation.None; // set back to None to avoid a deadlock
                        Monitor.Enter(this, ref lockTaken);

                        head = _headIndex;
                        int count = tail - head; // this count is stable, as we're holding the lock

                        // If we're full, expand the array.
                        if (count >= _mask)
                        {
                            // Expand the queue by doubling its size.
                            var newArray = new T[_array.Length << 1];
                            int headIdx = head & _mask;
                            if (headIdx == 0)
                            {
                                Array.Copy(_array, newArray, _array.Length);
                            }
                            else
                            {
                                Array.Copy(_array, headIdx, newArray, 0, _array.Length - headIdx);
                                Array.Copy(_array, 0, newArray, _array.Length - headIdx, headIdx);
                            }

                            // Reset the field values
                            _array = newArray;
                            _headIndex = 0;
                            _tailIndex = tail = count;
                            _mask = (_mask << 1) | 1;
                        }

                        // Add the element
                        _array[tail & _mask] = item;
                        _tailIndex = tail + 1;

                        // Now that the item has been added, if we were at 0 (now at 1) item,
                        // increase the empty to non-empty transition count.
                        if (count == 0)
                        {
                            // We just transitioned from empty to non-empty, so increment the transition count.
                            Interlocked.Increment(ref emptyToNonEmptyListTransitionCount);
                        }

                        // Update the count to avoid overflow.  We can trust _stealCount here,
                        // as we're inside the lock and it's only manipulated there.
                        _addTakeCount -= _stealCount;
                        _stealCount = 0;
                    }

                    // Increment the count from the add/take perspective
                    checked { _addTakeCount++; }
                }
                finally
                {
                    _currentOp = (int)Operation.None;
                    if (lockTaken)
                    {
                        Monitor.Exit(this);
                    }
                }
            }

            /// <summary>Clears the contents of the local queue.</summary>
            internal void LocalClear()
            {
                Debug.Assert(Environment.CurrentManagedThreadId == _ownerThreadId);
                lock (this) // synchronize with steals
                {
                    // If the queue isn't empty, reset the state to clear out all items.
                    if (_headIndex - _tailIndex < 0)
                    {
                        _headIndex = _tailIndex = StartIndex;
                        _addTakeCount = _stealCount = 0;
                        Array.Clear(_array);
                    }
                }
            }

            /// <summary>Remove an item from the tail of the queue.</summary>
            /// <param name="result">The removed item</param>
            internal bool TryLocalPop([MaybeNullWhen(false)] out T result)
            {
                Debug.Assert(Environment.CurrentManagedThreadId == _ownerThreadId);

                int tail = _tailIndex;
                if (_headIndex - tail >= 0)
                {
                    result = default(T);
                    return false;
                }

                bool lockTaken = false;
                try
                {
                    // Decrement the tail using a full fence to ensure subsequent reads don't reorder before this.
                    // If the read of _headIndex moved before this write to _tailIndex, we could erroneously end up
                    // popping an element that's concurrently being stolen, leading to the same element being
                    // dequeued from the bag twice.
                    _currentOp = (int)Operation.Take;
                    Interlocked.Exchange(ref _tailIndex, --tail);

                    // If there is no interaction with a steal, we can head down the fast path.
                    // Note that we use _headIndex < tail rather than _headIndex <= tail to account
                    // for stealing peeks, which don't increment _headIndex, and which could observe
                    // the written default(T) in a race condition to peek at the element.
                    if (!_frozen && (_headIndex - tail < 0))
                    {
                        int idx = tail & _mask;
                        result = _array[idx];
                        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                        {
                            _array[idx] = default(T)!;
                        }
                        _addTakeCount--;
                        return true;
                    }
                    else
                    {
                        // Interaction with steals: 0 or 1 elements left.
                        _currentOp = (int)Operation.None; // set back to None to avoid a deadlock
                        Monitor.Enter(this, ref lockTaken);
                        if (_headIndex - tail <= 0)
                        {
                            // Element still available. Take it.
                            int idx = tail & _mask;
                            result = _array[idx];
                            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                            {
                                _array[idx] = default(T)!;
                            }
                            _addTakeCount--;
                            return true;
                        }
                        else
                        {
                            // We encountered a race condition and the element was stolen, restore the tail.
                            _tailIndex = tail + 1;
                            result = default(T);
                            return false;
                        }
                    }
                }
                finally
                {
                    _currentOp = (int)Operation.None;
                    if (lockTaken)
                    {
                        Monitor.Exit(this);
                    }
                }
            }

            /// <summary>Peek an item from the tail of the queue.</summary>
            /// <param name="result">the peeked item</param>
            /// <returns>True if succeeded, false otherwise</returns>
            internal bool TryLocalPeek([MaybeNullWhen(false)] out T result)
            {
                Debug.Assert(Environment.CurrentManagedThreadId == _ownerThreadId);

                int tail = _tailIndex;
                if (_headIndex - tail < 0)
                {
                    // It is possible to enable lock-free peeks, following the same general approach
                    // that's used in TryLocalPop.  However, peeks are more complicated as we can't
                    // do the same kind of index reservation that's done in TryLocalPop; doing so could
                    // end up making a steal think that no item is available, even when one is. To do
                    // it correctly, then, we'd need to add spinning to TrySteal in case of a concurrent
                    // peek happening. With a lock, the common case (no contention with steals) will
                    // effectively only incur two interlocked operations (entering/exiting the lock) instead
                    // of one (setting Peek as the _currentOp).  Combined with Peeks on a bag being rare,
                    // for now we'll use the simpler/safer code.
                    lock (this)
                    {
                        if (_headIndex - tail < 0)
                        {
                            result = _array[(tail - 1) & _mask];
                            return true;
                        }
                    }
                }

                result = default(T);
                return false;
            }

            /// <summary>Steal an item from the head of the queue.</summary>
            /// <param name="result">the removed item</param>
            /// <param name="take">true to take the item; false to simply peek at it</param>
            internal bool TrySteal([MaybeNullWhen(false)] out T result, bool take)
            {
                lock (this)
                {
                    int head = _headIndex; // _headIndex is only manipulated under the lock
                    if (take)
                    {
                        // If there are <= 2 items in the list, we need to ensure no add operation
                        // is in progress, as add operations need to accurately count transitions
                        // from empty to non-empty, and they can only do that if there are no concurrent
                        // steal operations happening at the time.
                        if ((head - (_tailIndex - 2) >= 0) && _currentOp == (int)Operation.Add)
                        {
                            SpinWait spinner = default;
                            do
                            {
                                spinner.SpinOnce();
                            }
                            while (_currentOp == (int)Operation.Add);
                        }

                        // Increment head to tentatively take an element: a full fence is used to ensure the read
                        // of _tailIndex doesn't move earlier, as otherwise we could potentially end up stealing
                        // the same element that's being popped locally.
                        Interlocked.Exchange(ref _headIndex, unchecked(head + 1));

                        // If there's an element to steal, do it.
                        if (head < _tailIndex)
                        {
                            int idx = head & _mask;
                            result = _array[idx];
                            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                            {
                                _array[idx] = default(T)!;
                            }
                            _stealCount++;
                            return true;
                        }
                        else
                        {
                            // We contended with the local thread and lost the race, so restore the head.
                            _headIndex = head;
                        }
                    }
                    else if (head < _tailIndex)
                    {
                        // Peek, if there's an element available
                        result = _array[head & _mask];
                        return true;
                    }
                }

                // The queue was empty.
                result = default(T);
                return false;
            }

            /// <summary>Copies the contents of this queue to the target array starting at the specified index.</summary>
            internal int DangerousCopyTo(T[] array, int arrayIndex)
            {
                Debug.Assert(Monitor.IsEntered(this));
                Debug.Assert(_frozen);
                Debug.Assert(array != null);
                Debug.Assert(arrayIndex >= 0 && arrayIndex <= array.Length);

                int headIndex = _headIndex;
                int count = DangerousCount;
                Debug.Assert(
                    count == (_tailIndex - _headIndex) ||
                    count == (_tailIndex + 1 - _headIndex),
                    "Count should be the same as tail - head, but allowing for the possibility that " +
                    "a peek decremented _tailIndex before seeing that a freeze was happening.");
                Debug.Assert(arrayIndex <= array.Length - count);

                // Copy from this queue's array to the destination array, but in reverse
                // order to match the ordering of desktop.
                for (int i = arrayIndex + count - 1; i >= arrayIndex; i--)
                {
                    array[i] = _array[headIndex++ & _mask];
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
                    int stealCount = _stealCount;
                    int addTakeCount = _addTakeCount;
                    int count = addTakeCount - stealCount;
                    Debug.Assert(count >= 0, $"Expected _addTakeCount ({addTakeCount}) >= _stealCount ({stealCount}).");
                    return count;
                }
            }
        }

        /// <summary>Lock-free operations performed on a queue.</summary>
        internal enum Operation
        {
            None,
            Add,
            Take
        };

        /// <summary>Provides an enumerator for the bag.</summary>
        /// <remarks>
        /// The original implementation of ConcurrentBag used a <see cref="List{T}"/> as part of
        /// the GetEnumerator implementation.  That list was then changed to be an array, but array's
        /// GetEnumerator has different behavior than does list's, in particular for the case where
        /// Current is used after MoveNext returns false.  To avoid any concerns around compatibility,
        /// we use a custom enumerator rather than just returning array's. This enumerator provides
        /// the essential elements of both list's and array's enumerators.
        /// </remarks>
        private sealed class Enumerator : IEnumerator<T>
        {
            private readonly T[] _array;
            private T? _current;
            private int _index;

            public Enumerator(T[] array)
            {
                Debug.Assert(array != null);
                _array = array;
            }

            public bool MoveNext()
            {
                if (_index < _array.Length)
                {
                    _current = _array[_index++];
                    return true;
                }

                _index = _array.Length + 1;
                return false;
            }

            public T Current => _current!;

            object? IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || _index == _array.Length + 1)
                    {
                        throw new InvalidOperationException(SR.ConcurrentBag_Enumerator_EnumerationNotStartedOrAlreadyFinished);
                    }
                    return Current;
                }
            }

            public void Reset()
            {
                _index = 0;
                _current = default;
            }

            public void Dispose() { }
        }
    }
}
