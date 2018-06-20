// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "pal_compiler.h"

BEGIN_EXTERN_C

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
DLLEXPORT int32_t SystemNative_GetPwUidR(uint32_t uid, Passwd* pwd, char* buf, int32_t buflen);

/**
* Gets a password structure for the given user name.
* Implemented as shim to getpwnam_r(3).
*
* Returns 0 for success, -1 if no entry found, positive error
* number for any other failure.
*/
DLLEXPORT int32_t SystemNative_GetPwNamR(const char* name, Passwd* pwd, char* buf, int32_t buflen);

/**
* Gets and returns the effective user's identity.
* Implemented as shim to geteuid(2).
*
* Always succeeds.
*/
DLLEXPORT uint32_t SystemNative_GetEUid(void);

/**
* Gets and returns the effective group's identity.
* Implemented as shim to getegid(2).
*
* Always succeeds.
*/
DLLEXPORT uint32_t SystemNative_GetEGid(void);

/**
* Sets the effective user ID of the calling process
* Implemented as a shim to seteuid(2).
*
* Returns 0 for success. On error, -1 is returned and errno is set.
*/
DLLEXPORT int32_t SystemNative_SetEUid(uid_t euid);

END_EXTERN_C
