// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                            LoopCloning                                    XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"

#ifdef DEBUG

//--------------------------------------------------------------------------------------------------
// ArrIndex::Print - debug print an ArrIndex struct in form: `V01[V02][V03]`.
//
// Arguments:
//      dim     (Optional) Print up to but not including this dimension. Default: print all dimensions.
//
void ArrIndex::Print(unsigned dim /* = -1 */)
{
    printf("V%02d", arrLcl);
    for (unsigned i = 0; i < ((dim == (unsigned)-1) ? rank : dim); ++i)
    {
        printf("[V%02d]", indLcls.Get(i));
    }
}

//--------------------------------------------------------------------------------------------------
// ArrIndex::PrintBoundsCheckNodes - debug print an ArrIndex struct bounds check node tree ids in
// form: `[000125][000113]`.
//
// Arguments:
//      dim     (Optional) Print up to but not including this dimension. Default: print all dimensions.
//
void ArrIndex::PrintBoundsCheckNodes(unsigned dim /* = -1 */)
{
    for (unsigned i = 0; i < ((dim == (unsigned)-1) ? rank : dim); ++i)
    {
        Compiler::printTreeID(bndsChks.Get(i));
    }
}

#endif // DEBUG

//--------------------------------------------------------------------------------------------------
// ToGenTree - Convert an arrLen operation into a gentree node.
//
// Arguments:
//      comp    Compiler instance to allocate trees
//      bb      Basic block of the new tree
//
// Return Values:
//      Returns the gen tree representation for arrLen or MD Array node as defined by
//      the "type" member
//
// Notes:
//      This tree produces a GT_IND(GT_INDEX_ADDR) node, the caller is supposed to morph it.
//
GenTree* LC_Array::ToGenTree(Compiler* comp, BasicBlock* bb)
{
    // If jagged array
    if (type == Jagged)
    {
        // Create a a[i][j][k].length type node.
        GenTree* arr  = comp->gtNewLclvNode(arrIndex->arrLcl, comp->lvaTable[arrIndex->arrLcl].lvType);
        int      rank = GetDimRank();
        for (int i = 0; i < rank; ++i)
        {
            GenTree* idx     = comp->gtNewLclvNode(arrIndex->indLcls[i], comp->lvaTable[arrIndex->indLcls[i]].lvType);
            GenTree* arrAddr = comp->gtNewArrayIndexAddr(arr, idx, TYP_REF, NO_CLASS_HANDLE);

            // Clear the range check flag and mark the index as non-faulting: we guarantee that all necessary range
            // checking has already been done by the time this array index expression is invoked.
            arrAddr->gtFlags &= ~GTF_INX_RNGCHK;
            arrAddr->gtFlags |= GTF_INX_ADDR_NONNULL;

            arr = comp->gtNewIndexIndir(arrAddr->AsIndexAddr());
        }
        // If asked for arrlen invoke arr length operator.
        if (oper == ArrLen)
        {
            GenTree* arrLen = comp->gtNewArrLen(TYP_INT, arr, OFFSETOF__CORINFO_Array__length, bb);

            // We already guaranteed (by a sequence of preceding checks) that the array length operator will not
            // throw an exception because we null checked the base array.
            // So, we should be able to do the following:
            //     arrLen->gtFlags &= ~GTF_EXCEPT;
            //     arrLen->gtFlags |= GTF_IND_NONFAULTING;
            // However, we then end up with a mix of non-faulting array length operators as well as normal faulting
            // array length operators in the slow-path of the cloned loops. CSE doesn't keep these separate, so bails
            // out on creating CSEs on this very useful type of CSE, leading to CQ losses in the cloned loop fast path.
            // TODO-CQ: fix this.
            return arrLen;
        }
        else
        {
            assert(oper == None);
            return arr;
        }
    }
    else
    {
        // TODO-CQ: Optimize for MD Array.
        assert(!"Optimize for MD Array");
    }
    return nullptr;
}

//--------------------------------------------------------------------------------------------------
// ToGenTree - Convert an "identifier" into a gentree node.
//
// Arguments:
//      comp    Compiler instance to allocate trees
//      bb      Basic block of the new tree
//
// Return Values:
//      Returns the gen tree representation for either a constant or a variable or an arrLen operation
//      defined by the "type" member
//
GenTree* LC_Ident::ToGenTree(Compiler* comp, BasicBlock* bb)
{
    // Convert to GenTree nodes.
    switch (type)
    {
        case Const:
            assert(constant <= INT32_MAX);
            return comp->gtNewIconNode(constant);
        case Var:
            return comp->gtNewLclvNode(lclNum, comp->lvaTable[lclNum].lvType);
        case ArrAccess:
            return arrAccess.ToGenTree(comp, bb);
        case Null:
            return comp->gtNewIconNode(0, TYP_REF);
        case ClassHandle:
            return comp->gtNewIconHandleNode((size_t)clsHnd, GTF_ICON_CLASS_HDL);
        case IndirOfLocal:
        {
            GenTree* addr = comp->gtNewLclvNode(lclNum, TYP_REF);
            if (indirOffs != 0)
            {
                addr = comp->gtNewOperNode(GT_ADD, TYP_BYREF, addr,
                                           comp->gtNewIconNode(static_cast<ssize_t>(indirOffs), TYP_I_IMPL));
            }

            GenTree* const indir = comp->gtNewIndir(TYP_I_IMPL, addr, GTF_IND_INVARIANT);
            return indir;
        }
        case MethodAddr:
        {
            GenTreeIntCon* methodAddrHandle = comp->gtNewIconHandleNode((size_t)methAddr, GTF_ICON_FTN_ADDR);
            INDEBUG(methodAddrHandle->gtTargetHandle = (size_t)targetMethHnd);
            return methodAddrHandle;
        }
        case IndirOfMethodAddrSlot:
        {
            GenTreeIntCon* slot = comp->gtNewIconHandleNode((size_t)methAddr, GTF_ICON_FTN_ADDR);
            INDEBUG(slot->gtTargetHandle = (size_t)targetMethHnd);
            GenTree* indir = comp->gtNewIndir(TYP_I_IMPL, slot, GTF_IND_NONFAULTING | GTF_IND_INVARIANT);
            return indir;
        }
        default:
            assert(!"Could not convert LC_Ident to GenTree");
            unreached();
            break;
    }
}

//--------------------------------------------------------------------------------------------------
// ToGenTree - Convert an "expression" into a gentree node.
//
// Arguments:
//      comp    Compiler instance to allocate trees
//      bb      Basic block of the new tree
//
// Return Values:
//      Returns the gen tree representation for either a constant or a variable or an arrLen operation
//      defined by the "type" member
//
GenTree* LC_Expr::ToGenTree(Compiler* comp, BasicBlock* bb)
{
    // Convert to GenTree nodes.
    switch (type)
    {
        case Ident:
            return ident.ToGenTree(comp, bb);
        default:
            assert(!"Could not convert LC_Expr to GenTree");
            unreached();
            break;
    }
}

//--------------------------------------------------------------------------------------------------
// ToGenTree - Convert a "condition" into a gentree node.
//
// Arguments:
//      comp    Compiler instance to allocate trees
//      bb      Basic block of the new tree
//      invert  `true` if the condition should be inverted
//
// Return Values:
//      Returns the GenTree representation for the conditional operator on lhs and rhs trees
//
GenTree* LC_Condition::ToGenTree(Compiler* comp, BasicBlock* bb, bool invert)
{
    GenTree* op1Tree = op1.ToGenTree(comp, bb);
    GenTree* op2Tree = op2.ToGenTree(comp, bb);
    assert(genTypeSize(genActualType(op1Tree->TypeGet())) == genTypeSize(genActualType(op2Tree->TypeGet())));

    GenTree* result = comp->gtNewOperNode(invert ? GenTree::ReverseRelop(oper) : oper, TYP_INT, op1Tree, op2Tree);

    if (compareUnsigned)
    {
        result->gtFlags |= GTF_UNSIGNED;
    }

    return result;
}

//--------------------------------------------------------------------------------------------------
// Evaluates - Evaluate a given loop cloning condition if it can be statically evaluated.
//
// Arguments:
//      pResult     OUT parameter. The evaluation result
//
// Return Values:
//      Returns true if the condition can be statically evaluated. If the condition's result
//      is statically unknown then return false. In other words, `*pResult` is valid only if the
//      function returns true.
//
bool LC_Condition::Evaluates(bool* pResult)
{
    switch (oper)
    {
        case GT_EQ:
        case GT_GE:
        case GT_LE:
            // If op1 == op2 then equality should result in true.
            if (op1 == op2)
            {
                *pResult = true;
                return true;
            }
            break;

        case GT_GT:
        case GT_LT:
        case GT_NE:
            // If op1 == op2 then inequality should result in false.
            if (op1 == op2)
            {
                *pResult = false;
                return true;
            }
            break;

        default:
            // for all other 'oper' kinds, we will return false
            break;
    }
    return false;
}

//--------------------------------------------------------------------------------------------------
// Combines - Check whether two conditions would combine to yield a single new condition.
//
// Arguments:
//      cond        The condition that is checked if it would combine with "*this" condition.
//      newCond     The resulting combined condition.
//
// Return Values:
//      Returns true if "cond" combines with the "this" condition.
//      "newCond" contains the combines condition.
//
// Operation:
//      Check if both conditions are equal. If so, return just 1 of them.
//      Reverse their operators and check if their reversed operands match. If so, return either of them.
//
// Notes:
//      This is not a full-fledged expression optimizer, it is supposed
//      to remove redundant conditions that are generated for optimization
//      opportunities. Anything further should be implemented as needed.
//      For example, for (i = beg; i < end; i += inc) a[i]. Then, the conditions
//      would be: "beg >= 0, end <= a.len, inc > 0"
bool LC_Condition::Combines(const LC_Condition& cond, LC_Condition* newCond)
{
    if (oper == cond.oper && op1 == cond.op1 && op2 == cond.op2)
    {
        *newCond = *this;
        return true;
    }
    else if ((oper == GT_LT || oper == GT_LE || oper == GT_GT || oper == GT_GE) &&
             GenTree::ReverseRelop(oper) == cond.oper && op1 == cond.op2 && op2 == cond.op1)
    {
        *newCond = *this;
        return true;
    }
    return false;
}

//--------------------------------------------------------------------------------------------------
// GetLoopOptInfo - Retrieve the loop opt info candidate array.
//
// Arguments:
//      loopNum     the loop index.
//
// Return Values:
//      Return the optInfo array member. The method doesn't allocate memory.
//
JitExpandArrayStack<LcOptInfo*>* LoopCloneContext::GetLoopOptInfo(unsigned loopNum)
{
    return optInfo[loopNum];
}

//--------------------------------------------------------------------------------------------------
// CancelLoopOptInfo - Cancel loop cloning optimization for this loop.
//
// Arguments:
//      loopNum     the loop index.
//
// Return Values:
//      None.
//
void LoopCloneContext::CancelLoopOptInfo(unsigned loopNum)
{
    JITDUMP("Cancelling loop cloning for loop " FMT_LP "\n", loopNum);
    optInfo[loopNum] = nullptr;
    if (conditions[loopNum] != nullptr)
    {
        conditions[loopNum]->Reset();
        conditions[loopNum] = nullptr;
    }
}

//--------------------------------------------------------------------------------------------------
// EnsureLoopOptInfo - Retrieve the loop opt info candidate array, if it is not present, allocate
//      memory.
//
// Arguments:
//      loopNum     the loop index.
//
// Return Values:
//      The array of optimization candidates for the loop.
//
JitExpandArrayStack<LcOptInfo*>* LoopCloneContext::EnsureLoopOptInfo(unsigned loopNum)
{
    if (optInfo[loopNum] == nullptr)
    {
        optInfo[loopNum] = new (alloc) JitExpandArrayStack<LcOptInfo*>(alloc, 4);
    }
    return optInfo[loopNum];
}

//--------------------------------------------------------------------------------------------------
// EnsureLoopOptInfo - Retrieve the loop cloning conditions candidate array,
//      if it is not present, allocate memory.
//
// Arguments:
//      loopNum     the loop index.
//
// Return Values:
//      The array of cloning conditions for the loop.
//
JitExpandArrayStack<LC_Condition>* LoopCloneContext::EnsureConditions(unsigned loopNum)
{
    if (conditions[loopNum] == nullptr)
    {
        conditions[loopNum] = new (alloc) JitExpandArrayStack<LC_Condition>(alloc, 4);
    }
    return conditions[loopNum];
}

//--------------------------------------------------------------------------------------------------
// GetConditions - Get the cloning conditions array for the loop, no allocation.
//
// Arguments:
//      loopNum     the loop index.
//
// Return Values:
//      The array of cloning conditions for the loop.
//
JitExpandArrayStack<LC_Condition>* LoopCloneContext::GetConditions(unsigned loopNum)
{
    return conditions[loopNum];
}

//--------------------------------------------------------------------------------------------------
// EnsureArrayDerefs - Ensure an array of array dereferences is created if it doesn't exist.
//
// Arguments:
//      loopNum     the loop index.
//
// Return Values:
//      The array of array dereferences for the loop.
//
JitExpandArrayStack<LC_Array>* LoopCloneContext::EnsureArrayDerefs(unsigned loopNum)
{
    if (arrayDerefs[loopNum] == nullptr)
    {
        arrayDerefs[loopNum] = new (alloc) JitExpandArrayStack<LC_Array>(alloc, 4);
    }
    return arrayDerefs[loopNum];
}

//--------------------------------------------------------------------------------------------------
// EnsureObjDerefs - Ensure an array of object dereferences is created if it doesn't exist.
//
// Arguments:
//      loopNum     the loop index.
//
// Return Values:
//      The array of object dereferences for the loop.
//
JitExpandArrayStack<LC_Ident>* LoopCloneContext::EnsureObjDerefs(unsigned loopNum)
{
    if (objDerefs[loopNum] == nullptr)
    {
        objDerefs[loopNum] = new (alloc) JitExpandArrayStack<LC_Ident>(alloc, 4);
    }
    return objDerefs[loopNum];
}

//--------------------------------------------------------------------------------------------------
// HasBlockConditions - Check if there are block level conditions for the loop.
//
// Arguments:
//      loopNum     the loop index.
//
// Return Values:
//      Return true if there are any block level conditions.
//
bool LoopCloneContext::HasBlockConditions(unsigned loopNum)
{
    JitExpandArrayStack<JitExpandArrayStack<LC_Condition>*>* levelCond = blockConditions[loopNum];
    if (levelCond == nullptr)
    {
        return false;
    }

    // Walk through each block to check if any of them has conditions.
    for (unsigned i = 0; i < levelCond->Size(); ++i)
    {
        if ((*levelCond)[i]->Size() > 0)
        {
            return true;
        }
    }
    return false;
}

//--------------------------------------------------------------------------------------------------
// GetBlockConditions - Return block level conditions for the loop.
//
// Arguments:
//      loopNum     the loop index.
//
// Return Values:
//      Return block conditions.
//
JitExpandArrayStack<JitExpandArrayStack<LC_Condition>*>* LoopCloneContext::GetBlockConditions(unsigned loopNum)
{
    assert(HasBlockConditions(loopNum));
    return blockConditions[loopNum];
}

