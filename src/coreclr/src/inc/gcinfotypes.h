// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#ifndef __GCINFOTYPES_H__
#define __GCINFOTYPES_H__

#ifndef FEATURE_REDHAWK
#include "gcinfo.h"
#endif

// *****************************************************************************
// WARNING!!!: These values and code are also used by SOS in the diagnostics
// repo. Should updated in a backwards and forwards compatible way.
// See: https://github.com/dotnet/diagnostics/blob/master/src/inc/gcinfotypes.h
// *****************************************************************************

#define PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED

#define FIXED_STACK_PARAMETER_SCRATCH_AREA


#define BITS_PER_SIZE_T ((int)sizeof(size_t)*8)


//--------------------------------------------------------------------------------
// It turns out, that ((size_t)x) << y == x, when y is not a literal 
//      and its value is BITS_PER_SIZE_T
// I guess the processor only shifts of the right operand modulo BITS_PER_SIZE_T
// In many cases, we want the above operation to yield 0, 
//      hence the following macros
//--------------------------------------------------------------------------------
__forceinline size_t SAFE_SHIFT_LEFT(size_t x, size_t count)
{
    _ASSERTE(count <= BITS_PER_SIZE_T);
    return (x << 1) << (count - 1);
}
__forceinline size_t SAFE_SHIFT_RIGHT(size_t x, size_t count)
{
    _ASSERTE(count <= BITS_PER_SIZE_T);
    return (x >> 1) >> (count - 1);
}

inline UINT32 CeilOfLog2(size_t x)
{
    _ASSERTE(x > 0);
    UINT32 result = (x & (x - 1)) ? 1 : 0;
    while (x != 1)
    {
        result++;
        x >>= 1;
    }
    return result;
}

enum GcSlotFlags
{
    GC_SLOT_BASE      = 0x0,
    GC_SLOT_INTERIOR  = 0x1,
    GC_SLOT_PINNED    = 0x2,
    GC_SLOT_UNTRACKED = 0x4,

    // For internal use by the encoder/decoder
    GC_SLOT_IS_REGISTER = 0x8,
    GC_SLOT_IS_DELETED  = 0x10,
};

enum GcStackSlotBase
{
    GC_CALLER_SP_REL = 0x0,
    GC_SP_REL        = 0x1,
    GC_FRAMEREG_REL  = 0x2,

    GC_SPBASE_FIRST  = GC_CALLER_SP_REL,
    GC_SPBASE_LAST   = GC_FRAMEREG_REL,
};

#ifdef _DEBUG
const char* const GcStackSlotBaseNames[] =
{
    "caller.sp",
    "sp",
    "frame",
};
#endif

enum GcSlotState
{
    GC_SLOT_DEAD = 0x0,
    GC_SLOT_LIVE = 0x1,
};

struct GcStackSlot
{
    INT32 SpOffset;
    GcStackSlotBase Base;

    bool operator==(const GcStackSlot& other)
    {
        return ((SpOffset == other.SpOffset) && (Base == other.Base));
    }
    bool operator!=(const GcStackSlot& other)
    {
        return ((SpOffset != other.SpOffset) || (Base != other.Base));
    }
};

//--------------------------------------------------------------------------------
// ReturnKind -- encoding return type information in GcInfo
// 
// When a method is stopped at a call - site for GC (ex: via return-address 
// hijacking) the runtime needs to know whether the value is a GC - value
// (gc - pointer or gc - pointers stored in an aggregate).
// It needs this information so that mark - phase can preserve the gc-pointers 
// being returned.
//
// The Runtime doesn't need the precise return-type of a method. 
// It only needs to find the GC-pointers in the return value.
// The only scenarios currently supported by CoreCLR are:
// 1. Object references
// 2. ByRef pointers
// 3. ARM64/X64 only : Structs returned in two registers
// 4. X86 only : Floating point returns to perform the correct save/restore 
//    of the return value around return-hijacking.
//
// Based on these cases, the legal set of ReturnKind enumerations are specified 
// for each architecture/encoding. 
// A value of this enumeration is stored in the GcInfo header.
//
//--------------------------------------------------------------------------------

// RT_Unset: An intermediate step for staged bringup.
// When ReturnKind is RT_Unset, it means that the JIT did not set 
// the ReturnKind in the GCInfo, and therefore the VM cannot rely on it,
// and must use other mechanisms (similar to GcInfo ver 1) to determine 
// the Return type's GC information.
// 
// RT_Unset is only used in the following situations:
// X64: Used by JIT64 until updated to use GcInfo v2 API
// ARM: Used by JIT32 until updated to use GcInfo v2 API
//
// RT_Unset should have a valid encoding, whose bits are actually stored in the image.
// For X86, there are no free bits, and there's no RT_Unused enumeration.

#if defined(_TARGET_X86_)

// 00    RT_Scalar
// 01    RT_Object
// 10    RT_ByRef
// 11    RT_Float

#elif defined(_TARGET_ARM_)

