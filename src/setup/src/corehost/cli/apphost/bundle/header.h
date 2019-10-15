// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __HEADER_H__
#define __HEADER_H__

#include <cstdint>
#include "pal.h"
#include "reader.h"

namespace bundle
{
    // The Bundle Header contains:
    // Fixed size thunk (header_fixed_t)
    //   - Major Version     
    //   - Minor Version     
    //   - Number of embedded files
    // Variable size portion:
    //   - Bundle ID (7-bit extension encoded length prefixed string)

#pragma pack(push, 1)
    struct header_fixed_t
    {
    public:
        uint32_t major_version;
        uint32_t minor_version;
        int32_t num_embedded_files;

        bool is_valid() const;
    };
#pragma pack(pop)

    struct header_t
    {
    public:
        header_t(int32_t num_embedded_files = 0)
            : m_num_embedded_files(num_embedded_files)
            , m_bundle_id()
        {
        }

        static header_t read(reader_t& reader);
        const pal::string_t& bundle_id() { return m_bundle_id; }
        int32_t num_embedded_files() { return m_num_embedded_files;  }

        static const uint32_t major_version = 1;
        static const uint32_t minor_version = 0;

    private:
        int32_t m_num_embedded_files;
        pal::string_t m_bundle_id;

    };
}
#endif // __HEADER_H__
