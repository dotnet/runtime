// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ===========================================================================
// File: copywritebarriermanager.cpp
// ===========================================================================

// This manages which write barrier implementation is currently in use and
// patches its related constants.

#include "env/common.h"
#include "env/gcenv.h"
#include "env/gcenv.ee.h"
#include "writebarrierservices.h"

#ifdef GC_WRITE_BARRIER_STANDALONE
#include "gcenv.ee.standalone.inl"
#endif // GC_WRITE_BARRIER_STANDALONE

#include <assert.h>

#define WRITE_BARRIER_ASSERT(condition) assert(condition)

class WriteBarrierManager
{
public:
    WriteBarrierManager();
    void Initialize();

    int StompEphemeral(const WriteBarrierParameters& state, bool isRuntimeSuspended);
    int StompResize(
        const WriteBarrierParameters& state,
        bool isRuntimeSuspended,
        bool bReqUpperBoundsCheck);

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    int SwitchToWriteWatchBarrier(const WriteBarrierParameters& state, bool isRuntimeSuspended);
    int SwitchToNonWriteWatchBarrier(const WriteBarrierParameters& state, bool isRuntimeSuspended);
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    size_t GetCurrentWriteBarrierSize();
    void FlushInstructionCache();

private:
    template <typename T>
    int UpdateVariable(uint8_t* location, T value);

    uint8_t* GetCurrentWriteBarrierCode();
    int ChangeWriteBarrierTo(
        const WriteBarrierParameters& state,
        WriteBarrierType newWriteBarrier,
        bool isRuntimeSuspended);
    bool NeedDifferentWriteBarrier(
        bool bReqUpperBoundsCheck,
        const WriteBarrierParameters& state,
        WriteBarrierType* pNewWriteBarrierType);

    void UpdatePatchLocations(WriteBarrierType newWriteBarrier);

    WriteBarrierType m_currentWriteBarrier;
    bool m_isWriteBarrierCopyEnabled;
    bool m_isServerGC;
    bool m_useSlowDebugBarrier;

    uint8_t* m_pWriteWatchTableImmediate;    // PREGROW | POSTGROW | SVR | WRITE_WATCH | REGION
    uint8_t* m_pLowerBoundImmediate;         // PREGROW | POSTGROW |     | WRITE_WATCH | REGION
    uint8_t* m_pCardTableImmediate;          // PREGROW | POSTGROW | SVR | WRITE_WATCH | REGION
    uint8_t* m_pCardBundleTableImmediate;    // PREGROW | POSTGROW | SVR | WRITE_WATCH | REGION
    uint8_t* m_pUpperBoundImmediate;         //         | POSTGROW |     | WRITE_WATCH | REGION
    uint8_t* m_pRegionToGenTableImmediate;   //         |          |     | WRITE_WATCH | REGION
    uint8_t* m_pRegionShrDest;               //         |          |     | WRITE_WATCH | REGION
    uint8_t* m_pRegionShrSrc;                //         |          |     | WRITE_WATCH | REGION

};

namespace
{
WriteBarrierManager g_writeBarrierManager;

[[noreturn]] void WriteBarrierUnreachable()
{
    assert(false);
#ifdef _MSC_VER
    __assume(0);
#else
    __builtin_unreachable();
#endif
}
}

WriteBarrierManager::WriteBarrierManager() :
    m_currentWriteBarrier(WRITE_BARRIER_UNINITIALIZED),
    m_isWriteBarrierCopyEnabled(false),
    m_isServerGC(false),
    m_useSlowDebugBarrier(false)
{
}

uint8_t* WriteBarrierManager::GetCurrentWriteBarrierCode()
{
    return g_writeBarrierServices.GetWriteBarrierCode(m_currentWriteBarrier).source_start;
}

size_t WriteBarrierManager::GetCurrentWriteBarrierSize()
{
    return g_writeBarrierServices.GetWriteBarrierCode(m_currentWriteBarrier).size;
}


