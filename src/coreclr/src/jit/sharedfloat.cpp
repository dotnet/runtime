// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// NOTE: The code in this file is only used for LEGACY_BACKEND compiles.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "compiler.h"
#include "emit.h"
#include "codegen.h"

#ifdef LEGACY_BACKEND

#if FEATURE_STACK_FP_X87
    regMaskTP RegSet::rsGetMaskUsed()                    { return rsMaskUsedFloat; }
    regMaskTP RegSet::rsGetMaskVars()                    { return rsMaskRegVarFloat; }
    regMaskTP RegSet::rsGetMaskLock()                    { return rsMaskLockedFloat; }
    regMaskTP RegSet::rsGetMaskMult()                    { return 0; }

    void      RegSet::rsSetMaskUsed(regMaskTP maskUsed)  { rsMaskUsedFloat   = maskUsed; }
    void      RegSet::rsSetMaskVars(regMaskTP maskVars)  { rsMaskRegVarFloat = maskVars; }
    void      RegSet::rsSetMaskLock(regMaskTP maskLock)  { rsMaskLockedFloat = maskLock; }
    
    void      RegSet::rsSetUsedTree(regNumber  regNum, GenTreePtr tree)
    {
        assert(genUsedRegsFloat[regNum] == 0); 
        genUsedRegsFloat[regNum] = tree;
    }
    void      RegSet::rsFreeUsedTree(regNumber  regNum, GenTreePtr tree)
    {
        assert(genUsedRegsFloat[regNum] == tree); 
        genUsedRegsFloat[regNum] = 0;
    }

#else // !FEATURE_STACK_FP_X87
    regMaskTP RegSet::rsGetMaskUsed()                    { return rsMaskUsed; }
    regMaskTP RegSet::rsGetMaskVars()                    { return rsMaskVars; }
    regMaskTP RegSet::rsGetMaskLock()                    { return rsMaskLock; }
    regMaskTP RegSet::rsGetMaskMult()                    { return rsMaskMult; }

    void      RegSet::rsSetMaskUsed(regMaskTP maskUsed)  { rsMaskUsed = maskUsed; }
    void      RegSet::rsSetMaskVars(regMaskTP maskVars)  { rsMaskVars = maskVars; }
    void      RegSet::rsSetMaskLock(regMaskTP maskLock)  { rsMaskLock = maskLock; }

    void      RegSet::rsSetUsedTree(regNumber  regNum, GenTreePtr tree)
    {
        assert(rsUsedTree[regNum] == 0); 
        rsUsedTree[regNum] = tree;
    }
    void      RegSet::rsFreeUsedTree(regNumber  regNum, GenTreePtr tree)
    {
        assert(rsUsedTree[regNum] == tree); 
        rsUsedTree[regNum] = 0;
    }
#endif // !FEATURE_STACK_FP_X87

// float stress mode. Will lock out registers to stress high register pressure.
// This implies setting interferences in register allocator and pushing regs in
// the prolog and popping them before a ret.
#ifdef DEBUG    
int CodeGenInterface::genStressFloat()
{
    return compiler->compStressCompile(Compiler::STRESS_FLATFP, 40)?1:JitConfig.JitStressFP();
}
#endif

regMaskTP  RegSet::RegFreeFloat()
{
    regMaskTP mask = RBM_ALLFLOAT;
#if FEATURE_FP_REGALLOC
    mask &= m_rsCompiler->raConfigRestrictMaskFP();
#endif

    mask &= ~rsGetMaskUsed();
    mask &= ~rsGetMaskLock();
    mask &= ~rsGetMaskVars();

#ifdef DEBUG    
    if (m_rsCompiler->codeGen->genStressFloat())
    {
        mask &= ~(m_rsCompiler->codeGen->genStressLockedMaskFloat());
    }
#endif
    return mask;
}

