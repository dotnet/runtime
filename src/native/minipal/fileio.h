// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_FILEIO_H
#define HAVE_MINIPAL_FILEIO_H

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

#ifndef _WIN32
/**
 * Creates an owner-readable and owner-writable file, replacing an existing
 * regular file. Existing non-regular files are rejected. The returned
 * descriptor is write-only and close-on-exec.
 *
 * Returns the file descriptor on success, or -1 with errno set on failure.
 */
int minipal_create_owner_only_file_for_overwrite(const char* path);
#endif // !_WIN32

#ifdef __cplusplus
}
#endif // __cplusplus

#endif // HAVE_MINIPAL_FILEIO_H
