// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifdef LEGACY_BACKEND // This file is NOT used for the RyuJIT backend that uses the linear scan register allocator.

#ifdef _TARGET_AMD64_
#error AMD64 must be !LEGACY_BACKEND
#endif

#include "compiler.h"
#include "emit.h"
#include "codegen.h"

// Instruction list
// N=normal, R=reverse, P=pop
#if FEATURE_STACK_FP_X87
const static instruction FPmathNN[] = {INS_fadd, INS_fsub, INS_fmul, INS_fdiv};
const static instruction FPmathNP[] = {INS_faddp, INS_fsubp, INS_fmulp, INS_fdivp};
const static instruction FPmathRN[] = {INS_fadd, INS_fsubr, INS_fmul, INS_fdivr};
const static instruction FPmathRP[] = {INS_faddp, INS_fsubrp, INS_fmulp, INS_fdivrp};

FlatFPStateX87* CodeGenInterface::FlatFPAllocFPState(FlatFPStateX87* pInitFrom)
{
    FlatFPStateX87* pNewState;

    pNewState = new (compiler, CMK_FlatFPStateX87) FlatFPStateX87;
    pNewState->Init(pInitFrom);

    return pNewState;
}

bool CodeGen::FlatFPSameRegisters(FlatFPStateX87* pState, regMaskTP mask)
{
    int i;
    for (i = REG_FPV0; i < REG_FPCOUNT; i++)
    {
        if (pState->Mapped(i))
        {
            regMaskTP regmask = genRegMaskFloat((regNumber)i);
            if ((mask & regmask) == 0)
            {
                return false;
            }

            mask &= ~regmask;
        }
    }

    return mask ? false : true;
}

bool FlatFPStateX87::Mapped(unsigned uEntry)
{
    return m_uVirtualMap[uEntry] != (unsigned)FP_VRNOTMAPPED;
}

void FlatFPStateX87::Unmap(unsigned uEntry)
{
    assert(Mapped(uEntry));
    m_uVirtualMap[uEntry] = (unsigned)FP_VRNOTMAPPED;
}

bool FlatFPStateX87::AreEqual(FlatFPStateX87* pA, FlatFPStateX87* pB)
{
    unsigned i;

    assert(pA->IsConsistent());
    assert(pB->IsConsistent());

    if (pA->m_uStackSize != pB->m_uStackSize)
    {
        return false;
    }

    for (i = 0; i < pA->m_uStackSize; i++)
    {
        if (pA->m_uStack[i] != pB->m_uStack[i])
        {
            return false;
        }
    }

    return true;
}

#ifdef DEBUG
bool FlatFPStateX87::IsValidEntry(unsigned uEntry)
{
    return (Mapped(uEntry) && (m_uVirtualMap[uEntry] >= 0 && m_uVirtualMap[uEntry] < m_uStackSize)) || !Mapped(uEntry);
}

bool FlatFPStateX87::IsConsistent()
{
    unsigned i;

    for (i = 0; i < FP_VIRTUALREGISTERS; i++)
    {
        if (!IsValidEntry(i))
        {
            if (m_bIgnoreConsistencyChecks)
            {
                return true;
            }
            else
            {
                assert(!"Virtual register is marked as mapped but out of the stack range");
                return false;
            }
        }
    }

    for (i = 0; i < m_uStackSize; i++)
    {
        if (m_uVirtualMap[m_uStack[i]] != i)
        {
            if (m_bIgnoreConsistencyChecks)
            {
                return true;
            }
            else
            {
                assert(!"Register File and stack layout don't match!");
                return false;
            }
        }
    }

    return true;
}

void FlatFPStateX87::Dump()
{
    unsigned i;

    assert(IsConsistent());

    if (m_uStackSize > 0)
    {
        printf("Virtual stack state: ");
        for (i = 0; i < m_uStackSize; i++)
        {
            printf("ST(%i): FPV%i | ", StackToST(i), m_uStack[i]);
        }
        printf("\n");
    }
}

void FlatFPStateX87::UpdateMappingFromStack()
{
    memset(m_uVirtualMap, -1, sizeof(m_uVirtualMap));

    unsigned i;

    for (i = 0; i < m_uStackSize; i++)
    {
        m_uVirtualMap[m_uStack[i]] = i;
    }
}

#endif

unsigned FlatFPStateX87::StackToST(unsigned uEntry)
{
    assert(IsValidEntry(uEntry));
    return m_uStackSize - 1 - uEntry;
}

unsigned FlatFPStateX87::VirtualToST(unsigned uEntry)
{
    assert(Mapped(uEntry));

    return StackToST(m_uVirtualMap[uEntry]);
}

unsigned FlatFPStateX87::STToVirtual(unsigned uST)
{
    assert(uST < m_uStackSize);

    return m_uStack[m_uStackSize - 1 - uST];
}

void FlatFPStateX87::Init(FlatFPStateX87* pFrom)
{
    if (pFrom)
    {
        memcpy(this, pFrom, sizeof(*this));
    }
    else
    {
        memset(m_uVirtualMap, -1, sizeof(m_uVirtualMap));

#ifdef DEBUG
        memset(m_uStack, -1, sizeof(m_uStack));
#endif
        m_uStackSize = 0;
    }

#ifdef DEBUG
    m_bIgnoreConsistencyChecks = false;
#endif
}

void FlatFPStateX87::Associate(unsigned uEntry, unsigned uStack)
{
    assert(uStack < m_uStackSize);

    m_uStack[uStack]      = uEntry;
    m_uVirtualMap[uEntry] = uStack;
}

unsigned FlatFPStateX87::TopIndex()
{
    return m_uStackSize - 1;
}

unsigned FlatFPStateX87::TopVirtual()
{
    assert(m_uStackSize > 0);
    return m_uStack[m_uStackSize - 1];
}

void FlatFPStateX87::Rename(unsigned uVirtualTo, unsigned uVirtualFrom)
{
    assert(!Mapped(uVirtualTo));

    unsigned uSlot = m_uVirtualMap[uVirtualFrom];

    Unmap(uVirtualFrom);
    Associate(uVirtualTo, uSlot);
}

void FlatFPStateX87::Push(unsigned uEntry)
{
    assert(m_uStackSize <= FP_PHYSICREGISTERS);
    assert(!Mapped(uEntry));

    m_uStackSize++;
    Associate(uEntry, TopIndex());

    assert(IsConsistent());
}

unsigned FlatFPStateX87::Pop()
{
    assert(m_uStackSize != 0);

    unsigned uVirtual = m_uStack[--m_uStackSize];

#ifdef DEBUG
    m_uStack[m_uStackSize] = (unsigned)-1;
#endif

    Unmap(uVirtual);

    return uVirtual;
}

bool FlatFPStateX87::IsEmpty()
{
    return m_uStackSize == 0;
}

void CodeGen::genCodeForTransitionStackFP(FlatFPStateX87* pSrc, FlatFPStateX87* pDst)
{
    FlatFPStateX87  fpState;
    FlatFPStateX87* pTmp;
    int             i;

    // Make a temp copy
    memcpy(&fpState, pSrc, sizeof(FlatFPStateX87));
    pTmp = &fpState;

    // Make sure everything seems consistent.
    assert(pSrc->m_uStackSize >= pDst->m_uStackSize);
#ifdef DEBUG
    for (i = 0; i < FP_VIRTUALREGISTERS; i++)
    {
        if (!pTmp->Mapped(i) && pDst->Mapped(i))
        {
            assert(!"Dst stack state can't have a virtual register live if Src target has it dead");
        }
    }
#endif

    // First we need to get rid of the stuff that's dead in pDst
    for (i = 0; i < FP_VIRTUALREGISTERS; i++)
    {
        if (pTmp->Mapped(i) && !pDst->Mapped(i))
        {
            // We have to get rid of this one
            JITDUMP("Removing virtual register V%i from stack\n", i);

            // Don't need this virtual register any more
            FlatFPX87_Unload(pTmp, i);
        }
    }

    assert(pTmp->m_uStackSize == pDst->m_uStackSize);

    // Extract cycles
    int iProcessed = 0;

    // We start with the top of the stack so that we can
    // easily recognize the cycle that contains it
    for (i = pTmp->m_uStackSize - 1; i >= 0; i--)
    {
        // Have we processed this stack element yet?
        if (((1 << i) & iProcessed) == 0)
        {
            // Extract cycle
            int iCycle[FP_VIRTUALREGISTERS];
            int iCycleLength = 0;
            int iCurrent     = i;
            int iTOS         = pTmp->m_uStackSize - 1;

            do
            {
                // Mark current stack element as processed
                iProcessed |= (1 << iCurrent);

                // Update cycle
                iCycle[iCycleLength++] = iCurrent;

                // Next element in cycle
                iCurrent = pDst->m_uVirtualMap[pTmp->m_uStack[iCurrent]];

            } while ((iProcessed & (1 << iCurrent)) == 0);

#ifdef DEBUG
            if (verbose)
            {
                printf("Cycle: (");
                for (int l = 0; l < iCycleLength; l++)
                {
                    printf("%i", pTmp->StackToST(iCycle[l]));
                    if (l + 1 < iCycleLength)
                        printf(", ");
                }
                printf(")\n");
            }
#endif

            // Extract cycle
            if (iCycleLength == 1)
            {
                // Stack element in the same place. Nothing to do
            }
            else
            {
                if (iCycle[0] == iTOS)
                {
                    // Cycle includes stack element 0
                    int j;

                    for (j = 1; j < iCycleLength; j++)
                    {
                        FlatFPX87_SwapStack(pTmp, iCycle[j], iTOS);
                    }
                }
                else
                {
                    // Cycle doesn't include stack element 0
                    int j;

                    for (j = 0; j < iCycleLength; j++)
                    {
                        FlatFPX87_SwapStack(pTmp, iCycle[j], iTOS);
                    }

                    FlatFPX87_SwapStack(pTmp, iCycle[0], iTOS);
                }
            }
        }
    }

    assert(FlatFPStateX87::AreEqual(pTmp, pDst));
}

void CodeGen::genCodeForTransitionFromMask(FlatFPStateX87* pSrc, regMaskTP mask, bool bEmitCode)
{
    unsigned i;
    for (i = REG_FPV0; i < REG_FPCOUNT; i++)
    {
        if (pSrc->Mapped(i))
        {
            if ((mask & genRegMaskFloat((regNumber)i)) == 0)
            {
                FlatFPX87_Unload(pSrc, i, bEmitCode);
            }
        }
        else
        {
            assert((mask & genRegMaskFloat((regNumber)i)) == 0 &&
                   "A register marked as incoming live in the target block isnt live in the current block");
        }
    }
}

void CodeGen::genCodeForPrologStackFP()
{
    assert(compiler->compGeneratingProlog);
    assert(compiler->fgFirstBB);

    FlatFPStateX87* pState = compiler->fgFirstBB->bbFPStateX87;

    if (pState && pState->m_uStackSize)
    {
        VARSET_TP VARSET_INIT_NOCOPY(liveEnregIn, VarSetOps::Intersection(compiler, compiler->fgFirstBB->bbLiveIn,
                                                                          compiler->optAllFPregVars));
        unsigned i;

#ifdef DEBUG
        unsigned uLoads = 0;
#endif

        assert(pState->m_uStackSize <= FP_VIRTUALREGISTERS);
        for (i = 0; i < pState->m_uStackSize; i++)
        {
            // Get the virtual register that matches
            unsigned iVirtual = pState->STToVirtual(pState->m_uStackSize - i - 1);

            unsigned   varNum;
            LclVarDsc* varDsc;

            for (varNum = 0, varDsc = compiler->lvaTable; varNum < compiler->lvaCount; varNum++, varDsc++)
            {
                if (varDsc->IsFloatRegType() && varDsc->lvRegister && varDsc->lvRegNum == iVirtual)
                {
                    unsigned varIndex = varDsc->lvVarIndex;

                    // Is this variable live on entry?
                    if (VarSetOps::IsMember(compiler, liveEnregIn, varIndex))
                    {
                        if (varDsc->lvIsParam)
                        {
                            getEmitter()->emitIns_S(INS_fld, EmitSize(varDsc->TypeGet()), varNum, 0);
                        }
                        else
                        {
                            // unitialized regvar
                            getEmitter()->emitIns(INS_fldz);
                        }

#ifdef DEBUG
                        uLoads++;
#endif
                        break;
                    }
                }
            }

            assert(varNum != compiler->lvaCount); // We have to find the matching var!!!!
        }

        assert(uLoads == VarSetOps::Count(compiler, liveEnregIn));
    }
}

void CodeGen::genCodeForEndBlockTransitionStackFP(BasicBlock* block)
{
    switch (block->bbJumpKind)
    {
        case BBJ_EHFINALLYRET:
        case BBJ_EHFILTERRET:
        case BBJ_EHCATCHRET:
            // Nothing to do
            assert(compCurFPState.m_uStackSize == 0);
            break;
        case BBJ_THROW:
            break;
        case BBJ_RETURN:
            // Nothing to do
            assert((varTypeIsFloating(compiler->info.compRetType) && compCurFPState.m_uStackSize == 1) ||
                   compCurFPState.m_uStackSize == 0);
            break;
        case BBJ_COND:
        case BBJ_NONE:
            genCodeForBBTransitionStackFP(block->bbNext);
            break;
        case BBJ_ALWAYS:
            genCodeForBBTransitionStackFP(block->bbJumpDest);
            break;
        case BBJ_LEAVE:
            assert(!"BBJ_LEAVE blocks shouldn't get here");
            break;
        case BBJ_CALLFINALLY:
            assert(compCurFPState.IsEmpty() && "we don't enregister variables live on entry to finallys");
            genCodeForBBTransitionStackFP(block->bbJumpDest);
            break;
        case BBJ_SWITCH:
            // Nothing to do here
            break;
        default:
            noway_assert(!"Unexpected bbJumpKind");
            break;
    }
}

regMaskTP CodeGen::genRegMaskFromLivenessStackFP(VARSET_VALARG_TP varset)
{
    unsigned   varNum;
    LclVarDsc* varDsc;
    regMaskTP  result = 0;

    for (varNum = 0, varDsc = compiler->lvaTable; varNum < compiler->lvaCount; varNum++, varDsc++)
    {
        if (varDsc->IsFloatRegType() && varDsc->lvRegister)
        {

            unsigned varIndex = varDsc->lvVarIndex;

            /* Is this variable live on entry? */

            if (VarSetOps::IsMember(compiler, varset, varIndex))
            {
                // We should only call this function doing a transition
                // To a block which hasn't state yet. All incoming live enregistered variables
                // should have been already initialized.
                assert(varDsc->lvRegNum != REG_FPNONE);

                result |= genRegMaskFloat(varDsc->lvRegNum);
            }
        }
    }

    return result;
}

void CodeGen::genCodeForBBTransitionStackFP(BasicBlock* pDst)
{
    assert(compCurFPState.IsConsistent());
    if (pDst->bbFPStateX87)
    {
        // Target block has an associated state. generate transition
        genCodeForTransitionStackFP(&compCurFPState, pDst->bbFPStateX87);
    }
    else
    {
        // Target block hasn't got an associated state. As it can only possibly
        // have a subset of the current state, we'll take advantage of this and
        // generate the optimal transition

        // Copy current state
        pDst->bbFPStateX87 = FlatFPAllocFPState(&compCurFPState);

        regMaskTP liveRegIn =
            genRegMaskFromLivenessStackFP(VarSetOps::Intersection(compiler, pDst->bbLiveIn, compiler->optAllFPregVars));

        // Match to live vars
        genCodeForTransitionFromMask(pDst->bbFPStateX87, liveRegIn);
    }
}

void CodeGen::SpillTempsStackFP(regMaskTP canSpillMask)
{

    unsigned  i;
    regMaskTP spillMask = 0;
    regNumber reg;

    // First pass we determine which registers we spill
    for (i = 0; i < compCurFPState.m_uStackSize; i++)
    {
        reg               = (regNumber)compCurFPState.m_uStack[i];
        regMaskTP regMask = genRegMaskFloat(reg);
        if ((regMask & canSpillMask) && (regMask & regSet.rsMaskRegVarFloat) == 0)
        {
            spillMask |= regMask;
        }
    }

    // Second pass we do the actual spills
    for (i = REG_FPV0; i < REG_FPCOUNT; i++)
    {
        if ((genRegMaskFloat((regNumber)i) & spillMask))
        {
            JITDUMP("spilling temp in register %s\n", regVarNameStackFP((regNumber)i));
            SpillFloat((regNumber)i, true);
        }
    }
}

// Spills all the fp stack. We need this to spill
// across calls
void CodeGen::SpillForCallStackFP()
{
    unsigned i;
    unsigned uSize = compCurFPState.m_uStackSize;

    for (i = 0; i < uSize; i++)
    {
        SpillFloat((regNumber)compCurFPState.m_uStack[compCurFPState.TopIndex()], true);
    }
}

