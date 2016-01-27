; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.
; See the LICENSE file in the project root for more information.

; ==++==
;

;
; ==--==
; ***********************************************************************
; File: CrtHelpers.asm, see history in asmhelpers.asm
;
; ***********************************************************************

include AsmMacros.inc
include asmconstants.inc

; JIT_MemSet/JIT_MemCpy
;
; It is IMPORANT that the exception handling code is able to find these guys
; on the stack, but to keep them from being tailcalled by VC++ we need to turn
; off optimization and it ends up being a wasteful implementation.
;
; Hence these assembly helpers.
; 


;***
;memset.asm - set a section of memory to all one byte
;
; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.
; See the LICENSE file in the project root for more information.;
;
;*******************************************************************************

;***
;char *memset(dst, value, count) - sets "count" bytes at "dst" to "value"
;
;Purpose:
;   Sets the first "count" bytes of the memory starting
;   at "dst" to the character value "value".
;
;   Algorithm:
;   char *
;   memset (dst, value, count)
;       char *dst;
;       char value;
;       unsigned int count;
;       {
;       char *start = dst;
;
;       while (count--)
;           *dst++ = value;
;       return(start);
;       }
;
;Entry:
;   char *dst - pointer to memory to fill with value
;   char value - value to put in dst bytes
;   int count - number of bytes of dst to fill
;
;Exit:
;   returns dst, with filled bytes
;
;Uses:
;
;Exceptions:
;
;*******************************************************************************

CACHE_LIMIT_MEMSET equ 070000h                 ; limit for nontemporal fill

LEAF_ENTRY JIT_MemSet, _TEXT

        mov     rax, rcx                ; save destination address
        cmp     r8, 8                   ; check if 8 bytes to fill
        jb      short mset40            ; if b, less than 8 bytes to fill
        movzx   edx, dl                 ; set fill pattern
        mov     r9, 0101010101010101h   ; replicate fill over 8 bytes
        imul    rdx, r9                 ;
        cmp     r8, 64                  ; check if 64 bytes to fill
        jb      short mset20            ; if b, less than 64 bytes

;
; Large block - fill alignment bytes.
;

mset00: neg     rcx                     ; compute bytes to alignment
        and     ecx, 7                  ;
        jz      short mset10            ; if z, no alignment required
        sub     r8, rcx                 ; adjust remaining bytes by alignment
        mov     [rax], rdx              ; fill alignment bytes
mset10: add     rcx, rax                ; compute aligned destination address

;
; Attempt to fill 64-byte blocks
;

        mov     r9, r8                  ; copy count of bytes remaining
        and     r8, 63                  ; compute remaining byte count
        shr     r9, 6                   ; compute number of 64-byte blocks
        test    r9, r9                  ; remove partial flag stall caused by shr
        jnz    short mset70             ; if nz, 64-byte blocks to fill

;
; Fill 8-byte bytes.
;

mset20: mov     r9, r8                  ; copy count of bytes remaining
        and     r8, 7                   ; compute remaining byte count
        shr     r9, 3                   ; compute number of 8-byte blocks
        test    r9, r9                  ; remove partial flag stall caused by shr
        jz      short mset40            ; if z, no 8-byte blocks

        align                           ; simpler way to align instrucitons

mset30: mov     [rcx], rdx              ; fill 8-byte blocks
        add     rcx, 8                  ; advance to next 8-byte block
        dec     r9                      ; decrement loop count
        jnz     short mset30            ; if nz, more 8-byte blocks

;
; Fill residual bytes.
;

mset40: test    r8, r8                  ; test if any bytes to fill
        jz      short mset60            ; if z, no bytes to fill
mset50: mov     [rcx], dl               ; fill byte
        inc     rcx                     ; advance to next byte
        dec     r8                      ; decrement loop count
        jnz     short mset50            ; if nz, more bytes to fill
mset60: 
        ; for some reason the assembler doesn't like the REPRET macro on the same line as a label
        REPRET                          ; return

;
; Fill 64-byte blocks.
;

        align   16

        db      066h, 066h, 066h, 090h
        db      066h, 066h, 090h

mset70: cmp     r9, CACHE_LIMIT_MEMSET / 64    ; check if large fill
        jae     short mset90            ; if ae, large fill
mset80: mov     [rcx], rdx              ; fill 64-byte block
        mov     8[rcx], rdx             ;
        mov     16[rcx], rdx            ;
        add     rcx, 64                 ; advance to next block
        mov     (24 - 64)[rcx], rdx     ;
        mov     (32 - 64)[rcx], rdx     ;
        dec     r9                      ; decrement loop count
        mov     (40 - 64)[rcx], rdx     ;
        mov     (48 - 64)[rcx], rdx     ;
        mov     (56 - 64)[rcx], rdx     ;
        jnz     short mset80            ; if nz, more 64-byte blocks
        jmp     short mset20            ; finish in common code

