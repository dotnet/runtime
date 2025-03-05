// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef TARGET_WINDOWS
#include "windows.h"
#else
#include "dlfcn.h"
#endif
#include <cstdint>
#include <string>
#include <memory>
#include <iostream>

#ifndef TARGET_WINDOWS
#define __stdcall
#endif

// typedef for shared lib exported methods
using f_MultiplyIntegers = int32_t(__stdcall *)(int32_t, int32_t);
using f_getBaseDirectory = const char*(__stdcall *)();

#ifdef TARGET_WINDOWS
template<typename T>
struct CoTaskMemDeleter
{
    void operator()(T* p) const
    {
        CoTaskMemFree((void*)p);
    }
};
template<typename T>
using CoTaskMemPtr = std::unique_ptr<T, CoTaskMemDeleter<T>>;
#else
template<typename T>
using CoTaskMemPtr = std::unique_ptr<T>;
#endif

#ifdef TARGET_WINDOWS
int __cdecl main(int argc, char* argv[])
#else
int main(int argc, char* argv[])
#endif
{
    std::string pathToSubdir = argv[0];
    // Step out of the current directory and the parent directory.
    pathToSubdir = pathToSubdir.substr(0, pathToSubdir.find_last_of("/\\"));
    pathToSubdir = pathToSubdir.substr(0, pathToSubdir.find_last_of("/\\"));
#ifdef TARGET_WINDOWS
    pathToSubdir += "subdir\\";
    // We need to include System32 to find system dependencies of SharedLibraryDependencyLoading.dll
    HINSTANCE handle = LoadLibraryEx("..\\subdir\\SharedLibraryDependencyLoading.dll", nullptr, LOAD_LIBRARY_SEARCH_APPLICATION_DIR | LOAD_LIBRARY_SEARCH_SYSTEM32);
#else
#if TARGET_APPLE
    constexpr char const* ext = ".dylib";
#else
    constexpr char const* ext = ".so";
#endif

    pathToSubdir += "subdir/";
    std::string path = pathToSubdir +  "SharedLibraryDependencyLoading";
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

    if (multiplyIntegers(10, 7) != 70)
        return 2;

    CoTaskMemPtr<const char> baseDirectory;
#ifdef TARGET_WINDOWS
    f_getBaseDirectory getBaseDirectory = (f_getBaseDirectory)GetProcAddress(handle, "GetBaseDirectory");
#else
    f_getBaseDirectory getBaseDirectory = (f_getBaseDirectory)dlsym(handle, "GetBaseDirectory");
#endif

    baseDirectory.reset(getBaseDirectory());
    if (baseDirectory == nullptr)
        return 3;

    if (pathToSubdir != baseDirectory.get())
    {
        std::cout << "Expected base directory: " << pathToSubdir << std::endl;
        std::cout << "Actual base directory: " << baseDirectory.get() << std::endl;
        return 4;
    }

    return 100;
}

extern "C" const char* __stdcall __asan_default_options()
{
    // NativeAOT is not designed to be unloadable, so we'll leak a few allocations from the shared library.
    // Disable leak detection as we don't care about these leaks as of now.
    return "detect_leaks=0 use_sigaltstack=0";
}
