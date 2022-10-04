// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// DebugInfoStore



#include "common.h"
#include "debuginfostore.h"
#include "nibblestream.h"
#include "patchpointinfo.h"


#ifdef _DEBUG
// For debug builds only.
static bool Dbg_ShouldUseCookies()
{
    SUPPORTS_DAC;

    // Normally we want this as false b/c it would bloat the image.
    // But give us a hook to enable it in case we need it.
    return false;
}
#endif

//-----------------------------------------------------------------------------
// We have "Transfer" objects that sit on top of the streams.
// The objects look identical, but one serializes and the other deserializes.
// This lets the compression + restoration routines share all their compression
// logic and just swap out Transfer objects.
//
// It's not ideal that we have a lot of redundancy maintaining both Transfer
// objects, but at least the compiler can enforce that the Reader & Writer are
// in sync. It can't enforce that a 2 separate routines for Compression &
// restoration are in sync.
//
// We could have the TransferReader + Writer be polymorphic off a base class,
// but the virtual function calls will be extra overhead. May as well use
// templates and let the compiler resolve it all statically at compile time.
//-----------------------------------------------------------------------------


//-----------------------------------------------------------------------------
// Serialize to a NibbleWriter stream.
//-----------------------------------------------------------------------------
class TransferWriter
{
public:
    TransferWriter(NibbleWriter & w) : m_w(w)
    {
    }

    // Write an raw U32 in nibble encoded form.
    void DoEncodedU32(uint32_t dw) { m_w.WriteEncodedU32(dw); }

    // Use to encode a monotonically increasing delta.
    void DoEncodedDeltaU32(uint32_t & dw, uint32_t dwLast)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;
        _ASSERTE(dw >= dwLast);
        uint32_t dwDelta = dw - dwLast;
        m_w.WriteEncodedU32(dwDelta);
    }

    // Some U32 may have a few sentinel negative values .
    // We adjust it to be a real U32 and then encode that.
    // dwAdjust should be the lower bound on the enum.
    void DoEncodedAdjustedU32(uint32_t dw, uint32_t dwAdjust)
    {
        //_ASSERTE(dwAdjust < 0); // some negative lower bound.
        m_w.WriteEncodedU32(dw - dwAdjust);
    }

    void DoEncodedDeltaU32NonMonotonic(uint32_t& dw, uint32_t dwLast)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        int32_t dwDelta = static_cast<int32_t>(dw) - static_cast<int32_t>(dwLast);
        m_w.WriteEncodedI32(dwDelta);
    }

    // Typesafe versions of EncodeU32.
    void DoEncodedSourceType(ICorDebugInfo::SourceTypes & dw) { m_w.WriteEncodedU32(dw); }
    void DoEncodedVarLocType(ICorDebugInfo::VarLocType & dw) { m_w.WriteEncodedU32(dw); }
    void DoEncodedUnsigned(unsigned & dw) { m_w.WriteEncodedU32(dw); }

    // Stack offsets are aligned on a DWORD boundary, so that lets us shave off 2 bits.
    void DoEncodedStackOffset(signed & dwOffset)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;
#ifdef TARGET_X86
        _ASSERTE(dwOffset % sizeof(DWORD) == 0); // should be dword aligned. That'll save us 2 bits.
        m_w.WriteEncodedI32(dwOffset / sizeof(DWORD));
#else
        // Non x86 platforms don't need it to be dword aligned.
        m_w.WriteEncodedI32(dwOffset);
#endif
    }

    void DoEncodedRegIdx(ICorDebugInfo::RegNum & reg) { m_w.WriteEncodedU32(reg); }

    // For debugging purposes, inject cookies into the Compression.
    void DoCookie(BYTE b) {
#ifdef _DEBUG
        if (Dbg_ShouldUseCookies())
        {
            m_w.WriteNibble(b);
        }
#endif
    }

    void DoMethodHandle(CORINFO_METHOD_HANDLE p)
    {
        uintptr_t up = reinterpret_cast<uintptr_t>(p);

#ifdef TARGET_64BIT
        m_w.WriteUnencodedU32(static_cast<uint32_t>(up));
        m_w.WriteUnencodedU32(static_cast<uint32_t>(up >> 32));
#else
        m_w.WriteUnencodedU32(static_cast<uint32_t>(up));
#endif
    }

protected:
    NibbleWriter & m_w;

};

