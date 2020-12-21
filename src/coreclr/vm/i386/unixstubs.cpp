// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

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
