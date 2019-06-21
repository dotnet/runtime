// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/*****************************************************************************/

#ifndef _EMIT_H_
#define _EMIT_H_

#include "instr.h"

#ifndef _GCINFO_H_
#include "gcinfo.h"
#endif

#include "jitgcinfo.h"

/*****************************************************************************/
#ifdef TRANSLATE_PDB
#ifndef _ADDRMAP_INCLUDED_
#include "addrmap.h"
#endif
#ifndef _LOCALMAP_INCLUDED_
#include "localmap.h"
#endif
#ifndef _PDBREWRITE_H_
#include "pdbrewrite.h"
#endif
#endif // TRANSLATE_PDB

/*****************************************************************************/
#ifdef _MSC_VER
#pragma warning(disable : 4200) // allow arrays of 0 size inside structs
#endif
#define TRACK_GC_TEMP_LIFETIMES 0

/*****************************************************************************/

#if 0
#define EMITVERBOSE 1
#else
#define EMITVERBOSE (emitComp->verbose)
#endif

#if 0
#define EMIT_GC_VERBOSE 0
#else
#define EMIT_GC_VERBOSE (emitComp->verbose)
#endif

#if 1
#define EMIT_INSTLIST_VERBOSE 0
#else
#define EMIT_INSTLIST_VERBOSE (emitComp->verbose)
#endif

/*****************************************************************************/

#ifdef DEBUG
#define DEBUG_EMIT 1
#else
#define DEBUG_EMIT 0
#endif

#if EMITTER_STATS
void emitterStats(FILE* fout);
void emitterStaticStats(FILE* fout); // Static stats about the emitter (data structure offsets, sizes, etc.)
#endif

void printRegMaskInt(regMaskTP mask);

/*****************************************************************************/
/* Forward declarations */

class emitLocation;
class emitter;
struct insGroup;

typedef void (*emitSplitCallbackType)(void* context, emitLocation* emitLoc);

/*****************************************************************************/

//-----------------------------------------------------------------------------

inline bool needsGC(GCtype gcType)
{
    if (gcType == GCT_NONE)
    {
        return false;
    }
    else
    {
        assert(gcType == GCT_GCREF || gcType == GCT_BYREF);
        return true;
    }
}

//-----------------------------------------------------------------------------

#ifdef DEBUG

inline bool IsValidGCtype(GCtype gcType)
{
    return (gcType == GCT_NONE || gcType == GCT_GCREF || gcType == GCT_BYREF);
}

// Get a string name to represent the GC type

inline const char* GCtypeStr(GCtype gcType)
{
    switch (gcType)
    {
        case GCT_NONE:
            return "npt";
        case GCT_GCREF:
            return "gcr";
        case GCT_BYREF:
            return "byr";
        default:
            assert(!"Invalid GCtype");
            return "err";
    }
}

#endif // DEBUG

/*****************************************************************************/

#if DEBUG_EMIT
#define INTERESTING_JUMP_NUM -1 // set to 0 to see all jump info
//#define INTERESTING_JUMP_NUM    0
#endif

/*****************************************************************************
 *
 *  Represent an emitter location.
 */

class emitLocation
{
public:
    emitLocation() : ig(nullptr), codePos(0)
    {
    }

    emitLocation(insGroup* _ig) : ig(_ig), codePos(0)
    {
    }

    emitLocation(void* emitCookie) : ig((insGroup*)emitCookie), codePos(0)
    {
    }

    // A constructor for code that needs to call it explicitly.
    void Init()
    {
        *this = emitLocation();
    }

    void CaptureLocation(emitter* emit);

    bool IsCurrentLocation(emitter* emit) const;

    // This function is highly suspect, since it presumes knowledge of the codePos "cookie",
    // and doesn't look at the 'ig' pointer.
    bool IsOffsetZero() const
    {
        return (codePos == 0);
    }

    UNATIVE_OFFSET CodeOffset(emitter* emit) const;

    insGroup* GetIG() const
    {
        return ig;
    }

    int GetInsNum() const;

    bool operator!=(const emitLocation& other) const
    {
        return (ig != other.ig) || (codePos != other.codePos);
    }

    bool operator==(const emitLocation& other) const
    {
        return !(*this != other);
    }

    bool Valid() const
    {
        // Things we could validate:
        //   1. the instruction group pointer is non-nullptr.
        //   2. 'ig' is a legal pointer to an instruction group.
        //   3. 'codePos' is a legal offset into 'ig'.
        // Currently, we just do #1.
        // #2 and #3 should only be done in DEBUG, if they are implemented.

        if (ig == nullptr)
        {
            return false;
        }

        return true;
    }

    UNATIVE_OFFSET GetFuncletPrologOffset(emitter* emit) const;

#ifdef DEBUG
    void Print() const;
#endif // DEBUG

private:
    insGroup* ig;      // the instruction group
    unsigned  codePos; // the code position within the IG (see emitCurOffset())
};

enum class emitDataAlignment
{
    None,
    Preferred,
    Required
};

/************************************************************************/
/*          The following describes an instruction group                */
/************************************************************************/

enum insGroupPlaceholderType : unsigned char
{
    IGPT_PROLOG, // currently unused
    IGPT_EPILOG,
#if FEATURE_EH_FUNCLETS
    IGPT_FUNCLET_PROLOG,
    IGPT_FUNCLET_EPILOG,
#endif // FEATURE_EH_FUNCLETS
};

#if defined(_MSC_VER) && defined(_TARGET_ARM_)
// ARM aligns structures that contain 64-bit ints or doubles on 64-bit boundaries. This causes unwanted
// padding to be added to the end, so sizeof() is unnecessarily big.
#pragma pack(push)
#pragma pack(4)
#endif // defined(_MSC_VER) && defined(_TARGET_ARM_)

struct insPlaceholderGroupData
{
    insGroup*               igPhNext;
    BasicBlock*             igPhBB;
    VARSET_TP               igPhInitGCrefVars;
    regMaskTP               igPhInitGCrefRegs;
    regMaskTP               igPhInitByrefRegs;
    VARSET_TP               igPhPrevGCrefVars;
    regMaskTP               igPhPrevGCrefRegs;
    regMaskTP               igPhPrevByrefRegs;
    insGroupPlaceholderType igPhType;
}; // end of struct insPlaceholderGroupData

struct insGroup
{
    insGroup* igNext;

#ifdef DEBUG
    insGroup* igSelf; // for consistency checking
#endif

    UNATIVE_OFFSET igNum;     // for ordering (and display) purposes
    UNATIVE_OFFSET igOffs;    // offset of this group within method
    unsigned int   igFuncIdx; // Which function/funclet does this belong to? (Index into Compiler::compFuncInfos array.)
    unsigned short igFlags;   // see IGF_xxx below
    unsigned short igSize;    // # of bytes of code in this group

#define IGF_GC_VARS 0x0001    // new set of live GC ref variables
#define IGF_BYREF_REGS 0x0002 // new set of live by-ref registers
#if FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)
#define IGF_FINALLY_TARGET 0x0004 // this group is the start of a basic block that is returned to after a finally.
#endif                            // FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)
#define IGF_FUNCLET_PROLOG 0x0008 // this group belongs to a funclet prolog
#define IGF_FUNCLET_EPILOG 0x0010 // this group belongs to a funclet epilog.
#define IGF_EPILOG 0x0020         // this group belongs to a main function epilog
#define IGF_NOGCINTERRUPT 0x0040  // this IG is is a no-interrupt region (prolog, epilog, etc.)
#define IGF_UPD_ISZ 0x0080        // some instruction sizes updated
#define IGF_PLACEHOLDER 0x0100    // this is a placeholder group, to be filled in later
#define IGF_EMIT_ADD 0x0200       // this is a block added by the emitter
                                  // because the codegen block was too big. Also used for
                                  // placeholder IGs that aren't also labels.

// Mask of IGF_* flags that should be propagated to new blocks when they are created.
// This allows prologs and epilogs to be any number of IGs, but still be
// automatically marked properly.
#if FEATURE_EH_FUNCLETS
#ifdef DEBUG
#define IGF_PROPAGATE_MASK (IGF_EPILOG | IGF_FUNCLET_PROLOG | IGF_FUNCLET_EPILOG)
#else // DEBUG
#define IGF_PROPAGATE_MASK (IGF_EPILOG | IGF_FUNCLET_PROLOG)
#endif // DEBUG
#else  // FEATURE_EH_FUNCLETS
#define IGF_PROPAGATE_MASK (IGF_EPILOG)
#endif // FEATURE_EH_FUNCLETS

    // Try to do better packing based on how large regMaskSmall is (8, 16, or 64 bits).
    CLANG_FORMAT_COMMENT_ANCHOR;
#if REGMASK_BITS <= 32

    union {
        BYTE*                    igData;   // addr of instruction descriptors
        insPlaceholderGroupData* igPhData; // when igFlags & IGF_PLACEHOLDER
    };

#if EMIT_TRACK_STACK_DEPTH
    unsigned igStkLvl; // stack level on entry
#endif
    regMaskSmall  igGCregs; // set of registers with live GC refs
    unsigned char igInsCnt; // # of instructions  in this group

#else // REGMASK_BITS

    regMaskSmall igGCregs; // set of registers with live GC refs

    union {
        BYTE*                    igData;   // addr of instruction descriptors
        insPlaceholderGroupData* igPhData; // when igFlags & IGF_PLACEHOLDER
    };

#if EMIT_TRACK_STACK_DEPTH
    unsigned igStkLvl; // stack level on entry
#endif

    unsigned char igInsCnt; // # of instructions  in this group

#endif // REGMASK_BITS

    VARSET_VALRET_TP igGCvars() const
    {
        assert(igFlags & IGF_GC_VARS);

        BYTE* ptr = (BYTE*)igData;
        ptr -= sizeof(VARSET_TP);

        return *(VARSET_TP*)ptr;
    }

    unsigned igByrefRegs() const
    {
        assert(igFlags & IGF_BYREF_REGS);

        BYTE* ptr = (BYTE*)igData;

        if (igFlags & IGF_GC_VARS)
        {
            ptr -= sizeof(VARSET_TP);
        }

        ptr -= sizeof(unsigned);

        return *(unsigned*)ptr;
    }

}; // end of struct insGroup

//  For AMD64 the maximum prolog/epilog size supported on the OS is 256 bytes
//  Since it is incorrect for us to be jumping across funclet prolog/epilogs
//  we will use the following estimate as the maximum placeholder size.
//
#define MAX_PLACEHOLDER_IG_SIZE 256

#if defined(_MSC_VER) && defined(_TARGET_ARM_)
#pragma pack(pop)
#endif // defined(_MSC_VER) && defined(_TARGET_ARM_)

/*****************************************************************************/

#define DEFINE_ID_OPS
#include "emitfmts.h"
#undef DEFINE_ID_OPS

enum LclVarAddrTag
{
    LVA_STANDARD_ENCODING = 0,
    LVA_LARGE_OFFSET      = 1,
    LVA_COMPILER_TEMP     = 2,
    LVA_LARGE_VARNUM      = 3
};

struct emitLclVarAddr
{
    // Constructor
    void initLclVarAddr(int varNum, unsigned offset);

