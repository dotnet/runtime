// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "pal_types.h"
#include <stdio.h>
#include <dirent.h>
#include <sys/types.h>

/**
 * File status returned by Stat or FStat.
 */
struct FileStatus
{
    int32_t Flags;     // flags for testing if some members are present (see FileStatusFlags)
    int32_t Mode;      // file mode (see S_I* constants above for bit values)
    uint32_t Uid;      // user ID of owner
    uint32_t Gid;      // group ID of owner
    int64_t Size;      // total size, in bytes
    int64_t ATime;     // time of last access
    int64_t MTime;     // time of last modification
    int64_t CTime;     // time of last status change
    int64_t BirthTime; // time the file was created
};

/************
 * The values below in the header are fixed and correct for managed callers to use forever.
 * We must never change them. The implementation must either static_assert that they are equal
 * to the native equivalent OR convert them appropriately.
 */

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
    PAL_S_IFMT = 0xF000,  // Type of file (apply as mask to FileStatus.Mode and one of S_IF*)
    PAL_S_IFIFO = 0x1000, // FIFO (named pipe)
    PAL_S_IFCHR = 0x2000, // Character special
    PAL_S_IFDIR = 0x4000, // Directory
    PAL_S_IFREG = 0x8000, // Regular file
    PAL_S_IFLNK = 0xA000, // Symbolic link
    PAL_S_IFSOCK = 0xC000, // Socket
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
    PAL_O_RDONLY = 0x0000, // Open for read-only
    PAL_O_WRONLY = 0x0001, // Open for write-only
    PAL_O_RDWR = 0x0002,   // Open for read-write

    // Mask to get just the access mode. Some room is left for more.
    // POSIX also defines O_SEARCH and O_EXEC that are not available
    // everywhere.
    PAL_O_ACCESS_MODE_MASK = 0x000F,

    // Flags (combinable)
    // These numeric values are not defined by POSIX and vary across targets.
    PAL_O_CLOEXEC = 0x0010, // Close-on-exec
    PAL_O_CREAT = 0x0020,   // Create file if it doesn't already exist
    PAL_O_EXCL = 0x0040,    // When combined with CREAT, fails if file already exists
    PAL_O_TRUNC = 0x0080,   // Truncate file to length 0 if it already exists
    PAL_O_SYNC = 0x0100,    // Block writes call will block until physically written
};

/**
 * Constants for interpreting FileStatus.Flags.
 */
enum
{
    FILESTATUS_FLAGS_NONE = 0,
    FILESTATUS_FLAGS_HAS_BIRTHTIME = 1,
};

/**
 * Constants from dirent.h for the inode type returned from readdir variants
 */
enum NodeType : int32_t
{
    PAL_DT_UNKNOWN = 0, // Unknown file type
    PAL_DT_FIFO = 1,    // Named Pipe
    PAL_DT_CHR = 2,     // Character Device
    PAL_DT_DIR = 4,     // Directory
    PAL_DT_BLK = 6,     // Block Device
    PAL_DT_REG = 8,     // Regular file
    PAL_DT_LNK = 10,    // Symlink
    PAL_DT_SOCK = 12,   // Socket
    PAL_DT_WHT = 14     // BSD Whiteout
};

/**
 * Constants from sys/file.h for lock types
 */
enum LockOperations : int32_t
{
    PAL_LOCK_SH = 1, /* shared lock */
    PAL_LOCK_EX = 2, /* exclusive lock */
    PAL_LOCK_NB = 4, /* don't block when locking*/
    PAL_LOCK_UN = 8, /* unlock */
};

/**
 * Constants for changing the access permissions of a path
 */
enum AccessMode : int32_t
{
    PAL_F_OK = 0, /* Check for existence */
    PAL_X_OK = 1, /* Check for execute */
    PAL_W_OK = 2, /* Check for write */
    PAL_R_OK = 4, /* Check for read */
};

/**
 * Flags to pass to fnmatch for what type of pattern matching to do
 */
enum FnMatchFlags : int32_t
{
    PAL_FNM_NONE = 0,
};

