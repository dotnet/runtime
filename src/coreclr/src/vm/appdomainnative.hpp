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
#if FEATURE_COMINTEROP
    static FCDECL1(void, SetDisableInterfaceCache, AppDomainBaseObject* refThisUNSAFE);
#endif // FEATURE_COMINTEROP
    static FCDECL1(void*, GetSecurityDescriptor, AppDomainBaseObject* refThisUNSAFE);
#ifdef FEATURE_LOADER_OPTIMIZATION    
    static FCDECL2(void, UpdateLoaderOptimization, AppDomainBaseObject* refThisUNSAFE, DWORD optimization);
#endif // FEATURE_LOADER_OPTIMIZATION

    static FCDECL9(Object*, CreateDynamicAssembly, AppDomainBaseObject* refThisUNSAFE, AssemblyNameBaseObject* assemblyNameUNSAFE, Object* identityUNSAFE, StackCrawlMark* stackMark, U1Array* securityRulesBlobUNSAFE, U1Array* aptcaBlobUNSAFE, INT32 access, INT32 flags, SecurityContextSource securityContextSource);
    static FCDECL1(void, SetHostSecurityManagerFlags, DWORD dwFlags);
    static FCDECL1(Object*, GetFriendlyName, AppDomainBaseObject* refThisUNSAFE);
    static FCDECL1(FC_BOOL_RET, IsDefaultAppDomainForEvidence, AppDomainBaseObject* refThisUNSAFE);
    static FCDECL2(Object*, GetAssemblies, AppDomainBaseObject* refThisUNSAFE, CLR_BOOL fForIntrospection); 
    static FCDECL2(Object*, GetOrInternString, AppDomainBaseObject* refThisUNSAFE, StringObject* pStringUNSAFE);
    static FCDECL1(void, CreateContext, AppDomainBaseObject *refThisUNSAFE);
    static void QCALLTYPE SetupBindingPaths(__in_z LPCWSTR wszTrustedPlatformAssemblies, __in_z LPCWSTR wszPlatformResourceRoots, __in_z LPCWSTR wszAppPaths, __in_z LPCWSTR wszAppNiPaths, __in_z LPCWSTR appLocalWinMD);
    static FCDECL1(void, Unload, INT32 dwId);
    static FCDECL1(Object*, GetDynamicDir, AppDomainBaseObject* refThisUNSAFE);
    static FCDECL1(INT32, GetId, AppDomainBaseObject* refThisUNSAFE);
    static FCDECL1(INT32, GetIdForUnload, AppDomainBaseObject* refDomainUNSAFE);
    static FCDECL1(FC_BOOL_RET, IsDomainIdValid, INT32 dwId);
    static FCDECL1(FC_BOOL_RET, IsFinalizingForUnload, AppDomainBaseObject* refThisUNSAFE);
    static FCDECL1(void, ForceToSharedDomain, Object* pObjectUNSAFE);
    static FCDECL1(void, ChangeSecurityPolicy, AppDomainBaseObject* refThisUNSAFE);
    static FCDECL1(LPVOID,  GetFusionContext, AppDomainBaseObject* refThis);
    static FCDECL2(Object*, IsStringInterned, AppDomainBaseObject* refThis, StringObject* pString);
    static FCDECL1(FC_BOOL_RET, IsUnloadingForcedFinalize, AppDomainBaseObject* refThis);
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
    static
    void QCALLTYPE SetupDomainSecurity(QCall::AppDomainHandle pDomain,
                                       QCall::ObjectHandleOnStack ohEvidence,
                                       IApplicationSecurityDescriptor *pParentSecurityDescriptor,
                                       BOOL fPublishAppDomain);

    static
    void QCALLTYPE GetGrantSet(QCall::AppDomainHandle adhTarget,
                               QCall::ObjectHandleOnStack retGrantSet);


    static
    BOOL QCALLTYPE DisableFusionUpdatesFromADManager(QCall::AppDomainHandle adhTarget);

#ifdef FEATURE_APPX
    static
    INT32 QCALLTYPE GetAppXFlags();
#endif

    static
    void QCALLTYPE GetAppDomainManagerType(QCall::AppDomainHandle adhTarget,
                                           QCall::StringHandleOnStack shRetAssembly,
                                           QCall::StringHandleOnStack shRetType);

    static
    void QCALLTYPE SetAppDomainManagerType(QCall::AppDomainHandle adhTarget,
                                           __in_z LPCWSTR wszAssembly,
                                           __in_z LPCWSTR wszType);

    static
    void QCALLTYPE SetSecurityHomogeneousFlag(QCall::AppDomainHandle adhTarget,
                                              BOOL fRuntimeSuppliedHomgenousGrantSet);




};

#endif