//--------------------------------------------------------------------------------------------------
// EnsureBlockConditions - Allocate block level conditions for the loop if not exists.
//
// Arguments:
//      loopNum     the loop index.
//      condBlocks  the number of block-level conditions for each loop, corresponding to the blocks
//                  created.
//
// Return Values:
//      Return block conditions.
//
JitExpandArrayStack<JitExpandArrayStack<LC_Condition>*>* LoopCloneContext::EnsureBlockConditions(unsigned loopNum,
                                                                                                 unsigned condBlocks)
{
    if (blockConditions[loopNum] == nullptr)
    {
        blockConditions[loopNum] = new (alloc) JitExpandArrayStack<JitExpandArrayStack<LC_Condition>*>(alloc);
    }

    JitExpandArrayStack<JitExpandArrayStack<LC_Condition>*>* levelCond = blockConditions[loopNum];
    // Iterate backwards to make sure the expand array stack reallocs just once here.
    unsigned prevSize = levelCond->Size();
    for (unsigned i = condBlocks; i > prevSize; i--)
    {
        levelCond->Set(i - 1, new (alloc) JitExpandArrayStack<LC_Condition>(alloc));
    }

    return levelCond;
}

#ifdef DEBUG
void LoopCloneContext::PrintBlockConditions(unsigned loopNum)
{
    printf("Block conditions:\n");

    JitExpandArrayStack<JitExpandArrayStack<LC_Condition>*>* blockConds = blockConditions[loopNum];
    if (blockConds == nullptr || blockConds->Size() == 0)
    {
        printf("No block conditions\n");
        return;
    }

    for (unsigned i = 0; i < blockConds->Size(); ++i)
    {
        PrintBlockLevelConditions(i, (*blockConds)[i]);
    }
}
void LoopCloneContext::PrintBlockLevelConditions(unsigned level, JitExpandArrayStack<LC_Condition>* levelCond)
{
    printf("%d = ", level);
    for (unsigned j = 0; j < levelCond->Size(); ++j)
    {
        if (j != 0)
        {
            printf(" && ");
        }
        printf("(");
        (*levelCond)[j].Print();
        printf(")");
    }
    printf("\n");
}
#endif

//--------------------------------------------------------------------------------------------------
// EvaluateConditions - Evaluate the loop cloning conditions statically, if they can be evaluated.
//
// Arguments:
//      loopNum     the loop index.
//      pAllTrue    OUT parameter. `*pAllTrue` is set to `true` if all the cloning conditions statically
//                  evaluate to true.
//      pAnyFalse   OUT parameter. `*pAnyFalse` is set to `true` if some cloning condition statically
//                  evaluate to false.
//      verbose     verbose logging required.
//
// Return Values:
//      None.
//
// Operation:
//      For example, a condition like "V02 >= V02" statically evaluates to true. Caller should detect such
//      conditions and remove them from the "conditions" array.
//
//      Similarly, conditions like "V02 > V02" will evaluate to "false". In this case caller has to abort
//      loop cloning optimization for the loop. Note that the assumption for conditions is that they will
//      all be "AND"ed, so statically we know we will never take the fast path.
//
//      Sometimes we simply can't say statically whether "V02 > V01.length" is true or false.
//      In that case, `*pAllTrue` will be false because this condition doesn't evaluate to "true" and
//      `*pAnyFalse` could be false if no other condition statically evaluates to "false".
//
//      If `*pAnyFalse` is true, we set that and return, and `*pAllTrue` is not accurate, since the loop cloning
//      needs to be aborted.
//
void LoopCloneContext::EvaluateConditions(unsigned loopNum, bool* pAllTrue, bool* pAnyFalse DEBUGARG(bool verbose))
{
    bool allTrue  = true;
    bool anyFalse = false;

    JitExpandArrayStack<LC_Condition>& conds = *conditions[loopNum];

    JITDUMP("Evaluating %d loop cloning conditions for loop " FMT_LP "\n", conds.Size(), loopNum);

    assert(conds.Size() > 0);
    for (unsigned i = 0; i < conds.Size(); ++i)
    {
#ifdef DEBUG
        if (verbose)
        {
            printf("Considering condition %d: (", i);
            conds[i].Print();
        }
#endif

        bool res = false;
        // Check if this condition evaluates to true or false.
        if (conds[i].Evaluates(&res))
        {
            JITDUMP(") evaluates to %s\n", dspBool(res));
            if (!res)
            {
                anyFalse = true;

                // Since this will force us to abort loop cloning, there is no need compute an accurate `allTrue`,
                // so we can break out of the loop now.
                break;
            }
        }
        else
        {
            JITDUMP("), could not be evaluated\n");
            allTrue = false;
        }
    }

    JITDUMP("Evaluation result allTrue = %s, anyFalse = %s\n", dspBool(allTrue), dspBool(anyFalse));
    *pAllTrue  = allTrue;
    *pAnyFalse = anyFalse;
}

//--------------------------------------------------------------------------------------------------
// OptimizeConditions - Evaluate the loop cloning conditions statically, if they can be evaluated
//      then optimize the "conditions" array accordingly.
//
// Arguments:
//      conds   The conditions array to optimize.
//
// Return Values:
//      None.
//
// Operation:
//      For example, a condition like "V02 >= V02" statically evaluates to true. Remove such conditions
//      from the "conditions" array.
//
//      Similarly, conditions like "V02 > V02" will evaluate to "false". In this case abort loop cloning
//      optimization for the loop.
//
//      Sometimes, two conditions will combine together to yield a single condition, then remove a
//      duplicate condition.
void LoopCloneContext::OptimizeConditions(JitExpandArrayStack<LC_Condition>& conds)
{
    for (unsigned i = 0; i < conds.Size(); ++i)
    {
        // Check if the conditions evaluate.
        bool result = false;
        if (conds[i].Evaluates(&result))
        {
            // If statically known to be true, then remove this condition.
            if (result)
            {
                conds.Remove(i);
                --i;
                continue;
            }
            else
            {
                // Some condition is statically false, then simply indicate
                // not to clone this loop.
                CancelLoopOptInfo(i);
                break;
            }
        }

        // Check for all other conditions[j], if it would combine with
        // conditions[i].
        for (unsigned j = i + 1; j < conds.Size(); ++j)
        {
            LC_Condition newCond;
            if (conds[i].Combines(conds[j], &newCond))
            {
                conds.Remove(j);
                conds[i] = newCond;
                i        = -1;
                break;
            }
        }
    }
#ifdef DEBUG
    // Make sure we didn't miss some combining.
    for (unsigned i = 0; i < conds.Size(); ++i)
    {
        for (unsigned j = 0; j < conds.Size(); ++j)
        {
            LC_Condition newCond;
            if ((i != j) && conds[i].Combines(conds[j], &newCond))
            {
                assert(!"Loop cloning conditions can still be optimized further.");
            }
        }
    }
#endif
}

//--------------------------------------------------------------------------------------------------
// OptimizeBlockConditions - Optimize block level conditions.
//
// Arguments:
//      loopNum     the loop index.
//
// Operation:
//       Calls OptimizeConditions helper on block level conditions.
//
// Return Values:
//      None.
//
void LoopCloneContext::OptimizeBlockConditions(unsigned loopNum DEBUGARG(bool verbose))
{
    if (!HasBlockConditions(loopNum))
    {
        return;
    }
    JitExpandArrayStack<JitExpandArrayStack<LC_Condition>*>* levelCond = blockConditions[loopNum];
    for (unsigned i = 0; i < levelCond->Size(); ++i)
    {
        OptimizeConditions(*((*levelCond)[i]));
    }
#ifdef DEBUG
    if (verbose)
    {
        printf("After optimizing block-level cloning conditions\n\t");
        PrintConditions(loopNum);
        printf("\n");
    }
#endif
}

//--------------------------------------------------------------------------------------------------
// OptimizeConditions - Optimize cloning conditions.
//
// Arguments:
//      loopNum     the loop index.
//      verbose     verbose logging required.
//
// Operation:
//       Calls OptimizeConditions helper on cloning conditions.
//
// Return Values:
//      None.
//
void LoopCloneContext::OptimizeConditions(unsigned loopNum DEBUGARG(bool verbose))
{
#ifdef DEBUG
    if (verbose)
    {
        printf("Before optimizing cloning conditions\n\t");
        PrintConditions(loopNum);
        printf("\n");
    }
#endif
    JitExpandArrayStack<LC_Condition>& conds = *conditions[loopNum];
    OptimizeConditions(conds);

#ifdef DEBUG
    if (verbose)
    {
        printf("After optimizing cloning conditions\n\t");
        PrintConditions(loopNum);
        printf("\n");
    }
#endif
}

#ifdef DEBUG
//--------------------------------------------------------------------------------------------------
// PrintConditions - Print loop cloning conditions necessary to clone the loop.
//
// Arguments:
//      loopNum     the loop index.
//
// Return Values:
//      None.
//
void LoopCloneContext::PrintConditions(unsigned loopNum)
{
    if (conditions[loopNum] == nullptr)
    {
        printf("NO conditions");
        return;
    }
    if (conditions[loopNum]->Size() == 0)
    {
        printf("Conditions were optimized away! Will always take cloned path.");
        return;
    }
    for (unsigned i = 0; i < conditions[loopNum]->Size(); ++i)
    {
        if (i != 0)
        {
            printf(" && ");
        }
        printf("(");
        (*conditions[loopNum])[i].Print();
        printf(")");
    }
}
#endif

//--------------------------------------------------------------------------------------------------
// CondToStmtInBlock: Convert an array of conditions to IR. Evaluate them into a JTRUE stmt and add it to
// a new block after `insertAfter`.
//
// Arguments:
//      comp        Compiler instance
//      conds       Array of conditions to evaluate into a JTRUE stmt
//      slowHead    Branch here on condition failure
//      insertAfter Insert the conditions in a block after this block
//
// Notes:
//      If any condition fails, branch to the `slowHead` block. There are two options here:
//      1. Generate all the conditions in a single block using bitwise `&` to merge them, e.g.:
//            jmpTrue(cond1 & cond2 ... == 0) => slowHead
//         In this form, we always execute all the conditions (there is no short-circuit evaluation).
//         Since we expect that in the usual case all the conditions will fail, and we'll execute the
//         loop fast path, the lack of short-circuit evaluation is not a problem. If the code is smaller
//         and faster, this would be preferable.
//      2. Generate each condition in a separate block, e.g.:
//            jmpTrue(!cond1) => slowHead
//            jmpTrue(!cond2) => slowHead
//            ...
//         If this code is smaller/faster, this can be preferable. Also, the flow graph is more normal,
//         and amenable to downstream flow optimizations.
//
//      Which option we choose is currently compile-time determined.
//
//      We assume that `insertAfter` is a fall-through block, and we add it to the predecessors list
//      of the first newly added block. `insertAfter` is also assumed to be in the same loop (we can
//      clone its loop number).
//
// Return Value:
//      Last block added
//
BasicBlock* LoopCloneContext::CondToStmtInBlock(Compiler*                          comp,
                                                JitExpandArrayStack<LC_Condition>& conds,
                                                BasicBlock*                        slowHead,
                                                BasicBlock*                        insertAfter)
{
    noway_assert(conds.Size() > 0);
    assert(slowHead != nullptr);
    assert(insertAfter->KindIs(BBJ_NONE, BBJ_COND));

    // Choose how to generate the conditions
    const bool generateOneConditionPerBlock = true;

    if (generateOneConditionPerBlock)
    {
        BasicBlock* newBlk = nullptr;

        for (unsigned i = 0; i < conds.Size(); ++i)
        {
            newBlk = comp->fgNewBBafter(BBJ_COND, insertAfter, /*extendRegion*/ true);
            newBlk->inheritWeight(insertAfter);
            newBlk->bbNatLoopNum = insertAfter->bbNatLoopNum;
            newBlk->bbJumpDest   = slowHead;

            JITDUMP("Adding " FMT_BB " -> " FMT_BB "\n", newBlk->bbNum, newBlk->bbJumpDest->bbNum);
            comp->fgAddRefPred(newBlk->bbJumpDest, newBlk);

            JITDUMP("Adding " FMT_BB " -> " FMT_BB "\n", insertAfter->bbNum, newBlk->bbNum);
            comp->fgAddRefPred(newBlk, insertAfter);

            JITDUMP("Adding conditions %u to " FMT_BB "\n", i, newBlk->bbNum);

            GenTree*   cond        = conds[i].ToGenTree(comp, newBlk, /* invert */ true);
            GenTree*   jmpTrueTree = comp->gtNewOperNode(GT_JTRUE, TYP_VOID, cond);
            Statement* stmt        = comp->fgNewStmtFromTree(jmpTrueTree);

            comp->fgInsertStmtAtEnd(newBlk, stmt);

            // Remorph.
            JITDUMP("Loop cloning condition tree before morphing:\n");
            DBEXEC(comp->verbose, comp->gtDispTree(jmpTrueTree));
            JITDUMP("\n");
            comp->fgMorphBlockStmt(newBlk, stmt DEBUGARG("Loop cloning condition"));

            insertAfter = newBlk;
        }

        return newBlk;
    }
    else
    {
        BasicBlock* newBlk = comp->fgNewBBafter(BBJ_COND, insertAfter, /*extendRegion*/ true);
        newBlk->inheritWeight(insertAfter);
        newBlk->bbNatLoopNum = insertAfter->bbNatLoopNum;
        newBlk->bbJumpDest   = slowHead;

        JITDUMP("Adding " FMT_BB " -> " FMT_BB "\n", newBlk->bbNum, newBlk->bbJumpDest->bbNum);
        comp->fgAddRefPred(newBlk->bbJumpDest, newBlk);

        JITDUMP("Adding " FMT_BB " -> " FMT_BB "\n", insertAfter->bbNum, newBlk->bbNum);
        comp->fgAddRefPred(newBlk, insertAfter);

        JITDUMP("Adding conditions to " FMT_BB "\n", newBlk->bbNum);

        // Get the first condition.
        GenTree* cond = conds[0].ToGenTree(comp, newBlk, /* invert */ false);
        for (unsigned i = 1; i < conds.Size(); ++i)
        {
            // Append all conditions using AND operator.
            cond = comp->gtNewOperNode(GT_AND, TYP_INT, cond, conds[i].ToGenTree(comp, newBlk, /* invert */ false));
        }

        // Add "cond == 0" node
        cond = comp->gtNewOperNode(GT_EQ, TYP_INT, cond, comp->gtNewIconNode(0));

        // Add jmpTrue "cond == 0"
        GenTree*   jmpTrueTree = comp->gtNewOperNode(GT_JTRUE, TYP_VOID, cond);
        Statement* stmt        = comp->fgNewStmtFromTree(jmpTrueTree);

        comp->fgInsertStmtAtEnd(newBlk, stmt);

        // Remorph.
        JITDUMP("Loop cloning condition tree before morphing:\n");
        DBEXEC(comp->verbose, comp->gtDispTree(jmpTrueTree));
        JITDUMP("\n");
        comp->fgMorphBlockStmt(newBlk, stmt DEBUGARG("Loop cloning condition"));

        return newBlk;
    }
}

