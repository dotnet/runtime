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
    const CompactUnwindDescriptor* pDescriptor;

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

// Size of a block header, see CompactUnwindTableNode
#define COMPACT_UNWIND_BLOCK_SIZE 14

// See CompactUnwindRecordsNode in ILCompiler
struct CompactUnwindRecord
{
    int32_t Start;
    uint32_t Length;
    int32_t Lsda;
    int32_t Descriptor;
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
    static_assert(sizeof(CompactUnwindDescriptor) == 12, "Must match CompactUnwindTableNode");
    static_assert(sizeof(CompactUnwindDescriptor::SavedRegister) == 4, "Must match CompactUnwindTableNode");
    static_assert(sizeof(CompactUnwindDescriptor::WideSavedRegister) == 8, "Must match CompactUnwindTableNode");

    PTR_uint8_t p = dac_cast<PTR_uint8_t>(pTable);

    uint32_t entryCount = *dac_cast<PTR_uint32_t>(p + 4);
    uint32_t descriptorCount = *dac_cast<PTR_uint32_t>(p + 8);
    uint32_t blockCount = *dac_cast<PTR_uint32_t>(p + 12);
    uint32_t streamBits = *dac_cast<PTR_uint32_t>(p + 16);
    const uint32_t* pDescriptorOffsets = dac_cast<PTR_uint32_t>(p + 24);

    // Blocks follow the last descriptor, and the bit stream follows the blocks
    uint32_t blocksOffset = 24 + descriptorCount * sizeof(uint32_t);
    if (descriptorCount != 0)
    {
        const CompactUnwindDescriptor* pLast = (const CompactUnwindDescriptor*)(p + pDescriptorOffsets[descriptorCount - 1]);
        blocksOffset = pDescriptorOffsets[descriptorCount - 1] + pLast->GetSize();
    }
    uint32_t streamOffset = blocksOffset + blockCount * COMPACT_UNWIND_BLOCK_SIZE;
    ASSERT(streamOffset + (streamBits + 7) / 8 <= (uint32_t)length);
    ASSERT((entryCount == 0) == (blockCount == 0));

    m_pTable = p;
    m_pLsdaBase = p + *dac_cast<PTR_int32_t>(p);
    m_pDescriptorOffsets = pDescriptorOffsets;
    m_nDescriptors = descriptorCount;
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
        m_pBlocks[i].LsdaBase = ReadCompactUnwindUInt32(pBlock + 4);
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
    uint32_t indexBits = ((pBlock->Widths >> 10) & 0x1F) + 1;
    uint32_t entryBits = codeBits + lsdaBits + indexBits;
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
    uint32_t descriptorIndex = ReadCompactUnwindBits(m_pStream, position + codeBits + lsdaBits, indexBits);

    // Code without unwind information, such as code between managed code sections, or the end of a section
    if (descriptorIndex == m_nDescriptors)
        return false;

    // Each section ends with an entry without a descriptor, so a frame ends at the next entry of the same section
    ASSERT(descriptorIndex < m_nDescriptors);
    ASSERT(index + 1 < count || block + 1 < m_nBlocks);

