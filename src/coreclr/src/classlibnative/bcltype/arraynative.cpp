// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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

FCIMPL1(INT32, ArrayNative::GetRank, ArrayBase* array)
{
    FCALL_CONTRACT;

    VALIDATEOBJECT(array);

    if (array == NULL)
        FCThrow(kNullReferenceException);

    return array->GetRank();
}
FCIMPLEND


FCIMPL2(INT32, ArrayNative::GetLowerBound, ArrayBase* array, unsigned int dimension)
{
    FCALL_CONTRACT;

    VALIDATEOBJECT(array);

    if (array == NULL)
        FCThrow(kNullReferenceException);
    
    if (dimension != 0)
    {
        // Check the dimension is within our rank
        unsigned int rank = array->GetRank();
    
        if (dimension >= rank)
            FCThrowRes(kIndexOutOfRangeException, W("IndexOutOfRange_ArrayRankIndex"));
    }

    return array->GetLowerBoundsPtr()[dimension];
}
FCIMPLEND


// Get inclusive upper bound
FCIMPL2(INT32, ArrayNative::GetUpperBound, ArrayBase* array, unsigned int dimension)
{
    FCALL_CONTRACT;

    VALIDATEOBJECT(array);

    if (array == NULL)
        FCThrow(kNullReferenceException);
    
    if (dimension != 0)
    {
        // Check the dimension is within our rank
        unsigned int rank = array->GetRank();
    
        if (dimension >= rank)
            FCThrowRes(kIndexOutOfRangeException, W("IndexOutOfRange_ArrayRankIndex"));
    }

    return array->GetBoundsPtr()[dimension] + array->GetLowerBoundsPtr()[dimension] - 1;
}
FCIMPLEND


FCIMPL2(INT32, ArrayNative::GetLength, ArrayBase* array, unsigned int dimension)
{
    FCALL_CONTRACT;

    VALIDATEOBJECT(array);

    if (array==NULL)
        FCThrow(kNullReferenceException);
    
    if (dimension != 0)
    {
        // Check the dimension is within our rank
        unsigned int rank = array->GetRank();
        if (dimension >= rank)
            FCThrow(kIndexOutOfRangeException);
    }
    
    return array->GetBoundsPtr()[dimension];
}
FCIMPLEND


FCIMPL1(INT32, ArrayNative::GetLengthNoRank, ArrayBase* array)
{
    FCALL_CONTRACT;

    VALIDATEOBJECT(array);

    if (array==NULL)
        FCThrow(kNullReferenceException);

    SIZE_T numComponents = array->GetNumComponents();
    if (numComponents > INT32_MAX)
        FCThrow(kOverflowException);

    return (INT32)numComponents;
}
FCIMPLEND


FCIMPL1(INT64, ArrayNative::GetLongLengthNoRank, ArrayBase* array)
{
    FCALL_CONTRACT;

    VALIDATEOBJECT(array);

    if (array==NULL)
        FCThrow(kNullReferenceException);

    return array->GetNumComponents();
}
FCIMPLEND


FCIMPL1(void*, ArrayNative::GetRawArrayData, ArrayBase* array)
{
    FCALL_CONTRACT;

    VALIDATEOBJECT(array);

    _ASSERTE(array != NULL);

    return array->GetDataPtr();
}
FCIMPLEND

FCIMPL1(INT32, ArrayNative::GetElementSize, ArrayBase* array)
{
    FCALL_CONTRACT;

    VALIDATEOBJECT(array);

    _ASSERTE(array != NULL);

    return (INT32)array->GetComponentSize();
}
FCIMPLEND



