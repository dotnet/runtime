// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                               Lower                                       XX
XX                                                                           XX
XX  Preconditions:                                                           XX
XX                                                                           XX
XX  Postconditions (for the nodes currently handled):                        XX
XX    - All operands requiring a register are explicit in the graph          XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifndef LEGACY_BACKEND // This file is ONLY used for the RyuJIT backend that uses the linear scan register allocator

#include "lower.h"

//------------------------------------------------------------------------
// MakeSrcContained: Make "tree" a contained node
//
// Arguments:
//    'parentNode' is a non-leaf node that can contain its 'childNode' 
//    'childNode' is an op that will now be contained by its parent.
//
// Notes:
//    If 'childNode' it has any existing sources, they will now be sources for the parent.
//
void Lowering::MakeSrcContained(GenTreePtr parentNode, GenTreePtr childNode)
{
    assert(!parentNode->OperIsLeaf());
    int srcCount = childNode->gtLsraInfo.srcCount;
    assert(srcCount >= 0);
    m_lsra->clearOperandCounts(childNode);
    assert(parentNode->gtLsraInfo.srcCount > 0);
    parentNode->gtLsraInfo.srcCount += srcCount - 1;
}

//------------------------------------------------------------------------
// CheckImmedAndMakeContained: Check and make 'childNode' contained
// Arguments:
//    'parentNode' is any non-leaf node 
//    'childNode' is an child op of 'parentNode'
// Return value:
//     returns true if we are able to make childNode contained immediate
//
// Notes:
//    Checks if the 'childNode' is a containable immediate 
//    and then makes it contained
//
bool Lowering::CheckImmedAndMakeContained(GenTree* parentNode, GenTree* childNode)
{
    assert(!parentNode->OperIsLeaf());
    // If childNode is a containable immediate
    if (IsContainableImmed(parentNode, childNode))
    {
        // then make it contained within the parentNode
        MakeSrcContained(parentNode, childNode);
        return true;
    }
    return false;
}

//------------------------------------------------------------------------
// IsSafeToContainMem: Checks for conflicts between childNode and parentNode.
//
// Arguments:
//    parentNode  - a non-leaf binary node
//    childNode   - a memory op that is a child op of 'parentNode'
//
// Return value:
//    returns true if it is safe to make childNode a contained memory op
//
// Notes:
//    Checks for memory conflicts in the instructions between childNode and parentNode,
//    and returns true iff childNode can be contained.

bool Lowering::IsSafeToContainMem(GenTree* parentNode, GenTree* childNode)
{
    assert(parentNode->OperIsBinary());
    assert(childNode->isMemoryOp());

    // Check conflicts against nodes between 'childNode' and 'parentNode'
    GenTree* node;
    unsigned int childFlags = (childNode->gtFlags & GTF_ALL_EFFECT);
    for (node = childNode->gtNext;
         (node != parentNode) && (node != nullptr);
         node = node->gtNext)
    {
        if ((childFlags != 0) && node->IsCall())
        {
            bool isPureHelper = (node->gtCall.gtCallType == CT_HELPER) && comp->s_helperCallProperties.IsPure(comp->eeGetHelperNum(node->gtCall.gtCallMethHnd));
            if (!isPureHelper && ((node->gtFlags & childFlags & GTF_ALL_EFFECT) != 0))
            {
                return false;
            }
        }
        else if (node->OperIsStore() && comp->fgNodesMayInterfere(node, childNode))
        {
            return false;
        }
    }
    if (node != parentNode)
    {
        assert(!"Ran off end of stmt\n");
        return false;
    }
    return true;
}

//------------------------------------------------------------------------

Compiler::fgWalkResult Lowering::DecompNodeHelper(GenTreePtr* pTree, Compiler::fgWalkData* data)
{
    Lowering* lower = (Lowering*)data->pCallbackData;
    lower->DecomposeNode(pTree, data);
    return Compiler::WALK_CONTINUE;
}

Compiler::fgWalkResult Lowering::LowerNodeHelper(GenTreePtr* pTree, Compiler::fgWalkData* data)
{
    Lowering* lower = (Lowering*)data->pCallbackData;
    lower->LowerNode(pTree, data);
    return Compiler::WALK_CONTINUE;
}

#if !defined(_TARGET_64BIT_)
genTreeOps getHiOper(genTreeOps oper)
{
    switch(oper)
    {
    case GT_ADD: return GT_ADD_HI;  break;
    case GT_SUB: return GT_SUB_HI;  break;
    case GT_MUL: return GT_MUL_HI;  break;
    case GT_DIV: return GT_DIV_HI;  break;
    case GT_MOD: return GT_MOD_HI;  break;
    case GT_OR:  return GT_OR;      break;
    case GT_AND: return GT_AND;     break;
    case GT_XOR: return GT_XOR;     break;
    default:
        assert(!"getHiOper called for invalid oper");
        return GT_NONE;
    }
}
#endif // !defined(_TARGET_64BIT_)

//------------------------------------------------------------------------
// DecomposeNode: Decompose long-type trees into lower & upper halves.
//
// Arguments:
//    *pTree - A node that may or may not require decomposition.
//    data   - The tree-walk data that provides the context.
//
// Return Value:
//    None.
//
// Notes: The rationale behind this is to avoid adding code complexity
// downstream caused by the introduction of handling longs as special cases, especially in
// LSRA.
// This function is called just prior to the more general purpose lowering.
//
void Lowering::DecomposeNode(GenTreePtr* pTree, Compiler::fgWalkData* data)
{
#if !defined(_TARGET_64BIT_)
    GenTree* tree = *(pTree);

    // Handle the case where we are implicitly using the lower half of a long lclVar.
    if ((tree->TypeGet() == TYP_INT) && tree->OperIsLocal())
    {
        LclVarDsc *  varDsc = comp->lvaTable + tree->AsLclVarCommon()->gtLclNum;
        if (varTypeIsLong(varDsc) && varDsc->lvPromoted)
        {
#ifdef DEBUG
            if (comp->verbose)
            {
                printf("Changing implicit reference to lo half of long lclVar to an explicit reference of its promoted half:\n");
                comp->gtDispTree(tree);
            }
#endif
            comp->lvaDecRefCnts(tree);
            unsigned loVarNum = varDsc->lvFieldLclStart;
            tree->AsLclVarCommon()->SetLclNum(loVarNum);
            comp->lvaIncRefCnts(tree);
            return;
        }
    }
    if (tree->TypeGet() != TYP_LONG)
    {
        return;
    }
#ifdef DEBUG
    if (comp->verbose)
    {
        printf("Decomposing TYP_LONG tree.  BEFORE:\n");
        comp->gtDispTree(tree);
    }
#endif
    // The most common pattern is that we will create a loResult and a hiResult, where
    // the loResult reuses some or all of the existing tree, and the hiResult is new.
    // For these, we will set loResult and hiResult in the switch case below, ensuring
    // that loResult is correctly linked into the statement, and hiResult is not, but
    // is internally sequenced appropriately (if it is not a single node).
    // Common code after the switch will link in hiResult and create a GT_LONG node.

    GenTree* newTree = nullptr;
    GenTree* loResult = nullptr;
    GenTree* hiResult = nullptr;
    GenTreeStmt* curStmt = comp->compCurStmt->AsStmt();
    genTreeOps oper = tree->OperGet();
    switch(oper)
    {
    case GT_PHI:
    case GT_PHI_ARG:
        break;
    case GT_LCL_VAR:
        {
            loResult = tree;
            unsigned varNum = tree->AsLclVarCommon()->gtLclNum;
            LclVarDsc *  varDsc = comp->lvaTable + varNum;
            comp->lvaDecRefCnts(tree);
            hiResult = comp->gtNewLclLNode(varNum, TYP_INT);

            if (varDsc->lvPromoted)
            {
                assert(varDsc->lvFieldCnt == 2);
                unsigned loVarNum = varDsc->lvFieldLclStart;
                unsigned hiVarNum = loVarNum + 1;
                tree->AsLclVarCommon()->SetLclNum(loVarNum);
                hiResult->AsLclVarCommon()->SetLclNum(hiVarNum);
            }
            else
            {
                noway_assert(varDsc->lvLRACandidate == false);
                tree->SetOper(GT_LCL_FLD);
                tree->AsLclFld()->gtLclOffs = 0;
                tree->AsLclFld()->gtFieldSeq = FieldSeqStore::NotAField();
                hiResult->SetOper(GT_LCL_FLD);
                hiResult->AsLclFld()->gtLclOffs = 4;
                hiResult->AsLclFld()->gtFieldSeq = FieldSeqStore::NotAField();
            }
            tree->gtType = TYP_INT;

            comp->lvaIncRefCnts(loResult);
            comp->lvaIncRefCnts(hiResult);
            break;
        }
    case GT_LCL_FLD:
        loResult = tree;
        loResult->gtType = TYP_INT;

        hiResult = comp->gtNewLclFldNode(loResult->AsLclFld()->gtLclNum,
                                         TYP_INT,
                                         loResult->AsLclFld()->gtLclOffs + 4);
        break;
    case GT_STORE_LCL_VAR:
        {
            GenTree* nextTree = tree->gtNext;
            GenTree* rhs = tree->gtGetOp1();
            if (rhs->OperGet() == GT_PHI)
            {
                break;
            }
            noway_assert(rhs->OperGet() == GT_LONG);
            unsigned varNum = tree->AsLclVarCommon()->gtLclNum;
            LclVarDsc *  varDsc = comp->lvaTable + varNum;
            comp->lvaDecRefCnts(tree);
            GenTree* loRhs = rhs->gtGetOp1();
            GenTree* hiRhs = rhs->gtGetOp2();
            GenTree* hiStore = comp->gtNewLclLNode(varNum, TYP_INT);
            if (varDsc->lvPromoted)
            {
                assert(varDsc->lvFieldCnt == 2);
                unsigned loVarNum = varDsc->lvFieldLclStart;
                unsigned hiVarNum = loVarNum + 1;
                tree->AsLclVarCommon()->SetLclNum(loVarNum);
                hiStore->SetOper(GT_STORE_LCL_VAR);
                hiStore->AsLclVarCommon()->SetLclNum(hiVarNum);
            }
            else
            {
                noway_assert(varDsc->lvLRACandidate == false);
                tree->SetOper(GT_STORE_LCL_FLD);
                tree->AsLclFld()->gtLclOffs = 0;
                tree->AsLclFld()->gtFieldSeq = FieldSeqStore::NotAField();
                hiStore->SetOper(GT_STORE_LCL_FLD);
                hiStore->AsLclFld()->gtLclOffs = 4;
                hiStore->AsLclFld()->gtFieldSeq = FieldSeqStore::NotAField();
            }
            tree->gtOp.gtOp1 = loRhs;
            tree->gtType = TYP_INT;

            loRhs->gtNext = tree;
            tree->gtPrev = loRhs;

            hiStore->gtOp.gtOp1 = hiRhs;
            hiStore->CopyCosts(tree);
            hiStore->gtFlags |= GTF_VAR_DEF;

            comp->lvaIncRefCnts(tree);
            comp->lvaIncRefCnts(hiStore);

            hiRhs->gtPrev = tree;
            hiRhs->gtNext = hiStore;
            hiStore->gtPrev = hiRhs;
            hiStore->gtNext = nextTree;
            if (nextTree != nullptr)
            {
                nextTree->gtPrev = hiStore;
            }
            nextTree = hiRhs;
            GenTreeStmt* stmt;
            if (comp->compCurStmt->gtStmt.gtStmtExpr == tree)
            {
                tree->gtNext = nullptr;
                hiRhs->gtPrev = nullptr;
                stmt = comp->fgNewStmtFromTree(hiStore);
                comp->fgInsertStmtAfter(comp->compCurBB, comp->compCurStmt, stmt);
            }
            else
            {
                stmt = comp->fgMakeEmbeddedStmt(comp->compCurBB, hiStore, comp->compCurStmt);
            }
            stmt->gtStmtILoffsx = comp->compCurStmt->gtStmt.gtStmtILoffsx;
#ifdef DEBUG
            stmt->gtStmtLastILoffs = comp->compCurStmt->gtStmt.gtStmtLastILoffs;
#endif // DEBUG
        }
        break;
    case GT_CAST:
        {
            assert(tree->gtPrev == tree->gtGetOp1());
            NYI_IF(tree->gtOverflow(), "TYP_LONG cast with overflow");
            switch (tree->AsCast()->CastFromType())
            {
            case TYP_INT:
                loResult = tree->gtGetOp1();
                hiResult = new (comp, GT_CNS_INT) GenTreeIntCon(TYP_INT, 0);
                comp->fgSnipNode(curStmt, tree);
                break;
            default:
                NYI("Unimplemented type for Lowering of cast to TYP_LONG");
                break;
            }
            break;
        }
    case GT_CNS_LNG:
        {
            INT32 hiVal = tree->AsLngCon()->HiVal();

            loResult = tree;
            loResult->ChangeOperConst(GT_CNS_INT);
            loResult->gtType = TYP_INT;

            hiResult = new (comp, GT_CNS_INT) GenTreeIntCon(TYP_INT, hiVal);
            hiResult->CopyCosts(tree);
        }
        break;
    case GT_CALL:
        NYI("Call with TYP_LONG return value");
        break;
    case GT_RETURN:
        assert(tree->gtOp.gtOp1->OperGet() == GT_LONG);
        break;
    case GT_STOREIND:
        assert(tree->gtOp.gtOp2->OperGet() == GT_LONG);
        NYI("StoreInd of of TYP_LONG");
        break;
    case GT_STORE_LCL_FLD:
        assert(tree->gtOp.gtOp1->OperGet() == GT_LONG);
        NYI("st.lclFld of of TYP_LONG");
        break;
    case GT_IND:
        NYI("GT_IND of TYP_LONG");
        break;
    case GT_NOT:
        {
            GenTree* op1 = tree->gtGetOp1();
            noway_assert(op1->OperGet() == GT_LONG);
            GenTree* loOp1 = op1->gtGetOp1();
            GenTree* hiOp1 = op1->gtGetOp2();
            comp->fgSnipNode(curStmt, op1);
            loResult = tree;
            loResult->gtType = TYP_INT;
            loResult->gtOp.gtOp1 = loOp1;
            loOp1->gtNext = loResult;
            loResult->gtPrev = loOp1;

            hiResult = new (comp, oper) GenTreeOp(oper, TYP_INT, hiOp1, nullptr);
            hiOp1->gtNext = hiResult;
            hiResult->gtPrev = hiOp1;
        }
        break;
    case GT_NEG:
        NYI("GT_NEG of TYP_LONG");
        break;
    // Binary operators. Those that require different computation for upper and lower half are
    // handled by the use of getHiOper().
    case GT_ADD:
    case GT_OR:
    case GT_XOR:
    case GT_AND:
        {
            NYI_IF((tree->gtFlags & GTF_REVERSE_OPS) != 0, "Binary operator with GTF_REVERSE_OPS");
            GenTree* op1 = tree->gtGetOp1();
            GenTree* op2 = tree->gtGetOp2();
            // Both operands must have already been decomposed into GT_LONG operators.
            noway_assert((op1->OperGet() == GT_LONG) && (op2->OperGet() == GT_LONG));
            // Capture the lo and hi halves of op1 and op2.
            GenTree* loOp1 = op1->gtGetOp1();
            GenTree* hiOp1 = op1->gtGetOp2();
            GenTree* loOp2 = op2->gtGetOp1();
            GenTree* hiOp2 = op2->gtGetOp2();

            // Now, remove op1 and op2 from the node list.
            comp->fgSnipNode(curStmt, op1);
            comp->fgSnipNode(curStmt, op2);

            // We will reuse "tree" for the loResult, which will now be of TYP_INT, and its operands
            // will be the lo halves of op1 from above.
            loResult = tree;
            loResult->gtType = TYP_INT;
            loResult->gtOp.gtOp1 = loOp1;
            loResult->gtOp.gtOp2 = loOp2;

            // The various halves will be correctly threaded internally. We simply need to
            // relink them into the proper order, i.e. loOp1 is followed by loOp2, and then
            // the loResult node.
            // (This rethreading, and that below, are where we need to address the reverse ops case).
            // The current order is (after snipping op1 and op2):
            // ... loOp1-> ... hiOp1->loOp2First ... loOp2->hiOp2First ... hiOp2
            // The order we want is:
            // ... loOp1->loOp2First ... loOp2->loResult
            // ... hiOp1->hiOp2First ... hiOp2->hiResult
            // i.e. we swap hiOp1 and loOp2, and create (for now) separate loResult and hiResult trees
            GenTree* loOp2First = hiOp1->gtNext;
            GenTree* hiOp2First = loOp2->gtNext;

            // First, we will NYI if both hiOp1 and loOp2 have side effects.
            NYI_IF(((loOp2->gtFlags & GTF_ALL_EFFECT) != 0) && ((hiOp1->gtFlags & GTF_ALL_EFFECT) != 0),
                   "Binary long operator with non-reorderable sub expressions");

            // Now, we reorder the loOps and the loResult.
            loOp1->gtNext      = loOp2First;
            loOp2First->gtPrev = loOp1;
            loOp2->gtNext      = loResult;
            loResult->gtPrev   = loOp2;

            // Next, reorder the hiOps and the hiResult.
            hiResult = new (comp, oper) GenTreeOp(getHiOper(oper), TYP_INT, hiOp1, hiOp2);
            hiOp1->gtNext      = hiOp2First;
            hiOp2First->gtPrev = hiOp1;
            hiOp2->gtNext      = hiResult;
            hiResult->gtPrev   = hiOp2;

            // Below, we'll put the loResult and hiResult trees together, using the more
            // general fgInsertTreeInListAfter() method.
        }
        break;
    case GT_SUB:
    case GT_MUL:
    case GT_DIV:
    case GT_MOD:
    case GT_UDIV:
    case GT_UMOD:
    case GT_LSH:
    case GT_RSH:
    case GT_RSZ:
    case GT_ROL:
    case GT_ROR:
    case GT_MULHI:
        NYI("Arithmetic binary operators on TYP_LONG");
        break;
    case GT_LOCKADD:
    case GT_XADD:
    case GT_XCHG:
    case GT_CMPXCHG:
        NYI("Interlocked operations on TYP_LONG");
        break;
    default:
        {
            JITDUMP("Illegal TYP_LONG node %s in Lowering.", GenTree::NodeName(tree->OperGet()));
            noway_assert(!"Illegal TYP_LONG node in Lowering.");
            break;
        }
    }
    if (loResult != nullptr)
    {
        noway_assert(hiResult != nullptr);
        comp->fgInsertTreeInListAfter(hiResult, loResult, curStmt);
        hiResult->CopyCosts(tree);

        newTree = new (comp, GT_LONG) GenTreeOp(GT_LONG, TYP_LONG, loResult, hiResult);
        SimpleLinkNodeAfter(hiResult, newTree);
    }
    if (newTree != nullptr)
    {
        comp->fgFixupIfCallArg(data->parentStack, tree, newTree);
        newTree->CopyCosts(tree);
        *pTree = newTree;
    }
#ifdef DEBUG
    if (comp->verbose)
    {
        printf("  AFTER:\n");
        comp->gtDispTree(*pTree);
    }
#endif
#endif // //_TARGET_64BIT_
}

/** Creates an assignment of an existing tree to a new temporary local variable
 * and the specified reference count for the new variable.
 */
GenTreePtr Lowering::CreateLocalTempAsg(GenTreePtr rhs,
                                        unsigned refCount,
                                        GenTreePtr* ppLclVar) //out legacy arg
{
    unsigned lclNum = comp->lvaGrabTemp(true DEBUGARG("Lowering is creating a new local variable"));
    comp->lvaSortAgain = true;
    comp->lvaTable[lclNum].lvType = rhs->TypeGet();

    // Make sure we don't lose precision when downgrading to short
    noway_assert(FitsIn<short>(refCount));
    comp->lvaTable[lclNum].lvRefCnt = (short)(refCount);
    JITDUMP("Lowering has requested a new temporary local variable: V%02u with refCount %u \n", lclNum, refCount);

    GenTreeLclVar* store = new(comp, GT_STORE_LCL_VAR) GenTreeLclVar(GT_STORE_LCL_VAR, rhs->TypeGet(), lclNum, BAD_IL_OFFSET);
    store->gtOp1 = rhs;
    store->gtFlags = (rhs->gtFlags & GTF_COMMON_MASK);
    store->gtFlags |= GTF_VAR_DEF;
    return store;
}

// This is the main entry point for Lowering.  

