// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

#ifdef TARGET_UNIX
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
#endif // TARGET_UNIX
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

#ifdef TARGET_UNIX
    2,          // UWOP_SET_FPREG_LARGE
#endif // TARGET_UNIX
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
#define UNW_FLAG_LARGE 0x8 // Unwind Information V3 only

#ifdef TARGET_X86

typedef struct _UNWIND_INFO {
    ULONG FunctionLength;
} UNWIND_INFO, *PUNWIND_INFO;

#else // TARGET_X86

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

//
// Define AMD64 Unwind Information V3 structures.
//
// V3 is required for code using Intel APX: V1 cannot encode PUSH2/POP2 or registers R16-R31, and
// does not describe epilogs. Everything here is derived from the preview specification at
// https://learn.microsoft.com/en-us/cpp/build/x64-unwind-information-v3.
//
// The V1/V2 unwind information flags above keep their meanings; UNW_FLAG_LARGE is new in V3.
//

//
// Define winding operation descriptor (WOD) operation codes. The opcode is held in the low bits
// of the first byte of a WOD, and its width varies by operation. See
// https://learn.microsoft.com/en-us/cpp/build/x64-unwind-information-v3#opcode-dispatch-table.
//

typedef enum _WOD_OP_CODES {
    WOD_OP_SET_FPREG = 0,            // 8-bit opcode
    WOD_OP_ALLOC_HUGE = 1,           // 8-bit opcode
    WOD_OP_ALLOC_LARGE = 2,          // 8-bit opcode
    WOD_OP_PUSH_CANONICAL_FRAME = 3, // 8-bit opcode
    WOD_OP_PUSH = 4,                 // 3-bit opcode
    WOD_OP_SAVE_NONVOL_FAR = 5,      // 3-bit opcode
    WOD_OP_SAVE_NONVOL = 6,          // 3-bit opcode
    WOD_OP_PUSH_CONSECUTIVE_2 = 7,   // 3-bit opcode
    WOD_OP_ALLOC_SMALL = 8,          // 4-bit opcode
    WOD_OP_SAVE_XMM128_FAR = 9,      // 4-bit opcode
    WOD_OP_SAVE_XMM128 = 10,         // 4-bit opcode
    WOD_OP_PUSH2 = 32,               // 6-bit opcode
} WOD_OP_CODES, *PWOD_OP_CODES;

//
// Define winding operation descriptor structures. WODs are variable length, unaligned, and
// packed with no padding between them. The bitfields follow the bit-level layouts at
// https://learn.microsoft.com/en-us/cpp/build/x64-unwind-information-v3#wod-layouts-bit-level,
// allocated LSB first to match its bit numbering.
//
// In V1 every UNWIND_CODE (above) is a 2-byte slot and larger operations take extra slots. WODs
// are 1 to 5 bytes long and their opcode field is 3, 4, 6 or 8 bits wide, so there is no uniform
// slot: each WOD has its own structure.
//

#pragma pack(push, 1)

typedef struct _WOD_SET_FPREG {
    UCHAR OpCode;                   // WOD_OP_SET_FPREG
    UCHAR Register : 4;
    UCHAR Offset : 4;               // scaled by 16
} WOD_SET_FPREG, *PWOD_SET_FPREG;

typedef struct _WOD_ALLOC_HUGE {
    UCHAR OpCode;                   // WOD_OP_ALLOC_HUGE
    ULONG Size;                     // in bytes, unscaled
} WOD_ALLOC_HUGE, *PWOD_ALLOC_HUGE;

typedef struct _WOD_ALLOC_LARGE {
    UCHAR OpCode;                   // WOD_OP_ALLOC_LARGE
    USHORT Size;                    // scaled by 8
} WOD_ALLOC_LARGE, *PWOD_ALLOC_LARGE;

typedef struct _WOD_PUSH_CANONICAL_FRAME {
    UCHAR OpCode;                   // WOD_OP_PUSH_CANONICAL_FRAME
    UCHAR Type;                     // defined by the OS
} WOD_PUSH_CANONICAL_FRAME, *PWOD_PUSH_CANONICAL_FRAME;

typedef struct _WOD_PUSH {
    UCHAR OpCode : 3;               // WOD_OP_PUSH
    UCHAR Register : 5;
} WOD_PUSH, *PWOD_PUSH;

