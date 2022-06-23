
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
//
// File: DebuggerModule.cpp
//
// Stuff for tracking DebuggerModules.
//
//*****************************************************************************

#include "stdafx.h"
#include "../inc/common.h"
#include "eeconfig.h" // This is here even for retail & free builds...
#include "vars.hpp"
#include <limits.h>
#include "ilformatter.h"
#include "debuginfostore.h"
#include "../../vm/methoditer.h"

#ifndef DACCESS_COMPILE

bool DbgIsSpecialILOffset(DWORD offset)
{
    LIMITED_METHOD_CONTRACT;

    return (offset == (ULONG) ICorDebugInfo::PROLOG ||
            offset == (ULONG) ICorDebugInfo::EPILOG ||
            offset == (ULONG) ICorDebugInfo::NO_MAPPING);
}

// Helper to use w/ the debug stores.
BYTE* InteropSafeNew(void * , size_t cBytes)
{
    BYTE * p = new (interopsafe, nothrow) BYTE[cBytes];
    return p;
}


//
// This is only fur internal debugging.
//
#ifdef LOGGING
static void _dumpVarNativeInfo(ICorDebugInfo::NativeVarInfo* vni)
{
    WRAPPER_NO_CONTRACT;

    LOG((LF_CORDB, LL_INFO1000000, "Var %02d: 0x%04x-0x%04x vlt=",
            vni->varNumber,
            vni->startOffset, vni->endOffset,
            vni->loc.vlType));

    switch (vni->loc.vlType)
    {
    case ICorDebugInfo::VLT_REG:
        LOG((LF_CORDB, LL_INFO1000000, "REG reg=%d\n", vni->loc.vlReg.vlrReg));
        break;

    case ICorDebugInfo::VLT_REG_BYREF:
        LOG((LF_CORDB, LL_INFO1000000, "REG_BYREF reg=%d\n", vni->loc.vlReg.vlrReg));
        break;

    case ICorDebugInfo::VLT_STK:
        LOG((LF_CORDB, LL_INFO1000000, "STK reg=%d off=0x%04x (%d)\n",
             vni->loc.vlStk.vlsBaseReg,
             vni->loc.vlStk.vlsOffset,
             vni->loc.vlStk.vlsOffset));
        break;

    case ICorDebugInfo::VLT_STK_BYREF:
        LOG((LF_CORDB, LL_INFO1000000, "STK_BYREF reg=%d off=0x%04x (%d)\n",
             vni->loc.vlStk.vlsBaseReg,
             vni->loc.vlStk.vlsOffset,
             vni->loc.vlStk.vlsOffset));
        break;

    case ICorDebugInfo::VLT_REG_REG:
        LOG((LF_CORDB, LL_INFO1000000, "REG_REG reg1=%d reg2=%d\n",
             vni->loc.vlRegReg.vlrrReg1,
             vni->loc.vlRegReg.vlrrReg2));
        break;

    case ICorDebugInfo::VLT_REG_STK:
        LOG((LF_CORDB, LL_INFO1000000, "REG_STK reg=%d basereg=%d off=0x%04x (%d)\n",
             vni->loc.vlRegStk.vlrsReg,
             vni->loc.vlRegStk.vlrsStk.vlrssBaseReg,
             vni->loc.vlRegStk.vlrsStk.vlrssOffset,
             vni->loc.vlRegStk.vlrsStk.vlrssOffset));
        break;

    case ICorDebugInfo::VLT_STK_REG:
        LOG((LF_CORDB, LL_INFO1000000, "STK_REG basereg=%d off=0x%04x (%d) reg=%d\n",
             vni->loc.vlStkReg.vlsrStk.vlsrsBaseReg,
             vni->loc.vlStkReg.vlsrStk.vlsrsOffset,
             vni->loc.vlStkReg.vlsrStk.vlsrsOffset,
             vni->loc.vlStkReg.vlsrReg));
        break;

    case ICorDebugInfo::VLT_STK2:
        LOG((LF_CORDB, LL_INFO1000000, "STK_STK reg=%d off=0x%04x (%d)\n",
             vni->loc.vlStk2.vls2BaseReg,
             vni->loc.vlStk2.vls2Offset,
             vni->loc.vlStk2.vls2Offset));
        break;

    case ICorDebugInfo::VLT_FPSTK:
        LOG((LF_CORDB, LL_INFO1000000, "FPSTK reg=%d\n",
             vni->loc.vlFPstk.vlfReg));
        break;

    case ICorDebugInfo::VLT_FIXED_VA:
        LOG((LF_CORDB, LL_INFO1000000, "FIXED_VA offset=%d (%d)\n",
             vni->loc.vlFixedVarArg.vlfvOffset,
             vni->loc.vlFixedVarArg.vlfvOffset));
        break;


    default:
        LOG((LF_CORDB, LL_INFO1000000, "???\n"));
        break;
    }
}
#endif

#if defined(FEATURE_EH_FUNCLETS)
void DebuggerJitInfo::InitFuncletAddress()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    m_funcletCount = (int)g_pEEInterface->GetFuncletStartOffsets((const BYTE*)m_addrOfCode, NULL, 0);

    if (m_funcletCount == 0)
    {
        _ASSERTE(m_rgFunclet == NULL);
        return;
    }

    m_rgFunclet = (DWORD*)(new (interopsafe, nothrow) DWORD[m_funcletCount]);

    // All bets are off for stepping this method.
    if (m_rgFunclet == NULL)
    {
        m_funcletCount = 0;
        return;
    }

    // This will get the offsets relative to the parent method start as if
    // the funclet was in contiguous memory (i.e. not hot/cold split).
    g_pEEInterface->GetFuncletStartOffsets((const BYTE*)m_addrOfCode, m_rgFunclet, m_funcletCount);
}

//
// DebuggerJitInfo::GetFuncletOffsetByIndex()
//
// Given a funclet index, return its starting offset.
//
// parameters:   index - index of the funclet
//
// return value: starting offset of the specified funclet, or -1 if the index is invalid
//
DWORD DebuggerJitInfo::GetFuncletOffsetByIndex(int index)
{
    LIMITED_METHOD_CONTRACT;

    if (index < 0 || index >= m_funcletCount)
    {
        return (-1);
    }

    return m_rgFunclet[index];
}

//
// DebuggerJitInfo::GetFuncletIndex()
//
// Given an offset or an absolute address, return the index of the funclet containing it.
//
// parameters:   offsetOrAddr - an offset or an absolute address in the method
//               mode         - whether the first argument is an offset or an absolute address
//
// return value: the index of the funclet containing the specified offset or address,
//               or -1 if it's invalid
//
int DebuggerJitInfo::GetFuncletIndex(CORDB_ADDRESS offsetOrAddr, GetFuncletIndexMode mode)
{
    WRAPPER_NO_CONTRACT;

    DWORD offset = 0;
    if (mode == GFIM_BYOFFSET)
    {
        offset = (DWORD)offsetOrAddr;
    }

    // If the address doesn't fall in any of the funclets (or if the
    // method doesn't have any funclet at all), then return PARENT_METHOD_INDEX.
    // <TODO>
    // What if there's an overflow?
    // </TODO>
    if (!m_codeRegionInfo.IsMethodAddress((const BYTE *)(mode == GFIM_BYOFFSET ? (size_t)m_codeRegionInfo.OffsetToAddress(offset) : offsetOrAddr)))
    {
        return PARENT_METHOD_INDEX;
    }

    if ( ( m_funcletCount == 0 )                                   ||
         ( (mode == GFIM_BYOFFSET)  && (offset < m_rgFunclet[0]) ) ||
         ( (mode == GFIM_BYADDRESS) && (offsetOrAddr < (size_t)m_codeRegionInfo.OffsetToAddress(m_rgFunclet[0])) ) )
    {
        return PARENT_METHOD_INDEX;
    }

    for (int i = 0; i < m_funcletCount; i++)
    {
        if (i == (m_funcletCount - 1))
        {
            return i;
        }
        else if ( ( (mode == GFIM_BYOFFSET)  && (offset < m_rgFunclet[i+1]) ) ||
                  ( (mode == GFIM_BYADDRESS) && (offsetOrAddr < (size_t)m_codeRegionInfo.OffsetToAddress(m_rgFunclet[i+1])) ) )
        {
            return i;
        }
    }

    UNREACHABLE();
}

#endif // FEATURE_EH_FUNCLETS

// It is entirely possible that we have multiple sequence points for the
// same IL offset (because of funclets, optimization, etc.).  Just to be
// uniform in all cases, let's return the sequence point with the smallest
// native offset if fWantFirst is TRUE.
#if defined(FEATURE_EH_FUNCLETS)
#define ADJUST_MAP_ENTRY(_map, _wantFirst)                                                        \
    if ((_wantFirst))                                                                             \
        for ( ; (_map) > m_sequenceMap && (((_map)-1)->ilOffset == (_map)->ilOffset); (_map)--);  \
    else                                                                                          \
        for ( ; (_map) < m_sequenceMap + (m_sequenceMapCount-1) && (((_map)+1)->ilOffset == (_map)->ilOffset); (_map)++);
#else
#define ADJUST_MAP_ENTRY(_map, _wantFirst)
#endif // FEATURE_EH_FUNCLETS

DebuggerJitInfo::DebuggerJitInfo(DebuggerMethodInfo *minfo, NativeCodeVersion nativeCodeVersion) :
    m_nativeCodeVersion(nativeCodeVersion),
    m_pLoaderModule(nativeCodeVersion.GetMethodDesc()->GetLoaderModule()),
    m_jitComplete(false),
#ifdef EnC_SUPPORTED
    m_encBreakpointsApplied(false),
#endif //EnC_SUPPORTED
    m_methodInfo(minfo),
    m_addrOfCode(NULL),
    m_sizeOfCode(0), m_prevJitInfo(NULL), m_nextJitInfo(NULL),
    m_lastIL(0),
    m_sequenceMap(NULL),
    m_sequenceMapCount(0),
    m_callsiteMap(NULL),
    m_callsiteMapCount(0),
    m_sequenceMapSorted(false),
    m_varNativeInfo(NULL), m_varNativeInfoCount(0),
    m_fAttemptInit(false)
#if defined(FEATURE_EH_FUNCLETS)
    ,m_rgFunclet(NULL)
    , m_funcletCount(0)
#endif // defined(FEATURE_EH_FUNCLETS)
{
    WRAPPER_NO_CONTRACT;

    // A DJI is just the debugger's cache of interesting information +
    // various debugger-specific state for a method (like Enc).
    // So only be createing DJIs when a debugger is actually attached.
    // The profiler also piggy-backs on the DJIs.
    // @Todo - the managed stackwalker in the BCL also builds on DJIs.
    //_ASSERTE(CORDebuggerAttached() || CORProfilerPresent());

    _ASSERTE(minfo);
    m_encVersion = minfo->GetCurrentEnCVersion();
    _ASSERTE(m_encVersion >= CorDB_DEFAULT_ENC_FUNCTION_VERSION);
    LOG((LF_CORDB,LL_EVERYTHING, "DJI::DJI : created at 0x%p\n", this));

    // Debugger doesn't track LightWeight codegen methods.
    // We should never even be creating a DJI for one.
    _ASSERTE(!m_nativeCodeVersion.GetMethodDesc()->IsDynamicMethod());
}