// In addition to that, LowerNode is also responsible for initializing the
// treeNodeMap data structure consumed by LSRA.  This map is a 1:1 mapping between
// expression trees and TreeNodeInfo structs.  Currently, Lowering initializes
// treeNodeMap with new instances of TreeNodeInfo for each tree and also annotates them
// with the register requirements needed for each tree.
// We receive a double pointer to a tree in order to be able, if needed, to entirely
// replace the tree by creating a new one and updating the underying pointer so this
// enables in-place tree manipulation.
// The current design is made in such a way we perform a helper call for each different
// type of tree.  Currently, the only supported node is GT_IND and for that we call the
// LowerInd private method.  The build system picks up the appropiate Lower.cpp (either
// LowerArm/LowerX86/LowerAMD64) that has the machine dependent logic to lower each node.
// TODO-Throughput: Modify post-order traversal to propagate parent info OR
// implement child iterator directly on GenTree, so that we can
// lower in-place.
void Lowering::LowerNode(GenTreePtr* ppTree, Compiler::fgWalkData* data)
{
    // First, lower any child nodes (done via post-order walk)
    assert(ppTree);
    assert(*ppTree);
    switch ((*ppTree)->gtOper)
    {
    case GT_IND:
    case GT_STOREIND:
        LowerInd(ppTree);
        break;

    case GT_ADD:
        LowerAdd(ppTree, data);
        break;
        
    case GT_SWITCH:
        LowerSwitch(ppTree);
        break;

    case GT_CALL:
        LowerCall(*ppTree);
        break;

    case GT_JMP:
        LowerJmpMethod(*ppTree);
        break;

    case GT_RETURN:
        LowerRet(*ppTree);
        break;

    case GT_CAST:
        LowerCast(ppTree);
        break;

    case GT_ARR_ELEM:
        {
            GenTree* oldTree = *ppTree;
            LowerArrElem(ppTree, data);
            comp->fgFixupIfCallArg(data->parentStack, oldTree, *ppTree);
        }
        break;

    case GT_ROL:
    case GT_ROR:
        LowerRotate(*ppTree);
        break;

#ifdef FEATURE_SIMD
    case GT_SIMD:
        if ((*ppTree)->TypeGet() == TYP_SIMD12)
        {
            // GT_SIMD node requiring to produce TYP_SIMD12 in fact
            // produces a TYP_SIMD16 result
            (*ppTree)->gtType = TYP_SIMD16;
        }
        break;

    case GT_LCL_VAR:
    case GT_STORE_LCL_VAR:
        if ((*ppTree)->TypeGet() == TYP_SIMD12)
        {
#ifdef _TARGET_64BIT_
            // Assumption 1:
            // RyuJit backend depends on the assumption that on 64-Bit targets Vector3 size is rounded off
            // to TARGET_POINTER_SIZE and hence Vector3 locals on stack can be treated as TYP_SIMD16 for
            // reading and writing purposes. 
            //
            // Assumption 2:
            // RyuJit backend is making another implicit assumption that Vector3 type args when passed in
            // registers or on stack, the upper most 4-bytes will be zero.  
            //
            // TODO-64bit: assumptions 1 and 2 hold within RyuJIT generated code. It is not clear whether
            // these assumptions hold when a Vector3 type arg is passed by native code. Example: PInvoke
            // returning Vector3 type value or RPInvoke passing Vector3 type args.
            (*ppTree)->gtType = TYP_SIMD16;
#else
            NYI("Lowering of TYP_SIMD12 locals");
#endif // _TARGET_64BIT_
        }
#endif //FEATURE_SIMD

    default:
        return;
    }
}

/**  -- Switch Lowering --
 * The main idea of switch lowering is to keep transparency of the register requirements of this node
 * downstream in LSRA.  Given that the switch instruction is inherently a control statement which in the JIT
 * is represented as a simple tree node, at the time we actually generate code for it we end up
 * generating instructions that actually modify the flow of execution that imposes complicated
 * register requirement and lifetimes.
 *
 * So, for the purpose of LSRA, we want to have a more detailed specification of what a switch node actually
 * means and more importantly, which and when do we need a register for each instruction we want to issue
 * to correctly allocate them downstream.
 *
 * For this purpose, this procedure performs switch lowering in two different ways:
 *
 * a) Represent the switch statement as a zero-index jump table construct.  This means that for every destination
 *    of the switch, we will store this destination in an array of addresses and the code generator will issue
 *    a data section where this array will live and will emit code that based on the switch index, will indirect and
 *    jump to the destination specified in the jump table.
 *
 *    For this transformation we introduce a new GT node called GT_SWITCH_TABLE that is a specialization of the switch node
 *    for jump table based switches.
 *    The overall structure of a GT_SWITCH_TABLE is:
 *
 *    GT_SWITCH_TABLE
 *           |_________ localVar   (a temporary local that holds the switch index)
 *           |_________ jumpTable  (this is a special node that holds the address of the jump table array)
 *
 *     Now, the way we morph a GT_SWITCH node into this lowered switch table node form is the following:
 *
 *    Input:     GT_SWITCH (inside a basic block whose Branch Type is BBJ_SWITCH)
 *                    |_____ expr (an arbitrary complex GT_NODE that represents the switch index)
 *
 *    This gets transformed into the following statements inside a BBJ_COND basic block (the target would be
 *    the default case of the switch in case the conditional is evaluated to true).
 *
 *     GT_ASG
 *        |_____ tempLocal (a new temporary local variable used to store the switch index)
 *        |_____ expr      (the index expression)
 *
 *     GT_JTRUE
 *        |_____ GT_COND
 *                 |_____ GT_GE
 *                           |___ Int_Constant  (This constant is the index of the default case
 *                                               that happens to be the highest index in the jump table).
 *                           |___ tempLocal     (The local variable were we stored the index expression).
 *
 *     GT_SWITCH_TABLE
 *        |_____ tempLocal
 *        |_____ jumpTable (a new jump table node that now LSRA can allocate registers for explicitly
 *                          and LinearCodeGen will be responsible to generate downstream).
 *
 *     This way there are no implicit temporaries.
 *
 * b) For small-sized switches, we will actually morph them into a series of conditionals of the form
 *     if (case falls into the default){ goto jumpTable[size]; // last entry in the jump table is the default case }
 *     (For the default case conditional, we'll be constructing the exact same code as the jump table case one).
 *     else if (case == firstCase){ goto jumpTable[1]; }
 *     else if (case == secondCase) { goto jumptable[2]; } and so on.
 *
 *     This transformation is of course made in JIT-IR, not downstream to CodeGen level, so this way we no longer require
 *     internal temporaries to maintain the index we're evaluating plus we're using existing code from LinearCodeGen
 *     to implement this instead of implement all the control flow constructs using InstrDscs and InstrGroups downstream.
 */
void Lowering::LowerSwitch(GenTreePtr* pTree)
{
    unsigned     jumpCnt;
    unsigned     targetCnt;
    BasicBlock** jumpTab;
    GenTreePtr   tree = *pTree;

    assert(tree->gtOper == GT_SWITCH);

    // The first step is to build the default case conditional construct that is
    // shared between both kinds of expansion of the switch node.

    // To avoid confusion, we'll alias compCurBB to originalSwitchBB
    // that represents the node we're morphing.
    BasicBlock* originalSwitchBB = comp->compCurBB;

    // jumpCnt is the number of elements in the jump table array.
    // jumpTab is the actual pointer to the jump table array.
    // targetCnt is the number of unique targets in the jump table array.
    jumpCnt = originalSwitchBB->bbJumpSwt->bbsCount;
    jumpTab = originalSwitchBB->bbJumpSwt->bbsDstTab;
    targetCnt = originalSwitchBB->NumSucc(comp);

    JITDUMP("Lowering switch BB%02u, %d cases\n", originalSwitchBB->bbNum, jumpCnt);

    // Handle a degenerate case: if the switch has only a default case, just convert it
    // to an unconditional branch. This should only happen in minopts or with debuggable
    // code.
    if (targetCnt == 1)
    {
        JITDUMP("Lowering switch BB%02u: single target; converting to BBJ_ALWAYS\n", originalSwitchBB->bbNum);
        noway_assert(comp->opts.MinOpts() || comp->opts.compDbgCode);
        if (originalSwitchBB->bbNext == jumpTab[0])
        {
            originalSwitchBB->bbJumpKind = BBJ_NONE;
            originalSwitchBB->bbJumpDest = nullptr;
        }
        else
        {
            originalSwitchBB->bbJumpKind = BBJ_ALWAYS;
            originalSwitchBB->bbJumpDest = jumpTab[0];
        }
        // Remove extra predecessor links if there was more than one case.
        for (unsigned i = 1; i < jumpCnt; ++i)
        {
            (void) comp->fgRemoveRefPred(jumpTab[i], originalSwitchBB);
        }
        // We have to get rid of the GT_SWITCH node but a child might have side effects so just assign 
        // the result of the child subtree to a temp.
        GenTree* store = CreateLocalTempAsg(tree->gtOp.gtOp1, 1);
        tree->InsertAfterSelf(store, comp->compCurStmt->AsStmt());
        Compiler::fgSnipNode(comp->compCurStmt->AsStmt(), tree);
        *pTree = store;
        
        return;
    }

    noway_assert(jumpCnt >= 2);

    // Split the switch node to insert an assignment to a temporary variable.
    // Note that 'tree' is the GT_SWITCH, and its op1 may be overwritten by SplitTree
    //
    GenTreeStmt* asgStmt = comp->fgInsertEmbeddedFormTemp(&(tree->gtOp.gtOp1));

    // GT_SWITCH(indexExpression) is now two statements:
    //   1. a statement containing 'asg' (for temp = indexExpression) 
    //   2. and a statement with GT_SWITCH(temp)

    // The return value of fgInsertEmbeddedFormTemp is stmt 1 
    // The 'asg' can either be a GT_ASG or a GT_STORE_LCL_VAR
    // 'tree' is still a GT_SWITCH but tree->gtOp.gtOp1 is modified to be 'temp'
     
    // The asgStmt needs to pickup the IL offsets from the current statement
    //
    asgStmt->gtStmtILoffsx = comp->compCurStmt->gtStmt.gtStmtILoffsx;
#ifdef DEBUG
    asgStmt->gtStmtLastILoffs = comp->compCurStmt->gtStmt.gtStmtLastILoffs;
#endif // DEBUG

    assert(tree->gtOper == GT_SWITCH);
    GenTreePtr temp = tree->gtOp.gtOp1;
    assert(temp->gtOper == GT_LCL_VAR);
    unsigned tempLclNum = temp->gtLclVarCommon.gtLclNum;
    LclVarDsc *  tempVarDsc = comp->lvaTable + tempLclNum;
    var_types tempLclType = tempVarDsc->TypeGet();

    BasicBlock* defaultBB = jumpTab[jumpCnt - 1];
    BasicBlock* followingBB = originalSwitchBB->bbNext;

    /* Is the number of cases right for a test and jump switch? */
    const bool fFirstCaseFollows = (followingBB == jumpTab[0]);
    const bool fDefaultFollows   = (followingBB == defaultBB);

    unsigned minSwitchTabJumpCnt = 2; // table is better than just 2 cmp/jcc

    // This means really just a single cmp/jcc (aka a simple if/else)
    if (fFirstCaseFollows || fDefaultFollows)
        minSwitchTabJumpCnt++;

#if defined(_TARGET_ARM_)
    // On ARM for small switch tables we will
    // generate a sequence of compare and branch instructions
    // because the code to load the base of the switch
    // table is huge and hideous due to the relocation... :(
    minSwitchTabJumpCnt += 2;
#elif defined(_TARGET_ARM64_) // _TARGET_ARM_
    // In the case of ARM64 we'll stick to generate a sequence of
    // compare and branch for now to get switch working and revisit
    // to implement jump tables in the future.
    //
    // TODO-AMD64-NYI: Implement Jump Tables.
    minSwitchTabJumpCnt = -1; 
#endif // _TARGET_ARM64_
    // Once we have the temporary variable, we construct the conditional branch for
    // the default case.  As stated above, this conditional is being shared between
    // both GT_SWITCH lowering code paths.
    // This condition is of the form: if (temp > jumpTableLength - 2){ goto jumpTable[jumpTableLength - 1]; }
    GenTreePtr gtDefaultCaseCond = comp->gtNewOperNode(GT_GT, TYP_INT,
                                                       comp->gtNewLclvNode(tempLclNum, tempLclType),
                                                       comp->gtNewIconNode(jumpCnt - 2, TYP_INT));
    //
    // Make sure we perform an unsigned comparison, just in case the switch index in 'temp' 
    // is now less than zero 0 (that would also hit the default case).
    gtDefaultCaseCond->gtFlags |= GTF_UNSIGNED;

    /* Increment the lvRefCnt and lvRefCntWtd for temp */
    tempVarDsc->incRefCnts(originalSwitchBB->getBBWeight(comp), comp);

    GenTreePtr gtDefaultCaseJump = comp->gtNewOperNode(GT_JTRUE,
                                                       TYP_VOID,
                                                       gtDefaultCaseCond);
    gtDefaultCaseJump->gtFlags = tree->gtFlags;

    GenTreePtr condStmt = comp->fgNewStmtFromTree(gtDefaultCaseJump, originalSwitchBB, comp->compCurStmt->gtStmt.gtStmtILoffsx);

#ifdef DEBUG
    condStmt->gtStmt.gtStmtLastILoffs = comp->compCurStmt->gtStmt.gtStmtLastILoffs;
#endif // DEBUG

    comp->fgInsertStmtAfter(originalSwitchBB, comp->compCurStmt, condStmt);

    BasicBlock* afterDefCondBlock = comp->fgSplitBlockAfterStatement(originalSwitchBB, condStmt);

    // afterDefCondBlock is now the switch, and all the switch targets have it as a predecessor.
    // originalSwitchBB is now a BBJ_NONE, and there is a predecessor edge in afterDefCondBlock
    // representing the fall-through flow from originalSwitchBB.
    assert(originalSwitchBB->bbJumpKind == BBJ_NONE);
    assert(afterDefCondBlock->bbJumpKind == BBJ_SWITCH);
    assert(afterDefCondBlock->bbJumpSwt->bbsHasDefault);

    // Turn originalSwitchBB into a BBJ_COND.
    originalSwitchBB->bbJumpKind = BBJ_COND;
    originalSwitchBB->bbJumpDest = jumpTab[jumpCnt - 1];

    // Fix the pred for the default case: the default block target still has originalSwitchBB
    // as a predecessor, but the fgSplitBlockAfterStatement() moved all predecessors to point
    // to afterDefCondBlock.
    flowList* oldEdge = comp->fgRemoveRefPred(jumpTab[jumpCnt - 1], afterDefCondBlock);
    comp->fgAddRefPred(jumpTab[jumpCnt - 1], originalSwitchBB, oldEdge);

    // If we originally had 2 unique successors, check to see whether there is a unique
    // non-default case, in which case we can eliminate the switch altogether.
    // Note that the single unique successor case is handled above.
    BasicBlock* uniqueSucc = nullptr;
    if (targetCnt == 2)
    {
        uniqueSucc = jumpTab[0];
        noway_assert(jumpCnt >= 2);
        for (unsigned i = 1; i < jumpCnt - 1; i++)
        {
            if (jumpTab[i] != uniqueSucc)
            {
                uniqueSucc = nullptr;
                break;
            }
        }
    }
    if (uniqueSucc != nullptr)
    {
        // If the unique successor immediately follows this block, we have nothing to do -
        // it will simply fall-through after we remove the switch, below.
        // Otherwise, make this a BBJ_ALWAYS.
        // Now, fixup the predecessor links to uniqueSucc.  In the original jumpTab:
        //   jumpTab[i-1] was the default target, which we handled above,
        //   jumpTab[0] is the first target, and we'll leave that predecessor link.
        // Remove any additional predecessor links to uniqueSucc.
        for (unsigned i = 1; i < jumpCnt - 1; ++i)
        {
            assert(jumpTab[i] == uniqueSucc);
            (void) comp->fgRemoveRefPred(uniqueSucc, afterDefCondBlock);
        }
        if (afterDefCondBlock->bbNext == uniqueSucc)
        {
            afterDefCondBlock->bbJumpKind = BBJ_NONE;
            afterDefCondBlock->bbJumpDest = nullptr;
        }
        else
        {
            afterDefCondBlock->bbJumpKind = BBJ_ALWAYS;
            afterDefCondBlock->bbJumpDest = uniqueSucc;
        }
    }
    // If the number of possible destinations is small enough, we proceed to expand the switch
    // into a series of conditional branches, otherwise we follow the jump table based switch
    // transformation.
    else if (jumpCnt < minSwitchTabJumpCnt)
    {
        // Lower the switch into a series of compare and branch IR trees.
        //
        // In this case we will morph the tree in the following way:
        // 1. Generate a JTRUE statement to evaluate the default case. (This happens above.)
        // 2. Start splitting the switch basic block into subsequent basic blocks, each of which will contain
        //    a statement that is responsible for performing a comparison of the table index and conditional
        //    branch if equal.

        JITDUMP("Lowering switch BB%02u: using compare/branch expansion\n", originalSwitchBB->bbNum);

        // We'll use 'afterDefCondBlock' for the first conditional. After that, we'll add new
        // blocks. If we end up not needing it at all (say, if all the non-default cases just fall through),
        // we'll delete it.
        bool fUsedAfterDefCondBlock = false;
        BasicBlock* currentBlock = afterDefCondBlock;

        // Walk to entries 0 to jumpCnt - 1. If a case target follows, ignore it and let it fall through.
        // If no case target follows, the last one doesn't need to be a compare/branch: it can be an
        // unconditional branch.
        bool fAnyTargetFollows = false;
        for (unsigned i = 0; i < jumpCnt - 1; ++i)
        {
            assert(currentBlock != nullptr);

            // Remove the switch from the predecessor list of this case target's block.
            // We'll add the proper new predecessor edge later.
            flowList* oldEdge = comp->fgRemoveRefPred(jumpTab[i], afterDefCondBlock);

            if (jumpTab[i] == followingBB)
            {
                // This case label follows the switch; let it fall through.
                fAnyTargetFollows = true;
                continue;
            }

            // We need a block to put in the new compare and/or branch.
            // If we haven't used the afterDefCondBlock yet, then use that.
            if (fUsedAfterDefCondBlock)
            {
                BasicBlock* newBlock = comp->fgNewBBafter(BBJ_NONE, currentBlock, true);
                comp->fgAddRefPred(newBlock, currentBlock); // The fall-through predecessor.
                currentBlock = newBlock;
            }
            else
            {
                assert(currentBlock == afterDefCondBlock);
                fUsedAfterDefCondBlock = true;
            }

            // We're going to have a branch, either a conditional or unconditional,
            // to the target. Set the target.
            currentBlock->bbJumpDest = jumpTab[i];

            // Wire up the predecessor list for the "branch" case.
            comp->fgAddRefPred(jumpTab[i], currentBlock, oldEdge);

            if (!fAnyTargetFollows && (i == jumpCnt - 2))
            {
                // We're processing the last one, and there is no fall through from any case
                // to the following block, so we can use an unconditional branch to the final
                // case: there is no need to compare against the case index, since it's
                // guaranteed to be taken (since the default case was handled first, above).

                currentBlock->bbJumpKind = BBJ_ALWAYS;
            }
            else
            {
                // Otherwise, it's a conditional branch. Set the branch kind, then add the
                // condition statement.
                currentBlock->bbJumpKind = BBJ_COND;

                // Now, build the conditional statement for the current case that is
                // being evaluated:
                // GT_JTRUE
                //   |__ GT_COND
                //          |____GT_EQ
                //                 |____ (switchIndex) (The temp variable)
                //                 |____ (ICon)        (The actual case constant)
                GenTreePtr gtCaseCond = comp->gtNewOperNode(GT_EQ, TYP_INT,
                                                            comp->gtNewLclvNode(tempLclNum, tempLclType),
                                                            comp->gtNewIconNode(i, TYP_INT));
                /* Increment the lvRefCnt and lvRefCntWtd for temp */
                tempVarDsc->incRefCnts(originalSwitchBB->getBBWeight(comp), comp);

                GenTreePtr gtCaseBranch = comp->gtNewOperNode(GT_JTRUE, TYP_VOID, gtCaseCond);
                GenTreePtr gtCaseStmt = comp->fgNewStmtFromTree(gtCaseBranch, currentBlock);
                comp->fgInsertStmtAtEnd(currentBlock, gtCaseStmt);
            }
        }

        if (fAnyTargetFollows)
        {
            // There is a fall-through to the following block. In the loop
            // above, we deleted all the predecessor edges from the switch.
            // In this case, we need to add one back.
            comp->fgAddRefPred(currentBlock->bbNext, currentBlock);
        }

        if (!fUsedAfterDefCondBlock)
        {
            // All the cases were fall-through! We don't need this block.
            // Convert it from BBJ_SWITCH to BBJ_NONE and unset the BBF_DONT_REMOVE flag
            // so fgRemoveBlock() doesn't complain.
            JITDUMP("Lowering switch BB%02u: all switch cases were fall-through\n", originalSwitchBB->bbNum);
            assert(currentBlock == afterDefCondBlock);
            assert(currentBlock->bbJumpKind == BBJ_SWITCH);
            currentBlock->bbJumpKind = BBJ_NONE;
            currentBlock->bbFlags &= ~BBF_DONT_REMOVE;
            comp->fgRemoveBlock(currentBlock, /* unreachable */ false); // It's an empty block.
        }
    }
    else
    {
        // Lower the switch into an indirect branch using a jump table:
        //
        // 1. Create the constant for the default case
        // 2. Generate a GT_GE condition to compare to the default case
        // 3. Generate a GT_JTRUE to jump.
        // 4. Load the jump table address into a local (presumably the just
        //    created constant for GT_SWITCH).
        // 5. Create a new node for the lowered switch, this will both generate
        //    the branch table and also will be responsible for the indirect
        //    branch.

        JITDUMP("Lowering switch BB%02u: using jump table expansion\n", originalSwitchBB->bbNum);

        GenTreePtr gtTableSwitch = comp->gtNewOperNode(GT_SWITCH_TABLE,
                                                       TYP_VOID,
                                                       comp->gtNewLclvNode(tempLclNum, tempLclType),
                                                       comp->gtNewJmpTableNode());
        /* Increment the lvRefCnt and lvRefCntWtd for temp */
        tempVarDsc->incRefCnts(originalSwitchBB->getBBWeight(comp), comp);

        // this block no longer branches to the default block
        afterDefCondBlock->bbJumpSwt->removeDefault();
        comp->fgInvalidateSwitchDescMapEntry(afterDefCondBlock);

        GenTreeStmt* stmt = comp->fgNewStmtFromTree(gtTableSwitch);
        comp->fgInsertStmtAtEnd(afterDefCondBlock, stmt);
    }

    // Get rid of the original GT_SWITCH.
    comp->fgRemoveStmt(originalSwitchBB, comp->compCurStmt, false);
    // Set compCurStmt.  If asgStmt is top-level, we need to set it to that, so that any of
    // its embedded statements are traversed.  Otherwise, set it to condStmt, which will
    // contain the embedded asgStmt.
    if (asgStmt->gtStmtIsTopLevel())
    {
        comp->compCurStmt = asgStmt;
    }
    else
    {
#ifdef DEBUG
        GenTree* nextStmt = condStmt->gtNext;
        while (nextStmt != nullptr && nextStmt != asgStmt)
        {
            nextStmt = nextStmt->gtNext;
        }
        assert(nextStmt == asgStmt);
#endif // DEBUG
        comp->compCurStmt = condStmt;
    }
}

