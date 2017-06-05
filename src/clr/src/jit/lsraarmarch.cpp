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
// TreeNodeInfoInitStoreLoc: Set register requirements for a store of a lclVar
//
// Arguments:
//    storeLoc - the local store (GT_STORE_LCL_FLD or GT_STORE_LCL_VAR)
//
// Notes:
//    This involves:
//    - Setting the appropriate candidates for a store of a multi-reg call return value.
//    - Handling of contained immediates.
//
void Lowering::TreeNodeInfoInitStoreLoc(GenTreeLclVarCommon* storeLoc)
{
    TreeNodeInfo* info = &(storeLoc->gtLsraInfo);

    // Is this the case of var = call where call is returning
    // a value in multiple return registers?
    GenTree* op1 = storeLoc->gtGetOp1();
    if (op1->IsMultiRegCall())
    {
        // backend expects to see this case only for store lclvar.
        assert(storeLoc->OperGet() == GT_STORE_LCL_VAR);

        // srcCount = number of registers in which the value is returned by call
        GenTreeCall*    call        = op1->AsCall();
        ReturnTypeDesc* retTypeDesc = call->GetReturnTypeDesc();
        info->srcCount              = retTypeDesc->GetReturnRegCount();

        // Call node srcCandidates = Bitwise-OR(allregs(GetReturnRegType(i))) for all i=0..RetRegCount-1
        regMaskTP srcCandidates = m_lsra->allMultiRegCallNodeRegs(call);
        op1->gtLsraInfo.setSrcCandidates(m_lsra, srcCandidates);
        return;
    }

    CheckImmedAndMakeContained(storeLoc, op1);
}

//------------------------------------------------------------------------
// TreeNodeInfoInitCmp: Lower a GT comparison node.
//
// Arguments:
//    tree - the node to lower
//
// Return Value:
//    None.
//
void Lowering::TreeNodeInfoInitCmp(GenTreePtr tree)
{
    TreeNodeInfo* info = &(tree->gtLsraInfo);

    info->srcCount = 2;
    info->dstCount = tree->OperIs(GT_CMP) ? 0 : 1;

    CheckImmedAndMakeContained(tree, tree->gtOp.gtOp2);
}

void Lowering::TreeNodeInfoInitGCWriteBarrier(GenTree* tree)
{
    GenTreePtr dst  = tree;
    GenTreePtr addr = tree->gtOp.gtOp1;
    GenTreePtr src  = tree->gtOp.gtOp2;

    if (addr->OperGet() == GT_LEA)
    {
        // In the case where we are doing a helper assignment, if the dst
        // is an indir through an lea, we need to actually instantiate the
        // lea in a register
        GenTreeAddrMode* lea = addr->AsAddrMode();

        short leaSrcCount = 0;
        if (lea->Base() != nullptr)
        {
            leaSrcCount++;
        }
        if (lea->Index() != nullptr)
        {
            leaSrcCount++;
        }
        lea->gtLsraInfo.srcCount = leaSrcCount;
        lea->gtLsraInfo.dstCount = 1;
    }

#if NOGC_WRITE_BARRIERS
    NYI_ARM("NOGC_WRITE_BARRIERS");

    // For the NOGC JIT Helper calls
    //
    // the 'addr' goes into x14 (REG_WRITE_BARRIER_DST_BYREF)
    // the 'src'  goes into x15 (REG_WRITE_BARRIER)
    //
    addr->gtLsraInfo.setSrcCandidates(m_lsra, RBM_WRITE_BARRIER_DST_BYREF);
    src->gtLsraInfo.setSrcCandidates(m_lsra, RBM_WRITE_BARRIER);
#else
    // For the standard JIT Helper calls
    // op1 goes into REG_ARG_0 and
    // op2 goes into REG_ARG_1
    //
    addr->gtLsraInfo.setSrcCandidates(m_lsra, RBM_ARG_0);
    src->gtLsraInfo.setSrcCandidates(m_lsra, RBM_ARG_1);
#endif // NOGC_WRITE_BARRIERS

    // Both src and dst must reside in a register, which they should since we haven't set
    // either of them as contained.
    assert(addr->gtLsraInfo.dstCount == 1);
    assert(src->gtLsraInfo.dstCount == 1);
}