// 00    RT_Scalar
// 01    RT_Object
// 10    RT_ByRef
// 11    RT_Unset

#elif defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_) 

// Slim Header:

// 00    RT_Scalar
// 01    RT_Object
// 10    RT_ByRef
// 11    RT_Unset

// Fat Header:

// 0000  RT_Scalar
// 0001  RT_Object
// 0010  RT_ByRef
// 0011  RT_Unset
// 0100  RT_Scalar_Obj
// 1000  RT_Scalar_ByRef
// 0101  RT_Obj_Obj
// 1001  RT_Obj_ByRef
// 0110  RT_ByRef_Obj
// 1010  RT_ByRef_ByRef

#else
#ifdef PORTABILITY_WARNING
PORTABILITY_WARNING("Need ReturnKind for new Platform")
#endif // PORTABILITY_WARNING
#endif // Target checks 

enum ReturnKind {

    // Cases for Return in one register

    RT_Scalar = 0,
    RT_Object = 1,
    RT_ByRef = 2,

#ifdef _TARGET_X86_
    RT_Float = 3,       // Encoding 3 means RT_Float on X86
#else
    RT_Unset = 3,       // RT_Unset on other platforms
#endif // _TARGET_X86_

    // Cases for Struct Return in two registers
    //
    // We have the following equivalencies, because the VM's behavior is the same 
    // for both cases:
    // RT_Scalar_Scalar == RT_Scalar
    // RT_Obj_Scalar    == RT_Object
    // RT_ByRef_Scalar  == RT_Byref
    // The encoding for these equivalencies will play out well because 
    // RT_Scalar is zero. 
    //
    // Naming: RT_firstReg_secondReg
    // Encoding: <Two bits for secondRef> <Two bits for first Reg> 
    //
    // This encoding with exclusive bits for each register is chosen for ease of use,
    // and because it doesn't cost any more bits. 
    // It can be changed (ex: to a linear sequence) if necessary. 
    // For example, we can encode the GC-information for the two registers in 3 bits (instead of 4) 
    // if we approximate RT_Obj_ByRef and RT_ByRef_Obj as RT_ByRef_ByRef.

    // RT_Scalar_Scalar = RT_Scalar
    RT_Scalar_Obj   = RT_Object << 2 | RT_Scalar,
    RT_Scalar_ByRef = RT_ByRef << 2  | RT_Scalar,

    // RT_Obj_Scalar   = RT_Object
    RT_Obj_Obj      = RT_Object << 2 | RT_Object,
    RT_Obj_ByRef    = RT_ByRef << 2  | RT_Object,

    // RT_ByRef_Scalar  = RT_Byref
    RT_ByRef_Obj    = RT_Object << 2 | RT_ByRef,
    RT_ByRef_ByRef  = RT_ByRef << 2  | RT_ByRef,

    // Illegal or uninitialized value, 
    // Not a valid encoding, never written to image.
    RT_Illegal = 0xFF
};

// Identify ReturnKinds containing useful information
inline bool IsValidReturnKind(ReturnKind returnKind)
{
    return (returnKind != RT_Illegal)
#ifndef _TARGET_X86_
        && (returnKind != RT_Unset)
#endif // _TARGET_X86_
        ;
}

// Identify ReturnKinds that can be a part of a multi-reg struct return
inline bool IsValidFieldReturnKind(ReturnKind returnKind)
{
    return (returnKind == RT_Scalar || returnKind == RT_Object || returnKind == RT_ByRef);
}

inline bool IsPointerFieldReturnKind(ReturnKind returnKind)
{
    _ASSERTE(IsValidFieldReturnKind(returnKind));
    return (returnKind == RT_Object || returnKind == RT_ByRef);
}

inline bool IsValidReturnRegister(size_t regNo)
{
    return (regNo == 0)
#ifdef FEATURE_MULTIREG_RETURN
        || (regNo == 1)
#endif // FEATURE_MULTIREG_RETURN
        ;
}

inline bool IsStructReturnKind(ReturnKind returnKind)
{
    // Two bits encode integer/ref/float return-kinds.
    // Encodings needing more than two bits are (non-scalar) struct-returns.
    return returnKind > 3;
}

inline bool IsScalarReturnKind(ReturnKind returnKind)
{
    return (returnKind == RT_Scalar)
#ifdef _TARGET_X86_
        || (returnKind == RT_Float)
#endif // _TARGET_X86_
        ;
}

inline bool IsPointerReturnKind(ReturnKind returnKind)
{
    return IsValidReturnKind(returnKind) && !IsScalarReturnKind(returnKind);
}

// Helpers for combining/extracting individual ReturnKinds from/to Struct ReturnKinds.
// Encoding is two bits per register

inline ReturnKind GetStructReturnKind(ReturnKind reg0, ReturnKind reg1)
{
    _ASSERTE(IsValidFieldReturnKind(reg0) && IsValidFieldReturnKind(reg1));

    ReturnKind structReturnKind = (ReturnKind)(reg1 << 2 | reg0);

    _ASSERTE(IsValidReturnKind(structReturnKind));

    return structReturnKind;
}

