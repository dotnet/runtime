// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// This class contains all the data & functionality for code generation
// of a method, except for the target-specific elements, which are
// primarily in the Target class.
//

#ifndef _CODEGEN_H_
#define _CODEGEN_H_
#include "compiler.h" // temporary??
#include "codegeninterface.h"
#include "regset.h"
#include "jitgcinfo.h"

#if defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_) || defined(_TARGET_ARM_)
#define FOREACH_REGISTER_FILE(file)                                                                                    \
    for ((file) = &(this->intRegState); (file) != NULL;                                                                \
         (file) = ((file) == &(this->intRegState)) ? &(this->floatRegState) : NULL)
#else
#define FOREACH_REGISTER_FILE(file) (file) = &(this->intRegState);
#endif

class CodeGen : public CodeGenInterface
{
    friend class emitter;
    friend class DisAssembler;

public:
    // This could use further abstraction
    CodeGen(Compiler* theCompiler);

    virtual void genGenerateCode(void** codePtr, ULONG* nativeSizeOfCode);
    // TODO-Cleanup: Abstract out the part of this that finds the addressing mode, and
    // move it to Lower
    virtual bool genCreateAddrMode(GenTreePtr  addr,
                                   int         mode,
                                   bool        fold,
                                   regMaskTP   regMask,
                                   bool*       revPtr,
                                   GenTreePtr* rv1Ptr,
                                   GenTreePtr* rv2Ptr,
#if SCALED_ADDR_MODES
                                   unsigned* mulPtr,
#endif
                                   unsigned* cnsPtr,
                                   bool      nogen = false);

    // This should move to CodeGenClassic.h after genCreateAddrMode() is no longer dependent upon it
    void genIncRegBy(regNumber reg, ssize_t ival, GenTreePtr tree, var_types dstType = TYP_INT, bool ovfl = false);

private:
#if defined(_TARGET_XARCH_) && !FEATURE_STACK_FP_X87
    // Bit masks used in negating a float or double number.
    // The below gentrees encapsulate the data offset to the bitmasks as GT_CLS_VAR nodes.
    // This is to avoid creating more than one data constant for these bitmasks when a
    // method has more than one GT_NEG operation on floating point values.
    GenTreePtr negBitmaskFlt;
    GenTreePtr negBitmaskDbl;

    // Bit masks used in computing Math.Abs() of a float or double number.
    GenTreePtr absBitmaskFlt;
    GenTreePtr absBitmaskDbl;

    // Bit mask used in U8 -> double conversion to adjust the result.
    GenTreePtr u8ToDblBitmask;

    // Generates SSE2 code for the given tree as "Operand BitWiseOp BitMask"
    void genSSE2BitwiseOp(GenTreePtr treeNode);
#endif // defined(_TARGET_XARCH_) && !FEATURE_STACK_FP_X87

    void genPrepForCompiler();

    void genPrepForEHCodegen();

    inline RegState* regStateForType(var_types t)
    {
        return varTypeIsFloating(t) ? &floatRegState : &intRegState;
    }
    inline RegState* regStateForReg(regNumber reg)
    {
        return genIsValidFloatReg(reg) ? &floatRegState : &intRegState;
    }

    regNumber genFramePointerReg()
    {
        if (isFramePointerUsed())
        {
            return REG_FPBASE;
        }
        else
        {
            return REG_SPBASE;
        }
    }

    enum CompareKind
    {
        CK_SIGNED,
        CK_UNSIGNED,
        CK_LOGICAL
    };
    static emitJumpKind genJumpKindForOper(genTreeOps cmp, CompareKind compareKind);

    // For a given compare oper tree, returns the conditions to use with jmp/set in 'jmpKind' array.
    // The corresponding elements of jmpToTrueLabel indicate whether the target of the jump is to the
    // 'true' label or a 'false' label.
    //
    // 'true' label corresponds to jump target of the current basic block i.e. the target to
    // branch to on compare condition being true.  'false' label corresponds to the target to
    // branch to on condition being false.
    static void genJumpKindsForTree(GenTreePtr cmpTree, emitJumpKind jmpKind[2], bool jmpToTrueLabel[2]);

#if !defined(_TARGET_64BIT_)
    static void genJumpKindsForTreeLongHi(GenTreePtr cmpTree, emitJumpKind jmpKind[2]);
#endif //! defined(_TARGET_64BIT_)

    static bool genShouldRoundFP();

    GenTreeIndir indirForm(var_types type, GenTree* base);

