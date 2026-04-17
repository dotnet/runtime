// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef PAL_H
#define PAL_H

// ============================================================================
// C-compatible section (usable from both C and C++ source files)
// ============================================================================

#include <stddef.h>
#include <stdint.h>
#include <stdbool.h>
#include <stdarg.h>
#include <stdlib.h>
#include <string.h>
#include <assert.h>
#include <stdio.h>

// pal_char_t: wchar_t on Windows, char on non-Windows
#if defined(_WIN32)
#ifndef PAL_CHAR_T_DEFINED
#define PAL_CHAR_T_DEFINED
typedef wchar_t pal_char_t;
#endif
#ifndef _X
#define _X(s) L ## s
#endif
#else
#ifndef PAL_CHAR_T_DEFINED
#define PAL_CHAR_T_DEFINED
typedef char pal_char_t;
#endif
#ifndef _X
#define _X(s) s
#endif
#endif

#ifndef _STRINGIFY
#define _STRINGIFY(s) _X(s)
#endif

// Max path buffer size for apphost string operations
#define APPHOST_PATH_MAX 4096

#include "configure.h"

// Library naming
#if defined(TARGET_WINDOWS)
#ifndef LIB_PREFIX
#define LIB_PREFIX ""
#define LIB_FILE_EXT ".dll"
#endif
#elif defined(TARGET_OSX)
#ifndef LIB_PREFIX
#define LIB_PREFIX "lib"
#define LIB_FILE_EXT ".dylib"
#endif
#else
#ifndef LIB_PREFIX
#define LIB_PREFIX "lib"
#define LIB_FILE_EXT ".so"
#endif
#endif

#ifndef LIB_NAME
#define LIB_NAME(NAME) LIB_PREFIX NAME
#define LIB_FILE_NAME(NAME) LIB_PREFIX NAME LIB_FILE_EXT
#define LIB_FILE_NAME_X(NAME) _STRINGIFY(LIB_FILE_NAME(NAME))
#endif

#ifndef LIBFXR_NAME
#define LIBFXR_NAME LIB_FILE_NAME_X("hostfxr")
#endif

// RID platform
#ifndef HOST_RID_PLATFORM
#if defined(TARGET_WINDOWS)
    #define HOST_RID_PLATFORM "win"
#elif defined(TARGET_OSX)
    #define HOST_RID_PLATFORM "osx"
#elif defined(TARGET_ANDROID)
    #define HOST_RID_PLATFORM "linux-bionic"
#else
    #define HOST_RID_PLATFORM FALLBACK_HOST_OS
#endif
#endif

// Thread-local storage
#if defined(_WIN32)
#define PAL_THREAD_LOCAL __declspec(thread)
#else
#define PAL_THREAD_LOCAL _Thread_local
#endif

// ============================================================================
// Platform-specific C section
// ============================================================================

#if defined(_WIN32)

#define NOMINMAX
#include <windows.h>

#ifndef DIR_SEPARATOR
#define DIR_SEPARATOR L'\\'
#define DIR_SEPARATOR_STR L"\\"
#define PATH_SEPARATOR L';'
#endif

#ifndef PATH_MAX
#define PATH_MAX MAX_PATH
#endif

// String operations
#define pal_strlen(s) wcslen(s)
#define pal_strcmp(a, b) wcscmp(a, b)
#define pal_strncmp(a, b, n) wcsncmp(a, b, n)
#define pal_strchr(s, c) wcschr(s, c)
#define pal_strrchr(s, c) wcsrchr(s, c)
#define pal_strtoul(s, e, b) wcstoul(s, e, b)
#define pal_str_vprintf(buf, count, fmt, args) _vsnwprintf_s(buf, count, _TRUNCATE, fmt, args)
#define pal_strlen_vprintf(fmt, args) _vscwprintf(fmt, args)
#define pal_str_printf(buf, count, fmt, ...) _snwprintf_s(buf, count, _TRUNCATE, fmt, ##__VA_ARGS__)

// Static inline helpers — no extern "C" needed (header-only, no linkage)
static inline bool pal_getenv(const pal_char_t* name, pal_char_t* recv, size_t recv_len)
{
    if (recv_len == 0) return false;
    recv[0] = L'\0';
    DWORD result = GetEnvironmentVariableW(name, recv, (DWORD)recv_len);
    // result > 0 on success (chars written, not counting NUL); result >= recv_len means
    // the buffer was too small (GetEnvironmentVariableW returns required size in that case).
    return result > 0 && result < (DWORD)recv_len;
}

