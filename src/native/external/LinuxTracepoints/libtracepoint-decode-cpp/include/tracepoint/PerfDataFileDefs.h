// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#pragma once
#ifndef _included_PerfDataFileDefs_h
#define _included_PerfDataFileDefs_h

#include <stdint.h>

#ifdef _WIN32
#include <sal.h>
#endif
#ifndef _Field_size_
#define _Field_size_(size)
#endif
#ifndef _Field_z_
#define _Field_z_
#endif

// Forward declarations from PerfEventAbi.h or linux/uapi/linux/perf_event.h:
struct perf_event_attr;
struct perf_event_header;

namespace tracepoint_decode
{
    // Forward declaration from PerfEventMetadata.h:
    class PerfEventMetadata;

    // uint8 header index.
    // From: perf.data-file-format.txt, perf/util/header.h.
    enum PerfHeaderIndex : uint8_t {
        PERF_HEADER_RESERVED = 0,       // always cleared
        PERF_HEADER_FIRST_FEATURE = 1,
        PERF_HEADER_TRACING_DATA = 1,
        PERF_HEADER_BUILD_ID,
        PERF_HEADER_HOSTNAME,
        PERF_HEADER_OSRELEASE,
        PERF_HEADER_VERSION,
        PERF_HEADER_ARCH,
        PERF_HEADER_NRCPUS,
        PERF_HEADER_CPUDESC,
        PERF_HEADER_CPUID,
        PERF_HEADER_TOTAL_MEM,
        PERF_HEADER_CMDLINE,
        PERF_HEADER_EVENT_DESC,
        PERF_HEADER_CPU_TOPOLOGY,
        PERF_HEADER_NUMA_TOPOLOGY,
        PERF_HEADER_BRANCH_STACK,
        PERF_HEADER_PMU_MAPPINGS,
        PERF_HEADER_GROUP_DESC,
        PERF_HEADER_AUXTRACE,
        PERF_HEADER_STAT,
        PERF_HEADER_CACHE,
        PERF_HEADER_SAMPLE_TIME,
        PERF_HEADER_MEM_TOPOLOGY,
        PERF_HEADER_CLOCKID,
        PERF_HEADER_DIR_FORMAT,
        PERF_HEADER_BPF_PROG_INFO,
        PERF_HEADER_BPF_BTF,
        PERF_HEADER_COMPRESSED,
        PERF_HEADER_CPU_PMU_CAPS,
        PERF_HEADER_CLOCK_DATA,
        PERF_HEADER_HYBRID_TOPOLOGY,
        PERF_HEADER_PMU_CAPS,
        PERF_HEADER_LAST_FEATURE,
    };

    struct PerfEventDesc
    {
        perf_event_attr const* attr;    // NULL for unknown id.
        _Field_z_ char const* name;     // "" if no name available, e.g. if no PERF_HEADER_EVENT_DESC header.
        PerfEventMetadata const* metadata; // NULL if no metadata available.
        _Field_size_(ids_count) uint64_t const* ids; // The sample_ids that share this descriptor.
        uint32_t ids_count;
    };
}
// namespace tracepoint_decode

#endif // _included_PerfDataFileDefs_h
