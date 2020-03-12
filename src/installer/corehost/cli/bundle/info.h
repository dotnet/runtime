// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __INFO_H_
#define __INFO_H_

#include "error_codes.h"
#include "header.h"

// bundle::info supports:
// * API for identification of a single-file app bundle, and
// * Minimal probing and mapping functionality only for the app.runtimeconfig.json and app.deps.json files.
// bundle::info is used by HostFxr to read the above config files.

namespace bundle
{
    struct info_t
    {
        struct config_t
        {
            config_t() {}

            config_t(const config_t& config)
            {
                m_path = config.m_path;
                m_location = config.m_location;
            }

            config_t(const pal::string_t& path, const location_t *location)
            {
                m_path = path;
                m_location = location;
            }

            bool matches(const pal::string_t& path) const
            {
                return m_location->is_valid() && path.compare(m_path) == 0;
            }

            static bool probe(const pal::string_t& path)
            {
                return bundle::info_t::the_app->m_deps_json.matches(path) ||
                       bundle::info_t::the_app->m_runtimeconfig_json.matches(path);
            }

            static const int8_t* map(const pal::string_t& path, const location_t* &location);
            static void unmap(const int8_t* addr, const location_t* location);

        private:
            pal::string_t m_path;
            const location_t *m_location;
        } json_info;

        static StatusCode process_bundle(const pal::char_t* bundle_path, const pal::char_t *app_path, int64_t header_offset);
        static bool is_single_file_bundle() { return the_app != nullptr; }

        bool is_netcoreapp3_compat_mode() const { return m_header.is_netcoreapp3_compat_mode(); }
        const pal::string_t& base_path() const { return m_base_path; }

        // Global single-file info object
        static const info_t* the_app;

    protected:
        info_t(const pal::char_t* bundle_path_value,
            int64_t header_offset_value)
            : m_bundle_path(bundle_path_value)
            , m_bundle_size(0)
            , m_header_offset(header_offset_value)
            , m_deps_json()
            , m_runtimeconfig_json() {}

        info_t(const info_t* info)
        {
            m_bundle_path = info->m_bundle_path;
            m_base_path = info->m_base_path;
            m_bundle_size = info->m_bundle_size;
            m_header_offset = info->m_header_offset;
            m_header = info->m_header;
            m_deps_json = info->m_deps_json;
            m_runtimeconfig_json = info->m_runtimeconfig_json;
        }

        const int8_t* map_bundle();
        void unmap_bundle(const int8_t* addr) const;

        pal::string_t m_bundle_path;
        pal::string_t m_base_path;
        size_t m_bundle_size;
        int64_t m_header_offset;
        header_t m_header;
        config_t m_deps_json;
        config_t m_runtimeconfig_json;

    private:
        void init_config(const pal::string_t& app_path);
        StatusCode process_header();
    };
}
#endif // __INFO_H_
