// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal.h"
#include "trace.h"
#include "utils.h"

#include <cassert>
#include <locale>
#include <codecvt>

static std::wstring_convert<std::codecvt_utf8<wchar_t>, wchar_t> g_converter;

bool pal::find_coreclr(pal::string_t* recv)
{
    pal::string_t candidate;

    // Try %LocalAppData%\dotnet
    if (pal::getenv(_X("LocalAppData"), &candidate)) {
        append_path(&candidate, _X("dotnet"));
        append_path(&candidate, _X("runtime"));
        append_path(&candidate, _X("coreclr"));
        if (coreclr_exists_in_dir(candidate)) {
            recv->assign(candidate);
            return true;
        }
    }


    // Try %ProgramFiles%. Note this works for both x86 and x64/wow64 as per:
    // https://msdn.microsoft.com/en-us/library/windows/desktop/aa384274(v=vs.85).aspx

    // candidate.clear(); getenv clears it.
    if (pal::getenv(_X("ProgramFiles"), &candidate)) {
        append_path(&candidate, _X("dotnet"));
        append_path(&candidate, _X("bin"));
        if (coreclr_exists_in_dir(candidate)) {
            recv->assign(candidate);
            return true;
        }
    }
    return false;
}

bool pal::load_library(const char_t* path, dll_t* dll)
{
    *dll = ::LoadLibraryW(path);
    if (*dll == nullptr)
    {
        trace::error(_X("Failed to load the dll from %s, HRESULT: 0x%X"), path, HRESULT_FROM_WIN32(GetLastError()));
        return false;
    }

    // Pin the module
    HMODULE dummy_module;
    if (!::GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_PIN, path, &dummy_module))
    {
        trace::error(_X("Failed to pin library: %s"));
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

bool pal::get_default_packages_directory(string_t* recv)
{
    recv->clear();
    if (!pal::getenv(_X("USERPROFILE"), recv))
    {
        return false;
    }
    append_path(&*recv, _X(".nuget"));
    append_path(&*recv, _X("packages"));
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
            trace::error(_X("Failed to read environment variable '%s', HRESULT: 0x%X"), name, HRESULT_FROM_WIN32(GetLastError()));
        }
        return false;
    }
    auto buf = new char_t[length];
    if (::GetEnvironmentVariableW(name, buf, length) == 0)
    {
        trace::error(_X("Failed to read environment variable '%s', HRESULT: 0x%X"), name, HRESULT_FROM_WIN32(GetLastError()));
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

std::string pal::to_stdstring(const string_t& str)
{
    return g_converter.to_bytes(str);
}

pal::string_t pal::to_palstring(const std::string& str)
{
    return g_converter.from_bytes(str);
}

void pal::to_palstring(const char* str, pal::string_t* out)
{
    out->assign(g_converter.from_bytes(str));
}

void pal::to_stdstring(const pal::char_t* str, std::string* out)
{
    out->assign(g_converter.to_bytes(str));
}

bool pal::realpath(string_t* path)
{
    char_t buf[MAX_PATH];
    auto res = ::GetFullPathNameW(path->c_str(), MAX_PATH, buf, nullptr);
    if (res == 0 || res > MAX_PATH)
    {
        trace::error(_X("Error resolving path: %s"), path->c_str());
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

void pal::readdir(const string_t& path, std::vector<pal::string_t>* list)
{
    assert(list != nullptr);

    std::vector<string_t>& files = *list;

    string_t search_string(path);
    search_string.push_back(DIR_SEPARATOR);
    search_string.push_back(L'*');

    WIN32_FIND_DATAW data;
    auto handle = ::FindFirstFileW(search_string.c_str(), &data);
    do
    {
        string_t filepath(data.cFileName);
        files.push_back(filepath);
    } while (::FindNextFileW(handle, &data));
    ::FindClose(handle);
}
