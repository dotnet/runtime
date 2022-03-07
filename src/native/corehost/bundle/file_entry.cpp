// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "file_entry.h"
#include "trace.h"
#include "dir_utils.h"
#include "error_codes.h"

using namespace bundle;

bool file_entry_t::is_valid() const
{
    return m_offset > 0 && m_size >= 0 && m_compressedSize >= 0 &&
        static_cast<file_type_t>(m_type) < file_type_t::__last;
}

file_entry_t file_entry_t::read(reader_t &reader, uint32_t bundle_major_version, bool force_extraction)
{
    // First read the fixed-sized portion of file-entry
    file_entry_fixed_t fixed_data;

    // NB: the file data is potentially unaligned, thus we use "read" to fetch 64bit values
    reader.read(&fixed_data.offset, sizeof(int64_t));
    reader.read(&fixed_data.size, sizeof(int64_t));

    // compressedSize is present only in v6+ headers
    fixed_data.compressedSize = 0;
    if (bundle_major_version >= 6)
    {
        reader.read(&fixed_data.compressedSize, sizeof(int64_t));
    }

    fixed_data.type   = (file_type_t)reader.read_byte();

    file_entry_t entry(&fixed_data, force_extraction);

    if (!entry.is_valid())
    {
        trace::error(_X("Failure processing application bundle; possible file corruption."));
        trace::error(_X("Invalid FileEntry detected."));
        throw StatusCode::BundleExtractionFailure;
    }

    reader.read_path_string(entry.m_relative_path);
    dir_utils_t::fixup_path_separator(entry.m_relative_path);

    return entry;
}

bool file_entry_t::needs_extraction() const
{
    if (m_force_extraction)
        return true;

    switch (m_type)
    {
    case file_type_t::deps_json:
    case file_type_t::runtime_config_json:
    case file_type_t::assembly:
        return false;

    default:
        return true;
    }
}
