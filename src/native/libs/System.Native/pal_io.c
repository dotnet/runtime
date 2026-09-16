// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_compiler.h"
#include "pal_config.h"
#include "pal_errno.h"
#include "pal_io.h"
#include "pal_utilities.h"
#include "pal_safecrt.h"
#include "pal_types.h"

#include <assert.h>
#include <fcntl.h>
#include <errno.h>
#include <fnmatch.h>
#include <stddef.h>
#include <stdio.h>
#include <stdlib.h>
#include <sys/mman.h>
#include <sys/stat.h>
#include <sys/time.h>
#include <sys/types.h>
#include <sys/file.h>
#include <sys/ioctl.h>
#include <sys/socket.h>
#if !HAVE_MAKEDEV_FILEH && HAVE_MAKEDEV_SYSMACROSH
#include <sys/sysmacros.h>
#endif
#include <sys/uio.h>
#if HAVE_SYSLOG_H
#include <syslog.h>
#endif
#if HAVE_TERMIOS_H
#include <termios.h>
#endif
#include <unistd.h>
#include <limits.h>
#if HAVE_FCOPYFILE
#include <copyfile.h>
#elif HAVE_SENDFILE_4
#include <sys/sendfile.h>
#endif
#if HAVE_INOTIFY
#include <sys/inotify.h>
#endif
#if HAVE_STATFS_VFS // Linux
#include <sys/vfs.h>
#elif HAVE_STATFS_MOUNT // BSD
#include <sys/mount.h>
#elif HAVE_SYS_STATVFS_H && !HAVE_NON_LEGACY_STATFS && HAVE_STATVFS_BASETYPE // SunOS
#include <sys/types.h>
#include <sys/statvfs.h>
#if HAVE_STATFS_VFS
#include <sys/vfs.h>
#endif
#endif

#ifdef TARGET_SUNOS
#include <sys/param.h>
#endif

#ifdef TARGET_HAIKU
#include <fs_info.h>
#endif // TARGET_HAIKU

#ifdef _AIX
#include <alloca.h>
// Somehow, AIX mangles the definition for this behind a C++ def
// Redeclare it here
extern int     getpeereid(int, uid_t *__restrict__, gid_t *__restrict__);
#endif

#if defined(TARGET_SUNOS)
#include <procfs.h>
#endif

#ifdef __linux__
#include <sys/utsname.h>

// Ensure FICLONE is defined for all Linux builds.
#ifndef FICLONE
#define FICLONE _IOW(0x94, 9, int)
#endif /* __linux__ */

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wreserved-id-macro"
// Ensure __NR_copy_file_range is defined for portable builds.
#include <sys/syscall.h> // __NR_copy_file_range
# if !defined(__NR_copy_file_range)
#  if defined(__amd64__)
#   define __NR_copy_file_range  326
#  elif defined(__i386__)
#   define __NR_copy_file_range  377
#  elif defined(__arm__)
#   define __NR_copy_file_range  391
#  elif defined(__aarch64__)
#   define __NR_copy_file_range  285
#  else
#   error Unknown architecture
#  endif
# endif
#pragma clang diagnostic pop

#if HAVE_LINUX_IO_URING_H
// The CMake HAVE_LINUX_IO_URING_H check also verifies that __NR_io_uring_setup/enter/register
// are defined by <sys/syscall.h>, so no fallback definitions are needed here.
#include <linux/io_uring.h>
#include <stdatomic.h>
#endif // HAVE_LINUX_IO_URING_H

#endif

#if HAVE_STAT64
#define stat_ stat64
#define fstat_ fstat64
#define lstat_ lstat64
#else /* HAVE_STAT64 */
#define stat_ stat
#define fstat_ fstat
#define lstat_ lstat
#endif  /* HAVE_STAT64 */

// These numeric values are specified by POSIX.
// Validate that our definitions match.
c_static_assert(PAL_S_IRWXU == S_IRWXU);
c_static_assert(PAL_S_IRUSR == S_IRUSR);
c_static_assert(PAL_S_IWUSR == S_IWUSR);
c_static_assert(PAL_S_IXUSR == S_IXUSR);
c_static_assert(PAL_S_IRWXG == S_IRWXG);
c_static_assert(PAL_S_IRGRP == S_IRGRP);
c_static_assert(PAL_S_IWGRP == S_IWGRP);
c_static_assert(PAL_S_IXGRP == S_IXGRP);
c_static_assert(PAL_S_IRWXO == S_IRWXO);
c_static_assert(PAL_S_IROTH == S_IROTH);
c_static_assert(PAL_S_IWOTH == S_IWOTH);
c_static_assert(PAL_S_IXOTH == S_IXOTH);
c_static_assert(PAL_S_ISUID == S_ISUID);
c_static_assert(PAL_S_ISGID == S_ISGID);

// These numeric values are not specified by POSIX, but the values
// are common to our current targets.  If these static asserts fail,
// ConvertFileStatus needs to be updated to twiddle mode bits
// accordingly.
#if !defined(TARGET_WASI)
c_static_assert(PAL_S_IFMT == S_IFMT);
c_static_assert(PAL_S_IFIFO == S_IFIFO);
#endif /* TARGET_WASI */
c_static_assert(PAL_S_IFBLK == S_IFBLK);
c_static_assert(PAL_S_IFCHR == S_IFCHR);
c_static_assert(PAL_S_IFDIR == S_IFDIR);
c_static_assert(PAL_S_IFREG == S_IFREG);
c_static_assert(PAL_S_IFLNK == S_IFLNK);
c_static_assert(PAL_S_IFSOCK == S_IFSOCK);

// Validate that our enum for inode types is the same as what is
// declared by the dirent.h header on the local system.
// (AIX doesn't have dirent d_type, so none of this there)
// WebAssembly (BROWSER) has dirent d_type but is not correct
// by returning UNKNOWN the managed code properly stats the file
// to detect if entry is directory or not.
#if (defined(DT_UNKNOWN) || defined(TARGET_WASM)) && !defined(TARGET_WASI)
c_static_assert((int)PAL_DT_UNKNOWN == (int)DT_UNKNOWN);
c_static_assert((int)PAL_DT_FIFO == (int)DT_FIFO);
c_static_assert((int)PAL_DT_CHR == (int)DT_CHR);
c_static_assert((int)PAL_DT_DIR == (int)DT_DIR);
c_static_assert((int)PAL_DT_BLK == (int)DT_BLK);
c_static_assert((int)PAL_DT_REG == (int)DT_REG);
c_static_assert((int)PAL_DT_LNK == (int)DT_LNK);
c_static_assert((int)PAL_DT_SOCK == (int)DT_SOCK);
#ifdef DT_WHT // not available in OpenBSD
c_static_assert((int)PAL_DT_WHT == (int)DT_WHT);
#endif
#endif

// Validate that our Lock enum value are correct for the platform
c_static_assert(PAL_LOCK_SH == LOCK_SH);
c_static_assert(PAL_LOCK_EX == LOCK_EX);
c_static_assert(PAL_LOCK_NB == LOCK_NB);
c_static_assert(PAL_LOCK_UN == LOCK_UN);

// Validate our AccessMode enum values are correct for the platform
c_static_assert(PAL_F_OK == F_OK);
c_static_assert(PAL_X_OK == X_OK);
c_static_assert(PAL_W_OK == W_OK);
c_static_assert(PAL_R_OK == R_OK);

// Validate our SeekWhence enum values are correct for the platform
c_static_assert(PAL_SEEK_SET == SEEK_SET);
c_static_assert(PAL_SEEK_CUR == SEEK_CUR);
c_static_assert(PAL_SEEK_END == SEEK_END);

// Validate our NotifyEvents enum values are correct for the platform
#if HAVE_INOTIFY
c_static_assert(PAL_IN_ACCESS == IN_ACCESS);
c_static_assert(PAL_IN_MODIFY == IN_MODIFY);
c_static_assert(PAL_IN_ATTRIB == IN_ATTRIB);
c_static_assert(PAL_IN_MOVED_FROM == IN_MOVED_FROM);
c_static_assert(PAL_IN_MOVED_TO == IN_MOVED_TO);
c_static_assert(PAL_IN_CREATE == IN_CREATE);
c_static_assert(PAL_IN_DELETE == IN_DELETE);
c_static_assert(PAL_IN_Q_OVERFLOW == IN_Q_OVERFLOW);
c_static_assert(PAL_IN_IGNORED == IN_IGNORED);
c_static_assert(PAL_IN_ONLYDIR == IN_ONLYDIR);
c_static_assert(PAL_IN_DONT_FOLLOW == IN_DONT_FOLLOW);
#if HAVE_IN_EXCL_UNLINK
c_static_assert(PAL_IN_EXCL_UNLINK == IN_EXCL_UNLINK);
#endif // HAVE_IN_EXCL_UNLINK
c_static_assert(PAL_IN_MOVE_SELF == IN_MOVE_SELF);
c_static_assert(PAL_IN_ISDIR == IN_ISDIR);
#endif // HAVE_INOTIFY

// Validate that our UserFlags enum values match the platform, since
// SystemNative_LChflags and SystemNative_FChflags pass them directly to the OS.
#if HAVE_STAT_FLAGS && defined(UF_HIDDEN)
c_static_assert(PAL_UF_HIDDEN == UF_HIDDEN);
#endif

static void ConvertFileStatus(const struct stat_* src, FileStatus* dst)
{
    dst->Dev = (int64_t)src->st_dev;
    dst->RDev = (int64_t)src->st_rdev;
    dst->Ino = (int64_t)src->st_ino;
    dst->Flags = FILESTATUS_FLAGS_NONE;
    dst->Mode = (int32_t)src->st_mode;
    dst->Uid = src->st_uid;
    dst->Gid = src->st_gid;
    dst->Size = src->st_size;

    dst->ATime = src->st_atime;
    dst->MTime = src->st_mtime;
    dst->CTime = src->st_ctime;

    dst->ATimeNsec = ST_ATIME_NSEC(src);
    dst->MTimeNsec = ST_MTIME_NSEC(src);
    dst->CTimeNsec = ST_CTIME_NSEC(src);

#if HAVE_STAT_BIRTHTIME
    dst->BirthTime = src->st_birthtimespec.tv_sec;
    dst->BirthTimeNsec = src->st_birthtimespec.tv_nsec;
    dst->Flags |= FILESTATUS_FLAGS_HAS_BIRTHTIME;
#else
    // Linux path: until we use statx() instead
    dst->BirthTime = 0;
    dst->BirthTimeNsec = 0;
#endif

#if HAVE_STAT_FLAGS && defined(UF_HIDDEN)
    dst->UserFlags = (uint32_t)src->st_flags;
#else
    dst->UserFlags = 0;
#endif

    dst->HardLinkCount = (uint32_t)src->st_nlink;
}

int32_t SystemNative_Stat(const char* path, FileStatus* output)
{
    struct stat_ result;
    int ret;
    while ((ret = stat_(path, &result)) < 0 && errno == EINTR);

    if (ret == 0)
    {
        ConvertFileStatus(&result, output);
    }

    return ret;
}

int32_t SystemNative_FStat(intptr_t fd, FileStatus* output)
{
    struct stat_ result;
    int ret;
    while ((ret = fstat_(ToFileDescriptor(fd), &result)) < 0 && errno == EINTR);

    if (ret == 0)
    {
        ConvertFileStatus(&result, output);
    }

    return ret;
}

