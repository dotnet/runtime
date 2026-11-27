// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "common.h"

#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "PalLimitedContext.h"
#include "regdisplay.h"
#include "ICodeManager.h"
#include "UnixNativeCodeManager.h"
#include "NativePrimitiveDecoder.h"
#include "holder.h"

#include "CommonMacros.inl"

#define GCINFODECODER_NO_EE
#include "gcinfodecoder.cpp"

#include "NativeContext.h"
#include "UnwindHelpers.h"

#include "eventtracebase.h"

#if defined(TARGET_ARM64)
extern "C" void* PacStripPtr(void* ptr);
#endif // TARGET_ARM64

#define UBF_FUNC_KIND_MASK      0x03
#define UBF_FUNC_KIND_ROOT      0x00
#define UBF_FUNC_KIND_HANDLER   0x01
#define UBF_FUNC_KIND_FILTER    0x02

#define UBF_FUNC_HAS_EHINFO             0x04
#define UBF_FUNC_REVERSE_PINVOKE        0x08
#define UBF_FUNC_HAS_ASSOCIATED_DATA    0x10

struct UnixNativeMethodInfo
{
    PTR_VOID pMethodStartAddress;
    PTR_uint8_t pMainLSDA;
    PTR_uint8_t pLSDA;
    PTR_uint8_t pUnwindInfo;
    BodyUnwindSummary* pSummary;
    TADDR frameStart;
    uint32_t frameLength;
    bool packedUnwindInfo;
    bool unwindInfoFromTable;

    bool executionAborted;
};

// Ensure that UnixNativeMethodInfo fits into the space reserved by MethodInfo
static_assert(sizeof(UnixNativeMethodInfo) <= sizeof(MethodInfo), "UnixNativeMethodInfo too big");

UnixNativeCodeManager::UnixNativeCodeManager(TADDR moduleBase,
                                             PTR_VOID pvManagedCodeStartRange, uint32_t cbManagedCodeRange,
                                             PTR_PTR_VOID pClasslibFunctions, uint32_t nClasslibFunctions)
    : m_moduleBase(moduleBase),
      m_pvManagedCodeStartRange(pvManagedCodeStartRange), m_cbManagedCodeRange(cbManagedCodeRange),
      m_pClasslibFunctions(pClasslibFunctions), m_nClasslibFunctions(nClasslibFunctions)
{
}

// Sizes of the header and of a block header, see CompactUnwindTableNode
#define COMPACT_UNWIND_HEADER_SIZE 24
#define COMPACT_UNWIND_BLOCK_SIZE 14

// See CompactUnwindRecordsNode in ILCompiler
struct CompactUnwindRecord
{
    int32_t Start;
    uint32_t Length;
    int32_t Blob;
};

namespace
{
    // Reads the unaligned little endian uint32 at p
    uint32_t ReadCompactUnwindUInt32(PTR_uint8_t p)
    {
        uint32_t value;
        memcpy(&value, dac_cast<PTR_uint8_t>(p), sizeof(value));
        return value;
    }

    // Reads count (at most 32) bits at bit position of the bit stream, least significant bit first. ILC pads the
    // stream with 8 bytes, so reading 8 bytes at any position in the stream stays in the table.
    uint32_t ReadCompactUnwindBits(PTR_uint8_t pStream, uint64_t position, uint32_t count)
    {
        uint64_t bits;
        memcpy(&bits, dac_cast<PTR_uint8_t>(pStream + (position >> 3)), sizeof(bits));
        return (uint32_t)((bits >> (position & 7)) & ((1ull << count) - 1));
    }

    // Resolves the relative pointer at p
    PTR_uint8_t ResolveCompactUnwindRelPtr(const int32_t* p)
    {
        return dac_cast<PTR_uint8_t>((TADDR)p + *p);
    }

    TADDR GetCompactUnwindRecordStart(PTR_uint8_t pRecord)
    {
        return (TADDR)ResolveCompactUnwindRelPtr(&((const CompactUnwindRecord*)pRecord)->Start);
    }

    int CompareCompactUnwindRecords(const void* x, const void* y)
    {
        TADDR xStart = GetCompactUnwindRecordStart(*(PTR_uint8_t const*)x);
        TADDR yStart = GetCompactUnwindRecordStart(*(PTR_uint8_t const*)y);
        return xStart < yStart ? -1 : xStart > yStart ? 1 : 0;
    }
}

