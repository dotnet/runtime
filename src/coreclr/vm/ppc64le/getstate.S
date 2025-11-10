// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"
#include "asmconstants.h"

LEAF_ENTRY GetCurrentSP, _TEXT
	mr %r3, %r1
	blr
LEAF_END GetCurrentSP, _TEXT

LEAF_ENTRY GetCurrentIP, _TEXT
	mflr %r3
	blr
LEAF_END GetCurrentIP, _TEXT

// EXTERN_C void LazyMachStateCaptureState(struct LazyMachState *pState)
LEAF_ENTRY LazyMachStateCaptureState, _TEXT
	std %r14, OFFSETOF__MachState__m_Capture(%r3)
	std %r15, OFFSETOF__MachState__m_Capture + 8(%r3)
	std %r16, OFFSETOF__MachState__m_Capture + 16(%r3)
	std %r17, OFFSETOF__MachState__m_Capture + 24(%r3)
	std %r18, OFFSETOF__MachState__m_Capture + 32(%r3)
	std %r19, OFFSETOF__MachState__m_Capture + 40(%r3)
	std %r20, OFFSETOF__MachState__m_Capture + 48(%r3)
	std %r21, OFFSETOF__MachState__m_Capture + 56(%r3)
	std %r22, OFFSETOF__MachState__m_Capture + 64(%r3)
	std %r23, OFFSETOF__MachState__m_Capture + 72(%r3)
	std %r24, OFFSETOF__MachState__m_Capture + 80(%r3)
	std %r25, OFFSETOF__MachState__m_Capture + 88(%r3)
	std %r26, OFFSETOF__MachState__m_Capture + 96(%r3)
	std %r27, OFFSETOF__MachState__m_Capture + 104(%r3)
	std %r28, OFFSETOF__MachState__m_Capture + 112(%r3)
	std %r29, OFFSETOF__MachState__m_Capture + 120(%r3)
	std %r30, OFFSETOF__MachState__m_Capture + 128(%r3)
	std %r31, OFFSETOF__MachState__m_Capture + 136(%r3)

	std %r1, OFFSETOF__LazyMachState__m_CaptureSp(%r3)
	mflr %r0
	std %r0, OFFSETOF__LazyMachState__m_CaptureNip(%r3)	

	li %r0, 0
	std %r0, OFFSETOF__MachState___pRetAddr(%r3)

	blr
LEAF_END LazyMachStateCaptureState, _TEXT
