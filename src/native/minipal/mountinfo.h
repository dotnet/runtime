// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_MOUNTINFO_H
#define HAVE_MINIPAL_MOUNTINFO_H

#include <stdbool.h>
#include <stddef.h>

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

bool minipal_get_mountinfo_paths(const char* line, char* root, char* mountPoint, size_t bufferSize);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif // HAVE_MINIPAL_MOUNTINFO_H