//-----------------------------------------------------------------------------
// Deserializer that sits on top of a NibbleReader
// This class interface matches TransferWriter exactly. See that for details.
//-----------------------------------------------------------------------------
class TransferReader
{
public:
    TransferReader(NibbleReader & r) : m_r(r)
    {
        SUPPORTS_DAC;
    }

    void DoEncodedU32(uint32_t & dw)
    {
        SUPPORTS_DAC;
        dw = m_r.ReadEncodedU32();
    }

    // Use to decode a monotonically increasing delta.
    // dwLast was the last value; we update it to the current value on output.
    void DoEncodedDeltaU32(uint32_t & dw, uint32_t dwLast)
    {
        SUPPORTS_DAC;
        uint32_t dwDelta = m_r.ReadEncodedU32();
        dw = dwLast + dwDelta;
    }

    void DoEncodedAdjustedU32(uint32_t & dw, uint32_t dwAdjust)
    {
        SUPPORTS_DAC;
        //_ASSERTE(dwAdjust < 0);
        dw = m_r.ReadEncodedU32() + dwAdjust;
    }

    void DoEncodedDeltaU32NonMonotonic(uint32_t& dw, uint32_t dwLast)
    {
        SUPPORTS_DAC;
        int32_t dwDelta = m_r.ReadEncodedI32();
        dw = static_cast<uint32_t>(static_cast<int32_t>(dwLast) + dwDelta);
    }

    void DoEncodedSourceType(ICorDebugInfo::SourceTypes & dw)
    {
        SUPPORTS_DAC;
        dw = (ICorDebugInfo::SourceTypes) m_r.ReadEncodedU32();
    }

    void DoEncodedVarLocType(ICorDebugInfo::VarLocType & dw)
    {
        SUPPORTS_DAC;
        dw = (ICorDebugInfo::VarLocType) m_r.ReadEncodedU32();
    }

    void DoEncodedUnsigned(unsigned & dw)
    {
        SUPPORTS_DAC;
        dw = (unsigned) m_r.ReadEncodedU32();
    }


    // Stack offsets are aligned on a DWORD boundary, so that lets us shave off 2 bits.
    void DoEncodedStackOffset(signed & dwOffset)
    {
        SUPPORTS_DAC;
#ifdef TARGET_X86
        dwOffset = m_r.ReadEncodedI32() * sizeof(DWORD);
#else
        // Non x86 platforms don't need it to be dword aligned.
        dwOffset = m_r.ReadEncodedI32();
#endif
    }

    void DoEncodedRegIdx(ICorDebugInfo::RegNum & reg)
    {
        SUPPORTS_DAC;
        reg = (ICorDebugInfo::RegNum) m_r.ReadEncodedU32();
    }

    void DoMethodHandle(CORINFO_METHOD_HANDLE& p)
    {
#ifdef TARGET_64BIT
        uint32_t lo = m_r.ReadUnencodedU32();
        uint32_t hi = m_r.ReadUnencodedU32();
        p = reinterpret_cast<CORINFO_METHOD_HANDLE>(uintptr_t(lo) | (uintptr_t(hi) << 32));
#else
        uint32_t val = m_r.ReadUnencodedU32();
        p = reinterpret_cast<CORINFO_METHOD_HANDLE>(static_cast<uintptr_t>(val));
#endif
    }

    // For debugging purposes, inject cookies into the Compression.
    void DoCookie(BYTE b)
    {
        SUPPORTS_DAC;

#ifdef _DEBUG
        if (Dbg_ShouldUseCookies())
        {
            BYTE b2 = m_r.ReadNibble();
            _ASSERTE(b == b2);
        }
#endif
    }


protected:
    NibbleReader & m_r;
};


#if defined(_DEBUG) && !defined(DACCESS_COMPILE)
// Perf tracking
static int g_CDI_TotalMethods           = 0;
static int g_CDI_bMethodTotalUncompress = 0;
static int g_CDI_bMethodTotalCompress   = 0;

static int g_CDI_bVarsTotalUncompress   = 0;
static int g_CDI_bVarsTotalCompress     = 0;

static int g_CDI_bRichDebugInfoTotalUncompress = 0;
static int g_CDI_bRichDebugInfoTotalCompress = 0;
#endif

