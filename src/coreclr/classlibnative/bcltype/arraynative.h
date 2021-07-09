// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: ArrayNative.h
//

//
// ArrayNative
//  This file defines the native methods for the Array
//


#ifndef _ARRAYNATIVE_H_
#define _ARRAYNATIVE_H_

#include "fcall.h"

struct FCALLRuntimeFieldHandle
{
    ReflectFieldObject *pFieldDONOTUSEDIRECTLY;
};
#define FCALL_RFH_TO_REFLECTFIELD(x) (x).pFieldDONOTUSEDIRECTLY

class ArrayNative
{
public:
    static FCDECL1(INT32, GetCorElementTypeOfElementType, ArrayBase* arrayUNSAFE);

    static FCDECL1(void, Initialize, ArrayBase* pArray);

    static FCDECL2(FC_BOOL_RET, IsSimpleCopy, ArrayBase* pSrc, ArrayBase* pDst);
    static FCDECL5(void, CopySlow, ArrayBase* pSrc, INT32 iSrcIndex, ArrayBase* pDst, INT32 iDstIndex, INT32 iLength);

    // This method will create a new array of type type, with zero lower
    // bounds and rank.
    static FCDECL4(Object*, CreateInstance, void* elementTypeHandle, INT32 rank, INT32* pLengths, INT32* pBounds);

    // This method will return a TypedReference to the array element
    static FCDECL2(Object*, GetValue, ArrayBase* refThisUNSAFE, INT_PTR flattenedIndex);

    // This set of methods will set a value in an array
    static FCDECL3(void, SetValue, ArrayBase* refThisUNSAFE, Object* objUNSAFE, INT_PTR flattenedIndex);

    // This method will initialize an array from a TypeHandle
    // to a field.
    static FCDECL2_IV(void, InitializeArray, ArrayBase* vArrayRef, FCALLRuntimeFieldHandle structField);

private:
    // Helper for CreateInstance
    static void CheckElementType(TypeHandle elementType);

    // Return values for CanAssignArrayType
    enum AssignArrayEnum
    {
        AssignWrongType,
        AssignMustCast,
        AssignBoxValueClassOrPrimitive,
        AssignUnboxValueClass,
        AssignPrimitiveWiden,
    };

    // The following functions are all helpers for ArrayCopy
    static AssignArrayEnum CanAssignArrayType(const BASEARRAYREF pSrc, const BASEARRAYREF pDest);
    static void CastCheckEachElement(BASEARRAYREF pSrc, unsigned int srcIndex, BASEARRAYREF pDest, unsigned int destIndex, unsigned int length);
    static void BoxEachElement(BASEARRAYREF pSrc, unsigned int srcIndex, BASEARRAYREF pDest, unsigned int destIndex, unsigned int length);
    static void UnBoxEachElement(BASEARRAYREF pSrc, unsigned int srcIndex, BASEARRAYREF pDest, unsigned int destIndex, unsigned int length);
    static void PrimitiveWiden(BASEARRAYREF pSrc, unsigned int srcIndex, BASEARRAYREF pDest, unsigned int destIndex, unsigned int length);

};

#endif // _ARRAYNATIVE_H_
