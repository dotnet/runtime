// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __JSON_PARSER_H__
#define __JSON_PARSER_H__

#include "pal.h"
#include "rapidjson/document.h"
#include "rapidjson/fwd.h"
#include <vector>
#include "bundle/info.h"

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

        bool parse_stream(pal::istream_t& stream, const pal::string_t& context);
        bool parse_file(const pal::string_t& path);

        json_parser_t()
            : m_bundle_data(nullptr)
            , m_bundle_location(nullptr) {}

        ~json_parser_t();

    private:
        // This is a vector of char and not pal::char_t because JSON data
        // parsed by this class is always encoded in UTF-8.  On Windows,
        // where wide strings are used, m_json is kept in UTF-8, but converted
        // to UTF-16 by m_document during load.
        std::vector<char> m_json;
        document_t m_document;

        // If a json file is parsed from a single-file bundle, the following two fields represent:
        char* m_bundle_data; // The memory mapped bytes of the application bundle.
        const bundle::location_t* m_bundle_location; // Location of this json file within the bundle.

        void realloc_buffer(size_t size);
        bool parse_json(char* data, int64_t size, const pal::string_t& context);
};

#endif // __JSON_PARSER_H__
