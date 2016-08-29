// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// This file declares the types that constitute the interface between the
// code generator (CodeGen class) and the rest of the JIT.
//
// RegState
//
// CodeGenInterface includes only the public methods that are called by
// the Compiler.
//
// CodeGenContext contains the shared context between the code generator
// and other phases of the JIT, especially the register allocator and
// GC encoder.  It is distinct from CodeGenInterface so that it can be
// included in the Compiler object, and avoid an extra indirection when
// accessed from members of Compiler.
//

#ifndef _CODEGEN_INTERFACE_H_
#define _CODEGEN_INTERFACE_H_

#include "regset.h"
#include "jitgcinfo.h"

// Forward reference types

class CodeGenInterface;
class emitter;

// Small helper types

//-------------------- Register selection ---------------------------------

struct RegState
{
    regMaskTP rsCalleeRegArgMaskLiveIn; // mask of register arguments (live on entry to method)
#ifdef LEGACY_BACKEND
    unsigned rsCurRegArgNum; // current argument number (for caller)
#endif
    unsigned rsCalleeRegArgCount; // total number of incoming register arguments of this kind (int or float)
    bool     rsIsFloat;           // true for float argument registers, false for integer argument registers
};

//-------------------- CodeGenInterface ---------------------------------
// interface to hide the full CodeGen implementation from rest of Compiler

CodeGenInterface* getCodeGenerator(Compiler* comp);

class CodeGenInterface
{
    friend class emitter;

public:
    CodeGenInterface(Compiler* theCompiler);
    virtual void genGenerateCode(void** codePtr, ULONG* nativeSizeOfCode) = 0;

#ifndef LEGACY_BACKEND
    // genSpillVar is called by compUpdateLifeVar in the RyuJIT backend case.
    // TODO-Cleanup: We should handle the spill directly in CodeGen, rather than
    // calling it from compUpdateLifeVar.  Then this can be non-virtual.

    virtual void genSpillVar(GenTreePtr tree) = 0;
#endif // !LEGACY_BACKEND

    //-------------------------------------------------------------------------
    //  The following property indicates whether to align loops.
    //  (Used to avoid effects of loop alignment when diagnosing perf issues.)
    __declspec(property(get = doAlignLoops, put = setAlignLoops)) bool genAlignLoops;
    bool doAlignLoops()
    {
        return m_genAlignLoops;
    }
    void setAlignLoops(bool value)
    {
        m_genAlignLoops = value;
    }

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
                                   bool      nogen = false) = 0;

    void genCalcFrameSize();

    GCInfo gcInfo;

    RegSet   regSet;
    RegState intRegState;
    RegState floatRegState;

    // TODO-Cleanup: The only reason that regTracker needs to live in CodeGenInterface is that
    // in RegSet::rsUnspillOneReg, it needs to mark the new register as "trash"
    RegTracker regTracker;

public:
    void trashReg(regNumber reg)
    {
        regTracker.rsTrackRegTrash(reg);
    }

protected:
    Compiler* compiler;
    bool      m_genAlignLoops;

private:
    static const BYTE instInfo[INS_count];

#define INST_FP 0x01 // is it a FP instruction?
public:
    static bool instIsFP(instruction ins);

    //-------------------------------------------------------------------------
    // Liveness-related fields & methods
public:
    void genUpdateRegLife(const LclVarDsc* varDsc, bool isBorn, bool isDying DEBUGARG(GenTreePtr tree));
#ifndef LEGACY_BACKEND
    void genUpdateVarReg(LclVarDsc* varDsc, GenTreePtr tree);
#endif // !LEGACY_BACKEND

protected:
#ifdef DEBUG
    VARSET_TP genTempOldLife;
    bool      genTempLiveChg;
#endif

    VARSET_TP genLastLiveSet;  // A one element map (genLastLiveSet-> genLastLiveMask)
    regMaskTP genLastLiveMask; // these two are used in genLiveMask

    regMaskTP genGetRegMask(const LclVarDsc* varDsc);
    regMaskTP genGetRegMask(GenTreePtr tree);

    void genUpdateLife(GenTreePtr tree);
    void genUpdateLife(VARSET_VALARG_TP newLife);

#ifdef LEGACY_BACKEND
    regMaskTP genLiveMask(GenTreePtr tree);
    regMaskTP genLiveMask(VARSET_VALARG_TP liveSet);
#endif

    void genGetRegPairFromMask(regMaskTP regPairMask, regNumber* pLoReg, regNumber* pHiReg);

    // The following property indicates whether the current method sets up
    // an explicit stack frame or not.
private:
    PhasedVar<bool> m_cgFramePointerUsed;

