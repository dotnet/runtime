// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "minipalconfig.h"

#include <errno.h>
#include <fcntl.h>
#include <sys/stat.h>
#include <unistd.h>

#include "fileio.h"

int minipal_create_owner_only_file_for_overwrite(const char* path)
{
    if (path == NULL)
    {
        errno = EINVAL;
        return -1;
    }

    struct stat fileStatus;
    if (lstat(path, &fileStatus) == 0)
    {
        if (!S_ISREG(fileStatus.st_mode))
        {
            errno = EEXIST;
            return -1;
        }

        if (unlink(path) == -1)
        {
            return -1;
        }
    }
    else if (errno != ENOENT)
    {
        return -1;
    }

    int flags = O_WRONLY | O_CREAT | O_EXCL;
#if HAVE_O_CLOEXEC
    flags |= O_CLOEXEC;
#endif

    int fd = open(path, flags, S_IRUSR | S_IWUSR);
#if !HAVE_O_CLOEXEC
    if (fd != -1 && fcntl(fd, F_SETFD, FD_CLOEXEC) == -1)
    {
        int error = errno;
        close(fd);
        errno = error;
        return -1;
    }
#endif

    return fd;
}
