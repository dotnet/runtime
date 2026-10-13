// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <windows.h>
#include "minipal.h"

extern void* ClrVirtualAlloc(void* address, SIZE_T size, DWORD allocationType, DWORD protection);

void* VMToOSInterface::AllocateMemory(void* address, size_t size, MemoryProtection protection)
{
    return ClrVirtualAlloc(address, size, MEM_RESERVE | MEM_COMMIT, static_cast<DWORD>(protection));
}
