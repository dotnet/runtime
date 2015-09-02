// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "../config.h"
#include "pal_io.h"

#include <assert.h>
#include <errno.h>
#include <fcntl.h>
#include <sys/mman.h>
#include <sys/stat.h>
#include <sys/types.h>
#include <sys/file.h>
#include <unistd.h>
#include <fnmatch.h>
#include <stdlib.h>

#if HAVE_STAT64
#    define stat_ stat64
#    define fstat_ fstat64
#    define lstat_ lstat64
#else
#   define stat_ stat
#   define fstat_ fstat
#   define lstat_ lstat
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
static_assert(PAL_S_IFMT  == S_IFMT,  "");
static_assert(PAL_S_IFIFO == S_IFIFO, "");
static_assert(PAL_S_IFCHR == S_IFCHR, "");
static_assert(PAL_S_IFDIR == S_IFDIR, "");
static_assert(PAL_S_IFREG == S_IFREG, "");
static_assert(PAL_S_IFLNK == S_IFLNK, "");

// Validate that our enum for inode types is the same as what is 
// declared by the dirent.h header on the local system.
static_assert((int)NodeType::PAL_DT_UNKNOWN == DT_UNKNOWN, "");
static_assert((int)NodeType::PAL_DT_FIFO == DT_FIFO, "");
static_assert((int)NodeType::PAL_DT_CHR == DT_CHR, "");
static_assert((int)NodeType::PAL_DT_DIR == DT_DIR, "");
static_assert((int)NodeType::PAL_DT_BLK == DT_BLK, "");
static_assert((int)NodeType::PAL_DT_REG == DT_REG, "");
static_assert((int)NodeType::PAL_DT_LNK == DT_LNK, "");
static_assert((int)NodeType::PAL_DT_SOCK == DT_SOCK, "");
static_assert((int)NodeType::PAL_DT_WHT == DT_WHT, "");

// Validate that our Lock enum value are correct for the platform
static_assert((int)LockOperations::PAL_LOCK_SH == LOCK_SH, "");
static_assert((int)LockOperations::PAL_LOCK_EX == LOCK_EX, "");
static_assert((int)LockOperations::PAL_LOCK_NB == LOCK_NB, "");
static_assert((int)LockOperations::PAL_LOCK_UN == LOCK_UN, "");

// Validate our AccessMode enum values are correct for the platform
static_assert((int)AccessMode::PAL_F_OK == F_OK, "");
static_assert((int)AccessMode::PAL_X_OK == X_OK, "");
static_assert((int)AccessMode::PAL_W_OK == W_OK, "");
static_assert((int)AccessMode::PAL_R_OK == R_OK, "");

// Validate our SeekWhence enum values are correct for the platform
static_assert((int)SeekWhence::PAL_SEEK_SET == SEEK_SET, "");
static_assert((int)SeekWhence::PAL_SEEK_CUR == SEEK_CUR, "");
static_assert((int)SeekWhence::PAL_SEEK_END == SEEK_END, "");

static
void ConvertFileStatus(const struct stat_& src, FileStatus* dst)
{
    dst->Flags  = FILESTATUS_FLAGS_NONE;
    dst->Mode   = src.st_mode;
    dst->Uid    = src.st_uid;
    dst->Gid    = src.st_gid;
    dst->Size   = src.st_size;
    dst->ATime  = src.st_atime;
    dst->MTime  = src.st_mtime;
    dst->CTime  = src.st_ctime;

#if HAVE_STAT_BIRTHTIME
    dst->BirthTime = src.st_birthtime;
    dst->Flags |= FILESTATUS_FLAGS_HAS_BIRTHTIME;
#else
    dst->BirthTime = 0;
#endif
}

extern "C"
int32_t Stat(const char* path, FileStatus* output)
{
    struct stat_ result;
    int ret = stat_(path, &result);

     if (ret == 0)
     {
        ConvertFileStatus(result, output);
     }

    return ret;
}

extern "C"
int32_t FStat(int32_t fd, FileStatus* output)
{
    struct stat_ result;
    int ret = fstat_(fd, &result);

    if (ret == 0)
    {
        ConvertFileStatus(result, output);
    }

    return ret;
}

extern "C"
int32_t LStat(const char* path, FileStatus* output)
{
    struct stat_ result;
    int ret = lstat_(path, &result);

    if (ret == 0)
    {
        ConvertFileStatus(result, output);
    }

    return ret;
}

static 
int32_t ConvertOpenFlags(int32_t flags)
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
            assert(!"Unknown access mode.");
            return -1;
    }
    
    if (flags 
        & ~(PAL_O_ACCESS_MODE_MASK
          | PAL_O_CLOEXEC 
          | PAL_O_CREAT 
          | PAL_O_EXCL 
          | PAL_O_TRUNC 
          | PAL_O_SYNC
          ))
    {
        assert(!"Unknown flag.");
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
 
    return ret;
}

extern "C"
int32_t Open(const char* path, int32_t flags, int32_t mode)
{
    flags = ConvertOpenFlags(flags);
    if (flags < 0)
    {
        errno = EINVAL;
        return -1;
    }
    
    return open(path, flags, mode);
}

extern "C"
int32_t Close(int32_t fd)
{
    return close(fd);
}

