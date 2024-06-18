; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

; ***********************************************************************
; File: JitHelpers_InlineGetThread.asm, see history in jithelp.asm
;
; ***********************************************************************

include AsmMacros.inc
include asmconstants.inc

CopyValueClassUnchecked equ     ?CopyValueClassUnchecked@@YAXPEAX0PEAVMethodTable@@@Z
JIT_Box                 equ     ?JIT_Box@@YAPEAVObject@@PEAUCORINFO_CLASS_STRUCT_@@PEAX@Z

extern CopyValueClassUnchecked:proc
extern JIT_Box:proc

; HCIMPL2(Object*, JIT_Box, CORINFO_CLASS_HANDLE type, void* unboxedData)
NESTED_ENTRY JIT_BoxFastMP, _TEXT

        ; m_BaseSize is guaranteed to be a multiple of 8.
        mov     r8d, [rcx + OFFSET__MethodTable__m_BaseSize]

        INLINE_GET_ALLOC_CONTEXT r11
        mov     r10, [r11 + OFFSETOF__gc_alloc_context__alloc_limit]
        mov     rax, [r11 + OFFSETOF__gc_alloc_context__alloc_ptr]

        add     r8, rax

        cmp     r8, r10
        ja      AllocFailed

        test    rdx, rdx
        je      NullRef

        mov     [r11 + OFFSETOF__gc_alloc_context__alloc_ptr], r8
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
NESTED_END JIT_BoxFastMP, _TEXT

        end
