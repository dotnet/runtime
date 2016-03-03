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
#include <unordered_map>
#include <memory>
#include <algorithm>

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
#define LIB_PREFIX
#define MAKE_LIBNAME(NAME) (_X(NAME) _X(".dll"))
#elif defined(__APPLE__)
#define LIB_PREFIX _X("lib")
#define MAKE_LIBNAME(NAME) (LIB_PREFIX _X(NAME) _X(".dylib"))
#else
#define LIB_PREFIX _X("lib")
#define MAKE_LIBNAME(NAME) (LIB_PREFIX _X(NAME) _X(".so"))
#endif

#define LIBCORECLR_FILENAME (LIB_PREFIX _X("coreclr"))
#define LIBCORECLR_NAME MAKE_LIBNAME("coreclr")

#if !defined(PATH_MAX) && !defined(_WIN32)
#define PATH_MAX    4096
#endif


namespace pal
{
#if defined(_WIN32)
    #ifdef COREHOST_MAKE_DLL
        #define SHARED_API extern "C" __declspec(dllexport)
    #else
        #define SHARED_API
    #endif

    #define STDMETHODCALLTYPE __stdcall

    typedef wchar_t char_t;
    typedef std::wstring string_t;
    typedef std::wstringstream stringstream_t;
    // TODO: Agree on the correct encoding of the files: The PoR for now is to
    // temporarily wchar for Windows and char for Unix. Current implementation
    // implicitly expects the contents on both Windows and Unix as char and
    // converts them to wchar in code for Windows. This line should become:
    // typedef std::basic_ifstream<pal::char_t> ifstream_t.
    typedef std::basic_ifstream<char> ifstream_t;
    typedef std::istreambuf_iterator<ifstream_t::char_type> istreambuf_iterator_t;
    typedef HRESULT hresult_t;
    typedef HMODULE dll_t;
    typedef FARPROC proc_t;

    inline int strcmp(const char_t* str1, const char_t* str2) { return ::wcscmp(str1, str2); }
    inline int strcasecmp(const char_t* str1, const char_t* str2) { return ::_wcsicmp(str1, str2); }
    inline int strncmp(const char_t* str1, const char_t* str2, int len) { return ::wcsncmp(str1, str2, len); }
    inline int strncasecmp(const char_t* str1, const char_t* str2, int len) { return ::_wcsnicmp(str1, str2, len); }

    inline size_t strlen(const char_t* str) { return ::wcslen(str); }
    inline void err_vprintf(const char_t* format, va_list vl) { ::vfwprintf(stderr, format, vl); ::fputws(_X("\r\n"), stderr); }

    pal::string_t to_palstring(const std::string& str);
    std::string to_stdstring(const pal::string_t& str);
    void to_palstring(const char* str, pal::string_t* out);
    void to_stdstring(const pal::char_t* str, std::string* out);
#else
    #ifdef COREHOST_MAKE_DLL
        #define SHARED_API extern "C"
    #else
        #define SHARED_API
    #endif

    #define __cdecl    /* nothing */
    #define __stdcall  /* nothing */
    #define __fastcall /* nothing */
    #define STDMETHODCALLTYPE __stdcall

    typedef char char_t;
    typedef std::string string_t;
    typedef std::stringstream stringstream_t;
    typedef std::basic_ifstream<char> ifstream_t;
    typedef std::istreambuf_iterator<ifstream_t::char_type> istreambuf_iterator_t;
    typedef int hresult_t;
    typedef void* dll_t;
    typedef void* proc_t;

    inline int strcmp(const char_t* str1, const char_t* str2) { return ::strcmp(str1, str2); }
    inline int strcasecmp(const char_t* str1, const char_t* str2) { return ::strcasecmp(str1, str2); }
    inline int strncmp(const char_t* str1, const char_t* str2, int len) { return ::strncmp(str1, str2, len); }
    inline int strncasecmp(const char_t* str1, const char_t* str2, int len) { return ::strncasecmp(str1, str2, len); }
    inline size_t strlen(const char_t* str) { return ::strlen(str); }
    inline void err_vprintf(const char_t* format, va_list vl) { ::vfprintf(stderr, format, vl); ::fputc('\n', stderr); }
    inline pal::string_t to_palstring(const std::string& str) { return str; }
    inline std::string to_stdstring(const pal::string_t& str) { return str; }
    inline void to_palstring(const char* str, pal::string_t* out) { out->assign(str); }
    inline void to_stdstring(const char_t* str, std::string* out) { out->assign(str); }
#endif
    bool realpath(string_t* path);
    bool file_exists(const string_t& path);
    inline bool directory_exists(const string_t& path) { return file_exists(path); }
    void readdir(const string_t& path, std::vector<pal::string_t>* list);

    bool get_own_executable_path(string_t* recv);
    bool getenv(const char_t* name, string_t* recv);
    bool get_default_packages_directory(string_t* recv);
    bool is_path_rooted(const string_t& path);

    int xtoi(const char_t* input);

    bool load_library(const char_t* path, dll_t* dll);
    proc_t get_symbol(dll_t library, const char* name);
    void unload_library(dll_t library);

    bool find_coreclr(pal::string_t* recv);
}

#endif // PAL_H
