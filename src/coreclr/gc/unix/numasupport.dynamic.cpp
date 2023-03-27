// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "numasupport.h"

// The highest NUMA node available
int g_highestNumaNode = 0;
// Is numa available
bool g_numaAvailable = false;

#if HAVE_NUMA_H
#include <unistd.h>
#include <errno.h>
#include <stdio.h>
#include <dlfcn.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <fcntl.h>

#define PER_FUNCTION_BLOCK(fn) decltype(fn)* fn##_ptr;
FOR_ALL_NUMA_FUNCTIONS
#undef PER_FUNCTION_BLOCK

void* g_numaHandle = nullptr;

static bool ShouldOpenLibNuma()
{
#ifdef TARGET_LINUX
    // This is a simple heuristic to determine if libnuma.so should be opened.  There's
    // no point in linking and resolving everything in this library if we're running on
    // a system that's not NUMA-capable.
    int fd = open("/sys/devices/system/node/possible", O_RDONLY | O_CLOEXEC);

    if (fd == -1)
    {
        // sysfs might not be mounted, not available, or the interface might have
        // changed.  Return `true' here so NUMASupportInitialize() can try initializing
        // NUMA support with libnuma.
        return true;
    }

    while (true)
    {
        char buffer[32];
        ssize_t bytesRead = read(fd, buffer, 32);

        if (bytesRead == -1 && errno == EINTR)
        {
            continue;
        }

        close(fd);

        // If an unknown error happened (bytesRead < 0), or the file was empty
        // (bytesRead = 0), let libnuma handle this.  Otherwise, if there's just
        // one NUMA node, don't bother linking in libnuma.
        return (bytesRead <= 0) ? true : strncmp(buffer, "0\n", bytesRead) != 0;
    }
#else
    return true;
#endif // TARGET_LINUX
}

#endif // HAVE_NUMA_H

// Initialize data structures for getting and setting thread affinities to processors and
// querying NUMA related processor information.
// On systems with no NUMA support, it behaves as if there was a single NUMA node with
// a single group of processors.
void NUMASupportInitialize()
{
#if HAVE_NUMA_H
    if (!ShouldOpenLibNuma())
    {
        g_numaAvailable = false;
        g_highestNumaNode = 0;
        return;
    }

    g_numaHandle = dlopen("libnuma.so.1", RTLD_LAZY);
    if (g_numaHandle == 0)
    {
        g_numaHandle = dlopen("libnuma.so.1.0.0", RTLD_LAZY);
        if (g_numaHandle == 0)
        {
            g_numaHandle = dlopen("libnuma.so", RTLD_LAZY);
        }
    }
    if (g_numaHandle != 0)
    {
#define PER_FUNCTION_BLOCK(fn) \
    fn##_ptr = (decltype(fn)*)dlsym(g_numaHandle, #fn); \
    if (fn##_ptr == NULL) { fprintf(stderr, "Cannot get symbol " #fn " from libnuma\n"); abort(); }
FOR_ALL_NUMA_FUNCTIONS
#undef PER_FUNCTION_BLOCK

        if (numa_available() == -1)
        {
            dlclose(g_numaHandle);
        }
        else
        {
            g_numaAvailable = true;
            g_highestNumaNode = numa_max_node();
        }
    }
#endif // HAVE_NUMA_H
    if (!g_numaAvailable)
    {
        // No NUMA
        g_highestNumaNode = 0;
    }
}

// Cleanup of the NUMA support data structures
void NUMASupportCleanup()
{
#if HAVE_NUMA_H
    if (g_numaAvailable)
    {
        dlclose(g_numaHandle);
    }
#endif // HAVE_NUMA_H
}
