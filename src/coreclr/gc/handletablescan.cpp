// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
 * Generational GC handle manager.  Table Scanning Routines.
 *
 * Implements support for scanning handles in the table.
 *

 *
 */

#include "common.h"

#include "gcenv.h"

#include "gc.h"

#include "objecthandle.h"
#include "handletablepriv.h"

/****************************************************************************
 *
 * DEFINITIONS FOR WRITE-BARRIER HANDLING
 *
 ****************************************************************************/
 /*
How the macros work:
Handle table's generation (TableSegmentHeader::rgGeneration) is actually a byte array, each byte is generation of a clump.
However it's often used as a uint32_t array for perf reasons, 1 uint32_t contains 4 bytes for ages of 4 clumps. Operations on such
a uint32_t include:

1. COMPUTE_CLUMP_MASK. For some GC operations, we only want to scan handles in certain generation. To do that, we calculate
a Mask uint32_t from the original generation uint32_t:
    MaskDWORD = COMPUTE_CLUMP_MASK (GenerationDWORD, BuildAgeMask(generationToScan, MaxGen))
so that if a byte in GenerationDWORD is smaller than or equals to generationToScan, the corresponding byte in MaskDWORD is non-zero,
otherwise it is zero. However, if a byte in GenerationDWORD is between [2, 3E] and generationToScan is 2, the corresponding byte in
MaskDWORD is also non-zero.

2. AgeEphemeral. When Ephemeral GC happens, ages for handles which belong to the GC condemned generation should be
incremented by 1. The operation is done by calculating a new uint32_t using the old uint32_t value:
    NewGenerationDWORD = COMPUTE_AGED_CLUMPS(OldGenerationDWORD, BuildAgeMask(condemnedGeneration, MaxGen))
so that if a byte in OldGenerationDWORD is smaller than or equals to condemnedGeneration. the corresponding byte in
NewGenerationDWORD is 1 bigger than the old value, otherwise it remains unchanged.

3. Age. Similar as AgeEphemeral, but we use a special mask if condemned generation is max gen (2):
    NewGenerationDWORD = COMPUTE_AGED_CLUMPS(OldGenerationDWORD, GEN_FULLGC)
under this operation, if a byte in OldGenerationDWORD is bigger than or equals to max gen(2) but smaller than 3F, the corresponding byte in
NewGenerationDWORD will be incremented by 1. Basically, a handle clump's age could be in [0, 3E]. But from GC's point of view, [2,3E]
are all considered as gen 2.

If you change any of those algorithm, please verify it by this program:

        void Verify ()
        {
            //the initial value of each byte is 0xff, which means there's no handle in the clump
            VerifyMaskCalc (0xff, 0xff, 0xff, 0xff, 0);
            VerifyMaskCalc (0xff, 0xff, 0xff, 0xff, 1);
            VerifyMaskCalc (0xff, 0xff, 0xff, 0xff, 2);

            VerifyAgeEphemeralCalc (0xff, 0xff, 0xff, 0xff, 0);
            VerifyAgeEphemeralCalc (0xff, 0xff, 0xff, 0xff, 1);
            VerifyAgeCalc (0xff, 0xff, 0xff, 0xff);

            //each byte could independently change from 0 to 0x3e
            for (byte b0 = 0; b0 <= 0x3f; b0++)
            {
                for (byte b1 = 0; b1 <= 0x3f; b1++)
                {
                    for (byte b2 = 0; b2 <= 0x3f; b2++)
                    {
                        for (byte b3 = 0; b3 <= 0x3f; b3++)
                        {
                            //verify we calculate mask correctly
                            VerifyMaskCalc (b0, b1, b2, b3, 0);
                            VerifyMaskCalc (b0, b1, b2, b3, 1);
                            VerifyMaskCalc (b0, b1, b2, b3, 2);

                            //verify BlockAgeBlocksEphemeral would work correctly
                            VerifyAgeEphemeralCalc (b0, b1, b2, b3, 0);
                            VerifyAgeEphemeralCalc (b0, b1, b2, b3, 1);

                            //verify BlockAgeBlock would work correctly
                            VerifyAgeCalc (b0, b1, b2, b3);
                        }
                    }
                }
            }
        }

        void VerifyMaskCalc (byte b0, byte b1, byte b2, byte b3, uint gennum)
        {
            uint genDword = (uint)(b0 | b1 << 8 | b2 << 16 | b3 << 24);

            uint maskedByGen0 = COMPUTE_CLUMP_MASK(genDword, BuildAgeMask (gennum, 2));
            byte b0_ = (byte)(maskedByGen0 & 0xff);
            byte b1_ = (byte)((maskedByGen0 & 0xff00) >> 8);
            byte b2_ = (byte)((maskedByGen0 & 0xff0000) >> 16);
            byte b3_ = (byte)((maskedByGen0 & 0xff000000)>> 24);

            AssertGenMask (b0, b0_, gennum);
            AssertGenMask (b1, b1_, gennum);
            AssertGenMask (b2, b2_, gennum);
            AssertGenMask (b3, b3_, gennum);
        }

        void AssertGenMask (byte gen, byte mask, uint gennum)
        {
            //3f or ff is not a valid generation
            if (gen == 0x3f || gen == 0xff)
            {
                    assert (mask == 0);
                    return;
            }
            //any generation bigger than 2 is actually 2
            if (gen > 2)
                gen = 2;

            if (gen <= gennum)
                assert (mask != 0);
            else
                assert (mask == 0);
        }

        void VerifyAgeEphemeralCalc (byte b0, byte b1, byte b2, byte b3, uint gennum)
        {
            uint genDword = (uint)(b0 | b1 << 8 | b2 << 16 | b3 << 24);

            uint agedClump = COMPUTE_AGED_CLUMPS(genDword, BuildAgeMask (gennum, 2));
            byte b0_ = (byte)(agedClump & 0xff);
            byte b1_ = (byte)((agedClump & 0xff00) >> 8);
            byte b2_ = (byte)((agedClump & 0xff0000) >> 16);
            byte b3_ = (byte)((agedClump & 0xff000000) >> 24);

            AssertAgedClump (b0, b0_, gennum);
            AssertAgedClump (b1, b1_, gennum);
            AssertAgedClump (b2, b2_, gennum);
            AssertAgedClump (b3, b3_, gennum);
        }

        void AssertAgedClump (byte gen, byte agedGen, uint gennum)
        {
            //generation will stop growing at 0x3e
            if (gen >= 0x3e)
            {
                assert (agedGen == gen);
                return;
            }

            if (gen <= gennum || (gen > 2 && gennum >= 2))
                assert (agedGen == gen + 1);
            else
                assert (agedGen == gen);
        }

        void VerifyAgeCalc (byte b0, byte b1, byte b2, byte b3)
        {
            uint genDword = (uint)(b0 | b1 << 8 | b2 << 16 | b3 << 24);

            uint agedClump = COMPUTE_AGED_CLUMPS(genDword, GEN_FULLGC);
            byte b0_ = (byte)(agedClump & 0xff);
            byte b1_ = (byte)((agedClump & 0xff00) >> 8);
            byte b2_ = (byte)((agedClump & 0xff0000) >> 16);
            byte b3_ = (byte)((agedClump & 0xff000000) >> 24);

            AssertAgedClump (b0, b0_, 2);
            AssertAgedClump (b1, b1_, 2);
            AssertAgedClump (b2, b2_, 2);
            AssertAgedClump (b3, b3_, 2);
        }
 */

#define GEN_MAX_AGE                         (0x3F)
#define GEN_CLAMP                           (0x3F3F3F3F)
#define GEN_AGE_LIMIT                       (0x3E3E3E3E)
#define GEN_INVALID                         (0xC0C0C0C0)
#define GEN_FILL                            (0x80808080)
#define GEN_MASK                            (0x40404040)
#define GEN_INC_SHIFT                       (6)

