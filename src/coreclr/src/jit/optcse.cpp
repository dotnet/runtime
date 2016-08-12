// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                              OptCSE                                       XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

/*****************************************************************************/
#if FEATURE_ANYCSE
/*****************************************************************************/

/* static */
const size_t Compiler::s_optCSEhashSize = EXPSET_SZ * 2;

/*****************************************************************************
 *
 *  We've found all the candidates, build the index for easy access.
 */

void Compiler::optCSEstop()
{
    if (optCSECandidateCount == 0)
    {
        return;
    }

    CSEdsc*  dsc;
    CSEdsc** ptr;
    unsigned cnt;

    optCSEtab = new (this, CMK_CSE) CSEdsc*[optCSECandidateCount]();

    for (cnt = s_optCSEhashSize, ptr = optCSEhash; cnt; cnt--, ptr++)
    {
        for (dsc = *ptr; dsc; dsc = dsc->csdNextInBucket)
        {
            if (dsc->csdIndex)
            {
                noway_assert((unsigned)dsc->csdIndex <= optCSECandidateCount);
                if (optCSEtab[dsc->csdIndex - 1] == nullptr)
                {
                    optCSEtab[dsc->csdIndex - 1] = dsc;
                }
            }
        }
    }

#ifdef DEBUG
    for (cnt = 0; cnt < optCSECandidateCount; cnt++)
    {
        noway_assert(optCSEtab[cnt] != nullptr);
    }
#endif
}

/*****************************************************************************
 *
 *  Return the descriptor for the CSE with the given index.
 */

inline Compiler::CSEdsc* Compiler::optCSEfindDsc(unsigned index)
{
    noway_assert(index);
    noway_assert(index <= optCSECandidateCount);
    noway_assert(optCSEtab[index - 1]);

    return optCSEtab[index - 1];
}

/*****************************************************************************
 *
 *  For a previously marked CSE, decrement the use counts and unmark it
 */

void Compiler::optUnmarkCSE(GenTreePtr tree)
{
    if (!IS_CSE_INDEX(tree->gtCSEnum))
    {
        // This tree is not a CSE candidate, so there is nothing
        // to do.
        return;
    }

    unsigned CSEnum = GET_CSE_INDEX(tree->gtCSEnum);
    CSEdsc*  desc;

    // make sure it's been initialized
    noway_assert(optCSEweight <= BB_MAX_WEIGHT);

    /* Is this a CSE use? */
    if (IS_CSE_USE(tree->gtCSEnum))
    {
        desc = optCSEfindDsc(CSEnum);

#ifdef DEBUG
        if (verbose)
        {
            printf("Unmark CSE use #%02d at ", CSEnum);
            printTreeID(tree);
            printf(": %3d -> %3d\n", desc->csdUseCount, desc->csdUseCount - 1);
        }
#endif

        /* Reduce the nested CSE's 'use' count */

        noway_assert(desc->csdUseCount > 0);

        if (desc->csdUseCount > 0)
        {
            desc->csdUseCount -= 1;

            if (desc->csdUseWtCnt < optCSEweight)
            {
                desc->csdUseWtCnt = 0;
            }
            else
            {
                desc->csdUseWtCnt -= optCSEweight;
            }
        }
    }
    else
    {
        desc = optCSEfindDsc(CSEnum);

#ifdef DEBUG
        if (verbose)
        {
            printf("Unmark CSE def #%02d at ", CSEnum);
            printTreeID(tree);
            printf(": %3d -> %3d\n", desc->csdDefCount, desc->csdDefCount - 1);
        }
#endif

        /* Reduce the nested CSE's 'def' count */

        noway_assert(desc->csdDefCount > 0);

        if (desc->csdDefCount > 0)
        {
            desc->csdDefCount -= 1;

            if (desc->csdDefWtCnt < optCSEweight)
            {
                desc->csdDefWtCnt = 0;
            }
            else
            {
                desc->csdDefWtCnt -= optCSEweight;
            }
        }
    }

    tree->gtCSEnum = NO_CSE;
}

Compiler::fgWalkResult Compiler::optHasNonCSEChild(GenTreePtr* pTree, fgWalkData* data)
{
    if (*pTree == data->pCallbackData)
    {
        return WALK_CONTINUE;
    }

    if ((*pTree)->gtFlags & GTF_DONT_CSE)
    {

        // Fix 392756 WP7 Crossgen
        // Don't propagate the GTF_DONT_CSE flag up from a GT_CNS_INT
        //
        // During codegen optGetArrayRefScaleAndIndex() makes the assumption that op2 of a GT_MUL node
        // is a constant and is not capable of handling CSE'ing the elemSize constant into a lclvar.
        // Hence to prevent the constant from becoming a CSE we have marked it as NO_CSE, but this
        // should not prevent tree's above the constant from becoming CSE's.
        //
        if ((*pTree)->gtOper == GT_CNS_INT)
        {
            return WALK_SKIP_SUBTREES;
        }

        return WALK_ABORT;
    }

    return WALK_SKIP_SUBTREES;
}

Compiler::fgWalkResult Compiler::optPropagateNonCSE(GenTreePtr* pTree, fgWalkData* data)
{
    GenTree*  tree = *pTree;
    Compiler* comp = data->compiler;

    /* Calls get DONT_CSE implicitly */
    if (tree->OperGet() == GT_CALL)
    {
        if (!IsSharedStaticHelper(tree))
        {
            tree->gtFlags |= GTF_DONT_CSE;
        }
    }

    if ((tree->gtFlags & GTF_DONT_CSE) == 0)
    {
        /* Propagate the DONT_CSE flag from child to parent */
        if (comp->fgWalkTreePre(&tree, optHasNonCSEChild, tree) == WALK_ABORT)
        {
            tree->gtFlags |= GTF_DONT_CSE;
        }
    }

    return WALK_CONTINUE;
}

/*****************************************************************************
 *
 *  Helper passed to Compiler::fgWalkAllTreesPre() to unmark nested CSE's.
 */

/* static */
Compiler::fgWalkResult Compiler::optUnmarkCSEs(GenTreePtr* pTree, fgWalkData* data)
{
    GenTreePtr tree     = *pTree;
    Compiler*  comp     = data->compiler;
    GenTreePtr keepList = (GenTreePtr)(data->pCallbackData);

    // We may have a non-NULL side effect list that is being kept
    //
    if (keepList)
    {
        GenTreePtr keptTree = keepList;
        while (keptTree->OperGet() == GT_COMMA)
        {
            assert(keptTree->OperKind() & GTK_SMPOP);
            GenTreePtr op1 = keptTree->gtOp.gtOp1;
            GenTreePtr op2 = keptTree->gtGetOp2();

            // For the GT_COMMA case the op1 is part of the orginal CSE tree
            // that is being kept because it contains some side-effect
            //
            if (tree == op1)
            {
                // This tree and all of its sub trees are being kept
                return WALK_SKIP_SUBTREES;
            }

            // For the GT_COMMA case the op2 are the remaining side-effects of the orginal CSE tree
            // which can again be another GT_COMMA or the final side-effect part
            //
            keptTree = op2;
        }
        if (tree == keptTree)
        {
            // This tree and all of its sub trees are being kept
            return WALK_SKIP_SUBTREES;
        }
    }

    // This node is being removed from the graph of GenTreePtr
    // Call optUnmarkCSE and  decrement the LclVar ref counts.
    comp->optUnmarkCSE(tree);
    assert(!IS_CSE_INDEX(tree->gtCSEnum));

    /* Look for any local variable references */

    if (tree->gtOper == GT_LCL_VAR)
    {
        unsigned   lclNum;
        LclVarDsc* varDsc;

        /* This variable ref is going away, decrease its ref counts */

        lclNum = tree->gtLclVarCommon.gtLclNum;
        assert(lclNum < comp->lvaCount);
        varDsc = comp->lvaTable + lclNum;

        // make sure it's been initialized
        assert(comp->optCSEweight <= BB_MAX_WEIGHT);

        /* Decrement its lvRefCnt and lvRefCntWtd */

        varDsc->decRefCnts(comp->optCSEweight, comp);
    }

    return WALK_CONTINUE;
}

Compiler::fgWalkResult Compiler::optCSE_MaskHelper(GenTreePtr* pTree, fgWalkData* walkData)
{
    GenTree*         tree      = *pTree;
    Compiler*        comp      = walkData->compiler;
    optCSE_MaskData* pUserData = (optCSE_MaskData*)(walkData->pCallbackData);

    if (IS_CSE_INDEX(tree->gtCSEnum))
    {
        unsigned  cseIndex = GET_CSE_INDEX(tree->gtCSEnum);
        EXPSET_TP cseBit   = genCSEnum2bit(cseIndex);
        if (IS_CSE_DEF(tree->gtCSEnum))
        {
            pUserData->CSE_defMask |= cseBit;
        }
        else
        {
            pUserData->CSE_useMask |= cseBit;
        }
    }

    return WALK_CONTINUE;
}

// This functions walks all the node for an given tree
// and return the mask of CSE defs and uses for the tree
//
void Compiler::optCSE_GetMaskData(GenTreePtr tree, optCSE_MaskData* pMaskData)
{
    pMaskData->CSE_defMask = 0;
    pMaskData->CSE_useMask = 0;
    fgWalkTreePre(&tree, optCSE_MaskHelper, (void*)pMaskData);
}

//------------------------------------------------------------------------
// optCSE_canSwap: Determine if the execution order of two nodes can be swapped.
//
// Arguments:
//    op1 - The first node
//    op2 - The second node
//
// Return Value:
//    Return true iff it safe to swap the execution order of 'op1' and 'op2',
//    considering only the locations of the CSE defs and uses.
//
// Assumptions:
//    'op1' currently occurse before 'op2' in the execution order.
//
bool Compiler::optCSE_canSwap(GenTree* op1, GenTree* op2)
{
    // op1 and op2 must be non-null.
    assert(op1 != nullptr);
    assert(op2 != nullptr);

    bool canSwap = true; // the default result unless proven otherwise.

    optCSE_MaskData op1MaskData;
    optCSE_MaskData op2MaskData;

    optCSE_GetMaskData(op1, &op1MaskData);
    optCSE_GetMaskData(op2, &op2MaskData);

    // We cannot swap if op1 contains a CSE def that is used by op2
    if ((op1MaskData.CSE_defMask & op2MaskData.CSE_useMask) != 0)
    {
        canSwap = false;
    }
    else
    {
        // We also cannot swap if op2 contains a CSE def that is used by op1.
        if ((op2MaskData.CSE_defMask & op1MaskData.CSE_useMask) != 0)
        {
            canSwap = false;
        }
    }

    return canSwap;
}

