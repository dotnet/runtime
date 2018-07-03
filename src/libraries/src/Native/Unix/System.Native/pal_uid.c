// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_config.h"
#include "pal_uid.h"
#include "pal_utilities.h"

#include <assert.h>
#include <errno.h>
#include <stdlib.h>
#include <unistd.h>
#include <sys/types.h>
#include <pwd.h>

static int32_t ConvertNativePasswdToPalPasswd(int error, struct passwd* nativePwd, struct passwd* result, Passwd* pwd)
{
    // positive error number returned -> failure other than entry-not-found
    if (error != 0)
    {
        assert(error > 0);
        memset(pwd, 0, sizeof(Passwd)); // managed out param must be initialized
        return error;
    }

    // 0 returned with null result -> entry-not-found
    if (result == NULL)
    {
        memset(pwd, 0, sizeof(Passwd)); // managed out param must be initialized
        return -1; // shim convention for entry-not-found
    }

    // 0 returned with non-null result (guaranteed to be set to pwd arg) -> success
    assert(result == nativePwd);
    pwd->Name = nativePwd->pw_name;
    pwd->Password = nativePwd->pw_passwd;
    pwd->UserId = nativePwd->pw_uid;
    pwd->GroupId = nativePwd->pw_gid;
    pwd->UserInfo = nativePwd->pw_gecos;
    pwd->HomeDirectory = nativePwd->pw_dir;
    pwd->Shell = nativePwd->pw_shell;
    return 0;
}

int32_t SystemNative_GetPwUidR(uint32_t uid, Passwd* pwd, char* buf, int32_t buflen)
{
    assert(pwd != NULL);
    assert(buf != NULL);
    assert(buflen >= 0);

    if (buflen < 0)
        return EINVAL;

    struct passwd nativePwd;
    struct passwd* result;
    int error;
    while ((error = getpwuid_r(uid, &nativePwd, buf, Int32ToSizeT(buflen), &result)) == EINTR);

    return ConvertNativePasswdToPalPasswd(error, &nativePwd, result, pwd);
}

int32_t SystemNative_GetPwNamR(const char* name, Passwd* pwd, char* buf, int32_t buflen)
{
    assert(pwd != NULL);
    assert(buf != NULL);
    assert(buflen >= 0);

    if (buflen < 0)
        return EINVAL;

    struct passwd nativePwd;
    struct passwd* result;
    int error;
    while ((error = getpwnam_r(name, &nativePwd, buf, Int32ToSizeT(buflen), &result)) == EINTR);

    return ConvertNativePasswdToPalPasswd(error, &nativePwd, result, pwd);
}

uint32_t SystemNative_GetEUid()
{
    return geteuid();
}

uint32_t SystemNative_GetEGid()
{
    return getegid();
}

int32_t SystemNative_SetEUid(uid_t euid)
{
    return seteuid(euid);
}
