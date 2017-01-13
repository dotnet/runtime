// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                           Lowering for ARM                                XX
XX                                                                           XX
XX  This encapsulates all the logic for lowering trees for the ARM           XX
XX  architecture.  For a more detailed view of what is lowering, please      XX
XX  take a look at Lower.cpp                                                 XX
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
// LowerStoreLoc: Lower a store of a lclVar
//
// Arguments:
//    storeLoc - the local store (GT_STORE_LCL_FLD or GT_STORE_LCL_VAR)
//
// Notes:
//    This involves:
//    - Setting the appropriate candidates for a store of a multi-reg call return value.
//    - Handling of contained immediates and widening operations of unsigneds.
//
void Lowering::LowerStoreLoc(GenTreeLclVarCommon* storeLoc)
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

    // Try to widen the ops if they are going into a local var.
    if ((storeLoc->gtOper == GT_STORE_LCL_VAR) && (op1->gtOper == GT_CNS_INT))
    {
        GenTreeIntCon* con    = op1->AsIntCon();
        ssize_t        ival   = con->gtIconVal;
        unsigned       varNum = storeLoc->gtLclNum;
        LclVarDsc*     varDsc = comp->lvaTable + varNum;

        if (varDsc->lvIsSIMDType())
        {
            noway_assert(storeLoc->gtType != TYP_STRUCT);
        }
        unsigned size = genTypeSize(storeLoc);
        // If we are storing a constant into a local variable
        // we extend the size of the store here
        if ((size < 4) && !varTypeIsStruct(varDsc))
        {
            if (!varTypeIsUnsigned(varDsc))
            {
                if (genTypeSize(storeLoc) == 1)
                {
                    if ((ival & 0x7f) != ival)
                    {
                        ival = ival | 0xffffff00;
                    }
                }
                else
                {
                    assert(genTypeSize(storeLoc) == 2);
                    if ((ival & 0x7fff) != ival)
                    {
                        ival = ival | 0xffff0000;
                    }
                }
            }

            // A local stack slot is at least 4 bytes in size, regardless of
            // what the local var is typed as, so auto-promote it here
            // unless it is a field of a promoted struct
            // TODO-ARM-CQ: if the field is promoted shouldn't we also be able to do this?
            if (!varDsc->lvIsStructField)
            {
                storeLoc->gtType = TYP_INT;
                con->SetIconValue(ival);
            }
        }
    }
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
    CheckImmedAndMakeContained(tree, tree->gtOp.gtOp2);
}

