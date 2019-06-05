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
#include "codegeninterface.h"
#include "compiler.h" // temporary??
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
    virtual bool genCreateAddrMode(GenTree*  addr,
                                   bool      fold,
                                   bool*     revPtr,
                                   GenTree** rv1Ptr,
                                   GenTree** rv2Ptr,
#if SCALED_ADDR_MODES
                                   unsigned* mulPtr,
#endif // SCALED_ADDR_MODES
                                   ssize_t* cnsPtr);

private:
#if defined(_TARGET_XARCH_)
    // Bit masks used in negating a float or double number.
    // This is to avoid creating more than one data constant for these bitmasks when a
    // method has more than one GT_NEG operation on floating point values.
    CORINFO_FIELD_HANDLE negBitmaskFlt;
    CORINFO_FIELD_HANDLE negBitmaskDbl;

    // Bit masks used in computing Math.Abs() of a float or double number.
    CORINFO_FIELD_HANDLE absBitmaskFlt;
    CORINFO_FIELD_HANDLE absBitmaskDbl;

    // Bit mask used in U8 -> double conversion to adjust the result.
    CORINFO_FIELD_HANDLE u8ToDblBitmask;

    // Generates SSE2 code for the given tree as "Operand BitWiseOp BitMask"
    void genSSE2BitwiseOp(GenTree* treeNode);

    // Generates SSE41 code for the given tree as a round operation
    void genSSE41RoundOp(GenTreeOp* treeNode);
#endif // defined(_TARGET_XARCH_)

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

    static bool genShouldRoundFP();

    GenTreeIndir indirForm(var_types type, GenTree* base);
    GenTreeStoreInd storeIndirForm(var_types type, GenTree* base, GenTree* data);

    GenTreeIntCon intForm(var_types type, ssize_t value);

    void genRangeCheck(GenTree* node);

    void genLockedInstructions(GenTreeOp* node);
#ifdef _TARGET_XARCH_
    void genCodeForLockAdd(GenTreeOp* node);
#endif

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

    //-------------------------------------------------------------------------

    bool     genUseBlockInit;  // true if we plan to block-initialize the local stack frame
    unsigned genInitStkLclCnt; // The count of local variables that we need to zero init

    void SubtractStackLevel(unsigned adjustment)
    {
        assert(genStackLevel >= adjustment);
        unsigned newStackLevel = genStackLevel - adjustment;
        if (genStackLevel != newStackLevel)
        {
            JITDUMP("Adjusting stack level from %d to %d\n", genStackLevel, newStackLevel);
        }
        genStackLevel = newStackLevel;
    }

    void AddStackLevel(unsigned adjustment)
    {
        unsigned newStackLevel = genStackLevel + adjustment;
        if (genStackLevel != newStackLevel)
        {
            JITDUMP("Adjusting stack level from %d to %d\n", genStackLevel, newStackLevel);
        }
        genStackLevel = newStackLevel;
    }

    void SetStackLevel(unsigned newStackLevel)
    {
        if (genStackLevel != newStackLevel)
        {
            JITDUMP("Setting stack level from %d to %d\n", genStackLevel, newStackLevel);
        }
        genStackLevel = newStackLevel;
    }

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
#endif // DEBUG

    void genInitialize();

    void genInitializeRegisterState();

    void genCodeForBBlist();

public:
    void genSpillVar(GenTree* tree);

protected:
    void genEmitHelperCall(unsigned helper, int argSize, emitAttr retSize, regNumber callTarget = REG_NA);

    void genGCWriteBarrier(GenTree* tgt, GCInfo::WriteBarrierForm wbf);

    BasicBlock* genCreateTempLabel();

    void genDefineTempLabel(BasicBlock* label);

    void genAdjustSP(target_ssize_t delta);

    void genAdjustStackLevel(BasicBlock* block);

    void genExitCode(BasicBlock* block);

    void genJumpToThrowHlpBlk(emitJumpKind jumpKind, SpecialCodeKind codeKind, BasicBlock* failBlk = nullptr);

    void genCheckOverflow(GenTree* tree);

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
#if defined(UNIX_AMD64_ABI) && defined(FEATURE_SIMD)
    void genClearStackVec3ArgUpperBits();
#endif // UNIX_AMD64_ABI && FEATURE_SIMD

#if defined(_TARGET_ARM64_)
    bool genInstrWithConstant(instruction ins,
                              emitAttr    attr,
                              regNumber   reg1,
                              regNumber   reg2,
                              ssize_t     imm,
                              regNumber   tmpReg,
                              bool        inUnwindRegion = false);

    void genStackPointerAdjustment(ssize_t spAdjustment, regNumber tmpReg, bool* pTmpRegIsZero, bool reportUnwindData);

    void genPrologSaveRegPair(regNumber reg1,
                              regNumber reg2,
                              int       spOffset,
                              int       spDelta,
                              bool      useSaveNextPair,
                              regNumber tmpReg,
                              bool*     pTmpRegIsZero);

    void genPrologSaveReg(regNumber reg1, int spOffset, int spDelta, regNumber tmpReg, bool* pTmpRegIsZero);

    void genEpilogRestoreRegPair(regNumber reg1,
                                 regNumber reg2,
                                 int       spOffset,
                                 int       spDelta,
                                 bool      useSaveNextPair,
                                 regNumber tmpReg,
                                 bool*     pTmpRegIsZero);

    void genEpilogRestoreReg(regNumber reg1, int spOffset, int spDelta, regNumber tmpReg, bool* pTmpRegIsZero);

    // A simple struct to keep register pairs for prolog and epilog.
    struct RegPair
    {
        regNumber reg1;
        regNumber reg2;
        bool      useSaveNextPair;

        RegPair(regNumber reg1) : reg1(reg1), reg2(REG_NA), useSaveNextPair(false)
        {
        }

        RegPair(regNumber reg1, regNumber reg2) : reg1(reg1), reg2(reg2), useSaveNextPair(false)
        {
            assert(reg2 == REG_NEXT(reg1));
        }
    };

    static void genBuildRegPairsStack(regMaskTP regsMask, ArrayStack<RegPair>* regStack);
    static void genSetUseSaveNextPairs(ArrayStack<RegPair>* regStack);

    static int genGetSlotSizeForRegsInMask(regMaskTP regsMask);

    void genSaveCalleeSavedRegisterGroup(regMaskTP regsMask, int spDelta, int spOffset);
    void genRestoreCalleeSavedRegisterGroup(regMaskTP regsMask, int spDelta, int spOffset);

    void genSaveCalleeSavedRegistersHelp(regMaskTP regsToSaveMask, int lowestCalleeSavedOffset, int spDelta);
    void genRestoreCalleeSavedRegistersHelp(regMaskTP regsToRestoreMask, int lowestCalleeSavedOffset, int spDelta);

    void genPushCalleeSavedRegisters(regNumber initReg, bool* pInitRegZeroed);
