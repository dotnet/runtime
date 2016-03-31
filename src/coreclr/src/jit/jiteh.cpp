// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                          Exception Handling                               XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/
#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                          "EHblkDsc" functions                             XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

/*****************************************************************************/

BasicBlock* EHblkDsc::BBFilterLast()
{
    noway_assert(HasFilter());
    noway_assert(ebdFilter != nullptr);
    noway_assert(ebdHndBeg != nullptr);

    // The last block of the filter is the block immediately preceding the first block of the handler.
    return ebdHndBeg->bbPrev;
}

BasicBlock* EHblkDsc::ExFlowBlock()
{
    if (HasFilter())
    {
        return ebdFilter;
    }
    else
    {
        return ebdHndBeg;
    }
}

bool EHblkDsc::InTryRegionILRange(BasicBlock* pBlk)
{
    // BBF_INTERNAL blocks may not have a valid bbCodeOffs. This function 
    // should only be used before any BBF_INTERNAL blocks have been added.
    assert(!(pBlk->bbFlags & BBF_INTERNAL));

    return Compiler::jitIsBetween(pBlk->bbCodeOffs, ebdTryBegOffs(), ebdTryEndOffs());
}

bool EHblkDsc::InFilterRegionILRange(BasicBlock* pBlk)
{
    // BBF_INTERNAL blocks may not have a valid bbCodeOffs. This function 
    // should only be used before any BBF_INTERNAL blocks have been added.
    assert(!(pBlk->bbFlags & BBF_INTERNAL));

    return HasFilter() &&
           Compiler::jitIsBetween(pBlk->bbCodeOffs, ebdFilterBegOffs(), ebdFilterEndOffs());
}

bool EHblkDsc::InHndRegionILRange(BasicBlock* pBlk)
{
    // BBF_INTERNAL blocks may not have a valid bbCodeOffs. This function 
    // should only be used before any BBF_INTERNAL blocks have been added.
    assert(!(pBlk->bbFlags & BBF_INTERNAL));

    return Compiler::jitIsBetween(pBlk->bbCodeOffs, ebdHndBegOffs(), ebdHndEndOffs());
}

// HasCatchHandler: returns 'true' for either try/catch, or try/filter/filter-handler.
bool EHblkDsc::HasCatchHandler()
{
    return (ebdHandlerType == EH_HANDLER_CATCH) ||
           (ebdHandlerType == EH_HANDLER_FILTER);
}

bool EHblkDsc::HasFilter()
{
    return ebdHandlerType == EH_HANDLER_FILTER;
}

bool EHblkDsc::HasFinallyHandler()
{
    return ebdHandlerType == EH_HANDLER_FINALLY;
}

bool EHblkDsc::HasFaultHandler()
{
    return ebdHandlerType == EH_HANDLER_FAULT;
}

bool EHblkDsc::HasFinallyOrFaultHandler()
{
    return HasFinallyHandler() || HasFaultHandler();
}

/*****************************************************************************
 * Returns true if pBlk is a block in the range [pStart..pEnd).
 * The check is inclusive of pStart, exclusive of pEnd.
 */

bool EHblkDsc::InBBRange(BasicBlock* pBlk, BasicBlock* pStart, BasicBlock* pEnd)
{
    for (BasicBlock* pWalk = pStart; pWalk != pEnd; pWalk = pWalk->bbNext)
    {
        if (pWalk == pBlk)
            return true;
    }
    return false;
}

bool EHblkDsc::InTryRegionBBRange(BasicBlock* pBlk)
{
    return InBBRange(pBlk, ebdTryBeg, ebdTryLast->bbNext);
}

bool EHblkDsc::InFilterRegionBBRange(BasicBlock* pBlk)
{
    return HasFilter() && InBBRange(pBlk, ebdFilter, ebdHndBeg);
}

bool EHblkDsc::InHndRegionBBRange(BasicBlock* pBlk)
{
    return InBBRange(pBlk, ebdHndBeg, ebdHndLast->bbNext);
}

unsigned EHblkDsc::ebdGetEnclosingRegionIndex(bool* inTryRegion)
{
    if ((ebdEnclosingTryIndex == NO_ENCLOSING_INDEX) && (ebdEnclosingHndIndex == NO_ENCLOSING_INDEX))
    {
        return NO_ENCLOSING_INDEX;
    }
    else if (ebdEnclosingTryIndex == NO_ENCLOSING_INDEX)
    {
        assert(ebdEnclosingHndIndex != NO_ENCLOSING_INDEX);
        *inTryRegion = false;
        return ebdEnclosingHndIndex;
    }
    else if (ebdEnclosingHndIndex == NO_ENCLOSING_INDEX)
    {
        assert(ebdEnclosingTryIndex != NO_ENCLOSING_INDEX);
        *inTryRegion = true;
        return ebdEnclosingTryIndex;
    }
    else
    {
        assert(ebdEnclosingTryIndex != NO_ENCLOSING_INDEX);
        assert(ebdEnclosingHndIndex != NO_ENCLOSING_INDEX);
        assert(ebdEnclosingTryIndex != ebdEnclosingHndIndex);
        if (ebdEnclosingTryIndex < ebdEnclosingHndIndex)
        {
            *inTryRegion = true;
            return ebdEnclosingTryIndex;
        }
        else
        {
            *inTryRegion = false;
            return ebdEnclosingHndIndex;
        }
    }
}

/*****************************************************************************/

// We used to assert that the IL offsets in the EH table matched the IL offset stored
// on the blocks pointed to by the try/filter/handler block pointers. This is true at
// import time, but can fail to be true later in compilation when we start doing
// flow optimizations.
//
// That being said, the IL offsets in the EH table should only be examined early,
// during importing. After importing, use block info instead.

IL_OFFSET   EHblkDsc::ebdTryBegOffs()
{
    return ebdTryBegOffset;
}

IL_OFFSET   EHblkDsc::ebdTryEndOffs()
{
    return ebdTryEndOffset;
}

IL_OFFSET    EHblkDsc::ebdHndBegOffs()
{
    return ebdHndBegOffset;
}

IL_OFFSET    EHblkDsc::ebdHndEndOffs()
{
    return ebdHndEndOffset;
}

IL_OFFSET    EHblkDsc::ebdFilterBegOffs()
{
    assert(HasFilter());
    return ebdFilterBegOffset;
}

IL_OFFSET    EHblkDsc::ebdFilterEndOffs()
{
    assert(HasFilter());
    return ebdHndBegOffs(); // end of filter is beginning of handler
}

/* static */
bool                EHblkDsc::ebdIsSameILTry(EHblkDsc* h1, EHblkDsc* h2)
{
    return ((h1->ebdTryBegOffset == h2->ebdTryBegOffset) &&
            (h1->ebdTryEndOffset == h2->ebdTryEndOffset));
}

/*****************************************************************************/

/* static */
bool                EHblkDsc::ebdIsSameTry(EHblkDsc* h1, EHblkDsc* h2)
{
    return ((h1->ebdTryBeg  == h2->ebdTryBeg) &&
            (h1->ebdTryLast == h2->ebdTryLast));
}

bool                EHblkDsc::ebdIsSameTry(Compiler* comp, unsigned t2)
{
    EHblkDsc* h2 = comp->ehGetDsc(t2);
    return ebdIsSameTry(this, h2);
}

bool                EHblkDsc::ebdIsSameTry(BasicBlock* ebdTryBeg, BasicBlock* ebdTryLast)
{
    return ((this->ebdTryBeg  == ebdTryBeg) &&
            (this->ebdTryLast == ebdTryLast));
}

/*****************************************************************************/
#ifdef DEBUG
/*****************************************************************************/

void                EHblkDsc::DispEntry(unsigned XTnum)
{
    printf(" %2u  ::", XTnum);

#if !FEATURE_EH_FUNCLETS
    printf("  %2u  ", XTnum, ebdHandlerNestingLevel);
#endif // !FEATURE_EH_FUNCLETS

    if (ebdEnclosingTryIndex == NO_ENCLOSING_INDEX)
    {
        printf("      ");
    }
    else
    {
        printf("  %2u  ", ebdEnclosingTryIndex);
    }

    if (ebdEnclosingHndIndex == NO_ENCLOSING_INDEX)
    {
        printf("      ");
    }
    else
    {
        printf("  %2u  ", ebdEnclosingHndIndex);
    }

    //////////////
    ////////////// Protected (try) region
    //////////////

    printf("- Try at BB%02u..BB%02u",
        ebdTryBeg->bbNum,
        ebdTryLast->bbNum);

    /* ( brace matching editor workaround to compensate for the following line */
    printf(" [%03X..%03X), ", ebdTryBegOffset, ebdTryEndOffset);
    

    //////////////
    ////////////// Filter region
    //////////////

    if (HasFilter())
    {
        /* ( brace matching editor workaround to compensate for the following line */
        printf("Filter at BB%02u..BB%02u [%03X..%03X), ",
               ebdFilter->bbNum,
               BBFilterLast()->bbNum,
               ebdFilterBegOffset,
               ebdHndBegOffset);
    }

    //////////////
    ////////////// Handler region
    //////////////

    if (ebdHndBeg->bbCatchTyp == BBCT_FINALLY)
    {
        printf("Finally");
    }
    else if (ebdHndBeg->bbCatchTyp == BBCT_FAULT)
    {
        printf("Fault  ");
    }
    else
    {
        printf("Handler");
    }

    printf(" at BB%02u..BB%02u",
        ebdHndBeg->bbNum,
        ebdHndLast->bbNum);

    /* ( brace matching editor workaround to compensate for the following line */
    printf(" [%03X..%03X)", ebdHndBegOffset, ebdHndEndOffset);

    printf("\n");
}

/*****************************************************************************/
#endif // DEBUG
/*****************************************************************************/


/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                          "Compiler" functions                             XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

bool                Compiler::bbInCatchHandlerILRange(BasicBlock* blk)
{
    EHblkDsc* HBtab = ehGetBlockHndDsc(blk);

    if (HBtab == nullptr)
        return false;

    return HBtab->HasCatchHandler() && HBtab->InHndRegionILRange(blk);
}

bool                Compiler::bbInFilterILRange(BasicBlock* blk)
{
    EHblkDsc* HBtab = ehGetBlockHndDsc(blk);

    if (HBtab == nullptr)
        return false;

    return HBtab->InFilterRegionILRange(blk);
}

// Given a handler region, find the innermost try region that contains it.
// NOTE: handlerIndex is 1-based (0 means no handler).
unsigned short      Compiler::bbFindInnermostTryRegionContainingHandlerRegion(unsigned  handlerIndex)
{
    if (handlerIndex > 0)
    {
        unsigned    XTnum;
        EHblkDsc*   ehDsc;
        BasicBlock* blk = ehGetDsc(handlerIndex - 1)->ebdHndBeg;

        // handlerIndex is 1 based, therefore our interesting clauses start from clause compHndBBtab[handlerIndex]
        EHblkDsc* ehDscEnd = compHndBBtab + compHndBBtabCount;
        for (ehDsc = compHndBBtab + handlerIndex, XTnum = handlerIndex; 
             ehDsc < ehDscEnd;
             ehDsc++, XTnum++)
        {
            if (bbInTryRegions(XTnum, blk))
            {
                noway_assert(XTnum < MAX_XCPTN_INDEX);
                return (unsigned short)(XTnum + 1);  // Return the tryIndex
            }
        }
    }
    
    return 0;
}

// Given a try region, find the innermost handler region that contains it.
// NOTE: tryIndex is 1-based (0 means no handler).
unsigned short      Compiler::bbFindInnermostHandlerRegionContainingTryRegion(unsigned  tryIndex)
{
    if (tryIndex > 0)
    {
        unsigned    XTnum;
        EHblkDsc*   ehDsc;
        BasicBlock* blk = ehGetDsc(tryIndex - 1)->ebdTryBeg;

        // tryIndex is 1 based, our interesting clauses start from clause compHndBBtab[tryIndex]
        EHblkDsc* ehDscEnd = compHndBBtab + compHndBBtabCount;
        for (ehDsc = compHndBBtab + tryIndex, XTnum = tryIndex; 
             ehDsc < ehDscEnd;
             ehDsc++, XTnum++)
        {
            if (bbInHandlerRegions(XTnum, blk))
            {
                noway_assert(XTnum < MAX_XCPTN_INDEX);
                return (unsigned short)(XTnum + 1);  // Return the handlerIndex
            }
        }
    }
    
    return 0;
}

/*
   Given a block and a try region index, check to see if the block is within
   the try body. For this check, a funclet is considered to be in the region
   it was extracted from.
*/
bool                Compiler::bbInTryRegions(unsigned regionIndex, BasicBlock * blk)
{
    assert(regionIndex < EHblkDsc::NO_ENCLOSING_INDEX);
    unsigned tryIndex = blk->hasTryIndex() ? blk->getTryIndex() : EHblkDsc::NO_ENCLOSING_INDEX;

    // Loop outward until we find an enclosing try that is the same as the one
    // we are looking for or an outer/later one
    while (tryIndex < regionIndex)
    {
        tryIndex = ehGetEnclosingTryIndex(tryIndex);
    }

    // Now we have the index of 2 try bodies, either they match or not!
    return (tryIndex == regionIndex);
}

/*
   Given a block, check to see if it is in the handler block of the EH descriptor.
   For this check, a funclet is considered to be in the region it was extracted from.
*/
bool                Compiler::bbInHandlerRegions(unsigned regionIndex, BasicBlock * blk)
{
    assert(regionIndex < EHblkDsc::NO_ENCLOSING_INDEX);
    unsigned hndIndex = blk->hasHndIndex() ? blk->getHndIndex() : EHblkDsc::NO_ENCLOSING_INDEX;

    // We can't use the same simple trick here because there is no required ordering
    // of handlers (which also have no required ordering with respect to their try
    // bodies).
    while (hndIndex < EHblkDsc::NO_ENCLOSING_INDEX && hndIndex != regionIndex)
    {
        hndIndex = ehGetEnclosingHndIndex(hndIndex);
    }

    // Now we have the index of 2 try bodies, either they match or not!
    return (hndIndex == regionIndex);
}

/*
   Given a hndBlk, see if it is in one of tryBlk's catch handler regions.

   Since we create one EHblkDsc for each "catch" of a "try", we might end up
   with multiple EHblkDsc's that have the same ebdTryBeg and ebdTryLast, but different
   ebdHndBeg and ebdHndLast. Unfortunately getTryIndex() only returns the index of the first EHblkDsc.
   
   E.g. The following example shows that BB02 has a catch in BB03 and another catch in BB04.
 
       index  nest, enclosing
         0  ::   0,    1 - Try at BB01..BB02 [000..008], Handler at BB03       [009..016]
         1  ::   0,      - Try at BB01..BB02 [000..008], Handler at BB04       [017..022]

   This function will return true for
       bbInCatchHandlerRegions(BB02, BB03) and bbInCatchHandlerRegions(BB02, BB04)
        
*/
bool                Compiler::bbInCatchHandlerRegions(BasicBlock* tryBlk, BasicBlock* hndBlk)
{
    assert(tryBlk->hasTryIndex());
    if (!hndBlk->hasHndIndex())
        return false;
    
    unsigned  XTnum = tryBlk->getTryIndex();
    EHblkDsc* firstEHblkDsc = ehGetDsc(XTnum);
    EHblkDsc* ehDsc = firstEHblkDsc;

    // Rather than searching the whole list, take advantage of our sorting.
    // We will only match against blocks with the same try body (mutually
    // protect regions).  Because of our sort ordering, such regions will
    // always be immediately adjacent, any nested regions will be before the
    // first of the set, and any outer regions will be after the last.
    // Also siblings will be before or after according to their location,
    // but never in between;

    while (XTnum > 0)
    {
        assert(EHblkDsc::ebdIsSameTry(firstEHblkDsc, ehDsc));

        // Stop when the previous region is not mutually protect
        if (!EHblkDsc::ebdIsSameTry(firstEHblkDsc, ehDsc - 1))
        {
            break;
        }

        ehDsc--;
        XTnum--;
    }

    // XTnum and ehDsc are now referring to the first region in the set of
    // mutually protect regions.
    assert(EHblkDsc::ebdIsSameTry(firstEHblkDsc, ehDsc));
    assert((ehDsc == compHndBBtab) || !EHblkDsc::ebdIsSameTry(firstEHblkDsc, ehDsc - 1));

    do
    {
        if (ehDsc->HasCatchHandler() &&
            bbInHandlerRegions(XTnum, hndBlk))
        {
            return true;
        }
        XTnum++;
        ehDsc++;
    }
    while (XTnum < compHndBBtabCount && EHblkDsc::ebdIsSameTry(firstEHblkDsc, ehDsc));

    return false;
}

/******************************************************************************************
 * Give two blocks, return the inner-most enclosing try region that contains both of them.
 * Return 0 if it does not find any try region (which means the inner-most region 
 * is the method itself).
 */

unsigned short      Compiler::bbFindInnermostCommonTryRegion(BasicBlock* bbOne,
                                                             BasicBlock* bbTwo)
{
    unsigned        XTnum;

    for (XTnum = 0;
         XTnum < compHndBBtabCount;
         XTnum++)
    {
        if (bbInTryRegions(XTnum, bbOne) &&
            bbInTryRegions(XTnum, bbTwo))
        {
            noway_assert(XTnum < MAX_XCPTN_INDEX);
            return (unsigned short)(XTnum + 1);    // Return the tryIndex
        }
    }             

    return 0;   
}