//------------------------------------------------------------------------
// LowerCast: Lower GT_CAST(srcType, DstType) nodes.
//
// Arguments:
//    tree - GT_CAST node to be lowered
//
// Return Value:
//    None.
//
// Notes:
//    Casts from small int type to float/double are transformed as follows:
//    GT_CAST(byte, float/double)     =   GT_CAST(GT_CAST(byte, int32), float/double)
//    GT_CAST(sbyte, float/double)    =   GT_CAST(GT_CAST(sbyte, int32), float/double)
//    GT_CAST(int16, float/double)    =   GT_CAST(GT_CAST(int16, int32), float/double)
//    GT_CAST(uint16, float/double)   =   GT_CAST(GT_CAST(uint16, int32), float/double)
//
//    Similarly casts from float/double to a smaller int type are transformed as follows:
//    GT_CAST(float/double, byte)     =   GT_CAST(GT_CAST(float/double, int32), byte)
//    GT_CAST(float/double, sbyte)    =   GT_CAST(GT_CAST(float/double, int32), sbyte)
//    GT_CAST(float/double, int16)    =   GT_CAST(GT_CAST(double/double, int32), int16)
//    GT_CAST(float/double, uint16)   =   GT_CAST(GT_CAST(double/double, int32), uint16)
//
//    Note that for the overflow conversions we still depend on helper calls and
//    don't expect to see them here.
//    i) GT_CAST(float/double, int type with overflow detection)
//
void Lowering::LowerCast(GenTree* tree)
{
    assert(tree->OperGet() == GT_CAST);

    JITDUMP("LowerCast for: ");
    DISPNODE(tree);
    JITDUMP("\n");

    GenTreePtr op1     = tree->gtOp.gtOp1;
    var_types  dstType = tree->CastToType();
    var_types  srcType = op1->TypeGet();
    var_types  tmpType = TYP_UNDEF;

    // TODO-ARM-Cleanup: Remove following NYI assertions.
    if (varTypeIsFloating(srcType))
    {
        NYI_ARM("Lowering for cast from float"); // Not tested yet.
        noway_assert(!tree->gtOverflow());
    }

    // Case of src is a small type and dst is a floating point type.
    if (varTypeIsSmall(srcType) && varTypeIsFloating(dstType))
    {
        NYI_ARM("Lowering for cast from small type to float"); // Not tested yet.
        // These conversions can never be overflow detecting ones.
        noway_assert(!tree->gtOverflow());
        tmpType = TYP_INT;
    }
    // case of src is a floating point type and dst is a small type.
    else if (varTypeIsFloating(srcType) && varTypeIsSmall(dstType))
    {
        NYI_ARM("Lowering for cast from float to small type"); // Not tested yet.
        tmpType = TYP_INT;
    }

    if (tmpType != TYP_UNDEF)
    {
        GenTreePtr tmp = comp->gtNewCastNode(tmpType, op1, tmpType);
        tmp->gtFlags |= (tree->gtFlags & (GTF_UNSIGNED | GTF_OVERFLOW | GTF_EXCEPT));

        tree->gtFlags &= ~GTF_UNSIGNED;
        tree->gtOp.gtOp1 = tmp;
        BlockRange().InsertAfter(op1, tmp);
    }
}

//------------------------------------------------------------------------
// LowerRotate: Lower GT_ROL and GT_ROL nodes.
//
// Arguments:
//    tree - the node to lower
//
// Return Value:
//    None.
//
void Lowering::LowerRotate(GenTreePtr tree)
{
    NYI_ARM("ARM Lowering for ROL and ROR");
}

