// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef PAL_H
#define PAL_H

#include <string>
#include <vector>
#include <fstream>
#include <sstream>
#include <iostream>
#include <cstring>
#include <cstdarg>
#include <cstdint>
#include <tuple>
#include <unordered_map>
#include <unordered_set>
#include <memory>
#include <algorithm>
#include <cassert>

#if defined(_WIN32)

#define NOMINMAX
#include <windows.h>

#define xerr std::wcerr
#define xout std::wcout
#define DIR_SEPARATOR L'\\'
#define PATH_SEPARATOR L';'
#define PATH_MAX MAX_PATH
#define _X(s) L ## s

#else

#include <cstdlib>
#include <unistd.h>
#include <libgen.h>
#include <mutex>
#include <sys/stat.h>
#include <sys/types.h>
#include <sys/mman.h>

#define xerr std::cerr
#define xout std::cout
#define DIR_SEPARATOR '/'
#define PATH_SEPARATOR ':'
#undef _X
#define _X(s) s

#define S_OK        0x00000000
#define E_NOTIMPL   0x80004001
#define E_FAIL      0x80004005

#define SUCCEEDED(Status) ((Status) >= 0)

#endif

// When running on a platform that is not supported in RID fallback graph (because it was unknown
// at the time the SharedFX in question was built), we need to use a reasonable fallback RID to allow
// consuming the native assets.
//
// For Windows and OSX, we will maintain the last highest RID-Platform we are known to support for them as the
// degree of compat across their respective releases is usually high.
//
// We cannot maintain the same (compat) invariant for linux and thus, we will fallback to using lowest RID-Plaform.
#if defined(_WIN32)
#define LIB_PREFIX
#define MAKE_LIBNAME(NAME) (_X(NAME) _X(".dll"))
#define FALLBACK_HOST_RID _X("win10")
#elif defined(TARGET_OSX)
#define LIB_PREFIX _X("lib")
#define MAKE_LIBNAME(NAME) (LIB_PREFIX _X(NAME) _X(".dylib"))
#define FALLBACK_HOST_RID _X("osx.10.12")
#else
#define LIB_PREFIX _X("lib")
#define MAKE_LIBNAME(NAME) (LIB_PREFIX _X(NAME) _X(".so"))
#if defined(TARGET_FREEBSD)
#define FALLBACK_HOST_RID _X("freebsd")
#elif defined(TARGET_ILLUMOS)
#define FALLBACK_HOST_RID _X("illumos")
#elif defined(__sun)
#define FALLBACK_HOST_RID _X("solaris")
#else
#define FALLBACK_HOST_RID _X("linux")
#endif
#endif

#define LIBCORECLR_FILENAME (LIB_PREFIX _X("coreclr"))
#define LIBCORECLR_NAME MAKE_LIBNAME("coreclr")

#define CORELIB_NAME _X("System.Private.CoreLib.dll")

#define LIBHOSTPOLICY_FILENAME (LIB_PREFIX _X("hostpolicy"))
#define LIBHOSTPOLICY_NAME MAKE_LIBNAME("hostpolicy")

#define LIBFXR_NAME MAKE_LIBNAME("hostfxr")

#if !defined(PATH_MAX) && !defined(_WIN32)
#define PATH_MAX    4096
#endif


namespace pal
{
#if defined(_WIN32)
    #ifdef EXPORT_SHARED_API
        #define SHARED_API extern "C" __declspec(dllexport)
    #else
        #define SHARED_API extern "C"
    #endif

    #define STDMETHODCALLTYPE __stdcall

    typedef wchar_t char_t;
    typedef std::wstring string_t;
    typedef std::wstringstream stringstream_t;
    // TODO: Agree on the correct encoding of the files: The PoR for now is to
    // temporarily wchar for Windows and char for Unix. Current implementation
    // implicitly expects the contents on both Windows and Unix as char and
    // converts them to wchar in code for Windows. This line should become:
    // typedef std::basic_ifstream<char_t> ifstream_t.
    typedef std::basic_ifstream<char> ifstream_t;
    typedef std::istreambuf_iterator<ifstream_t::char_type> istreambuf_iterator_t;
    typedef std::basic_istream<char> istream_t;
    typedef HRESULT hresult_t;
    typedef HMODULE dll_t;
    typedef FARPROC proc_t;

    // Lockable object backed by CRITICAL_SECTION such that it does not pull in ConcRT.
    class mutex_t
    {
    public:
        mutex_t();
        ~mutex_t();

        mutex_t(const mutex_t&) = delete;
        mutex_t& operator=(const mutex_t&) = delete;

        void lock();
        void unlock();

