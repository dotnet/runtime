// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
#include "emit.h"

// Forward reference types

class CodeGenInterface;
class emitter;

// Small helper types

//-------------------- Register selection ---------------------------------

struct RegState
{
    regMaskOnlyOne rsCalleeRegArgMaskLiveIn; // mask of register arguments (live on entry to method)
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
    virtual void genGenerateCode(void** codePtr, uint32_t* nativeSizeOfCode) = 0;

    Compiler* GetCompiler() const
    {
        return compiler;
    }

#if defined(TARGET_AMD64)
    regMaskTP rbmAllFloat;
    regMaskTP rbmFltCalleeTrash;

    FORCEINLINE regMaskTP get_RBM_ALLFLOAT() const
    {
        return this->rbmAllFloat;
    }
    FORCEINLINE regMaskTP get_RBM_FLT_CALLEE_TRASH() const
    {
        return this->rbmFltCalleeTrash;
    }
#endif // TARGET_AMD64

#if defined(TARGET_XARCH)
    regMaskTP rbmAllMask;
    regMaskTP rbmMskCalleeTrash;

    // Call this function after the equivalent fields in Compiler have been initialized.
    void CopyRegisterInfo();

    FORCEINLINE regMaskTP get_RBM_ALLMASK() const
    {
        return this->rbmAllMask;
    }
    FORCEINLINE regMaskTP get_RBM_MSK_CALLEE_TRASH() const
    {
        return this->rbmMskCalleeTrash;
    }
#endif // TARGET_XARCH

    // genSpillVar is called by compUpdateLifeVar.
    // TODO-Cleanup: We should handle the spill directly in CodeGen, rather than
    // calling it from compUpdateLifeVar.  Then this can be non-virtual.

    virtual void genSpillVar(GenTree* tree) = 0;

    //-------------------------------------------------------------------------
    //  The following property indicates whether to align loops.
    //  (Used to avoid effects of loop alignment when diagnosing perf issues.)

    bool ShouldAlignLoops()
    {
        return m_genAlignLoops;
    }
    void SetAlignLoops(bool value)
    {
        m_genAlignLoops = value;
    }

    // TODO-Cleanup: Abstract out the part of this that finds the addressing mode, and
    // move it to Lower
    virtual bool genCreateAddrMode(GenTree*  addr,
                                   bool      fold,
                                   unsigned  naturalMul,
                                   bool*     revPtr,
                                   GenTree** rv1Ptr,
                                   GenTree** rv2Ptr,
                                   unsigned* mulPtr,
                                   ssize_t*  cnsPtr) = 0;

    GCInfo gcInfo;

    RegSet   regSet;
    RegState intRegState;
    RegState floatRegState;

protected:
    Compiler* compiler;
    bool      m_genAlignLoops;

private:
#if defined(TARGET_XARCH)
    static const insFlags instInfo[INS_count];
#elif defined(TARGET_ARM) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    static const BYTE instInfo[INS_count];
#else
#error Unsupported target architecture
#endif

#define INST_FP 0x01 // is it a FP instruction?
public:
    static bool instIsFP(instruction ins);
#if defined(TARGET_XARCH)
    static bool instIsEmbeddedBroadcastCompatible(instruction ins);
#endif // TARGET_XARCH
    //-------------------------------------------------------------------------
    // Liveness-related fields & methods
public:
    void genUpdateRegLife(const LclVarDsc* varDsc, bool isBorn, bool isDying DEBUGARG(GenTree* tree));
    void genUpdateVarReg(LclVarDsc* varDsc, GenTree* tree, int regIndex);
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
    bool genUseOptimizedWriteBarriers(GenTreeStoreInd* store);
    CorInfoHelpFunc genWriteBarrierHelperForWriteBarrierForm(GCInfo::WriteBarrierForm wbf);

#ifdef DEBUG
    bool genWriteBarrierUsed;
#endif

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
    int genCallerSPtoFPdelta() const;
    int genCallerSPtoInitialSPdelta() const;
    int genSPtoFPdelta() const;
    int genTotalFrameSize() const;

#ifdef TARGET_ARM64
    virtual void SetSaveFpLrWithAllCalleeSavedRegisters(bool value) = 0;
    virtual bool IsSaveFpLrWithAllCalleeSavedRegisters() const      = 0;
#endif // TARGET_ARM64

