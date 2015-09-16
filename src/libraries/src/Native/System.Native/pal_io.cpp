// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_config.h"
#include "pal_io.h"
#include "pal_utilities.h"

#include <assert.h>
#include <errno.h>
#include <fcntl.h>
#include <fnmatch.h>
#include <poll.h>
#include <stdio.h>
#include <stdlib.h>
#include <sys/mman.h>
#include <sys/stat.h>
#include <sys/types.h>
#include <sys/file.h>
#include <syslog.h>
#include <unistd.h>

#if HAVE_STAT64
#define stat_ stat64
#define fstat_ fstat64
#define lstat_ lstat64
#else
#define stat_ stat
#define fstat_ fstat
#define lstat_ lstat
#endif

// These numeric values are specified by POSIX.
// Validate that our definitions match.
static_assert(PAL_S_IRWXU == S_IRWXU, "");
static_assert(PAL_S_IRUSR == S_IRUSR, "");
static_assert(PAL_S_IWUSR == S_IWUSR, "");
static_assert(PAL_S_IXUSR == S_IXUSR, "");
static_assert(PAL_S_IRWXG == S_IRWXG, "");
static_assert(PAL_S_IRGRP == S_IRGRP, "");
static_assert(PAL_S_IWGRP == S_IWGRP, "");
static_assert(PAL_S_IXGRP == S_IXGRP, "");
static_assert(PAL_S_IRWXO == S_IRWXO, "");
static_assert(PAL_S_IROTH == S_IROTH, "");
static_assert(PAL_S_IWOTH == S_IWOTH, "");
static_assert(PAL_S_IXOTH == S_IXOTH, "");
static_assert(PAL_S_ISUID == S_ISUID, "");
static_assert(PAL_S_ISGID == S_ISGID, "");

// These numeric values are not specified by POSIX, but the values
// are common to our current targets.  If these static asserts fail,
// ConvertFileStatus needs to be updated to twiddle mode bits
// accordingly.
static_assert(PAL_S_IFMT == S_IFMT, "");
static_assert(PAL_S_IFIFO == S_IFIFO, "");
static_assert(PAL_S_IFCHR == S_IFCHR, "");
static_assert(PAL_S_IFDIR == S_IFDIR, "");
static_assert(PAL_S_IFREG == S_IFREG, "");
static_assert(PAL_S_IFLNK == S_IFLNK, "");

// Validate that our enum for inode types is the same as what is
// declared by the dirent.h header on the local system.
static_assert(PAL_DT_UNKNOWN == DT_UNKNOWN, "");
static_assert(PAL_DT_FIFO == DT_FIFO, "");
static_assert(PAL_DT_CHR == DT_CHR, "");
static_assert(PAL_DT_DIR == DT_DIR, "");
static_assert(PAL_DT_BLK == DT_BLK, "");
static_assert(PAL_DT_REG == DT_REG, "");
static_assert(PAL_DT_LNK == DT_LNK, "");
static_assert(PAL_DT_SOCK == DT_SOCK, "");
static_assert(PAL_DT_WHT == DT_WHT, "");

// Validate that our Lock enum value are correct for the platform
static_assert(PAL_LOCK_SH == LOCK_SH, "");
static_assert(PAL_LOCK_EX == LOCK_EX, "");
static_assert(PAL_LOCK_NB == LOCK_NB, "");
static_assert(PAL_LOCK_UN == LOCK_UN, "");

// Validate our AccessMode enum values are correct for the platform
static_assert(PAL_F_OK == F_OK, "");
static_assert(PAL_X_OK == X_OK, "");
static_assert(PAL_W_OK == W_OK, "");
static_assert(PAL_R_OK == R_OK, "");

// Validate our SeekWhence enum values are correct for the platform
static_assert(PAL_SEEK_SET == SEEK_SET, "");
static_assert(PAL_SEEK_CUR == SEEK_CUR, "");
static_assert(PAL_SEEK_END == SEEK_END, "");

// Validate our PollFlags enum values are correct for the platform
static_assert(PAL_POLLIN == POLLIN, "");
static_assert(PAL_POLLOUT == POLLOUT, "");
static_assert(PAL_POLLERR == POLLERR, "");
static_assert(PAL_POLLHUP == POLLHUP, "");
static_assert(PAL_POLLNVAL == POLLNVAL, "");