//------------------------------------------------------------------------
// TreeNodeInfoInitIndir: Specify register requirements for address expression
//                       of an indirection operation.
//
// Arguments:
//    indirTree - GT_IND, GT_STOREIND, block node or GT_NULLCHECK gentree node
//
void Lowering::TreeNodeInfoInitIndir(GenTreePtr indirTree)
{
    assert(indirTree->OperIsIndir());
    // If this is the rhs of a block copy (i.e. non-enregisterable struct),
    // it has no register requirements.
    if (indirTree->TypeGet() == TYP_STRUCT)
    {
        return;
    }

    GenTreePtr    addr = indirTree->gtGetOp1();
    TreeNodeInfo* info = &(indirTree->gtLsraInfo);

    GenTreePtr base  = nullptr;
    GenTreePtr index = nullptr;
    unsigned   cns   = 0;
    unsigned   mul;
    bool       rev;
    bool       modifiedSources = false;
    bool       makeContained   = true;

    if ((addr->OperGet() == GT_LEA) && IsSafeToContainMem(indirTree, addr))
    {
        GenTreeAddrMode* lea = addr->AsAddrMode();
        base                 = lea->Base();
        index                = lea->Index();
        cns                  = lea->gtOffset;

#ifdef _TARGET_ARM_
        // ARM floating-point load/store doesn't support a form similar to integer
        // ldr Rdst, [Rbase + Roffset] with offset in a register. The only supported
        // form is vldr Rdst, [Rbase + imm] with a more limited constraint on the imm.
        if (lea->HasIndex() || !emitter::emitIns_valid_imm_for_vldst_offset(cns))
        {
            if (indirTree->OperGet() == GT_STOREIND)
            {
                if (varTypeIsFloating(indirTree->AsStoreInd()->Data()))
                {
                    makeContained = false;
                }
            }
            else if (indirTree->OperGet() == GT_IND)
            {
                if (varTypeIsFloating(indirTree))
                {
                    makeContained = false;
                }
            }
        }
#endif

        if (makeContained)
        {
            m_lsra->clearOperandCounts(addr);
            // The srcCount is decremented because addr is now "contained",
            // then we account for the base and index below, if they are non-null.
            info->srcCount--;
        }
    }
    else if (comp->codeGen->genCreateAddrMode(addr, -1, true, 0, &rev, &base, &index, &mul, &cns, true /*nogen*/) &&
             !(modifiedSources = AreSourcesPossiblyModifiedLocals(indirTree, base, index)))
    {
        // An addressing mode will be constructed that may cause some
        // nodes to not need a register, and cause others' lifetimes to be extended
        // to the GT_IND or even its parent if it's an assignment

        assert(base != addr);
        m_lsra->clearOperandCounts(addr);

        // Traverse the computation below GT_IND to find the operands
        // for the addressing mode, marking the various constants and
        // intermediate results as not consuming/producing.
        // If the traversal were more complex, we might consider using
        // a traversal function, but the addressing mode is only made
        // up of simple arithmetic operators, and the code generator
        // only traverses one leg of each node.

        bool       foundBase  = (base == nullptr);
        bool       foundIndex = (index == nullptr);
        GenTreePtr nextChild  = nullptr;
        for (GenTreePtr child = addr; child != nullptr && !child->OperIsLeaf(); child = nextChild)
        {
            nextChild      = nullptr;
            GenTreePtr op1 = child->gtOp.gtOp1;
            GenTreePtr op2 = (child->OperIsBinary()) ? child->gtOp.gtOp2 : nullptr;

            if (op1 == base)
            {
                foundBase = true;
            }
            else if (op1 == index)
            {
                foundIndex = true;
            }
            else
            {
                m_lsra->clearOperandCounts(op1);
                if (!op1->OperIsLeaf())
                {
                    nextChild = op1;
                }
            }

            if (op2 != nullptr)
            {
                if (op2 == base)
                {
                    foundBase = true;
                }
                else if (op2 == index)
                {
                    foundIndex = true;
                }
                else
                {
                    m_lsra->clearOperandCounts(op2);
                    if (!op2->OperIsLeaf())
                    {
                        assert(nextChild == nullptr);
                        nextChild = op2;
                    }
                }
            }
        }
        assert(foundBase && foundIndex);
        info->srcCount--; // it gets incremented below.
    }
    else if (addr->gtOper == GT_ARR_ELEM)
    {
        // The GT_ARR_ELEM consumes all the indices and produces the offset.
        // The array object lives until the mem access.
        // We also consume the target register to which the address is
        // computed

        info->srcCount++;
        assert(addr->gtLsraInfo.srcCount >= 2);
        addr->gtLsraInfo.srcCount -= 1;
    }
    else
    {
        // it is nothing but a plain indir
        info->srcCount--; // base gets added in below
        base = addr;
    }

    if (!makeContained)
    {
        return;
    }

    if (base != nullptr)
    {
        info->srcCount++;
    }
    if (index != nullptr && !modifiedSources)
    {
        info->srcCount++;
    }

    // On ARM we may need a single internal register
    // (when both conditions are true then we still only need a single internal register)
    if ((index != nullptr) && (cns != 0))
    {
        // ARM does not support both Index and offset so we need an internal register
        info->internalIntCount = 1;
    }
    else if (!emitter::emitIns_valid_imm_for_ldst_offset(cns, emitTypeSize(indirTree)))
    {
        // This offset can't be contained in the ldr/str instruction, so we need an internal register
        info->internalIntCount = 1;
    }
}

