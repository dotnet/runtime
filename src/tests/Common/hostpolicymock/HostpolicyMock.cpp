// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Mock implementation of the hostpolicy.cpp exported methods.
// Used for testing CoreCLR/Corlib functionality which calls into hostpolicy.

#include <string>
#include <platformdefines.h>

// dllexport
#if defined _WIN32

#define SHARED_API extern "C" __declspec(dllexport)
typedef wchar_t char_t;
typedef std::wstring string_t;

#else //!_WIN32

#if __GNUC__ >= 4
#define SHARED_API extern "C" __attribute__ ((visibility ("default")))
#else
#define SHARED_API extern "C"
#endif

typedef char char_t;
typedef std::string string_t;

#endif //_WIN32

int g_corehost_resolve_component_dependencies_returnValue = -1;
string_t g_corehost_resolve_component_dependencies_assemblyPaths;
string_t g_corehost_resolve_component_dependencies_nativeSearchPaths;
string_t g_corehost_resolve_component_dependencies_resourceSearchPaths;

typedef void(__cdecl *Callback_corehost_resolve_component_dependencies)(const char_t *component_main_assembly_path);
Callback_corehost_resolve_component_dependencies g_corehost_resolve_component_dependencies_Callback;

typedef void(__cdecl *corehost_resolve_component_dependencies_result_fn)(
    const char_t* assembly_paths,
    const char_t* native_search_paths,
    const char_t* resource_search_paths);

SHARED_API int __cdecl corehost_resolve_component_dependencies(
    const char_t *component_main_assembly_path,
    corehost_resolve_component_dependencies_result_fn result)
{
    if (g_corehost_resolve_component_dependencies_Callback != NULL)
    {
        g_corehost_resolve_component_dependencies_Callback(component_main_assembly_path);
    }

    if (g_corehost_resolve_component_dependencies_returnValue == 0)
    {
        result(
            g_corehost_resolve_component_dependencies_assemblyPaths.data(),
            g_corehost_resolve_component_dependencies_nativeSearchPaths.data(),
            g_corehost_resolve_component_dependencies_resourceSearchPaths.data());
    }

    return g_corehost_resolve_component_dependencies_returnValue;
}

SHARED_API void __cdecl Set_corehost_resolve_component_dependencies_Values(
    int returnValue,
    const char_t *assemblyPaths,
    const char_t *nativeSearchPaths,
    const char_t *resourceSearchPaths)
{
    g_corehost_resolve_component_dependencies_returnValue = returnValue;
    g_corehost_resolve_component_dependencies_assemblyPaths.assign(assemblyPaths);
    g_corehost_resolve_component_dependencies_nativeSearchPaths.assign(nativeSearchPaths);
    g_corehost_resolve_component_dependencies_resourceSearchPaths.assign(resourceSearchPaths);
}

SHARED_API void __cdecl Set_corehost_resolve_component_dependencies_Callback(
    Callback_corehost_resolve_component_dependencies callback)
{
    g_corehost_resolve_component_dependencies_Callback = callback;
}


typedef void(__cdecl *corehost_error_writer_fn)(const char_t* message);
corehost_error_writer_fn g_corehost_set_error_writer_lastSet_error_writer;
corehost_error_writer_fn g_corehost_set_error_writer_returnValue;

SHARED_API corehost_error_writer_fn __cdecl corehost_set_error_writer(corehost_error_writer_fn error_writer)
{
    g_corehost_set_error_writer_lastSet_error_writer = error_writer;
    return g_corehost_set_error_writer_returnValue;
}

SHARED_API void __cdecl Set_corehost_set_error_writer_returnValue(corehost_error_writer_fn error_writer)
{
    g_corehost_set_error_writer_returnValue = error_writer;
}

SHARED_API corehost_error_writer_fn __cdecl Get_corehost_set_error_writer_lastSet_error_writer()
{
    return g_corehost_set_error_writer_lastSet_error_writer;
}
