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
};

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
