; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.
; See the LICENSE file in the project root for more information.

.586
.model flat

include asmmacros.inc
include asmconstants.inc

option casemap:none
.code

EXTERN _OnCallCountThresholdReached@8:proc

_OnCallCountThresholdReachedStub@0 proc public
    ; Pop the return address (the stub-identifying token) into a non-argument volatile register that can be trashed
    pop     eax
    jmp     _OnCallCountThresholdReachedStub2@0
_OnCallCountThresholdReachedStub@0 endp

_OnCallCountThresholdReachedStub2@0 proc public
    STUB_PROLOG

    mov     esi, esp

    push    eax ; stub-identifying token, see OnCallCountThresholdReachedStub
    push    esi ; TransitionBlock *
    call    _OnCallCountThresholdReached@8

    STUB_EPILOG
    jmp     eax

    ; This will never be executed. It is just to help out stack-walking logic
    ; which disassembles the epilog to unwind the stack.
    ret
_OnCallCountThresholdReachedStub2@0 endp

end
