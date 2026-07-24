// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef CRASHREPORTSTACKWALKER_H
#define CRASHREPORTSTACKWALKER_H

// Bring up the in-proc crash reporter with only its VM callbacks so on-demand
// reports are possible independently of the env-gated crash-dump configuration.
// A no-op on platforms where FEATURE_INPROC_CRASHREPORT is not defined.
void CrashReportInitialize();

// Based on the DOTNET_* crash-report configuration, initialize the reporter (via
// CrashReportInitialize), start its crash-dump services, and register the PAL
// signal-path dispatcher so the in-proc reporter replaces createdump. Intended to run
// once at runtime startup. A no-op unless the reporter is configured to replace
// createdump (FEATURE_INPROC_CRASHREPORT_REPLACE_CREATEDUMP), which is where the PAL
// signal callback would otherwise be registered.
void CrashReportConfigure();

#endif // CRASHREPORTSTACKWALKER_H