    regNumber genGetThisArgReg(GenTreeCall* call) const;

#ifdef TARGET_XARCH
#ifdef TARGET_AMD64
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
    // The following is used to make sure the value of 'GetInterruptible()' isn't
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
    // Methods to abstract target information

    bool validImmForInstr(instruction ins, target_ssize_t val, insFlags flags = INS_FLAGS_DONT_CARE);
    bool validDispForLdSt(target_ssize_t disp, var_types type);
    bool validImmForAdd(target_ssize_t imm, insFlags flags);
    bool validImmForAlu(target_ssize_t imm);
    bool validImmForMov(target_ssize_t imm);
    bool validImmForBL(ssize_t addr);

    instruction ins_Load(var_types srcType, bool aligned = false);
    instruction ins_Store(var_types dstType, bool aligned = false);
    instruction ins_StoreFromSrc(regNumber srcReg, var_types dstType, bool aligned = false);

    // Methods for spilling - used by RegSet
    void spillReg(var_types type, TempDsc* tmp, regNumber reg);
    void reloadReg(var_types type, TempDsc* tmp, regNumber reg);

    // The following method is used by xarch emitter for handling contained tree temps.
    TempDsc* getSpillTempDsc(GenTree* tree);

public:
    emitter* GetEmitter() const
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
    bool GetInterruptible()
    {
        return m_cgInterruptible;
    }
    void SetInterruptible(bool value)
    {
        m_cgInterruptible = value;
    }

#if defined(TARGET_ARMARCH) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)

    bool GetHasTailCalls()
    {
        return m_cgHasTailCalls;
    }
    void SetHasTailCalls(bool value)
    {
        m_cgHasTailCalls = value;
    }
#endif // TARGET_ARMARCH || TARGET_LOONGARCH64 || TARGET_RISCV64

private:
    bool m_cgInterruptible;
#if defined(TARGET_ARMARCH) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    bool m_cgHasTailCalls;
#endif // TARGET_ARMARCH || TARGET_LOONGARCH64 || TARGET_RISCV64

    //  The following will be set to true if we've determined that we need to
    //  generate a full-blown pointer register map for the current method.
    //  Currently it is equal to (GetInterruptible() || !isFramePointerUsed())
    //  (i.e. We generate the full-blown map for EBP-less methods and
    //        for fully interruptible methods)
    //
public:
    bool IsFullPtrRegMapRequired()
    {
        return m_cgFullPtrRegMap;
    }
    void SetFullPtrRegMapRequired(bool value)
    {
        m_cgFullPtrRegMap = value;
    }

private:
    bool m_cgFullPtrRegMap;

public:
    /* These are the different addressing modes used to access a local var.
     * The JIT has to report the location of the locals back to the EE
     * for debugging purposes.
     */

    enum siVarLocType
    {
        VLT_REG,
        VLT_REG_BYREF, // this type is currently only used for value types on X64
        VLT_REG_FP,
        VLT_STK,
        VLT_STK_BYREF, // this type is currently only used for value types on X64
        VLT_REG_REG,
        VLT_REG_STK,
        VLT_STK_REG,
        VLT_STK2,
        VLT_FPSTK,
        VLT_FIXED_VA,

        VLT_COUNT,
        VLT_INVALID
    };

    struct siVarLoc
    {
        siVarLocType vlType;

        union {
            // VLT_REG/VLT_REG_FP -- Any pointer-sized enregistered value (TYP_INT, TYP_REF, etc)
            // eg. EAX
            // VLT_REG_BYREF -- the specified register contains the address of the variable
            // eg. [EAX]

            struct
            {
                regNumber vlrReg;
            } vlReg;

            // VLT_STK       -- Any 32 bit value which is on the stack
            // eg. [ESP+0x20], or [EBP-0x28]
            // VLT_STK_BYREF -- the specified stack location contains the address of the variable
            // eg. mov EAX, [ESP+0x20]; [EAX]

            struct
            {
                regNumber     vlsBaseReg;
                NATIVE_OFFSET vlsOffset;
            } vlStk;

            // VLT_REG_REG -- TYP_LONG/TYP_DOUBLE with both DWords enregistered
            // eg. RBM_EAXEDX

            struct
            {
                regNumber vlrrReg1;
                regNumber vlrrReg2;
            } vlRegReg;

