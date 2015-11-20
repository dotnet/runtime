// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "utils.h"
#include "trace.h"

bool coreclr_exists_in_dir(const pal::string_t& candidate)
{
    pal::string_t test(candidate);
    append_path(test, LIBCORECLR_NAME);
    trace::verbose(_X("checking for CoreCLR in default location: %s"), test.c_str());
    return pal::file_exists(test);
}

bool ends_with(const pal::string_t& value, const pal::string_t& suffix)
{
    return suffix.length() <= value.length() &&
        (0 == value.compare(value.length() - suffix.length(), suffix.length(), suffix));
}

void append_path(pal::string_t& path1, const pal::char_t* path2)
{
    if (pal::is_path_rooted(path2))
    {
        path1.assign(path2);
    }
    else
    {
        if (path1.back() != DIR_SEPARATOR)
        {
            path1.push_back(DIR_SEPARATOR);
        }
        path1.append(path2);
    }
}

pal::string_t get_executable(const pal::string_t& filename)
{
    pal::string_t result(filename);

    if (ends_with(result, _X(".exe")))
    {
        // We need to strip off the old extension
        result.erase(result.length() - 4);
    }

    return result;
}

pal::string_t get_filename(const pal::string_t& path)
{
    // Find the last dir separator
    auto path_sep = path.find_last_of(DIR_SEPARATOR);
    if (path_sep == pal::string_t::npos)
    {
        return pal::string_t(path);
    }

    return path.substr(path_sep + 1);
}

pal::string_t get_directory(const pal::string_t& path)
{
    // Find the last dir separator
    auto path_sep = path.find_last_of(DIR_SEPARATOR);
    if (path_sep == pal::string_t::npos)
    {
        return pal::string_t(path);
    }

    return path.substr(0, path_sep);
}
