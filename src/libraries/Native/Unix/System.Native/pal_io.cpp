// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_config.h"
#include "pal_errno.h"
#include "pal_io.h"
#include "pal_utilities.h"
#include "pal_safecrt.h"

#include <assert.h>
#include <errno.h>
#include <fcntl.h>
#include <fnmatch.h>
#include <poll.h>
#include <stdio.h>
#include <stdlib.h>
#include <sys/mman.h>
#include <sys/stat.h>
#include <sys/time.h>
#include <sys/types.h>
#include <sys/file.h>
#include <sys/ioctl.h>
#include <sys/socket.h>
#include <syslog.h>
#include <termios.h>
#include <unistd.h>
#include <limits.h>
#if HAVE_FCOPYFILE
#include <copyfile.h>
#elif HAVE_SENDFILE
#include <sys/sendfile.h>
#endif
#if HAVE_INOTIFY
#include <sys/inotify.h>
#endif

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
static_assert(PAL_S_IFSOCK == S_IFSOCK, "");

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
static_assert(PAL_POLLPRI == POLLPRI, "");
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

// Validate our NotifyEvents enum values are correct for the platform
#if HAVE_INOTIFY
static_assert(PAL_IN_ACCESS == IN_ACCESS, "");
static_assert(PAL_IN_MODIFY == IN_MODIFY, "");
static_assert(PAL_IN_ATTRIB == IN_ATTRIB, "");
static_assert(PAL_IN_MOVED_FROM == IN_MOVED_FROM, "");
static_assert(PAL_IN_MOVED_TO == IN_MOVED_TO, "");
static_assert(PAL_IN_CREATE == IN_CREATE, "");
static_assert(PAL_IN_DELETE == IN_DELETE, "");
static_assert(PAL_IN_Q_OVERFLOW == IN_Q_OVERFLOW, "");
static_assert(PAL_IN_IGNORED == IN_IGNORED, "");
static_assert(PAL_IN_ONLYDIR == IN_ONLYDIR, "");
static_assert(PAL_IN_DONT_FOLLOW == IN_DONT_FOLLOW, "");
static_assert(PAL_IN_EXCL_UNLINK == IN_EXCL_UNLINK, "");
static_assert(PAL_IN_ISDIR == IN_ISDIR, "");
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

extern "C" int32_t SystemNative_Stat(const char* path, FileStatus* output)
{
    struct stat_ result;
    int ret;
    while (CheckInterrupted(ret = stat_(path, &result)));

    if (ret == 0)
    {
        ConvertFileStatus(result, output);
    }

    return ret;
}

extern "C" int32_t SystemNative_FStat(intptr_t fd, FileStatus* output)
{
    struct stat_ result;
    int ret;
    while (CheckInterrupted(ret = fstat_(ToFileDescriptor(fd), &result)));

    if (ret == 0)
    {
        ConvertFileStatus(result, output);
    }

    return ret;
}

extern "C" int32_t SystemNative_LStat(const char* path, FileStatus* output)
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

extern "C" intptr_t SystemNative_Open(const char* path, int32_t flags, int32_t mode)
{
    flags = ConvertOpenFlags(flags);
    if (flags == -1)
    {
        errno = EINVAL;
        return -1;
    }

    int result;
    while (CheckInterrupted(result = open(path, flags, static_cast<mode_t>(mode))));
    return result;
}

extern "C" int32_t SystemNative_Close(intptr_t fd)
{
    return close(ToFileDescriptor(fd));
}

extern "C" intptr_t SystemNative_Dup(intptr_t oldfd)
{
    int result;
    while (CheckInterrupted(result = dup(ToFileDescriptor(oldfd))));
    return result;
}

extern "C" int32_t SystemNative_Unlink(const char* path)
{
    int32_t result;
    while (CheckInterrupted(result = unlink(path)));
    return result;
}

extern "C" intptr_t SystemNative_ShmOpen(const char* name, int32_t flags, int32_t mode)
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

