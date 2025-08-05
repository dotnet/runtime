// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#include <tracepoint/PerfEventMetadata.h>
#include <tracepoint/PerfByteReader.h>
#include <assert.h>
#include <errno.h>
#include <stdlib.h>
#include <string.h>

using namespace std::string_view_literals;
using namespace tracepoint_decode;

#ifndef _In_
#define _In_
#endif
#ifndef _In_z_
#define _In_z_
#endif
#ifndef _Inout_ 
#define _Inout_ 
#endif
#ifndef _Out_
#define _Out_
#endif

//#define DEBUG_PRINTF(...) ((void)0)
#ifndef DEBUG_PRINTF
#define DEBUG_PRINTF(...) fprintf(stderr, __VA_ARGS__)
#include <stdio.h>
#endif // DEBUG_PRINTF

static bool
IsSpaceOrTab(char ch) noexcept
{
    return ch == ' ' || ch == '\t';
}

static bool
IsEolChar(char ch) noexcept
{
    return ch == '\r' || ch == '\n';
}

static constexpr bool
IsIdentStart(char ch) noexcept
{
    uint8_t const chLower = ch | 0x20;
    return ('a' <= chLower && chLower <= 'z') ||
        ch == '_';
}

static constexpr bool
IsIdentContinue(char ch) noexcept
{
    uint8_t const chLower = ch | 0x20;
    return ('a' <= chLower && chLower <= 'z') ||
        ('0' <= ch && ch <= '9') ||
        ch == '_';
}

static bool
ParseOptionalU16(_In_z_ char const* value, _Out_ uint16_t* pResult) noexcept
{
    errno = 0;
    auto const lresult = strtoul(value, nullptr, 0);
    if (0 != errno || lresult > 0xFFFF)
    {
        *pResult = 0;
        return false;
    }
    else
    {
        *pResult = static_cast<uint16_t>(lresult);
        return true;
    }
}

// Given p pointing after the opening quote, returns position after the closing quote.
static char const*
ConsumeString(char const* p, char const* pEnd, char quote) noexcept
{
    while (p != pEnd)
    {
        char consumed = *p;
        p += 1;

        if (consumed == quote)
        {
            break;
        }
        else if (consumed == '\\')
        {
            if (p == pEnd)
            {
                DEBUG_PRINTF("EOF within '\\' escape\n");
                break; // Unexpected.
            }

            // Ignore whatever comes after the backslash, which
            // is significant if it is quote or '\\'.
            p += 1;
        }
    }

    return p;
}

// Given p pointing after the opening brance, returns position after the closing brace.
static char const*
ConsumeBraced(char const* p, char const* pEnd, char open, char close) noexcept
{
    unsigned nesting = 1;
    while (p != pEnd)
    {
        char consumed = *p;
        p += 1;
        if (consumed == close)
        {
            nesting -= 1;
            if (nesting == 0)
            {
                break;
            }
        }
        else if (consumed == open)
        {
            nesting += 1;
        }
    }

    return p;
}

enum TokenType : uint8_t
{
    TokenNone,
    TokenIdent,         // e.g. MyFile
    TokenBrackets,      // e.g. [...]
    TokenParentheses,   // e.g. (...)
    TokenString,        // e.g. "asdf"
    TokenPunctuation,   // e.g. *
};

class Tokenizer
{
    char const* const pEnd;
    char const* pCurrent;

public:

    std::string_view value;
    TokenType type;

    explicit
    Tokenizer(std::string_view str) noexcept
        : pEnd(str.data() + str.size())
        , pCurrent(str.data())
        , value()
        , type() {}

    void
    MoveNext() noexcept
    {
        TokenType newType;
        auto p = pCurrent;

        while (p != pEnd && *p <= ' ')
        {
            p += 1;
        }

        auto const pTokenStart = p;

        if (p == pEnd)
        {
            newType = TokenNone;
        }
        else if (IsIdentStart(*p))
        {
            // Return identifier.
            p += 1;
            while (p != pEnd && IsIdentContinue(*p))
            {
                p += 1;
            }

            newType = TokenIdent;
        }
        else
        {
            switch (*p)
            {
            case '\'':
            case '\"':
                // Return up to the closing quote.
                p = ConsumeString(p + 1, pEnd, *p);
                newType = TokenString;
                break;
            case '(':
                // Return up to closing paren (allow nesting).
                p = ConsumeBraced(p + 1, pEnd, '(', ')');
                newType = TokenParentheses;
                break;
            case '[':
                // Return up to closing brace (allow nesting).
                p = ConsumeBraced(p + 1, pEnd, '[', ']');
                newType = TokenBrackets;
                break;
            default: // Return single character token.
                p += 1;
                newType = TokenPunctuation;
                break;
            }
        }

        pCurrent = p;
        value = { pTokenStart, static_cast<size_t>(p - pTokenStart) };
        type = newType;
    }
};