//--------------------------------------------------------------------------------------------------
// Lcl - the current node's local variable.
//
// Arguments:
//      None.
//
// Operation:
//      If level is 0, then just return the array base. Else return the index variable on dim 'level'
//
// Return Values:
//      The local variable in the node's level.
//
unsigned LC_ArrayDeref::Lcl()
{
    unsigned lvl = level;
    if (lvl == 0)
    {
        return array.arrIndex->arrLcl;
    }
    lvl--;
    return array.arrIndex->indLcls[lvl];
}

//--------------------------------------------------------------------------------------------------
// HasChildren - Check if there are children to 'this' node.
//
// Arguments:
//      None.
//
// Return Values:
//      Return true if children are present.
//
bool LC_ArrayDeref::HasChildren()
{
    return children != nullptr && children->Size() > 0;
}

//--------------------------------------------------------------------------------------------------
// DeriveLevelConditions - Generate conditions for each level of the tree.
//
// Arguments:
//      conds       An array of conditions for each level i.e., (level x conditions). This array will
//                  contain the conditions for the tree at the end of the method.
//
// Operation:
//      level0 yields only (a != null) condition. All other levels yield two conditions:
//      (level < a[...].length && a[...][level] != null)
//
// Return Values:
//      None
//
void LC_ArrayDeref::DeriveLevelConditions(JitExpandArrayStack<JitExpandArrayStack<LC_Condition>*>* conds)
{
    if (level == 0)
    {
        // For level 0, just push (a != null).
        (*conds)[level]->Push(
            LC_Condition(GT_NE, LC_Expr(LC_Ident::CreateVar(Lcl())), LC_Expr(LC_Ident::CreateNull())));
    }
    else
    {
        // Adjust for level0 having just 1 condition and push conditions (i >= 0) && (i < a.len).
        // We fold the two compares into one using unsigned compare, since we know a.len is non-negative.
        //
        LC_Array arrLen = array;
        arrLen.oper     = LC_Array::ArrLen;
        arrLen.dim      = level - 1;
        (*conds)[level * 2 - 1]->Push(LC_Condition(GT_LT, LC_Expr(LC_Ident::CreateVar(Lcl())),
                                                   LC_Expr(LC_Ident::CreateArrAccess(arrLen)), /*unsigned*/ true));

        // Push condition (a[i] != null)
        LC_Array arrTmp = array;
        arrTmp.dim      = level;
        (*conds)[level * 2]->Push(
            LC_Condition(GT_NE, LC_Expr(LC_Ident::CreateArrAccess(arrTmp)), LC_Expr(LC_Ident::CreateNull())));
    }

    // Invoke on the children recursively.
    if (HasChildren())
    {
        for (unsigned i = 0; i < children->Size(); ++i)
        {
            (*children)[i]->DeriveLevelConditions(conds);
        }
    }
}

//--------------------------------------------------------------------------------------------------
// EnsureChildren - Create an array of child nodes if nullptr.
//
// Arguments:
//      alloc   CompAllocator instance
//
// Return Values:
//      None
//
void LC_ArrayDeref::EnsureChildren(CompAllocator alloc)
{
    if (children == nullptr)
    {
        children = new (alloc) JitExpandArrayStack<LC_ArrayDeref*>(alloc);
    }
}

//--------------------------------------------------------------------------------------------------
// Find - Find the node representing the local variable in child nodes of the 'this' node.
//
// Arguments:
//      lcl     the local to find in the children array
//
// Return Values:
//      The child node if found or nullptr.
//
LC_ArrayDeref* LC_ArrayDeref::Find(unsigned lcl)
{
    return Find(children, lcl);
}

//--------------------------------------------------------------------------------------------------
// Find - Find the node representing the local variable in a list of nodes.
//
// Arguments:
//      lcl          the local to find.
//      children     the list of nodes to find the node representing the lcl.
//
// Return Values:
//      The node if found or nullptr.
//

// static
LC_ArrayDeref* LC_ArrayDeref::Find(JitExpandArrayStack<LC_ArrayDeref*>* children, unsigned lcl)
{
    if (children == nullptr)
    {
        return nullptr;
    }
    for (unsigned i = 0; i < children->Size(); ++i)
    {
        if ((*children)[i]->Lcl() == lcl)
        {
            return (*children)[i];
        }
    }
    return nullptr;
}

//------------------------------------------------------------------------
// optDeriveLoopCloningConditions: Derive loop cloning conditions.
//
// Arguments:
//     loopNum     -  the current loop index for which conditions are derived.
//     context     -  data structure where all loop cloning info is kept.
//
// Return Value:
//     "false" if conditions cannot be obtained. "true" otherwise.
//     The cloning conditions are updated in the "conditions"[loopNum] field
//     of the "context" parameter.
//
// Operation:
//     Inspect the loop cloning optimization candidates and populate the conditions necessary
//     for each optimization candidate. Checks if the loop stride is "> 0" if the loop
//     condition is `<` or `<=`. If the initializer is "var" init then adds condition
//     "var >= 0", and if the loop is var limit then, "var >= 0" and "var <= a.len"
//     are added to "context". These conditions are checked in the pre-header block
//     and the cloning choice is made.
//
// Assumption:
//      Callers should assume AND operation is used i.e., if all conditions are
//      true, then take the fast path.
//
bool Compiler::optDeriveLoopCloningConditions(unsigned loopNum, LoopCloneContext* context)
{
    JITDUMP("------------------------------------------------------------\n");
    JITDUMP("Deriving cloning conditions for " FMT_LP "\n", loopNum);

    LoopDsc*                         loop     = &optLoopTable[loopNum];
    JitExpandArrayStack<LcOptInfo*>* optInfos = context->GetLoopOptInfo(loopNum);
    assert(optInfos->Size() > 0);

    // We only need to check for iteration behavior if we have array checks.
    //
    bool checkIterationBehavior = false;

    for (unsigned i = 0; i < optInfos->Size(); ++i)
    {
        LcOptInfo* const optInfo = optInfos->Get(i);
        switch (optInfo->GetOptType())
        {
            case LcOptInfo::LcJaggedArray:
            case LcOptInfo::LcMdArray:
                checkIterationBehavior = true;
                break;

            case LcOptInfo::LcTypeTest:
            {
                LcTypeTestOptInfo* ttInfo      = optInfo->AsLcTypeTestOptInfo();
                LC_Ident           objDeref    = LC_Ident::CreateIndirOfLocal(ttInfo->lclNum, 0);
                LC_Ident           methodTable = LC_Ident::CreateClassHandle(ttInfo->clsHnd);
                LC_Condition       cond(GT_EQ, LC_Expr(objDeref), LC_Expr(methodTable));
                context->EnsureObjDerefs(loopNum)->Push(objDeref);
                context->EnsureConditions(loopNum)->Push(cond);
                break;
            }

            case LcOptInfo::LcMethodAddrTest:
            {
                LcMethodAddrTestOptInfo* test = optInfo->AsLcMethodAddrTestOptInfo();
                LC_Ident                 objDeref =
                    LC_Ident::CreateIndirOfLocal(test->delegateLclNum, eeGetEEInfo()->offsetOfDelegateFirstTarget);
                LC_Ident methAddr;
                if (test->isSlot)
                {
                    methAddr = LC_Ident::CreateIndirMethodAddrSlot(test->methAddr DEBUG_ARG(test->targetMethHnd));
                }
                else
                {
                    methAddr = LC_Ident::CreateMethodAddr(test->methAddr DEBUG_ARG(test->targetMethHnd));
                }

                LC_Condition cond(GT_EQ, LC_Expr(objDeref), LC_Expr(methAddr));

                context->EnsureObjDerefs(loopNum)->Push(objDeref);
                context->EnsureConditions(loopNum)->Push(cond);
                break;
            }

            default:
                JITDUMP("Unknown opt\n");
                return false;
        }
    }

    if (!checkIterationBehavior)
    {
        // No array conditions here, so we're done
        //
        JITDUMP("Conditions: ");
        DBEXEC(verbose, context->PrintConditions(loopNum));
        JITDUMP("\n");
        return true;
    }

    // Note we see cases where the test oper is NE (array.Len) which we could handle
    // with some extra care.
    //
    if (!GenTree::StaticOperIs(loop->lpTestOper(), GT_LT, GT_LE, GT_GT, GT_GE))
    {
        // We can't reason about how this loop iterates
        return false;
    }

    const bool isIncreasingLoop = loop->lpIsIncreasingLoop();
    assert(isIncreasingLoop || loop->lpIsDecreasingLoop());

    // We already know that this is either increasing or decreasing loop and the
    // stride is (> 0) or (< 0). Here, just take the abs() value and check if it
    // is beyond the limit.
    int stride = abs(loop->lpIterConst());

    if (stride >= 58)
    {
        // Array.MaxLength can have maximum of 0X7FFFFFC7 elements, so make sure
        // the stride increment doesn't overflow or underflow the index. Hence,
        // the maximum stride limit is set to
        // (int.MaxValue - (Array.MaxLength - 1) + 1), which is
        // (0X7fffffff - 0x7fffffc7 + 2) = 0x3a or 58.
        return false;
    }

    LC_Ident ident;
    // Init conditions
    if (loop->lpFlags & LPFLG_CONST_INIT)
    {
        // Only allowing non-negative const init at this time.
        // This is because the variable initialized with this constant will be used as an array index,
        // and array indices must be non-negative.
        if (loop->lpConstInit < 0)
        {
            JITDUMP("> Init %d is invalid\n", loop->lpConstInit);
            return false;
        }

        if (!isIncreasingLoop)
        {
            // For decreasing loop, the init value needs to be checked against the array length
            ident = LC_Ident::CreateConst(static_cast<unsigned>(loop->lpConstInit));
        }
    }
    else
    {
        // iterVar >= 0
        const unsigned initLcl = loop->lpIterVar();
        if (!genActualTypeIsInt(lvaGetDesc(initLcl)))
        {
            JITDUMP("> Init var V%02u not compatible with TYP_INT\n", initLcl);
            return false;
        }

        LC_Condition geZero;
        if (isIncreasingLoop)
        {
            geZero = LC_Condition(GT_GE, LC_Expr(LC_Ident::CreateVar(initLcl)), LC_Expr(LC_Ident::CreateConst(0u)));
        }
        else
        {
            // For decreasing loop, the init value needs to be checked against the array length
            ident  = LC_Ident::CreateVar(initLcl);
            geZero = LC_Condition(GT_GE, LC_Expr(ident), LC_Expr(LC_Ident::CreateConst(0u)));
        }
        context->EnsureConditions(loopNum)->Push(geZero);
    }

    // Limit Conditions
    if (loop->lpFlags & LPFLG_CONST_LIMIT)
    {
        int limit = loop->lpConstLimit();
        if (limit < 0)
        {
            JITDUMP("> limit %d is invalid\n", limit);
            return false;
        }

        if (isIncreasingLoop)
        {
            // For increasing loop, thelimit value needs to be checked against the array length
            ident = LC_Ident::CreateConst(static_cast<unsigned>(limit));
        }
    }
    else if (loop->lpFlags & LPFLG_VAR_LIMIT)
    {
        const unsigned limitLcl = loop->lpVarLimit();
        if (!genActualTypeIsInt(lvaGetDesc(limitLcl)))
        {
            JITDUMP("> Limit var V%02u not compatible with TYP_INT\n", limitLcl);
            return false;
        }

        LC_Condition geZero;
        if (isIncreasingLoop)
        {
            // For increasing loop, thelimit value needs to be checked against the array length
            ident  = LC_Ident::CreateVar(limitLcl);
            geZero = LC_Condition(GT_GE, LC_Expr(ident), LC_Expr(LC_Ident::CreateConst(0u)));
        }
        else
        {
            geZero = LC_Condition(GT_GE, LC_Expr(LC_Ident::CreateVar(limitLcl)), LC_Expr(LC_Ident::CreateConst(0u)));
        }

        context->EnsureConditions(loopNum)->Push(geZero);
    }
    else if (loop->lpFlags & LPFLG_ARRLEN_LIMIT)
    {
        ArrIndex* index = new (getAllocator(CMK_LoopClone)) ArrIndex(getAllocator(CMK_LoopClone));
        if (!loop->lpArrLenLimit(this, index))
        {
            JITDUMP("> ArrLen not matching\n");
            return false;
        }
        ident = LC_Ident::CreateArrAccess(LC_Array(LC_Array::Jagged, index, LC_Array::ArrLen));

        // Ensure that this array must be dereference-able, before executing the actual condition.
        LC_Array array(LC_Array::Jagged, index, LC_Array::None);
        context->EnsureArrayDerefs(loopNum)->Push(array);
    }
    else
    {
        JITDUMP("> Undetected limit\n");
        return false;
    }

    // Increasing loops
    // GT_LT loop test: (start < end) ==> (end <= arrLen)
    // GT_LE loop test: (start <= end) ==> (end < arrLen)
    //
    // Decreasing loops
    // GT_GT loop test: (end > start) ==> (end <= arrLen)
    // GT_GE loop test: (end >= start) ==> (end < arrLen)
    genTreeOps opLimitCondition;
    switch (loop->lpTestOper())
    {
        case GT_LT:
        case GT_GT:
            opLimitCondition = GT_LE;
            break;
        case GT_LE:
        case GT_GE:
            opLimitCondition = GT_LT;
            break;
        default:
            unreached();
    }

    for (unsigned i = 0; i < optInfos->Size(); ++i)
    {
        LcOptInfo* optInfo = optInfos->Get(i);
        switch (optInfo->GetOptType())
        {
            case LcOptInfo::LcJaggedArray:
            {
                LcJaggedArrayOptInfo* arrIndexInfo = optInfo->AsLcJaggedArrayOptInfo();
                LC_Array     arrLen(LC_Array::Jagged, &arrIndexInfo->arrIndex, arrIndexInfo->dim, LC_Array::ArrLen);
                LC_Ident     arrLenIdent = LC_Ident::CreateArrAccess(arrLen);
                LC_Condition cond(opLimitCondition, LC_Expr(ident), LC_Expr(arrLenIdent));
                context->EnsureConditions(loopNum)->Push(cond);

                // Ensure that this array must be dereference-able, before executing the actual condition.
                LC_Array array(LC_Array::Jagged, &arrIndexInfo->arrIndex, arrIndexInfo->dim, LC_Array::None);
                context->EnsureArrayDerefs(loopNum)->Push(array);
            }
            break;
            case LcOptInfo::LcMdArray:
            {
                LcMdArrayOptInfo* mdArrInfo = optInfo->AsLcMdArrayOptInfo();
                LC_Array arrLen(LC_Array(LC_Array::MdArray, mdArrInfo->GetArrIndexForDim(getAllocator(CMK_LoopClone)),
                                         mdArrInfo->dim, LC_Array::None));
                LC_Ident     arrLenIdent = LC_Ident::CreateArrAccess(arrLen);
                LC_Condition cond(opLimitCondition, LC_Expr(ident), LC_Expr(arrLenIdent));
                context->EnsureConditions(loopNum)->Push(cond);

                // TODO: ensure array is dereference-able?
            }
            break;
            case LcOptInfo::LcTypeTest:
                // handled above
                break;

            default:
                JITDUMP("Unknown opt\n");
                return false;
        }
    }

    JITDUMP("Conditions: ");
    DBEXEC(verbose, context->PrintConditions(loopNum));
    JITDUMP("\n");

    return true;
}

