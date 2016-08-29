// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// This file contains the members of CodeGen that are defined and used
// only by the "classic" JIT backend.  It is included by CodeGen.h in the
// definition of the CodeGen class.
//

#ifndef _CODEGENCLASSIC_H_
#define _CODEGENCLASSIC_H_

#ifdef LEGACY_BACKEND // Not necessary (it's this way in the #include location), but helpful to IntelliSense

public:
regNumber genIsEnregisteredIntVariable(GenTreePtr tree);

void sched_AM(instruction ins,
              emitAttr    size,
              regNumber   ireg,
              bool        rdst,
              GenTreePtr  tree,
              unsigned    offs,
              bool        cons  = false,
              int         cval  = 0,
              insFlags    flags = INS_FLAGS_DONT_CARE);

protected:
#if FEATURE_STACK_FP_X87
VARSET_TP genFPregVars;    // mask corresponding to genFPregCnt
unsigned  genFPdeadRegCnt; // The dead unpopped part of genFPregCnt
#endif                     // FEATURE_STACK_FP_X87

//-------------------------------------------------------------------------

void genSetRegToIcon(regNumber reg, ssize_t val, var_types type = TYP_INT, insFlags flags = INS_FLAGS_DONT_CARE);

regNumber genGetRegSetToIcon(ssize_t val, regMaskTP regBest = 0, var_types type = TYP_INT);
void genDecRegBy(regNumber reg, ssize_t ival, GenTreePtr tree);

void genMulRegBy(regNumber reg, ssize_t ival, GenTreePtr tree, var_types dstType = TYP_INT, bool ovfl = false);

//-------------------------------------------------------------------------

bool genRegTrashable(regNumber reg, GenTreePtr tree);

//
// Prolog functions and data (there are a few exceptions for more generally used things)
//

regMaskTP genPInvokeMethodProlog(regMaskTP initRegs);

void genPInvokeMethodEpilog();

regNumber genPInvokeCallProlog(LclVarDsc*            varDsc,
                               int                   argSize,
                               CORINFO_METHOD_HANDLE methodToken,
                               BasicBlock*           returnLabel);

void genPInvokeCallEpilog(LclVarDsc* varDsc, regMaskTP retVal);

regNumber genLclHeap(GenTreePtr size);

void genSinglePush();

void genSinglePop();

void genDyingVars(VARSET_VALARG_TP beforeSet, VARSET_VALARG_TP afterSet);

bool genContainsVarDeath(GenTreePtr from, GenTreePtr to, unsigned varNum);

void genComputeReg(
    GenTreePtr tree, regMaskTP needReg, RegSet::ExactReg mustReg, RegSet::KeepReg keepReg, bool freeOnly = false);

void genCompIntoFreeReg(GenTreePtr tree, regMaskTP needReg, RegSet::KeepReg keepReg);

void genReleaseReg(GenTreePtr tree);

void genRecoverReg(GenTreePtr tree, regMaskTP needReg, RegSet::KeepReg keepReg);

void genMoveRegPairHalf(GenTreePtr tree, regNumber dst, regNumber src, int off = 0);

void genMoveRegPair(GenTreePtr tree, regMaskTP needReg, regPairNo newPair);

void genComputeRegPair(
    GenTreePtr tree, regPairNo needRegPair, regMaskTP avoidReg, RegSet::KeepReg keepReg, bool freeOnly = false);

void genCompIntoFreeRegPair(GenTreePtr tree, regMaskTP avoidReg, RegSet::KeepReg keepReg);

void genComputeAddressable(GenTreePtr      tree,
                           regMaskTP       addrReg,
                           RegSet::KeepReg keptReg,
                           regMaskTP       needReg,
                           RegSet::KeepReg keepReg,
                           bool            freeOnly = false);

void genReleaseRegPair(GenTreePtr tree);

void genRecoverRegPair(GenTreePtr tree, regPairNo regPair, RegSet::KeepReg keepReg);