// array is GC protected by caller
void ArrayInitializeWorker(ARRAYBASEREF * arrayRef,
                           MethodTable* pArrayMT,
                           MethodTable* pElemMT)
{
    STATIC_CONTRACT_MODE_COOPERATIVE;

    // Ensure that the array element type is fully loaded before executing its code
    pElemMT->EnsureInstanceActive();

    //can not use contract here because of SEH
    _ASSERTE(IsProtectedByGCFrame (arrayRef));
    
    SIZE_T offset = ArrayBase::GetDataPtrOffset(pArrayMT);
    SIZE_T size = pArrayMT->GetComponentSize();
    SIZE_T cElements = (*arrayRef)->GetNumComponents();

    MethodTable * pCanonMT = pElemMT->GetCanonicalMethodTable();
    WORD slot = pCanonMT->GetDefaultConstructorSlot();

    PCODE ctorFtn = pCanonMT->GetSlot(slot);

#if defined(_TARGET_X86_) && !defined(FEATURE_PAL)
    BEGIN_CALL_TO_MANAGED();


    for (SIZE_T i = 0; i < cElements; i++)
    {
        // Since GetSlot() is not idempotent and may have returned
        // a non-optimal entry-point the first time round.
        if (i == 1)
        {
            ctorFtn = pCanonMT->GetSlot(slot);
        }

        BYTE* thisPtr = (((BYTE*) OBJECTREFToObject (*arrayRef)) + offset);

#ifdef _DEBUG
        __asm {
            mov ECX, thisPtr
            mov EDX, pElemMT // Instantiation argument if the type is generic
            call    [ctorFtn]
            nop                // Mark the fact that we can call managed code
        }
#else // _DEBUG
        typedef void (__fastcall * CtorFtnType)(BYTE*, BYTE*);
        (*(CtorFtnType)ctorFtn)(thisPtr, (BYTE*)pElemMT);
#endif // _DEBUG

        offset += size;
    }

    END_CALL_TO_MANAGED();
#else // _TARGET_X86_ && !FEATURE_PAL
    //
    // This is quite a bit slower, but it is portable.
    //

    for (SIZE_T i =0; i < cElements; i++)
    {
        // Since GetSlot() is not idempotent and may have returned
        // a non-optimal entry-point the first time round.
        if (i == 1)
        {
            ctorFtn = pCanonMT->GetSlot(slot);
        }

        BYTE* thisPtr = (((BYTE*) OBJECTREFToObject (*arrayRef)) + offset);

        PREPARE_NONVIRTUAL_CALLSITE_USING_CODE(ctorFtn);
        DECLARE_ARGHOLDER_ARRAY(args, 2);
        args[ARGNUM_0] = PTR_TO_ARGHOLDER(thisPtr);
        args[ARGNUM_1] = PTR_TO_ARGHOLDER(pElemMT); // Instantiation argument if the type is generic
        CALL_MANAGED_METHOD_NORET(args);

        offset += size;
    }
#endif // !_TARGET_X86_ || FEATURE_PAL
}


