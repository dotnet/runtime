/*
 * io-trace.h: tracing macros
 *
 * Authors:
 *  Marek Habersack <grendel@twistedcode.net>
 *
 * Copyright 2016 Xamarin, Inc (http://xamarin.com/)
 */

#ifndef __IO_TRACE_H

#ifdef DISABLE_IO_LAYER_TRACE
#define MONO_TRACE(...)
#else
#define MONO_TRACE(...) mono_trace (__VA_ARGS__)
#endif

#endif
