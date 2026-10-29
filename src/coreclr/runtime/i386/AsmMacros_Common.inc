; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

;
; Windows x86 assembler macros shared between CoreCLR and NativeAOT.
;

ALTERNATE_ENTRY macro Name

Name label proc
PUBLIC Name
        endm
