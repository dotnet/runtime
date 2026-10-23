// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "gcenv.h"
#if !defined(BUILD_AS_STANDALONE) || defined(FEATURE_STANDALONE_GC)
#include "gcenv.inl"
#endif
#include "side_metadata.h"

#include <string.h>

namespace
{
bool TryAddSize(uintptr_t start, size_t size, uintptr_t* end)
{
    if (size > (UINTPTR_MAX - start))
    {
        return false;
    }

    *end = start + size;
    return true;
}

bool TryAlignUp(uintptr_t value, size_t alignment, uintptr_t* result)
{
    uintptr_t mask = alignment - 1;
    if ((alignment == 0) || ((alignment & mask) != 0) || (value > (UINTPTR_MAX - mask)))
    {
        return false;
    }

    *result = (value + mask) & ~mask;
    return true;
}

SideMetadataResult AddSpec(
    SideMetadataSpec* spec,
    LxrSideMetadataKind kind,
    SideMetadataScope scope,
    uint8_t logBitsPerValue,
    uint8_t reservedLogBitsPerValue,
    uint8_t logBytesPerValue,
    uintptr_t* nextAddress,
    uintptr_t limit)
{
    constexpr uint8_t LogBitsPerByte = 3;
    constexpr uint8_t LogAddressSpaceBytes = LxrSideMetadataLayout::AddressBits;

    if ((logBitsPerValue > reservedLogBitsPerValue) ||
        (reservedLogBitsPerValue > 6) ||
        (logBytesPerValue >= LogAddressSpaceBytes))
    {
        return SideMetadataResult::InvalidLayout;
    }

    int32_t logicalExponent =
        LogAddressSpaceBytes - logBytesPerValue + logBitsPerValue - LogBitsPerByte;
    int32_t reservedExponent =
        LogAddressSpaceBytes - logBytesPerValue + reservedLogBitsPerValue - LogBitsPerByte;
    if ((logicalExponent < 0) ||
        (reservedExponent < 0) ||
        (reservedExponent >= static_cast<int32_t>(sizeof(size_t) * 8)))
    {
        return SideMetadataResult::InvalidLayout;
    }

    uintptr_t alignedAddress;
    if (!TryAlignUp(*nextAddress, sizeof(uintptr_t), &alignedAddress))
    {
        return SideMetadataResult::AddressOverflow;
    }

    size_t logicalSize = static_cast<size_t>(1) << logicalExponent;
    size_t reservedSize = static_cast<size_t>(1) << reservedExponent;
    uintptr_t end;
    if (!TryAddSize(alignedAddress, reservedSize, &end) || (end > limit))
    {
        return SideMetadataResult::InvalidLayout;
    }

    spec->kind = kind;
    spec->scope = scope;
    spec->base_address = alignedAddress;
    spec->logical_size = logicalSize;
    spec->reserved_size = reservedSize;
    spec->log_bits_per_value = logBitsPerValue;
    spec->reserved_log_bits_per_value = reservedLogBitsPerValue;
    spec->log_bytes_per_value = logBytesPerValue;
    *nextAddress = end;
    return SideMetadataResult::Success;
}

bool IsValueValid(uintptr_t value, uint32_t bitCount)
{
    return (bitCount == (sizeof(uintptr_t) * 8)) || ((value >> bitCount) == 0);
}
}

SideMetadataResult LxrSideMetadataLayout::Create(
    uint8_t logReferenceCountBits,
    LxrSideMetadataLayout* layout)
{
    if (layout == nullptr)
    {
        return SideMetadataResult::InvalidArgument;
    }

    if (sizeof(uintptr_t) != sizeof(uint64_t))
    {
        return SideMetadataResult::UnsupportedAddressSpace;
    }

    if ((logReferenceCountBits < 1) || (logReferenceCountBits > 3))
    {
        return SideMetadataResult::InvalidArgument;
    }

    memset(layout, 0, sizeof(*layout));
    uintptr_t global = static_cast<uintptr_t>(GlobalBaseAddress);
    uintptr_t local = static_cast<uintptr_t>(LocalBaseAddress);
    SideMetadataResult result;

#define ADD_SPEC(kind, scope, bits, reservedBits, bytes, next, limit) \
    result = AddSpec( \
        &layout->m_specs[static_cast<size_t>(LxrSideMetadataKind::kind)], \
        LxrSideMetadataKind::kind, \
        SideMetadataScope::scope, \
        bits, \
        reservedBits, \
        bytes, \
        &next, \
        limit); \
    if (result != SideMetadataResult::Success) \
    { \
        return result; \
    }

    ADD_SPEC(ValidObject, Global, 0, 0, 3, global, static_cast<uintptr_t>(LocalBaseAddress));
    ADD_SPEC(ReferenceCount, Global, logReferenceCountBits, 3, 3, global, static_cast<uintptr_t>(LocalBaseAddress));
    ADD_SPEC(FieldUnlogged, Global, 0, 0, 3, global, static_cast<uintptr_t>(LocalBaseAddress));
    ADD_SPEC(BlockDefrag, Global, 3, 3, 15, global, static_cast<uintptr_t>(LocalBaseAddress));

    ADD_SPEC(ObjectMark, Local, 0, 0, 3, local, static_cast<uintptr_t>(AddressLimit));
    ADD_SPEC(ReferenceCountStraddle, Local, 3, 3, 8, local, static_cast<uintptr_t>(AddressLimit));
    ADD_SPEC(BlockMark, Local, 3, 3, 15, local, static_cast<uintptr_t>(AddressLimit));
    ADD_SPEC(BlockLog, Local, 0, 0, 15, local, static_cast<uintptr_t>(AddressLimit));
    ADD_SPEC(NurseryPromotion, Local, 3, 3, 15, local, static_cast<uintptr_t>(AddressLimit));
    ADD_SPEC(PhaseEpoch, Local, 3, 3, 15, local, static_cast<uintptr_t>(AddressLimit));
    ADD_SPEC(LineReuse, Local, 3, 3, 8, local, static_cast<uintptr_t>(AddressLimit));
    ADD_SPEC(LargeObjectMarkNursery, Local, 1, 1, 3, local, static_cast<uintptr_t>(AddressLimit));
    ADD_SPEC(LargeObjectPageReuse, Local, 3, 3, 12, local, static_cast<uintptr_t>(AddressLimit));
    ADD_SPEC(BlockOwner, Local, 6, 6, 15, local, static_cast<uintptr_t>(AddressLimit));
    ADD_SPEC(BlockInUse, Local, 3, 3, 15, local, static_cast<uintptr_t>(AddressLimit));

#undef ADD_SPEC

    layout->m_global_size = global - static_cast<uintptr_t>(GlobalBaseAddress);
    layout->m_local_size = local - static_cast<uintptr_t>(LocalBaseAddress);
    return SideMetadataResult::Success;
}