#define PREFOLD_FILL_INTO_AGEMASK(msk)      (1 + (msk) + (~GEN_FILL))

#define GEN_FULLGC                          PREFOLD_FILL_INTO_AGEMASK(GEN_AGE_LIMIT)

#define MAKE_CLUMP_MASK_ADDENDS(bytes)      ((bytes) >> GEN_INC_SHIFT)
#define APPLY_CLUMP_ADDENDS(gen, addend)    ((gen) + (addend))

#define COMPUTE_CLUMP_MASK(gen, msk)        ((((gen) & GEN_CLAMP) - (msk)) & GEN_MASK)
#define COMPUTE_CLUMP_ADDENDS(gen, msk)     MAKE_CLUMP_MASK_ADDENDS(COMPUTE_CLUMP_MASK(gen, msk))
#define COMPUTE_AGED_CLUMPS(gen, msk)       APPLY_CLUMP_ADDENDS(gen, COMPUTE_CLUMP_ADDENDS(gen, msk))

/*--------------------------------------------------------------------------*/



/****************************************************************************
 *
 * SUPPORT STRUCTURES FOR ASYNCHRONOUS SCANNING
 *
 ****************************************************************************/

/*
 * ScanRange
 *
 * Specifies a range of blocks for scanning.
 *
 */
struct ScanRange
{
    /*
     * Start Index
     *
     * Specifies the first block in the range.
     */
    uint32_t uIndex;

    /*
     * Count
     *
     * Specifies the number of blocks in the range.
     */
    uint32_t uCount;
};


/*
 * ScanQNode
 *
 * Specifies a set of block ranges in a scan queue.
 *
 */
struct ScanQNode
{
    /*
     * Next Node
     *
     * Specifies the next node in a scan list.
     */
    struct ScanQNode *pNext;

    /*
     * Entry Count
     *
     * Specifies how many entries in this block are valid.
     */
    uint32_t          uEntries;

    /*
     * Range Entries
     *
     * Each entry specifies a range of blocks to process.
     */
    ScanRange         rgRange[HANDLE_BLOCKS_PER_SEGMENT / 4];
};

/*--------------------------------------------------------------------------*/



/****************************************************************************
 *
 * MISCELLANEOUS HELPER ROUTINES AND DEFINES
 *
 ****************************************************************************/

/*
 * INCLUSION_MAP_SIZE
 *
 * Number of elements in a type inclusion map.
 *
 */
#define INCLUSION_MAP_SIZE (HANDLE_MAX_INTERNAL_TYPES + 1)


/*
 * BuildInclusionMap
 *
 * Creates an inclusion map for the specified type array.
 *
 */
void BuildInclusionMap(BOOL *rgTypeInclusion, const uint32_t *puType, uint32_t uTypeCount)
{
    LIMITED_METHOD_CONTRACT;

    // by default, no types are scanned
    ZeroMemory(rgTypeInclusion, INCLUSION_MAP_SIZE * sizeof(BOOL));

    // add the specified types to the inclusion map
    for (uint32_t u = 0; u < uTypeCount; u++)
    {
        // fetch a type we are supposed to scan
        uint32_t uType = puType[u];

        // hope we aren't about to trash the stack :)
        _ASSERTE(uType < HANDLE_MAX_INTERNAL_TYPES);

        // add this type to the inclusion map
        rgTypeInclusion[uType + 1] = TRUE;
    }
}


/*
 * IsBlockIncluded
 *
 * Checks a type inclusion map for the inclusion of a particular block.
 *
 */
__inline BOOL IsBlockIncluded(TableSegment *pSegment, uint32_t uBlock, const BOOL *rgTypeInclusion)
{
    LIMITED_METHOD_CONTRACT;

    // fetch the adjusted type for this block
    uint32_t uType = (uint32_t)(((int)(signed char)pSegment->rgBlockType[uBlock]) + 1);

    // hope the adjusted type was valid
    _ASSERTE(uType <= HANDLE_MAX_INTERNAL_TYPES);

    // return the inclusion value for the block's type
    return rgTypeInclusion[uType];
}


/*
 * TypesRequireUserDataScanning
 *
 * Determines whether the set of types listed should get user data during scans
 *
 * if ALL types passed have user data then this function will enable user data support
 * otherwise it will disable user data support
 *
 * IN OTHER WORDS, SCANNING WITH A MIX OF USER-DATA AND NON-USER-DATA TYPES IS NOT SUPPORTED
 *
 */
BOOL TypesRequireUserDataScanning(HandleTable *pTable, const uint32_t *types, uint32_t typeCount)
{
    WRAPPER_NO_CONTRACT;

    // count up the number of types passed that have user data associated
    uint32_t userDataCount = 0;
    for (uint32_t u = 0; u < typeCount; u++)
    {
        if (TypeHasUserData(pTable, types[u]))
            userDataCount++;
    }

    // if all have user data then we can enum user data
    if (userDataCount == typeCount)
        return TRUE;

    // WARNING: user data is all or nothing in scanning!!!
    // since we have some types which don't support user data, we can't use the user data scanning code
    // this means all callbacks will get NULL for user data!!!!!
    _ASSERTE(userDataCount == 0);

    // no user data
    return FALSE;
}

/*
 * BuildAgeMask
 *
 * Builds an age mask to be used when examining/updating the write barrier.
 *
 */
uint32_t BuildAgeMask(uint32_t uGen, uint32_t uMaxGen)
{
    LIMITED_METHOD_CONTRACT;

    // an age mask is composed of repeated bytes containing the next older generation

    if (uGen == uMaxGen)
        uGen = GEN_MAX_AGE;

    uGen++;

    // clamp the generation to the maximum age we support in our macros
    if (uGen > GEN_MAX_AGE)
        uGen = GEN_MAX_AGE;

    // pack up a word with age bytes and fill bytes pre-folded as well
    return PREFOLD_FILL_INTO_AGEMASK(uGen | (uGen << 8) | (uGen << 16) | (uGen << 24));
}

/*--------------------------------------------------------------------------*/



/****************************************************************************
 *
 * SYNCHRONOUS HANDLE AND BLOCK SCANNING ROUTINES
 *
 ****************************************************************************/

/*
 * ARRAYSCANPROC
 *
 * Prototype for callbacks that implement handle array scanning logic.
 *
 */
typedef void (CALLBACK *ARRAYSCANPROC)(PTR_UNCHECKED_OBJECTREF pValue, PTR_UNCHECKED_OBJECTREF pLast,
                                       ScanCallbackInfo *pInfo, uintptr_t *pUserData);


/*
 * ScanConsecutiveHandlesWithoutUserData
 *
 * Unconditionally scans a consecutive range of handles.
 *
 * USER DATA PASSED TO CALLBACK PROC IS ALWAYS NULL!
 *
 */
void CALLBACK ScanConsecutiveHandlesWithoutUserData(PTR_UNCHECKED_OBJECTREF pValue,
                                                    PTR_UNCHECKED_OBJECTREF pLast,
                                                    ScanCallbackInfo *pInfo,
                                                    uintptr_t *)
{
    WRAPPER_NO_CONTRACT;

#ifdef _DEBUG
    // update our scanning statistics
    pInfo->DEBUG_HandleSlotsScanned += (int)(pLast - pValue);
#endif

    // get frequently used params into locals
    HANDLESCANPROC pfnScan = pInfo->pfnScan;
    uintptr_t      param1  = pInfo->param1;
    uintptr_t      param2  = pInfo->param2;

    // scan for non-zero handles
    do
    {
        // call the callback for any we find
        if (!HndIsNullOrDestroyedHandle(*pValue))
        {
#ifdef _DEBUG
            // update our scanning statistics
            pInfo->DEBUG_HandlesActuallyScanned++;
#endif

            // process this handle
            pfnScan(pValue, NULL, param1, param2);
        }

        // on to the next handle
        pValue++;

    } while (pValue < pLast);
}


