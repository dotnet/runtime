// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#pragma once
#ifndef _included_EventEnumerator_h
#define _included_EventEnumerator_h 1

#include <eventheader/eventheader.h>
#include <stdint.h>
#include <errno.h>

#ifdef _WIN32
#include <sal.h>
#endif
#ifndef _In_reads_bytes_
#define _In_reads_bytes_(cb)
#endif
#ifndef _Out_
#define _Out_
#endif
#ifndef _Field_z_
#define _Field_z_
#endif
#ifndef _Field_size_bytes_
#define _Field_size_bytes_(cb)
#endif
#ifndef _Field_size_bytes_opt_
#define _Field_size_bytes_opt_(cb)
#endif

namespace eventheader_decode
{
    // Forward declarations:
    class EventEnumerator;
    enum EventEnumeratorState : uint8_t;
    enum EventEnumeratorError : uint8_t;
    struct EventInfo;
    struct EventItemInfo;
    struct EventDataPosition;

    /// <summary>
    /// Helper for decoding EventHeader events.
    /// </summary>
    class EventEnumerator
    {
        static event_field_encoding const EncodingCountMask = static_cast<event_field_encoding>(
            event_field_encoding_carray_flag | event_field_encoding_varray_flag);

        static event_field_encoding const ReadFieldError = event_field_encoding_invalid;

        /// <summary>
        /// Substate allows us to flatten
        /// "switch (state)    { case X: if (condition) ... }" to
        /// "switch (substate) { case X_condition: ... }"
        /// which potentially improves performance.
        /// </summary>
        enum SubState : uint8_t
        {
            SubState_None,
            SubState_Error,
            SubState_AfterLastItem,
            SubState_BeforeFirstItem,
            SubState_Value_Metadata,
            SubState_Value_Scalar,
            SubState_Value_SimpleArrayElement,
            SubState_Value_ComplexArrayElement,
            SubState_ArrayBegin,
            SubState_ArrayEnd,
            SubState_StructBegin,
            SubState_StructEnd,
        };

        struct StackEntry
        {
            uint16_t NextOffset; // m_metaBuf[NextOffset] starts next field's name.
            uint16_t NameOffset; // m_metaBuf[NameOffset] starts current field's name.
            uint16_t NameSize;   // m_metaBuf[NameOffset + NameSize + 1] starts current field's type.
            uint16_t ArrayIndex;
            uint16_t ArrayCount;
            uint8_t  RemainingFieldCount; // Number of NextProperty() calls before popping stack.
            uint8_t  ArrayFlags; // Encoding & EncodingCountMask
        };

        struct FieldType
        {
            event_field_encoding Encoding : 8;
            event_field_format   Format : 8;
            unsigned Tag : 16;
        };

    private:

        // Set up by StartEvent:
        eventheader m_header;
        uint64_t m_keyword;
        uint8_t const* m_metaBuf;
        uint8_t const* m_dataBuf;
        uint8_t const* m_activityIdBuf;
        char const* m_tracepointName; // Not nul-terminated.
        uint8_t m_tracepointNameLength;
        uint8_t m_providerNameLength; // Index into m_tracepointName
        uint8_t m_optionsIndex; // Index into m_tracepointName
        uint16_t m_metaEnd;
        uint8_t m_activityIdSize;
        bool m_needByteSwap;
        uint16_t m_eventNameSize; // Name starts at m_metaBuf
        uint32_t m_dataEnd;

        // Values change during enumeration:
        uint32_t m_dataPosRaw;
        uint32_t m_moveNextRemaining;
        StackEntry m_stackTop;
        uint8_t m_stackIndex; // Number of items currently on stack.
        EventEnumeratorState m_state;
        SubState m_subState;
        EventEnumeratorError m_lastError;

        uint8_t m_elementSize; // 0 if item is variable-size or complex.
        FieldType m_fieldType; // Note: fieldType.Encoding is cooked.
        uint32_t m_dataPosCooked;
        uint32_t m_itemSizeRaw;
        uint32_t m_itemSizeCooked;