    GenTreeIntCon intForm(var_types type, ssize_t value);

    void genRangeCheck(GenTree* node);

    void genLockedInstructions(GenTree* node);

    //-------------------------------------------------------------------------
    // Register-related methods

    void rsInit();

#ifdef REG_OPT_RSVD
    // On some targets such as the ARM we may need to have an extra reserved register
    //  that is used when addressing stack based locals and stack based temps.
    //  This method returns the regNumber that should be used when an extra register
    //  is needed to access the stack based locals and stack based temps.
    //
    regNumber rsGetRsvdReg()
    {
        // We should have already added this register to the mask
        //  of reserved registers in regSet.rdMaskResvd
        noway_assert((regSet.rsMaskResvd & RBM_OPT_RSVD) != 0);

        return REG_OPT_RSVD;
    }
#endif // REG_OPT_RSVD

    regNumber findStkLclInReg(unsigned lclNum)
    {
#ifdef DEBUG
        genInterruptibleUsed = true;
#endif
        return regTracker.rsLclIsInReg(lclNum);
    }

    //-------------------------------------------------------------------------

    bool     genUseBlockInit;  // true if we plan to block-initialize the local stack frame
    unsigned genInitStkLclCnt; // The count of local variables that we need to zero init

    //  Keeps track of how many bytes we've pushed on the processor's stack.
    //
    unsigned genStackLevel;

#if STACK_PROBES
    // Stack Probes
    bool genNeedPrologStackProbe;

    void genGenerateStackProbe();
#endif

#ifdef LEGACY_BACKEND
    regMaskTP genNewLiveRegMask(GenTreePtr first, GenTreePtr second);

    // During codegen, determine the LiveSet after tree.
    // Preconditions: must be called during codegen, when compCurLife and
    // compCurLifeTree are being maintained, and tree must occur in the current
    // statement.
    VARSET_VALRET_TP genUpdateLiveSetForward(GenTreePtr tree);
#endif

    //-------------------------------------------------------------------------

    void genReportEH();

    // Allocates storage for the GC info, writes the GC info into that storage, records the address of the
    // GC info of the method with the EE, and returns a pointer to the "info" portion (just post-header) of
    // the GC info.  Requires "codeSize" to be the size of the generated code, "prologSize" and "epilogSize"
    // to be the sizes of the prolog and epilog, respectively.  In DEBUG, makes a check involving the
    // "codePtr", assumed to be a pointer to the start of the generated code.
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef JIT32_GCENCODER
    void* genCreateAndStoreGCInfo(unsigned codeSize, unsigned prologSize, unsigned epilogSize DEBUGARG(void* codePtr));
    void* genCreateAndStoreGCInfoJIT32(unsigned codeSize,
                                       unsigned prologSize,
                                       unsigned epilogSize DEBUGARG(void* codePtr));
#else  // !JIT32_GCENCODER
    void genCreateAndStoreGCInfo(unsigned codeSize, unsigned prologSize, unsigned epilogSize DEBUGARG(void* codePtr));
    void genCreateAndStoreGCInfoX64(unsigned codeSize, unsigned prologSize DEBUGARG(void* codePtr));
#endif // !JIT32_GCENCODER

    /**************************************************************************
     *                          PROTECTED
     *************************************************************************/

protected:
    // the current (pending) label ref, a label which has been referenced but not yet seen
    BasicBlock* genPendingCallLabel;

#ifdef DEBUG
    // Last instr we have displayed for dspInstrs
    unsigned genCurDispOffset;

    static const char* genInsName(instruction ins);
#endif // DEBUG

    //-------------------------------------------------------------------------

    // JIT-time constants for use in multi-dimensional array code generation.
    unsigned genOffsetOfMDArrayLowerBound(var_types elemType, unsigned rank, unsigned dimension);
    unsigned genOffsetOfMDArrayDimensionSize(var_types elemType, unsigned rank, unsigned dimension);

#ifdef DEBUG
    static const char* genSizeStr(emitAttr size);

    void genStressRegs(GenTreePtr tree);
#endif // DEBUG

    void genCodeForBBlist();

public:
#ifndef LEGACY_BACKEND
    // genSpillVar is called by compUpdateLifeVar in the !LEGACY_BACKEND case
    void genSpillVar(GenTreePtr tree);
#endif // !LEGACY_BACKEND

protected:
#ifndef LEGACY_BACKEND
    void genEmitHelperCall(unsigned helper, int argSize, emitAttr retSize, regNumber callTarget = REG_NA);
#else
    void genEmitHelperCall(unsigned helper, int argSize, emitAttr retSize);
#endif

