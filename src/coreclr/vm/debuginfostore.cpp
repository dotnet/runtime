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


static uint32_t u32_min(uint32_t a, uint32_t b)
{
    return a < b ? a : b;
}

static int32_t i32_max(int32_t a, int32_t b)
{
    return a > b ? a : b;
}

static uint32_t u32_max(uint32_t a, uint32_t b)
{
    return a > b ? a : b;
}

static uint64_t ReadFromBitOffsets(PTR_UINT64 pArray, uint32_t bitStart, uint32_t bitCount)
{
    uint32_t elementBitSize = (sizeof(uint64_t) * 8);
    _ASSERTE(bitCount <= elementBitSize);
    uint32_t lowOffset = bitStart / elementBitSize;

    uint64_t low = pArray[lowOffset];

    uint32_t bitOffsetInLow = bitStart - lowOffset * elementBitSize;
    uint32_t bitsInLow = u32_min(elementBitSize - bitOffsetInLow, bitCount);


    // Extract LowBits from low
    uint64_t lowShift1 = (low >> (int32_t)bitOffsetInLow);

    uint64_t loBitMask = ((1ULL << (bitsInLow)) - 1);
    uint64_t result = (lowShift1 & loBitMask);

    if (bitsInLow < bitCount)
    {
        uint64_t high = pArray[lowOffset + 1];
        int32_t bitsInHigh = bitCount - bitsInLow;
        uint64_t hiBitMask = ((1ULL << (bitsInHigh)) - 1);
        uint64_t BitsFromHigh = high & hiBitMask;
        uint64_t resultBitsFromHigh = BitsFromHigh << bitsInLow;

        result = result | resultBitsFromHigh;
    }

    return result;
}

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
        dw = m_r.ReadEncodedU32_NoThrow();
    }

    // Use to decode a monotonically increasing delta.
    // dwLast was the last value; we update it to the current value on output.
    void DoEncodedDeltaU32(uint32_t & dw, uint32_t dwLast)
    {
        SUPPORTS_DAC;
        uint32_t dwDelta = m_r.ReadEncodedU32_NoThrow();
        dw = dwLast + dwDelta;
    }

    void DoEncodedAdjustedU32(uint32_t & dw, uint32_t dwAdjust)
    {
        SUPPORTS_DAC;
        //_ASSERTE(dwAdjust < 0);
        dw = m_r.ReadEncodedU32_NoThrow() + dwAdjust;
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
        dw = (ICorDebugInfo::SourceTypes) m_r.ReadEncodedU32_NoThrow();
    }

    void DoEncodedVarLocType(ICorDebugInfo::VarLocType & dw)
    {
        SUPPORTS_DAC;
        dw = (ICorDebugInfo::VarLocType) m_r.ReadEncodedU32_NoThrow();
    }

    void DoEncodedUnsigned(unsigned & dw)
    {
        SUPPORTS_DAC;
        dw = (unsigned) m_r.ReadEncodedU32_NoThrow();
    }


    // Stack offsets are aligned on a DWORD boundary, so that lets us shave off 2 bits.
    void DoEncodedStackOffset(signed & dwOffset)
    {
        SUPPORTS_DAC;
#ifdef TARGET_X86
        dwOffset = m_r.ReadEncodedI32_NoThrow() * sizeof(DWORD);
#else
        // Non x86 platforms don't need it to be dword aligned.
        dwOffset = m_r.ReadEncodedI32_NoThrow();
#endif
    }

    void DoEncodedRegIdx(ICorDebugInfo::RegNum & reg)
    {
        SUPPORTS_DAC;
        reg = (ICorDebugInfo::RegNum) m_r.ReadEncodedU32_NoThrow();
    }

    void DoMethodHandle(CORINFO_METHOD_HANDLE& p)
    {
#ifdef TARGET_64BIT
        uint32_t lo = m_r.ReadUnencodedU32_NoThrow();
        uint32_t hi = m_r.ReadUnencodedU32_NoThrow();
        p = reinterpret_cast<CORINFO_METHOD_HANDLE>(uintptr_t(lo) | (uintptr_t(hi) << 32));
#else
        uint32_t val = m_r.ReadUnencodedU32_NoThrow();
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
            BYTE b2 = m_r.ReadNibble_NoThrow();
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

static int g_CDI_bAsyncDebugInfoTotalUncompress = 0;
static int g_CDI_bAsyncDebugInfoTotalCompress = 0;
#endif

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

template<typename T>
static void DoAsyncSuspensionPoints(
    T trans,
    ULONG32 cSuspensionPoints,
    ICorDebugInfo::AsyncSuspensionPoint* suspensionPoints)
{
    // Loop through and transfer each Entry in the Mapping.
    uint32_t lastRootILOffset = 0;
    uint32_t lastInlinee = 0;
    uint32_t lastILOffset = 0;
    for (uint32_t i = 0; i < cSuspensionPoints; i++)
    {
        ICorDebugInfo::AsyncSuspensionPoint* sp = &suspensionPoints[i];

        trans.DoEncodedDeltaU32NonMonotonic(sp->RootILOffset, lastRootILOffset);
        lastRootILOffset = sp->RootILOffset;

        trans.DoEncodedDeltaU32NonMonotonic(sp->Inlinee, lastInlinee);
        lastInlinee = sp->Inlinee;

        trans.DoEncodedDeltaU32NonMonotonic(sp->ILOffset, lastILOffset);
        lastILOffset = sp->ILOffset;

        trans.DoEncodedU32(sp->NumContinuationVars);
    }
}

template<typename T>
static void DoAsyncVars(
    T trans,
    ULONG32 cVars,
    ICorDebugInfo::AsyncContinuationVarInfo* vars)
{
    uint32_t lastOffset = 0;
    for (uint32_t i = 0; i < cVars; i++)
    {
        ICorDebugInfo::AsyncContinuationVarInfo* var = &vars[i];

        trans.DoEncodedAdjustedU32(var->VarNumber, (DWORD) ICorDebugInfo::MAX_ILNUM);

        trans.DoEncodedDeltaU32(var->Offset, lastOffset);
        lastOffset = var->Offset;
    }
}

#ifndef DACCESS_COMPILE

BYTE* DecompressNew(void*, size_t cBytes)
{
    BYTE* p = (BYTE *)malloc(cBytes);
    return p;
}

void DecompressDelete(void* p)
{
    free(p);
}

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
        // Get max IL offset and native offsets
        uint32_t maxNativeOffsetDelta = 0;
        uint32_t maxILOffset = 0;
        for (ULONG32 i = 0; i < cMap; i++)
        {
            maxILOffset = u32_max(maxILOffset, (uint32_t)((int32_t)pMap[i].ilOffset - (int32_t)ICorDebugInfo::MAX_MAPPING_VALUE));
            uint32_t prevNativeOffset = 0;
            if (i > 0)
            {
                prevNativeOffset = pMap[i - 1].nativeOffset;
            }   
            uint32_t nativeOffsetDelta = pMap[i].nativeOffset - prevNativeOffset;
            maxNativeOffsetDelta = u32_max(nativeOffsetDelta, maxNativeOffsetDelta);
        }
        uint32_t ilOffsetBits = 0;
        if (maxILOffset == 0)
        {
            ilOffsetBits = 1; // always have at least 1 bit
        }
        else
        {
            BitScanReverse((DWORD*)&ilOffsetBits, maxILOffset);
            ilOffsetBits++; // BitScanReverse finds the index of the highest set bit, which is one less than the number of bits needed.
        }
        uint32_t nativeOffsetBits = 0;
        if (maxNativeOffsetDelta == 0)
        {
            nativeOffsetBits = 1; // always have at least 1 bit
        }
        else
        {
            BitScanReverse((DWORD*)&nativeOffsetBits, maxNativeOffsetDelta);
            nativeOffsetBits++; // BitScanReverse finds the index of the highest set bit, which is one less than the number of bits needed.
        }
        
        pWriter->WriteEncodedU32(cMap);
        pWriter->WriteEncodedU32(nativeOffsetBits - 1);
        pWriter->WriteEncodedU32(ilOffsetBits - 1);

        uint32_t bitWidth = 2 + nativeOffsetBits + ilOffsetBits;

        uint8_t bitsInProgress = 0;
        uint8_t bitsInProgressCount = 0;

        for (uint32_t i = 0; i < cMap; i++)
        {
            uint32_t prevNativeOffset = 0;
            if (i > 0)
            {
                prevNativeOffset = pMap[i - 1].nativeOffset;
            }   
            uint32_t nativeOffsetDelta = pMap[i].nativeOffset - prevNativeOffset;

            ICorDebugInfo::OffsetMapping * pBound = &pMap[i];

            uint32_t sourceBits = 0;
            switch ((int)pBound->source)
            {
                case (int)ICorDebugInfo::SOURCE_TYPE_INVALID:
                    sourceBits = 0;
                    break;
                case (int)ICorDebugInfo::CALL_INSTRUCTION:
                    sourceBits = 1;
                    break;
                case (int)ICorDebugInfo::STACK_EMPTY:
                    sourceBits = 2;
                    break;
                case (int)(ICorDebugInfo::CALL_INSTRUCTION | ICorDebugInfo::STACK_EMPTY):
                    sourceBits = 3;
                    break;
                default:
                    _ASSERTE(!"Unknown source type in CompressDebugInfo::CompressBoundaries");
                    sourceBits = 0; // default to invalid
                    break;
            }


            uint64_t mappingDataEncoded = sourceBits | 
                ((uint64_t)nativeOffsetDelta << 2) | 
                ((uint64_t)((int32_t)pBound->ilOffset - (int32_t)ICorDebugInfo::MAX_MAPPING_VALUE) << (2 + nativeOffsetBits));

            for (uint8_t bitsToWrite = (uint8_t)bitWidth; bitsToWrite > 0;)
            {
                // Figure out next bits to write if we need to combine with a previous byte.
                if (bitsInProgressCount > 0)
                {
                    uint8_t bitsToAddFromNewEncoding = 8 - bitsInProgressCount;
                    uint8_t bitsToAddOnToInProgress = (mappingDataEncoded & ((1ULL << bitsToAddFromNewEncoding) - 1)) << bitsInProgressCount;
                    pWriter->WriteRawByte(bitsToAddOnToInProgress | bitsInProgress);
                    mappingDataEncoded >>= bitsToAddFromNewEncoding;
                    bitsToWrite -= bitsToAddFromNewEncoding;
                    bitsInProgressCount = 0;
                }
                else if (bitsToWrite >= 8)
                {
                    pWriter->WriteRawByte((uint8_t)mappingDataEncoded);
                    mappingDataEncoded >>= 8;
                    bitsToWrite -= 8;
                }
                else
                {
                    bitsInProgress = (uint8_t)mappingDataEncoded;
                    bitsInProgressCount = bitsToWrite;
                    bitsToWrite = 0;
                }
            }
        }
        if (bitsInProgressCount > 0)
        {
            _ASSERTE(bitsInProgressCount < 8);
            pWriter->WriteRawByte(bitsInProgress);
        }
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
    g_CDI_bRichDebugInfoTotalCompress   += cbBlob;
#endif
}

