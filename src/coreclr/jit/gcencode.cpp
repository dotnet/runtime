// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                          GCEncode                                         XX
XX                                                                           XX
XX   Logic to encode the JIT method header and GC pointer tables             XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "gcinfotypes.h"
#include "patchpointinfo.h"

ReturnKind VarTypeToReturnKind(var_types type)
{
    switch (type)
    {
        case TYP_REF:
            return RT_Object;
        case TYP_BYREF:
            return RT_ByRef;
#ifdef TARGET_X86
        case TYP_FLOAT:
        case TYP_DOUBLE:
            return RT_Float;
#endif // TARGET_X86
        default:
            return RT_Scalar;
    }
}

ReturnKind GCInfo::getReturnKind()
{
    // Note the GCInfo representation only supports structs with up to 2 GC pointers.
    ReturnTypeDesc retTypeDesc = compiler->compRetTypeDesc;
    const unsigned regCount    = retTypeDesc.GetReturnRegCount();

    switch (regCount)
    {
        case 1:
            return VarTypeToReturnKind(retTypeDesc.GetReturnRegType(0));
        case 2:
            return GetStructReturnKind(VarTypeToReturnKind(retTypeDesc.GetReturnRegType(0)),
                                       VarTypeToReturnKind(retTypeDesc.GetReturnRegType(1)));
        default:
#ifdef DEBUG
            for (unsigned i = 0; i < regCount; i++)
            {
                assert(!varTypeIsGC(retTypeDesc.GetReturnRegType(i)));
            }
#endif // DEBUG
            return RT_Scalar;
    }
}

#if !defined(JIT32_GCENCODER) || defined(FEATURE_EH_FUNCLETS)

// gcMarkFilterVarsPinned - Walk all lifetimes and make it so that anything
//     live in a filter is marked as pinned (often by splitting the lifetime
//     so that *only* the filter region is pinned).  This should only be
//     called once (after generating all lifetimes, but before slot ids are
//     finalized.
//
// DevDiv 376329 - The VM has to double report filters and their parent frame
// because they occur during the 1st pass and the parent frame doesn't go dead
// until we start unwinding in the 2nd pass.
//
// Untracked locals will only be reported in non-filter funclets and the
// parent.
// Registers can't be double reported by 2 frames since they're different.
// That just leaves stack variables which might be double reported.
//
// Technically double reporting is only a problem when the GC has to relocate a
// reference. So we avoid that problem by marking all live tracked stack
// variables as pinned inside the filter.  Thus if they are double reported, it
// won't be a problem since they won't be double relocated.
//
void GCInfo::gcMarkFilterVarsPinned()
{
    assert(compiler->ehAnyFunclets());

    for (EHblkDsc* const HBtab : EHClauses(compiler))
    {
        if (HBtab->HasFilter())
        {
            const UNATIVE_OFFSET filterBeg = compiler->ehCodeOffset(HBtab->ebdFilter);
            const UNATIVE_OFFSET filterEnd = compiler->ehCodeOffset(HBtab->ebdHndBeg);

            for (varPtrDsc* varTmp = gcVarPtrList; varTmp != nullptr; varTmp = varTmp->vpdNext)
            {
                // Get hold of the variable's flags.
                const unsigned lowBits = varTmp->vpdVarNum & OFFSET_MASK;

                // Compute the actual lifetime offsets.
                const unsigned begOffs = varTmp->vpdBegOfs;
                const unsigned endOffs = varTmp->vpdEndOfs;

                // Special case: skip any 0-length lifetimes.
                if (endOffs == begOffs)
                {
                    continue;
                }

                // Skip lifetimes with no overlap with the filter
                if ((endOffs <= filterBeg) || (begOffs >= filterEnd))
                {
                    continue;
                }

#ifndef JIT32_GCENCODER
                // Because there is no nesting within filters, nothing
                // should be already pinned.
                // For JIT32_GCENCODER, we should not do this check as gcVarPtrList are always sorted by vpdBegOfs
                // which means that we could see some varPtrDsc that were already pinned by previous splitting.
                assert((lowBits & pinned_OFFSET_FLAG) == 0);
#endif // JIT32_GCENCODER

                if (begOffs < filterBeg)
                {
                    if (endOffs > filterEnd)
                    {
                        // The variable lifetime is starts before AND ends after
                        // the filter, so we need to create 2 new lifetimes:
                        //     (1) a pinned one for the filter
                        //     (2) a regular one for after the filter
                        // and then adjust the original lifetime to end before
                        // the filter.
                        CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
                        if (compiler->verbose)
                        {
                            printf("Splitting lifetime for filter: [%04X, %04X).\nOld: ", filterBeg, filterEnd);
                            gcDumpVarPtrDsc(varTmp);
                        }
#endif // DEBUG

                        varPtrDsc* desc1 = new (compiler, CMK_GC) varPtrDsc;
                        desc1->vpdVarNum = varTmp->vpdVarNum | pinned_OFFSET_FLAG;
                        desc1->vpdBegOfs = filterBeg;
                        desc1->vpdEndOfs = filterEnd;

                        varPtrDsc* desc2 = new (compiler, CMK_GC) varPtrDsc;
                        desc2->vpdVarNum = varTmp->vpdVarNum;
                        desc2->vpdBegOfs = filterEnd;
                        desc2->vpdEndOfs = endOffs;

                        varTmp->vpdEndOfs = filterBeg;

                        gcInsertVarPtrDscSplit(desc1, varTmp);
                        gcInsertVarPtrDscSplit(desc2, varTmp);

#ifdef DEBUG
                        if (compiler->verbose)
                        {
                            printf("New (1 of 3): ");
                            gcDumpVarPtrDsc(varTmp);
                            printf("New (2 of 3): ");
                            gcDumpVarPtrDsc(desc1);
                            printf("New (3 of 3): ");
                            gcDumpVarPtrDsc(desc2);
                        }
#endif // DEBUG
                    }
                    else
                    {
                        // The variable lifetime started before the filter and ends
                        // somewhere inside it, so we only create 1 new lifetime,
                        // and then adjust the original lifetime to end before
                        // the filter.
                        CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
                        if (compiler->verbose)
                        {
                            printf("Splitting lifetime for filter.\nOld: ");
                            gcDumpVarPtrDsc(varTmp);
                        }
#endif // DEBUG

                        varPtrDsc* desc = new (compiler, CMK_GC) varPtrDsc;
                        desc->vpdVarNum = varTmp->vpdVarNum | pinned_OFFSET_FLAG;
                        desc->vpdBegOfs = filterBeg;
                        desc->vpdEndOfs = endOffs;

                        varTmp->vpdEndOfs = filterBeg;

                        gcInsertVarPtrDscSplit(desc, varTmp);

#ifdef DEBUG
                        if (compiler->verbose)
                        {
                            printf("New (1 of 2): ");
                            gcDumpVarPtrDsc(varTmp);
                            printf("New (2 of 2): ");
                            gcDumpVarPtrDsc(desc);
                        }
#endif // DEBUG
                    }
                }
                else
                {
                    if (endOffs > filterEnd)
                    {
                        // The variable lifetime starts inside the filter and
                        // ends somewhere after it, so we create 1 new
                        // lifetime for the part inside the filter and adjust
                        // the start of the original lifetime to be the end
                        // of the filter
                        CLANG_FORMAT_COMMENT_ANCHOR;
#ifdef DEBUG
                        if (compiler->verbose)
                        {
                            printf("Splitting lifetime for filter.\nOld: ");
                            gcDumpVarPtrDsc(varTmp);
                        }
#endif // DEBUG

                        varPtrDsc* desc = new (compiler, CMK_GC) varPtrDsc;
#ifndef JIT32_GCENCODER
                        desc->vpdVarNum = varTmp->vpdVarNum | pinned_OFFSET_FLAG;
                        desc->vpdBegOfs = begOffs;
                        desc->vpdEndOfs = filterEnd;

                        varTmp->vpdBegOfs = filterEnd;
#else
                        // Mark varTmp as pinned and generated use varPtrDsc(desc) as non-pinned
                        // since gcInsertVarPtrDscSplit requires that varTmp->vpdBegOfs must precede desc->vpdBegOfs
                        desc->vpdVarNum = varTmp->vpdVarNum;
                        desc->vpdBegOfs = filterEnd;
                        desc->vpdEndOfs = endOffs;

                        varTmp->vpdVarNum = varTmp->vpdVarNum | pinned_OFFSET_FLAG;
                        varTmp->vpdEndOfs = filterEnd;
#endif

                        gcInsertVarPtrDscSplit(desc, varTmp);

#ifdef DEBUG
                        if (compiler->verbose)
                        {
                            printf("New (1 of 2): ");
                            gcDumpVarPtrDsc(desc);
                            printf("New (2 of 2): ");
                            gcDumpVarPtrDsc(varTmp);
                        }
#endif // DEBUG
                    }
                    else
                    {
                        // The variable lifetime is completely within the filter,
                        // so just add the pinned flag.
                        CLANG_FORMAT_COMMENT_ANCHOR;
#ifdef DEBUG
                        if (compiler->verbose)
                        {
                            printf("Pinning lifetime for filter.\nOld: ");
                            gcDumpVarPtrDsc(varTmp);
                        }
#endif // DEBUG

                        varTmp->vpdVarNum |= pinned_OFFSET_FLAG;
#ifdef DEBUG
                        if (compiler->verbose)
                        {
                            printf("New : ");
                            gcDumpVarPtrDsc(varTmp);
                        }
#endif // DEBUG
                    }
                }
            }
        } // HasFilter
    }     // Foreach EH
}

// gcInsertVarPtrDscSplit - Insert varPtrDsc that were created by splitting lifetimes
//     From gcMarkFilterVarsPinned, we may have created one or two `varPtrDsc`s due to splitting lifetimes
//     and these newly created `varPtrDsc`s should be inserted in gcVarPtrList.
//     However the semantics of this call depend on the architecture.
//
//     x86-GCInfo requires gcVarPtrList to be sorted by vpdBegOfs.
//     Every time inserting an entry we should keep the order of entries.
//     So this function searches for a proper insertion point from "begin" then "desc" gets inserted.
//
//     For other architectures(ones that uses GCInfo{En|De}coder), we don't need any sort.
//     So the argument "begin" is unused and "desc" will be inserted at the front of the list.

void GCInfo::gcInsertVarPtrDscSplit(varPtrDsc* desc, varPtrDsc* begin)
{
#ifndef JIT32_GCENCODER
    (void)begin;
    desc->vpdNext = gcVarPtrList;
    gcVarPtrList  = desc;
#else  // JIT32_GCENCODER
    // "desc" and "begin" must not be null
    assert(desc != nullptr);
    assert(begin != nullptr);

    // The caller must guarantee that desc's BegOfs is equal or greater than begin's
    // since we will search for insertion point from "begin"
    assert(desc->vpdBegOfs >= begin->vpdBegOfs);

    varPtrDsc* varTmp    = begin->vpdNext;
    varPtrDsc* varInsert = begin;

    while (varTmp != nullptr && varTmp->vpdBegOfs < desc->vpdBegOfs)
    {
        varInsert = varTmp;
        varTmp    = varTmp->vpdNext;
    }

    // Insert point cannot be null
    assert(varInsert != nullptr);

    desc->vpdNext      = varInsert->vpdNext;
    varInsert->vpdNext = desc;
#endif // JIT32_GCENCODER
}

#ifdef DEBUG

void GCInfo::gcDumpVarPtrDsc(varPtrDsc* desc)
{
    const int    offs   = (desc->vpdVarNum & ~OFFSET_MASK);
    const GCtype gcType = (desc->vpdVarNum & byref_OFFSET_FLAG) ? GCT_BYREF : GCT_GCREF;
    const bool   isPin  = (desc->vpdVarNum & pinned_OFFSET_FLAG) != 0;

    printf("[%08X] %s%s var at [%s", dspPtr(desc), GCtypeStr(gcType), isPin ? "pinned-ptr" : "",
           compiler->isFramePointerUsed() ? STR_FPBASE : STR_SPBASE);

    if (offs < 0)
    {
        printf("-0x%02X", -offs);
    }
    else if (offs > 0)
    {
        printf("+0x%02X", +offs);
    }

    printf("] live from %04X to %04X\n", desc->vpdBegOfs, desc->vpdEndOfs);
}

#endif // DEBUG

#endif // !defined(JIT32_GCENCODER) || defined(FEATURE_EH_FUNCLETS)

#ifdef JIT32_GCENCODER

#include "emit.h"

/*****************************************************************************/
/*****************************************************************************/

/*****************************************************************************/
// (see jit.h) #define REGEN_SHORTCUTS 0
// To Regenerate the compressed info header shortcuts, define REGEN_SHORTCUTS
// and use the following command line pipe/filter to give you the 128
// most useful encodings.
//
// find . -name regen.txt | xargs cat | grep InfoHdr | sort | uniq -c | sort -r | head -128

// (see jit.h) #define REGEN_CALLPAT 0
// To Regenerate the compressed info header shortcuts, define REGEN_CALLPAT
// and use the following command line pipe/filter to give you the 80
// most useful encodings.
//
// find . -name regen.txt | xargs cat | grep CallSite | sort | uniq -c | sort -r | head -80

#if REGEN_SHORTCUTS || REGEN_CALLPAT
static FILE*     logFile = NULL;
CRITICAL_SECTION logFileLock;
#endif

#if REGEN_CALLPAT
static void regenLog(unsigned codeDelta,
                     unsigned argMask,
                     unsigned regMask,
                     unsigned argCnt,
                     unsigned byrefArgMask,
                     unsigned byrefRegMask,
                     BYTE*    base,
                     unsigned enSize)
{
    CallPattern pat;

    pat.fld.argCnt    = (argCnt < 0xff) ? argCnt : 0xff;
    pat.fld.regMask   = (regMask < 0xff) ? regMask : 0xff;
    pat.fld.argMask   = (argMask < 0xff) ? argMask : 0xff;
    pat.fld.codeDelta = (codeDelta < 0xff) ? codeDelta : 0xff;

    if (logFile == NULL)
    {
        logFile = fopen("regen.txt", "a");
        InitializeCriticalSection(&logFileLock);
    }

    assert(((enSize > 0) && (enSize < 256)) && ((pat.val & 0xffffff) != 0xffffff));

    EnterCriticalSection(&logFileLock);

    fprintf(logFile, "CallSite( 0x%08x, 0x%02x%02x, 0x", pat.val, byrefArgMask, byrefRegMask);

    while (enSize > 0)
    {
        fprintf(logFile, "%02x", *base++);
        enSize--;
    }
    fprintf(logFile, "),\n");
    fflush(logFile);

    LeaveCriticalSection(&logFileLock);
}
#endif

#if REGEN_SHORTCUTS
static void regenLog(unsigned encoding, InfoHdr* header, InfoHdr* state)
{
    if (logFile == NULL)
    {
        logFile = fopen("regen.txt", "a");
        InitializeCriticalSection(&logFileLock);
    }

    EnterCriticalSection(&logFileLock);

    fprintf(logFile, "InfoHdr( %2d, %2d, %1d, %1d, %1d,"
                     " %1d, %1d, %1d, %1d, %1d,"
                     " %1d, %1d, %1d, %1d, %1d, %1d,"
                     " %1d, %1d, %1d,"
                     " %1d, %2d, %2d,"
                     " %2d, %2d, %2d, %2d, %2d, %2d), \n",
            state->prologSize, state->epilogSize, state->epilogCount, state->epilogAtEnd, state->ediSaved,
            state->esiSaved, state->ebxSaved, state->ebpSaved, state->ebpFrame, state->interruptible,
            state->doubleAlign, state->security, state->handlers, state->localloc, state->editNcontinue, state->varargs,
            state->profCallbacks, state->genericsContext, state->genericsContextIsMethodDesc, state->returnKind,
            state->argCount, state->frameSize,
            (state->untrackedCnt <= SET_UNTRACKED_MAX) ? state->untrackedCnt : HAS_UNTRACKED,
            (state->varPtrTableSize == 0) ? 0 : HAS_VARPTR,
            (state->gsCookieOffset == INVALID_GS_COOKIE_OFFSET) ? 0 : HAS_GS_COOKIE_OFFSET,
            (state->syncStartOffset == INVALID_SYNC_OFFSET) ? 0 : HAS_SYNC_OFFSET,
            (state->syncStartOffset == INVALID_SYNC_OFFSET) ? 0 : HAS_SYNC_OFFSET,
            (state->revPInvokeOffset == INVALID_REV_PINVOKE_OFFSET) ? 0 : HAS_REV_PINVOKE_FRAME_OFFSET);

    fflush(logFile);

    LeaveCriticalSection(&logFileLock);
}
#endif

/*****************************************************************************
 *
 *  Given the four parameters return the index into the callPatternTable[]
 *  that is used to encoding these four items.  If an exact match cannot
 *  found then ignore the codeDelta and search the table again for a near
 *  match.
 *  Returns 0..79 for an exact match or
 *         (delta<<8) | (0..79) for a near match.
 *  A near match will be encoded using two bytes, the first byte will
 *  skip the adjustment delta that prevented an exact match and the
 *  rest of the delta plus the other three items are encoded in the
 *  second byte.
 */
int FASTCALL lookupCallPattern(unsigned argCnt, unsigned regMask, unsigned argMask, unsigned codeDelta)
{
    if ((argCnt <= CP_MAX_ARG_CNT) && (argMask <= CP_MAX_ARG_MASK))
    {
        CallPattern pat;

        pat.fld.argCnt    = (BYTE)argCnt;
        pat.fld.regMask   = (BYTE)regMask; // EBP,EBX,ESI,EDI
        pat.fld.argMask   = (BYTE)argMask;
        pat.fld.codeDelta = (BYTE)codeDelta;

        bool     codeDeltaOK = (pat.fld.codeDelta == codeDelta);
        unsigned bestDelta2  = 0xff;
        unsigned bestPattern = 0xff;
        unsigned patval      = pat.val;
        assert(sizeof(CallPattern) == sizeof(unsigned));

        const unsigned* curp = &callPatternTable[0];
        for (unsigned inx = 0; inx < 80; inx++, curp++)
        {
            unsigned curval = *curp;
            if ((patval == curval) && codeDeltaOK)
                return inx;

            if (((patval ^ curval) & 0xffffff) == 0)
            {
                unsigned delta2 = codeDelta - (curval >> 24);
                if (delta2 < bestDelta2)
                {
                    bestDelta2  = delta2;
                    bestPattern = inx;
                }
            }
        }

        if (bestPattern != 0xff)
        {
            return (bestDelta2 << 8) | bestPattern;
        }
    }
    return -1;
}

static bool initNeeded3(unsigned cur, unsigned tgt, unsigned max, unsigned* hint)
{
    assert(cur != tgt);

    unsigned tmp = tgt;
    unsigned nib = 0;
    unsigned cnt = 0;

    while (tmp > max)
    {
        nib = tmp & 0x07;
        tmp >>= 3;
        if (tmp == cur)
        {
            *hint = nib;
            return false;
        }
        cnt++;
    }

    *hint = tmp;
    return true;
}

static bool initNeeded4(unsigned cur, unsigned tgt, unsigned max, unsigned* hint)
{
    assert(cur != tgt);

    unsigned tmp = tgt;
    unsigned nib = 0;
    unsigned cnt = 0;

    while (tmp > max)
    {
        nib = tmp & 0x0f;
        tmp >>= 4;
        if (tmp == cur)
        {
            *hint = nib;
            return false;
        }
        cnt++;
    }

    *hint = tmp;
    return true;
}

static int bigEncoding3(unsigned cur, unsigned tgt, unsigned max)
{
    assert(cur != tgt);

    unsigned tmp = tgt;
    unsigned nib = 0;
    unsigned cnt = 0;

    while (tmp > max)
    {
        nib = tmp & 0x07;
        tmp >>= 3;
        if (tmp == cur)
            break;
        cnt++;
    }
    return cnt;
}

static int bigEncoding4(unsigned cur, unsigned tgt, unsigned max)
{
    assert(cur != tgt);

    unsigned tmp = tgt;
    unsigned nib = 0;
    unsigned cnt = 0;

    while (tmp > max)
    {
        nib = tmp & 0x0f;
        tmp >>= 4;
        if (tmp == cur)
            break;
        cnt++;
    }
    return cnt;
}

