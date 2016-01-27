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
//    uintptr_t valEx = (char)val;
//    valEx = valEx | valEx << 8;
//    valEx = valEx | valEx << 16;
//    valEx = valEx | valEx << 32;
//
//    // If not aligned then make it 8-byte aligned   
//    if(((uintptr_t)dst&0x7) != 0)
//    {
//        if(((uintptr_t)dst&0x3) == 0)
//        {
//            *(UINT*)dst = (UINT)valEx;
//            dst = (UINT*)dst + 1;
//            count-=4;
//        }
//        else if(((uintptr_t)dst&0x1) == 0)
//        {
//            while(count > 0 && ((uintptr_t)dst&0x7) != 0)
//            {
//                *(short*)dst = (short)valEx;
//                dst = (short*)dst + 1;
//                count-=2;
//            }
//        }
//        else
//        {
//            while(count > 0 && ((uintptr_t)dst&0x7) != 0)
//            {
//                *(char*)dst = (char)valEx;
//                dst = (char*)dst + 1;
//                count--;
//            }
//        }
//    }
//
//    while(count > 8)
//    {
//        *(uintptr_t*)dst = valEx;
//        dst = (uintptr_t*)dst + 1;
//        count-=8;
//    }
//
//    if(count & 4)
//    {
//        *(UINT*)dst = (UINT)valEx;
//        dst = (UINT*)dst + 1;
//    }
//
//    if(count & 2)
//    {
//        *(short*)dst = (short)valEx;
//        dst = (short*)dst + 1;
//    }
//
//    if(count & 1)
//    {
//        *(char*)dst = (char)valEx;
//    }
//
//

// Assembly code corresponding to above C++ method. JIT_MemSet can AV and clr exception personality routine needs to 
// determine if the exception has taken place inside JIT_Memset in order to throw corresponding managed exception.
// Determining this is slow if the method were implemented as C++ method (using unwind info). In .asm file by adding JIT_MemSet_End
// marker it can be easily determined if exception happened in JIT_MemSet. Therefore, JIT_MemSet has been written in assembly instead of 
// as C++ method.

LEAF_ENTRY JIT_MemSet, _TEXT
    sxtb        w8,w1
    sxtw        x8,w8
    orr         x8,x8,x8, lsl #8
    orr         x8,x8,x8, lsl #0x10
    orr         x9,x8,x8, lsl #0x20
    and         x8,x0,#7
    cbz         x8,LOCAL_LABEL(JIT_MemSet_0x7c)
    and         x8,x0,#3
    cbnz        x8,LOCAL_LABEL(JIT_MemSet_0x38)
    str         w9,[x0]
    add         x0,x0,#4
    mov         x8,#-4
    add         x2,x2,x8
    b           LOCAL_LABEL(JIT_MemSet_0x7c)
LOCAL_LABEL(JIT_MemSet_0x38):
    cbz         x2,LOCAL_LABEL(JIT_MemSet_0x7c)
    tbnz        x0,#0,LOCAL_LABEL(JIT_MemSet_0x60)
LOCAL_LABEL(JIT_MemSet_0x40):
    and         x8,x0,#7
    cbz         x8,LOCAL_LABEL(JIT_MemSet_0x7c)
    strh        w9,[x0]
    add         x0,x0,#2
    mov         x8,#-2
    add         x2,x2,x8
    cbnz        x2,LOCAL_LABEL(JIT_MemSet_0x40)
    b           LOCAL_LABEL(JIT_MemSet_0x7c)
LOCAL_LABEL(JIT_MemSet_0x60):
    and         x8,x0,#7
    cbz         x8,LOCAL_LABEL(JIT_MemSet_0x7c)
    strb        w9,[x0]
    add         x0,x0,#1
    mov         x8,#-1
    add         x2,x2,x8
    cbnz        x2,LOCAL_LABEL(JIT_MemSet_0x60)
LOCAL_LABEL(JIT_MemSet_0x7c):
    cmp         x2,#8
    bls         LOCAL_LABEL(JIT_MemSet_0xb8)
    mov         x8,#-9
    add         x8,x2,x8
    lsr         x8,x8,#3
    add         x11,x8,#1
    mov         x10,x0
    add         x8,x10,x11, lsl #3
LOCAL_LABEL(JIT_MemSet_0x9c):
    cmp         x10,x8
    beq         LOCAL_LABEL(JIT_MemSet_0xac)
    str         x9,[x10],#8
    b           LOCAL_LABEL(JIT_MemSet_0x9c)
LOCAL_LABEL(JIT_MemSet_0xac):
    mov         x8,#-8
    madd        x2,x11,x8,x2
    add         x0,x0,x11, lsl #3
LOCAL_LABEL(JIT_MemSet_0xb8):
    tbz         x2,#2,LOCAL_LABEL(JIT_MemSet_0xc4)
    str         w9,[x0]
    add         x0,x0,#4
LOCAL_LABEL(JIT_MemSet_0xc4):
    tbz         x2,#1,LOCAL_LABEL(JIT_MemSet_0xd0)
    strh        w9,[x0]
    add         x0,x0,#2
LOCAL_LABEL(JIT_MemSet_0xd0):
    tbz         x2,#0,LOCAL_LABEL(JIT_MemSet_0xd8)
    strb        w9,[x0]
LOCAL_LABEL(JIT_MemSet_0xd8):
    ret         lr
