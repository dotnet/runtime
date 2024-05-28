// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: ArrayNative.cpp
//

//
// This file contains the native methods that support the Array class
//


#include "common.h"
#include "arraynative.h"
#include "excep.h"
#include "field.h"
#include "invokeutil.h"

#include "arraynative.inl"

extern "C" PCODE QCALLTYPE Array_GetElementConstructorEntrypoint(QCall::TypeHandle pArrayTypeHnd)
{
    QCALL_CONTRACT;

    PCODE ctorEntrypoint = (PCODE)NULL;

    BEGIN_QCALL;

    TypeHandle th = pArrayTypeHnd.AsTypeHandle();
    MethodTable* pElemMT = th.GetArrayElementTypeHandle().AsMethodTable();
    ctorEntrypoint = pElemMT->GetDefaultConstructor()->GetMultiCallableAddrOfCode();

    pElemMT->EnsureInstanceActive();

    END_QCALL;

    return ctorEntrypoint;
}


//
// This is a GC safe variant of the memmove intrinsic. It sets the cards, and guarantees that the object references in the GC heap are
// updated atomically.
//
// The CRT version of memmove does not always guarantee that updates of aligned fields stay atomic (e.g. it is using "rep movsb" in some cases).
// Type safety guarantees and background GC scanning requires object references in GC heap to be updated atomically.
//
void memmoveGCRefs(void *dest, const void *src, size_t len)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    _ASSERTE(dest != nullptr);
    _ASSERTE(src != nullptr);

    // Make sure everything is pointer aligned
    _ASSERTE(IS_ALIGNED(dest, sizeof(SIZE_T)));
    _ASSERTE(IS_ALIGNED(src, sizeof(SIZE_T)));
    _ASSERTE(IS_ALIGNED(len, sizeof(SIZE_T)));

    _ASSERTE(CheckPointer(dest));
    _ASSERTE(CheckPointer(src));

    if (len != 0 && dest != src)
    {
        InlinedMemmoveGCRefsHelper(dest, src, len);
    }
}


// Check we're allowed to create an array with the given element type.
static void CheckElementType(TypeHandle elementType)
{
    // Check for simple types first.
    if (!elementType.IsTypeDesc())
    {
        MethodTable *pMT = elementType.AsMethodTable();

        // Check for byref-like types.
        if (pMT->IsByRefLike())
            COMPlusThrow(kNotSupportedException, W("NotSupported_ByRefLikeArray"));

        // Check for open generic types.
        if (pMT->ContainsGenericVariables())
            COMPlusThrow(kNotSupportedException, W("NotSupported_OpenType"));

        // Check for Void.
        if (elementType.GetSignatureCorElementType() == ELEMENT_TYPE_VOID)
            COMPlusThrow(kNotSupportedException, W("NotSupported_VoidArray"));
    }
    else
    {
        // ByRefs and generic type variables are never allowed.
        if (elementType.IsByRef() || elementType.IsGenericVariable())
            COMPlusThrow(kNotSupportedException, W("NotSupported_Type"));
    }
}

void QCALLTYPE Array_CreateInstance(QCall::TypeHandle pTypeHnd, INT32 rank, INT32* pLengths, INT32* pLowerBounds, BOOL createFromArrayType, QCall::ObjectHandleOnStack retArray)
{
    CONTRACTL {
        QCALL_CHECK;
        PRECONDITION(rank > 0);
        PRECONDITION(CheckPointer(pLengths));
        PRECONDITION(CheckPointer(pLowerBounds, NULL_OK));
    } CONTRACTL_END;

    BEGIN_QCALL;

    TypeHandle typeHnd = pTypeHnd.AsTypeHandle();

    if (createFromArrayType)
    {
        _ASSERTE((INT32)typeHnd.GetRank() == rank);
        _ASSERTE(typeHnd.IsArray());

        if (typeHnd.GetArrayElementTypeHandle().ContainsGenericVariables())
            COMPlusThrow(kNotSupportedException, W("NotSupported_OpenType"));

        if (!typeHnd.AsMethodTable()->IsMultiDimArray())
        {
            _ASSERTE(pLowerBounds == NULL || pLowerBounds[0] == 0);

            GCX_COOP();
            retArray.Set(AllocateSzArray(typeHnd, pLengths[0]));
            goto Done;
        }
    }
    else
    {
        CheckElementType(typeHnd);

        // Is it ELEMENT_TYPE_SZARRAY array?
        if (rank == 1 && (pLowerBounds == NULL || pLowerBounds[0] == 0))
        {
            CorElementType corType = typeHnd.GetSignatureCorElementType();

            // Shortcut for common cases
            if (CorTypeInfo::IsPrimitiveType(corType))
            {
                GCX_COOP();
                retArray.Set(AllocatePrimitiveArray(corType, pLengths[0]));
                goto Done;
            }

            typeHnd = ClassLoader::LoadArrayTypeThrowing(typeHnd);

            {
                GCX_COOP();
                retArray.Set(AllocateSzArray(typeHnd, pLengths[0]));
                goto Done;
            }
        }

        // Find the Array class...
        typeHnd = ClassLoader::LoadArrayTypeThrowing(typeHnd, ELEMENT_TYPE_ARRAY, rank);
    }

    {
        _ASSERTE(rank <= MAX_RANK); // Ensures that the stack buffer size allocations below won't overflow

        DWORD boundsSize = 0;
        INT32* bounds;
        if (pLowerBounds != NULL)
        {
            boundsSize = 2 * rank;
            bounds = (INT32*) _alloca(boundsSize * sizeof(INT32));

            for (int i=0;i<rank;i++) {
                bounds[2*i] = pLowerBounds[i];
                bounds[2*i+1] = pLengths[i];
            }
        }
        else
        {
            boundsSize = rank;
            bounds = (INT32*) _alloca(boundsSize * sizeof(INT32));

            // We need to create a private copy of pLengths to avoid holes caused
            // by caller mutating the array
            for (int i=0; i < rank; i++)
                bounds[i] = pLengths[i];
        }

        {
            GCX_COOP();
            retArray.Set(AllocateArrayEx(typeHnd, bounds, boundsSize));
        }
    }

Done: ;
    END_QCALL;
}

