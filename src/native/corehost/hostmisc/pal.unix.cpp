// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if defined(TARGET_FREEBSD)
#define _WITH_GETLINE
#endif

#include "pal.h"
#include "utils.h"
#include "trace.h"

#include <cassert>
#include <dlfcn.h>
#include <dirent.h>
#include <pwd.h>
#include <fcntl.h>
#include <fnmatch.h>
#include <ctime>
#include <locale>
#include <pwd.h>
#include "config.h"
#include <minipal/getexepath.h>

#if defined(TARGET_OSX)
#include <mach-o/dyld.h>
#include <sys/param.h>
#include <sys/sysctl.h>
#elif defined(__sun)
#include <sys/utsname.h>
#elif defined(TARGET_FREEBSD)
#include <sys/types.h>
#include <sys/param.h>
#include <sys/sysctl.h>
#endif

#if !HAVE_DIRENT_D_TYPE
#define DT_UNKNOWN 0
#define DT_DIR 4
#define DT_REG 8
#define DT_LNK 10
#endif

#ifdef __linux__
#define PAL_CWD_SIZE 0
#elif defined(MAXPATHLEN)
#define PAL_CWD_SIZE MAXPATHLEN
#elif defined(PATH_MAX)
#define PAL_CWD_SIZE PATH_MAX
#else
#error "Don't know how to obtain max path on this platform"
#endif

pal::string_t pal::get_timestamp()
{
    std::time_t t = std::time(nullptr);
    const std::size_t elems = 100;
    char_t buf[elems];
    std::strftime(buf, elems, _X("%c %Z"), std::gmtime(&t));

    return pal::string_t(buf);
}

bool pal::touch_file(const pal::string_t& path)
{
    int fd = open(path.c_str(), (O_CREAT | O_EXCL), (S_IRUSR | S_IRGRP | S_IROTH));
    if (fd == -1)
    {
        trace::warning(_X("open(%s) failed in %s"), path.c_str(), _STRINGIFY(__FUNCTION__));
        return false;
    }
    (void)close(fd);
    return true;
}

static void* map_file(const pal::string_t& path, size_t* length, int prot, int flags)
{
    int fd = open(path.c_str(), O_RDONLY);
    if (fd == -1)
    {
        trace::error(_X("Failed to map file. open(%s) failed with error %d"), path.c_str(), errno);
        return nullptr;
    }

    struct stat buf;
    if (fstat(fd, &buf) != 0)
    {
        trace::error(_X("Failed to map file. fstat(%s) failed with error %d"), path.c_str(), errno);
        close(fd);
        return nullptr;
    }
    size_t size = buf.st_size;

    if (length != nullptr)
    {
        *length = size;
    }

    void* address = mmap(nullptr, size, prot, flags, fd, 0);

    if (address == MAP_FAILED)
    {
        trace::error(_X("Failed to map file. mmap(%s) failed with error %d"), path.c_str(), errno);
        address = nullptr;
    }

    close(fd);
    return address;
}

const void* pal::mmap_read(const string_t& path, size_t* length)
{
    return map_file(path, length, PROT_READ, MAP_SHARED);
}

void* pal::mmap_copy_on_write(const string_t& path, size_t* length)
{
    return map_file(path, length, PROT_READ | PROT_WRITE, MAP_PRIVATE);
}

bool pal::getcwd(pal::string_t* recv)
{
    recv->clear();
    pal::char_t* buf = ::getcwd(nullptr, PAL_CWD_SIZE);
    if (buf == nullptr)
    {
        if (errno == ENOENT)
        {
            return false;
        }

        trace::error(_X("getcwd() failed: %s"), strerror(errno).c_str());
        return false;
    }

    recv->assign(buf);
    ::free(buf);
    return true;
}

