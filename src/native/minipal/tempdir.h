// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_TEMPDIR_H
#define HAVE_MINIPAL_TEMPDIR_H

#include <stdbool.h>
#include <stddef.h>

#ifdef __cplusplus
extern "C" {
#endif

/**
 * Resolve the system temporary directory and write it to @p buffer with a
 * trailing platform path separator and NUL terminator.
 *
 * The lookup order is:
 *   - Unix: $TMPDIR, otherwise "/tmp/"
 *   - Windows: %TMP%, then %TEMP%, otherwise "C:\\Temp\\"
 *
 * The implementation only calls getenv/strlen/memcpy and does no
 * heap allocation. POSIX does not list getenv as async-signal-safe,
 * so callers reaching this helper from a signal-handler path inherit
 * the same in-practice constraints as a direct getenv call (the
 * in-proc crash reporter already relies on this).
 *
 * @param buffer       Destination buffer; must be non-NULL.
 * @param buffer_size  Size of @p buffer in bytes.
 * @return true on success; false if @p buffer is NULL, @p buffer_size
 *         is zero, or the resolved directory plus separator and NUL
 *         does not fit. On failure @p buffer is left NUL-terminated
 *         (when @p buffer_size > 0) so callers can fall back without
 *         reading uninitialized memory.
 */
bool minipal_get_tempdir(char* buffer, size_t buffer_size);

#ifdef __cplusplus
}
#endif

#endif // HAVE_MINIPAL_TEMPDIR_H