void genEvalIntoFreeRegPair(GenTreePtr tree, regPairNo regPair, regMaskTP avoidReg);

void genMakeRegPairAvailable(regPairNo regPair);

bool genMakeIndAddrMode(GenTreePtr      addr,
                        GenTreePtr      oper,
                        bool            forLea,
                        regMaskTP       regMask,
                        RegSet::KeepReg keepReg,
                        regMaskTP*      useMaskPtr,
                        bool            deferOp = false);

regMaskTP genMakeRvalueAddressable(
    GenTreePtr tree, regMaskTP needReg, RegSet::KeepReg keepReg, bool forLoadStore, bool smallOK = false);

regMaskTP genMakeAddressable(
    GenTreePtr tree, regMaskTP needReg, RegSet::KeepReg keepReg, bool smallOK = false, bool deferOK = false);

regMaskTP genMakeAddrArrElem(GenTreePtr arrElem, GenTreePtr tree, regMaskTP needReg, RegSet::KeepReg keepReg);

regMaskTP genMakeAddressable2(GenTreePtr      tree,
                              regMaskTP       needReg,
                              RegSet::KeepReg keepReg,
                              bool            forLoadStore,
                              bool            smallOK      = false,
                              bool            deferOK      = false,
                              bool            evalSideEffs = false);

bool genStillAddressable(GenTreePtr tree);

regMaskTP genRestoreAddrMode(GenTreePtr addr, GenTreePtr tree, bool lockPhase);

regMaskTP genRestAddressable(GenTreePtr tree, regMaskTP addrReg, regMaskTP lockMask);

regMaskTP genKeepAddressable(GenTreePtr tree, regMaskTP addrReg, regMaskTP avoidMask = RBM_NONE);

void genDoneAddressable(GenTreePtr tree, regMaskTP addrReg, RegSet::KeepReg keptReg);

GenTreePtr genMakeAddrOrFPstk(GenTreePtr tree, regMaskTP* regMaskPtr, bool roundResult);

void genEmitGSCookieCheck(bool pushReg);

void genEvalSideEffects(GenTreePtr tree);

void genCondJump(GenTreePtr cond, BasicBlock* destTrue = NULL, BasicBlock* destFalse = NULL, bool bStackFPFixup = true);

emitJumpKind genCondSetFlags(GenTreePtr cond);

void genJCC(genTreeOps cmp, BasicBlock* block, var_types type);

void genJccLongHi(genTreeOps cmp, BasicBlock* jumpTrue, BasicBlock* jumpFalse, bool unsOper = false);

void genJccLongLo(genTreeOps cmp, BasicBlock* jumpTrue, BasicBlock* jumpFalse);

void genCondJumpLng(GenTreePtr cond, BasicBlock* jumpTrue, BasicBlock* jumpFalse, bool bFPTransition = false);

bool genUse_fcomip();

void genTableSwitch(regNumber reg, unsigned jumpCnt, BasicBlock** jumpTab);

regMaskTP WriteBarrier(GenTreePtr tgt, GenTreePtr assignVal, regMaskTP addrReg);

void genCodeForTreeConst(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg = RBM_NONE);

void genCodeForTreeLeaf(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg = RBM_NONE);

// If "tree" is a comma node, generates code for the left comma arguments,
// in order, returning the first right argument in the list that is not
// a comma node.
GenTreePtr genCodeForCommaTree(GenTreePtr tree);

void genCodeForTreeLeaf_GT_JMP(GenTreePtr tree);

static Compiler::fgWalkPreFn fgIsVarAssignedTo;

void genCodeForQmark(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg);

bool genCodeForQmarkWithCMOV(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg);

#ifdef _TARGET_XARCH_
void genCodeForMultEAX(GenTreePtr tree);
#endif
#ifdef _TARGET_ARM_
void genCodeForMult64(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg);
#endif

void genCodeForTreeSmpBinArithLogOp(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg);

void genCodeForTreeSmpBinArithLogAsgOp(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg);