static inline int pal_xtoi(const pal_char_t* s) { return _wtoi(s); }

// Convert a UTF-8 string to a pal_char_t (wchar_t) buffer.
// Returns true on success; out_buf is NUL-terminated on success.
static inline bool pal_utf8_to_palstr(const char* utf8, pal_char_t* out_buf, size_t out_buf_len)
{
    if (out_buf_len == 0)
        return false;
    int needed = MultiByteToWideChar(CP_UTF8, 0, utf8, -1, NULL, 0);
    if (needed <= 0 || (size_t)needed > out_buf_len)
        return false;
    return MultiByteToWideChar(CP_UTF8, 0, utf8, -1, out_buf, (int)out_buf_len) != 0;
}

#ifdef __cplusplus
extern "C" {
#endif

// pal function declarations (C equivalents of the C++ pal:: namespace)
bool pal_get_own_executable_path(pal_char_t* recv, size_t recv_len);
bool pal_fullpath(pal_char_t* path, size_t path_len);
bool pal_file_exists(const pal_char_t* path);
bool pal_directory_exists(const pal_char_t* path);
bool pal_is_path_fully_qualified(const pal_char_t* path);
bool pal_load_library(const pal_char_t* path, void** dll);
void pal_unload_library(void* library);
void* pal_get_symbol(void* library, const char* name);
void pal_err_print_line(const pal_char_t* message);

// Directory listing callback: called with each entry name, return true to continue
typedef bool (*pal_readdir_callback_fn)(const pal_char_t* entry_name, void* context);
void pal_readdir_onlydirectories(const pal_char_t* path, pal_readdir_callback_fn callback, void* context);

// Self-registered install location
bool pal_get_dotnet_self_registered_dir(pal_char_t* recv, size_t recv_len);
bool pal_get_default_installation_dir(pal_char_t* recv, size_t recv_len);
const pal_char_t* pal_get_dotnet_self_registered_config_location(pal_char_t* buf, size_t buf_len);

#ifdef __cplusplus
}
#endif

#else // !_WIN32

#include <unistd.h>
#include <libgen.h>
#include <sys/stat.h>
#include <sys/types.h>

#ifndef DIR_SEPARATOR
#define DIR_SEPARATOR '/'
#define DIR_SEPARATOR_STR "/"
#define PATH_SEPARATOR ':'
#endif

#ifndef S_OK
#define S_OK        0x00000000
#define E_NOTIMPL   0x80004001
#define E_FAIL      0x80004005
#endif

#ifndef SUCCEEDED
#define SUCCEEDED(Status) ((Status) >= 0)
#endif

#if !defined(PATH_MAX)
#define PATH_MAX 4096
#endif

// String operations
#define pal_strlen(s) strlen(s)
#define pal_strcmp(a, b) strcmp(a, b)
#define pal_strncmp(a, b, n) strncmp(a, b, n)
#define pal_strchr(s, c) strchr(s, c)
#define pal_strrchr(s, c) strrchr(s, c)
#define pal_strtoul(s, e, b) strtoul(s, e, b)
#define pal_str_vprintf(buf, count, fmt, args) vsnprintf(buf, (size_t)(count), fmt, args)
#define pal_strlen_vprintf(fmt, args) vsnprintf(NULL, 0, fmt, args)
#define pal_str_printf(buf, count, fmt, ...) snprintf(buf, (size_t)(count), fmt, ##__VA_ARGS__)

#ifdef __cplusplus
extern "C" {
#endif

// pal function declarations (C equivalents of the C++ pal:: namespace)
bool pal_get_own_executable_path(pal_char_t* recv, size_t recv_len);
bool pal_fullpath(pal_char_t* path, size_t path_len);
bool pal_file_exists(const pal_char_t* path);
bool pal_directory_exists(const pal_char_t* path);
bool pal_is_path_fully_qualified(const pal_char_t* path);
bool pal_getenv(const pal_char_t* name, pal_char_t* recv, size_t recv_len);
int pal_xtoi(const pal_char_t* input);
bool pal_load_library(const pal_char_t* path, void** dll);
void pal_unload_library(void* library);
void* pal_get_symbol(void* library, const char* name);
void pal_err_print_line(const pal_char_t* message);

// Directory listing callback: called with each entry name, return true to continue
typedef bool (*pal_readdir_callback_fn)(const pal_char_t* entry_name, void* context);
void pal_readdir_onlydirectories(const pal_char_t* path, pal_readdir_callback_fn callback, void* context);

// Self-registered install location
bool pal_get_dotnet_self_registered_dir(pal_char_t* recv, size_t recv_len);
bool pal_get_default_installation_dir(pal_char_t* recv, size_t recv_len);
const pal_char_t* pal_get_dotnet_self_registered_config_location(pal_char_t* buf, size_t buf_len);

#ifdef __cplusplus
}
#endif