BYTE FASTCALL encodeHeaderNext(const InfoHdr& header, InfoHdr* state, BYTE& codeSet)
{
    BYTE encoding = 0xff;
    codeSet       = 1; // codeSet is 1 or 2, depending on whether the returned encoding
                       // corresponds to InfoHdrAdjust, or InfoHdrAdjust2 enumerations.

    if (state->argCount != header.argCount)
    {
        // We have one-byte encodings for 0..8
        if (header.argCount <= SET_ARGCOUNT_MAX)
        {
            state->argCount = header.argCount;
            encoding        = (BYTE)(SET_ARGCOUNT + header.argCount);
            goto DO_RETURN;
        }
        else
        {
            unsigned hint;
            if (initNeeded4(state->argCount, header.argCount, SET_ARGCOUNT_MAX, &hint))
            {
                assert(hint <= SET_ARGCOUNT_MAX);
                state->argCount = (unsigned short)hint;
                encoding        = (BYTE)(SET_ARGCOUNT + hint);
                goto DO_RETURN;
            }
            else
            {
                assert(hint <= 0xf);
                state->argCount <<= 4;
                state->argCount += ((unsigned short)hint);
                encoding = (BYTE)(NEXT_FOUR_ARGCOUNT + hint);
                goto DO_RETURN;
            }
        }
    }

    if (state->frameSize != header.frameSize)
    {
        // We have one-byte encodings for 0..7
        if (header.frameSize <= SET_FRAMESIZE_MAX)
        {
            state->frameSize = header.frameSize;
            encoding         = (BYTE)(SET_FRAMESIZE + header.frameSize);
            goto DO_RETURN;
        }
        else
        {
            unsigned hint;
            if (initNeeded4(state->frameSize, header.frameSize, SET_FRAMESIZE_MAX, &hint))
            {
                assert(hint <= SET_FRAMESIZE_MAX);
                state->frameSize = hint;
                encoding         = (BYTE)(SET_FRAMESIZE + hint);
                goto DO_RETURN;
            }
            else
            {
                assert(hint <= 0xf);
                state->frameSize <<= 4;
                state->frameSize += hint;
                encoding = (BYTE)(NEXT_FOUR_FRAMESIZE + hint);
                goto DO_RETURN;
            }
        }
    }

    if ((state->epilogCount != header.epilogCount) || (state->epilogAtEnd != header.epilogAtEnd))
    {
        if (header.epilogCount > SET_EPILOGCNT_MAX)
            IMPL_LIMITATION("More than SET_EPILOGCNT_MAX epilogs");

        state->epilogCount = header.epilogCount;
        state->epilogAtEnd = header.epilogAtEnd;
        encoding           = SET_EPILOGCNT + header.epilogCount * 2;
        if (header.epilogAtEnd)
            encoding++;
        goto DO_RETURN;
    }

    if (state->varPtrTableSize != header.varPtrTableSize)
    {
        assert(state->varPtrTableSize == 0 || state->varPtrTableSize == HAS_VARPTR);

        if (state->varPtrTableSize == 0)
        {
            state->varPtrTableSize = HAS_VARPTR;
            encoding               = FLIP_VAR_PTR_TABLE_SZ;
            goto DO_RETURN;
        }
        else if (header.varPtrTableSize == 0)
        {
            state->varPtrTableSize = 0;
            encoding               = FLIP_VAR_PTR_TABLE_SZ;
            goto DO_RETURN;
        }
    }

    if (state->untrackedCnt != header.untrackedCnt)
    {
        assert(state->untrackedCnt <= SET_UNTRACKED_MAX || state->untrackedCnt == HAS_UNTRACKED);

        // We have one-byte encodings for 0..3
        if (header.untrackedCnt <= SET_UNTRACKED_MAX)
        {
            state->untrackedCnt = header.untrackedCnt;
            encoding            = (BYTE)(SET_UNTRACKED + header.untrackedCnt);
            goto DO_RETURN;
        }
        else if (state->untrackedCnt != HAS_UNTRACKED)
        {
            state->untrackedCnt = HAS_UNTRACKED;
            encoding            = FFFF_UNTRACKED_CNT;
            goto DO_RETURN;
        }
    }

    if (state->epilogSize != header.epilogSize)
    {
        // We have one-byte encodings for 0..10
        if (header.epilogSize <= SET_EPILOGSIZE_MAX)
        {
            state->epilogSize = header.epilogSize;
            encoding          = SET_EPILOGSIZE + header.epilogSize;
            goto DO_RETURN;
        }
        else
        {
            unsigned hint;
            if (initNeeded3(state->epilogSize, header.epilogSize, SET_EPILOGSIZE_MAX, &hint))
            {
                assert(hint <= SET_EPILOGSIZE_MAX);
                state->epilogSize = (BYTE)hint;
                encoding          = (BYTE)(SET_EPILOGSIZE + hint);
                goto DO_RETURN;
            }
            else
            {
                assert(hint <= 0x7);
                state->epilogSize <<= 3;
                state->epilogSize += (BYTE)hint;
                encoding = (BYTE)(NEXT_THREE_EPILOGSIZE + hint);
                goto DO_RETURN;
            }
        }
    }

    if (state->prologSize != header.prologSize)
    {
        // We have one-byte encodings for 0..16
        if (header.prologSize <= SET_PROLOGSIZE_MAX)
        {
            state->prologSize = header.prologSize;
            encoding          = SET_PROLOGSIZE + header.prologSize;
            goto DO_RETURN;
        }
        else
        {
            unsigned hint;
            assert(SET_PROLOGSIZE_MAX > 15);
            if (initNeeded3(state->prologSize, header.prologSize, 15, &hint))
            {
                assert(hint <= 15);
                state->prologSize = (BYTE)hint;
                encoding          = (BYTE)(SET_PROLOGSIZE + hint);
                goto DO_RETURN;
            }
            else
            {
                assert(hint <= 0x7);
                state->prologSize <<= 3;
                state->prologSize += ((BYTE)hint);
                encoding = (BYTE)(NEXT_THREE_PROLOGSIZE + hint);
                goto DO_RETURN;
            }
        }
    }

    if (state->ediSaved != header.ediSaved)
    {
        state->ediSaved = header.ediSaved;
        encoding        = FLIP_EDI_SAVED;
        goto DO_RETURN;
    }

    if (state->esiSaved != header.esiSaved)
    {
        state->esiSaved = header.esiSaved;
        encoding        = FLIP_ESI_SAVED;
        goto DO_RETURN;
    }

    if (state->ebxSaved != header.ebxSaved)
    {
        state->ebxSaved = header.ebxSaved;
        encoding        = FLIP_EBX_SAVED;
        goto DO_RETURN;
    }

    if (state->ebpSaved != header.ebpSaved)
    {
        state->ebpSaved = header.ebpSaved;
        encoding        = FLIP_EBP_SAVED;
        goto DO_RETURN;
    }

    if (state->ebpFrame != header.ebpFrame)
    {
        state->ebpFrame = header.ebpFrame;
        encoding        = FLIP_EBP_FRAME;
        goto DO_RETURN;
    }

    if (state->interruptible != header.interruptible)
    {
        state->interruptible = header.interruptible;
        encoding             = FLIP_INTERRUPTIBLE;
        goto DO_RETURN;
    }

#if DOUBLE_ALIGN
    if (state->doubleAlign != header.doubleAlign)
    {
        state->doubleAlign = header.doubleAlign;
        encoding           = FLIP_DOUBLE_ALIGN;
        goto DO_RETURN;
    }
#endif

    if (state->security != header.security)
    {
        state->security = header.security;
        encoding        = FLIP_SECURITY;
        goto DO_RETURN;
    }

    if (state->handlers != header.handlers)
    {
        state->handlers = header.handlers;
        encoding        = FLIP_HANDLERS;
        goto DO_RETURN;
    }

    if (state->localloc != header.localloc)
    {
        state->localloc = header.localloc;
        encoding        = FLIP_LOCALLOC;
        goto DO_RETURN;
    }

    if (state->editNcontinue != header.editNcontinue)
    {
        state->editNcontinue = header.editNcontinue;
        encoding             = FLIP_EDITnCONTINUE;
        goto DO_RETURN;
    }

    if (state->varargs != header.varargs)
    {
        state->varargs = header.varargs;
        encoding       = FLIP_VARARGS;
        goto DO_RETURN;
    }

    if (state->profCallbacks != header.profCallbacks)
    {
        state->profCallbacks = header.profCallbacks;
        encoding             = FLIP_PROF_CALLBACKS;
        goto DO_RETURN;
    }

    if (state->genericsContext != header.genericsContext)
    {
        state->genericsContext = header.genericsContext;
        encoding               = FLIP_HAS_GENERICS_CONTEXT;
        goto DO_RETURN;
    }

    if (state->genericsContextIsMethodDesc != header.genericsContextIsMethodDesc)
    {
        state->genericsContextIsMethodDesc = header.genericsContextIsMethodDesc;
        encoding                           = FLIP_GENERICS_CONTEXT_IS_METHODDESC;
        goto DO_RETURN;
    }

    if (state->returnKind != header.returnKind)
    {
        state->returnKind = header.returnKind;
        codeSet           = 2; // Two byte encoding
        encoding          = header.returnKind;
        _ASSERTE(encoding < SET_RET_KIND_MAX);
        goto DO_RETURN;
    }

    if (state->gsCookieOffset != header.gsCookieOffset)
    {
        assert(state->gsCookieOffset == INVALID_GS_COOKIE_OFFSET || state->gsCookieOffset == HAS_GS_COOKIE_OFFSET);

        if (state->gsCookieOffset == INVALID_GS_COOKIE_OFFSET)
        {
            // header.gsCookieOffset is non-zero. We can set it
            // to zero using FLIP_HAS_GS_COOKIE
            state->gsCookieOffset = HAS_GS_COOKIE_OFFSET;
            encoding              = FLIP_HAS_GS_COOKIE;
            goto DO_RETURN;
        }
        else if (header.gsCookieOffset == INVALID_GS_COOKIE_OFFSET)
        {
            state->gsCookieOffset = INVALID_GS_COOKIE_OFFSET;
            encoding              = FLIP_HAS_GS_COOKIE;
            goto DO_RETURN;
        }
    }

    if (state->syncStartOffset != header.syncStartOffset)
    {
        assert(state->syncStartOffset == INVALID_SYNC_OFFSET || state->syncStartOffset == HAS_SYNC_OFFSET);

        if (state->syncStartOffset == INVALID_SYNC_OFFSET)
        {
            // header.syncStartOffset is non-zero. We can set it
            // to zero using FLIP_SYNC
            state->syncStartOffset = HAS_SYNC_OFFSET;
            encoding               = FLIP_SYNC;
            goto DO_RETURN;
        }
        else if (header.syncStartOffset == INVALID_SYNC_OFFSET)
        {
            state->syncStartOffset = INVALID_SYNC_OFFSET;
            encoding               = FLIP_SYNC;
            goto DO_RETURN;
        }
    }

    if (state->revPInvokeOffset != header.revPInvokeOffset)
    {
        assert(state->revPInvokeOffset == INVALID_REV_PINVOKE_OFFSET ||
               state->revPInvokeOffset == HAS_REV_PINVOKE_FRAME_OFFSET);

        if (state->revPInvokeOffset == INVALID_REV_PINVOKE_OFFSET)
        {
            // header.revPInvokeOffset is non-zero.
            state->revPInvokeOffset = HAS_REV_PINVOKE_FRAME_OFFSET;
            encoding                = FLIP_REV_PINVOKE_FRAME;
            goto DO_RETURN;
        }
        else if (header.revPInvokeOffset == INVALID_REV_PINVOKE_OFFSET)
        {
            state->revPInvokeOffset = INVALID_REV_PINVOKE_OFFSET;
            encoding                = FLIP_REV_PINVOKE_FRAME;
            goto DO_RETURN;
        }
    }

DO_RETURN:
    _ASSERTE(encoding < MORE_BYTES_TO_FOLLOW);
    if (!state->isHeaderMatch(header))
        encoding |= MORE_BYTES_TO_FOLLOW;

    return encoding;
}

static int measureDistance(const InfoHdr& header, const InfoHdrSmall* p, int closeness)
{
    int distance = 0;

    if (p->untrackedCnt != header.untrackedCnt)
    {
        if (header.untrackedCnt > 3)
        {
            if (p->untrackedCnt != HAS_UNTRACKED)
                distance += 1;
        }
        else
        {
            distance += 1;
        }
        if (distance >= closeness)
            return distance;
    }

    if (p->varPtrTableSize != header.varPtrTableSize)
    {
        if (header.varPtrTableSize != 0)
        {
            if (p->varPtrTableSize != HAS_VARPTR)
                distance += 1;
        }
        else
        {
            assert(p->varPtrTableSize == HAS_VARPTR);
            distance += 1;
        }
        if (distance >= closeness)
            return distance;
    }

    if (p->frameSize != header.frameSize)
    {
        distance += 1;
        if (distance >= closeness)
            return distance;

        // We have one-byte encodings for 0..7
        if (header.frameSize > SET_FRAMESIZE_MAX)
        {
            distance += bigEncoding4(p->frameSize, header.frameSize, SET_FRAMESIZE_MAX);
            if (distance >= closeness)
                return distance;
        }
    }

    if (p->argCount != header.argCount)
    {
        distance += 1;
        if (distance >= closeness)
            return distance;

        // We have one-byte encodings for 0..8
        if (header.argCount > SET_ARGCOUNT_MAX)
        {
            distance += bigEncoding4(p->argCount, header.argCount, SET_ARGCOUNT_MAX);
            if (distance >= closeness)
                return distance;
        }
    }

    if (p->prologSize != header.prologSize)
    {
        distance += 1;
        if (distance >= closeness)
            return distance;

        // We have one-byte encodings for 0..16
        if (header.prologSize > SET_PROLOGSIZE_MAX)
        {
            assert(SET_PROLOGSIZE_MAX > 15);
            distance += bigEncoding3(p->prologSize, header.prologSize, 15);
            if (distance >= closeness)
                return distance;
        }
    }

    if (p->epilogSize != header.epilogSize)
    {
        distance += 1;
        if (distance >= closeness)
            return distance;
        // We have one-byte encodings for 0..10
        if (header.epilogSize > SET_EPILOGSIZE_MAX)
        {
            distance += bigEncoding3(p->epilogSize, header.epilogSize, SET_EPILOGSIZE_MAX);
            if (distance >= closeness)
                return distance;
        }
    }

    if ((p->epilogCount != header.epilogCount) || (p->epilogAtEnd != header.epilogAtEnd))
    {
        distance += 1;
        if (distance >= closeness)
            return distance;

        if (header.epilogCount > SET_EPILOGCNT_MAX)
            IMPL_LIMITATION("More than SET_EPILOGCNT_MAX epilogs");
    }

    if (p->ediSaved != header.ediSaved)
    {
        distance += 1;
        if (distance >= closeness)
            return distance;
    }

    if (p->esiSaved != header.esiSaved)
    {
        distance += 1;
        if (distance >= closeness)
            return distance;
    }

    if (p->ebxSaved != header.ebxSaved)
    {
        distance += 1;
        if (distance >= closeness)
            return distance;
    }

    if (p->ebpSaved != header.ebpSaved)
    {
        distance += 1;
        if (distance >= closeness)
            return distance;
    }

    if (p->ebpFrame != header.ebpFrame)
    {
        distance += 1;
        if (distance >= closeness)
            return distance;
    }

    if (p->interruptible != header.interruptible)
    {
        distance += 1;
        if (distance >= closeness)
            return distance;
    }

#if DOUBLE_ALIGN
    if (p->doubleAlign != header.doubleAlign)
    {
        distance += 1;
        if (distance >= closeness)
            return distance;
    }
#endif

    if (p->security != header.security)
    {
        distance += 1;
        if (distance >= closeness)
            return distance;
    }

    if (p->handlers != header.handlers)
    {
        distance += 1;
        if (distance >= closeness)
            return distance;
    }

    if (p->localloc != header.localloc)
    {
        distance += 1;
        if (distance >= closeness)
            return distance;
    }

    if (p->editNcontinue != header.editNcontinue)
    {
        distance += 1;
        if (distance >= closeness)
            return distance;
    }

    if (p->varargs != header.varargs)
    {
        distance += 1;
        if (distance >= closeness)
            return distance;
    }

    if (p->profCallbacks != header.profCallbacks)
    {
        distance += 1;
        if (distance >= closeness)
            return distance;
    }

    if (p->genericsContext != header.genericsContext)
    {
        distance += 1;
        if (distance >= closeness)
            return distance;
    }

    if (p->genericsContextIsMethodDesc != header.genericsContextIsMethodDesc)
    {
        distance += 1;
        if (distance >= closeness)
            return distance;
    }

    if (p->returnKind != header.returnKind)
    {
        // Setting the ReturnKind requires two bytes of encoding.
        distance += 2;
        if (distance >= closeness)
            return distance;
    }

    if (header.gsCookieOffset != INVALID_GS_COOKIE_OFFSET)
    {
        distance += 1;
        if (distance >= closeness)
            return distance;
    }

    if (header.syncStartOffset != INVALID_SYNC_OFFSET)
    {
        distance += 1;
        if (distance >= closeness)
            return distance;
    }

    if (header.revPInvokeOffset != INVALID_REV_PINVOKE_OFFSET)
    {
        distance += 1;
        if (distance >= closeness)
            return distance;
    }

    return distance;
}

// DllMain calls gcInitEncoderLookupTable to fill in this table
/* extern */ int infoHdrLookup[IH_MAX_PROLOG_SIZE + 2];

/* static */ void GCInfo::gcInitEncoderLookupTable()
{
    const InfoHdrSmall* p  = &infoHdrShortcut[0];
    int                 lo = -1;
    int                 hi = 0;
    int                 n;

    for (n = 0; n < 128; n++, p++)
    {
        if (p->prologSize != lo)
        {
            if (p->prologSize < lo)
            {
                assert(p->prologSize == 0);
                hi = IH_MAX_PROLOG_SIZE;
            }
            else
                hi = p->prologSize;

            assert(hi <= IH_MAX_PROLOG_SIZE);

            while (lo < hi)
                infoHdrLookup[++lo] = n;

            if (lo == IH_MAX_PROLOG_SIZE)
                break;
        }
    }

    assert(lo == IH_MAX_PROLOG_SIZE);
    assert(infoHdrLookup[IH_MAX_PROLOG_SIZE] < 128);

    while (p->prologSize == lo)
    {
        n++;
        if (n >= 128)
            break;
        p++;
    }

    infoHdrLookup[++lo] = n;

#ifdef DEBUG
    //
    // We do some other DEBUG only validity checks here
    //
    assert(callCommonDelta[0] < callCommonDelta[1]);
    assert(callCommonDelta[1] < callCommonDelta[2]);
    assert(callCommonDelta[2] < callCommonDelta[3]);
    assert(sizeof(CallPattern) == sizeof(unsigned));
    unsigned maxMarks = 0;
    for (unsigned inx = 0; inx < 80; inx++)
    {
        CallPattern pat;
        pat.val = callPatternTable[inx];

        assert(pat.fld.codeDelta <= CP_MAX_CODE_DELTA);
        if (pat.fld.codeDelta == CP_MAX_CODE_DELTA)
            maxMarks |= 0x01;

        assert(pat.fld.argCnt <= CP_MAX_ARG_CNT);
        if (pat.fld.argCnt == CP_MAX_ARG_CNT)
            maxMarks |= 0x02;

        assert(pat.fld.argMask <= CP_MAX_ARG_MASK);
        if (pat.fld.argMask == CP_MAX_ARG_MASK)
            maxMarks |= 0x04;
    }
    assert(maxMarks == 0x07);
#endif
}

const int NO_CACHED_HEADER = -1;