//------------------------------------------------------------------------
// TreeNodeInfoInitShiftRotate: Set the NodeInfo for a shift or rotate.
//
// Arguments:
//    tree      - The node of interest
//
// Return Value:
//    None.
//
void Lowering::TreeNodeInfoInitShiftRotate(GenTree* tree)
{
    TreeNodeInfo* info = &(tree->gtLsraInfo);
    LinearScan*   l    = m_lsra;

    info->srcCount = 2;
    info->dstCount = 1;

    GenTreePtr shiftBy = tree->gtOp.gtOp2;
    GenTreePtr source  = tree->gtOp.gtOp1;
    if (shiftBy->IsCnsIntOrI())
    {
        l->clearDstCount(shiftBy);
        info->srcCount--;
    }

#ifdef _TARGET_ARM_

    // The first operand of a GT_LSH_HI and GT_RSH_LO oper is a GT_LONG so that
    // we can have a three operand form. Increment the srcCount.
    if (tree->OperGet() == GT_LSH_HI || tree->OperGet() == GT_RSH_LO)
    {
        assert(source->OperGet() == GT_LONG);

        info->srcCount++;

        if (tree->OperGet() == GT_LSH_HI)
        {
            GenTreePtr sourceLo              = source->gtOp.gtOp1;
            sourceLo->gtLsraInfo.isDelayFree = true;
        }
        else
        {
            GenTreePtr sourceHi              = source->gtOp.gtOp2;
            sourceHi->gtLsraInfo.isDelayFree = true;
        }

        source->gtLsraInfo.hasDelayFreeSrc = true;
        info->hasDelayFreeSrc              = true;
    }

#endif // _TARGET_ARM_
}

//------------------------------------------------------------------------
// TreeNodeInfoInitPutArgReg: Set the NodeInfo for a PUTARG_REG.
//
// Arguments:
//    node                - The PUTARG_REG node.
//    argReg              - The register in which to pass the argument.
//    info                - The info for the node's using call.
//    isVarArgs           - True if the call uses a varargs calling convention.
//    callHasFloatRegArgs - Set to true if this PUTARG_REG uses an FP register.
//
// Return Value:
//    None.
//
void Lowering::TreeNodeInfoInitPutArgReg(
    GenTreeUnOp* node, regNumber argReg, TreeNodeInfo& info, bool isVarArgs, bool* callHasFloatRegArgs)
{
    assert(node != nullptr);
    assert(node->OperIsPutArgReg());
    assert(argReg != REG_NA);

    // Each register argument corresponds to one source.
    info.srcCount++;

    // Set the register requirements for the node.
    const regMaskTP argMask = genRegMask(argReg);
    node->gtLsraInfo.setDstCandidates(m_lsra, argMask);
    node->gtLsraInfo.setSrcCandidates(m_lsra, argMask);

    // To avoid redundant moves, have the argument operand computed in the
    // register in which the argument is passed to the call.
    node->gtOp.gtOp1->gtLsraInfo.setSrcCandidates(m_lsra, m_lsra->getUseCandidates(node));

    *callHasFloatRegArgs |= varTypeIsFloating(node->TypeGet());
}

