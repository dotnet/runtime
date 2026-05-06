// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_config.h"
#include "pal_safecrt.h"
#include "pal_uid.h"
#include "pal_utilities.h"

#include <assert.h>
#include <errno.h>
#include <stdlib.h>
#include <unistd.h>
#include <sys/types.h>


int32_t SystemNative_GetPwUidR(uint32_t uid, Passwd* pwd, char* buf, int32_t buflen)
{
    assert(pwd != NULL);
    assert(buf != NULL);
    assert(buflen >= 0);

    return EINVAL;
}

int32_t SystemNative_GetPwNamR(const char* name, Passwd* pwd, char* buf, int32_t buflen)
{
    assert(pwd != NULL);
    assert(buf != NULL);
    assert(buflen >= 0);

    return EINVAL;
}

uint32_t SystemNative_GetEUid(void)
{
    return EINVAL;
}

uint32_t SystemNative_GetEGid(void)
{
    return EINVAL;
}

int32_t SystemNative_SetEUid(uint32_t euid)
{
    return EINVAL;
}
int32_t SystemNative_GetGroupList(const char* name, uint32_t group, uint32_t* groups, int32_t* ngroups)
{
    assert(name != NULL);
    assert(groups != NULL);
    assert(ngroups != NULL);
    assert(*ngroups >= 0);
    return EINVAL;
}

int32_t SystemNative_GetGroups(int32_t ngroups, uint32_t* groups)
{
    assert(ngroups >= 0);
    assert(groups != NULL);

    return EINVAL;
}

char* SystemNative_GetGroupName(uint32_t gid)
{
    return NULL;
}
