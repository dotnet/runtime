// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <unixasmmacros.inc>
#include "AsmOffsets.inc"

NESTED_ENTRY RhpGetInlinedThreadStaticBase, _TEXT, NoHandler
        // On exit:
        //   x0 - the thread static base for the given type

        // x1 = GetThread()
        INLINE_GET_TLS_VAR x1, C_FUNC(tls_InlinedThreadStatics)

        // get per-thread storage
        ldr     x0, [x1]
        cbnz    x0, HaveValue
        mov     x0, x1
        b       C_FUNC(RhpGetInlinedThreadStaticBaseSlow)

HaveValue:
        // return it
        ret

NESTED_END RhpGetInlinedThreadStaticBase, _TEXT