//------------------------------------------------------------------------
// optCSE_canSwap: Determine if the execution order of a node's operands can be swapped.
//
// Arguments:
//    tree - The node of interest
//
// Return Value:
//    Return true iff it safe to swap the execution order of the operands of 'tree',
//    considering only the locations of the CSE defs and uses.
//
bool Compiler::optCSE_canSwap(GenTreePtr tree)
{
    // We must have a binary treenode with non-null op1 and op2
    assert((tree->OperKind() & GTK_SMPOP) != 0);

    GenTreePtr op1 = tree->gtOp.gtOp1;
    GenTreePtr op2 = tree->gtGetOp2();

    return optCSE_canSwap(op1, op2);
}

/*****************************************************************************
 *
 *  Compare function passed to qsort() by CSE_Heuristic::SortCandidates
 *  when (CodeOptKind() != Compiler::SMALL_CODE)
 */

/* static */
int __cdecl Compiler::optCSEcostCmpEx(const void* op1, const void* op2)
{
    CSEdsc* dsc1 = *(CSEdsc**)op1;
    CSEdsc* dsc2 = *(CSEdsc**)op2;

    GenTreePtr exp1 = dsc1->csdTree;
    GenTreePtr exp2 = dsc2->csdTree;

    int diff;

    diff = (int)(exp2->gtCostEx - exp1->gtCostEx);

    if (diff != 0)
    {
        return diff;
    }

    // Sort the higher Use Counts toward the top
    diff = (int)(dsc2->csdUseWtCnt - dsc1->csdUseWtCnt);

    if (diff != 0)
    {
        return diff;
    }

    // With the same use count, Sort the lower Def Counts toward the top
    diff = (int)(dsc1->csdDefWtCnt - dsc2->csdDefWtCnt);

    if (diff != 0)
    {
        return diff;
    }

    // In order to ensure that we have a stable sort, we break ties using the csdIndex
    return (int)(dsc1->csdIndex - dsc2->csdIndex);
}

/*****************************************************************************
 *
 *  Compare function passed to qsort() by CSE_Heuristic::SortCandidates
 *  when (CodeOptKind() == Compiler::SMALL_CODE)
 */

/* static */
int __cdecl Compiler::optCSEcostCmpSz(const void* op1, const void* op2)
{
    CSEdsc* dsc1 = *(CSEdsc**)op1;
    CSEdsc* dsc2 = *(CSEdsc**)op2;

    GenTreePtr exp1 = dsc1->csdTree;
    GenTreePtr exp2 = dsc2->csdTree;

    int diff;

    diff = (int)(exp2->gtCostSz - exp1->gtCostSz);

    if (diff != 0)
    {
        return diff;
    }

    // Sort the higher Use Counts toward the top
    diff = (int)(dsc2->csdUseCount - dsc1->csdUseCount);

    if (diff != 0)
    {
        return diff;
    }

    // With the same use count, Sort the lower Def Counts toward the top
    diff = (int)(dsc1->csdDefCount - dsc2->csdDefCount);

    if (diff != 0)
    {
        return diff;
    }

    // In order to ensure that we have a stable sort, we break ties using the csdIndex
    return (int)(dsc1->csdIndex - dsc2->csdIndex);
}

/*****************************************************************************/
#if FEATURE_VALNUM_CSE
/*****************************************************************************/

/*****************************************************************************
 *
 *  Initialize the Value Number CSE tracking logic.
 */

void Compiler::optValnumCSE_Init()
{
#ifdef DEBUG
    optCSEtab = nullptr;
#endif

    /* Allocate and clear the hash bucket table */

    optCSEhash = new (this, CMK_CSE) CSEdsc*[s_optCSEhashSize]();

    optCSECandidateCount = 0;
    optDoCSE             = false; // Stays false until we find duplicate CSE tree
}

/*****************************************************************************
 *
 *  Assign an index to the given expression (adding it to the lookup table,
 *  if necessary). Returns the index or 0 if the expression can not be a CSE.
 */

unsigned Compiler::optValnumCSE_Index(GenTreePtr tree, GenTreePtr stmt)
{
    unsigned key;
    unsigned hash;
    unsigned hval;
    CSEdsc*  hashDsc;

    ValueNum vnlib = tree->GetVN(VNK_Liberal);

    /* Compute the hash value for the expression */

    key = (unsigned)vnlib;

    hash = key;
    hash *= (unsigned)(s_optCSEhashSize + 1);
    hash >>= 7;

    hval = hash % s_optCSEhashSize;

    /* Look for a matching index in the hash table */

    bool newCSE = false;

    for (hashDsc = optCSEhash[hval]; hashDsc; hashDsc = hashDsc->csdNextInBucket)
    {
        if (hashDsc->csdHashValue == key)
        {
            treeStmtLstPtr newElem;

            /* Have we started the list of matching nodes? */

            if (hashDsc->csdTreeList == nullptr)
            {
                // Create the new element based upon the matching hashDsc element.

                newElem = new (this, CMK_TreeStatementList) treeStmtLst;

                newElem->tslTree  = hashDsc->csdTree;
                newElem->tslStmt  = hashDsc->csdStmt;
                newElem->tslBlock = hashDsc->csdBlock;
                newElem->tslNext  = nullptr;

                /* Start the list with the first CSE candidate recorded */

                hashDsc->csdTreeList = newElem;
                hashDsc->csdTreeLast = newElem;
            }

            noway_assert(hashDsc->csdTreeList);

            /* Append this expression to the end of the list */

            newElem = new (this, CMK_TreeStatementList) treeStmtLst;

            newElem->tslTree  = tree;
            newElem->tslStmt  = stmt;
            newElem->tslBlock = compCurBB;
            newElem->tslNext  = nullptr;

            hashDsc->csdTreeLast->tslNext = newElem;
            hashDsc->csdTreeLast          = newElem;

            optDoCSE = true; // Found a duplicate CSE tree

            /* Have we assigned a CSE index? */
            if (hashDsc->csdIndex == 0)
            {
                newCSE = true;
                break;
            }
#if 0 
            // Use this to see if this Value Number base CSE is also a lexical CSE
            bool treeMatch = GenTree::Compare(hashDsc->csdTree, tree, true);
#endif

            assert(FitsIn<signed char>(hashDsc->csdIndex));
            tree->gtCSEnum = ((signed char)hashDsc->csdIndex);
            return hashDsc->csdIndex;
        }
    }

    if (!newCSE)
    {
        /* Not found, create a new entry (unless we have too many already) */

        if (optCSECandidateCount < MAX_CSE_CNT)
        {
            hashDsc = new (this, CMK_CSE) CSEdsc;

            hashDsc->csdHashValue      = key;
            hashDsc->csdIndex          = 0;
            hashDsc->csdLiveAcrossCall = 0;
            hashDsc->csdDefCount       = 0;
            hashDsc->csdUseCount       = 0;
            hashDsc->csdDefWtCnt       = 0;
            hashDsc->csdUseWtCnt       = 0;

            hashDsc->csdTree     = tree;
            hashDsc->csdStmt     = stmt;
            hashDsc->csdBlock    = compCurBB;
            hashDsc->csdTreeList = nullptr;

            /* Append the entry to the hash bucket */

            hashDsc->csdNextInBucket = optCSEhash[hval];
            optCSEhash[hval]         = hashDsc;
        }
        return 0;
    }
    else // newCSE is true
    {
        /* We get here only after finding a matching CSE */

        /* Create a new CSE (unless we have the maximum already) */

        if (optCSECandidateCount == MAX_CSE_CNT)
        {
            return 0;
        }

        C_ASSERT((signed char)MAX_CSE_CNT == MAX_CSE_CNT);

        unsigned  CSEindex = ++optCSECandidateCount;
        EXPSET_TP CSEmask  = genCSEnum2bit(CSEindex);

        /* Record the new CSE index in the hashDsc */
        hashDsc->csdIndex = CSEindex;

        /* Update the gtCSEnum field in the original tree */
        noway_assert(hashDsc->csdTreeList->tslTree->gtCSEnum == 0);
        assert(FitsIn<signed char>(CSEindex));

        hashDsc->csdTreeList->tslTree->gtCSEnum = ((signed char)CSEindex);
        noway_assert(((unsigned)hashDsc->csdTreeList->tslTree->gtCSEnum) == CSEindex);

        tree->gtCSEnum = ((signed char)CSEindex);

#ifdef DEBUG
        if (verbose)
        {
            printf("\nCSE candidate #%02u, vn=", CSEindex);
            vnPrint(vnlib, 0);
            printf(" cseMask=%s in BB%02u, [cost=%2u, size=%2u]: \n", genES2str(genCSEnum2bit(CSEindex)),
                   compCurBB->bbNum, tree->gtCostEx, tree->gtCostSz);
            gtDispTree(tree);
        }
#endif // DEBUG

        return CSEindex;
    }
}

/*****************************************************************************
 *
 *  Locate CSE candidates and assign indices to them
 *  return 0 if no CSE candidates were found
 *  Also initialize bbCseIn, bbCseout and bbCseGen sets for all blocks
 */

