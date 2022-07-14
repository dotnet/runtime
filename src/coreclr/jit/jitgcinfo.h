// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//  Garbage-collector information
//  Keeps track of which variables hold pointers.
//  Generates the GC-tables

#ifndef _JITGCINFO_H_
#define _JITGCINFO_H_

#include "gcinfotypes.h"

#ifndef JIT32_GCENCODER
#include "gcinfoencoder.h"
#endif

/*****************************************************************************/

#ifndef JIT32_GCENCODER
// Shash typedefs
struct RegSlotIdKey
{
    unsigned short m_regNum;
    unsigned short m_flags;

    RegSlotIdKey()
    {
    }

    RegSlotIdKey(unsigned short regNum, unsigned flags) : m_regNum(regNum), m_flags((unsigned short)flags)
    {
        assert(m_flags == flags);
    }

    static unsigned GetHashCode(RegSlotIdKey rsk)
    {
        return (rsk.m_flags << (8 * sizeof(unsigned short))) + rsk.m_regNum;
    }

    static bool Equals(RegSlotIdKey rsk1, RegSlotIdKey rsk2)
    {
        return rsk1.m_regNum == rsk2.m_regNum && rsk1.m_flags == rsk2.m_flags;
    }
};

struct StackSlotIdKey
{
    int            m_offset;
    bool           m_fpRel;
    unsigned short m_flags;

    StackSlotIdKey()
    {
    }

    StackSlotIdKey(int offset, bool fpRel, unsigned flags)
        : m_offset(offset), m_fpRel(fpRel), m_flags((unsigned short)flags)
    {
        assert(flags == m_flags);
    }

    static unsigned GetHashCode(StackSlotIdKey ssk)
    {
        return (ssk.m_flags << (8 * sizeof(unsigned short))) ^ (unsigned)ssk.m_offset ^ (ssk.m_fpRel ? 0x1000000 : 0);
    }

    static bool Equals(StackSlotIdKey ssk1, StackSlotIdKey ssk2)
    {
        return ssk1.m_offset == ssk2.m_offset && ssk1.m_fpRel == ssk2.m_fpRel && ssk1.m_flags == ssk2.m_flags;
    }
};

typedef JitHashTable<RegSlotIdKey, RegSlotIdKey, GcSlotId>     RegSlotMap;
typedef JitHashTable<StackSlotIdKey, StackSlotIdKey, GcSlotId> StackSlotMap;
#endif

typedef JitHashTable<GenTree*, JitPtrKeyFuncs<GenTree>, VARSET_TP*> NodeToVarsetPtrMap;

class GCInfo
{
    friend class CodeGen;

private:
    Compiler* compiler;
    RegSet*   regSet;

public:
    GCInfo(Compiler* theCompiler);

    void gcResetForBB();

    void gcMarkRegSetGCref(regMaskTP regMask DEBUGARG(bool forceOutput = false));
    void gcMarkRegSetByref(regMaskTP regMask DEBUGARG(bool forceOutput = false));
    void gcMarkRegSetNpt(regMaskTP regMask DEBUGARG(bool forceOutput = false));
    void gcMarkRegPtrVal(regNumber reg, var_types type);

#ifdef DEBUG
    void gcDspGCrefSetChanges(regMaskTP gcRegGCrefSetNew DEBUGARG(bool forceOutput = false));
    void gcDspByrefSetChanges(regMaskTP gcRegByrefSetNew DEBUGARG(bool forceOutput = false));
#endif // DEBUG

    /*****************************************************************************/

    //-------------------------------------------------------------------------
    //
    //  The following keeps track of which registers currently hold pointer
    //  values.
    //

    regMaskTP gcRegGCrefSetCur; // current regs holding GCrefs
    regMaskTP gcRegByrefSetCur; // current regs holding Byrefs

    VARSET_TP gcTrkStkPtrLcls; // set of tracked stack ptr lcls (GCref and Byref) - no args
    VARSET_TP gcVarPtrSetCur;  // currently live part of "gcTrkStkPtrLcls"

    //-------------------------------------------------------------------------
    //
    //  The following keeps track of the lifetimes of non-register variables that
    //  hold pointers.
    //

    struct varPtrDsc
    {
        varPtrDsc* vpdNext;

        unsigned vpdVarNum; // which variable is this about?

