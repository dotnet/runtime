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

#if defined(TARGET_32BIT)
extern "C" INT32 QCALLTYPE DivInt32Internal(INT32 dividend, INT32 divisor);
extern "C" UINT32 QCALLTYPE DivUInt32Internal(UINT32 dividend, UINT32 divisor);
extern "C" INT64 QCALLTYPE DivInt64Internal(INT64 dividend, INT64 divisor);
extern "C" UINT64 QCALLTYPE DivUInt64Internal(UINT64 dividend, UINT64 divisor);
extern "C" INT32 QCALLTYPE ModInt32Internal(INT32 dividend, INT32 divisor);
extern "C" UINT32 QCALLTYPE ModUInt32Internal(UINT32 dividend, UINT32 divisor);
extern "C" INT64 QCALLTYPE ModInt64Internal(INT64 dividend, INT64 divisor);
extern "C" UINT64 QCALLTYPE ModUInt64Internal(UINT64 dividend, UINT64 divisor);
#endif // TARGET_32BIT

#endif // _JITQCALLHELPERS_H