//-----------------------------------------------------------------------------
// Serialize Bounds info.
//-----------------------------------------------------------------------------
template <class T>
static void DoBounds(
    T trans, // transfer object.
    ULONG32                       cMap,
    ICorDebugInfo::OffsetMapping *pMap
)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;


    // Bounds info contains (Native Offset, IL Offset, flags)
    // - Sorted by native offset (so use a delta encoding for that).
    // - IL offsets aren't sorted, but they should be close to each other (so a signed delta encoding)
    //   They may also include a sentinel value from MappingTypes.
    // - flags is 3 independent bits.

    // Loop through and transfer each Entry in the Mapping.
    uint32_t dwLastNativeOffset = 0;
    for(uint32_t i = 0; i < cMap; i++)
    {
        ICorDebugInfo::OffsetMapping * pBound = &pMap[i];

        trans.DoEncodedDeltaU32(pBound->nativeOffset, dwLastNativeOffset);
        dwLastNativeOffset = pBound->nativeOffset;


        trans.DoEncodedAdjustedU32(pBound->ilOffset, (DWORD) ICorDebugInfo::MAX_MAPPING_VALUE);

        trans.DoEncodedSourceType(pBound->source);

        trans.DoCookie(0xA);
    }
}



// Helper to write a compressed Native Var Info
template<class T>
static void DoNativeVarInfo(
    T trans,
    ICorDebugInfo::NativeVarInfo * pVar
)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;


    // Each Varinfo has a:
    // - native start +End offset. We can use a delta for the end offset.
    // - Il variable number. These are usually small.
    // - VarLoc information. This is a tagged variant.
    // The entries aren't sorted in any particular order.
    trans.DoCookie(0xB);
    trans.DoEncodedU32(pVar->startOffset);


    trans.DoEncodedDeltaU32(pVar->endOffset, pVar->startOffset);

    // record var number.
    trans.DoEncodedAdjustedU32(pVar->varNumber, (DWORD) ICorDebugInfo::MAX_ILNUM);


    // Now write the VarLoc... This is a variant like structure and so we'll get different
    // compressioned depending on what we've got.
    trans.DoEncodedVarLocType(pVar->loc.vlType);

    switch(pVar->loc.vlType)
    {
    case ICorDebugInfo::VLT_REG:
    case ICorDebugInfo::VLT_REG_FP:     // fall through
    case ICorDebugInfo::VLT_REG_BYREF:  // fall through
        trans.DoEncodedRegIdx(pVar->loc.vlReg.vlrReg);
        break;

    case ICorDebugInfo::VLT_STK:
    case ICorDebugInfo::VLT_STK_BYREF:  // fall through
        trans.DoEncodedRegIdx(pVar->loc.vlStk.vlsBaseReg);
        trans.DoEncodedStackOffset(pVar->loc.vlStk.vlsOffset);
        break;

    case ICorDebugInfo::VLT_REG_REG:
        trans.DoEncodedRegIdx(pVar->loc.vlRegReg.vlrrReg1);
        trans.DoEncodedRegIdx(pVar->loc.vlRegReg.vlrrReg2);
        break;

    case ICorDebugInfo::VLT_REG_STK:
        trans.DoEncodedRegIdx(pVar->loc.vlRegStk.vlrsReg);
        trans.DoEncodedRegIdx(pVar->loc.vlRegStk.vlrsStk.vlrssBaseReg);
        trans.DoEncodedStackOffset(pVar->loc.vlRegStk.vlrsStk.vlrssOffset);
        break;

    case ICorDebugInfo::VLT_STK_REG:
        trans.DoEncodedStackOffset(pVar->loc.vlStkReg.vlsrStk.vlsrsOffset);
        trans.DoEncodedRegIdx(pVar->loc.vlStkReg.vlsrStk.vlsrsBaseReg);
        trans.DoEncodedRegIdx(pVar->loc.vlStkReg.vlsrReg);
        break;

    case ICorDebugInfo::VLT_STK2:
        trans.DoEncodedRegIdx(pVar->loc.vlStk2.vls2BaseReg);
        trans.DoEncodedStackOffset(pVar->loc.vlStk2.vls2Offset);
        break;

    case ICorDebugInfo::VLT_FPSTK:
        trans.DoEncodedUnsigned(pVar->loc.vlFPstk.vlfReg);
        break;

    case ICorDebugInfo::VLT_FIXED_VA:
        trans.DoEncodedUnsigned(pVar->loc.vlFixedVarArg.vlfvOffset);
        break;

    default:
        _ASSERTE(!"Unknown varloc type!");
        break;
    }


    trans.DoCookie(0xC);
}

