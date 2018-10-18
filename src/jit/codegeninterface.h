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
#include "treelifeupdater.h"

// Forward reference types

class CodeGenInterface;
class emitter;

// Small helper types

//-------------------- Register selection ---------------------------------

struct RegState
{
    regMaskTP rsCalleeRegArgMaskLiveIn; // mask of register arguments (live on entry to method)
    unsigned  rsCalleeRegArgCount;      // total number of incoming register arguments of this kind (int or float)
    bool      rsIsFloat;                // true for float argument registers, false for integer argument registers
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

    // genSpillVar is called by compUpdateLifeVar.
    // TODO-Cleanup: We should handle the spill directly in CodeGen, rather than
    // calling it from compUpdateLifeVar.  Then this can be non-virtual.

    virtual void genSpillVar(GenTree* tree) = 0;

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
    virtual bool genCreateAddrMode(GenTree*  addr,
                                   bool      fold,
                                   bool*     revPtr,
                                   GenTree** rv1Ptr,
                                   GenTree** rv2Ptr,
#if SCALED_ADDR_MODES
                                   unsigned* mulPtr,
#endif // SCALED_ADDR_MODES
                                   ssize_t* cnsPtr) = 0;

    GCInfo gcInfo;

    RegSet   regSet;
    RegState intRegState;
    RegState floatRegState;

protected:
    Compiler* compiler;
    bool      m_genAlignLoops;

private:
#if defined(_TARGET_XARCH_)
    static const insFlags instInfo[INS_count];
#elif defined(_TARGET_ARM_) || defined(_TARGET_ARM64_)
    static const BYTE instInfo[INS_count];
#else
#error Unsupported target architecture
#endif

#define INST_FP 0x01 // is it a FP instruction?
public:
    static bool instIsFP(instruction ins);

    //-------------------------------------------------------------------------
    // Liveness-related fields & methods
public:
    void genUpdateRegLife(const LclVarDsc* varDsc, bool isBorn, bool isDying DEBUGARG(GenTree* tree));
    void genUpdateVarReg(LclVarDsc* varDsc, GenTree* tree);

protected:
#ifdef DEBUG
    VARSET_TP genTempOldLife;
    bool      genTempLiveChg;
#endif

    VARSET_TP genLastLiveSet;  // A one element map (genLastLiveSet-> genLastLiveMask)
    regMaskTP genLastLiveMask; // these two are used in genLiveMask

    regMaskTP genGetRegMask(const LclVarDsc* varDsc);
    regMaskTP genGetRegMask(GenTree* tree);

    void genUpdateLife(GenTree* tree);
    void genUpdateLife(VARSET_VALARG_TP newLife);

    TreeLifeUpdater<true>* treeLifeUpdater;

public:
    bool genUseOptimizedWriteBarriers(GCInfo::WriteBarrierForm wbf);
    bool genUseOptimizedWriteBarriers(GenTree* tgt, GenTree* assignVal);
    CorInfoHelpFunc genWriteBarrierHelperForWriteBarrierForm(GenTree* tgt, GCInfo::WriteBarrierForm wbf);

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

    regNumber genGetThisArgReg(GenTreeCall* call) const;

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

    //------------------------------------------------------------------------
    // resetWritePhaseForFramePointerRequired: Return m_cgFramePointerRequired into the write phase.
    // It is used only before the first phase, that locks this value, currently it is LSRA.
    // Use it if you want to skip checks that set this value to true if the value is already true.
    void resetWritePhaseForFramePointerRequired()
    {
        m_cgFramePointerRequired.ResetWritePhase();
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
#else // !DOUBLE_ALIGN

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
    unsigned InferStructOpSizeAlign(GenTree* op, unsigned* alignmentWB);
    unsigned InferOpSizeAlign(GenTree* op, unsigned* alignmentWB);

    void genMarkTreeInReg(GenTree* tree, regNumber reg);

    // Methods to abstract target information

    bool validImmForInstr(instruction ins, target_ssize_t val, insFlags flags = INS_FLAGS_DONT_CARE);
    bool validDispForLdSt(target_ssize_t disp, var_types type);
    bool validImmForAdd(target_ssize_t imm, insFlags flags);
    bool validImmForAlu(target_ssize_t imm);
    bool validImmForMov(target_ssize_t imm);
    bool validImmForBL(ssize_t addr);

    instruction ins_Load(var_types srcType, bool aligned = false);
    instruction ins_Store(var_types dstType, bool aligned = false);
    static instruction ins_FloatLoad(var_types type = TYP_DOUBLE);

    // Methods for spilling - used by RegSet
    void spillReg(var_types type, TempDsc* tmp, regNumber reg);
    void reloadReg(var_types type, TempDsc* tmp, regNumber reg);

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

#ifdef _TARGET_ARMARCH_
    __declspec(property(get = getHasTailCalls, put = setHasTailCalls)) bool hasTailCalls;
    bool getHasTailCalls()
    {
        return m_cgHasTailCalls;
    }
    void setHasTailCalls(bool value)
    {
        m_cgHasTailCalls = value;
    }
#endif // _TARGET_ARMARCH_

private:
    bool m_cgInterruptible;
#ifdef _TARGET_ARMARCH_
    bool m_cgHasTailCalls;
#endif // _TARGET_ARMARCH_

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

public:
    virtual void siUpdate() = 0;

#ifdef LATE_DISASM
public:
    virtual const char* siRegVarName(size_t offs, size_t size, unsigned reg) = 0;

    virtual const char* siStackVarName(size_t offs, size_t size, unsigned reg, unsigned stkOffs) = 0;
#endif // LATE_DISASM
};

#endif // _CODEGEN_INTERFACE_H_
