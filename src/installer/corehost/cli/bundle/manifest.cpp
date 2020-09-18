// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "manifest.h"

using namespace bundle;

manifest_t manifest_t::read(reader_t& reader, header_t& header)
{
    manifest_t manifest;
    manifest.m_netcoreapp3_compat_mode = header.is_netcoreapp3_compat_mode();

    for (int32_t i = 0; i < header.num_embedded_files(); i++)
    {
        file_entry_t entry = file_entry_t::read(reader);
        manifest.files.push_back(std::move(entry));
        manifest.m_files_need_extraction |= entry.needs_extraction();
    }

    manifest.m_files_need_extraction |= manifest.is_netcoreapp3_compat_mode();
    return manifest;
}