        // Limit events to 8 levels of nested structures.
        StackEntry m_stack[8];

    public:

        static uint32_t const MoveNextLimitDefault = 4096;

        /// <summary>
        /// Initializes a new instance of EventEnumerator. Sets State to None.
        /// </summary>
        EventEnumerator() noexcept;

        /// <summary>
        /// Returns the current state.
        /// </summary>
        EventEnumeratorState
        State() const noexcept;

        /// <summary>
        /// Gets status for the most recent call to StartEvent, MoveNext, or MoveNextSibling.
        /// </summary>
        EventEnumeratorError
        LastError() const noexcept;

        /// <summary>
        /// Sets State to None.
        /// </summary>
        void
        Clear() noexcept;

        /// <summary>
        /// <para>
        /// Starts decoding the specified EventHeader event: decodes the header and
        /// positions the enumerator before the first item.
        /// </para><para>
        /// On success, changes the state to BeforeFirstItem and returns true.
        /// On failure, changes the state to None (not Error) and returns false.
        /// </para><para>
        /// Note that the enumerator stores the pchTracepointName and pData pointers but
        /// does not copy the referenced data, so the referenced data must remain valid
        /// and unchanged while you are processing the data with this enumerator (i.e.
        /// do not deallocate or overwrite the name or data until you call Clear, make
        /// another call to StartEvent, or destroy this EventEnumerator instance).
        /// </para>
        /// </summary>
        /// <param name="pchTracepointName">Set to tep_event->name, e.g. "MyProvider_L4K1".
        /// Must follow the tracepoint name rules described in eventheader.h.</param>
        /// <param name="cchTracepointName">Set to strlen(tep_event->name). Must be less
        /// than EVENTHEADER_NAME_MAX.</param>
        /// <param name="pData">Set to pointer to the start of the event data (the eventheader_flags
        /// field of the event header), usually something like
        /// tep_record->data + tep_event->format.fields[0].offset.</param>
        /// <param name="cbData">Set to size of the data, usually something like
        /// tep_record->size - tep_event->format.fields[0].offset.</param>
        /// <param name="moveNextLimit">Set to the maximum number of MoveNext calls to allow when
        /// processing this event (to guard against DoS attacks from a maliciously-crafted
        /// event).</param>
        /// <returns>Returns false for failure. Check LastError for details.</returns>
        bool
        StartEvent(
            _In_reads_bytes_(cchTracepointName) char const* pchTracepointName, // e.g. "MyProvider_L4K1"
            size_t cchTracepointName, // e.g. strlen(pchTracepointName)
            _In_reads_bytes_(cbData) void const* pData, // points at the eventheader_flags field
            size_t cbData, // size in bytes of the pData buffer
            uint32_t moveNextLimit = MoveNextLimitDefault) noexcept;

        /// <summary>
        /// <para>
        /// Positions the enumerator before the first item.
        /// </para><para>
        /// PRECONDITION: Can be called when State != None, i.e. at any time after a
        /// successful call to StartEvent, until a call to Clear.
        /// </para>
        /// </summary>
        void
        Reset(uint32_t moveNextLimit = MoveNextLimitDefault) noexcept;