void CodeGenInterface::SpillFloat(regNumber reg, bool bIsCall)
{
#ifdef DEBUG
    regMaskTP mask = genRegMaskFloat(reg);

    // We can allow spilling regvars, but we don't need it at the moment, and we're
    // missing code in setupopforflatfp, so assert.
    assert(bIsCall || (mask & (regSet.rsMaskLockedFloat | regSet.rsMaskRegVarFloat)) == 0);
#endif

    JITDUMP("SpillFloat spilling register %s\n", regVarNameStackFP(reg));

    // We take the virtual register to the top of the stack
    FlatFPX87_MoveToTOS(&compCurFPState, reg);

    // Allocate spill structure
    RegSet::SpillDsc* spill = RegSet::SpillDsc::alloc(compiler, &regSet, TYP_FLOAT);

    // Fill out spill structure
    var_types type;
    if (regSet.genUsedRegsFloat[reg])
    {
        JITDUMP("will spill tree [%08p]\n", dspPtr(regSet.genUsedRegsFloat[reg]));
        // register used for temp stack
        spill->spillTree             = regSet.genUsedRegsFloat[reg];
        spill->bEnregisteredVariable = false;

        regSet.genUsedRegsFloat[reg]->gtFlags |= GTF_SPILLED;

        type = genActualType(regSet.genUsedRegsFloat[reg]->TypeGet());

        // Clear used flag
        regSet.SetUsedRegFloat(regSet.genUsedRegsFloat[reg], false);
    }
    else
    {
        JITDUMP("will spill varDsc [%08p]\n", dspPtr(regSet.genRegVarsFloat[reg]));

        // enregistered variable
        spill->spillVarDsc = regSet.genRegVarsFloat[reg];
        assert(spill->spillVarDsc);

        spill->bEnregisteredVariable = true;

        // Mark as spilled
        spill->spillVarDsc->lvSpilled = true;
        type                          = genActualType(regSet.genRegVarsFloat[reg]->TypeGet());

        // Clear register flag
        SetRegVarFloat(reg, type, 0);
    }

    // Add to spill list
    spill->spillNext    = regSet.rsSpillFloat;
    regSet.rsSpillFloat = spill;

    // Obtain space
    TempDsc* temp = spill->spillTemp = compiler->tmpGetTemp(type);
    emitAttr size                    = EmitSize(type);

    getEmitter()->emitIns_S(INS_fstp, size, temp->tdTempNum(), 0);
    compCurFPState.Pop();
}

void CodeGen::UnspillFloatMachineDep(RegSet::SpillDsc* spillDsc, bool useSameReg)
{
    NYI(!"Need not be implemented for x86.");
}

void CodeGen::UnspillFloatMachineDep(RegSet::SpillDsc* spillDsc)
{
    // Do actual unspill
    if (spillDsc->bEnregisteredVariable)
    {
        assert(spillDsc->spillVarDsc->lvSpilled);

        // Do the logic as it was a regvar birth
        genRegVarBirthStackFP(spillDsc->spillVarDsc);

        // Mark as not spilled any more
        spillDsc->spillVarDsc->lvSpilled = false;

        // Update stack layout.
        compCurFPState.Push(spillDsc->spillVarDsc->lvRegNum);
    }
    else
    {
        assert(spillDsc->spillTree->gtFlags & GTF_SPILLED);

        spillDsc->spillTree->gtFlags &= ~GTF_SPILLED;

        regNumber reg = regSet.PickRegFloat();
        genMarkTreeInReg(spillDsc->spillTree, reg);
        regSet.SetUsedRegFloat(spillDsc->spillTree, true);

        compCurFPState.Push(reg);
    }

    // load from spilled spot
    emitAttr size = EmitSize(spillDsc->spillTemp->tdTempType());
    getEmitter()->emitIns_S(INS_fld, size, spillDsc->spillTemp->tdTempNum(), 0);
}

// unspills any reg var that we have in the spill list. We need this
// because we can't have any spilled vars across basic blocks
void CodeGen::UnspillRegVarsStackFp()
{
    RegSet::SpillDsc* cur;
    RegSet::SpillDsc* next;

    for (cur = regSet.rsSpillFloat; cur; cur = next)
    {
        next = cur->spillNext;

        if (cur->bEnregisteredVariable)
        {
            UnspillFloat(cur);
        }
    }
}

#ifdef DEBUG
const char* regNamesFP[] = {
#define REGDEF(name, rnum, mask, sname) sname,
#include "registerfp.h"
};

// static
const char* CodeGenInterface::regVarNameStackFP(regNumber reg)
{
    return regNamesFP[reg];
}

bool CodeGen::ConsistentAfterStatementStackFP()
{
    if (!compCurFPState.IsConsistent())
    {
        return false;
    }

    if (regSet.rsMaskUsedFloat != 0)
    {
        assert(!"FP register marked as used after statement");
        return false;
    }
    if (regSet.rsMaskLockedFloat != 0)
    {
        assert(!"FP register marked as locked after statement");
        return false;
    }
    if (genCountBits(regSet.rsMaskRegVarFloat) > compCurFPState.m_uStackSize)
    {
        assert(!"number of FP regvars in regSet.rsMaskRegVarFloat doesnt match current FP state");
        return false;
    }

    return true;
}

#endif

int CodeGen::genNumberTemps()
{
    return compCurFPState.m_uStackSize - genCountBits(regSet.rsMaskRegVarFloat);
}

void CodeGen::genDiscardStackFP(GenTreePtr tree)
{
    assert(tree->InReg());
    assert(varTypeIsFloating(tree));

    FlatFPX87_Unload(&compCurFPState, tree->gtRegNum, true);
}

void CodeGen::genRegRenameWithMasks(regNumber dstReg, regNumber srcReg)
{
    regMaskTP dstregmask = genRegMaskFloat(dstReg);
    regMaskTP srcregmask = genRegMaskFloat(srcReg);

    // rename use register
    compCurFPState.Rename(dstReg, srcReg);

    regSet.rsMaskUsedFloat &= ~srcregmask;
    regSet.rsMaskUsedFloat |= dstregmask;

    if (srcregmask & regSet.rsMaskLockedFloat)
    {
        assert((dstregmask & regSet.rsMaskLockedFloat) == 0);
        // We will set the new one as locked
        regSet.rsMaskLockedFloat &= ~srcregmask;
        regSet.rsMaskLockedFloat |= dstregmask;
    }

    // Updated used tree
    assert(!regSet.genUsedRegsFloat[dstReg]);
    regSet.genUsedRegsFloat[dstReg]           = regSet.genUsedRegsFloat[srcReg];
    regSet.genUsedRegsFloat[dstReg]->gtRegNum = dstReg;
    regSet.genUsedRegsFloat[srcReg]           = NULL;
}

void CodeGen::genRegVarBirthStackFP(LclVarDsc* varDsc)
{
    // Mark the virtual register we're assigning to this local;
    regNumber reg = varDsc->lvRegNum;

#ifdef DEBUG
    regMaskTP regmask = genRegMaskFloat(reg);
#endif

    assert(varDsc->lvTracked && varDsc->lvRegister && reg != REG_FPNONE);
    if (regSet.genUsedRegsFloat[reg])
    {

        // Register was marked as used... will have to rename it so we can put the
        // regvar where it belongs.
        JITDUMP("Renaming used register %s\n", regVarNameStackFP(reg));

        regNumber newreg;

        newreg = regSet.PickRegFloat();

#ifdef DEBUG
        regMaskTP newregmask = genRegMaskFloat(newreg);
#endif

        // Update used mask
        assert((regSet.rsMaskUsedFloat & regmask) && (regSet.rsMaskUsedFloat & newregmask) == 0);

        genRegRenameWithMasks(newreg, reg);
    }

    // Mark the reg as holding a regvar
    varDsc->lvSpilled = false;
    SetRegVarFloat(reg, varDsc->TypeGet(), varDsc);
}

void CodeGen::genRegVarBirthStackFP(GenTreePtr tree)
{
#ifdef DEBUG
    if (compiler->verbose)
    {
        printf("variable V%i is going live in ", tree->gtLclVarCommon.gtLclNum);
        Compiler::printTreeID(tree);
        printf("\n");
    }
#endif // DEBUG

    // Update register in local var
    LclVarDsc* varDsc = compiler->lvaTable + tree->gtLclVarCommon.gtLclNum;

    genRegVarBirthStackFP(varDsc);
    assert(tree->gtRegNum == tree->gtRegVar.gtRegNum && tree->gtRegNum == varDsc->lvRegNum);
}

void CodeGen::genRegVarDeathStackFP(LclVarDsc* varDsc)
{
    regNumber reg = varDsc->lvRegNum;

    assert(varDsc->lvTracked && varDsc->lvRegister && reg != REG_FPNONE);
    SetRegVarFloat(reg, varDsc->TypeGet(), 0);
}

void CodeGen::genRegVarDeathStackFP(GenTreePtr tree)
{
#ifdef DEBUG
    if (compiler->verbose)
    {
        printf("register %s is going dead in ", regVarNameStackFP(tree->gtRegVar.gtRegNum));
        Compiler::printTreeID(tree);
        printf("\n");
    }
#endif // DEBUG

    LclVarDsc* varDsc = compiler->lvaTable + tree->gtLclVarCommon.gtLclNum;
    genRegVarDeathStackFP(varDsc);
}

void CodeGen::genLoadStackFP(GenTreePtr tree, regNumber reg)
{
#ifdef DEBUG
    if (compiler->verbose)
    {
        printf("genLoadStackFP");
        Compiler::printTreeID(tree);
        printf(" %s\n", regVarNameStackFP(reg));
    }
#endif // DEBUG

    if (tree->IsRegVar())
    {
        // if it has been spilled, unspill it.%
        LclVarDsc* varDsc = &compiler->lvaTable[tree->gtLclVarCommon.gtLclNum];
        if (varDsc->lvSpilled)
        {
            UnspillFloat(varDsc);
        }

        // if it's dying, just rename the register, else load it normally
        if (tree->IsRegVarDeath())
        {
            genRegVarDeathStackFP(tree);
            compCurFPState.Rename(reg, tree->gtRegVar.gtRegNum);
        }
        else
        {
            assert(tree->gtRegNum == tree->gtRegVar.gtRegNum);
            inst_FN(INS_fld, compCurFPState.VirtualToST(tree->gtRegVar.gtRegNum));
            FlatFPX87_PushVirtual(&compCurFPState, reg);
        }
    }
    else
    {
        FlatFPX87_PushVirtual(&compCurFPState, reg);
        inst_FS_TT(INS_fld, tree);
    }
}

void CodeGen::genMovStackFP(GenTreePtr dst, regNumber dstreg, GenTreePtr src, regNumber srcreg)
{
    if (dstreg == REG_FPNONE && !dst->IsRegVar())
    {
        regNumber reg;

        // reg to mem path
        if (srcreg == REG_FPNONE)
        {
            assert(src->IsRegVar());
            reg = src->gtRegNum;
        }
        else
        {
            reg = srcreg;
        }

        // Mov src to top of the stack
        FlatFPX87_MoveToTOS(&compCurFPState, reg);

        if (srcreg != REG_FPNONE || (src->IsRegVar() && src->IsRegVarDeath()))
        {
            // Emit instruction
            inst_FS_TT(INS_fstp, dst);

            // Update stack
            compCurFPState.Pop();
        }
        else
        {
            inst_FS_TT(INS_fst, dst);
        }
    }
    else
    {
        if (dstreg == REG_FPNONE)
        {
            assert(dst->IsRegVar());
            dstreg = dst->gtRegNum;
        }

        if (srcreg == REG_FPNONE && !src->IsRegVar())
        {
            // mem to reg
            assert(dst->IsRegVar() && dst->IsRegVarBirth());

            FlatFPX87_PushVirtual(&compCurFPState, dstreg);
            FlatFPX87_MoveToTOS(&compCurFPState, dstreg);

            if (src->gtOper == GT_CNS_DBL)
            {
                genConstantLoadStackFP(src);
            }
            else
            {
                inst_FS_TT(INS_fld, src);
            }
        }
        else
        {
            // disposable reg to reg, use renaming
            assert(dst->IsRegVar() && dst->IsRegVarBirth());
            assert(src->IsRegVar() || (src->InReg()));
            assert(src->gtRegNum != REG_FPNONE);

            if ((src->InReg()) || (src->IsRegVar() && src->IsRegVarDeath()))
            {
                // src is disposable and dst is a regvar, so we'll rename src to dst

                // SetupOp should have masked out the regvar
                assert(!src->IsRegVar() || !src->IsRegVarDeath() ||
                       !(genRegMaskFloat(src->gtRegVar.gtRegNum) & regSet.rsMaskRegVarFloat));

                // get slot that holds the value
                unsigned uStack = compCurFPState.m_uVirtualMap[src->gtRegNum];

                // unlink the slot that holds the value
                compCurFPState.Unmap(src->gtRegNum);

                regNumber tgtreg = dst->gtRegVar.gtRegNum;

                compCurFPState.IgnoreConsistencyChecks(true);

                if (regSet.genUsedRegsFloat[tgtreg])
                {
                    // tgtreg is used, we move it to src reg. We do this here as src reg won't be
                    // marked as used, if tgtreg is used it srcreg will be a candidate for moving
                    // which is something we don't want, so we do the renaming here.
                    genRegRenameWithMasks(src->gtRegNum, tgtreg);
                }

                compCurFPState.IgnoreConsistencyChecks(false);

                // Birth of FP var
                genRegVarBirthStackFP(dst);

                // Associate target reg with source physical register
                compCurFPState.Associate(tgtreg, uStack);
            }
            else
            {
                if (src->IsRegVar())
                {
                    // regvar that isnt dying to regvar
                    assert(!src->IsRegVarDeath());

                    // Birth of FP var
                    genRegVarBirthStackFP(dst);

                    // Load register
                    inst_FN(INS_fld, compCurFPState.VirtualToST(src->gtRegVar.gtRegNum));

                    // update our logic stack
                    FlatFPX87_PushVirtual(&compCurFPState, dst->gtRegVar.gtRegNum);
                }
                else
                {
                    // memory to regvar

                    // Birth of FP var
                    genRegVarBirthStackFP(dst);

                    // load into stack
                    inst_FS_TT(INS_fld, src);

                    // update our logic stack
                    FlatFPX87_PushVirtual(&compCurFPState, dst->gtRegVar.gtRegNum);
                }
            }
        }
    }
}

void CodeGen::genCodeForTreeStackFP_DONE(GenTreePtr tree, regNumber reg)
{
    return genCodeForTree_DONE(tree, reg);
}

// Does the setup of the FP stack on entry to block
void CodeGen::genSetupStateStackFP(BasicBlock* block)
{
    bool bGenerate = !block->bbFPStateX87;
    if (bGenerate)
    {
        // Allocate FP state
        block->bbFPStateX87 = FlatFPAllocFPState();
        block->bbFPStateX87->Init();
    }

    // Update liveset and lock enregistered live vars on entry
    VARSET_TP VARSET_INIT_NOCOPY(liveSet,
                                 VarSetOps::Intersection(compiler, block->bbLiveIn, compiler->optAllFPregVars));

    if (!VarSetOps::IsEmpty(compiler, liveSet))
    {
        unsigned   varNum;
        LclVarDsc* varDsc;

        for (varNum = 0, varDsc = compiler->lvaTable; varNum < compiler->lvaCount; varNum++, varDsc++)
        {
            if (varDsc->IsFloatRegType() && varDsc->lvRegister)
            {

                unsigned varIndex = varDsc->lvVarIndex;

                // Is this variable live on entry?
                if (VarSetOps::IsMember(compiler, liveSet, varIndex))
                {
                    JITDUMP("genSetupStateStackFP(): enregistered variable V%i is live on entry to block\n", varNum);

                    assert(varDsc->lvTracked);
                    assert(varDsc->lvRegNum != REG_FPNONE);

                    genRegVarBirthStackFP(varDsc);

                    if (bGenerate)
                    {
                        // If we're generating layout, update it.
                        block->bbFPStateX87->Push(varDsc->lvRegNum);
                    }
                }
            }
        }
    }

    compCurFPState.Init(block->bbFPStateX87);

    assert(block->bbFPStateX87->IsConsistent());
}

regMaskTP CodeGen::genPushArgumentStackFP(GenTreePtr args)
{
    regMaskTP addrReg = 0;
    unsigned  opsz    = genTypeSize(genActualType(args->TypeGet()));

    switch (args->gtOper)
    {
        GenTreePtr temp;
        GenTreePtr fval;
        size_t     flopsz;

        case GT_CNS_DBL:
        {
            float f    = 0.0;
            int*  addr = NULL;
            if (args->TypeGet() == TYP_FLOAT)
            {
                f = (float)args->gtDblCon.gtDconVal;
                // *(long*) (&f) used instead of *addr because of of strict
                // pointer aliasing optimization. According to the ISO C/C++
                // standard, an optimizer can assume two pointers of
                // non-compatible types do not point to the same memory.
                inst_IV(INS_push, *((int*)(&f)));
                genSinglePush();
                addrReg = 0;
            }
            else
            {
                addr = (int*)&args->gtDblCon.gtDconVal;

                // store forwarding fix for pentium 4 and Centrino
                // (even for down level CPUs as we don't care about their perf any more)
                fval = genMakeConst(&args->gtDblCon.gtDconVal, args->gtType, args, true);
                inst_FS_TT(INS_fld, fval);
                flopsz = (size_t)8;
                inst_RV_IV(INS_sub, REG_ESP, flopsz, EA_PTRSIZE);
                getEmitter()->emitIns_AR_R(INS_fstp, EA_ATTR(flopsz), REG_NA, REG_ESP, 0);
                genSinglePush();
                genSinglePush();

                addrReg = 0;
            }

            break;
        }

        case GT_CAST:
        {
            // Is the value a cast from double ?
            if ((args->gtOper == GT_CAST) && (args->CastFromType() == TYP_DOUBLE))
            {
                /* Load the value onto the FP stack */

                genCodeForTreeFlt(args->gtCast.CastOp(), false);

                /* Go push the value as a float/double */
                args = args->gtCast.CastOp();

                addrReg = 0;
                goto PUSH_FLT;
            }
            // Fall through to default case....
        }
        default:
        {
            temp = genMakeAddrOrFPstk(args, &addrReg, false);
            if (temp)
            {
                unsigned offs;

                // We have the address of the float operand, push its bytes
                offs = opsz;
                assert(offs % sizeof(int) == 0);

                if (offs == 4)
                {
                    assert(args->gtType == temp->gtType);
                    do
                    {
                        offs -= sizeof(int);
                        inst_TT(INS_push, temp, offs);
                        genSinglePush();
                    } while (offs);
                }
                else
                {
                    // store forwarding fix for pentium 4 and Centrino
                    inst_FS_TT(INS_fld, temp);
                    flopsz = (size_t)offs;
                    inst_RV_IV(INS_sub, REG_ESP, (size_t)flopsz, EA_PTRSIZE);
                    getEmitter()->emitIns_AR_R(INS_fstp, EA_ATTR(flopsz), REG_NA, REG_ESP, 0);
                    genSinglePush();
                    genSinglePush();
                }
            }
            else
            {
            // The argument is on the FP stack -- pop it into [ESP-4/8]

            PUSH_FLT:

                inst_RV_IV(INS_sub, REG_ESP, opsz, EA_PTRSIZE);

                genSinglePush();
                if (opsz == 2 * sizeof(unsigned))
                    genSinglePush();

                // Take reg to top of stack
                FlatFPX87_MoveToTOS(&compCurFPState, args->gtRegNum);

                // Pop it off to stack
                compCurFPState.Pop();
                getEmitter()->emitIns_AR_R(INS_fstp, EA_ATTR(opsz), REG_NA, REG_ESP, 0);
            }

            gcInfo.gcMarkRegSetNpt(addrReg);
            break;
        }
    }

    return addrReg;
}