DebuggerILToNativeMap *DebuggerJitInfo::MapILOffsetToMapEntry(SIZE_T offset, BOOL *exact, BOOL fWantFirst)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CAN_TAKE_LOCK;  // GetSequenceMapCount calls LazyInitBounds() which can eventually
                        // call ExecutionManager::IncrementReader
    }
    CONTRACTL_END;

    // Ideally we should be able to assert this, since the binary search in this function
    // assumes that the sequence points are sorted by IL offset (NO_MAPPING, PROLOG, and EPILOG
    // are actually -1, -2, and -3, respectively).  However, the sequence points in pdb's use
    // -1 to mean "end of the method", which is different from our semantics of using 0.
    // _ASSERTE(offset != NO_MAPPING && offset != PROLOG && offset != EPILOG);

    //
    // Binary search for matching map element.
    //

    DebuggerILToNativeMap *mMin = GetSequenceMap();
    DebuggerILToNativeMap *mMax = mMin + GetSequenceMapCount();

    _ASSERTE(m_sequenceMapSorted);
    _ASSERTE( mMin < mMax ); //otherwise we have no code

    if (exact)
    {
        *exact = FALSE;
    }

    if (mMin)
    {
        while (mMin + 1 < mMax)
        {
            _ASSERTE(mMin>=m_sequenceMap);
            DebuggerILToNativeMap *mMid = mMin + ((mMax - mMin)>>1);
            _ASSERTE(mMid>=m_sequenceMap);

            if (offset == mMid->ilOffset)
            {
                if (exact)
                {
                    *exact = TRUE;
                }
                ADJUST_MAP_ENTRY(mMid, fWantFirst);
                return mMid;
            }
            else if (offset < mMid->ilOffset && mMid->ilOffset != (ULONG) ICorDebugInfo::PROLOG)
            {
                mMax = mMid;
            }
            else
            {
                mMin = mMid;
            }
        }

        if (exact && offset == mMin->ilOffset)
        {
            *exact = TRUE;
        }
        ADJUST_MAP_ENTRY(mMin, fWantFirst);
    }
    return mMin;
}

void DebuggerJitInfo::InitILToNativeOffsetIterator(ILToNativeOffsetIterator &iterator, SIZE_T ilOffset)
{
    WRAPPER_NO_CONTRACT;

    iterator.Init(this, ilOffset);
}


DebuggerJitInfo::NativeOffset DebuggerJitInfo::MapILOffsetToNative(DebuggerJitInfo::ILOffset ilOffset)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    NativeOffset resultOffset;

    DebuggerILToNativeMap *map = MapILOffsetToMapEntry(ilOffset.m_ilOffset, &(resultOffset.m_fExact));

#if defined(FEATURE_EH_FUNCLETS)
    // See if we want the map entry for the parent.
    if (ilOffset.m_funcletIndex <= PARENT_METHOD_INDEX)
    {
#endif // FEATURE_EH_FUNCLETS
        PREFIX_ASSUME( map != NULL );
        LOG((LF_CORDB, LL_INFO10000, "DJI::MILOTN: ilOff 0x%x to nat 0x%x exact:0x%x (Entry IL Off:0x%x)\n",
             ilOffset.m_ilOffset, map->nativeStartOffset, resultOffset.m_fExact, map->ilOffset));

        resultOffset.m_nativeOffset = map->nativeStartOffset;

#if defined(FEATURE_EH_FUNCLETS)
    }
    else
    {
        // funcletIndex is guaranteed to be >= 0 at this point.
        if (ilOffset.m_funcletIndex > (m_funcletCount - 1))
        {
            resultOffset.m_fExact       = FALSE;
            resultOffset.m_nativeOffset = ((SIZE_T)-1);
        }
        else
        {
            // Initialize the funclet range.
            // ASSUMES that funclets are contiguous which they currently are...
            DWORD funcletStartOffset = GetFuncletOffsetByIndex(ilOffset.m_funcletIndex);
            DWORD funcletEndOffset;
            if (ilOffset.m_funcletIndex < (m_funcletCount - 1))
            {
                funcletEndOffset = GetFuncletOffsetByIndex(ilOffset.m_funcletIndex + 1);
            }
            else
            {
                funcletEndOffset = (DWORD)m_sizeOfCode;
            }

            SIZE_T ilTargetOffset = map->ilOffset;

            DebuggerILToNativeMap *mapEnd = GetSequenceMap() + GetSequenceMapCount();

            for (; map < mapEnd && map->ilOffset == ilTargetOffset; map++)
            {
                if ((map->nativeStartOffset >= funcletStartOffset) &&
                    (map->nativeStartOffset < funcletEndOffset))
                {
                    // This is the normal case where the start offset falls in
                    // the range of the funclet.
                    resultOffset.m_nativeOffset = map->nativeStartOffset;
                    break;
                }
            }

            if (map == mapEnd || map->ilOffset != ilTargetOffset)
            {
                resultOffset.m_fExact       = FALSE;
                resultOffset.m_nativeOffset = ((SIZE_T)-1);
            }
        }
    }
#endif // FEATURE_EH_FUNCLETS

    return resultOffset;
}


DebuggerJitInfo::ILToNativeOffsetIterator::ILToNativeOffsetIterator()
{
    LIMITED_METHOD_CONTRACT;

    m_dji = NULL;
    m_currentILOffset.m_ilOffset = INVALID_IL_OFFSET;
#ifdef FEATURE_EH_FUNCLETS
    m_currentILOffset.m_funcletIndex = PARENT_METHOD_INDEX;
#endif
}

void DebuggerJitInfo::ILToNativeOffsetIterator::Init(DebuggerJitInfo* dji, SIZE_T ilOffset)
{
    WRAPPER_NO_CONTRACT;

    m_dji = dji;
    m_currentILOffset.m_ilOffset = ilOffset;
#ifdef FEATURE_EH_FUNCLETS
    m_currentILOffset.m_funcletIndex = PARENT_METHOD_INDEX;
#endif

    m_currentNativeOffset = m_dji->MapILOffsetToNative(m_currentILOffset);
}

bool DebuggerJitInfo::ILToNativeOffsetIterator::IsAtEnd()
{
    LIMITED_METHOD_CONTRACT;

    return (m_currentILOffset.m_ilOffset == INVALID_IL_OFFSET);
}

SIZE_T DebuggerJitInfo::ILToNativeOffsetIterator::Current(BOOL* pfExact)
{
    LIMITED_METHOD_CONTRACT;

    if (pfExact != NULL)
    {
        *pfExact = m_currentNativeOffset.m_fExact;
    }
    return m_currentNativeOffset.m_nativeOffset;
}

SIZE_T DebuggerJitInfo::ILToNativeOffsetIterator::CurrentAssertOnlyOne(BOOL* pfExact)
{
    WRAPPER_NO_CONTRACT;

    SIZE_T nativeOffset = Current(pfExact);

    Next();
    _ASSERTE(IsAtEnd());

    return nativeOffset;
}

void DebuggerJitInfo::ILToNativeOffsetIterator::Next()
{
#if defined(FEATURE_EH_FUNCLETS)
    NativeOffset tmpNativeOffset;

    for (m_currentILOffset.m_funcletIndex += 1;
         m_currentILOffset.m_funcletIndex < m_dji->GetFuncletCount();
         m_currentILOffset.m_funcletIndex++)
    {
        tmpNativeOffset = m_dji->MapILOffsetToNative(m_currentILOffset);
        if (tmpNativeOffset.m_nativeOffset != ((SIZE_T)-1) &&
            tmpNativeOffset.m_nativeOffset != m_currentNativeOffset.m_nativeOffset)
        {
            m_currentNativeOffset = tmpNativeOffset;
            break;
        }
    }

    if (m_currentILOffset.m_funcletIndex == m_dji->GetFuncletCount())
    {
        m_currentILOffset.m_ilOffset = INVALID_IL_OFFSET;
    }
#else  // !FEATURE_EH_FUNCLETS
    m_currentILOffset.m_ilOffset = INVALID_IL_OFFSET;
#endif // !FEATURE_EH_FUNCLETS
}



// SIZE_T DebuggerJitInfo::MapSpecialToNative():  Maps something like
//      a prolog to a native offset.
// CordDebugMappingResult mapping:  Mapping type to be looking for.
// SIZE_T which:  Which one.  <TODO>For now, set to zero.  <@todo Later, we'll
//      change this to some value that we get back from MapNativeToILOffset
//      to indicate which of the (possibly multiple epilogs) that may
//      be present.</TODO>

SIZE_T DebuggerJitInfo::MapSpecialToNative(CorDebugMappingResult mapping,
                                           SIZE_T which,
                                           BOOL *pfAccurate)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(NULL != pfAccurate);
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO10000, "DJI::MSTN map:0x%x which:0x%x\n", mapping, which));

    bool fFound;
    SIZE_T  cFound = 0;

    DebuggerILToNativeMap *m = GetSequenceMap();
    DebuggerILToNativeMap *mEnd = m + GetSequenceMapCount();
    if (m)
    {
        while(m < mEnd)
        {
            _ASSERTE(m>=GetSequenceMap());

            fFound = false;

            if (DbgIsSpecialILOffset(m->ilOffset))
                cFound++;

            if (cFound == which)
            {
                _ASSERTE( (mapping == MAPPING_PROLOG &&
                    m->ilOffset == (ULONG) ICorDebugInfo::PROLOG) ||
                          (mapping == MAPPING_EPILOG &&
                    m->ilOffset == (ULONG) ICorDebugInfo::EPILOG) ||
                          ((mapping == MAPPING_NO_INFO || mapping == MAPPING_UNMAPPED_ADDRESS) &&
                    m->ilOffset == (ULONG) ICorDebugInfo::NO_MAPPING)
                        );

                (*pfAccurate) = TRUE;
                LOG((LF_CORDB, LL_INFO10000, "DJI::MSTN found mapping to nat:0x%x\n",
                    m->nativeStartOffset));
                return m->nativeStartOffset;
            }
            m++;
        }
    }

    LOG((LF_CORDB, LL_INFO10000, "DJI::MSTN No mapping found :(\n"));
    (*pfAccurate) = FALSE;

    return 0;
}