    void genGCWriteBarrier(GenTreePtr tree, GCInfo::WriteBarrierForm wbf);

    BasicBlock* genCreateTempLabel();

    void genDefineTempLabel(BasicBlock* label);

    void genAdjustSP(ssize_t delta);

    void genExitCode(BasicBlock* block);

    //-------------------------------------------------------------------------

    GenTreePtr genMakeConst(const void* cnsAddr, var_types cnsType, GenTreePtr cnsTree, bool dblAlign);

    //-------------------------------------------------------------------------

    void genJumpToThrowHlpBlk(emitJumpKind jumpKind, SpecialCodeKind codeKind, GenTreePtr failBlk = nullptr);

    void genCheckOverflow(GenTreePtr tree);

    //-------------------------------------------------------------------------
    //
    // Prolog/epilog generation
    //
    //-------------------------------------------------------------------------

    //
    // Prolog functions and data (there are a few exceptions for more generally used things)
    //

    void genEstablishFramePointer(int delta, bool reportUnwindData);
    void genFnPrologCalleeRegArgs(regNumber xtraReg, bool* pXtraRegClobbered, RegState* regState);
    void genEnregisterIncomingStackArgs();
    void genCheckUseBlockInit();
#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING) && defined(FEATURE_SIMD)
    void genClearStackVec3ArgUpperBits();
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING && FEATURE_SIMD

#if defined(_TARGET_ARM64_)
    bool genInstrWithConstant(instruction ins,
                              emitAttr    attr,
                              regNumber   reg1,
                              regNumber   reg2,
                              ssize_t     imm,
                              regNumber   tmpReg,
                              bool        inUnwindRegion = false);

    void genStackPointerAdjustment(ssize_t spAdjustment, regNumber tmpReg, bool* pTmpRegIsZero);

    void genPrologSaveRegPair(regNumber reg1,
                              regNumber reg2,
                              int       spOffset,
                              int       spDelta,
                              bool      lastSavedWasPreviousPair,
                              regNumber tmpReg,
                              bool*     pTmpRegIsZero);

    void genPrologSaveReg(regNumber reg1, int spOffset, int spDelta, regNumber tmpReg, bool* pTmpRegIsZero);

    void genEpilogRestoreRegPair(
        regNumber reg1, regNumber reg2, int spOffset, int spDelta, regNumber tmpReg, bool* pTmpRegIsZero);

    void genEpilogRestoreReg(regNumber reg1, int spOffset, int spDelta, regNumber tmpReg, bool* pTmpRegIsZero);

    void genSaveCalleeSavedRegistersHelp(regMaskTP regsToSaveMask, int lowestCalleeSavedOffset, int spDelta);

    void genRestoreCalleeSavedRegistersHelp(regMaskTP regsToRestoreMask, int lowestCalleeSavedOffset, int spDelta);

    void genPushCalleeSavedRegisters(regNumber initReg, bool* pInitRegZeroed);
#else
    void genPushCalleeSavedRegisters();
#endif

    void genAllocLclFrame(unsigned frameSize, regNumber initReg, bool* pInitRegZeroed, regMaskTP maskArgRegsLiveIn);

#if defined(_TARGET_ARM_)

    void genPushFltRegs(regMaskTP regMask);
    void genPopFltRegs(regMaskTP regMask);
    regMaskTP genStackAllocRegisterMask(unsigned frameSize, regMaskTP maskCalleeSavedFloat);

    regMaskTP genJmpCallArgMask();

    void genFreeLclFrame(unsigned           frameSize,
                         /* IN OUT */ bool* pUnwindStarted,
                         bool               jmpEpilog);

    bool genUsedPopToReturn; // True if we use the pop into PC to return,
                             // False if we didn't and must branch to LR to return.

    // A set of information that is used by funclet prolog and epilog generation. It is collected once, before
    // funclet prologs and epilogs are generated, and used by all funclet prologs and epilogs, which must all be the
    // same.
    struct FuncletFrameInfoDsc
    {
        regMaskTP fiSaveRegs;                  // Set of registers saved in the funclet prolog (includes LR)
        unsigned  fiFunctionCallerSPtoFPdelta; // Delta between caller SP and the frame pointer
        unsigned  fiSpDelta;                   // Stack pointer delta
        unsigned  fiPSP_slot_SP_offset;        // PSP slot offset from SP
        int       fiPSP_slot_CallerSP_offset;  // PSP slot offset from Caller SP
    };