BYTE FASTCALL encodeHeaderFirst(const InfoHdr& header, InfoHdr* state, int* more, int* pCached)
{
    // First try the cached value for an exact match, if there is one
    //
    int                 n = *pCached;
    const InfoHdrSmall* p;

    if (n != NO_CACHED_HEADER)
    {
        p = &infoHdrShortcut[n];
        if (p->isHeaderMatch(header))
        {
            // exact match found
            GetInfoHdr(n, state);
            *more = 0;
            return (BYTE)n;
        }
    }

    // Next search the table for an exact match
    // Only search entries that have a matching prolog size
    // Note: lo and hi are saved here as they specify the
    // range of entries that have the correct prolog size
    //
    unsigned psz = header.prologSize;
    int      lo  = 0;
    int      hi  = 0;

    if (psz <= IH_MAX_PROLOG_SIZE)
    {
        lo = infoHdrLookup[psz];
        hi = infoHdrLookup[psz + 1];
        p  = &infoHdrShortcut[lo];
        for (n = lo; n < hi; n++, p++)
        {
            assert(psz == p->prologSize);
            if (p->isHeaderMatch(header))
            {
                // exact match found
                GetInfoHdr(n, state);
                *pCached = n; // cache the value
                *more    = 0;
                return (BYTE)n;
            }
        }
    }

    //
    // no exact match in infoHdrShortcut[]
    //
    // find the nearest entry in the table
    //
    int nearest   = -1;
    int closeness = 255; // (i.e. not very close)

    //
    // Calculate the minimum acceptable distance
    // if we find an entry that is at least this close
    // we will stop the search and use that value
    //
    int min_acceptable_distance = 1;

    if (header.frameSize > SET_FRAMESIZE_MAX)
    {
        ++min_acceptable_distance;
        if (header.frameSize > 32)
            ++min_acceptable_distance;
    }
    if (header.argCount > SET_ARGCOUNT_MAX)
    {
        ++min_acceptable_distance;
        if (header.argCount > 32)
            ++min_acceptable_distance;
    }

    // First try the cached value
    // and see if it meets the minimum acceptable distance
    //
    if (*pCached != NO_CACHED_HEADER)
    {
        p            = &infoHdrShortcut[*pCached];
        int distance = measureDistance(header, p, closeness);
        assert(distance > 0);
        if (distance <= min_acceptable_distance)
        {
            GetInfoHdr(*pCached, state);
            *more = distance;
            return (BYTE)(0x80 | *pCached);
        }
        else
        {
            closeness = distance;
            nearest   = *pCached;
        }
    }

    // Then try the ones pointed to by [lo..hi),
    // (i.e. the ones that have the correct prolog size)
    //
    p = &infoHdrShortcut[lo];
    for (n = lo; n < hi; n++, p++)
    {
        if (n == *pCached)
            continue; // already tried this one
        int distance = measureDistance(header, p, closeness);
        assert(distance > 0);
        if (distance <= min_acceptable_distance)
        {
            GetInfoHdr(n, state);
            *pCached = n; // Cache this value
            *more    = distance;
            return (BYTE)(0x80 | n);
        }
        else if (distance < closeness)
        {
            closeness = distance;
            nearest   = n;
        }
    }

    int last = infoHdrLookup[IH_MAX_PROLOG_SIZE + 1];
    assert(last <= 128);

    // Then try all the rest [0..last-1]
    p = &infoHdrShortcut[0];
    for (n = 0; n < last; n++, p++)
    {
        if (n == *pCached)
            continue; // already tried this one
        if ((n >= lo) && (n < hi))
            continue; // already tried these
        int distance = measureDistance(header, p, closeness);
        assert(distance > 0);
        if (distance <= min_acceptable_distance)
        {
            GetInfoHdr(n, state);
            *pCached = n; // Cache this value
            *more    = distance;
            return (BYTE)(0x80 | n);
        }
        else if (distance < closeness)
        {
            closeness = distance;
            nearest   = n;
        }
    }

    //
    // If we reach here then there was no adjacent neighbor
    //  in infoHdrShortcut[], closeness indicate how many extra
    //  bytes we will need to encode this item.
    //
    assert((nearest >= 0) && (nearest <= 127));
    GetInfoHdr(nearest, state);
    *pCached = nearest; // Cache this value
    *more    = closeness;
    return (BYTE)(0x80 | nearest);
}

/*****************************************************************************
 *
 *  Write the initial part of the method info block. This is called twice;
 *  first to compute the size needed for the info (mask=0), the second time
 *  to actually generate the contents of the table (mask=-1,dest!=NULL).
 */

size_t GCInfo::gcInfoBlockHdrSave(
    BYTE* dest, int mask, unsigned methodSize, unsigned prologSize, unsigned epilogSize, InfoHdr* header, int* pCached)
{
#ifdef DEBUG
    if (compiler->verbose)
        printf("*************** In gcInfoBlockHdrSave()\n");
#endif
    size_t size = 0;

#if VERIFY_GC_TABLES
    *castto(dest, unsigned short*)++ = 0xFEEF;
    size += sizeof(short);
#endif

    /* Write the method size first (using between 1 and 5 bytes) */
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
    if (compiler->verbose)
    {
        if (mask)
            printf("GCINFO: methodSize = %04X\n", methodSize);
        if (mask)
            printf("GCINFO: prologSize = %04X\n", prologSize);
        if (mask)
            printf("GCINFO: epilogSize = %04X\n", epilogSize);
    }
#endif

    size_t methSz = encodeUnsigned(dest, methodSize);
    size += methSz;
    dest += methSz & mask;

    //
    // New style InfoBlk Header
    //
    // Typically only uses one-byte to store everything.
    //

    if (mask == 0)
    {
        memset(header, 0, sizeof(InfoHdr));
        *pCached = NO_CACHED_HEADER;
    }

    assert(FitsIn<unsigned char>(prologSize));
    header->prologSize = static_cast<unsigned char>(prologSize);
    assert(FitsIn<unsigned char>(epilogSize));
    header->epilogSize  = static_cast<unsigned char>(epilogSize);
    header->epilogCount = compiler->GetEmitter()->emitGetEpilogCnt();
    if (header->epilogCount != compiler->GetEmitter()->emitGetEpilogCnt())
        IMPL_LIMITATION("emitGetEpilogCnt() does not fit in InfoHdr::epilogCount");
    header->epilogAtEnd = compiler->GetEmitter()->emitHasEpilogEnd();

    if (compiler->codeGen->regSet.rsRegsModified(RBM_EDI))
        header->ediSaved = 1;
    if (compiler->codeGen->regSet.rsRegsModified(RBM_ESI))
        header->esiSaved = 1;
    if (compiler->codeGen->regSet.rsRegsModified(RBM_EBX))
        header->ebxSaved = 1;

    header->interruptible = compiler->codeGen->GetInterruptible();

    if (!compiler->isFramePointerUsed())
    {
#if DOUBLE_ALIGN
        if (compiler->genDoubleAlign())
        {
            header->ebpSaved = true;
            assert(!compiler->codeGen->regSet.rsRegsModified(RBM_EBP));
        }
#endif
        if (compiler->codeGen->regSet.rsRegsModified(RBM_EBP))
        {
            header->ebpSaved = true;
        }
    }
    else
    {
        header->ebpSaved = true;
        header->ebpFrame = true;
    }

#if DOUBLE_ALIGN
    header->doubleAlign = compiler->genDoubleAlign();
#endif

    header->security = false;

    header->handlers = compiler->ehHasCallableHandlers();
    header->localloc = compiler->compLocallocUsed;

    header->varargs         = compiler->info.compIsVarArgs;
    header->profCallbacks   = compiler->info.compProfilerCallback;
    header->editNcontinue   = compiler->opts.compDbgEnC;
    header->genericsContext = compiler->lvaReportParamTypeArg();
    header->genericsContextIsMethodDesc =
        header->genericsContext && (compiler->info.compMethodInfo->options & (CORINFO_GENERICS_CTXT_FROM_METHODDESC));

    ReturnKind returnKind = getReturnKind();
    _ASSERTE(IsValidReturnKind(returnKind) && "Return Kind must be valid");
    _ASSERTE(!IsStructReturnKind(returnKind) && "Struct Return Kinds Unexpected for JIT32");
    _ASSERTE(((int)returnKind < (int)SET_RET_KIND_MAX) && "ReturnKind has no legal encoding");
    header->returnKind = returnKind;

    header->gsCookieOffset = INVALID_GS_COOKIE_OFFSET;
    if (compiler->getNeedsGSSecurityCookie())
    {
        assert(compiler->lvaGSSecurityCookie != BAD_VAR_NUM);
        int stkOffs            = compiler->lvaTable[compiler->lvaGSSecurityCookie].GetStackOffset();
        header->gsCookieOffset = compiler->isFramePointerUsed() ? -stkOffs : stkOffs;
        assert(header->gsCookieOffset != INVALID_GS_COOKIE_OFFSET);
    }

    header->syncStartOffset = INVALID_SYNC_OFFSET;
    header->syncEndOffset   = INVALID_SYNC_OFFSET;

#ifndef UNIX_X86_ABI
    // JIT is responsible for synchronization on funclet-based EH model that x86/Linux uses.
    if (compiler->info.compFlags & CORINFO_FLG_SYNCH)
    {
        assert(compiler->syncStartEmitCookie != nullptr);
        header->syncStartOffset = compiler->GetEmitter()->emitCodeOffset(compiler->syncStartEmitCookie, 0);
        assert(header->syncStartOffset != INVALID_SYNC_OFFSET);

        assert(compiler->syncEndEmitCookie != nullptr);
        header->syncEndOffset = compiler->GetEmitter()->emitCodeOffset(compiler->syncEndEmitCookie, 0);
        assert(header->syncEndOffset != INVALID_SYNC_OFFSET);

        assert(header->syncStartOffset < header->syncEndOffset);
        // synchronized methods can't have more than 1 epilog
        assert(header->epilogCount <= 1);
    }
#endif

    header->revPInvokeOffset = INVALID_REV_PINVOKE_OFFSET;
    if (compiler->opts.IsReversePInvoke())
    {
        assert(compiler->lvaReversePInvokeFrameVar != BAD_VAR_NUM);
        int stkOffs              = compiler->lvaTable[compiler->lvaReversePInvokeFrameVar].GetStackOffset();
        header->revPInvokeOffset = compiler->isFramePointerUsed() ? -stkOffs : stkOffs;
        assert(header->revPInvokeOffset != INVALID_REV_PINVOKE_OFFSET);
    }

    assert((compiler->compArgSize & 0x3) == 0);

    size_t argCount =
        (compiler->compArgSize - (compiler->codeGen->intRegState.rsCalleeRegArgCount * REGSIZE_BYTES)) / REGSIZE_BYTES;
    assert(argCount <= MAX_USHORT_SIZE_T);
    header->argCount = static_cast<unsigned short>(argCount);

    header->frameSize = compiler->compLclFrameSize / sizeof(int);
    if (header->frameSize != (compiler->compLclFrameSize / sizeof(int)))
        IMPL_LIMITATION("compLclFrameSize does not fit in InfoHdr::frameSize");

    if (mask == 0)
    {
        gcCountForHeader((UNALIGNED unsigned int*)&header->untrackedCnt,
                         (UNALIGNED unsigned int*)&header->varPtrTableSize);
    }

    //
    // If the high-order bit of headerEncoding is set
    // then additional bytes will update the InfoHdr state
    // until the fully state is encoded
    //
    InfoHdr state;
    int     more           = 0;
    BYTE    headerEncoding = encodeHeaderFirst(*header, &state, &more, pCached);
    ++size;
    if (mask)
    {
#if REGEN_SHORTCUTS
        regenLog(headerEncoding, header, &state);
#endif
        *dest++ = headerEncoding;

        BYTE encoding = headerEncoding;
        BYTE codeSet  = 1;
        while (encoding & MORE_BYTES_TO_FOLLOW)
        {
            encoding = encodeHeaderNext(*header, &state, codeSet);

#if REGEN_SHORTCUTS
            regenLog(headerEncoding, header, &state);
#endif
            _ASSERTE((codeSet == 1 || codeSet == 2) && "Encoding must correspond to InfoHdrAdjust or InfoHdrAdjust2");
            if (codeSet == 2)
            {
                *dest++ = NEXT_OPCODE | MORE_BYTES_TO_FOLLOW;
                ++size;
            }

            *dest++ = encoding;
            ++size;
        }
    }
    else
    {
        size += more;
    }

    if (header->untrackedCnt > SET_UNTRACKED_MAX)
    {
        unsigned count = header->untrackedCnt;
        unsigned sz    = encodeUnsigned(mask ? dest : NULL, count);
        size += sz;
        dest += (sz & mask);
    }

    if (header->varPtrTableSize != 0)
    {
        unsigned count = header->varPtrTableSize;
        unsigned sz    = encodeUnsigned(mask ? dest : NULL, count);
        size += sz;
        dest += (sz & mask);
    }

    if (header->gsCookieOffset != INVALID_GS_COOKIE_OFFSET)
    {
        assert(mask == 0 || state.gsCookieOffset == HAS_GS_COOKIE_OFFSET);
        unsigned offset = header->gsCookieOffset;
        unsigned sz     = encodeUnsigned(mask ? dest : NULL, offset);
        size += sz;
        dest += (sz & mask);
    }

    if (header->syncStartOffset != INVALID_SYNC_OFFSET)
    {
        assert(mask == 0 || state.syncStartOffset == HAS_SYNC_OFFSET);

        {
            unsigned offset = header->syncStartOffset;
            unsigned sz     = encodeUnsigned(mask ? dest : NULL, offset);
            size += sz;
            dest += (sz & mask);
        }

        {
            unsigned offset = header->syncEndOffset;
            unsigned sz     = encodeUnsigned(mask ? dest : NULL, offset);
            size += sz;
            dest += (sz & mask);
        }
    }

    if (header->revPInvokeOffset != INVALID_REV_PINVOKE_OFFSET)
    {
        assert(mask == 0 || state.revPInvokeOffset == HAS_REV_PINVOKE_FRAME_OFFSET);
        unsigned offset = header->revPInvokeOffset;
        unsigned sz     = encodeUnsigned(mask ? dest : NULL, offset);
        size += sz;
        dest += (sz & mask);
    }

    if (header->epilogCount)
    {
        /* Generate table unless one epilog at the end of the method */

        if (header->epilogAtEnd == 0 || header->epilogCount != 1)
        {
#if VERIFY_GC_TABLES
            *castto(dest, unsigned short*)++ = 0xFACE;
            size += sizeof(short);
#endif

            /* Simply write a sorted array of offsets using encodeUDelta */

            gcEpilogTable      = mask ? dest : NULL;
            gcEpilogPrevOffset = 0;

            size_t sz = compiler->GetEmitter()->emitGenEpilogLst(gcRecordEpilog, this);

            /* Add the size of the epilog table to the total size */

            size += sz;
            dest += (sz & mask);
        }
    }

#if DISPLAY_SIZES

    if (mask)
    {
        if (compiler->codeGen->GetInterruptible())
        {
            genMethodICnt++;
        }
        else
        {
            genMethodNCnt++;
        }
    }

#endif // DISPLAY_SIZES

    return size;
}

/*****************************************************************************
 *
 *  Return the size of the pointer tracking tables.
 */

size_t GCInfo::gcPtrTableSize(const InfoHdr& header, unsigned codeSize, size_t* pArgTabOffset)
{
    BYTE temp[16 + 1];
#ifdef DEBUG
    temp[16] = 0xAB; // Set some marker
#endif

    /* Compute the total size of the tables */

    size_t size = gcMakeRegPtrTable(temp, 0, header, codeSize, pArgTabOffset);

    assert(temp[16] == 0xAB); // Check that marker didnt get overwritten

    return size;
}

/*****************************************************************************
 * Encode the callee-saved registers into 3 bits.
 */

unsigned gceEncodeCalleeSavedRegs(unsigned regs)
{
    unsigned encodedRegs = 0;

    if (regs & RBM_EBX)
        encodedRegs |= 0x04;
    if (regs & RBM_ESI)
        encodedRegs |= 0x02;
    if (regs & RBM_EDI)
        encodedRegs |= 0x01;

    return encodedRegs;
}

/*****************************************************************************
 * Is the next entry for a byref pointer. If so, emit the prefix for the
 * interruptible encoding. Check only for pushes and registers
 */

inline BYTE* gceByrefPrefixI(GCInfo::regPtrDsc* rpd, BYTE* dest)
{
    // For registers, we don't need a prefix if it is going dead.
    assert(rpd->rpdArg || rpd->rpdCompiler.rpdDel == 0);

    if (!rpd->rpdArg || rpd->rpdArgType == GCInfo::rpdARG_PUSH)
        if (rpd->rpdGCtypeGet() == GCT_BYREF)
            *dest++ = 0xBF;

    return dest;
}

/*****************************************************************************/

/* These functions are needed to work around a VC5.0 compiler bug */
/* DO NOT REMOVE, unless you are sure that the free build works   */
static int zeroFN()
{
    return 0;
}
static int (*zeroFunc)() = zeroFN;

/*****************************************************************************
 *  Modelling of the GC ptrs pushed on the stack
 */

typedef unsigned pasMaskType;
#define BITS_IN_pasMask (BITS_PER_BYTE * sizeof(pasMaskType))
#define HIGHEST_pasMask_BIT (((pasMaskType)0x1) << (BITS_IN_pasMask - 1))

//-----------------------------------------------------------------------------

class PendingArgsStack
{
public:
    PendingArgsStack(unsigned maxDepth, Compiler* pComp);

    void pasPush(GCtype gcType);
    void pasPop(unsigned count);
    void pasKill(unsigned gcCount);

    unsigned pasCurDepth()
    {
        return pasDepth;
    }
    pasMaskType pasArgMask()
    {
        assert(pasDepth <= BITS_IN_pasMask);
        return pasBottomMask;
    }
    pasMaskType pasByrefArgMask()
    {
        assert(pasDepth <= BITS_IN_pasMask);
        return pasByrefBottomMask;
    }
    bool pasHasGCptrs();

    // Use these in the case where there actually are more ptrs than pasArgMask
    unsigned pasEnumGCoffsCount();
#define pasENUM_START ((unsigned)-1)
#define pasENUM_LAST ((unsigned)-2)
#define pasENUM_END ((unsigned)-3)
    unsigned pasEnumGCoffs(unsigned iter, unsigned* offs);

protected:
    unsigned pasMaxDepth;

    unsigned pasDepth;

    pasMaskType pasBottomMask;      // The first 32 args
    pasMaskType pasByrefBottomMask; // byref qualifier for pasBottomMask

    BYTE*    pasTopArray;       // More than 32 args are represented here
    unsigned pasPtrsInTopArray; // How many GCptrs here
};

//-----------------------------------------------------------------------------

PendingArgsStack::PendingArgsStack(unsigned maxDepth, Compiler* pComp)
    : pasMaxDepth(maxDepth)
    , pasDepth(0)
    , pasBottomMask(0)
    , pasByrefBottomMask(0)
    , pasTopArray(NULL)
    , pasPtrsInTopArray(0)
{
    /* Do we need an array as well as the mask ? */

    if (pasMaxDepth > BITS_IN_pasMask)
        pasTopArray = pComp->getAllocator(CMK_Unknown).allocate<BYTE>(pasMaxDepth - BITS_IN_pasMask);
}

//-----------------------------------------------------------------------------

void PendingArgsStack::pasPush(GCtype gcType)
{
    assert(pasDepth < pasMaxDepth);

    if (pasDepth < BITS_IN_pasMask)
    {
        /* Shift the mask */

        pasBottomMask <<= 1;
        pasByrefBottomMask <<= 1;

        if (needsGC(gcType))
        {
            pasBottomMask |= 1;

            if (gcType == GCT_BYREF)
                pasByrefBottomMask |= 1;
        }
    }
    else
    {
        /* Push on array */

        pasTopArray[pasDepth - BITS_IN_pasMask] = (BYTE)gcType;

        if (gcType)
            pasPtrsInTopArray++;
    }

    pasDepth++;
}

//-----------------------------------------------------------------------------

void PendingArgsStack::pasPop(unsigned count)
{
    assert(pasDepth >= count);

    /* First pop from array (if applicable) */

    for (/**/; (pasDepth > BITS_IN_pasMask) && count; pasDepth--, count--)
    {
        unsigned topIndex = pasDepth - BITS_IN_pasMask - 1;

        GCtype topArg = (GCtype)pasTopArray[topIndex];

        if (needsGC(topArg))
            pasPtrsInTopArray--;
    }
    if (count == 0)
        return;

    /* Now un-shift the mask */

    assert(pasPtrsInTopArray == 0);
    assert(count <= BITS_IN_pasMask);

    if (count == BITS_IN_pasMask) // (x>>32) is a nop on x86. So special-case it
    {
        pasBottomMask = pasByrefBottomMask = 0;
        pasDepth                           = 0;
    }
    else
    {
        pasBottomMask >>= count;
        pasByrefBottomMask >>= count;
        pasDepth -= count;
    }
}

