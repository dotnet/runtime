// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <amd64/AsmMacros_Unix.inc>

//
// CONSTANTS -- INTEGER
//
#define TSF_Attached                    0x01
#define TSF_DoNotTriggerGc              0x10

// Note: these must match the defs in PInvokeTransitionFrameFlags
#define PTFF_SAVE_RBX            0x00000001
#define PTFF_SAVE_R12            0x00000010
#define PTFF_SAVE_R13            0x00000020
#define PTFF_SAVE_R14            0x00000040
#define PTFF_SAVE_R15            0x00000080
#define PTFF_SAVE_ALL_PRESERVED  0x000000F1   // NOTE: RBP is not included in this set!
#define PTFF_SAVE_RSP            0x00008000
#define PTFF_SAVE_RSI            0x00000002   // RSI is saved in hijack handler - in case it contains a GC ref
#define PTFF_SAVE_RDI            0x00000004   // RDI is saved in hijack handler - in case it contains a GC ref
#define PTFF_SAVE_RAX            0x00000100   // RAX is saved in hijack handler - in case it contains a GC ref
#define PTFF_SAVE_RCX            0x00000200   // RCX is saved in hijack handler - in case it contains a GC ref
#define PTFF_SAVE_RDX            0x00000400   // RDX is saved in hijack handler - in case it contains a GC ref
#define PTFF_SAVE_R8             0x00000800   // R8 is saved in hijack handler - in case it contains a GC ref
#define PTFF_SAVE_R9             0x00001000   // R9 is saved in hijack handler - in case it contains a GC ref
#define PTFF_SAVE_ALL_SCRATCH    0x00007F00
#define PTFF_THREAD_HIJACK       0x00100000   // indicates that this is a frame for a hijacked call

// These must match the TrapThreadsFlags enum
#define TrapThreadsFlags_None            0
#define TrapThreadsFlags_TrapThreads     1

.macro INLINE_GETTHREAD
#ifdef FEATURE_EMULATED_TLS
        call        C_FUNC(RhpGetThread)
#else
        // Inlined version of call C_FUNC(RhpGetThread)
        INLINE_GET_TLS_VAR tls_CurrentThread
#endif
.endm

.macro INLINE_GET_ALLOC_CONTEXT_BASE
        INLINE_GETTHREAD
.endm

.macro INLINE_THREAD_UNHIJACK threadReg, trashReg1, trashReg2
        //
        // Thread::Unhijack()
        //
        mov         \trashReg1, [\threadReg + OFFSETOF__Thread__m_pvHijackedReturnAddress]
        cmp         \trashReg1, 0
        je          1f

        mov         \trashReg2, [\threadReg + OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation]
        mov         [\trashReg2], \trashReg1
        mov         qword ptr [\threadReg + OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation], 0
        mov         qword ptr [\threadReg + OFFSETOF__Thread__m_pvHijackedReturnAddress], 0

1:
.endm

#define DEFAULT_FRAME_SAVE_FLAGS (PTFF_SAVE_ALL_PRESERVED + PTFF_SAVE_RSP)

.macro PUSH_COOP_PINVOKE_FRAME trashReg
    push_nonvol_reg rbp                         // push RBP frame
    mov             rbp, rsp
    lea             \trashReg, [rsp + 0x10]
    push_register   \trashReg                   // save caller's RSP
    push_nonvol_reg r15                         // save preserved registers
    push_nonvol_reg r14                         //   ..
    push_nonvol_reg r13                         //   ..
    push_nonvol_reg r12                         //   ..
    push_nonvol_reg rbx                         //   ..
    push_imm        DEFAULT_FRAME_SAVE_FLAGS    // save the register bitmask
    push_register   \trashReg                   // Thread * (unused by stackwalker)
    mov             \trashReg, [rsp + 8*8]      // Find and save the callers RBP
    push_register   \trashReg
    mov             \trashReg, [rsp + 10*8]     // Find and save the return address
    push_register   \trashReg
    lea             \trashReg, [rsp]            // trashReg == address of frame
.endm

.macro POP_COOP_PINVOKE_FRAME
    pop_register r10    // discard RIP
    pop_nonvol_reg rbp  // restore RBP
    pop_register r10    // discard thread
    pop_register r10    // discard bitmask
    pop_nonvol_reg rbx
    pop_nonvol_reg r12
    pop_nonvol_reg r13
    pop_nonvol_reg r14
    pop_nonvol_reg r15
    pop_register r10    // discard caller RSP
    pop_register r10    // discard RBP frame
.endm
