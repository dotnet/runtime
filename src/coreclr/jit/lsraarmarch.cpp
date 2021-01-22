// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX              Register Requirements for ARM and ARM64 common code          XX
XX                                                                           XX
XX  This encapsulates common logic for setting register requirements for     XX
XX  the ARM and ARM64 architectures.                                         XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifdef TARGET_ARMARCH // This file is ONLY used for ARM and ARM64 architectures

#include "jit.h"
#include "sideeffects.h"
#include "lower.h"
#include "lsra.h"

//------------------------------------------------------------------------
// BuildIndir: Specify register requirements for address expression
//                       of an indirection operation.
//
// Arguments:
//    indirTree - GT_IND, GT_STOREIND or block gentree node
//
// Return Value:
//    The number of sources consumed by this node.
//
int LinearScan::BuildIndir(GenTreeIndir* indirTree)
{
    // struct typed indirs are expected only on rhs of a block copy,
    // but in this case they must be contained.
    assert(indirTree->TypeGet() != TYP_STRUCT);

    GenTree* addr  = indirTree->Addr();
    GenTree* index = nullptr;
    int      cns   = 0;

#ifdef TARGET_ARM
    // Unaligned loads/stores for floating point values must first be loaded into integer register(s)
    if (indirTree->gtFlags & GTF_IND_UNALIGNED)
    {
        var_types type = TYP_UNDEF;
        if (indirTree->OperGet() == GT_STOREIND)
        {
            type = indirTree->AsStoreInd()->Data()->TypeGet();
        }
        else if (indirTree->OperGet() == GT_IND)
        {
            type = indirTree->TypeGet();
        }

        if (type == TYP_FLOAT)
        {
            buildInternalIntRegisterDefForNode(indirTree);
        }
        else if (type == TYP_DOUBLE)
        {
            buildInternalIntRegisterDefForNode(indirTree);
            buildInternalIntRegisterDefForNode(indirTree);
        }
    }
#endif

    if (addr->isContained())
    {
        if (addr->OperGet() == GT_LEA)
        {
            GenTreeAddrMode* lea = addr->AsAddrMode();
            index                = lea->Index();
            cns                  = lea->Offset();

            // On ARM we may need a single internal register
            // (when both conditions are true then we still only need a single internal register)
            if ((index != nullptr) && (cns != 0))
            {
                // ARM does not support both Index and offset so we need an internal register
                buildInternalIntRegisterDefForNode(indirTree);
            }
            else if (!emitter::emitIns_valid_imm_for_ldst_offset(cns, emitTypeSize(indirTree)))
            {
                // This offset can't be contained in the ldr/str instruction, so we need an internal register
                buildInternalIntRegisterDefForNode(indirTree);
            }
        }
#ifdef TARGET_ARM64
        else if (addr->OperGet() == GT_CLS_VAR_ADDR)
        {
            // Reserve int to load constant from memory (IF_LARGELDC)
            buildInternalIntRegisterDefForNode(indirTree);
        }
#endif // TARGET_ARM64
    }

#ifdef FEATURE_SIMD
    if (indirTree->TypeGet() == TYP_SIMD12)
    {
        // If indirTree is of TYP_SIMD12, addr is not contained. See comment in LowerIndir().
        assert(!addr->isContained());

        // Vector3 is read/written as two reads/writes: 8 byte and 4 byte.
        // To assemble the vector properly we would need an additional int register
        buildInternalIntRegisterDefForNode(indirTree);
    }
#endif // FEATURE_SIMD

    int srcCount = BuildIndirUses(indirTree);
    buildInternalRegisterUses();

    if (!indirTree->OperIs(GT_STOREIND, GT_NULLCHECK))
    {
        BuildDef(indirTree);
    }
    return srcCount;
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
    bool                  hasMultiRegRetVal = false;
    const ReturnTypeDesc* retTypeDesc       = nullptr;
    regMaskTP             dstCandidates     = RBM_NONE;

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

    GenTree*  ctrlExpr           = call->gtControlExpr;
    regMaskTP ctrlExprCandidates = RBM_NONE;
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
            // Fast tail call - make sure that call target is always computed in R12(ARM32)/IP0(ARM64)
            // so that epilog sequence can generate "br xip0/r12" to achieve fast tail call.
            ctrlExprCandidates = RBM_FASTTAILCALL_TARGET;
        }
    }
    else if (call->IsR2ROrVirtualStubRelativeIndir())
    {
        buildInternalIntRegisterDefForNode(call);
    }