// splice in a unary op, between the child and parent
// resulting in parent->newNode->child
void Lowering::SpliceInUnary(GenTreePtr parent, GenTreePtr* ppChild, GenTreePtr newNode)
{
    GenTreePtr oldChild = *ppChild;

    // Replace tree in the parent node
    *ppChild = newNode;
    newNode->gtOp.gtOp1 = oldChild;

    oldChild->InsertAfterSelf(newNode);
}

//------------------------------------------------------------------------
// NewPutArg: rewrites the tree to put an arg in a register or on the stack.
//
// Arguments:
//    call - the call whose arg is being rewritten.
//    arg  - the arg being rewritten.
//    fp   - the ArgTabEntry for the argument.
//    type - the type of the argument.
//
// Return Value:
//    The new tree that was created to put the arg in the right place
//    or the incoming arg if the arg tree was not rewritten.
//
// Assumptions:
//    call, arg, and fp must be non-null.
//
// Notes:
//    For System V systems with native struct passing (i.e. FEATURE_UNIX_AMD64_STRUCT_PASSING defined)
//    this method allocates a single GT_PUTARG_REG for 1 eightbyte structs and a GT_LIST of two GT_PUTARG_REGs
//    for two eightbyte structs.
//
//    For STK passed structs the method generates GT_PUTARG_STK tree. For System V systems with native struct passing 
//    (i.e. FEATURE_UNIX_AMD64_STRUCT_PASSING defined) this method also sets the GP pointers count and the pointers 
//    layout object, so the codegen of the GT_PUTARG_STK could use this for optimizing copying to the stack by value.
//    (using block copy primitives for non GC pointers and a single TARGET_POINTER_SIZE copy with recording GC info.)
//
GenTreePtr Lowering::NewPutArg(GenTreeCall* call, GenTreePtr arg, fgArgTabEntryPtr fp, var_types type)
{
    assert(call != nullptr);
    assert(arg != nullptr);
    assert(fp != nullptr);

    GenTreePtr putArg = nullptr;
    bool updateArgTable = true;

#if !defined(_TARGET_64BIT_)
    if (varTypeIsLong(type))
    {
        // For TYP_LONG, we leave the GT_LONG as the arg, and put the putArg below it.
        // Therefore, we don't update the arg table entry.
        updateArgTable = false;
        type = TYP_INT;
    }
#endif // !defined(_TARGET_64BIT_)

    bool  isOnStack = true;
#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
    if (varTypeIsStruct(type))
    {
        isOnStack = !fp->structDesc.passedInRegisters;
    }
    else
    {
        isOnStack = fp->regNum == REG_STK;
    }
#else // !FEATURE_UNIX_AMD64_STRUCT_PASSING
    isOnStack = fp->regNum == REG_STK; 
#endif // !FEATURE_UNIX_AMD64_STRUCT_PASSING

    if (!isOnStack)
    {
#ifdef FEATURE_SIMD
        // TYP_SIMD8 is passed in an integer register.  We need the putArg node to be of the int type.
        if (type == TYP_SIMD8 && genIsValidIntReg(fp->regNum))
        {
            type = TYP_LONG;
        }
#endif //FEATURE_SIMD
#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
        if (fp->isStruct)
        {
            // The following code makes sure a register passed struct arg is moved to
            // the register before the call is made.
            // There are two cases (comments added in the code below.)
            // 1. The struct is of size one eightbyte:
            //    In this case a new tree is created that is GT_PUTARG_REG
            //    with a op1 the original argument.
            // 2. The struct is contained in 2 eightbytes:
            //    in this case the arg comes as a GT_LIST of two GT_LCL_FLDs - the two eightbytes of the struct.
            //    The code creates a GT_PUTARG_REG node for each GT_LCL_FLD in the GT_LIST
            //    and splices it in the list with the corresponding original GT_LCL_FLD tree as op1.

            assert(fp->structDesc.eightByteCount != 0);

            if (fp->structDesc.eightByteCount == 1)
            {
                // Case 1 above: Create a GT_PUTARG_REG node with op1 of the original tree.
                //
                // Here the IR for this operation:
                // lowering call :
                //     N001(3, 2)[000017] ------ - N---- / --*  &lclVar   byref  V00 loc0
                //     N003(6, 5)[000052] * --XG------ - / --*  indir     int
                //     N004(3, 2)[000046] ------ - N---- + --*  &lclVar   byref  V02 tmp0
                //     (13, 11)[000070] -- - XG-- - R-- - arg0 in out + 00 / --*  storeIndir int
                //     N009(3, 4)[000054] ------ - N----arg0 in rdi + --*  lclFld    int    V02 tmp0[+0](last use)
                //     N011(33, 21)[000018] --CXG------ - *call      void   Test.Foo.test1
                //
                // args :
                //     lowering arg : (13, 11)[000070] -- - XG-- - R-- - *storeIndir int
                //
                // late :
                //    lowering arg : N009(3, 4)[000054] ------ - N----             *  lclFld    int    V02 tmp0[+0](last use)
                //    new node is : (3, 4)[000071] ------------             *  putarg_reg int    RV
                //
                // after :
                //    N001(3, 2)[000017] ------ - N---- / --*  &lclVar   byref  V00 loc0
                //    N003(6, 5)[000052] * --XG------ - / --*  indir     int
                //    N004(3, 2)[000046] ------ - N---- + --*  &lclVar   byref  V02 tmp0
                //    (13, 11)[000070] -- - XG-- - R-- - arg0 in out + 00 / --*  storeIndir int
                //    N009(3, 4)[000054] ------ - N---- | / --*  lclFld    int    V02 tmp0[+0](last use)
                //    (3, 4)[000071] ------------arg0 in rdi + --*  putarg_reg int    RV
                //    N011(33, 21)[000018] --CXG------ - *call      void   Test.Foo.test1
                //

                putArg = comp->gtNewOperNode(GT_PUTARG_REG, type, arg);
            }
            else if (fp->structDesc.eightByteCount == 2)
            {
                // Case 2 above: Convert the LCL_FLDs to PUTARG_REG
                //
                // lowering call :
                //     N001(3, 2)[000025] ------ - N----Source / --*  &lclVar   byref  V01 loc1
                //     N003(3, 2)[000056] ------ - N----Destination + --*  &lclVar   byref  V03 tmp1
                //     N006(1, 1)[000058] ------------ + --*  const     int    16
                //     N007(12, 12)[000059] - A--G---- - L - arg0 SETUP / --*  copyBlk   void
                //     N009(3, 4)[000061] ------ - N----arg0 in rdi + --*  lclFld    long   V03 tmp1[+0]
                //     N010(3, 4)[000063] ------------arg0 in rsi + --*  lclFld    long   V03 tmp1[+8](last use)
                //     N014(40, 31)[000026] --CXG------ - *call      void   Test.Foo.test2
                //
                // args :
                //     lowering arg : N007(12, 12)[000059] - A--G---- - L - *copyBlk   void
                //
                // late :
                //     lowering arg : N012(11, 13)[000065] ------------             *  <list>    struct
                //
                // after :
                //     N001(3, 2)[000025] ------ - N----Source / --*  &lclVar   byref  V01 loc1
                //     N003(3, 2)[000056] ------ - N----Destination + --*  &lclVar   byref  V03 tmp1
                //     N006(1, 1)[000058] ------------ + --*  const     int    16
                //     N007(12, 12)[000059] - A--G---- - L - arg0 SETUP / --*  copyBlk   void
                //     N009(3, 4)[000061] ------ - N---- | / --*  lclFld    long   V03 tmp1[+0]
                //     (3, 4)[000072] ------------arg0 in rdi + --*  putarg_reg long
                //     N010(3, 4)[000063] ------------ | / --*  lclFld    long   V03 tmp1[+8](last use)
                //     (3, 4)[000073] ------------arg0 in rsi + --*  putarg_reg long
                //     N014(40, 31)[000026] --CXG------ - *call      void   Test.Foo.test2
                //

                assert(arg->OperGet() == GT_LIST);
                GenTreeArgList* argListPtr = arg->AsArgList();
                
                for (unsigned ctr = 0; argListPtr != nullptr; argListPtr = argListPtr->Rest(), ctr++)
                {
                    // Create a new GT_PUTARG_REG node with op1 the original GT_LCL_FLD.
                    GenTreePtr newOper = comp->gtNewOperNode(
                        GT_PUTARG_REG,
                        comp->GetTypeFromClassificationAndSizes(fp->structDesc.eightByteClassifications[ctr], fp->structDesc.eightByteSizes[ctr]),
                        argListPtr->gtOp.gtOp1);

                    // CopyCosts
                    newOper->CopyCosts(argListPtr->gtOp.gtOp1);

                    // Splice in the new GT_PUTARG_REG node in the GT_LIST
                    SpliceInUnary(argListPtr, &argListPtr->gtOp.gtOp1, newOper);
                }

                // Just return arg. The GT_LIST is not replaced.
                // Nothing more to do.
                return arg;
            }
            else
            {
                assert(false && "Illegal count of eightbytes for the CLR type system"); // No more than 2 eightbytes for the CLR.
                
            }
        }
        else
#endif // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
        {
            putArg = comp->gtNewOperNode(GT_PUTARG_REG, type, arg);
        }
    }
    else
    {
        // Mark this one as tail call arg if it is a fast tail call.
        // This provides the info to put this argument in in-coming arg area slot 
        // instead of in out-going arg area slot.

        FEATURE_UNIX_AMD64_STRUCT_PASSING_ONLY(assert(fp->isStruct == varTypeIsStruct(type))); // Make sure state is correct

#if FEATURE_FASTTAILCALL
        putArg = new (comp, GT_PUTARG_STK) GenTreePutArgStk(GT_PUTARG_STK,
                                                            type,
                                                            arg, 
                                                            fp->slotNum
                                                            FEATURE_UNIX_AMD64_STRUCT_PASSING_ONLY_ARG(fp->numSlots)
                                                            FEATURE_UNIX_AMD64_STRUCT_PASSING_ONLY_ARG(fp->isStruct),
                                                            call->IsFastTailCall() 
                                                            DEBUG_ARG(call));
#else
        putArg = new (comp, GT_PUTARG_STK) GenTreePutArgStk(GT_PUTARG_STK,
                                                            type, 
                                                            arg, 
                                                            fp->slotNum
                                                            FEATURE_UNIX_AMD64_STRUCT_PASSING_ONLY_ARG(fp->numSlots)
                                                            FEATURE_UNIX_AMD64_STRUCT_PASSING_ONLY_ARG(fp->isStruct)
                                                            DEBUG_ARG(call));
#endif

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
        // If the ArgTabEntry indicates that this arg is a struct
        // get and store the number of slots that are references.
        // This is later used in the codegen for PUT_ARG_STK implementation
        // for struct to decide whether and how many single eight-byte copies 
        // to be done (only for reference slots), so gcinfo is emitted.
        // For non-reference slots faster/smaller size instructions are used - 
        // pair copying using XMM registers or rep mov instructions.
        if (fp->isStruct)
        {
            unsigned numRefs = 0;
            BYTE* gcLayout = new (comp, CMK_Codegen) BYTE[fp->numSlots];
            // We use GT_LDOBJ for non-SIMD struct arguments. However, for
            // SIMD arguments the GT_LDOBJ has already been transformed.
            if (arg->gtOper != GT_LDOBJ)
            {
                assert(varTypeIsSIMD(arg));
            }
            else
            {
                assert(!varTypeIsSIMD(arg));
                numRefs = comp->info.compCompHnd->getClassGClayout(arg->gtLdObj.gtClass, gcLayout);
            }

            putArg->AsPutArgStk()->setGcPointers(numRefs, gcLayout);
        }
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING
    }

    putArg->CopyCosts(arg);

    if (arg->InReg())
    {
        putArg->SetInReg();
    }
#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
    else if (fp->isStruct)
    {
        if (fp->structDesc.passedInRegisters)
        {
            putArg->SetInReg();
        }
    }
#endif

    JITDUMP("new node is : ");
    DISPNODE(putArg);
    JITDUMP("\n");

    if (arg->gtFlags & GTF_LATE_ARG)
    {
        putArg->gtFlags |= GTF_LATE_ARG;
    }
    else if (updateArgTable)
    {
        fp->node = putArg;
    }
    return putArg;
}

// lower one arg of a call
// this currently entails splicing in a "putarg" node in between the arg evaluation and call
// These are the point at which the source is consumed and the values transition from control
// of the register allocator to the calling convention.
void Lowering::LowerArg(GenTreeCall* call, GenTreePtr* ppArg)
{
    GenTreePtr arg = *ppArg;

    JITDUMP("lowering arg : ");
    DISPNODE(arg);

    // No assignments should remain by Lowering.
    assert(!arg->OperIsAssignment());

    // assignments/stores at this level are not really placing an arg
    // they are setting up temporary locals that will later be placed into
    // outgoing regs or stack
    if (
        !arg->OperIsAssignment()     && 
        !arg->OperIsStore()          &&
        !arg->IsArgPlaceHolderNode() &&
        !arg->IsNothingNode()        && 
#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
        !arg->OperIsPutArgStk()      &&
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING
        !arg->OperIsCopyBlkOp()) // these are de facto placeholders (apparently)
    {
        fgArgTabEntryPtr fp = comp->gtArgEntryByNode(call, arg);
        assert(fp->node == arg);
        bool isReg = (fp->regNum != REG_STK);
        var_types type = arg->TypeGet();

        if (varTypeIsSmall(type))
        {
            // Normalize 'type', it represents the item that we will be storing in the Outgoing Args
            type = TYP_INT;
        }

        GenTreePtr putArg;

        // if we hit this we are probably double-lowering
        assert(arg->gtOper != GT_PUTARG_REG && arg->gtOper != GT_PUTARG_STK);

#if !defined(_TARGET_64BIT_)
        if (varTypeIsLong(type))
        {
            if (isReg)
            {
                NYI("Lowering of long register argument");
            }
            // For longs, we will create two PUTARG_STKs below the GT_LONG.
            // This is because the lo/hi values will be marked localDefUse, and we need to ensure that
            // they are pushed onto the stack as soon as they are created.
            // We also need to reverse the order, since the hi argument needs to be pushed first.
            noway_assert(arg->OperGet() == GT_LONG);
            GenTreePtr argLo = arg->gtGetOp1();
            GenTreePtr argHi = arg->gtGetOp2();

            GenTreePtr putArgLo = NewPutArg(call, argLo, fp, type);
            GenTreePtr putArgHi = NewPutArg(call, argHi, fp, type);

            arg->gtOp.gtOp1 = putArgLo;
            arg->gtOp.gtOp2 = putArgHi;

            // Now, reorder the arguments and insert the putArg in the right place.

            GenTreePtr argLoFirst = comp->fgGetFirstNode(argLo);
            GenTreePtr argHiFirst = comp->fgGetFirstNode(argHi);
            GenTreePtr argLoPrev = argLoFirst->gtPrev;
            noway_assert(argHiFirst->gtPrev == argLo);
            noway_assert(arg->gtPrev == argHi);

            argHiFirst->gtPrev = argLoPrev;
            if (argLoPrev != nullptr)
            {
                argLoPrev->gtNext = argHiFirst;
            }
            else
            {
                assert(comp->compCurStmt->gtStmt.gtStmtList == argLoFirst);
                comp->compCurStmt->gtStmt.gtStmtList = argHiFirst;
            }
            argHi->gtNext = putArgHi;
            putArgHi->gtPrev = argHi;
            putArgHi->gtNext = argLoFirst;
            argLoFirst->gtPrev = putArgHi;
            argLo->gtNext = putArgLo;
            putArgLo->gtPrev = argLo;
            putArgLo->gtNext = arg;
            arg->gtPrev = putArgLo;

            assert((arg->gtFlags & GTF_REVERSE_OPS) == 0);
            arg->gtFlags |= GTF_REVERSE_OPS;
        }
        else
#endif // !defined(_TARGET_64BIT_)
        {
            putArg = NewPutArg(call, arg, fp, type);

            // In the case of register passable struct (in one or two registers)
            // the NewPutArg returns a new node (GT_PUTARG_REG or a GT_LIST with two GT_PUTARG_REGs.)
            // If an extra node is returned, splice it in the right place in the tree.
            if (arg != putArg)
            {
                // putArg and arg are equals if arg is GT_LIST (a list of multiple LCL_FLDs to be passed in registers.)
                SpliceInUnary(call, ppArg, putArg);
            }
        }
    }
}

// do lowering steps for each arg of a call
void Lowering::LowerArgsForCall(GenTreeCall* call)
{
    JITDUMP("\n");
    JITDUMP("objp:\n======\n");
    if (call->gtCallObjp)
    {
        LowerArg(call, &call->gtCallObjp);
    }

    GenTreeArgList* args = call->gtCallArgs;

    JITDUMP("\nargs:\n======\n");
    for (; args; args = args->Rest())
    {
        LowerArg(call, &args->Current());
    }

    JITDUMP("\nlate:\n======\n");
    for (args = call->gtCallLateArgs; args; args = args->Rest())
    {
        LowerArg(call, &args->Current());
    }

    JITDUMP("\nafter:\n=====\n");
    DISPTREE(call);
}

// helper that create a node representing a relocatable physical address computation
// (optionally specifying the register to place it in)
GenTree* Lowering::AddrGen(ssize_t addr, regNumber reg)
{
    //this should end up in codegen as : instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, reg, addr)
    GenTree* result = comp->gtNewIconHandleNode(addr, GTF_ICON_FTN_ADDR);

    result->gtRegNum = reg;

    return result;
}

// variant that takes a void*
GenTree* Lowering::AddrGen(void* addr, regNumber reg)
{
    return AddrGen((ssize_t)addr, reg);
}

// do some common operations on trees before they are inserted as top level statements
GenTreeStmt* Lowering::LowerMorphAndSeqTree(GenTree* tree)
{
    tree = comp->fgMorphTree(tree);
    GenTreeStmt* stmt = comp->fgNewStmtFromTree(tree);
    return stmt;
}

