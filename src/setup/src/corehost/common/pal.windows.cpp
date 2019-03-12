// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal.h"
#include "trace.h"
#include "utils.h"
#include "longfile.h"

#include <cassert>
#include <locale>
#include <codecvt>
#include <ShlObj.h>
#include <ctime>

bool GetModuleFileNameWrapper(HMODULE hModule, pal::string_t* recv)
{
    pal::string_t path;
    size_t dwModuleFileName = MAX_PATH / 2;

    do
    {
        path.resize(dwModuleFileName * 2);
        dwModuleFileName = GetModuleFileNameW(hModule, (LPWSTR)path.data(), static_cast<DWORD>(path.size()));
    } while (dwModuleFileName == path.size());

    if (dwModuleFileName == 0)
        return false;

    path.resize(dwModuleFileName);
    *recv = path;
    return true;
}

bool GetModuleHandleFromAddress(void *addr, HMODULE *hModule)
{
    BOOL res = ::GetModuleHandleExW(
        GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
        reinterpret_cast<LPCWSTR>(addr),
        hModule);

    return (res != FALSE);
}

pal::string_t pal::to_lower(const pal::string_t& in)
{
    pal::string_t ret = in;
    std::transform(ret.begin(), ret.end(), ret.begin(), ::towlower);
    return ret;
}

pal::string_t pal::to_string(int value)
{
    return std::to_wstring(value);
}

pal::string_t pal::get_timestamp()
{
    std::time_t t = std::time(0);
    const std::size_t elems = 100;
    char_t buf[elems];
    std::wcsftime(buf, elems, _X("%c GMT"), std::gmtime(&t));

    return pal::string_t(buf);
}

bool pal::touch_file(const pal::string_t& path)
{
    HANDLE hnd = ::CreateFileW(path.c_str(), 0, 0, NULL, CREATE_NEW, FILE_ATTRIBUTE_NORMAL, NULL);
    if (hnd == INVALID_HANDLE_VALUE)
    {
        trace::verbose(_X("Failed to leave breadcrumb, HRESULT: 0x%X"), HRESULT_FROM_WIN32(GetLastError()));
        return false;
    }
    ::CloseHandle(hnd);
    return true;
}

bool pal::getcwd(pal::string_t* recv)
{
    recv->clear();

    pal::char_t buf[MAX_PATH];
    DWORD result = GetCurrentDirectoryW(MAX_PATH, buf);
    if (result < MAX_PATH)
    {
        recv->assign(buf);
        return true;
    }
    else if (result != 0)
    {
        std::vector<pal::char_t> str;
        str.resize(result);
        result = GetCurrentDirectoryW(str.size(), str.data());
        assert(result <= str.size());
        if (result != 0)
        {
            recv->assign(str.data());
            return true;
        }
    }
    assert(result == 0);
    trace::error(_X("Failed to obtain working directory, HRESULT: 0x%X"), HRESULT_FROM_WIN32(GetLastError()));
    return false;
}

bool pal::load_library(const string_t* in_path, dll_t* dll)
{
    string_t path = *in_path;

    // LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR:
    //   In framework-dependent apps, coreclr would come from another directory than the host,
    //   so make sure coreclr dependencies can be resolved from coreclr.dll load dir.

    if (LongFile::IsPathNotFullyQualified(path))
    {
        if (!pal::realpath(&path))
        {
            trace::error(_X("Failed to load the dll from [%s], HRESULT: 0x%X"), path.c_str(), HRESULT_FROM_WIN32(GetLastError()));
            return false;
        }
    }
    
    //Adding the assert to ensure relative paths which are not just filenames are not used for LoadLibrary Calls
    assert(!LongFile::IsPathNotFullyQualified(path) || !LongFile::ContainsDirectorySeparator(path));

    *dll = ::LoadLibraryExW(path.c_str(), NULL, LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR | LOAD_LIBRARY_SEARCH_DEFAULT_DIRS);
    if (*dll == nullptr)
    {
        trace::error(_X("Failed to load the dll from [%s], HRESULT: 0x%X"), path.c_str(), HRESULT_FROM_WIN32(GetLastError()));
        return false;
    }

    // Pin the module
    HMODULE dummy_module;
    if (!::GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_PIN, path.c_str(), &dummy_module))
    {
        trace::error(_X("Failed to pin library [%s] in [%s]"), path.c_str(), _STRINGIFY(__FUNCTION__));
        return false;
    }

    if (trace::is_enabled())
    {
        string_t buf;
        GetModuleFileNameWrapper(*dll, &buf);
        trace::info(_X("Loaded library from %s"), buf.c_str());
    }

    return true;
}