void CompressDebugInfo::CompressAsyncDebugInfo(
    IN ICorDebugInfo::AsyncInfo*                asyncInfo,
    IN ICorDebugInfo::AsyncSuspensionPoint*     pSuspensionPoints,
    IN ICorDebugInfo::AsyncContinuationVarInfo* pAsyncVars,
    IN ULONG                                    iAsyncVars,
    IN OUT NibbleWriter*                        pWriter)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(pWriter != NULL);
    _ASSERTE((asyncInfo->NumSuspensionPoints > 0) && (pSuspensionPoints != NULL));
    pWriter->WriteEncodedU32(asyncInfo->NumSuspensionPoints);
    pWriter->WriteEncodedU32(iAsyncVars);

    TransferWriter t(*pWriter);
    DoAsyncSuspensionPoints(t, asyncInfo->NumSuspensionPoints, pSuspensionPoints);
    DoAsyncVars(t, iAsyncVars, pAsyncVars);

    pWriter->Flush();

#ifdef _DEBUG
    DWORD cbBlob;
    PVOID pBlob = pWriter->GetBlob(&cbBlob);

    g_CDI_bAsyncDebugInfoTotalUncompress += 8 + asyncInfo->NumSuspensionPoints * sizeof(ICorDebugInfo::AsyncSuspensionPoint) + iAsyncVars * sizeof(ICorDebugInfo::AsyncContinuationVarInfo);
    g_CDI_bAsyncDebugInfoTotalCompress   += cbBlob;
