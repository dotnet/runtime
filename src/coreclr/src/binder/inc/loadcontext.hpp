// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ============================================================
//
// LoadContext.hpp
//


//
// Defines the LoadContext template class
//
// ============================================================

#ifndef __BINDER__LOAD_CONTEXT_HPP__
#define __BINDER__LOAD_CONTEXT_HPP__

#include "assemblyhashtraits.hpp"
#include "contextentry.hpp"
#include "bindresult.hpp"

namespace BINDER_SPACE
{
    template <DWORD dwIncludeFlags>
    class LoadContext : protected SHash<AssemblyHashTraits<ContextEntry *, dwIncludeFlags> >
    {
    private:
        typedef SHash<AssemblyHashTraits<ContextEntry *, dwIncludeFlags> > Hash;
    public:
        LoadContext();
        ~LoadContext();

        ULONG AddRef();
        ULONG Release();
        ContextEntry *Lookup(/* in */ AssemblyName *pAssemblyName);
        HRESULT Register(BindResult *pBindResult);

    protected:
        LONG m_cRef;
    };

#include "loadcontext.inl"

    class ExecutionContext : public LoadContext<AssemblyName::INCLUDE_DEFAULT> {};
};

#endif
