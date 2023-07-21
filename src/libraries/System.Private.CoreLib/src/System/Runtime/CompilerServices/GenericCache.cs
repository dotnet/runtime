// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Runtime.CompilerServices
{
    // Unmanaged part of cache entry
    // it is not nested in GenericCache because generic types cannot have explicit layout.
    [StructLayout(LayoutKind.Explicit)]
    internal struct UnmanagedPart
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
        [FieldOffset(0)]
        internal uint _version;
        [FieldOffset(sizeof(int))]
        internal int _hash;

        // AuxData
        [FieldOffset(0)]
        internal int tableMask;
        [FieldOffset(sizeof(int))]
        internal byte hashShift;
        [FieldOffset(sizeof(int) + 1)]
        internal byte victimCounter;
    }

    // TKey may contain references, but we want it to be a struct,
    // so that equality is devirtualized.
    internal unsafe struct GenericCache<TKey, TValue>
        where TKey: struct, IEquatable<TKey>
    {
        private const int VERSION_NUM_SIZE = 29;
        private const uint VERSION_NUM_MASK = (1 << VERSION_NUM_SIZE) - 1;
        private const int BUCKET_SIZE = 8;

        // nothing is ever stored into this, so we can use a static instance.
        private static Entry[]? s_sentinelTable;

        // The actual storage.
        private Entry[] _table;

        // when flushing, remember the last size.
        private int _lastFlushSize;

        private int _initialCacheSize;
        private int _maxCacheSize;

        // creates a new cache instance
        public GenericCache(int initialCacheSize, int maxCacheSize)
        {
            _initialCacheSize = initialCacheSize;
            _maxCacheSize = maxCacheSize;

            // A trivial 2-elements table used for "flushing" the cache.
            // Nothing is ever stored in such a small table and identity of the sentinel is not important.
            // It is required that we are able to allocate this, we may need this in OOM cases.
            s_sentinelTable ??= CreateCastCache(2, throwOnFail: true);

            _table =
#if !DEBUG
            // Initialize to the sentinel in DEBUG as if just flushed, to ensure the sentinel can be handled in Set.
            CreateCastCache(initialCacheSize) ??
#endif
            s_sentinelTable!;
            _lastFlushSize = initialCacheSize;
        }

        private struct Entry
        {
            internal UnmanagedPart _unmanagedPart;
            internal TKey _key;
            internal TValue _value;

            [UnscopedRef]
            public ref uint Version => ref _unmanagedPart._version;
            [UnscopedRef]
            public ref int Hash => ref _unmanagedPart._hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int HashToBucket(ref Entry tableData, int hash)
        {
            byte hashShift = HashShift(ref tableData);
#if TARGET_64BIT
            return (int)(((ulong)hash * 11400714819323198485ul) >> hashShift);
#else
            return (int)(((uint)hash * 2654435769u) >> hashShift);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref Entry TableData(Entry[] table)
        {
            // points to element 0, which is used for embedded aux data
            return ref Unsafe.As<byte, Entry>(ref Unsafe.As<RawArrayData>(table).Data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref byte HashShift(ref Entry tableData)
        {
            return ref tableData._unmanagedPart.hashShift;
        }

        // TableMask is "size - 1"
        // we need that more often that we need size
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref int TableMask(ref Entry tableData)
        {
            return ref tableData._unmanagedPart.tableMask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref byte VictimCounter(ref Entry tableData)
        {
            return ref tableData._unmanagedPart.victimCounter;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref Entry Element(ref Entry tableData, int index)
        {
            // element 0 is used for embedded aux data, skip it
            return ref Unsafe.Add(ref tableData, index + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGet(TKey key, out TValue? value)
        {
            // table is always initialized and is not null.
            ref Entry tableData = ref TableData(_table!);
            int hash = key!.GetHashCode();
            int index = HashToBucket(ref tableData, hash);
            for (int i = 0; i < BUCKET_SIZE;)
            {
                ref Entry pEntry = ref Element(ref tableData, index);

                // we must read in this order: version -> [entry parts] -> version
                // if version is odd or changes, the entry is inconsistent and thus ignored
                uint version = Volatile.Read(ref pEntry.Version);

                if (hash == pEntry.Hash && key.Equals(pEntry._key))
                {
                    // we do ordinary reads of the value and
                    // Interlocked.ReadMemoryBarrier() before reading the version
                    value = pEntry._value;

                    // make sure the second read of 'version' happens after reading '_value'
                    Interlocked.ReadMemoryBarrier();

                    // mask the lower version bit to make it even.
                    // This way we can check if version is odd or changing in just one compare.
                    version &= unchecked((uint)~1);
                    if (version != pEntry.Version)
                    {
                        // oh, so close, the entry is in inconsistent state.
                        // it is either changing or has changed while we were reading.
                        // treat it as a miss.
                        break;
                    }

                    return true;
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

            value = default;
            return false;
        }

        // we generally do not OOM in casts, just return null unless throwOnFail is specified.
        private Entry[]? CreateCastCache(int size, bool throwOnFail = false)
        {
            // size must be positive
            Debug.Assert(size > 1);
            // size must be a power of two
            Debug.Assert((size & (size - 1)) == 0);

            Entry[]? table = null;
            try
            {
                table = new Entry[size + 1];
            }
            catch (OutOfMemoryException) when (!throwOnFail)
            {
            }

            if (table == null)
            {
                size = _initialCacheSize;
                try
                {
                    table = new Entry[size + 1];
                }
                catch (OutOfMemoryException)
                {
                }
            }

            if (table == null)
            {
                return table;
            }

            ref Entry tableData = ref TableData(table);

            // set the table mask. we need it often, do not want to compute each time.
            TableMask(ref tableData) = size - 1;

            // Fibonacci hash reduces the value into desired range by shifting right by the number of leading zeroes in 'size-1'
            byte shift = (byte)BitOperations.LeadingZeroCount(size - 1);
            HashShift(ref tableData) = shift;

            return table;
        }

        internal void TrySet(TKey key, TValue value)
        {
            int bucket;
            int hash = key!.GetHashCode();
            ref Entry tableData = ref Unsafe.NullRef<Entry>();

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

                bucket = HashToBucket(ref tableData, hash);
                int index = bucket;
                ref Entry pEntry = ref Element(ref tableData, index);

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

                    uint version = pEntry.Version;

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
                        uint versionOrig = Interlocked.CompareExchange(ref pEntry.Version, newVersion, version);
                        if (versionOrig == version)
                        {
                            pEntry.Hash = hash;
                            pEntry._key = key;
                            pEntry._value = value;

                            // entry is in inconsistent state and cannot be read or written to until we
                            // update the version, which is the last thing we do here
                            Volatile.Write(ref pEntry.Version, newVersion + 1);
                            return;
                        }
                        // someone snatched the entry. try the next one in the bucket.
                    }

                    if (hash == pEntry.Hash && key.Equals(pEntry._key))
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
            byte victimDistance = (byte)(VictimCounter(ref tableData)++ & (BUCKET_SIZE - 1));
            // position the victim in a quadratic reprobe bucket
            int victim = (victimDistance * victimDistance + victimDistance) / 2;

            {
                ref Entry pEntry = ref Element(ref tableData, (bucket + victim) & TableMask(ref tableData));

                uint version = pEntry.Version;

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

                uint newVersion = (uint)((victimDistance << VERSION_NUM_SIZE) + (version & VERSION_NUM_MASK) + 1);
                uint versionOrig = Interlocked.CompareExchange(ref pEntry.Version, newVersion, version);

                if (versionOrig == version)
                {
                    pEntry.Hash = hash;
                    pEntry._key = key;
                    pEntry._value = value;
                    Volatile.Write(ref pEntry.Version, newVersion + 1);
                }
            }
        }

        private static int CacheElementCount(ref Entry tableData)
        {
            return TableMask(ref tableData) + 1;
        }

        private void FlushCurrentCache()
        {
            ref Entry tableData = ref TableData(_table);
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
            Entry[]? newTable = CreateCastCache(size);
            if (newTable == null)
            {
                return false;
            }

            _table = newTable;
            return true;
        }

        private bool TryGrow(ref Entry tableData)
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