typedef struct _WOD_SAVE_NONVOL_FAR {
    UCHAR OpCode : 3;               // WOD_OP_SAVE_NONVOL_FAR
    UCHAR Register : 5;
    ULONG Displacement;             // in bytes, unscaled
} WOD_SAVE_NONVOL_FAR, *PWOD_SAVE_NONVOL_FAR;

typedef struct _WOD_SAVE_NONVOL {
    UCHAR OpCode : 3;               // WOD_OP_SAVE_NONVOL
    UCHAR Register : 5;
    USHORT Displacement;            // scaled by 8
} WOD_SAVE_NONVOL, *PWOD_SAVE_NONVOL;

//
// Pushes Register and then Register + 1, so Register must be in [0, 30].
//

typedef struct _WOD_PUSH_CONSECUTIVE_2 {
    UCHAR OpCode : 3;               // WOD_OP_PUSH_CONSECUTIVE_2
    UCHAR Register : 5;
} WOD_PUSH_CONSECUTIVE_2, *PWOD_PUSH_CONSECUTIVE_2;

typedef struct _WOD_ALLOC_SMALL {
    UCHAR OpCode : 4;               // WOD_OP_ALLOC_SMALL
    UCHAR Size : 4;                 // allocation is (Size + 1) * 8 bytes
} WOD_ALLOC_SMALL, *PWOD_ALLOC_SMALL;

typedef struct _WOD_SAVE_XMM128_FAR {
    UCHAR OpCode : 4;               // WOD_OP_SAVE_XMM128_FAR
    UCHAR Register : 4;
    ULONG Displacement;             // in bytes, unscaled
} WOD_SAVE_XMM128_FAR, *PWOD_SAVE_XMM128_FAR;

typedef struct _WOD_SAVE_XMM128 {
    UCHAR OpCode : 4;               // WOD_OP_SAVE_XMM128
    UCHAR Register : 4;
    USHORT Displacement;            // scaled by 16
} WOD_SAVE_XMM128, *PWOD_SAVE_XMM128;

//
// PUSH2 Register1, Register2 pushes Register1 first, so once it completes Register1 is at
// [rsp + 8] and Register2 at [rsp]. POP2 pops its first operand first, so "push2 a, b" pairs
// with "pop2 b, a", and a WOD describing a POP2 must swap its operands. Register1 straddles the
// two bytes.
//

typedef struct _WOD_PUSH2 {
    USHORT OpCode : 6;              // WOD_OP_PUSH2
    USHORT Register1 : 5;
    USHORT Register2 : 5;
} WOD_PUSH2, *PWOD_PUSH2;

//
// Define epilog descriptor structures.
//
// Each EPILOG_INFO_V3 is followed, when NumberOfOps != 0, by an EPILOG_INFO_EX_V3 (or an
// EPILOG_INFO_LARGE_EX_V3 if EPILOG_INFO_LARGE is set) and then by NumberOfOps IP offsets of
// one byte each (two if EPILOG_INFO_LARGE). A descriptor with NumberOfOps == 0 inherits
// NumberOfOps, FirstOp, IpOffsetOfLastInstruction and the IP offsets from the first preceding
// descriptor with NumberOfOps != 0.
//
// Epilog IP offsets are relative to the start of the epilog, in forward execution order: entry 0
// is the first epilog instruction with an unwind effect.
//

typedef struct _EPILOG_INFO_V3 {
    UCHAR Flags : 3;
    UCHAR NumberOfOps : 5;
    SHORT EpilogOffset;
} EPILOG_INFO_V3, *PEPILOG_INFO_V3;

typedef struct _EPILOG_INFO_EX_V3 {
    USHORT FirstOp;                 // byte index into the WOD pool
    UCHAR IpOffsetOfLastInstruction;
} EPILOG_INFO_EX_V3, *PEPILOG_INFO_EX_V3;

typedef struct _EPILOG_INFO_LARGE_EX_V3 {
    USHORT FirstOp;                 // byte index into the WOD pool
    USHORT IpOffsetOfLastInstruction;
} EPILOG_INFO_LARGE_EX_V3, *PEPILOG_INFO_LARGE_EX_V3;

typedef struct _UNWIND_INFO_LARGE_V3 {
    UCHAR SizeOfPrologHighByte;
} UNWIND_INFO_LARGE_V3, *PUNWIND_INFO_LARGE_V3;

#pragma pack(pop)

