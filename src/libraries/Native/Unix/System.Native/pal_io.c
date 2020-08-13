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
#include <syslog.h>
#include <termios.h>
#include <unistd.h>
#include <limits.h>
#if HAVE_CLONEFILE
#include <sys/attr.h>
#include <sys/clonefile.h>
#endif
#if HAVE_SENDFILE_4
#include <sys/sendfile.h>
#endif
#if HAVE_INOTIFY
#include <sys/inotify.h>
#endif

#ifdef _AIX
#include <alloca.h>
// Somehow, AIX mangles the definition for this behind a C++ def
// Redeclare it here
extern int     getpeereid(int, uid_t *__restrict__, gid_t *__restrict__);
// This function declaration is hidden behind `_XOPEN_SOURCE=700`, but we need
// `_ALL_SOURCE` to build the runtime, and that resets that definition to 600.
// Instead of trying to wrangle ifdefs in system headers with more definitions,
// just declare it here.
extern ssize_t  getline(char **, size_t *, FILE *);
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
    int error = readdir_r(dir, entry, &result);

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
    return opendir(path);
}

int32_t SystemNative_CloseDir(DIR* dir)
{
    return closedir(dir);
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
    int32_t result;
    while ((result = fsync(ToFileDescriptor(fd))) < 0 && errno == EINTR);
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

char* SystemNative_GetLine(FILE* stream)
{
    assert(stream != NULL);

    char* lineptr = NULL;
    size_t n = 0;
    ssize_t length = getline(&lineptr, &n, stream);

    return length >= 0 ? lineptr : NULL;
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

int32_t SystemNative_CopyFile(intptr_t sourceFd, const char* srcPath, const char* destPath, int32_t overwrite)
{
    int inFd = ToFileDescriptor(sourceFd);
    int outFd;
    int ret;
    int tmpErrno;
    int openFlags;
    struct stat_ sourceStat;

    while ((ret = fstat_(inFd, &sourceStat)) < 0 && errno == EINTR);
    if (ret != 0)
    {
        return -1;
    }

    struct stat_ destStat;
    while ((ret = stat_(destPath, &destStat)) < 0 && errno == EINTR);
    if (ret == 0)
    {
        if (!overwrite)
        {
            errno = EEXIST;
            return -1;
        }

        if (sourceStat.st_dev == destStat.st_dev && sourceStat.st_ino == destStat.st_ino)
        {
            // Attempt to copy file over itself. Fail with the same error code as
            // open would.
            errno = EBUSY;
            return -1;
        }

#if HAVE_CLONEFILE
        // For clonefile we need to unlink the destination file first but we need to
        // check permission first to ensure we don't try to unlink read-only file.
        if (access(destPath, W_OK) != 0)
        {
            return -1;
        }

        ret = unlink(destPath);
        if (ret != 0)
        {
            return ret;
        }
#endif
    }

#if HAVE_CLONEFILE
    while ((ret = clonefile(srcPath, destPath, 0)) < 0 && errno == EINTR);
    // EEXIST can happen due to race condition between the stat/unlink above
    // and the clonefile here. The file could be (re-)created from another
    // thread or process before we have a chance to call clonefile. Handle
    // it by falling back to the slow path.
    if (ret == 0 || (errno != ENOTSUP && errno != EXDEV && errno != EEXIST))
    {
        return ret;
    }
#else
    // Unused variable
    (void)srcPath;
#endif

    openFlags = O_WRONLY | O_TRUNC | O_CREAT | (overwrite ? 0 : O_EXCL);
#if HAVE_O_CLOEXEC
    openFlags |= O_CLOEXEC;
#endif
    while ((outFd = open(destPath, openFlags, sourceStat.st_mode & (S_IRWXU | S_IRWXG | S_IRWXO))) < 0 && errno == EINTR);
    if (outFd < 0)
    {
        return -1;
    }
#if !HAVE_O_CLOEXEC
    fcntl(outFd, F_SETFD, FD_CLOEXEC);
#endif

    // Get the stats on the source file.
    bool copied = false;

    // If sendfile is available (Linux), try to use it, as the whole copy
    // can be performed in the kernel, without lots of unnecessary copying.
#if HAVE_SENDFILE_4

    // On 32-bit, if you use 64-bit offsets, the last argument of `sendfile' will be a
    // `size_t' a 32-bit integer while the `st_size' field of the stat structure will be off64_t.
    // So `size' will have to be `uint64_t'. In all other cases, it will be `size_t'.
    uint64_t size = (uint64_t)sourceStat.st_size;

    // Note that per man page for large files, you have to iterate until the
    // whole file is copied (Linux has a limit of 0x7ffff000 bytes copied).
    while (size > 0)
    {
        ssize_t sent = sendfile(outFd, inFd, NULL, (size >= SSIZE_MAX ? SSIZE_MAX : (size_t)size));
        if (sent < 0)
        {
            if (errno != EINVAL && errno != ENOSYS)
            {
                tmpErrno = errno;
                close(outFd);
                errno = tmpErrno;
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
    // sendfile couldn't be used; fall back to a manual copy below. This could happen
    // if we're on an old kernel, for example, where sendfile could only be used
    // with sockets and not regular files.
#endif // HAVE_SENDFILE_4

    // Manually read all data from the source and write it to the destination.
    if (!copied && CopyFile_ReadWrite(inFd, outFd) != 0)
    {
        tmpErrno = errno;
        close(outFd);
        errno = tmpErrno;
        return -1;
    }

    // Now that the data from the file has been copied, copy over metadata
    // from the source file.  First copy the file times.
    // If futimes nor futimes are available on this platform, file times will
    // not be copied over.
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

    tmpErrno = errno;
    close(outFd);
    errno = tmpErrno;
    return 0;
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

int32_t SystemNative_LockFileRegion(intptr_t fd, int64_t offset, int64_t length, int16_t lockType)
{
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

    lockArgs.l_type = lockType;
    lockArgs.l_whence = SEEK_SET;
    lockArgs.l_start = (off_t)offset;
    lockArgs.l_len = (off_t)length;

    int32_t ret;
    while ((ret = fcntl (ToFileDescriptor(fd), F_SETLK, &lockArgs)) < 0 && errno == EINTR);
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