#endif // !_WIN32

// Duplicate a pal_char_t string. Returns NULL if s is NULL or on allocation failure.
// Caller must free the returned pointer with free().
static inline pal_char_t* pal_strdup(const pal_char_t* s)
{
    if (s == NULL)
        return NULL;
    size_t len = pal_strlen(s);
    pal_char_t* dup = (pal_char_t*)malloc((len + 1) * sizeof(pal_char_t));
    if (dup != NULL)
        memcpy(dup, s, (len + 1) * sizeof(pal_char_t));
    return dup;
}

// ============================================================================
// C++ section
// ============================================================================

#ifdef __cplusplus

#include <string>
#include <vector>
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
#include <functional>

#if defined(_WIN32)

#define xerr std::wcerr
#define xout std::wcout

#else

#include <cstdlib>
#include <mutex>
#include <sys/mman.h>

#define xerr std::cerr
#define xout std::cout

#endif

// When running on a platform that is not supported in RID fallback graph (because it was unknown
// at the time the SharedFX in question was built), we need to use a reasonable fallback RID to allow
// consuming the native assets.
//
// For Windows and OSX, we will maintain the last highest RID-Platform we are known to support for them as the
// degree of compat across their respective releases is usually high.
//
// We cannot maintain the same (compat) invariant for linux and thus, we will fallback to using lowest RID-Platform.

#define CORELIB_NAME _X("System.Private.CoreLib.dll")
#define LIBCORECLR_NAME LIB_FILE_NAME_X("coreclr")
#define LIBHOSTPOLICY_NAME LIB_FILE_NAME_X("hostpolicy")

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

    inline const pal::char_t* exe_suffix() { return _X(".exe"); }

    inline int cstrcasecmp(const char* str1, const char* str2) { return ::_stricmp(str1, str2); }
    inline int strcmp(const char_t* str1, const char_t* str2) { return ::wcscmp(str1, str2); }
    inline int strcasecmp(const char_t* str1, const char_t* str2) { return ::_wcsicmp(str1, str2); }
    inline int strncmp(const char_t* str1, const char_t* str2, size_t len) { return ::wcsncmp(str1, str2, len); }
    inline int strncasecmp(const char_t* str1, const char_t* str2, size_t len) { return ::_wcsnicmp(str1, str2, len); }
    inline int pathcmp(const pal::string_t& path1, const pal::string_t& path2) { return strcasecmp(path1.c_str(), path2.c_str()); }
    inline string_t to_string(int value) { return std::to_wstring(value); }

    inline size_t strlen(const char_t* str) { return ::wcslen(str); }

    inline FILE* file_open(const string_t& path, const char_t* mode) { return ::_wfsopen(path.c_str(), mode, _SH_DENYNO); }

    void file_vprintf(FILE* f, const char_t* format, va_list vl);
    void err_print_line(const char_t* message);
    void out_vprint_line(const char_t* format, va_list vl);

    inline int str_vprintf(char_t* buffer, size_t count, const char_t* format, va_list vl) { return ::_vsnwprintf_s(buffer, count, _TRUNCATE, format, vl); }
    inline int strlen_vprintf(const char_t* format, va_list vl) { return ::_vscwprintf(format, vl); }

    template <typename... Args>
    int str_printf(char_t* buffer, size_t count, const char_t* format, Args&&... args) { return ::_snwprintf_s(buffer, count, _TRUNCATE, format, std::forward<Args>(args)...); }

    template <typename... Args>
    inline int strlen_printf(const char_t* format, Args&&... args) { return ::_scwprintf(format, std::forward<Args>(args)...); }

    inline const string_t strerror(int errnum)
    {
        // Windows does not provide strerrorlen to get the actual error length.
        // Use 1024 as the buffer size based on the buffer size used by glibc.
        // _wcserror_s truncates (and null-terminates) if the buffer is too small
        char_t buffer[1024];
        ::_wcserror_s(buffer, sizeof(buffer) / sizeof(char_t), errnum);
        return buffer;
    }

    size_t pal_utf8string(const string_t& str, char* out_buffer, size_t len);
    bool pal_utf8string(const string_t& str, std::vector<char>* out);
    bool pal_clrstring(const string_t& str, std::vector<char>* out);
    bool clr_palstring(const char* cstr, string_t* out);

    inline bool mkdir(const char_t* dir, int mode, int& error_code)
    {
        BOOL result = ::CreateDirectoryW(dir, NULL);
        if (result != FALSE)
            return true;

        error_code = ::GetLastError();
        return false;
    }

    inline bool rmdir(const char_t* path) { return RemoveDirectoryW(path) != 0; }
    inline int rename(const char_t* old_name, const char_t* new_name) { return ::_wrename(old_name, new_name); }
    inline int remove(const char_t* path) { return ::_wremove(path); }
    inline bool munmap(void* addr, size_t length) { return UnmapViewOfFile(addr) != 0; }
    inline int get_pid() { return GetCurrentProcessId(); }
    inline void sleep(uint32_t milliseconds) { Sleep(milliseconds); }
