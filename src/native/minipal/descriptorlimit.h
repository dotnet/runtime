// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_DESCRIPTORLIMIT_H
#define HAVE_MINIPAL_DESCRIPTORLIMIT_H

#include <stdbool.h>

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

bool minipal_increase_descriptor_limit(void);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif // HAVE_MINIPAL_DESCRIPTORLIMIT_H
