// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "cpucount.h"

#include <stdio.h>
#include <unistd.h>

int minipal_get_cpu_max_possible_count(void)
{
#if defined(__linux__)
    int hi;
    FILE* f = fopen("/sys/devices/system/cpu/possible", "r");
    if (f != NULL)
    {
        if (fscanf(f, "%*d-%d", &hi) == 1)
        {
            fclose(f);
            return hi + 1;
        }
        fclose(f);
    }
#endif

    return (int)sysconf(_SC_NPROCESSORS_CONF);
}
