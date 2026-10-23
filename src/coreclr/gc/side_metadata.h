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

class SideMetadataManager;

// Inline fast path for the hot side-metadata accesses.
//
// The layout is chosen at run time, so the base address, the shifts and the value mask of one
// metadata kind are not compile-time constants. SideMetadataManager::GetAccessor computes them once
// and the owner of the accessor keeps the copy. One access is then a shift, an add of the base and
// one atomic word access, with the same memory orders as the checked SideMetadataManager functions.
//
// Invariant that makes the missing checks safe: the data address must belong to an object, a line or
// a block that the engine has already admitted (SideMetadataManager::CommitDataRange has committed the
// metadata of its range), and it must have the alignment that the object model guarantees. Callers
// that handle untrusted addresses (tests of error paths, interior pointers, conservative probes) must
// use the checked SideMetadataManager functions, which return an error code instead.
//
// Debug builds (_DEBUG) check every access against the checked path: the address must be owned, and
// the location (word, mask, shift, bit count) must be the same. Loads also compare the value with a
// checked load. A mismatch asserts. The value comparison repeats a few times because another thread
// may change the value between the two loads; a real decoding fault does not go away.
class SideMetadataAccessor final
{
public:
    SideMetadataAccessor() = default;

    bool IsValid() const
    {
        return m_base != 0;
    }

    LxrSideMetadataKind GetKind() const
    {
        return m_kind;
    }

    uintptr_t GetValueMask() const
    {
        return m_value_mask;
    }

    uint8_t GetLogBitsPerValue() const
    {
        return m_log_bits_per_value;
    }

    uint8_t GetLogBytesPerValue() const
    {
        return m_log_bytes_per_value;
    }

    SideMetadataLocation Locate(uintptr_t dataAddress) const;

    uintptr_t Load(uintptr_t dataAddress, SideMetadataMemoryOrder order) const;

    void Store(uintptr_t dataAddress, uintptr_t value, SideMetadataMemoryOrder order) const;

    // Returns true when the value was exchanged. The value that was seen is always returned in *observed.
    bool CompareExchange(
        uintptr_t dataAddress,
        uintptr_t value,
        uintptr_t comparand,
        SideMetadataMemoryOrder order,
        uintptr_t* observed) const;

    // Returns the previous value.
    uintptr_t FetchAddWrapping(
        uintptr_t dataAddress,
        uintptr_t value,
        SideMetadataMemoryOrder order) const;

    SideMetadataUpdateStatus IncrementSaturating(
        uintptr_t dataAddress,
        SideMetadataMemoryOrder order,
        uintptr_t* previous) const;

    SideMetadataUpdateStatus DecrementNonZeroNonSaturated(
        uintptr_t dataAddress,
        SideMetadataMemoryOrder order,
        uintptr_t* previous) const;

    // Same result as SideMetadataManager::IsRangeZero. The start and the size must be multiples of the
    // data bytes that one entry covers, and the whole range must be admitted. A size of zero is zero.
    bool IsRangeZero(uintptr_t dataStart, size_t dataSize, SideMetadataMemoryOrder order) const;

    static uintptr_t LoadWord(volatile uintptr_t* word, SideMetadataMemoryOrder order);
    static uintptr_t CompareExchangeWord(volatile uintptr_t* word, uintptr_t value, uintptr_t comparand);

private:
    friend class SideMetadataManager;

    // The byte that holds the entry, and the shift of the entry in it (little endian, entries of up to 8 bits).
    volatile uint8_t* LocateByte(uintptr_t dataAddress, uint32_t* shift) const;

    static bool AreWordsZero(
        const SideMetadataLocation& first,
        const SideMetadataLocation& last,
        SideMetadataMemoryOrder order);

#ifdef _DEBUG
    void VerifyLocation(uintptr_t dataAddress, const SideMetadataLocation& location) const;
    void VerifyLoad(uintptr_t dataAddress, const SideMetadataLocation& location, uintptr_t value) const;
#endif // _DEBUG