#else
    void genPushCalleeSavedRegisters();
#endif

    void genAllocLclFrame(unsigned frameSize, regNumber initReg, bool* pInitRegZeroed, regMaskTP maskArgRegsLiveIn);

#if defined(_TARGET_ARM_)

    bool genInstrWithConstant(
        instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, ssize_t imm, insFlags flags, regNumber tmpReg);

    bool genStackPointerAdjustment(ssize_t spAdjustment, regNumber tmpReg);

    void genPushFltRegs(regMaskTP regMask);
    void genPopFltRegs(regMaskTP regMask);
    regMaskTP genStackAllocRegisterMask(unsigned frameSize, regMaskTP maskCalleeSavedFloat);

    regMaskTP genJmpCallArgMask();

    void genFreeLclFrame(unsigned           frameSize,
                         /* IN OUT */ bool* pUnwindStarted,
                         bool               jmpEpilog);

    void genMov32RelocatableDisplacement(BasicBlock* block, regNumber reg);
    void genMov32RelocatableDataLabel(unsigned value, regNumber reg);
    void genMov32RelocatableImmediate(emitAttr size, BYTE* addr, regNumber reg);

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

#if defined(_TARGET_XARCH_)

    // Save/Restore callee saved float regs to stack
    void genPreserveCalleeSavedFltRegs(unsigned lclFrameSize);
    void genRestoreCalleeSavedFltRegs(unsigned lclFrameSize);
    // Generate VZeroupper instruction to avoid AVX/SSE transition penalty
    void genVzeroupperIfNeeded(bool check256bitOnly = true);

#endif // _TARGET_XARCH_

    void genZeroInitFltRegs(const regMaskTP& initFltRegs, const regMaskTP& initDblRegs, const regNumber& initReg);

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

    // clang-format off
    void genEmitCall(int                   callType,
                     CORINFO_METHOD_HANDLE methHnd,
                     INDEBUG_LDISASM_COMMA(CORINFO_SIG_INFO* sigInfo)
                     void*                 addr
                     X86_ARG(int  argSize),
                     emitAttr              retSize
                     MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(emitAttr secondRetSize),
                     IL_OFFSETX            ilOffset,
                     regNumber             base   = REG_NA,
                     bool                  isJump = false);
    // clang-format on

    // clang-format off
    void genEmitCall(int                   callType,
                     CORINFO_METHOD_HANDLE methHnd,
                     INDEBUG_LDISASM_COMMA(CORINFO_SIG_INFO* sigInfo)
                     GenTreeIndir*         indir
                     X86_ARG(int  argSize),
                     emitAttr              retSize
                     MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(emitAttr secondRetSize),
                     IL_OFFSETX            ilOffset);
    // clang-format on

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
#if defined(_TARGET_ARM_)
    void genInsertNopForUnwinder(BasicBlock* block);
#endif

#else // FEATURE_EH_FUNCLETS

    // This is a no-op when there are no funclets!
    void genUpdateCurrentFunclet(BasicBlock* block)
    {
        return;
    }

#if defined(_TARGET_ARM_)
    void genInsertNopForUnwinder(BasicBlock* block)
    {
        return;
    }
#endif

#endif // FEATURE_EH_FUNCLETS

    void genGeneratePrologsAndEpilogs();

#if defined(DEBUG) && defined(_TARGET_ARM64_)
    void genArm64EmitterUnitTests();
#endif

#if defined(DEBUG) && defined(LATE_DISASM) && defined(_TARGET_AMD64_)
    void genAmd64EmitterUnitTests();
#endif

#ifdef _TARGET_ARM64_
    virtual void SetSaveFpLrWithAllCalleeSavedRegisters(bool value);
    virtual bool IsSaveFpLrWithAllCalleeSavedRegisters() const;
    bool         genSaveFpLrWithAllCalleeSavedRegisters;
#endif // _TARGET_ARM64_

    //-------------------------------------------------------------------------
    //
    // End prolog/epilog generation
    //
    //-------------------------------------------------------------------------

    void      genSinglePush();
    void      genSinglePop();
    regMaskTP genPushRegs(regMaskTP regs, regMaskTP* byrefRegs, regMaskTP* noRefRegs);
    void genPopRegs(regMaskTP regs, regMaskTP byrefRegs, regMaskTP noRefRegs);

