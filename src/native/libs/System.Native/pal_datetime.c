// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_config.h"
#include "pal_datetime.h"
#include "pal_utilities.h"
#include <limits.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include <sys/time.h>
#if defined(TARGET_ANDROID)
#include <sys/system_properties.h>
#endif
#include <time.h>

static const int64_t TICKS_PER_SECOND = 10000000; /* 10^7 */
#if HAVE_CLOCK_REALTIME
static const int64_t NANOSECONDS_PER_TICK = 100;
#else
static const int64_t TICKS_PER_MICROSECOND = 10; /* 1000 / 100 */
#endif

#if defined(TARGET_WASI) || defined(TARGET_BROWSER)
extern bool mono_bundled_resources_get_data_resource_values (const char *id, const uint8_t **data_out, uint32_t *size_out);
#endif

//
// SystemNative_GetSystemTimeAsTicks return the system time as ticks (100 nanoseconds) 
// since 00:00 01 January 1970 UTC (Unix epoch) 
//
int64_t SystemNative_GetSystemTimeAsTicks(void)
{
#if HAVE_CLOCK_REALTIME
    struct timespec time;
    if (clock_gettime(CLOCK_REALTIME, &time) == 0)
    {
        return (int64_t)(time.tv_sec) * TICKS_PER_SECOND + (time.tv_nsec / NANOSECONDS_PER_TICK); 
    }
#else
    struct timeval time;
    if (gettimeofday(&time, NULL) == 0)
    {
        return (int64_t)(time.tv_sec) * TICKS_PER_SECOND + (time.tv_usec * TICKS_PER_MICROSECOND); 
    }
#endif
    // in failure we return 00:00 01 January 1970 UTC (Unix epoch)
    return 0;
}

#if defined(TARGET_ANDROID)
char* SystemNative_GetDefaultTimeZone(void)
{
    char defaulttimezone[PROP_VALUE_MAX];
    if (__system_property_get("persist.sys.timezone", defaulttimezone))
    {
        return strdup(defaulttimezone);
    }
    else
    {
        return NULL;
    }
}
#endif

const char* SystemNative_GetTimeZoneData(const char* name, int* length)
{
    assert(name != NULL);
    assert(length != NULL);
#if defined(TARGET_WASI) || defined(TARGET_BROWSER)
    const uint8_t *data = NULL;
    uint32_t data_len = 0;

    mono_bundled_resources_get_data_resource_values (name, &data, &data_len);
    assert (data_len <= INT_MAX);
    if (data_len > INT_MAX) {
        data_len = 0;
        data = NULL;
    }

    *length = (int)data_len;
    return (const char *)data;
#else
    assert_msg(false, "Not supported on this platform", 0);
    (void)name; // unused
    (void)length; // unused
    return NULL;
#endif
}
