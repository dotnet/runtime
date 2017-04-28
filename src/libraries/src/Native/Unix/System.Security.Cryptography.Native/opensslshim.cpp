// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

#include <dlfcn.h>
#include <stdio.h>

#include "opensslshim.h"

// Define pointers to all the used ICU functions
#define PER_FUNCTION_BLOCK(fn, isRequired) decltype(fn) fn##_ptr;
FOR_ALL_OPENSSL_FUNCTIONS
#undef PER_FUNCTION_BLOCK

static void* libssl = nullptr;

bool OpenLibrary()
{
    // First try the default versioned so naming as described in the OpenSSL doc
    libssl = dlopen("libssl.so.1.0.0", RTLD_LAZY);
    if (libssl == nullptr)
    {
        // Fedora derived distros use different naming for the version 1.0.0
        libssl = dlopen("libssl.so.10", RTLD_LAZY);
    }

    if (libssl == nullptr)
    {
        // Debian 9 has dropped support for SSLv3 and so they have bumped their soname
        libssl = dlopen("libssl.so.1.0.2", RTLD_LAZY);
    }

    return libssl != nullptr;
}

__attribute__((constructor))
void InitializeOpenSSLShim()
{
    if (!OpenLibrary())
    {
        fprintf(stderr, "No usable version of the libssl was found\n");
        abort();
    }

    // Get pointers to all the ICU functions that are needed
#define PER_FUNCTION_BLOCK(fn, isRequired) \
    fn##_ptr = reinterpret_cast<decltype(fn)>(dlsym(libssl, #fn)); \
    if ((fn##_ptr) == NULL && isRequired) { fprintf(stderr, "Cannot get required symbol " #fn " from libssl\n"); abort(); }

    FOR_ALL_OPENSSL_FUNCTIONS
#undef PER_FUNCTION_BLOCK    
}

__attribute__((destructor))
void ShutdownOpenSSLShim()
{
    if (libssl != nullptr)
    {
        dlclose(libssl);
    }
}
