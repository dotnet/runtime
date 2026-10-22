// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// This file is used by AsmOffsets.h to validate that our
// assembly-code offsets always match their C++ counterparts.
//
// NOTE: the offsets MUST be in hex notation WITHOUT the 0x prefix

PLAT_ASM_SIZEOF(9c, ExInfo)
PLAT_ASM_OFFSET(0, ExInfo, m_pPrevExInfo)
PLAT_ASM_OFFSET(4, ExInfo, m_pExContext)
PLAT_ASM_OFFSET(8, ExInfo, m_exception)
PLAT_ASM_OFFSET(0c, ExInfo, m_kind)
PLAT_ASM_OFFSET(0d, ExInfo, m_passNumber)
PLAT_ASM_OFFSET(10, ExInfo, m_idxCurClause)
PLAT_ASM_OFFSET(14, ExInfo, m_frameIter)
PLAT_ASM_OFFSET(98, ExInfo, m_notifyDebuggerSP)

PLAT_ASM_SIZEOF(84, StackFrameIterator)
PLAT_ASM_OFFSET(08, StackFrameIterator, m_FramePointer)
PLAT_ASM_OFFSET(0c, StackFrameIterator, m_ControlPC)
PLAT_ASM_OFFSET(10, StackFrameIterator, m_RegDisplay)
PLAT_ASM_OFFSET(7c, StackFrameIterator, m_OriginalControlPC)
PLAT_ASM_OFFSET(80, StackFrameIterator, m_pPreviousTransitionFrame)

PLAT_ASM_SIZEOF(4, PAL_LIMITED_CONTEXT)
PLAT_ASM_OFFSET(0, PAL_LIMITED_CONTEXT, IP)

PLAT_ASM_SIZEOF(0c, REGDISPLAY)
PLAT_ASM_OFFSET(0, REGDISPLAY, SP)
PLAT_ASM_OFFSET(4, REGDISPLAY, FP)
PLAT_ASM_OFFSET(8, REGDISPLAY, IP)
