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

// Returns a bool to indicate if the array is of primitive types or not.
FCIMPL1(INT32, ArrayNative::GetCorElementTypeOfElementType, ArrayBase* arrayUNSAFE)
{
    FCALL_CONTRACT;

    _ASSERTE(arrayUNSAFE != NULL);

    return arrayUNSAFE->GetArrayElementTypeHandle().GetVerifierCorElementType();
}
FCIMPLEND

extern "C" PCODE QCALLTYPE Array_GetElementConstructorEntrypoint(QCall::TypeHandle pArrayTypeHnd)
{
    QCALL_CONTRACT;

    PCODE ctorEntrypoint = NULL;

    BEGIN_QCALL;

    TypeHandle th = pArrayTypeHnd.AsTypeHandle();
    MethodTable* pElemMT = th.GetArrayElementTypeHandle().AsMethodTable();
    ctorEntrypoint = pElemMT->GetDefaultConstructor()->GetMultiCallableAddrOfCode();

    pElemMT->EnsureInstanceActive();

    END_QCALL;

    return ctorEntrypoint;
}

    // Returns whether you can directly copy an array of srcType into destType.
FCIMPL2(FC_BOOL_RET, ArrayNative::IsSimpleCopy, ArrayBase* pSrc, ArrayBase* pDst)
{
    FCALL_CONTRACT;

    _ASSERTE(pSrc != NULL);
    _ASSERTE(pDst != NULL);

    // This case is expected to be handled by the fast path
    _ASSERTE(pSrc->GetMethodTable() != pDst->GetMethodTable());

    TypeHandle srcTH = pSrc->GetMethodTable()->GetArrayElementTypeHandle();
    TypeHandle destTH = pDst->GetMethodTable()->GetArrayElementTypeHandle();
    if (srcTH == destTH) // This check kicks for different array kind or dimensions
        FC_RETURN_BOOL(true);

    if (srcTH.IsValueType())
    {
        // Value class boxing
        if (!destTH.IsValueType())
            FC_RETURN_BOOL(false);

        const CorElementType srcElType = srcTH.GetVerifierCorElementType();
        const CorElementType destElType = destTH.GetVerifierCorElementType();
        _ASSERTE(srcElType < ELEMENT_TYPE_MAX);
        _ASSERTE(destElType < ELEMENT_TYPE_MAX);

        // Copying primitives from one type to another
        if (CorTypeInfo::IsPrimitiveType_NoThrow(srcElType) && CorTypeInfo::IsPrimitiveType_NoThrow(destElType))
        {
            if (GetNormalizedIntegralArrayElementType(srcElType) == GetNormalizedIntegralArrayElementType(destElType))
                FC_RETURN_BOOL(true);
        }
    }
    else
    {
        // Value class unboxing
        if (destTH.IsValueType())
            FC_RETURN_BOOL(false);
    }

    TypeHandle::CastResult r = srcTH.CanCastToCached(destTH);
    if (r != TypeHandle::MaybeCast)
    {
        FC_RETURN_BOOL(r);
    }

    struct
    {
        OBJECTREF   src;
        OBJECTREF   dst;
    } gc;

    gc.src = ObjectToOBJECTREF(pSrc);
    gc.dst = ObjectToOBJECTREF(pDst);

    BOOL iRetVal = FALSE;

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);
    iRetVal = srcTH.CanCastTo(destTH);
    HELPER_METHOD_FRAME_END();

    FC_RETURN_BOOL(iRetVal);
}
FCIMPLEND


