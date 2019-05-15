// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ============================================================
//
// ApplicationContext.cpp
//


//
// Implements the ApplicationContext class
//
// ============================================================

#ifndef FEATURE_CORESYSTEM
#define DISABLE_BINDER_DEBUG_LOGGING
#endif

#include "applicationcontext.hpp"
#include "stringarraylist.h"
#include "loadcontext.hpp"
#include "propertymap.hpp"
#include "failurecache.hpp"
#include "assemblyidentitycache.hpp"
#ifdef FEATURE_VERSIONING_LOG
#include "debuglog.hpp"
#endif // FEATURE_VERSIONING_LOG
#include "utils.hpp"
#include "variables.hpp"
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
        m_pInspectionContext = NULL;
        m_pFailureCache = NULL;
        m_contextCS = NULL;
        m_pTrustedPlatformAssemblyMap = nullptr;
        m_pFileNameHash = nullptr;
    }

    ApplicationContext::~ApplicationContext()
    {
        SAFE_RELEASE(m_pExecutionContext);
        SAFE_RELEASE(m_pInspectionContext);
        SAFE_DELETE(m_pFailureCache);

        if (m_contextCS != NULL)
        {
            ClrDeleteCriticalSection(m_contextCS);
        }

        if (m_pTrustedPlatformAssemblyMap != nullptr)
        {
            delete m_pTrustedPlatformAssemblyMap;
        }

        if (m_pFileNameHash != nullptr)
        {
            delete m_pFileNameHash;
        }
    }

    HRESULT ApplicationContext::Init()
    {
        HRESULT hr = S_OK;
        BINDER_LOG_ENTER(W("ApplicationContext::Init"));
        BINDER_LOG_POINTER(W("this"), this);

        ReleaseHolder<ExecutionContext> pExecutionContext;
        ReleaseHolder<InspectionContext> pInspectionContext;

        PropertyMap *pPropertyMap = NULL;
        FailureCache *pFailureCache = NULL;

        // Allocate context objects
        SAFE_NEW(pExecutionContext, ExecutionContext);
        SAFE_NEW(pInspectionContext, InspectionContext);

        SAFE_NEW(pFailureCache, FailureCache);

        m_contextCS = ClrCreateCriticalSection(
                                               CrstFusionAppCtx,
                                               CRST_REENTRANCY);
        if (!m_contextCS)
        {
            SAFE_DELETE(pPropertyMap);
            SAFE_DELETE(pFailureCache);
            hr = E_OUTOFMEMORY;
        }
        else
        {
            m_pExecutionContext = pExecutionContext.Extract();
            m_pInspectionContext = pInspectionContext.Extract();

            m_pFailureCache = pFailureCache;
        }
        
    Exit:
        BINDER_LOG_LEAVE_HR(W("ApplicationContext::Init"), hr);
        return hr;
    }

    HRESULT GetNextPath(SString& paths, SString::Iterator& startPos, SString& outPath)
    {
        HRESULT hr = S_OK;

        bool wrappedWithQuotes = false;

        // Skip any leading spaces or path separators
        while (paths.Skip(startPos, W(' ')) || paths.Skip(startPos, PATH_SEPARATOR_CHAR_W)) {}

        if (startPos == paths.End())
        {
            // No more paths in the string and we just skipped over some white space
            outPath.Set(W(""));
            return S_FALSE;
        }

        // Support paths being wrapped with quotations
        if (paths.Skip(startPos, W('\"')))
        {
            wrappedWithQuotes = true;
        }

        SString::Iterator iEnd = startPos;      // Where current path ends
        SString::Iterator iNext;                // Where next path starts
        if (wrappedWithQuotes)
        {
            if (paths.Find(iEnd, W('\"')))
            {
                iNext = iEnd;
                // Find where the next path starts - there should be a path separator right after the closing quotation mark
                if (paths.Find(iNext, PATH_SEPARATOR_CHAR_W))
                {
                    iNext++;
                }
                else
                {
                    iNext = paths.End();
                }
            }
            else
            {
                // There was no terminating quotation mark - that's bad
                GO_WITH_HRESULT(E_INVALIDARG);
            }
        }
        else if (paths.Find(iEnd, PATH_SEPARATOR_CHAR_W))
        {
            iNext = iEnd + 1;
        }
        else
        {
            iNext = iEnd = paths.End();
        }

        // Skip any trailing spaces
        while (iEnd[-1] == W(' '))
        {
            iEnd--;
        }

        _ASSERTE(startPos < iEnd);

        outPath.Set(paths, startPos, iEnd);
        startPos = iNext;
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
        BINDER_LOG_ENTER(W("ApplicationContext::SetupBindingPaths"));
        BINDER_LOG_POINTER(W("this"), this);

#ifndef CROSSGEN_COMPILE
        CRITSEC_Holder contextLock(fAcquireLock ? GetCriticalSectionCookie() : NULL);
#endif
        if (m_pTrustedPlatformAssemblyMap != nullptr)
        {
#if defined(BINDER_DEBUG_LOG)
            BINDER_LOG(W("ApplicationContext::SetupBindingPaths: Binding paths already setup"));
#endif // BINDER_LOG_STRING
            GO_WITH_HRESULT(S_OK);
        }


        //
        // Parse TrustedPlatformAssemblies
        //
        m_pTrustedPlatformAssemblyMap = new SimpleNameToFileNameMap();
        m_pFileNameHash = new TpaFileNameHash();
        
        sTrustedPlatformAssemblies.Normalize();

        for (SString::Iterator i = sTrustedPlatformAssemblies.Begin(); i != sTrustedPlatformAssemblies.End(); )
        {
            SString fileName;
            HRESULT pathResult = S_OK;
            IF_FAIL_GO(pathResult = GetNextPath(sTrustedPlatformAssemblies, i, fileName));
            if (pathResult == S_FALSE)
            {
                break;
            }

#ifndef CROSSGEN_COMPILE
            if (Path::IsRelative(fileName))
            {
                BINDER_LOG_STRING(W("ApplicationContext::SetupBindingPaths: Relative path not allowed"), fileName);
                GO_WITH_HRESULT(E_INVALIDARG);
            }
#endif

            // Find the beginning of the simple name
            SString::Iterator iSimpleNameStart = fileName.End();
            
            if (!fileName.FindBack(iSimpleNameStart, DIRECTORY_SEPARATOR_CHAR_W))
            {
                iSimpleNameStart = fileName.Begin();
            }
            else
            {
                // Advance past the directory separator to the first character of the file name
                iSimpleNameStart++;
            }

            if (iSimpleNameStart == fileName.End())
            {
                GO_WITH_HRESULT(E_INVALIDARG);
            }

            SString simpleName;
            bool isNativeImage = false;

            // GCC complains if we create SStrings inline as part of a function call
            SString sNiDll(W(".ni.dll"));
            SString sNiExe(W(".ni.exe"));
            SString sNiWinmd(W(".ni.winmd"));
            SString sDll(W(".dll"));
            SString sExe(W(".exe"));
            SString sWinmd(W(".winmd"));
            
            if (fileName.EndsWithCaseInsensitive(sNiDll) ||
                fileName.EndsWithCaseInsensitive(sNiExe))
            {
                simpleName.Set(fileName, iSimpleNameStart, fileName.End() - 7);
                isNativeImage = true;
            }
            else if (fileName.EndsWithCaseInsensitive(sNiWinmd))
            {
                simpleName.Set(fileName, iSimpleNameStart, fileName.End() - 9);
                isNativeImage = true;
            }
            else if (fileName.EndsWithCaseInsensitive(sDll) ||
                     fileName.EndsWithCaseInsensitive(sExe))
            {
                simpleName.Set(fileName, iSimpleNameStart, fileName.End() - 4);
            }
            else if (fileName.EndsWithCaseInsensitive(sWinmd))
            {
                simpleName.Set(fileName, iSimpleNameStart, fileName.End() - 6);
            }
            else
            {
                // Invalid filename
                GO_WITH_HRESULT(E_INVALIDARG);
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
                    BINDER_LOG_STRING(W("ApplicationContext::SetupBindingPaths: Skipping TPA entry because of already existing IL/NI entry for short name "), fileName.GetUnicode());
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
            
            FileNameMapEntry fileNameExistenceEntry;
            fileNameExistenceEntry.m_wszFileName = wszFileName;
            m_pFileNameHash->AddOrReplace(fileNameExistenceEntry);
            
            BINDER_LOG_STRING(W("ApplicationContext::SetupBindingPaths: Added TPA entry"), wszFileName);
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
                BINDER_LOG_STRING(W("ApplicationContext::SetupBindingPaths: Relative path not allowed"), pathName);
                GO_WITH_HRESULT(E_INVALIDARG);
            }
#endif

            m_platformResourceRoots.Append(pathName);
            BINDER_LOG_STRING(W("ApplicationContext::SetupBindingPaths: Added resource root"), pathName);
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
                BINDER_LOG_STRING(W("ApplicationContext::SetupBindingPaths: Relative path not allowed"), pathName);
                GO_WITH_HRESULT(E_INVALIDARG);
            }
#endif

            m_appPaths.Append(pathName);
            BINDER_LOG_STRING(W("ApplicationContext::SetupBindingPaths: Added App Path"), pathName);
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
                BINDER_LOG_STRING(W("ApplicationContext::SetupBindingPaths: Relative path not allowed"), pathName);
                GO_WITH_HRESULT(E_INVALIDARG);
            }
#endif

            m_appNiPaths.Append(pathName);
            BINDER_LOG_STRING(W("ApplicationContext::SetupBindingPaths: Added App NI Path"), pathName);
        }

    Exit:
        BINDER_LOG_LEAVE_HR(W("ApplicationContext::SetupBindingPaths"), hr);
        return hr;
    }

    HRESULT ApplicationContext::GetAssemblyIdentity(LPCSTR                 szTextualIdentity,
                                                    AssemblyIdentityUTF8 **ppAssemblyIdentity)
    {
        HRESULT hr = S_OK;
        BINDER_LOG_ENTER(W("ApplicationContext::GetAssemblyIdentity"));
        BINDER_LOG_POINTER(W("this"), this);

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
        BINDER_LOG_LEAVE_HR(W("ApplicationContext::GetAssemblyIdentity"), hr);
        return hr;
    }

    bool ApplicationContext::IsTpaListProvided()
    {
        return m_pTrustedPlatformAssemblyMap != nullptr;
    }
};
