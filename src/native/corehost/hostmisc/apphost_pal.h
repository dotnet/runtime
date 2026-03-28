// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef APPHOST_PAL_H
#define APPHOST_PAL_H

#include <stddef.h>
#include <stdint.h>
#include <stdbool.h>
#include <stdarg.h>
#include <stdlib.h>
#include <string.h>
#include <assert.h>
#include <stdio.h>

#include "configure.h"

#if defined(_WIN32)
#error "This C PAL is only for non-Windows (Linux) platforms"
#endif

#include <unistd.h>
#include <libgen.h>
#include <sys/stat.h>
#include <sys/types.h>

#define DIR_SEPARATOR '/'
#define DIR_SEPARATOR_STR "/"
#define PATH_SEPARATOR ':'
#define _X(s) s

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

#define _STRINGIFY(s) _X(s)

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

// Max path buffer size for apphost string operations
#define APPHOST_PATH_MAX 4096

// pal function declarations (C equivalents of the C++ pal:: namespace)
bool pal_get_own_executable_path(char* recv, size_t recv_len);
bool pal_fullpath(char* path, size_t path_len);
bool pal_file_exists(const char* path);
bool pal_directory_exists(const char* path);
bool pal_is_path_fully_qualified(const char* path);
bool pal_getenv(const char* name, char* recv, size_t recv_len);
int pal_xtoi(const char* input);
bool pal_load_library(const char* path, void** dll);
void pal_unload_library(void* library);
void* pal_get_symbol(void* library, const char* name);
void pal_err_print_line(const char* message);

// Directory listing callback: called with each entry name, return true to continue
typedef bool (*pal_readdir_callback_fn)(const char* entry_name, void* context);
void pal_readdir_onlydirectories(const char* path, pal_readdir_callback_fn callback, void* context);

// Self-registered install location
bool pal_get_dotnet_self_registered_dir(char* recv, size_t recv_len);
bool pal_get_default_installation_dir(char* recv, size_t recv_len);
const char* pal_get_dotnet_self_registered_config_location(char* buf, size_t buf_len);

#endif // APPHOST_PAL_H