/*
 * ScanConsecutiveHandlesWithUserData
 *
 * Unconditionally scans a consecutive range of handles.
 *
 * USER DATA IS ASSUMED TO BE CONSECUTIVE!
 *
 */
void CALLBACK ScanConsecutiveHandlesWithUserData(PTR_UNCHECKED_OBJECTREF pValue,
                                                 PTR_UNCHECKED_OBJECTREF pLast,
                                                 ScanCallbackInfo *pInfo,
                                                 uintptr_t *pUserData)
{
    WRAPPER_NO_CONTRACT;

#ifdef _DEBUG
    // this function will crash if it is passed bad extra info
    _ASSERTE(pUserData);

    // update our scanning statistics
    pInfo->DEBUG_HandleSlotsScanned += (int)(pLast - pValue);
#endif

    // get frequently used params into locals
    HANDLESCANPROC pfnScan = pInfo->pfnScan;
    uintptr_t      param1  = pInfo->param1;
    uintptr_t      param2  = pInfo->param2;

    // scan for non-zero handles
    do
    {
        // call the callback for any we find
        if (!HndIsNullOrDestroyedHandle(*pValue))
        {
#ifdef _DEBUG
            // update our scanning statistics
            pInfo->DEBUG_HandlesActuallyScanned++;
#endif

            // process this handle
            pfnScan(pValue, pUserData, param1, param2);
        }

        // on to the next handle
        pValue++;
        pUserData++;

    } while (pValue < pLast);
}

/*
 * BlockAgeBlocks
 *
 * Ages all clumps in a range of consecutive blocks.
 *
 */
void CALLBACK BlockAgeBlocks(PTR_TableSegment pSegment, uint32_t uBlock, uint32_t uCount, ScanCallbackInfo *pInfo)
{
    LIMITED_METHOD_CONTRACT;
    UNREFERENCED_PARAMETER(pInfo);

#ifdef DACCESS_COMPILE
    UNREFERENCED_PARAMETER(pSegment);
    UNREFERENCED_PARAMETER(uBlock);
    UNREFERENCED_PARAMETER(uCount);
#else
    // set up to update the specified blocks
    uint32_t *pdwGen     = (uint32_t *)pSegment->rgGeneration + uBlock;
    uint32_t *pdwGenLast =             pdwGen                 + uCount;

    // loop over all the blocks, aging their clumps as we go
    do
    {
        // compute and store the new ages in parallel
        *pdwGen = COMPUTE_AGED_CLUMPS(*pdwGen, GEN_FULLGC);

    } while (++pdwGen < pdwGenLast);
#endif
}

/*
 * BlockScanBlocksWithoutUserData
 *
 * Calls the specified callback once for each handle in a range of blocks,
 * optionally aging the corresponding generation clumps.
 *
 */
void CALLBACK BlockScanBlocksWithoutUserData(PTR_TableSegment pSegment, uint32_t uBlock, uint32_t uCount, ScanCallbackInfo *pInfo)
{
    LIMITED_METHOD_CONTRACT;

#ifndef DACCESS_COMPILE
    // get the first and limit handles for these blocks
    _UNCHECKED_OBJECTREF *pValue = pSegment->rgValue + (uBlock * HANDLE_HANDLES_PER_BLOCK);
    _UNCHECKED_OBJECTREF *pLast  = pValue            + (uCount * HANDLE_HANDLES_PER_BLOCK);
#else
    PTR_UNCHECKED_OBJECTREF pValue = dac_cast<PTR_UNCHECKED_OBJECTREF>(PTR_HOST_MEMBER_TADDR(TableSegment, pSegment, rgValue))
                                                     + (uBlock * HANDLE_HANDLES_PER_BLOCK);
    PTR_UNCHECKED_OBJECTREF pLast  = pValue          + (uCount * HANDLE_HANDLES_PER_BLOCK);
#endif

    // scan the specified handles
    ScanConsecutiveHandlesWithoutUserData(pValue, pLast, pInfo, NULL);

    // optionally update the clump generations for these blocks too
    if (pInfo->uFlags & HNDGCF_AGE)
        BlockAgeBlocks(pSegment, uBlock, uCount, pInfo);

#ifdef _DEBUG
    // update our scanning statistics
    pInfo->DEBUG_BlocksScannedNonTrivially += uCount;
    pInfo->DEBUG_BlocksScanned += uCount;
#endif
}


/*
 * BlockScanBlocksWithUserData
 *
 * Calls the specified callback once for each handle in a range of blocks,
 * optionally aging the corresponding generation clumps.
 *
 */
void CALLBACK BlockScanBlocksWithUserData(PTR_TableSegment pSegment, uint32_t uBlock, uint32_t uCount, ScanCallbackInfo *pInfo)
{
    LIMITED_METHOD_CONTRACT;

    // iterate individual blocks scanning with user data
    for (uint32_t u = 0; u < uCount; u++)
    {
        // compute the current block
        uint32_t uCur = (u + uBlock);

        // fetch the user data for this block
        uintptr_t *pUserData = BlockFetchUserDataPointer(PTR__TableSegmentHeader(pSegment), uCur, TRUE);

#ifndef DACCESS_COMPILE
        // get the first and limit handles for these blocks
        _UNCHECKED_OBJECTREF *pValue = pSegment->rgValue + (uCur * HANDLE_HANDLES_PER_BLOCK);
        _UNCHECKED_OBJECTREF *pLast  = pValue            + HANDLE_HANDLES_PER_BLOCK;
#else
        PTR_UNCHECKED_OBJECTREF pValue = dac_cast<PTR_UNCHECKED_OBJECTREF>(PTR_HOST_MEMBER_TADDR(TableSegment, pSegment, rgValue))
                                                         + (uCur * HANDLE_HANDLES_PER_BLOCK);
        PTR_UNCHECKED_OBJECTREF pLast  = pValue          + HANDLE_HANDLES_PER_BLOCK;
#endif

        // scan the handles in this block
        ScanConsecutiveHandlesWithUserData(pValue, pLast, pInfo, pUserData);
    }

    // optionally update the clump generations for these blocks too
    if (pInfo->uFlags & HNDGCF_AGE)
        BlockAgeBlocks(pSegment, uBlock, uCount, pInfo);

#ifdef _DEBUG
    // update our scanning statistics
    pInfo->DEBUG_BlocksScannedNonTrivially += uCount;
    pInfo->DEBUG_BlocksScanned += uCount;
#endif
}


/*
 * BlockScanBlocksEphemeralWorker
 *
 * Calls the specified callback once for each handle in any clump
 * identified by the clump mask in the specified block.
 *
 */
