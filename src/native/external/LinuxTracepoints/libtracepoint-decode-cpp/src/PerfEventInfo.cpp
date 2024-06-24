// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#include <tracepoint/PerfEventInfo.h>
#include <tracepoint/PerfEventAbi.h>
#include <tracepoint/PerfDataFileDefs.h>
#include <assert.h>

using namespace tracepoint_decode;

uint64_t
PerfSampleEventInfo::SampleType() const noexcept
{
    assert(event_desc->attr); // Requires: GetSampleEventInfo() succeeded.
    return event_desc->attr->sample_type;
}

perf_event_attr const&
PerfSampleEventInfo::Attr() const noexcept
{
    assert(event_desc->attr); // Requires: GetSampleEventInfo() succeeded.
    return *event_desc->attr;
}

_Ret_z_ char const*
PerfSampleEventInfo::Name() const noexcept
{
    assert(event_desc->attr); // Requires: GetSampleEventInfo() succeeded.
    return event_desc->name;
}

_Ret_opt_ PerfEventMetadata const*
PerfSampleEventInfo::Metadata() const noexcept
{
    assert(event_desc->attr); // Requires: GetSampleEventInfo() succeeded.
    return event_desc->metadata;
}

uint64_t
PerfNonSampleEventInfo::SampleType() const noexcept
{
    assert(event_desc->attr); // Requires: GetNonSampleEventInfo() succeeded.
    return event_desc->attr->sample_type;
}

perf_event_attr const&
PerfNonSampleEventInfo::Attr() const noexcept
{
    assert(event_desc->attr); // Requires: GetNonSampleEventInfo() succeeded.
    return *event_desc->attr;
}

_Ret_z_ char const*
PerfNonSampleEventInfo::Name() const noexcept
{
    assert(event_desc->attr); // Requires: GetNonSampleEventInfo() succeeded.
    return event_desc->name;
}
