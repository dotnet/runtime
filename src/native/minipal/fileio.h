// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_FILEIO_H
#define HAVE_MINIPAL_FILEIO_H

#include <stdint.h>

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

/**
 * Creates a file that is accessible only to the current effective user,
 * replacing an existing regular file. Existing non-regular files are rejected.
 * Replacement is not atomic: the existing file is deleted before the new file
 * is created. If creation fails after deletion, the previous file is not restored.
 * On failure, the path may no longer exist.
 * The returned handle is write-only and is not inherited by child processes.
 * On Windows, concurrent readers must share write access.
 * On Windows, replacing a read-only file fails, as does replacing a file
 * when an existing handle does not share delete access.
 *
 * Returns the platform file handle as an intptr_t on success, or -1 on
 * failure.
 */
intptr_t minipal_create_owner_only_file_for_overwrite(const char* path);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif // HAVE_MINIPAL_FILEIO_H