// This method will initialize an array from a TypeHandle to a field.

FCIMPL2_IV(void, ArrayNative::InitializeArray, ArrayBase* pArrayRef, FCALLRuntimeFieldHandle structField)
{
    FCALL_CONTRACT;

    BASEARRAYREF arr = BASEARRAYREF(pArrayRef);
    REFLECTFIELDREF refField = (REFLECTFIELDREF)ObjectToOBJECTREF(FCALL_RFH_TO_REFLECTFIELD(structField));
    HELPER_METHOD_FRAME_BEGIN_2(arr, refField);

    if ((arr == 0) || (refField == NULL))
        COMPlusThrow(kArgumentNullException);

    FieldDesc* pField = (FieldDesc*) refField->GetField();

    if (!pField->IsRVA())
        COMPlusThrow(kArgumentException);

    // Note that we do not check that the field is actually in the PE file that is initializing
    // the array. Basically the data being published is can be accessed by anyone with the proper
    // permissions (C# marks these as assembly visibility, and thus are protected from outside
    // snooping)

    if (!CorTypeInfo::IsPrimitiveType(arr->GetArrayElementType()) && !arr->GetArrayElementTypeHandle().IsEnum())
        COMPlusThrow(kArgumentException);

    SIZE_T dwCompSize = arr->GetComponentSize();
    SIZE_T dwElemCnt = arr->GetNumComponents();
    SIZE_T dwTotalSize = dwCompSize * dwElemCnt;

    DWORD size = pField->LoadSize();

    // make certain you don't go off the end of the rva static
    if (dwTotalSize > size)
        COMPlusThrow(kArgumentException);

    void *src = pField->GetStaticAddressHandle(NULL);
    void *dest = arr->GetDataPtr();

#if BIGENDIAN
    DWORD i;
    switch (dwCompSize) {
    case 1:
        memcpyNoGCRefs(dest, src, dwElemCnt);
        break;
    case 2:
        for (i = 0; i < dwElemCnt; i++)
            *((UINT16*)dest + i) = GET_UNALIGNED_VAL16((UINT16*)src + i);
        break;
    case 4:
        for (i = 0; i < dwElemCnt; i++)
            *((UINT32*)dest + i) = GET_UNALIGNED_VAL32((UINT32*)src + i);
        break;
    case 8:
        for (i = 0; i < dwElemCnt; i++)
            *((UINT64*)dest + i) = GET_UNALIGNED_VAL64((UINT64*)src + i);
        break;
    default:
        // should not reach here.
        UNREACHABLE_MSG("Incorrect primitive type size!");
        break;
    }
#else
    memcpyNoGCRefs(dest, src, dwTotalSize);
#endif

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

FCIMPL3_VVI(void*, ArrayNative::GetSpanDataFrom, FCALLRuntimeFieldHandle structField, FCALLRuntimeTypeHandle targetTypeUnsafe, INT32* count)
{
    FCALL_CONTRACT;
    struct
    {
        REFLECTFIELDREF refField;
        REFLECTCLASSBASEREF refClass;
    } gc;
    gc.refField = (REFLECTFIELDREF)ObjectToOBJECTREF(FCALL_RFH_TO_REFLECTFIELD(structField));
    gc.refClass = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(FCALL_RTH_TO_REFLECTCLASS(targetTypeUnsafe));
    void* data = NULL;
    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    FieldDesc* pField = (FieldDesc*)gc.refField->GetField();

    if (!pField->IsRVA())
        COMPlusThrow(kArgumentException);

    TypeHandle targetTypeHandle = gc.refClass->GetType();
    if (!CorTypeInfo::IsPrimitiveType(targetTypeHandle.GetSignatureCorElementType()) && !targetTypeHandle.IsEnum())
        COMPlusThrow(kArgumentException);

    DWORD totalSize = pField->LoadSize();
    DWORD targetTypeSize = targetTypeHandle.GetSize();

    data = pField->GetStaticAddressHandle(NULL);
    _ASSERTE(data != NULL);
    _ASSERTE(count != NULL);

    if (AlignUp((UINT_PTR)data, targetTypeSize) != (UINT_PTR)data)
        COMPlusThrow(kArgumentException);

    *count = (INT32)totalSize / targetTypeSize;

#if BIGENDIAN
    COMPlusThrow(kPlatformNotSupportedException);
#endif

   HELPER_METHOD_FRAME_END();
   return data;
}
FCIMPLEND
