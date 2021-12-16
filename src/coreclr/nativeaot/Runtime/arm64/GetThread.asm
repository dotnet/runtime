;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros.h"

        TEXTAREA

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpGetThread
;;
;;
;; INPUT: none
;;
;; OUTPUT: x9: Thread pointer
;;
;; MUST PRESERVE ARGUMENT REGISTERS
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
    LEAF_ENTRY RhpGetThread
        ;; x9 = GetThread(), TRASHES xip0 (which can be used as an intra-procedure-call scratch register)
        INLINE_GETTHREAD x9, xip0
        ret
    LEAF_END
FASTCALL_ENDFUNC

    INLINE_GETTHREAD_CONSTANT_POOL

    end