int32_t SystemNative_LStat(const char* path, FileStatus* output)
{
    struct stat_ result;
    int ret = lstat_(path, &result);

    if (ret == 0)
    {
        ConvertFileStatus(&result, output);
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
            assert_msg(false, "Unknown Open access mode", (int)flags);
            return -1;
    }

    if (flags & ~(PAL_O_ACCESS_MODE_MASK | PAL_O_CLOEXEC | PAL_O_CREAT | PAL_O_EXCL | PAL_O_TRUNC | PAL_O_SYNC | PAL_O_NOFOLLOW))
    {
        assert_msg(false, "Unknown Open flag", (int)flags);
        return -1;
    }

#if HAVE_O_CLOEXEC
    if (flags & PAL_O_CLOEXEC)
        ret |= O_CLOEXEC;
#endif
    if (flags & PAL_O_CREAT)
        ret |= O_CREAT;
    if (flags & PAL_O_EXCL)
        ret |= O_EXCL;
    if (flags & PAL_O_TRUNC)
        ret |= O_TRUNC;
    if (flags & PAL_O_SYNC)
        ret |= O_SYNC;
    if (flags & PAL_O_NOFOLLOW)
        ret |= O_NOFOLLOW;

    assert(ret != -1);
    return ret;
}

intptr_t SystemNative_Open(const char* path, int32_t flags, int32_t mode)
{
// these two ifdefs are for platforms where we dont have the open version of CLOEXEC and thus
// must simulate it by doing a fcntl with the SETFFD version after the open instead
#if !HAVE_O_CLOEXEC
    int32_t old_flags = flags;
#endif
    flags = ConvertOpenFlags(flags);
    if (flags == -1)
    {
        errno = EINVAL;
        return -1;
    }

    // Prevent terminal devices from becoming the controlling terminal of this process.
    // WASM (browser/WASI) has no controlling terminals, and WASMFS's doOpen rejects any
    // flag outside its known set (O_NOCTTY is not among them), so skip it there.
#ifndef TARGET_WASM
    flags |= O_NOCTTY;
#endif

    int result;
    while ((result = open(path, flags, (mode_t)mode)) < 0 && errno == EINTR);
#if !HAVE_O_CLOEXEC
    if (old_flags & PAL_O_CLOEXEC)
    {
        fcntl(result, F_SETFD, FD_CLOEXEC);
    }
#endif
    return result;
}

int32_t SystemNative_Close(intptr_t fd)
{
    int result = close(ToFileDescriptor(fd));
    if (result < 0 && errno == EINTR) result = 0; // on all supported platforms, close(2) returning EINTR still means it was released
    return result;
}

intptr_t SystemNative_Dup(intptr_t oldfd)
{
    int result;
#if HAVE_F_DUPFD_CLOEXEC
    while ((result = fcntl(ToFileDescriptor(oldfd), F_DUPFD_CLOEXEC, 0)) < 0 && errno == EINTR);
#elif HAVE_F_DUPFD
    while ((result = fcntl(ToFileDescriptor(oldfd), F_DUPFD, 0)) < 0 && errno == EINTR);
    // do CLOEXEC here too
    fcntl(result, F_SETFD, FD_CLOEXEC);
#else
    // The main use cases for dup are setting up the classic Unix dance of setting up file descriptors in advance of performing a fork. Since WASI has no fork, these don't apply.
    // https://github.com/bytecodealliance/wasmtime/blob/b2fefe77148582a9b8013e34fe5808ada82b6efc/docs/WASI-rationale.md#why-no-dup
    result = oldfd;
#endif
    return result;
}

int32_t SystemNative_Unlink(const char* path)
{
    int32_t result;
    while ((result = unlink(path)) < 0 && errno == EINTR);
    return result;
}

#ifdef __NR_memfd_create
#ifndef MFD_CLOEXEC
#define MFD_CLOEXEC 0x0001U
#endif
#ifndef MFD_ALLOW_SEALING
#define MFD_ALLOW_SEALING 0x0002U
#endif
#ifndef F_ADD_SEALS
#define F_ADD_SEALS (1024 + 9)
#endif
#ifndef F_SEAL_WRITE
#define F_SEAL_WRITE 0x0008
#endif
#endif

int32_t SystemNative_IsMemfdSupported(void)
{
#ifdef __NR_memfd_create
#ifdef TARGET_LINUX
    struct utsname uts;
    int32_t major, minor;

    // memfd_create is known to only work properly on kernel version > 3.17.
    // On earlier versions, it may raise SIGSEGV instead of returning ENOTSUP.
    if (uname(&uts) == 0 && sscanf(uts.release, "%d.%d", &major, &minor) == 2 && (major < 3 || (major == 3 && minor < 17)))
    {
        return 0;
    }
#endif

    // Note that the name has no affect on file descriptor behavior. From linux manpage:
    //   Names do not affect the behavior of the file descriptor, and as such multiple files can have the same name without any side effects.
    int32_t fd = (int32_t)syscall(__NR_memfd_create, "test", MFD_CLOEXEC | MFD_ALLOW_SEALING);
    if (fd < 0) return 0;

    close(fd);
    return 1;
#else
    errno = ENOTSUP;
    return 0;
#endif
}

intptr_t SystemNative_MemfdCreate(const char* name, int32_t isReadonly)
{
#ifdef __NR_memfd_create
#if defined(SHM_NAME_MAX) // macOS
    assert(strlen(name) <= SHM_NAME_MAX);
#elif defined(PATH_MAX) // other Unixes
    assert(strlen(name) <= PATH_MAX);
#endif

    int32_t fd = (int32_t)syscall(__NR_memfd_create, name, MFD_CLOEXEC | MFD_ALLOW_SEALING);
    if (!isReadonly || fd < 0) return fd;

    // Add a write seal when readonly protection requested
    while (fcntl(fd, F_ADD_SEALS, F_SEAL_WRITE) < 0 && errno == EINTR);
    return fd;
#else
    (void)name;
    (void)isReadonly;
    errno = ENOTSUP;
    return -1;
#endif
}

intptr_t SystemNative_ShmOpen(const char* name, int32_t flags, int32_t mode)
{
#if defined(SHM_NAME_MAX) // macOS
    assert(strlen(name) <= SHM_NAME_MAX);
#elif defined(PATH_MAX) // other Unixes
    assert(strlen(name) <= PATH_MAX);
#endif

#if HAVE_SHM_OPEN_THAT_WORKS_WELL_ENOUGH_WITH_MMAP
    flags = ConvertOpenFlags(flags);
    if (flags == -1)
    {
        errno = EINVAL;
        return -1;
    }

    return shm_open(name, flags, (mode_t)mode);
#else
    (void)name, (void)flags, (void)mode;
    errno = ENOTSUP;
    return -1;
#endif
}

int32_t SystemNative_ShmUnlink(const char* name)
{
#if HAVE_SHM_OPEN_THAT_WORKS_WELL_ENOUGH_WITH_MMAP
    int32_t result;
    while ((result = shm_unlink(name)) < 0 && errno == EINTR);
    return result;
#else
    // Not supported on e.g. Android. Also, prevent a compiler error because name is unused
    (void)name;
    errno = ENOTSUP;
    return -1;
#endif
}

static void ConvertDirent(const struct dirent* entry, DirectoryEntry* outputEntry)
{
    // We use Marshal.PtrToStringAnsi on the managed side, which takes a pointer to
    // the start of the unmanaged string. Give the caller back a pointer to the
    // location of the start of the string that exists in their own byte buffer.
    outputEntry->Name = entry->d_name;
#if !defined(DT_UNKNOWN) || defined(TARGET_WASM)
    // AIX has no d_type, and since we can't get the directory that goes with
    // the filename from ReadDir, we can't stat the file. Return unknown and
    // hope that managed code can properly stat the file.
    // WebAssembly (BROWSER) has dirent d_type but is not correct
    // by returning UNKNOWN the managed code properly stats the file
    // to detect if entry is directory or not.
    outputEntry->InodeType = PAL_DT_UNKNOWN;
#else
    outputEntry->InodeType = (int32_t)entry->d_type;
#endif

#if HAVE_DIRENT_NAME_LEN
    outputEntry->NameLength = entry->d_namlen;
#else
    outputEntry->NameLength = -1; // sentinel value to mean we have to walk to find the first \0
#endif
}

// The caller must ensure no calls are made to readdir/closedir since those will invalidate
// the current dirent. We assume the platform supports concurrent readdir calls to different DIRs.
int32_t SystemNative_ReadDir(DIR* dir, DirectoryEntry* outputEntry)
{
    assert(dir != NULL);
    assert(outputEntry != NULL);

    errno = 0;
    struct dirent* entry = readdir(dir);

    // 0 returned with null result -> end-of-stream
    if (entry == NULL)
    {
        memset(outputEntry, 0, sizeof(*outputEntry)); // managed out param must be initialized

        //  kernel set errno -> failure
        if (errno != 0)
        {
            assert_err(errno == EBADF, "Invalid directory stream descriptor dir", errno);
            return errno;
        }
        return -1;
    }

    ConvertDirent(entry, outputEntry);
    return 0;
}

DIR* SystemNative_OpenDir(const char* path)
{
    DIR *result;

    // EINTR isn't documented, happens in practice on macOS.
    while ((result = opendir(path)) == NULL && errno == EINTR);

    return result;
}

int32_t SystemNative_CloseDir(DIR* dir)
{
    int32_t result;

    result = closedir(dir);

    // EINTR isn't documented, happens in practice on macOS.
    if (result < 0 && errno == EINTR)
    {
        result = 0;
    }

    return result;
}

int32_t SystemNative_IsAtomicNonInheritablePipeCreationSupported(void)
{
#if HAVE_PIPE2
    return 1;
#else
    return 0;
#endif
}

