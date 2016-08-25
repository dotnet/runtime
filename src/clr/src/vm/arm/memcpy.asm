; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.
; See the LICENSE file in the project root for more information.

;

;

; This is the fast memcpy implementation for ARM stolen from the CRT (original location 
; vctools\crt\crtw32\string\arm\memcpy.asm) and modified to be compatible with CLR.
;
; For reference, the unmodified crt version of memcpy is preserved as memcpy_crt.asm

#include "ksarm.h"
#include "asmmacros.h"

        IMPORT FCallMemCpy_GCPoll
        IMPORT g_TrapReturningThreads

        AREA    |.text|,ALIGN=5,CODE,READONLY

;
; void *memcpy(void *dst, const void *src, size_t length)
;
; Copy a block of memory in a forward direction.
;

        ALIGN 32
        LEAF_ENTRY FCallMemcpy

        pld     [r1]                                    ; preload the first cache line
        cmp     r2, #16                                 ; less than 16 bytes?
        mov     r3, r0                                  ; use r3 as our destination
        bhs.W     __FCallMemcpy_large                   ; go to the large copy case directly. ".W" indicates encoding using 32bits

CpySmal tbb     [pc, r2]                                ; branch to specialized bits for small copies
__SwitchTable1_Copy
CTable  dcb     (Copy0 - CTable) / 2                    ; 0B
        dcb     (Copy1 - CTable) / 2                    ; 1B
        dcb     (Copy2 - CTable) / 2                    ; 2B
        dcb     (Copy3 - CTable) / 2                    ; 3B
        dcb     (Copy4 - CTable) / 2                    ; 4B
        dcb     (Copy5 - CTable) / 2                    ; 5B
        dcb     (Copy6 - CTable) / 2                    ; 6B
        dcb     (Copy7 - CTable) / 2                    ; 7B
        dcb     (Copy8 - CTable) / 2                    ; 8B
        dcb     (Copy9 - CTable) / 2                    ; 9B
        dcb     (Copy10 - CTable) / 2                   ; 10B
        dcb     (Copy11 - CTable) / 2                   ; 11B
        dcb     (Copy12 - CTable) / 2                   ; 12B
        dcb     (Copy13 - CTable) / 2                   ; 13B
        dcb     (Copy14 - CTable) / 2                   ; 14B
        dcb     (Copy15 - CTable) / 2                   ; 15B
__SwitchTableEnd_Copy

Copy1   ldrb    r2, [r1]
        strb    r2, [r3]
Copy0   b       GC_POLL

Copy2   ldrh    r2, [r1]
        strh    r2, [r3]
        b       GC_POLL

