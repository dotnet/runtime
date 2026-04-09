// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef TARGET_WINDOWS
#include "windows.h"
#else
#include "dlfcn.h"
#endif
#include "stdio.h"
#include <stdint.h>
#include "string.h"

#ifndef TARGET_WINDOWS
#define __stdcall
#endif

// typedef for shared lib exported methods
#if defined(_WIN32)
typedef int(__stdcall *f___managed__Main)(int argc, wchar_t* argv[]);
#else
typedef int(__stdcall *f___managed__Main)(int argc, char* argv[]);
#endif
typedef void(__stdcall *f_IncrementExitCode)(int32_t amount);

#if defined(_WIN32)
int __cdecl wmain(int argc, wchar_t* argv[])
#else
int main(int argc, char* argv[])
#endif
{
#ifdef TARGET_WINDOWS
    HINSTANCE handle = LoadLibrary("CustomMainWithStubExe.dll");
#elif __APPLE__
    void *handle = dlopen(strcat(argv[0], ".dylib"), RTLD_LAZY);
#else
    void *handle = dlopen(strcat(argv[0], ".so"), RTLD_LAZY);
#endif

    if (!handle)
        return 1;

#ifdef TARGET_WINDOWS
    f___managed__Main __managed__MainFunc = (f___managed__Main)GetProcAddress(handle, "__managed__Main");
    f_IncrementExitCode IncrementExitCodeFunc = (f_IncrementExitCode)GetProcAddress(handle, "IncrementExitCode");
#else
    f___managed__Main __managed__MainFunc = (f___managed__Main)dlsym(handle, "__managed__Main");
    f_IncrementExitCode IncrementExitCodeFunc = (f_IncrementExitCode)dlsym(handle, "IncrementExitCode");
#endif

    puts("hello from native main");
    IncrementExitCodeFunc(61);
    return __managed__MainFunc(argc, argv);
}

extern "C" const char* __stdcall __asan_default_options()
{
    // NativeAOT is not designed to be unloadable, so we'll leak a few allocations from the shared library.
    // Disable leak detection as we don't care about these leaks as of now.
    return "detect_leaks=0 use_sigaltstack=0";
}
