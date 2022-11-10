// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"

// #error "TODO-RISCV64: missing implementation"

LEAF_ENTRY JIT_MemSet, _TEXT
// TODO RISCV NYI
    sw ra, 0(zero)
LEAF_END_MARKED JIT_MemSet, _TEXT

LEAF_ENTRY JIT_MemCpy, _TEXT
// TODO RISCV NYI
    sw ra, 0(zero)
LEAF_END_MARKED JIT_MemCpy, _TEXT
