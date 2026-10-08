// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"

// JIT_CheckedWriteBarrier(Object** dst, Object* src)
//
// Write barrier for writes to objects that may reside
// on the managed heap.
//
// On entry:
//   x14 : the destination address (LHS of the assignment).
//         May not be a heap location (hence the checked).
//   x15 : the object reference (RHS of the assignment).
//
// On exit:
//   x12  : trashed
//   x14  : preserved (the destination address is not modified)
//   x15  : trashed
//   x16  : trashed (ip0)
//   x17  : trashed (ip1)
    LEAF_ENTRY RhpCheckedAssignRef, _TEXT
    // See if dst is in GCHeap
    // Card bundle table value is a literal load from a patchable slot co-located with this code
    // (see RhpAssignRef below), rather than a PC-relative reference to an external global, so it
    // stays correct no matter where this code is copied to; see GetAssignRefFunctions.
        ldr     x16, LOCAL_LABEL(PatchableValue_CheckedAssignRef_CardBundleTable)
        lsr     x17, x14, #30                       // dst page index
        ldrb    w12, [x16, x17]
        cbnz    x12, LOCAL_LABEL(RhpCheckedEntry)

LOCAL_LABEL(NotInHeap):
    ALTERNATE_ENTRY RhpCheckedAssignRefAVLocation
        str  x15, [x14]
        ret  lr
    LEAF_END_MARKED RhpCheckedAssignRef

// JIT_WriteBarrier(Object** dst, Object* src)
//
// Write barrier for writes to objects that are known to
// reside on the managed heap.
//
// On entry:
//   x14  : the destination address (LHS of the assignment)
//   x15  : the object reference (RHS of the assignment)
//
// On exit:
//   x12  : trashed
//   x14  : preserved (the destination address is not modified)
//   x15  : trashed
//   x16  : trashed (ip0)
//   x17  : trashed (ip1)
    LEAF_ENTRY RhpAssignRef, _TEXT
    // check for escaping assignment
    // 1) check if we own the source region
#ifdef FEATURE_SATORI_EXTERNAL_OBJECTS
        ldr     x16, LOCAL_LABEL(PatchableValue_AssignRef_CardBundleTable)
    LOCAL_LABEL(RhpCheckedEntry):
        lsr     x17, x15, #30                   // source page index
        ldrb    w12, [x16, x17]
        cbz     x12, LOCAL_LABEL(JustAssign)                 // null or external (immutable) object
#else
    LOCAL_LABEL(RhpCheckedEntry):
        cbz     x15, LOCAL_LABEL(JustAssign)                 // assigning null
#endif
        and     x16,  x15, #0xFFFFFFFFFFE00000  // source region
        ldr     x12, [x16]                      // region tag

        // Must match SatoriUtil::GetCurrentThreadTag; x18 is not the Unix thread tag.
#ifdef TARGET_OSX
        mrs     x17, TPIDRRO_EL0
        and     x17, x17, #-8                   // thread tag on OSX
#else
        mrs     x17, TPIDR_EL0                  // thread tag on other Unix