const SideMetadataSpec* LxrSideMetadataLayout::GetSpec(LxrSideMetadataKind kind) const
{
    size_t index = static_cast<size_t>(kind);
    return index < SpecCount ? &m_specs[index] : nullptr;
}

SideMetadataManager::SideMetadataManager()
    : m_layout(nullptr)
    , m_global_reservation(nullptr)
    , m_local_reservation(nullptr)
    , m_data_ranges(nullptr)
    , m_enabled_spec_mask(0)
    , m_commit_failed(false)
{
}

SideMetadataManager::~SideMetadataManager()
{
    Shutdown();
}

SideMetadataResult SideMetadataManager::Initialize(
    const LxrSideMetadataLayout* layout,
    uint64_t enabledSpecMask)
{
    if ((layout == nullptr) || (m_layout != nullptr))
    {
        return SideMetadataResult::InvalidArgument;
    }

    if ((sizeof(uintptr_t) != sizeof(uint64_t)) ||
        (layout->GetGlobalSize() == 0) ||
        (layout->GetLocalSize() == 0))
    {
        return SideMetadataResult::UnsupportedAddressSpace;
    }

    uintptr_t globalEnd;
    uintptr_t localEnd;
    if (!TryAddSize(layout->GetGlobalBase(), layout->GetGlobalSize(), &globalEnd) ||
        !TryAddSize(layout->GetLocalBase(), layout->GetLocalSize(), &localEnd) ||
        (globalEnd > layout->GetLocalBase()) ||
        (localEnd > layout->GetAddressLimit()))
    {
        return SideMetadataResult::InvalidLayout;
    }

    uint64_t allSpecsMask = (UINT64_C(1) << layout->GetSpecCount()) - 1;
    if (enabledSpecMask == UINT64_MAX)
    {
        enabledSpecMask = allSpecsMask;
    }
    if ((enabledSpecMask == 0) || ((enabledSpecMask & ~allSpecsMask) != 0))
    {
        return SideMetadataResult::InvalidArgument;
    }

    m_global_reservation = GCToOSInterface::VirtualReserveAt(
        reinterpret_cast<void*>(layout->GetGlobalBase()),
        layout->GetGlobalSize(),
        VirtualReserveFlags::NoReserve);
    if (m_global_reservation == nullptr)
    {
        return SideMetadataResult::ReservationFailed;
    }

    m_local_reservation = GCToOSInterface::VirtualReserveAt(
        reinterpret_cast<void*>(layout->GetLocalBase()),
        layout->GetLocalSize(),
        VirtualReserveFlags::NoReserve);
    if (m_local_reservation == nullptr)
    {
        GCToOSInterface::VirtualRelease(m_global_reservation, layout->GetGlobalSize());
        m_global_reservation = nullptr;
        return SideMetadataResult::ReservationFailed;
    }

    m_layout = layout;
    m_enabled_spec_mask = enabledSpecMask;
    m_commit_failed = false;
    return SideMetadataResult::Success;
}

void SideMetadataManager::Shutdown()
{
    ClearDataRanges();

    if (m_layout != nullptr)
    {
        if (m_local_reservation != nullptr)
        {
            GCToOSInterface::VirtualRelease(m_local_reservation, m_layout->GetLocalSize());
        }
        if (m_global_reservation != nullptr)
        {
            GCToOSInterface::VirtualRelease(m_global_reservation, m_layout->GetGlobalSize());
        }
    }

    m_layout = nullptr;
    m_global_reservation = nullptr;
    m_local_reservation = nullptr;
    m_enabled_spec_mask = 0;
    m_commit_failed = false;
}

SideMetadataResult SideMetadataManager::CommitDataRange(uintptr_t start, size_t size)
{
    if ((m_layout == nullptr) || m_commit_failed || (size == 0))
    {
        return SideMetadataResult::InvalidArgument;
    }

    uintptr_t end;
    if (!TryAddSize(start, size, &end) || (end > m_layout->GetAddressLimit()))
    {
        return SideMetadataResult::AddressOverflow;
    }

    size_t pageSize = GCToOSInterface::GetPageSize();
    for (size_t index = 0; index < m_layout->GetSpecCount(); index++)
    {
        const SideMetadataSpec& spec = m_layout->GetSpecs()[index];
        if (!IsSpecEnabled(spec.kind))
        {
            continue;
        }
        uint8_t logBits = spec.log_bits_per_value;
        uintptr_t firstEntry = start >> spec.log_bytes_per_value;
        uintptr_t lastEntry = (end - 1) >> spec.log_bytes_per_value;
        uintptr_t firstOffset;
        uintptr_t lastOffset;
        size_t valueBytes;

        if (logBits <= 3)
        {
            firstOffset = firstEntry >> (3 - logBits);
            lastOffset = lastEntry >> (3 - logBits);
            valueBytes = 1;
        }
        else
        {
            uint8_t byteShift = logBits - 3;
            if ((firstEntry > (UINTPTR_MAX >> byteShift)) ||
                (lastEntry > (UINTPTR_MAX >> byteShift)))
            {
                return SideMetadataResult::AddressOverflow;
            }
            firstOffset = firstEntry << byteShift;
            lastOffset = lastEntry << byteShift;
            valueBytes = static_cast<size_t>(1) << byteShift;
        }

        uintptr_t metadataStart;
        uintptr_t metadataEnd;
        if (!TryAddSize(spec.base_address, firstOffset, &metadataStart) ||
            !TryAddSize(spec.base_address, lastOffset, &metadataEnd) ||
            !TryAddSize(metadataEnd, valueBytes, &metadataEnd))
        {
            return SideMetadataResult::AddressOverflow;
        }

        uintptr_t specEnd;
        if (!TryAddSize(spec.base_address, spec.logical_size, &specEnd) ||
            (metadataStart < spec.base_address) ||
            (metadataEnd > specEnd))
        {
            return SideMetadataResult::InvalidLayout;
        }

        uintptr_t commitStart = metadataStart & ~(static_cast<uintptr_t>(pageSize) - 1);
        uintptr_t commitEnd;
        if (!TryAlignUp(metadataEnd, pageSize, &commitEnd) ||
            !GCToOSInterface::VirtualCommit(
                reinterpret_cast<void*>(commitStart),
                commitEnd - commitStart))
        {
            return SideMetadataResult::CommitFailed;
        }
    }

    SideMetadataResult result = AddDataRange(start, end);
    return result;
}