pal::proc_t pal::get_symbol(dll_t library, const char* name)
{
    auto result = ::GetProcAddress(library, name);
    if (result == nullptr)
    {
        trace::info(_X("Probed for and did not resolve library symbol %S"), name);
    }

    return result;
}

void pal::unload_library(dll_t library)
{
    // No-op. On windows, we pin the library, so it can't be unloaded.
}

static
bool get_wow_mode_program_files(pal::string_t* recv)
{
#if defined(_TARGET_AMD64_)
    pal::char_t* env_key = _X("ProgramFiles(x86)");
#else
    pal::char_t* env_key = _X("ProgramFiles");
#endif

    return get_file_path_from_env(env_key,recv);
}

bool pal::get_default_breadcrumb_store(string_t* recv)
{
    recv->clear();

    pal::string_t prog_dat;
    if (!get_file_path_from_env(_X("ProgramData"), &prog_dat))
    {
        // We should have the path in prog_dat.
        trace::verbose(_X("Failed to read default breadcrumb store [%s]"), prog_dat.c_str());
        return false;
    }
    recv->assign(prog_dat);
    append_path(recv, _X("Microsoft"));
    append_path(recv, _X("NetFramework"));
    append_path(recv, _X("BreadcrumbStore"));
    return true;
}

bool pal::get_default_servicing_directory(string_t* recv)
{
    if (!get_wow_mode_program_files(recv))
    {
        return false;
    }
    append_path(recv, _X("coreservicing"));
    return true;
}

bool pal::get_default_installation_dir(pal::string_t* recv)
{
    pal::char_t* program_files_dir;
    if (pal::is_running_in_wow64())
    {
        program_files_dir = _X("ProgramFiles(x86)");
    }
    else
    {
        program_files_dir = _X("ProgramFiles");
    }

    if (!get_file_path_from_env(program_files_dir, recv))
    {
        return false;
    }

    append_path(recv, _X("dotnet"));

    return true;
}

