// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    //   - CompressedSize  - only in bundleVersion 6+
    //   - File Entry Type       
    // Variable Size portion
    //   - relative path (7-bit extension encoded length prefixed string)

#pragma pack(push, 1)
    struct file_entry_fixed_t
    {
        int64_t offset;
        int64_t size;
        int64_t compressedSize;
        file_type_t type;
    };
#pragma pack(pop)

    class file_entry_t
    {
    public:
        file_entry_t()
            : m_offset(0)
            , m_size(0)
            , m_compressedSize(0)
            , m_type(file_type_t::__last)
            , m_relative_path()
            , m_disabled(false)
            , m_force_extraction(false)
        {
        }

        file_entry_t(
            const file_entry_fixed_t *fixed_data,
            const bool force_extraction = false)
            : m_relative_path()
            , m_disabled(false)
            , m_force_extraction(force_extraction)
        {
            // File_entries in the bundle-manifest are expected to be used 
            // beyond startup (for loading files directly from bundle, lazy extraction, etc.).
            // The contents of fixed_data are copied on to file_entry in order to 
            // avoid memory mapped IO later.

            m_offset = fixed_data->offset;
            m_size = fixed_data->size;
            m_compressedSize = fixed_data->compressedSize;
            m_type = fixed_data->type;
        }

        const pal::string_t relative_path() const { return m_relative_path; }
        int64_t offset() const { return m_offset; }
        int64_t size() const { return m_size; }
        int64_t compressedSize() const { return m_compressedSize; }
        file_type_t type() const { return m_type; }
        void disable() { m_disabled = true; }
        bool is_disabled() const { return m_disabled; }
        bool needs_extraction() const;
        bool matches(const pal::string_t& path) const { return (pal::pathcmp(relative_path(), path) == 0) && !is_disabled(); }

        static file_entry_t read(reader_t &reader, uint32_t bundle_major_version, bool force_extraction);

    private:
        int64_t m_offset;
        int64_t m_size;
        int64_t m_compressedSize;
        file_type_t m_type;
        pal::string_t m_relative_path; // Path of an embedded file, relative to the extraction directory.
        // If the file represented by this entry is also found in a servicing location, the servicing location must take precedence.
        // But in general, bundle will take precedence over on-disk locations everywhere.
        // So in order to make sure that the servicing location is used, the file entry in the bundle is marked as "disabled"
        // in such case, and the lookup logic will behave as if the file is not present in the bundle.
        bool m_disabled;
        bool m_force_extraction;
        bool is_valid() const;
    };
}
#endif // __FILE_ENTRY_H__