#ifdef _TARGET_ARM_
// order registers are picked 
// go in reverse order to minimize chance of spilling with calls
static const regNumber pickOrder[] = {REG_F15, REG_F14, REG_F13, REG_F12, 
                                      REG_F11, REG_F10, REG_F9,  REG_F8, 
                                      REG_F7,  REG_F6,  REG_F5,  REG_F4, 
                                      REG_F3,  REG_F2,  REG_F1,  REG_F0, 

                                      REG_F16, REG_F17, REG_F18, REG_F19,
                                      REG_F20, REG_F21, REG_F22, REG_F23, 
                                      REG_F24, REG_F25, REG_F26, REG_F27, 
                                      REG_F28, REG_F29, REG_F30, REG_F31
};

#elif _TARGET_AMD64_
// order registers are picked 
static const regNumber pickOrder[] = {REG_XMM0,  REG_XMM1,  REG_XMM2,  REG_XMM3, 
                                      REG_XMM4,  REG_XMM5,  REG_XMM6,  REG_XMM7, 
                                      REG_XMM8,  REG_XMM9,  REG_XMM10, REG_XMM11, 
                                      REG_XMM12, REG_XMM13, REG_XMM14, REG_XMM15};

#elif _TARGET_X86_
// order registers are picked 
static const regNumber pickOrder[] = {REG_FPV0, REG_FPV1, REG_FPV2, REG_FPV3, 
                                      REG_FPV4, REG_FPV5, REG_FPV6, REG_FPV7};
#endif

// picks a reg other than the one specified
regNumber RegSet::PickRegFloatOtherThan(GenTreePtr tree, var_types type, regNumber reg)
{
    return PickRegFloatOtherThan(type, reg);
}

regNumber RegSet::PickRegFloatOtherThan(var_types type, regNumber reg)
{
    RegisterPreference pref(RBM_ALLFLOAT ^ genRegMask(reg), 0);
    return PickRegFloat(type, &pref);
}

regNumber RegSet::PickRegFloat(GenTreePtr tree, var_types type, RegisterPreference * pref, bool bUsed)
{
    return PickRegFloat(type, pref, bUsed);
}

regNumber RegSet::PickRegFloat(var_types type, RegisterPreference *pref, bool bUsed)
{
    regMaskTP wantedMask;
    bool      tryBest = true;
    bool      tryOk   = true;
    bool      bSpill  = false;
    regNumber reg    = REG_NA;

    while (tryOk)
    {
        if (pref)
        {
            if (tryBest)
            {
                wantedMask = pref->best;
                tryBest = false;
            }
            else
            {
                assert(tryOk);
                wantedMask = pref->ok;
                tryOk = false;
            }
        }
        else// pref is NULL
        {
            wantedMask = RBM_ALLFLOAT;
            tryBest = false;
            tryOk = false;
        }
        
        // better not have asked for a non-fp register
        assert((wantedMask & ~RBM_ALLFLOAT) == 0);
   
        regMaskTP availMask = RegFreeFloat();
        regMaskTP OKmask = availMask & wantedMask;

        if (OKmask == 0)
        {
            if (tryOk)
            {
                // the pref->best mask doesn't work so try the pref->ok mask next
                continue;
            }

            if (bUsed)
            {
                // Allow used registers to be picked
                OKmask |= rsGetMaskUsed() & ~rsGetMaskLock();
                bSpill = true;
            }
        }
#if FEATURE_FP_REGALLOC
        regMaskTP restrictMask = (m_rsCompiler->raConfigRestrictMaskFP() | RBM_FLT_CALLEE_TRASH);
#endif

        for (unsigned i=0; i<ArrLen(pickOrder); i++)
        {
            regNumber r = pickOrder[i];
            if (!floatRegCanHoldType(r, type))
                continue;

            regMaskTP mask = genRegMaskFloat(r, type);

#if FEATURE_FP_REGALLOC
            if ((mask & restrictMask) != mask)
                continue;
#endif
            if ((OKmask & mask) == mask)
            {
                reg = r;
                goto RET;
            }
        }

        if (tryOk)
        {
            // We couldn't find a register using tryBest
            continue;
        }

        assert(!"Unable to find a free FP virtual register");
        NO_WAY("FP register allocator was too optimistic!");
    }
RET:
    if (bSpill)
    {
        m_rsCompiler->codeGen->SpillFloat(reg);
    }

#if FEATURE_FP_REGALLOC
    rsSetRegsModified(genRegMaskFloat(reg, type));
#endif

    return reg;
}