FCIMPL1(void, ArrayNative::Initialize, ArrayBase* array)
{
    FCALL_CONTRACT;

    if (array == NULL)
    {
        FCThrowVoid(kNullReferenceException);
    }


    MethodTable* pArrayMT = array->GetMethodTable();

    TypeHandle thElem = pArrayMT->GetApproxArrayElementTypeHandle();
    if (thElem.IsTypeDesc())
        return;

    MethodTable * pElemMT = thElem.AsMethodTable();
    if (!pElemMT->HasDefaultConstructor() || !pElemMT->IsValueType())
        return;

    ARRAYBASEREF arrayRef (array);
    HELPER_METHOD_FRAME_BEGIN_1(arrayRef);

    ArrayInitializeWorker(&arrayRef, pArrayMT, pElemMT);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND






    // Returns an enum saying whether you can copy an array of srcType into destType.
ArrayNative::AssignArrayEnum ArrayNative::CanAssignArrayTypeNoGC(const BASEARRAYREF pSrc, const BASEARRAYREF pDest)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(pSrc != NULL);
        PRECONDITION(pDest != NULL);
    }
    CONTRACTL_END;

    // The next 50 lines are a little tricky.  Change them with great care.
    // 

    // This first bit is a minor optimization: e.g. when copying byte[] to byte[]
    // we do not need to call GetArrayElementTypeHandle().
    MethodTable *pSrcMT = pSrc->GetMethodTable();
    MethodTable *pDestMT = pDest->GetMethodTable();
    if (pSrcMT == pDestMT)
        return AssignWillWork;

    TypeHandle srcTH = pSrcMT->GetApproxArrayElementTypeHandle();
    TypeHandle destTH = pDestMT->GetApproxArrayElementTypeHandle();
    if (srcTH == destTH) // This check kicks for different array kind or dimensions
        return AssignWillWork;

    // Value class boxing
    if (srcTH.IsValueType() && !destTH.IsValueType())
    {
        switch (srcTH.CanCastToNoGC(destTH))
        {
        case TypeHandle::CanCast : return AssignBoxValueClassOrPrimitive;
        case TypeHandle::CannotCast : return AssignWrongType;
        default : return AssignDontKnow;
        }
    }

    // Value class unboxing.
    if (!srcTH.IsValueType() && destTH.IsValueType())
    {
        if (srcTH.CanCastToNoGC(destTH) == TypeHandle::CanCast)
            return AssignUnboxValueClass;
        else if (destTH.CanCastToNoGC(srcTH) == TypeHandle::CanCast)   // V extends IV. Copying from IV to V, or Object to V.
            return AssignUnboxValueClass;
        else
            return AssignDontKnow;
    }
    
    const CorElementType srcElType = srcTH.GetVerifierCorElementType();
    const CorElementType destElType = destTH.GetVerifierCorElementType();
    _ASSERTE(srcElType < ELEMENT_TYPE_MAX);
    _ASSERTE(destElType < ELEMENT_TYPE_MAX);

    // Copying primitives from one type to another
    if (CorTypeInfo::IsPrimitiveType_NoThrow(srcElType) && CorTypeInfo::IsPrimitiveType_NoThrow(destElType))
    {
        if (GetNormalizedIntegralArrayElementType(srcElType) == GetNormalizedIntegralArrayElementType(destElType))
            return AssignWillWork;

        if (InvokeUtil::CanPrimitiveWiden(destElType, srcElType))
            return AssignPrimitiveWiden;
        else
            return AssignWrongType;
    }
    
    // dest Object extends src
    if (srcTH.CanCastToNoGC(destTH) == TypeHandle::CanCast)
        return AssignWillWork;
    
    // src Object extends dest
    if (destTH.CanCastToNoGC(srcTH) == TypeHandle::CanCast)
        return AssignMustCast;
    
    // class X extends/implements src and implements dest.
    if (destTH.IsInterface() && srcElType != ELEMENT_TYPE_VALUETYPE)
        return AssignMustCast;
    
    // class X implements src and extends/implements dest
    if (srcTH.IsInterface() && destElType != ELEMENT_TYPE_VALUETYPE)
        return AssignMustCast;

    return AssignDontKnow;
}


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

    // The next 50 lines are a little tricky.  Change them with great care.
    // 

    // This first bit is a minor optimization: e.g. when copying byte[] to byte[]
    // we do not need to call GetArrayElementTypeHandle().
    MethodTable *pSrcMT = pSrc->GetMethodTable();
    MethodTable *pDestMT = pDest->GetMethodTable();
    if (pSrcMT == pDestMT)
        return AssignWillWork;

    TypeHandle srcTH = pSrcMT->GetApproxArrayElementTypeHandle();
    TypeHandle destTH = pDestMT->GetApproxArrayElementTypeHandle();
    if (srcTH == destTH) // This check kicks for different array kind or dimensions
        return AssignWillWork;
    
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
        if (srcElType == destElType)
            return AssignWillWork;
        if (InvokeUtil::CanPrimitiveWiden(destElType, srcElType))
            return AssignPrimitiveWiden;
        else
            return AssignWrongType;
    }
    
    // dest Object extends src
    if (srcTH.CanCastTo(destTH))
        return AssignWillWork;
    
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
    MethodTable * pSrcMT = srcTH.GetMethodTable();
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

    MethodTable * pDestMT = destTH.GetMethodTable();
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

