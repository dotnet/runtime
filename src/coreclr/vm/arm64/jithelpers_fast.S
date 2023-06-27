// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Helper to calculate the offset of native thread local variable `t_ThreadStatics`. The offset has to be found at runtime
// once linker does its relocation and fixup of thread locals. The runtime gets the address of this function, so
// it can walk through the instruction bytes to retrieve the offset embedded by the linker and calculate the
// final offset that should be passed to __tls_get_addr() in order to calculate the address of `t_ThreadStatics` for
// the current thread. Here, we have to call `__tls_get_addr()`, because if the linker tries to find the code pattern
// of "lea t_ThreadStatics@TLSGD", followed by `call __tls_get_addr()`. Without adding the call, the linker complains.
// We never have to call this method directly, and hence there is a `int 3` at the end.
LEAF_ENTRY JIT_GetThreadStaticsBaseOffset, _TEXT
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
        ret
LEAF_END JIT_GetThreadStaticsBaseOffset, _TEXT
