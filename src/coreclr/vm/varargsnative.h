// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: VarArgsNative.h
//

//
// This module contains the implementation of the native methods for the
//  varargs class(es)..
//

#ifndef _VARARGSNATIVE_H_
#define _VARARGSNATIVE_H_

#include "clrvarargs.h"

extern "C" void QCALLTYPE ArgIterator_Init(VARARGS* thisPtr, PVOID cookie);
extern "C" void QCALLTYPE ArgIterator_Init2(VARARGS* thisPtr, PVOID cookie, PVOID firstArg);
extern "C" void* QCALLTYPE ArgIterator_GetNextArgType(VARARGS* thisPtr);
extern "C" void QCALLTYPE ArgIterator_GetNextArg(VARARGS* thisPtr, TypedByRef* pResult);
extern "C" void QCALLTYPE ArgIterator_GetNextArg2(VARARGS* thisPtr, QCall::TypeHandle pType, TypedByRef* pResult);

#endif // _VARARGSNATIVE_H_
