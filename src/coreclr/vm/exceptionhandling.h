// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//

#ifndef __EXCEPTION_HANDLING_h__
#define __EXCEPTION_HANDLING_h__

#ifdef FEATURE_EH_FUNCLETS

#include "eexcp.h"
#include "exstatecommon.h"

// This address lies in the NULL pointer partition of the process memory.
// Accessing it will result in AV.
#define INVALID_RESUME_ADDRESS 0x000000000000bad0

EXTERN_C EXCEPTION_DISPOSITION __cdecl
ProcessCLRException(IN     PEXCEPTION_RECORD     pExceptionRecord,
                    IN     PVOID                 pEstablisherFrame,
                    IN OUT PT_CONTEXT            pContextRecord,
                    IN OUT PT_DISPATCHER_CONTEXT pDispatcherContext);

EXTERN_C EXCEPTION_DISPOSITION __cdecl
CallDescrWorkerUnwindFrameChainHandler(IN     PEXCEPTION_RECORD     pExceptionRecord,
                                       IN     PVOID                 pEstablisherFrame,
                                       IN OUT PT_CONTEXT            pContextRecord,
                                       IN OUT PT_DISPATCHER_CONTEXT pDispatcherContext);

VOID DECLSPEC_NORETURN DispatchManagedException(OBJECTREF throwable, CONTEXT *pExceptionContext, EXCEPTION_RECORD *pExceptionRecord = NULL);
VOID DECLSPEC_NORETURN DispatchManagedException(OBJECTREF throwable);
VOID DECLSPEC_NORETURN DispatchManagedException(RuntimeExceptionKind reKind);
VOID DECLSPEC_NORETURN DispatchRethrownManagedException();
VOID DECLSPEC_NORETURN DispatchRethrownManagedException(CONTEXT* pExceptionContext);

enum CLRUnwindStatus { UnwindPending, FirstPassComplete, SecondPassComplete };

enum TrackerMemoryType
{
    memManaged = 0x0001,
    memUnmanaged = 0x0002,
    memBoth = 0x0003,
};

// Enum that specifies the type of EH funclet we are about to invoke
enum EHFuncletType
{
    Filter = 0x0001,
    FaultFinally = 0x0002,
    Catch = 0x0004,
};

// These values are or-ed into the InlinedCallFrame::m_Datum field.
// The bit 0 is used for unrelated purposes (see comments on the
// InlinedCallFrame::m_Datum field for details).
enum class InlinedCallFrameMarker
{
#ifdef HOST_64BIT
    ExceptionHandlingHelper = 2,
    SecondPassFuncletCaller = 4,
#else // HOST_64BIT
    ExceptionHandlingHelper = 1,
    SecondPassFuncletCaller = 2,
#endif // HOST_64BIT
    Mask = ExceptionHandlingHelper | SecondPassFuncletCaller
};

#ifdef FEATURE_INTERPRETER
class ResumeAfterCatchException
{
    TADDR m_resumeSP;
    TADDR m_resumeIP;
public:
    ResumeAfterCatchException(TADDR resumeSP, TADDR resumeIP)
        : m_resumeSP(resumeSP),
          m_resumeIP(resumeIP)
    {}

    void GetResumeContext(TADDR * pResumeSP, TADDR * pResumeIP) const
    {
        *pResumeSP = m_resumeSP;
        *pResumeIP = m_resumeIP;
    }
};
#endif // FEATURE_INTERPRETER

void DECLSPEC_NORETURN ExecuteFunctionBelowContext(PCODE functionPtr, CONTEXT *pContext, size_t targetSSP, size_t arg1 = 0, size_t arg2 = 0);

#endif // FEATURE_EH_FUNCLETS

#if defined(TARGET_X86)
#define USE_CURRENT_CONTEXT_IN_FILTER
#endif // TARGET_X86

#endif  // __EXCEPTION_HANDLING_h__
