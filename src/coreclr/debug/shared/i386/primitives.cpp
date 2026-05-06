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
        CopyContextChunk(&(pDst->Ebp), &(pSrc->Ebp), pDst->ExtendedRegisters,
                         DT_CONTEXT_CONTROL);

    if ((dstFlags & srcFlags & DT_CONTEXT_INTEGER) == DT_CONTEXT_INTEGER)
        CopyContextChunk(&(pDst->Edi), &(pSrc->Edi), &(pDst->Ebp),
                         DT_CONTEXT_INTEGER);

    if ((dstFlags & srcFlags & DT_CONTEXT_SEGMENTS) == DT_CONTEXT_SEGMENTS)
        CopyContextChunk(&(pDst->SegGs), &(pSrc->SegGs), &(pDst->Edi),
                         DT_CONTEXT_SEGMENTS);

    if ((dstFlags & srcFlags & DT_CONTEXT_FLOATING_POINT) == DT_CONTEXT_FLOATING_POINT)
        CopyContextChunk(&(pDst->FloatSave), &(pSrc->FloatSave),
                         (&pDst->FloatSave)+1,
                         DT_CONTEXT_FLOATING_POINT);

    if ((dstFlags & srcFlags & DT_CONTEXT_DEBUG_REGISTERS) ==
        DT_CONTEXT_DEBUG_REGISTERS)
        CopyContextChunk(&(pDst->Dr0), &(pSrc->Dr0), &(pDst->FloatSave),
                         DT_CONTEXT_DEBUG_REGISTERS);

    if ((dstFlags & srcFlags & DT_CONTEXT_EXTENDED_REGISTERS) ==
        DT_CONTEXT_EXTENDED_REGISTERS)
        CopyContextChunk(pDst->ExtendedRegisters,
                         pSrc->ExtendedRegisters,
                         &(pDst->ExtendedRegisters[MAXIMUM_SUPPORTED_EXTENSION]),
                         DT_CONTEXT_EXTENDED_REGISTERS);
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
        pDRD->FP = (SIZE_T)CORDbgGetFP(pContext);
    }

    if ((flags & DT_CONTEXT_INTEGER) == DT_CONTEXT_INTEGER)
    {
        pDRD->Eax = pContext->Eax;
        pDRD->Ebx = pContext->Ebx;
        pDRD->Ecx = pContext->Ecx;
        pDRD->Edx = pContext->Edx;
        pDRD->Esi = pContext->Esi;
        pDRD->Edi = pContext->Edi;
    }
}

#if defined(ALLOW_VMPTR_ACCESS) || !defined(RIGHT_SIDE_COMPILE)
void SetDebuggerREGDISPLAYFromREGDISPLAY(DebuggerREGDISPLAY* pDRD, REGDISPLAY* pRD)
{
    SUPPORTS_DAC_HOST_ONLY;
    // Frame pointer
    LPVOID FPAddress = GetRegdisplayFPAddress(pRD);
    pDRD->FP  = (FPAddress == NULL ? 0 : *((SIZE_T *)FPAddress));
    pDRD->Edi = (pRD->GetEdiLocation() == NULL ? 0 : *pRD->GetEdiLocation());
    pDRD->Esi = (pRD->GetEsiLocation() == NULL ? 0 : *pRD->GetEsiLocation());
    pDRD->Ebx = (pRD->GetEbxLocation() == NULL ? 0 : *pRD->GetEbxLocation());
    pDRD->Edx = (pRD->GetEdxLocation() == NULL ? 0 : *pRD->GetEdxLocation());
    pDRD->Ecx = (pRD->GetEcxLocation() == NULL ? 0 : *pRD->GetEcxLocation());
    pDRD->Eax = (pRD->GetEaxLocation() == NULL ? 0 : *pRD->GetEaxLocation());

#if defined(USE_REMOTE_REGISTER_ADDRESS)
    pDRD->pFP = PushedRegAddr(pRD, FPAddress);
    pDRD->pEdi = PushedRegAddr(pRD, pRD->pEdi);
    pDRD->pEsi = PushedRegAddr(pRD, pRD->pEsi);
    pDRD->pEbx = PushedRegAddr(pRD, pRD->pEbx);
    pDRD->pEdx = PushedRegAddr(pRD, pRD->pEdx);
    pDRD->pEcx = PushedRegAddr(pRD, pRD->pEcx);
    pDRD->pEax = PushedRegAddr(pRD, pRD->pEax);
#else  // !USE_REMOTE_REGISTER_ADDRESS
    pDRD->pFP = NULL;
    pDRD->pEdi = NULL;
    pDRD->pEsi = NULL;
    pDRD->pEbx = NULL;
    pDRD->pEdx = NULL;
    pDRD->pEcx = NULL;
    pDRD->pEax = NULL;
#endif // !USE_REMOTE_REGISTER_ADDRESS

    pDRD->SP   = pRD->SP;
    pDRD->PC   = pRD->ControlPC;

    // Please leave EBP, ESP, EIP at the front so I don't have to scroll
    // left to see the most important registers.  Thanks!
    LOG( (LF_CORDB, LL_INFO1000, "DT::TASSC:Registers:"
          "Ebp = %x   Esp = %x   Eip = %x   Edi:%d   "
          "Esi = %x   Ebx = %x   Edx = %x   Ecx = %x   Eax = %x\n",
          pDRD->FP, pDRD->SP, pDRD->PC, pDRD->Edi,
          pDRD->Esi, pDRD->Ebx, pDRD->Edx, pDRD->Ecx, pDRD->Eax ) );
}
#endif // ALLOW_VMPTR_ACCESS || !RIGHT_SIDE_COMPILE