;
; Fill 64-byte blocks nontemporal.
;

        align

mset90: movnti  [rcx], rdx              ; fill 64-byte block
        movnti  8[rcx], rdx             ;
        movnti  16[rcx], rdx            ;
        add     rcx, 64                 ; advance to next block
        movnti  (24 - 64)[rcx], rdx     ;
        movnti  (32 - 64)[rcx], rdx     ;
        dec     r9                      ; decrement loop count
        movnti  (40 - 64)[rcx], rdx     ;
        movnti  (48 - 64)[rcx], rdx     ;
        movnti  (56 - 64)[rcx], rdx     ;
        jnz     short mset90            ; if nz, move 64-byte blocks
   lock or      byte ptr [rsp], 0       ; flush data to memory
        jmp     mset20                  ; finish in common code

LEAF_END_MARKED JIT_MemSet, _TEXT

;*******************************************************************************
; This ensures that atomic updates of aligned fields will stay atomic. 
;***
;JIT_MemCpy - Copy source buffer to destination buffer
;
;Purpose:
;JIT_MemCpy - Copy source buffer to destination buffer
;
;Purpose:
;       JIT_MemCpy() copies a source memory buffer to a destination memory
;       buffer. This routine recognize overlapping buffers to avoid propogation.
;       For cases where propogation is not a problem, memcpy() can be used.
;
;Entry:
;       void *dst = pointer to destination buffer
;       const void *src = pointer to source buffer
;       size_t count = number of bytes to copy
;
;Exit:
;       Returns a pointer to the destination buffer in AX/DX:AX
;
;Uses:
;       CX, DX
;
;Exceptions:
;*******************************************************************************
; This ensures that atomic updates of aligned fields will stay atomic. 

CACHE_LIMIT_MEMMOV equ 040000h                 ; limit for nontemporal fill
CACHE_BLOCK equ 01000h                  ; nontemporal move block size


LEAF_ENTRY JIT_MemCpy, _TEXT

        mov     r11, rcx                ; save destination address
        sub     rdx, rcx                ; compute offset to source buffer
        jb      mmov10                  ; if b, destination may overlap
        cmp     r8, 8                   ; check if 8 bytes to move
        jb      short mcpy40            ; if b, less than 8 bytes to move

;
; Move alignment bytes.
;

        test    cl, 7                   ; test if destination aligned
        jz      short mcpy20            ; if z, destination aligned
        test    cl, 1                   ; test if byte move needed
        jz      short mcpy00            ; if z, byte move not needed
        mov     al, [rcx + rdx]         ; move byte
        dec     r8                      ; decrement byte count
        mov     [rcx], al               ;
        inc     rcx                     ; increment destination address
mcpy00: test    cl, 2                   ; test if word move needed
        jz      short mcpy10            ; if z, word move not needed
        mov     ax, [rcx + rdx]         ; move word
        sub     r8, 2                   ; reduce byte count
        mov     [rcx], ax               ;
        add     rcx, 2                  ; advance destination address
mcpy10: test    cl, 4                   ; test if dword move needed
        jz      short mcpy20            ; if z, dword move not needed
        mov     eax, [rcx + rdx]        ; move dword
        sub     r8, 4                   ; reduce byte count
        mov     [rcx], eax              ;
        add     rcx, 4                  ; advance destination address

;
; Attempt to move 32-byte blocks.
;

mcpy20: mov     r9, r8                  ; copy count of bytes remaining
        shr     r9, 5                   ; compute number of 32-byte blocks
        test    r9, r9                  ; v-liti, remove partial flag stall caused by shr
        jnz     short mcpy60            ; if nz, 32-byte blocks to fill

        align
;
; Move 8-byte blocks.
;

mcpy25: mov     r9, r8                  ; copy count of bytes remaining
        shr     r9, 3                   ; compute number of 8-byte blocks
        test    r9, r9                  ; v-liti, remove partial flag stall caused by shr
        jz      short mcpy40            ; if z, no 8-byte blocks
        align

mcpy30: mov     rax, [rcx + rdx]        ; move 8-byte blocks
        mov     [rcx], rax              ;
        add     rcx, 8                  ; advance destination address
        dec     r9                      ; decrement loop count
        jnz     short mcpy30            ; if nz, more 8-byte blocks
        and     r8, 7                   ; compute remaining byte count

;
; Test for residual bytes.
;

mcpy40: test    r8, r8                  ; test if any bytes to move
        jnz     short mcpy50            ; if nz, residual bytes to move
        mov     rax, r11                ; set destination address
        ret                             ;

;
; Move residual bytes.
;

        align

