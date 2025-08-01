// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "asmconstants.h"
#include "unixasmmacros.inc"

LEAF_ENTRY GetCurrentIP, _TEXT
    ori  $a0, $ra, 0
    jirl  $r0, $ra, 0
LEAF_END GetCurrentIP, _TEXT

// LPVOID __stdcall GetCurrentSP(void)//
LEAF_ENTRY GetCurrentSP, _TEXT
    ori  $a0, $sp, 0
    jirl  $r0, $ra, 0
LEAF_END GetCurrentSP, _TEXT

//-----------------------------------------------------------------------------
// The following Macros help in WRITE_BARRIER Implementations
// WRITE_BARRIER_ENTRY
//
// Declare the start of a write barrier function. Use similarly to NESTED_ENTRY. This is the only legal way
// to declare a write barrier function.
//
.macro WRITE_BARRIER_ENTRY name
    LEAF_ENTRY \name, _TEXT
.endm

// WRITE_BARRIER_END
//
// The partner to WRITE_BARRIER_ENTRY, used like NESTED_END.
//
.macro WRITE_BARRIER_END name
    LEAF_END_MARKED \name, _TEXT
.endm

// void JIT_UpdateWriteBarrierState(bool skipEphemeralCheck, size_t writeableOffset)
//
// Update shadow copies of the various state info required for barrier
//
// State info is contained in a literal pool at the end of the function
// Placed in text section so that it is close enough to use ldr literal and still
// be relocatable. Eliminates need for PREPARE_EXTERNAL_VAR in hot code.
//
// Align and group state info together so it fits in a single cache line
// and each entry can be written atomically
//
WRITE_BARRIER_ENTRY JIT_UpdateWriteBarrierState

    // $a0-$a7,$t3 will contain intended new state
    // $t0 will preserve skipEphemeralCheck
    // $t2 will be used for pointers

    ori  $t0, $a0, 0
    ori  $t1, $a1, 0

    la.local  $a0, g_card_table
    ld.d  $a0, $a0, 0

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    la.local  $a1, g_card_bundle_table
    ld.d  $a1, $a1, 0
#endif

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    la.local  $a2, g_write_watch_table
    ld.d  $a2, $a2, 0
#endif

    la.local  $a3, g_ephemeral_low
    ld.d  $a3, $a3, 0

    la.local  $a4, g_ephemeral_high
    ld.d  $a4, $a4, 0

    beq  $t0, $zero, LOCAL_LABEL(EphemeralCheckEnabled)

    ori  $a3, $zero, 0
    addi.d  $a4, $zero, -1
LOCAL_LABEL(EphemeralCheckEnabled):

    la.local  $a5, g_lowest_address
    ld.d  $a5, $a5, 0

    la.local  $a6, g_highest_address
    ld.d  $a6, $a6, 0

#ifdef WRITE_BARRIER_CHECK
    la.local  $a7, g_GCShadow
    ld.d  $a7, $a7, 0

    la.local  $t3, g_GCShadowEnd
    ld.d  $t3, $t3, 0
#endif

    // Update wbs state
    la.local  $t2, JIT_WriteBarrier_Table_Loc
    ld.d  $t2, $t2, 0
    add.d $t2,$t2,$t1

    st.d  $a0, $t2, 0
    st.d  $a1, $t2, 8
    st.d  $a2, $t2, 16
    st.d  $a3, $t2, 24
    st.d  $a4, $t2, 32
    st.d  $a5, $t2, 40
    st.d  $a6, $t2, 48
    st.d  $a7, $t2, 56
    st.d  $t3, $t2, 64

    EPILOG_RETURN

WRITE_BARRIER_END JIT_UpdateWriteBarrierState

// ----------------------------------------------------------------------------------------
// __declspec(naked) void F_CALL_CONV JIT_WriteBarrier_Callable(Object **dst, Object* val)
LEAF_ENTRY  JIT_WriteBarrier_Callable, _TEXT

    // Setup args for JIT_WriteBarrier. $t0 = dst ; $t1 = val
    ori     $t6, $a0, 0                 // $t6 = dst
    ori     $t7, $a1, 0                 // $t7 = val

    // Branch to the write barrier
    la.local  $r21, JIT_WriteBarrier_Loc
    ld.d  $r21, $r21, 0
    jirl  $r0, $r21, 0
LEAF_END JIT_WriteBarrier_Callable, _TEXT


.balign 64  // Align to power of two at least as big as patchable literal pool so that it fits optimally in cache line
// ------------------------------------------------------------------
// Start of the writeable code region
LEAF_ENTRY JIT_PatchedCodeStart, _TEXT
    jirl  $r0, $ra, 0
LEAF_END JIT_PatchedCodeStart, _TEXT

// void JIT_ByRefWriteBarrier
//
// On entry:
//   t8 : the source address (points to object reference to write)
//   t6: the destination address (object reference written here)
//
// On exit:
//   t8  : incremented by 8
//   t7  : trashed
//
WRITE_BARRIER_ENTRY JIT_ByRefWriteBarrier
    ld.d  $t7, $t8, 0
    addi.d  $t8, $t8, 8
    b  C_FUNC(JIT_CheckedWriteBarrier)
WRITE_BARRIER_END JIT_ByRefWriteBarrier