void genCodeForUnsignedMod(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg);

void genCodeForSignedMod(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg);

void genCodeForUnsignedDiv(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg);

void genCodeForSignedDiv(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg);

void genCodeForGeneralDivide(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg);

void genCodeForAsgShift(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg);

void genCodeForShift(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg);

void genCodeForRelop(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg);

void genCodeForCopyObj(GenTreePtr tree, regMaskTP destReg);

void genCodeForBlkOp(GenTreePtr tree, regMaskTP destReg);

void genCodeForTreeSmpOp(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg = RBM_NONE);

regNumber genIntegerCast(GenTree* tree, regMaskTP needReg, regMaskTP bestReg);

void genCodeForNumericCast(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg);

void genCodeForTreeSmpOp_GT_ADDR(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg = RBM_NONE);

void genCodeForTreeSmpOpAsg(GenTreePtr tree);

void genCodeForTreeSmpOpAsg_DONE_ASSG(GenTreePtr tree, regMaskTP addrReg, regNumber reg, bool ovfl);

void genCodeForTreeSpecialOp(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg = RBM_NONE);

void genCodeForTree(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg = RBM_NONE);

void genCodeForTree_DONE_LIFE(GenTreePtr tree, regNumber reg)
{
    /* We've computed the value of 'tree' into 'reg' */

    assert(reg != 0xFEEFFAAFu);
    assert(!IsUninitialized(reg));

    genMarkTreeInReg(tree, reg);
}

void genCodeForTree_DONE_LIFE(GenTreePtr tree, regPairNo regPair)
{
    /* We've computed the value of 'tree' into 'regPair' */

    genMarkTreeInRegPair(tree, regPair);
}

void genCodeForTree_DONE(GenTreePtr tree, regNumber reg)
{
    /* Check whether this subtree has freed up any variables */

    genUpdateLife(tree);

    genCodeForTree_DONE_LIFE(tree, reg);
}

void genCodeForTree_REG_VAR1(GenTreePtr tree)
{
    /* Value is already in a register */

    regNumber reg = tree->gtRegNum;

    gcInfo.gcMarkRegPtrVal(reg, tree->TypeGet());

    genCodeForTree_DONE(tree, reg);
}

void genCodeForTreeLng(GenTreePtr tree, regMaskTP needReg, regMaskTP avoidReg);

regPairNo genCodeForLongModInt(GenTreePtr tree, regMaskTP needReg);

unsigned genRegCountForLiveIntEnregVars(GenTreePtr tree);

#ifdef _TARGET_ARM_
void genStoreFromFltRetRegs(GenTreePtr tree);
void genLoadIntoFltRetRegs(GenTreePtr tree);
void genLdStFltRetRegsPromotedVar(LclVarDsc* varDsc, bool isLoadIntoFltReg);
#endif

#if CPU_HAS_FP_SUPPORT
void genRoundFpExpression(GenTreePtr op, var_types type = TYP_UNDEF);
void genCodeForTreeFlt(GenTreePtr tree, regMaskTP needReg = RBM_ALLFLOAT, regMaskTP bestReg = RBM_NONE);
#endif

// FP stuff
#include "fp.h"

void genCodeForJumpTable(GenTreePtr tree);
void genCodeForSwitchTable(GenTreePtr tree);
void genCodeForSwitch(GenTreePtr tree);

regMaskTP genPushRegs(regMaskTP regs, regMaskTP* byrefRegs, regMaskTP* noRefRegs);
void genPopRegs(regMaskTP regs, regMaskTP byrefRegs, regMaskTP noRefRegs);

size_t genPushArgList(GenTreePtr call);

