// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ============================================================
//
// ApplicationContext.cpp
//


//
// Implements the ApplicationContext class
//
// ============================================================

#include "applicationcontext.hpp"
#include "stringarraylist.h"
#include "loadcontext.hpp"
#include "failurecache.hpp"
#include "utils.hpp"
#include "ex.h"
#include "clr/fs/path.h"
using namespace clr::fs;

namespace BINDER_SPACE
{
    ApplicationContext::ApplicationContext()
    {
        m_pExecutionContext = NULL;
        m_pFailureCache = NULL;
        m_contextCS = NULL;
        m_pTrustedPlatformAssemblyMap = nullptr;
    }

    ApplicationContext::~ApplicationContext()
    {
        SAFE_DELETE(m_pExecutionContext);
        SAFE_DELETE(m_pFailureCache);

        if (m_contextCS != NULL)
        {
            ClrDeleteCriticalSection(m_contextCS);
        }

        if (m_pTrustedPlatformAssemblyMap != nullptr)
        {
            delete m_pTrustedPlatformAssemblyMap;
        }
    }

    HRESULT ApplicationContext::Init()
    {
        HRESULT hr = S_OK;

        NewHolder<ExecutionContext> pExecutionContext;

        FailureCache *pFailureCache = NULL;

        // Allocate context objects
        SAFE_NEW(pExecutionContext, ExecutionContext);

        SAFE_NEW(pFailureCache, FailureCache);

        m_contextCS = ClrCreateCriticalSection(
                                               CrstFusionAppCtx,
                                               CRST_REENTRANCY);
        if (!m_contextCS)
        {
            SAFE_DELETE(pFailureCache);
            hr = E_OUTOFMEMORY;
        }
        else
        {
            m_pExecutionContext = pExecutionContext.Extract();

            m_pFailureCache = pFailureCache;
        }

    Exit:
        return hr;
    }

    HRESULT ApplicationContext::SetupBindingPaths(SString &sTrustedPlatformAssemblies,
                                                  SString &sPlatformResourceRoots,
                                                  SString &sAppPaths,
                                                  BOOL     fAcquireLock)
    {
        HRESULT hr = S_OK;

        CRITSEC_Holder contextLock(fAcquireLock ? GetCriticalSectionCookie() : NULL);
        if (m_pTrustedPlatformAssemblyMap != nullptr)
        {
            GO_WITH_HRESULT(S_OK);
        }

        //
        // Parse TrustedPlatformAssemblies
        //
        m_pTrustedPlatformAssemblyMap = new SimpleNameToFileNameMap();

        sTrustedPlatformAssemblies.Normalize();

        for (SString::Iterator i = sTrustedPlatformAssemblies.Begin(); i != sTrustedPlatformAssemblies.End(); )
        {
            SString fileName;
            SString simpleName;
            bool isNativeImage = false;
            HRESULT pathResult = S_OK;
            IF_FAIL_GO(pathResult = GetNextTPAPath(sTrustedPlatformAssemblies, i, /*dllOnly*/ false, fileName, simpleName, isNativeImage));
            if (pathResult == S_FALSE)
            {
                break;
            }

            const SimpleNameToFileNameMapEntry *pExistingEntry = m_pTrustedPlatformAssemblyMap->LookupPtr(simpleName.GetUnicode());

            if (pExistingEntry != nullptr)
            {
                //
                // We want to store only the first entry matching a simple name we encounter.
                // The exception is if we first store an IL reference and later in the string
                // we encounter a native image.  Since we don't touch IL in the presence of
                // native images, we replace the IL entry with the NI.
                //
                if ((pExistingEntry->m_wszILFileName != nullptr && !isNativeImage) ||
                    (pExistingEntry->m_wszNIFileName != nullptr && isNativeImage))
                {
                    continue;
                }
            }

            LPWSTR wszSimpleName = nullptr;
            if (pExistingEntry == nullptr)
            {
                wszSimpleName = new WCHAR[simpleName.GetCount() + 1];
                if (wszSimpleName == nullptr)
                {
                    GO_WITH_HRESULT(E_OUTOFMEMORY);
                }
                wcscpy_s(wszSimpleName, simpleName.GetCount() + 1, simpleName.GetUnicode());
            }
            else
            {
                wszSimpleName = pExistingEntry->m_wszSimpleName;
            }

            LPWSTR wszFileName = new WCHAR[fileName.GetCount() + 1];
            if (wszFileName == nullptr)
            {
                GO_WITH_HRESULT(E_OUTOFMEMORY);
            }
            wcscpy_s(wszFileName, fileName.GetCount() + 1, fileName.GetUnicode());

            SimpleNameToFileNameMapEntry mapEntry;
            mapEntry.m_wszSimpleName = wszSimpleName;
            if (isNativeImage)
            {
                mapEntry.m_wszNIFileName = wszFileName;
                mapEntry.m_wszILFileName = pExistingEntry == nullptr ? nullptr : pExistingEntry->m_wszILFileName;
            }
            else
            {
                mapEntry.m_wszILFileName = wszFileName;
                mapEntry.m_wszNIFileName = pExistingEntry == nullptr ? nullptr : pExistingEntry->m_wszNIFileName;
            }

            m_pTrustedPlatformAssemblyMap->AddOrReplace(mapEntry);
        }

        //
        // Parse PlatformResourceRoots
        //
        sPlatformResourceRoots.Normalize();
        for (SString::Iterator i = sPlatformResourceRoots.Begin(); i != sPlatformResourceRoots.End(); )
        {
            SString pathName;
            HRESULT pathResult = S_OK;

            IF_FAIL_GO(pathResult = GetNextPath(sPlatformResourceRoots, i, pathName));
            if (pathResult == S_FALSE)
            {
                break;
            }

            if (Path::IsRelative(pathName))
            {
                GO_WITH_HRESULT(E_INVALIDARG);
            }

            m_platformResourceRoots.Append(pathName);
        }

        //
        // Parse AppPaths
        //
        sAppPaths.Normalize();
        for (SString::Iterator i = sAppPaths.Begin(); i != sAppPaths.End(); )
        {
            SString pathName;
            HRESULT pathResult = S_OK;

            IF_FAIL_GO(pathResult = GetNextPath(sAppPaths, i, pathName));
            if (pathResult == S_FALSE)
            {
                break;
            }

            if (Path::IsRelative(pathName))
            {
                GO_WITH_HRESULT(E_INVALIDARG);
            }

            m_appPaths.Append(pathName);
        }

    Exit:
        return hr;
    }

    bool ApplicationContext::IsTpaListProvided()
    {
        return m_pTrustedPlatformAssemblyMap != nullptr;
    }
};
