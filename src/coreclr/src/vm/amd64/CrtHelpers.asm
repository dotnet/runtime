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

;char *memset(dst, value, count) - sets "count" bytes at "dst" to "value"
;
;Purpose:
;   Sets the first "count" bytes of the memory starting
;   at "dst" to the character value "value".
;
;Algorithm:
;Set dst based on count as follow
;   count [0, 16]: use 1/2/4/8 bytes width registers
;   count [16, 128]: use 16 bytes width registers (XMM) without loop
;   count [128, 512]: use 16 bytes width registers (XMM) with loops, unrolled 8 times
;   count [512, upper]: use rep stosb
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

LEAF_ENTRY JIT_MemSet, _TEXT

        movzx   edx, dl                 ; set fill pattern
        mov     r9, 0101010101010101h   
        imul    rdx, r9                 ; rdx is 8 bytes filler

        cmp     r8, 16                  
        jbe     mset04                 

        cmp     r8, 512                 
        jbe     mset00 
        
        ; count > 512
        mov     r10, rcx                ; save dst address
        mov     r11, rdi                ; save rdi
        mov     eax, edx                ; eax is value
        mov     rdi, rcx                ; rdi is dst
        mov     rcx, r8                 ; rcx is count
        rep     stosb
        mov     rdi, r11                ; restore rdi
        mov     rax, r10
        ret

        align 16
mset00: mov     rax, rcx                ; save dst address
        movd    xmm0, rdx				
        punpcklbw xmm0, xmm0            ; xmm0 is 16 bytes filler

        cmp     r8, 128                
        jbe     mset02  

        ; count > 128 && count <= 512
        mov     r9, r8
        shr     r9, 7                   ; count/128
        
        align 16
mset01: movdqu	[rcx], xmm0
        movdqu	16[rcx], xmm0
        movdqu	32[rcx], xmm0
        movdqu	48[rcx], xmm0
        movdqu	64[rcx], xmm0
        movdqu	80[rcx], xmm0
        movdqu	96[rcx], xmm0
        movdqu	112[rcx], xmm0
        add     rcx, 128
        dec     r9
        jnz     mset01    
        and     r8, 7fh                 ; and r8 with 0111 1111
        
        ; the remainder is from 0 to 127
        cmp     r8, 16                  
        jnbe    mset02                  
        
        ; the remainder <= 16 
        movdqu  -16[rcx + r8], xmm0
        ret
        
        ; count > 16 && count <= 128 for mset02
        align 16
mset02: movdqu	[rcx], xmm0         
        movdqu	-16[rcx + r8], xmm0    
        cmp     r8, 32                 
        jbe     mset03
        
        ; count > 32 && count <= 64
        movdqu	16[rcx], xmm0
        movdqu	-32[rcx + r8], xmm0
        cmp     r8, 64
        jbe     mset03
        
        ; count > 64 && count <= 128
        movdqu	32[rcx], xmm0
        movdqu	48[rcx], xmm0
        movdqu	-48[rcx + r8], xmm0
        movdqu	-64[rcx + r8], xmm0   
mset03: ret
 
        align 16
mset04: mov     rax, rcx                ; save dst address
        test    r8b, 24                 ; and r8b with 0001 1000
        jz      mset05
        
        ; count >= 8 && count <= 16
        mov     [rcx], rdx        
        mov     -8[rcx + r8], rdx
        ret

        align 16
mset05: test    r8b, 4                  ; and r8b with 0100
        jz      mset06
        
        ; count >= 4 && count < 8
        mov     [rcx], edx        
        mov     -4[rcx + r8], edx
        ret
        
        ; count >= 0 && count < 4
        align 16
mset06: test    r8b, 1                  ; and r8b with 0001
        jz      mset07
        mov     [rcx],dl
mset07: test    r8b, 2                  ; and r8b with 0010
        jz      mset08
        mov     -2[rcx + r8], dx
mset08: ret

LEAF_END_MARKED JIT_MemSet, _TEXT

;JIT_MemCpy - Copy source buffer to destination buffer
;
;Purpose:
;   JIT_MemCpy() copies a source memory buffer to a destination memory
;   buffer. This routine recognize overlapping buffers to avoid propogation.
;   For cases where propogation is not a problem, memcpy() can be used.
;
;Algorithm:
;Copy to destination based on count as follow
;   count [0, 64]: overlap check not needed
;       count [0, 16]: use 1/2/4/8 bytes width registers  
;       count [16, 64]: use 16 bytes width registers (XMM) without loop
;   count [64, upper]: check overlap
;       non-overlap:
;           count [64, 512]: use 16 bytes width registers (XMM) with loops, unrolled 4 times
;           count [512, upper]: use rep movsb
;       overlap::
;           use 16 bytes width registers (XMM) with loops to copy from end to beginnig
;
;Entry:
;   void *dst = pointer to destination buffer
;   const void *src = pointer to source buffer
;   size_t count = number of bytes to copy
;
;Exit:
;   Returns a pointer to the destination buffer
;
;Uses:
;
;Exceptions:
;*******************************************************************************

