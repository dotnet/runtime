// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __FILE_ENTRY_H__
#define __FILE_ENTRY_H__

#include "file_type.h"
#include "reader.h"

namespace bundle
{
    // FileEntry: Records information about embedded files.
    // 
    // The bundle manifest records the following meta-data for each 
    // file embedded in the bundle:
    // Fixed size portion (file_entry_fixed_t)
    //   - Offset     
    //   - Size       
    //   - File Entry Type       
    // Variable Size portion
    //   - relative path (7-bit extension encoded length prefixed string)

#pragma pack(push, 1)
    struct file_entry_fixed_t
    {
        int64_t offset;
        int64_t size;
        file_type_t type;
    };
#pragma pack(pop)

    class file_entry_t
    {
    public:
        file_entry_t()
            : m_offset(0)
            , m_size(0)
            , m_type(bundle::file_type_t::__last)
            , m_relative_path()
        {
        }

        file_entry_t(const file_entry_fixed_t *fixed_data)
            :m_relative_path()
        {
            // File_entries in the bundle-manifest are expected to be used 
            // beyond startup (for loading files directly from bundle, lazy extraction, etc.).
            // The contents of fixed_data are copied on to file_entry in order to 
            // avoid memory mapped IO later.

            m_offset = fixed_data->offset;
            m_size = fixed_data->size;
            m_type = fixed_data->type;
        }

        const pal::string_t relative_path() const { return m_relative_path; }
        int64_t offset() const { return m_offset; }
        int64_t size() const { return m_size; }
        file_type_t type() const { return m_type; }

        static file_entry_t read(reader_t &reader);

    private:
        int64_t m_offset;
        int64_t m_size;
        file_type_t m_type;
        pal::string_t m_relative_path; // Path of an embedded file, relative to the extraction directory.
        bool is_valid() const;
    };
}
#endif // __FILE_ENTRY_H__
