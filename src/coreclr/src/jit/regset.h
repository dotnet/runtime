// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*****************************************************************************/

#ifndef _REGSET_H
#define _REGSET_H
#include "vartype.h"
#include "target.h"

class LclVarDsc;
class TempDsc;
typedef struct GenTree* GenTreePtr;
class Compiler;
class CodeGen;
class GCInfo;

/*
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                           RegSet                                          XX
XX                                                                           XX
XX  Represents the register set, and their states during code generation     XX
XX  Can select an unused register, keeps track of the contents of the        XX
XX  registers, and can spill registers                                       XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

/*****************************************************************************
*
*  Keep track of the current state of each register. This is intended to be
*  used for things like register reload suppression, but for now the only
*  thing it does is note which registers we use in each method.
*/

enum regValKind
{
    RV_TRASH,          // random unclassified garbage
    RV_INT_CNS,        // integer constant
    RV_LCL_VAR,        // local variable value
    RV_LCL_VAR_LNG_LO, // lower half of long local variable
    RV_LCL_VAR_LNG_HI,
};

/*****************************************************************************/

class RegSet
{
    friend class CodeGen;
    friend class CodeGenInterface;

private:
    Compiler* m_rsCompiler;
    GCInfo&   m_rsGCInfo;

public:
    RegSet(Compiler* compiler, GCInfo& gcInfo);

#ifdef _TARGET_ARM_
    regMaskTP rsMaskPreSpillRegs(bool includeAlignment)
    {
        return includeAlignment ? (rsMaskPreSpillRegArg | rsMaskPreSpillAlign) : rsMaskPreSpillRegArg;
    }
#endif // _TARGET_ARM_

private:
    // The same descriptor is also used for 'multi-use' register tracking, BTW.
    struct SpillDsc
    {
        SpillDsc* spillNext; // next spilled value of same reg

        union {
            GenTreePtr spillTree; // the value that was spilled
#ifdef LEGACY_BACKEND
            LclVarDsc* spillVarDsc; // variable if it's an enregistered variable
#endif                              // LEGACY_BACKEND
        };

        TempDsc* spillTemp; // the temp holding the spilled value

#ifdef LEGACY_BACKEND
        GenTreePtr spillAddr; // owning complex address mode or nullptr

        union {
            bool spillMoreMultis;
            bool bEnregisteredVariable; // For FP. Indicates that what was spilled was
                                        // an enregistered variable
        };
#endif // LEGACY_BACKEND

        static SpillDsc* alloc(Compiler* pComp, RegSet* regSet, var_types type);
        static void freeDsc(RegSet* regSet, SpillDsc* spillDsc);
    };

#ifdef LEGACY_BACKEND
public:
    regMaskTP rsUseIfZero(regMaskTP regs, regMaskTP includeHint);
#endif // LEGACY_BACKEND

//-------------------------------------------------------------------------
//
//  Track the status of the registers
//
#ifdef LEGACY_BACKEND
public:                               // TODO-Cleanup: Should be private, but Compiler uses it
    GenTreePtr rsUsedTree[REG_COUNT]; // trees currently sitting in the registers
private:
    GenTreePtr rsUsedAddr[REG_COUNT];  // addr for which rsUsedTree[reg] is a part of the addressing mode
    SpillDsc*  rsMultiDesc[REG_COUNT]; // keeps track of 'multiple-use' registers.
#endif                                 // LEGACY_BACKEND

private:
    bool      rsNeededSpillReg;   // true if this method needed to spill any registers
    regMaskTP rsModifiedRegsMask; // mask of the registers modified by the current function.

#ifdef DEBUG
    bool rsModifiedRegsMaskInitialized; // Has rsModifiedRegsMask been initialized? Guards against illegal use.
#endif                                  // DEBUG

public:
    regMaskTP rsGetModifiedRegsMask() const
    {
        assert(rsModifiedRegsMaskInitialized);
        return rsModifiedRegsMask;
    }

    void rsClearRegsModified();

    void rsSetRegsModified(regMaskTP mask DEBUGARG(bool suppressDump = false));

    void rsRemoveRegsModified(regMaskTP mask);

    bool rsRegsModified(regMaskTP mask) const
    {
        assert(rsModifiedRegsMaskInitialized);
        return (rsModifiedRegsMask & mask) != 0;
    }

public: // TODO-Cleanup: Should be private, but GCInfo uses them
#ifdef LEGACY_BACKEND
    regMaskTP rsMaskUsed; // currently 'used' registers mask
#endif                    // LEGACY_BACKEND