SideMetadataResult SideMetadataManager::DecommitDataRange(uintptr_t start, size_t size)
{
    if ((m_layout == nullptr) || m_commit_failed || (size == 0))
    {
        return SideMetadataResult::InvalidArgument;
    }

    uintptr_t end;
    if (!TryAddSize(start, size, &end) || (end > m_layout->GetAddressLimit()))
    {
        return SideMetadataResult::AddressOverflow;
    }
    if (!IsDataRangeOwned(start, size))
    {
        return SideMetadataResult::AddressNotOwned;
    }

    uintptr_t pageSize = static_cast<uintptr_t>(GCToOSInterface::GetPageSize());
    for (size_t index = 0; index < m_layout->GetSpecCount(); index++)
    {
        const SideMetadataSpec& spec = m_layout->GetSpecs()[index];
        if (!IsSpecEnabled(spec.kind))
        {
            continue;
        }
        uint8_t logBits = spec.log_bits_per_value;
        uintptr_t granule = (static_cast<uintptr_t>(1) << spec.log_bytes_per_value) - 1;
        if (((start | end) & granule) != 0)
        {
            return SideMetadataResult::InvalidArgument;
        }
        uintptr_t firstEntry = start >> spec.log_bytes_per_value;
        uintptr_t endEntry = end >> spec.log_bytes_per_value;
        // Several entries can share a byte (a block-log bit covers one block). The entries in a byte that the range
        // fills only partly are cleared one by one; the whole bytes are handled by pages below.
        uintptr_t perByte = logBits < 3 ? static_cast<uintptr_t>(1) << (3 - logBits) : 1;
        uintptr_t wholeFirst = (firstEntry + perByte - 1) & ~(perByte - 1);
        uintptr_t wholeEnd = endEntry & ~(perByte - 1);
        if (wholeEnd < wholeFirst)
        {
            wholeFirst = wholeEnd = endEntry;
            SideMetadataResult reset = ResetRangeQuiescentCore(spec, start, size, 0);
            if (reset != SideMetadataResult::Success)
            {
                return reset;
            }
        }
        else
        {
            SideMetadataResult reset = SideMetadataResult::Success;
            if (wholeFirst > firstEntry)
            {
                reset = ResetRangeQuiescentCore(
                    spec, start, static_cast<size_t>(wholeFirst - firstEntry) << spec.log_bytes_per_value, 0);
            }
            if (reset == SideMetadataResult::Success && endEntry > wholeEnd)
            {
                reset = ResetRangeQuiescentCore(
                    spec, static_cast<uintptr_t>(wholeEnd) << spec.log_bytes_per_value,
                    static_cast<size_t>(endEntry - wholeEnd) << spec.log_bytes_per_value, 0);
            }
            if (reset != SideMetadataResult::Success)
            {
                return reset;
            }
        }
        uintptr_t firstOffset;
        uintptr_t endOffset;
        if (logBits <= 3)
        {
            firstOffset = wholeFirst >> (3 - logBits);
            endOffset = wholeEnd >> (3 - logBits);
        }
        else
        {
            uint8_t byteShift = logBits - 3;
            if (endEntry > (UINTPTR_MAX >> byteShift))
            {
                return SideMetadataResult::AddressOverflow;
            }
            firstOffset = wholeFirst << byteShift;
            endOffset = wholeEnd << byteShift;
        }
        uintptr_t metadataStart;
        uintptr_t metadataEnd;
        uintptr_t specEnd;
        if (!TryAddSize(spec.base_address, firstOffset, &metadataStart) ||
            !TryAddSize(spec.base_address, endOffset, &metadataEnd) ||
            !TryAddSize(spec.base_address, spec.logical_size, &specEnd) ||
            (metadataEnd > specEnd))
        {
            return SideMetadataResult::InvalidLayout;
        }
        if (metadataEnd == metadataStart)
        {
            continue;
        }
        uintptr_t pageLow = (metadataStart + pageSize - 1) & ~(pageSize - 1);
        uintptr_t pageHigh = metadataEnd & ~(pageSize - 1);
        bool topShared = false;
        if (pageHigh != metadataEnd)
        {
            // The last page also holds the entries of the data after the range. It can go too when no admitted data
            // lies there (the Immix space ends, and the large-object range is far away).
            uintptr_t slack = pageSize - (metadataEnd - pageHigh);
            uintptr_t dataSpan = logBits < 3 ? (slack * perByte) << spec.log_bytes_per_value
                : (slack >> (logBits - 3)) << spec.log_bytes_per_value;
            topShared = false;
            for (DataRange* range = m_data_ranges; range != nullptr; range = range->next)
            {
                topShared = topShared || ((range->end > end) && (range->start < end + dataSpan));
            }
            if (!topShared)
            {
                pageHigh = metadataEnd + slack;
            }
        }
        if (pageHigh <= pageLow)
        {
            memset(reinterpret_cast<void*>(metadataStart), 0, metadataEnd - metadataStart);
            continue;
        }
        memset(reinterpret_cast<void*>(metadataStart), 0, pageLow - metadataStart);
        if (topShared)
        {
            memset(reinterpret_cast<void*>(pageHigh), 0, metadataEnd - pageHigh);
        }
        if (!GCToOSInterface::VirtualDecommit(reinterpret_cast<void*>(pageLow), pageHigh - pageLow))
        {
            return SideMetadataResult::CommitFailed;
        }
    }

#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_S5B_KEEP_RANGE_OWNED_CONTROL)
    return SideMetadataResult::Success;
#else
    return RemoveDataRange(start, end);
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_S5B_KEEP_RANGE_OWNED_CONTROL
}

bool SideMetadataManager::IsDataAddressOwned(uintptr_t address) const
{
    for (DataRange* range = m_data_ranges; range != nullptr; range = range->next)
    {
        if ((address >= range->start) && (address < range->end))
        {
            return true;
        }
    }

    return false;
}

bool SideMetadataManager::IsDataRangeOwned(uintptr_t start, size_t size) const
{
    if (size == 0)
    {
        return true;
    }

    uintptr_t end;
    if (!TryAddSize(start, size, &end))
    {
        return false;
    }

    for (DataRange* range = m_data_ranges; range != nullptr; range = range->next)
    {
        if ((start >= range->start) && (end <= range->end))
        {
            return true;
        }
    }

    return false;
}

SideMetadataResult SideMetadataManager::GetLocation(
    LxrSideMetadataKind kind,
    uintptr_t dataAddress,
    SideMetadataLocation* location) const
{
    if ((m_layout == nullptr) || m_commit_failed || (location == nullptr))
    {
        return SideMetadataResult::InvalidArgument;
    }

    if (!IsDataAddressOwned(dataAddress))
    {
        return SideMetadataResult::AddressNotOwned;
    }

    if (!IsSpecEnabled(kind))
    {
        return SideMetadataResult::AddressNotOwned;
    }

    const SideMetadataSpec* spec = m_layout->GetSpec(kind);
    return spec == nullptr
        ? SideMetadataResult::InvalidArgument
        : GetLocationUnchecked(*spec, dataAddress, location);
}