            // VLT_REG_STK -- Partly enregistered TYP_LONG/TYP_DOUBLE
            // eg { LowerDWord=EAX UpperDWord=[ESP+0x8] }

            struct
            {
                regNumber vlrsReg;

                struct
                {
                    regNumber     vlrssBaseReg;
                    NATIVE_OFFSET vlrssOffset;
                } vlrsStk;
            } vlRegStk;

            // VLT_STK_REG -- Partly enregistered TYP_LONG/TYP_DOUBLE
            // eg { LowerDWord=[ESP+0x8] UpperDWord=EAX }

            struct
            {
                struct
                {
                    regNumber     vlsrsBaseReg;
                    NATIVE_OFFSET vlsrsOffset;
                } vlsrStk;

                regNumber vlsrReg;
            } vlStkReg;

            // VLT_STK2 -- Any 64 bit value which is on the stack, in 2 successive DWords
            // eg 2 DWords at [ESP+0x10]

            struct
            {
                regNumber     vls2BaseReg;
                NATIVE_OFFSET vls2Offset;
            } vlStk2;

            // VLT_FPSTK -- enregisterd TYP_DOUBLE (on the FP stack)
            // eg. ST(3). Actually it is ST("FPstkHeight - vpFpStk")

            struct
            {
                unsigned vlfReg;
            } vlFPstk;

            // VLT_FIXED_VA -- fixed argument of a varargs function.
            // The argument location depends on the size of the variable
            // arguments (...). Inspecting the VARARGS_HANDLE indicates the
            // location of the first arg. This argument can then be accessed
            // relative to the position of the first arg

            struct
            {
                unsigned vlfvOffset;
            } vlFixedVarArg;

            // VLT_MEMORY

            struct
            {
                void* rpValue; // pointer to the in-process
                               // location of the value.
            } vlMemory;
        };

        // Helper functions

        bool vlIsInReg(regNumber reg) const;
        bool vlIsOnStack(regNumber reg, signed offset) const;
        bool vlIsOnStack() const;

        void storeVariableInRegisters(regNumber reg, regNumber otherReg);
        void storeVariableOnStack(regNumber stackBaseReg, NATIVE_OFFSET variableStackOffset);

        siVarLoc(const LclVarDsc* varDsc, regNumber baseReg, int offset, bool isFramePointerUsed);
        siVarLoc(){};

        // An overload for the equality comparator
        static bool Equals(const siVarLoc* lhs, const siVarLoc* rhs);

    private:
        // Fill "siVarLoc" properties indicating the register position of the variable
        // using "LclVarDsc" and "baseReg"/"offset" if it has a part in the stack (x64 bit float or long).
        void siFillRegisterVarLoc(
            const LclVarDsc* varDsc, var_types type, regNumber baseReg, int offset, bool isFramePointerUsed);

        // Fill "siVarLoc" properties indicating the register position of the variable
        // using "LclVarDsc" and "baseReg"/"offset" if it is a variable with part in a register and
        // part in thestack
        void siFillStackVarLoc(
            const LclVarDsc* varDsc, var_types type, regNumber baseReg, int offset, bool isFramePointerUsed);
    };

public:
    siVarLoc getSiVarLoc(const LclVarDsc* varDsc, unsigned int stackLevel) const;

#ifdef DEBUG
    void dumpSiVarLoc(const siVarLoc* varLoc) const;
#endif

