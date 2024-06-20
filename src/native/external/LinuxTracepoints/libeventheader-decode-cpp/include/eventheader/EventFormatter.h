// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#pragma once
#ifndef _included_EventFormatter_h
#define _included_EventFormatter_h 1

#include "EventEnumerator.h"
#include <string>

namespace tracepoint_decode
{
    // Forward declarations from libtracepoint-decode
    struct PerfSampleEventInfo;
    class PerfFieldMetadata;
}

namespace eventheader_decode
{
    // Forward declarations from this file.
    enum EventFormatterJsonFlags : unsigned;
    enum EventFormatterMetaFlags : unsigned;

    /*
    Helper for converting event fields to strings.
    */
    class EventFormatter
    {
    public:

        /*
        Formats the specified sample as a UTF-8 JSON string and appends the
        result to dest.

        If the Name flag is not specified, the appended string is a valid JSON object.
        If the Name flag is specified, the appended string is a valid JSON member,
        i.e. it is a "FieldName": prefix followed by a valid JSON object.

        If the Space flag is specified, the appended string will begin with a space
        and will have spaces between elements, e.g. after ',' and ':'.

        Returns 0 for success, errno for error. May throw bad_alloc.
        */
        int
        AppendSampleAsJson(
            std::string& dest,
            tracepoint_decode::PerfSampleEventInfo const& sampleEventInfo,
            bool fileBigEndian,
            EventFormatterJsonFlags jsonFlags = static_cast<EventFormatterJsonFlags>(0),
            EventFormatterMetaFlags metaFlags = static_cast<EventFormatterMetaFlags>(0xffff),
            uint32_t moveNextLimit = 4096);

        /*
        Formats the specified sample field as a UTF-8 JSON string and appends the
        result to dest.

        If the Name flag is not specified, the appended string is a valid JSON value.
        If the Name flag is specified, the appended string is a valid JSON member,
        i.e. it is a "FieldName": prefix followed by a valid JSON value.

        If the Space flag is specified, the appended string will begin with a space
        and will have spaces between elements, e.g. after ',' and ':'.

        Returns 0 for success, errno for error. May throw bad_alloc.
        */
        int
        AppendSampleFieldAsJson(
            std::string& dest,
            _In_reads_bytes_(fieldRawDataSize) void const* fieldRawData,
            size_t fieldRawDataSize,
            tracepoint_decode::PerfFieldMetadata const& fieldMetadata,
            bool fileBigEndian,
            EventFormatterJsonFlags jsonFlags = static_cast<EventFormatterJsonFlags>(0));

        /*
        Formats the enumerator's event as a UTF-8 JSON string and appends the
        result to dest. Moves the enumerator to the end of the event.
    
        If the Name flag is not specified, the appended string is a valid JSON object.
        If the Name flag is specified, the appended string is a valid JSON member,
        i.e. it is a "FieldName": prefix followed by a valid JSON object.

        If the Space flag is specified, the appended string will begin with a space
        and will have spaces between elements, e.g. after ',' and ':'.

        Returns 0 for success, errno for error. May throw bad_alloc.

        Requires: enumerator.State is BeforeFirstItem.
        */
        int
        AppendEventAsJsonAndMoveToEnd(
            std::string& dest,
            EventEnumerator& enumerator,
            EventFormatterJsonFlags jsonFlags = static_cast<EventFormatterJsonFlags>(0),
            EventFormatterMetaFlags metaFlags = static_cast<EventFormatterMetaFlags>(0xffff));

        /*
        Formats the item at the enumerator's current position (a value, array
        begin, or structure begin) as UTF-8 JSON string and appends the result to
        dest. Moves the enumerator to the next item as if by MoveNextSibling.
    
        Returns 0 for success, errno for error. May throw bad_alloc.

        Requires: enumerator.State is Value, ArrayBegin, or StructBegin.

        Booleans, decimal integers, and finite floating-point data values will be
        unquoted. Other simple data values (including hexadecimal integers, infinities
        and NaNs) will be quoted. Complex items (structures and arrays) will be
        converted to JSON objects and arrays.

        If the Name flag is not specified, the appended string is a valid JSON value.
        If the Name flag is specified, the appended string is a valid JSON member,
        i.e. it is a "FieldName": prefix followed by a valid JSON value.

        If the Space flag is specified, the appended string will begin with a space
        and will have spaces between elements, e.g. after ',' and ':'.
        */
        int
        AppendItemAsJsonAndMoveNextSibling(
            std::string& dest,
            EventEnumerator& enumerator,
            EventFormatterJsonFlags jsonFlags = static_cast<EventFormatterJsonFlags>(0));

