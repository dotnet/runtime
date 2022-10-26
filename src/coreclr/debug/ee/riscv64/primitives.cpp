// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

#include "stdafx.h"
#include "threads.h"
#include "../../shared/riscv64/primitives.cpp"

// #error "TODO-RISCV64: missing implementation"
void CopyREGDISPLAY(REGDISPLAY* pDst, REGDISPLAY* pSrc)
{
    CONTEXT tmp;
    CopyRegDisplay(pSrc, pDst, &tmp);
}