#endif
}

// ----------------------------------------------------------------------------
// DacDbiInterfaceImpl::TranslateInstrumentedILOffsetToOriginal
//
// Description:
//    Helper function to convert an instrumented IL offset to the corresponding original IL offset.
//
// Arguments:
//    * ilOffset - offset to be translated
//    * pMapping - the profiler-provided mapping between original IL offsets and instrumented IL offsets
//
// Return Value:
//    Return the translated offset.
//


static ULONG TranslateInstrumentedILOffsetToOriginal(ULONG                               ilOffset,
                                                     const InstrumentedILOffsetMapping * pMapping)
{
    SIZE_T               cMap  = pMapping->GetCount();
    ARRAY_PTR_COR_IL_MAP rgMap = pMapping->GetOffsets();

    _ASSERTE((cMap == 0) == (rgMap == NULL));

    // Early out if there is no mapping, or if we are dealing with a special IL offset such as
    // prolog, epilog, etc.
    if ((cMap == 0) || ((int)ilOffset < 0))
    {
        return ilOffset;
    }

    SIZE_T i = 0;
    for (i = 1; i < cMap; i++)
    {
        if (ilOffset < rgMap[i].newOffset)
        {
            return rgMap[i - 1].oldOffset;
        }
    }
    return rgMap[i - 1].oldOffset;
}

