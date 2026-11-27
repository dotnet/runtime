// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_DESCRIPTORLIMIT_H
#define HAVE_MINIPAL_DESCRIPTORLIMIT_H

#include <stdbool.h>

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

/**
 * Raise the soft limit on open file descriptors of the current process to its hard limit (capped at
 * OPEN_MAX on Apple platforms). Does nothing on WebAssembly and on musl-based Linux.
 *
 * @return false if the limit could not be queried or set, true otherwise.
 */
bool minipal_increase_descriptor_limit(void);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif // HAVE_MINIPAL_DESCRIPTORLIMIT_H
