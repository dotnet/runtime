// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Contains wrappers for functions whose required headers conflict with the PAL

#include <sys/file.h>
#include <sys/stat.h>
#include <sys/types.h>

#include <errno.h>
#include <fcntl.h>
#include <signal.h>
#include <stdio.h>
#include <string.h>
#include <unistd.h>
#include <minipal/utils.h>

auto test_strcpy = strcpy;
auto test_strcmp = strcmp;
auto test_strlen = strlen;
auto test_snprintf = snprintf;
auto test_sscanf = sscanf;
auto test_close = close;
auto test_unlink = unlink;

unsigned int test_getpid()
{
    return getpid();
}

unsigned int test_getsid()
{
    return getsid(0);
}

unsigned int test_geteuid()
{
    return geteuid();
}

int test_kill(unsigned int pid)
{
    return kill(pid, SIGKILL);
}

bool TestFileExists(const char *path)
{
    int fd = open(path, O_RDWR);
    if (fd == -1)
        return false;
    close(fd);
    return true;
}

bool WriteHeaderInfo(const char *path, bool currentUserOnly, char sharedMemoryType, char version, int *fdRef)
{
    int fd = open(path, O_CREAT | O_RDWR, S_IRUSR | S_IWUSR | S_IRGRP | S_IWGRP | S_IROTH | S_IWOTH);
    if (fd == -1)
        return false;

    if (currentUserOnly)
    {
        int chmodResult;
        do
        {
            chmodResult = chmod(path, S_IRUSR | S_IWUSR);
        } while (chmodResult != 0 && errno == EINTR);

        if (chmodResult != 0)
            return false;
    }

    *fdRef = fd;
    if (ftruncate(fd, getpagesize()) != 0)
        return false;
    if (lseek(fd, 0, SEEK_SET) != 0)
        return false;

    // See SharedMemorySharedDataHeader for format
    char buffer[] = {sharedMemoryType, version};
    if (write(fd, buffer, ARRAY_SIZE(buffer)) != ARRAY_SIZE(buffer))
        return false;

    return flock(fd, LOCK_SH | LOCK_NB) == 0;
}
