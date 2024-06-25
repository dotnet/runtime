// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#pragma once
#ifndef _included_PerfEventInfo_h
#define _included_PerfEventInfo_h

#include <stdint.h>

#ifdef _WIN32
#include <sal.h>
#endif
#ifndef _Field_size_bytes_
#define _Field_size_bytes_(size)
#endif
#ifndef _Ret_z_
#define _Ret_z_
#endif
#ifndef _Ret_opt_
#define _Ret_opt_
#endif

// Forward declarations from PerfEventAbi.h or linux/uapi/linux/perf_event.h:
struct perf_event_attr;
struct perf_event_header;

namespace tracepoint_decode
{
    // Forward declaration from PerfDataFileDefs.h:
    struct PerfEventDesc;

    // Forward declaration from PerfEventSessionInfo.h:
    class PerfEventSessionInfo;

    // Forward declaration from PerfEventMetadata.h:
    class PerfEventMetadata;

    struct PerfSampleEventInfo
    {
        PerfEventDesc const* event_desc;        // Always valid if GetSampleEventInfo() succeeded.
        PerfEventSessionInfo const* session_info;//Always valid if GetSampleEventInfo() succeeded.
        perf_event_header const* header;        // Always valid if GetSampleEventInfo() succeeded. Points into event.
        uint64_t id;                            // Valid if SampleType() & (PERF_SAMPLE_IDENTIFIER | PERF_SAMPLE_ID).
        uint32_t pid, tid;                      // Valid if SampleType() & PERF_SAMPLE_TID.
        uint64_t time;                          // Valid if SampleType() & PERF_SAMPLE_TIME.
        uint64_t stream_id;                     // Valid if SampleType() & PERF_SAMPLE_STREAM_ID.
        uint32_t cpu, cpu_reserved;             // Valid if SampleType() & PERF_SAMPLE_CPU.
        uint64_t ip;                            // Valid if SampleType() & PERF_SAMPLE_IP.
        uint64_t addr;                          // Valid if SampleType() & PERF_SAMPLE_ADDR.
        uint64_t period;                        // Valid if SampleType() & PERF_SAMPLE_PERIOD.
        uint64_t const* read_values;            // Valid if SampleType() & PERF_SAMPLE_READ. Points into event.
        uint64_t const* callchain;              // Valid if SampleType() & PERF_SAMPLE_CALLCHAIN. Points into event.
        _Field_size_bytes_(raw_data_size) void const* raw_data; // Valid if SampleType() & PERF_SAMPLE_RAW. Points into event.
        uintptr_t raw_data_size;                // Valid if SampleType() & PERF_SAMPLE_RAW. Size of raw_data.

        // Requires: GetSampleEventInfo() succeeded.
        // Returns: event_desc->attr->sample_type.
        uint64_t
        SampleType() const noexcept;

        // Requires: GetSampleEventInfo() succeeded.
        // Returns: event_desc->attr.
        perf_event_attr const&
        Attr() const noexcept;

        // Requires: GetSampleEventInfo() succeeded.
        // Returns: event_desc->name.
        // May be "", e.g. if no PERF_HEADER_EVENT_DESC header. In that case,
        // caller should check for a name in Metadata().
        _Ret_z_ char const*
        Name() const noexcept;

        // Requires: GetSampleEventInfo() succeeded.
        // Returns: event_desc->metadata (may be NULL).
        // Valid if SampleType() & PERF_SAMPLE_RAW.
        _Ret_opt_ PerfEventMetadata const*
        Metadata() const noexcept;
    };

    struct PerfNonSampleEventInfo
    {
        PerfEventDesc const* event_desc;        // Always valid if GetNonSampleEventInfo() succeeded.
        PerfEventSessionInfo const* session_info;//Always valid if GetNonSampleEventInfo() succeeded.
        perf_event_header const* header;        // Always valid if GetNonSampleEventInfo() succeeded. Points into event.
        uint64_t id;                            // Valid if SampleType() & (PERF_SAMPLE_IDENTIFIER | PERF_SAMPLE_ID).
        uint32_t pid, tid;                      // Valid if SampleType() & PERF_SAMPLE_TID.
        uint64_t time;                          // Valid if SampleType() & PERF_SAMPLE_TIME.
        uint64_t stream_id;                     // Valid if SampleType() & PERF_SAMPLE_STREAM_ID.
        uint32_t cpu, cpu_reserved;             // Valid if SampleType() & PERF_SAMPLE_CPU.

        // Requires: GetNonSampleEventInfo() succeeded.
        // Returns: event_desc->attr->sample_type.
        uint64_t
        SampleType() const noexcept;

        // Requires: GetNonSampleEventInfo() succeeded.
        // Returns: event_desc->attr.
        perf_event_attr const&
        Attr() const noexcept;

        // Requires: GetNonSampleEventInfo() succeeded.
        // Returns: event_desc->name.
        _Ret_z_ char const*
        Name() const noexcept;
    };
}
// namespace tracepoint_decode

#endif // _included_PerfEventInfo_h
