// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"
#include "asmconstants.h"

NESTED_ENTRY ThePreStub, _TEXT, NoHandler
	mflr %r0
	std %r0, 16(%r1)
	std %r2, 24(%r1)
	std %r31, -8(%r1)
	stdu %r1, -504(%r1)
	.cfi_def_cfa_offset 504
	.cfi_offset 65, 16
	.cfi_offset 31, -8
	mr %r31, %r1
	.cfi_def_cfa_register 31
// Store GPR r3 to r10 and r14 to r31. (base from stack pointer r1)

	std %r3, 32(%r1)
	std %r4, 40(%r1)
	std %r5, 48(%r1)
	std %r6, 56(%r1)
	std %r7, 64(%r1)
	std %r8, 72(%r1)
	std %r9, 80(%r1)
	std %r10, 88(%r1)

	stfd %f1, 96(%r1)
	stfd %f2, 104(%r1)
	stfd %f3, 112(%r1)
	stfd %f4, 120(%r1)
	stfd %f5, 128(%r1)
	stfd %f6, 136(%r1)
	stfd %f7, 144(%r1)
	stfd %f8, 152(%r1)
	stfd %f9, 160(%r1)
	stfd %f10, 168(%r1)
	stfd %f11, 176(%r1)
	stfd %f12, 184(%r1)
	stfd %f13, 192(%r1)

	// leaving 8 byte for padding 

	std %r14, 208(%r1)
	std %r15, 216(%r1)
	std %r16, 224(%r1)
	std %r17, 232(%r1)
	std %r18, 240(%r1)
	std %r29, 248(%r1)	
	std %r20, 256(%r1)
	std %r21, 264(%r1)
	std %r22, 272(%r1)
	std %r23, 280(%r1)
	std %r24, 288(%r1)
	std %r25, 296(%r1)
	std %r26, 304(%r1)
	std %r27, 312(%r1)
	std %r28, 320(%r1)
	std %r29, 328(%r1)
	std %r30, 336(%r1)
	std %r31, 344(%r1)

	stfd %f14, 352(%r1)
	stfd %f15, 360(%r1)
	stfd %f16, 368(%r1)
	stfd %f17, 376(%r1)
	stfd %f18, 384(%r1)
	stfd %f19, 392(%r1)
	stfd %f20, 400(%r1)
	stfd %f21, 408(%r1)
	stfd %f22, 416(%r1)
	stfd %f23, 424(%r1)
	stfd %f24, 432(%r1)
	stfd %f25, 440(%r1)
	stfd %f26, 448(%r1)
	stfd %f27, 456(%r1)
	stfd %f29, 464(%r1)
	stfd %f29, 472(%r1)
	stfd %f30, 480(%r1)
	stfd %f31, 488(%r1)

	addi %r3, %r1, 504		//load pTransitionBlock structure address in r3 (parameter registers)
	mr %r4, METHODDESC_REGISTER	//load METHODDESC_REGISTER(r11)  in r4 (parameter registers)

	bl C_FUNC(PreStubWorker)	//call function PreStubWorker
	mr %r12, %r3

//Epiloge
	ld %r3, 32(%r1)
	ld %r4, 40(%r1)
	ld %r5, 48(%r1)
	ld %r6, 56(%r1)
	ld %r7, 64(%r1)
	ld %r8, 72(%r1)
	ld %r9, 80(%r1)
	ld %r10, 88(%r1)

	lfd %f1, 96(%r1)
	lfd %f2, 104(%r1)
	lfd %f3, 112(%r1)
	lfd %f4, 120(%r1)
	lfd %f5, 128(%r1)
	lfd %f6, 136(%r1)
	lfd %f7, 144(%r1)
	lfd %f8, 152(%r1)
	lfd %f9, 160(%r1)
	lfd %f10, 168(%r1)
	lfd %f11, 176(%r1)
	lfd %f12, 184(%r1)
	lfd %f13, 192(%r1)

	// leaving 8 byte for padding 

	ld %r14, 208(%r1)
	ld %r15, 216(%r1)
	ld %r16, 224(%r1)
	ld %r17, 232(%r1)
	ld %r18, 240(%r1)
	ld %r19, 248(%r1)
	ld %r20, 256(%r1)
	ld %r21, 264(%r1)
	ld %r22, 272(%r1)
	ld %r23, 280(%r1)
	ld %r24, 288(%r1)
	ld %r25, 296(%r1)
	ld %r26, 304(%r1)
	ld %r27, 312(%r1)
	ld %r28, 320(%r1)
	ld %r29, 328(%r1)
	ld %r30, 336(%r1)
	ld %r31, 344(%r1)

	lfd %f14, 352(%r1)
	lfd %f15, 360(%r1)
	lfd %f16, 368(%r1)
	lfd %f17, 376(%r1)
	lfd %f18, 384(%r1)
	lfd %f19, 392(%r1)
	lfd %f20, 400(%r1)
	lfd %f21, 408(%r1)
	lfd %f22, 416(%r1)
	lfd %f23, 424(%r1)
	lfd %f24, 432(%r1)
	lfd %f25, 440(%r1)
	lfd %f26, 448(%r1)
	lfd %f27, 456(%r1)
	lfd %f28, 464(%r1)
	lfd %f29, 472(%r1)
	lfd %f30, 480(%r1)
	lfd %f31, 488(%r1)

	addi %r1, %r1, 504
	.cfi_def_cfa %r1, 0
	ld %r0, 16(%r1) 
	ld %r2, 24(%r1)
	mtlr %r0
	ld %r31, -8(%r1)
	mtctr %r12
	bctr

NESTED_END ThePreStub, _TEXT

LEAF_ENTRY ThePreStubPatch, _TEXT
	nop	
PATCH_LABEL ThePreStubPatchLabel
	blr
LEAF_END ThePreStubPatch, _TEXT
