// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef TARGET_WINDOWS
#include "windows.h"
#else
#include "dlfcn.h"
#endif
#include <cstdint>

#ifndef TARGET_WINDOWS
#define __stdcall
#endif

// typedef for shared lib exported methods
using f_MultiplyIntegers = int32_t(__stdcall *)(int32_t, int32_t);

#ifdef TARGET_WINDOWS
int __cdecl main()
#else
int main(int argc, char* argv[])
#endif
{
#ifdef TARGET_WINDOWS
    // We need to include System32 to find system dependencies of SharedLibraryDependencyLoading.dll
    HINSTANCE handle = LoadLibraryEx("..\\subdir\\SharedLibraryDependencyLoading.dll", nullptr, LOAD_LIBRARY_SEARCH_APPLICATION_DIR | LOAD_LIBRARY_SEARCH_SYSTEM32);
#elif __APPLE__
    void *handle = dlopen("../subdir/SharedLibraryDependencyLoading.dylib", RTLD_LAZY);
#else
    void *handle = dlopen("../subdir/SharedLibraryDependencyLoading.so", RTLD_LAZY);
#endif

    if (!handle)
        return 1;

#ifdef TARGET_WINDOWS
    f_MultiplyIntegers multiplyIntegers = (f_MultiplyIntegers)GetProcAddress(handle, "MultiplyIntegers");
#else
    f_MultiplyIntegers multiplyIntegers = (f_MultiplyIntegers)dlsym(handle, "MultiplyIntegers");
#endif

    return multiplyIntegers(10, 7) == 70 ? 100 : 2;
}

extern "C" const char* __stdcall __asan_default_options()
{
    // NativeAOT is not designed to be unloadable, so we'll leak a few allocations from the shared library.
    // Disable leak detection as we don't care about these leaks as of now.
    return "detect_leaks=0 use_sigaltstack=0";
}
