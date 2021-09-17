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
                            BundleFileLocation bundleFileLocation);

STDAPI BinderAcquireImport(PEImage            *pPEImage,
                           IMDInternalImport **pIMetaDataAssemblyImport,
                           DWORD              *pdwPAFlags);

STDAPI BinderReleasePEImage(PEImage *pPEImage);

STDAPI BinderAddRefPEImage(PEImage *pPEImage);

namespace BINDER_SPACE
{
    // BINDER_SPACE::Assembly represents a result of binding to an actual assembly (PE image)
    // It is basically a tuple of 1) physical assembly and 2) binder which created/owns this binding
    // We also store whether it was bound using TPA list
    //
    // UNDONE: perhaps rename to "BoundAssembly"?
    //         check the ownership, if it is owned by the binder, do we need ref counting?
    //         elaborate why IsInTPA needed
    //
    class Assembly
    {
    public:
        ULONG AddRef();
        ULONG Release();

        Assembly();
        virtual ~Assembly();

        HRESULT Init(PEImage *pPEImage, BOOL fIsInTPA);

        LPCWSTR GetSimpleName();
        inline AssemblyName *GetAssemblyName(BOOL fAddRef = FALSE);
        inline PEImage* GetPEImage(BOOL fAddRef = FALSE);
        inline BOOL GetIsInTPA();

        static PEKIND GetSystemArchitecture();
        static BOOL IsValidArchitecture(PEKIND kArchitecture);

        AssemblyLoaderAllocator* GetLoaderAllocator();
        inline AssemblyBinder* GetBinder()
        {
            return m_pBinder;
        }

    private:
        inline void SetPEImage(PEImage *pPEImage);
        inline void SetAssemblyName(AssemblyName *pAssemblyName, BOOL fAddRef = TRUE);
        inline void SetIsInTPA(BOOL fIsInTPA);

        LONG                     m_cRef;
        PEImage                 *m_pPEImage;
        AssemblyName            *m_pAssemblyName;
        AssemblyBinder          *m_pBinder;
        bool                     m_isInTPA;

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