// Extract returnKind for the specified return register.
// Also determines if higher ordinal return registers contain object references
inline ReturnKind ExtractRegReturnKind(ReturnKind returnKind, size_t returnRegOrdinal, bool& moreRegs)
{
    _ASSERTE(IsValidReturnKind(returnKind));
    _ASSERTE(IsValidReturnRegister(returnRegOrdinal));

    // Return kind of each return register is encoded in two bits at returnRegOrdinal*2 position from LSB
    ReturnKind regReturnKind = (ReturnKind)((returnKind >> (returnRegOrdinal * 2)) & 3);
    
    // Check if any other higher ordinal return registers have object references. 
    // ReturnKind of higher ordinal return registers are encoded at (returnRegOrdinal+1)*2) position from LSB
    // If all of the remaining bits are 0 then there isn't any more RT_Object or RT_ByRef encoded in returnKind. 
    moreRegs = (returnKind >> ((returnRegOrdinal+1) * 2)) != 0;

    _ASSERTE(IsValidReturnKind(regReturnKind));
    _ASSERTE((returnRegOrdinal == 0) || IsValidFieldReturnKind(regReturnKind));

    return regReturnKind;
}

inline const char *ReturnKindToString(ReturnKind returnKind)
{
    switch (returnKind) {
    case RT_Scalar: return "Scalar";
    case RT_Object: return "Object";
    case RT_ByRef:  return "ByRef";
#ifdef _TARGET_X86_
    case RT_Float:  return "Float";
#else
    case RT_Unset:         return "UNSET";
#endif // _TARGET_X86_
    case RT_Scalar_Obj:    return "{Scalar, Object}";
    case RT_Scalar_ByRef:  return "{Scalar, ByRef}";
    case RT_Obj_Obj:       return "{Object, Object}";
    case RT_Obj_ByRef:     return "{Object, ByRef}";
    case RT_ByRef_Obj:     return "{ByRef, Object}";
    case RT_ByRef_ByRef:   return "{ByRef, ByRef}";

    case RT_Illegal:   return "<Illegal>";
    default: return "!Impossible!";
    }
}

#ifdef _TARGET_X86_

#include <stdlib.h>     // For memcmp()
#include "bitvector.h"  // for ptrArgTP

#ifndef FASTCALL
#define FASTCALL __fastcall
#endif

// we use offsetof to get the offset of a field
#include <stddef.h> // offsetof

enum infoHdrAdjustConstants {
    // Constants
    SET_FRAMESIZE_MAX = 7,
    SET_ARGCOUNT_MAX = 8,  // Change to 6
    SET_PROLOGSIZE_MAX = 16,
    SET_EPILOGSIZE_MAX = 10,  // Change to 6
    SET_EPILOGCNT_MAX = 4,
    SET_UNTRACKED_MAX = 3,
    SET_RET_KIND_MAX = 4,   // 2 bits for ReturnKind
    ADJ_ENCODING_MAX = 0x7f, // Maximum valid encoding in a byte
                             // Also used to mask off next bit from each encoding byte.
    MORE_BYTES_TO_FOLLOW = 0x80 // If the High-bit of a header or adjustment byte 
                               // is set, then there are more adjustments to follow.
};

//
// Enum to define codes that are used to incrementally adjust the InfoHdr structure.
// First set of opcodes
enum infoHdrAdjust {

    SET_FRAMESIZE = 0,                                            // 0x00
    SET_ARGCOUNT = SET_FRAMESIZE + SET_FRAMESIZE_MAX + 1,      // 0x08
    SET_PROLOGSIZE = SET_ARGCOUNT + SET_ARGCOUNT_MAX + 1,      // 0x11
    SET_EPILOGSIZE = SET_PROLOGSIZE + SET_PROLOGSIZE_MAX + 1,      // 0x22
    SET_EPILOGCNT = SET_EPILOGSIZE + SET_EPILOGSIZE_MAX + 1,      // 0x2d
    SET_UNTRACKED = SET_EPILOGCNT + (SET_EPILOGCNT_MAX + 1) * 2, // 0x37

    FIRST_FLIP = SET_UNTRACKED + SET_UNTRACKED_MAX + 1,