PerfFieldMetadata
PerfFieldMetadata::Parse(
    bool longSize64,
    std::string_view formatLine) noexcept
{
    PerfFieldMetadata metadata;

    std::string_view field;
    uint16_t offset = 0;
    bool foundOffset = false;
    uint16_t size = 0;
    bool foundSize = false;
    int8_t isSigned = -1;

    auto p = formatLine.data();
    auto const pEnd = p + formatLine.size();

    // FIND: field, offset, size

    // Search for " NAME: VALUE;"
    while (p != pEnd)
    {
        // Skip spaces and semicolons.
        while (IsSpaceOrTab(*p) || *p == ';')
        {
            p += 1;
            if (p == pEnd) goto Done;
        }

        // "NAME:"
        auto const pPropName = p;
        while (*p != ':')
        {
            p += 1;
            if (p == pEnd)
            {
                DEBUG_PRINTF("EOL before ':' in format\n");
                goto Done; // Unexpected.
            }
        }

        auto const propName = std::string_view(pPropName, p - pPropName);
        p += 1; // Skip ':'

        // Skip spaces.
        while (p != pEnd && IsSpaceOrTab(*p))
        {
            DEBUG_PRINTF("Space before propval in format\n");
            p += 1; // Unexpected.
        }

        // "VALUE;"
        auto const pPropValue = p;
        while (p != pEnd && *p != ';')
        {
            p += 1;
        }

        if (propName == "field"sv || propName == "field special"sv)
        {
            field = std::string_view(pPropValue, p - pPropValue);
        }
        else if (propName == "offset"sv && p != pEnd)
        {
            foundOffset = ParseOptionalU16(pPropValue, &offset);
        }
        else if (propName == "size"sv && p != pEnd)
        {
            foundSize = ParseOptionalU16(pPropValue, &size);
        }
        else if (propName == "signed"sv && p != pEnd)
        {
            uint16_t signedVal;
            isSigned = !ParseOptionalU16(pPropValue, &signedVal)
                ? -1 // Not a valid U16, isSigned = NULL.
                : signedVal != 0;
        }
    }

Done:

    if (!field.empty() && foundOffset && foundSize)
    {
        metadata = PerfFieldMetadata(longSize64, field, offset, size, isSigned);
    }

    return metadata;
}

