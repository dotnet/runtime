// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <unixasmmacros.inc>

// WARNING: Code in EHHelpers.cpp makes assumptions about this helper, in particular:
// - Function "InWriteBarrierHelper" assumes an AV due to passed in null pointer will happen at RhpLockCmpXchg32AVLocation
// - Function "UnwindSimpleHelperToCaller" assumes no registers were pushed and LR contains the return address
// x0 = destination address
// w1 = value
// w2 = comparand
LEAF_ENTRY RhpLockCmpXchg32, _TEXT
    mov     x8, x0          // Save value of x0 into x8 as x0 is used for the return value
ALTERNATE_ENTRY RhpLockCmpXchg32AVLocation
1: // loop
    ldaxr   w0, [x8]        // w0 = *x8
    cmp     w0, w2
    bne     2f              // if (w0 != w2) goto exit
    stlxr   w9, w1, [x8]    // if (w0 == w2) { try *x8 = w1 and goto loop if failed or goto exit }
    cbnz    w9, 1b

2: // exit
    InterlockedOperationBarrier
    ret
LEAF_END RhpLockCmpXchg32, _TEXT

// WARNING: Code in EHHelpers.cpp makes assumptions about this helper, in particular:
// - Function "InWriteBarrierHelper" assumes an AV due to passed in null pointer will happen at RhpLockCmpXchg64AVLocation
// - Function "UnwindSimpleHelperToCaller" assumes no registers were pushed and LR contains the return address
// x0 = destination address
// x1 = value
// x2 = comparand
LEAF_ENTRY RhpLockCmpXchg64, _TEXT
    mov     x8, x0          // Save value of x0 into x8 as x0 is used for the return value
ALTERNATE_ENTRY RhpLockCmpXchg64AVLocation
1: // loop
    ldaxr   x0, [x8]        // x0 = *x8
    cmp     x0, x2
    bne     2f              // if (x0 != x2) goto exit
    stlxr   w9, x1, [x8]    // if (x0 == x2) { try *x8 = x1 and goto loop if failed or goto exit }
    cbnz    w9, 1b
2: // exit
    InterlockedOperationBarrier
    ret
LEAF_END RhpLockCmpXchg64, _TEXT
