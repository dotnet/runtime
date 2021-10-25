// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "../../AnyOS/entrypoints.h"

// Include System.Net.Security.Native headers
#include "pal_gssapi.h"

static const Entry s_securityNative[] =
{
    DllImportEntry(NetSecurityNative_AcceptSecContext)
    DllImportEntry(NetSecurityNative_AcquireAcceptorCred)
    DllImportEntry(NetSecurityNative_DeleteSecContext)
    DllImportEntry(NetSecurityNative_DisplayMajorStatus)
    DllImportEntry(NetSecurityNative_DisplayMinorStatus)
    DllImportEntry(NetSecurityNative_EnsureGssInitialized)
    DllImportEntry(NetSecurityNative_GetUser)
    DllImportEntry(NetSecurityNative_ImportPrincipalName)
    DllImportEntry(NetSecurityNative_ImportUserName)
    DllImportEntry(NetSecurityNative_InitiateCredSpNego)
    DllImportEntry(NetSecurityNative_InitiateCredWithPassword)
    DllImportEntry(NetSecurityNative_InitSecContext)
    DllImportEntry(NetSecurityNative_InitSecContextEx)
    DllImportEntry(NetSecurityNative_IsNtlmInstalled)
    DllImportEntry(NetSecurityNative_ReleaseCred)
    DllImportEntry(NetSecurityNative_ReleaseGssBuffer)
    DllImportEntry(NetSecurityNative_ReleaseName)
    DllImportEntry(NetSecurityNative_Unwrap)
    DllImportEntry(NetSecurityNative_Wrap)
};

EXTERN_C const void* SecurityResolveDllImport(const char* name);

EXTERN_C const void* SecurityResolveDllImport(const char* name)
{
    return ResolveDllImport(s_securityNative, lengthof(s_securityNative), name);
}
