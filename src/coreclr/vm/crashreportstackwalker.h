// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef CRASHREPORTSTACKWALKER_H
#define CRASHREPORTSTACKWALKER_H

#ifdef FEATURE_INPROC_CRASHREPORT

// Bring up the in-proc crash reporter with only its VM callbacks so on-demand
// reports are possible independently of the env-gated crash-dump configuration.
void CrashReportInitialize();

// Based on the DOTNET_* crash-report configuration, initialize the reporter (via
// CrashReportInitialize), start its crash-dump services, and register the PAL
// signal-path dispatcher. A no-op that allocates nothing when crash reporting is
// not configured. Intended to run once at runtime startup.
void CrashReportConfigure();

#endif // FEATURE_INPROC_CRASHREPORT

#endif // CRASHREPORTSTACKWALKER_H
