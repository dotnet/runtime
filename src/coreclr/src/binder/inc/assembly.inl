// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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

BOOL Assembly::GetInspectionOnly()
{
    return ((m_dwAssemblyFlags & FLAG_INSPECTION_ONLY) != 0);
}

void Assembly::SetInspectionOnly(BOOL fInspectionOnly)
{
    if (fInspectionOnly)
    {
        m_dwAssemblyFlags |= FLAG_INSPECTION_ONLY;
    }
    else
    {
        m_dwAssemblyFlags &= ~FLAG_INSPECTION_ONLY;
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

BOOL Assembly::GetIsDynamicBind()
{
    return ((m_dwAssemblyFlags & FLAG_IS_DYNAMIC_BIND) != 0);
}

void Assembly::SetIsDynamicBind(BOOL fIsDynamicBind)
{
    if (fIsDynamicBind)
    {
        m_dwAssemblyFlags |= FLAG_IS_DYNAMIC_BIND;
    }
    else
    {
        m_dwAssemblyFlags &= ~FLAG_IS_DYNAMIC_BIND;
    }
}

BOOL Assembly::GetIsByteArray()
{
    return ((m_dwAssemblyFlags & FLAG_IS_BYTE_ARRAY) != 0);
}

void Assembly::SetIsByteArray(BOOL fIsByteArray)
{
    if (fIsByteArray)
    {
        m_dwAssemblyFlags |= FLAG_IS_BYTE_ARRAY;
    }
    else
    {
        m_dwAssemblyFlags &= ~FLAG_IS_BYTE_ARRAY;
    }
}

BOOL Assembly::GetIsSharable()
{
    return ((m_dwAssemblyFlags & FLAG_IS_SHARABLE) != 0);
}

void Assembly::SetIsSharable(BOOL fIsSharable)
{
    if (fIsSharable)
    {
        m_dwAssemblyFlags |= FLAG_IS_SHARABLE;
    }
    else
    {
        m_dwAssemblyFlags &= ~FLAG_IS_SHARABLE;
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

mdAssembly *Assembly::GetAssemblyRefTokens()
{
    return m_pAssemblyRefTokens;
}

DWORD Assembly::GetNbAssemblyRefTokens()
{
    return m_dwCAssemblyRefTokens;
}

void Assembly::SetNbAsssemblyRefTokens(DWORD dwCAssemblyRefTokens)
{
    m_dwCAssemblyRefTokens = dwCAssemblyRefTokens;
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
