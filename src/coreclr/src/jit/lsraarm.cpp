// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                     Register Requirements for ARM                         XX
XX                                                                           XX
XX  This encapsulates all the logic for setting register requirements for    XX
XX  the ARM  architecture.                                                   XX
XX                                                                           XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifndef LEGACY_BACKEND // This file is ONLY used for the RyuJIT backend that uses the linear scan register allocator

#ifdef _TARGET_ARM_

#include "jit.h"
#include "sideeffects.h"
#include "lower.h"
#include "lsra.h"

//------------------------------------------------------------------------
// TreeNodeInfoInitStoreLoc: Lower a store of a lclVar
//
// Arguments:
//    storeLoc - the local store (GT_STORE_LCL_FLD or GT_STORE_LCL_VAR)
//
// Notes:
//    This involves:
//    - Setting the appropriate candidates for a store of a multi-reg call return value.
//    - Handling of contained immediates and widening operations of unsigneds.
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
    info->dstCount = 1;

    GenTreePtr op1     = tree->gtOp.gtOp1;
    GenTreePtr op2     = tree->gtOp.gtOp2;
    var_types  op1Type = op1->TypeGet();
    var_types  op2Type = op2->TypeGet();

    // Long compares will consume GT_LONG nodes, each of which produces two results.
    // Thus for each long operand there will be an additional source.
    // TODO-ARM-CQ: Mark hiOp2 and loOp2 as contained if it is a constant.
    if (varTypeIsLong(op1Type))
    {
        info->srcCount++;
    }
    if (varTypeIsLong(op2Type))
    {
        info->srcCount++;
    }

    CheckImmedAndMakeContained(tree, tree->gtOp.gtOp2);
}

//------------------------------------------------------------------------
// TreeNodeInfoInitGCWriteBarrier: GC lowering helper.
//
// Arguments:
//    tree - the node to lower
//
// Return Value:
//    None.
//
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

    if ((addr->OperGet() == GT_LEA) && IsSafeToContainMem(indirTree, addr))
    {
        GenTreeAddrMode* lea = addr->AsAddrMode();
        base                 = lea->Base();
        index                = lea->Index();
        cns                  = lea->gtOffset;

        m_lsra->clearOperandCounts(addr);
        // The srcCount is decremented because addr is now "contained",
        // then we account for the base and index below, if they are non-null.
        info->srcCount--;
    }
    else if (comp->codeGen->genCreateAddrMode(addr, -1, true, 0, &rev, &base, &index, &mul, &cns, true /*nogen*/) &&
             !(modifiedSources = AreSourcesPossiblyModifiedLocals(indirTree, base, index)))
    {
        // An addressing mode will be constructed that may cause some
        // nodes to not need a register, and cause others' lifetimes to be extended
        // to the GT_IND or even its parent if it's an assignment

        assert(base != addr);
        m_lsra->clearOperandCounts(addr);

        GenTreePtr arrLength = nullptr;

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

    if (base != nullptr)
    {
        info->srcCount++;
    }

    if (index != nullptr && !modifiedSources)
    {
        info->srcCount++;
        info->internalIntCount++;
    }
}