// bbIsTryBeg() returns true if this block is the start of any try region.
//              This is computed by examining the current values in the
//              EH table rather than just looking at the block->bbFlags.
//
// Note that a block is the beginning of any try region if it is the beginning of the
// most nested try region it is a member of. Thus, we only need to check the EH
// table entry related to the try index stored on the block.
//
bool            Compiler::bbIsTryBeg(BasicBlock* block)
{
    EHblkDsc* ehDsc = ehGetBlockTryDsc(block);
    return (ehDsc != nullptr) && (block == ehDsc->ebdTryBeg);
}


// bbIsHanderBeg() returns true if "block" is the start of any handler or filter.
// Note that if a block is the beginning of a handler or filter, it must be the beginning
// of the most nested handler or filter region it is in. Thus, we only need to look at the EH
// descriptor corresponding to the handler index on the block.
//
bool            Compiler::bbIsHandlerBeg(BasicBlock* block)
{
    EHblkDsc* ehDsc = ehGetBlockHndDsc(block);
    return (ehDsc != nullptr) &&
           ((block == ehDsc->ebdHndBeg) ||
            (ehDsc->HasFilter() && (block == ehDsc->ebdFilter)));
}


bool            Compiler::bbIsExFlowBlock(BasicBlock* block, unsigned* regionIndex)
{
    if (block->hasHndIndex())
    {
        *regionIndex = block->getHndIndex();
        return block == ehGetDsc(*regionIndex)->ExFlowBlock();
    }
    else
    {
        return false;
    }
}


bool            Compiler::ehHasCallableHandlers()
{
#if FEATURE_EH_FUNCLETS

    // Any EH in the function?

    return compHndBBtabCount > 0;

#else // FEATURE_EH_FUNCLETS

    return ehNeedsShadowSPslots();

#endif // FEATURE_EH_FUNCLETS
}


/******************************************************************************************
 * Determine if 'block' is the last block of an EH 'try' or handler (ignoring filters). If so,
 * return the EH descriptor pointer for that EH region. Otherwise, return nullptr.
 */
EHblkDsc*       Compiler::ehIsBlockTryLast(BasicBlock* block)
{
    EHblkDsc* HBtab = ehGetBlockTryDsc(block);
    if ((HBtab != nullptr) && (HBtab->ebdTryLast == block))
    {
        return HBtab;
    }
    return nullptr;
}

EHblkDsc*       Compiler::ehIsBlockHndLast(BasicBlock* block)
{
    EHblkDsc* HBtab = ehGetBlockHndDsc(block);
    if ((HBtab != nullptr) && (HBtab->ebdHndLast == block))
    {
        return HBtab;
    }
    return nullptr;
}

bool            Compiler::ehIsBlockEHLast(BasicBlock* block)
{
    return (ehIsBlockTryLast(block) != nullptr) ||
           (ehIsBlockHndLast(block) != nullptr);
}


//------------------------------------------------------------------------
// ehGetMostNestedRegionIndex: Return the region index of the most nested EH region this block is in.
// The return value is in the range [0..compHndBBtabCount]. It is same scale as bbTryIndex/bbHndIndex:
// 0 means main method, N is used as an index to compHndBBtab[N - 1]. If we don't return 0, then
// *inTryRegion indicates whether the most nested region for the block is a 'try' clause or
// filter/handler clause. For 0 return, *inTryRegion is set to true.
//
// Arguments:
//    block - the BasicBlock we want the region index for.
//    inTryRegion - an out parameter. As described above.
//
// Return Value:
//    As described above.
//
unsigned        Compiler::ehGetMostNestedRegionIndex(BasicBlock* block, bool* inTryRegion)
{
    assert(block != nullptr);
    assert(inTryRegion != nullptr);

    unsigned mostNestedRegion;
    if (block->bbHndIndex == 0)
    {
        mostNestedRegion = block->bbTryIndex;
        *inTryRegion = true;
    }
    else if (block->bbTryIndex == 0)
    {
        mostNestedRegion = block->bbHndIndex;
        *inTryRegion = false;
    }
    else
    {
        if (block->bbTryIndex < block->bbHndIndex)
        {
            mostNestedRegion = block->bbTryIndex;
            *inTryRegion = true;
        }
        else
        {
            assert(block->bbTryIndex != block->bbHndIndex); // A block can't be both in the 'try' and 'handler' region of the same EH region
            mostNestedRegion = block->bbHndIndex;
            *inTryRegion = false;
        }
    }

    assert(mostNestedRegion <= compHndBBtabCount);
    return mostNestedRegion;
}


/*****************************************************************************
 * Returns the try index of the enclosing try, skipping all EH regions with the
 * same try region (that is, all 'mutual protect' regions). If there is no such
 * enclosing try, returns EHblkDsc::NO_ENCLOSING_INDEX.
 */
unsigned        Compiler::ehTrueEnclosingTryIndexIL(unsigned regionIndex)
{
    assert(regionIndex != EHblkDsc::NO_ENCLOSING_INDEX);

    EHblkDsc* ehDscRoot = ehGetDsc(regionIndex);
    EHblkDsc* HBtab = ehDscRoot;

    for (;;)
    {
        regionIndex = HBtab->ebdEnclosingTryIndex;
        if (regionIndex == EHblkDsc::NO_ENCLOSING_INDEX)
            break;  // No enclosing 'try'; we're done

        HBtab = ehGetDsc(regionIndex);
        if (!EHblkDsc::ebdIsSameILTry(ehDscRoot, HBtab))
            break;  // Found an enclosing 'try' that has a different 'try' region (is not mutually-protect with the original region). Return it.
    }

    return regionIndex;
}


unsigned        Compiler::ehGetEnclosingRegionIndex(unsigned regionIndex, bool* inTryRegion)
{
    assert(regionIndex != EHblkDsc::NO_ENCLOSING_INDEX);

    EHblkDsc* ehDsc = ehGetDsc(regionIndex);
    return ehDsc->ebdGetEnclosingRegionIndex(inTryRegion);
}

/*****************************************************************************
 * The argument 'block' has been deleted. Update the EH table so 'block' is no longer listed
 * as a 'last' block. You can't delete a 'begin' block this way.
 */
void            Compiler::ehUpdateForDeletedBlock(BasicBlock* block)
{
    assert(block->bbFlags & BBF_REMOVED);

    if (!block->hasTryIndex() && !block->hasHndIndex())
    {
        // The block is not part of any EH region, so there is nothing to do.
        return;
    }

    BasicBlock* bPrev = block->bbPrev;
    assert(bPrev != nullptr);

    ehUpdateLastBlocks(block, bPrev);
}


/*****************************************************************************
 * Determine if an empty block can be deleted, and still preserve the EH normalization
 * rules on blocks.
 *
 * We only consider the case where the block to be deleted is the last block of a region,
 * and the region is being contracted such that the previous block will become the new
 * 'last' block. If this previous block is already a 'last' block, then we can't do the
 * delete, as that would cause a single block to be the 'last' block of multiple regions.
 */
bool            Compiler::ehCanDeleteEmptyBlock(BasicBlock* block)
{
    assert(block->isEmpty());

    return true;

#if 0 // This is disabled while the "multiple last block" normalization is disabled
    if (!fgNormalizeEHDone)
    {
        return true;
    }

    if (ehIsBlockEHLast(block))
    {
        BasicBlock* bPrev = block->bbPrev;
        if ((bPrev != nullptr) && ehIsBlockEHLast(bPrev))
        {
            return false;
        }
    }

    return true;
#endif // 0
}

/*****************************************************************************
 * The 'last' block of one or more EH regions might have changed. Update the EH table.
 * This can happen if the EH region shrinks, where one or more blocks have been removed
 * from the region. It can happen if the EH region grows, where one or more blocks
 * have been added at the end of the region.
 *
 * We might like to verify the handler table integrity after doing this update, but we
 * can't because this might just be one step by the caller in a transformation back to
 * a legal state.
 *
 * Arguments:
 *      oldLast -- Search for this block as the 'last' block of one or more EH regions.
 *      newLast -- If 'oldLast' is found to be the 'last' block of an EH region, replace it by 'newLast'.
 */
void            Compiler::ehUpdateLastBlocks(BasicBlock* oldLast, BasicBlock* newLast)
{
    EHblkDsc*     HBtab;
    EHblkDsc*     HBtabEnd;

    for (HBtab = compHndBBtab, HBtabEnd = compHndBBtab + compHndBBtabCount;
         HBtab < HBtabEnd;
         HBtab++)
    {
        if (HBtab->ebdTryLast == oldLast)
        {
            fgSetTryEnd(HBtab, newLast);
        }
        if (HBtab->ebdHndLast == oldLast)
        {
            fgSetHndEnd(HBtab, newLast);
        }
    }
}

unsigned        Compiler::ehGetCallFinallyRegionIndex(unsigned finallyIndex, bool* inTryRegion)
{
    assert(finallyIndex != EHblkDsc::NO_ENCLOSING_INDEX);
    assert(ehGetDsc(finallyIndex)->HasFinallyHandler());

#if defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_)
    return ehGetDsc(finallyIndex)->ebdGetEnclosingRegionIndex(inTryRegion);
#else
    *inTryRegion = true;
    return finallyIndex;
#endif
}

void            Compiler::ehGetCallFinallyBlockRange(unsigned finallyIndex, BasicBlock** begBlk, BasicBlock** endBlk)
{
    assert(finallyIndex != EHblkDsc::NO_ENCLOSING_INDEX);
    assert(ehGetDsc(finallyIndex)->HasFinallyHandler());
    assert(begBlk != nullptr);
    assert(endBlk != nullptr);

    EHblkDsc* ehDsc = ehGetDsc(finallyIndex);

#if FEATURE_EH_CALLFINALLY_THUNKS
    bool inTryRegion;
    unsigned callFinallyRegionIndex = ehGetCallFinallyRegionIndex(finallyIndex, &inTryRegion);

    if (callFinallyRegionIndex == EHblkDsc::NO_ENCLOSING_INDEX)
    {
        *begBlk = fgFirstBB;
        *endBlk = fgEndBBAfterMainFunction();
    }
    else
    {
        EHblkDsc* ehDsc = ehGetDsc(callFinallyRegionIndex);

        if (inTryRegion)
        {
            *begBlk = ehDsc->ebdTryBeg;
            *endBlk = ehDsc->ebdTryLast->bbNext;
        }
        else
        {
            *begBlk = ehDsc->ebdHndBeg;
            *endBlk = ehDsc->ebdHndLast->bbNext;
        }
    }
#else // !FEATURE_EH_CALLFINALLY_THUNKS
    *begBlk = ehDsc->ebdTryBeg;
    *endBlk = ehDsc->ebdTryLast->bbNext;
#endif // !FEATURE_EH_CALLFINALLY_THUNKS
}

#ifdef DEBUG

bool            Compiler::ehCallFinallyInCorrectRegion(BasicBlock* blockCallFinally, unsigned finallyIndex)
{
    assert(blockCallFinally->bbJumpKind == BBJ_CALLFINALLY);
    assert(finallyIndex != EHblkDsc::NO_ENCLOSING_INDEX);
    assert(finallyIndex < compHndBBtabCount);
    assert(ehGetDsc(finallyIndex)->HasFinallyHandler());

    bool inTryRegion;
    unsigned callFinallyIndex = ehGetCallFinallyRegionIndex(finallyIndex, &inTryRegion);
    if (callFinallyIndex == EHblkDsc::NO_ENCLOSING_INDEX)
    {
        if (blockCallFinally->hasTryIndex() || blockCallFinally->hasHndIndex())
        {
            // The BBJ_CALLFINALLY is supposed to be in the main function body, not in any EH region.
            return false;
        }
        else
        {
            return true;
        }
    }
    else
    {
        if (inTryRegion)
        {
            if (bbInTryRegions(callFinallyIndex, blockCallFinally))
                return true;
        }
        else
        {
            if (bbInHandlerRegions(callFinallyIndex, blockCallFinally))
                return true;
        }
    }

    return false;
}

#endif // DEBUG

#if FEATURE_EH_FUNCLETS

/*****************************************************************************
 *
 *  Are there (or will there be) any funclets in the function?
 */

bool            Compiler::ehAnyFunclets()
{
    return compHndBBtabCount > 0;   // if there is any EH, there will be funclets
}

/*****************************************************************************
 *
 *  Count the number of EH funclets in the function. This will return the number
 *  there will be after funclets have been created, but because it runs over the
 *  EH table, it is accurate at any time.
 */

unsigned        Compiler::ehFuncletCount()
{
    unsigned        funcletCnt = 0;
    EHblkDsc*       HBtab;
    EHblkDsc*       HBtabEnd;

    for (HBtab = compHndBBtab, HBtabEnd = compHndBBtab + compHndBBtabCount;
         HBtab < HBtabEnd;
         HBtab++)
    {
        if (HBtab->HasFilter())
            ++funcletCnt;
        ++funcletCnt;
    }
    return funcletCnt;
}

/*****************************************************************************
 *
 *  Get the index to use as the cache key for sharing throw blocks.
 *  For non-funclet platforms, this is just the block's bbTryIndex, to ensure
 *  that throw is protected by the correct set of trys.  However, when we have
 *  funclets we also have to ensure that the throw blocks are *not* shared
 *  across funclets, so we use EHblkDsc index of either the funclet or
 *  the containing try region, whichever is inner-most.  We differentiate
 *  between the 3 cases by setting the high bits (0 = try, 1 = handler,
 *  2 = filter)
 *
 */
unsigned        Compiler::bbThrowIndex(BasicBlock* blk)
{
    if (!blk->hasTryIndex() && !blk->hasHndIndex())
    {
        return -1;
    }

    const unsigned tryIndex = blk->hasTryIndex() ? blk->getTryIndex() : USHRT_MAX;
    const unsigned hndIndex = blk->hasHndIndex() ? blk->getHndIndex() : USHRT_MAX;
    assert(tryIndex != hndIndex);
    assert(tryIndex != USHRT_MAX || hndIndex != USHRT_MAX);

    if (tryIndex < hndIndex)
    {
        // The most enclosing region is a try body, use it
        assert(tryIndex <= 0x3FFFFFFF);
        return tryIndex;
    }

    // The most enclosing region is a handler which will be a funclet
    // Now we have to figure out if blk is in the filter or handler
    assert(hndIndex <= 0x3FFFFFFF);
    if (ehGetDsc(hndIndex)->InFilterRegionBBRange(blk))
    {
        return hndIndex | 0x40000000;
    }

    return hndIndex | 0x80000000;
}

#endif // FEATURE_EH_FUNCLETS


/*****************************************************************************
 * Determine the emitter code cookie for a block, for unwind purposes.
 */

void*               Compiler::ehEmitCookie(BasicBlock* block)
{
    noway_assert(block);

    void* cookie;

#if FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)
    if (block->bbFlags & BBF_FINALLY_TARGET)
    {
        // Use the offset of the beginning of the NOP padding, not the main block.
        // This might include loop head padding, too, if this is a loop head.
        assert(block->bbUnwindNopEmitCookie); // probably not null-initialized, though, so this might not tell us anything
        cookie = block->bbUnwindNopEmitCookie;
    }
    else
#endif // FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)
    {
        cookie = block->bbEmitCookie;
    }

    noway_assert(cookie != nullptr);
    return cookie;
}


/*****************************************************************************
 * Determine the emitter code offset for a block. If the block is a finally
 * target, choose the offset of the NOP padding that precedes the block.
 */

UNATIVE_OFFSET      Compiler::ehCodeOffset(BasicBlock* block)
{
    return genEmitter->emitCodeOffset(ehEmitCookie(block), 0);
}

/****************************************************************************/

EHblkDsc*           Compiler::ehInitHndRange(BasicBlock* blk,
                                             IL_OFFSET*  hndBeg,
                                             IL_OFFSET*  hndEnd,
                                             bool*       inFilter)
{
    EHblkDsc* hndTab = ehGetBlockHndDsc(blk);
    if (hndTab != nullptr)
    {
        if (hndTab->InFilterRegionILRange(blk))
        {
            *hndBeg   = hndTab->ebdFilterBegOffs();
            *hndEnd   = hndTab->ebdFilterEndOffs();
            *inFilter = true;
        }
        else
        {
            *hndBeg   = hndTab->ebdHndBegOffs();
            *hndEnd   = hndTab->ebdHndEndOffs();
            *inFilter = false;
        }
    }
    else
    {
        *hndBeg   = 0;
        *hndEnd   = info.compILCodeSize;
        *inFilter = false;
    }
    return hndTab;
}

/****************************************************************************/

EHblkDsc*           Compiler::ehInitTryRange(BasicBlock* blk,
                                             IL_OFFSET*  tryBeg,
                                             IL_OFFSET*  tryEnd)
{
    EHblkDsc* tryTab = ehGetBlockTryDsc(blk);
    if (tryTab != nullptr)
    {
        *tryBeg = tryTab->ebdTryBegOffs();
        *tryEnd = tryTab->ebdTryEndOffs();
    }
    else
    {
        *tryBeg = 0;
        *tryEnd = info.compILCodeSize;
    }
    return tryTab;
}

/****************************************************************************/

EHblkDsc*           Compiler::ehInitHndBlockRange(BasicBlock*  blk,
                                                  BasicBlock** hndBeg,
                                                  BasicBlock** hndLast,
                                                  bool*        inFilter)
{
    EHblkDsc* hndTab = ehGetBlockHndDsc(blk);
    if (hndTab != nullptr)
    {
        if (hndTab->InFilterRegionBBRange(blk))
        {
            *hndBeg   = hndTab->ebdFilter;
            if (hndLast != nullptr)
            {
                *hndLast = hndTab->BBFilterLast();
            }
            *inFilter = true;
        }
        else
        {
            *hndBeg   = hndTab->ebdHndBeg;
            if (hndLast != nullptr)
            {
                *hndLast  = hndTab->ebdHndLast;
            }
            *inFilter = false;
        }
    }
    else
    {
        *hndBeg   = nullptr;
        if (hndLast != nullptr)
        {
            *hndLast  = nullptr;
        }
        *inFilter = false;
    }
    return hndTab;
}

