// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// This file is used by AsmOffsets.h to validate that our
// assembly-code offsets always match their C++ counterparts.
//
// NOTE: the offsets MUST be in hex notation WITHOUT the 0x prefix

PLAT_ASM_SIZEOF(c0, ExInfo)
PLAT_ASM_OFFSET(0, ExInfo, m_pPrevExInfo)
PLAT_ASM_OFFSET(4, ExInfo, m_pExContext)
PLAT_ASM_OFFSET(8, ExInfo, m_exception)
PLAT_ASM_OFFSET(0c, ExInfo, m_kind)
PLAT_ASM_OFFSET(0d, ExInfo, m_passNumber)
PLAT_ASM_OFFSET(10, ExInfo, m_idxCurClause)
PLAT_ASM_OFFSET(14, ExInfo, m_frameIter)
PLAT_ASM_OFFSET(bc, ExInfo, m_notifyDebuggerSP)

PLAT_ASM_OFFSET(0, PInvokeTransitionFrame, m_RIP)
PLAT_ASM_OFFSET(4, PInvokeTransitionFrame, m_FramePointer)
PLAT_ASM_OFFSET(8, PInvokeTransitionFrame, m_pThread)
PLAT_ASM_OFFSET(0c, PInvokeTransitionFrame, m_Flags)
PLAT_ASM_OFFSET(10, PInvokeTransitionFrame, m_PreservedRegs)

PLAT_ASM_SIZEOF(a8, StackFrameIterator)
PLAT_ASM_OFFSET(08, StackFrameIterator, m_FramePointer)
PLAT_ASM_OFFSET(0c, StackFrameIterator, m_ControlPC)
PLAT_ASM_OFFSET(10, StackFrameIterator, m_RegDisplay)
PLAT_ASM_OFFSET(a0, StackFrameIterator, m_OriginalControlPC)
PLAT_ASM_OFFSET(a4, StackFrameIterator, m_pPreviousTransitionFrame)

PLAT_ASM_SIZEOF(1c, PAL_LIMITED_CONTEXT)
PLAT_ASM_OFFSET(0, PAL_LIMITED_CONTEXT, IP)

PLAT_ASM_OFFSET(4, PAL_LIMITED_CONTEXT, Rsp)
PLAT_ASM_OFFSET(8, PAL_LIMITED_CONTEXT, Rbp)
PLAT_ASM_OFFSET(0c, PAL_LIMITED_CONTEXT, Rdi)
PLAT_ASM_OFFSET(10, PAL_LIMITED_CONTEXT, Rsi)
PLAT_ASM_OFFSET(14, PAL_LIMITED_CONTEXT, Rax)
PLAT_ASM_OFFSET(18, PAL_LIMITED_CONTEXT, Rbx)

PLAT_ASM_SIZEOF(24, REGDISPLAY)
PLAT_ASM_OFFSET(1c, REGDISPLAY, SP)

PLAT_ASM_OFFSET(0c, REGDISPLAY, pRbx)
PLAT_ASM_OFFSET(10, REGDISPLAY, pRbp)
PLAT_ASM_OFFSET(14, REGDISPLAY, pRsi)
PLAT_ASM_OFFSET(18, REGDISPLAY, pRdi)
