// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <emscripten.h>
#include <assert.h>

// this points to System.Threading.TimerQueue.TimerHandler C# method
static void (*timer_handler)() = NULL;

void
SystemJS_ExecuteTimerCallback (void)
{
	// callback could be null if timer was never used by the application, but only by prevent_timer_throttling_tick()
	if (timer_handler==NULL) {
		return;
	}
    timer_handler();
}

void 
SystemJS_InstallTimerCallback(void (*timerHandler)())
{
    assert (timerHandler);
    timer_handler = timerHandler;
}