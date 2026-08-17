// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "cpucount.h"

#include <stdio.h>
#include <unistd.h>

int minipal_get_cpu_max_possible_count(void)
{
#if defined(__linux__)
    FILE* f = fopen("/sys/devices/system/cpu/possible", "r");
    if (f != NULL)
    {
        int maxCpu = -1;
        for (;;)
        {
            int lo, hi;
            int matched = fscanf(f, "%d-%d", &lo, &hi);
            if (matched == 1)
            {
                hi = lo;
            }
            else if (matched != 2)
            {
                break;
            }

            if (maxCpu < hi)
            {
                maxCpu = hi;
            }

            int ch = fgetc(f);
            if (ch == EOF || ch != ',')
            {
                break;
            }
        }
        fclose(f);
        if (maxCpu != -1)
        {
            return maxCpu + 1;
        }
    }
#endif

    return (int)sysconf(_SC_NPROCESSORS_CONF);
}
