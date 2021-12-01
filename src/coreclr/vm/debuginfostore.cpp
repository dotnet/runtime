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


    // Some U32 may have a few sentinal negative values .
    // We adjust it to be a real U32 and then encode that.
    // dwAdjust should be the lower bound on the enum.
    void DoEncodedAdjustedU32(uint32_t dw, uint32_t dwAdjust)
    {
        //_ASSERTE(dwAdjust < 0); // some negative lower bound.
        m_w.WriteEncodedU32(dw - dwAdjust);
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
#endif

//-----------------------------------------------------------------------------
// Serialize Bounds info.
//-----------------------------------------------------------------------------
template <class T>
void DoBounds(
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
    // - flags is 3 indepedent bits.

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
void DoNativeVarInfo(
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

PTR_BYTE CompressDebugInfo::CompressBoundariesAndVars(
    IN ICorDebugInfo::OffsetMapping * pOffsetMapping,
    IN ULONG            iOffsetMapping,
    IN ICorDebugInfo::NativeVarInfo * pNativeVarInfo,
    IN ULONG            iNativeVarInfo,
    IN PatchpointInfo * patchpointInfo,
    IN OUT SBuffer    * pDebugInfoBuffer,
    IN LoaderHeap     * pLoaderHeap
    )
{
    CONTRACTL {
        THROWS; // compression routines throw
        PRECONDITION((iOffsetMapping == 0) == (pOffsetMapping == NULL));
        PRECONDITION((iNativeVarInfo == 0) == (pNativeVarInfo == NULL));
        PRECONDITION((pDebugInfoBuffer != NULL) ^ (pLoaderHeap != NULL));
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

    // Now write it all out to the buffer in a compact fashion.
    NibbleWriter w;
    w.WriteEncodedU32(cbBounds);
    w.WriteEncodedU32(cbVars);
    w.Flush();

    DWORD cbHeader;
    PVOID pHeader = w.GetBlob(&cbHeader);

#ifdef FEATURE_ON_STACK_REPLACEMENT
    S_UINT32 cbFinalSize = S_UINT32(1) + S_UINT32(cbPatchpointInfo) + S_UINT32(cbHeader) + S_UINT32(cbBounds) + S_UINT32(cbVars);
#else
    S_UINT32 cbFinalSize = S_UINT32(cbHeader) + S_UINT32(cbBounds) + S_UINT32(cbVars);
#endif

    if (cbFinalSize.IsOverflow())
        ThrowHR(COR_E_OVERFLOW);

    BYTE *ptrStart = NULL;
    if (pLoaderHeap != NULL)
    {
        ptrStart = (BYTE *)(void *)pLoaderHeap->AllocMem(S_SIZE_T(cbFinalSize.Value()));
    }
    else
    {
        // Create a conservatively large buffer to hold all the data.
        ptrStart = pDebugInfoBuffer->OpenRawBuffer(cbFinalSize.Value());
    }
    _ASSERTE(ptrStart != NULL); // throws on oom.

    BYTE *ptr = ptrStart;

#ifdef FEATURE_ON_STACK_REPLACEMENT

    // First byte is a flag byte:
    //   0 - no patchpoint info
    //   1 - patchpoint info

    *ptr++ = (cbPatchpointInfo > 0) ? 1 : 0;

    if (cbPatchpointInfo > 0)
    {
        memcpy(ptr, (BYTE*) patchpointInfo, cbPatchpointInfo);
        ptr += cbPatchpointInfo;
    }

#endif

    memcpy(ptr, pHeader, cbHeader);
    ptr += cbHeader;

    memcpy(ptr, pBounds, cbBounds);
    ptr += cbBounds;

    memcpy(ptr, pVars, cbVars);
    ptr += cbVars;

    if (pLoaderHeap != NULL)
    {
        return ptrStart;
    }
    else
    {
        pDebugInfoBuffer->CloseRawBuffer(cbFinalSize.Value());
        return NULL;
    }
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

#ifdef FEATURE_ON_STACK_REPLACEMENT
    if (hasFlagByte)
    {
        // Check flag byte and skip over any patchpoint info
        BYTE flagByte = *pDebugInfo;
        pDebugInfo++;

        if (flagByte == 1)
        {
            PTR_PatchpointInfo patchpointInfo = dac_cast<PTR_PatchpointInfo>(pDebugInfo);
            pDebugInfo += patchpointInfo->PatchpointInfoSize();
        }
        else
        {
            _ASSERTE(flagByte == 0);
        }
    }

#else
    _ASSERTE(!hasFlagByte);
#endif

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

    PTR_PatchpointInfo patchpointInfo = NULL;

    // Check flag byte.
    BYTE flagByte = *pDebugInfo;
    pDebugInfo++;

    if (flagByte == 1)
    {
        patchpointInfo = dac_cast<PTR_PatchpointInfo>(pDebugInfo);
    }
    else
    {
        _ASSERTE(flagByte == 0);
    }

    return patchpointInfo;
}

#endif

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

#ifdef FEATURE_ON_STACK_REPLACEMENT
    if (hasFlagByte)
    {
        // Check flag byte and skip over any patchpoint info
        BYTE flagByte = *pDebugInfo;
        pDebugInfo++;

        if (flagByte == 1)
        {
            PTR_PatchpointInfo patchpointInfo = dac_cast<PTR_PatchpointInfo>(pDebugInfo);
            pDebugInfo += patchpointInfo->PatchpointInfoSize();
        }
        else
        {
            _ASSERTE(flagByte == 0);
        }
    }
#else
    _ASSERTE(!hasFlagByte);
#endif

    NibbleReader r(pDebugInfo, 12 /* maximum size of compressed 2 UINT32s */);

    ULONG cbBounds = r.ReadEncodedU32();
    ULONG cbVars   = r.ReadEncodedU32();

    DacEnumMemoryRegion(dac_cast<TADDR>(pDebugInfo), r.GetNextByteIndex() + cbBounds + cbVars);
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