namespace
{
    bool get_loaded_library_from_proc_maps(const pal::char_t* library_name, pal::dll_t* dll, pal::string_t* path)
    {
        char* line = nullptr;
        size_t lineLen = 0;
        ssize_t read;
        FILE* file = pal::file_open(_X("/proc/self/maps"), _X("r"));
        if (file == nullptr)
            return false;

        // Read maps file line by line to check fo the library
        bool found = false;
        pal::string_t path_local;
        while ((read = getline(&line, &lineLen, file)) != -1)
        {
            char buf[PATH_MAX];
            if (sscanf(line, "%*p-%*p %*[-rwxsp] %*p %*[:0-9a-f] %*d %s\n", buf) == 1)
            {
                path_local = buf;
                size_t pos = path_local.rfind(DIR_SEPARATOR);
                if (pos == std::string::npos)
                    continue;

                pos = path_local.find(library_name, pos);
                if (pos != std::string::npos)
                {
                    found = true;
                    break;
                }
            }
        }

        fclose(file);
        if (!found)
            return false;

        pal::dll_t dll_maybe = dlopen(path_local.c_str(), RTLD_LAZY | RTLD_NOLOAD);
        if (dll_maybe == nullptr)
            return false;

        *dll = dll_maybe;
        path->assign(path_local);
        return true;
    }
}

bool pal::get_loaded_library(
    const char_t* library_name,
    const char* symbol_name,
    /*out*/ dll_t* dll,
    /*out*/ pal::string_t* path)
{
    pal::string_t library_name_local;
#if defined(TARGET_OSX)
    if (!pal::is_path_rooted(library_name))
        library_name_local.append("@rpath/");
#endif
    library_name_local.append(library_name);

    dll_t dll_maybe = dlopen(library_name_local.c_str(), RTLD_LAZY | RTLD_NOLOAD);
    if (dll_maybe == nullptr)
    {
        if (pal::is_path_rooted(library_name))
            return false;

        // dlopen on some systems only finds loaded libraries when given the full path
        // Check proc maps as a fallback
        return get_loaded_library_from_proc_maps(library_name, dll, path);
    }

    // Not all systems support getting the path from just the handle (e.g. dlinfo),
    // so we rely on the caller passing in a symbol name so that we get (any) address
    // in the library
    assert(symbol_name != nullptr);
    pal::proc_t proc = pal::get_symbol(dll_maybe, symbol_name);
    Dl_info info;
    if (dladdr(proc, &info) == 0)
    {
        dlclose(dll_maybe);
        return false;
    }

    *dll = dll_maybe;
    path->assign(info.dli_fname);
    return true;
}

bool pal::load_library(const string_t* path, dll_t* dll)
{
    *dll = dlopen(path->c_str(), RTLD_LAZY);
    if (*dll == nullptr)
    {
        trace::error(_X("Failed to load %s, error: %s"), path->c_str(), dlerror());
        return false;
    }
    return true;
}

