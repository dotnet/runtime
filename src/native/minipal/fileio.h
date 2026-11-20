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
 * The returned handle is write-only, permits concurrent reads, and is not
 * inherited by child processes.
 *
 * Returns the platform file handle as an intptr_t on success, or -1 on
 * failure. On Windows, GetLastError() provides the error;
 * ERROR_ALREADY_EXISTS indicates an existing non-regular file. On Unix, errno
 * provides the error; EEXIST indicates an existing non-regular file.
 */
intptr_t minipal_create_owner_only_file_for_overwrite(const char* path);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif // HAVE_MINIPAL_FILEIO_H
