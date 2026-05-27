// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Stub pwd.h for WASI

#ifndef _WASI_PWD_H
#define _WASI_PWD_H

#include <sys/types.h>

struct passwd {
    char *pw_name;
    char *pw_passwd;
    uid_t pw_uid;
    gid_t pw_gid;
    char *pw_gecos;
    char *pw_dir;
    char *pw_shell;
};

static inline struct passwd *getpwuid(uid_t uid) { (void)uid; return (struct passwd *)0; }

#endif // _WASI_PWD_H
