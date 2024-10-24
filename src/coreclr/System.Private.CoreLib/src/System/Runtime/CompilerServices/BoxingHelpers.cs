// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices
{
    [StackTraceHidden]
    [DebuggerStepThrough]
    internal static unsafe partial class BoxingHelpers
    {
        [DebuggerHidden]
        private static unsafe void InitValueClass(ref byte destBytes, MethodTable *pMT)
        {
            uint numInstanceFieldBytes = pMT->GetNumInstanceFieldBytes();
            if (((uint)Unsafe.AsPointer(ref destBytes) | numInstanceFieldBytes & ((uint)sizeof(void*) - 1)) != 0)
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
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool AreTypesEquivalent(MethodTable* pMTa, MethodTable* pMTb)
        {
            if (pMTa == pMTb)
            {
                return true;
            }

            if (pMTa->HasTypeEquivalence && pMTb->HasTypeEquivalence)
            {
                return false;
            }

            return RuntimeHelpers.AreTypesEquivalent(pMTa, pMTb);
        }

        [DebuggerHidden]
        private static bool IsNullableForType(MethodTable* typeMT, MethodTable* boxedMT)
        {
            if (!typeMT->IsNullable)
            {
                return false;
            }

            MethodTable *pMTNullableArg = typeMT->InstantiationArg0();
            if (pMTNullableArg == boxedMT)
            {
                return true;
            }
            else
            {
                return AreTypesEquivalent(pMTNullableArg, boxedMT);
            }
        }

        [DebuggerHidden]
        internal static void Unbox_Nullable(ref byte destPtr, MethodTable* typeMT, object? obj)
        {
            if (obj == null)
            {
                InitValueClass(ref destPtr, typeMT);
            }
            else
            {
                if (!IsNullableForType(typeMT, RuntimeHelpers.GetMethodTable(obj)))
                {
                    // For safety's sake, also allow true nullables to be unboxed normally.
                    // This should not happen normally, but we want to be robust
                    if (typeMT == RuntimeHelpers.GetMethodTable(obj))
                    {
                        Unsafe.CopyBlockUnaligned(ref destPtr, ref RuntimeHelpers.GetRawData(obj), typeMT->GetNumInstanceFieldBytes());
                        return;
                    }
                    CastHelpers.ThrowInvalidCastException(obj, typeMT);
                }

                // Set the hasValue field on the Nullable type. It MUST always be placed at the start of the object.
                Unsafe.As<byte, bool>(ref destPtr) = true;
                ref byte destValuePtr = ref typeMT->GetNullableValueFieldReferenceAndSize(ref destPtr, out uint size);
                Unsafe.CopyBlockUnaligned(ref destValuePtr, ref RuntimeHelpers.GetRawData(obj), size);
            }
        }

        internal static ref byte Unbox_Helper(MethodTable* pMT1, object obj)
        {
            // must be a value type
            Debug.Assert(pMT1->IsValueType);

            MethodTable* pMT2 = RuntimeHelpers.GetMethodTable(obj);
            if ((pMT1->IsPrimitive && pMT2->IsPrimitive &&
                pMT1->GetPrimitiveCorElementType() == pMT2->GetPrimitiveCorElementType()) ||
                AreTypesEquivalent(pMT1, pMT2))
            {
                return ref RuntimeHelpers.GetRawData(obj);
            }

            CastHelpers.ThrowInvalidCastException(obj, pMT1);
            return ref Unsafe.AsRef<byte>(null);
        }

        internal static void Unbox_TypeTest(MethodTable *pMT1, MethodTable *pMT2)
        {
            if (pMT1 == pMT2 ||
                (pMT1->IsPrimitive && pMT2->IsPrimitive &&
                pMT1->GetPrimitiveCorElementType() == pMT2->GetPrimitiveCorElementType()) ||
                AreTypesEquivalent(pMT1, pMT2))
            {
                return;
            }

            CastHelpers.ThrowInvalidCastException(pMT1, pMT2);
        }
    }
}