int32_t SystemNative_Pipe(int32_t pipeFds[2], int32_t flags)
{
#ifdef TARGET_WASM
    // Pipe is not supported on Wasm (browser or WASI)
    (void)pipeFds;
    (void)flags;
    errno = ENOTSUP;
    return -1;
#else // TARGET_WASM
    if ((flags & ~(PAL_O_CLOEXEC | PAL_O_NONBLOCK_READ | PAL_O_NONBLOCK_WRITE)) != 0)
    {
        assert_msg(false, "Unknown pipe flag", (int)flags);
        errno = EINVAL;
        return -1;
    }

    int32_t pipeFlags = 0;
    if ((flags & PAL_O_CLOEXEC) != 0)
    {
#if HAVE_O_CLOEXEC
        pipeFlags = O_CLOEXEC;
#endif
    }

    int32_t result;
#if HAVE_PIPE2
    // If pipe2 is available, use it.  This will handle O_CLOEXEC if it was set.
    while ((result = pipe2(pipeFds, pipeFlags)) < 0 && errno == EINTR);
#elif HAVE_PIPE
    // Otherwise, use pipe.
    while ((result = pipe(pipeFds)) < 0 && errno == EINTR);

    // Then, if O_CLOEXEC was specified, use fcntl to configure the file descriptors appropriately.
#if HAVE_O_CLOEXEC
    if ((pipeFlags & O_CLOEXEC) != 0 && result == 0)
#else
    if ((flags & PAL_O_CLOEXEC) != 0 && result == 0)
#endif
    {
        while ((result = fcntl(pipeFds[0], F_SETFD, FD_CLOEXEC)) < 0 && errno == EINTR);
        if (result == 0)
        {
            while ((result = fcntl(pipeFds[1], F_SETFD, FD_CLOEXEC)) < 0 && errno == EINTR);
        }

        if (result != 0)
        {
            int tmpErrno = errno;
            close(pipeFds[0]);
            close(pipeFds[1]);
            errno = tmpErrno;
        }
    }
#else /* HAVE_PIPE */
    result = -1;
#endif /* HAVE_PIPE */

    if (result == 0 && ((flags & (PAL_O_NONBLOCK_READ | PAL_O_NONBLOCK_WRITE)) != 0))
    {
        if ((flags & PAL_O_NONBLOCK_READ) != 0)
        {
            result = SystemNative_FcntlSetIsNonBlocking((intptr_t)pipeFds[0], 1);
        }

        if (result == 0 && (flags & PAL_O_NONBLOCK_WRITE) != 0)
        {
            result = SystemNative_FcntlSetIsNonBlocking((intptr_t)pipeFds[1], 1);
        }

        if (result != 0)
        {
            int tmpErrno = errno;
            close(pipeFds[0]);
            close(pipeFds[1]);
            errno = tmpErrno;
        }
    }

    return result;
#endif // TARGET_WASM
}

int32_t SystemNative_FcntlSetFD(intptr_t fd, int32_t flags)
{
    int result;
    while ((result = fcntl(ToFileDescriptor(fd), F_SETFD, ConvertOpenFlags(flags))) < 0 && errno == EINTR);
    return result;
}

int32_t SystemNative_FcntlGetFD(intptr_t fd)
{
    return fcntl(ToFileDescriptor(fd), F_GETFD);
}

int32_t SystemNative_FcntlCanGetSetPipeSz(void)
{
#if defined(F_GETPIPE_SZ) && defined(F_SETPIPE_SZ)
    return true;
#else
    return false;
#endif
}

int32_t SystemNative_FcntlGetPipeSz(intptr_t fd)
{
#ifdef F_GETPIPE_SZ
    int32_t result;
    while ((result = fcntl(ToFileDescriptor(fd), F_GETPIPE_SZ)) < 0 && errno == EINTR);
    return result;
#else
    (void)fd;
    errno = ENOTSUP;
    return -1;
#endif
}

int32_t SystemNative_FcntlSetPipeSz(intptr_t fd, int32_t size)
{
#ifdef F_SETPIPE_SZ
    int32_t result;
    while ((result = fcntl(ToFileDescriptor(fd), F_SETPIPE_SZ, size)) < 0 && errno == EINTR);
    return result;
#else
    (void)fd, (void)size;
    errno = ENOTSUP;
    return -1;
#endif
}

int32_t SystemNative_FcntlSetIsNonBlocking(intptr_t fd, int32_t isNonBlocking)
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

int32_t SystemNative_FcntlGetIsNonBlocking(intptr_t fd, int32_t* isNonBlocking)
{
    if (isNonBlocking == NULL)
    {
        return Error_EFAULT;
    }

    int flags = fcntl(ToFileDescriptor(fd), F_GETFL);
    if (flags == -1)
    {
        *isNonBlocking = 0;
        return -1;
    }

    *isNonBlocking = ((flags & O_NONBLOCK) == O_NONBLOCK) ? 1 : 0;
    return 0;
}

int32_t SystemNative_MkDir(const char* path, int32_t mode)
{
    int32_t result;
    while ((result = mkdir(path, (mode_t)mode)) < 0 && errno == EINTR);
    return result;
}

int32_t SystemNative_ChMod(const char* path, int32_t mode)
{
#if HAVE_CHMOD
    int32_t result;
    while ((result = chmod(path, (mode_t)mode)) < 0 && errno == EINTR);
    return result;
#else /* HAVE_CHMOD */
    (void)path; // unused
    (void)mode; // unused
    return EINTR;
#endif /* HAVE_CHMOD */
}

int32_t SystemNative_FChMod(intptr_t fd, int32_t mode)
{
#if HAVE_FCHMOD
    int32_t result;
    while ((result = fchmod(ToFileDescriptor(fd), (mode_t)mode)) < 0 && errno == EINTR);
    return result;
#else /* HAVE_FCHMOD */
    (void)fd; // unused
    (void)mode; // unused
    return EINTR;
#endif /* HAVE_FCHMOD */
}

int32_t SystemNative_FSync(intptr_t fd)
{
    int fileDescriptor = ToFileDescriptor(fd);

    int32_t result;
#ifdef TARGET_OSX
    while ((result = fcntl(fileDescriptor, F_FULLFSYNC)) < 0 && errno == EINTR);
    if (result >= 0)
    {
        return result;
    }

    // F_FULLFSYNC is not supported on all file systems and handle types (e.g.,
    // network file systems, read-only handles). Fall back to fsync.
    // For genuine I/O errors (e.g., EIO), fsync will also fail and propagate the error.
#endif
    while ((result = fsync(fileDescriptor)) < 0 && errno == EINTR);
    return result;
}

int32_t SystemNative_FLock(intptr_t fd, int32_t operation)
{
    int32_t result;
#if !defined(TARGET_WASI)
    while ((result = flock(ToFileDescriptor(fd), operation)) < 0 && errno == EINTR);
#else /* TARGET_WASI */
    result = EINTR;
#endif /* TARGET_WASI */
    return result;
}

int32_t SystemNative_ChDir(const char* path)
{
    int32_t result;
    while ((result = chdir(path)) < 0 && errno == EINTR);
    return result;
}

int32_t SystemNative_Access(const char* path, int32_t mode)
{
    return access(path, mode);
}

int64_t SystemNative_LSeek(intptr_t fd, int64_t offset, int32_t whence)
{
    int64_t result;
    while ((
        result =
#if HAVE_LSEEK64
            lseek64(
                 ToFileDescriptor(fd),
                 (off_t)offset,
                 whence)) < 0 && errno == EINTR);
#else
            lseek(
                 ToFileDescriptor(fd),
                 (off_t)offset,
                 whence)) < 0 && errno == EINTR);
#endif
    return result;
}

int32_t SystemNative_Link(const char* source, const char* linkTarget)
{
    int32_t result;
    while ((result = link(source, linkTarget)) < 0 && errno == EINTR);
    return result;
}

int32_t SystemNative_SymLink(const char* target, const char* linkPath)
{
    int32_t result;
    while ((result = symlink(target, linkPath)) < 0 && errno == EINTR);
    return result;
}

void SystemNative_GetDeviceIdentifiers(uint64_t dev, uint32_t* majorNumber, uint32_t* minorNumber)
{
#if !defined(TARGET_WASI)
    dev_t castedDev = (dev_t)dev;
#if !defined(TARGET_HAIKU)
    *majorNumber = (uint32_t)major(castedDev);
    *minorNumber = (uint32_t)minor(castedDev);
#else
    // Haiku has no concept of major/minor numbers, but it does have device IDs.
    *majorNumber = 0;
    *minorNumber = (uint32_t)dev;
#endif // TARGET_HAIKU
#else /* TARGET_WASI */
    dev_t castedDev = (dev_t)dev;
    *majorNumber = 0;
    *minorNumber = 0;
#endif /* TARGET_WASI */
}

int32_t SystemNative_MkNod(const char* pathName, uint32_t mode, uint32_t major, uint32_t minor)
{
#if !defined(TARGET_WASI)
#if !defined(TARGET_HAIKU)
    dev_t dev = (dev_t)makedev(major, minor);
#else
    (void)major;
    dev_t dev = (dev_t)minor;
#endif // !TARGET_HAIKU

    int32_t result;
    while ((result = mknod(pathName, (mode_t)mode, dev)) < 0 && errno == EINTR);
    return result;
#else /* TARGET_WASI */
    return EINTR;
#endif /* TARGET_WASI */
}

int32_t SystemNative_MkFifo(const char* pathName, uint32_t mode)
{
#if !defined(TARGET_WASI)
    int32_t result;
    while ((result = mkfifo(pathName, (mode_t)mode)) < 0 && errno == EINTR);
    return result;
#else /* TARGET_WASI */
    return EINTR;
#endif /* TARGET_WASI */
}

char* SystemNative_MkdTemp(char* pathTemplate)
{
#if !defined(TARGET_WASI)
    char* result = NULL;
    while ((result = mkdtemp(pathTemplate)) == NULL && errno == EINTR);
    return result;
#else /* TARGET_WASI */
    return NULL;
#endif /* TARGET_WASI */
}

intptr_t SystemNative_MksTemps(char* pathTemplate, int32_t suffixLength)
{
    intptr_t result;
#if HAVE_MKSTEMPS
    while ((result = mkstemps(pathTemplate, suffixLength)) < 0 && errno == EINTR);
#elif HAVE_MKSTEMP
    // mkstemps is not available bionic/Android, but mkstemp is
    // mkstemp doesn't allow the suffix that msktemps does allow, so we'll need to
    // remove that before passisng pathTemplate to mkstemp

    int32_t pathTemplateLength = (int32_t)strlen(pathTemplate);

    // pathTemplate must include at least XXXXXX (6 characters) which are not part of
    // the suffix
    if (suffixLength < 0 || suffixLength > pathTemplateLength - 6)
    {
        errno = EINVAL;
        return -1;
    }

    // Make mkstemp ignore the suffix by setting the first char of the suffix to \0,
    // if there is a suffix
    int32_t firstSuffixIndex = 0;
    char firstSuffixChar = 0;

    if (suffixLength > 0)
    {
        firstSuffixIndex = pathTemplateLength - suffixLength;
        firstSuffixChar = pathTemplate[firstSuffixIndex];
        pathTemplate[firstSuffixIndex] = 0;
    }

    while ((result = mkstemp(pathTemplate)) < 0 && errno == EINTR);

    // Reset the first char of the suffix back to its original value, if there is a suffix
    if (suffixLength > 0)
    {
        pathTemplate[firstSuffixIndex] = firstSuffixChar;
    }
#elif TARGET_WASI
    assert_msg(false, "Not supported on WASI", 0);
    result = -1;
#else
#error "Cannot find mkstemps nor mkstemp on this platform"
#endif
    return  result;
}

