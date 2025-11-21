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

#if HAVE_IMAGE_H
#include <image.h>
#endif

#if HAVE_OS_H
static void CopyTeamInfo(TeamInfo* info, const team_info* nativeInfo)
{
    memset(info, 0, sizeof(*info));

    info->team = nativeInfo->team;
    info->session_id = nativeInfo->session_id;
    info->parent = nativeInfo->parent;
    SafeStringCopy((char*)info->name, sizeof(info->name), (const char*)nativeInfo->name);
    info->start_time = nativeInfo->start_time;
}

static void CopyThreadInfo(HaikuThreadInfo* info, const thread_info* nativeInfo)
{
    memset(info, 0, sizeof(*info));

    info->thread = nativeInfo->thread;
    info->team = nativeInfo->team;
    info->state = nativeInfo->state;
    info->priority = nativeInfo->priority;
    info->user_time = nativeInfo->user_time;
    info->kernel_time = nativeInfo->kernel_time;
}
#endif

#if HAVE_IMAGE_H
c_static_assert(SYSTEMNATIVE_MAX_PATH >= MAXPATHLEN);
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

int32_t SystemNative_GetTeamInfo(int32_t team, TeamInfo* info)
{
#if HAVE_OS_H
    if (info == NULL)
    {
        return EINVAL;
    }

    team_info nativeInfo;
    memset(&nativeInfo, 0, sizeof(nativeInfo));

    status_t status = get_team_info((team_id)team, &nativeInfo);
    if (status != B_OK)
    {
        return (int32_t)status;
    }

    CopyTeamInfo(info, &nativeInfo);
    return (int32_t)status;
#else
    (void)team;
    (void)info;
    return ENOTSUP;
#endif
}

int32_t SystemNative_GetNextTeamInfo(int32_t* cookie, TeamInfo* info)
{
#if HAVE_OS_H
    if (cookie == NULL || info == NULL)
    {
        return EINVAL;
    }

    team_info nativeInfo;
    memset(&nativeInfo, 0, sizeof(nativeInfo));

    status_t status = get_next_team_info(cookie, &nativeInfo);
    if (status != B_OK)
    {
        return (int32_t)status;
    }

    CopyTeamInfo(info, &nativeInfo);
    return (int32_t)status;
#else
    (void)cookie;
    (void)info;
    return ENOTSUP;
#endif
}

int32_t SystemNative_GetNextTeamId(int32_t* cookie, int32_t* team)
{
#if HAVE_OS_H
    if (cookie == NULL || team == NULL)
    {
        return EINVAL;
    }

    team_info nativeInfo;
    memset(&nativeInfo, 0, sizeof(nativeInfo));

    status_t status = get_next_team_info(cookie, &nativeInfo);
    if (status != B_OK)
    {
        return (int32_t)status;
    }

    *team = (int32_t)nativeInfo.team;
    return (int32_t)status;
#else
    (void)cookie;
    (void)team;
    return ENOTSUP;
#endif
}

int32_t SystemNative_GetTeamUsageInfo(int32_t team, int32_t who, TeamUsageInfo* info)
{
#if HAVE_OS_H
    if (info == NULL)
    {
        return EINVAL;
    }

    team_usage_info nativeInfo;
    memset(&nativeInfo, 0, sizeof(nativeInfo));

    status_t status = get_team_usage_info((team_id)team, who, &nativeInfo);
    if (status != B_OK)
    {
        return (int32_t)status;
    }

    info->user_time = nativeInfo.user_time;
    info->kernel_time = nativeInfo.kernel_time;
    return (int32_t)status;
#else
    (void)team;
    (void)who;
    (void)info;
    return ENOTSUP;
#endif
}

int32_t SystemNative_SetThreadPriority(int32_t thread, int32_t newPriority)
{
#if HAVE_OS_H
    status_t status = set_thread_priority((thread_id)thread, newPriority);
    return (int32_t)status;
#else
    (void)thread;
    (void)newPriority;
    return ENOTSUP;
#endif
}

int32_t SystemNative_GetThreadInfo(int32_t thread, HaikuThreadInfo* info)
{
#if HAVE_OS_H
    if (info == NULL)
    {
        return EINVAL;
    }

    thread_info nativeInfo;
    memset(&nativeInfo, 0, sizeof(nativeInfo));

    status_t status = get_thread_info((thread_id)thread, &nativeInfo);
    if (status != B_OK)
    {
        return (int32_t)status;
    }

    CopyThreadInfo(info, &nativeInfo);
    return (int32_t)status;
#else
    (void)thread;
    (void)info;
    return ENOTSUP;
#endif
}

int32_t SystemNative_GetNextThreadInfo(int32_t team, int32_t* cookie, HaikuThreadInfo* info)
{
#if HAVE_OS_H
    if (cookie == NULL || info == NULL)
    {
        return EINVAL;
    }

    thread_info nativeInfo;
    memset(&nativeInfo, 0, sizeof(nativeInfo));

    status_t status = get_next_thread_info((team_id)team, cookie, &nativeInfo);
    if (status != B_OK)
    {
        return (int32_t)status;
    }

    CopyThreadInfo(info, &nativeInfo);
    return (int32_t)status;
#else
    (void)team;
    (void)cookie;
    (void)info;
    return ENOTSUP;
#endif
}

int32_t SystemNative_GetSystemInfo(SystemInfo* info)
{
#if HAVE_OS_H
    if (info == NULL)
    {
        return EINVAL;
    }

    system_info nativeInfo;
    memset(&nativeInfo, 0, sizeof(nativeInfo));

    status_t status = get_system_info(&nativeInfo);
    if (status != B_OK)
    {
        return (int32_t)status;
    }

    info->boot_time = nativeInfo.boot_time;
    return (int32_t)status;
#else
    (void)info;
    return ENOTSUP;
#endif
}

int32_t SystemNative_GetNextImageInfo(int32_t team, int32_t* cookie, ImageInfo* info)
{
#if HAVE_OS_H && HAVE_IMAGE_H
    if (cookie == NULL || info == NULL)
    {
        return EINVAL;
    }

    image_info nativeInfo;
    memset(&nativeInfo, 0, sizeof(nativeInfo));

    status_t status = get_next_image_info((team_id)team, cookie, &nativeInfo);
    if (status != B_OK)
    {
        return (int32_t)status;
    }

    memset(info, 0, sizeof(*info));
    info->type = nativeInfo.type;
    SafeStringCopy((char*)info->name, sizeof(info->name), (const char*)nativeInfo.name);
    info->text = (uintptr_t)nativeInfo.text;
    info->text_size = nativeInfo.text_size;
    info->data_size = nativeInfo.data_size;
    return (int32_t)status;
#else
    (void)team;
    (void)cookie;
    (void)info;
    return ENOTSUP;
#endif
}