/*
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                           Debugging Support                               XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

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

    void genSetScopeInfo(unsigned       which,
                         UNATIVE_OFFSET startOffs,
                         UNATIVE_OFFSET length,
                         unsigned       varNum,
                         unsigned       LVnum,
                         bool           avail,
                         siVarLoc*      varLoc);

    void genSetScopeInfo();
#ifdef USING_VARIABLE_LIVE_RANGE
    // Send VariableLiveRanges as debug info to the debugger
    void genSetScopeInfoUsingVariableRanges();
#endif // USING_VARIABLE_LIVE_RANGE

#ifdef USING_SCOPE_INFO
    void genSetScopeInfoUsingsiScope();

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

#endif // USING_VARIABLE_LIVE_RANGE
public:
    void siInit();
    void checkICodeDebugInfo();

    // The logic used to report debug info on debug code is the same for ScopeInfo and
    // VariableLiveRange
    void siBeginBlock(BasicBlock* block);
    void siEndBlock(BasicBlock* block);

    // VariableLiveRange and siScope needs this method to report variables on debug code
    void siOpenScopesForNonTrackedVars(const BasicBlock* block, unsigned int lastBlockILEndOffset);

protected:
#if FEATURE_EH_FUNCLETS
    bool siInFuncletRegion; // Have we seen the start of the funclet region?
#endif                      // FEATURE_EH_FUNCLETS

    IL_OFFSET siLastEndOffs; // IL offset of the (exclusive) end of the last block processed

#ifdef USING_SCOPE_INFO

public:
    // Closes the "ScopeInfo" of the tracked variables that has become dead.
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

    // Returns a "siVarLoc" instance representing the place where the variable lives base on
    // varDsc and scope description.
    CodeGenInterface::siVarLoc getSiVarLoc(const LclVarDsc* varDsc, const siScope* scope) const;

    siScope siOpenScopeList, siScopeList, *siOpenScopeLast, *siScopeLast;

    unsigned siScopeCnt;

    VARSET_TP siLastLife; // Life at last call to siUpdate()

    // Tracks the last entry for each tracked register variable

    siScope** siLatestTrackedScopes;

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
#endif // USING_SCOPE_INFO
public:
    void psiBegProlog();

    void psiEndProlog();

#ifdef USING_SCOPE_INFO
    void psiAdjustStackLevel(unsigned size);

    void psiMoveESPtoEBP();

    void psiMoveToReg(unsigned varNum, regNumber reg = REG_NA, regNumber otherReg = REG_NA);

    void psiMoveToStack(unsigned varNum);

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

        // Returns a "siVarLoc" instance representing the place where the variable lives base on
        // psiScope properties.
        CodeGenInterface::siVarLoc getSiVarLoc() const;
    };

    psiScope psiOpenScopeList, psiScopeList, *psiOpenScopeLast, *psiScopeLast;

    unsigned psiScopeCnt;

    // Implementation Functions

    psiScope* psiNewPrologScope(unsigned LVnum, unsigned slotNum);

    void psiEndPrologScope(psiScope* scope);

    void psiSetScopeOffset(psiScope* newScope, const LclVarDsc* lclVarDsc) const;
#endif // USING_SCOPE_INFO

    NATIVE_OFFSET psiGetVarStackOffset(const LclVarDsc* lclVarDsc) const;

    /*****************************************************************************
     *                        TrnslLocalVarInfo
     *
     * This struct holds the LocalVarInfo in terms of the generated native code
     * after a call to genSetScopeInfo()
     */

protected:
#ifdef DEBUG

    struct TrnslLocalVarInfo
    {
        unsigned       tlviVarNum;
        unsigned       tlviLVnum;
        VarName        tlviName;
        UNATIVE_OFFSET tlviStartPC;
        size_t         tlviLength;
        bool           tlviAvailable;
        siVarLoc       tlviVarLoc;
    };

    // Array of scopes of LocalVars in terms of native code

    TrnslLocalVarInfo* genTrnslLocalVarInfo;
    unsigned           genTrnslLocalVarCount;
#endif

    void genSetRegToConst(regNumber targetReg, var_types targetType, GenTree* tree);
    void genCodeForTreeNode(GenTree* treeNode);
    void genCodeForBinary(GenTreeOp* treeNode);

#if defined(_TARGET_X86_)
    void genCodeForLongUMod(GenTreeOp* node);
#endif // _TARGET_X86_

    void genCodeForDivMod(GenTreeOp* treeNode);
    void genCodeForMul(GenTreeOp* treeNode);
    void genCodeForMulHi(GenTreeOp* treeNode);
    void genLeaInstruction(GenTreeAddrMode* lea);
    void genSetRegToCond(regNumber dstReg, GenTree* tree);

#if defined(_TARGET_ARMARCH_)
    void genScaledAdd(emitAttr attr, regNumber targetReg, regNumber baseReg, regNumber indexReg, int scale);
#endif // _TARGET_ARMARCH_

#if defined(_TARGET_ARM_)
    void genCodeForMulLong(GenTreeMultiRegOp* treeNode);
#endif // _TARGET_ARM_

#if !defined(_TARGET_64BIT_)
    void genLongToIntCast(GenTree* treeNode);
