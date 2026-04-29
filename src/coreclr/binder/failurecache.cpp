// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ============================================================
//
// FailureCache.cpp
//


//
// Implements the FailureCache class
//
// ============================================================

#include "common.h"
#include "failurecache.hpp"

namespace BINDER_SPACE
{
    FailureCache::FailureCache() : SHash<FailureCacheHashTraits>::SHash()
    {
        // Nothing to do here
    }

    FailureCache::~FailureCache()
    {
        // Delete entries and contents array
        for (Hash::Iterator i = Hash::Begin(), end = Hash::End(); i != end; i++)
        {
            const FailureCacheEntry *pFailureCacheEntry = *i;
            delete pFailureCacheEntry;
        }
        RemoveAll();
    }

    HRESULT FailureCache::Add(SString &assemblyNameorPath,
                              HRESULT hrBindingResult,
                              LPCWSTR diagnosticInfo)
    {
        HRESULT hr = S_OK;

        NewHolder<FailureCacheEntry> pFailureCacheEntry;
        SAFE_NEW(pFailureCacheEntry, FailureCacheEntry);

        // No error occurred; report the original error
        hr = hrBindingResult;

        pFailureCacheEntry->GetAssemblyNameOrPath().Set(assemblyNameorPath);
        pFailureCacheEntry->SetBindingResult(hrBindingResult);
        if (diagnosticInfo != nullptr && *diagnosticInfo != W('\0'))
        {
            pFailureCacheEntry->SetDiagnosticInfo(diagnosticInfo);
        }

        Hash::Add(pFailureCacheEntry);
        pFailureCacheEntry.SuppressRelease();

    Exit:
        return hr;
    }

    HRESULT FailureCache::Lookup(SString &assemblyNameorPath,
                                 SString *pDiagnosticInfo)
    {
        HRESULT hr = S_OK;
        FailureCacheEntry *pFailureCachEntry = Hash::Lookup(assemblyNameorPath);

        if (pFailureCachEntry != NULL)
        {
            hr = pFailureCachEntry->GetBindingResult();
            if (pDiagnosticInfo != NULL && !pFailureCachEntry->GetDiagnosticInfo().IsEmpty())
            {
                StackSString format;
                format.LoadResource(IDS_BINDING_CACHED_FAILURE_PREFIX);
                pDiagnosticInfo->Printf(format.GetUTF8(), pFailureCachEntry->GetDiagnosticInfo().GetUTF8());
            }
        }

        return hr;
    }

    void FailureCache::Remove(SString &assemblyName)
    {
        FailureCacheEntry *pFailureCachEntry = Hash::Lookup(assemblyName);

        // Hash::Remove does not clean up entries
        Hash::Remove(assemblyName);
        SAFE_DELETE(pFailureCachEntry);
    }
};
