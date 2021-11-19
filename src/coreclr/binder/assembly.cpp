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
    Assembly::Assembly()
    {
        m_cRef = 1;
        m_pPEImage = NULL;
        m_pAssemblyName = NULL;
        m_isInTPA = false;
        m_pBinder = NULL;
        m_domainAssembly = NULL;
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
        pAssemblyName->SetIsDefinition(TRUE);

        // validate architecture
        if (!AssemblyBinderCommon::IsValidArchitecture(pAssemblyName->GetArchitecture()))
        {
            // Assembly image can't be executed on this platform
            IF_FAIL_GO(HRESULT_FROM_WIN32(ERROR_BAD_FORMAT));
        }

        m_isInTPA = fIsInTPA;

        pPEImage->AddRef();
        m_pPEImage = pPEImage;

        // Now take ownership of assembly name
        m_pAssemblyName = pAssemblyName.Extract();

    Exit:
        return hr;
    }

    PEImage* Assembly::GetPEImage()
    {
        return m_pPEImage;
    }

    LPCWSTR Assembly::GetSimpleName()
    {
        AssemblyName *pAsmName = GetAssemblyName();
        return (pAsmName == nullptr ? nullptr : (LPCWSTR)pAsmName->GetSimpleName());
    }
}

