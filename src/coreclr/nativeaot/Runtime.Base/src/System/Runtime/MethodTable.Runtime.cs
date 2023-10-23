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
#if !INPLACE_RUNTIME
        internal MethodTable* GetArrayEEType()
        {
            MethodTable* pThis = (MethodTable*)Unsafe.Pointer(ref this);
            void* pGetArrayEEType = InternalCalls.RhpGetClasslibFunctionFromEEType(pThis, ClassLibFunctionId.GetSystemArrayEEType);
            return ((delegate* <MethodTable*>)pGetArrayEEType)();
        }

        internal Exception GetClasslibException(ExceptionIDs id)
        {
            if (IsParameterizedType)
            {
                return RelatedParameterType->GetClasslibException(id);
            }

            return EH.GetClasslibExceptionFromEEType(id, (MethodTable*)Unsafe.AsPointer(ref this));
        }
#endif

        internal IntPtr GetClasslibFunction(ClassLibFunctionId id)
        {
            return (IntPtr)InternalCalls.RhpGetClasslibFunctionFromEEType((MethodTable*)Unsafe.AsPointer(ref this), id);
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
