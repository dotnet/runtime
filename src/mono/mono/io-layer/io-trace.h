/*
 * io-trace.h: tracing macros
 *
 * Authors:
 *  Marek Habersack <grendel@twistedcode.net>
 *
 * Copyright 2016 Xamarin, Inc (http://xamarin.com/)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef __IO_TRACE_H

#ifdef DISABLE_IO_LAYER_TRACE
#define MONO_TRACE(...)
#else
#include "mono/utils/mono-logger-internals.h"
#define MONO_TRACE(...) mono_trace (__VA_ARGS__)
#endif

#endif
