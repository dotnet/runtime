// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Loads the address of a thread-local variable into the target register,
// which cannot be a0. Preserves all other registers.
.macro INLINE_GET_TLS_VAR target, var
    .ifc  \target, $a0
        .error "target cannot be a0"
    .endif

    addi.d  $sp, $sp, -16
    st.d  $a0, $sp, 0
    st.d  $ra, $sp, 8

    // This instruction is recognized and potentially patched
    // by the linker (GD->IE/LE relaxation).
    la.tls.desc  $a0, \var

    ori  \target, $tp, 0
    add.d  \target, \target, $a0

    ld.d  $a0, $sp, 0
    ld.d  $ra, $sp, 8
    addi.d $sp, $sp, 16
.endm
