// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// copied from arm and left empty. wasm cannot do most of it

#ifndef __excepcpu_h__
#define __excepcpu_h__

#define THROW_CONTROL_FOR_THREAD_FUNCTION  ThrowControlForThread

#define STATUS_CLR_GCCOVER_CODE         STATUS_ILLEGAL_INSTRUCTION

class Thread;
class FaultingExceptionFrame;

#define INSTALL_EXCEPTION_HANDLING_RECORD(record)
#define UNINSTALL_EXCEPTION_HANDLING_RECORD(record)
#define DECLARE_CPFH_EH_RECORD(pCurThread)

//
// Retrieves the redirected CONTEXT* from the stack frame of one of the
// RedirectedHandledJITCaseForXXX_Stub's.
//
PTR_CONTEXT GetCONTEXTFromRedirectedStubStackFrame(T_CONTEXT * pContext);

inline
PCODE GetAdjustedCallAddress(PCODE returnAddress)
{
    return returnAddress;
}

BOOL AdjustContextForVirtualStub(EXCEPTION_RECORD *pExceptionRecord, T_CONTEXT *pContext);

#endif // __excepcpu_h__
