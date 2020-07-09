// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include <stdio.h>
#include <xplatform.h>
#include <platformdefines.h>

namespace
{
    void VoidVoidImpl()
    {
        // NOP
    }
}

extern "C" DLL_EXPORT void* GetVoidVoidFcnPtr()
{
    return (void*)&VoidVoidImpl;
}

extern "C" DLL_EXPORT bool CheckFcnPtr(bool(STDMETHODCALLTYPE *fcnptr)(long long))
{
    if (fcnptr == nullptr)
    {
        printf("CheckFcnPtr: Unmanaged received a null function pointer");
        return false;
    }
    else
    {
        return fcnptr(999999999999);
    }
}
