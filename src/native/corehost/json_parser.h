// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __JSON_PARSER_H__
#define __JSON_PARSER_H__

#ifdef __sun
// This optimization relies on zeros in higher 16-bits, whereas SunOS has 1s. More details at
// https://github.com/Tencent/rapidjson/issues/1596.
// The impact here was that runtimeOptions key available in hwapp.runtimeconfig.json was not
// located by RapidJson's FindMember() API from runtime_config_t::ensure_parsed().
#define RAPIDJSON_48BITPOINTER_OPTIMIZATION 0
#endif

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

        bool parse_raw_data(char* data, int64_t size, const pal::string_t& context);
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
};

#endif // __JSON_PARSER_H__