    int lvaVarNum(); // Returns the variable to access. Note that it returns a negative number for compiler spill temps.
    unsigned lvaOffset(); // returns the offset into the variable to access

    // This struct should be 32 bits in size for the release build.
    // We have this constraint because this type is used in a union
    // with several other pointer sized types in the instrDesc struct.
    //
protected:
    unsigned _lvaVarNum : 15; // Usually the lvaVarNum
    unsigned _lvaExtra : 15;  // Usually the lvaOffset
    unsigned _lvaTag : 2;     // tag field to support larger varnums
};

enum idAddrUnionTag
{
    iaut_ALIGNED_POINTER = 0x0,
    iaut_DATA_OFFSET     = 0x1,
    iaut_INST_COUNT      = 0x2,
    iaut_UNUSED_TAG      = 0x3,

    iaut_MASK  = 0x3,
    iaut_SHIFT = 2
};

class emitter
{
    friend class emitLocation;
    friend class Compiler;
    friend class CodeGen;
    friend class CodeGenInterface;

public:
    /*************************************************************************
     *
     *  Define the public entry points.
     */

    // Constructor.
    emitter()
    {
#ifdef DEBUG
        // There seem to be some cases where this is used without being initialized via CodeGen::inst_set_SV_var().
        emitVarRefOffs = 0;
#endif // DEBUG

#ifdef _TARGET_XARCH_
        SetUseVEXEncoding(false);
#endif // _TARGET_XARCH_

        emitDataSecCur = nullptr;
    }

#include "emitpub.h"

protected:
    /************************************************************************/
    /*                        Miscellaneous stuff                           */
    /************************************************************************/

    Compiler* emitComp;
    GCInfo*   gcInfo;
    CodeGen*  codeGen;

    typedef GCInfo::varPtrDsc varPtrDsc;
    typedef GCInfo::regPtrDsc regPtrDsc;
    typedef GCInfo::CallDsc   callDsc;

    void* emitGetMem(size_t sz);

    enum opSize : unsigned
    {
        OPSZ1      = 0,
        OPSZ2      = 1,
        OPSZ4      = 2,
        OPSZ8      = 3,
        OPSZ16     = 4,
        OPSZ32     = 5,
        OPSZ_COUNT = 6,
#ifdef _TARGET_AMD64_
        OPSZP = OPSZ8,
#else
        OPSZP = OPSZ4,
#endif
    };

#define OPSIZE_INVALID ((opSize)0xffff)

    static const emitter::opSize emitSizeEncode[];
    static const emitAttr        emitSizeDecode[];

    static emitter::opSize emitEncodeSize(emitAttr size);
    static emitAttr emitDecodeSize(emitter::opSize ensz);

    // Currently, we only allow one IG for the prolog
    bool emitIGisInProlog(const insGroup* ig)
    {
        return ig == emitPrologIG;
    }

    bool emitIGisInEpilog(const insGroup* ig)
    {
        return (ig != nullptr) && ((ig->igFlags & IGF_EPILOG) != 0);
    }

#if FEATURE_EH_FUNCLETS

    bool emitIGisInFuncletProlog(const insGroup* ig)
    {
        return (ig != nullptr) && ((ig->igFlags & IGF_FUNCLET_PROLOG) != 0);
    }

    bool emitIGisInFuncletEpilog(const insGroup* ig)
    {
        return (ig != nullptr) && ((ig->igFlags & IGF_FUNCLET_EPILOG) != 0);
    }

#endif // FEATURE_EH_FUNCLETS

    // If "ig" corresponds to the start of a basic block that is the
    // target of a funclet return, generate GC information for it's start
    // address "cp", as if it were the return address of a call.
    void emitGenGCInfoIfFuncletRetTarget(insGroup* ig, BYTE* cp);

    void emitRecomputeIGoffsets();

    /************************************************************************/
    /*          The following describes a single instruction                */
    /************************************************************************/

    enum insFormat : unsigned
    {
#define IF_DEF(en, op1, op2) IF_##en,
#include "emitfmts.h"

        IF_COUNT
    };

#define AM_DISP_BITS ((sizeof(unsigned) * 8) - 2 * (REGNUM_BITS + 1) - 2)
#define AM_DISP_BIG_VAL (-(1 << (AM_DISP_BITS - 1)))
#define AM_DISP_MIN (-((1 << (AM_DISP_BITS - 1)) - 1))
#define AM_DISP_MAX (+((1 << (AM_DISP_BITS - 1)) - 1))

    struct emitAddrMode
    {
        regNumber       amBaseReg : REGNUM_BITS + 1;
        regNumber       amIndxReg : REGNUM_BITS + 1;
        emitter::opSize amScale : 2;
        int             amDisp : AM_DISP_BITS;
    };

#ifdef DEBUG // This information is used in DEBUG builds to display the method name for call instructions

    struct instrDesc;

    struct instrDescDebugInfo
    {
        unsigned idNum;
        size_t   idSize;       // size of the instruction descriptor
        unsigned idVarRefOffs; // IL offset for LclVar reference
        size_t   idMemCookie;  // for display of method name  (also used by switch table)
#ifdef TRANSLATE_PDB
        unsigned int idilStart; // instruction descriptor source information for PDB translation
#endif
        bool              idFinallyCall; // Branch instruction is a call to finally
        bool              idCatchRet;    // Instruction is for a catch 'return'
        CORINFO_SIG_INFO* idCallSig;     // Used to report native call site signatures to the EE
    };

#endif // DEBUG

#ifdef _TARGET_ARM_
    unsigned insEncodeSetFlags(insFlags sf);

    enum insSize : unsigned
    {
        ISZ_16BIT,
        ISZ_32BIT,
        ISZ_48BIT // pseudo-instruction for conditional branch with imm24 range,
                  // encoded as IT of condition followed by an unconditional branch
    };

    unsigned insEncodeShiftOpts(insOpts opt);
    unsigned insEncodePUW_G0(insOpts opt, int imm);
    unsigned insEncodePUW_H0(insOpts opt, int imm);

#endif // _TARGET_ARM_

    struct instrDescCns;

    struct instrDesc
    {
    private:
// The assembly instruction
#if defined(_TARGET_XARCH_)
        static_assert_no_msg(INS_count <= 1024);
        instruction _idIns : 10;
#elif defined(_TARGET_ARM64_)
        static_assert_no_msg(INS_count <= 512);
        instruction _idIns : 9;
#else  // !(defined(_TARGET_XARCH_) || defined(_TARGET_ARM64_))
        static_assert_no_msg(INS_count <= 256);
        instruction _idIns : 8;
#endif // !(defined(_TARGET_XARCH_) || defined(_TARGET_ARM64_))

// The format for the instruction
#if defined(_TARGET_XARCH_)
        static_assert_no_msg(IF_COUNT <= 128);
        insFormat _idInsFmt : 7;
#else
        static_assert_no_msg(IF_COUNT <= 256);
        insFormat _idInsFmt : 8;
#endif

    public:
        instruction idIns() const
        {
            return _idIns;
        }
        void idIns(instruction ins)
        {
            assert((ins != INS_invalid) && (ins < INS_count));
            _idIns = ins;
        }

        insFormat idInsFmt() const
        {
            return _idInsFmt;
        }
        void idInsFmt(insFormat insFmt)
        {
#if defined(_TARGET_ARM64_)
            noway_assert(insFmt != IF_NONE); // Only the x86 emitter uses IF_NONE, it is invalid for ARM64 (and ARM32)
#endif
            assert(insFmt < IF_COUNT);
            _idInsFmt = insFmt;
        }

        void idSetRelocFlags(emitAttr attr)
        {
            _idCnsReloc = (EA_IS_CNS_RELOC(attr) ? 1 : 0);
            _idDspReloc = (EA_IS_DSP_RELOC(attr) ? 1 : 0);
        }

        ////////////////////////////////////////////////////////////////////////
        // Space taken up to here:
        // x86:   17 bits
        // amd64: 17 bits
        // arm:   16 bits
        // arm64: 17 bits

    private:
#if defined(_TARGET_XARCH_)
        unsigned _idCodeSize : 4; // size of instruction in bytes. Max size of an Intel instruction is 15 bytes.
        opSize   _idOpSize : 3;   // operand size: 0=1 , 1=2 , 2=4 , 3=8, 4=16, 5=32
                                  // At this point we have fully consumed first DWORD so that next field
                                  // doesn't cross a byte boundary.
#elif defined(_TARGET_ARM64_)
// Moved the definition of '_idOpSize' later so that we don't cross a 32-bit boundary when laying out bitfields
#else  // ARM
        opSize      _idOpSize : 2; // operand size: 0=1 , 1=2 , 2=4 , 3=8
#endif // ARM

        // On Amd64, this is where the second DWORD begins
        // On System V a call could return a struct in 2 registers. The instrDescCGCA struct below has  member that
        // stores the GC-ness of the second register.
        // It is added to the instrDescCGCA and not here (the base struct) since it is not needed by all the
        // instructions. This struct (instrDesc) is very carefully kept to be no more than 128 bytes. There is no more
        // space to add members for keeping GC-ness of the second return registers. It will also bloat the base struct
        // unnecessarily since the GC-ness of the second register is only needed for call instructions.
        // The instrDescCGCA struct's member keeping the GC-ness of the first return register is _idcSecondRetRegGCType.
        GCtype _idGCref : 2; // GCref operand? (value is a "GCtype")

        // The idReg1 and idReg2 fields hold the first and second register
        // operand(s), whenever these are present. Note that currently the
        // size of these fields is 6 bits on all targets, and care needs to
        // be taken to make sure all of these fields stay reasonably packed.

        // Note that we use the _idReg1 and _idReg2 fields to hold
        // the live gcrefReg mask for the call instructions on x86/x64
        //
        regNumber _idReg1 : REGNUM_BITS; // register num

        regNumber _idReg2 : REGNUM_BITS;

        ////////////////////////////////////////////////////////////////////////
        // Space taken up to here:
        // x86:   38 bits
        // amd64: 38 bits
        // arm:   32 bits
        // arm64: 31 bits
        CLANG_FORMAT_COMMENT_ANCHOR;

        unsigned _idSmallDsc : 1;  // is this a "small" descriptor?
        unsigned _idLargeCns : 1;  // does a large constant     follow?
        unsigned _idLargeDsp : 1;  // does a large displacement follow?
        unsigned _idLargeCall : 1; // large call descriptor used

        unsigned _idBound : 1;      // jump target / frame offset bound
        unsigned _idCallRegPtr : 1; // IL indirect calls: addr in reg
        unsigned _idCallAddr : 1;   // IL indirect calls: can make a direct call to iiaAddr
        unsigned _idNoGC : 1;       // Some helpers don't get recorded in GC tables

#ifdef _TARGET_ARM64_
        opSize   _idOpSize : 3; // operand size: 0=1 , 1=2 , 2=4 , 3=8, 4=16
        insOpts  _idInsOpt : 6; // options for instructions
        unsigned _idLclVar : 1; // access a local on stack
#endif

#ifdef _TARGET_ARM_
        insSize  _idInsSize : 2;   // size of instruction: 16, 32 or 48 bits
        insFlags _idInsFlags : 1;  // will this instruction set the flags
        unsigned _idLclVar : 1;    // access a local on stack
        unsigned _idLclFPBase : 1; // access a local on stack - SP based offset
        insOpts  _idInsOpt : 3;    // options for Load/Store instructions

// For arm we have used 16 bits
#define ID_EXTRA_BITFIELD_BITS (16)

#elif defined(_TARGET_ARM64_)
// For Arm64, we have used 17 bits from the second DWORD.
#define ID_EXTRA_BITFIELD_BITS (17)
#elif defined(_TARGET_XARCH_)
                                   // For xarch, we have used 14 bits from the second DWORD.
#define ID_EXTRA_BITFIELD_BITS (14)
#else
#error Unsupported or unset target architecture
#endif

