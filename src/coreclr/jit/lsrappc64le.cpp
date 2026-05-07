// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                    Register Requirements for POWERPC64                    XX
XX                                                                           XX
XX  This encapsulates all the logic for setting register requirements for    XX
XX  the POWERPC64 architecture.                                              XX
XX                                                                           XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifdef TARGET_POWERPC64

#include "jit.h"
#include "sideeffects.h"
#include "lower.h"

//------------------------------------------------------------------------
// getNextConsecutiveRefPosition: Get the next subsequent RefPosition.
//
// Arguments:
//    refPosition   - The RefPosition for which we need to find the next RefPosition.
//
// Return Value:
//    The next RefPosition or nullptr if there is not one.
//
RefPosition* LinearScan::getNextConsecutiveRefPosition(RefPosition* refPosition)
{
	    _ASSERTE(!"NYI");
}

//------------------------------------------------------------------------
// assignConsecutiveRegisters: For subsequent RefPositions, set the register
//   requirement to be the consecutive register(s) of the register that is assigned to
//   the firstRefPosition.
//   If one of the subsequent RefPosition is RefTypeUpperVectorRestore, sets the
//   registerAssignment to not include any of the consecutive registers that are being
//   assigned to the RefTypeUse RefPositions.
//
// Arguments:
//    firstRefPosition  - First RefPosition of the series of consecutive registers.
//    firstRegAssigned  - Register assigned to the first RefPosition.
//
//  Note:
//      This method will set the registerAssignment of subsequent RefPositions with consecutive registers.
//      Some of the registers could be busy, and they will be spilled. We would end up with busy registers if
//      we did not find free consecutive registers.
//
void LinearScan::assignConsecutiveRegisters(RefPosition* firstRefPosition, regNumber firstRegAssigned)
{
	    _ASSERTE(!"NYI");
}

//------------------------------------------------------------------------
// canAssignNextConsecutiveRegisters: Starting with `firstRegAssigned`, check if next
//   consecutive registers are free or are already assigned to the subsequent RefPositions.
//
// Arguments:
//    firstRefPosition  - First RefPosition of the series of consecutive registers.
//    firstRegAssigned  - Register assigned to the first RefPosition.
//
//  Returns:
//      True if all the consecutive registers starting from `firstRegAssigned` are assignable.
//      Even if one of them is busy, returns false.
//
bool LinearScan::canAssignNextConsecutiveRegisters(RefPosition* firstRefPosition, regNumber firstRegAssigned)
{
	    _ASSERTE(!"NYI");
}

//------------------------------------------------------------------------
// filterConsecutiveCandidates: Given `candidates`, check if `registersNeeded` consecutive
//   registers are available in it, and if yes, returns first bit set of every possible series.
//
// Arguments:
//    candidates                - Set of available candidates.
//    registersNeeded           - Number of consecutive registers needed.
//    allConsecutiveCandidates  - Mask returned containing all bits set for possible consecutive register candidates.
//
//  Returns:
//      From `candidates`, the mask of series of consecutive registers of `registersNeeded` size with just the first-bit
//      set.
//
SingleTypeRegSet LinearScan::filterConsecutiveCandidates(SingleTypeRegSet  floatCandidates,
		                                                         unsigned int      registersNeeded,
									                                                          SingleTypeRegSet* allConsecutiveCandidates)
{
	    _ASSERTE(!"NYI");
}

//------------------------------------------------------------------------
// filterConsecutiveCandidatesForSpill: Amoung the selected consecutiveCandidates,
//   check if there are any ranges that would require fewer registers to spill
//   and returns such mask. The return result would always be a subset of
//   consecutiveCandidates.
//
// Arguments:
//    consecutiveCandidates   - Consecutive candidates to filter  on.
//    registersNeeded         - Number of registers needed.
//
//  Returns:
//      Filtered candidates that needs fewer spilling.
//
SingleTypeRegSet LinearScan::filterConsecutiveCandidatesForSpill(SingleTypeRegSet consecutiveCandidates,
		                                                                 unsigned int     registersNeeded)
{
	    _ASSERTE(!"NYI");
}

