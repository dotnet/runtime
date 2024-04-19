// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Runtime.CompilerServices
{
    internal enum CastResult
    {
        CannotCast = 0,
        CanCast = 1,
        MaybeCast = 2
    }

    internal unsafe struct CastCache
    {
        private const int VERSION_NUM_SIZE = 29;
        private const uint VERSION_NUM_MASK = (1 << VERSION_NUM_SIZE) - 1;
        private const int BUCKET_SIZE = 8;

        // nothing is ever stored into this, so we can use a static instance.
        private static int[]? s_sentinelTable;

        // The actual storage.
        private int[] _table;

        // when flushing, remember the last size.
        private int _lastFlushSize;

        private int _initialCacheSize;
        private int _maxCacheSize;

        public CastCache(int initialCacheSize, int maxCacheSize)
        {
            Debug.Assert(BitOperations.PopCount((uint)initialCacheSize) == 1 && initialCacheSize > 1);
            Debug.Assert(BitOperations.PopCount((uint)maxCacheSize) == 1 && maxCacheSize >= initialCacheSize);

            _initialCacheSize = initialCacheSize;
            _maxCacheSize = maxCacheSize;

            // A trivial 2-elements table used for "flushing" the cache.
            // Nothing is ever stored in such a small table and identity of the sentinel is not important.
            // It is required that we are able to allocate this, we may need this in OOM cases.
            s_sentinelTable ??= CreateCastCache(2, throwOnFail: true);

            _table =
#if !DEBUG
            // Initialize to the sentinel in DEBUG as if just flushed, to ensure the sentinel can be handled in Set.
            CreateCastCache(_initialCacheSize) ??
#endif
            s_sentinelTable!;
            _lastFlushSize = _initialCacheSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CastCacheEntry
        {
            // version has the following structure:
            // [ distance:3bit |  versionNum:29bit ]
            //
            // distance is how many iterations the entry is from it ideal position.
            // we use that for preemption.
            //
            // versionNum is a monotonically increasing numerical tag.
            // Writer "claims" entry by atomically incrementing the tag. Thus odd number indicates an entry in progress.
            // Upon completion of adding an entry the tag is incremented again making it even. Even number indicates a complete entry.
            //
            // Readers will read the version twice before and after retrieving the entry.
            // To have a usable entry both reads must yield the same even version.
            //
            internal uint _version;
            internal nuint _source;
            // pointers have unused lower bits due to alignment, we use one for the result
            internal nuint _targetAndResult;

            internal void SetEntry(nuint source, nuint target, bool result)
            {
                _source = source;
                _targetAndResult = target | (nuint)(result ? 1 : 0);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int KeyToBucket(ref int tableData, nuint source, nuint target)
        {
            // upper bits of addresses do not vary much, so to reduce loss due to cancelling out,
            // we do `rotl(source, <half-size>) ^ target` for mixing inputs.
            // then we use fibonacci hashing to reduce the value to desired size.

            int hashShift = HashShift(ref tableData);
#if TARGET_64BIT
            ulong hash = BitOperations.RotateLeft((ulong)source, 32) ^ (ulong)target;
            return (int)((hash * 11400714819323198485ul) >> hashShift);
#else
            uint hash = BitOperations.RotateLeft((uint)source, 16) ^ (uint)target;
            return (int)((hash * 2654435769u) >> hashShift);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref int TableData(int[] table)
        {
            // element 0 is used for embedded aux data
            //
            // AuxData: { hashShift, tableMask, victimCounter }
            return ref Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref table.GetRawData(), (nint)sizeof(nint)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref int HashShift(ref int tableData)
        {
            return ref tableData;
        }

        // TableMask is "size - 1"
        // we need that more often that we need size
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref int TableMask(ref int tableData)
        {
            return ref Unsafe.Add(ref tableData, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref uint VictimCounter(ref int tableData)
        {
            return ref Unsafe.As<int, uint>(ref Unsafe.Add(ref tableData, 2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref CastCacheEntry Element(ref int tableData, int index)
        {
            // element 0 is used for embedded aux data, skip it
            return ref Unsafe.Add(ref Unsafe.As<int, CastCacheEntry>(ref tableData), index + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal CastResult TryGet(nuint source, nuint target)
        {
            // table is always initialized and is not null.
            return TryGet(_table!, source, target);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static CastResult TryGet(int[] table, nuint source, nuint target)
        {
            // table is always initialized and is not null.
            ref int tableData = ref TableData(table);

            int index = KeyToBucket(ref tableData, source, target);
            for (int i = 0; i < BUCKET_SIZE;)
            {
                ref CastCacheEntry pEntry = ref Element(ref tableData, index);

                // we must read in this order: version -> [entry parts] -> version
                // if version is odd or changes, the entry is inconsistent and thus ignored
                uint version = Volatile.Read(ref pEntry._version);
                nuint entrySource = pEntry._source;
                // mask the lower version bit to make it even.
                // This way we can check if version is odd or changing in just one compare.
                version &= unchecked((uint)~1);

                if (entrySource == source)
                {
                    // we do ordinary reads of the entry parts and
                    // Interlocked.ReadMemoryBarrier() before reading the version
                    nuint entryTargetAndResult = pEntry._targetAndResult;
                    // target never has its lower bit set.
                    // a matching entryTargetAndResult would the have same bits, except for the lowest one, which is the result.
                    entryTargetAndResult ^= target;
                    if (entryTargetAndResult <= 1)
                    {
                        // make sure the second read of 'version' happens after reading 'source' and 'targetAndResults'
                        //
                        // We can either:
                        // - use acquires for both _source and _targetAndResults or
                        // - issue a load barrier before reading _version
                        // benchmarks on available hardware (Jan 2020) show that use of a read barrier is cheaper.
                        Interlocked.ReadMemoryBarrier();

                        if (version != pEntry._version)
                        {
                            // oh, so close, the entry is in inconsistent state.
                            // it is either changing or has changed while we were reading.
                            // treat it as a miss.
                            break;
                        }

                        return (CastResult)entryTargetAndResult;
                    }
                }

                if (version == 0)
                {
                    // the rest of the bucket is unclaimed, no point to search further
                    break;
                }

                // quadratic reprobe
                i++;
                index = (index + i) & TableMask(ref tableData);
            }
            return CastResult.MaybeCast;
        }

        // the rest is the support for updating the cache.
        // in CoreClr the cache is only updated in the native code
        //
        // The following helpers must match native implementations in castcache.h and castcache.cpp

        // we generally do not OOM in casts, just return null unless throwOnFail is specified.
        private int[]? CreateCastCache(int size, bool throwOnFail = false)
        {
            // size must be positive
            Debug.Assert(size > 1);
            // size must be a power of two
            Debug.Assert((size & (size - 1)) == 0);

            int[]? table = null;
            try
            {
                table = new int[(size + 1) * sizeof(CastCacheEntry) / sizeof(int)];
            }
            catch (OutOfMemoryException) when (!throwOnFail)
            {
            }

            if (table == null)
            {
                size = _initialCacheSize;
                try
                {
                    table = new int[(size + 1) * sizeof(CastCacheEntry) / sizeof(int)];
                }
                catch (OutOfMemoryException)
                {
                }
            }

            if (table == null)
            {
                return table;
            }

            ref int tableData = ref TableData(table);

            // set the table mask. we need it often, do not want to compute each time.
            TableMask(ref tableData) = size - 1;

            // Fibonacci hash reduces the value into desired range by shifting right by the number of leading zeroes in 'size-1'
            byte shift = (byte)BitOperations.LeadingZeroCount((nuint)(size - 1));
            HashShift(ref tableData) = shift;

            return table;
        }

        internal void TrySet(nuint source, nuint target, bool result)
        {
            int bucket;
            ref int tableData = ref *(int*)0;

            do
            {
                tableData = ref TableData(_table);
                if (TableMask(ref tableData) == 1)
                {
                    // 2-element table is used as a sentinel.
                    // we did not allocate a real table yet or have flushed it.
                    // try replacing the table, but do not insert anything.
                    MaybeReplaceCacheWithLarger(_lastFlushSize);
                    return;
                }

                bucket = KeyToBucket(ref tableData, source, target);
                int index = bucket;
                ref CastCacheEntry pEntry = ref Element(ref tableData, index);

                for (int i = 0; i < BUCKET_SIZE;)
                {
                    // claim the entry if unused or is more distant than us from its origin.
                    // Note - someone familiar with Robin Hood hashing will notice that
                    //        we do the opposite - we are "robbing the poor".
                    //        Robin Hood strategy improves average lookup in a lossles dictionary by reducing
                    //        outliers via giving preference to more distant entries.
                    //        What we have here is a lossy cache with outliers bounded by the bucket size.
                    //        We improve average lookup by giving preference to the "richer" entries.
                    //        If we used Robin Hood strategy we could eventually end up with all
                    //        entries in the table being maximally "poor".

                    uint version = pEntry._version;

                    // mask the lower version bit to make it even.
                    // This way we will detect both if version is changing (odd) or has changed (even, but different).
                    version &= unchecked((uint)~1);

                    if ((version & VERSION_NUM_MASK) >= (VERSION_NUM_MASK - 2))
                    {
                        // If exactly VERSION_NUM_MASK updates happens between here and publishing, we may not recognize a race.
                        // It is extremely unlikely, but to not worry about the possibility, lets not allow version to go this high and just get a new cache.
                        // This will not happen often.
                        FlushCurrentCache();
                        return;
                    }

                    if (version == 0 || (version >> VERSION_NUM_SIZE) > i)
                    {
                        uint newVersion = ((uint)i << VERSION_NUM_SIZE) + (version & VERSION_NUM_MASK) + 1;
                        uint versionOrig = Interlocked.CompareExchange(ref pEntry._version, newVersion, version);
                        if (versionOrig == version)
                        {
                            pEntry.SetEntry(source, target, result);

                            // entry is in inconsistent state and cannot be read or written to until we
                            // update the version, which is the last thing we do here
                            Volatile.Write(ref pEntry._version, newVersion + 1);
                            return;
                        }
                        // someone snatched the entry. try the next one in the bucket.
                    }

                    if (pEntry._source == source && ((pEntry._targetAndResult ^ target) <= 1))
                    {
                        // looks like we already have an entry for this.
                        // duplicate entries are harmless, but a bit of a waste.
                        return;
                    }

                    // quadratic reprobe
                    i++;
                    index += i;
                    pEntry = ref Element(ref tableData, index & TableMask(ref tableData));
                }

                // bucket is full.
            } while (TryGrow(ref tableData));

            // reread tableData after TryGrow.
            tableData = ref TableData(_table);

            if (TableMask(ref tableData) == 1)
            {
                // do not insert into a sentinel.
                return;
            }

            // pick a victim somewhat randomly within a bucket
            // NB: ++ is not interlocked. We are ok if we lose counts here. It is just a number that changes.
            uint victimDistance = VictimCounter(ref tableData)++ & (BUCKET_SIZE - 1);
            // position the victim in a quadratic reprobe bucket
            uint victim = (victimDistance * victimDistance + victimDistance) / 2;

            {
                ref CastCacheEntry pEntry = ref Element(ref tableData, (bucket + (int)victim) & TableMask(ref tableData));

                uint version = pEntry._version;

                // mask the lower version bit to make it even.
                // This way we will detect both if version is changing (odd) or has changed (even, but different).
                version &= unchecked((uint)~1);

                if ((version & VERSION_NUM_MASK) >= (VERSION_NUM_MASK - 2))
                {
                    // If exactly VERSION_NUM_MASK updates happens between here and publishing, we may not recognize a race.
                    // It is extremely unlikely, but to not worry about the possibility, lets not allow version to go this high and just get a new cache.
                    // This will not happen often.
                    FlushCurrentCache();
                    return;
                }

                uint newVersion = (victimDistance << VERSION_NUM_SIZE) + (version & VERSION_NUM_MASK) + 1;
                uint versionOrig = Interlocked.CompareExchange(ref pEntry._version, newVersion, version);

                if (versionOrig == version)
                {
                    pEntry.SetEntry(source, target, result);
                    Volatile.Write(ref pEntry._version, newVersion + 1);
                }
            }
        }

        private static int CacheElementCount(ref int tableData)
        {
            return TableMask(ref tableData) + 1;
        }

        private void FlushCurrentCache()
        {
            ref int tableData = ref TableData(_table);
            int lastSize = CacheElementCount(ref tableData);
            if (lastSize < _initialCacheSize)
                lastSize = _initialCacheSize;

            // store the last size to use when creating a new table
            // it is just a hint, not needed for correctness, so no synchronization
            // with the writing of the table
            _lastFlushSize = lastSize;
            // flushing is just replacing the table with a sentinel.
            _table = s_sentinelTable!;
        }

        private bool MaybeReplaceCacheWithLarger(int size)
        {
            int[]? newTable = CreateCastCache(size);
            if (newTable == null)
            {
                return false;
            }

            _table = newTable;
            return true;
        }

        private bool TryGrow(ref int tableData)
        {
            int newSize = CacheElementCount(ref tableData) * 2;
            if (newSize <= _maxCacheSize)
            {
                return MaybeReplaceCacheWithLarger(newSize);
            }

            return false;
        }
    }
}
