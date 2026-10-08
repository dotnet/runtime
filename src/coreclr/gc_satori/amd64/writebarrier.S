// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"

//
//   rdi - dest address 
//   rsi - object
//
LEAF_ENTRY RhpCheckedAssignRef, _TEXT
    // See if dst is in GCHeap
    NOP_6_BYTE
    .byte   0x48, 0xB8              // mov rax, imm64 (REX.W B8+rax)
ALTERNATE_ENTRY PatchableValue_CheckedAssignRef_CardBundleTable
    .quad   0x0F0F0F0F0F0F0F0F0      // fetch the page byte map value (patched; see GetAssignRefFunctions)
    mov     r8,  rdi
    shr     r8,  30                    // dst page index
    cmp     byte ptr [rax + r8], 0
    jne     LOCAL_LABEL(RhpCheckedEntry)

NotInHeap:
ALTERNATE_ENTRY RhpCheckedAssignRefAVLocation
    mov     [rdi], rsi
    ret
LEAF_END_MARKED RhpCheckedAssignRef, _TEXT

//
//   rdi - dest address 
//   rsi - object
//
LEAF_ENTRY RhpAssignRef, _TEXT

#ifdef FEATURE_SATORI_EXTERNAL_OBJECTS
    NOP_6_BYTE
    .byte   0x48, 0xB8              // mov rax, imm64 (REX.W B8+rax)
    ALTERNATE_ENTRY PatchableValue_AssignRef_CardBundleTable
    .quad   0x0F0F0F0F0F0F0F0F0      // fetch the page byte map value (patched; see GetAssignRefFunctions)
    //ALTERNATE_ENTRY RhpCheckedEntry
LOCAL_LABEL(RhpCheckedEntry):
    mov     r8,  rsi
    shr     r8,  30                    // src page index
    cmp     byte ptr [rax + r8], 0
    je      JustAssign              // src not in heap
#else
    //ALTERNATE_ENTRY RhpCheckedEntry
LOCAL_LABEL(RhpCheckedEntry):
#endif

    // check for escaping assignment
    // 1) check if we own the source region
    mov     rdx, rsi
    and     rdx, 0xFFFFFFFFFFE00000  // source region

#ifndef FEATURE_SATORI_EXTERNAL_OBJECTS
    jz      JustAssign              // assigning null
#endif

#ifdef TARGET_OSX
    mov     rax,  gs:[0]            // thread tag
#else
    mov     rax,  fs:[0]            // thread tag
#endif
    cmp     qword ptr [rdx], rax     
    jne     AssignAndMarkCards      // no card marking, src is not a heap object

    // 2) check if the src and dst are from the same region
    mov     rax, rdi
    and     rax, 0xFFFFFFFFFFE00000
    cmp     rax, rdx
    jnz     RecordEscape            // cross region assignment. definitely escaping

    // 3) check if the target is exposed
    mov     rax, rdi
    and     rax, 0x1FFFFF
    shr     rax, 3
    bt      qword ptr [rdx], rax
    jb      RecordEscape            // target is exposed. record an escape.

JustAssign:
    ALTERNATE_ENTRY RhpAssignRefAVLocationNotHeap
    mov     [rdi], rsi              // no card marking, src is not a heap object
    // set rdi, rsi per contract with JIT_ByRefWriteBarrier
    //add     rdi, 8
    //mov     rsi, r10
    ret
    ////nop

AssignAndMarkCards:
    ALTERNATE_ENTRY RhpAssignRefAVLocation
    mov     [rdi], rsi

    // TUNING: barriers in different modes could be separate pieces of code, but barrier switch 
    //         needs to suspend EE, not sure if skipping mode check would worth that much.
    // No padding needed here: the preceding code already leaves the opcode start 8-byte-aligned
    // (mod 8) at the required offset; see note above PatchableValue_CheckedAssignRef_CardBundleTable.
    .byte   0x49, 0xBB              // mov r11, imm64 (REX.WB B8+(r11&7))
    ALTERNATE_ENTRY PatchableValue_AssignAndMarkCards_WriteBarrierState
    .quad   0x0F0F0F0F0F0F0F0F0      // fetch the barrier state value (patched; see GetAssignRefFunctions)

    // set rdi per contract with JIT_ByRefWriteBarrier
    mov     rax, rdi
    //add     rdi, 8

    // check the barrier state. this must be done after the assignment (in program order)
    // if state == 2 we do not set or dirty cards.
    cmp     r11, 2
    jne     DoCards
    // set rsi per contract with JIT_ByRefWriteBarrier
    //mov     rsi, r10