#endif

    struct GenIntCastDesc
    {
        enum CheckKind
        {
            CHECK_NONE,
            CHECK_SMALL_INT_RANGE,
            CHECK_POSITIVE,
#ifdef _TARGET_64BIT_
            CHECK_UINT_RANGE,
            CHECK_POSITIVE_INT_RANGE,
            CHECK_INT_RANGE,
#endif
        };

        enum ExtendKind
        {
            COPY,
            ZERO_EXTEND_SMALL_INT,
            SIGN_EXTEND_SMALL_INT,
#ifdef _TARGET_64BIT_
            ZERO_EXTEND_INT,
            SIGN_EXTEND_INT,
#endif
        };

    private:
        CheckKind  m_checkKind;
        unsigned   m_checkSrcSize;
        int        m_checkSmallIntMin;
        int        m_checkSmallIntMax;
        ExtendKind m_extendKind;
        unsigned   m_extendSrcSize;

    public:
        GenIntCastDesc(GenTreeCast* cast);

        CheckKind CheckKind() const
        {
            return m_checkKind;
        }

        unsigned CheckSrcSize() const
        {
            assert(m_checkKind != CHECK_NONE);
            return m_checkSrcSize;
        }

        int CheckSmallIntMin() const
        {
            assert(m_checkKind == CHECK_SMALL_INT_RANGE);
            return m_checkSmallIntMin;
        }

        int CheckSmallIntMax() const
        {
            assert(m_checkKind == CHECK_SMALL_INT_RANGE);
            return m_checkSmallIntMax;
        }

        ExtendKind ExtendKind() const
        {
            return m_extendKind;
        }

        unsigned ExtendSrcSize() const
        {
            return m_extendSrcSize;
        }
    };

    void genIntCastOverflowCheck(GenTreeCast* cast, const GenIntCastDesc& desc, regNumber reg);
    void genIntToIntCast(GenTreeCast* cast);
    void genFloatToFloatCast(GenTree* treeNode);
    void genFloatToIntCast(GenTree* treeNode);
    void genIntToFloatCast(GenTree* treeNode);
    void genCkfinite(GenTree* treeNode);
    void genCodeForCompare(GenTreeOp* tree);
    void genIntrinsic(GenTree* treeNode);
    void genPutArgStk(GenTreePutArgStk* treeNode);
    void genPutArgReg(GenTreeOp* tree);
#if FEATURE_ARG_SPLIT
    void genPutArgSplit(GenTreePutArgSplit* treeNode);
#endif // FEATURE_ARG_SPLIT

#if defined(_TARGET_XARCH_)
    unsigned getBaseVarForPutArgStk(GenTree* treeNode);
#endif // _TARGET_XARCH_

    unsigned getFirstArgWithStackSlot();

    void genCompareFloat(GenTree* treeNode);
    void genCompareInt(GenTree* treeNode);

#ifdef FEATURE_SIMD
    enum SIMDScalarMoveType{
        SMT_ZeroInitUpper,                  // zero initlaize target upper bits
        SMT_ZeroInitUpper_SrcHasUpperZeros, // zero initialize target upper bits; source upper bits are known to be zero
        SMT_PreserveUpper                   // preserve target upper bits
    };

#ifdef _TARGET_ARM64_
    insOpts genGetSimdInsOpt(emitAttr size, var_types elementType);
#endif
    instruction getOpForSIMDIntrinsic(SIMDIntrinsicID intrinsicId, var_types baseType, unsigned* ival = nullptr);
    void genSIMDScalarMove(
        var_types targetType, var_types type, regNumber target, regNumber src, SIMDScalarMoveType moveType);
    void genSIMDZero(var_types targetType, var_types baseType, regNumber targetReg);
    void genSIMDIntrinsicInit(GenTreeSIMD* simdNode);
    void genSIMDIntrinsicInitN(GenTreeSIMD* simdNode);
    void genSIMDIntrinsicUnOp(GenTreeSIMD* simdNode);
    void genSIMDIntrinsicBinOp(GenTreeSIMD* simdNode);
    void genSIMDIntrinsicRelOp(GenTreeSIMD* simdNode);
    void genSIMDIntrinsicDotProduct(GenTreeSIMD* simdNode);
    void genSIMDIntrinsicSetItem(GenTreeSIMD* simdNode);
    void genSIMDIntrinsicGetItem(GenTreeSIMD* simdNode);
    void genSIMDIntrinsicShuffleSSE2(GenTreeSIMD* simdNode);
    void genSIMDIntrinsicUpperSave(GenTreeSIMD* simdNode);
    void genSIMDIntrinsicUpperRestore(GenTreeSIMD* simdNode);
    void genSIMDLo64BitConvert(SIMDIntrinsicID intrinsicID,
                               var_types       simdType,
                               var_types       baseType,
                               regNumber       tmpReg,
                               regNumber       tmpIntReg,
                               regNumber       targetReg);
    void genSIMDIntrinsic32BitConvert(GenTreeSIMD* simdNode);
    void genSIMDIntrinsic64BitConvert(GenTreeSIMD* simdNode);
    void genSIMDIntrinsicNarrow(GenTreeSIMD* simdNode);
    void genSIMDExtractUpperHalf(GenTreeSIMD* simdNode, regNumber srcReg, regNumber tgtReg);
    void genSIMDIntrinsicWiden(GenTreeSIMD* simdNode);
    void genSIMDIntrinsic(GenTreeSIMD* simdNode);

    // TYP_SIMD12 (i.e Vector3 of size 12 bytes) is not a hardware supported size and requires
    // two reads/writes on 64-bit targets. These routines abstract reading/writing of Vector3
    // values through an indirection. Note that Vector3 locals allocated on stack would have
    // their size rounded to TARGET_POINTER_SIZE (which is 8 bytes on 64-bit targets) and hence
    // Vector3 locals could be treated as TYP_SIMD16 while reading/writing.
    void genStoreIndTypeSIMD12(GenTree* treeNode);
    void genLoadIndTypeSIMD12(GenTree* treeNode);
    void genStoreLclTypeSIMD12(GenTree* treeNode);
    void genLoadLclTypeSIMD12(GenTree* treeNode);