// Returns an enum saying whether you can copy an array of srcType into destType.
ArrayNative::AssignArrayEnum ArrayNative::CanAssignArrayType(const BASEARRAYREF pSrc, const BASEARRAYREF pDest)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(pSrc != NULL);
        PRECONDITION(pDest != NULL);
    }
    CONTRACTL_END;

    // This first bit is a minor optimization: e.g. when copying byte[] to byte[]
    // we do not need to call GetArrayElementTypeHandle().
    MethodTable *pSrcMT = pSrc->GetMethodTable();
    MethodTable *pDestMT = pDest->GetMethodTable();
    _ASSERTE(pSrcMT != pDestMT); // Handled by fast path

    TypeHandle srcTH = pSrcMT->GetArrayElementTypeHandle();
    TypeHandle destTH = pDestMT->GetArrayElementTypeHandle();
    _ASSERTE(srcTH != destTH);  // Handled by fast path

    // Value class boxing
    if (srcTH.IsValueType() && !destTH.IsValueType())
    {
        if (srcTH.CanCastTo(destTH))
            return AssignBoxValueClassOrPrimitive;
        else
            return AssignWrongType;
    }

    // Value class unboxing.
    if (!srcTH.IsValueType() && destTH.IsValueType())
    {
        if (srcTH.CanCastTo(destTH))
            return AssignUnboxValueClass;
        else if (destTH.CanCastTo(srcTH))   // V extends IV. Copying from IV to V, or Object to V.
            return AssignUnboxValueClass;
        else
            return AssignWrongType;
    }

    const CorElementType srcElType = srcTH.GetVerifierCorElementType();
    const CorElementType destElType = destTH.GetVerifierCorElementType();
    _ASSERTE(srcElType < ELEMENT_TYPE_MAX);
    _ASSERTE(destElType < ELEMENT_TYPE_MAX);

    // Copying primitives from one type to another
    if (CorTypeInfo::IsPrimitiveType_NoThrow(srcElType) && CorTypeInfo::IsPrimitiveType_NoThrow(destElType))
    {
        _ASSERTE(srcElType != destElType); // Handled by fast path
        if (InvokeUtil::CanPrimitiveWiden(destElType, srcElType))
            return AssignPrimitiveWiden;
        else
            return AssignWrongType;
    }

    // dest Object extends src
    _ASSERTE(!srcTH.CanCastTo(destTH)); // Handled by fast path

    // src Object extends dest
    if (destTH.CanCastTo(srcTH))
        return AssignMustCast;

    // class X extends/implements src and implements dest.
    if (destTH.IsInterface() && srcElType != ELEMENT_TYPE_VALUETYPE)
        return AssignMustCast;

    // class X implements src and extends/implements dest
    if (srcTH.IsInterface() && destElType != ELEMENT_TYPE_VALUETYPE)
        return AssignMustCast;

    return AssignWrongType;
}


// Casts and assigns each element of src array to the dest array type.
void ArrayNative::CastCheckEachElement(const BASEARRAYREF pSrcUnsafe, const unsigned int srcIndex, BASEARRAYREF pDestUnsafe, unsigned int destIndex, const unsigned int len)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(pSrcUnsafe != NULL);
        PRECONDITION(srcIndex >= 0);
        PRECONDITION(pDestUnsafe != NULL);
        PRECONDITION(len > 0);
    }
    CONTRACTL_END;

    // pSrc is either a PTRARRAYREF or a multidimensional array.
    TypeHandle destTH = pDestUnsafe->GetArrayElementTypeHandle();

    struct _gc
    {
        OBJECTREF obj;
        BASEARRAYREF pDest;
        BASEARRAYREF pSrc;
    } gc;

    gc.obj = NULL;
    gc.pDest = pDestUnsafe;
    gc.pSrc = pSrcUnsafe;

    GCPROTECT_BEGIN(gc);

    for(unsigned int i=srcIndex; i<srcIndex + len; ++i)
    {
        gc.obj = ObjectToOBJECTREF(*((Object**) gc.pSrc->GetDataPtr() + i));

        // Now that we have grabbed obj, we are no longer subject to races from another
        // mutator thread.
        if (gc.obj != NULL && !ObjIsInstanceOf(OBJECTREFToObject(gc.obj), destTH))
            COMPlusThrow(kInvalidCastException, W("InvalidCast_DownCastArrayElement"));

        OBJECTREF * destData = (OBJECTREF*)(gc.pDest->GetDataPtr()) + i - srcIndex + destIndex;
        SetObjectReference(destData, gc.obj);
    }

    GCPROTECT_END();

    return;
}


// Will box each element in an array of value classes or primitives into an array of Objects.
void ArrayNative::BoxEachElement(BASEARRAYREF pSrc, unsigned int srcIndex, BASEARRAYREF pDest, unsigned int destIndex, unsigned int length)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(pSrc != NULL);
        PRECONDITION(srcIndex >= 0);
        PRECONDITION(pDest != NULL);
        PRECONDITION(length > 0);
    }
    CONTRACTL_END;

    // pDest is either a PTRARRAYREF or a multidimensional array.
    _ASSERTE(pSrc!=NULL && srcIndex>=0 && pDest!=NULL && destIndex>=0 && length>=0);
    TypeHandle srcTH = pSrc->GetArrayElementTypeHandle();
