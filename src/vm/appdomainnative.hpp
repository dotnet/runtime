// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



/*============================================================
**
** Header:  AppDomainNative.hpp
**
** Purpose: Implements native methods for AppDomains
**
**
===========================================================*/
#ifndef _APPDOMAINNATIVE_H
#define _APPDOMAINNATIVE_H

#include "qcall.h"

class AppDomainNative
{
public:
    static AppDomain *ValidateArg(APPDOMAINREF pThis);
    static FCDECL2(void, SetupFriendlyName, AppDomainBaseObject* refThisUNSAFE, StringObject* strFriendlyNameUNSAFE);

    static FCDECL3(Object*, CreateDynamicAssembly, AssemblyNameBaseObject* assemblyNameUNSAFE, StackCrawlMark* stackMark, INT32 access);
    static FCDECL0(Object*, GetLoadedAssemblies);
    static FCDECL1(Object*, GetOrInternString, StringObject* pStringUNSAFE);
    static void QCALLTYPE SetupBindingPaths(__in_z LPCWSTR wszTrustedPlatformAssemblies, __in_z LPCWSTR wszPlatformResourceRoots, __in_z LPCWSTR wszAppPaths, __in_z LPCWSTR wszAppNiPaths, __in_z LPCWSTR appLocalWinMD);
    static FCDECL1(INT32, GetId, AppDomainBaseObject* refThisUNSAFE);
    static FCDECL1(Object*, IsStringInterned, StringObject* pString);
    static FCDECL1(UINT32,  GetAppDomainId, AppDomainBaseObject* refThisUNSAFE);
    static FCDECL1(void , PublishAnonymouslyHostedDynamicMethodsAssembly, AssemblyBaseObject * pAssemblyUNSAFE);
    static void QCALLTYPE SetNativeDllSearchDirectories(__in_z LPCWSTR wszAssembly);

#ifdef FEATURE_APPDOMAIN_RESOURCE_MONITORING
    static FCDECL0(void,  EnableMonitoring);
    static FCDECL0(FC_BOOL_RET, MonitoringIsEnabled);
    static FCDECL1(INT64, GetTotalProcessorTime, AppDomainBaseObject* refThisUNSAFE);
    static FCDECL1(INT64, GetTotalAllocatedMemorySize, AppDomainBaseObject* refThisUNSAFE);
    static FCDECL1(INT64, GetLastSurvivedMemorySize, AppDomainBaseObject* refThisUNSAFE);
    static FCDECL0(INT64, GetLastSurvivedProcessMemorySize);
#endif // FEATURE_APPDOMAIN_RESOURCE_MONITORING

#ifdef FEATURE_APPX
    static
    INT32 QCALLTYPE IsAppXProcess();
#endif
};

#endif