#else // _WIN32
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
    typedef int hresult_t;
    typedef void* dll_t;
    typedef void* proc_t;
    typedef std::mutex mutex_t;

    inline const pal::char_t* exe_suffix() { return nullptr; }

    inline int cstrcasecmp(const char* str1, const char* str2) { return ::strcasecmp(str1, str2); }
    inline int strcmp(const char_t* str1, const char_t* str2) { return ::strcmp(str1, str2); }
    inline int strcasecmp(const char_t* str1, const char_t* str2) { return ::strcasecmp(str1, str2); }
    inline int strncmp(const char_t* str1, const char_t* str2, int len) { return ::strncmp(str1, str2, len); }
    inline int strncasecmp(const char_t* str1, const char_t* str2, int len) { return ::strncasecmp(str1, str2, len); }
    inline int pathcmp(const pal::string_t& path1, const pal::string_t& path2) { return strcmp(path1.c_str(), path2.c_str()); }
    inline string_t to_string(int value) { return std::to_string(value); }

    inline size_t strlen(const char_t* str) { return ::strlen(str); }
    inline FILE* file_open(const string_t& path, const char_t* mode) { return fopen(path.c_str(), mode); }
    inline void file_vprintf(FILE* f, const char_t* format, va_list vl) { ::vfprintf(f, format, vl); ::fputc('\n', f); }
    inline void err_print_line(const char_t* message) { ::fputs(message, stderr); ::fputc(_X('\n'), stderr); }
    inline void out_vprint_line(const char_t* format, va_list vl) { ::vfprintf(stdout, format, vl); ::fputc('\n', stdout); }
    inline int str_vprintf(char_t* str, size_t size, const char_t* format, va_list vl) { return ::vsnprintf(str, size, format, vl); }
    inline int strlen_vprintf(const char_t* format, va_list vl) { return ::vsnprintf(nullptr, 0, format, vl); }

    template <typename... Args>
    int str_printf(char_t* buffer, size_t size, const char_t* format, Args&&... args) { return ::snprintf(buffer, size, format, std::forward<Args>(args)...); }

    template <typename... Args>
    inline int strlen_printf(const char_t* format, Args&&... args) { return ::snprintf(nullptr, 0, format, std::forward<Args>(args)...); }

    inline const string_t strerror(int errnum) { return ::strerror(errnum); }

    inline size_t pal_utf8string(const string_t& str, char* out_buffer, size_t buffer_len)
    {
        size_t len = str.size() + 1;
        if (buffer_len < len)
            return len;

        ::strncpy(out_buffer, str.c_str(), str.size());
        out_buffer[len - 1] = '\0';
        return len;
    }
    inline bool pal_utf8string(const string_t& str, std::vector<char>* out) { out->assign(str.begin(), str.end()); out->push_back('\0'); return true; }
    inline bool pal_clrstring(const string_t& str, std::vector<char>* out) { return pal_utf8string(str, out); }
    inline bool clr_palstring(const char* cstr, string_t* out) { out->assign(cstr); return true; }

    inline bool mkdir(const char_t* dir, int mode, int& error_code)
    {
        int ret = ::mkdir(dir, mode);
        if (ret == 0)
            return true;

        error_code = errno;
        return false;
    }

    inline bool rmdir(const char_t* path) { return ::rmdir(path) == 0; }
    inline int rename(const char_t* old_name, const char_t* new_name) { return ::rename(old_name, new_name); }
    inline int remove(const char_t* path) { return ::remove(path); }
    inline bool munmap(void* addr, size_t length) { return ::munmap(addr, length) == 0; }
    inline int get_pid() { return getpid(); }
    inline void sleep(uint32_t milliseconds) { usleep(milliseconds * 1000); }
