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
#include "clrprivbinding.h"

#include "clrprivbindercoreclr.h"

#if !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
#include "clrprivbinderassemblyloadcontext.h"
#endif // !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)

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
        : public ICLRPrivAssembly
    {
    public:
        // --------------------------------------------------------------------
        // IUnknown methods
        // --------------------------------------------------------------------
        STDMETHOD(QueryInterface)(REFIID riid,
                                  void ** ppv);
        STDMETHOD_(ULONG, AddRef)();
        STDMETHOD_(ULONG, Release)();

        // --------------------------------------------------------------------
        // ICLRPrivAssembly methods
        // --------------------------------------------------------------------
        LPCWSTR GetSimpleName();

        STDMETHOD(BindAssemblyByName)(
            /* [in] */ AssemblyNameData *pAssemblyNameData,
            /* [retval][out] */ ICLRPrivAssembly **ppAssembly);

        STDMETHOD(GetAvailableImageTypes)(PDWORD pdwImageTypes);

        STDMETHOD(GetBinderID)(UINT_PTR *pBinderId);

        STDMETHOD(GetLoaderAllocator)(LPVOID* pLoaderAllocator);

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
                     /* in */ BOOL                     fIsInGAC);

        inline AssemblyName *GetAssemblyName(BOOL fAddRef = FALSE);
        inline BOOL GetIsInGAC();

        inline SString &GetPath();

        inline PEImage *GetPEImage(BOOL fAddRef = FALSE);
        inline PEImage *GetNativePEImage(BOOL fAddRef = FALSE);
        inline PEImage *GetNativeOrILPEImage(BOOL fAddRef = FALSE);

        HRESULT GetMVID(GUID *pMVID);

        static PEKIND GetSystemArchitecture();
        static BOOL IsValidArchitecture(PEKIND kArchitecture);

        inline ICLRPrivBinder* GetBinder()
        {
            return m_pBinder;
        }

#ifndef CROSSGEN_COMPILE
    protected:
#endif
        // Assembly Flags
        enum
        {
            FLAG_NONE = 0x00,
            FLAG_IS_IN_GAC = 0x02,
            //FLAG_IS_DYNAMIC_BIND = 0x04,
            FLAG_IS_BYTE_ARRAY = 0x08,
        };

        inline void SetPEImage(PEImage *pPEImage);
        inline void SetNativePEImage(PEImage *pNativePEImage);

        inline void SetAssemblyName(AssemblyName *pAssemblyName,
                                    BOOL          fAddRef = TRUE);
        inline void SetIsInGAC(BOOL fIsInGAC);

        inline IMDInternalImport *GetMDImport();
        inline void SetMDImport(IMDInternalImport *pMDImport);

        LONG                     m_cRef;
        PEImage                 *m_pPEImage;
        PEImage                 *m_pNativePEImage;
        IMDInternalImport       *m_pMDImport;
        AssemblyName            *m_pAssemblyName;
        SString                  m_assemblyPath;
        DWORD                    m_dwAssemblyFlags;
        ICLRPrivBinder          *m_pBinder;

        inline void SetBinder(ICLRPrivBinder *pBinder)
        {
            _ASSERTE(m_pBinder == NULL || m_pBinder == pBinder);
            m_pBinder = pBinder;
        }

        friend class ::CLRPrivBinderCoreCLR;

#if !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
        friend class ::CLRPrivBinderAssemblyLoadContext;
#endif // !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
    };

    // This is a fast version which goes around the COM interfaces and directly
    // casts the interfaces and does't AddRef
    inline BINDER_SPACE::Assembly * GetAssemblyFromPrivAssemblyFast(ICLRPrivAssembly *pPrivAssembly);

#include "assembly.inl"
};

#endif
