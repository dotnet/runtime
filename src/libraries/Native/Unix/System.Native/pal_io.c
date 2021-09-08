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
#include <errno.h>
#include <fcntl.h>
#include <fnmatch.h>
#include <stdio.h>
#include <stdlib.h>
#include <sys/mman.h>
#include <sys/stat.h>
#include <sys/time.h>
#include <sys/types.h>
#include <sys/file.h>
#include <sys/ioctl.h>
#include <sys/socket.h>
#include <sys/uio.h>
#include <syslog.h>
#include <termios.h>
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
#elif !HAVE_NON_LEGACY_STATFS // SunOS
#include <sys/types.h>
#include <sys/statvfs.h>
#include <sys/vfs.h>
#endif

#ifdef _AIX
#include <alloca.h>
// Somehow, AIX mangles the definition for this behind a C++ def
// Redeclare it here
extern int     getpeereid(int, uid_t *__restrict__, gid_t *__restrict__);
#elif defined(TARGET_SUNOS)
#ifndef _KERNEL
#define _KERNEL
#define UNDEF_KERNEL
#endif
#include <sys/procfs.h>
#ifdef UNDEF_KERNEL
#undef _KERNEL
#endif
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
c_static_assert(PAL_S_IFMT == S_IFMT);
c_static_assert(PAL_S_IFIFO == S_IFIFO);
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
#if defined(DT_UNKNOWN) || defined(TARGET_WASM)
c_static_assert((int)PAL_DT_UNKNOWN == (int)DT_UNKNOWN);
c_static_assert((int)PAL_DT_FIFO == (int)DT_FIFO);
c_static_assert((int)PAL_DT_CHR == (int)DT_CHR);
c_static_assert((int)PAL_DT_DIR == (int)DT_DIR);
c_static_assert((int)PAL_DT_BLK == (int)DT_BLK);
c_static_assert((int)PAL_DT_REG == (int)DT_REG);
c_static_assert((int)PAL_DT_LNK == (int)DT_LNK);
c_static_assert((int)PAL_DT_SOCK == (int)DT_SOCK);
c_static_assert((int)PAL_DT_WHT == (int)DT_WHT);
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
c_static_assert(PAL_IN_ISDIR == IN_ISDIR);
#endif // HAVE_INOTIFY

static void ConvertFileStatus(const struct stat_* src, FileStatus* dst)
{
    dst->Dev = (int64_t)src->st_dev;
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

#if defined(HAVE_STAT_FLAGS) && defined(UF_HIDDEN)
    dst->UserFlags = ((src->st_flags & UF_HIDDEN) == UF_HIDDEN) ? PAL_UF_HIDDEN : 0;
#else
    dst->UserFlags = 0;
#endif
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

    if (flags & ~(PAL_O_ACCESS_MODE_MASK | PAL_O_CLOEXEC | PAL_O_CREAT | PAL_O_EXCL | PAL_O_TRUNC | PAL_O_SYNC))
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
    return close(ToFileDescriptor(fd));
}

intptr_t SystemNative_Dup(intptr_t oldfd)
{
    int result;
#if HAVE_F_DUPFD_CLOEXEC
    while ((result = fcntl(ToFileDescriptor(oldfd), F_DUPFD_CLOEXEC, 0)) < 0 && errno == EINTR);
#else
    while ((result = fcntl(ToFileDescriptor(oldfd), F_DUPFD, 0)) < 0 && errno == EINTR);
    // do CLOEXEC here too
    fcntl(result, F_SETFD, FD_CLOEXEC);
#endif
    return result;
}

int32_t SystemNative_Unlink(const char* path)
{
    int32_t result;
    while ((result = unlink(path)) < 0 && errno == EINTR);
    return result;
}

