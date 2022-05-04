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
void CORDbgCopyThreadContext(DT_CONTEXT* pDst, const DT_CONTEXT* pSrc)
{
    DWORD dstFlags = pDst->ContextFlags;
    DWORD srcFlags = pSrc->ContextFlags;
    LOG((LF_CORDB, LL_INFO1000000,
         "CP::CTC: pDst=0x%08x dstFlags=0x%x, pSrc=0x%08x srcFlags=0x%x\n",
         pDst, dstFlags, pSrc, srcFlags));

    if ((dstFlags & srcFlags & DT_CONTEXT_CONTROL) == DT_CONTEXT_CONTROL)
    {
        LOG((LF_CORDB, LL_INFO1000000,
             "CP::CTC: RA: pDst=0x%lx, pSrc=0x%lx, Flags=0x%x\n",
             pDst->RA, pSrc->RA, DT_CONTEXT_CONTROL));
        pDst->RA = pSrc->RA;

        LOG((LF_CORDB, LL_INFO1000000,
             "CP::CTC: SP: pDst=0x%lx, pSrc=0x%lx, Flags=0x%x\n",
             pDst->SP, pSrc->SP, DT_CONTEXT_CONTROL));
        pDst->SP = pSrc->SP;

        LOG((LF_CORDB, LL_INFO1000000,
             "CP::CTC: FP: pDst=0x%lx, pSrc=0x%lx, Flags=0x%x\n",
             pDst->FP, pSrc->FP, DT_CONTEXT_CONTROL));
        pDst->FP = pSrc->FP;

        LOG((LF_CORDB, LL_INFO1000000,
             "CP::CTC: PC: pDst=0x%lx, pSrc=0x%lx, Flags=0x%x\n",
             pDst->PC, pSrc->PC, DT_CONTEXT_CONTROL));
        pDst->PC = pSrc->PC;
    }

    if ((dstFlags & srcFlags & DT_CONTEXT_INTEGER) == DT_CONTEXT_INTEGER)
    {
        CopyContextChunk(&pDst->A0, &pSrc->A0, &pDst->FP,
                         DT_CONTEXT_INTEGER);
        CopyContextChunk(&pDst->S0, &pSrc->S0, &pDst->PC,
                         DT_CONTEXT_INTEGER);
    }

    if ((dstFlags & srcFlags & DT_CONTEXT_FLOATING_POINT) == DT_CONTEXT_FLOATING_POINT)
    {
        CopyContextChunk(&pDst->F[0], &pSrc->F[0], &pDst->F[32],
                         DT_CONTEXT_FLOATING_POINT);
    }
}

#if defined(ALLOW_VMPTR_ACCESS) || !defined(RIGHT_SIDE_COMPILE)
void SetDebuggerREGDISPLAYFromREGDISPLAY(DebuggerREGDISPLAY* pDRD, REGDISPLAY* pRD)
{
    SUPPORTS_DAC_HOST_ONLY;

    DT_CONTEXT* pContext = reinterpret_cast<DT_CONTEXT*>(pRD->pCurrentContext);

    // We must pay attention to the context flags so that we only use valid portions
    // of the context.
    DWORD flags = pContext->ContextFlags;
    if ((flags & DT_CONTEXT_CONTROL) == DT_CONTEXT_CONTROL)
    {
        pDRD->FP = (SIZE_T)CORDbgGetFP(pContext);
        pDRD->PC = (SIZE_T)pContext->PC;
        pDRD->RA = (SIZE_T)pContext->RA;
    }

    if ((flags & DT_CONTEXT_INTEGER) == DT_CONTEXT_INTEGER)
    {
        pDRD->TP = pContext->TP;
        memcpy(&pDRD->A0, &pContext->A0, sizeof(pDRD->A0)*(21 - 4 + 1));
        memcpy(&pDRD->S0, &pContext->S0, sizeof(pDRD->S0)* 9);
    }

    pDRD->SP   = pRD->SP;

    LOG( (LF_CORDB, LL_INFO1000, "DT::TASSC:Registers:"
          "SP = %x",
          pDRD->SP) );
}
#endif // ALLOW_VMPTR_ACCESS || !RIGHT_SIDE_COMPILE