//-----------------------------------------------------------------------------
// Given a instrumented IL map from the profiler that maps:
//   Original offset IL_A -> Instrumentend offset IL_B
// And a native mapping from the JIT that maps:
//   Instrumented offset IL_B -> native offset Native_C
// This function merges the two maps and stores the result back into the nativeMap.
// The nativeMap now maps:
//   Original offset IL_A -> native offset Native_C
// pEntryCount is the number of valid entries in nativeMap, and it may be adjusted downwards
// as part of the composition.
//-----------------------------------------------------------------------------
static void ComposeMapping(const InstrumentedILOffsetMapping * pProfilerILMap, ICorDebugInfo::OffsetMapping nativeMap[], ULONG32* pEntryCount)
{
    // Translate the IL offset if the profiler has provided us with a mapping.
    // The ICD public API should always expose the original IL offsets, but GetBoundaries()
    // directly accesses the debug info, which stores the instrumented IL offsets.

    ULONG32 entryCount = *pEntryCount;
    // The map pointer could be NULL or there could be no entries in the map, in either case no work to do
    if (pProfilerILMap && !pProfilerILMap->IsNull())
    {
        // If we did instrument, then we can't have any sequence points that
        // are "in-between" the old-->new map that the profiler gave us.
        // Ex, if map is:
        // (6 old -> 36 new)
        // (8 old -> 50 new)
        // And the jit gives us an entry for 44 new, that will map back to 6 old.
        // Since the map can only have one entry for 6 old, we remove 44 new.

        // First Pass: invalidate all the duplicate entries by setting their IL offset to MAX_ILNUM
        ULONG32 cDuplicate = 0;
        ULONG32 prevILOffset = (ULONG32)(ICorDebugInfo::MAX_ILNUM);
        for (ULONG32 i = 0; i < entryCount; i++)
        {
            ULONG32 origILOffset = TranslateInstrumentedILOffsetToOriginal(nativeMap[i].ilOffset, pProfilerILMap);

            if (origILOffset == prevILOffset)
            {
                // mark this sequence point as invalid; refer to the comment above
                nativeMap[i].ilOffset = (ULONG32)(ICorDebugInfo::MAX_ILNUM);
                cDuplicate += 1;
            }
            else
            {
                // overwrite the instrumented IL offset with the original IL offset
                nativeMap[i].ilOffset = origILOffset;
                prevILOffset = origILOffset;
            }
        }

        // Second Pass: move all the valid entries up front
        ULONG32 realIndex = 0;
        for (ULONG32 curIndex = 0; curIndex < entryCount; curIndex++)
        {
            if (nativeMap[curIndex].ilOffset != (ULONG32)(ICorDebugInfo::MAX_ILNUM))
            {
                // This is a valid entry.  Move it up front.
                nativeMap[realIndex] = nativeMap[curIndex];
                realIndex += 1;
            }
        }

        // make sure we have done the bookkeeping correctly
        _ASSERTE((realIndex + cDuplicate) == entryCount);

        // Final Pass: derecement entryCount
        entryCount -= cDuplicate;
        *pEntryCount = entryCount;
    }
}