extern "C" int32_t SystemNative_ShmUnlink(const char* name)
{
    int32_t result;
    while (CheckInterrupted(result = shm_unlink(name)));
    return result;
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

extern "C" int32_t SystemNative_GetDirentSize()
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
// 3) This function passes input byte[] buffer to the OS to fill with dirent
//    data which makes the 1st strcpy.
// 4) The ConvertDirent function will fill DirectoryEntry outputEntry with
//    pointers from byte[] buffer.
// 5) The managed code uses DirectoryEntry outputEntry to find start of d_name
//    and the value of d_namelen, if avalable, to copy the name from
//    byte[] buffer into a managed string that the caller can use; this makes
//    the 2nd and final strcpy.
extern "C" int32_t SystemNative_ReadDirR(DIR* dir, void* buffer, int32_t bufferSize, DirectoryEntry* outputEntry)
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
#if HAVE_READDIR_R
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
#else
    errno = 0;
    result = readdir(dir);

    // 0 returned with null result -> end-of-stream
    if (result == nullptr)
    {
        *outputEntry = {}; // managed out param must be initialized

        //  kernel set errno -> failure
        if (errno != 0)
        {
            assert(errno == EBADF); // Invalid directory stream descriptor dir.
            return errno;
        }
        return -1;
    }

    assert(result->d_reclen <= bufferSize);
    memcpy_s(entry, sizeof(dirent), result, static_cast<size_t>(result->d_reclen));
#endif
    ConvertDirent(*entry, outputEntry);
    return 0;
}

extern "C" DIR* SystemNative_OpenDir(const char* path)
{
    return opendir(path);
}

extern "C" int32_t SystemNative_CloseDir(DIR* dir)
{
    return closedir(dir);
}

extern "C" int32_t SystemNative_Pipe(int32_t pipeFds[2], int32_t flags)
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

    int32_t result;
#if HAVE_PIPE2
    // If pipe2 is available, use it.  This will handle O_CLOEXEC if it was set.
    while (CheckInterrupted(result = pipe2(pipeFds, flags)));
#else
    // Otherwise, use pipe.
    while (CheckInterrupted(result = pipe(pipeFds)));

    // Then, if O_CLOEXEC was specified, use fcntl to configure the file descriptors appropriately.
    if ((flags & O_CLOEXEC) != 0 && result == 0)
    {
        while (CheckInterrupted(result = fcntl(pipeFds[0], F_SETFD, FD_CLOEXEC)));
        if (result == 0)
        {
            while (CheckInterrupted(result = fcntl(pipeFds[1], F_SETFD, FD_CLOEXEC)));
        }

        if (result != 0)
        {
            int tmpErrno = errno;
            close(pipeFds[0]);
            close(pipeFds[1]);
            errno = tmpErrno;
        }
    }
#endif
    return result;
}

extern "C" int32_t SystemNative_FcntlSetCloseOnExec(intptr_t fd)
{
    int result;
    while (CheckInterrupted(result = fcntl(ToFileDescriptor(fd), F_SETFD, FD_CLOEXEC)));
    return result;
}

extern "C" int32_t SystemNative_FcntlCanGetSetPipeSz()
{
#if defined(F_GETPIPE_SZ) && defined(F_SETPIPE_SZ)
    return true;
#else
    return false;
#endif
}

extern "C" int32_t SystemNative_FcntlGetPipeSz(intptr_t fd)
{
#ifdef F_GETPIPE_SZ
    int32_t result;
    while (CheckInterrupted(result = fcntl(ToFileDescriptor(fd), F_GETPIPE_SZ)));
    return result;
#else
    (void)fd;
    errno = ENOTSUP;
    return -1;
#endif
}

extern "C" int32_t SystemNative_FcntlSetPipeSz(intptr_t fd, int32_t size)
{
#ifdef F_SETPIPE_SZ
    int32_t result;
    while (CheckInterrupted(result = fcntl(ToFileDescriptor(fd), F_SETPIPE_SZ, size)));
    return result;
#else
    (void)fd, (void)size;
    errno = ENOTSUP;
    return -1;
#endif
}

