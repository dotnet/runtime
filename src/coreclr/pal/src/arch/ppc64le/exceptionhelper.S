// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"
#include "asmconstants.h"

//////////////////////////////////////////////////////////////////////////
//
// This function creates a stack frame right below the target frame, restores all callee
// saved registers from the passed in context, sets R15 to that frame and sets the
// return address to the target frame's PSW address.
// Then it uses the ThrowExceptionHelper to throw the passed in exception from that context.
// EXTERN_C void ThrowExceptionFromContextInternal(CONTEXT* context, PAL_SEHException* ex);
LEAF_ENTRY ThrowExceptionFromContextInternal, _TEXT

	// Restore all non volatile floating point registers
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

	// Restore all non volatile general purpose registers
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

	ld %r0, CONTEXT_NIP(%r3)
	mtlr %r0

	ld %r1, CONTEXT_R1(%r3)

	// The PAL_SEHException pointer
	mr     %r3, %r4
	b      EXTERNAL_C_FUNC(ThrowExceptionHelper)
LEAF_END ThrowExceptionFromContextInternal, _TEXT
