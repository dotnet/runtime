// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#include <eventheader/EventEnumerator.h>
#include <assert.h>
#include <errno.h>
#include <stdlib.h>
#include <string.h>
#include <stdio.h>

#ifdef _WIN32

#include <windows.h> // UNALIGNED
#define bswap_16(u16) _byteswap_ushort(u16)

#else // _WIN32

#include <byteswap.h>
#define UNALIGNED __attribute__((aligned(1)))

#endif // _WIN32

using namespace eventheader_decode;

template<class UINTnn>
static char const*
LowercaseHexToInt(char const* pch, char const* pchEnd, UINTnn* pVal) noexcept
{
    UINTnn val = 0;
    for (; pch != pchEnd; pch += 1)
    {
        char const ch = *pch;
        uint8_t nibble;
        if (ch >= '0' && ch <= '9')
        {
            nibble = static_cast<uint8_t>(ch - '0');
        }
        else if (ch >= 'a' && ch <= 'f')
        {
            nibble = static_cast<uint8_t>(ch - 'a' + 10);
        }
        else
        {
            break;
        }

        val = static_cast<UINTnn>((val << 4) + nibble);
    }

    *pVal = val;
    return pch;
}

EventEnumerator::EventEnumerator() noexcept
    : m_state(EventEnumeratorState_None)
    , m_subState(SubState_None)
    , m_lastError(EventEnumeratorError_Success)
{
    static_assert(sizeof(FieldType) == 4, "Bad FieldType size");
    return;
}

EventEnumeratorState
EventEnumerator::State() const noexcept
{
    return m_state;
}

EventEnumeratorError
EventEnumerator::LastError() const noexcept
{
    return m_lastError;
}

void
EventEnumerator::Clear() noexcept
{
    SetNoneState(EventEnumeratorError_Success);
}

