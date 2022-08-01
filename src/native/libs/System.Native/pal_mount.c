// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_config.h"
#include "pal_mount.h"
#include "pal_utilities.h"
#include <assert.h>
#include <string.h>
#include <errno.h>
#include <limits.h>

// Check if we should use getmntinfo or /proc/mounts
#if HAVE_MNTINFO
#include <sys/mount.h>
#else
#include <sys/statfs.h>
#if HAVE_SYS_MNTENT_H
#include <sys/mntent.h>
#include <sys/mnttab.h>
#include <sys/statvfs.h>
#else
#include <mntent.h>
#endif
#define STRING_BUFFER_SIZE 8192

// Android does not define MNTOPT_RO
#ifndef MNTOPT_RO
#define MNTOPT_RO "r"
#endif
#endif

int32_t SystemNative_GetAllMountPoints(MountPointFound onFound)
{
#if HAVE_MNTINFO
    // getmntinfo returns pointers to OS-internal structs, so we don't need to worry about free'ing the object
#if HAVE_STATFS
    struct statfs* mounts = NULL;
#else
    struct statvfs* mounts = NULL;
#endif
    int count = getmntinfo(&mounts, MNT_WAIT);
    for (int32_t i = 0; i < count; i++)
    {
        onFound(mounts[i].f_mntonname);
    }

    return 0;
}

#elif HAVE_SYS_MNTENT_H
    int result = -1;
    FILE* fp = fopen("/proc/mounts", MNTOPT_RO);
    if (fp != NULL)
    {
        char buffer[STRING_BUFFER_SIZE] = {0};
        struct mnttab entry;
        while(getmntent(fp, &entry) == 0)
        {
            onFound(entry.mnt_mountp);
        }

        result = fclose(fp);
        assert(result == 1); // documented to always return 1
        result =
            0; // We need to standardize a success return code between our implementations, so settle on 0 for success
    }

    return result;
}

#else
    int result = -1;
    FILE* fp = setmntent("/proc/mounts", MNTOPT_RO);
    if (fp != NULL)
    {
        // The _r version of getmntent needs all buffers to be passed in; however, we don't know how big of a string
        // buffer we will need, so pick something that seems like it will be big enough.
        char buffer[STRING_BUFFER_SIZE] = {0};
        struct mntent entry;
        while (getmntent_r(fp, &entry, buffer, STRING_BUFFER_SIZE) != NULL)
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

int32_t SystemNative_GetSpaceInfoForMountPoint(const char* name, MountPointInformation* mpi)
{
    assert(name != NULL);
    assert(mpi != NULL);

#if HAVE_NON_LEGACY_STATFS
    struct statfs stats;
    memset(&stats, 0, sizeof(struct statfs));

    int result = statfs(name, &stats);
#else
    struct statvfs stats;
    memset(&stats, 0, sizeof(struct statvfs));

    int result = statvfs(name, &stats);
#endif
    if (result == 0)
    {
        // Note that these have signed integer types on some platforms but mustn't be negative.
        // Also, upcast here (some platforms have smaller types) to 64-bit before multiplying to
        // avoid overflow.
        uint64_t bsize = (uint64_t)(stats.f_bsize);
        uint64_t bavail = (uint64_t)(stats.f_bavail);
        uint64_t bfree = (uint64_t)(stats.f_bfree);
        uint64_t blocks = (uint64_t)(stats.f_blocks);

        mpi->AvailableFreeSpace = bsize * bavail;
        mpi->TotalFreeSpace = bsize * bfree;
        mpi->TotalSize = bsize * blocks;
    }
    else
    {
        memset(mpi, 0, sizeof(MountPointInformation));
    }

    return result;
}

int32_t
SystemNative_GetFormatInfoForMountPoint(const char* name, char* formatNameBuffer, int32_t bufferLength, int64_t* formatType)
{
    assert((formatNameBuffer != NULL) && (formatType != NULL));
    assert(bufferLength > 0);

#if HAVE_NON_LEGACY_STATFS
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
            SafeStringCopy(formatNameBuffer, Int32ToSizeT(bufferLength), stats.f_fstypename);
            *formatType = -1;
        }
#elif HAVE_NON_LEGACY_STATFS
        assert(formatType != NULL);
        *formatType = (int64_t)(stats.f_type);
        SafeStringCopy(formatNameBuffer, Int32ToSizeT(bufferLength), "");
#else
        *formatType = 0;
#endif
    }
    else
    {
        *formatType = 0;
    }

    return result;
}
