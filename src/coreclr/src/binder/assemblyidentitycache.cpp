// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ============================================================
//
// AssemblyIdentityCache.cpp
//


//
// Implements the AssemblyIdentityCache class
//
// ============================================================

#include "assemblyidentitycache.hpp"

namespace BINDER_SPACE
{
    AssemblyIdentityCache::AssemblyIdentityCache() : SHash<AssemblyIdentityHashTraits>::SHash()
    {
        // Nothing to do here
    }

    AssemblyIdentityCache::~AssemblyIdentityCache()
    {
        // Delete entries and contents array
        for (Hash::Iterator i = Hash::Begin(), end = Hash::End(); i != end; i++)
        {
            const AssemblyIdentityCacheEntry *pAssemblyIdentityCacheEntry = *i;
            delete pAssemblyIdentityCacheEntry;
        }
        RemoveAll();
    }

    HRESULT AssemblyIdentityCache::Add(LPCSTR                szTextualIdentity,
                                       AssemblyIdentityUTF8 *pAssemblyIdentity)
    {
        HRESULT hr = S_OK;

        NewHolder<AssemblyIdentityCacheEntry> pAssemblyIdentityCacheEntry;
        SAFE_NEW(pAssemblyIdentityCacheEntry, AssemblyIdentityCacheEntry);

        pAssemblyIdentityCacheEntry->SetTextualIdentity(szTextualIdentity);
        pAssemblyIdentityCacheEntry->SetAssemblyIdentity(pAssemblyIdentity);

        Hash::Add(pAssemblyIdentityCacheEntry);
        pAssemblyIdentityCacheEntry.SuppressRelease();

    Exit:
        return hr;
    }

    AssemblyIdentityUTF8 *AssemblyIdentityCache::Lookup(LPCSTR szTextualIdentity)
    {
        AssemblyIdentityUTF8 *pAssemblyIdentity = NULL;
        AssemblyIdentityCacheEntry *pAssemblyIdentityCacheEntry = Hash::Lookup(szTextualIdentity);

        if (pAssemblyIdentityCacheEntry != NULL)
        {
            pAssemblyIdentity = pAssemblyIdentityCacheEntry->GetAssemblyIdentity();
        }

        return pAssemblyIdentity;
    }
};