        unsigned vpdBegOfs; // the offset where life starts
        unsigned vpdEndOfs; // the offset where life starts
    };

    varPtrDsc* gcVarPtrList;
    varPtrDsc* gcVarPtrLast;

    void gcVarPtrSetInit();

    /*****************************************************************************/

    //  'pointer value' register tracking and argument pushes/pops tracking.

    enum rpdArgType_t
    {
        rpdARG_POP,
        rpdARG_PUSH,
        rpdARG_KILL
    };

    struct regPtrDsc
    {
        regPtrDsc* rpdNext; // next entry in the list
        unsigned   rpdOffs; // the offset of the instruction

        union // 2-16 byte union (depending on architecture)
        {
            struct // 2-16 byte structure (depending on architecture)
            {
                regMaskSmall rpdAdd; // regptr bitset being added
                regMaskSmall rpdDel; // regptr bitset being removed
            } rpdCompiler;

            unsigned short rpdPtrArg; // arg offset or popped arg count
        };

#ifndef JIT32_GCENCODER
        unsigned char rpdCallInstrSize; // Length of the call instruction.
#endif

        unsigned short rpdArg : 1;     // is this an argument descriptor?
        unsigned short rpdArgType : 2; // is this an argument push,pop, or kill?
        rpdArgType_t   rpdArgTypeGet()
        {
            return (rpdArgType_t)rpdArgType;
        }

        unsigned short rpdGCtype : 2; // is this a pointer, after all?
        GCtype         rpdGCtypeGet()
        {
            return (GCtype)rpdGCtype;
        }

        unsigned short rpdIsThis : 1;                       // is it the 'this' pointer
        unsigned short rpdCall : 1;                         // is this a true call site?
        unsigned short : 1;                                 // Padding bit, so next two start on a byte boundary
        unsigned short rpdCallGCrefRegs : CNT_CALLEE_SAVED; // Callee-saved registers containing GC pointers.
        unsigned short rpdCallByrefRegs : CNT_CALLEE_SAVED; // Callee-saved registers containing byrefs.

#ifndef JIT32_GCENCODER
        bool rpdIsCallInstr()
        {
            return rpdCall && rpdCallInstrSize != 0;
        }
#endif
    };

    regPtrDsc* gcRegPtrList;
    regPtrDsc* gcRegPtrLast;
    unsigned   gcPtrArgCnt;

#ifndef JIT32_GCENCODER
    enum MakeRegPtrMode
    {
        MAKE_REG_PTR_MODE_ASSIGN_SLOTS,
        MAKE_REG_PTR_MODE_DO_WORK
    };

    // This method has two modes.  In the "assign slots" mode, it figures out what stack locations are
    // used to contain GC references, and whether those locations contain byrefs or pinning references,
    // building up mappings from tuples of <offset X byref/pinning> to the corresponding slot id.
    // In the "do work" mode, we use these slot ids to actually declare live ranges to the encoder.
    void gcMakeVarPtrTable(GcInfoEncoder* gcInfoEncoder, MakeRegPtrMode mode);

    // At instruction offset "instrOffset," the set of registers indicated by "regMask" is becoming live or dead,
    // depending on whether "newState" is "GC_SLOT_DEAD" or "GC_SLOT_LIVE".  The subset of registers whose corresponding
    // bits are set in "byRefMask" contain by-refs rather than regular GC pointers. "*pPtrRegs" is the set of
    // registers currently known to contain pointers.  If "mode" is "ASSIGN_SLOTS", computes and records slot
    // ids for the registers.  If "mode" is "DO_WORK", informs "gcInfoEncoder" about the state transition,
    // using the previously assigned slot ids, and updates "*pPtrRegs" appropriately.
    void gcInfoRecordGCRegStateChange(GcInfoEncoder* gcInfoEncoder,
                                      MakeRegPtrMode mode,
                                      unsigned       instrOffset,
                                      regMaskSmall   regMask,
                                      GcSlotState    newState,
                                      regMaskSmall   byRefMask,
                                      regMaskSmall*  pPtrRegs);

    // regPtrDsc is also used to encode writes to the outgoing argument space (as if they were pushes)
    void gcInfoRecordGCStackArgLive(GcInfoEncoder* gcInfoEncoder, MakeRegPtrMode mode, regPtrDsc* genStackPtr);

