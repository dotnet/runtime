;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

    .586
    .xmm
    .model  flat
    option  casemap:none


EXTERN  _IsProcessorFeaturePresent@4 : PROC

PF_XMMI64_INSTRUCTIONS_AVAILABLE equ 10

    .data
canUseSSE2  db  0

    .code

_memclr_for_gc@8 proc public

;    x86 version

;   we get the following parameters
;   ecx = destination address
;   edx = size to clear

    push    ebx
    push    edi

    xor     eax, eax

    ; load destination
    mov     edi,[esp+8+4]

    ; load size
    mov     ebx,[esp+8+8]

    ; check alignment of destination
    test    edi,3
    jnz     alignDest
alignDone:
    ; now destination is dword aligned

    ; compute number of bytes to clear non-temporally
    ; we wish to clear the first 8k or so with rep stos,
    ; anything above that non-temporally

    xor     edx,edx
    cmp     ebx,8*1024
    jbe     noNonTempClear

    ; can we use SSE2 instructions?
    cmp     canUseSSE2,0
    js      noNonTempClear
    jz      computeCanUseSSE2

computeNonTempClear:

    ; compute the number of bytes above 8k
    ; and round down to a multiple of 64
    mov     edx,ebx
    sub     edx,8*1024
    and     edx,not 63

    ; compute remaining size to clear temporally
    sub     ebx,edx

noNonTempClear:
    ; do the temporal clear
    mov     ecx,ebx
    shr     ecx,2
    rep     stosd

    ; do the non-temporal clear
    test    edx,edx
    jne     nonTempClearLoop

nonTempClearDone:

    ; clear any remaining bytes
    mov     ecx,ebx
    and     ecx,3
    rep     stosb

    pop     edi
    pop     ebx
    ret     8

    ; this is the infrequent case, hence out of line
nonTempClearLoop:
    movnti  [edi+ 0],eax
    movnti  [edi+ 4],eax
    movnti  [edi+ 8],eax
    movnti  [edi+12],eax

    movnti  [edi+16],eax
    movnti  [edi+20],eax
    movnti  [edi+24],eax
    movnti  [edi+28],eax

    movnti  [edi+32],eax
    movnti  [edi+36],eax
    movnti  [edi+40],eax
    movnti  [edi+44],eax

    movnti  [edi+48],eax
    movnti  [edi+52],eax
    movnti  [edi+56],eax
    movnti  [edi+60],eax

    add     edi,64
    sub     edx,64
    ja      nonTempClearLoop
    jmp     nonTempClearDone

alignDest:
    test    ebx,ebx
    je      alignDone
alignLoop:
    mov     [edi],al
    add     edi,1
    sub     ebx,1
    jz      alignDone
    test    edi,3
    jnz     alignLoop
    jmp     alignDone

computeCanUseSSE2:
    ; we are not using the sse2 register set,
    ; just sse2 instructions (movnti),
    ; thus we just ask the OS about the usability of the instructions
    ; OS bugs about saving/restoring registers like in early versions
    ; of Vista etc. in the WoW shouldn't matter

    push    PF_XMMI64_INSTRUCTIONS_AVAILABLE
    call    _IsProcessorFeaturePresent@4
    mov     ecx,eax
    xor     eax,eax         ; reset eax to 0
    test    ecx,ecx
    mov     canUseSSE2,1
    jne     computeNonTempClear
    mov     canUseSSE2,-1
    xor     edx,edx
    jmp     noNonTempClear

_memclr_for_gc@8 endp

    end
