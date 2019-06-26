// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __FILE_ENTRY_H__
#define __FILE_ENTRY_H__

#include <cstdint>
#include "file_type.h"
#include "pal.h"

namespace bundle
{

    // FileEntry: Records information about embedded files.
    // 
    // The bundle manifest records the following meta-data for each 
    // file embedded in the bundle:
    // Fixed size portion (m_data)
    //   - Offset     
    //   - Size       
    //   - File Entry Type       
    //   - path-length  (7-bit extension encoding, 1 Byte due to MAX_PATH)
    // Variable Size portion
    //   - relative path  ("path-length" Bytes)

    class file_entry_t
    {
    private:

        // The inner structure represents the fields that can be 
        // read contiguously for every file_entry. 
#pragma pack(push, 1)
        struct
        {
            int64_t offset;
            int64_t size;
            file_type_t type;
            int8_t path_length_byte_1;
        } m_data;
#pragma pack(pop)

        pal::string_t m_relative_path; // Path of an embedded file, relative to the extraction directory.

    public:
        file_entry_t()
            :m_data(), m_relative_path()
        {
        }

        const pal::string_t& relative_path() { return m_relative_path; }
        int64_t offset() { return m_data.offset; }
        int64_t size() { return m_data.size; }
        file_type_t type() { return m_data.type; }

        static file_entry_t* read(FILE* stream);

    private:
        static const pal::char_t bundle_dir_separator = '/';
        bool is_valid();
    };
}
#endif // __FILE_ENTRY_H__
