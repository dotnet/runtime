// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

#include "stdafx.h"
#include "threads.h"
#include "../../shared/arm/primitives.cpp"

void CopyREGDISPLAY(REGDISPLAY* pDst, REGDISPLAY* pSrc)
{
    CONTEXT tmp;
    CopyRegDisplay(pSrc, pDst, &tmp);
}

void SetSSFlag(DT_CONTEXT *, Thread *pThread)
{
    _ASSERTE(pThread != NULL);

    pThread->EnableSingleStep();
}

void UnsetSSFlag(DT_CONTEXT *, Thread *pThread)
{
    _ASSERTE(pThread != NULL);

    pThread->DisableSingleStep();
}

// Check if single stepping is enabled.
bool IsSSFlagEnabled(DT_CONTEXT *, Thread *pThread)
{
    _ASSERTE(pThread != NULL);

    return pThread->IsSingleStepEnabled();
}