// Validate our FileAdvice enum values are correct for the platform
#if HAVE_POSIX_ADVISE
static_assert(PAL_POSIX_FADV_NORMAL == POSIX_FADV_NORMAL, "");
static_assert(PAL_POSIX_FADV_RANDOM == POSIX_FADV_RANDOM, "");
static_assert(PAL_POSIX_FADV_SEQUENTIAL == POSIX_FADV_SEQUENTIAL, "");
static_assert(PAL_POSIX_FADV_WILLNEED == POSIX_FADV_WILLNEED, "");
static_assert(PAL_POSIX_FADV_DONTNEED == POSIX_FADV_DONTNEED, "");
static_assert(PAL_POSIX_FADV_NOREUSE == POSIX_FADV_NOREUSE, "");
#endif

static void ConvertFileStatus(const struct stat_& src, FileStatus* dst)
{
    dst->Flags = FILESTATUS_FLAGS_NONE;
    dst->Mode = static_cast<int32_t>(src.st_mode);
    dst->Uid = src.st_uid;
    dst->Gid = src.st_gid;
    dst->Size = src.st_size;
    dst->ATime = src.st_atime;
    dst->MTime = src.st_mtime;
    dst->CTime = src.st_ctime;

#if HAVE_STAT_BIRTHTIME
    dst->BirthTime = src.st_birthtime;
    dst->Flags |= FILESTATUS_FLAGS_HAS_BIRTHTIME;
#else
    dst->BirthTime = 0;
#endif
}

extern "C" int32_t Stat(const char* path, FileStatus* output)
{
    struct stat_ result;
    int ret = stat_(path, &result);

    if (ret == 0)
    {
        ConvertFileStatus(result, output);
    }

    return ret;
}

extern "C" int32_t FStat(int32_t fd, FileStatus* output)
{
    struct stat_ result;
    int ret = fstat_(fd, &result);

    if (ret == 0)
    {
        ConvertFileStatus(result, output);
    }

    return ret;
}

extern "C" int32_t LStat(const char* path, FileStatus* output)
{
    struct stat_ result;
    int ret = lstat_(path, &result);

    if (ret == 0)
    {
        ConvertFileStatus(result, output);
    }

    return ret;
}

static int32_t ConvertOpenFlags(int32_t flags)
{
    int32_t ret;
    switch (flags & PAL_O_ACCESS_MODE_MASK)
    {
        case PAL_O_RDONLY:
            ret = O_RDONLY;
            break;
        case PAL_O_RDWR:
            ret = O_RDWR;
            break;
        case PAL_O_WRONLY:
            ret = O_WRONLY;
            break;
        default:
            assert(false && "Unknown Open access mode.");
            return -1;
    }

    if (flags & ~(PAL_O_ACCESS_MODE_MASK | PAL_O_CLOEXEC | PAL_O_CREAT | PAL_O_EXCL | PAL_O_TRUNC | PAL_O_SYNC))
    {
        assert(false && "Unknown Open flag.");
        return -1;
    }

    if (flags & PAL_O_CLOEXEC)
        ret |= O_CLOEXEC;
    if (flags & PAL_O_CREAT)
        ret |= O_CREAT;
    if (flags & PAL_O_EXCL)
        ret |= O_EXCL;
    if (flags & PAL_O_TRUNC)
        ret |= O_TRUNC;
    if (flags & PAL_O_SYNC)
        ret |= O_SYNC;

    assert(ret != -1);
    return ret;
}

extern "C" int32_t Open(const char* path, int32_t flags, int32_t mode)
{
    flags = ConvertOpenFlags(flags);
    if (flags == -1)
    {
        errno = EINVAL;
        return -1;
    }

    return open(path, flags, static_cast<mode_t>(mode));
}

extern "C" int32_t Close(int32_t fd)
{
    return close(fd);
}

extern "C" int32_t Unlink(const char* path)
{
    return unlink(path);
}

extern "C" int32_t ShmOpen(const char* name, int32_t flags, int32_t mode)
{
#if HAVE_SHM_OPEN_THAT_WORKS_WELL_ENOUGH_WITH_MMAP
    flags = ConvertOpenFlags(flags);
    if (flags == -1)
    {
        errno = EINVAL;
        return -1;
    }

    return shm_open(name, flags, static_cast<mode_t>(mode));
#else
    (void)name, (void)flags, (void)mode;
    errno = ENOTSUP;
    return -1;
#endif
}

extern "C" int32_t ShmUnlink(const char* name)
{
    return shm_unlink(name);
}

static void ConvertDirent(const dirent& entry, DirectoryEntry* outputEntry)
{
    // We use Marshal.PtrToStringAnsi on the managed side, which takes a pointer to
    // the start of the unmanaged string. Give the caller back a pointer to the
    // location of the start of the string that exists in their own byte buffer.
    outputEntry->Name = entry.d_name;
    outputEntry->InodeType = static_cast<NodeType>(entry.d_type);

#if HAVE_DIRENT_NAME_LEN
    outputEntry->NameLength = entry.d_namlen;
#else
    outputEntry->NameLength = -1; // sentinel value to mean we have to walk to find the first \0
#endif
}