unsigned Compiler::optValnumCSE_Locate()
{
    // Locate CSE candidates and assign them indices

    for (BasicBlock* block = fgFirstBB; block; block = block->bbNext)
    {
        GenTreePtr stmt;
        GenTreePtr tree;

        /* Make the block publicly available */

        compCurBB = block;

        /* Ensure that the BBF_VISITED and BBF_MARKED flag are clear */
        /* Everyone who uses these flags are required to clear afterwards */
        noway_assert((block->bbFlags & (BBF_VISITED | BBF_MARKED)) == 0);

        /* Walk the statement trees in this basic block */
        for (stmt = block->FirstNonPhiDef(); stmt; stmt = stmt->gtNext)
        {
            noway_assert(stmt->gtOper == GT_STMT);

            /* We walk the tree in the forwards direction (bottom up) */
            for (tree = stmt->gtStmt.gtStmtList; tree; tree = tree->gtNext)
            {
                if (!optIsCSEcandidate(tree))
                {
                    continue;
                }

                ValueNum vnlib = tree->GetVN(VNK_Liberal);

                if (ValueNumStore::isReservedVN(vnlib))
                {
                    continue;
                }

                // Don't CSE constant values, instead let the Value Number
                // based Assertion Prop phase handle them.
                //
                if (vnStore->IsVNConstant(vnlib))
                {
                    continue;
                }

                /* Assign an index to this expression */

                unsigned CSEindex = optValnumCSE_Index(tree, stmt);

                if (CSEindex != 0)
                {
                    noway_assert(((unsigned)tree->gtCSEnum) == CSEindex);
                }
            }
        }
    }

    /* We're done if there were no interesting expressions */

    if (!optDoCSE)
    {
        return 0;
    }

    /* We're finished building the expression lookup table */

    optCSEstop();

    return 1;
}

/*****************************************************************************
 *
 *  Compute each blocks bbCseGen
 *  This is the bitset that represents the CSEs that are generated within the block
 */
void Compiler::optValnumCSE_InitDataFlow()
{
    for (BasicBlock* block = fgFirstBB; block; block = block->bbNext)
    {
        GenTreePtr stmt;
        GenTreePtr tree;

        /* Initialize the blocks's bbCseIn set */

        bool init_to_zero = false;

        if (block == fgFirstBB)
        {
            /* Clear bbCseIn for the entry block */
            init_to_zero = true;
        }
#if !CSE_INTO_HANDLERS
        else
        {
            if (bbIsHandlerBeg(block))
            {
                /* Clear everything on entry to filters or handlers */
                init_to_zero = true;
            }
        }
#endif
        if (init_to_zero)
        {
            /* Initialize to {ZERO} prior to dataflow */

            block->bbCseIn = 0;
        }
        else
        {
            /* Initialize to {ALL} prior to dataflow */

            block->bbCseIn = EXPSET_ALL;
        }
        block->bbCseOut = EXPSET_ALL;

        /* Initialize to {ZERO} prior to locating the CSE candidates */
        block->bbCseGen = 0;
    }

    // We walk the set of CSE candidates and set the bit corresponsing to the CSEindex
    // in the block's bbCseGen bitset
    //
    for (unsigned cnt = 0; cnt < optCSECandidateCount; cnt++)
    {
        CSEdsc*        dsc      = optCSEtab[cnt];
        unsigned       CSEindex = dsc->csdIndex;
        treeStmtLstPtr lst      = dsc->csdTreeList;
        noway_assert(lst);

        while (lst != nullptr)
        {
            BasicBlock* block = lst->tslBlock;
            block->bbCseGen |= genCSEnum2bit(CSEindex);
            lst = lst->tslNext;
        }
    }

#ifdef DEBUG
    // Dump out the bbCseGen information that we just created
    //
    if (verbose)
    {
        bool headerPrinted = false;
        for (BasicBlock* block = fgFirstBB; block; block = block->bbNext)
        {
            if (block->bbCseGen != 0)
            {
                if (!headerPrinted)
                {
                    printf("\nBlocks that generate CSE def/uses\n");
                    headerPrinted = true;
                }
                printf("BB%02u", block->bbNum);
                printf(" cseGen = %s\n", genES2str(block->bbCseGen));
            }
        }
    }

    fgDebugCheckLinks();

#endif // DEBUG
}

/*****************************************************************************
 *
 * CSE Dataflow, so that all helper methods for dataflow are in a single place
 *
 */
class CSE_DataFlow
{
private:
    EXPSET_TP m_preMergeOut;

    Compiler* m_pCompiler;

public:
    CSE_DataFlow(Compiler* pCompiler) : m_pCompiler(pCompiler)
    {
    }

    Compiler* getCompiler()
    {
        return m_pCompiler;
    }

    // At the start of the merge function of the dataflow equations, initialize premerge state (to detect changes.)
    void StartMerge(BasicBlock* block)
    {
        m_preMergeOut = block->bbCseOut;
    }

    // During merge, perform the actual merging of the predecessor's (since this is a forward analysis) dataflow flags.
    void Merge(BasicBlock* block, BasicBlock* predBlock, flowList* preds)
    {
        block->bbCseIn &= predBlock->bbCseOut;
    }

    // At the end of the merge store results of the dataflow equations, in a postmerge state.
    bool EndMerge(BasicBlock* block)
    {
        EXPSET_TP mergeOut = block->bbCseOut & (block->bbCseIn | block->bbCseGen);
        block->bbCseOut    = mergeOut;
        return (mergeOut != m_preMergeOut);
    }
};

/*****************************************************************************
 *
 *  Perform a DataFlow forward analysis using the block CSE bitsets:
 *    Inputs:
 *      bbCseGen  - Exact CSEs that are become available within the block
 *      bbCseIn   - Maximal estimate of CSEs that are/could be available at input to the block
 *      bbCseOut  - Maximal estimate of CSEs that are/could be available at exit to the block
 *
 *    Outputs:
 *      bbCseIn   - Computed CSEs that are available at input to the block
 *      bbCseOut  - Computed CSEs that are available at exit to the block
 */

void Compiler::optValnumCSE_DataFlow()
{
    CSE_DataFlow cse(this);

    // Modified dataflow algorithm for available expressions.
    DataFlow cse_flow(this);

    cse_flow.ForwardAnalysis(cse);

#ifdef DEBUG
    if (verbose)
    {
        printf("\nAfter performing DataFlow for ValnumCSE's\n");

        for (BasicBlock* block = fgFirstBB; block; block = block->bbNext)
        {
            printf("BB%02u", block->bbNum);
            printf(" cseIn  = %s", genES2str(block->bbCseIn));
            printf(" cseOut = %s", genES2str(block->bbCseOut));
            printf("\n");
        }

        printf("\n");
    }
#endif // DEBUG
}

/*****************************************************************************
 *
 *   Using the information computed by CSE_DataFlow determine for each
 *   CSE whether the CSE is a definition (if the CSE was not available)
 *   or if the CSE is a use (if the CSE was previously made available)
 *   The implementation iterates of all blocks setting 'available_cses'
 *   to the CSEs that are available at input to the block.
 *   When a CSE expression is encountered it is classified as either
 *   as a definition (if the CSE is not in the 'available_cses' set) or
 *   as a use (if the CSE is  in the 'available_cses' set).  If the CSE
 *   is a definition then it is added to the 'available_cses' set.
 *   In the Value Number based CSEs we do not need to have kill sets
 */

void Compiler::optValnumCSE_Availablity()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("Labeling the CSEs with Use/Def information\n");
    }
#endif
    for (BasicBlock* block = fgFirstBB; block; block = block->bbNext)
    {
        GenTreePtr stmt;
        GenTreePtr tree;

        /* Make the block publicly available */

        compCurBB = block;

        EXPSET_TP available_cses = block->bbCseIn;

        optCSEweight = block->getBBWeight(this);

        /* Walk the statement trees in this basic block */

        for (stmt = block->FirstNonPhiDef(); stmt; stmt = stmt->gtNext)
        {
            noway_assert(stmt->gtOper == GT_STMT);

            /* We walk the tree in the forwards direction (bottom up) */
            for (tree = stmt->gtStmt.gtStmtList; tree; tree = tree->gtNext)
            {
                if (IS_CSE_INDEX(tree->gtCSEnum))
                {
                    EXPSET_TP mask = genCSEnum2bit(tree->gtCSEnum);
                    CSEdsc*   desc = optCSEfindDsc(tree->gtCSEnum);
                    unsigned  stmw = block->getBBWeight(this);

                    /* Is this expression available here? */

                    if (available_cses & mask)
                    {
                        /* This is a CSE use */

                        desc->csdUseCount += 1;
                        desc->csdUseWtCnt += stmw;
                    }
                    else
                    {
                        if (tree->gtFlags & GTF_COLON_COND)
                        {
                            // We can't create CSE definitions inside QMARK-COLON trees
                            tree->gtCSEnum = NO_CSE;
                            continue;
                        }

                        /* This is a CSE def */

                        desc->csdDefCount += 1;
                        desc->csdDefWtCnt += stmw;

                        /* Mark the node as a CSE definition */

                        tree->gtCSEnum = TO_CSE_DEF(tree->gtCSEnum);

                        /* This CSE will be available after this def */

                        available_cses |= mask;
                    }
#ifdef DEBUG
                    if (verbose && IS_CSE_INDEX(tree->gtCSEnum))
                    {
                        printf("BB%02u ", block->bbNum);
                        printTreeID(tree);
                        printf(" %s of CSE #%02u [weight=%s]\n", IS_CSE_USE(tree->gtCSEnum) ? "Use" : "Def",
                               GET_CSE_INDEX(tree->gtCSEnum), refCntWtd2str(stmw));
                    }
#endif
                }
            }
        }
    }
}

//  The following class handles the CSE heuristics
//  we use a complex set of heuristic rules
//  to determine if it is likely to be profitable to perform this CSE
//
class CSE_Heuristic
{
    Compiler* m_pCompiler;
    unsigned  m_addCSEcount;

    unsigned               aggressiveRefCnt;
    unsigned               moderateRefCnt;
    unsigned               enregCount; // count of the number of enregisterable variables
    bool                   largeFrame;
    bool                   hugeFrame;
    Compiler::codeOptimize codeOptKind;
    Compiler::CSEdsc**     sortTab;
    size_t                 sortSiz;
#ifdef DEBUG
    CLRRandom m_cseRNG;
    unsigned  m_bias;
#endif

public:
    CSE_Heuristic(Compiler* pCompiler) : m_pCompiler(pCompiler)
    {
        codeOptKind = m_pCompiler->compCodeOpt();
    }