    FLIP_EDI_SAVED = FIRST_FLIP, // 0x3b
    FLIP_ESI_SAVED,           // 0x3c
    FLIP_EBX_SAVED,           // 0x3d
    FLIP_EBP_SAVED,           // 0x3e
    FLIP_EBP_FRAME,           // 0x3f
    FLIP_INTERRUPTIBLE,       // 0x40
    FLIP_DOUBLE_ALIGN,        // 0x41
    FLIP_SECURITY,            // 0x42
    FLIP_HANDLERS,            // 0x43
    FLIP_LOCALLOC,            // 0x44
    FLIP_EDITnCONTINUE,       // 0x45
    FLIP_VAR_PTR_TABLE_SZ,    // 0x46 Flip whether a table-size exits after the header encoding
    FFFF_UNTRACKED_CNT,       // 0x47 There is a count (>SET_UNTRACKED_MAX) after the header encoding
    FLIP_VARARGS,             // 0x48
    FLIP_PROF_CALLBACKS,      // 0x49
    FLIP_HAS_GS_COOKIE,       // 0x4A - The offset of the GuardStack cookie follows after the header encoding
    FLIP_SYNC,                // 0x4B
    FLIP_HAS_GENERICS_CONTEXT,// 0x4C
    FLIP_GENERICS_CONTEXT_IS_METHODDESC,// 0x4D
    FLIP_REV_PINVOKE_FRAME,   // 0x4E
    NEXT_OPCODE,              // 0x4F -- see next Adjustment enumeration
    NEXT_FOUR_START = 0x50,
    NEXT_FOUR_FRAMESIZE = 0x50,
    NEXT_FOUR_ARGCOUNT = 0x60,
    NEXT_THREE_PROLOGSIZE = 0x70,
    NEXT_THREE_EPILOGSIZE = 0x78
};

// Second set of opcodes, when first code is 0x4F
enum infoHdrAdjust2 {
    SET_RETURNKIND = 0,  // 0x00-SET_RET_KIND_MAX Set ReturnKind to value
};

#define HAS_UNTRACKED               ((unsigned int) -1)
#define HAS_VARPTR                  ((unsigned int) -1)

#define INVALID_REV_PINVOKE_OFFSET   0
#define HAS_REV_PINVOKE_FRAME_OFFSET ((unsigned int) -1)
// 0 is not a valid offset for EBP-frames as all locals are at a negative offset
// For ESP frames, the cookie is above (at a higher address than) the buffers, 
// and so cannot be at offset 0.
#define INVALID_GS_COOKIE_OFFSET    0
// Temporary value to indicate that the offset needs to be read after the header
#define HAS_GS_COOKIE_OFFSET        ((unsigned int) -1)

// 0 is not a valid sync offset
#define INVALID_SYNC_OFFSET         0
// Temporary value to indicate that the offset needs to be read after the header
#define HAS_SYNC_OFFSET             ((unsigned int) -1)

#define INVALID_ARGTAB_OFFSET       0

#include <pshpack1.h>

// Working set optimization: saving 12 * 128 = 1536 bytes in infoHdrShortcut
struct InfoHdr;

struct InfoHdrSmall {
    unsigned char  prologSize;        // 0
    unsigned char  epilogSize;        // 1
    unsigned char  epilogCount : 3; // 2 [0:2]
    unsigned char  epilogAtEnd : 1; // 2 [3]
    unsigned char  ediSaved : 1; // 2 [4]      which callee-saved regs are pushed onto stack
    unsigned char  esiSaved : 1; // 2 [5]
    unsigned char  ebxSaved : 1; // 2 [6]
    unsigned char  ebpSaved : 1; // 2 [7]
    unsigned char  ebpFrame : 1; // 3 [0]      locals accessed relative to ebp
    unsigned char  interruptible : 1; // 3 [1]      is intr. at all points (except prolog/epilog), not just call-sites
    unsigned char  doubleAlign : 1; // 3 [2]      uses double-aligned stack (ebpFrame will be false)
    unsigned char  security : 1; // 3 [3]      has slot for security object
    unsigned char  handlers : 1; // 3 [4]      has callable handlers
    unsigned char  localloc : 1; // 3 [5]      uses localloc
    unsigned char  editNcontinue : 1; // 3 [6]      was JITed in EnC mode
    unsigned char  varargs : 1; // 3 [7]      function uses varargs calling convention
    unsigned char  profCallbacks : 1; // 4 [0]
    unsigned char  genericsContext : 1;//4 [1]      function reports a generics context parameter is present
    unsigned char  genericsContextIsMethodDesc : 1;//4[2]
    unsigned char  returnKind : 2; // 4 [4]  Available GcInfo v2 onwards, previously undefined 
    unsigned short argCount;          // 5,6        in bytes
    unsigned int   frameSize;         // 7,8,9,10   in bytes
    unsigned int   untrackedCnt;      // 11,12,13,14
    unsigned int   varPtrTableSize;   // 15.16,17,18

                                      // Checks whether "this" is compatible with "target".
                                      // It is not an exact bit match as "this" could have some 
                                      // marker/place-holder values, which will have to be written out
                                      // after the header.

    bool isHeaderMatch(const InfoHdr& target) const;
};


struct InfoHdr : public InfoHdrSmall {
    // 0 (zero) means that there is no GuardStack cookie
    // The cookie is either at ESP+gsCookieOffset or EBP-gsCookieOffset
    unsigned int   gsCookieOffset;    // 19,20,21,22
    unsigned int   syncStartOffset;   // 23,24,25,26
    unsigned int   syncEndOffset;     // 27,28,29,30
    unsigned int   revPInvokeOffset;  // 31,32,33,34 Available GcInfo v2 onwards, previously undefined 
                                      // 35 bytes total