//-----------------------------------------------------------------------------
// Kill (but don't pop) the top 'gcCount' args

void PendingArgsStack::pasKill(unsigned gcCount)
{
    assert(gcCount != 0);

    /* First kill args in array (if any) */

    for (unsigned curPos = pasDepth; (curPos > BITS_IN_pasMask) && gcCount; curPos--)
    {
        unsigned curIndex = curPos - BITS_IN_pasMask - 1;

        GCtype curArg = (GCtype)pasTopArray[curIndex];

        if (needsGC(curArg))
        {
            pasTopArray[curIndex] = GCT_NONE;
            pasPtrsInTopArray--;
            gcCount--;
        }
    }

    /* Now kill bits from the mask */

    assert(pasPtrsInTopArray == 0);
    assert(gcCount <= BITS_IN_pasMask);

    for (unsigned bitPos = 1; gcCount; bitPos <<= 1)
    {
        assert(pasBottomMask != 0);

        if (pasBottomMask & bitPos)
        {
            pasBottomMask &= ~bitPos;
            pasByrefBottomMask &= ~bitPos;
            --gcCount;
        }
        else
        {
            assert(bitPos != HIGHEST_pasMask_BIT);
        }
    }
}

//-----------------------------------------------------------------------------
// Used for the case where there are more than BITS_IN_pasMask args on stack,
// but none are any pointers. May avoid reporting anything to GCinfo

bool PendingArgsStack::pasHasGCptrs()
{
    if (pasDepth <= BITS_IN_pasMask)
        return pasBottomMask != 0;
    else
        return pasBottomMask != 0 || pasPtrsInTopArray != 0;
}

//-----------------------------------------------------------------------------
//  Iterates over mask and array to return total count.
//  Use only when you are going to emit a table of the offsets

unsigned PendingArgsStack::pasEnumGCoffsCount()
{
    /* Should only be used in the worst case, when just the mask can't be used */

    assert(pasDepth > BITS_IN_pasMask && pasHasGCptrs());

    /* Count number of set bits in mask */

    unsigned count = 0;

    for (pasMaskType mask = 0x1, i = 0; i < BITS_IN_pasMask; mask <<= 1, i++)
    {
        if (mask & pasBottomMask)
            count++;
    }

    return count + pasPtrsInTopArray;
}

//-----------------------------------------------------------------------------
//  Initialize enumeration by passing in iter=pasENUM_START.
//  Continue by passing in the return value as the new value of iter
//  End of enumeration when pasENUM_END is returned
//  If return value != pasENUM_END, *offs is set to the offset for GCinfo

unsigned PendingArgsStack::pasEnumGCoffs(unsigned iter, unsigned* offs)
{
    if (iter == pasENUM_LAST)
        return pasENUM_END;

    unsigned i = (iter == pasENUM_START) ? pasDepth : iter;

    for (/**/; i > BITS_IN_pasMask; i--)
    {
        GCtype curArg = (GCtype)pasTopArray[i - BITS_IN_pasMask - 1];
        if (needsGC(curArg))
        {
            unsigned offset;

            offset = (pasDepth - i) * TARGET_POINTER_SIZE;
            if (curArg == GCT_BYREF)
                offset |= byref_OFFSET_FLAG;

            *offs = offset;
            return i - 1;
        }
    }

    if (!pasBottomMask)
        return pasENUM_END;

    // Have we already processed some of the bits in pasBottomMask ?

    i = (iter == pasENUM_START || iter >= BITS_IN_pasMask) ? 0     // no
                                                           : iter; // yes

    for (pasMaskType mask = 0x1 << i; mask; i++, mask <<= 1)
    {
        if (mask & pasBottomMask)
        {
            unsigned lvl = (pasDepth > BITS_IN_pasMask) ? (pasDepth - BITS_IN_pasMask) : 0; // How many in pasTopArray[]
            lvl += i;

            unsigned offset;
            offset = lvl * TARGET_POINTER_SIZE;
            if (mask & pasByrefBottomMask)
                offset |= byref_OFFSET_FLAG;

            *offs = offset;

            unsigned remMask = -int(mask << 1);
            return ((pasBottomMask & remMask) ? (i + 1) : pasENUM_LAST);
        }
    }

    assert(!"Shouldnt reach here");
    return pasENUM_END;
}