//------------------------------------------------------------------------------------
// optComputeDerefConditions: Derive loop cloning conditions for dereferencing arrays.
//
// Arguments:
//     loopNum     -  the current loop index for which conditions are derived.
//     context     -  data structure where all loop cloning info is kept.
//
// Return Value:
//     "false" if conditions cannot be obtained. "true" otherwise.
//     The deref conditions are updated in the "derefConditions"[loopNum] field
//     of the "context" parameter.
//
// Definition of Deref Conditions:
//     To be able to check for the loop cloning condition that (limitVar <= a.len)
//     we should first be able to dereference "a". i.e., "a" is non-null.
//
//     Example:
//
//     for (i in 0..n)
//       for (j in 0..n)
//         for (k in 0..n)      // Inner most loop is being cloned. Cloning needs to check if
//                              // (n <= a[i][j].len) and other safer conditions to take the fast path
//           a[i][j][k] = 0
//
//     Now, we want to deref a[i][j] to invoke length operator on it to perform the cloning fast path check.
//     This involves deref of (a), (a[i]), (a[i][j]), therefore, the following should first
//     be true to do the deref.
//
//     (a != null) && (i < a.len) && (a[i] != null) && (j < a[i].len) && (a[i][j] != null) --> condition set (1)
//
//     Note the short circuiting AND. Implication: these conditions should be performed in separate
//     blocks each of which will branch to slow path if the condition evaluates to false.
//
//     Now, imagine a situation where, in the inner loop above, in addition to "a[i][j][k] = 0" we
//     also have:
//        a[x][y][k] = 20
//     where x and y are parameters, then our conditions will have to include:
//        (x < a.len) &&
//        (y < a[x].len)
//     in addition to the above conditions (1) to get rid of bounds check on index 'k'
//
//     But these conditions can be checked together with conditions
//     (i < a.len) without a need for a separate block. In summary, the conditions will be:
//
//     (a != null) &&
//     ((i < a.len) & (x < a.len)) &&      <-- Note the bitwise AND here.
//     (a[i] != null & a[x] != null) &&    <-- Note the bitwise AND here.
//     (j < a[i].len & y < a[x].len) &&    <-- Note the bitwise AND here.
//     (a[i][j] != null & a[x][y] != null) <-- Note the bitwise AND here.
//
//     This naturally yields a tree style pattern, where the nodes of the tree are
//     the array and indices respectively.
//
//     Example:
//         a => {
//             i => {
//                 j => {
//                     k => {}
//                 }
//             },
//             x => {
//                 y => {
//                     k => {}
//                 }
//             }
//         }
//
//         Notice that the variables in the same levels can have their conditions combined in the
//         same block with a bitwise AND. Whereas, the conditions in consecutive levels will be
//         combined with a short-circuiting AND (i.e., different basic blocks).
//
//  Operation:
//      Construct a tree of array indices and the array which will generate the optimal
//      conditions for loop cloning.
//
//      a[i][j][k], b[i] and a[i][y][k] are the occurrences in the loop. Then, the tree should be:
//
//      a => {
//          i => {
//              j => {
//                  k => {}
//              },
//              y => {
//                  k => {}
//              },
//          }
//      },
//      b => {
//          i => {}
//      }
//
//      In this method, we will construct such a tree by descending depth first into the array
//      index operation and forming a tree structure as we encounter the array or the index variables.
//
//      This tree structure will then be used to generate conditions like below:
//      (a != null) & (b != null) &&       // from the first level of the tree.
//
//      (i < a.len) & (i < b.len) &&       // from the second level of the tree. Levels can be combined.
//      (a[i] != null) & (b[i] != null) && // from the second level of the tree.
//
//      (j < a[i].len) & (y < a[i].len) &&       // from the third level.
//      (a[i][j] != null) & (a[i][y] != null) && // from the third level.
//
//      and so on.
//
bool Compiler::optComputeDerefConditions(unsigned loopNum, LoopCloneContext* context)
{
    // Get the dereference-able arrays and objects.
    JitExpandArrayStack<LC_Array>* const arrayDeref = context->EnsureArrayDerefs(loopNum);
    JitExpandArrayStack<LC_Ident>* const objDeref   = context->EnsureObjDerefs(loopNum);

    // We currently expect to have at least one of these.
    //
    assert((arrayDeref->Size() != 0) || (objDeref->Size() != 0));

    // Generate the array dereference checks.
    //
    // For each array in the dereference list, construct a tree,
    // where the arrayDerefNodes are array and index variables and an edge 'u-v'
    // exists if a node 'v' indexes node 'u' directly as in u[v] or an edge
    // 'u-v-w' transitively if u[v][w] occurs.
    //
    JitExpandArrayStack<LC_ArrayDeref*> arrayDerefNodes(getAllocator(CMK_LoopClone));
    int                                 maxRank = -1;

    for (unsigned i = 0; i < arrayDeref->Size(); ++i)
    {
        LC_Array& array = (*arrayDeref)[i];

        // First populate the array base variable.
        LC_ArrayDeref* node = LC_ArrayDeref::Find(&arrayDerefNodes, array.arrIndex->arrLcl);
        if (node == nullptr)
        {
            node = new (getAllocator(CMK_LoopClone)) LC_ArrayDeref(array, 0 /*level*/);
            arrayDerefNodes.Push(node);
        }

        // For each dimension (level) for the array, populate the tree with the variable
        // from that dimension.
        unsigned rank = (unsigned)array.GetDimRank();
        for (unsigned i = 0; i < rank; ++i)
        {
            node->EnsureChildren(getAllocator(CMK_LoopClone));
            LC_ArrayDeref* tmp = node->Find(array.arrIndex->indLcls[i]);
            if (tmp == nullptr)
            {
                tmp = new (getAllocator(CMK_LoopClone)) LC_ArrayDeref(array, node->level + 1);
                node->children->Push(tmp);
            }

            // Descend one level down.
            node = tmp;
        }

        // Keep the maxRank of all array dereferences.
        maxRank = max((int)rank, maxRank);
    }

#ifdef DEBUG
    if (verbose)
    {
        if (arrayDerefNodes.Size() > 0)
        {
            printf("Array deref condition tree:\n");
            for (unsigned i = 0; i < arrayDerefNodes.Size(); ++i)
            {
                arrayDerefNodes[i]->Print();
                printf("\n");
            }
        }
        else
        {
            printf("No array deref conditions\n");
        }
    }
#endif

    if (arrayDeref->Size() > 0)
    {
        // If we have array derefs we should have set maxRank.
        //
        assert(maxRank != -1);

        // First level will always yield the null-check, since it is made of the array base variables.
        // All other levels (dimensions) will yield two conditions ex: ((unsigned) i < a.length && a[i] != null)
        // So add 1 after rank * 2.
        const unsigned condBlocks = (unsigned)maxRank * 2 + 1;

        // Heuristic to not create too many blocks. Defining as 3 allows, effectively, loop cloning on
        // doubly-nested loops.
        // REVIEW: make this based on a COMPlus configuration, at least for debug?
        const unsigned maxAllowedCondBlocks = 3;
        if (condBlocks > maxAllowedCondBlocks)
        {
            JITDUMP("> Too many condition blocks (%u > %u)\n", condBlocks, maxAllowedCondBlocks);
            return false;
        }

        // Derive conditions into an 'array of level x array of conditions' i.e., levelCond[levels][conds]
        JitExpandArrayStack<JitExpandArrayStack<LC_Condition>*>* levelCond =
            context->EnsureBlockConditions(loopNum, condBlocks);
        for (unsigned i = 0; i < arrayDerefNodes.Size(); ++i)
        {
            arrayDerefNodes[i]->DeriveLevelConditions(levelCond);
        }
    }

    if (objDeref->Size() > 0)
    {
        JitExpandArrayStack<JitExpandArrayStack<LC_Condition>*>* levelCond = context->EnsureBlockConditions(loopNum, 1);

        for (unsigned i = 0; i < objDeref->Size(); ++i)
        {
            // ObjDeref array has indir(lcl), we want lcl.
            //
            LC_Ident& mtIndirIdent = (*objDeref)[i];
            LC_Ident  ident        = LC_Ident::CreateVar(mtIndirIdent.LclNum());
            (*levelCond)[0]->Push(LC_Condition(GT_NE, LC_Expr(ident), LC_Expr(LC_Ident::CreateNull())));
        }
    }

    DBEXEC(verbose, context->PrintBlockConditions(loopNum));

    return true;
}

#ifdef DEBUG
//----------------------------------------------------------------------------
// optDebugLogLoopCloning:  Insert a call to jithelper that prints a message.
//
// Arguments:
//      block        - the block in which the helper call needs to be inserted.
//      insertBefore - the stmt before which the helper call will be inserted.
//
void Compiler::optDebugLogLoopCloning(BasicBlock* block, Statement* insertBefore)
{
    if (JitConfig.JitDebugLogLoopCloning() == 0)
    {
        return;
    }
    GenTree*   logCall = gtNewHelperCallNode(CORINFO_HELP_DEBUG_LOG_LOOP_CLONING, TYP_VOID);
    Statement* stmt    = fgNewStmtFromTree(logCall);
    fgInsertStmtBefore(block, insertBefore, stmt);
    fgMorphBlockStmt(block, stmt DEBUGARG("Debug log loop cloning"));
}
#endif // DEBUG

//------------------------------------------------------------------------
// optPerformStaticOptimizations: Perform the optimizations for the optimization
//      candidates gathered during the cloning phase.
//
// Arguments:
//     loopNum     -  the current loop index for which the optimizations are performed.
//     context     -  data structure where all loop cloning info is kept.
//     dynamicPath -  If true, the optimization is performed in the fast path among the
//                    cloned loops. If false, it means this is the only path (i.e.,
//                    there is no slow path.)
//
// Operation:
//      Perform the optimizations on the fast path i.e., the path in which the
//      optimization candidates were collected at the time of identifying them.
//      The candidates store all the information necessary (the tree/stmt/block
//      they are from) to perform the optimization.
//
// Assumption:
//      The unoptimized path is either already cloned when this method is called or
//      there is no unoptimized path (got eliminated statically.) So this method
//      performs the optimizations assuming that the path in which the candidates
//      were collected is the fast path in which the optimizations will be performed.
//
void Compiler::optPerformStaticOptimizations(unsigned loopNum, LoopCloneContext* context DEBUGARG(bool dynamicPath))
{
    JitExpandArrayStack<LcOptInfo*>* optInfos = context->GetLoopOptInfo(loopNum);
    assert(optInfos != nullptr);
    for (unsigned i = 0; i < optInfos->Size(); ++i)
    {
        LcOptInfo* optInfo = optInfos->Get(i);
        switch (optInfo->GetOptType())
        {
            case LcOptInfo::LcJaggedArray:
            {
                LcJaggedArrayOptInfo* arrIndexInfo = optInfo->AsLcJaggedArrayOptInfo();
                compCurBB                          = arrIndexInfo->arrIndex.useBlock;

                // Remove all bounds checks for this array up to (and including) `arrIndexInfo->dim`. So, if that is 1,
                // Remove rank 0 and 1 bounds checks.

                for (unsigned dim = 0; dim <= arrIndexInfo->dim; dim++)
                {
                    GenTree* bndsChkNode = arrIndexInfo->arrIndex.bndsChks[dim];

#ifdef DEBUG
                    if (verbose)
                    {
                        printf("Remove bounds check ");
                        printTreeID(bndsChkNode->gtGetOp1());
                        printf(" for " FMT_STMT ", dim% d, ", arrIndexInfo->stmt->GetID(), dim);
                        arrIndexInfo->arrIndex.Print();
                        printf(", bounds check nodes: ");
                        arrIndexInfo->arrIndex.PrintBoundsCheckNodes();
                        printf("\n");
                    }
#endif // DEBUG

                    if (bndsChkNode->gtGetOp1()->OperIs(GT_BOUNDS_CHECK))
                    {
                        // This COMMA node will only represent a bounds check if we've haven't already removed this
                        // bounds check in some other nesting cloned loop. For example, consider:
                        //   for (i = 0; i < x; i++)
                        //      for (j = 0; j < y; j++)
                        //         a[i][j] = i + j;
                        // If the outer loop is cloned first, it will remove the a[i] bounds check from the optimized
                        // path. Later, when the inner loop is cloned, we want to remove the a[i][j] bounds check. If
                        // we clone the inner loop, we know that the a[i] bounds check isn't required because we'll add
                        // it to the loop cloning conditions. On the other hand, we can clone a loop where we get rid of
                        // the nested bounds check but nobody has gotten rid of the outer bounds check. As before, we
                        // know the outer bounds check is not needed because it's been added to the cloning conditions,
                        // so we can get rid of the bounds check here.
                        //
                        optRemoveCommaBasedRangeCheck(bndsChkNode, arrIndexInfo->stmt);
                    }
                    else
                    {
                        JITDUMP("  Bounds check already removed\n");

                        // If the bounds check node isn't there, it better have been converted to a GT_NOP.
                        assert(bndsChkNode->gtGetOp1()->OperIs(GT_NOP));
                    }
                }

                DBEXEC(dynamicPath, optDebugLogLoopCloning(arrIndexInfo->arrIndex.useBlock, arrIndexInfo->stmt));
            }
            break;
            case LcOptInfo::LcMdArray:
                // TODO-CQ: CLONE: Implement.
                break;
            case LcOptInfo::LcTypeTest:
            case LcOptInfo::LcMethodAddrTest:
            {
                Statement*    stmt;
                GenTreeIndir* indir;

                if (optInfo->GetOptType() == LcOptInfo::LcTypeTest)
                {
                    LcTypeTestOptInfo* typeTestInfo = optInfo->AsLcTypeTestOptInfo();
                    stmt                            = typeTestInfo->stmt;
                    indir                           = typeTestInfo->methodTableIndir;
                }
                else
                {
                    LcMethodAddrTestOptInfo* methodTestInfo = optInfo->AsLcMethodAddrTestOptInfo();
                    stmt                                    = methodTestInfo->stmt;
                    indir                                   = methodTestInfo->delegateAddressIndir;
                }

                JITDUMP("Updating flags on GDV guard inside hot loop. Before:\n");
                DISPSTMT(stmt);

                indir->gtFlags |= GTF_ORDER_SIDEEFF | GTF_IND_NONFAULTING;
                indir->gtFlags &= ~GTF_EXCEPT;
                assert(fgNodeThreading == NodeThreading::None);
                gtUpdateStmtSideEffects(stmt);

                JITDUMP("After:\n");
                DISPSTMT(stmt);

                break;
            }

            default:
                break;
        }
    }
}

