// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;

namespace Internal.Runtime
{
    // Extensions to MethodTable that are specific to the use in Runtime.Base.
    internal unsafe partial struct MethodTable
    {
#pragma warning disable CA1822
        internal MethodTable* GetArrayEEType()
        {
#if INPLACE_RUNTIME
            return MethodTable.Of<Array>();
#else
            MethodTable* pThis = (MethodTable*)Unsafe.Pointer(ref this);
            void* pGetArrayEEType = InternalCalls.RhpGetClasslibFunctionFromEEType(pThis, ClassLibFunctionId.GetSystemArrayEEType);
            return ((delegate* <MethodTable*>)pGetArrayEEType)();
#endif
        }

        internal Exception GetClasslibException(ExceptionIDs id)
        {
#if INPLACE_RUNTIME
            return RuntimeExceptionHelpers.GetRuntimeException(id);
#else
            if (IsParameterizedType)
            {
                return RelatedParameterType->GetClasslibException(id);
            }

            return EH.GetClasslibExceptionFromEEType(id, (MethodTable*)Unsafe.AsPointer(ref this));
#endif
        }
#pragma warning restore CA1822

        internal IntPtr GetClasslibFunction(ClassLibFunctionId id)
        {
            return (IntPtr)InternalCalls.RhpGetClasslibFunctionFromEEType((MethodTable*)Unsafe.AsPointer(ref this), id);
        }

        internal static bool AreSameType(MethodTable* mt1, MethodTable* mt2)
        {
            return mt1 == mt2;
        }
    }

    internal static class WellKnownEETypes
    {
        // Returns true if the passed in MethodTable is the MethodTable for System.Object
        // This is recognized by the fact that System.Object and interfaces are the only ones without a base type
        internal static unsafe bool IsSystemObject(MethodTable* pEEType)
        {
            if (pEEType->IsArray)
                return false;
            return (pEEType->NonArrayBaseType == null) && !pEEType->IsInterface;
        }

        // Returns true if the passed in MethodTable is the MethodTable for System.Array or System.Object.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe bool IsValidArrayBaseType(MethodTable* pEEType)
        {
            EETypeElementType elementType = pEEType->ElementType;
            return elementType == EETypeElementType.SystemArray
                || (elementType == EETypeElementType.Class && pEEType->NonArrayBaseType == null);
        }
    }
}