mcpy50: mov     al, [rcx + rdx]         ; move byte
        mov     [rcx], al               ;
        inc     rcx                     ; increment destiantion address
        dec     r8                      ; decrement loop count
        jnz     short mcpy50            ; if nz, more bytes to fill
        mov     rax, r11                ; set destination address
        ret                             ; return

;
; Move 32 byte blocks
;

        align   16

        db      066h, 066h, 066h, 090h
        db      066h, 066h, 090h

mcpy60: cmp     r9, CACHE_LIMIT_MEMMOV / 32    ; check if large move
        jae     short mcpy80            ; if ae, large move
mcpy70: mov     rax, [rcx + rdx]        ; move 32-byte block
        mov     r10, 8[rcx + rdx]       ;
        add     rcx, 32                 ; advance destination address
        mov     (-32)[rcx], rax         ;
        mov     (-24)[rcx], r10         ;
        mov     rax, (-16)[rcx + rdx]   ;
        mov     r10, (-8)[rcx + rdx]    ;
        dec     r9                      ;
        mov     (-16)[rcx], rax         ;
        mov     (-8)[rcx], r10          ;
        jnz     short mcpy70            ; if nz, more 32-byte blocks
        and     r8, 31                  ; compute remaining byte count
        jmp     mcpy25                  ;

;
; Move 64-byte blocks nontemporal.
;

        align

        db      066h, 090h

mcpy80: cmp     rdx, CACHE_BLOCK        ; check if cache block spacing
        jb      short mcpy70            ; if b, not cache block spaced
mcpy81: mov     eax, CACHE_BLOCK / 128  ; set loop count
mcpy85: prefetchnta [rcx + rdx]         ; prefetch 128 bytes
        prefetchnta 64[rcx + rdx]       ;
        add     rcx, 128                ; advance source address
        dec     eax                     ; decrement loop count
        jnz     short mcpy85            ; if nz, more to prefetch
        sub     rcx, CACHE_BLOCK        ; reset source address
        mov     eax, CACHE_BLOCK / 64   ; set loop count
mcpy90: mov     r9, [rcx + rdx]         ; move 64-byte block
        mov     r10, 8[rcx + rdx]       ;
        movnti  [rcx], r9               ;
        movnti  8[rcx], r10             ;
        mov     r9, 16[rcx + rdx]       ;
        mov     r10, 24[rcx + rdx]      ;
        movnti  16[rcx], r9             ;
        movnti  24[rcx], r10            ;
        mov     r9, 32[rcx + rdx]       ;
        mov     r10, 40[rcx + rdx]      ;
        add     rcx, 64                 ; advance destination address
        movnti  (32 - 64)[rcx], r9      ;
        movnti  (40 - 64)[rcx], r10     ;
        mov     r9, (48 - 64)[rcx + rdx] ;
        mov     r10, (56 - 64)[rcx + rdx] ;
        dec     eax                     ;
        movnti  (48 - 64)[rcx], r9      ;
        movnti  (56 - 64)[rcx], r10     ;
        jnz     short mcpy90            ; if nz, more 32-byte blocks
        sub     r8, CACHE_BLOCK         ; reduce remaining length
        cmp     r8, CACHE_BLOCK         ; check if cache block remains
        jae     mcpy81                  ; if ae, cache block remains
   lock or      byte ptr [rsp], 0       ; flush data to memory
        jmp     mcpy20                  ;

;
; The source address is less than the destination address.
;

        align

        db      066h, 066h, 066h, 090h
        db      066h, 066h, 066h, 090h
        db      066h, 090h

mmov10: add     rcx, r8                 ; compute ending destination address
        cmp     r8, 8                   ; check if 8 bytes to move
        jb      short mmov60            ; if b, less than 8 bytes to move

;
; Move alignment bytes.
;

        test    cl, 7                   ; test if destination aligned
        jz      short mmov30            ; if z, destination aligned
        test    cl, 1                   ; test if byte move needed
        jz      short mmov15            ; if z, byte move not needed
        dec     rcx                     ; decrement destination address
        mov     al, [rcx + rdx]         ; move byte
        dec     r8                      ; decrement byte count
        mov     [rcx], al               ;
mmov15: test    cl, 2                   ; test if word move needed
        jz      short mmov20            ; if z, word move not needed
        sub     rcx, 2                  ; reduce destination address
        mov     ax, [rcx + rdx]         ; move word
        sub     r8, 2                   ; reduce byte count
        mov     [rcx], ax               ;
mmov20: test    cl, 4                   ; test if dword move needed
        jz      short mmov30            ; if z, dword move not needed
        sub     rcx, 4                  ; reduce destination address
        mov     eax, [rcx + rdx]        ; move dword
        sub     r8, 4                   ; reduce byte count
        mov     [rcx], eax              ;

