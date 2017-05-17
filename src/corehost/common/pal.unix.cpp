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
    pal::char_t* buf = ::getcwd(nullptr, 0);
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

bool pal::load_library(const string_t* path, dll_t* dll)
{
    *dll = dlopen(path->c_str(), RTLD_LAZY);
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

static
bool is_executable(const pal::string_t& file_path)
{
    struct stat st;
    if (::stat(file_path.c_str(), &st) < 0)
    {
        return false;
    }

    return ((st.st_mode & S_IEXEC) != 0);
}

static
bool locate_dotnet_on_path(pal::string_t* dotnet_exe)
{
    pal::string_t path;
    if (!pal::getenv(_X("PATH"), &path))
    {
        return false;
    }
    
    pal::string_t tok;
    pal::stringstream_t ss(path);
    while (std::getline(ss, tok, PATH_SEPARATOR))
    {
        size_t start_pos = tok.find_first_not_of(_X(" \t"));
        if (start_pos == pal::string_t::npos)
        {
            continue;
        }

        append_path(&tok, _X("dotnet"));
        if (pal::realpath(&tok) && is_executable(tok))
        {
            *dotnet_exe = tok;
            return true;
        }
        tok.clear();
    }
    return false;
}

bool pal::get_local_dotnet_dir(pal::string_t* recv)
{
    recv->clear();
    pal::string_t dir;
    if (!pal::getenv("HOME", &dir))
    {
        struct passwd* pw = getpwuid(getuid());
        if (pw && pw->pw_dir)
        {
            dir.assign(pw->pw_dir);
        }
    }
    if (dir.empty())
    {
        return false;
    }
    append_path(&dir, _X(".dotnet"));
    append_path(&dir, get_arch());
    recv->assign(dir);
    return true;
}

 bool pal::get_global_dotnet_dir(pal::string_t* recv)
{
    recv->clear();
    pal::string_t dotnet_exe;
    if (!locate_dotnet_on_path(&dotnet_exe))
    {
        return false;
    }
    pal::string_t dir = get_directory(dotnet_exe);
    recv->assign(dir);
    return true;
}

pal::string_t trim_quotes(pal::string_t stringToCleanup)
{
    pal::char_t quote_array[2] = {'\"', '\''};
    for(int index = 0; index < sizeof(quote_array)/sizeof(quote_array[0]); index++)
    {
        size_t pos = stringToCleanup.find(quote_array[index]);
        while(pos != std::string::npos)
        {
            stringToCleanup = stringToCleanup.erase(pos, 1);
            pos = stringToCleanup.find(quote_array[index]);
        }
    }

    return stringToCleanup;
}

#if defined(__APPLE__)
pal::string_t pal::get_current_os_rid_platform()
{
    pal::string_t ridOS;
    
    char str[256];

    // There is no good way to get the visible version of OSX (i.e. something like 10.x.y) as
    // certain APIs work till 10.9 and have been deprecated and others require linking against
    // UI frameworks to get the data.
    //
    // We will, instead, use kern.osrelease and use its major version number
    // as a means to formulate the OSX 10.X RID.
    //
    // Needless to say, this will need to be updated if OSX RID were to become 11.* ever.
    size_t size = sizeof(str);
    int ret = sysctlbyname("kern.osrelease", str, &size, NULL, 0);
    if (ret == 0)
    {
        std::string release(str, size);
        size_t pos = release.find('.');
        if (pos != std::string::npos)
        {
            // Extract the major version and subtract 4 from it
            // to get the Minor version used in OSX versioning scheme.
            // That is, given a version 10.X.Y, we will get X below.
            int minorVersion = stoi(release.substr(0, pos)) - 4;
            if (minorVersion < 10)
            {
                // On OSX, our minimum supported RID is 10.12.
                minorVersion = 12;
            }

            ridOS.append(_X("osx.10."));
            ridOS.append(pal::to_string(minorVersion));
        }
    }

    return ridOS;
}
#else
// For some distros, we don't want to use the full version from VERSION_ID. One example is
// Red Hat Enterprise Linux, which includes a minor version in their VERSION_ID but minor
// versions are backwards compatable.
//
// In this case, we'll normalized RIDs like 'rhel.7.2' and 'rhel.7.3' to a generic
// 'rhel.7'. This brings RHEL in line with other distros like CentOS or Debian which
// don't put minor version numbers in their VERSION_ID fields because all minor versions
// are backwards compatible.
static
pal::string_t normalize_linux_rid(pal::string_t rid)
{
    pal::string_t rhelPrefix(_X("rhel."));

    if (rid.compare(0, rhelPrefix.length(), rhelPrefix) == 0)
    {
        size_t minorVersionSeparatorIndex = rid.find(_X("."), rhelPrefix.length());
        if (minorVersionSeparatorIndex != std::string::npos)
        {
            rid.erase(minorVersionSeparatorIndex, rid.length() - minorVersionSeparatorIndex);
        }
    }

    return rid;
}

pal::string_t pal::get_current_os_rid_platform()
{
    pal::string_t ridOS;
    pal::string_t versionFile(_X("/etc/os-release"));

    if (pal::file_exists(versionFile))
    {
        // Read the file to get ID and VERSION_ID data that will be used
        // to construct the RID.
        std::fstream fsVersionFile;
        
        fsVersionFile.open(versionFile, std::fstream::in);

        // Proceed only if we were able to open the file
        if (fsVersionFile.good())
        {
            pal::string_t line;
            pal::string_t strID(_X("ID="));
            pal::string_t valID;
            pal::string_t strVersionID(_X("VERSION_ID="));
            pal::string_t valVersionID;

            bool fFoundID = false, fFoundVersion = false;

            // Read the first line
            std::getline(fsVersionFile, line);

            // Loop until we are at the end of file
            while (!fsVersionFile.eof())
            {
                // Look for ID if we have not found it already
                if (!fFoundID)
                {
                    size_t pos = line.find(strID);
                    if ((pos != std::string::npos) && (pos == 0))
                    {
                        valID.append(line.substr(3));
                        fFoundID = true;
                    }
                }

                // Look for VersionID if we have not found it already
                if (!fFoundVersion)
                {
                    size_t pos = line.find(strVersionID);
                    if ((pos != std::string::npos) && (pos == 0))
                    {
                        valVersionID.append(line.substr(11));
                        fFoundVersion = true;
                    }
                }

                if (fFoundID && fFoundVersion)
                {
                    // We have everything we need to form the RID - break out of the loop.
                    break;
                }

                // Read the next line
                std::getline(fsVersionFile, line);
            }

            // Close the file now that we are done with it.
            fsVersionFile.close();

            if (fFoundID)
            {
                ridOS.append(valID);
            }
            
            if (fFoundVersion)
            {
                ridOS.append(_X("."));
                ridOS.append(valVersionID);
            }

            if (fFoundID || fFoundVersion)
            {
                // Remove any double-quotes
                ridOS = trim_quotes(ridOS);
            }
        }
    }

    return normalize_linux_rid(ridOS);
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
    int error_code = 0;
    error_code = sysctl(mib, 4, buf, &cb, NULL, 0);
    if (error_code == 0)
    {
        recv->assign(buf);
        return true;
    }
    
    // ENOMEM
    if (error_code == ENOMEM)
    {
        size_t len = sysctl(mib, 4, NULL, NULL, NULL, 0);
        std::unique_ptr<char[]> buffer = new (std::nothrow) char[len];

        if (buffer == NULL)
        {
            return false;
        }

        error_code = sysctl(mib, 4, buffer, &len, NULL, 0);
        if (error_code == 0)
        {
            recv->assign(buffer);
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
