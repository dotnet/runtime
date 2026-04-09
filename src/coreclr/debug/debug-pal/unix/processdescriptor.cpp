// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <pal.h>
#include <unistd.h>
#include <fcntl.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <limits.h>
#include <pal_assert.h>
#include "processdescriptor.h"

ProcessDescriptor ProcessDescriptor::FromCurrentProcess()
{
#ifdef __APPLE__
    return Create(GetCurrentProcessId(), PAL_GetApplicationGroupId());
#else
    return Create(GetCurrentProcessId(), nullptr);
#endif
}