void BlockScanBlocksEphemeralWorker(uint32_t *pdwGen, uint32_t dwClumpMask, ScanCallbackInfo *pInfo)
{
    WRAPPER_NO_CONTRACT;

    //
    // OPTIMIZATION: Since we expect to call this worker fairly rarely compared to
    //  the number of times we pass through the outer loop, this function intentionally
    //  does not take pSegment as a param.
    //
    //  We do this so that the compiler won't try to keep pSegment in a register during
    //  the outer loop, leaving more registers for the common codepath.
    //
    //  You might wonder why this is an issue considering how few locals we have in
    //  BlockScanBlocksEphemeral.  For some reason the x86 compiler doesn't like to use
    //  all the registers during that loop, so a little coaxing was necessary to get
    //  the right output.
    //

    // fetch the table segment we are working in
    PTR_TableSegment pSegment = pInfo->pCurrentSegment;

    // if we should age the clumps then do so now (before we trash dwClumpMask)
    if (pInfo->uFlags & HNDGCF_AGE)
        *pdwGen = APPLY_CLUMP_ADDENDS(*pdwGen, MAKE_CLUMP_MASK_ADDENDS(dwClumpMask));

    // compute the index of the first clump in the block
    uint32_t uClump = (uint32_t)((uint8_t *)pdwGen - pSegment->rgGeneration);

#ifndef DACCESS_COMPILE
    // compute the first handle in the first clump of this block
    _UNCHECKED_OBJECTREF *pValue = pSegment->rgValue + (uClump * HANDLE_HANDLES_PER_CLUMP);
#else
    PTR_UNCHECKED_OBJECTREF pValue = dac_cast<PTR_UNCHECKED_OBJECTREF>(PTR_HOST_MEMBER_TADDR(TableSegment, pSegment, rgValue))
                                                     + (uClump * HANDLE_HANDLES_PER_CLUMP);
#endif

    // some scans require us to report per-handle extra info - assume this one doesn't
    ARRAYSCANPROC pfnScanHandles = ScanConsecutiveHandlesWithoutUserData;
    uintptr_t       *pUserData = NULL;

    // do we need to pass user data to the callback?
    if (pInfo->fEnumUserData)
    {
        // scan with user data enabled
        pfnScanHandles = ScanConsecutiveHandlesWithUserData;

        // get the first user data slot for this block
        pUserData = BlockFetchUserDataPointer(PTR__TableSegmentHeader(pSegment), (uClump / HANDLE_CLUMPS_PER_BLOCK), TRUE);
    }

    // loop over the clumps, scanning those that are identified by the mask
    do
    {
        // compute the last handle in this clump
        PTR_UNCHECKED_OBJECTREF pLast = pValue + HANDLE_HANDLES_PER_CLUMP;

        // if this clump should be scanned then scan it
        if (dwClumpMask & GEN_CLUMP_0_MASK)
            pfnScanHandles(pValue, pLast, pInfo, pUserData);

        // skip to the next clump
        dwClumpMask = NEXT_CLUMP_IN_MASK(dwClumpMask);
        pValue = pLast;
        pUserData += HANDLE_HANDLES_PER_CLUMP;

    } while (dwClumpMask);

#ifdef _DEBUG
    // update our scanning statistics
    pInfo->DEBUG_BlocksScannedNonTrivially++;
#endif
}


/*
 * BlockScanBlocksEphemeral
 *
 * Calls the specified callback once for each handle from the specified
 * generation in a block.
 *
 */
void CALLBACK BlockScanBlocksEphemeral(PTR_TableSegment pSegment, uint32_t uBlock, uint32_t uCount, ScanCallbackInfo *pInfo)
{
    WRAPPER_NO_CONTRACT;

    // get frequently used params into locals
    uint32_t dwAgeMask = pInfo->dwAgeMask;

    // set up to update the specified blocks
    uint32_t *pdwGen     = (uint32_t *)pSegment->rgGeneration + uBlock;
    uint32_t *pdwGenLast =             pdwGen                 + uCount;

    // loop over all the blocks, checking for eligible clumps as we go
    do
    {
        // determine if any clumps in this block are eligible
        uint32_t dwClumpMask = COMPUTE_CLUMP_MASK(*pdwGen, dwAgeMask);

        // if there are any clumps to scan then scan them now
        if (dwClumpMask)
        {
            // ok we need to scan some parts of this block
            //
            // OPTIMIZATION: Since we expect to call the worker fairly rarely compared
            //  to the number of times we pass through the loop, the function below
            //  intentionally does not take pSegment as a param.
            //
            //  We do this so that the compiler won't try to keep pSegment in a register
            //  during our loop, leaving more registers for the common codepath.
            //
            //  You might wonder why this is an issue considering how few locals we have
            //  here.  For some reason the x86 compiler doesn't like to use all the
            //  registers available during this loop and instead was hitting the stack
            //  repeatedly, so a little coaxing was necessary to get the right output.
            //
            BlockScanBlocksEphemeralWorker(pdwGen, dwClumpMask, pInfo);
        }

        // on to the next block's generation info
        pdwGen++;

    } while (pdwGen < pdwGenLast);

#ifdef _DEBUG
    // update our scanning statistics
    pInfo->DEBUG_BlocksScanned += uCount;
#endif
}

#ifndef DACCESS_COMPILE
/*
 * BlockAgeBlocksEphemeral
 *
 * Ages all clumps within the specified generation.
 *
 */
void CALLBACK BlockAgeBlocksEphemeral(PTR_TableSegment pSegment, uint32_t uBlock, uint32_t uCount, ScanCallbackInfo *pInfo)
{
    LIMITED_METHOD_CONTRACT;

    // get frequently used params into locals
    uint32_t dwAgeMask = pInfo->dwAgeMask;

    // set up to update the specified blocks
    uint32_t *pdwGen     = (uint32_t *)pSegment->rgGeneration + uBlock;
    uint32_t *pdwGenLast =             pdwGen                 + uCount;

    // loop over all the blocks, aging their clumps as we go
    do
    {
        // compute and store the new ages in parallel
        *pdwGen = COMPUTE_AGED_CLUMPS(*pdwGen, dwAgeMask);

    } while (++pdwGen < pdwGenLast);
}

/*
 * BlockResetAgeMapForBlocksWorker
 *
 * Figures out the minimum age of the objects referred to by the handles in any clump
 * identified by the clump mask in the specified block.
 *
 */
void BlockResetAgeMapForBlocksWorker(uint32_t *pdwGen, uint32_t dwClumpMask, ScanCallbackInfo *pInfo)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    // fetch the table segment we are working in
    TableSegment *pSegment = pInfo->pCurrentSegment;

    // compute the index of the first clump in the block
    uint32_t uClump = (uint32_t)((uint8_t *)pdwGen - pSegment->rgGeneration);

    // compute the first handle in the first clump of this block
    _UNCHECKED_OBJECTREF *pValue = pSegment->rgValue + (uClump * HANDLE_HANDLES_PER_CLUMP);

    // loop over the clumps, scanning those that are identified by the mask
    do
    {
                // compute the last handle in this clump
        _UNCHECKED_OBJECTREF *pLast = pValue + HANDLE_HANDLES_PER_CLUMP;

        // if this clump should be scanned then scan it
        if (dwClumpMask & GEN_CLUMP_0_MASK)
        {
            // for each clump, determine the minimum age of the objects pointed at.
            int minAge = GEN_MAX_AGE;
            for ( ; pValue < pLast; pValue++)
            {
                if (!HndIsNullOrDestroyedHandle(*pValue))
                {
                    int thisAge = GetConvertedGeneration(*pValue);
                    if (minAge > thisAge)
                        minAge = thisAge;

#ifdef FEATURE_ASYNC_PINNED_HANDLES
                    GCToEEInterface::WalkAsyncPinned(*pValue, &minAge,
                        [](Object*, Object* to, void* ctx)
                        {
                            int* minAge = reinterpret_cast<int*>(ctx);
                            int generation = GetConvertedGeneration(to);
                            if (*minAge > generation)
                            {
                                *minAge = generation;
                            }
                        });
#endif
               }
            }
            _ASSERTE(FitsInU1(minAge));
            ((uint8_t *)pSegment->rgGeneration)[uClump] = static_cast<uint8_t>(minAge);
        }
        // skip to the next clump
        dwClumpMask = NEXT_CLUMP_IN_MASK(dwClumpMask);
        pValue = pLast;
        uClump++;
    } while (dwClumpMask);
}


/*
 * BlockResetAgeMapForBlocks
 *
 * Sets the age maps for a range of blocks. Called in the case of demotion. Even in this case
 * though, most handles refer to objects that don't get demoted and that need to be aged therefore.
 *
 */