#ifdef _DEBUG
    TypeHandle destTH = pDest->GetArrayElementTypeHandle();
#endif
    _ASSERTE(srcTH.GetSignatureCorElementType() == ELEMENT_TYPE_CLASS || srcTH.GetSignatureCorElementType() == ELEMENT_TYPE_VALUETYPE || CorTypeInfo::IsPrimitiveType(pSrc->GetArrayElementType()));
    _ASSERTE(!destTH.GetMethodTable()->IsValueType());

    // Get method table of type we're copying from - we need to allocate objects of that type.
    MethodTable * pSrcMT = srcTH.AsMethodTable();
    PREFIX_ASSUME(pSrcMT != NULL);

    if (!pSrcMT->IsClassInited())
    {
        BASEARRAYREF pSrcTmp = pSrc;
        BASEARRAYREF pDestTmp = pDest;
        GCPROTECT_BEGIN (pSrcTmp);
        GCPROTECT_BEGIN (pDestTmp);
        pSrcMT->CheckRunClassInitThrowing();
        pSrc = pSrcTmp;
        pDest = pDestTmp;
        GCPROTECT_END ();
        GCPROTECT_END ();
    }

    const unsigned int srcSize = pSrcMT->GetNumInstanceFieldBytes();
    unsigned int srcArrayOffset = srcIndex * srcSize;

    struct _gc
    {
        BASEARRAYREF src;
        BASEARRAYREF dest;
        OBJECTREF obj;
    }  gc;

    gc.src = pSrc;
    gc.dest = pDest;
    gc.obj = NULL;

    void* srcPtr = 0;
    GCPROTECT_BEGIN(gc);
    GCPROTECT_BEGININTERIOR(srcPtr);
    for (unsigned int i=destIndex; i < destIndex+length; i++, srcArrayOffset += srcSize)
    {
        srcPtr = (BYTE*)gc.src->GetDataPtr() + srcArrayOffset;
        gc.obj = pSrcMT->FastBox(&srcPtr);

        OBJECTREF * destData = (OBJECTREF*)((gc.dest)->GetDataPtr()) + i;
        SetObjectReference(destData, gc.obj);
    }
    GCPROTECT_END();
    GCPROTECT_END();
}


// Unboxes from an Object[] into a value class or primitive array.
void ArrayNative::UnBoxEachElement(BASEARRAYREF pSrc, unsigned int srcIndex, BASEARRAYREF pDest, unsigned int destIndex, unsigned int length)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(pSrc != NULL);
        PRECONDITION(srcIndex >= 0);
        PRECONDITION(pDest != NULL);
        PRECONDITION(destIndex >= 0);
        PRECONDITION(length > 0);
    }
    CONTRACTL_END;

#ifdef _DEBUG
    TypeHandle srcTH = pSrc->GetArrayElementTypeHandle();
#endif
    TypeHandle destTH = pDest->GetArrayElementTypeHandle();
    _ASSERTE(destTH.GetSignatureCorElementType() == ELEMENT_TYPE_CLASS || destTH.GetSignatureCorElementType() == ELEMENT_TYPE_VALUETYPE || CorTypeInfo::IsPrimitiveType(pDest->GetArrayElementType()));
    _ASSERTE(!srcTH.GetMethodTable()->IsValueType());

    MethodTable * pDestMT = destTH.AsMethodTable();
    PREFIX_ASSUME(pDestMT != NULL);

    SIZE_T destSize = pDest->GetComponentSize();
    BYTE* srcData = (BYTE*) pSrc->GetDataPtr() + srcIndex * sizeof(OBJECTREF);
    BYTE* data = (BYTE*) pDest->GetDataPtr() + destIndex * destSize;

    for(; length>0; length--, srcData += sizeof(OBJECTREF), data += destSize)
    {
        OBJECTREF obj = ObjectToOBJECTREF(*(Object**)srcData);

        // Now that we have retrieved the element, we are no longer subject to race
        // conditions from another array mutator.

        if (!pDestMT->UnBoxInto(data, obj))
            goto fail;
    }
    return;