// do lowering steps for a call
// this includes:
//   - adding the placement nodes (either stack or register variety) for arguments
//   - lowering the expression that calculates the target address
//   - adding nodes for other operations that occur after the call sequence starts and before
//        control transfer occurs (profiling and tail call helpers, pinvoke incantations)
//
void Lowering::LowerCall(GenTree* node)
{
    GenTreeCall* call = node->AsCall();
    GenTreeStmt* callStmt = comp->compCurStmt->AsStmt();
    assert(comp->fgTreeIsInStmt(call, callStmt));

    JITDUMP("lowering call:\n");
    DISPTREE(call);
    JITDUMP("\n");
    
    LowerArgsForCall(call);

// RyuJIT arm is not set up for lowered call control
#ifndef _TARGET_ARM_

    // note that everything generated from this point on runs AFTER the outgoing args are placed
    GenTree* result = nullptr;

    // for x86, this is where we record ESP for checking later to make sure stack is balanced

    // Check for Delegate.Invoke(). If so, we inline it. We get the
    // target-object and target-function from the delegate-object, and do
    // an indirect call.
    if (call->IsDelegateInvoke())
    {
        result = LowerDelegateInvoke(call);
    }
    else
    {
        //  Virtual and interface calls
        switch (call->gtFlags & GTF_CALL_VIRT_KIND_MASK)
        {
        case GTF_CALL_VIRT_STUB:
            result = LowerVirtualStubCall(call);
            break;

        case GTF_CALL_VIRT_VTABLE:
            // stub dispatching is off or this is not a virtual call (could be a tailcall)
            result = LowerVirtualVtableCall(call);
            break;

        case GTF_CALL_NONVIRT:
        {

#if INLINE_PINVOKE
            if (call->IsUnmanaged())
            {
                result = LowerNonvirtPinvokeCall(call);
                break;
            }
#endif
            if  (call->gtCallType == CT_INDIRECT)
            {
                result = LowerIndirectNonvirtCall(call);
                break;
            }
            else
            {
                result = LowerDirectCall(call);
                break;
            }
        }
        break;

        default:
            noway_assert(!"strange call type");
            break;
        }
    }


#ifdef DEBUG
    comp->fgDebugCheckNodeLinks(comp->compCurBB, comp->compCurStmt);
#endif

    if (result)
    {
        // The controlExpr is newly constructed, so we can use tree sequencing
        comp->gtSetEvalOrder(result);
        comp->fgSetTreeSeq(result, nullptr);

        JITDUMP("results of lowering call:\n");
        DISPTREE(result);
    }
    
    if (call->IsTailCallViaHelper())
    {
        // Either controlExpr or gtCallAddr must contain real call target.
        if (result == nullptr)
        {
            assert(call->gtCallAddr != nullptr);
            result = call->gtCallAddr;
        }

        result = LowerTailCallViaHelper(call, result);

        // We got a new call target constructed, so resequence it.
        comp->gtSetEvalOrder(result);
        comp->fgSetTreeSeq(result, nullptr);
        JITDUMP("results of lowering tail call via helper:\n");
        DISPTREE(result);            
    }
    else if (call->IsFastTailCall())
    {
        LowerFastTailCall(call);
    }
    
    if (result)
    {        
        GenTree* insertionPoint = call;
        if (!call->IsTailCallViaHelper())
        {            
            // The controlExpr should go before the gtCallCookie and the gtCallAddr, if they exist        
            if (call->gtCallType == CT_INDIRECT)
            {
                if (call->gtCallCookie != nullptr)
                {
                    insertionPoint = comp->fgGetFirstNode(call->gtCallCookie);
                }
                else if (call->gtCallAddr != nullptr)
                {
                    insertionPoint = comp->fgGetFirstNode(call->gtCallAddr);
                }
            }
        }

        comp->fgInsertTreeInListBefore(result, insertionPoint, callStmt);
        call->gtControlExpr = result;
    }
#endif //!_TARGET_ARM_

#ifdef DEBUG
    comp->fgDebugCheckNodeLinks(comp->compCurBB, callStmt);
#endif

    if (comp->opts.IsJit64Compat())
    {
        CheckVSQuirkStackPaddingNeeded(call);
    }
}

// Though the below described issue gets fixed in intellitrace dll of VS2015 (a.k.a Dev14), 
// we still need this quirk for desktop so that older version of VS (e.g. VS2010/2012)
// continues to work.
// This quirk is excluded from other targets that have no back compat burden.
//
// Quirk for VS debug-launch scenario to work: 
// See if this is a PInvoke call with exactly one param that is the address of a struct local.
// In such a case indicate to frame-layout logic to add 16-bytes of padding
// between save-reg area and locals.  This is to protect against the buffer
// overrun bug in microsoft.intellitrace.11.0.0.dll!ProfilerInterop.InitInterop(). 
//
// A work-around to this bug is to disable IntelliTrace debugging 
// (VS->Tools->Options->IntelliTrace->Enable IntelliTrace - uncheck this option).
// The reason why this works on Jit64 is that at the point of AV the call stack is
//
// GetSystemInfo() Native call
// IL_Stub generated for PInvoke declaration.
// ProfilerInterface::InitInterop()
// ProfilerInterface.Cctor()
// VM asm worker
//
// The cctor body has just the call to InitInterop().  VM asm worker is holding
// something in rbx that is used immediately after the Cctor call.  Jit64 generated
// InitInterop() method is pushing the registers in the following order
//
//  rbx
//  rbp
//  rsi
//  rdi
//  r12
//  r13
//  Struct local
//
// Due to buffer overrun, rbx doesn't get impacted.  Whereas RyuJIT jitted code of
// the same method is pushing regs in the following order
// 
//  rbp
//  rdi
//  rsi
//  rbx
//  struct local
//
// Therefore as a fix, we add padding between save-reg area and locals to 
// make this scenario work against JB.
//
// Note: If this quirk gets broken due to other JIT optimizations, we should consider
// more tolerant fix.  One such fix is to padd the struct.
void Lowering::CheckVSQuirkStackPaddingNeeded(GenTreeCall* call)
{
    assert(comp->opts.IsJit64Compat());

#ifdef _TARGET_AMD64_
    // Confine this to IL stub calls which aren't marked as unmanaged.
    if (call->IsPInvoke() && !call->IsUnmanaged())
    {
        bool paddingNeeded = false;
        GenTreePtr firstPutArgReg = nullptr;
        for (GenTreeArgList* args = call->gtCallLateArgs; args; args = args->Rest())
        {
            GenTreePtr tmp = args->Current();
            if (tmp->OperGet() == GT_PUTARG_REG)
            {
                if (firstPutArgReg == nullptr)
                {                    
                    firstPutArgReg = tmp;
                    GenTreePtr op1 = firstPutArgReg->gtOp.gtOp1;

                    if (op1->OperGet() == GT_LCL_VAR_ADDR)
                    {
                        unsigned lclNum = op1->AsLclVarCommon()->GetLclNum();
                        // TODO-1stClassStructs: This is here to duplicate previous behavior,
                        // but is not needed because the scenario being quirked did not involve
                        // a SIMD or enregisterable struct.
                        // if(comp->lvaTable[lclNum].TypeGet() == TYP_STRUCT)
                        if (varTypeIsStruct(comp->lvaTable[lclNum].TypeGet()))
                        {
                            // First arg is addr of a struct local.
                            paddingNeeded = true;
                        }
                        else 
                        {
                            // Not a struct local.
                            assert(paddingNeeded == false);
                            break;
                        }
                    }
                    else
                    {
                        // First arg is not a local var addr.
                        assert(paddingNeeded == false);
                        break;
                    }
                }
                else
                {
                    // Has more than one arg.
                    paddingNeeded = false;
                    break;
                }
            }
        }

        if (paddingNeeded)
        {
            comp->compVSQuirkStackPaddingNeeded = VSQUIRK_STACK_PAD;
        }
    }
#endif // _TARGET_AMD64_
}

// Inserts profiler hook, GT_PROF_HOOK for a tail call node.
//
// We need to insert this after all nested calls, but before all the arguments to this call have been set up.
// To do this, we look for the first GT_PUTARG_STK or GT_PUTARG_REG, and insert the hook immediately before
// that. If there are no args, then it should be inserted before the call node.
// 
// For example:
//              *  stmtExpr  void  (top level) (IL 0x000...0x010)
// arg0 SETUP   |  /--*  argPlace  ref    REG NA $c5
// this in rcx  |  |     /--*  argPlace  ref    REG NA $c1
//              |  |     |  /--*  call      ref    System.Globalization.CultureInfo.get_InvariantCulture $c2
// arg1 SETUP   |  |     +--*  st.lclVar ref    V02 tmp1          REG NA $c2
//              |  |     |  /--*  lclVar    ref    V02 tmp1         u : 2 (last use) REG NA $c2
// arg1 in rdx  |  |     +--*  putarg_reg ref    REG NA
//              |  |     |  /--*  lclVar    ref    V00 arg0         u : 2 (last use) REG NA $80
// this in rcx  |  |     +--*  putarg_reg ref    REG NA
//              |  |  /--*  call nullcheck ref    System.String.ToLower $c5
//              |  |  {  *  stmtExpr  void  (embedded)(IL 0x000... ? ? ? )
//              |  |  {  \--*  prof_hook void   REG NA
// arg0 in rcx  |  +--*  putarg_reg ref    REG NA
// control expr |  +--*  const(h)  long   0x7ffe8e910e98 ftn REG NA
//              \--*  call      void   System.Runtime.Remoting.Identity.RemoveAppNameOrAppGuidIfNecessary $VN.Void
// 
// In this case, the GT_PUTARG_REG src is a nested call. We need to put the embedded statement after that call (as shown).
// We assume that of all the GT_PUTARG_*, only the first one can have a nested call.
//
// Params:
//    callNode        - tail call node
//    insertionPoint  - if caller has an insertion point; If null
//                      profiler hook is inserted before args are setup
//                      but after all arg side effects are computed.
void  Lowering::InsertProfTailCallHook(GenTreeCall* call, GenTree *insertionPoint)
{
    assert(call->IsTailCall());
    assert(comp->compIsProfilerHookNeeded());

    if (insertionPoint == nullptr)
    {
        GenTreePtr tmp = nullptr;
        for (GenTreeArgList* args = call->gtCallArgs; args; args = args->Rest())
        {
            tmp = args->Current();
            assert(tmp->OperGet() != GT_PUTARG_REG); // We don't expect to see these in gtCallArgs
            if (tmp->OperGet() == GT_PUTARG_STK)
            {
                // found it
                insertionPoint = tmp;
                break;
            }
        }

        if (insertionPoint == nullptr)
        {
            for (GenTreeArgList* args = call->gtCallLateArgs; args; args = args->Rest())
            {
                tmp = args->Current();
                if ((tmp->OperGet() == GT_PUTARG_REG) || (tmp->OperGet() == GT_PUTARG_STK))
                {
                    // found it
                    insertionPoint = tmp;
                    break;
                }
            }

            // If there are no args, insert before the call node
            if (insertionPoint == nullptr)
            {
                insertionPoint = call;
            }
        }
    }

    assert(insertionPoint != nullptr);
    GenTreeStmt* callStmt = comp->compCurStmt->AsStmt();
    GenTreePtr profHookNode = new (comp, GT_PROF_HOOK) GenTree(GT_PROF_HOOK, TYP_VOID);
    comp->fgInsertTreeBeforeAsEmbedded(profHookNode, insertionPoint, callStmt, comp->compCurBB);
}

// Lower fast tail call implemented as epilog+jmp.
// Also inserts PInvoke method epilog if required.
void Lowering::LowerFastTailCall(GenTreeCall *call)
{
#if FEATURE_FASTTAILCALL
    // Tail call restrictions i.e. conditions under which tail prefix is ignored.
    // Most of these checks are already done by importer or fgMorphTailCall().
    // This serves as a double sanity check.
    assert((comp->info.compFlags & CORINFO_FLG_SYNCH) == 0);  // tail calls from synchronized methods
    assert(!comp->opts.compNeedSecurityCheck);                // tail call from methods that need security check
    assert(!call->IsUnmanaged());                             // tail calls to unamanaged methods
    assert(!comp->compLocallocUsed);                          // tail call from methods that also do localloc
    assert(!comp->getNeedsGSSecurityCookie());                // jit64 compat: tail calls from methods that need GS check

    // We expect to see a call that meets the following conditions
    assert(call->IsFastTailCall());

    // VM cannot use return address hijacking when A() and B() tail call each
    // other in mutual recursion.  Therefore, this block is reachable through
    // a GC-safe point or the whole method is marked as fully interruptible.
    //
    // TODO-Cleanup: 
    // optReachWithoutCall() depends on the fact that loop headers blocks
    // will have a block number > fgLastBB.  These loop headers gets added
    // after dominator computation and get skipped by OptReachWithoutCall().
    // The below condition cannot be asserted in lower because fgSimpleLowering() 
    // can add a new basic block for range check failure which becomes
    // fgLastBB with block number > loop header block number.
    //assert((comp->compCurBB->bbFlags & BBF_GC_SAFE_POINT) || !comp->optReachWithoutCall(comp->fgFirstBB, comp->compCurBB) || comp->genInterruptible);

#if INLINE_PINVOKE
    // If PInvokes are in-lined, we have to remember to execute PInvoke method epilog anywhere that
    // a method returns.  This is a case of caller method has both PInvokes and tail calls.
    if (comp->info.compCallUnmanaged)
    {
        InsertPInvokeMethodEpilog(comp->compCurBB DEBUGARG(call));
    }
#endif

    // Args for tail call are setup in incoming arg area.  The gc-ness of args of
    // caller and callee (which being tail called) may not match.  Therefore, everything
    // from arg setup until the epilog need to be non-interuptible by GC.  This is 
    // achieved by inserting GT_START_NONGC before the very first GT_PUTARG_STK node
    // of call is setup.  Note that once a stack arg is setup, it cannot have nested
    // calls subsequently in execution order to setup other args, because the nested
    // call could over-write the stack arg that is setup earlier.    
    GenTreePtr firstPutArgStk = nullptr;
    GenTreeArgList* args;
    ArrayStack<GenTree*> putargs(comp);

    for (args = call->gtCallArgs; args; args = args->Rest())
    {
        GenTreePtr tmp = args->Current();
        if (tmp->OperGet() == GT_PUTARG_STK)
            putargs.Push(tmp);
    }

    for (args = call->gtCallLateArgs; args; args = args->Rest())
    {
        GenTreePtr tmp = args->Current();
        if (tmp->OperGet() == GT_PUTARG_STK)
            putargs.Push(tmp);
    }

    if (putargs.Height() > 0)
        firstPutArgStk = putargs.Bottom();

    // If we have a putarg_stk node, also count the number of non-standard args the
    // call node has.  Note that while determining whether a tail call can be fast
    // tail called, we don't count non-standard args (passed in R10 or R11) since they
    // don't contribute to outgoing arg space.  These non-standard args are not
    // accounted in caller's arg count but accounted in callee's arg count after 
    // fgMorphArgs(). Therefore, exclude callee's non-standard args while mapping
    // callee's stack arg num to corresponding caller's stack arg num.
    unsigned calleeNonStandardArgCount = call->GetNonStandardArgCount();

#ifdef DEBUG
    // cross check non-standard arg count.
    if (firstPutArgStk && call->HasNonStandardArgs())
    {
        fgArgInfoPtr         argInfo = call->gtCall.fgArgInfo;
        unsigned            argCount = argInfo->ArgCount();
        fgArgTabEntryPtr *  argTable = argInfo->ArgTable();

        unsigned cnt = 0;
        for (unsigned i=0; i < argCount; i++)
        {
            if (argTable[i]->isNonStandard)
                ++cnt;
        }
        assert(cnt == calleeNonStandardArgCount);
    }
#endif


    // Say Caller(a, b, c, d, e) fast tail calls Callee(e, d, c, b, a)
    // i.e. passes its arguments in reverse to Callee. During call site
    // setup, after computing argument side effects, stack args are setup
    // first and reg args next.  In the above example, both Callers and 
    // Callee stack args (e and a respectively) share the same stack slot
    // and are alive at the same time.  The act of setting up Callee's 
    // stack arg will over-write the stack arg of Caller and if there are
    // further uses of Caller stack arg we have to make sure that we move
    // it to a temp before over-writing its slot and use temp in place of
    // the corresponding Caller stack arg.
    //
    // For the above example, conceptually this is what is done
    //       tmp = e;
    //       Stack slot of e  = a
    //       R9 = b, R8 = c, RDx = d
    //       RCX = tmp
    //
    // The below logic is meant to detect cases like this and introduce
    // temps to set up args correctly for Callee.
    GenTreeStmt* callStmt = comp->compCurStmt->AsStmt();

    for (int i = 0; i < putargs.Height(); i++)
    {
        GenTreePtr putArgStkNode = putargs.Bottom(i);

        assert(putArgStkNode->OperGet() == GT_PUTARG_STK);        
        
        // Get the caller arg num corresponding to this callee arg.
        // Note that these two args share the same stack slot. Therefore,
        // if there are further uses of corresponding caller arg, we need
        // to move it to a temp and use the temp in this call tree.
        //
        // Note that Caller is guaranteed to have a param corresponding to
        // this Callee's arg since fast tail call mechanism counts the 
        // stack slots required for both Caller and Callee for passing params
        // and allow fast tail call only if stack slots required by Caller >=
        // Callee.
        fgArgTabEntryPtr argTabEntry = comp->gtArgEntryByNode(call, putArgStkNode);
        assert(argTabEntry);
        unsigned callerArgNum = argTabEntry->argNum - calleeNonStandardArgCount;
        noway_assert(callerArgNum < comp->info.compArgsCount);

        unsigned callerArgLclNum = callerArgNum;
        LclVarDsc*  callerArgDsc = comp->lvaTable + callerArgLclNum;
        if (callerArgDsc->lvPromoted)
        {
            callerArgLclNum = callerArgDsc->lvFieldLclStart;  // update the callerArgNum to the promoted struct field's lclNum
            callerArgDsc = comp->lvaTable + callerArgLclNum;
        }
        noway_assert(callerArgDsc->lvIsParam);

        // Start searching in execution order list till we encounter call node
        unsigned tmpLclNum = BAD_VAR_NUM;
        var_types tmpType = TYP_UNDEF;
        for (GenTreePtr treeNode = putArgStkNode->gtNext; treeNode != call; treeNode = treeNode->gtNext)
        {            
            if (treeNode->OperIsLocal() || treeNode->OperIsLocalAddr())
            {                
                // This should neither be a GT_REG_VAR nor GT_PHI_ARG.
                assert((treeNode->OperGet() != GT_REG_VAR) && (treeNode->OperGet() != GT_PHI_ARG));

                GenTreeLclVarCommon *lcl = treeNode->AsLclVarCommon();
                LclVarDsc* lclVar = &comp->lvaTable[lcl->gtLclNum];

                // Fast tail calling criteria permits passing of structs of size 1, 2, 4 and 8 as args.
                // It is possible that the callerArgLclNum corresponds to such a struct whose stack slot
                // is getting over-written by setting up of a stack arg and there are further uses of
                // any of its fields if such a struct is type-dependently promoted.  In this case too
                // we need to introduce a temp.
                if ((lcl->gtLclNum == callerArgNum) || (lcl->gtLclNum == callerArgLclNum))
                {
                    // Create tmp and use it in place of callerArgDsc
                    if (tmpLclNum == BAD_VAR_NUM)
                    {
                        tmpLclNum = comp->lvaGrabTemp(true DEBUGARG("Fast tail call lowering is creating a new local variable"));
                        comp->lvaSortAgain = true;
                        tmpType = genActualType(callerArgDsc->lvaArgType());
                        comp->lvaTable[tmpLclNum].lvType = tmpType;
                        comp->lvaTable[tmpLclNum].lvRefCnt = 1;
                    }

                    lcl->SetLclNum(tmpLclNum);
                    lcl->SetOper(GT_LCL_VAR);

                }                
            }
        }

        // If we have created a temp, insert an embedded assignment stmnt before
        // the first putargStkNode i.e.
        //     tmpLcl = CallerArg
        if (tmpLclNum != BAD_VAR_NUM)
        {
            assert(tmpType != TYP_UNDEF);
            GenTreeLclVar* local = new(comp, GT_LCL_VAR) GenTreeLclVar(GT_LCL_VAR, tmpType, callerArgLclNum, BAD_IL_OFFSET);
            GenTree* assignExpr = comp->gtNewTempAssign(tmpLclNum, local);
            comp->fgInsertTreeBeforeAsEmbedded(assignExpr, firstPutArgStk, callStmt, comp->compCurBB);
        }
    }

    // Insert GT_START_NONGC node before the first GT_PUTARG_STK node.
    // Note that if there are no args to be setup on stack, no need to 
    // insert GT_START_NONGC node.        
    GenTreePtr startNonGCNode = nullptr;
    if (firstPutArgStk != nullptr)
    {                
        startNonGCNode = new (comp, GT_START_NONGC) GenTree(GT_START_NONGC, TYP_VOID);         
        comp->fgInsertTreeBeforeAsEmbedded(startNonGCNode, firstPutArgStk, callStmt, comp->compCurBB);

        // Gc-interruptability in the following case:
        //     foo(a, b, c, d, e) { bar(a, b, c, d, e); } 
        //     bar(a, b, c, d, e) { foo(a, b, d, d, e); }
        //
        // Since the instruction group starting from the instruction that sets up first
        // stack arg to the end of the tail call is marked as non-gc interruptible,
        // this will form a non-interruptible tight loop causing gc-starvation. To fix
        // this we insert GT_NO_OP as embedded stmt before GT_START_NONGC, if the method
        // has a single basic block and is not a GC-safe point.  The presence of a single
        // nop outside non-gc interruptible region will prevent gc starvation.
        if ((comp->fgBBcount == 1) && !(comp->compCurBB->bbFlags & BBF_GC_SAFE_POINT))
        {
            assert(comp->fgFirstBB == comp->compCurBB);
            GenTreePtr noOp = new (comp, GT_NO_OP) GenTree(GT_NO_OP, TYP_VOID);
            comp->fgInsertTreeBeforeAsEmbedded(noOp, startNonGCNode, callStmt, comp->compCurBB);
        }
    }

    // Insert GT_PROF_HOOK node to emit profiler tail call hook. This should be
    // inserted before the args are setup but after the side effects of args are
    // computed. That is, GT_PROF_HOOK node needs to be inserted before GT_START_NONGC
    // node if one exists.
    if (comp->compIsProfilerHookNeeded())
    {
        InsertProfTailCallHook(call, startNonGCNode);
    }

#else  // !FEATURE_FASTTAILCALL

    // Platform choose not to implement fast tail call mechanism.
    // In such a case we should never be reaching this method as
    // the expectation is that IsTailCallViaHelper() will always
    // be true on such a platform.
    unreached();
#endif
}

