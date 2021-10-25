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

inline AssemblyName *Assembly::GetAssemblyName(BOOL fAddRef /* = FALSE */)
{
    AssemblyName *pAssemblyName = m_pAssemblyName;

    if (fAddRef && (pAssemblyName != NULL))
    {
        pAssemblyName->AddRef();
    }
    return pAssemblyName;
}

inline BOOL Assembly::GetIsInTPA()
{
    return m_isInTPA;
}

#endif