//------------------------------------------------------------------------
// getConsecutiveCandidates: Returns the mask of all the consecutive candidates
//   for given RefPosition. For first RefPosition of a series of RefPositions that needs
//   consecutive registers, then returns only the mask such that it satisfies the need
//   of having free consecutive registers. If free consecutive registers are not available
//   it finds such a series that needs fewer registers spilling.
//
// Arguments:
//    allCandidates   - Register assigned to the first RefPosition.
//    refPosition     - Number of registers to check.
//    busyCandidates  - Register mask of free/busy registers.
//
//  Returns:
//      Register mask of free consecutive registers. If there are not enough free registers,
//      or the free registers are not consecutive, then return RBM_NONE. In that case,
//      `busyCandidates` will contain the register mask that can be assigned and will include
//      both free and busy registers.
//
//  Notes:
//      The consecutive registers mask includes just the bits of first registers or
//      (n - k) registers. For example, if we need 3 consecutive registers and
//      allCandidates = 0x1C080D0F00000000, the consecutive register mask returned
//      will be 0x400000300000000.
//
SingleTypeRegSet LinearScan::getConsecutiveCandidates(SingleTypeRegSet  allCandidates,
		                                                      RefPosition*      refPosition,
								                                                            SingleTypeRegSet* busyCandidates)
{
	    _ASSERTE(!"NYI");
}
//------------------------------------------------------------------------
// BuildPutArgSplit: Set the NodeInfo for a GT_PUTARG_SPLIT node
//
// Arguments:
//    argNode - a GT_PUTARG_SPLIT node
//
// Return Value:
//    The number of sources consumed by this node.
//
// Notes:
//    Set the child node(s) to be contained
//
int LinearScan::BuildPutArgSplit(GenTreePutArgSplit* argNode)
{
	    _ASSERTE(!"NYI");
}

//------------------------------------------------------------------------
// BuildCast: Set the NodeInfo for a GT_CAST.
//  
// Arguments:
//    cast - The GT_CAST node
//
// Return Value:
//    The number of sources consumed by this node.
//
int LinearScan::BuildCast(GenTreeCast* cast)
{
    _ASSERTE(!"NYI");
}

//------------------------------------------------------------------------
// BuildPutArgStk: Set the NodeInfo for a GT_PUTARG_STK node
//
// Arguments:
//    argNode - a GT_PUTARG_STK node
//
// Return Value:
//    The number of sources consumed by this node.
//
// Notes:
//    Set the child node(s) to be contained when we have a multireg arg
//
int LinearScan::BuildPutArgStk(GenTreePutArgStk* argNode)
{
    _ASSERTE(!"NYI");
}

//------------------------------------------------------------------------
/// BuildIndir: Specify register requirements for address expression
//                       of an indirection operation.
//      
// Arguments:
//    indirTree - GT_IND, GT_STOREIND or block GenTree node
//  
// Return Value:
//    The number of sources consumed by this node.
//
int LinearScan::BuildIndir(GenTreeIndir* indirTree)
{
    if (indirTree->OperGet() != GT_IND)
    {
        _ASSERTE(!"NYI");
    }

    // struct typed indirs are expected only on rhs of a block copy,
    // but in this case they must be contained.
    assert(indirTree->TypeGet() != TYP_STRUCT);

    GenTree* addr  = indirTree->Addr();
    GenTree* index = nullptr;
    int      cns   = 0;

    int srcCount = BuildIndirUses(indirTree);
    buildInternalRegisterUses();

    if (!indirTree->OperIs(GT_STOREIND, GT_NULLCHECK))
    {
        BuildDef(indirTree);
    }
    return srcCount;
}

//------------------------------------------------------------------------
// BuildSelect: Build RefPositions for a GT_SELECT node.
//
// Arguments:
//    select - The GT_SELECT node
//      
// Return Value:
//    The number of sources consumed by this node.
//
int LinearScan::BuildSelect(GenTreeOp* select)
{   
	    _ASSERTE(!"NYI");
}

//------------------------------------------------------------------------
// BuildBlockStore: Build the RefPositions for a block store node.
//
// Arguments:
//    blkNode - The block store node of interest
//
// Return Value:
//    The number of sources consumed by this node.
//
int LinearScan::BuildBlockStore(GenTreeBlk* blkNode)
{
	    _ASSERTE(!"NYI");
}