    // Walk all the pushes between genStackPtrFirst (inclusive) and genStackPtrLast (exclusive)
    // and mark them as going dead at instrOffset
    void gcInfoRecordGCStackArgsDead(GcInfoEncoder* gcInfoEncoder,
                                     unsigned       instrOffset,
                                     regPtrDsc*     genStackPtrFirst,
                                     regPtrDsc*     genStackPtrLast);

#endif

#if MEASURE_PTRTAB_SIZE
    static size_t s_gcRegPtrDscSize;
    static size_t s_gcTotalPtrTabSize;
#endif

    regPtrDsc* gcRegPtrAllocDsc();

    /*****************************************************************************/

    //-------------------------------------------------------------------------
    //
    //  If we're not generating fully interruptible code, we create a simple
    //  linked list of call descriptors.
    //

    struct CallDsc
    {
        CallDsc* cdNext;
        void*    cdBlock; // the code block of the call
        unsigned cdOffs;  // the offset     of the call
#ifndef JIT32_GCENCODER
        unsigned short cdCallInstrSize; // the size       of the call instruction.
#endif

        unsigned short cdArgCnt;

        union {
            struct // used if cdArgCnt == 0
            {
                unsigned cdArgMask;      // ptr arg bitfield
                unsigned cdByrefArgMask; // byref qualifier for cdArgMask
            } u1;

            unsigned* cdArgTable; // used if cdArgCnt != 0
        };

        regMaskSmall cdGCrefRegs;
        regMaskSmall cdByrefRegs;
    };

    CallDsc* gcCallDescList;
    CallDsc* gcCallDescLast;

//-------------------------------------------------------------------------

#ifdef JIT32_GCENCODER
    void gcCountForHeader(UNALIGNED unsigned int* pUntrackedCount, UNALIGNED unsigned int* pVarPtrTableSize);

    bool gcIsUntrackedLocalOrNonEnregisteredArg(unsigned varNum, bool* pThisKeptAliveIsInUntracked = nullptr);

    size_t gcMakeRegPtrTable(BYTE* dest, int mask, const InfoHdr& header, unsigned codeSize, size_t* pArgTabOffset);
#else
    RegSlotMap*   m_regSlotMap;
    StackSlotMap* m_stackSlotMap;
    // This method has two modes.  In the "assign slots" mode, it figures out what registers and stack
    // locations are used to contain GC references, and whether those locations contain byrefs or pinning
    // references, building up mappings from tuples of <reg/offset X byref/pinning> to the corresponding
    // slot id (in the two member fields declared above).  In the "do work" mode, we use these slot ids to
    // actually declare live ranges to the encoder.
    void gcMakeRegPtrTable(GcInfoEncoder* gcInfoEncoder,
                           unsigned       codeSize,
                           unsigned       prologSize,
                           MakeRegPtrMode mode,
                           unsigned*      callCntRef);
#endif

#ifdef JIT32_GCENCODER
    size_t gcPtrTableSize(const InfoHdr& header, unsigned codeSize, size_t* pArgTabOffset);
    BYTE* gcPtrTableSave(BYTE* destPtr, const InfoHdr& header, unsigned codeSize, size_t* pArgTabOffset);
#endif
    void gcRegPtrSetInit();
    /*****************************************************************************/

    // This enumeration yields the result of the analysis below, whether a store
    // requires a write barrier:
    enum WriteBarrierForm
    {
        WBF_NoBarrier,                     // No barrier is required
        WBF_BarrierUnknown,                // A barrier is required, no information on checked/unchecked.
        WBF_BarrierChecked,                // A checked barrier is required.
        WBF_BarrierUnchecked,              // An unchecked barrier is required.
        WBF_NoBarrier_CheckNotHeapInDebug, // We believe that no barrier is required because the
                                           // target is not in the heap -- but in debug build use a
                                           // barrier call that verifies this property.  (Because the
                                           // target not being in the heap relies on a convention that
                                           // might accidentally be violated in the future.)
    };

    WriteBarrierForm gcIsWriteBarrierCandidate(GenTreeStoreInd* store);
    WriteBarrierForm gcWriteBarrierFormFromTargetAddress(GenTree* tgtAddr);

    bool gcIsWriteBarrierStoreIndNode(GenTreeStoreInd* store)
    {
        return gcIsWriteBarrierCandidate(store) != WBF_NoBarrier;
    }