//----------------------------------------------------------------------------
// optIsLoopClonable: Determine whether this loop can be cloned.
//
// Arguments:
//      loopInd     loop index which needs to be checked if it can be cloned.
//
// Return Value:
//      Returns true if the loop can be cloned. If it returns false,
//      it prints a message to the JIT dump describing why the loop can't be cloned.
//
// Notes: if `true` is returned, then `fgReturnCount` is increased by the number of
// return blocks in the loop that will be cloned. (REVIEW: this 'predicate' function
// doesn't seem like the right place to do this change.)
//
bool Compiler::optIsLoopClonable(unsigned loopInd)
{
    const LoopDsc& loop            = optLoopTable[loopInd];
    const bool     requireIterable = !doesMethodHaveGuardedDevirtualization();

    if (requireIterable && !(loop.lpFlags & LPFLG_ITER))
    {
        JITDUMP("Loop cloning: rejecting loop " FMT_LP ". No LPFLG_ITER flag.\n", loopInd);
        return false;
    }

    if (loop.lpIsRemoved())
    {
        JITDUMP("Loop cloning: rejecting removed loop " FMT_LP ".\n", loopInd);
        return false;
    }

    // Make sure the loop doesn't have any embedded exception handling.
    // Walk the loop blocks from lexically first to lexically last (all blocks in this region must be
    // part of the loop), looking for a `try` begin block. Note that a loop must entirely contain any
    // EH region, or be itself entirely contained within an EH region. Thus, looking just for a `try`
    // begin is sufficient; there is no need to look for other EH constructs, such as a `catch` begin.
    //
    // TODO: this limitation could be removed if we do the work to insert new EH regions in the exception table,
    // for the cloned loop (and its embedded EH regions).
    //
    // Also, count the number of return blocks within the loop for future use.
    unsigned loopRetCount = 0;
    for (BasicBlock* const blk : loop.LoopBlocks())
    {
        if (blk->bbJumpKind == BBJ_RETURN)
        {
            loopRetCount++;
        }
        if (bbIsTryBeg(blk))
        {
            JITDUMP("Loop cloning: rejecting loop " FMT_LP ". It has a `try` begin.\n", loopInd);
            return false;
        }
    }

    // Is the entry block a handler or filter start?  If so, then if we cloned, we could create a jump
    // into the middle of a handler (to go to the cloned copy.)  Reject.
    if (bbIsHandlerBeg(loop.lpEntry))
    {
        JITDUMP("Loop cloning: rejecting loop " FMT_LP ". Entry block is a handler start.\n", loopInd);
        return false;
    }

    // If the head and entry are in different EH regions, reject.
    if (!BasicBlock::sameEHRegion(loop.lpHead, loop.lpEntry))
    {
        JITDUMP("Loop cloning: rejecting loop " FMT_LP ". Head and entry blocks are in different EH regions.\n",
                loopInd);
        return false;
    }

    // Is the first block after the last block of the loop a handler or filter start?
    // Usually, we create a dummy block after the original loop, to skip over the loop clone
    // and go to where the original loop did.  That raises problems when we don't actually go to
    // that block; this is one of those cases.  This could be fixed fairly easily; for example,
    // we could add a dummy nop block after the (cloned) loop bottom, in the same handler scope as the
    // loop.  This is just a corner to cut to get this working faster.
    BasicBlock* bbAfterLoop = loop.lpBottom->bbNext;
    if (bbAfterLoop != nullptr && bbIsHandlerBeg(bbAfterLoop))
    {
        JITDUMP("Loop cloning: rejecting loop " FMT_LP ". Next block after bottom is a handler start.\n", loopInd);
        return false;
    }

    // Reject cloning if this is a mid-entry loop and the entry has non-loop predecessors other than its head.
    // This loop may be part of a larger looping construct that we didn't recognize.
    //
    // We should really fix this in optCanonicalizeLoop.
    //
    if (!loop.lpIsTopEntry())
    {
        for (BasicBlock* const entryPred : loop.lpEntry->PredBlocks())
        {
            if ((entryPred != loop.lpHead) && !loop.lpContains(entryPred))
            {
                JITDUMP("Loop cloning: rejecting loop " FMT_LP
                        ". Is not top entry, and entry has multiple non-loop preds.\n",
                        loopInd);
                return false;
            }
        }
    }

    // We've previously made a decision whether to have separate return epilogs, or branch to one.
    // There's a GCInfo limitation in the x86 case, so that there can be no more than SET_EPILOGCNT_MAX separate
    // epilogs.  Other architectures have a limit of 4 here for "historical reasons", but this should be revisited
    // (or return blocks should not be considered part of the loop, rendering this issue moot).
    unsigned epilogLimit = 4;
#ifdef JIT32_GCENCODER
    epilogLimit = SET_EPILOGCNT_MAX;
#endif // JIT32_GCENCODER
    if (fgReturnCount + loopRetCount > epilogLimit)
    {
        JITDUMP("Loop cloning: rejecting loop " FMT_LP ". It has %d returns;"
                " if added to previously existing %d returns, it would exceed the limit of %d.\n",
                loopInd, loopRetCount, fgReturnCount, epilogLimit);
        return false;
    }

    if (requireIterable)
    {
        const unsigned ivLclNum = loop.lpIterVar();
        if (lvaVarAddrExposed(ivLclNum))
        {
            JITDUMP("Loop cloning: rejecting loop " FMT_LP ". Rejected V%02u as iter var because is address-exposed.\n",
                    loopInd, ivLclNum);
            return false;
        }
    }

    BasicBlock* top    = loop.lpTop;
    BasicBlock* bottom = loop.lpBottom;

    if (bottom->bbJumpKind != BBJ_COND)
    {
        JITDUMP("Loop cloning: rejecting loop " FMT_LP ". Couldn't find termination test.\n", loopInd);
        return false;
    }

    if (bottom->bbJumpDest != top)
    {
        JITDUMP("Loop cloning: rejecting loop " FMT_LP ". Branch at loop 'bottom' not looping to 'top'.\n", loopInd);
        return false;
    }

    if (requireIterable)
    {
        if ((loop.lpFlags & LPFLG_CONST_LIMIT) == 0 && (loop.lpFlags & LPFLG_VAR_LIMIT) == 0 &&
            (loop.lpFlags & LPFLG_ARRLEN_LIMIT) == 0)
        {
            JITDUMP("Loop cloning: rejecting loop " FMT_LP
                    ". Loop limit is neither constant, variable or array length.\n",
                    loopInd);
            return false;
        }

        // TODO-CQ: Handle other loops like:
        // - The ones whose limit operator is "==" or "!="
        // - The incrementing operator is multiple and divide
        // - The ones that are inverted are not handled here for cases like "i *= 2" because
        //   they are converted to "i + i".
        if (!(loop.lpIsIncreasingLoop() || loop.lpIsDecreasingLoop()))
        {
            JITDUMP("Loop cloning: rejecting loop " FMT_LP
                    ". Loop test (%s) doesn't agree with the direction (%s) of the loop.\n",
                    loopInd, GenTree::OpName(loop.lpTestOper()), GenTree::OpName(loop.lpIterOper()));
            return false;
        }

        if (!loop.lpTestTree->OperIsCompare() || !(loop.lpTestTree->gtFlags & GTF_RELOP_ZTT))
        {
            JITDUMP("Loop cloning: rejecting loop " FMT_LP
                    ". Loop inversion NOT present, loop test [%06u] may not protect "
                    "entry from head.\n",
                    loopInd, loop.lpTestTree->gtTreeID);
            return false;
        }

#ifdef DEBUG
        const unsigned ivLclNum = loop.lpIterVar();
        GenTree* const op1      = loop.lpIterator();
        assert((op1->gtOper == GT_LCL_VAR) && (op1->AsLclVarCommon()->GetLclNum() == ivLclNum));
#endif
    }

    // Otherwise, we're going to add those return blocks.
    fgReturnCount += loopRetCount;

    return true;
}

//--------------------------------------------------------------------------------------------------
// optInsertLoopChoiceConditions: Insert the loop conditions for a loop after the loop head.
//
// Arguments:
//      context     loop cloning context variable
//      loopNum     the loop index
//      slowHead    the slow path loop head, where the condition failures branch
//      insertAfter insert the conditions after this block
//
// Return Value:
//      The last condition block added.
//
// Operation:
//      Create the following structure.
//
//      h (fall through)
//      !cond0        -?> slowHead
//      !cond1        -?> slowHead
//      ...
//      !condn        -?> slowHead
//      h2/entry (fast)
//      ...
//      slowHead      -?> e2 (slowHead) branch or fall-through to e2
//
BasicBlock* Compiler::optInsertLoopChoiceConditions(LoopCloneContext* context,
                                                    unsigned          loopNum,
                                                    BasicBlock*       slowHead,
                                                    BasicBlock*       insertAfter)
{
    JITDUMP("Inserting loop " FMT_LP " loop choice conditions\n", loopNum);
    assert(context->HasBlockConditions(loopNum));
    assert(slowHead != nullptr);
    assert(insertAfter->bbJumpKind == BBJ_NONE);

    if (context->HasBlockConditions(loopNum))
    {
        JitExpandArrayStack<JitExpandArrayStack<LC_Condition>*>* levelCond = context->GetBlockConditions(loopNum);
        for (unsigned i = 0; i < levelCond->Size(); ++i)
        {
            JITDUMP("Adding loop " FMT_LP " level %u block conditions\n    ", loopNum, i);
            DBEXEC(verbose, context->PrintBlockLevelConditions(i, (*levelCond)[i]));
            insertAfter = context->CondToStmtInBlock(this, *((*levelCond)[i]), slowHead, insertAfter);
        }
    }

    // Finally insert cloning conditions after all deref conditions have been inserted.
    JITDUMP("Adding loop " FMT_LP " cloning conditions\n    ", loopNum);
    DBEXEC(verbose, context->PrintConditions(loopNum));
    JITDUMP("\n");
    insertAfter = context->CondToStmtInBlock(this, *(context->GetConditions(loopNum)), slowHead, insertAfter);

    return insertAfter;
}

