// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>

#ifndef TARGET_WINDOWS
#define __stdcall
#endif

#if defined(_WIN32)
extern "C" int __cdecl __managed__Main(int argc, wchar_t* argv[]);
#else
extern "C" int __managed__Main(int argc, char* argv[]);
#endif

extern "C" void __cdecl IncrementExitCode(int32_t amount);

#if defined(_WIN32)
int __cdecl wmain(int argc, wchar_t* argv[])
#else
int main(int argc, char* argv[])
#endif
{
    puts("hello from native main");
    IncrementExitCode(61);
    return __managed__Main(argc, argv);
}

extern "C" const char* __stdcall __asan_default_options()
{
    // NativeAOT is not designed to be unloadable, so we'll leak a few allocations from the shared library.
    // Disable leak detection as we don't care about these leaks as of now.
    return "detect_leaks=0 use_sigaltstack=0";
}
