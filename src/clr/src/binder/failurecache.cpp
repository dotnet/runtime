// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ============================================================
//
// FailureCache.cpp
//


//
// Implements the FailureCache class
//
// ============================================================

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
                              HRESULT hrBindingResult)
    {
        HRESULT hr = S_OK;
        BINDER_LOG_ENTER(L"FailureCache::Add");

        NewHolder<FailureCacheEntry> pFailureCacheEntry;
        SAFE_NEW(pFailureCacheEntry, FailureCacheEntry);

        // No error occurred; report the original error
        hr = hrBindingResult;

        pFailureCacheEntry->GetAssemblyNameOrPath().Set(assemblyNameorPath);
        pFailureCacheEntry->SetBindingResult(hrBindingResult);
        
        Hash::Add(pFailureCacheEntry);
        pFailureCacheEntry.SuppressRelease();

    Exit:
        BINDER_LOG_LEAVE_HR(L"FailureCache::Add", hr);
        return hr;
    }

    HRESULT FailureCache::Lookup(SString &assemblyNameorPath)
    {
        HRESULT hr = S_OK;
        BINDER_LOG_ENTER(L"FailureCache::Lookup");
        FailureCacheEntry *pFailureCachEntry = Hash::Lookup(assemblyNameorPath);

        if (pFailureCachEntry != NULL)
        {
            hr = pFailureCachEntry->GetBindingResult();
        }

        BINDER_LOG_LEAVE_HR(L"FailureCache::Lookup", hr);
        return hr;
    }

    void FailureCache::Remove(SString &assemblyName)
    {
        BINDER_LOG_ENTER(L"FailureCache::Remove");

        FailureCacheEntry *pFailureCachEntry = Hash::Lookup(assemblyName);

        // Hash::Remove does not clean up entries
        Hash::Remove(assemblyName);
        SAFE_DELETE(pFailureCachEntry);
        
        BINDER_LOG_LEAVE(L"FailureCache::Remove");
    }
};
