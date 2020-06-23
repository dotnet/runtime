// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// These are only used by rapidjson/error/en.h to declare the error messages,
// and have to be set to these values before any files are included.  They're
// defined here because it's the only place that calls GetParseError().
#undef RAPIDJSON_ERROR_CHARTYPE
#undef RAPIDJSON_ERROR_STRING
#define RAPIDJSON_ERROR_CHARTYPE pal::char_t
#define RAPIDJSON_ERROR_STRING(x) _X(x)

#include "json_parser.h"
#include "rapidjson/error/en.h"
#include "utils.h"
#include <cassert>
#include <cstdint>

namespace {

// Try to match 0xEF 0xBB 0xBF byte sequence (no endianness here.)
std::streampos get_utf8_bom_length(pal::istream_t& stream)
{
    if (stream.eof())
    {
        return 0;
    }

    auto peeked = stream.peek();
    if (peeked == EOF || ((peeked & 0xFF) != 0xEF))
    {
        return 0;
    }

    unsigned char bytes[3];
    stream.read(reinterpret_cast<char*>(bytes), 3);
    if ((stream.gcount() < 3) || (bytes[1] != 0xBB) || (bytes[2] != 0xBF))
    {
        return 0;
    }

    return 3;
}

void get_line_column_from_offset(const char* data, uint64_t size, size_t offset, int *line, int *column)
{
    assert(offset < size);

    *line = *column = 1;

    for (size_t i = 0; i < offset; i++)
    {
        (*column)++;

        if (data[i] == '\n')
        {
            (*line)++;
            *column = 1;
        }
        else if (data[i] == '\r' && data[i + 1] == '\n')
        {
            (*line)++;
            *column = 1;

            i++; // Discard carriage return
        }
    }
}

} // empty namespace

void json_parser_t::realloc_buffer(size_t size)
{
    m_json.resize(size + 1);
    m_json[size] = '\0';
}

bool json_parser_t::parse_json(char* data, int64_t size, const pal::string_t& context)
{
    constexpr auto flags = rapidjson::ParseFlag::kParseStopWhenDoneFlag | rapidjson::ParseFlag::kParseCommentsFlag;
#ifdef _WIN32
    // Can't use in-situ parsing on Windows, as JSON data is encoded in
    // UTF-8 and the host expects wide strings.  m_document will store
    // data in UTF-16 (with pal::char_t as the character type), but it
    // has to know that data is encoded in UTF-8 to convert during parsing.
    m_document.Parse<flags, rapidjson::UTF8<>>(data);
#else // _WIN32
    m_document.ParseInsitu<flags>(data);
#endif // _WIN32

    if (m_document.HasParseError())
    {
        int line, column;
        size_t offset = m_document.GetErrorOffset();

        get_line_column_from_offset(data, size, offset, &line, &column);

        trace::error(_X("A JSON parsing exception occurred in [%s], offset %zu (line %d, column %d): %s"),
            context.c_str(), offset, line, column,
            rapidjson::GetParseError_En(m_document.GetParseError()));
        return false;
    }

    if (!m_document.IsObject())
    {
        trace::error(_X("Expected a JSON object in [%s]"), context.c_str());
        return false;
    }

    return true;
}

bool json_parser_t::parse_stream(pal::istream_t& stream,
                                 const pal::string_t& context)
{
    if (!stream.good())
    {
        trace::error(_X("Cannot use stream for resource [%s]: %s"), context.c_str(), pal::strerror(errno));
        return false;
    }

    auto current_pos = ::get_utf8_bom_length(stream);
    stream.seekg(0, stream.end);
    auto stream_size = stream.tellg();
    stream.seekg(current_pos, stream.beg);

    realloc_buffer(stream_size - current_pos);
    stream.read(m_json.data(), stream_size - current_pos);

    return parse_json(m_json.data(), m_json.size(), context);
}

bool json_parser_t::parse_file(const pal::string_t& path)
{
    // This code assumes that the caller has checked that the file `path` exists
    // either within the bundle, or as a real file on disk.
    assert(m_bundle_data == nullptr);
    assert(m_bundle_location == nullptr);

    if (bundle::info_t::is_single_file_bundle())
    {
        // Due to in-situ parsing on Linux, 
        //  * The json file is mapped as copy-on-write.
        //  * The mapping cannot be immediately released, and will be unmapped by the json_parser destructor.
        m_bundle_data = bundle::info_t::config_t::map(path, m_bundle_location);

        if (m_bundle_data != nullptr)
        {
            bool result = parse_json(m_bundle_data, m_bundle_location->size, path);
            return result;
        }
    }

    pal::ifstream_t file{ path };
    return parse_stream(file, path);
}

json_parser_t::~json_parser_t()
{
    if (m_bundle_data != nullptr)
    {
        bundle::info_t::config_t::unmap(m_bundle_data, m_bundle_location);
    }
}