#if defined(FEATURE_EH_FUNCLETS)
//
// DebuggerJitInfo::MapILOffsetToNativeForSetIP()
//
// This function maps an IL offset to a native offset, taking into account cloned finallys and nested EH clauses.
//
// parameters:   offsetILTo         - the destination IP, in IL offset
//               funcletIndexFrom   - the funclet index of the source IP
//               pEHRT              - tree structure for keeping track of EH clause information
//               pExact             - pointer for returning whether the mapping is exact or not
//
// return value: destination IP, in native offset
//
SIZE_T DebuggerJitInfo::MapILOffsetToNativeForSetIP(SIZE_T offsetILTo, int funcletIndexFrom,
                                                    EHRangeTree* pEHRT, BOOL* pExact)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    DebuggerILToNativeMap* pMap    = MapILOffsetToMapEntry(offsetILTo, pExact, TRUE);
    DebuggerILToNativeMap* pMapEnd = GetSequenceMap() + GetSequenceMapCount();

    _ASSERTE(pMap == m_sequenceMap ||
             (pMap - 1)->ilOffset == (ULONG)ICorDebugInfo::NO_MAPPING ||
             (pMap - 1)->ilOffset == (ULONG)ICorDebugInfo::PROLOG ||
             (pMap - 1)->ilOffset == (ULONG)ICorDebugInfo::EPILOG ||
             pMap->ilOffset > (pMap - 1)->ilOffset);

    SIZE_T offsetNatTo = pMap->nativeStartOffset;

    if (m_funcletCount == 0 ||
        pEHRT == NULL       ||
        FAILED(pEHRT->m_hrInit))
    {
        return offsetNatTo;
    }

    // Multiple sequence points may have the same IL offset, which means that the code is duplicated in
    // multiple funclets and/or in the parent method.  If the destination offset maps to multiple sequence
    // points (and hence to multiple funclets), we try to find the a sequence point which is in the same
    // funclet as the source sequence point.  If we can't find one, then the operation is going to fail
    // anyway, so we just return the first sequence point we find.
    for (DebuggerILToNativeMap* pMapCur = pMap + 1;
        (pMapCur < pMapEnd) && (pMapCur->ilOffset == pMap->ilOffset);
        pMapCur++)
    {
        int funcletIndexTo = GetFuncletIndex(pMapCur->nativeStartOffset, DebuggerJitInfo::GFIM_BYOFFSET);
        if (funcletIndexFrom == funcletIndexTo)
        {
            return pMapCur->nativeStartOffset;
        }
    }

    return offsetNatTo;
}
#endif // FEATURE_EH_FUNCLETS

// void DebuggerJitInfo::MapILRangeToMapEntryRange():   MIRTMER
// calls MapILOffsetToNative for the startOffset (putting the
// result into start), and the endOffset (putting the result into end).
// SIZE_T startOffset:  IL offset from beginning of function.
// SIZE_T endOffset:  IL offset from beginngin of function,
// or zero to indicate that the end of the function should be used.
// DebuggerILToNativeMap **start:  Contains start & end
// native offsets that correspond to startOffset.  Set to NULL if
// there is no mapping info.
// DebuggerILToNativeMap **end:  Contains start & end native
// offsets that correspond to endOffset. Set to NULL if there
// is no mapping info.
void DebuggerJitInfo::MapILRangeToMapEntryRange(SIZE_T startOffset,
                                                SIZE_T endOffset,
                                                DebuggerILToNativeMap **start,
                                                DebuggerILToNativeMap **end)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO1000000,
         "DJI::MIRTMER: IL 0x%04x-0x%04x\n",
         startOffset, endOffset));

    if (GetSequenceMapCount() == 0)
    {
        *start = NULL;
        *end = NULL;
        return;
    }

    *start = MapILOffsetToMapEntry(startOffset);

    //
    // end points to the last range that endOffset maps to, not past
    // the last range.
    // We want to return the last IL, and exclude the epilog
    if (endOffset == 0)
    {
        *end = GetSequenceMap() + GetSequenceMapCount() - 1;
        _ASSERTE(*end>=m_sequenceMap);

        while ( ((*end)->ilOffset == (ULONG) ICorDebugInfo::EPILOG||
                (*end)->ilOffset == (ULONG) ICorDebugInfo::NO_MAPPING)
               && (*end) > m_sequenceMap)
        {
            (*end)--;
            _ASSERTE(*end>=m_sequenceMap);

        }
    }
    else
        *end = MapILOffsetToMapEntry(endOffset - 1, NULL
                                     BIT64_ARG(FALSE));

    _ASSERTE(*end>=m_sequenceMap);


    LOG((LF_CORDB, LL_INFO1000000,
         "DJI::MIRTMER: IL 0x%04x-0x%04x --> 0x%04x 0x%08x-0x%08x\n"
         "                               --> 0x%04x 0x%08x-0x%08x\n",
         startOffset, endOffset,
         (*start)->ilOffset,
         (*start)->nativeStartOffset, (*start)->nativeEndOffset,
         (*end)->ilOffset,
         (*end)->nativeStartOffset, (*end)->nativeEndOffset));
}

// @dbgtodo Microsoft inspection: This function has been replicated in DacDbiStructures so
// this version can be deleted when inspection is complete.

// DWORD DebuggerJitInfo::MapNativeOffsetToIL():   Given a native
//  offset for the DebuggerJitInfo, compute
//  the IL offset from the beginning of the same method.
// Returns: Offset of the IL instruction that contains
//  the native offset,
// SIZE_T nativeOffset:  [IN] Native Offset
// CorDebugMappingResult *map:  [OUT] explains the
//  quality of the matching & special cases
// SIZE_T which:  It's possible to have multiple EPILOGs, or
//  multiple unmapped regions within a method.  This opaque value
//  specifies which special region we're talking about.  This
//  param has no meaning if map & (MAPPING_EXACT|MAPPING_APPROXIMATE)
//  Basically, this gets handed back to MapSpecialToNative, later.
DWORD DebuggerJitInfo::MapNativeOffsetToIL(SIZE_T nativeOffsetToMap,
                                            CorDebugMappingResult *map,
                                            DWORD *which,
                                            BOOL skipPrologs)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(map != NULL);
        PRECONDITION(which != NULL);
    }
    CONTRACTL_END;

    DWORD nativeOffset = (DWORD)nativeOffsetToMap;

    (*which) = 0;
    DebuggerILToNativeMap *m = GetSequenceMap();
    DebuggerILToNativeMap *mEnd = m + GetSequenceMapCount();

    LOG((LF_CORDB,LL_INFO10000,"DJI::MNOTI: nativeOffset = 0x%x\n", nativeOffset));

    if (m)
    {
        while (m < mEnd)
        {
            _ASSERTE(m>=m_sequenceMap);

#ifdef LOGGING
            if (m->ilOffset == (ULONG) ICorDebugInfo::PROLOG )
                LOG((LF_CORDB,LL_INFO10000,"DJI::MNOTI: m->natStart:0x%x m->natEnd:0x%x il:PROLOG\n", m->nativeStartOffset, m->nativeEndOffset));
            else if (m->ilOffset == (ULONG) ICorDebugInfo::EPILOG )
                LOG((LF_CORDB,LL_INFO10000,"DJI::MNOTI: m->natStart:0x%x m->natEnd:0x%x il:EPILOG\n", m->nativeStartOffset, m->nativeEndOffset));
            else if (m->ilOffset == (ULONG) ICorDebugInfo::NO_MAPPING)
                LOG((LF_CORDB,LL_INFO10000,"DJI::MNOTI: m->natStart:0x%x m->natEnd:0x%x il:NO MAP\n", m->nativeStartOffset, m->nativeEndOffset));
            else
                LOG((LF_CORDB,LL_INFO10000,"DJI::MNOTI: m->natStart:0x%x m->natEnd:0x%x il:0x%x src:0x%x\n", m->nativeStartOffset, m->nativeEndOffset, m->ilOffset, m->source));
#endif // LOGGING

            if (m->ilOffset == (ULONG) ICorDebugInfo::PROLOG ||
                m->ilOffset == (ULONG) ICorDebugInfo::EPILOG ||
                m->ilOffset == (ULONG) ICorDebugInfo::NO_MAPPING)
            {
                (*which)++;
            }

            if (nativeOffset >= m->nativeStartOffset
                && ((m->nativeEndOffset == 0 &&
                    m->ilOffset != (ULONG) ICorDebugInfo::PROLOG)
                     || nativeOffset < m->nativeEndOffset))
            {
                ULONG ilOff = m->ilOffset;

                if( m->ilOffset == (ULONG) ICorDebugInfo::PROLOG )
                {
                    if (skipPrologs && nativeOffset < m->nativeEndOffset)
                    {
                        // If the caller requested to skip prologs, we simply restart the walk
                        // with the offset set to the end of the prolog.
                        nativeOffset = m->nativeEndOffset;
                        continue;
                    }

                    ilOff = 0;
                    (*map) = MAPPING_PROLOG;
                    LOG((LF_CORDB,LL_INFO10000,"DJI::MNOTI: MAPPING_PROLOG\n"));

                }
                else if (m->ilOffset == (ULONG) ICorDebugInfo::NO_MAPPING)
                {
                    ilOff = 0;
                    (*map) = MAPPING_UNMAPPED_ADDRESS ;
                    LOG((LF_CORDB,LL_INFO10000,"DJI::MNOTI:MAPPING_"
                        "UNMAPPED_ADDRESS\n"));
                }
                else if( m->ilOffset == (ULONG) ICorDebugInfo::EPILOG )
                {
                    ilOff = m_lastIL;
                    (*map) = MAPPING_EPILOG;
                    LOG((LF_CORDB,LL_INFO10000,"DJI::MNOTI:MAPPING_EPILOG\n"));
                }
                else if (nativeOffset == m->nativeStartOffset)
                {
                    (*map) = MAPPING_EXACT;
                    LOG((LF_CORDB,LL_INFO10000,"DJI::MNOTI:MAPPING_EXACT\n"));
                }
                else
                {
                    (*map) = MAPPING_APPROXIMATE;
                    LOG((LF_CORDB,LL_INFO10000,"DJI::MNOTI:MAPPING_"
                        "APPROXIMATE\n"));
                }

                return ilOff;
            }
            m++;
        }
    }

    (*map) = MAPPING_NO_INFO;
    LOG((LF_CORDB,LL_INFO10000,"DJI::MNOTI:NO_INFO\n"));
    return 0;
}

/******************************************************************************
 *
 ******************************************************************************/
DebuggerJitInfo::~DebuggerJitInfo()
{
    TRACE_FREE(m_sequenceMap);
    if (m_sequenceMap != NULL)
    {
        DeleteInteropSafe(((BYTE *)m_sequenceMap));
    }

    TRACE_FREE(m_varNativeInfo);
    if (m_varNativeInfo != NULL)
    {
        DeleteInteropSafe(m_varNativeInfo);
    }

#if defined(FEATURE_EH_FUNCLETS)
    if (m_rgFunclet)
    {
        DeleteInteropSafe(m_rgFunclet);
        m_rgFunclet = NULL;
    }
#endif // FEATURE_EH_FUNCLETS


#ifdef _DEBUG
    // Trash pointers to garbage.
    // Don't null out since there may be runtime checks against NULL.
    // Set to a non-null random pointer value that will cause an immediate AV on deref.
    m_methodInfo = (DebuggerMethodInfo*) 0x1;
    m_prevJitInfo = (DebuggerJitInfo*) 0x01;
    m_nextJitInfo = (DebuggerJitInfo*) 0x01;
#endif


    LOG((LF_CORDB,LL_EVERYTHING, "DJI::~DJI : deleted at 0x%p\n", this));
}