    uintptr_t m_base = 0;
    uintptr_t m_value_mask = 0;
    const SideMetadataManager* m_manager = nullptr;
    uint8_t m_log_bytes_per_value = 0;
    uint8_t m_log_bits_per_value = 0;
    LxrSideMetadataKind m_kind = LxrSideMetadataKind::Count;
};

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
    // The reverse of CommitDataRange for a range inside an owned range (in practice the free tail of the Immix space):
    // the metadata of the range is made zero again. Whole pages are decommitted, and the part of a page that stays
    // committed (shared with the data next to it) is cleared, down to single bits when several entries share a
    // byte. The range is no longer owned afterwards, so a checked access to it fails. Collector-quiescent, like
    // CommitDataRange. The range must start and end on a whole entry of every enabled kind (a block boundary does).
    SideMetadataResult DecommitDataRange(uintptr_t start, size_t size);
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

    // The refill clears and sets the planes of a range of lines as the Java LXR does: a memset of the metadata bytes
    // (Block::clear_field_unlog_table, Line::initialize_field_unlog_table_as_unlogged, Line::initialize_mark_table_as_marked
    // in mmtk-core). Only for a plane of one bit per 8 data bytes. The data range must start and end on a multiple of 64
    // bytes, so that it covers whole metadata bytes; byteValue is 0x00 (all entries 0) or 0xff (all entries 1).
    SideMetadataResult FillRangeBytes(
        LxrSideMetadataKind kind,
        uintptr_t dataStart,
        size_t dataSize,
        uint8_t byteValue);

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

    // Builds the inline accessor of one kind. The result is invalid when the manager is not
    // initialized or the kind is not enabled. Call it once and keep the copy.
    SideMetadataAccessor GetAccessor(LxrSideMetadataKind kind) const;

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
    SideMetadataResult RemoveDataRange(uintptr_t start, uintptr_t end);
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

inline uintptr_t SideMetadataAccessor::LoadWord(volatile uintptr_t* word, SideMetadataMemoryOrder order)
{
    if (order == SideMetadataMemoryOrder::Relaxed)
    {
        return VolatileLoadWithoutBarrier(word);
    }

    // P28 (F9, F10). A sequentially consistent load is an acquire load, as it is in the Java LXR (Rust compiles
    // load_atomic(SeqCst) to a plain load on x64 and to ldar on ARM64). The order against an earlier store comes from the
    // store: every store of this class is a compare-exchange loop, which is a full barrier on both architectures. The
    // earlier code put a full barrier before and after each load (two mfence on x64). The selector --f10-order tests it.
    return VolatileLoad(word);
}

inline uintptr_t SideMetadataAccessor::CompareExchangeWord(
    volatile uintptr_t* word,
    uintptr_t value,
    uintptr_t comparand)
{
    return reinterpret_cast<uintptr_t>(
        Interlocked::CompareExchangePointer(
            reinterpret_cast<void* volatile*>(word),
            reinterpret_cast<void*>(value),
            reinterpret_cast<void*>(comparand)));
}

inline volatile uint8_t* SideMetadataAccessor::LocateByte(uintptr_t dataAddress, uint32_t* shift) const
{
    uintptr_t bitPosition = (dataAddress >> m_log_bytes_per_value) << m_log_bits_per_value;
    *shift = static_cast<uint32_t>(bitPosition & 7);
    return reinterpret_cast<volatile uint8_t*>(m_base + (bitPosition >> 3));
}

inline SideMetadataLocation SideMetadataAccessor::Locate(uintptr_t dataAddress) const
{
    // Entries are numbered from the data address; each entry takes 1 << m_log_bits_per_value bits.
    // The base address is aligned to a word, so the word offset and the shift follow from the bit position.
    uintptr_t bitPosition = (dataAddress >> m_log_bytes_per_value) << m_log_bits_per_value;
    uintptr_t wordAddress = m_base + ((bitPosition >> 6) << 3);
#if BIGENDIAN
    uint32_t bitCount = static_cast<uint32_t>(1) << m_log_bits_per_value;
    uint32_t shift = SideMetadataManager::ComputeWordShiftForByteOrder(
        static_cast<size_t>((bitPosition >> 3) & (sizeof(uintptr_t) - 1)),
        bitCount >= 8 ? bitCount / 8 : 1,
        static_cast<uint32_t>(bitPosition & 7),
        true);
#else
    uint32_t shift = static_cast<uint32_t>(bitPosition & ((sizeof(uintptr_t) * 8) - 1));
#endif
    SideMetadataLocation location;
    location.word = reinterpret_cast<volatile uintptr_t*>(wordAddress);
    location.mask = m_value_mask << shift;
    location.shift = static_cast<uint8_t>(shift);
    location.bit_count = static_cast<uint8_t>(static_cast<uint32_t>(1) << m_log_bits_per_value);
    return location;
}

