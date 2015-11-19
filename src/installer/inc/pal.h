// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef PAL_H
#define PAL_H

#include <string>
#include <vector>
#include <fstream>
#include <sstream>
#include <iostream>
#include <cstring>
#include <cstdarg>
#include <tuple>

#if defined(_WIN32)

#include <Windows.h>

#define HOST_EXE_NAME L"corehost.exe"
#define xerr std::wcerr
#define xout std::wcout
#define DIR_SEPARATOR L'\\'
#define PATH_SEPARATOR L';'
#define PATH_MAX MAX_PATH
#define _X(s) L ## s

#else

#include <cstdlib>
#include <libgen.h>

#define HOST_EXE_NAME "corehost"
#define xerr std::cerr
#define xout std::cout
#define DIR_SEPARATOR '/'
#define PATH_SEPARATOR ':'
#define _X(s) s

#define S_OK        0x00000000
#define E_NOTIMPL   0x80004001
#define E_FAIL      0x80004005

#define SUCCEEDED(Status) ((Status) >= 0)

#endif

#if defined(_WIN32)
#define LIBCORECLR_NAME L"coreclr.dll"
#elif defined(__APPLE__)
#define LIBCORECLR_NAME "libcoreclr.dylib"
#else
#define LIBCORECLR_NAME "libcoreclr.so"
#endif

#if !defined(PATH_MAX) && !defined(_WIN32)
#define PATH_MAX    4096
#endif


namespace pal
{
#if defined(_WIN32)

    typedef wchar_t char_t;
    typedef std::wstring string_t;
    typedef std::wstringstream stringstream_t;
    typedef std::ifstream ifstream_t;
    typedef HRESULT hresult_t;
    typedef HMODULE dll_t;
    typedef FARPROC proc_t;

    inline int strcmp(const char_t* str1, const char_t* str2) { return ::wcscmp(str1, str2); }
    inline int strcasecmp(const char_t* str1, const char_t* str2) { return ::_wcsicmp(str1, str2); }
    inline size_t strlen(const char_t* str) { return ::wcslen(str); }
    inline void err_vprintf(const char_t* format, va_list vl) { ::vfwprintf(stderr, format, vl); ::fputws(_X("\r\n"), stderr); }

    pal::string_t to_palstring(const std::string& str);
    std::string to_stdstring(const pal::string_t& str);
#else
    typedef char char_t;
    typedef std::string string_t;
    typedef std::stringstream stringstream_t;
    typedef std::ifstream ifstream_t;
    typedef long hresult_t;
    typedef void* dll_t;
    typedef void* proc_t;

    inline int strcmp(const char_t* str1, const char_t* str2) { return ::strcmp(str1, str2); }
    inline int strcasecmp(const char_t* str1, const char_t* str2) { return ::strcasecmp(str1, str2); }
    inline size_t strlen(const char_t* str) { return ::strlen(str); }
    inline void err_vprintf(const char_t* format, va_list vl) { ::vfprintf(stderr, format, vl); ::fputc('\n', stderr); }
    inline pal::string_t to_palstring(const std::string& str) { return str; }
    inline std::string to_stdstring(const pal::string_t& str) { return str; }
#endif

    bool realpath(string_t& path);
    bool file_exists(const string_t& path);
    std::vector<pal::string_t> readdir(const string_t& path);

    bool get_own_executable_path(string_t& recv);
    bool getenv(const char_t* name, string_t& recv);
    bool get_default_packages_directory(string_t& recv);
    bool is_path_rooted(const string_t& path);

    int xtoi(const char_t* input);

    bool load_library(const char_t* path, dll_t& dll);
    proc_t get_symbol(dll_t library, const char* name);
    void unload_library(dll_t library);

    bool find_coreclr(pal::string_t& recv);
}

#endif // PAL_H
