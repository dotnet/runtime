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

inline ULONG Assembly::AddRef()
{
    return InterlockedIncrement(&m_cRef);
}

inline ULONG Assembly::Release()
{
    ULONG ulRef = InterlockedDecrement(&m_cRef);

    if (ulRef == 0)
    {
        delete this;
    }

    return ulRef;
}

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

BOOL Assembly::GetIsInTPA()
{
    return ((m_dwAssemblyFlags & FLAG_IS_IN_TPA) != 0);
}

void Assembly::SetIsInTPA(BOOL fIsInTPA)
{
    if (fIsInTPA)
    {
        m_dwAssemblyFlags |= FLAG_IS_IN_TPA;
    }
    else
    {
        m_dwAssemblyFlags &= ~FLAG_IS_IN_TPA;
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

#endif