//------------------------------------------------------------------------
// TreeNodeInfoInitReturn: Set the NodeInfo for a GT_RETURN.
//
// Arguments:
//    tree - The node of interest
//
// Return Value:
//    None.
//
void Lowering::TreeNodeInfoInitReturn(GenTree* tree)
{
    TreeNodeInfo* info     = &(tree->gtLsraInfo);
    LinearScan*   l        = m_lsra;
    Compiler*     compiler = comp;

    if (tree->TypeGet() == TYP_LONG)
    {
        GenTree* op1 = tree->gtGetOp1();
        noway_assert(op1->OperGet() == GT_LONG);
        GenTree* loVal = op1->gtGetOp1();
        GenTree* hiVal = op1->gtGetOp2();
        info->srcCount = 2;
        loVal->gtLsraInfo.setSrcCandidates(l, RBM_LNGRET_LO);
        hiVal->gtLsraInfo.setSrcCandidates(l, RBM_LNGRET_HI);
        info->dstCount = 0;
    }
    else
    {
        GenTree*  op1           = tree->gtGetOp1();
        regMaskTP useCandidates = RBM_NONE;

        info->srcCount = (tree->TypeGet() == TYP_VOID) ? 0 : 1;
        info->dstCount = 0;

        if (varTypeIsStruct(tree))
        {
            // op1 has to be either an lclvar or a multi-reg returning call
            if (op1->OperGet() == GT_LCL_VAR)
            {
                GenTreeLclVarCommon* lclVarCommon = op1->AsLclVarCommon();
                LclVarDsc*           varDsc       = &(compiler->lvaTable[lclVarCommon->gtLclNum]);
                assert(varDsc->lvIsMultiRegRet);

                // Mark var as contained if not enregistrable.
                if (!varTypeIsEnregisterableStruct(op1))
                {
                    MakeSrcContained(tree, op1);
                }
            }
            else
            {
                noway_assert(op1->IsMultiRegCall());

                ReturnTypeDesc* retTypeDesc = op1->AsCall()->GetReturnTypeDesc();
                info->srcCount              = retTypeDesc->GetReturnRegCount();
                useCandidates               = retTypeDesc->GetABIReturnRegs();
            }
        }
        else
        {
            // Non-struct type return - determine useCandidates
            switch (tree->TypeGet())
            {
                case TYP_VOID:
                    useCandidates = RBM_NONE;
                    break;
                case TYP_FLOAT:
                    useCandidates = RBM_FLOATRET;
                    break;
                case TYP_DOUBLE:
                    useCandidates = RBM_DOUBLERET;
                    break;
                case TYP_LONG:
                    useCandidates = RBM_LNGRET;
                    break;
                default:
                    useCandidates = RBM_INTRET;
                    break;
            }
        }

        if (useCandidates != RBM_NONE)
        {
            tree->gtOp.gtOp1->gtLsraInfo.setSrcCandidates(l, useCandidates);
        }
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
        }
    }
    else
    {
        info->internalIntCount = 1;
    }

    RegisterType registerType = call->TypeGet();

    // Set destination candidates for return value of the call.
    if (call->IsHelperCall(compiler, CORINFO_HELP_INIT_PINVOKE_FRAME))
    {
        // The ARM CORINFO_HELP_INIT_PINVOKE_FRAME helper uses a custom calling convention that returns with
        // TCB in REG_PINVOKE_TCB. fgMorphCall() sets the correct argument registers.
        info->setDstCandidates(l, RBM_PINVOKE_TCB);
    }
    else if (hasMultiRegRetVal)
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

    if (call->IsVarargs() && callHasFloatRegArgs && !call->IsFastTailCall() && (ctrlExpr != nullptr))
    {
        NYI_ARM("float reg varargs");
    }

    if (call->NeedsNullCheck())
    {
        info->internalIntCount++;
    }
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
            // We could use a ldp/stp sequence so we need two internal registers
            argNode->gtLsraInfo.internalIntCount = 2;

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

