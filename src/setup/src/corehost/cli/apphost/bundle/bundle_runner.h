// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __BUNDLE_RUNNER_H__
#define __BUNDLE_RUNNER_H__


#include <cstdint>
#include <memory>
#include "header.h"
#include "manifest.h"
#include "marker.h"
#include "error_codes.h"

namespace bundle
{
    class bundle_runner_t
    {
    public:
        bundle_runner_t(const pal::string_t& bundle_path)
            : m_bundle_stream(nullptr)
            , m_bundle_path(bundle_path)
        {
        }

        pal::string_t get_extraction_dir()
        {
            return m_extraction_dir;
        }

        StatusCode extract();

        static void read(void* buf, size_t size, FILE* stream);
        static void write(const void* buf, size_t size, FILE* stream);
        static size_t get_path_length(int8_t first_byte, FILE* stream);
        static void read_string(pal::string_t& str, size_t size, FILE* stream);

    private:
        void reopen_host_for_reading();
        static void seek(FILE* stream, long offset, int origin);

        int32_t num_embedded_files() { return m_header.num_embedded_files(); }
        const pal::string_t& bundle_id() { return m_header.bundle_id(); }

        void determine_extraction_dir();
        void create_working_extraction_dir();
        bool can_reuse_extraction();

        FILE* create_extraction_file(const pal::string_t& relative_path);
        void extract_file(const file_entry_t& entry);

        FILE* m_bundle_stream;
        header_t m_header;
        manifest_t m_manifest;
        pal::string_t m_bundle_path;
        pal::string_t m_extraction_dir;
        pal::string_t m_working_extraction_dir;
    };

}

#endif // __BUNDLE_RUNNER_H__