// Lower tail.call(void *copyRoutine, void *dummyArg, ...) as Jit_TailCall(void *copyRoutine, void *callTarget, ...).
// Also inserts PInvoke method epilog if required.
//
// Params
//    call         -  The call node
//    callTarget   -  The real call target.  This is used to replace the dummyArg during lowering.
//
// Returns control expr for making a call to helper Jit_TailCall.
GenTree* Lowering::LowerTailCallViaHelper(GenTreeCall* call, GenTree *callTarget)
{    
    NYI_X86("Lower tail call dispatched via helper");

    // Tail call restrictions i.e. conditions under which tail prefix is ignored.
    // Most of these checks are already done by importer or fgMorphTailCall().
    // This serves as a double sanity check.
    assert((comp->info.compFlags & CORINFO_FLG_SYNCH) == 0);  // tail calls from synchronized methods
    assert(!comp->opts.compNeedSecurityCheck);                // tail call from methods that need security check
    assert(!call->IsUnmanaged());                             // tail calls to unamanaged methods
    assert(!comp->compLocallocUsed);                          // tail call from methods that also do localloc
    assert(!comp->getNeedsGSSecurityCookie());                // jit64 compat: tail calls from methods that need GS check
    
    // We expect to see a call that meets the following conditions
    assert(call->IsTailCallViaHelper());
    assert(callTarget != nullptr);
   
    // TailCall helper though is a call never returns to caller nor GC interruptible. 
    // Therefore the block containg the tail call should be a GC-SafePoint to avoid
    // GC starvation.
    assert(comp->compCurBB->bbFlags & BBF_GC_SAFE_POINT);

#if INLINE_PINVOKE
    // If PInvokes are in-lined, we have to remember to execute PInvoke method epilog anywhere that
    // a method returns.  This is a case of caller method has both PInvokes and tail calls.
    if (comp->info.compCallUnmanaged)
    {
        InsertPInvokeMethodEpilog(comp->compCurBB DEBUGARG(call));
    }
#endif

    // Remove gtCallAddr from execution order if one present.
    GenTreeStmt* callStmt = comp->compCurStmt->AsStmt();     
    if (call->gtCallType == CT_INDIRECT)
    {
        assert(call->gtCallAddr != nullptr);
        comp->fgDeleteTreeFromList(callStmt, call->gtCallAddr);
    }

    // In case of helper based tail calls, first argument is CopyRoutine and second argument
    // is a place holder node. 
    fgArgTabEntry* argEntry;
#ifdef DEBUG
    argEntry = comp->gtArgEntryByArgNum(call, 0);
    assert(argEntry != nullptr);
    assert(argEntry->node->gtOper == GT_PUTARG_REG);
    GenTree *firstArg = argEntry->node->gtOp.gtOp1;
    assert(firstArg->gtOper == GT_CNS_INT);
#endif

    // Replace second arg by callTarget.
    argEntry = comp->gtArgEntryByArgNum(call, 1);
    assert(argEntry != nullptr);
    assert(argEntry->node->gtOper == GT_PUTARG_REG);
    GenTree *secondArg = argEntry->node->gtOp.gtOp1;
   
    comp->fgInsertTreeInListAfter(callTarget, secondArg, callStmt);    
    comp->fgDeleteTreeFromList(callStmt, secondArg);
    argEntry->node->gtOp.gtOp1 = callTarget;

    // Transform this call node into a call to Jit tail call helper.
    call->gtCallType = CT_HELPER;
    call->gtCallMethHnd = comp->eeFindHelper(CORINFO_HELP_TAILCALL);
    call->gtFlags &= ~GTF_CALL_VIRT_KIND_MASK;

    // Lower this as if it were a pure helper call.
    call->gtFlags &= ~(GTF_CALL_M_TAILCALL | GTF_CALL_M_TAILCALL_VIA_HELPER);
    GenTree *result = LowerDirectCall(call);

    // Now add back tail call flags for identifying this node as tail call dispatched via helper.
    call->gtFlags |= GTF_CALL_M_TAILCALL | GTF_CALL_M_TAILCALL_VIA_HELPER;

    // Insert profiler tail call hook if needed.
    // Since we don't know the insertion point, pass null for second param.
    if(comp->compIsProfilerHookNeeded())
    {
        InsertProfTailCallHook(call, nullptr);
    }

    return result;
}

// Lower "jmp <method>" tail call to insert PInvoke method epilog if required.
void Lowering::LowerJmpMethod(GenTree* jmp)
{
    assert(jmp->OperGet() == GT_JMP);

    JITDUMP("lowering GT_JMP\n");
    DISPTREE(jmp);
    JITDUMP("============");

#if INLINE_PINVOKE
    // If PInvokes are in-lined, we have to remember to execute PInvoke method epilog anywhere that
    // a method returns.
    if (comp->info.compCallUnmanaged)
    {
        InsertPInvokeMethodEpilog(comp->compCurBB DEBUGARG(jmp));
    }
#endif
}

// Lower GT_RETURN node to insert PInvoke method epilog if required.
void Lowering::LowerRet(GenTree* ret)
{
    assert(ret->OperGet() == GT_RETURN);

    JITDUMP("lowering GT_RETURN\n");
    DISPTREE(ret);
    JITDUMP("============");

#if INLINE_PINVOKE
    // Method doing PInvokes has exactly one return block unless it has tail calls.
    if (comp->info.compCallUnmanaged && (comp->compCurBB == comp->genReturnBB))
    {
        InsertPInvokeMethodEpilog(comp->compCurBB DEBUGARG(ret));
    }
#endif
}

GenTree* Lowering::LowerDirectCall(GenTreeCall* call)
{
    noway_assert(call->gtCallType == CT_USER_FUNC || call->gtCallType == CT_HELPER);
    
    // Don't support tail calling helper methods.
    // But we might encounter tail calls dispatched via JIT helper appear as a tail call to helper.
    noway_assert(!call->IsTailCall() || call->IsTailCallViaHelper() || call->gtCallType == CT_USER_FUNC);

    // Non-virtual direct/indirect calls: Work out if the address of the
    // call is known at JIT time.  If not it is either an indirect call
    // or the address must be accessed via an single/double indirection.

    void* addr;
    InfoAccessType accessType;
    CorInfoHelpFunc helperNum = comp->eeGetHelperNum(call->gtCallMethHnd);

#ifdef FEATURE_READYTORUN_COMPILER
    if (call->gtEntryPoint.addr != nullptr)
    {
        accessType = call->gtEntryPoint.accessType;
        addr = call->gtEntryPoint.addr;
    }
    else
#endif
    if (call->gtCallType == CT_HELPER)
    {
        noway_assert(helperNum != CORINFO_HELP_UNDEF);

        // the convention on getHelperFtn seems to be (it's not documented)
        // that it returns an address or if it returns null, pAddr is set to
        // another address, which requires an indirection
        void* pAddr;
        addr = comp->info.compCompHnd->getHelperFtn(helperNum, (void**)&pAddr);

        if (addr != nullptr)
        {
            accessType = IAT_VALUE;
        }
        else
        {
            accessType = IAT_PVALUE;
            addr = pAddr;
        }
    }
    else
    {
        noway_assert(helperNum == CORINFO_HELP_UNDEF);

        CORINFO_ACCESS_FLAGS  aflags = CORINFO_ACCESS_ANY;

        if (call->IsSameThis())
            aflags = (CORINFO_ACCESS_FLAGS)(aflags | CORINFO_ACCESS_THIS);

        if ((call->NeedsNullCheck()) == 0)
            aflags = (CORINFO_ACCESS_FLAGS)(aflags | CORINFO_ACCESS_NONNULL);

        CORINFO_CONST_LOOKUP addrInfo;
        comp->info.compCompHnd->getFunctionEntryPoint(call->gtCallMethHnd, &addrInfo, aflags);

        accessType = addrInfo.accessType;
        addr = addrInfo.addr;
    }

    GenTree* result = nullptr;
    switch (accessType)
    {
       case IAT_VALUE:
           // Non-virtual direct call to known address
           if (!IsCallTargetInRange(addr) || call->IsTailCall())
           {
               result = AddrGen(addr);
           }
           else
           {
               // a direct call within range of hardware relative call instruction
               // there is no extra code to generate
           }
           break;

       case IAT_PVALUE:
           // Non-virtual direct calls to addresses accessed by
           // a single indirection.
           result = Ind(AddrGen(addr));
           break;

       case IAT_PPVALUE:
           // Non-virtual direct calls to addresses accessed by
           // a double indirection.
           //
           // Double-indirection. Load the address into a register
           // and call indirectly through the register
           noway_assert(helperNum == CORINFO_HELP_UNDEF);
           result = AddrGen(addr);
           result = Ind(Ind(result));
           break;

       default:
           noway_assert(!"Bad accessType");
           break;
    }

    return result;
}


GenTree* Lowering::LowerDelegateInvoke(GenTreeCall* call)
{
    noway_assert(call->gtCallType == CT_USER_FUNC);

    assert((comp->info.compCompHnd->getMethodAttribs(call->gtCallMethHnd) & (CORINFO_FLG_DELEGATE_INVOKE|CORINFO_FLG_FINAL)) == (CORINFO_FLG_DELEGATE_INVOKE|CORINFO_FLG_FINAL));

    GenTree* thisNode;
    if (call->IsTailCallViaHelper())
    {
        // In case of helper dispatched tail calls, "thisptr" will be the third arg.
        // The first two args are: real call target and addr of args copy routine.
        const unsigned argNum = 2;
        fgArgTabEntryPtr thisArgTabEntry = comp->gtArgEntryByArgNum(call, argNum);
        thisNode = thisArgTabEntry->node;
    }
    else
    {
        thisNode = comp->gtGetThisArg(call);
    }

    assert(thisNode->gtOper == GT_PUTARG_REG);
    GenTree** pThisExpr = &(thisNode->gtOp.gtOp1);

    // replace original expression feeding into thisPtr with
    // [originalThis + offsetOfDelegateInstance]

    GenTreeStmt* newStmt = comp->fgInsertEmbeddedFormTemp(pThisExpr);
    GenTree* stloc = newStmt->gtStmtExpr;
    newStmt->gtFlags |= GTF_STMT_SKIP_LOWER;

    unsigned originalThisLclNum = stloc->AsLclVarCommon()->GetLclNum();

    GenTree* originalThisValue = *pThisExpr;

    GenTree* newThisAddr = new(comp, GT_LEA) GenTreeAddrMode(TYP_REF,
                                                             originalThisValue,
                                                             nullptr,
                                                             0,
                                                             comp->eeGetEEInfo()->offsetOfDelegateInstance);
    originalThisValue->InsertAfterSelf(newThisAddr);

    GenTree* newThis = comp->gtNewOperNode(GT_IND, TYP_REF, newThisAddr);
    newThis->SetCosts(IND_COST_EX, 2);
    newThisAddr->InsertAfterSelf(newThis);
    *pThisExpr = newThis;

    // the control target is
    // [originalThis + firstTgtOffs]

    GenTree* base = new (comp, GT_LCL_VAR) GenTreeLclVar(originalThisValue->TypeGet(), originalThisLclNum, BAD_IL_OFFSET);

    unsigned targetOffs = comp->eeGetEEInfo()->offsetOfDelegateFirstTarget;
    GenTree* result = new(comp, GT_LEA) GenTreeAddrMode(TYP_REF, base, nullptr, 0, targetOffs);
    GenTree* callTarget = Ind(result);

    // don't need to sequence and insert this tree, caller will do it
    return callTarget;
}

GenTree* Lowering::LowerIndirectNonvirtCall(GenTreeCall* call)
{
    // Indirect cookie calls gets transformed by fgMorphArgs as indirect call with non-standard args.
    // Hence we should never see this type of call in lower.

#ifdef _TARGET_X86_
    if (call->gtCallCookie != nullptr)
    {
        NYI_X86("Morphing indirect non-virtual call with non-standard args");
    }
#endif

    noway_assert(call->gtCallCookie == nullptr);

    return nullptr;
}

// The return trap checks some global location
// (the runtime tells us where that is and how many indirections to make)
// then based on the result conditionally calls a GC helper.
// we use a special node for this because at this time, introducing flow is tedious/difficult
GenTree* Lowering::CreateReturnTrapSeq()
{
    // the GT_RETURNTRAP expands to this:
    //  if (g_TrapReturningThreads)
    //  {
    //       RareDisablePreemptiveGC();
    //  }

    // the only thing to do here is build up the expression that evaluates 'g_TrapReturningThreads'

    LONG*   addrOfCaptureThreadGlobal;
    LONG**  pAddrOfCaptureThreadGlobal;

    addrOfCaptureThreadGlobal = comp->info.compCompHnd->getAddrOfCaptureThreadGlobal((void**) &pAddrOfCaptureThreadGlobal);

    GenTree* testTree;
    if (addrOfCaptureThreadGlobal)
    {
        testTree = Ind(AddrGen(addrOfCaptureThreadGlobal));
    }
    else
    {
        testTree = Ind(Ind(AddrGen(pAddrOfCaptureThreadGlobal)));
    }
    return comp->gtNewOperNode(GT_RETURNTRAP, TYP_INT, testTree);
}


// returns a tree that stores the given constant (1 or 0) into the thread's GC state field
GenTree* Lowering::SetGCState(int state)
{
    //  Thread.offsetOfGcState = 0/1

    assert(state == 0 || state == 1);

    CORINFO_EE_INFO* pInfo = comp->eeGetEEInfo();

    GenTree* base = new(comp, GT_LCL_VAR) GenTreeLclVar(TYP_I_IMPL, comp->info.compLvFrameListRoot, -1);

    GenTree* storeGcState = new(comp, GT_STOREIND)
        GenTreeStoreInd(TYP_BYTE,
                     new(comp, GT_LEA) GenTreeAddrMode(TYP_I_IMPL,
                                                       base,
                                                       nullptr, 1, pInfo->offsetOfGCState),
                     new (comp, GT_CNS_INT) GenTreeIntCon(TYP_BYTE, state));

    return storeGcState;
}

// Returns a tree that either links the locally-allocated Frame or unlinks it out of the frame list.
// This is used for pinvoke inlining
GenTree* Lowering::CreateFrameLinkUpdate(FrameLinkAction action)
{
    CORINFO_EE_INFO* pInfo = comp->eeGetEEInfo();
    CORINFO_EE_INFO::InlinedCallFrameInfo& callFrameInfo = pInfo->inlinedCallFrameInfo;

    GenTree* TCB = new(comp, GT_LCL_VAR)
        GenTreeLclVar(GT_LCL_VAR, TYP_I_IMPL, comp->info.compLvFrameListRoot, (IL_OFFSET)-1); // cast to resolve ambiguity.

    // Thread->m_pFrame
    GenTree* addr = new(comp, GT_LEA)
        GenTreeAddrMode(TYP_I_IMPL, TCB, nullptr, 1, comp->eeGetEEInfo()->offsetOfThreadFrame);

    GenTree*data = nullptr;

    if (action == PushFrame)
    {
        // Thread->m_pFrame = &InlinedCallFrame;
        data = new(comp, GT_LCL_FLD_ADDR)
            GenTreeLclFld(GT_LCL_FLD_ADDR, TYP_BYREF, comp->lvaInlinedPInvokeFrameVar, callFrameInfo.offsetOfFrameVptr);
    }
    else
    {
        assert(action == PopFrame);
        // Thread->m_pFrame = InlinedCallFrame.m_pNext;

        data = new(comp, GT_LCL_FLD)
            GenTreeLclFld(GT_LCL_FLD, TYP_BYREF, comp->lvaInlinedPInvokeFrameVar, pInfo->inlinedCallFrameInfo.offsetOfFrameLink);
    }
    GenTree* storeInd = new(comp, GT_STOREIND) GenTreeStoreInd(TYP_I_IMPL, addr, data);
    return storeInd;
}

// this is the lowering equivalent of genPInvokeMethodProlog
// This creates the code that runs at the start of every method that has pinvoke calls
void Lowering::InsertPInvokeMethodProlog()
{
    NYI_X86("Implement PInvoke frame init inlining for x86");
    noway_assert(comp->info.compCallUnmanaged);
    noway_assert(comp->lvaInlinedPInvokeFrameVar != BAD_VAR_NUM);

    if (comp->opts.ShouldUsePInvokeHelpers())
    {
        // Initialize the P/Invoke frame by calling CORINFO_HELP_INIT_PINVOKE_FRAME:
        //
        // OpaqueFrame opaqueFrame;
        // CORINFO_HELP_INIT_PINVOKE_FRAME(&opaqueFrame);

        GenTree* frameAddr = new(comp, GT_LCL_VAR_ADDR)
            GenTreeLclVar(GT_LCL_VAR_ADDR, TYP_BYREF, comp->lvaInlinedPInvokeFrameVar, BAD_IL_OFFSET);

        GenTree* helperCall = comp->gtNewHelperCallNode(CORINFO_HELP_INIT_PINVOKE_FRAME, TYP_VOID, 0, comp->gtNewArgList(frameAddr));

        GenTreeStmt* stmt = LowerMorphAndSeqTree(helperCall);
        comp->fgInsertStmtAtBeg(comp->fgFirstBB, stmt);

        return;
    }

    CORINFO_EE_INFO* pInfo = comp->eeGetEEInfo();
    const CORINFO_EE_INFO::InlinedCallFrameInfo& callFrameInfo = pInfo->inlinedCallFrameInfo;

    //first arg:  &compiler->lvaInlinedPInvokeFrameVar + callFrameInfo.offsetOfFrameVptr

    GenTree* frameAddr = new(comp, GT_LCL_FLD_ADDR)
        GenTreeLclFld(GT_LCL_FLD_ADDR, TYP_BYREF, comp->lvaInlinedPInvokeFrameVar, callFrameInfo.offsetOfFrameVptr);

    // obtain Thread object
    // call runtime helper to fill in our frame and push it:
    //     TCB = CORINFO_HELP_INIT_PINVOKE_FRAME(&symFrameStart, secretArg);
    GenTree* call = comp->gtNewHelperCallNode(CORINFO_HELP_INIT_PINVOKE_FRAME, TYP_I_IMPL, 0, comp->gtNewArgList(frameAddr, PhysReg(REG_SECRET_STUB_PARAM)));

    // some sanity checks on the frame list root vardsc
    LclVarDsc* varDsc = &comp->lvaTable[comp->info.compLvFrameListRoot];
    noway_assert(!varDsc->lvIsParam);
    noway_assert(varDsc->lvType == TYP_I_IMPL);

    GenTree* store = new(comp, GT_STORE_LCL_VAR)
        GenTreeLclVar(GT_STORE_LCL_VAR, TYP_I_IMPL, comp->info.compLvFrameListRoot, (IL_OFFSET)-1); // cast to resolve ambiguity.
    store->gtOp.gtOp1 = call;
    store->gtFlags |= GTF_VAR_DEF;

    GenTreeStmt* stmt = LowerMorphAndSeqTree(store);
    comp->fgInsertStmtAtBeg(comp->fgFirstBB, stmt);

    JITDUMP("inserting pinvoke method prolog\n");
    DISPTREE(stmt);
    GenTree* lastStmt = stmt;

    // --------------------------------------------------------
    // InlinedCallFrame.m_pCallSiteSP = @RSP;

    GenTreeLclFld* storeSP = new(comp, GT_STORE_LCL_FLD)
        GenTreeLclFld(GT_STORE_LCL_FLD, TYP_I_IMPL, comp->lvaInlinedPInvokeFrameVar,
                                                    callFrameInfo.offsetOfCallSiteSP);
    storeSP->gtOp1 = PhysReg(REG_SPBASE);


    GenTreeStmt* storeSPStmt = LowerMorphAndSeqTree(storeSP);
    comp->fgInsertStmtAfter(comp->fgFirstBB, lastStmt, storeSPStmt);
    lastStmt = storeSPStmt;


    // --------------------------------------------------------
    // InlinedCallFrame.m_pCalleeSavedEBP = @RBP;

    GenTreeLclFld* storeFP = new(comp, GT_STORE_LCL_FLD)
        GenTreeLclFld(GT_STORE_LCL_FLD, TYP_I_IMPL, comp->lvaInlinedPInvokeFrameVar,
                                                    callFrameInfo.offsetOfCalleeSavedFP);
    storeFP->gtOp1 = PhysReg(REG_FPBASE);

    GenTreeStmt* storeFPStmt = LowerMorphAndSeqTree(storeFP);
    comp->fgInsertStmtAfter(comp->fgFirstBB, lastStmt, storeFPStmt);
    lastStmt = storeFPStmt;

    // --------------------------------------------------------

    if (comp->opts.eeFlags & CORJIT_FLG_IL_STUB)
    {
        // Push a frame - if we are NOT in an IL stub, this is done right before the call
        // The init routine sets InlinedCallFrame's m_pNext, so we just set the thead's top-of-stack
        GenTree* frameUpd = CreateFrameLinkUpdate(PushFrame);

        comp->fgInsertStmtAfter(comp->fgFirstBB, lastStmt, LowerMorphAndSeqTree(frameUpd));
    }
}

