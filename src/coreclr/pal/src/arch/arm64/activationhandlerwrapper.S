// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"
#include "asmconstants.h"

// Offset of the return address from the ActivationHandler in the ActivationHandlerWrapper
.globl C_FUNC(ActivationHandlerReturnOffset)
C_FUNC(ActivationHandlerReturnOffset):
    .int LOCAL_LABEL(ActivationHandlerReturn)-C_FUNC(ActivationHandlerWrapper)

NESTED_ENTRY ActivationHandlerWrapper, _TEXT, NoHandler
    PROLOG_SAVE_REG_PAIR_INDEXED fp, lr, -(16 + CONTEXT_Size)
    // Should never actually run
    EMIT_BREAKPOINT
    bl      EXTERNAL_C_FUNC(ActivationHandler)
LOCAL_LABEL(ActivationHandlerReturn):
    // Should never return
    EMIT_BREAKPOINT
    EPILOG_RESTORE_REG_PAIR_INDEXED fp, lr, (16 + CONTEXT_Size)
    ret
NESTED_END ActivationHandlerWrapper, _TEXT