/****************************************************************************/

EHblkDsc*           Compiler::ehInitTryBlockRange(BasicBlock*  blk,
                                                  BasicBlock** tryBeg,
                                                  BasicBlock** tryLast)
{
    EHblkDsc* tryTab = ehGetBlockTryDsc(blk);
    if (tryTab != nullptr)
    {
        *tryBeg = tryTab->ebdTryBeg;
        if (tryLast != nullptr)
        {
            *tryLast = tryTab->ebdTryLast;
        }
    }
    else
    {
        *tryBeg = nullptr;
        if (tryLast != nullptr)
        {
            *tryLast = nullptr;
        }
    }
    return tryTab;
}

/*****************************************************************************
 *  This method updates the value of ebdTryLast.
 */

void Compiler::fgSetTryEnd(EHblkDsc*     handlerTab,
                           BasicBlock*   newTryLast)
{
    assert(newTryLast != nullptr);

    //
    // Check if we are going to change the existing value of endTryLast
    //
    if (handlerTab->ebdTryLast != newTryLast)
    {
        // Update the EH table with the newTryLast block
        handlerTab->ebdTryLast = newTryLast;

#ifdef DEBUG
        if (verbose)
        {
            printf("EH#%u: New last block of try: BB%02u\n", ehGetIndex(handlerTab), newTryLast->bbNum);
        }
#endif // DEBUG
    }
}

/*****************************************************************************
 *
 *  This method updates the value of ebdHndLast.
 */

void Compiler::fgSetHndEnd(EHblkDsc*     handlerTab,
                           BasicBlock*   newHndLast)
{
    assert(newHndLast != nullptr);

    //
    // Check if we are going to change the existing value of endHndLast
    //
    if (handlerTab->ebdHndLast != newHndLast)
    {
        // Update the EH table with the newHndLast block
        handlerTab->ebdHndLast = newHndLast;

#ifdef DEBUG
        if (verbose)
        {
            printf("EH#%u: New last block of handler: BB%02u\n", ehGetIndex(handlerTab), newHndLast->bbNum);
        }
#endif // DEBUG
    }
}


/*****************************************************************************
 *
 *  Given a EH handler table entry update the ebdTryLast and ebdHndLast pointers
 *  to skip basic blocks that have been removed. They are set to the first
 *  non-removed block after ebdTryBeg and ebdHndBeg, respectively.
 *
 *  Note that removed blocks are not in the global list of blocks (no block in the
 *  global list points to them). However, their pointers are still valid. We use
 *  this fact when we walk lists of removed blocks until we find a non-removed
 *  block, to be used for ending our iteration.
 */

void Compiler::fgSkipRmvdBlocks(EHblkDsc* handlerTab)
{
    BasicBlock*  block;
    BasicBlock*  bEnd;
    BasicBlock*  bLast;

    // Update ebdTryLast
    bLast = nullptr;

    // Find the first non-removed block after the 'try' region to end our iteration.
    bEnd = handlerTab->ebdTryLast->bbNext;
    while ((bEnd != nullptr) && (bEnd->bbFlags & BBF_REMOVED))
    {
        bEnd = bEnd->bbNext;
    }

    // Update bLast to account for any removed blocks
    block = handlerTab->ebdTryBeg;
    while (block != nullptr)
    {
        if ((block->bbFlags & BBF_REMOVED) == 0)
        {
            bLast = block;
        }

        block = block->bbNext;

        if (block == bEnd)
            break;
    }

    fgSetTryEnd(handlerTab, bLast);

    // Update ebdHndLast
    bLast = nullptr;

    // Find the first non-removed block after the handler region to end our iteration.
    bEnd = handlerTab->ebdHndLast->bbNext;
    while ((bEnd != nullptr) && (bEnd->bbFlags & BBF_REMOVED))
    {
        bEnd = bEnd->bbNext;
    }

    // Update bLast to account for any removed blocks
    block = handlerTab->ebdHndBeg;
    while (block != nullptr)
    {
        if ((block->bbFlags & BBF_REMOVED) == 0)
        {
            bLast = block;
        }

        block = block->bbNext;
        if (block == bEnd)
            break;
    }

    fgSetHndEnd(handlerTab, bLast);
}


/*****************************************************************************
 *
 *  Allocate the EH table
 */
void                Compiler::fgAllocEHTable()
{
#if FEATURE_EH_FUNCLETS

    // We need to allocate space for EH clauses that will be used by funclets
    // as well as one for each EH clause from the IL. Nested EH clauses pulled
    // out as funclets create one EH clause for each enclosing region. Thus,
    // the maximum number of clauses we will need might be very large. We allocate
    // twice the number of EH clauses in the IL, which should be good in practice.
    // In extreme cases, we might need to abandon this and reallocate. See
    // fgAddEHTableEntry() for more details.
#ifdef DEBUG
    compHndBBtabAllocCount = info.compXcptnsCount; // force the resizing code to hit more frequently in DEBUG
#else // DEBUG
    compHndBBtabAllocCount = info.compXcptnsCount * 2;
#endif // DEBUG

#else // FEATURE_EH_FUNCLETS

    compHndBBtabAllocCount = info.compXcptnsCount;

#endif // FEATURE_EH_FUNCLETS

    compHndBBtab = new(this, CMK_BasicBlock) EHblkDsc[compHndBBtabAllocCount];

    compHndBBtabCount = info.compXcptnsCount;
}

/*****************************************************************************
 *
 *  Remove a single exception table entry. Note that this changes the size of
 *  the exception table. If calling this within a loop over the exception table
 *  be careful to iterate again on the current entry (if XTnum) to not skip any.
 */
void                Compiler::fgRemoveEHTableEntry(unsigned XTnum)
{
    assert(compHndBBtabCount > 0);
    assert(XTnum < compHndBBtabCount);

    EHblkDsc*       HBtab;

    /* Reduce the number of entries in the EH table by one */
    compHndBBtabCount--;

    if (compHndBBtabCount == 0)
    {
        // No more entries remaining.
        INDEBUG(compHndBBtab = (EHblkDsc *)INVALID_POINTER_VALUE;)
    }
    else
    {
        /* If we recorded an enclosing index for xtab then see
         * if it needs to be updated due to the removal of this entry
         */

        HBtab = compHndBBtab + XTnum;

        EHblkDsc* xtabEnd;
        EHblkDsc* xtab;
        for (xtab = compHndBBtab, xtabEnd = compHndBBtab + compHndBBtabCount;
             xtab < xtabEnd;
             xtab++)
        {
            if ((xtab != HBtab) &&
                (xtab->ebdEnclosingTryIndex != EHblkDsc::NO_ENCLOSING_INDEX) &&
                (xtab->ebdEnclosingTryIndex >= XTnum))
            {
                // Update the enclosing scope link
                if (xtab->ebdEnclosingTryIndex == XTnum)
                {
                    xtab->ebdEnclosingTryIndex = HBtab->ebdEnclosingTryIndex;
                }
                if ((xtab->ebdEnclosingTryIndex > XTnum) &&
                    (xtab->ebdEnclosingTryIndex != EHblkDsc::NO_ENCLOSING_INDEX))
                {
                    xtab->ebdEnclosingTryIndex--;
                }
            }

            if ((xtab != HBtab) &&
                (xtab->ebdEnclosingHndIndex != EHblkDsc::NO_ENCLOSING_INDEX) &&
                (xtab->ebdEnclosingHndIndex >= XTnum))
            {
                // Update the enclosing scope link
                if (xtab->ebdEnclosingHndIndex == XTnum)
                {
                    xtab->ebdEnclosingHndIndex = HBtab->ebdEnclosingHndIndex;
                }
                if ((xtab->ebdEnclosingHndIndex > XTnum) &&
                    (xtab->ebdEnclosingHndIndex != EHblkDsc::NO_ENCLOSING_INDEX))
                {
                    xtab->ebdEnclosingHndIndex--;
                }
            }
        }

        /* We need to update all of the blocks' bbTryIndex */

        for (BasicBlock* blk = fgFirstBB; blk; blk = blk->bbNext)
        {
            if (blk->hasTryIndex())
            {
                if (blk->getTryIndex() == XTnum)
                {
                    noway_assert(blk->bbFlags & BBF_REMOVED);
                    INDEBUG(blk->setTryIndex(MAX_XCPTN_INDEX);) // Note: this is still a legal index, just unlikely
                }
                else if (blk->getTryIndex() > XTnum)
                {
                    blk->setTryIndex(blk->getTryIndex() - 1);
                }
            }

            if (blk->hasHndIndex())
            {
                if (blk->getHndIndex() == XTnum)
                {
                    noway_assert(blk->bbFlags & BBF_REMOVED);
                    INDEBUG(blk->setHndIndex(MAX_XCPTN_INDEX);) // Note: this is still a legal index, just unlikely
                }
                else if (blk->getHndIndex() > XTnum)
                {
                    blk->setHndIndex(blk->getHndIndex() - 1);
                }
            }
        }

        /* Now remove the unused entry from the table */

        if (XTnum < compHndBBtabCount)
        {
            /* We copy over the old entry */
            memmove(HBtab, HBtab + 1, (compHndBBtabCount - XTnum) * sizeof(*HBtab));
        }
        else
        {
            /* Last entry. Don't need to do anything */
            noway_assert(XTnum == compHndBBtabCount);
        }
    }
}


#if FEATURE_EH_FUNCLETS

/*****************************************************************************
 *
 *  Add a single exception table entry at index 'XTnum', [0 <= XTnum <= compHndBBtabCount].
 *  If 'XTnum' is compHndBBtabCount, then add the entry at the end.
 *  Note that this changes the size of the exception table.
 *  All the blocks referring to the various index values are updated.
 *  The table entry itself is not filled in.
 *  Returns a pointer to the new entry.
 */
EHblkDsc*           Compiler::fgAddEHTableEntry(unsigned XTnum)
{
    if (XTnum != compHndBBtabCount)
    {
        // Update all enclosing links that will get invalidated by inserting an entry at 'XTnum'

        EHblkDsc* xtabEnd;
        EHblkDsc* xtab;
        for (xtab = compHndBBtab, xtabEnd = compHndBBtab + compHndBBtabCount;
             xtab < xtabEnd;
             xtab++)
        {
            if ((xtab->ebdEnclosingTryIndex != EHblkDsc::NO_ENCLOSING_INDEX) &&
                (xtab->ebdEnclosingTryIndex >= XTnum))
            {
                // Update the enclosing scope link
                xtab->ebdEnclosingTryIndex++;
            }
            if ((xtab->ebdEnclosingHndIndex != EHblkDsc::NO_ENCLOSING_INDEX) &&
                (xtab->ebdEnclosingHndIndex >= XTnum))
            {
                // Update the enclosing scope link
                xtab->ebdEnclosingHndIndex++;
            }
        }

        // We need to update the BasicBlock bbTryIndex and bbHndIndex field for all blocks

        for (BasicBlock* blk = fgFirstBB; blk; blk = blk->bbNext)
        {
            if (blk->hasTryIndex() && (blk->getTryIndex() >= XTnum))
            {
                blk->setTryIndex(blk->getTryIndex() + 1);
            }

            if (blk->hasHndIndex() && (blk->getHndIndex() >= XTnum))
            {
                blk->setHndIndex(blk->getHndIndex() + 1);
            }
        }
    }

    // Increase the number of entries in the EH table by one

    if (compHndBBtabCount == compHndBBtabAllocCount)
    {
        // We need to reallocate the table

        if (compHndBBtabAllocCount == MAX_XCPTN_INDEX)      // We're already at the max size for indices to be unsigned short
            IMPL_LIMITATION("too many exception clauses");

        // Double the table size. For stress, we could use +1. Note that if the table isn't allocated
        // yet, such as when we add an EH region for synchronized methods that don't already have one,
        // we start at zero, so we need to make sure the new table has at least one entry.
        unsigned newHndBBtabAllocCount = max(1,compHndBBtabAllocCount * 2);
        noway_assert(compHndBBtabAllocCount < newHndBBtabAllocCount);   // check for overflow

        if (newHndBBtabAllocCount > MAX_XCPTN_INDEX)
        {
            newHndBBtabAllocCount = MAX_XCPTN_INDEX;    // increase to the maximum size we allow
        }

        JITDUMP("*********** fgAddEHTableEntry: increasing EH table size from %d to %d\n",
            compHndBBtabAllocCount, newHndBBtabAllocCount);

        compHndBBtabAllocCount = newHndBBtabAllocCount;

        EHblkDsc* newTable = new(this, CMK_BasicBlock) EHblkDsc[compHndBBtabAllocCount];

        // Move over the stuff before the new entry

        memcpy_s(newTable,
                 compHndBBtabAllocCount * sizeof(*compHndBBtab),
                 compHndBBtab,
                 XTnum * sizeof(*compHndBBtab));

        if (XTnum != compHndBBtabCount)
        {
            // Move over the stuff after the new entry
            memcpy_s(newTable + XTnum + 1,
                     (compHndBBtabAllocCount - XTnum - 1) * sizeof(*compHndBBtab),
                     compHndBBtab + XTnum,
                     (compHndBBtabCount - XTnum) * sizeof(*compHndBBtab));
        }

        // Now set the new table as the table to use. The old one gets lost, but we can't
        // free it because we don't have a freeing allocator.

        compHndBBtab = newTable;
    }
    else if (XTnum != compHndBBtabCount)
    {
        // Leave the elements before the new element alone. Move the ones after it, to make space.

        EHblkDsc* HBtab = compHndBBtab + XTnum;

        memmove_s(HBtab + 1,
                  (compHndBBtabAllocCount - XTnum - 1) * sizeof(*compHndBBtab),
                  HBtab,
                  (compHndBBtabCount - XTnum) * sizeof(*compHndBBtab));
    }

    // Now the entry is there, but not filled in

    compHndBBtabCount++;
    return compHndBBtab + XTnum;
}

#endif // FEATURE_EH_FUNCLETS


#if !FEATURE_EH

/*****************************************************************************
 *  fgRemoveEH: To facilitiate the bring-up of new platforms without having to
 *  worry about fully implementing EH, we want to simply remove EH constructs
 *  from the IR. This works because a large percentage of our tests contain
 *  EH constructs but don't actually throw exceptions. This function removes
 *  'catch', 'filter', 'filter-handler', and 'fault' clauses completely.
 *  It requires that the importer has created the EH table, and that normal
 *  EH well-formedness tests have been done, and 'leave' opcodes have been
 *  imported.
 *
 *  It currently does not handle 'finally' clauses, so tests that include
 *  'finally' will NYI(). To handle 'finally', we would need to inline the
 *  'finally' clause IL at each exit from a finally-protected 'try', or
 *  else call the 'finally' clause, like normal.
 *
 *  Walk the EH table from beginning to end. If a table entry is nested within
 *  a handler, we skip it, as we'll delete its code when we get to the enclosing
 *  handler. If a clause is enclosed within a 'try', or has no nesting, then we delete
 *  it (and its range of code blocks). We don't need to worry about cleaning up
 *  the EH table entries as we remove the individual handlers (such as calling
 *  fgRemoveEHTableEntry()), as we'll null out the entire table at the end.
 *
 *  This function assumes FEATURE_EH_FUNCLETS is defined.
 */