fail:
    COMPlusThrow(kInvalidCastException, W("InvalidCast_DownCastArrayElement"));
}


// Widen primitive types to another primitive type.
void ArrayNative::PrimitiveWiden(BASEARRAYREF pSrc, unsigned int srcIndex, BASEARRAYREF pDest, unsigned int destIndex, unsigned int length)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(pSrc != NULL);
        PRECONDITION(srcIndex >= 0);
        PRECONDITION(pDest != NULL);
        PRECONDITION(destIndex >= 0);
        PRECONDITION(length > 0);
    }
    CONTRACTL_END;

    // Get appropriate sizes, which requires method tables.
    TypeHandle srcTH = pSrc->GetArrayElementTypeHandle();
    TypeHandle destTH = pDest->GetArrayElementTypeHandle();

    const CorElementType srcElType = srcTH.GetVerifierCorElementType();
    const CorElementType destElType = destTH.GetVerifierCorElementType();
    const unsigned int srcSize = GetSizeForCorElementType(srcElType);
    const unsigned int destSize = GetSizeForCorElementType(destElType);

    BYTE* srcData = (BYTE*) pSrc->GetDataPtr() + srcIndex * srcSize;
    BYTE* data = (BYTE*) pDest->GetDataPtr() + destIndex * destSize;

    _ASSERTE(srcElType != destElType);  // We shouldn't be here if these are the same type.
    _ASSERTE(CorTypeInfo::IsPrimitiveType_NoThrow(srcElType) && CorTypeInfo::IsPrimitiveType_NoThrow(destElType));

    for(; length>0; length--, srcData += srcSize, data += destSize)
    {
        // We pretty much have to do some fancy datatype mangling every time here, for
        // converting w/ sign extension and floating point conversions.
        switch (srcElType)
        {
            case ELEMENT_TYPE_U1:
                switch (destElType)
                {
                    case ELEMENT_TYPE_R4:
                        *(float*)data = *(UINT8*)srcData;
                        break;

                    case ELEMENT_TYPE_R8:
                        *(double*)data = *(UINT8*)srcData;
                        break;
#ifndef BIGENDIAN
                    default:
                        *(UINT8*)data = *(UINT8*)srcData;
                        memset(data+1, 0, destSize - 1);
                        break;
#else // BIGENDIAN
                    case ELEMENT_TYPE_CHAR:
                    case ELEMENT_TYPE_I2:
                    case ELEMENT_TYPE_U2:
                        *(INT16*)data = *(UINT8*)srcData;
                        break;

                    case ELEMENT_TYPE_I4:
                    case ELEMENT_TYPE_U4:
                        *(INT32*)data = *(UINT8*)srcData;
                        break;

                    case ELEMENT_TYPE_I8:
                    case ELEMENT_TYPE_U8:
                        *(INT64*)data = *(UINT8*)srcData;
                        break;

                    default:
                        _ASSERTE(!"Array.Copy from U1 to another type hit unsupported widening conversion");
#endif // BIGENDIAN
                }
                break;


            case ELEMENT_TYPE_I1:
                switch (destElType)
                {
                    case ELEMENT_TYPE_I2:
                        *(INT16*)data = *(INT8*)srcData;
                        break;

                    case ELEMENT_TYPE_I4:
                        *(INT32*)data = *(INT8*)srcData;
                        break;

                    case ELEMENT_TYPE_I8:
                        *(INT64*)data = *(INT8*)srcData;
                        break;

                    case ELEMENT_TYPE_R4:
                        *(float*)data = *(INT8*)srcData;
                        break;

                    case ELEMENT_TYPE_R8:
                        *(double*)data = *(INT8*)srcData;
                        break;

                    default:
                        _ASSERTE(!"Array.Copy from I1 to another type hit unsupported widening conversion");
                }
                break;


            case ELEMENT_TYPE_U2:
            case ELEMENT_TYPE_CHAR:
                switch (destElType)
                {
                    case ELEMENT_TYPE_R4:
                        *(float*)data = *(UINT16*)srcData;
                        break;

                    case ELEMENT_TYPE_R8:
                        *(double*)data = *(UINT16*)srcData;
                        break;
#ifndef BIGENDIAN
                    default:
                        *(UINT16*)data = *(UINT16*)srcData;
                        memset(data+2, 0, destSize - 2);
                        break;
#else // BIGENDIAN
                    case ELEMENT_TYPE_U2:
                    case ELEMENT_TYPE_CHAR:
                        *(UINT16*)data = *(UINT16*)srcData;
                        break;

                    case ELEMENT_TYPE_I4:
                    case ELEMENT_TYPE_U4:
                        *(UINT32*)data = *(UINT16*)srcData;
                        break;

                    case ELEMENT_TYPE_I8:
                    case ELEMENT_TYPE_U8:
                        *(UINT64*)data = *(UINT16*)srcData;
                        break;

                    default:
                        _ASSERTE(!"Array.Copy from U1 to another type hit unsupported widening conversion");
#endif // BIGENDIAN
                }
                break;


            case ELEMENT_TYPE_I2:
                switch (destElType)
                {
                    case ELEMENT_TYPE_I4:
                        *(INT32*)data = *(INT16*)srcData;
                        break;

                    case ELEMENT_TYPE_I8:
                        *(INT64*)data = *(INT16*)srcData;
                        break;

                    case ELEMENT_TYPE_R4:
                        *(float*)data = *(INT16*)srcData;
                        break;

                    case ELEMENT_TYPE_R8:
                        *(double*)data = *(INT16*)srcData;
                        break;

                    default:
                        _ASSERTE(!"Array.Copy from I2 to another type hit unsupported widening conversion");
                }
                break;


            case ELEMENT_TYPE_I4:
                switch (destElType)
                {
                    case ELEMENT_TYPE_I8:
                        *(INT64*)data = *(INT32*)srcData;
                        break;

                    case ELEMENT_TYPE_R4:
                        *(float*)data = (float)*(INT32*)srcData;
                        break;

                    case ELEMENT_TYPE_R8:
                        *(double*)data = *(INT32*)srcData;
                        break;

                    default:
                        _ASSERTE(!"Array.Copy from I4 to another type hit unsupported widening conversion");
                }
                break;


            case ELEMENT_TYPE_U4:
                switch (destElType)
                {
                    case ELEMENT_TYPE_I8:
                    case ELEMENT_TYPE_U8:
                        *(INT64*)data = *(UINT32*)srcData;
                        break;

                    case ELEMENT_TYPE_R4:
                        *(float*)data = (float)*(UINT32*)srcData;
                        break;

                    case ELEMENT_TYPE_R8:
                        *(double*)data = *(UINT32*)srcData;
                        break;

                    default:
                        _ASSERTE(!"Array.Copy from U4 to another type hit unsupported widening conversion");
                }
                break;


            case ELEMENT_TYPE_I8:
                if (destElType == ELEMENT_TYPE_R4)
                {
                    *(float*) data = (float) *(INT64*)srcData;
                }
                else
                {
                    _ASSERTE(destElType==ELEMENT_TYPE_R8);
                    *(double*) data = (double) *(INT64*)srcData;
                }
                break;


            case ELEMENT_TYPE_U8:
                if (destElType == ELEMENT_TYPE_R4)
                {
                    //*(float*) data = (float) *(UINT64*)srcData;
                    INT64 srcVal = *(INT64*)srcData;
                    float f = (float) srcVal;
                    if (srcVal < 0)
                        f += 4294967296.0f * 4294967296.0f; // This is 2^64

                    *(float*) data = f;
                }
                else
                {
                    _ASSERTE(destElType==ELEMENT_TYPE_R8);
                    //*(double*) data = (double) *(UINT64*)srcData;
                    INT64 srcVal = *(INT64*)srcData;
                    double d = (double) srcVal;
                    if (srcVal < 0)
                        d += 4294967296.0 * 4294967296.0;   // This is 2^64

                    *(double*) data = d;
                }
                break;


            case ELEMENT_TYPE_R4:
                *(double*) data = *(float*)srcData;
                break;

            default:
                _ASSERTE(!"Fell through outer switch in PrimitiveWiden!  Unknown primitive type for source array!");
        }
    }
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

