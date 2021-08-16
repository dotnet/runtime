// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "nativeimage.h"

#ifndef DACCESS_COMPILE

HRESULT AssemblyBinder::BindAssemblyByName(AssemblyNameData* pAssemblyNameData,
    BINDER_SPACE::Assembly** ppAssembly)
{
    HRESULT hr = S_OK;
    VALIDATE_ARG_RET(pAssemblyNameData != nullptr && ppAssembly != nullptr);

    *ppAssembly = nullptr;

    ReleaseHolder<BINDER_SPACE::AssemblyName> pAssemblyName;
    SAFE_NEW(pAssemblyName, BINDER_SPACE::AssemblyName);
    IF_FAIL_GO(pAssemblyName->Init(*pAssemblyNameData));

    hr = BindUsingAssemblyName(pAssemblyName, ppAssembly);

Exit:
    return hr;
}

#endif  //DACCESS_COMPILE
