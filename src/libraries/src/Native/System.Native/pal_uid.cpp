// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "../config.h"
#include "pal_uid.h"

#include <assert.h>
#include <stdlib.h>
#include <unistd.h>
#include <sys/types.h>
#include <pwd.h>

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
	Passwd** result)
{
	assert(pwd != nullptr);
	assert(buf != nullptr);
	assert(buflen >= 0);
	assert(result != nullptr);

	struct passwd nativePwd;
	struct passwd* nativePwdResultPtr;
	int rv = getpwuid_r(uid, &nativePwd, buf, buflen, &nativePwdResultPtr);

	// If successful, the result will be null if the user couldn't be found,
	// or it'll contain the address of the provided pwd structure, into
	// which the results are stored.
	if (rv == 0 && nativePwdResultPtr != nullptr)
	{
		assert(nativePwdResultPtr == &nativePwd);
		
		pwd->Name = nativePwd.pw_name;
		pwd->Password = nativePwd.pw_passwd;
		pwd->UserId = nativePwd.pw_uid;
		pwd->GroupId = nativePwd.pw_gid;
		pwd->UserInfo = nativePwd.pw_gecos;
		pwd->HomeDirectory = nativePwd.pw_dir;
		pwd->Shell = nativePwd.pw_shell;
		*result = pwd;
	}
	else
	{
		// Make sure we zero out the results fields, as the managed
		// caller could be using out variables and expect them to be initialized
		// after the call.
		*pwd = { };
		*result = nullptr;
	}

	return rv;
}

extern "C"
int32_t GetEUid()
{
	return geteuid();
}

extern "C"
int32_t GetEGid()
{
	return getegid();
}
