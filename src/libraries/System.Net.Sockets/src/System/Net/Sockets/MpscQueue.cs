// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Net.Sockets
{
    /// <summary>
    /// Lock-free multi-producer, single-consumer queue optimized for the io_uring
    /// event loop pattern where many threads enqueue work items but exactly one
    /// thread drains them.
    ///
    /// Liveness contract:
    /// TryDequeue/IsEmpty may observe a producer between index claim and publish
    /// (Interlocked.Increment followed by Volatile.Write), and can transiently report
    /// no available item even though an enqueue is in progress. Callers must provide
    /// their own wakeup/progress mechanism after Enqueue.
    /// </summary>
    internal sealed class MpscQueue<T>
    {
        private const int DefaultSegmentSize = 256;
        private const int UnlinkedSegmentCacheCapacity = 4;
        private const int MaxEnqueueSlowAttempts = 2048;
#if DEBUG
        private static int s_testSegmentAllocationFailuresRemaining;
#endif

        private readonly int _segmentSize;
        private PaddedSegment _head;
        private PaddedSegment _tail;
        // Segment cache is shared by:
        // - unlinked segments that lost tail->next publication races, and
        // - drained head segments returned only after producer quiescence checks.
        // Cache bookkeeping is protected by a tiny lock because this path is already slow-path only.
        private readonly Lock _cachedUnlinkedSegmentGate = new Lock();
        private readonly Segment?[] _cachedUnlinkedSegments = new Segment?[UnlinkedSegmentCacheCapacity];
        private int _cachedUnlinkedSegmentCount;
        private int _activeEnqueueOperations;

        internal MpscQueue(int segmentSize = DefaultSegmentSize)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(segmentSize);
            _segmentSize = segmentSize;
            Segment initial = new Segment(segmentSize);
            _head.Value = initial;
            _tail.Value = initial;
        }

        /// <summary>
        /// Attempts to enqueue an item.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryEnqueue(T item)
        {
            if (TryEnqueueFast(item))
            {
                return true;
            }

            return TryEnqueueSlowWithProducerTracking(item);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool TryEnqueueSlowWithProducerTracking(T item)
        {
            // Only slow-path producers can retain stale segment references long enough to race with
            // drained-segment recycling. Fast-path success doesn't need this accounting.
            Interlocked.Increment(ref _activeEnqueueOperations);
            try
            {
                return TryEnqueueSlow(item);
            }
            finally
            {
                Interlocked.Decrement(ref _activeEnqueueOperations);
            }
        }

        /// <summary>
        /// Enqueues an item, retrying until an enqueue slot is observed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Enqueue(T item)
        {
            SpinWait spinner = default;
            while (!TryEnqueue(item))
            {
                spinner.SpinOnce();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryEnqueueFast(T item)
        {
            Segment tail = Volatile.Read(ref _tail.Value)!;
            T[] items = tail.Items;
            int[] states = tail.States;
            // Snapshot incarnation before claiming a slot. If the segment is recycled
            // between this read and the Interlocked.Increment, the incarnation will differ.
            int incarnation = Volatile.Read(ref tail.Incarnation);
            int index = Interlocked.Increment(ref tail.EnqueueIndex.Value) - 1;
            // A stale claim can over-increment the old segment index before incarnation
            // mismatch is detected; this is safe because ResetForReuse resets EnqueueIndex.
            if ((uint)index < (uint)states.Length)
            {
                // Verify segment was not recycled while we were claiming the slot.
                // A recycled segment has a different incarnation because ResetForReuse
                // increments it. Without this check, TryReturnDrainedSegmentToCache can
                // recycle the segment (since fast-path producers are not tracked by
                // _activeEnqueueOperations) and we would write into reused memory.
                if (Volatile.Read(ref tail.Incarnation) == incarnation)
                {
                    items[index] = item;
                    Volatile.Write(ref states[index], 1);
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool TryEnqueueSlow(T item)
        {
            SpinWait spinner = default;
            for (int attempt = 0; attempt < MaxEnqueueSlowAttempts; attempt++)
            {
                Segment tail = Volatile.Read(ref _tail.Value)!;
                T[] items = tail.Items;
                int[] states = tail.States;
                int index = Interlocked.Increment(ref tail.EnqueueIndex.Value) - 1;
                if ((uint)index < (uint)states.Length)
                {
                    items[index] = item;
                    Volatile.Write(ref states[index], 1);
                    return true;
                }

                Segment? next = Volatile.Read(ref tail.Next);
                if (next is null)
                {
                    Segment newSegment;
                    try
                    {
                        newSegment = RentUnlinkedSegment();
                    }
                    catch (OutOfMemoryException)
                    {
                        return false;
                    }

                    if (Interlocked.CompareExchange(ref tail.Next, newSegment, null) is null)
                    {
                        next = newSegment;
                    }
                    else
                    {
                        // Another producer linked its own segment first. Reuse ours later.
                        ReturnUnlinkedSegment(newSegment);
                        next = Volatile.Read(ref tail.Next);
                    }
                }

                if (next is not null)
                {
                    Interlocked.CompareExchange(ref _tail.Value, next, tail);
                }

                spinner.SpinOnce();
            }

            return false;
        }

        /// <summary>
        /// Attempts to dequeue an item. Must only be called by the single consumer thread.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryDequeue(out T item)
        {
            if (TryDequeueFast(out item))
            {
                return true;
            }

            return TryDequeueSlow(out item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryDequeueFromSegment(Segment head, out T item)
        {
            int[] states = head.States;
            int index = head.DequeueIndex;
            if ((uint)index >= (uint)states.Length)
            {
                item = default!;
                return false;
            }

            // Acquire published slot before reading the item value.
            if (Volatile.Read(ref states[index]) != 1)
            {
                item = default!;
                return false;
            }

            T[] items = head.Items;
            item = items[index];
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                items[index] = default!;
            }

            head.DequeueIndex = index + 1;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryDequeueFast(out T item)
        {
            Segment head = Volatile.Read(ref _head.Value)!;
            return TryDequeueFromSegment(head, out item);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool TryDequeueSlow(out T item)
        {
            Segment head = Volatile.Read(ref _head.Value)!;
            while ((uint)head.DequeueIndex >= (uint)head.States.Length)
            {
                Segment? next = Volatile.Read(ref head.Next);
                if (next is null)
                {
                    item = default!;
                    return false;
                }

                // Consumer publishes head advance; producers read _head when resolving slow-path
                // enqueue progress, so this store must be visible across cores.
                Volatile.Write(ref _head.Value, next);
                TryReturnDrainedSegmentToCache(head);
                head = next;
            }

            return TryDequeueFromSegment(head, out item);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void TryReturnDrainedSegmentToCache(Segment drainedSegment)
        {
            // Safe reuse requires producer quiescence and tail advancement away from this segment.
            // Without these checks, a producer that captured a stale segment pointer could publish
            // into a reset segment after it has been recycled.
            if (Volatile.Read(ref _activeEnqueueOperations) != 0 ||
                ReferenceEquals(Volatile.Read(ref _tail.Value), drainedSegment))
            {
                return;
            }

            ReturnUnlinkedSegment(drainedSegment);
        }

        /// <summary>
        /// Returns whether the queue currently appears empty (snapshot, not linearizable).
        /// A return value of <see langword="true"/> can also mean an enqueue is mid-flight.
        /// </summary>
        internal bool IsEmpty
        {
            get
            {
                Segment head = Volatile.Read(ref _head.Value)!;
                while (true)
                {
                    int[] states = head.States;
                    int index = head.DequeueIndex;
                    if ((uint)index >= (uint)states.Length)
                    {
                        Segment? next = Volatile.Read(ref head.Next);
                        if (next is null)
                        {
                            return true;
                        }

                        head = next;
                        continue;
                    }

                    return Volatile.Read(ref states[index]) != 1;
                }
            }
        }

        private Segment RentUnlinkedSegment()
        {
            lock (_cachedUnlinkedSegmentGate)
            {
                if (_cachedUnlinkedSegmentCount != 0)
                {
                    int nextIndex = _cachedUnlinkedSegmentCount - 1;
                    Segment segment = _cachedUnlinkedSegments[nextIndex]!;
                    _cachedUnlinkedSegments[nextIndex] = null;
                    _cachedUnlinkedSegmentCount = nextIndex;
                    segment.ResetForReuse();
                    return segment;
                }
            }

#if DEBUG
            if (TryConsumeSegmentAllocationFailureForTest())
            {
                throw new OutOfMemoryException("Injected MpscQueue segment allocation failure for test.");
            }
#endif

            return new Segment(_segmentSize);
        }

#if DEBUG
        internal static void SetSegmentAllocationFailuresForTest(int failureCount)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(failureCount);

            Volatile.Write(ref s_testSegmentAllocationFailuresRemaining, failureCount);
        }

        private static bool TryConsumeSegmentAllocationFailureForTest()
        {
            while (true)
            {
                int remainingFailures = Volatile.Read(ref s_testSegmentAllocationFailuresRemaining);
                if (remainingFailures <= 0)
                {
                    return false;
                }

                if (Interlocked.CompareExchange(
                    ref s_testSegmentAllocationFailuresRemaining,
                    remainingFailures - 1,
                    remainingFailures) == remainingFailures)
                {
                    return true;
                }
            }
        }
#endif

        private void ReturnUnlinkedSegment(Segment segment)
        {
            segment.ResetForReuse();
            lock (_cachedUnlinkedSegmentGate)
            {
                if (_cachedUnlinkedSegmentCount < _cachedUnlinkedSegments.Length)
                {
                    _cachedUnlinkedSegments[_cachedUnlinkedSegmentCount++] = segment;
                }
            }
        }

        private sealed class Segment
        {
            // SoA layout keeps producer-published states compact so consumer scans avoid
            // touching adjacent item payload cache lines.
            internal readonly T[] Items;
            internal readonly int[] States;
            internal int Incarnation;
            internal PaddedInt32 EnqueueIndex;
            internal int DequeueIndex;
            internal Segment? Next;

            internal Segment(int size)
            {
                Items = new T[size];
                States = new int[size];
                ResetForReuse();
            }

            internal void ResetForReuse()
            {
                Interlocked.Increment(ref Incarnation);
                EnqueueIndex.Value = 0;
                DequeueIndex = 0;
                Next = null;
                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                {
                    Array.Clear(Items);
                }
                Array.Clear(States);
            }
        }

#if TARGET_ARM64 || TARGET_LOONGARCH64
        private const int CacheLineWordCount = 16; // 128-byte cache line / sizeof(nint)
#else
        private const int CacheLineWordCount = 8;  // 64-byte cache line / sizeof(nint)
#endif

        [InlineArray(CacheLineWordCount - 1)]
        private struct CacheLinePadding
        {
            internal nint _element0;
        }

        private struct PaddedSegment
        {
            internal Segment? Value;
            internal CacheLinePadding _padding;
        }

        private struct PaddedInt32
        {
            internal int Value;
            internal CacheLinePadding _padding;
        }

    }
}
