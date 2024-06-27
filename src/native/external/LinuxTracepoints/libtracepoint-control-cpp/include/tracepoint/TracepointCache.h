// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/*
TracepointCache: class that loads, parses, and caches the metadata (format)
information for tracepoints.

The TracepointSession class uses TracepointCache to manage format information
for its tracepoints.
*/

#pragma once
#ifndef _included_TracepointCache_h
#define _included_TracepointCache_h 1

#include "TracepointName.h"
#include <tracepoint/PerfEventMetadata.h>
#include <unordered_map>
#include <string_view>
#include <memory>
#include <vector>

#ifndef _Success_
#define _Success_(condition)
#endif
#ifndef _In_z_
#define _In_z_
#endif
#ifndef _Out_
#define _Out_
#endif

namespace tracepoint_control
{
    class TracepointSpec; // Forward declaration

    /*
    Loads, parses, and caches the metadata (format) information for tracepoints.
    */
    class TracepointCache
    {
    public:

        TracepointCache(TracepointCache const&) = delete;
        void operator=(TracepointCache const&) = delete;
        ~TracepointCache();

        /*
        May throw std::bad_alloc.
        */
        TracepointCache() noexcept(false);

        /*
        If no events are present in cache, returns -1.
        Otherwise, returns the offset of the common_type field (usually 0).
        */
        int8_t
        CommonTypeOffset() const noexcept;

        /*
        If no events are present in cache, returns 0.
        Otherwise, returns the size of the common_type field (1, 2, or 4; usually 2).
        */
        uint8_t
        CommonTypeSize() const noexcept;

        /*
        If metadata for an event with the specified ID is cached, return it.
        Otherwise, return NULL. Note that ID is from the event's common_type field
        and is not the PERF_SAMPLE_ID or PERF_SAMPLE_IDENTIFIER value.
        */
        tracepoint_decode::PerfEventMetadata const*
        FindById(uint32_t id) const noexcept;

        /*
        If metadata for an event with the specified name is cached, return it.
        Otherwise, return NULL.
        */
        tracepoint_decode::PerfEventMetadata const*
        FindByName(TracepointName const& name) const noexcept;

        /*
        If metadata for an event with the specified data is cached,
        return it. Otherwise, return NULL.

        Implementation:

        - Assume that rawData is host-endian.
        - Use CommonTypeOffset() and CommonTypeSize() to extract the common_type
          field value from the rawData.
        - Use FindById() to find the matching metadata.
        */
        tracepoint_decode::PerfEventMetadata const*
        FindByRawData(std::string_view rawData) const noexcept;

        /*
        Parse the formatFileContents to get the metadata. If systemName or
        formatFileContents is invalid, return EINVAL. If metadata for an
        event with the same name or ID is already cached, return EEXIST.
        Otherwise, add the metadata to the cache.
        */
        _Success_(return == 0) int
        AddFromFormat(
            std::string_view systemName,
            std::string_view formatFileContents,
            bool longSize64 = sizeof(long) == 8) noexcept;

        /*
        Load and parse the "/sys/.../tracing/events/systemName/eventName/format"
        file. If name or the format data is invalid, return EINVAL. If metadata
        for an event with the same name or ID is already cached, return EEXIST.
        Otherwise, add the metadata to the cache.
        */
        _Success_(return == 0) int
        AddFromSystem(TracepointName const& name) noexcept;

        /*
        If metadata for an event with the specified name is cached, return it.
        Otherwise, return AddFromSystem(name).
        */
        _Success_(return == 0) int
        FindOrAddFromSystem(
            TracepointName const& name,
            _Out_ tracepoint_decode::PerfEventMetadata const** ppMetadata) noexcept;