//------------------------------------------------------------------------
// BuildCall: Set the NodeInfo for a call.
//
// Arguments:
//    call - The call node of interest
//
// Return Value:
//    The number of sources consumed by this node.
//
int LinearScan::BuildCall(GenTreeCall* call)
{
    bool                  hasMultiRegRetVal   = false;
    const ReturnTypeDesc* retTypeDesc         = nullptr;
    SingleTypeRegSet      singleDstCandidates = RBM_NONE;

    int srcCount = 0;
    int dstCount = 0;
    if (call->TypeGet() != TYP_VOID)
    {
        hasMultiRegRetVal = call->HasMultiRegRetVal();
        if (hasMultiRegRetVal)
        {
            // dst count = number of registers in which the value is returned by call
            retTypeDesc = call->GetReturnTypeDesc();
            dstCount    = retTypeDesc->GetReturnRegCount();
        }
        else
        {
            dstCount = 1;
        }
    }

    GenTree*         ctrlExpr           = call->gtControlExpr;
    SingleTypeRegSet ctrlExprCandidates = RBM_NONE;
    if (call->gtCallType == CT_INDIRECT)
    {
        // either gtControlExpr != null or gtCallAddr != null.
        // Both cannot be non-null at the same time.
        assert(ctrlExpr == nullptr);
        assert(call->gtCallAddr != nullptr);
        ctrlExpr = call->gtCallAddr;
    }

    // set reg requirements on call target represented as control sequence.
    if (ctrlExpr != nullptr)
    {
        // we should never see a gtControlExpr whose type is void.
        assert(ctrlExpr->TypeGet() != TYP_VOID);

        // In case of fast tail implemented as jmp, make sure that gtControlExpr is
        // computed into a register.
        if (call->IsFastTailCall())
        {
            // Fast tail call - make sure that call target is always computed in volatile registers
            // that will not be overridden by epilog sequence.
            ctrlExprCandidates = RBM_INT_CALLEE_TRASH.GetIntRegSet();
            assert(ctrlExprCandidates != RBM_NONE);
        }
    }

    RegisterType registerType = call->TypeGet();

    // Set destination candidates for return value of the call.
    if (!hasMultiRegRetVal)
    {
        if (varTypeUsesFloatArgReg(registerType))
        {
            singleDstCandidates = RBM_FLOATRET.GetFloatRegSet();
        }
        else if (registerType == TYP_LONG)
        {
            singleDstCandidates = RBM_LNGRET.GetIntRegSet();
        }
        else
        {
            singleDstCandidates = RBM_INTRET.GetIntRegSet();
        }
    }

    srcCount += BuildCallArgUses(call);

    if (ctrlExpr != nullptr)
    {
        BuildUse(ctrlExpr, ctrlExprCandidates);
        srcCount++;
    }

    buildInternalRegisterUses();

    // Now generate defs and kills.
    regMaskTP killMask = getKillSetForCall(call);
    if (dstCount > 0)
    {
        if (hasMultiRegRetVal)
        {
            assert(retTypeDesc != nullptr);
            regMaskTP multiDstCandidates = retTypeDesc->GetABIReturnRegs(call->GetUnmanagedCallConv());
            assert(genCountBits(multiDstCandidates) > 0);
            BuildCallDefsWithKills(call, dstCount, multiDstCandidates, killMask);
        }
        else
        {
            assert(dstCount == 1);
            BuildDefWithKills(call, dstCount, singleDstCandidates, killMask);
        }
    }
    else
    {
        BuildKills(call, killMask);
    }

    // No args are placed in registers anymore.
    placedArgRegs      = RBM_NONE;
    numPlacedArgLocals = 0;
    return srcCount;
}

