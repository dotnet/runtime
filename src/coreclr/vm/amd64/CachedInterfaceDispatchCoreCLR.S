// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

#ifdef FEATURE_CACHED_INTERFACE_DISPATCH

// Stub dispatch routine for dispatch to a vtable slot
LEAF_ENTRY RhpVTableOffsetDispatch, _TEXT

        // r11 currently contains the indirection cell address.
        // load r11 to point to the vtable offset (which is stored in the m_pCache field).
        mov     r11, [r11 + OFFSETOF__InterfaceDispatchCell__m_pCache]

        // r11 now contains the VTableOffset where the upper 32 bits are the offset to adjust
        // to get to the VTable chunk
        mov     rax, r11
        shr     rax, 32

        // Load the MethodTable from the object instance in rdi, and add it to the vtable offset
        // to get the address in the vtable chunk list of what we want to dereference
#ifdef TARGET_APPLE
// Apple's linker has issues which break unwind info if
// an ALTERNATE_ENTRY is present in the middle of a function see https://github.com/dotnet/runtime/issues/119005
.cfi_endproc
#endif
    ALTERNATE_ENTRY RhpVTableOffsetDispatchAVLocation
#ifdef TARGET_APPLE
.cfi_startproc
#endif
        add     rax, [rdi]

        // Load the target address of the vtable chunk into rax
        mov     rax, [rax]

        // Compute the chunk offset
        shr     r11d, 16

        // Load the target address of the virtual function into rax
        mov     rax, [rax + r11]

        TAILJMP_RAX
LEAF_END RhpVTableOffsetDispatch, _TEXT

// On Input:
//    r11                    contains the address of the indirection cell
//  [rsp+0] m_ReturnAddress: contains the return address of caller to stub
NESTED_ENTRY RhpInterfaceDispatchSlow, _TEXT, NoHandler

        PROLOG_WITH_TRANSITION_BLOCK 384

        // r11 holds indirection cell from the caller. Save before clobbering.
        mov             r10, r11

        SAVE_UPPER_VECTOR_REGISTERS 0x00

        lea             rdi, [rsp + __PWTB_TransitionBlock]         // pTransitionBlock
        mov             rsi, r10                                    // indirection cell

        call            C_FUNC(CID_ResolveWorker)
        mov             r10, rax

        RESTORE_FLOAT_ARGUMENT_REGISTERS __PWTB_FloatArgumentRegisters
        RESTORE_UPPER_VECTOR_REGISTERS 0x00

        free_stack      __PWTB_StackAlloc
        POP_ARGUMENT_REGISTERS
        POP_CALLEE_SAVED_REGISTERS
        jmp             r10

NESTED_END RhpInterfaceDispatchSlow, _TEXT

// On Input:
//    r11                    contains the address of the indirection cell (which is the MethodPtrAux field of the delegate)
NESTED_ENTRY CID_VirtualOpenDelegateDispatch, _TEXT, NoHandler

        PROLOG_WITH_TRANSITION_BLOCK 384

        // r11 holds indirection cell from the caller. Save before clobbering.
        mov             r10, r11

        SAVE_UPPER_VECTOR_REGISTERS 0x00

        lea             rdi, [rsp + __PWTB_TransitionBlock]         // pTransitionBlock
        mov             rsi, r10                                    // indirection cell

        call            C_FUNC(CID_VirtualOpenDelegateDispatchWorker)
        mov             r10, rax

        RESTORE_FLOAT_ARGUMENT_REGISTERS __PWTB_FloatArgumentRegisters
        RESTORE_UPPER_VECTOR_REGISTERS 0x00

        free_stack      __PWTB_StackAlloc
        POP_ARGUMENT_REGISTERS
        POP_CALLEE_SAVED_REGISTERS
        jmp             r10

NESTED_END CID_VirtualOpenDelegateDispatch, _TEXT

#endif // FEATURE_CACHED_INTERFACE_DISPATCH
