// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "bundle_runner.h"
#include "pal.h"
#include "error_codes.h"
#include "trace.h"
#include "utils.h"

using namespace bundle;

manifest_t manifest_t::read(FILE* stream, int32_t num_files)
{
    manifest_t manifest;

    for (int32_t i = 0; i < num_files; i++)
    {
        manifest.files.emplace_back(file_entry_t::read(stream));
    }

    return manifest;
}
