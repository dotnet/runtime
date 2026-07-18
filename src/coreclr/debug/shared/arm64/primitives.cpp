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


//
// CopyThreadContext() does an intelligent copy from pSrc to pDst,
// respecting the ContextFlags of both contexts.
//
void CORDbgCopyThreadContext(BYTE* pDstBuffer, ULONG32 cbDst, const BYTE* pSrcBuffer, ULONG32 cbSrc)
{
    _ASSERTE(cbDst >= sizeof(DT_CONTEXT));
    _ASSERTE(cbSrc >= sizeof(DT_CONTEXT));

    DT_CONTEXT* pDst = reinterpret_cast<DT_CONTEXT*>(pDstBuffer);
    const DT_CONTEXT* pSrc = reinterpret_cast<const DT_CONTEXT*>(pSrcBuffer);

    DWORD dstFlags = pDst->ContextFlags;
    DWORD srcFlags = pSrc->ContextFlags;
    LOG((LF_CORDB, LL_INFO1000000,
         "CP::CTC: pDst=0x%08x dstFlags=0x%x, pSrc=0x%08x srcFlags=0x%x\n",
         pDst, dstFlags, pSrc, srcFlags));

    if ((dstFlags & srcFlags & CONTEXT_CONTROL) == CONTEXT_CONTROL)
    {
        CopyContextChunk(&(pDst->Fp), &(pSrc->Fp), &(pDst->V),
                         CONTEXT_CONTROL);
        CopyContextChunk(&(pDst->Cpsr), &(pSrc->Cpsr), &(pDst->X),
                         CONTEXT_CONTROL);
    }

    if ((dstFlags & srcFlags & CONTEXT_INTEGER) == CONTEXT_INTEGER)
        CopyContextChunk(&(pDst->X[0]), &(pSrc->X[0]), &(pDst->Fp),
                         CONTEXT_INTEGER);

    if ((dstFlags & srcFlags & CONTEXT_FLOATING_POINT) == CONTEXT_FLOATING_POINT)
        CopyContextChunk(&(pDst->V[0]), &(pSrc->V[0]), &(pDst->Bcr[0]),
                         CONTEXT_FLOATING_POINT);

    if ((dstFlags & srcFlags & CONTEXT_DEBUG_REGISTERS) ==
        CONTEXT_DEBUG_REGISTERS)
        CopyContextChunk(&(pDst->Bcr[0]), &(pSrc->Bcr[0]), &(pDst->Wvr[ARM64_MAX_WATCHPOINTS]),
                         CONTEXT_DEBUG_REGISTERS);
}