#ifdef _TARGET_X86_
    void genStoreSIMD12ToStack(regNumber operandReg, regNumber tmpReg);
    void genPutArgStkSIMD12(GenTree* treeNode);
#endif // _TARGET_X86_
#endif // FEATURE_SIMD

#ifdef FEATURE_HW_INTRINSICS
    void genHWIntrinsic(GenTreeHWIntrinsic* node);
#if defined(_TARGET_XARCH_)
    void genHWIntrinsic_R_RM(GenTreeHWIntrinsic* node, instruction ins, emitAttr attr);
    void genHWIntrinsic_R_RM_I(GenTreeHWIntrinsic* node, instruction ins, int8_t ival);
    void genHWIntrinsic_R_R_RM(GenTreeHWIntrinsic* node, instruction ins, emitAttr attr);
    void genHWIntrinsic_R_R_RM(
        GenTreeHWIntrinsic* node, instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, GenTree* op2);
    void genHWIntrinsic_R_R_RM_I(GenTreeHWIntrinsic* node, instruction ins, int8_t ival);
    void genHWIntrinsic_R_R_RM_R(GenTreeHWIntrinsic* node, instruction ins);
    void genHWIntrinsic_R_R_R_RM(
        instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, regNumber op2Reg, GenTree* op3);
    void genBaseIntrinsic(GenTreeHWIntrinsic* node);
    void genSSEIntrinsic(GenTreeHWIntrinsic* node);
    void genSSE2Intrinsic(GenTreeHWIntrinsic* node);
    void genSSE41Intrinsic(GenTreeHWIntrinsic* node);
    void genSSE42Intrinsic(GenTreeHWIntrinsic* node);
    void genAvxOrAvx2Intrinsic(GenTreeHWIntrinsic* node);
    void genAESIntrinsic(GenTreeHWIntrinsic* node);
    void genBMI1OrBMI2Intrinsic(GenTreeHWIntrinsic* node);
    void genFMAIntrinsic(GenTreeHWIntrinsic* node);
    void genLZCNTIntrinsic(GenTreeHWIntrinsic* node);
    void genPCLMULQDQIntrinsic(GenTreeHWIntrinsic* node);
    void genPOPCNTIntrinsic(GenTreeHWIntrinsic* node);
    void genXCNTIntrinsic(GenTreeHWIntrinsic* node, instruction ins);
    template <typename HWIntrinsicSwitchCaseBody>
    void genHWIntrinsicJumpTableFallback(NamedIntrinsic            intrinsic,
                                         regNumber                 nonConstImmReg,
                                         regNumber                 baseReg,
                                         regNumber                 offsReg,
                                         HWIntrinsicSwitchCaseBody emitSwCase);
#endif // defined(_TARGET_XARCH_)
#if defined(_TARGET_ARM64_)
    instruction getOpForHWIntrinsic(GenTreeHWIntrinsic* node, var_types instrType);
    void genHWIntrinsicUnaryOp(GenTreeHWIntrinsic* node);
    void genHWIntrinsicCrcOp(GenTreeHWIntrinsic* node);
    void genHWIntrinsicSimdBinaryOp(GenTreeHWIntrinsic* node);
    void genHWIntrinsicSimdExtractOp(GenTreeHWIntrinsic* node);
    void genHWIntrinsicSimdInsertOp(GenTreeHWIntrinsic* node);
    void genHWIntrinsicSimdSelectOp(GenTreeHWIntrinsic* node);
    void genHWIntrinsicSimdSetAllOp(GenTreeHWIntrinsic* node);
    void genHWIntrinsicSimdUnaryOp(GenTreeHWIntrinsic* node);
    void genHWIntrinsicSimdBinaryRMWOp(GenTreeHWIntrinsic* node);
    void genHWIntrinsicSimdTernaryRMWOp(GenTreeHWIntrinsic* node);
    void genHWIntrinsicShaHashOp(GenTreeHWIntrinsic* node);
    void genHWIntrinsicShaRotateOp(GenTreeHWIntrinsic* node);
    template <typename HWIntrinsicSwitchCaseBody>
    void genHWIntrinsicSwitchTable(regNumber swReg, regNumber tmpReg, int swMax, HWIntrinsicSwitchCaseBody emitSwCase);
#endif // defined(_TARGET_XARCH_)
#endif // FEATURE_HW_INTRINSICS

#if !defined(_TARGET_64BIT_)

    // CodeGen for Long Ints

    void genStoreLongLclVar(GenTree* treeNode);

#endif // !defined(_TARGET_64BIT_)

    void genProduceReg(GenTree* tree);
    void genUnspillRegIfNeeded(GenTree* tree);
    regNumber genConsumeReg(GenTree* tree);
    void genCopyRegIfNeeded(GenTree* tree, regNumber needReg);
    void genConsumeRegAndCopy(GenTree* tree, regNumber needReg);

    void genConsumeIfReg(GenTree* tree)
    {
        if (!tree->isContained())
        {
            (void)genConsumeReg(tree);
        }
    }

    void genRegCopy(GenTree* tree);
    void genTransferRegGCState(regNumber dst, regNumber src);
    void genConsumeAddress(GenTree* addr);
    void genConsumeAddrMode(GenTreeAddrMode* mode);
    void genSetBlockSize(GenTreeBlk* blkNode, regNumber sizeReg);
    void genConsumeBlockSrc(GenTreeBlk* blkNode);
    void genSetBlockSrc(GenTreeBlk* blkNode, regNumber srcReg);
    void genConsumeBlockOp(GenTreeBlk* blkNode, regNumber dstReg, regNumber srcReg, regNumber sizeReg);

