// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ============================================================
//
// AssemblyIdentityCache.hpp
//


//
// Defines the AssemblyIdentityCache class and its helpers
//
// ============================================================

#ifndef __BINDER__ASSEMBLY_IDENTITY_CACHE_HPP__
#define __BINDER__ASSEMBLY_IDENTITY_CACHE_HPP__

#include "bindertypes.hpp"
#include "assemblyidentity.hpp"
#include "utils.hpp"
#include "sstring.h"
#include "shash.h"

namespace BINDER_SPACE
{
    class AssemblyIdentityCacheEntry
    {
    public:
        inline AssemblyIdentityCacheEntry()
        {
            m_szTextualIdentity = NULL;
            m_pAssemblyIdentity = NULL;
        }
        inline ~AssemblyIdentityCacheEntry()
        {
            SAFE_DELETE_ARRAY(m_szTextualIdentity);
            SAFE_DELETE(m_pAssemblyIdentity);
        }

        // Getters/Setters
        inline LPCSTR GetTextualIdentity()
        {
            return m_szTextualIdentity;
        }
        inline void SetTextualIdentity(LPCSTR szTextualIdentity)
        {
            size_t len = strlen(szTextualIdentity) + 1;

            m_szTextualIdentity = new char[len];
            strcpy_s((LPSTR) m_szTextualIdentity, len, szTextualIdentity);
        }
        inline AssemblyIdentityUTF8 *GetAssemblyIdentity()
        {
            return m_pAssemblyIdentity;
        }
        inline void SetAssemblyIdentity(AssemblyIdentityUTF8 *pAssemblyIdentity)
        {
            m_pAssemblyIdentity = pAssemblyIdentity;
        }

    protected:
        LPCSTR                m_szTextualIdentity;
        AssemblyIdentityUTF8 *m_pAssemblyIdentity;
    };

    class AssemblyIdentityHashTraits : public DefaultSHashTraits<AssemblyIdentityCacheEntry *>
    {
    public:
        typedef LPCSTR key_t;

        static key_t GetKey(element_t pAssemblyIdentityCacheEntry)
        {
            return pAssemblyIdentityCacheEntry->GetTextualIdentity();
        }
        static BOOL Equals(key_t textualIdentity1, key_t textualIdentity2)
        {
            if ((textualIdentity1 == NULL) && (textualIdentity2 == NULL))
                return TRUE;
            if ((textualIdentity1 == NULL) || (textualIdentity2 == NULL))
                return FALSE;

            return (strcmp(textualIdentity1, textualIdentity2) == 0);
        }
        static count_t Hash(key_t textualIdentity)
        {
            if (textualIdentity == NULL)
                return 0;
            else
                return HashStringA(textualIdentity);
        }
        static element_t Null()
        {
            return NULL;
        }
        static bool IsNull(const element_t &assemblyIdentityCacheEntry)
        {
            return (assemblyIdentityCacheEntry == NULL);
        }

    };

    class AssemblyIdentityCache : protected SHash<AssemblyIdentityHashTraits>
    {
    private:
        typedef SHash<AssemblyIdentityHashTraits> Hash;
    public:
        AssemblyIdentityCache();
        ~AssemblyIdentityCache();

        HRESULT Add(/* in */ LPCSTR               szTextualIdentity,
                    /* in */ AssemblyIdentityUTF8 *pAssemblyIdentity);
        AssemblyIdentityUTF8 *Lookup(/* in */ LPCSTR szTextualIdentity);
    };
};

#endif
