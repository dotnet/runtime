// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <assert.h>
#include <dlfcn.h>
#include <pthread.h>
#include <stdio.h>
#include <string.h>

#include "opensslshim.h"
#include "pal_atomic.h"

// Define pointers to all the used OpenSSL functions
#define REQUIRED_FUNCTION(fn) TYPEOF(fn) fn##_ptr;
#define REQUIRED_FUNCTION_110(fn) TYPEOF(fn) fn##_ptr;
#define LIGHTUP_FUNCTION(fn) TYPEOF(fn) fn##_ptr;
#define FALLBACK_FUNCTION(fn) TYPEOF(fn) fn##_ptr;
#define RENAMED_FUNCTION(fn,oldfn) TYPEOF(fn) fn##_ptr;
#define LEGACY_FUNCTION(fn) TYPEOF(fn) fn##_ptr;
FOR_ALL_OPENSSL_FUNCTIONS
#undef LEGACY_FUNCTION
#undef RENAMED_FUNCTION
#undef FALLBACK_FUNCTION
#undef LIGHTUP_FUNCTION
#undef REQUIRED_FUNCTION_110
#undef REQUIRED_FUNCTION
#if defined(TARGET_ARM) && defined(TARGET_LINUX)
TYPEOF(OPENSSL_gmtime) OPENSSL_gmtime_ptr;
#endif

// x.x.x, considering the max number of decimal digits for each component
#define MaxVersionStringLength 32

 static void* volatile libssl = NULL;

#ifdef __APPLE__
#define DYLIBNAME_PREFIX "libssl."
#define DYLIBNAME_SUFFIX ".dylib"
#define MAKELIB(v) DYLIBNAME_PREFIX v DYLIBNAME_SUFFIX
#else
#define LIBNAME "libssl.so"
#define SONAME_BASE LIBNAME "."
#define MAKELIB(v)  SONAME_BASE v
#endif

#if defined(TARGET_ARM) && defined(TARGET_LINUX)
// We support ARM32 linux distros that have Y2038-compatible glibc (those which support _TIME_BITS).
// Some such distros have not yet switched to _TIME_BITS=64 by default, so we may be running against an openssl
// that expects 32-bit time_t even though our time_t is 64-bit.
// This can be deleted once the minimum supported Linux Arm32 distros are
// at least Debian 13 and Ubuntu 24.04.
bool g_libSslUses32BitTime = false;
#endif

static void DlOpen(const char* libraryName)
{
    void* libsslNew = dlopen(libraryName, RTLD_LAZY);

    // check is someone else has opened and published libssl already
    if (!pal_atomic_cas_ptr(&libssl, libsslNew, NULL))
    {
        dlclose(libsslNew);
    }
}

static void OpenLibraryOnce(void)
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

#ifdef TARGET_ANDROID
    if (libssl == NULL)
    {
        // Android OpenSSL has no soname
        DlOpen(LIBNAME);
    }
#endif

    if (libssl == NULL)
    {
        // Prefer OpenSSL 3.x
        DlOpen(MAKELIB("3"));
    }

    if (libssl == NULL)
    {
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

#ifdef __FreeBSD__
    // The ports version of OpenSSL is used over base where possible
    if (libssl == NULL)
    {
        // OpenSSL 3.0 from ports
        DlOpen(MAKELIB("12"));
    }

    if (libssl == NULL)
    {
        // OpenSSL 3.0 from base as found in FreeBSD 14.0
        DlOpen(MAKELIB("30"));
    }

    // Fallbacks for OpenSSL 1.1.x
    if (libssl == NULL)
    {
        DlOpen(MAKELIB("11"));
    }

    if (libssl == NULL)
    {
        DlOpen(MAKELIB("111"));
    }
#endif

}

static pthread_once_t g_openLibrary = PTHREAD_ONCE_INIT;

int OpenLibrary(void)
{
    pthread_once(&g_openLibrary, OpenLibraryOnce);

    if (libssl != NULL)
    {
        return 1;
    }
    else
    {
        return 0;
    }
}

