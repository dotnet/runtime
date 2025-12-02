// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices
{
    [StackTraceHidden]
    [DebuggerStepThrough]
    internal static unsafe partial class CastHelpers
    {
        // In coreclr the table is allocated and written to on the native side.
        internal static int[]? s_table;

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThrowInvalidCastException")]
        private static partial void ThrowInvalidCastExceptionInternal(void* fromTypeHnd, void* toTypeHnd);

        [DoesNotReturn]
        internal static void ThrowInvalidCastException(void* fromTypeHnd, void* toTypeHnd)
        {
            ThrowInvalidCastExceptionInternal(fromTypeHnd, toTypeHnd);
            throw null!; // Provide hint to the inliner that this method does not return
        }

        [DoesNotReturn]
        internal static void ThrowInvalidCastException(object fromType, void* toTypeHnd)
        {
            ThrowInvalidCastExceptionInternal(RuntimeHelpers.GetMethodTable(fromType), toTypeHnd);
            GC.KeepAlive(fromType);
            throw null!; // Provide hint to the inliner that this method does not return
        }

        [LibraryImport(RuntimeHelpers.QCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool IsInstanceOf_NoCacheLookup(void *toTypeHnd, [MarshalAs(UnmanagedType.Bool)] bool throwCastException, ObjectHandleOnStack obj);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static object? IsInstanceOfAny_NoCacheLookup(void* toTypeHnd, object obj)
        {
            if (IsInstanceOf_NoCacheLookup(toTypeHnd, false, ObjectHandleOnStack.Create(ref obj)))
            {
                return obj;
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static object ChkCastAny_NoCacheLookup(void* toTypeHnd, object obj)
        {
            IsInstanceOf_NoCacheLookup(toTypeHnd, true, ObjectHandleOnStack.Create(ref obj));
            return obj;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void WriteBarrier(ref object? dst, object? obj);

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

#if FEATURE_TYPEEQUIVALENCE
            // this helper is not supposed to be used with type-equivalent "to" type.
            Debug.Assert(!((MethodTable*)toTypeHnd)->HasTypeEquivalence);
#endif // FEATURE_TYPEEQUIVALENCE

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

            object? obj2 = IsInstanceOfAny_NoCacheLookup(elementType, obj);
            if (obj2 == null)
            {
                ThrowArrayMismatchException();
            }

            WriteBarrier(ref element, obj2);
        }

        [DebuggerHidden]
        private static void ArrayTypeCheck(object obj, Array array)
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
        private static void ArrayTypeCheck_Helper(object obj, void* elementType)
        {
            Debug.Assert(obj != null);

            if (IsInstanceOfAny_NoCacheLookup(elementType, obj) == null)
            {
                ThrowArrayMismatchException();
            }
        }

        // Helpers for boxing
        [DebuggerHidden]
        internal static object? Box_Nullable(MethodTable* srcMT, ref byte nullableData)
        {
            Debug.Assert(srcMT->IsNullable);

            if (nullableData == 0)
                return null;

            // Allocate a new instance of the T in Nullable<T>.
            MethodTable* dstMT = srcMT->InstantiationArg0();
            ref byte srcValue = ref Unsafe.Add(ref nullableData, srcMT->NullableValueAddrOffset);

            // Delegate to non-nullable boxing implementation
            return Box(dstMT, ref srcValue);
        }

        [DebuggerHidden]
        internal static object Box(MethodTable* typeMT, ref byte unboxedData)
        {
            Debug.Assert(typeMT != null);
            Debug.Assert(typeMT->IsValueType);

            // A null can be passed for boxing of a null ref.
            _ = Unsafe.ReadUnaligned<byte>(ref unboxedData);

            object boxed = RuntimeTypeHandle.InternalAllocNoChecks(typeMT);
            if (typeMT->ContainsGCPointers)
            {
                Buffer.BulkMoveWithWriteBarrier(ref boxed.GetRawData(), ref unboxedData, typeMT->GetNumInstanceFieldBytesIfContainsGCPointers());
            }
            else
            {
                SpanHelpers.Memmove(ref boxed.GetRawData(), ref unboxedData, typeMT->GetNumInstanceFieldBytes());
            }

            return boxed;
        }

        // Helpers for Unboxing
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
        internal static bool IsNullableForType(MethodTable* typeMT, MethodTable* boxedMT)
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
            // Also allow true nullables to be unboxed normally.
            // This should not happen normally, but can happen in debugger scenarios.
            if (typeMT != RuntimeHelpers.GetMethodTable(obj))
            {
                CastHelpers.ThrowInvalidCastException(obj, typeMT);
            }
            Buffer.BulkMoveWithWriteBarrier(ref destPtr, ref RuntimeHelpers.GetRawData(obj), typeMT->GetNullableNumInstanceFieldBytes());
        }

        [DebuggerHidden]
        internal static void Unbox_Nullable(ref byte destPtr, MethodTable* typeMT, object? obj)
        {
            if (obj == null)
            {
                if (!typeMT->ContainsGCPointers)
                {
                    SpanHelpers.ClearWithoutReferences(ref destPtr, typeMT->GetNullableNumInstanceFieldBytes());
                }
                else
                {
                    SpanHelpers.ClearWithReferences(ref Unsafe.As<byte, IntPtr>(ref destPtr), typeMT->GetNumInstanceFieldBytesIfContainsGCPointers() / (nuint)sizeof(IntPtr));
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
                    Unsafe.As<byte, bool>(ref destPtr) = true;
                    ref byte dst = ref Unsafe.Add(ref destPtr, typeMT->NullableValueAddrOffset);
                    uint valueSize = typeMT->NullableValueSize;
                    ref byte src = ref RuntimeHelpers.GetRawData(obj);
                    if (typeMT->ContainsGCPointers)
                        Buffer.BulkMoveWithWriteBarrier(ref dst, ref src, valueSize);
                    else
                        SpanHelpers.Memmove(ref dst, ref src, valueSize);
                }
            }
        }

        [DebuggerHidden]
        internal static object? ReboxFromNullable(MethodTable* srcMT, object src)
        {
            ref byte nullableData = ref src.GetRawData();
            return Box_Nullable(srcMT, ref nullableData);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ref byte Unbox_Helper(MethodTable* pMT1, object obj)
        {
            // must be a value type
            Debug.Assert(pMT1->IsValueType);

            MethodTable* pMT2 = RuntimeHelpers.GetMethodTable(obj);
            if ((!pMT1->IsPrimitive || !pMT2->IsPrimitive ||
                pMT1->GetPrimitiveCorElementType() != pMT2->GetPrimitiveCorElementType())
#if FEATURE_TYPEEQUIVALENCE
                && !AreTypesEquivalent(pMT1, pMT2)
#endif // FEATURE_TYPEEQUIVALENCE
                )
            {
                CastHelpers.ThrowInvalidCastException(obj, pMT1);
            }

            return ref RuntimeHelpers.GetRawData(obj);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Unbox_TypeTest_Helper(MethodTable *pMT1, MethodTable *pMT2)
        {
            if ((!pMT1->IsPrimitive || !pMT2->IsPrimitive ||
                pMT1->GetPrimitiveCorElementType() != pMT2->GetPrimitiveCorElementType())
#if FEATURE_TYPEEQUIVALENCE
                && !AreTypesEquivalent(pMT1, pMT2)
#endif // FEATURE_TYPEEQUIVALENCE
                )
            {
                CastHelpers.ThrowInvalidCastException(pMT1, pMT2);
            }
        }

        [DebuggerHidden]
        private static void Unbox_TypeTest(MethodTable *pMT1, MethodTable *pMT2)
        {
            if (pMT1 == pMT2)
                return;
            else
                Unbox_TypeTest_Helper(pMT1, pMT2);
        }
    }
}
