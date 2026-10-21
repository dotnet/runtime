// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef LXR_COLLECTION_POLICY_H
#define LXR_COLLECTION_POLICY_H

#include "lxr_pause_schedule.h"
#include <atomic>
#include <cmath>
#include <cstddef>
#include <cstdint>

struct LxrPolicyConfiguration
{
    size_t traceThreshold = 20;
    size_t rcStopPercent = 15;
    size_t maxSurvivalMB = 128;
    size_t incrementsLimit = SIZE_MAX;
    size_t nurseryBlocks = SIZE_MAX;
    size_t youngLimitBytes = SIZE_MAX;
    bool weighted = false;
    bool harmonic = false;
};

enum class LxrCollectionCause : uint8_t
{
    None, HeapFull, FinalMark, Survival, ImmixSpaceFull, Increments, FixedNursery, YoungAllocation,
};

struct LxrPolicyPressure
{
    bool heapFull;
    bool marking;
    bool traceDrained;
    size_t youngBytes;
    size_t increments;
    size_t cleanNurseryBlocks;
    bool contiguous;
    size_t immixAvailableMB;
    size_t usedPages = 0;
    size_t admittedPages = SIZE_MAX;
    size_t collectionReservedPages = 0;
};

// Coupled HEAD oracle 304ce69d: plan/lxr/mod.rs and plan/lxr/global.rs.
class LxrCollectionPolicy final
{
public:
    void Configure(const LxrPolicyConfiguration& configuration) { m_configuration = configuration; }

    bool BeginPause(size_t allocated)
    {
        if (m_pause_pending)
        {
            return false;
        }
        m_allocated = allocated;
        m_copied = 0;
        m_pause_pending = true;
        return true;
    }

    bool RecordCopiedPromotion(size_t size)
    {
        if (!m_pause_pending || size > SIZE_MAX - m_copied)
        {
            return false;
        }
        m_copied += size;
        return true;
    }

    bool ReleasePause()
    {
        if (!m_pause_pending)
        {
            return false;
        }
        if (m_allocated != 0)
        {
            double previous = SurvivalRatio();
            double current = std::fmin(
                static_cast<double>(m_copied) / static_cast<double>(m_allocated), 1.0);
            double next;
            if (m_configuration.weighted)
            {
                next = current > previous
                    ? (3.0 * current + previous) / 4.0 : (current + 3.0 * previous) / 4.0;
            }
            else if (m_configuration.harmonic)
            {
                next = current >= previous
                    ? 2.0 * current * previous / (current + previous)
                    : (current * current + previous * previous) / (current + previous);
            }
            else
            {
                next = (3.0 * current + previous) / 4.0;
            }
            m_survival.store(std::fmin(next, 1.0), std::memory_order_release);
        }
        m_allocated = m_copied = 0;
        m_pause_pending = false;
        return true;
    }

    LxrCollectionCause Trigger(const LxrPolicyPressure& pressure) const
    {
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_POLICY_WRONG_ORDER_CONTROL)
        if (pressure.marking && pressure.traceDrained) { return LxrCollectionCause::FinalMark; }
#endif
        if (pressure.heapFull || pressure.usedPages >= pressure.admittedPages ||
            pressure.collectionReservedPages >= pressure.admittedPages - pressure.usedPages)
        {
            return LxrCollectionCause::HeapFull;
        }
        if (pressure.marking && pressure.traceDrained) { return LxrCollectionCause::FinalMark; }
        size_t prediction = static_cast<size_t>(
            static_cast<double>(pressure.youngBytes >> 20) * SurvivalRatio()) << 1;
        if (prediction >= m_configuration.maxSurvivalMB) { return LxrCollectionCause::Survival; }
        if (!pressure.contiguous && prediction >= pressure.immixAvailableMB)
        {
            return LxrCollectionCause::ImmixSpaceFull;
        }
        if (pressure.increments >= m_configuration.incrementsLimit) { return LxrCollectionCause::Increments; }
        if (pressure.cleanNurseryBlocks >= m_configuration.nurseryBlocks)
        {
            return LxrCollectionCause::FixedNursery;
        }
        if (pressure.youngBytes >= m_configuration.youngLimitBytes)
        {
            return LxrCollectionCause::YoungAllocation;
        }
        return LxrCollectionCause::None;
    }

    bool FinishLazySweep(
        LxrPauseKind pause, size_t totalPages, size_t maturePages, bool marking,
        bool concurrentEnabled, bool drained)
    {
        if (!drained || totalPages == 0 || totalPages > SIZE_MAX / 100 ||
            maturePages > SIZE_MAX / 100)
        {
            return false;
        }
        bool update = pause == LxrPauseKind::Full || pause == LxrPauseKind::FinalMark;
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_POLICY_WRONG_UPDATE_CONTROL)
        update = true;
#endif
        if (update)
        {
            m_mature.store((3.0 * static_cast<double>(maturePages) + MatureLivePages()) / 4.0,
                std::memory_order_release);
        }
        size_t predicted = static_cast<size_t>(MatureLivePages());
        size_t garbage = maturePages > predicted ? maturePages - predicted : 0;
        m_cycle.store(!marking && concurrentEnabled &&
            garbage * 100 >= m_configuration.traceThreshold * totalPages, std::memory_order_release);
        size_t minimum = totalPages * m_configuration.rcStopPercent / 100;
        size_t cap = size_t{1} << (30 - 12);
        minimum = minimum < cap ? minimum : cap;
        m_emergency.store(totalPages < minimum + maturePages, std::memory_order_release);
        return true;
    }

    LxrPauseKind Select(bool marking, bool traceDrained, bool emergency, bool user, bool concurrentEnabled) const
    {
        if (marking && traceDrained) { return LxrPauseKind::FinalMark; }
        if (emergency || user || EmergencyHint())
        {
            return marking ? LxrPauseKind::FinalMark : LxrPauseKind::Full;
        }
        if (CycleHint() && !marking)
        {
            return concurrentEnabled ? LxrPauseKind::InitialMark : LxrPauseKind::Full;
        }
        return LxrPauseKind::RefCount;
    }

    double SurvivalRatio() const { return m_survival.load(std::memory_order_acquire); }
    size_t CollectionReservedPages(size_t cleanNurseryBytes, size_t defragHeadroomPages) const
    {
        // Literal shipped HEAD accounting: the MiB-valued survival term is added as pages.
        size_t survival = static_cast<size_t>(static_cast<double>(cleanNurseryBytes >> 20) * SurvivalRatio());
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_RESERVE_UNITS_CONTROL)
        survival = static_cast<size_t>(static_cast<double>(cleanNurseryBytes >> 12) * SurvivalRatio());
#endif
        return (survival << 1) + defragHeadroomPages;
    }
    double MatureLivePages() const { return m_mature.load(std::memory_order_acquire); }
    bool CycleHint() const { return m_cycle.load(std::memory_order_acquire); }
    bool EmergencyHint() const { return m_emergency.load(std::memory_order_acquire); }

private:
    LxrPolicyConfiguration m_configuration{};
    std::atomic<double> m_survival{0.01};
    std::atomic<double> m_mature{0.0};
    std::atomic<bool> m_cycle{false};
    std::atomic<bool> m_emergency{false};
    size_t m_allocated = 0;
    size_t m_copied = 0;
    bool m_pause_pending = false;
};

#endif // LXR_COLLECTION_POLICY_H
