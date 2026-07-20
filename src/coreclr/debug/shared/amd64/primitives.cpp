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
