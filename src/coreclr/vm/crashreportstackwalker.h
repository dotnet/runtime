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
// signal-path dispatcher so the in-proc reporter replaces createdump. Intended to run
// once at runtime startup. A no-op unless the reporter is configured to replace
// createdump (FEATURE_INPROC_CRASHREPORT_REPLACE_CREATEDUMP), which is where the PAL
// signal callback would otherwise be registered.
void CrashReportConfigure();

#else // !FEATURE_INPROC_CRASHREPORT

// The in-proc crash reporter is not compiled on this platform. These are called
// unconditionally from the VM startup and fatal paths, so provide inline no-ops.
inline void CrashReportInitialize() {}
inline void CrashReportConfigure() {}

#endif // FEATURE_INPROC_CRASHREPORT

#endif // CRASHREPORTSTACKWALKER_H