Exit:
    ret

    NOP_2_BYTE
    NOP_2_BYTE
    NOP_3_BYTE

DoCards:
    // if same region, just check if barrier is not concurrent
    xor     rsi, rax
    shr     rsi, 21
    // set rsi per contract with JIT_ByRefWriteBarrier
    //mov     rsi, r10
    jz      CheckConcurrent         // same region, just check if barrier is not concurrent

    // if src is in gen2/3 and the barrier is not concurrent we do not need to mark cards
    cmp     dword ptr [rdx + 16], 2
    jl      MarkCards

    CheckConcurrent:
    // if concurrent, load card location
    cmp     r11, 0
    je      Exit

MarkCards:
    .byte   0x49, 0xB9              // mov r9, imm64 (REX.WB B8+(r9&7))
    ALTERNATE_ENTRY PatchableValue_MarkCards_CardTable
    .quad   0x0F0F0F0F0F0F0F0F0      // fetch the page map value (patched; see GetAssignRefFunctions)
    mov     rdx, rax
    shr     rax, 30
    mov     rax, qword ptr [r9 + rax * 8] // page
    sub     rdx, rax   // offset in page
    mov     r8,  rdx
    shr     rdx, 9     // card offset
    shr     r8,  20    // group index
    lea     r8,  [rax + r8 * 2 + 0x80] // group offset

     // check if concurrent marking is in progress
    cmp     r11, 0
    jne     DirtyCard

     // SETTING CARD
SetCard:
    cmp     byte ptr [rax + rdx], 0
    jne     Exit
    mov     byte ptr [rax + rdx], 1
SetGroup:
    cmp     byte ptr [r8], 0
    jne     CardSet
    mov     byte ptr [r8], 1
SetPage:
    cmp     byte ptr [rax], 0
    jne     CardSet
    mov     byte ptr [rax], 1

CardSet:
    // check if concurrent marking is still not in progress
    // 3 bytes of padding needed here; see note above PatchableValue_CheckedAssignRef_CardBundleTable.
    NOP_3_BYTE
    .byte   0x49, 0xB9              // mov r9, imm64 (REX.WB B8+(r9&7))
    ALTERNATE_ENTRY PatchableValue_CardSet_WriteBarrierState
    .quad   0x0F0F0F0F0F0F0F0F0      // fetch the barrier state value (patched; see GetAssignRefFunctions)
    cmp     r9, 0
    jne     DirtyCard
    ret

    // DIRTYING CARD
DirtyCard:
    mov     byte ptr [rax + rdx], 4
DirtyGroup:
    cmp     byte ptr [r8], 4
    je      Exit
    mov     byte ptr [r8], 4
DirtyPage:
    cmp     byte ptr [rax], 4
    je      Exit
    mov     byte ptr [rax], 4
    ret

    // this is expected to be rare.
RecordEscape:

    // 4) check if the source is escaped
    mov     rax, rsi
    add     rax, 8                             // escape bit is MT + 1
    and     rax, 0x1FFFFF
    shr     rax, 3
    bt      qword ptr [rdx], rax
    jb      AssignAndMarkCards            // source is already escaped.

    // Align rsp
    mov  r9, rsp
    and  rsp, -16
    sub  rsp, 8

    // save rsp, rdi, rsi, rdx and r10 (possibly preadjusted rsi)
    push r9
    push rdi
    push rsi
    push rdx
    push r10

    // also save xmm0, in case it is used for stack clearing, as JIT_ByRefWriteBarrier should not trash xmm0
    // Hopefully EscapeFn cannot corrupt other xmm regs, since there is no float math or vectorizable code in there.
    sub     rsp, 16
    movdqa  [rsp], xmm0

    // void SatoriRegion::EscapeFn(SatoriObject** dst, SatoriObject* src, SatoriRegion* region)
    call    qword ptr [rdx + 8]

    movdqa  xmm0, [rsp]
    add     rsp, 16

    pop     r10
    pop     rdx
    pop     rsi
    pop     rdi
    pop     rsp
    jmp     AssignAndMarkCards
LEAF_END_MARKED RhpAssignRef, _TEXT