// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "env/common.h"
#include "env/gcenv.h"
#include "env/gcenv.ee.h"
#include "writebarrierservices.h"

#ifdef GC_WRITE_BARRIER_STANDALONE
#include "gcenv.ee.standalone.inl"
#endif // GC_WRITE_BARRIER_STANDALONE

#include <assert.h>

struct WriteBarrierDescriptor
{
    uint32_t function_start_offset;
    uint32_t function_end_offset;
    uint32_t lowest_address_offset;
    uint32_t highest_address_offset;
    uint32_t ephemeral_low_offset;
    uint32_t ephemeral_high_offset;
    uint32_t card_table_offset;
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    uint32_t write_watch_table_offset;
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
};

extern "C" WriteBarrierDescriptor g_rgWriteBarrierDescriptors;
extern "C" uint32_t g_num_processors;

namespace
{
constexpr uintptr_t ThumbCode = 1;
constexpr uint32_t UnusedPatchOffset = UINT16_MAX;
constexpr size_t PointerPatchSize = 2 * sizeof(uint32_t);
constexpr size_t WriteBarrierCount = 4;
constexpr size_t CheckedWriteBarrierOffset = WriteBarrierCount;

enum WriteBarrierIndex : size_t
{
    WRITE_BARRIER_SP_PRE,
    WRITE_BARRIER_SP_POST,
    WRITE_BARRIER_MP_PRE,
    WRITE_BARRIER_MP_POST,
};

class WriteBarrierManager
{
public:
    WriteBarrierManager();

    void Initialize();
    int StompEphemeral(const WriteBarrierParameters& state, bool isRuntimeSuspended);
    int StompResize(
        const WriteBarrierParameters& state,
        bool isRuntimeSuspended,
        bool requiresUpperBoundsCheck);
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    int SwitchToWriteWatchBarrier(const WriteBarrierParameters& state, bool isRuntimeSuspended);
    int SwitchToNonWriteWatchBarrier(const WriteBarrierParameters& state, bool isRuntimeSuspended);
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    void FlushInstructionCache();

private:
    WriteBarrierDescriptor* GetDescriptor(size_t index);
    uint8_t* GetSourceStart(WriteBarrierDescriptor* descriptor);
    size_t GetSourceSize(WriteBarrierDescriptor* descriptor);
    void ValidateDescriptor(WriteBarrierDescriptor* descriptor, size_t destinationSize);
    void ValidatePatchOffset(WriteBarrierDescriptor* descriptor, uint32_t offset);
    void CopyWriteBarriers();
    void PatchWriteBarrier(
        uint8_t* destination,
        WriteBarrierDescriptor* descriptor,
        const WriteBarrierParameters& state);
    void PatchPointer(uint8_t* destination, uint32_t offset, uint8_t* value);
    void UpdateWriteBarriers(const WriteBarrierParameters& state, bool requiresUpperBoundsCheck);

    WriteBarrierDescriptor* m_writeBarrierDescriptor;
    WriteBarrierDescriptor* m_checkedWriteBarrierDescriptor;
    bool m_isWriteBarrierCopyEnabled;
    bool m_isPostGrow;
    bool m_copyRequired;
};

WriteBarrierManager g_writeBarrierManager;
}

WriteBarrierManager::WriteBarrierManager() :
    m_writeBarrierDescriptor(nullptr),
    m_checkedWriteBarrierDescriptor(nullptr),
    m_isWriteBarrierCopyEnabled(false),
    m_isPostGrow(false),
    m_copyRequired(true)
{
}

WriteBarrierDescriptor* WriteBarrierManager::GetDescriptor(size_t index)
{
    return &g_rgWriteBarrierDescriptors + index;
}

uint8_t* WriteBarrierManager::GetSourceStart(WriteBarrierDescriptor* descriptor)
{
    uintptr_t address =
        reinterpret_cast<uintptr_t>(descriptor) + descriptor->function_start_offset;
    return reinterpret_cast<uint8_t*>(address & ~ThumbCode);
}