//-----------------------------------------------------------------------------
// Simple WriteBarriers
// void JIT_CheckedWriteBarrier(Object** dst, Object* src)
//
// On entry:
//   t6 : the destination address (LHS of the assignment)
//   t7 : the object reference (RHS of the assignment)
//
// On exit:
//   $t1  : trashed
//   $t0  : trashed
//   $t3  : trashed
//   $t4  : trashed
//   $t6  : trashed (incremented by 8 to implement JIT_ByRefWriteBarrier contract)
//
WRITE_BARRIER_ENTRY JIT_CheckedWriteBarrier

    // load the address wbs_card_table to $t3
.Ltmp0:
    pcaddi $t3, 0
    .reloc .Ltmp0, R_LARCH_PCREL20_S2, wbs_card_table
    // wbs_highest_address = wbs_card_table + 48
    ld.d  $t1, $t3, 48
    // wbs_lowest_address  = wbs_card_table + 40
    ld.d  $t3, $t3, 40
    slt  $t4, $t6, $t3

    slt  $t0, $t1, $t6
    or  $t4, $t0, $t4
    beq  $t4, $zero, C_FUNC(JIT_WriteBarrier)

    st.d  $t7, $t6, 0
    addi.d  $t6, $t6, 8
    jirl  $r0, $ra, 0
WRITE_BARRIER_END JIT_CheckedWriteBarrier

// void JIT_WriteBarrier(Object** dst, Object* src)
// On entry:
//   t6  : the destination address (LHS of the assignment)
//   t7  : the object reference (RHS of the assignment)
//
// On exit:
//   $t0  : trashed
//   $t1  : trashed
//   $t3  : trashed
//   $t4  : trashed
//   $t6  : trashed (incremented by 8 to implement JIT_ByRefWriteBarrier contract)
//   $t7  : trashed
//
WRITE_BARRIER_ENTRY JIT_WriteBarrier

    dbar 0 // TODO: sync_release (runtime detection required)

    st.d  $t7, $t6, 0

    // load the address wbs_card_table to the $t3
    // and will cache the wbs_card_table by $t3 within the JIT_WriteBarrier.
    // So please DONT'T overwrite the $t3 within the JIT_WriteBarrier !!!
.Ltmp1:
    pcaddi $t3, 0
    .reloc .Ltmp1, R_LARCH_PCREL20_S2, wbs_card_table

#ifdef WRITE_BARRIER_CHECK
    // Update GC Shadow Heap

    // the wbs_GCShadow = $t3 + 56
    ld.d $t1, $t3, 56

    // Do not perform the work if g_GCShadow is 0
    beq  $t1, $zero, 22f //LOCAL_LABEL(ShadowUpdateDisabled)

    // Compute address of shadow heap location:
    //   pShadow = g_GCShadow + ($t6 - g_lowest_address)
    // the wbs_lowest_address = $t3 + 40
    ld.d $t0, $t3, 40

    sub.d  $t0, $t6, $t0
    add.d  $t0, $t0, $t1

    // if (pShadow >= g_GCShadowEnd) goto end.
    // load the wbs_GCShadowEnd = $t3 + 64
    ld.d $t1, $t3, 64

    slt  $t4, $t0, $t1
    beq  $t4, $zero, 22f //LOCAL_LABEL(ShadowUpdateEnd)

    // *pShadow = $t7
    st.d  $t7, $t0, 0

    // Ensure that the write to the shadow heap occurs before the read from the GC heap so that race
    // conditions are caught by INVALIDGCVALUE.
    dbar 0

    // if (*$t6 == $t7) goto end
    ld.d  $t1, $t6, 0
    beq  $t1, $t7, 22f //LOCAL_LABEL(ShadowUpdateEnd)

    // *pShadow = INVALIDGCVALUE (0xcccccccd)
    //lu12i.w  $t1, 0xccccc
    lu12i.w  $t1, -209716
    ori  $t1, $t1, 0xccd
    st.d $t1, $t0, 0
22:
//LOCAL_LABEL(ShadowUpdateEnd):
//LOCAL_LABEL(ShadowUpdateDisabled):
#endif

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    // Update the write watch table if necessary

    // the wbs_sw_ww_table = $t3 + 16
    ld.d  $t4, $t3, 16
    beq  $t4, $zero, 1f //LOCAL_LABEL(CheckCardTable)

    srli.d  $t0, $t6, 0xc
    add.d  $t4, $t4, $t0  // SoftwareWriteWatch::AddressToTableByteIndexShift
    ld.b  $t0, $t4, 0
    bne  $t0, $zero, 1f //LOCAL_LABEL(CheckCardTable)

    ori  $t0, $zero, 0xFF
    st.b  $t0, $t4, 0
1:
//LOCAL_LABEL(CheckCardTable):
#endif
    // Branch to Exit if the reference is not in the Gen0 heap
    // the wbs_ephemeral_low = $t3 + 24
    ld.d  $t0, $t3, 24
    beq  $t0, $zero, LOCAL_LABEL(SkipEphemeralCheck)

    slt  $t4, $t7, $t0
    // the wbs_ephemeral_high = $t3 + 32
    ld.d  $t1, $t3, 32
    slt  $t0, $t1, $t7
    or  $t4, $t0, $t4
    bne  $t4, $zero, LOCAL_LABEL(Exit)
