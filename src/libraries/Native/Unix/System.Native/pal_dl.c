// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_dl.h"
#include "dlfcn.h"
#include <stdlib.h>

#ifdef TARGET_ANDROID
void* SystemNative_GetDefaultSearchOrderPseudoHandle()
{
    return (void*)RTLD_DEFAULT;
}
#else
static void* g_defaultSearchOrderPseudoHandle = NULL;
void* SystemNative_GetDefaultSearchOrderPseudoHandle()
{
    if (g_defaultSearchOrderPseudoHandle == NULL)
    {
        g_defaultSearchOrderPseudoHandle = dlopen(NULL, RTLD_LAZY);
    }
    return g_defaultSearchOrderPseudoHandle;
}
#endif
