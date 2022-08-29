; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

; ***********************************************************************
; File: JitHelpers_InlineGetThread.asm, see history in jithelp.asm
;
; Notes: These routinues will be patched at runtime with the location in
;        the TLS to find the Thread* and are the fastest implementation
;        of their specific functionality.
; ***********************************************************************

include AsmMacros.inc
include asmconstants.inc

; Min amount of stack space that a nested function should allocate.
MIN_SIZE equ 28h

JIT_NEW                 equ     ?JIT_New@@YAPEAVObject@@PEAUCORINFO_CLASS_STRUCT_@@@Z
CopyValueClassUnchecked equ     ?CopyValueClassUnchecked@@YAXPEAX0PEAVMethodTable@@@Z
JIT_Box                 equ     ?JIT_Box@@YAPEAVObject@@PEAUCORINFO_CLASS_STRUCT_@@PEAX@Z
g_pStringClass          equ     ?g_pStringClass@@3PEAVMethodTable@@EA
FramedAllocateString    equ     ?FramedAllocateString@@YAPEAVStringObject@@K@Z
JIT_NewArr1             equ     ?JIT_NewArr1@@YAPEAVObject@@PEAUCORINFO_CLASS_STRUCT_@@_J@Z

INVALIDGCVALUE          equ     0CCCCCCCDh

extern JIT_NEW:proc
extern CopyValueClassUnchecked:proc
extern JIT_Box:proc
extern g_pStringClass:QWORD
extern FramedAllocateString:proc
extern JIT_NewArr1:proc

extern JIT_InternalThrow:proc

; IN: rcx: MethodTable*
; OUT: rax: new object
LEAF_ENTRY JIT_TrialAllocSFastMP_InlineGetThread, _TEXT
        mov     edx, [rcx + OFFSET__MethodTable__m_BaseSize]

        ; m_BaseSize is guaranteed to be a multiple of 8.

        INLINE_GETTHREAD r11
        mov     r10, [r11 + OFFSET__Thread__m_alloc_context__alloc_limit]
        mov     rax, [r11 + OFFSET__Thread__m_alloc_context__alloc_ptr]

        add     rdx, rax

        cmp     rdx, r10
        ja      AllocFailed

        mov     [r11 + OFFSET__Thread__m_alloc_context__alloc_ptr], rdx
        mov     [rax], rcx

        ret

    AllocFailed:
        jmp     JIT_NEW
LEAF_END JIT_TrialAllocSFastMP_InlineGetThread, _TEXT

; HCIMPL2(Object*, JIT_Box, CORINFO_CLASS_HANDLE type, void* unboxedData)
NESTED_ENTRY JIT_BoxFastMP_InlineGetThread, _TEXT

        ; m_BaseSize is guaranteed to be a multiple of 8.
        mov     r8d, [rcx + OFFSET__MethodTable__m_BaseSize]

        INLINE_GETTHREAD r11
        mov     r10, [r11 + OFFSET__Thread__m_alloc_context__alloc_limit]
        mov     rax, [r11 + OFFSET__Thread__m_alloc_context__alloc_ptr]

        add     r8, rax

        cmp     r8, r10
        ja      AllocFailed

        test    rdx, rdx
        je      NullRef

        mov     [r11 + OFFSET__Thread__m_alloc_context__alloc_ptr], r8
        mov     [rax], rcx

        ; Check whether the object contains pointers
        test    dword ptr [rcx + OFFSETOF__MethodTable__m_dwFlags], MethodTable__enum_flag_ContainsPointers
        jnz     ContainsPointers

        ; We have no pointers - emit a simple inline copy loop
        ; Copy the contents from the end
        mov     ecx, [rcx + OFFSET__MethodTable__m_BaseSize]
        sub     ecx, 18h  ; sizeof(ObjHeader) + sizeof(Object) + last slot

align 16
    CopyLoop:
        mov     r8, [rdx+rcx]
        mov     [rax+rcx+8], r8
        sub     ecx, 8
        jge     CopyLoop
        REPRET

    ContainsPointers:
        ; Do call to CopyValueClassUnchecked(object, data, pMT)
        push_vol_reg rax
        alloc_stack 20h
        END_PROLOGUE

        mov     r8, rcx
        lea     rcx, [rax + 8]
        call    CopyValueClassUnchecked

        add     rsp, 20h
        pop     rax
        ret

    AllocFailed:
    NullRef:
        jmp     JIT_Box
NESTED_END JIT_BoxFastMP_InlineGetThread, _TEXT