SideMetadataResult SideMetadataManager::GetLocationUnchecked(
    const SideMetadataSpec& spec,
    uintptr_t dataAddress,
    SideMetadataLocation* location) const
{
    uintptr_t entry = dataAddress >> spec.log_bytes_per_value;
    uintptr_t metadataOffset;
    uint32_t bitShift = 0;
    uint32_t bitCount = static_cast<uint32_t>(1) << spec.log_bits_per_value;
    size_t valueBytes = (bitCount + 7) / 8;

    if (spec.log_bits_per_value <= 3)
    {
        uint8_t fieldsPerByteShift = 3 - spec.log_bits_per_value;
        metadataOffset = entry >> fieldsPerByteShift;
        bitShift = static_cast<uint32_t>(entry & ((static_cast<uintptr_t>(1) << fieldsPerByteShift) - 1))
            << spec.log_bits_per_value;
    }
    else
    {
        uint8_t byteShift = spec.log_bits_per_value - 3;
        if (entry > (UINTPTR_MAX >> byteShift))
        {
            return SideMetadataResult::AddressOverflow;
        }
        metadataOffset = entry << byteShift;
    }

    uintptr_t metadataAddress;
    uintptr_t specEnd;
    uintptr_t metadataValueEnd;
    if (!TryAddSize(spec.base_address, metadataOffset, &metadataAddress) ||
        !TryAddSize(spec.base_address, spec.logical_size, &specEnd) ||
        !TryAddSize(metadataAddress, valueBytes, &metadataValueEnd) ||
        (metadataAddress < spec.base_address) ||
        (metadataAddress >= specEnd) ||
        (metadataValueEnd > specEnd))
    {
        return SideMetadataResult::InvalidLayout;
    }

    uintptr_t wordAddress = metadataAddress & ~(static_cast<uintptr_t>(sizeof(uintptr_t)) - 1);
    size_t byteInWord = metadataAddress - wordAddress;
    if ((byteInWord + valueBytes) > sizeof(uintptr_t))
    {
        return SideMetadataResult::InvalidLayout;
    }

    uint32_t wordShift = ComputeWordShiftForByteOrder(
        byteInWord,
        valueBytes,
        bitShift,
#if BIGENDIAN
        true
#else
        false
#endif
    );

    if ((wordShift + bitCount) > (sizeof(uintptr_t) * 8))
    {
        return SideMetadataResult::InvalidLayout;
    }

    uintptr_t mask = bitCount == (sizeof(uintptr_t) * 8)
        ? UINTPTR_MAX
        : (LowBits(bitCount) << wordShift);

    location->word = reinterpret_cast<volatile uintptr_t*>(wordAddress);
    location->mask = mask;
    location->shift = static_cast<uint8_t>(wordShift);
    location->bit_count = static_cast<uint8_t>(bitCount);
    return SideMetadataResult::Success;
}

SideMetadataResult SideMetadataManager::Load(
    LxrSideMetadataKind kind,
    uintptr_t dataAddress,
    SideMetadataMemoryOrder order,
    uintptr_t* value) const
{
    if (value == nullptr)
    {
        return SideMetadataResult::InvalidArgument;
    }

    SideMetadataLocation location;
    SideMetadataResult result = GetLocation(kind, dataAddress, &location);
    if (result != SideMetadataResult::Success)
    {
        return result;
    }

    *value = (AtomicLoadWord(location.word, order) & location.mask) >> location.shift;
    return SideMetadataResult::Success;
}

SideMetadataResult SideMetadataManager::Store(
    LxrSideMetadataKind kind,
    uintptr_t dataAddress,
    uintptr_t value,
    SideMetadataMemoryOrder order)
{
    return Update(kind, dataAddress, value, order, UpdateOperation::Store, nullptr);
}

SideMetadataResult SideMetadataManager::CompareExchange(
    LxrSideMetadataKind kind,
    uintptr_t dataAddress,
    uintptr_t value,
    uintptr_t comparand,
    SideMetadataMemoryOrder order,
    uintptr_t* observed,
    bool* exchanged)
{
    if ((observed == nullptr) || (exchanged == nullptr))
    {
        return SideMetadataResult::InvalidArgument;
    }

    SideMetadataLocation location;
    SideMetadataResult result = GetLocation(kind, dataAddress, &location);
    if (result != SideMetadataResult::Success)
    {
        return result;
    }

    if (!IsValueValid(value, location.bit_count) || !IsValueValid(comparand, location.bit_count))
    {
        return SideMetadataResult::InvalidArgument;
    }

    uintptr_t oldWord = AtomicLoadWord(location.word, order);
    while (true)
    {
        uintptr_t oldValue = (oldWord & location.mask) >> location.shift;
        *observed = oldValue;
        if (oldValue != comparand)
        {
            *exchanged = false;
            return SideMetadataResult::Success;
        }

        uintptr_t newWord = (oldWord & ~location.mask) | ((value << location.shift) & location.mask);
        uintptr_t actual = CompareExchangeWord(location.word, newWord, oldWord);
        if (actual == oldWord)
        {
            *exchanged = true;
            return SideMetadataResult::Success;
        }
        oldWord = actual;
    }
}

SideMetadataResult SideMetadataManager::FetchOr(
    LxrSideMetadataKind kind,
    uintptr_t dataAddress,
    uintptr_t value,
    SideMetadataMemoryOrder order,
    uintptr_t* previous)
{
    return Update(kind, dataAddress, value, order, UpdateOperation::Or, previous);
}

SideMetadataResult SideMetadataManager::FetchAnd(
    LxrSideMetadataKind kind,
    uintptr_t dataAddress,
    uintptr_t value,
    SideMetadataMemoryOrder order,
    uintptr_t* previous)
{
    return Update(kind, dataAddress, value, order, UpdateOperation::And, previous);
}

SideMetadataResult SideMetadataManager::FetchAddWrapping(
    LxrSideMetadataKind kind,
    uintptr_t dataAddress,
    uintptr_t value,
    SideMetadataMemoryOrder order,
    uintptr_t* previous)
{
    return Update(kind, dataAddress, value, order, UpdateOperation::AddWrapping, previous);
}

SideMetadataResult SideMetadataManager::Update(
    LxrSideMetadataKind kind,
    uintptr_t dataAddress,
    uintptr_t value,
    SideMetadataMemoryOrder order,
    UpdateOperation operation,
    uintptr_t* previous)
{
    SideMetadataLocation location;
    SideMetadataResult result = GetLocation(kind, dataAddress, &location);
    if (result != SideMetadataResult::Success)
    {
        return result;
    }

    if (!IsValueValid(value, location.bit_count))
    {
        return SideMetadataResult::InvalidArgument;
    }

    uintptr_t valueMask = LowBits(location.bit_count);
    uintptr_t oldWord = AtomicLoadWord(location.word, order);
    while (true)
    {
        uintptr_t oldValue = (oldWord & location.mask) >> location.shift;
        uintptr_t newValue;
        switch (operation)
        {
            case UpdateOperation::Store:
                newValue = value;
                break;
            case UpdateOperation::Or:
                newValue = oldValue | value;
                break;
            case UpdateOperation::And:
                newValue = oldValue & value;
                break;
            case UpdateOperation::AddWrapping:
                newValue = (oldValue + value) & valueMask;
                break;
            default:
                return SideMetadataResult::InvalidArgument;
        }

        uintptr_t newWord = (oldWord & ~location.mask) | ((newValue << location.shift) & location.mask);
        uintptr_t actual = CompareExchangeWord(location.word, newWord, oldWord);
        if (actual == oldWord)
        {
            if (previous != nullptr)
            {
                *previous = oldValue;
            }
            return SideMetadataResult::Success;
        }
        oldWord = actual;
    }
}