#ifdef TARGET_ARM
    else
    {
        buildInternalIntRegisterDefForNode(call);
    }

    if (call->NeedsNullCheck())
    {
        buildInternalIntRegisterDefForNode(call);
    }

#endif // TARGET_ARM

    RegisterType registerType = call->TypeGet();

// Set destination candidates for return value of the call.

#ifdef TARGET_ARM
    if (call->IsHelperCall(compiler, CORINFO_HELP_INIT_PINVOKE_FRAME))
    {
        // The ARM CORINFO_HELP_INIT_PINVOKE_FRAME helper uses a custom calling convention that returns with
        // TCB in REG_PINVOKE_TCB. fgMorphCall() sets the correct argument registers.
        dstCandidates = RBM_PINVOKE_TCB;
    }
    else
#endif // TARGET_ARM
        if (hasMultiRegRetVal)
    {
        assert(retTypeDesc != nullptr);
        dstCandidates = retTypeDesc->GetABIReturnRegs();
    }
    else if (varTypeUsesFloatArgReg(registerType))
    {
        dstCandidates = RBM_FLOATRET;
    }
    else if (registerType == TYP_LONG)
    {
        dstCandidates = RBM_LNGRET;
    }
    else
    {
        dstCandidates = RBM_INTRET;
    }

    // First, count reg args
    // Each register argument corresponds to one source.
    bool callHasFloatRegArgs = false;

    for (GenTreeCall::Use& arg : call->LateArgs())
    {
        GenTree* argNode = arg.GetNode();

#ifdef DEBUG
        // During Build, we only use the ArgTabEntry for validation,
        // as getting it is rather expensive.
        fgArgTabEntry* curArgTabEntry = compiler->gtArgEntryByNode(call, argNode);
        regNumber      argReg         = curArgTabEntry->GetRegNum();
        assert(curArgTabEntry != nullptr);
#endif

        if (argNode->gtOper == GT_PUTARG_STK)
        {
            // late arg that is not passed in a register
            assert(curArgTabEntry->GetRegNum() == REG_STK);
            // These should never be contained.
            assert(!argNode->isContained());
            continue;
        }

        // A GT_FIELD_LIST has a TYP_VOID, but is used to represent a multireg struct
        if (argNode->OperGet() == GT_FIELD_LIST)
        {
            assert(argNode->isContained());

            // There could be up to 2-4 PUTARG_REGs in the list (3 or 4 can only occur for HFAs)
            for (GenTreeFieldList::Use& use : argNode->AsFieldList()->Uses())
            {
#ifdef DEBUG
                assert(use.GetNode()->OperIs(GT_PUTARG_REG));
                assert(use.GetNode()->GetRegNum() == argReg);
                // Update argReg for the next putarg_reg (if any)
                argReg = genRegArgNext(argReg);

#if defined(TARGET_ARM)
                // A double register is modelled as an even-numbered single one
                if (use.GetNode()->TypeGet() == TYP_DOUBLE)
                {
                    argReg = genRegArgNext(argReg);
                }
#endif // TARGET_ARM
#endif
                BuildUse(use.GetNode(), genRegMask(use.GetNode()->GetRegNum()));
                srcCount++;
            }
        }
#if FEATURE_ARG_SPLIT
        else if (argNode->OperGet() == GT_PUTARG_SPLIT)
        {
            unsigned regCount = argNode->AsPutArgSplit()->gtNumRegs;
            assert(regCount == curArgTabEntry->numRegs);
            for (unsigned int i = 0; i < regCount; i++)
            {
                BuildUse(argNode, genRegMask(argNode->AsPutArgSplit()->GetRegNumByIdx(i)), i);
            }
            srcCount += regCount;
        }
#endif // FEATURE_ARG_SPLIT
        else
        {
            assert(argNode->OperIs(GT_PUTARG_REG));
            assert(argNode->GetRegNum() == argReg);
            HandleFloatVarArgs(call, argNode, &callHasFloatRegArgs);
#ifdef TARGET_ARM
            // The `double` types have been transformed to `long` on armel,
            // while the actual long types have been decomposed.
            // On ARM we may have bitcasts from DOUBLE to LONG.
            if (argNode->TypeGet() == TYP_LONG)
            {
                assert(argNode->IsMultiRegNode());
                BuildUse(argNode, genRegMask(argNode->GetRegNum()), 0);
                BuildUse(argNode, genRegMask(genRegArgNext(argNode->GetRegNum())), 1);
                srcCount += 2;
            }
            else
#endif // TARGET_ARM
            {
                BuildUse(argNode, genRegMask(argNode->GetRegNum()));
                srcCount++;
            }
        }
    }

