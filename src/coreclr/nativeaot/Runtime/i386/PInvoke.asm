;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

        .586
        .model  flat
        option  casemap:none
        .code

include AsmMacros.inc

FASTCALL_FUNC RhpPInvoke, 4
ALTERNATE_HELPER_ENTRY RhpPInvoke
        ;; TODO
        int 3
FASTCALL_ENDFUNC

FASTCALL_FUNC RhpPInvokeReturn, 4
ALTERNATE_HELPER_ENTRY RhpPInvokeReturn
        ;; TODO
        int 3
FASTCALL_ENDFUNC

        end
