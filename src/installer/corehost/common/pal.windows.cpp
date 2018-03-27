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

bool GetModuleFileNameWrapper(HMODULE hModule, pal::string_t* recv)
{
    pal::string_t path;
    DWORD dwModuleFileName = MAX_PATH / 2;

    do
    {
        path.resize(dwModuleFileName * 2);
        dwModuleFileName = GetModuleFileNameW(hModule, (LPWSTR)path.data(), path.size());
    } while (dwModuleFileName == path.size());

    if (dwModuleFileName != 0)
    {
        *recv = path;
        return true;
    }

    return false;

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
    return ::GetProcAddress(library, name);
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

bool pal::get_global_dotnet_dirs(std::vector<pal::string_t>* dirs)
{
    pal::string_t dir;
    if (!get_default_installation_dir(&dir))
    {
        return false;
    }

    dirs->push_back(dir);
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
bool pal::realpath(string_t* path)
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
        trace::error(_X("Error resolving full path [%s]"), path->c_str());
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
            trace::error(_X("Error resolving full path [%s]"), path->c_str());
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
    return pal::realpath(&tmp);
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
