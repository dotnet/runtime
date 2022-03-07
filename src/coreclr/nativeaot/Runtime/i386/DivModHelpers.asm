;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.


        .586
        .model  flat
        option  casemap:none
        .code


include AsmMacros.inc

EXTERN RhExceptionHandling_ThrowClasslibOverflowException                              : PROC
EXTERN RhExceptionHandling_ThrowClasslibDivideByZeroException                          : PROC
EXTERN __alldiv : PROC
EXTERN __allrem : PROC
EXTERN __aulldiv : PROC
EXTERN __aullrem : PROC
EXTERN __aulldvrm : PROC
EXTERN __alldvrm : PROC

esp_offsetof_dividend_low     equ 4
esp_offsetof_dividend_high    equ 8
esp_offsetof_divisor_low      equ 12
esp_offsetof_divisor_high     equ 16

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpLDiv
;;
;; INPUT:  [ESP+4]: dividend low
;;         [ESP+8]: dividend high
;;         [ESP+12]: divisor low
;;         [ESP+16]: divisor high
;;
;; OUTPUT: EAX: result low
;;         EDX: result high
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
FASTCALL_FUNC  RhpLDiv, 16

        ;; pretest for the problematic cases of overflow and divide by zero
        ;; overflow: dividend = 0x80000000`00000000 and divisor = -1l = 0xffffffff`ffffffff
        ;; divide by zero: divisor = 0x00000000`00000000
        ;;
        ;; quick pretest - if the two halves of the divisor are unequal, we cannot
        ;; have one of the problematic cases
        mov     eax,[esp+esp_offsetof_divisor_low]
        cmp     eax,[esp+esp_offsetof_divisor_high]
        je      LDivDoMoreTests
LDivOkToDivide:
        ;; tailcall to the actual divide routine
        jmp     __alldiv
LDivDoMoreTests:
        ;; we know the high and low halves of the divisor are equal
        ;;
        ;; check for the divide by zero case
        test    eax,eax
        je      ThrowClasslibDivideByZeroException
        ;;
        ;; is the divisor == -1l? I.e., can we have the overflow case?
        cmp     eax,-1
        jne     LDivOkToDivide
        ;;
        ;; is the dividend == 0x80000000`00000000?
        cmp     dword ptr [esp+esp_offsetof_dividend_low],0
        jne     LDivOkToDivide
        cmp     dword ptr [esp+esp_offsetof_dividend_high],80000000h
        jne     LDivOkToDivide
FASTCALL_ENDFUNC

        ;; make it look like the managed code called this directly
        ;; by popping the parameters and putting the return address in the proper place
ThrowClasslibOverflowException proc
        pop     ecx
        add     esp,16
        push    ecx
        ;; passing return address in ecx
        jmp     RhExceptionHandling_ThrowClasslibOverflowException
ThrowClasslibOverflowException endp

ThrowClasslibDivideByZeroException proc
        pop     ecx
        add     esp,16
        push    ecx
        ;; passing return address in ecx
        jmp     RhExceptionHandling_ThrowClasslibDivideByZeroException
ThrowClasslibDivideByZeroException endp

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpLMod
;;
;; INPUT:  [ESP+4]: dividend low
;;         [ESP+8]: dividend high
;;         [ESP+12]: divisor low
;;         [ESP+16]: divisor high
;;
;; OUTPUT: EAX: result low
;;         EDX: result high
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
FASTCALL_FUNC  RhpLMod, 16

        ;; pretest for the problematic cases of overflow and divide by zero
        ;; overflow: dividend = 0x80000000`00000000 and divisor = -1l = 0xffffffff`ffffffff
        ;; divide by zero: divisor = 0x00000000`00000000
        ;;
        ;; quick pretest - if the two halves of the divisor are unequal, we cannot
        ;; have one of the problematic cases
        mov     eax,[esp+esp_offsetof_divisor_low]
        cmp     eax,[esp+esp_offsetof_divisor_high]
        je      LModDoMoreTests
LModOkToDivide:
        jmp     __allrem
LModDoMoreTests:
        ;; we know the high and low halves of the divisor are equal
        ;;
        ;; check for the divide by zero case
        test    eax,eax
        je      ThrowClasslibDivideByZeroException
        ;;
        ;; is the divisor == -1l? I.e., can we have the overflow case?
        cmp     eax,-1
        jne     LModOkToDivide
        ;;
        ;; is the dividend == 0x80000000`00000000?
        cmp     dword ptr [esp+esp_offsetof_dividend_low],0
        jne     LModOkToDivide
        cmp     dword ptr [esp+esp_offsetof_dividend_high],80000000h
        jne     LModOkToDivide
        jmp     ThrowClasslibOverflowException

