// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ============================================================
//
// Assembly.hpp
//


//
// Defines the Assembly class
//
// ============================================================

#ifndef __BINDER__ASSEMBLY_HPP__
#define __BINDER__ASSEMBLY_HPP__

#include "bindertypes.hpp"
#include "assemblyname.hpp"

#include "corpriv.h"

#include "defaultassemblybinder.h"

#if !defined(DACCESS_COMPILE)
#include "customassemblybinder.h"
#endif // !defined(DACCESS_COMPILE)

#include "bundle.h"

STDAPI BinderAcquirePEImage(LPCTSTR            szAssemblyPath,
                            PEImage          **ppPEImage,
                            PEImage          **ppNativeImage,
                            BOOL               fExplicitBindToNativeImage,
                            BundleFileLocation bundleFileLocation);

STDAPI BinderAcquireImport(PEImage            *pPEImage,
                           IMDInternalImport **pIMetaDataAssemblyImport,
                           DWORD              *pdwPAFlags,
                           BOOL                bNativeImage);

STDAPI BinderHasNativeHeader(PEImage *pPEImage,
                             BOOL    *result);

STDAPI BinderReleasePEImage(PEImage *pPEImage);

STDAPI BinderAddRefPEImage(PEImage *pPEImage);

namespace BINDER_SPACE
{

    // An assembly represents a particular set of bits.  However we extend this to
    // also include whether those bits have precompiled information (NGEN).   Thus
    // and assembly knows whether it has an NGEN image or not.
    //
    // This allows us to preferentially use the NGEN image if it is available.
    class Assembly
    {
    public:
        ULONG AddRef();
        ULONG Release();

        LPCWSTR GetSimpleName();
        AssemblyLoaderAllocator* GetLoaderAllocator();

        // --------------------------------------------------------------------
        // Assembly methods
        // --------------------------------------------------------------------
        Assembly();
        virtual ~Assembly();

        HRESULT Init(/* in */ IMDInternalImport       *pIMetaDataAssemblyImport,
                     /* in */ PEKIND                   PeKind,
                     /* in */ PEImage                 *pPEImage,
                     /* in */ PEImage                 *pPENativeImage,
                     /* in */ SString                 &assemblyPath,
                     /* in */ BOOL                     fIsInTPA);

        inline AssemblyName *GetAssemblyName(BOOL fAddRef = FALSE);
        inline BOOL GetIsInTPA();

        inline SString &GetPath();

        inline PEImage *GetPEImage(BOOL fAddRef = FALSE);
        inline PEImage *GetNativePEImage(BOOL fAddRef = FALSE);
        inline PEImage *GetNativeOrILPEImage(BOOL fAddRef = FALSE);

        HRESULT GetMVID(GUID *pMVID);

        static PEKIND GetSystemArchitecture();
        static BOOL IsValidArchitecture(PEKIND kArchitecture);

        inline AssemblyBinder* GetBinder()
        {
            return m_pBinder;
        }

    private:
        // Assembly Flags
        enum
        {
            FLAG_NONE = 0x00,
            FLAG_IS_IN_TPA = 0x02,
            //FLAG_IS_DYNAMIC_BIND = 0x04,
            FLAG_IS_BYTE_ARRAY = 0x08,
        };

        inline void SetPEImage(PEImage *pPEImage);
        inline void SetNativePEImage(PEImage *pNativePEImage);

        inline void SetAssemblyName(AssemblyName *pAssemblyName,
                                    BOOL          fAddRef = TRUE);
        inline void SetIsInTPA(BOOL fIsInTPA);

        inline IMDInternalImport *GetMDImport();
        inline void SetMDImport(IMDInternalImport *pMDImport);

        LONG                     m_cRef;
        PEImage                 *m_pPEImage;
        PEImage                 *m_pNativePEImage;
        IMDInternalImport       *m_pMDImport;
        AssemblyName            *m_pAssemblyName;
        SString                  m_assemblyPath;
        DWORD                    m_dwAssemblyFlags;
        AssemblyBinder          *m_pBinder;

        inline void SetBinder(AssemblyBinder *pBinder)
        {
            _ASSERTE(m_pBinder == NULL || m_pBinder == pBinder);
            m_pBinder = pBinder;
        }

        friend class ::DefaultAssemblyBinder;

#if !defined(DACCESS_COMPILE)
        friend class ::CustomAssemblyBinder;
#endif // !defined(DACCESS_COMPILE)
    };

#include "assembly.inl"
};

#endif
