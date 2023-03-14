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
typedef void(__stdcall *fptr)(int);

#ifdef TARGET_WINDOWS
extern "C" int __managed__Main(int argc, wchar_t* argv[]);
#else
extern "C" int __managed__Main(int argc, char* argv[]);
#endif

#ifdef TARGET_WINDOWS
int __cdecl wmain(int argc, wchar_t* argv[])
#else
int main(int argc, char* argv[])
#endif
{
#ifdef TARGET_WINDOWS
    HINSTANCE handle = GetModuleHandle(NULL);
#else
    void *handle = dlopen(0, RTLD_LAZY);
#endif

    if (!handle)
        return 1;

#ifdef TARGET_WINDOWS
    fptr main_assembly_initializer = (fptr)GetProcAddress(handle, "InitializeMainAssembly");
    fptr ref1_assembly_initializer = (fptr)GetProcAddress(handle, "InitializeReferencedAssembly1");
    fptr ref2_assembly_initializer = (fptr)GetProcAddress(handle, "InitializeReferencedAssembly2");
#else
    fptr main_assembly_initializer = (fptr)dlsym(handle, "InitializeMainAssembly");
    fptr ref1_assembly_initializer = (fptr)dlsym(handle, "InitializeReferencedAssembly1");
    fptr ref2_assembly_initializer = (fptr)dlsym(handle, "InitializeReferencedAssembly2");
#endif

    // method must be exposed from ExportsFromReferencedAssemblies assembly
    if (!main_assembly_initializer)
        return 2;
    main_assembly_initializer(20);

    // method must be exposed from ReferencedAssembly1 assembly
    if (!ref1_assembly_initializer)
        return 3;
    ref1_assembly_initializer(30);

    // method must not be exposed from ReferencedAssembly2 assembly
    if (ref2_assembly_initializer)
        return 4;

    return __managed__Main(argc, argv);
}