    Compiler::codeOptimize CodeOptKind()
    {
        return codeOptKind;
    }

    // Perform the Initialization step for our CSE Heuristics
    // determine the various cut off values to use for
    // the aggressive, moderate and conservative CSE promotions
    // count the number of enregisterable variables
    // determine if the method has a large or huge stack frame.
    //
    void Initialize()
    {
        m_addCSEcount = 0; /* Count of the number of LclVars for CSEs that we added */

        // Record the weighted ref count of the last "for sure" callee saved LclVar
        aggressiveRefCnt = 0;
        moderateRefCnt   = 0;
        enregCount       = 0;
        largeFrame       = false;
        hugeFrame        = false;
        sortTab          = nullptr;
        sortSiz          = 0;

#ifdef _TARGET_XARCH_
        if (m_pCompiler->compLongUsed)
        {
            enregCount++;
        }
#endif

        unsigned   frameSize        = 0;
        unsigned   regAvailEstimate = ((CNT_CALLEE_ENREG * 3) + (CNT_CALLEE_TRASH * 2) + 1);
        unsigned   lclNum;
        LclVarDsc* varDsc;

        for (lclNum = 0, varDsc = m_pCompiler->lvaTable; lclNum < m_pCompiler->lvaCount; lclNum++, varDsc++)
        {
            if (varDsc->lvRefCnt == 0)
            {
                continue;
            }

            bool onStack = (regAvailEstimate == 0); // true when it is likely that this LclVar will have a stack home

            // Some LclVars always have stack homes
            if ((varDsc->lvDoNotEnregister) || (varDsc->lvType == TYP_LCLBLK))
            {
                onStack = true;
            }

#ifdef _TARGET_X86_
            // Treat floating point and 64 bit integers as always on the stack
            if (varTypeIsFloating(varDsc->TypeGet()) || varTypeIsLong(varDsc->TypeGet()))
                onStack = true;
#endif

            if (onStack)
            {
                frameSize += m_pCompiler->lvaLclSize(lclNum);
            }
            else
            {
                // For the purposes of estimating the frameSize we
                // will consider this LclVar as being enregistered.
                // Now we reduce the remaining regAvailEstimate by
                // an appropriate amount.
                if (varDsc->lvRefCnt <= 2)
                {
                    // a single use single def LclVar only uses 1
                    regAvailEstimate -= 1;
                }
                else
                {
                    // a LclVar with multiple uses and defs uses 2
                    if (regAvailEstimate >= 2)
                    {
                        regAvailEstimate -= 2;
                    }
                    else
                    {
                        // Don't try to subtract when regAvailEstimate is 1
                        regAvailEstimate = 0;
                    }
                }
            }
#ifdef _TARGET_XARCH_
            if (frameSize > 0x080)
            {
                // We likely have a large stack frame.
                // Thus we might need to use large displacements when loading or storing
                // to CSE LclVars that are not enregistered
                largeFrame = true;
                break; // early out,  we don't need to keep increasing frameSize
            }
#else // _TARGET_ARM_
            if (frameSize > 0x0400)
            {
                largeFrame = true;
            }
            if (frameSize > 0x10000)
            {
                hugeFrame = true;
                break;
            }
#endif
        }

        unsigned sortNum = 0;
        while (sortNum < m_pCompiler->lvaTrackedCount)
        {
            LclVarDsc* varDsc = m_pCompiler->lvaRefSorted[sortNum++];
            var_types  varTyp = varDsc->TypeGet();

            if (varDsc->lvDoNotEnregister)
            {
                continue;
            }

            if (!varTypeIsFloating(varTyp))
            {
                // TODO-1stClassStructs: Remove this; it is here to duplicate previous behavior.
                // Note that this makes genTypeStSz return 1.
                if (varTypeIsStruct(varTyp))
                {
                    varTyp = TYP_STRUCT;
                }
                enregCount += genTypeStSz(varTyp);
            }

            if ((aggressiveRefCnt == 0) && (enregCount > (CNT_CALLEE_ENREG * 3 / 2)))
            {
                if (CodeOptKind() == Compiler::SMALL_CODE)
                {
                    aggressiveRefCnt = varDsc->lvRefCnt + BB_UNITY_WEIGHT;
                }
                else
                {
                    aggressiveRefCnt = varDsc->lvRefCntWtd + BB_UNITY_WEIGHT;
                }
            }
            if ((moderateRefCnt == 0) && (enregCount > ((CNT_CALLEE_ENREG * 3) + (CNT_CALLEE_TRASH * 2))))
            {
                if (CodeOptKind() == Compiler::SMALL_CODE)
                {
                    moderateRefCnt = varDsc->lvRefCnt;
                }
                else
                {
                    moderateRefCnt = varDsc->lvRefCntWtd;
                }
            }
        }
        unsigned mult = 3;
        // use smaller value for mult when enregCount is in [0..4]
        if (enregCount <= 4)
        {
            mult = (enregCount <= 2) ? 1 : 2;
        }

        aggressiveRefCnt = max(BB_UNITY_WEIGHT * mult, aggressiveRefCnt);
        moderateRefCnt   = max((BB_UNITY_WEIGHT * mult) / 2, moderateRefCnt);

#ifdef DEBUG
        if (m_pCompiler->verbose)
        {
            printf("\n");
            printf("Aggressive CSE Promotion cutoff is %u\n", aggressiveRefCnt);
            printf("Moderate CSE Promotion cutoff is %u\n", moderateRefCnt);
            printf("Framesize estimate is 0x%04X\n", frameSize);
            printf("We have a %s frame\n", hugeFrame ? "huge" : (largeFrame ? "large" : "small"));
        }
#endif
    }

    void SortCandidates()
    {
        /* Create an expression table sorted by decreasing cost */
        sortTab = new (m_pCompiler, CMK_CSE) Compiler::CSEdsc*[m_pCompiler->optCSECandidateCount];

        sortSiz = m_pCompiler->optCSECandidateCount * sizeof(*sortTab);
        memcpy(sortTab, m_pCompiler->optCSEtab, sortSiz);

        if (CodeOptKind() == Compiler::SMALL_CODE)
        {
            qsort(sortTab, m_pCompiler->optCSECandidateCount, sizeof(*sortTab), m_pCompiler->optCSEcostCmpSz);
        }
        else
        {
            qsort(sortTab, m_pCompiler->optCSECandidateCount, sizeof(*sortTab), m_pCompiler->optCSEcostCmpEx);
        }

#ifdef DEBUG
        if (m_pCompiler->verbose)
        {
            printf("\nSorted CSE candidates:\n");
            /* Print out the CSE candidates */
            for (unsigned cnt = 0; cnt < m_pCompiler->optCSECandidateCount; cnt++)
            {
                Compiler::CSEdsc* dsc  = sortTab[cnt];
                GenTreePtr        expr = dsc->csdTree;

                unsigned def;
                unsigned use;

                if (CodeOptKind() == Compiler::SMALL_CODE)
                {
                    def = dsc->csdDefCount; // def count
                    use = dsc->csdUseCount; // use count (excluding the implicit uses at defs)
                }
                else
                {
                    def = dsc->csdDefWtCnt; // weighted def count
                    use = dsc->csdUseWtCnt; // weighted use count (excluding the implicit uses at defs)
                }

                printf("CSE #%02u,cseMask=%s,useCnt=%d: [def=%3u, use=%3u", dsc->csdIndex,
                       genES2str(genCSEnum2bit(dsc->csdIndex)), dsc->csdUseCount, def, use);
                printf("] :: ");
                m_pCompiler->gtDispTree(expr, nullptr, nullptr, true);
            }
            printf("\n");
        }
#endif // DEBUG
    }

    //  The following class nested within CSE_Heuristic encapsulates the information
    //  about the current CSE candidate that is under consideration
    //
    //  TODO-Cleanup: This is still very much based upon the old Lexical CSE implementation
    //  and needs to be reworked for the Value Number based implementation
    //
    class CSE_Candidate
    {
        CSE_Heuristic*    m_context;
        Compiler::CSEdsc* m_CseDsc;

        unsigned m_cseIndex;

        unsigned m_defCount;
        unsigned m_useCount;

        unsigned m_Cost;
        unsigned m_Size;

    public:
        CSE_Candidate(CSE_Heuristic* context, Compiler::CSEdsc* cseDsc) : m_context(context), m_CseDsc(cseDsc)
        {
            m_cseIndex = m_CseDsc->csdIndex;
        }

        Compiler::CSEdsc* CseDsc()
        {
            return m_CseDsc;
        }
        unsigned CseIndex()
        {
            return m_cseIndex;
        }
        unsigned DefCount()
        {
            return m_defCount;
        }
        unsigned UseCount()
        {
            return m_useCount;
        }
        // TODO-CQ: With ValNum CSE's the Expr and its cost can vary.
        GenTreePtr Expr()
        {
            return m_CseDsc->csdTree;
        }
        unsigned Cost()
        {
            return m_Cost;
        }
        unsigned Size()
        {
            return m_Size;
        }

        bool LiveAcrossCall()
        {
            return (m_CseDsc->csdLiveAcrossCall != 0);
        }