#ifdef _DEBUG
inline void SideMetadataAccessor::VerifyLocation(
    uintptr_t dataAddress,
    const SideMetadataLocation& location) const
{
    SideMetadataLocation checked;
    SideMetadataResult result = m_manager->GetLocation(m_kind, dataAddress, &checked);
    _ASSERTE(result == SideMetadataResult::Success);
    _ASSERTE(checked.word == location.word);
    _ASSERTE(checked.mask == location.mask);
    _ASSERTE(checked.shift == location.shift);
    _ASSERTE(checked.bit_count == location.bit_count);
    (void)result;
    (void)checked;
}

inline void SideMetadataAccessor::VerifyLoad(
    uintptr_t dataAddress,
    const SideMetadataLocation& location,
    uintptr_t value) const
{
    VerifyLocation(dataAddress, location);
    for (int attempt = 0; attempt < 16; attempt++)
    {
        uintptr_t checkedValue = 0;
        SideMetadataResult result = m_manager->Load(
            m_kind, dataAddress, SideMetadataMemoryOrder::Relaxed, &checkedValue);
        _ASSERTE(result == SideMetadataResult::Success);
        if ((result != SideMetadataResult::Success) || (checkedValue == value))
        {
            return;
        }
        value = (LoadWord(location.word, SideMetadataMemoryOrder::Relaxed) >> location.shift) & m_value_mask;
    }
    _ASSERTE(!"The inline side-metadata load and the checked load disagree.");
}
#endif // _DEBUG

inline uintptr_t SideMetadataAccessor::Load(uintptr_t dataAddress, SideMetadataMemoryOrder order) const
{
    SideMetadataLocation location = Locate(dataAddress);
    uintptr_t value = (LoadWord(location.word, order) >> location.shift) & m_value_mask;
#ifdef _DEBUG
    VerifyLoad(dataAddress, location, value);
#endif // _DEBUG
    return value;
}

inline void SideMetadataAccessor::Store(
    uintptr_t dataAddress,
    uintptr_t value,
    SideMetadataMemoryOrder order) const
{
    SideMetadataLocation location = Locate(dataAddress);
#ifdef _DEBUG
    VerifyLocation(dataAddress, location);
    _ASSERTE((value & ~m_value_mask) == 0);
#endif // _DEBUG
#ifdef LXR_F10_PLAIN_STORE_CONTROL
    // Control: a store is a plain store, which is not a barrier. A sequentially consistent load after it can be reordered.
    if (order == SideMetadataMemoryOrder::SequentiallyConsistent)
    {
        uintptr_t plainWord = VolatileLoadWithoutBarrier(location.word);
        VolatileStoreWithoutBarrier(
            const_cast<uintptr_t*>(location.word),
            static_cast<uintptr_t>((plainWord & ~location.mask) | ((value << location.shift) & location.mask)));
        return;
    }
#endif // LXR_F10_PLAIN_STORE_CONTROL
    uintptr_t oldWord = LoadWord(location.word, order);
    while (true)
    {
        uintptr_t newWord = (oldWord & ~location.mask) | ((value << location.shift) & location.mask);
        uintptr_t actual = CompareExchangeWord(location.word, newWord, oldWord);
        if (actual == oldWord)
        {
            return;
        }
        oldWord = actual;
    }
}