/**
 * Constants passed to lseek telling the OS where to seek from
 */
enum SeekWhence : int32_t
{
    PAL_SEEK_SET = 0, /* seek from the beginning of the stream */
    PAL_SEEK_CUR = 1, /* seek from the current position */
    PAL_SEEK_END = 2, /* seek from the end of the stream, wrapping if necessary */
};

/**
 * Constants for protection argument to MMap or MProtect.
 */
enum
{
    PAL_PROT_NONE = 0,  // pages may not be accessed (unless combined with one of below)
    PAL_PROT_READ = 1,  // pages may be read
    PAL_PROT_WRITE = 2, // pages may be written
    PAL_PROT_EXEC = 4,  // pages may be executed
};

/**
 * Constants for flags argument passed to MMap.
 */
enum
{
    // shared/private are mutually exclusive
    PAL_MAP_SHARED = 0x01,  // shared mapping
    PAL_MAP_PRIVATE = 0x02, // private copy-on-write-mapping

    PAL_MAP_ANONYMOUS = 0x10, // mapping is not backed by any file
};

/**
 * Constants for flags argument passed to MSync.
 */
enum
{
    // sync/async are mutually exclusive
    PAL_MS_ASYNC = 0x01, // request sync, but don't block on completion
    PAL_MS_SYNC = 0x02,  // block until sync completes

    PAL_MS_INVALIDATE = 0x10, // cause other mappings of the same file to be updated
};

/**
 * Advice argument to MAdvise.
 */
enum MemoryAdvice : int32_t
{
    PAL_MADV_DONTFORK = 1, // don't map pages in to forked process
};

/**
 * Name argument to SysConf.
 */
enum SysConfName : int32_t
{
    PAL_SC_CLK_TCK = 1,  // Number of clock ticks per second
    PAL_SC_PAGESIZE = 2, // Size of a page in bytes,
    PAL_SC_NPROCESSORS_ONLN = 3, // Number of active processors
};

/**
 * Constants passed to and from poll describing what to poll for and what
 * kind of data was received from poll.
 */
enum PollEvents : int16_t
{
    PAL_POLLIN = 0x0001,   /* non-urgent readable data available */
    PAL_POLLPRI = 0x0002,  /* urgent readable data available */
    PAL_POLLOUT = 0x0004,  /* data can be written without blocked */
    PAL_POLLERR = 0x0008,  /* an error occurred */
    PAL_POLLHUP = 0x0010,  /* the file descriptor hung up */
    PAL_POLLNVAL = 0x0020, /* the requested events were invalid */
};

/**
 * Constants passed to posix_advise to give hints to the kernel about the type of I/O
 * operations that will occur.
 */
enum FileAdvice : int32_t
{
    PAL_POSIX_FADV_NORMAL = 0,     /* no special advice, the default value */
    PAL_POSIX_FADV_RANDOM = 1,     /* random I/O access */
    PAL_POSIX_FADV_SEQUENTIAL = 2, /* sequential I/O access */
    PAL_POSIX_FADV_WILLNEED = 3,   /* will need specified pages */
    PAL_POSIX_FADV_DONTNEED = 4,   /* don't need the specified pages */
    PAL_POSIX_FADV_NOREUSE = 5,    /* data will only be acessed once */
};

/**
 * Our intermediate dirent struct that only gives back the data we need
 */
struct DirectoryEntry
{
    const char* Name;   // Address of the name of the inode
    int32_t NameLength; // Length (in chars) of the inode name
    NodeType InodeType; // The inode type as described in the NodeType enum
};

/**
 * Our intermediate pollfd struct to normalize the data types
 */
struct PollEvent
{
    int32_t    FileDescriptor;  // The file descriptor to poll
    PollEvents Events;          // The events to poll for
    PollEvents TriggeredEvents; // The events that triggered the poll
};