PerfFieldMetadata::PerfFieldMetadata(
    bool longSize64,
    std::string_view field,
    uint16_t offset,
    uint16_t size,
    int8_t isSigned) noexcept
    : m_name()
    , m_field(field)
    , m_offset(offset)
    , m_size(size)
    , m_fixedArrayCount()
    , m_elementSize()
    , m_format()
    , m_array()
{
    bool foundLongLong = false;
    bool foundLong = false;
    bool foundShort = false;
    bool foundUnsigned = false;
    bool foundSigned = false;
    bool foundStruct = false;
    bool foundDataLoc = false;
    bool foundRelLoc = false;
    bool foundArray = false;
    bool foundPointer = false;
    std::string_view baseType;

    // DEDUCE: m_name, m_fixedArrayCount

    Tokenizer tokenizer(m_field);
    for (;;)
    {
        tokenizer.MoveNext();
        switch (tokenizer.type)
        {
        case TokenNone:
            goto TokensDone;

        case TokenIdent:
            if (tokenizer.value == "long"sv)
            {
                if (foundLong)
                {
                    foundLongLong = true;
                }
                else
                {
                    foundLong = true;
                }
            }
            else if (tokenizer.value == "short"sv)
            {
                foundShort = true;
            }
            else if (tokenizer.value == "unsigned"sv)
            {
                foundUnsigned = true;
            }
            else if (tokenizer.value == "signed"sv)
            {
                foundSigned = true;
            }
            else if (tokenizer.value == "struct"sv)
            {
                foundStruct = true;
            }
            else if (tokenizer.value == "__data_loc"sv)
            {
                foundDataLoc = true;
            }
            else if (tokenizer.value == "__rel_loc"sv)
            {
                foundRelLoc = true;
            }
            else if (
                tokenizer.value != "__attribute__"sv &&
                tokenizer.value != "const"sv &&
                tokenizer.value != "volatile"sv)
            {
                baseType = m_name;
                m_name = tokenizer.value;
            }
            break;

        case TokenBrackets:
            foundArray = true;
            (void)ParseOptionalU16(tokenizer.value.data() + 1, &m_fixedArrayCount);
            tokenizer.MoveNext();
            if (tokenizer.type == TokenIdent)
            {
                baseType = m_name;
                m_name = tokenizer.value;
            }
            goto TokensDone;

        case TokenParentheses:
        case TokenString:
            // Ignored.
            break;

        case TokenPunctuation:
            // Most punctuation ignored.
            if (tokenizer.value == "*"sv)
            {
                foundPointer = true;
            }
            break;

        default:
            assert(false);
            goto TokensDone;
        }
    }

TokensDone:

    if (m_name.empty())
    {
        m_name = noname;
    }

    // DEDUCE: m_elementSize, m_format

    bool fixupElementSize = false;

    if (foundPointer)
    {
        m_format = PerfFieldFormatHex;
        m_elementSize = longSize64 ? PerfFieldElementSize64 : PerfFieldElementSize32;
    }
    else if (foundStruct)
    {
        m_format = PerfFieldFormatNone;
        m_elementSize = PerfFieldElementSize8;
    }
    else if (baseType.empty() || baseType == "int"sv)
    {
        m_format = foundUnsigned
            ? PerfFieldFormatUnsigned
            : PerfFieldFormatSigned;
        if (foundLongLong)
        {
            m_elementSize = PerfFieldElementSize64;
        }
        else if (foundLong)
        {
            m_elementSize = longSize64 ? PerfFieldElementSize64 : PerfFieldElementSize32;
            if (foundUnsigned)
            {
                m_format = PerfFieldFormatHex; // Use hex for unsigned long.
            }
        }
        else if (foundShort)
        {
            m_elementSize = PerfFieldElementSize16;
        }
        else
        {
            m_elementSize = PerfFieldElementSize32; // "unsigned" or "signed" means "int".
            if (baseType.empty() && !foundUnsigned && !foundSigned)
            {
                // Unexpected.
                DEBUG_PRINTF("No baseType found for \"%.*s\"\n",
                    (unsigned)m_field.size(), m_field.data());
            }
        }
    }
    else if (baseType == "char"sv)
    {
        m_format = foundUnsigned
            ? PerfFieldFormatUnsigned
            : foundSigned
            ? PerfFieldFormatSigned
            : PerfFieldFormatString;
        m_elementSize = PerfFieldElementSize8;
    }
    else if (baseType == "u8"sv || baseType == "__u8"sv || baseType == "uint8_t"sv)
    {
        m_format = PerfFieldFormatUnsigned;
        m_elementSize = PerfFieldElementSize8;
    }
    else if (baseType == "s8"sv || baseType == "__s8"sv || baseType == "int8_t"sv)
    {
        m_format = PerfFieldFormatSigned;
        m_elementSize = PerfFieldElementSize8;
    }
    else if (baseType == "u16"sv || baseType == "__u16"sv || baseType == "uint16_t"sv)
    {
        m_format = PerfFieldFormatUnsigned;
        m_elementSize = PerfFieldElementSize16;
    }
    else if (baseType == "s16"sv || baseType == "__s16"sv || baseType == "int16_t"sv)
    {
        m_format = PerfFieldFormatSigned;
        m_elementSize = PerfFieldElementSize16;
    }
    else if (baseType == "u32"sv || baseType == "__u32"sv || baseType == "uint32_t"sv)
    {
        m_format = PerfFieldFormatUnsigned;
        m_elementSize = PerfFieldElementSize32;
    }
    else if (baseType == "s32"sv || baseType == "__s32"sv || baseType == "int32_t"sv)
    {
        m_format = PerfFieldFormatSigned;
        m_elementSize = PerfFieldElementSize32;
    }
    else if (baseType == "u64"sv || baseType == "__u64"sv || baseType == "uint64_t"sv)
    {
        m_format = PerfFieldFormatUnsigned;
        m_elementSize = PerfFieldElementSize64;
    }
    else if (baseType == "s64"sv || baseType == "__s64"sv || baseType == "int64_t"sv)
    {
        m_format = PerfFieldFormatSigned;
        m_elementSize = PerfFieldElementSize64;
    }
    else
    {
        m_format = PerfFieldFormatHex;
        fixupElementSize = true;
    }

    // FIXUP: m_format

    if (m_format == PerfFieldFormatUnsigned || m_format == PerfFieldFormatSigned)
    {
        // If valid, isSigned overrides baseType.
        switch (isSigned)
        {
        default: break;
        case 0: m_format = PerfFieldFormatUnsigned; break;
        case 1: m_format = PerfFieldFormatSigned; break;
        }
    }

    // DEDUCE: m_array

    if (foundRelLoc)
    {
        m_array = PerfFieldArrayRelDyn;
        m_fixedArrayCount = 0;
    }
    else if (foundDataLoc)
    {
        m_array = PerfFieldArrayDynamic;
        m_fixedArrayCount = 0;
    }
    else if (foundArray)
    {
        m_array = PerfFieldArrayFixed;
        if (fixupElementSize && m_fixedArrayCount != 0 && m_size % m_fixedArrayCount == 0)
        {
            // Try to deduce element size from size and array count.
            switch (m_size / m_fixedArrayCount)
            {
            default:
                break;
            case 1:
                m_elementSize = PerfFieldElementSize8;
                fixupElementSize = false;
                break;
            case 2:
                m_elementSize = PerfFieldElementSize16;
                fixupElementSize = false;
                break;
            case 4:
                m_elementSize = PerfFieldElementSize32;
                fixupElementSize = false;
                break;
            case 8:
                m_elementSize = PerfFieldElementSize64;
                fixupElementSize = false;
                break;
            }
        }
    }
    else
    {
        m_array = PerfFieldArrayNone;
        m_fixedArrayCount = 0;

        // If valid, size overrides element size deduced from type name.
        switch (m_size)
        {
        default:
            break;
        case 1:
            m_elementSize = PerfFieldElementSize8;
            fixupElementSize = false;
            break;
        case 2:
            m_elementSize = PerfFieldElementSize16;
            fixupElementSize = false;
            break;
        case 4:
            m_elementSize = PerfFieldElementSize32;
            fixupElementSize = false;
            break;
        case 8:
            m_elementSize = PerfFieldElementSize64;
            fixupElementSize = false;
            break;
        }
    }

    if (fixupElementSize)
    {
        m_elementSize = PerfFieldElementSize8;
    }
}