bool CompactUnwindTable::Initialize(void* pTable, int length)
{
    PTR_uint8_t p = dac_cast<PTR_uint8_t>(pTable);

    uint32_t entryCount = *dac_cast<PTR_uint32_t>(p + 4);
    uint32_t blockCount = *dac_cast<PTR_uint32_t>(p + 8);
    uint32_t streamBits = *dac_cast<PTR_uint32_t>(p + 12);

    // Blocks follow the header, and the bit stream follows the blocks
    uint32_t blocksOffset = COMPACT_UNWIND_HEADER_SIZE;
    uint32_t streamOffset = blocksOffset + blockCount * COMPACT_UNWIND_BLOCK_SIZE;
    uint32_t offsetsOffset = ALIGN_UP(streamOffset + (streamBits + 7) / 8 + 8, 4);
    uint32_t unwindCount = *dac_cast<PTR_uint32_t>(p + 20);
    m_wideUnwindOffsets = (p[17] & 2) != 0;
    m_unitShift = p[18];
    m_pUnwindOffsets = p + offsetsOffset;
    m_pUnwindInfos = p + ALIGN_UP(offsetsOffset + unwindCount * (m_wideUnwindOffsets ? 4 : 2), 4);
    ASSERT(m_pUnwindInfos <= p + length);
    ASSERT((entryCount == 0) == (blockCount == 0));

    m_pLsdaBase = p + *dac_cast<PTR_int32_t>(p);
    m_packedBits = p[17] & 1;
    m_pStream = p + streamOffset;

    if (blockCount == 0)
        return true;

    // Decode the block headers, and index the blocks by page of code so that a lookup only compares the few
    // blocks of a page. There is about one page per block.
    PTR_uint8_t pBlocks = p + blocksOffset;
    m_codeBase = (TADDR)pBlocks + (int32_t)ReadCompactUnwindUInt32(pBlocks);
    m_pBlocks = new (nothrow) Block[blockCount];
    if (m_pBlocks == NULL)
        return false;

#if defined(TARGET_ARM64) || defined(TARGET_AMD64)
    m_pSummaries = new (nothrow) BodyUnwindSummary[unwindCount]();
    if (m_pSummaries == NULL)
        return false;
#endif

    for (uint32_t i = 0; i < blockCount; i++)
    {
        PTR_uint8_t pBlock = pBlocks + i * COMPACT_UNWIND_BLOCK_SIZE;
        TADDR start = (TADDR)pBlock + (int32_t)ReadCompactUnwindUInt32(pBlock);
        ASSERT(start >= m_codeBase && (uint64_t)(start - m_codeBase) <= UINT32_MAX);
        ASSERT(i == 0 || start > m_codeBase + m_pBlocks[i - 1].Start);

        uint32_t bitOffset = ReadCompactUnwindUInt32(pBlock + 8);
        uint32_t widths = pBlock[12] | ((uint32_t)pBlock[13] << 8);
        uint32_t entryBits = (widths & 0x1F) + ((widths >> 5) & 0x1F) + ((widths >> 10) & 0x1F) + 3;
        uint32_t endBitOffset = i + 1 == blockCount ? streamBits : ReadCompactUnwindUInt32(pBlock + COMPACT_UNWIND_BLOCK_SIZE + 8);
        uint32_t count = (endBitOffset - bitOffset) / entryBits;
        ASSERT(count != 0 && count <= UINT16_MAX && count * entryBits == endBitOffset - bitOffset);

        m_pBlocks[i].Start = (uint32_t)(start - m_codeBase);
        m_pBlocks[i].BlobBase = ReadCompactUnwindUInt32(pBlock + 4);
        m_pBlocks[i].BitOffset = bitOffset;
        m_pBlocks[i].Count = (uint16_t)count;
        m_pBlocks[i].Widths = (uint16_t)widths;
    }

    uint32_t codeSize = m_pBlocks[blockCount - 1].Start;
    uint32_t pageShift = 6;
    while (pageShift < 31 && (codeSize >> pageShift) >= blockCount)
        pageShift++;

    uint32_t pageCount = (codeSize >> pageShift) + 1;
    m_pPageBlocks = new (nothrow) uint32_t[pageCount + 1];
    if (m_pPageBlocks == NULL)
        return false;

    uint32_t block = 0;
    for (uint32_t page = 0; page < pageCount; page++)
    {
        while (block + 1 < blockCount && m_pBlocks[block + 1].Start <= (page << pageShift))
            block++;
        m_pPageBlocks[page] = block;
    }
    m_pPageBlocks[pageCount] = blockCount - 1;

    m_nPages = pageCount;
    m_pageShift = pageShift;
    m_nBlocks = blockCount;
    return true;
}

CompactUnwindTable::~CompactUnwindTable()
{
    delete[] m_pBlocks;
    delete[] m_pPageBlocks;
    delete[] m_pSummaries;
}

bool CompactUnwindTable::FindEntry(TADDR controlPC, CompactUnwindEntry* pEntry) const
{
    if (m_nBlocks == 0 || controlPC < m_codeBase)
        return false;

#ifdef TARGET_64BIT
    if (controlPC - m_codeBase > UINT32_MAX)
        return false;
#endif

    uint32_t offset = (uint32_t)(controlPC - m_codeBase);

    // Find the last block that starts at or before controlPC. It is between the block that contains the start
    // of the page and the one that contains the start of the next page.
    uint32_t page = offset >> m_pageShift;
    if (page >= m_nPages)
        page = m_nPages - 1;

    uint32_t block = m_pPageBlocks[page];
    uint32_t lastBlock = m_pPageBlocks[page + 1];
    while (block < lastBlock && m_pBlocks[block + 1].Start <= offset)
        block++;

    const Block* pBlock = &m_pBlocks[block];
    uint32_t codeBits = (pBlock->Widths & 0x1F) + 1;
    uint32_t lsdaBits = ((pBlock->Widths >> 5) & 0x1F) + 1;
    uint32_t unwindBits = ((pBlock->Widths >> 10) & 0x1F) + 1;
    uint32_t entryBits = codeBits + lsdaBits + unwindBits;
    uint32_t bitOffset = pBlock->BitOffset;
    uint32_t count = pBlock->Count;
    TADDR blockStart = m_codeBase + pBlock->Start;
    offset -= pBlock->Start;

    // Find the last entry of the block that starts at or before controlPC. The first one does.
    uint32_t low = 1;
    uint32_t high = count;
    while (low < high)
    {
        uint32_t mid = low + (high - low) / 2;
        if (ReadCompactUnwindBits(m_pStream, bitOffset + (uint64_t)mid * entryBits, codeBits) <= offset)
            low = mid + 1;
        else
            high = mid;
    }

    uint32_t index = low - 1;
    uint64_t position = bitOffset + (uint64_t)index * entryBits;
    uint32_t lsda = ReadCompactUnwindBits(m_pStream, position + codeBits, lsdaBits);

    // Code without unwind information, such as code between managed code sections, or the end of a section
    if (lsda == 0)
        return false;

    // Each section ends with an entry without an LSDA, so a frame ends at the next entry of the same section
    ASSERT(index + 1 < count || block + 1 < m_nBlocks);

    pEntry->Start = blockStart + ReadCompactUnwindBits(m_pStream, position, codeBits);
    pEntry->pLsda = m_pLsdaBase + pBlock->BlobBase + lsda - 1;
    uint32_t unwind = ReadCompactUnwindBits(m_pStream, position + codeBits + lsdaBits, unwindBits);
    pEntry->Packed = (unwind & m_packedBits) != 0;
    pEntry->FromTable = true;
    unwind >>= m_packedBits;
    uint32_t unwindOffset = m_wideUnwindOffsets
        ? dac_cast<PTR_uint32_t>(m_pUnwindOffsets)[unwind]
        : dac_cast<DPTR(uint16_t)>(m_pUnwindOffsets)[unwind];
    pEntry->pUnwindInfo = m_pUnwindInfos + (unwindOffset << m_unitShift);
    pEntry->pSummary = m_pSummaries != NULL ? m_pSummaries + unwind : NULL;

    TADDR next = index + 1 < count
        ? blockStart + ReadCompactUnwindBits(m_pStream, position + entryBits, codeBits)
        : m_codeBase + m_pBlocks[block + 1].Start;
    pEntry->Length = (uint32_t)(next - pEntry->Start);
    return true;
}