void CodeGen::genRoundFpExpressionStackFP(GenTreePtr op, var_types type)
{
    // Do nothing with memory resident opcodes - these are the right precision
    // (even if genMakeAddrOrFPstk loads them to the FP stack)
    if (type == TYP_UNDEF)
        type = op->TypeGet();

    switch (op->gtOper)
    {
        case GT_LCL_VAR:
        case GT_LCL_FLD:
        case GT_CLS_VAR:
        case GT_CNS_DBL:
        case GT_IND:
        case GT_LEA:
            if (type == op->TypeGet())
                return;
        default:
            break;
    }

    assert(op->gtRegNum != REG_FPNONE);

    // Take register to top of stack
    FlatFPX87_MoveToTOS(&compCurFPState, op->gtRegNum);

    // Allocate a temp for the expression
    TempDsc* temp = compiler->tmpGetTemp(type);

    // Store the FP value into the temp
    inst_FS_ST(INS_fstp, EmitSize(type), temp, 0);

    // Load the value back onto the FP stack
    inst_FS_ST(INS_fld, EmitSize(type), temp, 0);

    // We no longer need the temp
    compiler->tmpRlsTemp(temp);
}

void CodeGen::genCodeForTreeStackFP_Const(GenTreePtr tree)
{
#ifdef DEBUG
    if (compiler->verbose)
    {
        printf("genCodeForTreeStackFP_Const() ");
        Compiler::printTreeID(tree);
        printf("\n");
    }
#endif // DEBUG

#ifdef DEBUG
    if (tree->OperGet() != GT_CNS_DBL)
    {
        compiler->gtDispTree(tree);
        assert(!"bogus float const");
    }
#endif
    // Pick register
    regNumber reg = regSet.PickRegFloat();

    // Load constant
    genConstantLoadStackFP(tree);

    // Push register to virtual stack
    FlatFPX87_PushVirtual(&compCurFPState, reg);

    // Update tree
    genCodeForTreeStackFP_DONE(tree, reg);
}

void CodeGen::genCodeForTreeStackFP_Leaf(GenTreePtr tree)
{
#ifdef DEBUG
    if (compiler->verbose)
    {
        printf("genCodeForTreeStackFP_Leaf() ");
        Compiler::printTreeID(tree);
        printf("\n");
    }
#endif // DEBUG

    switch (tree->OperGet())
    {
        case GT_LCL_VAR:
        case GT_LCL_FLD:
        {
            assert(!compiler->lvaTable[tree->gtLclVarCommon.gtLclNum].lvRegister);

            // Pick register
            regNumber reg = regSet.PickRegFloat();

            // Load it
            genLoadStackFP(tree, reg);

            genCodeForTreeStackFP_DONE(tree, reg);

            break;
        }

        case GT_REG_VAR:
        {
            regNumber reg = regSet.PickRegFloat();

            genLoadStackFP(tree, reg);

            genCodeForTreeStackFP_DONE(tree, reg);

            break;
        }

        case GT_CLS_VAR:
        {
            // Pick register
            regNumber reg = regSet.PickRegFloat();

            // Load it
            genLoadStackFP(tree, reg);

            genCodeForTreeStackFP_DONE(tree, reg);

            break;
        }

        default:
#ifdef DEBUG
            compiler->gtDispTree(tree);
#endif
            assert(!"unexpected leaf");
    }

    genUpdateLife(tree);
}

void CodeGen::genCodeForTreeStackFP_Asg(GenTreePtr tree)
{
#ifdef DEBUG
    if (compiler->verbose)
    {
        printf("genCodeForTreeStackFP_Asg() ");
        Compiler::printTreeID(tree);
        printf("\n");
    }
#endif // DEBUG

    emitAttr   size;
    unsigned   offs;
    GenTreePtr op1 = tree->gtOp.gtOp1;
    GenTreePtr op2 = tree->gtGetOp2();

    assert(tree->OperGet() == GT_ASG);

    if (!op1->IsRegVar() && (op2->gtOper == GT_CAST) && (op1->gtType == op2->gtType) &&
        varTypeIsFloating(op2->gtCast.CastOp()))
    {
        /* We can discard the cast */
        op2 = op2->gtCast.CastOp();
    }

    size = EmitSize(op1);
    offs = 0;

    // If lhs is a comma expression, evaluate the non-last parts, make op1 be the remainder.
    // (But can't do this if the assignment is reversed...)
    if ((tree->gtFlags & GTF_REVERSE_OPS) == 0)
    {
        op1 = genCodeForCommaTree(op1);
    }

    GenTreePtr op1NonCom = op1->gtEffectiveVal();
    if (op1NonCom->gtOper == GT_LCL_VAR)
    {
#ifdef DEBUG
        LclVarDsc* varDsc = &compiler->lvaTable[op1NonCom->gtLclVarCommon.gtLclNum];
        // No dead stores
        assert(!varDsc->lvTracked || compiler->opts.MinOpts() || !(op1NonCom->gtFlags & GTF_VAR_DEATH));
#endif

#ifdef DEBUGGING_SUPPORT

        /* For non-debuggable code, every definition of a lcl-var has
         * to be checked to see if we need to open a new scope for it.
         */

        if (compiler->opts.compScopeInfo && !compiler->opts.compDbgCode && (compiler->info.compVarScopesCount > 0))
        {
            siCheckVarScope(op1NonCom->gtLclVarCommon.gtLclNum, op1NonCom->gtLclVar.gtLclILoffs);
        }
#endif
    }

    assert(op2);
    switch (op2->gtOper)
    {
        case GT_CNS_DBL:

            assert(compCurFPState.m_uStackSize <= FP_PHYSICREGISTERS);

            regMaskTP addrRegInt;
            addrRegInt = 0;
            regMaskTP addrRegFlt;
            addrRegFlt = 0;

            // op2 is already "evaluated," so doesn't matter if they're reversed or not...
            op1 = genCodeForCommaTree(op1);
            op1 = genMakeAddressableStackFP(op1, &addrRegInt, &addrRegFlt);

            // We want to 'cast' the constant to the op1'a type
            double constantValue;
            constantValue = op2->gtDblCon.gtDconVal;
            if (op1->gtType == TYP_FLOAT)
            {
                float temp    = forceCastToFloat(constantValue);
                constantValue = (double)temp;
            }

            GenTreePtr constantTree;
            constantTree = compiler->gtNewDconNode(constantValue);
            if (genConstantLoadStackFP(constantTree, true))
            {
                if (op1->IsRegVar())
                {
                    // regvar birth
                    genRegVarBirthStackFP(op1);

                    // Update
                    compCurFPState.Push(op1->gtRegNum);
                }
                else
                {
                    // store in target
                    inst_FS_TT(INS_fstp, op1);
                }
            }
            else
            {
                // Standard constant
                if (op1->IsRegVar())
                {
                    // Load constant to fp stack.

                    GenTreePtr cnsaddr;

                    // Create slot for constant
                    if (op1->gtType == TYP_FLOAT || StackFPIsSameAsFloat(op2->gtDblCon.gtDconVal))
                    {
                        // We're going to use that double as a float, so recompute addr
                        float f = forceCastToFloat(op2->gtDblCon.gtDconVal);
                        cnsaddr = genMakeConst(&f, TYP_FLOAT, tree, true);
                    }
                    else
                    {
                        cnsaddr = genMakeConst(&op2->gtDblCon.gtDconVal, TYP_DOUBLE, tree, true);
                    }

                    // Load into stack
                    inst_FS_TT(INS_fld, cnsaddr);

                    // regvar birth
                    genRegVarBirthStackFP(op1);

                    // Update
                    compCurFPState.Push(op1->gtRegNum);
                }
                else
                {
                    if (size == 4)
                    {

                        float f    = forceCastToFloat(op2->gtDblCon.gtDconVal);
                        int*  addr = (int*)&f;

                        do
                        {
                            inst_TT_IV(INS_mov, op1, *addr++, offs);
                            offs += sizeof(int);
                        } while (offs < size);
                    }
                    else
                    {
                        // store forwarding fix for pentium 4 and centrino and also
                        // fld for doubles that can be represented as floats, saving
                        // 4 bytes of load
                        GenTreePtr cnsaddr;

                        // Create slot for constant
                        if (op1->gtType == TYP_FLOAT || StackFPIsSameAsFloat(op2->gtDblCon.gtDconVal))
                        {
                            // We're going to use that double as a float, so recompute addr
                            float f = forceCastToFloat(op2->gtDblCon.gtDconVal);
                            cnsaddr = genMakeConst(&f, TYP_FLOAT, tree, true);
                        }
                        else
                        {
                            assert(tree->gtType == TYP_DOUBLE);
                            cnsaddr = genMakeConst(&op2->gtDblCon.gtDconVal, TYP_DOUBLE, tree, true);
                        }

                        inst_FS_TT(INS_fld, cnsaddr);
                        inst_FS_TT(INS_fstp, op1);
                    }
                }
            }

            genDoneAddressableStackFP(op1, addrRegInt, addrRegFlt, RegSet::KEEP_REG);
            genUpdateLife(op1);
            return;

        default:
            break;
    }

    // Not one of the easy optimizations. Proceed normally
    if (tree->gtFlags & GTF_REVERSE_OPS)
    {
        /* Evaluate the RHS onto the FP stack.
           We don't need to round it as we will be doing a spill for
           the assignment anyway (unless op1 is a GT_REG_VAR). */

        genSetupForOpStackFP(op1, op2, true, true, false, true);

        // Do the move
        genMovStackFP(op1, REG_FPNONE, op2, (op2->InReg()) ? op2->gtRegNum : REG_FPNONE);
    }
    else
    {
        // Have to evaluate left side before

        // This should never happen
        assert(!op1->IsRegVar());

        genSetupForOpStackFP(op1, op2, false, true, false, true);

        // Do the actual move
        genMovStackFP(op1, REG_FPNONE, op2, (op2->InReg()) ? op2->gtRegNum : REG_FPNONE);
    }
}

void CodeGen::genSetupForOpStackFP(
    GenTreePtr& op1, GenTreePtr& op2, bool bReverse, bool bMakeOp1Addressable, bool bOp1ReadOnly, bool bOp2ReadOnly)
{
    if (bMakeOp1Addressable)
    {
        if (bReverse)
        {
            genSetupForOpStackFP(op2, op1, false, false, bOp2ReadOnly, bOp1ReadOnly);
        }
        else
        {
            regMaskTP addrRegInt = 0;
            regMaskTP addrRegFlt = 0;

            op1 = genCodeForCommaTree(op1);

            // Evaluate RHS on FP stack
            if (bOp2ReadOnly && op2->IsRegVar() && !op2->IsRegVarDeath())
            {
                // read only and not dying, so just make addressable
                op1 = genMakeAddressableStackFP(op1, &addrRegInt, &addrRegFlt);
                genKeepAddressableStackFP(op1, &addrRegInt, &addrRegFlt);
                genUpdateLife(op2);
            }
            else
            {
                // Make target addressable
                op1 = genMakeAddressableStackFP(op1, &addrRegInt, &addrRegFlt);

                op2 = genCodeForCommaTree(op2);

                genCodeForTreeFloat(op2);

                regSet.SetUsedRegFloat(op2, true);
                regSet.SetLockedRegFloat(op2, true);

                // Make sure target is still adressable
                genKeepAddressableStackFP(op1, &addrRegInt, &addrRegFlt);

                regSet.SetLockedRegFloat(op2, false);
                regSet.SetUsedRegFloat(op2, false);
            }

            /* Free up anything that was tied up by the target address */
            genDoneAddressableStackFP(op1, addrRegInt, addrRegFlt, RegSet::KEEP_REG);
        }
    }
    else
    {
        assert(!bReverse ||
               !"Can't do this. if op2 is a reg var and dies in op1, we have a serious problem. For the "
                "moment, handle this in the caller");

        regMaskTP addrRegInt = 0;
        regMaskTP addrRegFlt = 0;

        op1 = genCodeForCommaTree(op1);

        if (bOp1ReadOnly && op1->IsRegVar() && !op1->IsRegVarDeath() &&
            !genRegVarDiesInSubTree(op2, op1->gtRegVar.gtRegNum)) // regvar can't die in op2 either
        {
            // First update liveness for op1, since we're "evaluating" it here
            genUpdateLife(op1);

            op2 = genCodeForCommaTree(op2);

            // read only and not dying, we dont have to do anything.
            op2 = genMakeAddressableStackFP(op2, &addrRegInt, &addrRegFlt);
            genKeepAddressableStackFP(op2, &addrRegInt, &addrRegFlt);
        }
        else
        {
            genCodeForTreeFloat(op1);

            regSet.SetUsedRegFloat(op1, true);

            op2 = genCodeForCommaTree(op2);

            op2 = genMakeAddressableStackFP(op2, &addrRegInt, &addrRegFlt);

            // Restore op1 if necessary
            if (op1->gtFlags & GTF_SPILLED)
            {
                UnspillFloat(op1);
            }

            // Lock op1
            regSet.SetLockedRegFloat(op1, true);

            genKeepAddressableStackFP(op2, &addrRegInt, &addrRegFlt);

            // unlock op1
            regSet.SetLockedRegFloat(op1, false);

            // mark as free
            regSet.SetUsedRegFloat(op1, false);
        }

        genDoneAddressableStackFP(op2, addrRegInt, addrRegFlt, RegSet::KEEP_REG);
    }
}

void CodeGen::genCodeForTreeStackFP_Arithm(GenTreePtr tree)
{
#ifdef DEBUG
    if (compiler->verbose)
    {
        printf("genCodeForTreeStackFP_Arithm() ");
        Compiler::printTreeID(tree);
        printf("\n");
    }
#endif // DEBUG

    assert(tree->OperGet() == GT_ADD || tree->OperGet() == GT_SUB || tree->OperGet() == GT_MUL ||
           tree->OperGet() == GT_DIV);

    // We handle the reverse here instead of leaving setupop to do it. As for this case
    //
    //              + with reverse
    //          op1    regvar
    //
    // and in regvar dies in op1, we would need a load of regvar, instead of a noop. So we handle this
    // here and tell genArithmStackFP to do the reverse operation
    bool bReverse;

    GenTreePtr op1, op2;

    if (tree->gtFlags & GTF_REVERSE_OPS)
    {
        bReverse = true;
        op1      = tree->gtGetOp2();
        op2      = tree->gtOp.gtOp1;
    }
    else
    {
        bReverse = false;
        op1      = tree->gtOp.gtOp1;
        op2      = tree->gtGetOp2();
    }

    regNumber result;

    // Fast paths
    genTreeOps oper = tree->OperGet();
    if (op1->IsRegVar() && op2->IsRegVar() && !op1->IsRegVarDeath() && op2->IsRegVarDeath())
    {
        // In this fastpath, we will save a load by doing the operation directly on the op2
        // register, as it's dying.

        // Mark op2 as dead
        genRegVarDeathStackFP(op2);

        // Do operation
        result = genArithmStackFP(oper, op2, op2->gtRegVar.gtRegNum, op1, REG_FPNONE, !bReverse);

        genUpdateLife(op1);
        genUpdateLife(op2);
    }
    else if (!op1->IsRegVar() &&                         // We don't do this for regvars, as we'll need a scratch reg
             ((tree->gtFlags & GTF_SIDE_EFFECT) == 0) && // No side effects
             GenTree::Compare(op1, op2))                 // op1 and op2 are the same
    {
        // op1 is same thing as op2. Ideal for CSEs that werent optimized
        // due to their low cost.

        // First we need to update lifetimes from op1
        VarSetOps::AssignNoCopy(compiler, compiler->compCurLife, genUpdateLiveSetForward(op1));
        compiler->compCurLifeTree = op1;

        genCodeForTreeFloat(op2);

        result = genArithmStackFP(oper, op2, op2->gtRegNum, op2, op2->gtRegNum, bReverse);
    }
    else
    {
        genSetupForOpStackFP(op1, op2, false, false, false, true);

        result = genArithmStackFP(oper, op1, (op1->InReg()) ? op1->gtRegNum : REG_FPNONE, op2,
                                  (op2->InReg()) ? op2->gtRegNum : REG_FPNONE, bReverse);
    }

    genCodeForTreeStackFP_DONE(tree, result);
}