/**
* Constants passed in the mask argument of INotifyAddWatch which identify inotify events.
*/
enum NotifyEvents : int32_t
{
    PAL_IN_ACCESS = 0x00000001,
    PAL_IN_MODIFY = 0x00000002,
    PAL_IN_ATTRIB = 0x00000004,
    PAL_IN_MOVED_FROM = 0x00000040,
    PAL_IN_MOVED_TO = 0x00000080,
    PAL_IN_CREATE = 0x00000100,
    PAL_IN_DELETE = 0x00000200,
    PAL_IN_Q_OVERFLOW = 0x00004000,
    PAL_IN_IGNORED = 0x00008000,
    PAL_IN_ONLYDIR = 0x01000000,
    PAL_IN_DONT_FOLLOW = 0x02000000,
    PAL_IN_EXCL_UNLINK = 0x04000000,
    PAL_IN_ISDIR = 0x40000000,
};

/**
 * Get file status from a descriptor. Implemented as shim to fstat(2).
 *
 * Returns 0 for success, -1 for failure. Sets errno on failure.
 */
extern "C" int32_t SystemNative_FStat(intptr_t fd, FileStatus* output);

/**
 * Get file status from a full path. Implemented as shim to stat(2).
 *
 * Returns 0 for success, -1 for failure. Sets errno on failure.
 */
extern "C" int32_t SystemNative_Stat(const char* path, FileStatus* output);

/**
 * Get file stats from a full path. Implemented as shim to lstat(2).
 *
 * Returns 0 for success, -1 for failure. Sets errno on failure.
 */
extern "C" int32_t SystemNative_LStat(const char* path, FileStatus* output);

/**
 * Open or create a file or device. Implemented as shim to open(2).
 *
 * Returns file descriptor or -1 for failure. Sets errno on failure.
 */
extern "C" intptr_t SystemNative_Open(const char* path, int32_t flags, int32_t mode);

/**
 * Close a file descriptor. Implemented as shim to open(2).
 *
 * Returns 0 for success, -1 for failure. Sets errno on failure.
 */
extern "C" int32_t SystemNative_Close(intptr_t fd);

/**
 * Duplicates a file descriptor.
 *
 * Returns the duplication descriptor for success, -1 for failure. Sets errno on failure.
 */
extern "C" intptr_t SystemNative_Dup(intptr_t oldfd);

/**
 * Delete an entry from the file system. Implemented as shim to unlink(2).
 *
 * Returns 0 for success, -1 for failure. Sets errno on failure.
 */
extern "C" int32_t SystemNative_Unlink(const char* path);

/**
 * Open or create a shared memory object. Implemented as shim to shm_open(3).
 *
 * Returns file descriptor or -1 on fiailure. Sets errno on failure.
 */
extern "C" intptr_t SystemNative_ShmOpen(const char* name, int32_t flags, int32_t mode);

/**
 * Unlink a shared memory object. Implemented as shim to shm_unlink(3).
 *
 * Returns 0 for success, -1 for failure. Sets errno on failure.
 */
extern "C" int32_t SystemNative_ShmUnlink(const char* name);

/**
 * Returns the size of the dirent struct on the current architecture
 */
extern "C" int32_t SystemNative_GetDirentSize();

/**
 * Re-entrant readdir that will retrieve the next dirent from the directory stream pointed to by dir.
 *
 * Returns 0 when data is retrieved; returns -1 when end-of-stream is reached; returns an error code on failure
 */
extern "C" int32_t SystemNative_ReadDirR(DIR* dir, void* buffer, int32_t bufferSize, DirectoryEntry* outputEntry);

/**
 * Returns a DIR struct containing info about the current path or NULL on failure; sets errno on fail.
 */
extern "C" DIR* SystemNative_OpenDir(const char* path);

/**
 * Closes the directory stream opened by opendir and returns 0 on success. On fail, -1 is returned and errno is set
 */
extern "C" int32_t SystemNative_CloseDir(DIR* dir);

/**
 * Creates a pipe. Implemented as shim to pipe(2) or pipe2(2) if available.
 * Flags are ignored if pipe2 is not available.
 *
 * Returns 0 for success, -1 for failure. Sets errno on failure.
 */
extern "C" int32_t SystemNative_Pipe(int32_t pipefd[2], // [out] pipefds[0] gets read end, pipefd[1] gets write end.
                        int32_t flags);    // 0 for defaults or PAL_O_CLOEXEC for close-on-exec

