// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_config.h"
#include "pal_uid.h"
#include "pal_utilities.h"

#include <assert.h>
#include <errno.h>
#include <stdlib.h>
#include <unistd.h>
#include <sys/types.h>
#include <pwd.h>

extern "C" int32_t GetPwUidR(uint32_t uid, Passwd* pwd, char* buf, int32_t buflen)
{
    assert(pwd != nullptr);
    assert(buf != nullptr);
    assert(buflen >= 0);

    if (buflen < 0)
        return EINVAL;

    struct passwd nativePwd;
    struct passwd* result;
    int error = getpwuid_r(uid, &nativePwd, buf, UnsignedCast(buflen), &result);

    // positive error number returned -> failure other than entry-not-found
    if (error != 0)
    {
        assert(error > 0);
        *pwd = {}; // managed out param must be initialized
        return error;
    }

    // 0 returned with null result -> entry-not-found
    if (result == nullptr)
    {
        *pwd = {}; // managed out param must be initialized
        return -1; // shim convention for entry-not-found
    }

    // 0 returned with non-null result (guaranteed to be set to pwd arg) -> success
    assert(result == &nativePwd);
    pwd->Name = nativePwd.pw_name;
    pwd->Password = nativePwd.pw_passwd;
    pwd->UserId = nativePwd.pw_uid;
    pwd->GroupId = nativePwd.pw_gid;
    pwd->UserInfo = nativePwd.pw_gecos;
    pwd->HomeDirectory = nativePwd.pw_dir;
    pwd->Shell = nativePwd.pw_shell;
    return 0;
}

extern "C" uint32_t GetEUid()
{
    return geteuid();
}

extern "C" uint32_t GetEGid()
{
    return getegid();
}
