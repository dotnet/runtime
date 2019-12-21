// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Internal.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

#pragma warning disable SA1121 // explicitly using type aliases instead of built-in types
#if BIT64
using nint = System.Int64;
#else
using nint = System.Int32;
#endif

namespace System.Runtime.CompilerServices
{
    internal static unsafe class CastHelpers
    {
        private static int[]? s_table;

        [DebuggerDisplay("Source = {_source}; Target = {_targetAndResult & ~1}; Result = {_targetAndResult & 1}; VersionNum = {_version & ((1 << 29) - 1)}; Distance = {_version >> 29};")]
        [StructLayout(LayoutKind.Sequential)]
        private struct CastCacheEntry
        {
            // version has the following structure:
            // [ distance:3bit |  versionNum:29bit ]
            //
            // distance is how many iterations is the entry from it ideal position.
            // we use that for preemption.
            //
            // versionNum is a monotonicaly increasing numerical tag.
            // Writer "claims" entry by atomically incrementing the tag. Thus odd number indicates an entry in progress.
            // Upon completion of adding an entry the tag is incremented again making it even. Even number indicates a complete entry.
            //
            // Readers will read the version twice before and after retrieving the entry.
            // To have a usable entry both reads must yield the same even version.
            //
            internal int  _version;
            internal nint _source;
            // pointers have unused lower bits due to alignment, we use one for the result
            internal nint _targetAndResult;
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int KeyToBucket(int[] table, nint source, nint target)
        {
            // upper bits of addresses do not vary much, so to reduce loss due to cancelling out,
            // we do `rotl(source, <half-size>) ^ target` for mixing inputs.
            // then we use fibonacci hashing to reduce the value to desired size.

#if BIT64
            ulong hash = (((ulong)source << 32) | ((ulong)source >> 32)) ^ (ulong)target;
            return (int)((hash * 11400714819323198485ul) >> HashShift(table));
#else
            uint hash = (((uint)source >> 16) | ((uint)source << 16)) ^ (uint)target;
            return (int)((hash * 2654435769ul) >> HashShift(table));
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref int AuxData(int[] table)
        {
            // element 0 is used for embedded aux data
            return ref Unsafe.As<byte, int>(ref table.GetRawSzArrayData());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref CastCacheEntry Element(int[] table, int index)
        {
            // element 0 is used for embedded aux data, skip it
            return ref Unsafe.Add(ref Unsafe.As<int, CastCacheEntry>(ref AuxData(table)), index + 1);
        }

        // TableMask is "size - 1"
        // we need that more often that we need size
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int TableMask(int[] table)
        {
            return AuxData(table);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int HashShift(int[] table)
        {
            return Unsafe.Add(ref AuxData(table), 1);
        }

        private enum CastResult
        {
            CannotCast = 0,
            CanCast = 1,
            MaybeCast = 2
        }

        // NOTE!!
        // This is a copy of C++ implementation in CastCache.cpp
        // Keep the copies, if possible, in sync.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static CastResult TryGet(nint source, nint target)
        {
            const int BUCKET_SIZE = 8;
            int[]? table = s_table;

            // we use NULL as a sentinel for a rare case when a table could not be allocated
            // because we avoid OOMs.
            // we could use 0-element table instead, but then we would have to check the size here.
            if (table == null)
            {
                return CastResult.MaybeCast;
            }

            int index = KeyToBucket(table, source, target);
            ref CastCacheEntry pEntry = ref Element(table, index);

            for (int i = 0; i < BUCKET_SIZE; i++)
            {
                // must read in this order: version -> entry parts -> version
                // if version is odd or changes, the entry is inconsistent and thus ignored
                int version1 = Volatile.Read(ref pEntry._version);
                nint entrySource = pEntry._source;

                if (entrySource == source)
                {
                    nint entryTargetAndResult = Volatile.Read(ref pEntry._targetAndResult);

                    // target never has its lower bit set.
                    // a matching entryTargetAndResult would have same bits, except for the lowest one, which is the result.
                    entryTargetAndResult ^= target;
                    if (entryTargetAndResult <= 1)
                    {
                        int version2 = pEntry._version;
                        if (version2 != version1 || ((version1 & 1) != 0))
                        {
                            // oh, so close, the entry is in inconsistent state.
                            // it is either changing or has changed while we were reading.
                            // treat it as a miss.
                            break;
                        }

                        return (CastResult)entryTargetAndResult;
                    }
                }

                if (version1 == 0)
                {
                    // the rest of the bucket is unclaimed, no point to search further
                    break;
                }

                // quadratic reprobe
                index += i;
                pEntry = ref Element(table, index & TableMask(table));
            }

            return CastResult.MaybeCast;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static CastResult ObjIsInstanceOfCached(object obj, void* toTypeHnd)
        {
            void* mt = RuntimeHelpers.GetMethodTable(obj);

            if (mt == toTypeHnd)
                return CastResult.CanCast;

            return TryGet((nint)mt, (nint)toTypeHnd);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern object JITutil_IsInstanceOfAny_NoCacheLookup(void* toTypeHnd, object obj);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern object JITutil_ChkCastAny_NoCacheLookup(void* toTypeHnd, object obj);

        // IsInstanceOf test used for unusual cases (naked type parameters, variant generic types)
        // Unlike the IsInstanceOfInterface, IsInstanceOfClass, and IsIsntanceofArray functions,
        // this test must deal with all kinds of type tests
        private static object? JIT_IsInstanceOfAny(void* toTypeHnd, object? obj)
        {
            CastResult result;

            if (obj == null ||
                (result = ObjIsInstanceOfCached(obj, toTypeHnd)) == CastResult.CanCast)
            {
                return obj;
            }

            if (result == CastResult.CannotCast)
            {
                return null;
            }

            // fall through to the slow helper
            return JITutil_IsInstanceOfAny_NoCacheLookup(toTypeHnd, obj);
        }

        // ChkCast test used for unusual cases (naked type parameters, variant generic types)
        // Unlike the ChkCastInterface, ChkCastClass, and ChkCastArray functions,
        // this test must deal with all kinds of type tests
        private static object? JIT_ChkCastAny(void* toTypeHnd, object? obj)
        {
            CastResult result;

            if (obj == null ||
                (result = ObjIsInstanceOfCached(obj, toTypeHnd)) == CastResult.CanCast)
            {
                return obj;
            }

            object objRet = JITutil_ChkCastAny_NoCacheLookup(toTypeHnd, obj);
            // Make sure that the fast helper have not lied
            Debug.Assert(result != CastResult.CannotCast);
            return objRet;
        }
    }
}
