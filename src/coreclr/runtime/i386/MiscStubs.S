// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "AsmMacros_Shared.h"

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
#define PROBE_STEP 0x1000
NESTED_ENTRY RhpStackProbe, _TEXT, NoHandler
    // On entry:
    //   eax - the lowest address of the stack frame being allocated (i.e. [InitialSp - FrameSize])
    //
    // NOTE: this helper will probe at least one page below the one pointed by esp.
    PROLOG_BEG
    PROLOG_END

    and     esp, -PROBE_STEP        // esp points to the **lowest address** on the last probed page
                                   // This is done to make the loop end condition simpler.

LOCAL_LABEL(ProbeLoop):
    sub     esp, PROBE_STEP         // esp points to the lowest address of the **next page** to probe
    test    [esp], eax             // esp points to the lowest address on the **last probed** page
    cmp     esp, eax
    jg      LOCAL_LABEL(ProbeLoop) // if esp > eax, then we need to probe at least one more page.

    EPILOG_BEG
    mov     esp, ebp
    EPILOG_END
    ret

NESTED_END RhpStackProbe, _TEXT

// *********************************************************************/
// LLsh - long shift left
//
// Purpose:
//    Does a Long Shift Left (signed and unsigned are identical)
//    Shifts a long left any number of bits.
//
// Entry:
//    EDX:EAX - long value to be shifted
//        ECX - number of bits to shift by
//
// Exit:
//    EDX:EAX - shifted value
//
.align 16
LEAF_ENTRY RhpLLsh, _TEXT
    // Reduce shift amount mod 64
    and     ecx, 63

    cmp     ecx, 32
    jae     LOCAL_LABEL(LLshMORE32)

    // Handle shifts of between bits 0 and 31
    shld    edx, eax, cl
    shl     eax, cl
    ret

LOCAL_LABEL(LLshMORE32):
    // Handle shifts of between bits 32 and 63
    // The x86 shift instructions only use the lower 5 bits.
    mov     edx, eax
    xor     eax, eax
    shl     edx, cl
    ret
LEAF_END RhpLLsh, _TEXT

// *********************************************************************/
// LRsh - long shift right
//
// Purpose:
//    Does a signed Long Shift Right
//    Shifts a long right any number of bits.
//
// Entry:
//    EDX:EAX - long value to be shifted
//        ECX - number of bits to shift by
//
// Exit:
//    EDX:EAX - shifted value
//
.align 16
LEAF_ENTRY RhpLRsh, _TEXT
    // Reduce shift amount mod 64
    and     ecx, 63

    cmp     ecx, 32
    jae     LOCAL_LABEL(LRshMORE32)

    // Handle shifts of between bits 0 and 31
    shrd    eax, edx, cl
    sar     edx, cl
    ret

LOCAL_LABEL(LRshMORE32):
    // Handle shifts of between bits 32 and 63
    // The x86 shift instructions only use the lower 5 bits.
    mov     eax, edx
    sar     edx, 31
    sar     eax, cl
    ret
LEAF_END RhpLRsh, _TEXT

// *********************************************************************/
// LRsz - long shift right zero-extended
//
// Purpose:
//    Does a unsigned Long Shift Right
//    Shifts a long right any number of bits.
//
// Entry:
//    EDX:EAX - long value to be shifted
//        ECX - number of bits to shift by
//
// Exit:
//    EDX:EAX - shifted value
//
.align 16
LEAF_ENTRY RhpLRsz, _TEXT
    // Reduce shift amount mod 64
    and     ecx, 63

    cmp     ecx, 32
    jae     LOCAL_LABEL(LRszMORE32)

    // Handle shifts of between bits 0 and 31
    shrd    eax, edx, cl
    shr     edx, cl
    ret

LOCAL_LABEL(LRszMORE32):
    // Handle shifts of between bits 32 and 63
    // The x86 shift instructions only use the lower 5 bits.
    mov     eax, edx
    xor     edx, edx
    shr     eax, cl
    ret
LEAF_END RhpLRsz, _TEXT
