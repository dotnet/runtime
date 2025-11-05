// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"
#include "asmconstants.h"

//extern "C" void CallDescrWorkerInternal(CallDescrData * pCallDescrData);

NESTED_ENTRY CallDescrWorkerInternal, _TEXT, NoHandler
        stmg    %r6,%r15,48(%r15)
        .cfi_offset 6, -112
        .cfi_offset 7, -104
        .cfi_offset 8, -96
        .cfi_offset 9, -88
        .cfi_offset 10, -80
        .cfi_offset 11, -72
        .cfi_offset 12, -64
        .cfi_offset 13, -56
        .cfi_offset 14, -48
        .cfi_offset 15, -40
        lay     %r15,-160(%r15)
        .cfi_def_cfa_offset 320
        lgr     %r11,%r15
        .cfi_def_cfa_register 11

        lgr     %r12,%r2                // save pCallDescrData in %r12

        // If there are stack arguments, allocate stack space
        // and copy the arguments in.

        lgf     %r1,CallDescrData__numStackSlots(%r2)
        cgijle  %r1,0,LOCAL_LABEL(NoStackArguments)

        sllg    %r3,%r1,3
        sgr     %r15,%r3

        lg      %r4,CallDescrData__pSrc(%r2)
        la      %r5,160(%r15)
LOCAL_LABEL(StackCopyLoop):
        lay     %r3,-8(%r3)
        lg      %r0,0(%r3,%r4)
        stg     %r0,0(%r3,%r5)
        brctg   %r1,LOCAL_LABEL(StackCopyLoop)

LOCAL_LABEL(NoStackArguments):

        // All argument registers are loaded regardless of the actual number
        // of arguments.

        lg      %r1,CallDescrData__pArgumentRegisters(%r12)
        lg      %r2,0(%r1)
        lg      %r3,8(%r1)
        lg      %r4,16(%r1)
        lg      %r5,24(%r1)
        lg      %r6,32(%r1)

        ltg     %r1,CallDescrData__pFloatArgumentRegisters(%r12)
        je      LOCAL_LABEL(NoFloatArguments)
        ld      %f0,0(%r1)
        ld      %f2,8(%r1)
        ld      %f4,16(%r1)
        ld      %f6,24(%r1)

LOCAL_LABEL(NoFloatArguments):

        // Call target function.

        lg      %r1,CallDescrData__pTarget(%r12)
        basr    %r14,%r1
LOCAL_LABEL(CallDescrWorkerInternalReturnAddress):

        // Check expected return value type.

        lt      %r1,CallDescrData__fpReturnSize(%r12)
        je      LOCAL_LABEL(ReturnsInt)
        chi     %r1,4
        je      LOCAL_LABEL(ReturnsFloat)
        chi     %r1,8
        je      LOCAL_LABEL(ReturnsDouble)
        .long   0x00CC

        // Store double-precision floating-point value.

LOCAL_LABEL(ReturnsDouble):
        std     %f0,CallDescrData__returnValue(%r12)
        j       LOCAL_LABEL(Epilog)

        // Store single-precision floating-point value.

LOCAL_LABEL(ReturnsFloat):
        ste     %f0,CallDescrData__returnValue+4(%r12)
        j       LOCAL_LABEL(Epilog)

        // Store integer return value.

LOCAL_LABEL(ReturnsInt):
        stg     %r2,CallDescrData__returnValue(%r12)
        // fall through

LOCAL_LABEL(Epilog):
        lmg     %r6,%r15,208(%r11)
        .cfi_restore 15
        .cfi_restore 14
        .cfi_restore 13
        .cfi_restore 12
        .cfi_restore 11
        .cfi_restore 10
        .cfi_restore 9
        .cfi_restore 8
        .cfi_restore 7
        .cfi_restore 6
        .cfi_def_cfa 15, 160
        br      %r14

PATCH_LABEL CallDescrWorkerInternalReturnAddressOffset
    .quad LOCAL_LABEL(CallDescrWorkerInternalReturnAddress) - C_FUNC(CallDescrWorkerInternal)

NESTED_END CallDescrWorkerInternal, _TEXT