    FuncletFrameInfoDsc genFuncletInfo;

#elif defined(_TARGET_ARM64_)

    // A set of information that is used by funclet prolog and epilog generation. It is collected once, before
    // funclet prologs and epilogs are generated, and used by all funclet prologs and epilogs, which must all be the
    // same.
    struct FuncletFrameInfoDsc
    {
        regMaskTP fiSaveRegs;                // Set of callee-saved registers saved in the funclet prolog (includes LR)
        int fiFunction_CallerSP_to_FP_delta; // Delta between caller SP and the frame pointer in the parent function
                                             // (negative)
        int fiSP_to_FPLR_save_delta;         // FP/LR register save offset from SP (positive)
        int fiSP_to_PSP_slot_delta;          // PSP slot offset from SP (positive)
        int fiSP_to_CalleeSave_delta;        // First callee-saved register slot offset from SP (positive)
        int fiCallerSP_to_PSP_slot_delta;    // PSP slot offset from Caller SP (negative)
        int fiFrameType;                     // Funclet frame types are numbered. See genFuncletProlog() for details.
        int fiSpDelta1;                      // Stack pointer delta 1 (negative)
        int fiSpDelta2;                      // Stack pointer delta 2 (negative)
    };

    FuncletFrameInfoDsc genFuncletInfo;

#elif defined(_TARGET_AMD64_)

    // A set of information that is used by funclet prolog and epilog generation. It is collected once, before
    // funclet prologs and epilogs are generated, and used by all funclet prologs and epilogs, which must all be the
    // same.
    struct FuncletFrameInfoDsc
    {
        unsigned fiFunction_InitialSP_to_FP_delta; // Delta between Initial-SP and the frame pointer
        unsigned fiSpDelta;                        // Stack pointer delta
        int      fiPSP_slot_InitialSP_offset;      // PSP slot offset from Initial-SP
    };

    FuncletFrameInfoDsc genFuncletInfo;

#endif // _TARGET_AMD64_

#if defined(_TARGET_XARCH_) && !FEATURE_STACK_FP_X87

    // Save/Restore callee saved float regs to stack
    void genPreserveCalleeSavedFltRegs(unsigned lclFrameSize);
    void genRestoreCalleeSavedFltRegs(unsigned lclFrameSize);

#endif // _TARGET_XARCH_ && FEATURE_STACK_FP_X87

#if !FEATURE_STACK_FP_X87
    void genZeroInitFltRegs(const regMaskTP& initFltRegs, const regMaskTP& initDblRegs, const regNumber& initReg);
#endif // !FEATURE_STACK_FP_X87

    regNumber genGetZeroReg(regNumber initReg, bool* pInitRegZeroed);

    void genZeroInitFrame(int untrLclHi, int untrLclLo, regNumber initReg, bool* pInitRegZeroed);

    void genReportGenericContextArg(regNumber initReg, bool* pInitRegZeroed);

    void genSetGSSecurityCookie(regNumber initReg, bool* pInitRegZeroed);

    void genFinalizeFrame();

#ifdef PROFILING_SUPPORTED
    void genProfilingEnterCallback(regNumber initReg, bool* pInitRegZeroed);
    void genProfilingLeaveCallback(unsigned helper = CORINFO_HELP_PROF_FCN_LEAVE);
#endif // PROFILING_SUPPORTED

    void genPrologPadForReJit();

    void genEmitCall(int                   callType,
                     CORINFO_METHOD_HANDLE methHnd,
                     INDEBUG_LDISASM_COMMA(CORINFO_SIG_INFO* sigInfo) void* addr X86_ARG(ssize_t argSize),
                     emitAttr retSize MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(emitAttr secondRetSize),
                     IL_OFFSETX ilOffset,
                     regNumber  base   = REG_NA,
                     bool       isJump = false,
                     bool       isNoGC = false);

    void genEmitCall(int                   callType,
                     CORINFO_METHOD_HANDLE methHnd,
                     INDEBUG_LDISASM_COMMA(CORINFO_SIG_INFO* sigInfo) GenTreeIndir* indir X86_ARG(ssize_t argSize),
                     emitAttr retSize MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(emitAttr secondRetSize),
                     IL_OFFSETX ilOffset);

    //
    // Epilog functions
    //
    CLANG_FORMAT_COMMENT_ANCHOR;

#if defined(_TARGET_ARM_)
    bool genCanUsePopToReturn(regMaskTP maskPopRegsInt, bool jmpEpilog);
#endif

#if defined(_TARGET_ARM64_)

