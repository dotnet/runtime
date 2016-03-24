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

#ifdef _TARGET_X86_

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

// Working set optimization: saving 12 * 128 = 1536 bytes in infoHdrShortcut
struct InfoHdr;

struct InfoHdrSmall {
    unsigned char  prologSize;        // 0
    unsigned char  epilogSize;        // 1
    unsigned char  epilogCount   : 3; // 2 [0:2]
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