LEAF_ENTRY AllocateStringFastMP_InlineGetThread, _TEXT
        ; We were passed the number of characters in ECX

        ; we need to load the method table for string from the global
        mov     r9, [g_pStringClass]

        ; Instead of doing elaborate overflow checks, we just limit the number of elements
        ; to (LARGE_OBJECT_SIZE - 256)/sizeof(WCHAR) or less.
        ; This will avoid all overflow problems, as well as making sure
        ; big string objects are correctly allocated in the big object heap.

        cmp     ecx, (ASM_LARGE_OBJECT_SIZE - 256)/2
        jae     OversizedString

        ; Calculate the final size to allocate.
        ; We need to calculate baseSize + cnt*2, then round that up by adding 7 and anding ~7.

        lea     edx, [STRING_BASE_SIZE + ecx*2 + 7]
        and     edx, -8

        INLINE_GETTHREAD r11
        mov     r10, [r11 + OFFSET__Thread__m_alloc_context__alloc_limit]
        mov     rax, [r11 + OFFSET__Thread__m_alloc_context__alloc_ptr]

        add     rdx, rax

        cmp     rdx, r10
        ja      AllocFailed

        mov     [r11 + OFFSET__Thread__m_alloc_context__alloc_ptr], rdx
        mov     [rax], r9

        mov     [rax + OFFSETOF__StringObject__m_StringLength], ecx

        ret

    OversizedString:
    AllocFailed:
        jmp     FramedAllocateString
LEAF_END AllocateStringFastMP_InlineGetThread, _TEXT

; HCIMPL2(Object*, JIT_NewArr1VC_MP_InlineGetThread, CORINFO_CLASS_HANDLE arrayMT, INT_PTR size)
LEAF_ENTRY JIT_NewArr1VC_MP_InlineGetThread, _TEXT
        ; We were passed a (shared) method table in RCX, which contains the element type.

        ; The element count is in RDX

        ; NOTE: if this code is ported for CORINFO_HELP_NEWSFAST_ALIGN8, it will need
        ; to emulate the double-specific behavior of JIT_TrialAlloc::GenAllocArray.

        ; Do a conservative check here.  This is to avoid overflow while doing the calculations.  We don't
        ; have to worry about "large" objects, since the allocation quantum is never big enough for
        ; LARGE_OBJECT_SIZE.

        ; For Value Classes, this needs to be 2^16 - slack (2^32 / max component size),
        ; The slack includes the size for the array header and round-up ; for alignment.  Use 256 for the
        ; slack value out of laziness.

        ; In both cases we do a final overflow check after adding to the alloc_ptr.

        cmp     rdx, (65535 - 256)
        jae     OversizedArray

        movzx   r8d, word ptr [rcx + OFFSETOF__MethodTable__m_dwFlags]  ; component size is low 16 bits
        imul    r8d, edx
        add     r8d, dword ptr [rcx + OFFSET__MethodTable__m_BaseSize]

        ; round the size to a multiple of 8

        add     r8d, 7
        and     r8d, -8


        INLINE_GETTHREAD r11
        mov     r10, [r11 + OFFSET__Thread__m_alloc_context__alloc_limit]
        mov     rax, [r11 + OFFSET__Thread__m_alloc_context__alloc_ptr]

        add     r8, rax
        jc      AllocFailed

        cmp     r8, r10
        ja      AllocFailed

        mov     [r11 + OFFSET__Thread__m_alloc_context__alloc_ptr], r8
        mov     [rax], rcx

        mov     dword ptr [rax + OFFSETOF__ArrayBase__m_NumComponents], edx

        ret

    OversizedArray:
    AllocFailed:
        jmp     JIT_NewArr1
LEAF_END JIT_NewArr1VC_MP_InlineGetThread, _TEXT


; HCIMPL2(Object*, JIT_NewArr1OBJ_MP_InlineGetThread, CORINFO_CLASS_HANDLE arrayMT, INT_PTR size)
LEAF_ENTRY JIT_NewArr1OBJ_MP_InlineGetThread, _TEXT
        ; We were passed a (shared) method table in RCX, which contains the element type.

        ; The element count is in RDX

        ; NOTE: if this code is ported for CORINFO_HELP_NEWSFAST_ALIGN8, it will need
        ; to emulate the double-specific behavior of JIT_TrialAlloc::GenAllocArray.

        ; Verifies that LARGE_OBJECT_SIZE fits in 32-bit.  This allows us to do array size
        ; arithmetic using 32-bit registers.
        .erre ASM_LARGE_OBJECT_SIZE lt 100000000h

        cmp     rdx, (ASM_LARGE_OBJECT_SIZE - 256)/8 ; sizeof(void*)
        jae     OversizedArray

        ; In this case we know the element size is sizeof(void *), or 8 for x64
        ; This helps us in two ways - we can shift instead of multiplying, and
        ; there's no need to align the size either

        mov     r8d, dword ptr [rcx + OFFSET__MethodTable__m_BaseSize]
        lea     r8d, [r8d + edx * 8]

        ; No need for rounding in this case - element size is 8, and m_BaseSize is guaranteed
        ; to be a multiple of 8.

        INLINE_GETTHREAD r11
        mov     r10, [r11 + OFFSET__Thread__m_alloc_context__alloc_limit]
        mov     rax, [r11 + OFFSET__Thread__m_alloc_context__alloc_ptr]

        add     r8, rax

        cmp     r8, r10
        ja      AllocFailed

        mov     [r11 + OFFSET__Thread__m_alloc_context__alloc_ptr], r8
        mov     [rax], rcx

        mov     dword ptr [rax + OFFSETOF__ArrayBase__m_NumComponents], edx

        ret

    OversizedArray:
    AllocFailed:
        jmp     JIT_NewArr1
LEAF_END JIT_NewArr1OBJ_MP_InlineGetThread, _TEXT


        end

