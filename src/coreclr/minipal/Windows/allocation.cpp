// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <windows.h>
#include "minipal.h"

void* VMToOSInterface::AllocateMemory(void* address, size_t size, MemoryProtection protection)
{
    return VirtualAlloc(address, size, MEM_RESERVE | MEM_COMMIT, static_cast<DWORD>(protection));
}
