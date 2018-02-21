// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

#ifndef LEGACY_BACKEND // This file is ONLY used for the RyuJIT backend that uses the linear scan register allocator

#ifdef _TARGET_ARMARCH_ // This file is ONLY used for ARM and ARM64 architectures

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
void LinearScan::BuildIndir(GenTreeIndir* indirTree)
{
    TreeNodeInfo* info = currentNodeInfo;
    // If this is the rhs of a block copy (i.e. non-enregisterable struct),
    // it has no register requirements.
    if (indirTree->TypeGet() == TYP_STRUCT)
    {
        return;
    }

    bool isStore   = (indirTree->gtOper == GT_STOREIND);
    info->srcCount = GetIndirInfo(indirTree);

    GenTree* addr  = indirTree->Addr();
    GenTree* index = nullptr;
    int      cns   = 0;

#ifdef _TARGET_ARM_
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
            info->internalIntCount = 1;
        }
        else if (type == TYP_DOUBLE)
        {
            info->internalIntCount = 2;
        }
    }
#endif

    if (addr->isContained())
    {
        assert(addr->OperGet() == GT_LEA);
        GenTreeAddrMode* lea = addr->AsAddrMode();
        index                = lea->Index();
        cns                  = lea->Offset();

        // On ARM we may need a single internal register
        // (when both conditions are true then we still only need a single internal register)
        if ((index != nullptr) && (cns != 0))
        {
            // ARM does not support both Index and offset so we need an internal register
            info->internalIntCount++;
        }
        else if (!emitter::emitIns_valid_imm_for_ldst_offset(cns, emitTypeSize(indirTree)))
        {
            // This offset can't be contained in the ldr/str instruction, so we need an internal register
            info->internalIntCount++;
        }
    }

#ifdef FEATURE_SIMD
    if (indirTree->TypeGet() == TYP_SIMD12)
    {
        // If indirTree is of TYP_SIMD12, addr is not contained. See comment in LowerIndir().
        assert(!indirTree->Addr()->isContained());

        // Vector3 is read/written as two reads/writes: 8 byte and 4 byte.
        // To assemble the vector properly we would need an additional int register
        info->internalIntCount = 1;
    }
#endif // FEATURE_SIMD
}

//------------------------------------------------------------------------
// BuildShiftRotate: Set the NodeInfo for a shift or rotate.
//
// Arguments:
//    tree      - The node of interest
//
// Return Value:
//    None.
//
int LinearScan::BuildShiftRotate(GenTree* tree)
{
    TreeNodeInfo* info    = currentNodeInfo;
    GenTree*      source  = tree->gtOp.gtOp1;
    GenTree*      shiftBy = tree->gtOp.gtOp2;
    assert(info->dstCount == 1);
    if (!shiftBy->isContained())
    {
        appendLocationInfoToList(shiftBy);
        info->srcCount = 1;
    }

#ifdef _TARGET_ARM_

    // The first operand of a GT_LSH_HI and GT_RSH_LO oper is a GT_LONG so that
    // we can have a three operand form. Increment the srcCount.
    if (tree->OperGet() == GT_LSH_HI || tree->OperGet() == GT_RSH_LO)
    {
        assert((source->OperGet() == GT_LONG) && source->isContained());
        info->srcCount += 2;

        LocationInfoListNode* sourceLoInfo = getLocationInfo(source->gtOp.gtOp1);
        useList.Append(sourceLoInfo);
        LocationInfoListNode* sourceHiInfo = getLocationInfo(source->gtOp.gtOp2);
        useList.Append(sourceHiInfo);
        if (tree->OperGet() == GT_LSH_HI)
        {
            sourceLoInfo->info.isDelayFree = true;
        }
        else
        {
            sourceHiInfo->info.isDelayFree = true;
        }
        info->hasDelayFreeSrc = true;
    }
    else
#endif // _TARGET_ARM_
    {
        appendLocationInfoToList(source);
        info->srcCount++;
    }
    return info->srcCount;
}

