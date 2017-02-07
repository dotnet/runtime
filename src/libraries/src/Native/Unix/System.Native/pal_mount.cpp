// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_config.h"
#include "pal_mount.h"
#include "pal_utilities.h"
#include <assert.h>
#include <string.h>
#include <errno.h>

// Check if we should use getmntinfo or /proc/mounts
#if HAVE_MNTINFO
#include <sys/mount.h>
#else
#include <sys/statfs.h>
#include <mntent.h>
#define STRING_BUFFER_SIZE 8192

// Android does not define MNTOPT_RO
#ifndef MNTOPT_RO
#define MNTOPT_RO "r"
#endif
#endif

static int32_t GetMountInfo(MountPointFound onFound)
{
#if HAVE_MNTINFO
    // getmntinfo returns pointers to OS-internal structs, so we don't need to worry about free'ing the object
#if HAVE_STATFS
    struct statfs* mounts = nullptr;
#else
    struct statvfs* mounts = nullptr;
#endif
    int count = getmntinfo(&mounts, 0);
    for (int32_t i = 0; i < count; i++)
    {
        onFound(mounts[i].f_mntonname);
    }

    return 0;
}

#else

    int result = -1;
    FILE* fp = setmntent("/proc/mounts", MNTOPT_RO);
    if (fp != nullptr)
    {
        // The _r version of getmntent needs all buffers to be passed in; however, we don't know how big of a string
        // buffer we will need, so pick something that seems like it will be big enough.
        char buffer[STRING_BUFFER_SIZE] = {};
        mntent entry;
        while (getmntent_r(fp, &entry, buffer, STRING_BUFFER_SIZE) != nullptr)
        {
            onFound(entry.mnt_dir);
        }

        result = endmntent(fp);
        assert(result == 1); // documented to always return 1
        result =
            0; // We need to standardize a success return code between our implementations, so settle on 0 for success
    }

    return result;
}

#endif

extern "C" int32_t SystemNative_GetAllMountPoints(MountPointFound onFound)
{
    return GetMountInfo(onFound);
}

extern "C" int32_t SystemNative_GetSpaceInfoForMountPoint(const char* name, MountPointInformation* mpi)
{
    assert(name != nullptr);
    assert(mpi != nullptr);

#if HAVE_STATFS
    struct statfs stats = {};
    int result = statfs(name, &stats);
#else
    struct statvfs stats = {};
    int result = statvfs(name, &stats);
#endif
    if (result == 0)
    {
        // Note that these have signed integer types on some platforms but mustn't be negative.
        // Also, upcast here (some platforms have smaller types) to 64-bit before multiplying to
        // avoid overflow.
        uint64_t bsize = UnsignedCast(stats.f_bsize);
        uint64_t bavail = UnsignedCast(stats.f_bavail);
        uint64_t bfree = UnsignedCast(stats.f_bfree);
        uint64_t blocks = UnsignedCast(stats.f_blocks);

        mpi->AvailableFreeSpace = bsize * bavail;
        mpi->TotalFreeSpace = bsize * bfree;
        mpi->TotalSize = bsize * blocks;
    }
    else
    {
        *mpi = {};
    }

    return result;
}

extern "C" int32_t
SystemNative_GetFormatInfoForMountPoint(const char* name, char* formatNameBuffer, int32_t bufferLength, int64_t* formatType)
{
    assert((formatNameBuffer != nullptr) && (formatType != nullptr));
    assert(bufferLength > 0);

#if HAVE_STATFS
    struct statfs stats;
    int result = statfs(name, &stats);
#else
    struct statvfs stats;
    int result = statvfs(name, &stats);
#endif
    if (result == 0)
    {

#if HAVE_STATFS_FSTYPENAME || HAVE_STATVFS_FSTYPENAME
#ifdef VFS_NAMELEN
        if (bufferLength < VFS_NAMELEN)
#else
        if (bufferLength < MFSNAMELEN)
#endif
        {
            result = ERANGE;
            *formatType = 0;
        }
        else
        {
            SafeStringCopy(formatNameBuffer, bufferLength, stats.f_fstypename);
            *formatType = -1;
        }
#else
        assert(formatType != nullptr);
        *formatType = SignedCast(stats.f_type);
        SafeStringCopy(formatNameBuffer, bufferLength, "");
#endif
    }
    else
    {
        *formatType = 0;
    }

    return result;
}