//------------------------------------------------------------------------
// BuildNode: Build the RefPositions for a node
//
// Arguments:
//    treeNode - the node of interest
//
// Return Value:
//    The number of sources consumed by this node.
//
// Notes:
// Preconditions:
//    LSRA Has been initialized.
//
// Postconditions:
//    RefPositions have been built for all the register defs and uses required
//    for this node.
//
int LinearScan::BuildNode(GenTree* tree)
{
    assert(!tree->isContained());
    int       srcCount;
    int       dstCount;
    regMaskTP killMask      = RBM_NONE;
    bool      isLocalDefUse = false;

    // Reset the build-related members of LinearScan.
    clearBuildState();

    // Set the default dstCount. This may be modified below.
    if (tree->IsValue())
    {
        dstCount = 1;
        if (tree->IsUnusedValue())
        {
            isLocalDefUse = true;
        }
    }
    else
    {
        dstCount = 0;
    }

    switch (tree->OperGet())
    {
        case GT_CNS_INT:
        {
            srcCount = 0;
            assert(dstCount == 1);
            RefPosition* def               = BuildDef(tree);
            def->getInterval()->isConstant = true;
        }
        break;

	case GT_IND:
	{
            assert(dstCount == (tree->OperIs(GT_NULLCHECK) ? 0 : 1));
            srcCount = BuildIndir(tree->AsIndir());
	}
        break;

	case GT_EQ:
        case GT_NE:
        case GT_LT:
        case GT_LE:
        case GT_GE:
        case GT_GT:
        case GT_TEST_EQ:
        case GT_TEST_NE:
        case GT_CMP:
        case GT_TEST:
        //case GT_CCMP:
        case GT_JCMP:
        case GT_JTEST:
            srcCount = BuildCmp(tree);
            break;

	case GT_LCL_ADDR:
        case GT_PHYSREG:
        case GT_IL_OFFSET:
        case GT_LABEL:
        case GT_PINVOKE_PROLOG:
        case GT_JCC:
        case GT_SETCC:
        case GT_MEMORYBARRIER:
            srcCount = BuildSimple(tree);
            break;

	case GT_CALL:
            srcCount = BuildCall(tree->AsCall());
            if (tree->AsCall()->HasMultiRegRetVal())
            {
                dstCount = tree->AsCall()->GetReturnTypeDesc()->GetReturnRegCount();
            }
            break;

	case GT_NO_OP:
        case GT_START_NONGC:
        case GT_PROF_HOOK:
            srcCount = 0;
            assert(dstCount == 0);
            break;

	case GT_RETURN:
            srcCount = BuildReturn(tree);
            killMask = getKillSetForReturn();
            BuildKills(tree, killMask);
            break;

	case GT_NOP:
            srcCount = 0;
            assert(tree->TypeIs(TYP_VOID));
            assert(dstCount == 0);
            break;

	case GT_STORE_LCL_VAR:
	    srcCount = BuildStoreLoc(tree->AsLclVarCommon());
            break;

	case GT_LCL_VAR:
	    srcCount = 0;
	    BuildDef(tree);
	    break;
              case GT_ADD:
              case GT_SUB:
                  if (varTypeIsFloating(tree->TypeGet()))
                  {
                      // Overflow operations aren't supported on float/double types.
                      assert(!tree->gtOverflow());

                      // No implicit conversions at this stage as the expectation is that
                      // everything is made explicit by adding casts.
                      assert(tree->gtGetOp1()->TypeGet() == tree->gtGetOp2()->TypeGet());
                  }
                  else if (tree->gtOverflow())
                  {
                      // Need a register different from target reg to check for overflow.
                     buildInternalIntRegisterDefForNode(tree);
                     setInternalRegsDelayFree = true;
                  }
                  FALLTHROUGH;

              case GT_AND:
              case GT_OR:
              case GT_XOR:
              case GT_LSH:
              case GT_RSH:
              case GT_RSZ:
              case GT_ROR:
                  srcCount = BuildBinaryUses(tree->AsOp());
                  buildInternalRegisterUses();
                  assert(dstCount == 1);
                  BuildDef(tree);
                  break;

              case GT_MUL:
                  if (tree->gtOverflow())
                  {
                      // Need a register different from target reg to check for overflow.
                      buildInternalIntRegisterDefForNode(tree);
                      setInternalRegsDelayFree = true;
                  }
                  FALLTHROUGH;

              case GT_DIV:
              case GT_UDIV:
  	            // Division operations don't need internal registers
    	            srcCount = BuildBinaryUses(tree->AsOp());
      	            buildInternalRegisterUses();
		    assert(dstCount == 1);
		    BuildDef(tree);
		    break;

	      case GT_MOD:
              case GT_UMOD:
	      {
		    // PowerPC64 doesn't have a direct MOD instruction
  	            // We compute: remainder = dividend - (quotient * divisor)
		    // This requires a temporary register to hold the quotient
		    buildInternalIntRegisterDefForNode(tree);
		    srcCount = BuildBinaryUses(tree->AsOp());
		    buildInternalRegisterUses();
		    assert(dstCount == 1);
		    BuildDef(tree);
	      }
	      break;

	      case GT_MULHI:
              {
                  srcCount = BuildBinaryUses(tree->AsOp());
                  buildInternalRegisterUses();
                  assert(dstCount == 1);
                  BuildDef(tree);
              }
              break;

	default:
	{
	    _ASSERTE(!"NYI");
	}
    }

    // We need to be sure that we've set srcCount and dstCount appropriately
    assert((dstCount < 2) || tree->IsMultiRegNode());
    assert(isLocalDefUse == (tree->IsValue() && tree->IsUnusedValue()));
    assert(!tree->IsValue() || (dstCount != 0));
    assert(dstCount == tree->GetRegisterDstCount(compiler));
    return srcCount;
}

