// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


/*****************************************************************************/
#ifndef _GCINFO_H_
#define _GCINFO_H_
/*****************************************************************************/

#include <stdlib.h>     // For memcmp()
#include "windef.h"     // For BYTE
#include "daccess.h"

#include "bitvector.h"  // for ptrArgTP

// Some declarations in this file are used on non-x86 platforms, but most are x86-specific.

// Use the lower 2 bits of the offsets stored in the tables
// to encode properties

const unsigned        OFFSET_MASK  = 0x3;  // mask to access the low 2 bits

//
//  Note for untracked locals the flags allowed are "pinned" and "byref"
//   and for tracked locals the flags allowed are "this" and "byref"
//  Note that these definitions should also match the definitions of
//   GC_CALL_INTERIOR and GC_CALL_PINNED in VM/gc.h
//
const unsigned  byref_OFFSET_FLAG  = 0x1;  // the offset is an interior ptr
const unsigned pinned_OFFSET_FLAG  = 0x2;  // the offset is a pinned ptr
const unsigned   this_OFFSET_FLAG  = 0x2;  // the offset is "this"

#if defined(_TARGET_X86_) || defined(_TARGET_ARM_)

#ifndef FASTCALL
#define FASTCALL __fastcall
#endif

// we use offsetof to get the offset of a field
#include <stddef.h> // offsetof
#ifndef offsetof
#define offsetof(s,m)   ((size_t)&(((s *)0)->m))
#endif

enum infoHdrAdjustConstants {
    // Constants
    SET_FRAMESIZE_MAX  =  7,
    SET_ARGCOUNT_MAX   =  8,  // Change to 6
    SET_PROLOGSIZE_MAX = 16,
    SET_EPILOGSIZE_MAX = 10,  // Change to 6
    SET_EPILOGCNT_MAX  =  4,
    SET_UNTRACKED_MAX  =  3
};

//
// Enum to define the 128 codes that are used to incrementally adjust the InfoHdr structure
//
enum infoHdrAdjust {

    SET_FRAMESIZE   = 0,                                            // 0x00
    SET_ARGCOUNT    = SET_FRAMESIZE  + SET_FRAMESIZE_MAX  + 1,      // 0x08
    SET_PROLOGSIZE  = SET_ARGCOUNT   + SET_ARGCOUNT_MAX   + 1,      // 0x11
    SET_EPILOGSIZE  = SET_PROLOGSIZE + SET_PROLOGSIZE_MAX + 1,      // 0x22
    SET_EPILOGCNT   = SET_EPILOGSIZE + SET_EPILOGSIZE_MAX + 1,      // 0x2d
    SET_UNTRACKED   = SET_EPILOGCNT  + (SET_EPILOGCNT_MAX + 1) * 2, // 0x37

    FIRST_FLIP      = SET_UNTRACKED  + SET_UNTRACKED_MAX + 1,

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

                              // 0x4E .. 0x4f unused

    NEXT_FOUR_START       = 0x50,
    NEXT_FOUR_FRAMESIZE   = 0x50,
    NEXT_FOUR_ARGCOUNT    = 0x60,
    NEXT_THREE_PROLOGSIZE = 0x70,
    NEXT_THREE_EPILOGSIZE = 0x78
};

#define HAS_UNTRACKED               ((unsigned int) -1)
#define HAS_VARPTR                  ((unsigned int) -1)
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

#ifdef BINDER
#ifdef TARGET_ARM

#define NUM_PRESERVED_REGS 9

enum RegMask
{
    RBM_NONE= 0x0000,
    RBM_R0  = 0x0001,
    RBM_R1  = 0x0002,
    RBM_R2  = 0x0004,
    RBM_R3  = 0x0008,
    RBM_R4  = 0x0010,   // callee saved
    RBM_R5  = 0x0020,   // callee saved
    RBM_R6  = 0x0040,   // callee saved
    RBM_R7  = 0x0080,   // callee saved
    RBM_R8  = 0x0100,   // callee saved
    RBM_R9  = 0x0200,   // callee saved
    RBM_R10 = 0x0400,   // callee saved
    RBM_R11 = 0x0800,   // callee saved
    RBM_R12 = 0x1000,
    RBM_SP  = 0x2000,
    RBM_LR  = 0x4000,   // callee saved, but not valid to be alive across a call!
    RBM_PC  = 0x8000,
    RBM_RETVAL = RBM_R0,
    RBM_CALLEE_SAVED_REGS = (RBM_R4|RBM_R5|RBM_R6|RBM_R7|RBM_R8|RBM_R9|RBM_R10|RBM_R11|RBM_LR),
    RBM_CALLEE_SAVED_REG_COUNT = 9,
    // Special case: LR is callee saved, but may not appear as a live GC ref except 
    // in the leaf frame because calls will trash it.  Therefore, we ALSO consider 
    // it a scratch register.
    RBM_SCRATCH_REGS = (RBM_R0|RBM_R1|RBM_R2|RBM_R3|RBM_R12|RBM_LR),
    RBM_SCRATCH_REG_COUNT = 6,

