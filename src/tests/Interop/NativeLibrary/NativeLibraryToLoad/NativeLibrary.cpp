// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include <stdio.h>
#include <platformdefines.h>
#ifndef TARGET_WINDOWS
#include <dlfcn.h>
#endif

extern "C" DLL_EXPORT int NativeSum(int a, int b)
{
    return a + b;
}

extern "C" DLL_EXPORT int RunExportedFunction(void *function, int arg1, int arg2)
{
   int(*f)(int, int) = reinterpret_cast<int(*)(int,int)>(function);
   return f(arg1, arg2);
}


extern "C" DLL_EXPORT void* LoadLibraryGlobally(const char* name)
{
#ifdef TARGET_WINDOWS
    // Windows doesn't support global symbol loading.
    return NULL;
#else
    return dlopen(name, RTLD_GLOBAL | RTLD_LAZY);
#endif
}