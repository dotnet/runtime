// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_TIME_H
#define HAVE_MINIPAL_TIME_H

#include <stdint.h>

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

    // Returns current count of high resolution monotonically increasing timer ticks
    int64_t minipal_hires_ticks();

    // Returns the frequency of high resolution timer ticks in Hz
    int64_t minipal_hires_tick_frequency();

    // Delays execution of current thread by `usecs` microseconds.
    // The delay is best-effort and may take longer than desired.
    // Some delays, depending on OS and duration, could be implemented via busy waiting.
    //
    // If not NULL, `usecsSinceYield` keeps track of busy-waiting time, so that
    // the containing algorithm could handle cases when busy-waiting time is too high.
    void minipal_microdelay(uint32_t usecs, uint32_t* usecsSinceYield);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif /* HAVE_MINIPAL_TIME_H */