LOCAL_LABEL(SkipEphemeralCheck):
    // Check if we need to update the card table
    // the wbs_card_table = $t3
    ld.d  $t0, $t3, 0
    srli.d  $t4, $t6, 11
    add.d  $t7, $t0, $t4
    ld.bu  $t1, $t7, 0
    ori  $t4, $zero, 0xFF
    beq  $t1, $t4, LOCAL_LABEL(Exit)

    st.b  $t4, $t7, 0

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    // Check if we need to update the card bundle table
    // the wbs_card_bundle_table = $t3 + 8
    ld.d  $t0, $t3, 8
    srli.d  $t4, $t6, 21
    add.d  $t7, $t0, $t4

    ld.bu  $t1, $t7, 0
    ori  $t4, $zero, 0xFF
    beq  $t1, $t4, LOCAL_LABEL(Exit)

    st.b  $t4, $t7, 0
#endif
LOCAL_LABEL(Exit):
    addi.d  $t6, $t6, 8
    jirl  $r0, $ra, 0
WRITE_BARRIER_END JIT_WriteBarrier


// Begin patchable literal pool
    .balign 64  // Align to power of two at least as big as patchable literal pool so that it fits optimally in cache line
WRITE_BARRIER_ENTRY JIT_WriteBarrier_Table
wbs_card_table:
    .quad 0
//wbs_card_bundle_table: =wbs_card_table+8
    .quad 0
//wbs_sw_ww_table:       =wbs_card_table+16
    .quad 0
//wbs_ephemeral_low:     =wbs_card_table+24
    .quad 0
//wbs_ephemeral_high:    =wbs_card_table+32
    .quad 0
//wbs_lowest_address:    =wbs_card_table+40
    .quad 0
//wbs_highest_address:   =wbs_card_table+48
    .quad 0
//wbs_GCShadow:          =wbs_card_table+56
    .quad 0
//wbs_GCShadowEnd:       =wbs_card_table+64
    .quad 0
WRITE_BARRIER_END JIT_WriteBarrier_Table

// ------------------------------------------------------------------
// End of the writeable code region
LEAF_ENTRY JIT_PatchedCodeLast, _TEXT
    jirl  $r0, $ra, 0
LEAF_END JIT_PatchedCodeLast, _TEXT

NESTED_ENTRY ThePreStub, _TEXT, NoHandler
    PROLOG_WITH_TRANSITION_BLOCK

    ori  $a1, $METHODDESC_REGISTER, 0 // pMethodDesc

    addi.d $a0, $sp, __PWTB_TransitionBlock        // pTransitionBlock
    bl  PreStubWorker
    ori  $t4,$a0,0

    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
    EPILOG_BRANCH_REG  $t4
NESTED_END ThePreStub, _TEXT

// ------------------------------------------------------------------
// The call in PInvokeImportPrecode points to this function.
NESTED_ENTRY PInvokeImportThunk, _TEXT, NoHandler

    //                            $fp,$ra
    PROLOG_SAVE_REG_PAIR_INDEXED  22, 1, 0xa0
    SAVE_ARGUMENT_REGISTERS  $sp, 0x20
    SAVE_FLOAT_ARGUMENT_REGISTERS  $sp, 0x60

    ori  $a0, $t2, 0
    bl C_FUNC(PInvokeImportWorker)
    ori  $t4,$a0,0

    // pop the stack and restore original register state
    RESTORE_FLOAT_ARGUMENT_REGISTERS  $sp, 0x60
    RESTORE_ARGUMENT_REGISTERS  $sp, 0x20
    //                              $fp,$ra
    EPILOG_RESTORE_REG_PAIR_INDEXED  22, 1, 0xa0

    // If we got back from PInvokeImportWorker, the MD has been successfully
    // linked. Proceed to execute the original DLL call.
    EPILOG_BRANCH_REG  $t4
NESTED_END PInvokeImportThunk, _TEXT

#ifdef FEATURE_PREJIT
//------------------------------------------------
// VirtualMethodFixupStub
//
// In NGEN images, virtual slots inherited from cross-module dependencies
// point to a jump thunk that calls into the following function that will
// call into a VM helper. The VM helper is responsible for patching up
// thunk, upon executing the precode, so that all subsequent calls go directly
// to the actual method body.
//
// This is done lazily for performance reasons.
//
// On entry:
//
// $a0 = "this" pointer
// $t2 = Address of thunk

NESTED_ENTRY VirtualMethodFixupStub, _TEXT, NoHandler

    // Save arguments and return address
    //                            $fp,$ra
    PROLOG_SAVE_REG_PAIR_INDEXED  22, 1, 0xa0
    SAVE_ARGUMENT_REGISTERS  $sp, 32
    SAVE_FLOAT_ARGUMENT_REGISTERS  $sp, 96


    // Call the helper in the VM to perform the actual fixup
    // and tell us where to tail call. $a0 already contains
    // the this pointer.

    // Move the thunk start address in $a1
    ori  $a1, $t2, 0
    bl VirtualMethodFixupWorker
    ori  $t4,$a0,0

    // On return, a0 contains the target to tailcall to

    // pop the stack and restore original register state
    RESTORE_FLOAT_ARGUMENT_REGISTERS  $sp, 96
    RESTORE_ARGUMENT_REGISTERS  $sp, 32
    //                              $fp,$ra
    EPILOG_RESTORE_REG_PAIR_INDEXED  22, 1, 0xa0

    PATCH_LABEL  VirtualMethodFixupPatchLabel

    // and tailcall to the actual method
    EPILOG_BRANCH_REG  $t4
NESTED_END VirtualMethodFixupStub, _TEXT
#endif // FEATURE_PREJIT

