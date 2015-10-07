//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

#ifdef BIT64
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

#endif // BIT64