void RegSet::SetUsedRegFloat(GenTreePtr tree, bool bValue)
{
    /* The value must be sitting in a register */
    assert(tree);
    assert(tree->gtFlags & GTF_REG_VAL);

    var_types  type    = tree->TypeGet();
#ifdef _TARGET_ARM_
    if (type == TYP_STRUCT)
    {
        assert(m_rsCompiler->IsHfa(tree));
        type = TYP_FLOAT;
    }
#endif
    regNumber  regNum  = tree->gtRegNum;
    regMaskTP  regMask = genRegMaskFloat(regNum, type);

    if (bValue)
    {
        // Mark as used

#ifdef  DEBUG
        if  (m_rsCompiler->verbose)
        {
            printf("\t\t\t\t\t\t\tThe register %s currently holds ", 
                   getRegNameFloat(regNum, type));
            Compiler::printTreeID(tree);
            printf("\n");
        }
#endif

        assert((rsGetMaskLock() & regMask) == 0);

#if FEATURE_STACK_FP_X87
        assert((rsGetMaskUsed() & regMask) == 0);
#else
        /* Is the register used by two different values simultaneously? */

        if  (regMask & rsGetMaskUsed())
        {
            /* Save the preceding use information */

            rsRecMultiReg(regNum, type);
        }
#endif
        /* Set the register's bit in the 'used' bitset */

        rsSetMaskUsed( (rsGetMaskUsed() | regMask) );

        // Assign slot
        rsSetUsedTree(regNum, tree);
    }
    else
    {
        // Mark as free

#ifdef DEBUG
        if  (m_rsCompiler->verbose)
        {
            printf("\t\t\t\t\t\t\tThe register %s no longer holds ",
                   getRegNameFloat(regNum, type));
            Compiler::printTreeID(tree);
            printf("\n");
        }
#endif

        assert((rsGetMaskUsed() & regMask) == regMask);

        // Are we freeing a multi-use registers?

        if  (regMask & rsGetMaskMult())
        {
            // Free any multi-use registers
            rsMultRegFree(regMask);
            return;
        }

        rsSetMaskUsed( (rsGetMaskUsed() & ~regMask) );

        // Free slot
        rsFreeUsedTree(regNum, tree);
    }
}

void RegSet::SetLockedRegFloat(GenTree * tree, bool bValue)
{
    regNumber  reg     = tree->gtRegNum;
    var_types  type    = tree->TypeGet();    assert(varTypeIsFloating(type));
    regMaskTP  regMask = genRegMaskFloat(reg, tree->TypeGet());

    if (bValue)
    {
        JITDUMP("locking register %s\n", getRegNameFloat(reg, type));

        assert((rsGetMaskUsed() & regMask) == regMask);
        assert((rsGetMaskLock() & regMask) == 0);

        rsSetMaskLock( (rsGetMaskLock() | regMask) );
    }
    else
    {
        JITDUMP("unlocking register %s\n", getRegNameFloat(reg, type));

        assert((rsGetMaskUsed()   & regMask) == regMask);
        assert((rsGetMaskLock() & regMask) == regMask);

        rsSetMaskLock( (rsGetMaskLock() & ~regMask) );
    }
}

bool RegSet::IsLockedRegFloat(GenTreePtr tree)
{
    /* The value must be sitting in a register */
    assert(tree);
    assert(tree->gtFlags & GTF_REG_VAL);
    assert(varTypeIsFloating(tree->TypeGet()));

    regMaskTP  regMask = genRegMaskFloat(tree->gtRegNum, tree->TypeGet());
    return (rsGetMaskLock() & regMask) == regMask;
}

