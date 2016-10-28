// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal.h"
#include "utils.h"
#include "trace.h"

#include <cassert>
#include <dlfcn.h>
#include <dirent.h>
#include <sys/stat.h>
#include <sys/types.h>
#include <pwd.h>
#include <unistd.h>
#include <fcntl.h>
#include <fnmatch.h>

#if defined(__APPLE__)
#include <mach-o/dyld.h>
#include <sys/param.h>
#include <sys/sysctl.h>
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

bool pal::touch_file(const pal::string_t& path)
{
    int fd = open(path.c_str(), (O_CREAT | O_EXCL), (S_IRUSR | S_IRGRP | S_IROTH));
    if (fd == -1)
    {
        trace::warning(_X("open(%s) failed in %s"), path.c_str(), _STRINGIFY(__FUNCTION__));
        return false;
    }
    (void) close(fd);
    return true;
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

bool pal::get_default_breadcrumb_store(string_t* recv)
{
    recv->clear();
    pal::string_t ext;
    if (pal::getenv(_X("CORE_BREADCRUMBS"), &ext) && pal::realpath(&ext))
    {
        // We should have the path in ext.
        trace::info(_X("Realpath CORE_BREADCRUMBS [%s]"), ext.c_str());
    }

    if (!pal::directory_exists(ext))
    {
        trace::info(_X("Directory core breadcrumbs [%s] was not specified or found"), ext.c_str());
        ext.clear();
        append_path(&ext, _X("opt"));
        append_path(&ext, _X("corebreadcrumbs"));
        if (!pal::directory_exists(ext))
        {
            trace::info(_X("Fallback directory core breadcrumbs at [%s] was not found"), ext.c_str());
            return false;
        }
    }

    if (access(ext.c_str(), (R_OK | W_OK)) != 0)
    {
        trace::info(_X("Breadcrumb store [%s] is not ACL-ed with rw-"), ext.c_str());
    }

    recv->assign(ext);
    return true;
}

bool pal::get_default_servicing_directory(string_t* recv)
{
    recv->clear();
    pal::string_t ext;
    if (pal::getenv(_X("CORE_SERVICING"), &ext) && pal::realpath(&ext))
    {
        // We should have the path in ext.
        trace::info(_X("Realpath CORE_SERVICING [%s]"), ext.c_str());
    }
    
    if (!pal::directory_exists(ext))
    {
        trace::info(_X("Directory core servicing at [%s] was not specified or found"), ext.c_str());
        ext.clear();
        append_path(&ext, _X("opt"));
        append_path(&ext, _X("coreservicing"));
        if (!pal::directory_exists(ext))
        {
            trace::info(_X("Fallback directory core servicing at [%s] was not found"), ext.c_str());
            return false;
        }
    }

    if (access(ext.c_str(), R_OK) != 0)
    {
        trace::info(_X("Directory core servicing at [%s] was not ACL-ed properly"), ext.c_str());
    }

    recv->assign(ext);
    trace::info(_X("Using core servicing at [%s]"), ext.c_str());
    return true;
}

#if defined(__APPLE__)
bool pal::get_os_moniker(os_moniker_t* moniker)
{
    char str[256];

    size_t size = sizeof(str);
    int ret = sysctlbyname("kern.osrelease", str, &size, NULL, 0);
    if (ret != 0)
    {
        return false;
    }

    std::string release(str, size);
    size_t pos = release.find('.');
    if (pos == std::string::npos)
    {
        return false;
    }

    *moniker = (os_moniker_t) stoi(release.substr(0, pos));
    return true;
}
#endif

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
#elif defined(__FreeBSD__)
bool pal::get_own_executable_path(pal::string_t* recv)
{
    int mib[4];
    mib[0] = CTL_KERN;
    mib[1] = KERN_PROC;
    mib[2] = KERN_PROC_PATHNAME;
    mib[3] = -1;
    char buf[PATH_MAX];
    size_t cb = sizeof(buf);
    if (sysctl(mib, 4, buf, &cb, NULL, 0) == 0)
    {
        recv->assign(buf);
        return true;
    }
    // ENOMEM
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

void pal::readdir(const string_t& path, const string_t& pattern, std::vector<pal::string_t>* list)
{
    assert(list != nullptr);

    std::vector<pal::string_t>& files = *list;

    auto dir = opendir(path.c_str());
    if (dir != nullptr)
    {
        struct dirent* entry = nullptr;
        while ((entry = readdir(dir)) != nullptr)
        {
            if (fnmatch(pattern.c_str(), entry->d_name, FNM_PATHNAME) != 0)
            {
                continue;
            }
             
            // We are interested in files only
            switch (entry->d_type)
            {
            case DT_DIR:
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

                    if (!S_ISREG(sb.st_mode) && !S_ISDIR(sb.st_mode))
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

void pal::readdir(const pal::string_t& path, std::vector<pal::string_t>* list)
{
    readdir(path, _X("*"), list);
}
