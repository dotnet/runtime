// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __AUTO_TRACE_H__
#define __AUTO_TRACE_H__
#ifdef FEATURE_AUTO_TRACE

void auto_trace_init();
void auto_trace_launch();
void auto_trace_launch_internal();
void auto_trace_wait();
void auto_trace_signal();

#endif // FEATURE_AUTO_TRACE
#endif // __AUTO_TRACE_H__
