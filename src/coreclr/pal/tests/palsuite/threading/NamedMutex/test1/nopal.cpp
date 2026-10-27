// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Contains wrappers for functions whose required headers conflict with the PAL

#include <sys/file.h>
#include <sys/stat.h>
#include <sys/types.h>

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

unsigned int test_geteuid()
{
    return geteuid();
}

int test_kill(unsigned int pid)
{
    return kill(pid, SIGKILL);
}

bool TestCreateDirectory(const char *path)
{
    return mkdir(path, S_IRUSR | S_IWUSR | S_IXUSR) == 0;
}

void TestRemoveDirectory(const char *path)
{
    rmdir(path);
}

bool TestPathStatus(const char *path, unsigned int expectedMode, unsigned int expectedUid)
{
    struct stat statInfo;
    return
        lstat(path, &statInfo) == 0 &&
        (statInfo.st_mode & (S_IRWXU | S_IRWXG | S_IRWXO)) == expectedMode &&
        statInfo.st_uid == expectedUid;
}

bool TestPathExists(const char *path)
{
    struct stat statInfo;
    return lstat(path, &statInfo) == 0;
}

bool TestChangeMode(const char *path, unsigned int mode)
{
    return chmod(path, mode) == 0;
}

bool TestFileExists(const char *path)
{
    int fd = open(path, O_RDWR);
    if (fd == -1)
        return false;
    close(fd);
    return true;
}

bool WriteHeaderInfo(const char *path, char sharedMemoryType, char version, int *fdRef)
{
    int fd = open(path, O_CREAT | O_RDWR, S_IRUSR | S_IWUSR | S_IRGRP | S_IWGRP | S_IROTH | S_IWOTH);
    if (fd == -1)
        return false;
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
