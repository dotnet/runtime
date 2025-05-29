// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// These are only used by rapidjson/error/en.h to declare the error messages,
// and have to be set to these values before any files are included.  They're
// defined here because it's the only place that calls GetParseError().
#undef RAPIDJSON_ERROR_CHARTYPE
#undef RAPIDJSON_ERROR_STRING
#define RAPIDJSON_ERROR_CHARTYPE pal::char_t
#define RAPIDJSON_ERROR_STRING(x) _X(x)

#include <json_parser.h>
#include <rapidjson/error/en.h>
#include "utils.h"
#include <cassert>
#include <cstdint>

namespace {

void get_line_column_from_offset(const char* data, uint64_t size, size_t offset, int *line, int *column)
{
    assert(offset <= size);

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

bool json_parser_t::parse_raw_data(char* data, int64_t size, const pal::string_t& context)
{
    assert(data != nullptr);

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

bool json_parser_t::parse_file(const pal::string_t& path)
{
    // This code assumes that the caller has checked that the file `path` exists
    // either within the bundle, or as a real file on disk.
    assert(m_data == nullptr);
    assert(m_bundle_location == nullptr);

    if (bundle::info_t::is_single_file_bundle())
    {
        // Due to in-situ parsing on Linux,
        //  * The json file is mapped as copy-on-write.
        //  * The mapping cannot be immediately released, and will be unmapped by the json_parser destructor.
        m_data = bundle::info_t::config_t::map(path, m_bundle_location);

        if (m_data != nullptr)
        {
            m_size = (size_t)m_bundle_location->size;
        }
    }

    if (m_data == nullptr)
    {
#ifdef _WIN32
        // We can't use in-situ parsing on Windows, as JSON data is encoded in
        // UTF-8 and the host expects wide strings.
        // We do not need copy-on-write, so read-only mapping will be enough.
        m_data = (char*)pal::mmap_read(path, &m_size);
#else // _WIN32
        m_data = (char*)pal::mmap_copy_on_write(path, &m_size);
#endif // _WIN32

        if (m_data == nullptr)
        {
            trace::error(_X("Cannot use file stream for [%s]: %s"), path.c_str(), pal::strerror(errno).c_str());
            return false;
        }
    }

    char *data = m_data;
    size_t size = m_size;

    // Skip over UTF-8 BOM, if present
    if (size >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[1] == 0xBF)
    {
        size -= 3;
        data += 3;
    }

    return parse_raw_data(data, size, path);
}

json_parser_t::~json_parser_t()
{
    if (m_data != nullptr)
    {
        if (m_bundle_location != nullptr)
        {
            bundle::info_t::config_t::unmap(m_data, m_bundle_location);
        }
        else
        {
            pal::munmap((void*)m_data, m_size);
        }
    }
}
