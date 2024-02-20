// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _UNWIND_X86_H
#define _UNWIND_X86_H

/*****************************************************************************
 <TODO>ToDo: Do we want to include JIT/IL/target.h? </TODO>
 */

enum regNum
{
        REGI_EAX, REGI_ECX, REGI_EDX, REGI_EBX,
        REGI_ESP, REGI_EBP, REGI_ESI, REGI_EDI,
        REGI_COUNT,
        REGI_NA = REGI_COUNT
};

/*****************************************************************************
 Register masks
 */

enum RegMask
{
    RM_EAX = 0x01,
    RM_ECX = 0x02,
    RM_EDX = 0x04,
    RM_EBX = 0x08,
    RM_ESP = 0x10,
    RM_EBP = 0x20,
    RM_ESI = 0x40,
    RM_EDI = 0x80,

    RM_NONE = 0x00,
    RM_ALL = (RM_EAX|RM_ECX|RM_EDX|RM_EBX|RM_ESP|RM_EBP|RM_ESI|RM_EDI),
    RM_CALLEE_SAVED = (RM_EBP|RM_EBX|RM_ESI|RM_EDI),
    RM_CALLEE_TRASHED = (RM_ALL & ~RM_CALLEE_SAVED),
};

/*****************************************************************************
 *
 *  Helper to extract basic info from a method info block.
 */

struct hdrInfo
{
    unsigned int        methodSize;     // native code bytes
    unsigned int        argSize;        // in bytes
    unsigned int        stackSize;      // including callee saved registers
    unsigned int        rawStkSize;     // excluding callee saved registers
    ReturnKind          returnKind;     // The ReturnKind for this method.

    unsigned int        prologSize;

    // Size of the epilogs in the method.
    // For methods which use CEE_JMP, some epilogs may end with a "ret" instruction
    // and some may end with a "jmp". The epilogSize reported should be for the
    // epilog with the smallest size.
    unsigned int        epilogSize;

    unsigned char       epilogCnt;
    bool                epilogEnd;      // is the epilog at the end of the method

    bool                ebpFrame;       // locals and arguments addressed relative to EBP
    bool                doubleAlign;    // is the stack double-aligned? locals addressed relative to ESP, and arguments relative to EBP
    bool                interruptible;  // intr. at all times (excluding prolog/epilog), not just call sites

    bool                handlers;       // has callable handlers
    bool                localloc;       // uses localloc
    bool                editNcontinue;  // has been compiled in EnC mode
    bool                varargs;        // is this a varargs routine
    bool                profCallbacks;  // does the method have Enter-Leave callbacks
    bool                genericsContext;// has a reported generic context parameter
    bool                genericsContextIsMethodDesc;// reported generic context parameter is methoddesc
    bool                isSpeculativeStackWalk; // is the stackwalk seeded by an untrusted source (e.g., sampling profiler)?

    // These always includes EBP for EBP-frames and double-aligned-frames
    RegMask             savedRegMask:8; // which callee-saved regs are saved on stack

    // Count of the callee-saved registers, excluding the frame pointer.
    // This does not include EBP for EBP-frames and double-aligned-frames.
    unsigned int        savedRegsCountExclFP;

    unsigned int        untrackedCnt;
    unsigned int        varPtrTableSize;
    unsigned int        argTabOffset;   // INVALID_ARGTAB_OFFSET if argtab must be reached by stepping through ptr tables
    unsigned int        gsCookieOffset; // INVALID_GS_COOKIE_OFFSET if there is no GuardStack cookie

    unsigned int        syncStartOffset; // start/end code offset of the protected region in synchronized methods.
    unsigned int        syncEndOffset;   // INVALID_SYNC_OFFSET if there not synchronized method
    unsigned int        syncEpilogStart; // The start of the epilog. Synchronized methods are guaranteed to have no more than one epilog.
    unsigned int        revPInvokeOffset; // INVALID_REV_PINVOKE_OFFSET if there is no Reverse PInvoke frame

    enum { NOT_IN_PROLOG = -1, NOT_IN_EPILOG = -1 };

    int                 prologOffs;     // NOT_IN_PROLOG if not in prolog
    int                 epilogOffs;     // NOT_IN_EPILOG if not in epilog. It is never 0

    //
    // Results passed back from scanArgRegTable
    //
    regNum              thisPtrResult;  // register holding "this"
    RegMask             regMaskResult;  // registers currently holding GC ptrs
    RegMask            iregMaskResult;  // iptr qualifier for regMaskResult
    unsigned            argHnumResult;
    PTR_CBYTE            argTabResult;  // Table of encoded offsets of pending ptr args
    unsigned              argTabBytes;  // Number of bytes in argTabResult[]

    // These next two are now large structs (i.e 132 bytes each)

    ptrArgTP            argMaskResult;  // pending arguments mask
    ptrArgTP           iargMaskResult;  // iptr qualifier for argMaskResult
};

#ifndef FEATURE_NATIVEAOT
bool UnwindStackFrame(PREGDISPLAY     pContext,
                      EECodeInfo     *pCodeInfo,
                      unsigned        flags,
                      CodeManState   *pState,
                      StackwalkCacheUnwindInfo  *pUnwindInfo);
#endif

size_t DecodeGCHdrInfo(GCInfoToken gcInfoToken,
                       unsigned    curOffset,
                       hdrInfo   * infoPtr);

#endif // _UNWIND_X86_H
