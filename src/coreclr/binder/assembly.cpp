// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ============================================================
//
// Assembly.cpp
//


//
// Implements the Assembly class
//
// ============================================================
#include "common.h"
#include "assembly.hpp"
#include "utils.hpp"
#include "assemblybindercommon.hpp"

namespace BINDER_SPACE
{
    namespace
    {
        BOOL IsPlatformArchitecture(PEKIND kArchitecture)
        {
            return ((kArchitecture != peMSIL) && (kArchitecture != peNone));
        }
    };

    Assembly::Assembly()
    {
        m_cRef = 1;
        m_pPEImage = NULL;
        m_pAssemblyName = NULL;
        m_isInTPA = false;
        m_pBinder = NULL;
    }

    Assembly::~Assembly()
    {
        SAFE_RELEASE(m_pPEImage);
        SAFE_RELEASE(m_pAssemblyName);
    }

    HRESULT Assembly::Init(PEImage *pPEImage, BOOL fIsInTPA)
    {
        HRESULT hr = S_OK;

        ReleaseHolder<AssemblyName> pAssemblyName;
        SAFE_NEW(pAssemblyName, AssemblyName);

        // Get assembly name def from meta data import and store it for later refs access
        IF_FAIL_GO(pAssemblyName->Init(pPEImage));

        // Safe architecture for validation
        PEKIND kAssemblyArchitecture;
        kAssemblyArchitecture = pAssemblyName->GetArchitecture();
        SetIsInTPA(fIsInTPA);
        SetPEImage(pPEImage);
        pAssemblyName->SetIsDefinition(TRUE);

        // Now take ownership of assembly names
        SetAssemblyName(pAssemblyName.Extract(), FALSE /* fAddRef */);

        // Finally validate architecture
        if (!AssemblyBinderCommon::IsValidArchitecture(kAssemblyArchitecture))
        {
            // Assembly image can't be executed on this platform
            IF_FAIL_GO(HRESULT_FROM_WIN32(ERROR_BAD_FORMAT));
        }

    Exit:
        return hr;
    }

    PEImage* Assembly::GetPEImage()
    {
        return m_pPEImage;
    }

    void Assembly::SetPEImage(PEImage* pPEImage)
    {
        pPEImage->AddRef();
        m_pPEImage = pPEImage;
    }

    LPCWSTR Assembly::GetSimpleName()
    {
        AssemblyName *pAsmName = GetAssemblyName();
        return (pAsmName == nullptr ? nullptr : (LPCWSTR)pAsmName->GetSimpleName());
    }

    AssemblyLoaderAllocator* Assembly::GetLoaderAllocator()
    {
        return m_pBinder ? m_pBinder->GetLoaderAllocator() : NULL;
    }
}