//------------------------------------------------------------------------
// LowerGCWriteBarrier: GC lowering helper.
//
// Arguments:
//    tree - the node to lower
//
// Return Value:
//    None.
//
void Lowering::LowerGCWriteBarrier(GenTree* tree)
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
// SetIndirAddrOpCounts: Specify register requirements for address expression
//                       of an indirection operation.
//
// Arguments:
//    indirTree - GT_IND, GT_STOREIND, block node or GT_NULLCHECK gentree node
//
void Lowering::SetIndirAddrOpCounts(GenTreePtr indirTree)
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

    GenTree*  op1           = tree->gtGetOp1();
    regMaskTP useCandidates = RBM_NONE;

    info->srcCount = (tree->TypeGet() == TYP_VOID) ? 0 : 1;
    info->dstCount = 0;

    if (varTypeIsStruct(tree))
    {
        NYI_ARM("struct return");
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

        var_types argType    = argNode->TypeGet();
        bool      argIsFloat = varTypeIsFloating(argType);
        NYI_IF(argIsFloat, "float argument");
        callHasFloatRegArgs |= argIsFloat;

        regNumber argReg = curArgTabEntry->regNum;
        // We will setup argMask to the set of all registers that compose this argument
        regMaskTP argMask = 0;

        argNode = argNode->gtEffectiveVal();

        // A GT_FIELD_LIST has a TYP_VOID, but is used to represent a multireg struct
        if (varTypeIsStruct(argNode) || (argNode->gtOper == GT_FIELD_LIST))
        {
            GenTreePtr actualArgNode = argNode;
            unsigned   originalSize  = 0;

            if (argNode->gtOper == GT_FIELD_LIST)
            {
                // There could be up to 2-4 PUTARG_REGs in the list (3 or 4 can only occur for HFAs)
                GenTreeFieldList* fieldListPtr = argNode->AsFieldList();

                // Initailize the first register and the first regmask in our list
                regNumber targetReg    = argReg;
                regMaskTP targetMask   = genRegMask(targetReg);
                unsigned  iterationNum = 0;
                originalSize           = 0;

                for (; fieldListPtr; fieldListPtr = fieldListPtr->Rest())
                {
                    GenTreePtr putArgRegNode = fieldListPtr->Current();
                    assert(putArgRegNode->gtOper == GT_PUTARG_REG);
                    GenTreePtr putArgChild = putArgRegNode->gtOp.gtOp1;

                    originalSize += REGSIZE_BYTES; // 8 bytes

                    // Record the register requirements for the GT_PUTARG_REG node
                    putArgRegNode->gtLsraInfo.setDstCandidates(l, targetMask);
                    putArgRegNode->gtLsraInfo.setSrcCandidates(l, targetMask);

                    // To avoid redundant moves, request that the argument child tree be
                    // computed in the register in which the argument is passed to the call.
                    putArgChild->gtLsraInfo.setSrcCandidates(l, targetMask);

                    // We consume one source for each item in this list
                    info->srcCount++;
                    iterationNum++;

                    // Update targetReg and targetMask for the next putarg_reg (if any)
                    targetReg  = genRegArgNext(targetReg);
                    targetMask = genRegMask(targetReg);
                }
            }
            else
            {
#ifdef DEBUG
                compiler->gtDispTreeRange(BlockRange(), argNode);
#endif
                noway_assert(!"Unsupported TYP_STRUCT arg kind");
            }

            unsigned  slots          = ((unsigned)(roundUp(originalSize, REGSIZE_BYTES))) / REGSIZE_BYTES;
            regNumber curReg         = argReg;
            regNumber lastReg        = argIsFloat ? REG_ARG_FP_LAST : REG_ARG_LAST;
            unsigned  remainingSlots = slots;

            while (remainingSlots > 0)
            {
                argMask |= genRegMask(curReg);
                remainingSlots--;

                if (curReg == lastReg)
                    break;

                curReg = genRegArgNext(curReg);
            }

            // Struct typed arguments must be fully passed in registers (Reg/Stk split not allowed)
            noway_assert(remainingSlots == 0);
            argNode->gtLsraInfo.internalIntCount = 0;
        }
        else // A scalar argument (not a struct)
        {
            // We consume one source
            info->srcCount++;

            argMask |= genRegMask(argReg);
            argNode->gtLsraInfo.setDstCandidates(l, argMask);
            argNode->gtLsraInfo.setSrcCandidates(l, argMask);

            if (argNode->gtOper == GT_PUTARG_REG)
            {
                GenTreePtr putArgChild = argNode->gtOp.gtOp1;

                // To avoid redundant moves, request that the argument child tree be
                // computed in the register in which the argument is passed to the call.
                putArgChild->gtLsraInfo.setSrcCandidates(l, argMask);
            }
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
//    This code is refactored originally from LSRA.
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

    switch (tree->OperGet())
    {
        GenTree* op1;
        GenTree* op2;

        case GT_STORE_LCL_FLD:
        case GT_STORE_LCL_VAR:
            info->srcCount = 1;
            info->dstCount = 0;
            LowerStoreLoc(tree->AsLclVarCommon());
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
                NYI_ARM("float cast");
            }
#endif // DEBUG

            if (tree->gtOverflow())
            {
                NYI_ARM("overflow checks");
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
            info->srcCount         = 2;
            info->internalIntCount = 1;
            info->dstCount         = 0;
            break;

        case GT_ASG:
        case GT_ASG_ADD:
        case GT_ASG_SUB:
            noway_assert(!"We should never hit any assignment operator in lowering");
            info->srcCount = 0;
            info->dstCount = 0;
            break;

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

            if ((index != nullptr) && (cns != 0))
            {
                NYI_ARM("GT_LEA: index and cns are not nil");
            }
            else if (!emitter::emitIns_valid_imm_for_add(cns, INS_FLAGS_DONT_CARE))
            {
                NYI_ARM("GT_LEA: invalid imm");
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
        {
            info->srcCount = 2;
            info->dstCount = 1;

            GenTreePtr shiftBy = tree->gtOp.gtOp2;
            GenTreePtr source  = tree->gtOp.gtOp1;
            if (shiftBy->IsCnsIntOrI())
            {
                l->clearDstCount(shiftBy);
                info->srcCount--;
            }
        }
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

        case GT_STOREIND:
        {
            info->srcCount = 2;
            info->dstCount = 0;
            GenTree* src   = tree->gtOp.gtOp2;

            if (compiler->codeGen->gcInfo.gcIsWriteBarrierAsgNode(tree))
            {
                LowerGCWriteBarrier(tree);
                break;
            }

            SetIndirAddrOpCounts(tree);
        }
        break;

        case GT_NULLCHECK:
            info->dstCount      = 0;
            info->srcCount      = 1;
            info->isLocalDefUse = true;
            // null check is an indirection on an addr
            SetIndirAddrOpCounts(tree);
            break;

        case GT_IND:
            info->dstCount = 1;
            info->srcCount = 1;
            SetIndirAddrOpCounts(tree);
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
            JitTls::GetCompiler()->gtDispTree(tree);
#endif
            NYI_ARM("TreeNodeInfoInit default case");
        case GT_LCL_FLD:
        case GT_LCL_VAR:
        case GT_LCL_VAR_ADDR:
        case GT_CLS_VAR_ADDR:
        case GT_IL_OFFSET:
        case GT_CNS_INT:
        case GT_PUTARG_REG:
        case GT_PUTARG_STK:
            info->dstCount = tree->IsValue() ? 1 : 0;
            if (kind & (GTK_CONST | GTK_LEAF))
            {
                info->srcCount = 0;
            }
            else if (kind & (GTK_SMPOP))
            {
                if (tree->gtGetOp2() != nullptr)
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

//------------------------------------------------------------------------
// IsCallTargetInRange: Can a call target address be encoded in-place?
//
// Return Value:
//    True if the addr fits into the range.
//
bool Lowering::IsCallTargetInRange(void* addr)
{
    return comp->codeGen->validImmForBL((ssize_t)addr);
}

//------------------------------------------------------------------------
// IsContainableImmed: Is an immediate encodable in-place?
//
// Return Value:
//    True if the immediate can be folded into an instruction,
//    for example small enough and non-relocatable.
bool Lowering::IsContainableImmed(GenTree* parentNode, GenTree* childNode)
{
    if (varTypeIsFloating(parentNode->TypeGet()))
    {
        // TODO-ARM-Cleanup: not tested yet.
        NYI_ARM("ARM IsContainableImmed for floating point type");

        // We can contain a floating point 0.0 constant in a compare instruction
        switch (parentNode->OperGet())
        {
            default:
                return false;

            case GT_EQ:
            case GT_NE:
            case GT_LT:
            case GT_LE:
            case GT_GE:
            case GT_GT:
                if (childNode->IsIntegralConst(0))
                    return true;
                break;
        }
    }
    else
    {
        // Make sure we have an actual immediate
        if (!childNode->IsCnsIntOrI())
            return false;
        if (childNode->IsIconHandle() && comp->opts.compReloc)
            return false;

        ssize_t  immVal = childNode->gtIntCon.gtIconVal;
        emitAttr attr   = emitActualTypeSize(childNode->TypeGet());
        emitAttr size   = EA_SIZE(attr);

        switch (parentNode->OperGet())
        {
            default:
                return false;

            case GT_ADD:
            case GT_SUB:
                if (emitter::emitIns_valid_imm_for_add(immVal, INS_FLAGS_DONT_CARE))
                    return true;
                break;

            case GT_EQ:
            case GT_NE:
            case GT_LT:
            case GT_LE:
            case GT_GE:
            case GT_GT:
            case GT_AND:
            case GT_OR:
            case GT_XOR:
                if (emitter::emitIns_valid_imm_for_alu(immVal))
                    return true;
                break;
        }
    }

    return false;
}

#endif // _TARGET_ARM_

#endif // !LEGACY_BACKEND
