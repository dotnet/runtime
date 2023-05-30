;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros.h"

    EXTERN RhpGetInlinedThreadStaticBaseSlow

    TEXTAREA

;; On exit:
;;   x0 - the thread static base for the given type
    LEAF_ENTRY RhpGetInlinedThreadStaticBase
        ;; x1 = &tls_InlinedThreadStatics, TRASHES x2
        INLINE_GET_TLS_VAR x1, x2, tls_InlinedThreadStatics

        ;; get per-thread storage
        ldr     x0, [x1]
        cbnz    x0, HaveValue
        mov     x0, x1
        b       RhpGetInlinedThreadStaticBaseSlow

HaveValue
        ;; return it
        ret
    LEAF_END RhpGetInlinedThreadStaticBase

    end
