// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

class CodeGen final : public CodeGenInterface
{
    friend class emitter;
    friend class DisAssembler;

public:
    // This could use further abstraction
    CodeGen(Compiler* theCompiler);

    virtual void genGenerateCode(void** codePtr, uint32_t* nativeSizeOfCode);

    void genGenerateMachineCode();
    void genEmitMachineCode();
    void genEmitUnwindDebugGCandEH();

    // TODO-Cleanup: Abstract out the part of this that finds the addressing mode, and
    // move it to Lower
    virtual bool genCreateAddrMode(
        GenTree* addr, bool fold, bool* revPtr, GenTree** rv1Ptr, GenTree** rv2Ptr, unsigned* mulPtr, ssize_t* cnsPtr);

private:
#if defined(TARGET_XARCH)
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

    instruction simdAlignedMovIns()
    {
        // We use movaps when non-VEX because it is a smaller instruction;
        // however the VEX version vmovaps would be used which is the same size as vmovdqa;
        // also vmovdqa has more available CPU ports on older processors so we switch to that
        return compiler->canUseVexEncoding() ? INS_movdqa : INS_movaps;
    }
    instruction simdUnalignedMovIns()
    {
        // We use movups when non-VEX because it is a smaller instruction;
        // however the VEX version vmovups would be used which is the same size as vmovdqu;
        // but vmovdqu has more available CPU ports on older processors so we switch to that
        return compiler->canUseVexEncoding() ? INS_movdqu : INS_movups;
    }
#endif // defined(TARGET_XARCH)

    void genPrepForCompiler();

    void genMarkLabelsForCodegen();