bool pal::get_dotnet_self_registered_dir(pal::string_t* recv)
{
#if !defined(_TARGET_AMD64_) && !defined(_TARGET_X86_)
    //  Self-registered SDK installation directory is only supported for x64 and x86 architectures.
    return false;
#else
    recv->clear();

    //  ***Used only for testing***
    pal::string_t environmentOverride;
    if (pal::getenv(_X("_DOTNET_TEST_SDK_SELF_REGISTERED_DIR"), &environmentOverride))
    {
        recv->assign(environmentOverride);
        return true;
    }
    //  ***************************

    DWORD size = 0;
    HKEY hkeyHive = HKEY_LOCAL_MACHINE;
    // The registry search occurs in the 32-bit registry in all cases.
    pal::string_t dotnet_key_path = pal::string_t(_X("SOFTWARE\\dotnet"));

    pal::string_t environmentRegistryPathOverride;
    if (pal::getenv(_X("_DOTNET_TEST_SDK_REGISTRY_PATH"), &environmentRegistryPathOverride))
    {
        pal::string_t hkcuPrefix = _X("HKEY_CURRENT_USER\\");
        if (environmentRegistryPathOverride.substr(0, hkcuPrefix.length()) == hkcuPrefix)
        {
            hkeyHive = HKEY_CURRENT_USER;
            environmentRegistryPathOverride = environmentRegistryPathOverride.substr(hkcuPrefix.length());
        }

        dotnet_key_path = environmentRegistryPathOverride;
    }

    pal::string_t sub_key = dotnet_key_path + pal::string_t(_X("\\Setup\\InstalledVersions\\")) + get_arch();
    pal::char_t* value = _X("InstallLocation");

    // Must use RegOpenKeyEx to be able to specify KEY_WOW64_32KEY to access the 32-bit registry in all cases.
    // The RegGetValue has this option available only on Win10.
    HKEY hkey = NULL;
    LSTATUS result = ::RegOpenKeyExW(hkeyHive, sub_key.c_str(), 0, KEY_READ | KEY_WOW64_32KEY, &hkey);
    if (result != ERROR_SUCCESS)
    {
        trace::verbose(_X("Can't open the SDK installed location registry key, result: 0x%X"), result);
        return false;
    }

    // Determine the size of the buffer
    result = ::RegGetValueW(hkey, nullptr, value, RRF_RT_REG_SZ, nullptr, nullptr, &size);
    if (result != ERROR_SUCCESS || size == 0)
    {
        trace::verbose(_X("Can't get the size of the SDK location registry value or it's empty, result: 0x%X"), result);
        ::RegCloseKey(hkey);
        return false;
    }

    // Get the key's value
    std::vector<pal::char_t> buffer(size/sizeof(pal::char_t));
    result = ::RegGetValueW(hkey, nullptr, value, RRF_RT_REG_SZ, nullptr, &buffer[0], &size);
    if (result != ERROR_SUCCESS)
    {
        trace::verbose(_X("Can't get the value of the SDK location registry value, result: 0x%X"), result);
        ::RegCloseKey(hkey);
        return false;
    }

    recv->assign(buffer.data());
    ::RegCloseKey(hkey);
    return true;
#endif
}

bool pal::get_global_dotnet_dirs(std::vector<pal::string_t>* dirs)
{
    pal::string_t default_dir;
    pal::string_t custom_dir;
    bool dir_found = false;
    if (pal::get_dotnet_self_registered_dir(&custom_dir))
    {
        dirs->push_back(custom_dir);
        dir_found = true;
    }
    if (get_default_installation_dir(&default_dir))
    {
        // Avoid duplicate global dirs.
        if (!dir_found || !are_paths_equal_with_normalized_casing(custom_dir, default_dir))
        {
            dirs->push_back(default_dir);
            dir_found = true;
        }
    }
    return dir_found;
}

// To determine the OS version, we are going to use RtlGetVersion API
// since GetVersion call can be shimmed on Win8.1+.
typedef NTSTATUS (WINAPI *pFuncRtlGetVersion)(RTL_OSVERSIONINFOW *);

pal::string_t pal::get_current_os_rid_platform()
{
    pal::string_t ridOS;
    
    RTL_OSVERSIONINFOW osinfo;

    // Init the buffer
    ZeroMemory(&osinfo, sizeof(osinfo));
    osinfo.dwOSVersionInfoSize = sizeof(osinfo);
    HMODULE hmodNtdll = LoadLibrary("ntdll.dll");
    if (hmodNtdll != NULL)
    {
        pFuncRtlGetVersion pRtlGetVersion = (pFuncRtlGetVersion)GetProcAddress(hmodNtdll, "RtlGetVersion");
        if (pRtlGetVersion)
        {
            if ((*pRtlGetVersion)(&osinfo) == 0)
            {
                // Win7 RID is the minimum supported version.
                int majorVer = 6;
                int minorVer = 1;

                if (osinfo.dwMajorVersion > majorVer)
                {
                    majorVer = osinfo.dwMajorVersion;
                    
                    // Reset the minor version since we picked a different major version.
                    minorVer = 0;
                }

                if (osinfo.dwMinorVersion > minorVer)
                {
                    minorVer = osinfo.dwMinorVersion;
                }

                if (majorVer == 6)
                {
                    switch(minorVer)
                    {
                        case 1:
                            ridOS.append(_X("win7"));
                            break;
                        case 2:
                            ridOS.append(_X("win8"));
                            break;
                        case 3:
                        default: 
                            // For unknown version, we will support the highest RID that we know for this major version.
                            ridOS.append(_X("win81"));
                            break;
                    }
                }
                else if (majorVer >= 10)
                {
                    // Return the major version for use in RID computation without applying any cap.
                    ridOS.append(_X("win"));
                    ridOS.append(pal::to_string(majorVer));
                }
            }
        }
    }
    
    return ridOS;
}

