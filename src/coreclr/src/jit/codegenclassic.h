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
regNumber genIsEnregisteredIntVariable(GenTree* tree);

void sched_AM(instruction ins,
              emitAttr    size,
              regNumber   ireg,
              bool        rdst,
              GenTree*    tree,
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
void genDecRegBy(regNumber reg, ssize_t ival, GenTree* tree);
void genIncRegBy(regNumber reg, ssize_t ival, GenTree* tree, var_types dstType = TYP_INT, bool ovfl = false);

void genMulRegBy(regNumber reg, ssize_t ival, GenTree* tree, var_types dstType = TYP_INT, bool ovfl = false);

//-------------------------------------------------------------------------

bool genRegTrashable(regNumber reg, GenTree* tree);

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

regNumber genLclHeap(GenTree* size);

void genDyingVars(VARSET_VALARG_TP beforeSet, VARSET_VALARG_TP afterSet);

bool genContainsVarDeath(GenTree* from, GenTree* to, unsigned varNum);

void genComputeReg(
    GenTree* tree, regMaskTP needReg, RegSet::ExactReg mustReg, RegSet::KeepReg keepReg, bool freeOnly = false);

void genCompIntoFreeReg(GenTree* tree, regMaskTP needReg, RegSet::KeepReg keepReg);

void genReleaseReg(GenTree* tree);

void genRecoverReg(GenTree* tree, regMaskTP needReg, RegSet::KeepReg keepReg);

void genMoveRegPairHalf(GenTree* tree, regNumber dst, regNumber src, int off = 0);

void genMoveRegPair(GenTree* tree, regMaskTP needReg, regPairNo newPair);

void genComputeRegPair(
    GenTree* tree, regPairNo needRegPair, regMaskTP avoidReg, RegSet::KeepReg keepReg, bool freeOnly = false);

void genCompIntoFreeRegPair(GenTree* tree, regMaskTP avoidReg, RegSet::KeepReg keepReg);

void genComputeAddressable(GenTree*        tree,
                           regMaskTP       addrReg,
                           RegSet::KeepReg keptReg,
                           regMaskTP       needReg,
                           RegSet::KeepReg keepReg,
                           bool            freeOnly = false);

void genReleaseRegPair(GenTree* tree);

void genRecoverRegPair(GenTree* tree, regPairNo regPair, RegSet::KeepReg keepReg);

void genEvalIntoFreeRegPair(GenTree* tree, regPairNo regPair, regMaskTP avoidReg);

void genMakeRegPairAvailable(regPairNo regPair);

bool genMakeIndAddrMode(GenTree*        addr,
                        GenTree*        oper,
                        bool            forLea,
                        regMaskTP       regMask,
                        RegSet::KeepReg keepReg,
                        regMaskTP*      useMaskPtr,
                        bool            deferOp = false);

regMaskTP genMakeRvalueAddressable(
    GenTree* tree, regMaskTP needReg, RegSet::KeepReg keepReg, bool forLoadStore, bool smallOK = false);

regMaskTP genMakeAddressable(
    GenTree* tree, regMaskTP needReg, RegSet::KeepReg keepReg, bool smallOK = false, bool deferOK = false);

regMaskTP genMakeAddrArrElem(GenTree* arrElem, GenTree* tree, regMaskTP needReg, RegSet::KeepReg keepReg);

regMaskTP genMakeAddressable2(GenTree*        tree,
                              regMaskTP       needReg,
                              RegSet::KeepReg keepReg,
                              bool            forLoadStore,
                              bool            smallOK      = false,
                              bool            deferOK      = false,
                              bool            evalSideEffs = false);

bool genStillAddressable(GenTree* tree);

regMaskTP genRestoreAddrMode(GenTree* addr, GenTree* tree, bool lockPhase);

regMaskTP genRestAddressable(GenTree* tree, regMaskTP addrReg, regMaskTP lockMask);

regMaskTP genKeepAddressable(GenTree* tree, regMaskTP addrReg, regMaskTP avoidMask = RBM_NONE);

void genDoneAddressable(GenTree* tree, regMaskTP addrReg, RegSet::KeepReg keptReg);

GenTree* genMakeAddrOrFPstk(GenTree* tree, regMaskTP* regMaskPtr, bool roundResult);

void genEmitGSCookieCheck(bool pushReg);

void genEvalSideEffects(GenTree* tree);

void genCondJump(GenTree* cond, BasicBlock* destTrue = NULL, BasicBlock* destFalse = NULL, bool bStackFPFixup = true);

emitJumpKind genCondSetFlags(GenTree* cond);

void genJCC(genTreeOps cmp, BasicBlock* block, var_types type);

void genJccLongHi(genTreeOps cmp, BasicBlock* jumpTrue, BasicBlock* jumpFalse, bool unsOper = false);

void genJccLongLo(genTreeOps cmp, BasicBlock* jumpTrue, BasicBlock* jumpFalse);

void genCondJumpLng(GenTree* cond, BasicBlock* jumpTrue, BasicBlock* jumpFalse, bool bFPTransition = false);

bool genUse_fcomip();

void genTableSwitch(regNumber reg, unsigned jumpCnt, BasicBlock** jumpTab);

regMaskTP WriteBarrier(GenTree* tgt, GenTree* assignVal, regMaskTP addrReg);

void genCodeForTreeConst(GenTree* tree, regMaskTP destReg, regMaskTP bestReg = RBM_NONE);

void genCodeForTreeLeaf(GenTree* tree, regMaskTP destReg, regMaskTP bestReg = RBM_NONE);

// If "tree" is a comma node, generates code for the left comma arguments,
// in order, returning the first right argument in the list that is not
// a comma node.
GenTree* genCodeForCommaTree(GenTree* tree);

void genCodeForTreeLeaf_GT_JMP(GenTree* tree);

static Compiler::fgWalkPreFn fgIsVarAssignedTo;

void genCodeForQmark(GenTree* tree, regMaskTP destReg, regMaskTP bestReg);

bool genCodeForQmarkWithCMOV(GenTree* tree, regMaskTP destReg, regMaskTP bestReg);

#ifdef _TARGET_XARCH_
void genCodeForMultEAX(GenTree* tree);
#endif
#ifdef _TARGET_ARM_
void genCodeForMult64(GenTree* tree, regMaskTP destReg, regMaskTP bestReg);
#endif

void genCodeForTreeSmpBinArithLogOp(GenTree* tree, regMaskTP destReg, regMaskTP bestReg);

void genCodeForTreeSmpBinArithLogAsgOp(GenTree* tree, regMaskTP destReg, regMaskTP bestReg);

void genCodeForUnsignedMod(GenTree* tree, regMaskTP destReg, regMaskTP bestReg);

void genCodeForSignedMod(GenTree* tree, regMaskTP destReg, regMaskTP bestReg);

void genCodeForUnsignedDiv(GenTree* tree, regMaskTP destReg, regMaskTP bestReg);

void genCodeForSignedDiv(GenTree* tree, regMaskTP destReg, regMaskTP bestReg);

void genCodeForGeneralDivide(GenTree* tree, regMaskTP destReg, regMaskTP bestReg);

void genCodeForAsgShift(GenTree* tree, regMaskTP destReg, regMaskTP bestReg);

void genCodeForShift(GenTree* tree, regMaskTP destReg, regMaskTP bestReg);

void genCodeForRelop(GenTree* tree, regMaskTP destReg, regMaskTP bestReg);

void genCodeForCopyObj(GenTree* tree, regMaskTP destReg);

void genCodeForBlkOp(GenTree* tree, regMaskTP destReg);

void genCodeForTreeSmpOp(GenTree* tree, regMaskTP destReg, regMaskTP bestReg = RBM_NONE);

regNumber genIntegerCast(GenTree* tree, regMaskTP needReg, regMaskTP bestReg);

void genCodeForNumericCast(GenTree* tree, regMaskTP destReg, regMaskTP bestReg);

void genCodeForTreeSmpOp_GT_ADDR(GenTree* tree, regMaskTP destReg, regMaskTP bestReg = RBM_NONE);

void genCodeForTreeSmpOpAsg(GenTree* tree);

void genCodeForTreeSmpOpAsg_DONE_ASSG(GenTree* tree, regMaskTP addrReg, regNumber reg, bool ovfl);

void genCodeForTreeSpecialOp(GenTree* tree, regMaskTP destReg, regMaskTP bestReg = RBM_NONE);

void genCodeForTree(GenTree* tree, regMaskTP destReg, regMaskTP bestReg = RBM_NONE);

void genCodeForTree_DONE_LIFE(GenTree* tree, regNumber reg)
{
    /* We've computed the value of 'tree' into 'reg' */

    assert(reg != 0xFEEFFAAFu);
    assert(!IsUninitialized(reg));

    genMarkTreeInReg(tree, reg);
}

void genCodeForTree_DONE_LIFE(GenTree* tree, regPairNo regPair)
{
    /* We've computed the value of 'tree' into 'regPair' */

    genMarkTreeInRegPair(tree, regPair);
}

void genCodeForTree_DONE(GenTree* tree, regNumber reg)
{
    /* Check whether this subtree has freed up any variables */

    genUpdateLife(tree);

    genCodeForTree_DONE_LIFE(tree, reg);
}

void genCodeForTree_REG_VAR1(GenTree* tree)
{
    /* Value is already in a register */

    regNumber reg = tree->gtRegNum;

    gcInfo.gcMarkRegPtrVal(reg, tree->TypeGet());

    genCodeForTree_DONE(tree, reg);
}

void genCodeForTreeLng(GenTree* tree, regMaskTP needReg, regMaskTP avoidReg);

regPairNo genCodeForLongModInt(GenTree* tree, regMaskTP needReg);

unsigned genRegCountForLiveIntEnregVars(GenTree* tree);

#ifdef _TARGET_ARM_
void genStoreFromFltRetRegs(GenTree* tree);
void genLoadIntoFltRetRegs(GenTree* tree);
void genLdStFltRetRegsPromotedVar(LclVarDsc* varDsc, bool isLoadIntoFltReg);
#endif

#if CPU_HAS_FP_SUPPORT
void genRoundFpExpression(GenTree* op, var_types type = TYP_UNDEF);
void genCodeForTreeFlt(GenTree* tree, regMaskTP needReg = RBM_ALLFLOAT, regMaskTP bestReg = RBM_NONE);
#endif

// FP stuff
#include "fp.h"

void genCodeForJumpTable(GenTree* tree);
void genCodeForSwitchTable(GenTree* tree);
void genCodeForSwitch(GenTree* tree);

size_t genPushArgList(GenTreeCall* call);

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
bool genFillSlotFromPromotedStruct(GenTree*       arg,
                                   fgArgTabEntry* curArgTabEntry,
                                   LclVarDsc*     promotedStructLocalVarDesc,
                                   emitAttr       fieldSize,
                                   unsigned*      pNextPromotedStructFieldVar,         // IN/OUT
                                   unsigned*      pBytesOfNextSlotOfCurPromotedStruct, // IN/OUT
                                   regNumber*     pCurRegNum,                          // IN/OUT
                                   int            argOffset,
                                   int            fieldOffsetOfFirstStackSlot,
                                   int            argOffsetOfFirstStackSlot,
                                   regMaskTP*     deadFieldVarRegs, // OUT
                                   regNumber*     pRegTmp);         // IN/OUT

#endif // _TARGET_ARM_
// Requires that "curr" is a cpblk.  If the RHS is a promoted struct local,
// then returns a regMaskTP representing the set of registers holding
// fieldVars of the RHS that go dead with this use (as determined by the live set
// of cpBlk).
regMaskTP genFindDeadFieldRegs(GenTree* cpBlk);

void SetupLateArgs(GenTreeCall* call);

#ifdef _TARGET_ARM_
void PushMkRefAnyArg(GenTree* mkRefAnyTree, fgArgTabEntry* curArgTabEntry, regMaskTP regNeedMask);
#endif // _TARGET_ARM_

regMaskTP genLoadIndirectCallTarget(GenTreeCall* call);

regMaskTP genCodeForCall(GenTreeCall* call, bool valUsed);

GenTree* genGetAddrModeBase(GenTree* tree);

GenTree* genIsAddrMode(GenTree* tree, GenTree** indxPtr);

private:
bool genIsLocalLastUse(GenTree* tree);

bool genIsRegCandidateLocal(GenTree* tree);

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
regMaskTP genPushArgumentStackFP(GenTree* arg);
void genRoundFpExpressionStackFP(GenTree* op, var_types type = TYP_UNDEF);
void genCodeForTreeStackFP_Const(GenTree* tree);
void genCodeForTreeStackFP_Leaf(GenTree* tree);
void genCodeForTreeStackFP_SmpOp(GenTree* tree);
void genCodeForTreeStackFP_Special(GenTree* tree);
void genCodeForTreeStackFP_Cast(GenTree* tree);
void genCodeForTreeStackFP(GenTree* tree);
void genCondJumpFltStackFP(GenTree* cond, BasicBlock* jumpTrue, BasicBlock* jumpFalse, bool bDoTransition = true);
void genCondJumpFloat(GenTree* cond, BasicBlock* jumpTrue, BasicBlock* jumpFalse);
void genCondJumpLngStackFP(GenTree* cond, BasicBlock* jumpTrue, BasicBlock* jumpFalse);

void genFloatConst(GenTree* tree, RegSet::RegisterPreference* pref);
void genFloatLeaf(GenTree* tree, RegSet::RegisterPreference* pref);
void genFloatSimple(GenTree* tree, RegSet::RegisterPreference* pref);
void genFloatMath(GenTree* tree, RegSet::RegisterPreference* pref);
void genFloatCheckFinite(GenTree* tree, RegSet::RegisterPreference* pref);
void genLoadFloat(GenTree* tree, regNumber reg);
void genFloatAssign(GenTree* tree);
void genFloatArith(GenTree* tree, RegSet::RegisterPreference* pref);
void genFloatAsgArith(GenTree* tree);

regNumber genAssignArithFloat(genTreeOps oper, GenTree* dst, regNumber dstreg, GenTree* src, regNumber srcreg);

GenTree* genMakeAddressableFloat(GenTree*   tree,
                                 regMaskTP* regMaskIntPtr,
                                 regMaskTP* regMaskFltPtr,
                                 bool       bCollapseConstantDoubles = true);

void genCodeForTreeFloat(GenTree* tree, RegSet::RegisterPreference* pref = NULL);

void genCodeForTreeFloat(GenTree* tree, regMaskTP needReg, regMaskTP bestReg);

regNumber genArithmFloat(
    genTreeOps oper, GenTree* dst, regNumber dstreg, GenTree* src, regNumber srcreg, bool bReverse);
void genCodeForTreeCastFloat(GenTree* tree, RegSet::RegisterPreference* pref);
void genCodeForTreeCastToFloat(GenTree* tree, RegSet::RegisterPreference* pref);
void genCodeForTreeCastFromFloat(GenTree* tree, RegSet::RegisterPreference* pref);
void genKeepAddressableFloat(GenTree* tree, regMaskTP* regMaskIntPtr, regMaskTP* regMaskFltPtr);
void genDoneAddressableFloat(GenTree* tree, regMaskTP addrRegInt, regMaskTP addrRegFlt, RegSet::KeepReg keptReg);
void genComputeAddressableFloat(GenTree*        tree,
                                regMaskTP       addrRegInt,
                                regMaskTP       addrRegFlt,
                                RegSet::KeepReg keptReg,
                                regMaskTP       needReg,
                                RegSet::KeepReg keepReg,
                                bool            freeOnly = false);
void genRoundFloatExpression(GenTree* op, var_types type);

#if FEATURE_STACK_FP_X87
// Assumes then block will be generated before else block.
struct QmarkStateStackFP
{
    FlatFPStateX87 stackState;
};

void genQMarkRegVarTransition(GenTree* nextNode, VARSET_VALARG_TP liveset);
void genQMarkBeforeElseStackFP(QmarkStateStackFP* pState, VARSET_VALARG_TP varsetCond, GenTree* nextNode);
void genQMarkAfterElseBlockStackFP(QmarkStateStackFP* pState, VARSET_VALARG_TP varsetCond, GenTree* nextNode);
void genQMarkAfterThenBlockStackFP(QmarkStateStackFP* pState);

#endif

GenTree* genMakeAddressableStackFP(GenTree*   tree,
                                   regMaskTP* regMaskIntPtr,
                                   regMaskTP* regMaskFltPtr,
                                   bool       bCollapseConstantDoubles = true);
void genKeepAddressableStackFP(GenTree* tree, regMaskTP* regMaskIntPtr, regMaskTP* regMaskFltPtr);
void genDoneAddressableStackFP(GenTree* tree, regMaskTP addrRegInt, regMaskTP addrRegFlt, RegSet::KeepReg keptReg);

void genCodeForTreeStackFP_Asg(GenTree* tree);
void genCodeForTreeStackFP_AsgArithm(GenTree* tree);
void genCodeForTreeStackFP_Arithm(GenTree* tree);
void genCodeForTreeStackFP_DONE(GenTree* tree, regNumber reg);
void genCodeForTreeFloat_DONE(GenTree* tree, regNumber reg);

void genSetupStateStackFP(BasicBlock* block);
regMaskTP genRegMaskFromLivenessStackFP(VARSET_VALARG_TP varset);

// bReverse means make op1 addressable and codegen for op2.
// If op1 or op2 are comma expressions, will do code-gen for their non-last comma parts,
// and set op1 and op2 to the remaining non-comma expressions.
void genSetupForOpStackFP(
    GenTree*& op1, GenTree*& op2, bool bReverse, bool bMakeOp1Addressable, bool bOp1ReadOnly, bool bOp2ReadOnly);

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
void genDiscardStackFP(GenTree* tree);
void genRegRenameWithMasks(regNumber dstReg, regNumber srcReg);
void genRegVarBirthStackFP(GenTree* tree);
void genRegVarBirthStackFP(LclVarDsc* varDsc);
void genRegVarDeathStackFP(GenTree* tree);
void genRegVarDeathStackFP(LclVarDsc* varDsc);
void genLoadStackFP(GenTree* tree, regNumber reg);
void genMovStackFP(GenTree* dst, regNumber dstreg, GenTree* src, regNumber srcreg);
bool genCompInsStackFP(GenTree* tos, GenTree* other);
regNumber genArithmStackFP(
    genTreeOps oper, GenTree* dst, regNumber dstreg, GenTree* src, regNumber srcreg, bool bReverse);
regNumber genAsgArithmStackFP(genTreeOps oper, GenTree* dst, regNumber dstreg, GenTree* src, regNumber srcreg);
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
bool genConstantLoadStackFP(GenTree* tree, bool bOnlyNoMemAccess = false);
void genEndOfStatement();

#if FEATURE_STACK_FP_X87
struct genRegVarDiesInSubTreeData
{
    regNumber reg;
    bool      result;
};
static Compiler::fgWalkPreFn genRegVarDiesInSubTreeWorker;
bool genRegVarDiesInSubTree(GenTree* tree, regNumber reg);
#endif // FEATURE_STACK_FP_X87

// Float spill
void UnspillFloat(RegSet::SpillDsc* spillDsc);
void UnspillFloat(GenTree* tree);
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

    genLivenessSet() : liveSet(VarSetOps::UninitVal()), varPtrSet(VarSetOps::UninitVal())
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
void genFlagsEqualToReg(GenTree* tree, regNumber reg);
void genFlagsEqualToVar(GenTree* tree, unsigned var);
bool genFlagsAreReg(regNumber reg);
bool genFlagsAreVar(unsigned var);

#endif // LEGACY_BACKEND

#endif // _CODEGENCLASSIC_H_
