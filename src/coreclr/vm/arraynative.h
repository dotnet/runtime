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

#include "qcall.h"

class ArrayNative
{
public:
    FCDECL1(static INT32, GetCorElementTypeOfElementType, ArrayBase* arrayUNSAFE);
};

extern "C" PCODE QCALLTYPE Array_GetElementConstructorEntrypoint(QCall::TypeHandle pArrayTypeHnd, QCallExceptionStatus* qcallError);
extern "C" void QCALLTYPE Array_CreateInstance(QCall::TypeHandle pTypeHnd, INT32 rank, INT32* pLengths, INT32* pBounds, BOOL createFromArrayType, QCall::ObjectHandleOnStack retArray, QCallExceptionStatus* qcallError);
extern "C" void QCALLTYPE Array_Ctor(MethodTable* pArrayMT, UINT32 dwNumArgs, INT32* pArgList, QCall::ObjectHandleOnStack retArray, QCallExceptionStatus* qcallError);

#endif // _ARRAYNATIVE_H_