intptr_t SystemNative_ShmOpen(const char* name, int32_t flags, int32_t mode)
{
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

#if HAVE_READDIR_R
// struct dirent typically contains 64-bit numbers (e.g. d_ino), so we align it at 8-byte.
static const size_t dirent_alignment = 8;
#endif

int32_t SystemNative_GetReadDirRBufferSize(void)
{
#if HAVE_READDIR_R
    // dirent should be under 2k in size
    assert(sizeof(struct dirent) < 2048);
    // add some extra space so we can align the buffer to dirent.
    return sizeof(struct dirent) + dirent_alignment - 1;
#else
    return 0;
#endif
}

// To reduce the number of string copies, the caller of this function is responsible to ensure the memory
// referenced by outputEntry remains valid until it is read.
// If the platform supports readdir_r, the caller provides a buffer into which the data is read.
// If the platform uses readdir, the caller must ensure no calls are made to readdir/closedir since those will invalidate
// the current dirent. We assume the platform supports concurrent readdir calls to different DIRs.
int32_t SystemNative_ReadDirR(DIR* dir, uint8_t* buffer, int32_t bufferSize, DirectoryEntry* outputEntry)
{
    assert(dir != NULL);
    assert(outputEntry != NULL);

#if HAVE_READDIR_R
    assert(buffer != NULL);

    // align to dirent
    struct dirent* entry = (struct dirent*)((size_t)(buffer + dirent_alignment - 1) & ~(dirent_alignment - 1));

    // check there is dirent size available at entry
    if ((buffer + bufferSize) < ((uint8_t*)entry + sizeof(struct dirent)))
    {
        assert(false && "Buffer size too small; use GetReadDirRBufferSize to get required buffer size");
        return ERANGE;
    }

    struct dirent* result = NULL;
#ifdef _AIX
    // AIX returns 0 on success, but bizarrely, it returns 9 for both error and
    // end-of-directory. result is NULL for both cases. The API returns the
    // same thing for EOD/error, so disambiguation between the two is nearly
    // impossible without clobbering errno for yourself and seeing if the API
    // changed it. See:
    // https://www.ibm.com/support/knowledgecenter/ssw_aix_71/com.ibm.aix.basetrf2/readdir_r.htm

    errno = 0; // create a success condition for the API to clobber
    int error = readdir_r(dir, entry, &result);

    if (error == 9)
    {
        memset(outputEntry, 0, sizeof(*outputEntry)); // managed out param must be initialized
        return errno == 0 ? -1 : errno;
    }
#else
    int error;

    // EINTR isn't documented, happens in practice on macOS.
    while ((error = readdir_r(dir, entry, &result)) && errno == EINTR);

    // positive error number returned -> failure
    if (error != 0)
    {
        assert(error > 0);
        memset(outputEntry, 0, sizeof(*outputEntry)); // managed out param must be initialized
        return error;
    }

    // 0 returned with null result -> end-of-stream
    if (result == NULL)
    {
        memset(outputEntry, 0, sizeof(*outputEntry)); // managed out param must be initialized
        return -1;         // shim convention for end-of-stream
    }
#endif

    // 0 returned with non-null result (guaranteed to be set to entry arg) -> success
    assert(result == entry);
#else
    (void)buffer;     // unused
    (void)bufferSize; // unused
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
#endif
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

int32_t SystemNative_Pipe(int32_t pipeFds[2], int32_t flags)
{
    switch (flags)
    {
        case 0:
            break;
        case PAL_O_CLOEXEC:
#if HAVE_O_CLOEXEC
            flags = O_CLOEXEC;
#endif
            break;
        default:
            assert_msg(false, "Unknown pipe flag", (int)flags);
            errno = EINVAL;
            return -1;
    }

    int32_t result;
#if HAVE_PIPE2
    // If pipe2 is available, use it.  This will handle O_CLOEXEC if it was set.
    while ((result = pipe2(pipeFds, flags)) < 0 && errno == EINTR);
#else
    // Otherwise, use pipe.
    while ((result = pipe(pipeFds)) < 0 && errno == EINTR);

    // Then, if O_CLOEXEC was specified, use fcntl to configure the file descriptors appropriately.
#if HAVE_O_CLOEXEC
    if ((flags & O_CLOEXEC) != 0 && result == 0)
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
#endif
    return result;
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
    int32_t result;
    while ((result = chmod(path, (mode_t)mode)) < 0 && errno == EINTR);
    return result;
}

int32_t SystemNative_FChMod(intptr_t fd, int32_t mode)
{
    int32_t result;
    while ((result = fchmod(ToFileDescriptor(fd), (mode_t)mode)) < 0 && errno == EINTR);
    return result;
}

int32_t SystemNative_FSync(intptr_t fd)
{
    int fileDescriptor = ToFileDescriptor(fd);

    int32_t result;
    while ((result =
#if defined(TARGET_OSX) && HAVE_F_FULLFSYNC
    fcntl(fileDescriptor, F_FULLFSYNC)
#else
    fsync(fileDescriptor)
#endif
    < 0) && errno == EINTR);
    return result;
}

int32_t SystemNative_FLock(intptr_t fd, int32_t operation)
{
    int32_t result;
    while ((result = flock(ToFileDescriptor(fd), operation)) < 0 && errno == EINTR);
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
#else
            lseek(
#endif
                 ToFileDescriptor(fd),
                 (off_t)offset,
                 whence)) < 0 && errno == EINTR);
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
#ifdef MADV_DONTFORK
            return madvise(address, (size_t)length, MADV_DONTFORK);
#else
            (void)address, (void)length, (void)advice;
            errno = ENOTSUP;
            return -1;
#endif
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

    return msync(address, (size_t)length, flags);
}

