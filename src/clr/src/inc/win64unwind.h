// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _WIN64UNWIND_H_
#define _WIN64UNWIND_H_

//
// Define AMD64 exception handling structures and function prototypes.
//
// Define unwind operation codes.
//

typedef enum _UNWIND_OP_CODES {
    UWOP_PUSH_NONVOL = 0,
    UWOP_ALLOC_LARGE,
    UWOP_ALLOC_SMALL,
    UWOP_SET_FPREG,
    UWOP_SAVE_NONVOL,
    UWOP_SAVE_NONVOL_FAR,
    UWOP_EPILOG,
    UWOP_SPARE_CODE,
    UWOP_SAVE_XMM128,
    UWOP_SAVE_XMM128_FAR,
    UWOP_PUSH_MACHFRAME,

#ifdef PLATFORM_UNIX
    // UWOP_SET_FPREG_LARGE is a CLR Unix-only extension to the Windows AMD64 unwind codes.
    // It is not part of the standard Windows AMD64 unwind codes specification.
    // UWOP_SET_FPREG allows for a maximum of a 240 byte offset between RSP and the
    // frame pointer, when the frame pointer is established. UWOP_SET_FPREG_LARGE
    // has a 32-bit range scaled by 16. When UWOP_SET_FPREG_LARGE is used,
    // UNWIND_INFO.FrameRegister must be set to the frame pointer register, and
    // UNWIND_INFO.FrameOffset must be set to 15 (its maximum value). UWOP_SET_FPREG_LARGE
    // is followed by two UNWIND_CODEs that are combined to form a 32-bit offset (the same
    // as UWOP_SAVE_NONVOL_FAR). This offset is then scaled by 16. The result must be less
    // than 2^32 (that is, the top 4 bits of the unscaled 32-bit number must be zero). This
    // result is used as the frame pointer register offset from RSP at the time the frame pointer
    // is established. Either UWOP_SET_FPREG or UWOP_SET_FPREG_LARGE can be used, but not both.

    UWOP_SET_FPREG_LARGE,
#endif // PLATFORM_UNIX
} UNWIND_OP_CODES, *PUNWIND_OP_CODES;

static const UCHAR UnwindOpExtraSlotTable[] = {
    0,          // UWOP_PUSH_NONVOL
    1,          // UWOP_ALLOC_LARGE (or 3, special cased in lookup code)
    0,          // UWOP_ALLOC_SMALL
    0,          // UWOP_SET_FPREG
    1,          // UWOP_SAVE_NONVOL
    2,          // UWOP_SAVE_NONVOL_FAR
    1,          // UWOP_EPILOG
    2,          // UWOP_SPARE_CODE      // previously 64-bit UWOP_SAVE_XMM_FAR
    1,          // UWOP_SAVE_XMM128
    2,          // UWOP_SAVE_XMM128_FAR
    0,          // UWOP_PUSH_MACHFRAME

#ifdef PLATFORM_UNIX
    2,          // UWOP_SET_FPREG_LARGE
#endif // PLATFORM_UNIX
};

//
// Define unwind code structure.
//

typedef union _UNWIND_CODE {
    struct {
        UCHAR CodeOffset;
        UCHAR UnwindOp : 4;
        UCHAR OpInfo : 4;
    };

    struct {
        UCHAR OffsetLow;
        UCHAR UnwindOp : 4;
        UCHAR OffsetHigh : 4;
    } EpilogueCode;

    USHORT FrameOffset;
} UNWIND_CODE, *PUNWIND_CODE;

//
// Define unwind information flags.
//

#define UNW_FLAG_NHANDLER 0x0
#define UNW_FLAG_EHANDLER 0x1
#define UNW_FLAG_UHANDLER 0x2
#define UNW_FLAG_CHAININFO 0x4

#ifdef _TARGET_X86_

typedef struct _UNWIND_INFO {
    ULONG FunctionLength;
} UNWIND_INFO, *PUNWIND_INFO;

#else // _TARGET_X86_

typedef struct _UNWIND_INFO {
    UCHAR Version : 3;
    UCHAR Flags : 5;
    UCHAR SizeOfProlog;
    UCHAR CountOfUnwindCodes;
    UCHAR FrameRegister : 4;
    UCHAR FrameOffset : 4;
    UNWIND_CODE UnwindCode[1];

//
// The unwind codes are followed by an optional DWORD aligned field that
// contains the exception handler address or the address of chained unwind
// information. If an exception handler address is specified, then it is
// followed by the language specified exception handler data.
//
//  union {
//      ULONG ExceptionHandler;
//      ULONG FunctionEntry;
//  };
//
//  ULONG ExceptionData[];
//

} UNWIND_INFO, *PUNWIND_INFO;

#endif // _TARGET_X86_
#endif // _WIN64UNWIND_H_
