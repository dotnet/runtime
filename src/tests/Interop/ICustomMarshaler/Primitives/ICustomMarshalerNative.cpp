// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdlib.h>
#include <xplatform.h>

using TestDelegate = LPCSTR(STDMETHODCALLTYPE*)(int);
using TestDelegateRef = LPCSTR(STDMETHODCALLTYPE*)(int*);

extern "C" int DLL_EXPORT STDMETHODCALLTYPE NativeParseInt(LPCSTR str)
{
    return atoi(str);
}

extern "C" int DLL_EXPORT STDMETHODCALLTYPE NativeParseIntRef(LPCSTR* str)
{
    return atoi(*str);
}

extern "C" void DLL_EXPORT STDMETHODCALLTYPE NativeParseIntOut(int* outVal)
{
    *outVal = 2334;
}

extern "C" int DLL_EXPORT STDMETHODCALLTYPE NativeParseIntDelegate(int val, TestDelegate del)
{
    return atoi(del(val));
}

extern "C" int DLL_EXPORT STDMETHODCALLTYPE NativeParseIntDelegateRef(int val, TestDelegateRef del)
{
    return atoi(del(&val));
}