inline bool SideMetadataAccessor::CompareExchange(
    uintptr_t dataAddress,
    uintptr_t value,
    uintptr_t comparand,
    SideMetadataMemoryOrder order,
    uintptr_t* observed) const
{
#if !BIGENDIAN
    if (m_log_bits_per_value <= 3 && order == SideMetadataMemoryOrder::Relaxed)
    {
        uint32_t byteShift;
        volatile uint8_t* byte = LocateByte(dataAddress, &byteShift);
#ifdef _DEBUG
        {
            SideMetadataLocation checked = Locate(dataAddress);
            VerifyLocation(dataAddress, checked);
            _ASSERTE(reinterpret_cast<volatile uint8_t*>(checked.word) + (checked.shift >> 3) == byte);
            _ASSERTE((checked.shift & 7) == byteShift);
        }
#endif // _DEBUG
#ifdef _DEBUG
        _ASSERTE((value & ~m_value_mask) == 0);
        _ASSERTE((comparand & ~m_value_mask) == 0);
#endif // _DEBUG
        const uint8_t mask = static_cast<uint8_t>(m_value_mask << byteShift);
        uint8_t oldByte = VolatileLoadWithoutBarrier(byte);
        while (true)
        {
            uint8_t oldValue = static_cast<uint8_t>((oldByte & mask) >> byteShift);
            *observed = oldValue;
            if (oldValue != comparand)
            {
                return false;
            }
            uint8_t newByte = static_cast<uint8_t>((oldByte & ~mask) | (value << byteShift));
            uint8_t actual = Interlocked::CompareExchange(byte, newByte, oldByte);
            if (actual == oldByte)
            {
                return true;
            }
            oldByte = actual;
        }
    }
#endif // !BIGENDIAN
    SideMetadataLocation location = Locate(dataAddress);
#ifdef _DEBUG
    VerifyLocation(dataAddress, location);
    _ASSERTE((value & ~m_value_mask) == 0);
    _ASSERTE((comparand & ~m_value_mask) == 0);
#endif // _DEBUG
    uintptr_t oldWord = LoadWord(location.word, order);
    while (true)
    {
        uintptr_t oldValue = (oldWord >> location.shift) & m_value_mask;
        *observed = oldValue;
        if (oldValue != comparand)
        {
            return false;
        }

        uintptr_t newWord = (oldWord & ~location.mask) | ((value << location.shift) & location.mask);
        uintptr_t actual = CompareExchangeWord(location.word, newWord, oldWord);
        if (actual == oldWord)
        {
            return true;
        }
        oldWord = actual;
    }
}

inline uintptr_t SideMetadataAccessor::FetchAddWrapping(
    uintptr_t dataAddress,
    uintptr_t value,
    SideMetadataMemoryOrder order) const
{
    SideMetadataLocation location = Locate(dataAddress);
#ifdef _DEBUG
    VerifyLocation(dataAddress, location);
    _ASSERTE((value & ~m_value_mask) == 0);
#endif // _DEBUG
    uintptr_t oldWord = LoadWord(location.word, order);
    while (true)
    {
        uintptr_t oldValue = (oldWord >> location.shift) & m_value_mask;
        uintptr_t newValue = (oldValue + value) & m_value_mask;
        uintptr_t newWord = (oldWord & ~location.mask) | ((newValue << location.shift) & location.mask);
        uintptr_t actual = CompareExchangeWord(location.word, newWord, oldWord);
        if (actual == oldWord)
        {
            return oldValue;
        }
        oldWord = actual;
    }
}

