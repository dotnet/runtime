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
#include "jitstd/algorithm.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "optcse.h"

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

inline CSEdsc* Compiler::optCSEfindDsc(unsigned index)
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
    noway_assert(optCSEweight >= 0);

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

        // 2. Unmark the CSE information in the node

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

    return WALK_CONTINUE;
}

// This functions walks all the node for an given tree
// and return the mask of CSE defs and uses for the tree
//
void Compiler::optCSE_GetMaskData(GenTree* tree, optCSE_MaskData* pMaskData)
{
    class MaskDataWalker : public GenTreeVisitor<MaskDataWalker>
    {
        optCSE_MaskData* m_maskData;

    public:
        enum
        {
            DoPreOrder = true,
        };

        MaskDataWalker(Compiler* comp, optCSE_MaskData* maskData) : GenTreeVisitor(comp), m_maskData(maskData)
        {
        }

        fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
        {
            GenTree* tree = *use;
            if (IS_CSE_INDEX(tree->gtCSEnum))
            {
                unsigned cseIndex = GET_CSE_INDEX(tree->gtCSEnum);
                // Note that we DO NOT use getCSEAvailBit() here, for the CSE_defMask/CSE_useMask
                unsigned cseBit = genCSEnum2bit(cseIndex);
                if (IS_CSE_DEF(tree->gtCSEnum))
                {
                    BitVecOps::AddElemD(m_compiler->cseMaskTraits, m_maskData->CSE_defMask, cseBit);
                }
                else
                {
                    BitVecOps::AddElemD(m_compiler->cseMaskTraits, m_maskData->CSE_useMask, cseBit);
                }
            }
            return fgWalkResult::WALK_CONTINUE;
        }
    };

    pMaskData->CSE_defMask = BitVecOps::MakeEmpty(cseMaskTraits);
    pMaskData->CSE_useMask = BitVecOps::MakeEmpty(cseMaskTraits);
    MaskDataWalker walker(this, pMaskData);
    walker.WalkTree(&tree, nullptr);
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
        cseMaskTraits = new (getAllocator(CMK_CSE)) BitVecTraits(optCSECandidateCount, this);
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

/*****************************************************************************
 *
 *  Compare function passed to jitstd::sort() by CSE_Heuristic::SortCandidates
 *  when (CodeOptKind() != Compiler::SMALL_CODE)
 */

/* static */
bool Compiler::optCSEcostCmpEx::operator()(const CSEdsc* dsc1, const CSEdsc* dsc2)
{
    GenTree* exp1 = dsc1->csdTree;
    GenTree* exp2 = dsc2->csdTree;

    auto expCost1 = exp1->GetCostEx();
    auto expCost2 = exp2->GetCostEx();

    if (expCost2 != expCost1)
    {
        return expCost2 < expCost1;
    }

    // Sort the higher Use Counts toward the top
    if (dsc2->csdUseWtCnt != dsc1->csdUseWtCnt)
    {
        return dsc2->csdUseWtCnt < dsc1->csdUseWtCnt;
    }

    // With the same use count, Sort the lower Def Counts toward the top
    if (dsc1->csdDefWtCnt != dsc2->csdDefWtCnt)
    {
        return dsc1->csdDefWtCnt < dsc2->csdDefWtCnt;
    }

    // In order to ensure that we have a stable sort, we break ties using the csdIndex
    return dsc1->csdIndex < dsc2->csdIndex;
}

/*****************************************************************************
 *
 *  Compare function passed to jitstd::sort() by CSE_Heuristic::SortCandidates
 *  when (CodeOptKind() == Compiler::SMALL_CODE)
 */

/* static */
bool Compiler::optCSEcostCmpSz::operator()(const CSEdsc* dsc1, const CSEdsc* dsc2)
{
    GenTree* exp1 = dsc1->csdTree;
    GenTree* exp2 = dsc2->csdTree;

    auto expCost1 = exp1->GetCostSz();
    auto expCost2 = exp2->GetCostSz();

    if (expCost2 != expCost1)
    {
        return expCost2 < expCost1;
    }

    // Sort the higher Use Counts toward the top
    if (dsc2->csdUseCount != dsc1->csdUseCount)
    {
        return dsc2->csdUseCount < dsc1->csdUseCount;
    }

    // With the same use count, Sort the lower Def Counts toward the top
    if (dsc1->csdDefCount != dsc2->csdDefCount)
    {
        return dsc1->csdDefCount < dsc2->csdDefCount;
    }

    // In order to ensure that we have a stable sort, we break ties using the csdIndex
    return dsc1->csdIndex < dsc2->csdIndex;
}

//---------------------------------------------------------------------------
// ComputeNumLocals: examine CSE def tree to compute number of locals it
//    uses
//
// Arguments:
//    compiler - compiler instance
//
// Notes:
//    Just looks at the first tree discovered.
//
void CSEdsc::ComputeNumLocals(Compiler* compiler)
{
    // Count the number of distinct locals and the total number of local var nodes in a tree.
    //
    class LocalCountingVisitor final : public GenTreeVisitor<LocalCountingVisitor>
    {
        struct LocalInfo
        {
            unsigned m_lclNum;
            unsigned m_occurrences;
        };
        enum
        {
            MAX_LOCALS = 8
        };
        LocalInfo m_locals[MAX_LOCALS];

    public:
        unsigned short m_count;
        unsigned short m_occurrences;

        enum
        {
            DoPreOrder    = true,
            DoLclVarsOnly = true,
        };

        LocalCountingVisitor(Compiler* compiler)
            : GenTreeVisitor<LocalCountingVisitor>(compiler), m_count(0), m_occurrences(0)
        {
        }

        Compiler::fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
        {
            GenTree* tree   = *use;
            unsigned lclNum = tree->AsLclVarCommon()->GetLclNum();

            m_occurrences++;
            for (unsigned i = 0; i < m_count; i++)
            {
                if (m_locals[i].m_lclNum == lclNum)
                {
                    m_locals[i].m_occurrences++;
                    return Compiler::fgWalkResult::WALK_CONTINUE;
                }
            }

            if (m_count >= MAX_LOCALS)
            {
                return Compiler::fgWalkResult::WALK_ABORT;
            }

            m_locals[m_count].m_lclNum      = lclNum;
            m_locals[m_count].m_occurrences = 1;
            m_count++;

            return Compiler::fgWalkResult::WALK_CONTINUE;
        }
    };

    LocalCountingVisitor lcv(compiler);
    lcv.WalkTree(&csdTree, nullptr);

    numDistinctLocals   = lcv.m_count;
    numLocalOccurrences = lcv.m_occurrences;
}

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
    bool     enableSharedConstCSE = false;
    bool     isSharedConst        = false;
    int      configValue          = JitConfig.JitConstCSE();

#if defined(TARGET_ARMARCH)
    // ARMARCH - allow to combine with nearby offsets, when config is not 2 or 4
    if ((configValue != CONST_CSE_ENABLE_ARM_NO_SHARING) && (configValue != CONST_CSE_ENABLE_ALL_NO_SHARING))
    {
        enableSharedConstCSE = true;
    }
#endif // TARGET_ARMARCH

    // All Platforms - also allow to combine with nearby offsets, when config is 3
    if (configValue == CONST_CSE_ENABLE_ALL)
    {
        enableSharedConstCSE = true;
    }

    // We use the liberal Value numbers when building the set of CSE
    ValueNum vnLib     = tree->GetVN(VNK_Liberal);
    ValueNum vnLibNorm = vnStore->VNNormalValue(vnLib);

    // We use the normal value number because we want the CSE candidate to
    // represent all expressions that produce the same normal value number.
    // We will handle the case where we have different exception sets when
    // promoting the candidates.
    //
    // We do this because a GT_IND will usually have a NullPtrExc entry in its
    // exc set, but we may have cleared the GTF_EXCEPT flag and if so, it won't
    // have an NullPtrExc, or we may have assigned the value of an GT_IND
    // into a LCL_VAR and then read it back later.
    //
    // When we are promoting the CSE candidates we ensure that any CSE
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
        // an BOUNDS_CHECK.
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
    if (enableSharedConstCSE && tree->IsIntegralConst())
    {
        assert(vnStore->IsVNConstant(vnLibNorm));

        // We don't share small offset constants when they require a reloc
        // Also, we don't share non-null const gc handles
        //
        if (!tree->AsIntConCommon()->ImmedValNeedsReloc(this) && (tree->IsIntegralConst(0) || !varTypeIsGC(tree)))
        {
            // Here we make constants that have the same upper bits use the same key
            //
            // We create a key that encodes just the upper bits of the constant by
            // shifting out some of the low bits, (12 or 16 bits)
            //
            // This is the only case where the hash key is not a ValueNumber
            //
            size_t constVal = vnStore->CoercedConstantValue<size_t>(vnLibNorm);
            key             = Encode_Shared_Const_CSE_Value(constVal);
            isSharedConst   = true;
        }
        else
        {
            // Use the vnLibNorm value as the key
            key = vnLibNorm;
        }
    }
    else // Not a GT_COMMA or a GT_CNS_INT
    {
        key = vnLibNorm;
    }

    // Make sure that the result of Is_Shared_Const_CSE(key) matches isSharedConst.
    // Note that when isSharedConst is true then we require that the TARGET_SIGN_BIT is set in the key
    // and otherwise we require that we never create a ValueNumber with the TARGET_SIGN_BIT set.
    //
    assert(isSharedConst == Is_Shared_Const_CSE(key));

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

            // Have we started the list of matching nodes?

            if (hashDsc->csdTreeList == nullptr)
            {
                // This is the second time we see this value. Handle cases
                // where the first value dominates the second one and we can
                // already prove that the first one is _not_ going to be a
                // valid def for the second one, due to the second one having
                // more exceptions. This happens for example in code like
                // CASTCLASS(x, y) where x was already proven to be of type y.
                // In those cases it is always better to let the second value
                // be the def.
                // This is essentially a less special-casey version of the
                // GT_COMMA handling above. However, it is quite limited since
                // it only handles the def/use being in the same block.
                if (compCurBB == hashDsc->csdBlock)
                {
                    GenTree* prevTree  = hashDsc->csdTree;
                    ValueNum prevVnLib = prevTree->GetVN(VNK_Liberal);
                    assert(vnStore->VNNormalValue(prevVnLib) == vnLibNorm);
                    if (prevVnLib != vnLib)
                    {
                        ValueNum prevExceptionSet = vnStore->VNExceptionSet(prevVnLib);
                        ValueNum curExceptionSet  = vnStore->VNExceptionSet(vnLib);
                        if (vnStore->VNExcIsSubset(curExceptionSet, prevExceptionSet))
                        {
                            prevTree->gtCSEnum = 0;
                            hashDsc->csdStmt   = stmt;
                            hashDsc->csdTree   = tree;
                            tree->gtCSEnum     = (signed char)hashDsc->csdIndex;
                            return hashDsc->csdIndex;
                        }
                    }
                }

                // Create the new element based upon the matching hashDsc element.

                newElem = new (this, CMK_TreeStatementList) treeStmtLst;

                newElem->tslTree  = hashDsc->csdTree;
                newElem->tslStmt  = hashDsc->csdStmt;
                newElem->tslBlock = hashDsc->csdBlock;
                newElem->tslNext  = nullptr;

                /* Start the list with the first CSE candidate recorded */

                hashDsc->csdTreeList = newElem;
                hashDsc->csdTreeLast = newElem;

                hashDsc->csdIsSharedConst = isSharedConst;
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

                hval                           = optCSEKeyToHashIndex(key, newOptCSEhashSize);
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
            hashDsc->csdIsSharedConst  = false;
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

        /* Record the new CSE index in the hashDsc */
        hashDsc->csdIndex = CSEindex;

        /* Update the gtCSEnum field in the original tree */
        noway_assert(hashDsc->csdTreeList->tslTree->gtCSEnum == 0);
        assert(FitsIn<signed char>(CSEindex));

        hashDsc->csdTreeList->tslTree->gtCSEnum = ((signed char)CSEindex);
        noway_assert(((unsigned)hashDsc->csdTreeList->tslTree->gtCSEnum) == CSEindex);

        tree->gtCSEnum = ((signed char)CSEindex);

        // Compute local info
        hashDsc->ComputeNumLocals(this);

#ifdef DEBUG
        if (verbose)
        {
            printf("\nCandidate " FMT_CSE ", key=", CSEindex);
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

//------------------------------------------------------------------------
// optValnumCSE_Locate: Locate CSE candidates and assign them indices.
//
// Arguments:
//    heuristic to consult in assessing candidates
//
// Returns:
//    true if there are any CSE candidates, false otherwise
//
bool Compiler::optValnumCSE_Locate(CSE_HeuristicCommon* heuristic)
{
    for (BasicBlock* const block : Blocks())
    {
        /* Make the block publicly available */
        compCurBB = block;

        /* Walk the statement trees in this basic block */
        for (Statement* const stmt : block->NonPhiStatements())
        {
            const bool isReturn = stmt->GetRootNode()->OperIs(GT_RETURN);

            /* We walk the tree in the forwards direction (bottom up) */
            bool stmtHasArrLenCandidate = false;
            for (GenTree* const tree : stmt->TreeList())
            {
                if (tree->OperIsCompare() && stmtHasArrLenCandidate)
                {
                    // Check if this compare is a function of (one of) the checked
                    // bound candidate(s); we may want to update its value number.
                    // if the array length gets CSEd
                    optCseUpdateCheckedBoundMap(tree);
                }

                if (!heuristic->ConsiderTree(tree, isReturn))
                {
                    continue;
                }

                /* Assign an index to this expression */

                unsigned CSEindex = optValnumCSE_Index(tree, stmt);

                if (CSEindex != 0)
                {
                    noway_assert(((unsigned)tree->gtCSEnum) == CSEindex);
                }

                if (IS_CSE_INDEX(CSEindex) && tree->OperIsArrLength())
                {
                    stmtHasArrLenCandidate = true;
                }
            }
        }
    }

    /* We're done if there were no interesting expressions */

    if (!optDoCSE)
    {
        return false;
    }

    /* We're finished building the expression lookup table */

    optCSEstop();

    return true;
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
//
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
                optCseCheckedBoundMap = new (getAllocator(CMK_CSE)) NodeToNodeMap(getAllocator());
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
    // (See CSE_DataFlow::EndMerge for an explanation of why this is necessary)
    //
    // The two bits per CSE candidate have the following meanings:
    //     11 - The CSE is available, and is also available when considering calls as killing availability.
    //     10 - The CSE is available, but is not available when considering calls as killing availability.
    //     00 - The CSE is not available
    //     01 - An illegal combination
    //
    const unsigned bitCount = (optCSECandidateCount * 2) + 1;

    // Init traits and cseCallKillsMask bitvectors.
    cseLivenessTraits = new (getAllocator(CMK_CSE)) BitVecTraits(bitCount, this);
    cseCallKillsMask  = BitVecOps::MakeEmpty(cseLivenessTraits);
    for (unsigned inx = 1; inx <= optCSECandidateCount; inx++)
    {
        unsigned cseAvailBit = getCSEAvailBit(inx);

        // a one preserves availability and a zero kills the availability
        // we generate this kind of bit pattern:  101010101010
        //
        BitVecOps::AddElemD(cseLivenessTraits, cseCallKillsMask, cseAvailBit);
    }

    for (BasicBlock* const block : Blocks())
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

    // We walk the set of CSE candidates and set the bit corresponding to the CSEindex
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
            unsigned    cseAvailBit          = getCSEAvailBit(CSEindex);
            unsigned    cseAvailCrossCallBit = getCSEAvailCrossCallBit(CSEindex);

            // This CSE is generated in 'block', we always set the cseAvailBit
            // If this block does not contain a call, we also set cseAvailCrossCallBit
            //
            // If we have a call in this block then in the loop below we walk the trees
            // backwards to find any CSEs that are generated after the last call in the block.
            //
            BitVecOps::AddElemD(cseLivenessTraits, block->bbCseGen, cseAvailBit);
            if (!block->HasFlag(BBF_HAS_CALL))
            {
                BitVecOps::AddElemD(cseLivenessTraits, block->bbCseGen, cseAvailCrossCallBit);
            }
            lst = lst->tslNext;
        }
    }

    for (BasicBlock* const block : Blocks())
    {
        // If the block doesn't contains a call then skip it...
        //
        if (!block->HasFlag(BBF_HAS_CALL))
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
                    unsigned cseAvailCrossCallBit = getCSEAvailCrossCallBit(CSEnum);
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
        for (BasicBlock* const block : Blocks())
        {
            if (!BitVecOps::IsEmpty(cseLivenessTraits, block->bbCseGen))
            {
                if (!headerPrinted)
                {
                    printf("\nBlocks that generate CSE def/uses\n");
                    headerPrinted = true;
                }
                printf(FMT_BB " cseGen = ", block->bbNum);
                optPrintCSEDataFlowSet(block->bbCseGen);
                printf("\n");
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

#if 0
#ifdef DEBUG
        if (m_comp->verbose)
        {
            printf("StartMerge " FMT_BB "\n", block->bbNum);
            printf("  :: cseOut    = %s\n", genES2str(m_comp->cseLivenessTraits, block->bbCseOut));
        }
#endif // DEBUG
#endif // 0
    }

    // Merge: perform the merging of each of the predecessor's liveness values (since this is a forward analysis)
    void Merge(BasicBlock* block, BasicBlock* predBlock, unsigned dupCount)
    {
#if 0
#ifdef DEBUG
        if (m_comp->verbose)
        {
            printf("Merge " FMT_BB " and " FMT_BB "\n", block->bbNum, predBlock->bbNum);
            printf("  :: cseIn     = %s\n", genES2str(m_comp->cseLivenessTraits, block->bbCseIn));
            printf("  :: cseOut    = %s\n", genES2str(m_comp->cseLivenessTraits, block->bbCseOut));
        }
#endif // DEBUG
#endif // 0

        BitVecOps::IntersectionD(m_comp->cseLivenessTraits, block->bbCseIn, predBlock->bbCseOut);

#if 0
#ifdef DEBUG
        if (m_comp->verbose)
        {
            printf("  => cseIn     = %s\n", genES2str(m_comp->cseLivenessTraits, block->bbCseIn));
        }
#endif // DEBUG
#endif // 0
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
    // We also handle the case where calls conditionally kill CSE availability.
    //
    bool EndMerge(BasicBlock* block)
    {
        // If this block is marked BBF_NO_CSE_IN (because of RBO), kill all CSEs.
        //
        if (block->HasFlag(BBF_NO_CSE_IN))
        {
            BitVecOps::ClearD(m_comp->cseLivenessTraits, block->bbCseIn);
        }

        // We can skip the calls kill step when our block doesn't have a callsite
        // or we don't have any available CSEs in our bbCseIn
        //
        if (!block->HasFlag(BBF_HAS_CALL) || BitVecOps::IsEmpty(m_comp->cseLivenessTraits, block->bbCseIn))
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
        // This is also why we need to allocate an extra bit in our cseLivenessTraits BitVecs.
        // We always need to visit our successor blocks once, thus we require that the first time
        // we visit a block we have a bit set in m_preMergeOut that won't be set when we compute
        // the new value of bbCseOut.
        //
        bool notDone = !BitVecOps::Equal(m_comp->cseLivenessTraits, block->bbCseOut, m_preMergeOut);

#if 0
#ifdef DEBUG
        if (m_comp->verbose)
        {
            printf("EndMerge " FMT_BB "\n", block->bbNum);
            printf("  :: cseIn     = %s\n", genES2str(m_comp->cseLivenessTraits, block->bbCseIn));
            if (block->HasFlag(BBC_HAS_CALL) &&
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
#endif // 0

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

        for (BasicBlock* const block : Blocks())
        {
            printf(FMT_BB "\n in: ", block->bbNum);
            optPrintCSEDataFlowSet(block->bbCseIn);
            printf("\ngen: ");
            optPrintCSEDataFlowSet(block->bbCseGen);
            printf("\nout: ");
            optPrintCSEDataFlowSet(block->bbCseOut);
            printf("\n");
        }

        printf("\n");
    }
#endif // DEBUG
}

//---------------------------------------------------------------------------
// optValnumCSE_Availability:
//
//     Using the information computed by CSE_DataFlow determine for each
//     CSE whether the CSE is a definition (if the CSE was not available)
//     or if the CSE is a use (if the CSE was previously made available).
//     The implementation iterates over all blocks setting 'available_cses'
//     to the CSEs that are available at input to the block.
//     When a CSE expression is encountered it is classified as either
//     as a definition (if the CSE is not in the 'available_cses' set) or
//     as a use (if the CSE is in the 'available_cses' set).  If the CSE
//     is a definition then it is added to the 'available_cses' set.
//
//     This algorithm uncovers the defs and uses gradually and as it does
//     so it also builds the exception set that all defs make: 'defExcSetCurrent'
//     and the exception set that the uses we have seen depend upon: 'defExcSetPromise'.
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
//             e2 = (p.a + q.b)     but both compute the same normal value
//        e.g. m.a = p.a;
//             e1 = (m.a + q.b)  :: e1 and e2 have different exception sets.
//             e2 = (p.a + q.b)     but both compute the same normal value
//
void Compiler::optValnumCSE_Availability()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("Labeling the CSEs with Use/Def information\n");
    }
#endif
    EXPSET_TP available_cses = BitVecOps::MakeEmpty(cseLivenessTraits);

    for (BasicBlock* const block : Blocks())
    {
        // Make the block publicly available

        compCurBB = block;

        // Retrieve the available CSE's at the start of this block

        BitVecOps::Assign(cseLivenessTraits, available_cses, block->bbCseIn);

        // Walk the statement trees in this basic block

        for (Statement* const stmt : block->NonPhiStatements())
        {
            // We walk the tree in the forwards direction (bottom up)

            for (GenTree* const tree : stmt->TreeList())
            {
                bool isUse = false;
                bool isDef = false;

                if (IS_CSE_INDEX(tree->gtCSEnum))
                {
                    unsigned CSEnum               = GET_CSE_INDEX(tree->gtCSEnum);
                    unsigned cseAvailBit          = getCSEAvailBit(CSEnum);
                    unsigned cseAvailCrossCallBit = getCSEAvailCrossCallBit(CSEnum);
                    CSEdsc*  desc                 = optCSEfindDsc(CSEnum);
                    weight_t stmw                 = block->getBBWeight(this);

                    isUse = BitVecOps::IsMember(cseLivenessTraits, available_cses, cseAvailBit);
                    isDef = !isUse; // If is isn't a CSE use, it is a CSE def

                    // Is this a "use", that we haven't yet marked as live across a call
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
                        printf(FMT_BB " ", block->bbNum);
                        printTreeID(tree);

                        printf(" %s of " FMT_CSE " [weight=%s]%s\n", isUse ? "Use" : "Def", CSEnum, refCntWtd2str(stmw),
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
                        // This is a CSE def

                        // Is defExcSetCurrent still set to the uninit marker value of VNForNull() ?
                        if (desc->defExcSetCurrent == vnStore->VNForNull())
                        {
                            // This is the first time visited, so record this defs exception set
                            desc->defExcSetCurrent = theLiberalExcSet;
                        }

                        // Have we seen a CSE use and made a promise of an exception set?
                        //
                        if (desc->defExcSetPromise != vnStore->VNForEmptyExcSet())
                        {
                            // The exception set held in desc->defExcSetPromise must be a subset of theLiberalExcSet
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
                        BitVecOps::AddElemD(cseLivenessTraits, available_cses, cseAvailBit);
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
                                // CSE use has an exception set item that is not promised
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

                // In order to determine if a CSE is live across a call, we model availability using two bits and
                // kill all of the cseAvailCrossCallBit for each CSE whenever we see a GT_CALL (unless the call
                // generates a CSE).
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
                                unsigned cseAvailCrossCallBit = getCSEAvailCrossCallBit(CSEnum);

                                BitVecOps::AddElemD(cseLivenessTraits, available_cses, cseAvailCrossCallBit);
                            }
                        }
                    }
                }
            }
        }
    }
}

//------------------------------------------------------------------------
// CSE_HeuristicCommon: construct basic CSE heuristic
//
// Arguments;
//  pCompiler - compiler instance
//
// Notes:
//  This creates the basic CSE heuristic. It never does any CSEs.
//
CSE_HeuristicCommon::CSE_HeuristicCommon(Compiler* pCompiler) : m_pCompiler(pCompiler)
{
    m_addCSEcount = 0; /* Count of the number of LclVars for CSEs that we added */
    sortTab       = nullptr;
    sortSiz       = 0;
    madeChanges   = false;
    codeOptKind   = m_pCompiler->compCodeOpt();

    enableConstCSE = true;

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

#ifdef DEBUG
    // Track the order of CSEs done (candidate number)
    //
    CompAllocator allocator = m_pCompiler->getAllocator(CMK_CSE);
    m_sequence              = new (allocator) jitstd::vector<unsigned>(allocator);
#endif

    JITDUMP("CONST CSE is %s\n", enableConstCSE ? "enabled" : "disabled");
}

//------------------------------------------------------------------------
// CanConsiderTree: check if this tree can be a CSE candidate
//
// Arguments:
//   tree - tree in question
//   isReturn - true if tree is part of a return statement
//
// Returns:
//    true if this tree can be a CSE candidate
//
// Notes:
//   This currently does both legality and profitability checks.
//   Eventually it should just do legality checks.
//
bool CSE_HeuristicCommon::CanConsiderTree(GenTree* tree, bool isReturn)
{
    // Don't allow CSE of constants if it is disabled
    //
    if (tree->IsIntegralConst())
    {
        if (!enableConstCSE &&
            // Unconditionally allow these constant handles to be CSE'd
            !tree->IsIconHandle(GTF_ICON_STATIC_HDL) && !tree->IsIconHandle(GTF_ICON_CLASS_HDL) &&
            !tree->IsIconHandle(GTF_ICON_STR_HDL) && !tree->IsIconHandle(GTF_ICON_OBJ_HDL))
        {
            return false;
        }
    }

    // Don't allow non-SIMD struct CSEs under a return; we don't fully
    // re-morph these if we introduce a CSE assignment, and so may create
    // IR that lower is not yet prepared to handle.
    //
    if (isReturn && varTypeIsStruct(tree->gtType) && !varTypeIsSIMD(tree->gtType))
    {
        return false;
    }

    // No good if the expression contains side effects or if it was marked as DONT CSE
    //
    if (tree->gtFlags & (GTF_ASG | GTF_DONT_CSE))
    {
        return false;
    }

    var_types type = tree->TypeGet();

    if (type == TYP_VOID)
    {
        return false;
    }

    unsigned cost;
    if (codeOptKind == Compiler::SMALL_CODE)
    {
        cost = tree->GetCostSz();
    }
    else
    {
        cost = tree->GetCostEx();
    }

    //  Don't bother if the potential savings are very low
    //
    if (cost < Compiler::MIN_CSE_COST)
    {
        return false;
    }

    genTreeOps oper = tree->OperGet();

#if !CSE_CONSTS
    //  Don't bother with constants
    //
    if (tree->OperIsConst())
    {
        return false;
    }
#endif

    // Check for special cases
    //
    switch (oper)
    {
        case GT_CALL:
        {
            GenTreeCall* const call = tree->AsCall();

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
                Compiler::s_helperCallProperties.IsAllocator(m_pCompiler->eeGetHelperNum(call->gtCallMethHnd)))
            {
                return false;
            }

            // If we have a simple helper call with no other persistent side-effects
            // then we allow this tree to be a CSE candidate
            //
            if (m_pCompiler->gtTreeHasSideEffects(tree, GTF_PERSISTENT_SIDE_EFFECTS | GTF_IS_IN_CSE))
            {
                return false;
            }
        }
        break;

        case GT_IND:
            // TODO-CQ: Review this...
            /* We try to cse GT_ARR_ELEM nodes instead of GT_IND(GT_ARR_ELEM).
                Doing the first allows cse to also kick in for code like
                "GT_IND(GT_ARR_ELEM) = GT_IND(GT_ARR_ELEM) + xyz", whereas doing
                the second would not allow it */

            if (tree->AsOp()->gtOp1->gtOper == GT_ARR_ELEM)
            {
                return false;
            }
            break;

        case GT_CNS_LNG:
#ifndef TARGET_64BIT
            return false; // Don't CSE 64-bit constants on 32-bit platforms
#endif
        case GT_CNS_INT:
        case GT_CNS_DBL:
        case GT_CNS_STR:
        case GT_CNS_VEC:
            break;

        case GT_ARR_ELEM:
        case GT_ARR_LENGTH:
        case GT_MDARR_LENGTH:
        case GT_MDARR_LOWER_BOUND:
            break;

        case GT_LCL_VAR:
            return false; // Can't CSE a volatile LCL_VAR

        case GT_NEG:
        case GT_NOT:
        case GT_BSWAP:
        case GT_BSWAP16:
        case GT_CAST:
        case GT_BITCAST:
            break;

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
            break;

        case GT_ADD: // Check for ADDRMODE flag on these Binary Operators
        case GT_MUL:
        case GT_LSH:
            if ((tree->gtFlags & GTF_ADDRMODE_NO_CSE) != 0)
            {
                return false;
            }
            break;

        case GT_EQ:
        case GT_NE:
        case GT_LT:
        case GT_LE:
        case GT_GE:
        case GT_GT:
            break;

#ifdef FEATURE_HW_INTRINSICS
        case GT_HWINTRINSIC:
        {
            GenTreeHWIntrinsic* hwIntrinsicNode = tree->AsHWIntrinsic();
            assert(hwIntrinsicNode != nullptr);
            HWIntrinsicCategory category = HWIntrinsicInfo::lookupCategory(hwIntrinsicNode->GetHWIntrinsicId());

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
        }
        break;

#endif // FEATURE_HW_INTRINSICS

        case GT_INTRINSIC:
            break;

        case GT_BLK:
        case GT_LCL_FLD:
            // TODO-1stClassStructs: support CSE for enregisterable TYP_STRUCTs.
            if (!varTypeIsEnregisterable(type))
            {
                return false;
            }
            break;

        case GT_COMMA:
            break;

        case GT_COLON:
        case GT_QMARK:
        case GT_NOP:
        case GT_RETURN:
            return false; // Currently the only special nodes that we hit
                          // that we know that we don't want to CSE

        default:
            return false;
    }

    ValueNumStore* const vnStore = m_pCompiler->GetValueNumStore();

    ValueNum valueVN = vnStore->VNNormalValue(tree->GetVN(VNK_Liberal));
    if (ValueNumStore::isReservedVN(valueVN) && (valueVN != ValueNumStore::VNForNull()))
    {
        return false;
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
        return false;
    }

    return true;
}

#ifdef DEBUG

//------------------------------------------------------------------------
// DumpMetrics: dump post-CSE metrics
//
void CSE_HeuristicCommon::DumpMetrics()
{
    printf(" %s", Name());
    printf(" seq ");
    for (unsigned i = 0; i < m_sequence->size(); i++)
    {
        printf("%s%i", (i == 0) ? "" : ",", (*m_sequence)[i]);
    }
}

//------------------------------------------------------------------------
// CSE_HeuristicRandom: construct random CSE heuristic
//
// Arguments;
//  pCompiler - compiler instance
//
// Notes:
//  This creates the random CSE heuristic. It does CSEs randomly, with some
//  predetermined likelihood (set by config or by stress).
//
CSE_HeuristicRandom::CSE_HeuristicRandom(Compiler* pCompiler) : CSE_HeuristicCommon(pCompiler)
{
    m_cseRNG.Init(m_pCompiler->info.compMethodHash() ^ JitConfig.JitRandomCSE());
    Announce();
}

//------------------------------------------------------------------------
// Announce: describe heuristic in jit dump
//
void CSE_HeuristicRandom::Announce()
{
    JITDUMP("JitRandomCSE is enabled with salt %d\n", JitConfig.JitRandomCSE());
}

//------------------------------------------------------------------------
// ConsiderTree: check if this tree can be a CSE candidate
//
// Arguments:
//   tree - tree in question
//   isReturn - true if tree is part of a return statement
//
// Returns:
//    true if this tree can be a CSE candidate
//
bool CSE_HeuristicRandom::ConsiderTree(GenTree* tree, bool isReturn)
{
    return CanConsiderTree(tree, isReturn);
}

//------------------------------------------------------------------------
// ConsiderCandidates: examine candidates and perform CSEs.
//
void CSE_HeuristicRandom::ConsiderCandidates()
{
    // Generate a random permutation of all candidates.
    // We rely on the fact that SortCandidates set up
    // sortTab to be a copy of m_pCompiler->optCSEtab.
    //
    const unsigned n = m_pCompiler->optCSECandidateCount;

    if (n == 0)
    {
        // No candidates
        return;
    }

    // Fill sortTab with random permutation of the optCSETab
    // (via the "inside-out" Fisher-Yates shuffle)
    //
    sortTab = new (m_pCompiler, CMK_CSE) CSEdsc*[n];

    for (unsigned i = 0; i < n; i++)
    {
        // Choose j in [0...i]
        //
        const unsigned j = m_cseRNG.Next(i + 1);
        if (i != j)
        {
            sortTab[i] = sortTab[j];
        }
        sortTab[j] = m_pCompiler->optCSEtab[i];
    }

    // Randomly perform the first K of these CSEs
    // where K is uniform within [1...n].
    //
    unsigned k = m_cseRNG.Next(n) + 1;

    CSEdsc** ptr = sortTab;
    for (; (k > 0); k--, ptr++)
    {
        const int     attempt = m_pCompiler->optCSEattempt++;
        CSEdsc* const dsc     = *ptr;
        CSE_Candidate candidate(this, dsc);

        JITDUMP("\nRandomly attempting " FMT_CSE "\n", candidate.CseIndex());
        JITDUMP("CSE Expression : \n");
        JITDUMPEXEC(m_pCompiler->gtDispTree(candidate.Expr()));
        JITDUMP("\n");

        if (dsc->defExcSetPromise == ValueNumStore::NoVN)
        {
            JITDUMP("Abandoned " FMT_CSE " because we had defs with different Exc sets\n", candidate.CseIndex());
            continue;
        }

        candidate.InitializeCounts();

        if (candidate.UseCount() == 0)
        {
            JITDUMP("Skipped " FMT_CSE " because use count is 0\n", candidate.CseIndex());
            continue;
        }

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

        PerformCSE(&candidate);
        madeChanges = true;
    }
}

//------------------------------------------------------------------------
// CSE_HeuristicReplay: construct replay CSE heuristic
//
// Arguments;
//  pCompiler - compiler instance
//
// Notes:
//  This creates the replay CSE heuristic. It does CSEs specifed by
//  the ArrayConfig parsing of JitReplayCSE.
//
CSE_HeuristicReplay::CSE_HeuristicReplay(Compiler* pCompiler) : CSE_HeuristicCommon(pCompiler)
{
    Announce();
}

//------------------------------------------------------------------------
// Announce: describe heuristic in jit dump
//
void CSE_HeuristicReplay::Announce()
{
    JITDUMP("JitReplayCSE is enabled with config %s\n", JitConfig.JitReplayCSE());
}

//------------------------------------------------------------------------
// ConsiderTree: check if this tree can be a CSE candidate
//
// Arguments:
//   tree - tree in question
//   isReturn - true if tree is part of a return statement
//
// Returns:
//    true if this tree can be a CSE candidate
//
bool CSE_HeuristicReplay::ConsiderTree(GenTree* tree, bool isReturn)
{
    return CanConsiderTree(tree, isReturn);
}

//------------------------------------------------------------------------
// ConsiderCandidates: examine candidates and perform CSEs.
//
// Notes:
//   Simply follows the script provided.
//
void CSE_HeuristicReplay::ConsiderCandidates()
{
    const unsigned n = m_pCompiler->optCSECandidateCount;

    if (n == 0)
    {
        // No candidates
        return;
    }

    static ConfigIntArray JitReplayCSEArray;
    JitReplayCSEArray.EnsureInit(JitConfig.JitReplayCSE());

    for (unsigned i = 0; i < JitReplayCSEArray.GetLength(); i++)
    {
        // optCSEtab is 0-based; candidate numbers are 1-based
        //
        const int index = JitReplayCSEArray.GetData()[i] - 1;

        if ((index < 0) || (index >= (int)n))
        {
            JITDUMP("Invalid candidate number %d\n", index + 1);
            continue;
        }
        const int     attempt = m_pCompiler->optCSEattempt++;
        CSEdsc* const dsc     = m_pCompiler->optCSEtab[index];
        CSE_Candidate candidate(this, dsc);

        JITDUMP("\nReplay attempting " FMT_CSE "\n", candidate.CseIndex());
        JITDUMP("CSE Expression : \n");
        JITDUMPEXEC(m_pCompiler->gtDispTree(candidate.Expr()));
        JITDUMP("\n");

        if (!dsc->IsViable())
        {
            JITDUMP("Abandoned " FMT_CSE " -- not viable\n", candidate.CseIndex());
            continue;
        }

        PerformCSE(&candidate);
        madeChanges = true;
    }
}

//------------------------------------------------------------------------
// CSE_HeuristicRL: construct RL CSE heuristic
//
// Arguments;
//  pCompiler - compiler instance
//
// Notes:
//  This creates the RL CSE heuristic. It does CSEs based on a stochastic
//  softmax policy, governed by a parameter vector.
//
//  JitRLCSE specified the initial parameter values.
//  JitRandomCSE can be used to supply salt for the RNG.
//  JitReplayCSE can be used to supply a sequence to follow.
//  JitReplayCSEReward can be used to supply the perf score for the sequence.
//
CSE_HeuristicRL::CSE_HeuristicRL(Compiler* pCompiler)
    : CSE_HeuristicCommon(pCompiler), m_alpha(0.0), m_updateParameters(false), m_greedy(false), m_verbose(false)
{
    // Set up the random state
    //
    m_cseRNG.Init(m_pCompiler->info.compMethodHash() ^ JitConfig.JitRandomCSE());

    // Parameters
    //
    static ConfigDoubleArray initialParameters;
    initialParameters.EnsureInit(JitConfig.JitRLCSE());
    const unsigned initialParamLength = initialParameters.GetLength();

    for (unsigned i = 0; (i < initialParamLength) && (i < numParameters); i++)
    {
        m_parameters[i] = initialParameters.GetData()[i];
    }

    if (numParameters > initialParamLength)
    {
        JITDUMP("Too few parameters (expected %d), trailing will be zero\n", numParameters);
        for (unsigned i = initialParamLength; i < numParameters; i++)
        {
            m_parameters[i] = 0;
        }
    }
    else if (numParameters < initialParamLength)
    {
        JITDUMP("Too many parameters (expected %d), trailing will be ignored\n", numParameters);
    }

    // Policy sub-behavior: explore / update / greedy
    //
    // We may be given a prior sequence and perf score to use to
    // update the parameters .... if so, we will replay same sequence of CSEs
    // (like the replay policy) and update the parameters via the policy
    // gradient algorithm.
    //
    // For updates:
    //
    // m_alpha controls the "step size" or learning rate; when we want to adjust
    // the parameters we only partially move them towards the gradient indicated values.
    //
    // m_rewards describes the reward associated with each step.
    //
    // This "two-pass" technique (first run the current policy and, obtain the perf score
    // and CSE sequence, then rerun with the same sequence and update the policy
    // parameters) ensures all the policy model logic is within the
    // JIT, so the preference computation and its gradient can be kept in sync.
    //
    if ((JitConfig.JitReplayCSE() != nullptr) && (JitConfig.JitReplayCSEReward() != nullptr))
    {
        m_updateParameters = true;

        // Reward
        //
        static ConfigDoubleArray rewards;
        rewards.EnsureInit(JitConfig.JitReplayCSEReward());
        const unsigned rewardsLength = rewards.GetLength();

        for (unsigned i = 0; (i < rewardsLength) && (i < maxSteps); i++)
        {
            m_rewards[i] = rewards.GetData()[i];
        }

        for (unsigned i = rewardsLength; i < maxSteps; i++)
        {
            m_rewards[i] = 0;
        }

        // Alpha
        //
        if (JitConfig.JitRLCSEAlpha() != nullptr)
        {
            static ConfigDoubleArray JitRLCSEAlphaArray;
            JitRLCSEAlphaArray.EnsureInit(JitConfig.JitRLCSEAlpha());
            m_alpha = JitRLCSEAlphaArray.GetData()[0];
        }
        else
        {
            m_alpha = 0.001;
        }
    }
    else if (JitConfig.JitRLCSEGreedy() > 0)
    {
        m_greedy = true;
    }

    // Stopping "parameter"
    //
    m_registerPressure = CNT_CALLEE_TRASH + CNT_CALLEE_SAVED;

    // Verbose
    //
    if (m_pCompiler->verbose || (JitConfig.JitRLCSEVerbose() > 0))
    {
        m_verbose = true;
    }

#ifdef DEBUG
    CompAllocator allocator = m_pCompiler->getAllocator(CMK_CSE);
    m_likelihoods           = new (allocator) jitstd::vector<double>(allocator);
    m_baseLikelihoods       = new (allocator) jitstd::vector<double>(allocator);
    m_features              = new (allocator) jitstd::vector<char*>(allocator);
#endif
    Announce();
}

//------------------------------------------------------------------------
// Name: name this jit heuristic
//
// Returns:
//   descriptive name string
//
const char* CSE_HeuristicRL::Name() const
{
    if (m_updateParameters)
    {
        return "RL Policy Gradient Update";
    }
    else if (m_greedy)
    {
        return "RL Policy Gradient Greedy";
    }
    else
    {
        return "RL Policy Gradient Stochastic";
    }
}

//------------------------------------------------------------------------
// Announce: describe heuristic in jit dump
//
void CSE_HeuristicRL::Announce()
{
    JITDUMP("%s salt %d parameters ", Name(), JitConfig.JitRandomCSE());
    for (int i = 0; i < numParameters; i++)
    {
        JITDUMP("%s%f", (i == 0) ? "" : ",", m_parameters[i]);
    }
    JITDUMP("\n");

    if (m_updateParameters)
    {
        JITDUMP("Operating in update mode with sequence %ls, rewards %ls, and alpha %f\n", JitConfig.JitReplayCSE(),
                JitConfig.JitReplayCSEReward(), m_alpha);
    }
}

//------------------------------------------------------------------------
// DumpMetrics: dump post-CSE metrics
//
void CSE_HeuristicRL::DumpMetrics()
{
    CSE_HeuristicCommon::DumpMetrics();

    if (m_updateParameters)
    {
        // For update, dump the new parameter values
        //
        printf(" updatedparams ");
        for (int i = 0; i < numParameters; i++)
        {
            printf("%s%f", (i == 0) ? "" : ",", m_parameters[i]);
        }

        if (JitConfig.JitRLCSECandidateFeatures() > 0)
        {
            bool first = true;
            printf(", features ");
            for (char* f : *m_features)
            {
                printf("%s%s", first ? "" : ",", f);
                first = false;
            }
        }
    }
    else if (m_greedy)
    {
        // Show the parameters used.
        //
        printf(" params ");
        for (int i = 0; i < numParameters; i++)
        {
            printf("%s%f", (i == 0) ? "" : ",", m_parameters[i]);
        }
    }
    else
    {
        // For evaluation, dump likelihood of the choices made
        //
        printf(" likelihoods ");
        bool first = true;
        for (double d : *m_likelihoods)
        {
            printf("%s%.3f", first ? "" : ",", d);
            first = false;
        }

        // For evaluation, dump initial likelihood each choice
        //
        printf(" baseLikelihoods ");
        first = true;
        for (double d : *m_baseLikelihoods)
        {
            printf("%s%.3f", first ? "" : ",", d);
            first = false;
        }
    }
}

//------------------------------------------------------------------------
// ConsiderTree: check if this tree can be a CSE candidate
//
// Arguments:
//   tree - tree in question
//   isReturn - true if tree is part of a return statement
//
// Returns:
//    true if this tree can be a CSE candidate
//
bool CSE_HeuristicRL::ConsiderTree(GenTree* tree, bool isReturn)
{
    return CanConsiderTree(tree, isReturn);
}

//------------------------------------------------------------------------
// CaptureLocalWeights: build a sorted vector of normalized enregisterable
//   local weights (highest to lowest)
//
// Notes:
//    Used to estimate where the temp introduced by a CSE would rank compared
//    to other locals in the method, as they compete for registers.
//
void CSE_HeuristicRL::CaptureLocalWeights()
{
    JITDUMP("Local weight table...\n");
    CompAllocator allocator = m_pCompiler->getAllocator(CMK_SSA);
    m_localWeights          = new (allocator) jitstd::vector<double>(allocator);

    for (unsigned trackedIndex = 0; trackedIndex < m_pCompiler->lvaTrackedCount; trackedIndex++)
    {
        LclVarDsc* const varDsc = m_pCompiler->lvaGetDescByTrackedIndex(trackedIndex);

        // Locals with no references aren't enregistered
        //
        if (varDsc->lvRefCnt() == 0)
        {
            continue;
        }

        // Some LclVars always have stack homes
        //
        if (varDsc->lvDoNotEnregister)
        {
            continue;
        }

        // Only consider for integral types
        //
        if (varTypeIsFloating(varDsc->TypeGet()) || varTypeIsMask(varDsc->TypeGet()))
        {
            continue;
        }

        JITDUMP("V%02u," FMT_WT "\n", m_pCompiler->lvaGetLclNum(varDsc), varDsc->lvRefCntWtd());
        m_localWeights->push_back(varDsc->lvRefCntWtd() / BB_UNITY_WEIGHT);
    }
}

//------------------------------------------------------------------------
// ConsiderCandidates: examine candidates and perform CSEs.
//
void CSE_HeuristicRL::ConsiderCandidates()
{
    const int numCandidates = m_pCompiler->optCSECandidateCount;
    sortTab                 = new (m_pCompiler, CMK_CSE) CSEdsc*[numCandidates];
    sortSiz                 = numCandidates * sizeof(*sortTab);
    memcpy(sortTab, m_pCompiler->optCSEtab, sortSiz);

    // Capture distribution of enregisterable local var weights.
    //
    CaptureLocalWeights();

    if (m_updateParameters)
    {
        UpdateParameters();
        return;
    }

    if (m_greedy)
    {
        GreedyPolicy();
        return;
    }

    SoftmaxPolicy();
}

//------------------------------------------------------------------------
// GreedyPolicy: use a greedy policy
//
// Notes:
//   This always performs the most-preferred choice, using lower candidate number
//   as a tie-breaker.
//
void CSE_HeuristicRL::GreedyPolicy()
{
    if (m_verbose)
    {
        printf("RL using greedy policy\n");
    }

    // Number of choices is num candidates + 1, since
    // early stopping is also a choice.
    //
    const int          numCandidates = m_pCompiler->optCSECandidateCount;
    ArrayStack<Choice> choices(m_pCompiler->getAllocator(CMK_CSE), numCandidates + 1);

    while (true)
    {
        Choice&       choice = ChooseGreedy(choices);
        CSEdsc* const dsc    = choice.m_dsc;

        if (dsc == nullptr)
        {
            m_likelihoods->push_back(choice.m_softmax);
            break;
        }

        // purge this CSE from sortTab so we won't choose it again
        //
        assert(sortTab[dsc->csdIndex - 1] == dsc);
        sortTab[dsc->csdIndex - 1] = nullptr;

        // ChooseCSE should only choose viable options
        //
        assert(dsc->IsViable());

        CSE_Candidate candidate(this, dsc);

        if (m_verbose)
        {
            printf("\nRL attempting " FMT_CSE "\n", candidate.CseIndex());
        }

        JITDUMP("CSE Expression : \n");
        JITDUMPEXEC(m_pCompiler->gtDispTree(candidate.Expr()));
        JITDUMP("\n");

        PerformCSE(&candidate);
        madeChanges = true;
        m_likelihoods->push_back(choice.m_softmax);
    }

    return;
}

//------------------------------------------------------------------------
// SoftmaxPolicy: use a randomized softmax policy
//
// Notes:
//   This converts preferences to likelihoods using softmax, and then
//   randomly selects a candidate proportional to its likelihood.
//
void CSE_HeuristicRL::SoftmaxPolicy()
{
    if (m_verbose)
    {
        printf("RL using softmax policy\n");
    }

    // Number of choices is num candidates + 1, since
    // early stopping is also a choice.
    //
    const int          numCandidates = m_pCompiler->optCSECandidateCount;
    ArrayStack<Choice> choices(m_pCompiler->getAllocator(CMK_CSE), numCandidates + 1);
    bool               first = true;

    while (true)
    {
        Choice& choice = ChooseSoftmax(choices);

        if (first)
        {
            for (int i = 0; i < choices.Height(); i++)
            {
                Choice& option = choices.TopRef(i);
                if (option.m_dsc == nullptr)
                {
                    m_baseLikelihoods->push_back(0);
                }
                else
                {
                    m_baseLikelihoods->push_back(option.m_dsc->csdIndex);
                }
                m_baseLikelihoods->push_back(option.m_softmax);
            }
            first = false;
        }

        CSEdsc* const dsc = choice.m_dsc;

        if (dsc == nullptr)
        {
            m_likelihoods->push_back(choice.m_softmax);
            break;
        }

        // purge this CSE from sortTab so we won't choose it again
        //
        assert(sortTab[dsc->csdIndex - 1] == dsc);
        sortTab[dsc->csdIndex - 1] = nullptr;

        // ChooseCSE should only choose viable options
        //
        assert(dsc->IsViable());

        CSE_Candidate candidate(this, dsc);

        if (m_verbose)
        {
            printf("\nRL attempting " FMT_CSE "\n", candidate.CseIndex());
        }

        JITDUMP("CSE Expression : \n");
        JITDUMPEXEC(m_pCompiler->gtDispTree(candidate.Expr()));
        JITDUMP("\n");

        PerformCSE(&candidate);
        madeChanges = true;
        m_likelihoods->push_back(choice.m_softmax);
    }

    return;
}

//------------------------------------------------------------------------
// GetFeatures: extract features for this CSE
//
// Arguments:
//    cse - cse descriptor
//    features - array to fill in with feature values
//
// Notes:
//    Current set of features:
//
//    0. cse costEx
//    1. cse use count weighted (log)
//    2. cse def count weighted (log)
//    3. cse costSz
//    4. cse use count
//    5. cse def count
//    6. cse live across call (0/1)
//    7. cse is int (0/1)
//    8. cse is a constant, but not shared (0/1)
//    9. cse is a shared const (0/1)
//   10. cse cost is MIN_CSE_COST (0/1)
//   11. cse is a constant and live across call (0/1)
//   12. cse is a constant and min cost (0/1)
//   13. cse cost is MIN_CSE_COST (0/1) and cse is live across call (0/1)
//   14. cse is marked GTF_MAKE_CSE (0/1)
//   15. cse num distinct locals
//   16. cse num local occurrences
//   17. cse has call (0/1)
//   18. log (cse use count weighted * costEx)
//   19. log (cse use count weighted * num local occurrences)
//   20. cse "distance" (max postorder num - min postorder num) / num BBs
//   21. cse is "containable" (0/1)
//   22. cse is cheap & containable (0/1)
//   23. is live across call in possible LSRA ordering (0/1)
//
//   -----
//
//   24. log (pressure estimate weight)
//
void CSE_HeuristicRL::GetFeatures(CSEdsc* cse, double* features)
{
    for (int i = 0; i < numParameters; i++)
    {
        features[i] = 0;
    }

    if (cse == nullptr)
    {
        GetStoppingFeatures(features);
        return;
    }

    const unsigned char costEx       = cse->csdTree->GetCostEx();
    const double        deMinimis    = 1e-3;
    const double        deMinimusAdj = -log(deMinimis);

    features[0] = costEx;
    features[1] = deMinimusAdj + log(max(deMinimis, cse->csdUseWtCnt));
    features[2] = deMinimusAdj + log(max(deMinimis, cse->csdDefWtCnt));
    features[3] = cse->csdTree->GetCostSz();
    features[4] = cse->csdUseCount;
    features[5] = cse->csdDefCount;

    // Boolean features get scaled up so their dynamic range
    // is similar to the features above, roughly [0...5]
    //
    const bool isLiveAcrossCall = cse->csdLiveAcrossCall;

    features[6] = booleanScale * isLiveAcrossCall;
    features[7] = booleanScale * varTypeUsesIntReg(cse->csdTree->TypeGet());

    const bool isConstant       = cse->csdTree->OperIsConst();
    const bool isSharedConstant = cse->csdIsSharedConst;

    features[8] = booleanScale * (isConstant & !isSharedConstant);
    features[9] = booleanScale * isSharedConstant;

    const bool isMinCost = (costEx == Compiler::MIN_CSE_COST);
    const bool isLowCost = (costEx <= Compiler::MIN_CSE_COST + 1);

    features[10] = booleanScale * isMinCost;

    // Joint features: constant / low cost CSEs seem to require extra scrutiny
    //
    features[11] = booleanScale * (isConstant & isLiveAcrossCall);
    features[12] = booleanScale * (isConstant & isMinCost);
    features[13] = booleanScale * (isMinCost & isLiveAcrossCall);

    // Is any CSE tree for this candidate marked GTF_MAKE_CSE (hoisting)
    // Also gather data for "distance" metric.
    //
    const unsigned numBBs            = m_pCompiler->fgBBcount;
    bool           isMakeCse         = false;
    unsigned       minPostorderNum   = numBBs;
    unsigned       maxPostorderNum   = 0;
    BasicBlock*    minPostorderBlock = nullptr;
    BasicBlock*    maxPostorderBlock = nullptr;
    for (treeStmtLst* treeList = cse->csdTreeList; treeList != nullptr && !isMakeCse; treeList = treeList->tslNext)
    {
        BasicBlock* const treeBlock    = treeList->tslBlock;
        unsigned          postorderNum = treeBlock->bbPostorderNum;
        if (postorderNum < minPostorderNum)
        {
            minPostorderNum   = postorderNum;
            minPostorderBlock = treeBlock;
        }

        if (postorderNum > maxPostorderNum)
        {
            maxPostorderNum   = postorderNum;
            maxPostorderBlock = treeBlock;
        }

        isMakeCse |= ((treeList->tslTree->gtFlags & GTF_MAKE_CSE) != 0);
    }
    const unsigned blockSpread = maxPostorderNum - minPostorderNum;

    features[14] = booleanScale * isMakeCse;

    // Locals data
    //
    features[15] = cse->numDistinctLocals;
    features[16] = cse->numLocalOccurrences;

    // More
    //
    features[17] = booleanScale * ((cse->csdTree->gtFlags & GTF_CALL) != 0);
    features[18] = deMinimusAdj + log(max(deMinimis, cse->csdUseCount * cse->csdUseWtCnt));
    features[19] = deMinimusAdj + log(max(deMinimis, cse->numLocalOccurrences * cse->csdUseWtCnt));
    features[20] = booleanScale * ((double)(blockSpread) / numBBs);

    const bool isContainable = cse->csdTree->OperIs(GT_ADD, GT_NOT, GT_MUL, GT_LSH);
    features[21]             = booleanScale * isContainable;
    features[22]             = booleanScale * (isContainable && isLowCost);

    // LSRA "is live across call"
    //
    bool isLiveAcrossCallLSRA = isLiveAcrossCall;

    if (!isLiveAcrossCallLSRA)
    {
        unsigned count = 0;
        for (BasicBlock *block                                                            = minPostorderBlock;
             block != nullptr && block != maxPostorderBlock && count < blockSpread; block = block->Next(), count++)
        {
            if (block->HasFlag(BBF_HAS_CALL))
            {
                isLiveAcrossCallLSRA = true;
                break;
            }
        }
    }

    features[23] = booleanScale * isLiveAcrossCallLSRA;
}

//------------------------------------------------------------------------
// GetStoppingFeatures: extract features for stopping CSE
//
// Arguments:
//    features - array to fill in with feature values
//
// Notes:
//
// Stopping features
//
//   24. int register pressure weight estimate (log)
//
// All boolean features are scaled up by booleanScale so their
// numeric range is similar to the non-boolean features
//
void CSE_HeuristicRL::GetStoppingFeatures(double* features)
{
    // Estimate the (log) weight at which a new CSE would cause a spill
    // if m_registerPressure registers were initially available.
    //
    // Todo (perhaps) also adjust weight distribution as we perform CSEs
    //
    //  "remove" weight per local use occurrences * weightUses
    //  "add" weight of the CSE temp times * (weigh defs*2) + weightUses
    //
    const double deMinimis     = 1e-3;
    double       spillAtWeight = deMinimis;
    const double deMinimusAdj  = -log(deMinimis);

    // Assume each already performed cse is occupying a registger
    //
    unsigned currentPressure = m_registerPressure;

    if (currentPressure > m_addCSEcount)
    {
        currentPressure -= m_addCSEcount;
    }
    else
    {
        currentPressure = 0;
    }

    if (currentPressure < m_localWeights->size())
    {
        spillAtWeight = (*m_localWeights)[currentPressure];
    }

    JITDUMP("Pressure count %u, pressure weight " FMT_WT "\n", currentPressure, spillAtWeight);

    // Large frame...?
    //  todo: scan all vars, not just tracked?
    //

    features[24] = deMinimusAdj + log(max(deMinimis, spillAtWeight));
}

//------------------------------------------------------------------------
// DumpFeatures: dump feature values for a CSE candidate
//
// Arguments:
//    dsc - cse descriptor
//    features - feature vector for that candidate
//
// Notes:
//    Dumps a comma separated row of data, prefixed by method index.
//
void CSE_HeuristicRL::DumpFeatures(CSEdsc* dsc, double* features)
{
    printf("features,%d," FMT_CSE, m_pCompiler->info.compMethodSuperPMIIndex, dsc == nullptr ? 0 : dsc->csdIndex);
    for (int i = 0; i < numParameters; i++)
    {
        printf(",%f", features[i]);
    }
    printf("\n");
}

//------------------------------------------------------------------------
// Preference: determine a preference score for this CSE
//
// Arguments:
//    cse - cse descriptor, or nullptr for the option to stop doing CSEs.
//
double CSE_HeuristicRL::Preference(CSEdsc* cse)
{
    double features[numParameters];
    GetFeatures(cse, features);

    if (JitConfig.JitRLCSECandidateFeatures() > 0)
    {
        DumpFeatures(cse, features);
    }

    double preference = 0;
    for (int i = 0; i < numParameters; i++)
    {
        preference += features[i] * m_parameters[i];
    }

    return preference;
}

//------------------------------------------------------------------------
// StoppingPreference: determine a preference score for this stopping CSE
//
// Arguments:
//    regAvail - number of registers threshold
//
double CSE_HeuristicRL::StoppingPreference()
{
    double features[numParameters];
    GetFeatures(nullptr, features);

    if (JitConfig.JitRLCSECandidateFeatures() > 0)
    {
        DumpFeatures(nullptr, features);
    }

    double preference = 0;
    for (int i = 0; i < numParameters; i++)
    {
        preference += features[i] * m_parameters[i];
    }

    return preference;
}

//------------------------------------------------------------------------
// ChooseGreedy: examine candidates and choose the next CSE to perform
//   via greedy policy
//
// Returns:
//   Choice of CSE to perform
//
// Notes:
//   Picks the most-preferred candidate.
//   If there is a tie, picks stop, or the lowest cse index.
//
CSE_HeuristicRL::Choice& CSE_HeuristicRL::ChooseGreedy(ArrayStack<Choice>& choices)
{
    choices.Reset();
    BuildChoices(choices);

    // Find the maximally preferred case.
    //
    Choice& bestChoice = choices.TopRef(0);
    int     choiceNum  = 0;

    for (int i = 1; i < choices.Height(); i++)
    {
        Choice&      choice = choices.TopRef(i);
        const double delta  = choice.m_preference - bestChoice.m_preference;

        bool update = false;

        if (delta > 0)
        {
            update = true;
        }
        else if (delta == 0)
        {
            if (choice.m_dsc == nullptr)
            {
                update = true;
            }
            else if ((bestChoice.m_dsc != nullptr) && (choice.m_dsc->csdIndex < bestChoice.m_dsc->csdIndex))
            {
                update = true;
            }
        }

        if (update)
        {
            bestChoice = choice;
            choiceNum  = i;
        }
    }

    if (m_verbose)
    {
        printf("Greedy candidate evaluation\n");
        DumpChoices(choices, choiceNum);
    }

    return bestChoice;
}

//------------------------------------------------------------------------
// ChooseSoftmax: examine candidates and choose the next CSE to perform
//   via softmax
//
// Returns:
//   Choice of CSE to perform
//
// Notes:
//   This is a softmax policy, meaning that there is some randomness
//   associated with choices it makes.
//
//   Each candidate is given a preference score; these are converted into
//   "spans" in the [0..1] range via softmax, and then a random value
//   is generated in [0..1] and we choose the candidate whose range contains
//   this value.
//
//   For example if there are 3 candidates with scores 1,0, 2.0, and 0.3,
//   the softmax sum is e^1.0 + e^2.0 + e^0.3 = 2.78 + 7.39 + 1.35 = 11.52,
//   and so the spans are 0.24, 0.64, 0.12 (note they sum to 1.0).
//
//   So if the random value is in [0.00, 0.24) we choose candidate 1;
//      if the random value is in [0.24, 0.88) we choose candidate 2;
//      else we choose candidate 3;
//
CSE_HeuristicRL::Choice& CSE_HeuristicRL::ChooseSoftmax(ArrayStack<Choice>& choices)
{
    choices.Reset();
    BuildChoices(choices);

    // Compute softmax likelihoods
    //
    Softmax(choices);

    // Generate a random number and choose the CSE to perform.
    //
    double randomFactor = m_cseRNG.NextDouble();
    double softmaxSum   = 0;
    int    choiceNum    = 0;
    for (int i = 0; i < choices.Height(); i++)
    {
        softmaxSum += choices.TopRef(i).m_softmax;

        if (randomFactor < softmaxSum)
        {
            choiceNum = i;
            break;
        }
    }

    if (m_verbose)
    {
        printf("Current candidate evaluation, rng is %f\n", randomFactor);
        DumpChoices(choices, choiceNum);
    }

    return choices.TopRef(choiceNum);
}

//------------------------------------------------------------------------
// BuildChoices: fill in the choices currently available
//
//   choices - array of choices to be filled in
//
// Notes:
//    Also computes the preference for each choice.
//
void CSE_HeuristicRL::BuildChoices(ArrayStack<Choice>& choices)
{
    for (unsigned i = 0; i < m_pCompiler->optCSECandidateCount; i++)
    {
        CSEdsc* const dsc = sortTab[i];
        if ((dsc == nullptr) || !dsc->IsViable())
        {
            // already did this cse,
            // or the cse is not viable
            continue;
        }

        double preference = Preference(dsc);
        choices.Emplace(dsc, preference);
    }

    // Doing nothing is also an option.
    //
    const double stoppingPreference = StoppingPreference();
    choices.Emplace(nullptr, stoppingPreference);
}

//------------------------------------------------------------------------
// Softmax: fill in likelihoods for each choice vis softmax
//
// Arguments:
//   choices - array of choices
//
// Notes:
//
//   Each choice has already been given a preference score.
//   These are converted into likelihoods in the [0..1] range via softmax,
//   where the sum across all choices is 1.0.
//
//   For each choice i, softmax(i) = e^preference(i) / sum_k (e^preference(k))
//
//   For example if there are 3 choices with preferences 1,0, 2.0, and 0.3,
//   the softmax sum is e^1.0 + e^2.0 + e^0.3 = 2.78 + 7.39 + 1.35 = 11.52,
//   and so the likelihoods are 0.24, 0.64, 0.12 (note they sum to 1.0).
//
void CSE_HeuristicRL::Softmax(ArrayStack<Choice>& choices)
{
    // Determine likelihood via softmax.
    //
    double softmaxSum = 0;
    for (int i = 0; i < choices.Height(); i++)
    {
        double softmax              = exp(choices.TopRef(i).m_preference);
        choices.TopRef(i).m_softmax = softmax;
        softmaxSum += softmax;
    }

    // Normalize each choice's softmax likelihood
    //
    for (int i = 0; i < choices.Height(); i++)
    {
        choices.TopRef(i).m_softmax /= softmaxSum;
    }
}

//------------------------------------------------------------------------
// DumpChoices: dump out information on current choices
//
// Arguments:
//   choices - array of choices
//   highlight - highlight this choice
//
void CSE_HeuristicRL::DumpChoices(ArrayStack<Choice>& choices, int highlight)
{
    for (int i = 0; i < choices.Height(); i++)
    {
        Choice&       choice = choices.TopRef(i);
        CSEdsc* const cse    = choice.m_dsc;
        const char*   msg    = i == highlight ? "=>" : "  ";
        if (cse != nullptr)
        {
            printf("%s%2d: " FMT_CSE " preference %10.7f likelihood %10.7f\n", msg, i, cse->csdIndex,
                   choice.m_preference, choice.m_softmax);
        }
        else
        {
            printf("%s%2d: QUIT    preference %10.7f likelihood %10.7f\n", msg, i, choice.m_preference,
                   choice.m_softmax);
        }
    }
}

//------------------------------------------------------------------------
// DumpChoices: dump out information on current choices
//
// Arguments:
//   choices - array of choices
//   highlight - highlight this choice
//
void CSE_HeuristicRL::DumpChoices(ArrayStack<Choice>& choices, CSEdsc* highlight)
{
    for (int i = 0; i < choices.Height(); i++)
    {
        Choice&       choice = choices.TopRef(i);
        CSEdsc* const cse    = choice.m_dsc;
        const char*   msg    = cse == highlight ? "=>" : "  ";
        if (cse != nullptr)
        {
            printf("%s%2d: " FMT_CSE " preference %10.7f likelihood %10.7f\n", msg, i, cse->csdIndex,
                   choice.m_preference, choice.m_softmax);
        }
        else
        {
            printf("%s%2d: QUIT    preference %10.7f likelihood %10.7f\n", msg, i, choice.m_preference,
                   choice.m_softmax);
        }
    }
}

//------------------------------------------------------------------------
// UpdateParameters: Replay an existing CSE sequence with known reward,
//   and update the model parameters via the policy gradient.
//
void CSE_HeuristicRL::UpdateParameters()
{
    const unsigned n = m_pCompiler->optCSECandidateCount;

    if (n == 0)
    {
        // No candidates, nothing to update.
        return;
    }

    ArrayStack<Choice>    choices(m_pCompiler->getAllocator(CMK_CSE));
    static ConfigIntArray JitReplayCSEArray;
    JitReplayCSEArray.EnsureInit(JitConfig.JitReplayCSE());

    // We have an undiscounted reward, so it applies equally
    // to all steps in the computation.
    //
    if (m_verbose)
    {
        printf("Updating parameters with sequence ");
        JitReplayCSEArray.Dump();
        printf(" alpha " FMT_WT " and rewards ", m_alpha);
        for (unsigned int i = 0; i < JitReplayCSEArray.GetLength(); i++)
        {
            printf("%s%7.4f", (i == 0 ? "" : ","), m_rewards[i]);
        }
        printf("\n");
    }

    // We need to evaluate likelihoods based on the current parameters
    // so we save up the accumulated upates here.
    double parameterDelta[numParameters];
    for (int i = 0; i < numParameters; i++)
    {
        parameterDelta[i] = 0;
    }

    const unsigned nSteps = JitReplayCSEArray.GetLength();
    unsigned       i      = 0;

    for (; i < nSteps; i++)
    {
        const int candNumber = JitReplayCSEArray.GetData()[i];

        // CSE "0" means stop.
        //
        if (candNumber == 0)
        {
            break;
        }

        // optCSEtab is 0-based; candidate numbers are 1-based
        //
        const int index = candNumber - 1;

        if ((index < 0) || (index >= (int)n))
        {
            JITDUMP("Invalid candidate number %d\n", index + 1);
            continue;
        }

        // Re-evaluate the available options.
        //
        choices.Reset();
        BuildChoices(choices);
        Softmax(choices);

        const int     attempt = m_pCompiler->optCSEattempt++;
        CSEdsc* const dsc     = sortTab[index];

        // purge this CSE so we don't consider it again when
        // building choices
        //
        assert(sortTab[dsc->csdIndex - 1] == dsc);
        sortTab[dsc->csdIndex - 1] = nullptr;
        if (!dsc->IsViable())
        {
            // If we are replaying an off-policy sequence
            // it may contain non-viable candidates.
            // Ignore them.
            continue;
        }

        // We are actually going to do this CSE since
        // we want the state to evolve as it did originally
        //
        CSE_Candidate candidate(this, dsc);

        if (m_verbose)
        {
            printf("\nRL Update attempting " FMT_CSE "\n", candidate.CseIndex());
        }

        JITDUMP("CSE Expression : \n");
        JITDUMPEXEC(m_pCompiler->gtDispTree(candidate.Expr()));
        JITDUMP("\n");

        // Compute the parameter update impact from this step
        // and add it to the net delta.
        //
        UpdateParametersStep(dsc, choices, m_rewards[i], parameterDelta);

        // Actually do the cse, since subsequent step updates
        // possibly can observe changes to the method caused by this CSE.
        //
        PerformCSE(&candidate);
        madeChanges = true;
    }

    // If we did not exhaust all choices (we stopped early) we need one
    // last parameter update.
    //
    choices.Reset();
    BuildChoices(choices);

    // See if there are any non-
    // then there is an option left besides stopping.
    //
    int undoneCSEs = choices.Height() - 1;
    if (undoneCSEs > 0)
    {
        if (m_verbose)
        {
            printf("\nRL Update stopping early (%d CSEs done, %d CSEs left undone)\n", i, undoneCSEs);
        }

        Softmax(choices);
        // nullptr here means "stopping"
        UpdateParametersStep(nullptr, choices, m_rewards[i], parameterDelta);
    }

    // Update the parameters to include the computed delta
    //
    for (int i = 0; i < numParameters; i++)
    {
        m_parameters[i] += parameterDelta[i];
    }
}

//------------------------------------------------------------------------
// UpdateParametersStep: perform parameter update for this step in
//   the CSE sequence
//
// Arguments;
//   dsc -- cse to perform (nullptr if stopping)
//   choices -- alternatives available, with preference and softmax computed
//   reward -- reward for this step
//   delta -- accumulated change to the parameters (in, out)
//
// Notes:
//   modifies delta to include the adjustments due to this
//   choice, with indicated reward (higher better).
//
//   Takes into account both the likelihood of the choice and the magnitude
//   of reward, briefly:
//   - likely   choices and good rewards are strongly encouraged
//   - unlikely choices and good rewards are mildly   encouraged
//   - unlikely choices and bad  rewards are mildly   discouraged
//   - likely   choices and bad  rewards are strongly discouraged
//
void CSE_HeuristicRL::UpdateParametersStep(CSEdsc* dsc, ArrayStack<Choice>& choices, double reward, double* delta)
{
    // Since this is an "on-policy" process, the dsc
    // should be among the possible choices.
    //
    // Eventually (with a well-trained policy) the current choice will
    // be (one of) the strongly preferred choice(s), if this is an optimal sequence.
    //
    Choice* const currentChoice = FindChoice(dsc, choices);
    if (m_verbose)
    {
        DumpChoices(choices, dsc);
        printf("Reward: %7.4f\n", reward);
    }

    // Compute the parameter update...
    //
    double currentFeatures[numParameters];
    GetFeatures(dsc, currentFeatures);

    double adjustment[numParameters];
    for (int i = 0; i < numParameters; i++)
    {
        adjustment[i] = 0;
    }

    for (int c = 0; c < choices.Height(); c++)
    {
        double choiceFeatures[numParameters];
        GetFeatures(choices.TopRef(c).m_dsc, choiceFeatures);
        double softmax = choices.TopRef(c).m_softmax;

        for (int i = 0; i < numParameters; i++)
        {
            adjustment[i] += softmax * choiceFeatures[i];
        }
    }

    double gradient[numParameters];
    for (int i = 0; i < numParameters; i++)
    {
        gradient[i] = currentFeatures[i] - adjustment[i];
    }

    double newDelta[numParameters];
    for (int i = 0; i < numParameters; i++)
    {
        // Todo: discount?
        newDelta[i] = m_alpha * reward * gradient[i];
    }

    if (m_verbose)
    {
        printf("Feat   OldDelta     Feature  Adjustment    Gradient   StepDelta   NewDelta\n");

        for (int i = 0; i < numParameters; i++)
        {
            printf("%4d  %10.7f  %10.7f  %10.7f  %10.7f  %10.7f %10.7f\n", i, delta[i], currentFeatures[i],
                   adjustment[i], gradient[i], newDelta[i], newDelta[i] + delta[i]);
        }
    }

    for (int i = 0; i < numParameters; i++)
    {
        delta[i] += newDelta[i];
    }
}

//------------------------------------------------------------------------
// FindChoice: Find the choice info for a particular CSE.
//
// Arguments:
//   dsc -- cse to search for
//   choices -- choice array to search
//
// Returns:
//   indicated choice, or nullptr
//
CSE_HeuristicRL::Choice* CSE_HeuristicRL::FindChoice(CSEdsc* dsc, ArrayStack<Choice>& choices)
{
    for (int i = 0; i < choices.Height(); i++)
    {
        if (choices.TopRef(i).m_dsc == dsc)
        {
            return &choices.TopRef(i);
        }
    }
    return nullptr;
}

#endif // DEBUG

//------------------------------------------------------------------------
// CSE_Heuristic: construct standard CSE heuristic
//
// Arguments;
//  pCompiler - compiler instance
//
// Notes:
//  This creates the standard CSE heuristic.
//
CSE_Heuristic::CSE_Heuristic(Compiler* pCompiler) : CSE_HeuristicCommon(pCompiler)
{
    aggressiveRefCnt = 0;
    moderateRefCnt   = 0;
    enregCount       = 0;
    largeFrame       = false;
    hugeFrame        = false;
}

//------------------------------------------------------------------------
// ConsiderTree: check if this tree can be a CSE candidate
//
// Arguments:
//   tree - tree in question
//   isReturn - true if tree is part of a return statement
//
// Returns:
//    true if this tree can be a CSE candidate
//
bool CSE_Heuristic::ConsiderTree(GenTree* tree, bool isReturn)
{
    return CanConsiderTree(tree, isReturn);
}

//------------------------------------------------------------------------
// Initialize: initialize the standard CSE heuristic
//
// Notes:
// Perform the Initialization step for our CSE Heuristics. Determine the various cut off values to use for
// the aggressive, moderate and conservative CSE promotions. Count the number of enregisterable variables.
// Determine if the method has a large or huge stack frame.
//
void CSE_Heuristic::Initialize()
{
    // Record the weighted ref count of the last "for sure" callee saved LclVar

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
        if (varDsc->lvDoNotEnregister)
        {
            onStack = true;
        }

#ifdef TARGET_X86
        // Treat floating point and 64 bit integers as always on the stack
        if (varTypeIsFloating(varDsc->TypeGet()) || varTypeIsLong(varDsc->TypeGet()))
        {
            onStack = true;
        }
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
            // On XARCH stack frame displacements can either use a 1-byte or a 4-byte displacement.
            // With a large frame we will need to use some 4-byte displacements.
            //
            largeFrame = true;
            break; // early out, we don't need to keep increasing frameSize
        }
#elif defined(TARGET_ARM)
        if (frameSize > 0x0400)
        {
            // We likely have a large stack frame.
            //
            // Thus we might need to use large displacements when loading or storing
            // to CSE LclVars that are not enregistered.
            // On ARM32 this means using rsGetRsvdReg() to hold the large displacement
            largeFrame = true;
        }
        if (frameSize > 0x10000)
        {
            hugeFrame = true;
            break; // early out, we don't need to keep increasing frameSize
        }
#elif defined(TARGET_ARM64)
        if (frameSize > 0x1000)
        {
            // We likely have a large stack frame.
            //
            // Thus we might need to use large displacements when loading or storing
            // to CSE LclVars that are not enregistered.
            // On ARM64 this means using rsGetRsvdReg() or R21 to hold the large displacement
            //
            largeFrame = true;
            break; // early out, we don't need to keep increasing frameSize
        }
#elif defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
        if (frameSize > 0x7ff)
        {
            // We likely have a large stack frame.
            //
            // Thus we might need to use large displacements when loading or storing
            // to CSE LclVars that are not enregistered.
            // On LoongArch64 this means using rsGetRsvdReg() to hold the large displacement.
            //
            largeFrame = true;
            break; // early out, we don't need to keep increasing frameSize
        }
#endif
    }

    // Iterate over the sorted list of tracked local variables. These are the register candidates for LSRA.
    // We normally visit the LclVars in order of their weighted ref counts and our heuristic assumes that the
    // highest weighted ref count LclVars will be enregistered and that the lowest weighted ref count
    // are likely be allocated in the stack frame. The value of enregCount is incremented when we visit a LclVar
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
        if (varDsc->lvDoNotEnregister)
        {
            continue;
        }

        // enregCount only tracks the uses of integer registers.
        //
        // We could track floating point register usage separately
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
        // On Windows x64 this yields:
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
        printf("Aggressive CSE Promotion cutoff is %f\n", aggressiveRefCnt);
        printf("Moderate CSE Promotion cutoff is %f\n", moderateRefCnt);
        printf("enregCount is %u\n", enregCount);
        printf("Framesize estimate is 0x%04X\n", frameSize);
        printf("We have a %s frame\n", hugeFrame ? "huge" : (largeFrame ? "large" : "small"));
    }
