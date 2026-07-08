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
void CORDbgCopyThreadContext(T_CONTEXT* pDst, const T_CONTEXT* pSrc)
{
    DWORD dstFlags = pDst->ContextFlags;
    DWORD srcFlags = pSrc->ContextFlags;
    LOG((LF_CORDB, LL_INFO1000000,
         "CP::CTC: pDst=0x%08x dstFlags=0x%x, pSrc=0x%08x srcFlags=0x%x\n",
         pDst, dstFlags, pSrc, srcFlags));

    if ((dstFlags & srcFlags & CONTEXT_CONTROL) == CONTEXT_CONTROL)
    {
        LOG((LF_CORDB, LL_INFO1000000,
             "CP::CTC: RA: pDst=0x%lx, pSrc=0x%lx, Flags=0x%x\n",
             pDst->Ra, pSrc->Ra, CONTEXT_CONTROL));
        pDst->Ra = pSrc->Ra;

        LOG((LF_CORDB, LL_INFO1000000,
             "CP::CTC: SP: pDst=0x%lx, pSrc=0x%lx, Flags=0x%x\n",
             pDst->Sp, pSrc->Sp, CONTEXT_CONTROL));
        pDst->Sp = pSrc->Sp;

        LOG((LF_CORDB, LL_INFO1000000,
             "CP::CTC: FP: pDst=0x%lx, pSrc=0x%lx, Flags=0x%x\n",
             pDst->Fp, pSrc->Fp, CONTEXT_CONTROL));
        pDst->Fp = pSrc->Fp;

        LOG((LF_CORDB, LL_INFO1000000,
             "CP::CTC: PC: pDst=0x%lx, pSrc=0x%lx, Flags=0x%x\n",
             pDst->Pc, pSrc->Pc, CONTEXT_CONTROL));
        pDst->Pc = pSrc->Pc;
    }

    if ((dstFlags & srcFlags & CONTEXT_INTEGER) == CONTEXT_INTEGER)
    {
        CopyContextChunk(&pDst->Gp, &pSrc->Gp, &pDst->Fp,
                         CONTEXT_INTEGER);
        CopyContextChunk(&pDst->S1, &pSrc->S1, &pDst->Pc,
                         CONTEXT_INTEGER);
        LOG((LF_CORDB, LL_INFO1000000,
             "CP::CTC: T0: pDst=0x%lx, pSrc=0x%lx, Flags=0x%x\n",
             pDst->R0, pSrc->R0, CONTEXT_INTEGER));
        pDst->R0 = pSrc->R0;
    }

    if ((dstFlags & srcFlags & CONTEXT_FLOATING_POINT) == CONTEXT_FLOATING_POINT)
    {
        CopyContextChunk(&pDst->F[0], &pSrc->F[0], &pDst->F[32],
                         CONTEXT_FLOATING_POINT);
        pDst->Fcsr = pSrc->Fcsr;
    }
}