        ////////////////////////////////////////////////////////////////////////
        // Space taken up to here:
        // x86:   46 bits
        // amd64: 46 bits
        // arm:   48 bits
        // arm64: 49 bits

        unsigned _idCnsReloc : 1; // LargeCns is an RVA and needs reloc tag
        unsigned _idDspReloc : 1; // LargeDsp is an RVA and needs reloc tag

#define ID_EXTRA_RELOC_BITS (2)

        ////////////////////////////////////////////////////////////////////////
        // Space taken up to here:
        // x86:   48 bits
        // amd64: 48 bits
        // arm:   50 bits
        // arm64: 51 bits
        CLANG_FORMAT_COMMENT_ANCHOR;

#define ID_EXTRA_BITS (ID_EXTRA_RELOC_BITS + ID_EXTRA_BITFIELD_BITS)

/* Use whatever bits are left over for small constants */

#define ID_BIT_SMALL_CNS (32 - ID_EXTRA_BITS)
#define ID_MIN_SMALL_CNS 0
#define ID_MAX_SMALL_CNS (int)((1 << ID_BIT_SMALL_CNS) - 1U)

        ////////////////////////////////////////////////////////////////////////
        // Small constant size:
        // x86:   16 bits
        // amd64: 16 bits
        // arm:   14 bits
        // arm64: 13 bits

        unsigned _idSmallCns : ID_BIT_SMALL_CNS;

        ////////////////////////////////////////////////////////////////////////
        // Space taken up to here: 64 bits, all architectures, by design.
        ////////////////////////////////////////////////////////////////////////
        CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG

        instrDescDebugInfo* _idDebugOnlyInfo;

    public:
        instrDescDebugInfo* idDebugOnlyInfo() const
        {
            return _idDebugOnlyInfo;
        }
        void idDebugOnlyInfo(instrDescDebugInfo* info)
        {
            _idDebugOnlyInfo = info;
        }

    private:
#endif // DEBUG

        CLANG_FORMAT_COMMENT_ANCHOR;

//
// This is the end of the 'small' instrDesc which is the same on all
//   platforms (except 64-bit DEBUG which is a little bigger).
// Non-DEBUG sizes:
//   x86/amd64/arm/arm64: 64 bits
// DEBUG sizes (includes one pointer):
//   x86:   2 DWORDs, 96 bits
//   amd64: 4 DWORDs, 128 bits
//   arm:   3 DWORDs, 96 bits
//   arm64: 4 DWORDs, 128 bits
// There should no padding or alignment issues on any platform or
//   configuration (including DEBUG which has 1 extra pointer).
//

/*
    If you add lots more fields that need to be cleared (such
    as various flags), you might need to update the body of
    emitter::emitAllocInstr() to clear them.
 */

#if DEBUG
#define SMALL_IDSC_DEBUG_EXTRA (sizeof(void*))
#else
#define SMALL_IDSC_DEBUG_EXTRA (0)
#endif

#define SMALL_IDSC_SIZE (8 + SMALL_IDSC_DEBUG_EXTRA)

        void checkSizes();

        union idAddrUnion {
// TODO-Cleanup: We should really add a DEBUG-only tag to this union so we can add asserts
// about reading what we think is here, to avoid unexpected corruption issues.

#ifndef _TARGET_ARM64_
            emitLclVarAddr iiaLclVar;
#endif
            BasicBlock*  iiaBBlabel;
            insGroup*    iiaIGlabel;
            BYTE*        iiaAddr;
            emitAddrMode iiaAddrMode;

            CORINFO_FIELD_HANDLE iiaFieldHnd; // iiaFieldHandle is also used to encode
                                              // an offset into the JIT data constant area
            bool iiaIsJitDataOffset() const;
            int  iiaGetJitDataOffset() const;

#ifdef _TARGET_ARMARCH_

            // iiaEncodedInstrCount and its accessor functions are used to specify an instruction
            // count for jumps, instead of using a label and multiple blocks. This is used in the
            // prolog as well as for IF_LARGEJMP pseudo-branch instructions.
            int iiaEncodedInstrCount;

            bool iiaHasInstrCount() const
            {
                return (iiaEncodedInstrCount & iaut_MASK) == iaut_INST_COUNT;
            }
            int iiaGetInstrCount() const
            {
                assert(iiaHasInstrCount());
                return (iiaEncodedInstrCount >> iaut_SHIFT);
            }
            void iiaSetInstrCount(int count)
            {
                assert(abs(count) < 10);
                iiaEncodedInstrCount = (count << iaut_SHIFT) | iaut_INST_COUNT;
            }

            struct
            {
#ifdef _TARGET_ARM64_
                // For 64-bit architecture this 32-bit structure can pack with these unsigned bit fields
                emitLclVarAddr iiaLclVar;
                unsigned       _idReg3Scaled : 1; // Reg3 is scaled by idOpSize bits
                GCtype         _idGCref2 : 2;
#endif
                regNumber _idReg3 : REGNUM_BITS;
                regNumber _idReg4 : REGNUM_BITS;
            };
#elif defined(_TARGET_XARCH_)
            struct
            {
                regNumber _idReg3 : REGNUM_BITS;
                regNumber _idReg4 : REGNUM_BITS;
            };
#endif // defined(_TARGET_XARCH_)

        } _idAddrUnion;

        /* Trivial wrappers to return properly typed enums */
    public:
        bool idIsSmallDsc() const
        {
            return (_idSmallDsc != 0);
        }
        void idSetIsSmallDsc()
        {
            _idSmallDsc = 1;
        }

#if defined(_TARGET_XARCH_)

        unsigned idCodeSize() const
        {
            return _idCodeSize;
        }
        void idCodeSize(unsigned sz)
        {
            if (sz > 15)
            {
                // This is a temporary workaround for non-precise instr size
                // estimator on XARCH. It often overestimates sizes and can
                // return value more than 15 that doesn't fit in 4 bits _idCodeSize.
                // If somehow we generate instruction that needs more than 15 bytes we
                // will fail on another assert in emit.cpp: noway_assert(id->idCodeSize() >= csz).
                // Issue https://github.com/dotnet/coreclr/issues/25050.
                sz = 15;
            }
            assert(sz <= 15); // Intel decoder limit.
            _idCodeSize = sz;
            assert(sz == _idCodeSize);
        }

#elif defined(_TARGET_ARM64_)
        unsigned idCodeSize() const
        {
            int size = 4;
            switch (idInsFmt())
            {
                case IF_LARGEADR:
                // adrp + add
                case IF_LARGEJMP:
                    // b<cond> + b<uncond>
                    size = 8;
                    break;
                case IF_LARGELDC:
                    if (isVectorRegister(idReg1()))
                    {
                        // adrp + ldr + fmov
                        size = 12;
                    }
                    else
                    {
                        // adrp + ldr
                        size = 8;
                    }
                    break;
                default:
                    break;
            }

            return size;
        }

#elif defined(_TARGET_ARM_)

        bool idInstrIsT1() const
        {
            return (_idInsSize == ISZ_16BIT);
        }
        unsigned idCodeSize() const
        {
            unsigned result = (_idInsSize == ISZ_16BIT) ? 2 : (_idInsSize == ISZ_32BIT) ? 4 : 6;
            return result;
        }
        insSize idInsSize() const
        {
            return _idInsSize;
        }
        void idInsSize(insSize isz)
        {
            _idInsSize = isz;
            assert(isz == _idInsSize);
        }
        insFlags idInsFlags() const
        {
            return _idInsFlags;
        }
        void idInsFlags(insFlags sf)
        {
            _idInsFlags = sf;
            assert(sf == _idInsFlags);
        }
#endif // _TARGET_ARM_

        emitAttr idOpSize()
        {
            return emitDecodeSize(_idOpSize);
        }
        void idOpSize(emitAttr opsz)
        {
            _idOpSize = emitEncodeSize(opsz);
        }

        GCtype idGCref() const
        {
            return (GCtype)_idGCref;
        }
        void idGCref(GCtype gctype)
        {
            _idGCref = gctype;
        }

        regNumber idReg1() const
        {
            return _idReg1;
        }
        void idReg1(regNumber reg)
        {
            _idReg1 = reg;
            assert(reg == _idReg1);
        }

#ifdef _TARGET_ARM64_
        GCtype idGCrefReg2() const
        {
            assert(!idIsSmallDsc());
            return (GCtype)idAddr()->_idGCref2;
        }
        void idGCrefReg2(GCtype gctype)
        {
            assert(!idIsSmallDsc());
            idAddr()->_idGCref2 = gctype;
        }
#endif // _TARGET_ARM64_

        regNumber idReg2() const
        {
            return _idReg2;
        }
        void idReg2(regNumber reg)
        {
            _idReg2 = reg;
            assert(reg == _idReg2);
        }

#if defined(_TARGET_XARCH_)
        regNumber idReg3() const
        {
            assert(!idIsSmallDsc());
            return idAddr()->_idReg3;
        }
        void idReg3(regNumber reg)
        {
            assert(!idIsSmallDsc());
            idAddr()->_idReg3 = reg;
            assert(reg == idAddr()->_idReg3);
        }
        regNumber idReg4() const
        {
            assert(!idIsSmallDsc());
            return idAddr()->_idReg4;
        }
        void idReg4(regNumber reg)
        {
            assert(!idIsSmallDsc());
            idAddr()->_idReg4 = reg;
            assert(reg == idAddr()->_idReg4);
        }
#endif // defined(_TARGET_XARCH_)
#ifdef _TARGET_ARMARCH_
        insOpts idInsOpt() const
        {
            return (insOpts)_idInsOpt;
        }
        void idInsOpt(insOpts opt)
        {
            _idInsOpt = opt;
            assert(opt == _idInsOpt);
        }

        regNumber idReg3() const
        {
            assert(!idIsSmallDsc());
            return idAddr()->_idReg3;
        }
        void idReg3(regNumber reg)
        {
            assert(!idIsSmallDsc());
            idAddr()->_idReg3 = reg;
            assert(reg == idAddr()->_idReg3);
        }
        regNumber idReg4() const
        {
            assert(!idIsSmallDsc());
            return idAddr()->_idReg4;
        }
        void idReg4(regNumber reg)
        {
            assert(!idIsSmallDsc());
            idAddr()->_idReg4 = reg;
            assert(reg == idAddr()->_idReg4);
        }
#ifdef _TARGET_ARM64_
        bool idReg3Scaled() const
        {
            assert(!idIsSmallDsc());
            return (idAddr()->_idReg3Scaled == 1);
        }
        void idReg3Scaled(bool val)
        {
            assert(!idIsSmallDsc());
            idAddr()->_idReg3Scaled = val ? 1 : 0;
        }
#endif // _TARGET_ARM64_

#endif // _TARGET_ARMARCH_

