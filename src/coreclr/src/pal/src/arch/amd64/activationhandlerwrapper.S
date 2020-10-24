// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

#ifdef HOST_64BIT
// Offset of the return address from the ActivationHandler in the ActivationHandlerWrapper
.globl C_FUNC(ActivationHandlerReturnOffset)
C_FUNC(ActivationHandlerReturnOffset):
    .int LOCAL_LABEL(ActivationHandlerReturn)-C_FUNC(ActivationHandlerWrapper)

NESTED_ENTRY ActivationHandlerWrapper, _TEXT, NoHandler
    push_nonvol_reg rbp
    mov     rbp, rsp
    alloc_stack (CONTEXT_Size)
    set_cfa_register rbp, (2*8)
    mov     rdi, rsp
    int3
    call    C_FUNC(ActivationHandler)
LOCAL_LABEL(ActivationHandlerReturn):
    int3
    free_stack (CONTEXT_Size)
    pop_nonvol_reg rbp
    ret
NESTED_END ActivationHandlerWrapper, _TEXT

#endif // HOST_64BIT
