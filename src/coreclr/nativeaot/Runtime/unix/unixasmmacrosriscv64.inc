// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "AsmOffsets.inc"

.macro NESTED_ENTRY Name, Section, Handler
        LEAF_ENTRY \Name, \Section
        .ifnc \Handler, NoHandler
        .cfi_personality 0x1b, C_FUNC(\Handler) // 0x1b == DW_EH_PE_pcrel | DW_EH_PE_sdata4 (standard across most platforms)
        .endif
.endm

.macro NESTED_END Name, Section
        LEAF_END \Name, \Section
.endm

.macro PATCH_LABEL Name
        .global C_FUNC(\Name)
C_FUNC(\Name):
.endm

.macro ALTERNATE_ENTRY Name
        .global C_FUNC(\Name)
        .hidden C_FUNC(\Name)
C_FUNC(\Name):
.endm

.macro LABELED_RETURN_ADDRESS Name
        .global C_FUNC(\Name)
        .hidden C_FUNC(\Name)
C_FUNC(\Name):
.endm

.macro LEAF_ENTRY Name, Section
    .global C_FUNC(\Name)
    .type \Name, %function
C_FUNC(\Name):
    .cfi_startproc
.endm

.macro LEAF_END Name, Section
    .size \Name, .-\Name
    .cfi_endproc
.endm

.macro PREPARE_EXTERNAL_VAR Name, HelperReg
    la \HelperReg, C_FUNC(\Name) // Resolves the address in one step
.endm

.macro PREPARE_EXTERNAL_VAR_INDIRECT_W Name, HelperReg
    la \HelperReg, C_FUNC(\Name)
    lw \HelperReg, 0(\HelperReg)
.endm

.macro PROLOG_STACK_ALLOC Size
    // If Size is larger than 2047, split it into multiple instructions
    .if (\Size > 2047) || (\Size < -2048)
        li t0, -\Size
        add sp, sp, t0
    .else
        addi sp, sp, -\Size
    .endif
.endm

.macro EPILOG_STACK_FREE Size
    // If Size is larger than 2047 or smaller than -2048, split into multiple instructions
    .if (\Size > 2047) || (\Size < -2048)
        li t0, \Size           // Load the large Size value into a temporary register
        add sp, sp, t0         // Use the add instruction for full 64-bit addition
        .cfi_adjust_cfa_offset -\Size
    .else
        addi sp, sp, \Size     // Handle small immediate directly with addi
        .cfi_adjust_cfa_offset -\Size
    .endif
.endm

.macro EPILOG_STACK_RESTORE
    mv  sp, fp
    .cfi_restore  fp
.endm

.macro PROLOG_SAVE_REG reg, ofs
    sd  \reg, \ofs(sp)
    .cfi_rel_offset \reg, \ofs
.endm

.macro PROLOG_SAVE_REG_PAIR reg1, reg2, ofs
    sd  \reg1, \ofs(sp)
    sd  \reg2, \ofs + 8(sp)
    .cfi_rel_offset  \reg1, \ofs
    .cfi_rel_offset  \reg2, \ofs + 8
    .ifc  \reg1, fp
    mv  fp, sp
    .cfi_def_cfa_register  fp
    .endif
.endm

.macro PROLOG_SAVE_REG_PAIR_INDEXED reg1, reg2, ssize, __def_cfa_save=1
    addi  sp, sp, -\ssize
    .cfi_adjust_cfa_offset  \ssize

    sd  \reg1, 0(sp)
    sd  \reg2, 8(sp)

    .cfi_rel_offset  \reg1, 0
    .cfi_rel_offset  \reg2, 8
    .if (\__def_cfa_save ==  1)
      mv  fp, sp
      .cfi_def_cfa_register  fp
    .endif
.endm

.macro PROLOG_SAVE_REG_PAIR_NO_FP_INDEXED reg1, reg2, ssize
    addi  sp, sp, -\ssize
    .cfi_adjust_cfa_offset  \ssize

    sd  \reg1, 0(sp)
    sd  \reg2, 8(sp)

    .cfi_rel_offset  \reg1, 0
    .cfi_rel_offset  \reg2, 8
.endm

.macro EPILOG_RESTORE_REG reg, ofs
        ld \reg, \ofs(sp)
        .cfi_restore \reg
.endm

.macro EPILOG_RESTORE_REG_PAIR reg1, reg2, ofs
        ld \reg1, \ofs(sp)
        ld \reg2, \ofs+8(sp)
        .cfi_restore \reg1
        .cfi_restore \reg2
.endm

