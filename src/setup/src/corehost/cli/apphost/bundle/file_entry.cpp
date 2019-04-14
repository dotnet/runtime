// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "bundle_runner.h"
#include "pal.h"
#include "error_codes.h"
#include "trace.h"
#include "utils.h"

using namespace bundle;

bool file_entry_t::is_valid()
{
    return data.offset > 0 && data.size > 0 &&
        (file_type_t)data.type < file_type_t::__last &&
        data.path_length > 0 && data.path_length <= PATH_MAX;
}

file_entry_t* file_entry_t::read(FILE* stream)
{
    file_entry_t* entry = new file_entry_t();

    // First read the fixed-sized portion of file-entry
    bundle_runner_t::read(&entry->data, sizeof(entry->data), stream);
    if (!entry->is_valid())
    {
        trace::error(_X("Failure processing application bundle; possible file corruption."));
        trace::error(_X("Invalid FileEntry detected."));
        throw StatusCode::BundleExtractionFailure;
    }

    // Read the relative-path, given its length 
    pal::string_t& path = entry->relative_path;
    bundle_runner_t::read_string(path, entry->data.path_length, stream);

    // Fixup the relative-path to have current platform's directory separator.
    if (bundle_dir_separator != DIR_SEPARATOR)
    {
        for (size_t pos = path.find(bundle_dir_separator);
            pos != pal::string_t::npos;
            pos = path.find(bundle_dir_separator, pos))
        {
            path[pos] = DIR_SEPARATOR;
        }
    }

    return entry;
}


