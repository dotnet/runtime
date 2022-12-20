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
typedef int(__stdcall *f_ReturnsPrimitiveInt)();
typedef bool(__stdcall *f_ReturnsPrimitiveBool)();
typedef char(__stdcall *f_ReturnsPrimitiveChar)();
typedef void(__stdcall *f_EnsureManagedClassLoaders)();

#ifdef TARGET_WINDOWS
int __cdecl main()
#else
int main(int argc, char* argv[])
#endif
{
#ifdef TARGET_WINDOWS
    HINSTANCE handle = LoadLibrary("SharedLibrary.dll");
#elif __APPLE__
    void *handle = dlopen(strcat(argv[0], ".dylib"), RTLD_LAZY);
#else
    void *handle = dlopen(strcat(argv[0], ".so"), RTLD_LAZY);
#endif

    if (!handle)
        return 1;

#ifdef TARGET_WINDOWS
    f_ReturnsPrimitiveInt returnsPrimitiveInt = (f_ReturnsPrimitiveInt)GetProcAddress(handle, "ReturnsPrimitiveInt");
    f_ReturnsPrimitiveBool returnsPrimitiveBool = (f_ReturnsPrimitiveBool)GetProcAddress(handle, "ReturnsPrimitiveBool");
    f_ReturnsPrimitiveChar returnsPrimitiveChar = (f_ReturnsPrimitiveChar)GetProcAddress(handle, "ReturnsPrimitiveChar");
    f_EnsureManagedClassLoaders ensureManagedClassLoaders = (f_EnsureManagedClassLoaders)GetProcAddress(handle, "EnsureManagedClassLoaders");
    f_ReturnsPrimitiveInt checkSimpleGCCollect = (f_ReturnsPrimitiveInt)GetProcAddress(handle, "CheckSimpleGCCollect");
    f_ReturnsPrimitiveInt checkSimpleExceptionHandling = (f_ReturnsPrimitiveInt)GetProcAddress(handle, "CheckSimpleExceptionHandling");
#else
    f_ReturnsPrimitiveInt returnsPrimitiveInt = (f_ReturnsPrimitiveInt)dlsym(handle, "ReturnsPrimitiveInt");
    f_ReturnsPrimitiveBool returnsPrimitiveBool = (f_ReturnsPrimitiveBool)dlsym(handle, "ReturnsPrimitiveBool");
    f_ReturnsPrimitiveChar returnsPrimitiveChar = (f_ReturnsPrimitiveChar)dlsym(handle, "ReturnsPrimitiveChar");
    f_EnsureManagedClassLoaders ensureManagedClassLoaders = (f_EnsureManagedClassLoaders)dlsym(handle, "EnsureManagedClassLoaders");
    f_ReturnsPrimitiveInt checkSimpleGCCollect = (f_ReturnsPrimitiveInt)dlsym(handle, "CheckSimpleGCCollect");
    f_ReturnsPrimitiveInt checkSimpleExceptionHandling = (f_ReturnsPrimitiveInt)dlsym(handle, "CheckSimpleExceptionHandling");
#endif

    if (returnsPrimitiveInt() != 10)
        return 1;

    if (!returnsPrimitiveBool())
        return 2;

    if (returnsPrimitiveChar() != 'a')
        return 3;

    // As long as no unmanaged exception is thrown
    // managed class loaders were initialized successfully
    ensureManagedClassLoaders();

    if (checkSimpleGCCollect() != 100)
        return 4;

    if (checkSimpleExceptionHandling() != 100)
        return 5;

    // NativeAOT is not designed to be unloadable, so this won't actually unload the library properly. Verify that attempt
    // to unload the library does not to crash at least.
#ifdef TARGET_WINDOWS
    FreeLibrary(handle);
#else
    // TODO: How to pin the library in memory on Unix?
    // dlclose(handle);
#endif

    return 100;
}