void CALLBACK BlockResetAgeMapForBlocks(TableSegment *pSegment, uint32_t uBlock, uint32_t uCount, ScanCallbackInfo *pInfo)
{
    WRAPPER_NO_CONTRACT;

#if 0
    // zero the age map for the specified range of blocks
    ZeroMemory((uint32_t *)pSegment->rgGeneration + uBlock, uCount * sizeof(uint32_t));
#else
    // Actually, we need to be more sophisticated than the above code - there are scenarios
    // where there is demotion in almost every gc cycle, so none of handles get
    // aged appropriately.

    // get frequently used params into locals
    uint32_t dwAgeMask = pInfo->dwAgeMask;

    // set up to update the specified blocks
    uint32_t *pdwGen     = (uint32_t *)pSegment->rgGeneration + uBlock;
    uint32_t *pdwGenLast =             pdwGen                 + uCount;

    // loop over all the blocks, checking for eligible clumps as we go
    do
    {
        // determine if any clumps in this block are eligible
        uint32_t dwClumpMask = COMPUTE_CLUMP_MASK(*pdwGen, dwAgeMask);

        // if there are any clumps to scan then scan them now
        if (dwClumpMask)
        {
            // ok we need to scan some parts of this block
            // This code is a variation of the code in BlockScanBlocksEphemeral,
            // so the OPTIMIZATION comment there applies here as well
            BlockResetAgeMapForBlocksWorker(pdwGen, dwClumpMask, pInfo);
        }

        // on to the next block's generation info
        pdwGen++;

    } while (pdwGen < pdwGenLast);
#endif
}

static void VerifyObject(_UNCHECKED_OBJECTREF from, _UNCHECKED_OBJECTREF obj)
{
#if defined(FEATURE_NATIVEAOT) || defined(BUILD_AS_STANDALONE)
    UNREFERENCED_PARAMETER(from);
    MethodTable* pMT = (MethodTable*)(obj->GetGCSafeMethodTable());
    pMT->SanityCheck();
#else
    obj->ValidateHeap();
#endif // FEATURE_NATIVEAOT
}

static void VerifyObjectAndAge(_UNCHECKED_OBJECTREF from, _UNCHECKED_OBJECTREF obj, uint8_t minAge)
{
    VerifyObject(from, obj);

    int thisAge = GetConvertedGeneration(obj);

    //debugging code
    //if (minAge > thisAge && thisAge < g_theGCHeap->GetMaxGeneration())
    //{
    //    if ((*pValue) == obj)
    //        printf("Handle (age %u) %p -> %p (age %u)", minAge, pValue, obj, thisAge);
    //    else
    //        printf("Handle (age %u) %p -> %p -> %p (age %u)", minAge, pValue, from, obj, thisAge);

    //    // for test programs - if the object is a string, print it
    //    if (obj->GetGCSafeMethodTable() == g_pStringClass)
    //    {
    //        wprintf("'%s'\n", ((StringObject *)obj)->GetBuffer());
    //    }
    //    else
    //    {
    //        printf("\n");
    //    }
    //}

    if (minAge >= GEN_MAX_AGE || (minAge > thisAge && thisAge < static_cast<int>(g_theGCHeap->GetMaxGeneration())))
    {
        _ASSERTE(!"Fatal Error in HandleTable.");
        GCToEEInterface::HandleFatalError(COR_E_EXECUTIONENGINE);
    }
}

/*
 * BlockVerifyAgeMapForBlocksWorker
 *
 * Verifies out the minimum age of the objects referred to by the handles in any clump
 * identified by the clump mask in the specified block.
 * Also validates the objects themselves.
 *
 */
void BlockVerifyAgeMapForBlocksWorker(uint32_t *pdwGen, uint32_t dwClumpMask, ScanCallbackInfo *pInfo, uint32_t uType)
{
    WRAPPER_NO_CONTRACT;

    // fetch the table segment we are working in
    TableSegment *pSegment = pInfo->pCurrentSegment;

    // compute the index of the first clump in the block
    uint32_t uClump = (uint32_t)((uint8_t *)pdwGen - pSegment->rgGeneration);

    // compute the first handle in the first clump of this block
    _UNCHECKED_OBJECTREF *pValue = pSegment->rgValue + (uClump * HANDLE_HANDLES_PER_CLUMP);

    // loop over the clumps, scanning those that are identified by the mask
    do
    {
        // compute the last handle in this clump
        _UNCHECKED_OBJECTREF *pLast = pValue + HANDLE_HANDLES_PER_CLUMP;

        // if this clump should be scanned then scan it
        if (dwClumpMask & GEN_CLUMP_0_MASK)
        {
            // for each clump, check whether any object is younger than the age indicated by the clump
            uint8_t minAge = ((uint8_t *)pSegment->rgGeneration)[uClump];
            for ( ; pValue < pLast; pValue++)
            {
                if (!HndIsNullOrDestroyedHandle(*pValue))
                {
                    VerifyObjectAndAge((*pValue), (*pValue), minAge);

#ifdef FEATURE_ASYNC_PINNED_HANDLES
                    GCToEEInterface::WalkAsyncPinned(*pValue, &minAge,
                        [](Object* from, Object* object, void* age)
                        {
                            uint8_t* minAge = reinterpret_cast<uint8_t*>(age);
                            VerifyObjectAndAge(from, object, *minAge);
                        });
#endif

                    if (uType == HNDTYPE_DEPENDENT)
                    {
                        PTR_uintptr_t pUserData = HandleQuickFetchUserDataPointer((OBJECTHANDLE)pValue);

                        // if we did then copy the value
                        if (pUserData)
                        {
                            _UNCHECKED_OBJECTREF pSecondary = (_UNCHECKED_OBJECTREF)(*pUserData);
                            if (pSecondary)
                            {
                                VerifyObject(pSecondary, pSecondary);
                            }
                        }
                    }
                }
            }
        }
//        else
//            printf("skipping clump with age %x\n", ((uint8_t *)pSegment->rgGeneration)[uClump]);

        // skip to the next clump
        dwClumpMask = NEXT_CLUMP_IN_MASK(dwClumpMask);
        pValue = pLast;
        uClump++;
    } while (dwClumpMask);
}

/*
 * BlockVerifyAgeMapForBlocks
 *
 * Sets the age maps for a range of blocks. Called in the case of demotion. Even in this case
 * though, most handles refer to objects that don't get demoted and that need to be aged therefore.
 *
 */
void CALLBACK BlockVerifyAgeMapForBlocks(TableSegment *pSegment, uint32_t uBlock, uint32_t uCount, ScanCallbackInfo *pInfo)
{
    WRAPPER_NO_CONTRACT;

    for (uint32_t u = 0; u < uCount; u++)
    {
        uint32_t uCur = (u + uBlock);

        uint32_t *pdwGen = (uint32_t *)pSegment->rgGeneration + uCur;

        uint32_t uType = pSegment->rgBlockType[uCur];

        BlockVerifyAgeMapForBlocksWorker(pdwGen, 0xFFFFFFFF, pInfo, uType);
    }
}

/*
 * BlockLockBlocks
 *
 * Locks all blocks in the specified range.
 *
 */
void CALLBACK BlockLockBlocks(TableSegment *pSegment, uint32_t uBlock, uint32_t uCount, ScanCallbackInfo *)
{
    WRAPPER_NO_CONTRACT;

    // loop over the blocks in the specified range and lock them
    for (uCount += uBlock; uBlock < uCount; uBlock++)
        BlockLock(pSegment, uBlock);
}


/*
 * BlockUnlockBlocks
 *
 * Unlocks all blocks in the specified range.
 *
 */
void CALLBACK BlockUnlockBlocks(TableSegment *pSegment, uint32_t uBlock, uint32_t uCount, ScanCallbackInfo *)
{
    WRAPPER_NO_CONTRACT;

    // loop over the blocks in the specified range and unlock them
    for (uCount += uBlock; uBlock < uCount; uBlock++)
        BlockUnlock(pSegment, uBlock);
}
#endif // !DACCESS_COMPILE

/*
 * BlockQueueBlocksForAsyncScan
 *
 * Queues the specified blocks to be scanned asynchronously.
 *
 */
