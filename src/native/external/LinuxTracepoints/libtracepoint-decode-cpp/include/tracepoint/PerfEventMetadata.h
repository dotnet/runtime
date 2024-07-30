// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#pragma once
#ifndef _included_PerfEventMetadata_h
#define _included_PerfEventMetadata_h

#include <stdint.h>
#include <string_view>
#include <vector>

#ifdef _WIN32
#include <sal.h>
#endif
#ifndef _In_reads_bytes_
#define _In_reads_bytes_(cb)
#endif

namespace tracepoint_decode
{
    enum PerfFieldElementSize : uint8_t
    {
        PerfFieldElementSize8  = 0, // sizeof(uint8_t)  == 1 << PerfFieldElementSize8
        PerfFieldElementSize16 = 1, // sizeof(uint16_t) == 1 << PerfFieldElementSize16
        PerfFieldElementSize32 = 2, // sizeof(uint32_t) == 1 << PerfFieldElementSize32
        PerfFieldElementSize64 = 3, // sizeof(uint64_t) == 1 << PerfFieldElementSize64
    };

    enum PerfFieldFormat : uint8_t
    {
        PerfFieldFormatNone,    // Type unknown (treat as binary blob)
        PerfFieldFormatUnsigned,// u8, u16, u32, u64, etc.
        PerfFieldFormatSigned,  // s8, s16, s32, s64, etc.
        PerfFieldFormatHex,     // unsigned long, pointers
        PerfFieldFormatString,  // char, char[]
    };

    enum PerfFieldArray : uint8_t
    {
        PerfFieldArrayNone,     // e.g. "char val"
        PerfFieldArrayFixed,    // e.g. "char val[12]"
        PerfFieldArrayDynamic,  // e.g. "__data_loc char val[]", value = (len << 16) | offset.
        PerfFieldArrayRelDyn,   // e.g. "__rel_loc char val[]", value = (len << 16) | relativeOffset.
    };

    class PerfFieldMetadata
    {
        static constexpr std::string_view noname = std::string_view("noname", 6);

        std::string_view m_name;     // deduced from field, e.g. "my_field".
        std::string_view m_field;    // value of "field:" property, e.g. "char my_field[8]".
        uint16_t m_offset;           // value of "offset:" property.
        uint16_t m_size;             // value of "size:" property.
        uint16_t m_fixedArrayCount;  // deduced from field, size.
        PerfFieldElementSize m_elementSize; // deduced from field, size.
        PerfFieldFormat m_format;    // deduced from field, size, signed.
        PerfFieldArray m_array;      // deduced from field, size.

    public:

        // Parses a line of the "format:" section of an event's "format" file. The
        // formatLine string will generally look like
        // "[whitespace?]field:[declaration]; offset:[number]; size:[number]; ...".
        //
        // If "field:" is non-empty, "offset:" is a valid unsigned integer, and
        // "size:" is a valid unsigned integer, this returns
        // PerfFieldMetadata(field, offset, size, isSigned). Otherwise, this
        // returns PerfFieldMetadata().
        //
        // Stored strings will point into formatLine, so the formatLine string must
        // outlive this object.
        static PerfFieldMetadata
        Parse(
            bool longSize64, // true if sizeof(long) == 8, false if sizeof(long) == 4.
            std::string_view formatLine) noexcept;

        // Same as PerfFieldMetadata(false, {}, 0, 0)
        constexpr
        PerfFieldMetadata() noexcept
            : m_name(noname)
            , m_field()
            , m_offset()
            , m_size()
            , m_fixedArrayCount()
            , m_elementSize()
            , m_format()
            , m_array() {}

        // Initializes Field, Offset, and Size properties exactly as specified.
        // Deduces the other properties. The isSigned parameter should be -1 if the
        // "signed:" property is not present in the format line.
        PerfFieldMetadata(
            bool longSize64, // true if sizeof(long) == 8, false if sizeof(long) == 4.
            std::string_view field,
            uint16_t offset,
            uint16_t size,
            int8_t isSigned = -1) noexcept;

        // Returns the field name, e.g. "my_field". Never empty. (Deduced from
        // "field:".)
        constexpr std::string_view
        Name() const noexcept { return m_name; }

        // Returns the field declaration, e.g. "char my_field[8]".
        // (Parsed directly from "field:".)
        constexpr std::string_view
        Field() const noexcept { return m_field; }

        // Returns the byte offset of the start of the field data from the start of
        // the event raw data. (Parsed directly from "offset:".)
        constexpr uint16_t
        Offset() const noexcept { return m_offset; }

        // Returns the byte size of the field data. (Parsed directly from "size:".)
        constexpr uint16_t
        Size() const noexcept { return m_size; }