// Lazy initialize the Debugger-Jit-Info
void DebuggerJitInfo::LazyInitBounds()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(ThisMaybeHelperThread());
        PRECONDITION(!g_pDebugger->HasDebuggerDataLock());
    } CONTRACTL_END;

    LOG((LF_CORDB, LL_EVERYTHING, "DJI::LazyInitBounds: this=0x%p m_fAttemptInit %s\n", this, m_fAttemptInit == true ? "true": "false"));

    // Only attempt lazy-init once
    if (m_fAttemptInit)
    {
        return;
    }

    EX_TRY
    {
        LOG((LF_CORDB, LL_EVERYTHING, "DJI::LazyInitBounds: this=0x%p Initing\n", this));

        // Should have already been jitted
        _ASSERTE(this->m_jitComplete);

        MethodDesc * mdesc = this->m_nativeCodeVersion.GetMethodDesc();
        DebugInfoRequest request;

        _ASSERTE(this->m_addrOfCode != NULL); // must have address to disambguate the Enc cases.
        // Caller already resolved generics when they craeted the DJI, so we don't need to repeat.
        // Note the MethodDesc may not yet have the jitted info, so we'll also use the starting address we got in the jit complete callback.
        request.InitFromStartingAddr(mdesc, (PCODE)this->m_addrOfCode);

        // Bounds info.
        ULONG32 cMap = 0;
        ICorDebugInfo::OffsetMapping *pMap = NULL;
        ULONG32 cVars = 0;
        ICorDebugInfo::NativeVarInfo *pVars = NULL;

        BOOL fSuccess = DebugInfoManager::GetBoundariesAndVars(
            request,
            InteropSafeNew, NULL, // allocator
            &cMap, &pMap,
            &cVars, &pVars);

        LOG((LF_CORDB,LL_EVERYTHING, "DJI::LazyInitBounds: this=0x%p GetBoundariesAndVars success=0x%x\n", this, fSuccess));

        // SetBoundaries uses the CodeVersionManager, need to take it now for lock ordering reasons
        CodeVersionManager::LockHolder codeVersioningLockHolder;
        Debugger::DebuggerDataLockHolder debuggerDataLockHolder(g_pDebugger);

        if (!m_fAttemptInit)
        {
            if (fSuccess)
            {
                this->SetBoundaries(cMap, pMap);
                this->SetVars(cVars, pVars);
            }
            m_fAttemptInit = true;
        }
        else
        {
            DeleteInteropSafe(pMap);
            DeleteInteropSafe(pVars);
        }
        // DebuggerDataLockHolder out of scope - release implied
    }
    EX_CATCH
    {
        LOG((LF_CORDB,LL_WARNING, "DJI::LazyInitBounds: this=0x%x Exception was thrown and caught\n", this));
        // Just catch the exception. The DJI maps may or may-not be intialized,
        // but they should still be in a consistent state, so we should be ok.
    }
    EX_END_CATCH(SwallowAllExceptions)
}

/******************************************************************************
 * SetVars() takes ownership of pVars
 ******************************************************************************/
void DebuggerJitInfo::SetVars(ULONG32 cVars, ICorDebugInfo::NativeVarInfo *pVars)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(m_varNativeInfo == NULL);

    m_varNativeInfo = pVars;
    m_varNativeInfoCount = cVars;

    LOG((LF_CORDB, LL_INFO1000000, "D::sV: var count is %d\n",
         m_varNativeInfoCount));

#ifdef LOGGING
    for (unsigned int i = 0; i < m_varNativeInfoCount; i++)
    {
        ICorDebugInfo::NativeVarInfo* vni = &(m_varNativeInfo[i]);
        _dumpVarNativeInfo(vni);
    }
#endif
}

CHECK DebuggerJitInfo::Check() const
{
    LIMITED_METHOD_CONTRACT;

    CHECK_OK;
}

// Invariants for a DebuggerJitInfo
// These should always be true at any well defined point.
CHECK DebuggerJitInfo::Invariant() const
{
    LIMITED_METHOD_CONTRACT;
    CHECK((m_sequenceMapCount == 0) == (m_sequenceMap == NULL));
    CHECK(m_methodInfo != NULL);
    CHECK(m_nativeCodeVersion.GetMethodDesc() != NULL);

    CHECK_OK;
}


#if !defined(DACCESS_COMPILE)
/******************************************************************************
 * SetBoundaries() takes ownership of pMap
 ******************************************************************************/
void DebuggerJitInfo::SetBoundaries(ULONG32 cMap, ICorDebugInfo::OffsetMapping *pMap)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
    }
    CONTRACTL_END;

    LOG((LF_CORDB,LL_EVERYTHING, "DJI::SetBoundaries: this=0x%p cMap=0x%x pMap=0x%p\n", this, cMap, pMap));
    _ASSERTE((cMap == 0) == (pMap == NULL));
    _ASSERTE(m_sequenceMap == NULL);

    if (cMap == 0)
        return;

    ULONG ilLast = 0;
#ifdef _DEBUG
    // We assume that the map is sorted by native offset
    if (cMap > 1)
    {
        for(ICorDebugInfo::OffsetMapping * pEntry = pMap;
            pEntry < (pMap + cMap - 1);
            pEntry++)
        {
            _ASSERTE(pEntry->nativeOffset <= (pEntry+1)->nativeOffset);
        }
    }
#endif //_DEBUG

    //
    // <TODO>@todo perf: allocating these on the heap is slow. We could do
    // better knowing that these live for the life of the run, just
    // like the DebuggerJitInfo's.</TODO>
    //
    m_sequenceMap = (DebuggerILToNativeMap *)new (interopsafe) DebuggerILToNativeMap[cMap];
    LOG((LF_CORDB,LL_EVERYTHING, "DJI::SetBoundaries: this=0x%p m_sequenceMap=0x%x\n", this, m_sequenceMap));
    _ASSERTE(m_sequenceMap != NULL); // we'll throw on null

    m_sequenceMapCount = cMap;

    DebuggerILToNativeMap *m = m_sequenceMap;

    // For the instrumented-IL case, we need to remove all duplicate entries.
    // So we keep a record of the last old IL offset. If the current old IL
    // offset is the same as the last old IL offset, we remove it.
    // Pick a unique initial value (-10) so that the 1st doesn't accidentally match.
    int ilPrevOld = -10;

    _ASSERTE(CodeVersionManager::IsLockOwnedByCurrentThread());

    InstrumentedILOffsetMapping mapping;

    ILCodeVersion ilVersion = m_nativeCodeVersion.GetILCodeVersion();
    if (!ilVersion.IsDefaultVersion())
    {
        // Did the current rejit provide a map?
        const InstrumentedILOffsetMapping *pReJitMap = ilVersion.GetInstrumentedILMap();
        if (pReJitMap != NULL)
        {
            mapping = *pReJitMap;
        }
    }
    else if (m_methodInfo->HasInstrumentedILMap())
    {
        // If a ReJIT hasn't happened, check for a profiler provided map.
        mapping = m_methodInfo->GetRuntimeModule()->GetInstrumentedILOffsetMapping(m_methodInfo->m_token);
    }


    //
    // <TODO>@todo perf: we could do the vast majority of this
    // post-processing work the first time the sequence point map is
    // demanded. That would allow us to simply hold the raw array for
    // 95% of the functions jitted while debugging, and 100% of them
    // when just running/tracking.</TODO>
    const DWORD call_inst = (DWORD)ICorDebugInfo::CALL_INSTRUCTION;
    for(ULONG32 idxJitMap = 0; idxJitMap < cMap; idxJitMap++)
    {
        const ICorDebugInfo::OffsetMapping * const pMapEntry = &pMap[idxJitMap];
        _ASSERTE(m >= m_sequenceMap);
        _ASSERTE(m < m_sequenceMap + m_sequenceMapCount);

        ilLast = max((int)ilLast, (int)pMapEntry->ilOffset);

        // Simply copy everything over, since we translate to
        // CorDebugMappingResults immediately prior to handing
        // back to user...
        m->nativeStartOffset    = pMapEntry->nativeOffset;
        m->ilOffset             = pMapEntry->ilOffset;
        m->source               = pMapEntry->source;

        // Keep in mind that if we have an instrumented code translation
        // table, we may have asked for completely different IL offsets
        // than the user thinks we did.....

        // If we did instrument, then we can't have any sequence points that
        // are "in-between" the old-->new map that the profiler gave us.
        // Ex, if map is:
        // (6 old -> 36 new)
        // (8 old -> 50 new)
        // And the jit gives us an entry for 44 new, that will map back to 6 old.
        // Since the map can only have one entry for 6 old, we remove 44 new.
        if (!mapping.IsNull())
        {
            int ilThisOld = m_methodInfo->TranslateToInstIL(&mapping,
                                                            pMapEntry->ilOffset,
                                                            bInstrumentedToOriginal);

            if (ilThisOld == ilPrevOld)
            {
                // If this translated to the same old IL offset as the last entry,
                // then this is "in between". Skip it.
                m_sequenceMapCount--; // one less seq point in the DJI's map
                continue;
            }
            m->ilOffset = ilThisOld;
            ilPrevOld = ilThisOld;
        }

        if (m > m_sequenceMap && (m->source & call_inst) != call_inst)
        {
            DebuggerILToNativeMap *last = m-1;
            if ((last->source & call_inst) == call_inst)
                last = (last > m_sequenceMap) ? last - 1 : NULL;

            if (last && (last->source & call_inst) != call_inst && m->ilOffset == last->ilOffset)
            {
                // JIT gave us an extra entry (probably zero), so mush
                // it into the one we've already got.
                // <TODO> Why does this happen?</TODO>
                m_sequenceMapCount--;
                continue;
            }
        }


        // Move to next entry in the debugger's table
        m++;
    } // end for

    DeleteInteropSafe(pMap);

    _ASSERTE(m == m_sequenceMap + m_sequenceMapCount);

    m_lastIL = ilLast;

    // Set nativeEndOffset in debugger's il->native map
    // Do this before we resort by IL.
    unsigned int i;
    for(i = 0; i < m_sequenceMapCount - 1; i++)
    {
        // We need to not use CALL_INSTRUCTION's IL start offset.
        unsigned int j = i + 1;
        while ((m_sequenceMap[j].source & call_inst) == call_inst && j < m_sequenceMapCount-1)
            j++;

        m_sequenceMap[i].nativeEndOffset = m_sequenceMap[j].nativeStartOffset;
    }

    m_sequenceMap[i].nativeEndOffset = 0;
    m_sequenceMap[i].source = (ICorDebugInfo::SourceTypes)
                ((DWORD) m_sequenceMap[i].source |
                (DWORD)ICorDebugInfo::NATIVE_END_OFFSET_UNKNOWN);

    // Now resort by IL.
    MapSortIL isort(m_sequenceMap, m_sequenceMapCount);

    isort.Sort();

    m_sequenceMapSorted = true;

    m_callsiteMapCount = m_sequenceMapCount;
    while (m_sequenceMapCount > 0 && (m_sequenceMap[m_sequenceMapCount-1].source & call_inst) == call_inst)
      m_sequenceMapCount--;

    m_callsiteMap = m_sequenceMap + m_sequenceMapCount;
    m_callsiteMapCount -= m_sequenceMapCount;

    LOG((LF_CORDB, LL_INFO100000, "DJI::SetBoundaries: this=0x%p boundary count is %d (%d callsites)\n",
         this, m_sequenceMapCount, m_callsiteMapCount));

