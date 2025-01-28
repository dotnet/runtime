// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

bool InterfaceDispatch_InitializePal()
{
    return true;
}

// Allocate memory aligned at sizeof(void*)*2 boundaries
void *InterfaceDispatch_AllocDoublePointerAligned(size_t size)
{
    return malloc(size);
}

// Allocate memory aligned at at least sizeof(void*)
void *InterfaceDispatch_AllocPointerAligned(size_t size)
{
    return malloc(size);
}
