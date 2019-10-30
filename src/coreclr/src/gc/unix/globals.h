// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __GLOBALS_H__
#define __GLOBALS_H__

#if HAVE_MACH_ABSOLUTE_TIME
#include <mach/mach_time.h>
#endif // HAVE_MACH_ABSOLUTE_TIME

const int tccSecondsToMilliSeconds = 1000;

// The number of microseconds in a second.
const int tccSecondsToMicroSeconds = 1000000;

// The number of nanoseconds in a second.
const int tccSecondsToNanoSeconds = 1000000000;

// The number of microseconds in a millisecond.
const int tccMilliSecondsToMicroSeconds = 1000;

// The number of nanoseconds in a millisecond.
const int tccMilliSecondsToNanoSeconds = 1000000;

#if HAVE_MACH_ABSOLUTE_TIME
extern mach_timebase_info_data_t g_TimebaseInfo;
#endif // HAVE_MACH_ABSOLUTE_TIME

#endif // __GLOBALS_H__
