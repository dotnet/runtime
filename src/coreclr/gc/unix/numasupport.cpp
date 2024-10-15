// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "numasupport.h"

#include <stdlib.h>
#include <unistd.h>
#include <errno.h>
#include <stdio.h>
#include <dirent.h>
#include <string.h>
#include <limits.h>
#include <sys/syscall.h>
#include <minipal/utils.h>

// The highest NUMA node available
int g_highestNumaNode = 0;
// Is numa available
bool g_numaAvailable = false;

#ifdef TARGET_LINUX
static int GetNodeNum(const char* path, bool firstOnly)
{
    DIR *dir;
    struct dirent *entry;
    int result = -1;

    dir = opendir(path);
    if (dir)
    {
        while ((entry = readdir(dir)) != NULL)
        {
            if (strncmp(entry->d_name, "node", STRING_LENGTH("node")))
                continue;

            unsigned long nodeNum = strtoul(entry->d_name + STRING_LENGTH("node"), NULL, 0);
            if (nodeNum > INT_MAX)
                nodeNum = INT_MAX;

            if (result < (int)nodeNum)
                result = (int)nodeNum;

            if (firstOnly)
                break;
        }

        closedir(dir);
    }

    return result;
}
#endif

void NUMASupportInitialize()
{
#ifdef TARGET_LINUX
    if (syscall(__NR_get_mempolicy, NULL, NULL, 0, 0, 0) < 0 && errno == ENOSYS)
        return;

    int highestNumaNode = GetNodeNum("/sys/devices/system/node", false);
    // we only use this implementation when there are two or more NUMA nodes available
    if (highestNumaNode < 1)
        return;

    g_numaAvailable = true;
    g_highestNumaNode = highestNumaNode;
#endif
}

int GetNumaNodeNumByCpu(int cpu)
{
#ifdef TARGET_LINUX
    char path[64];
    if (snprintf(path, sizeof(path), "/sys/devices/system/cpu/cpu%d", cpu) < 0)
        return -1;

    return GetNodeNum(path, true);
#else
    return -1;
#endif
}

long BindMemoryPolicy(void* start, unsigned long len, const unsigned long* nodemask, unsigned long maxnode)
{
#ifdef TARGET_LINUX
    return syscall(__NR_mbind, (long)start, len, 1, (long)nodemask, maxnode, 0);
#else
    return -1;
#endif
}
