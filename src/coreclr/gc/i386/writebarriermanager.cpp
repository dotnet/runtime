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
#include <string.h>

namespace
{
constexpr size_t WriteBarrierCount = 6;
constexpr size_t PreGrowSize = 34;
constexpr size_t PostGrowSize = 42;
constexpr size_t DestinationSize = 48;

constexpr size_t EphemeralLowerBoundOffset = 4;
constexpr size_t PostGrowEphemeralUpperBoundOffset = 12;
constexpr size_t PreGrowCardTableFirstOffset = 16;
constexpr size_t PreGrowCardTableSecondOffset = 28;
constexpr size_t PostGrowCardTableFirstOffset = 24;
constexpr size_t PostGrowCardTableSecondOffset = 36;

constexpr uint8_t WriteBarrierRegisters[WriteBarrierCount] =
{
    0, // EAX
    1, // ECX
    3, // EBX
    6, // ESI
    7, // EDI
    5, // EBP
};

constexpr WriteBarrierType WriteBarriers[WriteBarrierCount] =
{
    WRITE_BARRIER_X86_EAX,
    WRITE_BARRIER_X86_ECX,
    WRITE_BARRIER_X86_EBX,
    WRITE_BARRIER_X86_ESI,
    WRITE_BARRIER_X86_EDI,
    WRITE_BARRIER_X86_EBP,
};

constexpr WriteBarrierType DebugWriteBarriers[WriteBarrierCount] =
{
    WRITE_BARRIER_X86_DEBUG_EAX,
    WRITE_BARRIER_X86_DEBUG_ECX,
    WRITE_BARRIER_X86_DEBUG_EBX,
    WRITE_BARRIER_X86_DEBUG_ESI,
    WRITE_BARRIER_X86_DEBUG_EDI,
    WRITE_BARRIER_X86_DEBUG_EBP,
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
    void FlushInstructionCache();

private:
    void CopyWriteBarriers(WriteBarrierType templateType, size_t templateSize);
    void CreateWriteBarrier(
        size_t index,
        WriteBarrierType templateType,
        size_t templateSize,
        uint8_t* code);
    void PatchRegister(uint8_t* code, bool isPostGrow, uint8_t reg);
    bool PatchValue(size_t index, size_t offset, uint32_t value);
    void ValidatePatchLocation(const uint8_t* code, size_t offset);
    void ValidateWriteBarriers();

    bool m_isWriteBarrierCopyEnabled;
    bool m_useSlowDebugWriteBarrier;
    bool m_isPostGrow;
};

WriteBarrierManager g_writeBarrierManager;
}

WriteBarrierManager::WriteBarrierManager() :
    m_isWriteBarrierCopyEnabled(false),
    m_useSlowDebugWriteBarrier(false),
    m_isPostGrow(false)
{
}

void WriteBarrierManager::PatchRegister(uint8_t* code, bool isPostGrow, uint8_t reg)
{
    assert(code[0] == 0x89);
    code[1] = (code[1] & 0xc7) | (reg << 3);

    assert(code[2] == 0x81);
    code[3] = (code[3] & 0xf8) | reg;

    if (isPostGrow)
    {
        assert(code[10] == 0x81);
        code[11] = (code[11] & 0xf8) | reg;
    }
}

void WriteBarrierManager::CreateWriteBarrier(
    size_t index,
    WriteBarrierType templateType,
    size_t templateSize,
    uint8_t* code)
{
    WriteBarrierCodeDescriptor source = g_writeBarrierServices.GetWriteBarrierCode(templateType);
    WriteBarrierCodeDescriptor destination = g_writeBarrierServices.GetWriteBarrierCode(WriteBarriers[index]);

    assert(source.size == templateSize);
    assert(destination.size == DestinationSize);

    memcpy(code, source.source_start, templateSize);
    assert(code[templateSize - 1] == 0xc3);
    PatchRegister(code, templateType == WRITE_BARRIER_X86_POSTGROW, WriteBarrierRegisters[index]);

    if (m_useSlowDebugWriteBarrier)
    {
        WriteBarrierCodeDescriptor debug =
            g_writeBarrierServices.GetWriteBarrierCode(DebugWriteBarriers[index]);
        intptr_t displacement =
            debug.source_start - (destination.executable_start + sizeof(uint8_t) + sizeof(int32_t));
        assert(displacement >= INT32_MIN && displacement <= INT32_MAX);

        code[0] = 0xe9;
        int32_t relativeTarget = static_cast<int32_t>(displacement);
        memcpy(code + 1, &relativeTarget, sizeof(relativeTarget));
    }
}