    private:
        CRITICAL_SECTION _impl;
    };

    inline string_t exe_suffix() { return _X(".exe"); }

    inline int cstrcasecmp(const char* str1, const char* str2) { return ::_stricmp(str1, str2); }
    inline int strcmp(const char_t* str1, const char_t* str2) { return ::wcscmp(str1, str2); }
    inline int strcasecmp(const char_t* str1, const char_t* str2) { return ::_wcsicmp(str1, str2); }
    inline int strncmp(const char_t* str1, const char_t* str2, int len) { return ::wcsncmp(str1, str2, len); }
    inline int strncasecmp(const char_t* str1, const char_t* str2, int len) { return ::_wcsnicmp(str1, str2, len); }
    inline int pathcmp(const pal::string_t &path1, const pal::string_t &path2) { return strcasecmp(path1.c_str(), path2.c_str()); }
    inline string_t to_string(int value) { return std::to_wstring(value); }

    inline size_t strlen(const char_t* str) { return ::wcslen(str); }
    inline FILE * file_open(const string_t& path, const char_t* mode) { return ::_wfopen(path.c_str(), mode); }

    inline void file_vprintf(FILE* f, const char_t* format, va_list vl) { ::vfwprintf(f, format, vl); ::fputwc(_X('\n'), f); }
    inline void err_fputs(const char_t* message) { ::fputws(message, stderr); ::fputwc(_X('\n'), stderr); }
    inline void out_vprintf(const char_t* format, va_list vl) { ::vfwprintf(stdout, format, vl); ::fputwc(_X('\n'), stdout); }
    inline int str_vprintf(char_t* buffer, size_t count, const char_t* format, va_list vl) { return ::_vsnwprintf(buffer, count, format, vl); }
    inline const char_t* strerror(int errnum) { return ::_wcserror(errnum); }

    bool pal_utf8string(const string_t& str, std::vector<char>* out);
    bool pal_clrstring(const string_t& str, std::vector<char>* out);
    bool clr_palstring(const char* cstr, string_t* out);

    inline bool mkdir(const char_t* dir, int mode) { return CreateDirectoryW(dir, NULL) != 0; }
    inline bool rmdir (const char_t* path) { return RemoveDirectoryW(path) != 0; }
    inline int rename(const char_t* old_name, const char_t* new_name) { return ::_wrename(old_name, new_name); }
    inline int remove(const char_t* path) { return ::_wremove(path); }
    inline bool munmap(void* addr, size_t length) { return UnmapViewOfFile(addr) != 0; }
    inline int get_pid() { return GetCurrentProcessId(); }
    inline void sleep(uint32_t milliseconds) { Sleep(milliseconds); }
#else
    #ifdef EXPORT_SHARED_API
        #define SHARED_API extern "C" __attribute__((__visibility__("default")))
    #else
        #define SHARED_API extern "C"
    #endif

    #define __cdecl    /* nothing */
    #define __stdcall  /* nothing */
    #if !defined(TARGET_FREEBSD)
        #define __fastcall /* nothing */
    #endif
    #define STDMETHODCALLTYPE __stdcall

    typedef char char_t;
    typedef std::string string_t;
    typedef std::stringstream stringstream_t;
    typedef std::basic_ifstream<char> ifstream_t;
    typedef std::istreambuf_iterator<ifstream_t::char_type> istreambuf_iterator_t;
    typedef std::basic_istream<char> istream_t;
    typedef int hresult_t;
    typedef void* dll_t;
    typedef void* proc_t;
    typedef std::mutex mutex_t;

    inline string_t exe_suffix() { return _X(""); }

    inline int cstrcasecmp(const char* str1, const char* str2) { return ::strcasecmp(str1, str2); }
    inline int strcmp(const char_t* str1, const char_t* str2) { return ::strcmp(str1, str2); }
    inline int strcasecmp(const char_t* str1, const char_t* str2) { return ::strcasecmp(str1, str2); }
    inline int strncmp(const char_t* str1, const char_t* str2, int len) { return ::strncmp(str1, str2, len); }
    inline int strncasecmp(const char_t* str1, const char_t* str2, int len) { return ::strncasecmp(str1, str2, len); }
    inline int pathcmp(const pal::string_t& path1, const pal::string_t& path2) { return strcmp(path1.c_str(), path2.c_str()); }
    inline string_t to_string(int value) { return std::to_string(value); }

