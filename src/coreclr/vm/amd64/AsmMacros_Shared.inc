; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

; This file is used to allow sharing of assembly code between NativeAOT and CoreCLR, which have different conventions about how to ensure that constants offsets are accessible

include AsmConstants.inc
include AsmMacros.inc

EXTERN g_lowest_address     : QWORD
EXTERN g_highest_address    : QWORD
EXTERN g_ephemeral_low      : QWORD
EXTERN g_ephemeral_high     : QWORD
EXTERN g_card_table         : QWORD

ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN g_card_bundle_table  : QWORD
endif

ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
EXTERN g_write_watch_table  : QWORD
endif

EXTERN RhpGcAlloc : PROC
EXTERN RhExceptionHandling_FailedAllocation : PROC