LEAF_END JIT_MemSet, _TEXT

// See comments above for JIT_MemSet

//void JIT_MemCpy(void *dst, const void *src, SIZE_T count)
//
//    // If not aligned then make it 8-byte aligned   
//    if(((uintptr_t)dst&0x7) != 0)
//    {
//        if(((uintptr_t)dst&0x3) == 0)
//        {
//            *(UINT*)dst = *(UINT*)src;
//            dst = (UINT*)dst + 1;
//            src = (UINT*)src + 1;
//            count-=4;
//        }
//        else if(((uintptr_t)dst&0x1) == 0)
//        {
//            while(count > 0 && ((uintptr_t)dst&0x7) != 0)
//            {
//                *(short*)dst = *(short*)src;
//                dst = (short*)dst + 1;
//                src = (short*)src + 1;
//                count-=2;
//            }
//        }
//        else
//        {
//            while(count > 0 && ((uintptr_t)dst&0x7) != 0)
//            {
//                *(char*)dst = *(char*)src;
//                dst = (char*)dst + 1;
//                src = (char*)src + 1;
//                count--;
//            }
//        }
//    }
//
//    while(count > 8)
//    {
//        *(uintptr_t*)dst = *(uintptr_t*)src;
//        dst = (uintptr_t*)dst + 1;
//        src = (uintptr_t*)src + 1;
//        count-=8;
//    }
//
//    if(count & 4)
//    {
//        *(UINT*)dst = *(UINT*)src;
//        dst = (UINT*)dst + 1;
//        src = (UINT*)src + 1;
//    }
//
//    if(count & 2)
//    {
//        *(short*)dst = *(short*)src;
//        dst = (short*)dst + 1;
//        src = (short*)src + 1;
//    }
//
//    if(count & 1)
//    {
//        *(char*)dst = *(char*)src;
//    }
//
//

// Assembly code corresponding to above C++ method.
// See comments above for JIT_MemSet method
LEAF_ENTRY JIT_MemCpy, _TEXT
    and         x8,x0,#7
    cbz         x8,LOCAL_LABEL(JIT_MemCpy_0x80)
    and         x8,x0,#3
    cbnz        x8,LOCAL_LABEL(JIT_MemCpy_0x2c)
    ldr         w8,[x1]
    str         w8,[x0]
    add         x0,x0,#4
    add         x1,x1,#4
    mov         x8,#-4
    add         x2,x2,x8
    b           LOCAL_LABEL(JIT_MemCpy_0x80)
LOCAL_LABEL(JIT_MemCpy_0x2c):
    cbz         x2,LOCAL_LABEL(JIT_MemCpy_0x80)
    tbnz        x0,#0,LOCAL_LABEL(JIT_MemCpy_0x5c)
LOCAL_LABEL(JIT_MemCpy_0x34):
    and         x8,x0,#7
    cbz         x8,LOCAL_LABEL(JIT_MemCpy_0x80)
    ldrsh       w8,[x1]
    strh        w8,[x0]
    add         x0,x0,#2
    add         x1,x1,#2
    mov         x8,#-2
    add         x2,x2,x8
    cbnz        x2,LOCAL_LABEL(JIT_MemCpy_0x34)
    b           LOCAL_LABEL(JIT_MemCpy_0x80)
LOCAL_LABEL(JIT_MemCpy_0x5c):
    and         x8,x0,#7
    cbz         x8,LOCAL_LABEL(JIT_MemCpy_0x80)
    ldrsb       w8,[x1]
    strb        w8,[x0]
    add         x0,x0,#1
    add         x1,x1,#1
    mov         x8,#-1
    add         x2,x2,x8
    cbnz        x2,LOCAL_LABEL(JIT_MemCpy_0x5c)
LOCAL_LABEL(JIT_MemCpy_0x80):
    cmp         x2,#8
    bls         LOCAL_LABEL(JIT_MemCpy_0xb4)
    mov         x8,#-9
    add         x8,x2,x8
    lsr         x8,x8,#3
    add         x9,x8,#1
    mov         x8,#-8
    madd        x2,x9,x8,x2
LOCAL_LABEL(JIT_MemCpy_0xa0):
    ldr         x8,[x1],#8
    str         x8,[x0],#8
    mov         x8,#-1
    add         x9,x9,x8
    cbnz        x9,LOCAL_LABEL(JIT_MemCpy_0xa0)
LOCAL_LABEL(JIT_MemCpy_0xb4):
    tbz         x2,#2,LOCAL_LABEL(JIT_MemCpy_0xc8)
    ldr         w8,[x1]
    str         w8,[x0]
    add         x0,x0,#4
    add         x1,x1,#4
LOCAL_LABEL(JIT_MemCpy_0xc8):
    tbz         x2,#1,LOCAL_LABEL(JIT_MemCpy_0xdc)
    ldrsh       w8,[x1]
    strh        w8,[x0]
    add         x0,x0,#2
    add         x1,x1,#2
LOCAL_LABEL(JIT_MemCpy_0xdc):
    tbz         x2,#0,LOCAL_LABEL(JIT_MemCpy_0xe8)
    ldrsb       w8,[x1]
    strb        w8,[x0]
LOCAL_LABEL(JIT_MemCpy_0xe8):
    ret         lr
LEAF_END JIT_MemCpy, _TEXT