        inline static bool fitsInSmallCns(ssize_t val)
        {
            return ((val >= ID_MIN_SMALL_CNS) && (val <= ID_MAX_SMALL_CNS));
        }

        bool idIsLargeCns() const
        {
            return _idLargeCns != 0;
        }
        void idSetIsLargeCns()
        {
            _idLargeCns = 1;
        }

        bool idIsLargeDsp() const
        {
            return _idLargeDsp != 0;
        }
        void idSetIsLargeDsp()
        {
            _idLargeDsp = 1;
        }
        void idSetIsSmallDsp()
        {
            _idLargeDsp = 0;
        }

        bool idIsLargeCall() const
        {
            return _idLargeCall != 0;
        }
        void idSetIsLargeCall()
        {
            _idLargeCall = 1;
        }

        bool idIsBound() const
        {
            return _idBound != 0;
        }
        void idSetIsBound()
        {
            _idBound = 1;
        }

        bool idIsCallRegPtr() const
        {
            return _idCallRegPtr != 0;
        }
        void idSetIsCallRegPtr()
        {
            _idCallRegPtr = 1;
        }

        bool idIsCallAddr() const
        {
            return _idCallAddr != 0;
        }
        void idSetIsCallAddr()
        {
            _idCallAddr = 1;
        }

        // Only call instructions that call helper functions may be marked as "IsNoGC", indicating
        // that a thread executing such a call cannot be stopped for GC.  Thus, in partially-interruptible
        // code, it is not necessary to generate GC info for a call so labeled.
        bool idIsNoGC() const
        {
            return _idNoGC != 0;
        }
        void idSetIsNoGC(bool val)
        {
            _idNoGC = val;
        }

#ifdef _TARGET_ARMARCH_
        bool idIsLclVar() const
        {
            return _idLclVar != 0;
        }
        void idSetIsLclVar()
        {
            _idLclVar = 1;
        }
#endif // _TARGET_ARMARCH_

#if defined(_TARGET_ARM_)
        bool idIsLclFPBase() const
        {
            return _idLclFPBase != 0;
        }
        void idSetIsLclFPBase()
        {
            _idLclFPBase = 1;
        }
#endif // defined(_TARGET_ARM_)

        bool idIsCnsReloc() const
        {
            return _idCnsReloc != 0;
        }
        void idSetIsCnsReloc()
        {
            _idCnsReloc = 1;
        }

        bool idIsDspReloc() const
        {
            return _idDspReloc != 0;
        }
        void idSetIsDspReloc(bool val = true)
        {
            _idDspReloc = val;
        }
        bool idIsReloc()
        {
            return idIsDspReloc() || idIsCnsReloc();
        }

        unsigned idSmallCns() const
        {
            return _idSmallCns;
        }
        void idSmallCns(size_t value)
        {
            assert(fitsInSmallCns(value));
            _idSmallCns = value;
        }

        inline const idAddrUnion* idAddr() const
        {
            assert(!idIsSmallDsc());
            return &this->_idAddrUnion;
        }

        inline idAddrUnion* idAddr()
        {
            assert(!idIsSmallDsc());
            return &this->_idAddrUnion;
        }
    }; // End of  struct instrDesc

    void dispIns(instrDesc* id);

    void appendToCurIG(instrDesc* id);

    /********************************************************************************************/

    struct instrDescJmp : instrDesc
    {
        instrDescJmp* idjNext; // next jump in the group/method
        insGroup*     idjIG;   // containing group

        union {
            BYTE* idjAddr; // address of jump ins (for patching)
        } idjTemp;

        unsigned idjOffs : 30;    // Before jump emission, this is the byte offset within IG of the jump instruction.
                                  // After emission, for forward jumps, this is the target offset -- in bytes from the
                                  // beginning of the function -- of the target instruction of the jump, used to
                                  // determine if this jump needs to be patched.
        unsigned idjShort : 1;    // is the jump known to be a short  one?
        unsigned idjKeepLong : 1; // should the jump be kept long? (used for
                                  // hot to cold and cold to hot jumps)
    };

#if !defined(_TARGET_ARM64_) // This shouldn't be needed for ARM32, either, but I don't want to touch the ARM32 JIT.
    struct instrDescLbl : instrDescJmp
    {
        emitLclVarAddr dstLclVar;
    };
#endif // !_TARGET_ARM64_

    struct instrDescCns : instrDesc // large const
    {
        target_ssize_t idcCnsVal;
    };

    struct instrDescDsp : instrDesc // large displacement
    {
        target_ssize_t iddDspVal;
    };

    struct instrDescCnsDsp : instrDesc // large cons + disp
    {
        target_ssize_t iddcCnsVal;
        int            iddcDspVal;
    };

#ifdef _TARGET_XARCH_

    struct instrDescAmd : instrDesc // large addrmode disp
    {
        ssize_t idaAmdVal;
    };

    struct instrDescCnsAmd : instrDesc // large cons + addrmode disp
    {
        ssize_t idacCnsVal;
        ssize_t idacAmdVal;
    };

#endif // _TARGET_XARCH_

    struct instrDescCGCA : instrDesc // call with ...
    {
        VARSET_TP idcGCvars;    // ... updated GC vars or
        ssize_t   idcDisp;      // ... big addrmode disp
        regMaskTP idcGcrefRegs; // ... gcref registers
        regMaskTP idcByrefRegs; // ... byref registers
        unsigned  idcArgCnt;    // ... lots of args or (<0 ==> caller pops args)

#if MULTIREG_HAS_SECOND_GC_RET
        // This method handle the GC-ness of the second register in a 2 register returned struct on System V.
        GCtype idSecondGCref() const
        {
            return (GCtype)_idcSecondRetRegGCType;
        }
        void idSecondGCref(GCtype gctype)
        {
            _idcSecondRetRegGCType = gctype;
        }

    private:
        // This member stores the GC-ness of the second register in a 2 register returned struct on System V.
        // It is added to the call struct since it is not needed by the base instrDesc struct, which keeps GC-ness
        // of the first register for the instCall nodes.
        // The base instrDesc is very carefully kept to be no more than 128 bytes. There is no more space to add members
        // for keeping GC-ness of the second return registers. It will also bloat the base struct unnecessarily
        // since the GC-ness of the second register is only needed for call instructions.
        // The base struct's member keeping the GC-ness of the first return register is _idGCref.
        GCtype _idcSecondRetRegGCType : 2; // ... GC type for the second return register.
#endif                                     // MULTIREG_HAS_SECOND_GC_RET
    };

#ifdef _TARGET_ARM_

    struct instrDescReloc : instrDesc
    {
        BYTE* idrRelocVal;
    };

    BYTE* emitGetInsRelocValue(instrDesc* id);

#endif // _TARGET_ARM_

    insUpdateModes emitInsUpdateMode(instruction ins);
    insFormat emitInsModeFormat(instruction ins, insFormat base);

    static const BYTE emitInsModeFmtTab[];
#ifdef DEBUG
    static const unsigned emitInsModeFmtCnt;
#endif

    size_t emitGetInstrDescSize(const instrDesc* id);
    size_t emitGetInstrDescSizeSC(const instrDesc* id);

#ifdef _TARGET_XARCH_

    ssize_t emitGetInsCns(instrDesc* id);
    ssize_t emitGetInsDsp(instrDesc* id);
    ssize_t emitGetInsAmd(instrDesc* id);

    ssize_t emitGetInsCIdisp(instrDesc* id);
    unsigned emitGetInsCIargs(instrDesc* id);

    // Return the argument count for a direct call "id".
    int emitGetInsCDinfo(instrDesc* id);

#endif // _TARGET_XARCH_

    target_ssize_t emitGetInsSC(instrDesc* id);
    unsigned emitInsCount;

/************************************************************************/
/*           A few routines used for debug display purposes             */
/************************************************************************/

#if defined(DEBUG) || EMITTER_STATS

    static const char* emitIfName(unsigned f);

#endif // defined(DEBUG) || EMITTER_STATS

#ifdef DEBUG

    unsigned emitVarRefOffs;

    const char* emitRegName(regNumber reg, emitAttr size = EA_PTRSIZE, bool varName = true);
    const char* emitFloatRegName(regNumber reg, emitAttr size = EA_PTRSIZE, bool varName = true);

    const char* emitFldName(CORINFO_FIELD_HANDLE fieldVal);
    const char* emitFncName(CORINFO_METHOD_HANDLE callVal);

    void emitDispIGflags(unsigned flags);
    void emitDispIG(insGroup* ig, insGroup* igPrev = nullptr, bool verbose = false);
    void emitDispIGlist(bool verbose = false);
    void emitDispGCinfo();
    void emitDispClsVar(CORINFO_FIELD_HANDLE fldHnd, ssize_t offs, bool reloc = false);
    void emitDispFrameRef(int varx, int disp, int offs, bool asmfm);
    void emitDispInsOffs(unsigned offs, bool doffs);
    void emitDispInsHex(instrDesc* id, BYTE* code, size_t sz);

#else // !DEBUG
#define emitVarRefOffs 0
#endif // !DEBUG

    /************************************************************************/
    /*                      Method prolog and epilog                        */
    /************************************************************************/

    unsigned emitPrologEndPos;

    unsigned       emitEpilogCnt;
    UNATIVE_OFFSET emitEpilogSize;

#ifdef _TARGET_XARCH_

    void           emitStartExitSeq(); // Mark the start of the "return" sequence
    emitLocation   emitExitSeqBegLoc;
    UNATIVE_OFFSET emitExitSeqSize; // minimum size of any return sequence - the 'ret' after the epilog

#endif // _TARGET_XARCH_

    insGroup* emitPlaceholderList; // per method placeholder list - head
    insGroup* emitPlaceholderLast; // per method placeholder list - tail

#ifdef JIT32_GCENCODER

    // The x86 GC encoder needs to iterate over a list of epilogs to generate a table of
    // epilog offsets. Epilogs always start at the beginning of an IG, so save the first
    // IG of the epilog, and use it to find the epilog offset at the end of code generation.
    struct EpilogList
    {
        EpilogList*  elNext;
        emitLocation elLoc;

        EpilogList() : elNext(nullptr), elLoc()
        {
        }
    };

    EpilogList* emitEpilogList; // per method epilog list - head
    EpilogList* emitEpilogLast; // per method epilog list - tail

public:
    void emitStartEpilog();

    bool emitHasEpilogEnd();

    size_t emitGenEpilogLst(size_t (*fp)(void*, unsigned), void* cp);

#endif // JIT32_GCENCODER

    void emitBegPrologEpilog(insGroup* igPh);
    void emitEndPrologEpilog();

    void emitBegFnEpilog(insGroup* igPh);
    void emitEndFnEpilog();

#if FEATURE_EH_FUNCLETS

    void emitBegFuncletProlog(insGroup* igPh);
    void emitEndFuncletProlog();

    void emitBegFuncletEpilog(insGroup* igPh);
    void emitEndFuncletEpilog();

#endif // FEATURE_EH_FUNCLETS

/************************************************************************/
/*           Members and methods used in PDB translation                */
/************************************************************************/

#ifdef TRANSLATE_PDB

