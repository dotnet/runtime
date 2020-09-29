// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdint.h>

// Include System.Net.Security.Native headers
#include "pal_gssapi.h"

#define FCFuncStart(name) EXTERN_C const void* name[]; const void* name[] = {
#define FCFuncEnd() (void*)0x01 /* FCFuncFlag_EndOfArray */ };

#define QCFuncElement(name,impl) \
    (void*)0x8 /* FCFuncFlag_QCall */, (void*)(impl), (void*)name,

FCFuncStart(gEmbedded_NetSecurityNative)
    QCFuncElement("AcceptSecContext", NetSecurityNative_AcceptSecContext)
    QCFuncElement("AcquireAcceptorCred", NetSecurityNative_AcquireAcceptorCred)
    QCFuncElement("DeleteSecContext", NetSecurityNative_DeleteSecContext)
    QCFuncElement("DisplayMajorStatus", NetSecurityNative_DisplayMajorStatus)
    QCFuncElement("DisplayMinorStatus", NetSecurityNative_DisplayMinorStatus)
    QCFuncElement("GetUser", NetSecurityNative_GetUser)
    QCFuncElement("ImportPrincipalName", NetSecurityNative_ImportPrincipalName)
    QCFuncElement("ImportUserName", NetSecurityNative_ImportUserName)
    QCFuncElement("InitiateCredSpNego", NetSecurityNative_InitiateCredSpNego)
    QCFuncElement("InitiateCredWithPassword", NetSecurityNative_InitiateCredWithPassword)
    QCFuncElement("InitSecContext", NetSecurityNative_InitSecContext)
    QCFuncElement("InitSecContext", NetSecurityNative_InitSecContextEx)
    QCFuncElement("IsNtlmInstalled", NetSecurityNative_IsNtlmInstalled)
    QCFuncElement("ReleaseCred", NetSecurityNative_ReleaseCred)
    QCFuncElement("ReleaseGssBuffer", NetSecurityNative_ReleaseGssBuffer)
    QCFuncElement("ReleaseName", NetSecurityNative_ReleaseName)
    QCFuncElement("Unwrap", NetSecurityNative_Unwrap)
    QCFuncElement("Wrap", NetSecurityNative_Wrap)
FCFuncEnd()