#endif
        cmp     x12, x17
        bne     LOCAL_LABEL(AssignAndMarkCards)              // not local to this thread

    // 2) check if the src and dst are from the same region
        and     x12, x14, #0xFFFFFFFFFFE00000   // target aligned to region
        cmp     x12, x16
        bne     LOCAL_LABEL(RecordEscape)                    // cross region assignment. definitely escaping

    // 3) check if the target is exposed
        ubfx    x17, x14,#9,#12                 // word index = (dst >> 9) & 0x1FFFFF
        ldr     x17, [x16, x17, lsl #3]         // mark word = [region + index * 8]
        lsr     x12, x14, #3                    // bit = (dst >> 3) [& 63]
        lsr     x17, x17, x12
        tbnz    x17, #0, LOCAL_LABEL(RecordEscape)           // target is exposed. record an escape.

    // UNORDERED! assignment of unescaped, null or external (immutable) object
LOCAL_LABEL(JustAssign):
    ALTERNATE_ENTRY RhpAssignRefAVLocationNotHeap
        str      x15, [x14]
        ret      lr

LOCAL_LABEL(AssignAndMarkCards):
    //ALTERNATE_ENTRY RhpAssignRefAVLocation
#ifdef TARGET_APPLE
// Apple's linker has issues which break unwind info if
// an ALTERNATE_ENTRY is present in the middle of a function see https://github.com/dotnet/runtime/pull/114982#discussion_r2083272768
.cfi_endproc
#endif
    ALTERNATE_ENTRY RhpAssignRefAVLocation
#ifdef TARGET_APPLE
.cfi_startproc
#endif
        stlr    x15, [x14]

    // TUNING: barriers in different modes could be separate pieces of code, but barrier switch 
    //         needs to suspend EE, not sure if skipping mode check would worth that much.
        ldr     x17, LOCAL_LABEL(PatchableValue_AssignAndMarkCards_WriteBarrierState)
    // check the barrier state. this must be done after the assignment (in program order
    // if state == 2 we do not set or dirty cards.
        tbz     x17, #1, LOCAL_LABEL(DoCards)

LOCAL_LABEL(ExitNoCards):
        ret     lr

LOCAL_LABEL(DoCards):
    // if same region, just check if barrier is not concurrent
        and     x12, x14, #0xFFFFFFFFFFE00000   // target aligned to region
        cmp     x12, x16
        beq     LOCAL_LABEL(CheckConcurrent)    // same region, just check if barrier is not concurrent

    // if src is in gen2/3 and the barrier is not concurrent we do not need to mark cards
        ldr     w12, [x16, 16]                  // source region + 16 -> generation
        tbz     x12, #1, LOCAL_LABEL(MarkCards)

LOCAL_LABEL(CheckConcurrent):
    // if not concurrent, exit
        cbz     x17, LOCAL_LABEL(ExitNoCards)

LOCAL_LABEL(MarkCards):
    // need couple temps. Save before using.
        stp     x2,  x3,  [sp, -16]!

    // fetch card location for x14
        ldr     x12, LOCAL_LABEL(PatchableValue_MarkCards_CardTable)  // fetch the page map value (patched; see GetAssignRefFunctions)
        lsr     x16, x14, #30
        ldr     x16, [x12, x16, lsl #3]              // page
        sub     x2,  x14, x16   // offset in page
        lsr     x15, x2,  #20   // group index
        lsr     x2,  x2,  #9    // card offset
        lsl     x15, x15, #1    // group offset (index * 2)

    // check if concurrent marking is in progress
        cbnz    x17, LOCAL_LABEL(DirtyCard)

    // SETTING CARD FOR X14
LOCAL_LABEL(SetCard):
        ldrb    w3, [x16, x2]
        cbnz    w3, LOCAL_LABEL(Exit)
        mov     w17, #1
        strb    w17, [x16, x2]
LOCAL_LABEL(SetGroup):
        add     x12, x16, #0x80
        ldrb    w3, [x12, x15]
        cbnz    w3, LOCAL_LABEL(CardSet)
        strb    w17, [x12, x15]
LOCAL_LABEL(SetPage):
        ldrb    w3, [x16]
        cbnz    w3, LOCAL_LABEL(CardSet)
        strb    w17, [x16]

LOCAL_LABEL(CardSet):
    // check if concurrent marking is still not in progress
        ldr     x12, LOCAL_LABEL(PatchableValue_CardSet_WriteBarrierState)  // fetch the barrier state value (patched; see GetAssignRefFunctions)
        cbnz    x12, LOCAL_LABEL(DirtyCard)

LOCAL_LABEL(Exit):
        ldp  x2,  x3, [sp], 16
        ret  lr

    // DIRTYING CARD FOR X14
LOCAL_LABEL(DirtyCard):
        mov     w17, #4
        add     x2, x2, x16
        // must be after the field write to allow concurrent clean
        stlrb   w17, [x2]
LOCAL_LABEL(DirtyGroup):
        add     x12, x16, #0x80
        ldrb    w3, [x12, x15]
        tbnz    w3, #2, LOCAL_LABEL(Exit)
        strb    w17, [x12, x15]
LOCAL_LABEL(DirtyPage):
        ldrb    w3, [x16]
        tbnz    w3, #2, LOCAL_LABEL(Exit)
        strb    w17, [x16]
        b       LOCAL_LABEL(Exit)

    // this is expected to be rare.
LOCAL_LABEL(RecordEscape):

    // 4) check if the source is escaped (x16 has source region)
        add         x12, x15, #8                   // escape bit is MT + 1
        ubfx        x17, x12, #9,#12               // word index = (dst >> 9) & 0x1FFFFF
        ldr         x17, [x16, x17, lsl #3]        // mark word = [region + index * 8]
        lsr         x12, x12, #3                   // bit = (dst >> 3) [& 63]
        lsr         x17, x17, x12
        tbnz        x17, #0, LOCAL_LABEL(AssignAndMarkCards)        // source is already escaped.

        // because of the barrier call convention
        // we need to preserve caller-saved x0 through x15 and x29/x30

        stp     x29,x30, [sp, -16 * 9]!
        stp     x0, x1,  [sp, 16 * 1]
        stp     x2, x3,  [sp, 16 * 2]
        stp     x4, x5,  [sp, 16 * 3]
        stp     x6, x7,  [sp, 16 * 4]
        stp     x8, x9,  [sp, 16 * 5]
        stp     x10,x11, [sp, 16 * 6]
        stp     x12,x13, [sp, 16 * 7]
        stp     x14,x15, [sp, 16 * 8]

        // void SatoriRegion::EscapeFn(SatoriObject** dst, SatoriObject* src, SatoriRegion* region)
        // mov  x0, x14  EscapeFn does not use dst, it is just to avoid arg shuffle on x64
        mov  x1, x15
        mov  x2, x16                       // source region
        ldr  x12, [x16, #8]                 // EscapeFn address
        blr  x12

        ldp     x0, x1,  [sp, 16 * 1]
        ldp     x2, x3,  [sp, 16 * 2]
        ldp     x4, x5,  [sp, 16 * 3]
        ldp     x6, x7,  [sp, 16 * 4]
        ldp     x8, x9,  [sp, 16 * 5]
        ldp     x10,x11, [sp, 16 * 6]
        ldp     x12,x13, [sp, 16 * 7]
        ldp     x14,x15, [sp, 16 * 8]
        ldp     x29,x30, [sp], 16 * 9

        and     x16, x15, #0xFFFFFFFFFFE00000  // source region
        b       LOCAL_LABEL(AssignAndMarkCards)

    // Patchable literal pool holding the cached values of the globals referenced above. Keeping
    // this co-located with the code (rather than referencing the globals by address) means the
    // PC-relative literal loads above stay correct no matter where this code is copied to; the EE
    // refreshes these slots after copying and whenever the globals' values change; see
    // GetAssignRefFunctions.
        .align 8
    PATCH_LABEL PatchableValue_CheckedAssignRef_CardBundleTable
    LOCAL_LABEL(PatchableValue_CheckedAssignRef_CardBundleTable):
        .quad 0
    PATCH_LABEL PatchableValue_AssignRef_CardBundleTable
    LOCAL_LABEL(PatchableValue_AssignRef_CardBundleTable):
        .quad 0
    PATCH_LABEL PatchableValue_AssignAndMarkCards_WriteBarrierState
    LOCAL_LABEL(PatchableValue_AssignAndMarkCards_WriteBarrierState):
        .quad 0
    PATCH_LABEL PatchableValue_MarkCards_CardTable
    LOCAL_LABEL(PatchableValue_MarkCards_CardTable):
        .quad 0
    PATCH_LABEL PatchableValue_CardSet_WriteBarrierState
    LOCAL_LABEL(PatchableValue_CardSet_WriteBarrierState):
        .quad 0
    LEAF_END_MARKED RhpAssignRef