        void InitializeCounts()
        {
            if (m_context->CodeOptKind() == Compiler::SMALL_CODE)
            {
                m_Cost     = Expr()->gtCostSz;      // the estimated code size
                m_Size     = Expr()->gtCostSz;      // always the gtCostSz
                m_defCount = m_CseDsc->csdDefCount; // def count
                m_useCount = m_CseDsc->csdUseCount; // use count (excluding the implicit uses at defs)
            }
            else
            {
                m_Cost     = Expr()->gtCostEx;      // the estimated execution cost
                m_Size     = Expr()->gtCostSz;      // always the gtCostSz
                m_defCount = m_CseDsc->csdDefWtCnt; // weighted def count
                m_useCount = m_CseDsc->csdUseWtCnt; // weighted use count (excluding the implicit uses at defs)
            }
        }
    };

#ifdef DEBUG
    //------------------------------------------------------------------------
    // optConfigBiasedCSE:
    //     Stress mode to shuffle the decision to CSE or not using environment
    //     variable COMPlus_JitStressBiasedCSE (= 0 to 100%). When the bias value
    //     is not specified but COMPlus_JitStress is ON, generate a random bias.
    //
    // Return Value:
    //      0 -- This method is indifferent about this CSE (no bias specified and no stress)
    //      1 -- This CSE must be performed to maintain specified/generated bias.
    //     -1 -- This CSE mustn't be performed to maintain specified/generated bias.
    //
    // Operation:
    //     A debug stress only method that returns "1" with probability (P)
    //     defined by:
    //
    //         P = (COMPlus_JitStressBiasedCSE / 100) (or)
    //         P = (random(100) / 100) when COMPlus_JitStress is specified and
    //                                 COMPlus_JitStressBiasedCSE is unspecified.
    //
    //     When specified, the bias is reinterpreted as a decimal number between 0
    //     to 100.
    //     When bias is not specified, a bias is randomly generated if COMPlus_JitStress
    //     is non-zero.
    //
    //     Callers are supposed to call this method for each CSE promotion decision
    //     and ignore the call if return value is 0 and honor the 1 with a CSE and
    //     -1 with a no-CSE to maintain the specified/generated bias.
    //
    int optConfigBiasedCSE()
    {
        // Seed the PRNG, if never done before.
        if (!m_cseRNG.IsInitialized())
        {
            m_cseRNG.Init(m_pCompiler->info.compMethodHash());
            m_bias = m_cseRNG.Next(100);
        }

        // Obtain the bias value and reinterpret as decimal.
        unsigned bias = ReinterpretHexAsDecimal(JitConfig.JitStressBiasedCSE());

        // Invalid value, check if JitStress is ON.
        if (bias > 100)
        {
            if (!m_pCompiler->compStressCompile(Compiler::STRESS_MAKE_CSE, MAX_STRESS_WEIGHT))
            {
                // JitStress is OFF for CSE, nothing to do.
                return 0;
            }
            bias = m_bias;
            JITDUMP("JitStressBiasedCSE is OFF, but JitStress is ON: generated bias=%d.\n", bias);
        }

        // Generate a number between (0, 99) and if the generated
        // number is smaller than bias, then perform CSE.
        unsigned gen = m_cseRNG.Next(100);
        int      ret = (gen < bias) ? 1 : -1;

        if (m_pCompiler->verbose)
        {
            if (ret < 0)
            {
                printf("No CSE because gen=%d >= bias=%d\n", gen, bias);
            }
            else
            {
                printf("Promoting CSE because gen=%d < bias=%d\n", gen, bias);
            }
        }

        // Indicate whether to perform CSE or not.
        return ret;
    }
#endif

    // Given a CSE candidate decide whether it passes or fails the profitablity heuristic
    // return true if we believe that it is profitable to promote this candidate to a CSE
    //
    bool PromotionCheck(CSE_Candidate* candidate)
    {
        bool result = false;

#ifdef DEBUG
        int stressResult = optConfigBiasedCSE();
        if (stressResult != 0)
        {
            // Stress is enabled. Check whether to perform CSE or not.
            return (stressResult > 0);
        }

        if (m_pCompiler->optConfigDisableCSE2())
        {
            return false; // skip this CSE
        }
#endif

        /*
            Our calculation is based on the following cost estimate formula

            Existing costs are:

            (def + use) * cost

            If we introduce a CSE temp are each definition and
            replace the use with a CSE temp then our cost is:

            (def * (cost + cse-def-cost)) + (use * cse-use-cost)

            We must estimate the values to use for cse-def-cost and cse-use-cost

            If we are able to enregister the CSE then the cse-use-cost is one
            and cse-def-cost is either zero or one.  Zero in the case where
            we needed to evaluate the def into a register and we can use that
            register as the CSE temp as well.

            If we are unable to enregister the CSE then the cse-use-cost is IND_COST
            and the cse-def-cost is also IND_COST.

            If we want to be conservative we use IND_COST as the the value
            for both cse-def-cost and cse-use-cost and then we never introduce
            a CSE that could pessimize the execution time of the method.

            If we want to be more moderate we use (IND_COST_EX + 1) / 2 as the
            values for both cse-def-cost and cse-use-cost.

            If we want to be aggressive we use 1 as the values for both
            cse-def-cost and cse-use-cost.

            If we believe that the CSE very valuable in terms of weighted ref counts
            such that it would always be enregistered by the register allocator we choose
            the aggressive use def costs.

            If we believe that the CSE is somewhat valuable in terms of weighted ref counts
            such that it could be likely be enregistered by the register allocator we choose
            the moderate use def costs.

            otherwise we choose the conservative use def costs.

        */

        unsigned cse_def_cost;
        unsigned cse_use_cost;

        unsigned no_cse_cost    = 0;
        unsigned yes_cse_cost   = 0;
        unsigned extra_yes_cost = 0;
        unsigned extra_no_cost  = 0;

        // The 'cseRefCnt' is the RefCnt that we will have if we promote this CSE into a new LclVar
        // Each CSE Def will contain two Refs and each CSE Use wil have one Ref of this new LclVar
        unsigned cseRefCnt = (candidate->DefCount() * 2) + candidate->UseCount();

        if (CodeOptKind() == Compiler::SMALL_CODE)
        {
            if (cseRefCnt >= aggressiveRefCnt)
            {
#ifdef DEBUG
                if (m_pCompiler->verbose)
                {
                    printf("Aggressive CSE Promotion (%u >= %u)\n", cseRefCnt, aggressiveRefCnt);
                }
#endif
                cse_def_cost = 1;
                cse_use_cost = 1;

                if (candidate->LiveAcrossCall() != 0)
                {
                    if (largeFrame)
                    {
                        cse_def_cost++;
                        cse_use_cost++;
                    }
                    if (hugeFrame)
                    {
                        cse_def_cost++;
                        cse_use_cost++;
                    }
                }
            }
            else if (largeFrame)
            {
#ifdef DEBUG
                if (m_pCompiler->verbose)
                {
                    printf("Codesize CSE Promotion (large frame)\n");
                }
#endif
#ifdef _TARGET_XARCH_
                /* The following formula is good choice when optimizing CSE for SMALL_CODE */
                cse_def_cost = 6; // mov [EBP-0x00001FC],reg
                cse_use_cost = 5; //     [EBP-0x00001FC]
#else                             // _TARGET_ARM_
                if (hugeFrame)
                {
                    cse_def_cost = 12; // movw/movt r10 and str reg,[sp+r10]
                    cse_use_cost = 12;
                }
                else
                {
                    cse_def_cost = 8; // movw r10 and str reg,[sp+r10]
                    cse_use_cost = 8;
                }
#endif
            }
            else // small frame
            {
#ifdef DEBUG
                if (m_pCompiler->verbose)
                {
                    printf("Codesize CSE Promotion (small frame)\n");
                }
#endif
#ifdef _TARGET_XARCH_
                /* The following formula is good choice when optimizing CSE for SMALL_CODE */
                cse_def_cost = 3; // mov [EBP-1C],reg
                cse_use_cost = 2; //     [EBP-1C]
#else                             // _TARGET_ARM_
                cse_def_cost = 2; // str reg,[sp+0x9c]
                cse_use_cost = 2; // ldr reg,[sp+0x9c]
#endif
            }
        }
        else // not SMALL_CODE ...
        {
            if (cseRefCnt >= aggressiveRefCnt)
            {
#ifdef DEBUG
                if (m_pCompiler->verbose)
                {
                    printf("Aggressive CSE Promotion (%u >= %u)\n", cseRefCnt, aggressiveRefCnt);
                }
#endif
                cse_def_cost = 1;
                cse_use_cost = 1;
            }
            else if (cseRefCnt >= moderateRefCnt)
            {

                if (candidate->LiveAcrossCall() == 0)
                {
#ifdef DEBUG
                    if (m_pCompiler->verbose)
                    {
                        printf("Moderate CSE Promotion (CSE never live at call) (%u >= %u)\n", cseRefCnt,
                               moderateRefCnt);
                    }
#endif
                    cse_def_cost = 2;
                    cse_use_cost = 1;
                }
                else // candidate is live across call
                {
#ifdef DEBUG
                    if (m_pCompiler->verbose)
                    {
                        printf("Moderate CSE Promotion (%u >= %u)\n", cseRefCnt, moderateRefCnt);
                    }
#endif
                    cse_def_cost   = 2;
                    cse_use_cost   = 2;
                    extra_yes_cost = BB_UNITY_WEIGHT * 2; // Extra cost in case we have to spill/restore a caller
                                                          // saved register
                }
            }
            else // Conservative CSE promotion
            {
                if (candidate->LiveAcrossCall() == 0)
                {
#ifdef DEBUG
                    if (m_pCompiler->verbose)
                    {
                        printf("Conservative CSE Promotion (CSE never live at call) (%u < %u)\n", cseRefCnt,
                               moderateRefCnt);
                    }
#endif
                    cse_def_cost = 2;
                    cse_use_cost = 2;
                }
                else // candidate is live across call
                {
#ifdef DEBUG
                    if (m_pCompiler->verbose)
                    {
                        printf("Conservative CSE Promotion (%u < %u)\n", cseRefCnt, moderateRefCnt);
                    }
#endif
                    cse_def_cost   = 3;
                    cse_use_cost   = 3;
                    extra_yes_cost = BB_UNITY_WEIGHT * 4; // Extra cost in case we have to spill/restore a caller
                                                          // saved register
                }

                // If we have maxed out lvaTrackedCount then this CSE may end up as an untracked variable
                if (m_pCompiler->lvaTrackedCount == lclMAX_TRACKED)
                {
                    cse_def_cost++;
                    cse_use_cost++;
                }
            }

            if (largeFrame)
            {
                cse_def_cost++;
                cse_use_cost++;
            }
            if (hugeFrame)
            {
                cse_def_cost++;
                cse_use_cost++;
            }
        }

        // estimate the cost from lost codesize reduction if we do not perform the CSE
        if (candidate->Size() > cse_use_cost)
        {
            Compiler::CSEdsc* dsc = candidate->CseDsc(); // We need to retrieve the actual use count, not the
                                                         // weighted count
            extra_no_cost = candidate->Size() - cse_use_cost;
            extra_no_cost = extra_no_cost * dsc->csdUseCount * 2;
        }

        /* no_cse_cost  is the cost estimate when we decide not to make a CSE */
        /* yes_cse_cost is the cost estimate when we decide to make a CSE     */

        no_cse_cost  = candidate->UseCount() * candidate->Cost();
        yes_cse_cost = (candidate->DefCount() * cse_def_cost) + (candidate->UseCount() * cse_use_cost);

#if CPU_LONG_USES_REGPAIR
        if (candidate->Expr()->TypeGet() == TYP_LONG)
        {
            yes_cse_cost *= 2;
        }
#endif
        no_cse_cost += extra_no_cost;
        yes_cse_cost += extra_yes_cost;

#ifdef DEBUG
        if (m_pCompiler->verbose)
        {
            printf("cseRefCnt=%d, aggressiveRefCnt=%d, moderateRefCnt=%d\n", cseRefCnt, aggressiveRefCnt,
                   moderateRefCnt);
            printf("defCnt=%d, useCnt=%d, cost=%d, size=%d\n", candidate->DefCount(), candidate->UseCount(),
                   candidate->Cost(), candidate->Size());
            printf("def_cost=%d, use_cost=%d, extra_no_cost=%d, extra_yes_cost=%d\n", cse_def_cost, cse_use_cost,
                   extra_no_cost, extra_yes_cost);

            printf("CSE cost savings check (%u >= %u) %s\n", no_cse_cost, yes_cse_cost,
                   (no_cse_cost >= yes_cse_cost) ? "passes" : "fails");
        }
#endif

        // Should we make this candidate into a CSE?
        // Is the yes cost less than the no cost
        //
        if (yes_cse_cost <= no_cse_cost)
        {
            result = true; // Yes make this a CSE
        }
        else
        {
            /* In stress mode we will make some extra CSEs */
            if (no_cse_cost > 0)
            {
                int percentage = (no_cse_cost * 100) / yes_cse_cost;

                if (m_pCompiler->compStressCompile(Compiler::STRESS_MAKE_CSE, percentage))
                {
                    result = true; // Yes make this a CSE
                }
            }
        }

        return result;
    }

