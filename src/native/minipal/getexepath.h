// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_GETEXEPATH_H
#define HAVE_MINIPAL_GETEXEPATH_H

#ifdef __cplusplus
extern "C" {
#endif

/**
 * Get the full path to the executable for the current process.
 * Resolves symbolic links. The caller is responsible for releasing the buffer.
 *
 * @return A pointer to a null-terminated string containing the executable path,
 *         or NULL if an error occurs.
 */
char* minipal_getexepath(void);

#ifdef __cplusplus
}
#endif // extern "C"

#endif // HAVE_MINIPAL_GETEXEPATH_H