void CALLBACK BlockQueueBlocksForAsyncScan(PTR_TableSegment pSegment, uint32_t uBlock, uint32_t uCount, ScanCallbackInfo *)
{
    CONTRACTL
    {
        NOTHROW;
        WRAPPER(GC_TRIGGERS);
    }
    CONTRACTL_END;

    // fetch our async scan information
    AsyncScanInfo *pAsyncInfo = pSegment->pHandleTable->pAsyncScanInfo;

    // sanity
    _ASSERTE(pAsyncInfo);

    // fetch the current queue tail
    ScanQNode *pQNode = pAsyncInfo->pQueueTail;

    // did we get a tail?
    if (pQNode)
    {
        // we got an existing tail - is the tail node full already?
        if (pQNode->uEntries >= ARRAY_SIZE(pQNode->rgRange))
        {
            // the node is full - is there another node in the queue?
            if (!pQNode->pNext)
            {
                // no more nodes - allocate a new one
                ScanQNode *pQNodeT = new (nothrow) ScanQNode();

                // did it succeed?
                if (!pQNodeT)
                {
                    //
                    // We couldn't allocate another queue node.
                    //
                    // THIS IS NOT FATAL IF ASYNCHRONOUS SCANNING IS BEING USED PROPERLY
                    //
                    // The reason we can survive this is that asynchronous scans are not
                    // guaranteed to enumerate all handles anyway.  Since the table can
                    // change while the lock is released, the caller may assume only that
                    // asynchronous scanning will enumerate a reasonably high percentage
                    // of the handles requested, most of the time.
                    //
                    // The typical use of an async scan is to process as many handles as
                    // possible asynchronously, so as to reduce the amount of time spent
                    // in the inevitable synchronous scan that follows.
                    //
                    // As a practical example, the Concurrent Mark phase of garbage
                    // collection marks as many objects as possible asynchronously, and
                    // subsequently performs a normal, synchronous mark to catch the
                    // stragglers.  Since most of the reachable objects in the heap are
                    // already marked at this point, the synchronous scan ends up doing
                    // very little work.
                    //
                    // So the moral of the story is that yes, we happily drop some of
                    // your blocks on the floor in this out of memory case, and that's
                    // BY DESIGN.
                    //
                    LOG((LF_GC, LL_WARNING, "WARNING: Out of memory queueing for async scan.  Some blocks skipped.\n"));
                    return;
                }

                memset (pQNodeT, 0, sizeof(ScanQNode));

                // link the new node into the queue
                pQNode->pNext = pQNodeT;
            }

            // either way, use the next node in the queue
            pQNode = pQNode->pNext;
        }
    }
    else
    {
        // no tail - this is a brand new queue; start the tail at the head node
        pQNode = pAsyncInfo->pScanQueue;
    }

    // we will be using the last slot after the existing entries
    uint32_t uSlot = pQNode->uEntries;

    // fetch the slot where we will be storing the new block range
    ScanRange *pNewRange = pQNode->rgRange + uSlot;

    // update the entry count in the node
    pQNode->uEntries = uSlot + 1;

    // fill in the new slot with the block range info
    pNewRange->uIndex = uBlock;
    pNewRange->uCount = uCount;

    // remember the last block we stored into as the new queue tail
    pAsyncInfo->pQueueTail = pQNode;
}

/*--------------------------------------------------------------------------*/



/****************************************************************************
 *
 * ASYNCHRONOUS SCANNING WORKERS AND CALLBACKS
 *
 ****************************************************************************/

/*
 * QNODESCANPROC
 *
 * Prototype for callbacks that implement per ScanQNode scanning logic.
 *
 */
typedef void (CALLBACK *QNODESCANPROC)(AsyncScanInfo *pAsyncInfo, ScanQNode *pQNode, uintptr_t lParam);


/*
 * ProcessScanQueue
 *
 * Calls the specified handler once for each node in a scan queue.
 *
 */
void ProcessScanQueue(AsyncScanInfo *pAsyncInfo, QNODESCANPROC pfnNodeHandler, uintptr_t lParam, BOOL fCountEmptyQNodes)
{
    WRAPPER_NO_CONTRACT;

	if (pAsyncInfo->pQueueTail == NULL && fCountEmptyQNodes == FALSE)
		return;

    // if any entries were added to the block list after our initial node, clean them up now
    ScanQNode *pQNode = pAsyncInfo->pScanQueue;
    while (pQNode)
    {
        // remember the next node
        ScanQNode *pNext = pQNode->pNext;

        // call the handler for the current node and then advance to the next
        pfnNodeHandler(pAsyncInfo, pQNode, lParam);
        pQNode = pNext;
    }
}


/*
 * ProcessScanQNode
 *
 * Calls the specified block handler once for each range of blocks in a ScanQNode.
 *
 */
void CALLBACK ProcessScanQNode(AsyncScanInfo *pAsyncInfo, ScanQNode *pQNode, uintptr_t lParam)
{
    WRAPPER_NO_CONTRACT;

    // get the block handler from our lParam
    BLOCKSCANPROC     pfnBlockHandler = (BLOCKSCANPROC)lParam;

    // fetch the params we will be passing to the handler
    ScanCallbackInfo *pCallbackInfo = pAsyncInfo->pCallbackInfo;
    PTR_TableSegment  pSegment = pCallbackInfo->pCurrentSegment;

    // set up to iterate the ranges in the queue node
    ScanRange *pRange     = pQNode->rgRange;
    ScanRange *pRangeLast = pRange          + pQNode->uEntries;

    // loop over all the ranges, calling the block handler for each one
    while (pRange < pRangeLast) {
        // call the block handler with the current block range
        pfnBlockHandler(pSegment, pRange->uIndex, pRange->uCount, pCallbackInfo);

        // advance to the next range
        pRange++;

    }
}

#ifndef DACCESS_COMPILE
/*
 * UnlockAndForgetQueuedBlocks
 *
 * Unlocks all blocks referenced in the specified node and marks the node as empty.
 *
 */
void CALLBACK UnlockAndForgetQueuedBlocks(AsyncScanInfo *pAsyncInfo, ScanQNode *pQNode, uintptr_t)
{
    WRAPPER_NO_CONTRACT;

    // unlock the blocks named in this node
    ProcessScanQNode(pAsyncInfo, pQNode, (uintptr_t)BlockUnlockBlocks);

    // reset the node so it looks empty
    pQNode->uEntries = 0;
}
#endif

/*
 * FreeScanQNode
 *
 * Frees the specified ScanQNode
 *
 */
void CALLBACK FreeScanQNode(AsyncScanInfo *, ScanQNode *pQNode, uintptr_t)
{
    LIMITED_METHOD_CONTRACT;

    // free the node's memory
    delete  pQNode;
}


/*
 * xxxTableScanQueuedBlocksAsync
 *
 * Performs and asynchronous scan of the queued blocks for the specified segment.
 *
 * N.B. THIS FUNCTION LEAVES THE TABLE LOCK WHILE SCANNING.
 *
 */
void xxxTableScanQueuedBlocksAsync(PTR_HandleTable pTable, PTR_TableSegment pSegment, CrstHolderWithState *pCrstHolder)
{
    WRAPPER_NO_CONTRACT;

    //-------------------------------------------------------------------------------
    // PRE-SCAN PREPARATION

    // fetch our table's async and sync scanning info
    AsyncScanInfo    *pAsyncInfo    = pTable->pAsyncScanInfo;
    ScanCallbackInfo *pCallbackInfo = pAsyncInfo->pCallbackInfo;

    // make a note that we are now processing this segment
    pCallbackInfo->pCurrentSegment = pSegment;

#ifndef DACCESS_COMPILE
    // loop through and lock down all the blocks referenced by the queue
    ProcessScanQueue(pAsyncInfo, ProcessScanQNode, (uintptr_t)BlockLockBlocks, FALSE);
#endif

    //-------------------------------------------------------------------------------
    // ASYNCHRONOUS SCANNING OF QUEUED BLOCKS
    //

    // leave the table lock
    _ASSERTE(pCrstHolder->GetValue()==(&pTable->Lock));
    pCrstHolder->Release();

    // sanity - this isn't a very asynchronous scan if we don't actually leave
    _ASSERTE(!pTable->Lock.OwnedByCurrentThread());

    // perform the actual scanning of the specified blocks
    ProcessScanQueue(pAsyncInfo, ProcessScanQNode, (uintptr_t)pAsyncInfo->pfnBlockHandler, FALSE);

    // re-enter the table lock
    pCrstHolder->Acquire();


    //-------------------------------------------------------------------------------
    // POST-SCAN CLEANUP
    //

#ifndef DACCESS_COMPILE
    // loop through, unlock all the blocks we had locked, and reset the queue nodes
    ProcessScanQueue(pAsyncInfo, UnlockAndForgetQueuedBlocks, (uintptr_t)NULL, FALSE);
#endif

    // we are done processing this segment
    pCallbackInfo->pCurrentSegment = NULL;

    // reset the "queue tail" pointer to indicate an empty queue
    pAsyncInfo->pQueueTail = NULL;
}