bool pal::is_path_rooted(const string_t& path)
{
    return path.length() >= 2 && path[1] == L':';
}

// Returns true only if an env variable can be read successfully to be non-empty.
bool pal::getenv(const char_t* name, string_t* recv)
{
    recv->clear();

    auto length = ::GetEnvironmentVariableW(name, nullptr, 0);
    if (length == 0)
    {
        auto err = GetLastError();
        if (err != ERROR_ENVVAR_NOT_FOUND)
        {
            trace::error(_X("Failed to read environment variable [%s], HRESULT: 0x%X"), name, HRESULT_FROM_WIN32(GetLastError()));
        }
        return false;
    }
    auto buf = new char_t[length];
    if (::GetEnvironmentVariableW(name, buf, length) == 0)
    {
        trace::error(_X("Failed to read environment variable [%s], HRESULT: 0x%X"), name, HRESULT_FROM_WIN32(GetLastError()));
        return false;
    }

    recv->assign(buf);
    delete[] buf;

    return true;
}

int pal::xtoi(const char_t* input)
{
    return ::_wtoi(input);
}

bool pal::get_own_executable_path(string_t* recv)
{
    return GetModuleFileNameWrapper(NULL, recv);
}

bool pal::get_current_module(dll_t *mod)
{
    HMODULE hmod = nullptr;
    if (!GetModuleHandleFromAddress(&get_current_module, &hmod))
        return false;

    *mod = (pal::dll_t)hmod;
    return true;
}

bool pal::get_own_module_path(string_t* recv)
{
    HMODULE hmod;
    if (!GetModuleHandleFromAddress(&get_own_module_path, &hmod))
        return false;

    return GetModuleFileNameWrapper(hmod, recv);
}

static bool wchar_convert_helper(DWORD code_page, const char* cstr, int len, pal::string_t* out)
{
    out->clear();

    // No need of explicit null termination, so pass in the actual length.
    size_t size = ::MultiByteToWideChar(code_page, 0, cstr, len, nullptr, 0);
    if (size == 0)
    {
        return false;
    }
    out->resize(size, '\0');
    return ::MultiByteToWideChar(code_page, 0, cstr, len, &(*out)[0], out->size()) != 0;
}

bool pal::utf8_palstring(const std::string& str, pal::string_t* out)
{
    return wchar_convert_helper(CP_UTF8, &str[0], str.size(), out);
}

bool pal::pal_utf8string(const pal::string_t& str, std::vector<char>* out)
{
    out->clear();

    // Pass -1 as we want explicit null termination in the char buffer.
    size_t size = ::WideCharToMultiByte(CP_UTF8, 0, str.c_str(), -1, nullptr, 0, nullptr, nullptr);
    if (size == 0)
    {
        return false;
    }
    out->resize(size, '\0');
    return ::WideCharToMultiByte(CP_UTF8, 0, str.c_str(), -1, out->data(), out->size(), nullptr, nullptr) != 0;
}

bool pal::pal_clrstring(const pal::string_t& str, std::vector<char>* out)
{
    return pal_utf8string(str, out);
}

bool pal::clr_palstring(const char* cstr, pal::string_t* out)
{
    return wchar_convert_helper(CP_UTF8, cstr, ::strlen(cstr), out);
}