bool UnixNativeCodeManager::AddCompactUnwindTable(void* pTable, int length)
{
    ASSERT(m_pCompactUnwindRecordsStart == NULL);

    CompactUnwindTable* pNewTable = new (nothrow) CompactUnwindTable();
    if (pNewTable == NULL)
        return false;

    if (!pNewTable->Initialize(pTable, length))
    {
        delete pNewTable;
        return false;
    }

    // Each module of a Mach-O multi-file compilation has a table. Modules are registered at startup.
    pNewTable->m_pNext = m_pCompactUnwindTables;
    m_pCompactUnwindTables = pNewTable;
    return true;
}

bool UnixNativeCodeManager::SetCompactUnwindRecords(void* pHeader, int length)
{
    static_assert(sizeof(CompactUnwindRecord) == 12, "Must match CompactUnwindRecordsNode");
    ASSERT(length == 2 * sizeof(int32_t));
    ASSERT(m_pCompactUnwindTables == NULL);

    const int32_t* pRelPtrs = (const int32_t*)pHeader;
    PTR_uint8_t pStart = ResolveCompactUnwindRelPtr(pRelPtrs);
    PTR_uint8_t pEnd = ResolveCompactUnwindRelPtr(pRelPtrs + 1);

    // The linker merges the records of all the modules of the image, which therefore refer to the same range
    if (m_pCompactUnwindRecordsStart != NULL)
    {
        ASSERT(pStart == m_pCompactUnwindRecordsStart && pEnd == m_pCompactUnwindRecordsEnd);
        return true;
    }

    ASSERT((pEnd - pStart) % sizeof(CompactUnwindRecord) == 0);
    uint32_t count = (uint32_t)((pEnd - pStart) / sizeof(CompactUnwindRecord));

    // Linkers usually keep the records in the order of the code, which makes the index unnecessary
    bool sorted = true;
    for (uint32_t i = 1; i < count && sorted; i++)
    {
        sorted = GetCompactUnwindRecordStart(pStart + (i - 1) * sizeof(CompactUnwindRecord)) <=
            GetCompactUnwindRecordStart(pStart + i * sizeof(CompactUnwindRecord));
    }

    if (!sorted)
    {
        PTR_uint8_t* pIndex = new (nothrow) PTR_uint8_t[count];
        if (pIndex == NULL)
            return false;

        for (uint32_t i = 0; i < count; i++)
            pIndex[i] = pStart + i * sizeof(CompactUnwindRecord);

        qsort(pIndex, count, sizeof(PTR_uint8_t), CompareCompactUnwindRecords);
        m_pCompactUnwindRecordIndex = pIndex;
    }

    m_nCompactUnwindRecords = count;
    m_pCompactUnwindRecordsStart = pStart;
    m_pCompactUnwindRecordsEnd = pEnd;
    return true;
}

PTR_uint8_t UnixNativeCodeManager::GetCompactUnwindRecord(uint32_t index)
{
    return m_pCompactUnwindRecordIndex != NULL
        ? m_pCompactUnwindRecordIndex[index]
        : m_pCompactUnwindRecordsStart + index * sizeof(CompactUnwindRecord);
}

