// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Internal.Runtime.CompilerServices;

namespace System.Buffers
{
    /// <summary>
    /// Provides an ArrayPool implementation meant to be used as the singleton returned from ArrayPool.Shared.
    /// </summary>
    /// <remarks>
    /// The implementation uses a tiered caching scheme, with a small per-thread cache for each array size, followed
    /// by a cache per array size shared by all threads, split into per-core stacks meant to be used by threads
    /// running on that core.  Locks are used to protect each per-core stack, because a thread can migrate after
    /// checking its processor number, because multiple threads could interleave on the same core, and because
    /// a thread is allowed to check other core's buckets if its core's bucket is empty/full.
    /// </remarks>
    internal sealed partial class TlsOverPerCoreLockedStacksArrayPool<T> : ArrayPool<T>
    {
        /// <summary>The number of buckets (array sizes) in the pool, one for each array length, starting from length 16.</summary>
        private const int NumBuckets = 27; // Utilities.SelectBucketIndex(1024 * 1024 * 1024 + 1)
        /// <summary>Maximum number of per-core stacks to use per array size.</summary>
        private const int MaxPerCorePerArraySizeStacks = 64; // selected to avoid needing to worry about processor groups
        /// <summary>The maximum number of buffers to store in a bucket's global queue.</summary>
        private const int MaxBuffersPerArraySizePerCore = 8;

        /// <summary>A per-thread array of arrays, to cache one array per array size per thread.</summary>
        [ThreadStatic]
        private static ThreadLocalArray[]? t_tlsBuckets;
        /// <summary>Used to keep track of all thread local buckets for trimming if needed.</summary>
        private readonly ConditionalWeakTable<ThreadLocalArray[], object?> _allTlsBuckets = new ConditionalWeakTable<ThreadLocalArray[], object?>();
        /// <summary>
        /// An array of per-core array stacks. The slots are lazily initialized to avoid creating
        /// lots of overhead for unused array sizes.
        /// </summary>
        private readonly PerCoreLockedStacks?[] _buckets = new PerCoreLockedStacks[NumBuckets];
        /// <summary>Whether the callback to trim arrays in response to memory pressure has been created.</summary>
        private int _trimCallbackCreated;

        /// <summary>Allocate a new PerCoreLockedStacks and try to store it into the <see cref="_buckets"/> array.</summary>
        private PerCoreLockedStacks CreatePerCoreLockedStacks(int bucketIndex)
        {
            var inst = new PerCoreLockedStacks();
            return Interlocked.CompareExchange(ref _buckets[bucketIndex], inst, null) ?? inst;
        }

        /// <summary>Gets an ID for the pool to use with events.</summary>
        private int Id => GetHashCode();

        public override T[] Rent(int minimumLength)
        {
            ArrayPoolEventSource log = ArrayPoolEventSource.Log;
            T[]? buffer;

            // Get the bucket number for the array length. The result may be out of range of buckets,
            // either for too large a value or for 0 and negative values.
            int bucketIndex = Utilities.SelectBucketIndex(minimumLength);

            // First, try to get an array from TLS if possible.
            ThreadLocalArray[]? tlsBuckets = t_tlsBuckets;
            if (tlsBuckets is not null && (uint)bucketIndex < (uint)tlsBuckets.Length)
            {
                buffer = tlsBuckets[bucketIndex].Array;
                if (buffer is not null)
                {
                    tlsBuckets[bucketIndex].Array = null;
                    if (log.IsEnabled())
                    {
                        log.BufferRented(buffer.GetHashCode(), buffer.Length, Id, bucketIndex);
                    }
                    return buffer;
                }
            }

            // Next, try to get an array from one of the per-core stacks.
            PerCoreLockedStacks?[] perCoreBuckets = _buckets;
            if ((uint)bucketIndex < (uint)perCoreBuckets.Length)
            {
                PerCoreLockedStacks? b = perCoreBuckets[bucketIndex];
                if (b is not null)
                {
                    buffer = b.TryPop();
                    if (buffer is not null)
                    {
                        if (log.IsEnabled())
                        {
                            log.BufferRented(buffer.GetHashCode(), buffer.Length, Id, bucketIndex);
                        }
                        return buffer;
                    }
                }

                // No buffer available.  Ensure the length we'll allocate matches that of a bucket
                // so we can later return it.
                minimumLength = Utilities.GetMaxSizeForBucket(bucketIndex);
            }
            else if (minimumLength == 0)
            {
                // We allow requesting zero-length arrays (even though pooling such an array isn't valuable)
                // as it's a valid length array, and we want the pool to be usable in general instead of using
                // `new`, even for computed lengths. But, there's no need to log the empty array.  Our pool is
                // effectively infinite for empty arrays and we'll never allocate for rents and never store for returns.
                return Array.Empty<T>();
            }
            else if (minimumLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumLength));
            }

