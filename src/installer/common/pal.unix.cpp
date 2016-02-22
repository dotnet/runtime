// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal.h"
#include "utils.h"
#include "trace.h"

#include <cassert>
#include <dlfcn.h>
#include <dirent.h>
#include <sys/stat.h>

#include <unistd.h>

#if defined(__APPLE__)
#include <mach-o/dyld.h>
#endif

#if defined(__LINUX__)
#define symlinkEntrypointExecutable "/proc/self/exe"
#elif !defined(__APPLE__)
#define symlinkEntrypointExecutable "/proc/curproc/exe"
#endif

pal::string_t pal::to_string(int value) { return std::to_string(value); }

pal::string_t pal::to_lower(const pal::string_t& in)
{
    pal::string_t ret = in;
    std::transform(ret.begin(), ret.end(), ret.begin(), ::tolower);
    return ret;
}

bool pal::getcwd(pal::string_t* recv)
{
    recv->clear();
    pal::char_t* buf = ::getcwd(nullptr, PATH_MAX + 1);
    if (buf == nullptr)
    {
        if (errno == ENOENT)
        {
            return false;
        }
        perror("getcwd()");
        return false;
    }
    recv->assign(buf);
    ::free(buf);
    return true;
}

bool pal::find_coreclr(pal::string_t* recv)
{
    pal::string_t candidate;
    pal::string_t test;

    // Try /usr/share/dotnet and /usr/local/share/dotnet/cli
    // TODO: These paths should be consistent
    candidate.assign("/usr/share/dotnet/runtime/coreclr");
    if (coreclr_exists_in_dir(candidate)) {
        recv->assign(candidate);
        return true;
    }

    candidate.assign("/usr/local/share/dotnet/runtime/coreclr");
    if (coreclr_exists_in_dir(candidate)) {
        recv->assign(candidate);
        return true;
    }
    return false;
}

bool pal::load_library(const char_t* path, dll_t* dll)
{
    *dll = dlopen(path, RTLD_LAZY);
    if (*dll == nullptr)
    {
        trace::error(_X("Failed to load %s, error: %s"), path, dlerror());
        return false;
    }
    return true;
}

pal::proc_t pal::get_symbol(dll_t library, const char* name)
{
    auto result = dlsym(library, name);
    if (result == nullptr)
    {
        trace::error(_X("Failed to resolve library symbol %s, error: %s"), name, dlerror());
    }
    return result;
}

void pal::unload_library(dll_t library)
{
    if (dlclose(library) != 0)
    {
        trace::warning(_X("Failed to unload library, error: %s"), dlerror());
    }
}

int pal::xtoi(const char_t* input)
{
    return atoi(input);
}

bool pal::is_path_rooted(const pal::string_t& path)
{
    return path.front() == '/';
}

bool pal::get_default_packages_directory(pal::string_t* recv)
{
    recv->clear();
    if (!pal::getenv("HOME", recv))
    {
        return false;
    }
    append_path(&*recv, _X(".nuget"));
    append_path(&*recv, _X("packages"));
    return true;
}

#if defined(__APPLE__)
bool pal::get_own_executable_path(pal::string_t* recv)
{
    uint32_t path_length = 0;
    if (_NSGetExecutablePath(nullptr, &path_length) == -1)
    {
        char path_buf[path_length];
        if (_NSGetExecutablePath(path_buf, &path_length) == 0)
        {
            recv->assign(path_buf);
            return true;
        }
    }
    return false;
}
#else
bool pal::get_own_executable_path(pal::string_t* recv)
{
    // Just return the symlink to the exe from /proc
    // We'll call realpath on it later
    recv->assign(symlinkEntrypointExecutable);
    return true;
}
#endif

// Returns true only if an env variable can be read successfully to be non-empty.
bool pal::getenv(const pal::char_t* name, pal::string_t* recv)
{
    recv->clear();

    auto result = ::getenv(name);
    if (result != nullptr)
    {
        recv->assign(result);
    }

    return (recv->length() > 0);
}

bool pal::realpath(pal::string_t* path)
{
    auto resolved = ::realpath(path->c_str(), nullptr);
    if (resolved == nullptr)
    {
        if (errno == ENOENT)
        {
            return false;
        }
        perror("realpath()");
        return false;
    }
    path->assign(resolved);
    ::free(resolved);
    return true;
}

bool pal::file_exists(const pal::string_t& path)
{
    if (path.empty())
    {
        return false;
    }
    struct stat buffer;
    return (::stat(path.c_str(), &buffer) == 0);
}

void pal::readdir(const pal::string_t& path, std::vector<pal::string_t>* list)
{
    assert(list != nullptr);

    std::vector<pal::string_t>& files = *list;

    auto dir = opendir(path.c_str());
    if (dir != nullptr)
    {
        struct dirent* entry = nullptr;
        while((entry = readdir(dir)) != nullptr)
        {
            // We are interested in files only
            switch (entry->d_type)
            {
            case DT_REG:
                break;

            // Handle symlinks and file systems that do not support d_type
            case DT_LNK:
            case DT_UNKNOWN:
                {
                    std::string fullFilename;

                    fullFilename.append(path);
                    fullFilename.push_back(DIR_SEPARATOR);
                    fullFilename.append(entry->d_name);

                    struct stat sb;
                    if (stat(fullFilename.c_str(), &sb) == -1)
                    {
                        continue;
                    }

                    if (!S_ISREG(sb.st_mode))
                    {
                        continue;
                    }
                }
                break;

            default:
                continue;
            }

            files.push_back(pal::string_t(entry->d_name));
        }
    }
}
