// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices
{
    [StackTraceHidden]
    [DebuggerStepThrough]
    internal static unsafe partial class CastHelpers
    {
        // In coreclr the table is allocated and written to on the native side.
        internal static int[]? s_table;

        [LibraryImport(RuntimeHelpers.QCall)]
        internal static partial void ThrowInvalidCastException(void* fromTypeHnd, void* toTypeHnd);

        internal static void ThrowInvalidCastException(object fromTypeHnd, void* toTypeHnd)
        {
            ThrowInvalidCastException(RuntimeHelpers.GetMethodTable(fromTypeHnd), toTypeHnd);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern object IsInstanceOfAny_NoCacheLookup(void* toTypeHnd, object obj);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern object ChkCastAny_NoCacheLookup(void* toTypeHnd, object obj);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void WriteBarrier(ref object? dst, object? obj);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void UnboxNullableValue(ref byte destPtr, MethodTable* typeMT, object obj);

        // IsInstanceOf test used for unusual cases (naked type parameters, variant generic types)
        // Unlike the IsInstanceOfInterface and IsInstanceOfClass functions,
        // this test must deal with all kinds of type tests
        [DebuggerHidden]
        internal static object? IsInstanceOfAny(void* toTypeHnd, object? obj)
        {
            if (obj != null)
            {
                void* mt = RuntimeHelpers.GetMethodTable(obj);
                if (mt != toTypeHnd)
                {
                    CastResult result = CastCache.TryGet(s_table!, (nuint)mt, (nuint)toTypeHnd);
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
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static object? IsInstance_Helper(void* toTypeHnd, object obj)
        {
            CastResult result = CastCache.TryGet(s_table!, (nuint)RuntimeHelpers.GetMethodTable(obj), (nuint)toTypeHnd);
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
        internal static object? ChkCastAny(void* toTypeHnd, object? obj)
        {
            CastResult result;

            if (obj != null)
            {
                void* mt = RuntimeHelpers.GetMethodTable(obj);
                if (mt != toTypeHnd)
                {
                    result = CastCache.TryGet(s_table!, (nuint)mt, (nuint)toTypeHnd);
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
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static object? ChkCast_Helper(void* toTypeHnd, object obj)
        {
            CastResult result = CastCache.TryGet(s_table!, (nuint)RuntimeHelpers.GetMethodTable(obj), (nuint)toTypeHnd);
            if (result == CastResult.CanCast)
            {
                return obj;
            }

            // fall through to the slow helper
            return ChkCastAny_NoCacheLookup(toTypeHnd, obj);
        }

        [DebuggerHidden]
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
        private static object? ChkCastClass(void* toTypeHnd, object? obj)
        {
            if (obj == null || RuntimeHelpers.GetMethodTable(obj) == toTypeHnd)
            {
                return obj;
            }

            return ChkCastClassSpecial(toTypeHnd, obj);
        }

        // Optimized helper for classes. Assumes that the trivial cases
        // has been taken care of by the inlined check
        [DebuggerHidden]
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
        private static ref byte Unbox(MethodTable* toTypeHnd, object obj)
        {
            // This will throw NullReferenceException if obj is null.
            if (RuntimeHelpers.GetMethodTable(obj) == toTypeHnd)
                return ref obj.GetRawData();

            return ref Unbox_Helper(toTypeHnd, obj);
        }

        [DebuggerHidden]
        private static void ThrowIndexOutOfRangeException()
        {
            throw new IndexOutOfRangeException();
        }

        [DebuggerHidden]
        private static void ThrowArrayMismatchException()
        {
            throw new ArrayTypeMismatchException();
        }

        [DebuggerHidden]
        private static ref object? LdelemaRef(object?[] array, nint index, void* type)
        {
            // This will throw NullReferenceException if array is null.
            if ((nuint)index >= (uint)array.Length)
                ThrowIndexOutOfRangeException();

            Debug.Assert(index >= 0);
            ref object? element = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index);
            void* elementType = RuntimeHelpers.GetMethodTable(array)->ElementType;

            if (elementType != type)
                ThrowArrayMismatchException();

            return ref element;
        }

        [DebuggerHidden]
        private static void StelemRef(object?[] array, nint index, object? obj)
        {
            // This will throw NullReferenceException if array is null.
            if ((nuint)index >= (uint)array.Length)
                ThrowIndexOutOfRangeException();

            Debug.Assert(index >= 0);
            ref object? element = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index);
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
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void StelemRef_Helper(ref object? element, void* elementType, object obj)
        {
            CastResult result = CastCache.TryGet(s_table!, (nuint)RuntimeHelpers.GetMethodTable(obj), (nuint)elementType);
            if (result == CastResult.CanCast)
            {
                WriteBarrier(ref element, obj);
                return;
            }

            StelemRef_Helper_NoCacheLookup(ref element, elementType, obj);
        }

        [DebuggerHidden]
        private static void StelemRef_Helper_NoCacheLookup(ref object? element, void* elementType, object obj)
        {
            Debug.Assert(obj != null);

            obj = IsInstanceOfAny_NoCacheLookup(elementType, obj);
            if (obj == null)
            {
                ThrowArrayMismatchException();
            }

            WriteBarrier(ref element, obj);
        }

        [DebuggerHidden]
        private static unsafe void ArrayTypeCheck(object obj, Array array)
        {
            Debug.Assert(obj != null);

            void* elementType = RuntimeHelpers.GetMethodTable(array)->ElementType;
            Debug.Assert(elementType != RuntimeHelpers.GetMethodTable(obj)); // Should be handled by caller

            CastResult result = CastCache.TryGet(s_table!, (nuint)RuntimeHelpers.GetMethodTable(obj), (nuint)elementType);
            if (result == CastResult.CanCast)
            {
                return;
            }

            ArrayTypeCheck_Helper(obj, elementType);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe void ArrayTypeCheck_Helper(object obj, void* elementType)
        {
            Debug.Assert(obj != null);

            obj = IsInstanceOfAny_NoCacheLookup(elementType, obj);
            if (obj == null)
            {
                ThrowArrayMismatchException();
            }
        }

        // Helpers for Unboxing
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void InitValueClass(ref byte destBytes, MethodTable *pMT)
        {
            uint numInstanceFieldBytes = pMT->GetNumInstanceFieldBytes();
            if ((((uint)Unsafe.AsPointer(ref destBytes) | numInstanceFieldBytes) & ((uint)sizeof(void*) - 1)) != 0)
            {
                // If we have a non-pointer aligned instance field bytes count, or a non-aligned destBytes, we can zero out the data byte by byte
                // And we do not need to concern ourselves with references
                SpanHelpers.ClearWithoutReferences(ref destBytes, numInstanceFieldBytes);
            }
            else
            {
                // Otherwise, use the helper which is safe for that situation
                SpanHelpers.ClearWithReferences(ref Unsafe.As<byte, IntPtr>(ref destBytes), (nuint)numInstanceFieldBytes / (nuint)sizeof(IntPtr));
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void InitValueClassPtr(byte* destBytes, MethodTable *pMT)
        {
            uint numInstanceFieldBytes = pMT->GetNumInstanceFieldBytes();
            if ((((uint)destBytes | numInstanceFieldBytes) & ((uint)sizeof(void*) - 1)) != 0)
            {
                // If we have a non-pointer aligned instance field bytes count, or a non-aligned destBytes, we can zero out the data byte by byte
                // And we do not need to concern ourselves with references
                SpanHelpers.ClearWithoutReferences(ref Unsafe.AsRef<byte>(destBytes), numInstanceFieldBytes);
            }
            else
            {
                // Otherwise, use the helper which is safe for that situation
                SpanHelpers.ClearWithReferences(ref Unsafe.AsRef<IntPtr>(destBytes), (nuint)numInstanceFieldBytes / (nuint)sizeof(IntPtr));
            }
        }

#if FEATURE_TYPEEQUIVALENCE
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool AreTypesEquivalent(MethodTable* pMTa, MethodTable* pMTb)
        {
            if (pMTa == pMTb)
            {
                return true;
            }

            if (!pMTa->HasTypeEquivalence || !pMTb->HasTypeEquivalence)
            {
                return false;
            }

            return RuntimeHelpers.AreTypesEquivalent(pMTa, pMTb);
        }
#endif // FEATURE_TYPEEQUIVALENCE

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsNullableForType(MethodTable* typeMT, MethodTable* boxedMT)
        {
            if (!typeMT->IsNullable)
            {
                return false;
            }

            // Normally getting the first generic argument involves checking the PerInstInfo to get the count of generic dictionaries
            // in the hierarchy, and then doing a bit of math to find the right dictionary, but since we know this is nullable
            // we can do a simple double deference to do the same thing.
            Debug.Assert(typeMT->InstantiationArg0() == **typeMT->PerInstInfo);
            MethodTable *pMTNullableArg = **typeMT->PerInstInfo;
            if (pMTNullableArg == boxedMT)
            {
                return true;
            }
            else
            {
#if FEATURE_TYPEEQUIVALENCE
                return AreTypesEquivalent(pMTNullableArg, boxedMT);
#else
                return false;
#endif // FEATURE_TYPEEQUIVALENCE
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Unbox_Nullable_NotIsNullableForType(ref byte destPtr, MethodTable* typeMT, object obj)
        {
            // For safety's sake, also allow true nullables to be unboxed normally.
            // This should not happen normally, but we want to be robust
            if (typeMT == RuntimeHelpers.GetMethodTable(obj))
            {
                Buffer.BulkMoveWithWriteBarrier(ref destPtr, ref RuntimeHelpers.GetRawData(obj), typeMT->GetNumInstanceFieldBytes());
                return;
            }
            CastHelpers.ThrowInvalidCastException(obj, typeMT);
        }

        [DebuggerHidden]
        internal static void Unbox_Nullable(ref byte destPtr, MethodTable* typeMT, object? obj)
        {
            if (obj == null)
            {
                if (!typeMT->ContainsGCPointers)
                {
                    SpanHelpers.ClearWithoutReferences(ref destPtr, typeMT->GetNumInstanceFieldBytes());
                }
                else
                {
                    // If the type ContainsGCPointers, we can compute the size without resorting to loading the BaseSizePadding field from the EEClass
                    nuint numInstanceFieldBytes = typeMT->BaseSize - (nuint)(2 * sizeof(IntPtr));
                    // Otherwise, use the helper which is safe for that situation
                    SpanHelpers.ClearWithReferences(ref Unsafe.As<byte, IntPtr>(ref destPtr), (typeMT->BaseSize - (nuint)(2 * sizeof(IntPtr))) / (nuint)sizeof(IntPtr));
                }
            }
            else
            {
                if (!IsNullableForType(typeMT, RuntimeHelpers.GetMethodTable(obj)))
                {
                    Unbox_Nullable_NotIsNullableForType(ref destPtr, typeMT, obj);
                }
                else
                {
                    UnboxNullableValue(ref destPtr, typeMT, obj);
                }
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ref byte Unbox_Helper(MethodTable* pMT1, object obj)
        {
            // must be a value type
            Debug.Assert(pMT1->IsValueType);

            MethodTable* pMT2 = RuntimeHelpers.GetMethodTable(obj);
            if ((pMT1->IsPrimitive && pMT2->IsPrimitive &&
                pMT1->GetPrimitiveCorElementType() == pMT2->GetPrimitiveCorElementType())
#if FEATURE_TYPEEQUIVALENCE
                || AreTypesEquivalent(pMT1, pMT2)
#endif // FEATURE_TYPEEQUIVALENCE
                )
            {
                return ref RuntimeHelpers.GetRawData(obj);
            }

            CastHelpers.ThrowInvalidCastException(obj, pMT1);
            return ref Unsafe.AsRef<byte>(null);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Unbox_TypeTest_Helper(MethodTable *pMT1, MethodTable *pMT2)
        {
            if ((pMT1->IsPrimitive && pMT2->IsPrimitive &&
                pMT1->GetPrimitiveCorElementType() == pMT2->GetPrimitiveCorElementType())
#if FEATURE_TYPEEQUIVALENCE
                || AreTypesEquivalent(pMT1, pMT2)
#endif // FEATURE_TYPEEQUIVALENCE
                )
            {
                return;
            }

            CastHelpers.ThrowInvalidCastException(pMT1, pMT2);
        }

        [DebuggerHidden]
        internal static void Unbox_TypeTest(MethodTable *pMT1, MethodTable *pMT2)
        {
            if (pMT1 == pMT2)
                return;
            else
                Unbox_TypeTest_Helper(pMT1, pMT2);
        }
    }
}
