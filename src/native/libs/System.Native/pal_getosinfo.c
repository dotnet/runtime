// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_config.h"
#include "pal_getosinfo.h"
#include "pal_utilities.h"

#include <errno.h>
#include <string.h>

#if HAVE_OS_H
#include <OS.h>
#endif

int32_t SystemNative_GetNextAreaInfo(int32_t team, intptr_t* cookie, AreaInfo* areaInfo)
{
#if HAVE_OS_H
    if (cookie == NULL || areaInfo == NULL)
    {
        return EINVAL;
    }

    area_info nativeAreaInfo;
    memset(&nativeAreaInfo, 0, sizeof(nativeAreaInfo));

    status_t status = get_next_area_info((team_id)team, cookie, &nativeAreaInfo);
    if (status != B_OK)
    {
        return (int32_t)status;
    }

    areaInfo->size = (uintptr_t)nativeAreaInfo.size;
    areaInfo->ram_size = (uint32_t)nativeAreaInfo.ram_size;

    return (int32_t)status;
#else
    (void)team;
    (void)cookie;
    (void)areaInfo;
    return ENOTSUP;
#endif
}
