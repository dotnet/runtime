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

class DomainAssembly;

namespace BINDER_SPACE
{
    // BINDER_SPACE::Assembly represents a result of binding to an actual assembly (PEImage)
    // It is basically a tuple of 1) physical assembly and 2) binder which created/owns this binding
    // We also store whether it was bound using TPA list
    class Assembly
    {
    public:
        ULONG AddRef();
        ULONG Release();

        Assembly();
        virtual ~Assembly();

        HRESULT Init(PEImage *pPEImage, BOOL fIsInTPA);

        LPCWSTR GetSimpleName();
        AssemblyName *GetAssemblyName(BOOL fAddRef = FALSE);
        PEImage* GetPEImage();
        BOOL GetIsInTPA();

        inline AssemblyBinder* GetBinder()
        {
            return m_pBinder;
        }

        DomainAssembly* GetDomainAssembly()
        {
            return m_domainAssembly;
        }

        void SetDomainAssembly(DomainAssembly* value)
        {
            _ASSERTE(value == NULL || m_domainAssembly == NULL);
            m_domainAssembly = value;
        }

    private:
        LONG                     m_cRef;
        PEImage                 *m_pPEImage;
        AssemblyName            *m_pAssemblyName;
        AssemblyBinder          *m_pBinder;
        bool                     m_isInTPA;
        DomainAssembly          *m_domainAssembly;

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