#ifdef FEATURE_HW_INTRINSICS

#include "hwintrinsic.h"

//------------------------------------------------------------------------
// BuildHWIntrinsic: Set the NodeInfo for a GT_HWINTRINSIC tree.
//
// Arguments:
//    tree       - The GT_HWINTRINSIC node of interest
//    pDstCount  - OUT parameter - the number of registers defined for the given node
//
// Return Value:
//    The number of sources consumed by this node.
//
int LinearScan::BuildHWIntrinsic(GenTreeHWIntrinsic* intrinsicTree, int* pDstCount)
{
	    _ASSERTE(!"NYI");
}

//------------------------------------------------------------------------
//  BuildConsecutiveRegistersForUse: Build ref position(s) for `treeNode` that has a
//  requirement of allocating consecutive registers. It will create the RefTypeUse
//  RefPositions for as many consecutive registers are needed for `treeNode` and in
//  between, it might contain RefTypeUpperVectorRestore RefPositions.
//
//  For the first RefPosition of the series, it sets the `regCount` field equal to
//  the number of subsequent RefPositions (including the first one) involved for this
//  treeNode. For the subsequent RefPositions, it sets the `regCount` to 0. For all
//  the RefPositions created, it sets the `needsConsecutive` flag so it can be used to
//  identify these RefPositions during allocation.
//
//  It also populates a `RefPositionMap` to access the subsequent RefPositions from
//  a given RefPosition. This was preferred rather than adding a field in RefPosition
//  for this purpose.
//
// Arguments:
//    treeNode       - The GT_HWINTRINSIC node of interest
//    rmwNode        - Read-modify-write node.
//
// Return Value:
//    The number of sources consumed by this node.
//
int LinearScan::BuildConsecutiveRegistersForUse(GenTree* treeNode, GenTree* rmwNode)
{
	    _ASSERTE(!"NYI");
}

//------------------------------------------------------------------------
//  BuildConsecutiveRegistersForDef: Build RefTypeDef ref position(s) for
//  `treeNode` that produces `registerCount` consecutive registers.
//
//  For the first RefPosition of the series, it sets the `regCount` field equal to
//  the total number of RefPositions (including the first one) involved for this
//  treeNode. For the subsequent RefPositions, it sets the `regCount` to 0. For all
//  the RefPositions created, it sets the `needsConsecutive` flag so it can be used to
//  identify these RefPositions during allocation.
//
//  It also populates a `RefPositionMap` to access the subsequent RefPositions from
//  a given RefPosition. This was preferred rather than adding a field in RefPosition
//  for this purpose.
//
// Arguments:
//    treeNode       - The GT_HWINTRINSIC node of interest
//    registerCount  - Number of registers the treeNode produces
//
void LinearScan::BuildConsecutiveRegistersForDef(GenTree* treeNode, int registerCount)
{
	    _ASSERTE(!"NYI");
}

#ifdef DEBUG
//------------------------------------------------------------------------
// isLiveAtConsecutiveRegistersLoc: Check if the refPosition is live at the location
//    where consecutive registers are needed. This is used during JitStressRegs to
//    not constrain the register requirements for such refpositions, because a lot
//    of registers will be busy. For RefTypeUse, it will just see if the nodeLocation
//    matches with the tracking `consecutiveRegistersLocation`. For Def, it will check
//    the underlying `GenTree*` to see if the tree that produced it had consecutive
//    registers requirement.
//
//
// Arguments:
//    consecutiveRegistersLocation - The most recent location where consecutive
//     registers were needed.
//
// Returns: If the refposition is live at same location which has the requirement of
//    consecutive registers.
//
bool RefPosition::isLiveAtConsecutiveRegistersLoc(LsraLocation consecutiveRegistersLocation)
{
	    _ASSERTE(!"NYI");
}
#endif // DEBUG

//------------------------------------------------------------------------
// getLowVectorOperandAndCandidates: Instructions for certain intrinsics operate on low vector registers
//      depending on the size of the element. The method returns the candidates based on that size and
//      the operand number of the intrinsics that has the restriction.
//
// Arguments:
//    intrin - Intrinsics
//    operandNum (out) - The operand number having the low vector register restriction
//    candidates (out) - The restricted low vector registers
//
void LinearScan::getLowVectorOperandAndCandidates(HWIntrinsic intrin, size_t* operandNum, SingleTypeRegSet* candidates)
{
	    _ASSERTE(!"NYI");
}

#endif // FEATURE_HW_INTRINSICS

#endif //TARGET_POWERPC64