std::string_view
PerfFieldMetadata::GetFieldBytes(
    _In_reads_bytes_(eventRawDataSize) void const* eventRawData,
    uintptr_t eventRawDataSize,
    bool fileBigEndian) const noexcept
{
    std::string_view result;
    PerfByteReader const byteReader(fileBigEndian);
    auto const eventRawDataChars = static_cast<char const*>(eventRawData);

    if (static_cast<size_t>(m_offset) + m_size <= eventRawDataSize)
    {
        if (m_size == 0)
        {
            // size 0 means "the rest of the event data"
            result = { eventRawDataChars + m_offset, eventRawDataSize - m_offset };
        }
        else switch (m_array)
        {
        default:
            result = { eventRawDataChars + m_offset, m_size };
            break;

        case PerfFieldArrayDynamic:
        case PerfFieldArrayRelDyn:
            if (m_size == 4)
            {
                // 4-byte value is an offset/length pair leading to the real data.
                auto const dyn = byteReader.ReadAsU32(eventRawDataChars + m_offset);
                auto const dynSize = dyn >> 16;
                auto dynOffset = dyn & 0xFFFF;
                if (m_array == PerfFieldArrayRelDyn)
                {
                    // offset is relative to end of field.
                    dynOffset += m_offset + m_size;
                }

                if (dynOffset + dynSize <= eventRawDataSize)
                {
                    result = { eventRawDataChars + dynOffset, dynSize };
                }
            }
            else if (m_size == 2)
            {
                // 2-byte value is an offset leading to the real data, size is strlen.
                size_t dynOffset = byteReader.ReadAsU16(eventRawDataChars + m_offset);
                if (m_array == PerfFieldArrayRelDyn)
                {
                    // offset is relative to end of field.
                    dynOffset += m_offset + m_size;
                }

                if (dynOffset < eventRawDataSize)
                {
                    auto const dynSize = strnlen(
                        eventRawDataChars + dynOffset,
                        eventRawDataSize - dynOffset);
                    if (dynSize < eventRawDataSize - dynOffset)
                    {
                        result = { eventRawDataChars + dynOffset, dynSize };
                    }
                }
            }
            break;
        }
    }

    return result;
}

PerfEventMetadata::~PerfEventMetadata()
{
    return;
}