bool
EventEnumerator::StartEvent(
    _In_reads_bytes_(cchTracepointName) char const* pchTracepointName,
    size_t cchTracepointName,
    _In_reads_bytes_(cbData) void const* pData,
    size_t cbData,
    uint32_t moveNextLimit) noexcept
{
    static auto constexpr HostEndianFlag =
        EVENTHEADER_LITTLE_ENDIAN
        ? eventheader_flag_little_endian
        : eventheader_flag_none;

    static auto constexpr KnownFlags = static_cast<eventheader_flags>(
        eventheader_flag_pointer64 | eventheader_flag_little_endian | eventheader_flag_extension);

    auto const eventBuf = static_cast<uint8_t const*>(pData);
    uint32_t eventPos = 0;
    uint32_t const eventEnd = static_cast<uint32_t>(cbData);

    if (cbData < sizeof(eventheader) || cbData > 0x7FFFFFFF)
    {
        // Not a supported event: size < 8 or size >= 2GB.
        return SetNoneState(EventEnumeratorError_InvalidParameter);
    }

    // Get event header and validate it.

    memcpy(&m_header, &eventBuf[eventPos], sizeof(eventheader));
    eventPos += sizeof(eventheader);

    if (m_header.flags != (m_header.flags & KnownFlags))
    {
        // Not a supported event: unsupported flags.
        return SetNoneState(EventEnumeratorError_NotSupported);
    }

    m_needByteSwap = HostEndianFlag != (m_header.flags & eventheader_flag_little_endian);
    if (m_needByteSwap)
    {
        m_header.id = bswap_16(m_header.id);
        m_header.tag = bswap_16(m_header.tag);
    }

    // Validate Tracepoint name (e.g. "ProviderName_L1K2..."), extract keyword.

    if (cchTracepointName >= EVENTHEADER_NAME_MAX)
    {
        // Not a supported event: name too long.
        return SetNoneState(EventEnumeratorError_NotSupported);
    }

    if (memchr(pchTracepointName, 0, cchTracepointName))
    {
        // Not a supported event: name contains NUL character.
        return SetNoneState(EventEnumeratorError_NotSupported);
    }

    m_tracepointName = pchTracepointName;
    m_tracepointNameLength = static_cast<uint8_t>(cchTracepointName);

    auto const pAttribEnd = pchTracepointName + cchTracepointName;
    auto pAttrib = pAttribEnd;

    // Find the last underscore in pchTracepointName.
    for (;;)
    {
        if (pAttrib == pchTracepointName)
        {
            // Not a supported event: no underscore in name.
            return SetNoneState(EventEnumeratorError_NotSupported);
        }

        pAttrib -= 1;
        if (*pAttrib == '_')
        {
            break;
        }
    }

    // Provider name is the part of the tracepoint name before the last underscore.
    m_providerNameLength = static_cast<uint8_t>(pAttrib - pchTracepointName);

    pAttrib += 1; // Attribs start after the underscore.

    if (pAttrib == pAttribEnd || 'L' != *pAttrib)
    {
        // Not a supported event: no level in name.
        return SetNoneState(EventEnumeratorError_NotSupported);
    }

    uint8_t attribLevel;
    pAttrib = LowercaseHexToInt(pAttrib + 1, pAttribEnd, &attribLevel);
    if (attribLevel != m_header.level)
    {
        // Not a supported event: name's level != header's level.
        return SetNoneState(EventEnumeratorError_NotSupported);
    }

    if (pAttrib == pAttribEnd || 'K' != *pAttrib)
    {
        // Not a supported event: no keyword in name.
        return SetNoneState(EventEnumeratorError_NotSupported);
    }

    pAttrib = LowercaseHexToInt(pAttrib + 1, pAttribEnd, &m_keyword);

    // Options start after the keyword attribute.
    m_optionsIndex = static_cast<uint8_t>(pAttrib - pchTracepointName);

    // Validate but ignore any other attributes.

    while (pAttrib != pAttribEnd)
    {
        char ch;
        ch = *pAttrib;
        if (ch < 'A' || 'Z' < ch)
        {
            // Invalid attribute start character.
            return SetNoneState(EventEnumeratorError_NotSupported);
        }

        // Skip attribute value chars.
        for (pAttrib += 1; pAttrib != pAttribEnd; pAttrib += 1)
        {
            ch = *pAttrib;
            if ((ch < '0' || '9' < ch) && (ch < 'a' || 'z' < ch))
            {
                break;
            }
        }
    }

    // Parse header extensions.

    m_metaBuf = nullptr;
    m_metaEnd = 0;
    m_activityIdBuf = nullptr;
    m_activityIdSize = 0;

    if (0 != (m_header.flags & eventheader_flag_extension))
    {
        eventheader_extension ext;
        do
        {
            if (eventEnd - eventPos < sizeof(eventheader_extension))
            {
                return SetNoneState(EventEnumeratorError_InvalidData);
            }

            memcpy(&ext, eventBuf + eventPos, sizeof(eventheader_extension));
            eventPos += sizeof(eventheader_extension);

            if (m_needByteSwap)
            {
                ext.size = bswap_16(ext.size);
                ext.kind = bswap_16(ext.kind);
            }

            if (eventEnd - eventPos < ext.size)
            {
                return SetNoneState(EventEnumeratorError_InvalidData);
            }

            switch (ext.kind & eventheader_extension_kind_value_mask)
            {
            case eventheader_extension_kind_invalid:
                return SetNoneState(EventEnumeratorError_InvalidData);

            case eventheader_extension_kind_metadata:
                if (m_metaBuf != nullptr)
                {
                    // Multiple Metadata extensions.
                    return SetNoneState(EventEnumeratorError_InvalidData);
                }

                m_metaBuf = &eventBuf[eventPos];
                m_metaEnd = ext.size;
                break;

            case eventheader_extension_kind_activity_id:
                if (m_activityIdBuf != nullptr ||
                    (ext.size != 16 && ext.size != 32))
                {
                    // Multiple ActivityId extensions, or bad activity id size.
                    return SetNoneState(EventEnumeratorError_InvalidData);
                }

                m_activityIdBuf = &eventBuf[eventPos];
                m_activityIdSize = static_cast<uint8_t>(ext.size);
                break;

            default:
                break; // Ignore other extension types.
            }

            eventPos += ext.size;
        } while (0 != (ext.kind & eventheader_extension_kind_chain_flag));
    }

    if (m_metaBuf == nullptr)
    {
        // Not a supported event - no metadata extension.
        return SetNoneState(EventEnumeratorError_NotSupported);
    }

    m_eventNameSize = static_cast<uint16_t>(
        strnlen(reinterpret_cast<char const*>(m_metaBuf), m_metaEnd));
    if (m_eventNameSize == m_metaEnd)
    {
        // Event name not nul-terminated.
        return SetNoneState(EventEnumeratorError_InvalidData);
    }

    m_dataBuf = &eventBuf[eventPos];
    m_dataEnd = eventEnd - eventPos;

    ResetImpl(moveNextLimit);
    return true;
}

void
EventEnumerator::Reset(uint32_t moveNextLimit) noexcept
{
    assert(m_state != EventEnumeratorState_None); // PRECONDITION

    if (m_state == EventEnumeratorState_None)
    {
        m_lastError = EventEnumeratorError_InvalidState;
    }
    else
    {
        ResetImpl(moveNextLimit);
    }
}