    unsigned getCurrentStackLevel() const;

protected:
    //  Keeps track of how many bytes we've pushed on the processor's stack.
    unsigned genStackLevel;

public:
    //--------------------------------------------
    //
    // VariableLiveKeeper: Holds an array of "VariableLiveDescriptor", one for each variable
    //  whose location we track. It provides start/end/update/count operations over the
    //  "LiveRangeList" of any variable.
    //
    // Notes:
    //  This method could be implemented on Compiler class too, but the intention is to move code
    //  out of that class, which is huge. With this solution the only code needed in Compiler is
    //  a getter and an initializer of this class.
    //  The index of each variable in this array corresponds to the one in "compiler->lvaTable".
    //  We care about tracking the variable locations of arguments, special arguments, and local IL
    //  variables, and we ignore any other variable (like JIT temporary variables).
    //
    class VariableLiveKeeper
    {
    public:
        //--------------------------------------------
        //
        // VariableLiveRange: Represent part of the life of a variable. A
        //      variable lives in a location (represented with struct "siVarLoc")
        //      between two native offsets.
        //
        // Notes:
        //    We use emitLocation and not NATTIVE_OFFSET because location
        //    is captured when code is being generated (genCodeForBBList
        //    and genGeneratePrologsAndEpilogs) but only after the whole
        //    method's code is generated can we obtain a final, fixed
        //    NATIVE_OFFSET representing the actual generated code offset.
        //    There is also a IL_OFFSET, but this is more accurate and the
        //    debugger is expecting assembly offsets.
        //    This class doesn't have behaviour attached to itself, it is
        //    just putting a name to a representation. It is used to build
        //    typedefs LiveRangeList and LiveRangeListIterator, which are
        //    basically a list of this class and a const_iterator of that
        //    list.
        //
        class VariableLiveRange
        {
        public:
            emitLocation               m_StartEmitLocation; // first position from where "m_VarLocation" becomes valid
            emitLocation               m_EndEmitLocation;   // last position where "m_VarLocation" is valid
            CodeGenInterface::siVarLoc m_VarLocation;       // variable location

            VariableLiveRange(CodeGenInterface::siVarLoc varLocation,
                              emitLocation               startEmitLocation,
                              emitLocation               endEmitLocation)
                : m_StartEmitLocation(startEmitLocation), m_EndEmitLocation(endEmitLocation), m_VarLocation(varLocation)
            {
            }

#ifdef DEBUG
            // Dump "VariableLiveRange" when code has not been generated. We don't have the native code offset,
            // but we do have "emitLocation"s and "siVarLoc".
            void dumpVariableLiveRange(const CodeGenInterface* codeGen) const;

            // Dump "VariableLiveRange" when code has been generated and we have the native code offset of each
            // "emitLocation"
            void dumpVariableLiveRange(emitter* emit, const CodeGenInterface* codeGen) const;
#endif // DEBUG
        };

        typedef jitstd::list<VariableLiveRange> LiveRangeList;
        typedef LiveRangeList::const_iterator   LiveRangeListIterator;

    private:
#ifdef DEBUG
        //--------------------------------------------
        //
        // LiveRangeDumper: Used for debugging purposes during code
        //  generation on genCodeForBBList. Keeps an iterator to the first
        //  edited/added "VariableLiveRange" of a variable during the
        //  generation of code of one block.
        //
        // Notes:
        //  The first "VariableLiveRange" reported for a variable during
        //  a BasicBlock is sent to "setDumperStartAt" so we can dump all
        //  the "VariableLiveRange"s from that one.
        //  After we dump all the "VariableLiveRange"s we call "reset" with
        //  the "liveRangeList" to set the barrier to nullptr or the last
        //  "VariableLiveRange" if it is opened.
        //  If no "VariableLiveRange" was edited/added during block,
        //  the iterator points to the end of variable's LiveRangeList.
        //
        class LiveRangeDumper
        {
            // Iterator to the first edited/added position during actual block code generation. If last
            // block had a closed "VariableLiveRange" (with a valid "m_EndEmitLocation") and no changes
            // were applied to variable liveness, it points to the end of variable's LiveRangeList.
            LiveRangeListIterator m_startingLiveRange;
            bool                  m_hasLiveRangesToDump; // True if a live range for this variable has been
                                                         // reported from last call to EndBlock

        public:
            LiveRangeDumper(const LiveRangeList* liveRanges)
                : m_startingLiveRange(liveRanges->end()), m_hasLiveRangesToDump(false){};

            // Make the dumper point to the last "VariableLiveRange" opened or nullptr if all are closed
            void resetDumper(const LiveRangeList* list);

            // Make "LiveRangeDumper" instance point at the last "VariableLiveRange" added so we can
            // start dumping from there after the "BasicBlock"s code is generated.
            void setDumperStartAt(const LiveRangeListIterator liveRangeIt);

            // Return an iterator to the first "VariableLiveRange" edited/added during the current
            // "BasicBlock"
            LiveRangeListIterator getStartForDump() const;

            // Return whether at least a "VariableLiveRange" was alive during the current "BasicBlock"'s
            // code generation
            bool hasLiveRangesToDump() const;
        };
#endif // DEBUG

