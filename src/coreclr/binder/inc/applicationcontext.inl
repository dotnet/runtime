// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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

ExecutionContext *ApplicationContext::GetExecutionContext()
{
    return m_pExecutionContext;
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

StringArrayList * ApplicationContext::GetPlatformResourceRoots()
{
    return &m_platformResourceRoots;
}

CRITSEC_COOKIE ApplicationContext::GetCriticalSectionCookie()
{
    return m_contextCS;
}

#endif