Copy3   ldrh    r2, [r1]
        ldrb    r1, [r1, #2]
        strh    r2, [r3]
        strb    r1, [r3, #2]
        b       GC_POLL

Copy4   ldr     r2, [r1]
        str     r2, [r3]
        b       GC_POLL

Copy5   ldr     r2, [r1]
        ldrb    r1, [r1, #4]
        str     r2, [r3]
        strb    r1, [r3, #4]
        b       GC_POLL

Copy6   ldr     r2, [r1]
        ldrh    r1, [r1, #4]
        str     r2, [r3]
        strh    r1, [r3, #4]
        b       GC_POLL

Copy7   ldr     r12, [r1]
        ldrh    r2, [r1, #4]
        ldrb    r1, [r1, #6]
        str     r12, [r3]
        strh    r2, [r3, #4]
        strb    r1, [r3, #6]
        b       GC_POLL

Copy8   ldr     r2, [r1]
        ldr     r1, [r1, #4]
        str     r2, [r3]
        str     r1, [r3, #4]
        b       GC_POLL

Copy9   ldr     r12, [r1]
        ldr     r2, [r1, #4]
        ldrb    r1, [r1, #8]
        str     r12, [r3]
        str     r2, [r3, #4]
        strb    r1, [r3, #8]
        b       GC_POLL

Copy10  ldr     r12, [r1]
        ldr     r2, [r1, #4]
        ldrh    r1, [r1, #8]
        str     r12, [r3]
        str     r2, [r3, #4]
        strh    r1, [r3, #8]
        b       GC_POLL

Copy11  ldr     r12, [r1]
        ldr     r2, [r1, #4]
        str     r12, [r3]
        str     r2, [r3, #4]
        ldrh    r2, [r1, #8]
        ldrb    r1, [r1, #10]
        strh    r2, [r3, #8]
        strb    r1, [r3, #10]
        b       GC_POLL

Copy12  ldr     r12, [r1]
        ldr     r2, [r1, #4]
        ldr     r1, [r1, #8]
        str     r12, [r3]
        str     r2, [r3, #4]
        str     r1, [r3, #8]
        b       GC_POLL

Copy13  ldr     r12, [r1]
        ldr     r2, [r1, #4]
        str     r12, [r3]
        str     r2, [r3, #4]
        ldr     r2, [r1, #8]
        ldrb    r1, [r1, #12]
        str     r2, [r3, #8]
        strb    r1, [r3, #12]
        b       GC_POLL

Copy14  ldr     r12, [r1]
        ldr     r2, [r1, #4]
        str     r12, [r3]
        str     r2, [r3, #4]
        ldr     r2, [r1, #8]
        ldrh    r1, [r1, #12]
        str     r2, [r3, #8]
        strh    r1, [r3, #12]
        b       GC_POLL

Copy15  ldr     r12, [r1]
        ldr     r2, [r1, #4]
        str     r12, [r3]
        str     r2, [r3, #4]
        ldr     r12, [r1, #8]
        ldrh    r2, [r1, #12]
        ldrb    r1, [r1, #14]
        str     r12, [r3, #8]
        strh    r2, [r3, #12]
        strb    r1, [r3, #14]
GC_POLL
        ldr     r0, =g_TrapReturningThreads
        ldr     r0, [r0]
        cmp     r0, #0
        bne     FCallMemCpy_GCPoll

        bx      lr

        LEAF_END FCallMemcpy


;
; __memcpy_forward_large_integer (internal calling convention)
;
; Copy large (>= 16 bytes) blocks of memory in a forward direction,
; using integer registers only.
;

        ALIGN 32
        NESTED_ENTRY __FCallMemcpy_large
        
        PROLOG_NOP lsls r12, r3, #31                    ; C = bit 1, N = bit 0
        PROLOG_PUSH {r4-r9, r11, lr}

;
; Align destination to a word boundary
;

        bpl     %F1
        ldrb    r4, [r1], #1                            ; fetch byte
        subs    r2, r2, #1                              ; decrement count
        strb    r4, [r3], #1                            ; store byte
        lsls    r12, r3, #31                            ; compute updated status
1
        bcc     %F2                                     ; if already aligned, just skip ahead
        ldrh    r4, [r1], #2                            ; fetch halfword
        subs    r2, r2, #2                              ; decrement count
        strh    r4, [r3], #2                            ; store halfword
2
        tst     r1, #3                                  ; is the source now word-aligned?
        bne     %F20                                    ; if not, we have to use the slow path

;
; Source is word-aligned; fast case
;

10
        subs    r2, r2, #32                             ; take 32 off the top
        blo     %F13                                    ; if not enough, recover and do small copies
        subs    r2, r2, #32                             ; take off another 32
        pld     [r1, #32]                               ; pre-load one block ahead
        blo     %F12                                    ; skip the loop if that's all we have
11
        pld     [r1, #64]                               ; prefetch ahead
        subs    r2, r2, #32                             ; count the bytes for this block
        ldm     r1!, {r4-r9, r12, lr}                   ; load 32 bytes
        stm     r3!, {r4-r9, r12, lr}                   ; store 32 bytes
        bhs     %B11                                    ; keep going until we're done
12
        ldm     r1!, {r4-r9, r12, lr}                   ; load 32 bytes
        stm     r3!, {r4-r9, r12, lr}                   ; store 32 bytes
13
        adds    r2, r2, #(32 - 8)                       ; recover original count, and pre-decrement
        blo     %F15                                    ; if not enough remaining, skip this loop
14 
        subs    r2, r2, #8                              ; decrement count
        ldrd    r4, r5, [r1], #8                        ; fetch pair of words
        strd    r4, r5, [r3], #8                        ; store pair of words
        bhs     %B14                                    ; loop while we still have data remaining
15
        adds    r2, r2, #8                              ; recover final count

        EPILOG_POP {r4-r9, r11, lr}
        EPILOG_NOP bne CpySmal                          ; if some left, continue with small
        EPILOG_BRANCH GC_POLL

;
; Source is not word-aligned; slow case
;

20
        subs    r2, r2, #64                             ; pre-decrement to simplify the loop
        blo     %23                                     ; skip over the loop if we don't have enough
        pld     [r1, #32]                               ; pre-load one block ahead
21
        pld     [r1, #64]                               ; prefetch ahead
        ldr     r4, [r1, #0]                            ; load 32 bytes
        ldr     r5, [r1, #4]                            ;
        ldr     r6, [r1, #8]                            ;
        ldr     r7, [r1, #12]                           ;
        ldr     r8, [r1, #16]                           ;
        ldr     r9, [r1, #20]                           ;
        ldr     r12, [r1, #24]                          ;
        ldr     lr, [r1, #28]                           ;
        adds    r1, r1, #32                             ; update pointer
        subs    r2, r2, #32                             ; count the bytes for this block
        stm     r3!, {r4-r9, r12, lr}                   ; store 32 bytes
        bhs     %B21                                    ; keep going until we're done
23
        adds    r2, r2, #(64 - 8)                       ; recover original count, and pre-decrement
        blo     %F25                                    ; if not enough remaining, skip this loop
24
        ldr     r4, [r1]                                ; fetch pair of words
        ldr     r5, [r1, #4]                            ;
        adds    r1, r1, #8                              ; update pointer
        subs    r2, r2, #8                              ; decrement count
        strd    r4, r5, [r3], #8                        ; store pair of words
        bhs     %B24                                    ; loop while we still have data remaining
25
        adds    r2, r2, #8                              ; recover final count

        EPILOG_POP {r4-r9, r11, lr}
        EPILOG_NOP bne CpySmal                          ; if some left, continue with small
        EPILOG_BRANCH GC_POLL

        EXPORT FCallMemcpy_End                          ; this is used to place the entire 
FCallMemcpy_End                                         ; implementation in av-exclusion list

        NESTED_END __FCallMemcpy_large

        END