bool
EventEnumerator::MoveNext() noexcept
{
    assert(m_state >= EventEnumeratorState_BeforeFirstItem); // PRECONDITION

    if (m_moveNextRemaining == 0)
    {
        return SetErrorState(EventEnumeratorError_ImplementationLimit);
    }

    m_moveNextRemaining -= 1;

    bool movedToItem;

    switch (m_subState)
    {
    default:

        assert(!"Unexpected substate.");
        m_lastError = EventEnumeratorError_InvalidState;
        movedToItem = false;
        break;

    case SubState_BeforeFirstItem:

        assert(m_state == EventEnumeratorState_BeforeFirstItem);
        movedToItem = NextProperty();
        break;

    case SubState_Value_Metadata:

        m_lastError = EventEnumeratorError_InvalidState;
        movedToItem = false;
        break;

    case SubState_Value_Scalar:

        assert(m_state == EventEnumeratorState_Value);
        assert(m_fieldType.Encoding != event_field_encoding_struct);
        assert(!m_stackTop.ArrayFlags);
        assert(m_dataEnd - m_dataPosRaw >= m_itemSizeRaw);

        m_dataPosRaw += m_itemSizeRaw;
        movedToItem = NextProperty();
        break;

    case SubState_Value_SimpleArrayElement:

        assert(m_state == EventEnumeratorState_Value);
        assert(m_fieldType.Encoding != event_field_encoding_struct);
        assert(m_stackTop.ArrayFlags);
        assert(m_stackTop.ArrayIndex < m_stackTop.ArrayCount);
        assert(m_elementSize != 0); // Eligible for fast path.
        assert(m_dataEnd - m_dataPosRaw >= m_itemSizeRaw);

        m_dataPosRaw += m_itemSizeRaw;
        m_stackTop.ArrayIndex += 1;

        if (m_stackTop.ArrayCount == m_stackTop.ArrayIndex)
        {
            // End of array.
            SetEndState(EventEnumeratorState_ArrayEnd, SubState_ArrayEnd);
        }
        else
        {
            // Middle of array - get next element.
            StartValueSimple(); // Fast path for simple array elements.
        }

        movedToItem = true;
        break;

    case SubState_Value_ComplexArrayElement:

        assert(m_state == EventEnumeratorState_Value);
        assert(m_fieldType.Encoding != event_field_encoding_struct);
        assert(m_stackTop.ArrayFlags);
        assert(m_stackTop.ArrayIndex < m_stackTop.ArrayCount);
        assert(m_elementSize == 0); // Not eligible for fast path.
        assert(m_dataEnd - m_dataPosRaw >= m_itemSizeRaw);

        m_dataPosRaw += m_itemSizeRaw;
        m_stackTop.ArrayIndex += 1;

        if (m_stackTop.ArrayCount == m_stackTop.ArrayIndex)
        {
            // End of array.
            SetEndState(EventEnumeratorState_ArrayEnd, SubState_ArrayEnd);
            movedToItem = true;
        }
        else
        {
            // Middle of array - get next element.
            movedToItem = StartValue(); // Normal path for complex array elements.
        }

        break;

    case SubState_ArrayBegin:

        assert(m_state == EventEnumeratorState_ArrayBegin);
        assert(m_stackTop.ArrayFlags);
        assert(m_stackTop.ArrayIndex == 0);

        if (m_stackTop.ArrayCount == 0)
        {
            // 0-length array.
            SetEndState(EventEnumeratorState_ArrayEnd, SubState_ArrayEnd);
            movedToItem = true;
        }
        else if (m_elementSize != 0)
        {
            // First element of simple array.
            assert(m_fieldType.Encoding != event_field_encoding_struct);
            m_itemSizeCooked = m_elementSize;
            m_itemSizeRaw = m_elementSize;
            SetState(EventEnumeratorState_Value, SubState_Value_SimpleArrayElement);
            StartValueSimple();
            movedToItem = true;
        }
        else if (m_fieldType.Encoding != event_field_encoding_struct)
        {
            // First element of complex array.
            SetState(EventEnumeratorState_Value, SubState_Value_ComplexArrayElement);
            movedToItem = StartValue();
        }
        else
        {
            // First element of array of struct.
            StartStruct();
            movedToItem = true;
        }

        break;

    case SubState_ArrayEnd:

        assert(m_state == EventEnumeratorState_ArrayEnd);
        assert(m_stackTop.ArrayFlags);
        assert(m_stackTop.ArrayCount == m_stackTop.ArrayIndex);

        // 0-length array of struct means we won't naturally traverse
        // the child struct's metadata. Since m_stackTop.NextOffset
        // won't get updated naturally, we need to update it manually.
        if (m_fieldType.Encoding == event_field_encoding_struct &&
            m_stackTop.ArrayCount == 0 &&
            !SkipStructMetadata())
        {
            movedToItem = false;
        }
        else
        {
            movedToItem = NextProperty();
        }

        break;

    case SubState_StructBegin:

        assert(m_state == EventEnumeratorState_StructBegin);
        if (m_stackIndex == sizeof(m_stack) / sizeof(m_stack[0]))
        {
            movedToItem = SetErrorState(EventEnumeratorError_StackOverflow);
        }
        else
        {
            m_stack[m_stackIndex] = m_stackTop;
            m_stackIndex += 1;

            m_stackTop.RemainingFieldCount = m_fieldType.Format;
            // Parent's NextOffset is the correct starting point for the struct.
            movedToItem = NextProperty();
        }

        break;

    case SubState_StructEnd:

        assert(m_state == EventEnumeratorState_StructEnd);
        assert(m_fieldType.Encoding == event_field_encoding_struct);
        assert(m_itemSizeRaw == 0);

        m_stackTop.ArrayIndex += 1;

        if (m_stackTop.ArrayCount != m_stackTop.ArrayIndex)
        {
            assert(m_stackTop.ArrayFlags);
            assert(m_stackTop.ArrayIndex < m_stackTop.ArrayCount);

            // Middle of array - get next element.
            StartStruct();
            movedToItem = true;
        }
        else if (m_stackTop.ArrayFlags)
        {
            // End of array.
            SetEndState(EventEnumeratorState_ArrayEnd, SubState_ArrayEnd);
            movedToItem = true;
        }
        else
        {
            // End of property - move to next property.
            movedToItem = NextProperty();
        }

        break;
    }

    return movedToItem;
}

