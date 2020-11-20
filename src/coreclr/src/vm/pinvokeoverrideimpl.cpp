// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// REVIEW: THE IMPLEMENTATION FOR THE OVERRIDER WILL BE MOVED TO HOST
//

#include "common.h"
#include "pinvokeoverrideimpl.h"

extern "C" const void* GlobalizationResolveDllImport(const char* name);
extern "C" const void* CompressionResolveDllImport(const char* name);

const void* SuperHost::ResolveDllImport(const char* libraryName, const char* entrypointName)
{
    if (strcmp(libraryName, "libSystem.Globalization.Native") == 0)
    {
        return GlobalizationResolveDllImport(entrypointName);
    }

#if defined(_WIN32)
    if (strcmp(libraryName, "clrcompression") == 0)
#else
    if (strcmp(libraryName, "libSystem.IO.Compression.Native") == 0)
#endif
    {
        return CompressionResolveDllImport(entrypointName);
    }

    return nullptr;
}
