// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// C++ implementations of the C PAL API (pal_c.h) for Windows.
// These extern "C" wrappers delegate to the C++ pal:: namespace functions
// so that C code (e.g. apphost_main.c, fxr_resolver.c) can call them.

#include "pal.h"
#include "trace.h"

#include <cassert>
#include <cstdlib>
#include <cstring>
#include <vector>

extern "C"
{
#include "pal_c.h"
}

extern "C" bool pal_get_own_executable_path(pal_char_t* recv, size_t recv_len)
{
    pal::string_t path;
    if (!pal::get_own_executable_path(&path))
        return false;
    if (path.length() >= recv_len)
        return false;
    memcpy(recv, path.c_str(), (path.length() + 1) * sizeof(pal_char_t));
    return true;
}

extern "C" bool pal_fullpath(pal_char_t* path, size_t path_len)
{
    pal::string_t str(path);
    if (!pal::fullpath(&str))
        return false;
    if (str.length() >= path_len)
        return false;
    memcpy(path, str.c_str(), (str.length() + 1) * sizeof(pal_char_t));
    return true;
}

extern "C" bool pal_file_exists(const pal_char_t* path)
{
    return pal::file_exists(pal::string_t(path));
}

extern "C" bool pal_directory_exists(const pal_char_t* path)
{
    return pal::directory_exists(pal::string_t(path));
}

extern "C" bool pal_is_path_fully_qualified(const pal_char_t* path)
{
    return pal::is_path_fully_qualified(pal::string_t(path));
}

extern "C" bool pal_load_library(const pal_char_t* path, void** dll)
{
    pal::string_t str(path);
    return pal::load_library(&str, reinterpret_cast<pal::dll_t*>(dll));
}

extern "C" void pal_unload_library(void* library)
{
    pal::unload_library(reinterpret_cast<pal::dll_t>(library));
}

extern "C" void* pal_get_symbol(void* library, const char* name)
{
    return reinterpret_cast<void*>(pal::get_symbol(reinterpret_cast<pal::dll_t>(library), name));
}

extern "C" void pal_err_print_line(const pal_char_t* message)
{
    pal::err_print_line(message);
}

extern "C" void pal_readdir_onlydirectories(const pal_char_t* path, pal_readdir_callback_fn callback, void* context)
{
    std::vector<pal::string_t> entries;
    pal::readdir_onlydirectories(pal::string_t(path), &entries);
    for (const auto& entry : entries)
    {
        // pal::readdir_onlydirectories returns just the entry name (not the full path)
        if (!callback(entry.c_str(), context))
            break;
    }
}

extern "C" bool pal_get_dotnet_self_registered_dir(pal_char_t* recv, size_t recv_len)
{
    pal::string_t dir;
    if (!pal::get_dotnet_self_registered_dir(&dir))
        return false;
    if (dir.length() >= recv_len)
        return false;
    memcpy(recv, dir.c_str(), (dir.length() + 1) * sizeof(pal_char_t));
    return true;
}

extern "C" bool pal_get_default_installation_dir(pal_char_t* recv, size_t recv_len)
{
    pal::string_t dir;
    if (!pal::get_default_installation_dir(&dir))
        return false;
    if (dir.length() >= recv_len)
        return false;
    memcpy(recv, dir.c_str(), (dir.length() + 1) * sizeof(pal_char_t));
    return true;
}

extern "C" const pal_char_t* pal_get_dotnet_self_registered_config_location(pal_char_t* buf, size_t buf_len)
{
    // On Windows, the self-registered config location is stored in the registry,
    // not in a file. Return empty string since the file-based lookup is not applicable.
    if (buf_len > 0)
        buf[0] = L'\0';
    return buf;
}