inline SideMetadataUpdateStatus SideMetadataAccessor::IncrementSaturating(
    uintptr_t dataAddress,
    SideMetadataMemoryOrder order,
    uintptr_t* previous) const
{
#if !BIGENDIAN
    // P28 (F10). The count is updated with a compare-exchange on the byte that holds it, as the Java LXR does
    // (side_metadata fetch_update_atomic::<u8>), not on the 64-bit word: with counts of 2 bits a word holds 32 counts.
    if (m_log_bits_per_value <= 3 && order == SideMetadataMemoryOrder::Relaxed)
    {
        uint32_t byteShift;
        volatile uint8_t* byte = LocateByte(dataAddress, &byteShift);
#ifdef _DEBUG
        {
            SideMetadataLocation checked = Locate(dataAddress);
            VerifyLocation(dataAddress, checked);
            _ASSERTE(reinterpret_cast<volatile uint8_t*>(checked.word) + (checked.shift >> 3) == byte);
            _ASSERTE((checked.shift & 7) == byteShift);
        }
#endif // _DEBUG
        const uint8_t mask = static_cast<uint8_t>(m_value_mask << byteShift);
        uint8_t oldByte = VolatileLoadWithoutBarrier(byte);
        while (true)
        {
            uint8_t oldValue = static_cast<uint8_t>((oldByte & mask) >> byteShift);
            *previous = oldValue;
#ifdef LXR_F10_NO_SATURATE_CONTROL
            // Control: a count at its maximum wraps to zero instead of staying.
            uint8_t newByte = static_cast<uint8_t>((oldByte & ~mask) | (((oldValue + 1) & m_value_mask) << byteShift));
#else
            if (oldValue == m_value_mask)
            {
                return SideMetadataUpdateStatus::Saturated;
            }
            uint8_t newByte = static_cast<uint8_t>((oldByte & ~mask) | ((oldValue + 1) << byteShift));
#endif // LXR_F10_NO_SATURATE_CONTROL
#ifdef LXR_F10_BYTE_PLAIN_CONTROL
            // Control: the byte is written with a plain store, so an update of a neighbour can be lost.
            VolatileStoreWithoutBarrier(const_cast<uint8_t*>(byte), newByte);
            return SideMetadataUpdateStatus::Updated;
#else
            uint8_t actual = Interlocked::CompareExchange(byte, newByte, oldByte);
            if (actual == oldByte)
            {
                return SideMetadataUpdateStatus::Updated;
            }
            oldByte = actual;
#endif // LXR_F10_BYTE_PLAIN_CONTROL
        }
    }
#endif // !BIGENDIAN
    SideMetadataLocation location = Locate(dataAddress);
#ifdef _DEBUG
    VerifyLocation(dataAddress, location);
    _ASSERTE(m_kind == LxrSideMetadataKind::ReferenceCount);
#endif // _DEBUG
    uintptr_t oldWord = LoadWord(location.word, order);
    while (true)
    {
        uintptr_t oldValue = (oldWord >> location.shift) & m_value_mask;
        *previous = oldValue;
        if (oldValue == m_value_mask)
        {
            return SideMetadataUpdateStatus::Saturated;
        }

        uintptr_t newWord =
            (oldWord & ~location.mask) | (((oldValue + 1) << location.shift) & location.mask);
        uintptr_t actual = CompareExchangeWord(location.word, newWord, oldWord);
        if (actual == oldWord)
        {
            return SideMetadataUpdateStatus::Updated;
        }
        oldWord = actual;
    }
}

inline SideMetadataUpdateStatus SideMetadataAccessor::DecrementNonZeroNonSaturated(
    uintptr_t dataAddress,
    SideMetadataMemoryOrder order,
    uintptr_t* previous) const
{
#if !BIGENDIAN
    if (m_log_bits_per_value <= 3 && order == SideMetadataMemoryOrder::Relaxed)
    {
        uint32_t byteShift;
        volatile uint8_t* byte = LocateByte(dataAddress, &byteShift);
#ifdef _DEBUG
        {
            SideMetadataLocation checked = Locate(dataAddress);
            VerifyLocation(dataAddress, checked);
            _ASSERTE(reinterpret_cast<volatile uint8_t*>(checked.word) + (checked.shift >> 3) == byte);
            _ASSERTE((checked.shift & 7) == byteShift);
        }
#endif // _DEBUG
        const uint8_t mask = static_cast<uint8_t>(m_value_mask << byteShift);
        uint8_t oldByte = VolatileLoadWithoutBarrier(byte);
        while (true)
        {
            uint8_t oldValue = static_cast<uint8_t>((oldByte & mask) >> byteShift);
            *previous = oldValue;
            if (oldValue == 0)
            {
                return SideMetadataUpdateStatus::Zero;
            }
            if (oldValue == m_value_mask)
            {
                return SideMetadataUpdateStatus::Saturated;
            }
            uint8_t newByte = static_cast<uint8_t>((oldByte & ~mask) | ((oldValue - 1) << byteShift));
            uint8_t actual = Interlocked::CompareExchange(byte, newByte, oldByte);
            if (actual == oldByte)
            {
                return SideMetadataUpdateStatus::Updated;
            }
            oldByte = actual;
        }
    }
#endif // !BIGENDIAN
    SideMetadataLocation location = Locate(dataAddress);
#ifdef _DEBUG
    VerifyLocation(dataAddress, location);
    _ASSERTE(m_kind == LxrSideMetadataKind::ReferenceCount);
#endif // _DEBUG
    uintptr_t oldWord = LoadWord(location.word, order);
    while (true)
    {
        uintptr_t oldValue = (oldWord >> location.shift) & m_value_mask;
        *previous = oldValue;
        if (oldValue == 0)
        {
            return SideMetadataUpdateStatus::Zero;
        }
        if (oldValue == m_value_mask)
        {
            return SideMetadataUpdateStatus::Saturated;
        }

        uintptr_t newWord =
            (oldWord & ~location.mask) | (((oldValue - 1) << location.shift) & location.mask);
        uintptr_t actual = CompareExchangeWord(location.word, newWord, oldWord);
        if (actual == oldWord)
        {
            return SideMetadataUpdateStatus::Updated;
        }
        oldWord = actual;
    }
}