#ifdef _TARGET_ARM_
// We are generating code for a promoted struct local variable.  Fill the next slot (register or
// 4-byte stack slot) with one or more field variables of the promoted struct local -- or 2 such slots
// if the next field is a 64-bit value.
// The arguments are:
//    "arg" is the current argument node.
//
//    "curArgTabEntry" arg table entry pointer for "arg".
//
//    "promotedStructLocalVarDesc" describes the struct local being copied, assumed non-NULL.
//
//    "fieldSize" is somewhat misnamed; it must be the element in the struct's GC layout describing the next slot
//       of the struct -- it will be EA_4BYTE, EA_GCREF, or EA_BYREF.
//
//    "*pNextPromotedStructFieldVar" must be the the local variable number of the next field variable to copy;
//       this location will be updated by the call to reflect the bytes that are copied.
//
//    "*pBytesOfNextSlotOfCurPromotedStruct" must be the number of bytes within the struct local at which the next
//       slot to be copied starts.  This location will be updated by the call to reflect the bytes that are copied.
//
//    "*pCurRegNum" must be the current argument register number, and will be updated if argument registers are filled.
//
//    "argOffset" must be the offset of the next slot to be filled in the outgoing argument area, if the argument is to
//    be
//       put in the outgoing arg area of the stack (or else should be INT_MAX if the next slot to be filled is a
//       register).
//       (Strictly speaking, after the addition of "argOffsetOfFirstStackSlot", this arg is redundant, and is only used
//       in assertions, and could be removed.)
//
//    "fieldOffsetOfFirstStackSlot" must be the offset within the promoted struct local of the first slot that should be
//       copied to the outgoing argument area -- non-zero only in the case of a struct that spans registers and stack
//       slots.
//
//    "argOffsetOfFirstStackSlot" must be the 4-byte-aligned offset of the first offset in the outgoing argument area
//    which could
//       contain part of the struct.  (Explicit alignment may mean it doesn't actually contain part of the struct.)
//
//    "*deadFieldVarRegs" is an out parameter, the set of registers containing promoted field variables that become dead
//    after
//       this (implicit) use.
//
//    "*pRegTmp" -- if a temporary register is needed, and this is not REG_STK, uses that register.  Otherwise, if it is
//    REG_STK,
//       allocates a register, uses it, and sets "*pRegTmp" to the allocated register.
//
// Returns "true" iff it filled two slots with an 8-byte value.
bool genFillSlotFromPromotedStruct(GenTreePtr       arg,
                                   fgArgTabEntryPtr curArgTabEntry,
                                   LclVarDsc*       promotedStructLocalVarDesc,
                                   emitAttr         fieldSize,
                                   unsigned*        pNextPromotedStructFieldVar,         // IN/OUT
                                   unsigned*        pBytesOfNextSlotOfCurPromotedStruct, // IN/OUT
                                   regNumber*       pCurRegNum,                          // IN/OUT
                                   int              argOffset,
                                   int              fieldOffsetOfFirstStackSlot,
                                   int              argOffsetOfFirstStackSlot,
                                   regMaskTP*       deadFieldVarRegs, // OUT
                                   regNumber*       pRegTmp);         // IN/OUT

#endif // _TARGET_ARM_
// Requires that "curr" is a cpblk.  If the RHS is a promoted struct local,
// then returns a regMaskTP representing the set of registers holding
// fieldVars of the RHS that go dead with this use (as determined by the live set
// of cpBlk).
regMaskTP genFindDeadFieldRegs(GenTreePtr cpBlk);

void SetupLateArgs(GenTreePtr call);

#ifdef _TARGET_ARM_
void PushMkRefAnyArg(GenTreePtr mkRefAnyTree, fgArgTabEntryPtr curArgTabEntry, regMaskTP regNeedMask);
#endif // _TARGET_ARM_

regMaskTP genLoadIndirectCallTarget(GenTreePtr call);

regMaskTP genCodeForCall(GenTreePtr call, bool valUsed);

GenTreePtr genGetAddrModeBase(GenTreePtr tree);

GenTreePtr genIsAddrMode(GenTreePtr tree, GenTreePtr* indxPtr);

private:
bool genIsLocalLastUse(GenTreePtr tree);

bool genIsRegCandidateLocal(GenTreePtr tree);

//=========================================================================
//  Debugging support
//=========================================================================