regNumber CodeGen::genArithmStackFP(
    genTreeOps oper, GenTreePtr dst, regNumber dstreg, GenTreePtr src, regNumber srcreg, bool bReverse)
{
#ifdef DEBUG
    if (compiler->verbose)
    {
        printf("genArithmStackFP() dst: ");
        Compiler::printTreeID(dst);
        printf(" src: ");
        Compiler::printTreeID(src);
        printf(" dstreg: %s srcreg: %s\n", dstreg == REG_FPNONE ? "NONE" : regVarNameStackFP(dstreg),
               srcreg == REG_FPNONE ? "NONE" : regVarNameStackFP(srcreg));
    }
#endif // DEBUG

    // Select instruction depending on oper and bReverseOp

    instruction ins_NN;
    instruction ins_RN;
    instruction ins_RP;
    instruction ins_NP;

    switch (oper)
    {
        default:
            assert(!"Unexpected oper");
        case GT_ADD:
        case GT_SUB:
        case GT_MUL:
        case GT_DIV:

            /* Make sure the instruction tables look correctly ordered */
            assert(FPmathNN[GT_ADD - GT_ADD] == INS_fadd);
            assert(FPmathNN[GT_SUB - GT_ADD] == INS_fsub);
            assert(FPmathNN[GT_MUL - GT_ADD] == INS_fmul);
            assert(FPmathNN[GT_DIV - GT_ADD] == INS_fdiv);

            assert(FPmathNP[GT_ADD - GT_ADD] == INS_faddp);
            assert(FPmathNP[GT_SUB - GT_ADD] == INS_fsubp);
            assert(FPmathNP[GT_MUL - GT_ADD] == INS_fmulp);
            assert(FPmathNP[GT_DIV - GT_ADD] == INS_fdivp);

            assert(FPmathRN[GT_ADD - GT_ADD] == INS_fadd);
            assert(FPmathRN[GT_SUB - GT_ADD] == INS_fsubr);
            assert(FPmathRN[GT_MUL - GT_ADD] == INS_fmul);
            assert(FPmathRN[GT_DIV - GT_ADD] == INS_fdivr);

            assert(FPmathRP[GT_ADD - GT_ADD] == INS_faddp);
            assert(FPmathRP[GT_SUB - GT_ADD] == INS_fsubrp);
            assert(FPmathRP[GT_MUL - GT_ADD] == INS_fmulp);
            assert(FPmathRP[GT_DIV - GT_ADD] == INS_fdivrp);

            if (bReverse)
            {
                ins_NN = FPmathRN[oper - GT_ADD];
                ins_NP = FPmathRP[oper - GT_ADD];
                ins_RN = FPmathNN[oper - GT_ADD];
                ins_RP = FPmathNP[oper - GT_ADD];
            }
            else
            {
                ins_NN = FPmathNN[oper - GT_ADD];
                ins_NP = FPmathNP[oper - GT_ADD];
                ins_RN = FPmathRN[oper - GT_ADD];
                ins_RP = FPmathRP[oper - GT_ADD];
            }
    }

    regNumber result = REG_FPNONE;

    if (dstreg != REG_FPNONE)
    {
        if (srcreg == REG_FPNONE)
        {
            if (src->IsRegVar())
            {
                if (src->IsRegVarDeath())
                {
                    if (compCurFPState.TopVirtual() == (unsigned)dst->gtRegNum)
                    {
                        // Do operation and store in srcreg
                        inst_FS(ins_RP, compCurFPState.VirtualToST(src->gtRegNum));

                        // kill current dst and rename src as dst.
                        FlatFPX87_Kill(&compCurFPState, dstreg);
                        compCurFPState.Rename(dstreg, src->gtRegNum);
                    }
                    else
                    {
                        // Take src to top of stack
                        FlatFPX87_MoveToTOS(&compCurFPState, src->gtRegNum);

                        // do reverse and pop operation
                        inst_FS(ins_NP, compCurFPState.VirtualToST(dstreg));

                        // Kill the register
                        FlatFPX87_Kill(&compCurFPState, src->gtRegNum);
                    }

                    assert(!src->IsRegVar() || !src->IsRegVarDeath() ||
                           !(genRegMaskFloat(src->gtRegVar.gtRegNum) & regSet.rsMaskRegVarFloat));
                }
                else
                {
                    if (compCurFPState.TopVirtual() == (unsigned)src->gtRegNum)
                    {
                        inst_FS(ins_RN, compCurFPState.VirtualToST(dst->gtRegNum));
                    }
                    else
                    {
                        FlatFPX87_MoveToTOS(&compCurFPState, dst->gtRegNum);
                        inst_FN(ins_NN, compCurFPState.VirtualToST(src->gtRegNum));
                    }
                }
            }
            else
            {
                // do operation with memory and store in dest
                FlatFPX87_MoveToTOS(&compCurFPState, dst->gtRegNum);
                inst_FS_TT(ins_NN, src);
            }
        }
        else
        {
            if (dstreg == srcreg)
            {
                FlatFPX87_MoveToTOS(&compCurFPState, dstreg);
                inst_FN(ins_NN, compCurFPState.VirtualToST(dstreg));
            }
            else
            {
                if (compCurFPState.TopVirtual() == (unsigned)dst->gtRegNum)
                {
                    // Do operation and store in srcreg
                    inst_FS(ins_RP, compCurFPState.VirtualToST(srcreg));

                    // kill current dst and rename src as dst.
                    FlatFPX87_Kill(&compCurFPState, dstreg);
                    compCurFPState.Rename(dstreg, srcreg);
                }
                else
                {
                    FlatFPX87_MoveToTOS(&compCurFPState, srcreg);

                    // do reverse and pop operation
                    inst_FS(ins_NP, compCurFPState.VirtualToST(dstreg));

                    // Kill the register
                    FlatFPX87_Kill(&compCurFPState, srcreg);
                }
            }
        }

        result = dstreg;
    }
    else
    {
        assert(!"if we get here it means we didnt load op1 into a temp. Investigate why");
    }

    assert(result != REG_FPNONE);
    return result;
}

void CodeGen::genCodeForTreeStackFP_AsgArithm(GenTreePtr tree)
{
#ifdef DEBUG
    if (compiler->verbose)
    {
        printf("genCodeForTreeStackFP_AsgArithm() ");
        Compiler::printTreeID(tree);
        printf("\n");
    }
#endif // DEBUG

    assert(tree->OperGet() == GT_ASG_ADD || tree->OperGet() == GT_ASG_SUB || tree->OperGet() == GT_ASG_MUL ||
           tree->OperGet() == GT_ASG_DIV);

    GenTreePtr op1, op2;

    op1 = tree->gtOp.gtOp1;
    op2 = tree->gtGetOp2();

    genSetupForOpStackFP(op1, op2, (tree->gtFlags & GTF_REVERSE_OPS) ? true : false, true, false, true);

    regNumber result = genAsgArithmStackFP(tree->OperGet(), op1, (op1->InReg()) ? op1->gtRegNum : REG_FPNONE, op2,
                                           (op2->InReg()) ? op2->gtRegNum : REG_FPNONE);

    genCodeForTreeStackFP_DONE(tree, result);
}

regNumber CodeGen::genAsgArithmStackFP(
    genTreeOps oper, GenTreePtr dst, regNumber dstreg, GenTreePtr src, regNumber srcreg)
{
    regNumber result = REG_FPNONE;

#ifdef DEBUG
    if (compiler->verbose)
    {
        printf("genAsgArithmStackFP() dst: ");
        Compiler::printTreeID(dst);
        printf(" src: ");
        Compiler::printTreeID(src);
        printf(" dstreg: %s srcreg: %s\n", dstreg == REG_FPNONE ? "NONE" : regVarNameStackFP(dstreg),
               srcreg == REG_FPNONE ? "NONE" : regVarNameStackFP(srcreg));
    }
#endif // DEBUG

    instruction ins_NN;
    instruction ins_RN;
    instruction ins_RP;
    instruction ins_NP;

    switch (oper)
    {
        default:
            assert(!"Unexpected oper");
            break;
        case GT_ASG_ADD:
        case GT_ASG_SUB:
        case GT_ASG_MUL:
        case GT_ASG_DIV:

            assert(FPmathRN[GT_ASG_ADD - GT_ASG_ADD] == INS_fadd);
            assert(FPmathRN[GT_ASG_SUB - GT_ASG_ADD] == INS_fsubr);
            assert(FPmathRN[GT_ASG_MUL - GT_ASG_ADD] == INS_fmul);
            assert(FPmathRN[GT_ASG_DIV - GT_ASG_ADD] == INS_fdivr);

            assert(FPmathRP[GT_ASG_ADD - GT_ASG_ADD] == INS_faddp);
            assert(FPmathRP[GT_ASG_SUB - GT_ASG_ADD] == INS_fsubrp);
            assert(FPmathRP[GT_ASG_MUL - GT_ASG_ADD] == INS_fmulp);
            assert(FPmathRP[GT_ASG_DIV - GT_ASG_ADD] == INS_fdivrp);

            ins_NN = FPmathNN[oper - GT_ASG_ADD];
            ins_NP = FPmathNP[oper - GT_ASG_ADD];

            ins_RN = FPmathRN[oper - GT_ASG_ADD];
            ins_RP = FPmathRP[oper - GT_ASG_ADD];

            if (dstreg != REG_FPNONE)
            {
                assert(!"dst should be a regvar or memory");
            }
            else
            {
                if (dst->IsRegVar())
                {
                    if (src->IsRegVar())
                    {
                        if (src->IsRegVarDeath())
                        {
                            // Take src to top of stack
                            FlatFPX87_MoveToTOS(&compCurFPState, src->gtRegNum);

                            // Do op
                            inst_FS(ins_NP, compCurFPState.VirtualToST(dst->gtRegNum));

                            // Kill the register
                            FlatFPX87_Kill(&compCurFPState, src->gtRegNum);

                            // SetupOp should mark the regvar as dead
                            assert((genRegMaskFloat(src->gtRegVar.gtRegNum) & regSet.rsMaskRegVarFloat) == 0);
                        }
                        else
                        {
                            assert(src->gtRegNum == src->gtRegVar.gtRegNum &&
                                   "We shoudnt be loading regvar src on the stack as src is readonly");

                            // Take src to top of stack
                            FlatFPX87_MoveToTOS(&compCurFPState, src->gtRegNum);

                            // Do op
                            inst_FS(ins_RN, compCurFPState.VirtualToST(dst->gtRegNum));
                        }
                    }
                    else
                    {
                        if (srcreg == REG_FPNONE)
                        {
                            // take enregistered variable to top of stack
                            FlatFPX87_MoveToTOS(&compCurFPState, dst->gtRegNum);

                            // Do operation with mem
                            inst_FS_TT(ins_NN, src);
                        }
                        else
                        {
                            // take enregistered variable to top of stack
                            FlatFPX87_MoveToTOS(&compCurFPState, src->gtRegNum);

                            // do op
                            inst_FS(ins_NP, compCurFPState.VirtualToST(dst->gtRegNum));

                            // Kill the register
                            FlatFPX87_Kill(&compCurFPState, src->gtRegNum);
                        }
                    }
                }
                else
                {
                    // To memory
                    if ((src->IsRegVar()) && !src->IsRegVarDeath())
                    {
                        // We set src as read only, but as dst is in memory, we will need
                        // an extra physical register (which we should have, as we have a
                        // spare one for transitions).
                        //
                        // There used to be an assertion: assert(src->gtRegNum == src->gtRegVar.gtRegNum, ...)
                        // here, but there's actually no reason to assume that.  AFAICT, for FP vars under stack FP,
                        // src->gtRegVar.gtRegNum is the allocated stack pseudo-register, but src->gtRegNum is the
                        // FP stack position into which that is loaded to represent a particular use of the variable.
                        inst_FN(INS_fld, compCurFPState.VirtualToST(src->gtRegNum));

                        // Do operation with mem
                        inst_FS_TT(ins_RN, dst);

                        // store back
                        inst_FS_TT(INS_fstp, dst);
                    }
                    else
                    {
                        // put src in top of stack
                        FlatFPX87_MoveToTOS(&compCurFPState, srcreg);

                        // Do operation with mem
                        inst_FS_TT(ins_RN, dst);

                        // store back
                        inst_FS_TT(INS_fstp, dst);

                        // SetupOp should have marked the regvar as dead in tat case
                        assert(!src->IsRegVar() || !src->IsRegVarDeath() ||
                               (genRegMaskFloat(src->gtRegVar.gtRegNum) & regSet.rsMaskRegVarFloat) == 0);

                        FlatFPX87_Kill(&compCurFPState, srcreg);
                    }
                }
            }
    }

    return result;
}

void CodeGen::genCodeForTreeStackFP_SmpOp(GenTreePtr tree)
{
#ifdef DEBUG
    if (compiler->verbose)
    {
        printf("genCodeForTreeStackFP_SmpOp() ");
        Compiler::printTreeID(tree);
        printf("\n");
    }
#endif // DEBUG

    assert(tree->OperKind() & GTK_SMPOP);

    switch (tree->OperGet())
    {
        // Assignment
        case GT_ASG:
        {
            genCodeForTreeStackFP_Asg(tree);
            break;
        }

        // Arithmetic binops
        case GT_ADD:
        case GT_SUB:
        case GT_MUL:
        case GT_DIV:
        {
            genCodeForTreeStackFP_Arithm(tree);
            break;
        }

        // Asg-Arithmetic ops
        case GT_ASG_ADD:
        case GT_ASG_SUB:
        case GT_ASG_MUL:
        case GT_ASG_DIV:
        {
            genCodeForTreeStackFP_AsgArithm(tree);
            break;
        }

        case GT_IND:
        case GT_LEA:
        {
            regMaskTP addrReg;

            // Make sure the address value is 'addressable' */
            addrReg = genMakeAddressable(tree, 0, RegSet::FREE_REG);

            // Load the value onto the FP stack
            regNumber reg = regSet.PickRegFloat();
            genLoadStackFP(tree, reg);

            genDoneAddressable(tree, addrReg, RegSet::FREE_REG);

            genCodeForTreeStackFP_DONE(tree, reg);

            break;
        }

        case GT_RETURN:
        {
            GenTreePtr op1 = tree->gtOp.gtOp1;
            assert(op1);

            // Compute the result onto the FP stack
            if (op1->gtType == TYP_FLOAT)
            {
#if ROUND_FLOAT
                bool roundOp1 = false;

                switch (getRoundFloatLevel())
                {
                    case ROUND_NEVER:
                        /* No rounding at all */
                        break;

                    case ROUND_CMP_CONST:
                        break;

                    case ROUND_CMP:
                        /* Round all comparands and return values*/
                        roundOp1 = true;
                        break;

                    case ROUND_ALWAYS:
                        /* Round everything */
                        roundOp1 = true;
                        break;

                    default:
                        assert(!"Unsupported Round Level");
                        break;
                }
#endif
                genCodeForTreeFlt(op1);
            }
            else
            {
                assert(op1->gtType == TYP_DOUBLE);
                genCodeForTreeFloat(op1);

#if ROUND_FLOAT
                if ((op1->gtOper == GT_CAST) && (op1->CastFromType() == TYP_LONG))
                    genRoundFpExpressionStackFP(op1);
#endif
            }

            // kill enregistered variables
            compCurFPState.Pop();
            assert(compCurFPState.m_uStackSize == 0);
            break;
        }

        case GT_COMMA:
        {
            GenTreePtr op1 = tree->gtOp.gtOp1;
            GenTreePtr op2 = tree->gtGetOp2();

            if (tree->gtFlags & GTF_REVERSE_OPS)
            {
                genCodeForTreeFloat(op2);

                regSet.SetUsedRegFloat(op2, true);

                genEvalSideEffects(op1);

                if (op2->gtFlags & GTF_SPILLED)
                {
                    UnspillFloat(op2);
                }

                regSet.SetUsedRegFloat(op2, false);
            }
            else
            {
                genEvalSideEffects(op1);
                genCodeForTreeFloat(op2);
            }

            genCodeForTreeStackFP_DONE(tree, op2->gtRegNum);
            break;
        }
        case GT_CAST:
        {
            genCodeForTreeStackFP_Cast(tree);
            break;
        }

        case GT_NEG:
        {
            GenTreePtr op1 = tree->gtOp.gtOp1;

            // get the tree into a register
            genCodeForTreeFloat(op1);

            // Take reg to top of stack
            FlatFPX87_MoveToTOS(&compCurFPState, op1->gtRegNum);

            // change the sign
            instGen(INS_fchs);

            // mark register that holds tree
            genCodeForTreeStackFP_DONE(tree, op1->gtRegNum);
            return;
        }
        case GT_INTRINSIC:
        {
            assert(Compiler::IsMathIntrinsic(tree));

            GenTreePtr op1 = tree->gtOp.gtOp1;

            // get tree into a register
            genCodeForTreeFloat(op1);

            // Take reg to top of stack
            FlatFPX87_MoveToTOS(&compCurFPState, op1->gtRegNum);

            static const instruction mathIns[] = {
                INS_fsin, INS_fcos, INS_fsqrt, INS_fabs, INS_frndint,
            };

            assert(mathIns[CORINFO_INTRINSIC_Sin] == INS_fsin);
            assert(mathIns[CORINFO_INTRINSIC_Cos] == INS_fcos);
            assert(mathIns[CORINFO_INTRINSIC_Sqrt] == INS_fsqrt);
            assert(mathIns[CORINFO_INTRINSIC_Abs] == INS_fabs);
            assert(mathIns[CORINFO_INTRINSIC_Round] == INS_frndint);
            assert((unsigned)(tree->gtIntrinsic.gtIntrinsicId) < sizeof(mathIns) / sizeof(mathIns[0]));
            instGen(mathIns[tree->gtIntrinsic.gtIntrinsicId]);

            // mark register that holds tree
            genCodeForTreeStackFP_DONE(tree, op1->gtRegNum);

            return;
        }
        case GT_CKFINITE:
        {
            TempDsc* temp;
            int      offs;

            GenTreePtr op1 = tree->gtOp.gtOp1;

            // Offset of the DWord containing the exponent
            offs = (op1->gtType == TYP_FLOAT) ? 0 : sizeof(int);

            // get tree into a register
            genCodeForTreeFloat(op1);

            // Take reg to top of stack
            FlatFPX87_MoveToTOS(&compCurFPState, op1->gtRegNum);

            temp          = compiler->tmpGetTemp(op1->TypeGet());
            emitAttr size = EmitSize(op1);

            // Store the value from the FP stack into the temp
            getEmitter()->emitIns_S(INS_fst, size, temp->tdTempNum(), 0);

            regNumber reg = regSet.rsPickReg();

            // Load the DWord containing the exponent into a general reg.
            inst_RV_ST(INS_mov, reg, temp, offs, op1->TypeGet(), EA_4BYTE);
            compiler->tmpRlsTemp(temp);

            // 'reg' now contains the DWord containing the exponent
            regTracker.rsTrackRegTrash(reg);

            // Mask of exponent with all 1's - appropriate for given type

            int expMask;
            expMask = (op1->gtType == TYP_FLOAT) ? 0x7F800000  // TYP_FLOAT
                                                 : 0x7FF00000; // TYP_DOUBLE

            // Check if the exponent is all 1's

            inst_RV_IV(INS_and, reg, expMask, EA_4BYTE);
            inst_RV_IV(INS_cmp, reg, expMask, EA_4BYTE);

            // If exponent was all 1's, we need to throw ArithExcep
            genJumpToThrowHlpBlk(EJ_je, SCK_ARITH_EXCPN);

            genUpdateLife(tree);

            genCodeForTreeStackFP_DONE(tree, op1->gtRegNum);
            break;
        }
        default:
            NYI("opertype");
    }
}