    //-------------------------------------------------------------------------
    //
    //  These record the info about the procedure in the info-block
    //
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef JIT32_GCENCODER
private:
    BYTE* gcEpilogTable;

    unsigned gcEpilogPrevOffset;

    size_t gcInfoBlockHdrSave(BYTE*    dest,
                              int      mask,
                              unsigned methodSize,
                              unsigned prologSize,
                              unsigned epilogSize,
                              InfoHdr* header,
                              int*     s_cached);

public:
    static void gcInitEncoderLookupTable();

private:
    static size_t gcRecordEpilog(void* pCallBackData, unsigned offset);
#else // JIT32_GCENCODER
    void gcInfoBlockHdrSave(GcInfoEncoder* gcInfoEncoder, unsigned methodSize, unsigned prologSize);

#endif // JIT32_GCENCODER

#if !defined(JIT32_GCENCODER) || defined(FEATURE_EH_FUNCLETS)

    // This method expands the tracked stack variables lifetimes so that any lifetimes within filters
    // are reported as pinned.
    void gcMarkFilterVarsPinned();

    // Insert a varPtrDsc to gcVarPtrList that was generated by splitting lifetimes
    void gcInsertVarPtrDscSplit(varPtrDsc* desc, varPtrDsc* begin);

#ifdef DEBUG
    void gcDumpVarPtrDsc(varPtrDsc* desc);
#endif // DEBUG

#endif // !defined(JIT32_GCENCODER) || defined(FEATURE_EH_FUNCLETS)

#if DUMP_GC_TABLES

    void gcFindPtrsInFrame(const void* infoBlock, const void* codeBlock, unsigned offs);

#ifdef JIT32_GCENCODER
    size_t gcInfoBlockHdrDump(const BYTE* table,
                              InfoHdr*    header,      /* OUT */
                              unsigned*   methodSize); /* OUT */

    size_t gcDumpPtrTable(const BYTE* table, const InfoHdr& header, unsigned methodSize);

#endif // JIT32_GCENCODER
#endif // DUMP_GC_TABLES

public:
    // This method updates the appropriate reg masks when a variable is moved.
    void gcUpdateForRegVarMove(regMaskTP srcMask, regMaskTP dstMask, LclVarDsc* varDsc);

private:
    ReturnKind getReturnKind();
};

inline unsigned char encodeUnsigned(BYTE* dest, unsigned value)
{
    unsigned char size = 1;
    unsigned      tmp  = value;
    while (tmp > 0x7F)
    {
        tmp >>= 7;
        assert(size < 6); // Invariant.
        size++;
    }
    if (dest)
    {
        // write the bytes starting at the end of dest in LSB to MSB order
        BYTE* p    = dest + size;
        BYTE  cont = 0; // The last byte has no continuation flag
        while (value > 0x7F)
        {
            *--p = cont | (value & 0x7f);
            value >>= 7;
            cont = 0x80; // Non last bytes have a continuation flag
        }
        *--p = cont | (BYTE)value; // Now write the first byte
        assert(p == dest);
    }
    return size;
}

inline unsigned char encodeUDelta(BYTE* dest, unsigned value, unsigned lastValue)
{
    assert(value >= lastValue);
    return encodeUnsigned(dest, value - lastValue);
}

inline unsigned char encodeSigned(BYTE* dest, int val)
{
    unsigned char size  = 1;
    unsigned      value = val;
    BYTE          neg   = 0;
    if (val < 0)
    {
        value = -val;
        neg   = 0x40;
    }
    unsigned tmp = value;
    while (tmp > 0x3F)
    {
        tmp >>= 7;
        assert(size < 16); // Definitely sufficient for unsigned.  Fits in an unsigned char, certainly.
        size++;
    }
    if (dest)
    {
        // write the bytes starting at the end of dest in LSB to MSB order
        BYTE* p    = dest + size;
        BYTE  cont = 0; // The last byte has no continuation flag
        while (value > 0x3F)
        {
            *--p = cont | (value & 0x7f);
            value >>= 7;
            cont = 0x80; // Non last bytes have a continuation flag
        }
        *--p = neg | cont | (BYTE)value; // Now write the first byte
        assert(p == dest);
    }
    return size;
}

#endif // _JITGCINFO_H_
