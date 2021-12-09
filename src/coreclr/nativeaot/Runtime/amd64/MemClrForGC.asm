;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

include AsmMacros.inc


LEAF_ENTRY memclr_for_gc, _TEXT

;    x64 version

;   we get the following parameters
;   rcx = destination address
;   rdx = size to clear

    ; save rdi - this should be faster than a push
    mov     r11,rdi

    xor     eax, eax

    ; check alignment of destination
    test    cl,7
    jnz     alignDest
alignDone:
    ; now destination is qword aligned
    ; move it to rdi for rep stos
    mov     rdi,rcx

    ; compute number of bytes to clear non-temporally
    ; we wish to clear the first 8k or so with rep stos,
    ; anything above that non-temporally

    xor     r8,r8
    cmp     rdx,8*1024
    jbe     noNonTempClear

    ; compute the number of bytes above 8k
    ; and round down to a multiple of 64
    mov     r8,rdx
    sub     r8,8*1024
    and     r8,not 63

    ; compute remaining size to clear temporally
    sub     rdx,r8

noNonTempClear:

    ; do the temporal clear
    mov     rcx,rdx
    shr     rcx,3
    rep     stosq

    ; do the non-temporal clear
    test    r8,r8
    jne     nonTempClearLoop

nonTempClearDone:

    ; clear any remaining bytes
    mov     rcx,rdx
    and     rcx,7
    rep     stosb

    ; restore rdi
    mov     rdi,r11

    ret

    ; this is the infrequent case, hence out of line
nonTempClearLoop:
    movnti  [rdi+ 0],rax
    movnti  [rdi+ 8],rax
    movnti  [rdi+16],rax
    movnti  [rdi+24],rax

    movnti  [rdi+32],rax
    movnti  [rdi+40],rax
    movnti  [rdi+48],rax
    movnti  [rdi+56],rax

    add     rdi,64
    sub     r8,64
    ja      nonTempClearLoop
    jmp     nonTempClearDone

alignDest:
    test    rdx,rdx
    je      alignDone
alignLoop:
    mov     [rcx],al
    add     rcx,1
    sub     rdx,1
    jz      alignDone
    test    cl,7
    jnz     alignLoop
    jmp     alignDone

LEAF_END memclr_for_gc, _TEXT

    end
