// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************/

#ifndef _REGSET_H
#define _REGSET_H
#include "vartype.h"
#include "target.h"

class LclVarDsc;
class TempDsc;
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

#ifdef TARGET_ARM
    regMaskTP rsMaskPreSpillRegs(bool includeAlignment) const
    {
        return includeAlignment ? (rsMaskPreSpillRegArg | rsMaskPreSpillAlign) : rsMaskPreSpillRegArg;
    }
#endif // TARGET_ARM

private:
    // The same descriptor is also used for 'multi-use' register tracking, BTW.
    struct SpillDsc
    {
        SpillDsc* spillNext; // next spilled value of same reg
        GenTree*  spillTree; // the value that was spilled
        TempDsc*  spillTemp; // the temp holding the spilled value

        static SpillDsc* alloc(Compiler* pComp, RegSet* regSet, var_types type);
        static void freeDsc(RegSet* regSet, SpillDsc* spillDsc);
    };

    //-------------------------------------------------------------------------
    //
    //  Track the status of the registers
    //

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

    void verifyRegUsed(regNumber reg);

    void verifyRegistersUsed(regMaskTP regMask);

public:
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

#ifdef TARGET_ARMARCH
    regMaskTP rsMaskCalleeSaved; // mask of the registers pushed/popped in the prolog/epilog
#endif                           // TARGET_ARM

public:                    // TODO-Cleanup: Should be private, but Compiler uses it
    regMaskTP rsMaskResvd; // mask of the registers that are reserved for special purposes (typically empty)

public: // The PreSpill masks are used in LclVars.cpp
#ifdef TARGET_ARM
    regMaskTP rsMaskPreSpillAlign;  // Mask of alignment padding added to prespill to keep double aligned args
                                    // at aligned stack addresses.
    regMaskTP rsMaskPreSpillRegArg; // mask of incoming registers that are spilled at the start of the prolog
                                    // This includes registers used to pass a struct (or part of a struct)
                                    // and all enregistered user arguments in a varargs call
#endif                              // TARGET_ARM

private:
    //-------------------------------------------------------------------------
    //
    //  The following tables keep track of spilled register values.
    //

    // When a register gets spilled, the old information is stored here
    SpillDsc* rsSpillDesc[REG_COUNT];
    SpillDsc* rsSpillFree; // list of unused spill descriptors

    void rsSpillChk();
    void rsSpillInit();
    void rsSpillDone();
    void rsSpillBeg();
    void rsSpillEnd();

    void rsSpillTree(regNumber reg, GenTree* tree, unsigned regIdx = 0);

#if defined(TARGET_X86)
    void rsSpillFPStack(GenTreeCall* call);
#endif // defined(TARGET_X86)

    SpillDsc* rsGetSpillInfo(GenTree* tree, regNumber reg, SpillDsc** pPrevDsc = nullptr);

    TempDsc* rsGetSpillTempWord(regNumber oldReg, SpillDsc* dsc, SpillDsc* prevDsc);

    TempDsc* rsUnspillInPlace(GenTree* tree, regNumber oldReg, unsigned regIdx = 0);

    void rsMarkSpill(GenTree* tree, regNumber reg);

public:
    void tmpInit();

    enum TEMP_USAGE_TYPE
    {
        TEMP_USAGE_FREE,
        TEMP_USAGE_USED
    };

    static var_types tmpNormalizeType(var_types type);
    TempDsc* tmpGetTemp(var_types type); // get temp for the given type
    void tmpRlsTemp(TempDsc* temp);
    TempDsc* tmpFindNum(int temp, TEMP_USAGE_TYPE usageType = TEMP_USAGE_FREE) const;

    void     tmpEnd();
    TempDsc* tmpListBeg(TEMP_USAGE_TYPE usageType = TEMP_USAGE_FREE) const;
    TempDsc* tmpListNxt(TempDsc* curTemp, TEMP_USAGE_TYPE usageType = TEMP_USAGE_FREE) const;
    void tmpDone();

#ifdef DEBUG
    bool tmpAllFree() const;
#endif // DEBUG

    void tmpPreAllocateTemps(var_types type, unsigned count);

    unsigned tmpGetTotalSize()
    {
        return tmpSize;
    }

private:
    unsigned tmpCount; // Number of temps
    unsigned tmpSize;  // Size of all the temps
#ifdef DEBUG
    // Used by RegSet::rsSpillChk()
    unsigned tmpGetCount; // Temps which haven't been released yet
#endif
    static unsigned tmpSlot(unsigned size); // which slot in tmpFree[] or tmpUsed[] to use

    enum TEMP_CONSTANTS : unsigned
    {
#if defined(FEATURE_SIMD)
#if defined(TARGET_XARCH)
        TEMP_MAX_SIZE = YMM_REGSIZE_BYTES,
#elif defined(TARGET_ARM64)
        TEMP_MAX_SIZE = FP_REGSIZE_BYTES,
#endif // defined(TARGET_XARCH) || defined(TARGET_ARM64)
#else  // !FEATURE_SIMD
        TEMP_MAX_SIZE = sizeof(double),
#endif // !FEATURE_SIMD
        TEMP_SLOT_COUNT = (TEMP_MAX_SIZE / sizeof(int))
    };

    TempDsc* tmpFree[TEMP_MAX_SIZE / sizeof(int)];
    TempDsc* tmpUsed[TEMP_MAX_SIZE / sizeof(int)];
};

#endif // _REGSET_H