/*****************************************************************************
 *
 *  Generate the register pointer map, and return its total size in bytes. If
 *  'mask' is 0, we don't actually store any data in 'dest' (except for one
 *  entry, which is never more than 10 bytes), so this can be used to merely
 *  compute the size of the table.
 */

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable : 21000) // Suppress PREFast warning about overly large function
#endif
size_t GCInfo::gcMakeRegPtrTable(BYTE* dest, int mask, const InfoHdr& header, unsigned codeSize, size_t* pArgTabOffset)
{
    unsigned   varNum;
    LclVarDsc* varDsc;

    size_t   totalSize = 0;
    unsigned lastOffset;

    /* The mask should be all 0's or all 1's */

    assert(mask == 0 || mask == -1);

    /* Start computing the total size of the table */

    bool emitArgTabOffset = (header.varPtrTableSize != 0 || header.untrackedCnt > SET_UNTRACKED_MAX);
    if (mask != 0 && emitArgTabOffset)
    {
        assert(*pArgTabOffset <= MAX_UNSIGNED_SIZE_T);
        unsigned sz = encodeUnsigned(dest, static_cast<unsigned>(*pArgTabOffset));
        dest += sz;
        totalSize += sz;
    }

#if VERIFY_GC_TABLES
    if (mask)
    {
        *(short*)dest = (short)0xBEEF;
        dest += sizeof(short);
    }
    totalSize += sizeof(short);
#endif

/**************************************************************************
 *
 *                      Untracked ptr variables
 *
 **************************************************************************
 */
#if DEBUG
    unsigned untrackedCount  = 0;
    unsigned varPtrTableSize = 0;
    gcCountForHeader(&untrackedCount, &varPtrTableSize);
    assert(untrackedCount == header.untrackedCnt);
    assert(varPtrTableSize == header.varPtrTableSize);
#endif // DEBUG

    if (header.untrackedCnt != 0)
    {
        // Write the table of untracked pointer variables.

        int lastoffset = 0;

        for (varNum = 0, varDsc = compiler->lvaTable; varNum < compiler->lvaCount; varNum++, varDsc++)
        {
            if (compiler->lvaIsFieldOfDependentlyPromotedStruct(varDsc))
            {
                // Field local of a PROMOTION_TYPE_DEPENDENT struct must have been
                // reported through its parent local
                continue;
            }

            if (varTypeIsGC(varDsc->TypeGet()))
            {
                if (!gcIsUntrackedLocalOrNonEnregisteredArg(varNum))
                {
                    continue;
                }

                int offset = varDsc->GetStackOffset();
#if DOUBLE_ALIGN
                // For genDoubleAlign(), locals are addressed relative to ESP and
                // arguments are addressed relative to EBP.

                if (compiler->genDoubleAlign() && varDsc->lvIsParam && !varDsc->lvIsRegArg)
                    offset += compiler->codeGen->genTotalFrameSize();
#endif

                // The lower bits of the offset encode properties of the stk ptr

                assert(~OFFSET_MASK % sizeof(offset) == 0);

                if (varDsc->TypeGet() == TYP_BYREF)
                {
                    // Or in byref_OFFSET_FLAG for 'byref' pointer tracking
                    offset |= byref_OFFSET_FLAG;
                }

                if (varDsc->lvPinned)
                {
                    // Or in pinned_OFFSET_FLAG for 'pinned' pointer tracking
                    offset |= pinned_OFFSET_FLAG;
                }

                int encodedoffset = lastoffset - offset;
                lastoffset        = offset;

                if (mask == 0)
                    totalSize += encodeSigned(NULL, encodedoffset);
                else
                {
                    unsigned sz = encodeSigned(dest, encodedoffset);
                    dest += sz;
                    totalSize += sz;
                }
            }
            else if ((varDsc->TypeGet() == TYP_STRUCT) && varDsc->lvOnFrame && varDsc->HasGCPtr())
            {
                ClassLayout* layout = varDsc->GetLayout();
                unsigned     slots  = layout->GetSlotCount();

                for (unsigned i = 0; i < slots; i++)
                {
                    if (!layout->IsGCPtr(i))
                    {
                        continue;
                    }

                    unsigned offset = varDsc->GetStackOffset() + i * TARGET_POINTER_SIZE;
#if DOUBLE_ALIGN
                    // For genDoubleAlign(), locals are addressed relative to ESP and
                    // arguments are addressed relative to EBP.

                    if (compiler->genDoubleAlign() && varDsc->lvIsParam && !varDsc->lvIsRegArg)
                    {
                        offset += compiler->codeGen->genTotalFrameSize();
                    }
#endif
                    if (layout->GetGCPtrType(i) == TYP_BYREF)
                    {
                        offset |= byref_OFFSET_FLAG; // indicate it is a byref GC pointer
                    }

                    int encodedoffset = lastoffset - offset;
                    lastoffset        = offset;

                    if (mask == 0)
                    {
                        totalSize += encodeSigned(NULL, encodedoffset);
                    }
                    else
                    {
                        unsigned sz = encodeSigned(dest, encodedoffset);
                        dest += sz;
                        totalSize += sz;
                    }
                }
            }
        }

        /* Count&Write spill temps that hold pointers */

        assert(compiler->codeGen->regSet.tmpAllFree());
        for (TempDsc* tempItem = compiler->codeGen->regSet.tmpListBeg(); tempItem != nullptr;
             tempItem          = compiler->codeGen->regSet.tmpListNxt(tempItem))
        {
            if (varTypeIsGC(tempItem->tdTempType()))
            {
                {
                    int offset;

                    offset = tempItem->tdTempOffs();

                    if (tempItem->tdTempType() == TYP_BYREF)
                    {
                        offset |= byref_OFFSET_FLAG;
                    }

                    int encodedoffset = lastoffset - offset;
                    lastoffset        = offset;

                    if (mask == 0)
                    {
                        totalSize += encodeSigned(NULL, encodedoffset);
                    }
                    else
                    {
                        unsigned sz = encodeSigned(dest, encodedoffset);
                        dest += sz;
                        totalSize += sz;
                    }
                }
            }
        }
    }

#if VERIFY_GC_TABLES
    if (mask)
    {
        *(short*)dest = (short)0xCAFE;
        dest += sizeof(short);
    }
    totalSize += sizeof(short);
#endif

    /**************************************************************************
     *
     *  Generate the table of stack pointer variable lifetimes.
     *
     **************************************************************************
     */

    bool keepThisAlive = false;

    if (!compiler->info.compIsStatic)
    {
        unsigned thisArgNum = compiler->info.compThisArg;
        gcIsUntrackedLocalOrNonEnregisteredArg(thisArgNum, &keepThisAlive);
    }

    // First we check for the most common case - no lifetimes at all.

    if (header.varPtrTableSize != 0)
    {
#if !defined(FEATURE_EH_FUNCLETS)
        if (keepThisAlive)
        {
            // Encoding of untracked variables does not support reporting
            // "this". So report it as a tracked variable with a liveness
            // extending over the entire method.

            assert(compiler->lvaTable[compiler->info.compThisArg].TypeGet() == TYP_REF);

            unsigned varOffs = compiler->lvaTable[compiler->info.compThisArg].GetStackOffset();

            /* For negative stack offsets we must reset the low bits,
                * take abs and then set them back */

            varOffs = abs(static_cast<int>(varOffs));
            varOffs |= this_OFFSET_FLAG;

            size_t sz = 0;
            sz        = encodeUnsigned(mask ? (dest + sz) : NULL, varOffs);
            sz += encodeUDelta(mask ? (dest + sz) : NULL, 0, 0);
            sz += encodeUDelta(mask ? (dest + sz) : NULL, codeSize, 0);

            dest += (sz & mask);
            totalSize += sz;
        }
#endif // !FEATURE_EH_FUNCLETS

        /* We'll use a delta encoding for the lifetime offsets */

        lastOffset = 0;

        for (varPtrDsc* varTmp = gcVarPtrList; varTmp; varTmp = varTmp->vpdNext)
        {
            unsigned varOffs;
            unsigned lowBits;

            unsigned begOffs;
            unsigned endOffs;

            assert(~OFFSET_MASK % TARGET_POINTER_SIZE == 0);

            /* Get hold of the variable's stack offset */

            lowBits = varTmp->vpdVarNum & OFFSET_MASK;

            /* For negative stack offsets we must reset the low bits,
             * take abs and then set them back */

            varOffs = abs(static_cast<int>(varTmp->vpdVarNum & ~OFFSET_MASK));
            varOffs |= lowBits;

            /* Compute the actual lifetime offsets */

            begOffs = varTmp->vpdBegOfs;
            endOffs = varTmp->vpdEndOfs;

            /* Special case: skip any 0-length lifetimes */

            if (endOffs == begOffs)
                continue;

            /* Are we counting or generating? */

            size_t sz = 0;
            sz        = encodeUnsigned(mask ? (dest + sz) : NULL, varOffs);
            sz += encodeUDelta(mask ? (dest + sz) : NULL, begOffs, lastOffset);
            sz += encodeUDelta(mask ? (dest + sz) : NULL, endOffs, begOffs);

            dest += (sz & mask);
            totalSize += sz;

            /* The next entry will be relative to the one we just processed */

            lastOffset = begOffs;
        }
    }

    if (pArgTabOffset != NULL)
        *pArgTabOffset = totalSize;

#if VERIFY_GC_TABLES
    if (mask)
    {
        *(short*)dest = (short)0xBABE;
        dest += sizeof(short);
    }
    totalSize += sizeof(short);
#endif

    if (!mask && emitArgTabOffset)
    {
        assert(*pArgTabOffset <= MAX_UNSIGNED_SIZE_T);
        totalSize += encodeUnsigned(NULL, static_cast<unsigned>(*pArgTabOffset));
    }

    /**************************************************************************
     *
     * Prepare to generate the pointer register/argument map
     *
     **************************************************************************
     */

    lastOffset = 0;

    if (compiler->codeGen->GetInterruptible())
    {
#ifdef TARGET_X86
        assert(compiler->IsFullPtrRegMapRequired());

        unsigned ptrRegs = 0;

        regPtrDsc* genRegPtrTemp;

        /* Walk the list of pointer register/argument entries */

        for (genRegPtrTemp = gcRegPtrList; genRegPtrTemp; genRegPtrTemp = genRegPtrTemp->rpdNext)
        {
            BYTE* base = dest;

            unsigned nextOffset;
            DWORD    codeDelta;

            nextOffset = genRegPtrTemp->rpdOffs;

            /*
                Encoding table for methods that are fully interruptible

                The encoding used is as follows:

                ptr reg dead    00RRRDDD    [RRR != 100]
                ptr reg live    01RRRDDD    [RRR != 100]

            non-ptr arg push    10110DDD                    [SSS == 110]
                ptr arg push    10SSSDDD                    [SSS != 110] && [SSS != 111]
                ptr arg pop     11CCCDDD    [CCC != 000] && [CCC != 110] && [CCC != 111]
                little skip     11000DDD    [CCC == 000]
                bigger skip     11110BBB                    [CCC == 110]

                The values used in the above encodings are as follows:

                  DDD                 code offset delta from previous entry (0-7)
                  BBB                 bigger delta 000=8,001=16,010=24,...,111=64
                  RRR                 register number (EAX=000,ECX=001,EDX=010,EBX=011,
                                        EBP=101,ESI=110,EDI=111), ESP=100 is reserved
                  SSS                 argument offset from base of stack. This is
                                        redundant for frameless methods as we can
                                        infer it from the previous pushes+pops. However,
                                        for EBP-methods, we only report GC pushes, and
                                        so we need SSS
                  CCC                 argument count being popped (includes only ptrs for EBP methods)

                The following are the 'large' versions:

                  large delta skip        10111000 [0xB8] , encodeUnsigned(delta)

                  large     ptr arg push  11111000 [0xF8] , encodeUnsigned(pushCount)
                  large non-ptr arg push  11111001 [0xF9] , encodeUnsigned(pushCount)
                  large     ptr arg pop   11111100 [0xFC] , encodeUnsigned(popCount)
                  large         arg dead  11111101 [0xFD] , encodeUnsigned(popCount) for caller-pop args.
                                                              Any GC args go dead after the call,
                                                              but are still sitting on the stack

                  this pointer prefix     10111100 [0xBC]   the next encoding is a ptr live
                                                              or a ptr arg push
                                                              and contains the this pointer

                  interior or by-ref      10111111 [0xBF]   the next encoding is a ptr live
                       pointer prefix                         or a ptr arg push
                                                              and contains an interior
                                                              or by-ref pointer


                  The value 11111111 [0xFF] indicates the end of the table.
            */

            codeDelta = nextOffset - lastOffset;
            assert((int)codeDelta >= 0);

            // If the code delta is between 8 and (64+7),
            // generate a 'bigger delta' encoding

            if ((codeDelta >= 8) && (codeDelta <= (64 + 7)))
            {
                unsigned biggerDelta = ((codeDelta - 8) & 0x38) + 8;
                *dest++              = (BYTE)(0xF0 | ((biggerDelta - 8) >> 3));
                lastOffset += biggerDelta;
                codeDelta &= 0x07;
            }

            // If the code delta is still bigger than 7,
            // generate a 'large code delta' encoding

            if (codeDelta > 7)
            {
                *dest++ = 0xB8;
                dest += encodeUnsigned(dest, codeDelta);
                codeDelta = 0;

                /* Remember the new 'last' offset */

                lastOffset = nextOffset;
            }

            /* Is this a pointer argument or register entry? */

            if (genRegPtrTemp->rpdArg)
            {
                if (genRegPtrTemp->rpdArgTypeGet() == rpdARG_KILL)
                {
                    if (codeDelta)
                    {
                        /*
                            Use the small encoding:
                            little delta skip       11000DDD    [0xC0]
                         */

                        assert((codeDelta & 0x7) == codeDelta);
                        *dest++ = 0xC0 | (BYTE)codeDelta;

                        /* Remember the new 'last' offset */

                        lastOffset = nextOffset;
                    }

                    /* Caller-pop arguments are dead after call but are still
                       sitting on the stack */

                    *dest++ = 0xFD;
                    assert(genRegPtrTemp->rpdPtrArg != 0);
                    dest += encodeUnsigned(dest, genRegPtrTemp->rpdPtrArg);
                }
                else if (genRegPtrTemp->rpdPtrArg < 6 && genRegPtrTemp->rpdGCtypeGet())
                {
                    /* Is the argument offset/count smaller than 6 ? */

                    dest = gceByrefPrefixI(genRegPtrTemp, dest);

                    if (genRegPtrTemp->rpdArgTypeGet() == rpdARG_PUSH || (genRegPtrTemp->rpdPtrArg != 0))
                    {
                        /*
                          Use the small encoding:

                            ptr arg push 10SSSDDD [SSS != 110] && [SSS != 111]
                            ptr arg pop  11CCCDDD [CCC != 110] && [CCC != 111]
                         */

                        bool isPop = genRegPtrTemp->rpdArgTypeGet() == rpdARG_POP;

                        *dest++ = (BYTE)(0x80 | (BYTE)codeDelta | genRegPtrTemp->rpdPtrArg << 3 | isPop << 6);

                        /* Remember the new 'last' offset */

                        lastOffset = nextOffset;
                    }
                    else
                    {
                        assert(!"Check this");
                    }
                }
                else if (genRegPtrTemp->rpdGCtypeGet() == GCT_NONE)
                {
                    /*
                        Use the small encoding:
`                        non-ptr arg push 10110DDD [0xB0] (push of sizeof(int))
                     */

                    assert((codeDelta & 0x7) == codeDelta);
                    *dest++ = 0xB0 | (BYTE)codeDelta;
#ifndef UNIX_X86_ABI
                    assert(!compiler->isFramePointerUsed());
#endif

                    /* Remember the new 'last' offset */

                    lastOffset = nextOffset;
                }
                else
                {
                    /* Will have to use large encoding;
                     *   first do the code delta
                     */

                    if (codeDelta)
                    {
                        /*
                            Use the small encoding:
                            little delta skip       11000DDD    [0xC0]
                         */

                        assert((codeDelta & 0x7) == codeDelta);
                        *dest++ = 0xC0 | (BYTE)codeDelta;
                    }

                    /*
                        Now append a large argument record:

                            large ptr arg push  11111000 [0xF8]
                            large ptr arg pop   11111100 [0xFC]
                     */

                    bool isPop = genRegPtrTemp->rpdArgTypeGet() == rpdARG_POP;

                    dest = gceByrefPrefixI(genRegPtrTemp, dest);

                    *dest++ = 0xF8 | (isPop << 2);
                    dest += encodeUnsigned(dest, genRegPtrTemp->rpdPtrArg);

                    /* Remember the new 'last' offset */

                    lastOffset = nextOffset;
                }
            }
            else
            {
                unsigned regMask;

                /* Record any registers that are becoming dead */

                regMask = genRegPtrTemp->rpdCompiler.rpdDel & ptrRegs;

                while (regMask) // EAX,ECX,EDX,EBX,---,EBP,ESI,EDI
                {
                    unsigned  tmpMask;
                    regNumber regNum;

                    /* Get hold of the next register bit */

                    tmpMask = genFindLowestBit(regMask);
                    assert(tmpMask);

                    /* Remember the new state of this register */

                    ptrRegs &= ~tmpMask;

                    /* Figure out which register the next bit corresponds to */

                    regNum = genRegNumFromMask(tmpMask);
                    assert(regNum <= 7);

                    /* Reserve ESP, regNum==4 for future use */

                    assert(regNum != 4);

                    /*
                        Generate a small encoding:

                            ptr reg dead        00RRRDDD
                     */

                    assert((codeDelta & 0x7) == codeDelta);
                    *dest++ = (BYTE)(0x00 | regNum << 3 | (BYTE)codeDelta);

                    /* Turn the bit we've just generated off and continue */

                    regMask -= tmpMask; // EAX,ECX,EDX,EBX,---,EBP,ESI,EDI

                    /* Remember the new 'last' offset */

                    lastOffset = nextOffset;

                    /* Any entries that follow will be at the same offset */

                    codeDelta = zeroFunc(); /* DO NOT REMOVE */
                }

                /* Record any registers that are becoming live */

                regMask = genRegPtrTemp->rpdCompiler.rpdAdd & ~ptrRegs;

                while (regMask) // EAX,ECX,EDX,EBX,---,EBP,ESI,EDI
                {
                    unsigned  tmpMask;
                    regNumber regNum;

                    /* Get hold of the next register bit */

                    tmpMask = genFindLowestBit(regMask);
                    assert(tmpMask);

                    /* Remember the new state of this register */

                    ptrRegs |= tmpMask;

                    /* Figure out which register the next bit corresponds to */

                    regNum = genRegNumFromMask(tmpMask);
                    assert(regNum <= 7);

                    /*
                        Generate a small encoding:

                            ptr reg live        01RRRDDD
                     */

                    dest = gceByrefPrefixI(genRegPtrTemp, dest);

                    if (!keepThisAlive && genRegPtrTemp->rpdIsThis)
                    {
                        // Mark with 'this' pointer prefix
                        *dest++ = 0xBC;
                        // Can only have one bit set in regMask
                        assert(regMask == tmpMask);
                    }

                    assert((codeDelta & 0x7) == codeDelta);
                    *dest++ = (BYTE)(0x40 | (regNum << 3) | (BYTE)codeDelta);

                    /* Turn the bit we've just generated off and continue */

                    regMask -= tmpMask; // EAX,ECX,EDX,EBX,---,EBP,ESI,EDI

                    /* Remember the new 'last' offset */

                    lastOffset = nextOffset;

                    /* Any entries that follow will be at the same offset */

                    codeDelta = zeroFunc(); /* DO NOT REMOVE */
                }
            }

            /* Keep track of the total amount of generated stuff */

            totalSize += dest - base;

            /* Go back to the buffer start if we're not generating a table */

            if (!mask)
                dest = base;
        }
#endif // TARGET_X86

        /* Terminate the table with 0xFF */

        *dest = 0xFF;
        dest -= mask;
        totalSize++;
    }
    else if (compiler->isFramePointerUsed()) // GetInterruptible() is false
    {
#ifdef TARGET_X86
        /*
            Encoding table for methods with an EBP frame and
                               that are not fully interruptible

            The encoding used is as follows:

            this pointer encodings:

               01000000          this pointer in EBX
               00100000          this pointer in ESI
               00010000          this pointer in EDI

            tiny encoding:

               0bsdDDDD
                                 requires code delta > 0 & delta < 16 (4-bits)
                                 requires pushed argmask == 0

                 where    DDDD   is code delta
                             b   indicates that register EBX is a live pointer
                             s   indicates that register ESI is a live pointer
                             d   indicates that register EDI is a live pointer


            small encoding:

               1DDDDDDD bsdAAAAA

                                 requires code delta     < 120 (7-bits)
                                 requires pushed argmask <  64 (5-bits)

                 where DDDDDDD   is code delta
                         AAAAA   is the pushed args mask
                             b   indicates that register EBX is a live pointer
                             s   indicates that register ESI is a live pointer
                             d   indicates that register EDI is a live pointer

            medium encoding

               0xFD aaaaaaaa AAAAdddd bseDDDDD

                                 requires code delta     <  512  (9-bits)
                                 requires pushed argmask < 2048 (12-bits)

                 where    DDDDD  is the upper 5-bits of the code delta
                           dddd  is the low   4-bits of the code delta
                           AAAA  is the upper 4-bits of the pushed arg mask
                       aaaaaaaa  is the low   8-bits of the pushed arg mask
                              b  indicates that register EBX is a live pointer
                              s  indicates that register ESI is a live pointer
                              e  indicates that register EDI is a live pointer

            medium encoding with interior pointers

               0xF9 DDDDDDDD bsdAAAAAA iiiIIIII

                                 requires code delta     < 256 (8-bits)
                                 requires pushed argmask <  64 (5-bits)

                 where  DDDDDDD  is the code delta
                              b  indicates that register EBX is a live pointer
                              s  indicates that register ESI is a live pointer
                              d  indicates that register EDI is a live pointer
                          AAAAA  is the pushed arg mask
                            iii  indicates that EBX,EDI,ESI are interior pointers
                          IIIII  indicates that bits in the arg mask are interior
                                 pointers

            large encoding

               0xFE [0BSD0bsd][32-bit code delta][32-bit argMask]

                              b  indicates that register EBX is a live pointer
                              s  indicates that register ESI is a live pointer
                              d  indicates that register EDI is a live pointer
                              B  indicates that register EBX is an interior pointer
                              S  indicates that register ESI is an interior pointer
                              D  indicates that register EDI is an interior pointer
                                 requires pushed  argmask < 32-bits

            large encoding  with interior pointers

               0xFA [0BSD0bsd][32-bit code delta][32-bit argMask][32-bit interior pointer mask]


                              b  indicates that register EBX is a live pointer
                              s  indicates that register ESI is a live pointer
                              d  indicates that register EDI is a live pointer
                              B  indicates that register EBX is an interior pointer
                              S  indicates that register ESI is an interior pointer
                              D  indicates that register EDI is an interior pointer
                                 requires pushed  argmask < 32-bits
                                 requires pushed iArgmask < 32-bits


            huge encoding        This is the only encoding that supports
                                 a pushed argmask which is greater than
                                 32-bits.

               0xFB [0BSD0bsd][32-bit code delta]
                    [32-bit table count][32-bit table size]
                    [pushed ptr offsets table...]

                             b   indicates that register EBX is a live pointer
                             s   indicates that register ESI is a live pointer
                             d   indicates that register EDI is a live pointer
                             B   indicates that register EBX is an interior pointer
                             S   indicates that register ESI is an interior pointer
                             D   indicates that register EDI is an interior pointer
                             the list count is the number of entries in the list
                             the list size gives the byte-length of the list
                             the offsets in the list are variable-length
        */

        /* If "this" is enregistered, note it. We do this explicitly here as
           IsFullPtrRegMapRequired()==false, and so we don't have any regPtrDsc's. */

        if (compiler->lvaKeepAliveAndReportThis() && compiler->lvaTable[compiler->info.compThisArg].lvRegister)
        {
            unsigned thisRegMask   = genRegMask(compiler->lvaTable[compiler->info.compThisArg].GetRegNum());
            unsigned thisPtrRegEnc = gceEncodeCalleeSavedRegs(thisRegMask) << 4;

            if (thisPtrRegEnc)
            {
                totalSize += 1;
                if (mask)
                    *dest++ = (BYTE)thisPtrRegEnc;
            }
        }

        CallDsc* call;

        assert(compiler->IsFullPtrRegMapRequired() == false);

        /* Walk the list of pointer register/argument entries */

        for (call = gcCallDescList; call; call = call->cdNext)
        {
            BYTE*    base = dest;
            unsigned nextOffset;

            /* Figure out the code offset of this entry */

            nextOffset = call->cdOffs;

            /* Compute the distance from the previous call */

            DWORD codeDelta = nextOffset - lastOffset;

            assert((int)codeDelta >= 0);

            /* Remember the new 'last' offset */

            lastOffset = nextOffset;

            /* Compute the register mask */

            unsigned gcrefRegMask = 0;
            unsigned byrefRegMask = 0;

            gcrefRegMask |= gceEncodeCalleeSavedRegs(call->cdGCrefRegs);
            byrefRegMask |= gceEncodeCalleeSavedRegs(call->cdByrefRegs);

            assert((gcrefRegMask & byrefRegMask) == 0);

            unsigned regMask = gcrefRegMask | byrefRegMask;

            bool byref = (byrefRegMask | call->u1.cdByrefArgMask) != 0;

            /* Check for the really large argument offset case */
            /* The very rare Huge encodings */

            if (call->cdArgCnt)
            {
                unsigned argNum;
                DWORD    argCnt    = call->cdArgCnt;
                DWORD    argBytes  = 0;
                BYTE*    pArgBytes = DUMMY_INIT(NULL);

                if (mask != 0)
                {
                    *dest++       = 0xFB;
                    *dest++       = (BYTE)((byrefRegMask << 4) | regMask);
                    *(DWORD*)dest = codeDelta;
                    dest += sizeof(DWORD);
                    *(DWORD*)dest = argCnt;
                    dest += sizeof(DWORD);
                    // skip the byte-size for now. Just note where it will go
                    pArgBytes = dest;
                    dest += sizeof(DWORD);
                }

                for (argNum = 0; argNum < argCnt; argNum++)
                {
                    unsigned eltSize;
                    eltSize = encodeUnsigned(dest, call->cdArgTable[argNum]);
                    argBytes += eltSize;
                    if (mask)
                        dest += eltSize;
                }

                if (mask == 0)
                {
                    dest = base + 1 + 1 + 3 * sizeof(DWORD) + argBytes;
                }
                else
                {
                    assert(dest == pArgBytes + sizeof(argBytes) + argBytes);
                    *(DWORD*)pArgBytes = argBytes;
                }
            }

            /* Check if we can use a tiny encoding */
            else if ((codeDelta < 16) && (codeDelta != 0) && (call->u1.cdArgMask == 0) && !byref)
            {
                *dest++ = (BYTE)((regMask << 4) | (BYTE)codeDelta);
            }

            /* Check if we can use the small encoding */
            else if ((codeDelta < 0x79) && (call->u1.cdArgMask <= 0x1F) && !byref)
            {
                *dest++ = 0x80 | (BYTE)codeDelta;
                *dest++ = (BYTE)(call->u1.cdArgMask | (regMask << 5));
            }

            /* Check if we can use the medium encoding */
            else if (codeDelta <= 0x01FF && call->u1.cdArgMask <= 0x0FFF && !byref)
            {
                *dest++ = 0xFD;
                *dest++ = (BYTE)call->u1.cdArgMask;
                *dest++ = ((call->u1.cdArgMask >> 4) & 0xF0) | ((BYTE)codeDelta & 0x0F);
                *dest++ = (BYTE)(regMask << 5) | (BYTE)((codeDelta >> 4) & 0x1F);
            }

            /* Check if we can use the medium encoding with byrefs */
            else if (codeDelta <= 0x0FF && call->u1.cdArgMask <= 0x01F)
            {
                *dest++ = 0xF9;
                *dest++ = (BYTE)codeDelta;
                *dest++ = (BYTE)((regMask << 5) | call->u1.cdArgMask);
                *dest++ = (BYTE)((byrefRegMask << 5) | call->u1.cdByrefArgMask);
            }

            /* We'll use the large encoding */
            else if (!byref)
            {
                *dest++       = 0xFE;
                *dest++       = (BYTE)((byrefRegMask << 4) | regMask);
                *(DWORD*)dest = codeDelta;
                dest += sizeof(DWORD);
                *(DWORD*)dest = call->u1.cdArgMask;
                dest += sizeof(DWORD);
            }

            /* We'll use the large encoding with byrefs */
            else
            {
                *dest++       = 0xFA;
                *dest++       = (BYTE)((byrefRegMask << 4) | regMask);
                *(DWORD*)dest = codeDelta;
                dest += sizeof(DWORD);
                *(DWORD*)dest = call->u1.cdArgMask;
                dest += sizeof(DWORD);
                *(DWORD*)dest = call->u1.cdByrefArgMask;
                dest += sizeof(DWORD);
            }

            /* Keep track of the total amount of generated stuff */

            totalSize += dest - base;

            /* Go back to the buffer start if we're not generating a table */

            if (!mask)
                dest = base;
        }
#endif // TARGET_X86

        /* Terminate the table with 0xFF */

        *dest = 0xFF;
        dest -= mask;
        totalSize++;
    }
    else // GetInterruptible() is false and we have an EBP-less frame
    {
        assert(compiler->IsFullPtrRegMapRequired());

#ifdef TARGET_X86

        regPtrDsc*       genRegPtrTemp;
        regNumber        thisRegNum = regNumber(0);
        PendingArgsStack pasStk(compiler->GetEmitter()->emitMaxStackDepth, compiler);

        /* Walk the list of pointer register/argument entries */

        for (genRegPtrTemp = gcRegPtrList; genRegPtrTemp; genRegPtrTemp = genRegPtrTemp->rpdNext)
        {

            /*
             *    Encoding table for methods without an EBP frame and
             *     that are not fully interruptible
             *
             *               The encoding used is as follows:
             *
             *  push     000DDDDD                     ESP push one item with 5-bit delta
             *  push     00100000 [pushCount]         ESP push multiple items
             *  reserved 0010xxxx                     xxxx != 0000
             *  reserved 0011xxxx
             *  skip     01000000 [Delta]             Skip Delta, arbitrary sized delta
             *  skip     0100DDDD                     Skip small Delta, for call (DDDD != 0)
             *  pop      01CCDDDD                     ESP pop  CC items with 4-bit delta (CC != 00)
             *  call     1PPPPPPP                     Call Pattern, P=[0..79]
             *  call     1101pbsd DDCCCMMM            Call RegMask=pbsd,ArgCnt=CCC,
             *                                        ArgMask=MMM Delta=commonDelta[DD]
             *  call     1110pbsd [ArgCnt] [ArgMask]  Call ArgCnt,RegMask=pbsd,ArgMask
             *  call     11111000 [PBSDpbsd][32-bit delta][32-bit ArgCnt]
             *                    [32-bit PndCnt][32-bit PndSize][PndOffs...]
             *  iptr     11110000 [IPtrMask]          Arbitrary Interior Pointer Mask
             *  thisptr  111101RR                     This pointer is in Register RR
             *                                        00=EDI,01=ESI,10=EBX,11=EBP
             *  reserved 111100xx                     xx  != 00
             *  reserved 111110xx                     xx  != 00
             *  reserved 11111xxx                     xxx != 000 && xxx != 111(EOT)
             *
             *   The value 11111111 [0xFF] indicates the end of the table. (EOT)
             *
             *  An offset (at which stack-walking is performed) without an explicit encoding
             *  is assumed to be a trivial call-site (no GC registers, stack empty before and
             *  after) to avoid having to encode all trivial calls.
             *
             * Note on the encoding used for interior pointers
             *
             *   The iptr encoding must immediately precede a call encoding.  It is used
             *   to transform a normal GC pointer addresses into an interior pointers for
             *   GC purposes.  The mask supplied to the iptr encoding is read from the
             *   least signicant bit to the most signicant bit. (i.e the lowest bit is
             *   read first)
             *
             *   p   indicates that register EBP is a live pointer
             *   b   indicates that register EBX is a live pointer
             *   s   indicates that register ESI is a live pointer
             *   d   indicates that register EDI is a live pointer
             *   P   indicates that register EBP is an interior pointer
             *   B   indicates that register EBX is an interior pointer
             *   S   indicates that register ESI is an interior pointer
             *   D   indicates that register EDI is an interior pointer
             *
             *   As an example the following sequence indicates that EDI.ESI and the
             *   second pushed pointer in ArgMask are really interior pointers.  The
             *   pointer in ESI in a normal pointer:
             *
             *   iptr 11110000 00010011           => read Interior Ptr, Interior Ptr,
             *                                       Normal Ptr, Normal Ptr, Interior Ptr
             *
             *   call 11010011 DDCCC011 RRRR=1011 => read EDI is a GC-pointer,
             *                                            ESI is a GC-pointer.
             *                                            EBP is a GC-pointer
             *                           MMM=0011 => read two GC-pointers arguments
             *                                         on the stack (nested call)
             *
             *   Since the call instruction mentions 5 GC-pointers we list them in
             *   the required order:  EDI, ESI, EBP, 1st-pushed pointer, 2nd-pushed pointer
             *
             *   And we apply the Interior Pointer mask mmmm=10011 to the five GC-pointers
             *   we learn that EDI and ESI are interior GC-pointers and that
             *   the second push arg is an interior GC-pointer.
             */

            BYTE* base = dest;

            bool     usePopEncoding;
            unsigned regMask;
            unsigned argMask;
            unsigned byrefRegMask;
            unsigned byrefArgMask;
            DWORD    callArgCnt;

            unsigned nextOffset;
            DWORD    codeDelta;

            nextOffset = genRegPtrTemp->rpdOffs;

            /* Compute the distance from the previous call */

            codeDelta = nextOffset - lastOffset;
            assert((int)codeDelta >= 0);

#if REGEN_CALLPAT
            // Must initialize this flag to true when REGEN_CALLPAT is on
            usePopEncoding         = true;
            unsigned origCodeDelta = codeDelta;
#endif

            if (!keepThisAlive && genRegPtrTemp->rpdIsThis)
            {
                unsigned tmpMask = genRegPtrTemp->rpdCompiler.rpdAdd;

                /* tmpMask must have exactly one bit set */

                assert(tmpMask && ((tmpMask & (tmpMask - 1)) == 0));

                thisRegNum = genRegNumFromMask(tmpMask);
                switch (thisRegNum)
                {
                    case 0: // EAX
                    case 1: // ECX
                    case 2: // EDX
                    case 4: // ESP
                        break;
                    case 7:             // EDI
                        *dest++ = 0xF4; /* 11110100  This pointer is in EDI */
                        break;
                    case 6:             // ESI
                        *dest++ = 0xF5; /* 11110100  This pointer is in ESI */
                        break;
                    case 3:             // EBX
                        *dest++ = 0xF6; /* 11110100  This pointer is in EBX */
                        break;
                    case 5:             // EBP
                        *dest++ = 0xF7; /* 11110100  This pointer is in EBP */
                        break;
                    default:
                        break;
                }
            }

            /* Is this a stack pointer change or call? */

            if (genRegPtrTemp->rpdArg)
            {
                if (genRegPtrTemp->rpdArgTypeGet() == rpdARG_KILL)
                {
                    // kill 'rpdPtrArg' number of pointer variables in pasStk
                    pasStk.pasKill(genRegPtrTemp->rpdPtrArg);
                }
                /* Is this a call site? */
                else if (genRegPtrTemp->rpdCall)
                {
                    /* This is a true call site */

                    /* Remember the new 'last' offset */

                    lastOffset = nextOffset;

                    callArgCnt = genRegPtrTemp->rpdPtrArg;

                    unsigned gcrefRegMask = genRegPtrTemp->rpdCallGCrefRegs;

                    byrefRegMask = genRegPtrTemp->rpdCallByrefRegs;

                    assert((gcrefRegMask & byrefRegMask) == 0);

                    regMask = gcrefRegMask | byrefRegMask;

                    /* adjust argMask for this call-site */
                    pasStk.pasPop(callArgCnt);

                    /* Do we have to use the fat encoding */

                    if (pasStk.pasCurDepth() > BITS_IN_pasMask && pasStk.pasHasGCptrs())
                    {
                        /* use fat encoding:
                         *   11111000 [PBSDpbsd][32-bit delta][32-bit ArgCnt]
                         *            [32-bit PndCnt][32-bit PndSize][PndOffs...]
                         */

                        DWORD pndCount = pasStk.pasEnumGCoffsCount();
                        DWORD pndSize  = 0;
                        BYTE* pPndSize = DUMMY_INIT(NULL);

                        if (mask)
                        {
                            *dest++       = 0xF8;
                            *dest++       = (BYTE)((byrefRegMask << 4) | regMask);
                            *(DWORD*)dest = codeDelta;
                            dest += sizeof(DWORD);
                            *(DWORD*)dest = callArgCnt;
                            dest += sizeof(DWORD);
                            *(DWORD*)dest = pndCount;
                            dest += sizeof(DWORD);
                            pPndSize = dest;
                            dest += sizeof(DWORD); // Leave space for pndSize
                        }

                        unsigned offs, iter;

                        for (iter = pasStk.pasEnumGCoffs(pasENUM_START, &offs); pndCount;
                             iter = pasStk.pasEnumGCoffs(iter, &offs), pndCount--)
                        {
                            unsigned eltSize = encodeUnsigned(dest, offs);

                            pndSize += eltSize;
                            if (mask)
                                dest += eltSize;
                        }
                        assert(iter == pasENUM_END);

                        if (mask == 0)
                        {
                            dest = base + 2 + 4 * sizeof(DWORD) + pndSize;
                        }
                        else
                        {
                            assert(pPndSize + sizeof(pndSize) + pndSize == dest);
                            *(DWORD*)pPndSize = pndSize;
                        }

                        goto NEXT_RPD;
                    }

                    argMask = byrefArgMask = 0;

                    if (pasStk.pasHasGCptrs())
                    {
                        assert(pasStk.pasCurDepth() <= BITS_IN_pasMask);

                        argMask      = pasStk.pasArgMask();
                        byrefArgMask = pasStk.pasByrefArgMask();
                    }

                    /* Shouldn't be reporting trivial call-sites */

                    assert(regMask || argMask || callArgCnt || pasStk.pasCurDepth());

// Emit IPtrMask if needed

#define CHK_NON_INTRPT_ESP_IPtrMask                                                                                    \
                                                                                                                       \
    if (byrefRegMask || byrefArgMask)                                                                                  \
    {                                                                                                                  \
        *dest++        = 0xF0;                                                                                         \
        unsigned imask = (byrefArgMask << 4) | byrefRegMask;                                                           \
        dest += encodeUnsigned(dest, imask);                                                                           \
    }

                    /* When usePopEncoding is true:
                     *  this is not an interesting call site
                     *   because nothing is live here.
                     */
                    usePopEncoding = ((callArgCnt < 4) && (regMask == 0) && (argMask == 0));

                    if (!usePopEncoding)
                    {
                        int pattern = lookupCallPattern(callArgCnt, regMask, argMask, codeDelta);
                        if (pattern != -1)
                        {
                            if (pattern > 0xff)
                            {
                                codeDelta = pattern >> 8;
                                pattern &= 0xff;
                                if (codeDelta >= 16)
                                {
                                    /* use encoding: */
                                    /*   skip 01000000 [Delta] */
                                    *dest++ = 0x40;
                                    dest += encodeUnsigned(dest, codeDelta);
                                    codeDelta = 0;
                                }
                                else
                                {
                                    /* use encoding: */
                                    /*   skip 0100DDDD  small delta=DDDD */
                                    *dest++ = 0x40 | (BYTE)codeDelta;
                                }
                            }

                            // Emit IPtrMask if needed
                            CHK_NON_INTRPT_ESP_IPtrMask;

                            assert((pattern >= 0) && (pattern < 80));
                            *dest++ = (BYTE)(0x80 | pattern);
                            goto NEXT_RPD;
                        }

                        /* See if we can use 2nd call encoding
                         *     1101RRRR DDCCCMMM encoding */

                        if ((callArgCnt <= 7) && (argMask <= 7))
                        {
                            unsigned inx; // callCommonDelta[] index
                            unsigned maxCommonDelta = callCommonDelta[3];

                            if (codeDelta > maxCommonDelta)
                            {
                                if (codeDelta > maxCommonDelta + 15)
                                {
                                    /* use encoding: */
                                    /*   skip    01000000 [Delta] */
                                    *dest++ = 0x40;
                                    dest += encodeUnsigned(dest, codeDelta - maxCommonDelta);
                                }
                                else
                                {
                                    /* use encoding: */
                                    /*   skip 0100DDDD  small delta=DDDD */
                                    *dest++ = 0x40 | (BYTE)(codeDelta - maxCommonDelta);
                                }

                                codeDelta = maxCommonDelta;
                                inx       = 3;
                                goto EMIT_2ND_CALL_ENCODING;
                            }

                            for (inx = 0; inx < 4; inx++)
                            {
                                if (codeDelta == callCommonDelta[inx])
                                {
                                EMIT_2ND_CALL_ENCODING:
                                    // Emit IPtrMask if needed
                                    CHK_NON_INTRPT_ESP_IPtrMask;

                                    *dest++ = (BYTE)(0xD0 | regMask);
                                    *dest++ = (BYTE)((inx << 6) | (callArgCnt << 3) | argMask);
                                    goto NEXT_RPD;
                                }
                            }

                            unsigned minCommonDelta = callCommonDelta[0];

                            if ((codeDelta > minCommonDelta) && (codeDelta < maxCommonDelta))
                            {
                                assert((minCommonDelta + 16) > maxCommonDelta);
                                /* use encoding: */
                                /*   skip 0100DDDD  small delta=DDDD */
                                *dest++ = 0x40 | (BYTE)(codeDelta - minCommonDelta);

                                codeDelta = minCommonDelta;
                                inx       = 0;
                                goto EMIT_2ND_CALL_ENCODING;
                            }
                        }
                    }

                    if (codeDelta >= 16)
                    {
                        unsigned i = (usePopEncoding ? 15 : 0);
                        /* use encoding: */
                        /*   skip    01000000 [Delta]  arbitrary sized delta */
                        *dest++ = 0x40;
                        dest += encodeUnsigned(dest, codeDelta - i);
                        codeDelta = i;
                    }

                    if ((codeDelta > 0) || usePopEncoding)
                    {
                        if (usePopEncoding)
                        {
                            /* use encoding: */
                            /*   pop 01CCDDDD  ESP pop CC items, 4-bit delta */
                            if (callArgCnt || codeDelta)
                                *dest++ = (BYTE)(0x40 | (callArgCnt << 4) | codeDelta);
                            goto NEXT_RPD;
                        }
                        else
                        {
                            /* use encoding: */
                            /*   skip 0100DDDD  small delta=DDDD */
                            *dest++ = 0x40 | (BYTE)codeDelta;
                        }
                    }

                    // Emit IPtrMask if needed
                    CHK_NON_INTRPT_ESP_IPtrMask;

                    /* use encoding:                                   */
                    /*   call 1110RRRR [ArgCnt] [ArgMask]              */

                    *dest++ = (BYTE)(0xE0 | regMask);
                    dest += encodeUnsigned(dest, callArgCnt);

                    dest += encodeUnsigned(dest, argMask);
                }
                else
                {
                    /* This is a push or a pop site */

                    /* Remember the new 'last' offset */

                    lastOffset = nextOffset;

                    if (genRegPtrTemp->rpdArgTypeGet() == rpdARG_POP)
                    {
                        /* This must be a gcArgPopSingle */

                        assert(genRegPtrTemp->rpdPtrArg == 1);

                        if (codeDelta >= 16)
                        {
                            /* use encoding: */
                            /*   skip    01000000 [Delta] */
                            *dest++ = 0x40;
                            dest += encodeUnsigned(dest, codeDelta - 15);
                            codeDelta = 15;
                        }

                        /* use encoding: */
                        /*   pop1    0101DDDD  ESP pop one item, 4-bit delta */

                        *dest++ = 0x50 | (BYTE)codeDelta;

                        /* adjust argMask for this pop */
                        pasStk.pasPop(1);
                    }
                    else
                    {
                        /* This is a push */

                        if (codeDelta >= 32)
                        {
                            /* use encoding: */
                            /*   skip    01000000 [Delta] */
                            *dest++ = 0x40;
                            dest += encodeUnsigned(dest, codeDelta - 31);
                            codeDelta = 31;
                        }

                        assert(codeDelta < 32);

                        /* use encoding: */
                        /*   push    000DDDDD ESP push one item, 5-bit delta */

                        *dest++ = (BYTE)codeDelta;

                        /* adjust argMask for this push */
                        pasStk.pasPush(genRegPtrTemp->rpdGCtypeGet());
                    }
                }
            }

        /*  We ignore the register live/dead information, since the
         *  rpdCallRegMask contains all the liveness information
         *  that we need
         */
        NEXT_RPD:

            totalSize += dest - base;

            /* Go back to the buffer start if we're not generating a table */

            if (!mask)
                dest = base;

#if REGEN_CALLPAT
            if ((mask == -1) && (usePopEncoding == false) && ((dest - base) > 0))
                regenLog(origCodeDelta, argMask, regMask, callArgCnt, byrefArgMask, byrefRegMask, base, (dest - base));
#endif
        }

        /* Verify that we pop every arg that was pushed and that argMask is 0 */

        assert(pasStk.pasCurDepth() == 0);

#endif // TARGET_X86

        /* Terminate the table with 0xFF */

        *dest = 0xFF;
        dest -= mask;
        totalSize++;
    }

#if VERIFY_GC_TABLES
    if (mask)
    {
        *(short*)dest = (short)0xBEEB;
        dest += sizeof(short);
    }
    totalSize += sizeof(short);
#endif

#if MEASURE_PTRTAB_SIZE

    if (mask)
        s_gcTotalPtrTabSize += totalSize;

#endif

    return totalSize;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

/*****************************************************************************/
#if DUMP_GC_TABLES
/*****************************************************************************
 *
 *  Dump the contents of a GC pointer table.
 */

#include "gcdump.h"

#if VERIFY_GC_TABLES
const bool verifyGCTables = true;
#else
const bool verifyGCTables = false;
#endif

/*****************************************************************************
 *
 *  Dump the info block header.
 */

size_t GCInfo::gcInfoBlockHdrDump(const BYTE* table, InfoHdr* header, unsigned* methodSize)
{
    GCDump gcDump(GCINFO_VERSION);

#ifdef DEBUG
    gcDump.gcPrintf = gcDump_logf; // use my printf (which logs to VM)
#else
    gcDump.gcPrintf       = printf;
#endif

    printf("Method info block:\n");

    return gcDump.DumpInfoHdr(table, header, methodSize, verifyGCTables);
}

/*****************************************************************************/

size_t GCInfo::gcDumpPtrTable(const BYTE* table, const InfoHdr& header, unsigned methodSize)
{
    printf("Pointer table:\n");

    GCDump gcDump(GCINFO_VERSION);

#ifdef DEBUG
    gcDump.gcPrintf = gcDump_logf; // use my printf (which logs to VM)
#else
    gcDump.gcPrintf       = printf;
#endif

    return gcDump.DumpGCTable(table, header, methodSize, verifyGCTables);
}

/*****************************************************************************
 *
 *  Find all the live pointers in a stack frame.
 */

void GCInfo::gcFindPtrsInFrame(const void* infoBlock, const void* codeBlock, unsigned offs)
{
    GCDump gcDump(GCINFO_VERSION);

#ifdef DEBUG
    gcDump.gcPrintf = gcDump_logf; // use my printf (which logs to VM)
#else
    gcDump.gcPrintf       = printf;
#endif

    gcDump.DumpPtrsInFrame((PTR_CBYTE)infoBlock, (const BYTE*)codeBlock, offs, verifyGCTables);
}

#endif // DUMP_GC_TABLES

#else // !JIT32_GCENCODER

#include "gcinfoencoder.h"

// Do explicit instantiation.
template class JitHashTable<RegSlotIdKey, RegSlotIdKey, GcSlotId>;
template class JitHashTable<StackSlotIdKey, StackSlotIdKey, GcSlotId>;

#if defined(DEBUG) || DUMP_GC_TABLES

// This is a copy of GcStackSlotBaseNames from gcinfotypes.h so we can compile in to non-DEBUG builds.
const char* const JitGcStackSlotBaseNames[] = {"caller.sp", "sp", "frame"};

static const char* const GcSlotFlagsNames[] = {"",
                                               "(byref) ",
                                               "(pinned) ",
                                               "(byref, pinned) ",
                                               "(untracked) ",
                                               "(byref, untracked) ",
                                               "(pinned, untracked) ",
                                               "(byref, pinned, untracked) "};

// I'm making a local wrapper class for GcInfoEncoder so that can add logging of my own (DLD).
class GcInfoEncoderWithLogging
{
    GcInfoEncoder* m_gcInfoEncoder;
    bool           m_doLogging;

public:
    GcInfoEncoderWithLogging(GcInfoEncoder* gcInfoEncoder, bool verbose)
        : m_gcInfoEncoder(gcInfoEncoder), m_doLogging(verbose INDEBUG(|| JitConfig.JitGCInfoLogging() != 0))
    {
    }

    GcSlotId GetStackSlotId(INT32 spOffset, GcSlotFlags flags, GcStackSlotBase spBase = GC_CALLER_SP_REL)
    {
        GcSlotId newSlotId = m_gcInfoEncoder->GetStackSlotId(spOffset, flags, spBase);
        if (m_doLogging)
        {
            printf("Stack slot id for offset %d (%s0x%x) (%s) %s= %d.\n", spOffset, spOffset < 0 ? "-" : "",
                   abs(spOffset), JitGcStackSlotBaseNames[spBase], GcSlotFlagsNames[flags & 7], newSlotId);
        }
        return newSlotId;
    }

    GcSlotId GetRegisterSlotId(UINT32 regNum, GcSlotFlags flags)
    {
        GcSlotId newSlotId = m_gcInfoEncoder->GetRegisterSlotId(regNum, flags);
        if (m_doLogging)
        {
            printf("Register slot id for reg %s %s= %d.\n", getRegName(regNum), GcSlotFlagsNames[flags & 7], newSlotId);
        }
        return newSlotId;
    }

    void SetSlotState(UINT32 instructionOffset, GcSlotId slotId, GcSlotState slotState)
    {
        m_gcInfoEncoder->SetSlotState(instructionOffset, slotId, slotState);
        if (m_doLogging)
        {
            printf("Set state of slot %d at instr offset 0x%x to %s.\n", slotId, instructionOffset,
                   (slotState == GC_SLOT_LIVE ? "Live" : "Dead"));
        }
    }

    void DefineCallSites(UINT32* pCallSites, BYTE* pCallSiteSizes, UINT32 numCallSites)
    {
        m_gcInfoEncoder->DefineCallSites(pCallSites, pCallSiteSizes, numCallSites);
        if (m_doLogging)
        {
            printf("Defining %d call sites:\n", numCallSites);
            for (UINT32 k = 0; k < numCallSites; k++)
            {
                printf("    Offset 0x%x, size %d.\n", pCallSites[k], pCallSiteSizes[k]);
            }
        }
    }

    void DefineInterruptibleRange(UINT32 startInstructionOffset, UINT32 length)
    {
        m_gcInfoEncoder->DefineInterruptibleRange(startInstructionOffset, length);
        if (m_doLogging)
        {
            printf("Defining interruptible range: [0x%x, 0x%x).\n", startInstructionOffset,
                   startInstructionOffset + length);
        }
    }

    void SetCodeLength(UINT32 length)
    {
        m_gcInfoEncoder->SetCodeLength(length);
        if (m_doLogging)
        {
            printf("Set code length to %d.\n", length);
        }
    }

    void SetReturnKind(ReturnKind returnKind)
    {
        m_gcInfoEncoder->SetReturnKind(returnKind);
        if (m_doLogging)
        {
            printf("Set ReturnKind to %s.\n", ReturnKindToString(returnKind));
        }
    }

    void SetStackBaseRegister(UINT32 registerNumber)
    {
        m_gcInfoEncoder->SetStackBaseRegister(registerNumber);
        if (m_doLogging)
        {
            printf("Set stack base register to %s.\n", getRegName(registerNumber));
        }
    }

    void SetPrologSize(UINT32 prologSize)
    {
        m_gcInfoEncoder->SetPrologSize(prologSize);
        if (m_doLogging)
        {
            printf("Set prolog size 0x%x.\n", prologSize);
        }
    }

    void SetGSCookieStackSlot(INT32 spOffsetGSCookie, UINT32 validRangeStart, UINT32 validRangeEnd)
    {
        m_gcInfoEncoder->SetGSCookieStackSlot(spOffsetGSCookie, validRangeStart, validRangeEnd);
        if (m_doLogging)
        {
            printf("Set GS Cookie stack slot to %d, valid from 0x%x to 0x%x.\n", spOffsetGSCookie, validRangeStart,
                   validRangeEnd);
        }
    }

    void SetPSPSymStackSlot(INT32 spOffsetPSPSym)
    {
        m_gcInfoEncoder->SetPSPSymStackSlot(spOffsetPSPSym);
        if (m_doLogging)
        {
            printf("Set PSPSym stack slot to %d.\n", spOffsetPSPSym);
        }
    }

    void SetGenericsInstContextStackSlot(INT32 spOffsetGenericsContext, GENERIC_CONTEXTPARAM_TYPE type)
    {
        m_gcInfoEncoder->SetGenericsInstContextStackSlot(spOffsetGenericsContext, type);
        if (m_doLogging)
        {
            printf("Set generic instantiation context stack slot to %d, type is %s.\n", spOffsetGenericsContext,
                   (type == GENERIC_CONTEXTPARAM_THIS
                        ? "THIS"
                        : (type == GENERIC_CONTEXTPARAM_MT ? "MT"
                                                           : (type == GENERIC_CONTEXTPARAM_MD ? "MD" : "UNKNOWN!"))));
        }
    }

    void SetIsVarArg()
    {
        m_gcInfoEncoder->SetIsVarArg();
        if (m_doLogging)
        {
            printf("SetIsVarArg.\n");
        }
    }

#ifdef TARGET_AMD64
    void SetWantsReportOnlyLeaf()
    {
        m_gcInfoEncoder->SetWantsReportOnlyLeaf();
        if (m_doLogging)
        {
            printf("Set WantsReportOnlyLeaf.\n");
        }
    }
#elif defined(TARGET_ARMARCH)
    void SetHasTailCalls()
    {
        m_gcInfoEncoder->SetHasTailCalls();
        if (m_doLogging)
        {
            printf("Set HasTailCalls.\n");
        }
    }
#endif // TARGET_AMD64

    void SetSizeOfStackOutgoingAndScratchArea(UINT32 size)
    {
        m_gcInfoEncoder->SetSizeOfStackOutgoingAndScratchArea(size);
        if (m_doLogging)
        {
            printf("Set Outgoing stack arg area size to %d.\n", size);
        }
    }
};

#define GCENCODER_WITH_LOGGING(withLog, realEncoder)                                                                   \
    GcInfoEncoderWithLogging  withLog##Var(realEncoder, INDEBUG(compiler->verbose ||) compiler->opts.dspGCtbls);       \
    GcInfoEncoderWithLogging* withLog = &withLog##Var;

#else // !(defined(DEBUG) || DUMP_GC_TABLES)

#define GCENCODER_WITH_LOGGING(withLog, realEncoder) GcInfoEncoder* withLog = realEncoder;

#endif // !(defined(DEBUG) || DUMP_GC_TABLES)

void GCInfo::gcInfoBlockHdrSave(GcInfoEncoder* gcInfoEncoder, unsigned methodSize, unsigned prologSize)
{
#ifdef DEBUG
    if (compiler->verbose)
    {
        printf("*************** In gcInfoBlockHdrSave()\n");
    }
#endif

    GCENCODER_WITH_LOGGING(gcInfoEncoderWithLog, gcInfoEncoder);

    // Can't create tables if we've not saved code.

    gcInfoEncoderWithLog->SetCodeLength(methodSize);

    gcInfoEncoderWithLog->SetReturnKind(getReturnKind());

    if (compiler->isFramePointerUsed())
    {
        gcInfoEncoderWithLog->SetStackBaseRegister(REG_FPBASE);
    }

    if (compiler->info.compIsVarArgs)
    {
        gcInfoEncoderWithLog->SetIsVarArg();
    }
    // No equivalents.
    // header->profCallbacks = compiler->info.compProfilerCallback;
    // header->editNcontinue = compiler->opts.compDbgEnC;
    //
    if (compiler->lvaReportParamTypeArg())
    {
        // The predicate above is true only if there is an extra generic context parameter, not for
        // the case where the generic context is provided by "this."
        assert((SIZE_T)compiler->info.compTypeCtxtArg != BAD_VAR_NUM);
        GENERIC_CONTEXTPARAM_TYPE ctxtParamType = GENERIC_CONTEXTPARAM_NONE;
        switch (compiler->info.compMethodInfo->options & CORINFO_GENERICS_CTXT_MASK)
        {
            case CORINFO_GENERICS_CTXT_FROM_METHODDESC:
                ctxtParamType = GENERIC_CONTEXTPARAM_MD;
                break;
            case CORINFO_GENERICS_CTXT_FROM_METHODTABLE:
                ctxtParamType = GENERIC_CONTEXTPARAM_MT;
                break;

            case CORINFO_GENERICS_CTXT_FROM_THIS: // See comment above.
            default:
                // If we have a generic context parameter, then we should have
                // one of the two options flags handled above.
                assert(false);
        }

        const int offset = compiler->lvaToCallerSPRelativeOffset(compiler->lvaCachedGenericContextArgOffset(),
                                                                 compiler->isFramePointerUsed());

#ifdef DEBUG
        if (compiler->opts.IsOSR())
        {
            // Sanity check the offset vs saved patchpoint info.
            //
            const PatchpointInfo* const ppInfo = compiler->info.compPatchpointInfo;
#if defined(TARGET_AMD64)
            // PP info has FP relative offset, to get to caller SP we need to
            // subtract off 2 register slots (saved FP, saved RA).
            //
            const int osrOffset = ppInfo->GenericContextArgOffset() - 2 * REGSIZE_BYTES;
            assert(offset == osrOffset);
#elif defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
            // PP info has virtual offset. This is also the caller SP offset.
            //
            const int osrOffset = ppInfo->GenericContextArgOffset();
            assert(offset == osrOffset);
#endif
        }
#endif

        gcInfoEncoderWithLog->SetGenericsInstContextStackSlot(offset, ctxtParamType);
    }
    // As discussed above, handle the case where the generics context is obtained via
    // the method table of "this".
    else if (compiler->lvaKeepAliveAndReportThis())
    {
        assert(compiler->info.compThisArg != BAD_VAR_NUM);

        // OSR can report the root method's frame slot, if that method reported context.
        // If not, the OSR frame will have saved the needed context.
        //
        bool useRootFrameSlot = true;
        if (compiler->opts.IsOSR())
        {
            const PatchpointInfo* const ppInfo = compiler->info.compPatchpointInfo;

            useRootFrameSlot = ppInfo->HasKeptAliveThis();
        }

        const int offset = compiler->lvaToCallerSPRelativeOffset(compiler->lvaCachedGenericContextArgOffset(),
                                                                 compiler->isFramePointerUsed(), useRootFrameSlot);

#ifdef DEBUG
        if (compiler->opts.IsOSR() && useRootFrameSlot)
        {
            // Sanity check the offset vs saved patchpoint info.
            //
            const PatchpointInfo* const ppInfo = compiler->info.compPatchpointInfo;
#if defined(TARGET_AMD64)
            // PP info has FP relative offset, to get to caller SP we need to
            // subtract off 2 register slots (saved FP, saved RA).
            //
            const int osrOffset = ppInfo->KeptAliveThisOffset() - 2 * REGSIZE_BYTES;
            assert(offset == osrOffset);
#elif defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
            // PP info has virtual offset. This is also the caller SP offset.
            //
            const int osrOffset = ppInfo->KeptAliveThisOffset();
            assert(offset == osrOffset);
#endif
        }
#endif

        gcInfoEncoderWithLog->SetGenericsInstContextStackSlot(offset, GENERIC_CONTEXTPARAM_THIS);
    }

    if (compiler->getNeedsGSSecurityCookie())
    {
        assert(compiler->lvaGSSecurityCookie != BAD_VAR_NUM);

        // The lv offset is FP-relative, and the using code expects caller-sp relative, so translate.
        const int offset = compiler->lvaGetCallerSPRelativeOffset(compiler->lvaGSSecurityCookie);

        // The code offset ranges assume that the GS Cookie slot is initialized in the prolog, and is valid
        // through the remainder of the method.  We will not query for the GS Cookie while we're in an epilog,
        // so the question of where in the epilog it becomes invalid is moot.
        gcInfoEncoderWithLog->SetGSCookieStackSlot(offset, prologSize, methodSize);
    }
    else if (compiler->lvaReportParamTypeArg() || compiler->lvaKeepAliveAndReportThis())
    {
        gcInfoEncoderWithLog->SetPrologSize(prologSize);
    }

#if defined(FEATURE_EH_FUNCLETS)
    if (compiler->lvaPSPSym != BAD_VAR_NUM)
    {
#ifdef TARGET_AMD64
        // The PSPSym is relative to InitialSP on X64 and CallerSP on other platforms.
        gcInfoEncoderWithLog->SetPSPSymStackSlot(compiler->lvaGetInitialSPRelativeOffset(compiler->lvaPSPSym));
#else  // !TARGET_AMD64
        gcInfoEncoderWithLog->SetPSPSymStackSlot(compiler->lvaGetCallerSPRelativeOffset(compiler->lvaPSPSym));
#endif // !TARGET_AMD64
    }

#ifdef TARGET_AMD64
    if (compiler->ehAnyFunclets())
    {
        // Set this to avoid double-reporting the parent frame (unlike JIT64)
        gcInfoEncoderWithLog->SetWantsReportOnlyLeaf();
    }
#endif // TARGET_AMD64

#endif // FEATURE_EH_FUNCLETS

#ifdef TARGET_ARMARCH
    if (compiler->codeGen->GetHasTailCalls())
    {
        gcInfoEncoderWithLog->SetHasTailCalls();
    }
#endif // TARGET_ARMARCH

#if FEATURE_FIXED_OUT_ARGS
    // outgoing stack area size
    gcInfoEncoderWithLog->SetSizeOfStackOutgoingAndScratchArea(compiler->lvaOutgoingArgSpaceSize);
#endif // FEATURE_FIXED_OUT_ARGS

#if DISPLAY_SIZES

    if (compiler->codeGen->GetInterruptible())
    {
        genMethodICnt++;
    }
    else
    {
        genMethodNCnt++;
    }

#endif // DISPLAY_SIZES
}

#if defined(DEBUG) || DUMP_GC_TABLES
#define Encoder GcInfoEncoderWithLogging
#else
#define Encoder GcInfoEncoder
#endif

// Small helper class to handle the No-GC-Interrupt callbacks
// when reporting interruptible ranges.
//
// Encoder should be either GcInfoEncoder or GcInfoEncoderWithLogging
//
struct InterruptibleRangeReporter
{
    unsigned prevStart;
    Encoder* gcInfoEncoderWithLog;

    InterruptibleRangeReporter(unsigned _prevStart, Encoder* _gcInfo)
        : prevStart(_prevStart), gcInfoEncoderWithLog(_gcInfo)
    {
    }

    // This callback is called for each insGroup marked with
    // IGF_NOGCINTERRUPT (currently just prologs and epilogs).
    // Report everything between the previous region and the current
    // region as interruptible.

    bool operator()(unsigned igFuncIdx, unsigned igOffs, unsigned igSize)
    {
        if (igOffs < prevStart)
        {
            // We're still in the main method prolog, which has already
            // had it's interruptible range reported.
            assert(igFuncIdx == 0);
            assert(igOffs + igSize <= prevStart);
            return true;
        }

        assert(igOffs >= prevStart);
        if (igOffs > prevStart)
        {
            gcInfoEncoderWithLog->DefineInterruptibleRange(prevStart, igOffs - prevStart);
        }
        prevStart = igOffs + igSize;
        return true;
    }
};

void GCInfo::gcMakeRegPtrTable(
    GcInfoEncoder* gcInfoEncoder, unsigned codeSize, unsigned prologSize, MakeRegPtrMode mode, unsigned* callCntRef)
{
    GCENCODER_WITH_LOGGING(gcInfoEncoderWithLog, gcInfoEncoder);

    const bool noTrackedGCSlots =
        (compiler->opts.MinOpts() && !compiler->opts.jitFlags->IsSet(JitFlags::JIT_FLAG_PREJIT) &&
         !JitConfig.JitMinOptsTrackGCrefs());

    if (mode == MAKE_REG_PTR_MODE_ASSIGN_SLOTS)
    {
        m_regSlotMap   = new (compiler->getAllocator()) RegSlotMap(compiler->getAllocator());
        m_stackSlotMap = new (compiler->getAllocator()) StackSlotMap(compiler->getAllocator());
    }

    /**************************************************************************
     *
     *                      Untracked ptr variables
     *
     **************************************************************************
     */

    /* Count&Write untracked locals and non-enregistered args */

    unsigned   varNum;
    LclVarDsc* varDsc;
    for (varNum = 0, varDsc = compiler->lvaTable; varNum < compiler->lvaCount; varNum++, varDsc++)
    {
        if (compiler->lvaIsFieldOfDependentlyPromotedStruct(varDsc))
        {
            // Field local of a PROMOTION_TYPE_DEPENDENT struct must have been
            // reported through its parent local.
            continue;
        }

        if (varTypeIsGC(varDsc->TypeGet()))
        {
            // Do we have an argument or local variable?
            if (!varDsc->lvIsParam)
            {
                // If it is pinned, it must be an untracked local.
                assert(!varDsc->lvPinned || !varDsc->lvTracked);

                if (varDsc->lvTracked || !varDsc->lvOnFrame)
                {
                    continue;
                }
            }
            else
            {
                // Stack-passed arguments which are not enregistered
                // are always reported in this "untracked stack
                // pointers" section of the GC info even if lvTracked==true

                // Has this argument been fully enregistered?
                CLANG_FORMAT_COMMENT_ANCHOR;

                if (!varDsc->lvOnFrame)
                {
                    // If a CEE_JMP has been used, then we need to report all the arguments
                    // even if they are enregistered, since we will be using this value
                    // in a JMP call.  Note that this is subtle as we require that
                    // argument offsets are always fixed up properly even if lvRegister
                    // is set.
                    if (!compiler->compJmpOpUsed)
                    {
                        continue;
                    }
                }
                else
                {
                    if (varDsc->lvIsRegArg && varDsc->lvTracked)
                    {
                        // If this register-passed arg is tracked, then
                        // it has been allocated space near the other
                        // pointer variables and we have accurate life-
                        // time info. It will be reported with
                        // gcVarPtrList in the "tracked-pointer" section.
                        continue;
                    }
                }
            }

            // If we haven't continued to the next variable, we should report this as an untracked local.
            CLANG_FORMAT_COMMENT_ANCHOR;

            GcSlotFlags flags = GC_SLOT_UNTRACKED;

            if (varDsc->TypeGet() == TYP_BYREF)
            {
                // Or in byref_OFFSET_FLAG for 'byref' pointer tracking
                flags = (GcSlotFlags)(flags | GC_SLOT_INTERIOR);
            }

            if (varDsc->lvPinned)
            {
                // Or in pinned_OFFSET_FLAG for 'pinned' pointer tracking
                flags = (GcSlotFlags)(flags | GC_SLOT_PINNED);
            }
            GcStackSlotBase stackSlotBase = GC_SP_REL;
            if (varDsc->lvFramePointerBased)
            {
                stackSlotBase = GC_FRAMEREG_REL;
            }
            if (noTrackedGCSlots)
            {
                // No need to hash/lookup untracked GC refs; just grab a new Slot Id.
                if (mode == MAKE_REG_PTR_MODE_ASSIGN_SLOTS)
                {
                    gcInfoEncoderWithLog->GetStackSlotId(varDsc->GetStackOffset(), flags, stackSlotBase);
                }
            }
            else
            {
                StackSlotIdKey sskey(varDsc->GetStackOffset(), (stackSlotBase == GC_FRAMEREG_REL), flags);
                GcSlotId       varSlotId;
                if (mode == MAKE_REG_PTR_MODE_ASSIGN_SLOTS)
                {
                    if (!m_stackSlotMap->Lookup(sskey, &varSlotId))
                    {
                        varSlotId =
                            gcInfoEncoderWithLog->GetStackSlotId(varDsc->GetStackOffset(), flags, stackSlotBase);
                        m_stackSlotMap->Set(sskey, varSlotId);
                    }
                }
            }
        }

        // If this is a TYP_STRUCT, handle its GC pointers.
        // Note that the enregisterable struct types cannot have GC pointers in them.
        if ((varDsc->TypeGet() == TYP_STRUCT) && varDsc->GetLayout()->HasGCPtr() && varDsc->lvOnFrame &&
            (varDsc->lvExactSize() >= TARGET_POINTER_SIZE))
        {
            ClassLayout* layout = varDsc->GetLayout();
            unsigned     slots  = layout->GetSlotCount();

            for (unsigned i = 0; i < slots; i++)
            {
                if (!layout->IsGCPtr(i))
                {
                    continue;
                }

                unsigned const fieldOffset = i * TARGET_POINTER_SIZE;
                int const      offset      = varDsc->GetStackOffset() + fieldOffset;

#ifdef DEBUG
                if (varDsc->lvPromoted)
                {
                    assert(compiler->lvaGetPromotionType(varDsc) == Compiler::PROMOTION_TYPE_DEPENDENT);

                    // A dependently promoted tracked gc local can end up in the gc tracked
                    // frame range. If so it should be excluded from tracking via lvaIsGCTracked.
                    //
                    unsigned const fieldLclNum = compiler->lvaGetFieldLocal(varDsc, fieldOffset);
                    assert(fieldLclNum != BAD_VAR_NUM);
                    LclVarDsc* const fieldVarDsc = compiler->lvaGetDesc(fieldLclNum);

                    if (compiler->GetEmitter()->emitIsWithinFrameRangeGCRs(offset))
                    {
                        assert(!compiler->lvaIsGCTracked(fieldVarDsc));
                        JITDUMP("Untracked GC struct slot V%02u+%u (P-DEP promoted V%02u) is at frame offset %d within "
                                "tracked ref range; will report slot as untracked\n",
                                varNum, fieldOffset, fieldLclNum, offset);
                    }
                }
#endif

#if DOUBLE_ALIGN
                // For genDoubleAlign(), locals are addressed relative to ESP and
                // arguments are addressed relative to EBP.

                if (compiler->genDoubleAlign() && varDsc->lvIsParam && !varDsc->lvIsRegArg)
                    offset += compiler->codeGen->genTotalFrameSize();
#endif
                GcSlotFlags flags = GC_SLOT_UNTRACKED;
                if (layout->GetGCPtrType(i) == TYP_BYREF)
                {
                    flags = (GcSlotFlags)(flags | GC_SLOT_INTERIOR);
                }

                GcStackSlotBase stackSlotBase = GC_SP_REL;
                if (varDsc->lvFramePointerBased)
                {
                    stackSlotBase = GC_FRAMEREG_REL;
                }
                StackSlotIdKey sskey(offset, (stackSlotBase == GC_FRAMEREG_REL), flags);
                GcSlotId       varSlotId;
                if (mode == MAKE_REG_PTR_MODE_ASSIGN_SLOTS)
                {
                    if (!m_stackSlotMap->Lookup(sskey, &varSlotId))
                    {
                        varSlotId = gcInfoEncoderWithLog->GetStackSlotId(offset, flags, stackSlotBase);
                        m_stackSlotMap->Set(sskey, varSlotId);
                    }
                }
            }
        }
    }

    if (mode == MAKE_REG_PTR_MODE_ASSIGN_SLOTS)
    {
        // Count&Write spill temps that hold pointers.

        assert(compiler->codeGen->regSet.tmpAllFree());
        for (TempDsc* tempItem = compiler->codeGen->regSet.tmpListBeg(); tempItem != nullptr;
             tempItem          = compiler->codeGen->regSet.tmpListNxt(tempItem))
        {
            if (varTypeIsGC(tempItem->tdTempType()))
            {
                int offset = tempItem->tdTempOffs();

                GcSlotFlags flags = GC_SLOT_UNTRACKED;
                if (tempItem->tdTempType() == TYP_BYREF)
                {
                    flags = (GcSlotFlags)(flags | GC_SLOT_INTERIOR);
                }

                GcStackSlotBase stackSlotBase = GC_SP_REL;
                if (compiler->isFramePointerUsed())
                {
                    stackSlotBase = GC_FRAMEREG_REL;
                }
                StackSlotIdKey sskey(offset, (stackSlotBase == GC_FRAMEREG_REL), flags);
                GcSlotId       varSlotId;
                if (!m_stackSlotMap->Lookup(sskey, &varSlotId))
                {
                    varSlotId = gcInfoEncoderWithLog->GetStackSlotId(offset, flags, stackSlotBase);
                    m_stackSlotMap->Set(sskey, varSlotId);
                }
            }
        }

        if (compiler->lvaKeepAliveAndReportThis())
        {
            // We need to report the cached copy as an untracked pointer
            assert(compiler->info.compThisArg != BAD_VAR_NUM);
            assert(!compiler->lvaReportParamTypeArg());
            GcSlotFlags flags = GC_SLOT_UNTRACKED;

            if (compiler->lvaTable[compiler->info.compThisArg].TypeGet() == TYP_BYREF)
            {
                // Or in GC_SLOT_INTERIOR for 'byref' pointer tracking
                flags = (GcSlotFlags)(flags | GC_SLOT_INTERIOR);
            }

            GcStackSlotBase stackSlotBase = compiler->isFramePointerUsed() ? GC_FRAMEREG_REL : GC_SP_REL;

            gcInfoEncoderWithLog->GetStackSlotId(compiler->lvaCachedGenericContextArgOffset(), flags, stackSlotBase);
        }
    }

    // Generate the table of tracked stack pointer variable lifetimes.
    gcMakeVarPtrTable(gcInfoEncoder, mode);

    /**************************************************************************
     *
     * Prepare to generate the pointer register/argument map
     *
     **************************************************************************
     */

    if (compiler->codeGen->GetInterruptible())
    {
        assert(compiler->IsFullPtrRegMapRequired());

        regMaskSmall ptrRegs          = 0;
        regPtrDsc*   regStackArgFirst = nullptr;

        // Walk the list of pointer register/argument entries.

        for (regPtrDsc* genRegPtrTemp = gcRegPtrList; genRegPtrTemp != nullptr; genRegPtrTemp = genRegPtrTemp->rpdNext)
        {
            if (genRegPtrTemp->rpdArg)
            {
                if (genRegPtrTemp->rpdArgTypeGet() == rpdARG_KILL)
                {
                    // Kill all arguments for a call
                    if ((mode == MAKE_REG_PTR_MODE_DO_WORK) && (regStackArgFirst != nullptr))
                    {
                        // Record any outgoing arguments as becoming dead
                        gcInfoRecordGCStackArgsDead(gcInfoEncoder, genRegPtrTemp->rpdOffs, regStackArgFirst,
                                                    genRegPtrTemp);
                    }
                    regStackArgFirst = nullptr;
                }
                else if (genRegPtrTemp->rpdGCtypeGet() != GCT_NONE)
                {
                    if (genRegPtrTemp->rpdArgTypeGet() == rpdARG_PUSH || (genRegPtrTemp->rpdPtrArg != 0))
                    {
                        bool isPop = genRegPtrTemp->rpdArgTypeGet() == rpdARG_POP;
                        assert(!isPop);
                        gcInfoRecordGCStackArgLive(gcInfoEncoder, mode, genRegPtrTemp);
                        if (regStackArgFirst == nullptr)
                        {
                            regStackArgFirst = genRegPtrTemp;
                        }
                    }
                    else
                    {
                        // We know it's a POP.  Sometimes we'll record a POP for a call, just to make sure
                        // the call site is recorded.
                        // This is just the negation of the condition:
                        assert(genRegPtrTemp->rpdArgTypeGet() == rpdARG_POP && genRegPtrTemp->rpdPtrArg == 0);
                        // This asserts that we only get here when we're recording a call site.
                        assert(genRegPtrTemp->rpdArg && genRegPtrTemp->rpdIsCallInstr());

                        // Kill all arguments for a call
                        if ((mode == MAKE_REG_PTR_MODE_DO_WORK) && (regStackArgFirst != nullptr))
                        {
                            // Record any outgoing arguments as becoming dead
                            gcInfoRecordGCStackArgsDead(gcInfoEncoder, genRegPtrTemp->rpdOffs, regStackArgFirst,
                                                        genRegPtrTemp);
                        }
                        regStackArgFirst = nullptr;
                    }
                }
            }
            else
            {
                // Record any registers that are becoming dead.

                regMaskSmall regMask   = genRegPtrTemp->rpdCompiler.rpdDel & ptrRegs;
                regMaskSmall byRefMask = 0;
                if (genRegPtrTemp->rpdGCtypeGet() == GCT_BYREF)
                {
                    byRefMask = regMask;
                }
                gcInfoRecordGCRegStateChange(gcInfoEncoder, mode, genRegPtrTemp->rpdOffs, regMask, GC_SLOT_DEAD,
                                             byRefMask, &ptrRegs);

                // Record any registers that are becoming live.
                regMask   = genRegPtrTemp->rpdCompiler.rpdAdd & ~ptrRegs;
                byRefMask = 0;
                // As far as I (DLD, 2010) can tell, there's one GCtype for the entire genRegPtrTemp, so if
                // it says byref then all the registers in "regMask" contain byrefs.
                if (genRegPtrTemp->rpdGCtypeGet() == GCT_BYREF)
                {
                    byRefMask = regMask;
                }
                gcInfoRecordGCRegStateChange(gcInfoEncoder, mode, genRegPtrTemp->rpdOffs, regMask, GC_SLOT_LIVE,
                                             byRefMask, &ptrRegs);
            }
        }

        // Now we can declare the entire method body fully interruptible.
        if (mode == MAKE_REG_PTR_MODE_DO_WORK)
        {
            assert(prologSize <= codeSize);

            // Now exempt any other region marked as IGF_NOGCINTERRUPT
            // Currently just prologs and epilogs.

            InterruptibleRangeReporter reporter(prologSize, gcInfoEncoderWithLog);
            compiler->GetEmitter()->emitGenNoGCLst(reporter);
            prologSize = reporter.prevStart;

            // Report any remainder
            if (prologSize < codeSize)
            {
                gcInfoEncoderWithLog->DefineInterruptibleRange(prologSize, codeSize - prologSize);
            }
        }
    }
    else if (compiler->isFramePointerUsed()) // GetInterruptible() is false, and we're using EBP as a frame pointer.
    {
        assert(compiler->IsFullPtrRegMapRequired() == false);

        // Walk the list of pointer register/argument entries.

        // First count them.
        unsigned numCallSites = 0;

        // Now we can allocate the information.
        unsigned* pCallSites     = nullptr;
        BYTE*     pCallSiteSizes = nullptr;
        unsigned  callSiteNum    = 0;

        if (mode == MAKE_REG_PTR_MODE_DO_WORK)
        {
            if (gcCallDescList != nullptr)
            {
                if (noTrackedGCSlots)
                {
                    // We have the call count from the previous run.
                    numCallSites = *callCntRef;

                    // If there are no calls, tell the world and bail.
                    if (numCallSites == 0)
                    {
                        gcInfoEncoderWithLog->DefineCallSites(nullptr, nullptr, 0);
                        return;
                    }
                }
                else
                {
                    for (CallDsc* call = gcCallDescList; call != nullptr; call = call->cdNext)
                    {
                        numCallSites++;
                    }
                }
                pCallSites     = new (compiler, CMK_GC) unsigned[numCallSites];
                pCallSiteSizes = new (compiler, CMK_GC) BYTE[numCallSites];
            }
        }

        // Now consider every call.
        for (CallDsc* call = gcCallDescList; call != nullptr; call = call->cdNext)
        {
            // Figure out the code offset of this entry.
            unsigned nextOffset = call->cdOffs;

            // As far as I (DLD, 2010) can determine by asking around, the "call->u1.cdArgMask"
            // and "cdArgCnt" cases are to handle x86 situations in which a call expression is nested as an
            // argument to an outer call.  The "natural" (evaluation-order-preserving) thing to do is to
            // evaluate the outer call's arguments, pushing those that are not enregistered, until you
            // encounter the nested call.  These parts of the call description, then, describe the "pending"
            // pushed arguments.  This situation does not exist outside of x86, where we're going to use a
            // fixed-size stack frame: in situations like this nested call, we would evaluate the pending
            // arguments to temporaries, and only "push" them (really, write them to the outgoing argument section
            // of the stack frame) when it's the outer call's "turn."  So we can assert that these
            // situations never occur.
            assert(call->u1.cdArgMask == 0 && call->cdArgCnt == 0);

            // Other than that, we just have to deal with the regmasks.
            regMaskSmall gcrefRegMask = call->cdGCrefRegs & RBM_CALL_GC_REGS;
            regMaskSmall byrefRegMask = call->cdByrefRegs & RBM_CALL_GC_REGS;

            assert((gcrefRegMask & byrefRegMask) == 0);

            regMaskSmall regMask = gcrefRegMask | byrefRegMask;

            assert(call->cdOffs >= call->cdCallInstrSize);
            // call->cdOffs is actually the offset of the instruction *following* the call, so subtract
            // the call instruction size to get the offset of the actual call instruction...
            unsigned callOffset = nextOffset - call->cdCallInstrSize;

            if (noTrackedGCSlots && regMask == 0)
            {
                // No live GC refs in regs at the call -> don't record the call.
            }
            else
            {
                // Append an entry for the call if doing the real thing.
                if (mode == MAKE_REG_PTR_MODE_DO_WORK)
                {
                    pCallSites[callSiteNum] = callOffset;

                    assert(call->cdCallInstrSize <= BYTE_MAX);
                    pCallSiteSizes[callSiteNum] = (BYTE)call->cdCallInstrSize;
                }
                callSiteNum++;

                // Record that these registers are live before the call...
                gcInfoRecordGCRegStateChange(gcInfoEncoder, mode, callOffset, regMask, GC_SLOT_LIVE, byrefRegMask,
                                             nullptr);
                // ...and dead after.
                gcInfoRecordGCRegStateChange(gcInfoEncoder, mode, nextOffset, regMask, GC_SLOT_DEAD, byrefRegMask,
                                             nullptr);
            }
        }
        // Make sure we've recorded the expected number of calls
        assert(mode != MAKE_REG_PTR_MODE_DO_WORK || numCallSites == callSiteNum);
        // Return the actual recorded call count to the caller
        *callCntRef = callSiteNum;

        // OK, define the call sites.
        if (mode == MAKE_REG_PTR_MODE_DO_WORK)
        {
            gcInfoEncoderWithLog->DefineCallSites(pCallSites, pCallSiteSizes, numCallSites);
        }
    }
    else // GetInterruptible() is false and we have an EBP-less frame
    {
        assert(compiler->IsFullPtrRegMapRequired());

        // Walk the list of pointer register/argument entries */
        // First count them.
        unsigned numCallSites = 0;

        // Now we can allocate the information (if we're in the "DO_WORK" pass...)
        unsigned* pCallSites     = nullptr;
        BYTE*     pCallSiteSizes = nullptr;
        unsigned  callSiteNum    = 0;

        if (mode == MAKE_REG_PTR_MODE_DO_WORK)
        {
            for (regPtrDsc* genRegPtrTemp = gcRegPtrList; genRegPtrTemp != nullptr;
                 genRegPtrTemp            = genRegPtrTemp->rpdNext)
            {
                if (genRegPtrTemp->rpdArg && genRegPtrTemp->rpdIsCallInstr())
                {
                    numCallSites++;
                }
            }

            if (numCallSites > 0)
            {
                pCallSites     = new (compiler, CMK_GC) unsigned[numCallSites];
                pCallSiteSizes = new (compiler, CMK_GC) BYTE[numCallSites];
            }
        }

        for (regPtrDsc* genRegPtrTemp = gcRegPtrList; genRegPtrTemp != nullptr; genRegPtrTemp = genRegPtrTemp->rpdNext)
        {
            // Is this a call site?
            if (genRegPtrTemp->rpdIsCallInstr())
            {
                // This is a true call site.

                regMaskSmall gcrefRegMask = genRegMaskFromCalleeSavedMask(genRegPtrTemp->rpdCallGCrefRegs);

                regMaskSmall byrefRegMask = genRegMaskFromCalleeSavedMask(genRegPtrTemp->rpdCallByrefRegs);

                assert((gcrefRegMask & byrefRegMask) == 0);

                regMaskSmall regMask = gcrefRegMask | byrefRegMask;

                // The "rpdOffs" is (apparently) the offset of the following instruction already.
                // GcInfoEncoder wants the call instruction, so subtract the width of the call instruction.
                assert(genRegPtrTemp->rpdOffs >= genRegPtrTemp->rpdCallInstrSize);
                unsigned callOffset = genRegPtrTemp->rpdOffs - genRegPtrTemp->rpdCallInstrSize;

                // Tell the GCInfo encoder about these registers.  We say that the registers become live
                // before the call instruction, and dead after.
                gcInfoRecordGCRegStateChange(gcInfoEncoder, mode, callOffset, regMask, GC_SLOT_LIVE, byrefRegMask,
                                             nullptr);
                gcInfoRecordGCRegStateChange(gcInfoEncoder, mode, genRegPtrTemp->rpdOffs, regMask, GC_SLOT_DEAD,
                                             byrefRegMask, nullptr);

                // Also remember the call site.
                if (mode == MAKE_REG_PTR_MODE_DO_WORK)
                {
                    assert(pCallSites != nullptr && pCallSiteSizes != nullptr);
                    pCallSites[callSiteNum]     = callOffset;
                    pCallSiteSizes[callSiteNum] = genRegPtrTemp->rpdCallInstrSize;
                    callSiteNum++;
                }
            }
            else if (genRegPtrTemp->rpdArg)
            {
                // These are reporting outgoing stack arguments, but we don't need to report anything
                // for partially interruptible
                assert(genRegPtrTemp->rpdGCtypeGet() != GCT_NONE);
                assert(genRegPtrTemp->rpdArgTypeGet() == rpdARG_PUSH);
            }
        }

        // The routine is fully interruptible.
        if (mode == MAKE_REG_PTR_MODE_DO_WORK)
        {
            gcInfoEncoderWithLog->DefineCallSites(pCallSites, pCallSiteSizes, numCallSites);
        }
    }
}

void GCInfo::gcInfoRecordGCRegStateChange(GcInfoEncoder* gcInfoEncoder,
                                          MakeRegPtrMode mode,
                                          unsigned       instrOffset,
                                          regMaskSmall   regMask,
                                          GcSlotState    newState,
                                          regMaskSmall   byRefMask,
                                          regMaskSmall*  pPtrRegs)
{
    // Precondition: byRefMask is a subset of regMask.
    assert((byRefMask & ~regMask) == 0);

    GCENCODER_WITH_LOGGING(gcInfoEncoderWithLog, gcInfoEncoder);

    while (regMask)
    {
        // Get hold of the next register bit.
        regMaskTP tmpMask = genFindLowestBit(regMask);
        assert(tmpMask);

        // Remember the new state of this register.
        if (pPtrRegs != nullptr)
        {
            if (newState == GC_SLOT_DEAD)
            {
                *pPtrRegs &= ~tmpMask;
            }
            else
            {
                *pPtrRegs |= tmpMask;
            }
        }

        // Figure out which register the next bit corresponds to.
        regNumber regNum = genRegNumFromMask(tmpMask);

        /* Reserve SP future use */
        assert(regNum != REG_SPBASE);

        GcSlotFlags regFlags = GC_SLOT_BASE;
        if ((tmpMask & byRefMask) != 0)
        {
            regFlags = (GcSlotFlags)(regFlags | GC_SLOT_INTERIOR);
        }

        assert(regNum == (regNumberSmall)regNum);
        RegSlotIdKey rskey((unsigned short)regNum, regFlags);
        GcSlotId     regSlotId;
        if (mode == MAKE_REG_PTR_MODE_ASSIGN_SLOTS)
        {
            if (!m_regSlotMap->Lookup(rskey, &regSlotId))
            {
                regSlotId = gcInfoEncoderWithLog->GetRegisterSlotId(regNum, regFlags);
                m_regSlotMap->Set(rskey, regSlotId);
            }
        }
        else
        {
            bool b = m_regSlotMap->Lookup(rskey, &regSlotId);
            assert(b); // Should have been added in the first pass.
            gcInfoEncoderWithLog->SetSlotState(instrOffset, regSlotId, newState);
        }

        // Turn the bit we've just generated off and continue.
        regMask -= tmpMask; // EAX,ECX,EDX,EBX,---,EBP,ESI,EDI
    }
}

/**************************************************************************
 *
 *  gcMakeVarPtrTable - Generate the table of tracked stack pointer
 *      variable lifetimes.
 *
 *  In the first pass we'll allocate slot Ids
 *  In the second pass we actually generate the lifetimes.
 *
 **************************************************************************
 */

void GCInfo::gcMakeVarPtrTable(GcInfoEncoder* gcInfoEncoder, MakeRegPtrMode mode)
{
    GCENCODER_WITH_LOGGING(gcInfoEncoderWithLog, gcInfoEncoder);

    // Make sure any flags we hide in the offset are in the bits guaranteed
    // unused by alignment
    C_ASSERT((OFFSET_MASK + 1) <= sizeof(int));

#ifdef DEBUG
    if (mode == MAKE_REG_PTR_MODE_ASSIGN_SLOTS)
    {
        // Tracked variables can't be pinned, and the encoding takes
        // advantage of that by using the same bit for 'pinned' and 'this'
        // Since we don't track 'this', we should never see either flag here.
        // Check it now before we potentially add some pinned flags.
        for (varPtrDsc* varTmp = gcVarPtrList; varTmp != nullptr; varTmp = varTmp->vpdNext)
        {
            const unsigned flags = varTmp->vpdVarNum & OFFSET_MASK;
            assert((flags & pinned_OFFSET_FLAG) == 0);
            assert((flags & this_OFFSET_FLAG) == 0);
        }
    }
#endif // DEBUG

    // Only need to do this once, and only if we have EH.
    if ((mode == MAKE_REG_PTR_MODE_ASSIGN_SLOTS) && compiler->ehAnyFunclets())
    {
        gcMarkFilterVarsPinned();
    }

    for (varPtrDsc* varTmp = gcVarPtrList; varTmp != nullptr; varTmp = varTmp->vpdNext)
    {
        C_ASSERT((OFFSET_MASK + 1) <= sizeof(int));

        // Get hold of the variable's stack offset.

        unsigned lowBits = varTmp->vpdVarNum & OFFSET_MASK;

        // For negative stack offsets we must reset the low bits
        int varOffs = static_cast<int>(varTmp->vpdVarNum & ~OFFSET_MASK);

        // Compute the actual lifetime offsets.
        unsigned begOffs = varTmp->vpdBegOfs;
        unsigned endOffs = varTmp->vpdEndOfs;

        // Special case: skip any 0-length lifetimes.
        if (endOffs == begOffs)
        {
            continue;
        }

        GcSlotFlags flags = GC_SLOT_BASE;
        if ((lowBits & byref_OFFSET_FLAG) != 0)
        {
            flags = (GcSlotFlags)(flags | GC_SLOT_INTERIOR);
        }

        if ((lowBits & pinned_OFFSET_FLAG) != 0)
        {
            flags = (GcSlotFlags)(flags | GC_SLOT_PINNED);
        }

        GcStackSlotBase stackSlotBase = GC_SP_REL;
        if (compiler->isFramePointerUsed())
        {
            stackSlotBase = GC_FRAMEREG_REL;
        }
        StackSlotIdKey sskey(varOffs, (stackSlotBase == GC_FRAMEREG_REL), flags);
        GcSlotId       varSlotId;
        if (mode == MAKE_REG_PTR_MODE_ASSIGN_SLOTS)
        {
            if (!m_stackSlotMap->Lookup(sskey, &varSlotId))
            {
                varSlotId = gcInfoEncoderWithLog->GetStackSlotId(varOffs, flags, stackSlotBase);
                m_stackSlotMap->Set(sskey, varSlotId);
            }
        }
        else
        {
            bool b = m_stackSlotMap->Lookup(sskey, &varSlotId);
            assert(b); // Should have been added in the first pass.
            // Live from the beginning to the end.
            gcInfoEncoderWithLog->SetSlotState(begOffs, varSlotId, GC_SLOT_LIVE);
            gcInfoEncoderWithLog->SetSlotState(endOffs, varSlotId, GC_SLOT_DEAD);
        }
    }
}

void GCInfo::gcInfoRecordGCStackArgLive(GcInfoEncoder* gcInfoEncoder, MakeRegPtrMode mode, regPtrDsc* genStackPtr)
{
    // On non-x86 platforms, don't have pointer argument push/pop/kill declarations.
    // But we use the same mechanism to record writes into the outgoing argument space...
    assert(genStackPtr->rpdGCtypeGet() != GCT_NONE);
    assert(genStackPtr->rpdArg);
    assert(genStackPtr->rpdArgTypeGet() == rpdARG_PUSH);

    // We only need to report these when we're doing fully-interruptible
    assert(compiler->codeGen->GetInterruptible());

    GCENCODER_WITH_LOGGING(gcInfoEncoderWithLog, gcInfoEncoder);

    StackSlotIdKey sskey(genStackPtr->rpdPtrArg, false,
                         GcSlotFlags(genStackPtr->rpdGCtypeGet() == GCT_BYREF ? GC_SLOT_INTERIOR : GC_SLOT_BASE));
    GcSlotId varSlotId;
    if (mode == MAKE_REG_PTR_MODE_ASSIGN_SLOTS)
    {
        if (!m_stackSlotMap->Lookup(sskey, &varSlotId))
        {
            varSlotId = gcInfoEncoderWithLog->GetStackSlotId(sskey.m_offset, (GcSlotFlags)sskey.m_flags, GC_SP_REL);
            m_stackSlotMap->Set(sskey, varSlotId);
        }
    }
    else
    {
        bool b = m_stackSlotMap->Lookup(sskey, &varSlotId);
        assert(b); // Should have been added in the first pass.
        // Live until the call.
        gcInfoEncoderWithLog->SetSlotState(genStackPtr->rpdOffs, varSlotId, GC_SLOT_LIVE);
    }
}

void GCInfo::gcInfoRecordGCStackArgsDead(GcInfoEncoder* gcInfoEncoder,
                                         unsigned       instrOffset,
                                         regPtrDsc*     genStackPtrFirst,
                                         regPtrDsc*     genStackPtrLast)
{
    // After a call all of the outgoing arguments are marked as dead.
    // The calling loop keeps track of the first argument pushed for this call
    // and passes it in as genStackPtrFirst.
    // genStackPtrLast is the call.
    // Re-walk that list and mark all outgoing arguments that we're marked as live
    // earlier, as going dead after the call.

    // We only need to report these when we're doing fully-interruptible
    assert(compiler->codeGen->GetInterruptible());

    GCENCODER_WITH_LOGGING(gcInfoEncoderWithLog, gcInfoEncoder);

    for (regPtrDsc* genRegPtrTemp = genStackPtrFirst; genRegPtrTemp != genStackPtrLast;
         genRegPtrTemp            = genRegPtrTemp->rpdNext)
    {
        if (!genRegPtrTemp->rpdArg)
        {
            continue;
        }

        assert(genRegPtrTemp->rpdGCtypeGet() != GCT_NONE);
        assert(genRegPtrTemp->rpdArgTypeGet() == rpdARG_PUSH);

        StackSlotIdKey sskey(genRegPtrTemp->rpdPtrArg, false,
                             genRegPtrTemp->rpdGCtypeGet() == GCT_BYREF ? GC_SLOT_INTERIOR : GC_SLOT_BASE);
        GcSlotId varSlotId;
        bool     b = m_stackSlotMap->Lookup(sskey, &varSlotId);
        assert(b); // Should have been added in the first pass.
        // Live until the call.
        gcInfoEncoderWithLog->SetSlotState(instrOffset, varSlotId, GC_SLOT_DEAD);
    }
}

#undef GCENCODER_WITH_LOGGING

#endif // !JIT32_GCENCODER

/*****************************************************************************/
/*****************************************************************************/
