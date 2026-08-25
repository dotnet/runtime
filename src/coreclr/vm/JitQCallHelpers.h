// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Header: JitQCallHelpers.h
**
**
===========================================================*/

#ifndef _JITQCALLHELPERS_H
#define _JITQCALLHELPERS_H

#include "qcall.h"
#include "corinfo.h"

class Module;
class MethodTable;
class MethodDesc;

extern "C" QCallExceptionStatus QCALLTYPE ResolveVirtualFunctionPointer(QCall::ObjectHandleOnStack obj, CORINFO_CLASS_HANDLE classHnd, CORINFO_METHOD_HANDLE methodHnd, PCODE* pReturnValue);
extern "C" QCallExceptionStatus QCALLTYPE GenericHandleWorker(MethodDesc * pMD, MethodTable * pMT, LPVOID signature, DWORD dictionaryIndexAndSlot, Module* pModule, void** pReturnValue);
extern "C" QCallExceptionStatus QCALLTYPE InitClassHelper(MethodTable* pMT);
extern "C" QCallExceptionStatus QCALLTYPE ThrowInvalidCastException(CORINFO_CLASS_HANDLE pTargetType, CORINFO_CLASS_HANDLE pSourceType);
extern "C" QCallExceptionStatus QCALLTYPE GetThreadStaticsByMethodTable(QCall::ByteRefOnStack refHandle, MethodTable* pMT, BOOL gcStatic);
extern "C" QCallExceptionStatus QCALLTYPE GetThreadStaticsByIndex(QCall::ByteRefOnStack refHandle, uint32_t staticBlockIndex, BOOL gcStatic);
extern "C" QCallExceptionStatus QCALLTYPE IsInstanceOf_NoCacheLookup(CORINFO_CLASS_HANDLE type, BOOL throwCastException, QCall::ObjectHandleOnStack objOnStack, BOOL* pReturnValue);

#endif //_JITQCALLHELPERS_H
