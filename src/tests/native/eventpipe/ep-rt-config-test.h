// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef EP_RT_CONFIG_TEST_H
#define EP_RT_CONFIG_TEST_H

#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>

#define EP_THREAD_INCLUDE_ACTIVITY_ID

#ifdef HOST_WINDOWS
#define HOST_WIN32
#endif

#ifdef _DEBUG
#define EP_CHECKED_BUILD
#endif

#endif
