// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "../config.h"
#include "pal_stat.h"
#include <sys/stat.h>

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

static void ConvertFileStatus(const struct stat_& src, FileStatus* dst)
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
    dst->Flags |= FILESTATUSFLAGS_HAS_BIRTHTIME;
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
int32_t FStat(int32_t fileDescriptor, FileStatus* output)
{
    struct stat_ result;
    int ret = fstat_(fileDescriptor, &result);

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