#ifdef LOGGING
    for (unsigned int count = 0; count < m_sequenceMapCount + m_callsiteMapCount; count++)
    {
        if( m_sequenceMap[count].ilOffset ==
            (ULONG) ICorDebugInfo::PROLOG )
            LOG((LF_CORDB, LL_INFO1000000,
                 "D::sB: PROLOG               --> 0x%08x -- 0x%08x",
                 m_sequenceMap[count].nativeStartOffset,
                 m_sequenceMap[count].nativeEndOffset));
        else if ( m_sequenceMap[count].ilOffset ==
                  (ULONG) ICorDebugInfo::EPILOG )
            LOG((LF_CORDB, LL_INFO1000000,
                 "D::sB: EPILOG              --> 0x%08x -- 0x%08x",
                 m_sequenceMap[count].nativeStartOffset,
                 m_sequenceMap[count].nativeEndOffset));
        else if ( m_sequenceMap[count].ilOffset ==
                  (ULONG) ICorDebugInfo::NO_MAPPING )
            LOG((LF_CORDB, LL_INFO1000000,
                 "D::sB: NO MAP              --> 0x%08x -- 0x%08x",
                 m_sequenceMap[count].nativeStartOffset,
                 m_sequenceMap[count].nativeEndOffset));
        else
            LOG((LF_CORDB, LL_INFO1000000,
                 "D::sB: 0x%04x (Real:0x%04x) --> 0x%08x -- 0x%08x",
                 m_sequenceMap[count].ilOffset,
                 m_methodInfo->TranslateToInstIL(&mapping,
                                                 m_sequenceMap[count].ilOffset,
                                                 bOriginalToInstrumented),
                 m_sequenceMap[count].nativeStartOffset,
                 m_sequenceMap[count].nativeEndOffset));

        LOG((LF_CORDB, LL_INFO1000000, " Src:0x%x\n", m_sequenceMap[count].source));

    }
#endif //LOGGING
}
#endif // !DACCESS_COMPILE

// Init a DJI after it's jitted.
void DebuggerJitInfo::Init(TADDR newAddress)
{
    // Shouldn't initialize while holding the lock b/c intialzing may call functions that lock,
    // and thus we'd have a locking violation.
    _ASSERTE(!g_pDebugger->HasDebuggerDataLock());

    this->m_addrOfCode = (ULONG_PTR)PTR_TO_CORDB_ADDRESS((BYTE*) newAddress);
    this->m_jitComplete = true;

    this->m_codeRegionInfo.InitializeFromStartAddress(PINSTRToPCODE((TADDR)this->m_addrOfCode));
    this->m_sizeOfCode =  this->m_codeRegionInfo.getSizeOfTotalCode();

    this->m_encVersion = this->m_methodInfo->GetCurrentEnCVersion();

#if defined(FEATURE_EH_FUNCLETS)
    this->InitFuncletAddress();
#endif // FEATURE_EH_FUNCLETS

    LOG((LF_CORDB,LL_INFO10000,"De::JITCo:Got DJI 0x%p(V %d),"
         "Hot section from 0x%p to 0x%p "
         "Cold section from 0x%p to 0x%p "
         "varCount=%d  seqCount=%d\n",
         this, this->m_encVersion,
         this->m_codeRegionInfo.getAddrOfHotCode(),
         this->m_codeRegionInfo.getAddrOfHotCode() + this->m_codeRegionInfo.getSizeOfHotCode(),
         this->m_codeRegionInfo.getAddrOfColdCode(),
         this->m_codeRegionInfo.getAddrOfColdCode() + this->m_codeRegionInfo.getSizeOfColdCode(),
         (ULONG)this->m_addrOfCode,
         (ULONG)this->m_addrOfCode+(ULONG)this->m_sizeOfCode,
         this->GetVarNativeInfoCount(),
         this->GetSequenceMapCount()));

#if defined(LOGGING)
    for (unsigned int i = 0; i < this->GetSequenceMapCount(); i++)
    {
        LOG((LF_CORDB, LL_INFO10000, "De::JITCo: seq map 0x%x - "
             "IL offset 0x%x native start offset 0x%x native end offset 0x%x source 0x%x\n",
             i, this->GetSequenceMap()[i].ilOffset,
             this->GetSequenceMap()[i].nativeStartOffset,
             this->GetSequenceMap()[i].nativeEndOffset,
             this->GetSequenceMap()[i].source));
    }
#endif // LOGGING

}

/******************************************************************************
 *
 ******************************************************************************/
ICorDebugInfo::SourceTypes DebuggerJitInfo::GetSrcTypeFromILOffset(SIZE_T ilOffset)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    BOOL exact = FALSE;
    DebuggerILToNativeMap *pMap = MapILOffsetToMapEntry(ilOffset, &exact);

    LOG((LF_CORDB, LL_INFO100000, "DJI::GSTFILO: for il 0x%x, got entry 0x%p,"
        "(il 0x%x) nat 0x%x to 0x%x, SourceTypes 0x%x, exact:%x\n", ilOffset, pMap,
        pMap->ilOffset, pMap->nativeStartOffset, pMap->nativeEndOffset, pMap->source,
        exact));

    if (!exact)
    {
        return ICorDebugInfo::SOURCE_TYPE_INVALID;
    }

    return pMap->source;
}

/******************************************************************************
 *
 ******************************************************************************/
DebuggerMethodInfo::~DebuggerMethodInfo()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        DESTRUCTOR_CHECK;
    }
    CONTRACTL_END;

    DeleteJitInfoList();

    LOG((LF_CORDB,LL_EVERYTHING, "DMI::~DMI : deleted at 0x%p\n", this));
}

// Translate between old & new offsets (w/ respect to Instrumented IL).

// Don't interpolate
ULONG32 DebuggerMethodInfo::TranslateToInstIL(const InstrumentedILOffsetMapping * pMapping,
                                              ULONG32 offOrig,
                                              bool fOrigToInst)
{
    LIMITED_METHOD_CONTRACT;

    SIZE_T iMap;
    SIZE_T cMap = pMapping->GetCount();
    // some negative IL offsets have special meaning. Don't translate
    // those (just return as is). See ICorDebugInfo::MappingTypes
    if ((cMap == 0) || (offOrig < 0))
    {
        return offOrig;
    }

    ARRAY_PTR_COR_IL_MAP rgMap = pMapping->GetOffsets();

    // This assumes:
    // - map is sorted in increasing order by both old & new
    // - round down.
    if (fOrigToInst)
    {
        // Translate: old --> new

        // Treat it as prolog if offOrig is not in remapping range
        if ((offOrig < rgMap[0].oldOffset) || (offOrig == (ULONG32)ICorDebugInfo::PROLOG))
        {
            return (ULONG32)ICorDebugInfo::PROLOG;
        }

        if (offOrig == (ULONG32)ICorDebugInfo::EPILOG)
        {
            return (ULONG32)ICorDebugInfo::EPILOG;
        }

        if (offOrig == (ULONG32)ICorDebugInfo::NO_MAPPING)
        {
            return (ULONG32)ICorDebugInfo::NO_MAPPING;
        }

        for(iMap = 1; iMap < cMap; iMap++)
        {
            if (offOrig < rgMap[iMap].oldOffset)
                return rgMap[iMap-1].newOffset;
        }

        return rgMap[iMap - 1].newOffset;
    }
    else
    {
        // Translate: new --> old

        // Treat it as prolog if offOrig is not in remapping range
        if ((offOrig < rgMap[0].newOffset) || (offOrig == (ULONG32)ICorDebugInfo::PROLOG))
        {
            return (ULONG32)ICorDebugInfo::PROLOG;
        }

        if (offOrig == (ULONG32)ICorDebugInfo::EPILOG)
        {
            return (ULONG32)ICorDebugInfo::EPILOG;
        }

        if (offOrig == (ULONG32)ICorDebugInfo::NO_MAPPING)
        {
            return (ULONG32)ICorDebugInfo::NO_MAPPING;
        }

        for(iMap = 1; iMap < cMap; iMap++)
        {
            if (offOrig < rgMap[iMap].newOffset)
                return rgMap[iMap-1].oldOffset;
        }

        return rgMap[iMap - 1].oldOffset;
    }
}

/******************************************************************************
 * Constructor for DebuggerMethodInfo
 ******************************************************************************/
DebuggerMethodInfo::DebuggerMethodInfo(Module *module, mdMethodDef token) :
        m_currentEnCVersion(CorDB_DEFAULT_ENC_FUNCTION_VERSION),
        m_module(module),
        m_token(token),
        m_prevMethodInfo(NULL),
        m_nextMethodInfo(NULL),
        m_latestJitInfo(NULL),
        m_fHasInstrumentedILMap(false)
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_TRIGGERS);
        CONSTRUCTOR_CHECK;
    }
    CONTRACTL_END;

    LOG((LF_CORDB,LL_EVERYTHING, "DMI::DMI : created at 0x%p\n", this));

    _ASSERTE(g_pDebugger->HasDebuggerDataLock());

    DebuggerModule * pModule = GetPrimaryModule();

    m_fJMCStatus = false;

    // If there's no module, then this isn't a JMC function.
    // This can happen since DMIs are created for debuggable code, and
    // Modules are only created if a debugger is actually attached.
    if (pModule != NULL)
    {
        // Use the accessor so that we keep the module's count properly updated.
        SetJMCStatus(pModule->GetRuntimeModule()->GetJMCStatus());
    }
 }


/******************************************************************************
 * Get the primary debugger module for this DMI. This is 1:1 w/ an EE Module.
 ******************************************************************************/
DebuggerModule* DebuggerMethodInfo::GetPrimaryModule()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(g_pDebugger->HasDebuggerDataLock());

    DebuggerModuleTable * pTable = g_pDebugger->GetModuleTable();

    // If we're tracking debug info but no debugger's attached, then
    // we won't have a table for the modules yet.
    if (pTable == NULL)
        return NULL;

    DebuggerModule * pModule = pTable->GetModule(GetRuntimeModule());
    if (pModule == NULL)
    {
        // We may be missing the module even if we have the table.
        // 1.) If there's no debugger attached (so we're not getting ModuleLoad events).
        // 2.) If we're asking for this while in DllMain of the module we're currently loading,
        //     we won't have gotten the ModuleLoad event yet.
        return NULL;
    }

    // Only give back primary modules...
    DebuggerModule * p2 = pModule->GetPrimaryModule();
    _ASSERTE(p2 != NULL);

    return p2;
}

