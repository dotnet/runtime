// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Loads the address of a thread-local variable into the target register.
// The target register cannot be a0.
.macro INLINE_GET_TLS_VAR target, var, ofs = 0
    .ifc \target, a0
        .error "target cannot be a0"
    .endif

    addi sp, sp, -72
    sd ra,  64(sp)
    sd t1,  56(sp)
    sd a1,  48(sp)
    sd a2,  40(sp)
    sd a3,  32(sp)
    sd a4,  24(sp)
    sd a5,  16(sp)
    sd a6,   8(sp)
    sd a7,   0(sp)

    // global dynamic TLS, see https://github.com/riscv-non-isa/riscv-elf-psabi-doc/blob/eb2b2962/riscv-elf.adoc#global-dynamic
    la.tls.gd a0, \var
    call C_FUNC(__tls_get_addr)

    ld ra,  64(sp)
    ld t1,  56(sp)
    ld a1,  48(sp)
    ld a2,  40(sp)
    ld a3,  32(sp)
    ld a4,  24(sp)
    ld a5,  16(sp)
    ld a6,   8(sp)
    ld a7,   0(sp)
    addi sp, sp, 72

    add \target, a0, \ofs

    /*
    In the future we should switch to TLS descriptors. Its support was added in 2024 in glibc, musl, llvm, gcc and binutils,
    which is currently unavailable on majority devices. See https://maskray.me/blog/2024-01-23-riscv-tlsdesc-works

    When the support for TLS descriptors is available in NativeAOT baseline, actions to perform:
    * Apply this patch:
        ```
        --- a/src/coreclr/nativeaot/CMakeLists.txt
        +++ b/src/coreclr/nativeaot/CMakeLists.txt
        @@ -30,6 +30,8 @@ endif (CLR_CMAKE_HOST_UNIX)

        if(CLR_CMAKE_TARGET_ANDROID)
            add_definitions(-DFEATURE_EMULATED_TLS)
        +elseif(CLR_CMAKE_TARGET_ARCH_RISCV64)
        +    add_definitions(-mtls-dialect=desc)
        endif()

        add_subdirectory(Bootstrap)
        ```
    * Remove global dynamic code including prolog and epilog.
    * Uncomment the following code and remove these comments.

    // TLS descriptor, see https://github.com/riscv-non-isa/riscv-elf-psabi-doc/blob/eb2b2962/riscv-elf.adoc#tls-descriptors
    auipc  a0, %tlsdesc_hi(\var)
    lw     t0, %tlsdesc_load_lo(\var)(a0)
    addi   a0, a0, %tlsdesc_add_lo(\var)
    jalr   t0, 0(t0), %tlsdesc_call(\var)
    add    \target, tp, a0
    .ifnc \ofs, 0
            add     \target, \target, \ofs
    .endif

    */
.endm