extern "C"
int32_t Unlink(const char* path)
{
    return unlink(path);
}

extern "C"
int32_t ShmOpen(const char* name, int32_t flags, int32_t mode)
{
    flags = ConvertOpenFlags(flags);
    if (flags < 0)
    {
        errno = EINVAL;
        return -1;
    }
    
    return shm_open(name, flags, mode);
}

extern "C"
int32_t ShmUnlink(const char* name)
{
    return shm_unlink(name);
}

static
void ConvertDirent(const dirent& entry, DirectoryEntry* outputEntry)
{
    // We use Marshal.PtrToStringAnsi on the managed side, which takes a pointer to
    // the start of the unmanaged string. Give the caller back a pointer to the 
    // location of the start of the string that exists in their own byte buffer.
    outputEntry->Name = entry.d_name;
    outputEntry->InodeType = (NodeType)entry.d_type;

#if HAVE_DIRENT_NAME_LEN
    outputEntry->NameLength = entry.d_namlen;
#else
    outputEntry->NameLength = -1; // sentinel value to mean we have to walk to find the first \0
#endif
}

extern "C" 
int32_t GetDirentSize()
{
    // dirent should be under 2k in size
    static_assert(sizeof(dirent) < 2048, "");
    return static_cast<int32_t>(sizeof(dirent));
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
extern "C" 
int32_t ReadDirR(DIR* dir, void* buffer, int32_t bufferSize, DirectoryEntry* outputEntry)
{
    assert(buffer != nullptr);
    assert(dir != nullptr);
    assert(outputEntry != nullptr);

    if (bufferSize < sizeof(dirent))
    {
        assert(!"Buffer size too small; use GetDirentSize to get required buffer size");
        return ERANGE;
    }

    // On successful cal to readdir_r, result and &entry should point to the same
    // data; a NULL temp pointer but return of 0 means that we reached the end of the 
    // directory stream; finally, a NULL temp pointer with a positive return value
    // means an error occurred.
    dirent* result = nullptr;
    dirent* entry = (dirent*)buffer;
    int ret = readdir_r(dir, entry, &result);
    if (ret == 0)
    {
        if (result != nullptr)
        {
            assert(result == entry);
            ConvertDirent(*entry, outputEntry);
        }
        else
        {
            ret = -1; // errno values are positive so signal the end-of-stream with a non-error value
            *outputEntry = { };
        }
    }
    else
    {
        *outputEntry = { };
    }

    return ret;
}

extern "C" 
DIR* OpenDir(const char* path)
{
    return opendir(path);
}

extern "C" 
int32_t CloseDir(DIR* dir)
{
    return closedir(dir);
}

extern "C"
int32_t Pipe(int32_t pipeFds[2], int32_t flags)
{
    switch (flags)
    {
    case 0:
        break;
    case PAL_O_CLOEXEC:
        flags = O_CLOEXEC;
        break;
    default:
        assert(!"Unknown flag.");
        errno = EINVAL;
        return -1;
    }

#if HAVE_PIPE2
    return pipe2(pipeFds, flags);
#else
    return pipe(pipeFds); // CLOEXEC intentionally ignored on platforms without pipe2.
#endif
}

extern "C"
int32_t FcntlCanGetSetPipeSz()
{
#if defined(F_GETPIPE_SZ) && defined(F_SETPIPE_SZ)
    return true;
#else
    return false;
#endif
}

extern "C"
int32_t FcntlGetPipeSz(int32_t fd)
{
#ifdef F_GETPIPE_SZ
    return fcntl(fd, F_GETPIPE_SZ);
#else
    errno = ENOTSUP;
    return -1;
#endif
}

extern "C"
int32_t FcntlSetPipeSz(int32_t fd, int32_t size)
{
#ifdef F_SETPIPE_SZ
    return fcntl(fd, F_SETPIPE_SZ, size);
#else
    errno = ENOTSUP;
    return -1;
#endif
}

extern "C"
int32_t MkDir(const char* path, int32_t mode)
{
    return mkdir(path, mode);
}

extern "C"
int32_t ChMod(const char* path, int32_t mode)
{
    return chmod(path, mode);
}

extern "C"
int32_t MkFifo(const char* path, int32_t mode)
{
    return mkfifo(path, mode);
}

extern "C"
int32_t FSync(int32_t fd)
{
    return fsync(fd);
}

extern "C"
int32_t FLock(int32_t fd, LockOperations operation)
{
    return flock(fd, (int32_t)operation);
}

extern "C"
int32_t ChDir(const char* path)
{
    return chdir(path);
}

extern "C"
int32_t Access(const char* path, AccessMode mode)
{
    return access(path, (int32_t)mode);
}

extern "C"
int32_t FnMatch(const char* pattern, const char* path, FnMatchFlags flags)
{
    return fnmatch(pattern, path, (int32_t)flags);
}

extern "C"
int64_t LSeek(int32_t fd, int64_t offset, SeekWhence whence)
{
    return lseek(fd, offset, (int32_t)whence);
}

extern "C"
int32_t Link(const char* source, const char* linkTarget)
{
    return link(source, linkTarget);
}

extern "C"
int32_t MksTemps(char* pathTemplate, int32_t suffixLength)
{
    return mkstemps(pathTemplate, suffixLength);
}