    // TritonToDo: is frame pointer part of the saved registers? (stackwalker related)
    RBM_SAVED  = (RBM_R4|RBM_R5|RBM_R6|RBM_R7|RBM_R8|RBM_R9|RBM_R10|RBM_R11|RBM_LR),
    // TritonToDo: CHECK: is this the correct set of registers?
    RBM_VOLATILE = (RBM_R0|RBM_R1|RBM_R2|RBM_R3|RBM_R12|RBM_LR),
    RBM_ALLCODEGEN = (RBM_SAVED | RBM_VOLATILE),
};

enum RegNumber
{
    RN_R0   = 0,
    RN_R1   = 1,
    RN_R2   = 2,
    RN_R3   = 3,
    RN_R4   = 4,
    RN_R5   = 5,
    RN_R6   = 6,
    RN_R7   = 7,
    RN_R8   = 8,
    RN_R9   = 9,
    RN_R10  = 10,
    RN_R11  = 11,
    RN_R12  = 12,
    RN_SP   = 13,
    RN_LR   = 14,
    RN_PC   = 15,

    RN_NONE = 16,
};

enum CalleeSavedRegNum
{
    CSR_NUM_R4  = 0x00,
    CSR_NUM_R5  = 0x01,
    CSR_NUM_R6  = 0x02,
    CSR_NUM_R7  = 0x03,
    CSR_NUM_R8  = 0x04,
    CSR_NUM_R9  = 0x05,
    CSR_NUM_R10 = 0x06,
    CSR_NUM_R11 = 0x07,
    // NOTE: LR is omitted because it may not be live except as a 'scratch' reg
};

enum CalleeSavedRegMask
{
    CSR_MASK_NONE = 0x00,
    CSR_MASK_R4   = 0x001,
    CSR_MASK_R5   = 0x002,
    CSR_MASK_R6   = 0x004,
    CSR_MASK_R7   = 0x008,
    CSR_MASK_R8   = 0x010,
    CSR_MASK_R9   = 0x020,
    CSR_MASK_R10  = 0x040,
    CSR_MASK_R11  = 0x080,
    CSR_MASK_LR   = 0x100,

    CSR_MASK_ALL  = 0x1ff,
    CSR_MASK_HIGHEST = 0x100,
};

enum ScratchRegNum
{
    SR_NUM_R0   = 0x00,
    SR_NUM_R1   = 0x01,
    SR_NUM_R2   = 0x02,
    SR_NUM_R3   = 0x03,
    SR_NUM_R12  = 0x04,
    SR_NUM_LR   = 0x05,
};

enum ScratchRegMask
{
    SR_MASK_NONE = 0x00,
    SR_MASK_R0   = 0x01,
    SR_MASK_R1   = 0x02,
    SR_MASK_R2   = 0x04,
    SR_MASK_R3   = 0x08,
    SR_MASK_R12  = 0x10,
    SR_MASK_LR   = 0x20,
};

#else // TARGET_ARM

#ifdef TARGET_X64
#define NUM_PRESERVED_REGS 8
#else
#define NUM_PRESERVED_REGS 4
#endif

enum RegMask
{
    RBM_NONE = 0x0000,
    RBM_EAX  = 0x0001,
    RBM_ECX  = 0x0002,
    RBM_EDX  = 0x0004,
    RBM_EBX  = 0x0008,   // callee saved
    RBM_ESP  = 0x0010,
    RBM_EBP  = 0x0020,   // callee saved
    RBM_ESI  = 0x0040,   // callee saved
    RBM_EDI  = 0x0080,   // callee saved

    RBM_R8   = 0x0100,
    RBM_R9   = 0x0200,
    RBM_R10  = 0x0400,
    RBM_R11  = 0x0800,
    RBM_R12  = 0x1000,   // callee saved
    RBM_R13  = 0x2000,   // callee saved
    RBM_R14  = 0x4000,   // callee saved
    RBM_R15  = 0x8000,   // callee saved

