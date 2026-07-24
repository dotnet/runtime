// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_GETEXEPATH_H
#define HAVE_MINIPAL_GETEXEPATH_H

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
#elif defined(__OpenBSD__)
#include <string.h>
#include <unistd.h>
#include <sys/stat.h>
#include <sys/sysctl.h>
#elif defined(_WIN32)
#include <windows.h>
#elif defined(__HAIKU__)
#include <FindDirectory.h>
#include <StorageDefs.h>
#elif defined(TARGET_WASI)
#include <string.h>
#elif HAVE_GETAUXVAL
#include <sys/auxv.h>
#endif

#ifdef __cplusplus
extern "C" {
#endif

/**
 * Get the full path to the executable for the current process.
 * Resolves symbolic links. The caller is responsible for releasing the buffer.
 *
 * @return A pointer to a null-terminated string containing the executable path, 
 *         or NULL if an error occurs.
 */
static inline char* minipal_getexepath(void)
{
#if defined(__APPLE__)
    uint32_t len = PATH_MAX;
    char pathBuf[PATH_MAX];
    if (_NSGetExecutablePath(pathBuf, &len) != 0)
    {
        errno = EINVAL;
        return NULL;
    }

    return realpath(pathBuf, NULL);
#elif defined(__FreeBSD__)
    static const int name[] = { CTL_KERN, KERN_PROC, KERN_PROC_PATHNAME, -1 };
    char path[PATH_MAX];
    size_t len = sizeof(path);
    if (sysctl(name, 4, path, &len, NULL, 0) != 0)
    {
        return NULL;
    }

    return strdup(path);
#elif defined(__OpenBSD__)
    const int name[] = { CTL_KERN, KERN_PROC_ARGS, getpid(), KERN_PROC_ARGV };
    size_t len = 0;
    if (sysctl(name, 4, NULL, &len, NULL, 0) != 0 || len == 0)
    {
        return NULL;
    }

    char *buf = (char *)malloc(len);
    if (buf == NULL)
    {
        return NULL;
    }

    if (sysctl(name, 4, buf, &len, NULL, 0) != 0)
    {
        free(buf);
        return NULL;
    }

    // Cast the start of the buffer to access the char * layout safely
    char **argv = (char **)buf;
    const char *exe = argv[0];

    if (strchr(exe, '/') == NULL)
    {
        const char *p = getenv("PATH");
        while (p != NULL && *p != '\0')
        {
            size_t seg = strcspn(p, ":");
            char path[PATH_MAX];
            
            if (snprintf(path, sizeof(path), "%.*s/%s", (int)seg, p, exe) < (int)sizeof(path))
            {
                struct stat sb;
                if (stat(path, &sb) == 0 && S_ISREG(sb.st_mode))
                {
                    char *resolved = realpath(path, NULL);
                    free(buf);
                    return resolved;
                }
            }

            p += seg;
            if (*p == ':')
                p++;
        }
    }

    char *resolved = realpath(exe, NULL);
    free(buf);
    return resolved;
#elif defined(__sun)
    const char* path = getexecname();
    if (path == NULL)
    {
        return NULL;
    }

    return realpath(path, NULL);
#elif defined(__HAIKU__)
    char path[B_PATH_NAME_LENGTH];
    status_t status = find_path(B_APP_IMAGE_SYMBOL, B_FIND_PATH_IMAGE_PATH, NULL, path, B_PATH_NAME_LENGTH);
    if (status != B_OK)
    {
        errno = status;
        return NULL;
    }

    return realpath(path, NULL);
#elif defined(_WIN32)
    char path[MAX_PATH];
    if (GetModuleFileNameA(NULL, path, MAX_PATH) == 0)
    {
        return NULL;
    }

    return strdup(path);
#elif defined(TARGET_BROWSER)
    const char *browserVirtualAppBase = "/"; // keep in sync other places that define browserVirtualAppBase
    return strdup(browserVirtualAppBase);
#elif defined(TARGET_WASI)
    // WASI has no /proc, no AT_EXECFN, and argv[0] is unreliable (often "/").
    // corerun.wasm is launched with the CORE_ROOT env var set to the directory
    // that holds CoreCLR (System.Private.CoreLib.dll and friends). The PAL only
    // needs a path whose dirname is that directory, so synthesize one here.
    const char* coreRoot = getenv("CORE_ROOT");
    if (coreRoot == NULL || coreRoot[0] == '\0')
    {
        return strdup("/");
    }
    size_t coreRootLen = strlen(coreRoot);
    const char* suffix = "/corerun";
    size_t suffixLen = strlen(suffix);
    char* result = (char*)malloc(coreRootLen + suffixLen + 1);
    if (result == NULL)
    {
        return NULL;
    }
    memcpy(result, coreRoot, coreRootLen);
    memcpy(result + coreRootLen, suffix, suffixLen + 1);
    return result;
#else
#ifdef __linux__
    const char* symlinkEntrypointExecutable = "/proc/self/exe";
#else
    const char* symlinkEntrypointExecutable = "/proc/curproc/exe";
#endif

    // Resolve the symlink to the executable from /proc
    char* path = realpath(symlinkEntrypointExecutable, NULL);
    if (path)
    {
        return path;
    }

#if HAVE_GETAUXVAL && defined(AT_EXECFN)
    // fallback to AT_EXECFN, which does not work properly in rare cases
    // when .NET process is set as interpreter (shebang).
    const char* exePath = (const char *)(getauxval(AT_EXECFN));
    if (exePath)
    {
        return realpath(exePath, NULL);
    }
#endif // HAVE_GETAUXVAL && defined(AT_EXECFN)

    return NULL;
#endif // defined(__APPLE__)
}

#ifdef __cplusplus
}
#endif // extern "C"

#endif // HAVE_MINIPAL_GETEXEPATH_H