template<typename T>
static void DoInlineTreeNodes(
    T trans,
    ULONG32 cNodes,
    ICorDebugInfo::InlineTreeNode* nodes)
{
    uint32_t lastILOffset = static_cast<uint32_t>(ICorDebugInfo::PROLOG);
    uint32_t lastChildIndex = 0;
    uint32_t lastSiblingIndex = 0;

    for (uint32_t i = 0; i < cNodes; i++)
    {
        ICorDebugInfo::InlineTreeNode* node = &nodes[i];

        trans.DoMethodHandle(node->Method);

        trans.DoEncodedDeltaU32NonMonotonic(node->ILOffset, lastILOffset);
        lastILOffset = node->ILOffset;

        trans.DoEncodedDeltaU32NonMonotonic(node->Child, lastChildIndex);
        lastChildIndex = node->Child;

        trans.DoEncodedDeltaU32NonMonotonic(node->Sibling, lastSiblingIndex);
        lastSiblingIndex = node->Sibling;
    }
}

template<typename T>
static void DoRichOffsetMappings(
    T trans,
    ULONG32 cMappings,
    ICorDebugInfo::RichOffsetMapping* mappings)
{
    // Loop through and transfer each Entry in the Mapping.
    uint32_t lastNativeOffset = 0;
    uint32_t lastInlinee = 0;
    uint32_t lastILOffset = static_cast<uint32_t>(ICorDebugInfo::PROLOG);
    for (uint32_t i = 0; i < cMappings; i++)
    {
        ICorDebugInfo::RichOffsetMapping* mapping = &mappings[i];

        trans.DoEncodedDeltaU32(mapping->NativeOffset, lastNativeOffset);
        lastNativeOffset = mapping->NativeOffset;

        trans.DoEncodedDeltaU32NonMonotonic(mapping->Inlinee, lastInlinee);
        lastInlinee = mapping->Inlinee;

        trans.DoEncodedDeltaU32NonMonotonic(mapping->ILOffset, lastILOffset);
        lastILOffset = mapping->ILOffset;

        trans.DoEncodedSourceType(mapping->Source);
    }
}

enum EXTRA_DEBUG_INFO_FLAGS
{
    // Debug info contains patchpoint information
    EXTRA_DEBUG_INFO_PATCHPOINT = 1,
    // Debug info contains rich information
    EXTRA_DEBUG_INFO_RICH = 2,
};

#ifndef DACCESS_COMPILE

void CompressDebugInfo::CompressBoundaries(
    IN ULONG32                       cMap,
    IN ICorDebugInfo::OffsetMapping *pMap,
    IN OUT NibbleWriter             *pWriter
)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(pWriter != NULL);
    _ASSERTE((pMap == NULL) == (cMap == 0));

    if (cMap != 0)
    {
        pWriter->WriteEncodedU32(cMap);

        TransferWriter t(*pWriter);
        DoBounds(t, cMap, pMap);

        pWriter->Flush();
    }

#ifdef _DEBUG
    DWORD cbBlob;
    PVOID pBlob = pWriter->GetBlob(&cbBlob);

    // Track perf #s for compression...
    g_CDI_TotalMethods++;
    g_CDI_bMethodTotalUncompress += sizeof(ICorDebugInfo::OffsetMapping) * cMap;
    g_CDI_bMethodTotalCompress   += (int) cbBlob;
#endif // _DEBUG
}

void CompressDebugInfo::CompressVars(
    IN ULONG32                         cVars,
    IN ICorDebugInfo::NativeVarInfo    *vars,
    IN OUT NibbleWriter                *pWriter
)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(pWriter != NULL);
    _ASSERTE((cVars == 0) == (vars == NULL));

    if (cVars != 0)
    {
        pWriter->WriteEncodedU32(cVars);

        TransferWriter t(*pWriter);
        for(ULONG32 i = 0; i < cVars; i ++)
        {
            DoNativeVarInfo(t, &vars[i]);
        }

        pWriter->Flush();
    }

#ifdef _DEBUG
    DWORD cbBlob;
    PVOID pBlob = pWriter->GetBlob(&cbBlob);

    g_CDI_bVarsTotalUncompress += cVars * sizeof(ICorDebugInfo::NativeVarInfo);
    g_CDI_bVarsTotalCompress   += (int) cbBlob;
#endif
}

