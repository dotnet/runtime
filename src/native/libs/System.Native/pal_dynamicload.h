// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_types.h"

#if HAVE_GNU_LIBNAMES_H
#include <gnu/lib-names.h>
#endif

// libc file name:
// * For Linux, use the full name of the library that is defined in <gnu/lib-names.h> by the
//   LIBC_SO constant. The problem is that calling dlopen("libc.so") will fail for libc even
//   though it works for other libraries. The reason is that libc.so is just linker script
//   (i.e. a test file).
//   As a result, we have to use the full name (i.e. lib.so.6) that is defined by LIBC_SO.
// * For macOS, use constant value absolute path "/usr/lib/libc.dylib".
// * For FreeBSD, use constant value "libc.so.7".
// * For rest of Unices, use constant value "libc.so".
#if defined(__APPLE__)
#define LIBC_FILENAME "/usr/lib/libc.dylib"
#elif defined(__FreeBSD__)
#define LIBC_FILENAME "libc.so.7"
#elif defined(LIBC_SO)
#define LIBC_FILENAME LIBC_SO
#else
#define LIBC_FILENAME "libc.so"
#endif

PALEXPORT void* SystemNative_LoadLibrary(const char* filename);

PALEXPORT void* SystemNative_GetProcAddress(void* handle, const char* symbol);

PALEXPORT void SystemNative_FreeLibrary(void* handle);
