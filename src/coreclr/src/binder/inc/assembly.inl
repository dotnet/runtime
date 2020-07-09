// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ============================================================
//
// Assembly.inl
//


//
// Implements the inline methods of Assembly
//
// ============================================================

#ifndef __BINDER__ASSEMBLY_INL__
#define __BINDER__ASSEMBLY_INL__

PEImage *Assembly::GetPEImage(BOOL fAddRef /* = FALSE */)
{
    PEImage *pPEImage = m_pPEImage;

    if (fAddRef)
    {
        BinderAddRefPEImage(pPEImage);
    }

    return pPEImage;
}

PEImage *Assembly::GetNativePEImage(BOOL fAddRef /* = FALSE */)
{
    PEImage *pNativePEImage = m_pNativePEImage;

    if (fAddRef)
    {
        BinderAddRefPEImage(pNativePEImage);
    }

    return pNativePEImage;
}

PEImage *Assembly::GetNativeOrILPEImage(BOOL fAddRef /* = FALSE */)
{
    PEImage* pPEImage = GetNativePEImage(fAddRef);
    if (pPEImage == NULL)
        pPEImage = GetPEImage(fAddRef);
    return pPEImage;
}

void Assembly::SetPEImage(PEImage *pPEImage)
{
    BinderAddRefPEImage(pPEImage);
    m_pPEImage = pPEImage;
}

void Assembly::SetNativePEImage(PEImage *pNativePEImage)
{
    BinderAddRefPEImage(pNativePEImage);
    m_pNativePEImage = pNativePEImage;
}

AssemblyName *Assembly::GetAssemblyName(BOOL fAddRef /* = FALSE */)
{
    AssemblyName *pAssemblyName = m_pAssemblyName;

    if (fAddRef && (pAssemblyName != NULL))
    {
        pAssemblyName->AddRef();
    }
    return pAssemblyName;
}

void Assembly::SetAssemblyName(AssemblyName *pAssemblyName,
                               BOOL          fAddRef /* = TRUE */)
{
    SAFE_RELEASE(m_pAssemblyName);

    m_pAssemblyName = pAssemblyName;

    if (fAddRef && (pAssemblyName != NULL))
    {
        pAssemblyName->AddRef();
    }
}

BOOL Assembly::GetIsInGAC()
{
    return ((m_dwAssemblyFlags & FLAG_IS_IN_GAC) != 0);
}

void Assembly::SetIsInGAC(BOOL fIsInGAC)
{
    if (fIsInGAC)
    {
        m_dwAssemblyFlags |= FLAG_IS_IN_GAC;
    }
    else
    {
        m_dwAssemblyFlags &= ~FLAG_IS_IN_GAC;
    }
}

SString &Assembly::GetPath()
{
    return m_assemblyPath;
}

IMDInternalImport *Assembly::GetMDImport()
{
    return m_pMDImport;
}

void Assembly::SetMDImport(IMDInternalImport *pMDImport)
{
    SAFE_RELEASE(m_pMDImport);

    m_pMDImport = pMDImport;
    m_pMDImport->AddRef();
}

BINDER_SPACE::Assembly* GetAssemblyFromPrivAssemblyFast(ICLRPrivAssembly *pPrivAssembly)
{
#ifdef _DEBUG
    if(pPrivAssembly != nullptr)
    {
        // Ensure the pPrivAssembly we are about to cast is indeed a valid Assembly
        DWORD dwImageType = 0;
        pPrivAssembly->GetAvailableImageTypes(&dwImageType);
        _ASSERTE((dwImageType & ASSEMBLY_IMAGE_TYPE_ASSEMBLY) == ASSEMBLY_IMAGE_TYPE_ASSEMBLY);
    }
#endif
    return (BINDER_SPACE::Assembly *)pPrivAssembly;
}

#endif
