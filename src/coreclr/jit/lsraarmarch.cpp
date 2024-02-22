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
//    indirTree - GT_IND, GT_STOREIND or block GenTree node
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
            // Fast tail call - make sure that call target is always computed in volatile registers
            // that will not be overridden by epilog sequence.
            ctrlExprCandidates = allRegs(TYP_INT) & RBM_INT_CALLEE_TRASH & ~RBM_LR;
            if (compiler->getNeedsGSSecurityCookie())
            {
                ctrlExprCandidates &= ~(genRegMask(REG_GSCOOKIE_TMP_0) | genRegMask(REG_GSCOOKIE_TMP_1));
            }
            assert(ctrlExprCandidates != RBM_NONE);
        }
    }
    else if (call->IsR2ROrVirtualStubRelativeIndir())
    {
        // For R2R and VSD we have stub address in REG_R2R_INDIRECT_PARAM
        // and will load call address into the temp register from this register.
        regMaskTP candidates = RBM_NONE;
        if (call->IsFastTailCall())
        {
            candidates = allRegs(TYP_INT) & RBM_INT_CALLEE_TRASH;
            assert(candidates != RBM_NONE);
        }

        buildInternalIntRegisterDefForNode(call, candidates);
    }
#ifdef TARGET_ARM
    else
    {
        buildInternalIntRegisterDefForNode(call);
    }

    if (call->NeedsNullCheck())
    {
        // For fast tailcalls we are very constrained here as the only two
        // volatile registers left are lr and r12 and r12 might be needed for
        // the target. We do not handle these constraints on the same
        // refposition too well so we help ourselves a bit here by forcing the
        // null check with LR.
        regMaskTP candidates = call->IsFastTailCall() ? RBM_LR : RBM_NONE;
        buildInternalIntRegisterDefForNode(call, candidates);
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

    for (CallArg& arg : call->gtArgs.LateArgs())
    {
        CallArgABIInformation& abiInfo = arg.AbiInfo;
        GenTree*               argNode = arg.GetLateNode();

#ifdef DEBUG
        regNumber argReg = abiInfo.GetRegNum();
#endif

        if (argNode->gtOper == GT_PUTARG_STK)
        {
            // late arg that is not passed in a register
            assert(abiInfo.GetRegNum() == REG_STK);
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
        else if (argNode->OperGet() == GT_PUTARG_SPLIT)
        {
            unsigned regCount = argNode->AsPutArgSplit()->gtNumRegs;
            assert(regCount == abiInfo.NumRegs);
            for (unsigned int i = 0; i < regCount; i++)
            {
                BuildUse(argNode, genRegMask(argNode->AsPutArgSplit()->GetRegNumByIdx(i)), i);
            }
            srcCount += regCount;
        }
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

    for (CallArg& arg : call->gtArgs.EarlyArgs())
    {
        GenTree* argNode = arg.GetEarlyNode();

        // Skip arguments that have been moved to the late list
        if (arg.GetLateNode() == nullptr)
        {
            // PUTARG_SPLIT nodes must be in the late list, since they
            // define registers used by the call.
            assert(argNode->OperGet() != GT_PUTARG_SPLIT);
            if (argNode->gtOper == GT_PUTARG_STK)
            {
                assert(arg.AbiInfo.GetRegNum() == REG_STK);
            }
            else
            {
                assert(!argNode->IsValue() || argNode->IsUnusedValue());
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
#ifdef TARGET_ARM64
        if (compiler->IsTargetAbi(CORINFO_NATIVEAOT_ABI) && TargetOS::IsUnix && (call->gtArgs.CountArgs() == 0) &&
            ctrlExpr->IsTlsIconHandle())
        {
            // For NativeAOT linux/arm64, we generate the needed code as part of
            // call node because the generated code has to be in specific format
            // that linker can patch. As such, the code needs specific registers
            // that we will attach to this node to guarantee that they are available
            // during generating this node.
            assert(call->gtFlags & GTF_TLS_GET_ADDR);
            newRefPosition(REG_R0, currentLoc, RefTypeFixedReg, nullptr, genRegMask(REG_R0));
            newRefPosition(REG_R1, currentLoc, RefTypeFixedReg, nullptr, genRegMask(REG_R1));
            ctrlExprCandidates = genRegMask(REG_R2);
        }
#endif
        BuildUse(ctrlExpr, ctrlExprCandidates);
        srcCount++;
    }

    buildInternalRegisterUses();

    // Now generate defs and kills.
    regMaskTP killMask = getKillSetForCall(call);
    BuildDefsWithKills(call, dstCount, dstCandidates, killMask);

#ifdef SWIFT_SUPPORT
    if ((call->gtCallMoreFlags & GTF_CALL_M_SWIFT_ERROR_HANDLING) != 0)
    {
        // Tree is a Swift call with error handling; error register should have been killed
        assert(call->unmgdCallConv == CorInfoCallConvExtension::Swift);
        assert((killMask & RBM_SWIFT_ERROR) != 0);

        // After a Swift call that might throw returns, we expect the error register to be consumed
        // by a GT_SWIFT_ERROR node. However, we want to ensure the error register won't be trashed
        // before GT_SWIFT_ERROR can consume it.
        // (For example, the PInvoke epilog comes before the error register store.)
        // To do so, delay the freeing of the error register until the next node.
        // This only works if the next node after the call is the GT_SWIFT_ERROR node.
        // (InsertPInvokeCallEpilog should have moved the GT_SWIFT_ERROR node during lowering.)
        assert(call->gtNext != nullptr);
        assert(call->gtNext->OperIs(GT_SWIFT_ERROR));

        // We could use RefTypeKill, but RefTypeFixedReg is used less commonly, so the check for delayRegFree
        // during register allocation should be cheaper in terms of TP.
        RefPosition* pos  = newRefPosition(REG_SWIFT_ERROR, currentLoc, RefTypeFixedReg, call, RBM_SWIFT_ERROR);
        pos->delayRegFree = true;
    }
#endif // SWIFT_SUPPORT

    // No args are placed in registers anymore.
    placedArgRegs      = RBM_NONE;
    numPlacedArgLocals = 0;
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
            // We can use a ldp/stp sequence so we need two internal registers for ARM64; one for ARM.
            buildInternalIntRegisterDefForNode(argNode);
#ifdef TARGET_ARM64
            buildInternalIntRegisterDefForNode(argNode);
#endif // TARGET_ARM64

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
#if defined(FEATURE_SIMD)
        if (compAppleArm64Abi() && argNode->GetStackByteSize() == 12)
        {
            // Vector3 is read/written as two reads/writes: 8 byte and 4 byte.
            // To assemble the vector properly we would need an additional int register.
            // The other platforms can write it as 16-byte using 1 write.
            buildInternalIntRegisterDefForNode(argNode);
        }
#endif // FEATURE_SIMD
    }
    buildInternalRegisterUses();
    return srcCount;
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

    regNumber argReg  = argNode->GetRegNum();
    regMaskTP argMask = RBM_NONE;
    for (unsigned i = 0; i < argNode->gtNumRegs; i++)
    {
        regNumber thisArgReg = (regNumber)((unsigned)argReg + i);
        argMask |= genRegMask(thisArgReg);
        argNode->SetRegNumByIdx(thisArgReg, i);
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
        assert(src->isContained());
    }
    else
    {
        assert(src->TypeIs(TYP_STRUCT) && src->isContained());

        if (src->OperIs(GT_BLK))
        {
            // If the PUTARG_SPLIT clobbers only one register we may need an
            // extra internal register in case there is a conflict between the
            // source address register and target register.
            if (argNode->gtNumRegs == 1)
            {
                // We can use a ldr/str sequence so we need an internal register
                buildInternalIntRegisterDefForNode(argNode, allRegs(TYP_INT) & ~argMask);
            }

            // We will generate code that loads from the OBJ's address, which must be in a register.
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
    return srcCount;
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
#ifdef TARGET_ARM64
            {
                if (dstAddr->isContained())
                {
                    // Since the dstAddr is contained the address will be computed in CodeGen.
                    // This might require an integer register to store the value.
                    buildInternalIntRegisterDefForNode(blkNode);
                }

                if (size > FP_REGSIZE_BYTES)
                {
                    // For larger block sizes CodeGen can choose to use 16-byte SIMD instructions.
                    buildInternalFloatRegisterDefForNode(blkNode, internalFloatRegCandidates());
                }
            }
#endif // TARGET_ARM64
            break;

            case GenTreeBlk::BlkOpKindLoop:
                // Needed for offsetReg
                buildInternalIntRegisterDefForNode(blkNode, availableIntRegs);
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

        switch (blkNode->gtBlkOpKind)
        {
            case GenTreeBlk::BlkOpKindCpObjUnroll:
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
            break;

            case GenTreeBlk::BlkOpKindUnroll:
            {
                buildInternalIntRegisterDefForNode(blkNode);
#ifdef TARGET_ARM64
                const bool canUseLoadStorePairIntRegsInstrs = (size >= 2 * REGSIZE_BYTES);

                if (canUseLoadStorePairIntRegsInstrs)
                {
                    // CodeGen can use ldp/stp instructions sequence.
                    buildInternalIntRegisterDefForNode(blkNode);
                }

                const bool isSrcAddrLocal = src->OperIs(GT_LCL_VAR, GT_LCL_FLD) ||
                                            ((srcAddrOrFill != nullptr) && srcAddrOrFill->OperIs(GT_LCL_ADDR));
                const bool isDstAddrLocal = dstAddr->OperIs(GT_LCL_ADDR);

                // CodeGen can use 16-byte SIMD ldp/stp for larger block sizes.
                // This is the case, when both registers are either sp or fp.
                bool canUse16ByteWideInstrs = (size >= 2 * FP_REGSIZE_BYTES);

                // Note that the SIMD registers allocation is speculative - LSRA doesn't know at this point
                // whether CodeGen will use SIMD registers (i.e. if such instruction sequence will be more optimal).
                // Therefore, it must allocate an additional integer register anyway.
                if (canUse16ByteWideInstrs)
                {
                    buildInternalFloatRegisterDefForNode(blkNode, internalFloatRegCandidates());
                    buildInternalFloatRegisterDefForNode(blkNode, internalFloatRegCandidates());
                }

                const bool srcAddrMayNeedReg =
                    isSrcAddrLocal || ((srcAddrOrFill != nullptr) && srcAddrOrFill->isContained());
                const bool dstAddrMayNeedReg = isDstAddrLocal || dstAddr->isContained();

                // The following allocates an additional integer register in a case
                // when a load instruction and a store instruction cannot be encoded using offset
                // from a corresponding base register.
                if (srcAddrMayNeedReg && dstAddrMayNeedReg)
                {
                    buildInternalIntRegisterDefForNode(blkNode);
                }
#endif
            }
            break;

            case GenTreeBlk::BlkOpKindUnrollMemmove:
            {
#ifdef TARGET_ARM64

                // Prepare SIMD/GPR registers needed to perform an unrolled memmove. The idea that
                // we can ignore the fact that src and dst might overlap if we save the whole src
                // to temp regs in advance.

                // Lowering was expected to get rid of memmove in case of zero
                assert(size > 0);

                const unsigned simdSize = FP_REGSIZE_BYTES;
                if (size >= simdSize)
                {
                    unsigned simdRegs = size / simdSize;
                    if ((size % simdSize) != 0)
                    {
                        // TODO-CQ: Consider using GPR load/store here if the reminder is 1,2,4 or 8
                        simdRegs++;
                    }
                    for (unsigned i = 0; i < simdRegs; i++)
                    {
                        // It's too late to revert the unrolling so we hope we'll have enough SIMD regs
                        // no more than MaxInternalCount. Currently, it's controlled by getUnrollThreshold(memmove)
                        buildInternalFloatRegisterDefForNode(blkNode, internalFloatRegCandidates());
                    }
                }
                else if (isPow2(size))
                {
                    // Single GPR for 1,2,4,8
                    buildInternalIntRegisterDefForNode(blkNode, availableIntRegs);
                }
                else
                {
                    // Any size from 3 to 15 can be handled via two GPRs
                    buildInternalIntRegisterDefForNode(blkNode, availableIntRegs);
                    buildInternalIntRegisterDefForNode(blkNode, availableIntRegs);
                }
#else // TARGET_ARM64
                unreached();
#endif
            }
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
        BuildUse(blkNode->AsStoreDynBlk()->gtDynamicSize, sizeRegMask);
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
#endif

    int srcCount = BuildCastUses(cast, RBM_NONE);
    buildInternalRegisterUses();
    BuildDef(cast);
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
    assert(select->OperIs(GT_SELECT, GT_SELECTCC));

    int srcCount = 0;
    if (select->OperIs(GT_SELECT))
    {
        srcCount += BuildOperandUses(select->AsConditional()->gtCond);
    }

    srcCount += BuildOperandUses(select->gtOp1);
    srcCount += BuildOperandUses(select->gtOp2);
    BuildDef(select);

    return srcCount;
}

#endif // TARGET_ARMARCH