int64_t SystemNative_SysConf(int32_t name)
{
    switch (name)
    {
        case PAL_SC_CLK_TCK:
            return sysconf(_SC_CLK_TCK);
        case PAL_SC_PAGESIZE:
            return sysconf(_SC_PAGESIZE);
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

int32_t SystemNative_PosixFAllocate(intptr_t fd, int64_t offset, int64_t length)
{
    assert_msg(offset == 0, "Invalid offset value", (int)offset);

    int fileDescriptor = ToFileDescriptor(fd);
    int32_t result;
#if HAVE_POSIX_FALLOCATE64 // 64-bit Linux
    while ((result = posix_fallocate64(fileDescriptor, (off64_t)offset, (off64_t)length)) == EINTR);
#elif HAVE_POSIX_FALLOCATE // 32-bit Linux
    while ((result = posix_fallocate(fileDescriptor, (off_t)offset, (off_t)length)) == EINTR);
#elif defined(F_PREALLOCATE) // macOS
    fstore_t fstore;
    fstore.fst_flags = F_ALLOCATECONTIG; // ensure contiguous space
    fstore.fst_posmode = F_PEOFPOSMODE;  // allocate from the physical end of file, as offset MUST NOT be 0 for F_VOLPOSMODE
    fstore.fst_offset = (off_t)offset;
    fstore.fst_length = (off_t)length;
    fstore.fst_bytesalloc = 0; // output size, can be > length

    while ((result = fcntl(fileDescriptor, F_PREALLOCATE, &fstore)) == -1 && errno == EINTR);

    if (result == -1)
    {
        // we have failed to allocate contiguous space, let's try non-contiguous
        fstore.fst_flags = F_ALLOCATEALL; // all or nothing
        while ((result = fcntl(fileDescriptor, F_PREALLOCATE, &fstore)) == -1 && errno == EINTR);
    }
#elif defined(F_ALLOCSP) || defined(F_ALLOCSP64) // FreeBSD
    #if HAVE_FLOCK64
    struct flock64 lockArgs;
    int command = F_ALLOCSP64;
    #else
    struct flock lockArgs;
    int command = F_ALLOCSP;
    #endif

    lockArgs.l_whence = SEEK_SET;
    lockArgs.l_start = (off_t)offset;
    lockArgs.l_len = (off_t)length;

    while ((result = fcntl(fileDescriptor, command, &lockArgs)) == -1 && errno == EINTR);
#endif

#if defined(F_PREALLOCATE) || defined(F_ALLOCSP) || defined(F_ALLOCSP64)
    // most of the Unixes implement posix_fallocate which does NOT set the last error
    // fctnl does, but to mimic the posix_fallocate behaviour we just return error
    if (result == -1)
    {
        result = errno;
    }
    else
    {
        // align the behaviour with what posix_fallocate does (change reported file size)
        ftruncate(fileDescriptor, length);
    }
#endif

    // error codes can be OS-specific, so this is why this handling is done here rather than in the managed layer
    switch (result)
    {
        case ENOSPC: // there was not enough space
            return -1;
        case EFBIG: // the file was too large
            return -2;
        case ENODEV: // not a regular file
        case ESPIPE: // a pipe
            // We ignore it, as FileStream contract makes it clear that allocationSize is ignored for non-regular files.
            return 0;
        case EINVAL:
            // We control the offset and length so they are correct.
            assert_msg(length >= 0, "Invalid length value", (int)length);
            // But if the underlying filesystem does not support the operation, we just ignore it and treat as a hint.
            return 0;
        default:
            assert(result != EINTR); // it can't happen here as we retry the call on EINTR
            assert(result != EBADF); // it can't happen here as this method is being called after a succesfull call to open (with write permissions) before returning the SafeFileHandle to the user
            return 0;
    }
}

int32_t SystemNative_Read(intptr_t fd, void* buffer, int32_t bufferSize)
{
    return Common_Read(fd, buffer, bufferSize);
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
    sync();
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

int32_t SystemNative_CopyFile(intptr_t sourceFd, intptr_t destinationFd)
{
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
    struct stat_ sourceStat;
    bool copied = false;
#if HAVE_SENDFILE_4
    // If sendfile is available (Linux), try to use it, as the whole copy
    // can be performed in the kernel, without lots of unnecessary copying.
    while ((ret = fstat_(inFd, &sourceStat)) < 0 && errno == EINTR);
    if (ret != 0)
    {
        return -1;
    }

    // On 32-bit, if you use 64-bit offsets, the last argument of `sendfile' will be a
    // `size_t' a 32-bit integer while the `st_size' field of the stat structure will be off64_t.
    // So `size' will have to be `uint64_t'. In all other cases, it will be `size_t'.
    uint64_t size = (uint64_t)sourceStat.st_size;
    if (size != 0)
    {
        // Note that per man page for large files, you have to iterate until the
        // whole file is copied (Linux has a limit of 0x7ffff000 bytes copied).
        while (size > 0)
        {
            ssize_t sent = sendfile(outFd, inFd, NULL, (size >= SSIZE_MAX ? SSIZE_MAX : (size_t)size));
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
                assert((size_t)sent <= size);
                size -= (size_t)sent;
            }
        }

        if (size == 0)
        {
            copied = true;
        }
    }

    // sendfile couldn't be used; fall back to a manual copy below. This could happen
    // if we're on an old kernel, for example, where sendfile could only be used
    // with sockets and not regular files.  Additionally, certain files (e.g. procfs)
    // may return a size of 0 even though reading from then will produce data.  As such,
    // we avoid using sendfile with the queried size if the size is reported as 0.
#endif // HAVE_SENDFILE_4

    // Manually read all data from the source and write it to the destination.
    if (!copied && CopyFile_ReadWrite(inFd, outFd) != 0)
    {
        return -1;
    }

    // Now that the data from the file has been copied, copy over metadata
    // from the source file.  First copy the file times.
    // If futimes nor futimes are available on this platform, file times will
    // not be copied over.
    while ((ret = fstat_(inFd, &sourceStat)) < 0 && errno == EINTR);
    if (ret == 0)
    {
#if HAVE_FUTIMENS
        // futimens is prefered because it has a higher resolution.
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
    // Then copy permissions.
    while ((ret = fchmod(outFd, sourceStat.st_mode & (S_IRWXU | S_IRWXG | S_IRWXO))) < 0 && errno == EINTR);
    if (ret != 0 && errno != EPERM) // See EPERM comment above
    {
        return -1;
    }

    return 0;
#endif // HAVE_FCOPYFILE
}

intptr_t SystemNative_INotifyInit(void)
{
#if HAVE_INOTIFY
    return inotify_init();
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
#if defined(SO_PEERCRED) && !defined(TARGET_WASM)
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
    return realpath(path, NULL);
}

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

int64_t SystemNative_GetFileSystemType(intptr_t fd)
{
#if HAVE_STATFS_VFS || HAVE_STATFS_MOUNT
    int statfsRes;
    struct statfs statfsArgs;
    // for our needs (get file system type) statfs is always enough and there is no need to use statfs64
    // which got deprecated in macOS 10.6, in favor of statfs
    while ((statfsRes = fstatfs(ToFileDescriptor(fd), &statfsArgs)) == -1 && errno == EINTR) ;
    return statfsRes == -1 ? (int64_t)-1 : (int64_t)statfsArgs.f_type;
#elif !HAVE_NON_LEGACY_STATFS
    int statfsRes;
    struct statvfs statfsArgs;
    while ((statfsRes = fstatvfs(ToFileDescriptor(fd), &statfsArgs)) == -1 && errno == EINTR) ;
    if (statfsRes == -1) return (int64_t)-1;

    int64_t result = -1;

    if (strcmp(statfsArgs.f_basetype, "adfs") == 0) result = 0xADF5;
    else if (strcmp(statfsArgs.f_basetype, "affs") == 0) result = 0xADFF;
    else if (strcmp(statfsArgs.f_basetype, "afs") == 0) result = 0x5346414F;
    else if (strcmp(statfsArgs.f_basetype, "anoninode") == 0) result = 0x09041934;
    else if (strcmp(statfsArgs.f_basetype, "aufs") == 0) result = 0x61756673;
    else if (strcmp(statfsArgs.f_basetype, "autofs") == 0) result = 0x0187;
    else if (strcmp(statfsArgs.f_basetype, "autofs4") == 0) result = 0x6D4A556D;
    else if (strcmp(statfsArgs.f_basetype, "befs") == 0) result = 0x42465331;
    else if (strcmp(statfsArgs.f_basetype, "bdevfs") == 0) result = 0x62646576;
    else if (strcmp(statfsArgs.f_basetype, "bfs") == 0) result = 0x1BADFACE;
    else if (strcmp(statfsArgs.f_basetype, "bpf_fs") == 0) result = 0xCAFE4A11;
    else if (strcmp(statfsArgs.f_basetype, "binfmt_misc") == 0) result = 0x42494E4D;
    else if (strcmp(statfsArgs.f_basetype, "bootfs") == 0) result = 0xA56D3FF9;
    else if (strcmp(statfsArgs.f_basetype, "btrfs") == 0) result = 0x9123683E;
    else if (strcmp(statfsArgs.f_basetype, "ceph") == 0) result = 0x00C36400;
    else if (strcmp(statfsArgs.f_basetype, "cgroupfs") == 0) result = 0x0027E0EB;
    else if (strcmp(statfsArgs.f_basetype, "cgroup2fs") == 0) result = 0x63677270;
    else if (strcmp(statfsArgs.f_basetype, "cifs") == 0) result = 0xFF534D42;
    else if (strcmp(statfsArgs.f_basetype, "coda") == 0) result = 0x73757245;
    else if (strcmp(statfsArgs.f_basetype, "coherent") == 0) result = 0x012FF7B7;
    else if (strcmp(statfsArgs.f_basetype, "configfs") == 0) result = 0x62656570;
    else if (strcmp(statfsArgs.f_basetype, "cpuset") == 0) result = 0x01021994;
    else if (strcmp(statfsArgs.f_basetype, "cramfs") == 0) result = 0x28CD3D45;
    else if (strcmp(statfsArgs.f_basetype, "ctfs") == 0) result = 0x01021994;
    else if (strcmp(statfsArgs.f_basetype, "debugfs") == 0) result = 0x64626720;
    else if (strcmp(statfsArgs.f_basetype, "dev") == 0) result = 0x1373;
    else if (strcmp(statfsArgs.f_basetype, "devfs") == 0) result = 0x1373;
    else if (strcmp(statfsArgs.f_basetype, "devpts") == 0) result = 0x1CD1;
    else if (strcmp(statfsArgs.f_basetype, "ecryptfs") == 0) result = 0xF15F;
    else if (strcmp(statfsArgs.f_basetype, "efs") == 0) result = 0x00414A53;
    else if (strcmp(statfsArgs.f_basetype, "exofs") == 0) result = 0x5DF5;
    else if (strcmp(statfsArgs.f_basetype, "ext") == 0) result = 0x137D;
    else if (strcmp(statfsArgs.f_basetype, "ext2_old") == 0) result = 0xEF51;
    else if (strcmp(statfsArgs.f_basetype, "ext2") == 0) result = 0xEF53;
    else if (strcmp(statfsArgs.f_basetype, "ext3") == 0) result = 0xEF53;
    else if (strcmp(statfsArgs.f_basetype, "ext4") == 0) result = 0xEF53;
    else if (strcmp(statfsArgs.f_basetype, "f2fs") == 0) result = 0xF2F52010;
    else if (strcmp(statfsArgs.f_basetype, "fat") == 0) result = 0x4006;
    else if (strcmp(statfsArgs.f_basetype, "fd") == 0) result = 0xF00D1E;
    else if (strcmp(statfsArgs.f_basetype, "fhgfs") == 0) result = 0x19830326;
    else if (strcmp(statfsArgs.f_basetype, "fuse") == 0) result = 0x65735546;
    else if (strcmp(statfsArgs.f_basetype, "fuseblk") == 0) result = 0x65735546;
    else if (strcmp(statfsArgs.f_basetype, "fusectl") == 0) result = 0x65735543;
    else if (strcmp(statfsArgs.f_basetype, "futexfs") == 0) result = 0x0BAD1DEA;
    else if (strcmp(statfsArgs.f_basetype, "gfsgfs2") == 0) result = 0x1161970;
    else if (strcmp(statfsArgs.f_basetype, "gfs2") == 0) result = 0x01161970;
    else if (strcmp(statfsArgs.f_basetype, "gpfs") == 0) result = 0x47504653;
    else if (strcmp(statfsArgs.f_basetype, "hfs") == 0) result = 0x4244;
    else if (strcmp(statfsArgs.f_basetype, "hfsplus") == 0) result = 0x482B;
    else if (strcmp(statfsArgs.f_basetype, "hpfs") == 0) result = 0xF995E849;
    else if (strcmp(statfsArgs.f_basetype, "hugetlbfs") == 0) result = 0x958458F6;
    else if (strcmp(statfsArgs.f_basetype, "inodefs") == 0) result = 0x11307854;
    else if (strcmp(statfsArgs.f_basetype, "inotifyfs") == 0) result = 0x2BAD1DEA;
    else if (strcmp(statfsArgs.f_basetype, "isofs") == 0) result = 0x9660;
    else if (strcmp(statfsArgs.f_basetype, "jffs") == 0) result = 0x07C0;
    else if (strcmp(statfsArgs.f_basetype, "jffs2") == 0) result = 0x72B6;
    else if (strcmp(statfsArgs.f_basetype, "jfs") == 0) result = 0x3153464A;
    else if (strcmp(statfsArgs.f_basetype, "kafs") == 0) result = 0x6B414653;
    else if (strcmp(statfsArgs.f_basetype, "lofs") == 0) result = 0xEF53;
    else if (strcmp(statfsArgs.f_basetype, "logfs") == 0) result = 0xC97E8168;
    else if (strcmp(statfsArgs.f_basetype, "lustre") == 0) result = 0x0BD00BD0;
    else if (strcmp(statfsArgs.f_basetype, "minix_old") == 0) result = 0x137F;
    else if (strcmp(statfsArgs.f_basetype, "minix") == 0) result = 0x138F;
    else if (strcmp(statfsArgs.f_basetype, "minix2") == 0) result = 0x2468;
    else if (strcmp(statfsArgs.f_basetype, "minix2v2") == 0) result = 0x2478;
    else if (strcmp(statfsArgs.f_basetype, "minix3") == 0) result = 0x4D5A;
    else if (strcmp(statfsArgs.f_basetype, "mntfs") == 0) result = 0x01021994;
    else if (strcmp(statfsArgs.f_basetype, "mqueue") == 0) result = 0x19800202;
    else if (strcmp(statfsArgs.f_basetype, "msdos") == 0) result = 0x4D44;
    else if (strcmp(statfsArgs.f_basetype, "nfs") == 0) result = 0x6969;
    else if (strcmp(statfsArgs.f_basetype, "nfsd") == 0) result = 0x6E667364;
    else if (strcmp(statfsArgs.f_basetype, "nilfs") == 0) result = 0x3434;
    else if (strcmp(statfsArgs.f_basetype, "novell") == 0) result = 0x564C;
    else if (strcmp(statfsArgs.f_basetype, "ntfs") == 0) result = 0x5346544E;
    else if (strcmp(statfsArgs.f_basetype, "objfs") == 0) result = 0x01021994;
    else if (strcmp(statfsArgs.f_basetype, "ocfs2") == 0) result = 0x7461636F;
    else if (strcmp(statfsArgs.f_basetype, "openprom") == 0) result = 0x9FA1;
    else if (strcmp(statfsArgs.f_basetype, "omfs") == 0) result = 0xC2993D87;
    else if (strcmp(statfsArgs.f_basetype, "overlay") == 0) result = 0x794C7630;
    else if (strcmp(statfsArgs.f_basetype, "overlayfs") == 0) result = 0x794C764F;
    else if (strcmp(statfsArgs.f_basetype, "panfs") == 0) result = 0xAAD7AAEA;
    else if (strcmp(statfsArgs.f_basetype, "pipefs") == 0) result = 0x50495045;
    else if (strcmp(statfsArgs.f_basetype, "proc") == 0) result = 0x9FA0;
    else if (strcmp(statfsArgs.f_basetype, "pstorefs") == 0) result = 0x6165676C;
    else if (strcmp(statfsArgs.f_basetype, "qnx4") == 0) result = 0x002F;
    else if (strcmp(statfsArgs.f_basetype, "qnx6") == 0) result = 0x68191122;
    else if (strcmp(statfsArgs.f_basetype, "ramfs") == 0) result = 0x858458F6;
    else if (strcmp(statfsArgs.f_basetype, "reiserfs") == 0) result = 0x52654973;
    else if (strcmp(statfsArgs.f_basetype, "romfs") == 0) result = 0x7275;
    else if (strcmp(statfsArgs.f_basetype, "rootfs") == 0) result = 0x53464846;
    else if (strcmp(statfsArgs.f_basetype, "rpc_pipefs") == 0) result = 0x67596969;
    else if (strcmp(statfsArgs.f_basetype, "samba") == 0) result = 0x517B;
    else if (strcmp(statfsArgs.f_basetype, "securityfs") == 0) result = 0x73636673;
    else if (strcmp(statfsArgs.f_basetype, "selinux") == 0) result = 0xF97CFF8C;
    else if (strcmp(statfsArgs.f_basetype, "sffs") == 0) result = 0x786F4256;
    else if (strcmp(statfsArgs.f_basetype, "sharefs") == 0) result = 0x01021994;
    else if (strcmp(statfsArgs.f_basetype, "smb") == 0) result = 0x517B;
    else if (strcmp(statfsArgs.f_basetype, "smb2") == 0) result = 0xFE534D42;
    else if (strcmp(statfsArgs.f_basetype, "sockfs") == 0) result = 0x534F434B;
    else if (strcmp(statfsArgs.f_basetype, "squashfs") == 0) result = 0x73717368;
    else if (strcmp(statfsArgs.f_basetype, "sysfs") == 0) result = 0x62656572;
    else if (strcmp(statfsArgs.f_basetype, "sysv2") == 0) result = 0x012FF7B6;
    else if (strcmp(statfsArgs.f_basetype, "sysv4") == 0) result = 0x012FF7B5;
    else if (strcmp(statfsArgs.f_basetype, "tmpfs") == 0) result = 0x01021994;
    else if (strcmp(statfsArgs.f_basetype, "tracefs") == 0) result = 0x74726163;
    else if (strcmp(statfsArgs.f_basetype, "ubifs") == 0) result = 0x24051905;
    else if (strcmp(statfsArgs.f_basetype, "udf") == 0) result = 0x15013346;
    else if (strcmp(statfsArgs.f_basetype, "ufs") == 0) result = 0x00011954;
    else if (strcmp(statfsArgs.f_basetype, "ufscigam") == 0) result = 0x54190100;
    else if (strcmp(statfsArgs.f_basetype, "ufs2") == 0) result = 0x19540119;
    else if (strcmp(statfsArgs.f_basetype, "usbdevice") == 0) result = 0x9FA2;
    else if (strcmp(statfsArgs.f_basetype, "v9fs") == 0) result = 0x01021997;
    else if (strcmp(statfsArgs.f_basetype, "vagrant") == 0) result = 0x786F4256;
    else if (strcmp(statfsArgs.f_basetype, "vboxfs") == 0) result = 0x786F4256;
    else if (strcmp(statfsArgs.f_basetype, "vmhgfs") == 0) result = 0xBACBACBC;
    else if (strcmp(statfsArgs.f_basetype, "vxfs") == 0) result = 0xA501FCF5;
    else if (strcmp(statfsArgs.f_basetype, "vzfs") == 0) result = 0x565A4653;
    else if (strcmp(statfsArgs.f_basetype, "xenfs") == 0) result = 0xABBA1974;
    else if (strcmp(statfsArgs.f_basetype, "xenix") == 0) result = 0x012FF7B4;
    else if (strcmp(statfsArgs.f_basetype, "xfs") == 0) result = 0x58465342;
    else if (strcmp(statfsArgs.f_basetype, "xia") == 0) result = 0x012FD16D;
    else if (strcmp(statfsArgs.f_basetype, "udev") == 0) result = 0x01021994;
    else if (strcmp(statfsArgs.f_basetype, "zfs") == 0) result = 0x2FC12FC1;

    assert(result != -1);
    return result;
#else
    #error "Platform doesn't support fstatfs or fstatvfs"
#endif
}

int32_t SystemNative_LockFileRegion(intptr_t fd, int64_t offset, int64_t length, int16_t lockType)
{
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

#if defined(TARGET_ANDROID) && defined(HAVE_FLOCK64)
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

int32_t SystemNative_LChflagsCanSetHiddenFlag(void)
{
#if defined(UF_HIDDEN) && defined(HAVE_STAT_FLAGS) && defined(HAVE_LCHFLAGS)
    return true;
#else
    return false;
#endif
}

int32_t SystemNative_ReadProcessStatusInfo(pid_t pid, ProcessStatus* processStatus)
{
#ifdef __sun
    char statusFilename[64];
    snprintf(statusFilename, sizeof(statusFilename), "/proc/%d/psinfo", pid);

    intptr_t fd;
    while ((fd = open(statusFilename, O_RDONLY)) < 0 && errno == EINTR);
    if (fd < 0)
    {
        return 0;
    }

    psinfo_t status;
    int result = Common_Read(fd, &status, sizeof(psinfo_t));
    close(fd);
    if (result >= 0)
    {
        processStatus->ResidentSetSize = status.pr_rssize * 1024; // pr_rssize is in Kbytes
        return 1;
    }

    return 0;
#else
    (void)pid, (void)processStatus;
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

int64_t SystemNative_PReadV(intptr_t fd, IOVector* vectors, int32_t vectorCount, int64_t fileOffset)
{
    assert(vectors != NULL);
    assert(vectorCount >= 0);

    int64_t count = 0;
    int fileDescriptor = ToFileDescriptor(fd);
#if HAVE_PREADV && !defined(TARGET_WASM) // preadv is buggy on WASM
    while ((count = preadv(fileDescriptor, (struct iovec*)vectors, (int)vectorCount, (off_t)fileOffset)) < 0 && errno == EINTR);
#else
    int64_t current;
    for (int i = 0; i < vectorCount; i++)
    {
        IOVector vector = vectors[i];
        while ((current = pread(fileDescriptor, vector.Base, vector.Count, (off_t)(fileOffset + count))) < 0 && errno == EINTR);

        if (current < 0)
        {
            // if previous calls were succesfull, we return what we got so far
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

int64_t SystemNative_PWriteV(intptr_t fd, IOVector* vectors, int32_t vectorCount, int64_t fileOffset)
{
    assert(vectors != NULL);
    assert(vectorCount >= 0);

    int64_t count = 0;
    int fileDescriptor = ToFileDescriptor(fd);
#if HAVE_PWRITEV && !defined(TARGET_WASM) // pwritev is buggy on WASM
    while ((count = pwritev(fileDescriptor, (struct iovec*)vectors, (int)vectorCount, (off_t)fileOffset)) < 0 && errno == EINTR);
#else
    int64_t current;
    for (int i = 0; i < vectorCount; i++)
    {
        IOVector vector = vectors[i];
        while ((current = pwrite(fileDescriptor, vector.Base, vector.Count, (off_t)(fileOffset + count))) < 0 && errno == EINTR);

        if (current < 0)
        {
            // if previous calls were succesfull, we return what we got so far
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
