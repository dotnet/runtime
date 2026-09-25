// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "cpucount.h"

#include <stdio.h>
#include <unistd.h>

#if defined(__linux__)
// Parses a kernel cpulist file (comma-separated ranges like "0-3,5-7").
// Sets *outCount to total CPU count and *outMaxIndex to highest index on success.
static int parse_cpulist_file(const char* path, int* outCount, int* outMaxIndex)
{
    FILE* f = fopen(path, "r");
    if (f == NULL)
    {
        return 0;
    }

    int count = 0;
    int maxIndex = -1;
    int parseSuccess = 0;
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
            // Unexpected format, discard
            count = 0;
            break;
        }

        if (hi < lo)
        {
            // Invalid range, discard
            count = 0;
            break;
        }

        count += hi - lo + 1;
        if (hi > maxIndex)
        {
            maxIndex = hi;
        }

        int ch = fgetc(f);
        if (ch == ',')
        {
            continue;
        }
        else if (ch == '\n' || ch == EOF)
        {
            parseSuccess = 1;
            break;
        }
        else
        {
            // Unexpected character, discard
            count = 0;
            break;
        }
    }

    fclose(f);

    if (parseSuccess && count > 0)
    {
        if (outCount) *outCount = count;
        if (outMaxIndex) *outMaxIndex = maxIndex;
    }
    return parseSuccess && count > 0;
}
#endif

int minipal_get_cpu_max_possible_count(void)
{
#if defined(__linux__)
    int maxIndex;
    if (parse_cpulist_file("/sys/devices/system/cpu/possible", NULL, &maxIndex))
    {
        return maxIndex + 1;
    }
#endif
    return (int)sysconf(_SC_NPROCESSORS_CONF);
}

int minipal_get_cpu_present_count(void)
{
#if defined(__linux__)
    int count;
    if (parse_cpulist_file("/sys/devices/system/cpu/present", &count, NULL))
    {
        return count;
    }
#endif
    return -1;
}