                                      // Checks whether "this" is compatible with "target".
                                      // It is not an exact bit match as "this" could have some 
                                      // marker/place-holder values, which will have to be written out
                                      // after the header.

    bool isHeaderMatch(const InfoHdr& target) const
    {
#ifdef _ASSERTE
        // target cannot have place-holder values.
        _ASSERTE(target.untrackedCnt != HAS_UNTRACKED &&
            target.varPtrTableSize != HAS_VARPTR &&
            target.gsCookieOffset != HAS_GS_COOKIE_OFFSET &&
            target.syncStartOffset != HAS_SYNC_OFFSET && 
            target.revPInvokeOffset != HAS_REV_PINVOKE_FRAME_OFFSET);
#endif

        // compare two InfoHdr's up to but not including the untrackCnt field
        if (memcmp(this, &target, offsetof(InfoHdr, untrackedCnt)) != 0)
            return false;

        if (untrackedCnt != target.untrackedCnt) {
            if (target.untrackedCnt <= SET_UNTRACKED_MAX)
                return false;
            else if (untrackedCnt != HAS_UNTRACKED)
                return false;
        }

        if (varPtrTableSize != target.varPtrTableSize) {
            if ((varPtrTableSize != 0) != (target.varPtrTableSize != 0))
                return false;
        }

        if ((gsCookieOffset == INVALID_GS_COOKIE_OFFSET) !=
            (target.gsCookieOffset == INVALID_GS_COOKIE_OFFSET))
            return false;

        if ((syncStartOffset == INVALID_SYNC_OFFSET) !=
            (target.syncStartOffset == INVALID_SYNC_OFFSET))
            return false;

        if ((revPInvokeOffset == INVALID_REV_PINVOKE_OFFSET) !=
            (target.revPInvokeOffset == INVALID_REV_PINVOKE_OFFSET))
            return false;

        return true;
    }
};


union CallPattern {
    struct {
        unsigned char argCnt;
        unsigned char regMask;  // EBP=0x8, EBX=0x4, ESI=0x2, EDI=0x1
        unsigned char argMask;
        unsigned char codeDelta;
    }            fld;
    unsigned     val;
};

#include <poppack.h>

#define IH_MAX_PROLOG_SIZE (51)

extern const InfoHdrSmall infoHdrShortcut[];
extern int                infoHdrLookup[];

inline void GetInfoHdr(int index, InfoHdr * header)
{
    *((InfoHdrSmall *)header) = infoHdrShortcut[index];

    header->gsCookieOffset = INVALID_GS_COOKIE_OFFSET;
    header->syncStartOffset = INVALID_SYNC_OFFSET;
    header->syncEndOffset = INVALID_SYNC_OFFSET;
    header->revPInvokeOffset = INVALID_REV_PINVOKE_OFFSET;
}

PTR_CBYTE FASTCALL decodeHeader(PTR_CBYTE table, UINT32 version, InfoHdr* header);

BYTE FASTCALL encodeHeaderFirst(const InfoHdr& header, InfoHdr* state, int* more, int *pCached);
BYTE FASTCALL encodeHeaderNext(const InfoHdr& header, InfoHdr* state, BYTE &codeSet);

size_t FASTCALL decodeUnsigned(PTR_CBYTE src, unsigned* value);
size_t FASTCALL decodeUDelta(PTR_CBYTE src, unsigned* value, unsigned lastValue);
size_t FASTCALL decodeSigned(PTR_CBYTE src, int     * value);

#define CP_MAX_CODE_DELTA  (0x23)
#define CP_MAX_ARG_CNT     (0x02)
#define CP_MAX_ARG_MASK    (0x00)

extern const unsigned callPatternTable[];
extern const unsigned callCommonDelta[];


int  FASTCALL lookupCallPattern(unsigned    argCnt,
    unsigned    regMask,
    unsigned    argMask,
    unsigned    codeDelta);

void FASTCALL decodeCallPattern(int         pattern,
    unsigned *  argCnt,
    unsigned *  regMask,
    unsigned *  argMask,
    unsigned *  codeDelta);

#endif // _TARGET_86_ 

// Stack offsets must be 8-byte aligned, so we use this unaligned
//  offset to represent that the method doesn't have a security object
#define NO_SECURITY_OBJECT        (-1)
#define NO_GS_COOKIE              (-1)
#define NO_STACK_BASE_REGISTER    (0xffffffff)
#define NO_SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA (0xffffffff)
#define NO_GENERICS_INST_CONTEXT  (-1)
#define NO_REVERSE_PINVOKE_FRAME  (-1)
#define NO_PSP_SYM                (-1)

#if defined(_TARGET_AMD64_)

