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
//     condition is "less than". If the initializer is "var" init then adds condition
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

    if (loop->lpTestOper() == GT_LT)
    {
        // Stride conditions
        if (loop->lpIterConst() <= 0)
        {
            JITDUMP("> Stride %d is invalid\n", loop->lpIterConst());
            return false;
        }

        // Init conditions
        if (loop->lpFlags & LPFLG_CONST_INIT)
        {
            // Only allowing non-negative const init at this time.
            // REVIEW: why?
            if (loop->lpConstInit < 0)
            {
                JITDUMP("> Init %d is invalid\n", loop->lpConstInit);
                return false;
            }
        }
        else if (loop->lpFlags & LPFLG_VAR_INIT)
        {
            // initVar >= 0
            LC_Condition geZero(GT_GE, LC_Expr(LC_Ident(loop->lpVarInit, LC_Ident::Var)),
                                LC_Expr(LC_Ident(0, LC_Ident::Const)));
            context->EnsureConditions(loopNum)->Push(geZero);
        }
        else
        {
            JITDUMP("> Not variable init\n");
            return false;
        }

        // Limit Conditions
        LC_Ident ident;
        if (loop->lpFlags & LPFLG_CONST_LIMIT)
        {
            int limit = loop->lpConstLimit();
            if (limit < 0)
            {
                JITDUMP("> limit %d is invalid\n", limit);
                return false;
            }
            ident = LC_Ident(static_cast<unsigned>(limit), LC_Ident::Const);
        }
        else if (loop->lpFlags & LPFLG_VAR_LIMIT)
        {
            unsigned limitLcl = loop->lpVarLimit();
            ident             = LC_Ident(limitLcl, LC_Ident::Var);

            LC_Condition geZero(GT_GE, LC_Expr(ident), LC_Expr(LC_Ident(0, LC_Ident::Const)));

            context->EnsureConditions(loopNum)->Push(geZero);
        }
        else if (loop->lpFlags & LPFLG_ARRLEN_LIMIT)
        {
            ArrIndex* index = new (getAllocator(CMK_LoopClone)) ArrIndex(getAllocator(CMK_LoopClone));
            if (!loop->lpArrLenLimit(this, index))
            {
                JITDUMP("> ArrLen not matching");
                return false;
            }
            ident = LC_Ident(LC_Array(LC_Array::Jagged, index, LC_Array::ArrLen));

            // Ensure that this array must be dereference-able, before executing the actual condition.
            LC_Array array(LC_Array::Jagged, index, LC_Array::None);
            context->EnsureDerefs(loopNum)->Push(array);
        }
        else
        {
            JITDUMP("> Undetected limit\n");
            return false;
        }

        for (unsigned i = 0; i < optInfos->Size(); ++i)
        {
            LcOptInfo* optInfo = optInfos->GetRef(i);
            switch (optInfo->GetOptType())
            {
                case LcOptInfo::LcJaggedArray:
                {
                    // limit <= arrLen
                    LcJaggedArrayOptInfo* arrIndexInfo = optInfo->AsLcJaggedArrayOptInfo();
                    LC_Array arrLen(LC_Array::Jagged, &arrIndexInfo->arrIndex, arrIndexInfo->dim, LC_Array::ArrLen);
                    LC_Ident arrLenIdent = LC_Ident(arrLen);

                    LC_Condition cond(GT_LE, LC_Expr(ident), LC_Expr(arrLenIdent));
                    context->EnsureConditions(loopNum)->Push(cond);

                    // Ensure that this array must be dereference-able, before executing the actual condition.
                    LC_Array array(LC_Array::Jagged, &arrIndexInfo->arrIndex, arrIndexInfo->dim, LC_Array::None);
                    context->EnsureDerefs(loopNum)->Push(array);
                }
                break;
                case LcOptInfo::LcMdArray:
                {
                    // limit <= mdArrLen
                    LcMdArrayOptInfo* mdArrInfo = optInfo->AsLcMdArrayOptInfo();
                    LC_Condition      cond(GT_LE, LC_Expr(ident),
                                      LC_Expr(LC_Ident(LC_Array(LC_Array::MdArray, mdArrInfo->GetArrIndexForDim(
                                                                                       getAllocator(CMK_LoopClone)),
                                                                mdArrInfo->dim, LC_Array::None))));
                    context->EnsureConditions(loopNum)->Push(cond);
                }
                break;

                default:
                    JITDUMP("Unknown opt\n");
                    return false;
            }
        }
        JITDUMP("Conditions: (");
        DBEXEC(verbose, context->PrintConditions(loopNum));
        JITDUMP(")\n");
        return true;
    }
    return false;
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
    JitExpandArrayStack<LC_Deref*> nodes(getAllocator(CMK_LoopClone));
    int                            maxRank = -1;

    // Get the dereference-able arrays.
    JitExpandArrayStack<LC_Array>* deref = context->EnsureDerefs(loopNum);

    // For each array in the dereference list, construct a tree,
    // where the nodes are array and index variables and an edge 'u-v'
    // exists if a node 'v' indexes node 'u' directly as in u[v] or an edge
    // 'u-v-w' transitively if u[v][w] occurs.
    for (unsigned i = 0; i < deref->Size(); ++i)
    {
        LC_Array& array = (*deref)[i];

        // First populate the array base variable.
        LC_Deref* node = LC_Deref::Find(&nodes, array.arrIndex->arrLcl);
        if (node == nullptr)
        {
            node = new (getAllocator(CMK_LoopClone)) LC_Deref(array, 0 /*level*/);
            nodes.Push(node);
        }

        // For each dimension (level) for the array, populate the tree with the variable
        // from that dimension.
        unsigned rank = (unsigned)array.GetDimRank();
        for (unsigned i = 0; i < rank; ++i)
        {
            node->EnsureChildren(getAllocator(CMK_LoopClone));
            LC_Deref* tmp = node->Find(array.arrIndex->indLcls[i]);
            if (tmp == nullptr)
            {
                tmp = new (getAllocator(CMK_LoopClone)) LC_Deref(array, node->level + 1);
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
        printf("Deref condition tree:\n");
        for (unsigned i = 0; i < nodes.Size(); ++i)
        {
            if (i != 0)
            {
                printf(",");
            }
            nodes[i]->Print();
            printf("\n");
        }
    }
#endif

    if (maxRank == -1)
    {
        return false;
    }

    // First level will always yield the null-check, since it is made of the array base variables.
    // All other levels (dimensions) will yield two conditions ex: (i < a.length && a[i] != null)
    // So add 1 after rank * 2.
    unsigned condBlocks = (unsigned)maxRank * 2 + 1;

    // Heuristic to not create too many blocks.
    // REVIEW: due to the definition of `condBlocks`, above, the effective max is 3 blocks, meaning
    // `maxRank` of 1. Question: should the heuristic allow more blocks to be created in some situations?
    // REVIEW: make this based on a COMPlus configuration?
    if (condBlocks > 4)
    {
        return false;
    }

    // Derive conditions into an 'array of level x array of conditions' i.e., levelCond[levels][conds]
    JitExpandArrayStack<JitExpandArrayStack<LC_Condition>*>* levelCond =
        context->EnsureBlockConditions(loopNum, condBlocks);
    for (unsigned i = 0; i < nodes.Size(); ++i)
    {
        nodes[i]->DeriveLevelConditions(levelCond);
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
    for (unsigned i = 0; i < optInfos->Size(); ++i)
    {
        LcOptInfo* optInfo = optInfos->GetRef(i);
        switch (optInfo->GetOptType())
        {
            case LcOptInfo::LcJaggedArray:
            {
                LcJaggedArrayOptInfo* arrIndexInfo = optInfo->AsLcJaggedArrayOptInfo();
                compCurBB                          = arrIndexInfo->arrIndex.useBlock;
                optRemoveCommaBasedRangeCheck(arrIndexInfo->arrIndex.bndsChks[arrIndexInfo->dim], arrIndexInfo->stmt);
                DBEXEC(dynamicPath, optDebugLogLoopCloning(arrIndexInfo->arrIndex.useBlock, arrIndexInfo->stmt));
            }
            break;
            case LcOptInfo::LcMdArray:
                // TODO-CQ: CLONE: Implement.
                break;
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
    const LoopDsc& loop = optLoopTable[loopInd];

    if (!(loop.lpFlags & LPFLG_ITER))
    {
        JITDUMP("Loop cloning: rejecting loop " FMT_LP ". No LPFLG_ITER flag.\n", loopInd);
        return false;
    }

    if (loop.lpFlags & LPFLG_REMOVED)
    {
        JITDUMP("Loop cloning: rejecting loop " FMT_LP ". It is marked LPFLG_REMOVED.\n", loopInd);
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
    BasicBlock* stopAt       = loop.lpBottom->bbNext;
    unsigned    loopRetCount = 0;
    for (BasicBlock* blk = loop.lpFirst; blk != stopAt; blk = blk->bbNext)
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
    // Usually, we create a dummy block after the orginal loop, to skip over the loop clone
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

    unsigned ivLclNum = loop.lpIterVar();
    if (lvaVarAddrExposed(ivLclNum))
    {
        JITDUMP("Loop cloning: rejecting loop " FMT_LP ". Rejected V%02u as iter var because is address-exposed.\n",
                loopInd, ivLclNum);
        return false;
    }

    BasicBlock* head = loop.lpHead;
    BasicBlock* end  = loop.lpBottom;
    BasicBlock* beg  = head->bbNext;

    if (end->bbJumpKind != BBJ_COND)
    {
        JITDUMP("Loop cloning: rejecting loop " FMT_LP ". Couldn't find termination test.\n", loopInd);
        return false;
    }

    if (end->bbJumpDest != beg)
    {
        JITDUMP("Loop cloning: rejecting loop " FMT_LP ". Branch at loop 'end' not looping to 'begin'.\n", loopInd);
        return false;
    }

    // TODO-CQ: CLONE: Mark increasing or decreasing loops.
    if ((loop.lpIterOper() != GT_ADD) || (loop.lpIterConst() != 1))
    {
        JITDUMP("Loop cloning: rejecting loop " FMT_LP ". Loop iteration operator not matching.\n", loopInd);
        return false;
    }

    if ((loop.lpFlags & LPFLG_CONST_LIMIT) == 0 && (loop.lpFlags & LPFLG_VAR_LIMIT) == 0 &&
        (loop.lpFlags & LPFLG_ARRLEN_LIMIT) == 0)
    {
        JITDUMP("Loop cloning: rejecting loop " FMT_LP ". Loop limit is neither constant, variable or array length.\n",
                loopInd);
        return false;
    }

    if (!((GenTree::StaticOperIs(loop.lpTestOper(), GT_LT, GT_LE) && (loop.lpIterOper() == GT_ADD)) ||
          (GenTree::StaticOperIs(loop.lpTestOper(), GT_GT, GT_GE) && (loop.lpIterOper() == GT_SUB))))
    {
        JITDUMP("Loop cloning: rejecting loop " FMT_LP
                ". Loop test (%s) doesn't agree with the direction (%s) of the loop.\n",
                loopInd, GenTree::OpName(loop.lpTestOper()), GenTree::OpName(loop.lpIterOper()));
        return false;
    }

    if (!(loop.lpTestTree->OperKind() & GTK_RELOP) || !(loop.lpTestTree->gtFlags & GTF_RELOP_ZTT))
    {
        JITDUMP("Loop cloning: rejecting loop " FMT_LP ". Loop inversion NOT present, loop test [%06u] may not protect "
                "entry from head.\n",
                loopInd, loop.lpTestTree->gtTreeID);
        return false;
    }

#ifdef DEBUG
    GenTree* op1 = loop.lpIterator();
    assert((op1->gtOper == GT_LCL_VAR) && (op1->AsLclVarCommon()->GetLclNum() == ivLclNum));
#endif

    // Otherwise, we're going to add those return blocks.
    fgReturnCount += loopRetCount;

    return true;
}

//--------------------------------------------------------------------------------------------------
// optInsertLoopChoiceConditions - Insert the loop conditions for a loop between loop head and entry
//
// Arguments:
//      context     loop cloning context variable
//      loopNum     the loop index
//      head        loop head for "loopNum"
//      slowHead    the slow path loop head
//
// Return Value:
//      The last conditional block inserted.
//
// Operation:
//      Create the following structure.
//
//      Note below that the cond0 is inverted in head, i.e., if true jump to cond1. This is because
//      condn cannot jtrue to loop head h2. It has to be from a direct pred block.
//
//      cond0 (in h)  -?> cond1
//      slowHead      --> e2 (slowHead) always
//      !cond1        -?> slowHead
//      !cond2        -?> slowHead
//      ...
//      !condn        -?> slowHead
//      h2/entry (fast)
//
//      Insert condition 0 in 'h' and create other condition blocks and insert conditions in them.
//      On entry, block 'h' is a conditional block, but its bbJumpDest hasn't yet been set.
//
BasicBlock* Compiler::optInsertLoopChoiceConditions(LoopCloneContext* context,
                                                    unsigned          loopNum,
                                                    BasicBlock*       head,
                                                    BasicBlock*       slowHead)
{
    JITDUMP("Inserting loop cloning conditions\n");
    assert(context->HasBlockConditions(loopNum));

    BasicBlock*                                              curCond   = head;
    JitExpandArrayStack<JitExpandArrayStack<LC_Condition>*>* levelCond = context->GetBlockConditions(loopNum);
    for (unsigned i = 0; i < levelCond->Size(); ++i)
    {
        bool isHeaderBlock = (curCond == head);

        // Flip the condition if header block.
        context->CondToStmtInBlock(this, *((*levelCond)[i]), curCond, /*reverse*/ isHeaderBlock);

        // Create each condition block ensuring wiring between them.
        BasicBlock* tmp     = fgNewBBafter(BBJ_COND, isHeaderBlock ? slowHead : curCond, /*extendRegion*/ true);
        curCond->bbJumpDest = isHeaderBlock ? tmp : slowHead;

        JITDUMP("Adding " FMT_BB " -> " FMT_BB "\n", curCond->bbNum, curCond->bbJumpDest->bbNum);
        fgAddRefPred(curCond->bbJumpDest, curCond);

        if (!isHeaderBlock)
        {
            JITDUMP("Adding " FMT_BB " -> " FMT_BB "\n", curCond->bbNum, tmp->bbNum);
            fgAddRefPred(tmp, curCond);
        }

        curCond = tmp;

        curCond->inheritWeight(head);
        curCond->bbNatLoopNum = head->bbNatLoopNum;
        JITDUMP("Created new " FMT_BB " for new level %u\n", curCond->bbNum, i);
    }

    // Finally insert cloning conditions after all deref conditions have been inserted.
    context->CondToStmtInBlock(this, *(context->GetConditions(loopNum)), curCond, /*reverse*/ false);
    return curCond;
}

//------------------------------------------------------------------------
// OptEnsureUniqueHead: Ensure that loop "loopInd" has a unique head block.
// If the existing entry has non-loop predecessors other than the head entry,
// create a new, empty block that goes (only) to the entry, and redirects the
// preds of the entry to this new block. Sets the weight of the newly created
// block to "ambientWeight".
//
// NOTE: this is currently dead code, because it is only called by loop cloning,
// and loop cloning only works with single-entry loops where the immediately
// preceding head block is the only predecessor of the loop entry.
//
// Arguments:
//    loopInd       - index of loop to process
//    ambientWeight - weight to give the new head, if created.
//
void Compiler::optEnsureUniqueHead(unsigned loopInd, BasicBlock::weight_t ambientWeight)
{
    LoopDsc& loop = optLoopTable[loopInd];

    BasicBlock* h = loop.lpHead;
    BasicBlock* t = loop.lpTop;
    BasicBlock* e = loop.lpEntry;
    BasicBlock* b = loop.lpBottom;

    // If "h" dominates the entry block, then it is the unique header.
    if (fgDominate(h, e))
    {
        return;
    }

    // Otherwise, create a new empty header block, make it the pred of the entry block,
    // and redirect the preds of the entry block to go to this.

    BasicBlock* beforeTop = t->bbPrev;
    assert(!beforeTop->bbFallsThrough() || (beforeTop->bbNext == e));

    // Make sure that the new block is in the same region as the loop.
    // (We will only create loops that are entirely within a region.)
    BasicBlock* h2 = fgNewBBafter(BBJ_NONE, beforeTop, /*extendRegion*/ true);
    assert(beforeTop->bbNext == h2);

    // This is in the containing loop.
    h2->bbNatLoopNum = loop.lpParent;
    h2->bbWeight     = h2->isRunRarely() ? BB_ZERO_WEIGHT : ambientWeight;

    if (h2->bbNext != e)
    {
        h2->bbJumpKind = BBJ_ALWAYS;
        h2->bbJumpDest = e;
    }
    BlockSetOps::Assign(this, h2->bbReach, e->bbReach);

    fgAddRefPred(e, h2);

    // Redirect paths from preds of "e" to go to "h2" instead of "e".
    BlockToBlockMap* blockMap = new (getAllocator(CMK_LoopClone)) BlockToBlockMap(getAllocator(CMK_LoopClone));
    blockMap->Set(e, h2);

    for (flowList* predEntry = e->bbPreds; predEntry != nullptr; predEntry = predEntry->flNext)
    {
        BasicBlock* predBlock = predEntry->getBlock();

        // Skip if predBlock is in the loop.
        if (t->bbNum <= predBlock->bbNum && predBlock->bbNum <= b->bbNum)
        {
            continue;
        }

        optRedirectBlock(predBlock, blockMap);

        fgAddRefPred(h2, predBlock);
        fgRemoveRefPred(e, predBlock);
    }

    optUpdateLoopHead(loopInd, h, h2);
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

    JITDUMP("\nCloning loop " FMT_LP ": [head: " FMT_BB ", first: " FMT_BB ", top: " FMT_BB ", entry: " FMT_BB
            ", bottom: " FMT_BB ", child: " FMT_LP "].\n",
            loopInd, loop.lpHead->bbNum, loop.lpFirst->bbNum, loop.lpTop->bbNum, loop.lpEntry->bbNum,
            loop.lpBottom->bbNum, loop.lpChild);

    // Determine the depth of the loop, so we can properly weight blocks added (outside the cloned loop blocks).
    unsigned             depth         = optLoopDepth(loopInd);
    BasicBlock::weight_t ambientWeight = 1;
    for (unsigned j = 0; j < depth; j++)
    {
        BasicBlock::weight_t lastWeight = ambientWeight;
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
    const BasicBlock::weight_t fastPathWeightScaleFactor = 0.99f;
    const BasicBlock::weight_t slowPathWeightScaleFactor = 1.0f - fastPathWeightScaleFactor;

    // This is the containing loop, if any -- to label any blocks we create that are outside
    // the loop being cloned.
    unsigned char ambientLoop = loop.lpParent;

    // First, make sure that the loop has a unique header block, creating an empty one if necessary.
    optEnsureUniqueHead(loopInd, ambientWeight);

    // We're going to transform this loop:
    //
    // H --> E    (or, H conditionally branches around the loop and has fall-through to F == T == E)
    // F
    // T
    // E
    // B ?-> T
    // X
    //
    // to this pair of loops:
    //
    // H ?-> E2
    // H2--> E    (Optional; if E == T == F, let H fall through to F/T/E)
    // F
    // T
    // E
    // B  ?-> T
    // X2--> X
    // F2
    // T2
    // E2
    // B2 ?-> T2
    // X

    BasicBlock* h = loop.lpHead;
    if (h->bbJumpKind != BBJ_NONE && h->bbJumpKind != BBJ_ALWAYS)
    {
        // Make a new block to be the unique entry to the loop.
        JITDUMP("Create new unique single-successor entry to loop\n");
        assert((h->bbJumpKind == BBJ_COND) && (h->bbNext == loop.lpEntry));
        BasicBlock* newH = fgNewBBafter(BBJ_NONE, h, /*extendRegion*/ true);
        JITDUMP("Adding " FMT_BB " after " FMT_BB "\n", newH->bbNum, h->bbNum);
        newH->bbWeight = newH->isRunRarely() ? BB_ZERO_WEIGHT : ambientWeight;
        BlockSetOps::Assign(this, newH->bbReach, h->bbReach);
        // This is in the scope of a surrounding loop, if one exists -- the parent of the loop we're cloning.
        newH->bbNatLoopNum = ambientLoop;
        optUpdateLoopHead(loopInd, h, newH);

        fgAddRefPred(newH, h); // Add h->newH pred edge
        JITDUMP("Adding " FMT_BB " -> " FMT_BB "\n", h->bbNum, newH->bbNum);
        fgReplacePred(newH->bbNext, h, newH); // Replace pred in COND fall-through block.
        JITDUMP("Replace " FMT_BB " -> " FMT_BB " with " FMT_BB " -> " FMT_BB "\n", h->bbNum, newH->bbNext->bbNum,
                newH->bbNum, newH->bbNext->bbNum);

        h = newH;
    }
    assert(h == loop.lpHead);

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

    // Now we'll make "h2", after "h" to go to "e" -- unless the loop is a do-while,
    // so that "h" already falls through to "e" (e == t == f).
    // It might look like this code is unreachable, since "h" must be a BBJ_ALWAYS, but
    // later we will change "h" to a BBJ_COND along with a set of loop conditions.
    // TODO: it still might be unreachable, since cloning currently is restricted to "do-while" loop forms.
    BasicBlock* h2 = nullptr;
    if (h->bbNext != loop.lpEntry)
    {
        assert(h->bbJumpKind == BBJ_ALWAYS);
        JITDUMP("Create branch to entry of optimized loop\n");
        BasicBlock* h2 = fgNewBBafter(BBJ_ALWAYS, h, /*extendRegion*/ true);
        JITDUMP("Adding " FMT_BB " after " FMT_BB "\n", h2->bbNum, h->bbNum);
        h2->bbWeight = h2->isRunRarely() ? BB_ZERO_WEIGHT : ambientWeight;

        // This is in the scope of a surrounding loop, if one exists -- the parent of the loop we're cloning.
        h2->bbNatLoopNum = ambientLoop;

        h2->bbJumpDest = loop.lpEntry;

        fgAddRefPred(h2, h); // Add h->h2 pred edge
        JITDUMP("Adding " FMT_BB " -> " FMT_BB "\n", h->bbNum, h2->bbNum);
        fgReplacePred(loop.lpEntry, h, h2);
        JITDUMP("Replace " FMT_BB " -> " FMT_BB " with " FMT_BB " -> " FMT_BB "\n", h->bbNum, loop.lpEntry->bbNum,
                h2->bbNum, loop.lpEntry->bbNum);

        optUpdateLoopHead(loopInd, h, h2);

        // NOTE: 'h' is no longer the loop head; 'h2' is!
    }

    // Now we'll clone the blocks of the loop body. These cloned blocks will be the slow path.
    BasicBlock* newFirst = nullptr;

    BlockToBlockMap* blockMap = new (getAllocator(CMK_LoopClone)) BlockToBlockMap(getAllocator(CMK_LoopClone));
    for (BasicBlock* blk = loop.lpFirst; blk != loop.lpBottom->bbNext; blk = blk->bbNext)
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
        if (blk->isLoopAlign())
        {
            newBlk->bbFlags &= ~BBF_LOOP_ALIGN;
            JITDUMP("Removing LOOP_ALIGN flag from cloned loop in " FMT_BB "\n", newBlk->bbNum);
        }
#endif

        // TODO-Cleanup: The above clones the bbNatLoopNum, which is incorrect.  Eventually, we should probably insert
        // the cloned loop in the loop table.  For now, however, we'll just make these blocks be part of the surrounding
        // loop, if one exists -- the parent of the loop we're cloning.
        newBlk->bbNatLoopNum = loop.lpParent;

        if (newFirst == nullptr)
        {
            newFirst = newBlk;
        }
        newPred = newBlk;
        blockMap->Set(blk, newBlk);
    }

    // Perform the static optimizations on the fast path.
    optPerformStaticOptimizations(loopInd, context DEBUGARG(true));

    // Now go through the new blocks, remapping their jump targets within the loop
    // and updating the preds lists.
    for (BasicBlock* blk = loop.lpFirst; blk != loop.lpBottom->bbNext; blk = blk->bbNext)
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
            {
                for (unsigned i = 0; i < newblk->bbJumpSwt->bbsCount; i++)
                {
                    BasicBlock* switchDest = newblk->bbJumpSwt->bbsDstTab[i];
                    fgAddRefPred(switchDest, newblk);
                }
            }
            break;

            default:
                break;
        }
    }

#ifdef DEBUG
    // Display the preds for the new blocks, after all the new blocks have been redirected.
    JITDUMP("Preds after loop copy:\n");
    for (BasicBlock* blk = loop.lpFirst; blk != loop.lpBottom->bbNext; blk = blk->bbNext)
    {
        BasicBlock* newblk = nullptr;
        bool        b      = blockMap->Lookup(blk, &newblk);
        assert(b && newblk != nullptr);
        JITDUMP(FMT_BB ":", newblk->bbNum);
        for (flowList* pred = newblk->bbPreds; pred != nullptr; pred = pred->flNext)
        {
            JITDUMP(" " FMT_BB, pred->getBlock()->bbNum);
        }
        JITDUMP("\n");
    }
#endif // DEBUG

    // We will create the following structure
    //
    // cond0 (in h)  -?> cond1
    // slow          --> e2 (slow) always
    // !cond1        -?> slow
    // !cond2        -?> slow
    // ...
    // !condn        -?> slow
    // h2/entry (fast)
    //
    // We should always have block conditions, at the minimum, the array should be deref-able
    assert(context->HasBlockConditions(loopInd));

    if (h->bbJumpKind == BBJ_NONE)
    {
        assert((h->bbNext == h2) || (h->bbNext == loop.lpEntry));
    }
    else
    {
        assert(h->bbJumpKind == BBJ_ALWAYS);
        assert(h->bbJumpDest == loop.lpEntry);
    }

    // If all the conditions are true, go to E2.
    BasicBlock* e2      = nullptr;
    bool        foundIt = blockMap->Lookup(loop.lpEntry, &e2);

    // We're going to replace the fall-through path from "h".
    if (h->bbJumpKind == BBJ_NONE)
    {
        fgRemoveRefPred(h->bbNext, h);
    }

    // Create a unique header for the slow path.
    JITDUMP("Create unique head block for slow path loop\n");
    BasicBlock* slowHead = fgNewBBafter(BBJ_ALWAYS, h, /*extendRegion*/ true);
    JITDUMP("Adding " FMT_BB " after " FMT_BB "\n", slowHead->bbNum, h->bbNum);
    slowHead->bbWeight = h->isRunRarely() ? BB_ZERO_WEIGHT : ambientWeight;
    slowHead->scaleBBWeight(slowPathWeightScaleFactor);
    slowHead->bbNatLoopNum = ambientLoop;
    slowHead->bbJumpDest   = e2;

    fgAddRefPred(slowHead, h);
    JITDUMP("Adding " FMT_BB " -> " FMT_BB "\n", h->bbNum, slowHead->bbNum);

    // This is the only predecessor to the copied loop, and it hasn't been added yet.
    fgAddRefPred(slowHead->bbJumpDest, slowHead);
    JITDUMP("Adding " FMT_BB " -> " FMT_BB "\n", slowHead->bbNum, slowHead->bbJumpDest->bbNum);

    // "h" is now going to be a COND block
    h->bbJumpKind = BBJ_COND;

    BasicBlock* condLast = optInsertLoopChoiceConditions(context, loopInd, h, slowHead);
    condLast->bbJumpDest = slowHead;

    JITDUMP("Adding " FMT_BB " -> " FMT_BB "\n", condLast->bbNum, condLast->bbJumpDest->bbNum);
    fgAddRefPred(condLast->bbJumpDest, condLast);

    // Add the fall-through path pred.
    assert(condLast->bbJumpKind == BBJ_COND);
    JITDUMP("Adding " FMT_BB " -> " FMT_BB "\n", condLast->bbNum, condLast->bbNext->bbNum);
    fgAddRefPred(condLast->bbNext, condLast);

    // If h2 is present it is already the head or replace 'h' by 'condLast'.
    if (h2 == nullptr)
    {
        optUpdateLoopHead(loopInd, loop.lpHead, condLast);
    }
    assert(foundIt && e2 != nullptr);

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
//      tree        the tree to be checked if it is the array [] operation.
//      result      the extracted GT_INDEX information is updated in result.
//      lhsNum      for the root level (function is recursive) callers should pass BAD_VAR_NUM.
//
//  Return Value:
//      Returns true if array index can be extracted, else, return false. See assumption about
//      what will be extracted. The "result" variable's rank parameter is advanced for every
//      dimension of [] encountered.
//
//  Operation:
//      Given a "tree" extract the GT_INDEX node in "result" as ArrIndex. In FlowGraph morph
//      we have converted a GT_INDEX tree into a scaled index base offset expression. We need
//      to reconstruct this to be able to know if this is an array access.
//
//  Assumption:
//      The method extracts only if the array base and indices are GT_LCL_VAR.
//
//  TODO-CQ: CLONE: After morph make sure this method extracts values before morph.
//
//  Example tree to pattern match:
//
// *  COMMA     int
// +--*  ARR_BOUNDS_CHECK_Rng void
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
// +--*  ARR_BOUNDS_CHECK_Rng void
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
bool Compiler::optExtractArrIndex(GenTree* tree, ArrIndex* result, unsigned lhsNum)
{
    if (tree->gtOper != GT_COMMA)
    {
        return false;
    }
    GenTree* before = tree->gtGetOp1();
    if (before->gtOper != GT_ARR_BOUNDS_CHECK)
    {
        return false;
    }
    GenTreeBoundsChk* arrBndsChk = before->AsBoundsChk();
    if (arrBndsChk->gtIndex->gtOper != GT_LCL_VAR)
    {
        return false;
    }

    // For span we may see gtArrLen is a local var or local field or constant.
    // We won't try and extract those.
    if (arrBndsChk->gtArrLen->OperIs(GT_LCL_VAR, GT_LCL_FLD, GT_CNS_INT))
    {
        return false;
    }
    if (arrBndsChk->gtArrLen->gtGetOp1()->gtOper != GT_LCL_VAR)
    {
        return false;
    }
    unsigned arrLcl = arrBndsChk->gtArrLen->gtGetOp1()->AsLclVarCommon()->GetLclNum();
    if (lhsNum != BAD_VAR_NUM && arrLcl != lhsNum)
    {
        return false;
    }

    unsigned indLcl = arrBndsChk->gtIndex->AsLclVarCommon()->GetLclNum();

    GenTree* after = tree->gtGetOp2();

    if (after->gtOper != GT_IND)
    {
        return false;
    }
    // It used to be the case that arrBndsChks for struct types would fail the previous check because
    // after->gtOper was an address (for a block op).  In order to avoid asmDiffs we will for now
    // return false if the type of 'after' is a struct type.  (This was causing us to clone loops
    // that we were not previously cloning.)
    // TODO-1stClassStructs: Remove this check to enable optimization of array bounds checks for struct
    // types.
    if (varTypeIsStruct(after))
    {
        return false;
    }

    GenTree* sibo = after->gtGetOp1(); // sibo = scale*index + base + offset
    if (sibo->gtOper != GT_ADD)
    {
        return false;
    }
    GenTree* base = sibo->gtGetOp1();
    GenTree* sio  = sibo->gtGetOp2(); // sio == scale*index + offset
    if (base->OperGet() != GT_LCL_VAR || base->AsLclVarCommon()->GetLclNum() != arrLcl)
    {
        return false;
    }
    if (sio->gtOper != GT_ADD)
    {
        return false;
    }
    GenTree* ofs = sio->gtGetOp2();
    GenTree* si  = sio->gtGetOp1(); // si = scale*index
    if (ofs->gtOper != GT_CNS_INT)
    {
        return false;
    }
    GenTree* index;
    if (si->gtOper == GT_LSH)
    {
        GenTree* scale = si->gtGetOp2();
        index          = si->gtGetOp1();
        if (scale->gtOper != GT_CNS_INT)
        {
            return false;
        }
    }
    else
    {
        // No scale (e.g., byte array).
        index = si;
    }
#ifdef TARGET_64BIT
    if (index->gtOper != GT_CAST)
    {
        return false;
    }
    GenTree* indexVar = index->gtGetOp1();
#else
    GenTree* indexVar = index;
#endif
    if (indexVar->gtOper != GT_LCL_VAR || indexVar->AsLclVarCommon()->GetLclNum() != indLcl)
    {
        return false;
    }
    if (lhsNum == BAD_VAR_NUM)
    {
        result->arrLcl = arrLcl;
    }
    result->indLcls.Push(indLcl);
    result->bndsChks.Push(tree);
    result->useBlock = compCurBB;
    result->rank++;

    return true;
}

//---------------------------------------------------------------------------------------------------------------
//  optReconstructArrIndex: Reconstruct array index.
//
//  Arguments:
//      tree        the tree to be checked if it is an array [][][] operation.
//      result      OUT: the extracted GT_INDEX information.
//      lhsNum      for the root level (function is recursive) callers should pass BAD_VAR_NUM.
//
//  Return Value:
//      Returns true if array index can be extracted, else, return false. "rank" field in
//      "result" contains the array access depth. The "indLcls" fields contain the indices.
//
//  Operation:
//      Recursively look for a list of array indices. In the example below, we encounter,
//      V03 = ((V05 = V00[V01]), (V05[V02])) which corresponds to access of V00[V01][V02]
//      The return value would then be:
//      ArrIndex result { arrLcl: V00, indLcls: [V01, V02], rank: 2 }
//
//      V00[V01][V02] would be morphed as:
//
//      [000000001B366848] ---XG-------                        indir     int
//      [000000001B36BC50] ------------                                 V05 + (V02 << 2) + 16
//      [000000001B36C200] ---XG-------                     comma     int
//      [000000001B36BDB8] ---X--------                        arrBndsChk(V05, V02)
//      [000000001B36C278] -A-XG-------                  comma     int
//      [000000001B366730] R--XG-------                           indir     ref
//      [000000001B36C2F0] ------------                             V00 + (V01 << 3) + 24
//      [000000001B36C818] ---XG-------                        comma     ref
//      [000000001B36C458] ---X--------                           arrBndsChk(V00, V01)
//      [000000001B36BB60] -A-XG-------                     =         ref
//      [000000001B36BAE8] D------N----                        lclVar    ref    V05 tmp2
//      [000000001B36A668] -A-XG-------               =         int
//      [000000001B36A5F0] D------N----                  lclVar    int    V03 tmp0
//
//  Assumption:
//      The method extracts only if the array base and indices are GT_LCL_VAR.
//
bool Compiler::optReconstructArrIndex(GenTree* tree, ArrIndex* result, unsigned lhsNum)
{
    // If we can extract "tree" (which is a top level comma) return.
    if (optExtractArrIndex(tree, result, lhsNum))
    {
        return true;
    }
    // We have a comma (check if array base expr is computed in "before"), descend further.
    else if (tree->OperGet() == GT_COMMA)
    {
        GenTree* before = tree->gtGetOp1();
        // "before" should evaluate an array base for the "after" indexing.
        if (before->OperGet() != GT_ASG)
        {
            return false;
        }
        GenTree* lhs = before->gtGetOp1();
        GenTree* rhs = before->gtGetOp2();

        // "rhs" should contain an GT_INDEX
        if (!lhs->IsLocal() || !optReconstructArrIndex(rhs, result, lhsNum))
        {
            return false;
        }
        unsigned lhsNum = lhs->AsLclVarCommon()->GetLclNum();
        GenTree* after  = tree->gtGetOp2();
        // Pass the "lhsNum", so we can verify if indeed it is used as the array base.
        return optExtractArrIndex(after, result, lhsNum);
    }
    return false;
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
//  Return Value:
//      Skip sub trees if the optimization candidate is identified or else continue walking
//
Compiler::fgWalkResult Compiler::optCanOptimizeByLoopCloning(GenTree* tree, LoopCloneVisitorInfo* info)
{
    ArrIndex arrIndex(getAllocator(CMK_LoopClone));

    // Check if array index can be optimized.
    if (optReconstructArrIndex(tree, &arrIndex, BAD_VAR_NUM))
    {
        assert(tree->gtOper == GT_COMMA);

#ifdef DEBUG
        if (verbose)
        {
            printf("Found ArrIndex at tree ");
            printTreeID(tree);
            printf(" which is equivalent to: ");
            arrIndex.Print();
            printf("\n");
        }
#endif

        // Check that the array object local variable is invariant within the loop body.
        if (!optIsStackLocalInvariant(info->loopNum, arrIndex.arrLcl))
        {
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
    else if (tree->gtOper == GT_ARR_ELEM)
    {
        // TODO-CQ: CLONE: Implement.
        return WALK_SKIP_SUBTREES;
    }
    return WALK_CONTINUE;
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
    noway_assert(loopNum < optLoopCount);

    LoopDsc* pLoop = &optLoopTable[loopNum];

    BasicBlock* head = pLoop->lpHead;
    BasicBlock* beg  = head->bbNext; // should this be pLoop->lpFirst or pLoop->lpTop instead?
    BasicBlock* end  = pLoop->lpBottom;

    JITDUMP("Checking blocks " FMT_BB ".." FMT_BB " for optimization candidates\n", beg->bbNum, end->bbNum);

    LoopCloneVisitorInfo info(context, loopNum, nullptr);
    for (BasicBlock* block = beg; block != end->bbNext; block = block->bbNext)
    {
        compCurBB = block;
        for (Statement* stmt : block->Statements())
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
// in release builds. For debug builds, use the value of the COMPlus_JitCloneLoops
// flag (which defaults to 1, or allowed).
//
// Return Value:
//      true if loop cloning is allowed, false if disallowed.
//
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

#ifdef DEBUG
    if (verbose)
    {
        printf("\nBefore loop cloning:\n");
        fgDispBasicBlocks(/*dumpTrees*/ true);
    }
#endif

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
            if (allTrue)
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
        constexpr bool computePreds = false;
        fgUpdateChangedFlowGraph(computePreds);
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("Loops cloned: %d\n", optLoopsCloned);
        printf("Loops statically optimized: %d\n", optStaticallyOptimizedLoops);
        printf("After loop cloning:\n");
        fgDispBasicBlocks(/*dumpTrees*/ true);
    }

    fgDebugCheckLoopTable();
#endif

    return PhaseStatus::MODIFIED_EVERYTHING;
}
