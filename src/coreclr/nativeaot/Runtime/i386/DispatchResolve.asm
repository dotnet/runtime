;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

        .586
        .model  flat
        option  casemap:none
        .code

include AsmMacros.inc

EXTERN RhpCidResolve : PROC
EXTERN _RhpUniversalTransitionTailCall@0 : PROC

EXTERN _g_pDispatchCache : DWORD

;; Dispatching version of RhpResolveInterfaceMethod
FASTCALL_FUNC RhpInterfaceDispatch, 0
ALTERNATE_ENTRY _RhpInterfaceDispatch

        ;; Trigger an AV if we're dispatching on a null this.
        ;; The exception handling infrastructure is aware of the fact that this is the first
        ;; instruction of RhpInterfaceDispatch and uses it to translate an AV here
        ;; to a NullReferenceException at the callsite.
        cmp     dword ptr [ecx], ecx

        ;; eax currently contains the indirection cell address.
        ;; Save ebx so we can use it as scratch for the MethodTable pointer.
        push    ebx
        mov     ebx, [ecx]              ;; load object's MethodTable
        cmp     dword ptr [eax], ebx    ;; is this the monomorphic MethodTable?
        jne     Hashtable

        pop     ebx
        mov     eax, [eax + 4]          ;; load the cached monomorphic resolved code address
        jmp     eax

      Hashtable:

        ;; ebx = MethodTable, eax = cell, ecx = this, edx = arg2
        ;; [esp] = saved ebx (from push before monomorphic check)
        ;; Look up the target in the dispatch cache hashtable (GenericCache<Key, nint>).
        push    esi
        push    edi
        push    ecx
        push    edx
        push    eax
        push    ebx

        ;; Stack layout from esp:
        ;; [esp+0]  = MethodTable
        ;; [esp+4]  = cell
        ;; [esp+8]  = arg2
        ;; [esp+12] = this
        ;; [esp+16] = saved edi
        ;; [esp+20] = saved esi
        ;; [esp+24] = saved ebx (from push before monomorphic check)

        ;; Load the _table field (Entry[]) from the cache struct.
        mov     edi, dword ptr [_g_pDispatchCache]
        mov     edi, dword ptr [edi]

        ;; Compute 32-bit hash from Key.GetHashCode():
        ;; hash = RotateLeft(dispatchCell, 16) ^ objectType
        ;; On 32-bit, IntPtr.GetHashCode() is identity.
        mov     ecx, eax
        rol     ecx, 10h
        xor     ecx, ebx

        ;; HashToBucket: bucket = ((uint)hash * 0x9E3779B9) >> hashShift
        imul    ebx, ecx, -1640531527
        movzx   ecx, byte ptr [edi + 8]
        shr     ebx, cl
        xor     ecx, ecx

      ProbeLoop:
        ;; Compute entry address: table + 8 + (index + 1) * 16
        lea     eax, [ebx + 1]
        shl     eax, 4
        lea     eax, [edi + eax + 8]

        ;; Read version snapshot before key comparison (seqlock protocol).
        mov     edx, dword ptr [eax]
        test    edx, 1
        jne     ProbeMiss

        ;; Compare key (dispatchCell, objectType)
        mov     esi, dword ptr [esp + 4]
        cmp     esi, dword ptr [eax + 4]
        jne     ProbeMiss
        mov     esi, dword ptr [esp]
        cmp     esi, dword ptr [eax + 8]
        jne     ProbeMiss

        ;; Read the cached code pointer, then re-verify the version has not changed.
        mov     esi, dword ptr [eax + 0Ch]
        cmp     edx, dword ptr [eax]
        jne     CacheMiss

        ;; Dispatch to cached target.
        mov     eax, esi
        add     esp, 8
        pop     edx
        pop     ecx
        pop     edi
        pop     esi
        pop     ebx
        jmp     eax

      ProbeMiss:
        ;; If version is zero the rest of the bucket is unclaimed - stop probing.
        test    edx, edx
        je      CacheMiss

        ;; Quadratic reprobe: i++; index = (index + i) & tableMask
        inc     ecx
        add     ebx, ecx
        mov     eax, dword ptr [edi + 4]
        add     eax, -2
        and     ebx, eax
        cmp     ecx, 8
        jl      ProbeLoop

      CacheMiss:
        add     esp, 4
        pop     eax
        pop     edx
        pop     ecx
        pop     edi
        pop     esi
        pop     ebx

        ;; Setup call to Universal Transition thunk
        push    ebp
        mov     ebp, esp
        push    eax
        lea     eax, [RhpCidResolve]
        push    eax

        jmp     _RhpUniversalTransitionTailCall@0

FASTCALL_ENDFUNC

end
