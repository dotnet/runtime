// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;

using static System.Runtime.CompilerServices.CastCache;

namespace System.Runtime.CompilerServices
{
    internal static unsafe class CastCache
    {
        private static int[]? s_table;

        [DebuggerDisplay("Source = {_source}; Target = {_targetAndResult & ~1}; Result = {_targetAndResult & 1}; VersionNum = {_version & ((1 << 29) - 1)}; Distance = {_version >> 29};")]
        [StructLayout(LayoutKind.Sequential)]
        private struct CastCacheEntry
        {
            // version has the following structure:
            // [ distance:3bit |  versionNum:29bit ]
            //
            // distance is how many iterations the entry is from it ideal position.
            // we use that for preemption.
            //
            // versionNum is a monotonicaly increasing numerical tag.
            // Writer "claims" entry by atomically incrementing the tag. Thus odd number indicates an entry in progress.
            // Upon completion of adding an entry the tag is incremented again making it even. Even number indicates a complete entry.
            //
            // Readers will read the version twice before and after retrieving the entry.
            // To have a usable entry both reads must yield the same even version.
            //
            internal int _version;
            internal nuint _source;
            // pointers have unused lower bits due to alignment, we use one for the result
            internal nuint _targetAndResult;
        };

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
            return ref MemoryMarshal.GetArrayDataReference(table);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref CastCacheEntry Element(ref int tableData, int index)
        {
            // element 0 is used for embedded aux data, skip it
            return ref Unsafe.Add(ref Unsafe.As<int, CastCacheEntry>(ref tableData), index + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int HashShift(ref int tableData)
        {
            return tableData;
        }

        // TableMask is "size - 1"
        // we need that more often that we need size
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int TableMask(ref int tableData)
        {
            return Unsafe.Add(ref tableData, 1);
        }

        internal enum CastResult
        {
            CannotCast = 0,
            CanCast = 1,
            MaybeCast = 2
        }

        // NOTE!!
        // This is a copy of C++ implementation in castcache.cpp
        // Keep the copies, if possible, in sync.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static CastResult TryGet(nuint source, nuint target)
        {
            const int BUCKET_SIZE = 8;

            // table is initialized and updated by native code that guarantees it is not null.
            ref int tableData = ref TableData(s_table!);

            int index = KeyToBucket(ref tableData, source, target);
            for (int i = 0; i < BUCKET_SIZE;)
            {
                ref CastCacheEntry pEntry = ref Element(ref tableData, index);

                // must read in this order: version -> [entry parts] -> version
                // if version is odd or changes, the entry is inconsistent and thus ignored
                int version = Volatile.Read(ref pEntry._version);
                nuint entrySource = pEntry._source;

                // mask the lower version bit to make it even.
                // This way we can check if version is odd or changing in just one compare.
                version &= ~1;

                if (entrySource == source)
                {
                    nuint entryTargetAndResult = pEntry._targetAndResult;
                    // target never has its lower bit set.
                    // a matching entryTargetAndResult would the have same bits, except for the lowest one, which is the result.
                    entryTargetAndResult ^= target;
                    if (entryTargetAndResult <= 1)
                    {
                        // make sure 'version' is loaded after 'source' and 'targetAndResults'
                        //
                        // We can either:
                        // - use acquires for both _source and _targetAndResults or
                        // - issue a load barrier before reading _version
                        // benchmarks on available hardware show that use of a read barrier is cheaper.
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
    }
}