// ------------------------------------------------------------------
// ThePreStubPatch()
LEAF_ENTRY ThePreStubPatch, _TEXT
.globl C_FUNC(ThePreStubPatchLabel)
C_FUNC(ThePreStubPatchLabel):
    jirl  $r0, $ra, 0
LEAF_END ThePreStubPatch, _TEXT

#ifdef FEATURE_PREJIT
// ------------------------------------------------------------------
// void StubDispatchFixupStub(args in regs $a0-$a7 & stack, $t1:IndirectionCellAndFlags)
//
// The stub dispatch thunk which transfers control to StubDispatchFixupWorker.
NESTED_ENTRY StubDispatchFixupStub, _TEXT, NoHandler

    PROLOG_WITH_TRANSITION_BLOCK

    srli.d  $a1, $t8, 2
    slli.d  $a1, $a1, 2 // Indirection cell

    addi.d $a0, $sp, __PWTB_TransitionBlock        // pTransitionBlock
    ori  $a2, $zero, 0 // sectionIndex
    ori  $a3, $zero, 0 // pModule
    bl StubDispatchFixupWorker
    ori  $t4,$a0,0

    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
    PATCH_LABEL StubDispatchFixupPatchLabel
    EPILOG_BRANCH_REG  $t4
NESTED_END StubDispatchFixupStub, _TEXT
#endif

//
// $t2 = UMEntryThunkData*
//
NESTED_ENTRY TheUMEntryPrestub, _TEXT, UnhandledExceptionHandlerUnix

    // Save arguments and return address
    //                            $fp,$ra
    PROLOG_SAVE_REG_PAIR_INDEXED  22, 1, 0xa0
    SAVE_ARGUMENT_REGISTERS  $sp, 32
    SAVE_FLOAT_ARGUMENT_REGISTERS  $sp, 96


    ori  $a0, $t2, 0
    bl  TheUMEntryPrestubWorker
    ori  $t4,$a0,0

    // pop the stack and restore original register state
    RESTORE_FLOAT_ARGUMENT_REGISTERS  $sp, 96
    RESTORE_ARGUMENT_REGISTERS  $sp, 32
    //                              $fp,$ra
    EPILOG_RESTORE_REG_PAIR_INDEXED  22, 1, 0xa0

    // and tailcall to the actual method
    EPILOG_BRANCH_REG $t4
NESTED_END TheUMEntryPrestub, _TEXT

// Make sure the `FaultingExceptionFrame_StackAlloc` is 16-byte aligned.
#define FaultingExceptionFrame_StackAlloc (SIZEOF__FaultingExceptionFrame)
#define FaultingExceptionFrame_FrameOffset 0

.macro GenerateRedirectedStubWithFrame stub, target

    //
    // This is the primary function to which execution will be redirected to.
    //
    NESTED_ENTRY \stub, _TEXT, NoHandler

        //
        // IN: ra: original IP before redirect
        //

        PROLOG_SAVE_REG_PAIR_INDEXED  22, 1, 16

        // alloc stack for FaultingExceptionFrame.
        addi.d  $sp, $sp, -FaultingExceptionFrame_StackAlloc

        // stack must be 16 bytes aligned
        CHECK_STACK_ALIGNMENT

        // Save pointer to FEF for GetFrameFromRedirectedStubStackFrame
        addi.d  $a0, $sp, FaultingExceptionFrame_FrameOffset

        // Prepare to initialize to NULL
        st.d    $r0, $a0, 0  // Initialize vtbl (it is not strictly necessary)
        st.d    $r0, $a0, FaultingExceptionFrame__m_fFilterExecuted  // Initialize BOOL for personality routine

        bl   C_FUNC(\target)
        // Target should not return.
        EMIT_BREAKPOINT

    NESTED_END \stub, _TEXT

.endm

GenerateRedirectedStubWithFrame RedirectForThreadAbort, ThrowControlForThread

// ------------------------------------------------------------------
// ResolveWorkerChainLookupAsmStub
//
// This method will perform a quick chained lookup of the entry if the
//  initial cache lookup fails.
//
// On Entry:
//   $t1       contains the pointer to the current ResolveCacheElem
//   t8       contains the address of the indirection (and the flags in the low two bits)
//   $t2       contains our contract the DispatchToken
// Must be preserved:
//   $a0       contains the instance object ref that we are making an interface call on
//   $t1       Must point to a ResolveCacheElem [For Sanity]
//  [$a1-$a7]   contains any additional register arguments for the interface method
//
// Loaded from $a0
//   $t3       contains our type     the MethodTable  (from object ref in $a0)
//
// On Exit:
//   $a0, [$a1-$a7] arguments for the interface implementation target
//
// On Exit (to ResolveWorkerAsmStub):
//   t8       contains the address of the indirection and the flags in the low two bits.
//   $t2       contains our contract (DispatchToken)
//   t4 will be trashed
//

#define BACKPATCH_FLAG      1
#define PROMOTE_CHAIN_FLAG  2

NESTED_ENTRY ResolveWorkerChainLookupAsmStub, _TEXT, NoHandler
    andi  $t4, $t8, BACKPATCH_FLAG     // First we check if t8 has the BACKPATCH_FLAG set
    bne  $t4, $zero, LOCAL_LABEL(Fail) // If the BACKPATCH_FLAGS is set we will go directly to the ResolveWorkerAsmStub

    ld.d  $t3, $a0, 0         // retrieve the MethodTable from the object ref in $a0
