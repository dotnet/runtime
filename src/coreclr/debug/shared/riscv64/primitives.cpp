// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//*****************************************************************************
// File: primitives.cpp
//

//
// Platform-specific debugger primitives
//
//*****************************************************************************

#include "primitives.h"

// CopyThreadContext() does an intelligent copy from pSrc to pDst,
// respecting the ContextFlags of both contexts.
//
void CORDbgCopyThreadContext(DT_CONTEXT* pDst, const DT_CONTEXT* pSrc)
{
    _ASSERTE(!"RISCV64:NYI");
}

#if defined(ALLOW_VMPTR_ACCESS) || !defined(RIGHT_SIDE_COMPILE)
void SetDebuggerREGDISPLAYFromREGDISPLAY(DebuggerREGDISPLAY* pDRD, REGDISPLAY* pRD)
{
    _ASSERTE(!"RISCV64:NYI");
}
#endif // ALLOW_VMPTR_ACCESS || !RIGHT_SIDE_COMPILE