    __declspec(property(get = GetMaskVars, put = SetMaskVars)) regMaskTP rsMaskVars; // mask of registers currently
                                                                                     // allocated to variables

    regMaskTP GetMaskVars() const // 'get' property function for rsMaskVars property
    {
        return _rsMaskVars;
    }

    void SetMaskVars(regMaskTP newMaskVars); // 'put' property function for rsMaskVars property

    void AddMaskVars(regMaskTP addMaskVars) // union 'addMaskVars' with the rsMaskVars set
    {
        SetMaskVars(_rsMaskVars | addMaskVars);
    }

    void RemoveMaskVars(regMaskTP removeMaskVars) // remove 'removeMaskVars' from the rsMaskVars set (like bitset DiffD)
    {
        SetMaskVars(_rsMaskVars & ~removeMaskVars);
    }

    void ClearMaskVars() // Like SetMaskVars(RBM_NONE), but without any debug output.
    {
        _rsMaskVars = RBM_NONE;
    }

private:
    regMaskTP _rsMaskVars; // backing store for rsMaskVars property

#ifdef LEGACY_BACKEND
    regMaskTP rsMaskLock; // currently 'locked' registers mask
    regMaskTP rsMaskMult; // currently 'multiply used' registers mask
#endif                    // LEGACY_BACKEND

#ifdef _TARGET_ARMARCH_
    regMaskTP rsMaskCalleeSaved; // mask of the registers pushed/popped in the prolog/epilog
#endif                           // _TARGET_ARM_

public:                    // TODO-Cleanup: Should be private, but Compiler uses it
    regMaskTP rsMaskResvd; // mask of the registers that are reserved for special purposes (typically empty)

public: // The PreSpill masks are used in LclVars.cpp
#ifdef _TARGET_ARM_
    regMaskTP rsMaskPreSpillAlign;  // Mask of alignment padding added to prespill to keep double aligned args
                                    // at aligned stack addresses.
    regMaskTP rsMaskPreSpillRegArg; // mask of incoming registers that are spilled at the start of the prolog
                                    // This includes registers used to pass a struct (or part of a struct)
                                    // and all enregistered user arguments in a varargs call
#endif                              // _TARGET_ARM_

#ifdef LEGACY_BACKEND

private:
    // These getters/setters are ifdef here so that the accesses to these values in sharedfloat.cpp are redirected
    // to the appropriate value.
    // With FEATURE_STACK_FP_X87 (x86 FP codegen) we have separate register mask that just handle FP registers.
    // For all other platforms (and eventually on x86) we use unified register masks that handle both kinds.
    //
    regMaskTP rsGetMaskUsed(); // Getter for rsMaskUsed or rsMaskUsedFloat
    regMaskTP rsGetMaskVars(); // Getter for rsMaskVars or rsMaskRegVarFloat
    regMaskTP rsGetMaskLock(); // Getter for rsMaskLock or rsMaskLockedFloat
    regMaskTP rsGetMaskMult(); // Getter for rsMaskMult or 0

    void rsSetMaskUsed(regMaskTP maskUsed); // Setter for rsMaskUsed or rsMaskUsedFloat
    void rsSetMaskVars(regMaskTP maskVars); // Setter for rsMaskVars or rsMaskRegVarFloat
    void rsSetMaskLock(regMaskTP maskLock); // Setter for rsMaskLock or rsMaskLockedFloat

    void rsSetUsedTree(regNumber regNum, GenTreePtr tree);  // Setter for  rsUsedTree[]/genUsedRegsFloat[]
    void rsFreeUsedTree(regNumber regNum, GenTreePtr tree); // Free   for  rsUsedTree[]/genUsedRegsFloat[]

public:
    regPairNo rsFindRegPairNo(regMaskTP regMask);

private:
    bool rsIsTreeInReg(regNumber reg, GenTreePtr tree);

    regMaskTP rsExcludeHint(regMaskTP regs, regMaskTP excludeHint);
    regMaskTP rsNarrowHint(regMaskTP regs, regMaskTP narrowHint);
    regMaskTP rsMustExclude(regMaskTP regs, regMaskTP exclude);
    regMaskTP rsRegMaskFree();
    regMaskTP rsRegMaskCanGrab();

