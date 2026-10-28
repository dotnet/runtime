// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros_Shared.h"

// void* PacStripPtr(void *);
// This function strips the pointer of PAC info that is passed as an argument.
// We prefer to strip a pointer where it's not going to be used to branch execution to.
// It is a no-op on non-PAC enabled machines.
.arch_extension pauth
    NESTED_ENTRY PacStripPtr, _TEXT, NoHandler
        PROLOG_SAVE_REG_PAIR_INDEXED fp, lr, -16
        mov lr, x0
        xpaclri
        mov x0, lr
        EPILOG_RESTORE_REG_PAIR_INDEXED fp, lr, 16
        EPILOG_RETURN
    NESTED_END PacStripPtr, _TEXT

// void* PacSignPtr(void *, void *);
// This function signs the input pointer using x1 as salt. It is a no-op on non-PAC enabled machines.
.arch_extension pauth
    LEAF_ENTRY PacSignPtr, _TEXT
        mov x17, x0
        mov x16, x1
        pacia1716
        mov x0, x17
        ret
    LEAF_END PacSignPtr, _TEXT

// void* PacAuthPtr(void *, void *);
// This function authenticates the input signed-pointer using x1 as salt. It is a no-op on non-PAC enabled machines.
.arch_extension pauth
    LEAF_ENTRY PacAuthPtr, _TEXT
        mov x17, x0
        mov x16, x1
        autia1716
        mov x0, x17
        ret
    LEAF_END PacAuthPtr, _TEXT

LEAF_ENTRY RhpGcPoll, _TEXT
    PREPARE_EXTERNAL_VAR_INDIRECT_W G_TRAP_THREADS, 0
    cbnz    w0, LOCAL_LABEL(RhpGcPoll_RarePath)
    ret
LOCAL_LABEL(RhpGcPoll_RarePath):
    TAILCALL_GC_POLL_RARE
LEAF_END RhpGcPoll, _TEXT