void CodeGen::genCodeForTreeStackFP_Cast(GenTreePtr tree)
{
#ifdef DEBUG
    if (compiler->verbose)
    {
        printf("genCodeForTreeStackFP_Cast() ");
        Compiler::printTreeID(tree);
        printf("\n");
    }
#endif // DEBUG

#if ROUND_FLOAT
    bool roundResult = true;
#endif

    regMaskTP addrReg;
    TempDsc*  temp;
    emitAttr  size;

    GenTreePtr op1 = tree->gtOp.gtOp1;

    // If op1 is a comma expression, evaluate the non-last parts, make op1 be the rest.
    op1 = genCodeForCommaTree(op1);

    switch (op1->gtType)
    {
        case TYP_BOOL:
        case TYP_BYTE:
        case TYP_UBYTE:
        case TYP_CHAR:
        case TYP_SHORT:
        {

            // Operand too small for 'fild', load it into a register
            genCodeForTree(op1, 0);

#if ROUND_FLOAT
            // no need to round, can't overflow float or dbl
            roundResult = false;
#endif

            // fall through
        }
        case TYP_INT:
        case TYP_BYREF:
        case TYP_LONG:
        {
            // Can't 'fild' a constant, it has to be loaded from memory
            switch (op1->gtOper)
            {
                case GT_CNS_INT:
                    op1 = genMakeConst(&op1->gtIntCon.gtIconVal, TYP_INT, tree, false);
                    break;

                case GT_CNS_LNG:
                    // Our encoder requires fild on m64int to be 64-bit aligned.
                    op1 = genMakeConst(&op1->gtLngCon.gtLconVal, TYP_LONG, tree, true);
                    break;
                default:
                    break;
            }

            addrReg = genMakeAddressable(op1, 0, RegSet::FREE_REG);

            // Grab register for the cast
            regNumber reg = regSet.PickRegFloat();
            genMarkTreeInReg(tree, reg);
            compCurFPState.Push(reg);

            // Is the value now sitting in a register?
            if (op1->InReg())
            {
                // We'll have to store the value into the stack */
                size = EA_ATTR(roundUp(genTypeSize(op1->gtType)));
                temp = compiler->tmpGetTemp(op1->TypeGet());

                // Move the value into the temp
                if (op1->gtType == TYP_LONG)
                {
                    regPairNo regPair = op1->gtRegPair;

                    // This code is pretty ugly, but straightforward

                    if (genRegPairLo(regPair) == REG_STK)
                    {
                        regNumber rg1 = genRegPairHi(regPair);

                        assert(rg1 != REG_STK);

                        /* Move enregistered half to temp */

                        inst_ST_RV(INS_mov, temp, 4, rg1, TYP_LONG);

                        /* Move lower half to temp via "high register" */

                        inst_RV_TT(INS_mov, rg1, op1, 0);
                        inst_ST_RV(INS_mov, temp, 0, rg1, TYP_LONG);

                        /* Reload transfer register */

                        inst_RV_ST(INS_mov, rg1, temp, 4, TYP_LONG);
                    }
                    else if (genRegPairHi(regPair) == REG_STK)
                    {
                        regNumber rg1 = genRegPairLo(regPair);

                        assert(rg1 != REG_STK);

                        /* Move enregistered half to temp */

                        inst_ST_RV(INS_mov, temp, 0, rg1, TYP_LONG);

                        /* Move high half to temp via "low register" */

                        inst_RV_TT(INS_mov, rg1, op1, 4);
                        inst_ST_RV(INS_mov, temp, 4, rg1, TYP_LONG);

                        /* Reload transfer register */

                        inst_RV_ST(INS_mov, rg1, temp, 0, TYP_LONG);
                    }
                    else
                    {
                        /* Move the value into the temp */

                        inst_ST_RV(INS_mov, temp, 0, genRegPairLo(regPair), TYP_LONG);
                        inst_ST_RV(INS_mov, temp, 4, genRegPairHi(regPair), TYP_LONG);
                    }
                    genDoneAddressable(op1, addrReg, RegSet::FREE_REG);

                    /* Load the long from the temp */

                    inst_FS_ST(INS_fildl, size, temp, 0);
                }
                else
                {
                    /* Move the value into the temp */

                    inst_ST_RV(INS_mov, temp, 0, op1->gtRegNum, TYP_INT);

                    genDoneAddressable(op1, addrReg, RegSet::FREE_REG);

                    /* Load the integer from the temp */

                    inst_FS_ST(INS_fild, size, temp, 0);
                }

                // We no longer need the temp
                compiler->tmpRlsTemp(temp);
            }
            else
            {
                // Load the value from its address
                if (op1->gtType == TYP_LONG)
                    inst_TT(INS_fildl, op1);
                else
                    inst_TT(INS_fild, op1);

                genDoneAddressable(op1, addrReg, RegSet::FREE_REG);
            }

#if ROUND_FLOAT
            /* integer to fp conversions can overflow. roundResult
            * is cleared above in cases where it can't
            */
            if (roundResult &&
                ((tree->gtType == TYP_FLOAT) || ((tree->gtType == TYP_DOUBLE) && (op1->gtType == TYP_LONG))))
                genRoundFpExpression(tree);
#endif

            break;
        }
        case TYP_FLOAT:
        {
            //  This is a cast from float to double.
            //  Note that conv.r(r4/r8) and conv.r8(r4/r9) are indistinguishable
            //  as we will generate GT_CAST-TYP_DOUBLE for both. This would
            //  cause us to truncate precision in either case. However,
            //  conv.r was needless in the first place, and should have
            //  been removed */
            genCodeForTreeFloat(op1); // Trucate its precision

            if (op1->gtOper == GT_LCL_VAR || op1->gtOper == GT_LCL_FLD || op1->gtOper == GT_CLS_VAR ||
                op1->gtOper == GT_IND || op1->gtOper == GT_LEA)
            {
                // We take advantage here of the fact that we know that our
                // codegen will have just loaded this from memory, and that
                // therefore, no cast is really needed.
                // Ideally we wouldn't do this optimization here, but in
                // morphing, however, we need to do this after regalloc, as
                // this optimization doesnt apply if what we're loading is a
                // regvar
            }
            else
            {
                genRoundFpExpressionStackFP(op1, tree->TypeGet());
            }

            // Assign reg to tree
            genMarkTreeInReg(tree, op1->gtRegNum);

            break;
        }
        case TYP_DOUBLE:
        {
            // This is a cast from double to float or double
            // Load the value, store as destType, load back
            genCodeForTreeFlt(op1);

            if ((op1->gtOper == GT_LCL_VAR || op1->gtOper == GT_LCL_FLD || op1->gtOper == GT_CLS_VAR ||
                 op1->gtOper == GT_IND || op1->gtOper == GT_LEA) &&
                tree->TypeGet() == TYP_DOUBLE)
            {
                // We take advantage here of the fact that we know that our
                // codegen will have just loaded this from memory, and that
                // therefore, no cast is really needed.
                // Ideally we wouldn't do this optimization here, but in
                // morphing. However, we need to do this after regalloc, as
                // this optimization doesnt apply if what we're loading is a
                // regvar
            }
            else
            {
                genRoundFpExpressionStackFP(op1, tree->TypeGet());
            }

            // Assign reg to tree
            genMarkTreeInReg(tree, op1->gtRegNum);

            break;
        }
        default:
        {
            assert(!"unsupported cast");
            break;
        }
    }
}

void CodeGen::genCodeForTreeStackFP_Special(GenTreePtr tree)
{
#ifdef DEBUG
    if (compiler->verbose)
    {
        printf("genCodeForTreeStackFP_Special() ");
        Compiler::printTreeID(tree);
        printf("\n");
    }
#endif // DEBUG

    switch (tree->OperGet())
    {
        case GT_CALL:
        {
            genCodeForCall(tree, true);
            break;
        }
        default:
            NYI("genCodeForTreeStackFP_Special");
            break;
    }
}

void CodeGen::genCodeForTreeFloat(GenTreePtr tree, RegSet::RegisterPreference* pref)
{
    // TestTransitions();
    genTreeOps oper;
    unsigned   kind;

    assert(tree);
    assert(tree->gtOper != GT_STMT);
    assert(varTypeIsFloating(tree));

    // What kind of node do we have?
    oper = tree->OperGet();
    kind = tree->OperKind();

    if (kind & GTK_CONST)
    {
        genCodeForTreeStackFP_Const(tree);
    }
    else if (kind & GTK_LEAF)
    {
        genCodeForTreeStackFP_Leaf(tree);
    }
    else if (kind & GTK_SMPOP)
    {
        genCodeForTreeStackFP_SmpOp(tree);
    }
    else
    {
        genCodeForTreeStackFP_Special(tree);
    }

#ifdef DEBUG
    if (verbose)
    {
        JitDumpFPState();
    }
    assert(compCurFPState.IsConsistent());
#endif
}

bool CodeGen::genCompInsStackFP(GenTreePtr tos, GenTreePtr other)
{
    // assume gensetupop done

    bool bUseFcomip = genUse_fcomip();
    bool bReverse   = false;

    // Take op1 to top of the stack
    FlatFPX87_MoveToTOS(&compCurFPState, tos->gtRegNum);

    // We pop top of stack if it's not a live regvar
    bool bPopTos   = !(tos->IsRegVar() && !tos->IsRegVarDeath()) || (tos->InReg());
    bool bPopOther = !(other->IsRegVar() && !other->IsRegVarDeath()) || (other->InReg());

    assert(tos->IsRegVar() || (tos->InReg()));

    if (!(other->IsRegVar() || (other->InReg())))
    {
        // op2 in memory
        assert(bPopOther);

        if (bUseFcomip)
        {
            // We should have space for a load
            assert(compCurFPState.m_uStackSize < FP_PHYSICREGISTERS);

            // load from mem, now the comparison will be the other way around
            inst_FS_TT(INS_fld, other);
            inst_FN(INS_fcomip, 1);

            // pop if we've been asked to do so
            if (bPopTos)
            {
                inst_FS(INS_fstp, 0);
                FlatFPX87_Kill(&compCurFPState, tos->gtRegNum);
            }

            bReverse = true;
        }
        else
        {
            // compare directly with memory
            if (bPopTos)
            {
                inst_FS_TT(INS_fcomp, other);
                FlatFPX87_Kill(&compCurFPState, tos->gtRegNum);
            }
            else
            {
                inst_FS_TT(INS_fcom, other);
            }
        }
    }
    else
    {
        if (bUseFcomip)
        {
            if (bPopTos)
            {
                inst_FN(INS_fcomip, compCurFPState.VirtualToST(other->gtRegNum));
                FlatFPX87_Kill(&compCurFPState, tos->gtRegNum);
            }
            else
            {
                inst_FN(INS_fcomi, compCurFPState.VirtualToST(other->gtRegNum));
            }

            if (bPopOther)
            {
                FlatFPX87_Unload(&compCurFPState, other->gtRegNum);
            }
        }
        else
        {
            if (bPopTos)
            {
                inst_FN(INS_fcomp, compCurFPState.VirtualToST(other->gtRegNum));
                FlatFPX87_Kill(&compCurFPState, tos->gtRegNum);
            }
            else
            {
                inst_FN(INS_fcom, compCurFPState.VirtualToST(other->gtRegNum));
            }

            if (bPopOther)
            {
                FlatFPX87_Unload(&compCurFPState, other->gtRegNum);
            }
        }
    }

    if (!bUseFcomip)
    {
        // oops, we have to put result of compare in eflags

        // Grab EAX for the result of the fnstsw
        regSet.rsGrabReg(RBM_EAX);

        // Generate the 'fnstsw' and test its result
        inst_RV(INS_fnstsw, REG_EAX, TYP_INT);
        regTracker.rsTrackRegTrash(REG_EAX);
        instGen(INS_sahf);
    }

    return bReverse;
}

void CodeGen::genCondJumpFltStackFP(GenTreePtr cond, BasicBlock* jumpTrue, BasicBlock* jumpFalse, bool bDoTransition)
{
    assert(jumpTrue && jumpFalse);
    assert(!(cond->gtFlags & GTF_REVERSE_OPS)); // Done in genCondJump()
    assert(varTypeIsFloating(cond->gtOp.gtOp1));

    GenTreePtr op1 = cond->gtOp.gtOp1;
    GenTreePtr op2 = cond->gtOp.gtOp2;
    genTreeOps cmp = cond->OperGet();

    // Prepare operands.
    genSetupForOpStackFP(op1, op2, false, false, true, false);

    GenTreePtr tos;
    GenTreePtr other;
    bool       bReverseCmp = false;

    if ((op2->IsRegVar() || (op2->InReg())) &&                     // op2 is in a reg
        (compCurFPState.TopVirtual() == (unsigned)op2->gtRegNum && // Is it already at the top of the stack?
         (!op2->IsRegVar() || op2->IsRegVarDeath())))              // are we going to pop it off?
    {
        tos         = op2;
        other       = op1;
        bReverseCmp = true;
    }
    else
    {
        tos         = op1;
        other       = op2;
        bReverseCmp = false;
    }

    if (genCompInsStackFP(tos, other))
    {
        bReverseCmp = !bReverseCmp;
    }

    // do .un comparison
    if (cond->gtFlags & GTF_RELOP_NAN_UN)
    {
        // Generate the first jump (NaN check)
        genCondJmpInsStackFP(EJ_jpe, jumpTrue, NULL, bDoTransition);
    }
    else
    {
        jumpFalse->bbFlags |= BBF_JMP_TARGET | BBF_HAS_LABEL;

        // Generate the first jump (NaN check)
        genCondJmpInsStackFP(EJ_jpe, jumpFalse, NULL, bDoTransition);
    }

    /* Generate the second jump (comparison) */
    const static BYTE dblCmpTstJmp2[] = {
        EJ_je,  // GT_EQ
        EJ_jne, // GT_NE
        EJ_jb,  // GT_LT
        EJ_jbe, // GT_LE
        EJ_jae, // GT_GE
        EJ_ja,  // GT_GT
    };

    // Swap comp order if necessary
    if (bReverseCmp)
    {
        cmp = GenTree::SwapRelop(cmp);
    }

    genCondJmpInsStackFP((emitJumpKind)dblCmpTstJmp2[cmp - GT_EQ], jumpTrue, jumpFalse, bDoTransition);
}

BasicBlock* CodeGen::genTransitionBlockStackFP(FlatFPStateX87* pState, BasicBlock* pFrom, BasicBlock* pTarget)
{
    // Fast paths where a transition block is not necessary
    if (pTarget->bbFPStateX87 && FlatFPStateX87::AreEqual(pState, pTarget->bbFPStateX87) || pState->IsEmpty())
    {
        return pTarget;
    }

    // We shouldn't have any handlers if we're generating transition blocks, as we don't know
    // how to recover them
    assert(compiler->compMayHaveTransitionBlocks);
    assert(compiler->compHndBBtabCount == 0);

#ifdef DEBUG
    compiler->fgSafeBasicBlockCreation = true;
#endif

    // Create a temp block
    BasicBlock* pBlock = compiler->bbNewBasicBlock(BBJ_ALWAYS);

#ifdef DEBUG
    compiler->fgSafeBasicBlockCreation = false;
#endif

    VarSetOps::Assign(compiler, pBlock->bbLiveIn, pFrom->bbLiveOut);
    VarSetOps::Assign(compiler, pBlock->bbLiveOut, pFrom->bbLiveOut);

    pBlock->bbJumpDest = pTarget;
    pBlock->bbFlags |= BBF_JMP_TARGET;
    //
    // If either pFrom or pTarget are cold blocks then
    // the transition block also must be cold
    //
    pBlock->bbFlags |= (pFrom->bbFlags & BBF_COLD);
    pBlock->bbFlags |= (pTarget->bbFlags & BBF_COLD);

    // The FP state for the block is the same as the current one
    pBlock->bbFPStateX87 = FlatFPAllocFPState(pState);

    if ((pBlock->bbFlags & BBF_COLD) || (compiler->fgFirstColdBlock == NULL))
    {
        //
        // If this block is cold or if all blocks are hot
        // then we just insert it at the end of the method.
        //
        compiler->fgMoveBlocksAfter(pBlock, pBlock, compiler->fgLastBBInMainFunction());
    }
    else
    {
        //
        // This block is hot so we need to insert it in the hot region
        // of the method.
        //
        BasicBlock* lastHotBlock = compiler->fgFirstColdBlock->bbPrev;
        noway_assert(lastHotBlock != nullptr);

        if (lastHotBlock->bbFallsThrough())
            NO_WAY("Bad fgFirstColdBlock in genTransitionBlockStackFP()");

        //
        // Insert pBlock between lastHotBlock and fgFirstColdBlock
        //
        compiler->fgInsertBBafter(lastHotBlock, pBlock);
    }

    return pBlock;
}

void CodeGen::genCondJumpLngStackFP(GenTreePtr cond, BasicBlock* jumpTrue, BasicBlock* jumpFalse)
{
    // For the moment, and so we don't have to deal with the amount of special cases
    // we have, will insert a dummy block for jumpTrue (if necessary) that will do the
    // transition for us. For the jumpFalse case, we play a trick. For the false case ,
    // a Long conditional has a fallthrough (least significant DWORD check is false) and
    // also has a jump to the fallthrough (bbNext) if the most significant DWORD check
    // fails. However, we do want to make an FP transition if we're in the later case,
    // So what we do is create a label and make jumpFalse go there. This label is defined
    // before doing the FP transition logic at the end of the block, so now both exit paths
    // for false condition will go through the transition and then fall through to bbnext.
    assert(jumpFalse == compiler->compCurBB->bbNext);

    BasicBlock* pTransition = genCreateTempLabel();

    genCondJumpLng(cond, jumpTrue, pTransition, true);

    genDefineTempLabel(pTransition);
}