/******************************************************************************
 * Get the runtime module for this DMI
 ******************************************************************************/
Module * DebuggerMethodInfo::GetRuntimeModule()
{
    LIMITED_METHOD_CONTRACT;

    return m_module;
}

#endif // !DACCESS_COMPILE


//---------------------------------------------------------------------------------------
//
// Find the DebuggerJitInfo (DJI) for the given MethodDesc and native start address.
// We need the native start address because generic methods may have multiple instances
// of jitted code.  This function does not create the DJI if it does not already exist.
//
// Arguments:
//    pMD                 - the MD to lookup; must be non-NULL
//    addrNativeStartAddr - the native start address of jitted code
//
// Return Value:
//    Returns the DJI corresponding to the specified MD and native start address.
//    NULL if the DJI is not found.
//

DebuggerJitInfo * DebuggerMethodInfo::FindJitInfo(MethodDesc * pMD,
                                                  TADDR        addrNativeStartAddr)
{
    CONTRACTL
    {
        SUPPORTS_DAC;
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(pMD != NULL);
    }
    CONTRACTL_END;


    DebuggerJitInfo * pCheck = m_latestJitInfo;
    while (pCheck != NULL)
    {
        if ( (pCheck->m_nativeCodeVersion.GetMethodDesc() == dac_cast<PTR_MethodDesc>(pMD)) &&
             (pCheck->m_addrOfCode == addrNativeStartAddr) )
        {
            return pCheck;
        }

        pCheck = pCheck->m_prevJitInfo;
    }

    return NULL;
}


#if !defined(DACCESS_COMPILE)

/*
 * FindOrCreateInitAndAddJitInfo
 *
 * This routine tries to find an existing DJI based on the method desc and start address, or allocates a new DJI, adding it to
 * the DMI.
 *
 * Parameters:
 *   fd - the method desc to find or create a DJI for.
 *   startAddr - the start address to find or create the DJI for.
 *
 * Returns
 *   A pointer to the found or created DJI, or NULL.
 *
 */

DebuggerJitInfo *DebuggerMethodInfo::FindOrCreateInitAndAddJitInfo(MethodDesc* fd, PCODE startAddr)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(fd != NULL);

    // The debugger doesn't track Lightweight-codegen methods b/c they have no metadata.
    if (fd->IsDynamicMethod())
    {
        return NULL;
    }

    if (startAddr == NULL)
    {
        // This will grab the start address for the current code version.
        startAddr = g_pEEInterface->GetFunctionAddress(fd);
        if (startAddr == NULL)
        {
            return NULL;
        }
    }
    else
    {
        _ASSERTE(g_pEEInterface->GetNativeCodeMethodDesc(startAddr) == fd);
        _ASSERTE(g_pEEInterface->GetNativeCodeStartAddress(startAddr) == startAddr);
    }

    // Check the lsit to see if we've already populated an entry for this JitInfo.
    // If we didn't have a JitInfo before, lazily create it now.
    // We don't care if we were prejitted or not.
    //
    // We haven't got the lock yet so we'll repeat this lookup once
    // we've taken the lock.
    ARM_ONLY(_ASSERTE((startAddr & THUMB_CODE) == 1));
    DebuggerJitInfo * pResult = FindJitInfo(fd, startAddr);
    if (pResult != NULL)
    {
        return pResult;
    }

    // The DJI may already be populated in the cache, if so CreateInitAndAddJitInfo is a no-op and that is fine.
    // CreateInitAndAddJitInfo takes a lock and checks the list again, which makes this thread-safe.

    NativeCodeVersion nativeCodeVersion;
    if (fd->IsVersionable())
    {
        CodeVersionManager *pCodeVersionManager = fd->GetCodeVersionManager();
        CodeVersionManager::LockHolder codeVersioningLockHolder;
        nativeCodeVersion = pCodeVersionManager->GetNativeCodeVersion(fd, startAddr);
        if (nativeCodeVersion.IsNull())
        {
            return NULL;
        }
    }
    else
    {
        // Some day we'll get EnC to use code versioning properly, but until then we'll get the right behavior treating all EnC versions as the default native code version.
        nativeCodeVersion = NativeCodeVersion(fd);
    }

    BOOL jitInfoWasCreated;
    return CreateInitAndAddJitInfo(nativeCodeVersion, startAddr, &jitInfoWasCreated);
}

// Create a DJI around a method-desc. The EE already has all the information we need for a DJI,
// the DJI just serves as a cache of the information for the debugger.
// Caller makes no guarantees about whether the DJI is already in the table. (Caller should avoid this if
// it knows it's in the table, but b/c we can't expect caller to synchronize w/ the other threads).
DebuggerJitInfo *DebuggerMethodInfo::CreateInitAndAddJitInfo(NativeCodeVersion nativeCodeVersion, TADDR startAddr, BOOL* jitInfoWasCreated)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(!g_pDebugger->HasDebuggerDataLock());
    }
    CONTRACTL_END;

    MethodDesc* fd = nativeCodeVersion.GetMethodDesc();

    _ASSERTE(fd != NULL);

    // May or may-not be jitted, that's why we passed in the start addr & size explicitly.
    _ASSERTE(startAddr != NULL);

    *jitInfoWasCreated = FALSE;

    // No support for light-weight codegen methods.
    if (fd->IsDynamicMethod())
    {
        return NULL;
    }


    DebuggerJitInfo *dji = new (interopsafe) DebuggerJitInfo(this, nativeCodeVersion);
    _ASSERTE(dji != NULL); // throws on oom error

    _ASSERTE(dji->m_methodInfo == this); // this should be set

    TRACE_ALLOC(dji);

    // Init may take locks that violate the debugger-data lock, so we can't init while we hold that lock.
    // But we can't init after we add it to the table and release the lock b/c another thread may pick
    // if up in the uninitialized state.
    // So we initialize a private copy of the DJI before we take the debugger-data lock.
    dji->Init(startAddr);

    dji->m_nextJitInfo = NULL;

    //
    //<TODO>@TODO : _ASSERTE(EnC);</TODO>
    //
    {
        Debugger::DebuggerDataLockHolder debuggerDataLockHolder(g_pDebugger);

        // We need to ensure that another thread didn't go in and add this exact same DJI?
        {
            DebuggerJitInfo * pResult = FindJitInfo(dji->m_nativeCodeVersion.GetMethodDesc(), (TADDR)dji->m_addrOfCode);
            if (pResult != NULL)
            {
                // Found!
                _ASSERTE(pResult->m_sizeOfCode == dji->m_sizeOfCode);
                DeleteInteropSafe(dji);
                return pResult;
            }
            else
            {
                *jitInfoWasCreated = TRUE;
            }
        }

        // We know it's not in the table. Go add it!
        DebuggerJitInfo *djiPrev = m_latestJitInfo;

        LOG((LF_CORDB,LL_INFO10000,"DMI:CAAJI: current head of dji list:0x%p\n", djiPrev));

        if (djiPrev != NULL)
        {
            dji->m_prevJitInfo = djiPrev;
            djiPrev->m_nextJitInfo = dji;

            m_latestJitInfo = dji;

            LOG((LF_CORDB,LL_INFO10000,"DMI:CAAJI: DJI version 0x%04x for %s\n",
                 GetCurrentEnCVersion(),
                 dji->m_nativeCodeVersion.GetMethodDesc()->m_pszDebugMethodName));
        }
        else
        {
            m_latestJitInfo = dji;
        }

    } // DebuggerDataLockHolder out of scope - release implied

    // We've now added a new DJI into the table and released the lock. Thus any other thread
    // can come and use our DJI. Good thing we inited the DJI _before_ adding it to the table.

    LOG((LF_CORDB,LL_INFO10000,"DMI:CAAJI: new head of dji list:0x%p\n", m_latestJitInfo));

    return dji;
}

/*
 * DeleteJitInfo
 *
 * This routine remove a DJI from the DMI's list and deletes the memory.
 *
 * Parameters:
 *   dji - The DJI to delete.
 *
 * Returns
 *   None.
 *
 */

void DebuggerMethodInfo::DeleteJitInfo(DebuggerJitInfo *dji)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    Debugger::DebuggerDataLockHolder debuggerDataLockHolder(g_pDebugger);

    LOG((LF_CORDB,LL_INFO10000,"DMI:DJI: dji:0x%08x\n", dji));

    DebuggerJitInfo *djiPrev = dji->m_prevJitInfo;

    if (djiPrev != NULL)
    {
        djiPrev->m_nextJitInfo = dji->m_nextJitInfo;
    }

    if (dji->m_nextJitInfo != NULL)
    {
        dji->m_nextJitInfo->m_prevJitInfo = djiPrev;
    }
    else
    {
        //
        // This DJI is the head of the list
        //
        _ASSERTE(m_latestJitInfo == dji);

        m_latestJitInfo = djiPrev;
    }

    TRACE_FREE(dji);

    DeleteInteropSafe(dji);

    // DebuggerDataLockHolder out of scope - release implied
}

/*
 * DeleteJitInfoList
 *
 * This routine removes all the DJIs from the current DMI.
 *
 * Parameters:
 *   None.
 *
 * Returns
 *   None.
 *
 */

void DebuggerMethodInfo::DeleteJitInfoList(void)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    Debugger::DebuggerDataLockHolder debuggerDataLockHolder(g_pDebugger);

    while(m_latestJitInfo != NULL)
    {
        DeleteJitInfo(m_latestJitInfo);
    }

    // DebuggerDataLockHolder out of scope - release implied
}


// Iterate through all existing DJIs. See header for expected usage.
DebuggerMethodInfo::DJIIterator::DJIIterator()
{
    LIMITED_METHOD_CONTRACT;

    m_pCurrent = NULL;
    m_pLoaderModuleFilter = NULL;
}

bool DebuggerMethodInfo::DJIIterator::IsAtEnd()
{
    LIMITED_METHOD_CONTRACT;

    return m_pCurrent == NULL;
}

DebuggerJitInfo * DebuggerMethodInfo::DJIIterator::Current()
{
    LIMITED_METHOD_CONTRACT;

    return m_pCurrent;
}

void DebuggerMethodInfo::DJIIterator::Next(BOOL fFirst /*=FALSE*/)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        MODE_ANY;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    if (!fFirst)
    {
        PREFIX_ASSUME(m_pCurrent != NULL); // IsAtEnd() should have caught this.
        m_pCurrent = m_pCurrent->m_prevJitInfo;
    }

    // Check if we're at the end of the list, in which case we're done.
    for ( ; m_pCurrent != NULL; m_pCurrent = m_pCurrent->m_prevJitInfo)
    {
        Module * pLoaderModule = m_pCurrent->m_pLoaderModule;

        // Obey the module filter if it's provided
        if ((m_pLoaderModuleFilter != NULL) && (m_pLoaderModuleFilter != pLoaderModule))
            continue;

        //Obey the methodDesc filter if it is provided
        if ((m_pMethodDescFilter != NULL) && (m_pMethodDescFilter != m_pCurrent->m_nativeCodeVersion.GetMethodDesc()))
            continue;

        // Skip modules that are unloaded, but still hanging around. Note that we can't use DebuggerModule for this check
        // because of it is deleted pretty early during unloading, and we do not want to recreate it.
        if (pLoaderModule->GetLoaderAllocator()->IsUnloaded())
            continue;

        break;
    }
}