    void genPopCalleeSavedRegistersAndFreeLclFrame(bool jmpEpilog);

#else // !defined(_TARGET_ARM64_)

    void genPopCalleeSavedRegisters(bool jmpEpilog = false);

#endif // !defined(_TARGET_ARM64_)

    //
    // Common or driving functions
    //

    void genReserveProlog(BasicBlock* block); // currently unused
    void genReserveEpilog(BasicBlock* block);
    void genFnProlog();
    void genFnEpilog(BasicBlock* block);

#if FEATURE_EH_FUNCLETS

    void genReserveFuncletProlog(BasicBlock* block);
    void genReserveFuncletEpilog(BasicBlock* block);
    void genFuncletProlog(BasicBlock* block);
    void genFuncletEpilog();
    void genCaptureFuncletPrologEpilogInfo();

    void genSetPSPSym(regNumber initReg, bool* pInitRegZeroed);

    void genUpdateCurrentFunclet(BasicBlock* block);

#else // FEATURE_EH_FUNCLETS

    // This is a no-op when there are no funclets!
    void genUpdateCurrentFunclet(BasicBlock* block)
    {
        return;
    }

#endif // FEATURE_EH_FUNCLETS

    void genGeneratePrologsAndEpilogs();

#if defined(DEBUG) && defined(_TARGET_ARM64_)
    void genArm64EmitterUnitTests();
#endif

#if defined(DEBUG) && defined(LATE_DISASM) && defined(_TARGET_AMD64_)
    void genAmd64EmitterUnitTests();
#endif

//-------------------------------------------------------------------------
//
// End prolog/epilog generation
//
//-------------------------------------------------------------------------

/*****************************************************************************/
#ifdef DEBUGGING_SUPPORT
/*****************************************************************************/

#ifdef DEBUG
    void genIPmappingDisp(unsigned mappingNum, Compiler::IPmappingDsc* ipMapping);
    void genIPmappingListDisp();
#endif // DEBUG

    void genIPmappingAdd(IL_OFFSETX offset, bool isLabel);
    void genIPmappingAddToFront(IL_OFFSETX offset);
    void genIPmappingGen();

    void genEnsureCodeEmitted(IL_OFFSETX offsx);

    //-------------------------------------------------------------------------
    // scope info for the variables

    void genSetScopeInfo(unsigned            which,
                         UNATIVE_OFFSET      startOffs,
                         UNATIVE_OFFSET      length,
                         unsigned            varNum,
                         unsigned            LVnum,
                         bool                avail,
                         Compiler::siVarLoc& loc);

    void genSetScopeInfo();

    void genRemoveBBsection(BasicBlock* head, BasicBlock* tail);

protected:
    /*
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XX                                                                           XX
    XX                           ScopeInfo                                       XX
    XX                                                                           XX
    XX  Keeps track of the scopes during code-generation.                        XX
    XX  This is used to translate the local-variable debugging information       XX
    XX  from IL offsets to native code offsets.                                  XX
    XX                                                                           XX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    */

    /*****************************************************************************/
    /*****************************************************************************
     *                              ScopeInfo
     *
     * This class is called during code gen at block-boundaries, and when the
     * set of live variables changes. It keeps track of the scope of the variables
     * in terms of the native code PC.
     */

public:
    void siInit();

    void siBeginBlock(BasicBlock* block);

    void siEndBlock(BasicBlock* block);

    virtual void siUpdate();

    void siCheckVarScope(unsigned varNum, IL_OFFSET offs);

    void siCloseAllOpenScopes();

#ifdef DEBUG
    void siDispOpenScopes();
#endif

    /**************************************************************************
     *                          PROTECTED
     *************************************************************************/

protected:
    struct siScope
    {
        emitLocation scStartLoc; // emitter location of start of scope
        emitLocation scEndLoc;   // emitter location of end of scope

        unsigned scVarNum; // index into lvaTable
        unsigned scLVnum;  // 'which' in eeGetLVinfo()

        unsigned scStackLevel; // Only for stk-vars
        bool scAvailable : 1;  // It has a home / Home recycled - TODO-Cleanup: it appears this is unused (always true)

        siScope* scPrev;
        siScope* scNext;
    };

    siScope siOpenScopeList, siScopeList, *siOpenScopeLast, *siScopeLast;

    unsigned siScopeCnt;