//------------------------------------------------------------------------
// TreeNodeInfoInitCall: Set the NodeInfo for a call.
//
// Arguments:
//    call - The call node of interest
//
// Return Value:
//    None.
//
void Lowering::TreeNodeInfoInitCall(GenTreeCall* call)
{
    TreeNodeInfo*   info              = &(call->gtLsraInfo);
    LinearScan*     l                 = m_lsra;
    Compiler*       compiler          = comp;
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

    GenTree* ctrlExpr = call->gtControlExpr;
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

        info->srcCount++;

        // In case of fast tail implemented as jmp, make sure that gtControlExpr is
        // computed into a register.
        if (call->IsFastTailCall())
        {
            NYI_ARM("tail call");

#ifdef _TARGET_ARM64_
            // Fast tail call - make sure that call target is always computed in IP0
            // so that epilog sequence can generate "br xip0" to achieve fast tail call.
            ctrlExpr->gtLsraInfo.setSrcCandidates(l, genRegMask(REG_IP0));
#endif // _TARGET_ARM64_
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
        info->setDstCandidates(l, RBM_PINVOKE_TCB);
    }
    else
#endif // _TARGET_ARM_
        if (hasMultiRegRetVal)
    {
        assert(retTypeDesc != nullptr);
        info->setDstCandidates(l, retTypeDesc->GetABIReturnRegs());
    }
    else if (varTypeIsFloating(registerType))
    {
        info->setDstCandidates(l, RBM_FLOATRET);
    }
    else if (registerType == TYP_LONG)
    {
        info->setDstCandidates(l, RBM_LNGRET);
    }
    else
    {
        info->setDstCandidates(l, RBM_INTRET);
    }

    // If there is an explicit this pointer, we don't want that node to produce anything
    // as it is redundant
    if (call->gtCallObjp != nullptr)
    {
        GenTreePtr thisPtrNode = call->gtCallObjp;

        if (thisPtrNode->gtOper == GT_PUTARG_REG)
        {
            l->clearOperandCounts(thisPtrNode);
            l->clearDstCount(thisPtrNode->gtOp.gtOp1);
        }
        else
        {
            l->clearDstCount(thisPtrNode);
        }
    }

    // First, count reg args
    bool callHasFloatRegArgs = false;

    for (GenTreePtr list = call->gtCallLateArgs; list; list = list->MoveNext())
    {
        assert(list->OperIsList());

        GenTreePtr argNode = list->Current();

        fgArgTabEntryPtr curArgTabEntry = compiler->gtArgEntryByNode(call, argNode);
        assert(curArgTabEntry);

        if (curArgTabEntry->regNum == REG_STK)
        {
            // late arg that is not passed in a register
            assert(argNode->gtOper == GT_PUTARG_STK);

            TreeNodeInfoInitPutArgStk(argNode->AsPutArgStk(), curArgTabEntry);
            continue;
        }

        // A GT_FIELD_LIST has a TYP_VOID, but is used to represent a multireg struct
        if (argNode->OperGet() == GT_FIELD_LIST)
        {
            // There could be up to 2-4 PUTARG_REGs in the list (3 or 4 can only occur for HFAs)
            regNumber argReg = curArgTabEntry->regNum;
            for (GenTreeFieldList* entry = argNode->AsFieldList(); entry != nullptr; entry = entry->Rest())
            {
                TreeNodeInfoInitPutArgReg(entry->Current()->AsUnOp(), argReg, *info, false, &callHasFloatRegArgs);

                // Update argReg for the next putarg_reg (if any)
                argReg = genRegArgNext(argReg);

#if defined(_TARGET_ARM_)
                // A double register is modelled as an even-numbered single one
                if (entry->Current()->TypeGet() == TYP_DOUBLE)
                {
                    argReg = genRegArgNext(argReg);
                }
#endif // _TARGET_ARM_
            }
        }
        else
        {
            TreeNodeInfoInitPutArgReg(argNode->AsUnOp(), curArgTabEntry->regNum, *info, false, &callHasFloatRegArgs);
        }
    }

    // Now, count stack args
    // Note that these need to be computed into a register, but then
    // they're just stored to the stack - so the reg doesn't
    // need to remain live until the call.  In fact, it must not
    // because the code generator doesn't actually consider it live,
    // so it can't be spilled.

    GenTreePtr args = call->gtCallArgs;
    while (args)
    {
        GenTreePtr arg = args->gtOp.gtOp1;

        // Skip arguments that have been moved to the Late Arg list
        if (!(args->gtFlags & GTF_LATE_ARG))
        {
            if (arg->gtOper == GT_PUTARG_STK)
            {
                fgArgTabEntryPtr curArgTabEntry = compiler->gtArgEntryByNode(call, arg);
                assert(curArgTabEntry);

                assert(curArgTabEntry->regNum == REG_STK);

                TreeNodeInfoInitPutArgStk(arg->AsPutArgStk(), curArgTabEntry);
            }
            else
            {
                TreeNodeInfo* argInfo = &(arg->gtLsraInfo);
                if (argInfo->dstCount != 0)
                {
                    argInfo->isLocalDefUse = true;
                }

                argInfo->dstCount = 0;
            }
        }
        args = args->gtOp.gtOp2;
    }

    // If it is a fast tail call, it is already preferenced to use IP0.
    // Therefore, no need set src candidates on call tgt again.
    if (call->IsVarargs() && callHasFloatRegArgs && !call->IsFastTailCall() && (ctrlExpr != nullptr))
    {
        NYI_ARM("float reg varargs");

        // Don't assign the call target to any of the argument registers because
        // we will use them to also pass floating point arguments as required
        // by Arm64 ABI.
        ctrlExpr->gtLsraInfo.setSrcCandidates(l, l->allRegs(TYP_INT) & ~(RBM_ARG_REGS));
    }

#ifdef _TARGET_ARM_

    if (call->NeedsNullCheck())
    {
        info->internalIntCount++;
    }

#endif // _TARGET_ARM_
}

