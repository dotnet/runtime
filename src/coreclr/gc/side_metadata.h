// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef SIDE_METADATA_H
#define SIDE_METADATA_H

enum class SideMetadataScope : uint8_t
{
    Global,
    Local,
};

enum class SideMetadataMemoryOrder : uint8_t
{
    Relaxed,
    SequentiallyConsistent,
};

enum class SideMetadataResult : uint8_t
{
    Success,
    InvalidArgument,
    InvalidLayout,
    UnsupportedAddressSpace,
    AddressNotOwned,
    AddressOverflow,
    ReservationFailed,
    CommitFailed,
    OutOfMemory,
    InconsistentState,
};

enum class SideMetadataUpdateStatus : uint8_t
{
    Updated,
    Unchanged,
    Zero,
    Saturated,
};

enum class LxrSideMetadataKind : uint8_t
{
    ValidObject,
    ReferenceCount,
    FieldUnlogged,
    BlockDefrag,
    ObjectMark,
    ReferenceCountStraddle,
    BlockMark,
    BlockLog,
    NurseryPromotion,
    PhaseEpoch,
    LineReuse,
    LargeObjectMarkNursery,
    LargeObjectPageReuse,
    BlockOwner,
    BlockInUse,
    Count,
};

struct SideMetadataSpec
{
    LxrSideMetadataKind kind;
    SideMetadataScope scope;
    uintptr_t base_address;
    size_t logical_size;
    size_t reserved_size;
    uint8_t log_bits_per_value;
    uint8_t reserved_log_bits_per_value;
    uint8_t log_bytes_per_value;
};

class LxrSideMetadataLayout final
{
public:
    static constexpr uint8_t AddressBits = 47;
    static constexpr uint64_t AddressLimit = UINT64_C(1) << AddressBits;
    static constexpr uint64_t GlobalBaseAddress = UINT64_C(0x00000c0000000000);
    static constexpr uint64_t LocalBaseAddress = UINT64_C(0x00004c0000000000);
    static constexpr size_t SpecCount = static_cast<size_t>(LxrSideMetadataKind::Count);

    static SideMetadataResult Create(uint8_t logReferenceCountBits, LxrSideMetadataLayout* layout);

    const SideMetadataSpec* GetSpec(LxrSideMetadataKind kind) const;
    const SideMetadataSpec* GetSpecs() const
    {
        return m_specs;
    }

    size_t GetSpecCount() const
    {
        return SpecCount;
    }

    uintptr_t GetGlobalBase() const
    {
        return static_cast<uintptr_t>(GlobalBaseAddress);
    }

    size_t GetGlobalSize() const
    {
        return m_global_size;
    }

    uintptr_t GetLocalBase() const
    {
        return static_cast<uintptr_t>(LocalBaseAddress);
    }

    size_t GetLocalSize() const
    {
        return m_local_size;
    }

    uint64_t GetAddressLimit() const
    {
        return AddressLimit;
    }

private:
    SideMetadataSpec m_specs[SpecCount];
    size_t m_global_size;
    size_t m_local_size;
};

struct SideMetadataLocation
{
    volatile uintptr_t* word;
    uintptr_t mask;
    uint8_t shift;
    uint8_t bit_count;
};

typedef bool (*SideMetadataWordVisitor)(
    uintptr_t metadataAddress,
    uintptr_t value,
    uintptr_t coverageMask,
    void* context);

class SideMetadataManager final
{
public:
    SideMetadataManager();
    ~SideMetadataManager();
    SideMetadataManager(const SideMetadataManager&) = delete;
    SideMetadataManager& operator=(const SideMetadataManager&) = delete;

    SideMetadataResult Initialize(
        const LxrSideMetadataLayout* layout,
        uint64_t enabledSpecMask = UINT64_MAX);
    void Shutdown();

    // CommitDataRange is an initialization/collector-quiescent operation. It must not race
    // metadata accesses or another commit on the same manager.
    SideMetadataResult CommitDataRange(uintptr_t start, size_t size);
    bool IsDataAddressOwned(uintptr_t address) const;
    bool IsDataRangeOwned(uintptr_t start, size_t size) const;

    SideMetadataResult GetLocation(
        LxrSideMetadataKind kind,
        uintptr_t dataAddress,
        SideMetadataLocation* location) const;

    SideMetadataResult Load(
        LxrSideMetadataKind kind,
        uintptr_t dataAddress,
        SideMetadataMemoryOrder order,
        uintptr_t* value) const;

    SideMetadataResult Store(
        LxrSideMetadataKind kind,
        uintptr_t dataAddress,
        uintptr_t value,
        SideMetadataMemoryOrder order);

