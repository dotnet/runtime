;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros.h"

        TEXTAREA

        IMPORT fmod

        NESTED_ENTRY RhpFltRemRev

        PROLOG_PUSH     {r4,lr}     ; Save return address (and r4 for stack alignment)

        ;; The CRT only exports the double form of fmod, so we need to convert our input registers (s0, s1) to
        ;; doubles (d0, d1). Unfortunately these registers overlap (d0 == s0/s1) so we need to move our inputs
        ;; elsewhere first. In this case we can move them into s4/s5, which are also volatile and don't need
        ;; to be preserved.
        vmov.f32        s4, s0
        vmov.f32        s5, s1

        ;; Convert s4 and s5 into d0 and d1.
        vcvt.f64.f32    d0, s4
        vcvt.f64.f32    d1, s5

        ;; Call the CRT's fmod to calculate the remainder into d0.
        ldr             r12, =fmod
        blx             r12

        ;; Convert double result back to single. As far as I can see it's legal to do this directly even
        ;; though d0 overlaps s0.
        vcvt.f32.f64    s0, d0

        EPILOG_POP      {r4,lr}
        EPILOG_RETURN

        NESTED_END RhpFltRemRev

        end
