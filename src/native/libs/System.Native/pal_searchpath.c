// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_searchpath.h"
#include <stdlib.h>

const char* SystemNative_SearchPath(int32_t folderId)
{
    (void)folderId;
    __builtin_unreachable();
    return NULL;
}

const char* SystemNative_SearchPath_TempDirectory()
{
    __builtin_unreachable();
    return NULL;
}