void CompressDebugInfo::CompressRichDebugInfo(
    IN ULONG32                           cInlineTree,
    IN ICorDebugInfo::InlineTreeNode*    pInlineTree,
    IN ULONG32                           cRichOffsetMappings,
    IN ICorDebugInfo::RichOffsetMapping* pRichOffsetMappings,
    IN OUT NibbleWriter*                 pWriter)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(pWriter != NULL);
    _ASSERTE((cInlineTree > 0) || (cRichOffsetMappings > 0));
    pWriter->WriteEncodedU32(cInlineTree);
    pWriter->WriteEncodedU32(cRichOffsetMappings);

    TransferWriter t(*pWriter);
    DoInlineTreeNodes(t, cInlineTree, pInlineTree);
    DoRichOffsetMappings(t, cRichOffsetMappings, pRichOffsetMappings);

    pWriter->Flush();

#ifdef _DEBUG
    DWORD cbBlob;
    PVOID pBlob = pWriter->GetBlob(&cbBlob);

    g_CDI_bRichDebugInfoTotalUncompress += 8 + cInlineTree * sizeof(ICorDebugInfo::InlineTreeNode) + cRichOffsetMappings * sizeof(ICorDebugInfo::RichOffsetMapping);
    g_CDI_bRichDebugInfoTotalCompress   += 4 + cbBlob;
#endif
}

PTR_BYTE CompressDebugInfo::CompressBoundariesAndVars(
    IN ICorDebugInfo::OffsetMapping*     pOffsetMapping,
    IN ULONG                             iOffsetMapping,
    IN ICorDebugInfo::NativeVarInfo*     pNativeVarInfo,
    IN ULONG                             iNativeVarInfo,
    IN PatchpointInfo*                   patchpointInfo,
    IN ICorDebugInfo::InlineTreeNode*    pInlineTree,
    IN ULONG                             iInlineTree,
    IN ICorDebugInfo::RichOffsetMapping* pRichOffsetMappings,
    IN ULONG                             iRichOffsetMappings,
    IN BOOL                              writeFlagByte,
    IN LoaderHeap*                       pLoaderHeap
    )
{
    CONTRACTL {
        THROWS; // compression routines throw
        PRECONDITION((iOffsetMapping == 0) == (pOffsetMapping == NULL));
        PRECONDITION((iNativeVarInfo == 0) == (pNativeVarInfo == NULL));
        PRECONDITION((iInlineTree == 0) || (pInlineTree != NULL));
        PRECONDITION((iRichOffsetMappings == 0) || (pRichOffsetMappings != NULL));
        PRECONDITION(writeFlagByte || ((patchpointInfo == NULL) && (iInlineTree == 0) && (iRichOffsetMappings == 0)));
        PRECONDITION(pLoaderHeap != NULL);
    } CONTRACTL_END;

    // Patchpoint info is currently uncompressed.
    DWORD cbPatchpointInfo = 0;

#ifdef FEATURE_ON_STACK_REPLACEMENT
    if (patchpointInfo != NULL)
    {
        cbPatchpointInfo = patchpointInfo->PatchpointInfoSize();
    }
#else
    _ASSERTE(patchpointInfo == NULL);
#endif

    // Actually do the compression. These will throw on oom.
    NibbleWriter boundsBuffer;
    DWORD cbBounds = 0;
    PVOID pBounds = NULL;
    if (iOffsetMapping > 0)
    {
        CompressDebugInfo::CompressBoundaries(iOffsetMapping, pOffsetMapping, &boundsBuffer);
        pBounds = boundsBuffer.GetBlob(&cbBounds);
    }

    NibbleWriter varsBuffer;
    DWORD cbVars = 0;
    PVOID pVars = NULL;
    if (iNativeVarInfo > 0)
    {
        CompressDebugInfo::CompressVars(iNativeVarInfo, pNativeVarInfo, &varsBuffer);
        pVars = varsBuffer.GetBlob(&cbVars);
    }

    NibbleWriter richDebugInfoBuffer;
    DWORD cbRichDebugInfo = 0;
    PVOID pRichDebugInfo = NULL;
    if ((iInlineTree > 0) || (iRichOffsetMappings > 0))
    {
        CompressDebugInfo::CompressRichDebugInfo(iInlineTree, pInlineTree, iRichOffsetMappings, pRichOffsetMappings, &richDebugInfoBuffer);
        pRichDebugInfo = richDebugInfoBuffer.GetBlob(&cbRichDebugInfo);
    }

    // Now write it all out to the buffer in a compact fashion.
    NibbleWriter w;
    w.WriteEncodedU32(cbBounds);
    w.WriteEncodedU32(cbVars);
    w.Flush();

    DWORD cbHeader;
    PVOID pHeader = w.GetBlob(&cbHeader);

    S_UINT32 cbFinalSize(0);
    if (writeFlagByte)
        cbFinalSize += 1;

    cbFinalSize += cbPatchpointInfo;
    cbFinalSize += S_UINT32(4) + S_UINT32(cbRichDebugInfo);
    cbFinalSize += S_UINT32(cbHeader) + S_UINT32(cbBounds) + S_UINT32(cbVars);

    if (cbFinalSize.IsOverflow())
        ThrowHR(COR_E_OVERFLOW);

    BYTE *ptrStart = (BYTE *)(void *)pLoaderHeap->AllocMem(S_SIZE_T(cbFinalSize.Value()));
    BYTE *ptr = ptrStart;

    if (writeFlagByte)
    {
        BYTE flagByte = 0;
        if (cbPatchpointInfo > 0)
            flagByte |= EXTRA_DEBUG_INFO_PATCHPOINT;
        if (cbRichDebugInfo > 0)
            flagByte |= EXTRA_DEBUG_INFO_RICH;

        *ptr++ = flagByte;
    }

    if (cbPatchpointInfo > 0)
        memcpy(ptr, (BYTE*) patchpointInfo, cbPatchpointInfo);
    ptr += cbPatchpointInfo;

    if (cbRichDebugInfo > 0)
    {
        memcpy(ptr, &cbRichDebugInfo, 4);
        ptr += 4;
        memcpy(ptr, pRichDebugInfo, cbRichDebugInfo);
        ptr += cbRichDebugInfo;
    }

    memcpy(ptr, pHeader, cbHeader);
    ptr += cbHeader;

    if (cbBounds > 0)
        memcpy(ptr, pBounds, cbBounds);
    ptr += cbBounds;

    if (cbVars > 0)
        memcpy(ptr, pVars, cbVars);
    ptr += cbVars;

    return ptrStart;
}

