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

#ifdef TARGET_32BIT
extern "C" int32_t QCALLTYPE Math_ActualDivisionInt(int32_t i, int32_t j)
{
    QCALL_CONTRACT_NO_GC_TRANSITION

    ASSERT(j && "Divide by zero!");
    return i / j;
}

extern "C" uint32_t QCALLTYPE Math_ActualDivisionUInt(uint32_t i, uint32_t j)
{
    QCALL_CONTRACT_NO_GC_TRANSITION

    ASSERT(j && "Divide by zero!");
    return i / j;
}

extern "C" int32_t QCALLTYPE Math_ActualModulusInt(int32_t i, int32_t j)
{
    QCALL_CONTRACT_NO_GC_TRANSITION

    ASSERT(j && "Divide by zero!");
    return i % j;
}

extern "C" uint32_t QCALLTYPE Math_ActualModulusUInt(uint32_t i, uint32_t j)
{
    QCALL_CONTRACT_NO_GC_TRANSITION

    ASSERT(j && "Divide by zero!");
    return i % j;
}

extern "C" int64_t QCALLTYPE Math_ActualDivisionLong(int64_t i, int64_t j)
{
    QCALL_CONTRACT_NO_GC_TRANSITION

    ASSERT(j && "Divide by zero!");
    return i / j;
}

extern "C" uint64_t QCALLTYPE Math_ActualDivisionULong(uint64_t i, uint64_t j)
{
    QCALL_CONTRACT_NO_GC_TRANSITION

    ASSERT(j && "Divide by zero!");
    return i / j;
}

extern "C" int64_t QCALLTYPE Math_ActualModulusLong(int64_t i, int64_t j)
{
    QCALL_CONTRACT_NO_GC_TRANSITION

    ASSERT(j && "Divide by zero!");
    return i % j;
}

extern "C" uint64_t QCALLTYPE Math_ActualModulusULong(uint64_t i, uint64_t j)
{
    QCALL_CONTRACT_NO_GC_TRANSITION

    ASSERT(j && "Divide by zero!");
    return i % j;
}

#endif // TARGET_32BIT

#endif //_JITQCALLHELPERS_H
