// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "file_entry.h"
#include "trace.h"
#include "dir_utils.h"
#include "error_codes.h"

using namespace bundle;

bool file_entry_t::is_valid() const
{
    return m_offset > 0 && m_size >= 0 &&
        static_cast<file_type_t>(m_type) < file_type_t::__last;
}

file_entry_t file_entry_t::read(reader_t &reader, bool force_extraction)
{
    // First read the fixed-sized portion of file-entry
    const file_entry_fixed_t* fixed_data = reinterpret_cast<const file_entry_fixed_t*>(reader.read_direct(sizeof(file_entry_fixed_t)));
    file_entry_t entry(fixed_data, force_extraction);

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