// Code that needs to be run when exiting any method that has pinvoke inlines
// this needs to be inserted any place you can exit the function: returns, tailcalls and jmps
//
// Parameters
//    returnBB   -  basic block from which a method can return
//    lastExpr   -  Gentree of the last top level stmnt of returnBB  (debug only arg)
void Lowering::InsertPInvokeMethodEpilog(BasicBlock *returnBB
                                         DEBUGARG(GenTreePtr lastExpr) )
{
    assert(returnBB != nullptr);
    assert(comp->info.compCallUnmanaged);

    if (comp->opts.ShouldUsePInvokeHelpers())
    {
        return;
    }

    // Method doing Pinvoke calls has exactly one return block unless it has "jmp" or tail calls.
#ifdef DEBUG
    bool endsWithTailCallOrJmp = false;
#if FEATURE_FASTTAILCALL
    endsWithTailCallOrJmp = returnBB->endsWithTailCallOrJmp(comp);
#endif // FEATURE_FASTTAILCALL
    assert(((returnBB == comp->genReturnBB) && (returnBB->bbJumpKind == BBJ_RETURN)) || endsWithTailCallOrJmp);
#endif // DEBUG

    GenTreeStmt* lastTopLevelStmt = comp->fgGetLastTopLevelStmt(returnBB)->AsStmt();
    GenTreePtr lastTopLevelStmtExpr = lastTopLevelStmt->gtStmtExpr;

    // Gentree of the last top level stmnt should match.
    assert(lastTopLevelStmtExpr == lastExpr);   

    // Note: PInvoke Method Epilog (PME) needs to be inserted just before GT_RETURN, GT_JMP or GT_CALL node in execution order
    // so that it is guaranteed that there will be no further PInvokes after that point in the method.
    //
    // Example1: GT_RETURN(op1) - say execution order is: Op1, GT_RETURN.  After inserting PME, execution order would be
    //           Op1, PME, GT_RETURN
    //
    // Example2: GT_CALL(arg side effect computing nodes, Stk Args Setup, Reg Args setup). The execution order would be
    //           arg side effect computing nodes, Stk Args setup, Reg Args setup, GT_CALL
    //           After inserting PME execution order would be:
    //           arg side effect computing nodes, Stk Args setup, Reg Args setup, PME, GT_CALL
    //
    // Example3: GT_JMP.  After inserting PME execution order would be: PME, GT_JMP
    //           That is after PME, args for GT_JMP call will be setup.

    // TODO-Cleanup: setting GCState to 1 seems to be redundant as InsertPInvokeCallProlog will set it to zero before a PInvoke
    // call and InsertPInvokeCallEpilog() will set it back to 1 after the PInvoke.  Though this is redundant, it is harmeless.
    // Note that liveness is artificially extending the life of compLvFrameListRoot var if the method being compiled has
    // pinvokes.  Deleting the below stmnt would cause an an assert in lsra.cpp::SetLastUses() since compLvFrameListRoot
    // will be live-in to a BBJ_RETURN block without any uses.  Long term we need to fix liveness for x64 case to properly
    // extend the life of compLvFrameListRoot var.
    // 
    // Thread.offsetOfGcState = 0/1 
    // That is [tcb + offsetOfGcState] = 1
    GenTree* storeGCState = SetGCState(1);
    comp->fgInsertTreeBeforeAsEmbedded(storeGCState, lastTopLevelStmtExpr, lastTopLevelStmt, returnBB);

    if (comp->opts.eeFlags & CORJIT_FLG_IL_STUB)
    {
        // Pop the frame, in non-stubs we do this around each pinvoke call
        GenTree* frameUpd = CreateFrameLinkUpdate(PopFrame);

        comp->fgInsertTreeBeforeAsEmbedded(frameUpd, lastTopLevelStmtExpr, lastTopLevelStmt, returnBB);
    }
}

// This function emits the call-site prolog for direct calls to unmanaged code.
// It does all the necessary setup of the InlinedCallFrame.
// frameListRoot specifies the local containing the thread control block.
// argSize or methodHandle is the value to be copied into the m_datum
// field of the frame (methodHandle may be indirected & have a reloc)
void Lowering::InsertPInvokeCallProlog(GenTreeCall* call)
{
    GenTree* insertBefore = call;
    if (call->gtCallType == CT_INDIRECT)
    {
        insertBefore = comp->fgGetFirstNode(call->gtCallAddr);
    }

    CORINFO_EE_INFO* pInfo = comp->eeGetEEInfo();
    CORINFO_EE_INFO::InlinedCallFrameInfo& callFrameInfo = pInfo->inlinedCallFrameInfo;

    gtCallTypes callType = (gtCallTypes)call->gtCallType;

    noway_assert(comp->lvaInlinedPInvokeFrameVar != BAD_VAR_NUM);

#if COR_JIT_EE_VERSION > 460
    if (comp->opts.ShouldUsePInvokeHelpers())
    {
        // First argument is the address of the frame variable.
        GenTree* frameAddr = new(comp, GT_LCL_VAR_ADDR)
            GenTreeLclVar(GT_LCL_VAR_ADDR, TYP_BYREF, comp->lvaInlinedPInvokeFrameVar, BAD_IL_OFFSET);

        // Insert call to CORINFO_HELP_JIT_PINVOKE_BEGIN
        GenTree* helperCall = comp->gtNewHelperCallNode(CORINFO_HELP_JIT_PINVOKE_BEGIN, TYP_VOID, 0, comp->gtNewArgList(frameAddr));

        comp->fgMorphTree(helperCall);
        comp->fgInsertTreeBeforeAsEmbedded(helperCall, insertBefore, comp->compCurStmt->AsStmt(), currBlock);
        return;
    }
#endif

    // emit the following sequence
    //
    // frame.callTarget := methodHandle
    // (x86) frame.callSiteTracker = SP
    //       frame.callSiteReturnAddress = return address
    // thread->gcState = 0
    // (non-stub) - update top Frame on TCB
    //

    // ----------------------------------------------------------------------------------
    // Setup frame.callSiteTarget (what it referred to in the JIT)
    // The actual field is Frame.m_Datum which has many different uses and meanings.
    // 

    CORINFO_METHOD_HANDLE methodHandle;
    
    if (callType == CT_INDIRECT)
        methodHandle = nullptr;
    else
        methodHandle = call->gtCallMethHnd;

    GenTree* src = nullptr;


    if (callType == CT_INDIRECT)
    {
        assert(methodHandle == nullptr);
        if (comp->info.compPublishStubParam)
        {
            src = new (comp, GT_LCL_VAR) GenTreeLclVar(TYP_I_IMPL, comp->lvaStubArgumentVar, BAD_IL_OFFSET);
        }
        // else { If we don't have secret parameter, m_Datum will be initialized by VM code }
    }
    else
    {
        assert(callType == CT_USER_FUNC);

        void* embedMethodHandle;
        void* pEmbedMethodHandle;

        embedMethodHandle = (void*)comp->info.compCompHnd->embedMethodHandle(
            methodHandle,
            &pEmbedMethodHandle);

        noway_assert((!embedMethodHandle) != (!pEmbedMethodHandle));

        if (embedMethodHandle != NULL)
        {
            // frame.callSiteTarget = methodDesc
            src = AddrGen(embedMethodHandle);
        }
        else
        {
            // frame.callSiteTarget = *pEmbedMethodHandle
            src = Ind(AddrGen(pEmbedMethodHandle));
        }
    }

    if (src)
    {
        // store into frame.m_Datum which offset is given by offsetOfCallTarget
        GenTreeLclVarCommon* store = new(comp, GT_STORE_LCL_FLD)
            GenTreeLclFld(GT_STORE_LCL_FLD,
            TYP_I_IMPL,
            comp->lvaInlinedPInvokeFrameVar,
            callFrameInfo.offsetOfCallTarget);
        store->gtOp1 = src;
        comp->fgInsertTreeBeforeAsEmbedded(store, insertBefore, comp->compCurStmt->AsStmt(), currBlock);
    }


#ifdef _TARGET_X86_

    // ----------------------------------------------------------------------------------
    // frame.callSiteTracker = SP

    GenTree* storeTracker = new(comp, GT_STORE_LCL_FLD)
        GenTreeLclFld(GT_STORE_LCL_FLD, TYP_I_IMPL,
                      comp->lvaInlinedPInvokeFrameVar,
                      callFrameInfo.offsetOfCallSiteSP);

    storeTracker->AsLclVarCommon()->gtOp1 = PhysReg(REG_SPBASE);

    comp->fgInsertTreeBeforeAsEmbedded(storeTracker, insertBefore, comp->compCurStmt->AsStmt(), currBlock);
    DISPTREE(comp->compCurStmt);

#endif

    // ----------------------------------------------------------------------------------
    // frame.callSiteReturnAddress = &label (the point immediately after call)

    GenTree* storeLab = new(comp, GT_STORE_LCL_FLD)
        GenTreeLclFld(GT_STORE_LCL_FLD, TYP_I_IMPL,
                      comp->lvaInlinedPInvokeFrameVar,
                      callFrameInfo.offsetOfReturnAddress);


    // we don't have a real label, and inserting one is hard (even if we made a special node)
    // so for now we will just 'know' what this means in codegen
    GenTreeLabel* labelRef = new(comp, GT_LABEL) GenTreeLabel(nullptr);
    labelRef->gtType = TYP_I_IMPL;
    storeLab->AsLclVarCommon()->gtOp1 = labelRef;

    comp->fgInsertTreeBeforeAsEmbedded(storeLab, insertBefore, comp->compCurStmt->AsStmt(), currBlock);

    if (!(comp->opts.eeFlags & CORJIT_FLG_IL_STUB))
    {
        // Set the TCB's frame to be the one we just created.
        // Note the init routine for the InlinedCallFrame (CORINFO_HELP_INIT_PINVOKE_FRAME) 
        // has prepended it to the linked list to maintain the stack of Frames.
        //
        // Stubs do this once per stub, not once per call
        GenTree* frameUpd = CreateFrameLinkUpdate(PushFrame);

        comp->fgInsertTreeBeforeAsEmbedded(frameUpd, insertBefore, comp->compCurStmt->AsStmt(), currBlock);
    }

    // IMPORTANT **** This instruction must come last!!! ****
    // It changes the thread's state to Preemptive mode
    // ----------------------------------------------------------------------------------
    //  [tcb + offsetOfGcState] = 0

    GenTree* storeGCState = SetGCState(0);

    comp->fgInsertTreeBeforeAsEmbedded(storeGCState, insertBefore, comp->compCurStmt->AsStmt(), currBlock);
}

// insert the code that goes after every inlined pinvoke call
void Lowering::InsertPInvokeCallEpilog(GenTreeCall* call)
{
#if COR_JIT_EE_VERSION > 460
    if (comp->opts.ShouldUsePInvokeHelpers())
    {
        noway_assert(comp->lvaInlinedPInvokeFrameVar != BAD_VAR_NUM);

        // First argument is the address of the frame variable.
        GenTree* frameAddr = new(comp, GT_LCL_VAR)
            GenTreeLclVar(GT_LCL_VAR, TYP_BYREF, comp->lvaInlinedPInvokeFrameVar, BAD_IL_OFFSET);
        frameAddr->gtOper = GT_LCL_VAR_ADDR;

        // Insert call to CORINFO_HELP_JIT_PINVOKE_END
        GenTree* helperCall = comp->gtNewHelperCallNode(CORINFO_HELP_JIT_PINVOKE_END, TYP_VOID, 0, comp->gtNewArgList(frameAddr));

        comp->fgMorphTree(helperCall);
        comp->fgInsertTreeAfterAsEmbedded(helperCall, call, comp->compCurStmt->AsStmt(), currBlock);
        return;
    }
#endif

    CORINFO_EE_INFO* pInfo = comp->eeGetEEInfo();
    GenTreeStmt* newStmt;
    GenTreeStmt* topStmt = comp->compCurStmt->AsStmt();

    // gcstate = 1
    GenTree* latest = call;
    GenTree* tree = SetGCState(1);
    newStmt = comp->fgInsertTreeAfterAsEmbedded(tree, latest, topStmt, currBlock);
    latest = tree;
    if (newStmt->gtStmtIsTopLevel())
    {
        topStmt = newStmt;
    }

    tree = CreateReturnTrapSeq();
    newStmt = comp->fgInsertTreeAfterAsEmbedded(tree, latest, topStmt, currBlock);
    latest = tree;
    if (newStmt->gtStmtIsTopLevel())
    {
        topStmt = newStmt;
    }

    // Pop the frame
    if (!(comp->opts.eeFlags & CORJIT_FLG_IL_STUB))
    {
        GenTree* frameUpd = CreateFrameLinkUpdate(PopFrame);

        newStmt = comp->fgInsertTreeAfterAsEmbedded(frameUpd, latest, topStmt, currBlock);
        latest = frameUpd;
        if (newStmt->gtStmtIsTopLevel())
            topStmt = newStmt;
    }
}

GenTree* Lowering::LowerNonvirtPinvokeCall(GenTreeCall* call)
{
    //------------------------------------------------------
    // Non-virtual/Indirect calls: PInvoke calls.

    // PInvoke lowering varies depending on the flags passed in by the EE. By default,
    // GC transitions are generated inline; if CORJIT_FLG2_USE_PINVOKE_HELPERS is specified,
    // GC transitions are instead performed using helper calls. Examples of each case are given
    // below. Note that the data structure that is used to store information about a call frame
    // containing any P/Invoke calls is initialized in the method prolog (see
    // InsertPInvokeMethod{Prolog,Epilog} for details).
    //
    // Inline transitions:
    //     InlinedCallFrame inlinedCallFrame;
    //
    //     ...
    //
    //     // Set up frame information
    //     inlinedCallFrame.callTarget = methodHandle;
    //     inlinedCallFrame.callSiteTracker = SP; (x86 only)
    //     inlinedCallFrame.callSiteReturnAddress = &label; (the address of the instruction immediately following the call)
    //     thread->m_pFrame = &inlinedCallFrame; (non-IL-stub only)
    //
    //     // Switch the thread's GC mode to preemptive mode
    //     thread->m_fPreemptiveGCDisabled = 0;
    //
    //     // Call the unmanged method
    //     target();
    //
    //     // Switch the thread's GC mode back to cooperative mode
    //     thread->m_fPreemptiveGCDisabled = 1;
    //
    //     // Rendezvous with a running collection if necessary
    //     if (g_TrapReturningThreads)
    //         RareDisablePreemptiveGC();
    //
    // Transistions using helpers:
    //
    //     OpaqueFrame opaqueFrame;
    //
    //     ...
    //
    //     // Call the JIT_PINVOKE_BEGIN helper
    //     JIT_PINVOKE_BEGIN(&opaqueFrame);
    //
    //     // Call the unmanaged method
    //     target();
    //
    //     // Call the JIT_PINVOKE_END helper
    //     JIT_PINVOKE_END(&opaqueFrame);
    //
    // Note that the JIT_PINVOKE_{BEGIN.END} helpers currently use the default calling convention for the target platform.
    // They may be changed in the near future s.t. they preserve all register values.

    GenTree* result = nullptr;
    void* addr = nullptr;

    // assert we have seen one of these
    noway_assert(comp->info.compCallUnmanaged != 0);

    // All code generated by this function must not contain the randomly-inserted NOPs
    // that we insert to inhibit JIT spraying in partial trust scenarios.
    // The PINVOKE_PROLOG op signals this to the code generator/emitter.

    GenTree* prolog = new (comp, GT_NOP) GenTree(GT_PINVOKE_PROLOG, TYP_VOID);
    comp->fgInsertTreeBeforeAsEmbedded(prolog, call, comp->compCurStmt->AsStmt(), currBlock);

    InsertPInvokeCallProlog(call);

    if (call->gtCallType != CT_INDIRECT)
    {
        noway_assert(call->gtCallType == CT_USER_FUNC);
        CORINFO_METHOD_HANDLE methHnd  = call->gtCallMethHnd;

        CORINFO_CONST_LOOKUP lookup;
#if COR_JIT_EE_VERSION > 460
        comp->info.compCompHnd->getAddressOfPInvokeTarget(methHnd, &lookup);
#else
        void* pIndirection;
        lookup.accessType = IAT_PVALUE;
        lookup.addr = comp->info.compCompHnd->getAddressOfPInvokeFixup(methHnd, &pIndirection);
        if (lookup.addr == nullptr)
        {
            lookup.accessType = IAT_PPVALUE;
            lookup.addr = pIndirection;
        }
#endif

        GenTree* address = AddrGen(lookup.addr);
        switch (lookup.accessType)
        {
            case IAT_VALUE:
                result = address;
                break;

            case IAT_PVALUE:
                result = Ind(address);
                break;

            case IAT_PPVALUE:
                result = Ind(Ind(address));
                break;
        }
    }

    InsertPInvokeCallEpilog(call);

    return result;
}

// Expand the code necessary to calculate the control target.  
// Returns: the expression needed to calculate the control target
// May insert embedded statements
GenTree* Lowering::LowerVirtualVtableCall(GenTreeCall* call)
{
    noway_assert(call->gtCallType == CT_USER_FUNC);    

    // If this is a tail call via helper, thisPtr will be the third argument.
    int thisPtrArgNum;
    regNumber thisPtrArgReg;
    if (call->IsTailCallViaHelper())
    {
        thisPtrArgNum = 2;
#ifdef _TARGET_X86_
        NYI("Tail call via helper for x86");
        thisPtrArgReg = REG_NA;
#else // !_TARGET_X86_
        thisPtrArgReg = REG_ARG_2;
#endif // !_TARGET_X86_
    }
    else
    {
        thisPtrArgNum = 0;
        thisPtrArgReg = comp->codeGen->genGetThisArgReg(call);
    }

    // get a reference to the thisPtr being passed
    fgArgTabEntry* argEntry = comp->gtArgEntryByArgNum(call, thisPtrArgNum);
    assert(argEntry->regNum == thisPtrArgReg);
    assert(argEntry->node->gtOper == GT_PUTARG_REG);
    GenTree *thisPtr = argEntry->node->gtOp.gtOp1;

    // If what we are passing as the thisptr is not already a local, make a new local to place it in
    // because we will be creating expressions based on it.
    unsigned lclNum;
    if (thisPtr->IsLocal())
    {
        lclNum = thisPtr->gtLclVarCommon.gtLclNum;
    }
    else
    {
        // Split off the thisPtr and store to a temporary variable.
        if (vtableCallTemp == BAD_VAR_NUM)
        {
            vtableCallTemp = comp->lvaGrabTemp(true DEBUGARG("temp for virtual vtable call"));
        }
        GenTreeStmt* newStmt = comp->fgInsertEmbeddedFormTemp(&(argEntry->node->gtOp.gtOp1), vtableCallTemp);
        newStmt->gtFlags |= GTF_STMT_SKIP_LOWER; // we're in postorder so we have already processed this subtree
        GenTree* stLclVar = newStmt->gtStmtExpr;
        assert(stLclVar->OperIsLocalStore());
        lclNum = stLclVar->gtLclVar.gtLclNum;
    }

    // We'll introduce another use of this local so increase its ref count.
    comp->lvaTable[lclNum].incRefCnts(comp->compCurBB->getBBWeight(comp), comp);

    // If the thisPtr is a local field, then construct a local field type node
    GenTree* local;
    if (thisPtr->isLclField())
    {
        local = new(comp, GT_LCL_FLD) GenTreeLclFld(GT_LCL_FLD, thisPtr->TypeGet(), lclNum, thisPtr->AsLclFld()->gtLclOffs);
    }
    else
    {
        local = new(comp, GT_LCL_VAR) GenTreeLclVar(GT_LCL_VAR, thisPtr->TypeGet(), lclNum, BAD_IL_OFFSET);   
    }

    // pointer to virtual table = [REG_CALL_THIS + offs]
    GenTree* result = Ind(Offset(local, VPTR_OFFS));

    // Get hold of the vtable offset (note: this might be expensive)
    unsigned vtabOffsOfIndirection;
    unsigned vtabOffsAfterIndirection;    
    comp->info.compCompHnd->getMethodVTableOffset(call->gtCallMethHnd, &vtabOffsOfIndirection, &vtabOffsAfterIndirection);

    // Get the appropriate vtable chunk
    // result = [REG_CALL_IND_SCRATCH + vtabOffsOfIndirection]
    result = Ind(Offset(result, vtabOffsOfIndirection));

    // Load the function address
    // result = [reg+vtabOffs]
    result = Ind(Offset(result, vtabOffsAfterIndirection));

    return result;
}

