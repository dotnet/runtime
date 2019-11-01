// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <xplatform.h>
#include <algorithm>

void Reverse(LPCWSTR str, LPWSTR *res)
{
    LPCWSTR tmp = str;
    size_t len = 0;
    while (*tmp++)
        ++len;

    size_t strDataLen = (len + 1) * sizeof(str[0]);
    auto resLocal = (LPWSTR)CoreClrAlloc(strDataLen);
    if (resLocal == nullptr)
    {
        *res = nullptr;
        return;
    }

    memcpy(resLocal, str, strDataLen);
    
    std::reverse(resLocal, resLocal + len);
    *res = resLocal;
}

extern "C" void DLL_EXPORT ReverseString(LPCWSTR str, LCID lcid, LPWSTR* reversed)
{
    Reverse(str, reversed);
}

extern "C" BOOL DLL_EXPORT VerifyValidLCIDPassed(LCID actual, LCID expected)
{
    return actual == expected;
}
