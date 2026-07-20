// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "vmlimit.h"

#include <stdio.h>
#include <stdlib.h>
#include <inttypes.h>

#include <sys/resource.h>

#ifdef TARGET_LINUX
static size_t get_vmalloc_total(void)
{
    FILE* memInfoFile = fopen("/proc/meminfo", "r");
    if (memInfoFile == NULL)
    {
        return SIZE_MAX;
    }

    size_t result = SIZE_MAX;
    char* line = NULL;
    size_t lineLen = 0;

    while (getline(&line, &lineLen, memInfoFile) != -1)
    {
        uint64_t value;
        char units = '\0';
        if (sscanf(line, "VmallocTotal: %" SCNu64 " %cB", &value, &units) >= 1)
        {
            uint64_t multiplier = 1;
            switch (units)
            {
                case 'g':
                case 'G': multiplier = 1024 * 1024 * 1024; break;
                case 'm':
                case 'M': multiplier = 1024 * 1024; break;
                case 'k':
                case 'K': multiplier = 1024; break;
            }

            uint64_t total = value * multiplier;
            result = (total < (uint64_t)SIZE_MAX) ? (size_t)total : SIZE_MAX;
            break;
        }
    }

    free(line);
    fclose(memInfoFile);

    return result;
}
#endif // TARGET_LINUX

size_t minipal_get_virtual_address_space_limit(void)
{
    // Cache the result since the values don't change during process lifetime.
    // Use 0 as the "not yet computed" sentinel since a zero limit is not a valid result.
    static volatile size_t cached_limit = 0;

    if (cached_limit != 0)
    {
        return cached_limit;
    }

    size_t limit = SIZE_MAX;

#if defined(RLIMIT_AS) || defined(RLIMIT_DATA)
#ifdef RLIMIT_AS
    int addressSpace = RLIMIT_AS;
#else
    int addressSpace = RLIMIT_DATA;
#endif

    struct rlimit addressSpaceLimit;
    if ((getrlimit(addressSpace, &addressSpaceLimit) == 0) && (addressSpaceLimit.rlim_cur != RLIM_INFINITY))
    {
        limit = addressSpaceLimit.rlim_cur;
    }
#endif // RLIMIT_AS || RLIMIT_DATA

#ifdef TARGET_LINUX
    size_t vmallocTotal = get_vmalloc_total();
    if (vmallocTotal < limit)
    {
        limit = vmallocTotal;
    }
#endif // TARGET_LINUX

    cached_limit = limit;

    return limit;
}
