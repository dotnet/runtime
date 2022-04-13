; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

#include "ksarm64.h"
#include "asmconstants.h"
#include "asmmacros.h"

    IMPORT memset
    IMPORT memmove

; JIT_MemSet/JIT_MemCpy
;
; It is IMPORTANT that the exception handling code is able to find these guys
; on the stack, but on windows platforms we can just defer to the platform
; implementation.
;

; void JIT_MemSet(void* dest, int c, size_t count)
;
; Purpose:
;    Sets the first "count" bytes of the block of memory pointed byte
;    "dest" to the specified value (interpreted as an unsigned char).
;
; Entry:
;    RCX: void* dest    - Pointer to the block of memory to fill.
;    RDX: int c         - Value to be set.
;    R8:  size_t count  - Number of bytes to be set to the value.
;
; Exit:
;
; Uses:
;
; Exceptions:
;

    TEXTAREA
    
    LEAF_ENTRY JIT_MemSet
        cbz x2, JIT_MemSet_ret  ; check if count is zero, no bytes to set

        ldrb wzr, [x0]          ; check dest for null

        b     memset            ; forward to the CRT implementation

JIT_MemSet_ret
        ret         lr
    
    LEAF_END_MARKED JIT_MemSet

; void JIT_MemCpy(void* dest, const void* src, size_t count)
;
; Purpose:
;    Copies the values of "count" bytes from the location pointed to
;    by "src" to the memory block pointed by "dest".
;
; Entry:
;    RCX: void* dest             - Pointer to the destination array where content is to be copied.
;    RDX: const void* src        - Pointer to the source of the data to be copied.
;    R8:  size_t count           - Number of bytes to copy.
;
; Exit:
;
; Uses:
;
; Exceptions:
;
    LEAF_ENTRY JIT_MemCpy
        cbz x2, JIT_MemCpy_ret  ; check if count is zero, no bytes to set

        ldrb wzr, [x0]          ; check dest for null
        ldrb wzr, [x1]          ; check src for null

        b     memmove           ; forward to the CRT implementation

JIT_MemCpy_ret
    ret         lr

    LEAF_END_MARKED JIT_MemCpy

; Must be at very end of file
    END