void                Compiler::fgRemoveEH()
{
#ifdef DEBUG
    if  (verbose)
        printf("\n*************** In fgRemoveEH()\n");
#endif // DEBUG

    if (compHndBBtabCount == 0)
    {
        JITDUMP("No EH to remove\n\n");
        return;
    }

#ifdef DEBUG
    if  (verbose)
    {
        printf("\n*************** Before fgRemoveEH()\n");
        fgDispBasicBlocks();
        fgDispHandlerTab();
        printf("\n");
    }
#endif // DEBUG

    // Make sure we're early in compilation, so we don't need to update lots of data structures.
    assert(!fgComputePredsDone);
    assert(!fgDomsComputed);
    assert(!fgFuncletsCreated);
    assert(fgFirstFuncletBB == nullptr); // this should follow from "!fgFuncletsCreated"
    assert(!optLoopsMarked);

    unsigned        XTnum;
    EHblkDsc*       HBtab;

    for (XTnum = 0, HBtab = compHndBBtab;
         XTnum < compHndBBtabCount;
         XTnum++  , HBtab++)
    {
        if (HBtab->ebdEnclosingHndIndex != EHblkDsc::NO_ENCLOSING_INDEX)
        {
            // This entry is nested within some other handler. So, don't delete the
            // EH entry here; let the enclosing handler delete it. Note that for this
            // EH entry, both the 'try' and handler portions are fully nested within
            // the enclosing handler region, due to proper nesting rules.
            continue;
        }

        if (HBtab->HasCatchHandler() ||
            HBtab->HasFilter()       ||
            HBtab->HasFaultHandler())
        {
            // Remove all the blocks associated with the handler. Note that there is no
            // fall-through into the handler, or fall-through out of the handler, so
            // just deleting the blocks is sufficient. Note, however, that for every
            // BBJ_EHCATCHRET we delete, we need to fix up the reference count of the
            // block it points to (by subtracting one from its reference count).
            // Note that the blocks for a filter immediately preceed the blocks for its associated filter-handler.

            BasicBlock* blkBeg  = HBtab->HasFilter() ? HBtab->ebdFilter : HBtab->ebdHndBeg;
            BasicBlock* blkLast = HBtab->ebdHndLast;

            // Splice out the range of blocks from blkBeg to blkLast (inclusive).
            fgUnlinkRange(blkBeg, blkLast);

            BasicBlock* blk;

            // Walk the unlinked blocks and marked them as having been removed.
            for (blk = blkBeg; blk != blkLast->bbNext; blk = blk->bbNext)
            {
                blk->bbFlags |= BBF_REMOVED;

                if (blk->bbJumpKind == BBJ_EHCATCHRET)
                {
                    assert(blk->bbJumpDest->bbRefs > 0);
                    blk->bbJumpDest->bbRefs -= 1;
                }
            }

            // Walk the blocks of the 'try' and clear data that makes them appear to be within a 'try'.
            for (blk = HBtab->ebdTryBeg; blk != HBtab->ebdTryLast->bbNext; blk = blk->bbNext)
            {
                blk->clearTryIndex();
                blk->bbFlags &= ~BBF_TRY_BEG;
            }

            // If we are deleting a range of blocks whose last block is
            // the 'last' block of an enclosing try/hnd region, we need to
            // fix up the EH table. We only care about less nested
            // EH table entries, since we've already deleted everything up to XTnum.

            unsigned    XTnum2;
            EHblkDsc*   HBtab2;
            for (XTnum2 = XTnum + 1, HBtab2 = compHndBBtab + XTnum2;
                 XTnum2 < compHndBBtabCount;
                 XTnum2++          , HBtab2++)
            {
                // Handle case where deleted range is at the end of a 'try'.
                if (HBtab2->ebdTryLast == blkLast)
                {
                    fgSetTryEnd(HBtab2, blkBeg->bbPrev);
                }
                // Handle case where deleted range is at the end of a handler.
                // (This shouldn't happen, though, because we don't delete handlers
                // nested within other handlers; we wait until we get to the
                // enclosing handler.)
                if (HBtab2->ebdHndLast == blkLast)
                {
                    unreached();
                }
            }
        }
        else
        {
            // It must be a 'finally'. We still need to call the finally. Note that the
            // 'finally' can be "called" from multiple locations (e.g., the 'try' block
            // can have multiple 'leave' instructions, each leaving to different targets,
            // and each going through the 'finally'). We could inline the 'finally' at each
            // LEAVE site within a 'try'. If the 'try' exits at all (that is, no infinite loop),
            // there will be at least one since there is no "fall through" at the end of
            // the 'try'.

            assert(HBtab->HasFinallyHandler());

            NYI("remove finally blocks");
        }
    } /* end of the for loop over XTnum */

#ifdef DEBUG
    // Make sure none of the remaining blocks have any EH.

    BasicBlock* blk;
    foreach_block(this, blk)
    {
        assert(!blk->hasTryIndex());
        assert(!blk->hasHndIndex());
        assert((blk->bbFlags & BBF_TRY_BEG) == 0);
        assert((blk->bbFlags & BBF_FUNCLET_BEG) == 0);
        assert((blk->bbFlags & BBF_REMOVED) == 0);
        assert(blk->bbCatchTyp == BBCT_NONE);
    }
#endif // DEBUG

    // Delete the EH table

    compHndBBtab = nullptr;
    compHndBBtabCount = 0;
    // Leave compHndBBtabAllocCount alone.

    // Renumber the basic blocks
    JITDUMP("\nRenumbering the basic blocks for fgRemoveEH\n");
    fgRenumberBlocks();

#ifdef DEBUG
    if  (verbose)
    {
        printf("\n*************** After fgRemoveEH()\n");
        fgDispBasicBlocks();
        fgDispHandlerTab();
        printf("\n");
    }
#endif
}

#endif // !FEATURE_EH


/*****************************************************************************
 *
 *  Sort the EH table if necessary.
 */

void          Compiler::fgSortEHTable()
{
    if (!fgNeedToSortEHTable)
        return;

    // Now, all fields of the EH table are set except for those that are related
    // to nesting. We need to first sort the table to ensure that an EH clause
    // appears before any try or handler that it is nested within. The CLI spec
    // requires this for nesting in 'try' clauses, but does not require this
    // for handler clauses. However, parts of the JIT do assume this ordering.
    //
    // For example:
    //
    //      try { // A
    //      } catch {
    //          try { // B
    //          } catch {
    //          }
    //      }
    //
    // In this case, the EH clauses for A and B have no required ordering: the
    // clause for either A or B can come first, despite B being nested within
    // the catch clause for A.
    //
    // The CLI spec, section 12.4.2.5 "Overview of exception handling", states:
    // "The ordering of the exception clauses in the Exception Handler Table is
    // important. If handlers are nested, the most deeply nested try blocks shall
    // come before the try blocks that enclose them."
    //
    // Note, in particular, that it doesn't say "shall come before the *handler*
    // blocks that enclose them".
    //
    // Also, the same section states, "When an exception occurs, the CLI searches
    // the array for the first protected block that (1) Protects a region including the
    // current instruction pointer and (2) Is a catch handler block and (3) Whose
    // filter wishes to handle the exception."
    //
    // Once again, nothing about the ordering of the catch blocks.
    //
    // A more complicated example:
    //
    //      try { // A
    //      } catch {
    //          try { // B
    //              try { // C
    //              } catch {
    //              }
    //          } catch {
    //          }
    //      }
    //
    // The clause for C must come before the clause for B, but the clause for A can
    // be anywhere. Thus, we could have these orderings: ACB, CAB, CBA.
    //
    // One more example:
    //
    //      try { // A
    //      } catch {
    //          try { // B
    //          } catch {
    //              try { // C
    //              } catch {
    //              }
    //          }
    //      }
    //
    // There is no ordering requirement: the EH clauses can come in any order.
    //
    // In Dev11 (Visual Studio 2012), x86 did not sort the EH table (it never had before)
    // but ARM did. It turns out not sorting the table can cause the EH table to incorrectly
    // set the bbHndIndex value in some nested cases, and that can lead to a security exploit
    // that allows the execution of arbitrary code. 

#ifdef DEBUG
    if (verbose)
    {
        printf("fgSortEHTable: Sorting EH table\n");
    }
#endif // DEBUG

    EHblkDsc *      xtab1;
    EHblkDsc *      xtab2;
    unsigned        xtabnum1, xtabnum2;

    for (xtabnum1 = 0, xtab1 = compHndBBtab;
         xtabnum1 < compHndBBtabCount;
         xtabnum1++  , xtab1++)
    {
        for (xtabnum2 = xtabnum1 + 1, xtab2 = xtab1 + 1;
             xtabnum2 < compHndBBtabCount;
             xtabnum2++             , xtab2++)
        {
            // If the nesting is wrong, swap them. The nesting is wrong if
            // EH region 2 is nested in the try, handler, or filter of EH region 1.
            // Note that due to proper nesting rules, if any of 2 is nested in
            // the try or handler or filter of 1, then all of 2 is nested.
            // We must be careful when comparing the offsets of the 'try' clause, because
            // for "mutually-protect" try/catch, the 'try' bodies will be identical.
            // For this reason, we use the handler region to check nesting. Note
            // that we must check both beginning and end: a nested region can have a 'try'
            // body that starts at the beginning of a handler. Thus, if we just compared the
            // handler begin offset, we might get confused and think it is nested.

            IL_OFFSET hndBegOff = xtab2->ebdHndBegOffset;
            IL_OFFSET hndEndOff = xtab2->ebdHndEndOffset;
            assert(hndEndOff > hndBegOff);

            if (
                (hndBegOff >= xtab1->ebdTryBegOffset && hndEndOff <= xtab1->ebdTryEndOffset)
                ||
                (hndBegOff >= xtab1->ebdHndBegOffset && hndEndOff <= xtab1->ebdHndEndOffset)
                ||
                (xtab1->HasFilter() &&
                 (hndBegOff >= xtab1->ebdFilterBegOffset && hndEndOff <= xtab1->ebdHndBegOffset))
                 // Note that end of filter is beginning of handler
               )
            {
#ifdef DEBUG
                if (verbose)
                {
                    printf("fgSortEHTable: Swapping out-of-order EH#%u and EH#%u\n",
                        xtabnum1, xtabnum2);
                }

                // Assert that the 'try' region is also nested in the same place as the handler

                IL_OFFSET tryBegOff = xtab2->ebdTryBegOffset;
                IL_OFFSET tryEndOff = xtab2->ebdTryEndOffset;
                assert(tryEndOff > tryBegOff);

                if (hndBegOff >= xtab1->ebdTryBegOffset && hndEndOff <= xtab1->ebdTryEndOffset)
                    assert(tryBegOff >= xtab1->ebdTryBegOffset && tryEndOff <= xtab1->ebdTryEndOffset);
                if (hndBegOff >= xtab1->ebdHndBegOffset && hndEndOff <= xtab1->ebdHndEndOffset)
                    assert(tryBegOff >= xtab1->ebdHndBegOffset && tryEndOff <= xtab1->ebdHndEndOffset);
                if (xtab1->HasFilter() && (hndBegOff >= xtab1->ebdFilterBegOffset && hndEndOff <= xtab1->ebdHndBegOffset))
                    assert(tryBegOff >= xtab1->ebdFilterBegOffset && tryEndOff <= xtab1->ebdHndBegOffset);
#endif // DEBUG

                // Swap them!
                EHblkDsc tmp = *xtab1;
                *xtab1 = *xtab2;
                *xtab2 = tmp;
            }
        }
    }
}


// fgNormalizeEH: Enforce the following invariants:
//
//   1. No block is both the first block of a handler and the first block of a try. In IL (and on entry
//      to this function), this can happen if the "try" is more nested than the handler.
//
//      For example, consider:
//
//               try1 ----------------- BB01
//               |                      BB02
//               |--------------------- BB03
//               handler1
//               |----- try2 ---------- BB04
//               |      |               BB05
//               |      handler2 ------ BB06
//               |      |               BB07
//               |      --------------- BB08
//               |--------------------- BB09
//
//      Thus, the start of handler1 and the start of try2 are the same block. We will transform this to:
//
//               try1 ----------------- BB01
//               |                      BB02
//               |--------------------- BB03
//               handler1 ------------- BB10 // empty block
//               |      try2 ---------- BB04
//               |      |               BB05
//               |      handler2 ------ BB06
//               |      |               BB07
//               |      --------------- BB08
//               |--------------------- BB09
//
//   2. No block is the first block of more than one try or handler region.
//      (Note that filters cannot have EH constructs nested within them, so there can be no nested try or
//      handler that shares the filter begin or last block. For try/filter/filter-handler constructs nested
//      within a try or handler region, note that the filter block cannot be the first block of the try,
//      nor can it be the first block of the handler, since you can't "fall into" a filter, which that situation
//      would require.)
//
//      For example, we will transform this:
//
//               try3   try2   try1
//               |---   |---   |---   BB01
//               |      |      |      BB02
//               |      |      |---   BB03
//               |      |             BB04
//               |      |------------ BB05
//               |                    BB06
//               |------------------- BB07
//
//      to this:
//
//               try3 -------------   BB08  // empty BBJ_NONE block
//               |      try2 ------   BB09  // empty BBJ_NONE block
//               |      |      try1
//               |      |      |---   BB01
//               |      |      |      BB02
//               |      |      |---   BB03
//               |      |             BB04
//               |      |------------ BB05
//               |                    BB06
//               |------------------- BB07
//
//      The benefit of this is that adding a block to an EH region will not require examining every EH region,
//      looking for possible shared "first" blocks to adjust. It also makes it easier to put code at the top
//      of a particular EH region, especially for loop optimizations.
//
//      These empty blocks (BB08, BB09) will generate no code (unless some code is subsequently placed into them),
//      and will have the same native code offset as BB01 after code is generated. There may be labels generated
//      for them, if they are branch targets, so it is possible to have multiple labels targeting the same native
//      code offset. The blocks will not be merged with the blocks they are split from, because they will have a
//      different EH region, and we don't merge blocks from two different EH regions.
//
//      In the example, if there are branches to BB01, we need to distribute them to BB01, BB08, or BB09, appropriately.
//      1. A branch from BB01/BB02/BB03 to BB01 will still go to BB01. Branching to BB09 or BB08 would not be legal,
//         since it would branch out of a try region.
//      2. A branch from BB04/BB05 to BB01 will instead branch to BB09. Branching to BB08 would not be legal. Note
//         that branching to BB01 would still be legal, so we have a choice. It makes the most sense to branch to BB09,
//         so the source and target of a branch are in the same EH region.
//      3. Similarly, a branch from BB06/BB07 to BB01 will go to BB08, even though branching to BB09 would be legal.
//      4. A branch from outside this loop (at the top-level) to BB01 will go to BB08. This is one case where the
//         source and target of the branch are not in the same EH region.
//
//      The EH nesting rules for IL branches are described in the ECMA spec section 12.4.2.8.2.7 "Branches" and
//      section 12.4.2.8.2.9 "Examples".
//
//      There is one exception to this normalization rule: we do not change "mutually protect" regions. These are cases
//      where two EH table entries have exactly the same 'try' region, used to implement C# "try / catch / catch".
//      The first handler appears by our nesting to be an "inner" handler, with ebdEnclosingTryIndex pointing to the
//      second one. It is not true nesting, though, since they both protect the same "try". Both the these EH table
//      entries must keep the same "try" region begin/last block pointers. A block in this "try" region has a try index
//      of the first ("most nested") EH table entry.
//
//   3. No block is the last block of more than one try or handler region. Again, as described above,
//      filters need not be considered.
//
//      For example, we will transform this:
//
//               try3 ----------------- BB01
//               |      try2 ---------- BB02
//               |      |      handler1 BB03
//               |      |      |        BB04
//               |----- |----- |------- BB05
//
//      (where all three try regions end at BB05) to this:
//
//               try3 ----------------- BB01
//               |      try2 ---------- BB02
//               |      |      handler1 BB03
//               |      |      |        BB04
//               |      |      |------- BB05
//               |      |-------------- BB06 // empty BBJ_NONE block
//               |--------------------- BB07 // empty BBJ_NONE block
//
//      No branches need to change: if something branched to BB05, it will still branch to BB05. If BB05 is a
//      BBJ_NONE block, then control flow will fall through the newly added blocks as well. If it is anything
//      else, it will retain that block branch type and BB06 and BB07 will be unreachable.
//
//      The benefit of this is, once again, to remove the need to consider every EH region when adding new blocks.
//
// Overall, a block can appear in the EH table exactly once: as the begin or last block of a single try, filter, or handler.
// There is one exception: for a single-block EH region, the block can appear as both the "begin" and "last" block of the try,
// or the "begin" and "last" block of the handler (note that filters don't have a "last" block stored, so this case doesn't apply.)
// (Note: we could remove this special case if we wanted, and if it helps anything, but it doesn't appear that it will help.)
//
// These invariants simplify a number of things. When inserting a new block into a region, it is not necessary to traverse
// the entire EH table looking to see if any EH region needs to be updated. You only ever need to update a single region (except
// for mutually-protect "try" regions).
//
// Also, for example, when we're trying to determine the successors of a block B1 that leads into a try T1, if a block B2
// violates invariant #3 by being the first block of both the handler of T1, and an enclosed try T2, inserting a block to
// enforce this invariant prevents us from having to consider the first block of T2's handler as a possible successor of B1.
// This is somewhat akin to breaking of "critical edges" in a flowgraph.

void Compiler::fgNormalizeEH()
{
    if (compHndBBtabCount == 0)
    {
        // No EH? Nothing to do.
        INDEBUG(fgNormalizeEHDone = true;)
        return;
    }

#ifdef DEBUG
    if  (verbose)
    {
        printf("*************** In fgNormalizeEH()\n");
        fgDispBasicBlocks();
        fgDispHandlerTab();
    }
#endif

    bool modified = false;

    // Case #1: Prevent the first block of a handler from also being the first block of a 'try'.
    if (fgNormalizeEHCase1())
    {
        modified = true;
    }

    // Case #2: Prevent any two EH regions from starting with the same block (after case #3, we only need to worry about 'try' blocks).
    if (fgNormalizeEHCase2())
    {
        modified = true;
    }

#if 0
    // Case 3 normalization is disabled. The JIT really doesn't like having extra empty blocks around, especially blocks that are unreachable.
    // There are lots of asserts when such things occur. We will re-evaluate whether we can do this normalization.
    // Note: there are cases in fgVerifyHandlerTab() that are also disabled to match this.

    // Case #3: Prevent any two EH regions from ending with the same block.
    if (fgNormalizeEHCase3())
    {
        modified = true;
    }

#endif // 0

    INDEBUG(fgNormalizeEHDone = true;)

    if (modified)
    {
        // If we computed the cheap preds, don't let them leak out, in case other code doesn't maintain them properly.
        if (fgCheapPredsValid)
        {
            fgRemovePreds();
        }

        JITDUMP("Added at least one basic block in fgNormalizeEH.\n");
        fgRenumberBlocks();
#ifdef DEBUG
        // fgRenumberBlocks() will dump all the blocks and the handler table, so we don't need to do it here.
        fgVerifyHandlerTab();
#endif
    }
    else
    {
        JITDUMP("No EH normalization performed.\n");
    }
}