bool
EventEnumerator::MoveNextSibling() noexcept
{
    assert(m_state >= EventEnumeratorState_BeforeFirstItem); // PRECONDITION

    bool movedToItem;
    int depth = 0; // May reach -1 if we start on ArrayEnd/StructEnd.
    do
    {
        switch (m_state)
        {
        default:
            // Same as MoveNext.
            break;

        case EventEnumeratorState_ArrayEnd:
        case EventEnumeratorState_StructEnd:
            depth -= 1;
            break;

        case EventEnumeratorState_StructBegin:
            depth += 1;
            break;

        case EventEnumeratorState_ArrayBegin:
            if (m_elementSize == 0 || m_moveNextRemaining == 0)
            {
                // Use MoveNext for full processing.
                depth += 1;
                break;
            }
            else
            {
                // Array of simple elements - jump directly to next sibling.
                assert(m_subState == SubState_ArrayBegin);
                assert(m_fieldType.Encoding != event_field_encoding_struct);
                assert(m_stackTop.ArrayFlags);
                assert(m_stackTop.ArrayIndex == 0);
                m_dataPosRaw += static_cast<uint32_t>(m_stackTop.ArrayCount) * m_elementSize;
                m_moveNextRemaining -= 1;
                movedToItem = NextProperty();
                continue; // Skip MoveNext().
            }
        }

        movedToItem = MoveNext();
    } while (movedToItem && depth > 0);

    return movedToItem;
}

bool
EventEnumerator::MoveNextMetadata() noexcept
{
    if (m_subState != SubState_Value_Metadata)
    {
        assert(m_state == EventEnumeratorState_BeforeFirstItem); // PRECONDITION

        assert(m_subState == SubState_BeforeFirstItem);
        m_stackTop.ArrayIndex = 0;
        m_dataPosCooked = m_dataEnd;
        m_itemSizeCooked = 0;
        m_elementSize = 0;
        SetState(EventEnumeratorState_Value, SubState_Value_Metadata);
    }

    assert(m_state == EventEnumeratorState_Value);

    bool movedToItem;
    if (m_stackTop.NextOffset != m_metaEnd)
    {
        m_stackTop.NameOffset = m_stackTop.NextOffset;

        m_fieldType = ReadFieldNameAndType();
        if (m_fieldType.Encoding == ReadFieldError)
        {
            movedToItem = SetErrorState(EventEnumeratorError_InvalidData);
        }
        else if (
            event_field_encoding_struct == (m_fieldType.Encoding & event_field_encoding_value_mask) &&
            m_fieldType.Format == 0)
        {
            // Struct must have at least 1 field (potential for DoS).
            movedToItem = SetErrorState(EventEnumeratorError_InvalidData);
        }
        else if (0 == (m_fieldType.Encoding & EncodingCountMask))
        {
            // Non-array.

            m_stackTop.ArrayCount = 1;
            m_stackTop.ArrayFlags = 0;
            movedToItem = true;
        }
        else if (event_field_encoding_varray_flag == (m_fieldType.Encoding & EncodingCountMask))
        {
            // Runtime-variable array length.

            m_fieldType.Encoding = static_cast<event_field_encoding>(m_fieldType.Encoding & event_field_encoding_value_mask);
            m_stackTop.ArrayCount = 0;
            m_stackTop.ArrayFlags = event_field_encoding_varray_flag;
            movedToItem = true;
        }
        else if (event_field_encoding_carray_flag == (m_fieldType.Encoding & EncodingCountMask))
        {
            // Compile-time-constant array length.

            if (m_metaEnd - m_stackTop.NextOffset < 2)
            {
                movedToItem = SetErrorState(EventEnumeratorError_InvalidData);
            }
            else
            {
                m_stackTop.ArrayCount = *reinterpret_cast<uint16_t const UNALIGNED*>(m_metaBuf + m_stackTop.NextOffset);
                m_stackTop.ArrayFlags = event_field_encoding_carray_flag;
                m_fieldType.Encoding = static_cast<event_field_encoding>(m_fieldType.Encoding & event_field_encoding_value_mask);
                m_stackTop.NextOffset += 2;

                if (m_needByteSwap)
                {
                    m_stackTop.ArrayCount = bswap_16(m_stackTop.ArrayCount);
                }

                if (m_stackTop.ArrayCount == 0)
                {
                    // Constant-length array cannot have length of 0 (potential for DoS).
                    movedToItem = SetErrorState(EventEnumeratorError_InvalidData);
                }
                else
                {
                    movedToItem = true;
                }
            }
        }
        else
        {
            movedToItem = SetErrorState(EventEnumeratorError_NotSupported);
        }
    }
    else
    {
        // End of event.

        SetEndState(EventEnumeratorState_AfterLastItem, SubState_AfterLastItem);
        movedToItem = false; // No more items.
    }

    return movedToItem;
}

