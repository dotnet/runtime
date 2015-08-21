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
 * Constants for interpreting the flags passed to Open or ShmOpen.
 * There are several other values defined by POSIX but not implemented
 * everywhere. The set below is restricted to the current needs of
 * COREFX, which increases portability and speeds up conversion. We
 * can add more as needed.
 */
enum
{
    // Access modes (mutually exclusive).
    PAL_O_RDONLY           = 0x0000, // Open for read-only
    PAL_O_WRONLY           = 0x0001, // Open for write-only
    PAL_O_RDWR             = 0x0002, // Open for read-write

    // Mask to get just the access mode. Some room is left for more.
    // POSIX also defines O_SEARCH and O_EXEC that are not available
    // everywhere.
    PAL_O_ACCESS_MODE_MASK = 0x000F,

    // Flags (combineable)
    // These numeric values are not defined by POSIX and vary across targets.
    PAL_O_CLOEXEC          = 0x0010, // Close-on-exec
    PAL_O_CREAT            = 0x0020, // Create file if it doesn't already exist
    PAL_O_EXCL             = 0x0040, // When combined with CREAT, fails if file already exists
    PAL_O_TRUNC            = 0x0080, // Truncate file to length 0 if it already exists
    PAL_O_SYNC             = 0x0100, // Block writes call will block until physically written
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

/**
 * Open or create a file or device. Implemented as shim to open(2).
 *
 * Returns file descriptor or -1 for failure. Sets errno on failure.
 */
extern "C"
int32_t Open(
    const char* path,
    int32_t oflag,
    int32_t mode);

/**
 * Close a file descriptor. Implemented as shim to open(2).
 *
 * Returns 0 for success, -1 for failure. Sets errno on failure.
 */
extern "C"
int32_t Close(
    int32_t fileDescriptor);
    
/**
 * Delete an entry from the file system. Implemented as shim to unlink(2).
 *
 * Returns 0 for success, -1 for failure. Sets errno on failure.
 */
extern "C"
int32_t Unlink(
    const char* path);
    
/**
 * Open or create a shared memory object. Implemented as shim to shm_open(3).
 *
 * Returns file descriptor or -1 on fiailure. Sets errno on failure.
 */
extern "C"
int32_t ShmOpen(
     const char* name,
     int32_t oflag,
     int32_t mode);
     
/**
 * Unlink a shared memory object. Implemented as shim to shm_unlink(3).
 *
 * Returns 0 for success, -1 for failure. Sets errno on failure.
 */
extern "C"
int32_t ShmUnlink(
     const char* name);
