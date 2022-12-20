;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

include asmmacros.inc

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpGetThread
;;
;;
;; INPUT:
;;
;; OUTPUT: RAX: Thread pointer
;;
;; TRASHES: R10
;;
;; MUST PRESERVE ARGUMENT REGISTERS
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
LEAF_ENTRY RhpGetThread, _TEXT
        ;; rax = GetThread(), TRASHES r10
        INLINE_GETTHREAD rax, r10
        ret
LEAF_END RhpGetThread, _TEXT


        end