extern "C" int32_t GetDirentSize()
{
    // dirent should be under 2k in size
    static_assert(sizeof(dirent) < 2048, "");
    return sizeof(dirent);
}

// To reduce the number of string copies, this function calling pattern works as follows:
// 1) The managed code calls GetDirentSize() to get the platform-specific
//    size of the dirent struct.
// 2) The managed code creates a byte[] buffer of the size of the native dirent
//    and passes a pointer to this buffer to this function.
// 3) This function passes input byte[] buffer to the OS to fill with dirent data
//    which makes the 1st strcpy.
// 4) The ConvertDirent function will set a pointer to the start of the inode name
//    in the byte[] buffer so the managed code and find it and copy it out of the
//    buffer into a managed string that the caller of the framework can use, making
//    the 2nd and final strcpy.
extern "C" int32_t ReadDirR(DIR* dir, void* buffer, int32_t bufferSize, DirectoryEntry* outputEntry)
{
    assert(buffer != nullptr);
    assert(dir != nullptr);
    assert(outputEntry != nullptr);

    if (bufferSize < static_cast<int32_t>(sizeof(dirent)))
    {
        assert(false && "Buffer size too small; use GetDirentSize to get required buffer size");
        return ERANGE;
    }

    dirent* result = nullptr;
    dirent* entry = static_cast<dirent*>(buffer);
    int error = readdir_r(dir, entry, &result);

    // positive error number returned -> failure
    if (error != 0)
    {
        assert(error > 0);
        *outputEntry = {}; // managed out param must be initialized
        return error;
    }

    // 0 returned with null result -> end-of-stream
    if (result == nullptr)
    {
        *outputEntry = {}; // managed out param must be initialized
        return -1;         // shim convention for end-of-stream
    }

    // 0 returned with non-null result (guaranteed to be set to entry arg) -> success
    assert(result == entry);
    ConvertDirent(*entry, outputEntry);
    return 0;
}

extern "C" DIR* OpenDir(const char* path)
{
    return opendir(path);
}

extern "C" int32_t CloseDir(DIR* dir)
{
    return closedir(dir);
}

extern "C" int32_t Pipe(int32_t pipeFds[2], int32_t flags)
{
    switch (flags)
    {
        case 0:
            break;
        case PAL_O_CLOEXEC:
            flags = O_CLOEXEC;
            break;
        default:
            assert(false && "Unknown flag.");
            errno = EINVAL;
            return -1;
    }

#if HAVE_PIPE2
    return pipe2(pipeFds, flags);
#else
    return pipe(pipeFds);         // CLOEXEC intentionally ignored on platforms without pipe2.
#endif
}

extern "C" int32_t FcntlCanGetSetPipeSz()
{
#if defined(F_GETPIPE_SZ) && defined(F_SETPIPE_SZ)
    return true;
#else
    return false;
#endif
}

extern "C" int32_t FcntlGetPipeSz(int32_t fd)
{
#ifdef F_GETPIPE_SZ
    return fcntl(fd, F_GETPIPE_SZ);
#else
    (void)fd;
    errno = ENOTSUP;
    return -1;
#endif
}

extern "C" int32_t FcntlSetPipeSz(int32_t fd, int32_t size)
{
#ifdef F_SETPIPE_SZ
    return fcntl(fd, F_SETPIPE_SZ, size);
#else
    (void)fd, (void)size;
    errno = ENOTSUP;
    return -1;
#endif
}

extern "C" int32_t MkDir(const char* path, int32_t mode)
{
    return mkdir(path, static_cast<mode_t>(mode));
}

extern "C" int32_t ChMod(const char* path, int32_t mode)
{
    return chmod(path, static_cast<mode_t>(mode));
}

extern "C" int32_t MkFifo(const char* path, int32_t mode)
{
    return mkfifo(path, static_cast<mode_t>(mode));
}

extern "C" int32_t FSync(int32_t fd)
{
    return fsync(fd);
}

extern "C" int32_t FLock(int32_t fd, LockOperations operation)
{
    return flock(fd, operation);
}

extern "C" int32_t ChDir(const char* path)
{
    return chdir(path);
}

extern "C" int32_t Access(const char* path, AccessMode mode)
{
    return access(path, mode);
}

