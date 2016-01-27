// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ============================================================
//
// ApplicationContext.inl
//


//
// Implements inlined methods of ApplicationContext
//
// ============================================================

#ifndef __BINDER__APPLICATION_CONTEXT_INL__
#define __BINDER__APPLICATION_CONTEXT_INL__

LONG ApplicationContext::GetVersion()
{
    return m_cVersion;
}

void ApplicationContext::IncrementVersion()
{
    InterlockedIncrement(&m_cVersion);
}

SString &ApplicationContext::GetApplicationName()
{
    return m_applicationName;
}

DWORD ApplicationContext::GetAppDomainId()
{
    return m_dwAppDomainId;
}

void ApplicationContext::SetAppDomainId(DWORD dwAppDomainId)
{
    m_dwAppDomainId = dwAppDomainId;
}

ExecutionContext *ApplicationContext::GetExecutionContext()
{
    return m_pExecutionContext;
}

InspectionContext *ApplicationContext::GetInspectionContext()
{
    return m_pInspectionContext;
}

FailureCache *ApplicationContext::GetFailureCache()
{
    _ASSERTE(m_pFailureCache != NULL);
    return m_pFailureCache;
}

HRESULT ApplicationContext::AddToFailureCache(SString &assemblyNameOrPath,
                                              HRESULT  hrBindResult)
{
    HRESULT hr = GetFailureCache()->Add(assemblyNameOrPath, hrBindResult);
    IncrementVersion();
    return hr;
}

StringArrayList *ApplicationContext::GetAppPaths()
{
    return &m_appPaths;
}

SimpleNameToFileNameMap * ApplicationContext::GetTpaList()
{
    return m_pTrustedPlatformAssemblyMap;
}

TpaFileNameHash * ApplicationContext::GetTpaFileNameList()
{
    return m_pFileNameHash;
}

StringArrayList * ApplicationContext::GetPlatformResourceRoots()
{
    return &m_platformResourceRoots;
}

StringArrayList * ApplicationContext::GetAppNiPaths()
{
    return &m_appNiPaths;
}

CRITSEC_COOKIE ApplicationContext::GetCriticalSectionCookie()
{
    return m_contextCS;
}

#ifdef FEATURE_VERSIONING_LOG
BindingLog *ApplicationContext::GetBindingLog()
{
    return &m_bindingLog;
}

void ApplicationContext::ClearBindingLog()
{
    m_bindingLog.SetDebugLog(NULL);
}
#endif // FEATURE_VERSIONING_LOG


#endif
