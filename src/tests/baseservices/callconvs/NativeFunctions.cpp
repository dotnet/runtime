// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <platformdefines.h>

namespace
{
    int DoubleIntImpl(int a)
    {
        return a * 2;
    }
}

#if defined HOST_X86 && !defined TARGET_UNIX
#pragma comment(linker, "/export:DoubleInt=_DoubleInt@4")
#endif
extern "C" DLL_EXPORT int DoubleInt(int a)
{
    return DoubleIntImpl(a);
}

extern "C" DLL_EXPORT int __cdecl DoubleIntCdecl(int a)
{
    return DoubleIntImpl(a);
}

#if defined HOST_X86 && !defined TARGET_UNIX
#pragma comment(linker, "/export:DoubleIntStdcall=_DoubleIntStdcall@4")
#endif
extern "C" DLL_EXPORT int __stdcall DoubleIntStdcall(int a)
{
    return DoubleIntImpl(a);
}

#include <cwctype>

namespace
{
    WCHAR ToUpperImpl(WCHAR a)
    {
        return (WCHAR)std::towupper(a);
    }
}

#if defined HOST_X86 && !defined TARGET_UNIX
#pragma comment(linker, "/export:ToUpper=_ToUpper@4")
#endif
extern "C" DLL_EXPORT WCHAR ToUpper(WCHAR a)
{
    return ToUpperImpl(a);
}

extern "C" DLL_EXPORT WCHAR __cdecl ToUpperCdecl(WCHAR a)
{
    return ToUpperImpl(a);
}

#if defined HOST_X86 && !defined TARGET_UNIX
#pragma comment(linker, "/export:ToUpperStdcall=_ToUpperStdcall@4")
#endif
extern "C" DLL_EXPORT WCHAR __stdcall ToUpperStdcall(WCHAR a)
{
    return ToUpperImpl(a);
}
