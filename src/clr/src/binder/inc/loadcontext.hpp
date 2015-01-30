//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
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

    class InspectionContext :
        public LoadContext<AssemblyName::INCLUDE_VERSION | AssemblyName::INCLUDE_ARCHITECTURE> {};
    class ExecutionContext : public LoadContext<AssemblyName::INCLUDE_DEFAULT> {};
};

#endif