SideMetadataResult SideMetadataManager::IncrementSaturating(
    LxrSideMetadataKind kind,
    uintptr_t dataAddress,
    SideMetadataMemoryOrder order,
    uintptr_t* previous,
    SideMetadataUpdateStatus* status)
{
    if ((kind != LxrSideMetadataKind::ReferenceCount) ||
        (previous == nullptr) ||
        (status == nullptr))
    {
        return SideMetadataResult::InvalidArgument;
    }

    SideMetadataLocation location;
    SideMetadataResult result = GetLocation(kind, dataAddress, &location);
    if (result != SideMetadataResult::Success)
    {
        return result;
    }

    uintptr_t maximum = LowBits(location.bit_count);
    uintptr_t oldWord = AtomicLoadWord(location.word, order);
    while (true)
    {
        uintptr_t oldValue = (oldWord & location.mask) >> location.shift;
        *previous = oldValue;
        if (oldValue == maximum)
        {
            *status = SideMetadataUpdateStatus::Saturated;
            return SideMetadataResult::Success;
        }

        uintptr_t newWord =
            (oldWord & ~location.mask) | (((oldValue + 1) << location.shift) & location.mask);
        uintptr_t actual = CompareExchangeWord(location.word, newWord, oldWord);
        if (actual == oldWord)
        {
            *status = SideMetadataUpdateStatus::Updated;
            return SideMetadataResult::Success;
        }
        oldWord = actual;
    }
}

SideMetadataResult SideMetadataManager::DecrementNonZeroNonSaturated(
    LxrSideMetadataKind kind,
    uintptr_t dataAddress,
    SideMetadataMemoryOrder order,
    uintptr_t* previous,
    SideMetadataUpdateStatus* status)
{
    if ((kind != LxrSideMetadataKind::ReferenceCount) ||
        (previous == nullptr) ||
        (status == nullptr))
    {
        return SideMetadataResult::InvalidArgument;
    }

    SideMetadataLocation location;
    SideMetadataResult result = GetLocation(kind, dataAddress, &location);
    if (result != SideMetadataResult::Success)
    {
        return result;
    }

    uintptr_t maximum = LowBits(location.bit_count);
    uintptr_t oldWord = AtomicLoadWord(location.word, order);
    while (true)
    {
        uintptr_t oldValue = (oldWord & location.mask) >> location.shift;
        *previous = oldValue;
        if (oldValue == 0)
        {
            *status = SideMetadataUpdateStatus::Zero;
            return SideMetadataResult::Success;
        }
        if (oldValue == maximum)
        {
            *status = SideMetadataUpdateStatus::Saturated;
            return SideMetadataResult::Success;
        }

        uintptr_t newWord =
            (oldWord & ~location.mask) | (((oldValue - 1) << location.shift) & location.mask);
        uintptr_t actual = CompareExchangeWord(location.word, newWord, oldWord);
        if (actual == oldWord)
        {
            *status = SideMetadataUpdateStatus::Updated;
            return SideMetadataResult::Success;
        }
        oldWord = actual;
    }
}

SideMetadataResult SideMetadataManager::VisitWords(
    LxrSideMetadataKind kind,
    uintptr_t dataStart,
    size_t dataSize,
    SideMetadataMemoryOrder order,
    SideMetadataWordVisitor visitor,
    void* context) const
{
    if ((visitor == nullptr) || (m_layout == nullptr) || m_commit_failed)
    {
        return SideMetadataResult::InvalidArgument;
    }
    if (dataSize == 0)
    {
        return SideMetadataResult::Success;
    }

    const SideMetadataSpec* spec = m_layout->GetSpec(kind);
    if ((spec == nullptr) || !IsSpecEnabled(kind))
    {
        return SideMetadataResult::InvalidArgument;
    }

    size_t granularity = static_cast<size_t>(1) << spec->log_bytes_per_value;
    if (((dataStart & (granularity - 1)) != 0) ||
        ((dataSize & (granularity - 1)) != 0) ||
        !IsDataRangeOwned(dataStart, dataSize))
    {
        return SideMetadataResult::AddressNotOwned;
    }

    uintptr_t lastAddress = dataStart + dataSize - granularity;
    SideMetadataLocation first = {};
    SideMetadataLocation last = {};
    SideMetadataResult result = GetLocationUnchecked(*spec, dataStart, &first);
    if (result == SideMetadataResult::Success)
    {
        result = GetLocationUnchecked(*spec, lastAddress, &last);
    }
    if (result != SideMetadataResult::Success)
    {
        return result;
    }

    uintptr_t firstWordAddress = reinterpret_cast<uintptr_t>(first.word);
    uintptr_t lastWordAddress = reinterpret_cast<uintptr_t>(last.word);
    for (uintptr_t wordAddress = firstWordAddress; wordAddress <= lastWordAddress; wordAddress += sizeof(uintptr_t))
    {
        uintptr_t coverage = UINTPTR_MAX;
        if (wordAddress == firstWordAddress)
        {
            coverage &= ComputeFirstWordCoverageForByteOrder(
                first,
#if BIGENDIAN
                true
#else
                false
#endif
            );
        }
        if (wordAddress == lastWordAddress)
        {
            coverage &= ComputeLastWordCoverageForByteOrder(
                last,
#if BIGENDIAN
                true
#else
                false
#endif
            );
        }

        uintptr_t value = AtomicLoadWord(reinterpret_cast<volatile uintptr_t*>(wordAddress), order);
        if (!visitor(wordAddress, value, coverage, context))
        {
            break;
        }
    }

    return SideMetadataResult::Success;
}

SideMetadataResult SideMetadataManager::IsRangeZero(
    LxrSideMetadataKind kind,
    uintptr_t start,
    size_t size,
    SideMetadataMemoryOrder order,
    bool* zero) const
{
    if (zero == nullptr)
    {
        return SideMetadataResult::InvalidArgument;
    }
    *zero = false;
    bool allZero = true;
    SideMetadataWordVisitor visitor = [](
        uintptr_t, uintptr_t value, uintptr_t coverageMask, void* context)
    {
        if ((value & coverageMask) != 0)
        {
            *static_cast<bool*>(context) = false;
            return false;
        }
        return true;
    };
    SideMetadataResult result = VisitWords(
        kind, start, size, order, visitor, &allZero);
    if (result == SideMetadataResult::Success)
    {
        *zero = allZero;
    }
    return result;
}

