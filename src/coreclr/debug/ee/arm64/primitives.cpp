// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

#include "stdafx.h"
#include "threads.h"
#include "../../shared/arm64/primitives.cpp"

void CopyREGDISPLAY(REGDISPLAY* pDst, REGDISPLAY* pSrc)
{
    CONTEXT tmp;
    CopyRegDisplay(pSrc, pDst, &tmp);
}

#ifdef FEATURE_EMULATE_SINGLESTEP
void SetSSFlag(T_CONTEXT *, Thread *pThread)
{
    _ASSERTE(pThread != NULL);

    pThread->EnableSingleStep();
}

void UnsetSSFlag(T_CONTEXT *, Thread *pThread)
{
    _ASSERTE(pThread != NULL);

    pThread->DisableSingleStep();
}

// Check if single stepping is enabled.
bool IsSSFlagEnabled(T_CONTEXT *, Thread *pThread)
{
    _ASSERTE(pThread != NULL);

    return pThread->IsSingleStepEnabled();
}
#else // FEATURE_EMULATE_SINGLESTEP
void SetSSFlag(T_CONTEXT *pContext, Thread *)
{
    SetSSFlag(pContext);
}

void UnsetSSFlag(T_CONTEXT *pContext, Thread *)
{
    UnsetSSFlag(pContext);
}

// Check if single stepping is enabled.
bool IsSSFlagEnabled(T_CONTEXT *pContext, Thread *)
{
    return IsSSFlagEnabled(pContext);
}
#endif // FEATURE_EMULATE_SINGLESTEP