    // PerformCSE() takes a successful candidate and performs  the appropriate replacements:
    //
    // It will replace all of the CSE defs with assignments to a new "cse0" LclVar
    // and will replace all of the CSE uses with reads of the "cse0" LclVar
    //
    void PerformCSE(CSE_Candidate* successfulCandidate)
    {
        unsigned cseRefCnt = (successfulCandidate->DefCount() * 2) + successfulCandidate->UseCount();

        if (successfulCandidate->LiveAcrossCall() != 0)
        {
            // As we introduce new LclVars for these CSE we slightly
            // increase the cutoffs for aggressive and moderate CSE's
            //
            int incr = BB_UNITY_WEIGHT;

#if CPU_LONG_USES_REGPAIR
            if (successfulCandidate->Expr()->TypeGet() == TYP_LONG)
                incr *= 2;
#endif

            if (cseRefCnt > aggressiveRefCnt)
            {
                aggressiveRefCnt += incr;
            }

            if (cseRefCnt > moderateRefCnt)
            {
                moderateRefCnt += (incr / 2);
            }
        }

        /* Introduce a new temp for the CSE */

        // we will create a  long lifetime temp for the new cse LclVar
        unsigned  cseLclVarNum = m_pCompiler->lvaGrabTemp(false DEBUGARG("ValNumCSE"));
        var_types cseLclVarTyp = genActualType(successfulCandidate->Expr()->TypeGet());
        if (varTypeIsStruct(cseLclVarTyp))
        {
            m_pCompiler->lvaSetStruct(cseLclVarNum, m_pCompiler->gtGetStructHandle(successfulCandidate->Expr()), false);
        }
        m_pCompiler->lvaTable[cseLclVarNum].lvType  = cseLclVarTyp;
        m_pCompiler->lvaTable[cseLclVarNum].lvIsCSE = true;

        m_addCSEcount++; // Record that we created a new LclVar for use as a CSE temp
        m_pCompiler->optCSEcount++;

        /*  Walk all references to this CSE, adding an assignment
            to the CSE temp to all defs and changing all refs to
            a simple use of the CSE temp.

            We also unmark nested CSE's for all uses.
        */

        Compiler::treeStmtLstPtr lst;
        lst = successfulCandidate->CseDsc()->csdTreeList;
        noway_assert(lst);

#define QQQ_CHECK_CSE_VNS 0
#if QQQ_CHECK_CSE_VNS
        assert(lst != NULL);
        ValueNum firstVN = lst->tslTree->gtVN;
        lst              = lst->tslNext;
        bool allSame     = true;
        while (lst != NULL)
        {
            if (IS_CSE_INDEX(lst->tslTree->gtCSEnum))
            {
                if (lst->tslTree->gtVN != firstVN)
                {
                    allSame = false;
                    break;
                }
            }
            lst = lst->tslNext;
        }
        if (!allSame)
        {
            lst                  = dsc->csdTreeList;
            GenTreePtr firstTree = lst->tslTree;
            printf("In %s, CSE (oper = %s, type = %s) has differing VNs: ", info.compFullName,
                   GenTree::NodeName(firstTree->OperGet()), varTypeName(firstTree->TypeGet()));
            while (lst != NULL)
            {
                if (IS_CSE_INDEX(lst->tslTree->gtCSEnum))
                {
                    printf("0x%x(%s,%d)    ", lst->tslTree, IS_CSE_USE(lst->tslTree->gtCSEnum) ? "u" : "d",
                           lst->tslTree->gtVN);
                }
                lst = lst->tslNext;
            }
            printf("\n");
        }
        lst = dsc->csdTreeList;
#endif

        do
        {
            /* Process the next node in the list */
            GenTreePtr exp = lst->tslTree;
            GenTreePtr stm = lst->tslStmt;
            noway_assert(stm->gtOper == GT_STMT);
            BasicBlock* blk = lst->tslBlock;

            /* Advance to the next node in the list */
            lst = lst->tslNext;

            // Assert if we used DEBUG_DESTROY_NODE on this CSE exp
            assert(exp->gtOper != GT_COUNT);

            /* Ignore the node if it's not been marked as a CSE */
            if (!IS_CSE_INDEX(exp->gtCSEnum))
            {
                continue;
            }

            /* Make sure we update the weighted ref count correctly */
            m_pCompiler->optCSEweight = blk->getBBWeight(m_pCompiler);

            /* Figure out the actual type of the value */
            var_types expTyp = genActualType(exp->TypeGet());
            noway_assert(expTyp == cseLclVarTyp);

            // This will contain the replacement tree for exp
            // It will either be the CSE def or CSE ref
            //
            GenTreePtr    cse = nullptr;
            bool          isDef;
            FieldSeqNode* fldSeq               = nullptr;
            bool          hasZeroMapAnnotation = m_pCompiler->GetZeroOffsetFieldMap()->Lookup(exp, &fldSeq);

            if (IS_CSE_USE(exp->gtCSEnum))
            {
                /* This is a use of the CSE */
                isDef = false;
#ifdef DEBUG
                if (m_pCompiler->verbose)
                {
                    printf("\nCSE #%02u use at ", exp->gtCSEnum);
                    Compiler::printTreeID(exp);
                    printf(" replaced in BB%02u with temp use.\n", blk->bbNum);
                }
#endif // DEBUG

                /* check for and collect any SIDE_EFFECTS */
                GenTreePtr sideEffList = nullptr;

                if (exp->gtFlags & GTF_PERSISTENT_SIDE_EFFECTS)
                {
                    // Extract any side effects from exp
                    //
                    m_pCompiler->gtExtractSideEffList(exp, &sideEffList, GTF_PERSISTENT_SIDE_EFFECTS_IN_CSE);
                }

                // We will replace the CSE ref with a new tree
                // this is typically just a simple use of the new CSE LclVar
                //
                cse           = m_pCompiler->gtNewLclvNode(cseLclVarNum, cseLclVarTyp);
                cse->gtVNPair = exp->gtVNPair; // assign the proper Value Numbers
#ifdef DEBUG
                cse->gtDebugFlags |= GTF_DEBUG_VAR_CSE_REF;
#endif // DEBUG

                // If we have side effects then we need to create a GT_COMMA tree instead
                //
                if (sideEffList)
                {
                    noway_assert(sideEffList->gtFlags & GTF_SIDE_EFFECT);
#ifdef DEBUG
                    if (m_pCompiler->verbose)
                    {
                        printf("\nThe CSE has side effects! Extracting side effects...\n");
                        m_pCompiler->gtDispTree(sideEffList);
                        printf("\n");
                    }
#endif

                    GenTreePtr     cseVal         = cse;
                    GenTreePtr     curSideEff     = sideEffList;
                    ValueNumStore* vnStore        = m_pCompiler->vnStore;
                    ValueNumPair   exceptions_vnp = ValueNumStore::VNPForEmptyExcSet();

                    while ((curSideEff->OperGet() == GT_COMMA) || (curSideEff->OperGet() == GT_ASG))
                    {
                        GenTreePtr op1 = curSideEff->gtOp.gtOp1;
                        GenTreePtr op2 = curSideEff->gtOp.gtOp2;

                        ValueNumPair op1vnp;
                        ValueNumPair op1Xvnp = ValueNumStore::VNPForEmptyExcSet();
                        vnStore->VNPUnpackExc(op1->gtVNPair, &op1vnp, &op1Xvnp);

                        exceptions_vnp = vnStore->VNPExcSetUnion(exceptions_vnp, op1Xvnp);
                        curSideEff     = op2;
                    }

                    // We may have inserted a narrowing cast during a previous remorph
                    // and it will not have a value number.
                    if ((curSideEff->OperGet() == GT_CAST) && !curSideEff->gtVNPair.BothDefined())
                    {
                        // The inserted cast will have no exceptional effects
                        assert(curSideEff->gtOverflow() == false);
                        // Process the exception effects from the cast's operand.
                        curSideEff = curSideEff->gtOp.gtOp1;
                    }

                    ValueNumPair op2vnp;
                    ValueNumPair op2Xvnp = ValueNumStore::VNPForEmptyExcSet();
                    vnStore->VNPUnpackExc(curSideEff->gtVNPair, &op2vnp, &op2Xvnp);
                    exceptions_vnp = vnStore->VNPExcSetUnion(exceptions_vnp, op2Xvnp);

                    op2Xvnp = ValueNumStore::VNPForEmptyExcSet();
                    vnStore->VNPUnpackExc(cseVal->gtVNPair, &op2vnp, &op2Xvnp);
                    exceptions_vnp = vnStore->VNPExcSetUnion(exceptions_vnp, op2Xvnp);

                    /* Create a comma node with the sideEffList as op1 */
                    cse           = m_pCompiler->gtNewOperNode(GT_COMMA, expTyp, sideEffList, cseVal);
                    cse->gtVNPair = vnStore->VNPWithExc(op2vnp, exceptions_vnp);
                }

                exp->gtCSEnum = NO_CSE; // clear the gtCSEnum field

                /* Unmark any nested CSE's in the sub-operands */

                // But we do need to communicate the side effect list to optUnmarkCSEs
                // as any part of the 'exp' tree that is in the sideEffList is preserved
                // and is not deleted and does not have its ref counts decremented
                //
                m_pCompiler->optValnumCSE_UnmarkCSEs(exp, sideEffList);
            }
            else
            {
                /* This is a def of the CSE */
                isDef = true;
#ifdef DEBUG
                if (m_pCompiler->verbose)
                {
                    printf("\nCSE #%02u def at ", GET_CSE_INDEX(exp->gtCSEnum));
                    Compiler::printTreeID(exp);
                    printf(" replaced in BB%02u with def of V%02u\n", blk->bbNum, cseLclVarNum);
                }
#endif // DEBUG

                exp->gtCSEnum = NO_CSE; // clear the gtCSEnum field

                GenTreePtr val = exp;

                /* Create an assignment of the value to the temp */
                GenTreePtr asg = m_pCompiler->gtNewTempAssign(cseLclVarNum, val);

                // assign the proper Value Numbers
                asg->gtVNPair.SetBoth(ValueNumStore::VNForVoid()); // The GT_ASG node itself is $VN.Void
                asg->gtOp.gtOp1->gtVNPair = val->gtVNPair;         // The dest op is the same as 'val'

                noway_assert(asg->gtOp.gtOp1->gtOper == GT_LCL_VAR);
                noway_assert(asg->gtOp.gtOp2 == val);

                /* Create a reference to the CSE temp */
                GenTreePtr ref = m_pCompiler->gtNewLclvNode(cseLclVarNum, cseLclVarTyp);
                ref->gtVNPair  = val->gtVNPair; // The new 'ref' is the same as 'val'

                // If it has a zero-offset field seq, copy annotation to the ref
                if (hasZeroMapAnnotation)
                {
                    m_pCompiler->GetZeroOffsetFieldMap()->Set(ref, fldSeq);
                }

                /* Create a comma node for the CSE assignment */
                cse           = m_pCompiler->gtNewOperNode(GT_COMMA, expTyp, asg, ref);
                cse->gtVNPair = ref->gtVNPair; // The comma's value is the same as 'val'
                                               // as the assignment to the CSE LclVar
                                               // cannot add any new exceptions
            }

            // Increment ref count for the CSE ref
            m_pCompiler->lvaTable[cseLclVarNum].incRefCnts(blk->getBBWeight(m_pCompiler), m_pCompiler);

            if (isDef)
            {
                // Also increment ref count for the CSE assignment
                m_pCompiler->lvaTable[cseLclVarNum].incRefCnts(blk->getBBWeight(m_pCompiler), m_pCompiler);
            }

            // Walk the statement 'stm' and find the pointer
            // in the tree is pointing to 'exp'
            //
            GenTreePtr* link = m_pCompiler->gtFindLink(stm, exp);

#ifdef DEBUG
            if (link == nullptr)
            {
                printf("\ngtFindLink failed: stm=");
                Compiler::printTreeID(stm);
                printf(", exp=");
                Compiler::printTreeID(exp);
                printf("\n");
                printf("stm =");
                m_pCompiler->gtDispTree(stm);
                printf("\n");
                printf("exp =");
                m_pCompiler->gtDispTree(exp);
                printf("\n");
            }
#endif // DEBUG

            noway_assert(link);

            // Mutate this link, thus replacing the old exp with the new cse representation
            //
            *link = cse;

            // If it has a zero-offset field seq, copy annotation.
            if (hasZeroMapAnnotation)
            {
                m_pCompiler->GetZeroOffsetFieldMap()->Set(cse, fldSeq);
            }

            assert(m_pCompiler->fgRemoveRestOfBlock == false);

            /* re-morph the statement */
            m_pCompiler->fgMorphBlockStmt(blk, stm DEBUGARG("optValnumCSE"));

        } while (lst != nullptr);
    }