FASTCALL_ENDFUNC

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpLDivMod
;;
;; INPUT:  [ESP+4]: dividend low
;;         [ESP+8]: dividend high
;;         [ESP+12]: divisor low
;;         [ESP+16]: divisor high
;;
;; OUTPUT: EAX: quotient low
;;         EDX: quotient high
;;         ECX: remainder high
;;         EBX: remainder high
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
FASTCALL_FUNC  RhpLDivMod, 16

        ;; pretest for the problematic cases of overflow and divide by zero
        ;; overflow: dividend = 0x80000000`00000000 and divisor = -1l = 0xffffffff`ffffffff
        ;; divide by zero: divisor = 0x00000000`00000000
        ;;
        ;; quick pretest - if the two halves of the divisor are unequal, we cannot
        ;; have one of the problematic cases
        mov     eax,[esp+esp_offsetof_divisor_low]
        cmp     eax,[esp+esp_offsetof_divisor_high]
        je      LDivModDoMoreTests
LDivModOkToDivide:
        jmp     __alldvrm
LDivModDoMoreTests:
        ;; we know the high and low halves of the divisor are equal
        ;;
        ;; check for the divide by zero case
        test    eax,eax
        je      ThrowClasslibDivideByZeroException
        ;;
        ;; is the divisor == -1l? I.e., can we have the overflow case?
        cmp     eax,-1
        jne     LDivModOkToDivide
        ;;
        ;; is the dividend == 0x80000000`00000000?
        cmp     dword ptr [esp+esp_offsetof_dividend_low],0
        jne     LDivModOkToDivide
        cmp     dword ptr [esp+esp_offsetof_dividend_high],80000000h
        jne     LDivModOkToDivide
        jmp     ThrowClasslibOverflowException

FASTCALL_ENDFUNC

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpULDiv
;;
;; INPUT:  [ESP+4]: dividend low
;;         [ESP+8]: dividend high
;;         [ESP+12]: divisor low
;;         [ESP+16]: divisor high
;;
;; OUTPUT: EAX: result low
;;         EDX: result high
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
FASTCALL_FUNC  RhpULDiv, 16

        ;; pretest for divide by zero
        mov     eax,[esp+esp_offsetof_divisor_low]
        or      eax,[esp+esp_offsetof_divisor_high]
        jne     __aulldiv
        jmp     ThrowClasslibDivideByZeroException

FASTCALL_ENDFUNC

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpULMod
;;
;; INPUT:  [ESP+4]: dividend low
;;         [ESP+8]: dividend high
;;         [ESP+12]: divisor low
;;         [ESP+16]: divisor high
;;
;; OUTPUT: EAX: result low
;;         EDX: result high
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
FASTCALL_FUNC  RhpULMod, 16

        ;; pretest for divide by zero
        mov     eax,[esp+esp_offsetof_divisor_low]
        or      eax,[esp+esp_offsetof_divisor_high]
        jne     __aullrem
        jmp     ThrowClasslibDivideByZeroException

FASTCALL_ENDFUNC

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpULDivMod
;;
;; INPUT:  [ESP+4]: dividend low
;;         [ESP+8]: dividend high
;;         [ESP+12]: divisor low
;;         [ESP+16]: divisor high
;;
;; OUTPUT: EAX: quotient low
;;         EDX: quotient high
;;         ECX: remainder high
;;         EBX: remainder high
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
FASTCALL_FUNC  RhpULDivMod, 16

        ;; pretest for divide by zero
        mov     eax,[esp+esp_offsetof_divisor_low]
        or      eax,[esp+esp_offsetof_divisor_high]
        jne     __aulldvrm
        jmp     ThrowClasslibDivideByZeroException

FASTCALL_ENDFUNC


        end