void CodeGen::UnspillFloat(GenTreePtr tree)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("UnspillFloat() for tree ");
        Compiler::printTreeID(tree);
        printf("\n");
    }
#endif // DEBUG

    RegSet::SpillDsc*  cur = regSet.rsSpillFloat;
    assert(cur);    

    while (cur->spillTree != tree)
        cur = cur->spillNext;

    UnspillFloat(cur);
}

void CodeGen::UnspillFloat(LclVarDsc * varDsc)
{
    JITDUMP("UnspillFloat() for var [%08p]\n", dspPtr(varDsc));

    RegSet::SpillDsc*  cur = regSet.rsSpillFloat;
    assert(cur);    

    while (cur->spillVarDsc != varDsc)
        cur = cur->spillNext;

    UnspillFloat(cur);
}

void CodeGen::RemoveSpillDsc(RegSet::SpillDsc* spillDsc)
{
    RegSet::SpillDsc*  cur;
    RegSet::SpillDsc** prev;

    for (cur = regSet.rsSpillFloat, prev = &regSet.rsSpillFloat;
         cur != spillDsc ;
         prev = &cur->spillNext, cur = cur->spillNext)
        ; // EMPTY LOOP

    assert(cur);

    // Remove node from list
    *prev = cur->spillNext;
}

void CodeGen::UnspillFloat(RegSet::SpillDsc *spillDsc)
{
    JITDUMP("UnspillFloat() for SpillDsc [%08p]\n", dspPtr(spillDsc));

    RemoveSpillDsc(spillDsc);   
    UnspillFloatMachineDep(spillDsc);

    RegSet::SpillDsc::freeDsc(&regSet, spillDsc);
    compiler->tmpRlsTemp(spillDsc->spillTemp);
}

#if FEATURE_STACK_FP_X87

Compiler::fgWalkResult  CodeGen::genRegVarDiesInSubTreeWorker(GenTreePtr * pTree, Compiler::fgWalkData *data)
{
    GenTreePtr tree = *pTree;
    genRegVarDiesInSubTreeData* pData = (genRegVarDiesInSubTreeData*) data->pCallbackData;

    // if it's dying, just rename the register, else load it normally
    if (tree->IsRegVar() && 
        tree->IsRegVarDeath() && 
        tree->gtRegVar.gtRegNum == pData->reg)
    {
        pData->result = true;
        return Compiler::WALK_ABORT;
    }
    
    return Compiler::WALK_CONTINUE;
}


bool CodeGen::genRegVarDiesInSubTree                   (GenTreePtr tree, regNumber reg)
{
    genRegVarDiesInSubTreeData Data;
    Data.reg = reg;
    Data.result = false;

    compiler->fgWalkTreePre(&tree, genRegVarDiesInSubTreeWorker, (void*)&Data);
    
    return Data.result;
}

#endif // FEATURE_STACK_FP_X87

/*****************************************************************************
 *
 *  Force floating point expression results to memory, to get rid of the extra
 *  80 byte "temp-real" precision.
 *  Assumes the tree operand has been computed to the top of the stack.
 *  If type!=TYP_UNDEF, that is the desired presicion, else it is op->gtType
 */

void                CodeGen::genRoundFpExpression(GenTreePtr op,
                                                  var_types type)
{
#if FEATURE_STACK_FP_X87
    return genRoundFpExpressionStackFP(op, type);
#else
    return genRoundFloatExpression(op, type);
#endif
}

void CodeGen::genCodeForTreeFloat(GenTreePtr tree,
                                  regMaskTP  needReg,
                                  regMaskTP  bestReg)
{
    RegSet::RegisterPreference pref(needReg, bestReg);
    genCodeForTreeFloat(tree, &pref);
}

#endif // LEGACY_BACKEND

