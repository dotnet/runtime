// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ============================================================
//
// LoadContext.inl
//


//
// Implements the inlined methods of LoadContext template class
//
// ============================================================

#ifndef __BINDER__LOAD_CONTEXT_INL__
#define __BINDER__LOAD_CONTEXT_INL__

template <DWORD dwIncludeFlags>
LoadContext<dwIncludeFlags>::LoadContext() :
    SHash<AssemblyHashTraits<ContextEntry *, dwIncludeFlags> >::SHash()
{
    m_cRef = 1;
}

template <DWORD dwIncludeFlags>
LoadContext<dwIncludeFlags>::~LoadContext()
{
    // Delete context entries and contents array
    for (typename Hash::Iterator i = Hash::Begin(), end = Hash::End(); i != end; i++)
    {
        const ContextEntry *pContextEntry = *i;
        delete pContextEntry;
    }
    this->RemoveAll();
}

template <DWORD dwIncludeFlags>
ULONG LoadContext<dwIncludeFlags>::AddRef()
{
    return InterlockedIncrement(&m_cRef);
}

template <DWORD dwIncludeFlags>
ULONG LoadContext<dwIncludeFlags>::Release()
{
    ULONG ulRef = InterlockedDecrement(&m_cRef);

    if (ulRef == 0) 
    {
        delete this;
    }

    return ulRef;
}

template <DWORD dwIncludeFlags>
ContextEntry *LoadContext<dwIncludeFlags>::Lookup(AssemblyName *pAssemblyName)
{
    ContextEntry *pContextEntry =
        SHash<AssemblyHashTraits<ContextEntry *, dwIncludeFlags> >::Lookup(pAssemblyName);

    return pContextEntry;
}

template <DWORD dwIncludeFlags>
HRESULT LoadContext<dwIncludeFlags>::Register(BindResult *pBindResult)
{
    HRESULT hr = S_OK;
    ContextEntry *pContextEntry = NULL;

    SAFE_NEW(pContextEntry, ContextEntry);

    pContextEntry->SetIsDynamicBind(pBindResult->GetIsDynamicBind());
    pContextEntry->SetIsInGAC(pBindResult->GetIsInGAC());
    pContextEntry->SetIsSharable(pBindResult->GetIsSharable());
    pContextEntry->SetAssemblyName(pBindResult->GetAssemblyName(), TRUE /* fAddRef */);
    pContextEntry->SetAssembly(pBindResult->GetAssembly());

    if (pBindResult->GetIsFirstRequest())
    {
        pContextEntry->SetIsFirstRequest(TRUE);
    }

    SHash<AssemblyHashTraits<ContextEntry *, dwIncludeFlags> >::Add(pContextEntry);

 Exit:
    return hr;
}

#endif
