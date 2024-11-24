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
    static FCDECL1(INT32, GetCorElementTypeOfElementType, ArrayBase* arrayUNSAFE);
};

extern "C" void QCALLTYPE Array_CreateInstance(QCall::TypeHandle pTypeHnd, INT32 rank, INT32* pLengths, INT32* pBounds, BOOL createFromArrayType, QCall::ObjectHandleOnStack retArray);
extern "C" PCODE QCALLTYPE Array_GetElementConstructorEntrypoint(QCall::TypeHandle pArrayTypeHnd);

#endif // _ARRAYNATIVE_H_
