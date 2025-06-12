// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __JSON_PARSER_H__
#define __JSON_PARSER_H__

// Turn off flaky optimization. For details, see:
// https://github.com/Tencent/rapidjson/issues/1596#issuecomment-548774663
#define RAPIDJSON_48BITPOINTER_OPTIMIZATION 0

// see https://github.com/Tencent/rapidjson/issues/1448
// including windows.h on purpose to provoke a compile time problem as GetObject is a 
// macro that gets defined when windows.h is included
#ifdef _WIN32
#define NOMINMAX
#include <windows.h>
#endif

#include "pal.h"
#include <rapidjson/document.h>
#include <rapidjson/fwd.h>
#include <vector>
#include "bundle/info.h"

#undef GetObject

class json_parser_t {
    public:
#ifdef _WIN32
        using internal_encoding_type_t = rapidjson::UTF16<pal::char_t>;
#else
        using internal_encoding_type_t = rapidjson::UTF8<pal::char_t>;
#endif
        using value_t = rapidjson::GenericValue<internal_encoding_type_t>;
        using document_t = rapidjson::GenericDocument<internal_encoding_type_t>;

        const document_t& document() const { return m_document; }

        bool parse_raw_data(char* data, int64_t size, const pal::string_t& context);
        bool parse_file(const pal::string_t& path);

        json_parser_t()
            : m_data(nullptr)
            , m_bundle_location(nullptr) {}

        ~json_parser_t();

    private:
        char* m_data; // The memory mapped bytes of the file
        size_t m_size; // Size of the mapped memory

        // On Windows, where wide strings are used, m_data is kept in UTF-8, but converted
        // to UTF-16 by m_document during load.
        document_t m_document;

        // If a json file is parsed from a single-file bundle, the following fields represents
        // the location of this json file within the bundle.
        const bundle::location_t* m_bundle_location;
};

#endif // __JSON_PARSER_H__
