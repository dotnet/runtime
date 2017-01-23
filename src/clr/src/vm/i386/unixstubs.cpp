// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"

extern "C"
{
    void ThrowControlForThread(FaultingExceptionFrame *pfef)
    {
        PORTABILITY_ASSERT("Implement for PAL");
    }

    void NakedThrowHelper()
    {
        PORTABILITY_ASSERT("Implement for PAL");
    }

    void PInvokeStubForHost()
    {
        PORTABILITY_ASSERT("Implement for PAL");
    }

    void PInvokeStubForHostInner(DWORD dwStackSize, LPVOID pStackFrame, LPVOID pTarget)
    {
        PORTABILITY_ASSERT("Implement for PAL");
    }

    void ProfileEnterNaked(FunctionIDOrClientID functionIDOrClientID)    
    {
        PORTABILITY_ASSERT("Implement for PAL");
    }

    void ProfileLeaveNaked(FunctionIDOrClientID functionIDOrClientID)
    {
        PORTABILITY_ASSERT("Implement for PAL");
    }

    void ProfileTailcallNaked(FunctionIDOrClientID functionIDOrClientID)
    {
        PORTABILITY_ASSERT("Implement for PAL");
    }

    void STDCALL JIT_ProfilerEnterLeaveTailcallStub(UINT_PTR ProfilerHandle)
    {
    }

    BOOL CallRtlUnwind()
    {
        PORTABILITY_ASSERT("CallRtlUnwind");
        return FALSE;
    }
};

VOID __cdecl PopSEHRecords(LPVOID pTargetSP)
{
    PORTABILITY_ASSERT("Implement for PAL");
}

EXTERN_C VOID BackPatchWorkerAsmStub()
{
    PORTABILITY_ASSERT("BackPatchWorkerAsmStub");
}

EXTERN_C VOID JIT_TailCall()
{
  PORTABILITY_ASSERT("JIT_TailCall");
}

EXTERN_C VOID JIT_TailCallReturnFromVSD()
{
  PORTABILITY_ASSERT("JIT_TailCallReturnFromVSD");
}

EXTERN_C VOID JIT_TailCallVSDLeave()
{
  PORTABILITY_ASSERT("JIT_TailCallVSDLeave");
}

EXTERN_C VOID JIT_TailCallLeave()
{
  PORTABILITY_ASSERT("JIT_TailCallLeave");
}

PTR_CONTEXT GetCONTEXTFromRedirectedStubStackFrame(T_DISPATCHER_CONTEXT * pDispatcherContext)
{
    PORTABILITY_ASSERT("GetCONTEXTFromRedirectedStubStackFrame");
    return NULL;
}

FaultingExceptionFrame *GetFrameFromRedirectedStubStackFrame(DISPATCHER_CONTEXT *pDispatcherContext)
{
    PORTABILITY_ASSERT("GetFrameFromRedirectedStubStackFrame");
    return NULL;
}
