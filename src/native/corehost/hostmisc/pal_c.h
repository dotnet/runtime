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

// Non-Windows C PAL implementation
#if !defined(_WIN32)

#include "configure.h"

#include <unistd.h>
#include <libgen.h>
#include <sys/stat.h>
#include <sys/types.h>

#ifdef __cplusplus
extern "C" {
#endif

#define DIR_SEPARATOR '/'
#define DIR_SEPARATOR_STR "/"
#define PATH_SEPARATOR ':'

#define S_OK        0x00000000
#define E_NOTIMPL   0x80004001
#define E_FAIL      0x80004005

#define SUCCEEDED(Status) ((Status) >= 0)

#define LIB_PREFIX "lib"
#define LIB_FILE_EXT ".so"

#if defined(TARGET_OSX)
#undef LIB_FILE_EXT
#define LIB_FILE_EXT ".dylib"
#endif

#define LIB_NAME(NAME) LIB_PREFIX NAME
#define LIB_FILE_NAME(NAME) LIB_PREFIX NAME LIB_FILE_EXT
#define LIB_FILE_NAME_X(NAME) _STRINGIFY(LIB_FILE_NAME(NAME))

#define LIBFXR_NAME LIB_FILE_NAME_X("hostfxr")

#if !defined(PATH_MAX)
#define PATH_MAX 4096
#endif

#if defined(TARGET_WINDOWS)
    #define HOST_RID_PLATFORM "win"
#elif defined(TARGET_OSX)
    #define HOST_RID_PLATFORM "osx"
#elif defined(TARGET_ANDROID)
    #define HOST_RID_PLATFORM "linux-bionic"
#else
    #define HOST_RID_PLATFORM FALLBACK_HOST_OS
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
void* pal_get_symbol(void* library, const pal_char_t* name);
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

#endif // !defined(_WIN32)

#endif // PAL_C_H
