// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal.h"
#include "trace.h"
#include "utils.h"

#include <cassert>
#include <locale>
#include <codecvt>
#include <ShlObj.h>

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

void pal::setup_api_sets(const std::unordered_set<pal::string_t>& api_sets)
{
    if (api_sets.empty())
    {
        return;
    }

    pal::string_t path;

    (void) getenv(_X("PATH"), &path);

    // We need this ugly hack, as the PInvoked DLL's static dependencies can come from
    // some other NATIVE_DLL_SEARCH_DIRECTORIES and not necessarily side by side. However,
    // CoreCLR.dll loads PInvoke DLLs with LOAD_WITH_ALTERED_SEARCH_PATH. Note that this
    // option cannot be combined with LOAD_LIBRARY_SEARCH_USER_DIRS, so the AddDllDirectory
    // doesn't help much in telling CoreCLR where to load the PInvoke DLLs from.
    // So we resort to modifying the PATH variable on our own hoping Windows loader will do the right thing.
    for (const auto& as : api_sets)
    {
        // AddDllDirectory is still needed for Standalone App's CoreCLR load.
        ::AddDllDirectory(as.c_str());

        // Path patch is needed for static dependencies of a PInvoked DLL load out of the nuget cache.
        path.push_back(PATH_SEPARATOR);
        path.append(as);
    }

    trace::verbose(_X("Setting PATH=%s"), path.c_str());

    ::SetEnvironmentVariableW(_X("PATH"), path.c_str());
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

bool pal::load_library(const char_t* path, dll_t* dll)
{
    // LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR:
    //   In portable apps, coreclr would come from another directory than the host,
    //   so make sure coreclr dependencies can be resolved from coreclr.dll load dir.
    *dll = ::LoadLibraryExW(path, NULL, LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR | LOAD_LIBRARY_SEARCH_DEFAULT_DIRS);
    if (*dll == nullptr)
    {
        trace::error(_X("Failed to load the dll from [%s], HRESULT: 0x%X"), path, HRESULT_FROM_WIN32(GetLastError()));
        return false;
    }

    // Pin the module
    HMODULE dummy_module;
    if (!::GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_PIN, path, &dummy_module))
    {
        trace::error(_X("Failed to pin library [%s] in [%s]"), path, _STRINGIFY(__FUNCTION__));
        return false;
    }

    if (trace::is_enabled())
    {
        pal::char_t buf[PATH_MAX];
        ::GetModuleFileNameW(*dll, buf, PATH_MAX);
        trace::info(_X("Loaded library from %s"), buf);
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

bool pal::get_default_breadcrumb_store(string_t* recv)
{
    recv->clear();

    pal::char_t* prog_dat;
    HRESULT hr = ::SHGetKnownFolderPath(FOLDERID_ProgramData, 0,  NULL, &prog_dat);
    if (hr != S_OK)
    {
        trace::verbose(_X("Failed to read default breadcrumb store 0x%X"), hr);
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
    recv->clear();

    // See https://github.com/dotnet/cli/issues/2179
#if defined(_TARGET_AMD64_)
    if (!pal::getenv(_X("ProgramFiles(x86)"), recv))
#else
    // In WOW64 mode, PF maps to PFx86.
    if (!pal::getenv(_X("ProgramFiles"), recv))
#endif
    {
        return false;
    }

    append_path(recv, _X("coreservicing"));
    return true;
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
    char_t program_path[MAX_PATH];
    DWORD dwModuleFileName = ::GetModuleFileNameW(NULL, program_path, MAX_PATH);
    if (dwModuleFileName == 0 || dwModuleFileName >= MAX_PATH) {
        return false;
    }
    recv->assign(program_path);
    return true;
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

bool pal::realpath(string_t* path)
{
    char_t buf[MAX_PATH];
    auto res = ::GetFullPathNameW(path->c_str(), MAX_PATH, buf, nullptr);
    if (res == 0 || res > MAX_PATH)
    {
        trace::error(_X("Error resolving full path [%s]"), path->c_str());
        return false;
    }
    path->assign(buf);
    return true;
}

bool pal::file_exists(const string_t& path)
{
    if (path.empty())
    {
        return false;
    }

    WIN32_FIND_DATAW data;
    auto find_handle = ::FindFirstFileW(path.c_str(), &data);
    bool found = find_handle != INVALID_HANDLE_VALUE;
    ::FindClose(find_handle);
    return found;
}

void pal::readdir(const string_t& path, const string_t& pattern, std::vector<pal::string_t>* list)
{
    assert(list != nullptr);

    std::vector<string_t>& files = *list;

    string_t search_string(path);
    append_path(&search_string, pattern.c_str());

    WIN32_FIND_DATAW data = { 0 };
    auto handle = ::FindFirstFileExW(search_string.c_str(), FindExInfoStandard, &data, FindExSearchNameMatch, NULL, 0);
    if (handle == INVALID_HANDLE_VALUE)
    {
        return;
    }
    do
    {
        string_t filepath(data.cFileName);
        files.push_back(filepath);
    } while (::FindNextFileW(handle, &data));
    ::FindClose(handle);
}

void pal::readdir(const string_t& path, std::vector<pal::string_t>* list)
{
    pal::readdir(path, _X("*"), list);
}