        /// <summary>
        /// <para>
        /// Moves the enumerator to the next item in the current event, or to the end
        /// of the event if no more items. Returns true if moved to a valid item,
        /// false if no more items or decoding error.
        /// </para><para>
        /// PRECONDITION: Can be called when State >= BeforeFirstItem, i.e. after a
        /// successful call to StartEvent, until MoveNext returns false.
        /// </para><para>
        /// Typically called in a loop until it returns false, e.g.:
        /// </para><code>
        /// if (!e.StartEvent(...)) return e.LastError();
        /// while (e.MoveNext())
        /// {
        ///     EventItemInfo item = e.GetItemInfo();
        ///     switch (e.State())
        ///     {
        ///     case EventEnumeratorState_Value:
        ///         DoValue(item);
        ///         break;
        ///     case EventEnumeratorState_StructBegin:
        ///         DoStructBegin(item);
        ///         break;
        ///     case EventEnumeratorState_StructEnd:
        ///         DoStructEnd(item);
        ///         break;
        ///     case EventEnumeratorState_ArrayBegin:
        ///         DoArrayBegin(item);
        ///         break;
        ///     case EventEnumeratorState_ArrayEnd:
        ///         DoArrayEnd(item);
        ///         break;
        ///     }
        /// }
        /// return e.LastError();
        /// </code>
        /// </summary>
        /// <returns>
        /// Returns true if moved to a valid item.
        /// Returns false and sets state to AfterLastItem if no more items.
        /// Returns false and sets state to Error for decoding error.
        /// Check LastError for details.
        /// </returns>
        bool
        MoveNext() noexcept;

        /// <summary>
        /// <para>
        /// Moves the enumerator to the next sibling of the current item, or to the end
        /// of the event if no more items. Returns true if moved to a valid item, false
        /// if no more items or decoding error.
        /// </para><para>
        /// PRECONDITION: Can be called when State >= BeforeFirstItem, i.e. after a
        /// successful call to StartEvent, until MoveNext returns false.
        /// </para><list type="bullet"><item>
        /// If the current item is ArrayBegin or StructBegin, this efficiently moves
        /// enumeration to AFTER the corresponding ArrayEnd or StructEnd.
        /// </item><item>
        /// Otherwise, this is the same as MoveNext.
        /// </item></list><para>
        /// Typical use for this method is to efficiently skip past an array of fixed-size
        /// items (i.e. an array where ElementSize is nonzero) when you process all of the
        /// array items within the ArrayBegin state.
        /// </para><code>
        /// if (!e.StartEvent(...)) return e.LastError(); // Error.
        /// if (!e.MoveNext()) return e.LastError(); // AfterLastItem or Error.
        /// while (true)
        /// {
        ///     EventItemInfo item = e.GetItemInfo();
        ///     switch (e.State())
        ///     {
        ///     case EventEnumeratorState_Value:
        ///         DoValue(item);
        ///         break;
        ///     case EventEnumeratorState_StructBegin:
        ///         DoStructBegin(item);
        ///         break;
        ///     case EventEnumeratorState_StructEnd:
        ///         DoStructEnd(item);
        ///         break;
        ///     case EventEnumeratorState_ArrayBegin:
        ///         if (e.ElementSize == 0)
        ///         {
        ///             DoComplexArrayBegin(item);
        ///         }
        ///         else
        ///         {
        ///             // Process the entire array directly without using the enumerator.
        ///             DoSimpleArrayBegin(item);
        ///             for (unsigned i = 0; i != item.ArrayCount; i++)
        ///             {
        ///                 DoArrayElement(item, i);
        ///             }
        ///             DoSimpleArrayEnd(item);
        /// 
        ///             // Skip the entire array at once.
        ///             if (!e.MoveNextSibling()) // Instead of MoveNext().
        ///             {
        ///                 return e.LastError(); // AfterLastItem or Error.
        ///             }
        ///             continue; // Skip the MoveNext().
        ///         }
        ///         break;
        ///     case EventEnumeratorState_ArrayEnd:
        ///         DoComplexArrayEnd(item);
        ///         break;
        ///     }
        ///
        ///     if (!e.MoveNext())
        ///     {
        ///         return e.LastError(); // AfterLastItem or Error.
        ///     }
        /// }
        /// </code>
        /// </summary>
        /// <returns>
        /// Returns true if moved to a valid item.
        /// Returns false and sets state to AfterLastItem if no more items.
        /// Returns false and sets state to Error for decoding error.
        /// Check LastError for details.
        /// </returns>
        bool
        MoveNextSibling() noexcept;

