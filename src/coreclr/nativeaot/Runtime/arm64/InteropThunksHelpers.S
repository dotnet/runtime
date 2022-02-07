// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <unixasmmacros.inc>

//;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;  DATA SECTIONS  ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

POINTER_SIZE = 0x08

//;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;; Interop Thunks Helpers ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

    //
    // RhCommonStub
    //
    //  INPUT: xip0: thunk's data block
    //
    //  TRASHES: x9, x10, xip0
    //
    LEAF_ENTRY RhCommonStub, _TEXT
        // There are arbitrary callers passing arguments with arbitrary signatures.
        // Custom calling convention:
        //      xip0 pointer to the current thunk's data block (data contains 2 pointer values: context + target pointers)

#ifdef FEATURE_EMULATED_TLS
        // This doesn't save and restore the floating point argument registers. If we encounter a
        // target system that uses TLS emulation and modify these registers during this call we
        // need to save and restore them, too
        GETTHUNKDATA_ETLS_9
#else
        INLINE_GET_TLS_VAR x9, tls_thunkData
#endif

        // x9  = base address of TLS data
        // xip0 = address of context cell in thunk's data

        // store thunk address in thread static
        ldr         x10, [xip0]
        str         x10, [x9]

        // Now load the target address and jump to it.
        ldr         xip0, [xip0, #POINTER_SIZE]
        br          xip0

    LEAF_END RhCommonStub, _TEXT

    //
    // IntPtr RhGetCommonStubAddress()
    //
    LEAF_ENTRY RhGetCommonStubAddress, _TEXT
        PREPARE_EXTERNAL_VAR RhCommonStub, x0
        ret
    LEAF_END RhGetCommonStubAddress, _TEXT


#ifndef FEATURE_EMULATED_TLS
    //
    // IntPtr RhGetCurrentThunkContext()
    //
    LEAF_ENTRY RhGetCurrentThunkContext, _TEXT

        INLINE_GET_TLS_VAR x1, tls_thunkData

        ldr x0, [x1]

        ret

    LEAF_END RhGetCurrentThunkContext, _TEXT
#endif //FEATURE_EMULATED_TLS