#ifdef FEATURE_PUT_STRUCT_ARG_STK
    void genConsumePutStructArgStk(GenTreePutArgStk* putArgStkNode,
                                   regNumber         dstReg,
                                   regNumber         srcReg,
                                   regNumber         sizeReg);
#endif // FEATURE_PUT_STRUCT_ARG_STK
#if FEATURE_ARG_SPLIT
    void genConsumeArgSplitStruct(GenTreePutArgSplit* putArgNode);
#endif // FEATURE_ARG_SPLIT

    void genConsumeRegs(GenTree* tree);
    void genConsumeOperands(GenTreeOp* tree);
#ifdef FEATURE_HW_INTRINSICS
    void genConsumeHWIntrinsicOperands(GenTreeHWIntrinsic* tree);
#endif // FEATURE_HW_INTRINSICS
    void genEmitGSCookieCheck(bool pushReg);
    void genSetRegToIcon(regNumber reg, ssize_t val, var_types type = TYP_INT, insFlags flags = INS_FLAGS_DONT_CARE);
    void genCodeForShift(GenTree* tree);

#if defined(_TARGET_X86_) || defined(_TARGET_ARM_)
    void genCodeForShiftLong(GenTree* tree);
#endif

#ifdef _TARGET_XARCH_
    void genCodeForShiftRMW(GenTreeStoreInd* storeInd);
    void genCodeForBT(GenTreeOp* bt);
#endif // _TARGET_XARCH_

    void genCodeForCast(GenTreeOp* tree);
    void genCodeForLclAddr(GenTree* tree);
    void genCodeForIndexAddr(GenTreeIndexAddr* tree);
    void genCodeForIndir(GenTreeIndir* tree);
    void genCodeForNegNot(GenTree* tree);
    void genCodeForBswap(GenTree* tree);
    void genCodeForLclVar(GenTreeLclVar* tree);
    void genCodeForLclFld(GenTreeLclFld* tree);
    void genCodeForStoreLclFld(GenTreeLclFld* tree);
    void genCodeForStoreLclVar(GenTreeLclVar* tree);
    void genCodeForReturnTrap(GenTreeOp* tree);
    void genCodeForJcc(GenTreeCC* tree);
    void genCodeForSetcc(GenTreeCC* setcc);
    void genCodeForStoreInd(GenTreeStoreInd* tree);
    void genCodeForSwap(GenTreeOp* tree);
    void genCodeForCpObj(GenTreeObj* cpObjNode);
    void genCodeForCpBlkRepMovs(GenTreeBlk* cpBlkNode);
    void genCodeForCpBlkUnroll(GenTreeBlk* cpBlkNode);
#ifndef _TARGET_X86_
    void genCodeForCpBlkHelper(GenTreeBlk* cpBlkNode);
#endif
    void genCodeForPhysReg(GenTreePhysReg* tree);
    void genCodeForNullCheck(GenTreeOp* tree);
    void genCodeForCmpXchg(GenTreeCmpXchg* tree);

    void genAlignStackBeforeCall(GenTreePutArgStk* putArgStk);
    void genAlignStackBeforeCall(GenTreeCall* call);
    void genRemoveAlignmentAfterCall(GenTreeCall* call, unsigned bias = 0);

#if defined(UNIX_X86_ABI)

    unsigned curNestedAlignment; // Keep track of alignment adjustment required during codegen.
    unsigned maxNestedAlignment; // The maximum amount of alignment adjustment required.

    void SubtractNestedAlignment(unsigned adjustment)
    {
        assert(curNestedAlignment >= adjustment);
        unsigned newNestedAlignment = curNestedAlignment - adjustment;
        if (curNestedAlignment != newNestedAlignment)
        {
            JITDUMP("Adjusting stack nested alignment from %d to %d\n", curNestedAlignment, newNestedAlignment);
        }
        curNestedAlignment = newNestedAlignment;
    }

    void AddNestedAlignment(unsigned adjustment)
    {
        unsigned newNestedAlignment = curNestedAlignment + adjustment;
        if (curNestedAlignment != newNestedAlignment)
        {
            JITDUMP("Adjusting stack nested alignment from %d to %d\n", curNestedAlignment, newNestedAlignment);
        }
        curNestedAlignment = newNestedAlignment;

        if (curNestedAlignment > maxNestedAlignment)
        {
            JITDUMP("Max stack nested alignment changed from %d to %d\n", maxNestedAlignment, curNestedAlignment);
            maxNestedAlignment = curNestedAlignment;
        }
    }

#endif

#ifndef _TARGET_X86_
    void genPutArgStkFieldList(GenTreePutArgStk* putArgStk, unsigned outArgVarNum);
#endif // !_TARGET_X86_

#ifdef FEATURE_PUT_STRUCT_ARG_STK
#ifdef _TARGET_X86_
    bool genAdjustStackForPutArgStk(GenTreePutArgStk* putArgStk);
    void genPushReg(var_types type, regNumber srcReg);
    void genPutArgStkFieldList(GenTreePutArgStk* putArgStk);
#endif // _TARGET_X86_

    void genPutStructArgStk(GenTreePutArgStk* treeNode);

    unsigned genMove8IfNeeded(unsigned size, regNumber tmpReg, GenTree* srcAddr, unsigned offset);
    unsigned genMove4IfNeeded(unsigned size, regNumber tmpReg, GenTree* srcAddr, unsigned offset);
    unsigned genMove2IfNeeded(unsigned size, regNumber tmpReg, GenTree* srcAddr, unsigned offset);
    unsigned genMove1IfNeeded(unsigned size, regNumber tmpReg, GenTree* srcAddr, unsigned offset);
    void genStructPutArgRepMovs(GenTreePutArgStk* putArgStkNode);
    void genStructPutArgUnroll(GenTreePutArgStk* putArgStkNode);
    void genStoreRegToStackArg(var_types type, regNumber reg, int offset);
