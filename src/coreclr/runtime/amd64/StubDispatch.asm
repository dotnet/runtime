;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

include AsmMacros_Shared.inc


ifdef FEATURE_CACHED_INTERFACE_DISPATCH

EXTERN RhpInterfaceDispatchSlow : PROC

EXTERN g_pDispatchCache : QWORD

;; On Input:
;;    rcx                    contains the this pointer
;;    r11                    contains the address of the interface dispatch cell
LEAF_ENTRY RhpInterfaceDispatch, _TEXT

        ;; Load the MethodTable from the object instance in rcx.
        ;; Trigger an AV if we're dispatching on a null this.
        ;; The exception handling infrastructure is aware of the fact that this is the first
        ;; instruction of RhpInterfaceDispatch and uses it to translate an AV here
        ;; to a NullReferenceException at the callsite.
        mov     r10, [rcx]

        ;; r11 currently contains the indirection cell address.
        cmp     qword ptr [r11], r10 ;; is this the monomorphic MethodTable?
        jne     Hashtable

        mov     rax, [r11 + 8] ;; load the cached monomorphic resolved code address into rax
        jmp     rax

      Hashtable:

        ;; r10 = MethodTable, r11 = indirection cell address
        ;; Look up the target in the dispatch cache hashtable (see CachedInterfaceDispatch.cpp).
        ;; Spill argument registers to the caller-provided shadow space
        ;; so we don't modify rsp (this is a LEAF_ENTRY with no unwind info).
        mov     [rsp + 8h], rcx
        mov     [rsp + 10h], rdx
        mov     [rsp + 18h], r8
        mov     [rsp + 20h], r9

        ;; Load the current table.
        mov     r8, qword ptr [g_pDispatchCache]
        mov     r8, qword ptr [r8]

        ;; Compute 32-bit hash:
        ;; hash = IntPtr.GetHashCode(RotateLeft(dispatchCell, 16) ^ objectType)
        mov     rcx, r11
        rol     rcx, 10h
        xor     rcx, r10
        mov     r9, rcx
        sar     r9, 20h
        xor     r9d, ecx
        movsxd  r9, r9d

        ;; HashToBucket: bucket = (hash * 0x9E3779B97F4A7C15) >> hashShift
        mov     rax, 9E3779B97F4A7C15h
        imul    r9, rax
        movzx   ecx, byte ptr [r8 + 10h]
        shr     r9, cl
        xor     ecx, ecx

      ProbeLoop:
        ;; Compute entry address: Element(table, index) = table + 10h + (index + 1) * 20h
        lea     eax, [r9 + 1]
        movsxd  rax, eax
        shl     rax, 5
        lea     rax, [r8 + rax + 10h]

        ;; Read version snapshot before key comparison (seqlock protocol).
        mov     edx, dword ptr [rax]
        test    edx, 1
        jne     ProbeMiss

        ;; Compare key (dispatchCell, objectType)
        cmp     r11, qword ptr [rax + 8]
        jne     ProbeMiss
        cmp     r10, qword ptr [rax + 10h]
        jne     ProbeMiss

        ;; Read the cached code pointer, then re-verify the version has not changed.
        mov     r10, qword ptr [rax + 18h]
        cmp     edx, dword ptr [rax]
        jne     CacheMiss

        ;; Dispatch to cached target.
        mov     rax, r10

        mov     r9, [rsp + 20h]
        mov     r8, [rsp + 18h]
        mov     rdx, [rsp + 10h]
        mov     rcx, [rsp + 8h]

        jmp     rax

      ProbeMiss:
        ;; If version is zero the rest of the bucket is unclaimed - stop probing.
        test    edx, edx
        je      CacheMiss

        ;; Quadratic reprobe: i++; index = (index + i) & tableMask
        inc     ecx
        add     r9d, ecx
        mov     eax, dword ptr [r8 + 8]
        add     eax, 0FFFFFFFEh
        and     r9d, eax
        cmp     ecx, 8
        jl      ProbeLoop

      CacheMiss:
        mov     r9, [rsp + 20h]
        mov     r8, [rsp + 18h]
        mov     rdx, [rsp + 10h]
        mov     rcx, [rsp + 8h]

        ;; r11 still contains the indirection cell address.
        jmp     RhpInterfaceDispatchSlow

LEAF_END RhpInterfaceDispatch, _TEXT

endif ;; FEATURE_CACHED_INTERFACE_DISPATCH

end
