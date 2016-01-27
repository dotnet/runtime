// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ngenoptout.cpp
//

//
// 
// Contains functionality to reject native images at runtime


#include "common.h"
#ifndef FEATURE_CORECLR
#include "ngenoptout.h"
#include "assemblynamelist.h"

AssemblyNameList g_NgenOptoutList;

BOOL IsNativeImageOptedOut(IAssemblyName* pName)
{
    WRAPPER_NO_CONTRACT
    return g_NgenOptoutList.Lookup(pName) != NULL;
}

void AddNativeImageOptOut(IAssemblyName* pName)
{
    WRAPPER_NO_CONTRACT
    pName->AddRef();
    g_NgenOptoutList.Add(pName);
}
// HRESULT
HRESULT RuntimeIsNativeImageOptedOut(IAssemblyName* pName)
{
    WRAPPER_NO_CONTRACT
    return IsNativeImageOptedOut(pName) ? S_OK :S_FALSE;
}
#endif // FEATURE_CORECLR
