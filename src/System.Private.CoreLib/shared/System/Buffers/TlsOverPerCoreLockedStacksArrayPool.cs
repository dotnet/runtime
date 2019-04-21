// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
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
        // TODO: #7747: "Investigate optimizing ArrayPool heuristics"
        // - Explore caching in TLS more than one array per size per thread, and moving stale buffers to the global queue.
        // - Explore changing the size of each per-core bucket, potentially dynamically or based on other factors like array size.
        // - Explore changing number of buckets and what sizes of arrays are cached.
        // - Investigate whether false sharing is causing any issues, in particular on LockedStack's count and the contents of its array.
        // ...

        /// <summary>The number of buckets (array sizes) in the pool, one for each array length, starting from length 16.</summary>
        private const int NumBuckets = 17; // Utilities.SelectBucketIndex(2*1024*1024)
        /// <summary>Maximum number of per-core stacks to use per array size.</summary>
        private const int MaxPerCorePerArraySizeStacks = 64; // selected to avoid needing to worry about processor groups
        /// <summary>The maximum number of buffers to store in a bucket's global queue.</summary>
        private const int MaxBuffersPerArraySizePerCore = 8;

        /// <summary>The length of arrays stored in the corresponding indices in <see cref="_buckets"/> and <see cref="t_tlsBuckets"/>.</summary>
        private readonly int[] _bucketArraySizes;
        /// <summary>
        /// An array of per-core array stacks. The slots are lazily initialized to avoid creating
        /// lots of overhead for unused array sizes.
        /// </summary>
        private readonly PerCoreLockedStacks?[] _buckets = new PerCoreLockedStacks[NumBuckets];
        /// <summary>A per-thread array of arrays, to cache one array per array size per thread.</summary>
        [ThreadStatic]
        private static T[]?[]? t_tlsBuckets;

        private int _callbackCreated;

        private readonly static bool s_trimBuffers = GetTrimBuffers();

        /// <summary>
        /// Used to keep track of all thread local buckets for trimming if needed
        /// </summary>
        private static readonly ConditionalWeakTable<T[]?[], object?>? s_allTlsBuckets =
            s_trimBuffers ? new ConditionalWeakTable<T[]?[], object?>() : null;

        /// <summary>Initialize the pool.</summary>
        public TlsOverPerCoreLockedStacksArrayPool()
        {
            var sizes = new int[NumBuckets];
            for (int i = 0; i < sizes.Length; i++)
            {
                sizes[i] = Utilities.GetMaxSizeForBucket(i);
            }
            _bucketArraySizes = sizes;
        }

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
            // Arrays can't be smaller than zero.  We allow requesting zero-length arrays (even though
            // pooling such an array isn't valuable) as it's a valid length array, and we want the pool
            // to be usable in general instead of using `new`, even for computed lengths.
            if (minimumLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumLength));
            }
            else if (minimumLength == 0)
            {
                // No need to log the empty array.  Our pool is effectively infinite
                // and we'll never allocate for rents and never store for returns.
                return Array.Empty<T>();
            }

            ArrayPoolEventSource log = ArrayPoolEventSource.Log;
            T[]? buffer;

            // Get the bucket number for the array length
            int bucketIndex = Utilities.SelectBucketIndex(minimumLength);

            // If the array could come from a bucket...
            if (bucketIndex < _buckets.Length)
            {
                // First try to get it from TLS if possible.
                T[]?[]? tlsBuckets = t_tlsBuckets;
                if (tlsBuckets != null)
                {
                    buffer = tlsBuckets[bucketIndex];
                    if (buffer != null)
                    {
                        tlsBuckets[bucketIndex] = null;
                        if (log.IsEnabled())
                        {
                            log.BufferRented(buffer.GetHashCode(), buffer.Length, Id, bucketIndex);
                        }
                        return buffer;
                    }
                }

                // We couldn't get a buffer from TLS, so try the global stack.
                PerCoreLockedStacks? b = _buckets[bucketIndex];
                if (b != null)
                {
                    buffer = b.TryPop();
                    if (buffer != null)
                    {
                        if (log.IsEnabled())
                        {
                            log.BufferRented(buffer.GetHashCode(), buffer.Length, Id, bucketIndex);
                        }
                        return buffer;
                    }
                }

                // No buffer available.  Allocate a new buffer with a size corresponding to the appropriate bucket.
                buffer = new T[_bucketArraySizes[bucketIndex]];
            }
            else
            {
                // The request was for a size too large for the pool.  Allocate an array of exactly the requested length.
                // When it's returned to the pool, we'll simply throw it away.
                buffer = new T[minimumLength];
            }

            if (log.IsEnabled())
            {
                int bufferId = buffer.GetHashCode(), bucketId = -1; // no bucket for an on-demand allocated buffer
                log.BufferRented(bufferId, buffer.Length, Id, bucketId);
                log.BufferAllocated(bufferId, buffer.Length, Id, bucketId, bucketIndex >= _buckets.Length ?
                    ArrayPoolEventSource.BufferAllocatedReason.OverMaximumSize :
                    ArrayPoolEventSource.BufferAllocatedReason.PoolExhausted);
            }

            return buffer;
        }

        public override void Return(T[] array, bool clearArray = false)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            // Determine with what bucket this array length is associated
            int bucketIndex = Utilities.SelectBucketIndex(array.Length);

            // If we can tell that the buffer was allocated (or empty), drop it. Otherwise, check if we have space in the pool.
            if (bucketIndex < _buckets.Length)
            {
                // Clear the array if the user requests.
                if (clearArray)
                {
                    Array.Clear(array, 0, array.Length);
                }

                // Check to see if the buffer is the correct size for this bucket
                if (array.Length != _bucketArraySizes[bucketIndex])
                {
                    throw new ArgumentException(SR.ArgumentException_BufferNotFromPool, nameof(array));
                }

                // Write through the TLS bucket.  If there weren't any buckets, create them
                // and store this array into it.  If there were, store this into it, and
                // if there was a previous one there, push that to the global stack.  This
                // helps to keep LIFO access such that the most recently pushed stack will
                // be in TLS and the first to be popped next.
                T[]?[]? tlsBuckets = t_tlsBuckets;
                if (tlsBuckets == null)
                {
                    t_tlsBuckets = tlsBuckets = new T[NumBuckets][];
                    tlsBuckets[bucketIndex] = array;
                    if (s_trimBuffers)
                    {
                        Debug.Assert(s_allTlsBuckets != null, "Should be non-null iff s_trimBuffers is true");
                        s_allTlsBuckets.Add(tlsBuckets, null);
                        if (Interlocked.Exchange(ref _callbackCreated, 1) != 1)
                        {
                            Gen2GcCallback.Register(Gen2GcCallbackFunc, this);
                        }
                    }
                }
                else
                {
                    T[]? prev = tlsBuckets[bucketIndex];
                    tlsBuckets[bucketIndex] = array;

                    if (prev != null)
                    {
                        PerCoreLockedStacks stackBucket = _buckets[bucketIndex] ?? CreatePerCoreLockedStacks(bucketIndex);
                        stackBucket.TryPush(prev);
                    }
                }
            }

            // Log that the buffer was returned
            ArrayPoolEventSource log = ArrayPoolEventSource.Log;
            if (log.IsEnabled())
            {
                log.BufferReturned(array.GetHashCode(), array.Length, Id);
            }
        }

        public bool Trim()
        {
            Debug.Assert(s_trimBuffers);
            Debug.Assert(s_allTlsBuckets != null);

            int milliseconds = Environment.TickCount;
            MemoryPressure pressure = GetMemoryPressure();

            ArrayPoolEventSource log = ArrayPoolEventSource.Log;
            if (log.IsEnabled())
                log.BufferTrimPoll(milliseconds, (int)pressure);

            PerCoreLockedStacks?[] perCoreBuckets = _buckets;
            for (int i = 0; i < perCoreBuckets.Length; i++)
            {
                perCoreBuckets[i]?.Trim((uint)milliseconds, Id, pressure, _bucketArraySizes[i]);
            }

            if (pressure == MemoryPressure.High)
            {
                // Under high pressure, release all thread locals
                if (log.IsEnabled())
                {
                    foreach (KeyValuePair<T[]?[], object?> tlsBuckets in s_allTlsBuckets)
                    {
                        T[]?[] buckets = tlsBuckets.Key;
                        for (int i = 0; i < buckets.Length; i++)
                        {
                            T[]? buffer = Interlocked.Exchange(ref buckets[i], null);
                            if (buffer != null)
                            {
                                // As we don't want to take a perf hit in the rent path it
                                // is possible that a buffer could be rented as we "free" it.
                                log.BufferTrimmed(buffer.GetHashCode(), buffer.Length, Id);
                            }
                        }
                    }
                }
                else
                {
                    foreach (KeyValuePair<T[]?[], object?> tlsBuckets in s_allTlsBuckets)
                    {
                        T[]?[] buckets = tlsBuckets.Key;
                        Array.Clear(buckets, 0, buckets.Length);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// This is the static function that is called from the gen2 GC callback.
        /// The input object is the instance we want the callback on.
        /// </summary>
        /// <remarks>
        /// The reason that we make this function static and take the instance as a parameter is that
        /// we would otherwise root the instance to the Gen2GcCallback object, leaking the instance even when
        /// the application no longer needs it.
        /// </remarks>
        private static bool Gen2GcCallbackFunc(object target)
        {
            return ((TlsOverPerCoreLockedStacksArrayPool<T>)(target)).Trim();
        }

        private enum MemoryPressure
        {
            Low,
            Medium,
            High
        }

        private static MemoryPressure GetMemoryPressure()
        {
            const double HighPressureThreshold = .90;       // Percent of GC memory pressure threshold we consider "high"
            const double MediumPressureThreshold = .70;     // Percent of GC memory pressure threshold we consider "medium"

            GCMemoryInfo memoryInfo = GC.GetGCMemoryInfo();
            if (memoryInfo.MemoryLoadBytes >= memoryInfo.HighMemoryLoadThresholdBytes * HighPressureThreshold)
            {
                return MemoryPressure.High;
            }
            else if (memoryInfo.MemoryLoadBytes >= memoryInfo.HighMemoryLoadThresholdBytes * MediumPressureThreshold)
            {
                return MemoryPressure.Medium;
            }
            return MemoryPressure.Low;
        }

        private static bool GetTrimBuffers()
        {
            // Environment uses ArrayPool, so we have to hit the API directly.
#if !CORECLR
            // P/Invokes are different for CoreCLR/RT- for RT we'll not allow
            // enabling/disabling for now.
            return true;
#else
            return CLRConfig.GetBoolValueWithFallbacks("System.Buffers.ArrayPool.TrimShared", "DOTNET_SYSTEM_BUFFERS_ARRAYPOOL_TRIMSHARED", defaultValue: true);
#endif
        }

        /// <summary>
        /// Stores a set of stacks of arrays, with one stack per core.
        /// </summary>
        private sealed class PerCoreLockedStacks
        {
            /// <summary>The stacks.</summary>
            private readonly LockedStack[] _perCoreStacks;

            /// <summary>Initializes the stacks.</summary>
            public PerCoreLockedStacks()
            {
                // Create the stacks.  We create as many as there are processors, limited by our max.
                var stacks = new LockedStack[Math.Min(Environment.ProcessorCount, MaxPerCorePerArraySizeStacks)];
                for (int i = 0; i < stacks.Length; i++)
                {
                    stacks[i] = new LockedStack();
                }
                _perCoreStacks = stacks;
            }

            /// <summary>Try to push the array into the stacks. If each is full when it's tested, the array will be dropped.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void TryPush(T[] array)
            {
                // Try to push on to the associated stack first.  If that fails,
                // round-robin through the other stacks.
                LockedStack[] stacks = _perCoreStacks;
                int index = Thread.GetCurrentProcessorId() % stacks.Length;
                for (int i = 0; i < stacks.Length; i++)
                {
                    if (stacks[index].TryPush(array)) return;
                    if (++index == stacks.Length) index = 0;
                }
            }

            /// <summary>Try to get an array from the stacks.  If each is empty when it's tested, null will be returned.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public T[]? TryPop()
            {
                // Try to pop from the associated stack first.  If that fails,
                // round-robin through the other stacks.
                T[]? arr;
                LockedStack[] stacks = _perCoreStacks;
                int index = Thread.GetCurrentProcessorId() % stacks.Length;
                for (int i = 0; i < stacks.Length; i++)
                {
                    if ((arr = stacks[index].TryPop()) != null) return arr;
                    if (++index == stacks.Length) index = 0;
                }
                return null;
            }

            public void Trim(uint tickCount, int id, MemoryPressure pressure, int bucketSize)
            {
                LockedStack[] stacks = _perCoreStacks;
                for (int i = 0; i < stacks.Length; i++)
                {
                    stacks[i].Trim(tickCount, id, pressure, bucketSize);
                }
            }
        }

        /// <summary>Provides a simple stack of arrays, protected by a lock.</summary>
        private sealed class LockedStack
        {
            private readonly T[]?[] _arrays = new T[MaxBuffersPerArraySizePerCore][];
            private int _count;
            private uint _firstStackItemMS;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryPush(T[] array)
            {
                bool enqueued = false;
                Monitor.Enter(this);
                if (_count < MaxBuffersPerArraySizePerCore)
                {
                    if (s_trimBuffers && _count == 0)
                    {
                        // Stash the time the bottom of the stack was filled
                        _firstStackItemMS = (uint)Environment.TickCount;
                    }

                    _arrays[_count++] = array;
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
                if (_count > 0)
                {
                    arr = _arrays[--_count];
                    _arrays[_count] = null;
                }
                Monitor.Exit(this);
                return arr;
            }

            public void Trim(uint tickCount, int id, MemoryPressure pressure, int bucketSize)
            {
                const uint StackTrimAfterMS = 60 * 1000;                        // Trim after 60 seconds for low/moderate pressure
                const uint StackHighTrimAfterMS = 10 * 1000;                    // Trim after 10 seconds for high pressure
                const uint StackRefreshMS = StackTrimAfterMS / 4;               // Time bump after trimming (1/4 trim time)
                const int StackLowTrimCount = 1;                                // Trim one item when pressure is low
                const int StackMediumTrimCount = 2;                             // Trim two items when pressure is moderate
                const int StackHighTrimCount = MaxBuffersPerArraySizePerCore;   // Trim all items when pressure is high
                const int StackLargeBucket = 16384;                             // If the bucket is larger than this we'll trim an extra when under high pressure
                const int StackModerateTypeSize = 16;                           // If T is larger than this we'll trim an extra when under high pressure
                const int StackLargeTypeSize = 32;                              // If T is larger than this we'll trim an extra (additional) when under high pressure

                if (_count == 0)
                    return;
                uint trimTicks = pressure == MemoryPressure.High ? StackHighTrimAfterMS : StackTrimAfterMS;

                lock (this)
                {
                    if (_count > 0 && _firstStackItemMS > tickCount || (tickCount - _firstStackItemMS) > trimTicks)
                    {
                        // We've wrapped the tick count or elapsed enough time since the
                        // first item went into the stack. Drop the top item so it can
                        // be collected and make the stack look a little newer.

                        ArrayPoolEventSource log = ArrayPoolEventSource.Log;
                        int trimCount = StackLowTrimCount;
                        switch (pressure)
                        {
                            case MemoryPressure.High:
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
                            case MemoryPressure.Medium:
                                trimCount = StackMediumTrimCount;
                                break;
                        }

                        while (_count > 0 && trimCount-- > 0)
                        {
                            T[]? array = _arrays[--_count];
                            Debug.Assert(array != null, "No nulls should have been present in slots < _count.");
                            _arrays[_count] = null;

                            if (log.IsEnabled())
                            {
                                log.BufferTrimmed(array.GetHashCode(), array.Length, id);
                            }
                        }

                        if (_count > 0 && _firstStackItemMS < uint.MaxValue - StackRefreshMS)
                        {
                            // Give the remaining items a bit more time
                            _firstStackItemMS += StackRefreshMS;
                        }
                    }
                }
            }
        }
    }
}
