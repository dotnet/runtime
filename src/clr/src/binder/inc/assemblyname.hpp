//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
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

namespace BINDER_SPACE
{
    class AssemblyName : protected AssemblyIdentity
    {
    public:
        typedef enum
        {
            INCLUDE_DEFAULT                     = 0x00,
            INCLUDE_VERSION                     = 0x01,
            INCLUDE_ARCHITECTURE                = 0x02,
            INCLUDE_RETARGETABLE                = 0x04, 
            INCLUDE_CONTENT_TYPE                = 0x08,
            EXCLUDE_PUBLIC_KEY_TOKEN_IF_MISSING = 0x10,
            EXCLUDE_CULTURE                     = 0x20
        } INCLUDE_FLAGS;

        AssemblyName();
        ~AssemblyName();

        HRESULT Init(/* in */ IMDInternalImport       *pIMetaDataAssemblyImport,
                     /* in */ PEKIND                   PeKind,
                     /* in */ mdAssemblyRef            mda = 0,
                     /* in */ BOOL                     fIsDefinition = TRUE);
        HRESULT Init(/* in */ SString &assemblyDisplayName);
        HRESULT Init(/* in */ IAssemblyName *pIAssemblyName);
        HRESULT CreateFusionName(/* out */ IAssemblyName **ppIAssemblyName);

        ULONG AddRef();
        ULONG Release();

        // Getters/Setters
        inline SString &GetSimpleName();
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

        inline void SetHave(DWORD dwIdentityFlags);

        inline BOOL HaveAssemblyVersion();
        inline BOOL HaveNeutralCulture();

        SString &GetDeNormalizedCulture();
        BOOL IsStronglyNamed();

        BOOL IsMscorlib();

        // Translate textual identity into appropriate PEKIND
        HRESULT SetArchitecture(SString &architecture);

        ULONG Hash(/* in */ DWORD dwIncludeFlags);
        BOOL Equals(/* in */ AssemblyName *pAssemblyName,
                    /* in */ DWORD         dwIncludeFlags);

        // Compare assembly ref with assembly def ignoring assembly version
        BOOL RefEqualsDef(/* in */ AssemblyName *pAssemblyNameDef,
                          /* in */ BOOL          fInspectionOnly);

        HRESULT Clone(/* out */ AssemblyName **ppAssemblyName);

        void GetDisplayName(/* out */ PathString &displayName,
                            /* in */  DWORD       dwIncludeFlags);

        static SString &ArchitectureToString(PEKIND kArchitecture);        
    protected:
        enum
        {
            NAME_FLAG_NONE                           = 0x00,
            NAME_FLAG_RETARGETABLE                   = 0x01,
            NAME_FLAG_DEFINITION                     = 0x02,
        };

        SString &GetNormalizedCulture();

        LONG           m_cRef;
        DWORD          m_dwNameFlags;
    };

#include "assemblyname.inl"
};

#endif