//------------------------------------------------------------------------
// TreeNodeInfoInitPutArgStk: Set the NodeInfo for a GT_PUTARG_STK node
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
void Lowering::TreeNodeInfoInitPutArgStk(GenTreePutArgStk* argNode, fgArgTabEntryPtr info)
{
    assert(argNode->gtOper == GT_PUTARG_STK);

    GenTreePtr putArgChild = argNode->gtOp.gtOp1;

    // Initialize 'argNode' as not contained, as this is both the default case
    //  and how MakeSrcContained expects to find things setup.
    //
    argNode->gtLsraInfo.srcCount = 1;
    argNode->gtLsraInfo.dstCount = 0;

    // Do we have a TYP_STRUCT argument (or a GT_FIELD_LIST), if so it must be a multireg pass-by-value struct
    if ((putArgChild->TypeGet() == TYP_STRUCT) || (putArgChild->OperGet() == GT_FIELD_LIST))
    {
        // We will use store instructions that each write a register sized value

        if (putArgChild->OperGet() == GT_FIELD_LIST)
        {
            // We consume all of the items in the GT_FIELD_LIST
            argNode->gtLsraInfo.srcCount = info->numSlots;
        }
        else
        {
#ifdef _TARGET_ARM64_
            // We could use a ldp/stp sequence so we need two internal registers
            argNode->gtLsraInfo.internalIntCount = 2;
#else  // _TARGET_ARM_
            // We could use a ldr/str sequence so we need a internal register
            argNode->gtLsraInfo.internalIntCount = 1;
#endif // _TARGET_ARM_

            if (putArgChild->OperGet() == GT_OBJ)
            {
                GenTreePtr objChild = putArgChild->gtOp.gtOp1;
                if (objChild->OperGet() == GT_LCL_VAR_ADDR)
                {
                    // We will generate all of the code for the GT_PUTARG_STK, the GT_OBJ and the GT_LCL_VAR_ADDR
                    // as one contained operation
                    //
                    MakeSrcContained(putArgChild, objChild);
                }
            }

            // We will generate all of the code for the GT_PUTARG_STK and it's child node
            // as one contained operation
            //
            MakeSrcContained(argNode, putArgChild);
        }
    }
    else
    {
        // We must not have a multi-reg struct
        assert(info->numSlots == 1);
    }
}