void Lowering::TreeNodeInfoInitLclHeap(GenTree* tree)
{
    TreeNodeInfo* info     = &(tree->gtLsraInfo);
    LinearScan*   l        = m_lsra;
    Compiler*     compiler = comp;

    info->srcCount = 1;
    info->dstCount = 1;

    // Need a variable number of temp regs (see genLclHeap() in codegenarm.cpp):
    // Here '-' means don't care.
    //
    //  Size?                   Init Memory?    # temp regs
    //   0                          -               0
    //   const and <=4 ptr words    -             hasPspSym ? 1 : 0
    //   const and <PageSize        No            hasPspSym ? 1 : 0
    //   >4 ptr words               Yes           hasPspSym ? 2 : 1
    //   Non-const                  Yes           hasPspSym ? 2 : 1
    //   Non-const                  No            hasPspSym ? 2 : 1

    bool hasPspSym;
#if FEATURE_EH_FUNCLETS
    hasPspSym = (compiler->lvaPSPSym != BAD_VAR_NUM);
#else
    hasPspSym = false;
#endif

    GenTreePtr size = tree->gtOp.gtOp1;
    if (size->IsCnsIntOrI())
    {
        MakeSrcContained(tree, size);

        size_t sizeVal = size->gtIntCon.gtIconVal;
        if (sizeVal == 0)
        {
            info->internalIntCount = 0;
        }
        else
        {
            sizeVal                          = AlignUp(sizeVal, STACK_ALIGN);
            size_t cntStackAlignedWidthItems = (sizeVal >> STACK_ALIGN_SHIFT);

            // For small allocations up to 4 store instructions
            if (cntStackAlignedWidthItems <= 4)
            {
                info->internalIntCount = 0;
            }
            else if (!compiler->info.compInitMem)
            {
                // No need to initialize allocated stack space.
                if (sizeVal < compiler->eeGetPageSize())
                {
                    info->internalIntCount = 0;
                }
                else
                {
                    // target (regCnt) + tmp + [psp]
                    info->internalIntCount       = 1;
                    info->isInternalRegDelayFree = true;
                }
            }
            else
            {
                // target (regCnt) + tmp + [psp]
                info->internalIntCount       = 1;
                info->isInternalRegDelayFree = true;
            }

            if (hasPspSym)
            {
                info->internalIntCount++;
            }
        }
    }
    else
    {
        // target (regCnt) + tmp + [psp]
        info->internalIntCount       = hasPspSym ? 2 : 1;
        info->isInternalRegDelayFree = true;
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
            NYI_ARM("GT_STORE_OBJ is needed of write barriers implementation");
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

//------------------------------------------------------------------------
// TreeNodeInfoInit: Set the register requirements for RA.
//
// Notes:
//    Takes care of annotating the register requirements
//    for every TreeNodeInfo struct that maps to each tree node.
//
// Preconditions:
//    LSRA has been initialized and there is a TreeNodeInfo node
//    already allocated and initialized for every tree in the IR.
//
// Postconditions:
//    Every TreeNodeInfo instance has the right annotations on register
//    requirements needed by LSRA to build the Interval Table (source,
//    destination and internal [temp] register counts).
//
void Lowering::TreeNodeInfoInit(GenTree* tree)
{
    LinearScan* l        = m_lsra;
    Compiler*   compiler = comp;

    unsigned      kind         = tree->OperKind();
    TreeNodeInfo* info         = &(tree->gtLsraInfo);
    RegisterType  registerType = TypeGet(tree);

    JITDUMP("TreeNodeInfoInit for: ");
    DISPNODE(tree);

    NYI_IF(tree->TypeGet() == TYP_DOUBLE, "lowering double");

    switch (tree->OperGet())
    {
        GenTree* op1;
        GenTree* op2;

        case GT_STORE_LCL_FLD:
        case GT_STORE_LCL_VAR:
            if (tree->gtGetOp1()->OperGet() == GT_LONG)
            {
                info->srcCount = 2;
            }
            else
            {
                info->srcCount = 1;
            }
            info->dstCount = 0;
            LowerStoreLoc(tree->AsLclVarCommon());
            TreeNodeInfoInitStoreLoc(tree->AsLclVarCommon());
            break;

        case GT_NOP:
            // A GT_NOP is either a passthrough (if it is void, or if it has
            // a child), but must be considered to produce a dummy value if it
            // has a type but no child
            info->srcCount = 0;
            if (tree->TypeGet() != TYP_VOID && tree->gtOp.gtOp1 == nullptr)
            {
                info->dstCount = 1;
            }
            else
            {
                info->dstCount = 0;
            }
            break;

        case GT_INTRINSIC:
        {
            // TODO-ARM: Implement other type of intrinsics (round, sqrt and etc.)
            // Both operand and its result must be of the same floating point type.
            op1 = tree->gtOp.gtOp1;
            assert(varTypeIsFloating(op1));
            assert(op1->TypeGet() == tree->TypeGet());

            switch (tree->gtIntrinsic.gtIntrinsicId)
            {
                case CORINFO_INTRINSIC_Abs:
                case CORINFO_INTRINSIC_Sqrt:
                    info->srcCount = 1;
                    info->dstCount = 1;
                    break;
                default:
                    NYI_ARM("Lowering::TreeNodeInfoInit for GT_INTRINSIC");
                    break;
            }
        }
        break;

        case GT_CAST:
        {
            info->srcCount = 1;
            info->dstCount = 1;

            // Non-overflow casts to/from float/double are done using SSE2 instructions
            // and that allow the source operand to be either a reg or memop. Given the
            // fact that casts from small int to float/double are done as two-level casts,
            // the source operand is always guaranteed to be of size 4 or 8 bytes.
            var_types  castToType = tree->CastToType();
            GenTreePtr castOp     = tree->gtCast.CastOp();
            var_types  castOpType = castOp->TypeGet();
            if (tree->gtFlags & GTF_UNSIGNED)
            {
                castOpType = genUnsignedType(castOpType);
            }
#ifdef DEBUG
            if (!tree->gtOverflow() && (varTypeIsFloating(castToType) || varTypeIsFloating(castOpType)))
            {
                // If converting to float/double, the operand must be 4 or 8 byte in size.
                if (varTypeIsFloating(castToType))
                {
                    unsigned opSize = genTypeSize(castOpType);
                    assert(opSize == 4 || opSize == 8);
                }
            }
#endif // DEBUG

            if (varTypeIsLong(castOpType))
            {
                noway_assert(castOp->OperGet() == GT_LONG);
                info->srcCount = 2;
            }

            CastInfo castInfo;

            // Get information about the cast.
            getCastDescription(tree, &castInfo);

            if (castInfo.requiresOverflowCheck)
            {
                var_types srcType = castOp->TypeGet();
                emitAttr  cmpSize = EA_ATTR(genTypeSize(srcType));

                // If we cannot store the comparisons in an immediate for either
                // comparing against the max or min value, then we will need to
                // reserve a temporary register.

                bool canStoreMaxValue = emitter::emitIns_valid_imm_for_cmp(castInfo.typeMax, INS_FLAGS_DONT_CARE);
                bool canStoreMinValue = emitter::emitIns_valid_imm_for_cmp(castInfo.typeMin, INS_FLAGS_DONT_CARE);

                if (!canStoreMaxValue || !canStoreMinValue)
                {
                    info->internalIntCount = 1;
                }
            }
        }
        break;

        case GT_JTRUE:
            info->srcCount = 0;
            info->dstCount = 0;
            l->clearDstCount(tree->gtOp.gtOp1);
            break;

        case GT_JMP:
            info->srcCount = 0;
            info->dstCount = 0;
            break;

        case GT_SWITCH:
            // This should never occur since switch nodes must not be visible at this
            // point in the JIT.
            info->srcCount = 0;
            info->dstCount = 0; // To avoid getting uninit errors.
            noway_assert(!"Switch must be lowered at this point");
            break;

        case GT_JMPTABLE:
            info->srcCount = 0;
            info->dstCount = 1;
            break;

        case GT_SWITCH_TABLE:
            info->srcCount = 2;
            info->dstCount = 0;
            break;

        case GT_ASG:
        case GT_ASG_ADD:
        case GT_ASG_SUB:
            noway_assert(!"We should never hit any assignment operator in lowering");
            info->srcCount = 0;
            info->dstCount = 0;
            break;

        case GT_ADD_LO:
        case GT_ADD_HI:
        case GT_SUB_LO:
        case GT_SUB_HI:
        case GT_ADD:
        case GT_SUB:
            if (varTypeIsFloating(tree->TypeGet()))
            {
                // overflow operations aren't supported on float/double types.
                assert(!tree->gtOverflow());

                // No implicit conversions at this stage as the expectation is that
                // everything is made explicit by adding casts.
                assert(tree->gtOp.gtOp1->TypeGet() == tree->gtOp.gtOp2->TypeGet());

                info->srcCount = 2;
                info->dstCount = 1;

                break;
            }

            __fallthrough;

        case GT_AND:
        case GT_OR:
        case GT_XOR:
            info->srcCount = 2;
            info->dstCount = 1;
            // Check and make op2 contained (if it is a containable immediate)
            CheckImmedAndMakeContained(tree, tree->gtOp.gtOp2);
            break;

        case GT_RETURNTRAP:
            // this just turns into a compare of its child with an int
            // + a conditional call
            info->srcCount = 1;
            info->dstCount = 0;
            break;

        case GT_MUL:
            if (tree->gtOverflow())
            {
                // Need a register different from target reg to check for overflow.
                info->internalIntCount = 2;
            }
            __fallthrough;

        case GT_DIV:
        case GT_MULHI:
        case GT_UDIV:
        {
            info->srcCount = 2;
            info->dstCount = 1;
        }
        break;

        case GT_LIST:
        case GT_FIELD_LIST:
        case GT_ARGPLACE:
        case GT_NO_OP:
        case GT_START_NONGC:
        case GT_PROF_HOOK:
            info->srcCount = 0;
            info->dstCount = 0;
            break;

        case GT_LONG:
            if ((tree->gtLIRFlags & LIR::Flags::IsUnusedValue) != 0)
            {
                // An unused GT_LONG node needs to consume its sources.
                info->srcCount = 2;
            }
            else
            {
                // Passthrough
                info->srcCount = 0;
            }

            info->dstCount = 0;
            break;

        case GT_CNS_DBL:
            info->srcCount = 0;
            info->dstCount = 1;
            if (tree->TypeGet() == TYP_FLOAT)
            {
                // An int register for float constant
                info->internalIntCount = 1;
            }
            else
            {
                // TYP_DOUBLE
                assert(tree->TypeGet() == TYP_DOUBLE);

                // Two int registers for double constant
                info->internalIntCount = 2;
            }
            break;

        case GT_RETURN:
            TreeNodeInfoInitReturn(tree);
            break;

        case GT_RETFILT:
            if (tree->TypeGet() == TYP_VOID)
            {
                info->srcCount = 0;
                info->dstCount = 0;
            }
            else
            {
                assert(tree->TypeGet() == TYP_INT);

                info->srcCount = 1;
                info->dstCount = 0;

                info->setSrcCandidates(l, RBM_INTRET);
                tree->gtOp.gtOp1->gtLsraInfo.setSrcCandidates(l, RBM_INTRET);
            }
            break;

        case GT_ARR_BOUNDS_CHECK:
#ifdef FEATURE_SIMD
        case GT_SIMD_CHK:
#endif // FEATURE_SIMD
        {
            // Consumes arrLen & index - has no result
            info->srcCount = 2;
            info->dstCount = 0;
        }
        break;

        case GT_ARR_ELEM:
            // These must have been lowered to GT_ARR_INDEX
            noway_assert(!"We should never see a GT_ARR_ELEM in lowering");
            info->srcCount = 0;
            info->dstCount = 0;
            break;

        case GT_ARR_INDEX:
            info->srcCount = 2;
            info->dstCount = 1;

            // We need one internal register when generating code for GT_ARR_INDEX, however the
            // register allocator always may just give us the same one as it gives us for the 'dst'
            // as a workaround we will just ask for two internal registers.
            //
            info->internalIntCount = 2;

            // For GT_ARR_INDEX, the lifetime of the arrObj must be extended because it is actually used multiple
            // times while the result is being computed.
            tree->AsArrIndex()->ArrObj()->gtLsraInfo.isDelayFree = true;
            info->hasDelayFreeSrc                                = true;
            break;

        case GT_ARR_OFFSET:
            // This consumes the offset, if any, the arrObj and the effective index,
            // and produces the flattened offset for this dimension.
            info->srcCount         = 3;
            info->dstCount         = 1;
            info->internalIntCount = 1;

            // we don't want to generate code for this
            if (tree->gtArrOffs.gtOffset->IsIntegralConst(0))
            {
                MakeSrcContained(tree, tree->gtArrOffs.gtOffset);
            }
            break;

        case GT_LEA:
        {
            GenTreeAddrMode* lea = tree->AsAddrMode();

            GenTree* base  = lea->Base();
            GenTree* index = lea->Index();
            unsigned cns   = lea->gtOffset;

            // This LEA is instantiating an address,
            // so we set up the srcCount and dstCount here.
            info->srcCount = 0;
            if (base != nullptr)
            {
                info->srcCount++;
            }
            if (index != nullptr)
            {
                info->srcCount++;
            }
            info->dstCount = 1;

            // On ARM we may need a single internal register
            // (when both conditions are true then we still only need a single internal register)
            if ((index != nullptr) && (cns != 0))
            {
                // ARM does not support both Index and offset so we need an internal register
                info->internalIntCount = 1;
            }
            else if (!emitter::emitIns_valid_imm_for_add(cns, INS_FLAGS_DONT_CARE))
            {
                // This offset can't be contained in the add instruction, so we need an internal register
                info->internalIntCount = 1;
            }
        }
        break;

        case GT_NEG:
            info->srcCount = 1;
            info->dstCount = 1;
            break;

        case GT_NOT:
            info->srcCount = 1;
            info->dstCount = 1;
            break;

        case GT_LSH:
        case GT_RSH:
        case GT_RSZ:
        case GT_ROR:
        case GT_LSH_HI:
        case GT_RSH_LO:
            TreeNodeInfoInitShiftRotate(tree);
            break;

        case GT_EQ:
        case GT_NE:
        case GT_LT:
        case GT_LE:
        case GT_GE:
        case GT_GT:
            TreeNodeInfoInitCmp(tree);
            break;

        case GT_CALL:
            TreeNodeInfoInitCall(tree->AsCall());
            break;

        case GT_STORE_BLK:
        case GT_STORE_OBJ:
        case GT_STORE_DYN_BLK:
            LowerBlockStore(tree->AsBlk());
            TreeNodeInfoInitBlockStore(tree->AsBlk());
            break;

        case GT_LCLHEAP:
            TreeNodeInfoInitLclHeap(tree);
            break;

        case GT_STOREIND:
        {
            info->srcCount = 2;
            info->dstCount = 0;
            GenTree* src   = tree->gtOp.gtOp2;

            if (compiler->codeGen->gcInfo.gcIsWriteBarrierAsgNode(tree))
            {
                TreeNodeInfoInitGCWriteBarrier(tree);
                break;
            }

            TreeNodeInfoInitIndir(tree);
        }
        break;

        case GT_NULLCHECK:
            info->dstCount      = 0;
            info->srcCount      = 1;
            info->isLocalDefUse = true;
            // null check is an indirection on an addr
            TreeNodeInfoInitIndir(tree);
            break;

        case GT_IND:
            info->dstCount = 1;
            info->srcCount = 1;
            TreeNodeInfoInitIndir(tree);
            break;

        case GT_CATCH_ARG:
            info->srcCount = 0;
            info->dstCount = 1;
            info->setDstCandidates(l, RBM_EXCEPTION_OBJECT);
            break;

        case GT_CLS_VAR:
            info->srcCount = 0;
            // GT_CLS_VAR, by the time we reach the backend, must always
            // be a pure use.
            // It will produce a result of the type of the
            // node, and use an internal register for the address.

            info->dstCount = 1;
            assert((tree->gtFlags & (GTF_VAR_DEF | GTF_VAR_USEASG | GTF_VAR_USEDEF)) == 0);
            info->internalIntCount = 1;
            break;

        default:
#ifdef DEBUG
            char message[256];
            _snprintf_s(message, _countof(message), _TRUNCATE, "NYI: Unimplemented node type %s",
                        GenTree::NodeName(tree->OperGet()));
            NYIRAW(message);
#else
            NYI_ARM("TreeNodeInfoInit default case");
#endif
        case GT_LCL_FLD:
        case GT_LCL_FLD_ADDR:
        case GT_LCL_VAR:
        case GT_LCL_VAR_ADDR:
        case GT_PHYSREG:
        case GT_CLS_VAR_ADDR:
        case GT_IL_OFFSET:
        case GT_CNS_INT:
        case GT_PUTARG_REG:
        case GT_PUTARG_STK:
        case GT_LABEL:
        case GT_PINVOKE_PROLOG:
        case GT_JCC:
        case GT_MEMORYBARRIER:
            info->dstCount = tree->IsValue() ? 1 : 0;
            if (kind & (GTK_CONST | GTK_LEAF))
            {
                info->srcCount = 0;
            }
            else if (kind & (GTK_SMPOP))
            {
                if (tree->gtGetOp2IfPresent() != nullptr)
                {
                    info->srcCount = 2;
                }
                else
                {
                    info->srcCount = 1;
                }
            }
            else
            {
                unreached();
            }
            break;
    } // end switch (tree->OperGet())

    // We need to be sure that we've set info->srcCount and info->dstCount appropriately
    assert((info->dstCount < 2) || tree->IsMultiRegCall());
}

#endif // _TARGET_ARM_

#endif // !LEGACY_BACKEND