public:
    bool isFramePointerUsed() const
    {
        return m_cgFramePointerUsed;
    }
    void setFramePointerUsed(bool value)
    {
        m_cgFramePointerUsed = value;
    }
    void resetFramePointerUsedWritePhase()
    {
        m_cgFramePointerUsed.ResetWritePhase();
    }

    // The following property indicates whether the current method requires
    // an explicit frame. Does not prohibit double alignment of the stack.
private:
    PhasedVar<bool> m_cgFrameRequired;

public:
    bool isFrameRequired() const
    {
        return m_cgFrameRequired;
    }
    void setFrameRequired(bool value)
    {
        m_cgFrameRequired = value;
    }

public:
    int genCallerSPtoFPdelta();
    int genCallerSPtoInitialSPdelta();
    int genSPtoFPdelta();
    int genTotalFrameSize();

    regNumber genGetThisArgReg(GenTreePtr call);

#ifdef _TARGET_XARCH_
#ifdef _TARGET_AMD64_
    // There are no reloc hints on x86
    unsigned short genAddrRelocTypeHint(size_t addr);
#endif
    bool genDataIndirAddrCanBeEncodedAsPCRelOffset(size_t addr);
    bool genCodeIndirAddrCanBeEncodedAsPCRelOffset(size_t addr);
    bool genCodeIndirAddrCanBeEncodedAsZeroRelOffset(size_t addr);
    bool genCodeIndirAddrNeedsReloc(size_t addr);
    bool genCodeAddrNeedsReloc(size_t addr);
#endif

    // If both isFramePointerRequired() and isFrameRequired() are false, the method is eligible
    // for Frame-Pointer-Omission (FPO).

    // The following property indicates whether the current method requires
    // an explicit stack frame, and all arguments and locals to be
    // accessible relative to the Frame Pointer. Prohibits double alignment
    // of the stack.
private:
    PhasedVar<bool> m_cgFramePointerRequired;

public:
    bool isFramePointerRequired() const
    {
        return m_cgFramePointerRequired;
    }
    void setFramePointerRequired(bool value)
    {
        m_cgFramePointerRequired = value;
    }
    void setFramePointerRequiredEH(bool value);

    void setFramePointerRequiredGCInfo(bool value)
    {
#ifdef JIT32_GCENCODER
        m_cgFramePointerRequired = value;
#endif
    }

#if DOUBLE_ALIGN
    // The following property indicates whether we going to double-align the frame.
    // Arguments are accessed relative to the Frame Pointer (EBP), and
    // locals are accessed relative to the Stack Pointer (ESP).
public:
    bool doDoubleAlign() const
    {
        return m_cgDoubleAlign;
    }
    void setDoubleAlign(bool value)
    {
        m_cgDoubleAlign = value;
    }
    bool doubleAlignOrFramePointerUsed() const
    {
        return isFramePointerUsed() || doDoubleAlign();
    }

private:
    bool m_cgDoubleAlign;
#else  // !DOUBLE_ALIGN
public:
    bool doubleAlignOrFramePointerUsed() const
    {
        return isFramePointerUsed();
    }
#endif // !DOUBLE_ALIGN

#ifdef DEBUG
    // The following is used to make sure the value of 'genInterruptible' isn't
    // changed after it's been used by any logic that depends on its value.
public:
    bool isGCTypeFixed()
    {
        return genInterruptibleUsed;
    }

protected:
    bool genInterruptibleUsed;
#endif

public:
#if FEATURE_STACK_FP_X87
    FlatFPStateX87 compCurFPState;
    unsigned       genFPregCnt; // count of current FP reg. vars (including dead but unpopped ones)

    void SetRegVarFloat(regNumber reg, var_types type, LclVarDsc* varDsc);

    void inst_FN(instruction ins, unsigned stk);

    //  Keeps track of the current level of the FP coprocessor stack
    //  (excluding FP reg. vars).
    //  Do not use directly, instead use the processor agnostic accessor
    //  methods below
    //
    unsigned genFPstkLevel;

    void genResetFPstkLevel(unsigned newValue = 0);
    unsigned        genGetFPstkLevel();
    FlatFPStateX87* FlatFPAllocFPState(FlatFPStateX87* pInitFrom = 0);

    void genIncrementFPstkLevel(unsigned inc = 1);
    void genDecrementFPstkLevel(unsigned dec = 1);

    static const char* regVarNameStackFP(regNumber reg);

    // FlatFPStateX87_ functions are the actual verbs to do stuff
    // like doing a transition, loading   register, etc. It's also
    // responsible for emitting the x87 code to do so. We keep
    // them in Compiler because we don't want to store a pointer to the
    // emitter.
    void FlatFPX87_MoveToTOS(FlatFPStateX87* pState, unsigned iVirtual, bool bEmitCode = true);
    void FlatFPX87_SwapStack(FlatFPStateX87* pState, unsigned i, unsigned j, bool bEmitCode = true);

