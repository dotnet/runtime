// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "ijwhost_test.h"
#include <iostream>
#include <pal.h>

namespace
{
    std::vector<char> tostr(const pal::string_t &value)
    {
        std::vector<char> vect;
        pal::pal_utf8string(value, &vect);
        return vect;
    }

    bool try_get_entry_point(const pal::string_t &library_path, const pal::char_t *entry_point, void **entry_point_address)
    {
        pal::dll_t library;
        if (!pal::load_library(&library_path, &library))
        {
            std::cout << "Failed to load library: " << tostr(library_path).data() << std::endl;
            return false;
        }

        std::vector<char> entry_point_name = tostr(entry_point);
        *entry_point_address = pal::get_symbol(library, entry_point_name.data());
        if (*entry_point_address == nullptr)
        {
            std::cout << "Failed to find entry point: " << entry_point_name.data() << std::endl;
            return false;
        }

        return true;
    }

}

bool ijwhost_test::run(const pal::string_t &library_path, const pal::char_t *entry_point)
{
    void *entry_point_address;
    if (!try_get_entry_point(library_path, entry_point, &entry_point_address))
        return false;

    try
    {
        // Test is assuming __cdecl, no arguments, and a void return for simplicity.
        typedef void(__cdecl *entry_point_fn)();
        reinterpret_cast<entry_point_fn>(entry_point_address)();
    }
    catch (...)
    {
        // The entry point will throw in some tests. Catch the exception to prevent the CRT from displaying a modal dialog.
        return false;
    }

    return true;
}

bool ijwhost_test::load_context(const std::vector<pal::string_t> &library_paths, const std::vector<pal::string_t> &entry_points)
{
    if (library_paths.size() != entry_points.size() || library_paths.empty())
        return false;

    for (size_t i = 0; i < library_paths.size(); ++i)
    {
        if (!run(library_paths[i], entry_points[i].c_str()))
            return false;
    }

    return true;
}