int WriteBarrierManager::ChangeWriteBarrierTo(
    const WriteBarrierParameters& state,
    WriteBarrierType newWriteBarrier,
    bool isRuntimeSuspended)
{
    WriteBarrierRuntimeModeHolder modeHolder(!isRuntimeSuspended);
    int stompWBCompleteActions = SWB_PASS;
    if (!isRuntimeSuspended && m_currentWriteBarrier != WRITE_BARRIER_UNINITIALIZED)
    {
        GCToEEInterface::SuspendForWriteBarrier();
        stompWBCompleteActions |= SWB_EE_RESTART;
    }

    WRITE_BARRIER_ASSERT(m_currentWriteBarrier != newWriteBarrier);
    m_currentWriteBarrier = newWriteBarrier;

    // Patch locations are validated against the copied write barrier code.
    {
        WriteBarrierCodeDescriptor writeBarrierBuffer = g_writeBarrierServices.GetWriteBarrierCode(WRITE_BARRIER_BUFFER);
        GCToEEInterface::CopyWriteBarrierCode(
            writeBarrierBuffer.executable_start,
            GetCurrentWriteBarrierCode(),
            GetCurrentWriteBarrierSize());
        stompWBCompleteActions |= SWB_ICACHE_FLUSH;
    }

    UpdatePatchLocations(newWriteBarrier);

    stompWBCompleteActions |= StompEphemeral(state, true);
    stompWBCompleteActions |= StompResize(state, true, false);

    return stompWBCompleteActions;
}

void WriteBarrierManager::Initialize()
{
    m_isWriteBarrierCopyEnabled = GCToEEInterface::IsWriteBarrierCodeCopyEnabled();
    m_isServerGC = GCToEEInterface::IsServerGC();
    m_useSlowDebugBarrier = GCToEEInterface::UseSlowDebugWriteBarrier();

    g_writeBarrierServices.ValidateWriteBarrierLayout();
}

template <typename T>
int WriteBarrierManager::UpdateVariable(uint8_t* location, T value)
{
    if (*(T*)location != value)
    {
        GCToEEInterface::UpdateWriteBarrierValue(location, static_cast<uint64_t>(value), sizeof(T));
        return SWB_ICACHE_FLUSH;
    }
    return SWB_PASS;
}

bool WriteBarrierManager::NeedDifferentWriteBarrier(
    bool bReqUpperBoundsCheck,
    const WriteBarrierParameters& state,
    WriteBarrierType* pNewWriteBarrierType)
{
    WriteBarrierType writeBarrierType = m_currentWriteBarrier;

    for(;;)
    {
        switch (writeBarrierType)
        {
        case WRITE_BARRIER_UNINITIALIZED:
            // The default slow write barrier has some good asserts
            if (m_useSlowDebugBarrier)
            {
                break;
            }
            if (state.region_shr != 0)
            {
                writeBarrierType = state.region_use_bitwise_write_barrier
                    ? WRITE_BARRIER_BIT_REGIONS64
                    : WRITE_BARRIER_BYTE_REGIONS64;
            }
            else
            {
#ifdef FEATURE_SVR_GC
                writeBarrierType = m_isServerGC ? WRITE_BARRIER_SVR64 : WRITE_BARRIER_PREGROW64;
#else
                writeBarrierType = WRITE_BARRIER_PREGROW64;
#endif // FEATURE_SVR_GC
            }
            continue;

        case WRITE_BARRIER_PREGROW64:
            if (bReqUpperBoundsCheck)
            {
                writeBarrierType = WRITE_BARRIER_POSTGROW64;
            }
            break;

        case WRITE_BARRIER_POSTGROW64:
            break;

#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_SVR64:
            break;
#endif // FEATURE_SVR_GC

        case WRITE_BARRIER_BYTE_REGIONS64:
        case WRITE_BARRIER_BIT_REGIONS64:
            break;

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        case WRITE_BARRIER_WRITE_WATCH_PREGROW64:
            if (bReqUpperBoundsCheck)
            {
                writeBarrierType = WRITE_BARRIER_WRITE_WATCH_POSTGROW64;
            }
            break;

        case WRITE_BARRIER_WRITE_WATCH_POSTGROW64:
            break;

#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_WRITE_WATCH_SVR64:
            break;
#endif // FEATURE_SVR_GC
        case WRITE_BARRIER_WRITE_WATCH_BYTE_REGIONS64:
        case WRITE_BARRIER_WRITE_WATCH_BIT_REGIONS64:
            break;
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

        default:
            WriteBarrierUnreachable();
        }
        break;
    }

    *pNewWriteBarrierType = writeBarrierType;
    return m_currentWriteBarrier != writeBarrierType;
}