pal::proc_t pal::get_symbol(dll_t library, const char* name)
{
    auto result = dlsym(library, name);
    if (result == nullptr)
    {
        trace::info(_X("Probed for and did not find library symbol %s, error: %s"), name, dlerror());
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

bool is_read_write_able_directory(pal::string_t& dir)
{
    return pal::realpath(&dir) &&
        (access(dir.c_str(), R_OK | W_OK | X_OK) == 0);
}

bool get_extraction_base_parent_directory(pal::string_t& directory)
{
    // check for the POSIX standard environment variable
    if (pal::getenv(_X("HOME"), &directory))
    {
        if (is_read_write_able_directory(directory))
        {
            return true;
        }
        else
        {
            trace::error(_X("Default extraction directory [%s] either doesn't exist or is not accessible for read/write."), directory.c_str());
        }
    }
    else
    {
        // fallback to the POSIX standard getpwuid() library function
        struct passwd* pwuid = NULL;
        errno = 0;
        do
        {
            pwuid = getpwuid(getuid());
        } while (pwuid == NULL && errno == EINTR);

        if (pwuid != NULL)
        {
            directory.assign(pwuid->pw_dir);
            if (is_read_write_able_directory(directory))
            {
                return true;
            }
            else
            {
                trace::error(_X("Failed to determine default extraction location. Environment variable '$HOME' is not defined and directory reported by getpwuid() [%s] either doesn't exist or is not accessible for read/write."), pwuid->pw_dir);
            }
        }
        else
        {
            trace::error(_X("Failed to determine default extraction location. Environment variable '$HOME' is not defined and getpwuid() returned NULL."));
        }
    }

    return false;
}

bool pal::get_default_bundle_extraction_base_dir(pal::string_t& extraction_dir)
{
    if (!get_extraction_base_parent_directory(extraction_dir))
    {
        return false;
    }

    append_path(&extraction_dir, _X(".net"));
    if (is_read_write_able_directory(extraction_dir))
    {
        return true;
    }

    // Create $HOME/.net with rwx access to the owner
    if (::mkdir(extraction_dir.c_str(), S_IRWXU) == 0)
    {
        return true;
    }
    else if (errno != EEXIST)
    {
        trace::error(_X("Failed to create default extraction directory [%s]. %s"), extraction_dir.c_str(), pal::strerror(errno).c_str());
        return false;
    }

    return is_read_write_able_directory(extraction_dir);
}

bool pal::get_global_dotnet_dirs(std::vector<pal::string_t>* recv)
{
    // No support for global directories in Unix.
    return false;
}

pal::string_t pal::get_dotnet_self_registered_config_location(pal::architecture arch)
{
    pal::string_t config_location = _X("/etc/dotnet");

    //  ***Used only for testing***
    pal::string_t environment_install_location_override;
    if (test_only_getenv(_X("_DOTNET_TEST_INSTALL_LOCATION_PATH"), &environment_install_location_override))
    {
        config_location = environment_install_location_override;
    }

    append_path(&config_location, (_X("install_location_") + to_lower(get_arch_name(arch))).c_str());
    return config_location;
}

namespace
{
    bool get_line_from_file(FILE* pFile, pal::string_t& line)
    {
        line = pal::string_t();
        char buffer[256];
        while (fgets(buffer, sizeof(buffer), pFile))
        {
            line += (pal::char_t*)buffer;
            size_t len = line.length();

            // fgets includes the newline character in the string - so remove it.
            if (len > 0 && line[len - 1] == '\n')
            {
                line.pop_back();
                break;
            }
        }

        return !line.empty();
    }
}

bool get_install_location_from_file(const pal::string_t& file_path, bool& file_found, pal::string_t& install_location)
{
    file_found = true;
    bool install_location_found = false;
    FILE* install_location_file = pal::file_open(file_path, "r");
    if (install_location_file != nullptr)
    {
        if (!get_line_from_file(install_location_file, install_location))
        {
            trace::warning(_X("Did not find any install location in '%s'."), file_path.c_str());
        }
        else
        {
            install_location_found = true;
        }

        fclose(install_location_file);
        if (install_location_found)
            return true;
    }
    else
    {
        if (errno == ENOENT)
        {
            trace::verbose(_X("The install_location file ['%s'] does not exist - skipping."), file_path.c_str());
            file_found = false;
        }
        else
        {
            trace::error(_X("The install_location file ['%s'] failed to open: %s."), file_path.c_str(), pal::strerror(errno).c_str());
        }
    }

    return false;
}

bool pal::get_dotnet_self_registered_dir(pal::string_t* recv)
{
    //  ***Used only for testing***
    pal::string_t environment_override;
    if (test_only_getenv(_X("_DOTNET_TEST_GLOBALLY_REGISTERED_PATH"), &environment_override))
    {
        recv->assign(environment_override);
        return true;
    }
    //  ***************************

    return pal::get_dotnet_self_registered_dir_for_arch(get_current_arch(), recv);
}

bool pal::get_dotnet_self_registered_dir_for_arch(pal::architecture arch, pal::string_t* recv)
{
    recv->clear();

    pal::string_t arch_specific_install_location_file_path = get_dotnet_self_registered_config_location(arch);
    trace::verbose(_X("Looking for architecture-specific install_location file in '%s'."), arch_specific_install_location_file_path.c_str());

    pal::string_t install_location;
    bool file_found = false;
    if (!get_install_location_from_file(arch_specific_install_location_file_path, file_found, install_location))
    {
        if (file_found)
        {
            return false;
        }

        // If looking for the current architecture, also look for the non-architecture-specific file
        if (arch == get_current_arch())
        {
            pal::string_t legacy_install_location_file_path = get_directory(arch_specific_install_location_file_path);
            append_path(&legacy_install_location_file_path, _X("install_location"));
            trace::verbose(_X("Looking for install_location file in '%s'."), legacy_install_location_file_path.c_str());

            if (!get_install_location_from_file(legacy_install_location_file_path, file_found, install_location))
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }

    recv->assign(install_location);
    trace::verbose(_X("Found registered install location '%s'."), recv->c_str());
    return file_found;
}

namespace
{
    bool is_supported_multi_arch_install(pal::architecture arch)
    {
#if defined(TARGET_OSX) && defined(TARGET_ARM64)
        // arm64, looking for x64 install
        return arch == pal::architecture::x64;
#else
        // Others do not support default install locations on a different architecture
        return false;
#endif
    }
}

bool pal::get_default_installation_dir(pal::string_t* recv)
{
    //  ***Used only for testing***
    pal::string_t environmentOverride;
    if (test_only_getenv(_X("_DOTNET_TEST_DEFAULT_INSTALL_PATH"), &environmentOverride))
    {
        recv->assign(environmentOverride);
        return true;
    }
    //  ***************************

    return get_default_installation_dir_for_arch(get_current_arch(), recv);
}

bool pal::get_default_installation_dir_for_arch(pal::architecture arch, pal::string_t* recv)
{
    bool is_current_arch = arch == get_current_arch();

    // Bail out early for unsupported requests for different architectures
    if (!is_current_arch && !is_supported_multi_arch_install(arch))
        return false;

#if defined(TARGET_OSX)
    recv->assign(_X("/usr/local/share/dotnet"));
    if (is_current_arch && pal::is_emulating_x64())
    {
        append_path(recv, get_arch_name(arch));
    }
#if defined(TARGET_ARM64)
    else if (!is_current_arch)
    {
        // Running arm64, looking for x64 install
        assert(arch == pal::architecture::x64);
        append_path(recv, get_arch_name(arch));
    }
#endif
#else
    recv->assign(_X("/usr/share/dotnet"));
#endif
    return true;
}

pal::string_t trim_quotes(pal::string_t stringToCleanup)
{
    pal::char_t quote_array[2] = { '\"', '\'' };
    for (size_t index = 0; index < sizeof(quote_array) / sizeof(quote_array[0]); index++)
    {
        size_t pos = stringToCleanup.find(quote_array[index]);
        while (pos != std::string::npos)
        {
            stringToCleanup = stringToCleanup.erase(pos, 1);
            pos = stringToCleanup.find(quote_array[index]);
        }
    }

    return stringToCleanup;
}

#if defined(TARGET_OSX)
pal::string_t pal::get_current_os_rid_platform()
{
    pal::string_t ridOS;

    char str[256];
    size_t size = sizeof(str);

    // returns something like 10.5.2 or 11.6
    int ret = sysctlbyname("kern.osproductversion", str, &size, nullptr, 0);
    if (ret == 0)
    {
        // the value _should_ be null terminated but let's make sure
        str[size - 1] = 0;

        char* pos = strchr(str, '.');
        if (pos != NULL)
        {
            int major = atoi(str);
            if (major > 11)
            {
                // starting with 12.0 we track only major release
                *pos = 0;

            }
            else if (major == 11)
            {
                // for 11.x we publish RID as 11.0
                // if we return anything else, it would break the RID graph processing
                strcpy(str, "11.0");
            }
            else
            {
                // for 10.x the significant releases are actually the second digit
                pos = strchr(pos + 1, '.');

                if (pos != NULL)
                {
                    // strip anything after second dot and return something like 10.5
                    *pos = 0;
                }
            }
        }

        std::string release(str, strlen(str));
        ridOS.append(_X("osx."));
        ridOS.append(release);
    }

    return ridOS;
}
#elif defined(TARGET_FREEBSD)
// On FreeBSD get major version. Minors should be compatible
pal::string_t pal::get_current_os_rid_platform()
{
    pal::string_t ridOS;
    char str[256];
    size_t size = sizeof(str);
    int ret = sysctlbyname("kern.osrelease", str, &size, NULL, 0);

    if (ret == 0)
    {
        char* pos = strchr(str, '.');
        if (pos)
        {
            ridOS.append(_X("freebsd."))
                .append(str, pos - str);
        }
    }

    return ridOS;
}
#elif defined(TARGET_ILLUMOS)
pal::string_t pal::get_current_os_rid_platform()
{
    // Code:
    //   struct utsname u;
    //   if (uname(&u) != -1)
    //       printf("sysname: %s, release: %s, version: %s, machine: %s\n", u.sysname, u.release, u.version, u.machine);
    //
    // Output examples:
    //   on OmniOS
    //       sysname: SunOS, release: 5.11, version: omnios-r151018-95eaa7e, machine: i86pc
    //   on OpenIndiana Hipster:
    //       sysname: SunOS, release: 5.11, version: illumos-63878f749f, machine: i86pc
    //   on SmartOS:
    //       sysname: SunOS, release: 5.11, version: joyent_20200408T231825Z, machine: i86pc

    pal::string_t ridOS;
    struct utsname utsname_obj;
    if (uname(&utsname_obj) < 0)
    {
        return ridOS;
    }

    if (strncmp(utsname_obj.version, "omnios", strlen("omnios")) == 0)
    {
        ridOS.append(_X("omnios."))
            .append(utsname_obj.version, strlen("omnios-r"), 2); // e.g. omnios.15
    }
    else if (strncmp(utsname_obj.version, "illumos-", strlen("illumos-")) == 0)
    {
        ridOS.append(_X("openindiana")); // version-less
    }
    else if (strncmp(utsname_obj.version, "joyent_", strlen("joyent_")) == 0)
    {
        ridOS.append(_X("smartos."))
            .append(utsname_obj.version, strlen("joyent_"), 4); // e.g. smartos.2020
    }

    return ridOS;
}
#elif defined(__sun)
pal::string_t pal::get_current_os_rid_platform()
{
    // Code:
    //   struct utsname u;
    //   if (uname(&u) != -1)
    //       printf("sysname: %s, release: %s, version: %s, machine: %s\n", u.sysname, u.release, u.version, u.machine);
    //
    // Output example on Solaris 11:
    //       sysname: SunOS, release: 5.11, version: 11.3, machine: i86pc

    pal::string_t ridOS;
    struct utsname utsname_obj;
    if (uname(&utsname_obj) < 0)
    {
        return ridOS;
    }

    char* pos = strchr(utsname_obj.version, '.');
    if (pos)
    {
        ridOS.append(_X("solaris."))
            .append(utsname_obj.version, pos - utsname_obj.version); // e.g. solaris.11
    }

    return ridOS;
}
#else
// For some distros, we don't want to use the full version from VERSION_ID. One example is
// Red Hat Enterprise Linux, which includes a minor version in their VERSION_ID but minor
// versions are backwards compatible.
//
// In this case, we'll normalized RIDs like 'rhel.7.2' and 'rhel.7.3' to a generic
// 'rhel.7'. This brings RHEL in line with other distros like CentOS or Debian which
// don't put minor version numbers in their VERSION_ID fields because all minor versions
// are backwards compatible.
static
pal::string_t normalize_linux_rid(pal::string_t rid)
{
    pal::string_t rhelPrefix(_X("rhel."));
    pal::string_t alpinePrefix(_X("alpine."));
    pal::string_t rockyPrefix(_X("rocky."));
    size_t lastVersionSeparatorIndex = std::string::npos;

    if (rid.compare(0, rhelPrefix.length(), rhelPrefix) == 0)
    {
        lastVersionSeparatorIndex = rid.find(_X("."), rhelPrefix.length());
    }
    else if (rid.compare(0, alpinePrefix.length(), alpinePrefix) == 0)
    {
        size_t secondVersionSeparatorIndex = rid.find(_X("."), alpinePrefix.length());
        if (secondVersionSeparatorIndex != std::string::npos)
        {
            lastVersionSeparatorIndex = rid.find(_X("."), secondVersionSeparatorIndex + 1);
        }
    }
    else if (rid.compare(0, rockyPrefix.length(), rockyPrefix) == 0)
    {
        lastVersionSeparatorIndex = rid.find(_X("."), rockyPrefix.length());
    }

    if (lastVersionSeparatorIndex != std::string::npos)
    {
        rid.erase(lastVersionSeparatorIndex, rid.length() - lastVersionSeparatorIndex);
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

bool pal::get_own_executable_path(pal::string_t* recv)
{
    char* path = minipal_getexepath();
    if (!path)
    {
        return false;
    }

    recv->assign(path);
    free(path);
    return true;
}

bool pal::get_own_module_path(string_t* recv)
{
    Dl_info info;
    if (dladdr((void*)&pal::get_own_module_path, &info) == 0)
        return false;

    recv->assign(info.dli_fname);
    return true;
}

bool pal::get_method_module_path(string_t* recv, void* method)
{
    Dl_info info;
    if (dladdr(method, &info) == 0)
        return false;

    recv->assign(info.dli_fname);
    return true;
}

bool pal::get_module_path(dll_t module, string_t* recv)
{
    return false;
}

bool pal::get_current_module(dll_t* mod)
{
    return false;
}

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

bool pal::realpath(pal::string_t* path, bool skip_error_logging)
{
    auto resolved = ::realpath(path->c_str(), nullptr);
    if (resolved == nullptr)
    {
        if (errno == ENOENT)
        {
            return false;
        }

        if (!skip_error_logging)
        {
            trace::error(_X("realpath(%s) failed: %s"), path->c_str(), strerror(errno).c_str());
        }

        return false;
    }

    path->assign(resolved);
    ::free(resolved);
    return true;
}

bool pal::file_exists(const pal::string_t& path)
{
    return (::access(path.c_str(), F_OK) == 0);
}

static void readdir(const pal::string_t& path, const pal::string_t& pattern, bool onlydirectories, std::vector<pal::string_t>* list)
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

#if HAVE_DIRENT_D_TYPE
            int dirEntryType = entry->d_type;
#else
            int dirEntryType = DT_UNKNOWN;
#endif

            // We are interested in files only
            switch (dirEntryType)
            {
            case DT_DIR:
                break;

            case DT_REG:
                if (onlydirectories)
                {
                    continue;
                }
                break;

                // Handle symlinks and file systems that do not support d_type
            case DT_LNK:
            case DT_UNKNOWN:
            {
                struct stat sb;

                if (fstatat(dirfd(dir), entry->d_name, &sb, 0) == -1)
                {
                    continue;
                }

                if (onlydirectories)
                {
                    if (!S_ISDIR(sb.st_mode))
                    {
                        continue;
                    }
                    break;
                }
                else if (!S_ISREG(sb.st_mode) && !S_ISDIR(sb.st_mode))
                {
                    continue;
                }
            }
            break;

            default:
                continue;
            }

            if (!strcmp(entry->d_name, ".") || !strcmp(entry->d_name, ".."))
            {
                continue;
            }

            files.emplace_back(entry->d_name);
        }

        closedir(dir);
    }
}

void pal::readdir(const string_t& path, const string_t& pattern, std::vector<pal::string_t>* list)
{
    ::readdir(path, pattern, false, list);
}

void pal::readdir(const pal::string_t& path, std::vector<pal::string_t>* list)
{
    ::readdir(path, _X("*"), false, list);
}

void pal::readdir_onlydirectories(const pal::string_t& path, const string_t& pattern, std::vector<pal::string_t>* list)
{
    ::readdir(path, pattern, true, list);
}

void pal::readdir_onlydirectories(const pal::string_t& path, std::vector<pal::string_t>* list)
{
    ::readdir(path, _X("*"), true, list);
}

bool pal::is_running_in_wow64()
{
    return false;
}

bool pal::is_emulating_x64()
{
    int is_translated_process = 0;
#if defined(TARGET_OSX)
    size_t size = sizeof(is_translated_process);
    if (sysctlbyname("sysctl.proc_translated", &is_translated_process, &size, NULL, 0) == -1)
    {
        trace::info(_X("Could not determine whether the current process is running under Rosetta."));
        if (errno != ENOENT)
        {
            trace::info(_X("Call to sysctlbyname failed: %s"), strerror(errno).c_str());
        }

        return false;
    }
#endif

    return is_translated_process == 1;
}

bool pal::are_paths_equal_with_normalized_casing(const string_t& path1, const string_t& path2)
{
#if defined(TARGET_OSX)
    // On Mac, paths are case-insensitive
    return (strcasecmp(path1.c_str(), path2.c_str()) == 0);
#else
    // On Linux, paths are case-sensitive
    return path1 == path2;
#endif
}
