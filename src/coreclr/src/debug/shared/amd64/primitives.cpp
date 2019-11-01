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
    ULONG dstFlags = pDst->ContextFlags;
    ULONG srcFlags = pSrc->ContextFlags;
    LOG((LF_CORDB, LL_INFO1000000,
         "CP::CTC: pDst=0x%p dstFlags=0x%x, pSrc=0x%px srcFlags=0x%x\n",
         pDst, dstFlags, pSrc, srcFlags));

    // Unlike on X86 the AMD64 CONTEXT struct isn't nicely defined
    // to facilitate copying sections based on the CONTEXT flags.
    if ((dstFlags & srcFlags & CONTEXT_CONTROL) == CONTEXT_CONTROL)
    {
        // SegCs
        CopyContextChunk(&(pDst->SegCs), &(pSrc->SegCs), &(pDst->SegDs),
                         CONTEXT_CONTROL);
        // SegSs, EFlags
        CopyContextChunk(&(pDst->SegSs), &(pSrc->SegSs), &(pDst->Dr0),
                         CONTEXT_CONTROL);
        // Rsp
        CopyContextChunk(&(pDst->Rsp), &(pSrc->Rsp), &(pDst->Rbp),
                         CONTEXT_CONTROL);
        // Rip
        CopyContextChunk(&(pDst->Rip), &(pSrc->Rip), &(pDst->Xmm0),
                         CONTEXT_CONTROL);
    }
    
    if ((dstFlags & srcFlags & CONTEXT_INTEGER) == CONTEXT_INTEGER)
    {
        // Rax, Rcx, Rdx, Rbx
        CopyContextChunk(&(pDst->Rax), &(pSrc->Rax), &(pDst->Rsp),
                         CONTEXT_INTEGER);
        // Rbp, Rsi, Rdi, R8-R15
        CopyContextChunk(&(pDst->Rbp), &(pSrc->Rbp), &(pDst->Rip),
                         CONTEXT_INTEGET);
    }

    if ((dstFlags & srcFlags & CONTEXT_SEGMENTS) == CONTEXT_SEGMENTS)
    {
        // SegDs, SegEs, SegFs, SegGs
        CopyContextChunk(&(pDst->SegDs), &(pSrc->SegDs), &(pDst->SegSs),
                     CONTEXT_SEGMENTS);
    }
    
    if ((dstFlags & srcFlags & CONTEXT_FLOATING_POINT) == CONTEXT_FLOATING_POINT)
    {
        // Xmm0-Xmm15
        CopyContextChunk(&(pDst->Xmm0), &(pSrc->Xmm0), &(pDst->Xmm15) + 1,
                         CONTEXT_FLOATING_POINT);

        // MxCsr
        CopyContextChunk(&(pDst->MxCsr), &(pSrc->MxCsr), &(pDst->SegCs),
            CONTEXT_FLOATING_POINT);
    }
    
    if ((dstFlags & srcFlags & CONTEXT_DEBUG_REGISTERS) == CONTEXT_DEBUG_REGISTERS)
    {
        // Dr0-Dr3, Dr6-Dr7
        CopyContextChunk(&(pDst->Dr0), &(pSrc->Dr0), &(pDst->Rax),
                         CONTEXT_DEBUG_REGISTERS);
    }    
}