#ifdef DEBUG
    // Now, count stack args
    // Note that these need to be computed into a register, but then
    // they're just stored to the stack - so the reg doesn't
    // need to remain live until the call.  In fact, it must not
    // because the code generator doesn't actually consider it live,
    // so it can't be spilled.

    for (GenTreeCall::Use& use : call->Args())
    {
        GenTree* arg = use.GetNode();

        // Skip arguments that have been moved to the Late Arg list
        if ((arg->gtFlags & GTF_LATE_ARG) == 0)
        {
            fgArgTabEntry* curArgTabEntry = compiler->gtArgEntryByNode(call, arg);
            assert(curArgTabEntry != nullptr);
#if FEATURE_ARG_SPLIT
            // PUTARG_SPLIT nodes must be in the gtCallLateArgs list, since they
            // define registers used by the call.
            assert(arg->OperGet() != GT_PUTARG_SPLIT);
#endif // FEATURE_ARG_SPLIT
            if (arg->gtOper == GT_PUTARG_STK)
            {
                assert(curArgTabEntry->GetRegNum() == REG_STK);
            }
            else
            {
                assert(!arg->IsValue() || arg->IsUnusedValue());
            }
        }
    }
#endif // DEBUG

    // If it is a fast tail call, it is already preferenced to use IP0.
    // Therefore, no need set src candidates on call tgt again.
    if (call->IsVarargs() && callHasFloatRegArgs && !call->IsFastTailCall() && (ctrlExpr != nullptr))
    {
        NYI_ARM("float reg varargs");

        // Don't assign the call target to any of the argument registers because
        // we will use them to also pass floating point arguments as required
        // by Arm64 ABI.
        ctrlExprCandidates = allRegs(TYP_INT) & ~(RBM_ARG_REGS);
    }

    if (ctrlExpr != nullptr)
    {
        BuildUse(ctrlExpr, ctrlExprCandidates);
        srcCount++;
    }

    buildInternalRegisterUses();

    // Now generate defs and kills.
    regMaskTP killMask = getKillSetForCall(call);
    BuildDefsWithKills(call, dstCount, dstCandidates, killMask);
    return srcCount;
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

    GenTree* putArgChild = argNode->gtGetOp1();

    int srcCount = 0;

    // Do we have a TYP_STRUCT argument (or a GT_FIELD_LIST), if so it must be a multireg pass-by-value struct
    if ((putArgChild->TypeGet() == TYP_STRUCT) || (putArgChild->OperGet() == GT_FIELD_LIST))
    {
        // We will use store instructions that each write a register sized value

        if (putArgChild->OperGet() == GT_FIELD_LIST)
        {
            assert(putArgChild->isContained());
            // We consume all of the items in the GT_FIELD_LIST
            for (GenTreeFieldList::Use& use : putArgChild->AsFieldList()->Uses())
            {
                BuildUse(use.GetNode());
                srcCount++;
            }
        }
        else
        {
            // We can use a ldp/stp sequence so we need two internal registers for ARM64; one for ARM.
            buildInternalIntRegisterDefForNode(argNode);
#ifdef TARGET_ARM64
            buildInternalIntRegisterDefForNode(argNode);
#endif // TARGET_ARM64

            if (putArgChild->OperGet() == GT_OBJ)
            {
                assert(putArgChild->isContained());
                GenTree* objChild = putArgChild->gtGetOp1();
                if (objChild->OperGet() == GT_LCL_VAR_ADDR)
                {
                    // We will generate all of the code for the GT_PUTARG_STK, the GT_OBJ and the GT_LCL_VAR_ADDR
                    // as one contained operation, and there are no source registers.
                    //
                    assert(objChild->isContained());
                }
                else
                {
                    // We will generate all of the code for the GT_PUTARG_STK and its child node
                    // as one contained operation
                    //
                    srcCount = BuildOperandUses(objChild);
                }
            }
            else
            {
                // No source registers.
                putArgChild->OperIs(GT_LCL_VAR);
            }
        }
    }
    else
    {
        assert(!putArgChild->isContained());
        srcCount = BuildOperandUses(putArgChild);
    }
    buildInternalRegisterUses();
    return srcCount;
}

