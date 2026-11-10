// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file is used to allow sharing of assembly code between NativeAOT and CoreCLR, which have different conventions about how to ensure that constants offsets are accessible

#ifdef TARGET_WINDOWS
#include "ksarm64.h"
#include "asmconstants.h"
#include "asmmacros.h"

    IMPORT g_lowest_address
    IMPORT g_highest_address
    IMPORT g_ephemeral_low
    IMPORT g_ephemeral_high
    IMPORT g_card_table

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    IMPORT g_card_bundle_table
#endif

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    IMPORT g_write_watch_table
#endif

    IMPORT g_TrapReturningThreads
#define G_TRAP_THREADS g_TrapReturningThreads

    IMPORT g_pPollGC

    IMPORT RhpGcAlloc
    IMPORT RhExceptionHandling_FailedAllocation

    MACRO
        TAILCALL_GC_POLL_RARE

        ldr     x9, =g_pPollGC
        ldr     x9, [x9]
        br      x9
    MEND

#else
#include "asmconstants.h"
#include "unixasmmacros.inc"

.macro TAILCALL_GC_POLL_RARE
        PREPARE_EXTERNAL_VAR_INDIRECT g_pPollGC, x9
        br      x9
.endm

#endif