//------------------------------------------------------------------------
// optCloneLoop: Perform the mechanical cloning of the specified loop
//
// Arguments:
//    loopInd - loop index of loop to clone
//    context - data structure where all loop cloning info is kept.
//
void Compiler::optCloneLoop(unsigned loopInd, LoopCloneContext* context)
{
    assert(loopInd < optLoopCount);

    LoopDsc& loop = optLoopTable[loopInd];

    JITDUMP("\nCloning loop " FMT_LP ": [head: " FMT_BB ", top: " FMT_BB ", entry: " FMT_BB ", bottom: " FMT_BB
            ", child: " FMT_LP "].\n",
            loopInd, loop.lpHead->bbNum, loop.lpTop->bbNum, loop.lpEntry->bbNum, loop.lpBottom->bbNum, loop.lpChild);

    // Determine the depth of the loop, so we can properly weight blocks added (outside the cloned loop blocks).
    unsigned depth         = optLoopDepth(loopInd);
    weight_t ambientWeight = 1;
    for (unsigned j = 0; j < depth; j++)
    {
        weight_t lastWeight = ambientWeight;
        ambientWeight *= BB_LOOP_WEIGHT_SCALE;
        assert(ambientWeight > lastWeight);
    }

    // If we're in a non-natural loop, the ambient weight might be higher than we computed above.
    // Be safe by taking the max with the head block's weight.
    ambientWeight = max(ambientWeight, loop.lpHead->bbWeight);

    // We assume that the fast path will run 99% of the time, and thus should get 99% of the block weights.
    // The slow path will, correspondingly, get only 1% of the block weights. It could be argued that we should
    // mark the slow path as "run rarely", since it really shouldn't execute (given the currently optimized loop
    // conditions) except under exceptional circumstances.
    const weight_t fastPathWeightScaleFactor = 0.99;
    const weight_t slowPathWeightScaleFactor = 1.0 - fastPathWeightScaleFactor;

    // This is the containing loop, if any -- to label any blocks we create that are outside
    // the loop being cloned.
    unsigned char ambientLoop = loop.lpParent;

    // We're going to transform this loop:
    //
    // H --> E    (if T == E, H falls through to T/E)
    // T
    // E
    // B ?-> T
    // X
    //
    // to this pair of loops:
    //
    // H ?-> H3   (all loop failure conditions branch to new slow path loop head)
    // H2 --> E   (if T == E, H2 will fall through to T/E)
    // T
    // E
    // B  ?-> T
    // X2 --> X   (only need this if B -> is conditional, not BBJ_ALWAYS)
    // H3 --> E2  (aka slowHead. Or, H3 falls through to T2 == E2)
    // T2
    // E2
    // B2 ?-> T2
    // X

    BasicBlock* h = loop.lpHead;
    assert((h->bbFlags & BBF_LOOP_PREHEADER) != 0);

    // Make a new pre-header block 'h2' for the loop. 'h' will fall through to 'h2'.
    JITDUMP("Create new header block for loop\n");

    BasicBlock* h2 = fgNewBBafter(BBJ_NONE, h, /*extendRegion*/ true);
    JITDUMP("Adding " FMT_BB " after " FMT_BB "\n", h2->bbNum, h->bbNum);
    h2->bbWeight     = h2->isRunRarely() ? BB_ZERO_WEIGHT : ambientWeight;
    h2->bbNatLoopNum = ambientLoop;
    h2->bbFlags |= BBF_LOOP_PREHEADER;

    if (h->bbJumpKind != BBJ_NONE)
    {
        assert(h->bbJumpKind == BBJ_ALWAYS);
        assert(h->bbJumpDest == loop.lpEntry);
        h2->bbJumpKind = BBJ_ALWAYS;
        h2->bbJumpDest = loop.lpEntry;
    }

    fgReplacePred(loop.lpEntry, h, h2);
    JITDUMP("Replace " FMT_BB " -> " FMT_BB " with " FMT_BB " -> " FMT_BB "\n", h->bbNum, loop.lpEntry->bbNum,
            h2->bbNum, loop.lpEntry->bbNum);

    // 'h' is no longer the loop head; 'h2' is!
    h->bbFlags &= ~BBF_LOOP_PREHEADER;
    optUpdateLoopHead(loopInd, h, h2);

    // Make 'h' fall through to 'h2' (if it didn't already).
    // Don't add the h->h2 edge because we're going to insert the cloning conditions between 'h' and 'h2', and
    // optInsertLoopChoiceConditions() will add the edge.
    h->bbJumpKind = BBJ_NONE;
    h->bbJumpDest = nullptr;

    // Make X2 after B, if necessary.  (Not necessary if B is a BBJ_ALWAYS.)
    // "newPred" will be the predecessor of the blocks of the cloned loop.
    BasicBlock* b       = loop.lpBottom;
    BasicBlock* newPred = b;
    if (b->bbJumpKind != BBJ_ALWAYS)
    {
        assert(b->bbJumpKind == BBJ_COND);

        BasicBlock* x = b->bbNext;
        if (x != nullptr)
        {
            JITDUMP("Create branch around cloned loop\n");
            BasicBlock* x2 = fgNewBBafter(BBJ_ALWAYS, b, /*extendRegion*/ true);
            JITDUMP("Adding " FMT_BB " after " FMT_BB "\n", x2->bbNum, b->bbNum);
            x2->bbWeight = x2->isRunRarely() ? BB_ZERO_WEIGHT : ambientWeight;

            // This is in the scope of a surrounding loop, if one exists -- the parent of the loop we're cloning.
            x2->bbNatLoopNum = ambientLoop;

            x2->bbJumpDest = x;
            BlockSetOps::Assign(this, x2->bbReach, h->bbReach);

            fgAddRefPred(x2, b); // Add b->x2 pred edge
            JITDUMP("Adding " FMT_BB " -> " FMT_BB "\n", b->bbNum, x2->bbNum);
            fgReplacePred(x, b, x2); // The pred of x is now x2, not the fall-through of COND b.
            JITDUMP("Replace " FMT_BB " -> " FMT_BB " with " FMT_BB " -> " FMT_BB "\n", b->bbNum, x->bbNum, x2->bbNum,
                    x->bbNum);

            newPred = x2;
        }
    }

    // Create a new loop head for the slow loop immediately before the slow loop itself. All failed
    // conditions will branch to the slow head. The slow head will either fall through or unconditionally
    // branch to the slow path entry. The slowHead will be a loop pre-header, however we don't put the slow loop
    // in the loop table, so it isn't marked as a pre-header (if we re-built the loop table after cloning, slowHead
    // would end up in the loop table as a pre-header without creating any new pre-header block). This puts the
    // slow loop in the canonical loop form.
    JITDUMP("Create unique head block for slow path loop\n");
    BasicBlock* slowHead = fgNewBBafter(BBJ_NONE, newPred, /*extendRegion*/ true);
    JITDUMP("Adding " FMT_BB " after " FMT_BB "\n", slowHead->bbNum, newPred->bbNum);
    slowHead->bbWeight = newPred->isRunRarely() ? BB_ZERO_WEIGHT : ambientWeight;
    slowHead->scaleBBWeight(slowPathWeightScaleFactor);
    slowHead->bbNatLoopNum = ambientLoop;
    newPred                = slowHead;

    // Now we'll clone the blocks of the loop body. These cloned blocks will be the slow path.

    BlockToBlockMap* blockMap = new (getAllocator(CMK_LoopClone)) BlockToBlockMap(getAllocator(CMK_LoopClone));
    for (BasicBlock* const blk : loop.LoopBlocks())
    {
        BasicBlock* newBlk = fgNewBBafter(blk->bbJumpKind, newPred, /*extendRegion*/ true);
        JITDUMP("Adding " FMT_BB " (copy of " FMT_BB ") after " FMT_BB "\n", newBlk->bbNum, blk->bbNum, newPred->bbNum);

        // Call CloneBlockState to make a copy of the block's statements (and attributes), and assert that it
        // has a return value indicating success, because optCanOptimizeByLoopCloningVisitor has already
        // checked them to guarantee they are clonable.
        bool cloneOk = BasicBlock::CloneBlockState(this, newBlk, blk);
        noway_assert(cloneOk);

        // We're going to create the preds below, which will set the bbRefs properly,
        // so clear out the cloned bbRefs field.
        newBlk->bbRefs = 0;

        newBlk->scaleBBWeight(slowPathWeightScaleFactor);
        blk->scaleBBWeight(fastPathWeightScaleFactor);

// TODO: scale the pred edges of `blk`?

#if FEATURE_LOOP_ALIGN
        // If the original loop is aligned, do not align the cloned loop because cloned loop will be executed in
        // rare scenario. Additionally, having to align cloned loop will force us to disable some VEX prefix encoding
        // and adding compensation for over-estimated instructions.
        if (newBlk->isLoopAlign())
        {
            newBlk->bbFlags &= ~BBF_LOOP_ALIGN;
            JITDUMP("Removing LOOP_ALIGN flag from cloned loop in " FMT_BB "\n", newBlk->bbNum);
        }
#endif

        // If the loop we're cloning contains nested loops, we need to clear the pre-header bit on
        // any nested loop pre-header blocks, since they will no longer be loop pre-headers. (This is because
        // we don't add the slow loop or its child loops to the loop table. It would be simplest to
        // just re-build the loop table if we want to enable loop optimization of the slow path loops.)
        if ((newBlk->bbFlags & BBF_LOOP_PREHEADER) != 0)
        {
            JITDUMP("Removing BBF_LOOP_PREHEADER flag from nested cloned loop block " FMT_BB "\n", newBlk->bbNum);
            newBlk->bbFlags &= ~BBF_LOOP_PREHEADER;
        }

        // TODO-Cleanup: The above clones the bbNatLoopNum, which is incorrect.  Eventually, we should probably insert
        // the cloned loop in the loop table.  For now, however, we'll just make these blocks be part of the surrounding
        // loop, if one exists -- the parent of the loop we're cloning.
        newBlk->bbNatLoopNum = loop.lpParent;

        newPred = newBlk;
        blockMap->Set(blk, newBlk);
    }

    // Perform the static optimizations on the fast path.
    optPerformStaticOptimizations(loopInd, context DEBUGARG(true));

    // Now go through the new blocks, remapping their jump targets within the loop
    // and updating the preds lists.
    for (BasicBlock* const blk : loop.LoopBlocks())
    {
        BasicBlock* newblk = nullptr;
        bool        b      = blockMap->Lookup(blk, &newblk);
        assert(b && newblk != nullptr);

        assert(blk->bbJumpKind == newblk->bbJumpKind);

        // First copy the jump destination(s) from "blk".
        optCopyBlkDest(blk, newblk);

        // Now redirect the new block according to "blockMap".
        optRedirectBlock(newblk, blockMap);

        // Add predecessor edges for the new successors, as well as the fall-through paths.
        switch (newblk->bbJumpKind)
        {
            case BBJ_NONE:
                fgAddRefPred(newblk->bbNext, newblk);
                break;

            case BBJ_ALWAYS:
            case BBJ_CALLFINALLY:
                fgAddRefPred(newblk->bbJumpDest, newblk);
                break;

            case BBJ_COND:
                fgAddRefPred(newblk->bbNext, newblk);
                fgAddRefPred(newblk->bbJumpDest, newblk);
                break;

            case BBJ_SWITCH:
                for (BasicBlock* const switchDest : newblk->SwitchTargets())
                {
                    fgAddRefPred(switchDest, newblk);
                }
                break;

            default:
                break;
        }
    }

#ifdef DEBUG
    // Display the preds for the new blocks, after all the new blocks have been redirected.
    JITDUMP("Preds after loop copy:\n");
    for (BasicBlock* const blk : loop.LoopBlocks())
    {
        BasicBlock* newblk = nullptr;
        bool        b      = blockMap->Lookup(blk, &newblk);
        assert(b && newblk != nullptr);
        JITDUMP(FMT_BB ":", newblk->bbNum);
        for (BasicBlock* const predBlock : newblk->PredBlocks())
        {
            JITDUMP(" " FMT_BB, predBlock->bbNum);
        }
        JITDUMP("\n");
    }
#endif // DEBUG

    // Insert the loop choice conditions. We will create the following structure:
    //
    //      h (fall through)
    //      !cond0        -?> slowHead
    //      !cond1        -?> slowHead
    //      ...
    //      !condn        -?> slowHead
    //      h2            -?> e (fast loop pre-header branch or fall-through to e)
    //      ...
    //      slowHead      -?> e2 (slow loop pre-header branch or fall-through to e2)
    //
    // We should always have block conditions.

    assert(context->HasBlockConditions(loopInd));
    assert(h->bbJumpKind == BBJ_NONE);
    assert(h->bbNext == h2);

    // If any condition is false, go to slowHead (which branches or falls through to e2).
    BasicBlock* e2      = nullptr;
    bool        foundIt = blockMap->Lookup(loop.lpEntry, &e2);
    assert(foundIt && e2 != nullptr);

    if (slowHead->bbNext != e2)
    {
        // We can't just fall through to the slow path entry, so make it an unconditional branch.
        assert(slowHead->bbJumpKind == BBJ_NONE); // This is how we created it above.
        slowHead->bbJumpKind = BBJ_ALWAYS;
        slowHead->bbJumpDest = e2;
    }

    fgAddRefPred(e2, slowHead);
    JITDUMP("Adding " FMT_BB " -> " FMT_BB "\n", slowHead->bbNum, e2->bbNum);

    BasicBlock* condLast = optInsertLoopChoiceConditions(context, loopInd, slowHead, h);

    // Add the fall-through path pred (either to T/E for fall-through from conditions to fast path,
    // or H2 if branch to E of fast path).
    assert(condLast->bbJumpKind == BBJ_COND);
    JITDUMP("Adding " FMT_BB " -> " FMT_BB "\n", condLast->bbNum, condLast->bbNext->bbNum);
    fgAddRefPred(condLast->bbNext, condLast);

    // Don't unroll loops that we've cloned -- the unroller expects any loop it should unroll to
    // initialize the loop counter immediately before entering the loop, but we've left a shared
    // initialization of the loop counter up above the test that determines which version of the
    // loop to take.
    loop.lpFlags |= LPFLG_DONT_UNROLL;
}

//-------------------------------------------------------------------------
//  optIsStackLocalInvariant: Is stack local invariant in loop.
//
//  Arguments:
//      loopNum      The loop in which the variable is tested for invariance.
//      lclNum       The local that is tested for invariance in the loop.
//
//  Return Value:
//      Returns true if the variable is loop invariant in loopNum.
//
bool Compiler::optIsStackLocalInvariant(unsigned loopNum, unsigned lclNum)
{
    if (lvaVarAddrExposed(lclNum))
    {
        return false;
    }
    if (optIsVarAssgLoop(loopNum, lclNum))
    {
        return false;
    }
    return true;
}

//---------------------------------------------------------------------------------------------------------------
//  optExtractArrIndex: Try to extract the array index from "tree".
//
//  Arguments:
//      tree             the tree to be checked if it is the array [] operation.
//      result           the extracted GT_INDEX_ADDR information is updated in result.
//      lhsNum           for the root level (function is recursive) callers should pass BAD_VAR_NUM.
//      topLevelIsFinal  OUT: set to `true` if see a non-TYP_REF element type array.
//
//  Return Value:
//      Returns true if array index can be extracted, else, return false. See assumption about
//      what will be extracted. The "result" variable's rank parameter is advanced for every
//      dimension of [] encountered.
//
//  Operation:
//      Given a "tree" extract the GT_INDEX_ADDR node in "result" as ArrIndex. In morph
//      we have converted a GT_INDEX_ADDR tree into a scaled index base offset expression.
//      However, we don't actually bother to parse the morphed tree. All we care about is
//      the bounds check node: it contains the array base and element index. The other side
//      of the COMMA node can vary between array of primitive type and array of struct. There's
//      no need to parse that, as the array bounds check contains the only thing we care about.
//      In particular, we are trying to find bounds checks to remove, so only looking at the bounds
//      check makes sense. We could verify that the bounds check is against the same array base/index
//      but it isn't necessary.
//
//  Assumption:
//      The method extracts only if the array base and indices are GT_LCL_VAR.
//
//  TODO-CQ: CLONE: After morph make sure this method extracts values before morph.
//
//  Example tree to pattern match:
//
// *  COMMA     int
// +--*  BOUNDS_CHECK_Rng void
// |  +--*  LCL_VAR   int    V02 loc1
// |  \--*  ARR_LENGTH int
// |     \--*  LCL_VAR   ref    V00 arg0
// \--*  IND       int
//    \--*  ADD       byref
//       +--*  LCL_VAR   ref    V00 arg0
//       \--*  ADD       long
//          +--*  LSH       long
//          |  +--*  CAST      long <- int
//          |  |  \--*  LCL_VAR   int    V02 loc1
//          |  \--*  CNS_INT   long   2
//          \--*  CNS_INT   long   16 Fseq[#FirstElem]
//
// Note that byte arrays don't require the LSH to scale the index, so look like this:
//
// *  COMMA     ubyte
// +--*  BOUNDS_CHECK_Rng void
// |  +--*  LCL_VAR   int    V03 loc2
// |  \--*  ARR_LENGTH int
// |     \--*  LCL_VAR   ref    V00 arg0
// \--*  IND       ubyte
//    \--*  ADD       byref
//       +--*  LCL_VAR   ref    V00 arg0
//       \--*  ADD       long
//          +--*  CAST      long <- int
//          |  \--*  LCL_VAR   int    V03 loc2
//          \--*  CNS_INT   long   16 Fseq[#FirstElem]
//
// The COMMA op2 expression is the array index expression (or SIMD/Span expression). If we've got
// a "LCL_VAR int" index and "ARR_LENGTH(LCL_VAR ref)", that's good enough for us: we'll assume
// op2 is an array index expression. We don't need to match it just to ensure the index var is
// used as an index expression, or array base var is used as the array base. This saves us from parsing
// all the forms that morph can create, especially for arrays of structs.
//
bool Compiler::optExtractArrIndex(GenTree* tree, ArrIndex* result, unsigned lhsNum, bool* topLevelIsFinal)
{
    if (tree->gtOper != GT_COMMA)
    {
        return false;
    }
    GenTree* before = tree->gtGetOp1();
    if (!before->OperIs(GT_BOUNDS_CHECK))
    {
        return false;
    }
    GenTreeBoundsChk* arrBndsChk = before->AsBoundsChk();
    if (arrBndsChk->GetIndex()->gtOper != GT_LCL_VAR)
    {
        return false;
    }

    // For span we may see the array length is a local var or local field or constant.
    // We won't try and extract those.
    if (arrBndsChk->GetArrayLength()->OperIs(GT_LCL_VAR, GT_LCL_FLD, GT_CNS_INT))
    {
        return false;
    }
    if (arrBndsChk->GetArrayLength()->gtGetOp1()->gtOper != GT_LCL_VAR)
    {
        return false;
    }
    unsigned arrLcl = arrBndsChk->GetArrayLength()->gtGetOp1()->AsLclVarCommon()->GetLclNum();
    if (lhsNum != BAD_VAR_NUM && arrLcl != lhsNum)
    {
        return false;
    }

    unsigned indLcl = arrBndsChk->GetIndex()->AsLclVarCommon()->GetLclNum();

    if (lhsNum == BAD_VAR_NUM)
    {
        result->arrLcl = arrLcl;
    }
    result->indLcls.Push(indLcl);
    result->bndsChks.Push(tree);
    result->useBlock = compCurBB;
    result->rank++;

    // If the array element type (saved from the GT_INDEX_ADDR node during morphing) is anything but
    // TYP_REF, then it must the final level of jagged array.
    assert(arrBndsChk->gtInxType != TYP_VOID);
    *topLevelIsFinal = (arrBndsChk->gtInxType != TYP_REF);

    return true;
}