/******************************************************************************
 * Return true iff this method is jitted
 ******************************************************************************/
bool DebuggerMethodInfo::HasJitInfos()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(g_pDebugger->HasDebuggerDataLock());
    return (m_latestJitInfo != NULL);
}

/******************************************************************************
 * Return true iff this has been EnCed since last time function was jitted.
 ******************************************************************************/
bool DebuggerMethodInfo::HasMoreRecentEnCVersion()
{
    LIMITED_METHOD_CONTRACT;
    return ((m_latestJitInfo != NULL) &&
            (m_currentEnCVersion > m_latestJitInfo->m_encVersion));
}

/******************************************************************************
 * Updated the instrumented-IL map
 ******************************************************************************/
void DebuggerMethodInfo::SetInstrumentedILMap(COR_IL_MAP * pMap, SIZE_T cEntries)
{
    InstrumentedILOffsetMapping mapping;
    mapping.SetMappingInfo(cEntries, pMap);

    GetRuntimeModule()->SetInstrumentedILOffsetMapping(m_token, mapping);

    m_fHasInstrumentedILMap = true;
}

/******************************************************************************
 * Get the JMC status for a given function.
 ******************************************************************************/
bool DebuggerMethodInfo::IsJMCFunction()
{
    LIMITED_METHOD_CONTRACT;
    return m_fJMCStatus;
}

/******************************************************************************
 * Set the JMC status to a given value
 ******************************************************************************/
void DebuggerMethodInfo::SetJMCStatus(bool fStatus)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(g_pDebugger->HasDebuggerDataLock());

    // First check if this is a no-op.
    // Do this first b/c there may be some cases where we don't have a DebuggerModule
    // yet but are still calling SetJMCStatus(false), like if we detach before attach is complete.
    bool fOldStatus = IsJMCFunction();

    if (fOldStatus == fStatus)
    {
        // if no change, then there's nothing to do.
        LOG((LF_CORDB,LL_EVERYTHING, "DMI::SetJMCStatus: %p, keeping old status, %d\n", this, fStatus));
        return;
    }

    // For a perf-optimization, our Module needs to know if it has any user
    // code. If it doesn't, it shouldn't dispatch through the JMC probes.
    // So modules keep a count of # of JMC functions - if the count is 0, the
    // module can set is JMC probe flag to 0 and skip the JMC probes.
    Module * pRuntimeModule = this->GetRuntimeModule();

    // Update the module's count.
    if (!fStatus)
    {
        LOG((LF_CORDB,LL_EVERYTHING, "DMI::SetJMCStatus: %p, changing to non-user code\n", this));
        _ASSERTE(pRuntimeModule->HasAnyJMCFunctions());
        pRuntimeModule->DecJMCFuncCount();
    }
    else
    {
        LOG((LF_CORDB,LL_EVERYTHING, "DMI::SetJMCStatus: %p, changing to user code\n", this));
        pRuntimeModule->IncJMCFuncCount();
        _ASSERTE(pRuntimeModule->HasAnyJMCFunctions());
    }

    m_fJMCStatus = fStatus;

    // We should update our module's JMC status...
    g_pDebugger->UpdateModuleJMCFlag(pRuntimeModule, DebuggerController::GetTotalMethodEnter() != 0);

}

// Get an iterator that will go through ALL native code-blobs (DJI) in the specified
// AppDomain, optionally filtered by loader module (if pLoaderModuleFilter != NULL).
// This is EnC/ Generics / Prejit aware.
void DebuggerMethodInfo::IterateAllDJIs(AppDomain * pAppDomain, Module * pLoaderModuleFilter, MethodDesc * pMethodDescFilter, DebuggerMethodInfo::DJIIterator * pEnum)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(pEnum != NULL);
    _ASSERTE(pAppDomain != NULL || pMethodDescFilter != NULL);

    // Esnure we have DJIs for everything.
    CreateDJIsForNativeBlobs(pAppDomain, pLoaderModuleFilter, pMethodDescFilter);

    pEnum->m_pCurrent = m_latestJitInfo;
    pEnum->m_pLoaderModuleFilter = pLoaderModuleFilter;
    pEnum->m_pMethodDescFilter = pMethodDescFilter;

    // Advance to the first DJI that passes the filter
    pEnum->Next(TRUE);
}

//---------------------------------------------------------------------------------------
//
// Bring the DJI cache up to date.
//
// Arguments:
//      * pAppDomain - Create DJIs only for this AppDomain
//      * pLoaderModuleFilter - If non-NULL, create DJIs only for MethodDescs whose
//          loader module matches this one. (This can be different from m_module in the
//          case of generics defined in one module and instantiated in another). If
//          non-NULL, create DJIs for all modules in pAppDomain.
//      * pMethodDescFilter - If non-NULL, create DJIs only for this single MethodDesc.
//

void DebuggerMethodInfo::CreateDJIsForNativeBlobs(AppDomain * pAppDomain, Module * pLoaderModuleFilter, MethodDesc* pMethodDescFilter)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // If we're not stopped and the module we're iterating over allows types to load,
    // then it's possible new native blobs are being created underneath us.
    _ASSERTE(g_pDebugger->IsStopped() ||
             ((pLoaderModuleFilter != NULL) && !pLoaderModuleFilter->IsReadyForTypeLoad()) ||
             pMethodDescFilter != NULL);

    if (pMethodDescFilter != NULL)
    {
        CreateDJIsForMethodDesc(pMethodDescFilter);
    }
    else
    {
        // @todo - we really only need to do this if the stop-counter goes up (else we know nothing new is added).
        // B/c of generics, it's possible that new instantiations of a method may have been jitted.
        // So just loop through all known instantiations and ensure that we have all the DJIs.
        // Note that this iterator won't show previous EnC versions, but we're already guaranteed to
        // have DJIs for every verision of a method that was EnCed.
        // This also handles the possibility of getting the same methoddesc back from the iterator.
        // It also lets EnC + generics play nice together (including if an generic method was EnC-ed)
        LoadedMethodDescIterator it(pAppDomain, m_module, m_token);
        CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;
        while (it.Next(pDomainAssembly.This()))
        {
            MethodDesc * pDesc = it.Current();
            if (!pDesc->HasNativeCode())
            {
                continue;
            }

            Module * pLoaderModule = pDesc->GetLoaderModule();

            // Obey the module filter if it's provided
            if ((pLoaderModuleFilter != NULL) && (pLoaderModuleFilter != pLoaderModule))
                continue;

            // Skip modules that are unloaded, but still hanging around. Note that we can't use DebuggerModule for this check
            // because of it is deleted pretty early during unloading, and we do not want to recreate it.
            if (pLoaderModule->GetLoaderAllocator()->IsUnloaded())
                continue;

            CreateDJIsForMethodDesc(pDesc);
        }
    }
}


//---------------------------------------------------------------------------------------
//
// Bring the DJI cache up to date for jitted code instances of a particular MethodDesc.
//
//
void DebuggerMethodInfo::CreateDJIsForMethodDesc(MethodDesc * pMethodDesc)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

   LOG((LF_CORDB, LL_INFO10000, "DMI::CDJIFMD pMethodDesc:0x%p\n", pMethodDesc));

    // The debugger doesn't track Lightweight-codegen methods b/c they have no metadata.
    if (pMethodDesc->IsDynamicMethod())
    {
        return;
    }

#ifdef FEATURE_CODE_VERSIONING
    CodeVersionManager* pCodeVersionManager = pMethodDesc->GetCodeVersionManager();
    // grab the code version lock to iterate available versions of the code
    {
        CodeVersionManager::LockHolder codeVersioningLockHolder;
        NativeCodeVersionCollection nativeCodeVersions = pCodeVersionManager->GetNativeCodeVersions(pMethodDesc);

#ifdef LOGGING
        int count = 0;
#endif // LOGGING
        for (NativeCodeVersionIterator itr = nativeCodeVersions.Begin(), end = nativeCodeVersions.End(); itr != end; itr++)
        {
            // Some versions may not be compiled yet - skip those for now
            // if they compile later the JitCompiled callback will add a DJI to our cache at that time
            PCODE codeAddr = itr->GetNativeCode();
            LOG((LF_CORDB, LL_INFO10000, "DMI::CDJIFMD (%d) Native code for DJI - 0x%p\n", ++count, codeAddr));
            if (codeAddr)
            {
                // The DJI may already be populated in the cache, if so CreateInitAndAdd is
                // a no-op and that is fine.
                BOOL unusedDjiWasCreated;
                CreateInitAndAddJitInfo(*itr, codeAddr, &unusedDjiWasCreated);
                LOG((LF_CORDB, LL_INFO10000, "DMI::CDJIFMD Was DJI created? 0x%d\n", unusedDjiWasCreated));
            }
        }
        LOG((LF_CORDB, LL_INFO10000, "DMI::CDJIFMD NativeCodeVersion total %d for md=0x%p\n", count, pMethodDesc));
    }
#else
    // We just ask for the DJI to ensure that it's lazily created.
    // This should only fail in an oom scenario.
    DebuggerJitInfo * djiTest = g_pDebugger->GetLatestJitInfoFromMethodDesc(pDesc);
    if (djiTest == NULL)
    {
        // We're oom. Give up.
        ThrowOutOfMemory();
        return;
    }
#endif
}

/*
 * GetLatestJitInfo
 *
 * This routine returns the lastest DJI we have for a particular DMI.
 * DJIs are lazily created.
 * Parameters:
 *   None.
 *
 * Returns
 *   a possibly NULL pointer to a DJI.
 *
 */

// For logging and other internal purposes, provide a non-initializing accessor.
DebuggerJitInfo* DebuggerMethodInfo::GetLatestJitInfo_NoCreate()
{
    return m_latestJitInfo;
}


DebuggerMethodInfoTable::DebuggerMethodInfoTable() : CHashTableAndData<CNewZeroData>(101)
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        GC_NOTRIGGER;

        CONSTRUCTOR_CHECK;
    }
    CONTRACTL_END;

    SUPPRESS_ALLOCATION_ASSERTS_IN_THIS_SCOPE;
    HRESULT hr = NewInit(101, sizeof(DebuggerMethodInfoEntry), 101);

    if (FAILED(hr))
    {
        ThrowWin32(hr);
    }
}

