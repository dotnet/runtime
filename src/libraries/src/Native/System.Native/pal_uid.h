// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once

#include <stdint.h>

/**
* Passwd struct 
*/
struct Passwd {
	char*   Name;
	char*   Password; 
	int32_t UserId;
	int32_t GroupId;
	char*   UserInfo;
	char*   HomeDirectory;
	char*   Shell;
};

/**
* Gets a password structure for the given uid. 
* Implemented as shim to getpwuid_r(3).
*
* Returns 0 for success, -1 for failure. Sets errno on failure.
*/
extern "C"
int32_t GetPwUid(
	int32_t  uid,
	Passwd*  pwd,
	char*    buf,
	int64_t  buflen,
	Passwd** result);

/**
* Gets and returns the effective user's identity.
* Implemented as shim to geteuid(2).
*
* Always succeeds.
*/
extern "C"
int32_t GetEUid();

/**
* Gets and returns the effective group's identity.
* Implemented as shim to getegid(2).
*
* Always succeeds.
*/
extern "C"
int32_t GetEGid();
