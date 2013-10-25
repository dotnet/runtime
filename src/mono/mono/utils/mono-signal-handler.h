/*
 * mono-signal-handler.h: Handle signal handler differences across platforms
 *
 * Copyright (C) 2013 Xamarin Inc
 */

#ifndef __MONO_SIGNAL_HANDLER_H__
#define __MONO_SIGNAL_HANDLER_H__

#include "config.h"

#ifdef ENABLE_EXTENSION_MODULE
#include "../../../mono-extensions/mono/utils/mono-signal-handler.h"
#endif
/*
Not all platforms support signal handlers in the same way. Some have the same concept but
for some weird reason pass different kind of arguments.

All signal handler helpers should go here so they can be properly shared across the JIT,
utils and sgen.

TODO: Cleanup & move mini's macros to here so they can leveraged by other parts.

*/
#ifndef MONO_SIGNAL_HANDLER_FUNC
#define MONO_SIGNAL_HANDLER_FUNC(access, name, arglist) access void name arglist
#endif

#endif