        /*
        Formats the event field value at the enumerator's current position as a
        UTF-8 string and appends the result to dest.
    
        Returns 0 for success, errno for error. May throw bad_alloc.

        Requires: enumerator.State is Value.
        */
        int
        AppendValue(
            std::string& dest,
            EventEnumerator const& enumerator);

        /*
        Formats the event field value as a UTF-8 string and appends the
        result to dest.
    
        Returns 0 for success, errno for error. May throw bad_alloc.
    
        Requires: valueItem is a Value, not an ArrayBegin, StructEnd, etc.
        */
        int
        AppendValue(
            std::string& dest,
            EventItemInfo const& valueItemInfo);

        /*
        Formats the specified event field value as a UTF-8 string and appends the
        result to dest.
    
        Returns 0 for success, errno for error. May throw bad_alloc.
        */
        int
        AppendValue(
            std::string& dest,
            _In_reads_bytes_(valueSize) void const* valueData,
            uint32_t valueSize,
            event_field_encoding encoding,
            event_field_format format,
            bool needsByteSwap);

        /*
        Formats the specified big-endian UUID value as a UTF-8 string and appends
        the result to dest. UUID is formatted as 36 chars with dashes, e.g.
        "00000000-0000-0000-0000-000000000000".

        May throw bad_alloc.
        */
        void
        AppendUuid(
            std::string& dest,
            _In_reads_bytes_(16) uint8_t const* uuid);
    };

    /*
    Flags for use when formatting an item as a JSON string with
    AppendItemAsJsonAndMoveNextSibling.
    */
    enum EventFormatterJsonFlags : unsigned
    {
        EventFormatterJsonFlags_None = 0,
        EventFormatterJsonFlags_Name = 0x1,  // Include a "Name": prefix for root item.
        EventFormatterJsonFlags_Space = 0x2, // Include a space between values.
        EventFormatterJsonFlags_FieldTag = 0x4, // Append ";tag=0xNNNN" to name of fields if tag != 0.
    };

    /*
    Flags controlling the metadata to be included in the "meta" suffix of a JSON
    event string.

    Note that the "n" field is for the convenience of human readers of the JSON file.
    It contains the provider and event names and appears at the start of the event
    rather than in the "meta" section even though it is technically metadata.

    Note that the format of the "time" field depends on the clock information that was
    provided by the session:

    - If clock information is available: "yyyy-mm-ddThh:mm:ss.nnnnnnnnnZ"
    - Else, timestamp is seconds relative to an unknown epoch: 123.123456789

    For consistent behavior, always include clock information in the trace,
    e.g. "perf record -k monotonic -e ...".
    */
    enum EventFormatterMetaFlags : unsigned
    {
        EventFormatterMetaFlags_None = 0,           // disable the "meta" suffix.
        EventFormatterMetaFlags_n = 0x1,            // "n":"provider:event" before the user fields (not in the suffix).
        EventFormatterMetaFlags_time = 0x2,         // timestamp (only for sample events).
        EventFormatterMetaFlags_cpu = 0x4,          // cpu index (only for sample events).
        EventFormatterMetaFlags_pid = 0x8,          // process id (only for sample events).
        EventFormatterMetaFlags_tid = 0x10,         // thread id (only for sample events).
        EventFormatterMetaFlags_id = 0x20,          // eventheader id (decimal integer, omitted if 0).
        EventFormatterMetaFlags_version = 0x40,     // eventheader version (decimal integer, omitted if 0).
        EventFormatterMetaFlags_level = 0x80,       // eventheader level (decimal integer, omitted if 0).
        EventFormatterMetaFlags_keyword = 0x100,    // eventheader keyword (hexadecimal string, omitted if 0).
        EventFormatterMetaFlags_opcode = 0x200,     // eventheader opcode (decimal integer, omitted if 0).
        EventFormatterMetaFlags_tag = 0x400,        // eventheader tag (hexadecimal string, omitted if 0).
        EventFormatterMetaFlags_activity = 0x800,   // eventheader activity ID (UUID string, omitted if 0).
        EventFormatterMetaFlags_relatedActivity = 0x1000,// eventheader related activity ID (UUID string, omitted if not set).
        EventFormatterMetaFlags_provider = 0x10000, // provider name or system name (string).
        EventFormatterMetaFlags_event = 0x20000,    // event name or tracepoint name (string).
        EventFormatterMetaFlags_options = 0x40000,  // eventheader provider options (string, omitted if none).
        EventFormatterMetaFlags_flags = 0x80000,    // eventheader flags (hexadecimal string).
        EventFormatterMetaFlags_common = 0x100000,  // Include the common_* fields before the user fields (only for sample events).
        EventFormatterMetaFlags_Default = 0xffff,   // Include n..relatedActivity.
        EventFormatterMetaFlags_All = ~0u
    };
}
// namespace eventheader_decode

#endif // _included_EventFormatter_h