extern "C" int32_t SystemNative_FcntlSetIsNonBlocking(intptr_t fd, int32_t isNonBlocking)
{
    int fileDescriptor = ToFileDescriptor(fd);

    int flags = fcntl(fileDescriptor, F_GETFL);
    if (flags == -1)
    {
        return -1;
    }

    if (isNonBlocking == 0)
    {
        flags &= ~O_NONBLOCK;
    }
    else
    {
        flags |= O_NONBLOCK;
    }

    return fcntl(fileDescriptor, F_SETFL, flags);
}

extern "C" int32_t SystemNative_MkDir(const char* path, int32_t mode)
{
    int32_t result;
    while (CheckInterrupted(result = mkdir(path, static_cast<mode_t>(mode))));
    return result;
}

extern "C" int32_t SystemNative_ChMod(const char* path, int32_t mode)
{
    int32_t result;
    while (CheckInterrupted(result = chmod(path, static_cast<mode_t>(mode))));
    return result;
}

extern "C" int32_t SystemNative_FChMod(intptr_t fd, int32_t mode)
{
    int32_t result;
    while (CheckInterrupted(result = fchmod(ToFileDescriptor(fd), static_cast<mode_t>(mode))));
    return result;
}

extern "C" int32_t SystemNative_FSync(intptr_t fd)
{
    int32_t result;
    while (CheckInterrupted(result = fsync(ToFileDescriptor(fd))));
    return result;
}

extern "C" int32_t SystemNative_FLock(intptr_t fd, LockOperations operation)
{
    int32_t result;
    while (CheckInterrupted(result = flock(ToFileDescriptor(fd), operation)));
    return result;
}

extern "C" int32_t SystemNative_ChDir(const char* path)
{
    int32_t result;
    while (CheckInterrupted(result = chdir(path)));
    return result;
}

extern "C" int32_t SystemNative_Access(const char* path, AccessMode mode)
{
    return access(path, mode);
}

extern "C" int32_t SystemNative_FnMatch(const char* pattern, const char* path, FnMatchFlags flags)
{
    return fnmatch(pattern, path, flags);
}

extern "C" int64_t SystemNative_LSeek(intptr_t fd, int64_t offset, SeekWhence whence)
{
    int64_t result;
    while (CheckInterrupted(result = lseek(ToFileDescriptor(fd), offset, whence)));
    return result;
}

extern "C" int32_t SystemNative_Link(const char* source, const char* linkTarget)
{
    int32_t result;
    while (CheckInterrupted(result = link(source, linkTarget)));
    return result;
}

