// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// WASI dlfcn.h wrapper — adds Dl_info and dladdr stubs.

#ifndef _WASI_DLFCN_WRAPPER_H
#define _WASI_DLFCN_WRAPPER_H

#include_next <dlfcn.h>

#ifndef RTLD_NOLOAD
#define RTLD_NOLOAD 0
#endif

typedef struct {
    const char *dli_fname;
    void *dli_fbase;
    const char *dli_sname;
    void *dli_saddr;
} Dl_info;

static inline int dladdr(const void *addr, Dl_info *info) { (void)addr; (void)info; return 0; }

#endif // _WASI_DLFCN_WRAPPER_H