void InitializeOpenSSLShim(void)
{
    if (!OpenLibrary())
    {
        fprintf(stderr, "No usable version of libssl was found\n");
        abort();
    }

    // A function defined in libcrypto.so.1.0.0/libssl.so.1.0.0 that is not defined in
    // libcrypto.so.1.1.0/libssl.so.1.1.0
    const void* v1_0_sentinel = dlsym(libssl, "SSL_state");

    // Only permit a single assignment here so that two assemblies both triggering the initializer doesn't cause a
    // race where the fn_ptr is nullptr, then properly bound, then goes back to nullptr right before being used (then bound again).
    void* volatile tmp_ptr;

    // Get pointers to all the functions that are needed
#define REQUIRED_FUNCTION(fn) \
    if (!(fn##_ptr = (TYPEOF(fn))(dlsym(libssl, #fn)))) { fprintf(stderr, "Cannot get required symbol " #fn " from libssl\n"); abort(); }

#define REQUIRED_FUNCTION_110(fn) \
    if (!v1_0_sentinel && !(fn##_ptr = (TYPEOF(fn))(dlsym(libssl, #fn)))) { fprintf(stderr, "Cannot get required symbol " #fn " from libssl\n"); abort(); }

#define LIGHTUP_FUNCTION(fn) \
    fn##_ptr = (TYPEOF(fn))(dlsym(libssl, #fn));

#define FALLBACK_FUNCTION(fn) \
    if (!(tmp_ptr = dlsym(libssl, #fn))) { tmp_ptr = (void*)local_##fn; } \
    fn##_ptr = (TYPEOF(fn))tmp_ptr;

#define RENAMED_FUNCTION(fn,oldfn) \
    tmp_ptr = dlsym(libssl, #fn);\
    if (!tmp_ptr && !(tmp_ptr = dlsym(libssl, #oldfn))) { fprintf(stderr, "Cannot get required symbol " #oldfn " from libssl\n"); abort(); } \
    fn##_ptr = (TYPEOF(fn))tmp_ptr;

#define LEGACY_FUNCTION(fn) \
    if (v1_0_sentinel && !(fn##_ptr = (TYPEOF(fn))(dlsym(libssl, #fn)))) { fprintf(stderr, "Cannot get required symbol " #fn " from libssl\n"); abort(); }

    FOR_ALL_OPENSSL_FUNCTIONS
#undef LEGACY_FUNCTION
#undef RENAMED_FUNCTION
#undef FALLBACK_FUNCTION
#undef LIGHTUP_FUNCTION
#undef REQUIRED_FUNCTION_110
#undef REQUIRED_FUNCTION
#if defined(TARGET_ARM) && defined(TARGET_LINUX)
    if (!(OPENSSL_gmtime_ptr = (TYPEOF(OPENSSL_gmtime))(dlsym(libssl, "OPENSSL_gmtime")))) { fprintf(stderr, "Cannot get required symbol OPENSSL_gmtime from libssl\n"); abort(); }
#endif


    // Sanity check that we have at least one functioning way of reporting errors.
    if (ERR_put_error_ptr == &local_ERR_put_error)
    {
        if (ERR_new_ptr == NULL || ERR_set_debug_ptr == NULL || ERR_set_error_ptr == NULL)
        {
            fprintf(stderr, "Cannot determine the error reporting routine from libssl\n");
            abort();
        }
    }

#if defined(TARGET_ARM) && defined(TARGET_LINUX)
    // This value will represent a time in year 2038 if 64-bit time is used,
    // or 1901 if the lower 32 bits are interpreted as a 32-bit time_t value.
    time_t timeVal = (time_t)((unsigned long)INT_MAX + 1u);
    struct tm tmVal = { 0 };

    // Detect whether openssl is using 32-bit or 64-bit time_t.
    // If it uses 32-bit time_t, little-endianness means that the pointer
    // will be interpreted as a pointer to the lower 32 bits of timeVal.
    // tm_year is the number of years since 1900.
    if (!OPENSSL_gmtime(&timeVal, &tmVal) || (tmVal.tm_year != 138 && tmVal.tm_year != 1))
    {
        fprintf(stderr, "Cannot determine the time_t size used by libssl\n");
        abort();
    }

    g_libSslUses32BitTime = (tmVal.tm_year == 1);
#endif
}
