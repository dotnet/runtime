// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdlib.h>

#include "pal_jni.h"

void* xmalloc(size_t size)
{
    void *ret = malloc(size);
    abort_unless(ret != NULL, "Out of memory");
    return ret;
}

void* xcalloc(size_t nmemb, size_t size)
{
    void *ret = calloc(nmemb, size);
    abort_unless(ret != NULL, "Out of memory");
    return ret;
}
