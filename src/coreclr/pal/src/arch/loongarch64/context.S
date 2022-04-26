// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"

#if defined(_DEBUG)
NESTED_ENTRY DBG_CheckStackAlignment, _TEXT, NoHandler
    //                           $fp,$ra
    PROLOG_SAVE_REG_PAIR_INDEXED 22, 1, -16
    // Reading from an unaligned stack pointer will trigger a stack alignment fault
    ld.d $a0, $sp
    //                              $fp,$ra
    EPILOG_RESTORE_REG_PAIR_INDEXED 22, 1, 16
    jirl  $r0, $ra, 0
NESTED_END _DBG_CheckStackAlignment, _TEXT
#endif