            buffer = GC.AllocateUninitializedArray<T>(minimumLength);
            if (log.IsEnabled())
            {
                int bufferId = buffer.GetHashCode();
                log.BufferRented(bufferId, buffer.Length, Id, ArrayPoolEventSource.NoBucketId);
                log.BufferAllocated(bufferId, buffer.Length, Id, ArrayPoolEventSource.NoBucketId, bucketIndex >= _buckets.Length ?
                    ArrayPoolEventSource.BufferAllocatedReason.OverMaximumSize :
                    ArrayPoolEventSource.BufferAllocatedReason.PoolExhausted);
            }
            return buffer;
        }

        public override void Return(T[] array, bool clearArray = false)
        {
            if (array is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            // Determine with what bucket this array length is associated
            int bucketIndex = Utilities.SelectBucketIndex(array.Length);

            // Make sure our TLS buckets are initialized.  Technically we could avoid doing
            // this if the array being returned is erroneous or too large for the pool, but the
            // former condition is an error we don't need to optimize for, and the latter is incredibly
            // rare, given a max size of 1B elements.
            ThreadLocalArray[] tlsBuckets = t_tlsBuckets ?? InitializeTlsBucketsAndTrimming();

            bool haveBucket = false;
            bool returned = true;
            if ((uint)bucketIndex < (uint)tlsBuckets.Length)
            {
                haveBucket = true;

                // Clear the array if the user requested it.
                if (clearArray)
                {
                    Array.Clear(array);
                }

                // Check to see if the buffer is the correct size for this bucket.
                if (array.Length != Utilities.GetMaxSizeForBucket(bucketIndex))
                {
                    throw new ArgumentException(SR.ArgumentException_BufferNotFromPool, nameof(array));
                }

                // Store the array into the TLS bucket.  If there's already an array in it,
                // push that array down into the per-core stacks, preferring to keep the latest
                // one in TLS for better locality.
                ref ThreadLocalArray tla = ref tlsBuckets[bucketIndex];
                T[]? prev = tla.Array;
                tla = new ThreadLocalArray(array);
                if (prev is not null)
                {
                    PerCoreLockedStacks stackBucket = _buckets[bucketIndex] ?? CreatePerCoreLockedStacks(bucketIndex);
                    returned = stackBucket.TryPush(prev);
                }
            }

            // Log that the buffer was returned
            ArrayPoolEventSource log = ArrayPoolEventSource.Log;
            if (log.IsEnabled() && array.Length != 0)
            {
                log.BufferReturned(array.GetHashCode(), array.Length, Id);
                if (!(haveBucket & returned))
                {
                    log.BufferDropped(array.GetHashCode(), array.Length, Id,
                        haveBucket ? bucketIndex : ArrayPoolEventSource.NoBucketId,
                        haveBucket ? ArrayPoolEventSource.BufferDroppedReason.Full : ArrayPoolEventSource.BufferDroppedReason.OverMaximumSize);
                }
            }
        }

        public bool Trim()
        {
            int currentMilliseconds = Environment.TickCount;
            Utilities.MemoryPressure pressure = Utilities.GetMemoryPressure();

            // Log that we're trimming.
            ArrayPoolEventSource log = ArrayPoolEventSource.Log;
            if (log.IsEnabled())
            {
                log.BufferTrimPoll(currentMilliseconds, (int)pressure);
            }

            // Trim each of the per-core buckets.
            PerCoreLockedStacks?[] perCoreBuckets = _buckets;
            for (int i = 0; i < perCoreBuckets.Length; i++)
            {
                perCoreBuckets[i]?.Trim(currentMilliseconds, Id, pressure, Utilities.GetMaxSizeForBucket(i));
            }

            // Trim each of the TLS buckets. Note that threads may be modifying their TLS slots concurrently with
            // this trimming happening. We do not force synchronization with those operations, so we accept the fact
            // that we may end up firing a trimming event even if an array wasn't trimmed, and potentially
            // trim an array we didn't need to.  Both of these should be rare occurrences.

            // Under high pressure, release all thread locals.
            if (pressure == Utilities.MemoryPressure.High)
            {
                if (!log.IsEnabled())
                {
                    foreach (KeyValuePair<ThreadLocalArray[], object?> tlsBuckets in _allTlsBuckets)
                    {
                        Array.Clear(tlsBuckets.Key);
                    }
                }
                else
                {
                    foreach (KeyValuePair<ThreadLocalArray[], object?> tlsBuckets in _allTlsBuckets)
                    {
                        ThreadLocalArray[] buckets = tlsBuckets.Key;
                        for (int i = 0; i < buckets.Length; i++)
                        {
                            if (Interlocked.Exchange(ref buckets[i].Array, null) is T[] buffer)
                            {
                                log.BufferTrimmed(buffer.GetHashCode(), buffer.Length, Id);
                            }
                        }
                    }
                }
            }
            else
            {
                // Otherwise, release thread locals based on how long we've observed them to be stored. This time is
                // approximate, with the time set not when the array is stored but when we see it during a Trim, so it
                // takes at least two Trim calls (and thus two gen2 GCs) to drop an array, unless we're in high memory
                // pressure. These values have been set arbitrarily; we could tune them in the future.
                uint millisecondsThreshold = pressure switch
                {
                    Utilities.MemoryPressure.Medium => 15_000,
                    _ => 30_000,
                };

                foreach (KeyValuePair<ThreadLocalArray[], object?> tlsBuckets in _allTlsBuckets)
                {
                    ThreadLocalArray[] buckets = tlsBuckets.Key;
                    for (int i = 0; i < buckets.Length; i++)
                    {
                        if (buckets[i].Array is null)
                        {
                            continue;
                        }

                        // We treat 0 to mean it hasn't yet been seen in a Trim call. In the very rare case where Trim records 0,
                        // it'll take an extra Trim call to remove the array.
                        int lastSeen = buckets[i].MillisecondsTimeStamp;
                        if (lastSeen == 0)
                        {
                            buckets[i].MillisecondsTimeStamp = currentMilliseconds;
                        }
                        else if ((currentMilliseconds - lastSeen) >= millisecondsThreshold)
                        {
                            // Time noticeably wrapped, or we've surpassed the threshold.
                            // Clear out the array, and log its being trimmed if desired.
                            if (Interlocked.Exchange(ref buckets[i].Array, null) is T[] buffer &&
                                log.IsEnabled())
                            {
                                log.BufferTrimmed(buffer.GetHashCode(), buffer.Length, Id);
                            }
                        }
                    }
                }
            }

            return true;
        }

        private ThreadLocalArray[] InitializeTlsBucketsAndTrimming()
        {
            Debug.Assert(t_tlsBuckets is null, $"Non-null {nameof(t_tlsBuckets)}");

            var tlsBuckets = new ThreadLocalArray[NumBuckets];
            t_tlsBuckets = tlsBuckets;

            _allTlsBuckets.Add(tlsBuckets, null);
            if (Interlocked.Exchange(ref _trimCallbackCreated, 1) == 0)
            {
                Gen2GcCallback.Register(s => ((TlsOverPerCoreLockedStacksArrayPool<T>)s).Trim(), this);
            }

            return tlsBuckets;
        }

        /// <summary>Stores a set of stacks of arrays, with one stack per core.</summary>
        private sealed class PerCoreLockedStacks
        {
            /// <summary>Number of locked stacks to employ.</summary>
            private static readonly int s_lockedStackCount = Math.Min(Environment.ProcessorCount, MaxPerCorePerArraySizeStacks);
            /// <summary>The stacks.</summary>
            private readonly LockedStack[] _perCoreStacks;

            /// <summary>Initializes the stacks.</summary>
            public PerCoreLockedStacks()
            {
                // Create the stacks.  We create as many as there are processors, limited by our max.
                var stacks = new LockedStack[s_lockedStackCount];
                for (int i = 0; i < stacks.Length; i++)
                {
                    stacks[i] = new LockedStack();
                }
                _perCoreStacks = stacks;
            }

            /// <summary>Try to push the array into the stacks. If each is full when it's tested, the array will be dropped.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryPush(T[] array)
            {
                // Try to push on to the associated stack first.  If that fails,
                // round-robin through the other stacks.
                LockedStack[] stacks = _perCoreStacks;
                int index = (int)((uint)Thread.GetCurrentProcessorId() % (uint)s_lockedStackCount); // mod by constant in tier 1
                for (int i = 0; i < stacks.Length; i++)
                {
                    if (stacks[index].TryPush(array)) return true;
                    if (++index == stacks.Length) index = 0;
                }

                return false;
            }

            /// <summary>Try to get an array from the stacks.  If each is empty when it's tested, null will be returned.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public T[]? TryPop()
            {
                // Try to pop from the associated stack first.  If that fails, round-robin through the other stacks.
                T[]? arr;
                LockedStack[] stacks = _perCoreStacks;
                int index = (int)((uint)Thread.GetCurrentProcessorId() % (uint)s_lockedStackCount); // mod by constant in tier 1
                for (int i = 0; i < stacks.Length; i++)
                {
                    if ((arr = stacks[index].TryPop()) is not null) return arr;
                    if (++index == stacks.Length) index = 0;
                }
                return null;
            }

            public void Trim(int currentMilliseconds, int id, Utilities.MemoryPressure pressure, int bucketSize)
            {
                LockedStack[] stacks = _perCoreStacks;
                for (int i = 0; i < stacks.Length; i++)
                {
                    stacks[i].Trim(currentMilliseconds, id, pressure, bucketSize);
                }
            }
        }

        /// <summary>Provides a simple, bounded stack of arrays, protected by a lock.</summary>
        private sealed class LockedStack
        {
            /// <summary>The arrays in the stack.</summary>
            private readonly T[]?[] _arrays = new T[MaxBuffersPerArraySizePerCore][];
            /// <summary>Number of arrays stored in <see cref="_arrays"/>.</summary>
            private int _count;
            /// <summary>Timestamp set by Trim when it sees this as 0.</summary>
            private int _millisecondsTimestamp;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryPush(T[] array)
            {
                bool enqueued = false;
                Monitor.Enter(this);
                T[]?[] arrays = _arrays;
                int count = _count;
                if ((uint)count < (uint)arrays.Length)
                {
                    if (count == 0)
                    {
                        // Reset the time stamp now that we're transitioning from empty to non-empty.
                        // Trim will see this as 0 and initialize it to the current time when Trim is called.
                        _millisecondsTimestamp = 0;
                    }

                    arrays[count] = array;
                    _count = count + 1;
                    enqueued = true;
                }
                Monitor.Exit(this);
                return enqueued;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public T[]? TryPop()
            {
                T[]? arr = null;
                Monitor.Enter(this);
                T[]?[] arrays = _arrays;
                int count = _count - 1;
                if ((uint)count < (uint)arrays.Length)
                {
                    arr = arrays[count];
                    arrays[count] = null;
                    _count = count;
                }
                Monitor.Exit(this);
                return arr;
            }

            public void Trim(int currentMilliseconds, int id, Utilities.MemoryPressure pressure, int bucketSize)
            {
                const int StackTrimAfterMS = 60 * 1000;                        // Trim after 60 seconds for low/moderate pressure
                const int StackHighTrimAfterMS = 10 * 1000;                    // Trim after 10 seconds for high pressure
                const int StackLowTrimCount = 1;                                // Trim one item when pressure is low
                const int StackMediumTrimCount = 2;                             // Trim two items when pressure is moderate
                const int StackHighTrimCount = MaxBuffersPerArraySizePerCore;   // Trim all items when pressure is high
                const int StackLargeBucket = 16384;                             // If the bucket is larger than this we'll trim an extra when under high pressure
                const int StackModerateTypeSize = 16;                           // If T is larger than this we'll trim an extra when under high pressure
                const int StackLargeTypeSize = 32;                              // If T is larger than this we'll trim an extra (additional) when under high pressure

                if (_count == 0)
                {
                    return;
                }

                int trimMilliseconds = pressure == Utilities.MemoryPressure.High ? StackHighTrimAfterMS : StackTrimAfterMS;

                lock (this)
                {
                    if (_count == 0)
                    {
                        return;
                    }

                    if (_millisecondsTimestamp == 0)
                    {
                        _millisecondsTimestamp = currentMilliseconds;
                        return;
                    }

                    if ((currentMilliseconds - _millisecondsTimestamp) <= trimMilliseconds)
                    {
                        return;
                    }

                    // We've elapsed enough time since the first item went into the stack.
                    // Drop the top item so it can be collected and make the stack look a little newer.

                    ArrayPoolEventSource log = ArrayPoolEventSource.Log;
                    int trimCount = StackLowTrimCount;
                    switch (pressure)
                    {
                        case Utilities.MemoryPressure.High:
                            trimCount = StackHighTrimCount;

                            // When pressure is high, aggressively trim larger arrays.
                            if (bucketSize > StackLargeBucket)
                            {
                                trimCount++;
                            }
                            if (Unsafe.SizeOf<T>() > StackModerateTypeSize)
                            {
                                trimCount++;
                            }
                            if (Unsafe.SizeOf<T>() > StackLargeTypeSize)
                            {
                                trimCount++;
                            }
                            break;

                        case Utilities.MemoryPressure.Medium:
                            trimCount = StackMediumTrimCount;
                            break;
                    }

                    while (_count > 0 && trimCount-- > 0)
                    {
                        T[]? array = _arrays[--_count];
                        Debug.Assert(array is not null, "No nulls should have been present in slots < _count.");
                        _arrays[_count] = null;

                        if (log.IsEnabled())
                        {
                            log.BufferTrimmed(array.GetHashCode(), array.Length, id);
                        }
                    }

                    _millisecondsTimestamp = _count > 0 ?
                        _millisecondsTimestamp + (trimMilliseconds / 4) : // Give the remaining items a bit more time
                        0;
                }
            }
        }

        /// <summary>Wrapper for arrays stored in ThreadStatic buckets.</summary>
        private struct ThreadLocalArray
        {
            /// <summary>The stored array.</summary>
            public T[]? Array;
            /// <summary>Environment.TickCount timestamp for when this array was observed by Trim.</summary>
            public int MillisecondsTimeStamp;

            public ThreadLocalArray(T[] array)
            {
                Array = array;
                MillisecondsTimeStamp = 0;
            }
        }
    }
}
