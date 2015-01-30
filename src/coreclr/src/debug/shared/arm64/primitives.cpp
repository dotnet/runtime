//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
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
        CopyContextChunk(&(pDst->Fp), &(pSrc->Fp), &(pDst->V),
                         DT_CONTEXT_CONTROL);
        CopyContextChunk(&(pDst->Cpsr), &(pSrc->Cpsr), &(pDst->X),
                         DT_CONTEXT_CONTROL);
    }
    
    if ((dstFlags & srcFlags & DT_CONTEXT_INTEGER) == DT_CONTEXT_INTEGER)
        CopyContextChunk(&(pDst->X[0]), &(pSrc->X[0]), &(pDst->Fp),
                         DT_CONTEXT_INTEGER);

    if ((dstFlags & srcFlags & DT_CONTEXT_FLOATING_POINT) == DT_CONTEXT_FLOATING_POINT)
        CopyContextChunk(&(pDst->V[0]), &(pSrc->V[0]), &(pDst->Bcr[0]),
                         DT_CONTEXT_FLOATING_POINT);

    if ((dstFlags & srcFlags & DT_CONTEXT_DEBUG_REGISTERS) ==
        DT_CONTEXT_DEBUG_REGISTERS)
        CopyContextChunk(&(pDst->Bcr[0]), &(pSrc->Bcr[0]), &(pDst->Wvr[ARM64_MAX_WATCHPOINTS]),
                         DT_CONTEXT_DEBUG_REGISTERS);
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
        pDRD->LR = (SIZE_T)pContext->Lr;
        pDRD->PC = (SIZE_T)pContext->Pc;
    }

    if ((flags & DT_CONTEXT_INTEGER) == DT_CONTEXT_INTEGER)
    {
        for(int i = 0 ; i < 29 ; i++)
        {
           pDRD->X[i] = (SIZE_T)pContext->X[i];
        }
    }

    pDRD->SP   = pRD->SP;

    LOG( (LF_CORDB, LL_INFO1000, "DT::TASSC:Registers:"
          "SP = %x",
          pDRD->SP) );
}
#endif // ALLOW_VMPTR_ACCESS || !RIGHT_SIDE_COMPILE