static int32_t ConvertMMapProtection(int32_t protection)
{
    if (protection == PAL_PROT_NONE)
        return PROT_NONE;

    if (protection & ~(PAL_PROT_READ | PAL_PROT_WRITE | PAL_PROT_EXEC))
    {
        assert_msg(false, "Unknown protection", (int)protection);
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
        assert_msg(false, "Unknown MMap flag", (int)flags);
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
        assert_msg(false, "Unknown MSync flag", (int)flags);
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

void* SystemNative_MMap(void* address,
                      uint64_t length,
                      int32_t protection, // bitwise OR of PAL_PROT_*
                      int32_t flags,      // bitwise OR of PAL_MAP_*, but PRIVATE and SHARED are mutually exclusive.
                      intptr_t fd,
                      int64_t offset)
{
    if (length > SIZE_MAX)
    {
        errno = ERANGE;
        return NULL;
    }

    protection = ConvertMMapProtection(protection);
    flags = ConvertMMapFlags(flags);

    if (flags == -1 || protection == -1)
    {
        errno = EINVAL;
        return NULL;
    }

    // Use ToFileDescriptorUnchecked to allow -1 to be passed for the file descriptor, since managed code explicitly uses -1
    void* ret =
#if HAVE_MMAP64
        mmap64(
#else
        mmap(
#endif
            address,
            (size_t)length,
            protection,
            flags,
            ToFileDescriptorUnchecked(fd),
            (off_t)offset);

    if (ret == MAP_FAILED)
    {
        return NULL;
    }

    assert(ret != NULL);
    return ret;
}

int32_t SystemNative_MUnmap(void* address, uint64_t length)
{
    if (length > SIZE_MAX)
    {
        errno = ERANGE;
        return -1;
    }

    return munmap(address, (size_t)length);
}

int32_t SystemNative_MProtect(void* address, uint64_t length, int32_t protection)
{
    if (length > SIZE_MAX)
    {
        errno =  ERANGE;
        return -1;
    }

    protection = ConvertMMapProtection(protection);

    return mprotect(address, (size_t)length, protection);
}

int32_t SystemNative_MAdvise(void* address, uint64_t length, int32_t advice)
{
    if (length > SIZE_MAX)
    {
        errno = ERANGE;
        return -1;
    }

    switch (advice)
    {
        case PAL_MADV_DONTFORK:
#if defined(MADV_DONTFORK) && !defined(TARGET_WASM)
            return madvise(address, (size_t)length, MADV_DONTFORK);
#else
            (void)address, (void)length, (void)advice;
            errno = ENOTSUP;
            return -1;
#endif // MADV_DONTFORK && !TARGET_WASM
        default:
            break; // fall through to error
    }

    assert_msg(false, "Unknown MemoryAdvice", (int)advice);
    errno = EINVAL;
    return -1;
}

int32_t SystemNative_MSync(void* address, uint64_t length, int32_t flags)
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

#if !defined(TARGET_WASI)
    return msync(address, (size_t)length, flags);
#else
    return -1;
#endif
}

int64_t SystemNative_SysConf(int32_t name)
{
    switch (name)
    {
        case PAL_SC_CLK_TCK:
            return sysconf(_SC_CLK_TCK);
        case PAL_SC_PAGESIZE:
            return sysconf(_SC_PAGESIZE);
        default:
            break; // fall through to error
    }

    assert_msg(false, "Unknown SysConf name", (int)name);
    errno = EINVAL;
    return -1;
}

int32_t SystemNative_FTruncate(intptr_t fd, int64_t length)
{
    int32_t result;
    while ((
        result =
#if HAVE_FTRUNCATE64
        ftruncate64(
#else
        ftruncate(
#endif
            ToFileDescriptor(fd),
            (off_t)length)) < 0 && errno == EINTR);
    return result;
}

int32_t SystemNative_Poll(PollEvent* pollEvents, uint32_t eventCount, int32_t milliseconds, uint32_t* triggered)
{
    return Common_Poll(pollEvents, eventCount, milliseconds, triggered);
}

int32_t SystemNative_PosixFAdvise(intptr_t fd, int64_t offset, int64_t length, int32_t advice)
{
#if HAVE_POSIX_ADVISE
    // POSIX_FADV_* may be different on each platform. Convert the values from PAL to the system's.
    int32_t actualAdvice;
    switch (advice)
    {
        case PAL_POSIX_FADV_NORMAL:     actualAdvice = POSIX_FADV_NORMAL;     break;
        case PAL_POSIX_FADV_RANDOM:     actualAdvice = POSIX_FADV_RANDOM;     break;
        case PAL_POSIX_FADV_SEQUENTIAL: actualAdvice = POSIX_FADV_SEQUENTIAL; break;
        case PAL_POSIX_FADV_WILLNEED:   actualAdvice = POSIX_FADV_WILLNEED;   break;
        case PAL_POSIX_FADV_DONTNEED:   actualAdvice = POSIX_FADV_DONTNEED;   break;
        case PAL_POSIX_FADV_NOREUSE:    actualAdvice = POSIX_FADV_NOREUSE;    break;
        default: return EINVAL; // According to the man page
    }
    int32_t result;
    while ((
        result =
#if HAVE_POSIX_FADVISE64
            posix_fadvise64(
#else
            posix_fadvise(
#endif
                ToFileDescriptor(fd),
                (off_t)offset,
                (off_t)length,
                actualAdvice)) < 0 && errno == EINTR);
    return result;
#else
    // Not supported on this platform. Caller can ignore this failure since it's just a hint.
    (void)fd, (void)offset, (void)length, (void)advice;
    return ENOTSUP;
#endif
}

int32_t SystemNative_FAllocate(intptr_t fd, int64_t offset, int64_t length)
{
    assert_msg(offset == 0, "Invalid offset value", (int)offset);

    int fileDescriptor = ToFileDescriptor(fd);
    int32_t result;
#if HAVE_FALLOCATE // Linux
    while ((result = fallocate(fileDescriptor, FALLOC_FL_KEEP_SIZE, (off_t)offset, (off_t)length)) == -1 && errno == EINTR);
#elif defined(F_PREALLOCATE) // macOS
    fstore_t fstore;
    fstore.fst_flags = F_ALLOCATEALL; // Allocate all requested space or no space at all.
    fstore.fst_posmode = F_PEOFPOSMODE; // Allocate from the physical end of file.
    fstore.fst_offset = (off_t)offset;
    fstore.fst_length = (off_t)length;
    fstore.fst_bytesalloc = 0; // output size, can be > length

    while ((result = fcntl(fileDescriptor, F_PREALLOCATE, &fstore)) == -1 && errno == EINTR);
#else
    (void)offset; // unused
    (void)length; // unused
    result = -1;
    errno = EOPNOTSUPP;
#endif

    assert(result == 0 || errno != EINVAL);

    return result;
}

int32_t SystemNative_Read(intptr_t fd, void* buffer, int32_t bufferSize)
{
    return Common_Read(fd, buffer, bufferSize);
}

int32_t SystemNative_ReadFromNonblocking(intptr_t fd, void* buffer, int32_t bufferSize)
{
    while (1)
    {
        int32_t result = Common_Read(fd, buffer, bufferSize);
        if (result != -1 || (errno != EAGAIN && errno != EWOULDBLOCK))
        {
            return result;
        }

        // The fd is non-blocking and no data is available yet.
        // Block (on a thread pool thread) until data arrives or the pipe/socket is closed.
        PollEvent pollEvent = { .FileDescriptor = (int32_t)fd, .Events = PAL_POLLIN, .TriggeredEvents = 0 };
        uint32_t triggered = 0;
        int32_t pollResult = Common_Poll(&pollEvent, 1, -1, &triggered);
        if (pollResult != Error_SUCCESS)
        {
            errno = ConvertErrorPalToPlatform(pollResult);
            return -1;
        }

        if ((pollEvent.TriggeredEvents & (PAL_POLLHUP | PAL_POLLERR)) != 0 &&
            (pollEvent.TriggeredEvents & PAL_POLLIN) == 0)
        {
            // The pipe/socket was closed with no data available (EOF).
            return 0;
        }
    }
}

int32_t SystemNative_WriteToNonblocking(intptr_t fd, const void* buffer, int32_t bufferSize)
{
    while (1)
    {
        int32_t result = Common_Write(fd, buffer, bufferSize);
        if (result != -1 || (errno != EAGAIN && errno != EWOULDBLOCK))
        {
            return result;
        }

        // The fd is non-blocking and the write buffer is full.
        // Block (on a thread pool thread) until space is available or the pipe/socket is closed.
        PollEvent pollEvent = { .FileDescriptor = (int32_t)fd, .Events = PAL_POLLOUT, .TriggeredEvents = 0 };
        uint32_t triggered = 0;
        int32_t pollResult = Common_Poll(&pollEvent, 1, -1, &triggered);
        if (pollResult != Error_SUCCESS)
        {
            errno = ConvertErrorPalToPlatform(pollResult);
            return -1;
        }

        if ((pollEvent.TriggeredEvents & (PAL_POLLHUP | PAL_POLLERR)) != 0 &&
            (pollEvent.TriggeredEvents & PAL_POLLOUT) == 0)
        {
            // The pipe/socket was closed.
            errno = EPIPE;
            return -1;
        }
    }
}

int32_t SystemNative_ReadLink(const char* path, char* buffer, int32_t bufferSize)
{
    assert(buffer != NULL || bufferSize == 0);
    assert(bufferSize >= 0);

    if (bufferSize <= 0)
    {
        errno = EINVAL;
        return -1;
    }

    ssize_t count = readlink(path, buffer, (size_t)bufferSize);
    assert(count >= -1 && count <= bufferSize);

    return (int32_t)count;
}

int32_t SystemNative_Rename(const char* oldPath, const char* newPath)
{
    int32_t result;
    while ((result = rename(oldPath, newPath)) < 0 && errno == EINTR);
    return result;
}

int32_t SystemNative_RmDir(const char* path)
{
    int32_t result;
    while ((result = rmdir(path)) < 0 && errno == EINTR);
    return result;
}

void SystemNative_Sync(void)
{
#if !defined(TARGET_WASI)
    sync();
#endif /* TARGET_WASI */
}

int32_t SystemNative_Write(intptr_t fd, const void* buffer, int32_t bufferSize)
{
    return Common_Write(fd, buffer, bufferSize);
}

#if !HAVE_FCOPYFILE
// Read all data from inFd and write it to outFd
static int32_t CopyFile_ReadWrite(int inFd, int outFd)
{
    // Allocate a buffer
    const int BufferLength = 80 * 1024 * sizeof(char);
    char* buffer = (char*)malloc(BufferLength);
    if (buffer == NULL)
    {
        return -1;
    }

    // Repeatedly read from the source and write to the destination
    while (true)
    {
        // Read up to what will fit in our buffer.  We're done if we get back 0 bytes.
        ssize_t bytesRead;
        while ((bytesRead = read(inFd, buffer, BufferLength)) < 0 && errno == EINTR);
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
            while ((bytesWritten = write(outFd, buffer + offset, (size_t)bytesRead)) < 0 && errno == EINTR);
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


#ifdef __linux__
static ssize_t CopyFileRange(int inFd, int outFd, size_t len)
{
    return syscall(__NR_copy_file_range, inFd, NULL, outFd, NULL, len, 0);
}

static bool SupportsCopyFileRange(void)
{
    static volatile int s_isSupported = 0;

    int isSupported = s_isSupported;
    if (isSupported == 0)
    {
        isSupported = -1;

        // Avoid known issues with copy_file_range that are fixed in Linux 5.3+ (https://lwn.net/Articles/789527/).
        struct utsname name;
        if (uname(&name) == 0)
        {
            unsigned int major = 0, minor = 0;
            sscanf(name.release, "%u.%u", &major, &minor);
            if (major > 5 || (major == 5 && minor >=3))
            {
                isSupported = CopyFileRange(-1, -1, 0) == -1 && errno != ENOSYS ? 1 : -1;
            }
        }

        s_isSupported = isSupported;
    }
    return isSupported == 1;
}
#endif

int32_t SystemNative_CopyFile(intptr_t sourceFd, intptr_t destinationFd, int64_t sourceLength)
{
    // unused on some platforms.
    (void)sourceLength;

    int inFd = ToFileDescriptor(sourceFd);
    int outFd = ToFileDescriptor(destinationFd);

#if HAVE_FCOPYFILE
    // If fcopyfile is available (OS X), try to use it, as the whole copy
    // can be performed in the kernel, without lots of unnecessary copying.
    // Copy data and metadata.
    return fcopyfile(inFd, outFd, NULL, COPYFILE_ALL) == 0 ? 0 : -1;
#else
    // Get the stats on the source file.
    int ret;
    bool copied = false;
    bool trySendFile = true;

    // Certain files (e.g. procfs) may return a size of 0 even though reading them will
    // produce data. We use plain read/write for those.
#ifdef FICLONE
    // Try copying data using a copy-on-write clone. This shares storage between the files.
    if (sourceLength != 0)
    {
#if HAVE_IOCTL_WITH_INT_REQUEST
        while ((ret = ioctl(outFd, (int)FICLONE, inFd)) < 0 && errno == EINTR);
#else
        while ((ret = ioctl(outFd, FICLONE, inFd)) < 0 && errno == EINTR);
#endif
        copied = ret == 0;
    }
#endif
#ifdef __linux__
    if (SupportsCopyFileRange() && !copied && sourceLength != 0)
    {
        do
        {
            size_t copyLength = (sourceLength >= SSIZE_MAX ? SSIZE_MAX : (size_t)sourceLength);
            ssize_t sent = CopyFileRange(inFd, outFd, copyLength);
            if (sent <= 0)
            {
                // sendfile will likely encounter the same error, don't try it.
                trySendFile = false;
                break; // Fall through.
            }
            else
            {
                assert(sent <= sourceLength);
                sourceLength -= sent;
            }
        } while (sourceLength > 0);

        copied = sourceLength == 0;
    }
#endif
#if HAVE_SENDFILE_4
    // Try copying the data using sendfile.
    if (trySendFile && !copied && sourceLength != 0)
    {
        // Note that per man page for large files, you have to iterate until the
        // whole file is copied (Linux has a limit of 0x7ffff000 bytes copied).
        do
        {
            size_t copyLength = (sourceLength >= SSIZE_MAX ? SSIZE_MAX : (size_t)sourceLength);
            ssize_t sent = sendfile(outFd, inFd, NULL, copyLength);
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
            else if (sent == 0)
            {
                // The file was truncated (or maybe some other condition occurred).
                // Perform the remaining copying using read/write.
                break;
            }
            else
            {
                assert(sent <= sourceLength);
                sourceLength -= sent;
            }
        } while (sourceLength > 0);

        copied = sourceLength == 0;
    }
#endif // HAVE_SENDFILE_4

    // Perform a manual copy.
    if (!copied && CopyFile_ReadWrite(inFd, outFd) != 0)
    {
        return -1;
    }

    // Copy file times.
    struct stat_ sourceStat;
    while ((ret = fstat_(inFd, &sourceStat)) < 0 && errno == EINTR);
    if (ret == 0)
    {
#if HAVE_FUTIMENS
        // futimens is preferred because it has a higher resolution.
        struct timespec origTimes[2];
        origTimes[0].tv_sec = (time_t)sourceStat.st_atime;
        origTimes[0].tv_nsec = ST_ATIME_NSEC(&sourceStat);
        origTimes[1].tv_sec = (time_t)sourceStat.st_mtime;
        origTimes[1].tv_nsec = ST_MTIME_NSEC(&sourceStat);
        while ((ret = futimens(outFd, origTimes)) < 0 && errno == EINTR);
#elif HAVE_FUTIMES
        struct timeval origTimes[2];
        origTimes[0].tv_sec = sourceStat.st_atime;
        origTimes[0].tv_usec = (int32_t)(ST_ATIME_NSEC(&sourceStat) / 1000);
        origTimes[1].tv_sec = sourceStat.st_mtime;
        origTimes[1].tv_usec = (int32_t)(ST_MTIME_NSEC(&sourceStat) / 1000);
        while ((ret = futimes(outFd, origTimes)) < 0 && errno == EINTR);
#endif
    }
    // If we copied to a filesystem (eg EXFAT) that does not preserve POSIX ownership, all files appear
    // to be owned by root. If we aren't running as root, then we won't be an owner of our new file, and
    // attempting to copy metadata to it will fail with EPERM. We have copied successfully, we just can't
    // copy metadata. The best thing we can do is skip copying the metadata.
    if (ret != 0 && errno != EPERM)
    {
        return -1;
    }

#if HAVE_FCHMOD
    // Copy permissions.
    // Even though managed code created the file with permissions matching those of the source file,
    // we need to copy permissions because the open permissions may be filtered by 'umask'.
    while ((ret = fchmod(outFd, sourceStat.st_mode & (S_IRWXU | S_IRWXG | S_IRWXO))) < 0 && errno == EINTR);
    if (ret != 0 && errno != EPERM) // See EPERM comment above
    {
        return -1;
    }
#endif /* HAVE_FCHMOD */

    return 0;
#endif // HAVE_FCOPYFILE
}

intptr_t SystemNative_INotifyInit(void)
{
#if HAVE_INOTIFY
    return inotify_init1(IN_CLOEXEC);
#else
    errno = ENOTSUP;
    return -1;
#endif
}

int32_t SystemNative_INotifyAddWatch(intptr_t fd, const char* pathName, uint32_t mask)
{
    assert(fd >= 0);
    assert(pathName != NULL);

#if HAVE_INOTIFY
#if !HAVE_IN_EXCL_UNLINK
    mask &= ~((uint32_t)PAL_IN_EXCL_UNLINK);
#endif
    return inotify_add_watch(ToFileDescriptor(fd), pathName, mask);
#else
    (void)fd, (void)pathName, (void)mask;
    errno = ENOTSUP;
    return -1;
#endif
}

int32_t SystemNative_INotifyRemoveWatch(intptr_t fd, int32_t wd)
{
    assert(fd >= 0);
    assert(wd >= 0);

#if HAVE_INOTIFY
    return inotify_rm_watch(
        ToFileDescriptor(fd),
#if INOTIFY_RM_WATCH_WD_UNSIGNED
        (uint32_t)wd);
#else
        wd);
#endif
#else
    (void)fd, (void)wd;
    errno = ENOTSUP;
    return -1;
#endif
}

int32_t SystemNative_GetPeerID(intptr_t socket, uid_t* euid)
{
    int fd = ToFileDescriptor(socket);

    // ucred causes Emscripten to fail even though it's defined,
    // but getting peer credentials won't work for WebAssembly anyway
    // ucred also causes OpeBSD to fail because the struct definition is named
    // differently and on OpenBSD we can use getpeereid(3) instead anyways.
#if defined(SO_PEERCRED) && !defined(TARGET_WASM) && !defined(TARGET_OPENBSD)
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

char* SystemNative_RealPath(const char* path)
{
    assert(path != NULL);
#if !defined(TARGET_WASI)
    return realpath(path, NULL);
#else /* TARGET_WASI */
    return NULL;
#endif /* TARGET_WASI */
}

#if !defined(TARGET_WASI)
static int16_t ConvertLockType(int16_t managedLockType)
{
    // the managed enum Interop.Sys.LockType has no 1:1 mapping with corresponding Unix values
    // which can be different per distro:
    // https://github.com/torvalds/linux/blob/fcadab740480e0e0e9fa9bd272acd409884d431a/arch/alpha/include/uapi/asm/fcntl.h#L48-L50
    // https://github.com/freebsd/freebsd-src/blob/fb8c2f743ab695f6004650b58bf96972e2535b20/sys/sys/fcntl.h#L277-L279
    switch (managedLockType)
    {
        case 0:
            return F_RDLCK;
        case 1:
            return F_WRLCK;
        default:
            assert_msg(managedLockType == 2, "Unknown Lock Type", (int)managedLockType);
            return F_UNLCK;
    }
}

#if HAVE_STATFS_FSTYPENAME || HAVE_STATVFS_BASETYPE || defined(TARGET_HAIKU)
static uint32_t FileSystemNameSupportsLocking(const char* fileSystemName)
{
    if (strcmp(fileSystemName, "nfs") == 0 ||
        strcmp(fileSystemName, "cifs") == 0 ||
        strcmp(fileSystemName, "smb") == 0 ||
        strcmp(fileSystemName, "smb2") == 0)
    {
        return 0;
    }
    return 1;
}
#endif
#endif /* TARGET_WASI */

// LOCK_SH does not work well for write access on nfs/cifs/samba. For example, writes are dropped silently.
// See https://github.com/dotnet/runtime/issues/44546 and https://github.com/dotnet/runtime/issues/53182.
uint32_t SystemNative_FileSystemSupportsLocking(intptr_t fd, int32_t lockOperation, int32_t accessWrite)
{
    assert(lockOperation == PAL_LOCK_SH || lockOperation == PAL_LOCK_EX);
#if defined(TARGET_WASI) || defined(TARGET_WASM)
    return 0; // WASI/WASM doesn't support locking.
#else
    if (lockOperation == PAL_LOCK_EX || accessWrite == 0)
    {
        return 1;
    }
#if defined(TARGET_HAIKU)
    struct stat st;
    int fstatRes;
    while ((fstatRes = fstat(ToFileDescriptor(fd), &st)) == -1 && errno == EINTR);
    if (fstatRes == -1) return 0;

    struct fs_info info;
    int fsStatDevRes;
    while ((fsStatDevRes = fs_stat_dev(st.st_dev, &info)) == -1 && errno == EINTR);
    if (fsStatDevRes == -1) return 0;

    return FileSystemNameSupportsLocking(info.fsh_name);
#elif HAVE_STATFS_FSTYPENAME || defined(TARGET_LINUX)
    int statfsRes;
    struct statfs statfsArgs;
    // for our needs (get file system type) statfs is always enough and there is no need to use statfs64
    // which got deprecated in macOS 10.6, in favor of statfs
    while ((statfsRes = fstatfs(ToFileDescriptor(fd), &statfsArgs)) == -1 && errno == EINTR) ;
    if (statfsRes == -1) return 0;

#if HAVE_STATFS_FSTYPENAME
    return FileSystemNameSupportsLocking(statfsArgs.f_fstypename);
#elif defined(TARGET_LINUX)
    unsigned int f_type = (unsigned int)statfsArgs.f_type;
    if (f_type == 0x6969 ||     // NFS_SUPER_MAGIC
        f_type == 0xFF534D42 || // CIFS_SUPER_MAGIC
        f_type == 0x517B ||     // SMB_SUPER_MAGIC
        f_type == 0xFE534D42)   // SMB2_SUPER_MAGIC
    {
        return 0;
    }
    return 1;
#endif
#elif HAVE_STATVFS_BASETYPE
    int statfsRes;
    struct statvfs statfsArgs;
    while ((statfsRes = fstatvfs(ToFileDescriptor(fd), &statfsArgs)) == -1 && errno == EINTR) ;
    if (statfsRes == -1) return 0;

    return FileSystemNameSupportsLocking(statfsArgs.f_basetype);
#else
    #error "Platform doesn't support fstatfs or fstatvfs"
#endif
#endif
}

int32_t SystemNative_LockFileRegion(intptr_t fd, int64_t offset, int64_t length, int16_t lockType)
{
#if !defined(TARGET_WASI)
    int16_t unixLockType = ConvertLockType(lockType);
    if (offset < 0 || length < 0)
    {
        errno = EINVAL;
        return -1;
    }

#if HAVE_FLOCK64
    struct flock64 lockArgs;
#else
    struct flock lockArgs;
#endif

#if defined(TARGET_ANDROID) && HAVE_FLOCK64
    // On Android, fcntl is always implemented by fcntl64 but before https://github.com/aosp-mirror/platform_bionic/commit/09e77f35ab8d291bf88302bb9673aaa518c6bcb0
    // there was no remapping of F_SETLK to F_SETLK64 when _FILE_OFFSET_BITS=64 (which we set in eng/native/configurecompiler.cmake) so we need to always pass F_SETLK64
    int command = F_SETLK64;
#else
    int command = F_SETLK;
#endif

    lockArgs.l_type = unixLockType;
    lockArgs.l_whence = SEEK_SET;
    lockArgs.l_start = (off_t)offset;
    lockArgs.l_len = (off_t)length;

    int32_t ret;
    while ((ret = fcntl (ToFileDescriptor(fd), command, &lockArgs)) < 0 && errno == EINTR);
    return ret;
#else /* TARGET_WASI */
    return EINTR;
#endif /* TARGET_WASI */
}

int32_t SystemNative_LChflags(const char* path, uint32_t flags)
{
#if HAVE_LCHFLAGS
    int32_t result;
    while ((result = lchflags(path, flags)) < 0 && errno == EINTR);
    return result;
#else
    (void)path, (void)flags;
    errno = ENOTSUP;
    return -1;
#endif
}

int32_t SystemNative_FChflags(intptr_t fd, uint32_t flags)
{
#if HAVE_LCHFLAGS
    int32_t result;
    while ((result = fchflags(ToFileDescriptor(fd), flags)) < 0 && errno == EINTR);
    return result;
#else
    (void)fd, (void)flags;
    errno = ENOTSUP;
    return -1;
#endif
}

int32_t SystemNative_LChflagsCanSetHiddenFlag(void)
{
#if HAVE_LCHFLAGS
    return SystemNative_CanGetHiddenFlag();
#else
    return false;
#endif
}

int32_t SystemNative_CanGetHiddenFlag(void)
{
#if HAVE_STAT_FLAGS && defined(UF_HIDDEN)
    return true;
#else
    return false;
#endif
}

int32_t SystemNative_ReadThreadInfo(int32_t pid, int32_t tid, ThreadInfo* threadInfo)
{
#ifdef __sun
    char infoFilename[64];
    snprintf(infoFilename, sizeof(infoFilename), "/proc/%d/lwp/%d/lwpsinfo", pid, tid);

    intptr_t fd;
    while ((fd = open(infoFilename, O_RDONLY)) < 0 && errno == EINTR);
    if (fd < 0)
    {
        return 0;
    }

    lwpsinfo_t pr;
    int result = Common_Read(fd, &pr, sizeof(pr));
    close(ToFileDescriptor(fd));
    if (result < (int)sizeof(pr))
    {
        errno = EIO;
        return -1;
    }

    threadInfo->Tid = pr.pr_lwpid;
    threadInfo->Priority = pr.pr_pri;
    threadInfo->NiceVal = pr.pr_nice;
    // Status code, a char: ...
    threadInfo->StatusCode = (uchar_t)pr.pr_sname;
    // Thread start time and CPU time
    threadInfo->StartTime = pr.pr_start.tv_sec;
    threadInfo->StartTimeNsec = pr.pr_start.tv_nsec;
    threadInfo->CpuTotalTime = pr.pr_time.tv_sec;
    threadInfo->CpuTotalTimeNsec = pr.pr_time.tv_nsec;

    return 0;
#else
    (void)pid, (void)tid, (void)threadInfo;
    errno = ENOTSUP;
    return -1;
#endif // __sun
}

// The struct passing is limited, so the args string is handled separately here.
int32_t SystemNative_ReadProcessInfo(int32_t pid, ProcessInfo* processInfo, uint8_t *argBuf, int32_t argBufSize)
{
#ifdef __sun
    if (argBufSize != 0 && argBufSize < PRARGSZ)
    {
        errno = EINVAL;
        return -1;
    }

    char infoFilename[64];
    snprintf(infoFilename, sizeof(infoFilename), "/proc/%d/psinfo", pid);

    intptr_t fd;
    while ((fd = open(infoFilename, O_RDONLY)) < 0 && errno == EINTR);
    if (fd < 0)
    {
        return 0;
    }

    psinfo_t pr;
    int result = Common_Read(fd, &pr, sizeof(pr));
    close(ToFileDescriptor(fd));
    if (result < (int)sizeof(pr))
    {
        errno = EIO;
        return -1;
    }

    processInfo->Pid = pr.pr_pid;
    processInfo->ParentPid = pr.pr_ppid;
    processInfo->SessionId = pr.pr_sid;
    processInfo->Priority = pr.pr_lwp.pr_pri;
    processInfo->NiceVal = pr.pr_lwp.pr_nice;
    // pr_size and pr_rsize are in Kbytes.
    processInfo->VirtualSize = (uint64_t)pr.pr_size * 1024;
    processInfo->ResidentSetSize = (uint64_t)pr.pr_rssize * 1024;
    processInfo->StartTime = pr.pr_start.tv_sec;
    processInfo->StartTimeNsec = pr.pr_start.tv_nsec;
    processInfo->CpuTotalTime = pr.pr_time.tv_sec;
    processInfo->CpuTotalTimeNsec = pr.pr_time.tv_nsec;

    if (argBuf != NULL && argBufSize != 0)
    {
        SafeStringCopy((char*)argBuf, PRARGSZ, pr.pr_psargs);
    }

    return 0;
#else
    (void)pid, (void)processInfo, (void)argBuf, (void)argBufSize;
    errno = ENOTSUP;
    return -1;
#endif // __sun
}

int32_t SystemNative_PRead(intptr_t fd, void* buffer, int32_t bufferSize, int64_t fileOffset)
{
    assert(buffer != NULL);
    assert(bufferSize >= 0);

    ssize_t count;
    while ((count = pread(ToFileDescriptor(fd), buffer, (uint32_t)bufferSize, (off_t)fileOffset)) < 0 && errno == EINTR);

    assert(count >= -1 && count <= bufferSize);
    return (int32_t)count;
}

int32_t SystemNative_PWrite(intptr_t fd, void* buffer, int32_t bufferSize, int64_t fileOffset)
{
    assert(buffer != NULL);
    assert(bufferSize >= 0);

    ssize_t count;
    while ((count = pwrite(ToFileDescriptor(fd), buffer, (uint32_t)bufferSize, (off_t)fileOffset)) < 0 && errno == EINTR);

    assert(count >= -1 && count <= bufferSize);
    return (int32_t)count;
}

static int GetAllowedVectorCount(IOVector* vectors, int32_t vectorCount)
{
#if defined(IOV_MAX)
    const int IovMax = IOV_MAX;
#else
    // In theory all the platforms that we support define IOV_MAX,
    // but we want to be extra safe and provde a fallback
    // in case it turns out to not be true.
    // 16 is low, but supported on every platform.
    const int IovMax = 16;
#endif

    int allowedCount = (int)vectorCount;

    // We need to respect the limit of items that can be passed in iov.
    // In case of writes, the managed code is responsible for handling incomplete writes.
    // In case of reads, we simply returns the number of bytes read and it's up to the users.
    if (IovMax < allowedCount)
    {
        allowedCount = IovMax;
    }

#if defined(TARGET_APPLE)
    // For macOS preadv and pwritev can fail with EINVAL when the total length
    // of all vectors overflows a 32-bit integer.
    size_t totalLength = 0;
    for (int i = 0; i < allowedCount; i++)
    {
        assert(INT_MAX >= vectors[i].Count);

        totalLength += vectors[i].Count;

        if (totalLength > INT_MAX)
        {
            allowedCount = i;
            break;
        }
    }
#else
    (void)vectors;
#endif

    return allowedCount;
}

int64_t SystemNative_ReadV(intptr_t fd, IOVector* vectors, int32_t vectorCount)
{
    assert(vectors != NULL);
    assert(vectorCount >= 0);

    int fileDescriptor = ToFileDescriptor(fd);
    int allowedVectorCount = GetAllowedVectorCount(vectors, vectorCount);

    while (1)
    {
        int64_t count;
        while ((count = readv(fileDescriptor, (struct iovec*)vectors, allowedVectorCount)) < 0 && errno == EINTR);

        if (count != -1 || (errno != EAGAIN && errno != EWOULDBLOCK))
        {
            assert(count >= -1);
            return count;
        }

        // The fd is non-blocking and no data is available yet.
        // Block (on a thread pool thread) until data arrives or the pipe/socket is closed.
        PollEvent pollEvent = { .FileDescriptor = fileDescriptor, .Events = PAL_POLLIN, .TriggeredEvents = 0 };
        uint32_t triggered = 0;
        int32_t pollResult = Common_Poll(&pollEvent, 1, -1, &triggered);
        if (pollResult != Error_SUCCESS)
        {
            errno = ConvertErrorPalToPlatform(pollResult);
            return -1;
        }

        if ((pollEvent.TriggeredEvents & (PAL_POLLHUP | PAL_POLLERR)) != 0 &&
            (pollEvent.TriggeredEvents & PAL_POLLIN) == 0)
        {
            // The pipe/socket was closed with no data available (EOF).
            return 0;
        }
    }
}

int64_t SystemNative_PReadV(intptr_t fd, IOVector* vectors, int32_t vectorCount, int64_t fileOffset)
{
    assert(vectors != NULL);
    assert(vectorCount >= 0);

    int64_t count = 0;
    int fileDescriptor = ToFileDescriptor(fd);
#if HAVE_PREADV && !defined(TARGET_WASM) // preadv is buggy on WASM
    int allowedVectorCount = GetAllowedVectorCount(vectors, vectorCount);
    while ((count = preadv(fileDescriptor, (struct iovec*)vectors, allowedVectorCount, (off_t)fileOffset)) < 0 && errno == EINTR);
#else
    int64_t current;
    for (int i = 0; i < vectorCount; i++)
    {
        IOVector vector = vectors[i];
        while ((current = pread(fileDescriptor, vector.Base, vector.Count, (off_t)(fileOffset + count))) < 0 && errno == EINTR);

        if (current < 0)
        {
            // if previous calls were successful, we return what we got so far
            // otherwise, we return the error code
            return count > 0 ? count : current;
        }

        count += current;

        // Incomplete pread operation may happen for two reasons:
        // a) We have reached EOF.
        // b) The operation was interrupted by a signal handler.
        // To mimic preadv, we stop on the first incomplete operation.
        if (current != (int64_t)vector.Count)
        {
            return count;
        }
    }
#endif

    assert(count >= -1);
    return count;
}

int64_t SystemNative_WriteV(intptr_t fd, IOVector* vectors, int32_t vectorCount)
{
    assert(vectors != NULL);
    assert(vectorCount >= 0);

    int fileDescriptor = ToFileDescriptor(fd);
    int allowedVectorCount = GetAllowedVectorCount(vectors, vectorCount);

    while (1)
    {
        int64_t count;
        while ((count = writev(fileDescriptor, (struct iovec*)vectors, allowedVectorCount)) < 0 && errno == EINTR);

        if (count != -1 || (errno != EAGAIN && errno != EWOULDBLOCK))
        {
            assert(count >= -1);
            return count;
        }

        // The fd is non-blocking and the write buffer is full.
        // Block (on a thread pool thread) until space is available or the pipe/socket is closed.
        PollEvent pollEvent = { .FileDescriptor = fileDescriptor, .Events = PAL_POLLOUT, .TriggeredEvents = 0 };
        uint32_t triggered = 0;
        int32_t pollResult = Common_Poll(&pollEvent, 1, -1, &triggered);
        if (pollResult != Error_SUCCESS)
        {
            errno = ConvertErrorPalToPlatform(pollResult);
            return -1;
        }

        if ((pollEvent.TriggeredEvents & (PAL_POLLHUP | PAL_POLLERR)) != 0 &&
            (pollEvent.TriggeredEvents & PAL_POLLOUT) == 0)
        {
            // The pipe/socket was closed.
            errno = EPIPE;
            return -1;
        }
    }
}

int64_t SystemNative_PWriteV(intptr_t fd, IOVector* vectors, int32_t vectorCount, int64_t fileOffset)
{
    assert(vectors != NULL);
    assert(vectorCount >= 0);

    int64_t count = 0;
    int fileDescriptor = ToFileDescriptor(fd);
#if HAVE_PWRITEV && !defined(TARGET_WASM) // pwritev is buggy on WASM
    int allowedVectorCount = GetAllowedVectorCount(vectors, vectorCount);
    while ((count = pwritev(fileDescriptor, (struct iovec*)vectors, allowedVectorCount, (off_t)fileOffset)) < 0 && errno == EINTR);
#else
    int64_t current;
    for (int i = 0; i < vectorCount; i++)
    {
        IOVector vector = vectors[i];
        while ((current = pwrite(fileDescriptor, vector.Base, vector.Count, (off_t)(fileOffset + count))) < 0 && errno == EINTR);

        if (current < 0)
        {
            // if previous calls were successful, we return what we got so far
            // otherwise, we return the error code
            return count > 0 ? count : current;
        }

        count += current;

        // Incomplete pwrite operation may happen for few reasons:
        // a) There was not enough space available or the file is too large for given file system.
        // b) The operation was interrupted by a signal handler.
        // To mimic pwritev, we stop on the first incomplete operation.
        if (current != (int64_t)vector.Count)
        {
            return count;
        }
    }
#endif

    assert(count >= -1);
    return count;
}


// io_uring PAL implementation.
//
// This talks to the kernel via raw io_uring_setup/io_uring_enter syscalls (no liburing
// dependency), matching the existing pattern used for copy_file_range above. The submission
// queue (SQ), completion queue (CQ) and SQE array are shared memory regions mapped with mmap;
// the head/tail indices in those regions are accessed with acquire/release semantics since they
// are also read/written by the kernel.
//
// Submission and the io_uring_enter(2) syscall that actually asks the kernel to process pending
// entries are deliberately split into two PAL entrypoints (SystemNative_IoRingSubmit /
// SystemNative_IoRingKick): filling SQEs and publishing them to the SQ tail only touches this
// ring's local (non-atomic) submission-queue bookkeeping and must be serialized by the caller
// (e.g. via a lock), but io_uring_enter(2) itself is safe to call concurrently from multiple
// threads for a ring created without IORING_SETUP_SINGLE_ISSUER (the kernel serializes access to
// the ring internally) - so it is called outside of whatever lock protects the enqueue step.

#if HAVE_LINUX_IO_URING_H

typedef struct
{
    int Fd;

    void* SqRingPtr;
    size_t SqRingSize;
    void* CqRingPtr;
    size_t CqRingSize;
    void* SqesPtr;
    size_t SqesSize;

    uint32_t* SqHead;
    uint32_t* SqTail;
    uint32_t* SqRingMask;
    uint32_t* SqArray;
    uint32_t SqEntries;
    struct io_uring_sqe* Sqes;

    uint32_t* CqHead;
    uint32_t* CqTail;
    uint32_t* CqRingMask;
    struct io_uring_cqe* Cqes;
} IoRing;

static long IoUringSetup(uint32_t entries, struct io_uring_params* params)
{
    return syscall(__NR_io_uring_setup, entries, params);
}

static long IoUringEnter(int fd, uint32_t toSubmit, uint32_t minComplete, uint32_t flags)
{
    long result;
    while ((result = syscall(__NR_io_uring_enter, fd, toSubmit, minComplete, flags, NULL, (size_t)0)) < 0 && errno == EINTR);
    return result;
}

static void IoRingFillSqe(struct io_uring_sqe* sqe, IoRingRequest* request)
{
    memset(sqe, 0, sizeof(*sqe));
    sqe->fd = (int32_t)request->Fd;
    sqe->user_data = request->UserData;

    switch ((IoRingOp)request->OpCode)
    {
        case IoRingOp_Read:
            // A negative Offset means "non-positional": io_uring treats an off of -1 for
            // READ/WRITE/READV/WRITEV as "use (and update) the file's current position", just
            // like plain read(2)/write(2)/readv(2)/writev(2). This field is only meaningful for
            // these file-offset opcodes: for Recv/Send/Accept/Connect below, sqe->off aliases
            // other fields (or is unused), and the kernel rejects those opcodes with EINVAL
            // unless it is left at 0 (its memset'd default), so it must not be set here.
            sqe->off = request->Offset >= 0 ? (uint64_t)request->Offset : (uint64_t)-1;
            sqe->opcode = IORING_OP_READ;
            sqe->addr = (uint64_t)(uintptr_t)request->Buffer;
            sqe->len = (uint32_t)request->BufferLength;
            break;
        case IoRingOp_Write:
            sqe->off = request->Offset >= 0 ? (uint64_t)request->Offset : (uint64_t)-1;
            sqe->opcode = IORING_OP_WRITE;
            sqe->addr = (uint64_t)(uintptr_t)request->Buffer;
            sqe->len = (uint32_t)request->BufferLength;
            break;
        case IoRingOp_ReadV:
            sqe->off = request->Offset >= 0 ? (uint64_t)request->Offset : (uint64_t)-1;
            sqe->opcode = IORING_OP_READV;
            sqe->addr = (uint64_t)(uintptr_t)request->Vectors;
            // Just like plain readv(2)/writev(2) (see GetAllowedVectorCount above), io_uring
            // rejects IORING_OP_READV/WRITEV with more than IOV_MAX vectors (EINVAL). The managed
            // caller is responsible for handling a resulting short read/write by resubmitting the
            // remainder, the same way it already does for the non-io_uring PReadV/PWriteV path.
            sqe->len = (uint32_t)GetAllowedVectorCount(request->Vectors, request->VectorCount);
            break;
        case IoRingOp_WriteV:
            sqe->off = request->Offset >= 0 ? (uint64_t)request->Offset : (uint64_t)-1;
            sqe->opcode = IORING_OP_WRITEV;
            sqe->addr = (uint64_t)(uintptr_t)request->Vectors;
            sqe->len = (uint32_t)GetAllowedVectorCount(request->Vectors, request->VectorCount);
            break;
        case IoRingOp_Accept:
            // addr = output sockaddr*, addr2 (aliased with off) = output socklen_t* (peer address
            // length written back by the kernel on completion), accept_flags = flags.
            sqe->opcode = IORING_OP_ACCEPT;
            sqe->addr = (uint64_t)(uintptr_t)request->SockAddr;
            sqe->off = (uint64_t)(uintptr_t)request->SockAddrLen;
            sqe->accept_flags = (uint32_t)request->Flags;
            break;
        case IoRingOp_Connect:
            // addr = input sockaddr*, off (aliased with addr2) = input addrlen (by value, not a
            // pointer - unlike Accept's SockAddrLen).
            sqe->opcode = IORING_OP_CONNECT;
            sqe->addr = (uint64_t)(uintptr_t)request->SockAddr;
            sqe->off = request->SockAddrLen != NULL ? (uint64_t)(*request->SockAddrLen) : 0;
            break;
        case IoRingOp_Recv:
            sqe->opcode = IORING_OP_RECV;
            sqe->addr = (uint64_t)(uintptr_t)request->Buffer;
            sqe->len = (uint32_t)request->BufferLength;
            sqe->msg_flags = (uint32_t)request->Flags;
            break;
        case IoRingOp_Send:
            sqe->opcode = IORING_OP_SEND;
            sqe->addr = (uint64_t)(uintptr_t)request->Buffer;
            sqe->len = (uint32_t)request->BufferLength;
            sqe->msg_flags = (uint32_t)request->Flags;
            break;
    }
}

#endif // HAVE_LINUX_IO_URING_H

int32_t SystemNative_IoRingIsAvailable(void)
{
#if HAVE_LINUX_IO_URING_H
    static volatile int s_isAvailable = 0;

    int isAvailable = s_isAvailable;
    if (isAvailable == 0)
    {
        struct io_uring_params params;
        memset(&params, 0, sizeof(params));

        // A minimal ring is enough to probe support (kernel version, seccomp, sysctl, etc.)
        // without leaving any lasting state behind.
        long result = IoUringSetup(2, &params);
        if (result >= 0)
        {
            close((int)result);
            isAvailable = 1;
        }
        else
        {
            isAvailable = -1;
        }

        s_isAvailable = isAvailable;
    }

    return isAvailable == 1 ? 1 : 0;
#else
    return 0;
#endif
}

int32_t SystemNative_IoRingCreate(int32_t submissionQueueDepth, int32_t completionQueueDepth, int32_t singleIssuer, intptr_t* ringHandle)
{
    assert(ringHandle != NULL);
    *ringHandle = 0;

#if HAVE_LINUX_IO_URING_H
    if (submissionQueueDepth <= 0)
    {
        errno = EINVAL;
        return -1;
    }

    struct io_uring_params params;
    memset(&params, 0, sizeof(params));
    if (completionQueueDepth > 0)
    {
        params.flags |= IORING_SETUP_CQSIZE;
        params.cq_entries = (uint32_t)completionQueueDepth;
    }

    // If the caller asks for it, request IORING_SETUP_SINGLE_ISSUER together with
    // IORING_SETUP_DEFER_TASKRUN: from this point on, the kernel requires every
    // SystemNative_IoRingSubmit/SystemNative_IoRingKick/SystemNative_IoRingWaitForCompletions call
    // for this ring to come from the same single OS thread (whichever one happens to make the
    // first such call) - any other thread that tries gets -EEXIST. This lets the kernel skip an
    // internal ring-wide lock it would otherwise need to serialize concurrent submitters/reapers,
    // which is exactly the contention this pair of flags exists to avoid. DEFER_TASKRUN requires
    // SINGLE_ISSUER and additionally defers completion task-work until that same owning thread
    // calls io_uring_enter(2) with IORING_ENTER_GETEVENTS (see SystemNative_IoRingWaitForCompletions);
    // it is requested together with SINGLE_ISSUER here (never SINGLE_ISSUER alone) because a plain
    // SINGLE_ISSUER ring still requires every io_uring_enter call - including a GETEVENTS-only
    // completion wait with nothing to submit - to come from that same fixed thread, so trying to
    // let completion-reaping rotate across arbitrary threads (as a plain shared, non-SINGLE_ISSUER
    // ring allows) does not work: whichever thread happens to call in first permanently becomes
    // the ring's issuer, and every other thread's calls then fail with -EEXIST forever. Given that,
    // submission and completion-reaping must already be pinned to one fixed thread whenever
    // SINGLE_ISSUER is requested, so also requesting DEFER_TASKRUN is free extra performance with
    // no additional constraint over what SINGLE_ISSUER alone already forces on the caller.
#if defined(IORING_SETUP_SINGLE_ISSUER) && defined(IORING_SETUP_DEFER_TASKRUN)
    if (singleIssuer != 0)
    {
        params.flags |= IORING_SETUP_SINGLE_ISSUER | IORING_SETUP_DEFER_TASKRUN;
    }
#else
    // Older kernel headers used at compile time may not define these flags at all; silently ignore
    // the request rather than failing the build. The caller-side contract (only ever submit to /
    // reap from one dedicated thread) still holds; it just won't be kernel-enforced on such a
    // system, and the ring won't get the associated lock-elision benefit.
    (void)singleIssuer;
#endif

    long fd = IoUringSetup((uint32_t)submissionQueueDepth, &params);


    if (fd < 0)
    {
        return -1;
    }

    IoRing* ring = (IoRing*)calloc(1, sizeof(IoRing));
    if (ring == NULL)
    {
        close((int)fd);
        errno = ENOMEM;
        return -1;
    }

    ring->Fd = (int)fd;

    size_t sqRingSize = (size_t)params.sq_off.array + (size_t)params.sq_entries * sizeof(uint32_t);
    size_t cqRingSize = (size_t)params.cq_off.cqes + (size_t)params.cq_entries * sizeof(struct io_uring_cqe);
    size_t sqesSize = (size_t)params.sq_entries * sizeof(struct io_uring_sqe);

    void* sqRingPtr = mmap(NULL, sqRingSize, PROT_READ | PROT_WRITE, MAP_SHARED | MAP_POPULATE, ring->Fd, (off_t)IORING_OFF_SQ_RING);
    void* cqRingPtr = mmap(NULL, cqRingSize, PROT_READ | PROT_WRITE, MAP_SHARED | MAP_POPULATE, ring->Fd, (off_t)IORING_OFF_CQ_RING);
    void* sqesPtr = mmap(NULL, sqesSize, PROT_READ | PROT_WRITE, MAP_SHARED | MAP_POPULATE, ring->Fd, (off_t)IORING_OFF_SQES);

    if (sqRingPtr == MAP_FAILED || cqRingPtr == MAP_FAILED || sqesPtr == MAP_FAILED)
    {
        int savedErrno = errno;
        if (sqRingPtr != MAP_FAILED) munmap(sqRingPtr, sqRingSize);
        if (cqRingPtr != MAP_FAILED) munmap(cqRingPtr, cqRingSize);
        if (sqesPtr != MAP_FAILED) munmap(sqesPtr, sqesSize);
        close(ring->Fd);
        free(ring);
        errno = savedErrno;
        return -1;
    }

    ring->SqRingPtr = sqRingPtr;
    ring->SqRingSize = sqRingSize;
    ring->CqRingPtr = cqRingPtr;
    ring->CqRingSize = cqRingSize;
    ring->SqesPtr = sqesPtr;
    ring->SqesSize = sqesSize;

    ring->SqHead = (uint32_t*)((uint8_t*)sqRingPtr + params.sq_off.head);
    ring->SqTail = (uint32_t*)((uint8_t*)sqRingPtr + params.sq_off.tail);
    ring->SqRingMask = (uint32_t*)((uint8_t*)sqRingPtr + params.sq_off.ring_mask);
    ring->SqArray = (uint32_t*)((uint8_t*)sqRingPtr + params.sq_off.array);
    ring->SqEntries = params.sq_entries;
    ring->Sqes = (struct io_uring_sqe*)sqesPtr;

    ring->CqHead = (uint32_t*)((uint8_t*)cqRingPtr + params.cq_off.head);
    ring->CqTail = (uint32_t*)((uint8_t*)cqRingPtr + params.cq_off.tail);
    ring->CqRingMask = (uint32_t*)((uint8_t*)cqRingPtr + params.cq_off.ring_mask);
    ring->Cqes = (struct io_uring_cqe*)((uint8_t*)cqRingPtr + params.cq_off.cqes);

    *ringHandle = (intptr_t)ring;
    return 0;
#else
    (void)submissionQueueDepth, (void)completionQueueDepth;
    errno = ENOTSUP;
    return -1;
#endif
}

int32_t SystemNative_IoRingSubmit(intptr_t ringHandle, IoRingRequest* requests, int32_t requestCount, int32_t* submittedCount)
{
    assert(requests != NULL);
    assert(requestCount >= 0);
    assert(submittedCount != NULL);
    *submittedCount = 0;

#if HAVE_LINUX_IO_URING_H
    IoRing* ring = (IoRing*)ringHandle;
    if (ring == NULL)
    {
        errno = EINVAL;
        return -1;
    }

    // Single producer: the caller is responsible for ensuring only one thread submits to a
    // given ring at a time (see the io_uring PAL design notes).
    uint32_t sqTail = *ring->SqTail;
    uint32_t sqHead = __atomic_load_n(ring->SqHead, __ATOMIC_ACQUIRE);
    uint32_t sqMask = *ring->SqRingMask;

    int32_t queued = 0;
    for (int32_t i = 0; i < requestCount; i++)
    {
        if (sqTail - sqHead >= ring->SqEntries)
        {
            // The submission queue is full; stop here. The caller should retry the
            // remaining requests (requests[queued..requestCount)) once there's more room.
            break;
        }

        uint32_t index = sqTail & sqMask;
        IoRingFillSqe(&ring->Sqes[index], &requests[i]);
        ring->SqArray[index] = index;

        sqTail++;
        queued++;
    }

    if (queued == 0)
    {
        return 0;
    }

    __atomic_store_n(ring->SqTail, sqTail, __ATOMIC_RELEASE);

    // The entries above are now published via the SQ tail and visible to the kernel; this
    // cannot be undone. Deliberately do NOT call io_uring_enter here - see
    // SystemNative_IoRingKick. Doing the (relatively expensive, and otherwise-unnecessary-to-
    // serialize) syscall outside of whatever lock protects this enqueue step lets many
    // threads publish new entries into a shared ring quickly, without each blocking the next
    // behind a full syscall while holding that lock.
    *submittedCount = queued;
    return 0;
#else
    (void)ringHandle, (void)requests, (void)requestCount;
    errno = ENOTSUP;
    return -1;
#endif
}

int32_t SystemNative_IoRingKick(intptr_t ringHandle)
{
#if HAVE_LINUX_IO_URING_H
    IoRing* ring = (IoRing*)ringHandle;
    if (ring == NULL)
    {
        errno = EINVAL;
        return -1;
    }

    // Ask the kernel to consume as many currently-enqueued-but-not-yet-submitted entries as
    // possible. Passing the full ring depth (rather than trying to track exactly how many are
    // new) is safe: io_uring_enter only ever consumes what is actually available between its
    // own internal submission cursor and the current SQ tail, capped at to_submit - so this is
    // simply "submit everything pending" and never over-consumes or double-processes entries.
    // A negative/short result is not an error here: if another thread's concurrent kick (or
    // the driver's own waiting enter call) already consumed everything, this call legitimately
    // has nothing to do and that is not a failure.
    IoUringEnter(ring->Fd, ring->SqEntries, 0, 0);
    return 0;
#else
    (void)ringHandle;
    errno = ENOTSUP;
    return -1;
#endif
}

int32_t SystemNative_IoRingWaitForCompletions(intptr_t ringHandle, IoRingCompletion* completions, int32_t maxCompletions, int32_t minComplete, int32_t* completedCount)
{
    assert(completions != NULL);
    assert(maxCompletions >= 0);
    assert(minComplete >= 0);
    assert(completedCount != NULL);
    *completedCount = 0;

#if HAVE_LINUX_IO_URING_H
    IoRing* ring = (IoRing*)ringHandle;
    if (ring == NULL)
    {
        errno = EINVAL;
        return -1;
    }

    if (minComplete > 0)
    {
        long result = IoUringEnter(ring->Fd, 0, (uint32_t)minComplete, IORING_ENTER_GETEVENTS);
        if (result < 0)
        {
            return -1;
        }
    }
    else
    {
        // Always issue a plain (non-blocking-for-events, minComplete: 0) IORING_ENTER_GETEVENTS
        // call, even when the caller isn't waiting for anything in particular. This matters for
        // rings created with IORING_SETUP_DEFER_TASKRUN (see SystemNative_IoRingCreate): that flag
        // defers completion task-work until the ring's owning thread explicitly calls
        // io_uring_enter(2) with IORING_ENTER_GETEVENTS - without this call, completions would
        // never be posted to the CQ ring at all, no matter how long the caller waits afterwards or
        // how many times it re-reads the CQ tail. For rings not created with DEFER_TASKRUN this call
        // is a harmless, cheap no-op when nothing is ready.
        IoUringEnter(ring->Fd, 0, 0, IORING_ENTER_GETEVENTS);
    }

    // Single consumer: the caller is responsible for ensuring only one thread reaps
    // completions from a given ring at a time (see the io_uring PAL design notes).
    uint32_t cqHead = *ring->CqHead;
    uint32_t cqTail = __atomic_load_n(ring->CqTail, __ATOMIC_ACQUIRE);
    uint32_t cqMask = *ring->CqRingMask;

    int32_t count = 0;
    while (cqHead != cqTail && count < maxCompletions)
    {
        struct io_uring_cqe* cqe = &ring->Cqes[cqHead & cqMask];
        completions[count].UserData = cqe->user_data;
        completions[count].Result = cqe->res;
        completions[count].Flags = cqe->flags;

        cqHead++;
        count++;
    }

    if (count > 0)
    {
        __atomic_store_n(ring->CqHead, cqHead, __ATOMIC_RELEASE);
    }

    *completedCount = count;
    return 0;
#else
    (void)ringHandle, (void)completions, (void)maxCompletions, (void)minComplete;
    errno = ENOTSUP;
    return -1;
#endif
}

int32_t SystemNative_IoRingClose(intptr_t ringHandle)
{
#if HAVE_LINUX_IO_URING_H
    IoRing* ring = (IoRing*)ringHandle;
    if (ring == NULL)
    {
        return 0;
    }

    int result = 0;
    if (munmap(ring->SqesPtr, ring->SqesSize) != 0)
    {
        result = -1;
    }
    if (munmap(ring->CqRingPtr, ring->CqRingSize) != 0)
    {
        result = -1;
    }
    if (munmap(ring->SqRingPtr, ring->SqRingSize) != 0)
    {
        result = -1;
    }
    if (close(ring->Fd) != 0)
    {
        result = -1;
    }

    free(ring);
    return result;
#else
    (void)ringHandle;
    return 0;
#endif
}
