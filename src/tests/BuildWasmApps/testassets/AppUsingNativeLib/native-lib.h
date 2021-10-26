// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _NATIVELIB_H_
#define _NATIVELIB_H_

#ifdef __cplusplus
extern "C" {
#endif

int print_line(int x);

typedef int (*ManagedIntIntCallback)(int x);

int native_intint_callback_acceptor(ManagedIntIntCallback fn, int i);

#ifdef __cplusplus
}
#endif

#endif // _NATIVELIB_H_