void CodeGen::genQMarkRegVarTransition(GenTreePtr nextNode, VARSET_VALARG_TP liveset)
{
    // Kill any vars that may die in the transition
    VARSET_TP VARSET_INIT_NOCOPY(newLiveSet, VarSetOps::Intersection(compiler, liveset, compiler->optAllFPregVars));

    regMaskTP liveRegIn = genRegMaskFromLivenessStackFP(newLiveSet);
    genCodeForTransitionFromMask(&compCurFPState, liveRegIn);

    unsigned i;

    // Kill all regvars
    for (i = REG_FPV0; i < REG_FPCOUNT; i++)
    {
        if ((genRegMaskFloat((regNumber)i) & regSet.rsMaskRegVarFloat))
        {

            genRegVarDeathStackFP(regSet.genRegVarsFloat[i]);
        }
    }

    // Born necessary regvars
    for (i = 0; i < compiler->lvaTrackedCount; i++)
    {
        unsigned   lclVar = compiler->lvaTrackedToVarNum[i];
        LclVarDsc* varDsc = compiler->lvaTable + lclVar;

        assert(varDsc->lvTracked);

        if (varDsc->lvRegister && VarSetOps::IsMember(compiler, newLiveSet, i))
        {
            genRegVarBirthStackFP(varDsc);
        }
    }
}

void CodeGen::genQMarkBeforeElseStackFP(QmarkStateStackFP* pState, VARSET_VALARG_TP varsetCond, GenTreePtr nextNode)
{
    assert(regSet.rsMaskLockedFloat == 0);

    // Save current state at colon
    pState->stackState.Init(&compCurFPState);

    // Kill any vars that may die in the transition to then
    genQMarkRegVarTransition(nextNode, varsetCond);
}

void CodeGen::genQMarkAfterElseBlockStackFP(QmarkStateStackFP* pState, VARSET_VALARG_TP varsetCond, GenTreePtr nextNode)
{
    assert(regSet.rsMaskLockedFloat == 0);

    FlatFPStateX87 tempSwap;

    // Save current state. Now tempFPState will store the target state for the else block
    tempSwap.Init(&compCurFPState);

    compCurFPState.Init(&pState->stackState);

    pState->stackState.Init(&tempSwap);

    // Did any regvars die in the then block that are live on entry to the else block?
    unsigned i;
    for (i = 0; i < compiler->lvaTrackedCount; i++)
    {
        if (VarSetOps::IsMember(compiler, varsetCond, i) && VarSetOps::IsMember(compiler, compiler->optAllFPregVars, i))
        {
            // This variable should be live
            unsigned   lclnum = compiler->lvaTrackedToVarNum[i];
            LclVarDsc* varDsc = compiler->lvaTable + lclnum;

            if (regSet.genRegVarsFloat[varDsc->lvRegNum] != varDsc)
            {
                JITDUMP("genQMarkAfterThenBlockStackFP(): Fixing up regvar that was modified in then\n");
                if (regSet.genRegVarsFloat[varDsc->lvRegNum])
                {
                    genRegVarDeathStackFP(regSet.genRegVarsFloat[varDsc->lvRegNum]);
                }

                genRegVarBirthStackFP(varDsc);
            }
        }
    }

    // Kill any vars that may die in the transition
    genQMarkRegVarTransition(nextNode, varsetCond);
}

void CodeGen::genQMarkAfterThenBlockStackFP(QmarkStateStackFP* pState)
{
    JITDUMP("genQMarkAfterThenBlockStackFP()\n");
    assert(regSet.rsMaskLockedFloat == 0);

    // Generate transition to the previous one set by the then block
    genCodeForTransitionStackFP(&compCurFPState, &pState->stackState);

    // Update state
    compCurFPState.Init(&pState->stackState);
}

void CodeGenInterface::SetRegVarFloat(regNumber reg, var_types type, LclVarDsc* varDsc)
{
    regMaskTP mask = genRegMaskFloat(reg, type);

    if (varDsc)
    {
        JITDUMP("marking register %s as a regvar\n", getRegNameFloat(reg, type));

        assert(mask && ((regSet.rsMaskLockedFloat | regSet.rsMaskRegVarFloat | regSet.rsMaskUsedFloat) & mask) == 0);

        regSet.rsMaskRegVarFloat |= mask;
    }
    else
    {
        JITDUMP("unmarking register %s as a regvar\n", getRegNameFloat(reg, type));

        assert(mask && (regSet.rsMaskRegVarFloat & mask));

        regSet.rsMaskRegVarFloat &= ~mask;
    }

    // Update lookup table
    regSet.genRegVarsFloat[reg] = varDsc;
}

// Generates a conditional jump. It will do the appropiate stack matching for the jmpTrue.
// We don't use jumpFalse anywhere and the integer codebase assumes that it will be bbnext, and that is
// taken care of at the end of the bb code generation.
void CodeGen::genCondJmpInsStackFP(emitJumpKind jumpKind,
                                   BasicBlock*  jumpTrue,
                                   BasicBlock*  jumpFalse,
                                   bool         bDoTransition)
{
    // Assert the condition above.
    assert(!jumpFalse || jumpFalse == compiler->compCurBB->bbNext || !bDoTransition);

    // Do the fp stack matching.
    if (bDoTransition && !jumpTrue->bbFPStateX87 &&
        FlatFPSameRegisters(&compCurFPState, genRegMaskFromLivenessStackFP(jumpTrue->bbLiveIn)))
    {
        // Target block doesn't have state yet, but has the same registers, so
        // we allocate the block and generate the normal jump
        genCodeForBBTransitionStackFP(jumpTrue);
        inst_JMP(jumpKind, jumpTrue);
    }
    else if (!bDoTransition || compCurFPState.IsEmpty() || // If it's empty, target has to be empty too.
             (jumpTrue->bbFPStateX87 && FlatFPStateX87::AreEqual(&compCurFPState, jumpTrue->bbFPStateX87)))
    {
        // Nothing to do here. Proceed normally and generate the jump
        inst_JMP(jumpKind, jumpTrue);

        if (jumpFalse && jumpFalse != compiler->compCurBB->bbNext)
        {
            inst_JMP(EJ_jmp, jumpFalse);
        }
    }
    else
    {
        // temporal workaround for stack matching
        // do a forward conditional jump, generate the transition and jump to the target
        // The payload is an aditional jump instruction, but both jumps will be correctly
        // predicted by the processor in the loop case.
        BasicBlock* endLabel = NULL;

        endLabel = genCreateTempLabel();

        inst_JMP(emitter::emitReverseJumpKind(jumpKind), endLabel);

        genCodeForBBTransitionStackFP(jumpTrue);

        inst_JMP(EJ_jmp, jumpTrue);

        genDefineTempLabel(endLabel);
    }
}

void CodeGen::genTableSwitchStackFP(regNumber reg, unsigned jumpCnt, BasicBlock** jumpTab)
{
    // Only come here when we have to do something special for the FPU stack!
    //
    assert(!compCurFPState.IsEmpty());
    VARSET_TP VARSET_INIT_NOCOPY(liveInFP, VarSetOps::MakeEmpty(compiler));
    VARSET_TP VARSET_INIT_NOCOPY(liveOutFP, VarSetOps::MakeEmpty(compiler));
    for (unsigned i = 0; i < jumpCnt; i++)
    {
        VarSetOps::Assign(compiler, liveInFP, jumpTab[i]->bbLiveIn);
        VarSetOps::IntersectionD(compiler, liveInFP, compiler->optAllFPregVars);
        VarSetOps::Assign(compiler, liveOutFP, compiler->compCurBB->bbLiveOut);
        VarSetOps::IntersectionD(compiler, liveOutFP, compiler->optAllFPregVars);

        if (!jumpTab[i]->bbFPStateX87 && VarSetOps::Equal(compiler, liveInFP, liveOutFP))
        {
            // Hasn't state yet and regvar set is the same, so just copy state and don't change the jump
            jumpTab[i]->bbFPStateX87 = FlatFPAllocFPState(&compCurFPState);
        }
        else if (jumpTab[i]->bbFPStateX87 && FlatFPStateX87::AreEqual(&compCurFPState, jumpTab[i]->bbFPStateX87))
        {
            // Same state, don't change the jump
        }
        else
        {
            // We have to do a transition. First check if we can reuse another one
            unsigned j;
            for (j = 0; j < i; j++)
            {
                // Has to be already forwarded. If not it can't be targetting the same block
                if (jumpTab[j]->bbFlags & BBF_FORWARD_SWITCH)
                {
                    if (jumpTab[i] == jumpTab[j]->bbJumpDest)
                    {
                        // yipee, we can reuse this transition block
                        jumpTab[i] = jumpTab[j];
                        break;
                    }
                }
            }

            if (j == i)
            {
                // We will have to create a new transition block
                jumpTab[i] = genTransitionBlockStackFP(&compCurFPState, compiler->compCurBB, jumpTab[i]);

                jumpTab[i]->bbFlags |= BBF_FORWARD_SWITCH;
            }
        }
    }

    // Clear flag
    for (unsigned i = 0; i < jumpCnt; i++)
    {
        jumpTab[i]->bbFlags &= ~BBF_FORWARD_SWITCH;
    }

    // everything's fixed now, so go down the normal path
    return genTableSwitch(reg, jumpCnt, jumpTab);
}

bool CodeGen::genConstantLoadStackFP(GenTreePtr tree, bool bOnlyNoMemAccess)
{
    assert(tree->gtOper == GT_CNS_DBL);

    bool        bFastConstant  = false;
    instruction ins_ConstantNN = INS_fldz; // keep compiler happy

    // Both positive 0 and 1 are represnetable in float and double, beware if we add other constants
    switch (*((__int64*)&(tree->gtDblCon.gtDconVal)))
    {
        case 0:
            // CAREFUL here!, -0 is different than +0, a -0 shouldn't issue a fldz.
            ins_ConstantNN = INS_fldz;
            bFastConstant  = true;
            break;
        case I64(0x3ff0000000000000):
            ins_ConstantNN = INS_fld1;
            bFastConstant  = true;
    }

    if (bFastConstant == false && bOnlyNoMemAccess)
    {
        // Caller asked only to generate instructions if it didn't involve memory accesses
        return false;
    }

    if (bFastConstant)
    {
        assert(compCurFPState.m_uStackSize <= FP_PHYSICREGISTERS);
        instGen(ins_ConstantNN);
    }
    else
    {
        GenTreePtr addr;
        if (tree->gtType == TYP_FLOAT || StackFPIsSameAsFloat(tree->gtDblCon.gtDconVal))
        {
            float f = forceCastToFloat(tree->gtDblCon.gtDconVal);
            addr    = genMakeConst(&f, TYP_FLOAT, tree, false);
        }
        else
        {
            addr = genMakeConst(&tree->gtDblCon.gtDconVal, tree->gtType, tree, true);
        }

        inst_FS_TT(INS_fld, addr);
    }

    return true;
}

// Function called at the end of every statement. For stack based x87 its mission is to
// remove any remaining temps on the stack.
void CodeGen::genEndOfStatement()
{
    unsigned i;

#ifdef DEBUG
    // Sanity check
    unsigned uTemps = 0;
    for (i = REG_FPV0; i < REG_FPCOUNT; i++)
    {
        if (compCurFPState.Mapped(i) &&                                      // register is mapped
            (genRegMaskFloat((regNumber)i) & regSet.rsMaskRegVarFloat) == 0) // but not enregistered
        {
            uTemps++;
        }
    }
    assert(uTemps <= 1);
#endif

    for (i = REG_FPV0; i < REG_FPCOUNT; i++)
    {
        if (compCurFPState.Mapped(i) &&                                      // register is mapped
            (genRegMaskFloat((regNumber)i) & regSet.rsMaskRegVarFloat) == 0) // but not enregistered
        {
            // remove register from stacks
            FlatFPX87_Unload(&compCurFPState, i);
        }
    }

    assert(ConsistentAfterStatementStackFP());
}

bool CodeGen::StackFPIsSameAsFloat(double d)
{
    if (forceCastToFloat(d) == d)
    {
        JITDUMP("StackFPIsSameAsFloat is true for value %lf\n", d);
        return true;
    }
    else
    {
        JITDUMP("StackFPIsSameAsFloat is false for value %lf\n", d);
    }

    return false;
}

GenTreePtr CodeGen::genMakeAddressableStackFP(GenTreePtr tree,
                                              regMaskTP* regMaskIntPtr,
                                              regMaskTP* regMaskFltPtr,
                                              bool       bCollapseConstantDoubles)
{
    *regMaskIntPtr = *regMaskFltPtr = 0;

    switch (tree->OperGet())
    {
        case GT_CNS_DBL:
            if (tree->gtDblCon.gtDconVal == 0.0 || tree->gtDblCon.gtDconVal == 1.0)
            {
                // For constants like 0 or 1 don't waste memory
                genCodeForTree(tree, 0);
                regSet.SetUsedRegFloat(tree, true);

                *regMaskFltPtr = genRegMaskFloat(tree->gtRegNum);
                return tree;
            }
            else
            {
                GenTreePtr addr;
                if (tree->gtType == TYP_FLOAT ||
                    (bCollapseConstantDoubles && StackFPIsSameAsFloat(tree->gtDblCon.gtDconVal)))
                {
                    float f = forceCastToFloat(tree->gtDblCon.gtDconVal);
                    addr    = genMakeConst(&f, TYP_FLOAT, tree, true);
                }
                else
                {
                    addr = genMakeConst(&tree->gtDblCon.gtDconVal, tree->gtType, tree, true);
                }
#ifdef DEBUG
                if (compiler->verbose)
                {
                    printf("Generated new constant in tree ");
                    Compiler::printTreeID(addr);
                    printf(" with value %lf\n", tree->gtDblCon.gtDconVal);
                }
#endif // DEBUG
                tree->CopyFrom(addr, compiler);
                return tree;
            }
            break;
        case GT_REG_VAR:
            // We take care about this in genKeepAddressableStackFP
            return tree;
        case GT_LCL_VAR:
        case GT_LCL_FLD:
        case GT_CLS_VAR:
            return tree;

        case GT_LEA:
            if (!genMakeIndAddrMode(tree, tree, false, 0, RegSet::KEEP_REG, regMaskIntPtr, false))
            {
                assert(false);
            }
            genUpdateLife(tree);
            return tree;

        case GT_IND:
            // Try to make the address directly addressable

            if (genMakeIndAddrMode(tree->gtOp.gtOp1, tree, false, 0, RegSet::KEEP_REG, regMaskIntPtr, false))
            {
                genUpdateLife(tree);
                return tree;
            }
            else
            {
                GenTreePtr addr = tree;
                tree            = tree->gtOp.gtOp1;

                genCodeForTree(tree, 0);
                regSet.rsMarkRegUsed(tree, addr);

                *regMaskIntPtr = genRegMask(tree->gtRegNum);
                return addr;
            }

        // fall through

        default:
            genCodeForTreeFloat(tree);
            regSet.SetUsedRegFloat(tree, true);

            // update mask
            *regMaskFltPtr = genRegMaskFloat(tree->gtRegNum);

            return tree;
            break;
    }
}

void CodeGen::genKeepAddressableStackFP(GenTreePtr tree, regMaskTP* regMaskIntPtr, regMaskTP* regMaskFltPtr)
{
    regMaskTP regMaskInt, regMaskFlt;

    regMaskInt = *regMaskIntPtr;
    regMaskFlt = *regMaskFltPtr;

    *regMaskIntPtr = *regMaskFltPtr = 0;

    switch (tree->OperGet())
    {
        case GT_REG_VAR:
            // If register has been spilled, unspill it
            if (tree->gtFlags & GTF_SPILLED)
            {
                UnspillFloat(&compiler->lvaTable[tree->gtLclVarCommon.gtLclNum]);
            }

            // If regvar is dying, take it out of the regvar mask
            if (tree->IsRegVarDeath())
            {
                genRegVarDeathStackFP(tree);
            }
            genUpdateLife(tree);

            return;
        case GT_CNS_DBL:
        {
            if (tree->gtFlags & GTF_SPILLED)
            {
                UnspillFloat(tree);
            }

            *regMaskFltPtr = genRegMaskFloat(tree->gtRegNum);

            return;
        }
        case GT_LCL_FLD:
        case GT_LCL_VAR:
        case GT_CLS_VAR:
            genUpdateLife(tree);
            return;
        case GT_IND:
        case GT_LEA:
            if (regMaskFlt)
            {
                // fall through
            }
            else
            {
                *regMaskIntPtr = genKeepAddressable(tree, regMaskInt, 0);
                *regMaskFltPtr = 0;
                return;
            }
        default:

            *regMaskIntPtr = 0;
            if (tree->gtFlags & GTF_SPILLED)
            {
                UnspillFloat(tree);
            }
            *regMaskFltPtr = genRegMaskFloat(tree->gtRegNum);
            return;
    }
}

void CodeGen::genDoneAddressableStackFP(GenTreePtr      tree,
                                        regMaskTP       addrRegInt,
                                        regMaskTP       addrRegFlt,
                                        RegSet::KeepReg keptReg)
{
    assert(!(addrRegInt && addrRegFlt));

    if (addrRegInt)
    {
        return genDoneAddressable(tree, addrRegInt, keptReg);
    }
    else if (addrRegFlt)
    {
        if (keptReg == RegSet::KEEP_REG)
        {
            for (unsigned i = REG_FPV0; i < REG_FPCOUNT; i++)
            {
                if (genRegMaskFloat((regNumber)i) & addrRegFlt)
                {
                    regSet.SetUsedRegFloat(tree, false);
                }
            }
        }
    }
}

void CodeGen::FlatFPX87_Kill(FlatFPStateX87* pState, unsigned uVirtual)
{
    JITDUMP("Killing %s\n", regVarNameStackFP((regNumber)uVirtual));

    assert(pState->TopVirtual() == uVirtual);
    pState->Pop();
}