    void MapCode(int ilOffset, BYTE* imgDest);
    void MapFunc(int                imgOff,
                 int                procLen,
                 int                dbgStart,
                 int                dbgEnd,
                 short              frameReg,
                 int                stkAdjust,
                 int                lvaCount,
                 OptJit::LclVarDsc* lvaTable,
                 bool               framePtr);

private:
    int              emitInstrDescILBase; // code offset of IL that produced this instruction desctriptor
    int              emitInstrDescILBase; // code offset of IL that produced this instruction desctriptor
    static AddrMap*  emitPDBOffsetTable;  // translation table for mapping IL addresses to native addresses
    static LocalMap* emitPDBLocalTable;   // local symbol translation table
    static bool      emitIsPDBEnabled;    // flag to disable PDB translation code when a PDB is not found
    static BYTE*     emitILBaseOfCode;    // start of IL .text section
    static BYTE*     emitILMethodBase;    // beginning of IL method (start of header)
    static BYTE*     emitILMethodStart;   // beginning of IL method code (right after the header)
    static BYTE*     emitImgBaseOfCode;   // start of the image .text section

#endif

    /************************************************************************/
    /*    Methods to record a code position and later convert to offset     */
    /************************************************************************/

    unsigned emitFindInsNum(insGroup* ig, instrDesc* id);
    UNATIVE_OFFSET emitFindOffset(insGroup* ig, unsigned insNum);

/************************************************************************/
/*        Members and methods used to issue (encode) instructions.      */
/************************************************************************/

#ifdef DEBUG
    // If we have started issuing instructions from the list of instrDesc, this is set
    bool emitIssuing;
#endif

    BYTE* emitCodeBlock;     // Hot code block
    BYTE* emitColdCodeBlock; // Cold code block
    BYTE* emitConsBlock;     // Read-only (constant) data block

    UNATIVE_OFFSET emitTotalHotCodeSize;
    UNATIVE_OFFSET emitTotalColdCodeSize;

    UNATIVE_OFFSET emitCurCodeOffs(BYTE* dst)
    {
        size_t distance;
        if ((dst >= emitCodeBlock) && (dst <= (emitCodeBlock + emitTotalHotCodeSize)))
        {
            distance = (dst - emitCodeBlock);
        }
        else
        {
            assert(emitFirstColdIG);
            assert(emitColdCodeBlock);
            assert((dst >= emitColdCodeBlock) && (dst <= (emitColdCodeBlock + emitTotalColdCodeSize)));

            distance = (dst - emitColdCodeBlock + emitTotalHotCodeSize);
        }
        noway_assert((UNATIVE_OFFSET)distance == distance);
        return (UNATIVE_OFFSET)distance;
    }

    BYTE* emitOffsetToPtr(UNATIVE_OFFSET offset)
    {
        if (offset < emitTotalHotCodeSize)
        {
            return emitCodeBlock + offset;
        }
        else
        {
            assert(offset < (emitTotalHotCodeSize + emitTotalColdCodeSize));

            return emitColdCodeBlock + (offset - emitTotalHotCodeSize);
        }
    }

    BYTE* emitDataOffsetToPtr(UNATIVE_OFFSET offset)
    {
        assert(offset < emitDataSize());
        return emitConsBlock + offset;
    }

    bool emitJumpCrossHotColdBoundary(size_t srcOffset, size_t dstOffset)
    {
        if (emitTotalColdCodeSize == 0)
        {
            return false;
        }

        assert(srcOffset < (emitTotalHotCodeSize + emitTotalColdCodeSize));
        assert(dstOffset < (emitTotalHotCodeSize + emitTotalColdCodeSize));

        return ((srcOffset < emitTotalHotCodeSize) != (dstOffset < emitTotalHotCodeSize));
    }

    unsigned char emitOutputByte(BYTE* dst, ssize_t val);
    unsigned char emitOutputWord(BYTE* dst, ssize_t val);
    unsigned char emitOutputLong(BYTE* dst, ssize_t val);
    unsigned char emitOutputSizeT(BYTE* dst, ssize_t val);

#if defined(_TARGET_X86_)
    unsigned char emitOutputByte(BYTE* dst, size_t val);
    unsigned char emitOutputWord(BYTE* dst, size_t val);
    unsigned char emitOutputLong(BYTE* dst, size_t val);
    unsigned char emitOutputSizeT(BYTE* dst, size_t val);

    unsigned char emitOutputByte(BYTE* dst, unsigned __int64 val);
    unsigned char emitOutputWord(BYTE* dst, unsigned __int64 val);
    unsigned char emitOutputLong(BYTE* dst, unsigned __int64 val);
    unsigned char emitOutputSizeT(BYTE* dst, unsigned __int64 val);
#endif // defined(_TARGET_X86_)

    size_t emitIssue1Instr(insGroup* ig, instrDesc* id, BYTE** dp);
    size_t emitOutputInstr(insGroup* ig, instrDesc* id, BYTE** dp);

    bool emitHasFramePtr;

#ifdef PSEUDORANDOM_NOP_INSERTION
    bool emitInInstrumentation;
#endif // PSEUDORANDOM_NOP_INSERTION

    unsigned emitMaxTmpSize;

#ifdef DEBUG
    bool emitChkAlign; // perform some alignment checks
#endif

    insGroup* emitCurIG;

    void emitSetShortJump(instrDescJmp* id);
    void emitSetMediumJump(instrDescJmp* id);

public:
    CORINFO_FIELD_HANDLE emitAnyConst(const void* cnsAddr, unsigned cnsSize, emitDataAlignment alignment);

private:
    CORINFO_FIELD_HANDLE emitFltOrDblConst(double constValue, emitAttr attr);
    regNumber emitInsBinary(instruction ins, emitAttr attr, GenTree* dst, GenTree* src);
    regNumber emitInsTernary(instruction ins, emitAttr attr, GenTree* dst, GenTree* src1, GenTree* src2);
    void emitInsLoadInd(instruction ins, emitAttr attr, regNumber dstReg, GenTreeIndir* mem);
    void emitInsStoreInd(instruction ins, emitAttr attr, GenTreeStoreInd* mem);
    void emitInsStoreLcl(instruction ins, emitAttr attr, GenTreeLclVarCommon* varNode);
    insFormat emitMapFmtForIns(insFormat fmt, instruction ins);
    insFormat emitMapFmtAtoM(insFormat fmt);
    void emitHandleMemOp(GenTreeIndir* indir, instrDesc* id, insFormat fmt, instruction ins);
    void spillIntArgRegsToShadowSlots();

/************************************************************************/
/*      The logic that creates and keeps track of instruction groups    */
/************************************************************************/

#ifdef _TARGET_ARMARCH_
// The only place where this limited instruction group size is a problem is
// in the prolog, where we only support a single instruction group. We should really fix that.
// ARM32 and ARM64 both can require a bigger prolog instruction group. One scenario is where
// a function uses all the incoming integer and single-precision floating-point arguments,
// and must store them all to the frame on entry. If the frame is very large, we generate
// ugly code like "movw r10, 0x488; add r10, sp; vstr s0, [r10]" for each store, which
// eats up our insGroup buffer.
#define SC_IG_BUFFER_SIZE (100 * sizeof(emitter::instrDesc) + 14 * SMALL_IDSC_SIZE)
#else // !_TARGET_ARMARCH_
#define SC_IG_BUFFER_SIZE (50 * sizeof(emitter::instrDesc) + 14 * SMALL_IDSC_SIZE)
#endif // !_TARGET_ARMARCH_

    size_t emitIGbuffSize;

    insGroup* emitIGlist; // first  instruction group
    insGroup* emitIGlast; // last   instruction group
    insGroup* emitIGthis; // issued instruction group

    insGroup* emitPrologIG; // prolog instruction group

    instrDescJmp* emitJumpList;       // list of local jumps in method
    instrDescJmp* emitJumpLast;       // last of local jumps in method
    void          emitJumpDistBind(); // Bind all the local jumps in method

    void emitCheckFuncletBranch(instrDesc* jmp, insGroup* jmpIG); // Check for illegal branches between funclets

    bool emitFwdJumps;   // forward jumps present?
    bool emitNoGCIG;     // Are we generating IGF_NOGCINTERRUPT insGroups (for prologs, epilogs, etc.)
    bool emitForceNewIG; // If we generate an instruction, and not another instruction group, force create a new emitAdd
                         // instruction group.

    BYTE* emitCurIGfreeNext; // next available byte in buffer
    BYTE* emitCurIGfreeEndp; // one byte past the last available byte in buffer
    BYTE* emitCurIGfreeBase; // first byte address

    unsigned       emitCurIGinsCnt;   // # of collected instr's in buffer
    unsigned       emitCurIGsize;     // estimated code size of current group in bytes
    UNATIVE_OFFSET emitCurCodeOffset; // current code offset within group
    UNATIVE_OFFSET emitTotalCodeSize; // bytes of code in entire method

    insGroup* emitFirstColdIG; // first cold instruction group

    void emitSetFirstColdIGCookie(void* bbEmitCookie)
    {
        emitFirstColdIG = (insGroup*)bbEmitCookie;
    }

    int emitOffsAdj; // current code offset adjustment

    instrDescJmp* emitCurIGjmpList; // list of jumps   in current IG

    // emitPrev* and emitInit* are only used during code generation, not during
    // emission (issuing), to determine what GC values to store into an IG.
    // Note that only the Vars ones are actually used, apparently due to bugs
    // in that tracking. See emitSavIG(): the important use of ByrefRegs is commented
    // out, and GCrefRegs is always saved.

    VARSET_TP emitPrevGCrefVars;
    regMaskTP emitPrevGCrefRegs;
    regMaskTP emitPrevByrefRegs;

    VARSET_TP emitInitGCrefVars;
    regMaskTP emitInitGCrefRegs;
    regMaskTP emitInitByrefRegs;

    // If this is set, we ignore comparing emitPrev* and emitInit* to determine
    // whether to save GC state (to save space in the IG), and always save it.

    bool emitForceStoreGCState;

    // emitThis* variables are used during emission, to track GC updates
    // on a per-instruction basis. During code generation, per-instruction
    // tracking is done with variables gcVarPtrSetCur, gcRegGCrefSetCur,
    // and gcRegByrefSetCur. However, these are also used for a slightly
    // different purpose during code generation: to try to minimize the
    // amount of GC data stored to an IG, by only storing deltas from what
    // we expect to see at an IG boundary. Also, only emitThisGCrefVars is
    // really the only one used; the others seem to be calculated, but not
    // used due to bugs.

    VARSET_TP emitThisGCrefVars;
    regMaskTP emitThisGCrefRegs; // Current set of registers holding GC references
    regMaskTP emitThisByrefRegs; // Current set of registers holding BYREF references

    bool emitThisGCrefVset; // Is "emitThisGCrefVars" up to date?

    regNumber emitSyncThisObjReg; // where is "this" enregistered for synchronized methods?

#if MULTIREG_HAS_SECOND_GC_RET
    void emitSetSecondRetRegGCType(instrDescCGCA* id, emitAttr secondRetSize);
#endif // MULTIREG_HAS_SECOND_GC_RET