    VARSET_TP siLastLife; // Life at last call to siUpdate()

    // Tracks the last entry for each tracked register variable

    siScope* siLatestTrackedScopes[lclMAX_TRACKED];

    IL_OFFSET siLastEndOffs; // IL offset of the (exclusive) end of the last block processed

#if FEATURE_EH_FUNCLETS
    bool siInFuncletRegion; // Have we seen the start of the funclet region?
#endif                      // FEATURE_EH_FUNCLETS

    // Functions

    siScope* siNewScope(unsigned LVnum, unsigned varNum);

    void siRemoveFromOpenScopeList(siScope* scope);

    void siEndTrackedScope(unsigned varIndex);

    void siEndScope(unsigned varNum);

    void siEndScope(siScope* scope);

#ifdef DEBUG
    bool siVerifyLocalVarTab();
#endif

#ifdef LATE_DISASM
public:
    /* virtual */
    const char* siRegVarName(size_t offs, size_t size, unsigned reg);

    /* virtual */
    const char* siStackVarName(size_t offs, size_t size, unsigned reg, unsigned stkOffs);
#endif // LATE_DISASM

public:
    /*
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XX                                                                           XX
    XX                          PrologScopeInfo                                  XX
    XX                                                                           XX
    XX We need special handling in the prolog block, as the parameter variables  XX
    XX may not be in the same position described by genLclVarTable - they all    XX
    XX start out on the stack                                                    XX
    XX                                                                           XX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    */

public:
    void psiBegProlog();

    void psiAdjustStackLevel(unsigned size);

    void psiMoveESPtoEBP();

    void psiMoveToReg(unsigned varNum, regNumber reg = REG_NA, regNumber otherReg = REG_NA);

    void psiMoveToStack(unsigned varNum);

    void psiEndProlog();

    /**************************************************************************
     *                          PROTECTED
     *************************************************************************/

protected:
    struct psiScope
    {
        emitLocation scStartLoc; // emitter location of start of scope
        emitLocation scEndLoc;   // emitter location of end of scope

        unsigned scSlotNum; // index into lclVarTab
        unsigned scLVnum;   // 'which' in eeGetLVinfo()

        bool scRegister;

        union {
            struct
            {
                regNumberSmall scRegNum;

                // Used for:
                //  - "other half" of long var on architectures with 32 bit size registers - x86.
                //  - for System V structs it stores the second register
                //    used to pass a register passed struct.
                regNumberSmall scOtherReg;
            } u1;

            struct
            {
                regNumberSmall scBaseReg;
                NATIVE_OFFSET  scOffset;
            } u2;
        };

        psiScope* scPrev;
        psiScope* scNext;
    };

    psiScope psiOpenScopeList, psiScopeList, *psiOpenScopeLast, *psiScopeLast;

    unsigned psiScopeCnt;

    // Implementation Functions

    psiScope* psiNewPrologScope(unsigned LVnum, unsigned slotNum);

    void psiEndPrologScope(psiScope* scope);

    void psSetScopeOffset(psiScope* newScope, LclVarDsc* lclVarDsc1);

/*****************************************************************************
 *                        TrnslLocalVarInfo
 *
 * This struct holds the LocalVarInfo in terms of the generated native code
 * after a call to genSetScopeInfo()
 */

#ifdef DEBUG

    struct TrnslLocalVarInfo
    {
        unsigned           tlviVarNum;
        unsigned           tlviLVnum;
        VarName            tlviName;
        UNATIVE_OFFSET     tlviStartPC;
        size_t             tlviLength;
        bool               tlviAvailable;
        Compiler::siVarLoc tlviVarLoc;
    };

    // Array of scopes of LocalVars in terms of native code

    TrnslLocalVarInfo* genTrnslLocalVarInfo;
    unsigned           genTrnslLocalVarCount;
#endif

/*****************************************************************************/
#endif // DEBUGGING_SUPPORT
/*****************************************************************************/

#ifndef LEGACY_BACKEND
#include "codegenlinear.h"
#else // LEGACY_BACKEND
#include "codegenclassic.h"
#endif // LEGACY_BACKEND

    /*
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XX                                                                           XX
    XX                           Instruction                                     XX
    XX                                                                           XX
    XX  The interface to generate a machine-instruction.                         XX
    XX  Currently specific to x86                                                XX
    XX  TODO-Cleanup: Consider factoring this out of CodeGen                     XX
    XX                                                                           XX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    */

public:
    void instInit();

    regNumber genGetZeroRegister();