static_assert(sizeof(WOD_SET_FPREG) == 2, "WOD_SET_FPREG must be 2 bytes");
static_assert(sizeof(WOD_ALLOC_HUGE) == 5, "WOD_ALLOC_HUGE must be 5 bytes");
static_assert(sizeof(WOD_ALLOC_LARGE) == 3, "WOD_ALLOC_LARGE must be 3 bytes");
static_assert(sizeof(WOD_PUSH_CANONICAL_FRAME) == 2, "WOD_PUSH_CANONICAL_FRAME must be 2 bytes");
static_assert(sizeof(WOD_PUSH) == 1, "WOD_PUSH must be 1 byte");
static_assert(sizeof(WOD_SAVE_NONVOL_FAR) == 5, "WOD_SAVE_NONVOL_FAR must be 5 bytes");
static_assert(sizeof(WOD_SAVE_NONVOL) == 3, "WOD_SAVE_NONVOL must be 3 bytes");
static_assert(sizeof(WOD_PUSH_CONSECUTIVE_2) == 1, "WOD_PUSH_CONSECUTIVE_2 must be 1 byte");
static_assert(sizeof(WOD_ALLOC_SMALL) == 1, "WOD_ALLOC_SMALL must be 1 byte");
static_assert(sizeof(WOD_SAVE_XMM128_FAR) == 5, "WOD_SAVE_XMM128_FAR must be 5 bytes");
static_assert(sizeof(WOD_SAVE_XMM128) == 3, "WOD_SAVE_XMM128 must be 3 bytes");
static_assert(sizeof(WOD_PUSH2) == 2, "WOD_PUSH2 must be 2 bytes");
static_assert(sizeof(EPILOG_INFO_V3) == 3, "EPILOG_INFO_V3 must be 3 bytes");
static_assert(sizeof(EPILOG_INFO_EX_V3) == 3, "EPILOG_INFO_EX_V3 must be 3 bytes");
static_assert(sizeof(EPILOG_INFO_LARGE_EX_V3) == 4, "EPILOG_INFO_LARGE_EX_V3 must be 4 bytes");

//
// Define epilog descriptor flags.
//

#define EPILOG_INFO_PARENT_FRAGMENT_TRANSFER 0x1
#define EPILOG_INFO_LARGE 0x2

//
// Define field limits implied by the encoding.
//

#define UNWIND_INFO_V3_MAX_OPS 31               // NumberOfOps is 5 bits
#define UNWIND_INFO_V3_MAX_EPILOGS 7            // NumberOfEpilogs is 3 bits
#define UNWIND_INFO_V3_MAX_PAYLOAD_BYTES 510    // PayloadWords is 8 bits
#define UNWIND_INFO_V3_MAX_EPILOG_SIZE 255      // without EPILOG_INFO_LARGE
#define UNWIND_INFO_V3_MAX_SET_FPREG 240        // WOD_SET_FPREG.Offset is 4 bits scaled by 16

//
// Define the unwind information header. See
// https://learn.microsoft.com/en-us/cpp/build/x64-unwind-information-v3#unwind_info_v3-header.
//

typedef struct _UNWIND_INFO_V3 {
    UCHAR Version : 3;
    UCHAR Flags : 5;
    UCHAR SizeOfProlog;
    UCHAR PayloadWords;
    UCHAR NumberOfOps : 5;
    UCHAR NumberOfEpilogs : 3;

//
// The header is followed by PayloadWords 16-bit words of payload, in this order:
//
//  UNWIND_INFO_LARGE_V3 LargeInfo;             // only if UNW_FLAG_LARGE
//  UCHAR PrologIpOffset[NumberOfOps];          // USHORT if UNW_FLAG_LARGE
//  EPILOG_INFO_V3 Epilogs[NumberOfEpilogs];    // each with its extension and IP offsets
//  UCHAR WodPool[];                            // prolog WODs start at offset 0
//
// Prolog IP offsets are relative to the start of the fragment and give the offset of the
// instruction that performs the operation, not of the following instruction as in V1. Entry 0
// is the operation closest to the function body, and the prolog WODs are in the same order.
//
// The payload is followed by an optional DWORD aligned field, at the same place and with the
// same meaning as in V1: the exception handler address and its data, or the address of chained
// unwind information.
//

} UNWIND_INFO_V3, *PUNWIND_INFO_V3;

static_assert(sizeof(UNWIND_INFO_V3) == 4, "UNWIND_INFO_V3 must be 4 bytes");

#endif // TARGET_X86
#endif // _WIN64UNWIND_H_
