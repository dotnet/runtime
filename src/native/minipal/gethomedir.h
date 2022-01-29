// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_GETHOMEDIR_H
#define HAVE_MINIPAL_GETHOMEDIR_H

#include <errno.h>
#include <pwd.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>

#ifdef __cplusplus
extern "C" {
#endif

// Returns the full path to the home directory.
// The caller is responsible for releasing the buffer. Returns null on error.
static inline char* minipal_gethomedir(void)
{
    char* homedir = getenv("HOME");
    if (!homedir)
    {
        uid_t uid = getuid();
        struct passwd* pwuid = NULL;
        while ((pwuid = getpwuid(uid)) == NULL && errno == EINTR);
        if (pwuid)
        {
            homedir = pwuid->pw_dir;
        }
    }

    return homedir ? realpath(homedir, NULL) : NULL;
}

#ifdef __cplusplus
}
#endif // extern "C"

#endif // HAVE_MINIPAL_GETHOMEDIR_H
