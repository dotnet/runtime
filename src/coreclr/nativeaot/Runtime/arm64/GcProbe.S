// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <unixasmmacros.inc>
#include "AsmOffsets.inc"

    .global RhpGcPoll2

    LEAF_ENTRY RhpGcPoll
        PREPARE_EXTERNAL_VAR_INDIRECT_W RhpTrapThreads, 0
        cbnz    w0, RhpGcPollRare // TrapThreadsFlags_None = 0
        ret
    LEAF_END RhpGcPoll

    NESTED_ENTRY RhpGcPollRare, _TEXT, NoHandler
        PUSH_COOP_PINVOKE_FRAME x0
        bl RhpGcPoll2
        POP_COOP_PINVOKE_FRAME
        ret
    NESTED_END RhpGcPollRare