// NOTE: Rather than a general fcntl shim, we opt to export separate functions
// for each command. This allows use to have strongly typed arguments and saves
// complexity around converting command codes.

/**
 * Sets the O_CLOEXEC flag on a file descriptor.
 *
 * Returns 0 for success; -1 for failure. Sets errno for failure.
 */
extern "C" int32_t SystemNative_FcntlSetCloseOnExec(intptr_t fd);

/**
 * Determines if the current platform supports getting and setting pipe capacity.
 *
 * Returns true (non-zero) if supported, false (zero) if not.
 */
extern "C" int32_t SystemNative_FcntlCanGetSetPipeSz();

/**
 * Gets the capacity of a pipe.
 *
 * Returns the capacity or -1 with errno set aprropriately on failure.
 *
 * NOTE: Some platforms do not support this operation and will always fail with errno = ENOTSUP.
 */
extern "C" int32_t SystemNative_FcntlGetPipeSz(intptr_t fd);

/**
 * Sets the capacity of a pipe.
 *
 * Returns 0 for success, -1 for failure. Sets errno for failure.
 *
 * NOTE: Some platforms do not support this operation and will always fail with errno = ENOTSUP.
 */
extern "C" int32_t SystemNative_FcntlSetPipeSz(intptr_t fd, int32_t size);

/**
 * Sets whether or not a file descriptor is non-blocking.
 *
 * Returns 0 for success, -1 for failure. Sets errno for failure.
 */
extern "C" int32_t SystemNative_FcntlSetIsNonBlocking(intptr_t fd, int32_t isNonBlocking);

/**
 * Create a directory. Implemented as a shim to mkdir(2).
 *
 * Returns 0 for success, -1 for failure. Sets errno for failure.
 */
extern "C" int32_t SystemNative_MkDir(const char* path, int32_t mode);

/**
 * Change permissions of a file. Implemented as a shim to chmod(2).
 *
 * Returns 0 for success, -1 for failure. Sets errno for failure.
 */
extern "C" int32_t SystemNative_ChMod(const char* path, int32_t mode);

/**
* Change permissions of a file. Implemented as a shim to fchmod(2).
*
* Returns 0 for success, -1 for failure. Sets errno for failure.
*/
extern "C" int32_t SystemNative_FChMod(intptr_t fd, int32_t mode);

/**
 * Flushes all modified data and attribtues of the specified File Descriptor to the storage medium.
 *
 * Returns 0 for success; on fail, -1 is returned and errno is set.
 */
extern "C" int32_t SystemNative_FSync(intptr_t fd);

/**
 * Changes the advisory lock status on a given File Descriptor
 *
 * Returns 0 on success; otherwise, -1 is returned and errno is set
 */
extern "C" int32_t SystemNative_FLock(intptr_t fd, LockOperations operation);

/**
 * Changes the current working directory to be the specified path.
 *
 * Returns 0 on success; otherwise, returns -1 and errno is set
 */
extern "C" int32_t SystemNative_ChDir(const char* path);

/**
 * Checks the access permissions of the current calling user on the specified path for the specified mode.
 *
 * Returns -1 if the path cannot be found or the if desired access is not granted and errno is set; otherwise, returns
 * 0.
 */
extern "C" int32_t SystemNative_Access(const char* path, AccessMode mode);

/**
 * Tests whether a pathname matches a specified pattern.
 *
 * Returns 0 if the string matches; returns FNM_NOMATCH if the call succeeded but the
 * string does not match; otherwise, returns a non-zero error code.
 */
extern "C" int32_t SystemNative_FnMatch(const char* pattern, const char* path, FnMatchFlags flags);

/**
 * Seek to a specified location within a seekable stream
 *
 * On success, the resulting offet, in bytes, from the beginning of the stream; otherwise,
 * returns -1 and errno is set.
 */
extern "C" int64_t SystemNative_LSeek(intptr_t fd, int64_t offset, SeekWhence whence);

