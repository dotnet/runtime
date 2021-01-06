// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <assert.h>
#include <dlfcn.h>
#include <stdio.h>
#include <stdbool.h>
#include <string.h>
#include <android/log.h>

#include "opensslshim.h"

#define LOG_INFO(fmt, ...) ((void)__android_log_print(ANDROID_LOG_INFO, "DOTNET", "%s: " fmt, __FUNCTION__, ## __VA_ARGS__))
#define LOG_ERROR(fmt, ...) ((void)__android_log_print(ANDROID_LOG_ERROR, "DOTNET", "%s: " fmt, __FUNCTION__, ## __VA_ARGS__))

// Define pointers to all the used OpenSSL functions
#define REQUIRED_FUNCTION(fn) TYPEOF(fn) fn##_ptr;
#define NEW_REQUIRED_FUNCTION(fn) TYPEOF(fn) fn##_ptr;
#define LIGHTUP_FUNCTION(fn) TYPEOF(fn) fn##_ptr;
#define FALLBACK_FUNCTION(fn) TYPEOF(fn) fn##_ptr;
#define RENAMED_FUNCTION(fn,oldfn) TYPEOF(fn) fn##_ptr;
#define LEGACY_FUNCTION(fn) TYPEOF(fn) fn##_ptr;
FOR_ALL_OPENSSL_FUNCTIONS
#undef LEGACY_FUNCTION
#undef RENAMED_FUNCTION
#undef FALLBACK_FUNCTION
#undef LIGHTUP_FUNCTION
#undef NEW_REQUIRED_FUNCTION
#undef REQUIRED_FUNCTION

static void* libssl = NULL;

static bool OpenLibrary()
{
    // Open itself, system OpenSSL should already exist in the current process.
    libssl = dlopen(NULL, RTLD_NOW);
    return libssl != NULL;
}

__attribute__((constructor))
static void InitializeOpenSSLShim()
{
    LOG_INFO("InitializeOpenSSLShim");

    if (!OpenLibrary())
    {
        LOG_ERROR("No usable version of libssl was found\n");
        abort();
    }

    // A function defined in libcrypto.so.1.0.0/libssl.so.1.0.0 that is not defined in
    // libcrypto.so.1.1.0/libssl.so.1.1.0
    const void* v1_0_sentinel = dlsym(libssl, "SSL_state");

    // Get pointers to all the functions that are needed
#define REQUIRED_FUNCTION(fn) \
    if (!(fn##_ptr = (TYPEOF(fn))(dlsym(libssl, #fn)))) { LOG_ERROR("Cannot get required symbol " #fn " from libssl\n"); }

#define NEW_REQUIRED_FUNCTION(fn) \
    if (!v1_0_sentinel && !(fn##_ptr = (TYPEOF(fn))(dlsym(libssl, #fn)))) { LOG_ERROR("Cannot get required symbol " #fn " from libssl\n"); }

#define LIGHTUP_FUNCTION(fn) \
    fn##_ptr = (TYPEOF(fn))(dlsym(libssl, #fn));

#define FALLBACK_FUNCTION(fn) \
    if (!(fn##_ptr = (TYPEOF(fn))(dlsym(libssl, #fn)))) { fn##_ptr = (TYPEOF(fn))local_##fn; }

#define RENAMED_FUNCTION(fn,oldfn) \
    if (!v1_0_sentinel && !(fn##_ptr = (TYPEOF(fn))(dlsym(libssl, #fn)))) { LOG_ERROR("Cannot get required symbol " #fn " from libssl\n"); } \
    if (v1_0_sentinel && !(fn##_ptr = (TYPEOF(fn))(dlsym(libssl, #oldfn)))) { LOG_ERROR("Cannot get required symbol " #oldfn " from libssl\n"); }

#define LEGACY_FUNCTION(fn) \
    if (v1_0_sentinel && !(fn##_ptr = (TYPEOF(fn))(dlsym(libssl, #fn)))) { LOG_ERROR("Cannot get required symbol " #fn " from libssl\n"); }

    FOR_ALL_OPENSSL_FUNCTIONS
#undef LEGACY_FUNCTION
#undef RENAMED_FUNCTION
#undef FALLBACK_FUNCTION
#undef LIGHTUP_FUNCTION
#undef NEW_REQUIRED_FUNCTION
#undef REQUIRED_FUNCTION

    LOG_INFO("InitializeOpenSSLShim finished.");
}
