; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

;; ==++==
;;

;;
;; ==--==

#include "ksarm64.h"

    TEXTAREA

;void JIT_MemSet(void *dst, int val, SIZE_T count)
;{
;    uint64_t valEx = (unsigned char)val;
;    valEx = valEx | valEx << 8;
;    valEx = valEx | valEx << 16;
;    valEx = valEx | valEx << 32;
;
;    count-=16;
;
;    while(count >= 0)
;    {
;        *(uint64_t*)dst = valEx;
;        dst = (uint64_t*)dst + 1;
;        *(uint64_t*)dst = valEx;
;        dst = (uint64_t*)dst + 1;
;        count-=16;
;    }
;
;    if(count & 8)
;    {
;        *(uint64_t*)dst = valEx;
;        dst = (uint64_t*)dst + 1;
;    }
;
;    if(count & 4)
;    {
;        *(uint32_t*)dst = (uint32_t)valEx;
;        dst = (uint32_t*)dst + 1;
;    }
;
;    if(count & 2)
;    {
;        *(uint16_t*)dst = (uint16_t)valEx;
;        dst = (uint16_t*)dst + 1;
;    }
;
;    if(count & 1)
;    {
;        *(uint8_t*)dst = (uint8_t)valEx;
;    }
;}
;

; Assembly code corresponding to above C++ method. JIT_MemSet can AV and clr exception personality routine needs to
; determine if the exception has taken place inside JIT_Memset in order to throw corresponding managed exception.
; Determining this is slow if the method were implemented as C++ method (using unwind info). In .asm file by adding JIT_MemSet_End
; marker it can be easily determined if exception happened in JIT_MemSet. Therefore, JIT_MemSet has been written in assembly instead of
; as C++ method.

    LEAF_ENTRY JIT_MemSet
    ands        w1, w1, #0xff
    orr         w1, w1, w1, lsl #8
    orr         w1, w1, w1, lsl #0x10
    orr         x1, x1, x1, lsl #0x20

    b           JIT_MemSet_bottom
JIT_MemSet_top
    stp         x1, x1, [x0], #16
JIT_MemSet_bottom
    subs        x2, x2, #16
    bge        JIT_MemSet_top

    tbz         x2, #3, JIT_MemSet_tbz4
    str         x1, [x0], #8
JIT_MemSet_tbz4
    tbz         x2, #2, JIT_MemSet_tbz2
    str         w1, [x0], #4
JIT_MemSet_tbz2
    tbz         x2, #1, JIT_MemSet_tbz1
    strh        w1, [x0], #2
JIT_MemSet_tbz1
    tbz         x2, #0, JIT_MemSet_ret
    strb        w1, [x0]
JIT_MemSet_ret
    ret         lr
    LEAF_END

    LEAF_ENTRY JIT_MemSet_End
    nop
    LEAF_END


; See comments above for JIT_MemSet

;void JIT_MemCpy(void *dst, const void *src, SIZE_T count)
;{
;    count-=16;
;
;    while(count >= 0)
;    {
;        *(unit64_t*)dst = *(unit64_t*)src;
;        dst = (unit64_t*)dst + 1;
;        src = (unit64_t*)src + 1;
;        *(unit64_t*)dst = *(unit64_t*)src;
;        dst = (unit64_t*)dst + 1;
;        src = (unit64_t*)src + 1;
;        count-=16;
;    }
;
;    if(count & 8)
;    {
;        *(unit64_t*)dst = *(unit64_t*)src;
;        dst = (unit64_t*)dst + 1;
;        src = (unit64_t*)src + 1;
;    }
;
;    if(count & 4)
;    {
;        *(unit32_t*)dst = *(unit32_t*)src;
;        dst = (unit32_t*)dst + 1;
;        src = (unit32_t*)src + 1;
;    }
;
;    if(count & 2)
;    {
;        *(unit16_t*)dst = *(unit16_t*)src;
;        dst = (unit16_t*)dst + 1;
;        src = (unit16_t*)src + 1;
;    }
;
;    if(count & 1)
;    {
;        *(unit8_t*)dst = *(unit8_t*)src;
;    }
;}
;

; Assembly code corresponding to above C++ method.
; See comments above for JIT_MemSet method
    LEAF_ENTRY JIT_MemCpy
    b           JIT_MemCpy_bottom
JIT_MemCpy_top
    ldp         x8, x9, [x1], #16
    stp         x8, x9, [x0], #16
JIT_MemCpy_bottom
    subs        x2, x2, #16
    bge         JIT_MemCpy_top

    tbz         x2, #3, JIT_MemCpy_tbz4
    ldr         x8, [x1], #8
    str         x8, [x0], #8
JIT_MemCpy_tbz4
    tbz         x2, #2, JIT_MemCpy_tbz2
    ldr         w8, [x1], #4
    str         w8, [x0], #4
JIT_MemCpy_tbz2
    tbz         x2, #1, JIT_MemCpy_tbz1
    ldrsh       w8, [x1], #2
    strh        w8, [x0], #2
JIT_MemCpy_tbz1
    tbz         x2, #0, JIT_MemCpy_ret
    ldrsb       w8, [x1]
    strb        w8, [x0]
JIT_MemCpy_ret
    ret         lr
    LEAF_END

    LEAF_ENTRY JIT_MemCpy_End
    nop
    LEAF_END

; Must be at very end of file
    END
