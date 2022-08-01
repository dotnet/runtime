// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <unixasmmacros.inc>
#include "AsmOffsets.inc"

    .global memcpy
    .global memcpyGCRefs
    .global memcpyGCRefsWithWriteBarrier
    .global memcpyAnyWithWriteBarrier

//;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
//
// void* RhpCopyMultibyteNoGCRefs(void*, void*, size_t)
//
// The purpose of this wrapper is to hoist the potential null reference exceptions of copying memory up to a place where
// the stack unwinder and exception dispatch can properly transform the exception into a managed exception and dispatch
// it to managed code.
//

    LEAF_ENTRY    RhpCopyMultibyteNoGCRefs, _TEXT

        // x0    dest
        // x1    src
        // x2    count

        cbz     x2, NothingToCopy_NoGCRefs  // check for a zero-length copy

        // Now check the dest and src pointers.  If they AV, the EH subsystem will recognize the address of the AV,
        // unwind the frame, and fixup the stack to make it look like the (managed) caller AV'ed, which will be
        // translated to a managed exception as usual.
    ALTERNATE_ENTRY RhpCopyMultibyteNoGCRefsDestAVLocation
        ldrb    wzr, [x0]
    ALTERNATE_ENTRY RhpCopyMultibyteNoGCRefsSrcAVLocation
        ldrb    wzr, [x1]

        // tail-call to plain-old-memcpy
        b       memcpy

NothingToCopy_NoGCRefs:
        // dest is already in x0
        ret

    LEAF_END RhpCopyMultibyteNoGCRefs, _TEXT


//;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
//
// void* RhpCopyMultibyte(void*, void*, size_t)
//
// The purpose of this wrapper is to hoist the potential null reference exceptions of copying memory up to a place where
// the stack unwinder and exception dispatch can properly transform the exception into a managed exception and dispatch
// it to managed code.
//

    LEAF_ENTRY    RhpCopyMultibyte, _TEXT

        // x0    dest
        // x1    src
        // x2    count

        // check for a zero-length copy
        cbz     x2, NothingToCopy_RhpCopyMultibyte

        // Now check the dest and src pointers.  If they AV, the EH subsystem will recognize the address of the AV,
        // unwind the frame, and fixup the stack to make it look like the (managed) caller AV'ed, which will be
        // translated to a managed exception as usual.
    ALTERNATE_ENTRY RhpCopyMultibyteDestAVLocation
        ldrb    wzr, [x0]
    ALTERNATE_ENTRY RhpCopyMultibyteSrcAVLocation
        ldrb    wzr, [x1]

        // tail-call to the GC-safe memcpy implementation
        b       memcpyGCRefs

NothingToCopy_RhpCopyMultibyte:
        // dest is already still in x0
        ret

    LEAF_END RhpCopyMultibyte, _TEXT

//;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
//
// void* RhpCopyMultibyteWithWriteBarrier(void*, void*, size_t)
//
// The purpose of this wrapper is to hoist the potential null reference exceptions of copying memory up to a place where
// the stack unwinder and exception dispatch can properly transform the exception into a managed exception and dispatch
// it to managed code.
// Runs a card table update via RhpBulkWriteBarrier after the copy
//

    LEAF_ENTRY    RhpCopyMultibyteWithWriteBarrier, _TEXT

        // x0    dest
        // x1    src
        // x2    count

        // check for a zero-length copy
        cbz     x2, NothingToCopy_RhpCopyMultibyteWithWriteBarrier

        // Now check the dest and src pointers.  If they AV, the EH subsystem will recognize the address of the AV,
        // unwind the frame, and fixup the stack to make it look like the (managed) caller AV'ed, which will be
        // translated to a managed exception as usual.
    ALTERNATE_ENTRY RhpCopyMultibyteWithWriteBarrierDestAVLocation
        ldrb    wzr, [x0]
    ALTERNATE_ENTRY RhpCopyMultibyteWithWriteBarrierSrcAVLocation
        ldrb    wzr, [x1]

        // tail-call to the GC-safe memcpy implementation
        b       memcpyGCRefsWithWriteBarrier

NothingToCopy_RhpCopyMultibyteWithWriteBarrier:
        // dest is already still in x0
        ret
    LEAF_END RhpCopyMultibyteWithWriteBarrier, _TEXT

//;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
//
// void* RhpCopyAnyWithWriteBarrier(void*, void*, size_t)
//
// The purpose of this wrapper is to hoist the potential null reference exceptions of copying memory up to a place where
// the stack unwinder and exception dispatch can properly transform the exception into a managed exception and dispatch
// it to managed code.
// Runs a card table update via RhpBulkWriteBarrier after the copy if it contained GC pointers
//

    LEAF_ENTRY    RhpCopyAnyWithWriteBarrier, _TEXT

        // x0    dest
        // x1    src
        // x2    count

        // check for a zero-length copy
        cbz     x2, NothingToCopy_RhpCopyAnyWithWriteBarrier

        // Now check the dest and src pointers.  If they AV, the EH subsystem will recognize the address of the AV,
        // unwind the frame, and fixup the stack to make it look like the (managed) caller AV'ed, which will be
        // translated to a managed exception as usual.
    ALTERNATE_ENTRY RhpCopyAnyWithWriteBarrierDestAVLocation
        ldrb    wzr, [x0]
    ALTERNATE_ENTRY RhpCopyAnyWithWriteBarrierSrcAVLocation
        ldrb    wzr, [x1]

        // tail-call to the GC-safe memcpy implementation
        b       memcpyAnyWithWriteBarrier

NothingToCopy_RhpCopyAnyWithWriteBarrier:
        // dest is already still in x0
        ret

    LEAF_END RhpCopyAnyWithWriteBarrier, _TEXT