        /// <summary>
        /// <para>
        /// Advanced scenarios. This method is for extracting type information from an
        /// event without looking at value information. Moves the enumerator to the next
        /// field declaration (not the next field value). Returns true if moved to a valid
        /// item, false if no more items or decoding error.
        /// </para><para>
        /// PRECONDITION: Can be called after a successful call to StartEvent, until
        /// MoveNextMetadata returns false.
        /// </para><para>
        /// Note that metadata enumeration gives a flat view of arrays and structures.
        /// There are only Value items, no BeginArray, EndArray, BeginStruct, EndStruct.
        /// A struct shows up as a value with Encoding = Struct (Format holds field count).
        /// An array shows up as a value with ArrayFlags != 0, and ArrayCount is either zero
        /// (indicating a runtime-variable array length) or nonzero (indicating a compile-time
        /// constant array length). An array of struct is a field with Encoding = Struct and
        /// ArrayFlags != 0. ValueBytes will always be empty. ArrayIndex and ElementSize
        /// will always be zero.
        /// </para><para>
        /// Note that when enumerating metadata for a structure, the enumeration may end before
        /// the expected number of fields are seen. This is a supported scenario and is not an
        /// error in the event. A large field count just means "this structure contains all the
        /// remaining fields in the event".
        /// </para><para>
        /// Typically called in a loop until it returns false.
        /// </para><code>
        /// if (!e.StartEvent(...)) return e.LastError();
        /// while (e.MoveNextMetadata())
        /// {
        ///     DoFieldDeclaration(e.GetItemInfo());
        /// }
        /// return e.LastError();
        /// </code>
        /// </summary>
        /// <returns>
        /// Returns true if moved to a valid item.
        /// Returns false and sets state to AfterLastItem if no more items.
        /// Returns false and sets state to Error for decoding error.
        /// Check LastError for details.
        /// </returns>
        bool
        MoveNextMetadata() noexcept;

        /// <summary>
        /// <para>
        /// Gets information that applies to the current event, e.g. the event name,
        /// provider name, options, level, keyword, etc.
        /// </para><para>
        /// PRECONDITION: Can be called when State != None, i.e. at any time after a
        /// successful call to StartEvent, until a call to Clear.
        /// </para>
        /// </summary>
        EventInfo
        GetEventInfo() const noexcept;

        /// <summary>
        /// <para>
        /// Gets information that applies to the current item, e.g. the item's name,
        /// the item's type (integer, string, float, etc.), data pointer, data size.
        /// The current item changes each time MoveNext() is called.
        /// </para><para>
        /// PRECONDITION: Can be called when State > BeforeFirstItem, i.e. after MoveNext
        /// returns true.
        /// </para>
        /// </summary>
        EventItemInfo
        GetItemInfo() const noexcept;

        /// <summary>
        /// <para>
        /// Gets the remaining event payload, i.e. the event data that has not yet
        /// been decoded. The data position can change each time MoveNext is called.
        /// </para><para>
        /// PRECONDITION: Can be called when State != None, i.e. at any time after a
        /// successful call to StartEvent, until a call to Clear.
        /// </para><para>
        /// This can be useful after enumeration has completed to to determine
        /// whether the event contains any trailing data (data not described by the
        /// decoding information). Up to 3 bytes of trailing data is normal (padding
        /// between events), but 4 or more bytes of trailing data might indicate some
        /// kind of encoding problem or data corruption.
        /// </para>
        /// </summary>
        EventDataPosition
        GetRawDataPosition() const noexcept;

    private:

        void
        ResetImpl(uint32_t moveNextLimit) noexcept;

        bool
        SkipStructMetadata() noexcept;

        bool
        NextProperty() noexcept;

