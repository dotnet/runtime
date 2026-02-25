// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_config.h"
#include "pal_mount.h"
#include "pal_utilities.h"
#include "pal_safecrt.h"
#include <assert.h>
#include <string.h>
#include <errno.h>
#include <limits.h>
#include <stdlib.h>

// Check if we should use getfsstat or /proc/mounts
#if HAVE_MNTINFO
#include <sys/mount.h>
#else
#if HAVE_SYS_STATFS_H
#include <sys/statfs.h>
#endif
#if HAVE_SYS_MNTENT_H
#include <sys/mntent.h>
#include <sys/mnttab.h>
#elif HAVE_MNTENT_H
#include <mntent.h>
#endif
#include <sys/statvfs.h>
#define STRING_BUFFER_SIZE 8192

#ifdef __HAIKU__
#include <dirent.h>
#include <fs_info.h>
#include <fs_query.h>
#endif // __HAIKU__

// Android does not define MNTOPT_RO
#ifndef MNTOPT_RO
#define MNTOPT_RO "r"
#endif
#endif

int32_t SystemNative_GetAllMountPoints(MountPointFound onFound, void* context)
{
#if HAVE_MNTINFO
    // Use getfsstat which is thread-safe (unlike getmntinfo which uses internal static buffers)
#if HAVE_STATFS
    struct statfs* mounts = NULL;
#else
    struct statvfs* mounts = NULL;
#endif

    int count;
    int capacity = 0;
    size_t bufferSize = 0;

    // Loop to handle the case where mount points are added between calls
    while (1)
    {
        // Get the current number of mount points
        count = getfsstat(NULL, 0, MNT_NOWAIT);
        if (count < 0)
        {
            free(mounts);
            return -1;
        }

        // Reallocate buffer if needed - allocate one extra to detect if more mounts were added
        if (count >= capacity)
        {
            free(mounts);
            capacity = count + 1;
            if (!multiply_s((size_t)capacity, sizeof(*mounts), &bufferSize))
            {
                errno = ENOMEM;
                return -1;
            }
            if (bufferSize > INT_MAX)
            {
                errno = ENOMEM;
                return -1;
            }
#if HAVE_STATFS
            mounts = (struct statfs*)malloc(bufferSize);
#else
            mounts = (struct statvfs*)malloc(bufferSize);
#endif
            if (mounts == NULL)
            {
                errno = ENOMEM;
                return -1;
            }
        }

        // If count is 0, break - post-loop code handles empty case
        if (count == 0)
        {
            break;
        }

        // Get actual mount point information
        count = getfsstat(mounts, (int)bufferSize, MNT_NOWAIT);
        if (count < 0)
        {
            free(mounts);
            return -1;
        }

        // If count is less than capacity, we got all mount points
        if (count < capacity)
        {
            break;
        }
        // Otherwise, more mounts were added - loop again with larger buffer
    }

    for (int32_t i = 0; i < count; i++)
    {
        onFound(context, mounts[i].f_mntonname);
    }

    free(mounts);
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
            onFound(context, entry.mnt_mountp);
        }

        result = fclose(fp);
        assert(result == 1); // documented to always return 1
        result =
            0; // We need to standardize a success return code between our implementations, so settle on 0 for success
    }

    return result;
}

#elif HAVE_MNTENT_H
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
            onFound(context, entry.mnt_dir);
        }

        result = endmntent(fp);
        assert(result == 1); // documented to always return 1
        result =
            0; // We need to standardize a success return code between our implementations, so settle on 0 for success
    }

    return result;
}

#elif defined(__HAIKU__)
    int32 cookie = 0;
    dev_t currentDev;

    while ((long)(currentDev = next_dev(&cookie)) >= 0)
    {
        struct fs_info info;
        if (fs_stat_dev(currentDev, &info) != B_OK)
        {
            continue;
        }

        char name[STRING_BUFFER_SIZE];
        // Two bytes for the name as we're storing "."
        char buf[sizeof(struct dirent) + 2];
        struct dirent *entry = (struct dirent *)&buf;
        strncpy(entry->d_name, ".", 2);
        entry->d_pdev = currentDev;
        entry->d_pino = info.root;

        if (get_path_for_dirent(entry, name, sizeof(name)) != B_OK)
        {
            continue;
        }

        onFound(context, name);
    }

    return 0;
}
#else
#error "Don't know how to enumerate mount points on this platform"
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
SystemNative_GetFileSystemTypeNameForMountPoint(const char* name, char* formatNameBuffer, int32_t bufferLength, int64_t* formatType)
{
    assert((formatNameBuffer != NULL) && (formatType != NULL));
    assert(bufferLength > 0);

#if HAVE_NON_LEGACY_STATFS
    struct statfs stats;
    int result = statfs(name, &stats);
#elif defined(__HAIKU__)
    struct fs_info stats;
    int result = fs_stat_dev(dev_for_path(name), &stats);
#else
    struct statvfs stats;
    int result = statvfs(name, &stats);
#endif

    if (result == 0)
    {
#if HAVE_STATFS_FSTYPENAME || HAVE_STATVFS_FSTYPENAME
#ifdef HAVE_STATFS_FSTYPENAME
        if (bufferLength < (MFSNAMELEN + 1)) // MFSNAMELEN does not include the null byte
#elif HAVE_STATVFS_FSTYPENAME
        if (bufferLength < VFS_NAMELEN) // VFS_NAMELEN includes the null byte
#endif
        {
            errno = ERANGE;
            result = -1;
        }
        SafeStringCopy(formatNameBuffer, Int32ToSizeT(bufferLength), stats.f_fstypename);
        *formatType = -1;
#elif HAVE_STATVFS_BASETYPE
        if (bufferLength < _FSTYPSZ)        // SunOS
        {
            errno = ERANGE;
            result = -1;
        }
        SafeStringCopy(formatNameBuffer, Int32ToSizeT(bufferLength), stats.f_basetype);
        *formatType = -1;
#elif defined(__HAIKU__)
        if (bufferLength < B_OS_NAME_LENGTH)
        {
            result = ERANGE;
            *formatType = 0;
        }
        else
        {
            SafeStringCopy(formatNameBuffer, Int32ToSizeT(bufferLength), stats.fsh_name);
            *formatType = -1;
        }
#else
        SafeStringCopy(formatNameBuffer, Int32ToSizeT(bufferLength), "");
        *formatType = (int64_t)(stats.f_type);
#endif
    }
    else
    {
        SafeStringCopy(formatNameBuffer, Int32ToSizeT(bufferLength), "");
        *formatType = -1;
    }

    return result;
}