SideMetadataResult SideMetadataManager::ResetRangeQuiescent(
    LxrSideMetadataKind kind,
    uintptr_t dataStart,
    size_t dataSize,
    uintptr_t value)
{
    if ((m_layout == nullptr) || m_commit_failed || (dataSize == 0))
    {
        return dataSize == 0 ? SideMetadataResult::Success : SideMetadataResult::InvalidArgument;
    }

    const SideMetadataSpec* spec = m_layout->GetSpec(kind);
    if ((spec == nullptr) || !IsSpecEnabled(kind))
    {
        return SideMetadataResult::InvalidArgument;
    }

    uint32_t bitCount = static_cast<uint32_t>(1) << spec->log_bits_per_value;
    if (!IsValueValid(value, bitCount))
    {
        return SideMetadataResult::InvalidArgument;
    }

    size_t granularity = static_cast<size_t>(1) << spec->log_bytes_per_value;
    if (((dataStart & (granularity - 1)) != 0) ||
        ((dataSize & (granularity - 1)) != 0) ||
        !IsDataRangeOwned(dataStart, dataSize))
    {
        return SideMetadataResult::AddressNotOwned;
    }

    return ResetRangeQuiescentCore(*spec, dataStart, dataSize, value);
}

SideMetadataResult SideMetadataManager::FillRangeBytes(
    LxrSideMetadataKind kind,
    uintptr_t dataStart,
    size_t dataSize,
    uint8_t byteValue)
{
    if ((m_layout == nullptr) || m_commit_failed || (dataSize == 0))
    {
        return dataSize == 0 ? SideMetadataResult::Success : SideMetadataResult::InvalidArgument;
    }

    const SideMetadataSpec* spec = m_layout->GetSpec(kind);
    if ((spec == nullptr) || !IsSpecEnabled(kind) ||
        (spec->log_bits_per_value != 0) || (spec->log_bytes_per_value != 3) ||
        ((byteValue != 0) && (byteValue != 0xff)))
    {
        return SideMetadataResult::InvalidArgument;
    }

    // One metadata byte covers 8 entries of 8 data bytes each.
    constexpr size_t BytesPerMetadataByte = 64;
    if (((dataStart & (BytesPerMetadataByte - 1)) != 0) ||
        ((dataSize & (BytesPerMetadataByte - 1)) != 0) ||
        !IsDataRangeOwned(dataStart, dataSize))
    {
        return SideMetadataResult::AddressNotOwned;
    }

#if BIGENDIAN
    return ResetRangeQuiescentCore(*spec, dataStart, dataSize, byteValue == 0 ? 0 : 1);
#else
    SideMetadataLocation first = {};
    SideMetadataResult result = GetLocationUnchecked(*spec, dataStart, &first);
    if (result != SideMetadataResult::Success)
    {
        return result;
    }
    uint8_t* bytes = reinterpret_cast<uint8_t*>(const_cast<uintptr_t*>(first.word)) + (first.shift >> 3);
    _ASSERTE((first.shift & 7) == 0);
    memset(bytes, byteValue, dataSize / BytesPerMetadataByte);
    return SideMetadataResult::Success;
#endif // BIGENDIAN
}

SideMetadataResult SideMetadataManager::ResetRangeQuiescentCore(
    const SideMetadataSpec& spec,
    uintptr_t dataStart,
    size_t dataSize,
    uintptr_t value)
{
    uint32_t bitCount = static_cast<uint32_t>(1) << spec.log_bits_per_value;
    size_t granularity = static_cast<size_t>(1) << spec.log_bytes_per_value;
    SideMetadataLocation first = {};
    SideMetadataLocation last = {};
    SideMetadataResult result = GetLocationUnchecked(spec, dataStart, &first);
    if (result == SideMetadataResult::Success)
    {
        result = GetLocationUnchecked(spec, dataStart + dataSize - granularity, &last);
    }
    if (result != SideMetadataResult::Success)
    {
        return result;
    }

    uintptr_t pattern = ReplicateValue(value, bitCount);
    uintptr_t firstWordAddress = reinterpret_cast<uintptr_t>(first.word);
    uintptr_t lastWordAddress = reinterpret_cast<uintptr_t>(last.word);
    for (uintptr_t wordAddress = firstWordAddress; wordAddress <= lastWordAddress; wordAddress += sizeof(uintptr_t))
    {
        uintptr_t coverage = UINTPTR_MAX;
        if (wordAddress == firstWordAddress)
        {
            coverage &= ComputeFirstWordCoverageForByteOrder(
                first,
#if BIGENDIAN
                true
#else
                false
#endif
            );
        }
        if (wordAddress == lastWordAddress)
        {
            coverage &= ComputeLastWordCoverageForByteOrder(
                last,
#if BIGENDIAN
                true
#else
                false
#endif
            );
        }

        if (coverage == UINTPTR_MAX)
        {
            VolatileStoreWithoutBarrier(
                reinterpret_cast<uintptr_t*>(wordAddress),
                pattern);
        }
        else
        {
            volatile uintptr_t* word = reinterpret_cast<volatile uintptr_t*>(wordAddress);
            uintptr_t oldValue = AtomicLoadWord(word, SideMetadataMemoryOrder::Relaxed);
            while (true)
            {
                uintptr_t newValue = (oldValue & ~coverage) | (pattern & coverage);
                uintptr_t observed = CompareExchangeWord(word, newValue, oldValue);
                if (observed == oldValue)
                {
                    break;
                }
                oldValue = observed;
            }
        }
    }

    return SideMetadataResult::Success;
}

SideMetadataResult SideMetadataManager::ResetAllDataRangesQuiescent(
    LxrSideMetadataKind kind,
    uintptr_t value)
{
    if ((m_layout == nullptr) || m_commit_failed)
    {
        return SideMetadataResult::InvalidArgument;
    }

    const SideMetadataSpec* spec = m_layout->GetSpec(kind);
    if ((spec == nullptr) || !IsSpecEnabled(kind))
    {
        return SideMetadataResult::InvalidArgument;
    }

    uint32_t bitCount = static_cast<uint32_t>(1) << spec->log_bits_per_value;
    if (!IsValueValid(value, bitCount))
    {
        return SideMetadataResult::InvalidArgument;
    }

    size_t granularity = static_cast<size_t>(1) << spec->log_bytes_per_value;
    for (DataRange* range = m_data_ranges; range != nullptr; range = range->next)
    {
        uintptr_t alignedEnd;
        if (!TryAlignUp(range->end, granularity, &alignedEnd))
        {
            return SideMetadataResult::AddressOverflow;
        }
    }

    for (DataRange* range = m_data_ranges; range != nullptr; range = range->next)
    {
        uintptr_t alignedStart = range->start & ~(static_cast<uintptr_t>(granularity) - 1);
        uintptr_t alignedEnd;
        if (!TryAlignUp(range->end, granularity, &alignedEnd))
        {
            return SideMetadataResult::AddressOverflow;
        }

        SideMetadataResult result = ResetRangeQuiescentCore(
            *spec,
            alignedStart,
            alignedEnd - alignedStart,
            value);
        if (result != SideMetadataResult::Success)
        {
            return result;
        }
    }

    return SideMetadataResult::Success;
}

