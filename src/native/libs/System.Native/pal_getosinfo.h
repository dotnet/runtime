// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_types.h"

#define SYSTEMNATIVE_OS_NAME_LENGTH 32
#define SYSTEMNATIVE_MAX_PATH 1024

typedef struct
{
    uintptr_t size;
    uint32_t ram_size;
} AreaInfo;

typedef struct
{
    int32_t team;
    int32_t session_id;
    int32_t parent;
    uint8_t name[SYSTEMNATIVE_OS_NAME_LENGTH];
    int64_t start_time;
} TeamInfo;

typedef struct
{
    int64_t user_time;
    int64_t kernel_time;
} TeamUsageInfo;

typedef struct
{
    int32_t thread;
    int32_t team;
    int32_t state;
    int32_t priority;
    int64_t user_time;
    int64_t kernel_time;
} HaikuThreadInfo;

typedef struct
{
    int64_t boot_time;
} SystemInfo;

typedef struct
{
    int32_t type;
    uint8_t name[SYSTEMNATIVE_MAX_PATH];
    uintptr_t text;
    int32_t text_size;
    int32_t data_size;
} ImageInfo;

PALEXPORT int32_t SystemNative_GetNextAreaInfo(int32_t team, intptr_t* cookie, AreaInfo* areaInfo);
PALEXPORT int32_t SystemNative_GetTeamInfo(int32_t team, TeamInfo* info);
PALEXPORT int32_t SystemNative_GetNextTeamInfo(int32_t* cookie, TeamInfo* info);
PALEXPORT int32_t SystemNative_GetNextTeamId(int32_t* cookie, int32_t* team);
PALEXPORT int32_t SystemNative_GetTeamUsageInfo(int32_t team, int32_t who, TeamUsageInfo* info);
PALEXPORT int32_t SystemNative_SetThreadPriority(int32_t thread, int32_t newPriority);
PALEXPORT int32_t SystemNative_GetThreadInfo(int32_t thread, HaikuThreadInfo* info);
PALEXPORT int32_t SystemNative_GetNextThreadInfo(int32_t team, int32_t* cookie, HaikuThreadInfo* info);
PALEXPORT int32_t SystemNative_GetSystemInfo(SystemInfo* info);
PALEXPORT int32_t SystemNative_GetNextImageInfo(int32_t team, int32_t* cookie, ImageInfo* info);