    static void emitEncodeCallGCregs(regMaskTP regs, instrDesc* id);
    static unsigned emitDecodeCallGCregs(instrDesc* id);

    unsigned emitNxtIGnum;

    // random nop insertion to break up nop sleds
    unsigned emitNextNop;
    bool     emitRandomNops;
    void     emitEnableRandomNops()
    {
        emitRandomNops = true;
    }
    void emitDisableRandomNops()
    {
        emitRandomNops = false;
    }

    insGroup* emitAllocAndLinkIG();
    insGroup* emitAllocIG();
    void emitInitIG(insGroup* ig);
    void emitInsertIGAfter(insGroup* insertAfterIG, insGroup* ig);

    void emitNewIG();

#if !defined(JIT32_GCENCODER)
    void emitDisableGC();
    void emitEnableGC();
#endif // !defined(JIT32_GCENCODER)

    void emitGenIG(insGroup* ig);
    insGroup* emitSavIG(bool emitAdd = false);
    void emitNxtIG(bool emitAdd = false);

    bool emitCurIGnonEmpty()
    {
        return (emitCurIG && emitCurIGfreeNext > emitCurIGfreeBase);
    }

    instrDesc* emitLastIns;

#ifdef DEBUG
    void emitCheckIGoffsets();
#endif

    // Terminates any in-progress instruction group, making the current IG a new empty one.
    // Mark this instruction group as having a label; return the the new instruction group.
    // Sets the emitter's record of the currently live GC variables
    // and registers.  The "isFinallyTarget" parameter indicates that the current location is
    // the start of a basic block that is returned to after a finally clause in non-exceptional execution.
    void* emitAddLabel(VARSET_VALARG_TP GCvars, regMaskTP gcrefRegs, regMaskTP byrefRegs, BOOL isFinallyTarget = FALSE);

#ifdef _TARGET_ARMARCH_

    void emitGetInstrDescs(insGroup* ig, instrDesc** id, int* insCnt);

    bool emitGetLocationInfo(emitLocation* emitLoc, insGroup** pig, instrDesc** pid, int* pinsRemaining = NULL);

    bool emitNextID(insGroup*& ig, instrDesc*& id, int& insRemaining);

    typedef void (*emitProcessInstrFunc_t)(instrDesc* id, void* context);

    void emitWalkIDs(emitLocation* locFrom, emitProcessInstrFunc_t processFunc, void* context);

    static void emitGenerateUnwindNop(instrDesc* id, void* context);

#endif // _TARGET_ARMARCH_

#ifdef _TARGET_X86_
    void emitMarkStackLvl(unsigned stackLevel);
#endif

    int emitNextRandomNop();

    //
    // Functions for allocating instrDescs.
    //
    // The emitAllocXXX functions are the base level that allocate memory, and do little else.
    // The emitters themselves use emitNewXXX, which might be thin wrappers over the emitAllocXXX functions.
    //

    void* emitAllocAnyInstr(size_t sz, emitAttr attr);

    instrDesc* emitAllocInstr(emitAttr attr)
    {
#if EMITTER_STATS
        emitTotalIDescCnt++;
#endif // EMITTER_STATS
        return (instrDesc*)emitAllocAnyInstr(sizeof(instrDesc), attr);
    }

    instrDescJmp* emitAllocInstrJmp()
    {
#if EMITTER_STATS
        emitTotalIDescJmpCnt++;
#endif // EMITTER_STATS
        return (instrDescJmp*)emitAllocAnyInstr(sizeof(instrDescJmp), EA_1BYTE);
    }

#if !defined(_TARGET_ARM64_)
    instrDescLbl* emitAllocInstrLbl()
    {
#if EMITTER_STATS
        emitTotalIDescLblCnt++;
#endif // EMITTER_STATS
        return (instrDescLbl*)emitAllocAnyInstr(sizeof(instrDescLbl), EA_4BYTE);
    }
#endif // !_TARGET_ARM64_

    instrDescCns* emitAllocInstrCns(emitAttr attr)
    {
#if EMITTER_STATS
        emitTotalIDescCnsCnt++;
#endif // EMITTER_STATS
        return (instrDescCns*)emitAllocAnyInstr(sizeof(instrDescCns), attr);
    }

    instrDescCns* emitAllocInstrCns(emitAttr attr, target_size_t cns)
    {
        instrDescCns* result = emitAllocInstrCns(attr);
        result->idSetIsLargeCns();
        result->idcCnsVal = cns;
        return result;
    }

    instrDescDsp* emitAllocInstrDsp(emitAttr attr)
    {
#if EMITTER_STATS
        emitTotalIDescDspCnt++;
#endif // EMITTER_STATS
        return (instrDescDsp*)emitAllocAnyInstr(sizeof(instrDescDsp), attr);
    }

    instrDescCnsDsp* emitAllocInstrCnsDsp(emitAttr attr)
    {
#if EMITTER_STATS
        emitTotalIDescCnsDspCnt++;
#endif // EMITTER_STATS
        return (instrDescCnsDsp*)emitAllocAnyInstr(sizeof(instrDescCnsDsp), attr);
    }

#ifdef _TARGET_XARCH_

    instrDescAmd* emitAllocInstrAmd(emitAttr attr)
    {
#if EMITTER_STATS
        emitTotalIDescAmdCnt++;
#endif // EMITTER_STATS
        return (instrDescAmd*)emitAllocAnyInstr(sizeof(instrDescAmd), attr);
    }

    instrDescCnsAmd* emitAllocInstrCnsAmd(emitAttr attr)
    {
#if EMITTER_STATS
        emitTotalIDescCnsAmdCnt++;
#endif // EMITTER_STATS
        return (instrDescCnsAmd*)emitAllocAnyInstr(sizeof(instrDescCnsAmd), attr);
    }

#endif // _TARGET_XARCH_

    instrDescCGCA* emitAllocInstrCGCA(emitAttr attr)
    {
#if EMITTER_STATS
        emitTotalIDescCGCACnt++;
#endif // EMITTER_STATS
        return (instrDescCGCA*)emitAllocAnyInstr(sizeof(instrDescCGCA), attr);
    }

    instrDesc* emitNewInstrSmall(emitAttr attr);
    instrDesc* emitNewInstr(emitAttr attr = EA_4BYTE);
    instrDesc* emitNewInstrSC(emitAttr attr, target_ssize_t cns);
    instrDesc* emitNewInstrCns(emitAttr attr, target_ssize_t cns);
    instrDesc* emitNewInstrDsp(emitAttr attr, target_ssize_t dsp);
    instrDesc* emitNewInstrCnsDsp(emitAttr attr, target_ssize_t cns, int dsp);
#ifdef _TARGET_ARM_
    instrDesc* emitNewInstrReloc(emitAttr attr, BYTE* addr);
#endif // _TARGET_ARM_
    instrDescJmp* emitNewInstrJmp();

#if !defined(_TARGET_ARM64_)
    instrDescLbl* emitNewInstrLbl();
#endif // !_TARGET_ARM64_

    static const BYTE emitFmtToOps[];

#ifdef DEBUG
    static const unsigned emitFmtCount;
#endif

    bool emitIsScnsInsDsc(instrDesc* id);

    size_t emitSizeOfInsDsc(instrDesc* id);

    /************************************************************************/
    /*        The following keeps track of stack-based GC values            */
    /************************************************************************/

    unsigned emitTrkVarCnt;
    int*     emitGCrFrameOffsTab; // Offsets of tracked stack ptr vars (varTrkIndex -> stkOffs)

    unsigned    emitGCrFrameOffsCnt; // Number of       tracked stack ptr vars
    int         emitGCrFrameOffsMin; // Min offset of a tracked stack ptr var
    int         emitGCrFrameOffsMax; // Max offset of a tracked stack ptr var
    bool        emitContTrkPtrLcls;  // All lcl between emitGCrFrameOffsMin/Max are only tracked stack ptr vars
    varPtrDsc** emitGCrFrameLiveTab; // Cache of currently live varPtrs (stkOffs -> varPtrDsc)

    int emitArgFrameOffsMin;
    int emitArgFrameOffsMax;

    int emitLclFrameOffsMin;
    int emitLclFrameOffsMax;

    int emitSyncThisObjOffs; // what is the offset of "this" for synchronized methods?

public:
    void emitSetFrameRangeGCRs(int offsLo, int offsHi);
    void emitSetFrameRangeLcls(int offsLo, int offsHi);
    void emitSetFrameRangeArgs(int offsLo, int offsHi);

    static instruction emitJumpKindToIns(emitJumpKind jumpKind);
    static emitJumpKind emitInsToJumpKind(instruction ins);
    static emitJumpKind emitReverseJumpKind(emitJumpKind jumpKind);

#ifdef DEBUG
    void emitInsSanityCheck(instrDesc* id);
#endif

#ifdef _TARGET_ARMARCH_
    // Returns true if instruction "id->idIns()" writes to a register that might be used to contain a GC
    // pointer. This exempts the SP and PC registers, and floating point registers. Memory access
    // instructions that pre- or post-increment their memory address registers are *not* considered to write
    // to GC registers, even if that memory address is a by-ref: such an instruction cannot change the GC
    // status of that register, since it must be a byref before and remains one after.
    //
    // This may return false positives.
    bool emitInsMayWriteToGCReg(instrDesc* id);

    // Returns "true" if instruction "id->idIns()" writes to a LclVar stack location.
    bool emitInsWritesToLclVarStackLoc(instrDesc* id);

    // Returns true if the instruction may write to more than one register.
    bool emitInsMayWriteMultipleRegs(instrDesc* id);

    // Returns "true" if instruction "id->idIns()" writes to a LclVar stack slot pair.
    bool emitInsWritesToLclVarStackLocPair(instrDesc* id);
#endif // _TARGET_ARMARCH_

    /************************************************************************/
    /*    The following is used to distinguish helper vs non-helper calls   */
    /************************************************************************/

    static bool emitNoGChelper(CorInfoHelpFunc helpFunc);
    static bool emitNoGChelper(CORINFO_METHOD_HANDLE methHnd);

    /************************************************************************/
    /*         The following logic keeps track of live GC ref values        */
    /************************************************************************/

    bool emitFullArgInfo; // full arg info (including non-ptr arg)?
    bool emitFullGCinfo;  // full GC pointer maps?
    bool emitFullyInt;    // fully interruptible code?

    regMaskTP emitGetGCRegsSavedOrModified(CORINFO_METHOD_HANDLE methHnd);

    // Gets a register mask that represent the kill set for a NoGC helper call.
    regMaskTP emitGetGCRegsKilledByNoGCCall(CorInfoHelpFunc helper);

#if EMIT_TRACK_STACK_DEPTH
    unsigned emitCntStackDepth; // 0 in prolog/epilog, One DWORD elsewhere
    unsigned emitMaxStackDepth; // actual computed max. stack depth
#endif

    /* Stack modelling wrt GC */

    bool emitSimpleStkUsed; // using the "simple" stack table?

    union {
        struct // if emitSimpleStkUsed==true
        {
#define BITS_IN_BYTE (8)
#define MAX_SIMPLE_STK_DEPTH (BITS_IN_BYTE * sizeof(unsigned))