    inline size_t strlen(const char_t* str) { return ::strlen(str); }
    inline FILE * file_open(const string_t& path, const char_t* mode) { return fopen(path.c_str(), mode); }
    inline void file_vprintf(FILE* f, const char_t* format, va_list vl) { ::vfprintf(f, format, vl); ::fputc('\n', f); }
    inline void err_fputs(const char_t* message) { ::fputs(message, stderr); ::fputc(_X('\n'), stderr); }
    inline void out_vprintf(const char_t* format, va_list vl) { ::vfprintf(stdout, format, vl); ::fputc('\n', stdout); }
    inline int str_vprintf(char_t* str, size_t size, const char_t* format, va_list vl) { return ::vsnprintf(str, size, format, vl); }
    inline const char_t* strerror(int errnum) { return ::strerror(errnum); }

    inline bool pal_utf8string(const string_t& str, std::vector<char>* out) { out->assign(str.begin(), str.end()); out->push_back('\0'); return true; }
    inline bool pal_clrstring(const string_t& str, std::vector<char>* out) { return pal_utf8string(str, out); }
    inline bool clr_palstring(const char* cstr, string_t* out) { out->assign(cstr); return true; }

    inline bool mkdir(const char_t* dir, int mode) { return ::mkdir(dir, mode) == 0; }
    inline bool rmdir(const char_t* path) { return ::rmdir(path) == 0; }
    inline int rename(const char_t* old_name, const char_t* new_name) { return ::rename(old_name, new_name); }
    inline int remove(const char_t* path) { return ::remove(path); }
    inline bool munmap(void* addr, size_t length) { return ::munmap(addr, length) == 0; }
    inline int get_pid() { return getpid(); }
    inline void sleep(uint32_t milliseconds) { usleep(milliseconds * 1000); }

#endif

    inline int snwprintf(char_t* buffer, size_t count, const char_t* format, ...)
    {
        va_list args;
        va_start(args, format);
        int ret = str_vprintf(buffer, count, format, args);
        va_end(args);
        return ret;
    }

    string_t get_timestamp();

    bool getcwd(string_t* recv);
    string_t to_lower(const string_t& in);


    inline void file_flush(FILE *f) { std::fflush(f); }
    inline void err_flush() { std::fflush(stderr); }
    inline void out_flush() { std::fflush(stdout); }

    string_t get_current_os_rid_platform();
    inline string_t get_current_os_fallback_rid()
    {
        string_t fallbackRid(FALLBACK_HOST_RID);

        return fallbackRid;
    }

    const void* mmap_read(const string_t& path, size_t* length = nullptr);
    void* mmap_copy_on_write(const string_t& path, size_t* length = nullptr);

    bool touch_file(const string_t& path);
    bool realpath(string_t* path, bool skip_error_logging = false);
    bool file_exists(const string_t& path);
    inline bool directory_exists(const string_t& path) { return file_exists(path); }
    void readdir(const string_t& path, const string_t& pattern, std::vector<string_t>* list);
    void readdir(const string_t& path, std::vector<string_t>* list);
    void readdir_onlydirectories(const string_t& path, const string_t& pattern, std::vector<string_t>* list);
    void readdir_onlydirectories(const string_t& path, std::vector<string_t>* list);

    bool get_own_executable_path(string_t* recv);
    bool get_own_module_path(string_t* recv);
    bool get_method_module_path(string_t* recv, void* method);
    bool get_module_path(dll_t mod, string_t* recv);
    bool get_current_module(dll_t *mod);
    bool getenv(const char_t* name, string_t* recv);
    bool get_default_servicing_directory(string_t* recv);

    // Returns the globally registered install location (if any)
    bool get_dotnet_self_registered_dir(string_t* recv);
    // Returns name of the global registry location (for error messages)
    bool get_dotnet_self_registered_config_location(string_t* recv);

    // Returns the default install location for a given platform
    bool get_default_installation_dir(string_t* recv);

    // Returns the global locations to search for SDK/Frameworks - used when multi-level lookup is enabled
    bool get_global_dotnet_dirs(std::vector<string_t>* recv);

    bool get_default_breadcrumb_store(string_t* recv);
    bool is_path_rooted(const string_t& path);

    bool get_temp_directory(string_t& tmp_dir);

    // Returns a platform-specific, user-private directory within get_temp_directory()
    // that can be used for extracting out components of a single-file app.
    bool get_default_bundle_extraction_base_dir(string_t& extraction_dir);

    int xtoi(const char_t* input);

    bool get_loaded_library(const char_t *library_name, const char *symbol_name, /*out*/ dll_t *dll, /*out*/ string_t *path);
    bool load_library(const string_t* path, dll_t* dll);
    proc_t get_symbol(dll_t library, const char* name);
    void unload_library(dll_t library);

    bool is_running_in_wow64();

    bool are_paths_equal_with_normalized_casing(const string_t& path1, const string_t& path2);
}

#endif // PAL_H
