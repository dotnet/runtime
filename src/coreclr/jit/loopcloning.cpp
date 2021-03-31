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
//      This tree produces GT_INDEX node, the caller is supposed to morph it appropriately
//      so it can be codegen'ed.
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
            arr = comp->gtNewIndexRef(TYP_REF, arr, comp->gtNewLclvNode(arrIndex->indLcls[i],
                                                                        comp->lvaTable[arrIndex->indLcls[i]].lvType));
        }
        // If asked for arrlen invoke arr length operator.
        if (oper == ArrLen)
        {
            GenTree* arrLen = comp->gtNewArrLen(TYP_INT, arr, OFFSETOF__CORINFO_Array__length, bb);
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
            return comp->gtNewLclvNode(constant, comp->lvaTable[constant].lvType);
        case ArrLen:
            return arrLen.ToGenTree(comp, bb);
        case Null:
            return comp->gtNewIconNode(0, TYP_REF);
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
//
// Return Values:
//      Returns the gen tree representation for the conditional operator on lhs and rhs trees
//
GenTree* LC_Condition::ToGenTree(Compiler* comp, BasicBlock* bb)
{
    GenTree* op1Tree = op1.ToGenTree(comp, bb);
    GenTree* op2Tree = op2.ToGenTree(comp, bb);
    assert(genTypeSize(genActualType(op1Tree->TypeGet())) == genTypeSize(genActualType(op2Tree->TypeGet())));
    return comp->gtNewOperNode(oper, TYP_INT, op1Tree, op2Tree);
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
// EnsureDerefs - Ensure an array of dereferences is created if it doesn't exist.
//
// Arguments:
//      loopNum     the loop index.
//
// Return Values:
//      The array of dereferences for the loop.
//
JitExpandArrayStack<LC_Array>* LoopCloneContext::EnsureDerefs(unsigned loopNum)
{
    if (derefs[loopNum] == nullptr)
    {
        derefs[loopNum] = new (alloc) JitExpandArrayStack<LC_Array>(alloc, 4);
    }
    return derefs[loopNum];
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
        blockConditions[loopNum] =
            new (alloc) JitExpandArrayStack<JitExpandArrayStack<LC_Condition>*>(alloc, condBlocks);
    }
    JitExpandArrayStack<JitExpandArrayStack<LC_Condition>*>* levelCond = blockConditions[loopNum];
    for (unsigned i = 0; i < condBlocks; ++i)
    {
        levelCond->Set(i, new (alloc) JitExpandArrayStack<LC_Condition>(alloc));
    }
    return levelCond;
}

#ifdef DEBUG
void LoopCloneContext::PrintBlockConditions(unsigned loopNum)
{
    printf("Block conditions:\n");

    JitExpandArrayStack<JitExpandArrayStack<LC_Condition>*>* levelCond = blockConditions[loopNum];
    if (levelCond == nullptr || levelCond->Size() == 0)
    {
        printf("No block conditions\n");
        return;
    }

    for (unsigned i = 0; i < levelCond->Size(); ++i)
    {
        printf("%d = {", i);
        for (unsigned j = 0; j < ((*levelCond)[i])->Size(); ++j)
        {
            if (j != 0)
            {
                printf(" & ");
            }
            (*((*levelCond)[i]))[j].Print();
        }
        printf("}\n");
    }
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
                // REVIEW: it appears we never hit this condition in any test.
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
        JITDUMP("NO conditions");
        return;
    }
    if (conditions[loopNum]->Size() == 0)
    {
        JITDUMP("Conditions were optimized away! Will always take cloned path.");
        return;
    }
    for (unsigned i = 0; i < conditions[loopNum]->Size(); ++i)
    {
        if (i != 0)
        {
            JITDUMP(" & ");
        }
        (*conditions[loopNum])[i].Print();
    }
}
#endif

//--------------------------------------------------------------------------------------------------
// CondToStmtInBlock - Convert an array of conditions. Evaluate them into a JTRUE stmt and add it to
//      the block
//
// Arguments:
//      comp    Compiler instance
//      conds   Array of conditions to evaluate into a JTRUE stmt
//      block   Block to insert the stmt into
//      reverse Reverse conditions if true.
//
// Note:
//      The condition that will be generated: jmpTrue(cond1 & cond2 ... == 0)
//
// Return Values:
//      None.
//
void LoopCloneContext::CondToStmtInBlock(Compiler*                          comp,
                                         JitExpandArrayStack<LC_Condition>& conds,
                                         BasicBlock*                        block,
                                         bool                               reverse)
{
    noway_assert(conds.Size() > 0);

    // Get the first condition.
    GenTree* cond = conds[0].ToGenTree(comp, block);
    for (unsigned i = 1; i < conds.Size(); ++i)
    {
        // Append all conditions using AND operator.
        cond = comp->gtNewOperNode(GT_AND, TYP_INT, cond, conds[i].ToGenTree(comp, block));
    }

    // Add "cond == 0" node
    cond = comp->gtNewOperNode(reverse ? GT_NE : GT_EQ, TYP_INT, cond, comp->gtNewIconNode(0));

    // Add jmpTrue "cond == 0" to slow path.
    GenTree*   jmpTrueTree = comp->gtNewOperNode(GT_JTRUE, TYP_VOID, cond);
    Statement* stmt        = comp->fgNewStmtFromTree(jmpTrueTree);

    // Add stmt to the block.
    comp->fgInsertStmtAtEnd(block, stmt);

    // Remorph.
    comp->fgMorphBlockStmt(block, stmt DEBUGARG("Loop cloning condition"));
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
unsigned LC_Deref::Lcl()
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
bool LC_Deref::HasChildren()
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
void LC_Deref::DeriveLevelConditions(JitExpandArrayStack<JitExpandArrayStack<LC_Condition>*>* conds)
{
    if (level == 0)
    {
        // For level 0, just push (a != null).
        (*conds)[level]->Push(
            LC_Condition(GT_NE, LC_Expr(LC_Ident(Lcl(), LC_Ident::Var)), LC_Expr(LC_Ident(LC_Ident::Null))));
    }
    else
    {
        // Adjust for level0 having just 1 condition and push condition (i < a.len).
        LC_Array arrLen = array;
        arrLen.oper     = LC_Array::ArrLen;
        arrLen.dim      = level - 1;
        (*conds)[level * 2 - 1]->Push(
            LC_Condition(GT_LT, LC_Expr(LC_Ident(Lcl(), LC_Ident::Var)), LC_Expr(LC_Ident(arrLen))));

        // Push condition (a[i] != null)
        LC_Array arrTmp = array;
        arrTmp.dim      = level;
        (*conds)[level * 2]->Push(LC_Condition(GT_NE, LC_Expr(LC_Ident(arrTmp)), LC_Expr(LC_Ident(LC_Ident::Null))));
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
void LC_Deref::EnsureChildren(CompAllocator alloc)
{
    if (children == nullptr)
    {
        children = new (alloc) JitExpandArrayStack<LC_Deref*>(alloc);
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
LC_Deref* LC_Deref::Find(unsigned lcl)
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
LC_Deref* LC_Deref::Find(JitExpandArrayStack<LC_Deref*>* children, unsigned lcl)
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
