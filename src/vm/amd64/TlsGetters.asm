; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.
; See the LICENSE file in the project root for more information.

; ==++==
;

;
; ==--==
; ***********************************************************************
; File: TlsGetters.asm, see history in jithelp.asm
;
; Notes: These TlsGetters (GetAppDomain(), GetThread()) are implemented
;        in a generic fashion, but might be patched at runtime to contain
;        a much faster implementation which goes straight to the TLS for
;        the Thread* or AppDomain*. 
;
;        Note that the macro takes special care to not have these become 
;        non-unwindable after the patching has overwritten the prologue of 
;        the generic getter.
; ***********************************************************************

include AsmMacros.inc
include asmconstants.inc

; Min amount of stack space that a nested function should allocate.
MIN_SIZE equ 28h


; These generic TLS getters are used for GetThread() and GetAppDomain(), they do a little
; extra work to ensure that certain registers are preserved, those include the following 
; volatile registers
; 
;       rcx
;       rdx
;       r8
;       r9
;       r10
;       r11
;       
; The return value is in rax as usual
;
; They DO NOT save scratch flowing point registers, if you need those you need to save them.

ifdef ENABLE_GET_THREAD_GENERIC_FULL_CHECK
GetThreadGenericFullCheck equ ?GetThreadGenericFullCheck@@YAPEAVThread@@XZ
extern GetThreadGenericFullCheck:proc
endif ; ENABLE_GET_THREAD_GENERIC_FULL_CHECK

; Creates a generic TLS getter using the value from TLS slot gTLSIndex.  Set GenerateGetThread
; when using this macro to generate GetThread, as that will cause special code to be generated which
; enables additional debug-only checking, such as enforcement of EE_THREAD_NOT_REQUIRED contracts
GenerateOptimizedTLSGetter macro name, GenerateGetThread

extern g&name&TLSIndex:dword
extern __imp_TlsGetValue:qword

SIZEOF_PUSHED_ARGS equ 10h

NESTED_ENTRY Get&name&Generic, _TEXT
        push_vol_reg            r10
        push_vol_reg            r11
        alloc_stack             MIN_SIZE

        ; save argument registers in shadow space
        save_reg_postrsp        rcx, MIN_SIZE + 8h  + SIZEOF_PUSHED_ARGS
        save_reg_postrsp        rdx, MIN_SIZE + 10h + SIZEOF_PUSHED_ARGS
        save_reg_postrsp        r8,  MIN_SIZE + 18h + SIZEOF_PUSHED_ARGS
        save_reg_postrsp        r9,  MIN_SIZE + 20h + SIZEOF_PUSHED_ARGS
    END_PROLOGUE

ifdef _DEBUG
        cmp     dword ptr [g&name&TLSIndex], -1
        jnz     @F
        int     3
@@:
endif ; _DEBUG

CALL_GET_THREAD_GENERIC_FULL_CHECK=0

ifdef ENABLE_GET_THREAD_GENERIC_FULL_CHECK
if GenerateGetThread

; Generating the GetThread() tlsgetter, and GetThreadGenericFullCheck is
; defined in C (in threads.cpp).  So we'll want to delegate directly to
; GetThreadGenericFullCheck, which may choose to do additional checking, such
; as enforcing EE_THREAD_NOT_REQUIRED contracts
CALL_GET_THREAD_GENERIC_FULL_CHECK=1

endif   ; GenerateGetThread
endif   ; ENABLE_GET_THREAD_GENERIC_FULL_CHECK

if CALL_GET_THREAD_GENERIC_FULL_CHECK
        call    GetThreadGenericFullCheck
else
        ; Not generating the GetThread() tlsgetter (or there is no GetThreadGenericFullCheck
        ; to call), so do nothing special--just look up the value stored at TLS slot gTLSIndex
        mov     ecx, [g&name&TLSIndex]
        call    [__imp_TlsGetValue]
endif

        ; restore arguments from shadow space
        mov     rcx, [rsp + MIN_SIZE + 8h  + SIZEOF_PUSHED_ARGS]
        mov     rdx, [rsp + MIN_SIZE + 10h + SIZEOF_PUSHED_ARGS]
        mov     r8,  [rsp + MIN_SIZE + 18h + SIZEOF_PUSHED_ARGS]
        mov     r9,  [rsp + MIN_SIZE + 20h + SIZEOF_PUSHED_ARGS]

        ; epilog
        add     rsp, MIN_SIZE
        pop     r11
        pop     r10
        ret
NESTED_END Get&name&Generic, _TEXT

        endm    

GenerateOptimizedTLSGetter Thread, 1
GenerateOptimizedTLSGetter AppDomain, 0

        end
