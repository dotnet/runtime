// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef CRASHREPORTSTACKWALKER_H
#define CRASHREPORTSTACKWALKER_H

#ifdef FEATURE_INPROC_CRASHREPORT

// Bring up the in-proc crash reporter with only its VM callbacks so on-demand
// reports are possible independently of the startup crash-dump policy.
void CrashReportInitialize();

// Initializes the reporter, starts its crash-dump services, and registers the
// PAL signal-path dispatcher when the in-proc crash-report environment
// settings enable it.
void CrashReportConfigure();

#endif // FEATURE_INPROC_CRASHREPORT

#endif // CRASHREPORTSTACKWALKER_H