size_t WriteBarrierManager::GetSourceSize(WriteBarrierDescriptor* descriptor)
{
    uintptr_t descriptorAddress = reinterpret_cast<uintptr_t>(descriptor);
    uintptr_t startAddress =
        (descriptorAddress + descriptor->function_start_offset) & ~ThumbCode;
    uintptr_t endAddress =
        (descriptorAddress + descriptor->function_end_offset) & ~ThumbCode;
    assert(endAddress >= startAddress);
    return endAddress - startAddress;
}

void WriteBarrierManager::ValidatePatchOffset(
    WriteBarrierDescriptor* descriptor,
    uint32_t offset)
{
    size_t sourceSize = GetSourceSize(descriptor);
    assert(
        offset == UnusedPatchOffset ||
        (offset <= sourceSize && PointerPatchSize <= sourceSize - offset));
}

void WriteBarrierManager::ValidateDescriptor(
    WriteBarrierDescriptor* descriptor,
    size_t destinationSize)
{
    assert(destinationSize >= GetSourceSize(descriptor));
    ValidatePatchOffset(descriptor, descriptor->lowest_address_offset);
    ValidatePatchOffset(descriptor, descriptor->highest_address_offset);
    ValidatePatchOffset(descriptor, descriptor->ephemeral_low_offset);
    ValidatePatchOffset(descriptor, descriptor->ephemeral_high_offset);
    ValidatePatchOffset(descriptor, descriptor->card_table_offset);
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    ValidatePatchOffset(descriptor, descriptor->write_watch_table_offset);
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
}

void WriteBarrierManager::Initialize()
{
    m_isWriteBarrierCopyEnabled = GCToEEInterface::IsWriteBarrierCodeCopyEnabled();

    WriteBarrierCodeDescriptor writeBarrier = g_writeBarrierServices.GetWriteBarrierCode(WRITE_BARRIER_BUFFER);
    WriteBarrierCodeDescriptor checkedWriteBarrier =
        g_writeBarrierServices.GetWriteBarrierCode(WRITE_BARRIER_CHECKED_BUFFER);

    for (size_t index = 0; index < WriteBarrierCount; index++)
    {
        ValidateDescriptor(GetDescriptor(index), writeBarrier.size);
        ValidateDescriptor(
            GetDescriptor(index + CheckedWriteBarrierOffset),
            checkedWriteBarrier.size);
    }

    assert(
        GetDescriptor(WriteBarrierCount + CheckedWriteBarrierOffset)->function_start_offset == 0);
}

void WriteBarrierManager::CopyWriteBarriers()
{
    size_t index;
    if (g_num_processors > 1)
    {
        index = m_isPostGrow ? WRITE_BARRIER_MP_POST : WRITE_BARRIER_MP_PRE;
    }
    else
    {
        index = m_isPostGrow ? WRITE_BARRIER_SP_POST : WRITE_BARRIER_SP_PRE;
    }

    m_writeBarrierDescriptor = GetDescriptor(index);
    m_checkedWriteBarrierDescriptor = GetDescriptor(index + CheckedWriteBarrierOffset);

    WriteBarrierCodeDescriptor writeBarrier = g_writeBarrierServices.GetWriteBarrierCode(WRITE_BARRIER_BUFFER);
    GCToEEInterface::CopyWriteBarrierCode(
        writeBarrier.executable_start,
        GetSourceStart(m_writeBarrierDescriptor),
        GetSourceSize(m_writeBarrierDescriptor));

    WriteBarrierCodeDescriptor checkedWriteBarrier =
        g_writeBarrierServices.GetWriteBarrierCode(WRITE_BARRIER_CHECKED_BUFFER);
    GCToEEInterface::CopyWriteBarrierCode(
        checkedWriteBarrier.executable_start,
        GetSourceStart(m_checkedWriteBarrierDescriptor),
        GetSourceSize(m_checkedWriteBarrierDescriptor));

    m_copyRequired = false;
}