bool UnixNativeCodeManager::FindCompactUnwindRecord(TADDR controlPC, CompactUnwindEntry* pEntry)
{
    // Find the last record that starts at or before controlPC
    uint32_t low = 0;
    uint32_t high = m_nCompactUnwindRecords;
    while (low < high)
    {
        uint32_t mid = low + (high - low) / 2;
        if (GetCompactUnwindRecordStart(GetCompactUnwindRecord(mid)) <= controlPC)
            low = mid + 1;
        else
            high = mid;
    }

    if (low == 0)
        return false;

    const CompactUnwindRecord* pRecord = (const CompactUnwindRecord*)GetCompactUnwindRecord(low - 1);
    TADDR start = GetCompactUnwindRecordStart((PTR_uint8_t)pRecord);

    // Code without unwind information, such as linker generated code, isn't covered by any record
    if (controlPC - start >= pRecord->Length)
        return false;

    pEntry->Start = start;
    pEntry->Length = pRecord->Length;
    pEntry->pUnwindInfo = ResolveCompactUnwindRelPtr(&pRecord->Blob);
    pEntry->pLsda = pEntry->pUnwindInfo + UnwindHelpers::GetUnwindInfoSize(pEntry->pUnwindInfo);
    pEntry->Packed = false;
    pEntry->FromTable = false;
    pEntry->pSummary = NULL;
    return true;
}

bool UnixNativeCodeManager::FindCompactUnwindEntry(TADDR controlPC, CompactUnwindEntry* pEntry)
{
    for (const CompactUnwindTable* pTable = m_pCompactUnwindTables; pTable != NULL; pTable = pTable->m_pNext)
    {
        if (pTable->FindEntry(controlPC, pEntry))
            return true;
    }

    if (m_pCompactUnwindRecordsStart != NULL)
        return FindCompactUnwindRecord(controlPC, pEntry);

    return false;
}

extern "C" bool RhpAddCompactUnwindTable(ICodeManager* pCodeManager, void* pTable, int length)
{
    return static_cast<UnixNativeCodeManager*>(pCodeManager)->AddCompactUnwindTable(pTable, length);
}

extern "C" bool RhpSetCompactUnwindRecords(ICodeManager* pCodeManager, void* pHeader, int length)
{
    return static_cast<UnixNativeCodeManager*>(pCodeManager)->SetCompactUnwindRecords(pHeader, length);
}

UnixNativeCodeManager::~UnixNativeCodeManager()
{
    while (m_pCompactUnwindTables != NULL)
    {
        CompactUnwindTable* pTable = m_pCompactUnwindTables;
        m_pCompactUnwindTables = pTable->m_pNext;
        delete pTable;
    }

    delete[] m_pCompactUnwindRecordIndex;
}

// Virtually unwind stack to the caller of the context specified by the REGDISPLAY
bool UnixNativeCodeManager::VirtualUnwind(MethodInfo* pMethodInfo, REGDISPLAY* pRegisterSet, uintptr_t* pSpForPacSign)
{
    UnixNativeMethodInfo * pNativeMethodInfo = (UnixNativeMethodInfo *)pMethodInfo;
    return UnwindHelpers::StepFrame(pRegisterSet, pNativeMethodInfo->frameStart,
        pNativeMethodInfo->frameStart + pNativeMethodInfo->frameLength, pNativeMethodInfo->pUnwindInfo,
        pNativeMethodInfo->packedUnwindInfo, pNativeMethodInfo->unwindInfoFromTable ? pNativeMethodInfo->pLSDA : NULL,
        pNativeMethodInfo->pSummary, pSpForPacSign);
}

bool UnixNativeCodeManager::FindMethodInfo(PTR_VOID        ControlPC,
                                           MethodInfo *    pMethodInfoOut)
{
    // Stackwalker may call this with ControlPC that does not belong to this code manager
    if (dac_cast<TADDR>(ControlPC) < dac_cast<TADDR>(m_pvManagedCodeStartRange) ||
        dac_cast<TADDR>(m_pvManagedCodeStartRange) + m_cbManagedCodeRange <= dac_cast<TADDR>(ControlPC))
    {
        return false;
    }

    UnixNativeMethodInfo * pMethodInfo = (UnixNativeMethodInfo *)pMethodInfoOut;

    // Find LSDA and start address for a function at address controlPC

    CompactUnwindEntry entry;
    if (!FindCompactUnwindEntry((TADDR)ControlPC, &entry))
        return false;

    // The blob of the frame is its unwind information followed by its LSDA
    TADDR startIp = entry.Start;
    PTR_uint8_t pLsda = entry.pLsda;
    pMethodInfo->pUnwindInfo = entry.pUnwindInfo;
    pMethodInfo->packedUnwindInfo = entry.Packed;
    pMethodInfo->unwindInfoFromTable = entry.FromTable;
    pMethodInfo->pSummary = entry.pSummary;
    pMethodInfo->frameStart = entry.Start;
    pMethodInfo->frameLength = entry.Length;

    PTR_uint8_t p = pLsda;

    pMethodInfo->pLSDA = p;

    uint8_t unwindBlockFlags = *p++;

    if ((unwindBlockFlags & UBF_FUNC_KIND_MASK) != UBF_FUNC_KIND_ROOT)
    {
        // Funclets just refer to the main function's blob
        pMethodInfo->pMainLSDA = p + NativePrimitiveDecoder::PeekInt32(p);
        p += sizeof(int32_t);

        pMethodInfo->pMethodStartAddress = dac_cast<PTR_VOID>(startIp - NativePrimitiveDecoder::PeekInt32(p));
    }
    else
    {
        pMethodInfo->pMainLSDA = pLsda;
        pMethodInfo->pMethodStartAddress = dac_cast<PTR_VOID>(startIp);
    }

    pMethodInfo->executionAborted = false;

    return true;
}

bool UnixNativeCodeManager::IsFunclet(MethodInfo * pMethodInfo)
{
    UnixNativeMethodInfo * pNativeMethodInfo = (UnixNativeMethodInfo *)pMethodInfo;

    uint8_t unwindBlockFlags = *(pNativeMethodInfo->pLSDA);
    return (unwindBlockFlags & UBF_FUNC_KIND_MASK) != UBF_FUNC_KIND_ROOT;
}