#endif // _WIN32

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

    string_t get_current_os_rid_platform();
    inline string_t get_current_os_fallback_rid()
    {
        return _STRINGIFY(FALLBACK_HOST_OS);
    }

    const void* mmap_read(const string_t& path, size_t* length = nullptr);
    void* mmap_copy_on_write(const string_t& path, size_t* length = nullptr);

    bool touch_file(const string_t& path);
    // Realpath resolves a fully-qualified path to the target. It always resolves through file symlinks (not necessarily directory symlinks).
    bool realpath(string_t* path, bool skip_error_logging = false);
    // Fullpath resolves a fully-qualified path to the target. It may resolve through symlinks, depending on platform.
    bool fullpath(string_t* path, bool skip_error_logging = false);
    bool file_exists(const string_t& path);
    bool is_directory(const pal::string_t& path);
    inline bool directory_exists(const string_t& path) { return file_exists(path); }
    void readdir(const string_t& path, const string_t& pattern, std::vector<string_t>* list);
    void readdir(const string_t& path, std::vector<string_t>* list);
    void readdir_onlydirectories(const string_t& path, const string_t& pattern, std::vector<string_t>* list);
    void readdir_onlydirectories(const string_t& path, std::vector<string_t>* list);

    bool get_own_executable_path(string_t* recv);
    bool get_own_module_path(string_t* recv);
    bool get_method_module_path(string_t* recv, void* method);
    bool get_module_path(dll_t mod, string_t* recv);
    bool get_current_module(dll_t* mod);
    bool getenv(const char_t* name, string_t* recv);
    void enumerate_environment_variables(const std::function<void(const char_t*, const char_t*)> callback);
    bool get_default_servicing_directory(string_t* recv);

    enum class architecture
    {
        arm,
        arm64,
        armv6,
        loongarch64,
        ppc64le,
        riscv64,
        s390X,
        x64,
        x86,
        wasm,

        __last // Sentinel value
    };

    // Returns the globally registered install location (if any) for the current architecture
    bool get_dotnet_self_registered_dir(string_t* recv);

    // Returns the globally registered install location (if any) for the specified architecture
    bool get_dotnet_self_registered_dir_for_arch(architecture arch, string_t* recv);

    // Returns name of the config location for global install registration (for example, registry key or file path)
    string_t get_dotnet_self_registered_config_location(architecture arch);

    // Returns the default install location for a given platform for the current architecture
    bool get_default_installation_dir(string_t* recv);

    // Returns the default install location for a given platform for the specified architecture
    bool get_default_installation_dir_for_arch(architecture arch, string_t* recv);

    // Returns the global locations to search for SDK/Frameworks - used when multi-level lookup is enabled
    bool get_global_dotnet_dirs(std::vector<string_t>* recv);

    bool get_default_breadcrumb_store(string_t* recv);
    bool is_path_rooted(const string_t& path);
    bool is_path_fully_qualified(const string_t& path);

    // Returns a platform-specific, user-private directory
    // that can be used for extracting out components of a single-file app.
    bool get_default_bundle_extraction_base_dir(string_t& extraction_dir);

    int xtoi(const char_t* input);

    bool get_loaded_library(const char_t* library_name, const char* symbol_name, /*out*/ dll_t* dll, /*out*/ string_t* path);
    bool load_library(const string_t* path, dll_t* dll);
    proc_t get_symbol(dll_t library, const char* name);
    void unload_library(dll_t library);

    bool is_running_in_wow64();
    bool is_emulating_x64();

    bool are_paths_equal_with_normalized_casing(const string_t& path1, const string_t& path2);
}

#endif // __cplusplus

#endif // PAL_H
