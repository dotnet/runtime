// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "manifest.h"

using namespace bundle;

manifest_t manifest_t::read(reader_t& reader, int32_t num_files)
{
    manifest_t manifest;

    for (int32_t i = 0; i < num_files; i++)
    {
        manifest.files.emplace_back(file_entry_t::read(reader));
    }

    return manifest;
}
