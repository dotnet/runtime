// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __MONO_LIBRARY_BUILDER_H__
#define __MONO_LIBRARY_BUILDER_H__

#include <stdlib.h>

#if defined(HOST_ANDROID)

#include <android/log.h>

#define LOG_ERROR(fmt, ...) \
    do \
    { \
        __android_log_print(ANDROID_LOG_ERROR, "MONO_SELF_CONTAINED_LIBRARY", fmt, ##__VA_ARGS__); \
        abort (); \
    } while (0)

#elif defined(HOST_APPLE_MOBILE)

#include <os/log.h>

#define LOG_ERROR(fmt, ...) \
    do \
    { \
        os_log_error (OS_LOG_DEFAULT, fmt, ##__VA_ARGS__); \
        abort (); \
    } while (0)

#else

#error Unsupported Host Platform. Ensure the hosting platform is supported by the LibraryBuilder and the appropriate logging functions are added.

#endif

void register_aot_modules (void);
void preload_assemblies_with_exported_symbols ();
typedef void (*MonoRuntimeInitCallback) (void);
void mono_set_runtime_init_callback (MonoRuntimeInitCallback callback);

#endif /*__MONO_LIBRARY_BUILDER_H__*/
