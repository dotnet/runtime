// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <arm/AsmMacros_Unix.inc>

//
// CONSTANTS -- INTEGER
//

#define TSF_Attached                    0x01
#define TSF_DoNotTriggerGc              0x10

// These must match the PInvokeTransitionFrameFlags enum
#define PTFF_SAVE_ALL_PRESERVED 0x0000007F  // NOTE: R11 is not included in this set!
#define PTFF_SAVE_R9 0x00000020
#define PTFF_SAVE_SP 0x00000100
#define PTFF_SAVE_R0 0x00000200
#define PTFF_SAVE_R1 0x00000400
#define PTFF_SAVE_R2 0x00000800
#define PTFF_THREAD_HIJACK 0x00004000   // indicates that this is a frame for a hijacked call

#define DEFAULT_FRAME_SAVE_FLAGS (PTFF_SAVE_ALL_PRESERVED + PTFF_SAVE_SP)

// These must match the TrapThreadsFlags enum
#define TrapThreadsFlags_None            0
#define TrapThreadsFlags_TrapThreads     1

// GC minimal sized object. We use this to switch between 4 and 8 byte alignment in the GC heap (see AllocFast.asm).
#define ASM_MIN_OBJECT_SIZE 12

//
// Macro used from unmanaged helpers called from managed code where the helper does not transition immediately
// into pre-emptive mode but may cause a GC and thus requires the stack is crawlable. This is typically the
// case for helpers that meddle in GC state (e.g. allocation helpers) where the code must remain in
// cooperative mode since it handles object references and internal GC state directly but a garbage collection
// may be inevitable. In these cases we need to be able to transition to pre-meptive mode deep within the
// unmanaged code but still be able to initialize the stack iterator at the first stack frame which may hold
// interesting GC references. In all our helper cases this corresponds to the most recent managed frame (e.g.
// the helper's caller).
//
// This macro builds a frame describing the current state of managed code.
//
// INVARIANTS
// - The macro assumes it defines the method prolog, it should typically be the first code in a method and
//   certainly appear before any attempt to alter the stack pointer.
// - This macro uses trashReg (after its initial value has been saved in the frame) and upon exit trashReg
//   will contain the address of transition frame.
//
.macro PUSH_COOP_PINVOKE_FRAME trashReg

        PROLOG_STACK_ALLOC 4          // Save space for caller's SP
        PROLOG_PUSH "{r4-r10}"        // Save preserved registers
        PROLOG_STACK_ALLOC 8          // Save space for flags and Thread*
        PROLOG_PUSH "{r11,lr}"        // Save caller's frame pointer and PC

        // Compute SP value at entry to this method and save it in the last slot of the frame (slot #12).
        add         \trashReg, sp, #(12 * 4)
        str         \trashReg, [sp, #(11 * 4)]

        // Record the bitmask of saved registers in the frame (slot #3).
        mov         \trashReg, #DEFAULT_FRAME_SAVE_FLAGS
        str         \trashReg, [sp, #(3 * 4)]

        mov         \trashReg, sp
.endm

// Pop the frame and restore register state preserved by PUSH_COOP_PINVOKE_FRAME
.macro POP_COOP_PINVOKE_FRAME
        EPILOG_POP  "{r11,lr}"        // Restore caller's frame pointer and PC (return address)
        EPILOG_STACK_FREE 8           // Discard flags and Thread*
        EPILOG_POP  "{r4-r10}"        // Restore preserved registers
        EPILOG_STACK_FREE 4           // Discard caller's SP
.endm

.macro INLINE_GETTHREAD
#ifdef FEATURE_EMULATED_TLS
        bl C_FUNC(RhpGetThread)
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
        ldr         \trashReg1, [\threadReg, #OFFSETOF__Thread__m_pvHijackedReturnAddress]
        cbz         \trashReg1, 1f

        ldr         \trashReg2, [\threadReg, #OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation]
        str         \trashReg1, [\trashReg2]
        mov         \trashReg1, #0
        str         \trashReg1, [\threadReg, #OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation]
        str         \trashReg1, [\threadReg, #OFFSETOF__Thread__m_pvHijackedReturnAddress]

1:
.endm