        // Returns the number of elements in this field. Meaningful only when
        // Array() == Fixed. (Deduced from "field:" and "size:".)
        constexpr uint16_t
        FixedArrayCount() const noexcept { return m_fixedArrayCount; }

        // Returns the size of each element in this field. (Deduced from "field:"
        // and "size:".)
        constexpr PerfFieldElementSize
        ElementSize() const noexcept { return m_elementSize; }

        // Returns the format of the field. (Deduced from "field:" and "signed:".)
        constexpr PerfFieldFormat
        Format() const noexcept { return m_format; }

        // Returns whether this is an array, and if so, how the array length should
        // be determined. (Deduced from "field:" and "size:".)
        constexpr PerfFieldArray
        Array() const noexcept { return m_array; }

        // Given the event's raw data (e.g. PerfSampleEventInfo::raw_data), return
        // this field's raw data. Returns empty for error (e.g. out of bounds).
        //
        // Does not do any byte-swapping. This method uses fileBigEndian to resolve
        // data_loc and rel_loc references, not to fix up the field data.
        // 
        // Note that in some cases, the size returned by GetFieldBytes may be
        // different from the value returned by Size():
        //
        // - If eventRawDataSize < Offset() + Size(), returns {}.
        // - If Size() == 0, returns all data from offset to the end of the event,
        //   i.e. it returns eventRawDataSize - Offset() bytes.
        // - If Array() is Dynamic or RelDyn, the returned size depends on the
        //   event contents.
        std::string_view
        GetFieldBytes(
            _In_reads_bytes_(eventRawDataSize) void const* eventRawData,
            uintptr_t eventRawDataSize,
            bool fileBigEndian) const noexcept;
    };

    enum class PerfEventKind : uint8_t
    {
        Normal,         // No special handling detected.
        EventHeader,    // First user field is named "eventheader_flags".
    };

    class PerfEventMetadata
    {
        std::string_view m_systemName;
        std::string_view m_formatFileContents;
        std::string_view m_name;
        std::string_view m_printFmt;
        std::vector<PerfFieldMetadata> m_fields;
        uint32_t m_id; // From common_type; not the same as the perf_event_attr::id or PerfSampleEventInfo::id.
        uint16_t m_commonFieldCount; // fields[common_field_count] is the first user field.
        uint16_t m_commonFieldsSize; // Offset of the end of the last common field
        PerfEventKind m_kind;

    public:

        ~PerfEventMetadata();
        PerfEventMetadata() noexcept;

        // Returns the value of the systemName parameter, e.g. "user_events".
        constexpr std::string_view
        SystemName() const noexcept { return m_systemName; }

        // Returns the value of the formatFileContents parameter, e.g.
        // "name: my_event\nID: 1234\nformat:...".
        constexpr std::string_view
        FormatFileContents() const noexcept { return m_formatFileContents; }

        // Returns the value of the "name:" property, e.g. "my_event".
        constexpr std::string_view
        Name() const noexcept { return m_name; }

        // Returns the value of the "print fmt:" property.
        constexpr std::string_view
        PrintFmt() const noexcept { return m_printFmt; }

        // Returns the fields from the "format:" property.
        constexpr std::vector<PerfFieldMetadata> const&
        Fields() const noexcept { return m_fields; }

        // Returns the value of the "ID:" property. Note that this value gets
        // matched against the "common_type" field of an event, not the id field
        // of perf_event_attr or PerfSampleEventInfo.
        constexpr uint32_t
        Id() const noexcept { return m_id; }

        // Returns the number of "common_*" fields at the start of the event.
        // User fields start at this index. At present, there are 4 common fields:
        // common_type, common_flags, common_preempt_count, common_pid.
        constexpr uint16_t
        CommonFieldCount() const noexcept { return m_commonFieldCount; }

        // Returns the offset of the end of the last "common_*" field.
        // This is the start of the first user field.
        constexpr uint16_t
        CommonFieldsSize() const noexcept { return m_commonFieldsSize; }

        // Returns the detected event decoding system - Normal or EventHeader.
        constexpr PerfEventKind
        Kind() const noexcept { return m_kind; }

        // Sets all properties of this object to {} values.
        void
        Clear() noexcept;

        // Parses an event's "format" file and sets the fields of this object based
        // on the results. Returns true if "ID:" is a valid unsigned integer and
        // "name:" is non-empty, returns true. Throws bad_alloc for out-of-memory.
        //
        // Stored strings will point into systemName and formatFileContents, so
        // those strings must outlive this object.
        bool
        Parse(
            bool longSize64, // true if sizeof(long) == 8, false if sizeof(long) == 4.
            std::string_view systemName,
            std::string_view formatFileContents) noexcept(false); // May throw bad_alloc.
    };
}
// namespace tracepoint_decode

#endif // _included_PerfEventMetadata_h
