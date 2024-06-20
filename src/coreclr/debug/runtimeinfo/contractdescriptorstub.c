// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdint.h>

#ifdef _MSC_VER
#define DLLEXPORT __declspec(dllexport)
#else
#define DLLEXPORT __attribute__((visibility("default")))
#endif

struct DotNetRuntimeContractDescriptor
{
    uint64_t magic;
    uint32_t flags;
    const uint32_t descriptor_size;
    const char *descriptor;
    const uint32_t pointer_data_count;
    uint32_t pad0;
    const uintptr_t *pointer_data;
};

extern const uintptr_t contractDescriptorPointerData[];

// just the placeholder pointer
const uintptr_t contractDescriptorPointerData[] = { (uintptr_t)0 };

DLLEXPORT struct DotNetRuntimeContractDescriptor DotNetRuntimeContractDescriptor;

#define STUB_DESCRIPTOR "{\"version\":0,\"baseline\":\"empty\",\"contracts\":{},\"types\":{},\"globals\":{}}"

DLLEXPORT struct DotNetRuntimeContractDescriptor DotNetRuntimeContractDescriptor = {
    .magic = 0x0043414443434e44ull, // "DNCCDAC\0"
    .flags = 0x1u & (sizeof(void*) == 4 ? 0x02u : 0x00u),
    .descriptor_size = sizeof(STUB_DESCRIPTOR),
    .descriptor = STUB_DESCRIPTOR,
    .pointer_data_count = 1,
    .pointer_data = &contractDescriptorPointerData[0],
};
