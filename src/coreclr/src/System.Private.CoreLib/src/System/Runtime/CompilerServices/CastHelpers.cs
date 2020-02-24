// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Internal.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

#pragma warning disable SA1121 // explicitly using type aliases instead of built-in types
#if TARGET_64BIT
using nuint = System.UInt64;
#else
using nuint = System.UInt32;
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
            internal int  _version;
            internal nuint _source;
            // pointers have unused lower bits due to alignment, we use one for the result
            internal nuint _targetAndResult;
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int KeyToBucket(int[] table, nuint source, nuint target)
        {
            // upper bits of addresses do not vary much, so to reduce loss due to cancelling out,
            // we do `rotl(source, <half-size>) ^ target` for mixing inputs.
            // then we use fibonacci hashing to reduce the value to desired size.

            int hashShift = HashShift(table);
#if TARGET_64BIT
            ulong hash = (((ulong)source << 32) | ((ulong)source >> 32)) ^ (ulong)target;
            return (int)((hash * 11400714819323198485ul) >> hashShift);
#else
            uint hash = (((uint)source >> 16) | ((uint)source << 16)) ^ (uint)target;
            return (int)((hash * 2654435769u) >> hashShift);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref int AuxData(int[] table)
        {
            // element 0 is used for embedded aux data
            return ref MemoryMarshal.GetArrayDataReference(table);
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
        // This is a copy of C++ implementation in castcache.cpp
        // Keep the copies, if possible, in sync.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static CastResult TryGet(nuint source, nuint target)
        {
            const int BUCKET_SIZE = 8;
            int[]? table = s_table;

            // we use NULL as a sentinel for a rare case when a table could not be allocated
            // because we avoid OOMs.
            // we could use 0-element table instead, but then we would have to check the size here.
            if (table != null)
            {
                int index = KeyToBucket(table, source, target);
                for (int i = 0; i < BUCKET_SIZE;)
                {
                    ref CastCacheEntry pEntry = ref Element(table, index);

                    // must read in this order: version -> entry parts -> version
                    // if version is odd or changes, the entry is inconsistent and thus ignored
                    int version = Volatile.Read(ref pEntry._version);
                    nuint entrySource = pEntry._source;

                    // mask the lower version bit to make it even.
                    // This way we can check if version is odd or changing in just one compare.
                    version &= ~1;

                    if (entrySource == source)
                    {
                        nuint entryTargetAndResult = Volatile.Read(ref pEntry._targetAndResult);
                        // target never has its lower bit set.
                        // a matching entryTargetAndResult would the have same bits, except for the lowest one, which is the result.
                        entryTargetAndResult ^= target;
                        if (entryTargetAndResult <= 1)
                        {
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
                    index = (index + i) & TableMask(table);
                }
            }
            return CastResult.MaybeCast;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern object IsInstanceOfAny_NoCacheLookup(void* toTypeHnd, object obj);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern object ChkCastAny_NoCacheLookup(void* toTypeHnd, object obj);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern ref byte Unbox_Helper(void* toTypeHnd, object obj);

        // IsInstanceOf test used for unusual cases (naked type parameters, variant generic types)
        // Unlike the IsInstanceOfInterface and IsInstanceOfClass functions,
        // this test must deal with all kinds of type tests
        [DebuggerHidden]
        [StackTraceHidden]
        [DebuggerStepThrough]
        private static object? IsInstanceOfAny(void* toTypeHnd, object? obj)
        {
            if (obj != null)
            {
                void* mt = RuntimeHelpers.GetMethodTable(obj);
                if (mt != toTypeHnd)
                {
                    CastResult result = TryGet((nuint)mt, (nuint)toTypeHnd);
                    if (result == CastResult.CanCast)
                    {
                        // do nothing
                    }
                    else if (result == CastResult.CannotCast)
                    {
                        obj = null;
                    }
                    else
                    {
                        goto slowPath;
                    }
                }
            }

            return obj;

        slowPath:
            // fall through to the slow helper
            return IsInstanceOfAny_NoCacheLookup(toTypeHnd, obj);
        }

        [DebuggerHidden]
        [StackTraceHidden]
        [DebuggerStepThrough]
        private static object? IsInstanceOfInterface(void* toTypeHnd, object? obj)
        {
            if (obj != null)
            {
                MethodTable* mt = RuntimeHelpers.GetMethodTable(obj);
                nuint interfaceCount = mt->InterfaceCount;
                if (interfaceCount != 0)
                {
                    MethodTable** interfaceMap = mt->InterfaceMap;
                    for (nuint i = 0; ; i += 4)
                    {
                        if (interfaceMap[i + 0] == toTypeHnd)
                            goto done;
                        if (--interfaceCount == 0)
                            break;
                        if (interfaceMap[i + 1] == toTypeHnd)
                            goto done;
                        if (--interfaceCount == 0)
                            break;
                        if (interfaceMap[i + 2] == toTypeHnd)
                            goto done;
                        if (--interfaceCount == 0)
                            break;
                        if (interfaceMap[i + 3] == toTypeHnd)
                            goto done;
                        if (--interfaceCount == 0)
                            break;
                    }
                }

                if (mt->NonTrivialInterfaceCast)
                {
                    goto slowPath;
                }

                obj = null;
            }

        done:
            return obj;

        slowPath:
            return IsInstanceHelper(toTypeHnd, obj);
        }

        [DebuggerHidden]
        [StackTraceHidden]
        [DebuggerStepThrough]
        private static object? IsInstanceOfClass(void* toTypeHnd, object? obj)
        {
            if (obj == null || RuntimeHelpers.GetMethodTable(obj) == toTypeHnd)
                return obj;

            MethodTable* mt = RuntimeHelpers.GetMethodTable(obj)->ParentMethodTable;
            for (; ; )
            {
                if (mt == toTypeHnd)
                    goto done;

                if (mt == null)
                    break;

                mt = mt->ParentMethodTable;
                if (mt == toTypeHnd)
                    goto done;

                if (mt == null)
                    break;

                mt = mt->ParentMethodTable;
                if (mt == toTypeHnd)
                    goto done;

                if (mt == null)
                    break;

                mt = mt->ParentMethodTable;
                if (mt == toTypeHnd)
                    goto done;

                if (mt == null)
                    break;

                mt = mt->ParentMethodTable;
            }

            if (RuntimeHelpers.GetMethodTable(obj)->HasTypeEquivalence)
            {
                goto slowPath;
            }

            obj = null;

        done:
            return obj;

        slowPath:
            return IsInstanceHelper(toTypeHnd, obj);
        }

        [DebuggerHidden]
        [StackTraceHidden]
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static object? IsInstanceHelper(void* toTypeHnd, object obj)
        {
            CastResult result = TryGet((nuint)RuntimeHelpers.GetMethodTable(obj), (nuint)toTypeHnd);
            if (result == CastResult.CanCast)
            {
                return obj;
            }
            else if (result == CastResult.CannotCast)
            {
                return null;
            }

            // fall through to the slow helper
            return IsInstanceOfAny_NoCacheLookup(toTypeHnd, obj);
        }

        // ChkCast test used for unusual cases (naked type parameters, variant generic types)
        // Unlike the ChkCastInterface and ChkCastClass functions,
        // this test must deal with all kinds of type tests
        [DebuggerHidden]
        [StackTraceHidden]
        [DebuggerStepThrough]
        private static object? ChkCastAny(void* toTypeHnd, object? obj)
        {
            CastResult result;

            if (obj != null)
            {
                void* mt = RuntimeHelpers.GetMethodTable(obj);
                if (mt != toTypeHnd)
                {
                    result = TryGet((nuint)mt, (nuint)toTypeHnd);
                    if (result != CastResult.CanCast)
                    {
                        goto slowPath;
                    }
                }
            }

            return obj;

        slowPath:
            // fall through to the slow helper
            object objRet = ChkCastAny_NoCacheLookup(toTypeHnd, obj);
            // Make sure that the fast helper have not lied
            Debug.Assert(result != CastResult.CannotCast);
            return objRet;
        }

        [DebuggerHidden]
        [StackTraceHidden]
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static object? ChkCastHelper(void* toTypeHnd, object obj)
        {
            CastResult result = TryGet((nuint)RuntimeHelpers.GetMethodTable(obj), (nuint)toTypeHnd);
            if (result == CastResult.CanCast)
            {
                return obj;
            }

            // fall through to the slow helper
            return ChkCastAny_NoCacheLookup(toTypeHnd, obj);
        }

        [DebuggerHidden]
        [StackTraceHidden]
        [DebuggerStepThrough]
        private static object? ChkCastInterface(void* toTypeHnd, object? obj)
        {
            if (obj != null)
            {
                MethodTable* mt = RuntimeHelpers.GetMethodTable(obj);
                nuint interfaceCount = mt->InterfaceCount;
                if (interfaceCount == 0)
                {
                    goto slowPath;
                }

                MethodTable** interfaceMap = mt->InterfaceMap;
                for (nuint i = 0; ; i += 4)
                {
                    if (interfaceMap[i + 0] == toTypeHnd)
                        goto done;
                    if (--interfaceCount == 0)
                        goto slowPath;
                    if (interfaceMap[i + 1] == toTypeHnd)
                        goto done;
                    if (--interfaceCount == 0)
                        goto slowPath;
                    if (interfaceMap[i + 2] == toTypeHnd)
                        goto done;
                    if (--interfaceCount == 0)
                        goto slowPath;
                    if (interfaceMap[i + 3] == toTypeHnd)
                        goto done;
                    if (--interfaceCount == 0)
                        goto slowPath;
                }
            }

        done:
            return obj;

        slowPath:
            return ChkCastHelper(toTypeHnd, obj);
        }

        [DebuggerHidden]
        [StackTraceHidden]
        [DebuggerStepThrough]
        private static object? ChkCastClass(void* toTypeHnd, object? obj)
        {
            if (obj == null || RuntimeHelpers.GetMethodTable(obj) == toTypeHnd)
            {
                return obj;
            }

            return ChkCastClassSpecial(toTypeHnd, obj);
        }

        [DebuggerHidden]
        [StackTraceHidden]
        [DebuggerStepThrough]
        private static object? ChkCastClassSpecial(void* toTypeHnd, object obj)
        {
            MethodTable* mt = RuntimeHelpers.GetMethodTable(obj);
            Debug.Assert(mt != toTypeHnd, "The check for the trivial cases should be inlined by the JIT");

            for (; ; )
            {
                mt = mt->ParentMethodTable;
                if (mt == toTypeHnd)
                    goto done;

                if (mt == null)
                    break;

                mt = mt->ParentMethodTable;
                if (mt == toTypeHnd)
                    goto done;

                if (mt == null)
                    break;

                mt = mt->ParentMethodTable;
                if (mt == toTypeHnd)
                    goto done;

                if (mt == null)
                    break;

                mt = mt->ParentMethodTable;
                if (mt == toTypeHnd)
                    goto done;

                if (mt == null)
                    break;
            }

            goto slowPath;

        done:
            return obj;

        slowPath:
            return ChkCastHelper(toTypeHnd, obj);
        }

        [DebuggerHidden]
        [StackTraceHidden]
        [DebuggerStepThrough]
        private static ref byte Unbox(void* toTypeHnd, object obj)
        {
            // this will throw NullReferenceException if obj is null, attributed to the user code, as expected.
            if (RuntimeHelpers.GetMethodTable(obj) == toTypeHnd)
                return ref obj.GetRawData();

            return ref Unbox_Helper(toTypeHnd, obj);
        }
    }
}