    void instGen(instruction ins);
#ifdef _TARGET_XARCH_
    void instNop(unsigned size);
#endif

    void inst_JMP(emitJumpKind jmp, BasicBlock* tgtBlock);

    void inst_SET(emitJumpKind condition, regNumber reg);

    void inst_RV(instruction ins, regNumber reg, var_types type, emitAttr size = EA_UNKNOWN);

    void inst_RV_RV(instruction ins,
                    regNumber   reg1,
                    regNumber   reg2,
                    var_types   type  = TYP_I_IMPL,
                    emitAttr    size  = EA_UNKNOWN,
                    insFlags    flags = INS_FLAGS_DONT_CARE);

    void inst_RV_RV_RV(instruction ins,
                       regNumber   reg1,
                       regNumber   reg2,
                       regNumber   reg3,
                       emitAttr    size,
                       insFlags    flags = INS_FLAGS_DONT_CARE);

    void inst_IV(instruction ins, int val);
    void inst_IV_handle(instruction ins, int val);
    void inst_FS(instruction ins, unsigned stk = 0);

    void inst_RV_IV(instruction ins, regNumber reg, ssize_t val, emitAttr size, insFlags flags = INS_FLAGS_DONT_CARE);

    void inst_ST_RV(instruction ins, TempDsc* tmp, unsigned ofs, regNumber reg, var_types type);
    void inst_ST_IV(instruction ins, TempDsc* tmp, unsigned ofs, int val, var_types type);

    void inst_SA_RV(instruction ins, unsigned ofs, regNumber reg, var_types type);
    void inst_SA_IV(instruction ins, unsigned ofs, int val, var_types type);

    void inst_RV_ST(
        instruction ins, regNumber reg, TempDsc* tmp, unsigned ofs, var_types type, emitAttr size = EA_UNKNOWN);
    void inst_FS_ST(instruction ins, emitAttr size, TempDsc* tmp, unsigned ofs);

    void instEmit_indCall(GenTreePtr call,
                          size_t     argSize,
                          emitAttr retSize MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(emitAttr secondRetSize));

    void instEmit_RM(instruction ins, GenTreePtr tree, GenTreePtr addr, unsigned offs);

    void instEmit_RM_RV(instruction ins, emitAttr size, GenTreePtr tree, regNumber reg, unsigned offs);

    void instEmit_RV_RM(instruction ins, emitAttr size, regNumber reg, GenTreePtr tree, unsigned offs);

    void instEmit_RV_RIA(instruction ins, regNumber reg1, regNumber reg2, unsigned offs);

    void inst_TT(instruction ins, GenTreePtr tree, unsigned offs = 0, int shfv = 0, emitAttr size = EA_UNKNOWN);

    void inst_TT_RV(instruction ins,
                    GenTreePtr  tree,
                    regNumber   reg,
                    unsigned    offs  = 0,
                    emitAttr    size  = EA_UNKNOWN,
                    insFlags    flags = INS_FLAGS_DONT_CARE);

    void inst_TT_IV(instruction ins,
                    GenTreePtr  tree,
                    ssize_t     val,
                    unsigned    offs  = 0,
                    emitAttr    size  = EA_UNKNOWN,
                    insFlags    flags = INS_FLAGS_DONT_CARE);

    void inst_RV_AT(instruction ins,
                    emitAttr    size,
                    var_types   type,
                    regNumber   reg,
                    GenTreePtr  tree,
                    unsigned    offs  = 0,
                    insFlags    flags = INS_FLAGS_DONT_CARE);

    void inst_AT_IV(instruction ins, emitAttr size, GenTreePtr baseTree, int icon, unsigned offs = 0);

    void inst_RV_TT(instruction ins,
                    regNumber   reg,
                    GenTreePtr  tree,
                    unsigned    offs  = 0,
                    emitAttr    size  = EA_UNKNOWN,
                    insFlags    flags = INS_FLAGS_DONT_CARE);

    void inst_RV_TT_IV(instruction ins, regNumber reg, GenTreePtr tree, int val);

    void inst_FS_TT(instruction ins, GenTreePtr tree);

    void inst_RV_SH(instruction ins, emitAttr size, regNumber reg, unsigned val, insFlags flags = INS_FLAGS_DONT_CARE);

    void inst_TT_SH(instruction ins, GenTreePtr tree, unsigned val, unsigned offs = 0);

    void inst_RV_CL(instruction ins, regNumber reg, var_types type = TYP_I_IMPL);