int WriteBarrierManager::StompEphemeral(const WriteBarrierParameters& state, bool isRuntimeSuspended)
{
    if (!m_isWriteBarrierCopyEnabled)
    {
        return SWB_PASS;
    }

    WriteBarrierType newType;
    if (NeedDifferentWriteBarrier(false, state, &newType))
    {
        return ChangeWriteBarrierTo(state, newType, isRuntimeSuspended);
    }

    int stompWBCompleteActions = SWB_PASS;

#ifdef _DEBUG
    // Using debug-only write barrier?
    if (m_currentWriteBarrier == WRITE_BARRIER_UNINITIALIZED)
        return stompWBCompleteActions;
#endif

    if (m_pUpperBoundImmediate != nullptr)
    {
        stompWBCompleteActions |=
            UpdateVariable<uint64_t>(m_pUpperBoundImmediate, reinterpret_cast<uintptr_t>(state.ephemeral_high));
    }

    if (m_pLowerBoundImmediate != nullptr)
    {
        stompWBCompleteActions |=
            UpdateVariable<uint64_t>(m_pLowerBoundImmediate, reinterpret_cast<uintptr_t>(state.ephemeral_low));
    }

    if (g_writeBarrierServices.UpdateWriteBarrierImplementationState(state))
    {
        stompWBCompleteActions |= SWB_ICACHE_FLUSH;
    }

    return stompWBCompleteActions;
}

int WriteBarrierManager::StompResize(
    const WriteBarrierParameters& state,
    bool isRuntimeSuspended,
    bool bReqUpperBoundsCheck)
{
    if (!m_isWriteBarrierCopyEnabled)
    {
        return SWB_PASS;
    }

    // If we are told that we require an upper bounds check (GC did some heap reshuffling),
    // we need to switch to the WriteBarrier_PostGrow function for good.

    WriteBarrierType newType;
    if (NeedDifferentWriteBarrier(bReqUpperBoundsCheck, state, &newType))
    {
        return ChangeWriteBarrierTo(state, newType, isRuntimeSuspended);
    }

    int stompWBCompleteActions = SWB_PASS;

#ifdef _DEBUG
    // Using debug-only write barrier?
    if (m_currentWriteBarrier == WRITE_BARRIER_UNINITIALIZED)
        return stompWBCompleteActions;
#endif

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    if (m_pWriteWatchTableImmediate != nullptr)
    {
        stompWBCompleteActions |= UpdateVariable<uint64_t>(
            m_pWriteWatchTableImmediate,
            reinterpret_cast<uintptr_t>(state.write_watch_table));
    }
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

    if (m_pRegionToGenTableImmediate != nullptr)
    {
        stompWBCompleteActions |= UpdateVariable<uint64_t>(
            m_pRegionToGenTableImmediate,
            reinterpret_cast<uintptr_t>(state.region_to_generation_table));
    }

    if (m_pRegionShrDest != nullptr)
    {
        stompWBCompleteActions |= UpdateVariable<uint8_t>(m_pRegionShrDest, state.region_shr);
    }

    if (m_pRegionShrSrc != nullptr)
    {
        stompWBCompleteActions |= UpdateVariable<uint8_t>(m_pRegionShrSrc, state.region_shr);
    }

    WRITE_BARRIER_ASSERT(m_pCardTableImmediate != nullptr);
    stompWBCompleteActions |=
        UpdateVariable<uint64_t>(m_pCardTableImmediate, reinterpret_cast<uintptr_t>(state.card_table));
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    WRITE_BARRIER_ASSERT(m_pCardBundleTableImmediate != nullptr);
    stompWBCompleteActions |=
        UpdateVariable<uint64_t>(m_pCardBundleTableImmediate, reinterpret_cast<uintptr_t>(state.card_bundle_table));
#endif

    if (g_writeBarrierServices.UpdateWriteBarrierImplementationState(state))
    {
        stompWBCompleteActions |= SWB_ICACHE_FLUSH;
    }

    return stompWBCompleteActions;
}

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
int WriteBarrierManager::SwitchToWriteWatchBarrier(const WriteBarrierParameters& state, bool isRuntimeSuspended)
{
    if (!m_isWriteBarrierCopyEnabled)
    {
        return SWB_PASS;
    }

    WriteBarrierType newWriteBarrierType;
    switch (m_currentWriteBarrier)
    {
        case WRITE_BARRIER_UNINITIALIZED:
            // Using the debug-only write barrier
            return SWB_PASS;

        case WRITE_BARRIER_PREGROW64:
            newWriteBarrierType = WRITE_BARRIER_WRITE_WATCH_PREGROW64;
            break;

        case WRITE_BARRIER_POSTGROW64:
            newWriteBarrierType = WRITE_BARRIER_WRITE_WATCH_POSTGROW64;
            break;

#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_SVR64:
            newWriteBarrierType = WRITE_BARRIER_WRITE_WATCH_SVR64;
            break;
#endif // FEATURE_SVR_GC

        case WRITE_BARRIER_BYTE_REGIONS64:
            newWriteBarrierType = WRITE_BARRIER_WRITE_WATCH_BYTE_REGIONS64;
            break;

        case WRITE_BARRIER_BIT_REGIONS64:
            newWriteBarrierType = WRITE_BARRIER_WRITE_WATCH_BIT_REGIONS64;
            break;

        default:
            WriteBarrierUnreachable();
    }

    return ChangeWriteBarrierTo(state, newWriteBarrierType, isRuntimeSuspended);
}

