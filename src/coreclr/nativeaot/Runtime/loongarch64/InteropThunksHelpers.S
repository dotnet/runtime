// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <unixasmmacros.inc>

//;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;  DATA SECTIONS  ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

POINTER_SIZE = 0x08

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
        // This doesn't save and restore the floating point argument registers. If we encounter a
        // target system that uses TLS emulation and modify these registers during this call we
        // need to save and restore them, too
        GETTHUNKDATA_ETLS_9
#else
        INLINE_GET_TLS_VAR  $t0, C_FUNC(tls_thunkData)
#endif

        // t0  = base address of TLS data
        // tp = address of context cell in thunk's data

        // store thunk address in thread static
        ld.d  $t1, $t7, 0
        st.d  $t1, $t0, 0

        // Now load the target address and jump to it.
        ld.d  $t7, $t7, POINTER_SIZE
        jirl  $r0, $t7, 0

    LEAF_END RhCommonStub, _TEXT

    //
    // IntPtr RhGetCommonStubAddress()
    //
    LEAF_ENTRY RhGetCommonStubAddress, _TEXT
        PREPARE_EXTERNAL_VAR  RhCommonStub, $a0
        jirl  $r0, $ra, 0
    LEAF_END RhGetCommonStubAddress, _TEXT
