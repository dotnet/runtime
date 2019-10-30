// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

#include <dlfcn.h>
#include <stdio.h>
#include <stdbool.h>
#include <string.h>

#include "opensslshim.h"

// Define pointers to all the used OpenSSL functions
#define REQUIRED_FUNCTION(fn) __typeof(fn) fn##_ptr;
#define NEW_REQUIRED_FUNCTION(fn) __typeof(fn) fn##_ptr;
#define LIGHTUP_FUNCTION(fn) __typeof(fn) fn##_ptr;
#define FALLBACK_FUNCTION(fn) __typeof(fn) fn##_ptr;
#define RENAMED_FUNCTION(fn,oldfn) __typeof(fn) fn##_ptr;
#define LEGACY_FUNCTION(fn) __typeof(fn) fn##_ptr;
FOR_ALL_OPENSSL_FUNCTIONS
#undef LEGACY_FUNCTION
#undef RENAMED_FUNCTION
#undef FALLBACK_FUNCTION
#undef LIGHTUP_FUNCTION
#undef NEW_REQUIRED_FUNCTION
#undef REQUIRED_FUNCTION

// x.x.x, considering the max number of decimal digits for each component
#define MaxVersionStringLength 32
#define SONAME_BASE "libssl.so."

static void* libssl = NULL;

static bool OpenLibrary()
{
    // If there is an override of the version specified using the CLR_OPENSSL_VERSION_OVERRIDE
    // env variable, try to load that first.
    // The format of the value in the env variable is expected to be the version numbers,
    // like 1.0.0, 1.0.2 etc.
    char* versionOverride = getenv("CLR_OPENSSL_VERSION_OVERRIDE");

    if ((versionOverride != NULL) && strnlen(versionOverride, MaxVersionStringLength + 1) <= MaxVersionStringLength)
    {
        char soName[sizeof(SONAME_BASE) + MaxVersionStringLength] = SONAME_BASE;

        strcat(soName, versionOverride);
        libssl = dlopen(soName, RTLD_LAZY);
    }

    if (libssl == NULL)
    {
        // Prefer OpenSSL 1.1.x
        libssl = dlopen("libssl.so.1.1", RTLD_LAZY);
    }

    if (libssl == NULL)
    {
        // Debian 9 has dropped support for SSLv3 and so they have bumped their soname. Let's try it
        // before trying the version 1.0.0 to make it less probable that some of our other dependencies 
        // end up loading conflicting version of libssl.
        libssl = dlopen("libssl.so.1.0.2", RTLD_LAZY);
    }

    if (libssl == NULL)
    {
        // Now try the default versioned so naming as described in the OpenSSL doc
        libssl = dlopen("libssl.so.1.0.0", RTLD_LAZY);
    }

    if (libssl == NULL)
    {
        // Fedora derived distros use different naming for the version 1.0.0
        libssl = dlopen("libssl.so.10", RTLD_LAZY);
    }

    return libssl != NULL;
}

__attribute__((constructor))
static void InitializeOpenSSLShim()
{
    if (!OpenLibrary())
    {
        fprintf(stderr, "No usable version of libssl was found\n");
        abort();
    }

    // A function defined in libcrypto.so.1.0.0/libssl.so.1.0.0 that is not defined in
    // libcrypto.so.1.1.0/libssl.so.1.1.0
    const void* v1_0_sentinel = dlsym(libssl, "SSL_state");

    // Get pointers to all the functions that are needed
#define REQUIRED_FUNCTION(fn) \
    if (!(fn##_ptr = (__typeof(fn))(dlsym(libssl, #fn)))) { fprintf(stderr, "Cannot get required symbol " #fn " from libssl\n"); abort(); }

#define NEW_REQUIRED_FUNCTION(fn) \
    if (!v1_0_sentinel && !(fn##_ptr = (__typeof(fn))(dlsym(libssl, #fn)))) { fprintf(stderr, "Cannot get required symbol " #fn " from libssl\n"); abort(); }

#define LIGHTUP_FUNCTION(fn) \
    fn##_ptr = (__typeof(fn))(dlsym(libssl, #fn));

#define FALLBACK_FUNCTION(fn) \
    if (!(fn##_ptr = (__typeof(fn))(dlsym(libssl, #fn)))) { fn##_ptr = (__typeof(fn))local_##fn; }

#define RENAMED_FUNCTION(fn,oldfn) \
    if (!v1_0_sentinel && !(fn##_ptr = (__typeof(fn))(dlsym(libssl, #fn)))) { fprintf(stderr, "Cannot get required symbol " #fn " from libssl\n"); abort(); } \
    if (v1_0_sentinel && !(fn##_ptr = (__typeof(fn))(dlsym(libssl, #oldfn)))) { fprintf(stderr, "Cannot get required symbol " #oldfn " from libssl\n"); abort(); }

#define LEGACY_FUNCTION(fn) \
    if (v1_0_sentinel && !(fn##_ptr = (__typeof(fn))(dlsym(libssl, #fn)))) { fprintf(stderr, "Cannot get required symbol " #fn " from libssl\n"); abort(); }

    FOR_ALL_OPENSSL_FUNCTIONS
#undef LEGACY_FUNCTION
#undef RENAMED_FUNCTION
#undef FALLBACK_FUNCTION
#undef LIGHTUP_FUNCTION
#undef NEW_REQUIRED_FUNCTION
#undef REQUIRED_FUNCTION
}

__attribute__((destructor))
static void ShutdownOpenSSLShim()
{
    if (libssl != NULL)
    {
        dlclose(libssl);
    }
}