    void inst_TT_CL(instruction ins, GenTreePtr tree, unsigned offs = 0);

#if defined(_TARGET_XARCH_)
    void inst_RV_RV_IV(instruction ins, emitAttr size, regNumber reg1, regNumber reg2, unsigned ival);
#endif

    void inst_RV_RR(instruction ins, emitAttr size, regNumber reg1, regNumber reg2);

    void inst_RV_ST(instruction ins, emitAttr size, regNumber reg, GenTreePtr tree);

    void inst_mov_RV_ST(regNumber reg, GenTreePtr tree);

    void instGetAddrMode(GenTreePtr addr, regNumber* baseReg, unsigned* indScale, regNumber* indReg, unsigned* cns);

    void inst_set_SV_var(GenTreePtr tree);

#ifdef _TARGET_ARM_
    bool arm_Valid_Imm_For_Instr(instruction ins, ssize_t imm, insFlags flags);
    bool arm_Valid_Disp_For_LdSt(ssize_t disp, var_types type);
    bool arm_Valid_Imm_For_Alu(ssize_t imm);
    bool arm_Valid_Imm_For_Mov(ssize_t imm);
    bool arm_Valid_Imm_For_Small_Mov(regNumber reg, ssize_t imm, insFlags flags);
    bool arm_Valid_Imm_For_Add(ssize_t imm, insFlags flag);
    bool arm_Valid_Imm_For_Add_SP(ssize_t imm);
    bool arm_Valid_Imm_For_BL(ssize_t addr);

    bool ins_Writes_Dest(instruction ins);
#endif

    bool isMoveIns(instruction ins);
    instruction ins_Move_Extend(var_types srcType, bool srcInReg);

    instruction ins_Copy(var_types dstType);
    instruction ins_CopyIntToFloat(var_types srcType, var_types dstTyp);
    instruction ins_CopyFloatToInt(var_types srcType, var_types dstTyp);
    static instruction ins_FloatStore(var_types type = TYP_DOUBLE);
    static instruction ins_FloatCopy(var_types type = TYP_DOUBLE);
    instruction ins_FloatConv(var_types to, var_types from);
    instruction ins_FloatCompare(var_types type);
    instruction ins_MathOp(genTreeOps oper, var_types type);
    instruction ins_FloatSqrt(var_types type);

    void instGen_Return(unsigned stkArgSize);

    void instGen_MemoryBarrier();

    void instGen_Set_Reg_To_Zero(emitAttr size, regNumber reg, insFlags flags = INS_FLAGS_DONT_CARE);

    void instGen_Set_Reg_To_Imm(emitAttr size, regNumber reg, ssize_t imm, insFlags flags = INS_FLAGS_DONT_CARE);

    void instGen_Compare_Reg_To_Zero(emitAttr size, regNumber reg);

    void instGen_Compare_Reg_To_Reg(emitAttr size, regNumber reg1, regNumber reg2);

    void instGen_Compare_Reg_To_Imm(emitAttr size, regNumber reg, ssize_t imm);

    void instGen_Load_Reg_From_Lcl(var_types srcType, regNumber dstReg, int varNum, int offs);

    void instGen_Store_Reg_Into_Lcl(var_types dstType, regNumber srcReg, int varNum, int offs);

    void instGen_Store_Imm_Into_Lcl(
        var_types dstType, emitAttr sizeAttr, ssize_t imm, int varNum, int offs, regNumber regToUse = REG_NA);

#ifdef DEBUG
    void __cdecl instDisp(instruction ins, bool noNL, const char* fmt, ...);
#endif

#ifdef _TARGET_XARCH_
    instruction genMapShiftInsToShiftByConstantIns(instruction ins, int shiftByValue);
#endif // _TARGET_XARCH_
};

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                       Instruction                                         XX
XX                      Inline functions                                     XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#ifdef _TARGET_XARCH_
/*****************************************************************************
 *
 *  Generate a floating-point instruction that has one operand given by
 *  a tree (which has been made addressable).
 */

inline void CodeGen::inst_FS_TT(instruction ins, GenTreePtr tree)
{
    assert(instIsFP(ins));

    assert(varTypeIsFloating(tree->gtType));

    inst_TT(ins, tree, 0);
}
#endif

/*****************************************************************************
 *
 *  Generate a "shift reg, cl" instruction.
 */

inline void CodeGen::inst_RV_CL(instruction ins, regNumber reg, var_types type)
{
    inst_RV(ins, reg, type);
}

#endif // _CODEGEN_H_
