; Copyright (c) .NET Foundation and contributors. All rights reserved.
; Licensed under the MIT license. See LICENSE file in the project root for full license information.


        .586
        .model  flat

        include callconv.inc

        option  casemap:none
        .code

EXTERN _start_runtime_and_get_target_address@4:PROC

AlignCfgProc
_start_runtime_thunk_stub@0 proc public
    ; Stack on entry:
    ;      top->   vtfixup thunk return address
    ;              Unmanaged caller return address

    ; The idea here is similar to the prepad of the MethodDesc, in that we're
    ; using the return address of the call in the stub as a pointer to the
    ; bootstrap_thunk struct.

    pop     eax                         ; bootstrap_thunk*
    
    push    ebp                         ; Set up EBP frame
    mov     ebp,esp
    
    push    ecx                         ; Save caller registers
    push    edx
    
    push    eax                         ; Push the struct arg
    call    _start_runtime_and_get_target_address@4
    
    pop     edx                         ; Restore the registers
    pop     ecx
    
    pop     ebp                         ; Tear down the EBP frame
    push    eax                         ; Instead of "jmp eax", do "push eax; ret"
    ret                                 ; This keeps the call-return count balanced
_start_runtime_thunk_stub@0 endp

end
