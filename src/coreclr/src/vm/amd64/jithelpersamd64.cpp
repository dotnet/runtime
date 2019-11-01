// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: JITHelpers.CPP
// ===========================================================================

// This contains JITinterface routines that are specific to the
// AMD64 platform. They are modeled after the X86 specific routines
// found in JIThelp.asm


#include "common.h"
#include "jitinterface.h"
#include "eeconfig.h"
#include "excep.h"
#include "ecall.h"
#include "asmconstants.h"

EXTERN_C void JIT_TailCallHelperStub_ReturnAddress();

TailCallFrame * TailCallFrame::GetFrameFromContext(CONTEXT * pContext)
{
    _ASSERTE((void*)::GetIP(pContext) == JIT_TailCallHelperStub_ReturnAddress);
    return (TailCallFrame*)(pContext->R13 + sizeof(GSCookie));
}

// Assuming pContext is a plain generic call-site, adjust it to look like
// it called into TailCallHelperStub, and is at the point of the call.
TailCallFrame * TailCallFrame::AdjustContextForTailCallHelperStub(CONTEXT * pContext, size_t cbNewArgArea, Thread * pThread)
{
    TailCallFrame * pNewFrame = (TailCallFrame *)(GetSP(pContext) - sizeof(TailCallFrame));

    // R13 is the frame pointer (for popping the stack)
    pContext->R13 = (size_t)pNewFrame - sizeof(GSCookie);
    // R12 is the previous stack pointer, so we can determine if a return buffer from the
    // immediate caller (and thus being discarded via the tail call), or someplace else
    pContext->R12 = GetSP(pContext);
    // for the args and pushed return address of the 'call'
    SetSP(pContext, (size_t)pNewFrame - (cbNewArgArea + sizeof(void*) + sizeof(GSCookie)));

    // For popping the Frame, store the Thread
    pContext->R14 = (DWORD_PTR)pThread;
    // And the current head/top
    pContext->R15 = (DWORD_PTR)pThread->GetFrame(); // m_Next

    return (TailCallFrame *) pNewFrame;
}