EventInfo
EventEnumerator::GetEventInfo() const noexcept
{
    assert(m_state != EventEnumeratorState_None); // PRECONDITION

    EventInfo value;
    value.Name = reinterpret_cast<char const*>(m_metaBuf);
    value.TracepointName = m_tracepointName;
    value.ActivityId = m_activityIdBuf;
    value.RelatedActivityId = m_activityIdSize >= 32
        ? m_activityIdBuf + 16
        : nullptr;
    value.Header = m_header;
    value.Keyword = m_keyword;
    value.TracepointNameLength = m_tracepointNameLength;
    value.ProviderNameLength = m_providerNameLength;
    value.OptionsIndex = m_optionsIndex;
    return value;
}

EventItemInfo
EventEnumerator::GetItemInfo() const noexcept
{
    assert(m_state > EventEnumeratorState_BeforeFirstItem); // PRECONDITION

    EventItemInfo value;
    value.Name = reinterpret_cast<char const*>(m_metaBuf + m_stackTop.NameOffset);
    value.ValueData = m_dataBuf + m_dataPosCooked;
    value.ValueSize = m_itemSizeCooked;
    value.ArrayIndex = m_stackTop.ArrayIndex;
    value.ArrayCount = m_stackTop.ArrayCount;
    value.ElementSize = m_elementSize;
    value.Encoding = m_fieldType.Encoding;
    value.Format = m_fieldType.Format;
    value.NeedByteSwap = m_needByteSwap;
    value.ArrayFlags = static_cast<event_field_encoding>(m_stackTop.ArrayFlags);
    value.FieldTag = m_fieldType.Tag;
    return value;
}

EventDataPosition
EventEnumerator::GetRawDataPosition() const noexcept
{
    assert(m_state != EventEnumeratorState_None); // PRECONDITION

    EventDataPosition value;
    value.Data = m_dataBuf + m_dataPosRaw;
    value.Size = m_dataEnd - m_dataPosRaw;
    return value;
}

void
EventEnumerator::ResetImpl(uint32_t moveNextLimit) noexcept
{
    m_dataPosRaw = 0;
    m_moveNextRemaining = moveNextLimit;
    m_stackTop.NextOffset = m_eventNameSize + 1;
    m_stackTop.RemainingFieldCount = 255; // Go until we reach end of metadata.
    m_stackIndex = 0;
    SetState(EventEnumeratorState_BeforeFirstItem, SubState_BeforeFirstItem);
    m_lastError = EventEnumeratorError_Success;
}

/*
Sets m_stackTop.NextOffset to skip the struct's fields.
*/
bool
EventEnumerator::SkipStructMetadata() noexcept
{
    assert(m_fieldType.Encoding == event_field_encoding_struct);

    bool ok;
    for (uint32_t remainingFieldCount = m_fieldType.Format;;
        remainingFieldCount -= 1)
    {
        // It's a bit unusual but completely legal and fully supported to reach
        // end-of-metadata before remainingFieldCount == 0.
        if (remainingFieldCount == 0 || m_stackTop.NextOffset == m_metaEnd)
        {
            ok = true;
            break;
        }

        m_stackTop.NameOffset = m_stackTop.NextOffset;

        // Minimal validation, then skip the field:

        auto type = ReadFieldNameAndType();
        if (type.Encoding == ReadFieldError)
        {
            ok = SetErrorState(EventEnumeratorError_InvalidData);
            break;
        }

        if (event_field_encoding_struct == (type.Encoding & event_field_encoding_value_mask))
        {
            remainingFieldCount += type.Format;
        }

        if (0 == (type.Encoding & event_field_encoding_carray_flag))
        {
            // Scalar or runtime length. We're done with the field.
        }
        else if (event_field_encoding_carray_flag == (type.Encoding & EncodingCountMask))
        {
            // FlagCArray is set, FlagVArray is unset.
            // Compile-time-constant array length.
            // Skip the array length in metadata.

            if (m_metaEnd - m_stackTop.NextOffset < 2)
            {
                ok = SetErrorState(EventEnumeratorError_InvalidData);
                break;
            }

            m_stackTop.NextOffset += 2;
        }
        else
        {
            // Both FlagCArray and FlagVArray are set (reserved encoding).
            ok = SetErrorState(EventEnumeratorError_NotSupported);
            break;
        }
    }

    return ok;
}