#if FEATURE_STACK_FP_X87
/*
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                   Flat FP model                                           XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

bool StackFPIsSameAsFloat(double d);
bool FlatFPSameRegisters(FlatFPStateX87* pState, regMaskTP mask);

// FlatFPStateX87_ functions are the actual verbs to do stuff
// like doing a transition, loading   register, etc. It's also
// responsible for emitting the x87 code to do so. We keep
// them in Compiler because we don't want to store a pointer to the
// emitter.
void FlatFPX87_Kill(FlatFPStateX87* pState, unsigned iVirtual);
void FlatFPX87_PushVirtual(FlatFPStateX87* pState, unsigned iRegister, bool bEmitCode = true);
unsigned FlatFPX87_Pop(FlatFPStateX87* pState, bool bEmitCode = true);
unsigned FlatFPX87_Top(FlatFPStateX87* pState, bool bEmitCode = true);
void FlatFPX87_Unload(FlatFPStateX87* pState, unsigned iVirtual, bool bEmitCode = true);
#endif

// Codegen functions. This is the API that codegen will use
regMaskTP genPushArgumentStackFP(GenTreePtr arg);
void genRoundFpExpressionStackFP(GenTreePtr op, var_types type = TYP_UNDEF);
void genCodeForTreeStackFP_Const(GenTreePtr tree);
void genCodeForTreeStackFP_Leaf(GenTreePtr tree);
void genCodeForTreeStackFP_SmpOp(GenTreePtr tree);
void genCodeForTreeStackFP_Special(GenTreePtr tree);
void genCodeForTreeStackFP_Cast(GenTreePtr tree);
void genCodeForTreeStackFP(GenTreePtr tree);
void genCondJumpFltStackFP(GenTreePtr cond, BasicBlock* jumpTrue, BasicBlock* jumpFalse, bool bDoTransition = true);
void genCondJumpFloat(GenTreePtr cond, BasicBlock* jumpTrue, BasicBlock* jumpFalse);
void genCondJumpLngStackFP(GenTreePtr cond, BasicBlock* jumpTrue, BasicBlock* jumpFalse);

void genFloatConst(GenTree* tree, RegSet::RegisterPreference* pref);
void genFloatLeaf(GenTree* tree, RegSet::RegisterPreference* pref);
void genFloatSimple(GenTree* tree, RegSet::RegisterPreference* pref);
void genFloatMath(GenTree* tree, RegSet::RegisterPreference* pref);
void genFloatCheckFinite(GenTree* tree, RegSet::RegisterPreference* pref);
void genLoadFloat(GenTreePtr tree, regNumber reg);
void genFloatAssign(GenTree* tree);
void genFloatArith(GenTree* tree, RegSet::RegisterPreference* pref);
void genFloatAsgArith(GenTree* tree);

regNumber genAssignArithFloat(genTreeOps oper, GenTreePtr dst, regNumber dstreg, GenTreePtr src, regNumber srcreg);

GenTreePtr genMakeAddressableFloat(GenTreePtr tree,
                                   regMaskTP* regMaskIntPtr,
                                   regMaskTP* regMaskFltPtr,
                                   bool       bCollapseConstantDoubles = true);

void genCodeForTreeFloat(GenTreePtr tree, RegSet::RegisterPreference* pref = NULL);

void genCodeForTreeFloat(GenTreePtr tree, regMaskTP needReg, regMaskTP bestReg);

regNumber genArithmFloat(
    genTreeOps oper, GenTreePtr dst, regNumber dstreg, GenTreePtr src, regNumber srcreg, bool bReverse);
void genCodeForTreeCastFloat(GenTreePtr tree, RegSet::RegisterPreference* pref);
void genCodeForTreeCastToFloat(GenTreePtr tree, RegSet::RegisterPreference* pref);
void genCodeForTreeCastFromFloat(GenTreePtr tree, RegSet::RegisterPreference* pref);
void genKeepAddressableFloat(GenTreePtr tree, regMaskTP* regMaskIntPtr, regMaskTP* regMaskFltPtr);
void genDoneAddressableFloat(GenTreePtr tree, regMaskTP addrRegInt, regMaskTP addrRegFlt, RegSet::KeepReg keptReg);
void genComputeAddressableFloat(GenTreePtr      tree,
                                regMaskTP       addrRegInt,
                                regMaskTP       addrRegFlt,
                                RegSet::KeepReg keptReg,
                                regMaskTP       needReg,
                                RegSet::KeepReg keepReg,
                                bool            freeOnly = false);
void genRoundFloatExpression(GenTreePtr op, var_types type);

#if FEATURE_STACK_FP_X87
// Assumes then block will be generated before else block.
struct QmarkStateStackFP
{
    FlatFPStateX87 stackState;
};

void genQMarkRegVarTransition(GenTreePtr nextNode, VARSET_VALARG_TP liveset);
void genQMarkBeforeElseStackFP(QmarkStateStackFP* pState, VARSET_VALARG_TP varsetCond, GenTreePtr nextNode);
void genQMarkAfterElseBlockStackFP(QmarkStateStackFP* pState, VARSET_VALARG_TP varsetCond, GenTreePtr nextNode);
void genQMarkAfterThenBlockStackFP(QmarkStateStackFP* pState);

#endif

GenTreePtr genMakeAddressableStackFP(GenTreePtr tree,
                                     regMaskTP* regMaskIntPtr,
                                     regMaskTP* regMaskFltPtr,
                                     bool       bCollapseConstantDoubles = true);
void genKeepAddressableStackFP(GenTreePtr tree, regMaskTP* regMaskIntPtr, regMaskTP* regMaskFltPtr);
void genDoneAddressableStackFP(GenTreePtr tree, regMaskTP addrRegInt, regMaskTP addrRegFlt, RegSet::KeepReg keptReg);

void genCodeForTreeStackFP_Asg(GenTreePtr tree);
void genCodeForTreeStackFP_AsgArithm(GenTreePtr tree);
void genCodeForTreeStackFP_Arithm(GenTreePtr tree);
void genCodeForTreeStackFP_DONE(GenTreePtr tree, regNumber reg);
void genCodeForTreeFloat_DONE(GenTreePtr tree, regNumber reg);

void genSetupStateStackFP(BasicBlock* block);
regMaskTP genRegMaskFromLivenessStackFP(VARSET_VALARG_TP varset);

// bReverse means make op1 addressable and codegen for op2.
// If op1 or op2 are comma expressions, will do code-gen for their non-last comma parts,
// and set op1 and op2 to the remaining non-comma expressions.
void genSetupForOpStackFP(
    GenTreePtr& op1, GenTreePtr& op2, bool bReverse, bool bMakeOp1Addressable, bool bOp1ReadOnly, bool bOp2ReadOnly);

#if FEATURE_STACK_FP_X87

#ifdef DEBUG
bool ConsistentAfterStatementStackFP();
#endif

private:
void SpillTempsStackFP(regMaskTP canSpillMask);
void SpillForCallStackFP();
void UnspillRegVarsStackFp();

// Transition API. Takes care of the stack matching of basicblock boundaries
void genCodeForPrologStackFP();
void genCodeForEndBlockTransitionStackFP(BasicBlock* block);

void genCodeForBBTransitionStackFP(BasicBlock* pDst);
void genCodeForTransitionStackFP(FlatFPStateX87* pSrc, FlatFPStateX87* pDst);
void genCodeForTransitionFromMask(FlatFPStateX87* pSrc, regMaskTP mask, bool bEmitCode = true);
BasicBlock* genTransitionBlockStackFP(FlatFPStateX87* pState, BasicBlock* pFrom, BasicBlock* pTarget);

// This is the API codegen will use to emit virtual fp code. In theory, nobody above this API
// should know about x87 instructions.

int  genNumberTemps();
void genDiscardStackFP(GenTreePtr tree);
void genRegRenameWithMasks(regNumber dstReg, regNumber srcReg);
void genRegVarBirthStackFP(GenTreePtr tree);
void genRegVarBirthStackFP(LclVarDsc* varDsc);
void genRegVarDeathStackFP(GenTreePtr tree);
void genRegVarDeathStackFP(LclVarDsc* varDsc);
void genLoadStackFP(GenTreePtr tree, regNumber reg);
void genMovStackFP(GenTreePtr dst, regNumber dstreg, GenTreePtr src, regNumber srcreg);
bool genCompInsStackFP(GenTreePtr tos, GenTreePtr other);
regNumber genArithmStackFP(
    genTreeOps oper, GenTreePtr dst, regNumber dstreg, GenTreePtr src, regNumber srcreg, bool bReverse);
regNumber genAsgArithmStackFP(genTreeOps oper, GenTreePtr dst, regNumber dstreg, GenTreePtr src, regNumber srcreg);
void genCondJmpInsStackFP(emitJumpKind jumpKind,
                          BasicBlock*  jumpTrue,
                          BasicBlock*  jumpFalse,
                          bool         bDoTransition = true);
void genTableSwitchStackFP(regNumber reg, unsigned jumpCnt, BasicBlock** jumpTab);

void JitDumpFPState();
#else  // !FEATURE_STACK_FP_X87
void SpillForCallRegisterFP(regMaskTP noSpillMask);
#endif // !FEATURE_STACK_FP_X87

// When bOnlyNoMemAccess = true, the load will be generated only for constant loading that doesn't
// involve memory accesses, (ie: fldz for positive zero, or fld1 for 1). Will return true the function
// did the load
bool genConstantLoadStackFP(GenTreePtr tree, bool bOnlyNoMemAccess = false);
void genEndOfStatement();

#if FEATURE_STACK_FP_X87
struct genRegVarDiesInSubTreeData
{
    regNumber reg;
    bool      result;
};
static Compiler::fgWalkPreFn genRegVarDiesInSubTreeWorker;
bool genRegVarDiesInSubTree(GenTreePtr tree, regNumber reg);
#endif // FEATURE_STACK_FP_X87

// Float spill
void UnspillFloat(RegSet::SpillDsc* spillDsc);
void UnspillFloat(GenTreePtr tree);
void UnspillFloat(LclVarDsc* varDsc);
void UnspillFloatMachineDep(RegSet::SpillDsc* spillDsc);
void UnspillFloatMachineDep(RegSet::SpillDsc* spillDsc, bool useSameReg);
void RemoveSpillDsc(RegSet::SpillDsc* spillDsc);

protected:
struct genLivenessSet
{
    VARSET_TP    liveSet;
    VARSET_TP    varPtrSet;
    regMaskSmall maskVars;
    regMaskSmall gcRefRegs;
    regMaskSmall byRefRegs;

    genLivenessSet()
        : VARSET_INIT_NOCOPY(liveSet, VarSetOps::UninitVal()), VARSET_INIT_NOCOPY(varPtrSet, VarSetOps::UninitVal())
    {
    }
};

void saveLiveness(genLivenessSet* ls);
void restoreLiveness(genLivenessSet* ls);
void checkLiveness(genLivenessSet* ls);
void unspillLiveness(genLivenessSet* ls);

//-------------------------------------------------------------------------
//
//  If we know that the flags register is set to a value that corresponds
//  to the current value of a register or variable, the following values
//  record that information.
//

emitLocation genFlagsEqLoc;
regNumber    genFlagsEqReg;
unsigned     genFlagsEqVar;

void genFlagsEqualToNone();
void genFlagsEqualToReg(GenTreePtr tree, regNumber reg);
void genFlagsEqualToVar(GenTreePtr tree, unsigned var);
bool genFlagsAreReg(regNumber reg);
bool genFlagsAreVar(unsigned var);

#endif // LEGACY_BACKEND

#endif // _CODEGENCLASSIC_H_