// Lower stub dispatched virtual calls.
GenTree* Lowering::LowerVirtualStubCall(GenTreeCall* call)
{
    assert((call->gtFlags & GTF_CALL_VIRT_KIND_MASK) == GTF_CALL_VIRT_STUB);

    GenTree* result = nullptr;

    // An x86 JIT which uses full stub dispatch must generate only
    // the following stub dispatch calls:
    //
    // (1) isCallRelativeIndirect:
    //        call dword ptr [rel32]  ;  FF 15 ---rel32----
    // (2) isCallRelative:
    //        call abc                ;     E8 ---rel32----
    // (3) isCallRegisterIndirect:
    //     3-byte nop                 ;
    //     call dword ptr [eax]       ;     FF 10
    //
    // THIS IS VERY TIGHTLY TIED TO THE PREDICATES IN
    // vm\i386\cGenCpu.h, esp. isCallRegisterIndirect.
    
#ifdef _TARGET_64BIT_
    // Non-tail calls: Jump Stubs are not taken into account by VM for mapping an AV into a NullRef
    // exception. Therefore, JIT needs to emit an explicit null check.  Note that Jit64 too generates
    // an explicit null check.
    //
    // Tail calls: fgMorphTailCall() materializes null check explicitly and hence no need to emit
    // null check.

    // Non-64-bit: No need to null check the this pointer - the dispatch code will deal with this.
    // The VM considers exceptions that occur in stubs on 64-bit to be not managed exceptions and
    // it would be difficult to change this in a way so that it affects only the right stubs.

    if (!call->IsTailCallViaHelper())
    {
        call->gtFlags |= GTF_CALL_NULLCHECK;
    }
#endif

    // TODO-Cleanup: Disable emitting random NOPs

    // This is code to set up an indirect call to a stub address computed
    // via dictionary lookup.  
    if (call->gtCallType == CT_INDIRECT)
    {
        NYI_X86("Virtual Stub dispatched call lowering via dictionary lookup");
        
        // The importer decided we needed a stub call via a computed
        // stub dispatch address, i.e. an address which came from a dictionary lookup.
        //   - The dictionary lookup produces an indirected address, suitable for call
        //     via "call [REG_VIRTUAL_STUB_PARAM]"
        //
        // This combination will only be generated for shared generic code and when
        // stub dispatch is active.

        // fgMorphArgs will have created trees to pass the address in REG_VIRTUAL_STUB_PARAM.
        // All we have to do here is add an indirection to generate the actual call target.

        GenTree* ind = Ind(call->gtCallAddr);
        call->gtCallAddr->InsertAfterSelf(ind);
        call->gtCallAddr = ind;
    }
    else
    {
        // Direct stub call.
        // Get stub addr. This will return NULL if virtual call stubs are not active
        void* stubAddr = call->gtStubCallStubAddr;
        noway_assert(stubAddr != nullptr);

        // If not CT_INDIRECT,  then it should always be relative indir call.
        // This is ensured by VM.
        noway_assert(call->IsVirtualStubRelativeIndir());

        // Direct stub calls, though the stubAddr itself may still need to be
        // accesed via an indirection.        
        GenTree* addr = AddrGen(stubAddr);
        GenTree* indir = Ind(addr);

        // On x86 we generate this:
        //        call dword ptr [rel32]  ;  FF 15 ---rel32----
        // So we don't use a register.
#ifndef _TARGET_X86_
        // on x64 we must materialize the target using specific registers.
        addr->gtRegNum = REG_VIRTUAL_STUB_PARAM;
        indir->gtRegNum = REG_JUMP_THUNK_PARAM;
#endif
        result = indir;
    }

    // TODO-Cleanup: start emitting random NOPS
    return result;
}


//------------------------------------------------------------------------
// LowerIndCleanupHelper: Remove the nodes that are no longer used after an
// addressing mode is constructed
//
// Arguments:
//    addrMode - A pointer to a new GenTreeAddrMode
//    tree     - The tree currently being considered to removal
//
// Return Value:
//    None.
//
// Assumptions:
//    'addrMode' and 'tree' must be contained in comp->compCurStmt

void Lowering::LowerIndCleanupHelper(GenTreeAddrMode* addrMode, GenTreePtr tree)
{
    if (tree == addrMode->Base() || tree == addrMode->Index()) return;
    unsigned childCount = tree->NumChildren();
    for (unsigned i = 0; i < childCount; i++)
    {
        LowerIndCleanupHelper(addrMode, tree->GetChild(i));
    }
    Compiler::fgSnipNode(comp->compCurStmt->AsStmt(), tree);
}

// given two nodes which will be used in an addressing mode (src1, src2)
// walk backwards from the use to those nodes to determine if they are
// potentially modified in that range
//
// returns: true if the sources given may be modified before they are used
bool Lowering::AreSourcesPossiblyModified(GenTree* use, GenTree* src1, GenTree* src2)
{
    GenTree* cursor = use;
    GenTree* firstTree = comp->compCurStmt->AsStmt()->gtStmtList;

    while (cursor && cursor != firstTree)
    {
        cursor = cursor->gtPrev;

        if (cursor == src1)
            src1 = nullptr;
        if (cursor == src2)
            src2 = nullptr;
        if (src2 == nullptr && src1 == nullptr)
            return false;

        if (src1 && comp->fgNodesMayInterfere(src1, cursor))
        {
            return true;
        }

        if (src2 && comp->fgNodesMayInterfere(src2, cursor))
        {
            return true;
        }
        
    }
    assert(!"ran off beginning of stmt\n");
    return true;
}

//------------------------------------------------------------------------
// LowerAddrMode: recognize trees which can be implemented using an addressing
//    mode and transform them to a GT_LEA
//
// Arguments:
//    pTree:   pointer to the parent node's link to the node we care about
//    before:  node to insert the new GT_LEA before
//    data:    fgWalkData which is used to get info about parents and fixup call args
//    isIndir: true if this addressing mode is the child of an indir
//
void Lowering::LowerAddrMode(GenTreePtr* pTree, GenTree* before, Compiler::fgWalkData* data, bool isIndir)
{
    GenTree*    addr   = *pTree;
    GenTreePtr  base   = nullptr;
    GenTreePtr  index  = nullptr;
    unsigned    scale  = 0;
    unsigned    offset = 0;
    bool        rev    = false;

    // If it's not an indir, we need the fgWalkData to get info about the parent.
    assert(isIndir || data);

    // Find out if an addressing mode can be constructed
    bool doAddrMode =
        comp->codeGen->genCreateAddrMode(addr, -1, true, 0, &rev, &base, &index, &scale, &offset, true /*nogen*/);

    if (scale == 0)
        scale = 1;

    if (!isIndir)
    {
        // this is just a reg-const add
        if (index == nullptr)
        {
            return;
        }

        // this is just a reg-reg add
        if (scale == 1 && offset == 0)
        {
            return;
        }
    }

    // make sure there are not any side effects between def of leaves and use
    if (doAddrMode && !AreSourcesPossiblyModified(addr, base, index))
    {
        GenTreePtr arrLength = NULL;

        JITDUMP("Addressing mode:\n");
        JITDUMP("  Base\n");
        DISPNODE(base);
        if (index != nullptr)
        {
            JITDUMP("  + Index * %u + %u\n", scale, offset);
            DISPNODE(index);
        }
        else
        {
            JITDUMP("  + %u\n", offset);
        }

        var_types addrModeType = addr->TypeGet();
        if (addrModeType == TYP_REF) 
            addrModeType = TYP_BYREF;

        GenTreeAddrMode* addrMode =
            new(comp, GT_LEA) GenTreeAddrMode(addrModeType, base, index, scale, offset);
        
        addrMode->CopyCosts(addr);
        addrMode->gtRsvdRegs = addr->gtRsvdRegs;
        addrMode->gtFlags   |= (addr->gtFlags & (GTF_ALL_EFFECT | GTF_IND_FLAGS));

        JITDUMP("New addressing mode node:\n");
        DISPNODE(addrMode);
        JITDUMP("\n");

        // Required to prevent assert failure:
        //    Assertion failed 'op1 && op2' in flowgraph.cpp, Line: 34431
        // when iterating the operands of a GT_LEA
        // Test Case: self_host_tests_amd64\jit\jit64\opt\cse\VolatileTest_op_mul.exe
        //    Method: TestCSE:.cctor
        // The method genCreateAddrMode() above probably should be fixed
        //    to not return rev=true, when index is returned as NULL
        //
        if (rev && index == nullptr)
        {
            rev = false;
        }

        if (rev)
        {
            addrMode->gtFlags |= GTF_REVERSE_OPS;
        }
        else
        {
            addrMode->gtFlags &= ~(GTF_REVERSE_OPS);
        }

        comp->fgInsertLinearNodeBefore(addrMode, before);

        // Now we need to snip from the linear order all the nodes subsumed by the addrMode
        LowerIndCleanupHelper(addrMode, addr);

        GenTree* old = *pTree;
        *pTree = addrMode;

        if (!isIndir)
        {
            // this could be an arg to a call
            comp->fgFixupIfCallArg(data->parentStack, old, addrMode);
        }
    }
    else
    {
        JITDUMP("  No addressing mode\n");
    }
}

//------------------------------------------------------------------------
// LowerAdd: turn this add into a GT_LEA if that would be profitable
//
// Arguments:
//    pTree:   pointer to the parent node's link to the node we care about
//    data:    fgWalkData which is used to get info about parents and fixup call args

void Lowering::LowerAdd(GenTreePtr* pTree, Compiler::fgWalkData* data)
{
    GenTreePtr newNode = NULL;

    GenTreePtr addr = *pTree;

#ifdef _TARGET_ARMARCH_
    // For ARM architectures we don't have the LEA instruction
    // therefore we won't get much benefit from doing this.
    return;
#else // _TARGET_ARMARCH_
    if (data->parentStack->Height() < 2)
        return;
    
    // if this is a child of an indir, let the parent handle it
    if (data->parentStack->Index(1)->OperIsIndir())
        return;

    // if there is a chain of adds, only look at the topmost one
    if (data->parentStack->Index(1)->gtOper == GT_ADD)
        return;

    if (!varTypeIsIntegralOrI(addr))
        return;

    LowerAddrMode(pTree, addr, data, false);
#endif // !_TARGET_ARMARCH_
}


//------------------------------------------------------------------------
// LowerInd: attempt to transform indirected expression into an addressing mode
//
// Arguments:
//    pTree:   pointer to the parent node's link to the node we care about

void Lowering::LowerInd(GenTreePtr* pTree)
{
    GenTreePtr newNode = NULL;
    GenTreePtr cTree = *pTree;

    JITDUMP("\n");
    DISPNODE(cTree);

    GenTreePtr addr = cTree->gtOp.gtOp1;

    GenTreePtr before = cTree;
    if (cTree->OperGet() == GT_STOREIND && !cTree->IsReverseOp())
        before = comp->fgGetFirstNode(cTree->gtGetOp2());

    LowerAddrMode(&cTree->gtOp.gtOp1, before, nullptr, true);

    // Mark all GT_STOREIND nodes to indicate that it is not known
    // whether it represents a RMW memory op.  
    if (cTree->OperGet() == GT_STOREIND)
    {
        cTree->AsStoreInd()->SetRMWStatusDefault();
    }
}

//------------------------------------------------------------------------
// LowerArrElem: Lower a GT_ARR_ELEM node
//
// Arguments:
//    pTree - pointer to the field in the parent node that holds the pointer to the GT_ARR_ELEM node.
//
// Return Value:
//    None.
//
// Assumptions:
//    pTree points to a pointer to a GT_ARR_ELEM node.
//
// Notes:
//    This performs the following lowering.  We start with a node of the form:
//          /--*  <arrObj>
//          +--*  <index0>
//          +--*  <index1>
//       /--*  arrMD&[,]
//
//    First, we create temps for arrObj if it is not already a lclVar, and for any of the index
//    expressions that have side-effects.
//    We then transform the tree into:
//                      <offset is null - no accumulated offset for the first index>
//                   /--*  <arrObj>
//                   +--*  <index0>
//                /--*  ArrIndex[i, ]
//                +--*  <arrObj>
//             /--|  arrOffs[i, ]
//             |  +--*  <arrObj>
//             |  +--*  <index1>
//             +--*  ArrIndex[*,j]
//             +--*  <arrObj>
//          /--|  arrOffs[*,j]
//          +--*  lclVar NewTemp
//       /--*  lea (scale = element size, offset = offset of first element)
//
//    The new stmtExpr may be omitted if the <arrObj> is a lclVar.
//    The new stmtExpr may be embedded if the <arrObj> is not the first tree in linear order for
//    the statement containing the original arrMD.
//    Note that the arrMDOffs is the INDEX of the lea, but is evaluated before the BASE (which is the second
//    reference to NewTemp), because that provides more accurate lifetimes.
//    There may be 1, 2 or 3 dimensions, with 1, 2 or 3 arrMDIdx nodes, respectively.

void
Lowering::LowerArrElem(GenTree **ppTree, Compiler::fgWalkData* data)
{
    GenTreePtr      tree = *ppTree;
    // This will assert if we don't have an ArrElem node
    GenTreeArrElem* arrElem = tree->AsArrElem();
    Compiler*       comp = data->compiler;
    GenTreePtr      curStmt = comp->compCurStmt;
    unsigned char   rank = arrElem->gtArrElem.gtArrRank;

    JITDUMP("Lowering ArrElem\n");
    JITDUMP("============\n");
    DISPTREE(arrElem);
    JITDUMP("\n");

    assert(arrElem->gtArrObj->TypeGet() == TYP_REF);

    // We need to have the array object in a lclVar.
    if (!arrElem->gtArrObj->IsLocal())
    {
        // Split off the array object and store to a temporary variable.
        GenTreeStmt* newStmt = comp->fgInsertEmbeddedFormTemp(&(arrElem->gtArrObj));
        newStmt->gtFlags |= GTF_STMT_SKIP_LOWER;
        GenTreePtr  stLclVar = newStmt->gtStmtExpr;
        assert(stLclVar->OperIsLocalStore());

        // If we have made a new top-level statement, and it has inherited any
        // embedded statements from curStmt, they have not yet been lowered.
        if (newStmt->gtStmtIsTopLevel())
        {
            for (GenTreePtr nextEmbeddedStmt = newStmt->gtStmtNextIfEmbedded();
                 nextEmbeddedStmt != nullptr;
                 nextEmbeddedStmt = nextEmbeddedStmt->gtStmt.gtStmtNextIfEmbedded())
            {
                comp->compCurStmt = nextEmbeddedStmt;
                comp->fgWalkTreePost(&nextEmbeddedStmt->gtStmt.gtStmtExpr, &Lowering::LowerNodeHelper, this, true);
                nextEmbeddedStmt->gtFlags |= GTF_STMT_SKIP_LOWER;
            }
        }
        // Restore curStmt.
        comp->compCurStmt = curStmt;
    }
    GenTreePtr arrObjNode = arrElem->gtArrObj;
    assert(arrObjNode->IsLocal());

    GenTreePtr nextNode = arrElem;

    // We need to evaluate the index expressions up-front if they have side effects.
    for (unsigned char dim = 0; dim < rank; dim++)
    {
        GenTree* currIndexNode = arrElem->gtArrElem.gtArrInds[dim];
        assert(varTypeIsIntegral(currIndexNode->TypeGet()));
        if ((currIndexNode->gtFlags & GTF_SIDE_EFFECT) != 0)
        {
            // Split off this index computation and store to a temporary variable.
            GenTreeStmt* newStmt = comp->fgInsertEmbeddedFormTemp(&(arrElem->gtArrElem.gtArrInds[dim]));
            GenTreePtr stLclVar = newStmt->gtStmtExpr;
            assert(stLclVar->OperIsLocalStore());
            // We can't have made a new top-level statement, because we know we've got an ArrObj
            // prior to the index nodes.
            assert(newStmt->gtStmtIsEmbedded());
            newStmt->gtFlags |= GTF_STMT_SKIP_LOWER;
            // Restore curStmt (we've already lowered the tree we just split off).
            comp->compCurStmt = curStmt;
        }
    }

    // The first ArrOffs node will have 0 for the offset of the previous dimension.
    GenTree* prevArrOffs = new(comp, GT_CNS_INT) GenTreeIntCon(TYP_I_IMPL, 0);
    comp->fgInsertLinearNodeBefore(prevArrOffs, arrObjNode);

    for (unsigned char dim = 0; dim < rank; dim++)
    {
        GenTree* currIndexTree = arrElem->gtArrElem.gtArrInds[dim];
        GenTree* insertBeforeNode = nextNode;

        // Use the original arrObjNode on the 0th ArrIndex node, and clone it for subsequent ones.
        GenTreePtr idxArrObjNode;
        if (dim == 0)
        {
            idxArrObjNode = arrObjNode;
        }
        else
        {
            idxArrObjNode = comp->gtClone(arrObjNode);
            comp->fgInsertLinearNodeBefore(idxArrObjNode, nextNode);
        }

        // Move the index (temp created above, or non-side-effect computation) if needed.
        // (All side-effecting computations we've split above need to come before the GT_ARR_INDEX nodes.)
        if (currIndexTree->gtNext != insertBeforeNode)
        {
            GenTree* firstIndexNode = comp->fgGetFirstNode(currIndexTree);
            GenTree* oldPrevNode = firstIndexNode->gtPrev;
            GenTree* oldNextNode = currIndexTree->gtNext;
            GenTree* newPrevNode = insertBeforeNode->gtPrev;
            // All these are inner nodes, so they cannot be null.
            assert(oldPrevNode != nullptr && oldNextNode != nullptr && newPrevNode != nullptr);

            oldPrevNode->gtNext = oldNextNode;
            oldNextNode->gtPrev = oldPrevNode;

            firstIndexNode->gtPrev = newPrevNode;
            newPrevNode->gtNext = firstIndexNode;

            currIndexTree->gtNext = insertBeforeNode;
            insertBeforeNode->gtPrev = currIndexTree;
        }

        // Next comes the GT_ARR_INDEX node.
        GenTreeArrIndex* arrMDIdx = new(comp, GT_ARR_INDEX)
            GenTreeArrIndex(TYP_INT, idxArrObjNode, currIndexTree, dim, rank, arrElem->gtArrElem.gtArrElemType);
        arrMDIdx->gtFlags |= ((idxArrObjNode->gtFlags|currIndexTree->gtFlags) & GTF_ALL_EFFECT);
        comp->fgInsertLinearNodeBefore(arrMDIdx, insertBeforeNode);

        GenTree* offsArrObjNode = comp->gtClone(arrObjNode);
        comp->fgInsertLinearNodeBefore(offsArrObjNode, insertBeforeNode);

        GenTreeArrOffs* arrOffs = new(comp, GT_ARR_OFFSET)
            GenTreeArrOffs(TYP_I_IMPL, prevArrOffs, arrMDIdx, offsArrObjNode, dim, rank, arrElem->gtArrElem.gtArrElemType);
        comp->fgInsertLinearNodeBefore(arrOffs, insertBeforeNode);
        arrOffs->gtFlags |= ((prevArrOffs->gtFlags|arrMDIdx->gtFlags|offsArrObjNode->gtFlags) & GTF_ALL_EFFECT);

        prevArrOffs = arrOffs;
    }

    // Generate the LEA and make it reverse evaluation, because we want to evaluate the index expression before the base.
    GenTreePtr leaBase = comp->gtClone(arrObjNode);
    unsigned scale = arrElem->gtArrElem.gtArrElemSize;
    // Note that below we're specifically using genTypeSize(TYP_INT) because array indices are not native int.
    unsigned offset = comp->eeGetArrayDataOffset(arrElem->gtArrElem.gtArrElemType) + 2 * genTypeSize(TYP_INT) * arrElem->gtArrElem.gtArrRank;
    GenTreePtr leaIndexNode = prevArrOffs;
    if (!jitIsScaleIndexMul(scale))
    {
        // We do the address arithmetic in TYP_I_IMPL, though note that the lower bounds and lengths in memory are TYP_INT
        GenTreePtr scaleNode = new(comp, GT_CNS_INT) GenTreeIntCon(TYP_I_IMPL, scale);
        GenTreePtr mulNode = new(comp, GT_MUL) GenTreeOp(GT_MUL, TYP_I_IMPL, leaIndexNode, scaleNode); 
        comp->fgInsertLinearNodeBefore(scaleNode, nextNode);
        comp->fgInsertLinearNodeBefore(mulNode, nextNode);
        leaIndexNode = mulNode;
        scale = 1;
    }
    comp->fgInsertLinearNodeBefore(leaBase, nextNode);
    GenTreePtr leaNode = new(comp, GT_LEA) GenTreeAddrMode(arrElem->TypeGet(), leaBase, leaIndexNode, scale, offset);
    leaNode->gtFlags |= GTF_REVERSE_OPS;
    comp->fgInsertLinearNodeBefore(leaNode, nextNode);

    *ppTree = leaNode;

    if (arrElem->gtNext != nullptr)
    {
        comp->fgSnipInnerNode(arrElem);
    }
    else
    {
        // We can have a top-level GT_ARR_ELEM. For example, a function call
        // with a parameter of GT_ARR_ELEM can end up being simplified by the
        // inliner to single GT_ARR_ELEM node if the function has an empty body.
        arrElem->gtPrev->gtNext = nullptr;
        curStmt->gtStmt.gtStmtExpr = *ppTree;
    }

    // Update the costs.
    comp->gtSetStmtInfo(curStmt);

    JITDUMP("Results of lowering ArrElem:\n");
    DISPTREE(leaNode);
    JITDUMP("\nResulting statement:\n");
    DISPTREE(curStmt);
    JITDUMP("\n\n");
}