/*
Inputs: m_stackTop.RemainingFieldCount, m_stackTop.NextOffset, m_stackIndex, m_dataPosRaw.
*/
bool
EventEnumerator::NextProperty() noexcept
{
    bool movedToItem;
    if (m_stackTop.RemainingFieldCount != 0 &&
        m_stackTop.NextOffset != m_metaEnd)
    {
        m_stackTop.RemainingFieldCount -= 1;
        m_stackTop.ArrayIndex = 0;
        m_stackTop.NameOffset = m_stackTop.NextOffset;

        // Decode a field:

        m_fieldType = ReadFieldNameAndType();
        if (m_fieldType.Encoding == ReadFieldError)
        {
            movedToItem = SetErrorState(EventEnumeratorError_InvalidData);
        }
        else if (0 == (m_fieldType.Encoding & EncodingCountMask))
        {
            // Non-array.

            m_stackTop.ArrayCount = 1;
            m_stackTop.ArrayFlags = 0;
            if (event_field_encoding_struct != (m_fieldType.Encoding & event_field_encoding_value_mask))
            {
                SetState(EventEnumeratorState_Value, SubState_Value_Scalar);
                movedToItem = StartValue();
            }
            else if (m_fieldType.Format == 0)
            {
                // Struct must have at least 1 field (potential for DoS).
                movedToItem = SetErrorState(EventEnumeratorError_InvalidData);
            }
            else
            {
                StartStruct();
                movedToItem = true;
            }
        }
        else if (event_field_encoding_varray_flag == (m_fieldType.Encoding & EncodingCountMask))
        {
            // Runtime-variable array length.

            if (m_dataEnd - m_dataPosRaw < 2)
            {
                movedToItem = SetErrorState(EventEnumeratorError_InvalidData);
            }
            else
            {
                m_stackTop.ArrayCount = *reinterpret_cast<uint16_t const UNALIGNED*>(m_dataBuf + m_dataPosRaw);
                m_dataPosRaw += 2;

                if (m_needByteSwap)
                {
                    m_stackTop.ArrayCount = bswap_16(m_stackTop.ArrayCount);
                }

                movedToItem = StartArray(); // StartArray will set flags.
            }
        }
        else if (event_field_encoding_carray_flag == (m_fieldType.Encoding & EncodingCountMask))
        {
            // Compile-time-constant array length.

            if (m_metaEnd - m_stackTop.NextOffset < 2)
            {
                movedToItem = SetErrorState(EventEnumeratorError_InvalidData);
            }
            else
            {
                m_stackTop.ArrayCount = *reinterpret_cast<uint16_t const UNALIGNED*>(m_metaBuf + m_stackTop.NextOffset);
                m_stackTop.NextOffset += 2;

                if (m_needByteSwap)
                {
                    m_stackTop.ArrayCount = bswap_16(m_stackTop.ArrayCount);
                }

                if (m_stackTop.ArrayCount == 0)
                {
                    // Constant-length array cannot have length of 0 (potential for DoS).
                    movedToItem = SetErrorState(EventEnumeratorError_InvalidData);
                }
                else
                {
                    movedToItem = StartArray(); // StartArray will set flags.
                }
            }
        }
        else
        {
            movedToItem = SetErrorState(EventEnumeratorError_NotSupported);
        }
    }
    else if (m_stackIndex != 0)
    {
        // End of struct.
        // It's a bit unusual but completely legal and fully supported to reach
        // end-of-metadata before RemainingFieldCount == 0.

        // Pop child from stack.
        m_stackIndex -= 1;
        auto const childMetadataOffset = m_stackTop.NextOffset;
        m_stackTop = m_stack[m_stackIndex];

        m_fieldType = ReadFieldType(
            static_cast<uint16_t>(m_stackTop.NameOffset + m_stackTop.NameSize + 1u));
        assert(event_field_encoding_struct == (m_fieldType.Encoding & event_field_encoding_value_mask));
        m_fieldType.Encoding = event_field_encoding_struct; // Mask off array flags.
        m_elementSize = 0;

        // Unless parent is in the middle of an array, we need to set the
        // "next field" position to the child's metadata position.
        assert(m_stackTop.ArrayIndex < m_stackTop.ArrayCount);
        if (m_stackTop.ArrayIndex + 1 == m_stackTop.ArrayCount)
        {
            m_stackTop.NextOffset = childMetadataOffset;
        }

        SetEndState(EventEnumeratorState_StructEnd, SubState_StructEnd);
        movedToItem = true;
    }
    else
    {
        // End of event.

        if (m_stackTop.NextOffset != m_metaEnd)
        {
            // Event has metadata for more than MaxTopLevelProperties.
            movedToItem = SetErrorState(EventEnumeratorError_NotSupported);
        }
        else
        {
            SetEndState(EventEnumeratorState_AfterLastItem, SubState_AfterLastItem);
            movedToItem = false; // No more items.
        }
    }

    return movedToItem;
}

EventEnumerator::FieldType
EventEnumerator::ReadFieldNameAndType() noexcept
{
    uint16_t pos = m_stackTop.NameOffset;
    assert(m_metaEnd >= pos);

    auto const fieldName = reinterpret_cast<char const*>(m_metaBuf + pos);
    pos += static_cast<uint16_t>(strnlen(fieldName, m_metaEnd - pos));

    if (m_metaEnd - pos < 2)
    {
        // Missing nul termination or missing encoding.
        return { ReadFieldError, event_field_format_default, 0 };
    }
    else
    {
        m_stackTop.NameSize = pos - m_stackTop.NameOffset;
        return ReadFieldType(pos + 1);
    }
}