bool Compiler::fgNormalizeEHCase1()
{
    bool modified = false;

    //
    // Case #1: Is the first block of a handler also the first block of any try?
    //
    // Do this as a separate loop from case #2 to simplify the logic for cases where we have both multiple identical 'try' begin
    // blocks as well as this case, e.g.:
    //     try {
    //     } finally { try { try {
    //         } catch {}
    //         } catch {}
    //     }
    // where the finally/try/try are all the same block.
    // We also do this before case #2, so when we get to case #2, we only need to worry about updating 'try' begin blocks (and
    // only those within the 'try' region's parents), not handler begin blocks, when we are inserting new header blocks.
    //

    for (unsigned XTnum = 0; XTnum < compHndBBtabCount; XTnum++)
    {
        EHblkDsc* eh = ehGetDsc(XTnum);

        BasicBlock* handlerStart = eh->ebdHndBeg;
        EHblkDsc* handlerStartContainingTry = ehGetBlockTryDsc(handlerStart);
        // If the handler start block is in a try, and is in fact the first block of that try...
        if (handlerStartContainingTry != NULL && handlerStartContainingTry->ebdTryBeg == handlerStart)
        {
            // ...then we want to insert an empty, non-removable block outside the try to be the new first block of the
            // handler.
            BasicBlock* newHndStart = bbNewBasicBlock(BBJ_NONE);
            fgInsertBBbefore(eh->ebdHndBeg, newHndStart);

#ifdef DEBUG
            if (verbose)
            {
                printf("Handler begin for EH#%02u and 'try' begin for EH%02u are the same block; inserted new BB%02u before BB%02u as new handler begin for EH#%u.\n",
                    XTnum, ehGetIndex(handlerStartContainingTry), newHndStart->bbNum, eh->ebdHndBeg->bbNum, XTnum);
            }
#endif // DEBUG

            // The new block is the new handler begin.
            eh->ebdHndBeg = newHndStart;

            // Try index is the same as the enclosing try, if any, of eh:
            if (eh->ebdEnclosingTryIndex == EHblkDsc::NO_ENCLOSING_INDEX)
            {
                newHndStart->clearTryIndex();
            }
            else
            {
                newHndStart->setTryIndex(eh->ebdEnclosingTryIndex);
            }
            newHndStart->setHndIndex(XTnum);
            newHndStart->bbCatchTyp = handlerStart->bbCatchTyp;
            handlerStart->bbCatchTyp = BBCT_NONE;   // Now handlerStart is no longer the start of a handler...
            newHndStart->bbCodeOffs = handlerStart->bbCodeOffs;
            newHndStart->bbCodeOffsEnd = newHndStart->bbCodeOffs;  // code size = 0. TODO: use BAD_IL_OFFSET instead?
            newHndStart->inheritWeight(handlerStart);
#if FEATURE_STACK_FP_X87
            newHndStart->bbFPStateX87 = codeGen->FlatFPAllocFPState(handlerStart->bbFPStateX87);
#endif // FEATURE_STACK_FP_X87
            newHndStart->bbFlags |= (BBF_DONT_REMOVE | BBF_INTERNAL | BBF_HAS_LABEL);
            modified = true;

#ifdef DEBUG
            if  (0&&verbose) // Normally this is way too verbose, but it is useful for debugging
            {
                printf("*************** fgNormalizeEH() made a change\n");
                fgDispBasicBlocks();
                fgDispHandlerTab();
            }
#endif // DEBUG
        }
    }

    return modified;
}

bool Compiler::fgNormalizeEHCase2()
{
    bool modified = false;

    //
    // Case #2: Make sure no two 'try' have the same begin block (except for mutually-protect regions).
    // Note that this can only happen for nested 'try' regions, so we only need to look through the
    // 'try' nesting hierarchy.
    //

    for (unsigned XTnum = 0; XTnum < compHndBBtabCount; XTnum++)
    {
        EHblkDsc* eh = ehGetDsc(XTnum);

        if (eh->ebdEnclosingTryIndex != EHblkDsc::NO_ENCLOSING_INDEX)
        {
            BasicBlock* tryStart = eh->ebdTryBeg;
            BasicBlock* insertBeforeBlk = tryStart; // If we need to insert new blocks, we insert before this block.

            // We need to keep track of the last "mutually protect" region so we can properly not add additional header blocks
            // to the second and subsequent mutually protect try blocks. We can't just keep track of the EH region
            // pointer, because we're updating the 'try' begin blocks as we go. So, we need to keep track of the
            // pre-update 'try' begin/last blocks themselves.
            BasicBlock* mutualTryBeg  = eh->ebdTryBeg;
            BasicBlock* mutualTryLast = eh->ebdTryLast;
            unsigned mutualProtectIndex = XTnum;

            EHblkDsc* ehOuter = eh;
            do
            {
                unsigned ehOuterTryIndex = ehOuter->ebdEnclosingTryIndex;
                ehOuter = ehGetDsc(ehOuterTryIndex);
                BasicBlock* outerTryStart = ehOuter->ebdTryBeg;
                if (outerTryStart == tryStart)
                {
                    // We found two EH regions with the same 'try' begin! Should we do something about it?

                    if (ehOuter->ebdIsSameTry(mutualTryBeg, mutualTryLast))
                    {
                        // Don't touch mutually-protect regions: their 'try' regions must remain identical!
                        // We want to continue the looping outwards, in case we have something like this:
                        //
                        //               try3   try2   try1
                        //               |---   |----  |----  BB01
                        //               |      |      |      BB02
                        //               |      |----  |----  BB03
                        //               |                    BB04
                        //               |------------------- BB05
                        //
                        // (Thus, try1 & try2 are mutually-protect 'try' regions from BB01 to BB03. They are nested inside try3,
                        // which also starts at BB01. The 'catch' clauses have been elided.)
                        // In this case, we'll decline to add a new header block for try2, but we will add a new one for try3, ending with:
                        //
                        //               try3   try2   try1
                        //               |------------------- BB06
                        //               |      |----  |----  BB01
                        //               |      |      |      BB02
                        //               |      |----  |----  BB03
                        //               |                    BB04
                        //               |------------------- BB05
                        //
                        // More complicated (yes, this is real):
                        //
                        // try {
                        //     try {
                        //         try {
                        //             try {
                        //                 try {
                        //                     try {
                        //                         try {
                        //                             try {
                        //                             }
                        //                             catch {} // mutually-protect set #1
                        //                             catch {}
                        //                         } finally {}
                        //                     }
                        //                     catch {} // mutually-protect set #2
                        //                     catch {}
                        //                     catch {}
                        //                 } finally {}
                        //             } catch {}
                        //         } finally {}
                        //     } catch {}
                        //  } finally {}
                        //
                        // In this case, all the 'try' start at the same block! Note that there are two sets of mutually-protect regions,
                        // separated by some nesting.

#ifdef DEBUG
                        if (verbose)
                        {
                            printf("Mutually protect regions EH#%u and EH#%u; leaving identical 'try' begin blocks.\n",
                                mutualProtectIndex, ehGetIndex(ehOuter));
                        }
#endif // DEBUG

                        // We still need to update the tryBeg, if something more nested already did that.
                        ehOuter->ebdTryBeg = insertBeforeBlk;
                    }
                    else
                    {
                        // We're in a new set of mutual protect regions, so don't compare against the original.
                        mutualTryBeg  = ehOuter->ebdTryBeg;
                        mutualTryLast = ehOuter->ebdTryLast;
                        mutualProtectIndex = ehOuterTryIndex;

                        // We're going to need the preds. We compute them here, before inserting the new block,
                        // so our logic to add/remove preds below is the same for both the first time preds are
                        // created and subsequent times.
                        if (!fgCheapPredsValid)
                        {
                            fgComputeCheapPreds();
                        }

                        // We've got multiple 'try' blocks starting at the same place!
                        // Add a new first 'try' block for 'ehOuter' that will be outside 'eh'.

                        BasicBlock* newTryStart = bbNewBasicBlock(BBJ_NONE);
                        fgInsertBBbefore(insertBeforeBlk, newTryStart);

#ifdef DEBUG
                        if (verbose)
                        {
                            printf("'try' begin for EH#%u and EH#%u are same block; inserted new BB%02u before BB%02u as new 'try' begin for EH#%u.\n",
                                ehOuterTryIndex, XTnum, newTryStart->bbNum, insertBeforeBlk->bbNum, ehOuterTryIndex);
                        }
#endif // DEBUG

                        // The new block is the new 'try' begin.
                        ehOuter->ebdTryBeg = newTryStart;

                        newTryStart->copyEHRegion(tryStart);            // Copy the EH region info
                        newTryStart->setTryIndex(ehOuterTryIndex);      // ... but overwrite the 'try' index
                        newTryStart->bbCatchTyp = BBCT_NONE;
                        newTryStart->bbCodeOffs = tryStart->bbCodeOffs;
                        newTryStart->bbCodeOffsEnd = newTryStart->bbCodeOffs;  // code size = 0. TODO: use BAD_IL_OFFSET instead?
                        newTryStart->inheritWeight(tryStart);
#if FEATURE_STACK_FP_X87
                        newTryStart->bbFPStateX87 = codeGen->FlatFPAllocFPState(tryStart->bbFPStateX87);
#endif // FEATURE_STACK_FP_X87

                        // Note that we don't need to clear any flags on the old try start, since it is still a 'try' start.
                        newTryStart->bbFlags |= (BBF_TRY_BEG | BBF_DONT_REMOVE | BBF_INTERNAL | BBF_HAS_LABEL);

                        // Now we need to split any flow edges targetting the old try begin block between the old
                        // and new block. Note that if we are handling a multiply-nested 'try', we may have already
                        // split the inner set. So we need to split again, from the most enclosing block that we've
                        // already created, namely, insertBeforeBlk.
                        //
                        // For example:
                        //
                        //               try3   try2   try1
                        //               |----  |----  |----  BB01
                        //               |      |      |      BB02
                        //               |      |      |----  BB03
                        //               |      |-----------  BB04
                        //               |------------------  BB05
                        //
                        // We'll loop twice, to create two header blocks, one for try2, and the second time for try3 (in that order).
                        // After the first loop, we have:
                        //
                        //               try3   try2   try1
                        //                      |----         BB06
                        //               |----  |      |----  BB01
                        //               |      |      |      BB02
                        //               |      |      |----  BB03
                        //               |      |-----------  BB04
                        //               |------------------  BB05
                        //
                        // And all the external edges have been changed to point at try2. On the next loop, we'll create a unique
                        // header block for try3, and split the edges between try2 and try3, leaving us with:
                        //
                        //               try3   try2   try1
                        //               |----                BB07
                        //               |      |----         BB06
                        //               |      |      |----  BB01
                        //               |      |      |      BB02
                        //               |      |      |----  BB03
                        //               |      |-----------  BB04
                        //               |------------------  BB05

                        BasicBlockList* nextPred; // we're going to update the pred list as we go, so we need to keep track of the next pred in case it gets deleted.
                        for (BasicBlockList* pred = insertBeforeBlk->bbCheapPreds; pred != nullptr; pred = nextPred)
                        {
                            nextPred = pred->next;

                            // Who gets this predecessor?
                            BasicBlock* predBlock = pred->block;

                            if (!BasicBlock::sameTryRegion(insertBeforeBlk, predBlock))
                            {
                                // Move the edge to target newTryStart instead of insertBeforeBlk.
                                fgAddCheapPred(newTryStart, predBlock);
                                fgRemoveCheapPred(insertBeforeBlk, predBlock);

                                // Now change the branch. If it was a BBJ_NONE fall-through to the top block, this will do nothing.
                                // Since cheap preds contains dups (for switch duplicates), we will call this once per dup.
                                fgReplaceJumpTarget(predBlock, newTryStart, insertBeforeBlk);

#ifdef DEBUG
                                if (verbose)
                                {
                                    printf("Redirect BB%02u target from BB%02u to BB%02u.\n",
                                        predBlock->bbNum, insertBeforeBlk->bbNum, newTryStart->bbNum);
                                }
#endif // DEBUG
                            }
                        }

                        // The new block (a fall-through block) is a new predecessor.
                        fgAddCheapPred(insertBeforeBlk, newTryStart);

                        // We don't need to update the tryBeg block of other EH regions here because we are looping
                        // outwards in enclosing try index order, and we'll get to them later.

                        // Move the insert block backwards, to the one we just inserted.
                        insertBeforeBlk = insertBeforeBlk->bbPrev;
                        assert(insertBeforeBlk == newTryStart);

                        modified = true;

#ifdef DEBUG
                        if  (0&&verbose) // Normally this is way too verbose, but it is useful for debugging
                        {
                            printf("*************** fgNormalizeEH() made a change\n");
                            fgDispBasicBlocks();
                            fgDispHandlerTab();
                        }
#endif // DEBUG
                    }
                }
                else
                {
                    // If the 'try' start block in the outer block isn't the same, then none of the more-enclosing
                    // try regions (if any) can have the same 'try' start block, so we're done.
                    // Note that we could have a situation like this:
                    //
                    //        try4   try3   try2   try1
                    //        |---   |---   |      |      BB01
                    //        |      |      |      |      BB02
                    //        |      |      |----  |----  BB03
                    //        |      |      |             BB04
                    //        |      |      |------------ BB05
                    //        |      |                    BB06
                    //        |      |------------------- BB07
                    //        |-------------------------- BB08
                    //
                    // (Thus, try1 & try2 start at BB03, and are nested inside try3 & try4, which both start at BB01.)
                    // In this case, we'll process try1 and try2, then break out. Later, we'll get to try3 and process it
                    // and try4.

                    break;
                }
            }
            while (ehOuter->ebdEnclosingTryIndex != EHblkDsc::NO_ENCLOSING_INDEX);
        }
    }

    return modified;
}