bool UnixNativeCodeManager::IsFilter(MethodInfo * pMethodInfo)
{
    UnixNativeMethodInfo * pNativeMethodInfo = (UnixNativeMethodInfo *)pMethodInfo;

    uint8_t unwindBlockFlags = *(pNativeMethodInfo->pLSDA);
    return (unwindBlockFlags & UBF_FUNC_KIND_MASK) == UBF_FUNC_KIND_FILTER;
}

PTR_VOID UnixNativeCodeManager::GetFramePointer(MethodInfo *   pMethodInfo,
                                                REGDISPLAY *   pRegisterSet)
{
    UnixNativeMethodInfo * pNativeMethodInfo = (UnixNativeMethodInfo *)pMethodInfo;

    // Return frame pointer for methods with EH and funclets
    uint8_t unwindBlockFlags = *(pNativeMethodInfo->pLSDA);
    if ((unwindBlockFlags & UBF_FUNC_HAS_EHINFO) != 0 || (unwindBlockFlags & UBF_FUNC_KIND_MASK) != UBF_FUNC_KIND_ROOT)
    {
        return (PTR_VOID)pRegisterSet->GetFP();
    }

    return NULL;
}

uint32_t UnixNativeCodeManager::GetCodeOffset(MethodInfo* pMethodInfo, PTR_VOID address, /*out*/ PTR_uint8_t* gcInfo)
{
    UnixNativeMethodInfo* pNativeMethodInfo = (UnixNativeMethodInfo*)pMethodInfo;

    PTR_uint8_t p = pNativeMethodInfo->pMainLSDA;

    uint8_t unwindBlockFlags = *p++;

    if ((unwindBlockFlags & UBF_FUNC_HAS_ASSOCIATED_DATA) != 0)
        p += sizeof(int32_t);

    if ((unwindBlockFlags & UBF_FUNC_HAS_EHINFO) != 0)
        p += sizeof(int32_t);

    *gcInfo = p;

    uint32_t codeOffset = (uint32_t)(PINSTRToPCODE(dac_cast<TADDR>(address)) - PINSTRToPCODE(dac_cast<TADDR>(pNativeMethodInfo->pMethodStartAddress)));
    return codeOffset;
}

bool UnixNativeCodeManager::IsSafePoint(MethodInfo * pMethodInfo, PTR_VOID pvAddress)
{
    PTR_uint8_t gcInfo;
    uint32_t codeOffset = GetCodeOffset(pMethodInfo, pvAddress, &gcInfo);

    GcInfoDecoder decoder(
        GCInfoToken(gcInfo),
        GcInfoDecoderFlags(DECODE_INTERRUPTIBILITY),
        codeOffset
    );

    if (decoder.IsInterruptible())
        return true;

    if (decoder.IsSafePoint())
        return true;

    return false;
}

void UnixNativeCodeManager::EnumGcRefs(MethodInfo *    pMethodInfo,
                                       PTR_VOID        safePointAddress,
                                       REGDISPLAY *    pRegisterSet,
                                       GCEnumContext * hCallback,
                                       bool            isActiveStackFrame)
{
    PTR_uint8_t gcInfo;
    uint32_t codeOffset = GetCodeOffset(pMethodInfo, safePointAddress, &gcInfo);

#ifdef TARGET_ARM
    // Ensure that code offset doesn't have the Thumb bit set. We need
    // it to be aligned to instruction start
    ASSERT(((uintptr_t)codeOffset & 1) == 0);
#endif

    GcInfoDecoder decoder(
        GCInfoToken(gcInfo),
        GcInfoDecoderFlags(DECODE_GC_LIFETIMES | DECODE_SECURITY_OBJECT | DECODE_VARARG),
        codeOffset
    );

    ICodeManagerFlags flags = (ICodeManagerFlags)0;
    bool executionAborted = ((UnixNativeMethodInfo*)pMethodInfo)->executionAborted;
    if (executionAborted)
        flags = ICodeManagerFlags::ExecutionAborted;

    if (IsFilter(pMethodInfo))
        flags = (ICodeManagerFlags)(flags | ICodeManagerFlags::NoReportUntracked);

    if (isActiveStackFrame)
        flags = (ICodeManagerFlags)(flags | ICodeManagerFlags::ActiveStackFrame);

    if (!decoder.EnumerateLiveSlots(
        pRegisterSet,
        isActiveStackFrame /* reportScratchSlots */,
        flags,
        hCallback->pCallback,
        hCallback
        ))
    {
        assert(false);
    }
}

