// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Implementation of _CONTEXT_CaptureContext for the IBM power ppc64le platform.
// This function is processor dependent.  It is used by exception handling,
// and is always apply to the current thread.
//

#include "unixasmmacros.inc"
#include "asmconstants.h"

// Incoming:
//  R3: Context*
//
LEAF_ENTRY CONTEXT_CaptureContext, _TEXT

	// Store all general purpose registers
	std %r0, CONTEXT_R0(%r3)
	std %r1, CONTEXT_R1(%r3)
	std %r2, CONTEXT_R2(%r3)
	std %r3, CONTEXT_R3(%r3)
	std %r4, CONTEXT_R4(%r3)
	std %r5, CONTEXT_R5(%r3)
	std %r6, CONTEXT_R6(%r3)
	std %r7, CONTEXT_R7(%r3)
	std %r8, CONTEXT_R8(%r3)
	std %r9, CONTEXT_R9(%r3)
	std %r10, CONTEXT_R10(%r3)
	std %r11, CONTEXT_R11(%r3)
	std %r12, CONTEXT_R12(%r3)
	std %r13, CONTEXT_R13(%r3)
	std %r14, CONTEXT_R14(%r3)
	std %r15, CONTEXT_R15(%r3)
	std %r16, CONTEXT_R16(%r3)
	std %r17, CONTEXT_R17(%r3)
	std %r18, CONTEXT_R18(%r3)
	std %r19, CONTEXT_R19(%r3)
	std %r20, CONTEXT_R20(%r3)
	std %r21, CONTEXT_R21(%r3)
	std %r22, CONTEXT_R22(%r3)
	std %r23, CONTEXT_R23(%r3)
	std %r24, CONTEXT_R24(%r3)
	std %r25, CONTEXT_R25(%r3)
	std %r26, CONTEXT_R26(%r3)
	std %r27, CONTEXT_R27(%r3)
	std %r28, CONTEXT_R28(%r3)
	std %r29, CONTEXT_R29(%r3)
	std %r30, CONTEXT_R30(%r3)
	std %r31, CONTEXT_R31(%r3)

	// Store all floating point registers
	stfd %f0, CONTEXT_F0(%r3)
	stfd %f1, CONTEXT_F1(%r3)
	stfd %f2, CONTEXT_F2(%r3)
	stfd %f3, CONTEXT_F3(%r3)
	stfd %f4, CONTEXT_F4(%r3)
	stfd %f5, CONTEXT_F5(%r3)
	stfd %f6, CONTEXT_F6(%r3)
	stfd %f7, CONTEXT_F7(%r3)
	stfd %f8, CONTEXT_F8(%r3)
	stfd %f9, CONTEXT_F9(%r3)
	stfd %f10, CONTEXT_F10(%r3)
	stfd %f11, CONTEXT_F11(%r3)
	stfd %f12, CONTEXT_F12(%r3)
	stfd %f13, CONTEXT_F13(%r3)
	stfd %f14, CONTEXT_F14(%r3)
	stfd %f15, CONTEXT_F15(%r3)
	stfd %f16, CONTEXT_F16(%r3)
	stfd %f17, CONTEXT_F17(%r3)
	stfd %f18, CONTEXT_F18(%r3)
	stfd %f19, CONTEXT_F19(%r3)
	stfd %f20, CONTEXT_F20(%r3)
	stfd %f21, CONTEXT_F21(%r3)
	stfd %f22, CONTEXT_F22(%r3)
	stfd %f23, CONTEXT_F23(%r3)
	stfd %f24, CONTEXT_F24(%r3)
	stfd %f25, CONTEXT_F25(%r3)
	stfd %f26, CONTEXT_F26(%r3)
	stfd %f27, CONTEXT_F27(%r3)
	stfd %f28, CONTEXT_F28(%r3)
	stfd %f29, CONTEXT_F29(%r3)
	stfd %f30, CONTEXT_F30(%r3)
	stfd %f31, CONTEXT_F31(%r3)

	// Save Control Registers - XER, LR and CTR
	mfspr 5, 1
	std %r5, CONTEXT_XER(%r3)
	mflr %r5
	std %r5, CONTEXT_LINK(%r3)
	mfspr 5, 9
	std %r5, CONTEXT_CTR(%r3)

	// Restore r5 general purpose register
	ld %r5, CONTEXT_R5(%r3)
	
	blr