    SideMetadataResult CompareExchange(
        LxrSideMetadataKind kind,
        uintptr_t dataAddress,
        uintptr_t value,
        uintptr_t comparand,
        SideMetadataMemoryOrder order,
        uintptr_t* observed,
        bool* exchanged);

    SideMetadataResult FetchOr(
        LxrSideMetadataKind kind,
        uintptr_t dataAddress,
        uintptr_t value,
        SideMetadataMemoryOrder order,
        uintptr_t* previous);

    SideMetadataResult FetchAnd(
        LxrSideMetadataKind kind,
        uintptr_t dataAddress,
        uintptr_t value,
        SideMetadataMemoryOrder order,
        uintptr_t* previous);

    SideMetadataResult FetchAddWrapping(
        LxrSideMetadataKind kind,
        uintptr_t dataAddress,
        uintptr_t value,
        SideMetadataMemoryOrder order,
        uintptr_t* previous);

    SideMetadataResult IncrementSaturating(
        LxrSideMetadataKind kind,
        uintptr_t dataAddress,
        SideMetadataMemoryOrder order,
        uintptr_t* previous,
        SideMetadataUpdateStatus* status);

    SideMetadataResult DecrementNonZeroNonSaturated(
        LxrSideMetadataKind kind,
        uintptr_t dataAddress,
        SideMetadataMemoryOrder order,
        uintptr_t* previous,
        SideMetadataUpdateStatus* status);

    SideMetadataResult VisitWords(
        LxrSideMetadataKind kind,
        uintptr_t dataStart,
        size_t dataSize,
        SideMetadataMemoryOrder order,
        SideMetadataWordVisitor visitor,
        void* context) const;

    SideMetadataResult ResetRangeQuiescent(
        LxrSideMetadataKind kind,
        uintptr_t dataStart,
        size_t dataSize,
        uintptr_t value);

    SideMetadataResult IsRangeZero(
        LxrSideMetadataKind kind,
        uintptr_t start,
        size_t size,
        SideMetadataMemoryOrder order,
        bool* zero) const;

    SideMetadataResult ResetAllDataRangesQuiescent(
        LxrSideMetadataKind kind,
        uintptr_t value);

    SideMetadataResult CopyRangeQuiescent(
        LxrSideMetadataKind sourceKind,
        uintptr_t sourceStart,
        LxrSideMetadataKind destinationKind,
        uintptr_t destinationStart,
        size_t dataSize);

    bool IsSpecEnabled(LxrSideMetadataKind kind) const;
    uintptr_t GetBiasedBase(LxrSideMetadataKind kind) const;

    static uint32_t ComputeWordShiftForByteOrder(
        size_t byteInWord,
        size_t valueBytes,
        uint32_t bitShift,
        bool bigEndian);

    static uintptr_t ComputeFirstWordCoverageForByteOrder(
        const SideMetadataLocation& location,
        bool bigEndian);

    static uintptr_t ComputeLastWordCoverageForByteOrder(
        const SideMetadataLocation& location,
        bool bigEndian);

private:
    struct DataRange
    {
        uintptr_t start;
        uintptr_t end;
        DataRange* next;
    };

    enum class UpdateOperation : uint8_t
    {
        Store,
        Or,
        And,
        AddWrapping,
    };

    SideMetadataResult GetLocationUnchecked(
        const SideMetadataSpec& spec,
        uintptr_t dataAddress,
        SideMetadataLocation* location) const;

    SideMetadataResult Update(
        LxrSideMetadataKind kind,
        uintptr_t dataAddress,
        uintptr_t value,
        SideMetadataMemoryOrder order,
        UpdateOperation operation,
        uintptr_t* previous);
    SideMetadataResult ResetRangeQuiescentCore(
        const SideMetadataSpec& spec,
        uintptr_t dataStart,
        size_t dataSize,
        uintptr_t value);
    SideMetadataResult AddDataRange(uintptr_t start, uintptr_t end);
    void ClearDataRanges();

    static uintptr_t AtomicLoadWord(volatile uintptr_t* word, SideMetadataMemoryOrder order);
    static uintptr_t CompareExchangeWord(volatile uintptr_t* word, uintptr_t value, uintptr_t comparand);
    static uintptr_t LowBits(uint32_t count);
    static uintptr_t ReplicateValue(uintptr_t value, uint32_t bitCount);

    const LxrSideMetadataLayout* m_layout;
    void* m_global_reservation;
    void* m_local_reservation;
    DataRange* m_data_ranges;
    uint64_t m_enabled_spec_mask;
    bool m_commit_failed;
};

#endif // SIDE_METADATA_H
