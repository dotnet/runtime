// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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

#if !defined(FEATURE_FUSION)
#include "clrprivbindercoreclr.h"
#endif // !defined(FEATURE_FUSION)

#if defined(FEATURE_HOST_ASSEMBLY_RESOLVER) && !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE) && !defined(MDILNIGEN)
#include "clrprivbinderassemblyloadcontext.h"
#endif // defined(FEATURE_HOST_ASSEMBLY_RESOLVER) && !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE) && !defined(MDILNIGEN)

STDAPI BinderAcquirePEImage(LPCTSTR   szAssemblyPath,
                            PEImage **ppPEImage,
                            PEImage **ppNativeImage,
                            BOOL      fExplicitBindToNativeImage);

STDAPI BinderAcquireImport(PEImage                  *pPEImage,
                           IMDInternalImport       **pIMetaDataAssemblyImport,
                           DWORD                    *pdwPAFlags,
                           BOOL                     bNativeImage);

STDAPI BinderHasNativeHeader(PEImage *pPEImage,
                             BOOL    *result);
 
STDAPI BinderGetImagePath(PEImage *pPEImage,
                          SString &imagePath);

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
                IAssemblyName * pIAssemblyName,
                ICLRPrivAssembly ** ppAssembly);

        STDMETHOD(IsShareable)(BOOL * pbIsShareable);

        STDMETHOD(GetAvailableImageTypes)(PDWORD pdwImageTypes);

        STDMETHOD(GetImageResource)(
                DWORD dwImageType,
                DWORD *pdwImageType,
                ICLRPrivResource ** ppIResource);

        STDMETHOD(VerifyBind)(
                IAssemblyName * pIAssemblyName,
                ICLRPrivAssembly *pAssembly,
                ICLRPrivAssemblyInfo *pAssemblyInfo);

        STDMETHOD(GetBinderID)(UINT_PTR *pBinderId);

        STDMETHOD(FindAssemblyBySpec)(
                LPVOID pvAppDomain,
                LPVOID pvAssemblySpec,
                HRESULT * pResult,
                ICLRPrivAssembly ** ppAssembly);

        STDMETHOD(GetBinderFlags)(DWORD *pBinderFlags);

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
                     /* in */ BOOL                     fInspectionOnly,
                     /* in */ BOOL                     fIsInGAC);

        // Enumerates dependent assemblies
        HRESULT GetNextAssemblyNameRef(/* in  */ DWORD          nIndex,
                                       /* out */ AssemblyName **ppAssemblyName);

        inline AssemblyName *GetAssemblyName(BOOL fAddRef = FALSE);
        inline BOOL GetIsInGAC();
        inline BOOL GetIsDynamicBind();
        inline void SetIsDynamicBind(BOOL fIsDynamicBind);
        inline BOOL GetIsByteArray();
        inline void SetIsByteArray(BOOL fIsByteArray);
        inline BOOL GetIsSharable();
        inline void SetIsSharable(BOOL fIsSharable);
        inline SString &GetPath();

        inline PEImage *GetPEImage(BOOL fAddRef = FALSE);
        inline PEImage *GetNativePEImage(BOOL fAddRef = FALSE);
        inline PEImage *GetNativeOrILPEImage(BOOL fAddRef = FALSE);

        HRESULT GetMVID(GUID *pMVID);
        
        static PEKIND GetSystemArchitecture();
        static BOOL IsValidArchitecture(PEKIND kArchitecture);

#ifndef CROSSGEN_COMPILE
    protected:
#endif
        // Asssembly Flags
        enum
        {
            FLAG_NONE = 0x00,
            FLAG_INSPECTION_ONLY = 0x01,
            FLAG_IS_IN_GAC = 0x02,
            FLAG_IS_DYNAMIC_BIND = 0x04,
            FLAG_IS_BYTE_ARRAY = 0x08,
            FLAG_IS_SHARABLE = 0x10
        };

        inline void SetPEImage(PEImage *pPEImage);
        inline void SetNativePEImage(PEImage *pNativePEImage);

        inline void SetAssemblyName(AssemblyName *pAssemblyName,
                                    BOOL          fAddRef = TRUE);
        inline BOOL GetInspectionOnly();
        inline void SetInspectionOnly(BOOL fInspectionOnly);
        inline void SetIsInGAC(BOOL fIsInGAC);

        inline IMDInternalImport *GetMDImport();
        inline void SetMDImport(IMDInternalImport *pMDImport);
        inline mdAssembly *GetAssemblyRefTokens();

        inline DWORD GetNbAssemblyRefTokens();
        inline void SetNbAsssemblyRefTokens(DWORD dwCAssemblyRefTokens);

        LONG                     m_cRef;
        PEImage                 *m_pPEImage;
        PEImage                 *m_pNativePEImage;
        IMDInternalImport       *m_pMDImport;
        mdAssembly              *m_pAssemblyRefTokens;
        DWORD                    m_dwCAssemblyRefTokens;
        AssemblyName            *m_pAssemblyName;
        SString                  m_assemblyPath;
        DWORD                    m_dwAssemblyFlags;
        ICLRPrivBinder          *m_pBinder;

        // Nested class used to implement ICLRPriv binder related interfaces
        class CLRPrivResourceAssembly : 
            public ICLRPrivResource, public ICLRPrivResourceAssembly
        {
public:
            STDMETHOD(QueryInterface)(REFIID riid, void ** ppv);
            STDMETHOD_(ULONG, AddRef)();
            STDMETHOD_(ULONG, Release)();
            STDMETHOD(GetResourceType)(IID *pIID);
            STDMETHOD(GetAssembly)(LPVOID *ppAssembly);
         } m_clrPrivRes;
    
        inline void SetBinder(ICLRPrivBinder *pBinder)
        {
            _ASSERTE(m_pBinder == NULL || m_pBinder == pBinder);
            m_pBinder = pBinder;
        }

        inline ICLRPrivBinder* GetBinder()
        {
            return m_pBinder;
        }
        
#if !defined(FEATURE_FUSION)
        friend class ::CLRPrivBinderCoreCLR;
#endif // !defined(FEATURE_FUSION)

#if defined(FEATURE_HOST_ASSEMBLY_RESOLVER) && !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE) && !defined(MDILNIGEN)
        friend class ::CLRPrivBinderAssemblyLoadContext;
#endif // defined(FEATURE_HOST_ASSEMBLY_RESOLVER) && !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE) && !defined(MDILNIGEN)
    };

    // This is a fast version which goes around the COM interfaces and directly
    // casts the interfaces and does't AddRef
    inline BINDER_SPACE::Assembly * GetAssemblyFromPrivAssemblyFast(ICLRPrivAssembly *pPrivAssembly);

#include "assembly.inl"
};

#endif