void CodeGen::FlatFPX87_PushVirtual(FlatFPStateX87* pState, unsigned uRegister, bool bEmitCode)
{
    JITDUMP("Pushing %s to stack\n", regVarNameStackFP((regNumber)uRegister));

    pState->Push(uRegister);
}

unsigned CodeGen::FlatFPX87_Pop(FlatFPStateX87* pState, bool bEmitCode)
{
    assert(pState->m_uStackSize > 0);

    // Update state
    unsigned uVirtual = pState->Pop();

    // Emit instruction
    if (bEmitCode)
    {
        inst_FS(INS_fstp, 0);
    }

    return (uVirtual);
}

unsigned CodeGen::FlatFPX87_Top(FlatFPStateX87* pState, bool bEmitCode)
{
    return pState->TopVirtual();
}

void CodeGen::FlatFPX87_Unload(FlatFPStateX87* pState, unsigned uVirtual, bool bEmitCode)
{
    if (uVirtual != pState->TopVirtual())
    {
        // We will do an fstp to the right place

        // Update state
        unsigned uStack  = pState->m_uVirtualMap[uVirtual];
        unsigned uPhysic = pState->StackToST(uStack);

        pState->Unmap(uVirtual);
        pState->Associate(pState->TopVirtual(), uStack);
        pState->m_uStackSize--;

#ifdef DEBUG

        pState->m_uStack[pState->m_uStackSize] = (unsigned)-1;
#endif

        // Emit instruction
        if (bEmitCode)
        {
            inst_FS(INS_fstp, uPhysic);
        }
    }
    else
    {
        // Emit fstp
        FlatFPX87_Pop(pState, bEmitCode);
    }

    assert(pState->IsConsistent());
}

void CodeGenInterface::FlatFPX87_MoveToTOS(FlatFPStateX87* pState, unsigned uVirtual, bool bEmitCode)
{
    assert(!IsUninitialized(uVirtual));

    JITDUMP("Moving %s to top of stack\n", regVarNameStackFP((regNumber)uVirtual));

    if (uVirtual != pState->TopVirtual())
    {
        FlatFPX87_SwapStack(pState, pState->m_uVirtualMap[uVirtual], pState->TopIndex(), bEmitCode);
    }
    else
    {
        JITDUMP("%s already on the top of stack\n", regVarNameStackFP((regNumber)uVirtual));
    }

    assert(pState->IsConsistent());
}

void CodeGenInterface::FlatFPX87_SwapStack(FlatFPStateX87* pState, unsigned i, unsigned j, bool bEmitCode)
{
    assert(i != j);
    assert(i < pState->m_uStackSize);
    assert(j < pState->m_uStackSize);

    JITDUMP("Exchanging ST(%i) and ST(%i)\n", pState->StackToST(i), pState->StackToST(j));

    // issue actual swaps
    int iPhysic = pState->StackToST(i);
    int jPhysic = pState->StackToST(j);

    if (bEmitCode)
    {
        if (iPhysic == 0 || jPhysic == 0)
        {
            inst_FN(INS_fxch, iPhysic ? iPhysic : jPhysic);
        }
        else
        {
            inst_FN(INS_fxch, iPhysic);
            inst_FN(INS_fxch, jPhysic);
            inst_FN(INS_fxch, iPhysic);
        }
    }

    // Update State

    // Swap Register file
    pState->m_uVirtualMap[pState->m_uStack[i]] = j;
    pState->m_uVirtualMap[pState->m_uStack[j]] = i;

    // Swap stack
    int temp;
    temp                = pState->m_uStack[i];
    pState->m_uStack[i] = pState->m_uStack[j];
    pState->m_uStack[j] = temp;

    assert(pState->IsConsistent());
}

#ifdef DEBUG

void CodeGen::JitDumpFPState()
{
    int i;

    if ((regSet.rsMaskUsedFloat != 0) || (regSet.rsMaskRegVarFloat != 0))
    {
        printf("FPSTATE\n");
        printf("Used virtual registers: ");
        for (i = REG_FPV0; i < REG_FPCOUNT; i++)
        {
            if (genRegMaskFloat((regNumber)i) & regSet.rsMaskUsedFloat)
            {
                printf("FPV%i ", i);
            }
        }
        printf("\n");

        printf("virtual registers holding reg vars: ");
        for (i = REG_FPV0; i < REG_FPCOUNT; i++)
        {
            if (genRegMaskFloat((regNumber)i) & regSet.rsMaskRegVarFloat)
            {
                printf("FPV%i ", i);
            }
        }
        printf("\n");
    }
    compCurFPState.Dump();
}
#endif

//
//
//  Register allocation
//
struct ChangeToRegVarCallback
{
    unsigned  lclnum;
    regNumber reg;
};

void Compiler::raInitStackFP()
{
    // Reset local/reg interference
    for (int i = 0; i < REG_FPCOUNT; i++)
    {
        VarSetOps::AssignNoCopy(this, raLclRegIntfFloat[i], VarSetOps::MakeEmpty(this));
    }

    VarSetOps::AssignNoCopy(this, optAllFPregVars, VarSetOps::MakeEmpty(this));
    VarSetOps::AssignNoCopy(this, optAllNonFPvars, VarSetOps::MakeEmpty(this));
    VarSetOps::AssignNoCopy(this, optAllFloatVars, VarSetOps::MakeEmpty(this));

    raCntStkStackFP         = 0;
    raCntWtdStkDblStackFP   = 0;
    raCntStkParamDblStackFP = 0;

    VarSetOps::AssignNoCopy(this, raMaskDontEnregFloat, VarSetOps::MakeEmpty(this));

    // Calculate the set of all tracked FP/non-FP variables
    //  into compiler->optAllFloatVars and compiler->optAllNonFPvars
    unsigned   lclNum;
    LclVarDsc* varDsc;

    for (lclNum = 0, varDsc = lvaTable; lclNum < lvaCount; lclNum++, varDsc++)
    {
        /* Ignore the variable if it's not tracked */

        if (!varDsc->lvTracked)
            continue;

        /* Get hold of the index and the interference mask for the variable */

        unsigned varNum = varDsc->lvVarIndex;

        /* add to the set of all tracked FP/non-FP variables */

        if (varDsc->IsFloatRegType())
            VarSetOps::AddElemD(this, optAllFloatVars, varNum);
        else
            VarSetOps::AddElemD(this, optAllNonFPvars, varNum);
    }
}

#ifdef DEBUG
void Compiler::raDumpVariableRegIntfFloat()
{
    unsigned i;
    unsigned j;

    for (i = REG_FPV0; i < REG_FPCOUNT; i++)
    {
        if (!VarSetOps::IsEmpty(this, raLclRegIntfFloat[i]))
        {
            JITDUMP("FPV%u interferes with ", i);
            for (j = 0; j < lvaTrackedCount; j++)
            {
                assert(VarSetOps::IsEmpty(this, VarSetOps::Diff(this, raLclRegIntfFloat[i], optAllFloatVars)));

                if (VarSetOps::IsMember(this, raLclRegIntfFloat[i], j))
                {
                    JITDUMP("T%02u/V%02u, ", j, lvaTrackedToVarNum[j]);
                }
            }
            JITDUMP("\n");
        }
    }
}
#endif

// Returns the regnum for the variable passed as param takin in account
// the fpvar to register interference mask. If we can't find anything, we
// will return REG_FPNONE
regNumber Compiler::raRegForVarStackFP(unsigned varTrackedIndex)
{
    for (unsigned i = REG_FPV0; i < REG_FPCOUNT; i++)
    {
        if (!VarSetOps::IsMember(this, raLclRegIntfFloat[i], varTrackedIndex))
        {
            return (regNumber)i;
        }
    }

    return REG_FPNONE;
}

void Compiler::raAddPayloadStackFP(VARSET_VALARG_TP maskArg, unsigned weight)
{
    VARSET_TP VARSET_INIT_NOCOPY(mask, VarSetOps::Intersection(this, maskArg, optAllFloatVars));
    if (VarSetOps::IsEmpty(this, mask))
    {
        return;
    }

    for (unsigned i = 0; i < lvaTrackedCount; i++)
    {
        if (VarSetOps::IsMember(this, mask, i))
        {
            raPayloadStackFP[i] += weight;
        }
    }
}

bool Compiler::raVarIsGreaterValueStackFP(LclVarDsc* lv1, LclVarDsc* lv2)
{
    assert(lv1->lvTracked);
    assert(lv2->lvTracked);

    bool bSmall = (compCodeOpt() == SMALL_CODE);

    double weight1 = double(bSmall ? lv1->lvRefCnt : lv1->lvRefCntWtd) - double(raPayloadStackFP[lv1->lvVarIndex]) -
                     double(raHeightsStackFP[lv1->lvVarIndex][FP_VIRTUALREGISTERS]);

    double weight2 = double(bSmall ? lv2->lvRefCnt : lv2->lvRefCntWtd) - double(raPayloadStackFP[lv2->lvVarIndex]) -
                     double(raHeightsStackFP[lv2->lvVarIndex][FP_VIRTUALREGISTERS]);

    double diff = weight1 - weight2;

    if (diff)
    {
        return diff > 0 ? true : false;
    }
    else
    {
        return int(lv1->lvRefCnt - lv2->lvRefCnt) ? true : false;
    }
}

#ifdef DEBUG
// Dumps only interesting vars (the ones that are not enregistered yet
void Compiler::raDumpHeightsStackFP()
{
    unsigned i;
    unsigned j;

    JITDUMP("raDumpHeightsStackFP():\n");
    JITDUMP("--------------------------------------------------------\n");
    JITDUMP("Weighted Height Table Dump\n            ");
    for (i = 0; i < FP_VIRTUALREGISTERS; i++)
    {
        JITDUMP(" %i    ", i + 1);
    }

    JITDUMP("OVF\n");

    for (i = 0; i < lvaTrackedCount; i++)
    {
        if (VarSetOps::IsMember(this, optAllFloatVars, i) && !VarSetOps::IsMember(this, optAllFPregVars, i))
        {
            JITDUMP("V%02u/T%02u: ", lvaTrackedToVarNum[i], i);

            for (j = 0; j <= FP_VIRTUALREGISTERS; j++)
            {
                JITDUMP("%5u ", raHeightsStackFP[i][j]);
            }
            JITDUMP("\n");
        }
    }

    JITDUMP("\nNonweighted Height Table Dump\n            ");
    for (i = 0; i < FP_VIRTUALREGISTERS; i++)
    {
        JITDUMP(" %i    ", i + 1);
    }

    JITDUMP("OVF\n");

    for (i = 0; i < lvaTrackedCount; i++)
    {
        if (VarSetOps::IsMember(this, optAllFloatVars, i) && !VarSetOps::IsMember(this, optAllFPregVars, i))
        {
            JITDUMP("V%02u/T%02u: ", lvaTrackedToVarNum[i], i);

            for (j = 0; j <= FP_VIRTUALREGISTERS; j++)
            {
                JITDUMP("%5u ", raHeightsNonWeightedStackFP[i][j]);
            }
            JITDUMP("\n");
        }
    }
    JITDUMP("--------------------------------------------------------\n");
}
#endif

// Increases heights for tracked variables given in mask. We call this
// function when we enregister a variable and will cause the heights to
// shift one place to the right.
void Compiler::raUpdateHeightsForVarsStackFP(VARSET_VALARG_TP mask)
{
    assert(VarSetOps::IsSubset(this, mask, optAllFloatVars));

    for (unsigned i = 0; i < lvaTrackedCount; i++)
    {
        if (VarSetOps::IsMember(this, mask, i))
        {
            for (unsigned j = FP_VIRTUALREGISTERS; j > 0; j--)
            {
                raHeightsStackFP[i][j] = raHeightsStackFP[i][j - 1];

#ifdef DEBUG
                raHeightsNonWeightedStackFP[i][j] = raHeightsNonWeightedStackFP[i][j - 1];
#endif
            }

            raHeightsStackFP[i][0] = 0;
#ifdef DEBUG
            raHeightsNonWeightedStackFP[i][0] = 0;
#endif
        }
    }

#ifdef DEBUG
    raDumpHeightsStackFP();
#endif
}

