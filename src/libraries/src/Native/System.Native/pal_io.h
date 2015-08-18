// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once

#include <stdint.h>

/**
 * File status returned by Stat or FStat.
 */
struct FileStatus
{
    int32_t Flags;     // flags for testing if some members are present (see FileStatusFlags)
    int32_t Mode;      // file mode (see S_I* constants above for bit values)
    int32_t Uid;       // user ID of owner
    int32_t Gid;       // group ID of owner
    int64_t Size;      // total size, in bytes
    int64_t ATime;     // time of last access
    int64_t MTime;     // time of last modification
    int64_t CTime;     // time of last status change
    int64_t BirthTime; // time the file was created
};

/**
 * Constants for interpreting the permissions encoded in FileStatus.Mode.
 * Both the names (without the PAL_ prefix and numeric values are specified by POSIX.1.2008
 */
enum
{
    PAL_S_IRWXU = 00700, // Read, write, execute/search by owner.
    PAL_S_IRUSR = 00400, // Read permission, owner.
    PAL_S_IWUSR = 00200, // Write permission, owner.
    PAL_S_IXUSR = 00100, // Execute/search permission, owner.
    PAL_S_IRWXG = 00070, // Read, write, execute/search by group.
    PAL_S_IRGRP = 00040, // Read permission, group.
    PAL_S_IWGRP = 00020, // Write permission, group.
    PAL_S_IXGRP = 00010, // Execute/search permission, group.
    PAL_S_IRWXO = 00007, // Read, write, execute/search by others.
    PAL_S_IROTH = 00004, // Read permission, others.
    PAL_S_IWOTH = 00002, // Write permission, others.
    PAL_S_IXOTH = 00001, // Execute/search permission, others.
    PAL_S_ISUID = 04000, // Set-user-ID on execution.
    PAL_S_ISGID = 02000, // Set-group-ID on execution.
};

/**
 * Constants for interpreting the permissions encoded in FileStatus.Mode
 * Only the names (without the PAL_ prefix) are specified by POSIX.1.2008.
 * The values chosen below are in common use, but not guaranteed.
 */
enum
{
    PAL_S_IFMT  = 0xF000, // Type of file (apply as mask to FileStatus.Mode and one of S_IF*)
    PAL_S_IFIFO = 0x1000, // FIFO (named pipe)
    PAL_S_IFCHR = 0x2000, // Character special
    PAL_S_IFDIR = 0x4000, // Directory
    PAL_S_IFREG = 0x8000, // Regular file
    PAL_S_IFLNK = 0xA000, // Symbolic link
};

/**
 * Constants for interpreting FileStatus.Flags.
 */
enum
{
    FILESTATUS_FLAGS_NONE          = 0,
    FILESTATUS_FLAGS_HAS_BIRTHTIME = 1,
};

/**
 * Get file status from a decriptor. Implemented as shim to fstat(2).
 *
 * Returns 0 for success, -1 for failure. Sets errno on failure.
 */
extern "C"
int32_t FStat(
    int32_t fileDescriptor,
    FileStatus* output);

/**
 * Get file status from a full path. Implemented as shim to stat(2).
 *
 * Returns 0 for success, -1 for failure. Sets errno on failure.
 */
extern "C" 
int32_t Stat(
    const char* path,
    FileStatus* output);

/**
 * Get file stats from a full path. Implemented as shim to lstat(2).
 *
 * Returns 0 for success, -1 for failure. Sets errno on failure.
 */
extern "C"
int32_t LStat(
    const char* path, 
    FileStatus* output);
