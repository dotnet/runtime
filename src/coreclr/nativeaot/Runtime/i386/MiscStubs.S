// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include <unixasmmacros.inc>

// *********************************************************************/
// RhpStackProbe
//
// Purpose:
//   the helper will access ("probe") a word on each page of the stack
//   starting with the page right beneath esp down to the one pointed to by eax.
//   The procedure is needed to make sure that the "guard" page is pushed down below the allocated stack frame.
//   The call to the helper will be emitted by JIT in the function prolog when large (larger than 0x3000 bytes) stack frame is required.
//
// NOTE: this helper will modify a value of esp and must establish the frame pointer.
// NOTE: On Linux we must advance the stack pointer as we probe - it is not allowed to access 65535 bytes below esp.
//
#define PAGE_SIZE 0x1000
NESTED_ENTRY RhpStackProbe, _TEXT, NoHandler
    // On entry:
    //   eax - the lowest address of the stack frame being allocated (i.e. [InitialSp - FrameSize])
    //
    // NOTE: this helper will probe at least one page below the one pointed by esp.
    PROLOG_BEG
    PROLOG_END

    and     esp, -PAGE_SIZE        // esp points to the **lowest address** on the last probed page
                                   // This is done to make the loop end condition simpler.

LOCAL_LABEL(ProbeLoop):
    sub     esp, PAGE_SIZE         // esp points to the lowest address of the **next page** to probe
    test    [esp], eax             // esp points to the lowest address on the **last probed** page
    cmp     esp, eax
    jg      LOCAL_LABEL(ProbeLoop) // if esp > eax, then we need to probe at least one more page.

    EPILOG_BEG
    mov     esp, ebp
    EPILOG_END
    ret

NESTED_END RhpStackProbe, _TEXT
