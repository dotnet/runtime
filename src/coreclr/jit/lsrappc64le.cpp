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
    int srcCount = 0;
    assert(argNode->gtOper == GT_PUTARG_SPLIT);

    GenTree* src = argNode->gtGetOp1();

    // Registers for split argument corresponds to source
    int dstCount = argNode->gtNumRegs;

    regNumber        argReg  = argNode->GetRegNum();
    SingleTypeRegSet argMask = RBM_NONE;
    for (unsigned i = 0; i < argNode->gtNumRegs; i++)
    {
        regNumber thisArgReg = argNode->GetRegNumByIdx(i);
        argMask |= genSingleTypeRegMask(thisArgReg);
    }

    if (src->OperGet() == GT_FIELD_LIST)
    {
        // Generated code:
        // 1. Consume all of the items in the GT_FIELD_LIST (source)
        // 2. Store to target slot and move to target registers (destination) from source
        //
        unsigned sourceRegCount = 0;

        // To avoid redundant moves, have the argument operand computed in the
        // register in which the argument is passed to the call.

        for (GenTreeFieldList::Use& use : src->AsFieldList()->Uses())
        {
            GenTree* node = use.GetNode();
            assert(!node->isContained());
            
            // Consume all the registers, setting the appropriate register mask for the ones that
            // go into registers.
            SingleTypeRegSet sourceMask = RBM_NONE;
            if (sourceRegCount < argNode->gtNumRegs)
            {
                sourceMask = genSingleTypeRegMask(argNode->GetRegNumByIdx(sourceRegCount));
            }
            sourceRegCount++;
            BuildUse(node, sourceMask);
        }
        srcCount += sourceRegCount;
        assert(src->isContained());
    }
    else
    {
        // On PPC64LE, HFA-like structs may have type TYP_LONG instead of TYP_STRUCT
        assert((src->TypeIs(TYP_STRUCT) || src->TypeIs(TYP_LONG)) && src->isContained());

        if (src->OperIs(GT_BLK))
        {
            // If the PUTARG_SPLIT clobbers only one register we may need an
            // extra internal register in case there is a conflict between the
            // source address register and target register.
            if (argNode->gtNumRegs == 1)
            {
                // We can use a ld/std sequence so we need an internal register
                buildInternalIntRegisterDefForNode(argNode, allRegs(TYP_INT) & ~argMask);
            }

            // We will generate code that loads from the BLK's address, which must be in a register.
            srcCount = BuildOperandUses(src->AsBlk()->Addr());
        }
        else
        {
            // We will generate all of the code for the GT_PUTARG_SPLIT and LCL_VAR/LCL_FLD as one contained operation.
            assert(src->OperIsLocalRead());
        }
    }
    buildInternalRegisterUses();
    BuildDefs(argNode, dstCount, argMask);
    
    // Note: For HFA structs, getDefType() in lsra.h returns the correct HFA element type,
    // so BuildDefs creates intervals with the correct register type automatically.
    
    return srcCount;
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
    GenTree* src = cast->CastOp();

    // Casts can have contained memory operands.
    if (src->isContained())
    {
        return BuildOperandUses(src);
    }

    SingleTypeRegSet candidates = RBM_NONE;
    
    // For float <-> int casts, we may need specific register types
    var_types srcType = genActualType(src->TypeGet());
    var_types dstType = cast->TypeGet();
    
    if (varTypeIsFloating(srcType) && !varTypeIsFloating(dstType))
    {
        // Float/Double to Int cast - source must be in float register
        candidates = allRegs(TYP_FLOAT);
	// PowerPC64 needs an internal FP register to hold the converted value
        // before transferring to integer register via stack
        buildInternalFloatRegisterDefForNode(cast);
    }
    else if (!varTypeIsFloating(srcType) && varTypeIsFloating(dstType))
    {
        // Int to Float/Double cast - source must be in int register
        candidates = allRegs(TYP_INT);
	// PowerPC64 needs an internal FP register to hold the integer value
	// after loading from stack before conversion
	buildInternalFloatRegisterDefForNode(cast);
    }

    BuildUse(src, candidates);
    // Build internal register definitions if any were requested
    buildInternalRegisterUses();
    BuildDef(cast);
    
    return 1;
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
    assert(argNode->gtOper == GT_PUTARG_STK);

    GenTree* src      = argNode->Data();
    int      srcCount = 0;

    // Do we have a TYP_STRUCT argument, if so it must be a multireg pass-by-value struct
    if (src->TypeIs(TYP_STRUCT))
    {
        // We will use store instructions that each write a register sized value

        if (src->OperIs(GT_FIELD_LIST))
        {
            assert(src->isContained());
            // We consume all of the items in the GT_FIELD_LIST
            for (GenTreeFieldList::Use& use : src->AsFieldList()->Uses())
            {
                BuildUse(use.GetNode());
                srcCount++;

#if defined(FEATURE_SIMD)
                if (use.GetType() == TYP_SIMD12)
                {
                    // Vector3 is read/written as two reads/writes: 8 byte and 4 byte.
                    // To assemble the vector properly we would need an additional int register.
                    buildInternalIntRegisterDefForNode(use.GetNode());
                }
#endif // FEATURE_SIMD
            }
        }
        else
        {
            // We can use a ld/std sequence so we need two internal registers for PPC64LE
            buildInternalIntRegisterDefForNode(argNode);
            buildInternalIntRegisterDefForNode(argNode);

            assert(src->isContained());

            if (src->OperIs(GT_BLK))
            {
                // Build uses for the address to load from.
                //
                srcCount = BuildOperandUses(src->AsBlk()->Addr());
            }
            else
            {
                // No source registers.
                assert(src->OperIs(GT_LCL_VAR, GT_LCL_FLD));
            }
        }
    }
    else
    {
        assert(!src->isContained());
        srcCount = BuildOperandUses(src);
    }
    buildInternalRegisterUses();
    return srcCount;
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

    // Accept GT_IND, GT_STOREIND, and other indirect operations
    // The function handles them all the same way	
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
    GenTree* dstAddr = blkNode->Addr();
    GenTree* src     = blkNode->Data();
    unsigned size    = blkNode->Size();

    GenTree* srcAddrOrFill = nullptr;

    SingleTypeRegSet dstAddrRegMask = RBM_NONE;
    SingleTypeRegSet srcRegMask     = RBM_NONE;
    SingleTypeRegSet sizeRegMask    = RBM_NONE;

    if (blkNode->OperIsInitBlkOp())
    {
        if (src->OperIs(GT_INIT_VAL))
        {
            assert(src->isContained());
            src = src->AsUnOp()->gtGetOp1();
        }

        srcAddrOrFill = src;

        switch (blkNode->gtBlkOpKind)
        {
            case GenTreeBlk::BlkOpKindUnroll:
                // For unrolled init, we may need an internal register
                if (dstAddr->isContained())
                {
                    buildInternalIntRegisterDefForNode(blkNode);
                }
                break;

            case GenTreeBlk::BlkOpKindLoop:
                // Needed for offsetReg
                buildInternalIntRegisterDefForNode(blkNode, availableIntRegs);
                break;

            default:
                unreached();
        }
    }
    else
    {
        if (src->OperIs(GT_IND))
        {
            assert(src->isContained());
            srcAddrOrFill = src->AsIndir()->Addr();
        }

        switch (blkNode->gtBlkOpKind)
        {
            case GenTreeBlk::BlkOpKindCpObjUnroll:
            {
                // We don't need to materialize the struct size but we still need
                // a temporary register to perform the sequence of loads and stores.
                // We can't use the special Write Barrier registers, so exclude them from the mask
                SingleTypeRegSet internalIntCandidates =
                    allRegs(TYP_INT) &
                    ~(RBM_WRITE_BARRIER_DST_BYREF | RBM_WRITE_BARRIER_SRC_BYREF).GetRegSetForType(IntRegisterType);
                buildInternalIntRegisterDefForNode(blkNode, internalIntCandidates);

                if (size >= 2 * REGSIZE_BYTES)
                {
                    // We will use multiple load/store to reduce code size
                    // so we need to reserve an extra internal register
                    buildInternalIntRegisterDefForNode(blkNode, internalIntCandidates);
                }

                // If we have a dest address we want it in RBM_WRITE_BARRIER_DST_BYREF.
                dstAddrRegMask = RBM_WRITE_BARRIER_DST_BYREF.GetIntRegSet();

                // If we have a source address we want it in REG_WRITE_BARRIER_SRC_BYREF.
                if (srcAddrOrFill != nullptr)
                {
                    assert(!srcAddrOrFill->isContained());
                    srcRegMask = RBM_WRITE_BARRIER_SRC_BYREF.GetIntRegSet();
                }
            }
            break;

            case GenTreeBlk::BlkOpKindUnroll:
            {
                // Need at least one internal register for loads/stores
                buildInternalIntRegisterDefForNode(blkNode);

                // For larger copies, we may need additional registers
                if (size >= 2 * REGSIZE_BYTES)
                {
                    buildInternalIntRegisterDefForNode(blkNode);
                }
            }
            break;

            default:
                unreached();
        }
    }

    if (sizeRegMask != RBM_NONE)
    {
        // Reserve a temp register for the block size argument.
        buildInternalIntRegisterDefForNode(blkNode, sizeRegMask);
    }

    int useCount = 0;

    if (!dstAddr->isContained())
    {
        useCount++;
        BuildUse(dstAddr, dstAddrRegMask);
    }
    else if (dstAddr->OperIsAddrMode())
    {
        useCount += BuildAddrUses(dstAddr->AsAddrMode()->Base());
    }

    if (srcAddrOrFill != nullptr)
    {
        if (!srcAddrOrFill->isContained())
        {
            useCount++;
            BuildUse(srcAddrOrFill, srcRegMask);
        }
        else if (srcAddrOrFill->OperIsAddrMode())
        {
            useCount += BuildAddrUses(srcAddrOrFill->AsAddrMode()->Base());
        }
    }

    buildInternalRegisterUses();
    regMaskTP killMask = getKillSetForBlockStore(blkNode);
    BuildKills(blkNode, killMask);
    return useCount;
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
        case GT_CNS_DBL:
        {
            // For PPC64LE, we need an internal integer register to transfer
            // the floating-point constant from GPR to FPR via stack
            buildInternalIntRegisterDefForNode(tree);
            buildInternalRegisterUses();
        }
            FALLTHROUGH;

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

 case GT_STOREIND:
 {
            assert(dstCount == 0);

            if (compiler->codeGen->gcInfo.gcIsWriteBarrierStoreIndNode(tree->AsStoreInd()))
            {
                srcCount = BuildGCWriteBarrier(tree);
                break;
            }

            srcCount = BuildIndir(tree->AsIndir());
            if (!tree->gtGetOp2()->isContained())
            {
                BuildUse(tree->gtGetOp2());
                srcCount++;
            }
 }
        break;

 case GT_LEA:
 {
            GenTreeAddrMode* lea = tree->AsAddrMode();

            GenTree* base  = lea->Base();
            GenTree* index = lea->Index();
            int      cns   = lea->Offset();

            // This LEA is instantiating an address, so we set up the srcCount here.
            srcCount = 0;
            if (base != nullptr)
            {
                srcCount++;
                BuildUse(base);
            }
            if (index != nullptr)
            {
                srcCount++;
                BuildUse(index);
            }
            assert(dstCount == 1);

            // On PPC64LE we may need a single internal register
            // PowerPC doesn't have a direct LEA instruction like x86/x64
            // We need to compute: base + (index * scale) + offset
            // If we have both index and offset, or if offset is large, we need an internal register
            if ((index != nullptr) && (cns != 0))
            {
                // PPC64 needs to compute index contribution separately then add offset
                buildInternalIntRegisterDefForNode(tree);
            }
            else if ((cns != 0) && ((cns < -32768) || (cns > 32767)))
            {
                // This offset can't be contained in the addi instruction (16-bit signed immediate)
                // so we need an internal register
                buildInternalIntRegisterDefForNode(tree);
            }
            buildInternalRegisterUses();
            BuildDef(tree);
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

	case GT_PUTARG_REG:
	           srcCount = BuildPutArgReg(tree->AsUnOp());
	           break;

	case GT_PUTARG_STK:
	           srcCount = BuildPutArgStk(tree->AsPutArgStk());
	           break;

	case GT_PUTARG_SPLIT:
	           srcCount = BuildPutArgSplit(tree->AsPutArgSplit());
	           dstCount = tree->AsPutArgSplit()->gtNumRegs;
	           break;

	case GT_NOP:
            srcCount = 0;
            assert(tree->TypeIs(TYP_VOID));
            assert(dstCount == 0);
            break;

	case GT_STORE_LCL_VAR:
	case GT_STORE_LCL_FLD:
	    srcCount = BuildStoreLoc(tree->AsLclVarCommon());
            break;

	case GT_LCL_VAR:
	case GT_LCL_FLD:
	    // Local variable or field load - no sources, produces one result
	    srcCount = 0;
	    // Only build a def if the node is not contained
	    if (!tree->isContained())
	    {
	        BuildDef(tree);
	    }
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
              
	      case GT_NOT:
                 // Unary NOT operation - only one source operand
                 srcCount = BuildOperandUses(tree->gtGetOp1(), RBM_NONE);
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

	      case GT_CAST:
	             {
	       	  assert(dstCount == 1);
	                 srcCount = BuildCast(tree->AsCast());
	      }
	      break;

	      case GT_STORE_BLK:
	          srcCount = BuildBlockStore(tree->AsBlk());
	          break;
	      case GT_CATCH_ARG:
                  srcCount = 0;
                  assert(dstCount == 1);
                  BuildDef(tree, RBM_EXCEPTION_OBJECT.GetIntRegSet());
                  break;

	      case GT_INDEX_ADDR:  
    		  {
         	  	assert(dstCount == 1);
        	  	srcCount = BuildBinaryUses(tree->AsOp());
          
        	  // PowerPC64 may need internal registers for:
        	  // 1. Computing the scaled index (if not power-of-2 or needs widening)
        	  // 2. Large offsets that don't fit in immediate
        	 	 buildInternalIntRegisterDefForNode(tree);
          
        	  // If index needs widening from 32-bit to 64-bit, we may need another temp
        	 	 if (!tree->AsIndexAddr()->Index()->TypeIs(TYP_I_IMPL) &&
        	 	     !(isPow2(tree->AsIndexAddr()->gtElemSize) && (tree->AsIndexAddr()->gtElemSize <= 32768)))
        	 	 {
        	 	     buildInternalIntRegisterDefForNode(tree);
        	 	 }
          
        	 	 buildInternalRegisterUses();
        	 	 BuildDef(tree);
     		 }
		  break;
	      default:
	      {
	      printf("LSRA BuildNode: Unhandled operation: %s (oper=%d)\n", 
                     GenTree::OpName(tree->OperGet()), tree->OperGet());
            	     
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
	assert(pDstCount != nullptr);
    
    // PowerPC64LE: Hardware intrinsics not yet implemented
    // For now, return basic counts to avoid crash
    *pDstCount = intrinsicTree->IsValue() ? 1 : 0;
    
    // Build operands normally
    int srcCount = 0;
    for (GenTree* operand : intrinsicTree->Operands())
    {
        srcCount += BuildOperandUses(operand);
    }
    
    return srcCount;
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
    int       srcCount     = 0;
    Interval* rmwInterval  = nullptr;
    bool      rmwIsLastUse = false;
    
    if (rmwNode != nullptr)
    {
        if (isCandidateLocalRef(rmwNode))
        {
            rmwInterval  = getIntervalForLocalVarNode(rmwNode->AsLclVar());
            rmwIsLastUse = rmwNode->AsLclVar()->IsLastUse(0);
        }
    }
    
    if (treeNode->OperIsFieldList())
    {
        assert(compiler->info.compNeedsConsecutiveRegisters);

        unsigned     regCount    = 0;
        RefPosition* firstRefPos = nullptr;
        RefPosition* currRefPos  = nullptr;
        RefPosition* lastRefPos  = nullptr;

        NextConsecutiveRefPositionsMap* refPositionMap = getNextConsecutiveRefPositionsMap();
        
        for (GenTreeFieldList::Use& use : treeNode->AsFieldList()->Uses())
        {
            RefPosition*        restoreRefPos = nullptr;
            RefPositionIterator prevRefPos    = refPositions.backPosition();

            currRefPos = BuildUse(use.GetNode(), RBM_NONE, 0);

            RefPositionIterator tailRefPos = refPositions.backPosition();
            assert(tailRefPos == currRefPos);
            prevRefPos++;
            
            if (prevRefPos != tailRefPos)
            {
                restoreRefPos = prevRefPos;
                assert(restoreRefPos->refType == RefTypeUpperVectorRestore);
            }

            currRefPos->needsConsecutive = true;
            currRefPos->regCount         = 0;
            
#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
            if (restoreRefPos != nullptr)
            {
                restoreRefPos->needsConsecutive = true;
                restoreRefPos->regCount         = 0;
                
                if (firstRefPos == nullptr)
                {
                    firstRefPos = currRefPos;
                }
                
                refPositionMap->Set(lastRefPos, restoreRefPos, LinearScan::NextConsecutiveRefPositionsMap::Overwrite);
                refPositionMap->Set(restoreRefPos, currRefPos, LinearScan::NextConsecutiveRefPositionsMap::Overwrite);

                if (rmwNode != nullptr)
                {
                    if ((restoreRefPos->getInterval() != rmwInterval) || (!rmwIsLastUse && !restoreRefPos->lastUse))
                    {
                        setDelayFree(restoreRefPos);
                    }
                }
            }
            else
#endif // FEATURE_PARTIAL_SIMD_CALLEE_SAVE
            {
                if (firstRefPos == nullptr)
                {
                    firstRefPos = currRefPos;
                }
                refPositionMap->Set(lastRefPos, currRefPos, LinearScan::NextConsecutiveRefPositionsMap::Overwrite);
            }

            refPositionMap->Set(currRefPos, nullptr);

            lastRefPos = currRefPos;
            regCount++;
            
            if (rmwNode != nullptr)
            {
                if ((currRefPos->getInterval() != rmwInterval) || (!rmwIsLastUse && !currRefPos->lastUse))
                {
                    setDelayFree(currRefPos);
                }
            }
        }

        firstRefPos->regCount = regCount;

#ifdef DEBUG
        currRefPos = firstRefPos;
        while (currRefPos != nullptr)
        {
            currRefPos->minRegCandidateCount = regCount;
            currRefPos                       = getNextConsecutiveRefPosition(currRefPos);
        }
#endif
        srcCount += regCount;
    }
    else
    {
        RefPositionIterator refPositionMark   = refPositions.backPosition();
        int                 refPositionsAdded = BuildOperandUses(treeNode);

        if (rmwNode != nullptr)
        {
            RefPositionIterator iter = refPositionMark;

            for (iter++; iter != refPositions.end(); iter++)
            {
                RefPosition* refPositionAdded = &(*iter);

                if ((refPositionAdded->getInterval() != rmwInterval) || (!rmwIsLastUse && !refPositionAdded->lastUse))
                {
                    setDelayFree(refPositionAdded);
                }
            }
        }

        srcCount += refPositionsAdded;
    }

    return srcCount;
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
    assert(registerCount > 1);
    assert(compiler->info.compNeedsConsecutiveRegisters);

    RefPosition* currRefPos = nullptr;
    RefPosition* lastRefPos = nullptr;

    NextConsecutiveRefPositionsMap* refPositionMap = getNextConsecutiveRefPositionsMap();
    
    for (int fieldIdx = 0; fieldIdx < registerCount; fieldIdx++)
    {
        currRefPos                   = BuildDef(treeNode, RBM_NONE, fieldIdx);
        currRefPos->needsConsecutive = true;
        currRefPos->regCount         = 0;
        
#ifdef DEBUG
        // Set the minimum register candidates needed for stress to work.
        currRefPos->minRegCandidateCount = registerCount;
#endif
        
        if (fieldIdx == 0)
        {
            // Set `regCount` to actual consecutive registers count for first ref-position.
            // For others, set 0 so we can identify that this is non-first RefPosition.
            currRefPos->regCount = registerCount;
        }

        refPositionMap->Set(lastRefPos, currRefPos, LinearScan::NextConsecutiveRefPositionsMap::Overwrite);
        refPositionMap->Set(currRefPos, nullptr);

        lastRefPos = currRefPos;
    }
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
