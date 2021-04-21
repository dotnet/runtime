// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __HEADER_H__
#define __HEADER_H__

#include <cstdint>
#include "pal.h"
#include "reader.h"

namespace bundle
{
    // The Bundle Header (v1)
    // Fixed size thunk (header_fixed_t)
    //   - Major Version     
    //   - Minor Version     
    //   - Number of embedded files
    // Variable size portion:
    //   - Bundle ID (7-bit extension encoded length prefixed string)
    // The Bundle Header (v2) [additional content]
    // Fixed size thunk (header_fixed_v2_t)
    //   - DepsJson Location (Offset, Size)
    //   - RuntimeConfig Location (Offset, Size)
    //   - Flags

#pragma pack(push, 1)
    struct header_fixed_t
    {
    public:
        uint32_t major_version;
        uint32_t minor_version;
        int32_t num_embedded_files;

        bool is_valid() const;
    };

    // netcoreapp3_compat_mode flag is set on a .net5+ app, which chooses to build single-file apps in .netcore3.x compat mode,
    // This indicates that:
    //   All published files are bundled into the app; some of them will be extracted to disk.
    //   AppContext.BaseDirectory is set to the extraction directory (and not the AppHost directory).
    enum header_flags_t : uint64_t
    {
        none = 0,
        netcoreapp3_compat_mode = 1
    };

    struct location_t
    {
    public:
        int64_t offset;
        int64_t size;

        bool is_valid() const { return offset != 0; }
    };

    // header_fixed_v2_t is available in single-file apps targetting .net5+ frameworks.
    // It stores information that facilitates the host to process contents of the bundle without extraction.
    //
    // The location of deps.json and runtimeconfig.json is already available in the Bundle manifest.
    // However, the data is cached here in order to simplify the bundle-processing performed by hostfxr.
    struct header_fixed_v2_t
    {
    public:
        location_t deps_json_location;
        location_t runtimeconfig_json_location;
        header_flags_t flags;

        bool is_netcoreapp3_compat_mode() const { return (flags & header_flags_t::netcoreapp3_compat_mode) != 0; }
    };
#pragma pack(pop)

    struct header_t
    {
    public:
        header_t(uint32_t major_version, uint32_t minor_version, int32_t num_embedded_files)
            : m_num_embedded_files(num_embedded_files)
            , m_major_version(major_version)
            , m_minor_version(minor_version)
            , m_bundle_id()
            , m_v2_header()
        {
        }

        static header_t read(reader_t& reader);
        const pal::string_t& bundle_id() const { return m_bundle_id; }
        int32_t num_embedded_files() const { return m_num_embedded_files; }

        const location_t& deps_json_location() const { return m_v2_header.deps_json_location; }
        const location_t& runtimeconfig_json_location() const { return m_v2_header.runtimeconfig_json_location; }
        bool is_netcoreapp3_compat_mode() const { return m_v2_header.is_netcoreapp3_compat_mode(); }

        const uint32_t major_version() const { return m_major_version; };
        const uint32_t minor_version() const { return m_minor_version; };

    private:
        int32_t m_num_embedded_files;
        uint32_t m_major_version;
        uint32_t m_minor_version;
        pal::string_t m_bundle_id;
        header_fixed_v2_t m_v2_header;
    };
}
#endif // __HEADER_H__