            unsigned emitSimpleStkMask;      // bit per pushed dword (if it fits. Lowest bit <==> last pushed arg)
            unsigned emitSimpleByrefStkMask; // byref qualifier for emitSimpleStkMask
        } u1;

        struct // if emitSimpleStkUsed==false
        {
            BYTE   emitArgTrackLcl[16]; // small local table to avoid malloc
            BYTE*  emitArgTrackTab;     // base of the argument tracking stack
            BYTE*  emitArgTrackTop;     // top  of the argument tracking stack
            USHORT emitGcArgTrackCnt;   // count of pending arg records (stk-depth for frameless methods, gc ptrs on stk
                                        // for framed methods)
        } u2;
    };

    unsigned emitCurStackLvl; // amount of bytes pushed on stack

#if EMIT_TRACK_STACK_DEPTH
    /* Functions for stack tracking */

    void emitStackPush(BYTE* addr, GCtype gcType);

    void emitStackPushN(BYTE* addr, unsigned count);

    void emitStackPop(BYTE* addr, bool isCall, unsigned char callInstrSize, unsigned count = 1);

    void emitStackKillArgs(BYTE* addr, unsigned count, unsigned char callInstrSize);

    void emitRecordGCcall(BYTE* codePos, unsigned char callInstrSize);

    // Helpers for the above

    void emitStackPushLargeStk(BYTE* addr, GCtype gcType, unsigned count = 1);
    void emitStackPopLargeStk(BYTE* addr, bool isCall, unsigned char callInstrSize, unsigned count = 1);
#endif // EMIT_TRACK_STACK_DEPTH

    /* Liveness of stack variables, and registers */

    void emitUpdateLiveGCvars(VARSET_VALARG_TP vars, BYTE* addr);
    void emitUpdateLiveGCregs(GCtype gcType, regMaskTP regs, BYTE* addr);

#ifdef DEBUG
    const char* emitGetFrameReg();
    void emitDispRegSet(regMaskTP regs);
    void emitDispVarSet();
#endif

    void emitGCregLiveUpd(GCtype gcType, regNumber reg, BYTE* addr);
    void emitGCregLiveSet(GCtype gcType, regMaskTP mask, BYTE* addr, bool isThis);
    void emitGCregDeadUpdMask(regMaskTP, BYTE* addr);
    void emitGCregDeadUpd(regNumber reg, BYTE* addr);
    void emitGCregDeadSet(GCtype gcType, regMaskTP mask, BYTE* addr);

    void emitGCvarLiveUpd(int offs, int varNum, GCtype gcType, BYTE* addr);
    void emitGCvarLiveSet(int offs, GCtype gcType, BYTE* addr, ssize_t disp = -1);
    void emitGCvarDeadUpd(int offs, BYTE* addr);
    void emitGCvarDeadSet(int offs, BYTE* addr, ssize_t disp = -1);

    GCtype emitRegGCtype(regNumber reg);

    // We have a mixture of code emission methods, some of which return the size of the emitted instruction,
    // requiring the caller to add this to the current code pointer (dst += <call to emit code>), others of which
    // return the updated code pointer (dst = <call to emit code>).  Sometimes we'd like to get the size of
    // the generated instruction for the latter style.  This method accomplishes that --
    // "emitCodeWithInstructionSize(dst, <call to emitCode>, &instrSize)" will do the call, and set
    // "*instrSize" to the after-before code pointer difference.  Returns the result of the call.  (And
    // asserts that the instruction size fits in an unsigned char.)
    static BYTE* emitCodeWithInstructionSize(BYTE* codePtrBefore, BYTE* newCodePointer, unsigned char* instrSize);

    /************************************************************************/
    /*      The following logic keeps track of initialized data sections    */
    /************************************************************************/

    /* One of these is allocated for every blob of initialized data */

    struct dataSection
    {
        enum sectionType
        {
            data,
            blockAbsoluteAddr,
            blockRelative32
        };

        dataSection*   dsNext;
        UNATIVE_OFFSET dsSize;
        sectionType    dsType;
        // variable-sized array used to store the constant data
        // or BasicBlock* array in the block cases.
        BYTE dsCont[0];
    };

    /* These describe the entire initialized/uninitialized data sections */

    struct dataSecDsc
    {
        dataSection*   dsdList;
        dataSection*   dsdLast;
        UNATIVE_OFFSET dsdOffs;
        bool           align16;

        dataSecDsc() : dsdList(nullptr), dsdLast(nullptr), dsdOffs(0), align16(false)
        {
        }
    };

    dataSecDsc emitConsDsc;

    dataSection* emitDataSecCur;

    void emitOutputDataSec(dataSecDsc* sec, BYTE* dst);
#ifdef DEBUG
    void emitDispDataSec(dataSecDsc* section);
#endif

    /************************************************************************/
    /*              Handles to the current class and method.                */
    /************************************************************************/

    COMP_HANDLE emitCmpHandle;

    /************************************************************************/
    /*               Helpers for interface to EE                            */
    /************************************************************************/

    void emitRecordRelocation(void* location,       /* IN */
                              void* target,         /* IN */
                              WORD  fRelocType,     /* IN */
                              WORD  slotNum   = 0,  /* IN */
                              INT32 addlDelta = 0); /* IN */

#ifdef _TARGET_ARM_
    void emitHandlePCRelativeMov32(void* location, /* IN */
                                   void* target);  /* IN */
#endif

    void emitRecordCallSite(ULONG                 instrOffset,   /* IN */
                            CORINFO_SIG_INFO*     callSig,       /* IN */
                            CORINFO_METHOD_HANDLE methodHandle); /* IN */

#ifdef DEBUG
    // This is a scratch buffer used to minimize the number of sig info structs
    // we have to allocate for recordCallSite.
    CORINFO_SIG_INFO* emitScratchSigInfo;
#endif // DEBUG

/************************************************************************/
/*               Logic to collect and display statistics                */
/************************************************************************/

#if EMITTER_STATS

    friend void emitterStats(FILE* fout);
    friend void emitterStaticStats(FILE* fout);

    static size_t emitSizeMethod;

    static unsigned emitTotalInsCnt;

    static unsigned emitCurPrologInsCnt; // current number of prolog instrDescs
    static size_t   emitCurPrologIGSize; // current size of prolog instrDescs
    static unsigned emitMaxPrologInsCnt; // maximum number of prolog instrDescs
    static size_t   emitMaxPrologIGSize; // maximum size of prolog instrDescs

    static unsigned emitTotalIGcnt;   // total number of insGroup allocated
    static unsigned emitTotalPhIGcnt; // total number of insPlaceholderGroupData allocated
    static unsigned emitTotalIGicnt;
    static size_t   emitTotalIGsize;
    static unsigned emitTotalIGmcnt;    // total method count
    static unsigned emitTotalIGEmitAdd; // total number of 'emitAdd' (overflow) groups
    static unsigned emitTotalIGjmps;
    static unsigned emitTotalIGptrs;

    static unsigned emitTotalIDescSmallCnt;
    static unsigned emitTotalIDescCnt;
    static unsigned emitTotalIDescJmpCnt;
#if !defined(_TARGET_ARM64_)
    static unsigned emitTotalIDescLblCnt;
#endif // !defined(_TARGET_ARM64_)
    static unsigned emitTotalIDescCnsCnt;
    static unsigned emitTotalIDescDspCnt;
    static unsigned emitTotalIDescCnsDspCnt;
#ifdef _TARGET_XARCH_
    static unsigned emitTotalIDescAmdCnt;
    static unsigned emitTotalIDescCnsAmdCnt;
#endif // _TARGET_XARCH_
    static unsigned emitTotalIDescCGCACnt;
#ifdef _TARGET_ARM_
    static unsigned emitTotalIDescRelocCnt;
#endif // _TARGET_ARM_

    static size_t emitTotMemAlloc;

    static unsigned emitSmallDspCnt;
    static unsigned emitLargeDspCnt;

    static unsigned emitSmallCnsCnt;
#define SMALL_CNS_TSZ 256
    static unsigned emitSmallCns[SMALL_CNS_TSZ];
    static unsigned emitLargeCnsCnt;

    static unsigned emitIFcounts[IF_COUNT];

#endif // EMITTER_STATS

/*************************************************************************
 *
 *  Define any target-dependent emitter members.
 */

#include "emitdef.h"

    // It would be better if this were a constructor, but that would entail revamping the allocation
    // infrastructure of the entire JIT...
    void Init()
    {
        VarSetOps::AssignNoCopy(emitComp, emitPrevGCrefVars, VarSetOps::MakeEmpty(emitComp));
        VarSetOps::AssignNoCopy(emitComp, emitInitGCrefVars, VarSetOps::MakeEmpty(emitComp));
        VarSetOps::AssignNoCopy(emitComp, emitThisGCrefVars, VarSetOps::MakeEmpty(emitComp));
    }
};

/*****************************************************************************
 *
 *  Define any target-dependent inlines.
 */

#include "emitinl.h"

inline void emitter::instrDesc::checkSizes()
{
#ifdef DEBUG
    C_ASSERT(SMALL_IDSC_SIZE == (offsetof(instrDesc, _idDebugOnlyInfo) + sizeof(instrDescDebugInfo*)));
#endif
    C_ASSERT(SMALL_IDSC_SIZE == offsetof(instrDesc, _idAddrUnion));
}

/*****************************************************************************
 *
 *  Returns true if the given instruction descriptor is a "small
 *  constant" one (i.e. one of the descriptors that don't have all instrDesc
 *  fields allocated).
 */

inline bool emitter::emitIsScnsInsDsc(instrDesc* id)
{
    return id->idIsSmallDsc();
}

/*****************************************************************************
 *
 *  Given an instruction, return its "update mode" (RD/WR/RW).
 */

inline insUpdateModes emitter::emitInsUpdateMode(instruction ins)
{
#ifdef DEBUG
    assert((unsigned)ins < emitInsModeFmtCnt);
#endif
    return (insUpdateModes)emitInsModeFmtTab[ins];
}

/*****************************************************************************
 *
 *  Return the number of epilog blocks generated so far.
 */

inline unsigned emitter::emitGetEpilogCnt()
{
    return emitEpilogCnt;
}

/*****************************************************************************
 *
 *  Return the current size of the specified data section.
 */

inline UNATIVE_OFFSET emitter::emitDataSize()
{
    return emitConsDsc.dsdOffs;
}

/*****************************************************************************
 *
 *  Return a handle to the current position in the output stream. This can
 *  be later converted to an actual code offset in bytes.
 */

inline void* emitter::emitCurBlock()
{
    return emitCurIG;
}

/*****************************************************************************
 *
 *  The emitCurOffset() method returns a cookie that identifies the current
 *  position in the instruction stream. Due to things like scheduling (and
 *  the fact that the final size of some instructions cannot be known until
 *  the end of code generation), we return a value with the instruction number
 *  and its estimated offset to the caller.
 */

inline unsigned emitGetInsNumFromCodePos(unsigned codePos)
{
    return (codePos & 0xFFFF);
}

inline unsigned emitGetInsOfsFromCodePos(unsigned codePos)
{
    return (codePos >> 16);
}

