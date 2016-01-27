// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ===========================================================================
// File: JITHelpersARM.CPP
// ===========================================================================

// This contains JITinterface routines that are specific to the
// ARM platform. They are modeled after the AMD64 specific routines
// found in JIThelpersAMD64.cpp


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
    return (TailCallFrame*)(pContext->R7 - offsetof(TailCallFrame, m_calleeSavedRegisters));
}

// Assuming pContext is a plain generic call-site, adjust it to look like
// it called into TailCallHelperStub, and is at the point of the call.
TailCallFrame * TailCallFrame::AdjustContextForTailCallHelperStub(CONTEXT * pContext, size_t cbNewArgArea, Thread * pThread)
{
    TailCallFrame * pNewFrame = (TailCallFrame *) (GetSP(pContext) - sizeof(TailCallFrame));

    // The return addres for the pseudo-call
    pContext->Lr  = (DWORD_PTR)JIT_TailCallHelperStub_ReturnAddress;
    // The R11/ETW chain 'frame' pointer
    pContext->R11 = GetSP(pContext) - (2 * sizeof(DWORD)); // LR & R11
    // The unwind data frame pointer
    pContext->R7  = pContext->R11 - (7 * sizeof(DWORD)); // r4-R10 non-volatile registers
    // for the args and the remainder of the FrameWithCookie<TailCallFrame>
    SetSP(pContext, (size_t) pNewFrame - (cbNewArgArea + sizeof(GSCookie)));

    // For popping the Frame, store the Thread
    pContext->R6  = (DWORD_PTR)pThread;
    // And the current head/top
    pContext->R5  = (DWORD_PTR)pThread->GetFrame();

    return pNewFrame;
}

