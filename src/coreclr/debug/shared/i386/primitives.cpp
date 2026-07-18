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
    // The buffers must at least be large enough to hold the ContextFlags field, which
    // selects how much of the rest of the CONTEXT the copy below reads/writes.
    _ASSERTE(cbDst >= offsetof(DT_CONTEXT, ContextFlags) + sizeof(DWORD));
    _ASSERTE(cbSrc >= offsetof(DT_CONTEXT, ContextFlags) + sizeof(DWORD));

    DT_CONTEXT* pDst = reinterpret_cast<DT_CONTEXT*>(pDstBuffer);
    const DT_CONTEXT* pSrc = reinterpret_cast<const DT_CONTEXT*>(pSrcBuffer);

    DWORD dstFlags = pDst->ContextFlags;
    DWORD srcFlags = pSrc->ContextFlags;

    // The copy only touches the trailing ExtendedRegisters region when
    // CONTEXT_EXTENDED_REGISTERS is set, so a smaller CONTEXT buffer that omits that
    // array (e.g. CONTEXT_FULL without extended registers) is valid.
    _ASSERTE(cbDst >= (((dstFlags & CONTEXT_EXTENDED_REGISTERS) == CONTEXT_EXTENDED_REGISTERS)
                           ? sizeof(DT_CONTEXT) : offsetof(DT_CONTEXT, ExtendedRegisters)));
    _ASSERTE(cbSrc >= (((srcFlags & CONTEXT_EXTENDED_REGISTERS) == CONTEXT_EXTENDED_REGISTERS)
                           ? sizeof(DT_CONTEXT) : offsetof(DT_CONTEXT, ExtendedRegisters)));

    LOG((LF_CORDB, LL_INFO1000000,
         "CP::CTC: pDst=0x%08x dstFlags=0x%x, pSrc=0x%08x srcFlags=0x%x\n",
         pDst, dstFlags, pSrc, srcFlags));

    if ((dstFlags & srcFlags & CONTEXT_CONTROL) == CONTEXT_CONTROL)
        CopyContextChunk(&(pDst->Ebp), &(pSrc->Ebp), pDst->ExtendedRegisters,
                         CONTEXT_CONTROL);

    if ((dstFlags & srcFlags & CONTEXT_INTEGER) == CONTEXT_INTEGER)
        CopyContextChunk(&(pDst->Edi), &(pSrc->Edi), &(pDst->Ebp),
                         CONTEXT_INTEGER);

    if ((dstFlags & srcFlags & CONTEXT_SEGMENTS) == CONTEXT_SEGMENTS)
        CopyContextChunk(&(pDst->SegGs), &(pSrc->SegGs), &(pDst->Edi),
                         CONTEXT_SEGMENTS);

    if ((dstFlags & srcFlags & CONTEXT_FLOATING_POINT) == CONTEXT_FLOATING_POINT)
        CopyContextChunk(&(pDst->FloatSave), &(pSrc->FloatSave),
                         (&pDst->FloatSave)+1,
                         CONTEXT_FLOATING_POINT);

    if ((dstFlags & srcFlags & CONTEXT_DEBUG_REGISTERS) ==
        CONTEXT_DEBUG_REGISTERS)
        CopyContextChunk(&(pDst->Dr0), &(pSrc->Dr0), &(pDst->FloatSave),
                         CONTEXT_DEBUG_REGISTERS);

    if ((dstFlags & srcFlags & CONTEXT_EXTENDED_REGISTERS) ==
        CONTEXT_EXTENDED_REGISTERS)
        CopyContextChunk(pDst->ExtendedRegisters,
                         pSrc->ExtendedRegisters,
                         &(pDst->ExtendedRegisters[MAXIMUM_SUPPORTED_EXTENSION]),
                         CONTEXT_EXTENDED_REGISTERS);
}
