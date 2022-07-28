// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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

    pContextEntry->SetIsInTPA(pBindResult->GetIsInTPA());
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
