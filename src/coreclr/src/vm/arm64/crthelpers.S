// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
//

#include "unixasmmacros.inc"

//
// ==--==

// Calls to JIT_MemSet is emitted by jit for initialization of large structs. 
// We need to provide our own implementation of memset instead of using the ones in crt because crt implementation does not gurantee 
// that aligned 8/4/2 - byte memory will be written atomically. This is required because members in a struct can be read atomically 
// and their values should be written atomically.
// 
//
//void JIT_MemSet(void *dst, int val, SIZE_T count)
//
//    uint64_t valEx = (unsigned char)val;
//    valEx = valEx | valEx << 8;
//    valEx = valEx | valEx << 16;
//    valEx = valEx | valEx << 32;
//
//    size_t dc_zva_size = 4ULL << DCZID_EL0.BS;
//
//    uint64_t use_dc_zva = (val == 0) && !DCZID_EL0.p ? count / (2 * dc_zva_size) : 0; // ~Minimum size (assumes worst case alignment)
//
//    // If not aligned then make it 8-byte aligned   
//    if(((uint64_t)dst&0xf) != 0)
//    {
//        // Calculate alignment we can do without exceeding count
//        // Use math to avoid introducing more unpredictable branches
//        // Due to inherent mod in lsr, ~7 is used instead of ~0 to handle count == 0
//        // Note logic will fail is count >= (1 << 61).  But this exceeds max physical memory for arm64
//        uint8_t align = (dst & 0x7) & (~uint64_t(7) >> (countLeadingZeros(count) mod 64))
//
//        if(align&0x1)
//        {
//            *(unit8_t*)dst = (unit8_t)valEx;
//            dst = (unit8_t*)dst + 1;
//            count-=1;
//        }
//
//        if(align&0x2)
//        {
//            *(unit16_t*)dst = (unit16_t)valEx;
//            dst = (unit16_t*)dst + 1;
//            count-=2;
//        }
//
//        if(align&0x4)
//        {
//            *(unit32_t*)dst = (unit32_t)valEx;
//            dst = (unit32_t*)dst + 1;
//            count-=4;
//        }
//    }
//
//    if(use_dc_zva)
//    {
//        // If not aligned then make it aligned to dc_zva_size
//        if(dst&0x8)
//        {
//            *(uint64_t*)dst = (uint64_t)valEx;
//            dst = (uint64_t*)dst + 1;
//            count-=8;
//        }
//
//        while(dst & (dc_zva_size - 1))
//        {
//            *(uint64_t*)dst = valEx;
//            dst = (uint64_t*)dst + 1;
//            *(uint64_t*)dst = valEx;
//            dst = (uint64_t*)dst + 1;
//            count-=16;
//        }
//
//        count -= dc_zva_size;
//
//        while(count >= 0)
//        {
//            dc_zva(dst);
//            dst = (uint8_t*)dst + dc_zva_size;
//            count-=dc_zva_size;
//        }
//
//        count += dc_zva_size;
//    }
//
//    count-=16;
//
//    while(count >= 0)
//    {
//        *(uint64_t*)dst = valEx;
//        dst = (uint64_t*)dst + 1;
//        *(uint64_t*)dst = valEx;
//        dst = (uint64_t*)dst + 1;
//        count-=16;
//    }
//
//    if(count & 8)
//    {
//        *(uint64_t*)dst = valEx;
//        dst = (uint64_t*)dst + 1;
//    }
//
//    if(count & 4)
//    {
//        *(uint32_t*)dst = (uint32_t)valEx;
//        dst = (uint32_t*)dst + 1;
//    }
//
//    if(count & 2)
//    {
//        *(uint16_t*)dst = (uint16_t)valEx;
//        dst = (uint16_t*)dst + 1;
//    }
//
//    if(count & 1)
//    {
//        *(uint8_t*)dst = (uint8_t)valEx;
//    }
//
//

// Assembly code corresponding to above C++ method. JIT_MemSet can AV and clr exception personality routine needs to 
// determine if the exception has taken place inside JIT_Memset in order to throw corresponding managed exception.
// Determining this is slow if the method were implemented as C++ method (using unwind info). In .asm file by adding JIT_MemSet_End
// marker it can be easily determined if exception happened in JIT_MemSet. Therefore, JIT_MemSet has been written in assembly instead of 
// as C++ method.

