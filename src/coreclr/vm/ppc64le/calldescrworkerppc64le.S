// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"
#include "asmconstants.h"

//extern "C" void CallDescrWorkerInternal(CallDescrData * pCallDescrData);

NESTED_ENTRY CallDescrWorkerInternal, _TEXT, NoHandler
    mflr %r0
    std %r0, 16(%r1)
    std %r2, 24(%r1)
    std %r31, -8(%r1)

    // Save pCallDescrData in %r11
    mr %r11, %r3 
    lwa %r0, CallDescrData__numStackSlots(%r11)
    cmpdi %r0, 0
    beq LOCAL_LABEL(DefaultStackAllocation)

LOCAL_LABEL(CheckStackAlignment):
    li %r4, 2
    divd %r4, %r0, %r4
    mulli %r4, %r4, 2
    sub %r4, %r0, %r4
    cmpdi %r4, 0
    beq LOCAL_LABEL(StackAllocationLoop)
    addic %r0, %r0, 1


LOCAL_LABEL(StackAllocationLoop):
    .cfi_def_cfa_offset 0
    addic %r1, %r1, -8
    .cfi_adjust_cfa_offset 8
    addic %r0, %r0, -1
    cmpdi %r0, 0
    bne LOCAL_LABEL(StackAllocationLoop)

    addic %r1, %r1, -112
    std %r31, 0(%r1)
    .cfi_adjust_cfa_offset 112
    .cfi_offset 65, 16
    .cfi_offset 31, -8
    mr %r31, %r1
    .cfi_def_cfa_register 31
    std %r3, 32(%r1)

    // If there are stack arguments, copy the arguments in.
    ld %r4, CallDescrData__pSrc(%r11)  // Source address
    li %r5, 0                          // Source offset
    li %r6, 96                        // Destination offset on stack
    lwa %r0, CallDescrData__numStackSlots(%r11)   
 
LOCAL_LABEL(StackCopyLoop):
    
    add %r7, %r4, %r5
    ld %r8, 0(%r7)
    add %r9, %r1, %r6
    std %r8, 0(%r9)
    addic %r5, %r5, 8
    addic %r6, %r6, 8
    addic %r0, %r0, -1
    cmpdi %r0, 0
    bne LOCAL_LABEL(StackCopyLoop)
    beq LOCAL_LABEL(LoadArguments)

LOCAL_LABEL(DefaultStackAllocation):

    stdu %r1, -112(%r1)
    .cfi_def_cfa_offset 112
    .cfi_offset 65, 16
    .cfi_offset 31, -8
    mr %r31, %r1
    .cfi_def_cfa_register 31
    std %r3, 32(%r1)

LOCAL_LABEL(LoadArguments):
    // All argument registers are loaded regardless of the actual number
    // of arguments.
    ld %r12, CallDescrData__pArgumentRegisters(%r11)
    ld %r3, 0(%r12)
    ld %r4, 8(%r12)
    ld %r5, 16(%r12)
    ld %r6, 24(%r12)
    ld %r7, 32(%r12)
    ld %r8, 40(%r12)
    ld %r9, 48(%r12)
    ld %r10, 56(%r12)

    ld %r12, CallDescrData__pFloatArgumentRegisters(%r11)
    cmpdi %r12, 0
    beq LOCAL_LABEL(NoFloatArguments)
    lfd %f1, 0(%r12)
    lfd %f2, 8(%r12)
    lfd %f3, 16(%r12)
    lfd %f4, 24(%r12)
    lfd %f5, 32(%r12)
    lfd %f6, 40(%r12)
    lfd %f7, 48(%r12)
    lfd %f8, 56(%r12)
    lfd %f9, 64(%r12)
    lfd %f10, 72(%r12)
    lfd %f11, 80(%r12)
    lfd %f12, 88(%r12)
    lfd %f13, 96(%r12)

LOCAL_LABEL(NoFloatArguments):

    // Call target function.
    ld %r12, CallDescrData__pTarget(%r11)
	mtctr %r12
	bctrl

LOCAL_LABEL(CallDescrWorkerInternalReturnAddress):
    // Check expected return value type.
    ld %r4, 32(%r1)
    lwz %r0, CallDescrData__fpReturnSize(%r4)
    cmpdi %r0, 0
    beq      LOCAL_LABEL(ReturnsInt)
    cmpdi %r0, 4
    beq      LOCAL_LABEL(ReturnsFloat)
    cmpdi %r0, 8
    beq      LOCAL_LABEL(ReturnsDouble)

    // Store double-precision floating-point value.

LOCAL_LABEL(ReturnsDouble):
    stfd     %f1,CallDescrData__returnValue(%r4)
    b       LOCAL_LABEL(Epilog)

    // Store single-precision floating-point value.

LOCAL_LABEL(ReturnsFloat):
    stfs     %f1,CallDescrData__returnValue(%r4)
    b       LOCAL_LABEL(Epilog)

    // Store integer return value.

LOCAL_LABEL(ReturnsInt):
    std     %r3,CallDescrData__returnValue(%r4)
    // fall through

LOCAL_LABEL(Epilog):

	lwa %r5, CallDescrData__numStackSlots(%r4)
	li %r6, 2
	divd %r6, %r5, %r6
	mulli %r6, %r6, 2
	sub %r6, %r5, %r6
	cmpdi %r6, 0
	beq LOCAL_LABEL(NO_STACK_PADDING_EPILOG)
	addic %r5, %r5, 1

LOCAL_LABEL(NO_STACK_PADDING_EPILOG):
	mulli %r5, %r5, 8
	addic %r6, %r5, 112
	
	add   %r1, %r1, %r6

	.cfi_def_cfa %r1, 0	
	ld      %r2, 24(%r1)
	ld      %r0, 16(%r1)
	mtlr    %r0
	ld %r31, -8(%r1)
        blr

PATCH_LABEL CallDescrWorkerInternalReturnAddressOffset
    .quad LOCAL_LABEL(CallDescrWorkerInternalReturnAddress) - C_FUNC(CallDescrWorkerInternal)

NESTED_END CallDescrWorkerInternal, _TEXT
