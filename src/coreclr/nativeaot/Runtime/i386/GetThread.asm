;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

        .586
        .model  flat
        option  casemap:none
        .code


include AsmMacros.inc

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpGetThread
;;
;;
;; INPUT: none
;;
;; OUTPUT: EAX: Thread pointer
;;
;; MUST PRESERVE ARGUMENT REGISTERS
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
FASTCALL_FUNC RhpGetThread, 0
        push    ecx
        INLINE_GETTHREAD eax, ecx ; eax dest, ecx trash
        pop     ecx
        ret
FASTCALL_ENDFUNC

        end