inline bool SideMetadataAccessor::IsRangeZero(
    uintptr_t dataStart,
    size_t dataSize,
    SideMetadataMemoryOrder order) const
{
    if (dataSize == 0)
    {
        return true;
    }

    uintptr_t granularity = static_cast<uintptr_t>(1) << m_log_bytes_per_value;
#ifdef _DEBUG
    _ASSERTE(((dataStart | dataSize) & (granularity - 1)) == 0);
#endif // _DEBUG
    SideMetadataLocation first = Locate(dataStart);
    SideMetadataLocation last = Locate(dataStart + dataSize - granularity);
#ifdef _DEBUG
    VerifyLocation(dataStart, first);
    VerifyLocation(dataStart + dataSize - granularity, last);
    _ASSERTE(m_manager->IsDataRangeOwned(dataStart, dataSize));
#endif // _DEBUG
    bool zero = AreWordsZero(first, last, order);
#ifdef _DEBUG
    for (int attempt = 0; attempt < 16; attempt++)
    {
        bool checkedZero = false;
        SideMetadataResult result = m_manager->IsRangeZero(
            m_kind, dataStart, dataSize, SideMetadataMemoryOrder::Relaxed, &checkedZero);
        _ASSERTE(result == SideMetadataResult::Success);
        if ((result != SideMetadataResult::Success) || (checkedZero == zero))
        {
            return zero;
        }
        // Another thread may have changed the range between the two reads; look again.
        zero = AreWordsZero(first, last, SideMetadataMemoryOrder::Relaxed);
    }
    _ASSERTE(!"The inline side-metadata range check and the checked range check disagree.");
#endif // _DEBUG
    return zero;
}

inline bool SideMetadataAccessor::AreWordsZero(
    const SideMetadataLocation& first,
    const SideMetadataLocation& last,
    SideMetadataMemoryOrder order)
{
    uintptr_t firstWord = reinterpret_cast<uintptr_t>(first.word);
    uintptr_t lastWord = reinterpret_cast<uintptr_t>(last.word);
    for (uintptr_t wordAddress = firstWord; wordAddress <= lastWord; wordAddress += sizeof(uintptr_t))
    {
        uintptr_t coverage = UINTPTR_MAX;
        if (wordAddress == firstWord)
        {
            coverage &= SideMetadataManager::ComputeFirstWordCoverageForByteOrder(
                first,
#if BIGENDIAN
                true
#else
                false
#endif
            );
        }
        if (wordAddress == lastWord)
        {
            coverage &= SideMetadataManager::ComputeLastWordCoverageForByteOrder(
                last,
#if BIGENDIAN
                true
#else
                false
#endif
            );
        }

        if ((LoadWord(reinterpret_cast<volatile uintptr_t*>(wordAddress), order) & coverage) != 0)
        {
            return false;
        }
    }

    return true;
}

#endif // SIDE_METADATA_H