#endif // DACCESS_COMPILE

//-----------------------------------------------------------------------------
// Uncompression (restore) routines
//-----------------------------------------------------------------------------

// Uncompress data supplied by Compress functions.
void CompressDebugInfo::RestoreBoundariesAndVars(
    IN FP_IDS_NEW fpNew, IN void * pNewData,
    IN PTR_BYTE                         pDebugInfo,
    OUT ULONG32                       * pcMap, // number of entries in ppMap
    OUT ICorDebugInfo::OffsetMapping **ppMap, // pointer to newly allocated array
    OUT ULONG32                         *pcVars,
    OUT ICorDebugInfo::NativeVarInfo    **ppVars,
    BOOL hasFlagByte
    )
{
    CONTRACTL
    {
        THROWS; // reading from nibble stream may throw on invalid data.
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    if (pcMap != NULL) *pcMap = 0;
    if (ppMap != NULL) *ppMap = NULL;
    if (pcVars != NULL) *pcVars = 0;
    if (ppVars != NULL) *ppVars = NULL;

    if (hasFlagByte)
    {
        // Check flag byte and skip over any patchpoint info
        BYTE flagByte = *pDebugInfo;
        pDebugInfo++;

        if ((flagByte & EXTRA_DEBUG_INFO_PATCHPOINT) != 0)
        {
            PTR_PatchpointInfo patchpointInfo = dac_cast<PTR_PatchpointInfo>(pDebugInfo);
            pDebugInfo += patchpointInfo->PatchpointInfoSize();
            flagByte &= ~EXTRA_DEBUG_INFO_PATCHPOINT;
        }

        if ((flagByte & EXTRA_DEBUG_INFO_RICH) != 0)
        {
            UINT32 cbRichDebugInfo = *PTR_UINT32(pDebugInfo);
            pDebugInfo += 4;
            pDebugInfo += cbRichDebugInfo;
            flagByte &= ~EXTRA_DEBUG_INFO_RICH;
        }

        _ASSERTE(flagByte == 0);
    }

    NibbleReader r(pDebugInfo, 12 /* maximum size of compressed 2 UINT32s */);

    ULONG cbBounds = r.ReadEncodedU32();
    ULONG cbVars   = r.ReadEncodedU32();

    PTR_BYTE addrBounds = pDebugInfo + r.GetNextByteIndex();
    PTR_BYTE addrVars   = addrBounds + cbBounds;

    if ((pcMap != NULL || ppMap != NULL) && (cbBounds != 0))
    {
        NibbleReader r(addrBounds, cbBounds);
        TransferReader t(r);

        UINT32 cNumEntries = r.ReadEncodedU32();
        _ASSERTE(cNumEntries > 0);

        if (pcMap != NULL)
            *pcMap = cNumEntries;

        if (ppMap != NULL)
        {
            ICorDebugInfo::OffsetMapping * pMap = reinterpret_cast<ICorDebugInfo::OffsetMapping *>
                (fpNew(pNewData, cNumEntries * sizeof(ICorDebugInfo::OffsetMapping)));
            if (pMap == NULL)
            {
                ThrowOutOfMemory();
            }
            *ppMap = pMap;

            // Main decompression routine.
            DoBounds(t, cNumEntries, pMap);
        }
    }

    if ((pcVars != NULL || ppVars != NULL) && (cbVars != 0))
    {
        NibbleReader r(addrVars, cbVars);
        TransferReader t(r);

        UINT32 cNumEntries = r.ReadEncodedU32();
        _ASSERTE(cNumEntries > 0);

        if (pcVars != NULL)
            *pcVars = cNumEntries;

        if (ppVars != NULL)
        {
            ICorDebugInfo::NativeVarInfo * pVars = reinterpret_cast<ICorDebugInfo::NativeVarInfo *>
                (fpNew(pNewData, cNumEntries * sizeof(ICorDebugInfo::NativeVarInfo)));
            if (pVars == NULL)
            {
                ThrowOutOfMemory();
            }
            *ppVars = pVars;

            for(UINT32 i = 0; i < cNumEntries; i++)
            {
                DoNativeVarInfo(t, &pVars[i]);
            }
        }
    }
}

#ifdef FEATURE_ON_STACK_REPLACEMENT

PatchpointInfo * CompressDebugInfo::RestorePatchpointInfo(IN PTR_BYTE pDebugInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    // Check flag byte.
    BYTE flagByte = *pDebugInfo;
    pDebugInfo++;

    if ((flagByte & EXTRA_DEBUG_INFO_PATCHPOINT) == 0)
        return NULL;

    return static_cast<PatchpointInfo*>(PTR_READ(dac_cast<TADDR>(pDebugInfo), dac_cast<PTR_PatchpointInfo>(pDebugInfo)->PatchpointInfoSize()));
}

#endif

void CompressDebugInfo::RestoreRichDebugInfo(
        IN FP_IDS_NEW                          fpNew,
        IN void*                               pNewData,
        IN PTR_BYTE                            pDebugInfo,
        OUT ICorDebugInfo::InlineTreeNode**    ppInlineTree,
        OUT ULONG32*                           pNumInlineTree,
        OUT ICorDebugInfo::RichOffsetMapping** ppRichMappings,
        OUT ULONG32*                           pNumRichMappings)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    BYTE flagByte = *pDebugInfo;
    if ((flagByte & EXTRA_DEBUG_INFO_RICH) == 0)
    {
        *ppInlineTree = NULL;
        *pNumInlineTree = 0;
        *ppRichMappings = NULL;
        *pNumRichMappings = 0;
        return;
    }

    pDebugInfo++;

#ifdef FEATURE_ON_STACK_REPLACEMENT
    if ((flagByte & EXTRA_DEBUG_INFO_PATCHPOINT) != 0)
    {
        PTR_PatchpointInfo patchpointInfo = dac_cast<PTR_PatchpointInfo>(pDebugInfo);
        pDebugInfo += patchpointInfo->PatchpointInfoSize();
    }
#endif

    UINT32 cbRichDebugInfo = *PTR_UINT32(pDebugInfo);
    pDebugInfo += 4;
    NibbleReader r(pDebugInfo, cbRichDebugInfo);

    *pNumInlineTree = r.ReadEncodedU32();
    *pNumRichMappings = r.ReadEncodedU32();

    UINT32 cbInlineTree = *pNumInlineTree * sizeof(ICorDebugInfo::InlineTreeNode);
    *ppInlineTree = reinterpret_cast<ICorDebugInfo::InlineTreeNode*>(fpNew(pNewData, cbInlineTree));
    if (*ppInlineTree == NULL)
        ThrowOutOfMemory();

    UINT32 cbRichOffsetMappings = *pNumRichMappings * sizeof(ICorDebugInfo::RichOffsetMapping);
    *ppRichMappings = reinterpret_cast<ICorDebugInfo::RichOffsetMapping*>(fpNew(pNewData, cbRichOffsetMappings));
    if (*ppRichMappings == NULL)
        ThrowOutOfMemory();

    TransferReader t(r);
    DoInlineTreeNodes(t, *pNumInlineTree, *ppInlineTree);
    DoRichOffsetMappings(t, *pNumRichMappings, *ppRichMappings);
}

