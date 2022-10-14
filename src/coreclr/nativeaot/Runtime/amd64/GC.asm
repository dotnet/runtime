;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

include AsmMacros.inc

;; extern "C" DWORD __stdcall xmmYmmStateSupport();
LEAF_ENTRY xmmYmmStateSupport, _TEXT
        mov     ecx, 0                  ; Specify xcr0
        xgetbv                          ; result in EDX:EAX
        and eax, 06H
        cmp eax, 06H                    ; check OS has enabled both XMM and YMM state support
        jne     not_supported
        mov     eax, 1
        jmp     done
    not_supported:
        mov     eax, 0
    done:
        ret
LEAF_END xmmYmmStateSupport, _TEXT

;; extern "C" DWORD __stdcall avx512StateSupport();
LEAF_ENTRY avx512StateSupport, _TEXT
        mov     ecx, 0                  ; Specify xcr0
        xgetbv                          ; result in EDX:EAX
        and eax, 0E6H
        cmp eax, 0E6H                    ; check OS has enabled XMM, YMM and ZMM state support
        jne     not_supported
        mov     eax, 1
        jmp     done
    not_supported:
        mov     eax, 0
    done:
        ret
LEAF_END avx512StateSupport, _TEXT

        end