/**
 * Creates a hard-link at link pointing to source.
 *
 * Returns 0 on success; otherwise, returns -1 and errno is set.
 */
extern "C" int32_t SystemNative_Link(const char* source, const char* linkTarget);

/**
 * Creates a file name that adheres to the specified template, creates the file on disk with
 * 0600 permissions, and returns an open r/w File Descriptor on the file.
 *
 * Returns a valid File Descriptor on success; otherwise, returns -1 and errno is set.
 */
extern "C" intptr_t SystemNative_MksTemps(char* pathTemplate, int32_t suffixLength);

/**
 * Map file or device into memory. Implemented as shim to mmap(2).
 *
 * Returns 0 for success, nullptr for failure. Sets errno on failure.
 *
 * Note that null failure result is a departure from underlying
 * mmap(2) using non-null sentinel.
 */
extern "C" void* SystemNative_MMap(void* address,
                      uint64_t length,
                      int32_t protection, // bitwise OR of PAL_PROT_*
                      int32_t flags,      // bitwise OR of PAL_MAP_*, but PRIVATE and SHARED are mutually exclusive.
                      intptr_t fd,
                      int64_t offset);

/**
 * Unmap file or device from memory. Implemented as shim to mmap(2).
 *
 * Returns 0 for success, -1 for failure. Sets errno on failure.
 */
extern "C" int32_t SystemNative_MUnmap(void* address, uint64_t length);

/**
 * Give advice about use of memory. Implemented as shim to madvise(2).
 *
 * Returns 0 for success, -1 for failure. Sets errno on failure.
 */
extern "C" int32_t SystemNative_MAdvise(void* address, uint64_t length, MemoryAdvice advice);

/**
 * Lock memory from being swapped out. Implemented as shim to mlock(2).
 *
 * Returns 0 for success, -1 for failure. Sets errno on failure.
 */
extern "C" int32_t SystemNative_MLock(void* address, uint64_t length);

/**
 * Unlock memory, allowing it to be swapped out. Implemented as shim to munlock(2).
 *
 * Returns 0 for success, -1 for failure. Sets errno on failure.
 */
extern "C" int32_t SystemNative_MUnlock(void* address, uint64_t length);

/**
 * Set protection on a region of memory. Implemented as shim to mprotect(2).
 *
 * Returns 0 for success, -1 for failure. Sets errno on failure.
 */
extern "C" int32_t SystemNative_MProtect(void* address, uint64_t length, int32_t protection);

/**
 * Sycnhronize a file with a memory map. Implemented as shim to mmap(2).
 *
 * Returns 0 for success, -1 for failure. Sets errno on failure.
 */
extern "C" int32_t SystemNative_MSync(void* address, uint64_t length, int32_t flags);

/**
 * Get system configuration value. Implemented as shim to sysconf(3).
 *
 * Returns configuration value.
 *
 * Sets errno to EINVAL and returns -1 if name is invalid, but make
 * note that -1 can also be a meaningful successful return value, in
 * which case errno is unchanged.
 */
extern "C" int64_t SystemNative_SysConf(SysConfName name);

/**
 * Truncate a file to given length. Implemented as shim to ftruncate(2).
 *
 * Returns 0 for success, -1 for failure. Sets errno on failure.
 */
extern "C" int32_t SystemNative_FTruncate(intptr_t fd, int64_t length);

/**
 * Examines one or more file descriptors for the specified state(s) and blocks until the state(s) occur or the timeout
 * ellapses.
 *
 * Returns an error or PAL_SUCCESS. `triggered` is set to the number of ready descriptors if any. The number of
 * triggered descriptors may be zero in the event of a timeout.
 */
extern "C" Error SystemNative_Poll(PollEvent* pollEvents, uint32_t eventCount, int32_t milliseconds, uint32_t* triggered);

/**
 * Notifies the OS kernel that the specified file will be accessed in a particular way soon; this allows the kernel to
 * potentially optimize the access pattern of the file.
 *
 * Returns 0 on success; otherwise, the error code is returned and errno is NOT set.
 */