    pEntry->Start = blockStart + ReadCompactUnwindBits(m_pStream, position, codeBits);
    pEntry->pLsda = m_pLsdaBase + pBlock->LsdaBase + ReadCompactUnwindBits(m_pStream, position + codeBits, lsdaBits);
    pEntry->pDescriptor = (const CompactUnwindDescriptor*)(m_pTable + m_pDescriptorOffsets[descriptorIndex]);

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
    static_assert(sizeof(CompactUnwindRecord) == 16, "Must match CompactUnwindRecordsNode");
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
    pEntry->pLsda = ResolveCompactUnwindRelPtr(&pRecord->Lsda);
    pEntry->pDescriptor = (const CompactUnwindDescriptor*)ResolveCompactUnwindRelPtr(&pRecord->Descriptor);
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

bool UnixNativeCodeManager::IsInCompactUnwindProlog(PTR_VOID pvAddress)
{
    CompactUnwindEntry entry;
    if (!FindCompactUnwindEntry((TADDR)pvAddress, &entry))
        return false;

    return ((TADDR)pvAddress - entry.Start) < entry.pDescriptor->PrologSize;
}

extern "C" bool RhpAddCompactUnwindTable(ICodeManager* pCodeManager, void* pTable, int length)
{
    return static_cast<UnixNativeCodeManager*>(pCodeManager)->AddCompactUnwindTable(pTable, length);
}

extern "C" bool RhpSetCompactUnwindRecords(ICodeManager* pCodeManager, void* pHeader, int length)
{
    return static_cast<UnixNativeCodeManager*>(pCodeManager)->SetCompactUnwindRecords(pHeader, length);
}

bool UnixNativeCodeManager::GetFunctionBounds(TADDR controlPC, TADDR* pStart, TADDR* pEnd)
{
    CompactUnwindEntry entry;
    if (!FindCompactUnwindEntry(controlPC, &entry))
        return false;

    *pStart = entry.Start;
    *pEnd = entry.Start + entry.Length;
    return true;
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
bool UnixNativeCodeManager::VirtualUnwind(MethodInfo* pMethodInfo, REGDISPLAY* pRegisterSet)
{
    UnixNativeMethodInfo * pNativeMethodInfo = (UnixNativeMethodInfo *)pMethodInfo;
    return UnwindHelpers::StepFrame(pRegisterSet, pNativeMethodInfo->pDescriptor);
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

    TADDR startIp = entry.Start;
    PTR_uint8_t pLsda = entry.pLsda;
    pMethodInfo->pDescriptor = entry.pDescriptor;

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
    MethodInfo * pMethodInfo = NULL;

#if defined(TARGET_ARM)
    ASSERT(((uintptr_t)pvAddress & 1) == 0);
#endif

#if defined(TARGET_ARM64) || defined(TARGET_ARM) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    MethodInfo methodInfo;
    FindMethodInfo(pvAddress, &methodInfo);
    pMethodInfo = &methodInfo;
#endif

    // Compact unwind descriptors only describe the frame after the prolog.
    if (IsInCompactUnwindProlog(pvAddress))
        return false;

    // VirtualUnwind can't unwind epilogues.
    return TrailingEpilogueInstructionsCount(pMethodInfo, pvAddress) == 0;
}

// when stopped in an epilogue, returns the count of remaining stack-consuming instructions
// otherwise returns
//  0 - not in epilogue,
// -1 - unknown.
int UnixNativeCodeManager::TrailingEpilogueInstructionsCount(MethodInfo * pMethodInfo, PTR_VOID pvAddress)
{
#ifdef TARGET_AMD64

#define SIZE64_PREFIX 0x48
#define ADD_IMM8_OP 0x83
#define ADD_IMM32_OP 0x81
#define JMP_IMM8_OP 0xeb
#define JMP_IMM32_OP 0xe9
#define JMP_IND_OP 0xff
#define LEA_OP 0x8d
#define REPNE_PREFIX 0xf2
#define REP_PREFIX 0xf3
#define POP_OP 0x58
#define RET_OP 0xc3
#define RET_OP_2 0xc2
#define INT3_OP 0xcc

#define IS_REX_PREFIX(x) (((x) & 0xf0) == 0x40)

    //
    // Everything below is inspired by the code in minkernel\ntos\rtl\amd64\exdsptch.c file from Windows
    // For details see similar code in OOPStackUnwinderAMD64::UnwindEpilogue
    //
    //
    //
    // A canonical epilogue sequence consists of the following operations:
    //
    // 1. Optional cleanup of fixed and dynamic stack allocations, which is
    //    considered to be outside of the epilogue region.
    //
    //    add rsp, imm
    //        or
    //    lea rsp, disp[fp]
    //
    // 2. Zero or more pop nonvolatile-integer-register[0..15] instructions.
    //
    //    pop r64
    //        or
    //    REX.R pop r64
    //
    // 3. An optional one-byte pop r64 to a volatile register to clean up an
    //    RFLAGS register pushed with pushfq.
    //
    //    pop rcx
    //
    // 4. A control transfer instruction (ret or jump, in a case of a tailcall)
    //    For the purpose of inferring the state of the stack, ret and jump can be
    //    considered the same.
    //
    //    ret 0
    //        or
    //    jmp imm
    //        or
    //    jmp [target]
    //
    // 5. Occasionally we may see a breakpoint, possibly placed by the debugger.
    //    In such case we do not know what instruction it was and return -1 (unknown)
    //
    //    int 3
    //

    // if we are in an epilogue, there will be at least one instruction left.
    int trailingEpilogueInstructions = 1;
    uint8_t* pNextByte = (uint8_t*)pvAddress;

    //
    // Check for any number of:
    //
    //   pop nonvolatile-integer-register[0..15].
    //

    while (true)
    {
        if ((pNextByte[0] & 0xf8) == POP_OP)
        {
            pNextByte += 1;
            trailingEpilogueInstructions++;
        }
        else if (IS_REX_PREFIX(pNextByte[0]) && ((pNextByte[1] & 0xf8) == POP_OP))
        {
            pNextByte += 2;
            trailingEpilogueInstructions++;
        }
        else
        {
            break;
        }
    }

    //
    // A REPNE prefix may optionally precede a control transfer
    // instruction with no effect on unwinding.
    //

    if (pNextByte[0] == REPNE_PREFIX)
    {
        pNextByte += 1;
    }

    if (((pNextByte[0] == RET_OP) ||
        (pNextByte[0] == RET_OP_2)) ||
        (((pNextByte[0] == REP_PREFIX) && (pNextByte[1] == RET_OP))))
    {
        //
        // A return is an unambiguous indication of an epilogue.
        //
        return trailingEpilogueInstructions;
    }

    if ((pNextByte[0] == JMP_IMM8_OP) ||
        (pNextByte[0] == JMP_IMM32_OP))
    {
        //
        // An unconditional branch to a target that is equal to the start of
        // or outside of this routine is logically a call to another function.
        //

        size_t branchTarget = (size_t)pNextByte;
        if (pNextByte[0] == JMP_IMM8_OP)
        {
            branchTarget += 2 + (int8_t)pNextByte[1];
        }
        else
        {
            uint32_t delta =
                (uint32_t)pNextByte[1] |
                ((uint32_t)pNextByte[2] << 8) |
                ((uint32_t)pNextByte[3] << 16) |
                ((uint32_t)pNextByte[4] << 24);

            branchTarget += 5 + (int32_t)delta;
        }

        //
        // Determine whether the branch target refers to code within this
        // function. If not, then it is an epilogue indicator.
        //
        // A branch to the start of self implies a recursive call, so
        // is treated as an epilogue.
        //

        if ((uintptr_t)pvAddress >= (uintptr_t)m_pvManagedCodeStartRange &&
            (uintptr_t)pvAddress < (uintptr_t)m_pvManagedCodeStartRange + m_cbManagedCodeRange)
        {
            TADDR startIp, endIp;

            bool result = GetFunctionBounds((TADDR)pvAddress, &startIp, &endIp);
            ASSERT(result);

            if (branchTarget <= startIp || branchTarget >= endIp)
            {
                return trailingEpilogueInstructions;
            }
        }
    }
    else if ((pNextByte[0] == JMP_IND_OP) && (pNextByte[1] == 0x25))
    {
        //
        // An unconditional jump indirect.
        //
        // This is a jmp outside of the function, probably a tail call
        // to an import function.
        //

        return trailingEpilogueInstructions;
    }
    else if (((pNextByte[0] & 0xf8) == SIZE64_PREFIX) &&
        (pNextByte[1] == 0xff) &&
        (pNextByte[2] & 0x38) == 0x20)
    {
        //
        // This is an indirect jump opcode: 0x48 0xff /4.  The 64-bit
        // flag (REX.W) is always redundant here, so its presence is
        // overloaded to indicate a branch out of the function - a tail
        // call.
        //
        // Such an opcode is an unambiguous epilogue indication.
        //

        return trailingEpilogueInstructions;
    }
    else if (pNextByte[0] == INT3_OP)
    {
        //
        // A breakpoint, possibly placed by the debugger - we do not know what was here.
        //
        return -1;
    }

#elif defined(TARGET_ARM64)

#define AUTIASP_INSTR 0xD50323BF
#define AUTIBSP_INSTR 0xD50323FF

// ldr with unsigned immediate
// 1x11 1001 x1xx xxxx xxxx xxxx xxxx xxxx
#define LDR_BITS1 0xB9400000
#define LDR_MASK1 0xBF400000

// ldr with pre/post/no offset
// 1x11 1000 010x xxxx xxxx xxxx xxxx xxxx
#define LDR_BITS2 0xB8400000
#define LDR_MASK2 0xBFE00000

// ldr with register offset
// 1x11 1000 011x xxxx xxxx 10xx xxxx xxxx
#define LDR_BITS3 0xB8600800
#define LDR_MASK3 0xBFE00C00

// ldp with signed offset
// x010 1001 01xx xxxx xxxx xxxx xxxx xxxx
#define LDP_BITS1 0x29400000
#define LDP_MASK1 0x7FC00000

// ldp with pre/post/no offset
// x010 100x x1xx xxxx xxxx xxxx xxxx xxxx
#define LDP_BITS2 0x28400000
#define LDP_MASK2 0x7E400000

// Branches, Exception Generating and System instruction group
// xxx1 01xx xxxx xxxx xxxx xxxx xxxx xxxx
#define BEGS_BITS 0x14000000
#define BEGS_MASK 0x1C000000

    UnixNativeMethodInfo * pNativeMethodInfo = (UnixNativeMethodInfo *)pMethodInfo;
    ASSERT(pNativeMethodInfo != NULL);

    uint32_t* start  = (uint32_t*)pNativeMethodInfo->pMethodStartAddress;

    // Since we stop on branches, the search is roughly limited by the containing basic block.
    // We typically examine just 1-5 instructions and in rare cases up to 30.
    //
    // TODO: we can also limit the search by the longest possible epilogue length, but
    // we must be sure the longest length considers all possibilities,
    // which is somewhat nontrivial to derive/prove.
    // It does not seem urgent, but it could be nice to have a constant upper bound.
    for (uint32_t* pInstr = (uint32_t*)pvAddress - 1; pInstr > start; pInstr--)
    {
        uint32_t instr = *pInstr;

        // check for Branches, Exception Generating and System instruction group.
        // If we see such instruction before seeing FP or LR restored, we are not in an epilog.
        // Note: this includes RET, BRK, branches, calls, tailcalls, fences, etc...
        if ((instr & BEGS_MASK) == BEGS_BITS)
        {
            // Scan past authentication instructions to find the FP/LR restore.
            if (instr == AUTIASP_INSTR || instr == AUTIBSP_INSTR)
            {
                continue;
            }
            // not in an epilogue
            break;
        }

        // check for restoring FP or LR with ldr or ldp
        int operand = instr & 0x1f;
        if (operand == 30 || operand == 29)
        {
            if ((instr & LDP_MASK1) == LDP_BITS1 ||
                (instr & LDP_MASK2) == LDP_BITS2 ||
                (instr & LDR_MASK1) == LDR_BITS1 ||
                (instr & LDR_MASK2) == LDR_BITS2 ||
                (instr & LDR_MASK3) == LDR_BITS3)
            {
                return -1;
            }
        }

        // check for restoring FP or LR with ldp (as Rt2)
        operand = (instr >> 10) & 0x1f;
        if (operand == 30 || operand == 29)
        {
            if ((instr & LDP_MASK1) == LDP_BITS1 ||
                (instr & LDP_MASK2) == LDP_BITS2)
            {
                return -1;
            }
        }
    }

#elif defined(TARGET_ARM)

// ADD<c> SP, SP, #<imm>
// 1011 0000 0xxx xxxx
#define ADD_SP_IMM_BITS 0xB000
#define ADD_SP_IMM_MASK 0xFF80

// ADD{S}<c>.W SP, SP, #<const>
// 1111 0x01 000x 1101 0xxx 1101 xxxx xxxx
#define ADD_W_SP_IMM_BITS 0xF10D0D00
#define ADD_W_SP_IMM_MASK 0xFBEF8F00

// ADDW<c> SP, SP, #<imm12>
// 1111 0x10 0000 1101 0xxx 1101 xxxx xxxx
#define ADDW_SP_IMM_BITS 0xF20D0D00
#define ADDW_SP_IMM_MASK 0xFBFF8F00

// ADD<c> SP, <Rm>
// 0100 0100 1xxx x101
#define ADD_SP_REG_BITS 0x4485
#define ADD_SP_REG_MASK 0xFF87

// ADD{S}<c>.W SP, SP, <Rm>{, <shift>}
// 1110 1011 000x 1101 0xxx 1101 xxxx xxxx
#define ADD_W_SP_REG_BITS 0xEB0D0D00
#define ADD_W_SP_REG_MASK 0xFFEF8F00

// POP<c> <registers>
// 1011 110x xxxx xxxx
#define POP_BITS 0xBC00
#define POP_MASK 0xFE00

// BX LR
#define BX_LR_BITS 0x4770
#define BX_LR_MASK 0xFFFF

// POP<c>.W <registers>
// 1110 1000 1011 1101 xx0x xxxx xxxx xxxx
#define POP_W_BITS_T2 0xE8BD0000
#define POP_W_MASK_T2 0xFFFF2000

// POP<c>.W <registers>
// 1111 1000 0101 1101 xxxx 1011 0000 0100
#define POP_W_BITS_T3 0xF85D0B04
#define POP_W_MASK_T3 0xFFFF0FFF

// VPOP <list>
// 1110 1100 1x11 1101 xxxx 1011 xxxx xxxx
#define VPOP_BITS_T1 0xECBD0B00
#define VPOP_MASK_T1 0xFFBF0F00

// VPOP <list>
// 1110 1100 1x11 1101 xxxx 1010 xxxx xxxx
#define VPOP_BITS_T2 0xECBD0A00
#define VPOP_MASK_T2 0xFFBF0F00

    uint16_t *pInstr = (uint16_t *)pvAddress;
    uint32_t instr;

    while (1)
    {
        instr = *pInstr;

        if ((instr & 0xE000) == 0xE000 && (instr & 0xF800) != 0xE000)
        {
            // 32-bit instruction
            instr <<= 16;
            instr |= *(pInstr + 1);
            pInstr += 2;
        }
        else
        {
            pInstr++;
        }

        // POP, VPOP and BX LR are definitely epilog
        if ((instr & POP_MASK) == POP_BITS ||
            (instr & BX_LR_MASK) == BX_LR_BITS ||
            (instr & POP_W_MASK_T2) == POP_W_BITS_T2 ||
            (instr & POP_W_MASK_T3) == POP_W_BITS_T3 ||
            (instr & VPOP_MASK_T1) == VPOP_BITS_T1 ||
            (instr & VPOP_MASK_T2) == VPOP_BITS_T2)
        {
            return -1;
        }

        // ADD SP, xxx may be part of epilog but in methods with frame
        // pointer it can also appear in the body. Skip to the next
        // instruction and check if it's still epilog.
        if ((instr & ADD_SP_IMM_MASK) == ADD_SP_IMM_BITS ||
            (instr & ADD_SP_REG_MASK) == ADD_SP_REG_BITS ||
            (instr & ADD_W_SP_IMM_MASK) == ADD_W_SP_IMM_BITS ||
            (instr & ADDW_SP_IMM_MASK) == ADDW_SP_IMM_BITS ||
            (instr & ADD_W_SP_REG_MASK) == ADD_W_SP_REG_BITS)
        {
            continue;
        }

        return 0;
    }

#elif defined(TARGET_LOONGARCH64)

// addi.d $sp, $sp, x
// 0000 0010 11xx xxxx xxxx xx00 0110 0011
#define ADDI_D_SP_SP_BITS 0x02C00063
#define ADDI_D_SP_SP_MASK 0xFFC003FF

// ld.d
// 0010 1000 11xx xxxx xxxx xxxx xxxx xxxx
#define LD_BITS 0x28C00000
#define LD_MASK 0xFFC00000

// ldx.d with register offset
// 0011 1000 0000 1100 0xxx xxxx xxxx xxxx
#define LDX_BITS 0x380C0000
#define LDX_MASK 0xFFFF7000

// Branches, Exception Generating and System instruction group
// 01xx xxxx xxxx xxxx xxxx xxxx xxxx xxxx
#define BEGS_BITS 0x40000000
#define BEGS_MASK 0xC0000000

    UnixNativeMethodInfo * pNativeMethodInfo = (UnixNativeMethodInfo *)pMethodInfo;
    ASSERT(pNativeMethodInfo != NULL);

    uint32_t* start  = (uint32_t*)pNativeMethodInfo->pMethodStartAddress;

    // Since we stop on branches, the search is roughly limited by the containing basic block.
    // We typically examine just 1-5 instructions and in rare cases up to 30.
    //
    // TODO: we can also limit the search by the longest possible epilogue length, but
    // we must be sure the longest length considers all possibilities,
    // which is somewhat nontrivial to derive/prove.
    // It does not seem urgent, but it could be nice to have a constant upper bound.
    for (uint32_t* pInstr = (uint32_t*)pvAddress - 1; pInstr > start; pInstr--)
    {
        uint32_t instr = *pInstr;

        // check for Branches, Exception Generating and System instruction group.
        // If we see such instruction before seeing FP or RA restored, we are not in an epilog.
        // Note: this includes RET, BRK, branches, calls, tailcalls, fences, etc...
        if ((instr & BEGS_MASK) == BEGS_BITS)
        {
            // not in an epilogue
            break;
        }

        // check for restoring FP or RA with ld.d
        if ((instr & LD_MASK) == LD_BITS)
        {
            int rd = (instr & 0x1f);
            if (rd == 22 || rd == 1)
            {
                return -1;
            }
        }

        // Check for adjusting stack pointer
        if ((instr & ADDI_D_SP_SP_MASK) == ADDI_D_SP_SP_BITS)
        {
            return -1;
        }
    }

#elif defined(TARGET_RISCV64)

// addi sp, sp, x
#define ADD_SP_SP_BITS 0x00010113
#define ADD_SP_SP_MASK 0x000FFFFF

// Load with register offset
// LD with register offset
#define LD_BITS 0x00000003
#define LD_MASK 0x0000007F

// Branches
// BEQ, BNE, etc.
#define BEGS_BITS 0x00000063
#define BEGS_MASK 0x0000007F

    UnixNativeMethodInfo * pNativeMethodInfo = (UnixNativeMethodInfo *)pMethodInfo;
    ASSERT(pNativeMethodInfo != NULL);

    uint32_t* start  = (uint32_t*)pNativeMethodInfo->pMethodStartAddress;

    // Since we stop on branches, the search is roughly limited by the containing basic block.
    // We typically examine just 1-5 instructions and in rare cases up to 30.
    //
    // TODO: we can also limit the search by the longest possible epilogue length, but
    // we must be sure the longest length considers all possibilities,
    // which is somewhat nontrivial to derive/prove.
    // It does not seem urgent, but it could be nice to have a constant upper bound.
    for (uint32_t* pInstr = (uint32_t*)pvAddress - 1; pInstr > start; pInstr--)
    {
        uint32_t instr = *pInstr;

        // Check for branches, jumps, or system calls.
        // If we see such instructions before registers are restored, we are not in an epilogue.
        // Note: this includes RET, branches, jumps, and system calls.
        if ((instr & BEGS_MASK) == BEGS_BITS)
        {
            // not in an epilogue
            break;
        }

        // Check for restoring registers (FP or RA) with `ld`
        if ((instr & LD_MASK) == LD_BITS)  // Match `ld` instruction
        {
            int rd = (instr >> 7) & 0x1F;  // Extract the destination register
            if (rd == 8 || rd == 1)  // Check for FP (x8) or RA (x1)
            {
                return -1;
            }
        }

        // Check for adjusting stack pointer
        if ((instr & ADD_SP_SP_MASK) == ADD_SP_SP_BITS)
        {
            return -1;
        }
    }

#endif

    return 0;
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

#if defined(TARGET_ARM)
    // Ensure that PC doesn't have the Thumb bit set. Prolog and epilog
    // checks depend on it.
    ASSERT(((uintptr_t)pRegisterSet->IP & 1) == 0);
#endif

    int epilogueInstructions = TrailingEpilogueInstructionsCount(pMethodInfo, (PTR_VOID)pRegisterSet->IP);
    if (epilogueInstructions < 0)
    {
        // can't figure, possibly a breakpoint instruction
        return false;
    }

    if (epilogueInstructions > 0)
    {
        *ppvRetAddrLocation = (PTR_PTR_VOID)(pRegisterSet->GetSP() + (sizeof(TADDR) * (epilogueInstructions - 1)));
        return true;
    }

    // Compact unwind descriptors only describe the frame after the prolog.
    if (IsInCompactUnwindProlog((PTR_VOID)pRegisterSet->IP))
        return false;

    ASSERT(IsUnwindable((PTR_VOID)pRegisterSet->IP));

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

#if defined(TARGET_ARM64)
    bool hasPac = (pNativeMethodInfo->pDescriptor->Flags & CompactUnwindDescriptor::FlagReturnAddressSigned) != 0;
#endif

    PTR_uintptr_t oldLocation = pRegisterSet->GetReturnAddressRegisterLocation();
    if (!VirtualUnwind(pMethodInfo, pRegisterSet))
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

#if defined(TARGET_ARM64)
    if (hasPac)
    {
        // Unwinding recovers the entry SP, which NativeAOT used to sign LR.
        *pSpForArm64PacSign = pRegisterSet->GetSP();
    }
#endif

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
