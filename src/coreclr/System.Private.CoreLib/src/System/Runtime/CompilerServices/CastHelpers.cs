// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

using Internal.Runtime.CompilerServices;

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
        private static int KeyToBucket(ref int tableData, nuint source, nuint target)
        {
            // upper bits of addresses do not vary much, so to reduce loss due to cancelling out,
            // we do `rotl(source, <half-size>) ^ target` for mixing inputs.
            // then we use fibonacci hashing to reduce the value to desired size.

            int hashShift = HashShift(ref tableData);
#if TARGET_64BIT
            ulong hash = (((ulong)source << 32) | ((ulong)source >> 32)) ^ (ulong)target;
            return (int)((hash * 11400714819323198485ul) >> hashShift);
#else
            uint hash = (((uint)source >> 16) | ((uint)source << 16)) ^ (uint)target;
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

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern object IsInstanceOfAny_NoCacheLookup(void* toTypeHnd, object obj);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern object ChkCastAny_NoCacheLookup(void* toTypeHnd, object obj);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern ref byte Unbox_Helper(void* toTypeHnd, object obj);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void WriteBarrier(ref object? dst, object obj);

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
            const int unrollSize = 4;

            if (obj != null)
            {
                MethodTable* mt = RuntimeHelpers.GetMethodTable(obj);
                nint interfaceCount = mt->InterfaceCount;
                if (interfaceCount != 0)
                {
                    MethodTable** interfaceMap = mt->InterfaceMap;
                    if (interfaceCount < unrollSize)
                    {
                        // If not enough for unrolled, jmp straight to small loop
                        // as we already know there is one or more interfaces so don't need to check again.
                        goto few;
                    }

                    do
                    {
                        if (interfaceMap[0] == toTypeHnd ||
                            interfaceMap[1] == toTypeHnd ||
                            interfaceMap[2] == toTypeHnd ||
                            interfaceMap[3] == toTypeHnd)
                        {
                            goto done;
                        }

                        interfaceMap += unrollSize;
                        interfaceCount -= unrollSize;
                    } while (interfaceCount >= unrollSize);

                    if (interfaceCount == 0)
                    {
                        // If none remaining, skip the short loop
                        goto extra;
                    }

                few:
                    do
                    {
                        if (interfaceMap[0] == toTypeHnd)
                        {
                            goto done;
                        }

                        // Assign next offset
                        interfaceMap++;
                        interfaceCount--;
                    } while (interfaceCount > 0);
                }

            extra:
                if (mt->NonTrivialInterfaceCast)
                {
                    goto slowPath;
                }

                obj = null;
            }

        done:
            return obj;

        slowPath:
            return IsInstance_Helper(toTypeHnd, obj);
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

            // this helper is not supposed to be used with type-equivalent "to" type.
            Debug.Assert(!((MethodTable*)toTypeHnd)->HasTypeEquivalence);

            obj = null;

        done:
            return obj;
        }

        [DebuggerHidden]
        [StackTraceHidden]
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static object? IsInstance_Helper(void* toTypeHnd, object obj)
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
        private static object? ChkCast_Helper(void* toTypeHnd, object obj)
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
            const int unrollSize = 4;

            if (obj != null)
            {
                MethodTable* mt = RuntimeHelpers.GetMethodTable(obj);
                nint interfaceCount = mt->InterfaceCount;
                if (interfaceCount == 0)
                {
                    goto slowPath;
                }

                MethodTable** interfaceMap = mt->InterfaceMap;
                if (interfaceCount < unrollSize)
                {
                    // If not enough for unrolled, jmp straight to small loop
                    // as we already know there is one or more interfaces so don't need to check again.
                    goto few;
                }

                do
                {
                    if (interfaceMap[0] == toTypeHnd ||
                        interfaceMap[1] == toTypeHnd ||
                        interfaceMap[2] == toTypeHnd ||
                        interfaceMap[3] == toTypeHnd)
                    {
                        goto done;
                    }

                    // Assign next offset
                    interfaceMap += unrollSize;
                    interfaceCount -= unrollSize;
                } while (interfaceCount >= unrollSize);

                if (interfaceCount == 0)
                {
                    // If none remaining, skip the short loop
                    goto slowPath;
                }

            few:
                do
                {
                    if (interfaceMap[0] == toTypeHnd)
                    {
                        goto done;
                    }

                    // Assign next offset
                    interfaceMap++;
                    interfaceCount--;
                } while (interfaceCount > 0);

                goto slowPath;
            }

        done:
            return obj;

        slowPath:
            return ChkCast_Helper(toTypeHnd, obj);
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
            return ChkCast_Helper(toTypeHnd, obj);
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

        internal struct ArrayElement
        {
            public object? Value;
        }

        [DebuggerHidden]
        [StackTraceHidden]
        [DebuggerStepThrough]
        private static ref object? ThrowArrayMismatchException()
        {
            throw new ArrayTypeMismatchException();
        }

        [DebuggerHidden]
        [StackTraceHidden]
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static ref object? LdelemaRef(Array array, int index, void* type)
        {
            // this will throw appropriate exceptions if array is null or access is out of range.
            ref object? element = ref Unsafe.As<ArrayElement[]>(array)[index].Value;
            void* elementType = RuntimeHelpers.GetMethodTable(array)->ElementType;

            if (elementType == type)
                return ref element;

            return ref ThrowArrayMismatchException();
        }

        [DebuggerHidden]
        [StackTraceHidden]
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static void StelemRef(Array array, int index, object? obj)
        {
            // this will throw appropriate exceptions if array is null or access is out of range.
            ref object? element = ref Unsafe.As<ArrayElement[]>(array)[index].Value;
            void* elementType = RuntimeHelpers.GetMethodTable(array)->ElementType;

            if (obj == null)
                goto assigningNull;

            if (elementType != RuntimeHelpers.GetMethodTable(obj))
                goto notExactMatch;

            doWrite:
                WriteBarrier(ref element, obj);
                return;

            assigningNull:
                element = null;
                return;

            notExactMatch:
                if (array.GetType() == typeof(object[]))
                    goto doWrite;

            StelemRef_Helper(ref element, elementType, obj);
        }

        [DebuggerHidden]
        [StackTraceHidden]
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void StelemRef_Helper(ref object? element, void* elementType, object obj)
        {
            CastResult result = TryGet((nuint)RuntimeHelpers.GetMethodTable(obj), (nuint)elementType);
            if (result == CastResult.CanCast)
            {
                WriteBarrier(ref element, obj);
                return;
            }

            StelemRef_Helper_NoCacheLookup(ref element, elementType, obj);
        }

        [DebuggerHidden]
        [StackTraceHidden]
        [DebuggerStepThrough]
        private static void StelemRef_Helper_NoCacheLookup(ref object? element, void* elementType, object obj)
        {
            Debug.Assert(obj != null);

            obj = IsInstanceOfAny_NoCacheLookup(elementType, obj);
            if (obj != null)
            {
                WriteBarrier(ref element, obj);
                return;
            }

            throw new ArrayTypeMismatchException();
        }
    }
}
