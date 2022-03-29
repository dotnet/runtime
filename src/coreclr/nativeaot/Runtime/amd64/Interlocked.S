// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include <unixasmmacros.inc>

// WARNING: Code in EHHelpers.cpp makes assumptions about this helper, in particular:
// - Function "InWriteBarrierHelper" assumes an AV due to passed in null pointer will happen at RhpLockCmpXchg32AVLocation
// - Function "UnwindSimpleHelperToCaller" assumes the stack contains just the pushed return address
LEAF_ENTRY RhpLockCmpXchg32, _TEXT
    mov             rax, rdx
ALTERNATE_ENTRY RhpLockCmpXchg32AVLocation
    lock cmpxchg    [rdi], esi
    ret
LEAF_END RhpLockCmpXchg32, _TEXT

// WARNING: Code in EHHelpers.cpp makes assumptions about this helper, in particular:
// - Function "InWriteBarrierHelper" assumes an AV due to passed in null pointer will happen at RhpLockCmpXchg64AVLocation
// - Function "UnwindSimpleHelperToCaller" assumes the stack contains just the pushed return address
LEAF_ENTRY RhpLockCmpXchg64, _TEXT
    mov             rax, rdx
ALTERNATE_ENTRY RhpLockCmpXchg64AVLocation
    lock cmpxchg    [rdi], rsi
    ret
LEAF_END RhpLockCmpXchg64, _TEXT
