// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <minipal/entrypoints.h>

// Include System.Net.Security.Native.Apple headers
#include "pal_networkframework.h"

static const Entry s_netSecurityAppleNative[] =
{
    DllImportEntry(AppleNetNative_NwInit)
    DllImportEntry(AppleNetNative_NwCreateContext)
    DllImportEntry(AppleNetNative_NwSetTlsOptions)
    DllImportEntry(AppleNetNative_NwStartTlsHandshake)
    DllImportEntry(AppleNetNative_NwProcessInputData)
    DllImportEntry(AppleNetNative_NwSendToConnection)
    DllImportEntry(AppleNetNative_NwReadFromConnection)
    DllImportEntry(AppleNetNative_NwCancelConnection)
    DllImportEntry(AppleNetNative_NwGetConnectionInfo)
    DllImportEntry(AppleNetNative_NwCopyCertChain)
};

EXTERN_C const void* NetSecurityAppleResolveDllImport(const char* name);

EXTERN_C const void* NetSecurityAppleResolveDllImport(const char* name)
{
    return minipal_resolve_dllimport(s_netSecurityAppleNative, ARRAY_SIZE(s_netSecurityAppleNative), name);
} 