#endif // FEATURE_PUT_STRUCT_ARG_STK

    void genCodeForLoadOffset(instruction ins, emitAttr size, regNumber dst, GenTree* base, unsigned offset);
    void genCodeForStoreOffset(instruction ins, emitAttr size, regNumber src, GenTree* base, unsigned offset);

#ifdef _TARGET_ARM64_
    void genCodeForLoadPairOffset(regNumber dst, regNumber dst2, GenTree* base, unsigned offset);
    void genCodeForStorePairOffset(regNumber src, regNumber src2, GenTree* base, unsigned offset);
#endif // _TARGET_ARM64_

    void genCodeForStoreBlk(GenTreeBlk* storeBlkNode);
#ifndef _TARGET_X86_
    void genCodeForInitBlkHelper(GenTreeBlk* initBlkNode);
#endif
    void genCodeForInitBlkRepStos(GenTreeBlk* initBlkNode);
    void genCodeForInitBlkUnroll(GenTreeBlk* initBlkNode);
    void genJumpTable(GenTree* tree);
    void genTableBasedSwitch(GenTree* tree);
    void genCodeForArrIndex(GenTreeArrIndex* treeNode);
    void genCodeForArrOffset(GenTreeArrOffs* treeNode);
    instruction genGetInsForOper(genTreeOps oper, var_types type);
    bool genEmitOptimizedGCWriteBarrier(GCInfo::WriteBarrierForm writeBarrierForm, GenTree* addr, GenTree* data);
    void genCallInstruction(GenTreeCall* call);
    void genJmpMethod(GenTree* jmp);
    BasicBlock* genCallFinally(BasicBlock* block);
    void genCodeForJumpTrue(GenTreeOp* jtrue);
#ifdef _TARGET_ARM64_
    void genCodeForJumpCompare(GenTreeOp* tree);
#endif // _TARGET_ARM64_

#if FEATURE_EH_FUNCLETS
    void genEHCatchRet(BasicBlock* block);
#else  // !FEATURE_EH_FUNCLETS
    void genEHFinallyOrFilterRet(BasicBlock* block);
#endif // !FEATURE_EH_FUNCLETS

    void genMultiRegCallStoreToLocal(GenTree* treeNode);

    // Deals with codegen for muti-register struct returns.
    bool isStructReturn(GenTree* treeNode);
    void genStructReturn(GenTree* treeNode);

#if defined(_TARGET_X86_) || defined(_TARGET_ARM_)
    void genLongReturn(GenTree* treeNode);
#endif // _TARGET_X86_ ||  _TARGET_ARM_

#if defined(_TARGET_X86_)
    void genFloatReturn(GenTree* treeNode);
#endif // _TARGET_X86_

#if defined(_TARGET_ARM64_)
    void genSimpleReturn(GenTree* treeNode);
#endif // _TARGET_ARM64_

    void genReturn(GenTree* treeNode);

#ifdef _TARGET_ARMARCH_
    void genStackPointerConstantAdjustment(ssize_t spDelta);
#else  // !_TARGET_ARMARCH_
    void genStackPointerConstantAdjustment(ssize_t spDelta, regNumber regTmp);
#endif // !_TARGET_ARMARCH_

    void genStackPointerConstantAdjustmentWithProbe(ssize_t spDelta, regNumber regTmp);
    target_ssize_t genStackPointerConstantAdjustmentLoopWithProbe(ssize_t spDelta, regNumber regTmp);

#if defined(_TARGET_XARCH_)
    void genStackPointerDynamicAdjustmentWithProbe(regNumber regSpDelta, regNumber regTmp);
#endif // defined(_TARGET_XARCH_)

    void genLclHeap(GenTree* tree);

    bool genIsRegCandidateLocal(GenTree* tree)
    {
        if (!tree->IsLocal())
        {
            return false;
        }
        const LclVarDsc* varDsc = &compiler->lvaTable[tree->gtLclVarCommon.gtLclNum];
        return (varDsc->lvIsRegCandidate());
    }

#ifdef FEATURE_PUT_STRUCT_ARG_STK
#ifdef _TARGET_X86_
    bool m_pushStkArg;
#else  // !_TARGET_X86_
    unsigned m_stkArgVarNum;
    unsigned m_stkArgOffset;
#endif // !_TARGET_X86_
#endif // !FEATURE_PUT_STRUCT_ARG_STK

#if defined(DEBUG) && defined(_TARGET_XARCH_)
    void genStackPointerCheck(bool doStackPointerCheck, unsigned lvaStackPointerVar);
#endif // defined(DEBUG) && defined(_TARGET_XARCH_)

#ifdef DEBUG
    GenTree* lastConsumedNode;
    void genNumberOperandUse(GenTree* const operand, int& useNum) const;
    void genCheckConsumeNode(GenTree* const node);
#else  // !DEBUG
    inline void genCheckConsumeNode(GenTree* treeNode)
    {
    }