uintptr_t UnixNativeCodeManager::GetConservativeUpperBoundForOutgoingArgs(MethodInfo * pMethodInfo, REGDISPLAY * pRegisterSet)
{
    // Return value
    uintptr_t upperBound;

    UnixNativeMethodInfo * pNativeMethodInfo = (UnixNativeMethodInfo *)pMethodInfo;

    PTR_uint8_t p = pNativeMethodInfo->pLSDA;

    uint8_t unwindBlockFlags = *p++;

    if ((unwindBlockFlags & UBF_FUNC_REVERSE_PINVOKE) != 0)
    {
        // Reverse PInvoke transition should be on the main function body only
        assert(pNativeMethodInfo->pMainLSDA == pNativeMethodInfo->pLSDA);

        if ((unwindBlockFlags & UBF_FUNC_HAS_ASSOCIATED_DATA) != 0)
            p += sizeof(int32_t);

        if ((unwindBlockFlags & UBF_FUNC_HAS_EHINFO) != 0)
            p += sizeof(int32_t);

        GcInfoDecoder decoder(GCInfoToken(p), DECODE_REVERSE_PINVOKE_VAR);
        INT32 slot = decoder.GetReversePInvokeFrameStackSlot();
        assert(slot != NO_REVERSE_PINVOKE_FRAME);

        TADDR basePointer = (TADDR)NULL;
        UINT32 stackBasedRegister = decoder.GetStackBaseRegister();
        if (stackBasedRegister == NO_STACK_BASE_REGISTER)
        {
            basePointer = dac_cast<TADDR>(pRegisterSet->GetSP());
        }
        else
        {
            basePointer = dac_cast<TADDR>(pRegisterSet->GetFP());
        }

        // Reverse PInvoke case.  The embedded reverse PInvoke frame is guaranteed to reside above
        // all outgoing arguments.
        upperBound = (uintptr_t)dac_cast<TADDR>(basePointer + slot);
    }
    else
    {
        // The passed in pRegisterSet should be left intact
        REGDISPLAY localRegisterSet = *pRegisterSet;

        bool result = VirtualUnwind(pMethodInfo, &localRegisterSet);
        assert(result);

        // All common ABIs have outgoing arguments under caller SP (minus slot reserved for return address).
        // There are ABI-specific optimizations that could applied here, but they are not worth the complexity
        // given that this path is used rarely.
#if defined(TARGET_AMD64)
        upperBound = dac_cast<TADDR>(localRegisterSet.GetSP() - sizeof(TADDR));
#else
        upperBound = dac_cast<TADDR>(localRegisterSet.GetSP());
#endif
    }

    return upperBound;
}

bool UnixNativeCodeManager::UnwindStackFrame(MethodInfo *    pMethodInfo,
                                             uint32_t        flags,
                                             REGDISPLAY *    pRegisterSet,                 // in/out
                                             PInvokeTransitionFrame**      ppPreviousTransitionFrame)    // out
{
    UnixNativeMethodInfo * pNativeMethodInfo = (UnixNativeMethodInfo *)pMethodInfo;

    PTR_uint8_t p = pNativeMethodInfo->pLSDA;

    uint8_t unwindBlockFlags = *p++;

    if ((unwindBlockFlags & UBF_FUNC_REVERSE_PINVOKE) != 0)
    {
        // Reverse PInvoke transition should be on the main function body only
        assert(pNativeMethodInfo->pMainLSDA == pNativeMethodInfo->pLSDA);

        if ((unwindBlockFlags & UBF_FUNC_HAS_ASSOCIATED_DATA) != 0)
            p += sizeof(int32_t);

        if ((unwindBlockFlags & UBF_FUNC_HAS_EHINFO) != 0)
            p += sizeof(int32_t);

        GcInfoDecoder decoder(GCInfoToken(p), DECODE_REVERSE_PINVOKE_VAR);
        INT32 slot = decoder.GetReversePInvokeFrameStackSlot();
        assert(slot != NO_REVERSE_PINVOKE_FRAME);

        TADDR basePointer = (TADDR)NULL;
        UINT32 stackBasedRegister = decoder.GetStackBaseRegister();
        if (stackBasedRegister == NO_STACK_BASE_REGISTER)
        {
            basePointer = dac_cast<TADDR>(pRegisterSet->GetSP());
        }
        else
        {
            basePointer = dac_cast<TADDR>(pRegisterSet->GetFP());
        }

        *ppPreviousTransitionFrame = *(PInvokeTransitionFrame**)(basePointer + slot);

        if ((flags & USFF_StopUnwindOnTransitionFrame) != 0)
        {
            return true;
        }
    }
    else
    {
        *ppPreviousTransitionFrame = NULL;
    }

    if (!VirtualUnwind(pMethodInfo, pRegisterSet))
    {
        return false;
    }

#if defined(TARGET_ARM64)
    pRegisterSet->SetIP((PCODE)PacStripPtr((void*)pRegisterSet->GetIP()));
#endif // TARGET_ARM64

    return true;
}

bool UnixNativeCodeManager::IsUnwindable(PTR_VOID pvAddress)
{
    // The Windows unwind information describes the prolog, and the unwinder recognizes the epilogs.
    return true;
}