void ArrayNative::ArrayCopyNoTypeCheck(BASEARRAYREF pSrc, unsigned int srcIndex, BASEARRAYREF pDest, unsigned int destIndex, unsigned int length)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(pSrc != NULL);
        PRECONDITION(srcIndex >= 0);
        PRECONDITION(pDest != NULL);
        PRECONDITION(length > 0);
    }
    CONTRACTL_END;

    BYTE *src = (BYTE*)pSrc->GetDataPtr();
    BYTE *dst = (BYTE*)pDest->GetDataPtr();
    SIZE_T size = pSrc->GetComponentSize();

    src += srcIndex * size;
    dst += destIndex * size;

    if (pDest->GetMethodTable()->ContainsPointers())
    {
        memmoveGCRefs(dst, src, length * size);
    }
    else
    {
        memmove(dst, src, length * size);
    }
}

FCIMPL6(void, ArrayNative::ArrayCopy, ArrayBase* m_pSrc, INT32 m_iSrcIndex, ArrayBase* m_pDst, INT32 m_iDstIndex, INT32 m_iLength, CLR_BOOL reliable)
{
    FCALL_CONTRACT;
    
    struct _gc
    {
        BASEARRAYREF pSrc;
        BASEARRAYREF pDst;
    } gc;

    gc.pSrc = (BASEARRAYREF)m_pSrc;
    gc.pDst = (BASEARRAYREF)m_pDst;

    //
    // creating a HelperMethodFrame is quite expensive, 
    // so we want to delay this for the most common case which doesn't trigger a GC.
    // FCThrow is needed to throw an exception without a HelperMethodFrame
    //

    // cannot pass null for source or destination
    if (gc.pSrc == NULL || gc.pDst == NULL) {
        FCThrowArgumentNullVoid(gc.pSrc==NULL ? W("sourceArray") : W("destinationArray"));
    }

    // source and destination must be arrays
    _ASSERTE(gc.pSrc->GetMethodTable()->IsArray());
    _ASSERTE(gc.pDst->GetMethodTable()->IsArray());

    // Equal method tables should imply equal rank
    _ASSERTE(!(gc.pSrc->GetMethodTable() == gc.pDst->GetMethodTable() && gc.pSrc->GetRank() != gc.pDst->GetRank()));

    // Which enables us to avoid touching the EEClass in simple cases
    if (gc.pSrc->GetMethodTable() != gc.pDst->GetMethodTable() && gc.pSrc->GetRank() != gc.pDst->GetRank()) {
        FCThrowResVoid(kRankException, W("Rank_MustMatch"));
    }

    g_IBCLogger.LogMethodTableAccess(gc.pSrc->GetMethodTable());
    g_IBCLogger.LogMethodTableAccess(gc.pDst->GetMethodTable());

    int srcLB = gc.pSrc->GetLowerBoundsPtr()[0];
    int destLB = gc.pDst->GetLowerBoundsPtr()[0];
    // array bounds checking
    const unsigned int srcLen = gc.pSrc->GetNumComponents();
    const unsigned int destLen = gc.pDst->GetNumComponents();
    if (m_iLength < 0)
        FCThrowArgumentOutOfRangeVoid(W("length"), W("ArgumentOutOfRange_NeedNonNegNum"));

    if (m_iSrcIndex < srcLB || (m_iSrcIndex - srcLB < 0))
        FCThrowArgumentOutOfRangeVoid(W("sourceIndex"), W("ArgumentOutOfRange_ArrayLB"));
        
    if (m_iDstIndex < destLB || (m_iDstIndex - destLB < 0))
        FCThrowArgumentOutOfRangeVoid(W("destinationIndex"), W("ArgumentOutOfRange_ArrayLB"));

    if ((DWORD)(m_iSrcIndex - srcLB + m_iLength) > srcLen)
        FCThrowArgumentVoid(W("sourceArray"), W("Arg_LongerThanSrcArray"));
        
    if ((DWORD)(m_iDstIndex - destLB + m_iLength) > destLen)
        FCThrowArgumentVoid(W("destinationArray"), W("Arg_LongerThanDestArray"));

    int r = 0;

    // Small perf optimization - we copy from one portion of an array back to
    // itself a lot when resizing collections, etc.  The cost of doing the type
    // checking is significant for copying small numbers of bytes (~half of the time
    // for copying 1 byte within one array from element 0 to element 1).
    if (gc.pSrc == gc.pDst)
        r = AssignWillWork;
    else
        r = CanAssignArrayTypeNoGC(gc.pSrc, gc.pDst);

    if (r == AssignWrongType) {
        FCThrowResVoid(kArrayTypeMismatchException, W("ArrayTypeMismatch_CantAssignType"));
    }

    if (r == AssignWillWork) {
        if (m_iLength > 0)
            ArrayCopyNoTypeCheck(gc.pSrc, m_iSrcIndex - srcLB, gc.pDst, m_iDstIndex - destLB, m_iLength);

        FC_GC_POLL();
        return;
    }
    else if (reliable) {
        FCThrowResVoid(kArrayTypeMismatchException, W("ArrayTypeMismatch_ConstrainedCopy"));
    }

    HELPER_METHOD_FRAME_BEGIN_PROTECT(gc);
    if (r == AssignDontKnow)
    {
        r = CanAssignArrayType(gc.pSrc, gc.pDst);
    }
    CONSISTENCY_CHECK(r != AssignDontKnow);

    if (r == AssignWrongType)
        COMPlusThrow(kArrayTypeMismatchException, W("ArrayTypeMismatch_CantAssignType"));

    // If we were called from Array.ConstrainedCopy, ensure that the array copy
    // is guaranteed to succeed.
    _ASSERTE(!reliable || r == AssignWillWork);

    if (m_iLength > 0)
    {
        switch (r)
        {
            case AssignWillWork:
                ArrayCopyNoTypeCheck(gc.pSrc, m_iSrcIndex - srcLB, gc.pDst, m_iDstIndex - destLB, m_iLength);
                break;

            case AssignUnboxValueClass:
                UnBoxEachElement(gc.pSrc, m_iSrcIndex - srcLB, gc.pDst, m_iDstIndex - destLB, m_iLength);
                break;

            case AssignBoxValueClassOrPrimitive:
                BoxEachElement(gc.pSrc, m_iSrcIndex - srcLB, gc.pDst, m_iDstIndex - destLB, m_iLength);
                break;

            case AssignMustCast:
                CastCheckEachElement(gc.pSrc, m_iSrcIndex - srcLB, gc.pDst, m_iDstIndex - destLB, m_iLength);
                break;

            case AssignPrimitiveWiden:
                PrimitiveWiden(gc.pSrc, m_iSrcIndex - srcLB, gc.pDst, m_iDstIndex - destLB, m_iLength);
                break;

            default:
                _ASSERTE(!"Fell through switch in Array.Copy!");
        }
    }

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND


FCIMPL5(void*, ArrayNative::GetRawArrayGeometry, ArrayBase* pArray, UINT32* pNumComponents, UINT32* pElementSize, INT32* pLowerBound, CLR_BOOL* pContainsGCPointers)
{
   VALIDATEOBJECT(pArray);

   _ASSERTE(pArray != NULL);

    MethodTable *pMT = pArray->GetMethodTable();

    *pNumComponents = pArray->GetNumComponents();
    *pElementSize = pMT->RawGetComponentSize();
    *pLowerBound = pArray->GetLowerBoundsPtr()[0];
    *pContainsGCPointers = !!pMT->ContainsPointers();

    return (BYTE*)pArray + ArrayBase::GetDataPtrOffset(pMT);
}
FCIMPLEND



// Check we're allowed to create an array with the given element type.
void ArrayNative::CheckElementType(TypeHandle elementType)
{
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

        // That's all the dangerous simple types we know, it must be OK.
        return;
    }

    // Checks apply recursively for arrays of arrays etc.
    if (elementType.IsArray())
    {
        CheckElementType(elementType.GetElementType());
        return;
    }

    // ByRefs and generic type variables are never allowed.
    if (elementType.IsByRef() || elementType.IsGenericVariable())
        COMPlusThrow(kNotSupportedException, W("NotSupported_Type"));

    // We can create pointers and function pointers, but it requires skip verification permission.
    CorElementType etType = elementType.GetSignatureCorElementType();
    if (etType == ELEMENT_TYPE_PTR || etType == ELEMENT_TYPE_FNPTR)
    {
        return;
    }

    // We shouldn't get here (it means we've encountered a new type of typehandle if we do).
    _ASSERTE(!"Shouldn't get here, unknown type handle type");
    COMPlusThrow(kNotSupportedException);
}