#endif
}

//------------------------------------------------------------------------
// SortCandidates: standard heuristic candidate sort
//
// Notes:
//  Copies candidates to the sorted table, and then sorts (ranks) them from
//  most appealing to least appealing, based on heuristic criteria.
//
void CSE_Heuristic::SortCandidates()
{
    /* Create an expression table sorted by decreasing cost */
    sortTab = new (m_pCompiler, CMK_CSE) CSEdsc*[m_pCompiler->optCSECandidateCount];

    sortSiz = m_pCompiler->optCSECandidateCount * sizeof(*sortTab);
    memcpy(sortTab, m_pCompiler->optCSEtab, sortSiz);

    if (CodeOptKind() == Compiler::SMALL_CODE)
    {
        jitstd::sort(sortTab, sortTab + m_pCompiler->optCSECandidateCount, Compiler::optCSEcostCmpSz());
    }
    else
    {
        jitstd::sort(sortTab, sortTab + m_pCompiler->optCSECandidateCount, Compiler::optCSEcostCmpEx());
    }

#ifdef DEBUG
    if (m_pCompiler->verbose)
    {
        printf("\nSorted CSE candidates:\n");
        /* Print out the CSE candidates */
        for (unsigned cnt = 0; cnt < m_pCompiler->optCSECandidateCount; cnt++)
        {
            CSEdsc*  dsc  = sortTab[cnt];
            GenTree* expr = dsc->csdTree;

            weight_t def;
            weight_t use;
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
                printf(FMT_CSE ", {$%-3x, $%-3x} useCnt=%d: [def=%3f, use=%3f, cost=%3u%s]\n        :: ", dsc->csdIndex,
                       dsc->csdHashKey, dsc->defExcSetPromise, dsc->csdUseCount, def, use, cost,
                       dsc->csdLiveAcrossCall ? ", call" : "      ");
            }
            else
            {
                size_t kVal = Compiler::Decode_Shared_Const_CSE_Value(dsc->csdHashKey);
                printf(FMT_CSE ", {K_%p} useCnt=%d: [def=%3f, use=%3f, cost=%3u%s]\n        :: ", dsc->csdIndex,
                       dspPtr(kVal), dsc->csdUseCount, def, use, cost, dsc->csdLiveAcrossCall ? ", call" : "      ");
            }

            m_pCompiler->gtDispTree(expr, nullptr, nullptr, true);
        }
        printf("\n");
    }