LEAF_ENTRY JIT_MemSet, _TEXT
    ands        w8, w1, #0xff
    mrs         x3, DCZID_EL0                      // x3 = DCZID_EL0
    mov         x6, #4
    lsr         x11, x2, #3                        // x11 = count >> 3

    orr         w8, w8, w8, lsl #8
    and         x5, x3, #0xf                       // x5 = dczid_el0.bs
    csel        x11, x11, xzr, eq                  // x11 = (val == 0) ? count >> 3 : 0
    tst         x3, (1 << 4)

    orr         w8, w8, w8, lsl #0x10
    csel        x11, x11, xzr, eq                  // x11 = (val == 0) && !DCZID_EL0.p ? count >> 3 : 0
    ands        x3, x0, #7                         // x3 = dst & 7
    lsl         x9, x6, x5                         // x9 = size

    orr         x8, x8, x8, lsl #0x20
    lsr         x11, x11, x5                       // x11 = (val == 0) && !DCZID_EL0.p ? count >> (3 + DCZID_EL0.bs) : 0
    sub         x10, x9, #1                        // x10 = mask

    b.eq        LOCAL_LABEL(JIT_MemSet_0x80)

    movn        x4, #7
    clz         x5, x2
    lsr         x4, x4, x5
    and         x3, x3, x4

    tbz         x3, #0, LOCAL_LABEL(JIT_MemSet_0x2c)
    strb        w8, [x0], #1
    sub         x2, x2, #1
LOCAL_LABEL(JIT_MemSet_0x2c):
    tbz         x3, #1, LOCAL_LABEL(JIT_MemSet_0x5c)
    strh        w8, [x0], #2
    sub         x2, x2, #2
LOCAL_LABEL(JIT_MemSet_0x5c):
    tbz         x3, #2, LOCAL_LABEL(JIT_MemSet_0x80)
    str         w8, [x0], #4
    sub         x2, x2, #4
LOCAL_LABEL(JIT_MemSet_0x80):
    cbz         x11, LOCAL_LABEL(JIT_MemSet_0x9c)
    tbz         x0, #3, LOCAL_LABEL(JIT_MemSet_0x84)
    str         x8, [x0], #8
    sub         x2, x2, #8

    b           LOCAL_LABEL(JIT_MemSet_0x85)
LOCAL_LABEL(JIT_MemSet_0x84):
    stp         x8, x8, [x0], #16
    sub         x2, x2, #16
LOCAL_LABEL(JIT_MemSet_0x85):
    tst         x0, x10
    b.ne        LOCAL_LABEL(JIT_MemSet_0x84)

    b           LOCAL_LABEL(JIT_MemSet_0x8a)
LOCAL_LABEL(JIT_MemSet_0x88):
    dc          zva, x0
    add         x0, x0, x9
LOCAL_LABEL(JIT_MemSet_0x8a):
    subs        x2, x2, x9
    b.ge        LOCAL_LABEL(JIT_MemSet_0x88)

LOCAL_LABEL(JIT_MemSet_0x8c):
    add         x2, x2, x9

LOCAL_LABEL(JIT_MemSet_0x9c):
    b           LOCAL_LABEL(JIT_MemSet_0xa8)
LOCAL_LABEL(JIT_MemSet_0xa0):
    stp         x8, x8, [x0], #16
LOCAL_LABEL(JIT_MemSet_0xa8):
    subs        x2, x2, #16
    b.ge        LOCAL_LABEL(JIT_MemSet_0xa0)

LOCAL_LABEL(JIT_MemSet_0xb0):
    tbz         x2, #3, LOCAL_LABEL(JIT_MemSet_0xb4)
    str         x8, [x0], #8
LOCAL_LABEL(JIT_MemSet_0xb4):
    tbz         x2, #2, LOCAL_LABEL(JIT_MemSet_0xc8)
    str         w8, [x0], #4
LOCAL_LABEL(JIT_MemSet_0xc8):
    tbz         x2, #1, LOCAL_LABEL(JIT_MemSet_0xdc)
    strh        w8, [x0], #2
LOCAL_LABEL(JIT_MemSet_0xdc):
    tbz         x2, #0, LOCAL_LABEL(JIT_MemSet_0xe8)
    strb        w8, [x0]
LOCAL_LABEL(JIT_MemSet_0xe8):
    ret         lr
LEAF_END_MARKED JIT_MemSet, _TEXT

// See comments above for JIT_MemSet