extern "C" int32_t FnMatch(const char* pattern, const char* path, FnMatchFlags flags)
{
    return fnmatch(pattern, path, flags);
}

extern "C" int64_t LSeek(int32_t fd, int64_t offset, SeekWhence whence)
{
    return lseek(fd, offset, whence);
}

extern "C" int32_t Link(const char* source, const char* linkTarget)
{
    return link(source, linkTarget);
}

extern "C" int32_t MksTemps(char* pathTemplate, int32_t suffixLength)
{
    return mkstemps(pathTemplate, suffixLength);
}

static int32_t ConvertMMapProtection(int32_t protection)
{
    if (protection == PAL_PROT_NONE)
        return PROT_NONE;

    if (protection & ~(PAL_PROT_READ | PAL_PROT_WRITE | PAL_PROT_EXEC))
    {
        assert(false && "Unknown protection.");
        return -1;
    }

    int32_t ret = 0;
    if (protection & PAL_PROT_READ)
        ret |= PROT_READ;
    if (protection & PAL_PROT_WRITE)
        ret |= PROT_WRITE;
    if (protection & PAL_PROT_EXEC)
        ret |= PROT_EXEC;

    assert(ret != -1);
    return ret;
}

static int32_t ConvertMMapFlags(int32_t flags)
{
    if (flags & ~(PAL_MAP_SHARED | PAL_MAP_PRIVATE | PAL_MAP_ANONYMOUS))
    {
        assert(false && "Unknown MMap flag.");
        return -1;
    }

    int32_t ret = 0;
    if (flags & PAL_MAP_PRIVATE)
        ret |= MAP_PRIVATE;
    if (flags & PAL_MAP_SHARED)
        ret |= MAP_SHARED;
    if (flags & PAL_MAP_ANONYMOUS)
        ret |= MAP_ANON;

    assert(ret != -1);
    return ret;
}

static int32_t ConvertMSyncFlags(int32_t flags)
{
    if (flags & ~(PAL_MS_SYNC | PAL_MS_ASYNC | PAL_MS_INVALIDATE))
    {
        assert(false && "Unknown MSync flag.");
        return -1;
    }

    int32_t ret = 0;
    if (flags & PAL_MS_SYNC)
        ret |= MS_SYNC;
    if (flags & PAL_MS_ASYNC)
        ret |= MS_ASYNC;
    if (flags & PAL_MS_INVALIDATE)
        ret |= MS_INVALIDATE;

    assert(ret != -1);
    return ret;
}

extern "C" void* MMap(void* address,
                      uint64_t length,
                      int32_t protection, // bitwise OR of PAL_PROT_*
                      int32_t flags,      // bitwise OR of PAL_MAP_*, but PRIVATE and SHARED are mutually exclusive.
                      int32_t fd,
                      int64_t offset)
{
    if (length > SIZE_MAX)
    {
        errno = ERANGE;
        return nullptr;
    }

    protection = ConvertMMapProtection(protection);
    flags = ConvertMMapFlags(flags);

    if (flags == -1 || protection == -1)
    {
        errno = EINVAL;
        return nullptr;
    }

    void* ret = mmap(address, static_cast<size_t>(length), protection, flags, fd, offset);
    if (ret == MAP_FAILED)
    {
        return nullptr;
    }

    assert(ret != nullptr);
    return ret;
}

extern "C" int32_t MUnmap(void* address, uint64_t length)
{
    if (length > SIZE_MAX)
    {
        errno = ERANGE;
        return -1;
    }

    return munmap(address, static_cast<size_t>(length));
}

extern "C" int32_t MAdvise(void* address, uint64_t length, MemoryAdvice advice)
{
    if (length > SIZE_MAX)
    {
        errno = ERANGE;
        return -1;
    }

    switch (advice)
    {
        case PAL_MADV_DONTFORK:
#ifdef MADV_DONTFORK
            return madvise(address, static_cast<size_t>(length), MADV_DONTFORK);
#else
            (void)address, (void)length, (void)advice;
            errno = ENOTSUP;
            return -1;
#endif
    }

    assert(false && "Unknown MemoryAdvice");
    errno = EINVAL;
    return -1;
}

extern "C" int32_t MLock(void* address, uint64_t length)
{
    if (length > SIZE_MAX)
    {
        errno = ERANGE;
        return -1;
    }

    return mlock(address, static_cast<size_t>(length));
}

extern "C" int32_t MUnlock(void* address, uint64_t length)
{
    if (length > SIZE_MAX)
    {
        errno = ERANGE;
        return -1;
    }

    return munlock(address, static_cast<size_t>(length));
}

