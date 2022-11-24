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
#define HOST_PROPERTY_ENTRY_ASSEMBLY_NAME "ENTRY_ASSEMBLY_NAME"

struct host_runtime_contract
{
    void* context;

    bool(HOST_CONTRACT_CALLTYPE* bundle_probe)(
        const char* path,
        int64_t* offset,
        int64_t* size,
        int64_t* compressedSize);

    const void* (HOST_CONTRACT_CALLTYPE* pinvoke_override)(
        const char* library_name,
        const char* entry_point_name);

    size_t(HOST_CONTRACT_CALLTYPE* get_runtime_property)(
        const char* key,
        char* value_buffer,
        size_t value_buffer_size,
        void* contract_context);
};

#endif // __HOST_RUNTIME_CONTRACT_H__