.macro EPILOG_RESTORE_REG_PAIR_INDEXED reg1, reg2, ofs
        ld \reg1, (sp)
        ld \reg2, 8(sp)
        addi sp, sp, \ofs
        .cfi_restore \reg1
        .cfi_restore \reg2
        .cfi_adjust_cfa_offset -\ofs
.endm

.macro EPILOG_RETURN
        ret
.endm

.macro EMIT_BREAKPOINT
        ebreak
.endm

.macro EPILOG_BRANCH_REG reg
        jalr \reg
.endm

// Loads the address of a thread-local variable into the target register.
// The target register cannot be a0.
.macro INLINE_GET_TLS_VAR target, var
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

    mv \target, a0

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

    */
.endm

// Inlined version of RhpGetThread. Target cannot be x0.
.macro INLINE_GETTHREAD target
    INLINE_GET_TLS_VAR \target, C_FUNC(tls_CurrentThread)
.endm

.macro InterlockedOperationBarrier
    fence rw, rw
.endm

.macro INLINE_THREAD_UNHIJACK threadReg, trashReg1, trashReg2
    //
    // Thread::Unhijack()
    //
    ld \trashReg1, OFFSETOF__Thread__m_pvHijackedReturnAddress(\threadReg)
    beqz \trashReg1, 0f

    ld \trashReg2, OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation(\threadReg)
    sd \trashReg1, 0(\trashReg2)
    sd zero, OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation(\threadReg)
    sd zero, OFFSETOF__Thread__m_pvHijackedReturnAddress(\threadReg)
0:
.endm

// Note: these must match the defs in PInvokeTransitionFrameFlags
#define PTFF_SAVE_SP            0x00000800
#define PTFF_SAVE_A0            0x00004000
#define PTFF_SAVE_A1            0x00008000
#define PTFF_SAVE_ALL_PRESERVED 0x000007FF  // NOTE: S1-S11
#define PTFF_THREAD_HIJACK_HI   0x00000001  // upper 32 bits of the PTFF_THREAD_HIJACK

#define DEFAULT_FRAME_SAVE_FLAGS PTFF_SAVE_ALL_PRESERVED + PTFF_SAVE_SP

.macro PUSH_COOP_PINVOKE_FRAME trashReg
    PROLOG_SAVE_REG_PAIR_INDEXED   s0, ra, 128      // Push down stack pointer and store s0 (fp) and RA

    // 16 bytes reserved for Thread* and flags

    // Save callee saved registers
    PROLOG_SAVE_REG_PAIR   s1, s2, 32
    PROLOG_SAVE_REG_PAIR   s3, s4, 48
    PROLOG_SAVE_REG_PAIR   s5, s6, 64
    PROLOG_SAVE_REG_PAIR   s7, s8, 80
    PROLOG_SAVE_REG_PAIR   s9, s10, 96
    PROLOG_SAVE_REG        s11, 112

    // Save the value of SP before stack allocation to the last slot in the frame (slot #15)
    add \trashReg, sp, 128
    sd \trashReg, 120(sp)

    // Record the bitmask of saved registers in the frame (slot #3)
    li \trashReg, DEFAULT_FRAME_SAVE_FLAGS
    sd \trashReg, 24(sp)

    mv \trashReg, sp
.endm

// Pop the frame and restore register state preserved by PUSH_COOP_PINVOKE_FRAME
.macro POP_COOP_PINVOKE_FRAME
    EPILOG_RESTORE_REG_PAIR   s1, s2, 32
    EPILOG_RESTORE_REG_PAIR   s3, s4, 48
    EPILOG_RESTORE_REG_PAIR   s5, s6, 64
    EPILOG_RESTORE_REG_PAIR   s7, s8, 80
    EPILOG_RESTORE_REG_PAIR   s9, s10, 96
    EPILOG_RESTORE_REG        s11, 112
    EPILOG_RESTORE_REG_PAIR_INDEXED   s0, ra, 128
.endm

// Bit position for the flags above, to be used with tbz / tbnz instructions
#define PTFF_THREAD_ABORT_BIT 31

//
// CONSTANTS -- INTEGER
//
#define TSF_Attached                    0x01
#define TSF_SuppressGcStress            0x08
#define TSF_DoNotTriggerGc              0x10
#define TSF_SuppressGcStress__OR__TSF_DoNotTriggerGC 0x18

// Bit position for the flags above, to be used with tbz / tbnz instructions
#define TrapThreadsFlags_AbortInProgress_Bit 0
#define TrapThreadsFlags_TrapThreads_Bit     1

// These must match the TrapThreadsFlags enum
#define TrapThreadsFlags_None            0
#define TrapThreadsFlags_AbortInProgress 1
#define TrapThreadsFlags_TrapThreads     2
