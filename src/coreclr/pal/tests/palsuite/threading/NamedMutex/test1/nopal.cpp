// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Contains wrappers for functions whose required headers conflict with the PAL

#include <sys/file.h>
#include <sys/stat.h>
#include <sys/types.h>

#include <errno.h>
#include <fcntl.h>
#include <pwd.h>
#include <signal.h>
#include <stdio.h>
#include <stdlib.h>
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

static bool GetExpectedBasePath(char *path, size_t pathSize, bool isUserScope);

bool PrepareUserScopeBase(char *path, size_t pathSize)
{
#if defined(__APPLE__) || defined(__linux__)
#ifdef __APPLE__
    const char *applicationGroupId = getenv("DOTNET_SANDBOX_APPLICATION_GROUP_ID");
    if (applicationGroupId != nullptr && applicationGroupId[0] != '\0')
        return GetExpectedBasePath(path, pathSize, true /* isUserScope */);
#endif

    if (getcwd(path, pathSize) == nullptr)
        return false;

    size_t pathLength = strlen(path);
    int written = snprintf(
        path + pathLength,
        pathSize - pathLength,
        "/paltest_namedmutex_user_%u",
        getpid());
    if (written <= 0 || static_cast<size_t>(written) >= pathSize - pathLength)
        return false;

    if (mkdir(path, S_IRUSR | S_IWUSR | S_IXUSR) != 0 && errno != EEXIST)
        return false;

    if (chmod(path, S_IRUSR | S_IWUSR | S_IXUSR) != 0)
        return false;

#ifdef __APPLE__
    if (setenv("TMPDIR", path, 1) != 0)
        return false;
#else
    if (setenv("XDG_RUNTIME_DIR", path, 1) != 0)
        return false;
#endif

    return GetExpectedBasePath(path, pathSize, true /* isUserScope */);
#else
    return GetExpectedBasePath(path, pathSize, true /* isUserScope */);
#endif
}

static bool GetExpectedBasePath(char *path, size_t pathSize, bool isUserScope)
{
    const char *base = nullptr;
    bool useUserFallback = false;

#ifdef __APPLE__
    const char *applicationGroupId = getenv("DOTNET_SANDBOX_APPLICATION_GROUP_ID");
    if (applicationGroupId != nullptr && applicationGroupId[0] != '\0')
    {
        struct passwd *passwordEntry = getpwuid(getuid());
        if (passwordEntry == nullptr || passwordEntry->pw_dir == nullptr)
            return false;

        int written = snprintf(
            path,
            pathSize,
            "%s/Library/Group Containers/%s",
            passwordEntry->pw_dir,
            applicationGroupId);
        return written > 0 && static_cast<size_t>(written) < pathSize;
    }

    if (isUserScope)
    {
        base = getenv("TMPDIR");
        useUserFallback = base == nullptr || base[0] == '\0';
    }
    else
    {
        base = "/tmp";
    }
#elif defined(__linux__)
    if (isUserScope)
    {
        base = getenv("XDG_RUNTIME_DIR");
        useUserFallback = base == nullptr || base[0] == '\0';
    }
    else
    {
        base = "/tmp";
    }
#else
    if (isUserScope)
    {
        useUserFallback = true;
    }
    else
    {
        base = "/tmp";
    }
#endif

    if (!useUserFallback)
    {
        int written = snprintf(path, pathSize, "%s", base);
        return written > 0 && static_cast<size_t>(written) < pathSize;
    }

    const char *home = getenv("HOME");
    if (home == nullptr || home[0] == '\0')
    {
        struct passwd *passwordEntry = getpwuid(geteuid());
        if (passwordEntry == nullptr || passwordEntry->pw_dir == nullptr)
            return false;
        home = passwordEntry->pw_dir;
    }

    int written = snprintf(path, pathSize, "%s/.dotnet/corefx/Runtime/SharedMemory", home);
    return written > 0 && static_cast<size_t>(written) < pathSize;
}

