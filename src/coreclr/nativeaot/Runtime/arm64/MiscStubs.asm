;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros.h"

    TEXTAREA

; void* PacStripPtr(void *);
    LEAF_ENTRY PacStripPtr
        ; xpaci    x0
        DCD     0xDAC143E0
        ret     lr
    LEAF_END PacStripPtr

    end