extern "C" int32_t MProtect(void* address, uint64_t length, int32_t protection)
{
    if (length > SIZE_MAX)
    {
        errno = ERANGE;
        return -1;
    }

    protection = ConvertMMapProtection(protection);
    if (protection == -1)
    {
        errno = EINVAL;
        return -1;
    }

    return mprotect(address, static_cast<size_t>(length), protection);
}

extern "C" int32_t MSync(void* address, uint64_t length, int32_t flags)
{
    if (length > SIZE_MAX)
    {
        errno = ERANGE;
        return -1;
    }

    flags = ConvertMSyncFlags(flags);
    if (flags == -1)
    {
        errno = EINVAL;
        return -1;
    }

    return msync(address, static_cast<size_t>(length), flags);
}

extern "C" int64_t SysConf(SysConfName name)
{
    switch (name)
    {
        case PAL_SC_CLK_TCK:
            return sysconf(_SC_CLK_TCK);
        case PAL_SC_PAGESIZE:
            return sysconf(_SC_PAGESIZE);
    }

    assert(false && "Unknown SysConfName");
    errno = EINVAL;
    return -1;
}

extern "C" int32_t FTruncate(int32_t fd, int64_t length)
{
    return ftruncate(fd, length);
}

void ConvertPollFDPalToPlatform(const PollFD& pal, pollfd& native)
{
    native.fd = pal.FD;
    native.events = pal.Events;
    native.revents = pal.REvents;
}

static void ConvertPollFDPlatformToPal(const pollfd& native, PollFD& pal)
{
    pal.FD = native.fd;
    pal.Events = native.events;
    pal.REvents = native.revents;
}

extern "C" int32_t Poll(PollFD* pollData, uint32_t numberOfPollFds, int32_t timeout)
{
    assert(numberOfPollFds <= 2);

    if (numberOfPollFds > 2)
        return ERANGE;

    // Convert our standardized pollfd to the native one
    pollfd fds[2];
    for (uint32_t i = 0; i < numberOfPollFds; i++)
    {
        ConvertPollFDPalToPlatform(pollData[i], fds[i]);
    }

    // Call poll with the native pollfd and, if necessary, convert the results back to our standardized pollfd
    int32_t result = poll(fds, numberOfPollFds, timeout);
    if (result > 0) // We only have result data if the result is positive
    {
        for (uint32_t i = 0; i < numberOfPollFds; i++)
        {
            ConvertPollFDPlatformToPal(fds[i], pollData[i]);
        }
    }

    return result;
}

extern "C" int32_t PosixFAdvise(int32_t fd, int64_t offset, int64_t length, FileAdvice advice)
{
#if HAVE_POSIX_ADVISE
    return posix_fadvise(fd, offset, length, advice);
#else
    // Not supported on this platform; however, we don't want to #error here since
    // currently, this isn't used on platforms where it isn't supported.
    (void)fd, (void)offset, (void)length, (void)advice;
    return ENOTSUP;
#endif
}

extern "C" int32_t Read(int32_t fd, void* buffer, int32_t bufferSize)
{
    assert(buffer != nullptr || bufferSize == 0);
    assert(bufferSize >= 0);

    if (bufferSize < 0)
    {
        errno = EINVAL;
        return -1;
    }

    ssize_t count = read(fd, buffer, UnsignedCast(bufferSize));
    assert(count >= -1 && count <= bufferSize);
    return static_cast<int32_t>(count);
}

extern "C" int32_t ReadLink(const char* path, char* buffer, int32_t bufferSize)
{
    assert(buffer != nullptr || bufferSize == 0);
    assert(bufferSize >= 0);

    if (bufferSize < 0)
    {
        errno = EINVAL;
        return -1;
    }

    ssize_t count = readlink(path, buffer, static_cast<size_t>(bufferSize));
    assert(count >= -1 && count <= bufferSize);
    return static_cast<int32_t>(count);
}

extern "C" int32_t Rename(const char* oldPath, const char* newPath)
{
    return rename(oldPath, newPath);
}

extern "C" int32_t RmDir(const char* path)
{
    return rmdir(path);
}

extern "C" void Sync()
{
    sync();
}

extern "C" int32_t Write(int32_t fd, const void* buffer, int32_t bufferSize)
{
    assert(buffer != nullptr || bufferSize == 0);
    assert(bufferSize >= 0);

    if (bufferSize < 0)
    {
        errno = ERANGE;
        return -1;
    }

    ssize_t count = write(fd, buffer, UnsignedCast(bufferSize));
    assert(count >= -1 && count <= bufferSize);
    return static_cast<int32_t>(count);
}
