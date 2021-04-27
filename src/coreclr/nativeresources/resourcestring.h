// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __RESOURCE_STRING_H_
#define __RESOURCE_STRING_H_

// Struct to contain a resource ID and its corresponding
// English language string.
struct NativeStringResource
{
    unsigned int resourceId;
    const char* resourceString;
};

struct NativeStringResourceTable
{
    const int size;
    const NativeStringResource *table;
};

int LoadNativeStringResource(const NativeStringResourceTable &nativeStringResourceTable, unsigned int iResourceID, char16_t* szBuffer, int iMax, int *pcwchUsed);

#define CONCAT(a, b) a ## b

#define NATIVE_STRING_RESOURCE_TABLE(name) CONCAT(nativeStringResourceTable_, name)

#define DECLARE_NATIVE_STRING_RESOURCE_TABLE(name) \
    extern const NativeStringResourceTable NATIVE_STRING_RESOURCE_TABLE(name)

#endif // __RESOURCE_STRING_H_

