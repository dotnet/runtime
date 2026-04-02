// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef PAL_C_H
#define PAL_C_H

#include <stddef.h>
#include <stdint.h>
#include <stdbool.h>
#include <stdarg.h>
#include <stdlib.h>
#include <string.h>
#include <assert.h>
#include <stdio.h>

// === Cross-platform types and macros ===

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

// === Platform-specific ===

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
#define pal_str_printf(buf, count, fmt, ...) _snwprintf_s(buf, count, _TRUNCATE, fmt, __VA_ARGS__)

#ifdef __cplusplus
extern "C" {
#endif

static inline bool pal_getenv(const pal_char_t* name, pal_char_t* recv, size_t recv_len)
{
    return GetEnvironmentVariableW(name, recv, (DWORD)recv_len) > 0;
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

#ifdef __cplusplus
extern "C" {
#endif

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
#define pal_str_printf(buf, count, fmt, ...) snprintf(buf, (size_t)(count), fmt, __VA_ARGS__)

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

#endif // PAL_C_H