LEAF_ENTRY JIT_MemCpy, _TEXT

        mov     rax, rcx                ; save dst address
        cmp     r8, 16                  
        jbe     mcpy02
        
        cmp     r8, 64             
        jnbe    mcpy07

        ; count > 16 && count <= 64
        align 16        
mcpy00: movdqu  xmm0, [rdx]             
        movdqu  xmm1, -16[rdx + r8]     ; save 16 to 32 bytes src
        cmp     r8, 32
        jbe     mcpy01
        
        movdqu  xmm2, 16[rdx]           
        movdqu  xmm3, -32[rdx + r8]     ; save 32 to 64 bytes src
        
        ;count > 32 && count <= 64
        movdqu  16[rcx], xmm2
        movdqu  -32[rcx + r8], xmm3
        
        ;count > 16 && count <= 32
mcpy01: movdqu  [rcx], xmm0
        movdqu  -16[rcx + r8], xmm1
        ret

        ; count <= 16 
        align 16
mcpy02: test    r8b, 24                 ; test count with 0001 1000
        jz      mcpy03
        ; count >= 8 && count <= 16
        mov     r9, [rdx]
        mov     r10, -8[rdx + r8]
        mov     [rcx], r9
        mov     -8[rcx + r8], r10
        ret
        
        align 16
mcpy03: test    r8b, 4                  ; test count with 0100
        jz      mcpy04
        ; count >= 4 && count < 8
        mov     r9d, [rdx]
        mov     r10d, -4[rdx + r8]
        mov     [rcx], r9d
        mov     -4[rcx + r8], r10d
        ret
        
        ; count >= 0 && count < 4
        align 16
mcpy04: test    r8, r8                  
        jz      mcpy06                  ; count == 1/2/3
        mov     r9b, [rdx]              ; save the first byte
        
        test    r8b, 2                  ; test count with 0010
        jz      mcpy05
        mov     r10w, -2[rdx + r8]        
        mov     -2[rcx + r8], r10w
mcpy05: mov     [rcx], r9b
mcpy06: ret
 
        align 16
        ; count > 64, we need to check overlap
mcpy07: mov     r9, rdx                 ; r9 is src address
        sub     r9, rcx                 ; if src - dst < 0 jump to mcpy11
        jb      mcpy11                  ; if b, destination may overlap 
        
mcpy08: cmp     r8, 512
        jnbe    mcpy10
        
        ; count > 64 && count <= 512
        mov     r9, r8
        shr     r9, 6                   ; count/64
        
        align 16
mcpy09: movdqu  xmm0, [rdx] 
        movdqu  xmm1, 16[rdx]
        movdqu  xmm2, 32[rdx]
        movdqu  xmm3, 48[rdx]
        movdqu  [rcx], xmm0
        movdqu  16[rcx], xmm1
        movdqu  32[rcx], xmm2
        movdqu  48[rcx], xmm3
        add     rdx, 64
        add     rcx, 64
        dec     r9
        jnz     mcpy09
        
        ; the remainder is from 0 to 63
        and     r8, 3fh                 ; and with 0011 1111 
        cmp     r8, 16                  
        jnbe    mcpy00                  

        ; the remainder <= 16
        jmp     mcpy02
        ret
        
        ; count > 512
        align 16
mcpy10: mov     r10, rdi                ; save rdi
        mov     r11, rsi                ; save rsi
        mov     rdi, rcx                ; rdi is dst
        mov     rsi, rdx                ; rsi is src
        mov     rcx, r8                 ; rcx is count
        rep     movsb                   ; mov from rsi to rdi
        mov     rsi, r11                ; restore rsi
        mov     rdi, r10                ; restore rdi
        ret

; The source address is less than the destination address.

        align 16
mcpy11: add     r9, r8                  ; src - dst + count
        cmp     r9, 0                   ; src + count < = dst jump to mcpy08
        jle     mcpy08
        
        lea     r9, [rdx + r8]          ; r9 is the src + count     
        lea     r10, [rcx + r8]         ; r10 is the dst + count
        
        mov     r11, r8
        shr     r11, 6                  ; count/64
       
        ; count > 64
        align 16
mcpy12: movdqu  xmm0, -16[r9]
        movdqu  xmm1, -32[r9]
        movdqu  xmm2, -48[r9]
        movdqu  xmm3, -64[r9]
        movdqu  -16[r10], xmm0
        movdqu  -32[r10], xmm1
        movdqu  -48[r10], xmm2
        movdqu  -64[r10], xmm3    
        sub     r9, 64
        sub     r10, 64
        dec     r11
        jnz     mcpy12
        
        ; the remainder is from 0 to 63
        and     r8, 3fh                 ; and with 0011 1111 
        cmp     r8, 16                  
        jnbe    mcpy00                  

        ; the remainder <= 16
        jmp     mcpy02

LEAF_END_MARKED JIT_MemCpy, _TEXT
		end