    inline RegState* regStateForType(var_types t)
    {
        return varTypeUsesFloatReg(t) ? &floatRegState : &intRegState;
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

    static GenTreeIndir indirForm(var_types type, GenTree* base);
    static GenTreeStoreInd storeIndirForm(var_types type, GenTree* base, GenTree* data);

    GenTreeIntCon intForm(var_types type, ssize_t value);

    void genRangeCheck(GenTree* node);

    void genLockedInstructions(GenTreeOp* node);
#ifdef TARGET_XARCH
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

    void**    codePtr;
    uint32_t* nativeSizeOfCode;
    unsigned  codeSize;
    void*     coldCodePtr;
    void*     consPtr;

#ifdef DEBUG
    // Last instr we have displayed for dspInstrs
    unsigned genCurDispOffset;

    static const char* genInsName(instruction ins);
    const char* genInsDisplayName(emitter::instrDesc* id);

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

private:
    void genLogLabel(BasicBlock* bb);

protected:
    void genDefineTempLabel(BasicBlock* label);
    void genDefineInlineTempLabel(BasicBlock* label);

    void genAdjustStackLevel(BasicBlock* block);

    void genExitCode(BasicBlock* block);

    void genJumpToThrowHlpBlk(emitJumpKind jumpKind, SpecialCodeKind codeKind, BasicBlock* failBlk = nullptr);

    void genCheckOverflow(GenTree* tree);

    //-------------------------------------------------------------------------
    //
    // Prolog/epilog generation
    //
    //-------------------------------------------------------------------------

    unsigned prologSize;
    unsigned epilogSize;

    //
    // Prolog functions and data (there are a few exceptions for more generally used things)
    //

    void genEstablishFramePointer(int delta, bool reportUnwindData);
    void genFnPrologCalleeRegArgs(regNumber xtraReg, bool* pXtraRegClobbered, RegState* regState);
    void genEnregisterIncomingStackArgs();
#if defined(TARGET_ARM64)
    void genEnregisterOSRArgsAndLocals(regNumber initReg, bool* pInitRegZeroed);
#else
    void genEnregisterOSRArgsAndLocals();
#endif
    void genCheckUseBlockInit();
#if defined(UNIX_AMD64_ABI) && defined(FEATURE_SIMD)
    void genClearStackVec3ArgUpperBits();
#endif // UNIX_AMD64_ABI && FEATURE_SIMD

#if defined(TARGET_ARM64)
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

#if defined(TARGET_AMD64)
    void genOSRRecordTier0CalleeSavedRegistersAndFrame();
    void genOSRSaveRemainingCalleeSavedRegisters();
#endif // TARGET_AMD64

    void genAllocLclFrame(unsigned frameSize, regNumber initReg, bool* pInitRegZeroed, regMaskTP maskArgRegsLiveIn);

    void genPoisonFrame(regMaskTP bbRegLiveIn);

#if defined(TARGET_ARM)

    bool genInstrWithConstant(
        instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, ssize_t imm, insFlags flags, regNumber tmpReg);

    bool genStackPointerAdjustment(ssize_t spAdjustment, regNumber tmpReg);

    void genPushFltRegs(regMaskTP regMask);
    void genPopFltRegs(regMaskTP regMask);
    regMaskTP genStackAllocRegisterMask(unsigned frameSize, regMaskTP maskCalleeSavedFloat);

    regMaskTP genJmpCallArgMask();

    void genFreeLclFrame(unsigned           frameSize,
                         /* IN OUT */ bool* pUnwindStarted);

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

#elif defined(TARGET_ARM64)

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

#elif defined(TARGET_AMD64)

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

#endif // TARGET_AMD64

#if defined(TARGET_XARCH)

    // Save/Restore callee saved float regs to stack
    void genPreserveCalleeSavedFltRegs(unsigned lclFrameSize);
    void genRestoreCalleeSavedFltRegs(unsigned lclFrameSize);
    // Generate VZeroupper instruction to avoid AVX/SSE transition penalty
    void genVzeroupperIfNeeded(bool check256bitOnly = true);

#endif // TARGET_XARCH

    void genZeroInitFltRegs(const regMaskTP& initFltRegs, const regMaskTP& initDblRegs, const regNumber& initReg);

    regNumber genGetZeroReg(regNumber initReg, bool* pInitRegZeroed);

    void genZeroInitFrame(int untrLclHi, int untrLclLo, regNumber initReg, bool* pInitRegZeroed);
    void genZeroInitFrameUsingBlockInit(int untrLclHi, int untrLclLo, regNumber initReg, bool* pInitRegZeroed);

    void genReportGenericContextArg(regNumber initReg, bool* pInitRegZeroed);

    void genSetGSSecurityCookie(regNumber initReg, bool* pInitRegZeroed);

    void genFinalizeFrame();

#ifdef PROFILING_SUPPORTED
    void genProfilingEnterCallback(regNumber initReg, bool* pInitRegZeroed);
    void genProfilingLeaveCallback(unsigned helper);
#endif // PROFILING_SUPPORTED

    // clang-format off
    void genEmitCall(int                   callType,
                     CORINFO_METHOD_HANDLE methHnd,
                     INDEBUG_LDISASM_COMMA(CORINFO_SIG_INFO* sigInfo)
                     void*                 addr
                     X86_ARG(int argSize),
                     emitAttr              retSize
                     MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(emitAttr secondRetSize),
                     const DebugInfo&      di,
                     regNumber             base,
                     bool                  isJump);
    // clang-format on

    // clang-format off
    void genEmitCallIndir(int                   callType,
                          CORINFO_METHOD_HANDLE methHnd,
                          INDEBUG_LDISASM_COMMA(CORINFO_SIG_INFO* sigInfo)
                          GenTreeIndir*         indir
                          X86_ARG(int argSize),
                          emitAttr              retSize
                          MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(emitAttr secondRetSize),
                          const DebugInfo&      di,
                          bool                  isJump);
    // clang-format on

    //
    // Epilog functions
    //
    CLANG_FORMAT_COMMENT_ANCHOR;

#if defined(TARGET_ARM)
    bool genCanUsePopToReturn(regMaskTP maskPopRegsInt, bool jmpEpilog);
#endif

#if defined(TARGET_ARM64)

    void genPopCalleeSavedRegistersAndFreeLclFrame(bool jmpEpilog);

#else // !defined(TARGET_ARM64)

    void genPopCalleeSavedRegisters(bool jmpEpilog = false);

#if defined(TARGET_XARCH)
    unsigned genPopCalleeSavedRegistersFromMask(regMaskTP rsPopRegs);
#endif // !defined(TARGET_XARCH)

#endif // !defined(TARGET_ARM64)

    //
    // Common or driving functions
    //

    void genReserveProlog(BasicBlock* block); // currently unused
    void genReserveEpilog(BasicBlock* block);
    void genFnProlog();
    void genFnEpilog(BasicBlock* block);

#if defined(FEATURE_EH_FUNCLETS)

    void genReserveFuncletProlog(BasicBlock* block);
    void genReserveFuncletEpilog(BasicBlock* block);
    void genFuncletProlog(BasicBlock* block);
    void genFuncletEpilog();
    void genCaptureFuncletPrologEpilogInfo();

    /*-----------------------------------------------------------------------------
     *
     *  Set the main function PSPSym value in the frame.
     *  Funclets use different code to load the PSP sym and save it in their frame.
     *  See the document "CLR ABI.md" for a full description of the PSPSym.
     *  The PSPSym section of that document is copied here.
     *
     ***********************************
     *  The name PSPSym stands for Previous Stack Pointer Symbol.  It is how a funclet
     *  accesses locals from the main function body.
     *
     *  First, two definitions.
     *
     *  Caller-SP is the value of the stack pointer in a function's caller before the call
     *  instruction is executed. That is, when function A calls function B, Caller-SP for B
     *  is the value of the stack pointer immediately before the call instruction in A
     *  (calling B) was executed. Note that this definition holds for both AMD64, which
     *  pushes the return value when a call instruction is executed, and for ARM, which
     *  doesn't. For AMD64, Caller-SP is the address above the call return address.
     *
     *  Initial-SP is the initial value of the stack pointer after the fixed-size portion of
     *  the frame has been allocated. That is, before any "alloca"-type allocations.
     *
     *  The PSPSym is a pointer-sized local variable in the frame of the main function and
     *  of each funclet. The value stored in PSPSym is the value of Initial-SP/Caller-SP
     *  for the main function.  The stack offset of the PSPSym is reported to the VM in the
     *  GC information header.  The value reported in the GC information is the offset of the
     *  PSPSym from Initial-SP/Caller-SP. (Note that both the value stored, and the way the
     *  value is reported to the VM, differs between architectures. In particular, note that
     *  most things in the GC information header are reported as offsets relative to Caller-SP,
     *  but PSPSym on AMD64 is one (maybe the only) exception.)
     *
     *  The VM uses the PSPSym to find other locals it cares about (such as the generics context
     *  in a funclet frame). The JIT uses it to re-establish the frame pointer register, so that
     *  the frame pointer is the same value in a funclet as it is in the main function body.
     *
     *  When a funclet is called, it is passed the Establisher Frame Pointer. For AMD64 this is
     *  true for all funclets and it is passed as the first argument in RCX, but for ARM this is
     *  only true for first pass funclets (currently just filters) and it is passed as the second
     *  argument in R1. The Establisher Frame Pointer is a stack pointer of an interesting "parent"
     *  frame in the exception processing system. For the CLR, it points either to the main function
     *  frame or a dynamically enclosing funclet frame from the same function, for the funclet being
     *  invoked. The value of the Establisher Frame Pointer is Initial-SP on AMD64, Caller-SP on ARM.
     *
     *  Using the establisher frame, the funclet wants to load the value of the PSPSym. Since we
     *  don't know if the Establisher Frame is from the main function or a funclet, we design the
     *  main function and funclet frame layouts to place the PSPSym at an identical, small, constant
     *  offset from the Establisher Frame in each case. (This is also required because we only report
     *  a single offset to the PSPSym in the GC information, and that offset must be valid for the main
     *  function and all of its funclets). Then, the funclet uses this known offset to compute the
     *  PSPSym address and read its value. From this, it can compute the value of the frame pointer
     *  (which is a constant offset from the PSPSym value) and set the frame register to be the same
     *  as the parent function. Also, the funclet writes the value of the PSPSym to its own frame's
     *  PSPSym. This "copying" of the PSPSym happens for every funclet invocation, in particular,
     *  for every nested funclet invocation.
     *
     *  On ARM, for all second pass funclets (finally, fault, catch, and filter-handler) the VM
     *  restores all non-volatile registers to their values within the parent frame. This includes
     *  the frame register (R11). Thus, the PSPSym is not used to recompute the frame pointer register
     *  in this case, though the PSPSym is copied to the funclet's frame, as for all funclets.
     *
     *  Catch, Filter, and Filter-handlers also get an Exception object (GC ref) as an argument
     *  (REG_EXCEPTION_OBJECT).  On AMD64 it is the second argument and thus passed in RDX.  On
     *  ARM this is the first argument and passed in R0.
     *
     *  (Note that the JIT64 source code contains a comment that says, "The current CLR doesn't always
     *  pass the correct establisher frame to the funclet. Funclet may receive establisher frame of
     *  funclet when expecting that of original routine." It indicates this is the reason that a PSPSym
     *  is required in all funclets as well as the main function, whereas if the establisher frame was
     *  correctly reported, the PSPSym could be omitted in some cases.)
     ***********************************
     */
    void genSetPSPSym(regNumber initReg, bool* pInitRegZeroed);

    void genUpdateCurrentFunclet(BasicBlock* block);
#if defined(TARGET_ARM)
    void genInsertNopForUnwinder(BasicBlock* block);
#endif

#else // !FEATURE_EH_FUNCLETS

    // This is a no-op when there are no funclets!
    void genUpdateCurrentFunclet(BasicBlock* block)
    {
        return;
    }

#endif // !FEATURE_EH_FUNCLETS

    void genGeneratePrologsAndEpilogs();

#if defined(DEBUG) && defined(TARGET_ARM64)
    void genArm64EmitterUnitTests();
#endif

#if defined(DEBUG) && defined(LATE_DISASM) && defined(TARGET_AMD64)
    void genAmd64EmitterUnitTests();
#endif

#ifdef TARGET_ARM64
    virtual void SetSaveFpLrWithAllCalleeSavedRegisters(bool value);
    virtual bool IsSaveFpLrWithAllCalleeSavedRegisters() const;
    bool         genSaveFpLrWithAllCalleeSavedRegisters;
#endif // TARGET_ARM64

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
    void genIPmappingDisp(unsigned mappingNum, IPmappingDsc* ipMapping);
    void genIPmappingListDisp();
#endif // DEBUG

    void genIPmappingAdd(IPmappingDscKind kind, const DebugInfo& di, bool isLabel);
    void genIPmappingAddToFront(IPmappingDscKind kind, const DebugInfo& di, bool isLabel);
    void genIPmappingGen();

#ifdef DEBUG
    void genDumpPreciseDebugInfo();
    void genDumpPreciseDebugInfoInlineTree(FILE* file, InlineContext* context, bool* first);
    void genAddPreciseIPMappingHere(const DebugInfo& di);
#endif

    void genEnsureCodeEmitted(const DebugInfo& di);

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

#endif // USING_SCOPE_INFO
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
#if defined(FEATURE_EH_FUNCLETS)
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

    // For EBP-frames, the parameters are accessed via ESP on entry to the function,
    // but via EBP right after a "mov ebp,esp" instruction.
    void psiMoveESPtoEBP();

    // Close previous psiScope and open a new one on the location described by the registers.
    void psiMoveToReg(unsigned varNum, regNumber reg = REG_NA, regNumber otherReg = REG_NA);

    // Search the open "psiScope" of the "varNum" parameter, close it and open
    // a new one using "LclVarDsc" fields.
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

#if defined(TARGET_X86)
    void genCodeForLongUMod(GenTreeOp* node);
#endif // TARGET_X86

    void genCodeForDivMod(GenTreeOp* treeNode);
    void genCodeForMul(GenTreeOp* treeNode);
    void genCodeForIncSaturate(GenTree* treeNode);
    void genCodeForMulHi(GenTreeOp* treeNode);
    void genLeaInstruction(GenTreeAddrMode* lea);
    void genSetRegToCond(regNumber dstReg, GenTree* tree);

#if defined(TARGET_ARMARCH)
    void genScaledAdd(emitAttr attr, regNumber targetReg, regNumber baseReg, regNumber indexReg, int scale);
    void genCodeForMulLong(GenTreeOp* mul);
#endif // TARGET_ARMARCH

#if !defined(TARGET_64BIT)
    void genLongToIntCast(GenTree* treeNode);
#endif

    // Generate code for a GT_BITCAST that is not contained.
    void genCodeForBitCast(GenTreeOp* treeNode);

    // Generate the instruction to move a value between register files
    void genBitCast(var_types targetType, regNumber targetReg, var_types srcType, regNumber srcReg);

    struct GenIntCastDesc
    {
        enum CheckKind
        {
            CHECK_NONE,
            CHECK_SMALL_INT_RANGE,
            CHECK_POSITIVE,
#ifdef TARGET_64BIT
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
#ifdef TARGET_64BIT
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

#if defined(TARGET_XARCH)
    unsigned getBaseVarForPutArgStk(GenTree* treeNode);
#endif // TARGET_XARCH

    unsigned getFirstArgWithStackSlot();

    void genCompareFloat(GenTree* treeNode);
    void genCompareInt(GenTree* treeNode);

#ifdef FEATURE_SIMD
    enum SIMDScalarMoveType{
        SMT_ZeroInitUpper,                  // zero initlaize target upper bits
        SMT_ZeroInitUpper_SrcHasUpperZeros, // zero initialize target upper bits; source upper bits are known to be zero
        SMT_PreserveUpper                   // preserve target upper bits
    };

#ifdef TARGET_ARM64
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
    void genSIMDExtractUpperHalf(GenTreeSIMD* simdNode, regNumber srcReg, regNumber tgtReg);
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
#ifdef TARGET_X86
    void genStoreSIMD12ToStack(regNumber operandReg, regNumber tmpReg);
    void genPutArgStkSIMD12(GenTree* treeNode);
#endif // TARGET_X86
#endif // FEATURE_SIMD

#ifdef FEATURE_HW_INTRINSICS
    void genHWIntrinsic(GenTreeHWIntrinsic* node);
#if defined(TARGET_XARCH)
    void genHWIntrinsic_R_RM(GenTreeHWIntrinsic* node, instruction ins, emitAttr attr, regNumber reg, GenTree* rmOp);
    void genHWIntrinsic_R_RM_I(GenTreeHWIntrinsic* node, instruction ins, emitAttr attr, int8_t ival);
    void genHWIntrinsic_R_R_RM(GenTreeHWIntrinsic* node, instruction ins, emitAttr attr);
    void genHWIntrinsic_R_R_RM(
        GenTreeHWIntrinsic* node, instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, GenTree* op2);
    void genHWIntrinsic_R_R_RM_I(GenTreeHWIntrinsic* node, instruction ins, emitAttr attr, int8_t ival);
    void genHWIntrinsic_R_R_RM_R(GenTreeHWIntrinsic* node, instruction ins, emitAttr attr);
    void genHWIntrinsic_R_R_R_RM(
        instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, regNumber op2Reg, GenTree* op3);
    void genBaseIntrinsic(GenTreeHWIntrinsic* node);
    void genX86BaseIntrinsic(GenTreeHWIntrinsic* node);
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
#endif // defined(TARGET_XARCH)

#ifdef TARGET_ARM64
    class HWIntrinsicImmOpHelper final
    {
    public:
        HWIntrinsicImmOpHelper(CodeGen* codeGen, GenTree* immOp, GenTreeHWIntrinsic* intrin);

        void EmitBegin();
        void EmitCaseEnd();

        // Returns true after the last call to EmitCaseEnd() (i.e. this signals that code generation is done).
        bool Done() const
        {
            return (immValue > immUpperBound);
        }

        // Returns a value of the immediate operand that should be used for a case.
        int ImmValue() const
        {
            return immValue;
        }

    private:
        // Returns true if immOp is non contained immediate (i.e. the value of the immediate operand is enregistered in
        // nonConstImmReg).
        bool NonConstImmOp() const
        {
            return nonConstImmReg != REG_NA;
        }

        // Returns true if a non constant immediate operand can be either 0 or 1.
        bool TestImmOpZeroOrOne() const
        {
            assert(NonConstImmOp());
            return (immLowerBound == 0) && (immUpperBound == 1);
        }

        emitter* GetEmitter() const
        {
            return codeGen->GetEmitter();
        }

        CodeGen* const codeGen;
        BasicBlock*    endLabel;
        BasicBlock*    nonZeroLabel;
        int            immValue;
        int            immLowerBound;
        int            immUpperBound;
        regNumber      nonConstImmReg;
        regNumber      branchTargetReg;
    };
#endif // TARGET_ARM64

#endif // FEATURE_HW_INTRINSICS

#if !defined(TARGET_64BIT)

    // CodeGen for Long Ints

    void genStoreLongLclVar(GenTree* treeNode);

#endif // !defined(TARGET_64BIT)

    // Do liveness update for register produced by the current node in codegen after
    // code has been emitted for it.
    void genProduceReg(GenTree* tree);
    void genSpillLocal(unsigned varNum, var_types type, GenTreeLclVar* lclNode, regNumber regNum);
    void genUnspillLocal(
        unsigned varNum, var_types type, GenTreeLclVar* lclNode, regNumber regNum, bool reSpill, bool isLastUse);
    void genUnspillRegIfNeeded(GenTree* tree);
    void genUnspillRegIfNeeded(GenTree* tree, unsigned multiRegIndex);
    regNumber genConsumeReg(GenTree* tree);
    regNumber genConsumeReg(GenTree* tree, unsigned multiRegIndex);
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
    regNumber genRegCopy(GenTree* tree, unsigned multiRegIndex);
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
#if defined(FEATURE_SIMD) || defined(FEATURE_HW_INTRINSICS)
    void genConsumeMultiOpOperands(GenTreeMultiOp* tree);
#endif
    void genEmitGSCookieCheck(bool pushReg);
    void genCodeForShift(GenTree* tree);

#if defined(TARGET_X86) || defined(TARGET_ARM)
    void genCodeForShiftLong(GenTree* tree);
#endif

#ifdef TARGET_XARCH
    void genCodeForShiftRMW(GenTreeStoreInd* storeInd);
    void genCodeForBT(GenTreeOp* bt);
#endif // TARGET_XARCH

    void genCodeForCast(GenTreeOp* tree);
    void genCodeForLclAddr(GenTreeLclVarCommon* lclAddrNode);
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
#ifndef TARGET_X86
    void genCodeForCpBlkHelper(GenTreeBlk* cpBlkNode);
#endif
    void genCodeForPhysReg(GenTreePhysReg* tree);
    void genCodeForNullCheck(GenTreeIndir* tree);
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

#ifndef TARGET_X86
    void genPutArgStkFieldList(GenTreePutArgStk* putArgStk, unsigned outArgVarNum);
#endif // !TARGET_X86

#ifdef FEATURE_PUT_STRUCT_ARG_STK
#ifdef TARGET_X86
    bool genAdjustStackForPutArgStk(GenTreePutArgStk* putArgStk);
    void genPushReg(var_types type, regNumber srcReg);
    void genPutArgStkFieldList(GenTreePutArgStk* putArgStk);
#endif // TARGET_X86

    void genPutStructArgStk(GenTreePutArgStk* treeNode);

    unsigned genMove8IfNeeded(unsigned size, regNumber tmpReg, GenTree* srcAddr, unsigned offset);
    unsigned genMove4IfNeeded(unsigned size, regNumber tmpReg, GenTree* srcAddr, unsigned offset);
    unsigned genMove2IfNeeded(unsigned size, regNumber tmpReg, GenTree* srcAddr, unsigned offset);
    unsigned genMove1IfNeeded(unsigned size, regNumber tmpReg, GenTree* srcAddr, unsigned offset);
    void genCodeForLoadOffset(instruction ins, emitAttr size, regNumber dst, GenTree* base, unsigned offset);
    void genStoreRegToStackArg(var_types type, regNumber reg, int offset);
    void genStructPutArgRepMovs(GenTreePutArgStk* putArgStkNode);
    void genStructPutArgUnroll(GenTreePutArgStk* putArgStkNode);
#ifdef TARGET_X86
    void genStructPutArgPush(GenTreePutArgStk* putArgStkNode);
#else
    void genStructPutArgPartialRepMovs(GenTreePutArgStk* putArgStkNode);
#endif
#endif // FEATURE_PUT_STRUCT_ARG_STK

    void genCodeForStoreBlk(GenTreeBlk* storeBlkNode);
#ifndef TARGET_X86
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
    GenTree* getCallTarget(const GenTreeCall* call, CORINFO_METHOD_HANDLE* methHnd);
    regNumber getCallIndirectionCellReg(const GenTreeCall* call);
    void genCall(GenTreeCall* call);
    void genCallInstruction(GenTreeCall* call X86_ARG(target_ssize_t stackArgBytes));
    void genJmpMethod(GenTree* jmp);
    BasicBlock* genCallFinally(BasicBlock* block);
    void genCodeForJumpTrue(GenTreeOp* jtrue);
#ifdef TARGET_ARM64
    void genCodeForJumpCompare(GenTreeOp* tree);
    void genCodeForMadd(GenTreeOp* tree);
    void genCodeForBfiz(GenTreeOp* tree);
    void genCodeForAddEx(GenTreeOp* tree);
#endif // TARGET_ARM64

#if defined(FEATURE_EH_FUNCLETS)
    void genEHCatchRet(BasicBlock* block);
#else  // !FEATURE_EH_FUNCLETS
    void genEHFinallyOrFilterRet(BasicBlock* block);
#endif // !FEATURE_EH_FUNCLETS

    void genMultiRegStoreToSIMDLocal(GenTreeLclVar* lclNode);
    void genMultiRegStoreToLocal(GenTreeLclVar* lclNode);

    // Codegen for multi-register struct returns.
    bool isStructReturn(GenTree* treeNode);
#ifdef FEATURE_SIMD
    void genSIMDSplitReturn(GenTree* src, ReturnTypeDesc* retTypeDesc);
#endif
    void genStructReturn(GenTree* treeNode);

#if defined(TARGET_X86) || defined(TARGET_ARM)
    void genLongReturn(GenTree* treeNode);
#endif // TARGET_X86 ||  TARGET_ARM

#if defined(TARGET_X86)
    void genFloatReturn(GenTree* treeNode);
#endif // TARGET_X86

#if defined(TARGET_ARM64)
    void genSimpleReturn(GenTree* treeNode);
#endif // TARGET_ARM64

    void genReturn(GenTree* treeNode);

    void genStackPointerConstantAdjustment(ssize_t spDelta, regNumber regTmp);
    void genStackPointerConstantAdjustmentWithProbe(ssize_t spDelta, regNumber regTmp);
    target_ssize_t genStackPointerConstantAdjustmentLoopWithProbe(ssize_t spDelta, regNumber regTmp);

#if defined(TARGET_XARCH)
    void genStackPointerDynamicAdjustmentWithProbe(regNumber regSpDelta, regNumber regTmp);
#endif // defined(TARGET_XARCH)

    void genLclHeap(GenTree* tree);

    bool genIsRegCandidateLocal(GenTree* tree)
    {
        if (!tree->IsLocal())
        {
            return false;
        }
        return compiler->lvaGetDesc(tree->AsLclVarCommon())->lvIsRegCandidate();
    }

#ifdef FEATURE_PUT_STRUCT_ARG_STK
#ifdef TARGET_X86
    bool m_pushStkArg;
#else  // !TARGET_X86
    unsigned m_stkArgVarNum;
    unsigned m_stkArgOffset;
#endif // !TARGET_X86
#endif // !FEATURE_PUT_STRUCT_ARG_STK

#if defined(DEBUG) && defined(TARGET_XARCH)
    void genStackPointerCheck(bool doStackPointerCheck, unsigned lvaStackPointerVar);
#endif // defined(DEBUG) && defined(TARGET_XARCH)

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
    void instGen(instruction ins);

    void inst_JMP(emitJumpKind jmp, BasicBlock* tgtBlock);

    void inst_SET(emitJumpKind condition, regNumber reg);

    void inst_RV(instruction ins, regNumber reg, var_types type, emitAttr size = EA_UNKNOWN);

    void inst_Mov(var_types dstType,
                  regNumber dstReg,
                  regNumber srcReg,
                  bool      canSkip,
                  emitAttr  size  = EA_UNKNOWN,
                  insFlags  flags = INS_FLAGS_DONT_CARE);

    void inst_Mov_Extend(var_types srcType,
                         bool      srcInReg,
                         regNumber dstReg,
                         regNumber srcReg,
                         bool      canSkip,
                         emitAttr  size  = EA_UNKNOWN,
                         insFlags  flags = INS_FLAGS_DONT_CARE);

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

    void inst_IV(instruction ins, cnsval_ssize_t val);
    void inst_IV_handle(instruction ins, cnsval_ssize_t val);

    void inst_RV_IV(
        instruction ins, regNumber reg, target_ssize_t val, emitAttr size, insFlags flags = INS_FLAGS_DONT_CARE);

    void inst_ST_RV(instruction ins, TempDsc* tmp, unsigned ofs, regNumber reg, var_types type);

    void inst_FS_ST(instruction ins, emitAttr size, TempDsc* tmp, unsigned ofs);

    void inst_TT_RV(instruction ins, emitAttr size, GenTree* tree, regNumber reg);

    void inst_RV_SH(instruction ins, emitAttr size, regNumber reg, unsigned val, insFlags flags = INS_FLAGS_DONT_CARE);

#if defined(TARGET_XARCH)

    enum class OperandKind{
        ClsVar, // [CLS_VAR_ADDR]                 - "C" in the emitter.
        Local,  // [Local or spill temp + offset] - "S" in the emitter.
        Indir,  // [base+index*scale+disp]        - "A" in the emitter.
        Imm,    // immediate                      - "I" in the emitter.
        Reg     // reg                            - "R" in the emitter.
    };

    class OperandDesc
    {
        OperandKind m_kind;
        union {
            struct
            {
                CORINFO_FIELD_HANDLE m_fieldHnd;
            };
            struct
            {
                int      m_varNum;
                uint16_t m_offset;
            };
            struct
            {
                GenTree*      m_addr;
                GenTreeIndir* m_indir;
                var_types     m_indirType;
            };
            struct
            {
                ssize_t m_immediate;
                bool    m_immediateNeedsReloc;
            };
            struct
            {
                regNumber m_reg;
            };
        };

    public:
        OperandDesc(CORINFO_FIELD_HANDLE fieldHnd) : m_kind(OperandKind::ClsVar), m_fieldHnd(fieldHnd)
        {
        }

        OperandDesc(int varNum, uint16_t offset) : m_kind(OperandKind::Local), m_varNum(varNum), m_offset(offset)
        {
        }

        OperandDesc(GenTreeIndir* indir)
            : m_kind(OperandKind::Indir), m_addr(indir->Addr()), m_indir(indir), m_indirType(indir->TypeGet())
        {
        }

        OperandDesc(var_types indirType, GenTree* addr)
            : m_kind(OperandKind::Indir), m_addr(addr), m_indir(nullptr), m_indirType(indirType)
        {
        }

        OperandDesc(ssize_t immediate, bool immediateNeedsReloc)
            : m_kind(OperandKind::Imm), m_immediate(immediate), m_immediateNeedsReloc(immediateNeedsReloc)
        {
        }

        OperandDesc(regNumber reg) : m_kind(OperandKind::Reg), m_reg(reg)
        {
        }

        OperandKind GetKind() const
        {
            return m_kind;
        }

        CORINFO_FIELD_HANDLE GetFieldHnd() const
        {
            assert(m_kind == OperandKind::ClsVar);
            return m_fieldHnd;
        }

        int GetVarNum() const
        {
            assert(m_kind == OperandKind::Local);
            return m_varNum;
        }

        int GetLclOffset() const
        {
            assert(m_kind == OperandKind::Local);
            return m_offset;
        }

        // TODO-Cleanup: instead of this rather unsightly workaround with
        // "indirForm", create a new abstraction for address modes to pass
        // to the emitter (or at least just use "addr"...).
        GenTreeIndir* GetIndirForm(GenTreeIndir* pIndirForm)
        {
            if (m_indir == nullptr)
            {
                GenTreeIndir indirForm = CodeGen::indirForm(m_indirType, m_addr);
                memcpy(pIndirForm, &indirForm, sizeof(GenTreeIndir));
            }
            else
            {
                pIndirForm = m_indir;
            }

            return pIndirForm;
        }

        ssize_t GetImmediate() const
        {
            assert(m_kind == OperandKind::Imm);
            return m_immediate;
        }

        emitAttr GetEmitAttrForImmediate(emitAttr baseAttr) const
        {
            assert(m_kind == OperandKind::Imm);
            return m_immediateNeedsReloc ? EA_SET_FLG(baseAttr, EA_CNS_RELOC_FLG) : baseAttr;
        }

        regNumber GetReg() const
        {
            return m_reg;
        }

        bool IsContained() const
        {
            return m_kind != OperandKind::Reg;
        }
    };

    OperandDesc genOperandDesc(GenTree* op);

    void inst_TT(instruction ins, emitAttr size, GenTree* op1);
    void inst_RV_TT(instruction ins, emitAttr size, regNumber op1Reg, GenTree* op2);
    void inst_RV_RV_IV(instruction ins, emitAttr size, regNumber reg1, regNumber reg2, unsigned ival);
    void inst_RV_TT_IV(instruction ins, emitAttr attr, regNumber reg1, GenTree* rmOp, int ival);
    void inst_RV_RV_TT(instruction ins, emitAttr size, regNumber targetReg, regNumber op1Reg, GenTree* op2, bool isRMW);
#endif

    void inst_set_SV_var(GenTree* tree);

#ifdef TARGET_ARM
    bool arm_Valid_Imm_For_Instr(instruction ins, target_ssize_t imm, insFlags flags);
    bool arm_Valid_Imm_For_Add(target_ssize_t imm, insFlags flag);
    bool arm_Valid_Imm_For_Add_SP(target_ssize_t imm);
#endif

    instruction ins_Move_Extend(var_types srcType, bool srcInReg);

    instruction ins_Copy(var_types dstType);
    instruction ins_Copy(regNumber srcReg, var_types dstType);
    instruction ins_FloatConv(var_types to, var_types from);
    instruction ins_MathOp(genTreeOps oper, var_types type);

    void instGen_Return(unsigned stkArgSize);

    enum BarrierKind
    {
        BARRIER_FULL,      // full barrier
        BARRIER_LOAD_ONLY, // load barier
    };

    void instGen_MemoryBarrier(BarrierKind barrierKind = BARRIER_FULL);

    void instGen_Set_Reg_To_Zero(emitAttr size, regNumber reg, insFlags flags = INS_FLAGS_DONT_CARE);

    void instGen_Set_Reg_To_Imm(emitAttr  size,
                                regNumber reg,
                                ssize_t   imm,
                                insFlags flags = INS_FLAGS_DONT_CARE DEBUGARG(size_t targetHandle = 0)
                                    DEBUGARG(GenTreeFlags gtFlags = GTF_EMPTY));

#ifdef TARGET_XARCH
    instruction genMapShiftInsToShiftByConstantIns(instruction ins, int shiftByValue);
#endif // TARGET_XARCH

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
            assert(condition.GetCode() < ArrLen(map));
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

// A simple phase that just invokes a method on the codegen instance
//
class CodeGenPhase final : public Phase
{
public:
    CodeGenPhase(CodeGen* _codeGen, Phases _phase, void (CodeGen::*_action)())
        : Phase(_codeGen->GetCompiler(), _phase), codeGen(_codeGen), action(_action)
    {
    }

protected:
    virtual PhaseStatus DoPhase() override
    {
        (codeGen->*action)();
        return PhaseStatus::MODIFIED_EVERYTHING;
    }

private:
    CodeGen* codeGen;
    void (CodeGen::*action)();
};

// Wrapper for using CodeGenPhase
//
inline void DoPhase(CodeGen* _codeGen, Phases _phase, void (CodeGen::*_action)())
{
    CodeGenPhase phase(_codeGen, _phase, _action);
    phase.Run();
}

#endif // _CODEGEN_H_