#endif // DEBUG

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

    void inst_RV_IV(
        instruction ins, regNumber reg, target_ssize_t val, emitAttr size, insFlags flags = INS_FLAGS_DONT_CARE);

    void inst_ST_RV(instruction ins, TempDsc* tmp, unsigned ofs, regNumber reg, var_types type);
    void inst_ST_IV(instruction ins, TempDsc* tmp, unsigned ofs, int val, var_types type);

    void inst_SA_RV(instruction ins, unsigned ofs, regNumber reg, var_types type);
    void inst_SA_IV(instruction ins, unsigned ofs, int val, var_types type);

    void inst_RV_ST(
        instruction ins, regNumber reg, TempDsc* tmp, unsigned ofs, var_types type, emitAttr size = EA_UNKNOWN);
    void inst_FS_ST(instruction ins, emitAttr size, TempDsc* tmp, unsigned ofs);

    void inst_TT(instruction ins, GenTree* tree, unsigned offs = 0, int shfv = 0, emitAttr size = EA_UNKNOWN);

    void inst_TT_RV(instruction ins,
                    GenTree*    tree,
                    regNumber   reg,
                    unsigned    offs  = 0,
                    emitAttr    size  = EA_UNKNOWN,
                    insFlags    flags = INS_FLAGS_DONT_CARE);

    void inst_RV_TT(instruction ins,
                    regNumber   reg,
                    GenTree*    tree,
                    unsigned    offs  = 0,
                    emitAttr    size  = EA_UNKNOWN,
                    insFlags    flags = INS_FLAGS_DONT_CARE);

    void inst_FS_TT(instruction ins, GenTree* tree);

    void inst_RV_SH(instruction ins, emitAttr size, regNumber reg, unsigned val, insFlags flags = INS_FLAGS_DONT_CARE);

    void inst_TT_SH(instruction ins, GenTree* tree, unsigned val, unsigned offs = 0);

    void inst_RV_CL(instruction ins, regNumber reg, var_types type = TYP_I_IMPL);

    void inst_TT_CL(instruction ins, GenTree* tree, unsigned offs = 0);

#if defined(_TARGET_XARCH_)
    void inst_RV_RV_IV(instruction ins, emitAttr size, regNumber reg1, regNumber reg2, unsigned ival);
    void inst_RV_TT_IV(instruction ins, emitAttr attr, regNumber reg1, GenTree* rmOp, int ival);
#endif

    void inst_RV_RR(instruction ins, emitAttr size, regNumber reg1, regNumber reg2);

    void inst_RV_ST(instruction ins, emitAttr size, regNumber reg, GenTree* tree);

    void inst_mov_RV_ST(regNumber reg, GenTree* tree);

    void inst_set_SV_var(GenTree* tree);

#ifdef _TARGET_ARM_
    bool arm_Valid_Imm_For_Instr(instruction ins, target_ssize_t imm, insFlags flags);
    bool arm_Valid_Disp_For_LdSt(target_ssize_t disp, var_types type);
    bool arm_Valid_Imm_For_Alu(target_ssize_t imm);
    bool arm_Valid_Imm_For_Mov(target_ssize_t imm);
    bool arm_Valid_Imm_For_Small_Mov(regNumber reg, target_ssize_t imm, insFlags flags);
    bool arm_Valid_Imm_For_Add(target_ssize_t imm, insFlags flag);
    bool arm_Valid_Imm_For_Add_SP(target_ssize_t imm);
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

#ifdef _TARGET_ARM64_
    void instGen_MemoryBarrier(insBarrier barrierType = INS_BARRIER_ISH);
#else
    void instGen_MemoryBarrier();
#endif

    void instGen_Set_Reg_To_Zero(emitAttr size, regNumber reg, insFlags flags = INS_FLAGS_DONT_CARE);

    void instGen_Set_Reg_To_Imm(emitAttr size, regNumber reg, ssize_t imm, insFlags flags = INS_FLAGS_DONT_CARE);

    void instGen_Compare_Reg_To_Zero(emitAttr size, regNumber reg);

    void instGen_Compare_Reg_To_Reg(emitAttr size, regNumber reg1, regNumber reg2);

    void instGen_Compare_Reg_To_Imm(emitAttr size, regNumber reg, target_ssize_t imm);

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

    // Maps a GenCondition code to a sequence of conditional jumps or other conditional instructions
    // such as X86's SETcc. A sequence of instructions rather than just a single one is required for
    // certain floating point conditions.
    // For example, X86's UCOMISS sets ZF to indicate equality but it also sets it, together with PF,
    // to indicate an unordered result. So for GenCondition::FEQ we first need to check if PF is 0
    // and then jump if ZF is 1:
    //       JP fallThroughBlock
    //       JE jumpDestBlock
    //   fallThroughBlock:
    //       ...
    //   jumpDestBlock:
    //
    // This is very similar to the way shortcircuit evaluation of bool AND and OR operators works so
    // in order to make the GenConditionDesc mapping tables easier to read, a bool expression-like
    // pattern is used to encode the above:
    //     { EJ_jnp, GT_AND, EJ_je  }
    //     { EJ_jp,  GT_OR,  EJ_jne }
    //
    // For more details check inst_JCC and inst_SETCC functions.
    //
    struct GenConditionDesc
    {
        emitJumpKind jumpKind1;
        genTreeOps   oper;
        emitJumpKind jumpKind2;
        char         padTo4Bytes;

        static const GenConditionDesc& Get(GenCondition condition)
        {
            assert(condition.GetCode() < _countof(map));
            const GenConditionDesc& desc = map[condition.GetCode()];
            assert(desc.jumpKind1 != EJ_NONE);
            assert((desc.oper == GT_NONE) || (desc.oper == GT_AND) || (desc.oper == GT_OR));
            assert((desc.oper == GT_NONE) == (desc.jumpKind2 == EJ_NONE));
            return desc;
        }

    private:
        static const GenConditionDesc map[32];
    };

    void inst_JCC(GenCondition condition, BasicBlock* target);
    void inst_SETCC(GenCondition condition, var_types type, regNumber dstReg);
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

inline void CodeGen::inst_FS_TT(instruction ins, GenTree* tree)
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