bool Compiler::fgNormalizeEHCase3()
{
    bool modified = false;

    //
    // Case #3: Make sure no two 'try' or handler regions have the same 'last' block (except for mutually protect 'try' regions).
    // As above, there has to be EH region nesting for this to occur. However, since we need to consider handlers, there are more
    // cases.
    //
    // There are four cases to consider:
    //      (1) try     nested in try
    //      (2) handler nested in try
    //      (3) try     nested in handler
    //      (4) handler nested in handler
    //
    // Note that, before funclet generation, it would be unusual, though legal IL, for a 'try' to come at the end
    // of an EH region (either 'try' or handler region), since that implies that its corresponding handler precedes it.
    // That will never happen in C#, but is legal in IL.
    //
    // Only one of these cases can happen. For example, if we have case (2), where a try/catch is nested in a 'try' and the
    // nested handler has the same 'last' block as the outer handler, then, due to nesting rules, the nested 'try' must also
    // be within the outer handler, and obviously cannot share the same 'last' block.
    //

    for (unsigned XTnum = 0; XTnum < compHndBBtabCount; XTnum++)
    {
        EHblkDsc* eh = ehGetDsc(XTnum);

        // Find the EH region 'eh' is most nested within, either 'try' or handler or none.
        bool outerIsTryRegion;
        unsigned ehOuterIndex = eh->ebdGetEnclosingRegionIndex(&outerIsTryRegion);

        if (ehOuterIndex != EHblkDsc::NO_ENCLOSING_INDEX)
        {
            EHblkDsc* ehInner = eh;         // This gets updated as we loop outwards in the EH nesting
            unsigned ehInnerIndex = XTnum;  // This gets updated as we loop outwards in the EH nesting
            bool innerIsTryRegion;

            EHblkDsc* ehOuter = ehGetDsc(ehOuterIndex);

            // Debugging: say what type of block we're updating.
            INDEBUG(const char* outerType = ""; const char* innerType = "";)

            // 'insertAfterBlk' is the place we will insert new "normalization" blocks. We don't know yet if we will
            // insert them after the innermost 'try' or handler's "last" block, so we set it to nullptr. Once we determine
            // the innermost region that is equivalent, we set this, and then update it incrementally as we loop outwards.
            BasicBlock* insertAfterBlk = nullptr;

            bool foundMatchingLastBlock = false;

            // This is set to 'false' for mutual protect regions for which we will not insert a normalization block.
            bool insertNormalizationBlock = true;

            // Keep track of what the 'try' index and handler index should be for any new normalization block that we insert.
            // If we have a sequence of alternating nested 'try' and handlers with the same 'last' block, we'll need to update
            // these as we go. For example:
            //      try { // EH#5
            //          ...
            //          catch { // EH#4
            //              ...
            //              try { // EH#3
            //                  ...
            //                  catch { // EH#2
            //                      ...
            //                      try { // EH#1
            //                          BB01 // try=1, hnd=2
            //      }   }   }   }   } // all the 'last' blocks are the same
            //
            // after normalization:
            // 
            //      try { // EH#5
            //          ...
            //          catch { // EH#4
            //              ...
            //              try { // EH#3
            //                  ...
            //                  catch { // EH#2
            //                      ...
            //                      try { // EH#1
            //                          BB01 // try=1, hnd=2
            //                      }
            //                      BB02 // try=3, hnd=2
            //                  }
            //                  BB03 // try=3, hnd=4
            //              }
            //              BB04 // try=5, hnd=4
            //          }
            //          BB05 // try=5, hnd=0 (no enclosing hnd)
            //      }
            //
            unsigned nextTryIndex = EHblkDsc::NO_ENCLOSING_INDEX; // Initialization only needed to quell compiler warnings.
            unsigned nextHndIndex = EHblkDsc::NO_ENCLOSING_INDEX;

            // We compare the outer region against the inner region's 'try' or handler, determined by the 'outerIsTryRegion'
            // variable. Once we decide that, we know exactly the 'last' pointer that we will use to compare against
            // all enclosing EH regions.
            //
            // For example, if we have these nested EH regions (omitting some corresponding try/catch clauses for each nesting level):
            //
            //      try {
            //          ...
            //          catch {
            //              ...
            //              try {
            //      }   }   } // all the 'last' blocks are the same
            //
            // then we determine that the innermost region we are going to compare against is the 'try' region. There's
            // no reason to compare against its handler region for any enclosing region (since it couldn't possibly
            // share a 'last' block with the enclosing region). However, there's no harm, either (and it simplifies
            // the code for the first set of comparisons to be the same as subsequent, more enclosing cases).
            BasicBlock* lastBlockPtrToCompare = nullptr;

            // We need to keep track of the last "mutual protect" region so we can properly not add additional blocks
            // to the second and subsequent mutual protect try blocks. We can't just keep track of the EH region
            // pointer, because we're updating the last blocks as we go. So, we need to keep track of the
            // pre-update 'try' begin/last blocks themselves. These only matter if the "last" blocks that match are
            // from two (or more) nested 'try' regions.
            BasicBlock* mutualTryBeg  = nullptr;
            BasicBlock* mutualTryLast = nullptr;

            if (outerIsTryRegion)
            {
                nextTryIndex = EHblkDsc::NO_ENCLOSING_INDEX; // unused, since the outer block is a 'try' region.

                // The outer (enclosing) region is a 'try'
                if (ehOuter->ebdTryLast == ehInner->ebdTryLast)
                {
                    // Case (1) try nested in try.
                    foundMatchingLastBlock = true;
                    INDEBUG(innerType = "try"; outerType = "try";)
                    insertAfterBlk = ehOuter->ebdTryLast;
                    lastBlockPtrToCompare = insertAfterBlk;

                    if (EHblkDsc::ebdIsSameTry(ehOuter, ehInner))
                    {
                        // We can't touch this 'try', since it's mutual protect.
#ifdef DEBUG
                        if (verbose)
                        {
                            printf("Mutual protect regions EH#%u and EH#%u; leaving identical 'try' last blocks.\n",
                                ehOuterIndex, ehInnerIndex);
                        }
#endif // DEBUG

                        insertNormalizationBlock = false;
                    }
                    else
                    {
                        nextHndIndex = ehInner->ebdTryLast->hasHndIndex() ? ehInner->ebdTryLast->getHndIndex() : EHblkDsc::NO_ENCLOSING_INDEX;
                    }
                }
                else if (ehOuter->ebdTryLast == ehInner->ebdHndLast)
                {
                    // Case (2) handler nested in try.
                    foundMatchingLastBlock = true;
                    INDEBUG(innerType = "handler"; outerType = "try";)
                    insertAfterBlk = ehOuter->ebdTryLast;
                    lastBlockPtrToCompare = insertAfterBlk;

                    assert(ehInner->ebdHndLast->getHndIndex() == ehInnerIndex);
                    nextHndIndex = ehInner->ebdEnclosingHndIndex;
                }
                else
                {
                    // No "last" pointers match!
                }

                if (foundMatchingLastBlock)
                {
                    // The outer might be part of a new set of mutual protect regions (if it isn't part of one already).
                    mutualTryBeg  = ehOuter->ebdTryBeg;
                    mutualTryLast = ehOuter->ebdTryLast;
                }
            }
            else
            {
                nextHndIndex = EHblkDsc::NO_ENCLOSING_INDEX; // unused, since the outer block is a handler region.

                // The outer (enclosing) region is a handler (note that it can't be a filter; there is no nesting within a filter).
                if (ehOuter->ebdHndLast == ehInner->ebdTryLast)
                {
                    // Case (3) try nested in handler.
                    foundMatchingLastBlock = true;
                    INDEBUG(innerType = "try"; outerType = "handler";)
                    insertAfterBlk = ehOuter->ebdHndLast;
                    lastBlockPtrToCompare = insertAfterBlk;

                    assert(ehInner->ebdTryLast->getTryIndex() == ehInnerIndex);
                    nextTryIndex = ehInner->ebdEnclosingTryIndex;
                }
                else if (ehOuter->ebdHndLast == ehInner->ebdHndLast)
                {
                    // Case (4) handler nested in handler.
                    foundMatchingLastBlock = true;
                    INDEBUG(innerType = "handler"; outerType = "handler";)
                    insertAfterBlk = ehOuter->ebdHndLast;
                    lastBlockPtrToCompare = insertAfterBlk;

                    nextTryIndex = ehInner->ebdTryLast->hasTryIndex() ? ehInner->ebdTryLast->getTryIndex() : EHblkDsc::NO_ENCLOSING_INDEX;
                }
                else
                {
                    // No "last" pointers match!
                }
            }

            while (foundMatchingLastBlock)
            {
                assert(lastBlockPtrToCompare != nullptr);
                assert(insertAfterBlk != nullptr);
                assert(ehOuterIndex != EHblkDsc::NO_ENCLOSING_INDEX);
                assert(ehOuter != nullptr);

                // Add a normalization block

                if (insertNormalizationBlock)
                {
                    // Add a new last block for 'ehOuter' that will be outside the EH region with which it encloses and shares a 'last' pointer

                    BasicBlock* newLast = bbNewBasicBlock(BBJ_NONE);
                    assert(insertAfterBlk != nullptr);
                    fgInsertBBafter(insertAfterBlk, newLast);

#ifdef DEBUG
                    if (verbose)
                    {
                        printf("last %s block for EH#%u and last %s block for EH#%u are same block; inserted new BB%02u after BB%02u as new last %s block for EH#%u.\n",
                            outerType, ehOuterIndex, innerType, ehInnerIndex, newLast->bbNum, insertAfterBlk->bbNum, outerType, ehOuterIndex);
                    }
#endif // DEBUG

                    if (outerIsTryRegion)
                    {
                        ehOuter->ebdTryLast = newLast;
                        newLast->setTryIndex(ehOuterIndex);
                        if (nextHndIndex == EHblkDsc::NO_ENCLOSING_INDEX)
                        {
                            newLast->clearHndIndex();
                        }
                        else
                        {
                            newLast->setHndIndex(nextHndIndex);
                        }
                    }
                    else
                    {
                        ehOuter->ebdHndLast = newLast;
                        if (nextTryIndex == EHblkDsc::NO_ENCLOSING_INDEX)
                        {
                            newLast->clearTryIndex();
                        }
                        else
                        {
                            newLast->setTryIndex(nextTryIndex);
                        }
                        newLast->setHndIndex(ehOuterIndex);
                    }

                    newLast->bbCatchTyp = BBCT_NONE;    // bbCatchTyp is only set on the first block of a handler, which is this not
                    newLast->bbCodeOffs = insertAfterBlk->bbCodeOffsEnd;
                    newLast->bbCodeOffsEnd = newLast->bbCodeOffs; // code size = 0. TODO: use BAD_IL_OFFSET instead?
                    newLast->inheritWeight(insertAfterBlk);
#if FEATURE_STACK_FP_X87
                    newLast->bbFPStateX87 = codeGen->FlatFPAllocFPState(insertAfterBlk->bbFPStateX87);
#endif // FEATURE_STACK_FP_X87

                    newLast->bbFlags |= BBF_INTERNAL;

                    // The new block (a fall-through block) is a new predecessor.
                    if (fgCheapPredsValid)
                    {
                        fgAddCheapPred(newLast, insertAfterBlk);
                    }

                    // Move the insert pointer. More enclosing equivalent 'last' blocks will be inserted after this.
                    insertAfterBlk = newLast;

                    modified = true;

#ifdef DEBUG
                    if  (verbose) // Normally this is way too verbose, but it is useful for debugging
                    {
                        printf("*************** fgNormalizeEH() made a change\n");
                        fgDispBasicBlocks();
                        fgDispHandlerTab();
                    }
#endif // DEBUG
                }

                // Now find the next outer enclosing EH region and see if it also shares the last block.
                foundMatchingLastBlock = false; // assume nothing will match
                ehInner = ehOuter;
                ehInnerIndex = ehOuterIndex;
                innerIsTryRegion = outerIsTryRegion;

                ehOuterIndex = ehOuter->ebdGetEnclosingRegionIndex(&outerIsTryRegion);   // Loop outwards in the EH nesting.
                if (ehOuterIndex != EHblkDsc::NO_ENCLOSING_INDEX)
                {
                    // There are more enclosing regions; check for equivalent 'last' pointers.

                    INDEBUG(innerType = outerType; outerType = "";)

                    ehOuter = ehGetDsc(ehOuterIndex);

                    insertNormalizationBlock = true; // assume it's not mutual protect

                    if (outerIsTryRegion)
                    {
                        nextTryIndex = EHblkDsc::NO_ENCLOSING_INDEX; // unused, since the outer block is a 'try' region.

                        // The outer (enclosing) region is a 'try'
                        if (ehOuter->ebdTryLast == lastBlockPtrToCompare)
                        {
                            // Case (1) and (2): try or handler nested in try.
                            foundMatchingLastBlock = true;
                            INDEBUG(outerType = "try";)

                            if (innerIsTryRegion && ehOuter->ebdIsSameTry(mutualTryBeg, mutualTryLast))
                            {
                                // We can't touch this 'try', since it's mutual protect.
#ifdef DEBUG
                                if (verbose)
                                {
                                    printf("Mutual protect regions EH#%u and EH#%u; leaving identical 'try' last blocks.\n",
                                        ehOuterIndex, ehInnerIndex);
                                }
#endif // DEBUG

                                insertNormalizationBlock = false;

                                // We still need to update the 'last' pointer, in case someone inserted a normalization block before
                                // the start of the mutual protect 'try' region.
                                ehOuter->ebdTryLast = insertAfterBlk;
                            }
                            else
                            {
                                if (innerIsTryRegion)
                                {
                                    // Case (1) try nested in try.
                                    nextHndIndex = ehInner->ebdTryLast->hasHndIndex() ? ehInner->ebdTryLast->getHndIndex() : EHblkDsc::NO_ENCLOSING_INDEX;
                                }
                                else
                                {
                                    // Case (2) handler nested in try.
                                    assert(ehInner->ebdHndLast->getHndIndex() == ehInnerIndex);
                                    nextHndIndex = ehInner->ebdEnclosingHndIndex;
                                }
                            }

                            // The outer might be part of a new set of mutual protect regions (if it isn't part of one already).
                            mutualTryBeg  = ehOuter->ebdTryBeg;
                            mutualTryLast = ehOuter->ebdTryLast;
                        }
                    }
                    else
                    {
                        nextHndIndex = EHblkDsc::NO_ENCLOSING_INDEX; // unused, since the outer block is a handler region.

                        // The outer (enclosing) region is a handler (note that it can't be a filter; there is no nesting within a filter).
                        if (ehOuter->ebdHndLast == lastBlockPtrToCompare)
                        {
                            // Case (3) and (4): try nested in try or handler.
                            foundMatchingLastBlock = true;
                            INDEBUG(outerType = "handler";)

                            if (innerIsTryRegion)
                            {
                                // Case (3) try nested in handler.
                                assert(ehInner->ebdTryLast->getTryIndex() == ehInnerIndex);
                                nextTryIndex = ehInner->ebdEnclosingTryIndex;
                            }
                            else
                            {
                                // Case (4) handler nested in handler.
                                nextTryIndex = ehInner->ebdTryLast->hasTryIndex() ? ehInner->ebdTryLast->getTryIndex() : EHblkDsc::NO_ENCLOSING_INDEX;
                            }
                        }
                    }
                }

                // If we get to here and foundMatchingLastBlock is false, then the inner and outer region don't share any
                // 'last' blocks, so we're done. Note that we could have a situation like this:
                //
                //        try4   try3   try2   try1
                //        |----  |      |      |      BB01
                //        |      |----  |      |      BB02
                //        |      |      |----  |      BB03
                //        |      |      |      |----- BB04
                //        |      |      |----- |----- BB05
                //        |----  |------------------- BB06
                //
                // (Thus, try1 & try2 end at BB05, and are nested inside try3 & try4, which both end at BB06.)
                // In this case, we'll process try1 and try2, then break out. Later, as we iterate through the EH table,
                // we'll get to try3 and process it and try4.

            } // end while (foundMatchingLastBlock)
        } // if (ehOuterIndex != EHblkDsc::NO_ENCLOSING_INDEX)
    } // EH table iteration

    return modified;
}


/*****************************************************************************/
#ifdef DEBUG

void                Compiler::dispIncomingEHClause(unsigned num, const CORINFO_EH_CLAUSE& clause)
{
    printf("EH clause #%u:\n", num);
    printf("  Flags:         0x%x", clause.Flags);

    // Note: the flags field is kind of weird. It should be compared for equality
    // to determine the type of clause, even though it looks like a bitfield. In
    // Particular, CORINFO_EH_CLAUSE_NONE is zero, so you can't use "&" to check it.
    const DWORD CORINFO_EH_CLAUSE_TYPE_MASK = 0x7;
    switch (clause.Flags & CORINFO_EH_CLAUSE_TYPE_MASK)
    {
    case CORINFO_EH_CLAUSE_NONE:
        printf(" (catch)");
        break;
    case CORINFO_EH_CLAUSE_FILTER:
        printf(" (filter)");
        break;
    case CORINFO_EH_CLAUSE_FINALLY:
        printf(" (finally)");
        break;
    case CORINFO_EH_CLAUSE_FAULT:
        printf(" (fault)");
        break;
    default:
        printf(" (UNKNOWN type %u!)", clause.Flags & CORINFO_EH_CLAUSE_TYPE_MASK);
        break;
    }
    if (clause.Flags & ~CORINFO_EH_CLAUSE_TYPE_MASK)
    {
        printf(" (extra unknown bits: 0x%x)", clause.Flags & ~CORINFO_EH_CLAUSE_TYPE_MASK);
    }
    printf("\n");

    printf("  TryOffset:     0x%x\n", clause.TryOffset);
    printf("  TryLength:     0x%x\n", clause.TryLength);
    printf("  HandlerOffset: 0x%x\n", clause.HandlerOffset);
    printf("  HandlerLength: 0x%x\n", clause.HandlerLength);
    if (clause.Flags & CORINFO_EH_CLAUSE_FILTER)
    {
        printf("  FilterOffset:  0x%x\n", clause.FilterOffset);
    }
    else
    {
        printf("  ClassToken:    0x%x\n", clause.ClassToken);
    }
}

void                Compiler::dispOutgoingEHClause(unsigned num, const CORINFO_EH_CLAUSE& clause)
{
    if (opts.dspDiffable)
    {
        /* (( brace matching editor workaround to compensate for the following line */
        printf("EH#%u: try [%s..%s) handled by [%s..%s) ",
            num,
            genEmitter->emitOffsetToLabel(clause.TryOffset),
            genEmitter->emitOffsetToLabel(clause.TryLength),
            genEmitter->emitOffsetToLabel(clause.HandlerOffset),
            genEmitter->emitOffsetToLabel(clause.HandlerLength));
    }
    else
    {
        /* (( brace matching editor workaround to compensate for the following line */
        printf("EH#%u: try [%04X..%04X) handled by [%04X..%04X) ",
            num,
            dspOffset(clause.TryOffset),
            dspOffset(clause.TryLength),
            dspOffset(clause.HandlerOffset),
            dspOffset(clause.HandlerLength));
    }

    // Note: the flags field is kind of weird. It should be compared for equality
    // to determine the type of clause, even though it looks like a bitfield. In
    // Particular, CORINFO_EH_CLAUSE_NONE is zero, so you can "&" to check it.
    // You do need to mask off the bits, though, because COR_ILEXCEPTION_CLAUSE_DUPLICATED
    // is and'ed in.
    const DWORD CORINFO_EH_CLAUSE_TYPE_MASK = 0x7;
    switch (clause.Flags & CORINFO_EH_CLAUSE_TYPE_MASK)
    {
    case CORINFO_EH_CLAUSE_NONE:
        printf("(class: %04X)", clause.ClassToken);
        break;
    case CORINFO_EH_CLAUSE_FILTER:
        if (opts.dspDiffable)
        {
            /* ( brace matching editor workaround to compensate for the following line */
            printf("filter at [%s..%s)",
                genEmitter->emitOffsetToLabel(clause.ClassToken),
                genEmitter->emitOffsetToLabel(clause.HandlerOffset));
        }
        else
        {
            /* ( brace matching editor workaround to compensate for the following line */
            printf("filter at [%04X..%04X)", dspOffset(clause.ClassToken), dspOffset(clause.HandlerOffset));
        }
        break;
    case CORINFO_EH_CLAUSE_FINALLY:
        printf("(finally)");
        break;
    case CORINFO_EH_CLAUSE_FAULT:
        printf("(fault)");
        break;
    default:
        printf("(UNKNOWN type %u!)", clause.Flags & CORINFO_EH_CLAUSE_TYPE_MASK);
        assert(!"unknown type");
        break;
    }

    if ((clause.TryOffset == clause.TryLength) &&
        (clause.TryOffset == clause.HandlerOffset) &&
        ((clause.Flags & (COR_ILEXCEPTION_CLAUSE_DUPLICATED | COR_ILEXCEPTION_CLAUSE_FINALLY)) == (COR_ILEXCEPTION_CLAUSE_DUPLICATED | COR_ILEXCEPTION_CLAUSE_FINALLY)))
    {
        printf(" cloned finally");
    }
    else if (clause.Flags & COR_ILEXCEPTION_CLAUSE_DUPLICATED)
    {
        printf(" duplicated");
    }
    printf("\n");
}

/*****************************************************************************/