#if FEATURE_ARG_SPLIT
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

    GenTree* putArgChild = argNode->gtGetOp1();

    // Registers for split argument corresponds to source
    int dstCount = argNode->gtNumRegs;

    regNumber argReg  = argNode->GetRegNum();
    regMaskTP argMask = RBM_NONE;
    for (unsigned i = 0; i < argNode->gtNumRegs; i++)
    {
        regNumber thisArgReg = (regNumber)((unsigned)argReg + i);
        argMask |= genRegMask(thisArgReg);
        argNode->SetRegNumByIdx(thisArgReg, i);
    }

    if (putArgChild->OperGet() == GT_FIELD_LIST)
    {
        // Generated code:
        // 1. Consume all of the items in the GT_FIELD_LIST (source)
        // 2. Store to target slot and move to target registers (destination) from source
        //
        unsigned sourceRegCount = 0;

        // To avoid redundant moves, have the argument operand computed in the
        // register in which the argument is passed to the call.

        for (GenTreeFieldList::Use& use : putArgChild->AsFieldList()->Uses())
        {
            GenTree* node = use.GetNode();
            assert(!node->isContained());
            // The only multi-reg nodes we should see are OperIsMultiRegOp()
            unsigned currentRegCount;
#ifdef TARGET_ARM
            if (node->OperIsMultiRegOp())
            {
                currentRegCount = node->AsMultiRegOp()->GetRegCount();
            }
            else
#endif // TARGET_ARM
            {
                assert(!node->IsMultiRegNode());
                currentRegCount = 1;
            }
            // Consume all the registers, setting the appropriate register mask for the ones that
            // go into registers.
            for (unsigned regIndex = 0; regIndex < currentRegCount; regIndex++)
            {
                regMaskTP sourceMask = RBM_NONE;
                if (sourceRegCount < argNode->gtNumRegs)
                {
                    sourceMask = genRegMask((regNumber)((unsigned)argReg + sourceRegCount));
                }
                sourceRegCount++;
                BuildUse(node, sourceMask, regIndex);
            }
        }
        srcCount += sourceRegCount;
        assert(putArgChild->isContained());
    }
    else
    {
        assert(putArgChild->TypeGet() == TYP_STRUCT);
        assert(putArgChild->OperGet() == GT_OBJ);

        // We can use a ldr/str sequence so we need an internal register
        buildInternalIntRegisterDefForNode(argNode, allRegs(TYP_INT) & ~argMask);

        GenTree* objChild = putArgChild->gtGetOp1();
        if (objChild->OperGet() == GT_LCL_VAR_ADDR)
        {
            // We will generate all of the code for the GT_PUTARG_SPLIT, the GT_OBJ and the GT_LCL_VAR_ADDR
            // as one contained operation
            //
            assert(objChild->isContained());
        }
        else
        {
            srcCount = BuildIndirUses(putArgChild->AsIndir());
        }
        assert(putArgChild->isContained());
    }
    buildInternalRegisterUses();
    BuildDefs(argNode, dstCount, argMask);
    return srcCount;
}
#endif // FEATURE_ARG_SPLIT

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

    regMaskTP dstAddrRegMask = RBM_NONE;
    regMaskTP srcRegMask     = RBM_NONE;
    regMaskTP sizeRegMask    = RBM_NONE;

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
                break;

            case GenTreeBlk::BlkOpKindHelper:
                assert(!src->isContained());
                dstAddrRegMask = RBM_ARG_0;
                srcRegMask     = RBM_ARG_1;
                sizeRegMask    = RBM_ARG_2;
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

        if (blkNode->OperIs(GT_STORE_OBJ))
        {
            // We don't need to materialize the struct size but we still need
            // a temporary register to perform the sequence of loads and stores.
            // We can't use the special Write Barrier registers, so exclude them from the mask
            regMaskTP internalIntCandidates =
                allRegs(TYP_INT) & ~(RBM_WRITE_BARRIER_DST_BYREF | RBM_WRITE_BARRIER_SRC_BYREF);
            buildInternalIntRegisterDefForNode(blkNode, internalIntCandidates);

            if (size >= 2 * REGSIZE_BYTES)
            {
                // We will use ldp/stp to reduce code size and improve performance
                // so we need to reserve an extra internal register
                buildInternalIntRegisterDefForNode(blkNode, internalIntCandidates);
            }

            // If we have a dest address we want it in RBM_WRITE_BARRIER_DST_BYREF.
            dstAddrRegMask = RBM_WRITE_BARRIER_DST_BYREF;

            // If we have a source address we want it in REG_WRITE_BARRIER_SRC_BYREF.
            // Otherwise, if it is a local, codegen will put its address in REG_WRITE_BARRIER_SRC_BYREF,
            // which is killed by a StoreObj (and thus needn't be reserved).
            if (srcAddrOrFill != nullptr)
            {
                assert(!srcAddrOrFill->isContained());
                srcRegMask = RBM_WRITE_BARRIER_SRC_BYREF;
            }
        }
        else
        {
            switch (blkNode->gtBlkOpKind)
            {
                case GenTreeBlk::BlkOpKindUnroll:
                    buildInternalIntRegisterDefForNode(blkNode);
#ifdef TARGET_ARM64
                    if (size >= 2 * REGSIZE_BYTES)
                    {
                        // We will use ldp/stp to reduce code size and improve performance
                        // so we need to reserve an extra internal register
                        buildInternalIntRegisterDefForNode(blkNode);
                    }
#endif
                    break;

                case GenTreeBlk::BlkOpKindHelper:
                    dstAddrRegMask = RBM_ARG_0;
                    if (srcAddrOrFill != nullptr)
                    {
                        assert(!srcAddrOrFill->isContained());
                        srcRegMask = RBM_ARG_1;
                    }
                    sizeRegMask = RBM_ARG_2;
                    break;

                default:
                    unreached();
            }
        }
    }

    if (!blkNode->OperIs(GT_STORE_DYN_BLK) && (sizeRegMask != RBM_NONE))
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

    if (blkNode->OperIs(GT_STORE_DYN_BLK))
    {
        useCount++;
        BuildUse(blkNode->AsDynBlk()->gtDynamicSize, sizeRegMask);
    }

    buildInternalRegisterUses();
    regMaskTP killMask = getKillSetForBlockStore(blkNode);
    BuildDefsWithKills(blkNode, 0, RBM_NONE, killMask);
    return useCount;
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
    GenTree* src = cast->gtGetOp1();

    const var_types srcType  = genActualType(src->TypeGet());
    const var_types castType = cast->gtCastType;

#ifdef TARGET_ARM
    assert(!varTypeIsLong(srcType) || (src->OperIs(GT_LONG) && src->isContained()));

    // Floating point to integer casts requires a temporary register.
    if (varTypeIsFloating(srcType) && !varTypeIsFloating(castType))
    {
        buildInternalFloatRegisterDefForNode(cast, RBM_ALLFLOAT);
        setInternalRegsDelayFree = true;
    }
#else
    // Overflow checking cast from TYP_LONG to TYP_INT requires a temporary register to
    // store the min and max immediate values that cannot be encoded in the CMP instruction.
    if (cast->gtOverflow() && varTypeIsLong(srcType) && !cast->IsUnsigned() && (castType == TYP_INT))
    {
        buildInternalIntRegisterDefForNode(cast);
    }
#endif

    int srcCount = BuildOperandUses(src);
    buildInternalRegisterUses();
    BuildDef(cast);
    return srcCount;
}

#endif // TARGET_ARMARCH
