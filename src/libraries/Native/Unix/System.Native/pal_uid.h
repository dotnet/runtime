// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_types.h"
#include <sys/types.h>

/**
* Passwd struct
*/
typedef struct
{
    char* Name;
    char* Password;
    uint32_t UserId;
    uint32_t GroupId;
    char* UserInfo;
    char* HomeDirectory;
    char* Shell;
} Passwd;

/**
* Gets a password structure for the given uid.
* Implemented as shim to getpwuid_r(3).
*
* Returns 0 for success, -1 if no entry found, positive error
* number for any other failure.
*
*/
PALEXPORT int32_t SystemNative_GetPwUidR(uint32_t uid, Passwd* pwd, char* buf, int32_t buflen);

/**
* Gets a password structure for the given user name.
* Implemented as shim to getpwnam_r(3).
*
* Returns 0 for success, -1 if no entry found, positive error
* number for any other failure.
*/
PALEXPORT int32_t SystemNative_GetPwNamR(const char* name, Passwd* pwd, char* buf, int32_t buflen);

/**
* Gets and returns the effective user's identity.
* Implemented as shim to geteuid(2).
*
* Always succeeds.
*/
PALEXPORT uint32_t SystemNative_GetEUid(void);

/**
* Gets and returns the effective group's identity.
* Implemented as shim to getegid(2).
*
* Always succeeds.
*/
PALEXPORT uint32_t SystemNative_GetEGid(void);

/**
* Sets the effective user ID of the calling process
* Implemented as a shim to seteuid(2).
*
* Returns 0 for success. On error, -1 is returned and errno is set.
*/
PALEXPORT int32_t SystemNative_SetEUid(uint32_t euid);

/**
* Gets the list of groups to which a user belongs.
* Implemented as a shim to getgrouplist.
*
* Returns number of groups for success.
* If the buffer is too small, -1 is returned and ngroups contains the required size.
* On error, -1 is returned and errno is set.
*/
PALEXPORT int32_t SystemNative_GetGroupList(const char* name, uint32_t group, uint32_t* groups, int32_t* ngroups);

/**
* Gets and returns the real user's identity.
* Implemented as shim to getuid(2).
*
* Always succeeds.
*/
PALEXPORT uint32_t SystemNative_GetUid(void);

/**
* Gets groups associated with current process.
*
* Returns number of groups for success.
* On error, -1 is returned and errno is set.
* If the buffer is too small, errno is EINVAL.
*/
PALEXPORT int32_t SystemNative_GetGroups(int32_t ngroups, uint32_t* groups);
