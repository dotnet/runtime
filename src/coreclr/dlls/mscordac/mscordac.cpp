// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !defined(_MSC_VER)
#if !defined(__cdecl)
#if defined(__i386__)
#define __cdecl __attribute__((cdecl))
#else
#define __cdecl
#endif
#endif
#endif

extern "C" void __cdecl assertAbort(const char* why, const char* file, unsigned line)
{
#ifdef _MSC_VER
    __debugbreak();
#else // _MSC_VER
    __builtin_trap();
#endif // _MSC_VER
}