#ifndef TARGET_POINTER_SIZE
#define TARGET_POINTER_SIZE 8    // equal to sizeof(void*) and the managed pointer size in bytes for this target
#endif
#define NUM_NORM_CODE_OFFSETS_PER_CHUNK (64)
#define NUM_NORM_CODE_OFFSETS_PER_CHUNK_LOG2 (6)
#define NORMALIZE_STACK_SLOT(x) ((x)>>3)
#define DENORMALIZE_STACK_SLOT(x) ((x)<<3)
#define NORMALIZE_CODE_LENGTH(x) (x)
#define DENORMALIZE_CODE_LENGTH(x) (x)
// Encode RBP as 0
#define NORMALIZE_STACK_BASE_REGISTER(x) ((x) ^ 5)
#define DENORMALIZE_STACK_BASE_REGISTER(x) ((x) ^ 5)
#define NORMALIZE_SIZE_OF_STACK_AREA(x) ((x)>>3)
#define DENORMALIZE_SIZE_OF_STACK_AREA(x) ((x)<<3)
#define CODE_OFFSETS_NEED_NORMALIZATION 0
#define NORMALIZE_CODE_OFFSET(x) (x)
#define DENORMALIZE_CODE_OFFSET(x) (x)
#define NORMALIZE_REGISTER(x) (x)
#define DENORMALIZE_REGISTER(x) (x)
#define NORMALIZE_NUM_SAFE_POINTS(x) (x)
#define DENORMALIZE_NUM_SAFE_POINTS(x) (x)
#define NORMALIZE_NUM_INTERRUPTIBLE_RANGES(x) (x)
#define DENORMALIZE_NUM_INTERRUPTIBLE_RANGES(x) (x)

#define PSP_SYM_STACK_SLOT_ENCBASE 6 
#define GENERICS_INST_CONTEXT_STACK_SLOT_ENCBASE 6
#define SECURITY_OBJECT_STACK_SLOT_ENCBASE 6
#define GS_COOKIE_STACK_SLOT_ENCBASE 6
#define CODE_LENGTH_ENCBASE 8
#define SIZE_OF_RETURN_KIND_IN_SLIM_HEADER 2
#define SIZE_OF_RETURN_KIND_IN_FAT_HEADER  4
#define STACK_BASE_REGISTER_ENCBASE 3
#define SIZE_OF_STACK_AREA_ENCBASE 3
#define SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA_ENCBASE 4
#define REVERSE_PINVOKE_FRAME_ENCBASE 6
#define NUM_REGISTERS_ENCBASE 2
#define NUM_STACK_SLOTS_ENCBASE 2
#define NUM_UNTRACKED_SLOTS_ENCBASE 1
#define NORM_PROLOG_SIZE_ENCBASE 5
#define NORM_EPILOG_SIZE_ENCBASE 3
#define NORM_CODE_OFFSET_DELTA_ENCBASE 3
#define INTERRUPTIBLE_RANGE_DELTA1_ENCBASE 6
#define INTERRUPTIBLE_RANGE_DELTA2_ENCBASE 6
#define REGISTER_ENCBASE 3
#define REGISTER_DELTA_ENCBASE 2
#define STACK_SLOT_ENCBASE 6
#define STACK_SLOT_DELTA_ENCBASE 4
#define NUM_SAFE_POINTS_ENCBASE 2
#define NUM_INTERRUPTIBLE_RANGES_ENCBASE 1
#define NUM_EH_CLAUSES_ENCBASE 2
#define POINTER_SIZE_ENCBASE 3
#define LIVESTATE_RLE_RUN_ENCBASE 2
#define LIVESTATE_RLE_SKIP_ENCBASE 4

#elif defined(_TARGET_ARM_)

#ifndef TARGET_POINTER_SIZE
#define TARGET_POINTER_SIZE 4   // equal to sizeof(void*) and the managed pointer size in bytes for this target
#endif
#define NUM_NORM_CODE_OFFSETS_PER_CHUNK (64)
#define NUM_NORM_CODE_OFFSETS_PER_CHUNK_LOG2 (6)
#define NORMALIZE_STACK_SLOT(x) ((x)>>2)
#define DENORMALIZE_STACK_SLOT(x) ((x)<<2)
#define NORMALIZE_CODE_LENGTH(x) ((x)>>1)
#define DENORMALIZE_CODE_LENGTH(x) ((x)<<1)
// Encode R11 as zero
#define NORMALIZE_STACK_BASE_REGISTER(x) ((((x) - 4) & 7) ^ 7)
#define DENORMALIZE_STACK_BASE_REGISTER(x) (((x) ^ 7) + 4)
#define NORMALIZE_SIZE_OF_STACK_AREA(x) ((x)>>2)
#define DENORMALIZE_SIZE_OF_STACK_AREA(x) ((x)<<2)
#define CODE_OFFSETS_NEED_NORMALIZATION 1
#define NORMALIZE_CODE_OFFSET(x) (x)   // Instructions are 2/4 bytes long in Thumb/ARM states, 
#define DENORMALIZE_CODE_OFFSET(x) (x) // but the safe-point offsets are encoded with a -1 adjustment.
#define NORMALIZE_REGISTER(x) (x)
#define DENORMALIZE_REGISTER(x) (x)
#define NORMALIZE_NUM_SAFE_POINTS(x) (x)
#define DENORMALIZE_NUM_SAFE_POINTS(x) (x)
#define NORMALIZE_NUM_INTERRUPTIBLE_RANGES(x) (x)
#define DENORMALIZE_NUM_INTERRUPTIBLE_RANGES(x) (x)

