// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include <unixasmmacros.inc>

// Dispatching version of RhpResolveInterfaceMethod
.macro INTERFACE_DISPATCH DispatchName, ReturnTarget, DispatchCellReg

LEAF_ENTRY \DispatchName, _TEXT

        // Load the MethodTable from the object instance in rdi.
        // Trigger an AV if we're dispatching on a null this.
        // The exception handling infrastructure is aware of the fact that this is the first
        // instruction of RhpInterfaceDispatch and uses it to translate an AV here
        // to a NullReferenceException at the callsite.
        mov     r10, [rdi]

        // DispatchCellReg currently contains the indirection cell address.
        cmp     qword ptr [\DispatchCellReg], r10 // is this the monomorphic MethodTable?
        jne     1f

        mov     rax, [\DispatchCellReg + 8] // load the cached monomorphic resolved code address into rax
.if \ReturnTarget
        ret
.else
        jmp     rax
.endif

1:

        // r10 = MethodTable, DispatchCellReg = indirection cell address
        // Look up the target in the dispatch cache hashtable (GenericCache<Key, nint>).
.if !\ReturnTarget
        // Spill argument registers to the red zone below rsp
        // so we don't modify rsp (this is a LEAF_ENTRY with no unwind info).
        mov     [rsp - 8],  rcx
        mov     [rsp - 16], rdx
        mov     [rsp - 24], r8
        mov     [rsp - 32], r9
.endif

        // Load the _table field (Entry[]) from the cache struct.
        mov     r8, qword ptr [C_VAR(g_pDispatchCache)]
        mov     r8, qword ptr [r8]

        // Compute 32-bit hash from Key.GetHashCode():
        // hash = IntPtr.GetHashCode(RotateLeft(dispatchCell, 16) ^ objectType)
        mov     rcx, \DispatchCellReg
        rol     rcx, 0x10
        xor     rcx, r10
        mov     r9, rcx
        sar     r9, 0x20
        xor     r9d, ecx
        movsxd  r9, r9d

        // HashToBucket: bucket = (hash * 0x9E3779B97F4A7C15) >> hashShift
        mov     rax, 0x9E3779B97F4A7C15
        imul    r9, rax
        movzx   ecx, byte ptr [r8 + 0x10]
        shr     r9, cl
        xor     ecx, ecx

2:
        // Compute entry address: Element(table, index) = table + 0x10 + (index + 1) * 0x20
        lea     eax, [r9 + 1]
        movsxd  rax, eax
        shl     rax, 5
        lea     rax, [r8 + rax + 0x10]

        // Read version snapshot before key comparison (seqlock protocol).
        mov     edx, dword ptr [rax]
        test    edx, 1
        jne     3f

        // Compare key (dispatchCell, objectType)
        cmp     \DispatchCellReg, qword ptr [rax + 8]
        jne     3f
        cmp     r10, qword ptr [rax + 0x10]
        jne     3f

        // Read the cached code pointer, then re-verify the version has not changed.
        mov     r10, qword ptr [rax + 0x18]
        cmp     edx, dword ptr [rax]
        jne     4f

        // Dispatch to cached target.
        mov     rax, r10

.if \ReturnTarget
        ret
.else
        mov     r9,  [rsp - 32]
        mov     r8,  [rsp - 24]
        mov     rdx, [rsp - 16]
        mov     rcx, [rsp - 8]

        jmp     rax
.endif

3:
        // If version is zero the rest of the bucket is unclaimed — stop probing.
        test    edx, edx
        je      4f

        // Quadratic reprobe: i++; index = (index + i) & tableMask
        inc     ecx
        add     r9d, ecx
        mov     eax, dword ptr [r8 + 8]
        add     eax, 0xFFFFFFFE
        and     r9d, eax
        cmp     ecx, 8
        jl      2b

4:
.if \ReturnTarget
        jmp     C_FUNC(RhpCidResolve_Worker)
.else
        mov     r9,  [rsp - 32]
        mov     r8,  [rsp - 24]
        mov     rdx, [rsp - 16]
        mov     rcx, [rsp - 8]

        // DispatchCellReg contains indirection cell address
        lea     r10, [C_VAR(RhpCidResolve)]
        jmp     C_FUNC(RhpUniversalTransitionTailCall)
.endif

LEAF_END \DispatchName, _TEXT

.endm

        INTERFACE_DISPATCH RhpInterfaceDispatch, 0, r11
        INTERFACE_DISPATCH RhpDispatchResolve, 1, rsi
