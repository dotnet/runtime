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

extern "C" void * QCALLTYPE ResolveVirtualFunctionPointer(QCall::ObjectHandleOnStack obj, CORINFO_CLASS_HANDLE classHnd, CORINFO_METHOD_HANDLE methodHnd);
extern "C" CORINFO_GENERIC_HANDLE QCALLTYPE GenericHandleWorker(MethodDesc * pMD, MethodTable * pMT, LPVOID signature, DWORD dictionaryIndexAndSlot, Module* pModule);
extern "C" void QCALLTYPE InitClassHelper(MethodTable* pMT);
extern "C" void QCALLTYPE ThrowInvalidCastException(CORINFO_CLASS_HANDLE pTargetType, CORINFO_CLASS_HANDLE pSourceType);
extern "C" void QCALLTYPE GetThreadStaticsByMethodTable(QCall::ByteRefOnStack refHandle, MethodTable* pMT, bool gcStatic);
extern "C" void QCALLTYPE GetThreadStaticsByIndex(QCall::ByteRefOnStack refHandle, uint32_t staticBlockIndex, bool gcStatic);
extern "C" BOOL QCALLTYPE IsInstanceOf_NoCacheLookup(CORINFO_CLASS_HANDLE type, BOOL throwCastException, QCall::ObjectHandleOnStack objOnStack);

#endif //_JITQCALLHELPERS_H