// This is the prepass we do to adjust refcounts across calls and
// create the height structure.
void Compiler::raEnregisterVarsPrePassStackFP()
{
    BasicBlock* block;

    assert(!VarSetOps::IsEmpty(this, optAllFloatVars));

    // Initialization of the height table
    memset(raHeightsStackFP, 0, sizeof(raHeightsStackFP));

    // Initialization of the payload table
    memset(raPayloadStackFP, 0, sizeof(raPayloadStackFP));

#ifdef DEBUG
    memset(raHeightsNonWeightedStackFP, 0, sizeof(raHeightsStackFP));
#endif

    // We will have a quick table with the pointers to the interesting varDscs
    // so that we don't have to scan for them for each tree.
    unsigned FPVars[lclMAX_TRACKED];
    unsigned numFPVars = 0;
    for (unsigned i = 0; i < lvaTrackedCount; i++)
    {
        if (VarSetOps::IsMember(this, optAllFloatVars, i))
        {
            FPVars[numFPVars++] = i;
        }
    }

    assert(numFPVars == VarSetOps::Count(this, optAllFloatVars));

    // Things we check here:
    //
    // We substract 2 for each FP variable that's live across a call, as we will
    // have 2 memory accesses to spill and unpsill around it.
    //
    //
    //
    VARSET_TP VARSET_INIT_NOCOPY(blockLiveOutFloats, VarSetOps::MakeEmpty(this));
    for (block = fgFirstBB; block; block = block->bbNext)
    {
        compCurBB = block;
        /*
        This opt fails in the case of a variable that has it's entire lifetime contained in the 'then' of
        a qmark. The use mask for the whole qmark won't contain that variable as it variable's value comes
        from  a def in the else, and the def can't be set for the qmark if the else side of
        the qmark doesn't do a def.

        See VSW# 354454 for more info. Leaving the comment and code here just in case we try to be
        'smart' again in the future


        if (((block->bbVarUse |
              block->bbVarDef |
              block->bbLiveIn   ) & optAllFloatVars) == 0)
        {
            // Fast way out
            continue;
        }
        */
        VarSetOps::Assign(this, blockLiveOutFloats, block->bbLiveOut);
        VarSetOps::IntersectionD(this, blockLiveOutFloats, optAllFloatVars);
        if (!VarSetOps::IsEmpty(this, blockLiveOutFloats))
        {
            // See comment in compiler.h above declaration of compMayHaveTransitionBlocks
            // to understand the reason for this limitation of FP optimizer.
            switch (block->bbJumpKind)
            {
                case BBJ_COND:
                {
                    GenTreePtr stmt;
                    stmt = block->bbTreeList->gtPrev;
                    assert(stmt->gtNext == NULL && stmt->gtStmt.gtStmtExpr->gtOper == GT_JTRUE);

                    assert(stmt->gtStmt.gtStmtExpr->gtOp.gtOp1);
                    GenTreePtr cond = stmt->gtStmt.gtStmtExpr->gtOp.gtOp1;

                    assert(cond->OperIsCompare());

                    if (cond->gtOp.gtOp1->TypeGet() == TYP_LONG)
                    {
                        if (compHndBBtabCount > 0)
                        {
                            // If we have any handlers we won't enregister whatever is live out of this block
                            JITDUMP("PERF Warning: Taking out FP candidates due to transition blocks + exception "
                                    "handlers.\n");
                            VarSetOps::UnionD(this, raMaskDontEnregFloat,
                                              VarSetOps::Intersection(this, block->bbLiveOut, optAllFloatVars));
                        }
                        else
                        {
                            // long conditional jumps can generate transition bloks
                            compMayHaveTransitionBlocks = true;
                        }
                    }

                    break;
                }
                case BBJ_SWITCH:
                {
                    if (compHndBBtabCount > 0)
                    {
                        // If we have any handlers we won't enregister whatever is live out of this block
                        JITDUMP(
                            "PERF Warning: Taking out FP candidates due to transition blocks + exception handlers.\n");
                        VarSetOps::UnionD(this, raMaskDontEnregFloat,
                                          VarSetOps::Intersection(this, block->bbLiveOut, optAllFloatVars));
                    }
                    else
                    {
                        // fp vars are live out of the switch, so we may have transition blocks
                        compMayHaveTransitionBlocks = true;
                    }
                    break;
                    default:
                        break;
                }
            }
        }

        VARSET_TP VARSET_INIT(this, liveSet, block->bbLiveIn);
        for (GenTreePtr stmt = block->FirstNonPhiDef(); stmt; stmt = stmt->gtNext)
        {
            assert(stmt->gtOper == GT_STMT);

            unsigned prevHeight = stmt->gtStmt.gtStmtList->gtFPlvl;
            for (GenTreePtr tree = stmt->gtStmt.gtStmtList; tree; tree = tree->gtNext)
            {
                VarSetOps::AssignNoCopy(this, liveSet, fgUpdateLiveSet(liveSet, tree));
                switch (tree->gtOper)
                {
                    case GT_CALL:
                        raAddPayloadStackFP(liveSet, block->getBBWeight(this) * 2);
                        break;
                    case GT_CAST:
                        // For cast from long local var to double, decrement the ref count of the long
                        // to avoid store forwarding stall
                        if (tree->gtType == TYP_DOUBLE)
                        {
                            GenTreePtr op1 = tree->gtOp.gtOp1;
                            if (op1->gtOper == GT_LCL_VAR && op1->gtType == TYP_LONG)
                            {
                                unsigned int lclNum = op1->gtLclVarCommon.gtLclNum;
                                assert(lclNum < lvaCount);
                                LclVarDsc*   varDsc          = lvaTable + lclNum;
                                unsigned int weightedRefCnt  = varDsc->lvRefCntWtd;
                                unsigned int refCntDecrement = 2 * block->getBBWeight(this);
                                if (refCntDecrement > weightedRefCnt)
                                {
                                    varDsc->lvRefCntWtd = 0;
                                }
                                else
                                {
                                    varDsc->lvRefCntWtd = weightedRefCnt - refCntDecrement;
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }

                // Update heights
                unsigned height = tree->gtFPlvl;

                if (height != prevHeight)
                {
                    if (height > prevHeight && height < FP_VIRTUALREGISTERS)
                    {
                        for (unsigned i = 0; i < numFPVars; i++)
                        {
                            if (VarSetOps::IsMember(this, liveSet, FPVars[i]))
                            {
                                // The -1 are because we don't care about stack height 0
                                // and we will use offset FP_VIRTUALREGISTERS to know what's
                                // the count when we overflow. we multiply by 2, because that
                                // is the number of memory accesses we will do for each spill
                                // (even if we op directly with the spill)
                                if (compCodeOpt() == SMALL_CODE)
                                {
                                    raHeightsStackFP[FPVars[i]][height - 1] += 2;
                                }
                                else
                                {
                                    raHeightsStackFP[FPVars[i]][height - 1] += 2 * block->getBBWeight(this);
                                }

#ifdef DEBUG
                                raHeightsNonWeightedStackFP[FPVars[i]][height - 1]++;
#endif
                            }
                        }
                    }

                    prevHeight = height;
                }
            }
        }
    }
    compCurBB = NULL;

    if (compJmpOpUsed)
    {
        // Disable enregistering of FP vars for methods with jmp op. We have really no
        // coverage here.
        // The problem with FP enreg vars is that the returning block is marked with having
        // all variables live on exit. This works for integer vars, but for FP vars we must
        // do the work to unload them. This is fairly straightforward to do, but I'm worried
        // by the coverage, so I'll take the conservative aproach of disabling FP enregistering
        // and we will fix it if there is demand
        JITDUMP("PERF Warning: Disabling FP enregistering due to JMP op!!!!!!!.\n");
        VarSetOps::UnionD(this, raMaskDontEnregFloat, optAllFloatVars);
    }

#ifdef DEBUG
    raDumpHeightsStackFP();
#endif
}

void Compiler::raSetRegLclBirthDeath(GenTreePtr tree, VARSET_VALARG_TP lastlife, bool fromLDOBJ)
{
    assert(tree->gtOper == GT_LCL_VAR);

    unsigned lclnum = tree->gtLclVarCommon.gtLclNum;
    assert(lclnum < lvaCount);

    LclVarDsc* varDsc = lvaTable + lclnum;

    if (!varDsc->lvTracked)
    {
        // Not tracked, can't be one of the enreg fp vars
        return;
    }

    unsigned varIndex = varDsc->lvVarIndex;

    if (!VarSetOps::IsMember(this, optAllFPregVars, varIndex))
    {
        // Not one of the enreg fp vars
        return;
    }

    assert(varDsc->lvRegNum != REG_FPNONE);
    assert(!VarSetOps::IsMember(this, raMaskDontEnregFloat, varIndex));

    unsigned livenessFlags = (tree->gtFlags & GTF_LIVENESS_MASK);
    tree->ChangeOper(GT_REG_VAR);
    tree->gtFlags |= livenessFlags;
    tree->gtRegNum          = varDsc->lvRegNum;
    tree->gtRegVar.gtRegNum = varDsc->lvRegNum;
    tree->gtRegVar.SetLclNum(lclnum);

    // A liveset can change in a lclvar even if the lclvar itself is not
    // changing its life. This can happen for lclvars inside qmarks,
    // where lclvars die across the colon edge.
    // SO, either
    //     it is marked GTF_VAR_DEATH (already set by fgComputeLife)
    //     OR it is already live
    //     OR it is becoming live
    //
    if ((tree->gtFlags & GTF_VAR_DEATH) == 0)
    {
        if ((tree->gtFlags & GTF_VAR_DEF) != 0)

        {
            tree->gtFlags |= GTF_REG_BIRTH;
        }
    }

#ifdef DEBUG
    if (verbose)
        gtDispTree(tree);
#endif
}

// In this pass we set the regvars and set the birth and death flags. we do it
// for all enregistered variables at once.
void Compiler::raEnregisterVarsPostPassStackFP()
{
    if (VarSetOps::IsEmpty(this, optAllFPregVars))
    {
        // Nothing to fix up.
    }

    BasicBlock* block;

    JITDUMP("raEnregisterVarsPostPassStackFP:\n");

    for (block = fgFirstBB; block; block = block->bbNext)
    {
        compCurBB = block;

        /*
        This opt fails in the case of a variable that has it's entire lifetime contained in the 'then' of
        a qmark. The use mask for the whole qmark won't contain that variable as it variable's value comes
        from  a def in the else, and the def can't be set for the qmark if the else side of
        the qmark doesn't do a def.

        See VSW# 354454 for more info. Leaving the comment and code here just in case we try to be
        'smart' again in the future



        if (((block->bbVarUse |
              block->bbVarDef |
              block->bbLiveIn   ) & optAllFPregVars) == 0)
        {
            // Fast way out
            continue;
        }
        */

        VARSET_TP VARSET_INIT(this, lastlife, block->bbLiveIn);
        for (GenTreePtr stmt = block->FirstNonPhiDef(); stmt; stmt = stmt->gtNext)
        {
            assert(stmt->gtOper == GT_STMT);

            for (GenTreePtr tree = stmt->gtStmt.gtStmtList; tree;
                 VarSetOps::AssignNoCopy(this, lastlife, fgUpdateLiveSet(lastlife, tree)), tree = tree->gtNext)
            {
                if (tree->gtOper == GT_LCL_VAR)
                {
                    raSetRegLclBirthDeath(tree, lastlife, false);
                }
            }
        }
        assert(VarSetOps::Equal(this, lastlife, block->bbLiveOut));
    }
    compCurBB = NULL;
}

void Compiler::raGenerateFPRefCounts()
{
    // Update ref counts to stack
    assert(raCntWtdStkDblStackFP == 0);
    assert(raCntStkParamDblStackFP == 0);
    assert(raCntStkStackFP == 0);

    LclVarDsc* varDsc;
    unsigned   lclNum;
    for (lclNum = 0, varDsc = lvaTable; lclNum < lvaCount; lclNum++, varDsc++)
    {
        if (varDsc->lvType == TYP_DOUBLE ||
            varDsc->lvStructDoubleAlign) // Account for structs (A bit over aggressive here, we should
                                         // account for field accesses, but should be a reasonable
                                         // heuristic).
        {
            if (varDsc->lvRegister)
            {
                assert(varDsc->lvTracked);
            }
            else
            {
                // Increment tmp access
                raCntStkStackFP += varDsc->lvRefCnt;

                if (varDsc->lvIsParam)
                {
                    // Why is this not weighted?
                    raCntStkParamDblStackFP += varDsc->lvRefCnt;
                }
                else
                {
                    raCntWtdStkDblStackFP += varDsc->lvRefCntWtd;
                }
            }
        }
    }

#ifdef DEBUG
    if ((raCntWtdStkDblStackFP > 0) || (raCntStkParamDblStackFP > 0))
    {
        JITDUMP("StackFP double stack weighted ref count: %u ; param ref count: %u\n", raCntWtdStkDblStackFP,
                raCntStkParamDblStackFP);
    }
#endif
}

void Compiler::raEnregisterVarsStackFP()
{
    const int          FPENREGTHRESHOLD          = 1;
    const unsigned int FPENREGTHRESHOLD_WEIGHTED = FPENREGTHRESHOLD;

    // Do init
    raInitStackFP();

    if (opts.compDbgCode || opts.MinOpts())
    {
        // no enregistering for these options.
        return;
    }

    if (VarSetOps::IsEmpty(this, optAllFloatVars))
    {
        // No floating point vars. bail out
        return;
    }

    // Do additional pass updating weights and generating height table
    raEnregisterVarsPrePassStackFP();

    // Vars are ordered by weight
    LclVarDsc* varDsc;

    // Set an interference with V0 and V1, which we reserve as a temp registers.
    // We need only one temp. but we will take the easy way, as by using
    // two, we will need to teach codegen how to operate with spilled variables
    VarSetOps::Assign(this, raLclRegIntfFloat[REG_FPV0], optAllFloatVars);
    VarSetOps::Assign(this, raLclRegIntfFloat[REG_FPV1], optAllFloatVars);

#ifdef DEBUG
    if (codeGen->genStressFloat())
    {
        // Lock out registers for stress.
        regMaskTP locked = codeGen->genStressLockedMaskFloat();
        for (unsigned i = REG_FPV0; i < REG_FPCOUNT; i++)
        {
            if (locked & genRegMaskFloat((regNumber)i))
            {
                VarSetOps::Assign(this, raLclRegIntfFloat[i], optAllFloatVars);
            }
        }
    }
#endif

    // Build the interesting FP var table
    LclVarDsc* fpLclFPVars[lclMAX_TRACKED];
    unsigned   numFPVars = 0;
    for (unsigned i = 0; i < lvaTrackedCount; i++)
    {
        if (VarSetOps::IsMember(this, raMaskDontEnregFloat, i))
        {
            JITDUMP("Won't enregister V%02i (T%02i) because it's marked as dont enregister\n", lvaTrackedToVarNum[i],
                    i);
            continue;
        }

        if (VarSetOps::IsMember(this, optAllFloatVars, i))
        {
            varDsc = lvaTable + lvaTrackedToVarNum[i];

            assert(varDsc->lvTracked);

            if (varDsc->lvDoNotEnregister)
            {
                JITDUMP("Won't enregister V%02i (T%02i) because it's marked as DoNotEnregister\n",
                        lvaTrackedToVarNum[i], i);
                continue;
            }
#if !FEATURE_X87_DOUBLES
            if (varDsc->TypeGet() == TYP_FLOAT)
            {
                JITDUMP("Won't enregister V%02i (T%02i) because it's a TYP_FLOAT and we have disabled "
                        "FEATURE_X87_DOUBLES\n",
                        lvaTrackedToVarNum[i], i);
                continue;
            }
#endif

            fpLclFPVars[numFPVars++] = lvaTable + lvaTrackedToVarNum[i];
        }
    }

    unsigned maxRegVars = 0; // Max num of regvars at one time

    for (unsigned sortNum = 0; sortNum < numFPVars; sortNum++)
    {
#ifdef DEBUG
        {
            JITDUMP("\n");
            JITDUMP("FP regvar candidates:\n");

            for (unsigned i = sortNum; i < numFPVars; i++)
            {
                varDsc          = fpLclFPVars[i];
                unsigned lclNum = varDsc - lvaTable;
                unsigned varIndex;
                varIndex = varDsc->lvVarIndex;

                JITDUMP("V%02u/T%02u RefCount: %u Weight: %u ; Payload: %u ; Overflow: %u\n", lclNum, varIndex,
                        varDsc->lvRefCnt, varDsc->lvRefCntWtd, raPayloadStackFP[varIndex],
                        raHeightsStackFP[varIndex][FP_VIRTUALREGISTERS]);
            }
            JITDUMP("\n");
        }
#endif

        unsigned min = sortNum;

        // Find the one that will save us most
        for (unsigned i = sortNum + 1; i < numFPVars; i++)
        {
            if (raVarIsGreaterValueStackFP(fpLclFPVars[i], fpLclFPVars[sortNum]))
            {
                min = i;
            }
        }

        // Put it at the top of the array
        LclVarDsc* temp;
        temp                 = fpLclFPVars[min];
        fpLclFPVars[min]     = fpLclFPVars[sortNum];
        fpLclFPVars[sortNum] = temp;

        varDsc = fpLclFPVars[sortNum];

#ifdef DEBUG
        unsigned lclNum = varDsc - lvaTable;
#endif
        unsigned varIndex = varDsc->lvVarIndex;

        assert(VarSetOps::IsMember(this, optAllFloatVars, varIndex));

        JITDUMP("Candidate for enregistering: V%02u/T%02u RefCount: %u Weight: %u ; Payload: %u ; Overflow: %u\n",
                lclNum, varIndex, varDsc->lvRefCnt, varDsc->lvRefCntWtd, raPayloadStackFP[varIndex],
                raHeightsStackFP[varIndex][FP_VIRTUALREGISTERS]);

        bool bMeetsThreshold = true;

        if (varDsc->lvRefCnt < FPENREGTHRESHOLD || varDsc->lvRefCntWtd < FPENREGTHRESHOLD_WEIGHTED)
        {
            bMeetsThreshold = false;
        }

        // We don't want to enregister arguments with only one use, as they will be
        // loaded in the prolog. Just don't enregister them and load them lazily(
        if (varDsc->lvIsParam &&
            (varDsc->lvRefCnt <= FPENREGTHRESHOLD || varDsc->lvRefCntWtd <= FPENREGTHRESHOLD_WEIGHTED))
        {
            bMeetsThreshold = false;
        }

        if (!bMeetsThreshold
#ifdef DEBUG
            && codeGen->genStressFloat() != 1
#endif
            )
        {
            // Doesn't meet bar, do next
            JITDUMP("V%02u/T%02u doesnt meet threshold. Won't enregister\n", lclNum, varIndex);
            continue;
        }

        // We don't want to have problems with overflow (we now have 2 unsigned counters
        // that can possibly go to their limits), so we just promote to double here.
        // diff
        double balance =
            double(varDsc->lvRefCntWtd) -
            double(raPayloadStackFP[varIndex]) -                      // Additional costs of enregistering variable
            double(raHeightsStackFP[varIndex][FP_VIRTUALREGISTERS]) - // Spilling costs of enregistering variable
            double(FPENREGTHRESHOLD_WEIGHTED);

        JITDUMP("balance = %d - %d - %d - %d\n", varDsc->lvRefCntWtd, raPayloadStackFP[varIndex],
                raHeightsStackFP[varIndex][FP_VIRTUALREGISTERS], FPENREGTHRESHOLD_WEIGHTED);

        if (balance < 0.0
#ifdef DEBUG
            && codeGen->genStressFloat() != 1
#endif
            )
        {
            // Doesn't meet bar, do next
            JITDUMP("V%02u/T%02u doesnt meet threshold. Won't enregister\n", lclNum, varIndex);
            continue;
        }

        regNumber reg = raRegForVarStackFP(varDsc->lvVarIndex);
        if (reg == REG_FPNONE)
        {
            // Didn't make if (interferes with other regvars), do next
            JITDUMP("V%02u/T%02u interferes with other enreg vars. Won't enregister\n", lclNum, varIndex);

            continue;
        }

        if (lvaIsFieldOfDependentlyPromotedStruct(varDsc))
        {
            // Do not enregister if this is a floating field in a struct local of
            // promotion type PROMOTION_TYPE_DEPENDENT.
            continue;
        }

        // Yipee, we will enregister var.
        varDsc->lvRegister = true;
        varDsc->lvRegNum   = reg;
        VarSetOps::AddElemD(this, optAllFPregVars, varIndex);

#ifdef DEBUG
        raDumpVariableRegIntfFloat();

        if (verbose)
        {
            printf("; ");
            gtDispLclVar(lclNum);
            printf("V%02u/T%02u (refcnt=%2u,refwtd=%4u%s) enregistered in %s\n", varIndex, varDsc->lvVarIndex,
                   varDsc->lvRefCnt, varDsc->lvRefCntWtd / 2, (varDsc->lvRefCntWtd & 1) ? ".5" : "",
                   CodeGen::regVarNameStackFP(varDsc->lvRegNum));
        }

        JITDUMP("\n");
#endif

        // Create interferences with other variables.
        assert(VarSetOps::IsEmpty(this, VarSetOps::Diff(this, raLclRegIntfFloat[(int)reg], optAllFloatVars)));
        VARSET_TP VARSET_INIT_NOCOPY(intfFloats, VarSetOps::Intersection(this, lvaVarIntf[varIndex], optAllFloatVars));

        VarSetOps::UnionD(this, raLclRegIntfFloat[reg], intfFloats);

        // Update height tables for variables that interfere with this one.
        raUpdateHeightsForVarsStackFP(intfFloats);

        // Update max number of reg vars at once.
        maxRegVars = min(REG_FPCOUNT, max(maxRegVars, VarSetOps::Count(this, intfFloats)));
    }

    assert(VarSetOps::IsSubset(this, optAllFPregVars, optAllFloatVars));
    assert(VarSetOps::IsEmpty(this, VarSetOps::Intersection(this, optAllFPregVars, raMaskDontEnregFloat)));

    // This is a bit conservative, as they may not all go through a call.
    // If we have to, we can fix this.
    tmpDoubleSpillMax += maxRegVars;

    // Do pass marking trees as egvars
    raEnregisterVarsPostPassStackFP();

#ifdef DEBUG
    {
        JITDUMP("FP enregistration summary\n");

        unsigned i;
        for (i = 0; i < numFPVars; i++)
        {
            varDsc = fpLclFPVars[i];

            if (varDsc->lvRegister)
            {
                unsigned lclNum = varDsc - lvaTable;
                unsigned varIndex;
                varIndex = varDsc->lvVarIndex;

                JITDUMP("Enregistered V%02u/T%02u in FPV%i RefCount: %u Weight: %u \n", lclNum, varIndex,
                        varDsc->lvRegNum, varDsc->lvRefCnt, varDsc->lvRefCntWtd);
            }
        }
        JITDUMP("End of FP enregistration summary\n\n");
    }
#endif
}

#ifdef DEBUG

regMaskTP CodeGenInterface::genStressLockedMaskFloat()
{
    assert(genStressFloat());

    // Don't use REG_FPV0 or REG_FPV1, they're reserved
    if (genStressFloat() == 1)
    {
        return genRegMaskFloat(REG_FPV4) | genRegMaskFloat(REG_FPV5) | genRegMaskFloat(REG_FPV6) |
               genRegMaskFloat(REG_FPV7);
    }
    else
    {
        return genRegMaskFloat(REG_FPV2) | genRegMaskFloat(REG_FPV3) | genRegMaskFloat(REG_FPV4) |
               genRegMaskFloat(REG_FPV5) | genRegMaskFloat(REG_FPV6) | genRegMaskFloat(REG_FPV7);
    }
}

#endif

#endif // FEATURE_STACK_FP_X87

#endif // LEGACY_BACKEND
