// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "manifest.h"

using namespace bundle;

manifest_t manifest_t::read(reader_t& reader, const header_t& header)
{
    manifest_t manifest;

    for (int32_t i = 0; i < header.num_embedded_files(); i++)
    {
        file_entry_t entry = file_entry_t::read(reader, header.major_version(), header.is_netcoreapp3_compat_mode());
        manifest.files.push_back(std::move(entry));
        manifest.m_files_need_extraction |= entry.needs_extraction();
    }

    return manifest;
}