FCIMPL4(Object*, ArrayNative::CreateInstance, void* elementTypeHandle, INT32 rank, INT32* pLengths, INT32* pLowerBounds)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(rank > 0);
        PRECONDITION(CheckPointer(pLengths));
        PRECONDITION(CheckPointer(pLowerBounds, NULL_OK));
    } CONTRACTL_END;

    OBJECTREF pRet = NULL;
    TypeHandle elementType = TypeHandle::FromPtr(elementTypeHandle);

    _ASSERTE(!elementType.IsNull());

    // pLengths and pLowerBounds are pinned buffers. No need to protect them.
    HELPER_METHOD_FRAME_BEGIN_RET_0();

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

        DWORD boundsSize = 0;
        INT32* bounds;
        if (pLowerBounds != NULL) {
            if (!ClrSafeInt<DWORD>::multiply(rank, 2, boundsSize))
                COMPlusThrowOM();
            DWORD dwAllocaSize = 0;
            if (!ClrSafeInt<DWORD>::multiply(boundsSize, sizeof(INT32), dwAllocaSize))
                COMPlusThrowOM();

            bounds = (INT32*) _alloca(dwAllocaSize);

            for (int i=0;i<rank;i++) {
                bounds[2*i] = pLowerBounds[i];
                bounds[2*i+1] = pLengths[i];
            }
        }
        else {
            boundsSize = rank;

            DWORD dwAllocaSize = 0;
            if (!ClrSafeInt<DWORD>::multiply(boundsSize, sizeof(INT32), dwAllocaSize))
                COMPlusThrowOM();

            bounds = (INT32*) _alloca(dwAllocaSize);

            // We need to create a private copy of pLengths to avoid holes caused
            // by caller mutating the array
            for (int i=0;i<rank;i++)
                bounds[i] = pLengths[i];
        }

        pRet = AllocateArrayEx(typeHnd, bounds, boundsSize);
    }