void                Compiler::fgVerifyHandlerTab()
{
    if (compIsForInlining())
    {
        // We don't inline functions with EH. Don't bother verifying the EH table in the inlinee Compiler.
        return;
    }

    if (compHndBBtabCount == 0)
    {
        return;
    }

    // Did we do the normalization that prevents the first block of a handler from being a 'try' block (case 1)?
    bool handlerBegIsTryBegNormalizationDone = fgNormalizeEHDone;

    // Did we do the normalization that prevents multiple EH regions (namely, 'try' blocks) from starting on the same block (case 2)?
    bool multipleBegBlockNormalizationDone = fgNormalizeEHDone;

    // Did we do the normalization that prevents multiple EH regions ('try' or handler blocks) from ending on the same block (case 3)?
    bool multipleLastBlockNormalizationDone = false; // Currently disabled

    assert(compHndBBtabCount <= compHndBBtabAllocCount);

    unsigned        XTnum;
    EHblkDsc*       HBtab;

    for (XTnum = 0, HBtab = compHndBBtab;
         XTnum < compHndBBtabCount;
         XTnum++,   HBtab++)
    {
        assert(HBtab->ebdTryBeg  != nullptr);
        assert(HBtab->ebdTryLast != nullptr);
        assert(HBtab->ebdHndBeg  != nullptr);
        assert(HBtab->ebdHndLast != nullptr);

        assert(HBtab->ebdTryBeg->bbFlags & BBF_TRY_BEG);
        assert(HBtab->ebdTryBeg->bbFlags & BBF_DONT_REMOVE);
        assert(HBtab->ebdTryBeg->bbFlags & BBF_HAS_LABEL);

        assert(HBtab->ebdHndBeg->bbFlags & BBF_DONT_REMOVE);
        assert(HBtab->ebdHndBeg->bbFlags & BBF_HAS_LABEL);

        assert((HBtab->ebdTryBeg->bbFlags  & BBF_REMOVED) == 0);
        assert((HBtab->ebdTryLast->bbFlags & BBF_REMOVED) == 0);
        assert((HBtab->ebdHndBeg->bbFlags  & BBF_REMOVED) == 0);
        assert((HBtab->ebdHndLast->bbFlags & BBF_REMOVED) == 0);

        if (HBtab->HasFilter())
        {
            assert(HBtab->ebdFilter != nullptr);
            assert(HBtab->ebdFilter->bbFlags & BBF_DONT_REMOVE);
            assert((HBtab->ebdFilter->bbFlags & BBF_REMOVED) == 0);
        }

#if FEATURE_EH_FUNCLETS
        if (fgFuncletsCreated)
        {
            assert(HBtab->ebdHndBeg->bbFlags & BBF_FUNCLET_BEG);

            if (HBtab->HasFilter())
            {
                assert(HBtab->ebdFilter->bbFlags & BBF_FUNCLET_BEG);
            }
        }
#endif // FEATURE_EH_FUNCLETS

    }

    // I want to assert things about the relative ordering of blocks in the block list using
    // block number, but I don't want to renumber the basic blocks, which might cause a difference
    // between debug and non-debug code paths. So, create a renumbered block mapping: map the
    // existing block number to a renumbered block number that is ordered by block list order.

    unsigned bbNumMax = compIsForInlining() ? impInlineInfo->InlinerCompiler->fgBBNumMax : fgBBNumMax;

    // blockNumMap[old block number] => new block number
    size_t blockNumBytes = (bbNumMax + 1) * sizeof(unsigned);
    unsigned* blockNumMap = (unsigned*)_alloca(blockNumBytes);
    memset(blockNumMap, 0, blockNumBytes);

    BasicBlock* block;
    unsigned newBBnum = 1;
    for (block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        assert((block->bbFlags & BBF_REMOVED) == 0);
        assert(1 <= block->bbNum && block->bbNum <= bbNumMax);
        assert(blockNumMap[block->bbNum] == 0);  // If this fails, we have two blocks with the same block number.
        blockNumMap[block->bbNum] = newBBnum++;
    }
    // Note that there may be some blockNumMap[x] == 0, for a block number 'x' that has been deleted, if the blocks
    // haven't been renumbered since the deletion.

#if 0 // Useful for debugging, but don't want to put this in the dump all the time
    if (verbose)
    {
        printf("fgVerifyHandlerTab block number map: BB current => BB new\n");
        for (unsigned i = 0; i <= bbNumMax; i++)
        {
            if (blockNumMap[i] != 0)
            {
                printf("BB%02u => BB%02u\n", i, blockNumMap[i]);
            }
        }
    }
#endif

    // To verify that bbCatchTyp is set properly on all blocks, and that some BBF_* flags are only set on the first block
    // of 'try' or handlers, create two bool arrays indexed by block number: one for the set of blocks that are the beginning
    // blocks of 'try' regions, and one for blocks that are the beginning of handlers (including filters). Note that since
    // this checking function runs before EH normalization, we have to handle the case where blocks can be both the beginning
    // of a 'try' as well as the beginning of a handler. After we've iterated over the EH table, loop
    // over all blocks and verify that only handler begin blocks have bbCatchTyp == BBCT_NONE, and some other things.

    size_t blockBoolSetBytes = (bbNumMax + 1) * sizeof(bool);
    bool* blockTryBegSet = (bool*)_alloca(blockBoolSetBytes);
    bool* blockHndBegSet = (bool*)_alloca(blockBoolSetBytes);
    for (unsigned i = 0; i <= bbNumMax; i++)
    {
        blockTryBegSet[i] = false;
        blockHndBegSet[i] = false;
    }

#if FEATURE_EH_FUNCLETS
    bool isLegalFirstFunclet = false;
    unsigned bbNumFirstFunclet = 0;

    if (fgFuncletsCreated)
    {
        // Assert some things about the "first funclet block" pointer.
        assert(fgFirstFuncletBB != nullptr);
        assert((fgFirstFuncletBB->bbFlags & BBF_REMOVED) == 0);
        bbNumFirstFunclet = blockNumMap[fgFirstFuncletBB->bbNum];
        assert(bbNumFirstFunclet != 0);
    }
    else
    {
        assert(fgFirstFuncletBB == nullptr);
    }
#endif // FEATURE_EH_FUNCLETS

    for (XTnum = 0, HBtab = compHndBBtab;
         XTnum < compHndBBtabCount;
         XTnum++,   HBtab++)
    {
        unsigned bbNumTryBeg  = blockNumMap[HBtab->ebdTryBeg->bbNum];
        unsigned bbNumTryLast = blockNumMap[HBtab->ebdTryLast->bbNum];
        unsigned bbNumHndBeg  = blockNumMap[HBtab->ebdHndBeg->bbNum];
        unsigned bbNumHndLast = blockNumMap[HBtab->ebdHndLast->bbNum];
        unsigned bbNumFilter = 0; // This should never get used except under "if (HBtab->HasFilter())"
        if (HBtab->HasFilter())
        {
            bbNumFilter = blockNumMap[HBtab->ebdFilter->bbNum];
        }

        // Assert that the EH blocks are in the main block list
        assert(bbNumTryBeg  != 0);
        assert(bbNumTryLast != 0);
        assert(bbNumHndBeg  != 0);
        assert(bbNumHndLast != 0);
        if (HBtab->HasFilter())
        {
            assert(bbNumFilter != 0);
        }

        // Check relative ordering of the 'beg' and 'last' blocks. Note that in IL (and in our initial block list)
        // there is no required ordering between the 'try' and handler regions: the handler might come first!
        // After funclets have been created, all the handler blocks come in sequence at the end of the
        // function (this is checked below, with checks for the first funclet block). Note that a handler
        // might contain a nested 'try', which will also then be in the "funclet region".
        // Also, the 'try' and handler regions do not need to be adjacent.
        assert(bbNumTryBeg <= bbNumTryLast);
        assert(bbNumHndBeg <= bbNumHndLast);
        if (HBtab->HasFilter())
        {
            // Since the filter block must be different from the handler, this condition is "<", not "<=".
            assert(bbNumFilter < bbNumHndBeg);
        }

        // The EH regions are disjoint: the handler (including the filter, if applicable) is strictly before or after the 'try'.
        if (HBtab->HasFilter())
        {
            assert((bbNumHndLast < bbNumTryBeg) || (bbNumTryLast < bbNumFilter));
        }
        else
        {
            assert((bbNumHndLast < bbNumTryBeg) || (bbNumTryLast < bbNumHndBeg));
        }

#if FEATURE_EH_FUNCLETS
        // If funclets have been created, check the first funclet block. The first funclet block must be the
        // first block of a filter or handler. All filter/handler blocks must come after it.
        // Note that 'try' blocks might come either before or after it. If after, they will be nested within
        // a handler. If before, they might be nested within a try, but not within a handler.

        if (fgFuncletsCreated)
        {
            if (bbNumTryLast < bbNumFirstFunclet)
            {
                // This EH region can't be nested in a handler, or else it would be in the funclet region.
                assert(HBtab->ebdEnclosingHndIndex == EHblkDsc::NO_ENCLOSING_INDEX);
            }
            else
            {
                // The last block of the 'try' is in the funclet region; make sure the whole thing is.
                if (multipleBegBlockNormalizationDone)
                {
                    assert(bbNumTryBeg > bbNumFirstFunclet); // ">" because a 'try' can't be the first block of a handler (by EH normalization).
                }
                else
                {
                    assert(bbNumTryBeg >= bbNumFirstFunclet);
                }

                // This EH region must be nested in a handler.
                assert(HBtab->ebdEnclosingHndIndex != EHblkDsc::NO_ENCLOSING_INDEX);
            }

            if (HBtab->HasFilter())
            {
                assert(bbNumFirstFunclet <= bbNumFilter);
                if (fgFirstFuncletBB == HBtab->ebdFilter)
                {
                    assert(!isLegalFirstFunclet); // We can't have already found a matching block for the first funclet.
                    isLegalFirstFunclet = true;
                }
            }
            else
            {
                assert(bbNumFirstFunclet <= bbNumHndBeg);
                if (fgFirstFuncletBB == HBtab->ebdHndBeg)
                {
                    assert(!isLegalFirstFunclet); // We can't have already found a matching block for the first funclet.
                    isLegalFirstFunclet = true;
                }
            }
        }
#endif // FEATURE_EH_FUNCLETS

        // Check the 'try' region nesting, using ebdEnclosingTryIndex.
        // Only check one level of nesting, since we'll check the outer EH region (and its nesting) when we get to it later.

        if (HBtab->ebdEnclosingTryIndex != EHblkDsc::NO_ENCLOSING_INDEX)
        {
            assert(HBtab->ebdEnclosingTryIndex > XTnum); // The enclosing region must come after this one in the table
            EHblkDsc* HBtabOuter = ehGetDsc(HBtab->ebdEnclosingTryIndex);
            unsigned bbNumOuterTryBeg  = blockNumMap[HBtabOuter->ebdTryBeg->bbNum];
            unsigned bbNumOuterTryLast = blockNumMap[HBtabOuter->ebdTryLast->bbNum];

            // A few basic asserts (that will also get covered later, when this outer region gets handled).
            assert(bbNumOuterTryBeg  != 0);
            assert(bbNumOuterTryLast != 0);
            assert(bbNumOuterTryBeg <= bbNumOuterTryLast);

            if (!EHblkDsc::ebdIsSameTry(HBtab, HBtabOuter))
            {
                // If it's not a mutually protect region, then the outer 'try' must completely lexically contain all the blocks
                // in the nested EH region. However, if funclets have been created, this is no longer true, since this 'try' might
                // be in a handler that is pulled out to the funclet region, while the outer 'try' remains in the main function
                // region.

#if FEATURE_EH_FUNCLETS
                if (fgFuncletsCreated)
                {
                    // If both the 'try' region and the outer 'try' region are in the main function area, then we can do the normal
                    // nesting check. Otherwise, it's harder to find a useful assert to make about their relationship.
                    if ((bbNumTryLast < bbNumFirstFunclet) &&
                        (bbNumOuterTryLast < bbNumFirstFunclet))
                    {
                        if (multipleBegBlockNormalizationDone)
                        {
                            assert(bbNumOuterTryBeg < bbNumTryBeg);     // Two 'try' regions can't start at the same block (by EH normalization).
                        }
                        else
                        {
                            assert(bbNumOuterTryBeg <= bbNumTryBeg);
                        }
                        if (multipleLastBlockNormalizationDone)
                        {
                            assert(bbNumTryLast < bbNumOuterTryLast);   // Two 'try' regions can't end at the same block (by EH normalization).
                        }
                        else
                        {
                            assert(bbNumTryLast <= bbNumOuterTryLast);
                        }
                    }

                    // With funclets, all we can say about the handler blocks is that they are disjoint from the enclosing try.
                    assert((bbNumHndLast < bbNumOuterTryBeg) || (bbNumOuterTryLast < bbNumHndBeg));
                }
                else
#endif // FEATURE_EH_FUNCLETS
                {
                    if (multipleBegBlockNormalizationDone)
                    {
                        assert(bbNumOuterTryBeg < bbNumTryBeg);     // Two 'try' regions can't start at the same block (by EH normalization).
                    }
                    else
                    {
                        assert(bbNumOuterTryBeg <= bbNumTryBeg);
                    }
                    assert(bbNumOuterTryBeg < bbNumHndBeg);         // An inner handler can never start at the same block as an outer 'try' (by IL rules).
                    if (multipleLastBlockNormalizationDone)
                    {
                        // An inner EH region can't share a 'last' block with the outer 'try' (by EH normalization).
                        assert(bbNumTryLast < bbNumOuterTryLast);
                        assert(bbNumHndLast < bbNumOuterTryLast);
                    }
                    else
                    {
                        assert(bbNumTryLast <= bbNumOuterTryLast);
                        assert(bbNumHndLast <= bbNumOuterTryLast);
                    }
                }
            }
        }

        // Check the handler region nesting, using ebdEnclosingHndIndex.
        // Only check one level of nesting, since we'll check the outer EH region (and its nesting) when we get to it later.

        if (HBtab->ebdEnclosingHndIndex != EHblkDsc::NO_ENCLOSING_INDEX)
        {
            assert(HBtab->ebdEnclosingHndIndex > XTnum); // The enclosing region must come after this one in the table
            EHblkDsc* HBtabOuter = ehGetDsc(HBtab->ebdEnclosingHndIndex);
            unsigned bbNumOuterHndBeg  = blockNumMap[HBtabOuter->ebdHndBeg->bbNum];
            unsigned bbNumOuterHndLast = blockNumMap[HBtabOuter->ebdHndLast->bbNum];

            // A few basic asserts (that will also get covered later, when this outer regions gets handled).
            assert(bbNumOuterHndBeg  != 0);
            assert(bbNumOuterHndLast != 0);
            assert(bbNumOuterHndBeg <= bbNumOuterHndLast);

            // The outer handler must completely contain all the blocks in the EH region nested within it. However, if funclets have been created,
            // it's harder to make any relationship asserts about the order of nested handlers, which also have been made into funclets.

#if FEATURE_EH_FUNCLETS
            if (fgFuncletsCreated)
            {
                if (handlerBegIsTryBegNormalizationDone)
                {
                    assert(bbNumOuterHndBeg < bbNumTryBeg);     // An inner 'try' can't start at the same block as an outer handler (by EH normalization).
                }
                else
                {
                    assert(bbNumOuterHndBeg <= bbNumTryBeg);
                }
                if (multipleLastBlockNormalizationDone)
                {
                    assert(bbNumTryLast < bbNumOuterHndLast);   // An inner 'try' can't end at the same block as an outer handler (by EH normalization).
                }
                else
                {
                    assert(bbNumTryLast <= bbNumOuterHndLast);
                }

                // With funclets, all we can say about the handler blocks is that they are disjoint from the enclosing handler.
                assert((bbNumHndLast < bbNumOuterHndBeg) || (bbNumOuterHndLast < bbNumHndBeg));
            }
            else
#endif // FEATURE_EH_FUNCLETS
            {
                if (handlerBegIsTryBegNormalizationDone)
                {
                    assert(bbNumOuterHndBeg < bbNumTryBeg);     // An inner 'try' can't start at the same block as an outer handler (by EH normalization).
                }
                else
                {
                    assert(bbNumOuterHndBeg <= bbNumTryBeg);
                }
                assert(bbNumOuterHndBeg < bbNumHndBeg);         // An inner handler can never start at the same block as an outer handler (by IL rules).
                if (multipleLastBlockNormalizationDone)
                {
                    // An inner EH region can't share a 'last' block with the outer handler (by EH normalization).
                    assert(bbNumTryLast < bbNumOuterHndLast);
                    assert(bbNumHndLast < bbNumOuterHndLast);
                }
                else
                {
                    assert(bbNumTryLast <= bbNumOuterHndLast);
                    assert(bbNumHndLast <= bbNumOuterHndLast);
                }
            }
        }

        // Set up blockTryBegSet and blockHndBegSet.
        // We might want to have this assert:
        //    if (fgNormalizeEHDone) assert(!blockTryBegSet[HBtab->ebdTryBeg->bbNum]);
        // But we can't, because if we have mutually-protect 'try' regions, we'll see exactly the same tryBeg twice (or more).
        blockTryBegSet[HBtab->ebdTryBeg->bbNum] = true;
        assert(!blockHndBegSet[HBtab->ebdHndBeg->bbNum]);
        blockHndBegSet[HBtab->ebdHndBeg->bbNum] = true;

        if (HBtab->HasFilter())
        {
            assert(HBtab->ebdFilter->bbCatchTyp == BBCT_FILTER);
            assert(!blockHndBegSet[HBtab->ebdFilter->bbNum]);
            blockHndBegSet[HBtab->ebdFilter->bbNum] = true;
        }

        // Check the block bbCatchTyp for this EH region's filter and handler.

        if (HBtab->HasFilter())
        {
            assert(HBtab->ebdHndBeg->bbCatchTyp == BBCT_FILTER_HANDLER);
        }
        else if (HBtab->HasCatchHandler())
        {
            assert((HBtab->ebdHndBeg->bbCatchTyp != BBCT_NONE) && 
                   (HBtab->ebdHndBeg->bbCatchTyp != BBCT_FAULT) && 
                   (HBtab->ebdHndBeg->bbCatchTyp != BBCT_FINALLY) && 
                   (HBtab->ebdHndBeg->bbCatchTyp != BBCT_FILTER) && 
                   (HBtab->ebdHndBeg->bbCatchTyp != BBCT_FILTER_HANDLER));
        }
        else if (HBtab->HasFaultHandler())
        {
            assert(HBtab->ebdHndBeg->bbCatchTyp == BBCT_FAULT);
        }
        else if (HBtab->HasFinallyHandler())
        {
            assert(HBtab->ebdHndBeg->bbCatchTyp == BBCT_FINALLY);
        }
    }

#if FEATURE_EH_FUNCLETS
    assert(!fgFuncletsCreated || isLegalFirstFunclet);
#endif // FEATURE_EH_FUNCLETS

    // Figure out what 'try' and handler index each basic block should have,
    // and check the blocks against that. This depends on the more nested EH
    // clauses appearing first. For duplicate clauses, we use the duplicate
    // clause 'try' region to set the try index, since a handler that has
    // been pulled out of an enclosing 'try' wouldn't have had its try index
    // otherwise set. The duplicate clause handler is truly a duplicate of
    // a previously processed handler, so we ignore it.

    size_t blockIndexBytes = (bbNumMax + 1) * sizeof(unsigned short);
    unsigned short* blockTryIndex = (unsigned short*)_alloca(blockIndexBytes);
    unsigned short* blockHndIndex = (unsigned short*)_alloca(blockIndexBytes);
    memset(blockTryIndex, 0, blockIndexBytes);
    memset(blockHndIndex, 0, blockIndexBytes);

    for (XTnum = 0, HBtab = compHndBBtab;
         XTnum < compHndBBtabCount;
         XTnum++,   HBtab++)
    {
        BasicBlock* blockEnd;

        for (block = HBtab->ebdTryBeg, blockEnd = HBtab->ebdTryLast->bbNext; block != blockEnd; block = block->bbNext)
        {
            if (blockTryIndex[block->bbNum] == 0)
            {
                blockTryIndex[block->bbNum] = (unsigned short)(XTnum + 1);
            }
        }

        for (block = (HBtab->HasFilter() ? HBtab->ebdFilter : HBtab->ebdHndBeg), blockEnd = HBtab->ebdHndLast->bbNext; block != blockEnd; block = block->bbNext)
        {
            if (blockHndIndex[block->bbNum] == 0)
            {
                blockHndIndex[block->bbNum] = (unsigned short)(XTnum + 1);
            }
        }
    }

#if FEATURE_EH_FUNCLETS
    if (fgFuncletsCreated)
    {
        // Mark all the funclet 'try' indices correctly, since they do not exist in the linear 'try' region that
        // we looped over above. This is similar to duplicate clause logic, but we only need to look at the most
        // nested enclosing try index, not the entire set of enclosing try indices, since that is what we store
        // on the block.
        for (XTnum = 0, HBtab = compHndBBtab;
             XTnum < compHndBBtabCount;
             XTnum++,   HBtab++)
        {
            unsigned enclosingTryIndex = ehTrueEnclosingTryIndexIL(XTnum); // find the true enclosing try index, ignoring 'mutual protect' trys
            if (enclosingTryIndex != EHblkDsc::NO_ENCLOSING_INDEX)
            {
                // The handler funclet for 'XTnum' has a try index of 'enclosingTryIndex' (at least, the parts of the funclet that don't already
                // have a more nested 'try' index because a 'try' is nested within the handler).

                BasicBlock* blockEnd;
                for (block = (HBtab->HasFilter() ? HBtab->ebdFilter : HBtab->ebdHndBeg), blockEnd = HBtab->ebdHndLast->bbNext; block != blockEnd; block = block->bbNext)
                {
                    if (blockTryIndex[block->bbNum] == 0)
                    {
                        blockTryIndex[block->bbNum] = (unsigned short)(enclosingTryIndex + 1);
                    }
                }
            }
        }
    }
#endif // FEATURE_EH_FUNCLETS

    // Make sure that all blocks have the right index, including those blocks that should have zero (no EH region).
    for (block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        assert(block->bbTryIndex == blockTryIndex[block->bbNum]);
        assert(block->bbHndIndex == blockHndIndex[block->bbNum]);

        // Also, since we're walking the blocks, check that all blocks we didn't mark as EH handler 'begin' blocks
        // already have bbCatchTyp set properly.
        if (!blockHndBegSet[block->bbNum])
        {
            assert(block->bbCatchTyp == BBCT_NONE);

#if FEATURE_EH_FUNCLETS
            if (fgFuncletsCreated)
            {
                // Make sure blocks that aren't the first block of a funclet do not have the BBF_FUNCLET_BEG flag set.
                assert((block->bbFlags & BBF_FUNCLET_BEG) == 0);
            }
#endif // FEATURE_EH_FUNCLETS
        }

        // Only the first block of 'try' regions should have BBF_TRY_BEG set.
        if (!blockTryBegSet[block->bbNum])
        {
            assert((block->bbFlags & BBF_TRY_BEG) == 0);
        }
    }
}

