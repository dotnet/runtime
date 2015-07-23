//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include "nativeio.h"
#include <sys/stat.h>

#if HAVE_STAT64 && !(defined(__APPLE__) && defined(_AMD64_))
#    define stat_ stat64
#    define fstat_ fstat64
#else
#   define stat_ stat
#   define fstat_ fstat
#endif

static void ConvertFileStats(const struct stat_& src, FileStats* dst)
{
    dst->Flags = FILESTATS_FLAGS_NONE;
    dst->Mode = src.st_mode;
    dst->Uid = src.st_uid;
    dst->Gid = src.st_gid;
    dst->Size = src.st_size;
    dst->AccessTime = src.st_atime;
    dst->ModificationTime = src.st_mtime;
    dst->StatusChangeTime = src.st_ctime;

#if HAVE_STAT_BIRTHTIME
    dst->CreationTime = src->st_birthtime;
    dst->Flags |= FILESTATS_FLAGS_HAS_CREATION_TIME;
#endif
}

extern "C"
{
    int32_t Stat(const char* path, struct FileStats* output)
    {
        struct stat_ result;
        int ret = stat_(path, &result);

        if (ret == 0)
        {
            ConvertFileStats(result, output);
        }

        return ret; // TODO: errno conversion
    }

    int32_t FStat(int32_t fileDescriptor, FileStats* output)
    {
        struct stat_ result;
        int ret = fstat_(fileDescriptor, &result);

        if (ret == 0)
        {
            ConvertFileStats(result, output);
        }

        return ret; // TODO: errno conversion
    }
}
