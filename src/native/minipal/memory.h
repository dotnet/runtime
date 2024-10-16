// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef MINIPAL_MEMORY_H
#define MINIPAL_MEMORY_H

#include <stdlib.h>

// Allocate memory on the platform equivalent of the CoTaskMem heap.
void* minipal_co_task_mem_alloc(size_t cb);

// Free memory allocated on the platform equivalent of the CoTaskMem heap.
void minipal_co_task_mem_free(void* pv);

#endif // MINIPAL_MEMORY_H