void WriteBarrierManager::CopyWriteBarriers(
    WriteBarrierType templateType,
    size_t templateSize)
{
    for (size_t index = 0; index < WriteBarrierCount; index++)
    {
        uint8_t code[PostGrowSize];
        CreateWriteBarrier(index, templateType, templateSize, code);

        WriteBarrierCodeDescriptor destination =
            g_writeBarrierServices.GetWriteBarrierCode(WriteBarriers[index]);
        GCToEEInterface::CopyWriteBarrierCode(
            destination.executable_start,
            code,
            templateSize);
    }
}

void WriteBarrierManager::ValidatePatchLocation(const uint8_t* code, size_t offset)
{
    const uint32_t* location = reinterpret_cast<const uint32_t*>(code + offset);
    assert(
        (reinterpret_cast<uintptr_t>(location) & (sizeof(uint32_t) - 1)) == 0);
    assert(*location == 0xf0f0f0f0);
}

void WriteBarrierManager::ValidateWriteBarriers()
{
#ifndef CODECOVERAGE
    if (m_useSlowDebugWriteBarrier)
    {
        return;
    }

    WriteBarrierCodeDescriptor destination =
        g_writeBarrierServices.GetWriteBarrierCode(WRITE_BARRIER_X86_EAX);
    ValidatePatchLocation(destination.executable_start, EphemeralLowerBoundOffset);
    ValidatePatchLocation(destination.executable_start, PreGrowCardTableFirstOffset);
    ValidatePatchLocation(destination.executable_start, PreGrowCardTableSecondOffset);

    WriteBarrierCodeDescriptor postGrow =
        g_writeBarrierServices.GetWriteBarrierCode(WRITE_BARRIER_X86_POSTGROW);
    ValidatePatchLocation(postGrow.source_start, EphemeralLowerBoundOffset);
    ValidatePatchLocation(postGrow.source_start, PostGrowEphemeralUpperBoundOffset);
    ValidatePatchLocation(postGrow.source_start, PostGrowCardTableFirstOffset);
    ValidatePatchLocation(postGrow.source_start, PostGrowCardTableSecondOffset);
#endif // !CODECOVERAGE
}

void WriteBarrierManager::Initialize()
{
    g_writeBarrierServices.ValidateWriteBarrierLayout();

    m_isWriteBarrierCopyEnabled = GCToEEInterface::IsWriteBarrierCodeCopyEnabled();
    if (!m_isWriteBarrierCopyEnabled)
    {
        return;
    }

#ifdef WRITE_BARRIER_CHECK
    m_useSlowDebugWriteBarrier = GCToEEInterface::UseSlowDebugWriteBarrier();
#endif // WRITE_BARRIER_CHECK
    CopyWriteBarriers(WRITE_BARRIER_X86_PREGROW, PreGrowSize);
    ValidateWriteBarriers();
}

bool WriteBarrierManager::PatchValue(size_t index, size_t offset, uint32_t value)
{
    WriteBarrierCodeDescriptor destination =
        g_writeBarrierServices.GetWriteBarrierCode(WriteBarriers[index]);
    uint8_t* patchLocation = destination.executable_start + offset;
    uint32_t* currentValue = reinterpret_cast<uint32_t*>(patchLocation);

    assert(
        (reinterpret_cast<uintptr_t>(currentValue) & (sizeof(uint32_t) - 1)) == 0);
    if (*currentValue == value)
    {
        return false;
    }

    GCToEEInterface::UpdateWriteBarrierValue(patchLocation, value, sizeof(value));
    return true;
}

