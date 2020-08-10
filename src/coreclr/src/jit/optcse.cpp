// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
const size_t Compiler::s_optCSEhashSizeInitial  = EXPSET_SZ * 2;
const size_t Compiler::s_optCSEhashGrowthFactor = 2;
const size_t Compiler::s_optCSEhashBucketSize   = 4;

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
    size_t   cnt;

    optCSEtab = new (this, CMK_CSE) CSEdsc*[optCSECandidateCount]();

    for (cnt = optCSEhashSize, ptr = optCSEhash; cnt; cnt--, ptr++)
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

//------------------------------------------------------------------------
// Compiler::optUnmarkCSE
//
// Arguments:
//    tree  - A sub tree that originally was part of a CSE use
//            that we are currently in the process of removing.
//
// Return Value:
//    Returns true if we can safely remove the 'tree' node.
//    Returns false if the node is a CSE def that the caller
//    needs to extract and preserve.
//
// Notes:
//    If 'tree' is a CSE use then we perform an unmark CSE operation
//    so that the CSE used counts and weight are updated properly.
//    The only caller for this method is optUnmarkCSEs which is a
//    tree walker visitor function.  When we return false this method
//    returns WALK_SKIP_SUBTREES so that we don't visit the remaining
//    nodes of the CSE def.
//
bool Compiler::optUnmarkCSE(GenTree* tree)
{
    if (!IS_CSE_INDEX(tree->gtCSEnum))
    {
        // If this node isn't a CSE use or def we can safely remove this node.
        //
        return true;
    }

    // make sure it's been initialized
    noway_assert(optCSEweight <= BB_MAX_WEIGHT);

    // Is this a CSE use?
    if (IS_CSE_USE(tree->gtCSEnum))
    {
        unsigned CSEnum = GET_CSE_INDEX(tree->gtCSEnum);
        CSEdsc*  desc   = optCSEfindDsc(CSEnum);

#ifdef DEBUG
        if (verbose)
        {
            printf("Unmark CSE use #%02d at ", CSEnum);
            printTreeID(tree);
            printf(": %3d -> %3d\n", desc->csdUseCount, desc->csdUseCount - 1);
        }
#endif // DEBUG

        // Perform an unmark CSE operation

        // 1. Reduce the nested CSE's 'use' count

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

        // 2. Unmark the CSE infomation in the node

        tree->gtCSEnum = NO_CSE;
        return true;
    }
    else
    {
        // It is not safe to remove this node, so we will return false
        // and the caller must add this node to the side effect list
        //
        return false;
    }
}

Compiler::fgWalkResult Compiler::optCSE_MaskHelper(GenTree** pTree, fgWalkData* walkData)
{
    GenTree*         tree      = *pTree;
    Compiler*        comp      = walkData->compiler;
    optCSE_MaskData* pUserData = (optCSE_MaskData*)(walkData->pCallbackData);

    if (IS_CSE_INDEX(tree->gtCSEnum))
    {
        unsigned cseIndex = GET_CSE_INDEX(tree->gtCSEnum);
        unsigned cseBit   = genCSEnum2bit(cseIndex);
        if (IS_CSE_DEF(tree->gtCSEnum))
        {
            BitVecOps::AddElemD(comp->cseMaskTraits, pUserData->CSE_defMask, cseBit);
        }
        else
        {
            BitVecOps::AddElemD(comp->cseMaskTraits, pUserData->CSE_useMask, cseBit);
        }
    }

    return WALK_CONTINUE;
}

