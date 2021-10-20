// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Implementation of _CONTEXT_CaptureContext for the IBM s390x platform.
// This function is processor dependent.  It is used by exception handling,
// and is always apply to the current thread.
//

#include "unixasmmacros.inc"
#include "asmconstants.h"

// Incoming:
//  R2: Context*
//
LEAF_ENTRY CONTEXT_CaptureContext, _TEXT

    tm      CONTEXT_ContextFlags+3(%r2), CONTEXT_INTEGER
    je      LOCAL_LABEL(Done_CONTEXT_INTEGER)
    stmg    %r0, %r14, CONTEXT_R0(%r2)
LOCAL_LABEL(Done_CONTEXT_INTEGER):

    tm      CONTEXT_ContextFlags+3(%r2), CONTEXT_CONTROL
    je      LOCAL_LABEL(Done_CONTEXT_CONTROL)
    // Set PSW address to return address from %r14
    stg     %r14, CONTEXT_PSWAddr(%r2)
    // Extract PSW mask (CC is already changed; we ignore this here)
    epsw    %r0, %r1
    stm     %r0, %r1, CONTEXT_PSWMask(%r2)
    // Stack pointer is still unchanged
    stg     %r15, CONTEXT_R15(%r2)
LOCAL_LABEL(Done_CONTEXT_CONTROL):

    tm      CONTEXT_ContextFlags+3(%r2), CONTEXT_FLOATING_POINT
    je      LOCAL_LABEL(Done_CONTEXT_FLOATING_POINT)
    std     %f0, CONTEXT_F0(%r2)
    std     %f1, CONTEXT_F1(%r2)
    std     %f2, CONTEXT_F2(%r2)
    std     %f3, CONTEXT_F3(%r2)
    std     %f4, CONTEXT_F4(%r2)
    std     %f5, CONTEXT_F5(%r2)
    std     %f6, CONTEXT_F6(%r2)
    std     %f7, CONTEXT_F7(%r2)
    std     %f8, CONTEXT_F8(%r2)
    std     %f9, CONTEXT_F9(%r2)
    std     %f10, CONTEXT_F10(%r2)
    std     %f11, CONTEXT_F11(%r2)
    std     %f12, CONTEXT_F12(%r2)
    std     %f13, CONTEXT_F13(%r2)
    std     %f14, CONTEXT_F14(%r2)
    std     %f15, CONTEXT_F15(%r2)
LOCAL_LABEL(Done_CONTEXT_FLOATING_POINT):

    br %r14
LEAF_END CONTEXT_CaptureContext, _TEXT

LEAF_ENTRY RtlCaptureContext, _TEXT
    mvhhi    CONTEXT_ContextFlags+2(%r2), ((CONTEXT_S390X | CONTEXT_FULL) & 0xffff)
    mvhhi    CONTEXT_ContextFlags(%r2), ((CONTEXT_S390X | CONTEXT_FULL) >> 16)
    jg     C_FUNC(CONTEXT_CaptureContext)
LEAF_END RtlCaptureContext, _TEXT

LEAF_ENTRY RtlRestoreContext, _TEXT

    lgr     %r1, %r14

    tm      CONTEXT_ContextFlags+3(%r2), CONTEXT_FLOATING_POINT
    je      LOCAL_LABEL(Done_Restore_CONTEXT_FLOATING_POINT)
    ld      %f0, CONTEXT_F0(%r2)
    ld      %f1, CONTEXT_F1(%r2)
    ld      %f2, CONTEXT_F2(%r2)
    ld      %f3, CONTEXT_F3(%r2)
    ld      %f4, CONTEXT_F4(%r2)
    ld      %f5, CONTEXT_F5(%r2)
    ld      %f6, CONTEXT_F6(%r2)
    ld      %f7, CONTEXT_F7(%r2)
    ld      %f8, CONTEXT_F8(%r2)
    ld      %f9, CONTEXT_F9(%r2)
    ld      %f10, CONTEXT_F10(%r2)
    ld      %f11, CONTEXT_F11(%r2)
    ld      %f12, CONTEXT_F12(%r2)
    ld      %f13, CONTEXT_F13(%r2)
    ld      %f14, CONTEXT_F14(%r2)
    ld      %f15, CONTEXT_F15(%r2)
LOCAL_LABEL(Done_Restore_CONTEXT_FLOATING_POINT):

    tm      CONTEXT_ContextFlags+3(%r2), CONTEXT_CONTROL
    je      LOCAL_LABEL(Done_Restore_CONTEXT_CONTROL)
    // We do *not* attempt to restore the PSW mask here!
    lg      %r1, CONTEXT_PSWAddr(%r2)
    lg      %r15, CONTEXT_R15(%r2)
LOCAL_LABEL(Done_Restore_CONTEXT_CONTROL):

    tm      CONTEXT_ContextFlags+3(%r2), CONTEXT_INTEGER
    je      LOCAL_LABEL(Done_Restore_CONTEXT_INTEGER)
    // We do *not* restore %r0 and %r1 here!
    lmg     %r2, %r14, CONTEXT_R2(%r2)
LOCAL_LABEL(Done_Restore_CONTEXT_INTEGER):

    br %r1
LEAF_END RtlRestoreContext, _TEXT