FCIMPL5(void, ArrayNative::CopySlow, ArrayBase* pSrc, INT32 iSrcIndex, ArrayBase* pDst, INT32 iDstIndex, INT32 iLength)
{
    FCALL_CONTRACT;

    struct _gc
    {
        BASEARRAYREF pSrc;
        BASEARRAYREF pDst;
    } gc;

    gc.pSrc = (BASEARRAYREF)pSrc;
    gc.pDst = (BASEARRAYREF)pDst;

    // cannot pass null for source or destination
    _ASSERTE(gc.pSrc != NULL && gc.pDst != NULL);

    // source and destination must be arrays
    _ASSERTE(gc.pSrc->GetMethodTable()->IsArray());
    _ASSERTE(gc.pDst->GetMethodTable()->IsArray());

    _ASSERTE(gc.pSrc->GetRank() == gc.pDst->GetRank());

    // array bounds checking
    _ASSERTE(iLength >= 0);
    _ASSERTE(iSrcIndex >= 0);
    _ASSERTE(iDstIndex >= 0);
    _ASSERTE((DWORD)(iSrcIndex + iLength) <= gc.pSrc->GetNumComponents());
    _ASSERTE((DWORD)(iDstIndex + iLength) <= gc.pDst->GetNumComponents());

    HELPER_METHOD_FRAME_BEGIN_PROTECT(gc);

    int r = CanAssignArrayType(gc.pSrc, gc.pDst);

    if (r == AssignWrongType)
        COMPlusThrow(kArrayTypeMismatchException, W("ArrayTypeMismatch_CantAssignType"));

    if (iLength > 0)
    {
        switch (r)
        {
            case AssignUnboxValueClass:
                UnBoxEachElement(gc.pSrc, iSrcIndex, gc.pDst, iDstIndex, iLength);
                break;

            case AssignBoxValueClassOrPrimitive:
                BoxEachElement(gc.pSrc, iSrcIndex, gc.pDst, iDstIndex, iLength);
                break;

            case AssignMustCast:
                CastCheckEachElement(gc.pSrc, iSrcIndex, gc.pDst, iDstIndex, iLength);
                break;

            case AssignPrimitiveWiden:
                PrimitiveWiden(gc.pSrc, iSrcIndex, gc.pDst, iDstIndex, iLength);
                break;

            default:
                _ASSERTE(!"Fell through switch in Array.Copy!");
        }
    }

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND


// Check we're allowed to create an array with the given element type.
void ArrayNative::CheckElementType(TypeHandle elementType)
{
    // Checks apply recursively for arrays of arrays etc.
    while (elementType.IsArray())
    {
        elementType = elementType.GetArrayElementTypeHandle();
    }

    // Check for simple types first.
    if (!elementType.IsTypeDesc())
    {
        MethodTable *pMT = elementType.AsMethodTable();

        // Check for byref-like types.
        if (pMT->IsByRefLike())
            COMPlusThrow(kNotSupportedException, W("NotSupported_ByRefLikeArray"));

        // Check for open generic types.
        if (pMT->IsGenericTypeDefinition() || pMT->ContainsGenericVariables())
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

FCIMPL4(Object*, ArrayNative::CreateInstance, ReflectClassBaseObject* pElementTypeUNSAFE, INT32 rank, INT32* pLengths, INT32* pLowerBounds)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(rank > 0);
        PRECONDITION(CheckPointer(pLengths));
        PRECONDITION(CheckPointer(pLowerBounds, NULL_OK));
    } CONTRACTL_END;

    OBJECTREF pRet = NULL;

    REFLECTCLASSBASEREF pElementType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pElementTypeUNSAFE);

    // pLengths and pLowerBounds are pinned buffers. No need to protect them.
    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(pElementType);

    TypeHandle elementType(pElementType->GetType());

    CheckElementType(elementType);

    CorElementType CorType = elementType.GetSignatureCorElementType();

    CorElementType kind = ELEMENT_TYPE_ARRAY;

    // Is it ELEMENT_TYPE_SZARRAY array?
    if (rank == 1 && (pLowerBounds == NULL || pLowerBounds[0] == 0)
#ifdef FEATURE_64BIT_ALIGNMENT
        // On platforms where 64-bit types require 64-bit alignment and don't obtain it naturally force us
        // through the slow path where this will be handled.
        && (CorType != ELEMENT_TYPE_I8)
        && (CorType != ELEMENT_TYPE_U8)
        && (CorType != ELEMENT_TYPE_R8)
#endif
        )
    {
        // Shortcut for common cases
        if (CorTypeInfo::IsPrimitiveType(CorType))
        {
            pRet = AllocatePrimitiveArray(CorType,pLengths[0]);
            goto Done;
        }
        else
        if (CorTypeInfo::IsObjRef(CorType))
        {
            pRet = AllocateObjectArray(pLengths[0],elementType);
            goto Done;
        }

        kind = ELEMENT_TYPE_SZARRAY;
        pLowerBounds = NULL;
    }

    {
        // Find the Array class...
        TypeHandle typeHnd = ClassLoader::LoadArrayTypeThrowing(elementType, kind, rank);

        _ASSERTE(rank < MAX_RANK); // Ensures that the stack buffer size allocations below won't overlow

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

        pRet = AllocateArrayEx(typeHnd, bounds, boundsSize);
    }

Done: ;
    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(pRet);
}
FCIMPLEND

