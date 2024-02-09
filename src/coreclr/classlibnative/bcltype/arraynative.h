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
#include "runtimehandles.h"

struct FCALLRuntimeFieldHandle
{
    ReflectFieldObject *pFieldDONOTUSEDIRECTLY;
};
#define FCALL_RFH_TO_REFLECTFIELD(x) (x).pFieldDONOTUSEDIRECTLY

class ArrayNative
{
public:
    static FCDECL1(INT32, GetCorElementTypeOfElementType, ArrayBase* arrayUNSAFE);

    static FCDECL2(FC_BOOL_RET, IsSimpleCopy, ArrayBase* pSrc, ArrayBase* pDst);

    // This set of methods will set a value in an array
    static FCDECL3(void, SetValue, ArrayBase* refThisUNSAFE, Object* objUNSAFE, INT_PTR flattenedIndex);

    // This method will initialize an array from a TypeHandle
    // to a field.
    static FCDECL2_IV(void, InitializeArray, ArrayBase* vArrayRef, FCALLRuntimeFieldHandle structField);

    // This method will acquire data to create a span from a TypeHandle
    // to a field.
    static FCDECL3_VVI(void*, GetSpanDataFrom, FCALLRuntimeFieldHandle structField, FCALLRuntimeTypeHandle targetTypeUnsafe, INT32* count);
};

extern "C" void QCALLTYPE Array_CreateInstance(QCall::TypeHandle pTypeHnd, INT32 rank, INT32* pLengths, INT32* pBounds, BOOL createFromArrayType, QCall::ObjectHandleOnStack retArray);
extern "C" PCODE QCALLTYPE Array_GetElementConstructorEntrypoint(QCall::TypeHandle pArrayTypeHnd);
extern "C" INT32 QCALLTYPE Array_CanAssignArrayType(void* srcTH, void* destTH);

#endif // _ARRAYNATIVE_H_
