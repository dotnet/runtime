//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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