#ifdef DACCESS_COMPILE
void CompressDebugInfo::EnumMemoryRegions(CLRDataEnumMemoryFlags flags, PTR_BYTE pDebugInfo, BOOL hasFlagByte)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    PTR_BYTE pStart = pDebugInfo;

    if (hasFlagByte)
    {
        // Check flag byte and skip over any patchpoint info
        BYTE flagByte = *pDebugInfo;
        pDebugInfo++;

        if ((flagByte & EXTRA_DEBUG_INFO_PATCHPOINT) != 0)
        {
            PTR_PatchpointInfo patchpointInfo = dac_cast<PTR_PatchpointInfo>(pDebugInfo);
            pDebugInfo += patchpointInfo->PatchpointInfoSize();
            flagByte &= ~EXTRA_DEBUG_INFO_PATCHPOINT;
        }

        if ((flagByte & EXTRA_DEBUG_INFO_RICH) != 0)
        {
            UINT32 cbRichDebugInfo = *PTR_UINT32(pDebugInfo);
            pDebugInfo += 4;
            pDebugInfo += cbRichDebugInfo;
            flagByte &= ~EXTRA_DEBUG_INFO_RICH;
        }

        _ASSERTE(flagByte == 0);
    }

    NibbleReader r(pDebugInfo, 12 /* maximum size of compressed 2 UINT32s */);

    ULONG cbBounds = r.ReadEncodedU32();
    ULONG cbVars   = r.ReadEncodedU32();

    pDebugInfo += r.GetNextByteIndex() + cbBounds + cbVars;

    DacEnumMemoryRegion(dac_cast<TADDR>(pStart), pDebugInfo - pStart);
}
#endif // DACCESS_COMPILE

