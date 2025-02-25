// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef TARGET_WINDOWS
#include "windows.h"
#else
#include "dlfcn.h"
#endif
#include <cstdint>
#include <string>

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
#else
#if TARGET_APPLE
    constexpr char const* ext = ".dylib";
#else
    constexpr char const* ext = ".so";
#endif

    std::string path = argv[0];
    // Step out of the current directory and the parent directory.
    path = path.substr(0, path.find_last_of("/\\"));
    path = path.substr(0, path.find_last_of("/\\"));
    path += "/subdir/SharedLibraryDependencyLoading";
    path += ext;
    void* handle = dlopen(path.c_str(), RTLD_LAZY);
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
