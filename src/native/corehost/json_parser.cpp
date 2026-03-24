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

void get_line_column_from_offset(const char* data, size_t size, size_t offset, int *line, int *column)
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

bool json_parser_t::parse_fully_trusted_raw_data(char* data, size_t size, const pal::string_t& context)
{
    // This code assumes that the provided data is fully trusted; that is, that no portion
    // of it has been provided by a hostile agent.

    assert(data != nullptr);

    constexpr auto flags = rapidjson::ParseFlag::kParseStopWhenDoneFlag | rapidjson::ParseFlag::kParseCommentsFlag;

    // Can't use in-situ parsing, as RapidJson requires a null-terminated string,
    // and the provided data may not be null-terminated. The input data is always
    // expected to be UTF-8 encoded; m_document is initialized with the appropriate
    // encoding type for the underlying OS (UTF-16 on Windows; UTF-8 elsewhere).
    m_document.Parse<flags, rapidjson::UTF8<>>(data, size);

    if (m_document.HasParseError())
    {
        int line, column;
        size_t offset = m_document.GetErrorOffset();

        get_line_column_from_offset(data, size, offset, &line, &column);

        m_parse_error = utils::format_string(_X("JSON parsing exception: %s [offset %zu: line %d, column %d]"),
            rapidjson::GetParseError_En(m_document.GetParseError()),
            offset, line, column
        );
        return false;
    }

    if (!m_document.IsObject())
    {
        m_parse_error = _X("Expected a JSON object");
        return false;
    }

    return true;
}

bool json_parser_t::parse_fully_trusted_file(const pal::string_t& path)
{
    // This code assumes that the caller has checked that the file `path` exists
    // either within the bundle, or as a real file on disk. It also assumes
    // that the contents of the target file are fully trusted; that is, that no
    // portion of its contents has been provided by a hostile agent.

    assert(m_data == nullptr);
    assert(m_bundle_location == nullptr);

    if (bundle::info_t::is_single_file_bundle())
    {
        // The mapping cannot be immediately released; it will be unmapped by the json_parser destructor.
        m_data = bundle::info_t::config_t::map(path, m_bundle_location);

        if (m_data != nullptr)
        {
            m_size = (size_t)m_bundle_location->size;
        }
    }

    if (m_data == nullptr)
    {
        m_data = (char*)pal::mmap_read(path, &m_size);

        if (m_data == nullptr)
        {
            trace::error(_X("Cannot use file stream for [%s]: %s"), path.c_str(), pal::strerror(errno).c_str());
            return false;
        }
    }

    char *data = m_data;
    size_t size = m_size;

    // Skip over UTF-8 BOM, if present
    if (size >= 3 && static_cast<unsigned char>(data[0]) == 0xEF && static_cast<unsigned char>(data[1]) == 0xBB && static_cast<unsigned char>(data[2]) == 0xBF)
    {
        size -= 3;
        data += 3;
    }

    return parse_fully_trusted_raw_data(data, size, path);
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
