// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// EXCEPCPU.H -
//
// This header file is included from Excep.h if the target platform is AMD64
//


#ifndef __excepamd64_h__
#define __excepamd64_h__

#include "corerror.h"  // HResults for the COM+ Runtime

#include "../dlls/mscorrc/resource.h"

class FaultingExceptionFrame;


#define THROW_CONTROL_FOR_THREAD_FUNCTION  RedirectForThrowControl

EXTERN_C void RedirectForThrowControl();

#define STATUS_CLR_GCCOVER_CODE         STATUS_PRIVILEGED_INSTRUCTION

//
// No FS:0, nothing to do.
//
#define INSTALL_EXCEPTION_HANDLING_RECORD(record)
#define UNINSTALL_EXCEPTION_HANDLING_RECORD(record)

//
// On Win64, the COMPlusFrameHandler's work is done by our personality routine.
//
#define DECLARE_CPFH_EH_RECORD(pCurThread)

//
// Retrieves the redirected CONTEXT* from the stack frame of one of the
// RedirectedHandledJITCaseForXXX_Stub's.
//
PTR_CONTEXT GetCONTEXTFromRedirectedStubStackFrame(DISPATCHER_CONTEXT * pDispatcherContext);
PTR_CONTEXT GetCONTEXTFromRedirectedStubStackFrame(CONTEXT * pContext);

//
// Retrieves the FaultingExceptionFrame* from the stack frame of
// RedirectForThrowControl.
//
FaultingExceptionFrame *GetFrameFromRedirectedStubStackFrame (DISPATCHER_CONTEXT *pDispatcherContext);

//
// Functions that wrap RtlVirtualUnwind to make sure that in the AMD64 case all the
// breakpoints have been removed from the Epilogue if RtlVirtualUnwind is going to
// try and disassemble it.
//
#if !defined(DACCESS_COMPILE)
UCHAR GetOpcodeFromManagedBPForAddress(ULONG64 Address, BOOL* HasManagedBreakpoint, BOOL* HasUnmanagedBreakpoint);

#define RtlVirtualUnwind RtlVirtualUnwind_Wrapper

PEXCEPTION_ROUTINE
RtlVirtualUnwind (
          IN ULONG HandlerType,
          IN ULONG64 ImageBase,
          IN ULONG64 ControlPc,
          IN PT_RUNTIME_FUNCTION FunctionEntry,
          IN OUT PCONTEXT ContextRecord,
          OUT PVOID *HandlerData,
          OUT PULONG64 EstablisherFrame,
          IN OUT PKNONVOLATILE_CONTEXT_POINTERS ContextPointers OPTIONAL
          );

PEXCEPTION_ROUTINE
RtlVirtualUnwind_Worker (
          IN ULONG HandlerType,
          IN ULONG64 ImageBase,
          IN ULONG64 ControlPc,
          IN PT_RUNTIME_FUNCTION FunctionEntry,
          IN OUT PCONTEXT ContextRecord,
          OUT PVOID *HandlerData,
          OUT PULONG64 EstablisherFrame,
          IN OUT PKNONVOLATILE_CONTEXT_POINTERS ContextPointers OPTIONAL
          );
#endif // !DACCESS_COMPILE

BOOL AdjustContextForVirtualStub(EXCEPTION_RECORD *pExceptionRecord, CONTEXT *pContext);

#endif // __excepamd64_h__

