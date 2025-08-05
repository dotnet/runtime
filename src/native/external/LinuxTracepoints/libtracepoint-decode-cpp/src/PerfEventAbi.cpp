// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#include <tracepoint/PerfEventAbi.h>
#include <stddef.h>
#include <stdlib.h>
#include <stdio.h>

#ifdef _WIN32
#define bswap_16(n) _byteswap_ushort(n)
#define bswap_32(n) _byteswap_ulong(n)
#define bswap_64(n) _byteswap_uint64(n)
#else // _WIN32
#include <byteswap.h>
#endif // _WIN32

using namespace tracepoint_decode;

template<class T, unsigned N>
static constexpr unsigned
ArrayCount(T(&)[N]) noexcept
{
    return N;
}

template<class T>
static void
Bswap(T& value) noexcept
{
    if constexpr (sizeof(value) == sizeof(uint16_t))
    {
        value = static_cast<T>(bswap_16(value));
    }
    else if constexpr (sizeof(value) == sizeof(uint32_t))
    {
        value = static_cast<T>(bswap_32(value));
    }
    else if constexpr (sizeof(value) == sizeof(uint64_t))
    {
        value = static_cast<T>(bswap_64(value));
    }
    else
    {
        static_assert(sizeof(T) == 0, "Bad Bswap");
    }
}

void
perf_event_attr::ByteSwap() noexcept
{
    Bswap(type);
    Bswap(size);
    Bswap(config);
    Bswap(sample_period);
    Bswap(sample_type);
    Bswap(read_format);

    // Bitfield hack: Reverse bits within each byte, don't reorder bytes.
    unsigned constexpr offsetof_bitfield = offsetof(perf_event_attr, read_format) + sizeof(read_format);
    static_assert(offsetof_bitfield + sizeof(uint64_t) == offsetof(perf_event_attr, wakeup_events));
    for (uint8_t* pb = reinterpret_cast<uint8_t*>(this) + offsetof_bitfield;
        pb != reinterpret_cast<uint8_t*>(this) + offsetof_bitfield + sizeof(uint64_t);
        pb += 1)
    {
        uint8_t b = *pb;
        b = ((b & 0xF0) >> 4) | ((b & 0x0F) << 4);
        b = ((b & 0xCC) >> 2) | ((b & 0x33) << 2);
        b = ((b & 0xAA) >> 1) | ((b & 0x55) << 1);
        *pb = b;
    }

    Bswap(wakeup_events);
    Bswap(bp_type);
    Bswap(bp_addr);
    Bswap(bp_len);
    Bswap(branch_sample_type);
    Bswap(sample_regs_user);
    Bswap(sample_stack_user);
    Bswap(aux_watermark);
    Bswap(sample_max_stack);
    Bswap(aux_sample_size);

}

void
perf_event_header::ByteSwap() noexcept
{
    Bswap(type);
    Bswap(misc);
    Bswap(size);
}

_Ret_z_
static char const*
EnumToString(
    char const* const names[],
    unsigned nameCount,
    uint32_t value,
    _Pre_cap_(11) char* scratch) noexcept
{
    char const* str;
    if (value < nameCount)
    {
        str = names[value];
    }
    else
    {
        snprintf(scratch, 11, "%u", value);
        str = scratch;
    }
    return str;
}

_Ret_z_ char const* _ltpDecl
PerfEnumToString(perf_type_id value, _Pre_cap_(11) char* scratch) noexcept
{
    static char const* const names[] = {
        "HARDWARE",
        "SOFTWARE",
        "TRACEPOINT",
        "HW_CACHE",
        "RAW",
        "BREAKPOINT",
    };
    static_assert(ArrayCount(names) == PERF_TYPE_MAX);
    return EnumToString(names, ArrayCount(names), value, scratch);
}

_Ret_z_ char const* _ltpDecl
PerfEnumToString(perf_event_type value, _Pre_cap_(11) char* scratch) noexcept
{
    static char const* const names[] = {
        "0",
        "MMAP",
        "LOST",
        "COMM",
        "EXIT",
        "THROTTLE",
        "UNTHROTTLE",
        "FORK",
        "READ",
        "SAMPLE",
        "MMAP2",
        "AUX",
        "ITRACE_START",
        "LOST_SAMPLES",
        "SWITCH",
        "SWITCH_CPU_WIDE",
        "NAMESPACES",
        "KSYMBOL",
        "BPF_EVENT",
        "CGROUP",
        "TEXT_POKE",
        "AUX_OUTPUT_HW_ID",
    };
    static_assert(ArrayCount(names) == PERF_RECORD_MAX);

    static char const* const moreNames[] = {
        "HEADER_ATTR",
        "HEADER_EVENT_TYPE",
        "HEADER_TRACING_DATA",
        "HEADER_BUILD_ID",
        "FINISHED_ROUND",
        "ID_INDEX",
        "AUXTRACE_INFO",
        "AUXTRACE",
        "AUXTRACE_ERROR",
        "THREAD_MAP",
        "CPU_MAP",
        "STAT_CONFIG",
        "STAT",
        "STAT_ROUND",
        "EVENT_UPDATE",
        "TIME_CONV",
        "HEADER_FEATURE",
        "COMPRESSED",
        "FINISHED_INIT",
    };
    static_assert(ArrayCount(moreNames) == PERF_RECORD_FINISHED_INIT - PERF_RECORD_HEADER_ATTR + 1);

    return PERF_RECORD_HEADER_ATTR <= value && value <= PERF_RECORD_FINISHED_INIT
        ? moreNames[value - PERF_RECORD_HEADER_ATTR]
        : EnumToString(names, ArrayCount(names), value, scratch);
}