LOCAL_LABEL(MainLoop):
    ld.d  $t1, $t1, ResolveCacheElem__pNext     // $t1 <= the next entry in the chain
    beq  $t1, $zero, LOCAL_LABEL(Fail)

    ld.d  $t4, $t1, 0
    // compare our MT with the one in the ResolveCacheElem
    bne  $t4, $t3, LOCAL_LABEL(MainLoop)

    ld.d  $t4, $t1, 8
    // compare our DispatchToken with one in the ResolveCacheElem
    bne  $t2, $t4, LOCAL_LABEL(MainLoop)

LOCAL_LABEL(Success):
    PREPARE_EXTERNAL_VAR  g_dispatch_cache_chain_success_counter, $t3
    ld.d  $t4, $t3, 0
    addi.d $t4, $t4, -1
    st.d  $t4, $t3, 0
    blt $t4, $zero, LOCAL_LABEL(Promote)

    ld.d  $t4, $t1, ResolveCacheElem__target    // get the ImplTarget
    jirl  $r0, $t4, 0                // branch to interface implementation target

LOCAL_LABEL(Promote):
                          // Move this entry to head position of the chain
    addi.d  $t4, $zero, 256
    st.d  $t4, $t3, 0         // be quick to reset the counter so we don't get a bunch of contending threads
    ori  $t8, $t8, PROMOTE_CHAIN_FLAG   // set PROMOTE_CHAIN_FLAG
    ori  $t2, $t1, 0           // We pass the ResolveCacheElem to ResolveWorkerAsmStub instead of the DispatchToken

LOCAL_LABEL(Fail):
    b       C_FUNC(ResolveWorkerAsmStub) // call the ResolveWorkerAsmStub method to transition into the VM
NESTED_END ResolveWorkerChainLookupAsmStub, _TEXT

// ------------------------------------------------------------------
// void ResolveWorkerAsmStub(args in regs $a0-$a7 & stack, t8:IndirectionCellAndFlags, $t2:DispatchToken)
//
// The stub dispatch thunk which transfers control to VSD_ResolveWorker.
NESTED_ENTRY ResolveWorkerAsmStub, _TEXT, NoHandler

    PROLOG_WITH_TRANSITION_BLOCK

    ori  $a2, $t2, 0                 // DispatchToken
    addi.d  $a0, $sp, __PWTB_TransitionBlock        // pTransitionBlock
    srli.d  $a1, $t8, 2
    andi  $a3, $t8, 3              // flag
    slli.d  $a1, $a1, 2
    bl C_FUNC(VSD_ResolveWorker)
    ori  $t4,$a0,0

    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL

    EPILOG_BRANCH_REG  $t4
NESTED_END ResolveWorkerAsmStub, _TEXT

#ifdef FEATURE_HIJACK
// ------------------------------------------------------------------
// Hijack function for functions which return a scalar type or a struct (value type)
NESTED_ENTRY OnHijackTripThread, _TEXT, NoHandler
    //                             $fp,$ra
    PROLOG_SAVE_REG_PAIR_INDEXED   22, 1, 0x80

    // Spill callee saved registers
    //                    $s0,$s1
    PROLOG_SAVE_REG_PAIR   23, 24, 16
    PROLOG_SAVE_REG_PAIR   25, 26, 32
    PROLOG_SAVE_REG_PAIR   27, 28, 48
    PROLOG_SAVE_REG_PAIR   29, 30, 64
    PROLOG_SAVE_REG        31,     80

    // save any integral return value(s)
    st.d  $a0, $sp, 88
    st.d  $a1, $sp, 96

    // save any FP return value(s)
    fst.d  $f0, $sp, 104
    fst.d  $f1, $sp, 112

    ori  $a0, $sp, 0
    bl C_FUNC(OnHijackWorker)

    // restore callee saved registers

    // restore any integral return value(s)
    ld.d  $a0, $sp, 88
    ld.d  $a1, $sp, 96

    // restore any FP return value(s)
    fld.d  $f0, $sp, 104
    fld.d  $f1, $sp, 112

    EPILOG_RESTORE_REG_PAIR   23, 24, 16
    EPILOG_RESTORE_REG_PAIR   25, 26, 32
    EPILOG_RESTORE_REG_PAIR   27, 28, 48
    EPILOG_RESTORE_REG_PAIR   29, 30, 64
    EPILOG_RESTORE_REG        31,     80
    //                              $fp,$ra
    EPILOG_RESTORE_REG_PAIR_INDEXED  22, 1, 0x80
    EPILOG_RETURN
NESTED_END OnHijackTripThread, _TEXT

#endif // FEATURE_HIJACK

// ------------------------------------------------------------------
// Redirection Stub for GC in fully interruptible method
//GenerateRedirectedHandledJITCaseStub GCThreadControl
// ------------------------------------------------------------------
//GenerateRedirectedHandledJITCaseStub DbgThreadControl
// ------------------------------------------------------------------
//GenerateRedirectedHandledJITCaseStub UserSuspend

#ifdef _DEBUG
// ------------------------------------------------------------------
// Redirection Stub for GC Stress
GenerateRedirectedHandledJITCaseStub GCStress
#endif


