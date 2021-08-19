// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "assemblybinder.h"
#include "../binder/inc/assemblyname.hpp"

#ifndef DACCESS_COMPILE

HRESULT AssemblyBinder::BindAssemblyByName(AssemblyNameData* pAssemblyNameData,
    BINDER_SPACE::Assembly** ppAssembly)
{
    _ASSERTE(pAssemblyNameData != nullptr && ppAssembly != nullptr);

    HRESULT hr = S_OK;
    *ppAssembly = nullptr;

    ReleaseHolder<BINDER_SPACE::AssemblyName> pAssemblyName;
    SAFE_NEW(pAssemblyName, BINDER_SPACE::AssemblyName);
    IF_FAIL_GO(pAssemblyName->Init(*pAssemblyNameData));

    hr = BindUsingAssemblyName(pAssemblyName, ppAssembly);

Exit:
    return hr;
}

#endif  //DACCESS_COMPILE
