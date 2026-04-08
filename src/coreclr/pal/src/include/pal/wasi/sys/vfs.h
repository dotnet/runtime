// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Stub sys/vfs.h for WASI

#ifndef _WASI_SYS_VFS_H
#define _WASI_SYS_VFS_H

struct statfs {
    long f_type;
    long f_bsize;
    long f_blocks;
    long f_bfree;
    long f_bavail;
};

static inline int statfs(const char *path, struct statfs *buf) { (void)path; (void)buf; return -1; }

#endif // _WASI_SYS_VFS_H
