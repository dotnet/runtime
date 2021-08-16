// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"

#if defined(_DEBUG)
NESTED_ENTRY DBG_CheckStackAlignment, _TEXT, NoHandler
    PROLOG_SAVE_REG_PAIR_INDEXED fp, lr, -16
    // Reading from an unaligned stack pointer will trigger a stack alignment fault
    ldr x0, [sp]
    EPILOG_RESTORE_REG_PAIR_INDEXED fp, lr, 16
    ret
NESTED_END _DBG_CheckStackAlignment, _TEXT
#endif

