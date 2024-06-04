// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __HOST_RUNTIME_CONTRACT_H__
#define __HOST_RUNTIME_CONTRACT_H__

#include <stddef.h>
#include <stdint.h>

#if defined(_WIN32)
    #define HOST_CONTRACT_CALLTYPE __stdcall
#else
    #define HOST_CONTRACT_CALLTYPE
#endif

// Known host property names
#define HOST_PROPERTY_RUNTIME_CONTRACT "HOST_RUNTIME_CONTRACT"
#define HOST_PROPERTY_APP_PATHS "APP_PATHS"
#define HOST_PROPERTY_BUNDLE_PROBE "BUNDLE_PROBE"
#define HOST_PROPERTY_ENTRY_ASSEMBLY_NAME "ENTRY_ASSEMBLY_NAME"
#define HOST_PROPERTY_NATIVE_DLL_SEARCH_DIRECTORIES "NATIVE_DLL_SEARCH_DIRECTORIES"
#define HOST_PROPERTY_PINVOKE_OVERRIDE "PINVOKE_OVERRIDE"
#define HOST_PROPERTY_PLATFORM_RESOURCE_ROOTS "PLATFORM_RESOURCE_ROOTS"
#define HOST_PROPERTY_TRUSTED_PLATFORM_ASSEMBLIES "TRUSTED_PLATFORM_ASSEMBLIES"

struct host_runtime_contract
{
    size_t size;

    // Context for the contract. Pass to functions taking a contract context.
    void* context;

    // Get the value of a runtime property.
    // Returns the length of the property including a terminating null or -1 if not found.
    size_t(HOST_CONTRACT_CALLTYPE* get_runtime_property)(
        const char* key,
        /*out*/ char* value_buffer,
        size_t value_buffer_size,
        void* contract_context);

    // Probe an app bundle for `path`. Sets its location (`offset`, `size`) in the bundle if found.
    // Returns true if found, false otherwise.
    bool(HOST_CONTRACT_CALLTYPE* bundle_probe)(
        const char* path,
        /*out*/ int64_t* offset,
        /*out*/ int64_t* size,
        /*out*/ int64_t* compressedSize);

    // Get the function overriding the specified p/invoke (`library_name`, `entry_point_name`).
    // Returns a pointer to the function if the p/invoke is overridden, nullptr otherwise.
    const void* (HOST_CONTRACT_CALLTYPE* pinvoke_override)(
        const char* library_name,
        const char* entry_point_name);
};

#endif // __HOST_RUNTIME_CONTRACT_H__
