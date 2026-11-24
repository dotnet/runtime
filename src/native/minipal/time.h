// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_TIME_H
#define HAVE_MINIPAL_TIME_H

#include <stdbool.h>
#include <stdint.h>

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

/**
 * Get the current count of high resolution monotonically increasing timer ticks.
 *
 * @return The tick count.
 */
int64_t minipal_hires_ticks(void);

/**
 * Get the frequency of high resolution timer ticks.
 *
 * @return The frequency in Hz.
 */
int64_t minipal_hires_tick_frequency(void);

/**
 * Get a low-precision monotonically increasing timer.
 *
 * @return The timer value in milliseconds.
 */
int64_t minipal_lowres_ticks(void);

/**
 * Suspend execution of the current thread for the specified number of milliseconds.
 *
 * @param milliseconds The number of milliseconds. UINT32_MAX suspends indefinitely; zero yields the remainder
 *        of the thread's time slice.
 */
void minipal_sleep(uint32_t milliseconds);

/**
 * Yield to another thread, periodically sleeping after repeated yields.
 *
 * @param switchCount The number of yields so far.
 * @return true if the OS yield succeeded, false otherwise.
 */
bool minipal_switch_to_thread(uint32_t switchCount);

/**
 * Delay execution of the current thread by `usecs` microseconds.
 * The delay is best-effort and may take longer than desired.
 * Some delays, depending on OS and duration, could be implemented via busy waiting.
 *
 * @param usecs The delay in microseconds.
 * @param usecsSinceYield If not NULL, keeps track of busy-waiting time, so that the containing algorithm
 *        could handle cases when busy-waiting time is too high.
 */
void minipal_microdelay(uint32_t usecs, uint32_t* usecsSinceYield);

/**
 * Get the system time in Windows FILETIME precision.
 *
 * @return The time in 100ns units since January 1, 1601.
 */
uint64_t minipal_get_system_time(void);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif /* HAVE_MINIPAL_TIME_H */