bool BuildExpectedSharedMemoryPath(
    char *path,
    size_t pathSize,
    bool isUserScope,
    const char *contentsDirectoryName,
    bool includeSessionDirectory,
    const char *name)
{
    if (!GetExpectedBasePath(path, pathSize, isUserScope))
        return false;

    size_t pathLength = strlen(path);
    int written = snprintf(
        path + pathLength,
        pathSize - pathLength,
        "/.dotnet/%s",
        contentsDirectoryName);
    if (written <= 0 || static_cast<size_t>(written) >= pathSize - pathLength)
        return false;

    if (includeSessionDirectory)
    {
        pathLength = strlen(path);
        written = snprintf(path + pathLength, pathSize - pathLength, "/session%u", getsid(0));
        if (written <= 0 || static_cast<size_t>(written) >= pathSize - pathLength)
            return false;
    }

    if (name != nullptr)
    {
        pathLength = strlen(path);
        written = snprintf(path + pathLength, pathSize - pathLength, "/%s", name);
        if (written <= 0 || static_cast<size_t>(written) >= pathSize - pathLength)
            return false;
    }

    return true;
}

bool BuildExpectedGlobalSharedMemoryPath(char *path, size_t pathSize, const char *name)
{
    if (!GetExpectedBasePath(path, pathSize, false /* isUserScope */))
        return false;

    size_t pathLength = strlen(path);
    int written = snprintf(path + pathLength, pathSize - pathLength, "/.dotnet/shm/global/%s", name);
    return written > 0 && static_cast<size_t>(written) < pathSize - pathLength;
}

bool TestPathAttributes(const char *path, bool isDirectory, unsigned int expectedUid, unsigned int expectedMode)
{
    struct stat statInfo;
    if (lstat(path, &statInfo) != 0)
        return false;

    if ((isDirectory && !S_ISDIR(statInfo.st_mode)) || (!isDirectory && !S_ISREG(statInfo.st_mode)))
        return false;

    return statInfo.st_uid == expectedUid && (statInfo.st_mode & 0777) == expectedMode;
}

bool TestUserBaseAttributes(const char *path, unsigned int expectedUid)
{
    struct stat statInfo;
    if (lstat(path, &statInfo) != 0 || !S_ISDIR(statInfo.st_mode))
        return false;

    return
        (statInfo.st_mode & (S_IRUSR | S_IWUSR | S_IXUSR)) == (S_IRUSR | S_IWUSR | S_IXUSR) &&
        (
            (statInfo.st_mode & S_ISVTX) != 0 ||
            (statInfo.st_uid == expectedUid && (statInfo.st_mode & (S_IWGRP | S_IWOTH)) == 0)
        );
}

void CleanupPreparedUserScopeBase(const char *basePath)
{
#if defined(__APPLE__) || defined(__linux__)
    if (strstr(basePath, "/paltest_namedmutex_user_") == nullptr)
        return;

    char path[4096];
    snprintf(path, sizeof(path), "%s/.dotnet/lockfiles", basePath);
    rmdir(path);
    snprintf(path, sizeof(path), "%s/.dotnet/shm", basePath);
    rmdir(path);
    snprintf(path, sizeof(path), "%s/.dotnet", basePath);
    rmdir(path);
    rmdir(basePath);
#endif
}

bool TestFileExists(const char *path)
{
    int fd = open(path, O_RDWR);
    if (fd == -1)
        return false;
    close(fd);
    return true;
}

bool CreateSymbolicLink(const char *target, const char *linkPath)
{
    return symlink(target, linkPath) == 0;
}

bool WriteHeaderInfo(const char *path, char sharedMemoryType, char version, int *fdRef)
{
    int fd = open(path, O_CREAT | O_RDWR, S_IRUSR | S_IWUSR | S_IRGRP | S_IWGRP | S_IROTH | S_IWOTH);
    if (fd == -1)
        return false;
    if (fchmod(fd, S_IRUSR | S_IWUSR | S_IRGRP | S_IWGRP | S_IROTH | S_IWOTH) != 0)
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
