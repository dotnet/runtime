// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef TARGET_WINDOWS
#include "windows.h"
#else
#include "dlfcn.h"
#endif
#include "stdio.h"
#include "string.h"

#ifndef TARGET_WINDOWS
#define __stdcall
#endif

// typedef for shared lib exported methods
typedef void(__stdcall *fptr)();

#ifdef TARGET_WINDOWS
int __cdecl main()
#else
int main(int argc, char* argv[])
#endif
{
#ifdef TARGET_WINDOWS
    HINSTANCE handle = LoadLibrary("GenerateUnmanagedEntryPoints.dll");
#elif __APPLE__
    void *handle = dlopen(strcat(argv[0], ".dylib"), RTLD_LAZY);
#else
    void *handle = dlopen(strcat(argv[0], ".so"), RTLD_LAZY);
#endif

    if (!handle)
        return 1;

#ifdef TARGET_WINDOWS
    fptr main_assembly_method = (fptr)GetProcAddress(handle, "SharedLibraryAssemblyMethod");
    fptr ref1_assembly_method = (fptr)GetProcAddress(handle, "ReferencedAssembly1Method");
    fptr ref2_assembly_method = (fptr)GetProcAddress(handle, "ReferencedAssembly2Method");
#else
    fptr main_assembly_method = (fptr)dlsym(handle, "SharedLibraryAssemblyMethod");
    fptr ref1_assembly_method = (fptr)dlsym(handle, "ReferencedAssembly1Method");
    fptr ref2_assembly_method = (fptr)dlsym(handle, "ReferencedAssembly2Method");
#endif

    // method must be exposed from GenerateUnmanagedEntryPoints assembly
    if (!main_assembly_method)
        return 2;
    main_assembly_method();

    // method must be exposed from ReferencedAssembly1 assembly
    if (!ref1_assembly_method)
        return 3;
    ref1_assembly_method();

    // method must not be exposed from ReferencedAssembly2 assembly
    if (ref2_assembly_method)
        return 4;

    return 100;
}
