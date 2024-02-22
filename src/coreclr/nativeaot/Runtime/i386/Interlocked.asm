;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

        .586
        .xmm
        .model  flat
        option  casemap:none
        .code

include AsmMacros.inc

FASTCALL_FUNC  RhpLockCmpXchg64, 20

_value$ = 8
_comparand$ = 16

        mov     eax, DWORD PTR _comparand$[esp-4]
        mov     edx, DWORD PTR _comparand$[esp]
        push    ebx
        mov     ebx, DWORD PTR _value$[esp]
        push    esi
        mov     esi, ecx
        mov     ecx, DWORD PTR _value$[esp+8]
ALTERNATE_ENTRY _RhpLockCmpXchg64AVLocation
        lock cmpxchg8b QWORD PTR [esi]
        pop     esi
        pop     ebx
        ret     16

FASTCALL_ENDFUNC

end