extern "C" intptr_t SystemNative_MksTemps(char* pathTemplate, int32_t suffixLength)
{
    intptr_t result;
    while (CheckInterrupted(result = mkstemps(pathTemplate, suffixLength)));
    return  result;
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

extern "C" void* SystemNative_MMap(void* address,
                      uint64_t length,
                      int32_t protection, // bitwise OR of PAL_PROT_*
                      int32_t flags,      // bitwise OR of PAL_MAP_*, but PRIVATE and SHARED are mutually exclusive.
                      intptr_t fd,
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

    // Use ToFileDescriptorUnchecked to allow -1 to be passed for the file descriptor, since managed code explicitly uses -1
    void* ret = mmap(address, static_cast<size_t>(length), protection, flags, ToFileDescriptorUnchecked(fd), offset);
    if (ret == MAP_FAILED)
    {
        return nullptr;
    }

    assert(ret != nullptr);
    return ret;
}

extern "C" int32_t SystemNative_MUnmap(void* address, uint64_t length)
{
    if (length > SIZE_MAX)
    {
        errno = ERANGE;
        return -1;
    }

    return munmap(address, static_cast<size_t>(length));
}

extern "C" int32_t SystemNative_MAdvise(void* address, uint64_t length, MemoryAdvice advice)
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

extern "C" int32_t SystemNative_MLock(void* address, uint64_t length)
{
    if (length > SIZE_MAX)
    {
        errno = ERANGE;
        return -1;
    }

    return mlock(address, static_cast<size_t>(length));
}

extern "C" int32_t SystemNative_MUnlock(void* address, uint64_t length)
{
    if (length > SIZE_MAX)
    {
        errno = ERANGE;
        return -1;
    }

    return munlock(address, static_cast<size_t>(length));
}

extern "C" int32_t SystemNative_MProtect(void* address, uint64_t length, int32_t protection)
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

extern "C" int32_t SystemNative_MSync(void* address, uint64_t length, int32_t flags)
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

extern "C" int64_t SystemNative_SysConf(SysConfName name)
{
    switch (name)
    {
        case PAL_SC_CLK_TCK:
            return sysconf(_SC_CLK_TCK);
        case PAL_SC_PAGESIZE:
            return sysconf(_SC_PAGESIZE);
        case PAL_SC_NPROCESSORS_ONLN:
            return sysconf(_SC_NPROCESSORS_ONLN);
    }

    assert(false && "Unknown SysConfName");
    errno = EINVAL;
    return -1;
}

extern "C" int32_t SystemNative_FTruncate(intptr_t fd, int64_t length)
{
    int32_t result;
    while (CheckInterrupted(result = ftruncate(ToFileDescriptor(fd), length)));
    return result;
}

extern "C" Error SystemNative_Poll(PollEvent* pollEvents, uint32_t eventCount, int32_t milliseconds, uint32_t* triggered)
{
    if (pollEvents == nullptr || triggered == nullptr)
    {
        return PAL_EFAULT;
    }

    if (milliseconds < -1)
    {
        return PAL_EINVAL;
    }

    size_t bufferSize;
    if (!multiply_s(sizeof(pollfd), static_cast<size_t>(eventCount), &bufferSize))
    {
        return SystemNative_ConvertErrorPlatformToPal(EOVERFLOW);        
    }

    bool useStackBuffer = bufferSize <= 2048;
    pollfd* pollfds = reinterpret_cast<pollfd*>(useStackBuffer ? alloca(bufferSize) : malloc(bufferSize));
    if (pollfds == nullptr)
    {
        return PAL_ENOMEM;
    }

    for (uint32_t i = 0; i < eventCount; i++)
    {
        const PollEvent& event = pollEvents[i];
        pollfds[i] = { .fd = event.FileDescriptor, .events = event.Events, .revents = 0 };
    }

    int rv;
    while (CheckInterrupted(rv = poll(pollfds, static_cast<nfds_t>(eventCount), milliseconds)));

    if (rv < 0)
    {
        if (!useStackBuffer)
        {
            free(pollfds);
        }

        *triggered = 0;
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }

    for (uint32_t i = 0; i < eventCount; i++)
    {
        const pollfd& pfd = pollfds[i];
        assert(pfd.fd == pollEvents[i].FileDescriptor);
        assert(pfd.events == pollEvents[i].Events);

        pollEvents[i].TriggeredEvents = static_cast<PollEvents>(pfd.revents);
    }

    *triggered = static_cast<uint32_t>(rv);

    if (!useStackBuffer)
    {
        free(pollfds);
    }

    return PAL_SUCCESS;
}

extern "C" int32_t SystemNative_PosixFAdvise(intptr_t fd, int64_t offset, int64_t length, FileAdvice advice)
{
#if HAVE_POSIX_ADVISE
    int32_t result;
    while (CheckInterrupted(result = posix_fadvise(ToFileDescriptor(fd), offset, length, advice)));
    return result;
#else
    // Not supported on this platform. Caller can ignore this failure since it's just a hint.
    (void)fd, (void)offset, (void)length, (void)advice;
    return ENOTSUP;
#endif
}

extern "C" char* SystemNative_GetLine(FILE* stream)
{
    assert(stream != nullptr);

    char* lineptr = nullptr;
    size_t n = 0;
    ssize_t length = getline(&lineptr, &n, stream);
    
    return length >= 0 ? lineptr : nullptr;
}

extern "C" int32_t SystemNative_Read(intptr_t fd, void* buffer, int32_t bufferSize)
{
    assert(buffer != nullptr || bufferSize == 0);
    assert(bufferSize >= 0);

    if (bufferSize < 0)
    {
        errno = EINVAL;
        return -1;
    }

    ssize_t count;
    while (CheckInterrupted(count = read(ToFileDescriptor(fd), buffer, UnsignedCast(bufferSize))));

    assert(count >= -1 && count <= bufferSize);
    return static_cast<int32_t>(count);
}

extern "C" int32_t SystemNative_ReadLink(const char* path, char* buffer, int32_t bufferSize)
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

extern "C" int32_t SystemNative_Rename(const char* oldPath, const char* newPath)
{
    int32_t result;
    while (CheckInterrupted(result = rename(oldPath, newPath)));
    return result;
}

extern "C" int32_t SystemNative_RmDir(const char* path)
{
    int32_t result;
    while (CheckInterrupted(result = rmdir(path)));
    return result;
}

extern "C" void SystemNative_Sync()
{
    sync();
}

extern "C" int32_t SystemNative_Write(intptr_t fd, const void* buffer, int32_t bufferSize)
{
    assert(buffer != nullptr || bufferSize == 0);
    assert(bufferSize >= 0);

    if (bufferSize < 0)
    {
        errno = ERANGE;
        return -1;
    }

    ssize_t count;
    while (CheckInterrupted(count = write(ToFileDescriptor(fd), buffer, UnsignedCast(bufferSize))));

    assert(count >= -1 && count <= bufferSize);
    return static_cast<int32_t>(count);
}

#if !HAVE_FCOPYFILE
// Read all data from inFd and write it to outFd
static int32_t CopyFile_ReadWrite(int inFd, int outFd)
{
    // Allocate a buffer
    const int BufferLength = 80 * 1024 * sizeof(char);
    char* buffer = reinterpret_cast<char*>(malloc(BufferLength));
    if (buffer == nullptr)
    {
        return -1;
    }

    // Repeatedly read from the source and write to the destination
    while (true)
    {
        // Read up to what will fit in our buffer.  We're done if we get back 0 bytes.
        ssize_t bytesRead;
        while (CheckInterrupted(bytesRead = read(inFd, buffer, BufferLength)));
        if (bytesRead == -1)
        {
            int tmp = errno;
            free(buffer);
            errno = tmp;
            return -1;
        }
        if (bytesRead == 0)
        {
            break;
        }
        assert(bytesRead > 0);

        // Write what was read.
        ssize_t offset = 0;
        while (bytesRead > 0)
        {
            ssize_t bytesWritten;
            while (CheckInterrupted(bytesWritten = write(outFd, buffer + offset, static_cast<size_t>(bytesRead))));
            if (bytesWritten == -1)
            {
                int tmp = errno;
                free(buffer);
                errno = tmp;
                return -1;
            }
            assert(bytesWritten >= 0);
            bytesRead -= bytesWritten;
            offset += bytesWritten;
        }
    }

    free(buffer);
    return 0;
}
#endif // !HAVE_FCOPYFILE

extern "C" int32_t SystemNative_CopyFile(intptr_t sourceFd, intptr_t destinationFd)
{
    int inFd = ToFileDescriptor(sourceFd);
    int outFd = ToFileDescriptor(destinationFd);

#if HAVE_FCOPYFILE
    // If fcopyfile is available (OS X), try to use it, as the whole copy
    // can be performed in the kernel, without lots of unnecessary copying.
    // Copy data and metadata.
    return fcopyfile(inFd, outFd, nullptr, COPYFILE_ALL) == 0 ? 0 : -1;
#else
    // Get the stats on the source file.
    int ret;
    struct stat_ sourceStat;
    bool copied = false;
#if HAVE_SENDFILE
    // If sendfile is available (Linux), try to use it, as the whole copy
    // can be performed in the kernel, without lots of unnecessary copying.
    while (CheckInterrupted(ret = fstat_(inFd, &sourceStat)));
    if (ret != 0)
    {
        return -1;
    }


    // We use `auto' here to adapt the type of `size' depending on the running platform.
    // On 32-bit, if you use 64-bit offsets, the last argument of `sendfile' will be a
    // `size_t' a 32-bit integer while the `st_size' field of the stat structure will be off64_t.
    // So `size' will have to be `uint64_t'. In all other cases, it will be `size_t'.
    auto size = UnsignedCast(sourceStat.st_size);

    // Note that per man page for large files, you have to iterate until the
    // whole file is copied (Linux has a limit of 0x7ffff000 bytes copied).
    while (size > 0)
    {
        ssize_t sent = sendfile(outFd, inFd, nullptr, (size >= SSIZE_MAX ? SSIZE_MAX : static_cast<size_t>(size)));
        if (sent < 0)
        {
            if (errno != EINVAL && errno != ENOSYS)
            {
                return -1;
            }
            else
            {
                break;
            }
        }
        else
        {
            assert(UnsignedCast(sent) <= size);
            size -= UnsignedCast(sent);
        }
    }
    if (size == 0)
    {
        copied = true;
    }
    // sendfile couldn't be used; fall back to a manual copy below. This could happen
    // if we're on an old kernel, for example, where sendfile could only be used
    // with sockets and not regular files.
#endif // HAVE_SENDFILE

    // Manually read all data from the source and write it to the destination.
    if (!copied && CopyFile_ReadWrite(inFd, outFd) != 0)
    {
        return -1;
    }

    // Now that the data from the file has been copied, copy over metadata
    // from the source file.  First copy the file times.
    while (CheckInterrupted(ret = fstat_(inFd, &sourceStat)));
    if (ret == 0)
    {
        struct timeval origTimes[2];
        origTimes[0].tv_sec = sourceStat.st_atime;
        origTimes[0].tv_usec = 0;
        origTimes[1].tv_sec = sourceStat.st_mtime;
        origTimes[1].tv_usec = 0;
        while (CheckInterrupted(ret = futimes(outFd, origTimes)));
    }
    if (ret != 0)
    {
        return -1;
    }

    // Then copy permissions.
    while (CheckInterrupted(ret = fchmod(outFd, sourceStat.st_mode & (S_IRWXU | S_IRWXG | S_IRWXO))));
    if (ret != 0)
    {
        return -1;
    }

    return 0;
#endif // HAVE_FCOPYFILE
}

extern "C" intptr_t SystemNative_INotifyInit()
{
#if HAVE_INOTIFY
    return inotify_init();
#else
    errno = ENOTSUP;
    return -1;
#endif
}

extern "C" int32_t SystemNative_INotifyAddWatch(intptr_t fd, const char* pathName, uint32_t mask)
{
    assert(fd >= 0);
    assert(pathName != nullptr);

#if HAVE_INOTIFY
    return inotify_add_watch(ToFileDescriptor(fd), pathName, mask);
#else
    (void)fd, (void)pathName, (void)mask;
    errno = ENOTSUP;
    return -1;
#endif
}

extern "C" int32_t SystemNative_INotifyRemoveWatch(intptr_t fd, int32_t wd)
{
    assert(fd >= 0);
    assert(wd >= 0);

#if HAVE_INOTIFY
    return inotify_rm_watch(ToFileDescriptor(fd), wd);
#else
    (void)fd, (void)wd;
    errno = ENOTSUP;
    return -1;
#endif
}

extern "C" int32_t SystemNative_GetPeerID(intptr_t socket, uid_t* euid)
{
    int fd = ToFileDescriptor(socket);
#ifdef SO_PEERCRED
    struct ucred creds;
    socklen_t len = sizeof(creds);
    if (getsockopt(fd, SOL_SOCKET, SO_PEERCRED, &creds, &len) == 0)
    {
        *euid = creds.uid;
        return 0;
    }
    return -1;
#elif HAVE_GETPEEREID
    uid_t egid;
    return getpeereid(fd, euid, &egid);
#else
    (void)fd;
    (void)*euid;
    errno = ENOTSUP;
    return -1;
#endif
}

extern "C" char* SystemNative_RealPath(const char* path)
{
    assert(path != nullptr);
    return realpath(path, nullptr);
}

extern "C" int32_t SystemNative_LockFileRegion(intptr_t fd, int64_t offset, int64_t length, int16_t lockType)
{
    if (offset < 0 || length < 0) 
    {
        errno = EINVAL;
        return -1;
    }
    
    struct flock lockArgs;
    lockArgs.l_type = lockType;
    lockArgs.l_whence = SEEK_SET;
    lockArgs.l_start = offset;
    lockArgs.l_len = length;
    
    int32_t ret;
    while (CheckInterrupted(ret = fcntl (ToFileDescriptor(fd), F_SETLK, &lockArgs)));
    return ret;
}
