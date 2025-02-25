// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <unixasmmacros.inc>

//;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;  DATA SECTIONS  ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

#define POINTER_SIZE 0x08

//;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;; Interop Thunks Helpers ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

    //
    // RhCommonStub
    //
    //  INPUT: t1: thunk's data block
    //
    //  TRASHES: t0, t1, t2
    //
    LEAF_ENTRY RhCommonStub, _TEXT
        // There are arbitrary callers passing arguments with arbitrary signatures.
        // Custom calling convention:
        //      tp pointer to the current thunk's data block (data contains 2 pointer values: context + target pointers)

        mv               t2, a0
        INLINE_GET_TLS_VAR t0, C_FUNC(tls_thunkData)
        mv               a0, t2

        // t0 = base address of TLS data
        // t1 = address of context cell in thunk's data

        // Load the thunk address from the data block and store it in the thread's static storage
        ld    t2, 0(t1)        // Load thunk data into t2
        sd    t2, 0(t0)        // Store the thunk address in thread static storage

        // Load the target address from the data block and jump to it
        ld    t1, POINTER_SIZE(t1)  // Load target address into t1 from the data block
        jr    t1                    // Jump to the target address in t1

    LEAF_END RhCommonStub, _TEXT

    //
    // IntPtr RhGetCommonStubAddress()
    //
    LEAF_ENTRY RhGetCommonStubAddress, _TEXT
        PREPARE_EXTERNAL_VAR RhCommonStub, a0
        jr  ra                 // Return address in ra
    LEAF_END RhGetCommonStubAddress, _TEXT