// ------------------------------------------------------------------
// This helper enables us to call into a funclet after restoring Fp register
NESTED_ENTRY CallEHFunclet, _TEXT, NoHandler
    // On entry:
    //
    // $a0 = throwable
    // $a1 = PC to invoke
    // $a2 = address of CONTEXT record; used to restore the non-volatile registers of CrawlFrame
    // $a3 = address of the location where the SP of funclet's caller (i.e. this helper) should be saved.
    //

    //                            $fp,$ra
    PROLOG_SAVE_REG_PAIR_INDEXED   22, 1, 96, 0

    // Spill callee saved registers
    //                    $s0,$s1
    PROLOG_SAVE_REG_PAIR   23, 24, 16
    PROLOG_SAVE_REG_PAIR   25, 26, 32
    PROLOG_SAVE_REG_PAIR   27, 28, 48
    PROLOG_SAVE_REG_PAIR   29, 30, 64
    PROLOG_SAVE_REG        31,     80

    // Save the SP of this function
    st.d  $sp, $a3, 0

    ld.d  $fp, $a2, OFFSETOF__CONTEXT__Fp
    ld.d  $s0, $a2, OFFSETOF__CONTEXT__S0
    ld.d  $s1, $a2, OFFSETOF__CONTEXT__S0+8
    ld.d  $s2, $a2, OFFSETOF__CONTEXT__S0+16
    ld.d  $s3, $a2, OFFSETOF__CONTEXT__S0+24
    ld.d  $s4, $a2, OFFSETOF__CONTEXT__S0+32
    ld.d  $s5, $a2, OFFSETOF__CONTEXT__S0+40
    ld.d  $s6, $a2, OFFSETOF__CONTEXT__S0+48
    ld.d  $s7, $a2, OFFSETOF__CONTEXT__S0+56
    ld.d  $s8, $a2, OFFSETOF__CONTEXT__S0+64

    // Invoke the funclet
    jirl $ra, $a1, 0

    EPILOG_RESTORE_REG_PAIR   23, 24, 16
    EPILOG_RESTORE_REG_PAIR   25, 26, 32
    EPILOG_RESTORE_REG_PAIR   27, 28, 48
    EPILOG_RESTORE_REG_PAIR   29, 30, 64
    EPILOG_RESTORE_REG        31,     80
    //                              $fp,$ra
    EPILOG_RESTORE_REG_PAIR_INDEXED   22, 1, 96
    EPILOG_RETURN

NESTED_END CallEHFunclet, _TEXT

// This helper enables us to call into a filter funclet by passing it the CallerSP to lookup the
// frame pointer for accessing the locals in the parent method.
NESTED_ENTRY CallEHFilterFunclet, _TEXT, NoHandler
    //                             $fp,$ra
    PROLOG_SAVE_REG_PAIR_INDEXED   22, 1, 16, 0

    // On entry:
    //
    // $a0 = throwable
    // $a1 = FP of main function
    // $a2 = PC to invoke
    // $a3 = address of the location where the SP of funclet's caller (i.e. this helper) should be saved.
    //
    // Save the SP of this function
    st.d  $sp, $a3, 0
    // Restore frame pointer
    move  $fp, $a1
    // Invoke the filter funclet
    jirl  $ra, $a2, 0

    EPILOG_RESTORE_REG_PAIR_INDEXED   22, 1, 16
    EPILOG_RETURN
NESTED_END CallEHFilterFunclet, _TEXT

#ifdef FEATURE_COMINTEROP
// Function used by COM interop to get floating point return value (since it's not in the same
// register(s) as non-floating point values).
//
// On entry//
//   $a0          : size of the FP result (4 or 8 bytes)
//   $a1          : pointer to 64-bit buffer to receive result
//
// On exit:
//   buffer pointed to by $a1 on entry contains the float or double argument as appropriate
//
LEAF_ENTRY getFPReturn, _TEXT
    fst.d  $f0, $a1, 0
LEAF_END getFPReturn, _TEXT

// ------------------------------------------------------------------
// Function used by COM interop to set floating point return value (since it's not in the same
// register(s) as non-floating point values).
//
// On entry:
//   $a0          : size of the FP result (4 or 8 bytes)
//   $a1          : 32-bit or 64-bit FP result
//
// On exit:
//   f0          : float result if x0 == 4
//   f0          : double result if x0 == 8
//
LEAF_ENTRY setFPReturn, _TEXT
    movgr2fr.d  $f0, $a1
LEAF_END setFPReturn, _TEXT

#endif // FEATURE_COMINTEROP

// ------------------------------------------------------------------
// void* JIT_GetDynamicNonGCStaticBase(DynamicStaticsInfo* pStaticsInfo)

LEAF_ENTRY JIT_GetDynamicNonGCStaticBase_SingleAppDomain, _TEXT
    // If class is not initialized, bail to C++ helper
    ld.d $a1, $a0, OFFSETOF__DynamicStaticsInfo__m_pNonGCStatics
    dbar 0
    bnez $a1, LOCAL_LABEL(JIT_GetDynamicNonGCStaticBase_SingleAppDomain_CallHelper)
    ori  $a0, $a1, 0
    jirl $r0, $ra, 0

LOCAL_LABEL(JIT_GetDynamicNonGCStaticBase_SingleAppDomain_CallHelper):
    // Tail call GetNonGCStaticBase
    ld.d $a0, $a0, OFFSETOF__DynamicStaticsInfo__m_pMethodTable
    PREPARE_EXTERNAL_VAR g_pGetNonGCStaticBase, $t4
    ld.d $t4, $t4, 0
    EPILOG_BRANCH_REG        $t4
LEAF_END JIT_GetDynamicNonGCStaticBase_SingleAppDomain, _TEXT

// ------------------------------------------------------------------
// void* JIT_GetDynamicGCStaticBase(DynamicStaticsInfo* pStaticsInfo)

