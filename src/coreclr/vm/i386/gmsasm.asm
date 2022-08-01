; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

;
;  *** NOTE:  If you make changes to this file, propagate the changes to
;             gmsasm.s in this directory

	.586
	.model	flat

include asmconstants.inc

	option	casemap:none
	.code

; int __fastcall LazyMachStateCaptureState(struct LazyMachState *pState);
@LazyMachStateCaptureState@4 proc public
        mov [ecx+MachState__pRetAddr], 0 ; marks that this is not yet valid
        mov [ecx+MachState__edi], edi    ; remember register values
	mov [ecx+MachState__esi], esi
        mov [ecx+MachState__ebx], ebx
	mov [ecx+LazyMachState_captureEbp], ebp
	mov [ecx+LazyMachState_captureEsp], esp

        mov eax, [esp]                   ; capture return address
	mov [ecx+LazyMachState_captureEip], eax
	xor eax, eax
	retn
@LazyMachStateCaptureState@4 endp

end
