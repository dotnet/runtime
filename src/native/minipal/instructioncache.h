// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_INSTRUCTIONCACHE_H
#define HAVE_MINIPAL_INSTRUCTIONCACHE_H

#include <stdbool.h>
#include <stddef.h>

#ifdef __cplusplus
extern "C" {
#endif

/**
 * Flush the instruction cache for a range of memory in the current process, after writing code there.
 *
 * @param address Start of the range. On Windows, NULL flushes the whole instruction cache; elsewhere it does nothing.
 * @param size Size of the range in bytes.
 * @return true on success, false otherwise.
 */
bool minipal_flush_instruction_cache(const void* address, size_t size);

#ifdef __cplusplus
}
#endif

#endif // HAVE_MINIPAL_INSTRUCTIONCACHE_H
