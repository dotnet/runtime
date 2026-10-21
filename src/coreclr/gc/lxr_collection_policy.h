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
    // Port-specific: pages the young pause may need as to-space, and free Immix pages that can hold it.
    size_t copyReservePages = 0;
    size_t immixFreePages = SIZE_MAX;
    // Port-specific (P17 S5b): the pause may take blocks beyond the admitted capacity for its copy, so no to-space
    // reserve is needed. The heap counts as full when no whole block is free.
    bool overshoot = false;
};

// Coupled HEAD oracle 304ce69d: plan/lxr/mod.rs and plan/lxr/global.rs.
class LxrCollectionPolicy final
{
public:
    void Configure(const LxrPolicyConfiguration& configuration) { m_configuration = configuration; }

    bool BeginPause(size_t allocated)
    {
        return BeginPause(allocated, allocated);
    }

    // evacuable: the young bytes in clean nursery blocks. Only they can be copied, so the measured
    // survival ratio of the to-space reserve is taken against them (RecordSurvivors counts the same bytes).
    bool BeginPause(size_t allocated, size_t evacuable)
    {
        if (m_pause_pending)
        {
            return false;
        }
        m_allocated = allocated;
        m_evacuable = evacuable;
        m_copied = 0;
        m_survivors = 0;
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

    // Port-specific: bytes of evacuable young objects (in clean nursery blocks) that were promoted in this
    // pause, whether they were copied or kept in place. The Java LXR measures only copied bytes. Without
    // copying (or when the to-space is full and copying stops) that value is zero, so it cannot size a
    // to-space. Young objects in recycled blocks are never copied and are not counted.
    bool RecordSurvivors(size_t bytes)
    {
        if (!m_pause_pending || bytes > SIZE_MAX - m_survivors)
        {
            return false;
        }
        m_survivors += bytes;
        return true;
    }

    bool ReleasePause()
    {
        if (!m_pause_pending)
        {
            return false;
        }
        if (m_evacuable != 0)
        {
            double measured = std::fmin(
                static_cast<double>(m_survivors) / static_cast<double>(m_evacuable), 1.0);
            double reserve = m_reserve_survival.load(std::memory_order_relaxed);
            if (!m_reserve_measured)
            {
                reserve = measured;
                m_reserve_measured = true;
            }
            else
            {
                // Rise at once, fall slowly: an underestimate costs a failed copy, an overestimate only memory.
                reserve = measured > reserve ? (3.0 * measured + reserve) / 4.0 : (measured + 3.0 * reserve) / 4.0;
            }
            m_reserve_survival.store(std::fmin(reserve, 1.0), std::memory_order_release);
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
        m_allocated = m_evacuable = m_copied = m_survivors = 0;
        m_pause_pending = false;
        return true;
    }

    LxrCollectionCause Trigger(const LxrPolicyPressure& pressure) const
    {
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_POLICY_WRONG_ORDER_CONTROL)
        if (pressure.marking && pressure.traceDrained) { return LxrCollectionCause::FinalMark; }
#endif
        if (pressure.heapFull || pressure.usedPages >= pressure.admittedPages ||
            pressure.collectionReservedPages >= pressure.admittedPages - pressure.usedPages ||
            CopyReserveExhausted(pressure))
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

    // Port-specific deviation from the Java LXR (CollectionReservedPages above). The Java formula adds
    // a survival term in MiB as if it were pages, and relies on GC threads taking pages beyond the
    // heap budget during a pause. The port can copy only into blocks that are already admitted, so the
    // to-space must exist when the pause starts. This reserve is computed in bytes: the clean nursery bytes
    // allocated since the last pause (the only young bytes a pause can copy), times the measured survival
    // ratio of those bytes, times a safety factor. Young bytes in recycled blocks are promoted in place,
    // need no to-space and add nothing, so the reserve is zero when the mutator has no clean block.
    // Before the first pause no ratio is known, so all young bytes are assumed to survive.
    static constexpr size_t ReserveSafetyFactor = 2;
    // The measured ratio can fall to zero in a quiet phase; a later burst would then find no to-space.
    static constexpr double ReserveMinimumSurvival = 0.05;
    double MeasuredSurvivalRatio() const { return m_reserve_survival.load(std::memory_order_acquire); }
    double ReserveSurvivalRatio() const
    {
        return std::fmax(MeasuredSurvivalRatio(), ReserveMinimumSurvival);
    }
    size_t CollectionReservedBytes(size_t youngBytes) const
    {
        double bytes = static_cast<double>(youngBytes) * ReserveSurvivalRatio() *
            static_cast<double>(ReserveSafetyFactor);
        return bytes >= static_cast<double>(SIZE_MAX / 2) ? SIZE_MAX / 2 : static_cast<size_t>(std::ceil(bytes));
    }
    size_t CollectionReservedPagesForCopy(size_t youngBytes, size_t pageBytes) const
    {
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_RESERVE_UNITS_CONTROL)
        return static_cast<size_t>(static_cast<double>(youngBytes >> 20) * ReserveSurvivalRatio()) * ReserveSafetyFactor;
#else
        size_t bytes = CollectionReservedBytes(youngBytes);
        return bytes / pageBytes + (bytes % pageBytes != 0 ? 1 : 0);
#endif
    }
    bool CycleHint() const { return m_cycle.load(std::memory_order_acquire); }
    bool EmergencyHint() const { return m_emergency.load(std::memory_order_acquire); }

    // Pages of one Immix block: with overshoot the trigger keeps one block free for the mutator and nothing for the copy.
    static constexpr size_t OvershootFreePages = 8;

private:
    bool CopyReserveExhausted(const LxrPolicyPressure& pressure) const
    {
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_S5B_RESERVE_TRIGGER_CONTROL)
        return pressure.copyReservePages != 0 && pressure.copyReservePages >= pressure.immixFreePages;
#else
        if (pressure.overshoot)
        {
            return pressure.immixFreePages <= OvershootFreePages;
        }
        return pressure.copyReservePages != 0 && pressure.copyReservePages >= pressure.immixFreePages;
#endif
    }

    LxrPolicyConfiguration m_configuration{};
    std::atomic<double> m_survival{0.01};
    std::atomic<double> m_mature{0.0};
    std::atomic<double> m_reserve_survival{1.0};
    bool m_reserve_measured = false;
    std::atomic<bool> m_cycle{false};
    std::atomic<bool> m_emergency{false};
    size_t m_allocated = 0;
    size_t m_evacuable = 0;
    size_t m_copied = 0;
    size_t m_survivors = 0;
    bool m_pause_pending = false;
};

#endif // LXR_COLLECTION_POLICY_H