;
; Attempt to move 32-byte blocks
;

mmov30: mov     r9, r8                  ; copy count of bytes remaining
        shr     r9, 5                   ; compute number of 32-byte blocks
        test    r9, r9                  ; v-liti, remove partial flag stall caused by shr
        jnz     short mmov80            ; if nz, 32-byte blocks to fill

;
; Move 8-byte blocks.
;
        align

mmov40: mov     r9, r8                  ; copy count of bytes remaining
        shr     r9, 3                   ; compute number of 8-byte blocks
        test    r9, r9                  ; v-liti, remove partial flag stall caused by shr
        jz      short mmov60            ; if z, no 8-byte blocks

        align

mmov50: sub     rcx, 8                  ; reduce destination address
        mov     rax, [rcx + rdx]        ; move 8-byte blocks
        dec     r9                      ; decrement loop count
        mov     [rcx], rax              ;
        jnz     short mmov50            ; if nz, more 8-byte blocks
        and     r8, 7                   ; compute remaining byte count

;
; Test for residual bytes.
;

mmov60: test    r8, r8                  ; test if any bytes to move
        jnz     short mmov70            ; if nz, residual bytes to move
        mov     rax, r11                ; set destination address
        ret                             ;

;
; Move residual bytes.
;

        align

mmov70: dec     rcx                     ; decrement destination address
        mov     al, [rcx + rdx]         ; move byte
        dec     r8                      ; decrement loop count
        mov     [rcx], al               ;
        jnz     short mmov70            ; if nz, more bytes to fill
        mov     rax, r11                ; set destination address
        ret                             ; return

;
; Move 32 byte blocks
;

        align   16

        db      066h, 066h, 066h, 090h
        db      066h, 066h, 090h

mmov80: cmp     r9, CACHE_LIMIT_MEMMOV / 32    ; check if large move
        jae     short mmov93            ; if ae, large move
mmov90: mov     rax, (-8)[rcx + rdx]    ; move 32-byte block
        mov     r10, (-16)[rcx + rdx]   ;
        sub     rcx, 32                 ; reduce destination address
        mov     24[rcx], rax            ;
        mov     16[rcx], r10            ;
        mov     rax, 8[rcx + rdx]       ;
        mov     r10, [rcx + rdx]        ;
        dec     r9                      ;
        mov     8[rcx], rax             ;
        mov     [rcx], r10              ;
        jnz     short mmov90            ; if nz, more 32-byte blocks
        and     r8, 31                  ; compute remaining byte count
        jmp     mmov40                  ;

;
; Move 64-byte blocks nontemporal.
;

        align

        db      066h, 090h

mmov93: cmp     rdx, -CACHE_BLOCK       ; check if cache block spacing
        ja      short mmov90            ; if a, not cache block spaced
mmov94: mov     eax, CACHE_BLOCK / 128  ; set loop count
mmov95: sub     rcx, 128                ; reduce destination address
        prefetchnta [rcx + rdx]         ; prefetch 128 bytes
        prefetchnta 64[rcx + rdx]       ;
        dec     eax                     ; decrement loop count
        jnz     short mmov95            ; if nz, more to prefetch
        add     rcx, CACHE_BLOCK        ; reset source address
        mov     eax, CACHE_BLOCK / 64   ; set loop count
mmov97: mov     r9, (-8)[rcx + rdx]     ; move 64-byte block
        mov     r10, (-16)[rcx + rdx]   ;
        movnti  (-8)[rcx], r9           ;
        movnti  (-16)[rcx], r10         ;
        mov     r9, (-24)[rcx + rdx]    ;
        mov     r10, (-32)[rcx + rdx]   ;
        movnti  (-24)[rcx], r9          ;
        movnti  (-32)[rcx], r10         ;
        mov     r9, (-40)[rcx + rdx]    ;
        mov     r10, (-48)[rcx + rdx]   ;
        sub     rcx, 64                 ; reduce destination address
        movnti  (64 - 40)[rcx], r9      ;
        movnti  (64 - 48)[rcx], r10     ;
        mov     r9, (64 - 56)[rcx + rdx] ;
        mov     r10, (64 - 64)[rcx + rdx] ;
        dec     eax                     ; decrement loop count
        movnti  (64 - 56)[rcx], r9      ;
        movnti  (64 - 64)[rcx], r10     ;
        jnz     short mmov97            ; if nz, more 32-byte blocks
        sub     r8, CACHE_BLOCK         ; reduce remaining length
        cmp     r8, CACHE_BLOCK         ; check if cache block remains
        jae     mmov94                  ; if ae, cache block remains
   lock or      byte ptr [rsp], 0       ; flush data to memory
        jmp     mmov30                  ;

LEAF_END_MARKED JIT_MemCpy, _TEXT


        end