void                Compiler::fgDispHandlerTab()
{
    printf("\n***************  Exception Handling table");

    if (compHndBBtabCount == 0)
    {
        printf(" is empty\n");
        return;
    }

    printf("\nindex  ");
#if !FEATURE_EH_FUNCLETS
    printf("nest, ");
#endif // !FEATURE_EH_FUNCLETS
    printf("eTry, eHnd\n");

    unsigned        XTnum;
    EHblkDsc*       HBtab;

    for (XTnum = 0, HBtab = compHndBBtab;
         XTnum < compHndBBtabCount;
         XTnum++  , HBtab++)
    {
        HBtab->DispEntry(XTnum);
    }
}

#endif // DEBUG
/*****************************************************************************/

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                          "Compiler" functions: EH tree verification       XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

/*****************************************************************************
 * The following code checks the following rules for the EH table:
 * 1. Overlapping of try blocks not allowed.
 * 2. Handler blocks cannot be shared between different try blocks.
 * 3. Try blocks with Finally or Fault blocks cannot have other handlers.
 * 4. If block A contains block B, A should also contain B's try/filter/handler.
 * 5. A block cannot contain it's related try/filter/handler.
 * 6. Nested block must appear before containing block
 *
 */

void                Compiler::verInitEHTree(unsigned numEHClauses)
{
    ehnNext = new (this, CMK_BasicBlock) EHNodeDsc[numEHClauses * 3];
    ehnTree = NULL;
}


/* Inserts the try, handler and filter (optional) clause information in a tree structure
 * in order to catch incorrect eh formatting (e.g. illegal overlaps, incorrect order)
 */

void                Compiler::verInsertEhNode(CORINFO_EH_CLAUSE* clause, EHblkDsc* handlerTab)
{
    EHNodeDsc* tryNode = ehnNext++;
    EHNodeDsc* handlerNode = ehnNext++;
    EHNodeDsc* filterNode = NULL; // optional

    tryNode->ehnSetTryNodeType();
    tryNode->ehnStartOffset = clause->TryOffset;
    tryNode->ehnEndOffset   = clause->TryOffset+clause->TryLength - 1;
    tryNode->ehnHandlerNode = handlerNode;

    if (clause->Flags & CORINFO_EH_CLAUSE_FINALLY)
        handlerNode->ehnSetFinallyNodeType();
    else if (clause->Flags & CORINFO_EH_CLAUSE_FAULT)
        handlerNode->ehnSetFaultNodeType();
    else
        handlerNode->ehnSetHandlerNodeType();

    handlerNode->ehnStartOffset = clause->HandlerOffset;
    handlerNode->ehnEndOffset = clause->HandlerOffset + clause->HandlerLength - 1;
    handlerNode->ehnTryNode = tryNode;

    if (clause->Flags & CORINFO_EH_CLAUSE_FILTER)
    {
        filterNode = ehnNext++;
        filterNode->ehnStartOffset = clause->FilterOffset;
        BasicBlock * blk = handlerTab->BBFilterLast();
        filterNode->ehnEndOffset = blk->bbCodeOffsEnd - 1;

        noway_assert(filterNode->ehnEndOffset != 0);
        filterNode->ehnSetFilterNodeType();
        filterNode->ehnTryNode = tryNode;
        tryNode->ehnFilterNode = filterNode;
    }

    verInsertEhNodeInTree(&ehnTree, tryNode);
    verInsertEhNodeInTree(&ehnTree, handlerNode);
    if (filterNode)
        verInsertEhNodeInTree(&ehnTree,filterNode);
}

/*
    The root node could be changed by this method.

    node is inserted to

        (a) right       of root (root.right       <-- node)
        (b) left        of root (node.right       <-- root; node becomes root)
        (c) child       of root (root.child       <-- node)
        (d) parent      of root (node.child       <-- root; node becomes root)
        (e) equivalent  of root (root.equivalent  <-- node)

    such that siblings are ordered from left to right
    child parent relationship and equivalence relationship are not violated


    Here is a list of all possible cases

    Case 1 2 3 4 5 6 7 8 9 10 11 12 13

         | | | | |
         | | | | |
    .......|.|.|.|..................... [ root start ] .....
    |        | | | |             |  |
    |        | | | |             |  |
   r|        | | | |          |  |  |
   o|          | | |          |     |
   o|          | | |          |     |
   t|          | | |          |     |
    |          | | | |     |  |     |
    |          | | | |     |        |
    |..........|.|.|.|.....|........|.. [ root end ] ........
                 | | | |
                 | | | | |
                 | | | | |

        |<-- - - - n o d e - - - -->|


   Case Operation
   --------------
    1    (b)
    2    Error
    3    Error
    4    (d)
    5    (d)
    6    (d)
    7    Error
    8    Error
    9    (a)
    10   (c)
    11   (c)
    12   (c)
    13   (e)


*/

void                Compiler::verInsertEhNodeInTree(EHNodeDsc** ppRoot,
                                                    EHNodeDsc* node)
{
    unsigned nStart = node->ehnStartOffset;
    unsigned nEnd   = node->ehnEndOffset;

    if (nStart > nEnd)
    {
        BADCODE("start offset greater or equal to end offset");
    }
    node->ehnNext = NULL;
    node->ehnChild = NULL;
    node->ehnEquivalent = NULL;

    while (TRUE)
    {
        if (*ppRoot == NULL)
        {
            *ppRoot = node;
            break;
        }
        unsigned rStart = (*ppRoot)->ehnStartOffset;
        unsigned rEnd   = (*ppRoot)->ehnEndOffset;

        if (nStart < rStart)
        {
            // Case 1
            if (nEnd < rStart)
            {
                // Left sibling
                node->ehnNext     = *ppRoot;
                *ppRoot         = node;
                return;
            }
            // Case 2, 3
            if (nEnd < rEnd)
            {
//[Error]
                BADCODE("Overlapping try regions");
            }

            // Case 4, 5
//[Parent]
            verInsertEhNodeParent(ppRoot, node);
            return;
        }


        // Cases 6 - 13 (nStart >= rStart)

        if (nEnd > rEnd)
        {   // Case 6, 7, 8, 9

            // Case 9
            if (nStart > rEnd)
            {
//[RightSibling]

                // Recurse with Root.Sibling as the new root
                ppRoot = &((*ppRoot)->ehnNext);
                continue;
            }

            // Case 6
            if (nStart == rStart)
            {
//[Parent]
                if (node->ehnIsTryBlock() || (*ppRoot)->ehnIsTryBlock())
            {
                verInsertEhNodeParent(ppRoot, node);
                return;
            }

                // non try blocks are not allowed to start at the same offset
                BADCODE("Handlers start at the same offset");
            }

            // Case 7, 8
            BADCODE("Overlapping try regions");
        }

        // Case 10-13 (nStart >= rStart && nEnd <= rEnd)
        if ((nStart != rStart) || (nEnd != rEnd))
        {   // Cases 10,11,12
//[Child]

            if ((*ppRoot)->ehnIsTryBlock())
            {
                BADCODE("Inner try appears after outer try in exception handling table");
            }
            else
            {
                // We have an EH clause nested within a handler, but the parent
                // handler clause came first in the table. The rest of the compiler
                // doesn't expect this, so sort the EH table.

                fgNeedToSortEHTable = true;

                // Case 12 (nStart == rStart)
                // non try blocks are not allowed to start at the same offset
                if ((nStart == rStart) && !node->ehnIsTryBlock())
                {
                    BADCODE("Handlers start at the same offset");
                }

                // check this!
                ppRoot = &((*ppRoot)->ehnChild);
                continue;
            }
        }

        // Case 13
//[Equivalent]
        if (!node->ehnIsTryBlock() &&
            !(*ppRoot)->ehnIsTryBlock())
        {
            BADCODE("Handlers cannot be shared");
        }

        if (!node->ehnIsTryBlock() ||
            !(*ppRoot)->ehnIsTryBlock())
        {
            // Equivalent is only allowed for try bodies
            // If one is a handler, this means the nesting is wrong
            BADCODE("Handler and try with the same offset");
        }

        node->ehnEquivalent = node->ehnNext = *ppRoot;

        // check that the corresponding handler is either a catch handler
        // or a filter
        if (node->ehnHandlerNode->ehnIsFaultBlock()           ||
            node->ehnHandlerNode->ehnIsFinallyBlock()         ||
            (*ppRoot)->ehnHandlerNode->ehnIsFaultBlock()      ||
            (*ppRoot)->ehnHandlerNode->ehnIsFinallyBlock() )
        {
            BADCODE("Try block with multiple non-filter/non-handler blocks");
        }


        break;
    }
}

/**********************************************************************
 * Make node the parent of *ppRoot. All siblings of *ppRoot that are
 * fully or partially nested in node remain siblings of *ppRoot
 */

void            Compiler::verInsertEhNodeParent(EHNodeDsc** ppRoot,
                                                EHNodeDsc*  node)
{
    noway_assert(node->ehnNext == NULL);
    noway_assert(node->ehnChild == NULL);

    // Root is nested in Node
    noway_assert(node->ehnStartOffset <= (*ppRoot)->ehnStartOffset);
    noway_assert(node->ehnEndOffset   >= (*ppRoot)->ehnEndOffset);

    // Root is not the same as Node
    noway_assert(node->ehnStartOffset != (*ppRoot)->ehnStartOffset ||
           node->ehnEndOffset != (*ppRoot)->ehnEndOffset);

    if (node->ehnIsFilterBlock())
    {
        BADCODE("Protected block appearing within filter block");
    }

    EHNodeDsc *lastChild = NULL;
    EHNodeDsc *sibling   = (*ppRoot)->ehnNext;

    while (sibling)
    {
        // siblings are ordered left to right, largest right.
        // nodes have a width of at least one.
        // Hence sibling start will always be after Node start.

        noway_assert(sibling->ehnStartOffset > node->ehnStartOffset);   // (1)

        // disjoint
        if (sibling->ehnStartOffset > node->ehnEndOffset)
            break;

        // partial containment.
        if (sibling->ehnEndOffset > node->ehnEndOffset)   // (2)
        {
            BADCODE("Overlapping try regions");
        }
        //else full containment (follows from (1) and (2))

        lastChild = sibling;
        sibling = sibling->ehnNext;
    }

    // All siblings of Root up to and including lastChild will continue to be
    // siblings of Root (and children of Node). The node to the right of
    // lastChild will become the first sibling of Node.
    //

    if (lastChild)
    {
        // Node has more than one child including Root

        node->ehnNext      = lastChild->ehnNext;
        lastChild->ehnNext = NULL;
    }
    else
    {
        // Root is the only child of Node
        node->ehnNext      = (*ppRoot)->ehnNext;
        (*ppRoot)->ehnNext  = NULL;
    }

    node->ehnChild = *ppRoot;
    *ppRoot     = node;

}

/*****************************************************************************
 * Checks the following two conditions:
 * 1) If block A contains block B, A should also contain B's try/filter/handler.
 * 2) A block cannot contain its related try/filter/handler.
 * Both these conditions are checked by making sure that all the blocks for an
 * exception clause are at the same level.
 * The algorithm is: for each exception clause, determine the first block and
 * search through the next links for its corresponding try/handler/filter as the
 * case may be. If not found, then fail.
 */
void            Compiler::verCheckNestingLevel(EHNodeDsc* root)
{
    EHNodeDsc* ehnNode = root;

    #define exchange(a,b) { temp = a; a = b; b = temp;}

    for (unsigned XTnum = 0; XTnum < compHndBBtabCount; XTnum++)
    {
        EHNodeDsc *p1, *p2, *p3, *temp, *search;

        p1 = ehnNode++;
        p2 = ehnNode++;

        // we are relying on the fact that ehn nodes are allocated sequentially.
        noway_assert(p1->ehnHandlerNode == p2);
        noway_assert(p2->ehnTryNode == p1);

        // arrange p1 and p2 in sequential order
        if (p1->ehnStartOffset == p2->ehnStartOffset)
            BADCODE("shared exception handler");

        if (p1->ehnStartOffset > p2->ehnStartOffset)
            exchange(p1,p2);

        temp = p1->ehnNext;
        unsigned numSiblings = 0;

        search = p2;
        if (search->ehnEquivalent)
            search = search->ehnEquivalent;

        do {
            if (temp == search)
            {
                numSiblings++;
                break;
            }
            if (temp)
                temp = temp->ehnNext;
        } while (temp);

        CORINFO_EH_CLAUSE clause;
        info.compCompHnd->getEHinfo(info.compMethodHnd, XTnum, &clause);

        if (clause.Flags & CORINFO_EH_CLAUSE_FILTER)
        {
            p3 = ehnNode++;

            noway_assert(p3->ehnTryNode == p1 || p3->ehnTryNode == p2);
            noway_assert(p1->ehnFilterNode == p3 || p2->ehnFilterNode == p3);

            if (p3->ehnStartOffset < p1->ehnStartOffset)
            {
                temp = p3; search = p1;
            }
            else if (p3->ehnStartOffset < p2->ehnStartOffset)
            {
                temp = p1; search = p3;
            }
            else
            {
                temp = p2; search = p3;
            }
            if (search->ehnEquivalent)
                search = search->ehnEquivalent;
            do {
                if (temp == search)
                {
                    numSiblings++;
                    break;
                }
                temp = temp->ehnNext;
            } while (temp);
        }
        else
        {
            numSiblings++;
        }

        if (numSiblings != 2)
            BADCODE("Outer block does not contain all code in inner handler");
    }

}