SideMetadataResult SideMetadataManager::CopyRangeQuiescent(
    LxrSideMetadataKind sourceKind,
    uintptr_t sourceStart,
    LxrSideMetadataKind destinationKind,
    uintptr_t destinationStart,
    size_t dataSize)
{
    if ((m_layout == nullptr) || m_commit_failed)
    {
        return SideMetadataResult::InvalidArgument;
    }
    if (dataSize == 0)
    {
        return SideMetadataResult::Success;
    }

    const SideMetadataSpec* source = m_layout->GetSpec(sourceKind);
    const SideMetadataSpec* destination = m_layout->GetSpec(destinationKind);
    if ((source == nullptr) ||
        (destination == nullptr) ||
        !IsSpecEnabled(sourceKind) ||
        !IsSpecEnabled(destinationKind) ||
        (source->log_bits_per_value != destination->log_bits_per_value) ||
        (source->log_bytes_per_value != destination->log_bytes_per_value))
    {
        return SideMetadataResult::InvalidArgument;
    }

    size_t granularity = static_cast<size_t>(1) << source->log_bytes_per_value;
    if (((sourceStart | destinationStart | dataSize) & (granularity - 1)) != 0 ||
        !IsDataRangeOwned(sourceStart, dataSize) ||
        !IsDataRangeOwned(destinationStart, dataSize))
    {
        return SideMetadataResult::AddressNotOwned;
    }

    if (sourceStart == destinationStart)
    {
        SideMetadataLocation sourceFirst = {};
        SideMetadataLocation sourceLast = {};
        SideMetadataLocation destinationFirst = {};
        SideMetadataLocation destinationLast = {};
        SideMetadataResult result = GetLocationUnchecked(*source, sourceStart, &sourceFirst);
        if (result == SideMetadataResult::Success)
        {
            result = GetLocationUnchecked(
                *source,
                sourceStart + dataSize - granularity,
                &sourceLast);
        }
        if (result == SideMetadataResult::Success)
        {
            result = GetLocationUnchecked(*destination, destinationStart, &destinationFirst);
        }
        if (result == SideMetadataResult::Success)
        {
            result = GetLocationUnchecked(
                *destination,
                destinationStart + dataSize - granularity,
                &destinationLast);
        }
        if (result != SideMetadataResult::Success)
        {
            return result;
        }

        uintptr_t sourceFirstAddress = reinterpret_cast<uintptr_t>(sourceFirst.word);
        uintptr_t sourceLastAddress = reinterpret_cast<uintptr_t>(sourceLast.word);
        uintptr_t destinationFirstAddress = reinterpret_cast<uintptr_t>(destinationFirst.word);
        uintptr_t destinationLastAddress = reinterpret_cast<uintptr_t>(destinationLast.word);
        size_t sourceWordCount =
            ((sourceLastAddress - sourceFirstAddress) / sizeof(uintptr_t)) + 1;
        size_t destinationWordCount =
            ((destinationLastAddress - destinationFirstAddress) / sizeof(uintptr_t)) + 1;
        if ((sourceFirst.shift == destinationFirst.shift) &&
            (sourceLast.shift == destinationLast.shift) &&
            (sourceWordCount == destinationWordCount))
        {
            for (size_t index = 0; index < sourceWordCount; index++)
            {
                uintptr_t coverage = UINTPTR_MAX;
                if (index == 0)
                {
                    coverage &= ComputeFirstWordCoverageForByteOrder(
                        sourceFirst,
#if BIGENDIAN
                        true
#else
                        false
#endif
                    );
                }
                if (index == (sourceWordCount - 1))
                {
                    coverage &= ComputeLastWordCoverageForByteOrder(
                        sourceLast,
#if BIGENDIAN
                        true
#else
                        false
#endif
                    );
                }

                uintptr_t sourceWordAddress = sourceFirstAddress + (index * sizeof(uintptr_t));
                uintptr_t destinationWordAddress =
                    destinationFirstAddress + (index * sizeof(uintptr_t));
                uintptr_t sourceValue = VolatileLoadWithoutBarrier(
                    reinterpret_cast<uintptr_t*>(sourceWordAddress));
                if (coverage == UINTPTR_MAX)
                {
                    VolatileStoreWithoutBarrier(
                        reinterpret_cast<uintptr_t*>(destinationWordAddress),
                        sourceValue);
                }
                else
                {
                    volatile uintptr_t* destinationWord =
                        reinterpret_cast<volatile uintptr_t*>(destinationWordAddress);
                    uintptr_t oldValue =
                        AtomicLoadWord(destinationWord, SideMetadataMemoryOrder::Relaxed);
                    while (true)
                    {
                        uintptr_t newValue =
                            (oldValue & ~coverage) | (sourceValue & coverage);
                        uintptr_t observed =
                            CompareExchangeWord(destinationWord, newValue, oldValue);
                        if (observed == oldValue)
                        {
                            break;
                        }
                        oldValue = observed;
                    }
                }
            }
            return SideMetadataResult::Success;
        }
    }

    size_t count = dataSize / granularity;
    bool backward = (destinationStart > sourceStart) && (destinationStart < (sourceStart + dataSize));
    for (size_t offset = 0; offset < count; offset++)
    {
        size_t index = backward ? (count - offset - 1) : offset;
        uintptr_t value;
        SideMetadataResult result = Load(
            sourceKind,
            sourceStart + (index * granularity),
            SideMetadataMemoryOrder::Relaxed,
            &value);
        if (result != SideMetadataResult::Success)
        {
            return result;
        }

        result = Store(
            destinationKind,
            destinationStart + (index * granularity),
            value,
            SideMetadataMemoryOrder::Relaxed);
        if (result != SideMetadataResult::Success)
        {
            return result;
        }
    }

    return SideMetadataResult::Success;
}

uintptr_t SideMetadataManager::GetBiasedBase(LxrSideMetadataKind kind) const
{
    if ((m_layout == nullptr) || !IsSpecEnabled(kind))
    {
        return 0;
    }

    const SideMetadataSpec* spec = m_layout->GetSpec(kind);
    return ((spec != nullptr) && (spec->log_bits_per_value == 0)) ? spec->base_address : 0;
}