//void JIT_MemCpy(void *dst, const void *src, SIZE_T count)
//
//    // If not aligned then make it 8-byte aligned   
//    if(((uintptr_t)dst&0x7) != 0)
//    {
//        // Calculate alignment we can do without exceeding count
//        // Use math to avoid introducing more unpredictable branches
//        // Due to inherent mod in lsr, ~7 is used instead of ~0 to handle count == 0
//        // Note logic will fail is count >= (1 << 61).  But this exceeds max physical memory for arm64
//        uint8_t align = (dst & 0x7) & (~uint64_t(7) >> (countLeadingZeros(count) mod 64))
//
//        if(align&0x1)
//        {
//            *(unit8_t*)dst = *(unit8_t*)src;
//            dst = (unit8_t*)dst + 1;
//            src = (unit8_t*)src + 1;
//            count-=1;
//        }
//
//        if(align&0x2)
//        {
//            *(unit16_t*)dst = *(unit16_t*)src;
//            dst = (unit16_t*)dst + 1;
//            src = (unit16_t*)src + 1;
//            count-=2;
//        }
//
//        if(align&0x4)
//        {
//            *(unit32_t*)dst = *(unit32_t*)src;
//            dst = (unit32_t*)dst + 1;
//            src = (unit32_t*)src + 1;
//            count-=4;
//        }
//    }
//
//    count-=16;
//
//    while(count >= 0)
//    {
//        *(unit64_t*)dst = *(unit64_t*)src;
//        dst = (unit64_t*)dst + 1;
//        src = (unit64_t*)src + 1;
//        *(unit64_t*)dst = *(unit64_t*)src;
//        dst = (unit64_t*)dst + 1;
//        src = (unit64_t*)src + 1;
//        count-=16;
//    }
//
//    if(count & 8)
//    {
//        *(unit64_t*)dst = *(unit64_t*)src;
//        dst = (unit64_t*)dst + 1;
//        src = (unit64_t*)src + 1;
//    }
//
//    if(count & 4)
//    {
//        *(unit32_t*)dst = *(unit32_t*)src;
//        dst = (unit32_t*)dst + 1;
//        src = (unit32_t*)src + 1;
//    }
//
//    if(count & 2)
//    {
//        *(unit16_t*)dst = *(unit16_t*)src;
//        dst = (unit16_t*)dst + 1;
//        src = (unit16_t*)src + 1;
//    }
//
//    if(count & 1)
//    {
//        *(unit8_t*)dst = *(unit8_t*)src;
//    }
//
//

// Assembly code corresponding to above C++ method.
// See comments above for JIT_MemSet method
LEAF_ENTRY JIT_MemCpy, _TEXT
    ands        x3, x0, #7
    movn        x4, #7
    clz         x5, x2
    b.eq        LOCAL_LABEL(JIT_MemCpy_0xa8)
    lsr         x4, x4, x5
    and         x3, x3, x4
    tbz         x3, #0, LOCAL_LABEL(JIT_MemCpy_0x2c)
    ldrsb       w8, [x1], #1
    strb        w8, [x0], #1
    sub         x2, x2, #1
LOCAL_LABEL(JIT_MemCpy_0x2c):
    tbz         x3, #1, LOCAL_LABEL(JIT_MemCpy_0x5c)
    ldrsh       w8, [x1], #2
    strh        w8, [x0], #2
    sub         x2, x2, #2
LOCAL_LABEL(JIT_MemCpy_0x5c):
    tbz         x3, #2, LOCAL_LABEL(JIT_MemCpy_0xa8)
    ldr         w8, [x1], #4
    str         w8, [x0], #4
    sub         x2, x2, #4
    b           LOCAL_LABEL(JIT_MemCpy_0xa8)
LOCAL_LABEL(JIT_MemCpy_0xa0):
    ldp         x8, x9, [x1], #16
    stp         x8, x9, [x0], #16
LOCAL_LABEL(JIT_MemCpy_0xa8):
    subs        x2, x2, #16
    b.ge        LOCAL_LABEL(JIT_MemCpy_0xa0)
LOCAL_LABEL(JIT_MemCpy_0xb0):
    tbz         x2, #3, LOCAL_LABEL(JIT_MemCpy_0xb4)
    ldr         x8, [x1], #8
    str         x8, [x0], #8
LOCAL_LABEL(JIT_MemCpy_0xb4):
    tbz         x2, #2, LOCAL_LABEL(JIT_MemCpy_0xc8)
    ldr         w8, [x1], #4
    str         w8, [x0], #4
LOCAL_LABEL(JIT_MemCpy_0xc8):
    tbz         x2, #1, LOCAL_LABEL(JIT_MemCpy_0xdc)
    ldrsh       w8, [x1], #2
    strh        w8, [x0], #2
LOCAL_LABEL(JIT_MemCpy_0xdc):
    tbz         x2, #0, LOCAL_LABEL(JIT_MemCpy_0xe8)
    ldrsb       w8, [x1]
    strb        w8, [x0]
LOCAL_LABEL(JIT_MemCpy_0xe8):
    ret         lr
LEAF_END_MARKED JIT_MemCpy, _TEXT