    void rsMarkRegUsed(GenTreePtr tree, GenTreePtr addr = 0);
    // A special case of "rsMarkRegUsed": the register used is an argument register, used to hold part of
    // the given argument node "promotedStructArg".  (The name suggests that we're likely to use use this
    // for register holding a promoted struct argument, but the implementation doesn't depend on that.)  The
    // "isGCRef" argument indicates whether the register contains a GC reference.
    void rsMarkArgRegUsedByPromotedFieldArg(GenTreePtr promotedStructArg, regNumber regNum, bool isGCRef);

    void rsMarkRegPairUsed(GenTreePtr tree);

    void rsMarkRegFree(regMaskTP regMask);
    void rsMarkRegFree(regNumber reg, GenTreePtr tree);
    void rsMultRegFree(regMaskTP regMask);
    unsigned rsFreeNeededRegCount(regMaskTP needReg);

    void rsLockReg(regMaskTP regMask);
    void rsUnlockReg(regMaskTP regMask);
    void rsLockUsedReg(regMaskTP regMask);
    void rsUnlockUsedReg(regMaskTP regMask);
    void rsLockReg(regMaskTP regMask, regMaskTP* usedMask);
    void rsUnlockReg(regMaskTP regMask, regMaskTP usedMask);

    regMaskTP rsRegExclMask(regMaskTP regMask, regMaskTP rmvMask);

    regNumber rsPickRegInTmpOrder(regMaskTP regMask);

public: // used by emitter (!)
    regNumber rsGrabReg(regMaskTP regMask);

private:
    regNumber rsPickReg(regMaskTP regMask = RBM_NONE, regMaskTP regBest = RBM_NONE);

public: // used by emitter (!)
    regNumber rsPickFreeReg(regMaskTP regMaskHint = RBM_ALLINT);

private:
    regPairNo rsGrabRegPair(regMaskTP regMask);
    regPairNo rsPickRegPair(regMaskTP regMask);

    class RegisterPreference
    {
    public:
        regMaskTP ok;
        regMaskTP best;
        RegisterPreference(regMaskTP _ok, regMaskTP _best)
        {
            ok   = _ok;
            best = _best;
        }
    };
    regNumber PickRegFloat(GenTreePtr          tree,
                           var_types           type  = TYP_DOUBLE,
                           RegisterPreference* pref  = NULL,
                           bool                bUsed = true);
    regNumber PickRegFloat(var_types type = TYP_DOUBLE, RegisterPreference* pref = NULL, bool bUsed = true);
    regNumber PickRegFloatOtherThan(GenTreePtr tree, var_types type, regNumber reg);
    regNumber PickRegFloatOtherThan(var_types type, regNumber reg);

    regMaskTP RegFreeFloat();

    void SetUsedRegFloat(GenTreePtr tree, bool bValue);
    void SetLockedRegFloat(GenTreePtr tree, bool bValue);
    bool IsLockedRegFloat(GenTreePtr tree);

    var_types rsRmvMultiReg(regNumber reg);
    void rsRecMultiReg(regNumber reg, var_types type);
#endif // LEGACY_BACKEND

public:
#ifdef DEBUG
    /*****************************************************************************
        *  Should we stress register tracking logic ?
        *  This is set via COMPlus_JitStressRegs.
        *  The following values are ordered, such that any value greater than RS_xx
        *  implies RS_xx.
        *  LSRA defines a different set of values, but uses the same COMPlus_JitStressRegs
        *  value, with the same notion of relative ordering.
        *  1 = rsPickReg() picks 'bad' registers.
        *  2 = codegen spills at safe points. This is still flaky
        */
    enum rsStressRegsType
    {
        RS_STRESS_NONE  = 0,
        RS_PICK_BAD_REG = 01,
        RS_SPILL_SAFE   = 02,
    };
    rsStressRegsType rsStressRegs();
#endif // DEBUG

private:
    //-------------------------------------------------------------------------
    //
    //  The following tables keep track of spilled register values.
    //

    // When a register gets spilled, the old information is stored here
    SpillDsc* rsSpillDesc[REG_COUNT];
    SpillDsc* rsSpillFree; // list of unused spill descriptors

#ifdef LEGACY_BACKEND
    SpillDsc* rsSpillFloat;
#endif // LEGACY_BACKEND

    void rsSpillChk();
    void rsSpillInit();
    void rsSpillDone();
    void rsSpillBeg();
    void rsSpillEnd();

    void rsSpillTree(regNumber reg, GenTreePtr tree, unsigned regIdx = 0);

#if defined(_TARGET_X86_) && !FEATURE_STACK_FP_X87
    void rsSpillFPStack(GenTreeCall* call);
#endif // defined(_TARGET_X86_) && !FEATURE_STACK_FP_X87

#ifdef LEGACY_BACKEND
    void rsSpillReg(regNumber reg);
    void rsSpillRegIfUsed(regNumber reg);
    void rsSpillRegs(regMaskTP regMask);
#endif // LEGACY_BACKEND

