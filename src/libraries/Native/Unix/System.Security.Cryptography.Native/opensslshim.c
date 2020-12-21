// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <assert.h>
#include <dlfcn.h>
#include <stdio.h>
#include <stdbool.h>
#include <string.h>

#include "opensslshim.h"

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

// x.x.x, considering the max number of decimal digits for each component
#define MaxVersionStringLength 32

static void* libssl = NULL;

#ifdef __APPLE__
#define DYLIBNAME_PREFIX "libssl."
#define DYLIBNAME_SUFFIX ".dylib"
#define MAKELIB(v) DYLIBNAME_PREFIX v DYLIBNAME_SUFFIX
#else
#define SONAME_BASE "libssl.so."
#define MAKELIB(v)  SONAME_BASE v
#endif

static void DlOpen(const char* libraryName)
{
    assert(libssl == NULL);
    libssl = dlopen(libraryName, RTLD_LAZY);
}

static bool OpenLibrary()
{
    // If there is an override of the version specified using the CLR_OPENSSL_VERSION_OVERRIDE
    // env variable, try to load that first.
    // The format of the value in the env variable is expected to be the version numbers,
    // like 1.0.0, 1.0.2 etc.
    char* versionOverride = getenv("CLR_OPENSSL_VERSION_OVERRIDE");

    if ((versionOverride != NULL) && strnlen(versionOverride, MaxVersionStringLength + 1) <= MaxVersionStringLength)
    {
#ifdef __APPLE__
        char soName[sizeof(DYLIBNAME_PREFIX) + MaxVersionStringLength + sizeof(DYLIBNAME_SUFFIX)] =
            DYLIBNAME_PREFIX;

        strcat(soName, versionOverride);
        strcat(soName, DYLIBNAME_SUFFIX);
#else
        char soName[sizeof(SONAME_BASE) + MaxVersionStringLength] = SONAME_BASE;

        strcat(soName, versionOverride);
#endif

        DlOpen(soName);
    }

    if (libssl == NULL)
    {
        // Prefer OpenSSL 1.1.x
        DlOpen(MAKELIB("1.1"));
    }

    if (libssl == NULL)
    {
        // Debian 9 has dropped support for SSLv3 and so they have bumped their soname. Let's try it
        // before trying the version 1.0.0 to make it less probable that some of our other dependencies 
        // end up loading conflicting version of libssl.
        DlOpen(MAKELIB("1.0.2"));
    }

    if (libssl == NULL)
    {
        // Now try the default versioned so naming as described in the OpenSSL doc
        DlOpen(MAKELIB("1.0.0"));
    }

    if (libssl == NULL)
    {
        // Fedora derived distros use different naming for the version 1.0.0
        DlOpen(MAKELIB("10"));
    }

    // FreeBSD uses a different suffix numbering convention.
    // Current supported FreeBSD releases should use the order .11 -> .111 -> .8
    if (libssl == NULL)
    {
        DlOpen(MAKELIB("11"));
    }

    if (libssl == NULL)
    {
        DlOpen(MAKELIB("111"));
    }

    if (libssl == NULL)
    {
        DlOpen(MAKELIB("8"));
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
    if (!(fn##_ptr = (TYPEOF(fn))(dlsym(libssl, #fn)))) { fprintf(stderr, "Cannot get required symbol " #fn " from libssl\n"); abort(); }

#define NEW_REQUIRED_FUNCTION(fn) \
    if (!v1_0_sentinel && !(fn##_ptr = (TYPEOF(fn))(dlsym(libssl, #fn)))) { fprintf(stderr, "Cannot get required symbol " #fn " from libssl\n"); abort(); }

#define LIGHTUP_FUNCTION(fn) \
    fn##_ptr = (TYPEOF(fn))(dlsym(libssl, #fn));

#define FALLBACK_FUNCTION(fn) \
    if (!(fn##_ptr = (TYPEOF(fn))(dlsym(libssl, #fn)))) { fn##_ptr = (TYPEOF(fn))local_##fn; }

#define RENAMED_FUNCTION(fn,oldfn) \
    if (!v1_0_sentinel && !(fn##_ptr = (TYPEOF(fn))(dlsym(libssl, #fn)))) { fprintf(stderr, "Cannot get required symbol " #fn " from libssl\n"); abort(); } \
    if (v1_0_sentinel && !(fn##_ptr = (TYPEOF(fn))(dlsym(libssl, #oldfn)))) { fprintf(stderr, "Cannot get required symbol " #oldfn " from libssl\n"); abort(); }

#define LEGACY_FUNCTION(fn) \
    if (v1_0_sentinel && !(fn##_ptr = (TYPEOF(fn))(dlsym(libssl, #fn)))) { fprintf(stderr, "Cannot get required symbol " #fn " from libssl\n"); abort(); }

    FOR_ALL_OPENSSL_FUNCTIONS
#undef LEGACY_FUNCTION
#undef RENAMED_FUNCTION
#undef FALLBACK_FUNCTION
#undef LIGHTUP_FUNCTION
#undef NEW_REQUIRED_FUNCTION
#undef REQUIRED_FUNCTION
}