//---------------------------------------------------------------------------------------------------------------
//  optReconstructArrIndexHelp: Helper function for optReconstructArrIndex. See that function for more details.
//
//  Arguments:
//      tree             the tree to be checked if it is an array [][][] operation.
//      result           OUT: the extracted GT_INDEX_ADDR information.
//      lhsNum           var number of array object we're looking for.
//      topLevelIsFinal  OUT: set to `true` if we reached a non-TYP_REF element type array.
//
//  Return Value:
//      Returns true if array index can be extracted, else, return false. "rank" field in
//      "result" contains the array access depth. The "indLcls" field contains the indices.
//
bool Compiler::optReconstructArrIndexHelp(GenTree* tree, ArrIndex* result, unsigned lhsNum, bool* topLevelIsFinal)
{
    // If we can extract "tree" (which is a top level comma) return.
    if (optExtractArrIndex(tree, result, lhsNum, topLevelIsFinal))
    {
        return true;
    }
    // We have a comma (check if array base expr is computed in "before"), descend further.
    else if (tree->OperGet() == GT_COMMA)
    {
        GenTree* before = tree->gtGetOp1();

        // "before" should evaluate an array base for the "after" indexing.
        if (!before->OperIs(GT_STORE_LCL_VAR) ||
            !optReconstructArrIndexHelp(before->AsLclVar()->Data(), result, lhsNum, topLevelIsFinal))
        {
            return false;
        }

        // If rhs represents an array of elements other than arrays (e.g., an array of structs),
        // then we can't go any farther.
        if (*topLevelIsFinal)
        {
            return false;
        }

        unsigned lclNum = before->AsLclVar()->GetLclNum();
        GenTree* after  = tree->gtGetOp2();
        // Pass the "lclNum", so we can verify if indeed it is used as the array base.
        return optExtractArrIndex(after, result, lclNum, topLevelIsFinal);
    }
    return false;
}

//---------------------------------------------------------------------------------------------------------------
//  optReconstructArrIndex: Reconstruct array index from a post-morph tree.
//
//  Arguments:
//      tree        the tree to be checked if it is an array [][][] operation.
//      result      OUT: the extracted GT_INDEX_ADDR information.
//
//  Return Value:
//      Returns true if array index can be extracted, else, return false. "rank" field in
//      "result" contains the array access depth. The "indLcls" field contains the indices.
//
//  Operation:
//      Recursively look for a list of array indices. For example, if the tree is
//          V03 = (V05 = V00[V01]), V05[V02]
//      that corresponds to access of V00[V01][V02]. The return value would then be:
//      ArrIndex result { arrLcl: V00, indLcls: [V01, V02], rank: 2 }
//
//      Note that the array expression is implied by the array bounds check under the COMMA, and the array bounds
//      checks is what is parsed from the morphed tree; the array addressing expression is not parsed.
//      However, the array bounds checks are not quite sufficient because of the way "morph" alters the trees.
//      Specifically, we normally see a COMMA node with a LHS of the morphed array INDEX_ADDR expression and RHS
//      of the bounds check. E.g., for int[][], a[i][j] we have a pre-morph tree:
//
// \--*  IND       int
//    \--*  INDEX_ADDR byref int[]
//       +--*  IND       ref
//       |  \--*  INDEX_ADDR byref ref[]
//       |     +--*  LCL_VAR   ref    V00 arg0
//       |     \--*  LCL_VAR   int    V01 arg1
//       \--*  LCL_VAR   int    V02 arg2
//
//      and post-morph tree:
//
// \--*  COMMA     int
//    +--*  STORE_LCL_VAR   ref    V04 tmp1
//    |  \--*  COMMA     ref
//    |     +--*  BOUNDS_CHECK_Rng void
//    |     |  +--*  LCL_VAR   int    V01 arg1
//    |     |  \--*  ARR_LENGTH int
//    |     |     \--*  LCL_VAR   ref    V00 arg0
//    |     \--*  IND       ref
//    |        \--*  ARR_ADDR  byref ref[]
//    |           \--*  ADD       byref
//    |              +--*  LCL_VAR   ref    V00 arg0
//    |              \--*  ADD       long
//    |                 +--*  LSH       long
//    |                 |  +--*  CAST      long <- uint
//    |                 |  |  \--*  LCL_VAR   int    V01 arg1
//    |                 |  \--*  CNS_INT   long   3
//    |                 \--*  CNS_INT   long   16
//    \--*  COMMA     int
//       +--*  BOUNDS_CHECK_Rng void
//       |  +--*  LCL_VAR   int    V02 arg2
//       |  \--*  ARR_LENGTH int
//       |     \--*  LCL_VAR   ref    V04 tmp1
//       \--*  IND       int
//          \--*  ARR_ADDR  byref int[]
//             \--*  ADD       byref
//                +--*  LCL_VAR   ref    V04 tmp1
//                \--*  ADD       long
//                   +--*  LSH       long
//                   |  +--*  CAST      long <- uint
//                   |  |  \--*  LCL_VAR   int    V02 arg2
//                   |  \--*  CNS_INT   long   2
//                   \--*  CNS_INT   long   16
//
//      However, for an array of structs that contains an array field, e.g. ValueTuple<int[], int>[], expression
//      a[i].Item1[j],
//
// \--*  IND       int
//    \--*  INDEX_ADDR byref int[]
//       +--*  FIELD     ref    Item1
//       |  \--*  INDEX_ADDR byref System.ValueTuple`2[System.Int32[],System.Int32][]
//       |     +--*  LCL_VAR   ref    V00 arg0
//       |     \--*  LCL_VAR   int    V01 arg1
//       \--*  LCL_VAR   int    V02 arg2
//
//      Morph "hoists" the bounds check above the struct field access:
//
// \--*  COMMA     int
//    +--*  STORE_LCL_VAR   ref    V04 tmp1
//    |  \--*  COMMA     ref
//    |     +--*  BOUNDS_CHECK_Rng void
//    |     |  +--*  LCL_VAR   int    V01 arg1
//    |     |  \--*  ARR_LENGTH int
//    |     |     \--*  LCL_VAR   ref    V00 arg0
//    |     \--*  IND       ref
//    |        \--*  ARR_ADDR  byref System.ValueTuple`2[System.Int32[],System.Int32][] Zero Fseq[Item1]
//    |           \--*  ADD       byref
//    |              +--*  LCL_VAR   ref    V00 arg0
//    |              \--*  ADD       long
//    |                 +--*  LSH       long
//    |                 |  +--*  CAST      long <- uint
//    |                 |  |  \--*  LCL_VAR   int    V01 arg1
//    |                 |  \--*  CNS_INT   long   4
//    |                 \--*  CNS_INT   long   16
//    \--*  COMMA     int
//       +--*  BOUNDS_CHECK_Rng void
//       |  +--*  LCL_VAR   int    V02 arg2
//       |  \--*  ARR_LENGTH int
//       |     \--*  LCL_VAR   ref    V04 tmp1
//       \--*  IND       int
//          \--*  ARR_ADDR  byref int[]
//             \--*  ADD       byref
//                +--*  LCL_VAR   ref    V04 tmp1
//                \--*  ADD       long
//                   +--*  LSH       long
//                   |  +--*  CAST      long <- uint
//                   |  |  \--*  LCL_VAR   int    V02 arg2
//                   |  \--*  CNS_INT   long   2
//                   \--*  CNS_INT   long   16
//
//      This should not be parsed as a jagged array (e.g., a[i][j]). To ensure that it is not, the type of the
//      GT_INDEX_ADDR node is stashed in the GT_BOUNDS_CHECK node during morph. If we see a bounds check node
//      where the GT_INDEX_ADDR was not TYP_REF, then it must be the outermost jagged array level. E.g., if it is
//      TYP_STRUCT, then we have an array of structs, and any further bounds checks must be of one of its fields.
//
//      It would be much better if we didn't need to parse these trees at all, and did all this work pre-morph.
//
//  Assumption:
//      The method extracts only if the array base and indices are GT_LCL_VAR.
//
bool Compiler::optReconstructArrIndex(GenTree* tree, ArrIndex* result)
{
    bool topLevelIsFinal = false;
    return optReconstructArrIndexHelp(tree, result, BAD_VAR_NUM, &topLevelIsFinal);
}

//----------------------------------------------------------------------------------------------
//  optCanOptimizeByLoopCloning: Check if the tree can be optimized by loop cloning and if so,
//      identify as potential candidate and update the loop context.
//
//  Arguments:
//      tree         The tree encountered during the tree walk.
//      info         Supplies information about the current block or stmt in which the tree is.
//                   Also supplies the "context" pointer for updating with loop cloning
//                   candidates. Also supplies loopNum.
//
//  Operation:
//      If array index can be reconstructed, check if the iteration var of the loop matches the
//      array index var in some dimension. Also ensure other index vars before the identified
//      dimension are loop invariant.
//
//      If the loop has invariant type tests, check if they will succeed often enough that
//      they should inspire cloning (on their own, or in conjunction with array bounds checks).
//
//  Return Value:
//      Skip sub trees if the optimization candidate is identified or else continue walking
//
Compiler::fgWalkResult Compiler::optCanOptimizeByLoopCloning(GenTree* tree, LoopCloneVisitorInfo* info)
{
    ArrIndex arrIndex(getAllocator(CMK_LoopClone));

    // Check if array index can be optimized.
    //
    if (info->cloneForArrayBounds && optReconstructArrIndex(tree, &arrIndex))
    {
        assert(tree->gtOper == GT_COMMA);

#ifdef DEBUG
        if (verbose)
        {
            printf("Found ArrIndex at " FMT_BB " " FMT_STMT " tree ", arrIndex.useBlock->bbNum, info->stmt->GetID());
            printTreeID(tree);
            printf(" which is equivalent to: ");
            arrIndex.Print();
            printf(", bounds check nodes: ");
            arrIndex.PrintBoundsCheckNodes();
            printf("\n");
        }
#endif

        // Check that the array object local variable is invariant within the loop body.
        if (!optIsStackLocalInvariant(info->loopNum, arrIndex.arrLcl))
        {
            JITDUMP("V%02d is not loop invariant\n", arrIndex.arrLcl);
            return WALK_SKIP_SUBTREES;
        }

        // Walk the dimensions and see if iterVar of the loop is used as index.
        for (unsigned dim = 0; dim < arrIndex.rank; ++dim)
        {
            // Is index variable also used as the loop iter var?
            if (arrIndex.indLcls[dim] == optLoopTable[info->loopNum].lpIterVar())
            {
                // Check the previous indices are all loop invariant.
                for (unsigned dim2 = 0; dim2 < dim; ++dim2)
                {
                    if (optIsVarAssgLoop(info->loopNum, arrIndex.indLcls[dim2]))
                    {
                        JITDUMP("V%02d is assigned in loop\n", arrIndex.indLcls[dim2]);
                        return WALK_SKIP_SUBTREES;
                    }
                }
#ifdef DEBUG
                if (verbose)
                {
                    printf("Loop " FMT_LP " can be cloned for ArrIndex ", info->loopNum);
                    arrIndex.Print();
                    printf(" on dim %d\n", dim);
                }
#endif
                // Update the loop context.
                info->context->EnsureLoopOptInfo(info->loopNum)
                    ->Push(new (this, CMK_LoopOpt) LcJaggedArrayOptInfo(arrIndex, dim, info->stmt));
            }
            else
            {
                JITDUMP("Induction V%02d is not used as index on dim %d\n", optLoopTable[info->loopNum].lpIterVar(),
                        dim);
            }
        }
        return WALK_SKIP_SUBTREES;
    }

    if (info->cloneForGDVTests && tree->OperIs(GT_JTRUE))
    {
        JITDUMP("...GDV considering [%06u]\n", dspTreeID(tree));
        assert(info->stmt->GetRootNode() == tree);
        // Look for invariant type/method address tests.
        //
        GenTree* const relop = tree->AsOp()->gtGetOp1();

        // Must be an equality comparison of some kind.
        //
        if (!relop->OperIs(GT_EQ, GT_NE))
        {
            return WALK_CONTINUE;
        }

        GenTree* relopOp1 = relop->AsOp()->gtGetOp1();
        GenTree* relopOp2 = relop->AsOp()->gtGetOp2();

        // One side or the other must be an indir and the other must be loop
        // invariant. Currently, we'll just look for a constant or indir of a
        // constant. Start out by normalizing it to the right.
        //
        if (optIsHandleOrIndirOfHandle(relopOp1, GTF_ICON_CLASS_HDL) ||
            optIsHandleOrIndirOfHandle(relopOp1, GTF_ICON_FTN_ADDR))
        {
            std::swap(relopOp1, relopOp2);
        }

        if (!relopOp1->OperIs(GT_IND) || !relopOp1->TypeIs(TYP_I_IMPL, TYP_REF, TYP_BYREF))
        {
            return WALK_CONTINUE;
        }

        GenTreeIndir* indir     = relopOp1->AsIndir();
        GenTree*      indirAddr = indir->Addr();

        if (relopOp2->IsIconHandle(GTF_ICON_CLASS_HDL))
        {
            // The indir addr must be loop invariant TYP_REF local
            //

            if (!indirAddr->TypeIs(TYP_REF))
            {
                return WALK_CONTINUE;
            }

            if (!indirAddr->OperIs(GT_LCL_VAR))
            {
                return WALK_CONTINUE;
            }

            GenTreeLclVarCommon* const indirAddrLcl = indirAddr->AsLclVarCommon();
            const unsigned             lclNum       = indirAddrLcl->GetLclNum();

            JITDUMP("... right form for type test with local V%02u\n", lclNum);

            if (!optIsStackLocalInvariant(info->loopNum, lclNum))
            {
                JITDUMP("... but not invariant\n");
                return WALK_CONTINUE;
            }

            // Looks like we found an invariant type test.
            //
            JITDUMP("Loop " FMT_LP " has invariant type test [%06u] on V%02u\n", info->loopNum, dspTreeID(tree),
                    lclNum);

            if (optCheckLoopCloningGDVTestProfitable(relop->AsOp(), info))
            {
                // Update the loop context.
                //
                assert(relopOp2->IsIconHandle(GTF_ICON_CLASS_HDL));
                CORINFO_CLASS_HANDLE clsHnd = (CORINFO_CLASS_HANDLE)relopOp2->AsIntConCommon()->IconValue();

                assert(compCurBB->lastStmt() == info->stmt);
                info->context->EnsureLoopOptInfo(info->loopNum)
                    ->Push(new (this, CMK_LoopOpt) LcTypeTestOptInfo(info->stmt, indir, lclNum, clsHnd));
            }
        }
        else if (optIsHandleOrIndirOfHandle(relopOp2, GTF_ICON_FTN_ADDR))
        {
            //  ▌  JTRUE     void
            //  └──▌  NE        int
            //     ├──▌  CNS_INT(h) long   0x7ffdb1fa4a08 ftn
            //     └──▌  IND       long
            //        └──▌  ADD       byref  <- Matching this tree
            //           ├──▌  LCL_VAR   ref    V00 arg0
            //           └──▌  CNS_INT   long   24

            // We expect indirections of the 'target' fields. Currently we
            // support only the simple cases (one target address, i.e. no
            // shuffle thunk/unboxing stubs).

            ssize_t offset = 0;
            if (indirAddr->OperIs(GT_ADD))
            {
                if (!indirAddr->gtGetOp2()->IsCnsIntOrI() || !indirAddr->gtGetOp2()->TypeIs(TYP_I_IMPL) ||
                    indirAddr->gtGetOp2()->IsIconHandle())
                {
                    return WALK_CONTINUE;
                }

                offset    = indirAddr->gtGetOp2()->AsIntConCommon()->IconValue();
                indirAddr = indirAddr->gtGetOp1();
            }

            if (!indirAddr->TypeIs(TYP_REF))
            {
                return WALK_CONTINUE;
            }

            if (!indirAddr->OperIs(GT_LCL_VAR))
            {
                return WALK_CONTINUE;
            }

            if (offset != static_cast<ssize_t>(eeGetEEInfo()->offsetOfDelegateFirstTarget))
            {
                return WALK_CONTINUE;
            }

            unsigned lclNum = indirAddr->AsLclVarCommon()->GetLclNum();

            JITDUMP("... right form for method address test with local V%02u\n", lclNum);

            LclVarDsc* dsc = lvaGetDesc(lclNum);
            if (dsc->lvClassHnd == NO_CLASS_HANDLE)
            {
                JITDUMP("... but no class handle available for local\n");
                return WALK_CONTINUE;
            }

            unsigned attribs = this->info.compCompHnd->getClassAttribs(dsc->lvClassHnd);
            if ((attribs & CORINFO_FLG_DELEGATE) == 0)
            {
                JITDUMP("... but not a delegate instance\n");
                return WALK_CONTINUE;
            }

            if (!optIsStackLocalInvariant(info->loopNum, lclNum))
            {
                JITDUMP("... but not invariant\n");
                return WALK_CONTINUE;
            }

            JITDUMP("Loop " FMT_LP " has invariant method address test [%06u] on V%02u\n", info->loopNum,
                    dspTreeID(tree), lclNum);

            if (optCheckLoopCloningGDVTestProfitable(relop->AsOp(), info))
            {
                // Update the loop context.
                //
                GenTreeIntCon* iconHandle =
                    relopOp2->IsIconHandle() ? relopOp2->AsIntCon() : relopOp2->AsIndir()->Addr()->AsIntCon();
                assert(iconHandle->IsIconHandle(GTF_ICON_FTN_ADDR));
                assert(compCurBB->lastStmt() == info->stmt);
                LcMethodAddrTestOptInfo* optInfo = new (this, CMK_LoopOpt)
                    LcMethodAddrTestOptInfo(info->stmt, indir, lclNum, (void*)iconHandle->IconValue(),
                                            relopOp2 != iconHandle DEBUG_ARG(
                                                            (CORINFO_METHOD_HANDLE)iconHandle->gtTargetHandle));
                info->context->EnsureLoopOptInfo(info->loopNum)->Push(optInfo);
            }
        }
    }

    return WALK_CONTINUE;
}

