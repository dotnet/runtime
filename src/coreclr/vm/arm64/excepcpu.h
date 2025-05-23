// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
//


#ifndef __excepcpu_h__
#define __excepcpu_h__


#define THROW_CONTROL_FOR_THREAD_FUNCTION  RedirectForThreadAbort
EXTERN_C void RedirectForThreadAbort();


#define STATUS_CLR_GCCOVER_CODE         STATUS_ILLEGAL_INSTRUCTION

class Thread;
class FaultingExceptionFrame;

#define INSTALL_EXCEPTION_HANDLING_RECORD(record)
#define UNINSTALL_EXCEPTION_HANDLING_RECORD(record)
//
// On ARM, the COMPlusFrameHandler's work is done by our personality routine.
//
#define DECLARE_CPFH_EH_RECORD(pCurThread)

//
// Retrieves the redirected CONTEXT* from the stack frame of one of the
// RedirectedHandledJITCaseForXXX_Stub's.
//
#ifdef TARGET_WINDOWS
PTR_CONTEXT GetCONTEXTFromRedirectedStubStackFrame(T_DISPATCHER_CONTEXT * pDispatcherContext);
#endif // TARGET_WINDOWS
PTR_CONTEXT GetCONTEXTFromRedirectedStubStackFrame(T_CONTEXT * pContext);

#ifdef TARGET_WINDOWS
//
// Retrieves the FaultingExceptionFrame* from the stack frame of
// RedirectForThreadAbort.
//
FaultingExceptionFrame *GetFrameFromRedirectedStubStackFrame (T_DISPATCHER_CONTEXT *pDispatcherContext);
#endif // TARGET_WINDOWS

inline
PCODE GetAdjustedCallAddress(PCODE returnAddress)
{
    LIMITED_METHOD_CONTRACT;

    return returnAddress - 4;
}

BOOL AdjustContextForVirtualStub(EXCEPTION_RECORD *pExceptionRecord, T_CONTEXT *pContext);

#endif // __excepcpu_h__
