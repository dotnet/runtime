// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#include <stdio.h>
#include <xplatform.h>

extern "C" DLL_EXPORT bool __cdecl CheckFcnPtr(bool(STDMETHODCALLTYPE *fcnptr)(__int64))
{
    if (fcnptr == 0)
    {
        printf("CheckFcnPtr: Unmanaged received a null function pointer");
        return false;
    }
    else
    {
        return fcnptr(999999999999);
    }
}

int Return100()
{
    return 100;
}