FCIMPL3(void, ArrayNative::SetValue, ArrayBase* refThisUNSAFE, Object* objUNSAFE, INT_PTR flattenedIndex)
{
    FCALL_CONTRACT;

    BASEARRAYREF refThis(refThisUNSAFE);
    OBJECTREF obj(objUNSAFE);

    TypeHandle arrayElementType = refThis->GetArrayElementTypeHandle();

    // Legacy behavior (this handles pointers and function pointers)
    if (arrayElementType.IsTypeDesc())
    {
        FCThrowResVoid(kNotSupportedException, W("NotSupported_Type"));
    }

    _ASSERTE((SIZE_T)flattenedIndex < refThis->GetNumComponents());

    MethodTable* pElementTypeMT = arrayElementType.GetMethodTable();
    PREFIX_ASSUME(NULL != pElementTypeMT);

    void* pData = refThis->GetDataPtr() + flattenedIndex * refThis->GetComponentSize();

    if (obj == NULL)
    {
        // Null is the universal zero...
        if (pElementTypeMT->IsValueType())
            InitValueClass(pData,pElementTypeMT);
        else
            ClearObjectReference((OBJECTREF*)pData);
    }
    else
    if (arrayElementType == TypeHandle(g_pObjectClass))
    {
        // Everything is compatible with Object
        SetObjectReference((OBJECTREF*)pData,(OBJECTREF)obj);
    }
    else
    if (!pElementTypeMT->IsValueType())
    {
        if (ObjIsInstanceOfCached(OBJECTREFToObject(obj), arrayElementType) != TypeHandle::CanCast)
        {
            HELPER_METHOD_FRAME_BEGIN_2(refThis, obj);

            if (!ObjIsInstanceOf(OBJECTREFToObject(obj), arrayElementType))
                COMPlusThrow(kInvalidCastException,W("InvalidCast_StoreArrayElement"));

            HELPER_METHOD_FRAME_END();

            // Refresh pData in case GC moved objects around
            pData = refThis->GetDataPtr() + flattenedIndex * refThis->GetComponentSize();
        }

        SetObjectReference((OBJECTREF*)pData,obj);
    }
    else
    {
        // value class or primitive type

        if (!pElementTypeMT->UnBoxInto(pData, obj))
        {
            HELPER_METHOD_FRAME_BEGIN_2(refThis, obj);

            ARG_SLOT value = 0;

            // Allow enum -> primitive conversion, disallow primitive -> enum conversion
            TypeHandle thSrc = obj->GetTypeHandle();
            CorElementType srcType = thSrc.GetVerifierCorElementType();
            CorElementType targetType = arrayElementType.GetSignatureCorElementType();

            if (!InvokeUtil::IsPrimitiveType(srcType) || !InvokeUtil::IsPrimitiveType(targetType))
                COMPlusThrow(kInvalidCastException, W("InvalidCast_StoreArrayElement"));

            // Get a properly widened type
            InvokeUtil::CreatePrimitiveValue(targetType,srcType,obj,&value);

            // Refresh pData in case GC moved objects around
            pData = refThis->GetDataPtr() + flattenedIndex * refThis->GetComponentSize();

            UINT cbSize = CorTypeInfo::Size(targetType);
            memcpyNoGCRefs(pData, ArgSlotEndiannessFixup(&value, cbSize), cbSize);

            HELPER_METHOD_FRAME_END();
        }
    }
}
FCIMPLEND

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