//------------------------------------------------------------------------
// BuildCall: Set the NodeInfo for a call.
//
// Arguments:
//    call - The call node of interest
//
// Return Value:
//    None.
//
void LinearScan::BuildCall(GenTreeCall* call)
{
    TreeNodeInfo*   info              = currentNodeInfo;
    bool            hasMultiRegRetVal = false;
    ReturnTypeDesc* retTypeDesc       = nullptr;

    info->srcCount = 0;
    if (call->TypeGet() != TYP_VOID)
    {
        hasMultiRegRetVal = call->HasMultiRegRetVal();
        if (hasMultiRegRetVal)
        {
            // dst count = number of registers in which the value is returned by call
            retTypeDesc    = call->GetReturnTypeDesc();
            info->dstCount = retTypeDesc->GetReturnRegCount();
        }
        else
        {
            info->dstCount = 1;
        }
    }
    else
    {
        info->dstCount = 0;
    }

    GenTree*              ctrlExpr     = call->gtControlExpr;
    LocationInfoListNode* ctrlExprInfo = nullptr;
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
        ctrlExprInfo = getLocationInfo(ctrlExpr);

        // we should never see a gtControlExpr whose type is void.
        assert(ctrlExpr->TypeGet() != TYP_VOID);

        // In case of fast tail implemented as jmp, make sure that gtControlExpr is
        // computed into a register.
        if (call->IsFastTailCall())
        {
            // Fast tail call - make sure that call target is always computed in R12(ARM32)/IP0(ARM64)
            // so that epilog sequence can generate "br xip0/r12" to achieve fast tail call.
            ctrlExprInfo->info.setSrcCandidates(this, RBM_FASTTAILCALL_TARGET);
        }
    }
#ifdef _TARGET_ARM_
    else
    {
        info->internalIntCount = 1;
    }
#endif // _TARGET_ARM_

    RegisterType registerType = call->TypeGet();

// Set destination candidates for return value of the call.

#ifdef _TARGET_ARM_
    if (call->IsHelperCall(compiler, CORINFO_HELP_INIT_PINVOKE_FRAME))
    {
        // The ARM CORINFO_HELP_INIT_PINVOKE_FRAME helper uses a custom calling convention that returns with
        // TCB in REG_PINVOKE_TCB. fgMorphCall() sets the correct argument registers.
        info->setDstCandidates(this, RBM_PINVOKE_TCB);
    }
    else
