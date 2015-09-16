// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once

#include "pal_types.h"

/**
* Passwd struct
*/
struct Passwd
{
    char* Name;
    char* Password;
    uint32_t UserId;
    uint32_t GroupId;
    char* UserInfo;
    char* HomeDirectory;
    char* Shell;
};

/**
* Gets a password structure for the given uid.
* Implemented as shim to getpwuid_r(3).
*
* Returns 0 for success, -1 if no entry found, positive error
* number for any other failure.
*
*/
extern "C" int32_t GetPwUidR(uint32_t uid, Passwd* pwd, char* buf, int32_t buflen);

/**
* Gets and returns the effective user's identity.
* Implemented as shim to geteuid(2).
*
* Always succeeds.
*/
extern "C" uint32_t GetEUid();

/**
* Gets and returns the effective group's identity.
* Implemented as shim to getegid(2).
*
* Always succeeds.
*/
extern "C" uint32_t GetEGid();
