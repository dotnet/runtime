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

#endif //_JITQCALLHELPERS_H