// Return if path is valid and file exists, return true and adjust path as appropriate.
bool pal::realpath(string_t* path, bool skip_error_logging)
{
    if (LongFile::IsNormalized(path->c_str()))
    {
        WIN32_FILE_ATTRIBUTE_DATA data;
        if (GetFileAttributesExW(path->c_str(), GetFileExInfoStandard, &data) != 0)
        {
            return true;
        }
    }

    char_t buf[MAX_PATH];
    auto size = ::GetFullPathNameW(path->c_str(), MAX_PATH, buf, nullptr);
    if (size == 0)
    {
        if (!skip_error_logging)
        {
            trace::error(_X("Error resolving full path [%s]"), path->c_str());
        }
        return false;
    }

    string_t str;
    if (size < MAX_PATH)
    {
        str.assign(buf);
    }
    else
    {
        str.resize(size + LongFile::UNCExtendedPathPrefix.length(), 0);

        size = ::GetFullPathNameW(path->c_str(), size, (LPWSTR)str.data(), nullptr);
        assert(size <= str.size());

        if (size == 0)
        {
            if (!skip_error_logging)
            {
                trace::error(_X("Error resolving full path [%s]"), path->c_str());
            }
            return false;
        }

        const string_t* prefix = &LongFile::ExtendedPrefix;
        //Check if the resolved path is a UNC. By default we assume relative path to resolve to disk 
        if (str.compare(0, LongFile::UNCPathPrefix.length(), LongFile::UNCPathPrefix) == 0)
        {
            prefix = &LongFile::UNCExtendedPathPrefix;
            str.erase(0, LongFile::UNCPathPrefix.length());
            size = size - LongFile::UNCPathPrefix.length();
        }

        str.insert(0, *prefix);
        str.resize(size + prefix->length());
        str.shrink_to_fit();
    }

    WIN32_FILE_ATTRIBUTE_DATA data;
    if (GetFileAttributesExW(str.c_str(), GetFileExInfoStandard, &data) != 0)
    {
        *path = str;
        return true;
    }

    return false;
}

bool pal::file_exists(const string_t& path)
{
    if (path.empty())
    {
        return false;
    }

    string_t tmp(path);
    return pal::realpath(&tmp, true);
}

static void readdir(const pal::string_t& path, const pal::string_t& pattern, bool onlydirectories, std::vector<pal::string_t>* list)
{
    assert(list != nullptr);

    std::vector<pal::string_t>& files = *list;
    pal::string_t normalized_path(path);

    if (LongFile::ShouldNormalize(normalized_path))
    {
        if (!pal::realpath(&normalized_path))
        {
            return;
        }
    }

    pal::string_t search_string(normalized_path);
    append_path(&search_string, pattern.c_str());

    WIN32_FIND_DATAW data = { 0 };

    auto handle = ::FindFirstFileExW(search_string.c_str(), FindExInfoStandard, &data, FindExSearchNameMatch, NULL, 0);
    if (handle == INVALID_HANDLE_VALUE)
    {
        return;
    }
    do
    {
        if (!onlydirectories || (data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY))
        {
            pal::string_t filepath(data.cFileName);
            if (filepath != _X(".") && filepath != _X(".."))
            {
                files.push_back(filepath);
            }
        }
    } while (::FindNextFileW(handle, &data));
    ::FindClose(handle);
}

void pal::readdir(const string_t& path, const string_t& pattern, std::vector<pal::string_t>* list)
{
    ::readdir(path, pattern, false, list);
}

void pal::readdir(const string_t& path, std::vector<pal::string_t>* list)
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
    BOOL fWow64Process = FALSE;
    if (!IsWow64Process(GetCurrentProcess(), &fWow64Process))
    {
        return false;
    }
    return (fWow64Process != FALSE);
}

bool pal::are_paths_equal_with_normalized_casing(const string_t& path1, const string_t& path2)
{
    // On Windows, paths are case-insensitive
    return (strcasecmp(path1.c_str(), path2.c_str()) == 0);
}