//------------------------------------------------------------------------
// TreeNodeInfoInitBlockStore: Set the NodeInfo for a block store.
//
// Arguments:
//    blkNode       - The block store node of interest
//
// Return Value:
//    None.
//
void Lowering::TreeNodeInfoInitBlockStore(GenTreeBlk* blkNode)
{
    GenTree*    dstAddr  = blkNode->Addr();
    unsigned    size     = blkNode->gtBlkSize;
    GenTree*    source   = blkNode->Data();
    LinearScan* l        = m_lsra;
    Compiler*   compiler = comp;

    // Sources are dest address and initVal or source.
    // We may require an additional source or temp register for the size.
    blkNode->gtLsraInfo.srcCount = 2;
    blkNode->gtLsraInfo.dstCount = 0;
    GenTreePtr srcAddrOrFill     = nullptr;
    bool       isInitBlk         = blkNode->OperIsInitBlkOp();

    if (!isInitBlk)
    {
        // CopyObj or CopyBlk
        if (source->gtOper == GT_IND)
        {
            srcAddrOrFill = blkNode->Data()->gtGetOp1();
            // We're effectively setting source as contained, but can't call MakeSrcContained, because the
            // "inheritance" of the srcCount is to a child not a parent - it would "just work" but could be misleading.
            // If srcAddr is already non-contained, we don't need to change it.
            if (srcAddrOrFill->gtLsraInfo.getDstCount() == 0)
            {
                srcAddrOrFill->gtLsraInfo.setDstCount(1);
                srcAddrOrFill->gtLsraInfo.setSrcCount(source->gtLsraInfo.srcCount);
            }
            m_lsra->clearOperandCounts(source);
        }
        else if (!source->IsMultiRegCall() && !source->OperIsSIMD())
        {
            assert(source->IsLocal());
            MakeSrcContained(blkNode, source);
        }
    }

    if (isInitBlk)
    {
        GenTreePtr initVal = source;
        if (initVal->OperIsInitVal())
        {
            initVal = initVal->gtGetOp1();
        }
        srcAddrOrFill = initVal;

        if (blkNode->gtBlkOpKind == GenTreeBlk::BlkOpKindUnroll)
        {
            // TODO-ARM-CQ: Currently we generate a helper call for every
            // initblk we encounter.  Later on we should implement loop unrolling
            // code sequences to improve CQ.
            // For reference see the code in lsraxarch.cpp.
            NYI_ARM("initblk loop unrolling is currently not implemented.");

#ifdef _TARGET_ARM64_
            // No additional temporaries required
            ssize_t fill = initVal->gtIntCon.gtIconVal & 0xFF;
            if (fill == 0)
            {
                MakeSrcContained(blkNode, source);
            }
#endif // _TARGET_ARM64_
        }
        else
        {
            assert(blkNode->gtBlkOpKind == GenTreeBlk::BlkOpKindHelper);
            // The helper follows the regular ABI.
            dstAddr->gtLsraInfo.setSrcCandidates(l, RBM_ARG_0);
            initVal->gtLsraInfo.setSrcCandidates(l, RBM_ARG_1);
            if (size != 0)
            {
                // Reserve a temp register for the block size argument.
                blkNode->gtLsraInfo.setInternalCandidates(l, RBM_ARG_2);
                blkNode->gtLsraInfo.internalIntCount = 1;
            }
            else
            {
                // The block size argument is a third argument to GT_STORE_DYN_BLK
                noway_assert(blkNode->gtOper == GT_STORE_DYN_BLK);
                blkNode->gtLsraInfo.setSrcCount(3);
                GenTree* sizeNode = blkNode->AsDynBlk()->gtDynamicSize;
                sizeNode->gtLsraInfo.setSrcCandidates(l, RBM_ARG_2);
            }
        }
    }
    else
    {
        // CopyObj or CopyBlk
        // Sources are src and dest and size if not constant.
        if (blkNode->OperGet() == GT_STORE_OBJ)
        {
            // CopyObj
            // We don't need to materialize the struct size but we still need
            // a temporary register to perform the sequence of loads and stores.
            blkNode->gtLsraInfo.internalIntCount = 1;

            if (size >= 2 * REGSIZE_BYTES)
            {
                // We will use ldp/stp to reduce code size and improve performance
                // so we need to reserve an extra internal register
                blkNode->gtLsraInfo.internalIntCount++;
            }

            // We can't use the special Write Barrier registers, so exclude them from the mask
            regMaskTP internalIntCandidates = RBM_ALLINT & ~(RBM_WRITE_BARRIER_DST_BYREF | RBM_WRITE_BARRIER_SRC_BYREF);
            blkNode->gtLsraInfo.setInternalCandidates(l, internalIntCandidates);

            // If we have a dest address we want it in RBM_WRITE_BARRIER_DST_BYREF.
            dstAddr->gtLsraInfo.setSrcCandidates(l, RBM_WRITE_BARRIER_DST_BYREF);

            // If we have a source address we want it in REG_WRITE_BARRIER_SRC_BYREF.
            // Otherwise, if it is a local, codegen will put its address in REG_WRITE_BARRIER_SRC_BYREF,
            // which is killed by a StoreObj (and thus needn't be reserved).
            if (srcAddrOrFill != nullptr)
            {
                srcAddrOrFill->gtLsraInfo.setSrcCandidates(l, RBM_WRITE_BARRIER_SRC_BYREF);
            }
        }
        else
        {
            // CopyBlk
            short     internalIntCount      = 0;
            regMaskTP internalIntCandidates = RBM_NONE;

            if (blkNode->gtBlkOpKind == GenTreeBlk::BlkOpKindUnroll)
            {
                // TODO-ARM-CQ: cpblk loop unrolling is currently not implemented.
                // In case of a CpBlk with a constant size and less than CPBLK_UNROLL_LIMIT size
                // we should unroll the loop to improve CQ.
                // For reference see the code in lsraxarch.cpp.
                NYI_ARM("cpblk loop unrolling is currently not implemented.");

#ifdef _TARGET_ARM64_

                internalIntCount      = 1;
                internalIntCandidates = RBM_ALLINT;

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
                dstAddr->gtLsraInfo.setSrcCandidates(l, RBM_ARG_0);
                // The srcAddr goes in arg1.
                if (srcAddrOrFill != nullptr)
                {
                    srcAddrOrFill->gtLsraInfo.setSrcCandidates(l, RBM_ARG_1);
                }
                if (size != 0)
                {
                    // Reserve a temp register for the block size argument.
                    internalIntCandidates |= RBM_ARG_2;
                    internalIntCount++;
                }
                else
                {
                    // The block size argument is a third argument to GT_STORE_DYN_BLK
                    noway_assert(blkNode->gtOper == GT_STORE_DYN_BLK);
                    blkNode->gtLsraInfo.setSrcCount(3);
                    GenTree* blockSize = blkNode->AsDynBlk()->gtDynamicSize;
                    blockSize->gtLsraInfo.setSrcCandidates(l, RBM_ARG_2);
                }
            }
            if (internalIntCount != 0)
            {
                blkNode->gtLsraInfo.internalIntCount = internalIntCount;
                blkNode->gtLsraInfo.setInternalCandidates(l, internalIntCandidates);
            }
        }
    }
}

#endif // _TARGET_ARMARCH_

#endif // !LEGACY_BACKEND