void Lowering::DoPhase()
{
#if 0
    // The code in this #if can be used to debug lowering issues according to
    // method hash.  To use, simply set environment variables lowerhashlo and lowerhashhi
#ifdef DEBUG
    unsigned methHash = info.compMethodHash();
    char* lostr = getenv("lowerhashlo");
    unsigned methHashLo = 0;
    if (lostr != NULL)
    {
        sscanf_s(lostr, "%x", &methHashLo);
    }
    char* histr = getenv("lowerhashhi");
    unsigned methHashHi = UINT32_MAX;
    if (histr != NULL)
    {
        sscanf_s(histr, "%x", &methHashHi);
    }
    if (methHash < methHashLo || methHash > methHashHi)
        return;
    else
    {
        printf("Lowering for method %s, hash = 0x%x.\n",
               info.compFullName, info.compMethodHash());
        printf("");         // in our logic this causes a flush
    }
#endif
#endif

#if !defined(_TARGET_64BIT_)
    comp->lvaPromoteLongVars();
#endif // !defined(_TARGET_64BIT_)

#ifdef DEBUG
    unsigned stmtNum = 0;
#endif
    for (BasicBlock* block = comp->fgFirstBB; block; block = block->bbNext)
    {
        GenTreePtr      stmt;

        /* Make the block publicly available */
        currBlock = block;
        comp->compCurBB = block;

        /* Walk the statement trees in this basic block */
        for (stmt = block->bbTreeList; stmt; stmt = stmt->gtNext)
        {
            if (stmt->gtFlags & GTF_STMT_SKIP_LOWER)
            {
                continue;
            }
#ifdef DEBUG
            ++stmtNum;
            if  (comp->verbose)
            {
                // This is a useful location for a conditional breakpoint in Visual Studio (i.e. when stmtNum == 15)
                printf("Lowering BB%02u, stmt %u\n", block->bbNum, stmtNum); 
            }
#endif
            comp->compCurStmt = stmt;
#if !defined(_TARGET_64BIT_)
            comp->fgWalkTreePost(&stmt->gtStmt.gtStmtExpr, &Lowering::DecompNodeHelper, this, true);
#endif
            comp->fgWalkTreePost(&stmt->gtStmt.gtStmtExpr, &Lowering::LowerNodeHelper, this, true);
            // We may have removed "stmt" in LowerNode().
            stmt = comp->compCurStmt;
       }
    }

#if INLINE_PINVOKE
    // If we have any PInvoke calls, insert the one-time prolog code. We've already inserted the epilog code in the appropriate spots.
    // NOTE: there is a minor optimization opportunity here, as we still create p/invoke data structures and setup/teardown
    // even if we've eliminated all p/invoke calls due to dead code elimination.
    if (comp->info.compCallUnmanaged)
    {
        InsertPInvokeMethodProlog();
    }
#endif

#ifdef DEBUG
    JITDUMP("Lower has completed modifying nodes, proceeding to initialize LSRA TreeNodeInfo structs...\n");
    if (VERBOSE)
    {
        comp->fgDispBasicBlocks(true);
    }
#endif

    // TODO-Throughput: We re-sort local variables to get the goodness of enregistering recently
    // introduced local variables both by Rationalize and Lower; downside is we need to
    // recompute standard local variable liveness in order to get Linear CodeGen working.
    // For now we'll take the throughput hit of recomputing local liveness but in the long term
    // we're striving to use the unified liveness computation (fgLocalVarLiveness) and stop
    // computing it separately in LSRA.
    if (comp->lvaCount != 0)
    {
        comp->lvaSortAgain = true;
    }
    comp->EndPhase(PHASE_LOWERING_DECOMP);

    comp->fgLocalVarLiveness();
    // local var liveness can delete code, which may create empty blocks
    if  (!comp->opts.MinOpts() && !comp->opts.compDbgCode)
    {
        comp->optLoopsMarked = false;
        bool modified = comp->fgUpdateFlowGraph();
        if (modified || comp->lvaSortAgain)
        {
            JITDUMP("had to run another liveness pass:\n");
            comp->fgLocalVarLiveness();
        }
    }
#ifdef DEBUG
    JITDUMP("Liveness pass finished after lowering, IR:\n");
    JITDUMP("lvasortagain = %d\n", comp->lvaSortAgain);
    if (VERBOSE)
    {
        comp->fgDispBasicBlocks(true);
    }
#endif

    // The initialization code for the TreeNodeInfo map was initially part of a single full IR
    // traversal and it has been split because the order of traversal performed by fgWalkTreePost
    // does not necessarily lower nodes in execution order and also, it could potentially
    // add new BasicBlocks on the fly as part of the Lowering pass so the traversal won't be complete.
    //
    // Doing a new traversal guarantees we 'see' all new introduced trees and basic blocks allowing us
    // to correctly initialize all the data structures LSRA requires later on.
    // This code still has issues when it has to do with initialization of recently introduced locals by
    // lowering.  The effect of this is that any temporary local variable introduced by lowering won't be
    // enregistered yielding suboptimal CQ.
    // The reason for this is because we cannot re-sort the local variables per ref-count and bump of the number of
    // tracked variables just here because then LSRA will work with mismatching BitSets (i.e. BitSets with different
    // 'epochs' that were created before and after variable resorting, that will result in different number of tracked
    // local variables).
    //
    // The fix for this is to refactor this code to be run JUST BEFORE LSRA and not as part of lowering.
    // It's also desirable to avoid initializing this code using a non-execution order traversal.
    //
    LsraLocation currentLoc = 1;
    for( BasicBlock* block = m_lsra->startBlockSequence();
         block != nullptr;
         block = m_lsra->moveToNextBlock())
    {
        GenTreePtr stmt;

        // Increment the LsraLocation (currentLoc) at each BasicBlock.
        // This ensures that the block boundary (RefTypeBB, RefTypeExpUse and RefTypeDummyDef) RefPositions
        // are in increasing location order.
        currentLoc += 2;

        for (stmt = block->FirstNonPhiDef(); stmt; stmt = stmt->gtNext)
        {
            if (stmt->gtStmt.gtStmtIsEmbedded())
                continue;

            /* We increment the number position of each tree node by 2 to
            * simplify the logic when there's the case of a tree that implicitly
            * does a dual-definition of temps (the long case).  In this case
            * is easier to already have an idle spot to handle a dual-def instead
            * of making some messy adjustments if we only increment the
            * number position by one.
            */
            GenTreePtr node;
            foreach_treenode_execution_order(node, stmt)
            {
#ifdef DEBUG
                node->gtSeqNum = currentLoc;
#endif
                TreeNodeInfo* info = &node->gtLsraInfo;
                info->internalIntCount = 0;
                info->internalFloatCount = 0;
                info->isLocalDefUse = false;
                info->isHelperCallWithKills = false;
                info->isLsraAdded = false;

                // if there is a reg indicated on the tree node, use that for dstCandidates
                // the exception is the NOP, which sometimes show up around late args.
                // TODO-Cleanup: get rid of those NOPs.
                if (node->gtRegNum == REG_NA
                    || node->gtOper == GT_NOP)
                {
                    info->setDstCandidates(m_lsra, m_lsra->allRegs(node->TypeGet()));
                }
                else
                {
                    info->setDstCandidates(m_lsra, genRegMask(node->gtRegNum));
                }

                info->setSrcCandidates(m_lsra, info->getDstCandidates(m_lsra));
                info->setInternalCandidates(m_lsra, m_lsra->allRegs(TYP_INT));
                info->isInitialized = true;
                info->loc = currentLoc;
                node->gtClearReg(comp);
                currentLoc += 2;
            }
        }

        for (stmt = block->FirstNonPhiDef(); stmt; stmt = stmt->gtNext)
        {
            if (stmt->gtStmt.gtStmtIsEmbedded())
                continue;

            comp->compCurStmt = stmt;

            TreeNodeInfoInit(stmt);

            // In the special case where a comma node is at the top level, make it consume
            // its (op2) source
            GenTreePtr tree = stmt->gtStmt.gtStmtExpr;
            if (tree->gtOper == GT_COMMA && tree->TypeGet() != TYP_VOID)
            {
                tree->gtLsraInfo.srcCount = 1;
            }
            // In the special case where a lclVar node is at the top level, set it as
            // localDefUse
            // TODO-Cleanup: This used to be isCandidateLocalRef, but we haven't initialized the
            // lvLRACandidate field yet.  Fix this.
            else if (comp->optIsTrackedLocal(tree))
            {
                tree->gtLsraInfo.isLocalDefUse = true;
                tree->gtLsraInfo.dstCount = 0;
            }
#if 0
            // TODO-CQ: Enable this code after fixing the isContained() logic to not abort for these
            // top-level nodes that throw away their result.
            // If this is an interlocked operation that has a non-last-use lclVar as its op2,
            // make sure we allocate a target register for the interlocked operation.; otherwise we need
            // not allocate a register
            else if ((tree->OperGet() == GT_LOCKADD || tree->OperGet() == GT_XCHG || tree->OperGet() == GT_XADD))
            {
                tree->gtLsraInfo.dstCount = 0;
                if (tree->gtGetOp2()->IsLocal() && (tree->gtFlags & GTF_VAR_DEATH) == 0)
                    tree->gtLsraInfo.isLocalDefUse = true;
            }
#endif
        }
    }
    DBEXEC(VERBOSE, DumpNodeInfoMap());
}

/** Verifies if both of these trees represent the same indirection.
 * Used by Lower to annotate if CodeGen generate an instruction of the
 * form *addrMode BinOp= expr
 *
 * Preconditions: both trees are children of GT_INDs and their underlying children
 * have the same gtOper.
 *
 * This is a first iteration to actually recognize trees that can be code-generated
 * as a single read-modify-write instruction on AMD64/x86.  For now
 * this method only supports the recognition of simple addressing modes (through GT_LEA)
 * or local var indirections.  Local fields, array access and other more complex nodes are
 * not yet supported.
 *
 * TODO-CQ:  Perform tree recognition by using the Value Numbering Package, that way we can recognize
 * arbitrary complex trees and support much more addressing patterns.
 */
bool Lowering::IndirsAreEquivalent(GenTreePtr candidate, GenTreePtr storeInd)
{
    assert(candidate->OperGet() == GT_IND);
    assert(storeInd->OperGet()  == GT_STOREIND);
    
    // We should check the size of the indirections.  If they are
    // different, say because of a cast, then we can't call them equivalent.  Doing so could cause us
    // to drop a cast.
    // Signed-ness difference is okay and expected since a store indirection must always 
    // be signed based on the CIL spec, but a load could be unsigned.
    if (genTypeSize(candidate->gtType) != genTypeSize(storeInd->gtType))
        return false;
    
    GenTreePtr pTreeA = candidate->gtGetOp1();
    GenTreePtr pTreeB = storeInd->gtGetOp1();
    
    // This method will be called by codegen (as well as during lowering).
    // After register allocation, the sources may have been spilled and reloaded
    // to a different register, indicated by an inserted GT_RELOAD node.
    pTreeA = pTreeA->gtSkipReloadOrCopy();
    pTreeB = pTreeB->gtSkipReloadOrCopy();

    genTreeOps      oper;
    unsigned        kind;

    if (pTreeA->OperGet() != pTreeB->OperGet())
        return false;

    oper = pTreeA->OperGet();
    switch (oper)
    {
    case GT_LCL_VAR:
    case GT_LCL_VAR_ADDR:
    case GT_CLS_VAR_ADDR:
    case GT_CNS_INT:
        return NodesAreEquivalentLeaves(pTreeA, pTreeB);

    case GT_LEA:
        {
            GenTreeAddrMode* gtAddr1 = pTreeA->AsAddrMode();
            GenTreeAddrMode* gtAddr2 = pTreeB->AsAddrMode();
            return NodesAreEquivalentLeaves(gtAddr1->Base(), gtAddr2->Base()) &&
                NodesAreEquivalentLeaves(gtAddr1->Index(), gtAddr2->Index()) &&
                gtAddr1->gtScale == gtAddr2->gtScale &&
                gtAddr1->gtOffset == gtAddr2->gtOffset;
        }
    default:
        // We don't handle anything that is not either a constant,
        // a local var or LEA.
        return false;
    }
}

/** Test whether the two given nodes are the same leaves.
 *  Right now, only constant integers and local variables are supported
 */
bool Lowering::NodesAreEquivalentLeaves(GenTreePtr tree1, GenTreePtr tree2)
{
    if (tree1 == nullptr && tree2 == nullptr)
        return true;

    // both null, they are equivalent, otherwise if either is null not equivalent
    if (tree1 == nullptr || tree2 == nullptr)
        return false;

    tree1 = tree1->gtSkipReloadOrCopy();
    tree2 = tree2->gtSkipReloadOrCopy();

    if (tree1->TypeGet() != tree2->TypeGet())
        return false;

    if (tree1->OperGet() != tree2->OperGet())
        return false;

    if (!tree1->OperIsLeaf() || !tree2->OperIsLeaf())
        return false;

    switch (tree1->OperGet())
    {
    case GT_CNS_INT:
        return tree1->gtIntCon.gtIconVal == tree2->gtIntCon.gtIconVal &&
               tree1->IsIconHandle() == tree2->IsIconHandle();
    case GT_LCL_VAR:
    case GT_LCL_VAR_ADDR:
        return tree1->gtLclVarCommon.gtLclNum == tree2->gtLclVarCommon.gtLclNum;
    case GT_CLS_VAR_ADDR:
        return tree1->gtClsVar.gtClsVarHnd == tree2->gtClsVar.gtClsVarHnd;
    default:
        return false;
    }
}

/**
 * Takes care of replaing a GenTree node's child with a new tree.
 *
 *  Assumptions:
 *  a) replacementNode has been unlinked (orphaned) and the expression it represents
 *     is a valid tree, and correctly sequenced internally in case it's not a leaf node.
 *  b) The location specified in ppTreeLocation must be a descendant of 'stmt'.
 *
 */
void Lowering::ReplaceNode(GenTree** ppTreeLocation, GenTree* replacementNode, GenTree* stmt, BasicBlock* block)
{
    assert(ppTreeLocation != nullptr);
    GenTreePtr& treeLocation = *ppTreeLocation;

    assert(treeLocation != nullptr);
    assert(replacementNode != nullptr);
    JITDUMP("The node to replace is:\n");
    DISPNODE(treeLocation);
    JITDUMP("The node that replaces it is:\n");
    DISPTREE(replacementNode);

    assert(comp->fgStmtContainsNode((GenTreeStmt*) stmt, treeLocation));

    GenTreePtr first = comp->fgGetFirstNode(treeLocation);
    comp->fgRemoveContainedEmbeddedStatements(treeLocation, stmt->AsStmt(), block);

    assert(first != nullptr);

    GenTreePtr gtPrev = first->gtPrev;
    GenTreePtr gtNext = treeLocation->gtNext;

    assert(!treeLocation->OperIsLeaf() || gtPrev == treeLocation->gtPrev);

    if (gtPrev == nullptr)
    {
        stmt->gtStmt.gtStmtList = replacementNode;
    }
    else
    {
        gtPrev->gtNext = replacementNode;
    }

    // If we have an embedded statement, and the node we want to
    // replace it's the first one in execution order, it won't fit
    // the special case of having gtPrev == nullptr, so we have to
    // ask directly whether is the first or not.
    if (stmt->gtStmt.gtStmtIsEmbedded() && stmt->gtStmt.gtStmtList == first)
    {
        stmt->gtStmt.gtStmtList = replacementNode;
    }

    replacementNode->gtPrev = gtPrev;

    if (gtNext != nullptr)
    {
        gtNext->gtPrev = replacementNode;
    }

    replacementNode->gtNext = gtNext;
    treeLocation = replacementNode;
#ifdef DEBUG
    comp->fgDebugCheckLinks();
#endif
}

/**
 * Unlinks a node hanging from the specified location and replaces it with a GT_NOP
 *
 *  Assumptions:
 *  The location specified in ppParentLink must be a descendant of stmt.
 *
 */
void Lowering::UnlinkNode(GenTree** ppParentLink, GenTree* stmt, BasicBlock* block)
{
    ReplaceNode(ppParentLink, comp->gtNewNothingNode(), stmt, block);
}

void Lowering::SimpleLinkNodeAfter(GenTree* prevTree, GenTree* newTree)
{
    GenTree* nextTree = prevTree->gtNext;
    newTree->gtPrev = prevTree;
    prevTree->gtNext = newTree;
    if (nextTree != nullptr)
    {
        newTree->gtNext = nextTree;
        nextTree->gtPrev = newTree;
    }
}


#ifdef _TARGET_64BIT_
/**
 * Get common information required to handle a cast instruction
 *
 * Right now only supports 64 bit targets. In order to support 32 bit targets the
 * switch statement needs work.
 *
 */
void Lowering::getCastDescription(GenTreePtr treeNode, CastInfo* castInfo)
{
    // Intialize castInfo
    memset(castInfo, 0, sizeof(*castInfo));

    GenTreePtr castOp = treeNode->gtCast.CastOp();

    var_types dstType = treeNode->CastToType();
    var_types srcType = castOp->TypeGet();

    castInfo->unsignedDest = varTypeIsUnsigned(dstType);
    castInfo->unsignedSource = varTypeIsUnsigned(srcType);

    // If necessary, force the srcType to unsigned when the GT_UNSIGNED flag is set.
    if (!castInfo->unsignedSource && (treeNode->gtFlags & GTF_UNSIGNED) != 0)
    {
        srcType = genUnsignedType(srcType);
        castInfo->unsignedSource = true;
    }

    if (treeNode->gtOverflow() && (genTypeSize(srcType) >= genTypeSize(dstType) || (srcType == TYP_INT && dstType == TYP_ULONG)))
    {
        castInfo->requiresOverflowCheck = true;
    }

    if (castInfo->requiresOverflowCheck)
    {
        ssize_t    typeMin = 0;
        ssize_t    typeMax = 0;
        ssize_t    typeMask = 0;
        bool       signCheckOnly = false;

        // Do we need to compare the value, or just check masks

        switch (dstType)
        {
        default:
            assert(!"unreachable: getCastDescription");
            break;

        case TYP_BYTE:
            typeMask = ssize_t((int)0xFFFFFF80);
            typeMin = SCHAR_MIN;
            typeMax = SCHAR_MAX;
            break;

        case TYP_UBYTE:
            typeMask = ssize_t((int)0xFFFFFF00L);
            break;

        case TYP_SHORT:
            typeMask = ssize_t((int)0xFFFF8000);
            typeMin = SHRT_MIN;
            typeMax = SHRT_MAX;
            break;

        case TYP_CHAR:
            typeMask = ssize_t((int)0xFFFF0000L);
            break;

        case TYP_INT:
            if (srcType == TYP_UINT)
            {
                signCheckOnly = true;
            }
            else
            {
                typeMask = 0xFFFFFFFF80000000LL;
                typeMin = INT_MIN;
                typeMax = INT_MAX;
            }
            break;

        case TYP_UINT:
            if (srcType == TYP_INT)
            {
                signCheckOnly = true;
            }
            else
            {
                typeMask = 0xFFFFFFFF00000000LL;
            }
            break;

        case TYP_LONG:
            signCheckOnly = true;
            break;

        case TYP_ULONG:
            signCheckOnly = true;
            break;
        }

        if (signCheckOnly)
        {
            castInfo->signCheckOnly = true;
        }

        castInfo->typeMax = typeMax;
        castInfo->typeMin = typeMin;
        castInfo->typeMask = typeMask;
    }
}

#endif // _TARGET_64BIT_

#ifdef DEBUG
void Lowering::DumpNodeInfoMap()
{
    // dump tree node info
    printf("-----------------------------\n");
    printf("TREE NODE INFO DUMP\n");
    printf("-----------------------------\n");

    for (BasicBlock* block = comp->fgFirstBB; block; block = block->bbNext)
    {
        GenTreePtr      stmt;
        GenTreePtr      tree;
        for (stmt = block->FirstNonPhiDef(); stmt; stmt = stmt->gtNext)
        {
            GenTreePtr node;
            foreach_treenode_execution_order(node, stmt)
            {
                if (stmt->gtStmt.gtStmtIsEmbedded()) continue;
                comp->gtDispTree(node, nullptr, nullptr, true);
                printf("    +");
                node->gtLsraInfo.dump(m_lsra);
            }
        }
    }
}
#endif // DEBUG

#endif // !LEGACY_BACKEND