        //--------------------------------------------
        //
        // VariableLiveDescriptor: This class persist and update all the changes
        //  to the home of a variable. It has an instance of "LiveRangeList"
        //  and methods to report the start/end of a VariableLiveRange.
        //
        class VariableLiveDescriptor
        {
            LiveRangeList* m_VariableLiveRanges; // the variable locations of this variable
            INDEBUG(LiveRangeDumper* m_VariableLifeBarrier;)
            INDEBUG(unsigned m_varNum;)

        public:
            VariableLiveDescriptor(CompAllocator allocator DEBUG_ARG(unsigned varNum));

            bool           hasVariableLiveRangeOpen() const;
            LiveRangeList* getLiveRanges() const;

            void startLiveRangeFromEmitter(CodeGenInterface::siVarLoc varLocation, emitter* emit) const;
            void endLiveRangeAtEmitter(emitter* emit) const;
            void updateLiveRangeAtEmitter(CodeGenInterface::siVarLoc varLocation, emitter* emit) const;

#ifdef DEBUG
            void dumpAllRegisterLiveRangesForBlock(emitter* emit, const CodeGenInterface* codeGen) const;
            void dumpRegisterLiveRangesForBlockBeforeCodeGenerated(const CodeGenInterface* codeGen) const;
            bool hasVarLiveRangesToDump() const;
            bool hasVarLiveRangesFromLastBlockToDump() const;
            void endBlockLiveRanges();
#endif // DEBUG
        };

        unsigned int m_LiveDscCount;  // count of args, special args, and IL local variables to report home
        unsigned int m_LiveArgsCount; // count of arguments to report home

        Compiler* m_Compiler;

        VariableLiveDescriptor* m_vlrLiveDsc; // Array of descriptors that manage VariableLiveRanges.
                                              // Its indices correspond to lvaTable indexes (or lvSlotNum).

        VariableLiveDescriptor* m_vlrLiveDscForProlog; // Array of descriptors that manage VariableLiveRanges.
                                                       // Its indices correspond to lvaTable indexes (or lvSlotNum).

        bool m_LastBasicBlockHasBeenEmitted; // When true no more siEndVariableLiveRange is considered.
                                             // No update/start happens when code has been generated.

    public:
        VariableLiveKeeper(unsigned int  totalLocalCount,
                           unsigned int  argsCount,
                           Compiler*     compiler,
                           CompAllocator allocator);

        // For tracking locations during code generation
        void siStartOrCloseVariableLiveRange(const LclVarDsc* varDsc, unsigned int varNum, bool isBorn, bool isDying);
        void siStartOrCloseVariableLiveRanges(VARSET_VALARG_TP varsIndexSet, bool isBorn, bool isDying);
        void siStartVariableLiveRange(const LclVarDsc* varDsc, unsigned int varNum);
        void siEndVariableLiveRange(unsigned int varNum);
        void siUpdateVariableLiveRange(const LclVarDsc* varDsc, unsigned int varNum);
        void siEndAllVariableLiveRange(VARSET_VALARG_TP varsToClose);
        void siEndAllVariableLiveRange();

        LiveRangeList* getLiveRangesForVarForBody(unsigned int varNum) const;
        LiveRangeList* getLiveRangesForVarForProlog(unsigned int varNum) const;
        size_t getLiveRangesCount() const;

        // For parameters locations on prolog
        void psiStartVariableLiveRange(CodeGenInterface::siVarLoc varLocation, unsigned int varNum);
        void psiClosePrologVariableRanges();

#ifdef DEBUG
        void dumpBlockVariableLiveRanges(const BasicBlock* block);
        void dumpLvaVariableLiveRanges() const;
#endif // DEBUG
    };

    void initializeVariableLiveKeeper();

    VariableLiveKeeper* getVariableLiveKeeper() const;

protected:
    VariableLiveKeeper* varLiveKeeper; // Used to manage VariableLiveRanges of variables

#ifdef LATE_DISASM
public:
    virtual const char* siRegVarName(size_t offs, size_t size, unsigned reg) = 0;

    virtual const char* siStackVarName(size_t offs, size_t size, unsigned reg, unsigned stkOffs) = 0;
#endif // LATE_DISASM

#if defined(TARGET_XARCH)
    bool IsEmbeddedBroadcastEnabled(instruction ins, GenTree* op);
#endif
};

#endif // _CODEGEN_INTERFACE_H_