PTR_BYTE CompressDebugInfo::Compress(
    IN ICorDebugInfo::OffsetMapping*            pOffsetMapping,
    IN ULONG                                    iOffsetMapping,
    const InstrumentedILOffsetMapping *         pInstrumentedILBounds,
    IN ICorDebugInfo::NativeVarInfo*            pNativeVarInfo,
    IN ULONG                                    iNativeVarInfo,
    IN PatchpointInfo*                          patchpointInfo,
    IN ICorDebugInfo::InlineTreeNode*           pInlineTree,
    IN ULONG                                    iInlineTree,
    IN ICorDebugInfo::RichOffsetMapping*        pRichOffsetMappings,
    IN ULONG                                    iRichOffsetMappings,
    IN ICorDebugInfo::AsyncInfo*                asyncInfo,
    IN ICorDebugInfo::AsyncSuspensionPoint*     pSuspensionPoints,
    IN ICorDebugInfo::AsyncContinuationVarInfo* pAsyncVars,
    IN ULONG                                    iAsyncVars,
    IN LoaderHeap*                              pLoaderHeap
    )
{
    CONTRACTL {
        THROWS; // compression routines throw
        PRECONDITION((iOffsetMapping == 0) == (pOffsetMapping == NULL));
        PRECONDITION((iNativeVarInfo == 0) == (pNativeVarInfo == NULL));
        PRECONDITION((iInlineTree == 0) || (pInlineTree != NULL));
        PRECONDITION((iRichOffsetMappings == 0) || (pRichOffsetMappings != NULL));
        PRECONDITION((asyncInfo->NumSuspensionPoints == 0) || (pSuspensionPoints != NULL));
        PRECONDITION((iAsyncVars == 0) || (pAsyncVars != NULL));
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

    NibbleWriter instrumentedILBoundsBuffer;
    DWORD cbUninstrumentedBounds = 0;
    PVOID pUninstrumentedBounds = NULL;
    if (pInstrumentedILBounds != NULL)
    {
        NewArrayHolder<ICorDebugInfo::OffsetMapping> pInstrumentedILBoundsArray(
            new ICorDebugInfo::OffsetMapping[iOffsetMapping]);

        memcpy(pInstrumentedILBoundsArray, pOffsetMapping, iOffsetMapping * sizeof(ICorDebugInfo::OffsetMapping));

        uint32_t instrumentedEntryCount = iOffsetMapping;
        ComposeMapping(pInstrumentedILBounds, pInstrumentedILBoundsArray, &instrumentedEntryCount);

        CompressDebugInfo::CompressBoundaries(instrumentedEntryCount, pInstrumentedILBoundsArray, &instrumentedILBoundsBuffer);
        pUninstrumentedBounds = instrumentedILBoundsBuffer.GetBlob(&cbUninstrumentedBounds);
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

    NibbleWriter asyncInfoBuffer;
    DWORD cbAsyncInfo = 0;
    PVOID pAsyncInfoBlob = NULL;
    if (asyncInfo->NumSuspensionPoints > 0)
    {
        CompressDebugInfo::CompressAsyncDebugInfo(asyncInfo, pSuspensionPoints, pAsyncVars, iAsyncVars, &asyncInfoBuffer);
        pAsyncInfoBlob = asyncInfoBuffer.GetBlob(&cbAsyncInfo);
    }

    // Now write it all out to the buffer in a compact fashion.
    NibbleWriter w;

    bool isFat =
        (cbPatchpointInfo > 0) ||
        (cbRichDebugInfo > 0) ||
        (cbAsyncInfo > 0) ||
        (cbUninstrumentedBounds > 0);

    if (isFat)
    {
        w.WriteEncodedU32(DebugInfoFat);// 0xFFFFFFFF is used to indicate that this is a fat header
        w.WriteEncodedU32(cbBounds);
        w.WriteEncodedU32(cbVars);
        w.WriteEncodedU32(cbUninstrumentedBounds);
        w.WriteEncodedU32(cbPatchpointInfo);
        w.WriteEncodedU32(cbRichDebugInfo);
        w.WriteEncodedU32(cbAsyncInfo);
    }
    else
    {
        w.WriteEncodedU32(cbBounds);
        w.WriteEncodedU32(cbVars);
    }

    w.Flush();

    DWORD cbHeader;
    PVOID pHeader = w.GetBlob(&cbHeader);

    S_UINT32 cbFinalSize(0);
    cbFinalSize += cbHeader;
    cbFinalSize += cbBounds;
    cbFinalSize += cbVars;
    cbFinalSize += cbUninstrumentedBounds;
    cbFinalSize += cbPatchpointInfo;
    cbFinalSize += cbRichDebugInfo;
    cbFinalSize += cbAsyncInfo;

    if (cbFinalSize.IsOverflow())
        ThrowHR(COR_E_OVERFLOW);

    BYTE *ptrStart = (BYTE *)(void *)pLoaderHeap->AllocMem(S_SIZE_T(cbFinalSize.Value()));
    BYTE *ptr = ptrStart;

    memcpy(ptr, pHeader, cbHeader);
    ptr += cbHeader;

    if (cbBounds > 0)
        memcpy(ptr, pBounds, cbBounds);
    ptr += cbBounds;

    if (cbVars > 0)
        memcpy(ptr, pVars, cbVars);
    ptr += cbVars;

    if (cbUninstrumentedBounds > 0)
        memcpy(ptr, pUninstrumentedBounds, cbUninstrumentedBounds);
    ptr += cbUninstrumentedBounds;

    if (cbPatchpointInfo > 0)
        memcpy(ptr, (BYTE*) patchpointInfo, cbPatchpointInfo);
    ptr += cbPatchpointInfo;

    if (cbRichDebugInfo > 0)
        memcpy(ptr, pRichDebugInfo, cbRichDebugInfo);
    ptr += cbRichDebugInfo;

    if (cbAsyncInfo > 0)
        memcpy(ptr, pAsyncInfoBlob, cbAsyncInfo);
    ptr += cbAsyncInfo;

#ifdef _DEBUG
    ULONG32 cNewBounds = 0;
    ULONG32 cNewVars = 0;
    ICorDebugInfo::OffsetMapping *pNewMap = NULL;
    ICorDebugInfo::NativeVarInfo *pNewVars = NULL;
    RestoreBoundariesAndVars(
        DecompressNew, NULL, BoundsType::Instrumented, ptrStart, &cNewBounds, &pNewMap, &cNewVars, &pNewVars);

    _ASSERTE(cNewBounds == iOffsetMapping);
    _ASSERTE(cNewBounds == 0 || pNewMap != NULL);
    for (ULONG32 i = 0; i < cNewBounds; i++)
    {
        _ASSERTE(pNewMap[i].nativeOffset == pOffsetMapping[i].nativeOffset);
        _ASSERTE(pNewMap[i].ilOffset == pOffsetMapping[i].ilOffset);
        _ASSERTE(pNewMap[i].source == pOffsetMapping[i].source);
    }

    _ASSERTE(cNewVars == iNativeVarInfo);
    _ASSERTE(cNewVars == 0 || pNewVars != NULL);

    for (ULONG32 i = 0; i < iNativeVarInfo; i++)
    {
        _ASSERTE(pNewVars[i].startOffset == pNativeVarInfo[i].startOffset);
        _ASSERTE(pNewVars[i].endOffset == pNativeVarInfo[i].endOffset);
        _ASSERTE(pNewVars[i].varNumber == pNativeVarInfo[i].varNumber);
        _ASSERTE(pNewVars[i].loc == pNativeVarInfo[i].loc);
    }

    if (pNewMap != NULL)
    {
        DecompressDelete(pNewMap);
    }
    if (pNewVars != NULL)
    {
        DecompressDelete(pNewVars);
    }
#endif // _DEBUG

    return ptrStart;
}

#endif // DACCESS_COMPILE

template <typename TNumBounds, typename TPerBound>
static void DoBounds(PTR_BYTE addrBounds, uint32_t cbBounds, TNumBounds countHandler, TPerBound boundHandler)
{
    NibbleReader r(addrBounds, cbBounds);
    uint32_t cNumEntries = r.ReadEncodedU32_NoThrow();//            writer2.WriteUInt((uint)offsetMapping.Length); // We need the total count
    uint32_t bitsForNativeDelta = r.ReadEncodedU32_NoThrow() + 1; // Number of bits needed for native deltas
    uint32_t bitsForILOffsets = r.ReadEncodedU32_NoThrow() + 1; // How many bits needed for IL offsets

    uint32_t bitsPerEntry = bitsForNativeDelta + bitsForILOffsets + 2; // 2 bits for source type
    TADDR addrBoundsArray = dac_cast<TADDR>(addrBounds) + r.GetNextByteIndex();
    TADDR addrBoundsArrayForReads = AlignDown(addrBoundsArray, sizeof(uint64_t));
    uint32_t bitOffsetForReads = (uint32_t)((addrBoundsArray - addrBoundsArrayForReads) * 8); // We want to read using aligned 64bit reads, but we want to start at the right bit offset.
    uint32_t currentNativeOffset = 0;
    ICorDebugInfo::OffsetMapping bound;

    _ASSERTE(cNumEntries > 0);

    if (countHandler(cNumEntries))
    {
        for (uint32_t iEntry = 0; iEntry < cNumEntries; iEntry++, bitOffsetForReads += bitsPerEntry)
        {
            uint64_t mappingDataEncoded = ReadFromBitOffsets(dac_cast<PTR_UINT64>(addrBoundsArrayForReads), bitOffsetForReads, bitsPerEntry);
            switch (mappingDataEncoded & 0x3) // Last 2 bits are source type
            {
                case 0:
                    bound.source = ICorDebugInfo::SOURCE_TYPE_INVALID;
                    break;
                case 1:
                    bound.source = ICorDebugInfo::CALL_INSTRUCTION;
                    break;
                case 2:
                    bound.source = ICorDebugInfo::STACK_EMPTY;
                    break;
                case 3:
                    bound.source = (ICorDebugInfo::SourceTypes)(ICorDebugInfo::STACK_EMPTY | ICorDebugInfo::CALL_INSTRUCTION);
                    break;
            }

            mappingDataEncoded = mappingDataEncoded >> 2; // Remove source type bits
            uint32_t nativeOffsetDelta = (uint32_t)(mappingDataEncoded & ((1ULL << bitsForNativeDelta) - 1));
            currentNativeOffset += nativeOffsetDelta;
            bound.nativeOffset = currentNativeOffset;

            mappingDataEncoded = mappingDataEncoded >> bitsForNativeDelta; // Remove native offset delta bits
            bound.ilOffset = (uint32_t)((uint32_t)mappingDataEncoded + (uint32_t)ICorDebugInfo::MAX_MAPPING_VALUE);
            if (!boundHandler(bound))
                return;
        }
    }
}

//-----------------------------------------------------------------------------
// Uncompression (restore) routines
//-----------------------------------------------------------------------------

DebugInfoChunks CompressDebugInfo::Restore(IN PTR_BYTE pDebugInfo)
{
    CONTRACTL
    {
        THROWS; // reading from nibble stream may throw on invalid data.
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    NibbleReader r(pDebugInfo, 42 /* maximum size of compressed 7 UINT32s */);

    ULONG cbBoundsOrFatMarker = r.ReadEncodedU32();

    DebugInfoChunks chunks;

    if (cbBoundsOrFatMarker == DebugInfoFat)
    {
        // Fat header
        chunks.cbBounds = r.ReadEncodedU32();
        chunks.cbVars = r.ReadEncodedU32();
        chunks.cbUninstrumentedBounds = r.ReadEncodedU32();
        chunks.cbPatchpointInfo = r.ReadEncodedU32();
        chunks.cbRichDebugInfo = r.ReadEncodedU32();
        chunks.cbAsyncInfo = r.ReadEncodedU32();
    }
    else
    {
        chunks.cbBounds = cbBoundsOrFatMarker;
        chunks.cbVars = r.ReadEncodedU32();
        chunks.cbUninstrumentedBounds = 0;
        chunks.cbPatchpointInfo = 0;
        chunks.cbRichDebugInfo = 0;
        chunks.cbAsyncInfo = 0;
    }

    chunks.pBounds = pDebugInfo + r.GetNextByteIndex();
    chunks.pVars = chunks.pBounds + chunks.cbBounds;
    chunks.pUninstrumentedBounds = chunks.pVars + chunks.cbVars;
    chunks.pPatchpointInfo = chunks.pUninstrumentedBounds + chunks.cbUninstrumentedBounds;
    chunks.pRichDebugInfo = chunks.pPatchpointInfo + chunks.cbPatchpointInfo;
    chunks.pAsyncInfo = chunks.pRichDebugInfo + chunks.cbRichDebugInfo;
    chunks.pEnd = chunks.pAsyncInfo + chunks.cbAsyncInfo;
    return chunks;
}

// Uncompress data supplied by Compress functions.
void CompressDebugInfo::RestoreBoundariesAndVars(
    IN FP_IDS_NEW fpNew,
    IN void * pNewData,
    BoundsType boundsType,
    IN PTR_BYTE                         pDebugInfo,
    OUT ULONG32                       * pcMap, // number of entries in ppMap
    OUT ICorDebugInfo::OffsetMapping **ppMap, // pointer to newly allocated array
    OUT ULONG32                         *pcVars,
    OUT ICorDebugInfo::NativeVarInfo    **ppVars
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

    DebugInfoChunks chunks = Restore(pDebugInfo);

    PTR_BYTE addrBounds = chunks.pBounds;
    unsigned cbBounds = chunks.cbBounds;

    if ((boundsType == BoundsType::Uninstrumented) && chunks.cbUninstrumentedBounds != 0)
    {
        // If we have uninstrumented bounds, we will use them instead of the regular bounds.
        addrBounds = chunks.pUninstrumentedBounds;
        cbBounds = chunks.cbUninstrumentedBounds;
    }

    if ((pcMap != NULL || ppMap != NULL) && (cbBounds != 0))
    {
        uint32_t iEntry = 0;
        DoBounds(addrBounds, cbBounds,
            [fpNew, pNewData, &pcMap, &ppMap](uint32_t cNumEntries) 
            {
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
                    return true;
                }
                return false;
            },
            [&iEntry, &ppMap](ICorDebugInfo::OffsetMapping bound) 
            {
                (*ppMap)[iEntry++] = bound;
                return true;
            });
    }

    if ((pcVars != NULL || ppVars != NULL) && (chunks.cbVars != 0))
    {
        NibbleReader r(chunks.pVars, chunks.cbVars);
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

size_t CompressDebugInfo::WalkILOffsets(
    IN PTR_BYTE pDebugInfo,
    BoundsType boundsType,
    void* pContext,
    size_t (* pfnWalkILOffsets)(ICorDebugInfo::OffsetMapping *pOffsetMapping, void *pContext)
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

    DebugInfoChunks chunks = Restore(pDebugInfo);

    PTR_BYTE addrBounds = chunks.pBounds;
    unsigned cbBounds = chunks.cbBounds;

    if ((boundsType == BoundsType::Uninstrumented) && chunks.cbUninstrumentedBounds != 0)
    {
        // If we have uninstrumented bounds, we will use them instead of the regular bounds.
        addrBounds = chunks.pUninstrumentedBounds;
        cbBounds = chunks.cbUninstrumentedBounds;
    }

    if (cbBounds != 0)
    {
        size_t callbackResult = 0;
        DoBounds(addrBounds, cbBounds,
            [](uint32_t cNumEntries) 
            {
                return true;
            },
            [&callbackResult, pfnWalkILOffsets, pContext](ICorDebugInfo::OffsetMapping bound) 
            {
                callbackResult = pfnWalkILOffsets(&bound, pContext);
                return callbackResult == 0;
            }
            );
        if (callbackResult != 0)
        {
            // We have a callback that wants to stop the walk.
            return callbackResult;
        }

        ICorDebugInfo::OffsetMapping bound;
        bound.nativeOffset = 0xFFFFFFFF;
        bound.ilOffset = ICorDebugInfo::NO_MAPPING;
        bound.source = ICorDebugInfo::SOURCE_TYPE_INVALID;

        return pfnWalkILOffsets(&bound, pContext);
    }

    return 0;
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

    DebugInfoChunks chunks = Restore(pDebugInfo);

    if (chunks.cbPatchpointInfo == 0)
        return NULL;

    return static_cast<PatchpointInfo*>(PTR_READ(dac_cast<TADDR>(chunks.pPatchpointInfo), chunks.cbPatchpointInfo));
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

    DebugInfoChunks chunks = Restore(pDebugInfo);

    NibbleReader r(chunks.pRichDebugInfo, chunks.cbRichDebugInfo);

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

    DebugInfoChunks chunks = Restore(pDebugInfo);

    // NibbleReader reads in units of sizeof(NibbleChunkType)
    // So we need to account for any partial chunk at the end.
    PTR_BYTE pEnd = AlignUp(dac_cast<TADDR>(chunks.pEnd), sizeof(NibbleReader::NibbleChunkType));

    DacEnumMemoryRegion(dac_cast<TADDR>(pStart), pEnd - pStart);
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
    _ASSERTE(addrCode != (PCODE)NULL);

    this->m_pMD       = pMD;
    this->m_addrStart = addrCode;
}


//-----------------------------------------------------------------------------
// Impl for DebugInfoManager's IDebugInfoStore
//-----------------------------------------------------------------------------
BOOL DebugInfoManager::GetBoundariesAndVars(
    const DebugInfoRequest & request,
    IN FP_IDS_NEW fpNew,
    IN void * pNewData,
    BoundsType boundsType,
    OUT ULONG32 * pcMap,
    OUT ICorDebugInfo::OffsetMapping ** ppMap,
    OUT ULONG32 * pcVars,
    OUT ICorDebugInfo::NativeVarInfo ** ppVars)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    IJitManager* pJitMan = ExecutionManager::FindJitMan(request.GetStartAddress());
    if (pJitMan == NULL)
    {
        return FALSE; // no info available.
    }

    return pJitMan->GetBoundariesAndVars(request, fpNew, pNewData, boundsType, pcMap, ppMap, pcVars, ppVars);
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
void DebugInfoManager::EnumMemoryRegionsForMethodDebugInfo(CLRDataEnumMemoryFlags flags, EECodeInfo * pCodeInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    if (!pCodeInfo->IsValid())
    {
        return;
    }

    PCODE addrCode = pCodeInfo->GetStartAddress();
    if (addrCode == (PCODE)NULL)
    {
        return;
    }

    IJitManager* pJitMan = pCodeInfo->GetJitManager();
    if (pJitMan == NULL)
    {
        return; // no info available.
    }

    pJitMan->EnumMemoryRegionsForMethodDebugInfo(flags, pCodeInfo);
}
#endif
