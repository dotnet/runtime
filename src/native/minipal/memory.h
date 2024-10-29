// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef MINIPAL_MEMORY_H
#define MINIPAL_MEMORY_H

#include <stdlib.h>

#ifdef __cplusplus
    extern "C"
    {
#endif // __cplusplus

// Allocate memory on the platform equivalent of the CoTaskMem heap.
void* minicom_CoTaskMemAlloc(size_t cb);

// Free memory allocated on the platform equivalent of the CoTaskMem heap.
void minicom_CoTaskMemFree(void* pv);

#ifdef __cplusplus
    }
#endif // __cplusplus

#endif // MINIPAL_MEMORY_H