    RBM_RETVAL = RBM_EAX,

#ifdef TARGET_X64
    // TritonToDo: is RBM_EBP part of the saved registers? (stackwalker related)
    RBM_SAVED  = (RBM_EDI|RBM_ESI|RBM_EBX|RBM_R12|RBM_R13|RBM_R14|RBM_R15),
    //TritonToDo: CHECK: is this the correct set?
    RBM_VOLATILE = (RBM_EAX|RBM_ECX|RBM_EDX|RBM_R8|RBM_R9|RBM_R10|RBM_R11),
    RBM_ALLCODEGEN = (RBM_SAVED | RBM_VOLATILE),

    RBM_CALLEE_SAVED_REGS = (RBM_EDI|RBM_ESI|RBM_EBX|RBM_EBP|RBM_R12|RBM_R13|RBM_R14|RBM_R15),
    RBM_CALLEE_SAVED_REG_COUNT = 8,
    RBM_SCRATCH_REGS = (RBM_EAX|RBM_ECX|RBM_EDX|RBM_R8|RBM_R9|RBM_R10|RBM_R11),
    RBM_SCRATCH_REG_COUNT = 7,
#else
    RBM_SAVED = (RBM_ESI | RBM_EDI | RBM_EBX),
    RBM_VOLATILE = (RBM_EAX | RBM_ECX | RBM_EDX),
    RBM_ALLCODEGEN = (RBM_SAVED | RBM_VOLATILE),

    RBM_CALLEE_SAVED_REGS = (RBM_EDI|RBM_ESI|RBM_EBX|RBM_EBP),
    RBM_CALLEE_SAVED_REG_COUNT = 4,
    RBM_SCRATCH_REGS = (RBM_EAX|RBM_ECX|RBM_EDX),
    RBM_SCRATCH_REG_COUNT = 3,
#endif // TARGET_X64
};

enum RegNumber
{
    RN_EAX = 0,
    RN_ECX = 1,
    RN_EDX = 2,
    RN_EBX = 3,
    RN_ESP = 4,
    RN_EBP = 5,
    RN_ESI = 6,
    RN_EDI = 7,
    RN_R8  = 8,
    RN_R9  = 9,
    RN_R10 = 10,
    RN_R11 = 11,
    RN_R12 = 12,
    RN_R13 = 13,
    RN_R14 = 14,
    RN_R15 = 15,

    RN_NONE = 16,
};

enum CalleeSavedRegNum
{
    CSR_NUM_RBX = 0x00,
    CSR_NUM_RSI = 0x01,
    CSR_NUM_RDI = 0x02,
    CSR_NUM_RBP = 0x03,
    CSR_NUM_R12 = 0x04,
    CSR_NUM_R13 = 0x05,
    CSR_NUM_R14 = 0x06,
    CSR_NUM_R15 = 0x07,
};

enum CalleeSavedRegMask
{
    CSR_MASK_NONE = 0x00,
    CSR_MASK_RBX = 0x01,
    CSR_MASK_RSI = 0x02,
    CSR_MASK_RDI = 0x04,
    CSR_MASK_RBP = 0x08,
    CSR_MASK_R12 = 0x10,
    CSR_MASK_R13 = 0x20,
    CSR_MASK_R14 = 0x40,
    CSR_MASK_R15 = 0x80,

#ifdef TARGET_X64
    CSR_MASK_ALL = 0xFF,
    CSR_MASK_HIGHEST = 0x80,
#else
    CSR_MASK_ALL = 0x0F,
    CSR_MASK_HIGHEST = 0x08,
#endif
};

enum ScratchRegNum
{
    SR_NUM_RAX = 0x00,
    SR_NUM_RCX = 0x01,
    SR_NUM_RDX = 0x02,
    SR_NUM_R8  = 0x03,
    SR_NUM_R9  = 0x04,
    SR_NUM_R10 = 0x05,
    SR_NUM_R11 = 0x06,
};

enum ScratchRegMask
{
    SR_MASK_NONE = 0x00,
    SR_MASK_RAX  = 0x01,
    SR_MASK_RCX  = 0x02,
    SR_MASK_RDX  = 0x04,
    SR_MASK_R8   = 0x08,
    SR_MASK_R9   = 0x10,
    SR_MASK_R10  = 0x20,
    SR_MASK_R11  = 0x40,
};

#endif // TARGET_ARM

