// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include <unixasmmacros.inc>

LEAF_ENTRY RhpGcPoll, _TEXT
        cmp         dword ptr [C_VAR(RhpTrapThreads)], TrapThreadsFlags_None
        jne         LOCAL_LABEL(RhpGcPoll_RarePath)
        ret
LOCAL_LABEL(RhpGcPoll_RarePath):
        jmp C_FUNC(RhpGcPollRare)

LEAF_END RhpGcPoll, _TEXT

NESTED_ENTRY RhpGcPollRare, _TEXT, NoHandler
        PUSH_COOP_PINVOKE_FRAME rdi
        END_PROLOGUE
        call        C_FUNC(RhpGcPoll2)
        POP_COOP_PINVOKE_FRAME
        ret
NESTED_END RhpGcPollRare, _TEXT