bool UnixNativeCodeManager::GetReturnAddressHijackInfo(MethodInfo *    pMethodInfo,
                                                       REGDISPLAY *    pRegisterSet,       // in
                                                       PTR_PTR_VOID *  ppvRetAddrLocation, // out
                                                       uintptr_t *     pSpForArm64PacSign) // out
{
    *pSpForArm64PacSign = 0;

    UnixNativeMethodInfo* pNativeMethodInfo = (UnixNativeMethodInfo*)pMethodInfo;

    PTR_uint8_t p = pNativeMethodInfo->pLSDA;

    uint8_t unwindBlockFlags = *p++;

    // Check whether this is a funclet
    if ((unwindBlockFlags & UBF_FUNC_KIND_MASK) != UBF_FUNC_KIND_ROOT)
        return false;

    // Skip hijacking a reverse-pinvoke method - it doesn't get us much because we already synchronize
    // with the GC on the way back to native code.
    if ((unwindBlockFlags & UBF_FUNC_REVERSE_PINVOKE) != 0)
        return false;

    // Unwind the current method context to the caller's context to get its stack pointer
    // and obtain the location of the return address on the stack
#if defined(TARGET_AMD64)
    if (!VirtualUnwind(pMethodInfo, pRegisterSet))
    {
        return false;
    }

    *ppvRetAddrLocation = (PTR_PTR_VOID)(pRegisterSet->GetSP() - sizeof(TADDR));
    return true;

#elif defined(TARGET_ARM64) || defined(TARGET_ARM) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    if ((unwindBlockFlags & UBF_FUNC_HAS_ASSOCIATED_DATA) != 0)
        p += sizeof(int32_t);

    if ((unwindBlockFlags & UBF_FUNC_HAS_EHINFO) != 0)
        p += sizeof(int32_t);

    // Decode the GC info for the current method to determine if there are tailcalls
    GcInfoDecoderFlags flags = DECODE_HAS_TAILCALLS;
    GcInfoDecoder decoder(GCInfoToken(p), flags);
    if (decoder.HasTailCalls())
    {
        // Do not hijack functions that have tail calls, since there are two problems:
        // 1. When a function that tail calls another one is hijacked, the LR may be
        //    stored at a different location in the stack frame of the tail call target.
        //    So just by performing tail call, the hijacked location becomes invalid and
        //    unhijacking would corrupt stack by writing to that location.
        // 2. There is a small window after the caller pops LR from the stack in its
        //    epilog and before the tail called function pushes LR in its prolog when
        //    the hijacked return address would not be not on the stack and so we would
        //    not be able to unhijack.
        return false;
    }

    PTR_uintptr_t oldLocation = pRegisterSet->GetReturnAddressRegisterLocation();
    if (!VirtualUnwind(pMethodInfo, pRegisterSet, pSpForArm64PacSign))
    {
        return false;
    }

    if (pRegisterSet->GetReturnAddressRegisterLocation() == oldLocation)
    {
        // This is the case when we are either:
        //
        // 1) In a leaf method that does not push return address register on stack, OR
        // 2) In the prolog/epilog of a non-leaf method that has not yet pushed return address register on stack
        //    or has return address register already popped off.
        return false;
    }

    *ppvRetAddrLocation = (PTR_PTR_VOID)pRegisterSet->GetReturnAddressRegisterLocation();
    return true;
#else
    return false;
#endif // defined(TARGET_AMD64)
}

PTR_VOID UnixNativeCodeManager::RemapHardwareFaultToGCSafePoint(MethodInfo * pMethodInfo, PTR_VOID controlPC)
{
    // GCInfo decoder needs to know whether execution of the method is aborted
    // while querying for gc-info.  But ICodeManager::EnumGCRef() doesn't receive any
    // flags from mrt. Call to this method is used as a cue to mark the method info
    // as execution aborted. Note - if pMethodInfo was cached, this scheme would not work.
    //
    // If the method has EH, then JIT will make sure the method is fully interruptible
    // and we will have GC-info available at the faulting address as well.

    UnixNativeMethodInfo * pNativeMethodInfo = (UnixNativeMethodInfo *)pMethodInfo;
    pNativeMethodInfo->executionAborted = true;

    return controlPC;
}

struct UnixEHEnumState
{
    PTR_uint8_t pMethodStartAddress;
    PTR_uint8_t pEHInfo;
    uint32_t uClause;
    uint32_t nClauses;
};

// Ensure that UnixEHEnumState fits into the space reserved by EHEnumState
static_assert(sizeof(UnixEHEnumState) <= sizeof(EHEnumState), "UnixEHEnumState too big");

bool UnixNativeCodeManager::EHEnumInit(MethodInfo * pMethodInfo, PTR_VOID * pMethodStartAddress, EHEnumState * pEHEnumStateOut)
{
    assert(pMethodInfo != NULL);
    assert(pMethodStartAddress != NULL);
    assert(pEHEnumStateOut != NULL);

    UnixNativeMethodInfo * pNativeMethodInfo = (UnixNativeMethodInfo *)pMethodInfo;

    PTR_uint8_t p = pNativeMethodInfo->pMainLSDA;

    uint8_t unwindBlockFlags = *p++;

    if ((unwindBlockFlags & UBF_FUNC_HAS_ASSOCIATED_DATA) != 0)
        p += sizeof(int32_t);

    // return if there is no EH info associated with this method
    if ((unwindBlockFlags & UBF_FUNC_HAS_EHINFO) == 0)
    {
        return false;
    }

    UnixEHEnumState * pEnumState = (UnixEHEnumState *)pEHEnumStateOut;

    *pMethodStartAddress = pNativeMethodInfo->pMethodStartAddress;

    pEnumState->pMethodStartAddress = dac_cast<PTR_uint8_t>(pNativeMethodInfo->pMethodStartAddress);
    pEnumState->pEHInfo = dac_cast<PTR_uint8_t>(p + NativePrimitiveDecoder::PeekInt32(p));
    pEnumState->uClause = 0;
    pEnumState->nClauses = NativePrimitiveDecoder::ReadUnsigned(pEnumState->pEHInfo);

    return true;
}