Done: ;
    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(pRet);
}
FCIMPLEND


FCIMPL4(void, ArrayNative::GetReference, ArrayBase* refThisUNSAFE, TypedByRef* elemRef, INT32 rank, INT32* pIndices)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(rank >= 0);
    } CONTRACTL_END;

    // FC_GC_POLL not necessary. We poll for GC in Array.Rank that's always called
    // right before this function
    FC_GC_POLL_NOT_NEEDED();

    BASEARRAYREF    refThis  = (BASEARRAYREF) refThisUNSAFE;

    _ASSERTE(rank == (INT32)refThis->GetRank());

    SIZE_T Offset               = 0;
    const INT32 *pBoundsPtr     = refThis->GetBoundsPtr();

    if (rank == 1)
    {
        Offset = pIndices[0] - refThis->GetLowerBoundsPtr()[0];

        // Bounds check each index
        // Casting to unsigned allows us to use one compare for [0..limit-1]
        if (((UINT32) Offset) >= ((UINT32) pBoundsPtr[0]))
            FCThrowVoid(kIndexOutOfRangeException);
    }
    else
    {
        // Avoid redundant computation in GetLowerBoundsPtr
        const INT32 *pLowerBoundsPtr = pBoundsPtr + rank;
        _ASSERTE(refThis->GetLowerBoundsPtr() == pLowerBoundsPtr);

        SIZE_T Multiplier = 1;

        for (int i = rank; i >= 1; i--) {
            INT32 curIndex = pIndices[i-1] - pLowerBoundsPtr[i-1];

            // Bounds check each index
            // Casting to unsigned allows us to use one compare for [0..limit-1]
            if (((UINT32) curIndex) >= ((UINT32) pBoundsPtr[i-1]))
                FCThrowVoid(kIndexOutOfRangeException);

            Offset += curIndex * Multiplier;
            Multiplier *= pBoundsPtr[i-1];
        }
    }

    TypeHandle arrayElementType = refThis->GetArrayElementTypeHandle();

    // Legacy behavior
    if (arrayElementType.IsTypeDesc())
    {
        CorElementType elemtype = arrayElementType.AsTypeDesc()->GetInternalCorElementType();
        if (elemtype == ELEMENT_TYPE_PTR || elemtype == ELEMENT_TYPE_FNPTR)
            FCThrowResVoid(kNotSupportedException, W("NotSupported_Type"));
    }