#endif // BINDER

// Working set optimization: saving 12 * 128 = 1536 bytes in infoHdrShortcut
struct InfoHdr;

struct InfoHdrSmall {
    unsigned char  prologSize;        // 0
    unsigned char  epilogSize;        // 1
#if !defined(BINDER) || !defined(TARGET_ARM)
    unsigned char  epilogCount   : 3; // 2 [0:2]
#endif
    unsigned char  epilogAtEnd   : 1; // 2 [3]
    unsigned char  ediSaved      : 1; // 2 [4]      which callee-saved regs are pushed onto stack
    unsigned char  esiSaved      : 1; // 2 [5]
    unsigned char  ebxSaved      : 1; // 2 [6]
    unsigned char  ebpSaved      : 1; // 2 [7]
    unsigned char  ebpFrame      : 1; // 3 [0]      locals accessed relative to ebp
    unsigned char  interruptible : 1; // 3 [1]      is intr. at all points (except prolog/epilog), not just call-sites
    unsigned char  doubleAlign   : 1; // 3 [2]      uses double-aligned stack (ebpFrame will be false)
    unsigned char  security      : 1; // 3 [3]      has slot for security object
    unsigned char  handlers      : 1; // 3 [4]      has callable handlers
    unsigned char  localloc      : 1; // 3 [5]      uses localloc
    unsigned char  editNcontinue : 1; // 3 [6]      was JITed in EnC mode
    unsigned char  varargs       : 1; // 3 [7]      function uses varargs calling convention
    unsigned char  profCallbacks : 1; // 4 [0]
    unsigned char  genericsContext : 1;//4 [1]      function reports a generics context parameter is present
    unsigned char  genericsContextIsMethodDesc : 1;//4[2]
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

                                      // 31 bytes total
#ifdef BINDER
#ifdef TARGET_ARM
    unsigned int   calleeSavedRegMask : NUM_PRESERVED_REGS;  // 9 bits
    unsigned int   parmRegsPushed     : 1; // 1 if the prolog pushed R0-R3 on entry, 0 otherwise
    unsigned int   genericContextFlags : 2;
    unsigned int   epilogCount;
             int   callerSpToPspSlotOffset; // offset of PSP slot relative to incoming SP
                                            // only valid if handlers == 1
             int   outgoingArgSize;         // >= 0

             int   genericContextOffset;
             int   securityObjectOffset;    // relative to caller SP
#endif
#endif

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
                 target.syncStartOffset != HAS_SYNC_OFFSET);
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

        return true;
    }

