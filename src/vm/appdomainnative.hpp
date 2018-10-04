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

    static FCDECL4(Object*, CreateDynamicAssembly, AppDomainBaseObject* refThisUNSAFE, AssemblyNameBaseObject* assemblyNameUNSAFE, StackCrawlMark* stackMark, INT32 access);
    static FCDECL2(Object*, GetAssemblies, AppDomainBaseObject* refThisUNSAFE, CLR_BOOL fForIntrospection); 
    static FCDECL2(Object*, GetOrInternString, AppDomainBaseObject* refThisUNSAFE, StringObject* pStringUNSAFE);
    static FCDECL1(void, CreateContext, AppDomainBaseObject *refThisUNSAFE);
    static void QCALLTYPE SetupBindingPaths(__in_z LPCWSTR wszTrustedPlatformAssemblies, __in_z LPCWSTR wszPlatformResourceRoots, __in_z LPCWSTR wszAppPaths, __in_z LPCWSTR wszAppNiPaths, __in_z LPCWSTR appLocalWinMD);
    static FCDECL1(Object*, GetDynamicDir, AppDomainBaseObject* refThisUNSAFE);
    static FCDECL1(INT32, GetId, AppDomainBaseObject* refThisUNSAFE);
    static FCDECL1(void, ForceToSharedDomain, Object* pObjectUNSAFE);
    static FCDECL1(LPVOID,  GetFusionContext, AppDomainBaseObject* refThis);
    static FCDECL2(Object*, IsStringInterned, AppDomainBaseObject* refThis, StringObject* pString);
    static FCDECL3(void,    UpdateContextProperty, LPVOID fusionContext, StringObject* key, Object* value);
    static FCDECL2(StringObject*, nApplyPolicy, AppDomainBaseObject* refThisUNSAFE, AssemblyNameBaseObject* assemblyNameUNSAFE);
    static FCDECL2(FC_BOOL_RET, IsFrameworkAssembly, AppDomainBaseObject* refThisUNSAFE, AssemblyNameBaseObject* refAssemblyNameUNSAFE);
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

private:
    static INT32 ExecuteAssemblyHelper(Assembly* pAssembly,
                                       BOOL bCreatedConsole,
                                       PTRARRAYREF *pStringArgs);

public:
#ifdef FEATURE_APPX
    static
    INT32 QCALLTYPE GetAppXFlags();
#endif
};

#endif