LEAF_END CONTEXT_CaptureContext, _TEXT

LEAF_ENTRY RtlCaptureContext, _TEXT
	b C_FUNC(CONTEXT_CaptureContext)
LEAF_END RtlCaptureContext, _TEXT

LEAF_ENTRY RtlRestoreContext, _TEXT

	// Restore all floating point registers
	lfd %f0, CONTEXT_F0(%r3)
	lfd %f1, CONTEXT_F1(%r3)
	lfd %f2, CONTEXT_F2(%r3)
	lfd %f3, CONTEXT_F3(%r3)
	lfd %f4, CONTEXT_F4(%r3)
	lfd %f5, CONTEXT_F5(%r3)
	lfd %f6, CONTEXT_F6(%r3)
	lfd %f7, CONTEXT_F7(%r3)
	lfd %f8, CONTEXT_F8(%r3)
	lfd %f9, CONTEXT_F9(%r3)
	lfd %f10, CONTEXT_F10(%r3)
	lfd %f11, CONTEXT_F11(%r3)
	lfd %f12, CONTEXT_F12(%r3)
	lfd %f13, CONTEXT_F13(%r3)
	lfd %f14, CONTEXT_F14(%r3)
	lfd %f15, CONTEXT_F15(%r3)
	lfd %f16, CONTEXT_F16(%r3)
	lfd %f17, CONTEXT_F17(%r3)
	lfd %f18, CONTEXT_F18(%r3)
	lfd %f19, CONTEXT_F19(%r3)
	lfd %f20, CONTEXT_F20(%r3)
	lfd %f21, CONTEXT_F21(%r3)
	lfd %f22, CONTEXT_F22(%r3)
	lfd %f23, CONTEXT_F23(%r3)
	lfd %f24, CONTEXT_F24(%r3)
	lfd %f25, CONTEXT_F25(%r3)
	lfd %f26, CONTEXT_F26(%r3)
	lfd %f27, CONTEXT_F27(%r3)
	lfd %f28, CONTEXT_F28(%r3)
	lfd %f29, CONTEXT_F29(%r3)
	lfd %f30, CONTEXT_F30(%r3)
	lfd %f31, CONTEXT_F31(%r3)

	// Restore all general purpose registers
	ld %r0, CONTEXT_R0(%R3)
	ld %r1, CONTEXT_R1(%r3)
	ld %r2, CONTEXT_R2(%r3)
	ld %r4, CONTEXT_R4(%r3)
	ld %r5, CONTEXT_R5(%r3)
	ld %r6, CONTEXT_R6(%r3)
	ld %r7, CONTEXT_R7(%r3)
	ld %r8, CONTEXT_R8(%r3)
	ld %r9, CONTEXT_R9(%r3)
	ld %r10, CONTEXT_R10(%r3)
	ld %r11, CONTEXT_R11(%r3)
	ld %r12, CONTEXT_R12(%r3)
	ld %r13, CONTEXT_R13(%r3)
	ld %r14, CONTEXT_R14(%r3)
	ld %r15, CONTEXT_R15(%r3)
	ld %r16, CONTEXT_R16(%r3)
	ld %r17, CONTEXT_R17(%r3)
	ld %r18, CONTEXT_R18(%r3)
	ld %r19, CONTEXT_R19(%r3)
	ld %r20, CONTEXT_R20(%r3)
	ld %r21, CONTEXT_R21(%r3)
	ld %r22, CONTEXT_R22(%r3)
	ld %r23, CONTEXT_R23(%r3)
	ld %r24, CONTEXT_R24(%r3)
	ld %r25, CONTEXT_R25(%r3)
	ld %r26, CONTEXT_R26(%r3)
	ld %r27, CONTEXT_R27(%r3)
	ld %r28, CONTEXT_R28(%r3)
	ld %r29, CONTEXT_R29(%r3)
	ld %r30, CONTEXT_R30(%r3)
	ld %r31, CONTEXT_R31(%r3)

	// Restore Control Register - XER
	ld %r12, CONTEXT_XER(%r3)
	mtspr 1, 12

	// Restore Control Register - LR
	mtlr %r0

	// Restore Control Register - CTR
	ld %r12, CONTEXT_LINK(%r3)
	mtctr %r12

	// Restore R3 register
	ld %r3, CONTEXT_R3(%r3)

	// Branch to CTR register location
	bctr
LEAF_END RtlRestoreContext, _TEXT