inline unsigned emitter::emitCurOffset()
{
    unsigned codePos = emitCurIGinsCnt + (emitCurIGsize << 16);

    assert(emitGetInsOfsFromCodePos(codePos) == emitCurIGsize);
    assert(emitGetInsNumFromCodePos(codePos) == emitCurIGinsCnt);

    // printf("[IG=%02u;ID=%03u;OF=%04X] => %08X\n", emitCurIG->igNum, emitCurIGinsCnt, emitCurIGsize, codePos);

    return codePos;
}

extern const unsigned short emitTypeSizes[TYP_COUNT];

template <class T>
inline emitAttr emitTypeSize(T type)
{
    assert(TypeGet(type) < TYP_COUNT);
    assert(emitTypeSizes[TypeGet(type)] > 0);
    return (emitAttr)emitTypeSizes[TypeGet(type)];
}

extern const unsigned short emitTypeActSz[TYP_COUNT];

template <class T>
inline emitAttr emitActualTypeSize(T type)
{
    assert(TypeGet(type) < TYP_COUNT);
    assert(emitTypeActSz[TypeGet(type)] > 0);
    return (emitAttr)emitTypeActSz[TypeGet(type)];
}

/*****************************************************************************
 *
 *  Convert between an operand size in bytes and a smaller encoding used for
 *  storage in instruction descriptors.
 */

/* static */ inline emitter::opSize emitter::emitEncodeSize(emitAttr size)
{
    assert(size == EA_1BYTE || size == EA_2BYTE || size == EA_4BYTE || size == EA_8BYTE || size == EA_16BYTE ||
           size == EA_32BYTE);

    return emitSizeEncode[((int)size) - 1];
}

/* static */ inline emitAttr emitter::emitDecodeSize(emitter::opSize ensz)
{
    assert(((unsigned)ensz) < OPSZ_COUNT);

    return emitSizeDecode[ensz];
}

/*****************************************************************************
 *
 *  Little helpers to allocate various flavors of instructions.
 */

inline emitter::instrDesc* emitter::emitNewInstrSmall(emitAttr attr)
{
    instrDesc* id;

    id = (instrDesc*)emitAllocAnyInstr(SMALL_IDSC_SIZE, attr);
    id->idSetIsSmallDsc();

#if EMITTER_STATS
    emitTotalIDescSmallCnt++;
#endif // EMITTER_STATS

    return id;
}

inline emitter::instrDesc* emitter::emitNewInstr(emitAttr attr)
{
    // This is larger than the Small Descr
    return emitAllocInstr(attr);
}

inline emitter::instrDescJmp* emitter::emitNewInstrJmp()
{
    return emitAllocInstrJmp();
}

#if !defined(_TARGET_ARM64_)
inline emitter::instrDescLbl* emitter::emitNewInstrLbl()
{
    return emitAllocInstrLbl();
}
#endif // !_TARGET_ARM64_

inline emitter::instrDesc* emitter::emitNewInstrDsp(emitAttr attr, target_ssize_t dsp)
{
    if (dsp == 0)
    {
        instrDesc* id = emitAllocInstr(attr);

#if EMITTER_STATS
        emitSmallDspCnt++;
#endif

        return id;
    }
    else
    {
        instrDescDsp* id = emitAllocInstrDsp(attr);

        id->idSetIsLargeDsp();
        id->iddDspVal = dsp;

#if EMITTER_STATS
        emitLargeDspCnt++;
#endif

        return id;
    }
}

/*****************************************************************************
 *
 *  Allocate an instruction descriptor for an instruction with a constant operand.
 *  The instruction descriptor uses the idAddrUnion to save additional info
 *  so the smallest size that this can be is sizeof(instrDesc).
 *  Note that this very similar to emitter::emitNewInstrSC(), except it never
 *  allocates a small descriptor.
 */
inline emitter::instrDesc* emitter::emitNewInstrCns(emitAttr attr, target_ssize_t cns)
{
    if (instrDesc::fitsInSmallCns(cns))
    {
        instrDesc* id = emitAllocInstr(attr);
        id->idSmallCns(cns);

#if EMITTER_STATS
        emitSmallCnsCnt++;
        if ((cns - ID_MIN_SMALL_CNS) >= (SMALL_CNS_TSZ - 1))
            emitSmallCns[SMALL_CNS_TSZ - 1]++;
        else
            emitSmallCns[cns - ID_MIN_SMALL_CNS]++;
#endif

        return id;
    }
    else
    {
        instrDescCns* id = emitAllocInstrCns(attr, cns);

#if EMITTER_STATS
        emitLargeCnsCnt++;
#endif

        return id;
    }
}

/*****************************************************************************
 *
 *  Get the instrDesc size, general purpose version
 *
 */

inline size_t emitter::emitGetInstrDescSize(const instrDesc* id)
{
    if (id->idIsSmallDsc())
    {
        return SMALL_IDSC_SIZE;
    }

    if (id->idIsLargeCns())
    {
        return sizeof(instrDescCns);
    }

    return sizeof(instrDesc);
}

/*****************************************************************************
 *
 *  Allocate an instruction descriptor for an instruction with a small integer
 *  constant operand. This is the same as emitNewInstrCns() except that here
 *  any constant that is small enough for instrDesc::fitsInSmallCns() only gets
 *  allocated SMALL_IDSC_SIZE bytes (and is thus a small descriptor, whereas
 *  emitNewInstrCns() always allocates at least sizeof(instrDesc)).
 */

inline emitter::instrDesc* emitter::emitNewInstrSC(emitAttr attr, target_ssize_t cns)
{
    if (instrDesc::fitsInSmallCns(cns))
    {
        instrDesc* id = emitNewInstrSmall(attr);
        id->idSmallCns(cns);

#if EMITTER_STATS
        emitSmallCnsCnt++;
        if ((cns - ID_MIN_SMALL_CNS) >= (SMALL_CNS_TSZ - 1))
            emitSmallCns[SMALL_CNS_TSZ - 1]++;
        else
            emitSmallCns[cns - ID_MIN_SMALL_CNS]++;
#endif

        return id;
    }
    else
    {
        instrDescCns* id = emitAllocInstrCns(attr, cns);

#if EMITTER_STATS
        emitLargeCnsCnt++;
#endif

        return id;
    }
}

/*****************************************************************************
 *
 *  Get the instrDesc size for something that contains a constant
 */

inline size_t emitter::emitGetInstrDescSizeSC(const instrDesc* id)
{
    if (id->idIsSmallDsc())
    {
        return SMALL_IDSC_SIZE;
    }
    else if (id->idIsLargeCns())
    {
        return sizeof(instrDescCns);
    }
    else
    {
        return sizeof(instrDesc);
    }
}

#ifdef _TARGET_ARM_

inline emitter::instrDesc* emitter::emitNewInstrReloc(emitAttr attr, BYTE* addr)
{
    assert(EA_IS_RELOC(attr));

    instrDescReloc* id = (instrDescReloc*)emitAllocAnyInstr(sizeof(instrDescReloc), attr);
    assert(id->idIsReloc());

    id->idrRelocVal = addr;

#if EMITTER_STATS
    emitTotalIDescRelocCnt++;
#endif // EMITTER_STATS

    return id;
}

#endif // _TARGET_ARM_

#ifdef _TARGET_XARCH_

/*****************************************************************************
 *
 *  The following helpers should be used to access the various values that
 *  get stored in different places within the instruction descriptor.
 */

inline ssize_t emitter::emitGetInsCns(instrDesc* id)
{
    return id->idIsLargeCns() ? ((instrDescCns*)id)->idcCnsVal : id->idSmallCns();
}

inline ssize_t emitter::emitGetInsDsp(instrDesc* id)
{
    if (id->idIsLargeDsp())
    {
        if (id->idIsLargeCns())
        {
            return ((instrDescCnsDsp*)id)->iddcDspVal;
        }
        return ((instrDescDsp*)id)->iddDspVal;
    }
    return 0;
}

/*****************************************************************************
 *
 *  Get hold of the argument count for an indirect call.
 */

inline unsigned emitter::emitGetInsCIargs(instrDesc* id)
{
    if (id->idIsLargeCall())
    {
        return ((instrDescCGCA*)id)->idcArgCnt;
    }
    else
    {
        assert(id->idIsLargeDsp() == false);
        assert(id->idIsLargeCns() == false);

        ssize_t cns = emitGetInsCns(id);
        assert((unsigned)cns == (size_t)cns);
        return (unsigned)cns;
    }
}

#endif // _TARGET_XARCH_

/*****************************************************************************
 *
 *  Returns true if the given register contains a live GC ref.
 */

inline GCtype emitter::emitRegGCtype(regNumber reg)
{
    assert(emitIssuing);

    if ((emitThisGCrefRegs & genRegMask(reg)) != 0)
    {
        return GCT_GCREF;
    }
    else if ((emitThisByrefRegs & genRegMask(reg)) != 0)
    {
        return GCT_BYREF;
    }
    else
    {
        return GCT_NONE;
    }
}

#ifdef DEBUG

#if EMIT_TRACK_STACK_DEPTH
#define CHECK_STACK_DEPTH() assert((int)emitCurStackLvl >= 0)
#else
#define CHECK_STACK_DEPTH()
#endif

#endif // DEBUG

/*****************************************************************************
 *
 *  Return true when a given code offset is properly aligned for the target
 */

inline bool IsCodeAligned(UNATIVE_OFFSET offset)
{
    return ((offset & (CODE_ALIGN - 1)) == 0);
}

// Static:
inline BYTE* emitter::emitCodeWithInstructionSize(BYTE* codePtrBefore, BYTE* newCodePointer, unsigned char* instrSize)
{
    // DLD: Perhaps this method should return the instruction size, and we should do dst += <that size>
    // as is done in other cases?
    assert(newCodePointer >= codePtrBefore);
    ClrSafeInt<unsigned char> callInstrSizeSafe = ClrSafeInt<unsigned char>(newCodePointer - codePtrBefore);
    assert(!callInstrSizeSafe.IsOverflow());
    *instrSize = callInstrSizeSafe.Value();
    return newCodePointer;
}

/*****************************************************************************
 *
 *  Add a new IG to the current list, and get it ready to receive code.
 */

inline void emitter::emitNewIG()
{
    insGroup* ig = emitAllocAndLinkIG();

    /* It's linked in. Now, set it up to accept code */

    emitGenIG(ig);
}

#if !defined(JIT32_GCENCODER)
// Start a new instruction group that is not interruptable
inline void emitter::emitDisableGC()
{
    emitNoGCIG = true;

    if (emitCurIGnonEmpty())
    {
        emitNxtIG(true);
    }
    else
    {
        emitCurIG->igFlags |= IGF_NOGCINTERRUPT;
    }
}

// Start a new instruction group that is interruptable
inline void emitter::emitEnableGC()
{
    emitNoGCIG = false;

    // The next time an instruction needs to be generated, force a new instruction group.
    // It will be an emitAdd group in that case. Note that the next thing we see might be
    // a label, which will force a non-emitAdd group.
    //
    // Note that we can't just create a new instruction group here, because we don't know
    // if there are going to be any instructions added to it, and we don't support empty
    // instruction groups.
    emitForceNewIG = true;
}
#endif // !defined(JIT32_GCENCODER)

/*****************************************************************************/
#endif // _EMIT_H_
/*****************************************************************************/