int WriteBarrierManager::StompEphemeral(const WriteBarrierParameters& state, bool)
{
    if (!m_isWriteBarrierCopyEnabled || m_useSlowDebugWriteBarrier)
    {
        return SWB_PASS;
    }

    bool changed = false;
    for (size_t index = 0; index < WriteBarrierCount; index++)
    {
        WriteBarrierCodeDescriptor destination =
            g_writeBarrierServices.GetWriteBarrierCode(WriteBarriers[index]);
        assert(destination.executable_start[2] == 0x81);
        changed |= PatchValue(
            index,
            EphemeralLowerBoundOffset,
            static_cast<uint32_t>(reinterpret_cast<uintptr_t>(state.ephemeral_low)));

        if (m_isPostGrow)
        {
            assert(destination.executable_start[10] == 0x81);
            changed |= PatchValue(
                index,
                PostGrowEphemeralUpperBoundOffset,
                static_cast<uint32_t>(reinterpret_cast<uintptr_t>(state.ephemeral_high)));
        }
    }

    return changed ? SWB_ICACHE_FLUSH : SWB_PASS;
}

int WriteBarrierManager::StompResize(
    const WriteBarrierParameters& state,
    bool isRuntimeSuspended,
    bool requiresUpperBoundsCheck)
{
    if (!m_isWriteBarrierCopyEnabled || m_useSlowDebugWriteBarrier)
    {
        return SWB_PASS;
    }

    WriteBarrierRuntimeModeHolder modeHolder(
        !isRuntimeSuspended && !m_isPostGrow && requiresUpperBoundsCheck);
    int completionActions = SWB_PASS;
    if (!m_isPostGrow && requiresUpperBoundsCheck)
    {
        if (!isRuntimeSuspended)
        {
            GCToEEInterface::SuspendForWriteBarrier();
            completionActions |= SWB_EE_RESTART;
        }

        CopyWriteBarriers(WRITE_BARRIER_X86_POSTGROW, PostGrowSize);
        m_isPostGrow = true;
        completionActions |= SWB_ICACHE_FLUSH;
    }

    size_t firstOffset =
        m_isPostGrow ? PostGrowCardTableFirstOffset : PreGrowCardTableFirstOffset;
    size_t secondOffset =
        m_isPostGrow ? PostGrowCardTableSecondOffset : PreGrowCardTableSecondOffset;
    uint32_t cardTable =
        static_cast<uint32_t>(reinterpret_cast<uintptr_t>(state.card_table));

    for (size_t index = 0; index < WriteBarrierCount; index++)
    {
        WriteBarrierCodeDescriptor destination =
            g_writeBarrierServices.GetWriteBarrierCode(WriteBarriers[index]);
        assert(
            destination.executable_start[firstOffset - 2] == 0x80);
        assert(
            destination.executable_start[secondOffset - 2] == 0xc6);

        bool changed = PatchValue(index, firstOffset, cardTable);
        changed |= PatchValue(index, secondOffset, cardTable);
        if (changed)
        {
            completionActions |= SWB_ICACHE_FLUSH;
        }
    }

    if (m_isPostGrow)
    {
        completionActions |= StompEphemeral(state, isRuntimeSuspended);
    }

    return completionActions;
}

void WriteBarrierManager::FlushInstructionCache()
{
    if (!m_isWriteBarrierCopyEnabled)
    {
        return;
    }

    for (WriteBarrierType writeBarrier : WriteBarriers)
    {
        WriteBarrierCodeDescriptor destination =
            g_writeBarrierServices.GetWriteBarrierCode(writeBarrier);
        GCToEEInterface::FlushWriteBarrierInstructionCache(
            destination.executable_start,
            destination.size);
    }
}

#include "writebarriermanager.inl"