/*--------------------------------------------------------------------------*/



/****************************************************************************
 *
 * SEGMENT ITERATORS
 *
 ****************************************************************************/

/*
 * QuickSegmentIterator
 *
 * Returns the next segment to be scanned in a scanning loop.
 *
 */
PTR_TableSegment CALLBACK QuickSegmentIterator(PTR_HandleTable pTable, PTR_TableSegment pPrevSegment, CrstHolderWithState *)
{
    LIMITED_METHOD_CONTRACT;

    PTR_TableSegment pNextSegment;

    // do we have a previous segment?
    if (!pPrevSegment)
    {
        // nope - start with the first segment in our list
        pNextSegment = pTable->pSegmentList;
    }
    else
    {
        // yup, fetch the next segment in the list
        pNextSegment = pPrevSegment->pNextSegment;
    }

    // return the segment pointer
    return pNextSegment;
}


/*
 * StandardSegmentIterator
 *
 * Returns the next segment to be scanned in a scanning loop.
 *
 * This iterator performs some maintenance on the segments,
 * primarily making sure the block chains are sorted so that
 * g0 scans are more likely to operate on contiguous blocks.
 *
 */
PTR_TableSegment CALLBACK StandardSegmentIterator(PTR_HandleTable pTable, PTR_TableSegment pPrevSegment, CrstHolderWithState *)
{
    CONTRACTL
    {
        WRAPPER(NOTHROW);
        WRAPPER(GC_TRIGGERS);
        FORBID_FAULT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    // get the next segment using the quick iterator
    PTR_TableSegment pNextSegment = QuickSegmentIterator(pTable, pPrevSegment);

#ifndef DACCESS_COMPILE
    // re-sort the block chains if necessary
    if (pNextSegment && pNextSegment->fResortChains)
        SegmentResortChains(pNextSegment);
#endif

    // return the segment we found
    return pNextSegment;
}


/*
 * FullSegmentIterator
 *
 * Returns the next segment to be scanned in a scanning loop.
 *
 * This iterator performs full maintenance on the segments,
 * including freeing those it notices are empty along the way.
 *
 */
PTR_TableSegment CALLBACK FullSegmentIterator(PTR_HandleTable pTable, PTR_TableSegment pPrevSegment, CrstHolderWithState *)
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_TRIGGERS);
        FORBID_FAULT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    // we will be resetting the next segment's sequence number
    uint32_t uSequence = 0;

    // if we have a previous segment then compute the next sequence number from it
    if (pPrevSegment)
        uSequence = (uint32_t)pPrevSegment->bSequence + 1;

    // loop until we find an appropriate segment to return
    PTR_TableSegment pNextSegment;
    for (;;)
    {
        // first, call the standard iterator to get the next segment
        pNextSegment = StandardSegmentIterator(pTable, pPrevSegment);

        // if there are no more segments then we're done
        if (!pNextSegment)
            break;

#ifndef DACCESS_COMPILE
        // check if we should decommit any excess pages in this segment
        if (DoesSegmentNeedsToTrimExcessPages(pNextSegment))
        {
            CrstHolder ch(&pTable->Lock);
            SegmentTrimExcessPages(pNextSegment);
        }
#endif

        // if the segment has handles in it then it will survive and be returned
        if (pNextSegment->bEmptyLine > 0)
        {
            // update this segment's sequence number
            pNextSegment->bSequence = (uint8_t)(uSequence % 0x100);

            // break out and return the segment
            break;
        }

#ifndef DACCESS_COMPILE
        CrstHolder ch(&pTable->Lock);
        // this segment is completely empty - can we free it now?
        if (pNextSegment->bEmptyLine == 0 && TableCanFreeSegmentNow(pTable, pNextSegment))
        {
            // yup, we probably want to free this one
            PTR_TableSegment pNextNext = pNextSegment->pNextSegment;

            // was this the first segment in the list?
            if (!pPrevSegment)
            {
                // yes - are there more segments?
                if (pNextNext)
                {
                    // yes - unlink the head
                    pTable->pSegmentList = pNextNext;
                }
                else
                {
                    // no - leave this one in the list and enumerate it
                    break;
                }
            }
            else
            {
                // no - unlink this segment from the segment list
                pPrevSegment->pNextSegment = pNextNext;
            }

            // free this segment
            SegmentFree(pNextSegment);
        }
#else
        // The code above has a side effect we need to preserve:
        // while neither pNextSegment nor pPrevSegment are modified, their fields
        // are, which affects the handle table walk. Since TableCanFreeSegmentNow
        // actually only checks to see if something is asynchronously scanning this
        // segment (and returns FALSE if something is), we'll assume it always
        // returns TRUE.  (Since we can't free memory in the Dac, it doesn't matter
        // if there's another async scan going on.)
        pPrevSegment = pNextSegment;
#endif
    }

    // return the segment we found
    return pNextSegment;
}

/*
 * xxxAsyncSegmentIterator
 *
 * Implements the core handle scanning loop for a table.
 *
 * This iterator wraps another iterator, checking for queued blocks from the
 * previous segment before advancing to the next.  If there are queued blocks,
 * the function processes them by calling xxxTableScanQueuedBlocksAsync.
 *
 * N.B. THIS FUNCTION LEAVES THE TABLE LOCK WHILE SCANNING.
 *
 */
PTR_TableSegment CALLBACK xxxAsyncSegmentIterator(PTR_HandleTable pTable, PTR_TableSegment pPrevSegment, CrstHolderWithState *pCrstHolder)
{
    WRAPPER_NO_CONTRACT;

    // fetch our table's async scanning info
    AsyncScanInfo *pAsyncInfo = pTable->pAsyncScanInfo;

    // sanity
    _ASSERTE(pAsyncInfo);

    // if we have queued some blocks from the previous segment then scan them now
    if (pAsyncInfo->pQueueTail)
        xxxTableScanQueuedBlocksAsync(pTable, pPrevSegment, pCrstHolder);

    // fetch the underlying iterator from our async info
    SEGMENTITERATOR pfnCoreIterator = pAsyncInfo->pfnSegmentIterator;

    // call the underlying iterator to get the next segment
    return pfnCoreIterator(pTable, pPrevSegment, pCrstHolder);
}

/*--------------------------------------------------------------------------*/



/****************************************************************************
 *
 * CORE SCANNING LOGIC
 *
 ****************************************************************************/

/*
 * SegmentScanByTypeChain
 *
 * Implements the single-type block scanning loop for a single segment.
 *
 */