    // Consider each of the CSE candidates and if the CSE passes
    // the PromotionCheck then transform the CSE by calling PerformCSE
    //
    void ConsiderCandidates()
    {
        /* Consider each CSE candidate, in order of decreasing cost */
        unsigned           cnt = m_pCompiler->optCSECandidateCount;
        Compiler::CSEdsc** ptr = sortTab;
        for (; (cnt > 0); cnt--, ptr++)
        {
            Compiler::CSEdsc* dsc = *ptr;
            CSE_Candidate     candidate(this, dsc);

            candidate.InitializeCounts();

            if (candidate.UseCount() == 0)
            {
#ifdef DEBUG
                if (m_pCompiler->verbose)
                {
                    printf("Skipped CSE #%02u because use count is 0\n", candidate.CseIndex());
                }
#endif
                continue;
            }

#ifdef DEBUG
            if (m_pCompiler->verbose)
            {
                printf("\nConsidering CSE #%02u [def=%2u, use=%2u, cost=%2u] CSE Expression:\n", candidate.CseIndex(),
                       candidate.DefCount(), candidate.UseCount(), candidate.Cost());
                m_pCompiler->gtDispTree(candidate.Expr());
                printf("\n");
            }
#endif

            if ((dsc->csdDefCount <= 0) || (dsc->csdUseCount == 0))
            {
                // If we reach this point, then the CSE def was incorrectly marked or the
                // block with this use is unreachable. So skip and go to the next CSE.
                // Without the "continue", we'd generate bad code in retail.
                // Commented out a noway_assert(false) here due to bug: 3290124.
                // The problem is if there is sub-graph that is not reachable from the
                // entry point, the CSE flags propagated, would be incorrect for it.
                continue;
            }

            bool doCSE = PromotionCheck(&candidate);

#ifdef DEBUG
            if (m_pCompiler->verbose)
            {
                if (doCSE)
                {
                    printf("\nPromoting CSE:\n");
                }
                else
                {
                    printf("Did Not promote this CSE\n");
                }
            }
#endif // DEBUG

            if (doCSE)
            {
                PerformCSE(&candidate);
            }
        }
    }

    // Perform the necessary cleanup after our CSE heuristics have run
    //
    void Cleanup()
    {
        if (m_addCSEcount > 0)
        {
            /* We've added new local variables to the lvaTable so note that we need to recreate the sorted table */
            m_pCompiler->lvaSortAgain = true;
        }
    }
};

/*****************************************************************************
 *
 *  Routine for performing the Value Number based CSE using our heuristics
 */

void Compiler::optValnumCSE_Heuristic()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("\n************ Trees at start of optValnumCSE_Heuristic()\n");
        fgDumpTrees(fgFirstBB, nullptr);
        printf("\n");
    }
#endif // DEBUG

    CSE_Heuristic cse_heuristic(this);

    cse_heuristic.Initialize();
    cse_heuristic.SortCandidates();
    cse_heuristic.ConsiderCandidates();
    cse_heuristic.Cleanup();
}

/*****************************************************************************
 *
 *  Routine to unmark any CSEs contained within a tree
 *   - optionally a 'keepList' vcan be provided to specify a list of trees that will be kept
 *
 */

void Compiler::optValnumCSE_UnmarkCSEs(GenTreePtr deadTree, GenTreePtr keepList)
{
    assert(optValnumCSE_phase);

    // We need to communicate the 'keepList' to optUnmarkCSEs
    // as any part of the 'deadTree' tree that is in the keepList is preserved
    // and is not deleted and does not have its ref counts decremented
    // We communicate this value using the walkData.pCallbackData field
    //

    fgWalkTreePre(&deadTree, optUnmarkCSEs, (void*)keepList);
}

/*****************************************************************************
 *
 *  Perform common sub-expression elimination.
 */

void Compiler::optOptimizeValnumCSEs()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("\n*************** In optOptimizeValnumCSEs()\n");
    }

    if (optConfigDisableCSE())
    {
        return; // Disabled by JitNoCSE
    }
#endif

    optValnumCSE_phase = true;

    /* Initialize the expression tracking logic */

    optValnumCSE_Init();

    /* Locate interesting expressions and assign indices to them */

    if (optValnumCSE_Locate() > 0)
    {
        optCSECandidateTotal += optCSECandidateCount;

        optValnumCSE_InitDataFlow();

        optValnumCSE_DataFlow();

        optValnumCSE_Availablity();

        optValnumCSE_Heuristic();
    }

    optValnumCSE_phase = false;
}

#endif // FEATURE_VALNUM_CSE

/*****************************************************************************
 *
 *  The following determines whether the given expression is a worthy CSE
 *  candidate.
 */