// The choices of these encoding bases only affects space overhead
// and performance, not semantics/correctness.
#define PSP_SYM_STACK_SLOT_ENCBASE 5 
#define GENERICS_INST_CONTEXT_STACK_SLOT_ENCBASE 5
#define SECURITY_OBJECT_STACK_SLOT_ENCBASE 5
#define GS_COOKIE_STACK_SLOT_ENCBASE 5
#define CODE_LENGTH_ENCBASE 7
#define SIZE_OF_RETURN_KIND_IN_SLIM_HEADER 2
#define SIZE_OF_RETURN_KIND_IN_FAT_HEADER  2
#define STACK_BASE_REGISTER_ENCBASE 1
#define SIZE_OF_STACK_AREA_ENCBASE 3
#define SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA_ENCBASE 3
#define REVERSE_PINVOKE_FRAME_ENCBASE 5
#define NUM_REGISTERS_ENCBASE 2
#define NUM_STACK_SLOTS_ENCBASE 3
#define NUM_UNTRACKED_SLOTS_ENCBASE 3
#define NORM_PROLOG_SIZE_ENCBASE 5
#define NORM_EPILOG_SIZE_ENCBASE 3
#define NORM_CODE_OFFSET_DELTA_ENCBASE 3
#define INTERRUPTIBLE_RANGE_DELTA1_ENCBASE 4
#define INTERRUPTIBLE_RANGE_DELTA2_ENCBASE 6
#define REGISTER_ENCBASE 2
#define REGISTER_DELTA_ENCBASE 1
#define STACK_SLOT_ENCBASE 6
#define STACK_SLOT_DELTA_ENCBASE 4
#define NUM_SAFE_POINTS_ENCBASE 3
#define NUM_INTERRUPTIBLE_RANGES_ENCBASE 2
#define NUM_EH_CLAUSES_ENCBASE 3
#define POINTER_SIZE_ENCBASE 3
#define LIVESTATE_RLE_RUN_ENCBASE 2
#define LIVESTATE_RLE_SKIP_ENCBASE 4

#elif defined(_TARGET_ARM64_)

#ifndef TARGET_POINTER_SIZE
#define TARGET_POINTER_SIZE 8    // equal to sizeof(void*) and the managed pointer size in bytes for this target
#endif
#define NUM_NORM_CODE_OFFSETS_PER_CHUNK (64)
#define NUM_NORM_CODE_OFFSETS_PER_CHUNK_LOG2 (6)
#define NORMALIZE_STACK_SLOT(x) ((x)>>3)   // GC Pointers are 8-bytes aligned
#define DENORMALIZE_STACK_SLOT(x) ((x)<<3)
#define NORMALIZE_CODE_LENGTH(x) ((x)>>2)   // All Instructions are 4 bytes long
#define DENORMALIZE_CODE_LENGTH(x) ((x)<<2) 
#define NORMALIZE_STACK_BASE_REGISTER(x) ((x)^29) // Encode Frame pointer X29 as zero
#define DENORMALIZE_STACK_BASE_REGISTER(x) ((x)^29)
#define NORMALIZE_SIZE_OF_STACK_AREA(x) ((x)>>3)
#define DENORMALIZE_SIZE_OF_STACK_AREA(x) ((x)<<3)
#define CODE_OFFSETS_NEED_NORMALIZATION 0
#define NORMALIZE_CODE_OFFSET(x) (x)   // Instructions are 4 bytes long, but the safe-point
#define DENORMALIZE_CODE_OFFSET(x) (x) // offsets are encoded with a -1 adjustment.
#define NORMALIZE_REGISTER(x) (x)
#define DENORMALIZE_REGISTER(x) (x)
#define NORMALIZE_NUM_SAFE_POINTS(x) (x)
#define DENORMALIZE_NUM_SAFE_POINTS(x) (x)
#define NORMALIZE_NUM_INTERRUPTIBLE_RANGES(x) (x)
#define DENORMALIZE_NUM_INTERRUPTIBLE_RANGES(x) (x)

