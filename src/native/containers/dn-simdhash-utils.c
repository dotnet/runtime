// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "dn-utils.h"
#include "dn-simdhash-utils.h"

#include <stdio.h>
#include <stdlib.h>

// Define a default implementation of the assert failure function.

#ifdef _MSC_VER
#define DEFINE_ALTERNATENAME_3(part) _Pragma(#part)
#define DEFINE_ALTERNATENAME_2(part) DEFINE_ALTERNATENAME_3(comment(linker, part))
#define DEFINE_ALTERNATENAME_1(part) DEFINE_ALTERNATENAME_2(#part)
#define DEFINE_ALTERNATENAME(alias, func) DEFINE_ALTERNATENAME_1(/ALTERNATENAME:alias=func)
#ifdef _M_IX86
#define DEFAULT_IMPLEMENTATION(func, impl, retval) DEFINE_ALTERNATENAME(_ ## func, _ ## impl) retval __cdecl impl
#else
#define DEFAULT_IMPLEMENTATION(func, impl, retval) DEFINE_ALTERNATENAME(func, impl) retval impl
#endif
#else
#define DEFAULT_IMPLEMENTATION(func, impl, retval) __attribute__((weak)) retval func
#endif

DEFAULT_IMPLEMENTATION(dn_simdhash_assert_fail, dn_simdhash_assert_fail_default, void) (const char *file, int line, const char *condition)
{
    fprintf(stderr, "Assertion failed:%s\n\tFile: %s\n\tLine:%d\n", condition, file, line);
    abort();
}