#endif // DEBUG
}

//------------------------------------------------------------------------
// PromotionCheck: decide whether to perform this CSE
//
// Arguments:
//   candidate - cse candidate to consider
//
// Return Value:
//   true if the CSE should be performed
//
bool CSE_Heuristic::PromotionCheck(CSE_Candidate* candidate)
{
    bool result = false;

#ifdef DEBUG
    if (m_pCompiler->optConfigDisableCSE2())
    {
        return false; // skip this CSE
    }
#endif

    /*
      Our calculation is based on the following cost estimate formula

      Existing costs are:

      (def + use) * cost

      If we introduce a CSE temp at each definition and
      replace each use with a CSE temp then our cost is:

      (def * (cost + cse-def-cost)) + (use * cse-use-cost)

      We must estimate the values to use for cse-def-cost and cse-use-cost

      If we are able to enregister the CSE then the cse-use-cost is one
      and cse-def-cost is either zero or one.  Zero in the case where
      we needed to evaluate the def into a register and we can use that
      register as the CSE temp as well.

      If we are unable to enregister the CSE then the cse-use-cost is IND_COST
      and the cse-def-cost is also IND_COST.

      If we want to be conservative we use IND_COST as the value
      for both cse-def-cost and cse-use-cost and then we never introduce
      a CSE that could pessimize the execution time of the method.

      If we want to be more moderate we use (IND_COST_EX + 1) / 2 as the
      values for both cse-def-cost and cse-use-cost.

      If we want to be aggressive we use 1 as the values for both
      cse-def-cost and cse-use-cost.

      If we believe that the CSE is very valuable in terms of weighted ref counts
      such that it would always be enregistered by the register allocator we choose
      the aggressive use def costs.

      If we believe that the CSE is somewhat valuable in terms of weighted ref counts
      such that it could be likely be enregistered by the register allocator we choose
      the moderate use def costs.

      Otherwise we choose the conservative use def costs.

    */

    unsigned cse_def_cost;
    unsigned cse_use_cost;

    weight_t no_cse_cost    = 0;
    weight_t yes_cse_cost   = 0;
    unsigned extra_yes_cost = 0;
    unsigned extra_no_cost  = 0;

    // The 'cseRefCnt' is the RefCnt that we will have if we promote this CSE into a new LclVar
    // Each CSE Def will contain two Refs and each CSE Use will have one Ref of this new LclVar
    weight_t cseRefCnt = (candidate->DefCount() * 2) + candidate->UseCount();

    bool     canEnregister = true;
    unsigned slotCount     = 1;
    if (candidate->Expr()->TypeIs(TYP_STRUCT))
    {
        // This is a non-enregisterable struct.
        canEnregister = false;
        unsigned size = candidate->Expr()->GetLayout(m_pCompiler)->GetSize();
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
                printf("Aggressive CSE Promotion (%f >= %f)\n", cseRefCnt, aggressiveRefCnt);
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
#else                             // TARGET_ARM
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
                printf("Aggressive CSE Promotion (%f >= %f)\n", cseRefCnt, aggressiveRefCnt);
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
                    printf("Moderate CSE Promotion (CSE never live at call) (%f >= %f)\n", cseRefCnt, moderateRefCnt);
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
                    printf("Moderate CSE Promotion (%s) (%f >= %f)\n",
                           candidate->LiveAcrossCall() ? "CSE is live across a call" : "not enregisterable", cseRefCnt,
                           moderateRefCnt);
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
                    printf("Conservative CSE Promotion (%s) (%f < %f)\n",
                           candidate->LiveAcrossCall() ? "CSE is live across a call" : "not enregisterable", cseRefCnt,
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
                    printf("Conservative CSE Promotion (%f < %f)\n", cseRefCnt, moderateRefCnt);
                }
#endif
                cse_def_cost = 2;
                cse_use_cost = 3;
            }

            // If we have maxed out lvaTrackedCount then this CSE may end up as an untracked variable
            if (m_pCompiler->lvaTrackedCount == (unsigned)JitConfig.JitMaxLocalsToTrack())
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

    // If this CSE is live across a call then we may have additional costs
    //
    if (candidate->LiveAcrossCall())
    {
        // If we have a floating-point CSE that is both live across a call and there
        // are no callee-saved FP registers available, the RA will have to spill at
        // the def site and reload at the (first) use site, if the variable is a register
        // candidate. Account for that.
        if (varTypeIsFloating(candidate->Expr()) && (CNT_CALLEE_SAVED_FLOAT == 0) && !candidate->IsConservative())
        {
            cse_def_cost += 1;
            cse_use_cost += 1;
        }

        // If we don't have a lot of variables to enregister or we have a floating point type
        // then we will likely need to spill an additional caller save register.
        //
        if ((enregCount < (CNT_CALLEE_ENREG * 3 / 2)) || varTypeIsFloating(candidate->Expr()))
        {
            // Extra cost in case we have to spill/restore a caller saved register
            extra_yes_cost = BB_UNITY_WEIGHT_UNSIGNED;

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

#if defined(TARGET_XARCH)
            // If we have a SIMD32/64 that is live across a call we have even higher spill costs
            //
            if (candidate->Expr()->TypeIs(TYP_SIMD32, TYP_SIMD64))
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
#endif // TARGET_XARCH

            extra_yes_cost = (BB_UNITY_WEIGHT_UNSIGNED * spillSimdRegInProlog) * 3;
        }
#endif // FEATURE_SIMD
    }

    // estimate the cost from lost codesize reduction if we do not perform the CSE
    if (candidate->Size() > cse_use_cost)
    {
        CSEdsc* dsc = candidate->CseDsc(); // We need to retrieve the actual use count, not the
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
        printf("cseRefCnt=%f, aggressiveRefCnt=%f, moderateRefCnt=%f\n", cseRefCnt, aggressiveRefCnt, moderateRefCnt);
        printf("defCnt=%f, useCnt=%f, cost=%d, size=%d%s\n", candidate->DefCount(), candidate->UseCount(),
               candidate->Cost(), candidate->Size(), candidate->LiveAcrossCall() ? ", LiveAcrossCall" : "");
        printf("def_cost=%d, use_cost=%d, extra_no_cost=%d, extra_yes_cost=%d\n", cse_def_cost, cse_use_cost,
               extra_no_cost, extra_yes_cost);

        printf("CSE cost savings check (%f >= %f) %s\n", no_cse_cost, yes_cse_cost,
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
            int percentage = (int)((no_cse_cost * 100) / yes_cse_cost);

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
bool CSE_HeuristicCommon::IsCompatibleType(var_types cseLclVarTyp, var_types expTyp)
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
void CSE_HeuristicCommon::PerformCSE(CSE_Candidate* successfulCandidate)
{
    AdjustHeuristic(successfulCandidate);

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
    else if (successfulCandidate->IsRandom())
    {
        grabTempMessage = "CSE - random";
    }
#endif // DEBUG

    /* Introduce a new temp for the CSE */

    // we will create a  long lifetime temp for the new CSE LclVar
    unsigned  cseLclVarNum = m_pCompiler->lvaGrabTemp(false DEBUGARG(grabTempMessage));
    var_types cseLclVarTyp = genActualType(successfulCandidate->Expr()->TypeGet());

    LclVarDsc* lclDsc = m_pCompiler->lvaGetDesc(cseLclVarNum);
    if (cseLclVarTyp == TYP_STRUCT)
    {
        m_pCompiler->lvaSetStruct(cseLclVarNum, successfulCandidate->Expr()->GetLayout(m_pCompiler), false);
    }
    lclDsc->lvType  = cseLclVarTyp;
    lclDsc->lvIsCSE = true;

    // Record that we created a new LclVar for use as a CSE temp
    m_addCSEcount++;
    m_pCompiler->optCSEcount++;

    //  Walk all references to this CSE, adding an assignment
    //  to the CSE temp to all defs and changing all refs to
    //  a simple use of the CSE temp.
    //
    //  Later we will unmark any nested CSE's for the CSE uses.
    //
    CSEdsc* dsc = successfulCandidate->CseDsc();
    INDEBUG(m_sequence->push_back(dsc->csdIndex));

    // If there's just a single def for the CSE, we'll put this
    // CSE into SSA form on the fly. We won't need any PHIs.
    unsigned      cseSsaNum = SsaConfig::RESERVED_SSA_NUM;
    LclSsaVarDsc* ssaVarDsc = nullptr;

    if (dsc->csdDefCount == 1)
    {
        JITDUMP(FMT_CSE " is single-def, so associated CSE temp V%02u will be in SSA\n", dsc->csdIndex, cseLclVarNum);
        lclDsc->lvInSsa = true;

        // Allocate the ssa num
        CompAllocator allocator = m_pCompiler->getAllocator(CMK_SSA);
        cseSsaNum               = lclDsc->lvPerSsaData.AllocSsaNum(allocator);
        ssaVarDsc               = lclDsc->GetPerSsaData(cseSsaNum);
    }
    else
    {
        INDEBUG(lclDsc->lvIsMultiDefCSE = 1);
    }

    // Verify that all of the ValueNumbers in this list are correct as
    // Morph will change them when it performs a mutating operation.
    //
    bool         setRefCnt      = true;
    bool         allSame        = true;
    bool         isSharedConst  = successfulCandidate->IsSharedConst();
    ValueNum     bestVN         = ValueNumStore::NoVN;
    bool         bestIsDef      = false;
    ssize_t      bestConstValue = 0;
    treeStmtLst* lst            = dsc->csdTreeList;

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

                // The ARM addressing modes allow for a subtraction of up to 255
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

            BasicBlock* blk       = lst->tslBlock;
            weight_t    curWeight = blk->getBBWeight(m_pCompiler);

            if (setRefCnt)
            {
                lclDsc->setLvRefCnt(1);
                lclDsc->setLvRefCntWtd(curWeight);
                setRefCnt = false;
            }
            else
            {
                lclDsc->incRefCnts(curWeight, m_pCompiler);
            }

            // A CSE Def references the LclVar twice
            //
            if (isDef)
            {
                lclDsc->incRefCnts(curWeight, m_pCompiler);
                INDEBUG(lclDsc->lvIsHoist |= ((lst->tslTree->gtFlags & GTF_MAKE_CSE) != 0));
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
                        printf("[%06d](%s " FMT_VN ") ", m_pCompiler->dspTreeID(lst->tslTree),
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
        GenTree* const    exp  = lst->tslTree;
        Statement* const  stmt = lst->tslStmt;
        BasicBlock* const blk  = lst->tslBlock;

        /* Advance to the next node in the list */
        lst = lst->tslNext;

        // We may have cleared this CSE in optValuenumCSE_Availability
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
        GenTree* cse = nullptr;
        bool     isDef;

        if (IS_CSE_USE(exp->gtCSEnum))
        {
            /* This is a use of the CSE */
            isDef = false;
#ifdef DEBUG
            if (m_pCompiler->verbose)
            {
                printf("\nWorking on the replacement of the " FMT_CSE " use at ", exp->gtCSEnum);
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

            // If this local is in ssa, notify ssa there's a new use.
            if (ssaVarDsc != nullptr)
            {
                ssaVarDsc->AddUse(blk);
            }

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
                ValueNumPair sideEffExcSet        = vnStore->VNPExceptionSet(sideEffList->gtVNPair);
                ValueNumPair cseWithSideEffVNPair = vnStore->VNPWithExc(cse->gtVNPair, sideEffExcSet);

                // Create a comma node with the sideEffList as op1
                cse           = m_pCompiler->gtNewOperNode(GT_COMMA, expTyp, sideEffList, cse);
                cse->gtVNPair = cseWithSideEffVNPair;
            }
        }
        else
        {
            /* This is a def of the CSE */
            isDef = true;
#ifdef DEBUG
            if (m_pCompiler->verbose)
            {
                printf("\n" FMT_CSE " def at ", GET_CSE_INDEX(exp->gtCSEnum));
                Compiler::printTreeID(exp);
                printf(" replaced in " FMT_BB " with def of V%02u\n", blk->bbNum, cseLclVarNum);
            }
#endif // DEBUG

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

            /* Create a store of the value to the temp */
            GenTree* store     = m_pCompiler->gtNewTempStore(cseLclVarNum, val);
            GenTree* origStore = store;

            if (!store->OperIs(GT_STORE_LCL_VAR))
            {
                // This can only be the case for a struct in which the 'val' was a COMMA, so
                // the assignment is sunk below it.
                store = store->gtEffectiveVal();
                noway_assert(origStore->OperIs(GT_COMMA) && (origStore == val));
            }
            else
            {
                noway_assert(store->Data() == val);
            }

            // Assign the proper Value Numbers.
            store->gtVNPair = ValueNumStore::VNPForVoid(); // The store node itself is $VN.Void.
            noway_assert(store->OperIs(GT_STORE_LCL_VAR));

            // Backpatch the SSA def, if we're putting this CSE temp into ssa.
            store->AsLclVar()->SetSsaNum(cseSsaNum);

            // Move the information about the CSE def to the store; it now indicates a completed
            // CSE def instead of just a candidate. optCSE_canSwap uses this information to reason
            // about evaluation order in between substitutions of CSE defs/uses.
            store->gtCSEnum = exp->gtCSEnum;
            exp->gtCSEnum   = NO_CSE;

            if (cseSsaNum != SsaConfig::RESERVED_SSA_NUM)
            {
                LclSsaVarDsc* ssaVarDsc = m_pCompiler->lvaTable[cseLclVarNum].GetPerSsaData(cseSsaNum);

                // These should not have been set yet, since this is the first and
                // only def for this CSE.
                assert(ssaVarDsc->GetBlock() == nullptr);
                assert(ssaVarDsc->GetDefNode() == nullptr);

                ssaVarDsc->m_vnPair = val->gtVNPair;
                ssaVarDsc->SetBlock(blk);
                ssaVarDsc->SetDefNode(store->AsLclVarCommon());
            }

            /* Create a reference to the CSE temp */
            GenTree* cseLclVar = m_pCompiler->gtNewLclvNode(cseLclVarNum, cseLclVarTyp);
            cseLclVar->gtVNPair.SetBoth(dsc->csdConstDefVN);

            // Assign the ssa num for the lclvar use. Note it may be the reserved num.
            cseLclVar->AsLclVarCommon()->SetSsaNum(cseSsaNum);

            // If this local is in ssa, notify ssa there's a new use.
            if (ssaVarDsc != nullptr)
            {
                ssaVarDsc->AddUse(blk);
            }

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
            cseUse->gtVNPair = exp->gtVNPair; // The 'cseUse' is equal to the original expression.

            /* Create a comma node for the CSE assignment */
            cse           = m_pCompiler->gtNewOperNode(GT_COMMA, expTyp, origStore, cseUse);
            cse->gtVNPair = cseUse->gtVNPair; // The comma's value is the same as 'val'
            // as the assignment to the CSE LclVar
            // cannot add any new exceptions
        }

        cse->CopyReg(exp);  // The cse inheirits any reg num property from the original exp node
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

        assert(m_pCompiler->fgRemoveRestOfBlock == false);

        /* re-morph the statement */
        m_pCompiler->fgMorphBlockStmt(blk, stmt DEBUGARG("optValnumCSE"));

    } while (lst != nullptr);
}

void CSE_Heuristic::AdjustHeuristic(CSE_Candidate* successfulCandidate)
{
    weight_t cseRefCnt = (successfulCandidate->DefCount() * 2) + successfulCandidate->UseCount();

    // FACTOR THIS
    if (successfulCandidate->LiveAcrossCall() != 0)
    {
        // As we introduce new LclVars for these CSE we slightly
        // increase the cutoffs for aggressive and moderate CSE's
        //
        weight_t incr = BB_UNITY_WEIGHT;

        if (cseRefCnt > aggressiveRefCnt)
        {
            aggressiveRefCnt += incr;
        }

        if (cseRefCnt > moderateRefCnt)
        {
            moderateRefCnt += (incr / 2);
        }
    }
}

//------------------------------------------------------------------------
// ConsiderCandidates: examine candidates and perform CSEs.
//
// Notes:
//   Consider each of the CSE candidates and if the CSE passes
//   the PromotionCheck then transform the CSE by calling PerformCSE
//
void CSE_HeuristicCommon::ConsiderCandidates()
{
    /* Consider each CSE candidate, in order of decreasing cost */
    unsigned cnt = m_pCompiler->optCSECandidateCount;
    CSEdsc** ptr = sortTab;
    for (; (cnt > 0); cnt--, ptr++)
    {
        const int     attempt = m_pCompiler->optCSEattempt++;
        CSEdsc* const dsc     = *ptr;
        CSE_Candidate candidate(this, dsc);

        if (!dsc->IsViable())
        {
            continue;
        }

        candidate.InitializeCounts();

#ifdef DEBUG
        if (m_pCompiler->verbose)
        {
            if (!Compiler::Is_Shared_Const_CSE(dsc->csdHashKey))
            {
                printf("\nConsidering " FMT_CSE " {$%-3x, $%-3x} [def=%3f, use=%3f, cost=%3u%s]\n",
                       candidate.CseIndex(), dsc->csdHashKey, dsc->defExcSetPromise, candidate.DefCount(),
                       candidate.UseCount(), candidate.Cost(), dsc->csdLiveAcrossCall ? ", call" : "      ");
            }
            else
            {
                size_t kVal = Compiler::Decode_Shared_Const_CSE_Value(dsc->csdHashKey);
                printf("\nConsidering " FMT_CSE " {K_%p} [def=%3f, use=%3f, cost=%3u%s]\n", candidate.CseIndex(),
                       dspPtr(kVal), candidate.DefCount(), candidate.UseCount(), candidate.Cost(),
                       dsc->csdLiveAcrossCall ? ", call" : "      ");
            }
            printf("CSE Expression : \n");
            m_pCompiler->gtDispTree(candidate.Expr());
            printf("\n");
        }
#endif // DEBUG

        bool doCSE = PromotionCheck(&candidate);

#ifdef DEBUG

        const int hash = JitConfig.JitCSEHash();

        if ((hash == 0) || (m_pCompiler->info.compMethodHash() == (unsigned)hash))
        {
            // We can only mask the first 32 CSE attempts, so suppress anything beyond that.
            // Note methods with >= 32 CSEs are currently quite rare.
            //
            if (attempt >= 32)
            {
                doCSE = false;
                JITDUMP(FMT_CSE " attempt %u disabled, out of mask range\n", candidate.CseIndex(), attempt);
            }
            else
            {
                doCSE = ((1 << attempt) & ((unsigned)JitConfig.JitCSEMask())) != 0;
                JITDUMP(FMT_CSE " attempt %u mask 0x%08x: %s\n", candidate.CseIndex(), attempt, JitConfig.JitCSEMask(),
                        doCSE ? "allowed" : "disabled");
            }
        }

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
            madeChanges = true;
        }
    }
}

//------------------------------------------------------------------------
// optValnumCSE_Heuristic: Perform common sub-expression elimination
//    based on profitabiliy heuristic
//
// Arguments:
//    heurisic -- CSE heuristic to use
//
void Compiler::optValnumCSE_Heuristic(CSE_HeuristicCommon* heuristic)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("\n************ Trees at start of optValnumCSE_Heuristic()\n");
        fgDumpTrees(fgFirstBB, nullptr);
        printf("\n");
    }

    heuristic->Announce();
#endif // DEBUG

    heuristic->Initialize();
    heuristic->SortCandidates();
    heuristic->ConsiderCandidates();
    heuristic->Cleanup();
}

//------------------------------------------------------------------------
// optGetCSEheuristic: created or return the CSE heuristic for this method
//
// Returns:
//    The heuristic that will be used for CSE decisions.
//
CSE_HeuristicCommon* Compiler::optGetCSEheuristic()
{
    if (optCSEheuristic != nullptr)
    {
        return optCSEheuristic;
    }

#ifdef DEBUG

    // Enable optional policies
    //
    // RL takes precedence
    //
    if (optCSEheuristic == nullptr)
    {
        bool useRLHeuristic = (JitConfig.JitRLCSE() != nullptr);

        if (useRLHeuristic)
        {
            optCSEheuristic = new (this, CMK_CSE) CSE_HeuristicRL(this);
        }
    }

    // then Random
    //
    if (optCSEheuristic == nullptr)
    {
        bool useRandomHeuristic = false;

        if (JitConfig.JitRandomCSE() > 0)
        {
            JITDUMP("Using Random CSE heuristic (JitRandomCSE)\n");
            useRandomHeuristic = true;
        }
        else if (compStressCompile(Compiler::STRESS_MAKE_CSE, MAX_STRESS_WEIGHT))
        {
            JITDUMP("Using Random CSE heuristic (stress)\n");
            useRandomHeuristic = true;
        }

        if (useRandomHeuristic)
        {
            optCSEheuristic = new (this, CMK_CSE) CSE_HeuristicRandom(this);
        }
    }

    // then Replay
    //
    if (optCSEheuristic == nullptr)
    {
        bool useReplayHeuristic = (JitConfig.JitReplayCSE() != nullptr);

        if (useReplayHeuristic)
        {
            optCSEheuristic = new (this, CMK_CSE) CSE_HeuristicReplay(this);
        }
    }

#endif

    if (optCSEheuristic == nullptr)
    {
        JITDUMP("Using standard CSE heuristic\n");
        optCSEheuristic = new (this, CMK_CSE) CSE_Heuristic(this);
    }

    return optCSEheuristic;
}

//------------------------------------------------------------------------
// optOptimizeValnumCSEs: Perform common sub-expression elimination
//
// Returns:
//    Suitable phase status
//
PhaseStatus Compiler::optOptimizeValnumCSEs()
{
#ifdef DEBUG
    if (optConfigDisableCSE())
    {
        JITDUMP("Disabled by JitNoCSE\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }
#endif

    // Determine which heuristic to use...
    //
    CSE_HeuristicCommon* const heuristic = optGetCSEheuristic();

    optValnumCSE_phase = true;
    optCSEweight       = -1.0f;
    bool madeChanges   = false;

    optValnumCSE_Init();

    if (optValnumCSE_Locate(heuristic))
    {
        optValnumCSE_InitDataFlow();
        optValnumCSE_DataFlow();
        optValnumCSE_Availability();
        optValnumCSE_Heuristic(heuristic);
    }

    optValnumCSE_phase = false;

    return heuristic->MadeChanges() ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
}

//------------------------------------------------------------------------
// optIsCSEcandidate: Determine if this tree is a possible CSE candidate
//
// Arguments:
//   tree - tree in question
//   isReturn - true if this tree is part of a return statement.
//    If this is unknown then pass false (also the default value).
//
// Returns:
//   True if so
//
// Notes:
//   Useful to invoke upstream of CSE if you're trying to anticipate what
//   trees might be eligible for CSEs. A return value of false means the
//   tree will not be CSE'd; a return value of true means the tree might
//   be CSE'd.
//
//   Consults the CSE policy that will be used.
//
bool Compiler::optIsCSEcandidate(GenTree* tree, bool isReturn)
{
    return optGetCSEheuristic()->ConsiderTree(tree, isReturn);
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
    // Next check if DOTNET_JitNoCSE is set and applies to this method
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
    if (optCSEstart != BAD_VAR_NUM)
    {
        // CSE being run multiple times so we may need to clean up old
        // information.
        optCleanupCSEs();
    }

    optCSECandidateCount = 0;
    optCSEstart          = lvaCount;

    INDEBUG(optEnsureClearCSEInfo());
    optOptimizeValnumCSEs();
}

/*****************************************************************************
 *
 *  Cleanup after CSE to allow us to run more than once.
 */

void Compiler::optCleanupCSEs()
{
    for (BasicBlock* const block : Blocks())
    {
        // Walk the statement trees in this basic block.
        for (Statement* const stmt : block->NonPhiStatements())
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
    for (BasicBlock* const block : Blocks())
    {
        for (Statement* const stmt : block->NonPhiStatements())
        {
            for (GenTree* tree = stmt->GetRootNode(); tree; tree = tree->gtPrev)
            {
                assert(tree->gtCSEnum == NO_CSE);
            }
        }
    }
}

//------------------------------------------------------------------------
// optPrintCSEDataFlowSet: Print out one of the CSE dataflow sets bbCseGen, bbCseIn, bbCseOut,
// interpreting the bits in a more useful way for the dump.
//
// Arguments:
//    cseDataFlowSet - One of the dataflow sets to display
//    includeBits    - Display the actual bits of the set as well
//
void Compiler::optPrintCSEDataFlowSet(EXPSET_VALARG_TP cseDataFlowSet, bool includeBits /* = true */)
{
    if (includeBits)
    {
        printf("%s ", genES2str(cseLivenessTraits, cseDataFlowSet));
    }

    bool first = true;
    for (unsigned cseIndex = 1; cseIndex <= optCSECandidateCount; cseIndex++)
    {
        unsigned cseAvailBit          = getCSEAvailBit(cseIndex);
        unsigned cseAvailCrossCallBit = getCSEAvailCrossCallBit(cseIndex);

        if (BitVecOps::IsMember(cseLivenessTraits, cseDataFlowSet, cseAvailBit))
        {
            if (!first)
            {
                printf(", ");
            }
            const bool isAvailCrossCall = BitVecOps::IsMember(cseLivenessTraits, cseDataFlowSet, cseAvailCrossCallBit);
            printf(FMT_CSE "%s", cseIndex, isAvailCrossCall ? ".c" : "");
            first = false;
        }
    }
}

#endif // DEBUG