void CORDbgSetDebuggerREGDISPLAYFromContext(DebuggerREGDISPLAY* pDRD, 
                                            DT_CONTEXT* pContext)
{
    DWORD flags = pContext->ContextFlags;
    if ((flags & DT_CONTEXT_CONTROL) == DT_CONTEXT_CONTROL)
    {   
        pDRD->PC = (SIZE_T)CORDbgGetIP(pContext);
        pDRD->SP = (SIZE_T)CORDbgGetSP(pContext);
    }
    
    if ((flags & DT_CONTEXT_INTEGER) == DT_CONTEXT_INTEGER)
    {
        pDRD->Rax = pContext->Rax;
        pDRD->Rcx = pContext->Rcx;
        pDRD->Rdx = pContext->Rdx;
        pDRD->Rbx = pContext->Rbx;
        pDRD->Rbp = pContext->Rbp;
        pDRD->Rsi = pContext->Rsi;
        pDRD->Rdi = pContext->Rdi;
        pDRD->R8  = pContext->R8;
        pDRD->R9  = pContext->R9;
        pDRD->R10 = pContext->R10;
        pDRD->R11 = pContext->R11;
        pDRD->R12 = pContext->R12;
        pDRD->R13 = pContext->R13;
        pDRD->R14 = pContext->R14;
        pDRD->R15 = pContext->R15;
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
    // This doesn't set the pointers, only the values
    CORDbgSetDebuggerREGDISPLAYFromContext(pDRD, reinterpret_cast<DT_CONTEXT *>(pRD->pCurrentContext)); // MACTODO: KLUDGE UNDO Microsoft
    pRD->pCurrentContext->ContextFlags = contextFlags;

    // These pointers are always valid so we don't need to test them using PushedRegAddr
#if defined(USE_REMOTE_REGISTER_ADDRESS)
    pDRD->pRax  = pRD->pCurrentContextPointers->Integer.Register.Rax;
    pDRD->pRcx  = pRD->pCurrentContextPointers->Integer.Register.Rcx;
    pDRD->pRdx  = pRD->pCurrentContextPointers->Integer.Register.Rdx;
    pDRD->pRbx  = pRD->pCurrentContextPointers->Integer.Register.Rbx;
    pDRD->pRbp  = pRD->pCurrentContextPointers->Integer.Register.Rbp;
    pDRD->pRsi  = pRD->pCurrentContextPointers->Integer.Register.Rsi;
    pDRD->pRdi  = pRD->pCurrentContextPointers->Integer.Register.Rdi;
    pDRD->pR8   = pRD->pCurrentContextPointers->Integer.Register.R8;
    pDRD->pR9   = pRD->pCurrentContextPointers->Integer.Register.R9;
    pDRD->pR10  = pRD->pCurrentContextPointers->Integer.Register.R10;
    pDRD->pR11  = pRD->pCurrentContextPointers->Integer.Register.R11;
    pDRD->pR12  = pRD->pCurrentContextPointers->Integer.Register.R12;
    pDRD->pR13  = pRD->pCurrentContextPointers->Integer.Register.R13;
    pDRD->pR14  = pRD->pCurrentContextPointers->Integer.Register.R14;
    pDRD->pR15  = pRD->pCurrentContextPointers->Integer.Register.R15;
#else  // !USE_REMOTE_REGISTER_ADDRESS
    pDRD->pRax  = NULL;
    pDRD->pRcx  = NULL;
    pDRD->pRdx  = NULL;
    pDRD->pRbx  = NULL;
    pDRD->pRbp  = NULL;
    pDRD->pRsi  = NULL;
    pDRD->pRdi  = NULL;
    pDRD->pR8   = NULL;
    pDRD->pR9   = NULL;
    pDRD->pR10  = NULL;
    pDRD->pR11  = NULL;
    pDRD->pR12  = NULL;
    pDRD->pR13  = NULL;
    pDRD->pR14  = NULL;
    pDRD->pR15  = NULL;
#endif // USE_REMOTE_REGISTER_ADDRESS
    
    pDRD->PC    = pRD->ControlPC;
    pDRD->SP    = pRD->SP;

    // Please leave RSP, RIP at the front so I don't have to scroll
    // left to see the most important registers.  Thanks!
    LOG( (LF_CORDB, LL_INFO1000, "SDRFR:Registers:"
          "Rsp = %p   Rip = %p   Rbp = %p   Rdi = %p   "
          "Rsi = %p   Rbx = %p   Rdx = %p   Rcx = %p   Rax = %p"
          "R8  = %p   R9  = %p   R10 = %p   R11 = %p"
          "R12 = %p   R13 = %p   R14 = %p   R15 = %p\n",
          pDRD->SP, pDRD->PC,  pDRD->Rbp,  pDRD->Rdi,
          pDRD->Rsi, pDRD->Rbx, pDRD->Rdx, pDRD->Rcx, pDRD->Rax,
          pDRD->R8,  pDRD->R9,  pDRD->R10, pDRD->R11, pDRD->R12,
          pDRD->R13, pDRD->R14, pDRD->R15) );

}
#endif // ALLOW_VMPTR_ACCESS || !RIGHT_SIDE_COMPILE