#ifdef  BINDER
    void Init()
    {
        memset(this, 0, sizeof(InfoHdr));
    }

    void SetPrologSize(UInt32 sizeInBytes)
    {
        prologSize = sizeInBytes;
        _ASSERTE(prologSize == sizeInBytes);
    }

    void SetEpilogSize(UInt32 sizeInBytes)
    {
        epilogSize = sizeInBytes;
        _ASSERTE(epilogSize == sizeInBytes);
    }

    void SetEpilogCount(UInt32 count, bool isAtEnd)
    {
        epilogCount = count;
        epilogAtEnd = isAtEnd ? 1 : 0;

        _ASSERTE(epilogCount == count);
        _ASSERTE((count == 1) || !isAtEnd);
    }

    void SetReturnPopSize(UInt32 popSizeInBytes)
    {
        _ASSERTE(0 == (popSizeInBytes % POINTER_SIZE));
#ifdef  TARGET_ARM
        _ASSERTE(GetReturnPopSize() <= (int)popSizeInBytes);
#else
        _ASSERTE(GetReturnPopSize() == 0 || GetReturnPopSize() == (int)popSizeInBytes);
#endif
        argCount = popSizeInBytes / POINTER_SIZE;
    }

    void SetFrameSize(UInt32 frameSizeInBytes)
    {
        _ASSERTE(0 == (frameSizeInBytes % POINTER_SIZE));
        frameSize = frameSizeInBytes / POINTER_SIZE;
    }

    void SetRegSaved(CalleeSavedRegMask regMask)
    {
        _ASSERTE((regMask & ~CSR_MASK_ALL) == CSR_MASK_NONE);
#ifndef TARGET_ARM   //TritonToDo
        if (regMask & CSR_MASK_RSI)
            esiSaved = true;
        if (regMask & CSR_MASK_RDI)
            ediSaved = true;
        if (regMask & CSR_MASK_RBX)
            ebxSaved = true;
        if (regMask & CSR_MASK_RBP)
            ebpSaved = true;
#else
        calleeSavedRegMask = regMask;
#endif
    }

    void SetSavedRegs(CalleeSavedRegMask regMask)
    {
#ifndef TARGET_ARM   //TritonToDo
        esiSaved = (regMask & CSR_MASK_RSI) != 0;
        ediSaved = (regMask & CSR_MASK_RDI) != 0;
        ebxSaved = (regMask & CSR_MASK_RBX) != 0;
        ebpSaved = (regMask & CSR_MASK_RBP) != 0;
#else
        calleeSavedRegMask |= regMask;
#endif
    }

    void SetFramePointer(RegNumber regNum, UInt32 offsetInBytes)
    {
        if (regNum == RN_NONE)
        {
            ebpFrame = 0;
        }
        else
        {
#ifndef TARGET_ARM   //TritonToDo
            _ASSERTE(regNum == RN_EBP);
#else
#ifdef CLR_STANDALONE_BINDER
            _ASSERTE(regNum == RN_R11);
#else
            _ASSERTE(regNum == RN_R7); // REDHAWK
#endif
#endif
            ebpFrame = 1;
        }

#ifdef TARGET_X64
        ASSERT((offsetInBytes % 0x10) == 0);
        UInt32 offsetInSlots = offsetInBytes / 0x10;
        if (offsetInSlots < 7)
        {
            x64_framePtrOffsetSmall = offsetInSlots;
            x64_framePtrOffset = 0;
        }
        else
        {
            x64_framePtrOffsetSmall = 7;
            x64_framePtrOffset = offsetInSlots - 7;
        }
#else
        _ASSERTE(offsetInBytes == 0);
#endif // TARGET_X64
    }

    int GetFrameSize()
    {
        return frameSize*POINTER_SIZE;
    }

    int GetReturnPopSize() // returned in bytes
    {
        return argCount*POINTER_SIZE;
    }

    int GetPreservedRegsSaveSize() const // returned in bytes
    {
#ifndef TARGET_ARM   //TritonToDo
        return (ediSaved + esiSaved + ebxSaved + ebpSaved)*POINTER_SIZE;
#else
        UInt32 count = 0;
        UInt32 mask = calleeSavedRegMask;
        while (mask != 0)
        {
            count += mask & 1;
            mask >>= 1;
        }
        return (int) count * POINTER_SIZE;
#endif
    }

    CalleeSavedRegMask GetSavedRegs()
    {
        unsigned result = CSR_MASK_NONE;
#ifndef TARGET_ARM   //TritonToDo
        if (ediSaved)
            result |= CSR_MASK_RDI;
        if (esiSaved)
            result |= CSR_MASK_RSI;
        if (ebxSaved)
            result |= CSR_MASK_RBX;
        if (ebpSaved)
            result |= CSR_MASK_RBP;
        return (CalleeSavedRegMask)result;
#else
        return (CalleeSavedRegMask) calleeSavedRegMask;
#endif
    }

    bool HasFramePointer()
    {
        return ebpFrame;
    }

    bool IsRegSaved(CalleeSavedRegMask reg)
    {
        return (0 != (GetSavedRegs() & reg));
    }

#ifdef TARGET_ARM
    UInt32 GetEpilogCount()
    {
        return epilogCount;
    }

    bool IsEpilogAtEnd()
    {
        return epilogAtEnd != 0;
    }
#endif // TARGET_ARM

#endif // BINDER
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
    * ((InfoHdrSmall *) header) = infoHdrShortcut[index];

    header->gsCookieOffset  = 0;
    header->syncStartOffset = 0;
    header->syncEndOffset   = 0;
}

PTR_CBYTE FASTCALL decodeHeader(PTR_CBYTE table, InfoHdr* header);

BYTE FASTCALL encodeHeaderFirst(const InfoHdr& header, InfoHdr* state, int* more, int *pCached);
BYTE FASTCALL encodeHeaderNext (const InfoHdr& header, InfoHdr* state);

size_t FASTCALL decodeUnsigned (PTR_CBYTE src, unsigned* value);
size_t FASTCALL decodeUDelta   (PTR_CBYTE src, unsigned* value, unsigned lastValue);
size_t FASTCALL decodeSigned   (PTR_CBYTE src, int     * value);

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

#endif // _TARGET_86_ || _TARGET_ARM_

/*****************************************************************************/
#endif //_GCINFO_H_
/*****************************************************************************/
