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
#include "assemblyidentitycache.hpp"
#include "utils.hpp"
#include "ex.h"
#include "clr/fs/path.h"
using namespace clr::fs;

namespace BINDER_SPACE
{
    STDMETHODIMP ApplicationContext::QueryInterface(REFIID   riid,
                                                    void   **ppv)
    {
        HRESULT hr = S_OK;

        if (ppv == NULL)
        {
            hr = E_POINTER;
        }
        else
        {
            if (IsEqualIID(riid, IID_IUnknown))
            {
                AddRef();
                *ppv = static_cast<IUnknown *>(this);
            }
            else
            {
                *ppv = NULL;
                hr = E_NOINTERFACE;
            }
        }

        return hr;
    }

    STDMETHODIMP_(ULONG) ApplicationContext::AddRef()
    {
        return InterlockedIncrement(&m_cRef);
    }

    STDMETHODIMP_(ULONG) ApplicationContext::Release()
    {
        ULONG ulRef = InterlockedDecrement(&m_cRef);

        if (ulRef == 0)
        {
            delete this;
        }

        return ulRef;
    }

    ApplicationContext::ApplicationContext()
    {
        m_cRef = 1;
        m_dwAppDomainId = 0;
        m_pExecutionContext = NULL;
        m_pFailureCache = NULL;
        m_contextCS = NULL;
        m_pTrustedPlatformAssemblyMap = nullptr;
        m_binderID = 0;
    }

    ApplicationContext::~ApplicationContext()
    {
        SAFE_RELEASE(m_pExecutionContext);
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

    HRESULT ApplicationContext::Init(UINT_PTR binderID)
    {
        HRESULT hr = S_OK;

        ReleaseHolder<ExecutionContext> pExecutionContext;

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

        m_binderID = binderID;
    Exit:
        return hr;
    }

    HRESULT ApplicationContext::SetupBindingPaths(SString &sTrustedPlatformAssemblies,
                                                  SString &sPlatformResourceRoots,
                                                  SString &sAppPaths,
                                                  SString &sAppNiPaths,
                                                  BOOL     fAcquireLock)
    {
        HRESULT hr = S_OK;

#ifndef CROSSGEN_COMPILE
        CRITSEC_Holder contextLock(fAcquireLock ? GetCriticalSectionCookie() : NULL);
#endif
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

#ifndef CROSSGEN_COMPILE
            if (Path::IsRelative(pathName))
            {
                GO_WITH_HRESULT(E_INVALIDARG);
            }
#endif

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

#ifndef CROSSGEN_COMPILE
            if (Path::IsRelative(pathName))
            {
                GO_WITH_HRESULT(E_INVALIDARG);
            }
#endif

            m_appPaths.Append(pathName);
        }

        //
        // Parse AppNiPaths
        //
        sAppNiPaths.Normalize();
        for (SString::Iterator i = sAppNiPaths.Begin(); i != sAppNiPaths.End(); )
        {
            SString pathName;
            HRESULT pathResult = S_OK;

            IF_FAIL_GO(pathResult = GetNextPath(sAppNiPaths, i, pathName));
            if (pathResult == S_FALSE)
            {
                break;
            }

#ifndef CROSSGEN_COMPILE
            if (Path::IsRelative(pathName))
            {
                GO_WITH_HRESULT(E_INVALIDARG);
            }
#endif

            m_appNiPaths.Append(pathName);
        }

    Exit:
        return hr;
    }

    HRESULT ApplicationContext::GetAssemblyIdentity(LPCSTR                 szTextualIdentity,
                                                    AssemblyIdentityUTF8 **ppAssemblyIdentity)
    {
        HRESULT hr = S_OK;

        _ASSERTE(szTextualIdentity != NULL);
        _ASSERTE(ppAssemblyIdentity != NULL);

        CRITSEC_Holder contextLock(GetCriticalSectionCookie());

        AssemblyIdentityUTF8 *pAssemblyIdentity = m_assemblyIdentityCache.Lookup(szTextualIdentity);
        if (pAssemblyIdentity == NULL)
        {
            NewHolder<AssemblyIdentityUTF8> pNewAssemblyIdentity;
            SString sTextualIdentity;

            SAFE_NEW(pNewAssemblyIdentity, AssemblyIdentityUTF8);
            sTextualIdentity.SetUTF8(szTextualIdentity);

            IF_FAIL_GO(TextualIdentityParser::Parse(sTextualIdentity, pNewAssemblyIdentity));
            IF_FAIL_GO(m_assemblyIdentityCache.Add(szTextualIdentity, pNewAssemblyIdentity));

            pNewAssemblyIdentity->PopulateUTF8Fields();

            pAssemblyIdentity = pNewAssemblyIdentity.Extract();
        }

        *ppAssemblyIdentity = pAssemblyIdentity;

    Exit:
        return hr;
    }

    bool ApplicationContext::IsTpaListProvided()
    {
        return m_pTrustedPlatformAssemblyMap != nullptr;
    }
};