#endif // _TARGET_ARM_
        if (hasMultiRegRetVal)
    {
        assert(retTypeDesc != nullptr);
        info->setDstCandidates(this, retTypeDesc->GetABIReturnRegs());
    }
    else if (varTypeIsFloating(registerType))
    {
        info->setDstCandidates(this, RBM_FLOATRET);
    }
    else if (registerType == TYP_LONG)
    {
        info->setDstCandidates(this, RBM_LNGRET);
    }
    else
    {
        info->setDstCandidates(this, RBM_INTRET);
    }

    // First, count reg args
    // Each register argument corresponds to one source.
    bool callHasFloatRegArgs = false;

    for (GenTree* list = call->gtCallLateArgs; list; list = list->MoveNext())
    {
        assert(list->OperIsList());

        GenTree* argNode = list->Current();

#ifdef DEBUG
        // During Build, we only use the ArgTabEntry for validation,
        // as getting it is rather expensive.
        fgArgTabEntry* curArgTabEntry = compiler->gtArgEntryByNode(call, argNode);
        regNumber      argReg         = curArgTabEntry->regNum;
        assert(curArgTabEntry);
#endif

        if (argNode->gtOper == GT_PUTARG_STK)
        {
            // late arg that is not passed in a register
            assert(curArgTabEntry->regNum == REG_STK);
            GenTree* putArgChild = argNode->gtGetOp1();
            if (!varTypeIsStruct(putArgChild) && !putArgChild->OperIs(GT_FIELD_LIST))
            {
                unsigned expectedSlots = 1;
#ifdef _TARGET_ARM_
                // The `double` types could been transformed to `long` on arm, while the actual longs
                // have been decomposed.
                if (putArgChild->TypeGet() == TYP_LONG)
                {
                    useList.GetTreeNodeInfo(argNode).srcCount = 2;
                    expectedSlots                             = 2;
                }
                else if (putArgChild->TypeGet() == TYP_DOUBLE)
                {
                    expectedSlots = 2;
                }
#endif // !_TARGET_ARM_
                // Validate the slot count for this arg.
                assert(curArgTabEntry->numSlots == expectedSlots);
            }
            continue;
        }

        // A GT_FIELD_LIST has a TYP_VOID, but is used to represent a multireg struct
        if (argNode->OperGet() == GT_FIELD_LIST)
        {
            assert(argNode->isContained());

            // There could be up to 2-4 PUTARG_REGs in the list (3 or 4 can only occur for HFAs)
            for (GenTreeFieldList* entry = argNode->AsFieldList(); entry != nullptr; entry = entry->Rest())
            {
                info->srcCount++;
                appendLocationInfoToList(entry->Current());
#ifdef DEBUG
                assert(entry->Current()->OperIs(GT_PUTARG_REG));
                assert(entry->Current()->gtRegNum == argReg);
                // Update argReg for the next putarg_reg (if any)
                argReg = genRegArgNext(argReg);

#if defined(_TARGET_ARM_)
                // A double register is modelled as an even-numbered single one
                if (entry->Current()->TypeGet() == TYP_DOUBLE)
                {
                    argReg = genRegArgNext(argReg);
                }
#endif // _TARGET_ARM_
#endif
            }
        }
#ifdef _TARGET_ARM_
        else if (argNode->OperGet() == GT_PUTARG_SPLIT)
        {
            unsigned regCount = argNode->AsPutArgSplit()->gtNumRegs;
            assert(regCount == curArgTabEntry->numRegs);
            info->srcCount += regCount;
            appendLocationInfoToList(argNode);
        }
#endif
        else
        {
            assert(argNode->OperIs(GT_PUTARG_REG));
            assert(argNode->gtRegNum == argReg);
            HandleFloatVarArgs(call, argNode, &callHasFloatRegArgs);
#ifdef _TARGET_ARM_
            // The `double` types have been transformed to `long` on armel,
            // while the actual long types have been decomposed.
            // On ARM we may have bitcasts from DOUBLE to LONG.
            if (argNode->TypeGet() == TYP_LONG)
            {
                assert(argNode->IsMultiRegNode());
                info->srcCount += 2;
                appendLocationInfoToList(argNode);
            }
            else
#endif // _TARGET_ARM_
            {
                appendLocationInfoToList(argNode);
                info->srcCount++;
            }
        }
    }

    // Now, count stack args
    // Note that these need to be computed into a register, but then
    // they're just stored to the stack - so the reg doesn't
    // need to remain live until the call.  In fact, it must not
    // because the code generator doesn't actually consider it live,
    // so it can't be spilled.

    GenTree* args = call->gtCallArgs;
    while (args)
    {
        GenTree* arg = args->gtOp.gtOp1;

        // Skip arguments that have been moved to the Late Arg list
        if (!(args->gtFlags & GTF_LATE_ARG))
        {
#ifdef DEBUG
            fgArgTabEntry* curArgTabEntry = compiler->gtArgEntryByNode(call, arg);
            assert(curArgTabEntry);
#endif
#ifdef _TARGET_ARM_
            // PUTARG_SPLIT nodes must be in the gtCallLateArgs list, since they
            // define registers used by the call.
            assert(arg->OperGet() != GT_PUTARG_SPLIT);
#endif
            if (arg->gtOper == GT_PUTARG_STK)
            {
                assert(curArgTabEntry->regNum == REG_STK);
            }
            else
            {
                assert(!arg->IsValue() || arg->IsUnusedValue());
            }
        }
        args = args->gtOp.gtOp2;
    }

    // If it is a fast tail call, it is already preferenced to use IP0.
    // Therefore, no need set src candidates on call tgt again.
    if (call->IsVarargs() && callHasFloatRegArgs && !call->IsFastTailCall() && (ctrlExprInfo != nullptr))
    {
        NYI_ARM("float reg varargs");

        // Don't assign the call target to any of the argument registers because
        // we will use them to also pass floating point arguments as required
        // by Arm64 ABI.
        ctrlExprInfo->info.setSrcCandidates(this, allRegs(TYP_INT) & ~(RBM_ARG_REGS));
    }

    if (ctrlExprInfo != nullptr)
    {
        useList.Append(ctrlExprInfo);
        info->srcCount++;
    }

#ifdef _TARGET_ARM_

    if (call->NeedsNullCheck())
    {
        info->internalIntCount++;
    }

