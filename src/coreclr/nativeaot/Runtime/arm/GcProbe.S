// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <unixasmmacros.inc>
#include "AsmOffsets.inc"

    .global RhpGcPoll2

    LEAF_ENTRY RhpGcPoll
        ldr         r0, =RhpTrapThreads
        ldr         r0, [r0]
        cmp         r0, #TrapThreadsFlags_None
        bne         RhpGcPollRare
        bx          lr
    LEAF_END RhpGcPoll

    NESTED_ENTRY RhpGcPollRare, _TEXT, NoHandler
        PUSH_COOP_PINVOKE_FRAME r0
        bl RhpGcPoll2
        POP_COOP_PINVOKE_FRAME
        bx           lr
    NESTED_END RhpGcPollRare