    SpillDsc* rsGetSpillInfo(GenTreePtr tree,
                             regNumber  reg,
                             SpillDsc** pPrevDsc = nullptr
#ifdef LEGACY_BACKEND
                             ,
                             SpillDsc** pMultiDsc = NULL
#endif // LEGACY_BACKEND
                             );

    TempDsc* rsGetSpillTempWord(regNumber oldReg, SpillDsc* dsc, SpillDsc* prevDsc);

#ifdef LEGACY_BACKEND
    enum ExactReg
    {
        ANY_REG,
        EXACT_REG
    };
    enum KeepReg
    {
        FREE_REG,
        KEEP_REG
    };

    regNumber rsUnspillOneReg(GenTreePtr tree, regNumber oldReg, KeepReg willKeepNewReg, regMaskTP needReg);
#endif // LEGACY_BACKEND

    TempDsc* rsUnspillInPlace(GenTreePtr tree, regNumber oldReg, unsigned regIdx = 0);

#ifdef LEGACY_BACKEND
    void rsUnspillReg(GenTreePtr tree, regMaskTP needReg, KeepReg keepReg);

    void rsUnspillRegPair(GenTreePtr tree, regMaskTP needReg, KeepReg keepReg);
#endif // LEGACY_BACKEND

    void rsMarkSpill(GenTreePtr tree, regNumber reg);

#ifdef LEGACY_BACKEND
    void rsMarkUnspill(GenTreePtr tree, regNumber reg);
#endif // LEGACY_BACKEND

#if FEATURE_STACK_FP_X87
    regMaskTP  rsMaskUsedFloat;
    regMaskTP  rsMaskRegVarFloat;
    regMaskTP  rsMaskLockedFloat;
    GenTreePtr genUsedRegsFloat[REG_FPCOUNT];
    LclVarDsc* genRegVarsFloat[REG_FPCOUNT];
#endif // FEATURE_STACK_FP_X87
};

//-------------------------------------------------------------------------
//
//  These are used to track the contents of the registers during
//  code generation.
//
//  Only integer registers are tracked.
//

struct RegValDsc
{
    regValKind rvdKind;
    union {
        ssize_t  rvdIntCnsVal; // for rvdKind == RV_INT_CNS
        unsigned rvdLclVarNum; // for rvdKind == RV_LCL_VAR, RV_LCL_VAR_LNG_LO, RV_LCL_VAR_LNG_HI
    };
};

class RegTracker
{
    Compiler* compiler;
    RegSet*   regSet;
    RegValDsc rsRegValues[REG_COUNT];

public:
    void rsTrackInit(Compiler* comp, RegSet* rs)
    {
        compiler = comp;
        regSet   = rs;
        rsTrackRegClr();
    }

    void rsTrackRegClr();
    void rsTrackRegClrPtr();
    void rsTrackRegTrash(regNumber reg);
    void rsTrackRegMaskTrash(regMaskTP regMask);
    regMaskTP rsTrashRegsForGCInterruptability();
    void rsTrackRegIntCns(regNumber reg, ssize_t val);
    void rsTrackRegLclVar(regNumber reg, unsigned var);
    void rsTrackRegLclVarLng(regNumber reg, unsigned var, bool low);
    bool rsTrackIsLclVarLng(regValKind rvKind);
    void rsTrackRegClsVar(regNumber reg, GenTreePtr clsVar);
    void rsTrackRegCopy(regNumber reg1, regNumber reg2);
    void rsTrackRegSwap(regNumber reg1, regNumber reg2);
    void rsTrackRegAssign(GenTree* op1, GenTree* op2);

    regNumber rsIconIsInReg(ssize_t val, ssize_t* closeDelta = nullptr);
    bool rsIconIsInReg(ssize_t val, regNumber reg);
    regNumber rsLclIsInReg(unsigned var);
    regPairNo rsLclIsInRegPair(unsigned var);

//---------------------- Load suppression ---------------------------------

#if REDUNDANT_LOAD

    void rsTrashLclLong(unsigned var);
    void rsTrashLcl(unsigned var);
    void rsTrashRegSet(regMaskTP regMask);

    regMaskTP rsUselessRegs();

#endif // REDUNDANT_LOAD
};
#endif // _REGSET_H