void SegmentScanByTypeChain(PTR_TableSegment pSegment, uint32_t uType, BLOCKSCANPROC pfnBlockHandler, ScanCallbackInfo *pInfo)
{
    WRAPPER_NO_CONTRACT;

    // hope we are enumerating a valid type chain :)
    _ASSERTE(uType < HANDLE_MAX_INTERNAL_TYPES);

    // fetch the tail
    uint32_t uBlock = pSegment->rgTail[uType];

    // if we didn't find a terminator then there's blocks to enumerate
    if (uBlock != BLOCK_INVALID)
    {
        // start walking from the head
        uBlock = pSegment->rgAllocation[uBlock];

        // scan until we loop back to the first block
        uint32_t uHead = uBlock;
        do
        {
            // search forward trying to batch up sequential runs of blocks
            uint32_t uLast, uNext = uBlock;
            do
            {
                // compute the next sequential block for comparison
                uLast = uNext + 1;

                // fetch the next block in the allocation chain
                uNext = pSegment->rgAllocation[uNext];

            } while ((uNext == uLast) && (uNext != uHead));

            // call the callback for this group of blocks
            pfnBlockHandler(pSegment, uBlock, (uLast - uBlock), pInfo);

            // advance to the next block
            uBlock = uNext;

        } while (uBlock != uHead);
    }
}


/*
 * SegmentScanByTypeMap
 *
 * Implements the multi-type block scanning loop for a single segment.
 *
 */
void SegmentScanByTypeMap(PTR_TableSegment pSegment, const BOOL *rgTypeInclusion,
                          BLOCKSCANPROC pfnBlockHandler, ScanCallbackInfo *pInfo)
{
    WRAPPER_NO_CONTRACT;

    // start scanning with the first block in the segment
    uint32_t uBlock = 0;

    // we don't need to scan the whole segment, just up to the empty line
    uint32_t uLimit = pSegment->bEmptyLine;

    // loop across the segment looking for blocks to scan
    for (;;)
    {
        // find the first block included by the type map
        for (;;)
        {
            // if we are out of range looking for a start point then we're done
            if (uBlock >= uLimit)
                return;

            // if the type is one we are scanning then we found a start point
            if (IsBlockIncluded(pSegment, uBlock, rgTypeInclusion))
                break;

            // keep searching with the next block
            uBlock++;
        }

        // remember this block as the first that needs scanning
        uint32_t uFirst = uBlock;

        // find the next block not included in the type map
        for (;;)
        {
            // advance the block index
            uBlock++;

            // if we are beyond the limit then we are done
            if (uBlock >= uLimit)
                break;

            // if the type is not one we are scanning then we found an end point
            if (!IsBlockIncluded(pSegment, uBlock, rgTypeInclusion))
                break;
        }

        // call the callback for the group of blocks we found
        pfnBlockHandler(pSegment, uFirst, (uBlock - uFirst), pInfo);

        // look for another range starting with the next block
        uBlock++;
    }
}


/*
 * TableScanHandles
 *
 * Implements the core handle scanning loop for a table.
 *
 */
void CALLBACK TableScanHandles(PTR_HandleTable pTable,
                               const uint32_t *puType,
                               uint32_t uTypeCount,
                               SEGMENTITERATOR pfnSegmentIterator,
                               BLOCKSCANPROC pfnBlockHandler,
                               ScanCallbackInfo *pInfo,
                               CrstHolderWithState *pCrstHolder)
{
    WRAPPER_NO_CONTRACT;

    // sanity - caller must ALWAYS provide a valid ScanCallbackInfo
    _ASSERTE(pInfo);

    // we may need a type inclusion map for multi-type scans
    BOOL rgTypeInclusion[INCLUSION_MAP_SIZE];

    // we only need to scan types if we have a type array and a callback to call
    if (!pfnBlockHandler || !puType)
        uTypeCount = 0;

    // if we will be scanning more than one type then initialize the inclusion map
    if (uTypeCount > 1)
        BuildInclusionMap(rgTypeInclusion, puType, uTypeCount);

    // now, iterate over the segments, scanning blocks of the specified type(s)
    PTR_TableSegment pSegment = NULL;
    while ((pSegment = pfnSegmentIterator(pTable, pSegment, pCrstHolder)) != NULL)
    {
        // if there are types to scan then enumerate the blocks in this segment
        // (we do this test inside the loop since the iterators should still run...)
        if (uTypeCount >= 1)
        {
            // make sure the "current segment" pointer in the scan info is up to date
            pInfo->pCurrentSegment = pSegment;

            // is this a single type or multi-type enumeration?
            if (uTypeCount == 1)
            {
                // single type enumeration - walk the type's allocation chain
                SegmentScanByTypeChain(pSegment, *puType, pfnBlockHandler, pInfo);
            }
            else
            {
                // multi-type enumeration - walk the type map to find eligible blocks
                SegmentScanByTypeMap(pSegment, rgTypeInclusion, pfnBlockHandler, pInfo);
            }

            // make sure the "current segment" pointer in the scan info is up to date
            pInfo->pCurrentSegment = NULL;
        }
    }
}


/*
 * xxxTableScanHandlesAsync
 *
 * Implements asynchronous handle scanning for a table.
 *
 * N.B. THIS FUNCTION LEAVES THE TABLE LOCK WHILE SCANNING.
 *
 */
void CALLBACK xxxTableScanHandlesAsync(PTR_HandleTable pTable,
                                       const uint32_t *puType,
                                       uint32_t uTypeCount,
                                       SEGMENTITERATOR pfnSegmentIterator,
                                       BLOCKSCANPROC pfnBlockHandler,
                                       ScanCallbackInfo *pInfo,
                                       CrstHolderWithState *pCrstHolder)
{
    WRAPPER_NO_CONTRACT;

    // presently only one async scan is allowed at a time
    if (pTable->pAsyncScanInfo)
    {
        // somebody tried to kick off multiple async scans
        _ASSERTE(FALSE);
        return;
    }


    //-------------------------------------------------------------------------------
    // PRE-SCAN PREPARATION

    // we keep an initial scan list node on the stack (for perf)
    ScanQNode initialNode;

    initialNode.pNext    = NULL;
    initialNode.uEntries = 0;

    // initialize our async scanning info
    AsyncScanInfo asyncInfo;

    asyncInfo.pCallbackInfo      = pInfo;
    asyncInfo.pfnSegmentIterator = pfnSegmentIterator;
    asyncInfo.pfnBlockHandler    = pfnBlockHandler;
    asyncInfo.pScanQueue         = &initialNode;
    asyncInfo.pQueueTail         = NULL;

    // link our async scan info into the table
    pTable->pAsyncScanInfo = &asyncInfo;


    //-------------------------------------------------------------------------------
    // PER-SEGMENT ASYNCHRONOUS SCANNING OF BLOCKS
    //

    // call the synchronous scanner with our async callbacks
    TableScanHandles(pTable,
                     puType, uTypeCount,
                     xxxAsyncSegmentIterator,
                     BlockQueueBlocksForAsyncScan,
                     pInfo,
                     pCrstHolder);


    //-------------------------------------------------------------------------------
    // POST-SCAN CLEANUP
    //

    // if we dynamically allocated more nodes then free them now
    if (initialNode.pNext)
    {
        // adjust the head to point to the first dynamically allocated block
        asyncInfo.pScanQueue = initialNode.pNext;

        // loop through and free all the queue nodes
        ProcessScanQueue(&asyncInfo, FreeScanQNode, (uintptr_t)NULL, TRUE);
    }

    // unlink our async scanning info from the table
    pTable->pAsyncScanInfo = NULL;
}

#ifdef DACCESS_COMPILE
// TableSegment is variable size, where the data up to "rgValue" is static,
// then more is committed as TableSegment::bCommitLine * HANDLE_BYTES_PER_BLOCK.
// See SegmentInitialize in HandleTableCore.cpp.
uint32_t TableSegment::DacSize(TADDR addr)
{
    WRAPPER_NO_CONTRACT;

    uint8_t commitLine = 0;
    DacReadAll(addr + offsetof(TableSegment, bCommitLine), &commitLine, sizeof(commitLine), true);

    return offsetof(TableSegment, rgValue) + (uint32_t)commitLine * HANDLE_BYTES_PER_BLOCK;
}
#endif
/*--------------------------------------------------------------------------*/