bool UnixNativeCodeManager::EHEnumNext(EHEnumState * pEHEnumState, EHClause * pEHClauseOut)
{
    assert(pEHEnumState != NULL);
    assert(pEHClauseOut != NULL);

    UnixEHEnumState * pEnumState = (UnixEHEnumState *)pEHEnumState;
    if (pEnumState->uClause >= pEnumState->nClauses)
    {
        return false;
    }

    pEnumState->uClause++;

    pEHClauseOut->m_tryStartOffset = NativePrimitiveDecoder::ReadUnsigned(pEnumState->pEHInfo);

    uint32_t tryEndDeltaAndClauseKind = NativePrimitiveDecoder::ReadUnsigned(pEnumState->pEHInfo);
    pEHClauseOut->m_clauseKind = (EHClauseKind)(tryEndDeltaAndClauseKind & 0x3);
    pEHClauseOut->m_tryEndOffset = pEHClauseOut->m_tryStartOffset + (tryEndDeltaAndClauseKind >> 2);

    // For each clause, we have up to 4 integers:
    //      1)  try start offset
    //      2)  (try length << 2) | clauseKind
    //      3)  if (typed || fault || filter)    { handler start offset }
    //      4a) if (typed)                       { type RVA }
    //      4b) if (filter)                      { filter start offset }
    //
    // The first two integers have already been decoded

    switch (pEHClauseOut->m_clauseKind)
    {
    case EH_CLAUSE_TYPED:
        pEHClauseOut->m_handlerAddress = dac_cast<uint8_t*>(PINSTRToPCODE(dac_cast<TADDR>(pEnumState->pMethodStartAddress))) + NativePrimitiveDecoder::ReadUnsigned(pEnumState->pEHInfo);

        // Read target type
        {
            // @TODO: Compress EHInfo using type table index scheme
            // https://github.com/dotnet/corert/issues/972
            uint8_t* pBase = pEnumState->pEHInfo;
            int32_t typeRelAddr = NativePrimitiveDecoder::ReadInt32(pEnumState->pEHInfo);
            pEHClauseOut->m_pTargetType = dac_cast<PTR_VOID>(pBase + typeRelAddr);
        }
        break;
    case EH_CLAUSE_FAULT:
        pEHClauseOut->m_handlerAddress = dac_cast<uint8_t*>(PINSTRToPCODE(dac_cast<TADDR>(pEnumState->pMethodStartAddress))) + NativePrimitiveDecoder::ReadUnsigned(pEnumState->pEHInfo);
        break;
    case EH_CLAUSE_FILTER:
        pEHClauseOut->m_handlerAddress = dac_cast<uint8_t*>(PINSTRToPCODE(dac_cast<TADDR>(pEnumState->pMethodStartAddress))) + NativePrimitiveDecoder::ReadUnsigned(pEnumState->pEHInfo);
        pEHClauseOut->m_filterAddress = dac_cast<uint8_t*>(PINSTRToPCODE(dac_cast<TADDR>(pEnumState->pMethodStartAddress))) + NativePrimitiveDecoder::ReadUnsigned(pEnumState->pEHInfo);
        break;
    default:
        UNREACHABLE_MSG("unexpected EHClauseKind");
    }

    return true;
}

PTR_VOID UnixNativeCodeManager::GetOsModuleHandle()
{
    return (PTR_VOID)m_moduleBase;
}

PTR_VOID UnixNativeCodeManager::GetMethodStartAddress(MethodInfo * pMethodInfo)
{
    UnixNativeMethodInfo * pNativeMethodInfo = (UnixNativeMethodInfo *)pMethodInfo;
    return pNativeMethodInfo->pMethodStartAddress;
}

void * UnixNativeCodeManager::GetClasslibFunction(ClasslibFunctionId functionId)
{
    uint32_t id = (uint32_t)functionId;

    if (id >= m_nClasslibFunctions)
    {
        return nullptr;
    }

    return m_pClasslibFunctions[id];
}

PTR_VOID UnixNativeCodeManager::GetAssociatedData(PTR_VOID ControlPC)
{
    UnixNativeMethodInfo methodInfo;
    if (!FindMethodInfo(ControlPC, (MethodInfo*)&methodInfo))
        return NULL;

    PTR_uint8_t p = methodInfo.pLSDA;

    uint8_t unwindBlockFlags = *p++;

    if ((unwindBlockFlags & UBF_FUNC_KIND_MASK) != UBF_FUNC_KIND_ROOT)
        p += sizeof(uint32_t);

    if ((unwindBlockFlags & UBF_FUNC_HAS_ASSOCIATED_DATA) == 0)
        return NULL;

    return dac_cast<PTR_VOID>(p + NativePrimitiveDecoder::PeekInt32(p));
}

extern "C" void RegisterCodeManager(ICodeManager * pCodeManager, PTR_VOID pvStartRange, uint32_t cbRange);
extern "C" bool RegisterUnboxingStubs(PTR_VOID pvStartRange, uint32_t cbRange);

extern "C"
bool RhRegisterOSModule(void * pModule,
                        void * pvManagedCodeStartRange, uint32_t cbManagedCodeRange,
                        void * pvUnboxingStubsStartRange, uint32_t cbUnboxingStubsRange,
                        void ** pClasslibFunctions, uint32_t nClasslibFunctions)
{
    NewHolder<UnixNativeCodeManager> pUnixNativeCodeManager = new (nothrow) UnixNativeCodeManager((TADDR)pModule,
        pvManagedCodeStartRange, cbManagedCodeRange,
        pClasslibFunctions, nClasslibFunctions);

    if (pUnixNativeCodeManager == nullptr)
        return false;

    RegisterCodeManager(pUnixNativeCodeManager, pvManagedCodeStartRange, cbManagedCodeRange);

    if (!RegisterUnboxingStubs(pvUnboxingStubsStartRange, cbUnboxingStubsRange))
    {
        return false;
    }

    pUnixNativeCodeManager.SuppressRelease();

    ETW::LoaderLog::ModuleLoad(pModule);

    return true;
}
