// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Stub ucontext.h for WASI

#ifndef _WASI_UCONTEXT_H
#define _WASI_UCONTEXT_H

typedef struct {
    int dummy;
} mcontext_t;

typedef struct ucontext_t {
    mcontext_t uc_mcontext;
} ucontext_t;

#endif // _WASI_UCONTEXT_H
