// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.syntax unified
.thumb

#include <unixasmmacros.inc>
#include "AsmOffsets.inc"

.global RhpGcPoll2

LEAF_ENTRY RhpGcPoll
    PREPARE_EXTERNAL_VAR_INDIRECT RhpTrapThreads, r0
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

NESTED_ENTRY RhpGcProbeHijack, _TEXT, NoHandler
    // Not implemented
    EMIT_BREAKPOINT
NESTED_END RhpGcProbeHijack

#ifdef FEATURE_GC_STRESS
LEAF_ENTRY RhpGcStressHijack
    // Not implemented
    EMIT_BREAKPOINT
LEAF_END RhpGcStressHijack
#endif
