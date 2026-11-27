// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_MEMORYBARRIERPROCESSWIDE_H
#define HAVE_MINIPAL_MEMORYBARRIERPROCESSWIDE_H

#include <minipal/utils.h>

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

/**
 * Prepare minipal_memory_barrier_process_wide, which must not be called before this succeeds.
 * Not defined on Windows, which needs no initialization.
 *
 * @return true on success, false otherwise.
 */
bool minipal_initialize_memory_barrier_process_wide(void);

/**
 * Flush the write buffers of the processors that are executing threads of the current process,
 * as FlushProcessWriteBuffers does on Windows.
 */
void minipal_memory_barrier_process_wide(void);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif // HAVE_MINIPAL_MEMORYBARRIERPROCESSWIDE_H
