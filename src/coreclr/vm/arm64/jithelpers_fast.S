// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "asmconstants.h"
#include "unixasmmacros.inc"

// Helper to calculate the offset of native thread local variable `t_ThreadStatics`. The offset has to be found at runtime
// once linker does its relocation and fixup of thread locals. The offset, after calculation is returned in `x0` register.

LEAF_ENTRY JIT_GetThreadStaticsBaseOffset, _TEXT
        PROLOG_SAVE_REG_PAIR_INDEXED   fp, lr, -32
#ifdef TARGET_OSX
        adrp    x0,     _t_ThreadStatics@TLVPPAGE
        ldr     x0,     [x0, _t_ThreadStatics@TLVPPAGEOFF]
#else
        adrp    x0,     :tlsdesc:t_ThreadStatics
        ldr     x1,     [x0, #:tlsdesc_lo12:t_ThreadStatics]
        add     x0,     x0, :tlsdesc_lo12:t_ThreadStatics
        .tlsdesccall    t_ThreadStatics
        blr     x1
#endif
        EPILOG_RESTORE_REG_PAIR_INDEXED fp, lr, 32
        EPILOG_RETURN
LEAF_END JIT_GetThreadStaticsBaseOffset, _TEXT