SideMetadataAccessor SideMetadataManager::GetAccessor(LxrSideMetadataKind kind) const
{
    SideMetadataAccessor accessor;
    if ((m_layout == nullptr) || !IsSpecEnabled(kind))
    {
        return accessor;
    }

    const SideMetadataSpec* spec = m_layout->GetSpec(kind);
    // The inline location formula needs the base address aligned to a word and values that fit in a word.
    if ((spec == nullptr) ||
        (spec->base_address == 0) ||
        ((spec->base_address % sizeof(uintptr_t)) != 0) ||
        (spec->log_bits_per_value > 6))
    {
        return accessor;
    }

    accessor.m_base = spec->base_address;
    accessor.m_value_mask = LowBits(static_cast<uint32_t>(1) << spec->log_bits_per_value);
    accessor.m_manager = this;
    accessor.m_log_bytes_per_value = spec->log_bytes_per_value;
    accessor.m_log_bits_per_value = spec->log_bits_per_value;
    accessor.m_kind = kind;
    return accessor;
}

SideMetadataResult SideMetadataManager::AddDataRange(uintptr_t start, uintptr_t end)
{
    for (DataRange* range = m_data_ranges; range != nullptr; range = range->next)
    {
        if ((start >= range->start) && (end <= range->end))
        {
            return SideMetadataResult::Success;
        }
    }

    DataRange* mergedRange = new (nothrow) DataRange;
    if (mergedRange == nullptr)
    {
        return SideMetadataResult::OutOfMemory;
    }

    bool merged;
    do
    {
        merged = false;
        DataRange** current = &m_data_ranges;
        while (*current != nullptr)
        {
            DataRange* range = *current;
            if ((end < range->start) || (start > range->end))
            {
                current = &range->next;
                continue;
            }

            start = min(start, range->start);
            end = max(end, range->end);
            *current = range->next;
            delete range;
            merged = true;
            break;
        }
    }
    while (merged);

    mergedRange->start = start;
    mergedRange->end = end;
    mergedRange->next = m_data_ranges;
    m_data_ranges = mergedRange;
    return SideMetadataResult::Success;
}

SideMetadataResult SideMetadataManager::RemoveDataRange(uintptr_t start, uintptr_t end)
{
    DataRange** current = &m_data_ranges;
    while (*current != nullptr)
    {
        DataRange* range = *current;
        if ((start >= range->start) && (end <= range->end))
        {
            bool atStart = start == range->start;
            bool atEnd = end == range->end;
            if (atStart && atEnd)
            {
                *current = range->next;
                delete range;
            }
            else if (atStart)
            {
                range->start = end;
            }
            else if (atEnd)
            {
                range->end = start;
            }
            else
            {
                // Touching ranges were merged when they were added (the Immix and the large-object range), so the
                // range that is given back can lie inside one of them.
                DataRange* upper = new (nothrow) DataRange;
                if (upper == nullptr)
                {
                    return SideMetadataResult::OutOfMemory;
                }
                upper->start = end;
                upper->end = range->end;
                upper->next = range->next;
                range->end = start;
                range->next = upper;
            }
            return SideMetadataResult::Success;
        }
        current = &range->next;
    }
    return SideMetadataResult::InvalidArgument;
}

void SideMetadataManager::ClearDataRanges()
{
    while (m_data_ranges != nullptr)
    {
        DataRange* range = m_data_ranges;
        m_data_ranges = range->next;
        delete range;
    }
}

uintptr_t SideMetadataManager::AtomicLoadWord(
    volatile uintptr_t* word,
    SideMetadataMemoryOrder order)
{
    return SideMetadataAccessor::LoadWord(word, order);
}

uintptr_t SideMetadataManager::CompareExchangeWord(
    volatile uintptr_t* word,
    uintptr_t value,
    uintptr_t comparand)
{
    return SideMetadataAccessor::CompareExchangeWord(word, value, comparand);
}

uintptr_t SideMetadataManager::LowBits(uint32_t count)
{
    return count >= (sizeof(uintptr_t) * 8)
        ? UINTPTR_MAX
        : ((static_cast<uintptr_t>(1) << count) - 1);
}

uintptr_t SideMetadataManager::ReplicateValue(uintptr_t value, uint32_t bitCount)
{
    if (bitCount >= (sizeof(uintptr_t) * 8))
    {
        return value;
    }

    uintptr_t pattern = 0;
    for (uint32_t shift = 0; shift < (sizeof(uintptr_t) * 8); shift += bitCount)
    {
        pattern |= value << shift;
    }
    return pattern;
}

uint32_t SideMetadataManager::ComputeWordShiftForByteOrder(
    size_t byteInWord,
    size_t valueBytes,
    uint32_t bitShift,
    bool bigEndian)
{
    return bigEndian
        ? static_cast<uint32_t>((sizeof(uintptr_t) - byteInWord - valueBytes) * 8) + bitShift
        : static_cast<uint32_t>(byteInWord * 8) + bitShift;
}

uintptr_t SideMetadataManager::ComputeFirstWordCoverageForByteOrder(
    const SideMetadataLocation& location,
    bool bigEndian)
{
    if (!bigEndian)
    {
        return UINTPTR_MAX << location.shift;
    }
    if (location.bit_count >= 8)
    {
        return LowBits(location.shift + location.bit_count);
    }

    uint32_t byteShift = location.shift & ~7u;
    uint32_t bitInByte = location.shift - byteShift;
    uintptr_t laterBytes = LowBits(byteShift);
    uintptr_t currentByte =
        ((static_cast<uintptr_t>(UINT8_MAX) << bitInByte) & UINT8_MAX) << byteShift;
    return laterBytes | currentByte;
}

uintptr_t SideMetadataManager::ComputeLastWordCoverageForByteOrder(
    const SideMetadataLocation& location,
    bool bigEndian)
{
    if (!bigEndian)
    {
        return LowBits(location.shift + location.bit_count);
    }
    if (location.bit_count >= 8)
    {
        return UINTPTR_MAX << location.shift;
    }

    uint32_t byteShift = location.shift & ~7u;
    uint32_t bitInByte = location.shift - byteShift;
    uintptr_t earlierBytes = (byteShift + 8) >= (sizeof(uintptr_t) * 8)
        ? 0
        : (UINTPTR_MAX << (byteShift + 8));
    uintptr_t currentByte =
        LowBits(bitInByte + location.bit_count) << byteShift;
    return earlierBytes | currentByte;
}

bool SideMetadataManager::IsSpecEnabled(LxrSideMetadataKind kind) const
{
    size_t index = static_cast<size_t>(kind);
    return (index < LxrSideMetadataLayout::SpecCount) &&
        ((m_enabled_spec_mask & (UINT64_C(1) << index)) != 0);
}