        /*
        Given the name of a user_events EventHeader tracepoint, pre-register and
        cache the specified event.

        Example eventName: "user_events:MyProvider_L1Kff"

        Details:

        - If the specified name is not a valid user_events EventHeader name, return EINVAL.
        - If metadata for "user_events:eventName" is already cached, return EEXIST.
        - Try to register an EventHeader tracepoint with the given tracepoint name. If
          this fails, return the error.
        - Return AddFromSystem("user_events:eventName").

        If this operation succeeds, the event will remain registered as long as this cache
        object exists.
        */
        _Success_(return == 0) int
        PreregisterEventHeaderTracepoint(TracepointName const& name) noexcept;

        /*
        Given a tracepoint definition, pre-register and cache the specified event.

        Details:

        - If spec.Kind is not Definition or EventHeaderDefinition, return EINVAL.
        - If spec.SystemName is not "user_events", return EINVAL.
        - If spec.EventName, spec.Flags, or spec.Fields is invalid, return EINVAL.
        - If metadata for "user_events:eventName" is already cached, return EEXIST.
        - Try to register a tracepoint with the given tracepoint name, flags, and
          fields. If this fails, return the error.
        - Return AddFromSystem("user_events:eventName").

        If this operation succeeds, the event will remain registered as long as this cache
        object exists.
        */
        _Success_(return == 0) int
        PreregisterTracepointDefinition(TracepointSpec const& spec) noexcept;

        /*
        Given the registration command for a user_events tracepoint, pre-register and
        cache the specified event.

        Example registerCommand: "MyEventName __rel_loc u8[] MyField1; int MyField2"

        Details:

        - Parse the command to determine the eventName. If invalid, return EINVAL.
        - If metadata for "user_events:eventName" is already cached, return EEXIST.
        - Try to register a user_events tracepoint using the specified command string. If
          this fails, return the error.
        - Return AddFromSystem("user_events:eventName").

        If this operation succeeds, the event will remain registered as long as this cache
        object exists.
        */
        _Success_(return == 0) int
        PreregisterTracepoint(_In_z_ char const* registerCommand) noexcept;

    private:

        struct TracepointRegistration
        {
            int DataFile;
            int WriteIndex;
            unsigned StatusWord;

            TracepointRegistration(TracepointRegistration const&) = delete;
            void operator=(TracepointRegistration const&) = delete;
            ~TracepointRegistration();
            TracepointRegistration() noexcept;
        };

        struct CacheVal
        {
            std::vector<char> SystemAndFormat; // = "SystemName\nFormatFileContents"
            tracepoint_decode::PerfEventMetadata Metadata; // Points into SystemAndFormat
            std::unique_ptr<TracepointRegistration> Registration;

            CacheVal(CacheVal const&) = delete;
            void operator=(CacheVal const&) = delete;
            ~CacheVal();

            CacheVal(
                std::vector<char>&& systemAndFormat,
                tracepoint_decode::PerfEventMetadata&& metadata,
                std::unique_ptr<TracepointRegistration> registration) noexcept;
        };

        struct NameHashOps
        {
            size_t operator()(TracepointName const&) const noexcept; // Hash
            size_t operator()(TracepointName const&, TracepointName const&) const noexcept; // Equal
        };

        _Success_(return == 0) int
        PreregisterTracepointImpl(_In_z_ char const* registerCommand, unsigned eventNameSize) noexcept;

        /*
        systemAndFormat = "SystemName\nFormatFileContents".
        */
        _Success_(return == 0) int
        Add(std::vector<char>&& systemAndFormat,
            size_t systemNameSize,
            bool longSize64,
            std::unique_ptr<TracepointRegistration> registration) noexcept;

        std::unordered_map<uint32_t, CacheVal> m_byId;
        std::unordered_map<TracepointName, CacheVal const&, NameHashOps, NameHashOps> m_byName;
        int8_t m_commonTypeOffset; // -1 = unset
        uint8_t m_commonTypeSize; // 0 = unset
    };
}
// namespace tracepoint_control

#endif // _included_TracepointCache_h
