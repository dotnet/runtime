// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ============================================================
//
// AssemblyIdentity.hpp
//


//
// Defines the AssemblyIdentity class
//
// ============================================================

#ifndef __BINDER__ASSEMBLY_IDENTITY_HPP__
#define __BINDER__ASSEMBLY_IDENTITY_HPP__

#include "bindertypes.hpp"
#include "assemblyversion.hpp"

namespace BINDER_SPACE
{
    class AssemblyIdentity
    {
    public:
        enum
        {
            IDENTITY_FLAG_EMPTY                  = 0x000,
            IDENTITY_FLAG_SIMPLE_NAME            = 0x001,
            IDENTITY_FLAG_VERSION                = 0x002,
            IDENTITY_FLAG_PUBLIC_KEY_TOKEN       = 0x004,
            IDENTITY_FLAG_PUBLIC_KEY             = 0x008,
            IDENTITY_FLAG_CULTURE                = 0x010,
            IDENTITY_FLAG_LANGUAGE               = 0x020,
            IDENTITY_FLAG_PROCESSOR_ARCHITECTURE = 0x040,
            IDENTITY_FLAG_RETARGETABLE           = 0x080,
            IDENTITY_FLAG_PUBLIC_KEY_TOKEN_NULL  = 0x100,
            IDENTITY_FLAG_CUSTOM                 = 0x200,
            IDENTITY_FLAG_CUSTOM_NULL            = 0x400,
            IDENTITY_FLAG_CONTENT_TYPE           = 0x800,
            IDENTITY_FLAG_FULL_NAME              = (IDENTITY_FLAG_SIMPLE_NAME |
                                                    IDENTITY_FLAG_VERSION)
        };

        AssemblyIdentity()
        {
            m_dwIdentityFlags = IDENTITY_FLAG_EMPTY;
            m_kProcessorArchitecture = peNone;
            m_kContentType = AssemblyContentType_Default;

            // Need to pre-populate SBuffers because of bogus asserts
            static const BYTE byteArr[] = { 0 };
            m_publicKeyOrTokenBLOB.SetImmutable(byteArr, sizeof(byteArr));
            m_customBLOB.SetImmutable(byteArr, sizeof(byteArr));
        }
        ~AssemblyIdentity()
        {
            // Nothing to do here
        }

        static BOOL Have(DWORD dwUseIdentityFlags, DWORD dwIdentityFlags)
        {
            return ((dwUseIdentityFlags & dwIdentityFlags) != 0);
        }

        BOOL Have(DWORD dwIdentityFlags)
        {
            return Have(m_dwIdentityFlags, dwIdentityFlags);
        }

        void SetHave(DWORD dwIdentityFlags)
        {
            m_dwIdentityFlags |= dwIdentityFlags;
        }

        void SetClear(DWORD dwIdentityFlags)
        {
            m_dwIdentityFlags &= ~dwIdentityFlags;
        }

        SString             m_simpleName;
        AssemblyVersion     m_version;
        SString             m_cultureOrLanguage;
        SBuffer             m_publicKeyOrTokenBLOB;
        PEKIND              m_kProcessorArchitecture;
        AssemblyContentType m_kContentType;
        SBuffer             m_customBLOB;
        DWORD               m_dwIdentityFlags;
    };

    class AssemblyIdentityUTF8 final : public AssemblyIdentity
    {
    public:
        AssemblyIdentityUTF8()
        {
            m_szSimpleNameUTF8 = NULL;
            m_szCultureOrLanguageUTF8 = NULL;
        }

        void PopulateUTF8Fields()
        {
            m_szSimpleNameUTF8 = m_simpleName.GetUTF8(sSimpleNameBuffer);

            if (Have(IDENTITY_FLAG_CULTURE) && !m_cultureOrLanguage.IsEmpty())
            {
                m_szCultureOrLanguageUTF8 = m_cultureOrLanguage.GetUTF8(sCultureBuffer);
            }
        }

        inline LPCSTR GetSimpleNameUTF8()
        {
            return m_szSimpleNameUTF8;
        }

        inline LPCSTR GetCultureOrLanguageUTF8()
        {
            return m_szCultureOrLanguageUTF8;
        }

        inline const BYTE *GetPublicKeyOrTokenArray()
        {
            const BYTE *pPublicKeyOrToken = m_publicKeyOrTokenBLOB;

            return pPublicKeyOrToken;
        }

    protected:
        StackScratchBuffer sSimpleNameBuffer;
        StackScratchBuffer sCultureBuffer;
        LPCSTR m_szSimpleNameUTF8;
        LPCSTR m_szCultureOrLanguageUTF8;
    };
};

#endif
