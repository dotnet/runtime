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
