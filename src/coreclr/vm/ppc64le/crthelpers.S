// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"
#include "asmconstants.h"

// JIT_MemSet/JIT_MemCpy
//
// It is IMPORTANT that the exception handling code is able to find these guys
// on the stack, but on non-windows platforms we can just defer to the platform
// implementation.
//

// void JIT_MemSet(void* dest, int c, size_t count)
//
// Purpose:
//    Sets the first "count" bytes of the block of memory pointed byte
//    "dest" to the specified value (interpreted as an unsigned char).
//
// Entry:
//    R3: void* dest    - Pointer to the block of memory to fill.
//    R4: int c         - Value to be set.
//    R5: size_t count  - Number of bytes to be set to the value.
//
// Exit:
//
// Uses:
//
// Exceptions:
//
LEAF_ENTRY JIT_MemSet, _TEXT
	bl abort
	nop
LEAF_END_MARKED JIT_MemSet, _TEXT

// void JIT_MemCpy(void* dest, const void* src, size_t count)
//
// Purpose:
//    Copies the values of "count" bytes from the location pointed to
//    by "src" to the memory block pointed by "dest".
//
// Entry:
//    R3: void* dest             - Pointer to the destination array where content is to be copied.
//    R4: const void* src        - Pointer to the source of the data to be copied.
//    R5: size_t count           - Number of bytes to copy.
//
// Exit:
//
// Uses:
//
// Exceptions:
//
LEAF_ENTRY JIT_MemCpy, _TEXT
	bl abort
	nop
LEAF_END_MARKED JIT_MemCpy, _TEXT