        /// <summary>
        /// Requires m_metaEnd >= m_stackTop.NameOffset.
        /// Reads name, encoding, format, tag starting at m_stackTop.NameOffset.
        /// Updates m_stackTop.NameSize, m_stackTop.NextOffset.
        /// On failure, returns Encoding = None.
        /// </summary>
        FieldType
        ReadFieldNameAndType() noexcept;

        /// <summary>
        /// Requires m_metaEnd > typeOffset.
        /// Reads encoding, format, tag starting at m_stackTop.NameOffset.
        /// Updates m_stackTop.NextOffset.
        /// On failure, returns Encoding = None.
        /// </summary>
        FieldType
        ReadFieldType(uint16_t typeOffset) noexcept;

        bool
        StartArray() noexcept;

        void
        StartStruct() noexcept;

        bool
        StartValue() noexcept;

        void
        StartValueSimple() noexcept;

        template<class CH>
        void
        StartValueStringNul() noexcept;

        void
        StartValueStringLength16(uint8_t charSizeShift) noexcept;

        void
        SetState(EventEnumeratorState newState, SubState newSubState) noexcept;

        void
        SetEndState(EventEnumeratorState newState, SubState newSubState) noexcept;

        bool
        SetNoneState(EventEnumeratorError error) noexcept;

        bool
        SetErrorState(EventEnumeratorError error) noexcept;
    };

    /// <summary>
    /// Enumeration states.
    /// </summary>
    enum EventEnumeratorState : uint8_t
    {
        /// <summary>
        /// After construction, a call to Clear, or a failed StartEvent.
        /// </summary>
        EventEnumeratorState_None,

        /// <summary>
        /// After an error has been returned by MoveNext.
        /// </summary>
        EventEnumeratorState_Error,

        /// <summary>
        /// Positioned after the last item in the event.
        /// </summary>
        EventEnumeratorState_AfterLastItem,

        // MoveNext() is an invalid operation for all states above this line.
        // MoveNext() is a valid operation for all states below this line.

        /// <summary>
        /// Positioned before the first item in the event.
        /// </summary>
        EventEnumeratorState_BeforeFirstItem,

        // GetItemInfo() is an invalid operation for all states above this line.
        // GetItemInfo() is a valid operation for all states below this line.

        /// <summary>
        /// Positioned at an item with data (a field or an array element).
        /// </summary>
        EventEnumeratorState_Value,

        /// <summary>
        /// Positioned before the first item in an array.
        /// </summary>
        EventEnumeratorState_ArrayBegin,

        /// <summary>
        /// Positioned after the last item in an array.
        /// </summary>
        EventEnumeratorState_ArrayEnd,

        /// <summary>
        /// Positioned before the first item in a struct.
        /// </summary>
        EventEnumeratorState_StructBegin,

        /// <summary>
        /// Positioned after the last item in a struct.
        /// </summary>
        EventEnumeratorState_StructEnd,
    };

    /// <summary>
    /// Values for the LastError property.
    /// </summary>
    enum EventEnumeratorError : uint8_t
    {
        /// <summary>
        /// No error.
        /// </summary>
        EventEnumeratorError_Success = 0,

        /// <summary>
        /// Event is smaller than 8 bytes or larger than 2GB,
        /// or TracepointName is longer than 255 characters.
        /// </summary>
        EventEnumeratorError_InvalidParameter = EINVAL,

        /// <summary>
        /// Event does not follow the EventHeader naming/layout rules,
        /// is big-endian, has unrecognized flags, or unrecognized types.
        /// </summary>
        EventEnumeratorError_NotSupported = ENOTSUP,

        /// <summary>
        /// Resource usage limit (moveNextLimit) reached.
        /// </summary>
        EventEnumeratorError_ImplementationLimit = E2BIG,

        /// <summary>
        /// Event has an out-of-range value.
        /// </summary>
        EventEnumeratorError_InvalidData = EBADMSG,

        /// <summary>
        /// Event has more than 8 levels of nested structs.
        /// </summary>
        EventEnumeratorError_StackOverflow = EOVERFLOW,

