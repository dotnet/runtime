// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef GETEXEPATH_H
#define GETEXEPATH_H

#include <errno.h>
#include <limits.h>
#include <stdlib.h>

#if defined(__APPLE__)
#include <mach-o/dyld.h>
#elif defined(__FreeBSD__)
#include <string.h>
#include <sys/types.h>
#include <sys/param.h>
#include <sys/sysctl.h>
#endif

#ifdef __cplusplus
extern "C" {
#endif

// Returns the full path to the executable for the current process, resolving symbolic links.
// The caller is responsible for releasing the buffer. Returns null on error.
extern inline char* getexepath(void)
{
#if defined(__APPLE__)
    uint32_t path_length = 0;
    if (_NSGetExecutablePath(NULL, &path_length) != -1)
    {
        errno = EINVAL;
        return NULL;
    }

    char path_buf[path_length];
    if (_NSGetExecutablePath(path_buf, &path_length) != 0)
    {
        errno = EINVAL;
        return NULL;
    }

    return realpath(path_buf, NULL);
#elif defined(__FreeBSD__)
    static const int name[] = { CTL_KERN, KERN_PROC, KERN_PROC_PATHNAME, -1 };
    char path[PATH_MAX];
    size_t len = sizeof(path);
    if (sysctl(name, 4, path, &len, NULL, 0) != 0)
    {
        return NULL;
    }

    return strdup(path);
#elif defined(__sun)
    const char* path = getexecname();
    if (path == NULL)
    {
        return NULL;
    }

    return realpath(path, NULL);
#else
#if HAVE_GETAUXVAL && defined(AT_EXECFN)
    const char* path = (const char *)getauxval(AT_EXECFN);
    if (path)
    {
        return realpath(path, NULL);
    }
#endif // HAVE_GETAUXVAL && defined(AT_EXECFN)
#ifdef __linux__
    const char* symlinkEntrypointExecutable = "/proc/self/exe";
#else
    const char* symlinkEntrypointExecutable = "/proc/curproc/exe";
#endif

    // Resolve the symlink to the executable from /proc
    return realpath(symlinkEntrypointExecutable, NULL);
#endif // defined(__APPLE__)
}

#ifdef __cplusplus
}
#endif // extern "C"

#endif // GETEXEPATH_H
