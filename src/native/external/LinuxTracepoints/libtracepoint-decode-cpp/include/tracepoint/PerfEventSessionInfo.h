// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#pragma once
#ifndef _included_PerfEventSessionInfo_h
#define _included_PerfEventSessionInfo_h

#include <stdint.h>

#ifdef _WIN32
#include <sal.h>
#endif
#ifndef _Out_
#define _Out_
#endif

namespace tracepoint_decode
{
    // Semantics equivalent to struct timespec from <time.h>.
    struct PerfEventTimeSpec
    {
        int64_t tv_sec;     // Seconds since 1970.
        uint32_t tv_nsec;   // Nanoseconds.
    };

    class PerfEventSessionInfo
    {
        int64_t m_clockOffsetSec;
        uint32_t m_clockOffsetNsec;
        uint32_t m_clockId;
        bool m_clockOffsetKnown;

    public:

        constexpr
        PerfEventSessionInfo() noexcept
            : m_clockOffsetSec(0)
            , m_clockOffsetNsec(0)
            , m_clockId(0xFFFFFFFF)
            , m_clockOffsetKnown(false)
        {
            return;
        }

        // From HEADER_CLOCKID. If unknown, use SetClockid(0xFFFFFFFF).
        void
        SetClockid(uint32_t clockid) noexcept;

        // From HEADER_CLOCK_DATA. If unknown, use SetClockData(0xFFFFFFFF, 0, 0).
        void
        SetClockData(uint32_t clockid, uint64_t wallClockNS, uint64_t clockidTimeNS) noexcept;

        // Gets offset values suitable for use in HEADER_CLOCK_DATA.
        // Note: The returned NS values may be normalized relative to the values provided
        // to SetClockData, but the difference between them will be the same as the
        // difference between the values provided to SetClockData.
        void
        GetClockData(
            _Out_ uint64_t* wallClockNS,
            _Out_ uint64_t* clockidTimeNS) const noexcept;

        // Returns the clockid of the session timestamp, e.g. CLOCK_MONOTONIC.
        // Returns 0xFFFFFFFF if the session timestamp clockid is unknown.
        constexpr uint32_t
        Clockid() const noexcept
        {
            return m_clockId;
        }

        // Returns the CLOCK_REALTIME value that corresponds to an event timestamp of 0
        // for this session. Returns 1970 if the session timestamp offset is unknown.
        PerfEventTimeSpec
        ClockOffset() const noexcept;

        // Returns true if session clock offset is known.
        constexpr bool
        ClockOffsetKnown() const noexcept
        {
            return m_clockOffsetKnown;
        }

        // Converts time from session timestamp to real-time (time since 1970):
        // TimeToRealTime = ClockOffset() + time.
        // If session clock offset is unknown, assume 1970.
        PerfEventTimeSpec
        TimeToRealTime(uint64_t time) const noexcept;
    };
}
// namespace tracepoint_decode

#endif // _included_PerfEventSessionInfo_h
