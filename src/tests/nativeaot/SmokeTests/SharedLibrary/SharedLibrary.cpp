// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef TARGET_WINDOWS
#include "windows.h"
#else
#include "dlfcn.h"
#endif
#include "stdio.h"
#include "string.h"

#ifdef __ELF__
#include <elf.h>
#include <stdlib.h>

// ElfW() is not available on all ELF platforms (e.g. OpenBSD)
#if defined(__LP64__) || defined(_LP64)
#define ELF_TYPE(type) Elf64_##type
#else
#define ELF_TYPE(type) Elf32_##type
#endif
#endif

#ifndef TARGET_WINDOWS
#define __stdcall
#define __cdecl
#endif

#ifdef __ELF__
// Returns true if the ELF shared library at the given path has a DT_SONAME equal to the expected value.
static bool HasSoname(const char* path, const char* expected)
{
    FILE* file = fopen(path, "rb");
    if (!file)
        return false;

    bool result = false;
    char* data = NULL;
    long size = 0;
    if (fseek(file, 0, SEEK_END) == 0 && (size = ftell(file)) > 0 && fseek(file, 0, SEEK_SET) == 0)
    {
        data = (char*)malloc(size);
        if (data && fread(data, 1, size, file) == (size_t)size)
        {
            ELF_TYPE(Ehdr)* ehdr = (ELF_TYPE(Ehdr)*)data;
            ELF_TYPE(Shdr)* shdrs = (ELF_TYPE(Shdr)*)(data + ehdr->e_shoff);
            for (int i = 0; i < ehdr->e_shnum; i++)
            {
                if (shdrs[i].sh_type != SHT_DYNAMIC)
                    continue;

                const char* strtab = data + shdrs[shdrs[i].sh_link].sh_offset;
                for (ELF_TYPE(Dyn)* dyn = (ELF_TYPE(Dyn)*)(data + shdrs[i].sh_offset); dyn->d_tag != DT_NULL; dyn++)
                {
                    if (dyn->d_tag == DT_SONAME)
                        result = strcmp(strtab + dyn->d_un.d_val, expected) == 0;
                }
            }
        }
    }

    free(data);
    fclose(file);
    return result;
}
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
    f_ReturnsPrimitiveInt thisMethodMustBeCalledFirst = (f_ReturnsPrimitiveInt)GetProcAddress(handle, "ThisMethodMustBeCalledFirst");
    f_ReturnsPrimitiveInt returnsPrimitiveInt = (f_ReturnsPrimitiveInt)GetProcAddress(handle, "ReturnsPrimitiveInt");
    f_ReturnsPrimitiveBool returnsPrimitiveBool = (f_ReturnsPrimitiveBool)GetProcAddress(handle, "ReturnsPrimitiveBool");
    f_ReturnsPrimitiveChar returnsPrimitiveChar = (f_ReturnsPrimitiveChar)GetProcAddress(handle, "ReturnsPrimitiveChar");
    f_EnsureManagedClassLoaders ensureManagedClassLoaders = (f_EnsureManagedClassLoaders)GetProcAddress(handle, "EnsureManagedClassLoaders");
    f_ReturnsPrimitiveInt checkSimpleGCCollect = (f_ReturnsPrimitiveInt)GetProcAddress(handle, "CheckSimpleGCCollect");
    f_ReturnsPrimitiveInt checkSimpleExceptionHandling = (f_ReturnsPrimitiveInt)GetProcAddress(handle, "CheckSimpleExceptionHandling");
#else
    f_ReturnsPrimitiveInt thisMethodMustBeCalledFirst = (f_ReturnsPrimitiveInt)dlsym(handle, "ThisMethodMustBeCalledFirst");
    f_ReturnsPrimitiveInt returnsPrimitiveInt = (f_ReturnsPrimitiveInt)dlsym(handle, "ReturnsPrimitiveInt");
    f_ReturnsPrimitiveBool returnsPrimitiveBool = (f_ReturnsPrimitiveBool)dlsym(handle, "ReturnsPrimitiveBool");
    f_ReturnsPrimitiveChar returnsPrimitiveChar = (f_ReturnsPrimitiveChar)dlsym(handle, "ReturnsPrimitiveChar");
    f_EnsureManagedClassLoaders ensureManagedClassLoaders = (f_EnsureManagedClassLoaders)dlsym(handle, "EnsureManagedClassLoaders");
    f_ReturnsPrimitiveInt checkSimpleGCCollect = (f_ReturnsPrimitiveInt)dlsym(handle, "CheckSimpleGCCollect");
    f_ReturnsPrimitiveInt checkSimpleExceptionHandling = (f_ReturnsPrimitiveInt)dlsym(handle, "CheckSimpleExceptionHandling");
#endif

    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    // !!!!!      This must be the first managed method called in this test      !!!!!
    // !!!!! Regression test for https://github.com/dotnet/runtime/issues/136201 !!!!!
    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    if (!thisMethodMustBeCalledFirst || thisMethodMustBeCalledFirst() != 100)
        return 6;

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

#ifdef __ELF__
    // argv[0] has been suffixed with ".so" above
    if (!HasSoname(argv[0], "SharedLibrary.so"))
        return 7;
#endif

    // NativeAOT is not designed to be unloadable, so this won't actually unload the library properly. Verify that attempt
    // to unload the library does not to crash at least.
#ifdef TARGET_WINDOWS
    FreeLibrary(handle);
#else
    dlclose(handle);
#endif

    return 100;
}

extern "C" const char* __cdecl __asan_default_options()
{
    // NativeAOT is not designed to be unloadable, so we'll leak a few allocations from the shared library.
    // Disable leak detection as we don't care about these leaks as of now.
    return "detect_leaks=0 use_sigaltstack=0";
}
