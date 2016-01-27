// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//
// EXCEPX86.H -
//
// This header file is optionally included from Excep.h if the target platform is x86
//


#ifndef __excepx86_h__
#define __excepx86_h__

#include "corerror.h"  // HResults for the COM+ Runtime

#include "../dlls/mscorrc/resource.h"

#define THROW_CONTROL_FOR_THREAD_FUNCTION  ThrowControlForThread

#define STATUS_CLR_GCCOVER_CODE         STATUS_PRIVILEGED_INSTRUCTION

class Thread;

#if defined(_MSC_VER)
#pragma warning(disable:4733) // Inline asm assigning to `FS:0` : handler not registered as safe handler
                              // Actually, the handler getting set is properly registered
#endif

#define INSTALL_EXCEPTION_HANDLING_RECORD(record)               \
    {                                                           \
        PEXCEPTION_REGISTRATION_RECORD __record = (record);     \
        _ASSERTE(__record < GetCurrentSEHRecord());             \
        __record->Next = (PEXCEPTION_REGISTRATION_RECORD)__readfsdword(0); \
        __writefsdword(0, (DWORD)__record);                     \
    }

//
// Note: this only pops a handler from the top of the stack. It will not remove a record from the middle of the
// chain, and I can assure you that you don't want to do that anyway.
//
#define UNINSTALL_EXCEPTION_HANDLING_RECORD(record)             \
    {                                                           \
        PEXCEPTION_REGISTRATION_RECORD __record = (record);     \
        _ASSERTE(__record == GetCurrentSEHRecord());            \
        __writefsdword(0, (DWORD)__record->Next);               \
    }

// stackOverwriteBarrier is used to detect overwriting of stack which will mess up handler registration
#if defined(_DEBUG)
#define DECLARE_CPFH_EH_RECORD(pCurThread) \
    FrameHandlerExRecordWithBarrier *___pExRecordWithBarrier = (FrameHandlerExRecordWithBarrier *)_alloca(sizeof(FrameHandlerExRecordWithBarrier)); \
    for (int ___i =0; ___i < STACK_OVERWRITE_BARRIER_SIZE; ___i++) \
        ___pExRecordWithBarrier->m_StackOverwriteBarrier[___i] = STACK_OVERWRITE_BARRIER_VALUE; \
    FrameHandlerExRecord *___pExRecord = &(___pExRecordWithBarrier->m_ExRecord); \
    ___pExRecord->m_ExReg.Handler = (PEXCEPTION_ROUTINE)COMPlusFrameHandler; \
    ___pExRecord->m_pEntryFrame = (pCurThread)->GetFrame();

#else
#define DECLARE_CPFH_EH_RECORD(pCurThread) \
    FrameHandlerExRecord *___pExRecord = (FrameHandlerExRecord *)_alloca(sizeof(FrameHandlerExRecord)); \
    ___pExRecord->m_ExReg.Handler = (PEXCEPTION_ROUTINE)COMPlusFrameHandler; \
    ___pExRecord->m_pEntryFrame = (pCurThread)->GetFrame();

#endif

//
// Retrieves the redirected CONTEXT* from the stack frame of one of the
// RedirectedHandledJITCaseForXXX_Stub's.
//
PTR_CONTEXT GetCONTEXTFromRedirectedStubStackFrame(CONTEXT * pContext);

PEXCEPTION_REGISTRATION_RECORD GetCurrentSEHRecord();
PEXCEPTION_REGISTRATION_RECORD GetFirstCOMPlusSEHRecord(Thread*);

// Determine the address of the instruction that made the current call.
inline
PCODE GetAdjustedCallAddress(PCODE returnAddress)
{
    LIMITED_METHOD_CONTRACT;
    return returnAddress - 5;
}

BOOL AdjustContextForVirtualStub(EXCEPTION_RECORD *pExceptionRecord, CONTEXT *pContext);

#endif // __excepx86_h__