// This functions walks all the node for an given tree
// and return the mask of CSE defs and uses for the tree
//
void Compiler::optCSE_GetMaskData(GenTree* tree, optCSE_MaskData* pMaskData)
{
    pMaskData->CSE_defMask = BitVecOps::MakeEmpty(cseMaskTraits);
    pMaskData->CSE_useMask = BitVecOps::MakeEmpty(cseMaskTraits);
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

    // If we haven't setup cseMaskTraits, do it now
    if (cseMaskTraits == nullptr)
    {
        cseMaskTraits = new (getAllocator()) BitVecTraits(optCSECandidateCount, this);
    }

    optCSE_MaskData op1MaskData;
    optCSE_MaskData op2MaskData;

    optCSE_GetMaskData(op1, &op1MaskData);
    optCSE_GetMaskData(op2, &op2MaskData);

    // We cannot swap if op1 contains a CSE def that is used by op2
    if (!BitVecOps::IsEmptyIntersection(cseMaskTraits, op1MaskData.CSE_defMask, op2MaskData.CSE_useMask))
    {
        canSwap = false;
    }
    else
    {
        // We also cannot swap if op2 contains a CSE def that is used by op1.
        if (!BitVecOps::IsEmptyIntersection(cseMaskTraits, op2MaskData.CSE_defMask, op1MaskData.CSE_useMask))
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
bool Compiler::optCSE_canSwap(GenTree* tree)
{
    // We must have a binary treenode with non-null op1 and op2
    assert((tree->OperKind() & GTK_SMPOP) != 0);

    GenTree* op1 = tree->AsOp()->gtOp1;
    GenTree* op2 = tree->gtGetOp2();

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

    GenTree* exp1 = dsc1->csdTree;
    GenTree* exp2 = dsc2->csdTree;

    int diff;

    diff = (int)(exp2->GetCostEx() - exp1->GetCostEx());

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

    GenTree* exp1 = dsc1->csdTree;
    GenTree* exp2 = dsc2->csdTree;

    int diff;

    diff = (int)(exp2->GetCostSz() - exp1->GetCostSz());

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

    // This gets set in optValnumCSE_InitDataFlow
    cseLivenessTraits = nullptr;

    // Initialize when used by optCSE_canSwap()
    cseMaskTraits = nullptr;

    // Allocate and clear the hash bucket table
    optCSEhash = new (this, CMK_CSE) CSEdsc*[s_optCSEhashSizeInitial]();

    optCSEhashSize                 = s_optCSEhashSizeInitial;
    optCSEhashMaxCountBeforeResize = optCSEhashSize * s_optCSEhashBucketSize;
    optCSEhashCount                = 0;

    optCSECandidateCount = 0;
    optDoCSE             = false; // Stays false until we find duplicate CSE tree

    // optCseCheckedBoundMap is unused in most functions, allocated only when used
    optCseCheckedBoundMap = nullptr;
}

unsigned optCSEKeyToHashIndex(size_t key, size_t optCSEhashSize)
{
    unsigned hash;

    hash = (unsigned)key;
#ifdef TARGET_64BIT
    hash ^= (unsigned)(key >> 32);
#endif
    hash *= (unsigned)(optCSEhashSize + 1);
    hash >>= 7;

    return hash % optCSEhashSize;
}

//---------------------------------------------------------------------------
// optValnumCSE_Index:
//               - Returns the CSE index to use for this tree,
//                 or zero if this expression is not currently a CSE.
//
// Arguments:
//    tree       - The current candidate CSE expression
//    stmt       - The current statement that contains tree
//
//
// Notes:   We build a hash table that contains all of the expressions that
//          are presented to this method.  Whenever we see a duplicate expression
//          we have a CSE candidate.  If it is the first time seeing the duplicate
//          we allocate a new CSE index. If we have already allocated a CSE index
//          we return that index.  There currently is a limit on the number of CSEs
//          that we can have of MAX_CSE_CNT (64)
//
unsigned Compiler::optValnumCSE_Index(GenTree* tree, Statement* stmt)
{
    size_t   key;
    unsigned hval;
    CSEdsc*  hashDsc;
    bool     isIntConstHash       = false;
    bool     enableSharedConstCSE = false;
    int      configValue          = JitConfig.JitConstCSE();

#if defined(TARGET_ARM64)
    // ARM64 - allow to combine with nearby offsets, when config is not 2 or 4
    if ((configValue != CONST_CSE_ENABLE_ARM64_NO_SHARING) && (configValue != CONST_CSE_ENABLE_ALL_NO_SHARING))
    {
        enableSharedConstCSE = true;
    }
#endif // TARGET_ARM64

    // All Platforms - also allow to combine with nearby offsets, when config is 3
    if (configValue == CONST_CSE_ENABLE_ALL)
    {
        enableSharedConstCSE = true;
    }

    // We use the liberal Value numbers when building the set of CSE
    ValueNum vnLib     = tree->GetVN(VNK_Liberal);
    ValueNum vnLibNorm = vnStore->VNNormalValue(vnLib);

    // We use the normal value number because we want the CSE candidate to
    // represent all expressions that produce the same normal value number
    // We will handle the case where we have different exception sets when
    // promoting the candidates.
    //
    // We do this because a GT_IND will usually have a NullPtrExc entry in its
    // exc set, but we may have cleared the GTF_EXCEPT flag and if so, it won't
    // have an NullPtrExc, or we may have assigned the value of an  GT_IND
    // into a LCL_VAR and then read it back later.
    //
    // When we are promoting the CSE candidates we insure that any CSE
    // uses that we promote have an exc set that is the same as the CSE defs
    // or have an empty set.  And that all of the CSE defs produced the required
    // set of exceptions for the CSE uses.
    //

    // We assign either vnLib or vnLibNorm as the hash key
    //
    // The only exception to using the normal value is for the GT_COMMA nodes.
    // Here we check to see if we have a GT_COMMA with a different value number
    // than the one from its op2.  For this case we want to create two different
    // CSE candidates. This allows us to CSE the GT_COMMA separately from its value.
    //
    if (tree->OperGet() == GT_COMMA)
    {
        // op2 is the value produced by a GT_COMMA
        GenTree* op2      = tree->AsOp()->gtOp2;
        ValueNum vnOp2Lib = op2->GetVN(VNK_Liberal);

        // If the value number for op2 and tree are different, then some new
        // exceptions were produced by op1. For that case we will NOT use the
        // normal value. This allows us to CSE commas with an op1 that is
        // an ARR_BOUNDS_CHECK.
        //
        if (vnOp2Lib != vnLib)
        {
            key = vnLib; // include the exc set in the hash key
        }
        else
        {
            key = vnLibNorm;
        }

        // If we didn't do the above we would have op1 as the CSE def
        // and the parent comma as the CSE use (but with a different exc set)
        // This would prevent us from making any CSE with the comma
        //
        assert(vnLibNorm == vnStore->VNNormalValue(vnOp2Lib));
    }
    else if (enableSharedConstCSE && tree->IsIntegralConst())
    {
        assert(vnStore->IsVNConstant(vnLibNorm));
        key = vnStore->CoercedConstantValue<size_t>(vnLibNorm);

        // We don't shared small offset constants when we require a reloc
        if (!tree->AsIntConCommon()->ImmedValNeedsReloc(this))
        {
            // Make constants that have the same upper bits use the same key

            // Shift the key right by CSE_CONST_SHARED_LOW_BITS bits, this sets the upper bits to zero
            key >>= CSE_CONST_SHARED_LOW_BITS;
        }
        assert((key & TARGET_SIGN_BIT) == 0);

        // We use the sign bit of 'key' as the flag
        // that we are hashing constants (with a shared offset)
        key |= TARGET_SIGN_BIT;
    }
    else // Not a GT_COMMA or a GT_CNS_INT
    {
        key = vnLibNorm;
    }

    // Compute the hash value for the expression

    hval = optCSEKeyToHashIndex(key, optCSEhashSize);

    /* Look for a matching index in the hash table */

    bool newCSE = false;

    for (hashDsc = optCSEhash[hval]; hashDsc; hashDsc = hashDsc->csdNextInBucket)
    {
        if (hashDsc->csdHashKey == key)
        {
            // Check for mismatched types on GT_CNS_INT nodes
            if ((tree->OperGet() == GT_CNS_INT) && (tree->TypeGet() != hashDsc->csdTree->TypeGet()))
            {
                continue;
            }

            treeStmtLst* newElem;

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

                hashDsc->csdTreeList  = newElem;
                hashDsc->csdTreeLast  = newElem;
                hashDsc->csdStructHnd = NO_CLASS_HANDLE;

                hashDsc->csdStructHndMismatch = false;

                if (varTypeIsStruct(tree->gtType))
                {
                    // When we have a GT_IND node with a SIMD type then we don't have a reliable
                    // struct handle and gtGetStructHandleIfPresent returns a guess that can be wrong
                    //
                    if ((hashDsc->csdTree->OperGet() != GT_IND) || !varTypeIsSIMD(tree))
                    {
                        hashDsc->csdStructHnd = gtGetStructHandleIfPresent(hashDsc->csdTree);
                    }
                }
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

            if (varTypeIsStruct(newElem->tslTree->gtType))
            {
                // When we have a GT_IND node with a SIMD type then we don't have a reliable
                // struct handle and gtGetStructHandleIfPresent returns a guess that can be wrong
                //
                if ((newElem->tslTree->OperGet() != GT_IND) || !varTypeIsSIMD(newElem->tslTree))
                {
                    CORINFO_CLASS_HANDLE newElemStructHnd = gtGetStructHandleIfPresent(newElem->tslTree);
                    if (newElemStructHnd != NO_CLASS_HANDLE)
                    {
                        if (hashDsc->csdStructHnd == NO_CLASS_HANDLE)
                        {
                            // The previous node(s) were GT_IND's and didn't carry the struct handle info
                            // The current node does have the struct handle info, so record it now
                            //
                            hashDsc->csdStructHnd = newElemStructHnd;
                        }
                        else if (newElemStructHnd != hashDsc->csdStructHnd)
                        {
                            hashDsc->csdStructHndMismatch = true;
#ifdef DEBUG
                            if (verbose)
                            {
                                printf("Abandoned - CSE candidate has mismatching struct handles!\n");
                                printTreeID(newElem->tslTree);
                            }
#endif // DEBUG
                        }
                    }
                }
            }

            optDoCSE = true; // Found a duplicate CSE tree

            /* Have we assigned a CSE index? */
            if (hashDsc->csdIndex == 0)
            {
                newCSE = true;
                break;
            }

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
            if (optCSEhashCount == optCSEhashMaxCountBeforeResize)
            {
                size_t   newOptCSEhashSize = optCSEhashSize * s_optCSEhashGrowthFactor;
                CSEdsc** newOptCSEhash     = new (this, CMK_CSE) CSEdsc*[newOptCSEhashSize]();

                // Iterate through each existing entry, moving to the new table
                CSEdsc** ptr;
                CSEdsc*  dsc;
                size_t   cnt;
                for (cnt = optCSEhashSize, ptr = optCSEhash; cnt; cnt--, ptr++)
                {
                    for (dsc = *ptr; dsc;)
                    {
                        CSEdsc* nextDsc = dsc->csdNextInBucket;

                        size_t newHval = optCSEKeyToHashIndex(dsc->csdHashKey, newOptCSEhashSize);

                        // Move CSEdsc to bucket in enlarged table
                        dsc->csdNextInBucket   = newOptCSEhash[newHval];
                        newOptCSEhash[newHval] = dsc;

                        dsc = nextDsc;
                    }
                }

                optCSEhash                     = newOptCSEhash;
                optCSEhashSize                 = newOptCSEhashSize;
                optCSEhashMaxCountBeforeResize = optCSEhashMaxCountBeforeResize * s_optCSEhashGrowthFactor;
            }

            ++optCSEhashCount;
            hashDsc = new (this, CMK_CSE) CSEdsc;

            hashDsc->csdHashKey        = key;
            hashDsc->csdConstDefValue  = 0;
            hashDsc->csdConstDefVN     = vnStore->VNForNull(); // uninit value
            hashDsc->csdIndex          = 0;
            hashDsc->csdLiveAcrossCall = false;
            hashDsc->csdDefCount       = 0;
            hashDsc->csdUseCount       = 0;
            hashDsc->csdDefWtCnt       = 0;
            hashDsc->csdUseWtCnt       = 0;
            hashDsc->defExcSetPromise  = vnStore->VNForEmptyExcSet();
            hashDsc->defExcSetCurrent  = vnStore->VNForNull(); // uninit value
            hashDsc->defConservNormVN  = vnStore->VNForNull(); // uninit value

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
#ifdef DEBUG
            if (verbose)
            {
                printf("Exceeded the MAX_CSE_CNT, not using tree:\n");
                gtDispTree(tree);
            }
#endif // DEBUG
            return 0;
        }

        C_ASSERT((signed char)MAX_CSE_CNT == MAX_CSE_CNT);

        unsigned CSEindex = ++optCSECandidateCount;
        // EXPSET_TP  CSEmask  = genCSEnum2bit(CSEindex);

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
            printf("\nCSE candidate #%02u, key=", CSEindex);
            if (!Compiler::Is_Shared_Const_CSE(key))
            {
                vnPrint((unsigned)key, 0);
            }
            else
            {
                size_t kVal = Compiler::Decode_Shared_Const_CSE_Value(key);
                printf("K_%p", dspPtr(kVal));
            }

            printf(" in " FMT_BB ", [cost=%2u, size=%2u]: \n", compCurBB->bbNum, tree->GetCostEx(), tree->GetCostSz());
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
 */

unsigned Compiler::optValnumCSE_Locate()
{
    // Locate CSE candidates and assign them indices

    bool enableConstCSE = true;

    int configValue = JitConfig.JitConstCSE();

    // all platforms - disable CSE of constant values when config is 1
    if (configValue == CONST_CSE_DISABLE_ALL)
    {
        enableConstCSE = false;
    }

#if !defined(TARGET_ARM64)
    // non-ARM64 platforms - disable by default
    //
    enableConstCSE = false;

    // Check for the two enable cases for all platforms
    //
    if ((configValue == CONST_CSE_ENABLE_ALL) || (configValue == CONST_CSE_ENABLE_ALL_NO_SHARING))
    {
        enableConstCSE = true;
    }
#endif

    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        /* Make the block publicly available */

        compCurBB = block;

        /* Ensure that the BBF_VISITED and BBF_MARKED flag are clear */
        /* Everyone who uses these flags are required to clear afterwards */
        noway_assert((block->bbFlags & (BBF_VISITED | BBF_MARKED)) == 0);

        /* Walk the statement trees in this basic block */
        for (Statement* stmt : StatementList(block->FirstNonPhiDef()))
        {
            /* We walk the tree in the forwards direction (bottom up) */
            bool stmtHasArrLenCandidate = false;
            for (GenTree* tree = stmt->GetTreeList(); tree != nullptr; tree = tree->gtNext)
            {
                if (tree->OperIsCompare() && stmtHasArrLenCandidate)
                {
                    // Check if this compare is a function of (one of) the checked
                    // bound candidate(s); we may want to update its value number.
                    // if the array length gets CSEd
                    optCseUpdateCheckedBoundMap(tree);
                }

                // Don't allow CSE of constants if it is disabled
                //
                if (tree->IsIntegralConst())
                {
                    if (!enableConstCSE)
                    {
                        continue;
                    }
                }

                if (!optIsCSEcandidate(tree))
                {
                    continue;
                }

                if (ValueNumStore::isReservedVN(tree->GetVN(VNK_Liberal)))
                {
                    continue;
                }

                // We want to CSE simple constant leaf nodes, but we don't want to
                // CSE non-leaf trees that compute CSE constant values.
                // Instead we let the Value Number based Assertion Prop phase handle them.
                //
                // Here, unlike the rest of optCSE, we use the conservative value number
                // rather than the liberal one, since the conservative one
                // is what the Value Number based Assertion Prop will use
                // and the point is to avoid optimizing cases that it will
                // handle.
                //
                if (!tree->OperIsLeaf() && vnStore->IsVNConstant(vnStore->VNConservativeNormalValue(tree->gtVNPair)))
                {
                    continue;
                }

                /* Assign an index to this expression */

                unsigned CSEindex = optValnumCSE_Index(tree, stmt);

                if (CSEindex != 0)
                {
                    noway_assert(((unsigned)tree->gtCSEnum) == CSEindex);
                }

                if (IS_CSE_INDEX(CSEindex) && (tree->OperGet() == GT_ARR_LENGTH))
                {
                    stmtHasArrLenCandidate = true;
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

//------------------------------------------------------------------------
// optCseUpdateCheckedBoundMap: Check if this compare is a tractable function of
//                     a checked bound that is a CSE candidate, and insert
//                     an entry in the optCseCheckedBoundMap if so.  This facilitates
//                     subsequently updating the compare's value number if
//                     the bound gets CSEd.
//
// Arguments:
//    compare - The compare node to check

void Compiler::optCseUpdateCheckedBoundMap(GenTree* compare)
{
    assert(compare->OperIsCompare());

    ValueNum  compareVN = compare->gtVNPair.GetConservative();
    VNFuncApp cmpVNFuncApp;

    if (!vnStore->GetVNFunc(compareVN, &cmpVNFuncApp) || (cmpVNFuncApp.m_func != GetVNFuncForNode(compare)))
    {
        // Value numbering inferred this compare as something other
        // than its own operator; leave its value number alone.
        return;
    }

    // Now look for a checked bound feeding the compare
    ValueNumStore::CompareCheckedBoundArithInfo info;

    GenTree* boundParent = nullptr;

    if (vnStore->IsVNCompareCheckedBound(compareVN))
    {
        // Simple compare of an bound against something else.

        vnStore->GetCompareCheckedBound(compareVN, &info);
        boundParent = compare;
    }
    else if (vnStore->IsVNCompareCheckedBoundArith(compareVN))
    {
        // Compare of a bound +/- some offset to something else.

        GenTree* op1 = compare->gtGetOp1();
        GenTree* op2 = compare->gtGetOp2();

        vnStore->GetCompareCheckedBoundArithInfo(compareVN, &info);
        if (GetVNFuncForNode(op1) == (VNFunc)info.arrOper)
        {
            // The arithmetic node is the bound's parent.
            boundParent = op1;
        }
        else if (GetVNFuncForNode(op2) == (VNFunc)info.arrOper)
        {
            // The arithmetic node is the bound's parent.
            boundParent = op2;
        }
    }

    if (boundParent != nullptr)
    {
        GenTree* bound = nullptr;

        // Find which child of boundParent is the bound.  Abort if neither
        // conservative value number matches the one from the compare VN.

        GenTree* child1 = boundParent->gtGetOp1();
        if ((info.vnBound == child1->gtVNPair.GetConservative()) && IS_CSE_INDEX(child1->gtCSEnum))
        {
            bound = child1;
        }
        else
        {
            GenTree* child2 = boundParent->gtGetOp2();
            if ((info.vnBound == child2->gtVNPair.GetConservative()) && IS_CSE_INDEX(child2->gtCSEnum))
            {
                bound = child2;
            }
        }

        if (bound != nullptr)
        {
            // Found a checked bound feeding a compare that is a tractable function of it;
            // record this in the map so we can update the compare VN if the bound
            // node gets CSEd.

            if (optCseCheckedBoundMap == nullptr)
            {
                // Allocate map on first use.
                optCseCheckedBoundMap = new (getAllocator()) NodeToNodeMap(getAllocator());
            }

            optCseCheckedBoundMap->Set(bound, compare);
        }
    }
}

/*****************************************************************************
 *
 *  Compute each blocks bbCseGen
 *  This is the bitset that represents the CSEs that are generated within the block
 *  Also initialize bbCseIn, bbCseOut and bbCseGen sets for all blocks
 */
void Compiler::optValnumCSE_InitDataFlow()
{
    // BitVec trait information for computing CSE availability using the CSE_DataFlow algorithm.
    // Two bits are allocated per CSE candidate to compute CSE availability
    // plus an extra bit to handle the initial unvisited case.
    // (See CSE_DataFlow::EndMerge for an explaination of why this is necessary)
    //
    // The two bits per CSE candidate have the following meanings:
    //     11 - The CSE is available, and is also available when considering calls as killing availability.
    //     10 - The CSE is available, but is not available when considering calls as killing availability.
    //     00 - The CSE is not available
    //     01 - An illegal combination
    //
    const unsigned bitCount = (optCSECandidateCount * 2) + 1;

    // Init traits and cseCallKillsMask bitvectors.
    cseLivenessTraits = new (getAllocator()) BitVecTraits(bitCount, this);
    cseCallKillsMask  = BitVecOps::MakeEmpty(cseLivenessTraits);
    for (unsigned inx = 0; inx < optCSECandidateCount; inx++)
    {
        unsigned cseAvailBit = inx * 2;

        // a one preserves availability and a zero kills the availability
        // we generate this kind of bit pattern:  101010101010
        //
        BitVecOps::AddElemD(cseLivenessTraits, cseCallKillsMask, cseAvailBit);
    }

    for (BasicBlock* block = fgFirstBB; block; block = block->bbNext)
    {
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
            block->bbCseIn = BitVecOps::MakeEmpty(cseLivenessTraits);
        }
        else
        {
            /* Initialize to {ALL} prior to dataflow */
            block->bbCseIn = BitVecOps::MakeFull(cseLivenessTraits);
        }

        block->bbCseOut = BitVecOps::MakeFull(cseLivenessTraits);

        /* Initialize to {ZERO} prior to locating the CSE candidates */
        block->bbCseGen = BitVecOps::MakeEmpty(cseLivenessTraits);
    }

    // We walk the set of CSE candidates and set the bit corresponsing to the CSEindex
    // in the block's bbCseGen bitset
    //
    for (unsigned inx = 0; inx < optCSECandidateCount; inx++)
    {
        CSEdsc*      dsc      = optCSEtab[inx];
        unsigned     CSEindex = dsc->csdIndex;
        treeStmtLst* lst      = dsc->csdTreeList;
        noway_assert(lst);

        while (lst != nullptr)
        {
            BasicBlock* block                = lst->tslBlock;
            unsigned    CseAvailBit          = genCSEnum2bit(CSEindex) * 2;
            unsigned    cseAvailCrossCallBit = CseAvailBit + 1;

            // This CSE is generated in 'block', we always set the CseAvailBit
            // If this block does not contain a call, we also set cseAvailCrossCallBit
            //
            // If we have a call in this block then in the loop below we walk the trees
            // backwards to find any CSEs that are generated after the last call in the block.
            //
            BitVecOps::AddElemD(cseLivenessTraits, block->bbCseGen, CseAvailBit);
            if ((block->bbFlags & BBF_HAS_CALL) == 0)
            {
                BitVecOps::AddElemD(cseLivenessTraits, block->bbCseGen, cseAvailCrossCallBit);
            }
            lst = lst->tslNext;
        }
    }

    for (BasicBlock* block = fgFirstBB; block; block = block->bbNext)
    {
        // If the block doesn't contains a call then skip it...
        //
        if ((block->bbFlags & BBF_HAS_CALL) == 0)
        {
            continue;
        }

        // We only need to examine blocks that generate CSEs
        //
        if (BitVecOps::IsEmpty(cseLivenessTraits, block->bbCseGen))
        {
            continue;
        }

        // If the block contains a call and generates CSEs, we may need to update
        // the bbCseGen set as we may generate some CSEs after the last call in the block.
        //
        // We walk the statements in this basic block starting at the end and walking backwards,
        // until we reach the first call
        //
        Statement* stmt      = block->lastStmt();
        bool       foundCall = false;
        while (!foundCall)
        {
            // Also walk the tree in the backwards direction (bottom up)
            // looking for CSE's and updating block->bbCseGen
            // When we reach a call node, we can exit the for loop
            //
            for (GenTree* tree = stmt->GetRootNode(); tree != nullptr; tree = tree->gtPrev)
            {
                if (IS_CSE_INDEX(tree->gtCSEnum))
                {
                    unsigned CSEnum               = GET_CSE_INDEX(tree->gtCSEnum);
                    unsigned cseAvailCrossCallBit = (genCSEnum2bit(CSEnum) * 2) + 1;
                    BitVecOps::AddElemD(cseLivenessTraits, block->bbCseGen, cseAvailCrossCallBit);
                }
                if (tree->OperGet() == GT_CALL)
                {
                    // Any cse's that we haven't placed in the block->bbCseGen set
                    // aren't currently alive (using cseAvailCrossCallBit)
                    //
                    foundCall = true;
                    break;
                }
            }
            // The JIT can sometimes remove the only call in the block
            if (stmt == block->firstStmt())
            {
                break;
            }
            stmt = stmt->GetPrevStmt();
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
            if (block->bbCseGen != nullptr)
            {
                if (!headerPrinted)
                {
                    printf("\nBlocks that generate CSE def/uses\n");
                    headerPrinted = true;
                }
                printf(FMT_BB, block->bbNum);
                printf(" cseGen = %s\n", genES2str(cseLivenessTraits, block->bbCseGen));
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
    Compiler* m_comp;
    EXPSET_TP m_preMergeOut;

public:
    CSE_DataFlow(Compiler* pCompiler) : m_comp(pCompiler), m_preMergeOut(BitVecOps::UninitVal())
    {
    }

    // At the start of the merge function of the dataflow equations, initialize premerge state (to detect changes.)
    void StartMerge(BasicBlock* block)
    {
        // Record the initial value of block->bbCseOut in m_preMergeOut.
        // It is used in EndMerge() to control the termination of the DataFlow algorithm.
        // Note that the first time we visit a block, the value of bbCseOut is MakeFull()
        //
        BitVecOps::Assign(m_comp->cseLivenessTraits, m_preMergeOut, block->bbCseOut);

#ifdef DEBUG
        if (m_comp->verbose)
        {
            printf("StartMerge BB%02u\n", block->bbNum);
            printf("  :: cseOut    = %s\n", genES2str(m_comp->cseLivenessTraits, block->bbCseOut));
        }
#endif // DEBUG
    }

    // Merge: perform the merging of each of the predecessor's liveness values (since this is a forward analysis)
    void Merge(BasicBlock* block, BasicBlock* predBlock, flowList* preds)
    {
#ifdef DEBUG
        if (m_comp->verbose)
        {
            printf("Merge BB%02u and BB%02u\n", block->bbNum, predBlock->bbNum);
            printf("  :: cseIn     = %s\n", genES2str(m_comp->cseLivenessTraits, block->bbCseIn));
            printf("  :: cseOut    = %s\n", genES2str(m_comp->cseLivenessTraits, block->bbCseOut));
        }
#endif // DEBUG

        BitVecOps::IntersectionD(m_comp->cseLivenessTraits, block->bbCseIn, predBlock->bbCseOut);

#ifdef DEBUG
        if (m_comp->verbose)
        {
            printf("  => cseIn     = %s\n", genES2str(m_comp->cseLivenessTraits, block->bbCseIn));
        }
#endif // DEBUG
    }

    //------------------------------------------------------------------------
    // MergeHandler: Merge CSE values into the first exception handler/filter block.
    //
    // Arguments:
    //   block         - the block that is the start of a handler or filter;
    //   firstTryBlock - the first block of the try for "block" handler;
    //   lastTryBlock  - the last block of the try for "block" handler;.
    //
    // Notes:
    //   We can jump to the handler from any instruction in the try region.
    //   It means we can propagate only CSE that are valid for the whole try region.
    void MergeHandler(BasicBlock* block, BasicBlock* firstTryBlock, BasicBlock* lastTryBlock)
    {
        // TODO CQ: add CSE for handler blocks, CSE_INTO_HANDLERS should be defined.
    }

    // At the end of the merge store results of the dataflow equations, in a postmerge state.
    // We also handle the case where calls conditionally kill CSE availabilty.
    //
    bool EndMerge(BasicBlock* block)
    {
        // We can skip the calls kill step when our block doesn't have a callsite
        // or we don't have any available CSEs in our bbCseIn
        //
        if (((block->bbFlags & BBF_HAS_CALL) == 0) || BitVecOps::IsEmpty(m_comp->cseLivenessTraits, block->bbCseIn))
        {
            // No callsite in 'block' or 'block->bbCseIn was empty, so we can use bbCseIn directly
            //
            BitVecOps::DataFlowD(m_comp->cseLivenessTraits, block->bbCseOut, block->bbCseGen, block->bbCseIn);
        }
        else
        {
            // We will create a temporary BitVec to pass to DataFlowD()
            //
            EXPSET_TP cseIn_withCallsKill = BitVecOps::UninitVal();

            // cseIn_withCallsKill is set to (bbCseIn AND cseCallKillsMask)
            //
            BitVecOps::Assign(m_comp->cseLivenessTraits, cseIn_withCallsKill, block->bbCseIn);
            BitVecOps::IntersectionD(m_comp->cseLivenessTraits, cseIn_withCallsKill, m_comp->cseCallKillsMask);

            // Call DataFlowD with the modified BitVec: (bbCseIn AND cseCallKillsMask)
            //
            BitVecOps::DataFlowD(m_comp->cseLivenessTraits, block->bbCseOut, block->bbCseGen, cseIn_withCallsKill);
        }

        // The bool 'notDone' is our terminating condition.
        // If it is 'true' then the initial value of m_preMergeOut was different than the final value that
        // we computed for bbCseOut.  When it is true we will visit every the successor of 'block'
        //
        // This is also why we need to allocate an extra bit in our cseLivenessTrair BitVecs.
        // We always need to visit our successor blocks once, thus we require that that the first time
        // that we visit a block we have a bit set in m_preMergeOut that won't be set when we compute
        // the new value of bbCseOut.
        //
        bool notDone = !BitVecOps::Equal(m_comp->cseLivenessTraits, block->bbCseOut, m_preMergeOut);

#ifdef DEBUG
        if (m_comp->verbose)
        {
            printf("EndMerge BB%02u\n", block->bbNum);
            printf("  :: cseIn     = %s\n", genES2str(m_comp->cseLivenessTraits, block->bbCseIn));
            if (((block->bbFlags & BBF_HAS_CALL) != 0) &&
                !BitVecOps::IsEmpty(m_comp->cseLivenessTraits, block->bbCseIn))
            {
                printf("  -- cseKill   = %s\n", genES2str(m_comp->cseLivenessTraits, m_comp->cseCallKillsMask));
            }
            printf("  :: cseGen    = %s\n", genES2str(m_comp->cseLivenessTraits, block->bbCseGen));
            printf("  => cseOut    = %s\n", genES2str(m_comp->cseLivenessTraits, block->bbCseOut));
            printf("  != preMerge  = %s, => %s\n", genES2str(m_comp->cseLivenessTraits, m_preMergeOut),
                   notDone ? "true" : "false");
        }
#endif // DEBUG

        return notDone;
    }
};

/*****************************************************************************
 *
 *  Perform a DataFlow forward analysis using the block CSE bitsets:
 *    Inputs:
 *      bbCseGen  - Exact CSEs that are always generated within the block
 *      bbCseIn   - Maximal estimate of CSEs that are/could be available at input to the block
 *      bbCseOut  - Maximal estimate of CSEs that are/could be available at exit to the block
 *
 *    Outputs:
 *      bbCseIn   - Computed CSEs that are available at input to the block
 *      bbCseOut  - Computed CSEs that are available at exit to the block
 */

void Compiler::optValnumCSE_DataFlow()
{

#ifdef DEBUG
    if (verbose)
    {
        printf("\nPerforming DataFlow for ValnumCSE's\n");
    }
#endif // DEBUG

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
            printf(FMT_BB, block->bbNum);
            printf(" cseIn  = %s,", genES2str(cseLivenessTraits, block->bbCseIn));
            printf(" cseGen = %s,", genES2str(cseLivenessTraits, block->bbCseGen));
            printf(" cseOut = %s", genES2str(cseLivenessTraits, block->bbCseOut));
            printf("\n");
        }

        printf("\n");
    }
#endif // DEBUG
}

//---------------------------------------------------------------------------
// optValnumCSE_Availablity:
//
//     Using the information computed by CSE_DataFlow determine for each
//     CSE whether the CSE is a definition (if the CSE was not available)
//     or if the CSE is a use (if the CSE was previously made available)
//     The implementation iterates of all blocks setting 'available_cses'
//     to the CSEs that are available at input to the block.
//     When a CSE expression is encountered it is classified as either
//     as a definition (if the CSE is not in the 'available_cses' set) or
//     as a use (if the CSE is  in the 'available_cses' set).  If the CSE
//     is a definition then it is added to the 'available_cses' set.
//
//     This algorithm uncovers the defs and uses gradually and as it does
//     so it also builds the exception set that all defs make: 'defExcSetCurrent'
//     and the exception set that the uses we have seen depend upon: 'defExcSetPromise'
//
//     Typically expressions with the same normal ValueNum generate exactly the
//     same exception sets. There are two way that we can get different exception
//     sets with the same Normal value number.
//
//     1. We used an arithmetic identiity:
//        e.g. (p.a + q.b) * 0   :: The normal value for the expression is zero
//                                  and we have NullPtrExc(p) and NullPtrExc(q)
//        e.g. (p.a - p.a)       :: The normal value for the expression is zero
//                                  and we have NullPtrExc(p)
//     2. We stored an expression into a LclVar or into Memory and read it later
//        e.g. t = p.a;
//             e1 = (t + q.b)    :: e1 has one NullPtrExc and e2 has two.
//             e2 = (p.a + q.b)     but both compute the same normal value//
//        e.g. m.a = p.a;
//             e1 = (m.a + q.b)  :: e1 and e2 have different exception sets.
//             e2 = (p.a + q.b)     but both compute the same normal value
//
//
void Compiler::optValnumCSE_Availablity()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("Labeling the CSEs with Use/Def information\n");
    }
#endif
    EXPSET_TP available_cses = BitVecOps::MakeEmpty(cseLivenessTraits);

    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        // Make the block publicly available

        compCurBB = block;

        // Retrieve the available CSE's at the start of this block

        BitVecOps::Assign(cseLivenessTraits, available_cses, block->bbCseIn);

        // Walk the statement trees in this basic block

        for (Statement* stmt : StatementList(block->FirstNonPhiDef()))
        {
            // We walk the tree in the forwards direction (bottom up)

            for (GenTree* tree = stmt->GetTreeList(); tree != nullptr; tree = tree->gtNext)
            {
                bool isUse = false;
                bool isDef = false;

                if (IS_CSE_INDEX(tree->gtCSEnum))
                {
                    unsigned CSEnum               = GET_CSE_INDEX(tree->gtCSEnum);
                    unsigned CseAvailBit          = genCSEnum2bit(CSEnum) * 2;
                    unsigned cseAvailCrossCallBit = CseAvailBit + 1;
                    CSEdsc*  desc                 = optCSEfindDsc(CSEnum);
                    unsigned stmw                 = block->getBBWeight(this);

                    isUse = BitVecOps::IsMember(cseLivenessTraits, available_cses, CseAvailBit);
                    isDef = !isUse; // If is isn't a CSE use, it is a CSE def

                    // Is this a "use", that we haven't yet marked as live accross a call
                    // and it is not available when we have calls that kill CSE's (cseAvailCrossCallBit)
                    // if the above is true then we will mark this the CSE as live across a call
                    //
                    bool madeLiveAcrossCall = false;
                    if (isUse && !desc->csdLiveAcrossCall &&
                        !BitVecOps::IsMember(cseLivenessTraits, available_cses, cseAvailCrossCallBit))
                    {
                        desc->csdLiveAcrossCall = true;
                        madeLiveAcrossCall      = true;
                    }

#ifdef DEBUG
                    // If this is a CSE def (i.e. the CSE is not available here, since it is being defined), then the
                    // call-kill bit
                    // should also be zero since it is also not available across a call.
                    //
                    if (isDef)
                    {
                        assert(!BitVecOps::IsMember(cseLivenessTraits, available_cses, cseAvailCrossCallBit));
                    }

                    if (verbose)
                    {
                        printf("BB%02u ", block->bbNum);
                        printTreeID(tree);

                        printf(" %s of CSE #%02u [weight=%s]%s\n", isUse ? "Use" : "Def", CSEnum, refCntWtd2str(stmw),
                               madeLiveAcrossCall ? " *** Now Live Across Call ***" : "");
                    }
#endif // DEBUG

                    // Have we decided to abandon work on this CSE?
                    if (desc->defExcSetPromise == ValueNumStore::NoVN)
                    {
                        // This candidate had defs with differing liberal exc set VNs
                        // We have abandoned CSE promotion for this candidate

                        // Clear the CSE flag
                        tree->gtCSEnum = NO_CSE;

                        JITDUMP(" Abandoned - CSE candidate has defs with different exception sets!\n");
                        continue;
                    }

                    // Record the exception set for tree's liberal value number
                    //
                    ValueNum theLiberalExcSet = vnStore->VNExceptionSet(tree->gtVNPair.GetLiberal());

                    // Is this a CSE use or a def?

                    if (isDef)
                    {
                        // @ToDo - Remove this block as it no longer applies
                        if (tree->gtFlags & GTF_COLON_COND)
                        {
                            // We can't create CSE definitions inside QMARK-COLON trees
                            tree->gtCSEnum = NO_CSE;

                            JITDUMP(" NO_CSE - This CSE def occurs in a GTF_COLON_COND!\n");
                            continue;
                        }

                        // This is a CSE def

                        // Is defExcSetCurrent still set to the uninit marker value of VNForNull() ?
                        if (desc->defExcSetCurrent == vnStore->VNForNull())
                        {
                            // This is the first time visited, so record this defs exeception set
                            desc->defExcSetCurrent = theLiberalExcSet;
                        }

                        // Have we seen a CSE use and made a promise of an exception set?
                        //
                        if (desc->defExcSetPromise != vnStore->VNForEmptyExcSet())
                        {
                            // The exeception set held in desc->defExcSetPromise must be a subset of theLiberalExcSet
                            //
                            if (vnStore->VNExcIsSubset(theLiberalExcSet, desc->defExcSetPromise))
                            {
                                // This new def still satisfies any promise made to all the CSE uses that we have
                                // encountered
                                //

                                // no update is needed when these are the same VN
                                if (desc->defExcSetCurrent != theLiberalExcSet)
                                {
                                    // We will change the value of desc->defExcSetCurrent to be the intersection of
                                    // these two sets.
                                    // This is the set of exceptions that all CSE defs have (that we have visited so
                                    // far)
                                    //
                                    ValueNum intersectionExcSet =
                                        vnStore->VNExcSetIntersection(desc->defExcSetCurrent, theLiberalExcSet);
#ifdef DEBUG
                                    if (this->verbose)
                                    {
                                        VNFuncApp excSeq;

                                        vnStore->GetVNFunc(desc->defExcSetCurrent, &excSeq);
                                        printf(">>> defExcSetCurrent is ");
                                        vnStore->vnDumpExcSeq(this, &excSeq, true);
                                        printf("\n");

                                        vnStore->GetVNFunc(theLiberalExcSet, &excSeq);
                                        printf(">>> theLiberalExcSet is ");
                                        vnStore->vnDumpExcSeq(this, &excSeq, true);
                                        printf("\n");

                                        if (intersectionExcSet == vnStore->VNForEmptyExcSet())
                                        {
                                            printf(">>> the intersectionExcSet is the EmptyExcSet\n");
                                        }
                                        else
                                        {
                                            vnStore->GetVNFunc(intersectionExcSet, &excSeq);
                                            printf(">>> the intersectionExcSet is ");
                                            vnStore->vnDumpExcSeq(this, &excSeq, true);
                                            printf("\n");
                                        }
                                    }
#endif // DEBUG

                                    // Change the defExcSetCurrent to be a subset of its prior value
                                    //
                                    assert(vnStore->VNExcIsSubset(desc->defExcSetCurrent, intersectionExcSet));
                                    desc->defExcSetCurrent = intersectionExcSet;
                                }
                            }
                            else // This CSE def doesn't satisfy one of the exceptions already promised to a CSE use
                            {
                                // So, we will abandon all CSE promotions for this candidate
                                //
                                // We use the marker value of NoVN to indicate that we
                                // should abandon this CSE candidate
                                //
                                desc->defExcSetPromise = ValueNumStore::NoVN;
                                tree->gtCSEnum         = NO_CSE;

                                JITDUMP(" Abandon - CSE candidate has defs with exception sets that do not satisfy "
                                        "some CSE use\n");
                                continue;
                            }
                        }

                        // For shared const CSE we don't set/use the defConservNormVN
                        //
                        if (!Is_Shared_Const_CSE(desc->csdHashKey))
                        {
                            // Record or update the value of desc->defConservNormVN
                            //
                            ValueNum theConservNormVN = vnStore->VNConservativeNormalValue(tree->gtVNPair);

                            // Is defConservNormVN still set to the uninit marker value of VNForNull() ?
                            if (desc->defConservNormVN == vnStore->VNForNull())
                            {
                                // This is the first def that we have visited, set defConservNormVN
                                desc->defConservNormVN = theConservNormVN;
                            }
                            else
                            {
                                // Check to see if all defs have the same conservative normal VN
                                if (theConservNormVN != desc->defConservNormVN)
                                {
                                    // This candidate has defs with differing conservative normal VNs, mark it with NoVN
                                    desc->defConservNormVN = ValueNumStore::NoVN; // record the marker for differing VNs
                                }
                            }
                        }

                        // If we get here we have accepted this node as a valid CSE def

                        desc->csdDefCount += 1;
                        desc->csdDefWtCnt += stmw;

                        // Mark the node as a CSE definition

                        tree->gtCSEnum = TO_CSE_DEF(tree->gtCSEnum);

                        // This CSE becomes available after this def
                        BitVecOps::AddElemD(cseLivenessTraits, available_cses, CseAvailBit);
                        BitVecOps::AddElemD(cseLivenessTraits, available_cses, cseAvailCrossCallBit);
                    }
                    else // We are visiting a CSE use
                    {
                        assert(isUse);

                        // If the CSE use has no requirements for an exception set then we don't have to do anything
                        // here
                        //
                        if (theLiberalExcSet != vnStore->VNForEmptyExcSet())
                        {
                            // Are we visiting a use first, before visiting any defs of this CSE?
                            // This is an atypical case that can occur with a bottom tested loop.
                            //
                            // Is defExcSetCurrent still set to the uninit marker value of VNForNull() ?
                            if (desc->defExcSetCurrent == vnStore->VNForNull())
                            {
                                // Update defExcSetPromise, this is our required exception set for all CSE defs
                                // that we encounter later.
                                //
                                // We could see multiple uses before a def, so we require the Union of all exception
                                // sets
                                //
                                desc->defExcSetPromise =
                                    vnStore->VNExcSetUnion(desc->defExcSetPromise, theLiberalExcSet);
                            }
                            else // we have already seen a def for this CSE and defExcSetCurrent is setup
                            {
                                if (vnStore->VNExcIsSubset(desc->defExcSetCurrent, theLiberalExcSet))
                                {
                                    // The current set of exceptions produced by all CSE defs have (that we have
                                    // visited so far) meets our requirement
                                    //
                                    // Add any exception items to the defExcSetPromise set
                                    //
                                    desc->defExcSetPromise =
                                        vnStore->VNExcSetUnion(desc->defExcSetPromise, theLiberalExcSet);
                                }
                            }

                            // At this point defExcSetPromise contains all of the exception items that we can promise
                            // here.
                            //
                            if (!vnStore->VNExcIsSubset(desc->defExcSetPromise, theLiberalExcSet))
                            {
                                // We can't safely make this into a CSE use, because this
                                // CSE use has an exeception set item that is not promised
                                // by all of our CSE defs.
                                //
                                // We will omit this CSE use from the graph and proceed,
                                // the other uses and defs can still participate in the CSE optimization.

                                // So this can't be a CSE use
                                tree->gtCSEnum = NO_CSE;

                                JITDUMP(" NO_CSE - This use has an exception set item that isn't contained in the "
                                        "defs!\n");
                                continue;
                            }
                        }

                        // When we get here we have accepted this node as a valid CSE use

                        desc->csdUseCount += 1;
                        desc->csdUseWtCnt += stmw;
                    }
                }

                // In order to determine if a CSE is live across a call, we model availablity using two bits and
                // kill all of the cseAvailCrossCallBit for each CSE whenever we see a GT_CALL (unless the call
                // generates A cse)
                //
                if (tree->OperGet() == GT_CALL)
                {
                    // Check for the common case of an already empty available_cses set
                    // and thus nothing needs to be killed
                    //
                    if (!(BitVecOps::IsEmpty(cseLivenessTraits, available_cses)))
                    {
                        if (isUse)
                        {
                            // For a CSE Use we will assume that the CSE logic will replace it with a CSE LclVar and
                            // not make the call so kill nothing
                        }
                        else
                        {
                            // partially kill any cse's that are currently alive (using the cseCallKillsMask set)
                            //
                            BitVecOps::IntersectionD(cseLivenessTraits, available_cses, cseCallKillsMask);

                            if (isDef)
                            {
                                // We can have a GT_CALL that produces a CSE,
                                // (i.e. HELPER.CORINFO_HELP_GETSHARED_*STATIC_BASE or
                                // CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE)
                                //
                                // The CSE becomes available after the call, so set the cseAvailCrossCallBit bit in
                                // available_cses
                                //
                                unsigned CSEnum               = GET_CSE_INDEX(tree->gtCSEnum);
                                unsigned cseAvailCrossCallBit = (genCSEnum2bit(CSEnum) * 2) + 1;

                                BitVecOps::AddElemD(cseLivenessTraits, available_cses, cseAvailCrossCallBit);
                            }
                        }
                    }
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
    unsigned               enregCount; // count of the number of predicted enregistered variables
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

        unsigned   frameSize        = 0;
        unsigned   regAvailEstimate = ((CNT_CALLEE_ENREG * 3) + (CNT_CALLEE_TRASH * 2) + 1);
        unsigned   lclNum;
        LclVarDsc* varDsc;

        for (lclNum = 0, varDsc = m_pCompiler->lvaTable; lclNum < m_pCompiler->lvaCount; lclNum++, varDsc++)
        {
            // Locals with no references don't use any local stack frame slots
            if (varDsc->lvRefCnt() == 0)
            {
                continue;
            }

            // Incoming stack arguments don't use any local stack frame slots
            if (varDsc->lvIsParam && !varDsc->lvIsRegArg)
            {
                continue;
            }

#if FEATURE_FIXED_OUT_ARGS
            // Skip the OutgoingArgArea in computing frame size, since
            // its size is not yet known and it doesn't affect local
            // offsets from the frame pointer (though it may affect
            // them from the stack pointer).
            noway_assert(m_pCompiler->lvaOutgoingArgSpaceVar != BAD_VAR_NUM);
            if (lclNum == m_pCompiler->lvaOutgoingArgSpaceVar)
            {
                continue;
            }
#endif // FEATURE_FIXED_OUT_ARGS

            bool onStack = (regAvailEstimate == 0); // true when it is likely that this LclVar will have a stack home

            // Some LclVars always have stack homes
            if ((varDsc->lvDoNotEnregister) || (varDsc->lvType == TYP_LCLBLK))
            {
                onStack = true;
            }

#ifdef TARGET_X86
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
                //
                if (varDsc->lvRefCnt() <= 2)
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
#ifdef TARGET_XARCH
            if (frameSize > 0x080)
            {
                // We likely have a large stack frame.
                //
                // On XARCH stack frame displacements can either use a 1-byte or a 4-byte displacement
                // with a large franme we will need to use some 4-byte displacements.
                //
                largeFrame = true;
                break; // early out,  we don't need to keep increasing frameSize
            }
#elif defined(TARGET_ARM)
            if (frameSize > 0x0400)
            {
                // We likely have a large stack frame.
                //
                // Thus we might need to use large displacements when loading or storing
                // to CSE LclVars that are not enregistered
                // On ARM32 this means using rsGetRsvdReg() to hold the large displacement
                largeFrame = true;
            }
            if (frameSize > 0x10000)
            {
                hugeFrame = true;
                break; // early out,  we don't need to keep increasing frameSize
            }
#elif defined(TARGET_ARM64)
            if (frameSize > 0x1000)
            {
                // We likely have a large stack frame.
                //
                // Thus we might need to use large displacements when loading or storing
                // to CSE LclVars that are not enregistered
                // On ARM64 this means using rsGetRsvdReg() to hold the large displacement
                //
                largeFrame = true;
                break; // early out,  we don't need to keep increasing frameSize
            }
#endif
        }

        // Iterate over the sorted list of tracked local variables
        // these are the register candidates for LSRA
        // We normally vist the LclVar in order of their weighted ref counts
        // and our hueristic assumes that the highest weighted ref count
        // LclVars will be enregistered and that the lowest weighted ref count
        // are likely be allocated in the stack frame.
        // The value of enregCount is incremented when we visit a LclVar
        // that can be enregistered.
        //
        for (unsigned trackedIndex = 0; trackedIndex < m_pCompiler->lvaTrackedCount; trackedIndex++)
        {
            LclVarDsc* varDsc = m_pCompiler->lvaGetDescByTrackedIndex(trackedIndex);
            var_types  varTyp = varDsc->TypeGet();

            // Locals with no references aren't enregistered
            if (varDsc->lvRefCnt() == 0)
            {
                continue;
            }

            // Some LclVars always have stack homes
            if ((varDsc->lvDoNotEnregister) || (varDsc->lvType == TYP_LCLBLK))
            {
                continue;
            }

            // The enregCount only tracks the uses of integer registers
            //
            // We could track floating point register usage seperately
            // but it isn't worth the additional complexity as floating point CSEs
            // are rare and we typically have plenty of floating point register available.
            //
            if (!varTypeIsFloating(varTyp))
            {
                enregCount++; // The primitive types, including TYP_SIMD types use one register

#ifndef TARGET_64BIT
                if (varTyp == TYP_LONG)
                {
                    enregCount++; // on 32-bit targets longs use two registers
                }
#endif
            }

            // Set the cut off values to use for deciding when we want to use aggressive, moderate or conservative
            //
            // The value of aggressiveRefCnt and moderateRefCnt start off as zero and
            // when enregCount reached a certain value we assign the current LclVar
            // (weighted) ref count to aggressiveRefCnt or moderateRefCnt.
            //
            const unsigned aggressiveEnregNum = (CNT_CALLEE_ENREG * 3 / 2);
            const unsigned moderateEnregNum   = ((CNT_CALLEE_ENREG * 3) + (CNT_CALLEE_TRASH * 2));
            //
            // On Windows x64 this yeilds:
            // aggressiveEnregNum == 12 and moderateEnregNum == 38
            // Thus we will typically set the cutoff values for
            //   aggressiveRefCnt based upon the weight of T13 (the 13th tracked LclVar)
            //   moderateRefCnt based upon the weight of T39 (the 39th tracked LclVar)
            //
            // For other architecture and platforms these values dynamically change
            // based upon the number of callee saved and callee scratch registers.
            //
            if ((aggressiveRefCnt == 0) && (enregCount > aggressiveEnregNum))
            {
                if (CodeOptKind() == Compiler::SMALL_CODE)
                {
                    aggressiveRefCnt = varDsc->lvRefCnt();
                }
                else
                {
                    aggressiveRefCnt = varDsc->lvRefCntWtd();
                }
                aggressiveRefCnt += BB_UNITY_WEIGHT;
            }
            if ((moderateRefCnt == 0) && (enregCount > ((CNT_CALLEE_ENREG * 3) + (CNT_CALLEE_TRASH * 2))))
            {
                if (CodeOptKind() == Compiler::SMALL_CODE)
                {
                    moderateRefCnt = varDsc->lvRefCnt();
                }
                else
                {
                    moderateRefCnt = varDsc->lvRefCntWtd();
                }
                moderateRefCnt += (BB_UNITY_WEIGHT / 2);
            }
        }

        // The minumum value that we want to use for aggressiveRefCnt is BB_UNITY_WEIGHT * 2
        // so increase it when we are below that value
        //
        aggressiveRefCnt = max(BB_UNITY_WEIGHT * 2, aggressiveRefCnt);

        // The minumum value that we want to use for moderateRefCnt is BB_UNITY_WEIGHT
        // so increase it when we are below that value
        //
        moderateRefCnt = max(BB_UNITY_WEIGHT, moderateRefCnt);

#ifdef DEBUG
        if (m_pCompiler->verbose)
        {
            printf("\n");
            printf("Aggressive CSE Promotion cutoff is %u\n", aggressiveRefCnt);
            printf("Moderate CSE Promotion cutoff is %u\n", moderateRefCnt);
            printf("enregCount is %u\n", enregCount);
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
                GenTree*          expr = dsc->csdTree;

                unsigned def;
                unsigned use;
                unsigned cost;

                if (CodeOptKind() == Compiler::SMALL_CODE)
                {
                    def  = dsc->csdDefCount; // def count
                    use  = dsc->csdUseCount; // use count (excluding the implicit uses at defs)
                    cost = dsc->csdTree->GetCostSz();
                }
                else
                {
                    def  = dsc->csdDefWtCnt; // weighted def count
                    use  = dsc->csdUseWtCnt; // weighted use count (excluding the implicit uses at defs)
                    cost = dsc->csdTree->GetCostEx();
                }

                if (!Compiler::Is_Shared_Const_CSE(dsc->csdHashKey))
                {
                    printf("CSE #%02u, {$%-3x, $%-3x} useCnt=%d: [def=%3u, use=%3u, cost=%3u%s]\n        :: ",
                           dsc->csdIndex, dsc->csdHashKey, dsc->defExcSetPromise, dsc->csdUseCount, def, use, cost,
                           dsc->csdLiveAcrossCall ? ", call" : "      ");
                }
                else
                {
                    size_t kVal = Compiler::Decode_Shared_Const_CSE_Value(dsc->csdHashKey);
                    printf("CSE #%02u, {K_%p} useCnt=%d: [def=%3u, use=%3u, cost=%3u%s]\n        :: ", dsc->csdIndex,
                           dspPtr(kVal), dsc->csdUseCount, def, use, cost,
                           dsc->csdLiveAcrossCall ? ", call" : "      ");
                }

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

        // When this Candidate is successfully promoted to a CSE we record
        // the following information about what category was used when promoting it.
        //
        //  We will set m_Aggressive:
        //    When we believe that the CSE very valuable in terms of weighted ref counts,
        //    such that it would always be enregistered by the register allocator.
        //
        //  We will set m_Moderate:
        //    When we believe that the CSE is moderately valuable in terms of weighted ref counts,
        //    such that it is more likely than not to be enregistered by the register allocator
        //
        //  We will set m_Conservative:
        //    When we didn't set m_Aggressive or  m_Moderate.
        //    Such candidates typically are expensive to compute and thus are
        //    always profitable to promote even when they aren't enregistered.
        //
        //  We will set  m_StressCSE:
        //    When the candidate is only being promoted because of a Stress mode.
        //
        bool m_Aggressive;
        bool m_Moderate;
        bool m_Conservative;
        bool m_StressCSE;

    public:
        CSE_Candidate(CSE_Heuristic* context, Compiler::CSEdsc* cseDsc)
            : m_context(context)
            , m_CseDsc(cseDsc)
            , m_cseIndex(m_CseDsc->csdIndex)
            , m_defCount(0)
            , m_useCount(0)
            , m_Cost(0)
            , m_Size(0)
            , m_Aggressive(false)
            , m_Moderate(false)
            , m_Conservative(false)
            , m_StressCSE(false)
        {
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
        GenTree* Expr()
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
            return m_CseDsc->csdLiveAcrossCall;
        }

        void SetAggressive()
        {
            m_Aggressive = true;
        }

        bool IsAggressive()
        {
            return m_Aggressive;
        }

        void SetModerate()
        {
            m_Moderate = true;
        }

        bool IsModerate()
        {
            return m_Moderate;
        }

        void SetConservative()
        {
            m_Conservative = true;
        }

        bool IsConservative()
        {
            return m_Conservative;
        }

        void SetStressCSE()
        {
            m_StressCSE = true;
        }

        bool IsStressCSE()
        {
            return m_StressCSE;
        }

        void InitializeCounts()
        {
            m_Size = Expr()->GetCostSz(); // always the GetCostSz()
            if (m_context->CodeOptKind() == Compiler::SMALL_CODE)
            {
                m_Cost     = m_Size;                // the estimated code size
                m_defCount = m_CseDsc->csdDefCount; // def count
                m_useCount = m_CseDsc->csdUseCount; // use count (excluding the implicit uses at defs)
            }
            else
            {
                m_Cost     = Expr()->GetCostEx();   // the estimated execution cost
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

    // Given a CSE candidate decide whether it passes or fails the profitability heuristic
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
            if (stressResult > 0)
            {
                candidate->SetStressCSE();
                return true;
            }
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
        // Each CSE Def will contain two Refs and each CSE Use will have one Ref of this new LclVar
        unsigned cseRefCnt = (candidate->DefCount() * 2) + candidate->UseCount();

        bool      canEnregister = true;
        unsigned  slotCount     = 1;
        var_types cseLclVarTyp  = genActualType(candidate->Expr()->TypeGet());
        if (candidate->Expr()->TypeGet() == TYP_STRUCT)
        {
            // This is a non-enregisterable struct.
            canEnregister                  = false;
            GenTree*             value     = candidate->Expr();
            CORINFO_CLASS_HANDLE structHnd = m_pCompiler->gtGetStructHandleIfPresent(candidate->Expr());
            if (structHnd == NO_CLASS_HANDLE)
            {
                JITDUMP("Can't determine the struct size, so we can't consider it for CSE promotion\n");
                return false; //  Do not make this a CSE
            }

            unsigned size = m_pCompiler->info.compCompHnd->getClassSize(structHnd);
            // Note that the slotCount is used to estimate the reference cost, but it may overestimate this
            // because it doesn't take into account that we might use a vector register for struct copies.
            slotCount = (size + TARGET_POINTER_SIZE - 1) / TARGET_POINTER_SIZE;
        }

        if (CodeOptKind() == Compiler::SMALL_CODE)
        {
            // Note that when optimizing for SMALL_CODE we set the cse_def_cost/cse_use_cost based
            // upon the code size and we use unweighted ref counts instead of weighted ref counts.
            // Also note that optimizing for SMALL_CODE is rare, we typically only optimize this way
            // for class constructors, because we know that they will only run once.
            //
            if (cseRefCnt >= aggressiveRefCnt)
            {
                // Record that we are choosing to use the aggressive promotion rules
                //
                candidate->SetAggressive();
#ifdef DEBUG
                if (m_pCompiler->verbose)
                {
                    printf("Aggressive CSE Promotion (%u >= %u)\n", cseRefCnt, aggressiveRefCnt);
                }
#endif
                // With aggressive promotion we expect that the candidate will be enregistered
                // so we set the use and def costs to their miniumum values
                //
                cse_def_cost = 1;
                cse_use_cost = 1;

                // Check if this candidate is likely to live on the stack
                //
                if (candidate->LiveAcrossCall() || !canEnregister)
                {
                    // Increase the costs when we have a large or huge frame
                    //
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
            else // not aggressiveRefCnt
            {
                // Record that we are choosing to use the conservative promotion rules
                //
                candidate->SetConservative();
                if (largeFrame)
                {
#ifdef DEBUG
                    if (m_pCompiler->verbose)
                    {
                        printf("Codesize CSE Promotion (%s frame)\n", hugeFrame ? "huge" : "large");
                    }
#endif
#ifdef TARGET_XARCH
                    /* The following formula is good choice when optimizing CSE for SMALL_CODE */
                    cse_def_cost = 6; // mov [EBP-0x00001FC],reg
                    cse_use_cost = 5; //     [EBP-0x00001FC]
#else                                 // TARGET_ARM
                    if (hugeFrame)
                    {
                        cse_def_cost = 10 + 2; // movw/movt r10 and str reg,[sp+r10]
                        cse_use_cost = 10 + 2;
                    }
                    else
                    {
                        cse_def_cost = 6 + 2; // movw r10 and str reg,[sp+r10]
                        cse_use_cost = 6 + 2;
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
#ifdef TARGET_XARCH
                    /* The following formula is good choice when optimizing CSE for SMALL_CODE */
                    cse_def_cost = 3; // mov [EBP-1C],reg
                    cse_use_cost = 2; //     [EBP-1C]

#else // TARGET_ARM

                    cse_def_cost = 2; // str reg,[sp+0x9c]
                    cse_use_cost = 2; // ldr reg,[sp+0x9c]
#endif
                }
            }
#ifdef TARGET_AMD64
            if (varTypeIsFloating(candidate->Expr()->TypeGet()))
            {
                // floating point loads/store encode larger
                cse_def_cost += 2;
                cse_use_cost += 1;
            }
#endif // TARGET_AMD64
        }
        else // not SMALL_CODE ...
        {
            // Note that when optimizing for BLENDED_CODE or FAST_CODE we set cse_def_cost/cse_use_cost
            // based upon the execution costs of the code and we use weighted ref counts.
            //
            if ((cseRefCnt >= aggressiveRefCnt) && canEnregister)
            {
                // Record that we are choosing to use the aggressive promotion rules
                //
                candidate->SetAggressive();
#ifdef DEBUG
                if (m_pCompiler->verbose)
                {
                    printf("Aggressive CSE Promotion (%u >= %u)\n", cseRefCnt, aggressiveRefCnt);
                }
#endif
                // With aggressive promotion we expect that the candidate will be enregistered
                // so we set the use and def costs to their miniumum values
                //
                cse_def_cost = 1;
                cse_use_cost = 1;
            }
            else if (cseRefCnt >= moderateRefCnt)
            {
                // Record that we are choosing to use the moderate promotion rules
                //
                candidate->SetModerate();
                if (!candidate->LiveAcrossCall() && canEnregister)
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
                else // candidate is live across call or not enregisterable.
                {
#ifdef DEBUG
                    if (m_pCompiler->verbose)
                    {
                        printf("Moderate CSE Promotion (%s) (%u >= %u)\n",
                               candidate->LiveAcrossCall() ? "CSE is live across a call" : "not enregisterable",
                               cseRefCnt, moderateRefCnt);
                    }
#endif
                    cse_def_cost = 2;
                    if (canEnregister)
                    {
                        if (enregCount < (CNT_CALLEE_ENREG * 3 / 2))
                        {
                            cse_use_cost = 1;
                        }
                        else
                        {
                            cse_use_cost = 2;
                        }
                    }
                    else
                    {
                        cse_use_cost = 3;
                    }
                }
            }
            else // Conservative CSE promotion
            {
                // Record that we are choosing to use the conservative promotion rules
                //
                candidate->SetConservative();
                if (!candidate->LiveAcrossCall() && canEnregister)
                {
#ifdef DEBUG
                    if (m_pCompiler->verbose)
                    {
                        printf("Conservative CSE Promotion (%s) (%u < %u)\n",
                               candidate->LiveAcrossCall() ? "CSE is live across a call" : "not enregisterable",
                               cseRefCnt, moderateRefCnt);
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
                    cse_def_cost = 2;
                    cse_use_cost = 3;
                }

                // If we have maxed out lvaTrackedCount then this CSE may end up as an untracked variable
                if (m_pCompiler->lvaTrackedCount == lclMAX_TRACKED)
                {
                    cse_def_cost += 1;
                    cse_use_cost += 1;
                }
            }
        }

        if (slotCount > 1)
        {
            cse_def_cost *= slotCount;
            cse_use_cost *= slotCount;
        }

        // If this CSE is live across a call then we may need to spill an additional caller save register
        //
        if (candidate->LiveAcrossCall())
        {
            // If we don't have a lot of variables to enregister or we have a floating point type
            // then we will likely need to spill an additional caller save register.
            //
            if ((enregCount < (CNT_CALLEE_ENREG * 3 / 2)) || varTypeIsFloating(candidate->Expr()->TypeGet()))
            {
                // Extra cost in case we have to spill/restore a caller saved register
                extra_yes_cost = BB_UNITY_WEIGHT;

                if (cseRefCnt < moderateRefCnt) // If Conservative CSE promotion
                {
                    extra_yes_cost *= 2; // full cost if we are being Conservative
                }
            }

#ifdef FEATURE_SIMD
            // SIMD types may cause a SIMD register to be spilled/restored in the prolog and epilog.
            //
            if (varTypeIsSIMD(candidate->Expr()->TypeGet()))
            {
                // We don't have complete information about when these extra spilled/restore will be needed.
                // Instead we are conservative and assume that each SIMD CSE that is live across a call
                // will cause an additional spill/restore in the prolog and epilog.
                //
                int spillSimdRegInProlog = 1;

                // If we have a SIMD32 that is live across a call we have even higher spill costs
                //
                if (candidate->Expr()->TypeGet() == TYP_SIMD32)
                {
                    // Additionally for a simd32 CSE candidate we assume that and second spilled/restore will be needed.
                    // (to hold the upper half of the simd32 register that isn't preserved across the call)
                    //
                    spillSimdRegInProlog++;

                    // We also increase the CSE use cost here to because we may have to generate instructions
                    // to move the upper half of the simd32 before and after a call.
                    //
                    cse_use_cost += 2;
                }

                extra_yes_cost = (BB_UNITY_WEIGHT * spillSimdRegInProlog) * 3;
            }
#endif // FEATURE_SIMD
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

        no_cse_cost += extra_no_cost;
        yes_cse_cost += extra_yes_cost;

#ifdef DEBUG
        if (m_pCompiler->verbose)
        {
            printf("cseRefCnt=%d, aggressiveRefCnt=%d, moderateRefCnt=%d\n", cseRefCnt, aggressiveRefCnt,
                   moderateRefCnt);
            printf("defCnt=%d, useCnt=%d, cost=%d, size=%d%s\n", candidate->DefCount(), candidate->UseCount(),
                   candidate->Cost(), candidate->Size(), candidate->LiveAcrossCall() ? ", LiveAcrossCall" : "");
            printf("def_cost=%d, use_cost=%d, extra_no_cost=%d, extra_yes_cost=%d\n", cse_def_cost, cse_use_cost,
                   extra_no_cost, extra_yes_cost);

            printf("CSE cost savings check (%u >= %u) %s\n", no_cse_cost, yes_cse_cost,
                   (no_cse_cost >= yes_cse_cost) ? "passes" : "fails");
        }
#endif // DEBUG

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

    // IsCompatibleType() takes two var_types and returns true if they
    // are compatible types for CSE substitution
    //
    bool IsCompatibleType(var_types cseLclVarTyp, var_types expTyp)
    {
        // Exact type match is the expected case
        if (cseLclVarTyp == expTyp)
        {
            return true;
        }

        // We also allow TYP_BYREF and TYP_I_IMPL as compatible types
        //
        if ((cseLclVarTyp == TYP_BYREF) && (expTyp == TYP_I_IMPL))
        {
            return true;
        }
        if ((cseLclVarTyp == TYP_I_IMPL) && (expTyp == TYP_BYREF))
        {
            return true;
        }

        // Otherwise we have incompatible types
        return false;
    }

    // PerformCSE() takes a successful candidate and performs  the appropriate replacements:
    //
    // It will replace all of the CSE defs with assignments to a new "cse0" LclVar
    // and will replace all of the CSE uses with reads of the "cse0" LclVar
    //
    // It will also put cse0 into SSA if there is just one def.
    void PerformCSE(CSE_Candidate* successfulCandidate)
    {
        unsigned cseRefCnt = (successfulCandidate->DefCount() * 2) + successfulCandidate->UseCount();

        if (successfulCandidate->LiveAcrossCall() != 0)
        {
            // As we introduce new LclVars for these CSE we slightly
            // increase the cutoffs for aggressive and moderate CSE's
            //
            int incr = BB_UNITY_WEIGHT;

            if (cseRefCnt > aggressiveRefCnt)
            {
                aggressiveRefCnt += incr;
            }

            if (cseRefCnt > moderateRefCnt)
            {
                moderateRefCnt += (incr / 2);
            }
        }

#ifdef DEBUG
        // Setup the message arg for lvaGrabTemp()
        //
        const char* grabTempMessage = "CSE - unknown";

        if (successfulCandidate->IsAggressive())
        {
            grabTempMessage = "CSE - aggressive";
        }
        else if (successfulCandidate->IsModerate())
        {
            grabTempMessage = "CSE - moderate";
        }
        else if (successfulCandidate->IsConservative())
        {
            grabTempMessage = "CSE - conservative";
        }
        else if (successfulCandidate->IsStressCSE())
        {
            grabTempMessage = "CSE - stress mode";
        }
#endif // DEBUG

        /* Introduce a new temp for the CSE */

        // we will create a  long lifetime temp for the new CSE LclVar
        unsigned  cseLclVarNum = m_pCompiler->lvaGrabTemp(false DEBUGARG(grabTempMessage));
        var_types cseLclVarTyp = genActualType(successfulCandidate->Expr()->TypeGet());
        if (varTypeIsStruct(cseLclVarTyp))
        {
            // Retrieve the struct handle that we recorded while bulding the list of CSE candidates.
            // If all occurances were in GT_IND nodes it could still be NO_CLASS_HANDLE
            //
            CORINFO_CLASS_HANDLE structHnd = successfulCandidate->CseDsc()->csdStructHnd;
            if (structHnd == NO_CLASS_HANDLE)
            {
                assert(varTypeIsSIMD(cseLclVarTyp));
                // We are not setting it for `SIMD* indir` during the first path
                // because it is not precise, see `optValnumCSE_Index`.
                structHnd = m_pCompiler->gtGetStructHandle(successfulCandidate->CseDsc()->csdTree);
            }
            assert(structHnd != NO_CLASS_HANDLE);
            m_pCompiler->lvaSetStruct(cseLclVarNum, structHnd, false);
        }
        m_pCompiler->lvaTable[cseLclVarNum].lvType  = cseLclVarTyp;
        m_pCompiler->lvaTable[cseLclVarNum].lvIsCSE = true;

        // Record that we created a new LclVar for use as a CSE temp
        m_addCSEcount++;
        m_pCompiler->optCSEcount++;

        //  Walk all references to this CSE, adding an assignment
        //  to the CSE temp to all defs and changing all refs to
        //  a simple use of the CSE temp.
        //
        //  Later we will unmark any nested CSE's for the CSE uses.
        //
        Compiler::CSEdsc* dsc = successfulCandidate->CseDsc();

        // If there's just a single def for the CSE, we'll put this
        // CSE into SSA form on the fly. We won't need any PHIs.
        unsigned cseSsaNum = SsaConfig::RESERVED_SSA_NUM;

        if (dsc->csdDefCount == 1)
        {
            JITDUMP("CSE #%02u is single-def, so associated CSE temp V%02u will be in SSA\n", dsc->csdIndex,
                    cseLclVarNum);
            m_pCompiler->lvaTable[cseLclVarNum].lvInSsa = true;

            // Allocate the ssa num
            CompAllocator allocator = m_pCompiler->getAllocator(CMK_SSA);
            cseSsaNum               = m_pCompiler->lvaTable[cseLclVarNum].lvPerSsaData.AllocSsaNum(allocator);
        }

        // Verify that all of the ValueNumbers in this list are correct as
        // Morph will change them when it performs a mutating operation.
        //
        bool                   setRefCnt      = true;
        bool                   allSame        = true;
        bool                   isSharedConst  = Compiler::Is_Shared_Const_CSE(dsc->csdHashKey);
        ValueNum               bestVN         = ValueNumStore::NoVN;
        bool                   bestIsDef      = false;
        ssize_t                bestConstValue = 0;
        Compiler::treeStmtLst* lst            = dsc->csdTreeList;

        while (lst != nullptr)
        {
            // Ignore this node if the gtCSEnum value has been cleared
            if (IS_CSE_INDEX(lst->tslTree->gtCSEnum))
            {
                // We used the liberal Value numbers when building the set of CSE
                ValueNum currVN = m_pCompiler->vnStore->VNLiberalNormalValue(lst->tslTree->gtVNPair);
                assert(currVN != ValueNumStore::NoVN);
                ssize_t curConstValue = isSharedConst ? m_pCompiler->vnStore->CoercedConstantValue<ssize_t>(currVN) : 0;

                GenTree* exp   = lst->tslTree;
                bool     isDef = IS_CSE_DEF(exp->gtCSEnum);

                if (bestVN == ValueNumStore::NoVN)
                {
                    // first entry
                    // set bestVN
                    bestVN = currVN;

                    if (isSharedConst)
                    {
                        // set bestConstValue and bestIsDef
                        bestConstValue = curConstValue;
                        bestIsDef      = isDef;
                    }
                }
                else if (currVN != bestVN)
                {
                    assert(isSharedConst); // Must be true when we have differing VNs

                    // subsequent entry
                    // clear allSame and check for a lower constant
                    allSame = false;

                    ssize_t diff = curConstValue - bestConstValue;

                    // The ARM64 ldr addressing modes allow for a subtraction of up to 255
                    // so we will allow the diff to be up to -255 before replacing a CSE def
                    // This will minimize the number of extra subtract instructions.
                    //
                    if ((bestIsDef && (diff < -255)) || (!bestIsDef && (diff < 0)))
                    {
                        // set new bestVN, bestConstValue and bestIsDef
                        bestVN         = currVN;
                        bestConstValue = curConstValue;
                        bestIsDef      = isDef;
                    }
                }

                BasicBlock*          blk       = lst->tslBlock;
                BasicBlock::weight_t curWeight = blk->getBBWeight(m_pCompiler);

                if (setRefCnt)
                {
                    m_pCompiler->lvaTable[cseLclVarNum].setLvRefCnt(1);
                    m_pCompiler->lvaTable[cseLclVarNum].setLvRefCntWtd(curWeight);
                    setRefCnt = false;
                }
                else
                {
                    m_pCompiler->lvaTable[cseLclVarNum].incRefCnts(curWeight, m_pCompiler);
                }

                // A CSE Def references the LclVar twice
                //
                if (isDef)
                {
                    m_pCompiler->lvaTable[cseLclVarNum].incRefCnts(curWeight, m_pCompiler);
                }
            }
            lst = lst->tslNext;
        }

        dsc->csdConstDefValue = bestConstValue;
        dsc->csdConstDefVN    = bestVN;

#ifdef DEBUG
        if (m_pCompiler->verbose)
        {
            if (!allSame)
            {
                if (isSharedConst)
                {
                    printf("\nWe have shared Const CSE's and selected " FMT_VN " with a value of 0x%p as the base.\n",
                           dsc->csdConstDefVN, dspPtr(dsc->csdConstDefValue));
                }
                else // !isSharedConst
                {
                    lst                = dsc->csdTreeList;
                    GenTree* firstTree = lst->tslTree;
                    printf("In %s, CSE (oper = %s, type = %s) has differing VNs: ", m_pCompiler->info.compFullName,
                           GenTree::OpName(firstTree->OperGet()), varTypeName(firstTree->TypeGet()));
                    while (lst != nullptr)
                    {
                        if (IS_CSE_INDEX(lst->tslTree->gtCSEnum))
                        {
                            ValueNum currVN = m_pCompiler->vnStore->VNLiberalNormalValue(lst->tslTree->gtVNPair);
                            printf("0x%x(%s " FMT_VN ") ", lst->tslTree,
                                   IS_CSE_USE(lst->tslTree->gtCSEnum) ? "use" : "def", currVN);
                        }
                        lst = lst->tslNext;
                    }
                    printf("\n");
                }
            }
        }
#endif // DEBUG

        // Setup 'lst' to point at the start of this candidate list
        lst = dsc->csdTreeList;
        noway_assert(lst);

        do
        {
            /* Process the next node in the list */
            GenTree*    exp  = lst->tslTree;
            Statement*  stmt = lst->tslStmt;
            BasicBlock* blk  = lst->tslBlock;

            /* Advance to the next node in the list */
            lst = lst->tslNext;

            // We may have cleared this CSE in optValuenumCSE_Availablity
            // due to different exception sets.
            //
            // Ignore this node if the gtCSEnum value has been cleared
            if (!IS_CSE_INDEX(exp->gtCSEnum))
            {
                continue;
            }

            // Assert if we used DEBUG_DESTROY_NODE on this CSE exp
            assert(exp->gtOper != GT_COUNT);

            /* Make sure we update the weighted ref count correctly */
            m_pCompiler->optCSEweight = blk->getBBWeight(m_pCompiler);

            /* Figure out the actual type of the value */
            var_types expTyp = genActualType(exp->TypeGet());

            // The cseLclVarType must be a compatible with expTyp
            //
            ValueNumStore* vnStore = m_pCompiler->vnStore;
            noway_assert(IsCompatibleType(cseLclVarTyp, expTyp) || (dsc->csdConstDefVN != vnStore->VNForNull()));

            // This will contain the replacement tree for exp
            // It will either be the CSE def or CSE ref
            //
            GenTree*      cse = nullptr;
            bool          isDef;
            FieldSeqNode* fldSeq               = nullptr;
            bool          commaOnly            = true;
            GenTree*      effectiveExp         = exp->gtEffectiveVal(commaOnly);
            const bool    hasZeroMapAnnotation = m_pCompiler->GetZeroOffsetFieldMap()->Lookup(effectiveExp, &fldSeq);

            if (IS_CSE_USE(exp->gtCSEnum))
            {
                /* This is a use of the CSE */
                isDef = false;
#ifdef DEBUG
                if (m_pCompiler->verbose)
                {
                    printf("\nWorking on the replacement of the CSE #%02u use at ", exp->gtCSEnum);
                    Compiler::printTreeID(exp);
                    printf(" in " FMT_BB "\n", blk->bbNum);
                }
#endif // DEBUG

                // We will replace the CSE ref with a new tree
                // this is typically just a simple use of the new CSE LclVar
                //

                // Create a reference to the CSE temp
                GenTree* cseLclVar = m_pCompiler->gtNewLclvNode(cseLclVarNum, cseLclVarTyp);
                cseLclVar->gtVNPair.SetBoth(dsc->csdConstDefVN);

                // Assign the ssa num for the lclvar use. Note it may be the reserved num.
                cseLclVar->AsLclVarCommon()->SetSsaNum(cseSsaNum);

                cse = cseLclVar;
                if (isSharedConst)
                {
                    ValueNum currVN   = m_pCompiler->vnStore->VNLiberalNormalValue(exp->gtVNPair);
                    ssize_t  curValue = m_pCompiler->vnStore->CoercedConstantValue<ssize_t>(currVN);
                    ssize_t  delta    = curValue - dsc->csdConstDefValue;
                    if (delta != 0)
                    {
                        GenTree* deltaNode = m_pCompiler->gtNewIconNode(delta, cseLclVarTyp);
                        cse                = m_pCompiler->gtNewOperNode(GT_ADD, cseLclVarTyp, cseLclVar, deltaNode);
                        cse->SetDoNotCSE();
                    }
                }

                // assign the proper ValueNumber, A CSE use discards any exceptions
                cse->gtVNPair = vnStore->VNPNormalPair(exp->gtVNPair);

                // shared const CSE has the correct value number assigned
                // and both liberal and conservative are identical
                // and they do not use theConservativeVN
                //
                if (!isSharedConst)
                {
                    ValueNum theConservativeVN = successfulCandidate->CseDsc()->defConservNormVN;

                    if (theConservativeVN != ValueNumStore::NoVN)
                    {
                        // All defs of this CSE share the same normal conservative VN, and we are rewriting this
                        // use to fetch the same value with no reload, so we can safely propagate that
                        // conservative VN to this use.  This can help range check elimination later on.
                        cse->gtVNPair.SetConservative(theConservativeVN);

                        // If the old VN was flagged as a checked bound, propagate that to the new VN
                        // to make sure assertion prop will pay attention to this VN.
                        ValueNum oldVN = exp->gtVNPair.GetConservative();
                        if (!vnStore->IsVNConstant(theConservativeVN) && vnStore->IsVNCheckedBound(oldVN))
                        {
                            vnStore->SetVNIsCheckedBound(theConservativeVN);
                        }

                        GenTree* cmp;
                        if ((m_pCompiler->optCseCheckedBoundMap != nullptr) &&
                            (m_pCompiler->optCseCheckedBoundMap->Lookup(exp, &cmp)))
                        {
                            // Propagate the new value number to this compare node as well, since
                            // subsequent range check elimination will try to correlate it with
                            // the other appearances that are getting CSEd.

                            ValueNum oldCmpVN = cmp->gtVNPair.GetConservative();
                            ValueNum newCmpArgVN;

                            ValueNumStore::CompareCheckedBoundArithInfo info;
                            if (vnStore->IsVNCompareCheckedBound(oldCmpVN))
                            {
                                // Comparison is against the bound directly.

                                newCmpArgVN = theConservativeVN;
                                vnStore->GetCompareCheckedBound(oldCmpVN, &info);
                            }
                            else
                            {
                                // Comparison is against the bound +/- some offset.

                                assert(vnStore->IsVNCompareCheckedBoundArith(oldCmpVN));
                                vnStore->GetCompareCheckedBoundArithInfo(oldCmpVN, &info);
                                newCmpArgVN = vnStore->VNForFunc(vnStore->TypeOfVN(info.arrOp), (VNFunc)info.arrOper,
                                                                 info.arrOp, theConservativeVN);
                            }
                            ValueNum newCmpVN = vnStore->VNForFunc(vnStore->TypeOfVN(oldCmpVN), (VNFunc)info.cmpOper,
                                                                   info.cmpOp, newCmpArgVN);
                            cmp->gtVNPair.SetConservative(newCmpVN);
                        }
                    }
                }
#ifdef DEBUG
                cse->gtDebugFlags |= GTF_DEBUG_VAR_CSE_REF;
#endif // DEBUG

                // Now we need to unmark any nested CSE's uses that are found in 'exp'
                // As well we extract any nested CSE defs that are found in 'exp' and
                // these are appended to the sideEffList

                // Afterwards the set of nodes in the 'sideEffectList' are preserved and
                // all other nodes are removed.
                //
                exp->gtCSEnum = NO_CSE; // clear the gtCSEnum field

                GenTree* sideEffList = nullptr;
                m_pCompiler->gtExtractSideEffList(exp, &sideEffList, GTF_PERSISTENT_SIDE_EFFECTS | GTF_IS_IN_CSE);

                // If we have any side effects or extracted CSE defs then we need to create a GT_COMMA tree instead
                //
                if (sideEffList != nullptr)
                {
#ifdef DEBUG
                    if (m_pCompiler->verbose)
                    {
                        printf("\nThis CSE use has side effects and/or nested CSE defs. The sideEffectList:\n");
                        m_pCompiler->gtDispTree(sideEffList);
                        printf("\n");
                    }
#endif

                    GenTree*     cseVal         = cse;
                    GenTree*     curSideEff     = sideEffList;
                    ValueNumPair exceptions_vnp = ValueNumStore::VNPForEmptyExcSet();

                    while ((curSideEff->OperGet() == GT_COMMA) || (curSideEff->OperGet() == GT_ASG))
                    {
                        GenTree* op1 = curSideEff->AsOp()->gtOp1;
                        GenTree* op2 = curSideEff->AsOp()->gtOp2;

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
                        curSideEff = curSideEff->AsOp()->gtOp1;
                    }

                    ValueNumPair op2vnp;
                    ValueNumPair op2Xvnp = ValueNumStore::VNPForEmptyExcSet();
                    vnStore->VNPUnpackExc(curSideEff->gtVNPair, &op2vnp, &op2Xvnp);
                    exceptions_vnp = vnStore->VNPExcSetUnion(exceptions_vnp, op2Xvnp);

                    op2Xvnp = ValueNumStore::VNPForEmptyExcSet();
                    vnStore->VNPUnpackExc(cseVal->gtVNPair, &op2vnp, &op2Xvnp);
                    exceptions_vnp = vnStore->VNPExcSetUnion(exceptions_vnp, op2Xvnp);

                    // Create a comma node with the sideEffList as op1
                    cse           = m_pCompiler->gtNewOperNode(GT_COMMA, expTyp, sideEffList, cseVal);
                    cse->gtVNPair = vnStore->VNPWithExc(op2vnp, exceptions_vnp);
                }
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
                    printf(" replaced in " FMT_BB " with def of V%02u\n", blk->bbNum, cseLclVarNum);
                }
#endif // DEBUG

                exp->gtCSEnum = NO_CSE; // clear the gtCSEnum field

                GenTree* val = exp;
                if (isSharedConst)
                {
                    ValueNum currVN   = m_pCompiler->vnStore->VNLiberalNormalValue(exp->gtVNPair);
                    ssize_t  curValue = m_pCompiler->vnStore->CoercedConstantValue<ssize_t>(currVN);
                    ssize_t  delta    = curValue - dsc->csdConstDefValue;
                    if (delta != 0)
                    {
                        val = m_pCompiler->gtNewIconNode(dsc->csdConstDefValue, cseLclVarTyp);
                        val->gtVNPair.SetBoth(dsc->csdConstDefVN);
                    }
                }

                /* Create an assignment of the value to the temp */
                GenTree* asg     = m_pCompiler->gtNewTempAssign(cseLclVarNum, val);
                GenTree* origAsg = asg;

                if (!asg->OperIs(GT_ASG))
                {
                    // This can only be the case for a struct in which the 'val' was a COMMA, so
                    // the assignment is sunk below it.
                    asg = asg->gtEffectiveVal(true);
                    noway_assert(origAsg->OperIs(GT_COMMA) && (origAsg == val));
                }
                else
                {
                    noway_assert(asg->AsOp()->gtOp2 == val);
                }

                // assign the proper Value Numbers
                asg->gtVNPair.SetBoth(ValueNumStore::VNForVoid()); // The GT_ASG node itself is $VN.Void
                asg->AsOp()->gtOp1->gtVNPair = val->gtVNPair;      // The dest op is the same as 'val'

                noway_assert(asg->AsOp()->gtOp1->gtOper == GT_LCL_VAR);

                // Backpatch the SSA def, if we're putting this CSE temp into ssa.
                asg->AsOp()->gtOp1->AsLclVar()->SetSsaNum(cseSsaNum);

                if (cseSsaNum != SsaConfig::RESERVED_SSA_NUM)
                {
                    LclSsaVarDsc* ssaVarDsc = m_pCompiler->lvaTable[cseLclVarNum].GetPerSsaData(cseSsaNum);

                    // These should not have been set yet, since this is the first and
                    // only def for this CSE.
                    assert(ssaVarDsc->GetBlock() == nullptr);
                    assert(ssaVarDsc->GetAssignment() == nullptr);

                    ssaVarDsc->m_vnPair = val->gtVNPair;
                    ssaVarDsc->SetBlock(blk);
                    ssaVarDsc->SetAssignment(asg->AsOp());
                }

                /* Create a reference to the CSE temp */
                GenTree* cseLclVar = m_pCompiler->gtNewLclvNode(cseLclVarNum, cseLclVarTyp);
                cseLclVar->gtVNPair.SetBoth(dsc->csdConstDefVN);

                // Assign the ssa num for the lclvar use. Note it may be the reserved num.
                cseLclVar->AsLclVarCommon()->SetSsaNum(cseSsaNum);

                GenTree* cseUse = cseLclVar;
                if (isSharedConst)
                {
                    ValueNum currVN   = m_pCompiler->vnStore->VNLiberalNormalValue(exp->gtVNPair);
                    ssize_t  curValue = m_pCompiler->vnStore->CoercedConstantValue<ssize_t>(currVN);
                    ssize_t  delta    = curValue - dsc->csdConstDefValue;
                    if (delta != 0)
                    {
                        GenTree* deltaNode = m_pCompiler->gtNewIconNode(delta, cseLclVarTyp);
                        cseUse             = m_pCompiler->gtNewOperNode(GT_ADD, cseLclVarTyp, cseLclVar, deltaNode);
                        cseUse->SetDoNotCSE();
                    }
                }
                cseUse->gtVNPair = val->gtVNPair; // The 'cseUse' is equal to 'val'

                /* Create a comma node for the CSE assignment */
                cse           = m_pCompiler->gtNewOperNode(GT_COMMA, expTyp, origAsg, cseUse);
                cse->gtVNPair = cseUse->gtVNPair; // The comma's value is the same as 'val'
                                                  // as the assignment to the CSE LclVar
                                                  // cannot add any new exceptions
            }

            cse->CopyReg(exp);  // The cse inheirits any reg num property from the orginal exp node
            exp->ClearRegNum(); // The exp node (for a CSE def) no longer has a register requirement

            // Walk the statement 'stmt' and find the pointer
            // in the tree is pointing to 'exp'
            //
            Compiler::FindLinkData linkData = m_pCompiler->gtFindLink(stmt, exp);
            GenTree**              link     = linkData.result;

#ifdef DEBUG
            if (link == nullptr)
            {
                printf("\ngtFindLink failed: stm=");
                Compiler::printStmtID(stmt);
                printf(", exp=");
                Compiler::printTreeID(exp);
                printf("\n");
                printf("stm =");
                m_pCompiler->gtDispStmt(stmt);
                printf("\n");
                printf("exp =");
                m_pCompiler->gtDispTree(exp);
                printf("\n");
            }
#endif // DEBUG

            noway_assert(link);

            // Mutate this link, thus replacing the old exp with the new CSE representation
            //
            *link = cse;

            // If it has a zero-offset field seq, copy annotation.
            if (hasZeroMapAnnotation)
            {
                m_pCompiler->fgAddFieldSeqForZeroOffset(cse, fldSeq);
            }

            assert(m_pCompiler->fgRemoveRestOfBlock == false);

            /* re-morph the statement */
            m_pCompiler->fgMorphBlockStmt(blk, stmt DEBUGARG("optValnumCSE"));

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

            if (dsc->defExcSetPromise == ValueNumStore::NoVN)
            {
                JITDUMP("Abandoned CSE #%02u because we had defs with different Exc sets\n", candidate.CseIndex());
                continue;
            }

            if (dsc->csdStructHndMismatch)
            {
                JITDUMP("Abandoned CSE #%02u because we had mismatching struct handles\n", candidate.CseIndex());
                continue;
            }

            candidate.InitializeCounts();

            if (candidate.UseCount() == 0)
            {
                JITDUMP("Skipped CSE #%02u because use count is 0\n", candidate.CseIndex());
                continue;
            }

#ifdef DEBUG
            if (m_pCompiler->verbose)
            {
                if (!Compiler::Is_Shared_Const_CSE(dsc->csdHashKey))
                {
                    printf("\nConsidering CSE #%02u {$%-3x, $%-3x} [def=%3u, use=%3u, cost=%3u%s]\n",
                           candidate.CseIndex(), dsc->csdHashKey, dsc->defExcSetPromise, candidate.DefCount(),
                           candidate.UseCount(), candidate.Cost(), dsc->csdLiveAcrossCall ? ", call" : "      ");
                }
                else
                {
                    size_t kVal = Compiler::Decode_Shared_Const_CSE_Value(dsc->csdHashKey);
                    printf("\nConsidering CSE #%02u {K_%p} [def=%3u, use=%3u, cost=%3u%s]\n", candidate.CseIndex(),
                           dspPtr(kVal), candidate.DefCount(), candidate.UseCount(), candidate.Cost(),
                           dsc->csdLiveAcrossCall ? ", call" : "      ");
                }
                printf("CSE Expression : \n");
                m_pCompiler->gtDispTree(candidate.Expr());
                printf("\n");
            }
#endif // DEBUG

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
        // Nothing to do, currently.
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
bool Compiler::optIsCSEcandidate(GenTree* tree)
{
    /* No good if the expression contains side effects or if it was marked as DONT CSE */

    if (tree->gtFlags & (GTF_ASG | GTF_DONT_CSE))
    {
        return false;
    }

    var_types  type = tree->TypeGet();
    genTreeOps oper = tree->OperGet();

    if (type == TYP_VOID)
    {
        return false;
    }

    // If this is a struct type (including SIMD*), we can only consider it for CSE-ing
    // if we can get its handle, so that we can create a temp.
    if (varTypeIsStruct(type) && (gtGetStructHandleIfPresent(tree) == NO_CLASS_HANDLE))
    {
        return false;
    }

#ifdef TARGET_X86
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
        cost = tree->GetCostSz();
    }
    else
    {
        cost = tree->GetCostEx();
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

            GenTreeCall* call;
            call = tree->AsCall();

            // Don't mark calls to allocation helpers as CSE candidates.
            // Marking them as CSE candidates usually blocks CSEs rather than enables them.
            // A typical case is:
            // [1] GT_IND(x) = GT_CALL ALLOC_HELPER
            // ...
            // [2] y = GT_IND(x)
            // ...
            // [3] z = GT_IND(x)
            // If we mark CALL ALLOC_HELPER as a CSE candidate, we later discover
            // that it can't be a CSE def because GT_INDs in [2] and [3] can cause
            // more exceptions (NullRef) so we abandon this CSE.
            // If we don't mark CALL ALLOC_HELPER as a CSE candidate, we are able
            // to use GT_IND(x) in [2] as a CSE def.
            if ((call->gtCallType == CT_HELPER) &&
                s_helperCallProperties.IsAllocator(eeGetHelperNum(call->gtCallMethHnd)))
            {
                return false;
            }

            // If we have a simple helper call with no other persistent side-effects
            // then we allow this tree to be a CSE candidate
            //
            if (gtTreeHasSideEffects(tree, GTF_PERSISTENT_SIDE_EFFECTS | GTF_IS_IN_CSE) == false)
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

            return (tree->AsOp()->gtOp1->gtOper != GT_ARR_ELEM);

        case GT_CNS_LNG:
#ifndef TARGET_64BIT
            return false; // Don't CSE 64-bit constants on 32-bit platforms
#endif
        case GT_CNS_INT:
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
        case GT_BSWAP:
        case GT_BSWAP16:
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
            return true; // Allow the CSE of Comparison operators

#ifdef FEATURE_SIMD
        case GT_SIMD:
            return true; // allow SIMD intrinsics to be CSE-ed

#endif // FEATURE_SIMD

#ifdef FEATURE_HW_INTRINSICS
        case GT_HWINTRINSIC:
        {
            GenTreeHWIntrinsic* hwIntrinsicNode = tree->AsHWIntrinsic();
            assert(hwIntrinsicNode != nullptr);
            HWIntrinsicCategory category = HWIntrinsicInfo::lookupCategory(hwIntrinsicNode->gtHWIntrinsicId);

            switch (category)
            {
#ifdef TARGET_XARCH
                case HW_Category_SimpleSIMD:
                case HW_Category_IMM:
                case HW_Category_Scalar:
                case HW_Category_SIMDScalar:
                case HW_Category_Helper:
                    break;
#elif defined(TARGET_ARM64)
                case HW_Category_SIMD:
                case HW_Category_SIMDByIndexedElement:
                case HW_Category_ShiftLeftByImmediate:
                case HW_Category_ShiftRightByImmediate:
                case HW_Category_Scalar:
                case HW_Category_Helper:
                    break;
#endif

                case HW_Category_MemoryLoad:
                case HW_Category_MemoryStore:
                case HW_Category_Special:
                default:
                    return false;
            }

            if (hwIntrinsicNode->OperIsMemoryStore())
            {
                // NI_BMI2_MultiplyNoFlags, etc...
                return false;
            }
            if (hwIntrinsicNode->OperIsMemoryLoad())
            {
                // NI_AVX2_BroadcastScalarToVector128, NI_AVX2_GatherVector128, etc...
                return false;
            }

            return true; // allow Hardware Intrinsics to be CSE-ed
        }

#endif // FEATURE_HW_INTRINSICS

        case GT_INTRINSIC:
            return true; // allow Intrinsics to be CSE-ed

        case GT_OBJ:
            return varTypeIsEnregisterable(type); // Allow enregisterable GT_OBJ's to be CSE-ed. (i.e. SIMD types)

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
    // We must clear the BBF_VISITED and BBF_MARKED flags.
    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        // And clear all the "visited" bits on the block.
        block->bbFlags &= ~(BBF_VISITED | BBF_MARKED);

        // Walk the statement trees in this basic block.
        for (Statement* stmt : StatementList(block->FirstNonPhiDef()))
        {
            // We must clear the gtCSEnum field.
            for (GenTree* tree = stmt->GetRootNode(); tree; tree = tree->gtPrev)
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

        // Initialize 'stmt' to the first non-Phi statement
        // Walk the statement trees in this basic block
        for (Statement* stmt : StatementList(block->FirstNonPhiDef()))
        {
            for (GenTree* tree = stmt->GetRootNode(); tree; tree = tree->gtPrev)
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