#endif // FEATURE_STACK_FP_X87

#ifndef LEGACY_BACKEND
    regNumber genGetAssignedReg(GenTreePtr tree);
#endif // !LEGACY_BACKEND

#ifdef LEGACY_BACKEND
    // Changes GT_LCL_VAR nodes to GT_REG_VAR nodes if possible.
    bool genMarkLclVar(GenTreePtr tree);

    void genBashLclVar(GenTreePtr tree, unsigned varNum, LclVarDsc* varDsc);
#endif // LEGACY_BACKEND

public:
    unsigned InferStructOpSizeAlign(GenTreePtr op, unsigned* alignmentWB);
    unsigned InferOpSizeAlign(GenTreePtr op, unsigned* alignmentWB);

    void genMarkTreeInReg(GenTreePtr tree, regNumber reg);
#if CPU_LONG_USES_REGPAIR
    void genMarkTreeInRegPair(GenTreePtr tree, regPairNo regPair);
#endif
    // Methods to abstract target information

    bool validImmForInstr(instruction ins, ssize_t val, insFlags flags = INS_FLAGS_DONT_CARE);
    bool validDispForLdSt(ssize_t disp, var_types type);
    bool validImmForAdd(ssize_t imm, insFlags flags);
    bool validImmForAlu(ssize_t imm);
    bool validImmForMov(ssize_t imm);
    bool validImmForBL(ssize_t addr);

    instruction ins_Load(var_types srcType, bool aligned = false);
    instruction ins_Store(var_types dstType, bool aligned = false);
    static instruction ins_FloatLoad(var_types type = TYP_DOUBLE);

    // Methods for spilling - used by RegSet
    void spillReg(var_types type, TempDsc* tmp, regNumber reg);
    void reloadReg(var_types type, TempDsc* tmp, regNumber reg);
    void reloadFloatReg(var_types type, TempDsc* tmp, regNumber reg);

#ifdef LEGACY_BACKEND
    void SpillFloat(regNumber reg, bool bIsCall = false);
#endif // LEGACY_BACKEND

    // The following method is used by xarch emitter for handling contained tree temps.
    TempDsc* getSpillTempDsc(GenTree* tree);

public:
    emitter* getEmitter()
    {
        return m_cgEmitter;
    }

protected:
    emitter* m_cgEmitter;

#ifdef LATE_DISASM
public:
    DisAssembler& getDisAssembler()
    {
        return m_cgDisAsm;
    }

protected:
    DisAssembler m_cgDisAsm;
#endif // LATE_DISASM

public:
#ifdef DEBUG
    void setVerbose(bool value)
    {
        verbose = value;
    }
    bool verbose;
#ifdef LEGACY_BACKEND
    // Stress mode
    int       genStressFloat();
    regMaskTP genStressLockedMaskFloat();
#endif // LEGACY_BACKEND
#endif // DEBUG

    // The following is set to true if we've determined that the current method
    // is to be fully interruptible.
    //
public:
    __declspec(property(get = getInterruptible, put = setInterruptible)) bool genInterruptible;
    bool getInterruptible()
    {
        return m_cgInterruptible;
    }
    void setInterruptible(bool value)
    {
        m_cgInterruptible = value;
    }

private:
    bool m_cgInterruptible;

    //  The following will be set to true if we've determined that we need to
    //  generate a full-blown pointer register map for the current method.
    //  Currently it is equal to (genInterruptible || !isFramePointerUsed())
    //  (i.e. We generate the full-blown map for EBP-less methods and
    //        for fully interruptible methods)
    //
public:
    __declspec(property(get = doFullPtrRegMap, put = setFullPtrRegMap)) bool genFullPtrRegMap;
    bool doFullPtrRegMap()
    {
        return m_cgFullPtrRegMap;
    }
    void setFullPtrRegMap(bool value)
    {
        m_cgFullPtrRegMap = value;
    }

private:
    bool m_cgFullPtrRegMap;

#ifdef DEBUGGING_SUPPORT
public:
    virtual void siUpdate() = 0;
#endif // DEBUGGING_SUPPORT

#ifdef LATE_DISASM
public:
    virtual const char* siRegVarName(size_t offs, size_t size, unsigned reg) = 0;

    virtual const char* siStackVarName(size_t offs, size_t size, unsigned reg, unsigned stkOffs) = 0;
#endif // LATE_DISASM
};

#endif // _CODEGEN_INTERFACE_H_
