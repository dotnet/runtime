// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Implementation of _CONTEXT_CaptureContext for the RISCV64 platform.
// This function is processor dependent.  It is used by exception handling,
// and is always apply to the current thread.
//

#include "unixasmmacros.inc"
#include "asmconstants.h"

// #error "TODO RISCV64: missing implementation"
LEAF_ENTRY RtlRestoreContext, _TEXT
// TODO RISCV64 NYI
    sw ra, 0(zero)
LEAF_END RtlRestoreContext, _TEXT

LEAF_ENTRY RtlCaptureContext, _TEXT
// TODO RISCV64 NYI
    sw ra, 0(zero)
LEAF_END RtlCaptureContext, _TEXT

LEAF_ENTRY CONTEXT_CaptureContext, _TEXT
// TODO RISCV64 NYI
    sw ra, 0(zero)
LEAF_END CONTEXT_CaptureContext, _TEXT
