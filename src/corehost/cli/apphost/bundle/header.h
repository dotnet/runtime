// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __HEADER_H__
#define __HEADER_H__

#include <cstdint>
#include "pal.h"

namespace bundle
{
    // The Bundle Header contains:
    // Fixed size thunk (m_data)
    //   - Major Version     
    //   - Minor Version     
    //   - Number of embedded files
    //   - Bundle ID length 
    // Variable size portion:
    //   - Bundle ID ("Bundle ID length" bytes)

    struct header_t
    {
    public:
        header_t()
            :m_data(), m_bundle_id()
        {
        }

        bool is_valid();
        static header_t* read(FILE* stream);
        const pal::string_t& bundle_id() { return m_bundle_id; }
        int32_t num_embedded_files() { return m_data.num_embedded_files;  }

    private:
#pragma pack(push, 1)
        struct
        {
            uint32_t major_version;
            uint32_t minor_version;
            int32_t num_embedded_files;
            int8_t bundle_id_length_byte_1;
        } m_data;
#pragma pack(pop)
        pal::string_t m_bundle_id;

        const uint32_t current_major_version = 1;
        const uint32_t current_minor_version = 0;
    };
}
#endif // __HEADER_H__
