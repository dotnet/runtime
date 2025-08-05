// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// --------------------------------------------------------------------------------
// DomainAssembly.cpp
//

#include "common.h"
#include "invokeutil.h"
#include "eeconfig.h"
#include "dynamicmethod.h"
#include "field.h"
#include "dbginterface.h"
#include "eventtrace.h"

#include "dllimportcallback.h"
#include "peimagelayout.inl"

#ifndef DACCESS_COMPILE
DomainAssembly::DomainAssembly(PEAssembly* pPEAssembly, LoaderAllocator* pLoaderAllocator, AllocMemTracker* memTracker)
    : m_pAssembly(NULL)
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        THROWS;             // ValidateForExecution
        GC_TRIGGERS;        // ValidateForExecution
        MODE_ANY;
    }
    CONTRACTL_END;

    pPEAssembly->AddRef();
    pPEAssembly->ValidateForExecution();

    // Create the Assembly
    NewHolder<Assembly> assembly = Assembly::Create(pPEAssembly, memTracker, pLoaderAllocator);

    m_pAssembly = assembly.Extract();

    m_pAssembly->SetDomainAssembly(this);

#ifndef PEIMAGE_FLAT_LAYOUT_ONLY
    // Creating the Assembly should have ensured the PEAssembly is loaded
    _ASSERT(GetPEAssembly()->IsLoaded());
#endif
}

DomainAssembly::~DomainAssembly()
{
    CONTRACTL
    {
        DESTRUCTOR_CHECK;
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_pAssembly != NULL)
    {
        delete m_pAssembly;
    }
}
#endif //!DACCESS_COMPILE


PEAssembly* DomainAssembly::GetPEAssembly()
{
    return PTR_PEAssembly(m_pAssembly->GetPEAssembly());
}