LEAF_ENTRY JIT_GetDynamicGCStaticBase_SingleAppDomain, _TEXT
    // If class is not initialized, bail to C++ helper
    ld.d $a1, $a0, OFFSETOF__DynamicStaticsInfo__m_pGCStatics
    dbar 0
    bnez $a1, LOCAL_LABEL(JIT_GetDynamicGCStaticBase_SingleAppDomain_CallHelper)
    ori  $a0, $a1, 0
    jirl $r0, $ra, 0

LOCAL_LABEL(JIT_GetDynamicGCStaticBase_SingleAppDomain_CallHelper):
    // Tail call GetGCStaticBase
    ld.d $a0, $a0, OFFSETOF__DynamicStaticsInfo__m_pMethodTable
    PREPARE_EXTERNAL_VAR g_pGetGCStaticBase, $t4
    ld.d $t4, $t4, 0
    EPILOG_BRANCH_REG        $t4
LEAF_END JIT_GetDynamicGCStaticBase_SingleAppDomain, _TEXT

#ifdef FEATURE_READYTORUN

NESTED_ENTRY DelayLoad_MethodCall_FakeProlog, _TEXT, NoHandler
C_FUNC(DelayLoad_MethodCall):
    .global C_FUNC(DelayLoad_MethodCall)
    PROLOG_WITH_TRANSITION_BLOCK

    ori $a1, $t8, 0      // Indirection cell
    ori $a2, $t0, 0      // sectionIndex
    ori $a3, $t1, 0      // Module*

    addi.d  $a0, $sp, __PWTB_TransitionBlock        // pTransitionBlock
    bl C_FUNC(ExternalMethodFixupWorker)
    ori  $t4,$a0,0

    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
    EPILOG_BRANCH_REG   $t4
NESTED_END DelayLoad_MethodCall_FakeProlog, _TEXT


.macro DynamicHelper frameFlags, suffix
NESTED_ENTRY DelayLoad_Helper\suffix\()_FakeProlog, _TEXT, NoHandler
DelayLoad_Helper\suffix:
    .global DelayLoad_Helper\suffix

    PROLOG_WITH_TRANSITION_BLOCK

    //DynamicHelperWorker(TransitionBlock * pTransitionBlock, TADDR * pCell,
    //                    DWORD sectionIndex, Module * pModule, INT frameFlags)
    ori  $a1, $t8, 0      // Indirection cell
    ori  $a2, $t0, 0      // sectionIndex
    ori  $a3, $t1, 0      // Module*
    ori  $a4, $r0, \frameFlags

    addi.d  $a0, $sp, __PWTB_TransitionBlock        // pTransitionBlock
    bl  DynamicHelperWorker

    bne $a0, $r0, LOCAL_LABEL(FakeProlog\suffix\()_0)

    ld.d  $a0, $sp, __PWTB_ArgumentRegisters
    EPILOG_WITH_TRANSITION_BLOCK_RETURN
LOCAL_LABEL(FakeProlog\suffix\()_0):
    ori  $t4,$a0,0
    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
    EPILOG_BRANCH_REG  $t4

NESTED_END DelayLoad_Helper\suffix\()_FakeProlog, _TEXT
.endm

DynamicHelper DynamicHelperFrameFlags_Default
DynamicHelper DynamicHelperFrameFlags_ObjectArg, _Obj
DynamicHelper DynamicHelperFrameFlags_ObjectArg | DynamicHelperFrameFlags_ObjectArg2, _ObjObj
#endif


#ifdef PROFILING_SUPPORTED

// ------------------------------------------------------------------
LEAF_ENTRY JIT_ProfilerEnterLeaveTailcallStub, _TEXT
    jirl  $r0, $ra, 0
LEAF_END JIT_ProfilerEnterLeaveTailcallStub, _TEXT

// ------------------------------------------------------------------
.macro GenerateProfileHelper helper, flags
NESTED_ENTRY \helper\()Naked, _TEXT, NoHandler
    // On entry:
    //   $t1 = functionIDOrClientID
    //   $t2 = profiledSp
    //   $t3 = throwable
    //
    // On exit:
    //   Values of $a0-$a7, $fa0-$fa7, $fp are preserved.
    //   Values of other volatile registers are not preserved.

    //                          $fp,$ra
    PROLOG_SAVE_REG_PAIR_INDEXED 22, 1, SIZEOF__PROFILE_PLATFORM_SPECIFIC_DATA // Allocate space and save Fp, Pc.

    SAVE_ARGUMENT_REGISTERS       $sp, PROFILE_PLATFORM_SPECIFIC_DATA__argumentRegisters
    SAVE_FLOAT_ARGUMENT_REGISTERS $sp, PROFILE_PLATFORM_SPECIFIC_DATA__floatArgumentRegisters
    st.d    $zero, $sp, PROFILE_PLATFORM_SPECIFIC_DATA__functionId
    addi.d  $t3, $sp, SIZEOF__PROFILE_PLATFORM_SPECIFIC_DATA // Compute probeSp - initial value of Sp on entry to the helper.
    st.d    $t3, $sp, PROFILE_PLATFORM_SPECIFIC_DATA__probeSp
    st.d    $t2, $sp, PROFILE_PLATFORM_SPECIFIC_DATA__profiledSp

    st.d    $zero, $sp, PROFILE_PLATFORM_SPECIFIC_DATA__hiddenArg
    addi.w  $t3, $zero, \flags
    st.w    $t3, $sp, PROFILE_PLATFORM_SPECIFIC_DATA__flags

    ori     $a0, $t1, 0
    ori     $a1, $sp, 0
    bl      C_FUNC(\helper)

    RESTORE_ARGUMENT_REGISTERS       $sp, PROFILE_PLATFORM_SPECIFIC_DATA__argumentRegisters
    RESTORE_FLOAT_ARGUMENT_REGISTERS $sp, PROFILE_PLATFORM_SPECIFIC_DATA__floatArgumentRegisters
    //                             $fp, $ra
    EPILOG_RESTORE_REG_PAIR_INDEXED 22, 1, SIZEOF__PROFILE_PLATFORM_SPECIFIC_DATA
    EPILOG_RETURN