#endif // _TARGET_ARM_
}

//------------------------------------------------------------------------
// BuildPutArgStk: Set the NodeInfo for a GT_PUTARG_STK node
//
// Arguments:
//    argNode - a GT_PUTARG_STK node
//
// Return Value:
//    None.
//
// Notes:
//    Set the child node(s) to be contained when we have a multireg arg
//
void LinearScan::BuildPutArgStk(GenTreePutArgStk* argNode)
{
    TreeNodeInfo* info = currentNodeInfo;
    assert(argNode->gtOper == GT_PUTARG_STK);

    GenTree* putArgChild = argNode->gtOp.gtOp1;

    info->srcCount = 0;
    info->dstCount = 0;

    // Do we have a TYP_STRUCT argument (or a GT_FIELD_LIST), if so it must be a multireg pass-by-value struct
    if ((putArgChild->TypeGet() == TYP_STRUCT) || (putArgChild->OperGet() == GT_FIELD_LIST))
    {
        // We will use store instructions that each write a register sized value

        if (putArgChild->OperGet() == GT_FIELD_LIST)
        {
            assert(putArgChild->isContained());
            // We consume all of the items in the GT_FIELD_LIST
            for (GenTreeFieldList* current = putArgChild->AsFieldList(); current != nullptr; current = current->Rest())
            {
                appendLocationInfoToList(current->Current());
                info->srcCount++;
            }
        }
        else
        {
#ifdef _TARGET_ARM64_
            // We could use a ldp/stp sequence so we need two internal registers
            info->internalIntCount = 2;
#else  // _TARGET_ARM_
            // We could use a ldr/str sequence so we need a internal register
            info->internalIntCount = 1;
#endif // _TARGET_ARM_

            if (putArgChild->OperGet() == GT_OBJ)
            {
                assert(putArgChild->isContained());
                GenTree* objChild = putArgChild->gtOp.gtOp1;
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
                    appendLocationInfoToList(objChild);
                    info->srcCount = 1;
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
        info->srcCount = GetOperandInfo(putArgChild);
    }
}

#ifdef _TARGET_ARM_
//------------------------------------------------------------------------
// BuildPutArgSplit: Set the NodeInfo for a GT_PUTARG_SPLIT node
//
// Arguments:
//    argNode - a GT_PUTARG_SPLIT node
//
// Return Value:
//    None.
//
// Notes:
//    Set the child node(s) to be contained
//
void LinearScan::BuildPutArgSplit(GenTreePutArgSplit* argNode)
{
    TreeNodeInfo* info = currentNodeInfo;
    assert(argNode->gtOper == GT_PUTARG_SPLIT);

    GenTree* putArgChild = argNode->gtOp.gtOp1;

    // Registers for split argument corresponds to source
    info->dstCount = argNode->gtNumRegs;

    regNumber argReg  = argNode->gtRegNum;
    regMaskTP argMask = RBM_NONE;
    for (unsigned i = 0; i < argNode->gtNumRegs; i++)
    {
        argMask |= genRegMask((regNumber)((unsigned)argReg + i));
    }
    info->setDstCandidates(this, argMask);
    info->setSrcCandidates(this, argMask);

    if (putArgChild->OperGet() == GT_FIELD_LIST)
    {
        // Generated code:
        // 1. Consume all of the items in the GT_FIELD_LIST (source)
        // 2. Store to target slot and move to target registers (destination) from source
        //
        unsigned sourceRegCount = 0;

        // To avoid redundant moves, have the argument operand computed in the
        // register in which the argument is passed to the call.

        for (GenTreeFieldList* fieldListPtr = putArgChild->AsFieldList(); fieldListPtr != nullptr;
             fieldListPtr                   = fieldListPtr->Rest())
        {
            GenTree* node = fieldListPtr->gtGetOp1();
            assert(!node->isContained());
            LocationInfoListNode* nodeInfo        = getLocationInfo(node);
            unsigned              currentRegCount = nodeInfo->info.dstCount;
            regMaskTP             sourceMask      = RBM_NONE;
            if (sourceRegCount < argNode->gtNumRegs)
            {
                for (unsigned regIndex = 0; regIndex < currentRegCount; regIndex++)
                {
                    sourceMask |= genRegMask((regNumber)((unsigned)argReg + sourceRegCount + regIndex));
                }
                nodeInfo->info.setSrcCandidates(this, sourceMask);
            }
            sourceRegCount += currentRegCount;
            useList.Append(nodeInfo);
        }
        info->srcCount += sourceRegCount;
        assert(putArgChild->isContained());
    }
    else
    {
        assert(putArgChild->TypeGet() == TYP_STRUCT);
        assert(putArgChild->OperGet() == GT_OBJ);

        // We can use a ldr/str sequence so we need an internal register
        info->internalIntCount = 1;
        regMaskTP internalMask = RBM_ALLINT & ~argMask;
        info->setInternalCandidates(this, internalMask);

        GenTree* objChild = putArgChild->gtOp.gtOp1;
        if (objChild->OperGet() == GT_LCL_VAR_ADDR)
        {
            // We will generate all of the code for the GT_PUTARG_SPLIT, the GT_OBJ and the GT_LCL_VAR_ADDR
            // as one contained operation
            //
            assert(objChild->isContained());
        }
        else
        {
            info->srcCount = GetIndirInfo(putArgChild->AsIndir());
        }
        assert(putArgChild->isContained());
    }
}
#endif // _TARGET_ARM_

//------------------------------------------------------------------------
// BuildBlockStore: Set the NodeInfo for a block store.
//
// Arguments:
//    blkNode       - The block store node of interest
//
// Return Value:
//    None.
//
void LinearScan::BuildBlockStore(GenTreeBlk* blkNode)
{
    TreeNodeInfo* info    = currentNodeInfo;
    GenTree*      dstAddr = blkNode->Addr();
    unsigned      size    = blkNode->gtBlkSize;
    GenTree*      source  = blkNode->Data();

    LocationInfoListNode* dstAddrInfo = nullptr;
    LocationInfoListNode* sourceInfo  = nullptr;
    LocationInfoListNode* sizeInfo    = nullptr;

    // Sources are dest address and initVal or source.
    // We may require an additional source or temp register for the size.
    if (!dstAddr->isContained())
    {
        info->srcCount++;
        dstAddrInfo = getLocationInfo(dstAddr);
    }
    assert(info->dstCount == 0);
    GenTree* srcAddrOrFill = nullptr;
    bool     isInitBlk     = blkNode->OperIsInitBlkOp();

    regMaskTP dstAddrRegMask = RBM_NONE;
    regMaskTP sourceRegMask  = RBM_NONE;
    regMaskTP blkSizeRegMask = RBM_NONE;

    short     internalIntCount      = 0;
    regMaskTP internalIntCandidates = RBM_NONE;

    if (isInitBlk)
    {
        GenTree* initVal = source;
        if (initVal->OperIsInitVal())
        {
            assert(initVal->isContained());
            initVal = initVal->gtGetOp1();
        }
        srcAddrOrFill = initVal;
        if (!initVal->isContained())
        {
            info->srcCount++;
            sourceInfo = getLocationInfo(initVal);
        }

        if (blkNode->gtBlkOpKind == GenTreeBlk::BlkOpKindUnroll)
        {
            // TODO-ARM-CQ: Currently we generate a helper call for every
            // initblk we encounter.  Later on we should implement loop unrolling
            // code sequences to improve CQ.
            // For reference see the code in lsraxarch.cpp.
            NYI_ARM("initblk loop unrolling is currently not implemented.");
        }
        else
        {
            assert(blkNode->gtBlkOpKind == GenTreeBlk::BlkOpKindHelper);
            assert(!initVal->isContained());
            // The helper follows the regular ABI.
            dstAddrRegMask = RBM_ARG_0;
            sourceRegMask  = RBM_ARG_1;
            blkSizeRegMask = RBM_ARG_2;
        }
    }
    else
    {
        // CopyObj or CopyBlk
        // Sources are src and dest and size if not constant.
        if (source->gtOper == GT_IND)
        {
            assert(source->isContained());
            srcAddrOrFill = source->gtGetOp1();
            assert(!srcAddrOrFill->isContained());
            sourceInfo = getLocationInfo(srcAddrOrFill);
            info->srcCount++;
        }
        if (blkNode->OperGet() == GT_STORE_OBJ)
        {
            // CopyObj
            // We don't need to materialize the struct size but we still need
            // a temporary register to perform the sequence of loads and stores.
            internalIntCount = 1;

            if (size >= 2 * REGSIZE_BYTES)
            {
                // We will use ldp/stp to reduce code size and improve performance
                // so we need to reserve an extra internal register
                internalIntCount++;
            }

            // We can't use the special Write Barrier registers, so exclude them from the mask
            internalIntCandidates = RBM_ALLINT & ~(RBM_WRITE_BARRIER_DST_BYREF | RBM_WRITE_BARRIER_SRC_BYREF);

            // If we have a dest address we want it in RBM_WRITE_BARRIER_DST_BYREF.
            dstAddrRegMask = RBM_WRITE_BARRIER_DST_BYREF;

            // If we have a source address we want it in REG_WRITE_BARRIER_SRC_BYREF.
            // Otherwise, if it is a local, codegen will put its address in REG_WRITE_BARRIER_SRC_BYREF,
            // which is killed by a StoreObj (and thus needn't be reserved).
            if (srcAddrOrFill != nullptr)
            {
                sourceRegMask = RBM_WRITE_BARRIER_SRC_BYREF;
            }
        }
        else
        {
            // CopyBlk
            if (blkNode->gtBlkOpKind == GenTreeBlk::BlkOpKindUnroll)
            {
                // In case of a CpBlk with a constant size and less than CPBLK_UNROLL_LIMIT size
                // we should unroll the loop to improve CQ.
                // For reference see the code in lsraxarch.cpp.

                internalIntCount      = 1;
                internalIntCandidates = RBM_ALLINT;

#ifdef _TARGET_ARM64_
                if (size >= 2 * REGSIZE_BYTES)
                {
                    // We will use ldp/stp to reduce code size and improve performance
                    // so we need to reserve an extra internal register
                    internalIntCount++;
                }
#endif // _TARGET_ARM64_
            }
            else
            {
                assert(blkNode->gtBlkOpKind == GenTreeBlk::BlkOpKindHelper);
                dstAddrRegMask = RBM_ARG_0;
                // The srcAddr goes in arg1.
                if (srcAddrOrFill != nullptr)
                {
                    sourceRegMask = RBM_ARG_1;
                }
                blkSizeRegMask = RBM_ARG_2;
            }
        }
    }
    if (dstAddrInfo != nullptr)
    {
        if (dstAddrRegMask != RBM_NONE)
        {
            dstAddrInfo->info.setSrcCandidates(this, dstAddrRegMask);
        }
        useList.Append(dstAddrInfo);
    }
    if (sourceRegMask != RBM_NONE)
    {
        if (sourceInfo != nullptr)
        {
            sourceInfo->info.setSrcCandidates(this, sourceRegMask);
        }
        else
        {
            // This is a local source; we'll use a temp register for its address.
            internalIntCandidates |= sourceRegMask;
            internalIntCount++;
        }
    }
    if (sourceInfo != nullptr)
    {
        useList.Add(sourceInfo, blkNode->IsReverseOp());
    }

    if (blkNode->OperIs(GT_STORE_DYN_BLK))
    {
        // The block size argument is a third argument to GT_STORE_DYN_BLK
        info->srcCount++;

        GenTree* blockSize = blkNode->AsDynBlk()->gtDynamicSize;
        sizeInfo           = getLocationInfo(blockSize);
        useList.Add(sizeInfo, blkNode->AsDynBlk()->gtEvalSizeFirst);
    }

    if (blkSizeRegMask != RBM_NONE)
    {
        if (size != 0)
        {
            // Reserve a temp register for the block size argument.
            internalIntCandidates |= blkSizeRegMask;
            internalIntCount++;
        }
        else
        {
            // The block size argument is a third argument to GT_STORE_DYN_BLK
            assert((blkNode->gtOper == GT_STORE_DYN_BLK) && (sizeInfo != nullptr));
            info->setSrcCount(3);
            sizeInfo->info.setSrcCandidates(this, blkSizeRegMask);
        }
    }
    if (internalIntCount != 0)
    {
        info->internalIntCount = internalIntCount;
        info->setInternalCandidates(this, internalIntCandidates);
    }
}

#endif // _TARGET_ARMARCH_

#endif // !LEGACY_BACKEND