//----------------------------------------------------------------------------
// optIsHandleOrIndirOfHandle:
//   Check if a tree is a specified handle type or indirection of that handle type.
//
// Arguments:
//      tree       - the tree
//      handleType - the type of handle to check for
//
// Returns:
//   True if the tree is such a handle.
//
bool Compiler::optIsHandleOrIndirOfHandle(GenTree* tree, GenTreeFlags handleType)
{
    return tree->OperIs(GT_IND) ? tree->AsIndir()->Addr()->IsIconHandle(handleType) : tree->IsIconHandle(handleType);
}

//----------------------------------------------------------------------------
// optCheckLoopCloningGDVTestProfitable:
//   Check heuristically if doing loop cloning for a GDV test is profitable.
//
// Arguments:
//      guard - the GDV test
//      info  - info for the cloning we are doing
//
// Returns:
//   True if cloning is considered profitable.
//
bool Compiler::optCheckLoopCloningGDVTestProfitable(GenTreeOp* guard, LoopCloneVisitorInfo* info)
{
    JITDUMP("Checking whether cloning is profitable ...\n");
    // We only want GDV tests to inspire cloning if
    //
    // (1) we have profile data
    // (2) the loop iterates frequently each time the method is called
    // (3) the test is frequently hit during the loop iteration
    // (4) the test is biased and highly likely to succeed
    //
    const LoopDsc&    loopDsc           = optLoopTable[info->loopNum];
    BasicBlock* const loopEntry         = loopDsc.lpEntry;
    BasicBlock* const typeTestBlock     = compCurBB;
    double const      loopFrequency     = 0.50;
    double const      typeTestFrequency = 0.50;
    double const      typeTestBias      = 0.05;

    // Check for (1)
    //
    if (!loopEntry->hasProfileWeight() || !typeTestBlock->hasProfileWeight())
    {
        JITDUMP("  No; loop does not have profile data.\n");
        return WALK_CONTINUE;
    }

    // Check for (2)
    //
    if (loopEntry->getBBWeight(this) < (loopFrequency * BB_UNITY_WEIGHT))
    {
        JITDUMP("  No; loop does not iterate often enough.\n");
        return WALK_CONTINUE;
    }

    // Check for (3)
    //
    if (typeTestBlock->bbWeight < (typeTestFrequency * loopEntry->bbWeight))
    {
        JITDUMP("  No; guard does not execute often enough within the loop.\n");
        return WALK_CONTINUE;
    }

    // Check for (4)
    //
    BasicBlock* const hotSuccessor  = guard->OperIs(GT_EQ) ? typeTestBlock->bbJumpDest : typeTestBlock->bbNext;
    BasicBlock* const coldSuccessor = guard->OperIs(GT_EQ) ? typeTestBlock->bbNext : typeTestBlock->bbJumpDest;

    if (!hotSuccessor->hasProfileWeight() || !coldSuccessor->hasProfileWeight())
    {
        JITDUMP("  No; guard successor blocks were not profiled.\n");
        return WALK_CONTINUE;
    }

    if (hotSuccessor->bbWeight == BB_ZERO_WEIGHT)
    {
        JITDUMP("  No; guard hot successor block " FMT_BB " is rarely run.\n", hotSuccessor->bbNum);
        return WALK_CONTINUE;
    }

    if (coldSuccessor->bbWeight > BB_ZERO_WEIGHT)
    {
        const weight_t bias = coldSuccessor->bbWeight / (hotSuccessor->bbWeight + coldSuccessor->bbWeight);

        if (bias > typeTestBias)
        {
            JITDUMP("  No; guard not sufficiently biased: failure likelihood is " FMT_WT " > " FMT_WT "\n", bias,
                    typeTestBias);
            return WALK_CONTINUE;
        }
    }

    JITDUMP("  Yes\n");
    return true;
}

/* static */
Compiler::fgWalkResult Compiler::optCanOptimizeByLoopCloningVisitor(GenTree** pTree, Compiler::fgWalkData* data)
{
    return data->compiler->optCanOptimizeByLoopCloning(*pTree, (LoopCloneVisitorInfo*)data->pCallbackData);
}

//------------------------------------------------------------------------
// optIdentifyLoopOptInfo: Identify loop optimization candidates.
// Also, check if the loop is suitable for the optimizations performed.
//
// Arguments:
//     loopNum     -  the current loop index for which conditions are derived.
//     context     -  data structure where all loop cloning candidates will be updated.
//
// Return Value:
//     If the loop is not suitable for the optimizations, return false - context
//     should not contain any optimization candidate for the loop if false.
//     Else return true.
//
// Operation:
//      Check if the loop is well formed for this optimization and identify the
//      optimization candidates and update the "context" parameter with all the
//      contextual information necessary to perform the optimization later.
//
bool Compiler::optIdentifyLoopOptInfo(unsigned loopNum, LoopCloneContext* context)
{
    const LoopDsc& loop = optLoopTable[loopNum];
    const bool     canCloneForArrayBounds =
        ((optMethodFlags & OMF_HAS_ARRAYREF) != 0) && ((loop.lpFlags & LPFLG_ITER) != 0);
    const bool canCloneForTypeTests = ((optMethodFlags & OMF_HAS_GUARDEDDEVIRT) != 0);

    if (!canCloneForArrayBounds && !canCloneForTypeTests)
    {
        JITDUMP("Not checking loop " FMT_LP " -- no array bounds or type tests in this method\n", loopNum);
        return false;
    }

    bool shouldCloneForArrayBounds = canCloneForArrayBounds;
    bool shouldCloneForGdvTests    = canCloneForTypeTests;

#ifdef DEBUG
    shouldCloneForGdvTests &= JitConfig.JitCloneLoopsWithGdvTests() != 0;
#endif

    JITDUMP("Checking loop " FMT_LP " for optimization candidates%s%s\n", loopNum,
            shouldCloneForArrayBounds ? " (array bounds)" : "", shouldCloneForGdvTests ? " (GDV tests)" : "");

    LoopCloneVisitorInfo info(context, loopNum, nullptr, shouldCloneForArrayBounds, shouldCloneForGdvTests);
    for (BasicBlock* const block : loop.LoopBlocks())
    {
        compCurBB = block;
        for (Statement* const stmt : block->Statements())
        {
            info.stmt               = stmt;
            const bool lclVarsOnly  = false;
            const bool computeStack = false;
            fgWalkTreePre(stmt->GetRootNodePointer(), optCanOptimizeByLoopCloningVisitor, &info, lclVarsOnly,
                          computeStack);
        }
    }

    return true;
}

//------------------------------------------------------------------------------
// optObtainLoopCloningOpts: Identify optimization candidates and update
//      the "context" for array optimizations.
//
// Arguments:
//     context     -  data structure where all loop cloning info is kept. The
//                    optInfo fields of the context are updated with the
//                    identified optimization candidates.
//
// Returns:
//   true if there are any clonable loops.
//
bool Compiler::optObtainLoopCloningOpts(LoopCloneContext* context)
{
    bool result = false;
    for (unsigned i = 0; i < optLoopCount; i++)
    {
        JITDUMP("Considering loop " FMT_LP " to clone for optimizations.\n", i);
        if (optIsLoopClonable(i))
        {
            if (optIdentifyLoopOptInfo(i, context))
            {
                result = true;
            }
        }
        JITDUMP("------------------------------------------------------------\n");
    }
    JITDUMP("\n");
    return result;
}

//----------------------------------------------------------------------------
// optLoopCloningEnabled: Determine whether loop cloning is allowed. It is allowed
// in release builds. For debug builds, use the value of the DOTNET_JitCloneLoops
// flag (which defaults to 1, or allowed).
//
// Return Value:
//      true if loop cloning is allowed, false if disallowed.
//
// static
bool Compiler::optLoopCloningEnabled()
{
#ifdef DEBUG
    return JitConfig.JitCloneLoops() != 0;
#else
    return true;
#endif
}

//------------------------------------------------------------------------
// optCloneLoops: Implements loop cloning optimization.
//
// Identify loop cloning opportunities, derive loop cloning conditions,
// perform loop cloning, use the derived conditions to choose which
// path to take.
//
// Returns:
//   suitable phase status
//
PhaseStatus Compiler::optCloneLoops()
{
    JITDUMP("\n*************** In optCloneLoops()\n");
    if (optLoopCount == 0)
    {
        JITDUMP("  No loops to clone\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }
    if (!optLoopCloningEnabled())
    {
        JITDUMP("  Loop cloning disabled\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    LoopCloneContext context(optLoopCount, getAllocator(CMK_LoopClone));

    // Obtain array optimization candidates in the context.
    if (!optObtainLoopCloningOpts(&context))
    {
        JITDUMP("  No clonable loops\n");
        // TODO: if we can verify that the IR was not modified, we can return PhaseStatus::MODIFIED_NOTHING
        return PhaseStatus::MODIFIED_EVERYTHING;
    }

    unsigned optStaticallyOptimizedLoops = 0;

    // For each loop, derive cloning conditions for the optimization candidates.
    for (unsigned i = 0; i < optLoopCount; ++i)
    {
        JitExpandArrayStack<LcOptInfo*>* optInfos = context.GetLoopOptInfo(i);
        if (optInfos == nullptr)
        {
            continue;
        }

        if (!optDeriveLoopCloningConditions(i, &context) || !optComputeDerefConditions(i, &context))
        {
            JITDUMP("> Conditions could not be obtained\n");
            context.CancelLoopOptInfo(i);
        }
        else
        {
            bool allTrue  = false;
            bool anyFalse = false;
            context.EvaluateConditions(i, &allTrue, &anyFalse DEBUGARG(verbose));
            if (anyFalse)
            {
                context.CancelLoopOptInfo(i);
            }
            else if (allTrue)
            {
                // Perform static optimizations on the fast path since we always
                // have to take the cloned path.
                optPerformStaticOptimizations(i, &context DEBUGARG(false));

                ++optStaticallyOptimizedLoops;

                // No need to clone.
                context.CancelLoopOptInfo(i);
            }
        }
    }

#if 0
    // The code in this #if has been useful in debugging loop cloning issues, by
    // enabling selective enablement of the loop cloning optimization according to
    // method hash.
#ifdef DEBUG
    unsigned methHash = info.compMethodHash();
    char* lostr = getenv("loopclonehashlo");
    unsigned methHashLo = 0;
    if (lostr != NULL)
    {
        sscanf_s(lostr, "%x", &methHashLo);
        // methHashLo = (unsigned(atoi(lostr)) << 2);  // So we don't have to use negative numbers.
    }
    char* histr = getenv("loopclonehashhi");
    unsigned methHashHi = UINT32_MAX;
    if (histr != NULL)
    {
        sscanf_s(histr, "%x", &methHashHi);
        // methHashHi = (unsigned(atoi(histr)) << 2);  // So we don't have to use negative numbers.
    }
    if (methHash < methHashLo || methHash > methHashHi)
    {
        return PhaseStatus::MODIFIED_EVERYTHING;
    }
#endif
#endif

    assert(optLoopsCloned == 0); // It should be initialized, but not yet changed.
    for (unsigned i = 0; i < optLoopCount; ++i)
    {
        if (context.GetLoopOptInfo(i) != nullptr)
        {
            optLoopsCloned++;
            context.OptimizeConditions(i DEBUGARG(verbose));
            context.OptimizeBlockConditions(i DEBUGARG(verbose));
            optCloneLoop(i, &context);
        }
    }

    if (optLoopsCloned > 0)
    {
        JITDUMP("Recompute reachability and dominators after loop cloning\n");
        // TODO: recompute the loop table, to include the slow loop path in the table?
        fgUpdateChangedFlowGraph(FlowGraphUpdates::COMPUTE_DOMS);
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("Loops cloned: %d\n", optLoopsCloned);
        printf("Loops statically optimized: %d\n", optStaticallyOptimizedLoops);
        printf("After loop cloning:\n");
        fgDispBasicBlocks(/*dumpTrees*/ true);
    }

#endif

    return PhaseStatus::MODIFIED_EVERYTHING;
}