NESTED_END \helper\()Naked, _TEXT
.endm

GenerateProfileHelper ProfileEnter, PROFILE_ENTER
GenerateProfileHelper ProfileLeave, PROFILE_LEAVE
GenerateProfileHelper ProfileTailcall, PROFILE_TAILCALL

#endif // PROFILING_SUPPORTED


#ifdef FEATURE_TIERED_COMPILATION

NESTED_ENTRY OnCallCountThresholdReachedStub, _TEXT, NoHandler
    PROLOG_WITH_TRANSITION_BLOCK

    addi.d  $a0, $sp, __PWTB_TransitionBlock // TransitionBlock *
    ori     $a1, $t1, 0 // stub-identifying token
    bl      C_FUNC(OnCallCountThresholdReached)
    ori     $t4,$a0,0

    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
    EPILOG_BRANCH_REG $t4
NESTED_END OnCallCountThresholdReachedStub, _TEXT

NESTED_ENTRY JIT_Patchpoint, _TEXT, NoHandler
    PROLOG_WITH_TRANSITION_BLOCK

    addi.d  $a0, $sp, __PWTB_TransitionBlock // TransitionBlock *
    bl      C_FUNC(JIT_PatchpointWorkerWorkerWithPolicy)

    EPILOG_WITH_TRANSITION_BLOCK_RETURN
NESTED_END JIT_Patchpoint, _TEXT

// first arg register holds iloffset, which needs to be moved to the second register, and the first register filled with NULL
LEAF_ENTRY JIT_PatchpointForced, _TEXT
    move    $a1, $a0
    li.d    $a0, 0
    b C_FUNC(JIT_Patchpoint)
LEAF_END JIT_PatchpointForced, _TEXT

#endif // FEATURE_TIERED_COMPILATION

// ------------------------------------------------------------------
// size_t GetThreadStaticsVariableOffset()

// Helper to calculate the offset of native thread local variable `t_ThreadStatics` in TCB. The offset has to be found at runtime
// once linker does its relocation and fixup of thread locals. The offset, after calculation is returned in `$a0` register.

LEAF_ENTRY GetThreadStaticsVariableOffset, _TEXT
        PROLOG_SAVE_REG_PAIR_INDEXED   22, 1, 16
        la.tls.desc   $a0, t_ThreadStatics
        EPILOG_RESTORE_REG_PAIR_INDEXED 22, 1, 16
        EPILOG_RETURN
LEAF_END GetThreadStaticsVariableOffset, _TEXT
// ------------------------------------------------------------------

// ------------------------------------------------------------------
// size_t GetTLSResolverAddress()

// Helper to get the TLS resolver address. This will be then used to determine if we have a static or dynamic resolver.
LEAF_ENTRY GetTLSResolverAddress, _TEXT
        //                           $fp,$ra
        PROLOG_SAVE_REG_PAIR_INDEXED  22, 1, 16
        pcalau12i  $a0, %desc_pc_hi20(t_ThreadStatics)
        addi.d     $a0, $a0, %desc_pc_lo12(t_ThreadStatics)
        ld.d       $a0, $a0, %desc_ld(t_ThreadStatics)
        EPILOG_RESTORE_REG_PAIR_INDEXED  22, 1, 16
        EPILOG_RETURN
LEAF_END GetTLSResolverAddress, _TEXT
// ------------------------------------------------------------------

LEAF_ENTRY JIT_PollGC, _TEXT
        PREPARE_EXTERNAL_VAR g_TrapReturningThreads, $t0
        ld.w    $t0, $t0, 0
        bne     $t0, $zero, LOCAL_LABEL(JIT_PollGCRarePath)
        jirl $r0, $ra, 0
LOCAL_LABEL(JIT_PollGCRarePath):
        PREPARE_EXTERNAL_VAR g_pPollGC, $t0
        ld.d    $t0, $t0, 0
        EPILOG_BRANCH_REG $t0
LEAF_END JIT_PollGC, _TEXT

//a0 -This pointer
//a1 -ReturnBuffer
LEAF_ENTRY ThisPtrRetBufPrecodeWorker, _TEXT
    ld.d  $METHODDESC_REGISTER, $METHODDESC_REGISTER, ThisPtrRetBufPrecodeData__Target
    move  $t0, $a0     // Move first arg pointer to temp register
    move  $a0, $a1     // Move ret buf arg pointer from location in ABI for return buffer for instance method to location in ABI for return buffer for static method
    move  $a1, $t0     // Move temp register to first arg register for static method with return buffer
    EPILOG_BRANCH_REG $METHODDESC_REGISTER
LEAF_END ThisPtrRetBufPrecodeWorker, _TEXT