#ifdef _DEBUG
    CorElementType elemtype = arrayElementType.GetInternalCorElementType();
    _ASSERTE(elemtype != ELEMENT_TYPE_PTR && elemtype != ELEMENT_TYPE_FNPTR);
#endif

    elemRef->data = refThis->GetDataPtr() + (Offset * refThis->GetComponentSize());
    elemRef->type = arrayElementType;
}
FCIMPLEND

FCIMPL2(void, ArrayNative::SetValue, TypedByRef * target, Object* objUNSAFE)
{
    FCALL_CONTRACT;
    
    OBJECTREF obj = ObjectToOBJECTREF(objUNSAFE);

    TypeHandle thTarget(target->type);

    MethodTable* pTargetMT = thTarget.GetMethodTable();
    PREFIX_ASSUME(NULL != pTargetMT);

    if (obj == NULL)
    {
        // Null is the universal zero...
        if (pTargetMT->IsValueType())
            InitValueClass(target->data,pTargetMT);
        else
            ClearObjectReference((OBJECTREF*)target->data);
    }
    else
    if (thTarget == TypeHandle(g_pObjectClass))
    {
        // Everything is compatible with Object
        SetObjectReference((OBJECTREF*)target->data,(OBJECTREF)obj);
    }
    else
    if (!pTargetMT->IsValueType())
    {
        if (ObjIsInstanceOfNoGC(OBJECTREFToObject(obj), thTarget) != TypeHandle::CanCast)
        {
            // target->data is protected by the caller
            HELPER_METHOD_FRAME_BEGIN_1(obj);

            if (!ObjIsInstanceOf(OBJECTREFToObject(obj), thTarget))
                COMPlusThrow(kInvalidCastException,W("InvalidCast_StoreArrayElement"));

            HELPER_METHOD_FRAME_END();
        }

        SetObjectReference((OBJECTREF*)target->data,obj);
    }
    else
    {
        // value class or primitive type

        if (!pTargetMT->UnBoxInto(target->data, obj))
        {
            // target->data is protected by the caller
            HELPER_METHOD_FRAME_BEGIN_1(obj);

            ARG_SLOT value = 0;

            // Allow enum -> primitive conversion, disallow primitive -> enum conversion
            TypeHandle thSrc = obj->GetTypeHandle();
            CorElementType srcType = thSrc.GetVerifierCorElementType();
            CorElementType targetType = thTarget.GetSignatureCorElementType();

            if (!InvokeUtil::IsPrimitiveType(srcType) || !InvokeUtil::IsPrimitiveType(targetType))
                COMPlusThrow(kInvalidCastException, W("InvalidCast_StoreArrayElement"));

            // Get a properly widened type
            InvokeUtil::CreatePrimitiveValue(targetType,srcType,obj,&value);

            UINT cbSize = CorTypeInfo::Size(targetType);
            memcpyNoGCRefs(target->data, ArgSlotEndianessFixup(&value, cbSize), cbSize);

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

    // Report the RVA field to the logger.
    g_IBCLogger.LogRVADataAccess(pField);

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
