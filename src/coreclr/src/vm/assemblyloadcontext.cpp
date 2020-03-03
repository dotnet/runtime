// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"

AssemblyLoadContext::AssemblyLoadContext()
{
}

HRESULT AssemblyLoadContext::GetBinderID(
    UINT_PTR* pBinderId)
{
    *pBinderId = reinterpret_cast<UINT_PTR>(this);
    return S_OK;
}
