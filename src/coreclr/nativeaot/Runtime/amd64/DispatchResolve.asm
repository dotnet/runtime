;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

include AsmMacros.inc


EXTERN RhpCidResolve : PROC
EXTERN RhpCidResolve_Worker : PROC
EXTERN RhpUniversalTransitionTailCall : PROC
EXTERN RhpUniversalTransitionGuardedTailCall : PROC

EXTERN g_pDispatchCache : QWORD

EXTERN __guard_dispatch_icall_fptr : QWORD

;; Macro that generates an interface dispatch or dispatch resolve stub.
;; DispatchName: the name of the dispatch entry point
;; Guarded: if non-zero, validate indirect call targets using Control Flow Guard
;; ReturnTarget: if non-zero, return the resolved target in rax instead of dispatching to it
;; DispatchCellReg: register containing the indirection cell address
;; VersionReg32: scratch register used for entry version checks
INTERFACE_DISPATCH macro DispatchName, Guarded, ReturnTarget, DispatchCellReg, VersionReg32

LOCAL Hashtable, ProbeLoop, ProbeMiss, CacheMiss, SlowPath

LEAF_ENTRY DispatchName, _TEXT

        ;; Load the MethodTable from the object instance in rcx.
        ;; Trigger an AV if we're dispatching on a null this.
        ;; The exception handling infrastructure is aware of the fact that this is the first
        ;; instruction of the dispatch stub and uses it to translate an AV here
        ;; to a NullReferenceException at the callsite.
        mov     r10, [rcx]

        ;; DispatchCellReg currently contains the indirection cell address.
        cmp     qword ptr [DispatchCellReg], r10 ;; is this the monomorhpic MethodTable?
        jne     Hashtable

        mov     rax, [DispatchCellReg + 8] ;; load the cached monomorphic resolved code address into rax
if ReturnTarget ne 0
        ret
else
if Guarded ne 0
        jmp     [__guard_dispatch_icall_fptr]
else
        jmp     rax
endif
endif

      Hashtable:

        ;; r10 = MethodTable, DispatchCellReg = indirection cell address
        ;; Look up the target in the dispatch cache hashtable (GenericCache<Key, nint>).
if ReturnTarget ne 0
        ;; Preserve this for the slow path. The return-only helper doesn't need
        ;; to preserve the rest of the argument registers.
        mov     [rsp + 8h], rcx
else
        ;; Spill argument registers to the caller-provided shadow space
        ;; so we don't modify rsp (this is a LEAF_ENTRY with no unwind info).
        mov     [rsp + 8h], rcx
        mov     [rsp + 10h], rdx
        mov     [rsp + 18h], r8
        mov     [rsp + 20h], r9
endif

        ;; Load the _table field (Entry[]) from the cache struct.
        mov     r8, qword ptr [g_pDispatchCache]
        mov     r8, qword ptr [r8]

        ;; Compute 32-bit hash from Key.GetHashCode():
        ;; hash = IntPtr.GetHashCode(RotateLeft(dispatchCell, 16) ^ objectType)
        mov     rcx, DispatchCellReg
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
        mov     VersionReg32, dword ptr [rax]
        test    VersionReg32, 1
        jne     ProbeMiss

        ;; Compare key (dispatchCell, objectType)
        cmp     DispatchCellReg, qword ptr [rax + 8]
        jne     ProbeMiss
        cmp     r10, qword ptr [rax + 10h]
        jne     ProbeMiss

        ;; Read the cached code pointer, then re-verify the version has not changed.
        mov     r10, qword ptr [rax + 18h]
        cmp     VersionReg32, dword ptr [rax]
        jne     CacheMiss

        ;; Dispatch to cached target.
        mov     rax, r10

if ReturnTarget ne 0
        ret
else
        mov     r9, [rsp + 20h]
        mov     r8, [rsp + 18h]
        mov     rdx, [rsp + 10h]
        mov     rcx, [rsp + 8h]

if Guarded ne 0
        jmp     [__guard_dispatch_icall_fptr]
else
        jmp     rax
endif
endif

      ProbeMiss:
        ;; If version is zero the rest of the bucket is unclaimed - stop probing.
        test    VersionReg32, VersionReg32
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
if ReturnTarget ne 0
        mov     rcx, [rsp + 8h]
        mov     rdx, DispatchCellReg
        jmp     RhpCidResolve_Worker
else
        mov     r9, [rsp + 20h]
        mov     r8, [rsp + 18h]
        mov     rdx, [rsp + 10h]
        mov     rcx, [rsp + 8h]

      SlowPath:
        ;; DispatchCellReg contains indirection cell address
        lea     r10, RhpCidResolve
if Guarded ne 0
        jmp     RhpUniversalTransitionGuardedTailCall
else
        jmp     RhpUniversalTransitionTailCall
endif
endif

LEAF_END DispatchName, _TEXT

        endm

        INTERFACE_DISPATCH RhpInterfaceDispatch, 0, 0, r11, edx
        INTERFACE_DISPATCH RhpInterfaceDispatchGuarded, 1, 0, r11, edx
        INTERFACE_DISPATCH RhpDispatchResolve, 0, 1, rdx, r11d

end