        /// <summary>
        /// Method call invalid for current State().
        /// </summary>
        EventEnumeratorError_InvalidState = EPERM,
    };

    struct EventDataPosition
    {
        _Field_size_bytes_(Size) void const* Data;
        uint32_t Size;
    };

    struct EventInfo
    {
        // "EventName" followed by 0 or more event attributes.
        // Each attribute is ";AttribName=AttribValue".
        // EventName should not contain ';'.
        // AttribName should not contain ';' or '='.
        // AttribValue may contain ";;" which should be unescaped to ";".
        _Field_z_ char const* Name;

        // TracepointName, e.g. "ProviderName_LnKnnnOptions".
        // May not be nul-terminated. Length is TracepointNameLength.
        _Field_size_bytes_(TracepointNameLength) char const* TracepointName;

        // 128-bit big-endian activity id, or NULL if none.
        _Field_size_bytes_opt_(16) uint8_t const* ActivityId;

        // 128-bit big-endian related activity id, or NULL if none.
        _Field_size_bytes_opt_(16) uint8_t const* RelatedActivityId;

        // flags, version, id, tag, opcode, level.
        eventheader Header;

        // Event category bits.
        uint64_t Keyword;

        // Length of TracepointName.
        uint8_t TracepointNameLength;

        // Length of the ProviderName part of TracepointName, e.g. if
        // TracepointName is "ProviderName_LnKnnnOptions", this will be 12
        // since strlen("ProviderName") = 12.
        uint8_t ProviderNameLength;

        // Index to the Options part of TracepointName, i.e. the part of the
        // TracepointName after level and keyword, e.g. if TracepointName is
        // "ProviderName_LnKnnnOptions", this will be 19.
        uint8_t OptionsIndex;
    };

    struct EventItemInfo
    {
        // "FieldName" followed by 0 or more field attributes.
        // Each attribute is ";AttribName=AttribValue".
        // FieldName should not contain ';'.
        // AttribName should not contain ';' or '='.
        // AttribValue may contain ";;" which should be unescaped to ";".
        _Field_z_ char const* Name;

        // Raw field value bytes.
        // May need byte-swap (check e.NeedByteSwap() or eventInfo.header.flags).
        // For strings, does not include length prefix or nul-termination.
        _Field_size_bytes_(ValueSize) void const* ValueData;

        // Raw field value size (in bytes).
        // This is nonzero for Value items and for ArrayBegin of array of simple values.
        // This is zero for everything else, including ArrayBegin of array of complex items.
        // For strings, does not include length prefix or nul-termination.
        uint32_t ValueSize;

        // Array element index.
        // For non-array, this is 0.
        // For ArrayBegin, this is 0.
        // For ArrayEnd, this is ArrayCount.
        uint16_t ArrayIndex;

        // Array element count. For non-array, this is 1.
        uint16_t ArrayCount;

        // Nonzero for simple items (fixed-size non-struct).
        // Zero for complex items (variable-size or struct).
        uint32_t ElementSize : 8;

        // Field's underlying encoding. The encoding indicates how to determine the field's
        // size and the semantic type to use when Format = Default.
        event_field_encoding Encoding : 8;

        // Field's semantic type. May be Default, in which case the semantic type should be
        // determined based on the default format for the field's encoding.
        // For StructBegin/StructEnd, this contains the struct field count.
        event_field_format Format : 8;

        // 0 if item is a non-array Value.
        // event_field_encoding_carray_flag if item is a fixed-length ArrayBegin, ArrayEnd, or array Value.
        // event_field_encoding_varray_flag if item is a variable-length ArrayBegin, ArrayEnd, or array Value.
        event_field_encoding ArrayFlags : 8;

        // True if event's byte order != host byte order.
        bool NeedByteSwap;

        // Field tag, or 0 if none.
        uint16_t FieldTag;
    };
}
// namespace eventheader_decode

#endif // _included_EventEnumerator_h
