; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

; This file is used to allow sharing of assembly code between NativeAOT and CoreCLR, which have different conventions about how to ensure that constants offsets are accessible

include asmconstants.inc
include asmmacros.inc

G_LOWEST_ADDRESS                            equ _g_lowest_address
G_HIGHEST_ADDRESS                           equ _g_highest_address
G_EPHEMERAL_LOW                             equ _g_ephemeral_low
G_EPHEMERAL_HIGH                            equ _g_ephemeral_high
G_CARD_TABLE                                equ _g_card_table

EXTERN G_LOWEST_ADDRESS : DWORD
EXTERN G_HIGHEST_ADDRESS : DWORD
EXTERN G_EPHEMERAL_LOW : DWORD
EXTERN G_EPHEMERAL_HIGH : DWORD
EXTERN G_CARD_TABLE : DWORD

RhpGcAlloc TEXTEQU <_RhpGcAlloc@16>

EXTERN RhpGcAlloc : PROC
EXTERN RhExceptionHandling_FailedAllocation : PROC
