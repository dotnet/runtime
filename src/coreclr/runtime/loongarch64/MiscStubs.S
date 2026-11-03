// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros_Shared.h"

LEAF_ENTRY RhpGcPoll, _TEXT
    PREPARE_EXTERNAL_VAR_INDIRECT_W G_TRAP_THREADS, $a0
    bnez  $a0, LOCAL_LABEL(RhpGcPoll_RarePath)
    jirl  $r0, $ra, 0
LOCAL_LABEL(RhpGcPoll_RarePath):
    TAILCALL_GC_POLL_RARE
LEAF_END RhpGcPoll, _TEXT
