; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.
; See the LICENSE file in the project root for more information.

#include "ksarm.h"
#include "asmconstants.h"
#include "asmmacros.h"

    import OnCallCountThresholdReached

    NESTED_ENTRY OnCallCountThresholdReachedStub
        PROLOG_WITH_TRANSITION_BLOCK

        add     r0, sp, #__PWTB_TransitionBlock ; TransitionBlock *
        mov     r1, r12 ; stub-identifying token
        bl      OnCallCountThresholdReached
        mov     r12, r0

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
        EPILOG_BRANCH_REG r12
    NESTED_END

    end
