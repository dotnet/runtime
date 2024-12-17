// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <unixasmmacros.inc>

//;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;  DATA SECTIONS  ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

#define POINTER_SIZE 0x08

//;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;; Interop Thunks Helpers ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

    //
    // RhCommonStub
    //
    //  INPUT: tp: thunk's data block
    //
    //  TRASHES: t0, t1, tp
    //
    LEAF_ENTRY RhCommonStub, _TEXT
        // There are arbitrary callers passing arguments with arbitrary signatures.
        // Custom calling convention:
        //      tp pointer to the current thunk's data block (data contains 2 pointer values: context + target pointers)

#ifdef FEATURE_EMULATED_TLS
        // If using TLS emulation, fetch the TLS data block address
        GETTHUNKDATA_ETLS_9
#else
        // Inline function to get TLS variable
        INLINE_GET_TLS_VAR t0, C_FUNC(tls_thunkData)
#endif

        // t0 = base address of TLS data
        // tp = address of context cell in thunk's data

        // Load the thunk address from the data block and store it in the thread's static storage
        ld    t1, 0(t0)        // Load thunk address into t1 from the TLS base address
        sd    t1, 0(t0)        // Store the thunk address in thread static storage

        // Load the target address from the data block and jump to it
        ld    t1, POINTER_SIZE(t0)  // Load target address into t1 from the data block
        jalr  t1                    // Jump to the target address in t1

    LEAF_END RhCommonStub, _TEXT

    //
    // IntPtr RhGetCommonStubAddress()
    //
    LEAF_ENTRY RhGetCommonStubAddress, _TEXT
        PREPARE_EXTERNAL_VAR RhCommonStub, a0
        jr  ra                 // Return address in ra
    LEAF_END RhGetCommonStubAddress, _TEXT