int WriteBarrierManager::SwitchToNonWriteWatchBarrier(const WriteBarrierParameters& state, bool isRuntimeSuspended)
{
    if (!m_isWriteBarrierCopyEnabled)
    {
        return SWB_PASS;
    }

    WriteBarrierType newWriteBarrierType;
    switch (m_currentWriteBarrier)
    {
        case WRITE_BARRIER_UNINITIALIZED:
            // Using the debug-only write barrier
            return SWB_PASS;

        case WRITE_BARRIER_WRITE_WATCH_PREGROW64:
            newWriteBarrierType = WRITE_BARRIER_PREGROW64;
            break;

        case WRITE_BARRIER_WRITE_WATCH_POSTGROW64:
            newWriteBarrierType = WRITE_BARRIER_POSTGROW64;
            break;

#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_WRITE_WATCH_SVR64:
            newWriteBarrierType = WRITE_BARRIER_SVR64;
            break;
#endif // FEATURE_SVR_GC

        case WRITE_BARRIER_WRITE_WATCH_BYTE_REGIONS64:
            newWriteBarrierType = WRITE_BARRIER_BYTE_REGIONS64;
            break;

        case WRITE_BARRIER_WRITE_WATCH_BIT_REGIONS64:
            newWriteBarrierType = WRITE_BARRIER_BIT_REGIONS64;
            break;

        default:
            WriteBarrierUnreachable();
    }

    return ChangeWriteBarrierTo(state, newWriteBarrierType, isRuntimeSuspended);
}
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP


void WriteBarrierManager::UpdatePatchLocations(WriteBarrierType newWriteBarrier)
{
    WriteBarrierPatchLocations locations = g_writeBarrierServices.GetWriteBarrierPatchLocations(newWriteBarrier);
    m_pWriteWatchTableImmediate = locations.write_watch_table;
    m_pRegionToGenTableImmediate = locations.region_to_generation;
    m_pRegionShrDest = locations.region_shr_dest;
    m_pRegionShrSrc = locations.region_shr_src;
    m_pLowerBoundImmediate = locations.lower_bound;
    m_pUpperBoundImmediate = locations.upper_bound;
    m_pCardTableImmediate = locations.card_table;
    m_pCardBundleTableImmediate = locations.card_bundle_table;
}

#undef WRITE_BARRIER_ASSERT

void WriteBarrierManager::FlushInstructionCache()
{
    if (!m_isWriteBarrierCopyEnabled)
    {
        return;
    }

    WriteBarrierCodeDescriptor writeBarrierBuffer = g_writeBarrierServices.GetWriteBarrierCode(WRITE_BARRIER_BUFFER);
    GCToEEInterface::FlushWriteBarrierInstructionCache(
        writeBarrierBuffer.executable_start,
        GetCurrentWriteBarrierSize());
}

#include "writebarriermanager.inl"
