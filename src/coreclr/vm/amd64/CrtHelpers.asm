; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

; ==++==
;

;
; ==--==
include AsmMacros.inc

extern memset:proc
extern memmove:proc

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
LEAF_ENTRY JIT_MemSet, _TEXT
        test    r8, r8                  ; check if count is zero
        jz      Exit_MemSet             ; if zero, no bytes to set

        cmp     byte ptr [rcx], 0       ; check dest for null

        jmp     memset                  ; forward to the CRT implementation

Exit_MemSet:
        ret

LEAF_END_MARKED JIT_MemSet, _TEXT

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
LEAF_ENTRY JIT_MemCpy, _TEXT
        test    r8, r8                  ; check if count is zero
        jz      Exit_MemCpy             ; if zero, no bytes to copy

        cmp     byte ptr [rcx], 0       ; check dest for null
        cmp     byte ptr [rdx], 0       ; check src for null

        ; Use memmove to handle overlapping buffers for better
        ; compatibility with .NET Framework. Needing to handle
        ; overlapping buffers in cpblk is undefined by the spec.
        jmp     memmove                 ; forward to the CRT implementation

Exit_MemCpy:
        ret

LEAF_END_MARKED JIT_MemCpy, _TEXT
		end
