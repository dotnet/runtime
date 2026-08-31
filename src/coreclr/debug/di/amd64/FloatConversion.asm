; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

;// @dbgtodo Microsoft inspection: remove the implementation from vm\amd64\getstate.asm when we remove the
;// ipc event to load the float state.

;// this is the same implementation as the function of the same name in vm\amd64\getstate.asm and they must
;// remain in sync.
;// Arguments
;//     input: (in rcx) the M128 value to be converted to a double
;//     output: the double corresponding to the M128 input value

_TEXT segment para 'CODE'
FPFillR8    proc
            movdqa  xmm0, [rcx]
            ret
FPFillR8    endp

END
