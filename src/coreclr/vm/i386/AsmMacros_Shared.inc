; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

; This file is used to allow sharing of assembly code between NativeAOT and CoreCLR, which have different conventions about how to ensure that constants offsets are accessible

include asmconstants.inc
include asmmacros.inc

ifndef GC_WRITE_BARRIER_STANDALONE
RhpGcAlloc TEXTEQU <_RhpGcAlloc@16>

EXTERN RhpGcAlloc : PROC
EXTERN RhExceptionHandling_FailedAllocation : PROC
endif