HRESULT DebuggerMethodInfoTable::AddMethodInfo(Module *pModule,
                   mdMethodDef token,
                   DebuggerMethodInfo *mi)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;

        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(pModule));
        PRECONDITION(CheckPointer(mi));
    }
    CONTRACTL_END;

   LOG((LF_CORDB, LL_INFO1000, "DMIT::AMI Adding dmi:0x%x Mod:0x%x tok:"
        "0x%x nVer:0x%x\n", mi, pModule, token, mi->GetCurrentEnCVersion()));

   _ASSERTE(mi != NULL);

    _ASSERTE(g_pDebugger->HasDebuggerDataLock());

    HRESULT hr = OverwriteMethodInfo(pModule, token, mi, TRUE);
    if (hr == S_OK)
        return hr;

    DebuggerMethodInfoKey dmik;
    dmik.pModule = pModule;
    dmik.token = token;

    DebuggerMethodInfoEntry *dmie =
        (DebuggerMethodInfoEntry *) Add(HASH(&dmik));

    if (dmie != NULL)
    {
        dmie->key.pModule = pModule;
        dmie->key.token = token;
        dmie->mi = mi;

        LOG((LF_CORDB, LL_INFO1000, "DMIT::AJI: mod:0x%x tok:0%x ",
            pModule, token));
        return S_OK;
    }

    ThrowOutOfMemory();
    return S_OK;
}

HRESULT DebuggerMethodInfoTable::OverwriteMethodInfo(Module *pModule,
                         mdMethodDef token,
                         DebuggerMethodInfo *mi,
                         BOOL fOnlyIfNull)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;

        PRECONDITION(CheckPointer(pModule));
        PRECONDITION(CheckPointer(mi));
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO1000, "DMIT::OJI: dmi:0x%x mod:0x%x tok:0x%x\n", mi,
        pModule, token));

    _ASSERTE(g_pDebugger->HasDebuggerDataLock());

    DebuggerMethodInfoKey dmik;
    dmik.pModule = pModule;
    dmik.token = token;

    DebuggerMethodInfoEntry *entry
      = (DebuggerMethodInfoEntry *) Find(HASH(&dmik), KEY(&dmik));
    if (entry != NULL)
    {
        if ( (fOnlyIfNull &&
              entry->mi == NULL) ||
             !fOnlyIfNull)
        {
            entry->mi = mi;

            LOG((LF_CORDB, LL_INFO1000, "DMIT::OJI: mod:0x%x tok:0x%x remap"
                "nVer:0x%x\n", pModule, token, entry->nVersionLastRemapped));
            return S_OK;
        }
    }

    return E_FAIL;
}

// pModule is being destroyed - remove any entries that belong to it.  Why?
// (a) Correctness: the module can be reloaded at the same address,
//      which will cause accidental matches with our hashtable (indexed by
//      {Module*,mdMethodDef}
// (b) Perf: don't waste the memory!
void DebuggerMethodInfoTable::ClearMethodsOfModule(Module *pModule)
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(g_pDebugger->HasDebuggerDataLock());

    LOG((LF_CORDB, LL_INFO1000000, "CMOM:mod:0x%x (%S)\n", pModule
        ,pModule->GetDebugName()));

    HASHFIND info;

    DebuggerMethodInfoEntry *entry
      = (DebuggerMethodInfoEntry *) FindFirstEntry(&info);
    while(entry != NULL)
    {
        Module *pMod = entry->key.pModule ;
        if (pMod == pModule)
        {
            // This method actually got mitted, at least
            // once - remove all version info.
            while(entry->mi != NULL)
            {
                DeleteEntryDMI(entry);
            }

            Delete(HASH(&(entry->key)), (HASHENTRY*)entry);
        }
        else
        {
            //
            // Delete generic DJIs that have lifetime attached to this module
            //
            DebuggerMethodInfo * dmi = entry->mi;
            while (dmi != NULL)
            {
                DebuggerJitInfo * dji = dmi->GetLatestJitInfo_NoCreate();
                while (dji != NULL)
                {
                    DebuggerJitInfo * djiPrev = dji->m_prevJitInfo;;

                    if (dji->m_pLoaderModule == pModule)
                        dmi->DeleteJitInfo(dji);

                    dji = djiPrev;
                }

                dmi = dmi->m_prevMethodInfo;
            }
        }

        entry = (DebuggerMethodInfoEntry *) FindNextEntry(&info);
    }
}

void DebuggerMethodInfoTable::DeleteEntryDMI(DebuggerMethodInfoEntry *entry)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CAN_TAKE_LOCK;      // DeleteInteropSafe() eventually calls DebuggerMethodInfo::DeleteJitInfoList
                            // which locks.
    }
    CONTRACTL_END;

    DebuggerMethodInfo *dmiPrev = entry->mi->m_prevMethodInfo;
    TRACE_FREE(entry->mi);
    DeleteInteropSafe(entry->mi);
    entry->mi = dmiPrev;
    if ( dmiPrev != NULL )
        dmiPrev->m_nextMethodInfo = NULL;
}

#endif // #ifndef DACCESS_COMPILE

PTR_DebuggerJitInfo DebuggerMethodInfo::GetLatestJitInfo(MethodDesc *mdesc)
{
    // dac checks ngen'ed image content first, so
    // only check for existing JIT info.
#ifndef DACCESS_COMPILE

    CONTRACTL
    {
        THROWS;
        CALLED_IN_DEBUGGERDATALOCK_HOLDER_SCOPE_MAY_GC_TRIGGERS_CONTRACT;
        PRECONDITION(!g_pDebugger->HasDebuggerDataLock());
    }
    CONTRACTL_END;


    if (m_latestJitInfo && m_latestJitInfo->m_nativeCodeVersion.GetMethodDesc() == mdesc && !m_latestJitInfo->m_nativeCodeVersion.GetMethodDesc()->HasClassOrMethodInstantiation())
        return m_latestJitInfo;

    // This ensures that there is an entry in the DJI list for this particular MethodDesc.
    // in the case of generic code it may not be the first entry in the list.
    FindOrCreateInitAndAddJitInfo(mdesc, NULL /* startAddr */);

#endif // #ifndef DACCESS_COMPILE

    return m_latestJitInfo;
}

DebuggerMethodInfo *DebuggerMethodInfoTable::GetMethodInfo(Module *pModule, mdMethodDef token)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    DebuggerMethodInfoKey dmik;
    dmik.pModule = dac_cast<PTR_Module>(pModule);
    dmik.token = token;

    DebuggerMethodInfoEntry *entry = dac_cast<PTR_DebuggerMethodInfoEntry>(Find(HASH(&dmik), KEY(&dmik)));

    if (entry == NULL )
    {
        return NULL;
    }
    else
    {
        LOG((LF_CORDB, LL_INFO1000, "DMI::GMI: for methodDef 0x%x, got 0x%p prev:0x%p\n",
            token, entry->mi, (entry->mi?entry->mi->m_prevMethodInfo:0)));
        return entry->mi;
    }
}


DebuggerMethodInfo *DebuggerMethodInfoTable::GetFirstMethodInfo(HASHFIND *info)
{
    CONTRACT(DebuggerMethodInfo*)
    {
        NOTHROW;
        GC_NOTRIGGER;

        PRECONDITION(CheckPointer(info));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    _ASSERTE(g_pDebugger->HasDebuggerDataLock());

    DebuggerMethodInfoEntry *entry = PTR_DebuggerMethodInfoEntry
        (PTR_HOST_TO_TADDR(FindFirstEntry(info)));
    if (entry == NULL)
        RETURN NULL;
    else
        RETURN entry->mi;
}

DebuggerMethodInfo *DebuggerMethodInfoTable::GetNextMethodInfo(HASHFIND *info)
{
    CONTRACT(DebuggerMethodInfo*)
    {
        NOTHROW;
        GC_NOTRIGGER;

        PRECONDITION(CheckPointer(info));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    _ASSERTE(g_pDebugger->HasDebuggerDataLock());

    DebuggerMethodInfoEntry *entry = PTR_DebuggerMethodInfoEntry
        (PTR_HOST_TO_TADDR(FindNextEntry(info)));

    // We may have incremented the version number
    // for methods that never got JITted, so we should
    // pretend like they don't exist here.
    while (entry != NULL &&
           entry->mi == NULL)
    {
        entry = PTR_DebuggerMethodInfoEntry
            (PTR_HOST_TO_TADDR(FindNextEntry(info)));
    }

    if (entry == NULL)
        RETURN NULL;
    else
        RETURN entry->mi;
}



#ifdef DACCESS_COMPILE
void
DebuggerMethodInfoEntry::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;

    // This structure is in an array in the hash
    // so the 'this' is implicitly enumerated by the
    // array enum in CHashTable.

    // For a MiniDumpNormal, what is needed for modules is already enumerated elsewhere.
    // Don't waste time doing it here an extra time. Also, this will add many MB extra into the dump.
    if ((key.pModule.IsValid()) &&
        CLRDATA_ENUM_MEM_MINI != flags
        && CLRDATA_ENUM_MEM_TRIAGE != flags)
    {
        key.pModule->EnumMemoryRegions(flags, true);
    }

    while (mi.IsValid())
    {
        mi->EnumMemoryRegions(flags);
        mi = mi->m_prevMethodInfo;
    }
}

void
DebuggerMethodInfo::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    DAC_ENUM_DTHIS();
    SUPPORTS_DAC;

    if (flags != CLRDATA_ENUM_MEM_MINI && flags != CLRDATA_ENUM_MEM_TRIAGE)
    {
        // Modules are enumerated already for minidumps, save the empty calls.
        if (m_module.IsValid())
        {
            m_module->EnumMemoryRegions(flags, true);
        }

    }

    PTR_DebuggerJitInfo jitInfo = m_latestJitInfo;
    while (jitInfo.IsValid())
    {
        jitInfo->EnumMemoryRegions(flags);
        jitInfo = jitInfo->m_prevJitInfo;
    }
}

void
DebuggerJitInfo::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    DAC_ENUM_DTHIS();
    SUPPORTS_DAC;

    if (m_methodInfo.IsValid())
    {
        m_methodInfo->EnumMemoryRegions(flags);
    }

    if (flags != CLRDATA_ENUM_MEM_MINI && flags != CLRDATA_ENUM_MEM_TRIAGE)
    {
        if (m_nativeCodeVersion.GetMethodDesc().IsValid())
        {
            m_nativeCodeVersion.GetMethodDesc()->EnumMemoryRegions(flags);
        }

        DacEnumMemoryRegion(PTR_TO_TADDR(GetSequenceMap()),
                            GetSequenceMapCount() * sizeof(DebuggerILToNativeMap));
        DacEnumMemoryRegion(PTR_TO_TADDR(GetVarNativeInfo()),
                            GetVarNativeInfoCount() *
                            sizeof(ICorDebugInfo::NativeVarInfo));
    }
}


void DebuggerMethodInfoTable::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    WRAPPER_NO_CONTRACT;

    DAC_ENUM_VTHIS();
    CHashTableAndData<CNewZeroData>::EnumMemoryRegions(flags);

    for (ULONG i = 0; i < m_iEntries; i++)
    {
        DebuggerMethodInfoEntry* entry =
            PTR_DebuggerMethodInfoEntry(PTR_HOST_TO_TADDR(EntryPtr(i)));
        entry->EnumMemoryRegions(flags);
    }
}
#endif // #ifdef DACCESS_COMPILE
