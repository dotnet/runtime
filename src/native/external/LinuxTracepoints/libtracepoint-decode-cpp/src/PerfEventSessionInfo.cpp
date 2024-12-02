// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#include <tracepoint/PerfEventSessionInfo.h>
#include <stdio.h>

using namespace tracepoint_decode;

static auto const Billion = 1000000000u;

void
PerfEventSessionInfo::SetClockid(
    uint32_t clockid) noexcept
{
    m_clockId = clockid;
}

void
PerfEventSessionInfo::SetClockData(
    uint32_t clockid,
    uint64_t wallClockNS,
    uint64_t clockidTimeNS) noexcept
{
    if (clockid == 0xFFFFFFFF)
    {
        // Offset is unspecified.

        m_clockOffsetSec = 0;
        m_clockOffsetNsec = 0;
        m_clockId = clockid;
        m_clockOffsetKnown = false;
    }
    else if (clockidTimeNS <= wallClockNS)
    {
        // Offset is positive.

        // wallClockNS = clockidTimeNS + offsetNS
        // offsetNS = wallClockNS - clockidTimeNS
        auto const offsetNS = wallClockNS - clockidTimeNS;

        // offsetNS = sec * Billion + nsec

        // sec = offsetNS / Billion
        m_clockOffsetSec = static_cast<int64_t>(offsetNS / Billion);

        // nsec = offsetNS % Billion
        m_clockOffsetNsec = static_cast<uint32_t>(offsetNS % Billion);

        m_clockId = clockid;
        m_clockOffsetKnown = true;
    }
    else
    {
        // Offset is negative.

        // wallClockNS = clockidTimeNS + offsetNS
        // offsetNS = wallClockNS - clockidTimeNS
        // -negOffsetNS = wallClockNS - clockidTimeNS
        // negOffsetNS = clockidTimeNS - wallClockNS
        auto const negOffsetNS = clockidTimeNS - wallClockNS;

        // negOffsetNS = (negOffsetNS / Billion) * Billion + (negOffsetNS % Billion)
        // negOffsetNS = (negOffsetNS / Billion) * Billion + (negOffsetNS % Billion) - Billion + Billion
        // negOffsetNS = (negOffsetNS / Billion + 1) * Billion + (negOffsetNS % Billion) - Billion

        // negOffsetNS = negSec * Billion + negNsec
        // negSec = negOffsetNS / Billion + 1
        // negNsec = (negOffsetNS % Billion) - Billion

        // sec = -(negOffsetNS / Billion + 1)
        m_clockOffsetSec = -static_cast<int64_t>(negOffsetNS / Billion) - 1;

        // nsec = -((negOffsetNS % Billion) - Billion)
        m_clockOffsetNsec = Billion - static_cast<uint32_t>(negOffsetNS % Billion);

        // Fix up case where nsec is too large.
        if (m_clockOffsetNsec == Billion)
        {
            m_clockOffsetSec += 1;
            m_clockOffsetNsec -= Billion;
        }

        m_clockId = clockid;
        m_clockOffsetKnown = true;
    }
}

void
PerfEventSessionInfo::GetClockData(
    _Out_ uint64_t* wallClockNS,
    _Out_ uint64_t* clockidTimeNS) const noexcept
{
    if (m_clockOffsetSec >= 0)
    {
        *clockidTimeNS = 0;
        *wallClockNS = static_cast<uint64_t>(m_clockOffsetSec) * Billion + m_clockOffsetNsec;
    }
    else
    {
        *wallClockNS = 0;
        *clockidTimeNS = static_cast<uint64_t>(-m_clockOffsetSec) * Billion - m_clockOffsetNsec;
    }
}

PerfEventTimeSpec
PerfEventSessionInfo::ClockOffset() const noexcept
{
    return { m_clockOffsetSec, m_clockOffsetNsec };
}

PerfEventTimeSpec
PerfEventSessionInfo::TimeToRealTime(uint64_t time) const noexcept
{
    auto sec = static_cast<int64_t>(time / Billion);
    auto nsec = static_cast<uint32_t>(time % Billion);
    sec += m_clockOffsetSec;
    nsec += m_clockOffsetNsec;
    if (nsec >= Billion)
    {
        sec += 1;
        nsec -= Billion;
    }
    return { sec, nsec };
}
