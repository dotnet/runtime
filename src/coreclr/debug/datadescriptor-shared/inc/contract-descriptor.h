// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef CONTRACT_DESCRIPTOR_H
#define CONTRACT_DESCRIPTOR_H

#include <stdint.h>

struct ContractDescriptor
{
    uint64_t magic;
    uint32_t flags;
    const uint32_t descriptor_size;
    const char* descriptor;
    const uint32_t pointer_data_count;
    uint32_t pad0;
    const void** pointer_data;
};

#endif // CONTRACT_DESCRIPTOR_H
