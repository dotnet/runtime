// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

#include <dlfcn.h>
#include <stdio.h>
#include <stdbool.h>

#include "opensslshim.h"

// Define pointers to all the used ICU functions
#define PER_FUNCTION_BLOCK(fn, isRequired) __typeof(fn) fn##_ptr;
FOR_ALL_OPENSSL_FUNCTIONS
#undef PER_FUNCTION_BLOCK

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
        fprintf(stderr, "No usable version of the libssl was found\n");
        abort();
    }

    // Get pointers to all the ICU functions that are needed
#define PER_FUNCTION_BLOCK(fn, isRequired) \
    fn##_ptr = (__typeof(fn))(dlsym(libssl, #fn)); \
    if ((fn##_ptr) == NULL && isRequired) { fprintf(stderr, "Cannot get required symbol " #fn " from libssl\n"); abort(); }

    FOR_ALL_OPENSSL_FUNCTIONS
#undef PER_FUNCTION_BLOCK    
}

__attribute__((destructor))
static void ShutdownOpenSSLShim()
{
    if (libssl != NULL)
    {
        dlclose(libssl);
    }
}