// Init given a starting address from the start of code.
void DebugInfoRequest::InitFromStartingAddr(MethodDesc * pMD, PCODE addrCode)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    _ASSERTE(pMD != NULL);
    _ASSERTE(addrCode != NULL);

    this->m_pMD       = pMD;
    this->m_addrStart = addrCode;
}


//-----------------------------------------------------------------------------
// Impl for DebugInfoManager's IDebugInfoStore
//-----------------------------------------------------------------------------
BOOL DebugInfoManager::GetBoundariesAndVars(
    const DebugInfoRequest & request,
    IN FP_IDS_NEW fpNew, IN void * pNewData,
    OUT ULONG32 * pcMap,
    OUT ICorDebugInfo::OffsetMapping ** ppMap,
    OUT ULONG32 * pcVars,
    OUT ICorDebugInfo::NativeVarInfo ** ppVars)
{
    CONTRACTL
    {
        THROWS;
        WRAPPER(GC_TRIGGERS); // depends on fpNew
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    IJitManager* pJitMan = ExecutionManager::FindJitMan(request.GetStartAddress());
    if (pJitMan == NULL)
    {
        return FALSE; // no info available.
    }

    return pJitMan->GetBoundariesAndVars(request, fpNew, pNewData, pcMap, ppMap, pcVars, ppVars);
}

BOOL DebugInfoManager::GetRichDebugInfo(
    const DebugInfoRequest& request,
    IN FP_IDS_NEW fpNew, IN void* pNewData,
    OUT ICorDebugInfo::InlineTreeNode** ppInlineTree,
    OUT ULONG32* pNumInlineTree,
    OUT ICorDebugInfo::RichOffsetMapping** ppRichMappings,
    OUT ULONG32* pNumRichMappings)
{
    CONTRACTL
    {
        THROWS;
        WRAPPER(GC_TRIGGERS); // depends on fpNew
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    IJitManager* pJitMan = ExecutionManager::FindJitMan(request.GetStartAddress());
    if (pJitMan == NULL)
    {
        return FALSE; // no info available.
    }

    return pJitMan->GetRichDebugInfo(request, fpNew, pNewData, ppInlineTree, pNumInlineTree, ppRichMappings, pNumRichMappings);
}

#ifdef DACCESS_COMPILE
void DebugInfoManager::EnumMemoryRegionsForMethodDebugInfo(CLRDataEnumMemoryFlags flags, MethodDesc * pMD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    PCODE addrCode = pMD->GetNativeCode();
    if (addrCode == NULL)
    {
        return;
    }

    IJitManager* pJitMan = ExecutionManager::FindJitMan(addrCode);
    if (pJitMan == NULL)
    {
        return; // no info available.
    }

    pJitMan->EnumMemoryRegionsForMethodDebugInfo(flags, pMD);
}
#endif