#define PSP_SYM_STACK_SLOT_ENCBASE 6 
#define GENERICS_INST_CONTEXT_STACK_SLOT_ENCBASE 6
#define SECURITY_OBJECT_STACK_SLOT_ENCBASE 6
#define GS_COOKIE_STACK_SLOT_ENCBASE 6
#define CODE_LENGTH_ENCBASE 8
#define SIZE_OF_RETURN_KIND_IN_SLIM_HEADER 2
#define SIZE_OF_RETURN_KIND_IN_FAT_HEADER  4
#define STACK_BASE_REGISTER_ENCBASE 2 // FP encoded as 0, SP as 2.
#define SIZE_OF_STACK_AREA_ENCBASE 3
#define SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA_ENCBASE 4
#define REVERSE_PINVOKE_FRAME_ENCBASE 6
#define NUM_REGISTERS_ENCBASE 3
#define NUM_STACK_SLOTS_ENCBASE 2
#define NUM_UNTRACKED_SLOTS_ENCBASE 1
#define NORM_PROLOG_SIZE_ENCBASE 5
#define NORM_EPILOG_SIZE_ENCBASE 3
#define NORM_CODE_OFFSET_DELTA_ENCBASE 3
#define INTERRUPTIBLE_RANGE_DELTA1_ENCBASE 6
#define INTERRUPTIBLE_RANGE_DELTA2_ENCBASE 6
#define REGISTER_ENCBASE 3
#define REGISTER_DELTA_ENCBASE 2
#define STACK_SLOT_ENCBASE 6
#define STACK_SLOT_DELTA_ENCBASE 4
#define NUM_SAFE_POINTS_ENCBASE 3
#define NUM_INTERRUPTIBLE_RANGES_ENCBASE 1
#define NUM_EH_CLAUSES_ENCBASE 2
#define POINTER_SIZE_ENCBASE 3
#define LIVESTATE_RLE_RUN_ENCBASE 2
#define LIVESTATE_RLE_SKIP_ENCBASE 4

#else

#ifndef _TARGET_X86_
#ifdef PORTABILITY_WARNING
PORTABILITY_WARNING("Please specialize these definitions for your platform!")
#endif
#endif

#ifndef TARGET_POINTER_SIZE
#define TARGET_POINTER_SIZE 4   // equal to sizeof(void*) and the managed pointer size in bytes for this target
#endif
#define NUM_NORM_CODE_OFFSETS_PER_CHUNK (64)
#define NUM_NORM_CODE_OFFSETS_PER_CHUNK_LOG2 (6)
#define NORMALIZE_STACK_SLOT(x) (x)
#define DENORMALIZE_STACK_SLOT(x) (x)
#define NORMALIZE_CODE_LENGTH(x) (x)
#define DENORMALIZE_CODE_LENGTH(x) (x)
#define NORMALIZE_STACK_BASE_REGISTER(x) (x)
#define DENORMALIZE_STACK_BASE_REGISTER(x) (x)
#define NORMALIZE_SIZE_OF_STACK_AREA(x) (x)
#define DENORMALIZE_SIZE_OF_STACK_AREA(x) (x)
#define CODE_OFFSETS_NEED_NORMALIZATION 0
#define NORMALIZE_CODE_OFFSET(x) (x)
#define DENORMALIZE_CODE_OFFSET(x) (x)
#define NORMALIZE_REGISTER(x) (x)
#define DENORMALIZE_REGISTER(x) (x)
#define NORMALIZE_NUM_SAFE_POINTS(x) (x)
#define DENORMALIZE_NUM_SAFE_POINTS(x) (x)
#define NORMALIZE_NUM_INTERRUPTIBLE_RANGES(x) (x)
#define DENORMALIZE_NUM_INTERRUPTIBLE_RANGES(x) (x)

#define PSP_SYM_STACK_SLOT_ENCBASE 6 
#define GENERICS_INST_CONTEXT_STACK_SLOT_ENCBASE 6
#define SECURITY_OBJECT_STACK_SLOT_ENCBASE 6
#define GS_COOKIE_STACK_SLOT_ENCBASE 6
#define CODE_LENGTH_ENCBASE 6
#define SIZE_OF_RETURN_KIND_IN_SLIM_HEADER 2
#define SIZE_OF_RETURN_KIND_IN_FAT_HEADER  2
#define STACK_BASE_REGISTER_ENCBASE 3
#define SIZE_OF_STACK_AREA_ENCBASE 6
#define SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA_ENCBASE 3
#define REVERSE_PINVOKE_FRAME_ENCBASE 6
#define NUM_REGISTERS_ENCBASE 3
#define NUM_STACK_SLOTS_ENCBASE 5
#define NUM_UNTRACKED_SLOTS_ENCBASE 5
#define NORM_PROLOG_SIZE_ENCBASE 4
#define NORM_EPILOG_SIZE_ENCBASE 3
#define NORM_CODE_OFFSET_DELTA_ENCBASE 3
#define INTERRUPTIBLE_RANGE_DELTA1_ENCBASE 5
#define INTERRUPTIBLE_RANGE_DELTA2_ENCBASE 5
#define REGISTER_ENCBASE 3
#define REGISTER_DELTA_ENCBASE REGISTER_ENCBASE
#define STACK_SLOT_ENCBASE 6
#define STACK_SLOT_DELTA_ENCBASE 4
#define NUM_SAFE_POINTS_ENCBASE 4
#define NUM_INTERRUPTIBLE_RANGES_ENCBASE 1
#define NUM_EH_CLAUSES_ENCBASE 2
#define POINTER_SIZE_ENCBASE 3
#define LIVESTATE_RLE_RUN_ENCBASE 2
#define LIVESTATE_RLE_SKIP_ENCBASE 4

#endif

#endif // !__GCINFOTYPES_H__