void WriteBarrierManager::PatchPointer(uint8_t* destination, uint32_t offset, uint8_t* value)
{
    if (offset != UnusedPatchOffset)
    {
        GCToEEInterface::PatchWriteBarrierPointer(destination + offset, value);
    }
}

void WriteBarrierManager::PatchWriteBarrier(
    uint8_t* destination,
    WriteBarrierDescriptor* descriptor,
    const WriteBarrierParameters& state)
{
    PatchPointer(destination, descriptor->lowest_address_offset, state.lowest_address);
    PatchPointer(destination, descriptor->highest_address_offset, state.highest_address);
    PatchPointer(destination, descriptor->ephemeral_low_offset, state.ephemeral_low);
    PatchPointer(destination, descriptor->ephemeral_high_offset, state.ephemeral_high);
    PatchPointer(
        destination,
        descriptor->card_table_offset,
        reinterpret_cast<uint8_t*>(state.card_table));
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    PatchPointer(destination, descriptor->write_watch_table_offset, state.write_watch_table);
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
}

void WriteBarrierManager::UpdateWriteBarriers(
    const WriteBarrierParameters& state,
    bool requiresUpperBoundsCheck)
{
    if (requiresUpperBoundsCheck && !m_isPostGrow)
    {
        m_isPostGrow = true;
        m_copyRequired = true;
    }

    if (m_copyRequired)
    {
        CopyWriteBarriers();
    }

    PatchWriteBarrier(
        g_writeBarrierServices.GetWriteBarrierCode(WRITE_BARRIER_BUFFER).executable_start,
        m_writeBarrierDescriptor,
        state);
    PatchWriteBarrier(
        g_writeBarrierServices.GetWriteBarrierCode(WRITE_BARRIER_CHECKED_BUFFER).executable_start,
        m_checkedWriteBarrierDescriptor,
        state);
}

int WriteBarrierManager::StompEphemeral(
    const WriteBarrierParameters& state,
    bool isRuntimeSuspended)
{
    if (!m_isWriteBarrierCopyEnabled)
    {
        return SWB_PASS;
    }

    assert(isRuntimeSuspended);
    UpdateWriteBarriers(state, false);
    return SWB_ICACHE_FLUSH;
}

int WriteBarrierManager::StompResize(
    const WriteBarrierParameters& state,
    bool isRuntimeSuspended,
    bool requiresUpperBoundsCheck)
{
    if (!m_isWriteBarrierCopyEnabled)
    {
        return SWB_PASS;
    }

    WriteBarrierRuntimeModeHolder modeHolder(true);
    int completionActions = SWB_ICACHE_FLUSH;
    if (!isRuntimeSuspended)
    {
        GCToEEInterface::SuspendForWriteBarrier();
        completionActions |= SWB_EE_RESTART;
    }

    UpdateWriteBarriers(state, requiresUpperBoundsCheck);
    return completionActions;
}

void WriteBarrierManager::FlushInstructionCache()
{
    if (!m_isWriteBarrierCopyEnabled)
    {
        return;
    }

    WriteBarrierCodeDescriptor writeBarrier = g_writeBarrierServices.GetWriteBarrierCode(WRITE_BARRIER_BUFFER);
    GCToEEInterface::FlushWriteBarrierInstructionCache(writeBarrier.executable_start, writeBarrier.size);

    WriteBarrierCodeDescriptor checkedWriteBarrier =
        g_writeBarrierServices.GetWriteBarrierCode(WRITE_BARRIER_CHECKED_BUFFER);
    GCToEEInterface::FlushWriteBarrierInstructionCache(
        checkedWriteBarrier.executable_start,
        checkedWriteBarrier.size);
}

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
int WriteBarrierManager::SwitchToWriteWatchBarrier(
    const WriteBarrierParameters& state,
    bool isRuntimeSuspended)
{
    return StompEphemeral(state, isRuntimeSuspended);
}

int WriteBarrierManager::SwitchToNonWriteWatchBarrier(
    const WriteBarrierParameters& state,
    bool isRuntimeSuspended)
{
    return StompEphemeral(state, isRuntimeSuspended);
}
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

#include "writebarriermanager.inl"