extern "C" int32_t SystemNative_PosixFAdvise(intptr_t fd, int64_t offset, int64_t length, FileAdvice advice);

/**
* Reads a line from the provided stream.
*
* Returns the read line, or null if no line could be read.  The caller is responsible for freeing the malloc'd line.
*/
extern "C" char* SystemNative_GetLine(FILE* stream);

/**
 * Reads the number of bytes specified into the provided buffer from the specified, opened file descriptor.
 *
 * Returns the number of bytes read on success; otherwise, -1 is returned an errno is set.
 *
 * Note - on fail. the position of the stream may change depending on the platform; consult man 2 read for more info
 */
extern "C" int32_t SystemNative_Read(intptr_t fd, void* buffer, int32_t bufferSize);

/**
 * Takes a path to a symbolic link and attempts to place the link target path into the buffer. If the buffer is too
 * small, the path will be truncated. No matter what, the buffer will not be null terminated.
 *
 * Returns the number of bytes placed into the buffer on success; otherwise, -1 is returned and errno is set.
 */
extern "C" int32_t SystemNative_ReadLink(const char* path, char* buffer, int32_t bufferSize);

/**
 * Renames a file, moving to the correct destination if necessary. There are many edge cases to this call, check man 2
 * rename for more info
 *
 * Returns 0 on succes; otherwise, returns -1 and errno is set.
 */
extern "C" int32_t SystemNative_Rename(const char* oldPath, const char* newPath);

/**
 * Deletes the specified empty directory.
 *
 * Returns 0 on success; otherwise, returns -1 and errno is set.
 */
extern "C" int32_t SystemNative_RmDir(const char* path);

/**
 * Forces a write of all modified I/O buffers to their storage mediums.
 */
extern "C" void SystemNative_Sync();

/**
 * Writes the specified buffer to the provided open file descriptor
 *
 * Returns the number of bytes written on success; otherwise, returns -1 and sets errno
 */
extern "C" int32_t SystemNative_Write(intptr_t fd, const void* buffer, int32_t bufferSize);

/**
 * Copies all data from the source file descriptor to the destination file descriptor.
 *
 * Returns 0 on success; otherwise, returns -1 and sets errno.
 */
extern "C" int32_t SystemNative_CopyFile(intptr_t sourceFd, intptr_t destinationFd);

/**
* Initializes a new inotify instance and returns a file
* descriptor associated with a new inotify event queue.
*
* Returns a new file descriptor on success.
* On error, -1 is returned, and errno is set to indicate the error.
*/
extern "C" intptr_t SystemNative_INotifyInit();

/**
* Adds a new watch, or modifies an existing watch,
* for the file whose location is specified in pathname.
*
* Returns a nonnegative watch descriptor on success.
* On error -1 is returned and errno is set appropriately.
*/
extern "C" int32_t SystemNative_INotifyAddWatch(intptr_t fd, const char* pathName, uint32_t mask);

/**
* Removes the watch associated with the watch descriptor wd
* from the inotify instance associated with the file descriptor fd.
*
* Returns 0 on success, or -1 if an error occurred (in which case, errno is set appropriately).
*/
extern "C" int32_t SystemNative_INotifyRemoveWatch(intptr_t fd, int32_t wd);

/**
* Expands all symbolic links and expands all paths to return an absolute path
*
* Returns the result absolute path on success or null on error with errno set appropriately.
*/
extern "C" char* SystemNative_RealPath(const char* path);

/**
* Attempts to retrieve the ID of the process at the end of the given socket
*
* Returns 0 on success, or -1 if an error occurred (in which case, errno is set appropriately).
*/
extern "C" int32_t SystemNative_GetPeerID(intptr_t socket, uid_t* euid);

/**
* Attempts to lock/unlock the region of the file "fd" specified by the offset and length. lockType
* can be set to F_UNLCK (2) for unlock or F_WRLCK (3) for lock.
*
* Returns 0 on success, or -1 if an error occurred (in which case, errno is set appropriately).
*/
extern "C" int32_t SystemNative_LockFileRegion(intptr_t fd, int64_t offset, int64_t length, int16_t lockType);
