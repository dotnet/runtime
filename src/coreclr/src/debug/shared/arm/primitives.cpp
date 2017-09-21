// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
        CopyContextChunk(&(pDst->Sp), &(pSrc->Sp), &(pDst->Fpscr),
                         DT_CONTEXT_CONTROL);
    
    if ((dstFlags & srcFlags & DT_CONTEXT_INTEGER) == DT_CONTEXT_INTEGER)
        CopyContextChunk(&(pDst->R0), &(pSrc->R0), &(pDst->Sp),
                         DT_CONTEXT_INTEGER);

    if ((dstFlags & srcFlags & DT_CONTEXT_FLOATING_POINT) == DT_CONTEXT_FLOATING_POINT)
        CopyContextChunk(&(pDst->Fpscr), &(pSrc->Fpscr), &(pDst->Bvr[0]),
                         DT_CONTEXT_FLOATING_POINT);

    if ((dstFlags & srcFlags & DT_CONTEXT_DEBUG_REGISTERS) ==
        DT_CONTEXT_DEBUG_REGISTERS)
        CopyContextChunk(&(pDst->Bvr[0]), &(pSrc->Bvr[0]), &(pDst->Wcr[DT_ARM_MAX_WATCHPOINTS]),
                         DT_CONTEXT_DEBUG_REGISTERS);
}


// Update the regdisplay from a given context. 
void CORDbgSetDebuggerREGDISPLAYFromContext(DebuggerREGDISPLAY *pDRD, 
                                            DT_CONTEXT* pContext)
{
    // We must pay attention to the context flags so that we only use valid portions
    // of the context.
    DWORD flags = pContext->ContextFlags;
    if ((flags & DT_CONTEXT_CONTROL) == DT_CONTEXT_CONTROL)
    {    
        pDRD->PC = (SIZE_T)CORDbgGetIP(pContext);
        pDRD->SP = (SIZE_T)CORDbgGetSP(pContext);
        pDRD->LR = (SIZE_T)pContext->Lr;
    }

    if ((flags & DT_CONTEXT_INTEGER) == DT_CONTEXT_INTEGER)
    {
        pDRD->R0 = (SIZE_T)pContext->R0;
        pDRD->R1 = (SIZE_T)pContext->R1;
        pDRD->R2 = (SIZE_T)pContext->R2;
        pDRD->R3 = (SIZE_T)pContext->R3;
        pDRD->R4 = (SIZE_T)pContext->R4;
        pDRD->R5 = (SIZE_T)pContext->R5;
        pDRD->R6 = (SIZE_T)pContext->R6;
        pDRD->R7 = (SIZE_T)pContext->R7;
        pDRD->R8 = (SIZE_T)pContext->R8;
        pDRD->R9 = (SIZE_T)pContext->R9;
        pDRD->R10 = (SIZE_T)pContext->R10;
        pDRD->R11 = (SIZE_T)pContext->R11;
        pDRD->R12 = (SIZE_T)pContext->R12;
    }
}

#if defined(ALLOW_VMPTR_ACCESS) || !defined(RIGHT_SIDE_COMPILE)
void SetDebuggerREGDISPLAYFromREGDISPLAY(DebuggerREGDISPLAY* pDRD, REGDISPLAY* pRD)
{
    SUPPORTS_DAC_HOST_ONLY;
    // CORDbgSetDebuggerREGDISPLAYFromContext() checks the context flags.  In cases where we don't have a filter
    // context from the thread, we initialize a CONTEXT on the stack and use that to do our stack walking.  We never
    // initialize the context flags in such cases.  Since this function is called from the stackwalker, we can
    // guarantee that the integer, control, and floating point sections are valid.  So we set the flags here and
    // restore them afterwards.
    DWORD contextFlags = pRD->pCurrentContext->ContextFlags;
    pRD->pCurrentContext->ContextFlags = CONTEXT_FULL;
    CORDbgSetDebuggerREGDISPLAYFromContext(pDRD, reinterpret_cast<DT_CONTEXT*>(pRD->pCurrentContext));
    pRD->pCurrentContext->ContextFlags = contextFlags;

    pDRD->SP   = pRD->SP;
    pDRD->PC   = (SIZE_T)*(pRD->pPC);

    LOG( (LF_CORDB, LL_INFO1000, "DT::TASSC:Registers:"
          "SP = %x   PC = %x",
          pDRD->SP, pDRD->PC) );
}
#endif // ALLOW_VMPTR_ACCESS || !RIGHT_SIDE_COMPILE