PerfEventMetadata::PerfEventMetadata() noexcept
    : m_systemName()
    , m_formatFileContents()
    , m_name()
    , m_printFmt()
    , m_fields()
    , m_id()
    , m_commonFieldCount()
    , m_commonFieldsSize()
    , m_kind()
{
    return;
}

void
PerfEventMetadata::Clear() noexcept
{
    m_systemName = {};
    m_name = {};
    m_printFmt = {};
    m_fields = {};
    m_id = {};
    m_commonFieldCount = {};
    m_commonFieldsSize = {};
    m_kind = {};
}

bool
PerfEventMetadata::Parse(
    bool longSize64,
    std::string_view systemName,
    std::string_view formatFileContents) noexcept(false)
{
    Clear();

    m_systemName = systemName;
    m_formatFileContents = formatFileContents;

    bool foundId = false;
    auto p = formatFileContents.data();
    auto const pEnd = p + formatFileContents.size();

    // Search for lines like "NAME: VALUE..."
    while (p != pEnd)
    {
    ContinueNextLine:

        // Skip any newlines.
        while (IsEolChar(*p))
        {
            p += 1;
            if (p == pEnd) goto Done;
        }

        // Skip spaces.
        while (IsSpaceOrTab(*p))
        {
            DEBUG_PRINTF("Space before propname in event\n");
            p += 1; // Unexpected.
            if (p == pEnd) goto Done;
        }

        // "NAME:"
        auto const pPropName = p;
        while (*p != ':')
        {
            if (IsEolChar(*p))
            {
                DEBUG_PRINTF("EOL before ':' in format\n");
                goto ContinueNextLine; // Unexpected.
            }

            p += 1;

            if (p == pEnd)
            {
                DEBUG_PRINTF("EOF before ':' in format\n");
                goto Done; // Unexpected.
            }
        }

        auto const propName = std::string_view(pPropName, p - pPropName);
        p += 1; // Skip ':'

        // Skip spaces.
        while (p != pEnd && IsSpaceOrTab(*p))
        {
            p += 1;
        }

        auto const pPropValue = p;

        // "VALUE..."
        while (p != pEnd && !IsEolChar(*p))
        {
            char consumed;

            consumed = *p;
            p += 1;

            if (consumed == '"')
            {
                p = ConsumeString(p, pEnd, '"');
            }
        }

        // Did we find something we can use?
        if (propName == "name"sv)
        {
            m_name = std::string_view(pPropValue, p - pPropValue);
        }
        else if (propName == "ID"sv && p != pEnd)
        {
            errno = 0;
            m_id = strtoul(pPropValue, nullptr, 0);
            foundId = 0 == errno;
        }
        else if (propName == "print fmt"sv)
        {
            m_printFmt = std::string_view(pPropValue, p - pPropValue);
        }
        else if (propName == "format"sv)
        {
            bool common = true;
            m_fields.clear();

            // Search for lines like: " field:TYPE NAME; offset:N; size:N; signed:N;"
            while (p != pEnd)
            {
                assert(IsEolChar(*p));

                if (pEnd - p >= 2 && p[0] == '\r' && p[1] == '\n')
                {
                    p += 2; // Skip CRLF.
                }
                else
                {
                    p += 1; // Skip CR or LF.
                }

                auto const pLine = p;
                while (p != pEnd && !IsEolChar(*p))
                {
                    p += 1;
                }

                if (pLine == p)
                {
                    // Blank line.
                    if (common)
                    {
                        // First blank line means we're done with common fields.
                        common = false;
                        continue;
                    }
                    else
                    {
                        // Second blank line means we're done with format.
                        break;
                    }
                }

                m_fields.push_back(PerfFieldMetadata::Parse(longSize64, std::string_view(pLine, p - pLine)));
                if (m_fields.back().Field().empty())
                {
                    DEBUG_PRINTF("Field parse failure\n");
                    m_fields.pop_back(); // Unexpected.
                }
                else
                {
                    m_commonFieldCount += common;
                }
            }
        }
    }

Done:

    if (m_commonFieldCount == 0)
    {
        m_commonFieldsSize = 0;
    }
    else
    {
        auto const& lastCommonField = m_fields[m_commonFieldCount - 1];
        m_commonFieldsSize = lastCommonField.Offset() + lastCommonField.Size();
    }

    m_kind =
        m_fields.size() > m_commonFieldCount &&
        m_fields[m_commonFieldCount].Name() == "eventheader_flags"sv
        ? PerfEventKind::EventHeader
        : PerfEventKind::Normal;
    return !m_name.empty() && foundId;
}
