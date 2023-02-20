// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_config.h"
#include "pal_dynamicload.h"
#include "pal_utilities.h"

#include <string.h>
#include <stdio.h>


void* SystemNative_LoadLibrary(const char* filename)
{
    assert_msg(false, "Not supported on WASI", 0);
    return NULL;
}

void* SystemNative_GetLoadLibraryError(void)
{
    assert_msg(false, "Not supported on WASI", 0);
    return NULL;
}

void* SystemNative_GetProcAddress(void* handle, const char* symbol)
{
    assert_msg(false, "Not supported on WASI", 0);
    return NULL;
}

void SystemNative_FreeLibrary(void* handle)
{
}

void* SystemNative_GetDefaultSearchOrderPseudoHandle(void)
{
    assert_msg(false, "Not supported on WASI", 0);
    return NULL;
}
