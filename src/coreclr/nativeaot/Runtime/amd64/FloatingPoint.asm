;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

include asmmacros.inc

LEAF_ENTRY RhpFltRemRev, _TEXT

        sub         rsp, 18h

        movss       dword ptr [rsp + 10h], xmm1     ; divisor
        movss       dword ptr [rsp +  8h], xmm0     ; dividend

        fld         dword ptr [rsp + 10h]           ; divisor
        fld         dword ptr [rsp +  8h]           ; dividend

fremloop:
        fprem
        fstsw       ax
        test        ax, 0400h
        jnz         fremloop

        fstp        dword ptr [rsp]
        movlps      xmm0,qword ptr [rsp]

        fstp        st(0)
        add         rsp,18h
        ret

LEAF_END RhpFltRemRev, _TEXT


LEAF_ENTRY RhpDblRemRev, _TEXT

        sub         rsp, 18h

        movsd       qword ptr [rsp + 10h], xmm1     ; divisor
        movsd       qword ptr [rsp +  8h], xmm0     ; dividend

        fld         qword ptr [rsp + 10h]           ; divisor
        fld         qword ptr [rsp +  8h]           ; dividend

fremloopd:
        fprem
        fstsw       ax
        test        ax, 0400h
        jnz         fremloopd

        fstp        qword ptr [rsp]
        movlpd      xmm0,qword ptr [rsp]

        fstp        st(0)
        add         rsp,18h
        ret

LEAF_END RhpDblRemRev, _TEXT

        END