EventEnumerator::FieldType
EventEnumerator::ReadFieldType(uint16_t typeOffset) noexcept
{
    uint16_t pos = typeOffset;
    assert(m_metaEnd > pos);

    auto encoding = static_cast<event_field_encoding>(m_metaBuf[pos]);
    pos += 1;

    auto format = event_field_format_default;
    uint16_t tag = 0;
    if (0 != (encoding & event_field_encoding_chain_flag))
    {
        if (m_metaEnd == pos)
        {
            // Missing format.
            encoding = ReadFieldError;
        }
        else
        {
            format = static_cast<event_field_format>(m_metaBuf[pos]);
            pos += 1;
            if (0 != (format & event_field_format_chain_flag))
            {
                if (m_metaEnd - pos < 2)
                {
                    // Missing tag.
                    encoding = ReadFieldError;
                }
                else
                {
                    tag = *reinterpret_cast<uint16_t UNALIGNED const*>(m_metaBuf + pos);
                    pos += 2;

                    if (m_needByteSwap)
                    {
                        tag = bswap_16(tag);
                    }
                }
            }
        }
    }

    m_stackTop.NextOffset = pos;
    return {
        static_cast<event_field_encoding>(encoding & ~event_field_encoding_chain_flag),
        static_cast<event_field_format>(format & ~event_field_format_chain_flag),
        tag };
}

bool
EventEnumerator::StartArray() noexcept
{
    m_stackTop.ArrayFlags = m_fieldType.Encoding & EncodingCountMask;
    m_fieldType.Encoding = static_cast<event_field_encoding>(m_fieldType.Encoding & event_field_encoding_value_mask);
    m_elementSize = 0;
    m_itemSizeRaw = 0;
    m_dataPosCooked = m_dataPosRaw;
    m_itemSizeCooked = 0;
    SetState(EventEnumeratorState_ArrayBegin, SubState_ArrayBegin);

    // Determine the m_elementSize value.
    bool movedToItem;
    switch (m_fieldType.Encoding)
    {
    case event_field_encoding_struct:
        movedToItem = true;
        goto Done;

    case event_field_encoding_value8:
        m_elementSize = 1;
        break;

    case event_field_encoding_value16:
        m_elementSize = 2;
        break;

    case event_field_encoding_value32:
        m_elementSize = 4;
        break;

    case event_field_encoding_value64:
        m_elementSize = 8;
        break;

    case event_field_encoding_value128:
        m_elementSize = 16;
        break;

    case event_field_encoding_zstring_char8:
    case event_field_encoding_zstring_char16:
    case event_field_encoding_zstring_char32:
    case event_field_encoding_string_length16_char8:
    case event_field_encoding_string_length16_char16:
    case event_field_encoding_string_length16_char32:
        movedToItem = true;
        goto Done;

    case event_field_encoding_invalid:
        movedToItem = SetErrorState(EventEnumeratorError_InvalidData);
        goto Done;

    default:
        movedToItem = SetErrorState(EventEnumeratorError_NotSupported);
        goto Done;
    }

    // For simple array element types, validate that Count * m_elementSize <= RemainingSize.
    // That way we can skip per-element validation and we can safely expose the array data
    // during ArrayBegin.
    {
        unsigned const cbRemaining = m_dataEnd - m_dataPosRaw;
        unsigned const cbArray = static_cast<unsigned>(m_elementSize) * m_stackTop.ArrayCount;
        if (cbRemaining < cbArray)
        {
            movedToItem = SetErrorState(EventEnumeratorError_InvalidData);
        }
        else
        {
            m_itemSizeRaw = m_itemSizeCooked = cbArray;
            movedToItem = true;
        }
    }

Done:

    return movedToItem;
}

void
EventEnumerator::StartStruct() noexcept
{
    assert(m_fieldType.Encoding == event_field_encoding_struct);
    m_elementSize = 0;
    m_itemSizeRaw = 0;
    m_dataPosCooked = m_dataPosRaw;
    m_itemSizeCooked = 0;
    SetState(EventEnumeratorState_StructBegin, SubState_StructBegin);
}