bool Compiler::optIsCSEcandidate(GenTreePtr tree)
{
    /* No good if the expression contains side effects or if it was marked as DONT CSE */

    if (tree->gtFlags & (GTF_ASG | GTF_DONT_CSE))
    {
        return false;
    }

    /* The only reason a TYP_STRUCT tree might occur is as an argument to
       GT_ADDR. It will never be actually materialized. So ignore them.
       Also TYP_VOIDs */

    var_types  type = tree->TypeGet();
    genTreeOps oper = tree->OperGet();

    // TODO-1stClassStructs: Enable CSE for struct types (depends on either transforming
    // to use regular assignments, or handling copyObj.
    if (varTypeIsStruct(type) || type == TYP_VOID)
    {
        return false;
    }

#ifdef _TARGET_X86_
    if (type == TYP_FLOAT)
    {
        // TODO-X86-CQ: Revisit this
        // Don't CSE a TYP_FLOAT on x86 as we currently can only enregister doubles
        return false;
    }
#else
    if (oper == GT_CNS_DBL)
    {
        // TODO-CQ: Revisit this
        // Don't try to CSE a GT_CNS_DBL as they can represent both float and doubles
        return false;
    }
#endif

    unsigned cost;
    if (compCodeOpt() == SMALL_CODE)
    {
        cost = tree->gtCostSz;
    }
    else
    {
        cost = tree->gtCostEx;
    }

    /* Don't bother if the potential savings are very low */
    if (cost < MIN_CSE_COST)
    {
        return false;
    }

#if !CSE_CONSTS
    /* Don't bother with constants */
    if (tree->OperKind() & GTK_CONST)
        return false;
#endif

    /* Check for some special cases */

    switch (oper)
    {
        case GT_CALL:
            // If we have a simple helper call with no other persistent side-effects
            // then we allow this tree to be a CSE candidate
            //
            if (gtTreeHasSideEffects(tree, GTF_PERSISTENT_SIDE_EFFECTS_IN_CSE) == false)
            {
                return true;
            }
            else
            {
                // Calls generally cannot be CSE-ed
                return false;
            }

        case GT_IND:
            // TODO-CQ: Review this...
            /* We try to cse GT_ARR_ELEM nodes instead of GT_IND(GT_ARR_ELEM).
                Doing the first allows cse to also kick in for code like
                "GT_IND(GT_ARR_ELEM) = GT_IND(GT_ARR_ELEM) + xyz", whereas doing
                the second would not allow it */

            return (tree->gtOp.gtOp1->gtOper != GT_ARR_ELEM);

        case GT_CNS_INT:
        case GT_CNS_LNG:
        case GT_CNS_DBL:
        case GT_CNS_STR:
            return true; // We reach here only when CSE_CONSTS is enabled

        case GT_ARR_ELEM:
        case GT_ARR_LENGTH:
        case GT_CLS_VAR:
        case GT_LCL_FLD:
            return true;

        case GT_LCL_VAR:
            return false; // Can't CSE a volatile LCL_VAR

        case GT_NEG:
        case GT_NOT:
        case GT_CAST:
            return true; // CSE these Unary Operators

        case GT_SUB:
        case GT_DIV:
        case GT_MOD:
        case GT_UDIV:
        case GT_UMOD:
        case GT_OR:
        case GT_AND:
        case GT_XOR:
        case GT_RSH:
        case GT_RSZ:
        case GT_ROL:
        case GT_ROR:
            return true; // CSE these Binary Operators

        case GT_ADD: // Check for ADDRMODE flag on these Binary Operators
        case GT_MUL:
        case GT_LSH:
            if ((tree->gtFlags & GTF_ADDRMODE_NO_CSE) != 0)
            {
                return false;
            }

        case GT_EQ:
        case GT_NE:
        case GT_LT:
        case GT_LE:
        case GT_GE:
        case GT_GT:
            return true; // Also CSE these Comparison Operators

        case GT_INTRINSIC:
            return true; // Intrinsics

        case GT_COMMA:
            return true; // Allow GT_COMMA nodes to be CSE-ed.

        case GT_COLON:
        case GT_QMARK:
        case GT_NOP:
        case GT_RETURN:
            return false; // Currently the only special nodes that we hit
                          // that we know that we don't want to CSE

        default:
            break; // Any new nodes that we might add later...
    }

    return false;
}

#ifdef DEBUG
//
// A Debug only method that allows you to control whether the CSE logic is enabled for this method.
//
// If this method returns false then the CSE phase should be performed.
// If the method returns true then the CSE phase should be skipped.
//
bool Compiler::optConfigDisableCSE()
{
    // Next check if COMPlus_JitNoCSE is set and applies to this method
    //
    unsigned jitNoCSE = JitConfig.JitNoCSE();

    if (jitNoCSE > 0)
    {
        unsigned methodCount = Compiler::jitTotalMethodCompiled;
        if ((jitNoCSE & 0xF000000) == 0xF000000)
        {
            unsigned methodCountMask = methodCount & 0xFFF;
            unsigned bitsZero        = (jitNoCSE >> 12) & 0xFFF;
            unsigned bitsOne         = (jitNoCSE >> 0) & 0xFFF;

            if (((methodCountMask & bitsOne) == bitsOne) && ((~methodCountMask & bitsZero) == bitsZero))
            {
                if (verbose)
                {
                    printf(" Disabled by JitNoCSE methodCountMask\n");
                }

                return true; // The CSE phase for this method is disabled
            }
        }
        else if (jitNoCSE <= (methodCount + 1))
        {
            if (verbose)
            {
                printf(" Disabled by JitNoCSE > methodCount\n");
            }

            return true; // The CSE phase for this method is disabled
        }
    }

    return false;
}

//
// A Debug only method that allows you to control whether the CSE logic is enabled for
// a particular CSE in a method
//
// If this method returns false then the CSE should be performed.
// If the method returns true then the CSE should be skipped.
//
bool Compiler::optConfigDisableCSE2()
{
    static unsigned totalCSEcount = 0;

    unsigned jitNoCSE2 = JitConfig.JitNoCSE2();

    totalCSEcount++;

    if (jitNoCSE2 > 0)
    {
        if ((jitNoCSE2 & 0xF000000) == 0xF000000)
        {
            unsigned totalCSEMask = totalCSEcount & 0xFFF;
            unsigned bitsZero     = (jitNoCSE2 >> 12) & 0xFFF;
            unsigned bitsOne      = (jitNoCSE2 >> 0) & 0xFFF;

            if (((totalCSEMask & bitsOne) == bitsOne) && ((~totalCSEMask & bitsZero) == bitsZero))
            {
                if (verbose)
                {
                    printf(" Disabled by jitNoCSE2 Ones/Zeros mask\n");
                }
                return true;
            }
        }
        else if ((jitNoCSE2 & 0xF000000) == 0xE000000)
        {
            unsigned totalCSEMask = totalCSEcount & 0xFFF;
            unsigned disableMask  = jitNoCSE2 & 0xFFF;

            disableMask >>= (totalCSEMask % 12);

            if (disableMask & 1)
            {
                if (verbose)
                {
                    printf(" Disabled by jitNoCSE2 rotating disable mask\n");
                }
                return true;
            }
        }
        else if (jitNoCSE2 <= totalCSEcount)
        {
            if (verbose)
            {
                printf(" Disabled by jitNoCSE2 > totalCSEcount\n");
            }
            return true;
        }
    }
    return false;
}
#endif

void Compiler::optOptimizeCSEs()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("\n*************** In optOptimizeCSEs()\n");
        printf("Blocks/Trees at start of optOptimizeCSE phase\n");
        fgDispBasicBlocks(true);
    }
#endif // DEBUG

    optCSECandidateCount = 0;
    optCSEstart          = lvaCount;

#if FEATURE_VALNUM_CSE
    INDEBUG(optEnsureClearCSEInfo());
    optOptimizeValnumCSEs();
    EndPhase(PHASE_OPTIMIZE_VALNUM_CSES);
#endif // FEATURE_VALNUM_CSE
}

/*****************************************************************************
 *
 *  Cleanup after CSE to allow us to run more than once.
 */

void Compiler::optCleanupCSEs()
{
    // We must clear the BBF_VISITED and BBF_MARKED flags
    //
    for (BasicBlock* block = fgFirstBB; block; block = block->bbNext)
    {
        unsigned blkFlags = block->bbFlags;

        // And clear all the "visited" bits on the block
        //
        block->bbFlags &= ~(BBF_VISITED | BBF_MARKED);

        /* Walk the statement trees in this basic block */

        GenTreePtr stmt;

        // Initialize 'stmt' to the first non-Phi statement
        stmt = block->FirstNonPhiDef();

        for (; stmt; stmt = stmt->gtNext)
        {
            noway_assert(stmt->gtOper == GT_STMT);

            /* We must clear the gtCSEnum field */
            for (GenTreePtr tree = stmt->gtStmt.gtStmtExpr; tree; tree = tree->gtPrev)
            {
                tree->gtCSEnum = NO_CSE;
            }
        }
    }
}

#ifdef DEBUG

/*****************************************************************************
 *
 *  Ensure that all the CSE information in the IR is initialized the way we expect it,
 *  before running a CSE phase. This is basically an assert that optCleanupCSEs() is not needed.
 */

void Compiler::optEnsureClearCSEInfo()
{
    for (BasicBlock* block = fgFirstBB; block; block = block->bbNext)
    {
        assert((block->bbFlags & (BBF_VISITED | BBF_MARKED)) == 0);

        /* Walk the statement trees in this basic block */

        GenTreePtr stmt;

        // Initialize 'stmt' to the first non-Phi statement
        stmt = block->FirstNonPhiDef();

        for (; stmt; stmt = stmt->gtNext)
        {
            assert(stmt->gtOper == GT_STMT);

            for (GenTreePtr tree = stmt->gtStmt.gtStmtExpr; tree; tree = tree->gtPrev)
            {
                assert(tree->gtCSEnum == NO_CSE);
            }
        }
    }
}

#endif // DEBUG

/*****************************************************************************/
#endif // FEATURE_ANYCSE
/*****************************************************************************/
