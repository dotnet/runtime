// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ============================================================
//
// AssemblyName.hpp
//


//
// Defines the AssemblyName class
//
// ============================================================

#ifndef __BINDER__ASSEMBLY_NAME_HPP__
#define __BINDER__ASSEMBLY_NAME_HPP__

#include "bindertypes.hpp"
#include "assemblyidentity.hpp"

class PEImage;

namespace BINDER_SPACE
{
    class AssemblyName final : public AssemblyIdentity
    {
    public:
        typedef enum
        {
            INCLUDE_DEFAULT             = 0x00,
            INCLUDE_VERSION             = 0x01,
            INCLUDE_ARCHITECTURE        = 0x02,
            INCLUDE_RETARGETABLE        = 0x04,
            INCLUDE_CONTENT_TYPE        = 0x08,
            INCLUDE_PUBLIC_KEY_TOKEN    = 0x10,
            EXCLUDE_CULTURE             = 0x20,
            INCLUDE_ALL                 = INCLUDE_VERSION
                                            | INCLUDE_ARCHITECTURE
                                            | INCLUDE_RETARGETABLE
                                            | INCLUDE_CONTENT_TYPE
                                            | INCLUDE_PUBLIC_KEY_TOKEN,
        } INCLUDE_FLAGS;

        AssemblyName();

        HRESULT Init(PEImage* pPEImage);
        HRESULT Init(const AssemblyNameData &data);

        ULONG AddRef();
        ULONG Release();

        // Getters/Setters
        inline const SString &GetSimpleName();
        inline void SetSimpleName(SString &simpleName);
        inline AssemblyVersion *GetVersion();
        inline void SetVersion(/* in */ AssemblyVersion *pAssemblyVersion);
        inline SString &GetCulture();
        inline void SetCulture(SString &culture);
        inline SBuffer &GetPublicKeyTokenBLOB();
        inline PEKIND GetArchitecture();
        inline void SetArchitecture(PEKIND kArchitecture);
        inline AssemblyContentType GetContentType();
        inline void SetContentType(AssemblyContentType kContentType);
        inline BOOL GetIsRetargetable();
        inline void SetIsRetargetable(BOOL fIsRetargetable);
        inline BOOL GetIsDefinition();
        inline void SetIsDefinition(BOOL fIsDefinition);

        BOOL IsCoreLib();
        bool IsNeutralCulture();

        ULONG Hash(/* in */ DWORD dwIncludeFlags);
        BOOL Equals(/* in */ AssemblyName *pAssemblyName,
                    /* in */ DWORD         dwIncludeFlags);

        void GetDisplayName(/* out */ PathString &displayName,
                            /* in */  DWORD       dwIncludeFlags);

    private:
        SString &GetNormalizedCulture();

        LONG           m_cRef;
        bool           m_isDefinition;
    };

#include "assemblyname.inl"
};

#endif