bool
EventEnumerator::StartValue() noexcept
{
    unsigned const cbRemaining = m_dataEnd - m_dataPosRaw;

    assert(m_state == EventEnumeratorState_Value);
    assert(m_fieldType.Encoding ==
        (m_metaBuf[m_stackTop.NameOffset + m_stackTop.NameSize + 1] & event_field_encoding_value_mask));
    m_dataPosCooked = m_dataPosRaw;
    m_elementSize = 0;

    bool movedToItem;
    switch (m_fieldType.Encoding)
    {
    case event_field_encoding_value8:
        m_itemSizeRaw = m_itemSizeCooked = m_elementSize = 1;
        if (m_itemSizeRaw <= cbRemaining)
        {
            movedToItem = true;
            goto Done;
        }
        break;

    case event_field_encoding_value16:
        m_itemSizeRaw = m_itemSizeCooked = m_elementSize = 2;
        if (m_itemSizeRaw <= cbRemaining)
        {
            movedToItem = true;
            goto Done;
        }
        break;

    case event_field_encoding_value32:
        m_itemSizeRaw = m_itemSizeCooked = m_elementSize = 4;
        if (m_itemSizeRaw <= cbRemaining)
        {
            movedToItem = true;
            goto Done;
        }
        break;

    case event_field_encoding_value64:
        m_itemSizeRaw = m_itemSizeCooked = m_elementSize = 8;
        if (m_itemSizeRaw <= cbRemaining)
        {
            movedToItem = true;
            goto Done;
        }
        break;

    case event_field_encoding_value128:
        m_itemSizeRaw = m_itemSizeCooked = m_elementSize = 16;
        if (m_itemSizeRaw <= cbRemaining)
        {
            movedToItem = true;
            goto Done;
        }
        break;

    case event_field_encoding_zstring_char8:
        StartValueStringNul<uint8_t>();
        break;

    case event_field_encoding_zstring_char16:
        StartValueStringNul<uint16_t>();
        break;

    case event_field_encoding_zstring_char32:
        StartValueStringNul<uint32_t>();
        break;

    case event_field_encoding_string_length16_char8:
        StartValueStringLength16(0);
        break;

    case event_field_encoding_string_length16_char16:
        StartValueStringLength16(1);
        break;

    case event_field_encoding_string_length16_char32:
        StartValueStringLength16(2);
        break;

    case event_field_encoding_invalid:
    case event_field_encoding_struct: // Should never happen.
    default:
        assert(m_fieldType.Encoding != event_field_encoding_struct);
        m_itemSizeRaw = m_itemSizeCooked = 0;
        movedToItem = SetErrorState(EventEnumeratorError_InvalidData);
        goto Done;
    }

    if (cbRemaining < m_itemSizeRaw)
    {
        m_itemSizeRaw = m_itemSizeCooked = 0;
        movedToItem = SetErrorState(EventEnumeratorError_InvalidData);
    }
    else
    {
        movedToItem = true;
    }

Done:

    return movedToItem;
}

void
EventEnumerator::StartValueSimple() noexcept
{
    assert(m_stackTop.ArrayIndex < m_stackTop.ArrayCount);
    assert(m_stackTop.ArrayFlags);
    assert(m_fieldType.Encoding != event_field_encoding_struct);
    assert(m_elementSize != 0);
    assert(m_itemSizeCooked == m_elementSize);
    assert(m_itemSizeRaw == m_elementSize);
    assert(m_dataEnd >= m_dataPosRaw + m_itemSizeRaw);
    assert(m_state == EventEnumeratorState_Value);
    m_dataPosCooked = m_dataPosRaw;
}

template<class CH>
void
EventEnumerator::StartValueStringNul() noexcept
{
    // cch = strnlen(value, cchRemaining)
    CH const UNALIGNED* pch = reinterpret_cast<CH const*>(m_dataBuf + m_dataPosRaw);
    uint32_t const cchMax = (m_dataEnd - m_dataPosRaw) / sizeof(CH);
    uint32_t cch;
    for (cch = 0; cch != cchMax && pch[cch] != 0; cch += 1) {}

    m_itemSizeCooked = static_cast<unsigned>(cch * sizeof(CH));
    m_itemSizeRaw = m_itemSizeCooked + sizeof(CH);
}

void
EventEnumerator::StartValueStringLength16(uint8_t charSizeShift) noexcept
{
    unsigned const cbRemaining = m_dataEnd - m_dataPosRaw;
    if (cbRemaining < sizeof(uint16_t))
    {
        m_itemSizeRaw = sizeof(uint16_t);
    }
    else
    {
        m_dataPosCooked = m_dataPosRaw + sizeof(uint16_t);

        auto cch = *reinterpret_cast<uint16_t const UNALIGNED*>(m_dataBuf + m_dataPosRaw);
        if (m_needByteSwap)
        {
            cch = bswap_16(cch);
        }

        m_itemSizeCooked = cch << charSizeShift;
        m_itemSizeRaw = m_itemSizeCooked + sizeof(uint16_t);
    }
}

void
EventEnumerator::SetState(EventEnumeratorState newState, SubState newSubState) noexcept
{
    m_state = newState;
    m_subState = newSubState;
}

/*
Resets: m_dataPosCooked, m_itemSizeCooked, m_itemSizeRaw, m_state.
*/
void
EventEnumerator::SetEndState(EventEnumeratorState newState, SubState newSubState) noexcept
{
    m_dataPosCooked = m_dataPosRaw;
    m_itemSizeRaw = 0;
    m_itemSizeCooked = 0;
    m_state = newState;
    m_subState = newSubState;
}

bool
EventEnumerator::SetNoneState(EventEnumeratorError error) noexcept
{
    m_dataBuf = nullptr;
    m_metaBuf = nullptr;
    m_tracepointName = {};
    m_state = EventEnumeratorState_None;
    m_subState = SubState_None;
    m_lastError = error;
    return false;
}

bool
EventEnumerator::SetErrorState(EventEnumeratorError error) noexcept
{
    m_state = EventEnumeratorState_Error;
    m_subState = SubState_Error;
    m_lastError = error;
    return false;
}
